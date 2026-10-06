namespace ITBees.DeepL.Services;

/// <summary>
/// Translation with the DeepL settings saved by the platform operator (key, address, main switch). Every call
/// throws <see cref="DeepLApiException"/> with ErrorCode "integration_disabled" while the integration is off
/// or has no key - check <see cref="IsEnabled"/> first when the feature is optional.
/// </summary>
public interface IDeepLTranslationService
{
    /// <summary>True when the integration is switched on and has an API key.</summary>
    bool IsEnabled();

    /// <summary>Translates one text into <paramref name="targetLanguage"/> (e.g. PL, EN-GB, DE).</summary>
    Task<DeepLApiTranslation> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null,
        DeepLTranslationOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Translates many texts, in the order given. Texts go to DeepL in batches of 50 (the API limit per call).
    /// </summary>
    Task<List<DeepLApiTranslation>> TranslateAsync(IReadOnlyList<string> texts, string targetLanguage,
        string? sourceLanguage = null, DeepLTranslationOptions? options = null, CancellationToken ct = default);

    /// <summary>Languages DeepL translates from (Source) or into (Target).</summary>
    Task<List<DeepLApiLanguage>> GetLanguagesAsync(DeepLLanguageType type, CancellationToken ct = default);

    /// <summary>Characters used and available in the current billing period.</summary>
    Task<DeepLApiUsage> GetUsageAsync(CancellationToken ct = default);
}

/// <summary>Optional DeepL translation parameters; null = DeepL default.</summary>
public class DeepLTranslationOptions
{
    /// <summary>default, more, less, prefer_more or prefer_less - for target languages that support formality (PL does).</summary>
    public string? Formality { get; set; }

    /// <summary>Keep the formatting (line breaks, punctuation at the ends) of the input.</summary>
    public bool? PreserveFormatting { get; set; }

    /// <summary>"xml" or "html" - the markup is kept, only the text is translated.</summary>
    public string? TagHandling { get; set; }

    /// <summary>Additional context that influences the translation but is not translated itself.</summary>
    public string? Context { get; set; }
}
