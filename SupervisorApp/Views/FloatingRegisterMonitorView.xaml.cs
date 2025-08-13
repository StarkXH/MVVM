using SupervisorApp.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace SupervisorApp.Views
{
    /// <summary>
    /// 浮动寄存器监控窗口
    /// </summary>
    public partial class FloatingRegisterMonitorView : Window
    {
        public FloatingRegisterMonitorViewModel ViewModel { get; }

        public FloatingRegisterMonitorView()
        {
            InitializeComponent();

            ViewModel = new FloatingRegisterMonitorViewModel();
            DataContext = ViewModel;

            // 处理窗口关闭事件
            Closing += OnWindowClosing;

            // 设置窗口初始位置
            SetInitialPosition();
        }

        private void SetInitialPosition()
        {
            // 将窗口定位在屏幕右上角
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width - 20;
            Top = workingArea.Top + 20;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // 隐藏窗口而不是关闭，这样可以保持监控状态
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// 显示窗口并激活
        /// </summary>
        public void ShowAndActivate()
        {
            Show();
            Activate();
            Focus();
        }

        /// <summary>
        /// 切换窗口显示/隐藏状态
        /// </summary>
        public void ToggleVisibility()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                ShowAndActivate();
            }
        }
    }
}
