using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class OwnershipResolutionStepDefinitions
{
    private readonly FakeOwnershipManagementClient _client = new();
    private UpstashRedisOwnershipResolutionResult? _result;
    private Exception? _exception;

    [Given("the Upstash ownership resolver finds no database named {string}")]
    public void GivenTheUpstashOwnershipResolverFindsNoDatabaseNamed(string databaseName)
    {
        _client.DatabaseName = databaseName;
        _client.Database = null;
    }

    [Given("the Upstash ownership resolver finds database {string} in region {string} with TLS enabled")]
    public void GivenTheUpstashOwnershipResolverFindsDatabaseInRegionWithTlsEnabled(string databaseName, string primaryRegion)
    {
        ConfigureFoundDatabase(databaseName, primaryRegion, tls: true);
    }

    [Given("the Upstash ownership resolver finds database {string} in region {string} with TLS disabled")]
    public void GivenTheUpstashOwnershipResolverFindsDatabaseInRegionWithTlsDisabled(string databaseName, string primaryRegion)
    {
        ConfigureFoundDatabase(databaseName, primaryRegion, tls: false);
    }

    [When("ownership is resolved for database {string} with mode {string}")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithMode(string databaseName, string ownershipMode)
    {
        await ResolveAsync(databaseName, ownershipMode, options => options.Tls = true).ConfigureAwait(false);
    }

    [When("ownership is resolved for database {string} with mode {string} and TLS unset")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithModeAndTlsUnset(string databaseName, string ownershipMode)
    {
        await ResolveAsync(databaseName, ownershipMode, _ => { }).ConfigureAwait(false);
    }

    [When("ownership is resolved for database {string} with mode {string} and primary region {string}")]
    public async Task WhenOwnershipIsResolvedForDatabaseWithModeAndPrimaryRegion(
        string databaseName,
        string ownershipMode,
        string primaryRegion)
    {
        await ResolveAsync(
            databaseName,
            ownershipMode,
            options =>
            {
                options.PrimaryRegion = primaryRegion;
                options.Tls = true;
            }).ConfigureAwait(false);
    }

    [Then("the ownership resolver selects the {string} path")]
    public void ThenTheOwnershipResolverSelectsThePath(string action)
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);
        Assert.Equal(Enum.Parse<UpstashRedisOwnershipResolutionAction>(action), _result.Action);
    }

    [Then("the ownership resolver selected database {string}")]
    public void ThenTheOwnershipResolverSelectedDatabase(string databaseName)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Database);
        Assert.Equal(databaseName, _result.Database.DatabaseName);
    }

    [Then("the ownership resolver looked up database {string}")]
    public void ThenTheOwnershipResolverLookedUpDatabase(string databaseName)
    {
        string lookup = Assert.Single(_client.Lookups);
        Assert.Equal(databaseName, lookup);
    }

    [Then("ownership resolution fails because {string}")]
    public void ThenOwnershipResolutionFailsBecause(string failureReason)
    {
        UpstashRedisOwnershipResolutionException exception = Assert.IsType<UpstashRedisOwnershipResolutionException>(_exception);
        Assert.Equal(Enum.Parse<UpstashRedisOwnershipResolutionFailureReason>(failureReason), exception.FailureReason);
    }

    [Then("the ownership failure message contains {string}")]
    public void ThenTheOwnershipFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    private void ConfigureFoundDatabase(string databaseName, string primaryRegion, bool tls)
    {
        _client.DatabaseName = databaseName;
        _client.Database = new UpstashRedisDatabaseDetails
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

    private async Task ResolveAsync(
        string databaseName,
        string ownershipMode,
        Action<UpstashRedisDeploymentOptions> configure)
    {
        UpstashRedisDeploymentOptions options = new();
        configure(options);

        UpstashRedisOwnershipResolutionRequest request = new(
            databaseName,
            Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode),
            options.ToProviderOptions());

        _exception = await Record.ExceptionAsync(async () =>
            _result = await UpstashRedisOwnershipResolver
                .ResolveAsync(request, _client, CancellationToken.None)
                .ConfigureAwait(false)).ConfigureAwait(false);
    }

    private sealed class FakeOwnershipManagementClient : IUpstashRedisManagementClient
    {
        public string? DatabaseName { get; set; }

        public UpstashRedisDatabaseDetails? Database { get; set; }

        public List<string> Lookups { get; } = [];

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            Lookups.Add(databaseName);

            return Task.FromResult(string.Equals(databaseName, DatabaseName, StringComparison.Ordinal)
                ? Database
                : null);
        }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(UpstashRedisCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateReadRegionsAsync(string databaseId, UpstashRedisUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ChangePlanAsync(string databaseId, UpstashRedisChangePlanRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateBudgetAsync(string databaseId, UpstashRedisUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
