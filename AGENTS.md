# AGENTS.md
## WORK DIARY

### Purpose
- Keep a small, high-signal diary so future sessions can resume quickly.

### When to read/write
- On session start: read the diary file (if it exists) to regain context.
- Once per response (just before replying): update the diary ONLY if you took meaningful actions (code/config changes, important commands run, decisions made, constraints/bugs discovered, tasks created that affect next steps). Otherwise: do not write.

### Location
- Always read/write inside `.diary/`

### Filename (from git branch)
- If branch is `vk/<suffix>` or `feature/<suffix>` → file is `.diary/<suffix>.md`
  - e.g. `vk/ab12-foo-bar` → `.diary/ab12-foo-bar.md`
- The branch should always have a prefix, but the prefix cannot be guaranteed, just use the suffix in every case after the `/`.

### Format (Markdown, compact)
- The file has two sections:

1) Rolling state (edit in place; keep ≤12 bullets total)
```
## Rolling state
- Goal: <one sentence>
- Current plan: <1–3 bullets>
- Open questions/risks: <0–3 bullets>
- Next actions: <1–5 bullets>
- Key paths: <optional; 1–5 entries>
```

2) Session log (append-only; per response keep ≤5 bullets)
```
## Session log
### <YYYY-MM-DD HH:MM Z> (<branch>)
- <Verb + object> [area] (impact: none|low|med|high)
  - Why: <reason/decision>
  - Change: <what changed> (files: <a,b,c> | cmds: `<...>`)
  - Notes: <gotchas/follow-ups> (optional)
```

### Compression rules
- Prefer deltas over narration (Add/Remove/Refactor/Fix).
- Use short tags for area: [ui] [api] [db] [auth] [infra] [build] [tests] etc...
- Include "Why" for any non-obvious decision and "Notes" for any caveat.
- Do not exceed caps; omit low-value detail.

## Operating Principles

### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Repository-Specific Guidance

### Preserve Baseline Content
- Everything above this section is the user-authored baseline. Keep it intact.
- Add repo-state updates beneath the existing content unless a future change clearly requires restructuring for accuracy.

### Repository Goal
- This repository is for an open source Aspire hosting integration for Upstash Redis.
- The intended consumer experience starts from standard Aspire Redis usage, such as `builder.AddRedis("cache")`.
- Local development should continue to behave like normal Aspire Redis.
- Upstash behavior is opt-in and should only happen during `aspire deploy`, not during normal local runs.

### Planned Product Contract
- The package should let a consumer opt a standard Redis resource into Upstash publishing through a single publish-oriented API.
- The deployment contract must support three ownership modes:
  - create-only
  - existing-only
  - create-or-adopt
- The remote Upstash database name is explicit and required.
- Upstash management authentication is infrastructure-only and uses separate account email and API key values.
- The consuming application should receive Redis connection details, not the Upstash management API key.
- Repeated deploys should target the same intended remote database and reconcile only settings the caller explicitly set.
- If an explicitly requested setting cannot be safely reconciled, deployment must fail clearly.
- The package must never auto-delete remote Upstash databases in v1.
- App-facing outputs are expected to include:
  - a standard Redis connection string
  - host / endpoint
  - port
  - password
  - TLS enabled flag
  - database name

