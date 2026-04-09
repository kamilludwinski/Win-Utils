using WinUtil.Services;
using Xunit;

namespace WinUtil.Tests;

public sealed class VersionComparerTests
{
    [Theory]
    [InlineData("1.0.0", "v1.0.1", true)]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.1", "v1.0.0", false)]
    [InlineData("2.0.0", "v1.9.9", false)]
    [InlineData("1.0.0", "v1.0.0", false)]
    [InlineData("1.0.0", "v1.0.0-rc1", false)]
    public void IsRemoteNewer_matches_expected(string current, string remote, bool expected) =>
        Assert.Equal(expected, VersionComparer.IsRemoteNewer(current, remote));
}
