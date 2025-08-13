using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;

namespace SupervisorApp.ViewModels
{
    /// <summary>
    /// Bit field item ViewModel
    /// Time: 2025-08-06 07:37:35 UTC
    /// Author: StarkXH
    /// </summary>
    public class BitFieldItemViewModel : ViewModelBase
    {
        private readonly BitField _bitField;
        private readonly RegisterItemViewModel _parentRegister;

        private uint _currentValue;
        private string _displayValue;
        private string _inputValue;
        private bool _isUpdatingFromRegister; // Flag to prevent circular updates

        public BitFieldItemViewModel(BitField bitField, RegisterItemViewModel parentRegister)
        {
            _bitField = bitField ?? throw new ArgumentNullException(nameof(bitField));
            _parentRegister = parentRegister ?? throw new ArgumentNullException(nameof(parentRegister));

            InitializeCommands();
        }

        #region Properties

        /// <summary>
        /// Bit field
        /// </summary>
        public BitField BitField => _bitField;

        /// <summary>
        /// Bit field name
        /// </summary>
        public string Name => _bitField.Name;

        /// <summary>
        /// Bit field description
        /// </summary>
        public string Description => _bitField.Description;

        /// <summary>
        /// Bit position
        /// </summary>
        public int BitPosition => _bitField.BitPosition;

        /// <summary>
        /// Bit width
        /// </summary>
        public int BitWidth => _bitField.BitWidth;

        /// <summary>
        /// Bit range display
        /// </summary>
        public string BitRange
        {
            get
            {
                if (BitWidth == 1)
                    return $"[{BitPosition}]";
                else
                    return $"[{BitPosition + BitWidth - 1}:{BitPosition}]";
            }
        }

        /// <summary>
        /// Whether the bit field control is enabled. (based on parent register's write access)
        /// </summary>
        public bool IsEnabled => _parentRegister.CanWrite;

        /// <summary>
        /// Current value
        /// </summary>
        public uint CurrentValue
        {
            get => _currentValue;
            set
            {
                var newValue = Math.Min(value, MaxValue); // 确保值在范围内
                if (Set(ref _currentValue, newValue))
                {
                    InputValue = newValue.ToString();

                    // Update parent register if not being updated from register
                    if (!_isUpdatingFromRegister)
                    {
                        _parentRegister.UpdateFromBitFieldChange(Name, newValue);
                        LogService.Instance.LogInfo($"Bit field {Name} changed to {newValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Display value (meaning description)
        /// </summary>
        public string DisplayValue
        {
            get => _displayValue;
            set => Set(ref _displayValue, value);
        }

        /// <summary>
        /// Input value
        /// </summary>
        public string InputValue
        {
            get => _inputValue;
            set => Set(ref _inputValue, value);
        }

        /// <summary>
        /// Binary representation
        /// </summary>
        public string AsBinary
        {
            get
            {
                var binaryString = Convert.ToString(CurrentValue, 2);
                return binaryString.PadLeft(BitWidth, '0');
            }
        }

        /// <summary>
        /// Maximum value
        /// </summary>
        public uint MaxValue => (uint)((1 << BitWidth) - 1);

        #endregion

        #region Commands

        public ICommand SetValueCommand { get; private set; }

        #endregion

        private void InitializeCommands()
        {
            SetValueCommand = new AsyncRelayCommand(SetValueAsync, () => _parentRegister.CanWrite);
        }

        /// <summary>
        /// Update bit field value (called by parent register)
        /// </summary>
        public void UpdateFromRegisterChange(uint value, string description)
        {
            _isUpdatingFromRegister = true;
            try
            {
                CurrentValue = value;
                DisplayValue = description ?? $"Value: {value}";
            }
            finally
            {
                _isUpdatingFromRegister = false;
            }
        }

        /// <summary>
        /// Update bit field value (legacy method for compatibility)
        /// </summary>
        public void UpdateValue(uint value, string description)
        {
            UpdateFromRegisterChange(value, description);
        }

        /// <summary>
        /// Set bit field value
        /// </summary>
        private async Task SetValueAsync()
        {
            try
            {
                if (uint.TryParse(InputValue, out uint newValue))
                {
                    if (newValue <= MaxValue)
                    {
                        await _parentRegister.SetBitFieldValueAsync(Name, newValue);
                        LogService.Instance.LogInfo($"Bit field {Name} set to {newValue} via command");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException($"Value {newValue} out of range (0-{MaxValue})");
                    }
                }
                else
                {
                    throw new FormatException("Please enter a valid number");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"Failed to set bit field value: {ex.Message}");
            }
        }
    }
}