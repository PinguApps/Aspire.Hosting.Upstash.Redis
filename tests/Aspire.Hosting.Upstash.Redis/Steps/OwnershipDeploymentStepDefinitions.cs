#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class OwnershipDeploymentStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;
    private readonly OwnershipDeploymentManagementClient _client = new();
    private readonly OwnershipDeploymentStateManager _deploymentStateManager = new();
    private RedisResource? _redisResource;
    private UpstashRedisOutputs? _outputs;
    private UpstashRedisDatabaseDetails? _result;
    private UpstashRedisRemoteIdentityState? _cachedIdentity;
    private UpstashRedisRemoteIdentityState? _savedIdentity;
    private Exception? _exception;
    private int _previousCreateCount;
    private string? _liveDatabaseName;
    private bool _liveCleanupRegistered;

    public OwnershipDeploymentStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("an Upstash ownership deployment for database {string} with mode {string}")]
    public void GivenAnUpstashOwnershipDeploymentForDatabaseWithMode(string databaseName, string ownershipMode)
    {
        UpstashRedisOwnershipMode mode = Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode);

        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        IResourceBuilder<RedisResource> redis = builder
            .AddRedis("cache")
            .PublishToUpstash(
                databaseName,
                builder.AddParameter("upstash-account-email", "owner@example.com"),
                builder.AddParameter("upstash-api-key", "management-secret", secret: true),
                mode,
                options =>
                {
                    options.Platform = "aws";
                    options.PrimaryRegion = "eu-west-1";
                    options.Tls = true;
                });

        _redisResource = redis.Resource;
        _outputs = redis.Resource.GetUpstashRedisOutputs();
    }

    [Given("the Upstash ownership deployment provider has no database named {string}")]
    public void GivenTheUpstashOwnershipDeploymentProviderHasNoDatabaseNamed(string databaseName)
    {
        Assert.DoesNotContain(_client.Databases, database => database.DatabaseName == databaseName);
    }

    [Given("the Upstash ownership deployment provider has database {string} with id {string}")]
    public void GivenTheUpstashOwnershipDeploymentProviderHasDatabaseWithId(string databaseName, string databaseId)
    {
        _client.AddDatabase(CreateDatabase(databaseName, databaseId));
    }

    [Given("the Upstash ownership deployment provider has duplicate databases named {string}")]
    public void GivenTheUpstashOwnershipDeploymentProviderHasDuplicateDatabasesNamed(string databaseName)
    {
        _client.AddDatabase(CreateDatabase(databaseName, "db-orders-a"));
        _client.AddDatabase(CreateDatabase(databaseName, "db-orders-b"));
    }

    [Given("cached Upstash ownership deployment identity is database {string} with id {string}")]
    public void GivenCachedUpstashOwnershipDeploymentIdentityIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new UpstashRedisRemoteIdentityState(databaseName, databaseId);
    }

    [Given("a live Upstash ownership deployment for isolated database prefix {string}")]
    public void GivenALiveUpstashOwnershipDeploymentForIsolatedDatabasePrefix(string prefix)
    {
        _liveDatabaseName = LiveUpstashTestSession.CreateDisposableDatabaseName(prefix);
    }

    [Given("the live Upstash ownership provider has an isolated database to adopt")]
    public async Task GivenTheLiveUpstashOwnershipProviderHasAnIsolatedDatabaseToAdopt()
    {
        LiveOwnershipManagementClient client = CreateLiveClient();
        client.CreatedDatabase = databaseId => RegisterLiveDeleteCleanup(client, databaseId);

        UpstashRedisDatabaseDetails created = await client.CreateDatabaseAsync(
            CreateLiveDatabaseRequest(GetLiveDatabaseName()),
            CancellationToken.None).ConfigureAwait(false);

        await client
            .WaitUntilReadyAsync(created.DatabaseId, UpstashRedisReadinessPollingOptions.Default, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [When("the Upstash ownership deployment pipeline runs")]
    public async Task WhenTheUpstashOwnershipDeploymentPipelineRuns()
    {
        await RunPipelineAsync().ConfigureAwait(false);
    }

    [When("the Upstash ownership deployment pipeline runs again")]
    public async Task WhenTheUpstashOwnershipDeploymentPipelineRunsAgain()
    {
        await RunPipelineAsync().ConfigureAwait(false);
    }

    [When("the Upstash ownership deployment pipeline is attempted again")]
    public async Task WhenTheUpstashOwnershipDeploymentPipelineIsAttemptedAgain()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);
    }

    [When("the Upstash ownership deployment pipeline is attempted")]
    public async Task WhenTheUpstashOwnershipDeploymentPipelineIsAttempted()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);
    }

    [When("the live Upstash ownership deployment runs with mode {string}")]
    public async Task WhenTheLiveUpstashOwnershipDeploymentRunsWithMode(string ownershipMode)
    {
        LiveOwnershipManagementClient client = CreateLiveClient();
        client.CreatedDatabase = databaseId => RegisterLiveDeleteCleanup(client, databaseId);
        UpstashRedisResolvedDeployment deployment = CreateLiveDeployment(
            GetLiveDatabaseName(),
            Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode));

        try
        {
            _result = await UpstashRedisDeploymentPipeline.ExecuteAsync(
                deployment,
                client,
                cachedIdentity: null,
                saveIdentityStateAsync: identity =>
                {
                    _savedIdentity = identity;
                    return Task.CompletedTask;
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (client.CreateCount == 0)
            {
                client.Dispose();
            }
        }
    }

    [Then("the Upstash ownership deployment succeeds using the {string} path")]
    [Then("the Upstash ownership deployment succeeded using the {string} path")]
    public void ThenTheUpstashOwnershipDeploymentSucceedsUsingThePath(string path)
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);

        bool expectedPathUsed = path switch
        {
            "Create" => _client.CreateCount > _previousCreateCount,
            "Adopt" => _client.CreateCount == _previousCreateCount,
            _ => throw new ArgumentOutOfRangeException(nameof(path), path, "Expected ownership path to be 'Create' or 'Adopt'.")
        };

        Assert.True(expectedPathUsed, $"Expected ownership deployment to use the '{path}' path.");
    }

    [Then("the Upstash ownership deployment saved remote identity database {string}")]
    public void ThenTheUpstashOwnershipDeploymentSavedRemoteIdentityDatabase(string databaseName)
    {
        Assert.NotNull(_savedIdentity);
        Assert.Equal(databaseName, _savedIdentity.DatabaseName);
        Assert.Equal(_result?.DatabaseId, _savedIdentity.ProviderDatabaseId);
    }

    [Then("the Upstash ownership deployment populated Redis outputs for database {string}")]
    public async Task ThenTheUpstashOwnershipDeploymentPopulatedRedisOutputsForDatabase(string databaseName)
    {
        UpstashRedisOutputs outputs = GetOutputs();

        Assert.Equal(databaseName, await outputs.DatabaseName.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(_result?.Endpoint, await outputs.Endpoint.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal(_result?.Password, await outputs.Password.GetValueAsync(CancellationToken.None).ConfigureAwait(false));
    }

    [Then("the Upstash ownership deployment created {int} database")]
    [Then("the Upstash ownership deployment created {int} databases")]
    public void ThenTheUpstashOwnershipDeploymentCreatedDatabases(int createCount)
    {
        Assert.Equal(createCount, _client.CreateCount);
    }

    [Then("the Upstash ownership deployment did not create a database")]
    public void ThenTheUpstashOwnershipDeploymentDidNotCreateADatabase()
    {
        Assert.Equal(0, _client.CreateCount);
    }

    [Then("the Upstash ownership deployment fails because {string}")]
    public void ThenTheUpstashOwnershipDeploymentFailsBecause(string failureReason)
    {
        UpstashRedisOwnershipResolutionException exception = Assert.IsType<UpstashRedisOwnershipResolutionException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisOwnershipResolutionFailureReason>(failureReason), exception.FailureReason);
    }

    [Then("the Upstash ownership deployment fails with provider kind {string}")]
    public void ThenTheUpstashOwnershipDeploymentFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash ownership deployment failure message contains {string}")]
    public void ThenTheUpstashOwnershipDeploymentFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    [Then("the live Upstash ownership deployment created a database")]
    public void ThenTheLiveUpstashOwnershipDeploymentCreatedADatabase()
    {
        Assert.NotNull(_result);
        Assert.Equal(GetLiveDatabaseName(), _result.DatabaseName);
    }

    [Then("the live Upstash ownership deployment adopted the database")]
    public void ThenTheLiveUpstashOwnershipDeploymentAdoptedTheDatabase()
    {
        Assert.NotNull(_result);
        Assert.Equal(GetLiveDatabaseName(), _result.DatabaseName);
    }

    [Then("the live Upstash ownership deployment registered delete cleanup")]
    public void ThenTheLiveUpstashOwnershipDeploymentRegisteredDeleteCleanup()
    {
        Assert.True(_liveCleanupRegistered);
        Assert.True(_context.LiveUpstash.CleanupActionCount > 0);
    }

    private async Task RunPipelineAsync()
    {
        _previousCreateCount = _client.CreateCount;

        RedisResource resource = GetRedisResource();
        UpstashRedisRemoteIdentityDeploymentStateStore identityStore = new(_deploymentStateManager);

        if (_cachedIdentity is not null)
        {
            await identityStore.SaveAsync(resource.Name, _cachedIdentity, CancellationToken.None).ConfigureAwait(false);
        }

        await UpstashRedisDeploymentPipeline.ExecuteAsync(resource, CreatePipelineStepContext(resource)).ConfigureAwait(false);

        _savedIdentity = await identityStore.LoadAsync(resource.Name, CancellationToken.None).ConfigureAwait(false);
        _cachedIdentity = _savedIdentity;

        if (_savedIdentity is not null)
        {
            _result = await _client.GetDatabaseAsync(_savedIdentity.ProviderDatabaseId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private UpstashRedisOutputs GetOutputs()
    {
        return _outputs ?? throw new InvalidOperationException("No Upstash Redis outputs were configured.");
    }

    private RedisResource GetRedisResource()
    {
        return _redisResource ?? throw new InvalidOperationException("No ownership deployment was configured.");
    }

    private PipelineStepContext CreatePipelineStepContext(RedisResource resource)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton<IDeploymentStateManager>(_deploymentStateManager)
            .AddSingleton<IUpstashRedisManagementClient>(_client)
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

    private string GetLiveDatabaseName()
    {
        return _liveDatabaseName ?? throw new InvalidOperationException("No live database name was configured.");
    }

    private LiveOwnershipManagementClient CreateLiveClient()
    {
        return new LiveOwnershipManagementClient(
            _context.LiveUpstash.AccountEmail ?? throw new InvalidOperationException("Missing UPSTASH_EMAIL."),
            _context.LiveUpstash.ApiKey ?? throw new InvalidOperationException("Missing UPSTASH_API_KEY."));
    }

    private void RegisterLiveDeleteCleanup(LiveOwnershipManagementClient client, string databaseId)
    {
        _context.LiveUpstash.RegisterCleanup(async () =>
        {
            try
            {
                await client.DeleteDatabaseIfExistsAsync(databaseId, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                client.Dispose();
            }
        });
        _liveCleanupRegistered = true;
    }

    private UpstashRedisResolvedDeployment CreateLiveDeployment(
        string databaseName,
        UpstashRedisOwnershipMode ownershipMode)
    {
        UpstashRedisDeploymentOptions options = new()
        {
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            Tls = true,
        };

        return new UpstashRedisResolvedDeployment(
            databaseName,
            ownershipMode,
            new UpstashRedisManagementCredentials(
                _context.LiveUpstash.AccountEmail ?? throw new InvalidOperationException("Missing UPSTASH_EMAIL."),
                _context.LiveUpstash.ApiKey ?? throw new InvalidOperationException("Missing UPSTASH_API_KEY.")),
            options.ToProviderOptions());
    }

    private static UpstashRedisCreateDatabaseRequest CreateLiveDatabaseRequest(string databaseName)
    {
        return new UpstashRedisCreateDatabaseRequest
        {
            DatabaseName = databaseName,
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            Tls = true,
        };
    }

    private static UpstashRedisDatabaseDetails CreateDatabase(string databaseName, string databaseId)
    {
        return new UpstashRedisDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.upstash.io",
            Port = 6379,
            Password = "redis-password",
            Tls = true,
            State = "active",
            ModifyingState = null,
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = "payg",
            Budget = 360,
            Eviction = true,
        };
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

    private sealed class OwnershipDeploymentStateManager : IDeploymentStateManager
    {
        private readonly Dictionary<string, DeploymentStateSection> _sections = [];

        public string StateFilePath => "/tmp/fake-aspire-state.json";

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

    private sealed class OwnershipDeploymentManagementClient : IUpstashRedisManagementClient
    {
        private readonly List<UpstashRedisDatabaseDetails> _databases = [];

        public IReadOnlyList<UpstashRedisDatabaseDetails> Databases => _databases;

        public int CreateCount { get; private set; }

        public void AddDatabase(UpstashRedisDatabaseDetails database)
        {
            _databases.Add(Clone(database));
        }

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

            List<UpstashRedisDatabaseDetails> matches =
                [.. _databases.Where(database => database.DatabaseName == databaseName).Take(2)];

            if (matches.Count > 1)
            {
                throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.ProviderContract,
                    statusCode: null,
                    $"Upstash Redis returned more than one database named '{databaseName}'.");
            }

            UpstashRedisDatabaseDetails? match = matches.SingleOrDefault();

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

            UpstashRedisDatabaseDetails database = CreateDatabase(
                request.DatabaseName,
                $"db-{request.DatabaseName}");
            database.PrimaryRegion = request.PrimaryRegion;
            database.ReadRegions = request.ReadRegions;
            database.Type = request.Plan ?? "payg";
            database.Budget = request.Budget;
            database.Eviction = request.Eviction;

            _databases.Add(database);

            return Task.FromResult(new UpstashRedisDatabaseDetails
            {
                DatabaseId = database.DatabaseId,
                DatabaseName = database.DatabaseName,
                State = database.State,
                ModifyingState = database.ModifyingState,
            });
        }

        public Task UpdateReadRegionsAsync(
            string databaseId,
            UpstashRedisUpdateRegionsRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(
            string databaseId,
            UpstashRedisChangePlanRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetMutableDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(
            string databaseId,
            UpstashRedisUpdateBudgetRequest request,
            CancellationToken cancellationToken)
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
    }

    private sealed class LiveOwnershipManagementClient : IUpstashRedisManagementClient, IDisposable
    {
        private readonly UpstashRedisManagementCredentials _credentials;
        private readonly HttpClient _httpClient;
        private readonly UpstashRedisManagementClient _inner;

        public LiveOwnershipManagementClient(string accountEmail, string apiKey)
        {
            _credentials = new UpstashRedisManagementCredentials(accountEmail, apiKey);
            _httpClient = new HttpClient { BaseAddress = new Uri("https://api.upstash.com/v2/") };
            _inner = new UpstashRedisManagementClient(_httpClient, _credentials);
        }

        public int CreateCount { get; private set; }

        public Action<string>? CreatedDatabase { get; set; }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            return _inner.ListDatabasesAsync(cancellationToken);
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            return _inner.GetDatabaseAsync(databaseId, cancellationToken);
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            return _inner.FindDatabaseByNameAsync(databaseName, cancellationToken);
        }

        public async Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
            UpstashRedisCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            UpstashRedisDatabaseDetails database = await _inner
                .CreateDatabaseAsync(request, cancellationToken)
                .ConfigureAwait(false);

            CreateCount++;
            CreatedDatabase?.Invoke(database.DatabaseId);

            return database;
        }

        public Task UpdateReadRegionsAsync(
            string databaseId,
            UpstashRedisUpdateRegionsRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.UpdateReadRegionsAsync(databaseId, request, cancellationToken);
        }

        public Task ChangePlanAsync(
            string databaseId,
            UpstashRedisChangePlanRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.ChangePlanAsync(databaseId, request, cancellationToken);
        }

        public Task UpdateBudgetAsync(
            string databaseId,
            UpstashRedisUpdateBudgetRequest request,
            CancellationToken cancellationToken)
        {
            return _inner.UpdateBudgetAsync(databaseId, request, cancellationToken);
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            return _inner.SetEvictionAsync(databaseId, enabled, cancellationToken);
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return _inner.WaitUntilReadyAsync(databaseId, pollingOptions, cancellationToken);
        }

        public async Task DeleteDatabaseIfExistsAsync(string databaseId, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, $"redis/database/{Uri.EscapeDataString(databaseId)}");
            request.Headers.Authorization = _credentials.CreateAuthorizationHeader();

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            throw new InvalidOperationException(
                $"Failed to delete live Upstash Redis database '{databaseId}' during test cleanup: {(int)response.StatusCode} {response.StatusCode} {content}");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
