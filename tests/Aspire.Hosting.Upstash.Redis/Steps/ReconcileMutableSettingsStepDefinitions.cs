using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class ReconcileMutableSettingsStepDefinitions
{
    private readonly FakeReconcileManagementClient _client = new();
    private UpstashRedisDatabaseDetails? _database;
    private UpstashRedisDatabaseDetails? _result;
    private Exception? _exception;

    [Given("the Upstash reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public void GivenTheUpstashReconcileTargetDatabaseHasSettingsWithEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        SetDatabase(readRegions, plan, budget, eviction: true);
    }

    [Given("the Upstash reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction disabled")]
    public void GivenTheUpstashReconcileTargetDatabaseHasSettingsWithEvictionDisabled(
        string readRegions,
        string plan,
        int budget)
    {
        SetDatabase(readRegions, plan, budget, eviction: false);
    }

    [Given("the Upstash reconcile provider fails plan mutations")]
    public void GivenTheUpstashReconcileProviderFailsPlanMutations()
    {
        _client.FailingMutation = "plan";
    }

    [Given("the Upstash reconcile provider does not persist budget mutations")]
    public void GivenTheUpstashReconcileProviderDoesNotPersistBudgetMutations()
    {
        _client.IgnoredMutation = "budget";
    }

    [When("Upstash Redis reconciliation runs with read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public async Task WhenUpstashRedisReconciliationRunsWithSettingsAndEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        await ReconcileAsync(options =>
        {
            options.ReadRegions = ParseReadRegions(readRegions);
            options.Plan = plan;
            options.SetBudget(budget);
            options.Eviction = true;
        }).ConfigureAwait(false);
    }

    [When("Upstash Redis reconciliation runs with only plan {string}")]
    public async Task WhenUpstashRedisReconciliationRunsWithOnlyPlan(string plan)
    {
        await ReconcileAsync(options => options.Plan = plan).ConfigureAwait(false);
    }

    [When("Upstash Redis reconciliation is attempted with only plan {string}")]
    public async Task WhenUpstashRedisReconciliationIsAttemptedWithOnlyPlan(string plan)
    {
        await TryReconcileAsync(options => options.Plan = plan).ConfigureAwait(false);
    }

    [When("Upstash Redis reconciliation is attempted with only budget {int}")]
    public async Task WhenUpstashRedisReconciliationIsAttemptedWithOnlyBudget(int budget)
    {
        await TryReconcileAsync(options => options.SetBudget(budget)).ConfigureAwait(false);
    }

    [When("a general Upstash reconciliation exception is created with constructor {string}")]
    public void WhenAGeneralUpstashReconciliationExceptionIsCreatedWithConstructor(string constructor)
    {
        _exception = constructor switch
        {
            "Parameterless" => new UpstashRedisReconciliationException(),
            "Message" => new UpstashRedisReconciliationException("Reconciliation failure."),
            "MessageAndInner" => new UpstashRedisReconciliationException("Reconciliation failure.", new InvalidOperationException()),
            _ => throw new InvalidOperationException($"Unknown constructor '{constructor}'."),
        };
    }

    [Then("Upstash Redis reconciliation succeeds")]
    public void ThenUpstashRedisReconciliationSucceeds()
    {
        Assert.Null(_exception);
        Assert.NotNull(_result);
    }

    [Then("the Upstash reconcile provider recorded no mutation calls")]
    public void ThenTheUpstashReconcileProviderRecordedNoMutationCalls()
    {
        Assert.Empty(_client.Mutations);
    }

    [Then("the Upstash reconcile provider recorded mutation calls in order:")]
    public void ThenTheUpstashReconcileProviderRecordedMutationCallsInOrder(DataTable table)
    {
        string[] expectedMutations = [.. table.Rows.Select(row => row["mutation"])];

        Assert.Equal(expectedMutations, _client.Mutations);
    }

    [Then("the Upstash reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction enabled")]
    public void ThenTheUpstashReconcileTargetDatabaseHasSettingsWithEvictionEnabled(
        string readRegions,
        string plan,
        int budget)
    {
        AssertDatabase(readRegions, plan, budget, eviction: true);
    }

    [Then("the Upstash reconcile target database has read regions {string}, plan {string}, budget {int}, and eviction disabled")]
    public void ThenTheUpstashReconcileTargetDatabaseHasSettingsWithEvictionDisabled(
        string readRegions,
        string plan,
        int budget)
    {
        AssertDatabase(readRegions, plan, budget, eviction: false);
    }

    [Then("Upstash Redis reconciliation fails for setting {string}")]
    public void ThenUpstashRedisReconciliationFailsForSetting(string settingName)
    {
        UpstashRedisReconciliationException exception = Assert.IsType<UpstashRedisReconciliationException>(_exception);

        Assert.Equal(settingName, exception.SettingName);
    }

    [Then("Upstash Redis reconciliation fails with provider kind {string}")]
    public void ThenUpstashRedisReconciliationFailsWithProviderKind(string failureKind)
    {
        UpstashRedisReconciliationException exception = Assert.IsType<UpstashRedisReconciliationException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash Redis reconciliation failure message contains {string}")]
    public void ThenTheUpstashRedisReconciliationFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    private void SetDatabase(string readRegions, string plan, int budget, bool eviction)
    {
        _database = new UpstashRedisDatabaseDetails
        {
            DatabaseId = "db-orders-cache",
            DatabaseName = "orders-cache",
            Endpoint = "global-apt-1.upstash.io",
            Port = 6379,
            Password = "test-password",
            Tls = true,
            State = "active",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ParseRegionNames(readRegions),
            Type = plan,
            Budget = budget,
            Eviction = eviction,
        };

        _client.Database = _database;
    }

    private async Task ReconcileAsync(Action<UpstashRedisDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => ReconcileCoreAsync(configure)).ConfigureAwait(false);

        if (_exception is not null)
        {
            throw _exception;
        }
    }

    private async Task TryReconcileAsync(Action<UpstashRedisDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => ReconcileCoreAsync(configure)).ConfigureAwait(false);
    }

    private async Task ReconcileCoreAsync(Action<UpstashRedisDeploymentOptions> configure)
    {
        UpstashRedisDeploymentOptions options = new();
        configure(options);

        UpstashRedisDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        _result = await new UpstashRedisReconciler(_client)
            .ReconcileAsync(database, options.ToProviderOptions(), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private void AssertDatabase(string readRegions, string plan, int budget, bool eviction)
    {
        UpstashRedisDatabaseDetails database =
            _result ?? throw new InvalidOperationException("Reconciliation has not completed.");

        Assert.Equal(ParseRegionNames(readRegions).Order(StringComparer.Ordinal), (database.ReadRegions ?? []).Order(StringComparer.Ordinal));
        Assert.Equal(plan, database.Type);
        Assert.Equal(budget, database.Budget);
        Assert.Equal(eviction, database.Eviction);
    }

    private static IReadOnlyList<UpstashRedisValue> ParseReadRegions(string readRegions)
    {
        return [.. ParseRegionNames(readRegions).Select(UpstashRedisValue.FromString)];
    }

    private static IReadOnlyList<string> ParseRegionNames(string readRegions)
    {
        return [.. readRegions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
    }

    private sealed class FakeReconcileManagementClient : IUpstashRedisManagementClient
    {
        public UpstashRedisDatabaseDetails? Database { get; set; }

        public string? FailingMutation { get; set; }

        public string? IgnoredMutation { get; set; }

        public List<string> Mutations { get; } = [];

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            UpstashRedisDatabaseDetails database = GetDatabase(databaseId);

            return Task.FromResult(Clone(database));
        }

        public Task UpdateReadRegionsAsync(string databaseId, UpstashRedisUpdateRegionsRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "read regions", database => database.ReadRegions = request.ReadRegions);

            return Task.CompletedTask;
        }

        public Task ChangePlanAsync(string databaseId, UpstashRedisChangePlanRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "plan", database => database.Type = request.PlanName);

            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(string databaseId, UpstashRedisUpdateBudgetRequest request, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "budget", database => database.Budget = request.Budget);

            return Task.CompletedTask;
        }

        public Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
        {
            Mutate(databaseId, "eviction", database => database.Eviction = enabled);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(UpstashRedisCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            UpstashRedisDatabaseDetails database = GetDatabase(databaseId);

            return Task.FromResult(Clone(database));
        }

        private void Mutate(string databaseId, string mutation, Action<UpstashRedisDatabaseDetails> apply)
        {
            Mutations.Add(mutation);

            if (FailingMutation == mutation)
            {
                throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.Validation,
                    statusCode: null,
                    $"Provider rejected {mutation}.");
            }

            if (IgnoredMutation == mutation)
            {
                return;
            }

            apply(GetDatabase(databaseId));
        }

        private UpstashRedisDatabaseDetails GetDatabase(string databaseId)
        {
            UpstashRedisDatabaseDetails database =
                Database ?? throw new InvalidOperationException("No fake database is configured.");

            Assert.Equal(database.DatabaseId, databaseId);

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
                Budget = database.Budget,
                Eviction = database.Eviction,
                CustomerId = database.CustomerId,
            };
        }
    }
}
