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
$workspace = Split-Path -Parent $repository
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

# Full live execution cannot be enabled by caller-authored acceptance/provider metadata.
# Candidate C4 discovery, ordered backfill/pump and catalogue source exist, but independently
# qualified runtime bindings, worker enrollment and receiver lookup are not installed.
if ($Mode -eq 'Live') {
    throw 'Live gate closed before any calls: candidate discovery/backfill/pump/catalogue source is not independently qualified or installed with current authority, worker enrollment and an authenticated exact receiver acknowledgement. Production SDK source attestation/compare-append and approval bindings remain unestablished. No caller manifest can override incomplete installation or qualification. Execution requires complete installed behavior and actual Available owner records, or an explicitly accepted all-owner qualification cohort.'
}

New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null
# Keep each execution's logs, XML and build graph separate from prior runs and sibling builds.
$ArtifactsPath = Join-Path $ArtifactsPath ('run-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ'))
New-Item -ItemType Directory -Path $ArtifactsPath | Out-Null
Write-Output "Execution evidence: $ArtifactsPath"
$properties = @('-p:UseHexalithProjectReferences=true', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0')
foreach ($pair in @(@('HexalithEventStoreRoot', 'eventstore'), @('HexalithCommonsRoot', 'commons'), @('HexalithTenantsRoot', 'tenants'))) {
    $source = Join-Path $workspace $pair[1]
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing local source reference: $source" }
    $properties += "-p:$($pair[0])=$source"
}
# Tenants forwards its package pin as Version on EventStore project references. Resolve the
# selected EventStore source's own version once and use it throughout this local graph, so
# different central package observations cannot produce two versions in one output directory.
$eventStoreProject = Join-Path $workspace 'eventstore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj'
$versionArguments = @('msbuild', $eventStoreProject, '-getProperty:HexalithEventStoreVersion', '-p:Configuration=Debug') + $properties
$eventStoreVersion = (& dotnet @versionArguments 2> (Join-Path $ArtifactsPath 'eventstore-version.stderr.log') | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $eventStoreVersion -notmatch '\A\d+\.\d+\.\d+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z') {
    throw 'Cannot resolve the selected EventStore source version for a consistent local build graph.'
}
$eventStoreVersion | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'eventstore-version.log')
$properties += "-p:HexalithEventStoreVersion=$eventStoreVersion"
$projects = @('Hexalith.Conversations.Contracts.Tests', 'Hexalith.Conversations.Tests', 'Hexalith.Conversations.Server.Tests', 'Hexalith.Conversations.Client.Tests')
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
    SourceRevision = (& git -C $repository rev-parse HEAD).Trim(); Lanes = $lanes;
    RemainingGates = @('Owner full target/date/command acceptance', 'Real current authority and immutable Party',
        'Independently qualified SDK complete command-source attestation and actor compare/append', 'Installed complete authenticated tenant catalogue under the accepted creation window',
        'Independent deletion approval with authentic policy/source-bound logical-deletion audit evidence',
        'Qualified runtime publication discovery/backfill/pump binding and actual dedicated service-Party enrollment',
        'Authenticated receiver, independent remote receipt lookup and accepted exact target', 'Live storage/restart/replica/race evidence') }
$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'local-evidence.json') -Encoding utf8
Write-Output 'Local verification passed. This is not live readiness or dependency availability.'
