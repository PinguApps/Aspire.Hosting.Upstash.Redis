using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Management;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class LiveDeployOutputsStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;
    private string? _databaseName;
    private UpstashRedisDatabaseDetails? _firstDeploymentDatabase;
    private UpstashRedisDatabaseDetails? _secondDeploymentDatabase;
    private bool _cleanupRegistered;

    public LiveDeployOutputsStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("a live disposable Upstash Redis deployment with prefix {string}")]
    public async Task GivenALiveDisposableUpstashRedisDeploymentWithPrefix(string prefix)
    {
        _context.AddRedis("cache");

        _databaseName = LiveUpstashTestSession.CreateDisposableDatabaseName(prefix);
        _context.MarkRedisForUpstash(_databaseName, UpstashRedisOwnershipMode.CreateOrAdopt);

        await _context.LiveUpstash.RegisterDatabaseDeletionByNameAsync(_databaseName).ConfigureAwait(false);
        _cleanupRegistered = true;
    }

    [When("the live Upstash deployment runs")]
    public async Task WhenTheLiveUpstashDeploymentRuns()
    {
        _firstDeploymentDatabase = await RunDeploymentAsync(cachedIdentity: null).ConfigureAwait(false);
    }

    [When("the live Upstash deployment runs twice")]
    public async Task WhenTheLiveUpstashDeploymentRunsTwice()
    {
        _firstDeploymentDatabase = await RunDeploymentAsync(cachedIdentity: null).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunDeploymentAsync(
            new UpstashRedisRemoteIdentityState(GetDatabaseName(), _firstDeploymentDatabase.DatabaseId))
            .ConfigureAwait(false);
    }

    [Then("the live Upstash database exists with the configured name")]
    public void ThenTheLiveUpstashDatabaseExistsWithTheConfiguredName()
    {
        Assert.Equal(GetDatabaseName(), GetFirstDeploymentDatabase().DatabaseName);
    }

    [Then("the live Upstash database is registered for deletion")]
    public void ThenTheLiveUpstashDatabaseIsRegisteredForDeletion()
    {
        Assert.True(_cleanupRegistered);
        Assert.True(_context.LiveUpstash.CleanupActionCount > 0);
    }

    [Then("both live Upstash deployments returned the same provider id")]
    public void ThenBothLiveUpstashDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("only one live Upstash database exists with the configured name")]
    public async Task ThenOnlyOneLiveUpstashDatabaseExistsWithTheConfiguredName()
    {
        UpstashRedisManagementClient client = _context.LiveUpstash.CreateManagementClient();
        IReadOnlyList<UpstashRedisDatabaseSummary> databases = await client
            .ListDatabasesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(databases, database => database.DatabaseName == GetDatabaseName());
    }

    [Then("the live Redis connection string matches the provider details")]
    public async Task ThenTheLiveRedisConnectionStringMatchesTheProviderDetails()
    {
        UpstashRedisDatabaseDetails database = GetFirstDeploymentDatabase();
        IResourceWithConnectionString redisConnection =
            Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);

        string? connectionString = await redisConnection
            .GetConnectionStringAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(
            $"{database.Endpoint}:{database.Port},password={database.Password},ssl=true",
            connectionString);
    }

    [Then("the live supplementary Upstash Redis outputs match the provider details")]
    public async Task ThenTheLiveSupplementaryUpstashRedisOutputsMatchTheProviderDetails()
    {
        UpstashRedisDatabaseDetails database = GetFirstDeploymentDatabase();
        UpstashRedisOutputs outputs = _context.RedisBuilder.Resource.GetUpstashRedisOutputs();

        await AssertOutputAsync(outputs.Endpoint, database.Endpoint).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Port, database.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Password, database.Password).ConfigureAwait(false);
        await AssertOutputAsync(outputs.Tls, "true").ConfigureAwait(false);
        await AssertOutputAsync(outputs.DatabaseName, database.DatabaseName).ConfigureAwait(false);
    }

    [Then("the live supplementary Upstash Redis password output is secret")]
    public void ThenTheLiveSupplementaryUpstashRedisPasswordOutputIsSecret()
    {
        UpstashRedisOutputs outputs = _context.RedisBuilder.Resource.GetUpstashRedisOutputs();

        Assert.True(outputs.Password.Secret);
        Assert.True(UpstashRedisOutputs.IsSecret(outputs.Password.Name));
        Assert.All(
            outputs.Properties.Where(output => output.Name != outputs.Password.Name),
            output => Assert.False(output.Secret));
    }

    private async Task<UpstashRedisDatabaseDetails> RunDeploymentAsync(UpstashRedisRemoteIdentityState? cachedIdentity)
    {
        UpstashRedisDatabaseDetails? database = await UpstashRedisDeploymentPipeline
            .ExecuteAsync(
                CreateDeployment(),
                _context.LiveUpstash.CreateManagementClient(),
                cachedIdentity,
                saveIdentityStateAsync: null,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(database);

        _context.RedisBuilder.Resource.ApplyUpstashRedisConnectionOutput(database);
        _context.RedisBuilder.Resource.GetUpstashRedisOutputs().Populate(database);

        return database;
    }

    private UpstashRedisResolvedDeployment CreateDeployment()
    {
        UpstashRedisDeploymentOptions options = new()
        {
            Tls = true,
        };
        options.SetPlatform(UpstashRedisCloudPlatform.Aws);
        options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);

        return new UpstashRedisResolvedDeployment(
            GetDatabaseName(),
            UpstashRedisOwnershipMode.CreateOrAdopt,
            new UpstashRedisManagementCredentials(
                _context.LiveUpstash.AccountEmail ?? throw new InvalidOperationException("UPSTASH_EMAIL is not configured."),
                _context.LiveUpstash.ApiKey ?? throw new InvalidOperationException("UPSTASH_API_KEY is not configured.")),
            options.ToProviderOptions());
    }

    private string GetDatabaseName()
    {
        return _databaseName ?? throw new InvalidOperationException("The live database name has not been configured.");
    }

    private UpstashRedisDatabaseDetails GetFirstDeploymentDatabase()
    {
        return _firstDeploymentDatabase ?? throw new InvalidOperationException("The first live deployment has not run.");
    }

    private UpstashRedisDatabaseDetails GetSecondDeploymentDatabase()
    {
        return _secondDeploymentDatabase ?? throw new InvalidOperationException("The second live deployment has not run.");
    }

    private static async Task AssertOutputAsync(UpstashRedisOutputReference output, string? expectedValue)
    {
        string? actualValue = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(expectedValue, actualValue);
    }
}
