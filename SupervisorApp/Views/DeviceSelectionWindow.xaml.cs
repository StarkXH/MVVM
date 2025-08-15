using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.ViewModels;
using System;
using System.Windows;

namespace SupervisorApp.Views
{
    /// <summary>
    /// 设备选择窗口
    /// </summary>
    public partial class DeviceSelectionWindow : Window
    {
        public DeviceSelectionViewModel ViewModel { get; }

        /// <summary>
        /// 选择的设备实例
        /// </summary>
        public IDevice SelectedDevice { get; private set; }

        public DeviceSelectionWindow()
        {
            InitializeComponent();

            ViewModel = new DeviceSelectionViewModel();
            DataContext = ViewModel;

            // 订阅ViewModel事件
            ViewModel.DeviceSelected += OnDeviceSelected;
            ViewModel.SelectionCancelled += OnSelectionCancelled;

            // 窗口事件
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;

            LogService.Instance.LogInfo("Device selection window initialized");
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("Device selection window loaded");
                
                // 可以在这里添加窗口加载时的初始化逻辑
                // 例如：自动选择默认设备、显示欢迎信息等
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during window load: {ex.Message}");
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("Device selection window closing");
                
                // 清理ViewModel资源
                ViewModel?.Cleanup();
                
                // 取消订阅事件
                if (ViewModel != null)
                {
                    ViewModel.DeviceSelected -= OnDeviceSelected;
                    ViewModel.SelectionCancelled -= OnSelectionCancelled;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error during window closing: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理设备选择确认事件
        /// </summary>
        private void OnDeviceSelected(object sender, DeviceSelectedEventArgs e)
        {
            try
            {
                SelectedDevice = e.DeviceInstance;
                
                LogService.Instance.LogInfo($"✅ Device selected in window: {e.SelectedDeviceInfo.DisplayName}");
                
                // 设置对话框结果为OK
                DialogResult = true;
                
                // 关闭窗口
                Close();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling device selection: {ex.Message}");
                MessageBox.Show($"An error occurred during the selection of the processing device: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理取消选择事件
        /// </summary>
        private void OnSelectionCancelled(object sender, EventArgs e)
        {
            try
            {
                LogService.Instance.LogInfo("❌ Device selection cancelled in window");
                
                // 设置对话框结果为Cancel
                DialogResult = false;
                
                // 关闭窗口
                Close();
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling selection cancellation: {ex.Message}");
                MessageBox.Show($"An error occurred while processing the cancellation operation.: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 静态方法：显示设备选择对话框
        /// </summary>
        /// <returns>选择的设备实例，如果取消则返回null</returns>
        public static IDevice ShowDeviceSelectionDialog()
        {
            try
            {
                var window = new DeviceSelectionWindow();
                
                LogService.Instance.LogInfo("Showing device selection dialog");
                
                var result = window.ShowDialog();
                
                if (result == true && window.SelectedDevice != null)
                {
                    LogService.Instance.LogInfo($"✅ Device selection dialog completed: {window.SelectedDevice.DeviceName}");
                    return window.SelectedDevice;
                }
                else
                {
                    LogService.Instance.LogInfo("❌ Device selection dialog cancelled or no device selected");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error showing device selection dialog: {ex.Message}");
                MessageBox.Show($"An error occurred when displaying the device selection dialog box: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}