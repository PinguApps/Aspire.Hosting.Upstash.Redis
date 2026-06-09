# TypeScript AppHost Usage

TypeScript AppHosts use Aspire's generated module surface from the NuGet package.

Start from a fresh AppHost:

```powershell
aspire new aspire-ts-empty --name MyApp --output .\MyApp --non-interactive
Set-Location .\MyApp
```

Add the Redis hosting package and this package to `aspire.config.json`:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.Upstash.Redis": "<package version>"
  },
  "Parameters": {
    "upstash-database-name": "typescript-demo-cache",
    "upstash-account-email": "demo@example.com",
    "upstash-api-key": "demo-management-api-key"
  }
}
```

When validating this repository checkout instead of the published package, point `PinguApps.Aspire.Hosting.Upstash.Redis` at the local project path instead of a version string.

Then generate the TypeScript surface:

```powershell
aspire restore --non-interactive
```

Import the generated Upstash value catalogs and extension methods from `./.aspire/modules/aspire.mjs`:

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

The maintained TypeScript demo is [`samples/TypeScriptAppHost/apphost.mts`](../samples/TypeScriptAppHost/apphost.mts). The tested fixture is [`tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/apphost.mts`](../tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/apphost.mts).

## Local Validation

From the AppHost directory:

```powershell
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
aspire start --non-interactive --isolated
aspire wait cache --status healthy --timeout 120 --non-interactive
aspire stop --non-interactive
```

The local path continues to use standard Aspire Redis behaviour. Upstash credentials are deploy-time inputs, but local demo commands are easier if `aspire.config.json` contains placeholder `Parameters` values like the example above.

## Deploy

For non-interactive deploys, provide real values as Aspire parameter environment variables:

```powershell
$env:Parameters__upstash_database_name = "upstash-ts-test"
$env:Parameters__upstash_account_email = $env:UPSTASH_EMAIL
$env:Parameters__upstash_api_key = $env:UPSTASH_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

Run the same deploy again to verify that the package reuses the same configured remote database name instead of creating a second database.

## Shape Differences From C#

The TypeScript API intentionally differs from C#:

- TypeScript has one `publishToUpstash(databaseName, accountEmail, apiKey, options?)` method.
- The first three arguments are Aspire parameters.
- Optional settings are passed as a DTO object.
- Value catalogs such as `upstashRedisOwnershipMode.createOrAdopt` are used instead of raw strings.
- Supplementary outputs are async getter methods from `await cache.getUpstashRedisOutputs()`.

C# keeps callback-based options and literal-or-parameter helpers because those are natural in C# and do not translate cleanly through Aspire's guest-language generation.

## Outputs

Most consumers should keep using the standard Redis reference path:

```ts
worker = await worker.withReference(cache);
```

When a consumer needs individual Upstash Redis output references:

```ts
const outputs = await cache.getUpstashRedisOutputs();

const endpoint = await outputs.endpoint();
const port = await outputs.port();
const password = await outputs.password();
const tls = await outputs.tls();
const databaseNameOutput = await outputs.databaseName();
```

Only the Redis password output is secret. The Upstash Management API key is not an output.
