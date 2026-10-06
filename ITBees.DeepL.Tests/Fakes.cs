using ITBees.DeepL.Entities;
using ITBees.DeepL.Models;
using ITBees.DeepL.Services;

namespace ITBees.DeepL.Tests;

/// <summary>Settings service over an in-memory row (null = nothing saved yet).</summary>
internal sealed class FakeSettingsService : IDeepLIntegrationSettingsService
{
    public DeepLIntegrationSettings? Saved { get; set; }

    public DeepLIntegrationSettingsVm Get()
    {
        return Saved == null ? new DeepLIntegrationSettingsVm() : new DeepLIntegrationSettingsVm(Saved);
    }

    public DeepLIntegrationSettingsVm Update(DeepLIntegrationSettingsIm im, Guid? modifiedByGuid = null)
    {
        Saved = new DeepLIntegrationSettings
        {
            Id = 1,
            Enabled = im.Enabled,
            ApiKey = im.ApiKey.Trim(),
            BaseUrl = im.BaseUrl.Trim().TrimEnd('/'),
            Modified = DateTime.Now,
            ModifiedByGuid = modifiedByGuid
        };
        return new DeepLIntegrationSettingsVm(Saved);
    }

    public DeepLIntegrationSettings? GetEnabledSettingsOrNull()
    {
        if (Saved == null || !Saved.Enabled || string.IsNullOrWhiteSpace(Saved.ApiKey))
        {
            return null;
        }

        return Saved;
    }
}

/// <summary>API client that records the calls and answers from the configured values.</summary>
internal sealed class FakeApiClient : IDeepLApiClient
{
    public List<(string BaseUrl, string ApiKey, string Operation)> Calls { get; } = new();
    public List<DeepLApiTranslateRequest> TranslateRequests { get; } = new();

    public DeepLApiUsage Usage { get; set; } = new() { CharacterCount = 0, CharacterLimit = 500_000 };
    public DeepLApiException? Failure { get; set; }
    public List<DeepLApiLanguage> Languages { get; set; } = new();

    public Task<DeepLApiUsage> GetUsageAsync(string baseUrl, string apiKey, CancellationToken ct = default)
    {
        Calls.Add((baseUrl, apiKey, "usage"));
        if (Failure != null) throw Failure;
        return Task.FromResult(Usage);
    }

    public Task<DeepLApiTranslateResponse> TranslateAsync(string baseUrl, string apiKey,
        DeepLApiTranslateRequest request, CancellationToken ct = default)
    {
        Calls.Add((baseUrl, apiKey, "translate"));
        TranslateRequests.Add(request);
        if (Failure != null) throw Failure;

        // Echo every text back, upper-cased, so the order can be checked.
        return Task.FromResult(new DeepLApiTranslateResponse
        {
            Translations = request.Text
                .Select(t => new DeepLApiTranslation { DetectedSourceLanguage = "PL", Text = t.ToUpperInvariant() })
                .ToList()
        });
    }

    public Task<List<DeepLApiLanguage>> GetLanguagesAsync(string baseUrl, string apiKey, DeepLLanguageType type,
        CancellationToken ct = default)
    {
        Calls.Add((baseUrl, apiKey, $"languages:{type}"));
        if (Failure != null) throw Failure;
        return Task.FromResult(Languages);
    }
}
