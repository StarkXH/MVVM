using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SplashDemo
{
    /// <summary>
    /// SplashWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SplashWindow : Window
    {
        private MainWindow _mainWindow;
        private bool _isMainWindowLoaded = false;
        private bool _isClosingInitiated = false;
        private readonly int _minimumDisplayTime = 2000; // 最小显示时间2秒
        private DateTime _startTime;

        public SplashWindow()
        {
            InitializeComponent();
            _startTime = DateTime.Now;
            this.Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置进度条为确定模式
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 0;

                await LoadMainWindowWithProgressAsync();
            }
            catch (Exception ex)
            {
                await HandleLoadingError(ex);
            }
        }

        private async Task LoadMainWindowWithProgressAsync()
        {
            var progress = new Progress<(int percentage, string status)>(report =>
            {
                // 确保UI更新在UI线程上执行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBar.Value = report.percentage;
                    StatusText.Text = report.status;
                }));
            });

            await LoadMainWindowAsync(progress);

            // 确保最小显示时间
            await EnsureMinimumDisplayTime();

            // 显示主窗口并关闭启动画面
            await CloseWithAnimation();
        }

        private async Task LoadMainWindowAsync(IProgress<(int, string)> progress)
        {
            var loadingTasks = new List<(Func<Task> task, int percentage, string status)>
            {
                (LoadConfiguration, 15, "正在加载配置文件..."),
                (ConnectToServices, 35, "正在连接数据服务..."),
                (LoadUserData, 55, "正在加载用户数据..."),
                (InitializeUIComponents, 75, "正在初始化界面组件..."),
                (FinalizeInitialization, 95, "正在完成初始化...")
            };

            foreach (var (task, percentage, status) in loadingTasks)
            {
                progress?.Report((percentage, status));
                await task();

                // 添加适当的延迟以显示进度
                await Task.Delay(200);
            }

            progress?.Report((100, "加载完成"));
            _isMainWindowLoaded = true;
            await Task.Delay(300);
        }

        private async Task LoadConfiguration()
        {
            // 模拟配置加载
            await Task.Delay(300);
            // 这里可以添加实际的配置加载逻辑
        }

        private async Task ConnectToServices()
        {
            // 模拟服务连接
            await Task.Delay(500);
            // 这里可以添加数据库连接、API连接等逻辑
        }

        private async Task LoadUserData()
        {
            // 模拟用户数据加载
            await Task.Delay(400);
            // 这里可以添加用户数据、设置等加载逻辑
        }

        private async Task InitializeUIComponents()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    _mainWindow = new MainWindow();
                    // 预加载主窗口的资源
                    _mainWindow.Loaded += (s, e) => { }; // 确保窗口完全加载
                });
            });
        }

        private async Task FinalizeInitialization()
        {
            // 最终初始化步骤
            await Task.Delay(200);
            // 这里可以添加最后的初始化逻辑
        }

        private async Task EnsureMinimumDisplayTime()
        {
            var elapsedTime = (DateTime.Now - _startTime).TotalMilliseconds;
            var remainingTime = _minimumDisplayTime - elapsedTime;

            if (remainingTime > 0)
            {
                await Task.Delay((int)remainingTime);
            }
        }

        private async Task CloseWithAnimation()
        {
            if (_isClosingInitiated) return;
            _isClosingInitiated = true;

            if (_mainWindow != null && _isMainWindowLoaded)
            {
                // 启动淡出动画
                var fadeOutStoryboard = FindResource("FadeOutAnimation") as Storyboard;
                if (fadeOutStoryboard != null)
                {
                    fadeOutStoryboard.Completed += (s, e) =>
                    {
                        _mainWindow.Show();
                        this.Close();
                    };
                    fadeOutStoryboard.Begin(this);
                }
                else
                {
                    // 如果动画不可用，直接关闭
                    _mainWindow.Show();
                    this.Close();
                }
            }
        }

        private async Task HandleLoadingError(Exception ex)
        {
            var errorMessage = $"应用程序启动时发生错误:\n\n{ex.Message}";

            // 更新状态显示错误
            StatusText.Text = "启动失败";
            ProgressBar.Foreground = Brushes.Red;

            await Task.Delay(1000); // 显示错误状态1秒

            var result = MessageBox.Show(
                $"{errorMessage}\n\n是否重试启动？",
                "启动错误",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                // 重置状态并重试
                ResetToInitialState();
                await LoadMainWindowWithProgressAsync();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void ResetToInitialState()
        {
            ProgressBar.Value = 0;
            ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(0, 124, 204));
            StatusText.Text = "正在初始化...";
            _isMainWindowLoaded = false;
            _isClosingInitiated = false;
            _startTime = DateTime.Now;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 防止用户意外关闭启动画面
            if (!_isMainWindowLoaded && !_isClosingInitiated)
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }

        // 添加键盘支持（Esc键取消启动）
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var result = MessageBox.Show(
                    "确定要取消启动并退出应用程序吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
            base.OnKeyDown(e);
        }
    }
}
