using System.Globalization;

namespace WinUtil.Services;

/// <summary>Compares semantic-ish version strings (GitHub tags vs app version).</summary>
public static class VersionComparer
{
    /// <summary>Returns true if <paramref name="remote"/> is greater than <paramref name="current"/>.</summary>
    public static bool IsRemoteNewer(string current, string remote)
    {
        if (!TryNormalize(current, out var c))
            return false;
        if (!TryNormalize(remote, out var r))
            return false;
        return r > c;
    }

    private static bool TryNormalize(string? s, out Version v)
    {
        v = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim().TrimStart('v', 'V');
        var dash = s.IndexOf('-', StringComparison.Ordinal);
        if (dash > 0)
            s = s[..dash];

        var parts = s.Split('.');
        if (parts.Length == 0)
            return false;

        var major = ParsePart(parts[0]);
        var minor = parts.Length > 1 ? ParsePart(parts[1]) : 0;
        var build = parts.Length > 2 ? ParsePart(parts[2]) : 0;
        try
        {
            v = new Version(major, minor, build);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ParsePart(string p) =>
        int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
