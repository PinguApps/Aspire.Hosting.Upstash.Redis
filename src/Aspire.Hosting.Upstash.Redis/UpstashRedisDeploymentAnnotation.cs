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
    /// <param name="accountEmail">The infrastructure-only Upstash account email value.</param>
    /// <param name="apiKey">The infrastructure-only Upstash API key value.</param>
    /// <param name="options">The optional deployment settings and explicit-setting tracking.</param>
    public UpstashRedisDeploymentAnnotation(
        UpstashRedisValue databaseName,
        UpstashRedisOwnershipMode ownershipMode,
        UpstashRedisValue accountEmail,
        UpstashRedisValue apiKey,
        UpstashRedisDeploymentOptions options)
    {
        ArgumentNullException.ThrowIfNull(databaseName);
        ArgumentNullException.ThrowIfNull(accountEmail);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Upstash Redis ownership mode is not supported.");
        }

        options.ToProviderOptions();

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        AccountEmail = accountEmail;
        ApiKey = apiKey;
        OptionsSnapshot = new UpstashRedisDeploymentOptions(options);
    }

    /// <summary>
    /// Gets the explicit remote Upstash Redis database name.
    /// </summary>
    public UpstashRedisValue DatabaseName
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
    /// Gets the infrastructure-only Upstash account email value.
    /// </summary>
    public UpstashRedisValue AccountEmail
    {
        get;
    }

    /// <summary>
    /// Gets the infrastructure-only Upstash API key value.
    /// </summary>
    public UpstashRedisValue ApiKey
    {
        get;
    }

    /// <summary>
    /// Gets the optional deployment settings and explicit-setting tracking.
    /// </summary>
    public UpstashRedisDeploymentOptions Options => new(OptionsSnapshot);

    private UpstashRedisDeploymentOptions OptionsSnapshot
    {
        get;
    }
}
