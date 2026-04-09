using Serilog;

namespace WinUtil.Tests;

internal static class ModuleInitializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void InitSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Fatal()
            .CreateLogger();
    }
}
