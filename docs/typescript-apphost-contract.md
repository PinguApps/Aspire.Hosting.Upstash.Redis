# TypeScript AppHost Export Contract

This note records the TypeScript AppHost authoring contract for `PinguApps.Aspire.Hosting.Upstash.Redis`. Implementation tickets should treat this as the API-shape source of truth unless a later ADR explicitly supersedes it.

## Decision Summary

TypeScript AppHosts use Aspire's NuGet-first guest-language integration flow. The existing NuGet package remains the required integration package; Aspire loads the .NET hosting assembly, reads ATS metadata, and generates the TypeScript authoring surface from that assembly.

The TypeScript surface should be DTO-based and intentionally smaller than the current C# surface:

- C# keeps the existing `PublishToUpstash(..., Action<UpstashRedisDeploymentOptions>? configure = null)` overloads.
- TypeScript gets one exported `publishToUpstash` shape that marks an existing `RedisResource` for deploy-time Upstash Redis.
- The exported TypeScript options use a DTO, not a callback, because callbacks are a poor fit for ATS/JSON transport and generated guest-language APIs.
- Provider/deployment mechanics remain internal and are not exported.

## Recommended TypeScript Shape

The generated TypeScript AppHost API shape:

```ts
import {
  createBuilder,
  upstashRedisCloudPlatform,
  upstashRedisOwnershipMode,
  upstashRedisPlan,
  upstashRedisRegion,
} from "./.modules/aspire.js";

const builder = await createBuilder();

const databaseName = await builder.addParameter("upstash-database-name");
const accountEmail = await builder.addParameter("upstash-account-email");
const apiKey = await builder.addParameter("upstash-api-key", { secret: true });

let cache = await builder.addRedis("cache");
cache = await cache.publishToUpstash(
  databaseName,
  accountEmail,
  apiKey,
  {
    ownershipMode: upstashRedisOwnershipMode.createOrAdopt,
    platform: upstashRedisCloudPlatform.aws,
    primaryRegion: upstashRedisRegion.awsEuWest1,
    readRegions: [upstashRedisRegion.awsEuWest2],
    plan: upstashRedisPlan.payAsYouGo,
    budget: 20,
    eviction: true,
    tls: true,
  },
);

const outputs = await cache.getUpstashRedisOutputs();

let api = await builder.addProject("api", "../Api/Api.csproj");
api = await api.withReference(cache);
api = await api.withEnvironment("UPSTASH_REDIS_ENDPOINT", await outputs.endpoint());
api = await api.withEnvironment("UPSTASH_REDIS_TLS", await outputs.tls());
```

The first three arguments are Aspire parameter builders/resources so the remote database name and management credentials remain deploy-time values. Literal database names remain C#-only in the v1 TypeScript surface. The DTO is optional; omitting it means `CreateOrAdopt` ownership and no explicit create/reconcile settings.

Supplementary outputs are accessed through `await cache.getUpstashRedisOutputs()`. Because ATS generates getter-only C# properties as async TypeScript methods, the returned object exposes getter methods: `endpoint()`, `port()`, `password()`, `tls()`, and `databaseName()`. Callers await and consume the returned references in expressions; they should not construct or mutate output references.

## Export And Ignore Map

