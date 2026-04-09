using System.Reflection;

namespace WinUtil.Services;

/// <summary>Resolves <c>owner/repo</c> from settings.json or build-time assembly metadata.</summary>
public static class UpdateRepositoryResolver
{
    /// <summary>Returns <c>owner/repo</c> or null if not configured.</summary>
    public static string? Resolve(AppSettings settings)
    {
        var fromSettings = NormalizeToOwnerRepo(settings.GitHubRepository);
        if (!string.IsNullOrEmpty(fromSettings))
            return fromSettings;

        var fromAsm = NormalizeToOwnerRepo(
            Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "GitHubRepository")?.Value);

        return string.IsNullOrEmpty(fromAsm) ? null : fromAsm;
    }

    /// <summary>
    /// Accepts <c>owner/repo</c> or a GitHub URL (<c>https://github.com/owner/repo</c>).
    /// Returns normalized <c>owner/repo</c> or null.
    /// </summary>
    public static string? NormalizeToOwnerRepo(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var t = raw.Trim();

        if (Uri.TryCreate(t, UriKind.Absolute, out var uri) &&
            (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase)))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
                return $"{segments[0]}/{segments[1]}";
            return null;
        }

        return TryParsePlainOwnerRepo(t);
    }

    private static string? TryParsePlainOwnerRepo(string t)
    {
        var i = t.IndexOf('/');
        if (i <= 0 || i >= t.Length - 1)
            return null;

        var owner = t[..i].Trim();
        var repo = t[(i + 1)..].Trim();
        if (owner.Length == 0 || repo.Length == 0)
            return null;

        // Drop trailing path segments (e.g. owner/repo/issues)
        var repoSlash = repo.IndexOf('/');
        if (repoSlash > 0)
            repo = repo[..repoSlash].Trim();

        return $"{owner}/{repo}";
    }

    public static (string? Owner, string? Repo) ParseOwnerRepo(string ownerRepo)
    {
        var normalized = NormalizeToOwnerRepo(ownerRepo);
        if (string.IsNullOrEmpty(normalized))
            return (null, null);

        var i = normalized.IndexOf('/');
        var owner = normalized[..i];
        var repo = normalized[(i + 1)..];
        return (owner, repo);
    }
}
