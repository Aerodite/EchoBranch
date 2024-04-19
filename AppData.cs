using System;
using System.IO;

namespace EchoBranch
{
    public abstract class AppData
    {
        public static string GetAppDataPath()
        {
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "EchoBranch");
        }

        public static void CreateAppDataFolder()
        {
            var appDataPath = GetAppDataPath();
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
                LogFileHandler.CreateLogFile();
                LogFileHandler.WriteLog("AppData folder created");
            }
            else
            {
                Console.WriteLine("AppData folder already exists; last checked at: " + DateTime.Now);
                LogFileHandler.CreateLogFile();
                LogFileHandler.WriteLog("AppData folder already exists.");
            }
        }
    }
}