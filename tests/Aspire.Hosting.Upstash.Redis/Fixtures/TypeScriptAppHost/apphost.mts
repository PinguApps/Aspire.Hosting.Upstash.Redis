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
  readRegions: [upstashRedisRegion.awsEuWest2],
  plan: upstashRedisPlan.payAsYouGo,
  budget: 20,
  eviction: true,
  tls: true,
});

const outputs = await cache.getUpstashRedisOutputs();
const outputReferences = [
  await outputs.endpoint(),
  await outputs.port(),
  await outputs.password(),
  await outputs.tls(),
  await outputs.databaseName(),
];

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);
void outputReferences;

const app = await builder.build();
await app.run();
