namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes how deployment should treat an Upstash Redis database with the requested name.
/// </summary>
public enum UpstashRedisOwnershipMode
{
    /// <summary>
    /// Deployment must create a new database and fail if one already exists.
    /// </summary>
    CreateOnly,

    /// <summary>
    /// Deployment must use an existing database and fail if it cannot be found.
    /// </summary>
    ExistingOnly,

    /// <summary>
    /// Deployment may create a missing database or adopt an existing database with the requested name.
    /// </summary>
    CreateOrAdopt
}
