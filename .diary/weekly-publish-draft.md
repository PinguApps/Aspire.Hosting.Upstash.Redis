## Rolling state
- Goal: Add a manual and weekly publisher for existing release drafts when main has advanced since the last published release.
- Current plan: Open PR and converge through CI and review.
- Open questions/risks: Publishing with GITHUB_TOKEN does not trigger the release event workflow; the new workflow calls package publishing directly.
- Next actions: Push implementation, open PR, action feedback, complete final audit.
- Key paths: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`

## Session log
### 2026-09-28 00:33 +01:00 (agent/weekly-publish-draft)
- Add weekly release draft publisher [build] (impact: med)
  - Why: Publish the Release Drafter draft only when main has commits after the last published tag.
  - Change: Added Sunday 09:17 UTC/manual workflow, draft-only PATCH, and reusable package publishing for GITHUB_TOKEN runs (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Read-only API check selected published `v1.1.0` and draft `v1.1.1`; Actionlint passed with the existing custom runner label ignored.
