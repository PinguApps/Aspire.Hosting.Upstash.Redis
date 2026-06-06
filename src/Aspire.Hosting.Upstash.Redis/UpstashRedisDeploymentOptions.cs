#pragma warning disable IDE0032

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Optional Upstash Redis settings that should be reconciled only when explicitly configured.
/// </summary>
public sealed class UpstashRedisDeploymentOptions
{
    private readonly HashSet<string> _explicitSettings = [];
    private string? _platform;
    private string? _primaryRegion;
    private IReadOnlyList<string>? _readRegions;
    private string? _plan;
    private string? _budget;
    private bool? _eviction;
    private bool? _tls;

    /// <summary>
    /// Gets or sets the Upstash platform or cloud provider.
    /// </summary>
    public string? Platform
    {
        get => _platform;
        set
        {
            _platform = value;
            TrackExplicitSetting(nameof(Platform));
        }
    }

    /// <summary>
    /// Gets or sets the primary Upstash region.
    /// </summary>
    public string? PrimaryRegion
    {
        get => _primaryRegion;
        set
        {
            _primaryRegion = value;
            TrackExplicitSetting(nameof(PrimaryRegion));
        }
    }

    /// <summary>
    /// Gets or sets optional read regions.
    /// </summary>
    public IReadOnlyList<string>? ReadRegions
    {
        get => _readRegions;
        set
        {
            _readRegions = value;
            TrackExplicitSetting(nameof(ReadRegions));
        }
    }

    /// <summary>
    /// Gets or sets the Upstash plan.
    /// </summary>
    public string? Plan
    {
        get => _plan;
        set
        {
            _plan = value;
            TrackExplicitSetting(nameof(Plan));
        }
    }

    /// <summary>
    /// Gets or sets the budget setting.
    /// </summary>
    public string? Budget
    {
        get => _budget;
        set
        {
            _budget = value;
            TrackExplicitSetting(nameof(Budget));
        }
    }

    /// <summary>
    /// Gets or sets whether eviction is enabled.
    /// </summary>
    public bool? Eviction
    {
        get => _eviction;
        set
        {
            _eviction = value;
            TrackExplicitSetting(nameof(Eviction));
        }
    }

    /// <summary>
    /// Gets or sets whether TLS should be enabled.
    /// </summary>
    public bool? Tls
    {
        get => _tls;
        set
        {
            _tls = value;
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
