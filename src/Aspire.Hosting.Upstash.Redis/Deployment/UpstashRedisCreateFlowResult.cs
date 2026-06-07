using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisCreateFlowResult
{
    public UpstashRedisCreateFlowResult(UpstashRedisDatabaseDetails database, bool created)
    {
        ArgumentNullException.ThrowIfNull(database);

        Database = database;
        Created = created;
    }

    public UpstashRedisDatabaseDetails Database { get; }

    public bool Created { get; }
}
