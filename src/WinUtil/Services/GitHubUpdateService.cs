using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Serilog;

namespace WinUtil.Services;

public sealed class GitHubLatestReleaseInfo
{
    public required string TagName { get; init; }
    public required string ReleasePageUrl { get; init; }
    public string? SetupDownloadUrl { get; init; }
}

public sealed class GitHubUpdateService
{
    private static readonly HttpClient Http = CreateClient();
    private static readonly HttpClient DownloadHttp = CreateDownloadClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WinUtil", "1.0"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    private static HttpClient CreateDownloadClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WinUtil", "1.0"));
        return c;
    }

    /// <summary>Downloads the release installer to <paramref name="destinationPath"/>; returns false on failure.</summary>
    public async Task<bool> DownloadSetupToFileAsync(string browserDownloadUrl, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await DownloadHttp.GetAsync(browserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Setup download returned {Status} for {Url}", response.StatusCode, browserDownloadUrl);
                return false;
            }

            await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "DownloadSetupToFileAsync failed for {Url}", browserDownloadUrl);
            return false;
        }
    }

    /// <summary>Fetches the latest release; returns null on failure.</summary>
    public async Task<GitHubLatestReleaseInfo?> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/releases/latest";
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("GitHub releases/latest returned {StatusCode} for {Url}", response.StatusCode, url);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = doc.RootElement;

        var tag = root.GetProperty("tag_name").GetString();
        var html = root.GetProperty("html_url").GetString();
        if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(html))
            return null;

        string? setupUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var a in assets.EnumerateArray())
            {
                if (!a.TryGetProperty("name", out var nameEl))
                    continue;
                var name = nameEl.GetString();
                if (name is null || !a.TryGetProperty("browser_download_url", out var dlEl))
                    continue;
                var dl = dlEl.GetString();
                if (dl is null)
                    continue;
                if (name.Equals("WinUtilSetup.exe", StringComparison.OrdinalIgnoreCase))
                {
                    setupUrl = dl;
                    break;
                }
            }
        }

        var downloadUrl = setupUrl;
        if (downloadUrl is not null)
            Log.Debug("Release asset URL resolved: {Url}", downloadUrl);

        return new GitHubLatestReleaseInfo
        {
            TagName = tag,
            ReleasePageUrl = html,
            SetupDownloadUrl = downloadUrl,
        };
    }

    /// <summary>True if the latest GitHub tag is newer than <paramref name="currentVersion"/> (no leading v).</summary>
    public static bool IsNewerThanCurrent(string currentVersion, string remoteTagName) =>
        VersionComparer.IsRemoteNewer(currentVersion, remoteTagName);
}
