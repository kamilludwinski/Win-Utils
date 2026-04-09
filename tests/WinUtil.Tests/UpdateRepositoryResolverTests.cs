using WinUtil.Services;
using Xunit;

namespace WinUtil.Tests;

public sealed class UpdateRepositoryResolverTests
{
    [Theory]
    [InlineData("https://github.com/kamilludwinski/WinUtil", "kamilludwinski/WinUtil")]
    [InlineData("https://www.github.com/foo/bar/", "foo/bar")]
    [InlineData("kamilludwinski/winutil", "kamilludwinski/winutil")]
    [InlineData("  org/repo  ", "org/repo")]
    public void NormalizeToOwnerRepo_accepts_urls_and_owner_slash_repo(string input, string expected) =>
        Assert.Equal(expected, UpdateRepositoryResolver.NormalizeToOwnerRepo(input));

    [Fact]
    public void ParseOwnerRepo_splits_normalized_pair()
    {
        var (owner, repo) = UpdateRepositoryResolver.ParseOwnerRepo("https://github.com/kamilludwinski/WinUtil");
        Assert.Equal("kamilludwinski", owner);
        Assert.Equal("WinUtil", repo);
    }
}
