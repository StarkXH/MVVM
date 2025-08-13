using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;

namespace SupervisorApp.Examples
{
    /// <summary>
    /// BitField测试设备 - 专门测试寄存器和位字段操作
    /// 时间：2025-08-08
    /// 作者：StarkXH
    /// </summary>
    public class BitFieldTestDevice : IDevice
    {
        #region 私有字段

        private readonly Dictionary<uint, byte> _registers;
        private readonly Random _random;
        private DeviceConnectionState _connectionState;
        private DateTime? _lastCommunicationTime;

        #endregion

        #region 构造函数

        public BitFieldTestDevice()
        {
            // 基本设备信息
            DeviceID = "BITFIELD_TEST_001";
            DeviceName = "BitField Test Device";
            DeviceType = DeviceType.CustomDevice;
            Protocol = CommunicationProtocol.I2C;
            DeviceAddress = 0x50;
            DeviceModel = "BITFIELD-TEST";
            FirmwareVersion = "2.0.0";

            // 初始化
            _registers = new Dictionary<uint, byte>();
            _random = new Random();
            _connectionState = DeviceConnectionState.Uninitialized;
            Statistics = new CommunicationStatistics();

            // 初始化寄存器数据
            InitializeRegisters();

            LogService.Instance.LogInfo("🔧 BitField Test Device created");
        }

        #endregion

        #region IDevice 基本属性

        public string DeviceID { get; }
        public string DeviceName { get; }
        public DeviceType DeviceType { get; }
        public CommunicationProtocol Protocol { get; }
        public uint DeviceAddress { get; }
        public string DeviceModel { get; }
        public string FirmwareVersion { get; }
        public DateTime? LastCommunicationTime => _lastCommunicationTime;
        public CommunicationStatistics Statistics { get; }

        public DeviceConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                var oldState = _connectionState;
                _connectionState = value;

