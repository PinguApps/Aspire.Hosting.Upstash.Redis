using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class DeploymentDiagnosticsStepDefinitions
{
    private readonly DiagnosticManagementClient _client = new();
    private readonly CapturingProgressReporter _progressReporter = new();
    private UpstashRedisResolvedDeployment? _deployment;
    private Exception? _exception;
    private string? _redactedMessage;

    [Given("an Upstash diagnostic deployment for database {string}")]
    public void GivenAnUpstashDiagnosticDeploymentForDatabase(string databaseName)
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
            UpstashRedisOwnershipMode.CreateOrAdopt,
            new UpstashRedisManagementCredentials("owner@example.com", "api-key-secret"),
            options.ToProviderOptions());
    }

    [Given("the Upstash diagnostic provider has no existing database")]
    public void GivenTheUpstashDiagnosticProviderHasNoExistingDatabase()
    {
        _client.Database = null;
    }

    [Given("the Upstash diagnostic provider has existing database {string} with id {string}")]
    public void GivenTheUpstashDiagnosticProviderHasExistingDatabaseWithId(string databaseName, string databaseId)
    {
        _client.Database = CreateDatabase(databaseName, databaseId, plan: "payg");
    }

    [Given("the Upstash diagnostic provider fails plan mutations")]
    public void GivenTheUpstashDiagnosticProviderFailsPlanMutations()
    {
        _client.Database = CreateDatabase("orders-cache", "db-orders", plan: "free");
        _client.FailPlanMutation = true;
    }

    [When("the Upstash diagnostic deployment pipeline runs")]
    public async Task WhenTheUpstashDiagnosticDeploymentPipelineRuns()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);

        if (_exception is not null)
        {
            throw _exception;
        }
    }

    [When("the Upstash diagnostic deployment pipeline is attempted")]
    public async Task WhenTheUpstashDiagnosticDeploymentPipelineIsAttempted()
    {
        _exception = await Record.ExceptionAsync(RunPipelineAsync).ConfigureAwait(false);
    }

    [When("the Upstash diagnostic message {string} is redacted")]
    public void WhenTheUpstashDiagnosticMessageIsRedacted(string message)
    {
        UpstashRedisResolvedDeployment deployment = GetDeployment();
        UpstashRedisDatabaseDetails database = _client.Database
            ?? CreateDatabase(deployment.DatabaseName, "db-orders", plan: "payg");

        _redactedMessage = UpstashRedisDeploymentDiagnostics.Redact(message, deployment, database);
    }

    [Then("the Upstash diagnostic progress phases are:")]
    public void ThenTheUpstashDiagnosticProgressPhasesAre(DataTable table)
    {
        UpstashRedisDeploymentPhase[] expectedPhases =
            [.. table.Rows.Select(row => Enum.Parse<UpstashRedisDeploymentPhase>(row["phase"].Trim()))];

        Assert.Equal(expectedPhases, _progressReporter.Progress.Select(progress => progress.Phase));
    }

    [Then("the Upstash diagnostic progress contains {string}")]
    public void ThenTheUpstashDiagnosticProgressContains(string expectedText)
    {
        Assert.Contains(
            _progressReporter.Progress,
            progress => progress.Message.Contains(expectedText, StringComparison.Ordinal));
    }

    [Then("the Upstash diagnostic progress contains provider id {string}")]
    public void ThenTheUpstashDiagnosticProgressContainsProviderId(string providerDatabaseId)
    {
        Assert.Contains(
            _progressReporter.Progress,
            progress => string.Equals(progress.ProviderDatabaseId, providerDatabaseId, StringComparison.Ordinal));
    }

    [Then("the redacted Upstash diagnostic message does not contain {string}")]
    public void ThenTheRedactedUpstashDiagnosticMessageDoesNotContain(string unexpectedText)
    {
        Assert.DoesNotContain(unexpectedText, GetRedactedMessage(), StringComparison.Ordinal);
    }

    [Then("the redacted Upstash diagnostic message contains {string}")]
    public void ThenTheRedactedUpstashDiagnosticMessageContains(string expectedText)
    {
        Assert.Contains(expectedText, GetRedactedMessage(), StringComparison.Ordinal);
    }

    [Then("the Upstash diagnostic deployment failure message contains {string}")]
    public void ThenTheUpstashDiagnosticDeploymentFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    private async Task RunPipelineAsync()
    {
        await UpstashRedisDeploymentPipeline.ExecuteAsync(
            GetDeployment(),
            _client,
            cachedIdentity: null,
            saveIdentityStateAsync: null,
            _progressReporter,
            resourceName: "cache",
            CancellationToken.None).ConfigureAwait(false);
    }

    private UpstashRedisResolvedDeployment GetDeployment()
    {
        return _deployment ?? throw new InvalidOperationException("No diagnostic deployment was configured.");
    }

    private string GetRedactedMessage()
    {
        return _redactedMessage ?? throw new InvalidOperationException("No diagnostic message was redacted.");
    }

    private static UpstashRedisDatabaseDetails CreateDatabase(string databaseName, string databaseId, string plan)
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
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = plan,
            Budget = 360,
            Eviction = true,
        };
    }

    private sealed class CapturingProgressReporter : IUpstashRedisDeploymentProgressReporter
    {
        public List<UpstashRedisDeploymentProgress> Progress { get; } = [];

        public void Report(UpstashRedisDeploymentProgress progress)
        {
            Progress.Add(progress);
        }
    }

    private sealed class DiagnosticManagementClient : IUpstashRedisManagementClient
    {
        public UpstashRedisDatabaseDetails? Database { get; set; }

        public bool FailPlanMutation { get; set; }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            return Task.FromResult(Database?.DatabaseName == databaseName ? Clone(Database) : null);
        }

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Clone(GetDatabase(databaseId)));
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(UpstashRedisCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            Database = CreateDatabase(request.DatabaseName, $"db-{request.DatabaseName}", request.Plan ?? "payg");

            return Task.FromResult(Clone(Database));
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Clone(GetDatabase(databaseId)));
        }

        public Task UpdateReadRegionsAsync(string databaseId, UpstashRedisUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).ReadRegions = request.ReadRegions;

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, UpstashRedisChangePlanRequest request, CancellationToken cancellationToken)
        {
            if (FailPlanMutation)
            {
                throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.Validation,
                    statusCode: null,
                    "Provider rejected plan.");
            }

            GetDatabase(databaseId).Type = request.PlanName;

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, UpstashRedisUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).Budget = request.Budget;

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            GetDatabase(databaseId).Eviction = enabled;

            return Task.CompletedTask;
        }

        private UpstashRedisDatabaseDetails GetDatabase(string databaseId)
        {
            UpstashRedisDatabaseDetails? database = Database;

            Assert.NotNull(database);
            Assert.Equal(databaseId, database.DatabaseId);

            return database;
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
