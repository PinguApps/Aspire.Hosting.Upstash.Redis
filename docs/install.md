# Install

## C# AppHost

```powershell
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```

Import the namespace in the AppHost:

```csharp
using Aspire.Hosting.Upstash.Redis;
```

## TypeScript AppHost

TypeScript AppHosts also consume this integration through NuGet, but the package is added through `aspire.config.json`, not `dotnet add package`.

For a released package:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.Upstash.Redis": "<package version>"
  }
}
```

When validating this repository checkout instead of the published package, point the package entry at the local project:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "13.4.3",
    "PinguApps.Aspire.Hosting.Upstash.Redis": "../../src/Aspire.Hosting.Upstash.Redis/Aspire.Hosting.Upstash.Redis.csproj"
  }
}
```

Then generate the TypeScript surface:

```powershell
aspire restore --non-interactive
```

Aspire loads the .NET hosting assembly, reads its export metadata, and generates the TypeScript module consumed by the AppHost. That is why the TypeScript examples import from `./.aspire/modules/aspire.mjs` after `aspire restore` rather than from an npm package owned by this repository.

An npm package is not required for the integration itself. Adding one would create a second distribution surface for the same deploy-time behaviour.

Use npm only for normal TypeScript tooling such as `typescript` or `tsx`.

## Required Parameters

Every AppHost needs:

| Parameter | Secret | Purpose |
| --- | --- | --- |
| `upstash-database-name` | No | Remote Upstash Redis database name and repeated-deploy identity. |
| `upstash-account-email` | No | Upstash account email used by deployment infrastructure. |
| `upstash-api-key` | Yes | Upstash Management API key used by deployment infrastructure. |

The account email and Management API key are deployment inputs. Application resources receive Redis connection details only.

For non-interactive deploys, provide real values as Aspire parameter environment variables:

```powershell
$env:Parameters__upstash_database_name = "upstash-ts-test"
$env:Parameters__upstash_account_email = $env:UPSTASH_EMAIL
$env:Parameters__upstash_api_key = $env:UPSTASH_API_KEY
```
