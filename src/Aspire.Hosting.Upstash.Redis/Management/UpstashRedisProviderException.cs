using System.Net;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisProviderException : Exception
{
    public UpstashRedisProviderException()
    {
    }

    public UpstashRedisProviderException(string message)
        : base(message)
    {
    }

    public UpstashRedisProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UpstashRedisProviderException(
        UpstashRedisProviderFailureKind failureKind,
        HttpStatusCode? statusCode,
        string message)
        : base(message)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public UpstashRedisProviderException(
        UpstashRedisProviderFailureKind failureKind,
        HttpStatusCode? statusCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public UpstashRedisProviderFailureKind FailureKind
    {
        get;
    } = UpstashRedisProviderFailureKind.Unexpected;

    public HttpStatusCode? StatusCode
    {
        get;
    }
}
