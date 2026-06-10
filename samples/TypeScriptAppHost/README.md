# Minimal TypeScript AppHost Demo

This sample demonstrates the supported TypeScript AppHost shape for `PinguApps.Aspire.Hosting.Upstash.Redis`.

Run from this directory:

```powershell
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
aspire start --non-interactive --isolated
aspire wait cache --status healthy --timeout 120 --non-interactive
aspire stop --non-interactive
```

For a live non-interactive deploy, provide real Aspire parameter environment variables:

```powershell
$env:Parameters__upstash_database_name = "upstash-ts-demo"
$env:Parameters__upstash_account_email = $env:UPSTASH_EMAIL
$env:Parameters__upstash_api_key = $env:UPSTASH_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

`aspire.config.json` references the in-repo package project so the generated TypeScript module matches this checkout. Generated `.aspire/` content is intentionally ignored.
