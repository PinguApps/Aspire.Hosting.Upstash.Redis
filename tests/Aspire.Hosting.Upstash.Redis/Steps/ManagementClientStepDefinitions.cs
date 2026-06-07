using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.Upstash.Redis.Management;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class ManagementClientStepDefinitions : IDisposable
{
    private const string DefaultAccountEmail = "pingu@example.com";
    private const string DefaultApiKey = "secret-key";

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;
    private UpstashRedisDatabaseDetails? _lastDatabase;
    private Exception? _lastException;

    public ManagementClientStepDefinitions()
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

    [Given("the Upstash management API returns an empty database list")]
    public void GivenTheUpstashManagementApiReturnsAnEmptyDatabaseList()
    {
        _handler.Enqueue(HttpStatusCode.OK, "[]");
    }

    [Given("the Upstash management API returns a list containing database {string}")]
    public void GivenTheUpstashManagementApiReturnsAListContainingDatabase(string databaseName)
    {
        _handler.Enqueue(
            HttpStatusCode.OK,
            $$"""
            [
              {
                "database_id": "db-orders",
                "database_name": "{{databaseName}}",
                "endpoint": "global-apt-1.upstash.io",
                "port": 6379,
                "state": "active",
                "primary_region": "eu-west-1",
                "read_regions": ["eu-west-2"]
              }
            ]
            """);
    }

    [Given("the Upstash management API returns duplicate databases named {string}")]
    public void GivenTheUpstashManagementApiReturnsDuplicateDatabasesNamed(string databaseName)
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

    [Given("the Upstash management API returns database details for {string}")]
    public void GivenTheUpstashManagementApiReturnsDatabaseDetailsFor(string databaseName)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, includePassword: true));
    }

    [Given("the Upstash management API returns database details for {string} with id {string}")]
    public void GivenTheUpstashManagementApiReturnsDatabaseDetailsForWithId(string databaseName, string databaseId)
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson(databaseName, includePassword: true, databaseId: databaseId));
    }

    [Given("the Upstash management API returns database details without a password")]
    public void GivenTheUpstashManagementApiReturnsDatabaseDetailsWithoutAPassword()
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: false));
    }

    [Given("the Upstash management API returns status {int} with error {string}")]
    public void GivenTheUpstashManagementApiReturnsStatusWithError(int statusCode, string error)
    {
        _handler.Enqueue((HttpStatusCode)statusCode, $$"""{ "error": "{{error}}" }""");
    }

    [Given("the Upstash management API returns OK for five operations")]
    public void GivenTheUpstashManagementApiReturnsOkForFiveOperations()
    {
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
        _handler.Enqueue(HttpStatusCode.OK, "\"OK\"\n");
    }

    [Given("the Upstash management API returns a modifying database then an active database")]
    public void GivenTheUpstashManagementApiReturnsAModifyingDatabaseThenAnActiveDatabase()
    {
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: true, state: "active", modifyingState: "updating"));
        _handler.Enqueue(HttpStatusCode.OK, CreateDatabaseDetailsJson("orders-cache", includePassword: true));
    }

    [Given("the Upstash management API waits until cancellation")]
    public void GivenTheUpstashManagementApiWaitsUntilCancellation()
    {
        _handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            };
        });
    }

    [Given("the Upstash management API fails before responding with {string}")]
    public void GivenTheUpstashManagementApiFailsBeforeRespondingWith(string failure)
    {
        _handler.Enqueue((_, _) => failure switch
        {
            "RequestException" => throw new HttpRequestException("The Upstash host could not be reached."),
            "Timeout" => throw new TaskCanceledException("The Upstash request timed out."),
            _ => throw new InvalidOperationException($"Unknown transport failure '{failure}'."),
        });
    }

    [When("the Upstash management client lists databases with account {string} and API key {string}")]
    public async Task WhenTheUpstashManagementClientListsDatabasesWithAccountAndApiKey(string accountEmail, string apiKey)
    {
        await CaptureExceptionAsync(async () =>
        {
            IUpstashRedisManagementClient client = CreateClient(accountEmail, apiKey);
            await client.ListDatabasesAsync(CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Upstash management client gets database {string}")]
    public async Task WhenTheUpstashManagementClientGetsDatabase(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().GetDatabaseAsync(databaseId, CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Upstash management client finds database {string} by name")]
    public async Task WhenTheUpstashManagementClientFindsDatabaseByName(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().FindDatabaseByNameAsync(databaseName, CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Upstash management client creates database {string}")]
    public async Task WhenTheUpstashManagementClientCreatesDatabase(string databaseName)
    {
        await CaptureExceptionAsync(async () =>
        {
            _lastDatabase = await CreateClient().CreateDatabaseAsync(
                new UpstashRedisCreateDatabaseRequest
                {
                    DatabaseName = databaseName,
                    Platform = "aws",
                    PrimaryRegion = "eu-west-1",
                    ReadRegions = ["eu-west-2"],
                    Plan = "payg",
                    Budget = 50,
                    Eviction = true,
                    Tls = true,
                },
                CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Upstash management client updates mutable settings for database {string}")]
    public async Task WhenTheUpstashManagementClientUpdatesMutableSettingsForDatabase(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
        {
            IUpstashRedisManagementClient client = CreateClient();

            await client.UpdateReadRegionsAsync(
                databaseId,
                new UpstashRedisUpdateRegionsRequest { ReadRegions = ["eu-west-2"] },
                CancellationToken.None).ConfigureAwait(false);

            await client.ChangePlanAsync(
                databaseId,
                new UpstashRedisChangePlanRequest { PlanName = "payg" },
                CancellationToken.None).ConfigureAwait(false);

            await client.UpdateBudgetAsync(
                databaseId,
                new UpstashRedisUpdateBudgetRequest { Budget = 50 },
                CancellationToken.None).ConfigureAwait(false);

            await client.SetEvictionAsync(databaseId, enabled: true, CancellationToken.None).ConfigureAwait(false);
            await client.SetEvictionAsync(databaseId, enabled: false, CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [When("the Upstash management client waits for database {string} to become ready")]
    public async Task WhenTheUpstashManagementClientWaitsForDatabaseToBecomeReady(string databaseId)
    {
        await CaptureExceptionAsync(async () =>
            _lastDatabase = await CreateClient().WaitUntilReadyAsync(
                databaseId,
                new UpstashRedisReadinessPollingOptions
                {
                    Timeout = TimeSpan.FromSeconds(1),
                    Delay = TimeSpan.Zero,
                },
                CancellationToken.None).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("the Upstash management client lists databases with a cancelled token")]
    public async Task WhenTheUpstashManagementClientListsDatabasesWithACancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync().ConfigureAwait(false);

        await CaptureExceptionAsync(async () =>
            await CreateClient().ListDatabasesAsync(cancellation.Token).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [When("a general Upstash provider exception is created with constructor {string}")]
    public void WhenAGeneralUpstashProviderExceptionIsCreatedWithConstructor(string constructor)
    {
        _lastException = constructor switch
        {
            "Parameterless" => new UpstashRedisProviderException(),
            "Message" => new UpstashRedisProviderException("Provider failure."),
            "MessageAndInner" => new UpstashRedisProviderException("Provider failure.", new InvalidOperationException()),
            _ => throw new InvalidOperationException($"Unknown constructor '{constructor}'."),
        };
    }

    [Then("the Upstash management request uses {word} {string}")]
    public void ThenTheUpstashManagementRequestUses(string method, string path)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);

        Assert.Equal(method, request.Method.Method);
        Assert.Equal(path, request.PathAndQuery);
    }

    [Then("the Upstash management request has the expected Basic auth header for account {string} and API key {string}")]
    public void ThenTheUpstashManagementRequestHasTheExpectedBasicAuthHeaderForAccountAndApiKey(string accountEmail, string apiKey)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);
        string expectedParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountEmail}:{apiKey}"));

        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(expectedParameter, request.AuthorizationParameter);
    }

    [Then("the Upstash management client returns database {string} with credentials")]
    public void ThenTheUpstashManagementClientReturnsDatabaseWithCredentials(string databaseName)
    {
        UpstashRedisDatabaseDetails database =
            _lastDatabase ?? throw new InvalidOperationException("No database was returned.");

        Assert.Equal("db-orders", database.DatabaseId);
        Assert.Equal(databaseName, database.DatabaseName);
        Assert.Equal("global-apt-1.upstash.io", database.Endpoint);
        Assert.Equal(6379, database.Port);
        Assert.Equal("redis-password", database.Password);
        Assert.True(database.Tls);
        Assert.Equal("active", database.State);
        Assert.Null(database.ModifyingState);
        Assert.Equal("eu-west-1", database.PrimaryRegion);
        Assert.Equal(["eu-west-2"], database.ReadRegions);
        Assert.Equal("payg", database.Type);
        Assert.Equal(50, database.Budget);
        Assert.True(database.Eviction);
        Assert.Equal("cust-1", database.CustomerId);
    }

    [Then("the Upstash management request sequence is:")]
    public void ThenTheUpstashManagementRequestSequenceIs(DataTable table)
    {
        Assert.Equal(table.Rows.Count, _handler.Requests.Count);

        for (int requestIndex = 0; requestIndex < table.Rows.Count; requestIndex++)
        {
            Assert.Equal(table.Rows[requestIndex]["Method"], _handler.Requests[requestIndex].Method.Method);
            Assert.Equal(table.Rows[requestIndex]["Path"], _handler.Requests[requestIndex].PathAndQuery);
        }
    }

    [Then("the Upstash management request body contains:")]
    public void ThenTheUpstashManagementRequestBodyContains(DataTable table)
    {
        CapturedHttpRequest request = Assert.Single(_handler.Requests);
        Assert.NotNull(request.Content);

        using JsonDocument document = JsonDocument.Parse(request.Content);

        foreach (DataTableRow row in table.Rows)
        {
            JsonElement value = document.RootElement.GetProperty(row["Property"]);
            Assert.Equal(row["Value"], value.ValueKind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.GetInt32().ToString(),
                _ => value.GetString(),
            });
        }
    }

    [Then("the Upstash management client fails with provider kind {string}")]
    public void ThenTheUpstashManagementClientFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_lastException);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash management failure message does not contain {string}")]
    public void ThenTheUpstashManagementFailureMessageDoesNotContain(string value)
    {
        Exception exception = _lastException ?? throw new InvalidOperationException("No exception was captured.");

        Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);
    }

    [Then("the Upstash management client operation is cancelled")]
    public void ThenTheUpstashManagementClientOperationIsCancelled()
    {
        Assert.IsAssignableFrom<OperationCanceledException>(_lastException);
    }

    private IUpstashRedisManagementClient CreateClient(
        string accountEmail = DefaultAccountEmail,
        string apiKey = DefaultApiKey)
    {
        return new UpstashRedisManagementClient(
            _httpClient,
            new UpstashRedisManagementCredentials(accountEmail, apiKey));
    }

    private async Task CaptureExceptionAsync(Func<Task> operation)
    {
        _lastException = await Record.ExceptionAsync(operation).ConfigureAwait(false);
    }

    private static string CreateDatabaseDetailsJson(
        string databaseName,
        bool includePassword,
        string state = "active",
        string? modifyingState = null,
        string databaseId = "db-orders")
    {
        string passwordJson = includePassword ? "\"password\": \"redis-password\"," : string.Empty;
        string modifyingStateJson = modifyingState is null ? "null" : $"\"{modifyingState}\"";

        return $$"""
        {
          "database_id": "{{databaseId}}",
          "database_name": "{{databaseName}}",
          "endpoint": "global-apt-1.upstash.io",
          "port": 6379,
          {{passwordJson}}
          "tls": true,
          "state": "{{state}}",
          "modifying_state": {{modifyingStateJson}},
          "primary_region": "eu-west-1",
          "read_regions": ["eu-west-2"],
          "type": "payg",
          "budget": 50,
          "eviction": true,
          "customer_id": "cust-1"
        }
        """;
    }
}
