using System;
using System.Threading.Tasks;

namespace SupervisorApp.Core.Messaging
{
    /// <summary>
    /// 消息处理基础接口
    /// 为什么需要 IMessageHandler 接口?
    /// 1. 统一处理不同类型的处理器
    /// 2. 支持优先级排序
    /// 3. 方便扩展(异步处理器、过滤器等)
    /// </summary>
    public interface IMessageHandler
    {
        MessagePriority Priority { get; }
        void Handle(object message);
    }

    /// <summary>
    /// 异步消息处理器接口
    /// 为什么需要异步处理器？
    /// 上位机场景：
    /// - 数据保存到数据库(IO密集)
    /// - 发送网络请求(网络延迟)
    /// - 复杂数据计算(CPU密集)
    /// </summary>
    public interface IAsyncMessageHandler : IMessageHandler
    {
        Task HandleAsync(object message);
    }

    /// <summary>
    /// 同步消息处理器实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ActionMessageHandler<T> : IMessageHandler where T : class
    {
        public Action<T> Handler { get; }
        public MessagePriority Priority { get; }

        public ActionMessageHandler(Action<T> handler, MessagePriority priority)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }

        public void Handle(object message)
        {
            if (message is T typedMessage)
            {
                Handler(typedMessage);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ActionMessageHandler<T> other && ReferenceEquals(Handler, other.Handler);
        }

        public override int GetHashCode()
        {
            return Handler?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// 异步消息处理器实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class AsyncMessageHandler<T> : IAsyncMessageHandler where T : class
    {
        public Func<T, Task> Handler { get; }
        public MessagePriority Priority { get; }
        public AsyncMessageHandler(Func<T, Task> handler, MessagePriority priority)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }
        public void Handle(object message)
        {
            if (message is T typedMessage)
            {
                // 启动异步任务但不等待(Fire and Forget)
                _= HandleAsync(typedMessage);
            }
        }
        public async Task HandleAsync(object message)
        {
            if (message is T typedMessage)
            {
                await Handler(typedMessage);
            }
        }
        public override bool Equals(object obj)
        {
            return obj is AsyncMessageHandler<T> other && ReferenceEquals(Handler, other.Handler);
        }
        public override int GetHashCode()
        {
            return Handler?.GetHashCode() ?? 0;
        }
    }
}
