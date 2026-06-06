namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

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
