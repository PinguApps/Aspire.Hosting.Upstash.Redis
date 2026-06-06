namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes the cloud platform used by an Upstash Redis database.
/// </summary>
public enum UpstashRedisCloudPlatform
{
    /// <summary>
    /// Amazon Web Services.
    /// </summary>
    Aws,

    /// <summary>
    /// Google Cloud Platform.
    /// </summary>
    Gcp
}
