using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Samples;

public static class UpstashRedisAppHostSnippets
{
    public static void ConfigureCreateOrAdopt(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("upstash-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);

        IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
            .PublishToUpstash(
                databaseName,
                accountEmail,
                apiKey,
                UpstashRedisOwnershipMode.CreateOrAdopt,
                options =>
                {
                    options.SetPlatform(UpstashRedisCloudPlatform.Aws);
                    options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
                    options.SetPlan(UpstashRedisPlan.PayAsYouGo);
                    options.Eviction = true;
                });

        builder.AddProject<Projects.Api>("api")
            .WithReference(cache);
    }

    public static void ConfigureCreateOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);

        builder.AddRedis("cache")
            .PublishToUpstash(
                "orders-cache",
                accountEmail,
                apiKey,
                UpstashRedisOwnershipMode.CreateOnly,
                options =>
                {
                    options.SetPlatform(UpstashRedisCloudPlatform.Aws);
                    options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
                    options.SetReadRegions(UpstashRedisRegion.AwsEuWest2);
                    options.SetPlan(UpstashRedisPlan.PayAsYouGo);
                    options.SetBudget(360);
                    options.Eviction = true;
                });
    }

    public static void ConfigureExistingOnly(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);

        builder.AddRedis("cache")
            .PublishToUpstash(
                "orders-cache",
                accountEmail,
                apiKey,
                UpstashRedisOwnershipMode.ExistingOnly);
    }

    public static void ConfigureParameterizedOptions(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("upstash-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);
        IResourceBuilder<ParameterResource> platform = builder.AddParameter("upstash-platform");
        IResourceBuilder<ParameterResource> primaryRegion = builder.AddParameter("upstash-primary-region");
        IResourceBuilder<ParameterResource> readRegion = builder.AddParameter("upstash-read-region");
        IResourceBuilder<ParameterResource> budget = builder.AddParameter("upstash-budget");

        builder.AddRedis("cache")
            .PublishToUpstash(
                UpstashRedisValue.FromParameter(databaseName),
                accountEmail,
                apiKey,
                UpstashRedisOwnershipMode.CreateOnly,
                options =>
                {
                    options.Platform = UpstashRedisValue.FromParameter(platform);
                    options.PrimaryRegion = UpstashRedisValue.FromParameter(primaryRegion);
                    options.ReadRegions = [UpstashRedisValue.FromParameter(readRegion)];
                    options.Plan = "payg";
                    options.Budget = UpstashRedisValue.FromParameter(budget);
                    options.Eviction = true;
                });
    }

    public static void ConfigureSupplementaryOutputConsumer(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> databaseName = builder.AddParameter("upstash-database-name");
        IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
        IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);

        IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
            .PublishToUpstash(
                databaseName,
                accountEmail,
                apiKey,
                UpstashRedisOwnershipMode.CreateOrAdopt);

        UpstashRedisOutputs outputs = cache.Resource.GetUpstashRedisOutputs();

        builder.AddContainer("redis-dashboard", "redis-dashboard")
            .WithEnvironment("UPSTASH_REDIS_ENDPOINT", outputs.Endpoint)
            .WithEnvironment("UPSTASH_REDIS_PORT", outputs.Port)
            .WithEnvironment("UPSTASH_REDIS_PASSWORD", outputs.Password)
            .WithEnvironment("UPSTASH_REDIS_TLS", outputs.Tls)
            .WithEnvironment("UPSTASH_REDIS_DATABASE_NAME", outputs.DatabaseName);
    }
}
