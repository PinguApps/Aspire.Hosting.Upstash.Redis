## Rolling state
- Goal: Update repo guidance so agents can run opt-in real Upstash validation with forwarded credentials while guaranteeing cleanup.
- Current plan: Keep AGENTS, README, and the live-test plan tasks aligned on credential gating and teardown rules.
- Open questions/risks: The actual live-test implementation still needs shared cleanup helpers and safe disposable naming once the test tasks are worked.
- Next actions: Implement the gated live-test harness in `1.2`; add real-provider ownership scenarios in `6.2`; add real-provider reconcile/output scenarios in `6.3`; enforce live validation cleanup reporting in `7.3`.
- Key paths: `AGENTS.md`, `README.md`, `plans/1.2-define-reqnroll-spec-matrix.md`, `plans/6.2-add-tests-for-ownership-modes.md` and `plans/6.3-add-tests-for-reconcile-failures-and-outputs.md`, `plans/7.3-run-full-validation-and-release-hardening.md`

## Session log
### 2026-06-06 14:13 Z (feature/forward-upstash-env)
- Add live Upstash test guidance [tests] (impact: med)
  - Why: Task agents can now receive `UPSTASH_EMAIL` and `UPSTASH_API_KEY`, so the repo needed an explicit contract for real-provider validation and mandatory cleanup.
  - Change: Updated repo guidance and plan tasks to require env-var gating plus teardown or state restoration for live tests. (files: AGENTS.md, README.md, plans/1.2-define-reqnroll-spec-matrix.md, plans/6.2-add-tests-for-ownership-modes.md, plans/6.3-add-tests-for-reconcile-failures-and-outputs.md, plans/7.3-run-full-validation-and-release-hardening.md)
  - Notes: No tests run; this change only updates documentation and future task requirements.
