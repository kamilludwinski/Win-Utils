using System.Windows;
using System.Windows.Threading;
using WinUtil.Services;

namespace WinUtil;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        AppLogging.Initialize();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        ThemeService.Initialize();
        base.OnStartup(e);

        var main = new MainWindow();
        MainWindow = main;
        main.ShowInTaskbar = false;
        main.WindowState = WindowState.Minimized;
        main.Show();
        main.Hide();
        Serilog.Log.Information("Started in system tray (main window hidden).");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Serilog.Log.Error(args.Exception, "Dispatcher unhandled exception");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception ex)
            Serilog.Log.Fatal(ex, "AppDomain unhandled exception. IsTerminating={IsTerminating}", args.IsTerminating);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        AppLogging.Shutdown();
        base.OnExit(e);
    }
}