| Surface | Decision | Rationale |
| --- | --- | --- |
| `UpstashRedisBuilderExtensions` | Export a new ATS-friendly extension method named `PublishToUpstashForTypeScript` with `[AspireExport("pinguapps.upstash.redis.publishToUpstash", MethodName = "publishToUpstash")]`. Keep the current callback overloads C#-only with `[AspireExportIgnore]`. | A single generated method avoids overload ambiguity and preserves existing C# ergonomics. |
| Existing `PublishToUpstash(..., Action<UpstashRedisDeploymentOptions>?)` overloads | `[AspireExportIgnore]`. | Callback mutation is not a stable guest-language transport contract. |
| `PublishToUpstashForTypeScript(IResourceBuilder<RedisResource>, IResourceBuilder<ParameterResource>, IResourceBuilder<ParameterResource>, IResourceBuilder<ParameterResource>, UpstashRedisDeploymentOptionsDto? options = null)` | `[AspireExport("pinguapps.upstash.redis.publishToUpstash", MethodName = "publishToUpstash")]`. | Gives TypeScript one concise method while allowing the C# implementation name to avoid overload conflicts. |
| `UpstashRedisDeploymentOptions` | C# only; do not export. | Mutable callback options track explicit settings for C# and should not leak as the TypeScript DTO. |
| New `UpstashRedisDeploymentOptionsDto` | `[AspireDto]`. | DTO carries guest-language optional settings through ATS/JSON. |
| `UpstashRedisValue` | C# only; do not export. Mark public factories/operators `[AspireExportIgnore]` if ATS would otherwise see them. | TypeScript v1 uses parameter builders for required deploy-time values and value catalogs for optional settings. |
| `UpstashRedisResourceExtensions.GetUpstashRedisOutputs` | Export a builder extension named `GetUpstashRedisOutputsForTypeScript` with `[AspireExport("pinguapps.upstash.redis.getUpstashRedisOutputs", MethodName = "getUpstashRedisOutputs")]`. Keep the existing `RedisResource` extension C#-only. | TypeScript authors should call `await cache.getUpstashRedisOutputs()` without reaching through `cache.resource`. |
| `UpstashRedisOutputs` | `[AspireExport("pinguapps.upstash.redis.outputs", ExposeProperties = true, ExposeMethods = false)]`. Mark `Properties`, `Populate`, and `IsSecret` ignored. | TypeScript needs generated getter methods for deployed app-facing output references, not population mechanics or helper collections. |
| `UpstashRedisOutputReference` | `[AspireExport("pinguapps.upstash.redis.outputReference", ExposeProperties = false, ExposeMethods = false)]`; ignore `Name`, `Secret`, `References`, `ValueExpression`, `SetValue`, and provider/reference helper methods if ATS would otherwise see them. | Callers consume references in expressions/environment variables; reference metadata is not part of the TypeScript authoring surface. |
| `UpstashRedisOwnershipMode` | Do not export the enum type directly. Export enum members as `[AspireValue("upstashRedisOwnershipMode", Name = "...")]` catalog entries. | Generated TypeScript should use stable values such as `upstashRedisOwnershipMode.createOrAdopt`. |
| `UpstashRedisCloudPlatform` | Do not export the enum type directly. Export enum members as `[AspireValue("upstashRedisCloudPlatform", Name = "...")]` catalog entries. | Avoid raw provider strings and keep generated names discoverable. |
| `UpstashRedisPlan` | Do not export the enum type directly. Export enum members as `[AspireValue("upstashRedisPlan", Name = "...")]` catalog entries. | Keeps supported plans explicit and type-checkable. |
| `UpstashRedisRegion` | Do not export the enum type directly. Export enum members as `[AspireValue("upstashRedisRegion", Name = "...")]` catalog entries. | Keeps supported regions explicit and type-checkable. |
| `UpstashRedisOutputNames` | Do not export. | Generated output properties are the public cross-language API. |
| Deployment, management, provider, reconciliation, drift, annotation, state, and pipeline types | Leave internal or mark `[AspireExportIgnore]` if any public type becomes visible to ATS. | These are implementation mechanics and must not become guest-language commitments. |

## DTO Contract

`UpstashRedisDeploymentOptionsDto` should contain only values a TypeScript author can reasonably set:

| Property | TypeScript shape | Notes |
| --- | --- | --- |
| `ownershipMode` | ownership value catalog entry | Optional, defaults to `createOrAdopt`. |
| `platform` | cloud platform value catalog entry | Create-time setting; fail on incompatible existing database drift. |
| `primaryRegion` | region value catalog entry | Create-time setting; fail on incompatible existing database drift. |
| `readRegions` | region value catalog entry array | Mutable reconcile setting. |
| `plan` | plan value catalog entry | Mutable reconcile setting. |
| `budget` | positive number | Mutable reconcile setting; valid only when plan allows budget. |
| `eviction` | boolean | Mutable reconcile setting. |
| `tls` | boolean | Optional but `false` must fail with the existing TLS-required validation. |

