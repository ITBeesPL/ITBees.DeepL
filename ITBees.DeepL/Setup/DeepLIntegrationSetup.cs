using ITBees.DeepL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ITBees.DeepL.Setup;

public class DeepLIntegrationSetup
{
    /// <summary>
    /// Registers the DeepL API client (named HttpClient), the settings service (settings live in the host
    /// database - see <see cref="DbModelBuilder.Register"/>), the connection test and the translation service.
    /// The /DeepLIntegrationSettings and /DeepLConnectionTest controllers are discovered automatically by ASP.NET.
    /// </summary>
    public void Register(IServiceCollection services)
    {
        // 60 s: a batch of long texts can take a while; usage and languages answer within a second.
        services.AddHttpClient(DeepLApiClient.HttpClientName,
                client => client.Timeout = TimeSpan.FromSeconds(60))
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddTransient<IDeepLIntegrationSettingsService, DeepLIntegrationSettingsService>();
        services.AddTransient<IDeepLApiClient, DeepLApiClient>();
        services.AddTransient<IDeepLConnectionTestService, DeepLConnectionTestService>();
        services.AddTransient<IDeepLTranslationService, DeepLTranslationService>();
    }
}
