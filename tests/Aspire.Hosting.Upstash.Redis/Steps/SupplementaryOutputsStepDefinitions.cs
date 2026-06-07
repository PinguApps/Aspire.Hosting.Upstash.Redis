using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class SupplementaryOutputsStepDefinitions
{
    private const string ManagementApiKey = "management-secret";

    private UpstashRedisOutputs? _outputs;
    private RedisResource? _resource;
    private UpstashRedisResolvedDeployment? _deployment;
    private FakeSupplementaryOutputsManagementClient? _client;
    private Exception? _exception;

    [Given("an Upstash Redis resource with supplementary outputs")]
    public void GivenAnUpstashRedisResourceWithSupplementaryOutputs()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        IResourceBuilder<RedisResource> redis = builder
            .AddRedis("cache")
            .PublishToUpstash(
                "orders-cache",
                builder.AddParameter("upstash-account-email", "owner@example.com"),
                builder.AddParameter("upstash-api-key", ManagementApiKey, secret: true),
                UpstashRedisOwnershipMode.CreateOnly,
                options =>
                {
                    options.Platform = "aws";
                    options.PrimaryRegion = "eu-west-1";
                    options.ReadRegions = ["eu-west-2"];
                    options.Plan = "payg";
                    options.Budget = "360";
                    options.Eviction = true;
                    options.Tls = true;
                });

        _outputs = redis.Resource.GetUpstashRedisOutputs();
        _resource = redis.Resource;
        _deployment = new UpstashRedisResolvedDeployment(
            "orders-cache",
            UpstashRedisOwnershipMode.CreateOnly,
            new UpstashRedisManagementCredentials("owner@example.com", ManagementApiKey),
            new UpstashRedisDeploymentOptions
            {
                Platform = "aws",
                PrimaryRegion = "eu-west-1",
                ReadRegions = ["eu-west-2"],
                Plan = "payg",
                Budget = "360",
                Eviction = true,
                Tls = true,
            }.ToProviderOptions());
    }

    [Given("the Upstash deployment provider will create database {string} with id {string}")]
    public void GivenTheUpstashDeploymentProviderWillCreateDatabaseWithId(string databaseName, string databaseId)
    {
        _client = new FakeSupplementaryOutputsManagementClient(CreateDatabase(databaseName, databaseId));
    }

    [Given("the Upstash deployment provider will create database {string} with id {string} without a password")]
    public void GivenTheUpstashDeploymentProviderWillCreateDatabaseWithIdWithoutAPassword(string databaseName, string databaseId)
    {
        UpstashRedisDatabaseDetails database = CreateDatabase(databaseName, databaseId);
        database.Password = null;

        _client = new FakeSupplementaryOutputsManagementClient(database);
    }

    [When("the Upstash deployment pipeline populates supplementary outputs")]
    public async Task WhenTheUpstashDeploymentPipelinePopulatesSupplementaryOutputs()
    {
        await UpstashRedisDeploymentPipeline.ExecuteAsync(
            GetDeployment(),
            GetClient(),
            GetOutputs(),
            CancellationToken.None).ConfigureAwait(false);
    }

    [When("the Upstash deployment pipeline attempts to populate supplementary outputs")]
    public async Task WhenTheUpstashDeploymentPipelineAttemptsToPopulateSupplementaryOutputs()
    {
        _exception = await Record.ExceptionAsync(WhenTheUpstashDeploymentPipelinePopulatesSupplementaryOutputs).ConfigureAwait(false);
    }

    [Then("the supplementary Upstash Redis outputs are:")]
    public async Task ThenTheSupplementaryUpstashRedisOutputsAre(DataTable table)
    {
        IReadOnlyDictionary<string, UpstashRedisOutputReference> outputs = GetOutputReferences();

        foreach (DataTableRow row in table.Rows)
        {
            UpstashRedisOutputReference output = Assert.Contains(row["Name"], outputs);
            string? value = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(row["Value"], value);
        }
    }

    [Then("only the supplementary Upstash Redis password output is secret")]
    public void ThenOnlyTheSupplementaryUpstashRedisPasswordOutputIsSecret()
    {
        UpstashRedisOutputs outputs = GetOutputs();

        foreach (UpstashRedisOutputReference output in outputs.Properties)
        {
            Assert.Equal(
                string.Equals(output.Name, UpstashRedisOutputNames.Password, StringComparison.Ordinal),
                output.Secret);
            Assert.Equal(output.Secret, UpstashRedisOutputs.IsSecret(output.Name));
        }

        ReferenceExpression passwordExpression = outputs.Password.AsReferenceExpression();
        UpstashRedisOutputReference passwordProvider =
            Assert.IsType<UpstashRedisOutputReference>(Assert.Single(passwordExpression.ValueProviders));

        Assert.True(passwordProvider.Secret);
    }

    [Then("the Upstash management API key is not surfaced as a supplementary output")]
    public async Task ThenTheUpstashManagementApiKeyIsNotSurfacedAsASupplementaryOutput()
    {
        foreach (UpstashRedisOutputReference output in GetOutputs().Properties)
        {
            string? value = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.DoesNotContain(ManagementApiKey, output.Name, StringComparison.Ordinal);
            Assert.DoesNotContain(ManagementApiKey, value, StringComparison.Ordinal);
        }
    }

    [Then("the supplementary Upstash Redis output names are stable")]
    public void ThenTheSupplementaryUpstashRedisOutputNamesAreStable()
    {
        Assert.Equal(
            [
                UpstashRedisOutputNames.Endpoint,
                UpstashRedisOutputNames.Port,
                UpstashRedisOutputNames.Password,
                UpstashRedisOutputNames.Tls,
                UpstashRedisOutputNames.DatabaseName,
            ],
            GetOutputs().Properties.Select(property => property.Name));
    }

    [Then("each supplementary Upstash Redis output references the Redis resource")]
    public void ThenEachSupplementaryUpstashRedisOutputReferencesTheRedisResource()
    {
        RedisResource resource = GetResource();

        foreach (ReferenceExpression output in GetOutputs().Properties.Select(property => property.AsReferenceExpression()))
        {
            IValueProvider valueProvider = Assert.Single(output.ValueProviders);
            IValueWithReferences valueWithReferences = Assert.IsAssignableFrom<IValueWithReferences>(valueProvider);
            Assert.Contains(resource, valueWithReferences.References);
        }
    }

    [Then("supplementary Upstash Redis output population fails with provider kind {string}")]
    public void ThenSupplementaryUpstashRedisOutputPopulationFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the supplementary Upstash Redis output failure message contains {string}")]
    public void ThenTheSupplementaryUpstashRedisOutputFailureMessageContains(string expectedText)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("Supplementary output population did not fail.");

        Assert.Contains(expectedText, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Upstash supplementary output provider did not attempt reset-password")]
    public void ThenTheUpstashSupplementaryOutputProviderDidNotAttemptResetPassword()
    {
        Assert.DoesNotContain(GetClient().Operations, operation => operation.Contains("reset-password", StringComparison.Ordinal));
    }

    private IReadOnlyDictionary<string, UpstashRedisOutputReference> GetOutputReferences()
    {
        return GetOutputs().Properties.ToDictionary(
            property => property.Name,
            StringComparer.Ordinal);
    }

    private UpstashRedisOutputs GetOutputs()
    {
        return _outputs ?? throw new InvalidOperationException("The supplementary outputs were not created.");
    }

    private RedisResource GetResource()
    {
        return _resource ?? throw new InvalidOperationException("The Redis resource was not created.");
    }

    private UpstashRedisResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("The deployment was not created.");
    }

    private FakeSupplementaryOutputsManagementClient GetClient()
    {
        return _client ?? throw new InvalidOperationException("The provider was not created.");
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

    private sealed class FakeSupplementaryOutputsManagementClient : IUpstashRedisManagementClient
    {
        private readonly UpstashRedisDatabaseDetails _database;

        public FakeSupplementaryOutputsManagementClient(UpstashRedisDatabaseDetails database)
        {
            _database = database;
        }

        public List<string> Operations { get; } = [];

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add("GET /redis/databases");

            return Task.FromResult<IReadOnlyList<UpstashRedisDatabaseSummary>>([]);
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"GET /redis/databases?name={databaseName}");

            return Task.FromResult<UpstashRedisDatabaseDetails?>(null);
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
            UpstashRedisCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add("POST /redis/database");

            return Task.FromResult(new UpstashRedisDatabaseDetails
            {
                DatabaseId = _database.DatabaseId,
                DatabaseName = request.DatabaseName,
            });
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"WAIT /redis/database/{databaseId}");

            Assert.Equal(_database.DatabaseId, databaseId);
            return Task.FromResult(_database);
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"GET /redis/database/{databaseId}");

            if (!string.Equals(databaseId, _database.DatabaseId, StringComparison.Ordinal))
            {
                throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.NotFound,
                    HttpStatusCode.NotFound,
                    "missing database");
            }

            return Task.FromResult(_database);
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
    }
}
