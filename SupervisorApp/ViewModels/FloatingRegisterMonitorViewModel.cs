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

        private double _windowOpacity = 0.9;
        private string _windowTitle = "Register Monitor";

        #endregion

        public FloatingRegisterMonitorViewModel()
        {
            DisplayName = "Floating Register Monitor";
            MonitoredRegisters = new ObservableCollection<RegisterItemViewModel>();
            InitializeCommands();
        }

        #region Events

        /// <summary>
        /// 监控寄存器集合变化事件
        /// </summary>
        public event EventHandler<MonitoredRegisterChangedEventArgs> MonitoredRegisterChanged;

        /// <summary>
        /// 触发监控寄存器变化事件
        /// </summary>
        /// <param name="register">变化的寄存器</param>
        /// <param name="changeType">变化类型</param>
        protected virtual void OnMonitoredRegisterChanged(RegisterItemViewModel register, MonitorChangeType changeType)
        {
            MonitoredRegisterChanged?.Invoke(this, new MonitoredRegisterChangedEventArgs(register, changeType));
        }

        #endregion

        #region Properties

        /// <summary>
        /// 被监控的寄存器集合
        /// </summary>
        public ObservableCollection<RegisterItemViewModel> MonitoredRegisters { get; }

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

            // 🟢 通知外部监听者寄存器已添加
            OnMonitoredRegisterChanged(register, MonitorChangeType.Added);

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

                // 🟢 通知外部监听者寄存器已移除
                OnMonitoredRegisterChanged(register, MonitorChangeType.Removed);

                LogService.Instance.LogInfo($"📊 Removed register {register.Name} from floating monitor");
            }
        }

        /// <summary>
        /// 清除所有监控寄存器
        /// </summary>
        public void ClearAllRegisters()
        {
            var registersToRemove = MonitoredRegisters.ToList(); // 创建副本避免集合修改异常

            MonitoredRegisters.Clear();
            UpdateCommandStates();
            RaisePropertyChanged(nameof(MonitoredCount));

            // 🟢 通知外部监听者所有寄存器已被清除
            foreach (var register in registersToRemove)
            {
                OnMonitoredRegisterChanged(register, MonitorChangeType.Removed);
            }

            LogService.Instance.LogInfo("📊 Cleared all monitored registers from floating monitor");
        }

        #endregion
    }

    /// <summary>
    /// 监控变化类型
    /// </summary>
    public enum MonitorChangeType
    {
        Added,
        Removed
    }

    /// <summary>
    /// 监控寄存器变化事件参数
    /// </summary>
    public class MonitoredRegisterChangedEventArgs : EventArgs
    {
        public RegisterItemViewModel Register { get; }
        public MonitorChangeType ChangeType { get; }

        public MonitoredRegisterChangedEventArgs(RegisterItemViewModel register, MonitorChangeType changeType)
        {
            Register = register;
            ChangeType = changeType;
        }
    }
}
