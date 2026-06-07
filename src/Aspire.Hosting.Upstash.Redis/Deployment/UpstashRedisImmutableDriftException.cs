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
        : base(drift.Message)
    {
        ArgumentNullException.ThrowIfNull(drift);

        Drift = drift;
    }

    public UpstashRedisImmutableDrift? Drift { get; }
}
