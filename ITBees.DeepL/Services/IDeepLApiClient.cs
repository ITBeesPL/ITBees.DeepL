using System.Text.Json.Serialization;

namespace ITBees.DeepL.Services;

/// <summary>
/// Client of the DeepL API v2 (header "Authorization: DeepL-Auth-Key ..."). The key and the address come per
/// call, because they live in the host database (admin panel) and may change at runtime. Use
/// <see cref="IDeepLTranslationService"/> when the saved settings should be used.
/// </summary>
public interface IDeepLApiClient
{
    /// <summary>GET /v2/usage - checks the key and returns the characters used in the current billing period.</summary>
    Task<DeepLApiUsage> GetUsageAsync(string baseUrl, string apiKey, CancellationToken ct = default);

    /// <summary>POST /v2/translate - translates up to 50 texts (128 KiB in total) in one call.</summary>
    Task<DeepLApiTranslateResponse> TranslateAsync(string baseUrl, string apiKey, DeepLApiTranslateRequest request,
        CancellationToken ct = default);

    /// <summary>GET /v2/languages?type=source|target - languages DeepL translates from / into.</summary>
    Task<List<DeepLApiLanguage>> GetLanguagesAsync(string baseUrl, string apiKey, DeepLLanguageType type,
        CancellationToken ct = default);
}

public enum DeepLLanguageType
{
    Source,
    Target
}

/// <summary>Response of GET /v2/usage (the subset common to Free and Pro plans).</summary>
public class DeepLApiUsage
{
    [JsonPropertyName("character_count")]
    public long CharacterCount { get; set; }

    [JsonPropertyName("character_limit")]
    public long CharacterLimit { get; set; }
}

/// <summary>Request of POST /v2/translate. Null options are left out of the JSON.</summary>
public class DeepLApiTranslateRequest
{
    [JsonPropertyName("text")]
    public List<string> Text { get; set; } = new();

    /// <summary>Target language code, e.g. PL, EN-GB, DE.</summary>
    [JsonPropertyName("target_lang")]
    public string TargetLang { get; set; } = "";

    /// <summary>Source language code; null = DeepL detects the language.</summary>
    [JsonPropertyName("source_lang")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceLang { get; set; }

    /// <summary>default, more, less, prefer_more or prefer_less.</summary>
    [JsonPropertyName("formality")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formality { get; set; }

    [JsonPropertyName("preserve_formatting")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PreserveFormatting { get; set; }

    /// <summary>"xml" or "html".</summary>
    [JsonPropertyName("tag_handling")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TagHandling { get; set; }

    /// <summary>Additional context that influences the translation but is not translated itself.</summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; set; }
}

public class DeepLApiTranslateResponse
{
    [JsonPropertyName("translations")]
    public List<DeepLApiTranslation> Translations { get; set; } = new();
}

public class DeepLApiTranslation
{
    /// <summary>Language DeepL detected in the input, e.g. EN.</summary>
    [JsonPropertyName("detected_source_language")]
    public string DetectedSourceLanguage { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class DeepLApiLanguage
{
    /// <summary>Language code, e.g. PL, EN-GB.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "";

    /// <summary>English name, e.g. Polish.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Only for target languages: whether the formality option is supported.</summary>
    [JsonPropertyName("supports_formality")]
    public bool? SupportsFormality { get; set; }
}

/// <summary>Error body of the DeepL API - "message" and sometimes "detail".</summary>
public class DeepLApiError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

/// <summary>Error answered by the DeepL API, no connection to it, or the integration being switched off.</summary>
public class DeepLApiException : Exception
{
    public DeepLApiException(string errorCode, string message, int statusCode) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    /// <summary>
    /// unauthorized (401/403), quota_exceeded (456), too_many_requests (429), bad_request (400),
    /// request_too_large (413), server_error (5xx), connection_failed, timeout, empty_response,
    /// unexpected_response, integration_disabled or http_&lt;code&gt;.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>HTTP status of the DeepL response; 0 when there was no response.</summary>
    public int StatusCode { get; }
}
