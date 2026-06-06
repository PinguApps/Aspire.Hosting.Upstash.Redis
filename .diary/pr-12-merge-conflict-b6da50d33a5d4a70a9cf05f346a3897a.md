## Rolling state
- Goal: Resolve PR 12 merge conflicts for PIN-159 against latest main.
- Current plan: Conflicts resolved; validate and commit merge resolution.
- Open questions/risks: None known after build/test pass.
- Next actions: Commit resolved merge files.
- Key paths: `AGENTS.md`, `README.md`, `src/Aspire.Hosting.Upstash.Redis/UpstashRedisDeploymentState.cs`, `tests/Aspire.Hosting.Upstash.Redis/`

## Session log
### 2026-06-06 19:25 Z (pr-12-merge-conflict-b6da50d33a5d4a70a9cf05f346a3897a)
- Resolve merge conflicts [build] (impact: med)
  - Why: Preserve base task 2.1 state annotation model and PR task 2.3 option/domain validation.
  - Change: Kept annotation `State`, moved provider-domain validation through `UpstashRedisDeploymentState`, merged docs/test steps, and verified. (files: `AGENTS.md`, `README.md`, `src/Aspire.Hosting.Upstash.Redis/UpstashRedisDeploymentAnnotation.cs`, `src/Aspire.Hosting.Upstash.Redis/UpstashRedisDeploymentState.cs`, `tests/Aspire.Hosting.Upstash.Redis/Steps/PublishToUpstashStepDefinitions.cs`, `tests/Aspire.Hosting.Upstash.Redis/Support/UpstashRedisScenarioContext.cs` | cmds: `dotnet build ./Aspire.Hosting.Upstash.Redis.slnx`, `dotnet test ./tests/Aspire.Hosting.Upstash.Redis/Aspire.Hosting.Upstash.Redis.Tests.csproj`)
