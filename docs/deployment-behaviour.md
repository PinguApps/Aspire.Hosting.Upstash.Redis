# Deployment Behaviour

`PublishToUpstash` and `publishToUpstash` are deploy-time integrations.

## Deploy Flow

During `aspire deploy`, the package:

1. Resolves AppHost parameters.
2. Looks up the configured Upstash database name.
3. Applies the selected ownership mode.
4. Creates the database when allowed and required.
5. Validates immutable settings such as platform and primary region.
6. Reconciles explicitly configured mutable settings.
7. Retrieves final Redis connection details.
8. Redirects the standard Aspire Redis connection output to Upstash.

## Repeated Deployments

The configured database name is the stable remote identity. Repeated deployments target that name.

The deployment pipeline can cache a provider database id, but it revalidates that cached identity against the configured name before reuse. This prevents stale state from silently adopting the wrong database.

## Local Behaviour

Local AppHost runs continue to behave like standard Aspire Redis. The package does not create, update, or delete Upstash databases during local model construction.

## Cloud Publishing

When Redis is published to Upstash, the Redis resource is excluded from cloud compute publishing. For Azure Container Apps, that means deployment should not create a fallback Redis container app for the `cache` resource.

## Failure Behaviour

Deployment fails clearly when:

- required create settings are missing for a create path
- a requested existing database cannot be found
- a create-only deployment collides with an unrelated existing database
- platform or primary region drift would be unsafe
- `tls` is explicitly set to `false`
- an explicit mutable setting cannot be reconciled

The package does not auto-delete remote Upstash databases.

## Diagnostics

Deployment diagnostics report progress through configuration resolution, lookup, drift validation, create, reconciliation, and output retrieval. Diagnostics redact management credentials, Redis passwords and tokens, and full Redis connection strings.
