namespace ITBees.DeepL.Services;

/// <summary>
/// DeepL API addresses. A Free plan key (":fx" suffix) works only on api-free.deepl.com and a Pro key only on
/// api.deepl.com - the wrong pairing is answered with 403.
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
    /// An explicit address (trimmed, without the trailing slash) wins; otherwise the address follows the key.
    /// </summary>
    public static string Resolve(string? apiKey, string? baseUrl)
    {
        var url = baseUrl?.Trim().TrimEnd('/') ?? "";
        if (url != "")
        {
            return url;
        }

        return IsFreeApiKey(apiKey) ? FreeApiUrl : ProApiUrl;
    }

    /// <summary>"Free" / "Pro" for the official addresses, null for anything else (proxy, test server).</summary>
    public static string? PlanFor(string apiUrl)
    {
        var url = apiUrl.Trim().TrimEnd('/');
        if (url.Equals(FreeApiUrl, StringComparison.OrdinalIgnoreCase)) return "Free";
        if (url.Equals(ProApiUrl, StringComparison.OrdinalIgnoreCase)) return "Pro";
        return null;
    }
}
