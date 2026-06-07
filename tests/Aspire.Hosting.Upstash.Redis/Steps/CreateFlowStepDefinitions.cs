using System.Net;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class CreateFlowStepDefinitions
{
    private readonly FakeCreateFlowManagementClient _client = new();
    private UpstashRedisResolvedDeployment? _deployment;
    private UpstashRedisOwnershipResolutionResult? _ownership;
    private UpstashRedisCreateFlowResult? _result;
    private Exception? _exception;

    [Given("an Upstash create flow deployment for database {string}")]
    public void GivenAnUpstashCreateFlowDeploymentForDatabase(string databaseName)
    {
        UpstashRedisDeploymentOptions options = new()
        {
            Platform = "aws",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Plan = "payg",
            Budget = "360",
            Eviction = true,
            Tls = true,
        };

        _deployment = new UpstashRedisResolvedDeployment(
            databaseName,
            UpstashRedisOwnershipMode.CreateOnly,
            new UpstashRedisManagementCredentials("owner@example.com", "management-secret"),
            options.ToProviderOptions());
    }

    [Given("ownership resolution selected create")]
    public void GivenOwnershipResolutionSelectedCreate()
    {
        _ownership = UpstashRedisOwnershipResolutionResult.Create();
    }

    [Given("ownership resolution selected adopt for database {string} with id {string}")]
    public void GivenOwnershipResolutionSelectedAdoptForDatabaseWithId(string databaseName, string databaseId)
    {
        _ownership = UpstashRedisOwnershipResolutionResult.Adopt(CreateDatabaseDetails(databaseName, databaseId, includePassword: true));
    }

    [Given("the Upstash create API returns database id {string}")]
    public void GivenTheUpstashCreateApiReturnsDatabaseId(string databaseId)
    {
        UpstashRedisResolvedDeployment deployment = GetDeployment();
        _client.CreateResponse = new UpstashRedisDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = deployment.DatabaseName,
        };
    }

    [Given("the Upstash create API fails with provider kind {string} and message {string}")]
    public void GivenTheUpstashCreateApiFailsWithProviderKindAndMessage(string failureKind, string message)
    {
        _client.CreateException = new UpstashRedisProviderException(
            Enum.Parse<UpstashRedisProviderFailureKind>(failureKind),
            HttpStatusCode.BadRequest,
            message);
    }

    [Given("the Upstash readiness API returns active database {string} with id {string}")]
    public void GivenTheUpstashReadinessApiReturnsActiveDatabaseWithId(string databaseName, string databaseId)
    {
        _client.ReadyResponse = CreateDatabaseDetails(databaseName, databaseId, includePassword: true);
    }

    [Given("the Upstash readiness API returns active database {string} with id {string} without a password")]
    public void GivenTheUpstashReadinessApiReturnsActiveDatabaseWithIdWithoutAPassword(string databaseName, string databaseId)
    {
        _client.ReadyResponse = CreateDatabaseDetails(databaseName, databaseId, includePassword: false);
    }

    [Given("the Upstash readiness API returns active database {string} with id {string} with invalid connection field {string}")]
    public void GivenTheUpstashReadinessApiReturnsActiveDatabaseWithInvalidConnectionField(string databaseName, string databaseId, string field)
    {
        UpstashRedisDatabaseDetails database = CreateDatabaseDetails(databaseName, databaseId, includePassword: true);

        switch (field)
        {
            case "endpoint":
                database.Endpoint = string.Empty;
                break;
            case "port":
                database.Port = 0;
                break;
            case "tls":
                database.Tls = false;
                break;
            default:
                throw new InvalidOperationException($"Unknown connection field '{field}'.");
        }

        _client.ReadyResponse = database;
    }

    [When("the Upstash create flow executes")]
    public async Task WhenTheUpstashCreateFlowExecutes()
    {
        UpstashRedisCreateFlow flow = new(_client);
        _result = await flow.ExecuteAsync(GetDeployment(), GetOwnership(), CancellationToken.None).ConfigureAwait(false);
    }

    [When("the Upstash create flow is attempted")]
    public async Task WhenTheUpstashCreateFlowIsAttempted()
    {
        _exception = await Record.ExceptionAsync(WhenTheUpstashCreateFlowExecutes).ConfigureAwait(false);
    }

    [Then("the Upstash create flow creates the database")]
    public void ThenTheUpstashCreateFlowCreatesTheDatabase()
    {
        Assert.True(GetResult().Created);
        Assert.NotNull(_client.LastCreateRequest);
    }

    [Then("the Upstash create flow does not create the database")]
    public void ThenTheUpstashCreateFlowDoesNotCreateTheDatabase()
    {
        Assert.False(GetResult().Created);
        Assert.Null(_client.LastCreateRequest);
    }

    [Then("the Upstash create request payload is:")]
    public void ThenTheUpstashCreateRequestPayloadIs(DataTable table)
    {
        UpstashRedisCreateDatabaseRequest request = _client.LastCreateRequest
            ?? throw new InvalidOperationException("No create request was captured.");

        foreach (DataTableRow row in table.Rows)
        {
            object? actualValue = row["Property"] switch
            {
                nameof(UpstashRedisCreateDatabaseRequest.DatabaseName) => request.DatabaseName,
                nameof(UpstashRedisCreateDatabaseRequest.Platform) => request.Platform,
                nameof(UpstashRedisCreateDatabaseRequest.PrimaryRegion) => request.PrimaryRegion,
                nameof(UpstashRedisCreateDatabaseRequest.Plan) => request.Plan,
                nameof(UpstashRedisCreateDatabaseRequest.Budget) => request.Budget,
                nameof(UpstashRedisCreateDatabaseRequest.Eviction) => request.Eviction,
                nameof(UpstashRedisCreateDatabaseRequest.Tls) => request.Tls,
                _ => throw new InvalidOperationException($"Unknown create request property '{row["Property"]}'."),
            };

            Assert.Equal(row["Value"], Convert.ToString(actualValue, System.Globalization.CultureInfo.InvariantCulture)?.ToLowerInvariant());
        }
    }

    [Then("the Upstash create request read regions are {string}")]
    public void ThenTheUpstashCreateRequestReadRegionsAre(string readRegions)
    {
        UpstashRedisCreateDatabaseRequest request = _client.LastCreateRequest
            ?? throw new InvalidOperationException("No create request was captured.");

        Assert.Equal(readRegions.Split(',', StringSplitOptions.TrimEntries), request.ReadRegions);
    }

    [Then("the Upstash create flow returns Redis credentials for database {string}")]
    public void ThenTheUpstashCreateFlowReturnsRedisCredentialsForDatabase(string databaseName)
    {
        UpstashRedisDatabaseDetails database = GetResult().Database;

        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal("global-apt-1.upstash.io", database.Endpoint);
        Assert.Equal(6379, database.Port);
        Assert.Equal("redis-password", database.Password);
        Assert.True(database.Tls);
    }

    [Then("the Upstash create flow waits for database {string}")]
    public void ThenTheUpstashCreateFlowWaitsForDatabase(string databaseId)
    {
        string waitedDatabaseId = Assert.Single(_client.WaitedDatabaseIds);
        Assert.Equal(databaseId, waitedDatabaseId);
    }

    [Then("the Upstash create flow returns remote identity database {string} with id {string}")]
    public void ThenTheUpstashCreateFlowReturnsRemoteIdentityDatabaseWithId(string databaseName, string databaseId)
    {
        UpstashRedisRemoteIdentityState remoteIdentity = GetResult().RemoteIdentity;

        Assert.Equal(databaseName, remoteIdentity.DatabaseName);
        Assert.Equal(databaseId, remoteIdentity.ProviderDatabaseId);
    }

    [Then("the Upstash create flow fails with {string}")]
    public void ThenTheUpstashCreateFlowFailsWith(string exceptionTypeName)
    {
        Exception exception = _exception ?? throw new InvalidOperationException("The create flow did not fail.");

        Assert.Equal(exceptionTypeName, exception.GetType().Name);
    }

    [Then("the Upstash create flow fails with provider kind {string}")]
    public void ThenTheUpstashCreateFlowFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash create flow failure message contains {string}")]
    public void ThenTheUpstashCreateFlowFailureMessageContains(string value)
    {
        Exception exception = _exception ?? throw new InvalidOperationException("The create flow did not fail.");

        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    private UpstashRedisResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("No deployment was configured.");
    }

    private UpstashRedisOwnershipResolutionResult GetOwnership()
    {
        return _ownership ?? throw new InvalidOperationException("No ownership resolution was configured.");
    }

    private UpstashRedisCreateFlowResult GetResult()
    {
        return _result ?? throw new InvalidOperationException("No create flow result was captured.");
    }

    private static UpstashRedisDatabaseDetails CreateDatabaseDetails(
        string databaseName,
        string databaseId,
        bool includePassword)
    {
        return new UpstashRedisDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.upstash.io",
            Port = 6379,
            Password = includePassword ? "redis-password" : null,
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

    private sealed class FakeCreateFlowManagementClient : IUpstashRedisManagementClient
    {
        public UpstashRedisCreateDatabaseRequest? LastCreateRequest { get; private set; }

        public UpstashRedisDatabaseDetails? CreateResponse { get; set; }

        public UpstashRedisDatabaseDetails? ReadyResponse { get; set; }

        public UpstashRedisProviderException? CreateException { get; set; }

        public List<string> WaitedDatabaseIds { get; } = [];

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
            UpstashRedisCreateDatabaseRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCreateRequest = request;

            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(CreateResponse ?? throw new InvalidOperationException("No create response was configured."));
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WaitedDatabaseIds.Add(databaseId);

            return Task.FromResult(ReadyResponse ?? throw new InvalidOperationException("No ready response was configured."));
        }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
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
    }
}
