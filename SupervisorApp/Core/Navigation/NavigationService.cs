using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SupervisorApp.Core.Common;

/* 传统直接跳转的问题
 * 1. 直接创建窗口，紧耦合
 *      var settingsWindow = new SettingsWindow();
        settingsWindow.Show();
 * 2. 无法传递参数
 * 3. 无法管理窗口生命周期
 * 4. 无法记录访问历史
 * 5. 内存泄露风险
 * 6. 无法统一管理页面状态
 * 
 * 问题：每次都要重复这些逻辑
        var deviceWindow = new DeviceDetailsWindow();
        deviceWindow.DataContext = new DeviceDetailsViewModel(_selectedDevice);
        deviceWindow.Owner = this;
        deviceWindow.ShowDialog();
 * 
 * 使用导航服务的现代方式
 * 简洁、统一的导航方式
        await _navigationService.NavigateAsync("Settings");
 * 支持参数传递
        await _navigationService.NavigateAsync("DeviceDetails", device);
 * **/

namespace SupervisorApp.Core.Navigation
{
    /// <summary>
    /// 导航服务实现
    /// 支持页面导航、参数传递、导航历史
    /// </summary>
    public class NavigationService : INavigationService
    {
        // 关键设计1: 导航历史栈
        private readonly Stack<NavigationEntry> _navigationHistory = new Stack<NavigationEntry>();

        // 为什么用 Stack ?
        // - 后进先出: 最后访问的页面最先返回
        // - 自然符合用户的"返回"行为预期，类似浏览器的历史记录

        // 关键设计2: 页面和视图模型注册表
        private readonly Dictionary<string, Type> _pageRegistry = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> _viewModelRegistry = new Dictionary<string, Type>();

        // 为什么需要注册表？
        // - 将字符串键映射到具体类型
        // - 支持延迟加载（用到时才创建）
        // - 便于配置和管理
        // - 支持动态替换页面实现

        private Frame _navigationFrame;
        private IServiceProvider _serviceProvider;

        // 为什么需要这些？
        // - Frame：WPF 的导航容器，负责实际的页面切换
        // - ServiceProvider：依赖注入，创建 ViewModel 实例

        public event EventHandler<NavigationEventArgs> Navigated;
        public event EventHandler<NavigatingEventArgs> Navigating;

        public bool CanGoBack => _navigationHistory.Count > 1;        // 因为栈顶是当前页面，只有当栈中有超过1个页面时才能返回
        public NavigationEntry CurrentPage => _navigationHistory.Count > 0 ? _navigationHistory.Peek() : null;

