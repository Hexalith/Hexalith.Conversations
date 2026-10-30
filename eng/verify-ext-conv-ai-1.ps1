[CmdletBinding()]
param(
    [ValidateSet('Local', 'Live')][string]$Mode = 'Local',
    [string]$ArtifactsPath = '/tmp/hexalith-agents54-conversations-artifacts',
    [string]$LiveManifest
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = Split-Path -Parent $PSScriptRoot
$requiredLanes = @('Membership', 'Posting', 'Facilitator', 'ActiveCount', 'CurrentReads', 'ApprovedDeletionDelivery', 'CrossTenant')

function Invoke-Checked([string[]]$Arguments, [string]$Log) {
    & dotnet @Arguments *> $Log
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): dotnet $($Arguments -join ' '). Evidence: $Log" }
}
function Read-PassingTests([string]$XmlPath, [string]$ExpectedClass = '') {
    if (-not (Test-Path -LiteralPath $XmlPath)) { throw "Missing executed-test evidence: $XmlPath" }
    [xml]$document = Get-Content -LiteralPath $XmlPath -Raw
    $tests = @($document.SelectNodes('//test'))
    if ($tests.Count -eq 0 -or @($tests | Where-Object { $_.result -ne 'Pass' }).Count -ne 0) {
        throw "Required tests were empty, failed or skipped: $XmlPath"
    }
    if ($ExpectedClass -and @($tests | Where-Object { $_.type -ne $ExpectedClass }).Count -ne 0) {
        throw "Executed class differed from the accepted exact class: $XmlPath"
    }
    foreach ($assembly in $document.SelectNodes('//assembly')) {
        if ([int]$assembly.errors -ne 0 -or [int]$assembly.failed -ne 0 -or [int]$assembly.skipped -ne 0 -or [int]$assembly.'not-run' -ne 0) {
            throw "Required assembly did not complete every test: $XmlPath"
        }
    }
    return $tests
}
function Assert-Hash([string]$Path, [string]$Expected) {
    if (-not $Path -or -not (Test-Path -LiteralPath $Path) -or $Expected -notmatch '^[a-fA-F0-9]{64}$' -or
        (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected) {
        throw 'Missing or changed exact accepted artifact.'
    }
}

if ($Mode -eq 'Live') {
    & (Join-Path $PSScriptRoot 'verify-ext-conv-ai-1-live.ps1') -ArtifactsPath $ArtifactsPath -LiveManifest $LiveManifest
    return
}

New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null
# Keep each execution's logs, XML and build graph separate from prior runs and sibling builds.
$ArtifactsPath = Join-Path $ArtifactsPath ('run-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ'))
New-Item -ItemType Directory -Path $ArtifactsPath | Out-Null
Write-Output "Execution evidence: $ArtifactsPath"
# The Platform/Builds producer owns dependency roots, versions and source/package mode.
# Inherit its complete materialized environment; never reconstruct a sibling layout here.
$mappingEnvironment = @{}
foreach ($name in @('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps',
        'CustomBeforeDirectoryBuildTargets', 'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets')) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrWhiteSpace($value) -or -not (Test-Path -LiteralPath $value -PathType Leaf)) {
        throw "Local verification requires the complete producer-selected source mapping environment: missing $name. See module/README.md."
    }
    $mappingEnvironment[$name] = $value
}
$mappingEnvironment['HEXALITH_SOURCE_MAPPING_HASH'] = [Environment]::GetEnvironmentVariable('HEXALITH_SOURCE_MAPPING_HASH')
$mappingEnvironment['Configuration'] = [Environment]::GetEnvironmentVariable('Configuration')
if ($mappingEnvironment['HEXALITH_SOURCE_MAPPING_HASH'] -cnotmatch '^[0-9a-f]{64}$' -or $mappingEnvironment['Configuration'] -cne 'Debug') {
    throw 'Local verification requires a producer-selected source/Debug mapping with its exact SHA-256.'
}
$properties = @('-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0')
$mappingLog = Join-Path $ArtifactsPath 'source-mapping-evaluation.json'
$mappingProject = Join-Path $repository 'src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj'
Invoke-Checked (@('msbuild', $mappingProject,
    '-getProperty:HexalithSourceMappingMode,UseHexalithProjectReferences,HexalithSourceMappingHash,HexalithSourceMappingApplies,Configuration') + $properties) $mappingLog
