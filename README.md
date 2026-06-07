# Aspire.Hosting.Upstash.Redis

This package is an Aspire hosting integration for publishing a standard Aspire Redis resource to Upstash Redis during deployment.

Current state: the Aspire integration shape, public API shape, internal resource state model, Upstash Redis management client layer, Upstash Redis option/domain model, deploy-time parameter resolution, ownership-resolution decision engine, remote identity resolver, create flow, mutable-setting reconciler, immutable drift detector, primary Redis connection-string output, supplementary app-facing outputs, deploy diagnostics/progress reporting, and end-to-end ownership-mode deployment hardening are implemented. The package extends the built-in Redis resource returned by `builder.AddRedis("cache")`, attaches internal Upstash deployment state with `.PublishToUpstash(...)`, resolves the configured deployment values from the Aspire deploy pipeline step, verifies and persists remote identity state, resolves ownership by explicit database name, can create a missing Upstash Redis database when ownership selects the create path, can fail fast when an adopted database conflicts with create-time-only or unsafe settings, can reconcile supported mutable settings on an adopted database, redirects the deployed Redis connection string to the final Upstash database details, can populate supplementary outputs from the deployed database detail response, and reports a secret-safe deployment timeline. Later implementation tasks still own release docs and samples.

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

The internal ownership resolver looks up the configured remote database name through the management client before choosing a path:

- `CreateOnly` selects create when no database exists, adopts only this deployment's verified cached remote identity on repeated deploys, and fails if an unmanaged database with the name already exists.
- `ExistingOnly` adopts an existing compatible database and fails if the name is missing.
- `CreateOrAdopt` adopts an existing compatible database or selects create when the name is missing.

When an existing database conflicts with immutable/read-only settings the detector can verify from provider details, deployment fails before later create or reconcile steps. The enforced v1 immutable/unsafe set is database name identity, platform where the provider primary region reveals it, explicit primary region, and TLS disabled state. The mutable reconciler then enforces only explicit desired read regions, plan, budget, and eviction settings.

When ownership selects create, the deploy flow builds the Upstash `POST /redis/database` payload from the resolved v1 surface: database name, platform, primary region, read regions, plan, budget, eviction, and TLS. `platform` and `primary_region` must be explicitly configured before creating a new remote database because Upstash requires them and the package does not invent provider defaults. TLS is sent as `true`; `false` remains rejected. After create, deployment polls `GET /redis/database/{id}` until the provider reports `state == active` and an empty `modifying_state`. The ready detail response must include endpoint, port, TLS state, and password for later app-facing outputs; if any required connection field is missing or unusable, deployment fails with a provider-contract error and never calls `reset-password`.

The deploy pipeline reports a structured, secret-safe timeline through the Aspire pipeline logger: resolving configuration, locating the configured database, validating immutable drift, creating the database when needed, reconciling mutable settings, and retrieving Redis connection outputs. Normal messages include the Redis resource name, explicit Upstash database name, and provider database id when known. Provider ids are safe to show. The Upstash Management API key, Redis password/token values, and full `redis://`/`rediss://` connection strings are redacted before diagnostics reach logs.

Required values and optional string settings are represented as `UpstashRedisValue`, which can hold either a literal string or an Aspire `ParameterResource`. Literal strings convert implicitly; parameterized optional settings use `UpstashRedisValue.FromParameter(...)`. Internally, the Redis resource annotation stores a single deployment state snapshot containing required values, ownership mode, infrastructure-only management credential sources, optional settings, and explicit-setting metadata for deploy reconciliation.

Use Aspire parameters for secrets and deploy-specific values:

```csharp
var databaseName = builder.AddParameter("upstash-database-name");
var accountEmail = builder.AddParameter("upstash-account-email");
var apiKey = builder.AddParameter("upstash-api-key", secret: true);
```

Aspire can resolve those parameters from its normal parameter sources during deployment, including cached prompted values and configuration-backed parameters such as `builder.AddParameterFromConfiguration(...)`. The Upstash Management API key is resolved only by the internal deploy pipeline path and is kept in infrastructure-only management credentials; it is not added to Redis connection properties, supplementary outputs, or application references.

Advanced consumers can access supplementary app-facing outputs from the Redis resource after it is marked for Upstash publishing:

```csharp
var outputs = cache.Resource.GetUpstashRedisOutputs();

var endpoint = outputs.Endpoint;
var port = outputs.Port;
var password = outputs.Password;
var tls = outputs.Tls;
var databaseName = outputs.DatabaseName;
```

