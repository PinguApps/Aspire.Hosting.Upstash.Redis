namespace Aspire.Hosting.Upstash.Redis.Management;

internal interface IUpstashRedisManagementClient
{
    public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken);

    public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken);

    public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken);

    public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(UpstashRedisCreateDatabaseRequest request, CancellationToken cancellationToken);

    public Task UpdateReadRegionsAsync(string databaseId, UpstashRedisUpdateRegionsRequest request, CancellationToken cancellationToken);

    public Task ChangePlanAsync(string databaseId, UpstashRedisChangePlanRequest request, CancellationToken cancellationToken);

    public Task UpdateBudgetAsync(string databaseId, UpstashRedisUpdateBudgetRequest request, CancellationToken cancellationToken);

    public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken);

    public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
        string databaseId,
        UpstashRedisReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken);
}
