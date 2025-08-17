using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OperativaLogistica
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            HandleException("CurrentDomain_UnhandledException", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException("TaskScheduler_UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private void HandleException(string source, Exception? ex)
        {
            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var path = Path.Combine(desktop, $"OperativaLogistica_error_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path,
                    $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {source}{Environment.NewLine}{ex}");
                MessageBox.Show(
                    $"Se ha producido un error inesperado.\n\nOrigen: {source}\nMensaje: {ex?.Message}\n\n" +
                    $"Se ha guardado un log en:\n{path}",
                    "Operativa Logística", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // último recurso
                MessageBox.Show($"Error inesperado ({source}).", "Operativa Logística",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
