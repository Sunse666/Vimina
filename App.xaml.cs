using System.Windows;
using System.Windows.Threading;

namespace Vimina;

public partial class App : System.Windows.Application
{
    private static System.Threading.Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "Vimina_SingleInstance";
        _mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            var existingHwnd = Core.Helpers.WindowHelper.FindWindowByTitle("Vimina");
            if (existingHwnd != IntPtr.Zero)
            {
                Core.Helpers.WindowHelper.SetForegroundWindow(existingHwnd);
                Core.Helpers.WindowHelper.ShowWindow(existingHwnd, Core.Helpers.WindowHelper.SW_RESTORE);
            }
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show($"发生错误: {e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        System.Windows.MessageBox.Show($"发生严重错误: {ex?.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
