using SupervisorApp.Examples;
using SupervisorApp.Test;
using SupervisorApp.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SupervisorApp.Views
{
    /// <summary>
    /// RegisterMapView.xaml 的交互逻辑
    /// </summary>
    public partial class RegisterMapView : UserControl
    {
        private RegisterMapViewModel _viewModel;

        public RegisterMapView()
        {
            InitializeComponent();

            // 创建ViewModel
            _viewModel = new RegisterMapViewModel();
            DataContext = _viewModel;

        }
    }
}

