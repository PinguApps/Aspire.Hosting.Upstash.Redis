using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisRemoteIdentityResolution
{
    private UpstashRedisRemoteIdentityResolution(
        UpstashRedisDatabaseDetails? database,
        UpstashRedisRemoteIdentityState? identityState,
        bool resolvedFromCachedIdentity)
    {
        Database = database;
        IdentityState = identityState;
        ResolvedFromCachedIdentity = resolvedFromCachedIdentity;
    }

    public UpstashRedisDatabaseDetails? Database { get; }

    public UpstashRedisRemoteIdentityState? IdentityState { get; }

    public bool Found => Database is not null;

    public bool ResolvedFromCachedIdentity { get; }

    public static UpstashRedisRemoteIdentityResolution NotFound() => new(null, null, resolvedFromCachedIdentity: false);

    public static UpstashRedisRemoteIdentityResolution FoundDatabase(
        UpstashRedisDatabaseDetails database,
        bool resolvedFromCachedIdentity = false)
    {
        ArgumentNullException.ThrowIfNull(database);

        return new(
            database,
            new UpstashRedisRemoteIdentityState(database.DatabaseName, database.DatabaseId),
            resolvedFromCachedIdentity);
    }
}
