using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using Serilog;
using WinUtil.Native;
using WinUtil.Services;
using WinUtil.SystemTray;
using WinUtil.ViewModels;

namespace WinUtil;

public partial class MainWindow : Window
{
    private const int HotKeyId = 9001;

    private readonly DispatcherTimer _processRefreshTimer;
    private readonly DispatcherTimer _portsRefreshTimer;

    /// <summary>While the window is visible (not in tray), poll for updates; 0.5s tick, GitHub throttled in the view model.</summary>
    private readonly DispatcherTimer _updatePollTimer;

    private NotifyIcon? _notifyIcon;
    private System.Drawing.Icon? _trayIcon;
    private bool _disposeTrayIcon;
    private bool _exitRequested;
    private bool _firstActivation = true;
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private DateTime _hotKeyConfigLastWriteUtc;

    public MainWindow()
    {
        InitializeComponent();

        _processRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _processRefreshTimer.Tick += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
                return;
            if (!IsActive || MainTabs.SelectedIndex != 0)
                return;
            vm.LoadProcesses();
        };

        _portsRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _portsRefreshTimer.Tick += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
                return;
            if (!IsActive || MainTabs.SelectedIndex != 1)
                return;
            vm.LoadPorts();
        };

        _updatePollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _updatePollTimer.Tick += OnUpdatePollTick;

        Loaded += OnLoaded;
        Activated += OnWindowActivated;
        Deactivated += (_, _) => UpdateRefreshTimers();
        SourceInitialized += OnSourceInitialized;
        Closing += OnWindowClosing;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private async void OnUpdatePollTick(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm || !IsVisible)
            return;

        try
        {
            await vm.CheckForUpdatesAsync(showCheckingIndicator: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Update poll tick failed");
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("MainWindow loaded");
        UpdateRefreshTimers();
        UpdateUpdatePollTimer();
        if (DataContext is MainViewModel vm)
            await vm.CheckForUpdatesAsync();
    }

    private void MainTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainTabs))
            return;

        UpdateRefreshTimers();

        if (DataContext is MainViewModel vm && MainTabs.SelectedIndex == 2)
            vm.RefreshSettingsCounts();
    }

    private void UpdateRefreshTimers()
    {
        var idx = MainTabs.SelectedIndex;
        var active = IsActive;
        _processRefreshTimer.IsEnabled = active && idx == 0;
        _portsRefreshTimer.IsEnabled = active && idx == 1;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (_firstActivation)
                _firstActivation = false;
            else
                vm.RefreshAllDataFromDisk();
        }

        TryReloadHotKeyIfChanged();
        UpdateRefreshTimers();
        Log.Debug("Window activated");
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ShowInTaskbar = IsVisible;
        UpdateUpdatePollTimer();
    }

    private void UpdateUpdatePollTimer()
    {
        _updatePollTimer.IsEnabled = IsVisible;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hwnd = helper.EnsureHandle();
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);

        var tray = LoadTrayIcon();
        _trayIcon = tray.Icon;
        _disposeTrayIcon = tray.DisposeWhenDone;
        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Visible = true,
            Text = "WinUtil",
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        _notifyIcon.ContextMenuStrip = TrayMenuBuilder.Create(ShowFromTray, RequestExit);

        RegisterHotKeyFromConfig();
    }

    /// <summary>Tray icon: embedded exe icon, else Assets/app.ico, else system default.</summary>
    private static (System.Drawing.Icon Icon, bool DisposeWhenDone) LoadTrayIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                if (icon is not null)
                    return (icon, true);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "ExtractAssociatedIcon failed for tray");
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(path))
                return (new System.Drawing.Icon(path), true);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Loading Assets/app.ico for tray failed");
        }

        return (System.Drawing.SystemIcons.Application, false);
    }

    public void ReregisterGlobalHotKey() => RegisterHotKeyFromConfig();

    private void RegisterHotKeyFromConfig()
    {
        if (_hwnd == IntPtr.Zero)
            return;

        User32HotKey.UnregisterHotKey(_hwnd, HotKeyId);

        var cfg = new SettingsStore().Load().HotKey;
        if (File.Exists(SettingsStore.FilePath))
            _hotKeyConfigLastWriteUtc = File.GetLastWriteTimeUtc(SettingsStore.FilePath);

        if (User32HotKey.RegisterHotKey(_hwnd, HotKeyId, cfg.Modifiers, cfg.VirtualKey))
        {
            Log.Information("Global hotkey registered: modifiers={Modifiers}, virtualKey={VirtualKey}", cfg.Modifiers, cfg.VirtualKey);
            return;
        }

        var err = Marshal.GetLastWin32Error();
        Log.Warning("RegisterHotKey failed with Win32 error {Error}", err);
        _notifyIcon?.ShowBalloonTip(
            5000,
            "WinUtil — hotkey not registered",
            $"RegisterHotKey failed (error {err}). Another app may own that shortcut. Edit Assets/settings.json (hotKey) — e.g. modifiers 10 and virtualKey 87 for Ctrl+Shift+W, then save and focus WinUtil again.",
            ToolTipIcon.Warning);
    }

    private void TryReloadHotKeyIfChanged()
    {
        if (_hwnd == IntPtr.Zero || !File.Exists(SettingsStore.FilePath))
            return;

        var t = File.GetLastWriteTimeUtc(SettingsStore.FilePath);
        if (t == _hotKeyConfigLastWriteUtc)
            return;

        RegisterHotKeyFromConfig();
    }

    private void RequestExit()
    {
        Log.Information("Exit requested from tray");
        _exitRequested = true;
        Close();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32HotKey.WmHotkey && wParam.ToInt32() == HotKeyId)
        {
            handled = true;
            Dispatcher.Invoke(OnGlobalHotKeyPressed);
        }

        return IntPtr.Zero;
    }

    /// <summary>Hotkey: hide to tray when the window is up; otherwise show/restore (same as tray “Open”).</summary>
    private void OnGlobalHotKeyPressed()
    {
        Log.Debug("Global hotkey pressed");
        if (IsVisible && WindowState != WindowState.Minimized)
        {
            Hide();
            return;
        }

        ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        ShowInTaskbar = true;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_exitRequested)
            return;

        e.Cancel = true;
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _processRefreshTimer.IsEnabled = false;
        _portsRefreshTimer.IsEnabled = false;
        _updatePollTimer.IsEnabled = false;

        if (_hwnd != IntPtr.Zero)
        {
            User32HotKey.UnregisterHotKey(_hwnd, HotKeyId);
            _hwnd = IntPtr.Zero;
        }

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;

        if (_notifyIcon != null)
        {
            var strip = _notifyIcon.ContextMenuStrip;
            _notifyIcon.ContextMenuStrip = null;
            strip?.Dispose();
            _notifyIcon.Icon = null;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_disposeTrayIcon)
            _trayIcon?.Dispose();
        _trayIcon = null;

        Log.Debug("MainWindow closed");
        base.OnClosed(e);
    }
}
