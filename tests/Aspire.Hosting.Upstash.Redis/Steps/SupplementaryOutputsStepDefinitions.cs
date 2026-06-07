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
    private UpstashRedisResolvedDeployment? _deployment;
    private FakeSupplementaryOutputsManagementClient? _client;

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

    [When("the Upstash deployment pipeline populates supplementary outputs")]
    public async Task WhenTheUpstashDeploymentPipelinePopulatesSupplementaryOutputs()
    {
        await UpstashRedisDeploymentPipeline.ExecuteAsync(
            GetDeployment(),
            GetClient(),
            GetOutputs(),
            CancellationToken.None).ConfigureAwait(false);
    }

    [Then("the supplementary Upstash Redis outputs are:")]
    public async Task ThenTheSupplementaryUpstashRedisOutputsAre(DataTable table)
    {
        IReadOnlyDictionary<string, ReferenceExpression> outputs = GetOutputReferences();

        foreach (DataTableRow row in table.Rows)
        {
            ReferenceExpression output = Assert.Contains(row["Name"], outputs);
            string? value = await output.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(row["Value"], value);
        }
    }

    [Then("only the supplementary Upstash Redis password output is secret")]
    public void ThenOnlyTheSupplementaryUpstashRedisPasswordOutputIsSecret()
    {
        UpstashRedisOutputs outputs = GetOutputs();

        foreach (string name in outputs.Properties.Select(property => property.Key))
        {
            Assert.Equal(
                string.Equals(name, UpstashRedisOutputNames.Password, StringComparison.Ordinal),
                UpstashRedisOutputs.IsSecret(name));
        }
    }

    [Then("the Upstash management API key is not surfaced as a supplementary output")]
    public async Task ThenTheUpstashManagementApiKeyIsNotSurfacedAsASupplementaryOutput()
    {
        foreach (KeyValuePair<string, ReferenceExpression> output in GetOutputs().Properties)
        {
            string? value = await output.Value.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.DoesNotContain(ManagementApiKey, output.Key, StringComparison.Ordinal);
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
            GetOutputs().Properties.Select(property => property.Key));
    }

    private IReadOnlyDictionary<string, ReferenceExpression> GetOutputReferences()
    {
        return GetOutputs().Properties.ToDictionary(
            property => property.Key,
            property => property.Value,
            StringComparer.Ordinal);
    }

    private UpstashRedisOutputs GetOutputs()
    {
        return _outputs ?? throw new InvalidOperationException("The supplementary outputs were not created.");
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

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyList<UpstashRedisDatabaseSummary>>([]);
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<UpstashRedisDatabaseDetails?>(null);
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
            UpstashRedisCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            Assert.Equal(_database.DatabaseId, databaseId);
            return Task.FromResult(_database);
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
