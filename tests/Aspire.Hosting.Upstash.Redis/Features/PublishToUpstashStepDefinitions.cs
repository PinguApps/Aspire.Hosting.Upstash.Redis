#pragma warning disable IDE0032

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Features;

[Binding]
public sealed class PublishToUpstashStepDefinitions
{
    private IDistributedApplicationBuilder? _appBuilder;
    private IResourceBuilder<RedisResource>? _redisBuilder;
    private IResourceBuilder<ContainerResource>? _containerBuilder;
    private IResourceBuilder<ParameterResource>? _accountEmail;
    private IResourceBuilder<ParameterResource>? _apiKey;

    [Given("a standard Aspire Redis resource named {string}")]
    public void GivenAStandardAspireRedisResourceNamed(string resourceName)
    {
        _appBuilder = DistributedApplication.CreateBuilder();
        _redisBuilder = _appBuilder.AddRedis(resourceName);
    }

    [When("the Redis resource is marked for Upstash database {string}")]
    public void WhenTheRedisResourceIsMarkedForUpstashDatabase(string databaseName)
    {
        _accountEmail ??= AppBuilder.AddParameter("upstash-account-email");
        _apiKey ??= AppBuilder.AddParameter("upstash-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToUpstash(
            databaseName,
            _accountEmail,
            _apiKey,
            configure: options =>
            {
                options.PrimaryRegion = "eu-west-1";
                options.Tls = true;
            });
    }

    [When("a consuming container references the Redis resource")]
    public void WhenAConsumingContainerReferencesTheRedisResource()
    {
        _containerBuilder = AppBuilder.AddContainer("worker", "redis-reference-test")
            .WithReference(RedisBuilder);
    }

    [Then("the resource remains a standard Aspire Redis resource")]
    public void ThenTheResourceRemainsAStandardAspireRedisResource()
    {
        Assert.IsType<RedisResource>(RedisBuilder.Resource);
    }

    [Then("the resource has Upstash deployment metadata for database {string}")]
    public void ThenTheResourceHasUpstashDeploymentMetadataForDatabase(string databaseName)
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        Assert.Equal(databaseName, annotation.DatabaseName);
        Assert.Equal(UpstashRedisOwnershipMode.CreateOrAdopt, annotation.OwnershipMode);
        Assert.Equal("upstash-account-email", annotation.AccountEmail.Name);
        Assert.Equal("upstash-api-key", annotation.ApiKey.Name);
        Assert.Equal("eu-west-1", annotation.Options.PrimaryRegion);
        Assert.Equal(true, annotation.Options.Tls);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), annotation.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), annotation.Options.ExplicitSettings);
    }

    [Then("the explicit setting snapshot cannot mutate deployment metadata")]
    public void ThenTheExplicitSettingSnapshotCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        if (annotation.Options.ExplicitSettings is ISet<string> exposedSettings)
        {
            exposedSettings.Add(nameof(UpstashRedisDeploymentOptions.Plan));
        }

        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), annotation.Options.ExplicitSettings);
    }

    [Then("the resource keeps the standard Redis connection properties")]
    public void ThenTheResourceKeepsTheStandardRedisConnectionProperties()
    {
        IResourceWithConnectionString connectionResource = Assert.IsAssignableFrom<IResourceWithConnectionString>(RedisBuilder.Resource);
        string[] propertyNames = [.. connectionResource.GetConnectionProperties().Select(property => property.Key)];

        Assert.Contains("Host", propertyNames);
        Assert.Contains("Port", propertyNames);
        Assert.Contains("Password", propertyNames);
        Assert.Contains("Uri", propertyNames);
    }

    [Then("the Redis reference chain is configured for the consuming container")]
    public void ThenTheRedisReferenceChainIsConfiguredForTheConsumingContainer()
    {
        IResourceBuilder<ContainerResource> containerBuilder =
            _containerBuilder ?? throw new InvalidOperationException("The consuming container has not been created.");

        Assert.Contains(
            containerBuilder.Resource.Annotations,
            annotation => annotation is EnvironmentCallbackAnnotation);
    }

    [Then("the resource has one Upstash deployment pipeline step")]
    public void ThenTheResourceHasOneUpstashDeploymentPipelineStep()
    {
        Assert.Single(
            RedisBuilder.Resource.Annotations,
            annotation => annotation.GetType().FullName == "Aspire.Hosting.Pipelines.PipelineStepAnnotation");
    }

    private IDistributedApplicationBuilder AppBuilder =>
        _appBuilder ?? throw new InvalidOperationException("The application builder has not been created.");

    private IResourceBuilder<RedisResource> RedisBuilder =>
        _redisBuilder ?? throw new InvalidOperationException("The Redis resource has not been created.");
}
