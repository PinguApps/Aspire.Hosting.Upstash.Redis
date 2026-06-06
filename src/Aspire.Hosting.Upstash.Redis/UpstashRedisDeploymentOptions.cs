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
    /// Sets the Upstash platform or cloud provider from the public provider enum.
    /// </summary>
    /// <param name="platform">The Upstash Redis cloud platform.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="platform"/> is not supported.</exception>
    public void SetPlatform(UpstashRedisCloudPlatform platform)
    {
        Platform = UpstashRedisProviderDomain.MapCloudPlatform(platform);
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
    /// Sets the primary Upstash region from the public region enum.
    /// </summary>
    /// <param name="region">The primary Upstash Redis region.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="region"/> is not supported.</exception>
    public void SetPrimaryRegion(UpstashRedisRegion region)
    {
        PrimaryRegion = UpstashRedisProviderDomain.MapRegion(region);
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
    /// Sets optional read regions from the public region enum.
    /// </summary>
    /// <param name="regions">The Upstash Redis read regions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regions"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A supplied region is not supported.</exception>
    public void SetReadRegions(params UpstashRedisRegion[] regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        ReadRegions = Array.AsReadOnly(regions
            .Select(static region => UpstashRedisValue.FromString(UpstashRedisProviderDomain.MapRegion(region)))
            .ToArray());
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
    /// Sets the Upstash plan from the public plan enum.
    /// </summary>
    /// <param name="plan">The Upstash Redis plan.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="plan"/> is not supported.</exception>
    public void SetPlan(UpstashRedisPlan plan)
    {
        Plan = UpstashRedisProviderDomain.MapPlan(plan);
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
    /// Sets the monthly Upstash budget.
    /// </summary>
    /// <param name="budget">The monthly budget.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="budget"/> is less than or equal to zero.</exception>
    public void SetBudget(int budget)
    {
        if (budget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), budget, "The Upstash Redis budget must be a positive integer.");
        }

        Budget = budget.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    internal UpstashRedisProviderDeploymentOptions ToProviderOptions()
    {
        Validate();

        UpstashRedisProviderValue? platform = null;
        UpstashRedisProviderValue? primaryRegion = null;
        IReadOnlyList<UpstashRedisProviderValue>? readRegions = null;
        UpstashRedisProviderValue? plan = null;
        UpstashRedisProviderValue? budget = null;
        UpstashRedisProviderValue? eviction = null;
        UpstashRedisProviderValue? tls = null;

        UpstashRedisCloudPlatform? platformLiteral = null;
        UpstashRedisRegion? primaryRegionLiteral = null;
        UpstashRedisPlan? planLiteral = null;
        int? budgetLiteral = null;

        if (Platform is not null)
        {
            string? providerValue = null;

            if (Platform.LiteralValue is not null)
            {
                platformLiteral = UpstashRedisProviderDomain.ParseCloudPlatform(Platform.LiteralValue, "platform");
                providerValue = UpstashRedisProviderDomain.MapCloudPlatform(platformLiteral.Value);
            }

            platform = new(Platform, providerValue);
        }

        if (PrimaryRegion is not null)
        {
            string? providerValue = null;

            if (PrimaryRegion.LiteralValue is not null)
            {
                primaryRegionLiteral = UpstashRedisProviderDomain.ParsePrimaryRegion(PrimaryRegion.LiteralValue, "primary region");
                providerValue = UpstashRedisProviderDomain.MapRegion(primaryRegionLiteral.Value);
            }

            primaryRegion = new(PrimaryRegion, providerValue);
        }

        if (ReadRegions is not null)
        {
            List<UpstashRedisProviderValue> mappedReadRegions = [];
            HashSet<string> literalReadRegions = new(StringComparer.Ordinal);

            foreach (UpstashRedisValue readRegion in ReadRegions)
            {
                string? providerValue = null;

                if (readRegion.LiteralValue is not null)
                {
                    UpstashRedisRegion readRegionLiteral = UpstashRedisProviderDomain.ParseReadRegion(readRegion.LiteralValue, "read region");
                    providerValue = UpstashRedisProviderDomain.MapRegion(readRegionLiteral);

                    if (!literalReadRegions.Add(providerValue))
                    {
                        throw new InvalidOperationException($"Upstash Redis read region '{providerValue}' is configured more than once.");
                    }

                    ValidateReadRegionCombination(platformLiteral, primaryRegionLiteral, readRegionLiteral);
                }

                mappedReadRegions.Add(new(readRegion, providerValue));
            }

            readRegions = mappedReadRegions.AsReadOnly();
        }

        ValidatePrimaryRegionCombination(platformLiteral, primaryRegionLiteral);

        if (Plan is not null)
        {
            string? providerValue = null;

            if (Plan.LiteralValue is not null)
            {
                planLiteral = UpstashRedisProviderDomain.ParsePlan(Plan.LiteralValue, "plan");
                providerValue = UpstashRedisProviderDomain.MapPlan(planLiteral.Value);
            }

            plan = new(Plan, providerValue);
        }

        if (Budget is not null)
        {
            if (Budget.LiteralValue is not null)
            {
                budgetLiteral = UpstashRedisProviderDomain.ParseBudget(Budget.LiteralValue, "budget");
            }

            budget = new(Budget, budgetLiteral);
        }

        if (planLiteral is not null && planLiteral != UpstashRedisPlan.PayAsYouGo && budget is not null)
        {
            throw new InvalidOperationException("Upstash Redis budget can only be configured with the pay-as-you-go plan.");
        }

        if (Eviction is not null)
        {
            eviction = new(UpstashRedisValue.FromString(Eviction.Value.ToString()), Eviction.Value);
        }

        if (Tls is not null)
        {
            tls = new(UpstashRedisValue.FromString(Tls.Value.ToString()), Tls.Value);
        }

        return new UpstashRedisProviderDeploymentOptions(
            platform,
            primaryRegion,
            readRegions,
            plan,
            budget,
            eviction,
            tls,
            ExplicitSettings);
    }

    private static void ValidatePrimaryRegionCombination(
        UpstashRedisCloudPlatform? platformLiteral,
        UpstashRedisRegion? primaryRegionLiteral)
    {
        if (platformLiteral is null || primaryRegionLiteral is null)
        {
            return;
        }

        UpstashRedisCloudPlatform primaryRegionPlatform = UpstashRedisProviderDomain.GetCloudPlatform(primaryRegionLiteral.Value);

        if (platformLiteral.Value != primaryRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Upstash Redis primary region '{UpstashRedisProviderDomain.MapRegion(primaryRegionLiteral.Value)}' is a {UpstashRedisProviderDomain.MapCloudPlatform(primaryRegionPlatform)} region and cannot be used with platform '{UpstashRedisProviderDomain.MapCloudPlatform(platformLiteral.Value)}'.");
        }
    }

    private static void ValidateReadRegionCombination(
        UpstashRedisCloudPlatform? platformLiteral,
        UpstashRedisRegion? primaryRegionLiteral,
        UpstashRedisRegion readRegionLiteral)
    {
        UpstashRedisCloudPlatform readRegionPlatform = UpstashRedisProviderDomain.GetCloudPlatform(readRegionLiteral);

        if (platformLiteral is not null && platformLiteral.Value != readRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Upstash Redis read region '{UpstashRedisProviderDomain.MapRegion(readRegionLiteral)}' is a {UpstashRedisProviderDomain.MapCloudPlatform(readRegionPlatform)} region and cannot be used with platform '{UpstashRedisProviderDomain.MapCloudPlatform(platformLiteral.Value)}'.");
        }

        if (primaryRegionLiteral is null)
        {
            return;
        }

        if (primaryRegionLiteral.Value == readRegionLiteral)
        {
            throw new InvalidOperationException(
                $"Upstash Redis read region '{UpstashRedisProviderDomain.MapRegion(readRegionLiteral)}' cannot match the primary region.");
        }

        UpstashRedisCloudPlatform primaryRegionPlatform = UpstashRedisProviderDomain.GetCloudPlatform(primaryRegionLiteral.Value);

        if (primaryRegionPlatform != readRegionPlatform)
        {
            throw new InvalidOperationException(
                $"Upstash Redis read region '{UpstashRedisProviderDomain.MapRegion(readRegionLiteral)}' cannot be used with primary region '{UpstashRedisProviderDomain.MapRegion(primaryRegionLiteral.Value)}'.");
        }
    }

    private void TrackExplicitSetting(string settingName)
    {
        _explicitSettings.Add(settingName);
    }
}
