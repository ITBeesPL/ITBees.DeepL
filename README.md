# ITBees.DeepL

DeepL API (v2) integration for FAS applications, in the same shape as `ITBees.DzielnikIntegration` and
`ITBees.Inpost`: the API key is edited by the platform operator in the admin panel and stored in the host
database, the library provides the client, the settings endpoints and a connection test.

What is inside:

| Element | Purpose |
|---|---|
| `Entities/DeepLIntegrationSettings` | One row in the host database: `Enabled`, `ApiKey`, `BaseUrl` (empty = chosen by the key), `Modified`, `ModifiedByGuid` |
| `GET/PUT /DeepLIntegrationSettings` | Settings for the admin panel (`PlatformOperator` only) |
| `POST /DeepLConnectionTest` | "Testuj połączenie": checks the key with `GET /v2/usage` (free of charge), returns the plan and the characters used in the billing period. Empty fields in the request mean the saved settings, so a key can be checked before it is saved |
| `IDeepLTranslationService` | Translation with the saved settings: `TranslateAsync` (one text or many, batches of 50), `GetLanguagesAsync`, `GetUsageAsync`, `IsEnabled()` |
| `IDeepLApiClient` | Raw client (`/v2/translate`, `/v2/usage`, `/v2/languages`) taking the key and the address per call |
| `DeepLApiUrl` | Free keys (`:fx` suffix) go to `https://api-free.deepl.com`, Pro keys to `https://api.deepl.com`; an explicit `BaseUrl` wins, but only when it is on the allow-list (see below) |

## Host registration

```csharp
// DependencyRegistration
new DeepLIntegrationSetup().Register(builder.Services);

// DbContext.OnModelCreating
ITBees.DeepL.Setup.DbModelBuilder.Register(modelBuilder);
```

Then add a migration (`dotnet ef migrations add AddDeepLIntegration`) - one table, `DeepLIntegrationSettings`.
The host must provide the generic ITBees repositories (`IReadOnlyRepository<>` / `IWriteOnlyRepository<>`,
e.g. `ITBees.MysqlRepository`) and `IHttpClientFactory` (`AddHttpClient` is called by the setup). The controllers
are discovered automatically by ASP.NET once the package is referenced.

## API address allow-list (SSRF)

The address comes from a form in the admin panel, so the library never calls anything but
`https://api.deepl.com`, `https://api-free.deepl.com` and the proxies the host lists in `DeepLApiOptions`.
Every request goes through `DeepLApiUrl.EnsureAllowed`: absolute https URL, no user info, query or fragment
(`https://host/x#` would swallow the appended `/v2/usage`), exact match with the allow-list. `PUT
/DeepLIntegrationSettings` answers 400 for any other address, `POST /DeepLConnectionTest` reports it as a
failed test without sending anything, and the named HttpClient does not follow redirects. DeepL's own error
text is appended to messages capped at 300 characters.

To put a company proxy in front of DeepL, allow it in the host (https only):

```csharp
new DeepLIntegrationSetup().Register(builder.Services, new DeepLApiOptions
{
    AdditionalAllowedBaseUrls = { "https://deepl-proxy.example.com" }
});
```

## Using translations

```csharp
public class ProductDescriptionTranslator
{
    private readonly IDeepLTranslationService _deepLTranslationService;

    public ProductDescriptionTranslator(IDeepLTranslationService deepLTranslationService)
    {
        _deepLTranslationService = deepLTranslationService;
    }

    public async Task<string> ToEnglishAsync(string polishText, CancellationToken ct)
    {
        if (!_deepLTranslationService.IsEnabled())
        {
            return polishText; // the operator has not configured DeepL
        }

        var translation = await _deepLTranslationService.TranslateAsync(polishText, "EN-GB", "PL",
            new DeepLTranslationOptions { PreserveFormatting = true }, ct);
        return translation.Text;
    }
}
```

Calls throw `DeepLApiException` with an `ErrorCode`: `integration_disabled` (switched off or no key),
`unauthorized` (403 - wrong key or Free key on the Pro address), `quota_exceeded` (456), `too_many_requests`
(429), `connection_failed`, `timeout`, `server_error`, `bad_request`. Messages are in Polish and can be shown
to the operator as they are.

## Admin panel

The panel side is the "DeepL" tab of "Ustawienia Platformy" > "Integracje": a switch, the key, an optional API
address, "Zapisz" and "Testuj połączenie". Generated TypeScript services: `DeepLIntegrationSettingsService`
(`deep-l-integration-settings.service.ts`) and `DeepLConnectionTestService` (`deep-l-connection-test.service.ts`).

## Releasing

A merge to `master` builds and publishes the NuGet package to the ITBees feed
(`https://nugets.itbees.pl/v3/index.json`). The build assigns the package version itself - the first merge
produced 8.0.2 while the csproj still said 8.0.1 - so `<Version>` in `ITBees.DeepL/ITBees.DeepL.csproj` only
matters for a local `dotnet pack` and is not bumped for a release. The host takes the version the feed reports
(e.g. `dotnet package search ITBees.DeepL`).
