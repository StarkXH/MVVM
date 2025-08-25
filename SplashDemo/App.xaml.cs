using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SplashDemo
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 创建并显示启动画面，而不是主窗口
            var splashWindow = new SplashWindow();
            splashWindow.Show();

            // 不要显示主窗口，让启动画面来处理
            // MainWindow = new MainWindow();
            // MainWindow.Show();
        }
    }
}
