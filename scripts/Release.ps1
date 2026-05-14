#requires -Version 7.0

<#
.SYNOPSIS
    Local release script for Texnomic.Curl.Impersonate.

.DESCRIPTION
    Packs the solution, signs every nupkg with the certificate identified by
    -CertificateFingerprint (typically the Certum SimplySign cert exposed
    through the Windows certificate store by SimplySign Desktop), pushes the
    signed packages to NuGet.org, and creates a GitHub Release with the
    artefacts attached.

    Each step can be skipped independently with the -SkipX switches so the
    script doubles as a debugging tool. Use -DryRun to walk through the steps
    without performing any writes.

.PARAMETER Version
    The release version (e.g. 1.0.0, 1.2.3-rc.1). Must follow semver.

.PARAMETER CertificateFingerprint
    SHA-256 thumbprint of the code-signing certificate to use.
    Defaults to $env:SIGNING_CERT_FINGERPRINT.

.PARAMETER NuGetApiKey
    NuGet.org API key. Defaults to $env:NUGET_API_KEY.

.PARAMETER Timestamper
    RFC 3161 timestamp authority URL. Defaults to DigiCert's free service.

.PARAMETER NuGetSource
    NuGet feed URL. Defaults to the public NuGet.org v3 feed.

.PARAMETER SkipBuild
    Skip the build + test phase (use existing artefacts under artifacts/).

.PARAMETER SkipSign
    Skip the signing phase. Useful when iterating on the script itself.

.PARAMETER SkipPush
    Skip the push to NuGet.org.

.PARAMETER SkipGitHubRelease
    Skip the gh release create step.

.PARAMETER DryRun
    Print every shelled-out command but do not execute it.

.EXAMPLE
    pwsh .\scripts\Release.ps1 -Version 1.0.0

.EXAMPLE
    pwsh .\scripts\Release.ps1 -Version 1.0.1 -SkipGitHubRelease
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[\w\.]+)?$')]
    [string]$Version,

    [string]$CertificateFingerprint = $env:SIGNING_CERT_FINGERPRINT,

    [string]$NuGetApiKey = $env:NUGET_API_KEY,

    [string]$Timestamper = 'http://timestamp.digicert.com',

    [string]$NuGetSource = 'https://api.nuget.org/v3/index.json',

    [switch]$SkipBuild,

    [switch]$SkipSign,

    [switch]$SkipPush,

    [switch]$SkipGitHubRelease,

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# ----- helpers ---------------------------------------------------------------

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Native {
    param([string[]]$Command)
    Write-Host "    $($Command -join ' ')" -ForegroundColor DarkGray
    if ($DryRun) { return }
    & $Command[0] $Command[1..($Command.Length - 1)]
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $($Command -join ' ')"
    }
}

# ----- preflight -------------------------------------------------------------

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $RepoRoot

Write-Step "Release $Version from $RepoRoot"

# Working tree must be clean
$GitStatus = git status --porcelain
if ($GitStatus -and -not $DryRun) {
    throw "Working tree is not clean. Commit or stash before releasing:`n$GitStatus"
}

# Must be on main
$Branch = git rev-parse --abbrev-ref HEAD
if ($Branch -ne 'main' -and -not $DryRun) {
    throw "Releases must be cut from 'main'. Current branch: $Branch"
}

# Local main must be up-to-date
git fetch --quiet origin main
$Behind = git rev-list --count HEAD..origin/main
if ($Behind -ne '0' -and -not $DryRun) {
    throw "Local main is $Behind commits behind origin/main. Pull first."
}

# Tag must not already exist on the remote
$TagName = "v$Version"
$RemoteTag = git ls-remote --tags origin "refs/tags/$TagName"
if ($RemoteTag -and -not $DryRun) {
    throw "Tag $TagName already exists on origin. Pick a new version or delete the tag."
}

# ----- build + pack ----------------------------------------------------------

$ArtifactsDir = Join-Path $RepoRoot 'artifacts'

