#requires -Version 7.0
[CmdletBinding()]
param(
    [string] $Browser = 'chromium',
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repoRoot 'tests\Hexalith.Conversations.Admin.Web.Tests\Hexalith.Conversations.Admin.Web.Tests.csproj'

if (-not (Test-Path $project)) {
    throw "Admin Web E2E project not found at $project. Run from the repository root."
}

if (-not $SkipBuild) {
    Write-Host 'Building Admin Web E2E project to materialize the Playwright runtime...' -ForegroundColor Cyan
    & dotnet build $project --configuration Debug --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'Build failed; cannot install browsers without the runtime.'
    }
}

$buildOutput = Join-Path $repoRoot 'tests\Hexalith.Conversations.Admin.Web.Tests\bin\Debug'
$installer = Get-ChildItem -Path $buildOutput -Recurse -Filter 'playwright.ps1' -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $installer) {
    throw "Could not locate playwright.ps1 under $buildOutput. Re-run after a successful build, or omit -SkipBuild."
}

Write-Host "Installing Playwright browser: $Browser" -ForegroundColor Cyan
& pwsh -NoProfile -File $installer.FullName install $Browser
exit $LASTEXITCODE
