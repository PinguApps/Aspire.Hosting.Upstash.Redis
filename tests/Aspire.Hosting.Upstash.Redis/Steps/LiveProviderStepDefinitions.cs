using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class LiveProviderStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;

    public LiveProviderStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("live Upstash credentials are available")]
    public void GivenLiveUpstashCredentialsAreAvailable()
    {
        Assert.False(string.IsNullOrWhiteSpace(_context.LiveUpstash.AccountEmail));
        Assert.False(string.IsNullOrWhiteSpace(_context.LiveUpstash.ApiKey));
    }

    [Then("live Upstash cleanup is registered through the shared cleanup path")]
    public void ThenLiveUpstashCleanupIsRegisteredThroughTheSharedCleanupPath()
    {
        _context.LiveUpstash.RegisterCleanup(static () => Task.CompletedTask);

        Assert.Equal(1, _context.LiveUpstash.CleanupActionCount);
    }
}
