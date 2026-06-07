# Reqnroll Test Matrix

The test project uses Reqnroll feature files and step definitions for product behaviour. Keep new behaviour coverage in this structure rather than adding a second unit-test style.

## Feature Groups

- `Features/ApiShape`: public extension methods, option capture, annotations, and model-time validation.
- `Features/LocalBehavior`: guarantees that normal Aspire Redis local usage and references remain unchanged.
- `Features/OwnershipModes`: create-only, existing-only, and create-or-adopt deployment semantics.
- `Features/ProviderClient`: deterministic fake-provider behaviour plus opt-in live-provider patterns.
- `Features/DeployReconcileOutputs`: deploy auth, lookup, create, reconcile, failure, diagnostics, and app-facing outputs.
- `Features/DocsSamples`: README and sample snippets where they can be validated without brittle text tests.
- `Features/CodeStyle`: narrow source-style guards agreed during review.

Only the groups needed by the current skeleton have feature files today. Future tasks should add scenarios under the group that matches the behaviour they implement.

## Shared Support

- `UpstashRedisScenarioContext` is the per-scenario object model for Aspire builders, captured options, fake provider state, and live-provider cleanup.
- `AspireModelInspector` and `AspireModelAssertions` centralize annotation, connection-property, pipeline-step, and reference-chain checks.
- `FakeUpstashProvider` is the default Upstash simulation path. It is deterministic, in memory, and records provider interactions for assertions.
- `LiveUpstashTestSession` is the opt-in live-provider pattern. Live scenarios must use the `@live-upstash` tag, read only `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, and register every delete or restore action through the shared cleanup stack. Cleanup runs best-effort through the full stack and reports failures after all actions have been attempted.

## Scenario Map

- `1.1`: extend `Features/ApiShape` with the final overload and ownership-mode API matrix.
- `2.1`: covered in `Features/ApiShape` and `Features/LocalBehavior` for annotation/state-model details, explicit-setting snapshots, unchanged Redis connection properties, and the standard reference chain.
- `2.2`: covered by `Features/ProviderClient/ManagementClient.feature` for management auth, request paths and bodies, response parsing, credential contract failures, provider error classification, readiness polling, mutable operation endpoints, and cancellation.
- `2.3`: extend `Features/ApiShape` and `Features/DeployReconcileOutputs` for option/domain validation.
- `3.1`: covered in `Features/DeployReconcileOutputs` for deploy-time parameter resolution, missing required values, secret boundaries, and local model construction without deploy-only credentials.
- `3.2`: covered by `Features/OwnershipModes/OwnershipResolution.feature` for create-only, existing-only, create-or-adopt branching, lookup-by-name behavior, and ownership failure wording.
- `3.3`: covered in `Features/OwnershipModes/RemoteIdentity.feature` for first lookup, cached provider-id reuse, configured-name changes, duplicate names, detail/name drift, unsafe cached-id collisions, and Aspire deployment-state cache persistence.
- `4.1`: covered by `Features/DeployReconcileOutputs/CreateFlow.feature` for create-path request mapping, credential-bearing ready details, readiness polling, create failure wording, missing connection-field provider-contract failures for create/adopt, and adopt no-op behavior.
- `4.2`: covered in `Features/DeployReconcileOutputs/ReconcileMutableSettings.feature` for no-op reconcile, fixed-plan equivalence, deterministic read-regions/plan/budget/eviction update order, explicit-setting-only enforcement, provider mutation failures, and convergence failures.
- `4.3`: covered in `Features/DeployReconcileOutputs/ImmutableDrift.feature` for database-name identity, detectable platform, explicit primary-region, TLS-disabled failures, actionable wording, and mutable settings staying out of immutable drift.
- `5.1`: covered in `Features/DeployReconcileOutputs/RedisConnectionOutput.feature` for the standard Redis connection string, deploy-time connection-property overrides, local pre-deploy no-op behavior, secret boundaries, and endpoint slug rejection.
- `5.3`: covered in `Features/DeployReconcileOutputs/DeploymentDiagnostics.feature` for deploy progress phase order, actionable failure context, secret redaction, and provider id visibility.
- `5.2`: covered in `Features/DeployReconcileOutputs/SupplementaryOutputs.feature` for deployed supplementary output values, stable names, secret classification, and management-key exclusion.
- `6.1`: covered in `Features/LocalBehavior/RedisReference.feature` and `Features/ApiShape/PublishToUpstash.feature` for plain `AddRedis` no-op behavior, local pre-deploy Upstash no-op behavior, standard Redis references, overload consistency, same-builder fluent chaining, and management-secret exclusion from app-facing Redis outputs/references.
- `6.2`: hardens the already populated ownership, reconcile, and output groups rather than duplicating shallow coverage elsewhere.
- `6.3`: covered in `Features/DeployReconcileOutputs` for per-setting mutable reconciliation, TLS non-mutation, adopted missing-credential, output missing-password failures, no automatic `reset-password` on missing credentials, secret redaction, and opt-in `@live-upstash` disposable deploy/repeat-deploy/output scenarios. Live 6.3 scenarios generate isolated database names and register provider deletion by configured name through the shared cleanup stack before deployment runs.
- `7.1` and `7.2`: add `Features/DocsSamples` scenarios for README and sample snippets where practical.
