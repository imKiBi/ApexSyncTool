using System;
using System.IO;
using System.Text;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Handles application logging
    /// </summary>
    public class Logger
    {
        private string _logPath;
        private object _lockObj = new object();

        public Logger(string logBasePath = null)
        {
            if (string.IsNullOrEmpty(logBasePath))
            {
                _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }
            else
            {
                _logPath = logBasePath;
            }

            if (!Directory.Exists(_logPath))
            {
                try
                {
                    Directory.CreateDirectory(_logPath);
                }
                catch { }
            }
        }

        public string LogPath => _logPath;

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (_lockObj)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logFile = Path.Combine(_logPath, $"ApexSync_{DateTime.Now:yyyy-MM-dd}.log");
                    string logLine = $"[{timestamp}] [{level}] {message}";

                    File.AppendAllText(logFile, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }

        public void LogError(string message, Exception ex = null)
        {
            string fullMessage = ex != null ? $"{message} => {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : message;
            Log(fullMessage, LogLevel.Error);
        }

        public string ExportLog()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string exportFile = Path.Combine(_logPath, $"ApexSync_log_export_{timestamp}.txt");
                string logFile = Path.Combine(_logPath, $"ApexSync_{DateTime.Now:yyyy-MM-dd}.log");

                if (File.Exists(logFile))
                {
                    File.Copy(logFile, exportFile, true);
                    return exportFile;
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError("导出日志失败", ex);
                return null;
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }
}
