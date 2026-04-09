using WinUtil.ViewModels;
using Xunit;

namespace WinUtil.Tests;

public sealed class ProcessGroupViewModelTests
{
    [Fact]
    public void PidsDisplay_formats_bracketed_list()
    {
        var vm = new ProcessGroupViewModel("notepad", [4, 12, 99]);
        Assert.Equal("notepad", vm.Name);
        Assert.Equal("[4, 12, 99]", vm.PidsDisplay);
        Assert.Equal(3, vm.Pids.Count);
    }
}
