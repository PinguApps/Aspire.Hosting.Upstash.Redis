# Aspire Integration Decision

## Status
Accepted for Aspire `13.4.2`.

## Decision
The package keeps Aspire's built-in Redis resource as the resource of record. Consumers continue to create Redis through:

```csharp
var cache = builder.AddRedis("cache");
```

Upstash opt-in is attached to that same resource:

```csharp
cache.PublishToUpstash(
    "orders-cache",
    builder.AddParameter("upstash-account-email"),
    builder.AddParameter("upstash-api-key", secret: true));
```

The extension point is an `IResourceBuilder<RedisResource>` extension method. It attaches an `UpstashRedisDeploymentAnnotation` to `cache.Resource` and registers a deploy-only Aspire pipeline step with `WithPipelineStepFactory`. The step depends on `WellKnownPipelineSteps.DeployPrereq` and is required by `WellKnownPipelineSteps.Deploy`.

## Evidence
- Aspire `13.4.2` Redis uses `RedisResource : ContainerResource, IResourceWithConnectionString`. Its connection string is built from the primary endpoint host/port, optional password, and TLS flag. Its connection properties are `Host`, `Port`, optional `Password`, and `Uri`.
- Aspire `WithReference` for `IResourceWithConnectionString` injects `ConnectionStrings__{resourceName}` plus connection properties from `GetConnectionProperties()`. Therefore the Upstash integration must not replace Redis with a wrapper that fails to preserve this interface and property behavior.
- Aspire `13.4` exposes deployment pipelines through `PipelineStepAnnotation` and `WithPipelineStepFactory`. `WellKnownPipelineSteps.Deploy` is the deploy aggregation step; `WellKnownPipelineSteps.DeployPrereq` runs before deploy work and initializes deployment state.
- Aspire also exposes `IDeploymentStateManager`, but v1 should not rely on cached state as the source of truth. The deterministic remote database name remains the stable identity. Deployment state may cache provider IDs later as an optimization or diagnostic aid, but deploy logic must be able to re-lookup by explicit remote name.

Source references used for this decision:

- `Aspire.Hosting.Redis` `13.4.2` package and `dotnet/aspire` `release/13.4` source for `RedisResource` and `RedisBuilderExtensions`.
- `dotnet/aspire` `release/13.4` source for `ResourceBuilderExtensions.WithReference`.
- `dotnet/aspire` `release/13.4` source for `PipelineStepFactoryExtensions`, `WellKnownPipelineSteps`, and `IDeploymentStateManager`.
- Aspire deployment pipeline documentation: <https://aspire.dev/deployment/pipelines/>
- Aspire custom deployment documentation: <https://learn.microsoft.com/dotnet/aspire/fundamentals/custom-deployments>

## Rejected Alternatives
- **Wrapper/custom Redis resource:** rejected for v1 because it would make standard `WithReference(cache)` compatibility harder and would duplicate Aspire Redis behavior that already exists in `RedisResource`.
- **Directly subclassing `RedisResource`:** rejected because consumers start from `builder.AddRedis("cache")`; replacing the resource type would either require a different creation API or invasive resource swapping.
- **`DeployingCallbackAnnotation` only:** rejected for Aspire `13.4.2` because the pipeline system is the current deploy mechanism and gives explicit step ordering, dependencies, tags, activity reporting, and deployment state access.
- **`PublishingCallbackAnnotation` plus `DeployingCallbackAnnotation`:** rejected because this package does not need to emit publish artifacts for Upstash Redis in v1. Upstash provisioning/reconciliation belongs to deploy execution.

## Model-Time Boundary
Model-time code may:

- expose public fluent APIs
- validate immediately-known arguments
- attach `UpstashRedisDeploymentAnnotation`
- capture explicit option settings
- register the deploy pipeline step
- preserve all normal Redis resource annotations, endpoints, password handling, connection properties, and references

Model-time code must not:

- call the Upstash API
- resolve management credentials
- create or reconcile remote databases
- replace Redis connection behavior used by local `aspire run`

## Deploy-Time Boundary
Deploy-time code will:

- resolve account email and API key parameters
- locate, create, adopt, or reconcile the explicit remote database name
- fail clearly on unsupported drift
- retrieve app-facing Redis outputs
- update connection output behavior for deployment without leaking management credentials
- optionally use `IDeploymentStateManager` for cache/diagnostic state while still re-looking up by remote database name when needed

## Initial Skeleton
This task adds a no-op deployment skeleton so later tasks have concrete types to build behind:

- `UpstashRedisBuilderExtensions.PublishToUpstash(...)`
- `UpstashRedisDeploymentAnnotation`
- `UpstashRedisDeploymentOptions`
- `UpstashRedisOwnershipMode`

Reqnroll scenarios prove that `.PublishToUpstash(...)` attaches metadata to the existing `RedisResource`, keeps standard Redis connection properties, and does not prevent a consuming container from building the normal `WithReference(cache)` chain.
