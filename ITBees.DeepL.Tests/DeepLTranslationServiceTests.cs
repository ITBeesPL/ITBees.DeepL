using ITBees.DeepL.Entities;
using ITBees.DeepL.Services;
using Xunit;

namespace ITBees.DeepL.Tests;

public class DeepLTranslationServiceTests
{
    private readonly FakeSettingsService _settings = new();
    private readonly FakeApiClient _apiClient = new();
    private readonly DeepLTranslationService _service;

    public DeepLTranslationServiceTests()
    {
        _service = new DeepLTranslationService(_settings, _apiClient);
    }

    [Fact]
    public async Task Disabled_ThrowsIntegrationDisabled_WithoutCallingDeepL()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = false, ApiKey = "key:fx" };

        Assert.False(_service.IsEnabled());
        var e = await Assert.ThrowsAsync<DeepLApiException>(() => _service.TranslateAsync("Cześć", "EN-GB"));

        Assert.Equal("integration_disabled", e.ErrorCode);
        Assert.Empty(_apiClient.Calls);
    }

    [Fact]
    public async Task EnabledWithoutKey_CountsAsDisabled()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = " " };

        Assert.False(_service.IsEnabled());
        await Assert.ThrowsAsync<DeepLApiException>(() => _service.GetUsageAsync());
    }

    [Fact]
    public async Task Enabled_UsesTheSavedKeyAndTheAddressChosenByIt_AndUpperCasesLanguages()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = "key:fx", BaseUrl = "" };

        Assert.True(_service.IsEnabled());
        var translation = await _service.TranslateAsync("cześć", "en-gb", "pl",
            new DeepLTranslationOptions { Formality = "less", PreserveFormatting = true });

        var call = Assert.Single(_apiClient.Calls);
        Assert.Equal(("https://api-free.deepl.com", "key:fx", "translate"), call);
        var request = Assert.Single(_apiClient.TranslateRequests);
        Assert.Equal("EN-GB", request.TargetLang);
        Assert.Equal("PL", request.SourceLang);
        Assert.Equal("less", request.Formality);
        Assert.True(request.PreserveFormatting);
        Assert.Equal("CZEŚĆ", translation.Text);
    }

    [Fact]
    public async Task HostConfiguredProxy_IsUsedAsSaved()
    {
        _settings.Saved = new DeepLIntegrationSettings
        {
            Enabled = true,
            ApiKey = "key",
            BaseUrl = "https://deepl-proxy.example.com"
        };
        var options = new DeepLApiOptions { AdditionalAllowedBaseUrls = { "https://deepl-proxy.example.com" } };
        var service = new DeepLTranslationService(_settings, _apiClient, options);

        await service.GetLanguagesAsync(DeepLLanguageType.Target);

        Assert.Equal("https://deepl-proxy.example.com", _apiClient.Calls.Single().BaseUrl);
    }

    [Fact]
    public async Task DisallowedSavedAddress_IsRefusedBeforeAnyCall()
    {
        _settings.Saved = new DeepLIntegrationSettings
        {
            Enabled = true,
            ApiKey = "key",
            BaseUrl = "http://127.0.0.1:8080/internal/status#"
        };

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => _service.TranslateAsync("tekst", "EN"));

        Assert.Equal("invalid_base_url", e.ErrorCode);
        Assert.Empty(_apiClient.Calls);
    }

    [Fact]
    public async Task ManyTexts_GoInBatchesOfFifty_InOrder()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = "key" };
        var texts = Enumerable.Range(1, 120).Select(i => $"tekst {i}").ToList();

        var translations = await _service.TranslateAsync(texts, "DE");

        Assert.Equal(3, _apiClient.TranslateRequests.Count);
        Assert.Equal(new[] { 50, 50, 20 }, _apiClient.TranslateRequests.Select(r => r.Text.Count));
        Assert.Equal(120, translations.Count);
        Assert.Equal("TEKST 1", translations[0].Text);
        Assert.Equal("TEKST 120", translations[119].Text);
        Assert.All(_apiClient.TranslateRequests, r => Assert.Null(r.SourceLang));
    }

    [Fact]
    public async Task MissingTargetLanguage_IsRejectedBeforeAnyCall()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = "key" };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.TranslateAsync("tekst", " "));
        Assert.Empty(_apiClient.Calls);
    }
}
