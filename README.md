# Aspire.Hosting.Upstash.Redis

This package is an Aspire hosting integration for publishing a standard Aspire Redis resource to Upstash Redis during deployment.

Current state: the Aspire integration shape, public API shape, internal resource state model, Upstash Redis management client layer, Upstash Redis option/domain model, and deploy-time parameter resolution are implemented. The package extends the built-in Redis resource returned by `builder.AddRedis("cache")`, attaches internal Upstash deployment state with `.PublishToUpstash(...)`, and resolves the configured deployment values from the Aspire deploy pipeline step. Later implementation tasks still own remote lookup, creation, reconciliation, and deployed connection outputs.

```csharp
var databaseName = builder.AddParameter("upstash-database-name");
var accountEmail = builder.AddParameter("upstash-account-email");
var apiKey = builder.AddParameter("upstash-api-key", secret: true);

var cache = builder.AddRedis("cache")
    .PublishToUpstash(
        databaseName,
        accountEmail,
        apiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt);

builder.AddProject<Projects.Api>("api")
    .WithReference(cache);
```

Ownership intent is explicit at the call site:

```csharp
builder.AddRedis("cache")
    .PublishToUpstash(
        "orders-cache",
        accountEmail,
        apiKey,
        UpstashRedisOwnershipMode.CreateOnly);

builder.AddRedis("cache")
    .PublishToUpstash(
        "orders-cache",
        accountEmail,
        apiKey,
        UpstashRedisOwnershipMode.ExistingOnly);

builder.AddRedis("cache")
    .PublishToUpstash(
        "orders-cache",
        accountEmail,
        apiKey,
        UpstashRedisOwnershipMode.CreateOrAdopt);
```

Required values and optional string settings are represented as `UpstashRedisValue`, which can hold either a literal string or an Aspire `ParameterResource`. Literal strings convert implicitly; parameterized optional settings use `UpstashRedisValue.FromParameter(...)`. Internally, the Redis resource annotation stores a single deployment state snapshot containing required values, ownership mode, infrastructure-only management credential sources, optional settings, and explicit-setting metadata for later reconcile tasks.

Use Aspire parameters for secrets and deploy-specific values:

```csharp
var databaseName = builder.AddParameter("upstash-database-name");
var accountEmail = builder.AddParameter("upstash-account-email");
var apiKey = builder.AddParameter("upstash-api-key", secret: true);
```

Aspire can resolve those parameters from its normal parameter sources during deployment, including cached prompted values and configuration-backed parameters such as `builder.AddParameterFromConfiguration(...)`. The Upstash Management API key is resolved only by the internal deploy pipeline path and is kept in infrastructure-only management credentials; it is not added to Redis connection properties or application references.

Optional provider-domain settings can be configured with typed helpers when values are known at AppHost configuration time:

```csharp
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

Literal option values are validated during AppHost model construction and mapped internally to Upstash API payload values. Parameter-backed option values keep their source and are validated after deploy-time parameter resolution. Missing required deploy-time parameters fail with an explicit message naming the missing parameter. TLS remains required-on/read-only for v1: `Tls = false` is rejected and deployment logic must not try to disable TLS.

Local Aspire behavior is preserved: `.PublishToUpstash(...)` does not replace the Redis resource, does not call Upstash during model construction or local runs, and does not prevent normal `WithReference(cache)` usage.

The test suite is Reqnroll-first. Feature files live under behavior-focused folders in `tests/Aspire.Hosting.Upstash.Redis/Features/`, shared scenario support lives under `tests/Aspire.Hosting.Upstash.Redis/Support/`, and the scenario map for future tasks is documented in `tests/Aspire.Hosting.Upstash.Redis/README.md`.

Contributor validation note: opt-in live Upstash validation can now use the environment variables `UPSTASH_EMAIL` and `UPSTASH_API_KEY`. Any live test must stay explicitly gated and must always tear down or restore any remote state it touches so the Upstash account is left unchanged after the run.

The Upstash management capability matrix is documented in [`plans/0.2-confirm-upstash-management-capability-matrix.md`](plans/0.2-confirm-upstash-management-capability-matrix.md). Key v1 decisions from that investigation:

- Management authentication uses separate native Upstash account email and Management API key values.
- Third-party marketplace Upstash accounts are not supported by the Developer API and should fail fast with a tailored error.
- Remote lookup uses list-by-account plus explicit database-name matching, with provider id preferred after discovery when safe state is available.
- Create supports database name, platform, primary region, read regions, plan, budget, and eviction.
- Reconcile supports read regions, plan, budget, and eviction only.
- TLS is treated as required-on/read-only for v1, not as a mutable setting.
- Password reset, rename, delete, team move, backups, autoscaling, prod pack, ACL, and private networking are intentionally out of scope for v1.
- App-facing outputs come from credential-bearing database detail responses and must never expose the Upstash Management API key.

The internal management client in `src/Aspire.Hosting.Upstash.Redis/Management/` implements only the v1 Redis endpoints from that matrix:

- `GET /redis/databases`
- `GET /redis/database/{id}`
- `POST /redis/database`
- `POST /redis/update-regions/{id}`
- `POST /redis/{id}/change-plan`
- `PATCH /redis/update-budget/{id}`
- `POST /redis/enable-eviction/{id}`
- `POST /redis/disable-eviction/{id}`

Provider failures are surfaced as typed `UpstashRedisProviderException` values with a failure kind such as validation, authentication, authorization, not found, rate limited, transient, provider contract, or unexpected. The client preserves provider error text where useful and redacts the Management API key. Credential-bearing database detail fetches require `password`; if Upstash omits it, the client fails with a provider-contract error rather than guessing or rotating credentials.
