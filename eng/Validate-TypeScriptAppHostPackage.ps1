param(
    [string] $Configuration = "Release",
    [string] $PackageVersion = "9999.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "Aspire.Hosting.Upstash.Redis.slnx"
$fixtureSource = Join-Path $repoRoot "tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost"
$artifactsRoot = Join-Path $repoRoot "artifacts/typescript-apphost-package"
$packageOutput = Join-Path $artifactsRoot "packages"
$fixtureWork = Join-Path $artifactsRoot "fixture"
$nugetPackages = Join-Path $artifactsRoot ".nuget-packages"
$packageId = "PinguApps.Aspire.Hosting.Upstash.Redis"

Remove-Item $artifactsRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item $packageOutput -ItemType Directory -Force | Out-Null
New-Item $nugetPackages -ItemType Directory -Force | Out-Null

dotnet restore $solutionPath
dotnet build $solutionPath -c $Configuration --no-restore -p:ContinuousIntegrationBuild=true
dotnet pack $solutionPath -c $Configuration --no-build -p:Version=$PackageVersion -o $packageOutput

$packageFile = Join-Path $packageOutput "$packageId.$PackageVersion.nupkg"
$packageCacheId = $packageId.ToLowerInvariant()
$packageCachePath = Join-Path $nugetPackages "$packageCacheId/$PackageVersion"

Remove-Item $packageCachePath -Recurse -Force -ErrorAction SilentlyContinue
New-Item $packageCachePath -ItemType Directory -Force | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($packageFile, $packageCachePath)
Copy-Item $packageFile (Join-Path $packageCachePath "$packageCacheId.$PackageVersion.nupkg")

$packageBytes = [IO.File]::ReadAllBytes($packageFile)
$packageHash = [Convert]::ToBase64String([System.Security.Cryptography.SHA512]::HashData($packageBytes))

Set-Content (Join-Path $packageCachePath "$packageCacheId.$PackageVersion.nupkg.sha512") $packageHash -Encoding ASCII

[ordered]@{
    version = 2
    contentHash = $packageHash
    source = (Resolve-Path $packageOutput).Path
} | ConvertTo-Json | Set-Content (Join-Path $packageCachePath ".nupkg.metadata") -Encoding UTF8

Copy-Item $fixtureSource $fixtureWork -Recurse
Remove-Item (Join-Path $fixtureWork ".aspire") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $fixtureWork ".modules") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $fixtureWork "node_modules") -Recurse -Force -ErrorAction SilentlyContinue

$aspireConfigPath = Join-Path $fixtureWork "aspire.config.json"
$aspireConfig = Get-Content $aspireConfigPath -Raw | ConvertFrom-Json
$aspireConfig.packages.$packageId = $PackageVersion
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
  <packageSourceMapping>
    <packageSource key="local-package-gate">
      <package pattern="PinguApps.Aspire.Hosting.Upstash.Redis" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content (Join-Path $fixtureWork "NuGet.Config") -Encoding UTF8

Push-Location $fixtureWork
try {
    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nugetPackages

    aspire restore --non-interactive
    npm ci --no-audit --no-fund
    npm run typecheck
    aspire publish --non-interactive --list-steps
}
finally {
    if ($null -eq $previousNuGetPackages) {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }
    else {
        $env:NUGET_PACKAGES = $previousNuGetPackages
    }

    Pop-Location
}
