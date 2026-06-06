namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisReadinessPollingOptions
{
    public static UpstashRedisReadinessPollingOptions Default { get; } = new();

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(2);
}
