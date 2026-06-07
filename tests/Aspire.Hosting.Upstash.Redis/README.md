# Reqnroll Test Matrix

The test project uses Reqnroll feature files and step definitions for product behaviour. Keep new behaviour coverage in this structure rather than adding a second unit-test style.

## Feature Groups

- `Features/ApiShape`: public extension methods, option capture, annotations, and model-time validation.
- `Features/LocalBehavior`: guarantees that normal Aspire Redis local usage and references remain unchanged.
- `Features/OwnershipModes`: create-only, existing-only, and create-or-adopt deployment semantics.
- `Features/ProviderClient`: deterministic fake-provider behaviour plus opt-in live-provider patterns.
- `Features/DeployReconcileOutputs`: deploy auth, lookup, create, reconcile, failure, diagnostics, and app-facing outputs.
- `Features/DocsSamples`: README and sample snippets where they can be validated without brittle text tests.

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
- `3.1`: extend `Features/DeployReconcileOutputs` for deploy-time parameter resolution and secret boundaries.
- `3.2`: covered by `Features/OwnershipModes/OwnershipResolution.feature` for create-only, existing-only, create-or-adopt branching, lookup-by-name behavior, and ownership failure wording.
- `3.3`: extend `Features/OwnershipModes` scenarios for stable remote identity.
- `4.1`, `4.2`, and `4.3`: extend `Features/DeployReconcileOutputs` for create, reconcile, and immutable drift failures.
- `5.1`, `5.2`, and `5.3`: extend `Features/DeployReconcileOutputs` for Redis outputs, supplementary outputs, progress, and diagnostics.
- `6.1`, `6.2`, and `6.3`: harden the already populated groups rather than duplicating shallow coverage elsewhere.
- `7.1` and `7.2`: add `Features/DocsSamples` scenarios for README and sample snippets where practical.
