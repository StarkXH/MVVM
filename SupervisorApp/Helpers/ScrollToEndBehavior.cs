using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SupervisorApp.Helpers
{
    /// <summary>
    /// TextBox????????????
    /// ????XAML??? helpers:ScrollToEndBehavior.AutoScrollToEnd="True"
    /// </summary>
    public static class ScrollToEndBehavior
    {
        #region AutoScrollToEnd ????

        /// <summary>
        /// ??????????????
        /// </summary>
        public static readonly DependencyProperty AutoScrollToEndProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToEnd",
                typeof(bool),
                typeof(ScrollToEndBehavior),
                new PropertyMetadata(false, OnAutoScrollToEndChanged));

        /// <summary>
        /// ??AutoScrollToEnd???
        /// </summary>
        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToEndProperty);
        }

        /// <summary>
        /// ??AutoScrollToEnd???
        /// </summary>
        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

        /// <summary>
        /// AutoScrollToEnd????????
        /// </summary>
        private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    // ??????
                    textBox.TextChanged += TextBox_TextChanged;
                }
                else
                {
                    // ??????
                    textBox.TextChanged -= TextBox_TextChanged;
                }
            }
        }

        /// <summary>
        /// TextBox???????? - ???????
        /// </summary>
        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ScrollTextBoxToEnd(textBox);
            }
        }

        #endregion

        #region ??????

        /// <summary>
        /// ?TextBox?????
        /// </summary>
        /// <param name="textBox">????TextBox</param>
        public static void ScrollTextBoxToEnd(TextBox textBox)
        {
            if (textBox == null) return;

            try
            {
                // ?? ??1?????ScrollViewer??????
                var scrollViewer = FindParentScrollViewer(textBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
                    return;
                }

                // ?? ??2???TextBox???????
                textBox.CaretIndex = textBox.Text.Length;
                
                // ?? ??3???????????????Selection???
                textBox.Select(textBox.Text.Length, 0);
            }
            catch
            {
                // ?????????????????
            }
        }

        /// <summary>
        /// ?????????ScrollViewer
        /// </summary>
        /// <param name="child">???</param>
        /// <returns>???ScrollViewer????????null</returns>
        private static ScrollViewer FindParentScrollViewer(DependencyObject child)
        {
            if (child == null) return null;

            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        #endregion
    }
}