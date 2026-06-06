# Aspire Hosting Upstash Redis

This package is an Aspire hosting integration for publishing a standard Aspire Redis resource to Upstash Redis during deployment.

Current state: the Aspire integration shape is confirmed and an implementation-ready skeleton is present. The package now extends the built-in Redis resource returned by `builder.AddRedis("cache")` and attaches Upstash deployment metadata with `.PublishToUpstash(...)`. The deploy pipeline step is intentionally a no-op until the later implementation tasks add Upstash API calls, reconciliation, and deployed connection outputs.

```csharp
var accountEmail = builder.AddParameter("upstash-account-email");
var apiKey = builder.AddParameter("upstash-api-key", secret: true);

var cache = builder.AddRedis("cache")
    .PublishToUpstash("orders-cache", accountEmail, apiKey);

builder.AddProject<Projects.Api>("api")
    .WithReference(cache);
```

Local Aspire behavior is preserved: `.PublishToUpstash(...)` does not replace the Redis resource, does not call Upstash during model construction or local runs, and does not prevent normal `WithReference(cache)` usage.
