namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

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
