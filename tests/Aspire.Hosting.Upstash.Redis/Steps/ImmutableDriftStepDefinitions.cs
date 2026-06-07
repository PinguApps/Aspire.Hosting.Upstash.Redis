using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class ImmutableDriftStepDefinitions
{
    private UpstashRedisDatabaseDetails? _existingDatabase;
    private UpstashRedisImmutableDrift? _drift;
    private Exception? _exception;

    [Given("an existing Upstash Redis database detail named {string} in region {string} with TLS enabled")]
    public void GivenAnExistingUpstashRedisDatabaseDetailNamedInRegionWithTlsEnabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: true);
    }

    [Given("an existing Upstash Redis database detail named {string} in region {string} with TLS disabled")]
    public void GivenAnExistingUpstashRedisDatabaseDetailNamedInRegionWithTlsDisabled(string databaseName, string primaryRegion)
    {
        SetExistingDatabase(databaseName, primaryRegion, tls: false);
    }

    [When("immutable drift is checked for requested database {string} with default options")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithDefaultOptions(string databaseName)
    {
        CheckDrift(databaseName, _ => { });
    }

    [When("immutable drift is checked for requested database {string} with platform {string}")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithPlatform(string databaseName, string platform)
    {
        CheckDrift(databaseName, options => options.Platform = platform);
    }

    [When("immutable drift is checked for requested database {string} with primary region {string}")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithPrimaryRegion(string databaseName, string primaryRegion)
    {
        CheckDrift(databaseName, options => options.PrimaryRegion = primaryRegion);
    }

    [When("immutable drift is checked for requested database {string} with mutable settings")]
    public void WhenImmutableDriftIsCheckedForRequestedDatabaseWithMutableSettings(string databaseName)
    {
        CheckDrift(
            databaseName,
            options =>
            {
                options.SetReadRegions(UpstashRedisRegion.AwsEuWest2);
                options.SetPlan(UpstashRedisPlan.PayAsYouGo);
                options.SetBudget(360);
                options.Eviction = false;
            });
    }

    [Then("immutable drift detection succeeds")]
    public void ThenImmutableDriftDetectionSucceeds()
    {
        Assert.Null(_exception);
        Assert.Null(_drift);
    }

    [Then("immutable drift detection fails because {string}")]
    public void ThenImmutableDriftDetectionFailsBecause(string failureReason)
    {
        UpstashRedisImmutableDriftException exception = Assert.IsType<UpstashRedisImmutableDriftException>(_exception);
        UpstashRedisImmutableDrift drift =
            exception.Drift ?? throw new InvalidOperationException("Immutable drift exception did not include drift details.");

        Assert.Equal(Enum.Parse<UpstashRedisImmutableDriftFailureReason>(failureReason), drift.FailureReason);
        _drift = drift;
    }

    [Then("the immutable drift failure message contains {string}")]
    public void ThenTheImmutableDriftFailureMessageContains(string expectedText)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("Immutable drift detection did not fail.");

        Assert.Contains(expectedText, exception.Message, StringComparison.Ordinal);
    }

    private void SetExistingDatabase(string databaseName, string primaryRegion, bool tls)
    {
        _existingDatabase = new UpstashRedisDatabaseDetails
        {
            DatabaseId = $"db-{databaseName}",
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.upstash.io",
            Port = 6379,
            Password = "test-password",
            PrimaryRegion = primaryRegion,
            Tls = tls,
        };
    }

    private void CheckDrift(string databaseName, Action<UpstashRedisDeploymentOptions> configure)
    {
        UpstashRedisDatabaseDetails existingDatabase =
            _existingDatabase ?? throw new InvalidOperationException("The existing database detail has not been configured.");

        UpstashRedisDeploymentOptions options = new();
        configure(options);

        _exception = Record.Exception(() =>
            UpstashRedisImmutableDriftDetector.Validate(
                databaseName,
                options.ToProviderOptions(),
                existingDatabase));

        _drift = (_exception as UpstashRedisImmutableDriftException)?.Drift;
    }
}
