namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisDeploymentState
{
    public UpstashRedisDeploymentState(
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

        options.Validate();

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        AccountEmail = accountEmail;
        ApiKey = apiKey;
        OptionsSnapshot = new UpstashRedisDeploymentOptions(options);
    }

    public UpstashRedisValue DatabaseName
    {
        get;
    }

    public UpstashRedisOwnershipMode OwnershipMode
    {
        get;
    }

    public UpstashRedisValue AccountEmail
    {
        get;
    }

    public UpstashRedisValue ApiKey
    {
        get;
    }

    public UpstashRedisDeploymentOptions Options => new(OptionsSnapshot);

    private UpstashRedisDeploymentOptions OptionsSnapshot
    {
        get;
    }
}
