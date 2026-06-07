namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisRemoteIdentityState
{
    public UpstashRedisRemoteIdentityState(string databaseName, string providerDatabaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerDatabaseId);

        DatabaseName = databaseName;
        ProviderDatabaseId = providerDatabaseId;
    }

    public string DatabaseName { get; }

    public string ProviderDatabaseId { get; }
}
