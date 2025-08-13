using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SupervisorApp.Core.Common
{
    /// <summary>
    /// Global log service
    /// </summary>
    public class LogService : INotifyPropertyChanged
    {
        private static LogService _instance;
        private static readonly object _lock = new object();
        private readonly StringBuilder _logBuffer;
        private string _logText;

        private LogService()
        {
            _logBuffer = new StringBuilder();
            _logText = string.Empty;
        }

        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new LogService();
                    }
                }
                return _instance;
            }
        }

        public string LogText
        {
            get => _logText;
            private set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public void LogInfo(string message)
        {
            LogMessage("INFO", message);
        }

        public void LogWarning(string message)
        {
            LogMessage("WARN", message);
        }

        public void LogError(string message)
        {
            LogMessage("ERROR", message);
        }

        public void LogDebug(string message)
        {
            LogMessage("DEBUG", message);
        }

        private void LogMessage(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}\r\n";

            // Update on UI thread
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _logBuffer.Append(logEntry);
                    LogText = _logBuffer.ToString();
                }), DispatcherPriority.Background);
            }
            else
            {
                _logBuffer.Append(logEntry);
                LogText = _logBuffer.ToString();
            }
        }

        public void Clear()
        {
            _logBuffer.Clear();
            LogText = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
