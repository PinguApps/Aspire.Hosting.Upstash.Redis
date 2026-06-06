# Aspire.Hosting.Upstash.Redis

This package is an Aspire hosting integration for publishing a standard Aspire Redis resource to Upstash Redis during deployment.

Current state: the Aspire integration shape, public API shape, and internal resource state model are confirmed. The package extends the built-in Redis resource returned by `builder.AddRedis("cache")` and attaches internal Upstash deployment state with `.PublishToUpstash(...)`. The deploy pipeline step is intentionally a no-op until the later implementation tasks add Upstash API calls, reconciliation, and deployed connection outputs.

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
