# TypeScript AppHost Acceptance Plan

This document defines what full TypeScript AppHost support means for this repository. It is the finish line for later implementation, test, sample, and documentation tickets.

TypeScript support is complete only when a maintainer can validate the same deploy-only Upstash Redis behaviour from a TypeScript AppHost that is already covered for .NET AppHosts:

- local AppHost runs keep using normal Aspire Redis behaviour
- `publishToUpstash` records deploy-time intent without calling Upstash at model/build time
- `aspire deploy` resolves parameters, creates or adopts the named Upstash Redis database, reconciles only explicit mutable settings, fails on unsafe drift, and redirects app-facing Redis outputs
- management credentials remain infrastructure-only
- repeated deploys target the same configured remote database name
- docs, snippets, fixtures, and demos stay aligned through automated checks where practical

## Automated Validation Matrix

| Area | Required scenarios | Normal CI | Gated or opt-in | Manual only |
| --- | --- | --- | --- | --- |
| Model/build-time validation | A TypeScript AppHost can import the generated Aspire module, call the Upstash Redis API, build the model, and represent the same annotations/options as the equivalent .NET AppHost. Invalid literal values fail before deploy where TypeScript can express literals. Parameter-backed values remain deploy-time values. | Yes. Exercise through the Reqnroll suite using a checked-in TypeScript fixture and deterministic assertions over the generated model or generated artifacts. | No. | No. |
| Generated SDK validation | The generated TypeScript surface exposes the expected `publishToUpstash` shape, ownership modes, option names, output helpers, and supported value constants without inventing a separate npm package story. | Yes. Reqnroll scenarios should compile/type-check the fixture against the generated module output produced by the current package. | No. | No. |
| Local AppHost run validation | A TypeScript AppHost with `addRedis(...).publishToUpstash(...)` starts locally without Upstash credentials being required by the local application path. Standard Redis references keep working as a normal Aspire Redis resource. | Yes, if it can run without containers or external services in the existing CI environment by validating the model and local run contract. | If local runtime dependencies require containers or host services unavailable in CI, run through an explicit opt-in job. | No. |
| Publish validation | `aspire publish` for the TypeScript AppHost emits deployment artifacts where the Redis resource is treated as deploy-time Upstash intent and no fallback cloud compute Redis resource is published. | Yes for deterministic artifact inspection. | No. | No. |
| Deploy validation with fake provider | Deployment pipeline behaviour from the TypeScript fixture covers create-only, existing-only, create-or-adopt, repeated deploy identity reuse, mutable reconciliation, immutable drift failures, output redirection, and secret redaction against the deterministic fake provider. | Yes. Product behaviour remains in Reqnroll and reuses the existing fake-provider/support model. | No. | No. |
| Live-provider validation | A disposable TypeScript AppHost deploy can create/adopt a uniquely named Upstash database, repeat deploy against the same remote identity, verify app-facing Redis outputs, and clean up without leaving account changes behind. | No. | Yes. Tag with `@live-upstash`, require `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, generate isolated names, and use the shared cleanup stack. | No, except for exploratory provider troubleshooting after a gated failure. |
| Docs/snippet validation | README and docs TypeScript snippets compile or are generated from compile-validated sample source. The minimal .NET and TypeScript examples stay aligned with the fixture/sample contract. | Yes. Add Reqnroll `DocsSamples` coverage rather than brittle prose assertions where possible. | No. | No. |
| Demo validation | In-repo sample assets build/type-check and can be used by the documented demo commands. Demo source is covered by the same snippet or fixture validation used by docs. | Yes for compile/type-check and deterministic publish checks. | Yes for live deploy demo flow. | No. |
| Human acceptance walkthrough | A maintainer starts from a fresh TypeScript AppHost and follows the docs end to end through local run, publish, live deploy, remote verification, and friction notes. | No. | No. | Yes. This is the final release walkthrough, not a substitute for automated coverage. |

Normal CI must stay practical: it should avoid real Upstash calls, avoid account mutation, avoid relying on a published NuGet package, and run with the rest of the Reqnroll product suite. Gated checks are only for live-provider or host-runtime requirements that cannot be made deterministic in normal CI.

## TypeScript Fixture Strategy

TypeScript fixtures should live under `tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/`. The test project owns these files because they are product-validation fixtures, not consumer samples.

The fixture should include:

- a minimal TypeScript AppHost using standard Aspire Redis plus `publishToUpstash`
- a variant with explicit optional settings for platform, primary region, read regions, plan, budget, eviction, and TLS-on intent
- a small consumer resource reference so standard Redis connection output can be inspected
- TypeScript package metadata only as needed to run the Aspire AppHost and generated module checks

Package consumption should use both paths, for different purposes:

- normal CI uses a project/package reference to the current repository output so tests validate the working tree directly
- a gated packaging check packs the NuGet package and verifies the TypeScript fixture can consume that `.nupkg`, catching packaging and generated-module layout issues before release

Generated `.aspire/modules/` output is not committed. Tests should regenerate it from the current package and inspect or type-check the generated output. If a stable expected shape is needed, assert targeted facts such as exported names and callable signatures instead of snapshotting the whole generated directory.

Product behaviour must remain in the Reqnroll suite. TypeScript support should add feature files under the existing groups where the behaviour belongs:

- `Features/ApiShape` for generated TypeScript API shape and option capture
- `Features/LocalBehavior` for unchanged local Redis behaviour
- `Features/DeployReconcileOutputs` for publish/deploy outputs, drift, reconciliation, and diagnostics
- `Features/OwnershipModes` for TypeScript fixture coverage of ownership semantics
- `Features/DocsSamples` for README/docs/sample drift checks

Do not add a separate Jest, Vitest, or npm-centred product behaviour suite. Node or TypeScript commands may be invoked by Reqnroll step definitions as fixture build tools, but Reqnroll remains the assertion surface.

## Documentation Information Architecture

`README.md` should become the front door, not the full manual. It must keep:

- package metadata, badges, and one-paragraph summary
- install command
- minimal .NET example
- minimal TypeScript example
- concise behaviour summary covering deploy-only intent, standard Redis local behaviour, and infrastructure-only management credentials
- links to deeper docs

Target `docs/` pages:

- `docs/getting-started-dotnet.md`: .NET AppHost setup, parameters, minimal deploy path, and common ownership choices.
- `docs/getting-started-typescript.md`: TypeScript AppHost setup, generated module expectations, parameters/secrets, local run, publish, and deploy.
- `docs/configuration.md`: ownership modes, required inputs, optional settings, supported literal values, mutable vs immutable settings, TLS contract.
- `docs/deployment-behaviour.md`: deploy-time resolution, create/adopt flow, repeated deploy identity, drift handling, output redirection, diagnostics, and no auto-delete guarantee.
- `docs/outputs-and-security.md`: standard Redis connection output, supplementary Upstash outputs, secret classification, and management credential boundary.
- `docs/samples-and-demos.md`: in-repo samples, demo commands, live-provider prerequisites, cleanup expectations, and drift checks.
- `docs/testing.md`: Reqnroll test taxonomy, TypeScript fixture topology, normal CI vs gated live-provider coverage, and how to add scenarios.

The README should link directly to the relevant page at each decision point. It should not duplicate the full settings tables once those pages exist; keep only enough in README for a new user to choose the right deeper page.

## Demo Definition

The feature is fully demoable when the repository contains enough assets for a maintainer to show TypeScript AppHost support without writing new code during the demo.

Minimum in-repo sample assets:

- `samples/TypeScriptAppHost/` with a minimal TypeScript AppHost using `addRedis(...).publishToUpstash(...)`
- a tiny consumer app or container resource that receives the standard Redis reference
- sample parameter/secret setup instructions aligned with `docs/getting-started-typescript.md`
- a publish/deploy command path that uses the current package rather than an unrelated published version

A maintainer should be able to run:

- a local TypeScript AppHost run showing normal Redis behaviour without Upstash management credentials reaching the app-facing path
- a publish command showing the Redis resource is represented as deploy-time Upstash intent, not fallback Redis cloud compute
- an opt-in live deploy using `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, creating or adopting a uniquely named database and exposing standard Redis outputs

