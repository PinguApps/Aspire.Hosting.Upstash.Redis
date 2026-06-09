# Minimal TypeScript AppHost Demo

This sample demonstrates the supported TypeScript AppHost shape for `PinguApps.Aspire.Hosting.Upstash.Redis`.

Run from this directory:

```bash
aspire restore --non-interactive
npm install --no-audit --no-fund
npm run typecheck
aspire publish --non-interactive --list-steps
```

`aspire.config.json` references the in-repo package project so the generated TypeScript module matches this checkout. Generated `.aspire/` content is intentionally ignored.
