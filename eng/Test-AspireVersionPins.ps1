$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$pins = @()

function Add-Pin {
  param(
    [string] $Path,
    [string] $Name,
    [string] $Version
  )

  if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Missing Aspire version pin '$Name' in '$Path'."
  }

  $script:pins += [pscustomobject]@{
    Path = $Path
    Name = $Name
    Version = $Version
  }
}

function Add-RegexPin {
  param(
    [string] $Path,
    [string] $Name,
    [string] $Pattern
  )

  $fullPath = Join-Path $repoRoot $Path
  $content = Get-Content -Raw -Path $fullPath
  $matches = [regex]::Matches($content, $Pattern)

  if ($matches.Count -eq 0) {
    throw "Missing Aspire version pin '$Name' in '$Path'."
  }

  foreach ($match in $matches) {
    Add-Pin -Path $Path -Name $Name -Version $match.Groups["version"].Value
  }
}

$propsPath = Join-Path $repoRoot "Directory.Packages.props"
[xml] $props = Get-Content -Raw -Path $propsPath

$packageVersions = @{}
foreach ($packageVersion in $props.Project.ItemGroup.PackageVersion) {
  $packageVersions[$packageVersion.Include] = $packageVersion.Version
}

$baseline = $packageVersions["Aspire.Hosting"]
if ([string]::IsNullOrWhiteSpace($baseline)) {
  throw "Missing Aspire.Hosting baseline in Directory.Packages.props."
}

Add-Pin -Path "Directory.Packages.props" -Name "Aspire.Hosting.Redis" -Version $packageVersions["Aspire.Hosting.Redis"]

$typeScriptConfigPaths = @(
  "samples/TypeScriptAppHost/aspire.config.json",
  "tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost/aspire.config.json"
)

foreach ($path in $typeScriptConfigPaths) {
  $config = Get-Content -Raw -Path (Join-Path $repoRoot $path) | ConvertFrom-Json
  Add-Pin -Path $path -Name "sdk.version" -Version $config.sdk.version
  Add-Pin -Path $path -Name "packages.Aspire.Hosting.Redis" -Version $config.packages."Aspire.Hosting.Redis"
}

$workflowPaths = @(
  ".github/workflows/_run-tests.yml",
  ".github/workflows/pr-validation.yml",
  ".github/workflows/publish.yml"
)

foreach ($path in $workflowPaths) {
  Add-RegexPin -Path $path -Name "Aspire.Cli" -Pattern "dotnet tool install -g Aspire\.Cli --version (?<version>\S+)"
}

Add-RegexPin -Path "README.md" -Name "Tested Aspire baseline" -Pattern 'Tested Aspire baseline:\s*`(?<version>[^`]+)`'
Add-RegexPin -Path "README.md" -Name "Aspire.Hosting.Redis sample" -Pattern '"Aspire\.Hosting\.Redis"\s*:\s*"(?<version>[^"]+)"'
Add-RegexPin -Path "AGENTS.md" -Name "Target Aspire version" -Pattern 'Target Aspire version:\s*`(?<version>[^`]+)`'

$driftedPins = $pins | Where-Object { $_.Version -ne $baseline }

if ($driftedPins) {
  $details = $driftedPins |
    ForEach-Object { " - $($_.Path) [$($_.Name)] is $($_.Version)" }
  $detailsText = $details -join "`n"

  throw "Aspire version pin drift detected. Expected every pin to match Directory.Packages.props Aspire.Hosting version '$baseline'.`n$detailsText"
}

Write-Host "All Aspire version pins match $baseline."
