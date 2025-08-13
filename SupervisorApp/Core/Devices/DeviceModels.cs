using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupervisorApp.Core.Devices
{
    /// <summary>
    /// 设备相关数据模型 - 统一文件
    /// 时间：2025-08-05 08:45:51 UTC
    /// 作者：StarkXH
    /// </summary>

    #region 核心设备数据模型
    /// <summary>
    /// 设备数据读取结果
    /// </summary>
    public class DeviceDataResult
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; }
        public string ErrorMessage { get; set; }
        public CommunicationErrorType? ErrorType { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public uint RegisterAddress { get; set; }
        public bool ChecksumValid { get; set; } = true;

        public DeviceDataResult()
        {
            Timestamp = DateTime.Now;
        }

        public static DeviceDataResult CreateSuccess(byte[] data, TimeSpan responseTime, uint registerAddress = 0)
        {
            return new DeviceDataResult
            {
                Success = true,
                Data = data,
                ResponseTime = responseTime,
                RegisterAddress = registerAddress
            };
        }

        public static DeviceDataResult CreateFailure(string errorMessage, CommunicationErrorType errorType, uint registerAddress = 0)
        {
            return new DeviceDataResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType,
                RegisterAddress = registerAddress
            };
        }

        // 便利方法：获取不同数据类型
        public byte? GetByte() => Data?.Length >= 1 ? (byte?)Data[0] : null;

        public ushort? GetUInt16(ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (Data?.Length >= 2)
            {
                return byteOrder == ByteOrder.LittleEndian
                    ? (ushort)(Data[0] | (Data[1] << 8))
                    : (ushort)(Data[1] | (Data[0] << 8));
            }
            return null;
        }

        public uint? GetUInt32(ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (Data?.Length >= 4)
            {
                return byteOrder == ByteOrder.LittleEndian
                    ? (uint)(Data[0] | (Data[1] << 8) | (Data[2] << 16) | (Data[3] << 24))
                    : (uint)(Data[3] | (Data[2] << 8) | (Data[1] << 16) | (Data[0] << 24));
            }
            return null;
        }

        public float? GetFloat(ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            var uintValue = GetUInt32(byteOrder);
            return uintValue.HasValue ? (float?)BitConverter.ToSingle(BitConverter.GetBytes(uintValue.Value), 0) : null;
        }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfo
    {
        public string ManufacturerId { get; set; }     // 制造商ID
        public string DeviceId { get; set; }           // 设备ID
        public string ProductId { get; set; }          // 产品ID
        public string SerialNumber { get; set; }       // 序列号
        public string FirmwareVersion { get; set; }    // 固件版本
        public string HardwareRevision { get; set; }   // 硬件版本
        public DateTime ManufacturingDate { get; set; } // 制造日期
        public Dictionary<string, string> ExtendedInfo { get; set; } = new Dictionary<string, string>();
    }
    #endregion

    #region 通信相关模型
    /// <summary>
    /// 通信配置
    /// </summary>
    public class CommunicationConfig
    {
        public CommunicationProtocol Protocol { get; set; }
        public uint DeviceAddress { get; set; }
        public int BusSpeed { get; set; }          // 总线速度 (Hz)
        public int Timeout { get; set; }           // 超时时间 (ms)
        public int RetryCount { get; set; }        // 重试次数
        public bool UseChecksums { get; set; }     // 是否使用校验和
        public Dictionary<string, object> ProtocolSpecificSettings { get; set; } = new Dictionary<string, object>();

        // I2C特有设置
        public bool UseStretching => ProtocolSpecificSettings.GetValueOrDefault("UseStretching", false);
        public bool Use10BitAddressing => ProtocolSpecificSettings.GetValueOrDefault("Use10BitAddressing", false);

        // SPI特有设置
        public SpiMode SpiMode => (SpiMode)ProtocolSpecificSettings.GetValueOrDefault("SpiMode", SpiMode.Mode0);
        public int ChipSelectPin => (int)ProtocolSpecificSettings.GetValueOrDefault("ChipSelectPin", -1);
        public bool ChipSelectActiveHigh => ProtocolSpecificSettings.GetValueOrDefault("ChipSelectActiveHigh", false);

        // SMBus特有设置
        public bool UsePEC => ProtocolSpecificSettings.GetValueOrDefault("UsePEC", false); // Packet Error Checking
        public bool UseHostNotify => ProtocolSpecificSettings.GetValueOrDefault("UseHostNotify", false);
    }

    /// <summary>
    /// SPI模式枚举
    /// </summary>
    public enum SpiMode
    {
        Mode0 = 0,  // CPOL=0, CPHA=0
        Mode1 = 1,  // CPOL=0, CPHA=1
        Mode2 = 2,  // CPOL=1, CPHA=0
        Mode3 = 3   // CPOL=1, CPHA=1
    }

    /// <summary>
    /// 通信统计信息
    /// </summary>
    public class CommunicationStatistics
    {
        public long TotalTransactions { get; set; }        // 总事务数
        public long SuccessfulTransactions { get; set; }   // 成功事务数
        public long FailedTransactions { get; set; }       // 失败事务数
        public long BytesSent { get; set; }                // 发送字节数
        public long BytesReceived { get; set; }            // 接收字节数
        public TimeSpan TotalCommunicationTime { get; set; } // 总通信时间
        public TimeSpan AverageResponseTime { get; set; }  // 平均响应时间
        public TimeSpan MaxResponseTime { get; set; }      // 最大响应时间
        public TimeSpan MinResponseTime { get; set; }      // 最小响应时间
        public DateTime LastResetTime { get; set; }        // 上次重置时间

        public double SuccessRate => TotalTransactions > 0 ?
            (double)SuccessfulTransactions / TotalTransactions * 100 : 0;

        public void Reset()
        {
            TotalTransactions = 0;
            SuccessfulTransactions = 0;
            FailedTransactions = 0;
            BytesSent = 0;
            BytesReceived = 0;
            TotalCommunicationTime = TimeSpan.Zero;
            AverageResponseTime = TimeSpan.Zero;
            MaxResponseTime = TimeSpan.Zero;
            MinResponseTime = TimeSpan.MaxValue;
            LastResetTime = DateTime.Now;
        }

        public void UpdateResponseTime(TimeSpan responseTime)
        {
            TotalCommunicationTime += responseTime;

            if (responseTime > MaxResponseTime)
                MaxResponseTime = responseTime;

            if (responseTime < MinResponseTime)
                MinResponseTime = responseTime;

            if (TotalTransactions > 0)
            {
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    TotalCommunicationTime.TotalMilliseconds / TotalTransactions);
            }
        }
    }

    /// <summary>
    /// 通信错误类型
    /// </summary>
    public enum CommunicationErrorType
    {
        Timeout = 0,            // 超时
        NoAcknowledge = 1,      // 无应答
        BusError = 2,           // 总线错误
        DataCorruption = 3,     // 数据损坏
        ChecksumError = 4,      // 校验错误
        AddressError = 5,       // 地址错误
        ProtocolError = 6,      // 协议错误
        HardwareError = 7,      // 硬件错误
        Unknown = 99            // 未知错误
    }
    #endregion

    #region 寄存器相关模型
    /// <summary>
    /// 寄存器映射
    /// </summary>
    public class RegisterMap
    {
        public uint Address { get; set; }              // 寄存器地址
        public string Name { get; set; }               // 寄存器名称
        public RegisterType Type { get; set; }         // 寄存器类型
        public int Size { get; set; }                  // 寄存器大小（字节）
        public RegisterAccess Access { get; set; }     // 访问权限
        public string Description { get; set; }        // 描述
        public object DefaultValue { get; set; }       // 默认值
        public object MinValue { get; set; }           // 最小值
        public object MaxValue { get; set; }           // 最大值
        public string Unit { get; set; }               // 单位
        public double Scale { get; set; } = 1.0;       // 缩放因子
        public double Offset { get; set; } = 0.0;      // 偏移量
        public List<BitField> BitFields { get; set; } = new List<BitField>(); // 位字段定义
    }

    /// <summary>
    /// 寄存器类型
    /// </summary>
    public enum RegisterType
    {
        Control = 0,        // 控制寄存器
        Status = 1,         // 状态寄存器
        Data = 2,           // 数据寄存器
        Configuration = 3,  // 配置寄存器
        Calibration = 4,    // 校准寄存器
        Identity = 5        // 身份寄存器
    }

    /// <summary>
    /// 寄存器访问权限
    /// </summary>
    public enum RegisterAccess
    {
        ReadOnly = 0,       // 只读
        WriteOnly = 1,      // 只写
        ReadWrite = 2,      // 读写
        WriteOnce = 3       // 一次写入
    }

    /// <summary>
    /// 位字段定义
    /// </summary>
    public class BitField
    {
        public string Name { get; set; }           // 位字段名称
        public int BitPosition { get; set; }       // 起始位位置
        public int BitWidth { get; set; }          // 位宽
        public string Description { get; set; }    // 描述
        public Dictionary<int, string> ValueMappings { get; set; } = new Dictionary<int, string>(); // 值映射

        /*new BitField
            {
                Name = "TEMP_ALERT",
                BitPosition = 0,
                BitWidth = 1,
                Description = "温度警报状态",
                ValueMappings = new Dictionary<int, string>
                {
                    { 0, "无温度警报" },
                    { 1, "温度超出设定限制" }
                }
            },
         * **/
    }

    /// <summary>
    /// 位字段值
    /// </summary>
    public class BitFieldValue
    {
        public BitField BitField { get; set; }
        public uint RawValue { get; set; }
        public string Description { get; set; } // 位字段值对应的描述例如 "无温度报警"
        public DateTime Timestamp { get; set; }

        public bool AsBool => RawValue != 0;
        public int AsInt => (int)RawValue;
        public string AsHex => $"0x{RawValue:X}";
        public string AsBinary => Convert.ToString(RawValue, 2).PadLeft(BitField.BitWidth, '0');
    }
    #endregion

    #region 事件参数
    /// <summary>
    /// 设备连接状态变化事件参数
    /// </summary>
    public class DeviceConnectionStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public DeviceConnectionState OldState { get; }
        public DeviceConnectionState NewState { get; }
        public string Reason { get; }
        public DateTime Timestamp { get; }

        public DeviceConnectionStateChangedEventArgs(string deviceId, DeviceConnectionState oldState,
            DeviceConnectionState newState, string reason = null)
        {
            DeviceId = deviceId;
            OldState = oldState;
            NewState = newState;
            Reason = reason;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 设备数据接收事件参数
    /// </summary>
    public class DeviceDataReceivedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public byte[] Data { get; }
        public string DataType { get; }
        public DateTime Timestamp { get; }

        public DeviceDataReceivedEventArgs(string deviceId, byte[] data, string dataType = null)
        {
            DeviceId = deviceId;
            Data = data;
            DataType = dataType;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 通信错误事件参数
    /// </summary>
    public class CommunicationErrorEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public CommunicationErrorType ErrorType { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public byte[] RawData { get; }
        public DateTime Timestamp { get; }

        public CommunicationErrorEventArgs(string deviceId, CommunicationErrorType errorType,
            string errorMessage, Exception exception = null, byte[] rawData = null)
        {
            DeviceId = deviceId;
            ErrorType = errorType;
            ErrorMessage = errorMessage;
            Exception = exception;
            RawData = rawData;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 设备警告事件参数
    /// </summary>
    public class DeviceWarningEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string WarningCode { get; }
        public string WarningMessage { get; }
        public DateTime Timestamp { get; }

        public DeviceWarningEventArgs(string deviceId, string warningCode, string warningMessage)
        {
            DeviceId = deviceId;
            WarningCode = warningCode;
            WarningMessage = warningMessage;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 寄存器值变化事件参数
    /// </summary>
    public class RegisterValueChangedEventArgs : EventArgs
    {
        public uint Address { get; set; }
        public byte OldValue { get; set; }
        public byte NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string RegisterName => $"REG_0x{Address:X4}";
        public string Description => $"Value changed from 0x{OldValue:X2} to 0x{NewValue:X2}";
    }

    #endregion
}
