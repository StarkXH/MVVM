using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace SupervisorApp.Core.Common
{
    /// <summary>
    /// 自定义异步命令实现 - 补充MVVMLight的功能
    /// 提供完整的异步执行、取消支持和状态监控
    /// 时间：2025-08-06 08:08:25 UTC
    /// 作者：StarkXH
    /// </summary>
    public class AsyncRelayCommand : ICommand, INotifyPropertyChanged, IDisposable
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dispatcher _dispatcher;

        private bool _isExecuting;
        private bool _isCancellationRequested;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _cancellationTokenSource = new CancellationTokenSource();
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否已请求取消
        /// </summary>
        public bool IsCancellationRequested
        {
            get => _isCancellationRequested;
            private set => SetProperty(ref _isCancellationRequested, value);
        }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public bool CanExecute(object parameter)
        {
            if (IsExecuting)
                return false;

            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync();
        }

        /// <summary>
        /// 异步执行方法
        /// </summary>
        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
                return;

            try
            {
                IsExecuting = true;
                IsCancellationRequested = false;

                await _execute();
            }
            catch (OperationCanceledException)
            {
                // 正常的取消操作
            }
            catch (Exception ex)
            {
                OnExecutionFailed(ex);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 取消命令执行
        /// </summary>
        public void Cancel()
        {
            if (IsExecuting && !IsCancellationRequested)
            {
                IsCancellationRequested = true;
                _cancellationTokenSource.Cancel();
            }
        }

        public event EventHandler CanExecuteChanged;
        public event EventHandler<AsyncCommandExecutionFailedEventArgs> ExecutionFailed;

        public void RaiseCanExecuteChanged()
        {
            if (_dispatcher.CheckAccess())
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
        }

        protected virtual void OnExecutionFailed(Exception exception)
        {
            if (_dispatcher.CheckAccess())
            {
                ExecutionFailed?.Invoke(this, new AsyncCommandExecutionFailedEventArgs(exception));
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => ExecutionFailed?.Invoke(this, new AsyncCommandExecutionFailedEventArgs(exception))));
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (_dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// 泛型参数化异步命令
    /// </summary>
    public class AsyncRelayCommand<T> : ICommand, INotifyPropertyChanged, IDisposable
    {
        private readonly Func<T, Task> _execute;
        private readonly Predicate<T> _canExecute;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dispatcher _dispatcher;

        private bool _isExecuting;
        private bool _isCancellationRequested;

        public AsyncRelayCommand(Func<T, Task> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _cancellationTokenSource = new CancellationTokenSource();
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsCancellationRequested
        {
            get => _isCancellationRequested;
            private set => SetProperty(ref _isCancellationRequested, value);
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public bool CanExecute(object parameter)
        {
            if (IsExecuting)
                return false;

            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return false;

            if (parameter != null && !(parameter is T))
                return false;

            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync((T)parameter);
        }

        public async Task ExecuteAsync(T parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                IsExecuting = true;
                IsCancellationRequested = false;

                await _execute(parameter);
            }
            catch (OperationCanceledException)
            {
                // 正常的取消操作
            }
            catch (Exception ex)
            {
                OnExecutionFailed(ex);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void Cancel()
        {
            if (IsExecuting && !IsCancellationRequested)
            {
                IsCancellationRequested = true;
                _cancellationTokenSource.Cancel();
            }
        }

        public event EventHandler CanExecuteChanged;
        public event EventHandler<AsyncCommandExecutionFailedEventArgs> ExecutionFailed;

        public void RaiseCanExecuteChanged()
        {
            if (_dispatcher.CheckAccess())
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
        }

        protected virtual void OnExecutionFailed(Exception exception)
        {
            if (_dispatcher.CheckAccess())
            {
                ExecutionFailed?.Invoke(this, new AsyncCommandExecutionFailedEventArgs(exception));
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => ExecutionFailed?.Invoke(this, new AsyncCommandExecutionFailedEventArgs(exception))));
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (_dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))));
            }
        }

        protected bool SetProperty<TProperty>(ref TProperty field, TProperty value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// 异步命令执行失败事件参数
    /// </summary>
    public class AsyncCommandExecutionFailedEventArgs : EventArgs
    {
        public AsyncCommandExecutionFailedEventArgs(Exception exception)
        {
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }

        public Exception Exception { get; }
        public DateTime Timestamp { get; }
        public string Message => Exception?.Message;
        public string ExceptionType => Exception?.GetType().Name;
    }
}
