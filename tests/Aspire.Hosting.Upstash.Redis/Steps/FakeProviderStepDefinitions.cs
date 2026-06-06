using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class FakeProviderStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;

    public FakeProviderStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("the fake Upstash provider contains database {string} in region {string}")]
    public void GivenTheFakeUpstashProviderContainsDatabaseInRegion(string databaseName, string primaryRegion)
    {
        _context.FakeProvider.AddDatabase(
            new FakeUpstashRedisDatabase(
                databaseName,
                $"db-{databaseName}",
                primaryRegion,
                "global-apt-1.upstash.io",
                6379,
                "test-password",
                tlsEnabled: true));
    }

    [When("the fake Upstash provider is asked to find database {string}")]
    public void WhenTheFakeUpstashProviderIsAskedToFindDatabase(string databaseName)
    {
        _context.LastProviderDatabase = _context.FakeProvider.FindByName(databaseName);
    }

    [Then("the fake Upstash provider returns database {string}")]
    public void ThenTheFakeUpstashProviderReturnsDatabase(string databaseName)
    {
        Assert.NotNull(_context.LastProviderDatabase);
        Assert.Equal(databaseName, _context.LastProviderDatabase.DatabaseName);
    }

    [Then("the fake Upstash provider recorded a {string} interaction for database {string}")]
    public void ThenTheFakeUpstashProviderRecordedAnInteractionForDatabase(string interactionName, string databaseName)
    {
        FakeUpstashProviderInteraction interaction = Assert.Single(_context.FakeProvider.Interactions);

        Assert.Equal(interactionName, interaction.Name);
        Assert.Equal(databaseName, interaction.DatabaseName);
    }
}
