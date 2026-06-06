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
    private UpstashRedisDeploymentOptions? _capturedDeploymentOptions;
    private Exception? _configurationException;
    private readonly List<UpstashRedisValue> _configuredReadRegions = ["eu-west-2"];

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
                _capturedDeploymentOptions = options;
                options.PrimaryRegion = "eu-west-1";
                options.ReadRegions = _configuredReadRegions;
                options.Tls = true;
            });
    }

    [When("the Redis resource is marked for Upstash database {string} with ownership mode {string}")]
    public void WhenTheRedisResourceIsMarkedForUpstashDatabaseWithOwnershipMode(string databaseName, string ownershipMode)
    {
        _accountEmail ??= AppBuilder.AddParameter("upstash-account-email");
        _apiKey ??= AppBuilder.AddParameter("upstash-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToUpstash(
            databaseName,
            _accountEmail,
            _apiKey,
            Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode));
    }

    [When("the Redis resource is marked for Upstash with parameter-based inputs")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithParameterBasedInputs()
    {
        IResourceBuilder<ParameterResource> databaseName = AppBuilder.AddParameter("upstash-database-name");
        _accountEmail = AppBuilder.AddParameter("upstash-account-email");
        _apiKey = AppBuilder.AddParameter("upstash-api-key", secret: true);
        IResourceBuilder<ParameterResource> primaryRegion = AppBuilder.AddParameter("upstash-primary-region");
        IResourceBuilder<ParameterResource> readRegion = AppBuilder.AddParameter("upstash-read-region");

        _redisBuilder = RedisBuilder.PublishToUpstash(
            databaseName,
            _accountEmail,
            _apiKey,
            UpstashRedisOwnershipMode.ExistingOnly,
            options =>
            {
                options.PrimaryRegion = UpstashRedisValue.FromParameter(primaryRegion);
                options.ReadRegions = [UpstashRedisValue.FromParameter(readRegion)];
                options.Plan = "payg";
            });
    }

    [When("the Redis resource is marked for a blank Upstash database name")]
    public void WhenTheRedisResourceIsMarkedForABlankUpstashDatabaseName()
    {
        _configurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                " ",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true)));
    }

    [When("the Redis resource is marked for Upstash with a missing API key value")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAMissingApiKeyValue()
    {
        _configurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                null!));
    }

    [When("the Redis resource is marked for Upstash with an unsupported ownership mode")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithAnUnsupportedOwnershipMode()
    {
        _configurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true),
                (UpstashRedisOwnershipMode)999));
    }

    [When("the Redis resource is marked for Upstash with disabled TLS")]
    public void WhenTheRedisResourceIsMarkedForUpstashWithDisabledTls()
    {
        _configurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true),
                configure: options => options.Tls = false));
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

        Assert.Equal(databaseName, annotation.DatabaseName.LiteralValue);
        Assert.Equal(UpstashRedisOwnershipMode.CreateOrAdopt, annotation.OwnershipMode);
        Assert.Equal("upstash-account-email", annotation.AccountEmail.Parameter?.Name);
        Assert.Equal("upstash-api-key", annotation.ApiKey.Parameter?.Name);
        Assert.Equal("eu-west-1", annotation.Options.PrimaryRegion?.LiteralValue);
        Assert.Equal(["eu-west-2"], annotation.Options.ReadRegions?.Select(region => region.LiteralValue));
        Assert.Equal(true, annotation.Options.Tls);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion), annotation.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions), annotation.Options.ExplicitSettings);
        Assert.Contains(nameof(UpstashRedisDeploymentOptions.Tls), annotation.Options.ExplicitSettings);
    }

    [Then("the resource has Upstash ownership mode {string}")]
    public void ThenTheResourceHasUpstashOwnershipMode(string ownershipMode)
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        Assert.Equal(Enum.Parse<UpstashRedisOwnershipMode>(ownershipMode), annotation.OwnershipMode);
    }

    [Then("the resource stores parameter references for the required Upstash inputs")]
    public void ThenTheResourceStoresParameterReferencesForTheRequiredUpstashInputs()
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        Assert.Equal("upstash-database-name", annotation.DatabaseName.Parameter?.Name);
        Assert.Equal("upstash-account-email", annotation.AccountEmail.Parameter?.Name);
        Assert.Equal("upstash-api-key", annotation.ApiKey.Parameter?.Name);
        Assert.Null(annotation.ApiKey.LiteralValue);
    }

    [Then("the resource stores parameter references for optional Upstash inputs")]
    public void ThenTheResourceStoresParameterReferencesForOptionalUpstashInputs()
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        Assert.Equal("upstash-primary-region", annotation.Options.PrimaryRegion?.Parameter?.Name);
        UpstashRedisValue readRegion = Assert.Single(annotation.Options.ReadRegions ?? []);
        Assert.Equal("upstash-read-region", readRegion.Parameter?.Name);
        Assert.Equal("payg", annotation.Options.Plan?.LiteralValue);
    }

    [Then("the Upstash configuration fails with {string}")]
    public void ThenTheUpstashConfigurationFailsWith(string exceptionTypeName)
    {
        Exception configurationException =
            _configurationException ?? throw new InvalidOperationException("The Upstash configuration did not fail.");

        Assert.Equal(exceptionTypeName, configurationException.GetType().Name);
    }

    [Then("mutating captured callback options cannot mutate deployment metadata")]
    public void ThenMutatingCapturedCallbackOptionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentOptions capturedOptions =
            _capturedDeploymentOptions ?? throw new InvalidOperationException("The deployment options were not captured.");

        capturedOptions.PrimaryRegion = "us-east-1";
        capturedOptions.Plan = "payg";
        capturedOptions.Tls = false;

        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        Assert.Equal("eu-west-1", annotation.Options.PrimaryRegion?.LiteralValue);
        Assert.Null(annotation.Options.Plan);
        Assert.Equal(true, annotation.Options.Tls);
        Assert.DoesNotContain(nameof(UpstashRedisDeploymentOptions.Plan), annotation.Options.ExplicitSettings);
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

    [Then("mutating the configured read regions cannot mutate deployment metadata")]
    public void ThenMutatingTheConfiguredReadRegionsCannotMutateDeploymentMetadata()
    {
        UpstashRedisDeploymentAnnotation annotation = Assert.Single(RedisBuilder.Resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());

        _configuredReadRegions.Add("us-east-1");

        Assert.Equal(["eu-west-2"], annotation.Options.ReadRegions?.Select(region => region.LiteralValue));
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
