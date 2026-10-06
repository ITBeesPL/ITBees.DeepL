using ITBees.DeepL.Services;
using Xunit;

namespace ITBees.DeepL.Tests;

public class DeepLApiUrlTests
{
    [Theory]
    [InlineData("279a2e9d-83b3-c416-7e2d-f721593e42a0:fx", true)]
    [InlineData("279a2e9d-83b3-c416-7e2d-f721593e42a0:FX ", true)]
    [InlineData("279a2e9d-83b3-c416-7e2d-f721593e42a0", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsFreeApiKey_RecognisesTheFxSuffix(string? apiKey, bool expected)
    {
        Assert.Equal(expected, DeepLApiUrl.IsFreeApiKey(apiKey));
    }

    [Fact]
    public void Resolve_FreeKeyWithoutAddress_GoesToTheFreeEndpoint()
    {
        Assert.Equal("https://api-free.deepl.com", DeepLApiUrl.Resolve("abc:fx", ""));
    }

    [Fact]
    public void Resolve_ProKeyWithoutAddress_GoesToTheProEndpoint()
    {
        Assert.Equal("https://api.deepl.com", DeepLApiUrl.Resolve("abc", null));
    }

    [Fact]
    public void Resolve_ExplicitAddressWins_AndLosesTheTrailingSlash()
    {
        Assert.Equal("https://deepl-proxy.example.com", DeepLApiUrl.Resolve("abc:fx", " https://deepl-proxy.example.com/ "));
    }

    [Theory]
    [InlineData("https://api-free.deepl.com", "Free")]
    [InlineData("https://api.deepl.com/", "Pro")]
    [InlineData("https://deepl-proxy.example.com", null)]
    public void PlanFor_NamesOnlyTheOfficialEndpoints(string apiUrl, string? expected)
    {
        Assert.Equal(expected, DeepLApiUrl.PlanFor(apiUrl));
    }
}
