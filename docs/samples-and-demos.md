# Samples And Demos

## C# Sample

The compile-validated C# sample source is [`samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs`](../samples/AppHostSnippets/UpstashRedisAppHostSnippets.cs).

It covers:

- `CreateOrAdopt`
- `CreateOnly`
- `ExistingOnly`
- parameter-backed optional settings
- supplementary output consumption

The test suite compiles these snippets through the test project so the docs do not drift from the public API.

## TypeScript Demo

The minimal TypeScript demo is [`samples/TypeScriptAppHost/`](../samples/TypeScriptAppHost/).

Useful commands from that directory:

```powershell
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
aspire start --non-interactive --isolated
aspire wait cache --status healthy --timeout 120 --non-interactive
aspire stop --non-interactive
```

`aspire restore` writes the generated SDK under `.aspire/modules/`. AppHost code should import from `./.aspire/modules/aspire.mjs`. Do not commit generated `.aspire/` content.

The demo uses local `Parameters` values in `aspire.config.json` for deterministic restore, type-checking, and local run commands. Non-interactive deploys should use real Aspire parameter environment variables and a disposable database name.

## Live Provider Expectations

Live-provider runs should:

- use a disposable database name
- set `Parameters__upstash_account_email` from `UPSTASH_EMAIL`
- set secret `Parameters__upstash_api_key` from `UPSTASH_API_KEY`
- verify repeated deploys reuse the same configured database name
- leave the remote account unchanged after cleanup

The package itself never auto-deletes remote databases.