The stable supplementary output names are `Endpoint`, `Port`, `Password`, `Tls`, and `DatabaseName`. Each accessor returns an `UpstashRedisOutputReference` with public `Name`, `ValueExpression`, and `Secret` metadata; `Password` is the only supplementary output whose `Secret` flag is true. These values are populated from the credential-bearing Upstash database detail response after deployment create/adopt/reconcile work succeeds; the Upstash Management API key, REST tokens, provider id, customer id, and billing/security metadata are not application outputs.

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

Repeated deployments reconcile supported mutable settings in deterministic order: read regions, plan, budget, then eviction. Unspecified options are left untouched, matching explicit provider state is a no-op with no mutation call, provider readiness/detail state is re-fetched after each mutation, and final state is verified before later output generation. If a provider update fails or does not converge, reconciliation raises an `UpstashRedisReconciliationException` naming the setting.

Local Aspire behavior is preserved: `.PublishToUpstash(...)` does not replace the Redis resource, does not call Upstash during model construction or local runs, and does not prevent normal `WithReference(cache)` usage.

Remote identity uses the explicit Upstash Redis database name as the source of truth. First deployments look up the configured name through `GET /redis/databases`, require exactly one case-sensitive match, then fetch details by provider id. Repeated deployments can load the cached `UpstashRedisRemoteIdentityState` through the Aspire deployment-state-backed store and pass it back into the resolver; the resolver accepts that id only when the cached configured name still matches, the fetched database still reports both the configured name and cached provider id, and a fresh configured-name lookup resolves exactly one database with that same provider id. If the configured name changes, v1 treats it as selecting a different remote database identity and never calls the provider rename endpoint. If a cached identity for the same configured name now points at a renamed database, the name resolves to a different provider id, or duplicate configured-name matches appear, deployment logic must fail rather than take over the wrong database.

After a successful create or adopt path and any mutable reconciliation, the deploy pipeline saves the resulting database name and provider id back to deployment state and applies app-facing Redis connection output to the existing Redis resource. The standard `WithReference(cache)` path receives a Redis connection string in the same format Aspire Redis uses for StackExchange.Redis-style configuration: `host:port,password=password,ssl=true`. The host, port, password, and TLS values come from the credential-bearing `GET /redis/database/{id}` detail response, not from Upstash management credentials. Provider endpoints must already be complete host names; single-label slugs and URI-like endpoint values fail as provider-contract errors rather than producing a broken connection string.

The test suite is Reqnroll-first. Feature files live under behavior-focused folders in `tests/Aspire.Hosting.Upstash.Redis/Features/`, shared scenario support lives under `tests/Aspire.Hosting.Upstash.Redis/Support/`, and the scenario map for future tasks is documented in `tests/Aspire.Hosting.Upstash.Redis/README.md`. Ownership-mode hardening includes pipeline-level fake-provider coverage for create-only, existing-only, create-or-adopt, repeated deploy identity reuse, duplicate-name failures, and cached identity edge cases.

Contributor validation note: opt-in live Upstash validation can now use the environment variables `UPSTASH_EMAIL` and `UPSTASH_API_KEY`. Any live test must stay explicitly gated and must always tear down or restore any remote state it touches so the Upstash account is left unchanged after the run. The live ownership scenarios use isolated `pin-170-*` database names and register delete cleanup as soon as a created provider id is known.

The Upstash management capability matrix is documented in [`plans/0.2-confirm-upstash-management-capability-matrix.md`](plans/0.2-confirm-upstash-management-capability-matrix.md). Key v1 decisions from that investigation:

- Management authentication uses separate native Upstash account email and Management API key values.
- Third-party marketplace Upstash accounts are not supported by the Developer API and should fail fast with a tailored error.
- Remote lookup uses list-by-account plus explicit database-name matching, with provider id accepted after discovery only when cached identity state still verifies against the explicit configured name, returned provider id, and a duplicate-checked configured-name lookup.
- Ownership resolution is deterministic: create-only rejects unmanaged existing names but preserves this deployment's verified cached remote identity on repeated deploys, existing-only rejects missing names, and create-or-adopt creates only when the explicit name is absent.
- Create supports database name, platform, primary region, read regions, plan, budget, eviction, and required-on TLS.
- Reconcile supports read regions, plan, budget, and eviction only.
- Immutable drift detection fails for database name identity mismatch, detectable platform mismatch, explicit primary-region mismatch, and remote TLS disabled state; these settings are not renamed, moved, replaced, or repaired automatically in v1.
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
