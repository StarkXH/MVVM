using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SupervisorApp.Models
{
    /// <summary>
    /// 100寄存器测试设备 - 完整实现IDevice接口
    /// 时间：2025-08-06 08:44:49 UTC
    /// 作者：StarkXH
    /// </summary>
    public class TestDevice100 : IDevice
    {
        #region 私有字段

        private readonly Dictionary<uint, byte> _virtualRegisters;
        private readonly object _registerLock = new object(); // 添加锁
        private readonly Random _random;
        private System.Timers.Timer _simulationTimer;
        private bool _simulationTimerDisposed = false; // 🔧 添加标志来跟踪timer状态
        private readonly CommunicationStatistics _statistics;
        private readonly CommunicationConfig _communicationConfig;

        private DeviceConnectionState _connectionState;
        private bool _simulateErrors;
        private double _errorRate = 0.05;
        private DateTime? _lastCommunicationTime;

        #endregion

        public TestDevice100()
        {
            // 基本属性初始化
            DeviceID = "TEST_100_001";
            DeviceName = "TestDevice";
            DeviceType = DeviceType.CustomDevice;
            Protocol = CommunicationProtocol.I2C;
            DeviceAddress = 0x48; // 典型的I2C地址
            DeviceModel = "TEST100-DEV";
            FirmwareVersion = "1.0.0";

            // 初始化集合和统计
            _virtualRegisters = new Dictionary<uint, byte>();
            _random = new Random();
            _statistics = new CommunicationStatistics();
            _connectionState = DeviceConnectionState.Uninitialized;

            // 初始化通信配置
            _communicationConfig = new CommunicationConfig
            {
                Protocol = CommunicationProtocol.I2C,
                DeviceAddress = DeviceAddress,
                BusSpeed = 100000, // 100kHz
                Timeout = 1000,    // 1秒超时
                RetryCount = 3,
                UseChecksums = false
            };

            InitializeVirtualRegisters();
            InitializeSimulationTimer();
        }

        #region 模拟定时器接口

        /// <summary>
        /// 是否启用模拟定时器
        /// </summary>
        public bool SimulationEnabled
        {
            get => _simulationTimer?.Enabled ?? false;
            set
            {
                // 🔧 添加线程安全和空检查
                lock (_registerLock)
                {
                    if (_simulationTimer != null && !_simulationTimerDisposed)
                    {
                        try
                        {
                            if (value && ConnectionState == DeviceConnectionState.Ready)
                            {
                                _simulationTimer.Start();
                                LogService.Instance.LogInfo("🎭 Simulation timer started");
                            }
                            else
                            {
                                _simulationTimer.Stop();
                                LogService.Instance.LogInfo("🎭 Simulation timer stopped");
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            LogService.Instance.LogWarning("⚠️ Simulation timer already disposed");
                            _simulationTimerDisposed = true;
                        }
                    }
                    else if (value)
                    {
                        LogService.Instance.LogWarning("⚠️ Cannot start simulation: timer is null or disposed");
                    }
                }
            }
        }

        /// <summary>
        /// 启动模拟定时器
        /// </summary>
        public void StartSimulation()
        {
            // 🔧 添加更完整的检查
            if (ConnectionState != DeviceConnectionState.Ready)
            {
                LogService.Instance.LogWarning("⚠️ Cannot start simulation: device not ready");
                return;
            }

            lock (_registerLock)
            {
                if (_simulationTimer == null || _simulationTimerDisposed)
                {
                    LogService.Instance.LogWarning("⚠️ Simulation timer is null or disposed, recreating...");
                    InitializeSimulationTimer();
                }

                try
                {
                    SimulationEnabled = true;
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"❌ Failed to start simulation: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 停止模拟定时器
        /// </summary>
        public void StopSimulation()
        {
            lock (_registerLock)
            {
                try
                {
                    SimulationEnabled = false;
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"❌ Failed to stop simulation: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 设置模拟定时器间隔
        /// </summary>
        /// <param name="intervalMs">间隔时间（毫秒）</param>
        public void SetSimulationInterval(int intervalMs)
        {
            if (intervalMs <= 0)
            {
                LogService.Instance.LogWarning("⚠️ Invalid simulation interval, must be > 0");
                return;
            }

            lock (_registerLock)
            {
                try
                {
                    if (_simulationTimer != null && !_simulationTimerDisposed)
                    {
                        bool wasEnabled = _simulationTimer.Enabled;

                        _simulationTimer.Stop();
                        _simulationTimer.Interval = intervalMs;

                        if (wasEnabled && ConnectionState == DeviceConnectionState.Ready)
                        {
                            _simulationTimer.Start();
                        }

                        LogService.Instance.LogInfo($"🎭 Simulation interval updated to {intervalMs}ms");
                    }
                    else
                    {
                        LogService.Instance.LogWarning("⚠️ Cannot set interval: timer is null or disposed");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"❌ Failed to set simulation interval: {ex.Message}");
                }
            }
        }

        #endregion

        #region IDevice 基本属性实现

        public string DeviceID { get; }
        public string DeviceName { get; }
        public DeviceType DeviceType { get; }
        public CommunicationProtocol Protocol { get; }
        public uint DeviceAddress { get; }
        public string DeviceModel { get; }
        public string FirmwareVersion { get; }

        public DeviceConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                var oldState = _connectionState;
                _connectionState = value;

                // 触发连接状态变化事件
                if (oldState != value)
                {
                    ConnectionStateChanged?.Invoke(this,
                        new DeviceConnectionStateChangedEventArgs(DeviceID, oldState, value));
                }
            }
        }

        public DateTime? LastCommunicationTime => _lastCommunicationTime;
        public CommunicationStatistics Statistics => _statistics;

        #endregion

        #region IDevice 事件定义

        public event EventHandler<DeviceConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DeviceDataReceivedEventArgs> DataReceived;
        public event EventHandler<CommunicationErrorEventArgs> CommunicationError;
        public event EventHandler<DeviceWarningEventArgs> WarningOccurred;
        public event EventHandler<RegisterValueChangedEventArgs> RegisterValueChanged;

        #endregion

        #region IDevice 连接管理实现

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            ConnectionState = DeviceConnectionState.Initializing;

            try
            {
                // 模拟初始化延迟
                await Task.Delay(800, cancellationToken);

                // 模拟初始化失败 (5%概率)
                if (_random.NextDouble() < 0.05)
                {
                    ConnectionState = DeviceConnectionState.Error;
                    throw new InvalidOperationException("Device initialization failed.");
                }

                SetupTestScenarios(); // 连接设备时设置模拟值
                ConnectionState = DeviceConnectionState.Ready;
                // _simulationTimer?.Start();
                _statistics.Reset();

                return true;
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Uninitialized;
                throw;
            }
            catch (Exception)
            {
                ConnectionState = DeviceConnectionState.Error;
                throw;
            }
        }

        public async Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                StopSimulation();
                ConnectionState = DeviceConnectionState.Uninitialized;

                await Task.Delay(100, cancellationToken); // 模拟释放延迟
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(50, cancellationToken); // 模拟探测延迟

                // 模拟设备有时无响应
                if (_random.NextDouble() < 0.02) // 2%概率无响应
                {
                    ConnectionState = DeviceConnectionState.NotResponding;
                    return false;
                }

                return ConnectionState == DeviceConnectionState.Ready;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public async Task<bool> ResetAsync(DeviceResetType resetType = DeviceResetType.Soft, CancellationToken cancellationToken = default)
        {
            ConnectionState = DeviceConnectionState.Resetting;

            try
            {
                // 不同重置类型的延迟
                int delay = resetType switch
                {
                    DeviceResetType.Soft => 200,
                    DeviceResetType.Hard => 500,
                    DeviceResetType.Factory => 1000,
                    _ => 200
                };

                await Task.Delay(delay, cancellationToken);

                // 重置寄存器值
                if (resetType == DeviceResetType.Factory)
                {
                    InitializeVirtualRegisters(); // 恢复出厂设置
                }
                else
                {
                    SetupTestScenarios(); // 软重置只重置部分寄存器
                }

                _statistics.Reset();
                ConnectionState = DeviceConnectionState.Ready;

                return true;
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Error;
                return false;
            }
        }

        #endregion

        #region IDevice 数据通信实现

        public async Task<DeviceDataResult> ReadRegisterAsync(uint registerAddress, int length, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                // 更新统计信息
                _statistics.TotalTransactions++;

                // 检查连接状态
                if (ConnectionState != DeviceConnectionState.Ready)
                {
                    var result = DeviceDataResult.CreateFailure(
                        $"The equipment is not ready yet. Current status: {ConnectionState}",
                        CommunicationErrorType.HardwareError,
                        registerAddress);

                    _statistics.FailedTransactions++;
                    return result;
                }

                // 不设置Busy状态，允许并发读取
                // ConnectionState = DeviceConnectionState.Busy;

                // 模拟读取延迟
                var delay = _random.Next(5, 50);
                await Task.Delay(delay, cancellationToken);

                var responseTime = DateTime.Now - startTime;
                _lastCommunicationTime = DateTime.Now;

                // 模拟读取错误
                if (_simulateErrors && _random.NextDouble() < _errorRate)
                {
                    var errorResult = CreateRandomError(registerAddress);
                    _statistics.FailedTransactions++;
                    _statistics.UpdateResponseTime(responseTime);

                    ConnectionState = DeviceConnectionState.Ready;

                    // 触发通信错误事件
                    CommunicationError?.Invoke(this,
                        new CommunicationErrorEventArgs(DeviceID, errorResult.ErrorType.Value, errorResult.ErrorMessage));

                    return errorResult;
                }

                // 线程安全的读取寄存器
                var data = new byte[length];
                lock (_registerLock)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var address = registerAddress + (uint)i;
                        if (_virtualRegisters.TryGetValue(address, out byte value))
                        {
                            data[i] = value;
                        }
                        else
                        {
                            var errorResult = DeviceDataResult.CreateFailure(
                                $"Register Address 0x{address:X4} does not exist.",
                                CommunicationErrorType.AddressError,
                                address);

                            _statistics.FailedTransactions++;
                            ConnectionState = DeviceConnectionState.Ready;
                            return errorResult;
                        }
                    }
                }

                // 成功读取
                var successResult = DeviceDataResult.CreateSuccess(data, responseTime, registerAddress);
                _statistics.SuccessfulTransactions++;
                _statistics.BytesReceived += length;
                _statistics.UpdateResponseTime(responseTime);

                ConnectionState = DeviceConnectionState.Ready;

                // 触发数据接收事件
                DataReceived?.Invoke(this,
                    new DeviceDataReceivedEventArgs(DeviceID, data, "RegisterRead"));

                return successResult;
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Ready;
                _statistics.FailedTransactions++;
                return DeviceDataResult.CreateFailure("The operation has been cancelled.", CommunicationErrorType.Timeout, registerAddress);
            }
            catch (Exception ex)
            {
                ConnectionState = DeviceConnectionState.Error;
                _statistics.FailedTransactions++;
                return DeviceDataResult.CreateFailure($"Read exception: {ex.Message}", CommunicationErrorType.Unknown, registerAddress);
            }
        }

        public async Task<bool> WriteRegisterAsync(uint registerAddress, byte[] data, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                _statistics.TotalTransactions++;

                if (ConnectionState != DeviceConnectionState.Ready)
                    return false;

                ConnectionState = DeviceConnectionState.Busy;

                // 模拟写入延迟
                await Task.Delay(_random.Next(10, 80), cancellationToken);

                var responseTime = DateTime.Now - startTime;
                _lastCommunicationTime = DateTime.Now;

                // 模拟写入错误
                if (_simulateErrors && _random.NextDouble() < _errorRate)
                {
                    _statistics.FailedTransactions++;
                    _statistics.UpdateResponseTime(responseTime);
                    ConnectionState = DeviceConnectionState.Ready;
                    return false;
                }

                // 🟢 线程安全写入数据并触发事件
                for (int i = 0; i < data.Length; i++)
                {
                    var address = registerAddress + (uint)i;
                    if (!SetRegisterValue(address, data[i]))
                    {
                        _statistics.FailedTransactions++;
                        ConnectionState = DeviceConnectionState.Ready;
                        return false;
                    }
                }

                _statistics.SuccessfulTransactions++;
                _statistics.BytesSent += data.Length;
                _statistics.UpdateResponseTime(responseTime);
                ConnectionState = DeviceConnectionState.Ready;

                return true;
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Ready;
                _statistics.FailedTransactions++;
                return false;
            }
            catch (Exception)
            {
                ConnectionState = DeviceConnectionState.Error;
                _statistics.FailedTransactions++;
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
            // SPI风格的传输 - 先写后读
            var startTime = DateTime.Now;

            try
            {
                ConnectionState = DeviceConnectionState.Busy;

                await Task.Delay(_random.Next(20, 100), cancellationToken);

                var responseTime = DateTime.Now - startTime;
                _lastCommunicationTime = DateTime.Now;

                // 模拟SPI传输返回的数据
                var readData = new byte[readLength];
                _random.NextBytes(readData);

                ConnectionState = DeviceConnectionState.Ready;

                return DeviceDataResult.CreateSuccess(readData, responseTime);
            }
            catch (OperationCanceledException)
            {
                ConnectionState = DeviceConnectionState.Ready;
                return DeviceDataResult.CreateFailure("Transmission has been cancelled", CommunicationErrorType.Timeout);
            }
        }

        #endregion

        #region IDevice 配置管理实现

        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // 模拟读取设备信息的延迟

            return new DeviceInfo
            {
                ManufacturerId = "TEST_MFG",
                DeviceId = DeviceID,
                ProductId = "TEST100",
                SerialNumber = "SN123456789",
                FirmwareVersion = FirmwareVersion,
                HardwareRevision = "Rev_A",
                ManufacturingDate = new DateTime(2024, 1, 15),
                ExtendedInfo = new Dictionary<string, string>
                {
                    { "RegisterCount", "100" },
                    { "TestMode", "Simulation" },
                    { "ErrorRate", _errorRate.ToString("P2") },
                    { "ProtocolSupport", "I2C, SPI, SMBus" }
                }
            };
        }

        public CommunicationConfig GetCommunicationConfig()
        {
            return _communicationConfig;
        }

        public async Task<bool> SetCommunicationConfigAsync(CommunicationConfig config)
        {
            await Task.Delay(50); // 模拟配置延迟

            // 这里可以更新配置，但对于测试设备，我们只是模拟接受
            return true;
        }

        public IEnumerable<RegisterMap> GetRegisterMaps()
        {
            return TestDevice100RegisterMaps.GetRegisterMaps();
        }

        /// <summary>
        /// 更新BitField的值映射
        /// </summary>
        /// <param name="address"></param>
        /// <param name="position"></param>
        /// <param name="mappings"></param>
        public void UpdateValueMappings(uint address, int position, Dictionary<int, string> mappings)
        {
            try
            {
                var result = GetRegisterMaps();
                foreach (var map in result)
                {
                    if (map.Address == address)
                    {
                        foreach (var bitField in map.BitFields)
                        {
                            if (bitField.BitPosition == position)
                            {
                                bitField.ValueMappings = mappings;
                                LogService.Instance.LogInfo($"📝 Updated value mappings for register 0x{address:X4} at bit {position}");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error updating value mappings for register 0x{address:X4} at bit {position}: {ex.Message}");
            }
            
        }

        #endregion

        #region 测试控制方法

        /// <summary>
        /// 启用/禁用 错误模拟
        /// </summary>
        public void SetErrorSimulation(bool enabled, double errorRate = 0.05)
        {
            _simulateErrors = enabled;
            _errorRate = Math.Max(0, Math.Min(1, errorRate));
        }

        /// <summary>
        /// 手动设置寄存器值
        /// </summary>
        public bool SetRegisterValue(uint address, byte value)
        {
            lock (_registerLock)
            {
                if (_virtualRegisters.ContainsKey(address))
                {
                    var oldValue = _virtualRegisters[address];
                    if (oldValue != value)
                    {
                        _virtualRegisters[address] = value;

                        // 🟢 触发寄存器值变化事件
                        RegisterValueChanged?.Invoke(this, new RegisterValueChangedEventArgs
                        {
                            Address = address,
                            OldValue = oldValue,
                            NewValue = value,
                            Timestamp = DateTime.Now
                        });

                        LogService.Instance.LogInfo($"📝 Register 0x{address:X4} manually updated: 0x{oldValue:X2} → 0x{value:X2}");
                    }
                }
                else
                {
                    LogService.Instance.LogInfo($"📝 Register 0x{address:X4} does not exist");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 🟢 批量更新寄存器值
        /// </summary>
        public void SetMultipleRegisterValues(Dictionary<uint, byte> registerValues)
        {
            if (registerValues == null || !registerValues.Any())
                return;

            foreach (var kvp in registerValues)
            {
                SetRegisterValue(kvp.Key, kvp.Value);
            }

            LogService.Instance.LogInfo($"📝 Batch updated {registerValues.Count} registers");
        }

        /// <summary>
        /// 获取寄存器值
        /// </summary>
        public byte? GetRegisterValue(uint address)
        {
            return _virtualRegisters.TryGetValue(address, out byte value) ? (byte?)value : null;
        }

        /// <summary>
        /// 触发警告事件 - 用于测试
        /// </summary>
        public void TriggerWarning(string warningCode, string message)
        {
            WarningOccurred?.Invoke(this,
                new DeviceWarningEventArgs(DeviceID, warningCode, message));
        }

        /// <summary>
        /// 重置所有寄存器为随机值
        /// </summary>
        public void RandomizeAllRegisters()
        {
            var addresses = new List<uint>(_virtualRegisters.Keys);
            foreach (var address in addresses)
            {
                _virtualRegisters[address] = (byte)_random.Next(0, 256);
            }
        }

        /// <summary>
        /// 🟢 随机化指定寄存器
        /// </summary>
        public void RandomizeRegister(uint address)
        {
            var newValue = (byte)_random.Next(0, 256);
            SetRegisterValue(address, newValue);
        }

        /// <summary>
        /// 🟢 模拟寄存器位翻转
        /// </summary>
        public void FlipRegisterBit(uint address, int bitPosition)
        {
            if (bitPosition < 0 || bitPosition > 7)
                throw new ArgumentException("Bit position must be between 0 and 7");

            lock (_registerLock)
            {
                if (_virtualRegisters.ContainsKey(address))
                {
                    var oldValue = _virtualRegisters[address];
                    var newValue = (byte)(oldValue ^ (1 << bitPosition));

                    if (oldValue != newValue)
                    {
                        _virtualRegisters[address] = newValue;

                        RegisterValueChanged?.Invoke(this, new RegisterValueChangedEventArgs
                        {
                            Address = address,
                            OldValue = oldValue,
                            NewValue = newValue,
                            Timestamp = DateTime.Now
                        });

                        LogService.Instance.LogInfo($"🔄 Register 0x{address:X4} bit {bitPosition} flipped: 0x{oldValue:X2} → 0x{newValue:X2}");
                    }
                }
            }
        }

        #endregion

        #region 私有方法

        private DeviceDataResult CreateRandomError(uint registerAddress)
        {
            var errorTypes = new[] {
                CommunicationErrorType.Timeout,
                CommunicationErrorType.NoAcknowledge,
                CommunicationErrorType.BusError,
                CommunicationErrorType.DataCorruption,
                CommunicationErrorType.ChecksumError
            };

            var errorType = errorTypes[_random.Next(errorTypes.Length)];
            var errorMessage = GetErrorMessage(errorType, registerAddress);

            return DeviceDataResult.CreateFailure(errorMessage, errorType, registerAddress);
        }

        private string GetErrorMessage(CommunicationErrorType errorType, uint address)
        {
            return errorType switch
            {
                CommunicationErrorType.Timeout => $"Read register 0x{address:X4} timeout",
                CommunicationErrorType.NoAcknowledge => $"Register 0x{address:X4} NACK",
                CommunicationErrorType.BusError => $"Bus Error - Register 0x{address:X4}",
                CommunicationErrorType.DataCorruption => $"Data corruption - Register 0x{address:X4}",
                CommunicationErrorType.ChecksumError => $"Checksum Error - Register 0x{address:X4}",
                CommunicationErrorType.AddressError => $"Address Error - Register 0x{address:X4}",
                _ => $"Unkown Error - Reigster 0x{address:X4}"
            };
        }

        private void InitializeVirtualRegisters()
        {
            // 清空现有寄存器
            _virtualRegisters.Clear();

            // 初始化100个寄存器，地址从0x1000开始
            for (uint i = 0x1000; i < 0x1064; i++) // 0x1000 到 0x1063 (100个)
            {
                _virtualRegisters[i] = (byte)_random.Next(0, 256);
            }

            // 设置特定的测试值
            // SetupTestScenarios();
        }

        private void SetupTestScenarios()
        {
            // GPIO配置寄存器
            SetRegisterValue(0x1000, 0b00000101); // PIN0和PIN2为输出‘
            SetRegisterValue(0x1001, 0b00000001); // PIN0输出高
            SetRegisterValue(0x1002, 0b00000010); // PIN1输入高

            // SHT30传感器寄存器
            SetRegisterValue(0x1010, 0b00001000); // 数据就绪
            SetRegisterValue(0x1011, 0x61); // 温度高字节
            SetRegisterValue(0x1012, 0xA0); // 温度低字节

            // 电机控制寄存器
            SetRegisterValue(0x1020, 0x00); // 电机停止
            SetRegisterValue(0x1021, 0x00); // 速度为0

            // 系统状态寄存器
            SetRegisterValue(0x1030, 0b00001001); // 电源正常，初始化完成
        }

        private void InitializeSimulationTimer()
        {
            // 🔧 释放旧的timer
            lock (_registerLock)
            {
                try
                {
                    _simulationTimer = new System.Timers.Timer(2000); // 每2秒更新一次
                    _simulationTimer.Elapsed += OnSimulationTick;
                    _simulationTimer.AutoReset = true;
                    _simulationTimerDisposed = false; // 🔧 重置标志
                    LogService.Instance.LogInfo("🎭 Simulation timer initialized");
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"❌ Failed to initialize simulation timer: {ex.Message}");
                    _simulationTimerDisposed = true;
                }
            }
        }

        private void OnSimulationTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 🔧 添加安全检查
            try
            {
                if (ConnectionState != DeviceConnectionState.Ready) 
                    return;

                SimulateDeviceChanges();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error in simulation tick: {ex.Message}");
            }
        }

        private void SimulateDeviceChanges()
        {
            // 模拟温度传感器数据变化
            if (_virtualRegisters.ContainsKey(0x1011) && _virtualRegisters.ContainsKey(0x1012))
            {
                var tempValue = 24.0 + (_random.NextDouble() * 2.0); // 24-26°C
                var tempRaw = (ushort)((tempValue + 45) * 65535 / 175);

                SetRegisterValue(0x1011, (byte)(tempRaw >> 8));
                SetRegisterValue(0x1012, (byte)(tempRaw & 0xFF));
            }

            // 模拟GPIO输入变化
            if (_virtualRegisters.ContainsKey(0x1002))
            {
                if (_random.NextDouble() < 0.3) // 30%概率改变
                {
                    var currentInput = _virtualRegisters[0x1002];
                    var newInput = currentInput ^ (byte)(1 << _random.Next(0, 8));
                    SetRegisterValue(0x1002, (byte)newInput);
                }
            }

            // 模拟计数器递增
            if (_virtualRegisters.ContainsKey(0x1040))
            {
                SetRegisterValue(0x1040, (byte)((_virtualRegisters[0x1040] + 1) % 256));
            }

            // 模拟系统状态变化
            if (_virtualRegisters.ContainsKey(0x1030))
            {
                if (_random.NextDouble() < 0.1) // 10%概率触发不同状态
                {
                    var statusBits = new byte[] { 0x09, 0x0B, 0x0D }; // 不同的状态组合
                    SetRegisterValue(0x1030, statusBits[_random.Next(statusBits.Length)]);
                }
            }

            // 偶尔触发警告事件
            if (_random.NextDouble() < 0.05) // 5%概率触发警告
            {
                var warnings = new[]
                {
                    ("TEMP_HIGH", "The temperature sensor reading is too high"),
                    ("COMM_SLOW", "The communication response is slow"),
                    ("POWER_FLUCTUATION", "Power supply voltage fluctuation")
                };

                var warning = warnings[_random.Next(warnings.Length)];
                TriggerWarning(warning.Item1, warning.Item2);
            }
        }

        public void Dispose()
        {
            // 🔧 确保线程安全的dispose
            lock (_registerLock)
            {
                try
                {
                    if (_simulationTimer != null && !_simulationTimerDisposed)
                    {
                        _simulationTimer.Stop();
                        _simulationTimer.Dispose();
                        _simulationTimerDisposed = true; // 🔧 设置标志
                        _simulationTimer = null;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogWarning($"⚠️ Error disposing simulation timer: {ex.Message}");
                }

                try
                {
                    ConnectionState = DeviceConnectionState.Uninitialized;
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogWarning($"⚠️ Error setting connection state: {ex.Message}");
                }
            }

            LogService.Instance.LogInfo("🧹 TestDevice100 disposed");
        }

        #endregion
    }
}