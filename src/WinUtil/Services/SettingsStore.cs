using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;

namespace WinUtil.Services;

/// <summary>Persists <see cref="AppSettings"/> to <c>Assets/settings.json</c>.</summary>
public sealed class SettingsStore
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string FilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "Assets", FileName);

    public AppSettings Load()
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);

        if (!File.Exists(FilePath))
            TryMigrateFromLegacyFiles();

        if (!File.Exists(FilePath))
        {
            var fresh = new AppSettings();
            Normalize(fresh);
            Save(fresh);
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (s is null)
                s = new AppSettings();
            Normalize(s);
            Log.Debug("Settings loaded from {Path}", FilePath);
            return s;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read settings from {Path}; using defaults", FilePath);
            var fallback = new AppSettings();
            Normalize(fallback);
            return fallback;
        }
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(FilePath, json);
        Log.Debug("Settings saved to {Path}", FilePath);
    }

    public IReadOnlyList<string> LoadExcludedAppNames() =>
        Load().ExcludedApps
            .Select(e => e.Name.Trim())
            .Where(n => n.Length > 0)
            .ToList();

    public IReadOnlyList<string> LoadExcludedPortNames() =>
        Load().ExcludedPorts
            .Select(e => e.Name.Trim())
            .Where(n => n.Length > 0)
            .ToList();

    private static void Normalize(AppSettings s)
    {
        s.HotKey ??= new HotKeyConfig();
        s.ExcludedApps ??= [];
        s.ExcludedPorts ??= [];
        s.GitHubRepository = string.IsNullOrWhiteSpace(s.GitHubRepository) ? null : s.GitHubRepository.Trim();

        if (s.HotKey.VirtualKey == 0 || s.HotKey.VirtualKey > 0xFF)
            s.HotKey.VirtualKey = 0x57;
    }

    private void TryMigrateFromLegacyFiles()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);

            var settings = new AppSettings();
            var migrated = false;

            var legacyHotKey = Path.Combine(dir, "hotkey.json");
            if (File.Exists(legacyHotKey))
            {
                migrated = true;
                try
                {
                    var json = File.ReadAllText(legacyHotKey);
                    var hk = JsonSerializer.Deserialize<HotKeyConfig>(json, JsonOptions);
                    if (hk is not null)
                        settings.HotKey = hk;
                }
                catch
                {
                    // ignored
                }
            }

            var appsCsv = Path.Combine(dir, "excluded-apps.csv");
            if (File.Exists(appsCsv))
            {
                migrated = true;
                var lines = File.ReadAllLines(appsCsv);
                settings.ExcludedApps = ExclusionCsvParser.LoadExclusionEntries(lines);
            }

            var portsCsv = Path.Combine(dir, "excluded-ports.csv");
            if (File.Exists(portsCsv))
            {
                migrated = true;
                var lines = File.ReadAllLines(portsCsv);
                settings.ExcludedPorts = ExclusionCsvParser.LoadExclusionEntries(lines);
            }

            var legacyExclusions = Path.Combine(dir, "exclusions.txt");
            if (settings.ExcludedApps.Count == 0 && File.Exists(legacyExclusions))
            {
                migrated = true;
                var lines = File.ReadAllLines(legacyExclusions);
                var names = ExclusionCsvParser.LoadLegacyCommentedLines(lines);
                settings.ExcludedApps = names
                    .Select(n => new ExclusionEntry { Name = n, Description = string.Empty })
                    .ToList();
            }

            var legacyAppData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinUtil",
                "exclusions.txt");
            if (settings.ExcludedApps.Count == 0 && File.Exists(legacyAppData))
            {
                migrated = true;
                var lines = File.ReadAllLines(legacyAppData);
                var names = ExclusionCsvParser.LoadLegacyCommentedLines(lines);
                settings.ExcludedApps = names
                    .Select(n => new ExclusionEntry { Name = n, Description = string.Empty })
                    .ToList();
            }

            if (!migrated)
                return;

            Log.Information("Migrating legacy settings into {Path}", FilePath);
            Normalize(settings);
            Save(settings);

            TryDelete(Path.Combine(dir, "hotkey.json"));
            TryDelete(Path.Combine(dir, "excluded-apps.csv"));
            TryDelete(Path.Combine(dir, "excluded-ports.csv"));
            TryDelete(Path.Combine(dir, "excluded-apps.txt"));
            TryDelete(Path.Combine(dir, "excluded-ports.txt"));
        }
        catch
        {
            // ignored
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}
