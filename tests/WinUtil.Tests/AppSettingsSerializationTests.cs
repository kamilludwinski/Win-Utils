using System.Text.Json;
using WinUtil.Services;
using Xunit;

namespace WinUtil.Tests;

public sealed class AppSettingsSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void AppSettings_round_trips_json()
    {
        var original = new AppSettings
        {
            GitHubRepository = "acme/winutil",
            HotKey = new HotKeyConfig { Modifiers = 2, VirtualKey = 87 },
            ExcludedApps =
            [
                new ExclusionEntry { Name = "foo", Description = "bar" },
            ],
            ExcludedPorts =
            [
                new ExclusionEntry { Name = "(system)", Description = "kernel" },
            ],
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var back = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        Assert.NotNull(back);
        Assert.Equal("acme/winutil", back!.GitHubRepository);
        Assert.Equal(2u, back.HotKey.Modifiers);
        Assert.Equal(87u, back.HotKey.VirtualKey);
        Assert.Single(back.ExcludedApps);
        Assert.Equal("foo", back.ExcludedApps[0].Name);
        Assert.Equal("bar", back.ExcludedApps[0].Description);
        Assert.Single(back.ExcludedPorts);
        Assert.Equal("(system)", back.ExcludedPorts[0].Name);
    }
}
