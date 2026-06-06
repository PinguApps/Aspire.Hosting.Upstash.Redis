using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class LiveProviderStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;
    private bool _olderCleanupActionRan;
    private Exception? _cleanupFailure;

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

    [Given("live Upstash cleanup has an older action registered")]
    public void GivenLiveUpstashCleanupHasAnOlderActionRegistered()
    {
        _context.LiveUpstash.RegisterCleanup(() =>
        {
            _olderCleanupActionRan = true;

            return Task.CompletedTask;
        });
    }

    [Given("live Upstash cleanup has a newer failing action registered")]
    public void GivenLiveUpstashCleanupHasANewerFailingActionRegistered()
    {
        _context.LiveUpstash.RegisterCleanup(static () => throw new InvalidOperationException("Cleanup failed."));
    }

    [When("live Upstash cleanup runs")]
    public async Task WhenLiveUpstashCleanupRuns()
    {
        _cleanupFailure = await Record.ExceptionAsync(_context.LiveUpstash.CleanupAsync);
    }

    [Then("the older live Upstash cleanup action ran")]
    public void ThenTheOlderLiveUpstashCleanupActionRan()
    {
        Assert.True(_olderCleanupActionRan);
    }

    [Then("the live Upstash cleanup failure is reported")]
    public void ThenTheLiveUpstashCleanupFailureIsReported()
    {
        InvalidOperationException failure = Assert.IsType<InvalidOperationException>(_cleanupFailure);

        Assert.Equal("Cleanup failed.", failure.Message);
    }
}
