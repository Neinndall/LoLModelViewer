using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;

namespace LoLModelViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            var mainWindow = new LoLModelViewer.Views.MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred during startup: {ex.ToString()}", "Startup Error");
            Shutdown();
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;
        LogUnhandledException(ex, "AppDomain.CurrentDomain.UnhandledException");
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        // Prevent default unhandled exception processing
        e.Handled = true;
    }

    private void LogUnhandledException(Exception ex, string source)
    {
        try
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            File.AppendAllText(logFilePath, string.Format("[{0}] Unhandled Exception ({1}): {2}\n{3}", DateTime.Now, source, ex.Message, ex.StackTrace));
        }
        catch (Exception logEx)
        {
            // Fallback if logging to file fails
            MessageBox.Show(string.Format("Failed to write to log file: {0}\nOriginal Exception: {1}", logEx.Message, ex.Message), "Logging Error");
        }
    }
}