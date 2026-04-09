using System.Text;
using System.Windows.Input;

namespace WinUtil.Helpers;

public static class HotkeyCaptureHelper
{
    public static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;

    /// <summary>Maps WPF modifiers to RegisterHotKey fsModifiers (ALT=1, CTRL=2, SHIFT=4, WIN=8).</summary>
    public static uint ToRegisterHotKeyModifiers(ModifierKeys mk)
    {
        uint f = 0;
        if (mk.HasFlag(ModifierKeys.Control))
            f |= 0x0002;
        if (mk.HasFlag(ModifierKeys.Shift))
            f |= 0x0004;
        if (mk.HasFlag(ModifierKeys.Alt))
            f |= 0x0001;
        if (mk.HasFlag(ModifierKeys.Windows))
            f |= 0x0008;
        return f;
    }

    public static string FormatPartialChord(ModifierKeys mk)
    {
        var parts = new List<string>();
        if (mk.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (mk.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (mk.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (mk.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");
        if (parts.Count == 0)
            return "…";
        return string.Join(" + ", parts) + " + …";
    }

    public static string FormatDisplay(ModifierKeys mk, Key key)
    {
        var parts = new List<string>();
        if (mk.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (mk.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (mk.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (mk.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");
        parts.Add(KeyToDisplayName(key));
        return string.Join(" + ", parts);
    }

    private static string KeyToDisplayName(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
            return ((char)('0' + (key - Key.D0))).ToString();
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return ((char)('0' + (key - Key.NumPad0))).ToString();
        if (key >= Key.A && key <= Key.Z)
            return key.ToString();
        return key switch
        {
            Key.OemMinus => "-",
            Key.OemPlus => "+",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Back => "Backspace",
            Key.Tab => "Tab",
            _ => key.ToString(),
        };
    }
}
