using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupervisorApp.Core.Devices
{
    /// <summary>
    /// 硬件通信设备基础接口 - 专注于I2C、SPI、SMBUS等底层协议
    /// 时间：2025-08-05
    /// 作者：StarkXH
    /// 阶段：阶段二-硬件设备管理系统
    /// </summary>
    public interface IDevice : IDisposable
    {
        #region 基本属性

        /// <summary>
        /// 设备唯一标识
        /// </summary>
        string DeviceID { get; }

        /// <summary>
        /// 设备名称
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// 通信协议类型
        /// </summary>
        CommunicationProtocol Protocol { get; }

        /// <summary>
        /// 设备地址(I2C地址、SPI片选等)
        /// </summary>
        uint DeviceAddress { get; }

        /// <summary>
        /// 设备型号
        /// </summary>
        string DeviceModel { get; }
        
        /// <summary>
        /// 固件版本
        /// </summary>
        string FirmwareVersion { get; }

        /// <summary>
        /// 连接状态
        /// </summary>
        DeviceConnectionState ConnectionState { get; }

        /// <summary>
        /// 最后一次通信时间
        /// </summary>
        DateTime? LastCommunicationTime { get; }

        /// <summary>
        /// 通信统计信息
        /// </summary>
        CommunicationStatistics Statistics { get; }

        #endregion

        #region 连接管理

        /// <summary>
        /// 初始化连接设备
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化是否成功</returns>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放连接设备
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放是否成功</returns>
        Task<bool> ReleaseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检测设备是否响应
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>设备是否响应</returns>
        Task<bool> ProbeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 重置设备
        /// </summary>
        /// <param name="resetType">重置类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重置是否成功</returns>
        Task<bool> ResetAsync(DeviceResetType resetType = DeviceResetType.Soft, CancellationToken cancellationToken = default);

        #endregion

        #region 数据通信

        /// <summary>
        /// 读取寄存器数据
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取结果</returns>
        Task<DeviceDataResult> ReadRegisterAsync(uint registerAddress, int length, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写入寄存器数据
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="data">要写入的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入是否成功</returns>
        Task<bool> WriteRegisterAsync(uint registerAddress, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取单个字节
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取的字节值</returns>
        Task<byte?> ReadByteAsync(uint registerAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写入单个字节
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> WriteByteAsync(uint registerAddress, byte value, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取16位数据
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="byteOrder">字节序</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取的16位值</returns>
        Task<ushort?> ReadUInt16Async(uint registerAddress, ByteOrder byteOrder = ByteOrder.LittleEndian, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写入16位数据
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="byteOrder">字节序</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入是否成功</returns>
        Task<bool> WriteUInt16Async(uint registerAddress, ushort value, ByteOrder byteOrder = ByteOrder.LittleEndian, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量传输数据(SPI模式)
        /// </summary>
        /// <param name="writeData">要发送的数据</param>
        /// <param name="readLength">要读取的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>传输结果</returns>
        Task<DeviceDataResult> TransferAsync(byte[] writeData, int readLength, CancellationToken cancellationToken = default);

        #endregion

        #region 配置管理

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>设备信息</returns>
        Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取通信配置
        /// </summary>
        /// <returns>通信配置</returns>
        CommunicationConfig GetCommunicationConfig();

        /// <summary>
        /// 设置通信配置k
        /// </summary>
        /// <param name="config">通信配置</param>
        /// <returns>设置是否成功</returns>
        Task<bool> SetCommunicationConfigAsync(CommunicationConfig config);

        /// <summary>
        /// 获取寄存器映射列表
        /// </summary>
        /// <returns>寄存器映射表</returns>
        IEnumerable<RegisterMap> GetRegisterMaps();

        #endregion

        #region 事件

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<DeviceConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        event EventHandler<DeviceDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 通信错误事件
        /// </summary>
        event EventHandler<CommunicationErrorEventArgs> CommunicationError;

        /// <summary>
        /// 设备警告事件
        /// </summary>
        event EventHandler<DeviceWarningEventArgs> WarningOccurred;

        /// <summary>
        /// 寄存器值更改事件
        /// </summary>
        event EventHandler<RegisterValueChangedEventArgs> RegisterValueChanged;

        #endregion
    }

    /// <summary>
    /// 设备类型枚举 - 专注于硬件传感器和控制器
    /// </summary>
    public enum DeviceType
    {
        Unknown = 0,

        // 传感器类型
        TemperatureSensor = 10,    // 温度传感器
        HumiditySensor = 11,       // 湿度传感器
        PressureSensor = 12,       // 压力传感器
        AccelerometerSensor = 13,  // 加速度传感器
        GyroscopeSensor = 14,      // 陀螺仪传感器
        MagnetometerSensor = 15,   // 磁力计传感器
        LightSensor = 16,          // 光线传感器
        ProximitySensor = 17,      // 接近传感器
        GasSensor = 18,            // 气体传感器
        FlowSensor = 19,           // 流量传感器

        // 控制器类型
        PWMController = 30,        // PWM控制器
        ADCController = 31,        // ADC控制器
        DACController = 32,        // DAC控制器
        GPIOController = 33,       // GPIO控制器
        MotorController = 34,      // 电机控制器
        ServoController = 35,      // 舵机控制器
        RelayController = 36,      // 继电器控制器

        // 存储器类型
        EEPROM = 50,               // EEPROM存储器
        FlashMemory = 51,          // Flash存储器
        FRAM = 52,                 // FRAM存储器

        // 显示器类型
        LCDDisplay = 70,           // LCD显示器
        OLEDDisplay = 71,          // OLED显示器
        LEDMatrix = 72,            // LED矩阵

        // 通信芯片
        UARTBridge = 90,           // UART桥接芯片
        CANController = 91,        // CAN控制器
        EthernetController = 92,   // 以太网控制器

        CustomDevice = 99          // 自定义设备
    }

    /// <summary>
    /// 通信协议类型
    /// </summary>
    public enum CommunicationProtocol
    {
        I2C = 1,        // I2C总线
        SPI = 2,        // SPI总线
        SMBUS = 3,      // SMBus协议
        OneWire = 4,    // 单总线协议
        UART = 5,       // UART串行通信
        Custom = 99     // 自定义协议
    }

    /// <summary>
    /// 字节序枚举
    /// </summary>
    public enum ByteOrder
    {
        LittleEndian = 0,   // 小端序
        BigEndian = 1       // 大端序
    }

    /// <summary>
    /// 设备重置类型
    /// </summary>
    public enum DeviceResetType
    {
        Soft = 0,       // 软重置
        Hard = 1,       // 硬重置
        Factory = 2     // 恢复出厂设置
    }

    /// <summary>
    /// 设备连接状态枚举
    /// </summary>
    public enum DeviceConnectionState
    {
        Uninitialized = 0,     // 未初始化
        Initializing = 1,      // 初始化中
        Ready = 2,             // 就绪
        Busy = 3,              // 忙碌
        Error = 4,             // 错误
        NotResponding = 5,     // 无响应
        Resetting = 6          // 重置中
    }
}
