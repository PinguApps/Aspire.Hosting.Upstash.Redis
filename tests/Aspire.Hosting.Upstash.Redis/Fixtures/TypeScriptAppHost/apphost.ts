import { createBuilder } from "./.aspire/modules/aspire.mjs";
import {
  upstashRedisCloudPlatform,
  upstashRedisOwnershipMode,
  upstashRedisPlan,
  upstashRedisRegion,
} from "./.aspire/modules/pinguapps-aspire-hosting-upstash-redis.mjs";

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

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);
worker = await worker.withEnvironment("UPSTASH_REDIS_ENDPOINT", await outputs.endpoint());
worker = await worker.withEnvironment("UPSTASH_REDIS_PORT", await outputs.port());
worker = await worker.withEnvironment("UPSTASH_REDIS_PASSWORD", await outputs.password());
worker = await worker.withEnvironment("UPSTASH_REDIS_TLS", await outputs.tls());
worker = await worker.withEnvironment("UPSTASH_REDIS_DATABASE_NAME", await outputs.databaseName());

await builder.build().runAsync();
