using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Captures the Upstash deployment intent attached to a standard Aspire Redis resource.
/// </summary>
/// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
/// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
/// <param name="accountEmail">The infrastructure-only Upstash account email parameter.</param>
/// <param name="apiKey">The infrastructure-only Upstash API key parameter.</param>
/// <param name="options">The optional deployment settings and explicit-setting tracking.</param>
public sealed class UpstashRedisDeploymentAnnotation(
    string databaseName,
    UpstashRedisOwnershipMode ownershipMode,
    ParameterResource accountEmail,
    ParameterResource apiKey,
    UpstashRedisDeploymentOptions options) : IResourceAnnotation
{
    /// <summary>
    /// Gets the explicit remote Upstash Redis database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// Gets the requested ownership mode.
    /// </summary>
    public UpstashRedisOwnershipMode OwnershipMode { get; } = ownershipMode;

    /// <summary>
    /// Gets the infrastructure-only Upstash account email parameter.
    /// </summary>
    public ParameterResource AccountEmail { get; } = accountEmail;

    /// <summary>
    /// Gets the infrastructure-only Upstash API key parameter.
    /// </summary>
    public ParameterResource ApiKey { get; } = apiKey;

    /// <summary>
    /// Gets the optional deployment settings and explicit-setting tracking.
    /// </summary>
    public UpstashRedisDeploymentOptions Options { get; } = options;
}
