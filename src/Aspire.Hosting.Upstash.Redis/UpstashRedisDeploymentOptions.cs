namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Optional Upstash Redis settings that should be reconciled only when explicitly configured.
/// </summary>
public sealed class UpstashRedisDeploymentOptions
{
    private readonly HashSet<string> _explicitSettings = [];

    /// <summary>
    /// Gets or sets the Upstash platform or cloud provider.
    /// </summary>
    public string? Platform
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
    public string? PrimaryRegion
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
    public IReadOnlyList<string>? ReadRegions
    {
        get;
        set
        {
            field = value;
            TrackExplicitSetting(nameof(ReadRegions));
        }
    }

    /// <summary>
    /// Gets or sets the Upstash plan.
    /// </summary>
    public string? Plan
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
    public string? Budget
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

    /// <summary>
    /// Gets the option property names explicitly set by the caller.
    /// </summary>
    public IReadOnlySet<string> ExplicitSettings => new HashSet<string>(_explicitSettings);

    private void TrackExplicitSetting(string settingName)
    {
        _explicitSettings.Add(settingName);
    }
}