Demo drift is caught by:

- Reqnroll fixture scenarios compiling/type-checking the sample or sample-derived snippet
- docs sample coverage for commands and code snippets that can be validated deterministically
- gated live-provider scenarios for the deploy path
- avoiding checked-in generated `.aspire/modules/` output

## Human Acceptance Checklist

Use this checklist in the final walkthrough ticket.

1. Start from a clean checkout of the repository and confirm normal tests pass without Upstash credentials.
2. Create a fresh TypeScript AppHost using the documented Aspire command path.
3. Add the current repository package to that AppHost using the documented local package or project-reference path.
4. Add a normal Redis resource and opt it into Upstash Redis with `publishToUpstash`.
5. Configure parameters for `upstash-database-name`, `upstash-account-email`, and secret `upstash-api-key`.
6. Add explicit create settings for platform and primary region, plus at least one mutable setting such as plan or eviction.
7. Run the AppHost locally and verify the local path behaves like standard Aspire Redis.
8. Confirm local application-facing configuration does not expose the Upstash Management API key.
9. Run the documented publish command and inspect the artifacts for deploy-time Upstash intent and no fallback Redis cloud compute resource.
10. Set `UPSTASH_EMAIL` and `UPSTASH_API_KEY` for a disposable Upstash account/database name.
11. Deploy with `CreateOrAdopt` and verify a new remote Upstash Redis database is created when missing.
12. Verify the deployed app-facing Redis connection details point at the Upstash endpoint and classify the Redis password as secret.
13. Deploy again with the same configured database name and verify the deployment reuses the same remote identity.
14. Change one explicit mutable setting and verify the next deploy reconciles only that setting.
15. Attempt an unsafe immutable drift case, such as changing the primary region for an existing database, and verify the deployment fails clearly.
16. Clean up the disposable remote database.
17. Follow the README links into the TypeScript docs and record any missing command, unclear prerequisite, or mismatch between docs, sample, and observed behaviour.
