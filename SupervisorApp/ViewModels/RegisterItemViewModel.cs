using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SupervisorApp.ViewModels
{
    /// <summary>
    /// Single register item ViewModel - Based on enhanced ViewModelBase
    /// Time: 2025-08-06 08:12:21 UTC
    /// Author: StarkXH
    /// </summary>
    public class RegisterItemViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly RegisterMap _registerMap;
        private readonly IDevice _device;

        private byte _currentValue;
        private string _hexValue;
        private string _binaryValue;
        private DateTime _lastUpdateTime;
        private bool _isUpdatingBitFields; // Flag to prevent circular updates
        private bool _isUpdatingFromDevice = false;

        #endregion

        public RegisterItemViewModel(RegisterMap registerMap, IDevice device)
        {
            _registerMap = registerMap ?? throw new ArgumentNullException(nameof(registerMap));
            _device = device ?? throw new ArgumentNullException(nameof(device));

            DisplayName = $"Register {registerMap.Name}";
            BitFieldItems = new ObservableCollection<BitFieldItemViewModel>();

            InitializeCommands();
            InitializeBitFields();
        }

        #region Properties

        public uint Address => _registerMap.Address;
        public string Name => _registerMap.Name;
        public string Description => _registerMap.Description;
        public RegisterType Type => _registerMap.Type;
        public RegisterAccess Access => _registerMap.Access;

        /// <summary>
        /// Current value (byte)
        /// </summary>
        public byte CurrentValue
        {
            get => _currentValue;
            set
            {
                if (Set(ref _currentValue, value))
                {
                    UpdateFormattedValues();

                    // 🟢 只有不是来自设备更新时才更新位字段
                    if (!_isUpdatingFromDevice && !_isUpdatingBitFields)
                    {
                        UpdateBitFields();
                    }

                    LastUpdateTime = DateTime.Now;

                    if (!_isUpdatingFromDevice)
                    {
                        LogService.Instance.LogInfo($"Register {Name} (0x{Address:X4}) value updated to 0x{value:X2}");
                    }
                }
            }
        }

        /// <summary>
        /// Hexadecimal value
        /// </summary>
        public string HexValue
        {
            get => _hexValue;
            set => Set(ref _hexValue, value);
        }

        /// <summary>
        /// Binary value
        /// </summary>
        public string BinaryValue
        {
            get => _binaryValue;
            set => Set(ref _binaryValue, value);
        }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => Set(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// Bit field items collection
        /// </summary>
        public ObservableCollection<BitFieldItemViewModel> BitFieldItems { get; }

        /// <summary>
        /// Whether it's readable
        /// </summary>
        public bool CanRead => _registerMap.Access != RegisterAccess.WriteOnly;

        /// <summary>
        /// Whether it's writable
        /// </summary>
        public bool CanWrite => _registerMap.Access != RegisterAccess.ReadOnly;

        #endregion

        #region Commands

        public AsyncRelayCommand WriteCommand { get; private set; }
        public AsyncRelayCommand ReadCommand { get; private set; }

        private void InitializeCommands()
        {
            WriteCommand = new AsyncRelayCommand(
                async () => await WriteValueAsync(CurrentValue), 
                () => CanWrite);
            ReadCommand = new AsyncRelayCommand(
                async () => await ReadValueAsync(), 
                () => CanRead);

        }

        #endregion

        #region Register Operations - Using base class ExecuteAsync

        /// <summary>
        /// Read register value - Shows how to simplify async operations
        /// </summary>
        public async Task ReadValueAsync()
        {
            await ExecuteAsync(async () =>
            {
                var result = await _device.ReadRegisterAsync(_registerMap.Address, _registerMap.Size);

                if (result.Success && result.Data?.Length > 0)
                {
                    CurrentValue = result.Data[0];
                    LogService.Instance.LogInfo($"Successfully read register {Name} (0x{Address:X4}): 0x{result.Data[0]:X2}");
                }
                else
                {
                    throw new InvalidOperationException(result.ErrorMessage ?? "Read failed");
                }

            }, $"Reading register {Address:X4}...");
        }

        /// <summary>
        /// Write register value
        /// </summary>
        public async Task WriteValueAsync(byte newValue)
        {
            await ExecuteAsync(async () =>
            {
                bool success = await _device.WriteRegisterAsync(_registerMap.Address, new[] { newValue });

                if (success)
                {
                    CurrentValue = newValue;
                    LogService.Instance.LogInfo($"Successfully wrote register {Name} (0x{Address:X4}): 0x{newValue:X2}");
                }
                else
                {
                    throw new InvalidOperationException("Write failed");
                }

            }, $"Writing register {Address:X4}...");
        }

        /// <summary>
        /// Set bit field value (called by bit field view models)
        /// </summary>
        public async Task SetBitFieldValueAsync(string bitFieldName, uint newValue)
        {
            await ExecuteAsync(async () =>
            {
                var bitField = _registerMap.BitFields.FirstOrDefault(bf => bf.Name == bitFieldName);
                if (bitField == null)
                    throw new ArgumentException($"Bit field {bitFieldName} does not exist");

                // Calculate new register value
                var currentData = new[] { CurrentValue };
                var newData = BitFieldUtils.SetBitField(currentData, bitField, newValue);

                // Write new value to device
                bool success = await _device.WriteRegisterAsync(_registerMap.Address, newData);
                if (success)
                {
                    // Update register value (this will trigger bit field updates)
                    CurrentValue = newData[0];
                    LogService.Instance.LogInfo($"Bit field {bitFieldName} set to {newValue}, register {Name} updated to 0x{newData[0]:X2}");
                }
                else
                {
                    throw new InvalidOperationException("Setting bit field failed");
                }

            }, $"Setting bit field {bitFieldName}...");
        }

        /// <summary>
        /// Update register value from bit field change (without writing to device)
        /// </summary>
        public void UpdateFromBitFieldChange(string bitFieldName, uint newValue)
        {
            try
            {
                var bitField = _registerMap.BitFields.FirstOrDefault(bf => bf.Name == bitFieldName);
                if (bitField == null) return;

                _isUpdatingBitFields = true;

                // Calculate new register value
                var currentData = new[] { CurrentValue };
                var newData = BitFieldUtils.SetBitField(currentData, bitField, newValue);

                // Update register value (this won't trigger bit field updates due to flag)
                CurrentValue = newData[0];

                LogService.Instance.LogInfo($"Register {Name} updated from bit field {bitFieldName} change: 0x{newData[0]:X2}");
            }
            finally
            {
                _isUpdatingBitFields = false;
            }
        }

        #endregion

        #region Device Update Methods

        /// <summary>
        /// 🟢 从外部更新寄存器值（来自设备事件）
        /// </summary>
        /// <param name="newValue">新值</param>
        /// <param name="skipDeviceWrite">是否跳过写入设备（默认跳过，因为值已经来自设备）</param>
        public void UpdateValueFromDevice(byte newValue, bool skipDeviceWrite = true)
        {
            if (_currentValue != newValue)
            {
                var oldValue = _currentValue;

                // 🟢 设置标志，防止循环更新
                _isUpdatingFromDevice = true;

                try
                {
                    CurrentValue = newValue;
                    LastUpdateTime = DateTime.Now;

                    // 🟢 确保格式化值也更新
                    UpdateFormattedValues();

                    // 🟢 确保位字段也更新
                    UpdateBitFields();

                    LogService.Instance.LogInfo($"📱 Register {Name} (0x{Address:X4}) updated from device: 0x{oldValue:X2} → 0x{newValue:X2}");

                    // 🟢 强制通知所有相关属性更新
                    RaisePropertyChanged(nameof(CurrentValue));
                    RaisePropertyChanged(nameof(HexValue));
                    RaisePropertyChanged(nameof(BinaryValue));
                    RaisePropertyChanged(nameof(LastUpdateTime));
                }
                finally
                {
                    _isUpdatingFromDevice = false;
                }
            }
        }

        /// <summary>
        /// 🟢 批量从设备更新多个寄存器值
        /// </summary>
        public void UpdateMultipleValuesFromDevice(Dictionary<uint, byte> registerValues)
        {
            if (registerValues.ContainsKey(Address))
            {
                UpdateValueFromDevice(registerValues[Address]);
            }
        }

        #endregion

        #region Helper Methods

        private void InitializeBitFields()
        {
            BitFieldItems.Clear();

            foreach (var bitField in _registerMap.BitFields)
            {
                var bitFieldVM = new BitFieldItemViewModel(bitField, this);
                BitFieldItems.Add(bitFieldVM);
            }
        }

        private void UpdateFormattedValues()
        {
            HexValue = $"0x{CurrentValue:X2}";
            BinaryValue = Convert.ToString(CurrentValue, 2).PadLeft(8, '0');
        }

        private void UpdateBitFields()
        {
            if (_registerMap.BitFields.Any())
            {
                var data = new[] { CurrentValue };
                var bitFieldValues = BitFieldUtils.ParseAllBitFields(data, _registerMap);

                foreach (var bitFieldVM in BitFieldItems)
                {
                    if (bitFieldValues.TryGetValue(bitFieldVM.Name, out var parsedValue))
                    {
                        bitFieldVM.UpdateFromRegisterChange(parsedValue.RawValue, parsedValue.Description);
                    }
                }
            }
        }

        /// <summary>
        /// Override error handling to provide register-specific error messages
        /// </summary>
        protected override string GetUserFriendlyErrorMessage(Exception exception)
        {
            if (exception is ArgumentException && exception.Message.Contains("input"))
                return "Input format error, please enter decimal numbers or hexadecimal numbers starting with 0x";

            return base.GetUserFriendlyErrorMessage(exception);
        }

        /// <summary>
        /// Handle value change from UI control
        /// </summary>
        public void OnValueChangedFromUI(ulong newValue)
        {
            var byteValue = (byte)Math.Min(newValue, 255);
            CurrentValue = byteValue;
        }

        /// <summary>
        /// Notify all bit fields that the parent register's access rights may have changed
        /// </summary>
        public void NotifyAccessChanged()
        {
            foreach (var bitFieldVM in BitFieldItems)
            {
                bitFieldVM.RaisePropertyChanged(nameof(bitFieldVM.IsEnabled));
            }
        }

        #endregion
    }
}