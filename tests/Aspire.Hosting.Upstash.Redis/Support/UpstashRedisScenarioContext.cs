#pragma warning disable IDE0032

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

public sealed class UpstashRedisScenarioContext
{
    private IDistributedApplicationBuilder? _appBuilder;
    private IResourceBuilder<RedisResource>? _redisBuilder;
    private IResourceBuilder<ContainerResource>? _containerBuilder;
    private IResourceBuilder<ParameterResource>? _accountEmail;
    private IResourceBuilder<ParameterResource>? _apiKey;

    public UpstashRedisDeploymentOptions? CapturedDeploymentOptions
    {
        get;
        private set;
    }

    public List<UpstashRedisValue> ConfiguredReadRegions
    {
        get;
    } = ["eu-west-2"];

    internal FakeUpstashProvider FakeProvider
    {
        get;
    } = new();

    internal FakeUpstashRedisDatabase? LastProviderDatabase
    {
        get;
        set;
    }

    internal Exception? LastCleanupException
    {
        get;
        set;
    }

    internal List<string> LiveCleanupLog
    {
        get;
    } = [];

    internal LiveUpstashTestSession LiveUpstash
    {
        get;
    } = new();

    internal IResourceBuilder<RedisResource> RedisBuilder =>
        _redisBuilder ?? throw new InvalidOperationException("The Redis resource has not been created.");

    internal IResourceBuilder<ContainerResource> ContainerBuilder =>
        _containerBuilder ?? throw new InvalidOperationException("The consuming container has not been created.");

    private IDistributedApplicationBuilder AppBuilder =>
        _appBuilder ?? throw new InvalidOperationException("The application builder has not been created.");

    public void AddRedis(string resourceName)
    {
        _appBuilder = DistributedApplication.CreateBuilder();
        _redisBuilder = _appBuilder.AddRedis(resourceName);
    }

    public void MarkRedisForUpstash(string databaseName)
    {
        _accountEmail ??= AppBuilder.AddParameter("upstash-account-email");
        _apiKey ??= AppBuilder.AddParameter("upstash-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToUpstash(
            databaseName,
            _accountEmail,
            _apiKey,
            configure: options =>
            {
                CapturedDeploymentOptions = options;
                options.PrimaryRegion = "eu-west-1";
                options.ReadRegions = ConfiguredReadRegions;
                options.Tls = true;
            });
    }

    public void MarkRedisForUpstash(string databaseName, UpstashRedisOwnershipMode ownershipMode)
    {
        _accountEmail ??= AppBuilder.AddParameter("upstash-account-email");
        _apiKey ??= AppBuilder.AddParameter("upstash-api-key", secret: true);

        _redisBuilder = RedisBuilder.PublishToUpstash(
            databaseName,
            _accountEmail,
            _apiKey,
            ownershipMode);
    }

    public void MarkRedisForUpstashWithParameterBasedInputs()
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

    public Exception? ConfigurationException
    {
        get;
        private set;
    }

    public void TryMarkRedisForBlankUpstashDatabaseName()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                " ",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true)));
    }

    public void TryMarkRedisForUpstashWithMissingApiKey()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                null!));
    }

    public void TryMarkRedisForUpstashWithUnsupportedOwnershipMode()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true),
                (UpstashRedisOwnershipMode)999));
    }

    public void TryMarkRedisForUpstashWithDisabledTls()
    {
        ConfigurationException = Record.Exception(() =>
            RedisBuilder.PublishToUpstash(
                "orders-cache",
                AppBuilder.AddParameter("upstash-account-email"),
                AppBuilder.AddParameter("upstash-api-key", secret: true),
                configure: options => options.Tls = false));
    }

    public void AddConsumingContainerReference()
    {
        _containerBuilder = AppBuilder.AddContainer("worker", "redis-reference-test")
            .WithReference(RedisBuilder);
    }
}
