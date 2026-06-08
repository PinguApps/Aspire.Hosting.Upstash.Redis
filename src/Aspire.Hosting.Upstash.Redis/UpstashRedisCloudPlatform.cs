namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes the cloud platform used by an Upstash Redis database.
/// </summary>
public enum UpstashRedisCloudPlatform
{
    /// <summary>
    /// Amazon Web Services.
    /// </summary>
    [AspireValue("upstashRedisCloudPlatform", Name = "aws")]
    Aws,

    /// <summary>
    /// Google Cloud Platform.
    /// </summary>
    [AspireValue("upstashRedisCloudPlatform", Name = "gcp")]
    Gcp
}
