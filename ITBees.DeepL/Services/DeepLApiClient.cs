using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ITBees.DeepL.Services;

public class DeepLApiClient : IDeepLApiClient
{
    public const string HttpClientName = "DeepLApi";

    /// <summary>DeepL's own error text is appended to our message up to this length.</summary>
    private const int MaxApiMessageLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeepLApiOptions _options;

    // One constructor on purpose: hosts register a bare HttpClient in DI, so a second constructor taking
    // HttpClient would end in "ambiguous constructors" (as it did in ITBees.Inpost).
    public DeepLApiClient(IHttpClientFactory httpClientFactory, DeepLApiOptions? options = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options ?? new DeepLApiOptions();
    }

    public async Task<DeepLApiUsage> GetUsageAsync(string baseUrl, string apiKey, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Get, baseUrl, "/v2/usage", apiKey);
        return await SendAsync<DeepLApiUsage>(request, ct);
    }

    public async Task<DeepLApiTranslateResponse> TranslateAsync(string baseUrl, string apiKey,
        DeepLApiTranslateRequest translateRequest, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, baseUrl, "/v2/translate", apiKey);
        request.Content = JsonContent.Create(translateRequest, options: JsonOptions);
        return await SendAsync<DeepLApiTranslateResponse>(request, ct);
    }

    public async Task<List<DeepLApiLanguage>> GetLanguagesAsync(string baseUrl, string apiKey,
        DeepLLanguageType type, CancellationToken ct = default)
    {
        var typeQuery = type == DeepLLanguageType.Source ? "source" : "target";
        using var request = CreateRequest(HttpMethod.Get, baseUrl, $"/v2/languages?type={typeQuery}", apiKey);
        return await SendAsync<List<DeepLApiLanguage>>(request, ct);
    }

    /// <summary>
    /// The address is validated here, right before the request, whatever the caller: only the DeepL endpoints
    /// and the host-configured proxies are called (see <see cref="DeepLApiUrl.EnsureAllowed"/>).
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string baseUrl, string path, string apiKey)
    {
        var apiUrl = DeepLApiUrl.EnsureAllowed(baseUrl, _options);
        var request = new HttpRequestMessage(method, $"{apiUrl}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey.Trim());
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (HttpRequestException e)
        {
            throw new DeepLApiException("connection_failed",
                $"Nie udało się połączyć z API DeepL ({request.RequestUri?.Host}): {e.Message}", 0);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested == false)
        {
            throw new DeepLApiException("timeout", "API DeepL nie odpowiedziało w wyznaczonym czasie.", 0);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var result = Deserialize<T>(body);
                if (result == null)
                {
                    throw new DeepLApiException("empty_response", "Pusta odpowiedź z API DeepL.", statusCode);
                }

                return result;
            }

            var error = Deserialize<DeepLApiError>(body);
            var apiMessage = string.IsNullOrWhiteSpace(error?.Message) ? error?.Detail : error.Message;
            throw new DeepLApiException(ErrorCodeFor(statusCode), MessageFor(statusCode, apiMessage), statusCode);
        }
    }

    private static string ErrorCodeFor(int statusCode)
    {
        return statusCode switch
        {
            401 or 403 => "unauthorized",
            400 => "bad_request",
            413 => "request_too_large",
            429 => "too_many_requests",
            456 => "quota_exceeded",
            >= 500 => "server_error",
            _ => $"http_{statusCode}"
        };
    }

    /// <summary>Operator-facing explanation of the status; DeepL's own (trimmed, capped) message is appended when present.</summary>
    private static string MessageFor(int statusCode, string? apiMessage)
    {
        var text = statusCode switch
        {
            401 or 403 =>
                "DeepL odrzucił klucz API - klucz jest nieprawidłowy, unieważniony albo użyty pod złym adresem " +
                "(klucz Free działa tylko na api-free.deepl.com, klucz Pro tylko na api.deepl.com).",
            400 => "API DeepL odrzuciło żądanie jako nieprawidłowe.",
            413 => "Żądanie do DeepL jest za duże (limit 128 KiB tekstu w jednym wywołaniu).",
            429 => "Zbyt wiele żądań do API DeepL - odczekaj chwilę i ponów.",
            456 => "Wyczerpany limit znaków konta DeepL w bieżącym okresie rozliczeniowym.",
            >= 500 => "API DeepL zgłosiło błąd po swojej stronie - spróbuj ponownie za chwilę.",
            _ => $"API DeepL odpowiedziało kodem {statusCode}."
        };

        if (string.IsNullOrWhiteSpace(apiMessage))
        {
            return text;
        }

        var detail = apiMessage.Trim();
        if (detail.Length > MaxApiMessageLength)
        {
            detail = detail[..MaxApiMessageLength] + "…";
        }

        return $"{text} ({detail})";
    }

    private static T? Deserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
