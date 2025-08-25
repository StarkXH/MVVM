using System.Threading;
using System.Windows;

namespace SplashScreenApp
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            // 创建并显示启动窗口
            var splashScreen = new SplashScreen();
            splashScreen.Show();

            //// 在主线程上异步加载主窗口
            //Thread thread = new Thread(new ThreadStart(() =>
            //{
            //    // 模拟加载过程
            //    for (int i = 0; i <= 100; i++)
            //    {
            //        // 更新进度
            //        splashScreen.Dispatcher.Invoke(() =>
            //        {
            //            splashScreen.UpdateProgress(i);
            //        });

            //        // 模拟工作
            //        Thread.Sleep(30);
            //    }

            //    // 加载完成，关闭启动窗口并打开主窗口
            //    splashScreen.Dispatcher.Invoke(() =>
            //    {
            //        // 创建主窗口
            //        MainWindow mainWindow = new MainWindow();

            //        // 关闭启动窗口
            //        splashScreen.Close();

            //        // 显示主窗口
            //        mainWindow.Show();
            //    });
            //}));

            //thread.SetApartmentState(ApartmentState.STA);
            //thread.IsBackground = true;
            //thread.Start();
        }
    }
}
