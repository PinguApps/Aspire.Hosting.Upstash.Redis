using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisOwnershipResolutionRequest
{
    public UpstashRedisOwnershipResolutionRequest(
        string databaseName,
        UpstashRedisOwnershipMode ownershipMode,
        UpstashRedisProviderDeploymentOptions options,
        UpstashRedisDatabaseDetails? existingDatabase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Upstash Redis ownership mode is not supported.");
        }

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        Options = options;
        ExistingDatabase = existingDatabase;
    }

    public string DatabaseName { get; }

    public UpstashRedisOwnershipMode OwnershipMode { get; }

    public UpstashRedisProviderDeploymentOptions Options { get; }

    public UpstashRedisDatabaseDetails? ExistingDatabase { get; }
}
