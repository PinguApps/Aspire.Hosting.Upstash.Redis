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
        UpstashRedisDeploymentAnnotation annotation = AspireModelInspector.GetUpstashAnnotation(_context.RedisBuilder.Resource);

        Assert.Equal(databaseName, annotation.DatabaseName);
        Assert.Equal(UpstashRedisOwnershipMode.CreateOrAdopt, annotation.OwnershipMode);
        Assert.Equal("upstash-account-email", annotation.AccountEmail.Name);
        Assert.Equal("upstash-api-key", annotation.ApiKey.Name);
        Assert.Equal("eu-west-1", annotation.Options.PrimaryRegion);
        Assert.Equal(["eu-west-2"], annotation.Options.ReadRegions);
        Assert.Equal(true, annotation.Options.Tls);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), annotation.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions), annotation.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), annotation.Options.ExplicitSettings);
    }

    [Then("mutating captured callback options cannot mutate deployment metadata")]
    public void ThenMutatingCapturedCallbackOptionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentOptions capturedOptions =
            _context.CapturedDeploymentOptions ?? throw new InvalidOperationException("The deployment options were not captured.");

        capturedOptions.PrimaryRegion = "us-east-1";
        capturedOptions.Plan = "payg";
        capturedOptions.Tls = false;

        UpstashRedisDeploymentAnnotation annotation = AspireModelInspector.GetUpstashAnnotation(_context.RedisBuilder.Resource);

        Assert.Equal("eu-west-1", annotation.Options.PrimaryRegion);
        Assert.Null(annotation.Options.Plan);
        Assert.Equal(true, annotation.Options.Tls);
        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), annotation.Options.ExplicitSettings);
    }

    [Then("the explicit setting snapshot cannot mutate deployment metadata")]
    public void ThenTheExplicitSettingSnapshotCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentAnnotation annotation = AspireModelInspector.GetUpstashAnnotation(_context.RedisBuilder.Resource);

        if (annotation.Options.ExplicitSettings is ISet<string> exposedSettings)
        {
            exposedSettings.Add(nameof(UpstashRedisDeploymentOptions.Plan));
        }

        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), annotation.Options.ExplicitSettings);
    }

    [Then("mutating the configured read regions cannot mutate deployment metadata")]
    public void ThenMutatingTheConfiguredReadRegionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentAnnotation annotation = AspireModelInspector.GetUpstashAnnotation(_context.RedisBuilder.Resource);

        _context.ConfiguredReadRegions.Add("us-east-1");

        Assert.Equal(["eu-west-2"], annotation.Options.ReadRegions);
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
