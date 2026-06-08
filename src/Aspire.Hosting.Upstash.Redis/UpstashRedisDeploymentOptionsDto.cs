namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// TypeScript-friendly Upstash Redis deployment options.
/// </summary>
[AspireDto]
public sealed class UpstashRedisDeploymentOptionsDto
{
    /// <summary>
    /// Gets or sets how deployment should treat a database with the requested name.
    /// </summary>
    public UpstashRedisOwnershipMode? OwnershipMode { get; set; }

    /// <summary>
    /// Gets or sets the Upstash platform or cloud provider.
    /// </summary>
    public UpstashRedisCloudPlatform? Platform { get; set; }

    /// <summary>
    /// Gets or sets the primary Upstash region.
    /// </summary>
    public UpstashRedisRegion? PrimaryRegion { get; set; }

    /// <summary>
    /// Gets or sets optional read regions.
    /// </summary>
    public IReadOnlyList<UpstashRedisRegion>? ReadRegions { get; set; }

    /// <summary>
    /// Gets or sets the Upstash plan.
    /// </summary>
    public UpstashRedisPlan? Plan { get; set; }

    /// <summary>
    /// Gets or sets the monthly Upstash budget.
    /// </summary>
    public int? Budget { get; set; }

    /// <summary>
    /// Gets or sets whether eviction is enabled.
    /// </summary>
    public bool? Eviction { get; set; }

    /// <summary>
    /// Gets or sets whether TLS should be enabled.
    /// </summary>
    public bool? Tls { get; set; }

    internal UpstashRedisOwnershipMode GetOwnershipMode()
    {
        return OwnershipMode ?? UpstashRedisOwnershipMode.CreateOrAdopt;
    }

    internal UpstashRedisDeploymentOptions ToDeploymentOptions()
    {
        UpstashRedisDeploymentOptions options = new();

        if (Platform is not null)
        {
            options.SetPlatform(Platform.Value);
        }

        if (PrimaryRegion is not null)
        {
            options.SetPrimaryRegion(PrimaryRegion.Value);
        }

        if (ReadRegions is not null)
        {
            options.SetReadRegions([.. ReadRegions]);
        }

        if (Plan is not null)
        {
            options.SetPlan(Plan.Value);
        }

        if (Budget is not null)
        {
            options.SetBudget(Budget.Value);
        }

        if (Eviction is not null)
        {
            options.Eviction = Eviction;
        }

        if (Tls is not null)
        {
            options.Tls = Tls;
        }

        return options;
    }
}
