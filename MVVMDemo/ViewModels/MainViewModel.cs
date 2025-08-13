using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace MVVMLightLearning.ViewModels
{
    /// <summary>  
    /// MainViewModel - 演示 MVVMLight的基础功能  
    /// </summary>  
    public class MainViewModel : ViewModelBase
    {
        #region 私有字段  

        private string _welcomeMessage = "Welcome to MVVMLight Learning!";
        private int _counter = 0;
        private string _userName = "Stark";
        private bool _isButtonEnabled = true;

        #endregion

        #region 公共属性  

        /// <summary>  
        /// 欢迎消息 - 演示基本属性绑定  
        /// </summary>  
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set
            {
                Set(ref _welcomeMessage, value);
            }
        }

        /// <summary>
        /// 计数器 - 演示数据变化通知
        /// </summary>
        public int Counter
        {
            get => _counter;
            set
            {
                Set(() => Counter, ref _counter, value);

                // 当计数器改变时，更新欢迎消息
                WelcomeMessage = $"Hello {UserName}, you have clicked {Counter} times!";

                // 手动通知命令状态可能已改变
                ResetCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => Set(() => UserName, ref _userName, value);
        }

        /// <summary>
        /// 按钮是否启用 - 演示 UI 状态控制
        /// </summary>
        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set => Set(() => IsButtonEnabled, ref _isButtonEnabled, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 点击命令 - 演示无参数命令
        /// </summary>
        public ICommand ClickCommand { get; }

        /// <summary>
        /// 重置命令 - 演示带条件的命令
        /// </summary>
        public RelayCommand ResetCommand { get; }

        /// <summary>
        /// 切换按钮状态命令
        /// </summary>
        public ICommand ToggleButtonCommand { get; }

        /// <summary>
        /// 带参数的命令 - 演示参数传递
        /// </summary>
        public ICommand AddValueCommand { get; }

        #endregion

        #region 构造函数

        public MainViewModel()
        {
            ClickCommand = new RelayCommand(OnClick);
            ResetCommand = new RelayCommand(OnReset, CanReset);
            ToggleButtonCommand = new RelayCommand(OnToggleButton);
            AddValueCommand = new RelayCommand<int>(OnAddValue);
            WelcomeMessage = $"Hello {UserName}, welcome to MVVMLight Learning!";
        }

        #endregion

        #region 命令处理方法

        /// <summary>
        /// 处理点击事件
        /// </summary>
        private void OnClick()
        {
            Counter++;

            if (Counter == 10)
            {
                WelcomeMessage = $"Congratulations {UserName}, you have clicked 10 times!";
            }
        }

        /// <summary>
        /// 处理重置事件
        /// </summary>
        private void OnReset()
        {
            Counter = 0;
            WelcomeMessage = $"Hello {UserName}, counter has reseted!";
        }

        /// <summary>
        /// 判断是否可以执行重置命令
        /// </summary>
        /// <returns>当计数器大于0时返回true</returns>
        private bool CanReset()
        {
            return Counter > 0;
        }

        /// <summary>
        /// 切换按钮状态
        /// </summary>
        private void OnToggleButton()
        {
            IsButtonEnabled = !IsButtonEnabled;
        }

        /// <summary>
        /// 添加指定数值到计数器
        /// </summary>
        /// <param name="value"></param>
        private void OnAddValue(int value)
        {
            Counter += value;
            WelcomeMessage = $"{UserName}, you have added {value} to the counter. Total: {Counter}";
            if (Counter < 0)
            {
                WelcomeMessage = $"{UserName}, the counter is less than zero. Total: {Counter}";
            }
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            // 在这里清理资源或取消订阅事件
            base.Cleanup();
        }

        #endregion
    }
}