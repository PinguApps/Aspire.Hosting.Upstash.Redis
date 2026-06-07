using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisRemoteIdentityResolution
{
    private UpstashRedisRemoteIdentityResolution(
        UpstashRedisDatabaseDetails? database,
        UpstashRedisRemoteIdentityState? identityState)
    {
        Database = database;
        IdentityState = identityState;
    }

    public UpstashRedisDatabaseDetails? Database { get; }

    public UpstashRedisRemoteIdentityState? IdentityState { get; }

    public bool Found => Database is not null;

    public static UpstashRedisRemoteIdentityResolution NotFound() => new(null, null);

    public static UpstashRedisRemoteIdentityResolution FoundDatabase(UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return new(
            database,
            new UpstashRedisRemoteIdentityState(database.DatabaseName, database.DatabaseId));
    }
}
