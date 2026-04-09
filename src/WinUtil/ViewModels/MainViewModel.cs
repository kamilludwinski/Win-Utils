using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinUtil;
using WinUtil.Services;

namespace WinUtil.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SettingsStore _settings = new();

    [ObservableProperty]
    private string _pidFilter = string.Empty;

    [ObservableProperty]
    private string _processAppNameFilter = string.Empty;

    [ObservableProperty]
    private string _portFilter = string.Empty;

    [ObservableProperty]
    private string _portAppNameFilter = string.Empty;

    [ObservableProperty]
    private ProcessGroupViewModel? _selectedProcess;

    [ObservableProperty]
    private PortEntry? _selectedPort;

    [ObservableProperty]
    private int _excludedAppsCount;

    [ObservableProperty]
    private int _excludedPortsCount;

    public ObservableCollection<ProcessGroupViewModel> ProcessRows { get; } = [];

    public ObservableCollection<PortEntry> PortRows { get; } = [];

    /// <summary>Folder containing the executable, settings, and logs.</summary>
    public string AppDirectoryPath =>
        Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

    public MainViewModel()
    {
        Log.Debug("MainViewModel constructed");
        LoadProcesses();
        LoadPorts();
        RefreshSettingsCounts();
    }

    /// <summary>Reload data from disk after editing settings.json or on window activation.</summary>
    public void RefreshAllDataFromDisk()
    {
        Log.Debug("RefreshAllDataFromDisk");
        LoadProcesses();
        LoadPorts();
        RefreshSettingsCounts();
    }

    /// <summary>Legacy name used by window activation — refreshes processes, ports, and counts.</summary>
    public void ReloadProcessesFromFile() => RefreshAllDataFromDisk();

    public void RefreshSettingsCounts()
    {
        var s = _settings.Load();
        ExcludedAppsCount = s.ExcludedApps.Count(e => e.Name.Trim().Length > 0);
        ExcludedPortsCount = s.ExcludedPorts.Count(e => e.Name.Trim().Length > 0);
    }

    partial void OnPidFilterChanged(string value) => ApplyProcessFilter();

    partial void OnProcessAppNameFilterChanged(string value) => ApplyProcessFilter();

    partial void OnPortFilterChanged(string value) => ApplyPortFilter();

    partial void OnPortAppNameFilterChanged(string value) => ApplyPortFilter();

    [RelayCommand]
    private void ClearPidFilter() => PidFilter = string.Empty;

    [RelayCommand]
    private void ClearProcessAppNameFilter() => ProcessAppNameFilter = string.Empty;

    [RelayCommand]
    private void ClearPortFilter() => PortFilter = string.Empty;

    [RelayCommand]
    private void ClearPortAppNameFilter() => PortAppNameFilter = string.Empty;

    private List<ProcessGroupViewModel> _allProcessRows = [];
    private List<PortEntry> _allPortRows = [];

    public void LoadProcesses()
    {
        var excluded = _settings.LoadExcludedAppNames();
        var groups = ProcessEnumerator.GroupProcesses(excluded);
        _allProcessRows = groups
            .Select(g => new ProcessGroupViewModel(g.Name, g.Pids))
            .ToList();

        Log.Debug("LoadProcesses: {Count} groups (after exclusions)", _allProcessRows.Count);
        ApplyProcessFilter();
    }

    private void ApplyProcessFilter()
    {
        var prevName = SelectedProcess?.Name;

        var pidPart = PidFilter.Trim();
        var appPart = ProcessAppNameFilter.Trim();
        IEnumerable<ProcessGroupViewModel> query = _allProcessRows;

        if (pidPart.Length > 0)
        {
            query = query.Where(g =>
                g.Pids.Any(pid => pid.ToString().Contains(pidPart, StringComparison.Ordinal)));
        }

        if (appPart.Length > 0)
        {
            query = query.Where(g =>
                g.Name.Contains(appPart, StringComparison.OrdinalIgnoreCase));
        }

        ProcessRows.Clear();
        foreach (var row in query)
            ProcessRows.Add(row);

        if (prevName is not null)
        {
            var found = ProcessRows.FirstOrDefault(r => r.Name == prevName);
            if (found is not null)
                SelectedProcess = found;
        }
    }

    public void LoadPorts()
    {
        var hiddenApps = new HashSet<string>(_settings.LoadExcludedPortNames(), StringComparer.OrdinalIgnoreCase);
        var raw = PortEnumerator.GetOpenPorts();
        _allPortRows = raw
            .Where(p => !hiddenApps.Contains(p.AppName))
            .ToList();

        Log.Debug("LoadPorts: {Count} rows (after exclusions)", _allPortRows.Count);
        ApplyPortFilter();
    }

    private void ApplyPortFilter()
    {
        var prev = SelectedPort;

        var portPart = PortFilter.Trim();
        var appPart = PortAppNameFilter.Trim();
        IEnumerable<PortEntry> query = _allPortRows;

        if (portPart.Length > 0)
        {
            query = query.Where(p =>
                p.Port.ToString().Contains(portPart, StringComparison.Ordinal));
        }

        if (appPart.Length > 0)
        {
            query = query.Where(p =>
                p.AppName.Contains(appPart, StringComparison.OrdinalIgnoreCase));
        }

        PortRows.Clear();
        foreach (var row in query)
            PortRows.Add(row);

        if (prev is not null)
        {
            var found = PortRows.FirstOrDefault(r => r == prev);
            if (found is not null)
                SelectedPort = found;
        }
    }

    [RelayCommand]
    private void OpenAppDirectory()
    {
        var dir = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        try
        {
            Log.Information("Opening app directory in Explorer: {Dir}", dir);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenAppDirectory failed");
            System.Windows.MessageBox.Show(
                ex.Message,
                "WinUtil",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void EditExcludedApps()
    {
        try
        {
            _ = _settings.Load();
            Process.Start(new ProcessStartInfo
            {
                FileName = SettingsStore.FilePath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EditExcludedApps failed");
            System.Windows.MessageBox.Show(
                ex.Message,
                "WinUtil",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void EditExcludedPorts()
    {
        try
        {
            _ = _settings.Load();
            Process.Start(new ProcessStartInfo
            {
                FileName = SettingsStore.FilePath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EditExcludedPorts failed");
            System.Windows.MessageBox.Show(
                ex.Message,
                "WinUtil",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void EditHotKeyConfig()
    {
        try
        {
            var dlg = new HotkeyConfigWindow
            {
                Owner = System.Windows.Application.Current.MainWindow,
            };
            if (dlg.ShowDialog() == true
                && System.Windows.Application.Current.MainWindow is MainWindow mw)
                mw.ReregisterGlobalHotKey();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EditHotKeyConfig failed");
            System.Windows.MessageBox.Show(
                ex.Message,
                "WinUtil",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void KillProcess(ProcessGroupViewModel? row)
    {
        row ??= SelectedProcess;
        if (row is null)
            return;

        Log.Information("KillProcess: name={Name}, pids={Pids}", row.Name, string.Join(",", row.Pids));
        foreach (var pid in row.Pids)
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                p.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "KillProcess failed for PID {Pid}", pid);
            }
        }

        LoadProcesses();
    }

    [RelayCommand]
    private void KillPortProcess(PortEntry? row)
    {
        if (row is null || row.Pid <= 0)
            return;

        Log.Information(
            "KillPortProcess: app={App}, pid={Pid}, port={Port}",
            row.AppName,
            row.Pid,
            row.Port);
        try
        {
            using var p = Process.GetProcessById(row.Pid);
            p.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "KillPortProcess failed for PID {Pid}", row.Pid);
        }

        LoadPorts();
        LoadProcesses();
    }

    private readonly SemaphoreSlim _updateCheckGate = new(1, 1);

    /// <summary>GitHub unauthenticated API allows ~60 requests/hour; do not call more often when polling.</summary>
    private static readonly TimeSpan MinIntervalBetweenPollRequests = TimeSpan.FromSeconds(60);

    private DateTimeOffset _lastPollRequestUtc = DateTimeOffset.MinValue;

    private const string ReleaseNotesUrlTemplate = "https://www.kamilludwinski.net/projects/winutil?version=";

    /// <summary>Status line next to the version label (not including the version itself).</summary>
    [ObservableProperty]
    private string _versionStatusText = "";

    [ObservableProperty]
    private bool _showUpdateDownloadButton;

    [ObservableProperty]
    private string? _updateDownloadUrl;

    private string? _updateReleasePageUrl;

    /// <summary>Direct <c>WinUtilSetup.exe</c> asset URL when present; otherwise update must open the release page.</summary>
    private string? _setupInstallerAssetUrl;

    [ObservableProperty]
    private bool _isUpdateDownloading;

    partial void OnIsUpdateDownloadingChanged(bool value) =>
        OpenUpdateDownloadCommand.NotifyCanExecuteChanged();

    /// <param name="showCheckingIndicator">When false (background poll), do not flash &quot;Checking…&quot;; GitHub calls are throttled to about once per minute.</param>
    public async Task CheckForUpdatesAsync(bool showCheckingIndicator = true)
    {
        if (!showCheckingIndicator)
        {
            if (DateTimeOffset.UtcNow - _lastPollRequestUtc < MinIntervalBetweenPollRequests)
                return;
        }

        if (!await _updateCheckGate.WaitAsync(0).ConfigureAwait(true))
            return;

        try
        {
            if (showCheckingIndicator)
            {
                Log.Information("CheckForUpdatesAsync started");
                VersionStatusText = "Checking for updates…";
                ShowUpdateDownloadButton = false;
                UpdateDownloadUrl = null;
                _updateReleasePageUrl = null;
                _setupInstallerAssetUrl = null;
                IsUpdateDownloading = false;
            }
            else
                Log.Debug("CheckForUpdatesAsync (poll)");

            await RunUpdateCheckAsync().ConfigureAwait(true);
            _lastPollRequestUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CheckForUpdatesAsync failed");
            VersionStatusText = "Could not check for updates.";
            _lastPollRequestUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            _updateCheckGate.Release();
        }
    }

    private async Task RunUpdateCheckAsync()
    {
        var settings = _settings.Load();
        var repo = UpdateRepositoryResolver.Resolve(settings);
        if (string.IsNullOrEmpty(repo))
        {
            Log.Debug("Update check skipped: no repository configured");
            VersionStatusText = "Set gitHubRepository in settings (owner/repo) to check for updates.";
            return;
        }

        var pair = UpdateRepositoryResolver.ParseOwnerRepo(repo);
        if (pair.Owner is null || pair.Repo is null)
        {
            Log.Warning("Invalid gitHubRepository: {Repo}", repo);
            VersionStatusText = "Invalid gitHubRepository (use owner/repo).";
            return;
        }

        var svc = new GitHubUpdateService();
        var latest = await svc.GetLatestReleaseAsync(pair.Owner, pair.Repo).ConfigureAwait(true);
        if (latest is null)
        {
            Log.Warning("GitHub latest release returned null for {Owner}/{Repo}", pair.Owner, pair.Repo);
            VersionStatusText = "Could not reach GitHub releases.";
            return;
        }

        var current = AppVersionHelper.CurrentVersion;
        var remoteTag = latest.TagName;
        Log.Debug("Update compare: current={Current}, remoteTag={RemoteTag}", current, remoteTag);
        if (!GitHubUpdateService.IsNewerThanCurrent(current, remoteTag))
        {
            VersionStatusText = "This is the most recent release.";
            return;
        }

        var remoteLabel = remoteTag.TrimStart('v', 'V');
        Log.Information("Update available: {RemoteLabel}", remoteLabel);
        VersionStatusText = $"A newer version ({remoteLabel}) is available.";
        _updateReleasePageUrl = latest.ReleasePageUrl;
        _setupInstallerAssetUrl = latest.SetupDownloadUrl;
        UpdateDownloadUrl = latest.SetupDownloadUrl ?? latest.ReleasePageUrl;
        ShowUpdateDownloadButton = true;
    }

    private bool CanOpenUpdateDownload() => !IsUpdateDownloading;

    [RelayCommand(CanExecute = nameof(CanOpenUpdateDownload))]
    private async Task OpenUpdateDownloadAsync()
    {
        if (!string.IsNullOrEmpty(_setupInstallerAssetUrl))
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"WinUtilSetup-{Guid.NewGuid():N}.exe");
            try
            {
                IsUpdateDownloading = true;
                VersionStatusText = "Downloading update…";
                var svc = new GitHubUpdateService();
                var ok = await svc.DownloadSetupToFileAsync(_setupInstallerAssetUrl, tempPath).ConfigureAwait(true);
                if (!ok)
                {
                    VersionStatusText = "Could not download the installer. Try again or open the release page.";
                    return;
                }

                VersionStatusText = "Starting installer…";
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "OpenUpdateDownload failed");
                VersionStatusText = "Could not run the installer. Try again or open the release page.";
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup failure
                }
            }
            finally
            {
                IsUpdateDownloading = false;
            }

            return;
        }

        var url = UpdateDownloadUrl ?? _updateReleasePageUrl;
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenUpdateDownload failed");
        }
    }

    [RelayCommand]
    private void OpenReleaseNotes()
    {
        var url = ReleaseNotesUrlTemplate + Uri.EscapeDataString(AppVersionHelper.CurrentVersion);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenReleaseNotes failed");
        }
    }
}