if (-not $SkipBuild) {
    Write-Step 'Cleaning artifacts/'
    if (Test-Path $ArtifactsDir) {
        Remove-Item $ArtifactsDir -Recurse -Force
    }

    Write-Step 'dotnet restore'
    Invoke-Native @('dotnet', 'restore', 'Texnomic.Curl.Impersonate.slnx')

    Write-Step "dotnet build -c Release /p:Version=$Version"
    Invoke-Native @('dotnet', 'build', 'Texnomic.Curl.Impersonate.slnx',
        '--configuration', 'Release', '--no-restore',
        "/p:Version=$Version")

    Write-Step 'dotnet test (excluding Category=E2E)'
    Invoke-Native @('dotnet', 'test', 'Texnomic.Curl.Impersonate.slnx',
        '--configuration', 'Release', '--no-build',
        '--filter', 'Category!=E2E')

    Write-Step "dotnet pack -c Release /p:Version=$Version"
    Invoke-Native @('dotnet', 'pack', 'Texnomic.Curl.Impersonate.slnx',
        '--configuration', 'Release', '--no-build',
        '--output', $ArtifactsDir,
        "/p:Version=$Version")
} else {
    Write-Step 'Skipping build phase (-SkipBuild)'
}

$NupkgPaths = Get-ChildItem -Path $ArtifactsDir -Filter '*.nupkg' -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '\.symbols\.' } |
    ForEach-Object { $_.FullName }

if (-not $NupkgPaths) {
    throw "No .nupkg files found under $ArtifactsDir"
}

Write-Host ''
Write-Host 'Packages ready:' -ForegroundColor Green
$NupkgPaths | ForEach-Object { Write-Host "  $_" }

# ----- sign ------------------------------------------------------------------

if (-not $SkipSign) {
    if (-not $CertificateFingerprint) {
        throw "CertificateFingerprint is required. Pass -CertificateFingerprint or set `$env:SIGNING_CERT_FINGERPRINT."
    }

    Write-Step "Signing $($NupkgPaths.Count) package(s) with cert $CertificateFingerprint"
    Write-Host '    (SimplySign Mobile may prompt you to approve each signing — keep your phone handy.)' -ForegroundColor Yellow

    foreach ($Nupkg in $NupkgPaths) {
        Invoke-Native @('dotnet', 'nuget', 'sign', $Nupkg,
            '--certificate-fingerprint', $CertificateFingerprint,
            '--timestamper', $Timestamper,
            '--overwrite')
    }

    Write-Step 'Verifying signatures'
    foreach ($Nupkg in $NupkgPaths) {
        Invoke-Native @('dotnet', 'nuget', 'verify', $Nupkg, '--all')
    }
} else {
    Write-Step 'Skipping signing phase (-SkipSign)'
}

# ----- push ------------------------------------------------------------------

if (-not $SkipPush) {
    if (-not $NuGetApiKey) {
        throw "NuGetApiKey is required. Pass -NuGetApiKey or set `$env:NUGET_API_KEY."
    }

    Write-Step "Pushing $($NupkgPaths.Count) package(s) to $NuGetSource"

    foreach ($Nupkg in $NupkgPaths) {
        Invoke-Native @('dotnet', 'nuget', 'push', $Nupkg,
            '--api-key', $NuGetApiKey,
            '--source', $NuGetSource,
            '--skip-duplicate')
    }
} else {
    Write-Step 'Skipping push phase (-SkipPush)'
}

# ----- tag + GitHub Release --------------------------------------------------

if (-not $SkipGitHubRelease) {
    Write-Step "Tagging and pushing $TagName"
    Invoke-Native @('git', 'tag', '-a', $TagName, '-m', "Release $TagName")
    Invoke-Native @('git', 'push', 'origin', $TagName)

    Write-Step "Creating GitHub Release $TagName"
    $GhArgs = @('release', 'create', $TagName,
        '--title', $TagName,
        '--generate-notes',
        '--latest')
    foreach ($Nupkg in $NupkgPaths) { $GhArgs += $Nupkg }
    Invoke-Native (@('gh') + $GhArgs)
} else {
    Write-Step 'Skipping GitHub Release phase (-SkipGitHubRelease)'
}

Write-Host ''
Write-Host "✔ Release $Version complete." -ForegroundColor Green
Write-Host "    NuGet:  https://www.nuget.org/packages/Texnomic.Curl.Impersonate/$Version"
Write-Host "    GitHub: https://github.com/Texnomic/Curl.Impersonate/releases/tag/$TagName"
