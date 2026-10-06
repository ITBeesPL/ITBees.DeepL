using System.Net;
using System.Text.Json;
using ITBees.DeepL.Services;
using Xunit;

namespace ITBees.DeepL.Tests;

public class DeepLApiClientTests
{
    private const string ApiKey = "279a2e9d-83b3-c416-7e2d-f721593e42a0:fx";

    private static (DeepLApiClient client, FakeDeepLHandler handler) CreateClient(
        Func<CapturedRequest, HttpResponseMessage> responder)
    {
        var handler = new FakeDeepLHandler(responder);
        return (new DeepLApiClient(new FakeHttpClientFactory(handler)), handler);
    }

    [Fact]
    public async Task GetUsage_SendsTheDeepLAuthKeyHeader_AndParsesTheCounts()
    {
        var (client, handler) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.OK,
            """{"character_count": 180118, "character_limit": 1250000}"""));

        var usage = await client.GetUsageAsync("https://api-free.deepl.com/", ApiKey);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api-free.deepl.com/v2/usage", request.Url);
        Assert.Equal($"DeepL-Auth-Key {ApiKey}", request.Authorization);
        Assert.Equal(180118, usage.CharacterCount);
        Assert.Equal(1250000, usage.CharacterLimit);
    }

    [Fact]
    public async Task Translate_PostsSnakeCaseJsonWithoutNullOptions_AndParsesTranslations()
    {
        var (client, handler) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.OK,
            """{"translations":[{"detected_source_language":"EN","text":"Witaj, świecie!"}]}"""));

        var response = await client.TranslateAsync("https://api.deepl.com", ApiKey, new DeepLApiTranslateRequest
        {
            Text = new List<string> { "Hello, world!" },
            TargetLang = "PL",
            SourceLang = "EN",
            Formality = "more"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.deepl.com/v2/translate", request.Url);

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        Assert.Equal("Hello, world!", root.GetProperty("text")[0].GetString());
        Assert.Equal("PL", root.GetProperty("target_lang").GetString());
        Assert.Equal("EN", root.GetProperty("source_lang").GetString());
        Assert.Equal("more", root.GetProperty("formality").GetString());
        Assert.False(root.TryGetProperty("context", out _));
        Assert.False(root.TryGetProperty("tag_handling", out _));
        Assert.False(root.TryGetProperty("preserve_formatting", out _));

        var translation = Assert.Single(response.Translations);
        Assert.Equal("EN", translation.DetectedSourceLanguage);
        Assert.Equal("Witaj, świecie!", translation.Text);
    }

    [Fact]
    public async Task GetLanguages_AsksForTheRequestedType()
    {
        var (client, handler) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.OK,
            """[{"language":"PL","name":"Polish","supports_formality":true}]"""));

        var languages = await client.GetLanguagesAsync("https://api.deepl.com", ApiKey, DeepLLanguageType.Target);

        Assert.Equal("https://api.deepl.com/v2/languages?type=target", handler.Requests.Single().Url);
        var language = Assert.Single(languages);
        Assert.Equal("PL", language.Language);
        Assert.True(language.SupportsFormality);
    }

    [Fact]
    public async Task Forbidden_IsReportedAsUnauthorized_WithDeepLsOwnMessage()
    {
        var (client, _) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.Forbidden,
            """{"message":"Wrong endpoint. Use https://api-free.deepl.com"}"""));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("unauthorized", e.ErrorCode);
        Assert.Equal(403, e.StatusCode);
        Assert.Contains("odrzucił klucz API", e.Message);
        Assert.Contains("Wrong endpoint", e.Message);
    }

    [Fact]
    public async Task QuotaExceeded456_IsReportedAsQuotaExceeded()
    {
        var (client, _) = CreateClient(_ => FakeDeepLHandler.Json(456, """{"message":"Quota Exceeded"}"""));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("quota_exceeded", e.ErrorCode);
        Assert.Equal(456, e.StatusCode);
    }

    [Fact]
    public async Task TooManyRequests_IsReportedAsTooManyRequests()
    {
        var (client, _) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.TooManyRequests, ""));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("too_many_requests", e.ErrorCode);
        Assert.Equal(429, e.StatusCode);
    }

    [Fact]
    public async Task ServerError_IsReportedAsServerError()
    {
        var (client, _) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.ServiceUnavailable, "<html>oops</html>"));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("server_error", e.ErrorCode);
        Assert.Equal(503, e.StatusCode);
    }

    [Fact]
    public async Task NoConnection_IsReportedAsConnectionFailed()
    {
        var (client, _) = CreateClient(_ => throw new HttpRequestException("No such host is known."));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("connection_failed", e.ErrorCode);
        Assert.Equal(0, e.StatusCode);
        Assert.Contains("api.deepl.com", e.Message);
    }

    [Fact]
    public async Task EmptySuccessBody_IsReportedAsEmptyResponse()
    {
        var (client, _) = CreateClient(_ => FakeDeepLHandler.Json(HttpStatusCode.OK, ""));

        var e = await Assert.ThrowsAsync<DeepLApiException>(() => client.GetUsageAsync("https://api.deepl.com", ApiKey));

        Assert.Equal("empty_response", e.ErrorCode);
    }
}
