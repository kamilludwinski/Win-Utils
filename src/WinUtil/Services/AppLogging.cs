using System.IO;
using Serilog;

namespace WinUtil.Services;

/// <summary>Configures Serilog: daily rolling files under <c>logs/</c> next to the app, or LocalAppData if not writable.</summary>
public static class AppLogging
{
    public const int RetainedDailyFiles = 31;

    public static string LogDirectory { get; private set; } = "";

    public static void Initialize()
    {
        var logDir = ResolveLogDirectory();
        LogDirectory = logDir;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDir, "winutil-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedDailyFiles,
                shared: true)
            .CreateLogger();

        Log.Information(
            "WinUtil starting. Version={Version}, BaseDirectory={BaseDirectory}, LogDirectory={LogDirectory}",
            AppVersionHelper.CurrentVersion,
            AppContext.BaseDirectory,
            logDir);
    }

    private static string ResolveLogDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "logs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinUtil", "logs"),
            Path.Combine(Path.GetTempPath(), "WinUtil", "logs"),
        };

        foreach (var dir in candidates)
        {
            try
            {
                Directory.CreateDirectory(dir);
                VerifyWritable(dir);
                return dir;
            }
            catch
            {
                // try next candidate
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "logs");
    }

    private static void VerifyWritable(string dir)
    {
        var test = Path.Combine(dir, ".write-test");
        File.WriteAllText(test, "x");
        File.Delete(test);
    }

    public static void Shutdown()
    {
        try
        {
            Log.Information("WinUtil exiting.");
        }
        catch
        {
            // ignored
        }

        Log.CloseAndFlush();
    }
}
