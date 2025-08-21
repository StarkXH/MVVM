using SupervisorApp.Core.Common;
using SupervisorApp.Factories;
using SupervisorApp.Helpers;
using SupervisorApp.ViewModels;
using System;
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

            // 自动加载测试设备
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 🟢 不再自动加载测试设备，因为设备已经在App启动时选择
            // 如果ViewModel中还没有设备，说明设备选择流程有问题
            if (RegisterMapView?.DataContext is RegisterMapViewModel viewModel)
            {
                if (viewModel.CurrentDevice == null)
                {
                    LogService.Instance.LogWarning("⚠️ No device assigned to ViewModel, loading default test device");
                    SafeOperationExecutor.ExecuteSafelyQuiet(
                        LoadTestDeviceToRegisterMap, 
                        "Fallback: Auto-load test device");
                }
                else
                {
                    LogService.Instance.LogInfo($"✅ Device already assigned: {viewModel.CurrentDevice.DeviceName}");
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 在关闭窗口时清理资源
            LogService.Instance.LogInfo("🚪 MainWindow is closing, cleaning up resources...");
            
            try
            {
                // 清理RegisterMapView的ViewModel资源
                if (RegisterMapView?.DataContext is RegisterMapViewModel viewModel)
                {
                    LogService.Instance.LogInfo("🧹 Cleaning up RegisterMapViewModel...");
                    viewModel.Cleanup();
                }
                
                LogService.Instance.LogInfo("✅ Resource cleanup completed");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during resource cleanup: {ex.Message}");
            }
        }

        private void LoadTestDeviceToRegisterMap()
        {
            // 获取RegisterMapView的ViewModel
            if (RegisterMapView?.DataContext is RegisterMapViewModel viewModel)
            {
                // 🟢 检查是否已经有设备和是否已连接
                if (viewModel.CurrentDevice == null || !viewModel.IsConnected)
                {
                    // 🟢 使用DeviceFactory创建设备
                    if (viewModel.CurrentDevice == null)
                    {
                        viewModel.CurrentDevice = DeviceFactory.CreateDefaultTestDevice();
                        LogService.Instance.LogInfo("Test device created and assigned to ViewModel");
                    }
                }
                else
                {
                    LogService.Instance.LogInfo("Device already loaded and connected, skipping initialization");
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadTestDevice_Click(object sender, RoutedEventArgs e)
        {
            // 🟢 使用SafeOperationExecutor简化异常处理和消息显示
            SafeOperationExecutor.ExecuteSafely(
                LoadTestDeviceToRegisterMap,
                "Load test device",
                showSuccessMessage: true,
                successMessage: "Test Device loaded successfully!");
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            SafeOperationExecutor.ExecuteSafelyQuiet(() =>
            {
                LogService.Instance.Clear();
                LogService.Instance.LogInfo("Log cleared by user");
            }, "Clear log");
        }

        private void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            SafeOperationExecutor.ExecuteSafelyQuiet(() =>
            {
                if (!string.IsNullOrEmpty(LogService.Instance.LogText))
                {
                    Clipboard.SetText(LogService.Instance.LogText);
                    LogService.Instance.LogInfo("Log content copied to clipboard");
                }
            }, "Copy log to clipboard");
        }
    }
}