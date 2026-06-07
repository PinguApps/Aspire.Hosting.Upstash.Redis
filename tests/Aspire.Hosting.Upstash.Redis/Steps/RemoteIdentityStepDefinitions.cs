#pragma warning disable ASPIREPIPELINES002

using System.Net;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Management;
using Aspire.Hosting.Pipelines;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class RemoteIdentityStepDefinitions : IDisposable
{
    private const string AccountEmail = "pingu@example.com";
    private const string ApiKey = "secret-key";

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly FakeDeploymentStateManager _deploymentStateManager = new();
    private readonly HttpClient _httpClient;
    private UpstashRedisRemoteIdentityState? _cachedIdentity;
    private UpstashRedisRemoteIdentityResolution? _lastResolution;
    private Exception? _lastException;

    public RemoteIdentityStepDefinitions()
    {
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.upstash.com/v2/"),
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Given("cached Upstash remote identity is database {string} with id {string}")]
    public void GivenCachedUpstashRemoteIdentityIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new UpstashRedisRemoteIdentityState(databaseName, databaseId);
    }

    [Given("the Upstash identity API returns an empty database list")]
    public void GivenTheUpstashIdentityApiReturnsAnEmptyDatabaseList()
    {
        _handler.Enqueue(HttpStatusCode.OK, "[]");
    }

    [Given("the Upstash identity API returns a list containing database {string} with id {string}")]
    public void GivenTheUpstashIdentityApiReturnsAListContainingDatabaseWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "{{databaseId}}",
                "database_name": "{{databaseName}}"
              }
            ]
            """);
    }

    [Given("the Upstash identity API returns duplicate databases named {string}")]
    public void GivenTheUpstashIdentityApiReturnsDuplicateDatabasesNamed(string databaseName)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "db-orders-1",
                "database_name": "{{databaseName}}"
              },
              {
                "database_id": "db-orders-2",
                "database_name": "{{databaseName}}"
              }
            ]
            """);
    }

    [Given("the Upstash identity API returns details for database {string} with id {string}")]
    public void GivenTheUpstashIdentityApiReturnsDetailsForDatabaseWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, databaseId));
    }

    [Given("the Upstash identity API returns not found")]
    public void GivenTheUpstashIdentityApiReturnsNotFound()
    {
        _handler.Enqueue(HttpStatusCode.NotFound, """{ "error": "not found" }""");
    }

    [When("the Upstash remote identity resolver resolves configured database {string}")]
    public async Task WhenTheUpstashRemoteIdentityResolverResolvesConfiguredDatabase(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
        {
            IUpstashRedisManagementClient client = new UpstashRedisManagementClient(
                _httpClient,
                new UpstashRedisManagementCredentials(AccountEmail, ApiKey));
            UpstashRedisRemoteIdentityResolver resolver = new(client);

            _lastResolution = await resolver.ResolveAsync(databaseName, _cachedIdentity, CancellationToken.None)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Upstash remote identity cache for Redis resource {string} is saved as database {string} with id {string}")]
    public async Task WhenTheUpstashRemoteIdentityCacheForRedisResourceIsSavedAsDatabaseWithId(
        string resourceName,
        string databaseName,
        string databaseId)
    {
        UpstashRedisRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);

        await store.SaveAsync(
            resourceName,
            new UpstashRedisRemoteIdentityState(databaseName, databaseId),
            CancellationToken.None).ConfigureAwait(false);
    }

    [Then("the Upstash remote identity resolver returns database {string} with id {string}")]
    public void ThenTheUpstashRemoteIdentityResolverReturnsDatabaseWithId(string databaseName, string databaseId)
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.True(resolution.Found);
        Assert.NotNull(resolution.Database);
        Assert.Equal(databaseName, resolution.Database.DatabaseName);
        Assert.Equal(databaseId, resolution.Database.DatabaseId);
    }

    [Then("the Upstash remote identity resolver returns no database")]
    public void ThenTheUpstashRemoteIdentityResolverReturnsNoDatabase()
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.False(resolution.Found);
        Assert.Null(resolution.Database);
    }

    [Then("the Upstash remote identity cache is database {string} with id {string}")]
    public void ThenTheUpstashRemoteIdentityCacheIsDatabaseWithId(string databaseName, string databaseId)
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.NotNull(resolution.IdentityState);
        Assert.Equal(databaseName, resolution.IdentityState.DatabaseName);
        Assert.Equal(databaseId, resolution.IdentityState.ProviderDatabaseId);
    }

    [Then("the Upstash remote identity cache is empty")]
    public void ThenTheUpstashRemoteIdentityCacheIsEmpty()
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.Null(resolution.IdentityState);
    }

    [Then("the Upstash remote identity was resolved from the cached identity")]
    public void ThenTheUpstashRemoteIdentityWasResolvedFromTheCachedIdentity()
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.True(resolution.ResolvedFromCachedIdentity);
    }

    [Then("the Upstash remote identity was not resolved from the cached identity")]
    public void ThenTheUpstashRemoteIdentityWasNotResolvedFromTheCachedIdentity()
    {
        UpstashRedisRemoteIdentityResolution resolution =
            _lastResolution ?? throw new InvalidOperationException("No remote identity resolution was captured.");

        Assert.False(resolution.ResolvedFromCachedIdentity);
    }

    [Then("the Upstash remote identity cache for Redis resource {string} loads database {string} with id {string}")]
    public async Task ThenTheUpstashRemoteIdentityCacheForRedisResourceLoadsDatabaseWithId(
        string resourceName,
        string databaseName,
        string databaseId)
    {
        UpstashRedisRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);
        UpstashRedisRemoteIdentityState? state = await store.LoadAsync(resourceName, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(state);
        Assert.Equal(databaseName, state.DatabaseName);
        Assert.Equal(databaseId, state.ProviderDatabaseId);
    }

    [Then("the Upstash remote identity cache for Redis resource {string} is empty")]
    public async Task ThenTheUpstashRemoteIdentityCacheForRedisResourceIsEmpty(string resourceName)
    {
        UpstashRedisRemoteIdentityDeploymentStateStore store = new(_deploymentStateManager);
        UpstashRedisRemoteIdentityState? state = await store.LoadAsync(resourceName, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Null(state);
    }

    [Then("the Upstash remote identity resolver fails with provider kind {string}")]
    public void ThenTheUpstashRemoteIdentityResolverFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_lastException);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash remote identity failure message contains {string}")]
    public void ThenTheUpstashRemoteIdentityFailureMessageContains(string value)
    {
        Exception exception = _lastException ?? throw new InvalidOperationException("No exception was captured.");

        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Upstash identity request sequence is:")]
    public void ThenTheUpstashIdentityRequestSequenceIs(DataTable table)
    {
        Assert.Equal(table.Rows.Count, _handler.Requests.Count);

        for (int requestIndex = 0; requestIndex < table.Rows.Count; requestIndex++)
        {
            Assert.Equal(table.Rows[requestIndex]["Method"], _handler.Requests[requestIndex].Method.Method);
            Assert.Equal(table.Rows[requestIndex]["Path"], _handler.Requests[requestIndex].PathAndQuery);
        }
    }

    private async Task CaptureExceptionAsync(Func<Task> operation)
    {
        _lastException = await Record.ExceptionAsync(operation).ConfigureAwait(false);
    }

    private static string CreateDatabaseDetailsJson(string databaseName, string databaseId)
    {
        return $$"""
        {
          "database_id": "{{databaseId}}",
          "database_name": "{{databaseName}}",
          "endpoint": "global-apt-1.upstash.io",
          "port": 6379,
          "password": "redis-password",
          "tls": true,
          "state": "active",
          "modifying_state": null,
          "primary_region": "eu-west-1",
          "read_regions": ["eu-west-2"],
          "type": "payg",
          "budget": 50,
          "eviction": true,
          "customer_id": "cust-1"
        }
        """;
    }

    private sealed class FakeDeploymentStateManager : IDeploymentStateManager
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
}
