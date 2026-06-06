using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

[Binding]
public sealed class LiveUpstashScenarioHooks
{
    private readonly UpstashRedisScenarioContext _context;

    public LiveUpstashScenarioHooks(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [BeforeScenario("live-upstash")]
    public void SkipLiveScenarioWithoutCredentials()
    {
        Assert.SkipUnless(
            _context.LiveUpstash.HasCredentials,
            "Live Upstash scenarios require UPSTASH_EMAIL and UPSTASH_API_KEY.");
    }

    [AfterScenario("live-upstash")]
    public Task RunLiveScenarioCleanup()
    {
        return _context.LiveUpstash.CleanupAsync();
    }
}
