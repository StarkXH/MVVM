using System;
using System.Windows;

namespace SupervisorApp
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 全局异常处理
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 可以在这里添加应用程序初始化代码
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // 应用程序初始化逻辑
            Console.WriteLine("SupervisorApp Starting...");
            Console.WriteLine($"Starting Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Application Error: {e.Exception.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);

            // 标记异常已处理，防止应用程序崩溃
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show($"Unresolved exception: {exception?.Message}", "Serious Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("SupervisorApp is closing...");
            base.OnExit(e);
        }
    }
}