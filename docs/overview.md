# Overview

`PinguApps.Aspire.Hosting.Upstash.Redis` lets an Aspire AppHost keep modelling Redis as Aspire's built-in `RedisResource` while opting that resource into Upstash Redis for deployment.

The package is intentionally narrow:

- Upstash Redis only.
- Deploy-time behaviour only.
- Normal local Aspire Redis behaviour remains intact.
- The explicit Upstash database name is the remote identity.
- The package never auto-deletes remote Upstash databases.
- Application-facing outputs expose Redis connection details, not Upstash Management API credentials.

## Product Contract

Consumer AppHosts start with standard Aspire Redis:

```csharp
IResourceBuilder<RedisResource> cache = builder.AddRedis("cache");
```

C# AppHosts add `.PublishToUpstash(...)`. TypeScript AppHosts add `await cache.publishToUpstash(...)` through Aspire's generated module.

In both languages the resource remains a normal Redis resource for consumers:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(cache);
```

```ts
let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);
```

## Local Vs Deploy

Local AppHost model construction records metadata only. It does not call Upstash, does not require valid Upstash credentials for local Redis behaviour, and does not replace the local Redis resource.

During `aspire deploy`, the package resolves configured parameters, talks to the Upstash Management API, creates or adopts the named database, validates immutable settings, reconciles explicitly configured mutable settings, and redirects the standard Redis connection output to the deployed Upstash Redis database.
