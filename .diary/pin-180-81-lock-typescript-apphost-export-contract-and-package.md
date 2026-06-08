## Rolling state
- Goal: Lock the TypeScript AppHost export contract and package strategy for Upstash Redis.
- Current plan: Contract documented; next stage can implement ATS attributes, DTO, catalogs, and TypeScript fixture coverage.
- Open questions/risks: Generated ATS output must verify the documented `publishToUpstash` and `getUpstashRedisOutputs` names.
- Next actions: Implement DTO adapter; add ATS exports/ignores/catalogs; add Reqnroll API-shape coverage.
- Key paths: `docs/typescript-apphost-contract.md`; `docs/typescript-apphost-acceptance.md`; `src/Aspire.Hosting.Upstash.Redis/`

## Session log
### 2026-06-08 21:57 Z (agent/pin-180-81-lock-typescript-apphost-export-contract-and-package)
- Add TypeScript AppHost export contract [docs] (impact: med)
  - Why: PIN-180 needs a single implementation-ready API-shape and packaging decision before ATS implementation.
  - Change: Documented DTO-based `publishToUpstash`, value catalogs, output access, NuGet-first packaging, compatibility guardrails, and next tasks (files: `docs/typescript-apphost-contract.md`, `docs/typescript-apphost-acceptance.md` | cmds: `dotnet restore Aspire.Hosting.Upstash.Redis.slnx`, `dotnet build Aspire.Hosting.Upstash.Redis.slnx --no-restore`)
  - Notes: Verified Aspire 13.4.2 ATS attribute names locally through restored assembly reflection.
