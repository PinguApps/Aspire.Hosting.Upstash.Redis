# PinguApps.Aspire.Hosting.Upstash.Redis

[![PinguApps.Aspire.Hosting.Upstash.Redis version](https://img.shields.io/nuget/v/PinguApps.Aspire.Hosting.Upstash.Redis?style=for-the-badge&label=PinguApps.Aspire.Hosting.Upstash.Redis)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/) [![PinguApps.Aspire.Hosting.Upstash.Redis downloads](https://img.shields.io/nuget/dt/PinguApps.Aspire.Hosting.Upstash.Redis?style=for-the-badge&label=downloads)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/)

`PinguApps.Aspire.Hosting.Upstash.Redis` lets an Aspire AppHost publish a normal Aspire Redis resource to Upstash Redis during `aspire deploy`.


`Aspire.Hosting.Upstash.Redis` lets you keep using a normal Aspire Redis resource in your AppHost and opt it into Upstash Redis only for deployment.
You still start with `builder.AddRedis("cache")`. You still reference that resource with normal Aspire APIs such as `.WithReference(cache)`. The only extra step is calling `.PublishToUpstash(...)` so `aspire deploy` can create or adopt a real Upstash Redis database and redirect the standard Redis connection details to it.
This package is intentionally deploy-focused. It does not replace Aspire's built-in `RedisResource`, it does not change local AppHost behaviour, and it does not push Upstash management credentials into application code.

## Contents
- [Package](#package)
- [Install](#install)
- [Quick Start](#quick-start)
- [How It Behaves](#how-it-behaves)
- [Required Inputs](#required-inputs)
- [Ownership Modes](#ownership-modes)
- [Configuration Model](#configuration-model)
- [Optional Settings Reference](#optional-settings-reference)
- [Supported Literal Values](#supported-literal-values)
- [Output Surfaces](#output-surfaces)
- [Full Examples](#full-examples)
- [Deployment Behaviour](#deployment-behaviour)
- [Security Boundaries](#security-boundaries)

## Package
- Package id: [PinguApps.Aspire.Hosting.Upstash.Redis](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Upstash.Redis/)
- Provider scope: Upstash Redis through the Upstash Developer API
This package is for the AppHost project. Install it where you call `builder.AddRedis(...)`.

## Install
Add the package to your AppHost:
```powershell
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```
Import the namespace in the AppHost:
```csharp
using Aspire.Hosting.Upstash.Redis;
```

## Quick Start
This is the recommended starting point for most applications:
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> upstashDatabaseName = builder.AddParameter("upstash-database-name");
IResourceBuilder<ParameterResource> upstashAccountEmail = builder.AddParameter("upstash-account-email");
IResourceBuilder<ParameterResource> upstashApiKey = builder.AddParameter("upstash-api-key", secret: true);

IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
    .PublishToUpstash(
        upstashDatabaseName,
        upstashAccountEmail,
        upstashApiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt,
        options =>
        {
            options.SetPlatform(UpstashRedisCloudPlatform.Aws);
            options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
            options.SetPlan(UpstashRedisPlan.PayAsYouGo);
            options.Eviction = true;
        });

builder.AddProject<Projects.Api>("api")
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
```

What this does:
- `builder.AddRedis("cache")` keeps the resource model standard Aspire Redis.
- `.PublishToUpstash(...)` adds deploy-time intent to that resource.
- `CreateOrAdopt` creates the named Upstash database if it does not exist, or adopts the compatible existing one if it does.
- `WithReference(cache)` continues to work through Aspire's normal Redis connection-string path.
- During deployment the final application-facing Redis connection details come from Upstash, not from a local container.
- During deployment the Redis resource itself is excluded from cloud compute publishing, so Azure Container Apps does not deploy a fallback `cache` container app for that resource.

## How It Behaves
The package has two very different modes depending on what you are doing:
- Local model construction and local runs:
`PublishToUpstash(...)` only records deploy-time metadata on the existing Redis resource. It does not call Upstash while the AppHost model is being built, and it does not stop local development from behaving like standard Aspire Redis.
- `aspire deploy`:
the deployment pipeline resolves your parameters, finds or creates the named Upstash database, validates immutable settings, reconciles explicitly configured mutable settings, and redirects the resource's standard Redis connection output to the final Upstash endpoint.
The important design point is that your consuming projects keep referencing a normal Redis resource throughout.

## Required Inputs
Every `PublishToUpstash(...)` call needs four pieces of information:
| Input | Purpose | Recommended source |
| --- | --- | --- |
| `databaseName` | The explicit remote Upstash database name and the stable identity used by repeated deployments. | Aspire parameter |
| `accountEmail` | Upstash management account email used only by the deployment pipeline. | Aspire parameter |
| `apiKey` | Upstash management API key used only by the deployment pipeline. | Secret Aspire parameter |
| `ownershipMode` | The deploy-time ownership contract for the named remote database. | Enum value |

Recommended pattern:
```csharp
IResourceBuilder<ParameterResource> upstashDatabaseName = builder.AddParameter("upstash-database-name");
IResourceBuilder<ParameterResource> upstashAccountEmail = builder.AddParameter("upstash-account-email");
IResourceBuilder<ParameterResource> upstashApiKey = builder.AddParameter("upstash-api-key", secret: true);
```
The database name is explicit by design. This package does not invent a remote name for you. Repeated deployments target the database identified by that configured name.

## Ownership Modes
Ownership is always explicit at the call site:
```csharp
builder.AddRedis("cache")
    .PublishToUpstash(
        upstashDatabaseName,
        upstashAccountEmail,
        upstashApiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt);
```

### CreateOrAdopt
Use this when the deployment is allowed to create the database if it is missing, but should also be able to attach to the compatible existing database with the same configured name.
- Missing database: creates it
- Existing compatible database with that name: adopts it
- Existing incompatible database with that name: fails clearly
This is the most flexible option and the best default for many teams.

### CreateOnly
Use this when the deployment is supposed to create the database rather than attach to an unrelated existing one.
- Missing database: creates it
- Existing uncached database (in deployment pipeline cache) with that name: fails
- Repeated deployments of the same verified identity: may reuse the cached verified identity and continue without recreating
That last point matters. `CreateOnly` is still strict about collisions, but it does not force an already-managed deployment to recreate the same database every time.

### ExistingOnly
Use this when infrastructure must already exist before the deployment runs.
- Missing database: fails
- Existing compatible database with that name: adopts it
- Existing incompatible database with that name: fails clearly
This is the mode to choose when database lifecycle is managed outside this AppHost.

### When Create Settings Are Required
If a deployment might need to create a database, configure at least:
- `Platform`
- `PrimaryRegion`
That applies to:
- `CreateOnly`
- `CreateOrAdopt` when the database may not already exist

## Configuration Model
The package gives you three overload shapes:
```csharp
PublishToUpstash(
    IResourceBuilder<ParameterResource> databaseName,
    IResourceBuilder<ParameterResource> accountEmail,
    IResourceBuilder<ParameterResource> apiKey,
    UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
    Action<UpstashRedisDeploymentOptions>? configure = null)
```
```csharp
PublishToUpstash(
    UpstashRedisValue databaseName,
    IResourceBuilder<ParameterResource> accountEmail,
    IResourceBuilder<ParameterResource> apiKey,
    UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
    Action<UpstashRedisDeploymentOptions>? configure = null)
```
```csharp
PublishToUpstash(
    UpstashRedisValue databaseName,
    UpstashRedisValue accountEmail,
    UpstashRedisValue apiKey,
    UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
    Action<UpstashRedisDeploymentOptions>? configure = null)
```

In practice:
- Use the all-parameter overload for the normal recommended setup.
- Use the `UpstashRedisValue databaseName` overload when the database name needs to come from either a literal or a parameter-backed helper.
- Use the full `UpstashRedisValue` overload only when your calling code already deals in `UpstashRedisValue` and you intentionally want to pass all required values that way.

### UpstashRedisValue
`UpstashRedisValue` is the package's literal-or-parameter wrapper for string deployment values.
You can create one from a literal:
```csharp
UpstashRedisValue databaseName = "orders-cache";
UpstashRedisValue plan = "payg";
```

Or from a parameter:
```csharp
IResourceBuilder<ParameterResource> databaseNameParameter = builder.AddParameter("upstash-database-name");
UpstashRedisValue databaseName = UpstashRedisValue.FromParameter(databaseNameParameter);
```

`UpstashRedisValue` is used by:
- the `databaseName` input
- advanced overloads for `accountEmail` and `apiKey`
- optional string settings such as platform, region, plan, and budget
Validation timing is deliberate:
- literal values are validated while the AppHost model is built
- parameter-backed values are validated after deployment-time parameter resolution
That means invalid literal configuration fails fast, while parameter-backed configuration still supports real deploy-time substitution.

### Typed Helpers Vs Literal Strings
For optional settings, prefer the typed helpers when the value is known in code:
```csharp
options.SetPlatform(UpstashRedisCloudPlatform.Aws);
options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
options.SetReadRegions(UpstashRedisRegion.AwsEuWest2);
options.SetPlan(UpstashRedisPlan.PayAsYouGo);
options.SetBudget(360);
options.Eviction = true;
```

Use `UpstashRedisValue` when the value must come from a parameter:
```csharp
IResourceBuilder<ParameterResource> platform = builder.AddParameter("upstash-platform");
IResourceBuilder<ParameterResource> primaryRegion = builder.AddParameter("upstash-primary-region");
IResourceBuilder<ParameterResource> readRegion = builder.AddParameter("upstash-read-region");
IResourceBuilder<ParameterResource> budget = builder.AddParameter("upstash-budget");

builder.AddRedis("cache")
    .PublishToUpstash(
        upstashDatabaseName,
        upstashAccountEmail,
        upstashApiKey,
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
```

## Optional Settings Reference
`UpstashRedisDeploymentOptions` only reconciles settings that you explicitly set. Leaving a property alone means "do not manage this setting."
| Setting | Type | Required when | Mutable on existing database | Notes |
| --- | --- | --- | --- | --- |
| `Platform` | `UpstashRedisValue?` or `SetPlatform(...)` | Required for create paths | No | Must resolve to `aws` or `gcp`. |
| `PrimaryRegion` | `UpstashRedisValue?` or `SetPrimaryRegion(...)` | Required for create paths | No | Must be a supported Upstash primary region and must match the platform. |
| `ReadRegions` | `IReadOnlyList<UpstashRedisValue>?` or `SetReadRegions(...)` | Optional | Yes | Each read region must support read replicas, must not duplicate, and must not equal the primary region. |
| `Plan` | `UpstashRedisValue?` or `SetPlan(...)` | Optional | Yes | Supports `free`, `payg`, and fixed plans. |
| `Budget` | `UpstashRedisValue?` or `SetBudget(int)` | Optional | Yes | Must be a positive integer. If `Plan` is also set, it must be `payg`. |
| `Eviction` | `bool?` | Optional | Yes | Explicitly manages provider eviction on or off. |
| `Tls` | `bool?` | Optional | No | Only `true` or unset is valid in v1. `false` is rejected. |

### What "Explicitly Set" Means
This package tracks intent, not just values.
- If you never touch `options.Plan`, the deployment leaves the current Upstash plan alone.
- If you set `options.Plan`, the deployment treats that as managed intent and reconciles it.
- If you set a mutable property to a new explicit value later, the next deploy tries to converge the remote database to that value.
That behaviour is central to repeated deployments. The package does not attempt to fully re-specify every provider setting unless you asked it to.

### Setting-By-Setting Detail
#### Platform
Use this to choose the cloud platform used for database creation.
```csharp
options.SetPlatform(UpstashRedisCloudPlatform.Aws);
```
or:
```csharp
options.Platform = "aws";
```

Key rules:
- required for create paths
- immutable after creation for v1
- must match the primary region platform

#### PrimaryRegion
Use this to choose the write region for database creation.
```csharp
options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
```
or:
```csharp
options.PrimaryRegion = "eu-west-1";
```

Key rules:
- required for create paths
- immutable after creation for v1
- must be supported by Upstash
- must match the chosen platform

#### ReadRegions
Use this to configure read replica regions when supported by the chosen platform and primary region.
```csharp
options.SetReadRegions(
    UpstashRedisRegion.AwsEuWest2,
    UpstashRedisRegion.AwsEuCentral1);
```
or:
```csharp
options.ReadRegions =
[
    "eu-west-2",
    "eu-central-1",
];
```

Key rules:
- mutable on existing databases
- each region must support read replicas
- duplicates are rejected
- a read region cannot equal the primary region
- a read region must be compatible with the chosen platform and primary region

#### Plan
Use this to control the Upstash plan.
```csharp
options.SetPlan(UpstashRedisPlan.PayAsYouGo);
```
or:
```csharp
options.Plan = "payg";
```

Key rules:
- mutable on existing databases
- fixed plan literals are supported
- if you also explicitly set `Budget`, the plan must be `payg`

#### Budget
Use this to configure the monthly budget value recognised by Upstash.
```csharp
options.SetBudget(360);
```
or:
```csharp
options.Budget = "360";
```

Key rules:
- mutable on existing databases
- must be a positive integer
- if `Plan` is also explicitly managed, it must resolve to `payg`

#### Eviction
Use this to explicitly manage whether eviction is enabled.
```csharp
options.Eviction = true;
```

Key rules:
- mutable on existing databases
- `true` enables eviction
- `false` disables eviction
- leaving it unset means "do not manage eviction"

#### Tls
Use this only if you want to state the requirement explicitly:
```csharp
options.Tls = true;
```

Key rules:
- Upstash Redis is treated as TLS-on for v1
- `false` is rejected during configuration
- the package does not attempt to mutate TLS off/on for an existing database

## Supported Literal Values
Use typed helpers where possible. If you need literal strings or parameter-backed values, these are the supported provider values.

### Platforms
| Typed helper | Literal value |
| --- | --- |
| `UpstashRedisCloudPlatform.Aws` | `aws` |
| `UpstashRedisCloudPlatform.Gcp` | `gcp` |

### Plans
| Typed helper | Literal value |
| --- | --- |
| `UpstashRedisPlan.Free` | `free` |
| `UpstashRedisPlan.PayAsYouGo` | `payg` |
| `UpstashRedisPlan.Fixed250Mb` | `fixed_250mb` |
| `UpstashRedisPlan.Fixed1Gb` | `fixed_1gb` |
| `UpstashRedisPlan.Fixed5Gb` | `fixed_5gb` |
| `UpstashRedisPlan.Fixed10Gb` | `fixed_10gb` |
| `UpstashRedisPlan.Fixed50Gb` | `fixed_50gb` |
| `UpstashRedisPlan.Fixed100Gb` | `fixed_100gb` |
| `UpstashRedisPlan.Fixed500Gb` | `fixed_500gb` |

### Supported Primary Regions
| Typed helper | Literal value |
| --- | --- |
| `UpstashRedisRegion.AwsUsEast1` | `us-east-1` |
| `UpstashRedisRegion.AwsUsEast2` | `us-east-2` |
| `UpstashRedisRegion.AwsUsWest1` | `us-west-1` |
| `UpstashRedisRegion.AwsUsWest2` | `us-west-2` |
| `UpstashRedisRegion.AwsCaCentral1` | `ca-central-1` |
| `UpstashRedisRegion.AwsEuCentral1` | `eu-central-1` |
| `UpstashRedisRegion.AwsEuWest1` | `eu-west-1` |
| `UpstashRedisRegion.AwsEuWest2` | `eu-west-2` |
| `UpstashRedisRegion.AwsSaEast1` | `sa-east-1` |
| `UpstashRedisRegion.AwsApSouth1` | `ap-south-1` |
| `UpstashRedisRegion.AwsApNortheast1` | `ap-northeast-1` |
| `UpstashRedisRegion.AwsApSoutheast1` | `ap-southeast-1` |
| `UpstashRedisRegion.AwsApSoutheast2` | `ap-southeast-2` |
| `UpstashRedisRegion.AwsAfSouth1` | `af-south-1` |
| `UpstashRedisRegion.GcpUsCentral1` | `us-central1` |
| `UpstashRedisRegion.GcpUsEast4` | `us-east4` |
| `UpstashRedisRegion.GcpEuropeWest1` | `europe-west1` |
| `UpstashRedisRegion.GcpAsiaNortheast1` | `asia-northeast1` |

### Supported Read Regions
Read regions are more restrictive than primary regions. The following typed values support read replicas:
- `UpstashRedisRegion.AwsUsEast1`
- `UpstashRedisRegion.AwsUsEast2`
- `UpstashRedisRegion.AwsUsWest1`
- `UpstashRedisRegion.AwsUsWest2`
- `UpstashRedisRegion.AwsCaCentral1`
- `UpstashRedisRegion.AwsEuCentral1`
- `UpstashRedisRegion.AwsEuWest1`
- `UpstashRedisRegion.AwsEuWest2`
- `UpstashRedisRegion.AwsSaEast1`
- `UpstashRedisRegion.AwsApSouth1`
- `UpstashRedisRegion.AwsApNortheast1`
- `UpstashRedisRegion.AwsApSoutheast1`
- `UpstashRedisRegion.AwsApSoutheast2`

`AwsAfSouth1` and the current GCP regions are valid primary regions but not valid read regions for this package's v1 contract.

## Output Surfaces
There are two output surfaces and they serve different purposes.

### 1. Standard Aspire Redis Connection Output
This is the main path used by normal consumers:
```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(cache);
```

After successful deployment, this package redirects the existing Aspire Redis connection output to the deployed Upstash database using normal Redis connection details in Aspire Redis format.
Your app continues to consume Redis the same way it would for a standard Aspire Redis resource.

### 2. Supplementary Upstash Redis Outputs
If you need more than the connection string, call `GetUpstashRedisOutputs()` on the `RedisResource`:
```csharp
UpstashRedisOutputs outputs = cache.Resource.GetUpstashRedisOutputs();

var endpoint = outputs.Endpoint;
var port = outputs.Port;
var password = outputs.Password;
var tls = outputs.Tls;
var databaseName = outputs.DatabaseName;
```

Those stable output names are:
- `Endpoint`
- `Port`
- `Password`
- `Tls`
- `DatabaseName`
Each output reference exposes:
- `Name`
- `ValueExpression`
- `Secret`
Only `Password` is classified as secret.
Example consumer:
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> upstashDatabaseName = builder.AddParameter("upstash-database-name");
IResourceBuilder<ParameterResource> upstashAccountEmail = builder.AddParameter("upstash-account-email");
IResourceBuilder<ParameterResource> upstashApiKey = builder.AddParameter("upstash-api-key", secret: true);

IResourceBuilder<RedisResource> cache = builder.AddRedis("cache")
    .PublishToUpstash(
        upstashDatabaseName,
        upstashAccountEmail,
        upstashApiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt);

UpstashRedisOutputs outputs = cache.Resource.GetUpstashRedisOutputs();

builder.AddContainer("redis-dashboard", "redis-dashboard")
    .WithEnvironment("UPSTASH_REDIS_ENDPOINT", outputs.Endpoint)
    .WithEnvironment("UPSTASH_REDIS_PORT", outputs.Port)
    .WithEnvironment("UPSTASH_REDIS_PASSWORD", outputs.Password)
    .WithEnvironment("UPSTASH_REDIS_TLS", outputs.Tls)
    .WithEnvironment("UPSTASH_REDIS_DATABASE_NAME", outputs.DatabaseName);
```

What these outputs do not expose:
- Upstash management API key
- provider database id
- customer id
- REST tokens
- broader account or billing metadata

## Full Examples
### Canonical CreateOrAdopt
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
```

Choose this when you want the package to create the database if it does not exist, but adopt it if it already does.

### CreateOnly With Explicit Create Settings
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

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
```

Choose this when the deployment is expected to create the database rather than adopt an unrelated pre-existing one.

### ExistingOnly
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> accountEmail = builder.AddParameter("upstash-account-email");
IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("upstash-api-key", secret: true);

builder.AddRedis("cache")
    .PublishToUpstash(
        "orders-cache",
        accountEmail,
        apiKey,
        UpstashRedisOwnershipMode.ExistingOnly);
```

Choose this when the database already exists and must not be created by this deployment.

### Parameter-Backed Optional Settings
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

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
```

Choose this when optional provider settings must come from deploy-time parameters rather than from code literals or enums.

### Supplementary Output Consumer
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
        UpstashRedisOwnershipMode.CreateOrAdopt);

UpstashRedisOutputs outputs = cache.Resource.GetUpstashRedisOutputs();

builder.AddContainer("redis-dashboard", "redis-dashboard")
    .WithEnvironment("UPSTASH_REDIS_ENDPOINT", outputs.Endpoint)
    .WithEnvironment("UPSTASH_REDIS_PORT", outputs.Port)
    .WithEnvironment("UPSTASH_REDIS_PASSWORD", outputs.Password)
    .WithEnvironment("UPSTASH_REDIS_TLS", outputs.Tls)
    .WithEnvironment("UPSTASH_REDIS_DATABASE_NAME", outputs.DatabaseName);
```

Choose this when a consumer needs stable individual output references instead of only the standard Redis connection string.

## Deployment Behaviour
### Repeated Deployments
Repeated deployments keep targeting the intended remote database by explicit configured database name.
The deployment pipeline may also persist a cached provider database id, but that cached id is only reused after it still proves to be the same configured database name and a fresh name lookup stays consistent. That safety check prevents an old cached id from silently adopting the wrong database.

### What Gets Reconciled
Mutable settings are reconciled only when you explicitly configure them:
- `ReadRegions`
- `Plan`
- `Budget`
- `Eviction`

Immutable or fail-fast settings are treated differently:
- `databaseName` is the stable identity
- `Platform` is create-time or fail-fast
- `PrimaryRegion` is create-time or fail-fast
- `Tls = false` is rejected

If an explicitly requested setting cannot be safely reconciled, deployment fails clearly. The package does not silently switch to a different database and it does not overwrite unspecified settings just to make the deployment fit.

### Connection Output Generation
After create/adopt/reconcile succeeds, the package populates:
- the standard Redis connection string output used by Aspire references
- connection properties for host, port, password, and URI
- supplementary Upstash Redis outputs from `GetUpstashRedisOutputs()`
Provider endpoint values are validated before output generation. URI-like endpoint strings or single-label slugs are rejected rather than emitted as broken Redis connection details.

### Diagnostics
Deployment progress is reported through explicit phases for:
- configuration resolution
- database lookup
- immutable drift validation
- create
- mutable reconciliation
- output retrieval
Diagnostics are secret-safe. They redact management credentials, Redis passwords and tokens, and full Redis connection strings.

## Security Boundaries
This package draws a hard line between infrastructure management credentials and application Redis credentials.
Infrastructure-only values:
- Upstash account email
- Upstash management API key
App-facing values:
- Redis connection string
- endpoint
- port
- password
- TLS flag
- database name
That means:
- the package uses the management API key only to talk to Upstash during deployment
- application references do not receive the management API key
- supplementary outputs do not expose the management API key
- only `Password` is marked secret among the supplementary outputs
Recommended practice:
- keep `apiKey` in a secret Aspire parameter
- treat `accountEmail` as deployment-only configuration
- keep application code on the normal Redis reference path unless it truly needs the supplementary outputs
