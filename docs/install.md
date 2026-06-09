# Install

Install `PinguApps.Aspire.Hosting.Upstash.Redis` into the AppHost project.

```powershell
dotnet add package PinguApps.Aspire.Hosting.Upstash.Redis
```

For C# AppHosts, import the namespace:

```csharp
using Aspire.Hosting.Upstash.Redis;
```

## TypeScript Package Consumption

TypeScript AppHosts also consume this integration through NuGet.

Aspire loads the .NET hosting assembly, reads its export metadata, and generates the TypeScript module consumed by the AppHost. That is why the TypeScript examples import from `./.modules/aspire.js` after `aspire restore` rather than from an npm package owned by this repository.

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
