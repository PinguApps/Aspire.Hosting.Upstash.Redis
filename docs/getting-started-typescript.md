# TypeScript AppHost Usage

TypeScript AppHosts use Aspire's generated module surface from the NuGet package. Run `aspire restore` so Aspire generates `./.modules/aspire.js`, then import the generated Upstash value catalogs and extension methods from that file.

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

The maintained TypeScript demo is [`samples/TypeScriptAppHost/apphost.ts`](../samples/TypeScriptAppHost/apphost.ts). The tested fixture is [`tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/apphost.ts`](../tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/apphost.ts).

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
