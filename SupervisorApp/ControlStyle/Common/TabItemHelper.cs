using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ControlStyle
{
    /// <summary>
    /// 为TabItem提供附加属性，支持图标显示和自定义
    /// </summary>
    public static class TabItemHelper
    {
        #region ImageSource附加属性

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.RegisterAttached(
                "ImageSource",
                typeof(ImageSource),
                typeof(TabItemHelper),
                new PropertyMetadata(null));

        public static void SetImageSource(UIElement element, ImageSource value)
        {
            element.SetValue(ImageSourceProperty, value);
        }

        public static ImageSource GetImageSource(UIElement element)
        {
            return (ImageSource)element.GetValue(ImageSourceProperty);
        }

        #endregion

        #region ImageWidth附加属性

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.RegisterAttached(
                "ImageWidth",
                typeof(double),
                typeof(TabItemHelper),
                new PropertyMetadata(16.0));

        public static void SetImageWidth(UIElement element, double value)
        {
            element.SetValue(ImageWidthProperty, value);
        }

        public static double GetImageWidth(UIElement element)
        {
            return (double)element.GetValue(ImageWidthProperty);
        }

        #endregion

        #region ImageHeight附加属性

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.RegisterAttached(
                "ImageHeight",
                typeof(double),
                typeof(TabItemHelper),
                new PropertyMetadata(16.0));

        public static void SetImageHeight(UIElement element, double value)
        {
            element.SetValue(ImageHeightProperty, value);
        }

        public static double GetImageHeight(UIElement element)
        {
            return (double)element.GetValue(ImageHeightProperty);
        }

        #endregion

        #region ImageMargin附加属性

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.RegisterAttached(
                "ImageMargin",
                typeof(Thickness),
                typeof(TabItemHelper),
                new PropertyMetadata(new Thickness(5, 0, 3, 0)));

        public static void SetImageMargin(UIElement element, Thickness value)
        {
            element.SetValue(ImageMarginProperty, value);
        }

        public static Thickness GetImageMargin(UIElement element)
        {
            return (Thickness)element.GetValue(ImageMarginProperty);
        }

        #endregion
    }
}