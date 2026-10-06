namespace ITBees.DeepL.Services;

/// <summary>
/// DeepL API addresses and the allow-list every request goes through. A Free plan key (":fx" suffix) works
/// only on api-free.deepl.com and a Pro key only on api.deepl.com - the wrong pairing is answered with 403.
/// Only these two addresses (plus proxies the host lists in <see cref="DeepLApiOptions"/>) are ever called:
/// the address comes from a form in the admin panel, so without the allow-list an operator could point the
/// server at internal hosts (SSRF) or send the key over plain http.
/// </summary>
public static class DeepLApiUrl
{
    public const string FreeApiUrl = "https://api-free.deepl.com";
    public const string ProApiUrl = "https://api.deepl.com";

    public static bool IsFreeApiKey(string? apiKey)
    {
        return apiKey?.Trim().EndsWith(":fx", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// An explicit address wins (validated by <see cref="EnsureAllowed"/>); otherwise the address follows the key.
    /// </summary>
    /// <exception cref="DeepLApiException">ErrorCode "invalid_base_url" when the explicit address is not allowed.</exception>
    public static string Resolve(string? apiKey, string? baseUrl, DeepLApiOptions? options = null)
    {
        var url = baseUrl?.Trim() ?? "";
        if (url == "")
        {
            return IsFreeApiKey(apiKey) ? FreeApiUrl : ProApiUrl;
        }

        return EnsureAllowed(url, options);
    }

    /// <summary>
    /// Validates an API address: an absolute https URL without user info, query or fragment that equals one of
    /// the DeepL endpoints or a host-configured proxy. Returns the normalized address (lower-case scheme and
    /// host, default port dropped, no trailing slash).
    /// </summary>
    /// <exception cref="DeepLApiException">ErrorCode "invalid_base_url".</exception>
    public static string EnsureAllowed(string baseUrl, DeepLApiOptions? options = null)
    {
        if (!TryNormalize(baseUrl, out var normalized))
        {
            throw new DeepLApiException("invalid_base_url",
                "Adres API DeepL musi być pełnym adresem https (np. https://api.deepl.com) bez parametrów, " +
                "fragmentu i danych logowania.", 0);
        }

        var allowed = AllowedBaseUrls(options);
        if (!allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new DeepLApiException("invalid_base_url",
                $"Adres API „{normalized}” nie jest dozwolony. Dopuszczalne adresy: {string.Join(", ", allowed)}.", 0);
        }

        return normalized;
    }

    public static bool IsAllowed(string baseUrl, DeepLApiOptions? options = null)
    {
        return TryNormalize(baseUrl, out var normalized)
               && AllowedBaseUrls(options).Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The official endpoints followed by the host-configured proxies (normalized; invalid entries are skipped).</summary>
    public static IReadOnlyList<string> AllowedBaseUrls(DeepLApiOptions? options = null)
    {
        var allowed = new List<string> { ProApiUrl, FreeApiUrl };
        foreach (var extra in options?.AdditionalAllowedBaseUrls ?? Enumerable.Empty<string>())
        {
            if (TryNormalize(extra, out var normalized) && !allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                allowed.Add(normalized);
            }
        }

        return allowed;
    }

    /// <summary>"Free" / "Pro" for the official addresses, null for anything else (a proxy).</summary>
    public static string? PlanFor(string apiUrl)
    {
        var url = apiUrl.Trim().TrimEnd('/');
        if (url.Equals(FreeApiUrl, StringComparison.OrdinalIgnoreCase)) return "Free";
        if (url.Equals(ProApiUrl, StringComparison.OrdinalIgnoreCase)) return "Pro";
        return null;
    }

    private static bool TryNormalize(string? baseUrl, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        // https only (the key travels in a header), and nothing that could smuggle the appended "/v2/..." path
        // somewhere else: "https://host/x#" would put the path into the fragment, "user@" into user info.
        if (uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return true;
    }
}
