using Aspire.Hosting.Upstash.Redis;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class PublishToUpstashStepDefinitions
{
    private readonly UpstashRedisScenarioContext _context;

    public PublishToUpstashStepDefinitions(UpstashRedisScenarioContext context)
    {
        _context = context;
    }

    [Given("a standard Aspire Redis resource named {string}")]
    public void GivenAStandardAspireRedisResourceNamed(string resourceName)
    {
        _context.AddRedis(resourceName);
    }

    [When("the Redis resource is marked for Upstash database {string}")]
    public void WhenTheRedisResourceIsMarkedForUpstashDatabase(string databaseName)
    {
        _context.MarkRedisForUpstash(databaseName);
    }

    [When("the Redis resource is marked for Upstash database {string} with ownership mode {string}")]
    public void WhenTheRedisResourceIsMarkedForUpstashDatabaseWithOwnershipMode(string databaseName, string ownershipMode)
    {
        _context.MarkRedisForUpstash(databaseName, Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode));
    }

    [When("the Redis resource is marked for Upstash with parameter-based inputs")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithParameterBasedInputs()
    {
        _context.MarkRedisForUpstashWithParameterBasedInputs();
    }

    [When("the Redis resource is marked for Upstash with an explicitly unset primary region")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAnExplicitlyUnsetPrimaryRegion()
    {
        _context.MarkRedisForUpstashWithExplicitNullPrimaryRegion();
    }

    [When("the Redis resource is marked for a blank Upstash database name")]
    public void WhenTheRedisResourceIsMarkedForABlankUpstashDatabaseName()
    {
        _context.TryMarkRedisForBlankUpstashDatabaseName();
    }

    [When("the Redis resource is marked for Upstash with a missing API key value")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAMissingApiKeyValue()
    {
        _context.TryMarkRedisForUpstashWithMissingApiKey();
    }

    [When("the Redis resource is marked for Upstash with an unsupported ownership mode")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAnUnsupportedOwnershipMode()
    {
        _context.TryMarkRedisForUpstashWithUnsupportedOwnershipMode();
    }

    [When("the Redis resource is marked for Upstash with disabled TLS")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithDisabledTls()
    {
        _context.TryMarkRedisForUpstashWithDisabledTls();
    }

    [When("a consuming container references the Redis resource")]
    public void WhenAConsumingContainerReferencesTheRedisResource()
    {
        _context.AddConsumingContainerReference();
    }

    [Then("the resource remains a standard Aspire Redis resource")]
    public void ThenTheResourceRemainsAStandardAspireRedisResource()
    {
        AspireModelAssertions.AssertStandardRedisResource(_context.RedisBuilder.Resource);
    }

    [Then("the resource has Upstash deployment metadata for database {string}")]
    public void ThenTheResourceHasUpstashDeploymentMetadataForDatabase(string databaseName)
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Equal(databaseName, state.DatabaseName.LiteralValue);
        Assert.Equal(UpstashRedisOwnershipMode.CreateOrAdopt, state.OwnershipMode);
        Assert.Equal("upstash-account-email", state.AccountEmail.Parameter?.Name);
        Assert.Equal("upstash-api-key", state.ApiKey.Parameter?.Name);
        Assert.Equal("eu-west-1", state.Options.PrimaryRegion?.LiteralValue);
        Assert.Equal(["eu-west-2"], state.Options.ReadRegions?.Select(region => region.LiteralValue));
        Assert.Equal(true, state.Options.Tls);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), state.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions), state.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), state.Options.ExplicitSettings);
    }

    [Then("the resource has Upstash ownership mode {string}")]
    public void ThenTheResourceHasUpstashOwnershipMode(string ownershipMode)
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Equal(Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode), state.OwnershipMode);
    }

    [Then("the resource stores parameter references for the required Upstash inputs")]
    public void ThenTheResourceStoresParameterReferencesForTheRequiredUpstashInputs()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Equal("upstash-database-name", state.DatabaseName.Parameter?.Name);
        Assert.Equal("upstash-account-email", state.AccountEmail.Parameter?.Name);
        Assert.Equal("upstash-api-key", state.ApiKey.Parameter?.Name);
        Assert.Null(state.ApiKey.LiteralValue);
    }

    [Then("the resource stores parameter references for optional Upstash inputs")]
    public void ThenTheResourceStoresParameterReferencesForOptionalUpstashInputs()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Equal("upstash-primary-region", state.Options.PrimaryRegion?.Parameter?.Name);
        UpstashRedisValue readRegion = Assert.Single(state.Options.ReadRegions ?? []);
        Assert.Equal("upstash-read-region", readRegion.Parameter?.Name);
        Assert.Equal("payg", state.Options.Plan?.LiteralValue);
    }

    [Then("the Upstash state distinguishes the explicitly unset primary region from an unspecified plan")]
    public void ThenTheUpstashStateDistinguishesTheExplicitlyUnsetPrimaryRegionFromAnUnspecifiedPlan()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Null(state.Options.PrimaryRegion);
        Assert.Null(state.Options.Plan);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), state.Options.ExplicitSettings);
        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("the Upstash configuration fails with {string}")]
    public void ThenTheUpstashConfigurationFailsWith(string exceptionTypeName)
    {
        Exception configurationException =
            _context.ConfigurationException ?? throw new InvalidOperationException("The Upstash configuration did not fail.");

        Assert.Equal(exceptionTypeName, configurationException.GetType().Name);
    }

    [Then("mutating captured callback options cannot mutate deployment metadata")]
    public void ThenMutatingCapturedCallbackOptionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentOptions capturedOptions =
            _context.CapturedDeploymentOptions ?? throw new InvalidOperationException("The deployment options were not captured.");

        capturedOptions.PrimaryRegion = "us-east-1";
        capturedOptions.Plan = "payg";
        capturedOptions.Tls = false;

        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        Assert.Equal("eu-west-1", state.Options.PrimaryRegion?.LiteralValue);
        Assert.Null(state.Options.Plan);
        Assert.Equal(true, state.Options.Tls);
        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("the explicit setting snapshot cannot mutate deployment metadata")]
    public void ThenTheExplicitSettingSnapshotCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        if (state.Options.ExplicitSettings is ISet<string> exposedSettings)
        {
            exposedSettings.Add(nameof(UpstashRedisDeploymentOptions.Plan));
        }

        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), state.Options.ExplicitSettings);
    }

    [Then("mutating the configured read regions cannot mutate deployment metadata")]
    public void ThenMutatingTheConfiguredReadRegionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        _context.ConfiguredReadRegions.Add("us-east-1");

        Assert.Equal(["eu-west-2"], state.Options.ReadRegions?.Select(region => region.LiteralValue));
    }

    [Then("the resource keeps the standard Redis connection properties")]
    public void ThenTheResourceKeepsTheStandardRedisConnectionProperties()
    {
        AspireModelAssertions.AssertRedisConnectionProperties(_context.RedisBuilder.Resource);
    }

    [Then("the Redis reference chain is configured for the consuming container")]
    public void ThenTheRedisReferenceChainIsConfiguredForTheConsumingContainer()
    {
        AspireModelAssertions.AssertContainerHasEnvironmentCallback(_context.ContainerBuilder.Resource);
    }

    [Then("the resource has one Upstash deployment pipeline step")]
    public void ThenTheResourceHasOneUpstashDeploymentPipelineStep()
    {
        Assert.Equal(1, AspireModelInspector.GetPipelineStepCount(_context.RedisBuilder.Resource));
    }
}
