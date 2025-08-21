using SupervisorApp.Core.Common;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SupervisorApp.Views
{
    /// <summary>
    /// SupervisorApp 启动画面窗口
    /// 提供专业的应用程序启动体验，包含系统初始化、进度显示和用户交互
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        #region 私有字段

        private readonly DispatcherTimer _initializationTimer;
        private int _currentProgress = 0;
        private readonly string[] _initializationSteps = {
            "Loading system configurations...",
            "Initializing device drivers...",
            "Setting up communication protocols...",
            "Loading register map definitions...",
            "Preparing user interface...",
            "Finalizing system startup..."
        };

        #endregion

        #region 公共事件

        /// <summary>
        /// 用户选择继续进入主应用程序时触发
        /// </summary>
        public event EventHandler ContinueRequested;

        /// <summary>
        /// 用户选择退出应用程序时触发
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// 用户选择打开设置时触发
        /// </summary>
        public event EventHandler SettingsRequested;

        #endregion

        #region 构造函数和初始化

        public SplashScreenWindow()
        {
            InitializeComponent();

            // 初始化定时器
            _initializationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 每500ms更新一次
            };
            _initializationTimer.Tick += OnInitializationTick;

            // 窗口事件
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;

            LogService.Instance.LogInfo("🌟 Splash screen initialized");
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("🚀 Starting application initialization sequence");

                // 启动动画
                StartAnimations();

                // 开始初始化过程
                StartInitializationProcess();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during splash screen load: {ex.Message}");
                HandleInitializationError(ex);
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 停止所有动画和定时器
                StopAnimations();
                _initializationTimer?.Stop();

                LogService.Instance.LogInfo("🚪 Splash screen closing");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during splash screen closing: {ex.Message}");
            }
        }

        #endregion

        #region 动画管理

        /// <summary>
        /// 启动所有动画效果
        /// </summary>
        private void StartAnimations()
        {
            try
            {
                // 启动旋转动画
                var rotationStoryboard = (Storyboard)FindResource("RotationAnimation");
                rotationStoryboard?.Begin();

                LogService.Instance.LogInfo("✨ Splash screen animations started");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error starting animations: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止所有动画效果
        /// </summary>
        private void StopAnimations()
        {
            try
            {
                // 停止所有Storyboard
                var rotationStoryboard = (Storyboard)FindResource("RotationAnimation");
                rotationStoryboard?.Stop();

                LogService.Instance.LogInfo("⏹️ Splash screen animations stopped");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error stopping animations: {ex.Message}");
            }
        }

        #endregion

        #region 初始化过程管理

        /// <summary>
        /// 开始系统初始化过程
        /// </summary>
        private void StartInitializationProcess()
        {
            try
            {
                _currentProgress = 0;
                UpdateInitializationStatus(_initializationSteps[0], 0);

                // 开始初始化定时器
                _initializationTimer.Start();

                LogService.Instance.LogInfo("⚙️ System initialization process started");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error starting initialization: {ex.Message}");
                HandleInitializationError(ex);
            }
        }

        /// <summary>
        /// 初始化定时器事件处理
        /// </summary>
        private void OnInitializationTick(object sender, EventArgs e)
        {
            try
            {
                _currentProgress += 20; // 每次增加20%

                if (_currentProgress < 100)
                {
                    int stepIndex = Math.Min(_currentProgress / 20, _initializationSteps.Length - 1);
                    UpdateInitializationStatus(_initializationSteps[stepIndex], _currentProgress);

                    // 模拟实际的初始化工作
                    SimulateInitializationStep(stepIndex);
                }
                else
                {
                    // 初始化完成
                    CompleteInitialization();
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during initialization tick: {ex.Message}");
                HandleInitializationError(ex);
            }
        }

        /// <summary>
        /// 模拟初始化步骤
        /// </summary>
        private void SimulateInitializationStep(int stepIndex)
        {
            switch (stepIndex)
            {
                case 0: // Loading system configurations
                    LogService.Instance.LogInfo("📋 Loading system configurations...");
                    break;
                case 1: // Initializing device drivers
                    LogService.Instance.LogInfo("🔧 Initializing device drivers...");
                    break;
                case 2: // Setting up communication protocols
                    LogService.Instance.LogInfo("🌐 Setting up communication protocols...");
                    break;
                case 3: // Loading register map definitions
                    LogService.Instance.LogInfo("📊 Loading register map definitions...");
                    break;
                case 4: // Preparing user interface
                    LogService.Instance.LogInfo("🎨 Preparing user interface...");
                    break;
                case 5: // Finalizing system startup
                    LogService.Instance.LogInfo("🚀 Finalizing system startup...");
                    break;
            }
        }

        /// <summary>
        /// 更新初始化状态显示
        /// </summary>
        private void UpdateInitializationStatus(string statusText, int progress)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusText.Text = statusText;
                LoadingProgress.Value = progress;
                ProgressText.Text = $"{progress}%";
            }));
        }

        /// <summary>
        /// 完成初始化过程
        /// </summary>
        private void CompleteInitialization()
        {
            try
            {
                _initializationTimer.Stop();

                // 更新最终状态
                UpdateInitializationStatus("System ready! Click 'Continue' to proceed.", 100);

                // 停止加载动画
                var rotationStoryboard = (Storyboard)FindResource("RotationAnimation");
                rotationStoryboard?.Stop();

                // 启用继续按钮
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ContinueButton.IsEnabled = true;
                    StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF27AE60"));
                }));

                LogService.Instance.LogInfo("✅ System initialization completed successfully");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error completing initialization: {ex.Message}");
                HandleInitializationError(ex);
            }
        }

        /// <summary>
        /// 处理初始化错误
        /// </summary>
        private void HandleInitializationError(Exception ex)
        {
            _initializationTimer?.Stop();
            StopAnimations();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusText.Text = "❌ Initialization failed. Please check the logs and try again.";
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE74C3C"));
                
                // 启用设置和退出按钮
                SettingsButton.IsEnabled = true;
                ExitButton.IsEnabled = true;
            }));

            LogService.Instance.LogError($"💥 Initialization failed: {ex.Message}");
        }

        #endregion

        #region 用户交互事件

        /// <summary>
        /// 继续按钮点击事件
        /// </summary>
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("🚀 User requested to continue to main application");
                ContinueRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling continue request: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("⚙️ User requested to open settings");
                SettingsRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling settings request: {ex.Message}");
            }
        }

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("❌ User requested to exit application");
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling exit request: {ex.Message}");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 手动设置初始化进度
        /// </summary>
        /// <param name="progress">进度百分比 (0-100)</param>
        /// <param name="statusText">状态文本</param>
        public void SetProgress(int progress, string statusText = null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadingProgress.Value = Math.Max(0, Math.Min(100, progress));
                ProgressText.Text = $"{progress}%";
                
                if (!string.IsNullOrEmpty(statusText))
                {
                    StatusText.Text = statusText;
                }
            }));
        }

        /// <summary>
        /// 启用或禁用继续按钮
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetContinueButtonEnabled(bool enabled)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ContinueButton.IsEnabled = enabled;
            }));
        }

        /// <summary>
        /// 重置初始化过程
        /// </summary>
        public void ResetInitialization()
        {
            try
            {
                _initializationTimer?.Stop();
                _currentProgress = 0;
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadingProgress.Value = 0;
                    ProgressText.Text = "0%";
                    StatusText.Text = "Resetting system...";
                    ContinueButton.IsEnabled = false;
                }));

                LogService.Instance.LogInfo("🔄 Splash screen initialization reset");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error resetting initialization: {ex.Message}");
            }
        }

        #endregion
    }
}