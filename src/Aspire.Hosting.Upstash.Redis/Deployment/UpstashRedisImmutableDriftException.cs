namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisImmutableDriftException : InvalidOperationException
{
    public UpstashRedisImmutableDriftException()
    {
    }

    public UpstashRedisImmutableDriftException(string message)
        : base(message)
    {
    }

    public UpstashRedisImmutableDriftException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UpstashRedisImmutableDriftException(UpstashRedisImmutableDrift drift)
        : base(GetMessage(drift))
    {
        Drift = drift;
    }

    public UpstashRedisImmutableDrift? Drift { get; }

    private static string GetMessage(UpstashRedisImmutableDrift drift)
    {
        ArgumentNullException.ThrowIfNull(drift);

        return drift.Message;
    }
}
