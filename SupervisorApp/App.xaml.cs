using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.ViewModels;
using SupervisorApp.Views;
using System;
using System.Windows;

namespace SupervisorApp
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // 全局异常处理
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 初始化日志服务
            LogService.Instance.LogInfo("🚀 SupervisorApp starting...");

            try
            {
                // 应用程序启动流程
                StartApplicationFlow();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Application startup failed: {ex.Message}");
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// 应用程序启动流程
        /// </summary>
        private void StartApplicationFlow()
        {
            LogService.Instance.LogInfo("📋 Starting device selection process...");

            // 🔧 设置应用程序关闭模式为显式关闭，防止自动退出
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 步骤1: 显示设备选择对话框
            var selectedDevice = DeviceSelectionWindow.ShowDeviceSelectionDialog();

            if (selectedDevice == null)
            {
                // 用户取消选择或选择失败，退出应用程序
                LogService.Instance.LogInfo("❌ No device selected, exiting application");
                Shutdown();
                return;
            }

            LogService.Instance.LogInfo($"✅ Device selected: {selectedDevice.DeviceName}");

            // 步骤2: 创建并显示主窗口，传入选择的设备
            try
            {
                var mainWindow = new MainWindow();
                
                // 🔧 设置为应用程序的主窗口，并改变关闭模式
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                
                LogService.Instance.LogInfo("🪟 MainWindow created, assigning device...");
                
                // 将选择的设备传递给主窗口
                if (mainWindow.RegisterMapView?.DataContext is RegisterMapViewModel viewModel)
                {
                    LogService.Instance.LogInfo("📊 Found ViewModel via DataContext, assigning device");
                    viewModel.CurrentDevice = selectedDevice;
                    LogService.Instance.LogInfo($"🔗 Device assigned via DataContext: {selectedDevice.DeviceName}");
                }

                // 显示主窗口
                mainWindow.Show();
                LogService.Instance.LogInfo("🪟 Main window displayed successfully");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to create main window: {ex.Message}");
                selectedDevice?.Dispose(); // 清理设备资源
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var errorMessage = $"Application Error: {e.Exception.Message}";
            LogService.Instance.LogError($"❌ Unhandled exception: {errorMessage}");
            
            MessageBox.Show(errorMessage, "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);

            // 标记异常已处理，防止应用程序崩溃
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var errorMessage = $"Unhandled exception: {exception?.Message}";
            LogService.Instance.LogError($"❌ {errorMessage}");
            
            MessageBox.Show(errorMessage, "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Instance.LogInfo("🚪 SupervisorApp is closing...");
            LogService.Instance.LogInfo($"🕐 Session duration: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            base.OnExit(e);
        }
    }
}