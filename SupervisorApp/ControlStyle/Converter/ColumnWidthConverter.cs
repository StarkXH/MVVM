using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ControlStyle
{
    public class ColumnWidthConverter : IValueConverter
    {
        public double MinWidth { get; set; } = 30;
        public double AddressColumnWidth { get; set; } = 100;

        /// <summary>
        /// 转换器，根据ListView宽度动态计算列宽
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double listViewWidth)
            {
                // 获取参数中的列索引
                int columnIndex = parameter != null && int.TryParse(parameter.ToString(), out int index) ? index : -1;

                // 计算可用空间
                double availableWidth = listViewWidth - AddressColumnWidth - SystemParameters.VerticalScrollBarWidth;

                // 特殊处理地址列
                if (columnIndex == 0)
                    return AddressColumnWidth;

                // 计算数据列宽度（假设有16列数据）
                int dataColumnCount = 16;
                double columnWidth = Math.Max(MinWidth, (availableWidth / dataColumnCount));

                return columnWidth;
            }
            return MinWidth;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

