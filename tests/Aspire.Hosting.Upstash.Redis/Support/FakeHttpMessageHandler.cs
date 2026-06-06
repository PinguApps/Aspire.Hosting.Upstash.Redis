using System.Net;
using System.Text;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = [];
    private readonly List<CapturedHttpRequest> _requests = [];

    public IReadOnlyList<CapturedHttpRequest> Requests => _requests;

    public void Enqueue(HttpStatusCode statusCode, string content)
    {
        _responses.Enqueue((_, _) => Task.FromResult(CreateResponse(statusCode, content)));
    }

    public void Enqueue(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    {
        _responses.Enqueue(responseFactory);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? content = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _requests.Add(
            new CapturedHttpRequest(
                request.Method,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                content));

        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory =
            _responses.Count == 0
                ? throw new InvalidOperationException("No fake HTTP response was queued.")
                : _responses.Dequeue();

        return await responseFactory(request, cancellationToken).ConfigureAwait(false);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }
}

internal sealed class CapturedHttpRequest
{
    public CapturedHttpRequest(
        HttpMethod method,
        string pathAndQuery,
        string? authorizationScheme,
        string? authorizationParameter,
        string? content)
    {
        Method = method;
        PathAndQuery = pathAndQuery;
        AuthorizationScheme = authorizationScheme;
        AuthorizationParameter = authorizationParameter;
        Content = content;
    }

    public HttpMethod Method
    {
        get;
    }

    public string PathAndQuery
    {
        get;
    }

    public string? AuthorizationScheme
    {
        get;
    }

    public string? AuthorizationParameter
    {
        get;
    }

    public string? Content
    {
        get;
    }
}