### Current Repository State
- The repository currently contains the package project, the test project, shared build settings, planning artifacts, decision records, the Aspire integration skeleton from task `0.1`, the locked public API shape from task `1.1`, the internal resource annotation/state model from task `2.1`, the typed Upstash Redis management client layer from task `2.2`, the Upstash Redis option/domain model from task `2.3`, deploy-time auth/parameter resolution from task `3.1`, the ownership-resolution decision engine from task `3.2`, the remote identity resolver from task `3.3`, the create flow from task `4.1`, the mutable-setting reconciler from task `4.2` with deploy-pipeline remote identity cache wiring, and immutable drift detection from task `4.3`.
- `src/Aspire.Hosting.Upstash.Redis/Aspire.Hosting.Upstash.Redis.csproj` is the main package project to implement.
- `tests/Aspire.Hosting.Upstash.Redis/` is the single test project and should remain the home for the package test suite.
- The test project now has a Reqnroll feature taxonomy and shared support layer from task `1.2`; read `tests/Aspire.Hosting.Upstash.Redis/README.md` before adding scenarios.
- `plans/` contains the current implementation roadmap as 22 numbered task files from `0.1` through `7.3`.
- `decisions/` contains accepted architecture and product decision records. Future files that record durable decisions, rejected alternatives, or investigation outcomes should live there rather than in `plans/`.
- `.diary/` contains branch-specific session state and must be read and maintained per the diary rules above.
- `README.md` documents the current skeleton and planning/investigation state and must continue to be brought into sync as real behavior lands.
- Plan `0.2` is complete and now contains the authoritative Upstash Redis management capability matrix for v1.
- Plan `1.1` is complete; `.PublishToUpstash(...)` is the locked public entry point, ownership is expressed with `UpstashRedisOwnershipMode`, and required/optional deploy-time strings are captured as `UpstashRedisValue` literal-or-parameter sources.
- Plan `1.2` is complete and now defines the Reqnroll spec matrix, fake-provider default pattern, Aspire model inspection helpers, and opt-in live-provider cleanup pattern.
- Plan `2.1` is complete; `.PublishToUpstash(...)` attaches an internal `UpstashRedisDeploymentState` snapshot to the built-in `RedisResource` through an internal annotation, preserving required inputs, ownership mode, management credential value sources, optional settings, and explicit-setting metadata without changing local Redis behavior.
- Plan `2.2` is complete; `src/Aspire.Hosting.Upstash.Redis/Management/` contains the narrow internal client for the supported Upstash Redis Developer API endpoints, typed DTOs, Basic-auth helper, readiness polling helper, and typed provider failure classification.
- Plan `2.3` is complete; public typed helpers now cover Upstash Redis cloud platforms, regions, plans, and budgets while internal provider-domain mapping validates literal values and preserves parameter-backed sources for deploy-time resolution.
- Plan `3.1` is complete; the registered Redis deploy pipeline step resolves `UpstashRedisDeploymentState` through Aspire `ParameterResource.GetValueAsync(...)`, validates parameter-backed provider options after resolution, produces actionable missing-parameter failures, and keeps the Upstash Management API key inside infrastructure-only management credentials rather than app-facing Redis outputs.
- Plan `3.2` is complete; `src/Aspire.Hosting.Upstash.Redis/Deployment/` contains the internal ownership resolver that looks up the explicit remote database name through the management client, selects create or adopt for the three ownership modes, and raises stable ownership-resolution failures for create-only collisions, existing-only misses, and incompatible existing databases.
- Plan `3.3` is complete; `UpstashRedisRemoteIdentityResolver` uses explicit database-name lookup as the v1 identity source of truth, can reuse a cached provider id through `UpstashRedisRemoteIdentityState` loaded from the Aspire deployment-state-backed store, verifies cached detail responses still have the configured name and cached provider id, confirms a fresh configured-name lookup resolves exactly one matching provider id before cached reuse, treats configured-name changes as selecting a different remote identity, and fails unsafe drift or duplicate-name situations.
- Plan `4.1` is complete; `UpstashRedisCreateFlow` maps resolved v1 options to `POST /redis/database`, runs only for create ownership results, sends TLS as required-on, waits for the created database to become active with no modifying state, returns credential-bearing details for later output tasks while failing provider-contract responses that omit required Redis connection fields, and the deploy pipeline persists the resolved remote identity after successful create/adopt.
- Plan `4.2` is complete; `UpstashRedisReconciler` enforces only explicit desired read regions, plan, budget, and eviction settings on existing databases, applies updates in deterministic order, re-fetches readiness/detail state after each mutation, verifies final convergence, and raises setting-specific reconciliation failures.
- Plan `4.3` is complete; `UpstashRedisImmutableDriftDetector` enforces fail-fast drift for database name identity, detectable platform, explicit primary region, and TLS disabled state before unsafe mutation, while leaving read regions, plan, budget, and eviction for mutable reconciliation.
- Task agents can now receive Upstash management credentials through environment variables `UPSTASH_EMAIL` and `UPSTASH_API_KEY`.

