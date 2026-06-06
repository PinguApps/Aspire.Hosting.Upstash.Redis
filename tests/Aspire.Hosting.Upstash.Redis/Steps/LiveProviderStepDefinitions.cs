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

    [Given("live Upstash cleanup action {string} succeeds")]
    public void GivenLiveUpstashCleanupActionSucceeds(string actionName)
    {
        _context.LiveUpstash.RegisterCleanup(() =>
        {
            _context.LiveCleanupLog.Add(actionName);
            return Task.CompletedTask;
        });
    }

    [Given("live Upstash cleanup action {string} fails")]
    public void GivenLiveUpstashCleanupActionFails(string actionName)
    {
        _context.LiveUpstash.RegisterCleanup(() =>
        {
            _context.LiveCleanupLog.Add(actionName);
            throw new InvalidOperationException(actionName);
        });
    }

    [When("a null live Upstash cleanup action is registered")]
    public void WhenANullLiveUpstashCleanupActionIsRegistered()
    {
        _context.LastCleanupException = Record.Exception(() => _context.LiveUpstash.RegisterCleanup(null!));
    }

    [When("live Upstash cleanup runs")]
    public async Task WhenLiveUpstashCleanupRuns()
    {
        _cleanupFailure = await Record.ExceptionAsync(_context.LiveUpstash.CleanupAsync);
        _context.LastCleanupException = _cleanupFailure;
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

    [Then("live Upstash cleanup registration fails for a null cleanup action")]
    public void ThenLiveUpstashCleanupRegistrationFailsForANullCleanupAction()
    {
        Assert.IsType<ArgumentNullException>(_context.LastCleanupException);
    }

    [Then("every live Upstash cleanup action has run")]
    public void ThenEveryLiveUpstashCleanupActionHasRun()
    {
        Assert.Equal(["third", "second", "first"], _context.LiveCleanupLog);
        Assert.Equal(0, _context.LiveUpstash.CleanupActionCount);
    }

    [Then("live Upstash cleanup reports {int} failures")]
    public void ThenLiveUpstashCleanupReportsFailures(int failureCount)
    {
        AggregateException exception = Assert.IsType<AggregateException>(_context.LastCleanupException);

        Assert.Equal(failureCount, exception.InnerExceptions.Count);
    }
}
