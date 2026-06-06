namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes an Upstash Redis plan accepted by the management API.
/// </summary>
public enum UpstashRedisPlan
{
    /// <summary>
    /// Upstash free plan.
    /// </summary>
    Free,

    /// <summary>
    /// Upstash pay-as-you-go plan.
    /// </summary>
    PayAsYouGo,

    /// <summary>
    /// Fixed 250 MB plan.
    /// </summary>
    Fixed250Mb,

    /// <summary>
    /// Fixed 1 GB plan.
    /// </summary>
    Fixed1Gb,

    /// <summary>
    /// Fixed 5 GB plan.
    /// </summary>
    Fixed5Gb,

    /// <summary>
    /// Fixed 10 GB plan.
    /// </summary>
    Fixed10Gb,

    /// <summary>
    /// Fixed 50 GB plan.
    /// </summary>
    Fixed50Gb,

    /// <summary>
    /// Fixed 100 GB plan.
    /// </summary>
    Fixed100Gb,

    /// <summary>
    /// Fixed 500 GB plan.
    /// </summary>
    Fixed500Gb
}
