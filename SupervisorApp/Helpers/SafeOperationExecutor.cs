using SupervisorApp.Core.Common;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SupervisorApp.Helpers
{
    /// <summary>
    /// 安全操作执行器 - 统一处理异常和用户反馈
    /// 解决重复的try-catch代码问题
    /// </summary>
    public static class SafeOperationExecutor
    {
        /// <summary>
        /// 安全执行同步操作，自动处理异常和日志记录
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="showSuccessMessage">是否显示成功消息</param>
        /// <param name="successMessage">成功消息内容</param>
        /// <returns>操作是否成功</returns>
        public static bool ExecuteSafely(Action operation, string operationName, 
            bool showSuccessMessage = false, string successMessage = null)
        {
            try
            {
                operation?.Invoke();
                
                if (showSuccessMessage && !string.IsNullOrEmpty(successMessage))
                {
                    MessageBox.Show(successMessage, "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                LogService.Instance.LogInfo($"✅ {operationName} completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"{operationName} failed: {ex.Message}";
                LogService.Instance.LogError($"❌ {errorMessage}");
                
                MessageBox.Show(errorMessage, "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="showSuccessMessage">是否显示成功消息</param>
        /// <param name="successMessage">成功消息内容</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> ExecuteSafelyAsync(Func<Task> operation, string operationName,
            bool showSuccessMessage = false, string successMessage = null)
        {
            try
            {
                if (operation != null)
                {
                    await operation();
                }
                
                if (showSuccessMessage && !string.IsNullOrEmpty(successMessage))
                {
                    MessageBox.Show(successMessage, "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                LogService.Instance.LogInfo($"✅ {operationName} completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"{operationName} failed: {ex.Message}";
                LogService.Instance.LogError($"❌ {errorMessage}");
                
                MessageBox.Show(errorMessage, "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 安全执行有返回值的操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">失败时的默认返回值</param>
        /// <returns>操作结果</returns>
        public static T ExecuteSafely<T>(Func<T> operation, string operationName, T defaultValue = default)
        {
            try
            {
                var result = operation != null ? operation() : defaultValue;
                LogService.Instance.LogInfo($"✅ {operationName} completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"{operationName} failed: {ex.Message}";
                LogService.Instance.LogError($"❌ {errorMessage}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 仅记录日志，不显示消息框的安全执行
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>操作是否成功</returns>
        public static bool ExecuteSafelyQuiet(Action operation, string operationName)
        {
            try
            {
                operation?.Invoke();
                LogService.Instance.LogInfo($"✅ {operationName} completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ {operationName} failed: {ex.Message}");
                return false;
            }
        }
    }
}