namespace WinUtil.Services;

/// <summary>RegisterHotKey modifier flags: Alt=1, Ctrl=2, Shift=4, Win=8.</summary>
public sealed class HotKeyConfig
{
    /// <summary>Default: Ctrl+W (Ctrl=2).</summary>
    public uint Modifiers { get; set; } = 2;

    public uint VirtualKey { get; set; } = 0x57;
}

public sealed class ExclusionEntry
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
}

/// <summary>Single settings file: global hotkey + process/port exclusion lists.</summary>
public sealed class AppSettings
{
    /// <summary>Optional <c>owner/repo</c> for GitHub release update checks. Empty = use build metadata only.</summary>
    public string? GitHubRepository { get; set; }

    public HotKeyConfig HotKey { get; set; } = new();

    public List<ExclusionEntry> ExcludedApps { get; set; } = [];

    public List<ExclusionEntry> ExcludedPorts { get; set; } = [];
}
