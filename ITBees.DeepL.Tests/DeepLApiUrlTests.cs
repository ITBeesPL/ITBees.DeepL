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

    [Theory]
    [InlineData("https://api.deepl.com", "https://api.deepl.com")]
    [InlineData("https://api.deepl.com/", "https://api.deepl.com")]
    [InlineData(" HTTPS://API-FREE.DEEPL.COM:443/ ", "https://api-free.deepl.com")]
    public void EnsureAllowed_AcceptsTheOfficialEndpoints_Normalized(string input, string expected)
    {
        Assert.Equal(expected, DeepLApiUrl.EnsureAllowed(input));
        Assert.True(DeepLApiUrl.IsAllowed(input));
    }

    [Theory]
    // The reported SSRF payload: the fragment would swallow the appended "/v2/usage".
    [InlineData("http://127.0.0.1:8080/internal/status#")]
    [InlineData("https://127.0.0.1:8080/internal/status#")]
    [InlineData("https://10.0.0.5/")]
    // Plain http would send the key in clear text.
    [InlineData("http://api.deepl.com")]
    // Look-alikes and smuggling attempts.
    [InlineData("https://api.deepl.com/v2")]
    [InlineData("https://api.deepl.com?x=1")]
    [InlineData("https://user:pass@api.deepl.com")]
    [InlineData("https://api.deepl.com.evil.example")]
    [InlineData("https://evil.example/api.deepl.com")]
    // A proxy the host did not configure.
    [InlineData("https://deepl-proxy.example.com")]
    [InlineData("api.deepl.com")]
    [InlineData("not a url")]
    [InlineData("")]
    public void EnsureAllowed_RejectsEverythingElse(string input)
    {
        var e = Assert.Throws<DeepLApiException>(() => DeepLApiUrl.EnsureAllowed(input));

        Assert.Equal("invalid_base_url", e.ErrorCode);
        Assert.False(DeepLApiUrl.IsAllowed(input));
    }

    [Fact]
    public void Resolve_ValidatesAnExplicitAddressToo()
    {
        var e = Assert.Throws<DeepLApiException>(() => DeepLApiUrl.Resolve("key:fx", "http://127.0.0.1:8080/internal/status#"));

        Assert.Equal("invalid_base_url", e.ErrorCode);
    }

    [Fact]
    public void HostConfiguredProxy_IsAccepted_ButOnlyOverHttps()
    {
        var options = new DeepLApiOptions
        {
            AdditionalAllowedBaseUrls = { "https://deepl-proxy.example.com/", "http://insecure-proxy.example.com" }
        };

        Assert.Equal("https://deepl-proxy.example.com", DeepLApiUrl.EnsureAllowed("https://deepl-proxy.example.com", options));
        Assert.Equal("https://deepl-proxy.example.com", DeepLApiUrl.Resolve("key:fx", "https://deepl-proxy.example.com/", options));
        Assert.Throws<DeepLApiException>(() => DeepLApiUrl.EnsureAllowed("http://insecure-proxy.example.com", options));
        Assert.Equal(
            new[] { "https://api.deepl.com", "https://api-free.deepl.com", "https://deepl-proxy.example.com" },
            DeepLApiUrl.AllowedBaseUrls(options));
    }

    [Fact]
    public void DisallowedAddress_MessageListsTheAllowedOnes()
    {
        var e = Assert.Throws<DeepLApiException>(() => DeepLApiUrl.EnsureAllowed("https://deepl-proxy.example.com"));

        Assert.Contains("https://api.deepl.com", e.Message);
        Assert.Contains("https://api-free.deepl.com", e.Message);
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
