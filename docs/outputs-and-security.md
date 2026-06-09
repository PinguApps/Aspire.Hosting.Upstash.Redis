# Outputs And Security

There are two application-facing output paths.

## Standard Redis Reference

This is the recommended path:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(cache);
```

```ts
worker = await worker.withReference(cache);
```

After a successful deployment, the normal Redis connection output points at the Upstash Redis database.

## Supplementary Outputs

C#:

```csharp
UpstashRedisOutputs outputs = cache.Resource.GetUpstashRedisOutputs();
```

TypeScript:

```ts
const outputs = await cache.getUpstashRedisOutputs();
const endpoint = await outputs.endpoint();
const port = await outputs.port();
const password = await outputs.password();
const tls = await outputs.tls();
const databaseName = await outputs.databaseName();
```

Supplementary outputs expose:

| Output | Secret |
| --- | --- |
| Endpoint | No |
| Port | No |
| Password | Yes |
| Tls | No |
| DatabaseName | No |

They do not expose the Upstash Management API key, provider database id, customer id, REST tokens, billing metadata, or account-wide metadata.

## Credential Boundary

Infrastructure-only inputs:

- Upstash account email
- Upstash Management API key

Application-facing values:

- Redis connection string
- endpoint
- port
- password
- TLS flag
- database name

Keep `upstash-api-key` as a secret parameter. Application projects should normally consume Redis through `WithReference(cache)`.
