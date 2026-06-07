# Aspire.Hosting.Upstash.Redis

`Aspire.Hosting.Upstash.Redis` lets an Aspire AppHost publish a normal Aspire Redis resource to Upstash Redis during `aspire deploy`.

The package is intentionally small: you still start with Aspire's built-in `builder.AddRedis("cache")`, opt that resource into Upstash publishing with `.PublishToUpstash(...)`, and keep using normal Aspire Redis references for your application.

## Supported Versions

- Package id: `PinguApps.Aspire.Hosting.Upstash.Redis`
- Target framework: `.NET 10`
- Aspire packages: `Aspire.Hosting` and `Aspire.Hosting.Redis` `13.4.2`
- Provider scope: Upstash Redis through the native Upstash Developer API

The Upstash Management API requires a native Upstash account email and Management API key. Third-party marketplace Upstash accounts are not supported by that API.

## Install

Add the package to your AppHost project:

```bash
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```

Then import the extension namespace in the AppHost:

```csharp
using Aspire.Hosting.Upstash.Redis;
```

## Canonical Usage

Use Aspire parameters for the remote database name and management credentials. The database name is explicit because it is the stable remote identity used by repeated deployments.

```csharp
using Aspire.Hosting.Upstash.Redis;

var builder = DistributedApplication.CreateBuilder(args);

var upstashDatabaseName = builder.AddParameter("upstash-database-name");
var upstashAccountEmail = builder.AddParameter("upstash-account-email");
var upstashApiKey = builder.AddParameter("upstash-api-key", secret: true);

var cache = builder.AddRedis("cache")
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
    .WithReference(cache);

builder.Build().Run();
```

The compile-validated source for the AppHost snippets lives in [`samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`](samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs). The test project links that file into compilation and exercises each snippet against a fresh Aspire builder so README examples and future docs can use it as the source of truth.

During local development this remains standard Aspire Redis behavior. `.PublishToUpstash(...)` attaches deployment metadata and a deploy pipeline step, but it does not call Upstash during model construction or local runs and it does not replace the `RedisResource`.

During deployment the package resolves the parameters, finds or creates the configured Upstash Redis database according to the ownership mode, reconciles only explicitly configured mutable settings, and redirects the standard Redis connection string output to the deployed Upstash endpoint. Your app can keep using the normal `WithReference(cache)` path.

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

- `CreateOrAdopt`: create the database when the configured name is missing, or adopt the existing compatible database with that name.
- `CreateOnly`: create the database when the configured name is missing. If an unmanaged database with that name already exists, deployment fails. Repeated deploys may reuse this deployment's verified cached identity.
- `ExistingOnly`: adopt an existing compatible database with the configured name. If it does not exist, deployment fails.

Create paths require enough placement information for Upstash to create a database. In practice, configure at least `Platform` and `PrimaryRegion` when `CreateOrAdopt` or `CreateOnly` may create a new database.

## Parameters and Values

The recommended pattern is:

```csharp
var upstashDatabaseName = builder.AddParameter("upstash-database-name");
var upstashAccountEmail = builder.AddParameter("upstash-account-email");
var upstashApiKey = builder.AddParameter("upstash-api-key", secret: true);
```

`upstash-account-email` and `upstash-api-key` are infrastructure-only management credentials. They are used by the deployment pipeline to call Upstash and are not exposed to application Redis references or outputs.

Required values and optional string settings use `UpstashRedisValue`, which can hold either:

- a literal string, including implicit string conversion
- an Aspire `ParameterResource`, usually through `UpstashRedisValue.FromParameter(...)`

Typed helpers are preferred when the value is known in code:

```csharp
options.SetPlatform(UpstashRedisCloudPlatform.Aws);
options.SetPrimaryRegion(UpstashRedisRegion.AwsEuWest1);
options.SetReadRegions(UpstashRedisRegion.AwsEuWest2);
options.SetPlan(UpstashRedisPlan.PayAsYouGo);
options.SetBudget(360);
options.Eviction = true;
```

Use `UpstashRedisValue.FromParameter(...)` when an optional setting must be supplied at deploy time:

