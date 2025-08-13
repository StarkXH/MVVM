using SupervisorApp.Core.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace SupervisorApp.Core.Messaging
{
    public class MessageBus : IMessageBus
    {
        #region 私有字段

        // 设计要点1: 线程安全的处理器存储
        /// <summary>
        /// 1. ConcurrentDictionary - 线程安全
        /// - UI线程处理用户操作
        /// - 后台线程采集数据
        /// - 定时器线程检查状态
        /// - 网络线程接收数据
        /// 
        /// 2. ConcurrentBag - 高性能的线程安全集合
        /// 特点: 添加操作非常快，适合多写少读的场景
        /// 缺点: 不支持直接删除(这就是为什么取消订阅比较麻烦)
        /// 
        /// 3. Type 作为 Key
        /// 允许基于消息类型进行路由：
        /// Type messageType = typeof(DeviceConnectedMessage);
        /// _handlers[messageType] // 获取所有处理该类型消息的处理器
        /// 
        /// 4. string 作为 Key
        /// 允许命名消息，更灵活:
        /// _namedHandlers["DeviceStatus"] // 获取"设备状态"消息的处理器
        /// </summary>
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IMessageHandler>> _handlers
                        = new ConcurrentDictionary<Type, ConcurrentBag<IMessageHandler>>();

        // 命名处理器字典
        private readonly ConcurrentDictionary<string, ConcurrentBag<IMessageHandler>> _namedHandlers
                        = new ConcurrentDictionary<string, ConcurrentBag<IMessageHandler>>();

        private readonly object _lockObject = new object();

        //        MessageBus(单例)
        //          │
        //          ├── _handlers(全局字典)
        //          │   ├── [typeof(DeviceMessage)] → handlers(ConcurrentBag)
        //          │   │   ├── ActionMessageHandler<DeviceMessage> { handler1 }
        //          │   │   ├── ActionMessageHandler<DeviceMessage> { handler2 }
        //          │   │   └── AsyncMessageHandler<DeviceMessage> { handler3 }
        //          │   │
        //          │   ├── [typeof(ErrorMessage)] → handlers(ConcurrentBag)
        //          │   │   └── ActionMessageHandler<ErrorMessage> { handler4 }
        //          │   │
        //          │   └── [typeof(DataMessage)] → handlers(ConcurrentBag)
        //          │       ├── ActionMessageHandler<DataMessage> { handler5 }
        //          │       └── ActionMessageHandler<DataMessage> { handler6 }

        #endregion

        #region 单例实现

        // 设计要点2: 单例模式，确保全局唯一的消息总线实例
        private static readonly Lazy<IMessageBus> _instance = new Lazy<IMessageBus>(() => new MessageBus());
        public static IMessageBus Instance => _instance.Value;

        // 私有构造函数，确保单例
        private MessageBus() { }

        #endregion

        #region 订阅方法

        // 1. 用户定义的 handler（具体处理逻辑）
        // Action<DeviceMessage> handler = (msg) =>
        // {
        //     Console.WriteLine($"设备 {msg.DeviceId} 状态：{msg.Status}");
        // };

        // 2. 订阅时的流程
        // MessageBus.Instance.Subscribe<DeviceMessage>(handler);

        // 内部流程：
        // a) 从 _handlers 获取 DeviceMessage 类型对应的 handlers 集合
        // var handlers = _handlers.GetOrAdd(typeof(DeviceMessage), _ => new ConcurrentBag<IMessageHandler>());

        // b) 将用户的 handler 包装成 MessageHandler
        // var messageHandler = new ActionMessageHandler<DeviceMessage>(handler, priority);

        // c) 添加到特定类型的 handlers 集合中
        // handlers.Add(messageHandler);


        /// <summary>
        /// 同步订阅消息处理器
        /// 设计要点：为什么需要泛型约束 where T : class？
        /// 1. 确保消息类型是引用类型（避免装箱拆箱）
        /// 2. 消息通常包含复杂数据，值类型不合适
        /// 3. 便于消息的多态处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Subscribe<T>(Action<T> handler, MessagePriority priority = MessagePriority.Normal) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageHandler = new ActionMessageHandler<T>(handler, priority);
            var handlers = _handlers.GetOrAdd(typeof(T), _ => new ConcurrentBag<IMessageHandler>());
            handlers.Add(messageHandler);

            LogSubscription(typeof(T).Name, "同步", priority);
        }

        /// <summary>
        /// 异步订阅消息处理器
        /// 为什么需要？
        /// 上位机中的异步场景：
        /// - 数据保存：await _database.SaveAsync(data);
        /// - 网络通信：await _client.SendAsync(message);
        /// - 文件操作：await File.WriteAllTextAsync(path, content);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SubscribeAsync<T>(Func<T, Task> handler, MessagePriority priority = MessagePriority.Normal) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageHandler = new AsyncMessageHandler<T>(handler, priority);
            var handlers = _handlers.GetOrAdd(typeof(T), _ => new ConcurrentBag<IMessageHandler>());
            handlers.Add(messageHandler);

            LogSubscription(typeof(T).Name, "异步", priority);
        }

        /// <summary>
        /// 命名消息订阅
        /// 使用场景：
        /// 1. 同一个消息类型在不同上下文中有不同含义
        /// 2. 需要更精确的消息路由控制
        /// 3. 便于调试和日志记录
        /// 例如：
        /// Subscribe string ("DeviceError", msg => HandleDeviceError(msg));
        /// Subscribe string ("UserMessage", msg => ShowUserMessage(msg));
        /// 虽然都是 string 类型，但处理方式完全不同
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageName"></param>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Subscribe<T>(string messageName, Action<T> handler, MessagePriority priority = MessagePriority.Normal) where T : class
        {
            if (string.IsNullOrWhiteSpace(messageName)) throw new ArgumentNullException("消息名称不能为空", nameof(messageName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageHandler = new ActionMessageHandler<T>(handler, priority);
            var handlers = _namedHandlers.GetOrAdd(messageName, _ => new ConcurrentBag<IMessageHandler>());
            handlers.Add(messageHandler);

            LogSubscription(messageName, "命名消息", priority);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;

            if (_handlers.TryGetValue(typeof(T), out var handlers))
            {
                lock (_lockObject)
                {
                    // 创建新的处理器集合，排除要取消订阅的处理器
                    var newHandlers = new ConcurrentBag<IMessageHandler>();
                    var targetHandler = new ActionMessageHandler<T>(handler, MessagePriority.Normal);

                    foreach (var h in handlers)
                    {
                        if (!h.Equals(targetHandler))
                        {
                            newHandlers.Add(h);
                        }
                    }

                    _handlers.TryUpdate(typeof(T), newHandlers, handlers);
                }
            }

            LogUnsubscription(typeof(T).Name);
        }

        #endregion

        #region 发布方法

        // 1. 消息发布
        // MessageBus.Instance.Publish(new DeviceMessage { Status = "Connected" });
        
        // 2. PublishToTypeHandlers 获取并排序处理器
        // var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();
        
        // 3. ExecuteHandlers 进行线程调度
        // ExecuteHandlers(sortedHandlers, message);
        
        // 4a. 如果在非UI线程，调度到UI线程
        // Application.Current.Dispatcher.BeginInvoke(() => ExecuteHandlersInternal(...));
        
        // 4b. 如果在UI线程，直接执行
        // ExecuteHandlersInternal(sortedHandlers, message);
        
        // 5. ExecuteHandlersInternal 遍历执行
        // foreach (var handler in handlers)
        // {
        //     handler.Handle(message);  // 最终执行用户的处理逻辑
        // }

        /// <summary>
        /// 发布消息(同步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public void Publish<T>(T message) where T : class
        {
            if (message == null) return;

            LogPublish(typeof(T).Name, "同步");
            PublishToTypeHandlers(message);
        }

        /// <summary>
        /// 发布命名消息(同步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageName"></param>
        /// <param name="message"></param>
        public void Publish<T>(string messageName, T message) where T : class
        {
            if (string.IsNullOrWhiteSpace(messageName) || message == null) return;

            LogPublish(messageName, "命名消息");
            PublishToNamedHandlers(messageName, message);
        }

        /// <summary>
        /// 发布消息(异步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishAsync<T>(T message) where T : class
        {
            if (message == null) return;

            LogPublish(typeof(T).Name, "异步");
            await PublishToTypeHandlersAsync(message);
        }

        /// <summary>
        /// 发布命名消息(异步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishAsync<T>(string messageName, T message) where T : class
        {
            if (string.IsNullOrWhiteSpace(messageName) || message == null) return;

            LogPublish(messageName, "命名异步消息");
            await PublishToNamedHandlersAsync(messageName, message);
        }

        #endregion

        #region 私有实现方法

        /// <summary>
        /// 发布到类型处理器(同步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        private void PublishToTypeHandlers<T>(T message) where T : class
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers) || !handlers.Any())
            {
                LogNoHandlers(typeof(T).Name);
                return;
            }

            var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();
            LogHandlerCount(typeof(T).Name, sortedHandlers.Count);

            ExecuteHandlers(sortedHandlers, message);
        }

        /// <summary>
        /// 发布到命名处理器(同步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageName"></param>
        /// <param name="message"></param>
        private void PublishToNamedHandlers<T>(string messageName, T message) where T : class
        {
            if (!_namedHandlers.TryGetValue(messageName, out var handlers) || !handlers.Any())
            {
                LogNoHandlers(messageName);
                return;
            }

            var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();
            LogHandlerCount(messageName, sortedHandlers.Count);

            ExecuteHandlers(sortedHandlers, message);
        }

        /// <summary>
        /// 发布到类型处理器(异步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishToTypeHandlersAsync<T>(T message) where T : class
        {
            if (_handlers.TryGetValue(typeof(T), out var handlers) || !handlers.Any())
            {
                LogNoHandlers(typeof(T).Name);
                return;
            }

            var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();
            LogHandlerCount(typeof(T).Name, sortedHandlers.Count);

            await ExecuteHandlersAsync(sortedHandlers, message);
        }

        /// <summary>
        /// 发布到命名处理器(异步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishToNamedHandlersAsync<T>(string messageName, T message) where T : class
        {
            if (_namedHandlers.TryGetValue(messageName, out var handlers) || !handlers.Any())
            {
                LogNoHandlers(messageName);
                return;
            }

            var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();
            LogHandlerCount(messageName, sortedHandlers.Count);

            await ExecuteHandlersAsync(sortedHandlers, message);
        }

        /// <summary>
        /// 执行处理器(同步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handlers"></param>
        /// <param name="message"></param>
        private void ExecuteHandlers<T>(List<IMessageHandler> handlers, T message) where T : class
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                // 需要在UI线程执行
                Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                {
                    ExecuteHandlersInternal(handlers, message);
                }));
            }
            else
            {
                // 当前已在正确线程或非UI环境
                ExecuteHandlersInternal(handlers, message);
            }
        }


        /// <summary>
        /// 执行处理器内部实现
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="message"></param>
        private void ExecuteHandlersInternal(List<IMessageHandler> handlers, object message)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handle(message);
                }
                catch (Exception ex)
                {
                    HandleMessageError(ex, message, handler);
                }
            }
        }

        //ExecuteHandlersAsync 开始
        //│
        //├── 遍历所有处理器
        //│   ├── 异步处理器 → 添加到 tasks 集合
        //│   │   ├── SaveToDatabase Task
        //│   │   ├── SendToServer Task  
        //│   │   └── GenerateReport Task
        //│   │
        //│   └── 同步处理器 → 立即执行
        //│       ├── UI线程检查
        //│       └── 更新界面
        //│
        //└── await Task.WhenAll(tasks) // 等待所有异步任务完成

        /// <summary>
        /// 异步执行处理器
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task ExecuteHandlersAsync(List<IMessageHandler> handlers, object message)
        {
            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is IAsyncMessageHandler asyncHandler)
                    {
                        // 异步处理器
                        tasks.Add(asyncHandler.HandleAsync(message));
                    }
                    else
                    {
                        // 同步处理器，在UI线程执行
                        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                        {
                            // 等待同步处理器在UI线程执行完成
                            await Application.Current.Dispatcher.InvokeAsync(() => handler.Handle(message));
                            // 只有这个处理器执行完成后，才会继续下一个
                        }
                        else
                        {
                            // 当前已在正确线程或非UI环境
                            handler.Handle(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleMessageError(ex, message, handler);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    LogError($"异步处理器执行失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理消息错误
        /// </summary>
        private void HandleMessageError(Exception exception, object message, IMessageHandler handler)
        {
            var errorInfo = $"消息处理错误 - 消息类型: {message?.GetType().Name ?? "Unknown"}, " +
                           $"处理器: {handler?.GetType().Name ?? "Unknown"}, " +
                           $"错误: {exception?.Message ?? "Unknown"}";

            LogError(errorInfo);

            // 这里可以添加更复杂的错误处理逻辑
            // 例如：发送错误通知、记录错误统计等
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                var totalHanlders = _handlers.Values.Sum(bag => bag.Count) + _namedHandlers.Values.Sum(bag => bag.Count);

                _handlers.Clear();
                _namedHandlers.Clear();

                LogClear(totalHanlders);
            }
        }

        #endregion

        #region 日志记录方法

        private void LogSubscription(string messageType, string handlerType, MessagePriority priority)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 订阅 - 消息: {messageType}, 类型: {handlerType}, 优先级: {priority}");
        }

        private void LogUnsubscription(string messageType)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 取消订阅 - 消息: {messageType}");
        }

        private void LogPublish(string messageType, string publishType)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 发布 - 消息: {messageType}, 类型: {publishType}");
        }

        private void LogNoHandlers(string messageType)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 没有找到处理器 - 消息: {messageType}");
        }

        private void LogHandlerCount(string messageType, int count)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 执行 - 消息: {messageType}, 处理器数量: {count}");
        }

        private void LogError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 错误 - {message}");
        }

        private void LogClear(int totalHandlers)
        {
            System.Diagnostics.Debug.WriteLine($"[MessageBus] 清理 - 总共清理了 {totalHandlers} 个处理器");
        }

        #endregion

        #region 调试和统计方法

        public string GetSubscriptionInfo()
        {
            var typeSubscription = _handlers.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value.Count);
            var namedSubscription = _namedHandlers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

            var info = "=== MessageBus 订阅信息 ===\n";
            info += "类型订阅数量: {typeSubscription.Count}\n";
            info += "命名订阅数量: {namedSubscription.Count}\n\n";
            info += "详细信息:\n";

            foreach (var sub in typeSubscription)
            {
                info += $"  {sub.Key}: {sub.Value} 个处理器\n";
            }

            foreach (var sub in namedSubscription)
            {
                info += $"  {sub.Key}: {sub.Value} 个处理器\n";
            }

            return info;
        }

        #endregion
    }
}
