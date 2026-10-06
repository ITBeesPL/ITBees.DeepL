using System.Globalization;
using ITBees.DeepL.Models;

namespace ITBees.DeepL.Services;

public interface IDeepLConnectionTestService
{
    Task<DeepLConnectionTestVm> TestAsync(DeepLConnectionTestIm im, CancellationToken ct = default);
}

/// <summary>
/// Checks a DeepL key with GET /v2/usage - the call is free of charge and answers 403 for a wrong key or a key
/// used on the wrong (Free / Pro) address. Reports the characters used in the billing period.
/// </summary>
public class DeepLConnectionTestService : IDeepLConnectionTestService
{
    private static readonly CultureInfo MessageCulture = CultureInfo.GetCultureInfo("pl-PL");

    private readonly IDeepLIntegrationSettingsService _settingsService;
    private readonly IDeepLApiClient _deepLApiClient;

    public DeepLConnectionTestService(IDeepLIntegrationSettingsService settingsService,
        IDeepLApiClient deepLApiClient)
    {
        _settingsService = settingsService;
        _deepLApiClient = deepLApiClient;
    }

    public async Task<DeepLConnectionTestVm> TestAsync(DeepLConnectionTestIm im, CancellationToken ct = default)
    {
        // Empty fields mean the saved values - so a key can be checked before it is saved.
        var saved = _settingsService.Get();
        var apiKey = string.IsNullOrWhiteSpace(im.ApiKey) ? saved.ApiKey : im.ApiKey.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(im.BaseUrl) ? saved.BaseUrl : im.BaseUrl.Trim();
        var apiUrl = DeepLApiUrl.Resolve(apiKey, baseUrl);

        var result = new DeepLConnectionTestVm
        {
            ApiUrl = apiUrl,
            Plan = DeepLApiUrl.PlanFor(apiUrl)
        };

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Message = "Podaj klucz API DeepL (konto DeepL -> Klucze API).";
            return result;
        }

        try
        {
            var usage = await _deepLApiClient.GetUsageAsync(apiUrl, apiKey, ct);
            result.Success = true;
            result.CharacterCount = usage.CharacterCount;
            result.CharacterLimit = usage.CharacterLimit;
            result.UsagePercent = usage.CharacterLimit > 0
                ? (int)Math.Min(100, Math.Round(usage.CharacterCount * 100.0 / usage.CharacterLimit))
                : 0;
            result.Message = BuildSuccessMessage(result);
        }
        catch (DeepLApiException e)
        {
            result.Message = e.Message;
        }

        return result;
    }

    private static string BuildSuccessMessage(DeepLConnectionTestVm result)
    {
        var account = result.Plan == null ? "Konto DeepL API" : $"Konto DeepL API {result.Plan}";
        var used = result.CharacterCount.ToString("N0", MessageCulture);

        var message = result.CharacterLimit > 0
            ? $"Połączenie działa. {account}: wykorzystano {used} z " +
              $"{result.CharacterLimit.ToString("N0", MessageCulture)} znaków ({result.UsagePercent}%) " +
              "w bieżącym okresie rozliczeniowym."
            : $"Połączenie działa. {account}: wykorzystano {used} znaków w bieżącym okresie rozliczeniowym.";

        if (result.CharacterLimit > 0 && result.CharacterCount >= result.CharacterLimit)
        {
            message += " Limit znaków jest wyczerpany - tłumaczenia nie będą działać do końca okresu.";
        }

        return message;
    }
}
