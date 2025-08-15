using GalaSoft.MvvmLight.Command;
using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.Factories;
using SupervisorApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SupervisorApp.ViewModels
{
    /// <summary>
    /// 设备选择ViewModel
    /// </summary>
    public class DeviceSelectionViewModel : ViewModelBase
    {
        #region Private Fields

        private DeviceSelectionItem _selectedDevice;
        private IDevice _previewDevice;
        private string _connectionStatus = "Disconnected";
        private bool _isConnecting = false;

        #endregion

        public DeviceSelectionViewModel()
        {
            DisplayName = "Device select";
            
            AvailableDevices = new ObservableCollection<DeviceSelectionItem>();
            
            InitializeCommands();
            InitializeAvailableDevices();
        }

        #region Properties

        /// <summary>
        /// 可用设备列表
        /// </summary>
        public ObservableCollection<DeviceSelectionItem> AvailableDevices { get; }

        /// <summary>
        /// 选中的设备
        /// </summary>
        public DeviceSelectionItem SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (Set(ref _selectedDevice, value))
                {
                    UpdateDevicePreview();
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 预览设备实例
        /// </summary>
        public IDevice PreviewDevice
        {
            get => _previewDevice;
            private set => Set(ref _previewDevice, value);
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => Set(ref _connectionStatus, value);
        }

        /// <summary>
        /// 是否正在连接
        /// </summary>
        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (Set(ref _isConnecting, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 设备详细信息
        /// </summary>
        public string DeviceDetails
        {
            get
            {
                if (SelectedDevice == null) return "Please selecte a device";
                
                return $"Device Name: {SelectedDevice.DisplayName}\n" +
                       $"Device Type: {SelectedDevice.DeviceType}\n" +
                       $"Manufacturer: {SelectedDevice.Manufacturer ?? "Unknown"}\n" +
                       $"Model: {SelectedDevice.Model ?? "Unknown\""}\n" +
                       $"Protocol: {SelectedDevice.Protocol ?? "Unknown\""}\n" +
                       $"Description: {SelectedDevice.Description}";
            }
        }

        #endregion

        #region Commands

        public RelayCommand RefreshDevicesCommand { get; private set; }
        public AsyncRelayCommand TestConnectionCommand { get; private set; }
        public RelayCommand ConfirmSelectionCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        private void InitializeCommands()
        {
            RefreshDevicesCommand = new RelayCommand(
                RefreshDevices,
                () => !IsBusy);

            TestConnectionCommand = new AsyncRelayCommand(
                () => ExecuteAsync(TestConnectionAsync, "Test device connection..."),
                () => SelectedDevice != null && !IsConnecting && !IsBusy);

            ConfirmSelectionCommand = new RelayCommand(
                ConfirmSelection,
                () => SelectedDevice != null);

            CancelCommand = new RelayCommand(
                Cancel,
                () => !IsConnecting);
        }

        private void UpdateCommandStates()
        {
            RefreshDevicesCommand?.RaiseCanExecuteChanged();
            TestConnectionCommand?.RaiseCanExecuteChanged();
            ConfirmSelectionCommand?.RaiseCanExecuteChanged();
            CancelCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Events

        /// <summary>
        /// 设备选择确认事件
        /// </summary>
        public event EventHandler<DeviceSelectedEventArgs> DeviceSelected;

        /// <summary>
        /// 取消选择事件
        /// </summary>
        public event EventHandler SelectionCancelled;

        #endregion

        #region Methods

        /// <summary>
        /// 初始化可用设备列表
        /// </summary>
        private void InitializeAvailableDevices()
        {
            try
            {
                AvailableDevices.Clear();

                // 添加TestDevice100
                AvailableDevices.Add(new DeviceSelectionItem
                {
                    DeviceType = "TestDevice100",
                    DisplayName = "Test Device 100",
                    Description = "100 register test device, Used for development and debugging",
                    Manufacturer = "Stark Labs",
                    Model = "TEST100-DEV",
                    Protocol = "I2C",
                    IsRecommended = true,
                    IsAvailable = true,
                    StatusMessage = "Ready",
                    IconPath = "🧪" // 使用Emoji作为图标
                });

                // 可以添加更多设备类型
                AvailableDevices.Add(new DeviceSelectionItem
                {
                    DeviceType = "TestDevice200",
                    DisplayName = "Test Device 200",
                    Description = "200 register advanced testing equipment",
                    Manufacturer = "Stark Labs", 
                    Model = "TEST200-DEV",
                    Protocol = "SPI",
                    IsRecommended = false,
                    IsAvailable = false,
                    StatusMessage = "Under development",
                    IconPath = "🔬"
                });

                AvailableDevices.Add(new DeviceSelectionItem
                {
                    DeviceType = "RealDevice",
                    DisplayName = "VA79A",
                    Description = "A 15-CH high voltage level shifter",
                    Manufacturer = "Silergy",
                    Model = "REAL-DEV",
                    Protocol = "UART",
                    IsRecommended = false,
                    IsAvailable = false,
                    StatusMessage = "Hardware is required",
                    IconPath = "🔧"
                });

                // 默认选择推荐设备
                SelectedDevice = AvailableDevices.FirstOrDefault(d => d.IsRecommended && d.IsAvailable);

                LogService.Instance.LogInfo($"📋 Initialized {AvailableDevices.Count} available devices");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to initialize devices: {ex.Message}");
                HandleError(ex);
            }
        }

        /// <summary>
        /// 刷新设备列表
        /// </summary>
        private void RefreshDevices()
        {
            try
            {
                LogService.Instance.LogInfo("🔄 Refreshing device list...");
                InitializeAvailableDevices();
                LogService.Instance.LogInfo("✅ Device list refreshed successfully");
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to refresh devices: {ex.Message}");
                HandleError(ex);
            }
        }

        /// <summary>
        /// 更新设备预览
        /// </summary>
        private void UpdateDevicePreview()
        {
            try
            {
                // 清理之前的预览设备
                PreviewDevice?.Dispose();
                PreviewDevice = null;
                ConnectionStatus = "Disconnected";

                if (SelectedDevice?.IsAvailable == true)
                {
                    // 创建设备预览实例
                    PreviewDevice = DeviceFactory.CreateDevice(SelectedDevice.DeviceType);
                    ConnectionStatus = "The device has been loaded";
                    
                    LogService.Instance.LogInfo($"📱 Device preview updated: {SelectedDevice.DisplayName}");
                }

                // 通知UI更新设备详情
                RaisePropertyChanged(nameof(DeviceDetails));
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Creat device failed";
                LogService.Instance.LogError($"❌ Failed to create device preview: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试设备连接
        /// </summary>
        private async Task TestConnectionAsync()
        {
            if (PreviewDevice == null) return;

            IsConnecting = true;
            ConnectionStatus = "Testing connection...";

            try
            {
                // 测试设备初始化
                var initResult = await PreviewDevice.InitializeAsync();
                if (initResult)
                {
                    ConnectionStatus = "✅ Connection test successful";
                    LogService.Instance.LogInfo($"🔌 Connection test successful: {SelectedDevice.DisplayName}");

                    // 测试基本读取操作
                    var testResult = await PreviewDevice.ProbeAsync();
                    if (testResult)
                    {
                        ConnectionStatus = "✅ Device probe successful";
                        LogService.Instance.LogInfo($"📡 Device probe successful: {SelectedDevice.DisplayName}");
                    }
                    else
                    {
                        ConnectionStatus = "⚠️ Device probe failed";
                        LogService.Instance.LogWarning($"📡 Device probe failed: {SelectedDevice.DisplayName}");
                    }
                }
                else
                {
                    ConnectionStatus = "❌ Connection test failed";
                    LogService.Instance.LogError($"🔌 Connection test failed: {SelectedDevice.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"❌ Connection test error: {ex.Message}";
                LogService.Instance.LogError($"🔌 Connection test error: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        /// <summary>
        /// 确认选择
        /// </summary>
        private void ConfirmSelection()
        {
            if (SelectedDevice?.IsAvailable != true)
            {
                MessageBox.Show("Please select an available device", "Device selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                LogService.Instance.LogInfo($"✅ Device selected: {SelectedDevice.DisplayName}");
                
                // 触发设备选择事件
                DeviceSelected?.Invoke(this, new DeviceSelectedEventArgs(SelectedDevice, PreviewDevice));
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Failed to confirm selection: {ex.Message}");
                HandleError(ex);
            }
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        private void Cancel()
        {
            LogService.Instance.LogInfo("❌ Device selection cancelled");
            
            // 清理资源
            PreviewDevice?.Dispose();
            PreviewDevice = null;
            
            // 触发取消事件
            SelectionCancelled?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            PreviewDevice?.Dispose();
            PreviewDevice = null;
            base.Cleanup();
        }

        #endregion
    }

    /// <summary>
    /// 设备选择事件参数
    /// </summary>
    public class DeviceSelectedEventArgs : EventArgs
    {
        public DeviceSelectionItem SelectedDeviceInfo { get; }
        public IDevice DeviceInstance { get; }

        public DeviceSelectedEventArgs(DeviceSelectionItem deviceInfo, IDevice deviceInstance)
        {
            SelectedDeviceInfo = deviceInfo;
            DeviceInstance = deviceInstance;
        }
    }
}