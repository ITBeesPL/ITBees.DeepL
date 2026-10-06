using ITBees.DeepL.Services;
using ITBees.DeepL.Setup;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ITBees.DeepL.Tests;

public class DeepLIntegrationSetupTests
{
    [Fact]
    public void Register_HandsTheHostOptionsToTheClient()
    {
        var services = new ServiceCollection();
        var options = new DeepLApiOptions { AdditionalAllowedBaseUrls = { "https://deepl-proxy.example.com" } };

        new DeepLIntegrationSetup().Register(services, options);
        using var provider = services.BuildServiceProvider();

        Assert.Same(options, provider.GetRequiredService<DeepLApiOptions>());
        Assert.IsType<DeepLApiClient>(provider.GetRequiredService<IDeepLApiClient>());
    }

    [Fact]
    public void Register_WithoutOptions_AllowsOnlyTheDeepLEndpoints()
    {
        var services = new ServiceCollection();

        new DeepLIntegrationSetup().Register(services);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<DeepLApiOptions>();
        Assert.Empty(options.AdditionalAllowedBaseUrls);
        Assert.Equal(new[] { "https://api.deepl.com", "https://api-free.deepl.com" }, DeepLApiUrl.AllowedBaseUrls(options));
    }

    [Fact]
    public void Register_DisablesAutomaticRedirects()
    {
        var services = new ServiceCollection();
        new DeepLIntegrationSetup().Register(services);
        using var provider = services.BuildServiceProvider();

        HttpMessageHandler handler = provider.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(DeepLApiClient.HttpClientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        var primary = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(primary.AllowAutoRedirect);
    }
}
