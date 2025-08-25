using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SplashScreenApp
{
    public partial class SplashScreen : Window
    {
        private DispatcherTimer animationTimer;

        public SplashScreen()
        {
            InitializeComponent();

            // 启动窗口时开始动画
            Loaded += SplashScreen_Loaded;
        }

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // 启动淡入动画
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1));
            BeginAnimation(OpacityProperty, fadeIn);

            // 启动发光动画
            StartGlowAnimation();

            // 启动流动进度指示器动画
            StartProgressAnimation();
        }

        private void StartGlowAnimation()
        {
            Storyboard glowAnimation = (Storyboard)FindResource("GlowAnimation");
            glowAnimation.Begin(glowBorder);
        }

        private void StartProgressAnimation()
        {
            DoubleAnimation progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            ProgressIndicator.BeginAnimation(WidthProperty, progressAnimation);
        }

        public void UpdateProgress(int value)
        {
            // 更新进度条
            ProgressBar.Width = (value / 100.0) * 740; // 740是进度条容器的宽度

            // 更新状态文本
            if (value < 20)
                ProgressText.Text = "Initializing...";
            else if (value < 40)
                ProgressText.Text = "Loading modules...";
            else if (value < 60)
                ProgressText.Text = "Processing data...";
            else if (value < 80)
                ProgressText.Text = "Finalizing...";
            else
                ProgressText.Text = "Almost ready...";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}