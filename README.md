# PinguApps.Aspire.Hosting.Upstash.Redis

[![PinguApps.Aspire.Hosting.Upstash.Redis version](https://img.shields.io/nuget/v/PinguApps.Aspire.Hosting.Upstash.Redis?style=for-the-badge&label=PinguApps.Aspire.Hosting.Upstash.Redis)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/) [![PinguApps.Aspire.Hosting.Upstash.Redis downloads](https://img.shields.io/nuget/dt/PinguApps.Aspire.Hosting.Upstash.Redis?style=for-the-badge&label=downloads)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/)

`PinguApps.Aspire.Hosting.Upstash.Redis` lets an Aspire AppHost publish a normal Aspire Redis resource to Upstash Redis during `aspire deploy`.

- Package id: [`PinguApps.Aspire.Hosting.Upstash.Redis`](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/)
- Distribution: NuGet for both C# and TypeScript AppHosts
- Tested Aspire baseline: `13.4.6`
- Provider scope: Upstash Redis through the Upstash Developer API
- Local behaviour: standard Aspire Redis
- Deploy behaviour: opt-in Upstash create/adopt/reconcile flow

## Install

C# AppHost:

```powershell
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```

TypeScript AppHost:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.6",
    "PinguApps.Aspire.Hosting.Upstash.Redis": "<package version>"
  }
}
```

Then run:

```powershell
aspire restore --non-interactive
```

No npm package is required for the integration itself. TypeScript AppHosts consume the same NuGet package through Aspire's generated guest-language module flow.

## Minimal .NET Example

Maintained sample source: [`samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`](samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs)

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

## Minimal TypeScript AppHost Example

Maintained demo source: [`samples/TypeScriptAppHost/apphost.mts`](samples/TypeScriptAppHost/apphost.mts)

```ts
import {
  createBuilder,
  upstashRedisCloudPlatform,
  upstashRedisOwnershipMode,
  upstashRedisPlan,
  upstashRedisRegion,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const databaseName = await builder.addParameter("upstash-database-name");
const accountEmail = await builder.addParameter("upstash-account-email");
const apiKey = await builder.addParameter("upstash-api-key", { secret: true });

let cache = await builder.addRedis("cache");
cache = await cache.publishToUpstash(databaseName, accountEmail, apiKey, {
  ownershipMode: upstashRedisOwnershipMode.createOrAdopt,
  platform: upstashRedisCloudPlatform.aws,
  primaryRegion: upstashRedisRegion.awsEuWest1,
  plan: upstashRedisPlan.payAsYouGo,
  eviction: true,
});

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);

const app = await builder.build();
await app.run();
```

## Deploy Inputs

| Input | Purpose |
| --- | --- |
| `upstash-database-name` | Explicit remote Upstash database name and stable deployment identity. |
| `upstash-account-email` | Infrastructure-only Upstash account email. |
| `upstash-api-key` | Infrastructure-only Upstash Management API key. Mark it secret. |

The management API key is never exposed as an application-facing Redis output.

For non-interactive deploys, provide real values as Aspire parameter environment variables:

```powershell
$env:Parameters__upstash_database_name = "upstash-ts-test"
$env:Parameters__upstash_account_email = $env:UPSTASH_EMAIL
$env:Parameters__upstash_api_key = $env:UPSTASH_API_KEY
```

## Behaviour Summary

`builder.AddRedis("cache")` remains the resource of record. `PublishToUpstash(...)` or `publishToUpstash(...)` attaches deploy-time intent to that normal Redis resource.

Local runs keep using standard Aspire Redis behaviour and do not call Upstash while the AppHost model is built. During `aspire deploy`, the package resolves parameters, creates or adopts the named Upstash database, reconciles explicitly configured mutable settings, fails on unsafe drift, and redirects app-facing Redis connection details to Upstash.

## Docs

- [Overview and product contract](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/overview.md)
- [Install and package consumption](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/install.md)
- [C# AppHost usage](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/getting-started-dotnet.md)
- [TypeScript AppHost usage](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/getting-started-typescript.md)
- [Configuration and ownership modes](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/configuration.md)
- [Deployment behaviour](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/deployment-behaviour.md)
- [Outputs and security boundaries](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/outputs-and-security.md)
- [Samples and demos](https://github.com/PinguApps/Aspire.Hosting.Upstash.Redis/blob/main/docs/samples-and-demos.md)
