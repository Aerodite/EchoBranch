using System;
using System.Diagnostics;
using System.IO;
using NLog;

namespace EchoBranch
{
    public abstract class LogFileHandler
    {
        private static string _logFilePath = Path.Combine(AppData.GetAppDataPath(), "logs/log.txt");

        public static void CreateLogFile()
        {
            var logDirectoryPath = Path.Combine(AppData.GetAppDataPath(), "logs");
            if (!Directory.Exists(logDirectoryPath))
            {
                Directory.CreateDirectory(logDirectoryPath);
            }

            var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            var logFileName = $"log_{dateTimeString}.txt";

            _logFilePath = Path.Combine(logDirectoryPath, logFileName);

            if (!File.Exists(_logFilePath))
            {
                File.Create(_logFilePath).Close();
            }
            Debug.WriteLine($"Log Directory: {_logFilePath}");
            LogManager.Flush();
        }

        public static void WriteLog(string logMessage)
        {
            var timestampedMessage = $"{DateTime.Now} - {logMessage}";
            File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
            LogManager.Flush();
        }
    }
}