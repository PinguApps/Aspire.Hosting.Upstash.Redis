#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class TypeScriptDeploymentStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;
    private readonly TypeScriptDeploymentManagementClient _fakeClient = new();
    private readonly TypeScriptDeploymentStateManager _stateManager = new();
    private RedisResource? _redisResource;
    private UpstashRedisDatabaseDetails? _firstDeploymentDatabase;
    private UpstashRedisDatabaseDetails? _secondDeploymentDatabase;
    private string? _databaseName;
    private bool _cleanupRegistered;

    public TypeScriptDeploymentStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("a TypeScript-authored Upstash Redis deployment for database {string}")]
    public void GivenATypeScriptAuthoredUpstashRedisDeploymentForDatabase(string databaseName)
    {
        _databaseName = databaseName;
        _redisResource = CreateTypeScriptAuthoredRedis(databaseName).Resource;
    }

    [Given("the TypeScript deployment fake provider has no database named {string}")]
    public void GivenTheTypeScriptDeploymentFakeProviderHasNoDatabaseNamed(string databaseName)
    {
        Assert.DoesNotContain(_fakeClient.Databases, database => database.DatabaseName == databaseName);
    }

    [Given("a live TypeScript-authored Upstash Redis deployment with prefix {string}")]
    public async Task GivenALiveTypeScriptAuthoredUpstashRedisDeploymentWithPrefix(string prefix)
    {
        _databaseName = LiveUpstashTestSession.CreateDisposableDatabaseName(prefix);
        _redisResource = CreateTypeScriptAuthoredRedis(_databaseName).Resource;

        await _context.LiveUpstash.RegisterDatabaseDeletionByNameAsync(_databaseName).ConfigureAwait(false);
        _cleanupRegistered = true;
    }

    [When("the TypeScript-authored Upstash deployment pipeline runs twice")]
    public async Task WhenTheTypeScriptAuthoredUpstashDeploymentPipelineRunsTwice()
    {
        _firstDeploymentDatabase = await RunPipelineAsync(_fakeClient).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunPipelineAsync(_fakeClient).ConfigureAwait(false);
    }

    [When("the live TypeScript-authored Upstash deployment pipeline runs twice")]
    public async Task WhenTheLiveTypeScriptAuthoredUpstashDeploymentPipelineRunsTwice()
    {
        IUpstashRedisManagementClient client = _context.LiveUpstash.CreateManagementClient();

        _firstDeploymentDatabase = await RunPipelineAsync(client).ConfigureAwait(false);
        _secondDeploymentDatabase = await RunPipelineAsync(client).ConfigureAwait(false);
    }

    [Then("the TypeScript-authored Upstash deployment created {int} database")]
    [Then("the TypeScript-authored Upstash deployment created {int} databases")]
    public void ThenTheTypeScriptAuthoredUpstashDeploymentCreatedDatabase(int createCount)
    {
        Assert.Equal(createCount, _fakeClient.CreateCount);
    }

    [Then("the TypeScript-authored Upstash deployments returned the same provider id")]
    public void ThenTheTypeScriptAuthoredUpstashDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("the TypeScript-authored Upstash deployment populated Redis outputs for database {string}")]
    public async Task ThenTheTypeScriptAuthoredUpstashDeploymentPopulatedRedisOutputsForDatabase(string databaseName)
    {
        UpstashRedisDatabaseDetails database = GetSecondDeploymentDatabase();
        UpstashRedisOutputs outputs = GetRedisResource().GetUpstashRedisOutputs();

        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal(databaseName, await outputs.DatabaseName.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(database.Endpoint, await outputs.Endpoint.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(database.Password, await outputs.Password.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
    }

    [Then("the live TypeScript-authored Upstash deployments returned the same provider id")]
    public void ThenTheLiveTypeScriptAuthoredUpstashDeploymentsReturnedTheSameProviderId()
    {
        Assert.Equal(GetFirstDeploymentDatabase().DatabaseId, GetSecondDeploymentDatabase().DatabaseId);
    }

    [Then("only one live TypeScript-authored Upstash database exists with the configured name")]
    public async Task ThenOnlyOneLiveTypeScriptAuthoredUpstashDatabaseExistsWithTheConfiguredName()
    {
        UpstashRedisManagementClient client = _context.LiveUpstash.CreateManagementClient();
        IReadOnlyList<UpstashRedisDatabaseSummary> databases = await client
            .ListDatabasesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(databases, database => database.DatabaseName == GetDatabaseName());
    }

    [Then("the live TypeScript-authored Upstash database is registered for deletion")]
    public void ThenTheLiveTypeScriptAuthoredUpstashDatabaseIsRegisteredForDeletion()
    {
        Assert.True(_cleanupRegistered);
        Assert.True(_context.LiveUpstash.CleanupActionCount > 0);
    }

    private IResourceBuilder<RedisResource> CreateTypeScriptAuthoredRedis(string databaseName)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<RedisResource> redis = builder.AddRedis("cache");

        return redis.PublishToUpstashForTypeScript(
            builder.AddParameter("upstash-database-name", databaseName),
            builder.AddParameter("upstash-account-email", "owner@example.com"),
            builder.AddParameter("upstash-api-key", "management-secret", secret: true),
            new UpstashRedisDeploymentOptionsDto
            {
                OwnershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
                Platform = UpstashRedisCloudPlatform.Aws,
                PrimaryRegion = UpstashRedisRegion.AwsEuWest1,
                ReadRegions = [UpstashRedisRegion.AwsEuWest2],
                Plan = UpstashRedisPlan.PayAsYouGo,
                Budget = 20,
                Eviction = true,
                Tls = true
            });
    }

    private async Task<UpstashRedisDatabaseDetails> RunPipelineAsync(IUpstashRedisManagementClient client)
    {
        RedisResource resource = GetRedisResource();

        await UpstashRedisDeploymentPipeline.ExecuteAsync(resource, CreatePipelineStepContext(resource, client)).ConfigureAwait(false);

        UpstashRedisRemoteIdentityDeploymentStateStore identityStore = new(_stateManager);
        UpstashRedisRemoteIdentityState? savedIdentity = await identityStore
            .LoadAsync(resource.Name, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(savedIdentity);

        return await client.GetDatabaseAsync(savedIdentity.ProviderDatabaseId, CancellationToken.None).ConfigureAwait(false);
    }

    private PipelineStepContext CreatePipelineStepContext(RedisResource resource, IUpstashRedisManagementClient client)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(_stateManager)
            .AddSingleton<IDeploymentStateManager>(_stateManager)
            .AddSingleton(client)
            .AddSingleton<IUpstashRedisManagementClient>(client)
            .BuildServiceProvider();

        PipelineContext pipelineContext = new(
            new DistributedApplicationModel([resource]),
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
            services,
            NullLogger.Instance,
            CancellationToken.None);

        return new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = null!,
        };
    }

    private RedisResource GetRedisResource()
    {
        return _redisResource ?? throw new InvalidOperationException("The TypeScript-authored Redis resource has not been configured.");
    }

    private string GetDatabaseName()
    {
        return _databaseName ?? throw new InvalidOperationException("The TypeScript-authored database name has not been configured.");
    }

    private UpstashRedisDatabaseDetails GetFirstDeploymentDatabase()
    {
        return _firstDeploymentDatabase ?? throw new InvalidOperationException("The first TypeScript-authored deployment has not run.");
    }

    private UpstashRedisDatabaseDetails GetSecondDeploymentDatabase()
    {
        return _secondDeploymentDatabase ?? throw new InvalidOperationException("The second TypeScript-authored deployment has not run.");
    }

    private sealed class TypeScriptDeploymentStateManager : IDeploymentStateManager
    {
        private readonly Dictionary<string, DeploymentStateSection> _sections = [];

        public string StateFilePath => "/tmp/fake-aspire-typescript-state.json";

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_sections.TryGetValue(sectionName, out DeploymentStateSection? section))
            {
                section = new DeploymentStateSection(sectionName, [], version: 0);
                _sections[sectionName] = section;
            }

            return Task.FromResult(section);
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections[section.SectionName] = section;

            return Task.CompletedTask;
        }

        public Task DeleteSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections.Remove(section.SectionName);

            return Task.CompletedTask;
        }

        public Task ClearAllStateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sections.Clear();

            return Task.CompletedTask;
        }
    }

    private sealed class TypeScriptDeploymentManagementClient : IUpstashRedisManagementClient
    {
        private readonly List<UpstashRedisDatabaseDetails> _databases = [];

        public IReadOnlyList<UpstashRedisDatabaseDetails> Databases => _databases;

        public int CreateCount { get; private set; }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<UpstashRedisDatabaseSummary> summaries =
            [
                .. _databases.Select(database => new UpstashRedisDatabaseSummary
                {
                    DatabaseId = database.DatabaseId,
                    DatabaseName = database.DatabaseName,
                    Endpoint = database.Endpoint,
                    Port = database.Port,
                    State = database.State,
                    ModifyingState = database.ModifyingState,
                    PrimaryRegion = database.PrimaryRegion,
                    ReadRegions = database.ReadRegions,
                }),
            ];

            return Task.FromResult(summaries);
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpstashRedisDatabaseDetails database = _databases.SingleOrDefault(database => database.DatabaseId == databaseId)
                ?? throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    $"Database '{databaseId}' was not found.");

            return Task.FromResult(Clone(database));
        }

        public async Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpstashRedisDatabaseDetails? match = _databases.SingleOrDefault(database => database.DatabaseName == databaseName);

            return match is null
                ? null
                : await GetDatabaseAsync(match.DatabaseId, cancellationToken).ConfigureAwait(false);
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
            UpstashRedisCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;

            UpstashRedisDatabaseDetails database = new()
            {
                DatabaseId = $"db-{request.DatabaseName}",
                DatabaseName = request.DatabaseName,
                Endpoint = "global-apt-1.upstash.io",
                Port = 6379,
                Password = "redis-password",
                Tls = request.Tls ?? true,
                State = "active",
                ModifyingState = null,
                PrimaryRegion = request.PrimaryRegion,
                ReadRegions = request.ReadRegions,
                Type = request.Plan ?? "payg",
                Budget = request.Budget,
                Eviction = request.Eviction,
            };

            _databases.Add(database);

            return Task.FromResult(new UpstashRedisDatabaseDetails
            {
                DatabaseId = database.DatabaseId,
                DatabaseName = database.DatabaseName,
                State = database.State,
                ModifyingState = database.ModifyingState,
            });
        }

        public Task UpdateReadRegionsAsync(string databaseId, UpstashRedisUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, UpstashRedisChangePlanRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, UpstashRedisUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).Budget = request.Budget;

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetMutableDatabase(databaseId).Eviction = enabled;

            return Task.CompletedTask;
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return GetDatabaseAsync(databaseId, cancellationToken);
        }

        private UpstashRedisDatabaseDetails GetMutableDatabase(string databaseId)
        {
            return _databases.SingleOrDefault(database => database.DatabaseId == databaseId)
                ?? throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    $"Database '{databaseId}' was not found.");
        }

        private static UpstashRedisDatabaseDetails Clone(UpstashRedisDatabaseDetails database)
        {
            return new UpstashRedisDatabaseDetails
            {
                DatabaseId = database.DatabaseId,
                DatabaseName = database.DatabaseName,
                Endpoint = database.Endpoint,
                Port = database.Port,
                Password = database.Password,
                Tls = database.Tls,
                State = database.State,
                ModifyingState = database.ModifyingState,
                PrimaryRegion = database.PrimaryRegion,
                ReadRegions = database.ReadRegions is null ? null : [.. database.ReadRegions],
                Type = database.Type,
                DbDiskThreshold = database.DbDiskThreshold,
                Budget = database.Budget,
                Eviction = database.Eviction,
                CustomerId = database.CustomerId,
            };
        }
    }
}
