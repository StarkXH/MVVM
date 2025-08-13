using GalaSoft.MvvmLight.Command;
using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.Helpers;
using SupervisorApp.Models;
using SupervisorApp.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;       // 用于路径操作
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SupervisorApp.ViewModels
{
    /// <summary>
    /// Register Map Visualization ViewModel - Based on enhanced ViewModelBase
    /// Time: 2025-08-06 08:12:21 UTC
    /// Author: StarkXH
    /// </summary>
    public class RegisterMapViewModel : ViewModelBase
    {
        #region Private Fields

        private IDevice _currentDevice;
        private RegisterItemViewModel _selectedRegister;
        private bool _isConnected;
        private int _simulationInterval = 2000;
        private bool _simulationEnabled = false;
        private string _deviceStatus = "Disconnected";
        private string _deviceId = "N/A";
        private string _deviceName = "N/A";
        private int _registerCount = 0;
        private ObservableCollection<SimulationIntervalOption> _simulationIntervalOptions;
        private System.Timers.Timer _timeUpdateTimer;
        private DateTime _currentTime;
        private FloatingRegisterMonitorView _floatingMonitorView;

        #endregion

        public RegisterMapViewModel()
        {
            DisplayName = "Register Monitor";
            RegisterItems = new ObservableCollection<RegisterItemViewModel>();

            InitializeCommands();
            InitalizeTimeUpdateTimer();
            // Don't load device in constructor, do it in OnLoadedAsync
        }

        #region Properties

        /// <summary>
        /// Current device
        /// </summary>
        public IDevice CurrentDevice
        {
            get => _currentDevice;
            set
            {
                if (Set(ref _currentDevice, value))
                {
                    UpdateDeviceInfo();
                    _ = OnDeviceChangedAsync(); // Handle device switching asynchronously
                }
            }
        }

        /// <summary>
        /// Device Status
        /// </summary>
        public string DeviceStatus
        {
            get => _deviceStatus;
            set => Set(ref _deviceStatus, value);
        }

        /// <summary>
        /// Device ID
        /// </summary>
        public string DeviceId
        {
            get => _deviceId;
            set => Set(ref _deviceId, value);
        }

        /// <summary>
        /// Device Name
        /// </summary>
        public string DeviceName
        {
            get => _deviceName;
            set => Set(ref _deviceName, value);
        }

        /// <summary>
        /// Register Count
        /// </summary>
        public int RegisterCount
        {
            get => _registerCount;
            set => Set(ref _registerCount, value);
        }

        /// <summary>
        /// Register items collection
        /// </summary>
        public ObservableCollection<RegisterItemViewModel> RegisterItems { get; }

        /// <summary>
        /// Selected register
        /// </summary>
        public RegisterItemViewModel SelectedRegister
        {
            get => _selectedRegister;
            set => Set(ref _selectedRegister, value);
        }

        /// <summary>
        /// Whether connected
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (Set(ref _isConnected, value))
                {
                    DeviceStatus = value ? "Connected" : "Disconnected";
                    // 🟢 连接状态变化时控制设备模拟
                    UpdateDeviceSimulation();

                    // 🟢 断开连接时自动停止自动刷新
                    if (!value && SimulationEnabled)
                    {
                        LogService.Instance.LogInfo("🔌 Device disconnected, auto refresh stopped");
                    }

                    // 命令状态更新
                    UpdateAllCommandStates();
                }
            }
        }

        /// <summary>
        /// Whether busy - 重写 IsBusy 属性
        /// </summary>
        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy != value)
                {
                    base.IsBusy = value;
                    UpdateAllCommandStates();
                }
            }
        }

        public int SimulationInterval
        {
            get => _simulationInterval;
            set
            {
                if (Set(ref _simulationInterval, value))
                {
                    UpdateDeviceSimulationInterval();
                }
            }
        }

        /// <summary>
        /// 🟢 是否启用设备模拟
        /// </summary>
        public bool SimulationEnabled
        {
            get => _simulationEnabled;
            set
            {
                if (Set(ref _simulationEnabled, value))
                {
                    UpdateDeviceSimulation();
                    LogService.Instance.LogInfo($"🎭 Device simulation {(value ? "enabled" : "disabled")} (Connected: {IsConnected})");
                }
            }
        }

        /// <summary>
        /// 🟢 模拟状态显示
        /// </summary>
        public string SimulationStatus
        {
            get
            {
                if (!IsConnected)
                    return "Simulation: Disconnected";

                if (!SimulationEnabled)
                    return "Simulation: Disabled";

                return $"Simulation: Active ({SimulationInterval}ms)";
            }
        }

        public ObservableCollection<SimulationIntervalOption> SimulationIntervalOptions
        {
            get
            {
                if (_simulationIntervalOptions == null)
                {
                    _simulationIntervalOptions = new ObservableCollection<SimulationIntervalOption>
                    {
                        new SimulationIntervalOption { Display = "1s", Value = 1000 },
                        new SimulationIntervalOption { Display = "2s", Value = 2000 },
                        new SimulationIntervalOption { Display = "3s", Value = 3000 },
                        new SimulationIntervalOption { Display = "5s", Value = 5000 },
                        new SimulationIntervalOption { Display = "10s", Value = 10000 }
                    };
                }
                return _simulationIntervalOptions;
            }
        }

        public DateTime Time
        {
            get => _currentTime;
            private set => Set(ref _currentTime, value);
        }

        public bool IsFloatingMonitorVisible => _floatingMonitorView?.IsVisible ?? false;

        #endregion

        #region Commands

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand DisconnectCommand { get; private set; }
        public AsyncRelayCommand RefreshAllCommand { get; private set; }
        public AsyncRelayCommand ReadAllCommand { get; private set; }
        public AsyncRelayCommand LoadTestCommand { get; private set; }
        public AsyncRelayCommand ExportToExcelCommand { get; private set; }
        public AsyncRelayCommand ImportFromExcelCommand { get; private set; }
        public RelayCommand StartSimulationCommand { get; private set; }
        public RelayCommand StopSimulationCommand { get; private set; }
        public RelayCommand<RegisterItemViewModel> AddToFloatingMonitorCommand { get; private set; }
        public RelayCommand<RegisterItemViewModel> RemoveFromFloatingMonitorCommand { get; private set; }
        public RelayCommand ShowFloatingMonitorCommand { get; private set; }
        public RelayCommand HideFloatingMonitorCommand { get; private set; }
        public RelayCommand ToggleFloatingMonitorCommand { get; private set; }

        private void InitializeCommands()
        {
            // Use base class ExecuteAsync to simplify async operations
            ConnectCommand = new RelayCommand(
                () => _ = ExecuteAsync(ConnectAsync, "Connecting to device..."),
                () => !IsConnected && !IsBusy);

            DisconnectCommand = new RelayCommand(
                DisconnectDevice,
                () => IsConnected && !IsBusy);

            RefreshAllCommand = new AsyncRelayCommand(
                () => ExecuteAsync(RefreshAllRegistersAsync, "Refreshing registers..."),
                () => IsConnected && !IsBusy);

            ReadAllCommand = new AsyncRelayCommand(
                () => ExecuteAsync(ReadAllRegistersAsync, "Reading all registers..."),
                () => IsConnected && !IsBusy);

            LoadTestCommand = new AsyncRelayCommand(
                () => ExecuteAsync(LoadTestAsync, "Loading test ..."),
                () => !IsBusy);

            ExportToExcelCommand = new AsyncRelayCommand(
                () => ExecuteAsync(ExportToExcelAsync, "Exporting BitField data to Excel..."),
                () => !IsBusy && RegisterItems.Count > 0);

            ImportFromExcelCommand = new AsyncRelayCommand(
                () => ExecuteAsync(ImportFromExcelAsync, "Importing BitField data from Excel..."),
                () => !IsBusy);

            StartSimulationCommand = new RelayCommand(
                StartSimulation,
                () => IsConnected && !SimulationEnabled && !IsBusy);

            StopSimulationCommand = new RelayCommand(
                StopSimulation,
                () => IsConnected && SimulationEnabled);

            AddToFloatingMonitorCommand = new RelayCommand<RegisterItemViewModel>(
                AddToFloatingMonitor,
                register => !IsRegisterInFloatingMonitor(register));

            RemoveFromFloatingMonitorCommand = new RelayCommand<RegisterItemViewModel>(
                RemoveFromFloatingMonitor,
                register => IsRegisterInFloatingMonitor(register));

            ShowFloatingMonitorCommand = new RelayCommand(
                ShowFloatingMonitor,
                () => !IsFloatingMonitorVisible);

            HideFloatingMonitorCommand = new RelayCommand(
                HideFloatingMonitor,
                () => IsFloatingMonitorVisible);

            ToggleFloatingMonitorCommand = new RelayCommand(
                ToggleFloatingMonitor);

        }

        private void UpdateAllCommandStates()
        {
            try
            {
                // 触发所有命令的 CanExecute 重新评估
                ConnectCommand?.RaiseCanExecuteChanged();
                DisconnectCommand?.RaiseCanExecuteChanged();
                RefreshAllCommand?.RaiseCanExecuteChanged();
                ReadAllCommand?.RaiseCanExecuteChanged();
                LoadTestCommand?.RaiseCanExecuteChanged();
                ExportToExcelCommand?.RaiseCanExecuteChanged();
                ImportFromExcelCommand?.RaiseCanExecuteChanged();
                StartSimulationCommand?.RaiseCanExecuteChanged();
                StopSimulationCommand?.RaiseCanExecuteChanged();

                UpdateFloatingMonitorCommands();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to update command states: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新浮动监视器命令状态
        /// </summary>
        private void UpdateFloatingMonitorCommands()
        {
            try
            {
                AddToFloatingMonitorCommand?.RaiseCanExecuteChanged();
                RemoveFromFloatingMonitorCommand?.RaiseCanExecuteChanged();
                ShowFloatingMonitorCommand?.RaiseCanExecuteChanged();
                HideFloatingMonitorCommand?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to update floating monitor commands: {ex.Message}");
            }
        }

        /// <summary>
        /// 🟢 强制刷新所有命令状态（用于调试）
        /// </summary>
        public void RefreshCommandStates()
        {
            UpdateAllCommandStates();
        }

        /// <summary>
        /// 重写 ExecuteAsync 以自动处理命令状态更新
        /// </summary>
        protected new async Task ExecuteAsync(Func<Task> operation, string operationDescription = null)
        {
            // 操作开始前更新命令状态
            UpdateAllCommandStates();

            try
            {
                await base.ExecuteAsync(operation, operationDescription);
            }
            finally
            {
                // 操作完成后再次更新命令状态
                UpdateAllCommandStates();
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Async initialization when page loads
        /// </summary>
        public override async Task OnLoadedAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Load test device automatically
                await LoadTestAsync();

            }, "Initializing device...");
        }

        /// <summary>
        /// Cleanup when page unloads
        /// </summary>
        public override void OnUnloaded()
        {
            if (_floatingMonitorView != null)
            {
                // 🟢 取消订阅事件，避免内存泄漏
                _floatingMonitorView.ViewModel.MonitoredRegisterChanged -= OnFloatingMonitorRegisterChanged;

                _floatingMonitorView.Close();
                _floatingMonitorView = null;
            }
            // 🟢 停止时间更新定时器
            StopTimeUpdateTimer();

            // 🟢 停止设备模拟
            if (CurrentDevice is TestDevice100 testDevice)
            {
                testDevice.StopSimulation();
            }

            // Disconnect device
            DisconnectDevice();

            base.OnUnloaded();
        }

        #endregion

        #region Device Simulation Control

        /// <summary>
        /// 更新设备模拟状态
        /// </summary>
        private void UpdateDeviceSimulation()
        {
            if (CurrentDevice is TestDevice100 testDevice)
            {
                if (SimulationEnabled && IsConnected)
                {
                    testDevice.StartSimulation();
                }
                else
                {
                    testDevice.StopSimulation();
                }

                // 通知状态变化
                RaisePropertyChanged(nameof(SimulationStatus));
            }
        }

        /// <summary>
        /// 更新设备模拟间隔
        /// </summary>
        private void UpdateDeviceSimulationInterval()
        {
            if (CurrentDevice is TestDevice100 testDevice)
            {
                testDevice.SetSimulationInterval(SimulationInterval);

                LogService.Instance.LogInfo($"🎭 Simulation interval updated to {SimulationInterval}ms");
                RaisePropertyChanged(nameof(SimulationStatus));
            }
        }

        /// <summary>
        /// 启动设备模拟
        /// </summary>
        public void StartSimulation()
        {
            if (IsConnected)
            {
                SimulationEnabled = true;
                UpdateAllCommandStates();
            }
            else
            {
                LogService.Instance.LogWarning("⚠️ Cannot start simulation: device not connected");
            }
        }

        /// <summary>
        /// 🟢 停止设备模拟
        /// </summary>
        public void StopSimulation()
        {
            SimulationEnabled = false;
            UpdateAllCommandStates();
        }

        #endregion

        #region Device Management

        private async Task LoadTestAsync()
        {
            await DeviceTestHelper.RunComprehensiveTest(CurrentDevice);
        }

        private void UpdateDeviceInfo()
        {
            if (CurrentDevice != null)
            {
                DeviceId = CurrentDevice.DeviceID ?? "Unknown";
                DeviceName = CurrentDevice.DeviceName ?? "Unknown Device";

                // Update register count
                var registerMaps = CurrentDevice.GetRegisterMaps()?.ToList();
                RegisterCount = registerMaps?.Count ?? 0;

                LogService.Instance.LogInfo($"Device info updated: ID={DeviceId}, Name={DeviceName}, Registers={RegisterCount}");
            }
            else
            {
                DeviceId = "N/A";
                DeviceName = "N/A";
                RegisterCount = 0;
                DeviceStatus = "No Device";
            }
        }

        private async Task OnDeviceChangedAsync()
        {
            await ExecuteAsync(async () =>
            {
                // 🟢 更换设备时先取消旧事件订阅
                UnsubscribeFromDeviceEvents();

                RegisterItems.Clear();

                if (CurrentDevice != null)
                {
                    var registerMaps = CurrentDevice.GetRegisterMaps();
                    foreach (var registerMap in registerMaps.Take(20))
                    {
                        var registerVM = new RegisterItemViewModel(registerMap, CurrentDevice);
                        RegisterItems.Add(registerVM);
                    }

                    LogService.Instance.LogInfo($"Loaded {RegisterItems.Count} registers from device");
                }

                await Task.CompletedTask; // Ensure async method

            }, "Loading register definitions...");
        }

        private async Task ConnectAsync()
        {
            if (CurrentDevice == null)
                throw new InvalidOperationException("No device selected");

            try
            {
                // 🟢 先订阅事件，然后初始化设备
                SubscribeToDeviceEvents();
                await CurrentDevice.InitializeAsync();

                // 🟢 确保连接状态正确设置
                IsConnected = CurrentDevice.ConnectionState == DeviceConnectionState.Ready;
                LogService.Instance.LogInfo($"Connected to device: {DeviceName}");

                // Read data once after successful connection
                await ReadAllRegistersAsync();
            }
            catch (Exception)
            {
                // 🟢 连接失败时确保状态正确
                IsConnected = false;
                throw;
            }
        }

        private void DisconnectDevice()
        {
            try
            {
                // 🟢 断开连接前先停止设备模拟
                if (CurrentDevice is TestDevice100 testDevice)
                {
                    testDevice.StopSimulation();
                }
                // 🟢 先取消事件订阅
                UnsubscribeFromDeviceEvents();
                CurrentDevice?.Dispose();
                IsConnected = false; // 🟢 手动设置为断开状态（因为 Dispose 后事件不会触发）
                LogService.Instance.LogInfo("Device disconnected");
                ClearError(); // Clear connection-related errors
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                UpdateAllCommandStates();
            }
        }

        #endregion

        #region Register Operations

        private async Task RefreshAllRegistersAsync()
        {
            if (!IsConnected || RegisterItems.Count == 0)
                return;

            // 🟢 顺序读取，避免并发竞争
            foreach (var registerVM in RegisterItems)
            {
                try
                {
                    var result = await CurrentDevice.ReadRegisterAsync(registerVM.Address, 1);
                    if (result.Success && result.Data?.Length > 0)
                    {
                        var newValue = result.Data[0];
                        // 强制更新UI显示
                        registerVM.UpdateValueFromDevice(newValue);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"Failed to refresh register {registerVM.Address:X4}: {ex.Message}");
                }
            }
            // 通知UI整个集合已经更新
            RaisePropertyChanged(nameof(RegisterItems));
            LogService.Instance.LogInfo($"Refreshed {RegisterItems.Count} registers");
        }

        private async Task ReadAllRegistersAsync()
        {
            if (!IsConnected || RegisterItems.Count == 0)
                return;

            var semaphore = new SemaphoreSlim(3, 3); // 最多三个并发

            // Read all registers in parallel
            var readTasks = RegisterItems.Select(async registerVM =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await registerVM.ReadValueAsync();
                }
                catch (Exception ex)
                {
                    // Single register read failure doesn't affect other registers
                    LogService.Instance.LogError($"Failed to read register {registerVM.Address:X4}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(readTasks);
            LogService.Instance.LogInfo($"Read operation completed for {RegisterItems.Count} registers");
        }

        #endregion

        #region Device Event Handling

        private void SubscribeToDeviceEvents()
        {
            if (CurrentDevice != null)
            {
                CurrentDevice.ConnectionStateChanged += OnDeviceConnectionStateChanged;
                CurrentDevice.DataReceived += OnDeviceDataReceived;
                CurrentDevice.CommunicationError += OnDeviceCommunicationError;
                CurrentDevice.WarningOccurred += OnDeviceWarningOccurred;
                CurrentDevice.RegisterValueChanged += OnRegisterValueChanged;
            }
        }

        private void UnsubscribeFromDeviceEvents()
        {
            if (CurrentDevice != null)
            {
                CurrentDevice.ConnectionStateChanged -= OnDeviceConnectionStateChanged;
                CurrentDevice.DataReceived -= OnDeviceDataReceived;
                CurrentDevice.CommunicationError -= OnDeviceCommunicationError;
                CurrentDevice.WarningOccurred -= OnDeviceWarningOccurred;
                CurrentDevice.RegisterValueChanged -= OnRegisterValueChanged;
            }
        }

        /// <summary>
        /// 🟢 处理设备连接状态变化 - 这里建立了真正的联系
        /// </summary>
        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionStateChangedEventArgs e)
        {
            // 🟢 根据设备的实际状态更新 IsConnected
            bool newIsConnected = e.NewState == DeviceConnectionState.Ready;

            if (IsConnected != newIsConnected)
            {
                IsConnected = newIsConnected;
                DeviceStatus = $"{e.NewState}";

                LogService.Instance.LogInfo($"🔌 Device connection state changed: {e.OldState} → {e.NewState}");
                LogService.Instance.LogInfo($"📊 ViewModel IsConnected updated: {IsConnected}");

                // 🟢 设备状态变化后强制更新命令状态
                UpdateAllCommandStates();
            }
        }

        private void OnDeviceDataReceived(object sender, DeviceDataReceivedEventArgs e)
        {
            LogService.Instance.LogInfo($"📡 Data received from {e.DeviceId}: {e.DataType}");
        }

        private void OnDeviceCommunicationError(object sender, CommunicationErrorEventArgs e)
        {
            LogService.Instance.LogError($"❌ Communication error from {e.DeviceId}: {e.ErrorMessage}");
        }

        private void OnDeviceWarningOccurred(object sender, DeviceWarningEventArgs e)
        {
            LogService.Instance.LogWarning($"⚠️ Device warning [{e.WarningCode}]: {e.WarningMessage}");
        }

        /// <summary>
        /// 🟢 处理寄存器值变化事件
        /// </summary>
        private void OnRegisterValueChanged(object sender, RegisterValueChangedEventArgs e)
        {
            try
            {
                // 查找对应的寄存器ViewModel
                var registerVM = RegisterItems.FirstOrDefault(r => r.Address == e.Address);
                if (registerVM != null)
                {
                    // 🟢 使用专门的设备更新方法，避免循环更新
                    registerVM.UpdateValueFromDevice(e.NewValue);

                    LogService.Instance.LogInfo($"🔄 Auto-updated {e.RegisterName}: {e.Description}");
                }
                else
                {
                    LogService.Instance.LogInfo($"🔄 Register value changed but not monitored: {e.RegisterName} = 0x{e.NewValue:X2}");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling register value change: {ex.Message}");
            }
        }

        #endregion

        #region Excel Import/Export Implementation

        /// <summary>
        /// 导出BitField数据到Excel
        /// </summary>
        private async Task ExportToExcelAsync()
        {
            try
            {
                // 先刷新所有寄存器值以获取最新数据
                await RefreshAllRegistersAsync();

                // 创建保存文件对话框
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export BitField Configuration to Excel",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"BitField_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 使用 BitFieldUtils 导出到 Excel
                    bool success = await BitFieldUtils.SaveRegisterMapToExcelAsync(
                        CurrentDevice,
                        saveFileDialog.FileName,
                        includeCurrentValues: true);

                    if (success)
                    {
                        LogService.Instance.LogInfo($"✅ BitField data exported to: {saveFileDialog.FileName}");

                        // 询问是否打开文件
                        var result = MessageBox.Show(
                            $"BitField configuration exported successfully!\n\nFile: {saveFileDialog.FileName}\n\nDo you want to open the Excel file?",
                            "Export Successful",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(saveFileDialog.FileName);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to export BitField configuration to Excel. Please check the log for details.",
                                       "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    LogService.Instance.LogInfo("Export cancelled");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Excel export failed: {ex.Message}");
                MessageBox.Show($"Excel export failed: {ex.Message}", "Export Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从Excel导入BitField配置
        /// </summary>
        private async Task ImportFromExcelAsync()
        {
            try
            {
                // 创建打开文件对话框
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import BitField Configuration from Excel",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    DefaultExt = "xlsx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // 使用 BitFieldUtils 从 Excel 导入
                    var result = await BitFieldUtils.LoadRegisterMapFromExcelAsync(
                        CurrentDevice,
                        openFileDialog.FileName);

                    if (result.Success)
                    {
                        LogService.Instance.LogInfo($"✅ BitField data imported from: {openFileDialog.FileName}");
                        LogService.Instance.LogInfo($"📊 Applied {result.AppliedCount} configuration changes");

                        // 导入成功后刷新所有寄存器值
                        await RefreshAllRegistersAsync();

                        string message = $"BitField configuration imported successfully!\n\n" +
                                       $"File: {Path.GetFileName(openFileDialog.FileName)}\n" +
                                       $"Applied Changes: {result.AppliedCount}\n" +
                                       $"Warnings: {result.ValidationErrors.Count(e => !e.IsError)}";

                        MessageBox.Show(message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string errorDetails = string.IsNullOrEmpty(result.ErrorMessage)
                            ? "Unknown error occurred"
                            : result.ErrorMessage;

                        if (result.ValidationErrors.Any())
                        {
                            errorDetails += "\n\nValidation Errors:\n" +
                                           string.Join("\n", result.ValidationErrors.Take(5).Select(e => $"• {e.Message}"));

                            if (result.ValidationErrors.Count > 5)
                            {
                                errorDetails += $"\n... and {result.ValidationErrors.Count - 5} more errors";
                            }
                        }

                        MessageBox.Show($"Failed to import BitField configuration:\n\n{errorDetails}",
                                       "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    LogService.Instance.LogInfo("Import cancelled");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Excel import failed: {ex.Message}");
                MessageBox.Show($"Excel import failed: {ex.Message}", "Import Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Time update

        private void InitalizeTimeUpdateTimer()
        {
            _timeUpdateTimer = new System.Timers.Timer(1000); // 每秒更新一次
            _timeUpdateTimer.Elapsed += OnTimeUpdateTick;
            _timeUpdateTimer.AutoReset = true; // 自动重置
            _timeUpdateTimer.Enabled = true;
        }

        /// <summary>
        /// 时间更新定时器事件处理
        /// </summary>
        private void OnTimeUpdateTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var newTime = DateTime.Now;

                // 🟢 只在秒数变化时才更新UI
                if (newTime.Second != _currentTime.Second)
                {
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        Time = newTime;
                    }));
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error updating time: {ex.Message}");
            }
        }

        private void StopTimeUpdateTimer()
        {
            if (_timeUpdateTimer != null)
            {
                _timeUpdateTimer.Stop();
                _timeUpdateTimer.Dispose();
                _timeUpdateTimer = null;
            }
        }

        #endregion

        #region Floating Monitor Management

        /// <summary>
        /// 获取或创建浮动监视窗口
        /// </summary>
        /// <returns></returns>
        private FloatingRegisterMonitorView GetOrCreateFloatingMonitor()
        {
            if (_floatingMonitorView == null)
            {
                _floatingMonitorView = new FloatingRegisterMonitorView();

                // 🟢 订阅浮动监视器的变化事件
                _floatingMonitorView.ViewModel.MonitoredRegisterChanged += OnFloatingMonitorRegisterChanged;

                LogService.Instance.LogInfo("🪟 Floating monitor window created");
            }
            return _floatingMonitorView;
        }

        /// <summary>
        /// 🟢 处理浮动监视器寄存器变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFloatingMonitorRegisterChanged(object sender, MonitoredRegisterChangedEventArgs e)
        {
            try
            {
                // 当浮动监视器中的寄存器发生变化时，更新相关命令状态
                UpdateFloatingMonitorCommands();

                LogService.Instance.LogInfo($"🔄 Updated commands for register {e.Register.Name} ({e.ChangeType})");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling floating monitor register change: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加寄存器到浮动监视器
        /// </summary>
        /// <param name="register"></param>
        public void AddToFloatingMonitor(RegisterItemViewModel register)
        {
            if (register == null) return;

            var monitor = GetOrCreateFloatingMonitor();
            monitor.ViewModel.AddRegister(register);

            // 如果窗口没有显示，则显示并激活
            if (!monitor.IsVisible)
            {
                monitor.ShowAndActivate();
            }

            UpdateFloatingMonitorCommands();
            RaisePropertyChanged(nameof(IsFloatingMonitorVisible));
        }

        /// <summary>
        /// 从浮动监视器中移除寄存器
        /// </summary>
        /// <param name="register"></param>
        public void RemoveFromFloatingMonitor(RegisterItemViewModel register)
        {
            if (register == null || _floatingMonitorView == null) return;

            _floatingMonitorView.ViewModel.RemoveRegister(register);
            UpdateFloatingMonitorCommands();
        }

        /// <summary>
        /// 显示浮动监视器窗口
        /// </summary>
        public void ShowFloatingMonitor()
        {
            var monitor = GetOrCreateFloatingMonitor();
            monitor.ShowAndActivate();
            UpdateFloatingMonitorCommands();
            RaisePropertyChanged(nameof(IsFloatingMonitorVisible));
        }

        /// <summary>
        /// 隐藏浮动监视器窗口
        /// </summary>
        public void HideFloatingMonitor()
        {
            if (_floatingMonitorView != null)
            {
                _floatingMonitorView.Hide();
                UpdateFloatingMonitorCommands();
                RaisePropertyChanged(nameof(IsFloatingMonitorVisible));
            }
        }

        /// <summary>
        /// 切换浮动监视器窗口的显示状态
        /// </summary>
        public void ToggleFloatingMonitor()
        {
            if (IsFloatingMonitorVisible)
            {
                HideFloatingMonitor();
            }
            else
            {
                ShowFloatingMonitor();
            }
        }

        /// <summary>
        /// 🟢 检查寄存器是否在浮动监视器中
        /// </summary>
        public bool IsRegisterInFloatingMonitor(RegisterItemViewModel register)
        {
            return _floatingMonitorView?.ViewModel.MonitoredRegisters.Contains(register) ?? false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Override error handling to provide more specific device-related error messages
        /// </summary>
        protected override string GetUserFriendlyErrorMessage(Exception exception)
        {
            // Device-specific error handling
            if (exception.Message.Contains("serial port") || exception.Message.Contains("串口"))
                return "Serial port communication failed, please check device connection";
            if (exception.Message.Contains("timeout") || exception.Message.Contains("超时"))
                return "Device response timeout, please check device status";
            if (exception.Message.Contains("address") || exception.Message.Contains("地址"))
                return "Invalid register address";

            // Fallback to base class generic handling
            return base.GetUserFriendlyErrorMessage(exception);
        }

        #endregion
    }

    /// <summary>
    /// 🟢 模拟间隔选项
    /// </summary>
    public class SimulationIntervalOption
    {
        public string Display { get; set; }
        public int Value { get; set; }
    }
}
