namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisProviderDeploymentOptions
{
    public UpstashRedisProviderDeploymentOptions(
        UpstashRedisProviderValue? platform,
        UpstashRedisProviderValue? primaryRegion,
        IReadOnlyList<UpstashRedisProviderValue>? readRegions,
        UpstashRedisProviderValue? plan,
        UpstashRedisProviderValue? budget,
        UpstashRedisProviderValue? eviction,
        UpstashRedisProviderValue? tls,
        IReadOnlySet<string> explicitSettings)
    {
        Platform = platform;
        PrimaryRegion = primaryRegion;
        ReadRegions = readRegions;
        Plan = plan;
        Budget = budget;
        Eviction = eviction;
        Tls = tls;
        ExplicitSettings = explicitSettings;
    }

    public UpstashRedisProviderValue? Platform { get; }

    public UpstashRedisProviderValue? PrimaryRegion { get; }

    public IReadOnlyList<UpstashRedisProviderValue>? ReadRegions { get; }

    public UpstashRedisProviderValue? Plan { get; }

    public UpstashRedisProviderValue? Budget { get; }

    public UpstashRedisProviderValue? Eviction { get; }

    public UpstashRedisProviderValue? Tls { get; }

    public IReadOnlySet<string> ExplicitSettings { get; }
}
