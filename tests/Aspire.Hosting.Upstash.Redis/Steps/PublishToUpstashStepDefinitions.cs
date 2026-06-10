using Aspire.Hosting;
using Aspire.Hosting.Upstash.Redis;
using PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;
using Reqnroll;
using System.Reflection;
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

    [When("the Redis resource is marked for Upstash through the TypeScript bridge with DTO options")]
    public void WhenTheRedisResourceIsMarkedForUpstashThroughTheTypeScriptBridgeWithDtoOptions()
    {
        _context.MarkRedisForUpstashThroughTypeScriptBridgeWithDtoOptions();
    }

    [When("the Redis resource is marked for Upstash through the TypeScript bridge with disabled TLS")]
    public void WhenTheRedisResourceIsMarkedForUpstashThroughTheTypeScriptBridgeWithDisabledTls()
    {
        _context.TryMarkRedisForUpstashThroughTypeScriptBridgeWithDisabledTls();
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

    [Then("the resource is excluded from publish")]
    public void ThenTheResourceIsExcludedFromPublish()
    {
        Assert.True(AspireModelInspector.IsExcludedFromPublish(_context.RedisBuilder.Resource));
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

    [Then("the TypeScript DTO deployment metadata maps to provider payload values")]
    public void ThenTheTypeScriptDtoDeploymentMetadataMapsToProviderPayloadValues()
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
        Assert.Equal(true, providerOptions.Tls?.LiteralValue);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Platform), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Plan), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Budget), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Eviction), providerOptions.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), providerOptions.ExplicitSettings);
    }

    [Then("the TypeScript output bridge returns the supplementary Upstash Redis outputs")]
    public void ThenTheTypeScriptOutputBridgeReturnsTheSupplementaryUpstashRedisOutputs()
    {
        _context.GetOutputsThroughTypeScriptBridge();

        UpstashRedisOutputs outputs = _context.LastOutputs ?? throw new InvalidOperationException("The outputs were not captured.");

        Assert.Same(_context.RedisBuilder.Resource.GetUpstashRedisOutputs(), outputs);
        Assert.NotNull(outputs.Endpoint);
        Assert.NotNull(outputs.Port);
        Assert.NotNull(outputs.Password);
        Assert.NotNull(outputs.Tls);
        Assert.NotNull(outputs.DatabaseName);
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

    [Then("the TypeScript export metadata matches the approved Upstash Redis contract")]
    public void ThenTheTypeScriptExportMetadataMatchesTheApprovedUpstashRedisContract()
    {
        MethodInfo publishMethod = typeof(UpstashRedisBuilderExtensions).GetMethod(nameof(UpstashRedisBuilderExtensions.PublishToUpstashForTypeScript))
            ?? throw new InvalidOperationException("The TypeScript publish bridge was not found.");
        Assert.Equal("PinguApps.Aspire.Hosting.Upstash.Redis", publishMethod.DeclaringType?.Assembly.GetName().Name);
        AspireExportAttribute publishExport = Assert.Single(publishMethod.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.upstash.redis.publishToUpstash", publishExport.Id);
        Assert.Equal("publishToUpstash", publishExport.MethodName);

        MethodInfo outputsMethod = typeof(UpstashRedisResourceExtensions).GetMethod(nameof(UpstashRedisResourceExtensions.GetUpstashRedisOutputsForTypeScript))
            ?? throw new InvalidOperationException("The TypeScript outputs bridge was not found.");
        AspireExportAttribute outputsExport = Assert.Single(outputsMethod.GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.upstash.redis.getUpstashRedisOutputs", outputsExport.Id);
        Assert.Equal("getUpstashRedisOutputs", outputsExport.MethodName);

        Assert.NotNull(typeof(UpstashRedisDeploymentOptionsDto).GetCustomAttribute<AspireDtoAttribute>());
        AssertOutputExportMetadata();
        AssertValueCatalogMetadata();
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

    private static void AssertOutputExportMetadata()
    {
        AspireExportAttribute outputsExport = Assert.Single(typeof(UpstashRedisOutputs).GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.upstash.redis.outputs", outputsExport.Id);
        Assert.True(outputsExport.ExposeProperties);
        Assert.False(outputsExport.ExposeMethods);
        Assert.NotNull(typeof(UpstashRedisOutputs).GetProperty(nameof(UpstashRedisOutputs.Properties))?.GetCustomAttribute<AspireExportIgnoreAttribute>());
        Assert.NotNull(typeof(UpstashRedisOutputs).GetMethod(nameof(UpstashRedisOutputs.IsSecret))?.GetCustomAttribute<AspireExportIgnoreAttribute>());

        AspireExportAttribute referenceExport = Assert.Single(typeof(UpstashRedisOutputReference).GetCustomAttributes<AspireExportAttribute>());
        Assert.Equal("pinguapps.upstash.redis.outputReference", referenceExport.Id);
        Assert.False(referenceExport.ExposeProperties);
        Assert.False(referenceExport.ExposeMethods);
    }

    private static void AssertValueCatalogMetadata()
    {
        AssertValueCatalog(
            UpstashRedisOwnershipMode.CreateOrAdopt,
            "upstashRedisOwnershipMode",
            "createOrAdopt");
        AssertValueCatalog(UpstashRedisOwnershipMode.CreateOnly, "upstashRedisOwnershipMode", "createOnly");
        AssertValueCatalog(UpstashRedisOwnershipMode.ExistingOnly, "upstashRedisOwnershipMode", "existingOnly");
        AssertValueCatalog(UpstashRedisCloudPlatform.Aws, "upstashRedisCloudPlatform", "aws");
        AssertValueCatalog(UpstashRedisCloudPlatform.Gcp, "upstashRedisCloudPlatform", "gcp");
        AssertValueCatalog(UpstashRedisPlan.PayAsYouGo, "upstashRedisPlan", "payAsYouGo");
        AssertValueCatalog(UpstashRedisPlan.Fixed250Mb, "upstashRedisPlan", "fixed250Mb");
        AssertValueCatalog(UpstashRedisRegion.AwsEuWest2, "upstashRedisRegion", "awsEuWest2");
        AssertValueCatalog(UpstashRedisRegion.GcpEuropeWest1, "upstashRedisRegion", "gcpEuropeWest1");
    }

    private static void AssertValueCatalog<TEnum>(TEnum value, string catalogName, string name)
        where TEnum : struct, Enum
    {
        FieldInfo field = typeof(TEnum).GetField(value.ToString())
            ?? throw new InvalidOperationException($"The enum value '{value}' was not found.");
        AspireValueAttribute attribute = Assert.Single(field.GetCustomAttributes<AspireValueAttribute>());

        Assert.Equal(catalogName, attribute.CatalogName);
        Assert.Equal(name, attribute.Name);
    }
}