The DTO-to-options adapter should create the existing `UpstashRedisDeploymentOptions` internally and reuse its validation/provider mapping. Do not duplicate provider strings or reconciliation rules in the DTO. Parameter-backed optional settings remain C#-only for v1 TypeScript support.

## Naming And Capability IDs

- Exported method name: `publishToUpstash`.
- C# implementation method name: `PublishToUpstashForTypeScript`.
- Supplementary output method name: `getUpstashRedisOutputs`.
- Do not export multiple TypeScript overloads with the same semantic job.
- Use one options DTO rather than split methods like `publishToExistingUpstash` or `publishToNewUpstash`; ownership is an option value.
- Value catalog names should be lower camel case in TypeScript:
  - `upstashRedisOwnershipMode.createOrAdopt`, `createOnly`, `existingOnly`
  - `upstashRedisCloudPlatform.aws`, `gcp`
  - `upstashRedisPlan.free`, `payAsYouGo`, `fixed250Mb`, `fixed1Gb`, `fixed5Gb`, `fixed10Gb`, `fixed50Gb`, `fixed100Gb`, `fixed500Gb`
  - `upstashRedisRegion.awsEuWest2`, etc.
- Supplementary output getter method names should be lower camel case: `endpoint()`, `port()`, `password()`, `tls()`, `databaseName()`. Generated TypeScript should not expose helper accessors such as `properties()`, `name()`, `secret()`, `references()`, or `valueExpression()`.
- Capability IDs:
  - `pinguapps.upstash.redis.publishToUpstash`
  - `pinguapps.upstash.redis.getUpstashRedisOutputs`
  - `pinguapps.upstash.redis.outputs`
  - `pinguapps.upstash.redis.outputReference`

## Packaging Decision

NuGet remains the primary and required package for both C# and TypeScript AppHosts.

No npm package is required for the core integration. Publishing npm as the source of truth would duplicate the .NET hosting integration, fight Aspire's generated guest-language SDK flow, and create two compatibility surfaces for the same deploy-time behavior.

A JavaScript or TypeScript artifact may be added later only as a sample, template, or helper around the generated Aspire module. It must not contain provider logic, deployment behavior, or an alternate definition of the integration contract.

## Backward-Compatibility Guardrails

- Existing C# AppHost usage remains supported.
- Local `builder.AddRedis("cache")` behavior remains normal Aspire Redis behavior.
- Upstash behavior remains opt-in and deploy-only through `publishToUpstash`/`PublishToUpstash`.
- Upstash account email and Management API key remain infrastructure-only inputs.
- Application-facing outputs expose Redis connection details, never the Upstash Management API key.
- C# and TypeScript must share the same deployment pipeline, ownership rules, drift checks, reconciliation behavior, and output redirection.
- The package must not auto-delete remote Upstash databases.

## Implementation Tasks

1. Add the DTO type and DTO-to-`UpstashRedisDeploymentOptions` adapter.
2. Add the single `[AspireExport(MethodName = "publishToUpstash")]` TypeScript-facing extension method.
3. Mark current callback overloads and non-contract public helpers with `[AspireExportIgnore]` where ATS would otherwise see them.
4. Add value catalog metadata for ownership modes, cloud platforms, plans, and regions.
5. Add and export the builder-level `getUpstashRedisOutputs` method and getter-only output reference properties, then validate their generated TypeScript accessors are method calls.
6. Add Reqnroll API-shape coverage for generated TypeScript names, DTO option capture, value catalogs, and supplementary output references.
7. Update README and TypeScript getting-started docs only after generated API validation confirms the names above.
