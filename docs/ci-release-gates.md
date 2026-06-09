# CI and Release Gates

This repository validates TypeScript AppHost support through the NuGet package export path. The core integration is still published as `PinguApps.Aspire.Hosting.Upstash.Redis`; no hand-maintained npm SDK or npm publish workflow is part of the release path.

## Always-on Checks

Pull request validation runs:

- `TypeScript AppHost package gate`: builds the solution with analyzers enabled, packs the NuGet package, restores ATS-generated TypeScript modules from that local `.nupkg`, type-checks the fixture, and lists TypeScript AppHost publish steps.
- `Run test suite`: runs the Reqnroll product suite in Release configuration, excluding `@live-upstash` scenarios by default.

Release publishing runs the same NuGet-backed TypeScript AppHost package gate before pushing packages to NuGet.org. This catches package layout, ATS export, fixture type-check, publish-step, analyzer, and pack regressions before release.

## Credential-gated Checks

The `Pull Request Validation` workflow can be run manually with `run_live_upstash` enabled. That job runs only scenarios tagged `@live-upstash` and passes `UPSTASH_EMAIL` and `UPSTASH_API_KEY` from repository secrets.

Live scenarios must use isolated database names and the shared cleanup support. They are not part of normal PR validation because they can mutate a real Upstash account and depend on external provider availability.

## Manual Maintainer Checks

Manual release readiness is limited to the human TypeScript walkthrough in [`typescript-apphost-acceptance.md`](typescript-apphost-acceptance.md). It is used for final confidence and documentation friction notes, not as a replacement for the automated gates.

Do not add an npm publish step for the core integration unless a separate product decision changes the distribution model.
