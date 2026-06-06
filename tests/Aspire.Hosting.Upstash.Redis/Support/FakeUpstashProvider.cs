namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal sealed class FakeUpstashProvider
{
    private readonly List<FakeUpstashRedisDatabase> _databases = [];
    private readonly List<FakeUpstashProviderInteraction> _interactions = [];

    public IReadOnlyList<FakeUpstashRedisDatabase> Databases => _databases;

    public IReadOnlyList<FakeUpstashProviderInteraction> Interactions => _interactions;

    public void AddDatabase(FakeUpstashRedisDatabase database)
    {
        _databases.Add(database);
    }

    public FakeUpstashRedisDatabase? FindByName(string databaseName)
    {
        _interactions.Add(new FakeUpstashProviderInteraction("find-by-name", databaseName));

        return _databases.SingleOrDefault(database => database.DatabaseName == databaseName);
    }
}
