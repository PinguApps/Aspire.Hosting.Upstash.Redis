# C# AppHost Usage

Install the package in the AppHost:

```powershell
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```

Then start with standard Aspire Redis and add `PublishToUpstash`.

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
```

The maintained compile-validated C# sample is [`samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`](../samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs).

## Deploy

Run deploys from the AppHost directory. For non-interactive deploys, provide the required values as Aspire parameter environment variables:

```powershell
$env:Parameters__upstash_database_name = "orders-cache"
$env:Parameters__upstash_account_email = $env:UPSTASH_EMAIL
$env:Parameters__upstash_api_key = $env:UPSTASH_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

Repeated deploys should target the same configured database name.

## Overload Shapes

C# supports parameter-backed and literal-or-parameter value overloads:

```csharp
PublishToUpstash(databaseName, accountEmail, apiKey, ownershipMode, options => { });
PublishToUpstash("orders-cache", accountEmail, apiKey, ownershipMode, options => { });
PublishToUpstash(UpstashRedisValue.FromParameter(databaseName), accountEmail, apiKey, ownershipMode, options => { });
```

Use Aspire parameters for management credentials. Literal database names are supported in C#, but parameters are usually easier to promote across environments.

## Local Run

Local runs behave like standard Aspire Redis. `PublishToUpstash` records deploy-time intent and does not call Upstash during model construction.
