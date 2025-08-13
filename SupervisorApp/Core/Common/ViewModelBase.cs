using System;
using System.Threading.Tasks;

/* 传统 MVVM 开发中常见的问题：
     * 1. 每个异步操作都要手动处理
     * private async void LoadData()
       {
           IsBusy = true;  // 手动设置
           try
           {
               var data = await _service.GetDataAsync();
               // 处理数据...
           }
           catch (Exception ex)
           {
               // 每个地方都要重复错误处理
               MessageBox.Show(ex.Message);
           }
           finally
           {
               IsBusy = false;  // 手动重置
           }
       }
     * 2. 错误处理代码重复
     * 3. 没有统一的状态管理
     * 4. 缺乏生命周期管理
     * **/

namespace SupervisorApp.Core.Common
{

    public abstract class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase
    {
        #region 私有字段

        private bool _isBusy = false;
        private string _busyMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;

        #endregion

        #region 公共属性

        /// <summary>
        /// 为什么需要 IsBusy ?
        /// 1. 防止用户在操作进行中重复点击
        /// 2. 给用户明确的反馈 (加载动画、禁用按钮)
        /// 3. 统一管理所有异步操作的状态
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            protected set => Set(ref _isBusy, value); // protected: 只有子类可以设置
        }

        /// <summary>
        /// 为什么需要 BusyMessage ?
        /// 1. 给用户具体的操作反馈
        /// 2. 不同操作显示不同消息 ("正在连接设备..."、"正在保存数据...")
        /// 3. 提升用户体验
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            protected set => Set(ref _busyMessage, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            protected set
            {
                Set(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            private set => Set(ref _hasError, value);
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public virtual string DisplayName { get; protected set; } = "";

        #endregion

        #region 异步操作支持 - 解决重复的异步代码模块

        /// <summary>
        /// 为什么需要 ExecuteAsync ?
        /// 
        /// 传统异步操作的问题：
        /// 1. 每个异步方法都要手动设置 IsBusy
        /// 2. 错误处理代码重复
        /// 3. 容易忘记在 finally 中重置状态
        /// 4. 没有统一的异常处理策略
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="busyMessage"></param>
        /// <returns></returns>
        protected async Task ExecuteAsync(Func<Task> operation, string busyMessage = "正在处理...")
        {
            // 防止重复执行
            if (IsBusy) return;

            try
            {
                // 1. 自动设置忙状态
                SetBusyState(true, busyMessage);

                // 2. 清除之前的错误
                ClearError();

                // 3. 执行实际操作
                await operation();
            }
            catch (Exception ex)
            {
                // 4. 统一错误处理
                HandleError(ex);
            }
            finally
            {
                // 5. 确保状态重置 (即使发生异常)
                SetBusyState(false);
            }
        }

        protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string busyMessage = "正在处理...")
        {
            if (IsBusy) return default(T);
            try
            {
                SetBusyState(true, busyMessage);
                ClearError();
                return await operation();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return default;
            }
            finally
            {
                SetBusyState(false);
            }
        }

        #endregion

        #region 错误处理 - 统一的错误处理机制

        /// <summary>
        /// 为什么需要统一错误处理？
        /// 
        /// 问题场景：
        /// - 串口通信失败
        /// - 网络连接超时
        /// - 数据解析错误
        /// - 文件读写失败
        /// 
        /// 传统做法： 每个地方都写 try-catch， 代码重复且不一致
        /// </summary>
        /// <param name="exception"></param>
        protected virtual void HandleError(Exception exception)
        {
            // 1. 转换为用户友好的消息
            var errorMsg = GetUserFriendlyErrorMessage(exception);
            ErrorMessage = errorMsg;

            // 2. 记录详细错误到日志 (开发调试用)
            LogError(exception);

            // 3. 发送全局错误事件 (可被错误处理服务捕获)
            MessengerInstance?.Send(new ErrorOccurredMessage(exception, this));
        }

        /// <summary>
        /// 用户友好错误消息转换
        /// 为什么重要？技术错误信心用户看不懂
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected virtual string GetUserFriendlyErrorMessage(Exception exception)
        {
            if (exception is UnauthorizedAccessException) return "没有权限执行次操作";
            if (exception is TimeoutException) return "操作超时，请稍后重试";
            if (exception is System.IO.IOException) return "文件操作失败，请检查文件路径或权限";
            if (exception is System.Net.NetworkInformation.PingException) return "网络连接失败，请检查网络设置";
            if (exception is ArgumentException) return "参数错误";
            if (exception is InvalidOperationException) return "当前状态下无法执行此操作";
            else return $"操作失败: {exception.Message}";
        }

        /// <summary>
        /// 记录错误到日志
        /// </summary>
        /// <param name="exception"></param>
        protected virtual void LogError(Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in {GetType().Name}: {exception}");
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        /// <param name="isBusy"></param>
        /// <param name="message"></param>
        private void SetBusyState(bool isBusy, string message = "")
        {
            IsBusy = isBusy;
            BusyMessage = isBusy ? message : string.Empty;
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 为什么要生命周期管理？
        /// 
        /// 上位机应用中的典型场景:
        /// 1. 页面加载时需要连接设备
        /// 2. 页面切换时需要停止数据采集
        /// 3. 应用关闭时需要释放串口资源
        /// 4. 内存泄露预防
        /// </summary>

        /// <summary>
        /// 视图加载时调用 - 替代构造函数进行异步初始化
        /// </summary>
        public virtual async Task OnLoadedAsync()
        {
            // 子类重写此方法进行初始化
            // 例如: 连接设备、启动数据采集、加载配置等
            await Task.CompletedTask;
        }

        public virtual void OnUnloaded()
        {
            // 子类可以重写此方法进行清理
            // 例如: 断开设备连接、停止定时器、保存状态等
        }

        /// <summary>
        /// 最终处理 - 防止内存泄露
        /// </summary>
        public override void Cleanup()
        {
            OnUnloaded();
            base.Cleanup(); // 调用 MVVMLight 的清理
        }

        #endregion
    }

    /// <summary>
    /// 错误发生消息
    /// </summary>
    public class ErrorOccurredMessage
    {
        public Exception Exception { get; }
        public ViewModelBase Source { get; }
        public DateTime Timestamp { get; }

        public ErrorOccurredMessage(Exception exception, ViewModelBase source)
        {
            Exception = exception;
            Source = source;
            Timestamp = DateTime.Now;
        }
    }
}
