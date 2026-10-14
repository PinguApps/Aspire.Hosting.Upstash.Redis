# AGENTS.md
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
- Keep this section concise and accurate for the current released state of the repository.

### Repository Overview
- This repository contains the released `PinguApps.Aspire.Hosting.Upstash.Redis` package.
- The package lets an Aspire AppHost opt a normal `RedisResource` into Upstash Redis during `aspire deploy`.
- Consumer usage starts from standard Aspire Redis, such as `builder.AddRedis("cache")`, then adds `.PublishToUpstash(...)`.
- Local development should continue to behave like normal Aspire Redis. Upstash behavior is deploy-only and opt-in.

### Current Product Contract
- This package is Upstash Redis only.
- Remote identity is the explicit Upstash database name.
- Supported ownership modes are `CreateOnly`, `ExistingOnly`, and `CreateOrAdopt`.
- Management authentication uses Upstash account email plus Management API key and is infrastructure-only.
- Application-facing outputs expose Redis connection details, never the Upstash Management API key.
- Repeated deploys must target the same intended remote database and only reconcile settings the caller explicitly set.
- Deployment must fail clearly on unsafe drift or unreconcilable explicit settings.
- The package must not auto-delete remote Upstash databases.

### Key Paths
- `src/Aspire.Hosting.Upstash.Redis/` contains the package source.
- `src/Aspire.Hosting.Upstash.Redis/Management/` contains the typed Upstash management client layer.
- `src/Aspire.Hosting.Upstash.Redis/Deployment/` contains deploy-time resolution, ownership, create, reconcile, drift, and diagnostics logic.
- `tests/Aspire.Hosting.Upstash.Redis/` contains the full Reqnroll-based test suite.
- `tests/Aspire.Hosting.Upstash.Redis/README.md` explains the feature taxonomy and test support patterns.
- `samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs` is the compile-validated sample source used by docs tests.
- `README.md` is the consumer-facing package guide and should stay aligned with shipped behavior.

### Technical Baseline
- Target framework: `.NET 10`.
- Target Aspire version: `13.5.2`.
- Keep Aspire's built-in `RedisResource` as the resource of record.
- Preserve normal local Redis behavior unless the work is explicitly about deploy-time Upstash behavior.
- Keep app-facing Redis outputs separate from infrastructure-only management credentials.
- TLS is required-on/read-only for v1.
- Mutable provider settings are read regions, plan, budget, and eviction.
- Database identity, platform, primary region, and TLS disabled state are create-time or fail-fast checks.

### Testing And Docs
- Any behavior change must update or add Reqnroll coverage in `tests/Aspire.Hosting.Upstash.Redis/`.
- Do not introduce a second testing style for product behavior.
- Live-provider scenarios must use `@live-upstash`, skip cleanly without `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, and leave the remote account unchanged after the run.
- Keep `README.md`, `AGENTS.md`, samples, and tests in sync with the actual shipped behavior.
