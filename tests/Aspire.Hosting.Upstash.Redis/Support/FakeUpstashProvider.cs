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

internal sealed class FakeUpstashProviderInteraction
{
    public FakeUpstashProviderInteraction(string name, string databaseName)
    {
        Name = name;
        DatabaseName = databaseName;
    }

    public string Name
    {
        get;
    }

    public string DatabaseName
    {
        get;
    }
}

internal sealed class FakeUpstashRedisDatabase
{
    public FakeUpstashRedisDatabase(
        string databaseName,
        string databaseId,
        string primaryRegion,
        string endpoint,
        int port,
        string password,
        bool tlsEnabled)
    {
        DatabaseName = databaseName;
        DatabaseId = databaseId;
        PrimaryRegion = primaryRegion;
        Endpoint = endpoint;
        Port = port;
        Password = password;
        TlsEnabled = tlsEnabled;
    }

    public string DatabaseName
    {
        get;
    }

    public string DatabaseId
    {
        get;
    }

    public string PrimaryRegion
    {
        get;
    }

    public string Endpoint
    {
        get;
    }

    public int Port
    {
        get;
    }

    public string Password
    {
        get;
    }

    public bool TlsEnabled
    {
        get;
    }
}