        #region 初始化

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        /// <param name="navigationFrame"></param>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Initialize(Frame navigationFrame, IServiceProvider serviceProvider)
        {
            _navigationFrame = navigationFrame ?? throw new ArgumentNullException(nameof(navigationFrame));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 注册页面
        /// </summary>
        /// <typeparam name="TPage"></typeparam>
        /// <typeparam name="TViewModel"></typeparam>
        /// <param name="pageKey"></param>
        public void RegisterPage<TPage, TViewModel>(string pageKey)
            where TPage : Page
            where TViewModel : ViewModelBase
        {
            // 注册页面类型
            _pageRegistry[pageKey] = typeof(TPage);

            // 注册对应的 ViewModel 类型
            _viewModelRegistry[pageKey] = typeof(TViewModel);

            // 这样做的好处：
            // 1. 类型安全：编译时检查 TPage 确实是 Page
            // 2. 自动关联：页面和 ViewModel 自动关联
            // 3. 延迟创建：只有导航时才创建实例
            // 4. 便于测试：可以注册 Mock 实现
        }

        /// <summary>
        /// 注册页面
        /// </summary>
        /// <typeparam name="TPage"></typeparam>
        /// <param name="pageKey"></param>
        public void RegisterPage<TPage>(string pageKey) where TPage : Page
        {
            // ✅ 适合只注册 TPage 的场景：
            // 1. 页面逻辑非常简单
            // 2. 主要是静态内容展示
            // 3. 使用第三方控件且控件有自己的数据机制
            // 4. 直接绑定到业务模型对象
            // 5. 简单的表单验证和提交
            // 6. 嵌入外部内容（Web、报表等）
            _pageRegistry[pageKey] = typeof(TPage);
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="passKey"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task<bool> NavigateAsync(string pageKey, object parameter = null)
        {
            // 步骤1: 参数验证
            if (string.IsNullOrEmpty(pageKey) || !_pageRegistry.ContainsKey(pageKey))
                return false;

            try
            {
                // 步骤2: 触发导航前事件(可以被取消)
                var navigatingArgs = new NavigatingEventArgs(pageKey, parameter);
                Navigating?.Invoke(this, navigatingArgs);

                if (navigatingArgs.Cancel)
                    return false; // 导航被取消

                // 步骤3: 创建页面实例
                var pageType = _pageRegistry[pageKey];
                var page = (Page)Activator.CreateInstance(pageType);

                // 为什么每次创建新实例？
                // - 确保页面状态干净
                // - 避免数据残留
                // - 支持同一页面的多个实例

                // 步骤4: 创建并配置 ViewModel
                if (_viewModelRegistry.ContainsKey(pageKey))
                {
                    var viewModelType = _viewModelRegistry[pageKey];
                    var viewModel = CreateViewModel(viewModelType);

                    if (viewModel != null)
                    {
                        // 设置 DataContext
                        page.DataContext = viewModel;

                        // 步骤5: 生命周期管理
                        if (parameter != null && viewModel is INavigationAware navigationAware)
                        {
                            // 传递导航参数
                            await navigationAware.OnNavigatedToAsync(parameter);
                        }
                        else if (viewModel is ViewModelBase viewModelBase)
                        {
                            // 执行初始化
                            await viewModelBase.OnLoadedAsync();
                        }
                    }
                }

                // 步骤6: 执行实际导航
                _navigationFrame.Navigate(page);

                // 步骤7: 记录导航历史
                var entry = new NavigationEntry(pageKey, parameter, page, page.DataContext as ViewModelBase);
                _navigationHistory.Push(entry);

                // 步骤8: 触发导航完成事件
                Navigated?.Invoke(this, new NavigationEventArgs(pageKey, parameter, true));

                return true;
            }
            catch (Exception ex)
            {
                // 步骤9: 处理异常
                System.Diagnostics.Debug.WriteLine($"导航失败: {ex.Message}");
                Navigated?.Invoke(this, new NavigationEventArgs(pageKey, parameter, false, ex));
                return false;
            }
        }

        /// <summary>
        /// 返回上一页
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoBackAsync()
        {
            if (!CanGoBack) return false; // 检查是否可以返回

            try
            {
                // 步骤1: 从历史栈中移除当前页面
                var currentEntry = _navigationHistory.Pop();

                // 步骤2: 清理当前页面的 ViewModel
                if (currentEntry.ViewModel is INavigationAware currentNavigationAware)
                {
                    // 同时 ViewModel 即将离开
                    await currentNavigationAware.OnNavigatedFromAsync();
                }

                // 释放资源
                currentEntry.ViewModel?.Cleanup();

                // 步骤3: 获取上一个页面
                var previousEntry = _navigationHistory.Peek();

                // 步骤4: 导航到上一页
                _navigationFrame.Navigate(previousEntry.Page);
                
                // 步骤5: 重新激活上一页的 ViewModel
                if (previousEntry.ViewModel is INavigationAware previousNavigationAware)
                {
                    // 重新激活，传入原始参数
                    await previousNavigationAware.OnNavigatedToAsync(previousEntry.Parameter);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"返回导航失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 私有方法

        private ViewModelBase CreateViewModel(Type viewModelType)
        {
            try
            {
                // 依赖注入方式
                if (_serviceProvider != null)
                {
                    return _serviceProvider.GetService(viewModelType) as ViewModelBase;
                }

                return Activator.CreateInstance(viewModelType) as ViewModelBase;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建 ViewModel 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清空导航历史
        /// </summary>
        public void ClearHistory()
        {
            while (_navigationHistory.Count > 1)
            {
                var entry = _navigationHistory.Pop();
                entry.ViewModel?.Cleanup();
            }
        }

        #endregion

        #region 清理

        public void Dispose()
        {
            ClearHistory();

            if (_navigationHistory.Count > 0)
            {
                var lastEntry = _navigationHistory.Pop();
                lastEntry.ViewModel?.Cleanup();
            }

            _pageRegistry.Clear();
            _viewModelRegistry.Clear();
        }

        #endregion

    }

    /// <summary>
    /// 导航服务接口
    /// </summary>
    public interface INavigationService : IDisposable
    {
        event EventHandler<NavigationEventArgs> Navigated;
        event EventHandler<NavigatingEventArgs> Navigating;

        bool CanGoBack { get; }
        NavigationEntry CurrentPage { get; }

        void Initialize(Frame navigationFrame, IServiceProvider serviceProvider);
        void RegisterPage<TPage, TViewModel>(string pageKey) where TPage : Page where TViewModel : ViewModelBase;
        void RegisterPage<TPage>(string pageKey) where TPage : Page;
        Task<bool> NavigateAsync(string pageKey, object parameter = null);
        Task<bool> GoBackAsync();
        void ClearHistory();
    }

    /// <summary>
    /// 导航感知接口
    /// </summary>
    public interface INavigationAware
    {
        Task OnNavigatedToAsync(object parameter);
        Task OnNavigatedFromAsync();
    }

    /// <summary>
    /// 导航条目
    /// </summary>
    public class NavigationEntry
    {
        public string PageKey { get; }          // 页面标识
        public object Parameter { get; }        // 导航参数
        public Page Page { get; }               // 页面实例
        public ViewModelBase ViewModel { get; } // ViewModel 实例
        public DateTime NavigatedAt { get; }    // 导航时间

        public NavigationEntry(string pageKey, object parameter, Page page, ViewModelBase viewModel)
        {
            PageKey = pageKey;
            Parameter = parameter;
            this.Page = page;
            ViewModel = viewModel;
            NavigatedAt =  DateTime.Now;
        }
    }

    // 导航过程的时间线：
    // 1. 用户触发导航 (NavigateAsync 被调用)
    //    ↓
    // 2. 🔔 Navigating 事件触发 (导航前) - 可以被取消
    //    ↓
    // 3. 执行实际导航逻辑 (创建页面、设置 ViewModel 等)
    //    ↓
    // 4. 🔔 Navigated 事件触发 (导航后) - 报告结果

    /// <summary>
    /// 导航后事件参数 - "我已经尝试去某个地方了，这是结果"
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        // 🎯 核心特征：
        // 1. 包含结果信息（成功/失败，异常详情）
        // 2. 没有 Cancel 属性，无法改变导航结果
        // 3. 在导航执行后触发
        // 4. 主要用于日志记录、统计和后续处理
        public string PageKey { get; }
        public object Parameter { get; }
        public bool Success { get; }        // 🔑 关键：导航是否成功
        public Exception Exception{ get; }  // 🔑 关键：失败时的异常信息

        public NavigationEventArgs(string pageKey, object parameter, bool success, Exception exception = null)
        {
            PageKey = pageKey;
            Parameter = parameter;
            Success = success;
            Exception = exception;
        }
    }

    /// <summary>
    /// 导航前事件参数 - "我即将去某个地方，你可以阻止我"
    /// </summary>
    public class NavigatingEventArgs : EventArgs
    {
        // 🎯 核心特征：
        // 1. 只有输入信息（要去哪里，带什么参数）
        // 2. 有 Cancel 属性，可以阻止导航
        // 3. 在导航执行前触发
        // 4. 主要用于验证和权限检查
        public string PageKey { get; }
        public object Parameter { get; }
        public bool Cancel { get; set; }    // 关键: 可以取消导航

        public NavigatingEventArgs(string pageKey, object parameter)
        {
            PageKey = pageKey;
            Parameter = parameter;
        }
    }
}
