using System.Net;
using System.Text;

namespace ITBees.DeepL.Tests;

/// <summary>Records every request and answers with whatever the test's responder returns.</summary>
internal sealed class FakeDeepLHandler : HttpMessageHandler
{
    private readonly Func<CapturedRequest, HttpResponseMessage> _responder;

    public List<CapturedRequest> Requests { get; } = new();

    public FakeDeepLHandler(Func<CapturedRequest, HttpResponseMessage> responder) => _responder = responder;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var captured = new CapturedRequest(request.Method, request.RequestUri!.AbsoluteUri,
            request.Headers.Authorization?.ToString(), body);
        Requests.Add(captured);
        return _responder(captured);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    /// <summary>DeepL answers non-standard codes too (456 = quota exceeded), so the status is an int here.</summary>
    public static HttpResponseMessage Json(int status, string json) => Json((HttpStatusCode)status, json);
}

internal sealed record CapturedRequest(HttpMethod Method, string Url, string? Authorization, string? Body);

/// <summary>IHttpClientFactory handing out clients over one fake handler, whatever the client name.</summary>
internal sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
