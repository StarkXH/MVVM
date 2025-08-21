using SupervisorApp.Views;
using System;
using System.Windows;

namespace SupervisorApp.Test
{
    /// <summary>
    /// ??????????
    /// </summary>
    public static class SplashScreenTester
    {
        /// <summary>
        /// ????????
        /// </summary>
        public static void TestSplashScreen()
        {
            try
            {
                var splash = new SplashScreenWindow();
                
                // ???????????
                splash.ContinueRequested += (sender, e) =>
                {
                    MessageBox.Show("Continue button clicked!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    splash.Close();
                };

                splash.SettingsRequested += (sender, e) =>
                {
                    MessageBox.Show("Settings button clicked!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                };

                splash.ExitRequested += (sender, e) =>
                {
                    MessageBox.Show("Exit button clicked!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    splash.Close();
                };

                // ??????
                splash.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing splash screen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ??????
        /// </summary>
        public static void TestSimplified()
        {
            var window = new Window
            {
                Title = "Button Test",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // ????1
            var button1 = new System.Windows.Controls.Button
            {
                Content = "?? Test Button 1",
                Width = 200,
                Height = 40,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            button1.Click += (s, e) => MessageBox.Show("Button 1 clicked!");

            // ????2
            var button2 = new System.Windows.Controls.Button
            {
                Content = "?? Test Button 2",
                Width = 200,
                Height = 40,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            button2.Click += (s, e) => MessageBox.Show("Button 2 clicked!");

            stackPanel.Children.Add(button1);
            stackPanel.Children.Add(button2);
            window.Content = stackPanel;

            window.ShowDialog();
        }
    }
}