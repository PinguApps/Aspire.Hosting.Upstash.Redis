using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisOwnershipResolutionResult
{
    private UpstashRedisOwnershipResolutionResult(
        UpstashRedisOwnershipResolutionAction action,
        UpstashRedisDatabaseDetails? database)
    {
        Action = action;
        Database = database;
    }

    public UpstashRedisOwnershipResolutionAction Action { get; }

    public UpstashRedisDatabaseDetails? Database { get; }

    public static UpstashRedisOwnershipResolutionResult Create()
    {
        return new(UpstashRedisOwnershipResolutionAction.Create, database: null);
    }

    public static UpstashRedisOwnershipResolutionResult Adopt(UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return new(UpstashRedisOwnershipResolutionAction.Adopt, database);
    }
}
