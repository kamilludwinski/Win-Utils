using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Serilog;
using WinUtil.Helpers;
using WinUtil.Services;

namespace WinUtil;

public partial class HotkeyConfigWindow : Window
{
    private readonly SettingsStore _settingsStore = new();

    private uint _capturedModifiers;
    private uint _capturedVk;
    private bool _hasCapture;

    public HotkeyConfigWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var cfg = _settingsStore.Load().HotKey;
        try
        {
            var mk = ModifierKeys.None;
            if ((cfg.Modifiers & 0x0001) != 0)
                mk |= ModifierKeys.Alt;
            if ((cfg.Modifiers & 0x0002) != 0)
                mk |= ModifierKeys.Control;
            if ((cfg.Modifiers & 0x0004) != 0)
                mk |= ModifierKeys.Shift;
            if ((cfg.Modifiers & 0x0008) != 0)
                mk |= ModifierKeys.Windows;

            var wpfKey = KeyInterop.KeyFromVirtualKey((int)cfg.VirtualKey);
            HotkeyDisplayText.Text = HotkeyCaptureHelper.FormatDisplay(mk, wpfKey);
            _capturedModifiers = cfg.Modifiers;
            _capturedVk = cfg.VirtualKey;
            _hasCapture = true;
            OkButton.IsEnabled = true;
        }
        catch
        {
            HotkeyDisplayText.Text = "…";
        }

        _ = new WindowInteropHelper(this).EnsureHandle();
        Focus();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DialogResult = false;
            Close();
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (HotkeyCaptureHelper.IsModifierKey(key))
        {
            e.Handled = true;
            OkButton.IsEnabled = false;
            _hasCapture = false;
            Dispatcher.BeginInvoke(
                () =>
                {
                    var mk = Keyboard.Modifiers;
                    HotkeyDisplayText.Text = HotkeyCaptureHelper.FormatPartialChord(mk);
                },
                DispatcherPriority.Input);
            return;
        }

        e.Handled = true;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None)
        {
            HotkeyDisplayText.Text = "Hold at least one modifier (e.g. Win, Ctrl) with the key.";
            OkButton.IsEnabled = false;
            _hasCapture = false;
            return;
        }

        var fs = HotkeyCaptureHelper.ToRegisterHotKeyModifiers(mods);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0 || vk > 0xFF)
        {
            HotkeyDisplayText.Text = "Unsupported key.";
            OkButton.IsEnabled = false;
            _hasCapture = false;
            return;
        }

        _capturedModifiers = fs;
        _capturedVk = vk;
        _hasCapture = true;
        HotkeyDisplayText.Text = HotkeyCaptureHelper.FormatDisplay(mods, key);
        OkButton.IsEnabled = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasCapture)
            return;

        var settings = _settingsStore.Load();
        settings.HotKey = new HotKeyConfig
        {
            Modifiers = _capturedModifiers,
            VirtualKey = _capturedVk,
        };
        _settingsStore.Save(settings);
        Log.Information("Hotkey saved: modifiers={Modifiers}, virtualKey={VirtualKey}", _capturedModifiers, _capturedVk);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
