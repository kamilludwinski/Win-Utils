using System.Reflection;

namespace WinUtil.Services;

/// <summary>Current app version label for UI and update checks.</summary>
public static class AppVersionHelper
{
    /// <summary>e.g. <c>1.2.3</c> (no leading v).</summary>
    public static string CurrentVersion { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
        {
            var plus = info.IndexOf('+', StringComparison.Ordinal);
            if (plus >= 0)
                info = info[..plus];
            info = info.Trim();
            var dash = info.IndexOf('-', StringComparison.Ordinal);
            if (dash > 0)
                info = info[..dash];

            if (TryThreePartVersion(info, out var v))
                return v;
        }

        var vn = asm.GetName().Version;
        return vn is null ? "0.0.0" : $"{vn.Major}.{vn.Minor}.{vn.Build}";
    }

    private static bool TryThreePartVersion(string s, out string result)
    {
        result = "0.0.0";
        var parts = s.Split('.');
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            return false;

        static int P(string[] a, int i) =>
            i < a.Length && int.TryParse(a[i], out var n) ? n : 0;

        var major = P(parts, 0);
        var minor = P(parts, 1);
        var build = P(parts, 2);
        result = $"{major}.{minor}.{build}";
        return true;
    }
}
