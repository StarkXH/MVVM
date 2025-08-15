using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.Models;
using System;
using System.Collections.Generic;

namespace SupervisorApp.Factories
{
    /// <summary>
    /// 设备工厂 - 统一创建和管理设备实例
    /// 解决重复的设备创建逻辑
    /// </summary>
    public static class DeviceFactory
    {
        private static readonly Dictionary<string, Func<IDevice>> _deviceFactories = new Dictionary<string, Func<IDevice>>();

        static DeviceFactory()
        {
            // 注册内置设备类型
            RegisterDeviceType("TestDevice100", () => new TestDevice100());
            
            // 🔧 为演示添加更多设备类型
            RegisterDeviceType("TestDevice200", () => CreateTestDevice200());
            RegisterDeviceType("RealDevice", () => CreateRealDevice());
            
            LogService.Instance.LogInfo("🏭 DeviceFactory initialized with supported device types");
        }

        /// <summary>
        /// 注册设备工厂方法
        /// </summary>
        private static void RegisterDeviceFactories()
        {
            _deviceFactories["TestDevice100"] = () => new TestDevice100();
            
            // 可以在这里添加更多设备类型
            // _deviceFactories["RealDevice"] = () => new RealDevice();
            // _deviceFactories["ModbusDevice"] = () => new ModbusDevice();
        }

        /// <summary>
        /// 创建指定类型的设备
        /// </summary>
        /// <param name="deviceType">设备类型名称</param>
        /// <returns>设备实例</returns>
        public static IDevice CreateDevice(string deviceType)
        {
            if (string.IsNullOrEmpty(deviceType))
            {
                throw new ArgumentException("Device type cannot be null or empty", nameof(deviceType));
            }

            if (_deviceFactories.TryGetValue(deviceType, out var factory))
            {
                var device = factory();
                LogService.Instance.LogInfo($"✅ Created device: {deviceType}");
                return device;
            }

            throw new NotSupportedException($"Device type '{deviceType}' is not supported");
        }

        /// <summary>
        /// 创建默认的测试设备
        /// </summary>
        /// <returns>测试设备实例</returns>
        public static IDevice CreateDefaultTestDevice()
        {
            return CreateDevice("TestDevice100");
        }

        /// <summary>
        /// 获取所有支持的设备类型
        /// </summary>
        /// <returns>设备类型列表</returns>
        public static IEnumerable<string> GetSupportedDeviceTypes()
        {
            return _deviceFactories.Keys;
        }

        /// <summary>
        /// 注册新的设备类型
        /// </summary>
        /// <param name="deviceType">设备类型名称</param>
        /// <param name="factory">设备创建工厂方法</param>
        public static void RegisterDeviceType(string deviceType, Func<IDevice> factory)
        {
            if (string.IsNullOrEmpty(deviceType))
            {
                throw new ArgumentException("Device type cannot be null or empty", nameof(deviceType));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _deviceFactories[deviceType] = factory;
            LogService.Instance.LogInfo($"📝 Registered device type: {deviceType}");
        }

        /// <summary>
        /// 检查设备类型是否支持
        /// </summary>
        /// <param name="deviceType">设备类型名称</param>
        /// <returns>是否支持</returns>
        public static bool IsDeviceTypeSupported(string deviceType)
        {
            return !string.IsNullOrEmpty(deviceType) && _deviceFactories.ContainsKey(deviceType);
        }

        #region 示例设备创建方法

        /// <summary>
        /// 创建TestDevice200示例（暂未实现）
        /// </summary>
        private static IDevice CreateTestDevice200()
        {
            // 这里可以创建一个更高级的测试设备
            // 暂时返回TestDevice100的实例作为占位符
            var device = new TestDevice100();
            device.GetType().GetField("_deviceName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(device, "TestDevice200");
            return device;
        }

        /// <summary>
        /// 创建真实设备示例（暂未实现）
        /// </summary>
        private static IDevice CreateRealDevice()
        {
            // 这里可以创建真实硬件设备的实例
            // 暂时抛出异常表示未实现
            throw new NotImplementedException("Real device implementation is not available yet");
        }

        #endregion
    }
}