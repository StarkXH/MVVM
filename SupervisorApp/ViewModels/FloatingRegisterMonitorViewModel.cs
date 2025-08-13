using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;

namespace SupervisorApp.ViewModels
{
    public class FloatingRegisterMonitorViewModel : ViewModelBase
    {
        #region Private Fields

        private bool _isAlwaysOnTop = true;
        private double _windowOpacity = 0.9;
        private string _windowTitle = "Register Monitor";

        #endregion

        public FloatingRegisterMonitorViewModel()
        {
            DisplayName = "Floating Register Monitor";
            MonitoredRegisters = new ObservableCollection<RegisterItemViewModel>();
            InitializeCommands();
        }

        #region Properties

        /// <summary>
        /// 被监控的寄存器集合
        /// </summary>
        public ObservableCollection<RegisterItemViewModel> MonitoredRegisters { get; }

        /// <summary>
        /// 窗口是否总在前
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => _isAlwaysOnTop;
            set => Set(ref _isAlwaysOnTop, value);
        }

        /// <summary>
        /// 窗口透明度
        /// </summary>
        public double WindowOpacity
        {
            get => _windowOpacity;
            set => Set(ref _windowOpacity, value);
        }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => Set(ref _windowTitle, value);
        }

        /// <summary>
        /// 被监控寄存器的数量
        /// </summary>
        public int MonitoredCount => MonitoredRegisters.Count;

        #endregion

        #region Commands

        public RelayCommand<RegisterItemViewModel> RemoveRegisterCommand { get; private set; }
        public RelayCommand ClearAllCommand { get; private set; }
        public RelayCommand IncreaseOpacityCommand { get; private set; }
        public RelayCommand DecreaseOpacityCommand { get; private set; }
        public RelayCommand ToggleAlwaysOnTopCommand { get; private set; }

        private void InitializeCommands()
        {
            RemoveRegisterCommand = new RelayCommand<RegisterItemViewModel>(
                RemoveRegister);

            ClearAllCommand = new RelayCommand(
                ClearAllRegisters,
                () => MonitoredRegisters.Any());

            IncreaseOpacityCommand = new RelayCommand(
                () => WindowOpacity = Math.Min(1.0, WindowOpacity + 0.1),
                () => WindowOpacity < 1.0);

            DecreaseOpacityCommand = new RelayCommand(
                () => WindowOpacity = Math.Max(0.1, WindowOpacity - 0.1),
                () => WindowOpacity > 0.1);

            ToggleAlwaysOnTopCommand = new RelayCommand(
                () => IsAlwaysOnTop = !IsAlwaysOnTop);
        }

        private void UpdateCommandStates()
        {
            ClearAllCommand?.RaiseCanExecuteChanged();
            IncreaseOpacityCommand?.RaiseCanExecuteChanged();
            DecreaseOpacityCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Pulic Methods

        /// <summary>
        /// 添加寄存器到监控列表
        /// </summary>
        /// <param name="register"></param>
        public void AddRegister(RegisterItemViewModel register)
        {
            if (register == null) return;

            if (MonitoredRegisters.Any(r => r.Address == register.Address))
            {
                LogService.Instance.LogWarning($"⚠️ Register {register.Name} (0x{register.Address:X4}) is already being monitored");
                return;
            }

            MonitoredRegisters.Add(register);
            UpdateCommandStates();
            RaisePropertyChanged(nameof(MonitoredCount));

            LogService.Instance.LogInfo($"📊 Added register {register.Name} to floating monitor");
        }

        /// <summary>
        /// 从监控列表中移除寄存器
        /// </summary>
        /// <param name="register"></param>
        public void RemoveRegister(RegisterItemViewModel register)
        {
            if (register == null) return;

            if (MonitoredRegisters.Remove(register))
            {
                UpdateCommandStates();
                RaisePropertyChanged(nameof(MonitoredCount));
                LogService.Instance.LogInfo($"📊 Removed register {register.Name} from floating monitor");
            }
        }

        /// <summary>
        /// 清除所有监控寄存器
        /// </summary>
        public void ClearAllRegisters()
        {
            MonitoredRegisters.Clear();
            UpdateCommandStates();
            RaisePropertyChanged(nameof(MonitoredCount));
            LogService.Instance.LogInfo("📊 Cleared all monitored registers from floating monitor");
        }

        #endregion
    }
}
