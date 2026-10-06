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

# Live preflight is complete before any test or service call. This proposal does not mint acceptance.
if ($Mode -eq 'Live') {
    if (-not $LiveManifest -or -not (Test-Path -LiteralPath $LiveManifest)) {
        throw 'Live gate closed: missing full owner acceptance, exact targets/commands and configured authenticated authority/Party/source-attestation/catalogue/approval/receiver providers. Local fixtures cannot qualify EXT-CONV-AI-1.'
    }
    $manifest = Get-Content -LiteralPath $LiveManifest -Raw | ConvertFrom-Json -AsHashtable
    foreach ($field in @('AcceptedStatus', 'TargetCommit', 'TargetIntegrationDate', 'FullSixSeamContractVersion', 'WindowSemanticsAccepted',
        'VerifierSha256', 'AcceptanceEvidencePath', 'AcceptanceEvidenceSha256', 'Prerequisites', 'Providers', 'Lanes')) {
        if (-not $manifest.ContainsKey($field)) { throw "Live gate closed: missing accepted $field." }
    }
    $acceptedDate = [DateTime]::MinValue
    if ($manifest.AcceptedStatus -notin @('Committed', 'Available') -or $manifest.FullSixSeamContractVersion -ne 1 -or
        $manifest.WindowSemanticsAccepted -ne $true -or $manifest.TargetCommit -notmatch '^[a-fA-F0-9]{40}$' -or
        -not [DateTime]::TryParse($manifest.TargetIntegrationDate, [ref]$acceptedDate)) {
        throw 'Live gate closed: the complete target/date/window/contract is not accepted.'
    }
    Assert-Hash $PSCommandPath $manifest.VerifierSha256
    Assert-Hash $manifest.AcceptanceEvidencePath $manifest.AcceptanceEvidenceSha256
    $actualCommit = (& git -C $repository rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $manifest.TargetCommit -or (& git -C $repository status --porcelain)) {
        throw 'Live gate closed: checkout differs from the accepted immutable full target.'
    }
    foreach ($id in @('EXT-PARTIES-1')) {
        if (-not $manifest.Prerequisites.ContainsKey($id) -or $manifest.Prerequisites[$id].AcceptedStatus -ne 'Available' -or
            -not $manifest.Prerequisites[$id].Target -or -not $manifest.Prerequisites[$id].VerificationCommand) {
            throw "Live gate closed: prerequisite $id has no Available exact accepted target/command."
        }
    }
    foreach ($name in @('CurrentAuthority', 'ImmutableOrganizationParty', 'AuthenticatedCommandSourceAttestation', 'CompleteTenantCatalogue',
        'IndependentDeletionApproval', 'AuthenticatedReceiver', 'ExactSourceAndReceiverTarget')) {
        if (-not $manifest.Providers.ContainsKey($name)) { throw "Live gate closed: missing production provider $name." }
        $provider = $manifest.Providers[$name]
        if ($provider.Configured -ne $true -or -not $provider.TypeName -or $provider.TypeName -match 'Unavailable|Fixture|Mock|Fake|NoOp|Deferred|InMemory') {
            throw "Live gate closed: $name is missing or substituted."
        }
        Assert-Hash $provider.AssemblyPath $provider.AssemblySha256
    }
    foreach ($name in $requiredLanes) {
        if (-not $manifest.Lanes.ContainsKey($name)) { throw "Live gate closed: missing required $name command." }
        $lane = $manifest.Lanes[$name]
        if (-not $lane.Class -or $lane.Class -match 'LocalFixture|ConversationAgentSixSeamTests|ConversationAgentClientTests' -or
            $lane.TargetCommit -ne $manifest.TargetCommit) { throw "Live gate closed: $name is not accepted exact-target live evidence." }
        Assert-Hash $lane.AssemblyPath $lane.AssemblySha256
    }
    New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null
    foreach ($name in $requiredLanes) {
        $lane = $manifest.Lanes[$name]
        $xml = Join-Path $ArtifactsPath "live-$name.xml"
        Invoke-Checked @($lane.AssemblyPath, '-class', $lane.Class, '-result-xml', $xml) (Join-Path $ArtifactsPath "live-$name.log")
        $null = Read-PassingTests $xml $lane.Class
    }
    Write-Output 'Live six-seam compatibility commands passed against the accepted full target. Register acceptance is a separate owner action.'
    exit 0
}

New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null
$properties = @('-p:UseHexalithProjectReferences=true', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0')
foreach ($pair in @(@('HexalithEventStoreRoot', 'eventstore'), @('HexalithCommonsRoot', 'commons'), @('HexalithTenantsRoot', 'tenants'))) {
    $source = Join-Path $workspace $pair[1]
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing local source reference: $source" }
    $properties += "-p:$($pair[0])=$source"
}
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
        'Authenticated SDK complete command-source attestation and actor compare/append', 'Complete authenticated tenant catalogue and accepted window',
        'Independent deletion approval', 'Authenticated receiver and accepted exact target', 'Live storage/restart/replica/race evidence') }
$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'local-evidence.json') -Encoding utf8
Write-Output 'Local verification passed. This is not live readiness or dependency availability.'
