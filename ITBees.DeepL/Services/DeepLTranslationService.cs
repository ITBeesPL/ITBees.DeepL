namespace ITBees.DeepL.Services;

public class DeepLTranslationService : IDeepLTranslationService
{
    /// <summary>Maximum number of texts the DeepL API accepts in one /v2/translate call.</summary>
    public const int MaxTextsPerRequest = 50;

    private readonly IDeepLIntegrationSettingsService _settingsService;
    private readonly IDeepLApiClient _deepLApiClient;

    public DeepLTranslationService(IDeepLIntegrationSettingsService settingsService, IDeepLApiClient deepLApiClient)
    {
        _settingsService = settingsService;
        _deepLApiClient = deepLApiClient;
    }

    public bool IsEnabled()
    {
        return _settingsService.GetEnabledSettingsOrNull() != null;
    }

    public async Task<DeepLApiTranslation> TranslateAsync(string text, string targetLanguage,
        string? sourceLanguage = null, DeepLTranslationOptions? options = null, CancellationToken ct = default)
    {
        var translations = await TranslateAsync(new[] { text }, targetLanguage, sourceLanguage, options, ct);
        return translations[0];
    }

    public async Task<List<DeepLApiTranslation>> TranslateAsync(IReadOnlyList<string> texts, string targetLanguage,
        string? sourceLanguage = null, DeepLTranslationOptions? options = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new ArgumentException("Target language is required.", nameof(targetLanguage));
        }

        var (apiUrl, apiKey) = GetConnection();
        var result = new List<DeepLApiTranslation>(texts.Count);

        for (var offset = 0; offset < texts.Count; offset += MaxTextsPerRequest)
        {
            var batch = texts.Skip(offset).Take(MaxTextsPerRequest).ToList();
            var response = await _deepLApiClient.TranslateAsync(apiUrl, apiKey, new DeepLApiTranslateRequest
            {
                Text = batch,
                TargetLang = NormalizeLanguage(targetLanguage)!,
                SourceLang = NormalizeLanguage(sourceLanguage),
                Formality = options?.Formality,
                PreserveFormatting = options?.PreserveFormatting,
                TagHandling = options?.TagHandling,
                Context = options?.Context
            }, ct);

            if (response.Translations.Count != batch.Count)
            {
                throw new DeepLApiException("unexpected_response",
                    $"API DeepL zwróciło {response.Translations.Count} tłumaczeń dla {batch.Count} tekstów.", 200);
            }

            result.AddRange(response.Translations);
        }

        return result;
    }

    public Task<List<DeepLApiLanguage>> GetLanguagesAsync(DeepLLanguageType type, CancellationToken ct = default)
    {
        var (apiUrl, apiKey) = GetConnection();
        return _deepLApiClient.GetLanguagesAsync(apiUrl, apiKey, type, ct);
    }

    public Task<DeepLApiUsage> GetUsageAsync(CancellationToken ct = default)
    {
        var (apiUrl, apiKey) = GetConnection();
        return _deepLApiClient.GetUsageAsync(apiUrl, apiKey, ct);
    }

    private (string apiUrl, string apiKey) GetConnection()
    {
        var settings = _settingsService.GetEnabledSettingsOrNull();
        if (settings == null)
        {
            throw new DeepLApiException("integration_disabled",
                "Integracja z DeepL jest wyłączona albo nie ma klucza API - włącz ją w panelu " +
                "(Ustawienia platformy -> Integracje -> DeepL).", 0);
        }

        return (DeepLApiUrl.Resolve(settings.ApiKey, settings.BaseUrl), settings.ApiKey.Trim());
    }

    /// <summary>DeepL codes are upper case (PL, EN-GB); null / blank stays null (= detect).</summary>
    private static string? NormalizeLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToUpperInvariant();
    }
}