### Technical Baseline
- Target framework: `.NET 10`.
- Target Aspire version for v1 planning: `13.4.2`.
- The package is Upstash Redis only. Do not broaden scope to non-Redis Upstash products in v1.
- The desired v1 option surface includes:
  - database name
  - platform / cloud provider
  - primary region
  - read regions
  - plan
  - budget
  - eviction
- TLS, treated as required-on/read-only rather than safely mutable
- The v1 mutable provider settings are read regions, plan, budget, and eviction.
- The v1 create-time-only or fail-fast settings include platform, primary region, database name identity, and TLS disabled state.
- Literal platform, region, plan, and budget values are validated during AppHost model construction. Parameter-backed values preserve their source and must be validated after deploy-time resolution.
- Remote identity is deterministic by explicit database name. Cached provider ids are an optimization/diagnostic state only and must be name-verified before reuse.

### Testing Rules For This Repository
- Full test coverage is the goal.
- Every task that introduces or changes code must add or update tests in the same PR.
- All tests in this repository should use Reqnroll feature files and step definitions. Do not introduce a second testing style for product behavior.
- Each task should validate its own work before PR creation. Do not defer obvious missing coverage to later tasks unless the plan explicitly says that later task is the first valid place for it.
- Opt-in live-provider tests are allowed when `UPSTASH_EMAIL` and `UPSTASH_API_KEY` are present. Deterministic fake-provider coverage remains the default path for normal test runs.
- Live-provider scenarios must use the `@live-upstash` tag so the shared hook can skip without credentials and run registered cleanup actions.
- Any live Upstash test must leave the remote account in the same state it found it. Use isolated explicit database names where possible, always tear down databases created by the test, and restore any pre-existing resource settings the test changed before it exits.
- Live-provider tests must skip cleanly when the required environment variables are absent, and they must never rely on leftover remote state from prior runs.

### Plans As Source Of Truth
- Treat `plans/` as the execution board for this repository.
- The numbering expresses dependency stages and parallelism:
  - `0.x` tasks can run first
  - tasks with the same stage prefix may run in parallel if their dependencies allow it
  - later stages assume earlier blocking work is complete
- If implementation or investigation changes the actual required order, scope, or dependency graph, update the affected plan files in the same PR before it is created.
- Do not let the plan drift behind the code.

### Decisions As Source Of Truth
- Treat `decisions/` as the repository's home for durable decision records.
- Decision files should capture the accepted direction, important evidence, rejected alternatives, and any boundaries that future implementation must preserve.
- If later work changes or supersedes a recorded decision, update or add the affected decision record in the same PR.

### Documentation Sync Rule
- Keep the repository's documented state accurate at all times.
- Whenever a task changes behavior, structure, workflow, usage, constraints, validation, or roadmap reality, update the relevant documentation in the same PR.
- At minimum, consider whether the change requires updates to:
  - `README.md`
  - `AGENTS.md`
  - one or more files in `plans/`
  - one or more files in `decisions/`
  - `.diary/<branch>.md`
- A PR is not complete if the code, tests, `README.md`, `AGENTS.md`, and relevant plan files describe different realities.

### Task Execution Rule
- When working a plan task, read the specific file in `plans/` first and treat it as an executable brief.
- If a task includes investigation and that investigation changes assumptions, update the impacted task files before opening the PR.
- If a task discovers new required work, create or revise plan files in the same branch rather than leaving the follow-up only in PR prose.
- PR descriptions should explain the work in enough detail that the next agent can understand what changed, why it changed, what was tested, and whether any plan files were updated.

### Implementation Boundaries
- Prefer the smallest correct implementation that satisfies the current task and the locked product contract.
- Preserve normal Aspire Redis local behavior unless the task explicitly concerns deploy-time Upstash behavior.
- Keep application-facing Redis outputs separate from infrastructure-only Upstash management credentials.
- Do not add speculative features such as auto-delete, non-Redis Upstash products, or extra provider abstractions that are not required by the current plan.
- The `0.1` decision keeps Aspire's built-in `RedisResource` as the resource of record. Upstash intent is attached through resource annotations and Aspire `13.4.2` deploy pipeline steps, not through a wrapper resource or local-run behavior.
