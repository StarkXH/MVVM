using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupervisorApp.Core.Devices
{
    /// <summary>
    /// 设备扩展方法
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 安全获取字典值，如果键不存在则返回默认值
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary == null) return defaultValue;

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 安全获取字典值
        /// </summary>
        public static T GetValueOrDefault<T>(this Dictionary<string, object> dictionary, string key, T defaultValue = default)
        {
            if (dictionary?.TryGetValue(key, out var value) == true && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        
    }
}
