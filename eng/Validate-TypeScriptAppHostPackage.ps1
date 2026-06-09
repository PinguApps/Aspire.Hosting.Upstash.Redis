param(
    [string] $Configuration = "Release",
    [string] $PackageVersion = "0.0.0-ci"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "Aspire.Hosting.Upstash.Redis.slnx"
$fixtureSource = Join-Path $repoRoot "tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost"
$artifactsRoot = Join-Path $repoRoot "artifacts/typescript-apphost-package"
$packageOutput = Join-Path $artifactsRoot "packages"
$fixtureWork = Join-Path $artifactsRoot "fixture"

Remove-Item $artifactsRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item $packageOutput -ItemType Directory -Force | Out-Null

dotnet restore $solutionPath
dotnet build $solutionPath -c $Configuration --no-restore -p:ContinuousIntegrationBuild=true
dotnet pack $solutionPath -c $Configuration --no-build -p:Version=$PackageVersion -o $packageOutput

Copy-Item $fixtureSource $fixtureWork -Recurse
Remove-Item (Join-Path $fixtureWork ".aspire") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $fixtureWork ".modules") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $fixtureWork "node_modules") -Recurse -Force -ErrorAction SilentlyContinue

$aspireConfigPath = Join-Path $fixtureWork "aspire.config.json"
$aspireConfig = Get-Content $aspireConfigPath -Raw | ConvertFrom-Json
$aspireConfig.packages."PinguApps.Aspire.Hosting.Upstash.Redis" = $PackageVersion
$aspireConfig | ConvertTo-Json -Depth 10 | Set-Content $aspireConfigPath -Encoding UTF8

$packageOutputFullPath = (Resolve-Path $packageOutput).Path
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-package-gate" value="$packageOutputFullPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $fixtureWork "NuGet.Config") -Encoding UTF8

Push-Location $fixtureWork
try {
    aspire restore --non-interactive
    npm ci --no-audit --no-fund
    npm run typecheck
    aspire publish --non-interactive --list-steps
}
finally {
    Pop-Location
}
