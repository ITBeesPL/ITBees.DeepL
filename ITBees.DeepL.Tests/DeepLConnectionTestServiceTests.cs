using ITBees.DeepL.Entities;
using ITBees.DeepL.Models;
using ITBees.DeepL.Services;
using Xunit;

namespace ITBees.DeepL.Tests;

public class DeepLConnectionTestServiceTests
{
    private readonly FakeSettingsService _settings = new();
    private readonly FakeApiClient _apiClient = new();
    private readonly DeepLConnectionTestService _service;

    public DeepLConnectionTestServiceTests()
    {
        _service = new DeepLConnectionTestService(_settings, _apiClient);
    }

    [Fact]
    public async Task NoKeyAnywhere_FailsWithAHint_WithoutCallingDeepL()
    {
        var result = await _service.TestAsync(new DeepLConnectionTestIm());

        Assert.False(result.Success);
        Assert.Contains("Podaj klucz API", result.Message);
        Assert.Empty(_apiClient.Calls);
    }

    [Fact]
    public async Task EmptyForm_UsesTheSavedKeyAndAddress()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = "saved-key:fx", BaseUrl = "" };

        var result = await _service.TestAsync(new DeepLConnectionTestIm { ApiKey = "  ", BaseUrl = null });

        var call = Assert.Single(_apiClient.Calls);
        Assert.Equal(("https://api-free.deepl.com", "saved-key:fx", "usage"), call);
        Assert.True(result.Success);
        Assert.Equal("Free", result.Plan);
        Assert.Equal("https://api-free.deepl.com", result.ApiUrl);
    }

    [Fact]
    public async Task FormValues_WinOverTheSavedOnes_SoAKeyCanBeCheckedBeforeSaving()
    {
        _settings.Saved = new DeepLIntegrationSettings { Enabled = true, ApiKey = "saved-key:fx" };

        await _service.TestAsync(new DeepLConnectionTestIm { ApiKey = " new-pro-key ", BaseUrl = "" });

        var call = Assert.Single(_apiClient.Calls);
        Assert.Equal("new-pro-key", call.ApiKey);
        Assert.Equal("https://api.deepl.com", call.BaseUrl);
    }

    [Fact]
    public async Task Success_ReportsThePlanAndTheUsage()
    {
        _apiClient.Usage = new DeepLApiUsage { CharacterCount = 250_000, CharacterLimit = 500_000 };

        var result = await _service.TestAsync(new DeepLConnectionTestIm { ApiKey = "key:fx" });

        Assert.True(result.Success);
        Assert.Equal(250_000, result.CharacterCount);
        Assert.Equal(500_000, result.CharacterLimit);
        Assert.Equal(50, result.UsagePercent);
        Assert.StartsWith("Połączenie działa. Konto DeepL API Free", result.Message);
        Assert.Contains("(50%)", result.Message);
    }

    [Fact]
    public async Task LimitReached_StillSucceeds_ButWarns()
    {
        _apiClient.Usage = new DeepLApiUsage { CharacterCount = 500_000, CharacterLimit = 500_000 };

        var result = await _service.TestAsync(new DeepLConnectionTestIm { ApiKey = "key:fx" });

        Assert.True(result.Success);
        Assert.Equal(100, result.UsagePercent);
        Assert.Contains("Limit znaków jest wyczerpany", result.Message);
    }

    [Fact]
    public async Task CustomAddress_HasNoPlan()
    {
        var result = await _service.TestAsync(new DeepLConnectionTestIm
        {
            ApiKey = "key",
            BaseUrl = "https://deepl-proxy.example.com/"
        });

        Assert.True(result.Success);
        Assert.Null(result.Plan);
        Assert.Equal("https://deepl-proxy.example.com", result.ApiUrl);
        Assert.StartsWith("Połączenie działa. Konto DeepL API:", result.Message);
    }

    [Fact]
    public async Task RejectedKey_FailsWithTheClientsMessage()
    {
        _apiClient.Failure = new DeepLApiException("unauthorized", "DeepL odrzucił klucz API - test.", 403);

        var result = await _service.TestAsync(new DeepLConnectionTestIm { ApiKey = "bad-key" });

        Assert.False(result.Success);
        Assert.Equal("DeepL odrzucił klucz API - test.", result.Message);
        Assert.Equal("Pro", result.Plan);
    }
}
