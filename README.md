# Aspire.Hosting.Upstash.Redis

This repository is building an Aspire hosting integration for publishing standard Aspire Redis resources to Upstash Redis during deployment.

Current state: planning/investigation only. Product code is not implemented yet.

The Upstash management capability matrix is documented in [`plans/0.2-confirm-upstash-management-capability-matrix.md`](plans/0.2-confirm-upstash-management-capability-matrix.md). Key v1 decisions from that investigation:

- Management authentication uses separate native Upstash account email and Management API key values.
- Third-party marketplace Upstash accounts are not supported by the Developer API and should fail fast with a tailored error.
- Remote lookup uses list-by-account plus explicit database-name matching, with provider id preferred after discovery when safe state is available.
- Create supports database name, platform, primary region, read regions, plan, budget, and eviction.
- Reconcile supports read regions, plan, budget, and eviction only.
- TLS is treated as required-on/read-only for v1, not as a mutable setting.
- Password reset, rename, delete, team move, backups, autoscaling, prod pack, ACL, and private networking are intentionally out of scope for v1.
- App-facing outputs come from credential-bearing database detail responses and must never expose the Upstash Management API key.
