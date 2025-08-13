using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace SupervisorApp.Converters
{
    /// <summary>
    /// 布尔值到颜色转换器
    /// 时间：2025-08-06 07:37:35 UTC
    /// 作者：StarkXH
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Colors.Green : Colors.Red;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到连接状态文本转换器
    /// </summary>
    public class BoolToConnectionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Connected" : "Disconnected";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UintToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint uintValue)
            {
                return uintValue != 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return (uint)(boolValue ? 1 : 0);
            }
            return (uint)0;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class ByteToUlongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte byteValue)
            {
                return (ulong)byteValue;
            }
            if (value is uint uintValue)
            {
                return (ulong)uintValue;
            }
            return 0UL;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ulong ulongValue)
            {
                if (targetType == typeof(byte))
                {
                    return (byte)Math.Min(ulongValue, 255);
                }
                if (targetType == typeof(uint))
                {
                    return (uint)ulongValue;
                }
            }
            return (byte)0;
        }
    }

    public class BitFieldMappingsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 2 &&
                values[0] is Dictionary<int, string> mappings &&
                values[1] is uint maxValue)
            {
                var result = new List<KeyValuePair<uint, string>>();

                if (mappings?.Any() == true)
                {
                    // 使用预定义的映射
                    foreach (var mapping in mappings)
                    {
                        result.Add(new KeyValuePair<uint, string>((uint)mapping.Key, mapping.Value));
                    }
                }
                else
                {
                    // 生成所有可能的值
                    for (uint i = 0; i <= maxValue; i++)
                    {
                        result.Add(new KeyValuePair<uint, string>(i, $"值: {i}"));
                    }
                }

                return result;
            }

            return new List<KeyValuePair<uint, string>>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}