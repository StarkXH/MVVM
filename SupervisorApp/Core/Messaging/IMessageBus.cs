using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupervisorApp.Core.Messaging
{
    /// <summary>
    /// 消息总线接口
    /// </summary>
    public interface IMessageBus
    {
        // 同步订阅
        void Subscribe<T>(Action<T> handler, MessagePriority priority = MessagePriority.Normal) where T : class;

        // 异步订阅
        void SubscribeAsync<T>(Func<T, Task> handler, MessagePriority priority = MessagePriority.Normal) where T : class;

        // 命名消息订阅
        void Subscribe<T>(string messageName, Action<T> handler, MessagePriority priority = MessagePriority.Normal) where T : class;

        // 取消订阅
        void Unsubscribe<T>(Action<T> handler) where T : class;

        // 同步发布
        void Publish<T>(T message) where T : class;

        // 命名消息发布
        void Publish<T>(string messageName, T message) where T : class;

        // 异步发布
        Task PublishAsync<T>(T message) where T : class;

        // 命名异步发布
        Task PublishAsync<T>(string messageName, T message) where T : class;

        // 清理
        void Clear();
    }

    /// <summary>
    /// 消息优先级枚举
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3,
    }
}
