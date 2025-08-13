using GalaSoft.MvvmLight;
using SupervisorApp.Core.Common;
using SupervisorApp.Examples;
using SupervisorApp.Test;
using SupervisorApp.ViewModels;
using SupervisorApp.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SupervisorApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize log service
            LogService.Instance.LogInfo("Application started successfully");

            // Auto-scroll log to bottom when new content is added
            LogService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LogService.LogText))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogTextBox.ScrollToEnd();
                    }));
                }
            };

            // 自动加载测试设备
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 自动加载测试设备到RegisterMapView
                LoadTestDeviceToRegisterMap();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"Failed to auto-load test device: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 在关闭窗口时清理资源
            LogService.Instance.LogInfo("Application is closing, cleaning up resources...");
            Environment.Exit(0);
        }

        private void LoadTestDeviceToRegisterMap()
        {
            try
            {
                // 获取RegisterMapView的ViewModel
                if (RegisterMapView?.DataContext is RegisterMapViewModel viewModel)
                {
                    // 🟢 检查是否已经有设备和是否已连接
                    if (viewModel.CurrentDevice == null || !viewModel.IsConnected)
                    {
                        // 🟢 如果没有设备，创建设备但不连接
                        if (viewModel.CurrentDevice == null)
                        {
                            var testDevice = new TestDevice100();
                            viewModel.CurrentDevice = testDevice;

                            LogService.Instance.LogInfo("Test device created and assigned to ViewModel");
                        }
                    }
                    else
                    {
                        LogService.Instance.LogInfo("Device already loaded and connected, skipping initialization");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"Failed to load test device: {ex.Message}");
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadTestDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadTestDeviceToRegisterMap();
                MessageBox.Show("Test Device loaded successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test Device load failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.Clear();
            LogService.Instance.LogInfo("Log cleared by user");
        }

        private void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LogService.Instance.LogText))
            {
                Clipboard.SetText(LogService.Instance.LogText);
                LogService.Instance.LogInfo("Log content copied to clipboard");
            }
        }
    }

}
