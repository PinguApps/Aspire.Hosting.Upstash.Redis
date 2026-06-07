## Rolling state
- Goal: Fix PIN-179 so `CreateOnly` fails whenever the configured Upstash Redis database name already exists.
- Current plan: Implemented strict CreateOnly ownership resolution; updated ownership deployment/resolution coverage and docs.
- Open questions/risks: None.
- Next actions: Prepare final summary.
- Key paths: `src/Aspire.Hosting.Upstash.Redis/Deployment/UpstashRedisOwnershipResolver.cs`, `tests/Aspire.Hosting.Upstash.Redis/Features/OwnershipModes/OwnershipDeployment.feature`, `tests/Aspire.Hosting.Upstash.Redis/Features/OwnershipModes/OwnershipResolution.feature`

## Session log
### 2026-06-07 22:53 Z (agent/pin-179-createonly-doesn-t-fail-when-db-already-exists)
- Fix strict CreateOnly ownership [deployment] (impact: med)
  - Why: PIN-179 showed `CreateOnly` adopting an existing named database through cached identity reuse.
  - Change: Removed the CreateOnly cached-managed-identity adoption exception; CreateOnly now fails on any existing named database. (files: `src/Aspire.Hosting.Upstash.Redis/Deployment/UpstashRedisOwnershipResolver.cs`)
  - Notes: `CreateOrAdopt` and `ExistingOnly` still reuse verified cached identity where appropriate.
- Update ownership coverage and docs [tests] (impact: med)
  - Why: Existing Reqnroll scenarios and README documented the old repeated CreateOnly adoption behavior.
  - Change: Replaced repeated CreateOnly adoption scenarios with strict failure coverage; updated README/test matrix/identity-plan wording. (files: `tests/Aspire.Hosting.Upstash.Redis/Features/OwnershipModes/OwnershipDeployment.feature`, `tests/Aspire.Hosting.Upstash.Redis/Features/OwnershipModes/OwnershipResolution.feature`, `tests/Aspire.Hosting.Upstash.Redis/Steps/OwnershipDeploymentStepDefinitions.cs`, `README.md`, `tests/Aspire.Hosting.Upstash.Redis/README.md`, `plans/3.3-implement-remote-lookup-and-stable-identity-handling.md`)
  - Notes: Validated with `dotnet test tests/Aspire.Hosting.Upstash.Redis/Aspire.Hosting.Upstash.Redis.Tests.csproj --filter "FullyQualifiedName~Ownership"` and full `dotnet test tests/Aspire.Hosting.Upstash.Redis/Aspire.Hosting.Upstash.Redis.Tests.csproj`.
