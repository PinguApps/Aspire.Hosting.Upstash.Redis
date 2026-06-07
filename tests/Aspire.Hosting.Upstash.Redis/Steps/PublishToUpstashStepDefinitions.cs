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

    [When("the Redis resource is marked for Upstash with literal management credentials")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithLiteralManagementCredentials()
    {
        _context.MarkRedisForUpstashWithLiteralManagementCredentials();
    }

    [When("the Redis resource is marked for Upstash through the {string} overload")]
    public void WhenTheRedisResourceIsMarkedForUpstashThroughTheOverload(string overload)
    {
        _context.MarkRedisForUpstashThroughOverload(overload);
    }

    [When("the Redis resource is marked for Upstash with parameter-based inputs")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithParameterBasedInputs()
    {
        _context.MarkRedisForUpstashWithParameterBasedInputs();
    }

    [When("the Redis resource is marked for Upstash with typed domain options")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithTypedDomainOptions()
    {
        _context.MarkRedisForUpstashWithTypedDomainOptions();
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

    [When("the Redis resource is marked for Upstash with unsupported platform")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithUnsupportedPlatform()
    {
        _context.TryMarkRedisForUpstashWithUnsupportedPlatform();
    }

    [When("the Redis resource is marked for Upstash with mismatched platform and primary region")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithMismatchedPlatformAndPrimaryRegion()
    {
        _context.TryMarkRedisForUpstashWithMismatchedPlatformAndPrimaryRegion();
    }

    [When("the Redis resource is marked for Upstash with a fixed plan budget")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAFixedPlanBudget()
    {
        _context.TryMarkRedisForUpstashWithBudgetOnFixedPlan();
    }

    [Given("a consuming container references the Redis resource")]
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

    [Then("the provider domain maps the typed options to Upstash payload values")]
    public void ThenTheProviderDomainMapsTheTypedOptionsToUpstashPayloadValues()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);
        UpstashRedisProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.Equal(UpstashRedisOwnershipMode.CreateOnly, state.OwnershipMode);
        Assert.Equal("aws", providerOptions.Platform?.LiteralValue);
        Assert.Equal("eu-west-1", providerOptions.PrimaryRegion?.LiteralValue);
        UpstashRedisProviderValue readRegion = Assert.Single(providerOptions.ReadRegions ?? []);
        Assert.Equal("eu-west-2", readRegion.LiteralValue);
        Assert.Equal("payg", providerOptions.Plan?.LiteralValue);
        Assert.Equal(360, providerOptions.Budget?.LiteralValue);
        Assert.Equal(true, providerOptions.Eviction?.LiteralValue);
        Assert.Equal("true", providerOptions.Eviction?.Source.LiteralValue);
        Assert.Equal(true, providerOptions.Tls?.LiteralValue);
        Assert.Equal("true", providerOptions.Tls?.Source.LiteralValue);
    }

    [Then("the provider domain preserves explicit settings for reconcile")]
    public void ThenTheProviderDomainPreservesExplicitSettingsForReconcile()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);
        UpstashRedisProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Platform), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Plan), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Budget), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Eviction), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), providerOptions.ExplicitSettings);
    }

    [Then("the provider domain preserves parameter-backed option sources")]
    public void ThenTheProviderDomainPreservesParameterBackedOptionSources()
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);
        UpstashRedisProviderDeploymentOptions providerOptions = state.Options.ToProviderOptions();

        Assert.True(providerOptions.PrimaryRegion?.IsParameter);
        Assert.Null(providerOptions.PrimaryRegion?.LiteralValue);
        UpstashRedisProviderValue readRegion = Assert.Single(providerOptions.ReadRegions ?? []);
        Assert.True(readRegion.IsParameter);
        Assert.Null(readRegion.LiteralValue);
        Assert.Equal("payg", providerOptions.Plan?.LiteralValue);
    }

    [Then("the Upstash configuration fails with {string}")]
    public void ThenTheUpstashConfigurationFailsWith(string exceptionTypeName)
    {
        Exception configurationException =
            _context.ConfigurationException ?? throw new InvalidOperationException("The Upstash configuration did not fail.");

        Assert.Equal(exceptionTypeName, configurationException.GetType().Name);
    }

    [Then("the Upstash configuration failure message contains {string}")]
    public void ThenTheUpstashConfigurationFailureMessageContains(string expectedMessage)
    {
        Exception configurationException =
            _context.ConfigurationException ?? throw new InvalidOperationException("The Upstash configuration did not fail.");

        Assert.Contains(expectedMessage, configurationException.Message, StringComparison.Ordinal);
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

    [Then("the resource has no Upstash deployment metadata")]
    public void ThenTheResourceHasNoUpstashDeploymentMetadata()
    {
        Assert.False(AspireModelInspector.HasUpstashState(_context.RedisBuilder.Resource));
    }

    [Then("the resource has no Upstash deployment pipeline step")]
    public void ThenTheResourceHasNoUpstashDeploymentPipelineStep()
    {
        Assert.Equal(0, AspireModelInspector.GetPipelineStepCount(_context.RedisBuilder.Resource));
    }

    [Then("the resource has no supplementary Upstash Redis outputs")]
    public void ThenTheResourceHasNoSupplementaryUpstashRedisOutputs()
    {
        Assert.DoesNotContain(
            _context.RedisBuilder.Resource.Annotations,
            annotation => annotation is UpstashRedisOutputsAnnotation);
    }

    [Then("the fake Upstash provider has no recorded interactions")]
    public void ThenTheFakeUpstashProviderHasNoRecordedInteractions()
    {
        Assert.Empty(_context.FakeProvider.Interactions);
    }

    [Then("the app-facing Redis outputs and references do not contain {string}")]
    public async Task ThenTheAppFacingRedisOutputsAndReferencesDoNotContain(string unexpectedValue)
    {
        AspireModelAssertions.AssertRedisConnectionPropertiesDoNotContain(_context.RedisBuilder.Resource, unexpectedValue);
        await AspireModelAssertions.AssertContainerEnvironmentDoesNotContainAsync(_context.ContainerBuilder.Resource, unexpectedValue);

        UpstashRedisOutputs outputs = _context.RedisBuilder.Resource.GetUpstashRedisOutputs();

        foreach (UpstashRedisOutputReference output in outputs.Properties)
        {
            Assert.DoesNotContain(unexpectedValue, output.Name, StringComparison.Ordinal);
            Assert.DoesNotContain(unexpectedValue, output.ValueExpression, StringComparison.Ordinal);
        }
    }

    [Then("the Upstash deployment metadata matches the {string} overload")]
    public void ThenTheUpstashDeploymentMetadataMatchesTheOverload(string overload)
    {
        UpstashRedisDeploymentState state = AspireModelInspector.GetUpstashState(_context.RedisBuilder.Resource);

        switch (overload)
        {
            case "literal database and parameter credentials":
                Assert.Equal("orders-cache", state.DatabaseName.LiteralValue);
                Assert.Equal("upstash-account-email", state.AccountEmail.Parameter?.Name);
                Assert.Equal("upstash-api-key", state.ApiKey.Parameter?.Name);
                break;

            case "parameter database and parameter credentials":
                Assert.Equal("upstash-database-name", state.DatabaseName.Parameter?.Name);
                Assert.Equal("upstash-account-email", state.AccountEmail.Parameter?.Name);
                Assert.Equal("upstash-api-key", state.ApiKey.Parameter?.Name);
                break;

            case "literal deployment values":
                Assert.Equal("orders-cache", state.DatabaseName.LiteralValue);
                Assert.Equal("owner@example.com", state.AccountEmail.LiteralValue);
                Assert.Equal("management-secret", state.ApiKey.LiteralValue);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(overload), overload, "Unknown PublishToUpstash overload.");
        }
    }

    [Then("the fluent API returns the same Redis resource builder")]
    public void ThenTheFluentApiReturnsTheSameRedisResourceBuilder()
    {
        Assert.True(_context.FluentApiReturnedSameBuilder);
    }
}
