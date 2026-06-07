using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisResolvedDeployment
{
    public UpstashRedisResolvedDeployment(
        string databaseName,
        UpstashRedisOwnershipMode ownershipMode,
        UpstashRedisManagementCredentials managementCredentials,
        UpstashRedisProviderDeploymentOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(managementCredentials);
        ArgumentNullException.ThrowIfNull(options);

        DatabaseName = databaseName;
        OwnershipMode = ownershipMode;
        ManagementCredentials = managementCredentials;
        Options = options;
    }

    public string DatabaseName { get; }

    public UpstashRedisOwnershipMode OwnershipMode { get; }

    public UpstashRedisManagementCredentials ManagementCredentials { get; }

    public UpstashRedisProviderDeploymentOptions Options { get; }
}
