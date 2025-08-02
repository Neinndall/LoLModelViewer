using System;
using System.IO;
using System.Windows;

namespace LoLModelViewer.Services
{
    public static class LogService
    {
        public static void LogError(string message)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                File.AppendAllText(logFilePath, string.Format("[{0}] {1}\n", DateTime.Now, message));
            }
            catch (Exception logEx)
            {
                MessageBox.Show(string.Format("Failed to write to log file: {0}\nOriginal Error: {1}", logEx.Message, message), "Logging Error");
            }
        }
    }
}
