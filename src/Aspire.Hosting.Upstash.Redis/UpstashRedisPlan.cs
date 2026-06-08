namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes an Upstash Redis plan accepted by the management API.
/// </summary>
public enum UpstashRedisPlan
{
    /// <summary>
    /// Upstash free plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "free")]
    Free,

    /// <summary>
    /// Upstash pay-as-you-go plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "payAsYouGo")]
    PayAsYouGo,

    /// <summary>
    /// Fixed 250 MB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed250Mb")]
    Fixed250Mb,

    /// <summary>
    /// Fixed 1 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed1Gb")]
    Fixed1Gb,

    /// <summary>
    /// Fixed 5 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed5Gb")]
    Fixed5Gb,

    /// <summary>
    /// Fixed 10 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed10Gb")]
    Fixed10Gb,

    /// <summary>
    /// Fixed 50 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed50Gb")]
    Fixed50Gb,

    /// <summary>
    /// Fixed 100 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed100Gb")]
    Fixed100Gb,

    /// <summary>
    /// Fixed 500 GB plan.
    /// </summary>
    [AspireValue("upstashRedisPlan", Name = "fixed500Gb")]
    Fixed500Gb
}
