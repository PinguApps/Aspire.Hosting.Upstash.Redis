## Rolling state
- Goal: Implement ATS-enabled TypeScript AppHost package surface for Upstash Redis while preserving the C# contract.
- Current plan: package surface implemented; analyzer/build/test validation passed.
- Open questions/risks: Generated TypeScript fixture validation remains a later ticket; DTO uses `IReadOnlyList<UpstashRedisRegion>` for analyzer-clean collection shape.
- Next actions: Add TypeScript AppHost fixture/codegen validation in the next implementation stage.
- Key paths: `src/Aspire.Hosting.Upstash.Redis/`, `tests/Aspire.Hosting.Upstash.Redis/Features/ApiShape/PublishToUpstash.feature`

## Session log
### 2026-06-08 22:58 Z (agent/pin-182-91-implement-ats-enabled-typescript-apphost-support-in)
- Add ATS package surface [api] (impact: high)
  - Why: Enable Aspire guest-language generation for the approved TypeScript AppHost contract.
  - Change: Enabled Aspire integration analyzers, added DTO publish bridge, output bridge, value catalogs, and explicit export/ignore metadata. (files: `src/Aspire.Hosting.Upstash.Redis/*`)
  - Notes: `[AspireExport]` analyzer requires classic static extension methods.
- Cover TypeScript bridge API shape [tests] (impact: med)
  - Why: Public surface changed and must preserve existing deployment semantics.
  - Change: Added Reqnroll scenarios for DTO option capture, TLS rejection, output bridge, and export metadata. (files: `tests/Aspire.Hosting.Upstash.Redis/Features/ApiShape/PublishToUpstash.feature`, `tests/Aspire.Hosting.Upstash.Redis/Steps/PublishToUpstashStepDefinitions.cs`, `tests/Aspire.Hosting.Upstash.Redis/Support/UpstashRedisScenarioContext.cs`)
  - Notes: Validation passed with `dotnet build --nologo && dotnet test --no-build --nologo`.