                if (oldState != value)
                {
                    ConnectionStateChanged?.Invoke(this,
                        new DeviceConnectionStateChangedEventArgs(DeviceID, oldState, value));
                }
            }
        }

        #endregion

        #region IDevice 连接管理

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            ConnectionState = DeviceConnectionState.Initializing;

            try
            {
                // 模拟初始化延迟
                await Task.Delay(500, cancellationToken);

                ConnectionState = DeviceConnectionState.Ready;
                Statistics.Reset();

                LogService.Instance.LogInfo("✅ BitField Test Device initialized successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Error;
                LogService.Instance.LogError("❌ Device initialization cancelled");
                return false;
            }
            catch (Exception ex)
            {
                ConnectionState = DeviceConnectionState.Error;
                LogService.Instance.LogError($"❌ Device initialization failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(100, cancellationToken);
                ConnectionState = DeviceConnectionState.Uninitialized;
                LogService.Instance.LogInfo("✅ BitField Test Device released");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Device release failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(50, cancellationToken);
                return ConnectionState == DeviceConnectionState.Ready;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetAsync(DeviceResetType resetType = DeviceResetType.Soft, CancellationToken cancellationToken = default)
        {
            ConnectionState = DeviceConnectionState.Resetting;

            try
            {
                await Task.Delay(300, cancellationToken);
                
                if (resetType == DeviceResetType.Factory)
                {
                    InitializeRegisters(); // 恢复默认值
                }

                ConnectionState = DeviceConnectionState.Ready;
                Statistics.Reset();

                LogService.Instance.LogInfo($"✅ Device reset completed: {resetType}");
                return true;
            }
            catch (Exception ex)
            {
                ConnectionState = DeviceConnectionState.Error;
                LogService.Instance.LogError($"❌ Device reset failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDevice 数据通信

        public async Task<DeviceDataResult> ReadRegisterAsync(uint registerAddress, int length, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                // 检查连接状态
                if (ConnectionState != DeviceConnectionState.Ready)
                {
                    Statistics.FailedTransactions++;
                    return DeviceDataResult.CreateFailure(
                        $"Device not ready: {ConnectionState}",
                        CommunicationErrorType.HardwareError,
                        registerAddress);
                }

                // 模拟读取延迟
                await Task.Delay(_random.Next(10, 50), cancellationToken);

                var responseTime = DateTime.Now - startTime;
                _lastCommunicationTime = DateTime.Now;

                // 读取数据
                var data = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    var address = registerAddress + (uint)i;
                    if (_registers.TryGetValue(address, out byte value))
                    {
                        data[i] = value;
                    }
                    else
                    {
                        Statistics.FailedTransactions++;
                        return DeviceDataResult.CreateFailure(
                            $"Register 0x{address:X4} not found",
                            CommunicationErrorType.AddressError,
                            address);
                    }
                }

                // 更新统计
                Statistics.TotalTransactions++;
                Statistics.SuccessfulTransactions++;
                Statistics.BytesReceived += length;
                Statistics.UpdateResponseTime(responseTime);

                LogService.Instance.LogInfo($"📖 Read 0x{registerAddress:X4}: [{string.Join(" ", data.Select(b => $"0x{b:X2}"))}]");

                return DeviceDataResult.CreateSuccess(data, responseTime, registerAddress);
            }
            catch (OperationCanceledException)
            {
                Statistics.FailedTransactions++;
                return DeviceDataResult.CreateFailure("Operation cancelled", CommunicationErrorType.Timeout, registerAddress);
            }
            catch (Exception ex)
            {
                Statistics.FailedTransactions++;
                return DeviceDataResult.CreateFailure($"Read error: {ex.Message}", CommunicationErrorType.Unknown, registerAddress);
            }
        }

        public async Task<bool> WriteRegisterAsync(uint registerAddress, byte[] data, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                // 检查连接状态
                if (ConnectionState != DeviceConnectionState.Ready)
                {
                    Statistics.FailedTransactions++;
                    return false;
                }

                // 模拟写入延迟
                await Task.Delay(_random.Next(15, 60), cancellationToken);

                var responseTime = DateTime.Now - startTime;
                _lastCommunicationTime = DateTime.Now;

                // 写入数据
                for (int i = 0; i < data.Length; i++)
                {
                    var address = registerAddress + (uint)i;
                    if (_registers.ContainsKey(address))
                    {
                        var oldValue = _registers[address];
                        _registers[address] = data[i];

                        // 触发值变化事件
                        RegisterValueChanged?.Invoke(this, new RegisterValueChangedEventArgs
                        {
                            Address = address,
                            OldValue = oldValue,
                            NewValue = data[i],
                            Timestamp = DateTime.Now
                        });
                    }
                    else
                    {
                        Statistics.FailedTransactions++;
                        return false;
                    }
                }

                // 更新统计
                Statistics.TotalTransactions++;
                Statistics.SuccessfulTransactions++;
                Statistics.BytesSent += data.Length;
                Statistics.UpdateResponseTime(responseTime);

                LogService.Instance.LogInfo($"✍️ Write 0x{registerAddress:X4}: [{string.Join(" ", data.Select(b => $"0x{b:X2}"))}]");

                return true;
            }
            catch (OperationCanceledException)
            {
                Statistics.FailedTransactions++;
                return false;
            }
            catch (Exception ex)
            {
                Statistics.FailedTransactions++;
                LogService.Instance.LogError($"❌ Write error: {ex.Message}");
                return false;
            }
        }

        public async Task<byte?> ReadByteAsync(uint registerAddress, CancellationToken cancellationToken = default)
        {
            var result = await ReadRegisterAsync(registerAddress, 1, cancellationToken);
            return result.Success ? result.GetByte() : null;
        }

        public async Task<bool> WriteByteAsync(uint registerAddress, byte value, CancellationToken cancellationToken = default)
        {
            return await WriteRegisterAsync(registerAddress, new[] { value }, cancellationToken);
        }

        public async Task<ushort?> ReadUInt16Async(uint registerAddress, ByteOrder byteOrder = ByteOrder.LittleEndian, CancellationToken cancellationToken = default)
        {
            var result = await ReadRegisterAsync(registerAddress, 2, cancellationToken);
            return result.Success ? result.GetUInt16(byteOrder) : null;
        }

        public async Task<bool> WriteUInt16Async(uint registerAddress, ushort value, ByteOrder byteOrder = ByteOrder.LittleEndian, CancellationToken cancellationToken = default)
        {
            byte[] data = byteOrder == ByteOrder.LittleEndian
                ? new[] { (byte)(value & 0xFF), (byte)(value >> 8) }
                : new[] { (byte)(value >> 8), (byte)(value & 0xFF) };

            return await WriteRegisterAsync(registerAddress, data, cancellationToken);
        }

        public async Task<DeviceDataResult> TransferAsync(byte[] writeData, int readLength, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_random.Next(20, 100), cancellationToken);
            var readData = new byte[readLength];
            _random.NextBytes(readData);
            return DeviceDataResult.CreateSuccess(readData, TimeSpan.FromMilliseconds(50));
        }

        #endregion

        #region IDevice 配置管理

        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);

            return new DeviceInfo
            {
                ManufacturerId = "TEST_MFG",
                DeviceId = DeviceID,
                ProductId = "BITFIELD_TEST",
                SerialNumber = "BT789012",
                FirmwareVersion = FirmwareVersion,
                HardwareRevision = "Rev_2.0",
                ManufacturingDate = DateTime.Today,
                ExtendedInfo = new Dictionary<string, string>
                {
                    { "RegisterCount", _registers.Count.ToString() },
                    { "Type", "BitField Test Device" },
                    { "Features", "Register & BitField Operations" }
                }
            };
        }

        public CommunicationConfig GetCommunicationConfig()
        {
            return new CommunicationConfig
            {
                Protocol = Protocol,
                DeviceAddress = DeviceAddress,
                BusSpeed = 100000,
                Timeout = 1000,
                RetryCount = 3,
                UseChecksums = false
            };
        }

        public async Task<bool> SetCommunicationConfigAsync(CommunicationConfig config)
        {
            await Task.Delay(50);
            return true;
        }

        public IEnumerable<RegisterMap> GetRegisterMaps()
        {
            return BitFieldTestDeviceRegisterMaps.GetRegisterMaps();
        }

        #endregion

        #region 事件定义

        public event EventHandler<DeviceConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DeviceDataReceivedEventArgs> DataReceived;
        public event EventHandler<CommunicationErrorEventArgs> CommunicationError;
        public event EventHandler<DeviceWarningEventArgs> WarningOccurred;
        public event EventHandler<RegisterValueChangedEventArgs> RegisterValueChanged;

        #endregion

        #region 私有方法

        private void InitializeRegisters()
        {
            _registers.Clear();

            // 创建测试寄存器 (地址从 0x2000 开始)
            _registers[0x2000] = 0x85; // 控制寄存器：启用位=1, 模式=2, 速度=1
            _registers[0x2001] = 0x3C; // 状态寄存器：就绪=1, 错误=0, 忙=1, 电源=3
            _registers[0x2002] = 0xA0; // GPIO配置：PIN7,PIN5 为输出
            _registers[0x2003] = 0x1F; // GPIO输出：PIN0-4 输出高
            _registers[0x2004] = 0x07; // 中断配置：启用中断0,1,2
            _registers[0x2005] = 0xF0; // 数据寄存器：高4位数据
            _registers[0x2006] = 0x42; // 阈值寄存器：测试阈值

            LogService.Instance.LogInfo($"📋 Initialized {_registers.Count} BitField test registers");
        }

        public void Dispose()
        {
            ConnectionState = DeviceConnectionState.Uninitialized;
            LogService.Instance.LogInfo("🗑️ BitField Test Device disposed");
        }

        #endregion

        #region 寄存器值变化事件

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

    /// <summary>
    /// BitField测试设备的寄存器映射定义
    /// </summary>
    public static class BitFieldTestDeviceRegisterMaps
    {
        public static IEnumerable<RegisterMap> GetRegisterMaps()
        {
            return new[]
            {
                CreateControlRegister(),
                CreateStatusRegister(),
                CreateGpioConfigRegister(),
                CreateGpioOutputRegister(),
                CreateInterruptConfigRegister(),
                CreateDataRegister(),
                CreateThresholdRegister()
            };
        }

        private static RegisterMap CreateControlRegister()
        {
            return new RegisterMap
            {
                Address = 0x2000,
                Name = "CONTROL",
                Type = RegisterType.Control,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "Device control register with multiple bit fields",
                DefaultValue = (byte)0x85,
                BitFields = new List<BitField>
                {
                    new BitField
                    {
                        Name = "ENABLE",
                        BitPosition = 7,
                        BitWidth = 1,
                        Description = "Device enable control",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Disabled" },
                            { 1, "Enabled" }
                        }
                    },
                    new BitField
                    {
                        Name = "MODE",
                        BitPosition = 4,
                        BitWidth = 2,
                        Description = "Operation mode selection",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Idle" },
                            { 1, "Normal" },
                            { 2, "Fast" },
                            { 3, "Turbo" }
                        }
                    },
                    new BitField
                    {
                        Name = "SPEED",
                        BitPosition = 2,
                        BitWidth = 2,
                        Description = "Speed setting",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Slow" },
                            { 1, "Medium" },
                            { 2, "Fast" },
                            { 3, "Very Fast" }
                        }
                    },
                    new BitField
                    {
                        Name = "RESET",
                        BitPosition = 0,
                        BitWidth = 1,
                        Description = "Software reset bit",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Normal" },
                            { 1, "Reset" }
                        }
                    }
                }
            };
        }

        private static RegisterMap CreateStatusRegister()
        {
            return new RegisterMap
            {
                Address = 0x2001,
                Name = "STATUS",
                Type = RegisterType.Status,
                Size = 1,
                Access = RegisterAccess.ReadOnly,
                Description = "Device status register",
                DefaultValue = (byte)0x3C,
                BitFields = new List<BitField>
                {
                    new BitField
                    {
                        Name = "READY",
                        BitPosition = 7,
                        BitWidth = 1,
                        Description = "Device ready status",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Not Ready" },
                            { 1, "Ready" }
                        }
                    },
                    new BitField
                    {
                        Name = "ERROR",
                        BitPosition = 6,
                        BitWidth = 1,
                        Description = "Error status",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "No Error" },
                            { 1, "Error" }
                        }
                    },
                    new BitField
                    {
                        Name = "BUSY",
                        BitPosition = 5,
                        BitWidth = 1,
                        Description = "Busy status",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Idle" },
                            { 1, "Busy" }
                        }
                    },
                    new BitField
                    {
                        Name = "POWER_LEVEL",
                        BitPosition = 2,
                        BitWidth = 3,
                        Description = "Power level indication",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Very Low" },
                            { 1, "Low" },
                            { 2, "Medium" },
                            { 3, "High" },
                            { 4, "Very High" },
                            { 5, "Maximum" },
                            { 6, "Critical" },
                            { 7, "Emergency" }
                        }
                    }
                }
            };
        }

        private static RegisterMap CreateGpioConfigRegister()
        {
            return new RegisterMap
            {
                Address = 0x2002,
                Name = "GPIO_CONFIG",
                Type = RegisterType.Configuration,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "GPIO pin configuration (1=Output, 0=Input)",
                DefaultValue = (byte)0xA0,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "PIN0_DIR", BitPosition = 0, BitWidth = 1, Description = "PIN0 direction" },
                    new BitField { Name = "PIN1_DIR", BitPosition = 1, BitWidth = 1, Description = "PIN1 direction" },
                    new BitField { Name = "PIN2_DIR", BitPosition = 2, BitWidth = 1, Description = "PIN2 direction" },
                    new BitField { Name = "PIN3_DIR", BitPosition = 3, BitWidth = 1, Description = "PIN3 direction" },
                    new BitField { Name = "PIN4_DIR", BitPosition = 4, BitWidth = 1, Description = "PIN4 direction" },
                    new BitField { Name = "PIN5_DIR", BitPosition = 5, BitWidth = 1, Description = "PIN5 direction" },
                    new BitField { Name = "PIN6_DIR", BitPosition = 6, BitWidth = 1, Description = "PIN6 direction" },
                    new BitField { Name = "PIN7_DIR", BitPosition = 7, BitWidth = 1, Description = "PIN7 direction" }
                }
            };
        }

        private static RegisterMap CreateGpioOutputRegister()
        {
            return new RegisterMap
            {
                Address = 0x2003,
                Name = "GPIO_OUTPUT",
                Type = RegisterType.Data,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "GPIO output values (only for output pins)",
                DefaultValue = (byte)0x1F,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "PIN0_OUT", BitPosition = 0, BitWidth = 1, Description = "PIN0 output" },
                    new BitField { Name = "PIN1_OUT", BitPosition = 1, BitWidth = 1, Description = "PIN1 output" },
                    new BitField { Name = "PIN2_OUT", BitPosition = 2, BitWidth = 1, Description = "PIN2 output" },
                    new BitField { Name = "PIN3_OUT", BitPosition = 3, BitWidth = 1, Description = "PIN3 output" },
                    new BitField { Name = "PIN4_OUT", BitPosition = 4, BitWidth = 1, Description = "PIN4 output" },
                    new BitField { Name = "PIN5_OUT", BitPosition = 5, BitWidth = 1, Description = "PIN5 output" },
                    new BitField { Name = "PIN6_OUT", BitPosition = 6, BitWidth = 1, Description = "PIN6 output" },
                    new BitField { Name = "PIN7_OUT", BitPosition = 7, BitWidth = 1, Description = "PIN7 output" }
                }
            };
        }

        private static RegisterMap CreateInterruptConfigRegister()
        {
            return new RegisterMap
            {
                Address = 0x2004,
                Name = "INT_CONFIG",
                Type = RegisterType.Configuration,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "Interrupt configuration register",
                DefaultValue = (byte)0x07,
                BitFields = new List<BitField>
                {
                    new BitField
                    {
                        Name = "INT_ENABLE",
                        BitPosition = 7,
                        BitWidth = 1,
                        Description = "Global interrupt enable",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Disabled" },
                            { 1, "Enabled" }
                        }
                    },
                    new BitField
                    {
                        Name = "INT_TYPE",
                        BitPosition = 4,
                        BitWidth = 2,
                        Description = "Interrupt type",
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "Level" },
                            { 1, "Edge" },
                            { 2, "Pulse" },
                            { 3, "Toggle" }
                        }
                    },
                    new BitField
                    {
                        Name = "INT_SOURCES",
                        BitPosition = 0,
                        BitWidth = 4,
                        Description = "Interrupt source mask (bit0-3 for sources 0-3)"
                    }
                }
            };
        }

        private static RegisterMap CreateDataRegister()
        {
            return new RegisterMap
            {
                Address = 0x2005,
                Name = "DATA",
                Type = RegisterType.Data,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "Data register with multiple data fields",
                DefaultValue = (byte)0xF0,
                BitFields = new List<BitField>
                {
                    new BitField
                    {
                        Name = "HIGH_NIBBLE",
                        BitPosition = 4,
                        BitWidth = 4,
                        Description = "High 4-bit data"
                    },
                    new BitField
                    {
                        Name = "LOW_NIBBLE",
                        BitPosition = 0,
                        BitWidth = 4,
                        Description = "Low 4-bit data"
                    }
                }
            };
        }

        private static RegisterMap CreateThresholdRegister()
        {
            return new RegisterMap
            {
                Address = 0x2006,
                Name = "THRESHOLD",
                Type = RegisterType.Configuration,
                Size = 1,
                Access = RegisterAccess.ReadWrite,
                Description = "Threshold configuration register",
                DefaultValue = (byte)0x42,
                BitFields = new List<BitField>
                {
                    new BitField
                    {
                        Name = "UPPER_THRESHOLD",
                        BitPosition = 4,
                        BitWidth = 4,
                        Description = "Upper threshold value (0-15)"
                    },
                    new BitField
                    {
                        Name = "LOWER_THRESHOLD",
                        BitPosition = 0,
                        BitWidth = 4,
                        Description = "Lower threshold value (0-15)"
                    }
                }
            };
        }
    }
}
