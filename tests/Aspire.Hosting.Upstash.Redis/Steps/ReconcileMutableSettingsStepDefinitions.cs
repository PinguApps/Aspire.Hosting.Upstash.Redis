using System.Runtime.ExceptionServices;
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
    private UpstashRedisRemoteIdentityState? _cachedIdentity;
    private UpstashRedisRemoteIdentityState? _savedIdentity;
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

    [Given("the Upstash reconcile target database has read regions {string}, coarse plan {string}, fixed plan {string}, budget {int}, and eviction disabled")]
    public void GivenTheUpstashReconcileTargetDatabaseHasFixedPlanWithEvictionDisabled(
        string readRegions,
        string coarsePlan,
        string fixedPlan,
        int budget)
    {
        SetDatabase(readRegions, coarsePlan, budget, eviction: false);

        UpstashRedisDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.DbDiskThreshold = GetFixedPlanBytes(fixedPlan);
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

    [Given("cached Upstash remote identity for deployment is database {string} with id {string}")]
    public void GivenCachedUpstashRemoteIdentityForDeploymentIsDatabaseWithId(string databaseName, string databaseId)
    {
        _cachedIdentity = new UpstashRedisRemoteIdentityState(databaseName, databaseId);
    }

    [Given("the Upstash reconcile provider has database {string} with id {string}")]
    public void GivenTheUpstashReconcileProviderHasDatabaseWithId(string databaseName, string databaseId)
    {
        _client.Databases.Add(CreateDatabase(databaseName, databaseId, "eu-west-1", "payg", 100, eviction: false));
    }

    [Given("the Upstash reconcile target database provider name is {string}")]
    public void GivenTheUpstashReconcileTargetDatabaseProviderNameIs(string databaseName)
    {
        UpstashRedisDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.DatabaseName = databaseName;
    }

    [Given("the Upstash reconcile target database has no password")]
    public void GivenTheUpstashReconcileTargetDatabaseHasNoPassword()
    {
        UpstashRedisDatabaseDetails database =
            _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        database.Password = null;
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

    [When("Upstash Redis reconciliation runs with only {word} set to {string}")]
    public async Task WhenUpstashRedisReconciliationRunsWithOnlySettingSetTo(string settingName, string value)
    {
        await ReconcileAsync(options => ApplySetting(options, settingName, value)).ConfigureAwait(false);
    }

    [When("Upstash Redis reconciliation runs with only read regions set to {string}")]
    public async Task WhenUpstashRedisReconciliationRunsWithOnlyReadRegionsSetTo(string value)
    {
        await ReconcileAsync(options => ApplySetting(options, "read regions", value)).ConfigureAwait(false);
    }

    [When("Upstash Redis reconciliation runs with only TLS enabled")]
    public async Task WhenUpstashRedisReconciliationRunsWithOnlyTlsEnabled()
    {
        await ReconcileAsync(options => options.Tls = true).ConfigureAwait(false);
    }

    [When("the Upstash Redis deployment pipeline runs for existing-only with only plan {string}")]
    public async Task WhenTheUpstashRedisDeploymentPipelineRunsForExistingOnlyWithOnlyPlan(string plan)
    {
        await TryRunDeploymentPipelineAsync(
            UpstashRedisOwnershipMode.ExistingOnly,
            options => options.Plan = plan).ConfigureAwait(false);
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

    [Then("the Upstash reconcile provider did not attempt reset-password")]
    public void ThenTheUpstashReconcileProviderDidNotAttemptResetPassword()
    {
        Assert.DoesNotContain(_client.Operations, operation => operation.Contains("reset-password", StringComparison.Ordinal));
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

    [Then("Upstash Redis deployment fails with provider kind {string}")]
    public void ThenUpstashRedisDeploymentFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash Redis reconciliation failure message contains {string}")]
    public void ThenTheUpstashRedisReconciliationFailureMessageContains(string expectedText)
    {
        Assert.NotNull(_exception);
        Assert.Contains(expectedText, _exception.Message, StringComparison.Ordinal);
    }

    [Then("the Upstash Redis deployment saved remote identity database {string} with id {string}")]
    public void ThenTheUpstashRedisDeploymentSavedRemoteIdentityDatabaseWithId(string databaseName, string databaseId)
    {
        Assert.NotNull(_savedIdentity);
        Assert.Equal(databaseName, _savedIdentity.DatabaseName);
        Assert.Equal(databaseId, _savedIdentity.ProviderDatabaseId);
    }

    private void SetDatabase(string readRegions, string plan, int budget, bool eviction)
    {
        _database = CreateDatabase("orders-cache", "db-orders-cache", readRegions, plan, budget, eviction);

        _client.Database = _database;
    }

    private async Task ReconcileAsync(Action<UpstashRedisDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => ReconcileCoreAsync(configure)).ConfigureAwait(false);

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
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

    private async Task TryRunDeploymentPipelineAsync(
        UpstashRedisOwnershipMode ownershipMode,
        Action<UpstashRedisDeploymentOptions> configure)
    {
        _exception = await Record.ExceptionAsync(() => RunDeploymentPipelineAsync(ownershipMode, configure)).ConfigureAwait(false);
    }

    private async Task RunDeploymentPipelineAsync(
        UpstashRedisOwnershipMode ownershipMode,
        Action<UpstashRedisDeploymentOptions> configure)
    {
        UpstashRedisDeploymentOptions options = new();
        configure(options);

        _ = _database ?? throw new InvalidOperationException("The reconcile target database has not been configured.");

        _result = await UpstashRedisDeploymentPipeline
            .ExecuteAsync(
                new UpstashRedisResolvedDeployment(
                    "orders-cache",
                    ownershipMode,
                    new UpstashRedisManagementCredentials("owner@example.com", "management-secret"),
                    options.ToProviderOptions()),
                _client,
                _cachedIdentity,
                identityState =>
                {
                    _savedIdentity = identityState;
                    return Task.CompletedTask;
                },
                CancellationToken.None)
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

    private static void ApplySetting(
        UpstashRedisDeploymentOptions options,
        string settingName,
        string value)
    {
        switch (settingName)
        {
            case "read regions":
                options.ReadRegions = ParseReadRegions(value);
                break;
            case "plan":
                options.Plan = value;
                break;
            case "budget":
                options.SetBudget(int.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case "eviction":
                options.Eviction = bool.Parse(value);
                break;
            default:
                throw new InvalidOperationException($"Unknown setting '{settingName}'.");
        }
    }

    private static UpstashRedisDatabaseDetails CreateDatabase(
        string databaseName,
        string databaseId,
        string readRegions,
        string plan,
        int budget,
        bool eviction)
    {
        return new UpstashRedisDatabaseDetails
        {
            DatabaseId = databaseId,
            DatabaseName = databaseName,
            Endpoint = "global-apt-1.upstash.io",
            Port = 6379,
            Password = "test-password",
            Tls = true,
            State = "active",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ParseRegionNames(readRegions),
            Type = plan,
            DbDiskThreshold = plan == "payg" ? 100L * 1024L * 1024L * 1024L : null,
            Budget = budget,
            Eviction = eviction,
        };
    }

    private static long GetFixedPlanBytes(string fixedPlan)
    {
        long? fixedPlanBytes = GetFixedPlanBytesOrNull(fixedPlan);

        return fixedPlanBytes
            ?? throw new InvalidOperationException($"Unknown fixed plan '{fixedPlan}'.");
    }

    private static long? GetFixedPlanBytesOrNull(string fixedPlan)
    {
        const long mebibyte = 1024L * 1024L;
        const long gibibyte = 1024L * mebibyte;

        switch (fixedPlan)
        {
            case "fixed_250mb":
                return 250L * mebibyte;
            case "fixed_1gb":
                return 1L * gibibyte;
            case "fixed_5gb":
                return 5L * gibibyte;
            case "fixed_10gb":
                return 10L * gibibyte;
            case "fixed_50gb":
                return 50L * gibibyte;
            case "fixed_100gb":
                return 100L * gibibyte;
            case "fixed_500gb":
                return 500L * gibibyte;
            default:
                return null;
        }
    }

    private sealed class FakeReconcileManagementClient : IUpstashRedisManagementClient
    {
        public UpstashRedisDatabaseDetails? Database { get; set; }

        public List<UpstashRedisDatabaseDetails> Databases { get; } = [];

        public string? FailingMutation { get; set; }

        public string? IgnoredMutation { get; set; }

        public List<string> Mutations { get; } = [];

        public List<string> Operations { get; } = [];

        public Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            Operations.Add($"GET /redis/database/{databaseId}");

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
            Mutate(databaseId, "plan", database => ApplyPlanMutation(database, request.PlanName));

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
            Operations.Add("GET /redis/databases");

            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
        {
            Operations.Add($"GET /redis/databases?name={databaseName}");

            UpstashRedisDatabaseDetails? database = GetDatabases()
                .SingleOrDefault(database => database.DatabaseName == databaseName);

            return Task.FromResult(database is not null
                ? Clone(database)
                : null);
        }

        public Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(UpstashRedisCreateDatabaseRequest request, CancellationToken cancellationToken)
        {
            Operations.Add("POST /redis/database");

            throw new NotSupportedException();
        }

        public Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
            string databaseId,
            UpstashRedisReadinessPollingOptions pollingOptions,
            CancellationToken cancellationToken)
        {
            Operations.Add($"WAIT /redis/database/{databaseId}");

            UpstashRedisDatabaseDetails database = GetDatabase(databaseId);

            return Task.FromResult(Clone(database));
        }

        private void Mutate(string databaseId, string mutation, Action<UpstashRedisDatabaseDetails> apply)
        {
            Operations.Add($"MUTATE {mutation} /redis/database/{databaseId}");
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

        private static void ApplyPlanMutation(UpstashRedisDatabaseDetails database, string planName)
        {
            long? fixedPlanBytes = GetFixedPlanBytesOrNull(planName);

            if (fixedPlanBytes is not null)
            {
                database.Type = "pro";
                database.DbDiskThreshold = fixedPlanBytes;

                return;
            }

            database.Type = planName;
            database.DbDiskThreshold = planName == "payg" ? 100L * 1024L * 1024L * 1024L : null;
        }

        private UpstashRedisDatabaseDetails GetDatabase(string databaseId)
        {
            UpstashRedisDatabaseDetails? database = GetDatabases()
                .SingleOrDefault(database => database.DatabaseId == databaseId);

            Assert.NotNull(database);

            return database;
        }

        private IEnumerable<UpstashRedisDatabaseDetails> GetDatabases()
        {
            if (Database is not null)
            {
                yield return Database;
            }

            foreach (UpstashRedisDatabaseDetails database in Databases)
            {
                yield return database;
            }
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
