using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Aspire.Hosting.Upstash.Redis.Management;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class RedisConnectionOutputStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;
    private Exception? _exception;

    public RedisConnectionOutputStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [When("Upstash Redis connection output is applied with endpoint {string}, port {int}, password {string}, and TLS enabled")]
    public void WhenUpstashRedisConnectionOutputIsAppliedWithEndpointPortPasswordAndTlsEnabled(
        string endpoint,
        int port,
        string password)
    {
        _context.RedisBuilder.Resource.ApplyUpstashRedisConnectionOutput(CreateDatabase(endpoint, port, password, tls: true));
    }

    [When("applying Upstash Redis connection output with endpoint {string} is attempted")]
    public void WhenApplyingUpstashRedisConnectionOutputWithEndpointIsAttempted(string endpoint)
    {
        _exception = Record.Exception(() =>
            _context.RedisBuilder.Resource.ApplyUpstashRedisConnectionOutput(CreateDatabase(endpoint, 6379, "redis-password", tls: true)));
    }

    [When("applying Upstash Redis connection output without an endpoint is attempted")]
    public void WhenApplyingUpstashRedisConnectionOutputWithoutAnEndpointIsAttempted()
    {
        _exception = Record.Exception(() =>
            _context.RedisBuilder.Resource.ApplyUpstashRedisConnectionOutput(CreateDatabase(endpoint: null, 6379, "redis-password", tls: true)));
    }

    [Then("the Redis connection string reference resolves to {string}")]
    public async Task ThenTheRedisConnectionStringReferenceResolvesTo(string expectedConnectionString)
    {
        IResourceWithConnectionString redisConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);

        string? connectionString = await redisConnection
            .GetConnectionStringAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Then("the Redis connection properties contain:")]
    public void ThenTheRedisConnectionPropertiesContain(DataTable table)
    {
        Dictionary<string, ReferenceExpression> properties = _context.RedisBuilder.Resource.Annotations
            .OfType<ConnectionPropertyAnnotation>()
            .ToDictionary(annotation => annotation.Name, annotation => annotation.Value, StringComparer.OrdinalIgnoreCase);

        foreach (DataTableRow row in table.Rows)
        {
            ReferenceExpression property = Assert.Contains(row["Name"], properties);

            Assert.Equal(row["Value"], property.ValueExpression);
        }
    }

    [Then("the Redis connection output does not contain {string}")]
    public void ThenTheRedisConnectionOutputDoesNotContain(string unexpectedValue)
    {
        UpstashRedisConnectionOutput output = GetOutput();

        Assert.DoesNotContain(unexpectedValue, output.ConnectionString, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Host, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Port.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Password, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedValue, output.Uri, StringComparison.Ordinal);
    }

    [Then("the Redis resource has no Upstash connection output")]
    public void ThenTheRedisResourceHasNoUpstashConnectionOutput()
    {
        Assert.DoesNotContain(
            _context.RedisBuilder.Resource.Annotations,
            annotation => annotation is UpstashRedisConnectionOutputAnnotation or ConnectionStringRedirectAnnotation);
    }

    [Then("the Redis connection properties still use the standard Redis surface")]
    public void ThenTheRedisConnectionPropertiesStillUseTheStandardRedisSurface()
    {
        AspireModelAssertions.AssertRedisConnectionProperties(_context.RedisBuilder.Resource);
    }

    [Then("Upstash Redis connection output fails with provider kind {string}")]
    public void ThenUpstashRedisConnectionOutputFailsWithProviderKind(string failureKind)
    {
        UpstashRedisProviderException exception = Assert.IsType<UpstashRedisProviderException>(_exception);

        Assert.Equal(Enum.Parse<UpstashRedisProviderFailureKind>(failureKind), exception.FailureKind);
    }

    [Then("the Upstash Redis connection output failure message contains {string}")]
    public void ThenTheUpstashRedisConnectionOutputFailureMessageContains(string expectedMessage)
    {
        Exception exception =
            _exception ?? throw new InvalidOperationException("The Upstash Redis connection output did not fail.");

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    private static UpstashRedisDatabaseDetails CreateDatabase(
        string? endpoint,
        int port,
        string password,
        bool tls)
    {
        return new UpstashRedisDatabaseDetails
        {
            DatabaseId = "db-orders-cache",
            DatabaseName = "orders-cache",
            Endpoint = endpoint!,
            Port = port,
            Password = password,
            Tls = tls,
            State = "active",
            PrimaryRegion = "eu-west-1",
            ReadRegions = ["eu-west-2"],
            Type = "payg",
            Budget = 100,
            Eviction = true,
        };
    }

    private UpstashRedisConnectionOutput GetOutput()
    {
        return Assert
            .Single(_context.RedisBuilder.Resource.Annotations.OfType<UpstashRedisConnectionOutputAnnotation>())
            .Output;
    }
}
