namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Optional Upstash Redis settings that should be reconciled only when explicitly configured.
/// </summary>
public sealed class UpstashRedisDeploymentOptions
{
    private readonly HashSet<string> _explicitSettings = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="UpstashRedisDeploymentOptions"/> class.
    /// </summary>
    public UpstashRedisDeploymentOptions()
    {
    }

    internal UpstashRedisDeploymentOptions(UpstashRedisDeploymentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Platform = source.Platform;
        PrimaryRegion = source.PrimaryRegion;
        ReadRegions = source.ReadRegions;
        Plan = source.Plan;
        Budget = source.Budget;
        Eviction = source.Eviction;
        Tls = source.Tls;

        _explicitSettings.Clear();
        _explicitSettings.UnionWith(source._explicitSettings);
    }

    /// <summary>
    /// Gets or sets the Upstash platform or cloud provider.
    /// </summary>
    public UpstashRedisValue? Platform
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Platform));
        }
    }

    /// <summary>
    /// Gets or sets the primary Upstash region.
    /// </summary>
    public UpstashRedisValue? PrimaryRegion
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(PrimaryRegion));
        }
    }

    /// <summary>
    /// Gets or sets optional read regions.
    /// </summary>
    public IReadOnlyList<UpstashRedisValue>? ReadRegions
    {
        get;
        set
        {
            field = value is null ? null : Array.AsReadOnly([.. value]);
            TrackExplicitSetting(nameof(ReadRegions));
        }
    }

    /// <summary>
    /// Gets or sets the Upstash plan.
    /// </summary>
    public UpstashRedisValue? Plan
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Plan));
        }
    }

    /// <summary>
    /// Gets or sets the budget setting.
    /// </summary>
    public UpstashRedisValue? Budget
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Budget));
        }
    }

    /// <summary>
    /// Gets or sets whether eviction is enabled.
    /// </summary>
    public bool? Eviction
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Eviction));
        }
    }

    /// <summary>
    /// Gets or sets whether TLS should be enabled.
    /// </summary>
    public bool? Tls
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(Tls));
        }
    }

    internal IReadOnlySet<string> ExplicitSettings => new HashSet<string>(_explicitSettings);

    internal void Validate()
    {
        if (Tls == false)
        {
            throw new InvalidOperationException("Upstash Redis requires TLS for v1 deployments. Set TLS to true or leave it unset.");
        }
    }

    private void TrackExplicitSetting(string settingName)
    {
        _explicitSettings.Add(settingName);
    }
}
