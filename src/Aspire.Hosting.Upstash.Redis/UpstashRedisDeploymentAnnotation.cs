using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Captures the Upstash deployment intent attached to a standard Aspire Redis resource.
/// </summary>
public sealed class UpstashRedisDeploymentAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpstashRedisDeploymentAnnotation"/> class.
    /// </summary>
    /// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
    /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
    /// <param name="accountEmail">The infrastructure-only Upstash account email parameter.</param>
    /// <param name="apiKey">The infrastructure-only Upstash API key parameter.</param>
    /// <param name="options">The optional deployment settings and explicit-setting tracking.</param>
    public UpstashRedisDeploymentAnnotation(
        string databaseName,
        UpstashRedisOwnershipMode ownershipMode,
        ParameterResource accountEmail,
        ParameterResource apiKey,
        UpstashRedisDeploymentOptions options)
    {
        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        AccountEmail = accountEmail;
        ApiKey = apiKey;
        Options = options;
    }

    /// <summary>
    /// Gets the explicit remote Upstash Redis database name.
    /// </summary>
    public string DatabaseName
    {
        get;
    }

    /// <summary>
    /// Gets the requested ownership mode.
    /// </summary>
    public UpstashRedisOwnershipMode OwnershipMode
    {
        get;
    }

    /// <summary>
    /// Gets the infrastructure-only Upstash account email parameter.
    /// </summary>
    public ParameterResource AccountEmail
    {
        get;
    }

    /// <summary>
    /// Gets the infrastructure-only Upstash API key parameter.
    /// </summary>
    public ParameterResource ApiKey
    {
        get;
    }

    /// <summary>
    /// Gets the optional deployment settings and explicit-setting tracking.
    /// </summary>
    public UpstashRedisDeploymentOptions Options
    {
        get;
    }
}
