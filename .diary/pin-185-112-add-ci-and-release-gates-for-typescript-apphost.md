## Rolling state
- Goal: Add durable CI/release gates for TypeScript AppHost support.
- Current plan: Implemented always-on NuGet-backed TypeScript package gate; split Reqnroll live checks behind manual workflow dispatch; documented gate taxonomy.
- Open questions/risks: GitHub workflow syntax was inspected manually; local YAML parser tools were unavailable.
- Next actions: None.
- Key paths: `.github/workflows/pr-validation.yml`, `.github/workflows/_run-tests.yml`, `.github/workflows/publish.yml`, `eng/Validate-TypeScriptAppHostPackage.ps1`, `docs/ci-release-gates.md`

## Session log
### 2026-06-09 07:26 Z (agent/pin-185-112-add-ci-and-release-gates-for-typescript-apphost)
- Add TypeScript CI/release gates [ci] (impact: med)
  - Why: Prevent package/export/docs/examples drift for TypeScript AppHost support.
  - Change: Added NuGet-backed fixture package gate, default non-live Reqnroll filter, manual `@live-upstash` workflow path, and release pre-publish gate. (files: `.github/workflows/*`, `eng/Validate-TypeScriptAppHostPackage.ps1`, `docs/ci-release-gates.md`, docs/test README)
  - Notes: Validated with package gate and filtered Reqnroll suite; Docker-backed local runtime scenario skipped locally.