$selection = (Get-Content -LiteralPath $mappingLog -Raw | ConvertFrom-Json -AsHashtable).Properties
if ($selection['HexalithSourceMappingMode'] -cne 'source' -or $selection['UseHexalithProjectReferences'] -cne 'true' -or
    $selection['HexalithSourceMappingApplies'] -cne 'true' -or $selection['Configuration'] -cne 'Debug' -or
    $selection['HexalithSourceMappingHash'] -cne $mappingEnvironment['HEXALITH_SOURCE_MAPPING_HASH']) {
    throw 'Local verification refused an absent, changed or non-source producer mapping.'
}
$mappingEnvironment | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'source-mapping-environment.json') -Encoding utf8
$projects = @('Hexalith.Conversations.Contracts.Tests', 'Hexalith.Conversations.Tests', 'Hexalith.Conversations.Server.Tests', 'Hexalith.Conversations.Client.Tests', 'Hexalith.Conversations.DeletionConformance.Tests')
foreach ($name in $projects) {
    $project = Join-Path $repository "tests/$name/$name.csproj"
    Invoke-Checked (@('build', $project, '-c', 'Debug', '--artifacts-path', $ArtifactsPath, '-m:1') + $properties) (Join-Path $ArtifactsPath "$name-build.log")
}
$class = 'Hexalith.Conversations.Server.Tests.Agents.ConversationAgentSixSeamTests'
$assembly = Join-Path $ArtifactsPath 'bin/Hexalith.Conversations.Server.Tests/debug/Hexalith.Conversations.Server.Tests.dll'
$xml = Join-Path $ArtifactsPath 'local-six-seam.xml'
Invoke-Checked @($assembly, '-class', $class, '-result-xml', $xml) (Join-Path $ArtifactsPath 'local-six-seam.log')
$executed = Read-PassingTests $xml $class
$methods = @{
    Membership = 'MembershipRetryRemovalAndSerializedReplay'
    Posting = 'DeterministicPostingLostAcknowledgementAndConcurrentIntent'
    Facilitator = 'FacilitatorRosterAndCurrentEditedDeletedRedactedContent'
    ActiveCount = 'CompleteTenantActiveCountIncludesZeroAgentCallsAndMissingCatalogueIsUnavailable'
    CurrentReads = 'FacilitatorRosterAndCurrentEditedDeletedRedactedContent'
    ApprovedDeletionDelivery = 'ApprovedDeletionPublicationDeliveryBackfillRolloverAndPoison'
    CrossTenant = 'CrossTenantUnrelatedPartyAndRevokedAuthorityFailBeforeDisclosure'
}
$lanes = @()
foreach ($name in $requiredLanes) {
    $test = @($executed | Where-Object { $_.method -eq $methods[$name] -and $_.result -eq 'Pass' })
    if ($test.Count -ne 1) { throw "Required Local lane $name did not execute exactly once." }
    $lanes += @{ Name = $name; Result = 'Pass'; ExecutedClass = $class; ExecutedMethod = $methods[$name]; Xml = $xml }
    Write-Output "Local $name passed (serialized local simulation)."
}
foreach ($name in $projects) {
    # Original documentation tests walk AppContext.BaseDirectory to the source checkout.
    # Stage exact built bytes under its ignored artifacts folder, without rebuilding or altering those tests.
    $builtDirectory = Join-Path $ArtifactsPath "bin/$name/debug"
    $runDirectory = Join-Path $repository ".artifacts/ext-conv-ai-1-tests/$name/debug/run"
    New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
    Copy-Item -Path (Join-Path $builtDirectory '*') -Destination $runDirectory -Recurse -Force
    $assembly = Join-Path $runDirectory "$name.dll"
    Assert-Hash $assembly (Get-FileHash -LiteralPath (Join-Path $builtDirectory "$name.dll") -Algorithm SHA256).Hash
    $xml = Join-Path $ArtifactsPath "$name.xml"
    Invoke-Checked @($assembly, '-result-xml', $xml) (Join-Path $ArtifactsPath "$name.log")
    $null = Read-PassingTests $xml
}
$evidence = @{ Mode = 'Local'; EvidenceKind = 'SerializedLocalSimulation'; LiveReady = $false;
    SourceMappingHash = $mappingEnvironment['HEXALITH_SOURCE_MAPPING_HASH'];
    SourceRevision = (& git -C $repository rev-parse HEAD).Trim(); Lanes = $lanes;
    RemainingGates = @('Replacement immutable target and exact command acceptance by Agents owner', 'Real current authority and immutable Party',
        'Independently qualified SDK complete command-source attestation and actor compare/append', 'Installed complete authenticated tenant catalogue under the accepted creation window',
        'Independent deletion approval with authentic policy/source-bound logical-deletion audit evidence',
        'Qualified runtime publication discovery/backfill/pump binding and actual dedicated service-Party enrollment',
        'Authenticated receiver, independent remote receipt lookup and accepted exact target', 'Live storage/restart/replica/race evidence') }
$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'local-evidence.json') -Encoding utf8
Write-Output 'Local verification passed. This is not live readiness or dependency availability.'
