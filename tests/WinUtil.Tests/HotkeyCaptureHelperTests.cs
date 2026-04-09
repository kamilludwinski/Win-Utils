using System.Windows.Input;
using WinUtil.Helpers;
using Xunit;

namespace WinUtil.Tests;

public sealed class HotkeyCaptureHelperTests
{
    [Fact]
    public void ToRegisterHotKeyModifiers_Ctrl_only()
    {
        Assert.Equal(2u, HotkeyCaptureHelper.ToRegisterHotKeyModifiers(ModifierKeys.Control));
    }

    [Fact]
    public void ToRegisterHotKeyModifiers_CtrlShift()
    {
        var mk = ModifierKeys.Control | ModifierKeys.Shift;
        Assert.Equal(6u, HotkeyCaptureHelper.ToRegisterHotKeyModifiers(mk));
    }

    [Fact]
    public void ToRegisterHotKeyModifiers_Win()
    {
        Assert.Equal(8u, HotkeyCaptureHelper.ToRegisterHotKeyModifiers(ModifierKeys.Windows));
    }

    [Fact]
    public void FormatDisplay_includes_modifiers_and_key()
    {
        var s = HotkeyCaptureHelper.FormatDisplay(ModifierKeys.Control, Key.W);
        Assert.Equal("Ctrl + W", s);
    }

    [Fact]
    public void FormatPartialChord_empty_modifiers()
    {
        Assert.Equal("…", HotkeyCaptureHelper.FormatPartialChord(ModifierKeys.None));
    }

    [Theory]
    [InlineData(Key.LeftCtrl, true)]
    [InlineData(Key.RWin, true)]
    [InlineData(Key.W, false)]
    [InlineData(Key.D5, false)]
    public void IsModifierKey(Key key, bool expected)
    {
        Assert.Equal(expected, HotkeyCaptureHelper.IsModifierKey(key));
    }
}