```csharp
var primaryRegion = builder.AddParameter("upstash-primary-region");
var platform = builder.AddParameter("upstash-platform");
var readRegion = builder.AddParameter("upstash-read-region");
var budget = builder.AddParameter("upstash-budget");

builder.AddRedis("cache")
    .PublishToUpstash(
        upstashDatabaseName,
        upstashAccountEmail,
        upstashApiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt,
        options =>
        {
            options.Platform = UpstashRedisValue.FromParameter(platform);
            options.PrimaryRegion = UpstashRedisValue.FromParameter(primaryRegion);
            options.ReadRegions = [UpstashRedisValue.FromParameter(readRegion)];
            options.Plan = "payg";
            options.Budget = UpstashRedisValue.FromParameter(budget);
        });
```

Literal platform, region, plan, and budget values are validated while the AppHost model is built. Parameter-backed values are validated after deployment parameter resolution.

## Optional Settings

The deployment options support:

- `Platform`: `Aws` or `Gcp`. Required for create paths.
- `PrimaryRegion`: the primary Upstash Redis region. Required for create paths.
- `ReadRegions`: optional read replica regions.
- `Plan`: `Free`, `PayAsYouGo`, or one of the fixed-size plans.
- `Budget`: a positive monthly budget value. If `Plan` is also configured, it must be `PayAsYouGo`.
- `Eviction`: whether eviction is enabled.
- `Tls`: required-on/read-only for v1. Leave it unset or set it to `true`; `false` is rejected.

Only read regions, plan, budget, and eviction are mutable on existing databases. Database name identity, platform, primary region, and TLS disabled state are treated as create-time or fail-fast safety checks.

## App-Facing Outputs

There are two output surfaces.

The standard Redis connection string output is owned by Aspire Redis and is the path used by normal `WithReference(cache)` consumers. After a successful Upstash deployment, this package redirects that connection string to the deployed Upstash database using Aspire Redis's normal `host:port,password=password,ssl=true` shape.

Advanced consumers can also access supplementary Upstash Redis outputs:

```csharp
var outputs = cache.Resource.GetUpstashRedisOutputs();

var endpoint = outputs.Endpoint;
var port = outputs.Port;
var password = outputs.Password;
var tls = outputs.Tls;
var databaseName = outputs.DatabaseName;
```

The stable supplementary output names are:

- `Endpoint`
- `Port`
- `Password`
- `Tls`
- `DatabaseName`

Each supplementary output exposes `Name`, `ValueExpression`, and `Secret` metadata. `Password` is the only supplementary output classified as secret.

These outputs come from the credential-bearing Upstash database detail response after create/adopt/reconcile succeeds. They do not expose the Upstash Management API key, REST tokens, provider id, customer id, or billing/security metadata.

## Deployment Behavior

Repeated deploys target the same intended remote database by explicit database name. The deployment pipeline can persist and reuse the provider id as cached identity state, but the cached id is accepted only after it still verifies against the configured name and a fresh duplicate-checked lookup.

The package reconciles only settings you explicitly configure. Unspecified provider settings are left unchanged. Mutable settings are reconciled in a deterministic order: read regions, plan, budget, then eviction.

If an explicitly requested setting cannot be safely reconciled, deployment fails clearly instead of replacing or taking over a different database.

The package never auto-deletes Upstash Redis databases in v1. It also does not automatically rename databases, reset passwords, move teams, configure backups, configure ACLs, configure private networking, or broaden into non-Redis Upstash products.

## Safety Notes

- Use a native Upstash account email and Management API key.
- Keep the Management API key in a secret Aspire parameter.
- Do not pass the Management API key to application code; the package keeps it in infrastructure-only deployment state.
- The consuming application receives Redis connection details, not Upstash management credentials.
- Provider endpoints must be host names. URI-like endpoint values or single-label slugs are rejected before connection-string output is generated.
- Deployment diagnostics redact the Management API key, Redis passwords/tokens, and full Redis connection strings.

## Validation

The README snippets are backed by the compile-validated sample source in `samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`, the docs sample scenarios in `tests/Aspire.Hosting.Upstash.Redis/Features/DocsSamples/`, and the Reqnroll-backed API-shape coverage in `tests/Aspire.Hosting.Upstash.Redis/`. The full validation command for this repository is:

```bash
dotnet build ./Aspire.Hosting.Upstash.Redis.slnx
dotnet test ./Aspire.Hosting.Upstash.Redis.slnx
```
