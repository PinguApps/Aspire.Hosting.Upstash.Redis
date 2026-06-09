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

```bash
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire start --non-interactive --isolated
aspire publish --non-interactive --list-steps
```

`aspire restore` generates `.aspire/modules/aspire.mjs`. Do not commit generated `.aspire/` content.

The demo uses local parameter values in `aspire.config.json` for deterministic restore and type-checking. Live deployment should use real `UPSTASH_EMAIL` and `UPSTASH_API_KEY` values through your normal Aspire parameter flow and a disposable database name.

## Live Provider Expectations

Live-provider runs should:

- use a disposable database name
- set `upstash-account-email` from `UPSTASH_EMAIL`
- set secret `upstash-api-key` from `UPSTASH_API_KEY`
- verify repeated deploys reuse the same configured database name
- leave the remote account unchanged after cleanup

The package itself never auto-deletes remote databases.
