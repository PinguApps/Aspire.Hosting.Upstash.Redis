# Testing

Product behaviour is covered by the Reqnroll suite under [`tests/Aspire.Hosting.Upstash.Redis/`](../tests/Aspire.Hosting.Upstash.Redis/).

## Test Taxonomy

- `Features/ApiShape` covers public API shape, including TypeScript generated surface expectations.
- `Features/LocalBehavior` covers unchanged local Redis behaviour.
- `Features/DeployReconcileOutputs` covers deploy-time resolution, create flow, reconciliation, drift, diagnostics, and output redirection.
- `Features/OwnershipModes` covers ownership resolution and repeated identity behaviour.
- `Features/DocsSamples` covers compile-validated sample snippets.
- `Features/ProviderClient` covers fake and live Upstash management client behaviour.

Do not add a second product behaviour test style for this package.

## TypeScript Fixture

The validated TypeScript fixture lives at [`tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/`](../tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/).

It restores generated Aspire modules from the current repository package, type-checks the generated SDK surface, starts locally when host dependencies are available, and validates publish step listing.

The sample under [`samples/TypeScriptAppHost/`](../samples/TypeScriptAppHost/) mirrors the same API shape for maintainer demos.

## Live Tests

Live-provider scenarios must use `@live-upstash`, skip cleanly without `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, and avoid leaving remote account changes behind.
