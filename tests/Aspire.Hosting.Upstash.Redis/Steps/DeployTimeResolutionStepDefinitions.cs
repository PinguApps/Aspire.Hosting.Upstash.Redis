using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class DeployTimeResolutionStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;

    public DeployTimeResolutionStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [When("the Redis resource is marked for Upstash with resolvable parameter inputs")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithResolvableParameterInputs()
    {
        _context.MarkRedisForUpstashWithResolvableParameterInputs();
    }

    [When("the Redis resource is marked for Upstash with an unresolved API key parameter")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAnUnresolvedApiKeyParameter()
    {
        _context.MarkRedisForUpstashWithUnresolvedApiKeyParameter();
    }

    [When("the Upstash deployment inputs are resolved")]
    public async Task WhenTheUpstashDeploymentInputsAreResolved()
    {
        await _context.ResolveUpstashDeploymentInputsAsync();
    }

    [When("resolving the Upstash deployment inputs is attempted")]
    public async Task WhenResolvingTheUpstashDeploymentInputsIsAttempted()
    {
        await _context.TryResolveUpstashDeploymentInputsAsync();
    }

    [Then("the resolved Upstash deployment targets database {string}")]
    public void ThenTheResolvedUpstashDeploymentTargetsDatabase(string databaseName)
    {
        UpstashRedisResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal(databaseName, deployment.DatabaseName);
        Assert.Equal(UpstashRedisOwnershipMode.CreateOnly, deployment.OwnershipMode);
    }

    [Then("the resolved Upstash management credentials use account email {string}")]
    public void ThenTheResolvedUpstashManagementCredentialsUseAccountEmail(string accountEmail)
    {
        UpstashRedisResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal(accountEmail, deployment.ManagementCredentials.AccountEmail);
    }

    [Then("the resolved Upstash deployment options contain the parameter values")]
    public void ThenTheResolvedUpstashDeploymentOptionsContainTheParameterValues()
    {
        UpstashRedisResolvedDeployment deployment = GetResolvedDeployment();

        Assert.Equal("aws", deployment.Options.Platform?.LiteralValue);
        Assert.Equal("eu-west-1", deployment.Options.PrimaryRegion?.LiteralValue);
        UpstashRedisProviderValue readRegion = Assert.Single(deployment.Options.ReadRegions ?? []);
        Assert.Equal("eu-west-2", readRegion.LiteralValue);
        Assert.Equal("payg", deployment.Options.Plan?.LiteralValue);
        Assert.Equal(360, deployment.Options.Budget?.LiteralValue);
        Assert.Equal(true, deployment.Options.Eviction?.LiteralValue);
        Assert.Equal(true, deployment.Options.Tls?.LiteralValue);
    }

    [Then("the resolved Upstash management API key is infrastructure-only")]
    public void ThenTheResolvedUpstashManagementApiKeyIsInfrastructureOnly()
    {
        UpstashRedisResolvedDeployment deployment = GetResolvedDeployment();
        string apiKey = deployment.ManagementCredentials.ApiKey;

        Assert.Equal("management-secret", apiKey);
        Assert.DoesNotContain(apiKey, deployment.DatabaseName, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, deployment.ManagementCredentials.AccountEmail, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, deployment.Options.ExplicitSettings, StringComparer.Ordinal);
        IResourceWithConnectionString redisConnection = Assert.IsAssignableFrom<IResourceWithConnectionString>(_context.RedisBuilder.Resource);

        Assert.DoesNotContain(apiKey, redisConnection.GetConnectionProperties().Select(property => property.Key), StringComparer.Ordinal);
    }

    [Then("the Upstash deployment resolution fails with {string}")]
    public void ThenTheUpstashDeploymentResolutionFailsWith(string exceptionTypeName)
    {
        Exception exception =
            _context.DeploymentResolutionException ?? throw new InvalidOperationException("The Upstash deployment resolution did not fail.");

        Assert.Equal(exceptionTypeName, exception.GetType().Name);
    }

    [Then("the Upstash deployment resolution failure message contains {string}")]
    public void ThenTheUpstashDeploymentResolutionFailureMessageContains(string expectedMessage)
    {
        Exception exception =
            _context.DeploymentResolutionException ?? throw new InvalidOperationException("The Upstash deployment resolution did not fail.");

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    private UpstashRedisResolvedDeployment GetResolvedDeployment()
    {
        return _context.ResolvedDeployment
            ?? throw new InvalidOperationException("The Upstash deployment inputs have not been resolved.");
    }
}
