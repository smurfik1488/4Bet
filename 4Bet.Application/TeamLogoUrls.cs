namespace _4Bet.Application.Services;

/// <summary>
/// Rewrites external team logo URLs to same-origin proxy paths so the browser
/// does not depend on third-party hotlink / referrer policies.
/// </summary>
public static class TeamLogoUrls
{
    public const string ProxyRelativePath = "/api/Sport/team-logo";

    public static string? WrapForBrowserProxy(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return trimmed;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return trimmed;
        }

        if (!IsAllowedLogoHost(uri.Host))
        {
            return trimmed;
        }

        return $"{ProxyRelativePath}?u={Uri.EscapeDataString(trimmed)}";
    }

    public static bool IsAllowedLogoHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var h = host.Trim().ToLowerInvariant();
        return h is "media.api-sports.io" or "www.thesportsdb.com" or "thesportsdb.com"
               || h.EndsWith(".thesportsdb.com", StringComparison.Ordinal)
               || h.EndsWith(".api-sports.io", StringComparison.Ordinal);
    }
}
