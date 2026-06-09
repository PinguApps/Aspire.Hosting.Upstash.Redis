## Rolling state
- Goal: Move long-form package docs under `docs/`, keep README concise, and add maintained .NET/TypeScript examples.
- Current plan: README/front-door rewrite done; long-form docs added; TypeScript demo sample added and covered by docs-sample test.
- Open questions/risks: TypeScript demo restore/typecheck not run here because `aspire` CLI is unavailable.
- Next actions: Run `aspire restore`/`npm run typecheck` for `samples/TypeScriptAppHost` when Aspire CLI is available; adjust docs if restore output differs.
- Key paths: `README.md`; `docs/`; `samples/TypeScriptAppHost/`; `samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`; `tests/Aspire.Hosting.Upstash.Redis/Features/DocsSamples/AppHostSnippets.feature`

## Session log
### 2026-06-09 07:23 Z (agent/pin-184-111-move-long-form-docs-to-docs-and-add-minimal-net)
- Restructure docs [docs] (impact: high)
  - Why: PIN-184 requires README to become a concise front door and long-form guidance to live under `docs/`.
  - Change: Replaced long README manual with concise metadata/examples/links and added overview, install, .NET, TypeScript, configuration, deployment, outputs/security, samples, and testing docs. (files: `README.md`, `docs/*.md`)
- Add TypeScript demo [samples] (impact: med)
  - Why: README/docs need a real maintained minimal TypeScript AppHost example.
  - Change: Added `samples/TypeScriptAppHost` with `apphost.ts`, Aspire config, npm scripts, tsconfig, README, and ignores. (files: `samples/TypeScriptAppHost/*`)
- Align TypeScript docs/tests [tests] (impact: med)
  - Why: Existing planning docs had stale generated module paths and sample drift needed coverage.
  - Change: Updated TypeScript docs to `./.modules/aspire.js` and added a Reqnroll docs-sample check for the TypeScript demo source. (files: `docs/typescript-apphost-contract.md`, `docs/typescript-apphost-acceptance.md`, `tests/Aspire.Hosting.Upstash.Redis/Features/DocsSamples/AppHostSnippets.feature`, `tests/Aspire.Hosting.Upstash.Redis/Steps/AppHostSnippetsStepDefinitions.cs`)
  - Notes: `dotnet test Aspire.Hosting.Upstash.Redis.slnx` passed; `aspire` CLI was unavailable, so sample restore/typecheck could not be run.
