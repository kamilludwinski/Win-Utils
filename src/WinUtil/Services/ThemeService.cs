using System.Windows.Media;
using Microsoft.Win32;
using Serilog;

namespace WinUtil.Services;

public static class ThemeService
{
    private static bool _subscribed;

    public static void Initialize()
    {
        Log.Debug("ThemeService.Initialize; system dark={Dark}", IsSystemDarkMode());
        ApplyTheme();
        if (_subscribed)
            return;
        _subscribed = true;
        SystemEvents.UserPreferenceChanged += (_, _) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(ApplyTheme);
        };
    }

    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = key?.GetValue("AppsUseLightTheme");
            if (v is int i)
                return i == 0;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public static void ApplyTheme()
    {
        var app = System.Windows.Application.Current;
        var dark = IsSystemDarkMode();

        if (dark)
            ApplyDark(app);
        else
            ApplyLight(app);
    }

    private static void ApplyLight(System.Windows.Application app)
    {
        Set(app, "WindowBrush", 0xFA, 0xFA, 0xFA);
        Set(app, "SurfaceBrush", 0xFF, 0xFF, 0xFF);
        Set(app, "SurfaceAltBrush", 0xF5, 0xF5, 0xF5);
        Set(app, "TextBrush", 0x1A, 0x1A, 0x1A);
        Set(app, "MutedTextBrush", 0x60, 0x60, 0x60);
        Set(app, "BorderBrush", 0xE0, 0xE0, 0xE0);
        Set(app, "AccentBrush", 0x00, 0x78, 0xD4);
        Set(app, "DangerBrush", 0xC4, 0x2B, 0x1C);
        Set(app, "DataGridHeaderBrush", 0xF0, 0xF0, 0xF0);
        Set(app, "DataGridRowAltBrush", 0xF8, 0xF8, 0xF8);
        Set(app, "TabHoverBrush", 0xEE, 0xEE, 0xEE);
    }

    private static void ApplyDark(System.Windows.Application app)
    {
        Set(app, "WindowBrush", 0x20, 0x20, 0x20);
        Set(app, "SurfaceBrush", 0x2C, 0x2C, 0x2C);
        Set(app, "SurfaceAltBrush", 0x26, 0x26, 0x26);
        Set(app, "TextBrush", 0xE8, 0xE8, 0xE8);
        Set(app, "MutedTextBrush", 0xA0, 0xA0, 0xA0);
        Set(app, "BorderBrush", 0x45, 0x45, 0x45);
        Set(app, "AccentBrush", 0x4C, 0xC2, 0xFF);
        Set(app, "DangerBrush", 0xFF, 0x99, 0x99);
        Set(app, "DataGridHeaderBrush", 0x32, 0x32, 0x32);
        Set(app, "DataGridRowAltBrush", 0x2A, 0x2A, 0x2A);
        Set(app, "TabHoverBrush", 0x38, 0x38, 0x38);
    }

    private static void Set(System.Windows.Application app, string key, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        if (brush.CanFreeze)
            brush.Freeze();
        app.Resources[key] = brush;
    }
}
