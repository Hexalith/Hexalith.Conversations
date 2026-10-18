[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ArtifactsPath,
    [string]$LiveManifest
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($LiveManifest)) { $LiveManifest = $env:EXT_CONV_AI_1_LIVE_MANIFEST }
$repository = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null
$runPath = Join-Path $ArtifactsPath ('live-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ'))
New-Item -ItemType Directory -Path $runPath | Out-Null
$evidence = [ordered]@{ Mode = 'Live'; Result = 'BLOCKED'; EvidenceKind = 'AuthenticatedPersistedRuntime';
    TargetCommit = $null; ManifestPath = $LiveManifest; Gates = @(); Lanes = @() }

function Save-Evidence {
    $evidence | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $runPath 'live-evidence.json') -Encoding utf8
}
function Block([string]$code, [string]$reason) {
    $evidence.Gates += @{ Code = $code; Result = 'BLOCKED'; Reason = $reason }
    Save-Evidence
    throw "EXT-CONV-AI-1 Live BLOCKED [$code]: $reason. Evidence: $runPath/live-evidence.json"
}
function Fail([string]$code, [string]$reason) {
    $evidence.Result = 'FAIL'
    $evidence.Gates += @{ Code = $code; Result = 'FAIL'; Reason = $reason }
    Save-Evidence
    throw "EXT-CONV-AI-1 Live FAIL [$code]: $reason. Evidence: $runPath/live-evidence.json"
}
function Require-Text($value, [string]$name) {
    if ($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value) -or $value -eq 'Pending') {
        Block 'RUNTIME_REFERENCE_MISSING' "Missing exact $name."
    }
    return $value
}
function Assert-Live([bool]$condition, [string]$code, [string]$reason) {
    if (-not $condition) { Fail $code $reason }
}
function Payload($response) {
    if ($response.Body -is [System.Collections.IDictionary] -and $response.Body.Contains('success')) {
        Assert-Live ($response.Body.success -eq $true -and $null -ne $response.Body.payload) 'SOURCE_QUERY_UNAVAILABLE' "Authenticated query $($response.Name) did not return a complete payload."
        return $response.Body.payload
    }
    return $response.Body
}
function Assert-Outcome($response, [string]$expected) {
    $value = Payload $response
    $names = @('Available', 'Absent', 'ConversationDeleted', 'PrincipalRemovedFromConversation',
        'Denied', 'Unavailable', 'Conflict', 'Quarantined', 'Invalid')
    $actual = if ($value -is [System.Collections.IDictionary]) { $value.outcome } else { $null }
    if ($actual -is [int] -or $actual -is [long]) {
        $actual = if ($actual -ge 0 -and $actual -lt $names.Count) { $names[[int]$actual] } else { $null }
    }
    Assert-Live ($actual -eq $expected) 'TYPED_OUTCOME_MISMATCH' "Operation $($response.Name) did not return $expected."
    return $value
}
function Same-Signal($left, $right) {
    return $null -ne $left -and $null -ne $right -and
        $left.conversationDeletionSignalId -eq $right.conversationDeletionSignalId -and
        $left.sourceRevision -eq $right.sourceRevision -and $left.sourceStream -eq $right.sourceStream -and
        $left.tenantId -eq $right.tenantId -and $left.conversationId -eq $right.conversationId -and
        $left.approvalReference -eq $right.approvalReference -and $left.sourceContractVersion -eq $right.sourceContractVersion
}
function Same-Receipt($left, $right) {
    return $null -ne $left -and $null -ne $right -and $left.signalId -eq $right.signalId -and
        $left.sourceRevision -eq $right.sourceRevision -and
        $left.protectedDeletionRevision -eq $right.protectedDeletionRevision -and
        $left.targetVersion -eq $right.targetVersion -and $left.evidence -eq $right.evidence
}

if (-not $LiveManifest -or -not (Test-Path -LiteralPath $LiveManifest -PathType Leaf)) {
    Block 'LIVE_MANIFEST_MISSING' 'An exact installed runtime manifest is required before any Live call.'
}
try { $manifest = Get-Content -LiteralPath $LiveManifest -Raw | ConvertFrom-Json -AsHashtable }
catch { Block 'LIVE_MANIFEST_INVALID' 'The installed runtime manifest is not valid JSON.' }
Assert-Live ($manifest.schemaVersion -eq 1) 'LIVE_MANIFEST_INVALID' 'The runtime manifest schema version must be 1.'
$head = (& git -C $repository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { Block 'TARGET_UNAVAILABLE' 'Cannot resolve the immutable source target.' }
$evidence.TargetCommit = $head
$clean = & git -C $repository status --porcelain
if ($LASTEXITCODE -ne 0 -or $clean) { Block 'TARGET_NOT_CLEAN' 'The exact source target has uncommitted changes.' }
$agents = Join-Path (Split-Path -Parent $repository) 'agents'
$registerLines = & git -C $agents show 'HEAD:_bmad-output/planning-artifacts/external-dependency-register.md' 2> $null
if ($LASTEXITCODE -ne 0 -or -not $registerLines) {
    Block 'OWNER_RECORD_UNAVAILABLE' 'The committed Agents owner acceptance record cannot be read.'
}
$register = $registerLines -join "`n"
$recordStart = $register.IndexOf('### EXT-CONV-AI-1 ', [StringComparison]::Ordinal)
$recordEnd = if ($recordStart -ge 0) { $register.IndexOf('### ', $recordStart + 4, [StringComparison]::Ordinal) } else { -1 }
if ($recordStart -lt 0 -or $recordEnd -lt 0) {
    Block 'OWNER_RECORD_UNAVAILABLE' 'The exact accepted Conversations record cannot be isolated.'
}
$record = $register.Substring($recordStart, $recordEnd - $recordStart)
$acceptedTarget = [Regex]::Match($record, '(?m)^\| `TargetVersionOrCommit` \| `([0-9a-f]{40})` \|\s*$')
$acceptedStatus = [Regex]::Match($record, '(?m)^\| `AcceptedStatus` \| `(Committed|Available)` \|\s*$')
if (-not $acceptedTarget.Success -or -not $acceptedStatus.Success -or $acceptedTarget.Groups[1].Value -ne $head) {
    Block 'OWNER_TARGET_NOT_ACCEPTED' 'The committed Agents register has not accepted this exact immutable Conversations target for owner qualification.'
}
Assert-Live ((Require-Text $manifest.targetCommit 'targetCommit') -eq $head) 'TARGET_MISMATCH' 'The installed target differs from the clean source commit.'
foreach ($name in @('tenantId', 'foreignTenantId', 'servicePartyId', 'workerPrincipalId', 'messageId',
    'namespaceInstallationId', 'receiverTargetVersion', 'storageIdentity', 'zeroAgentConversationId',
    'createdFromInclusive', 'createdToExclusive')) {
    $null = Require-Text $manifest[$name] $name
}
Assert-Live ($manifest.tenantId -ne $manifest.foreignTenantId) 'SCOPE_INVALID' 'Cross-tenant fixture must use a different tenant.'
Assert-Live ($manifest.workerPrincipalId -ne $manifest.servicePartyId) 'WORKER_IDENTITY_INVALID' 'Worker principal and immutable Party must be distinct identities.'
$bases = $manifest.bases
$credentials = $manifest.credentialReferences
$operations = $manifest.operations
Assert-Live ($bases -is [System.Collections.IDictionary] -and $credentials -is [System.Collections.IDictionary] -and
    $operations -is [System.Collections.IDictionary]) 'LIVE_MANIFEST_INVALID' 'Bases, credential references and operations are required.'
foreach ($name in @('gateway', 'parties', 'receiver', 'control', 'observer')) {
    $raw = Require-Text $bases[$name] "bases.$name"
    $uri = $null
    if (-not [Uri]::TryCreate($raw, [UriKind]::Absolute, [ref]$uri) -or
        ($uri.Scheme -ne 'https' -and -not ($uri.Scheme -eq 'http' -and $uri.IsLoopback))) {
        Block 'TARGET_URI_INVALID' "Base $name must use HTTPS or loopback HTTP."
    }
    $bases[$name] = $uri.AbsoluteUri.TrimEnd('/') + '/'
}
Assert-Live ($bases.parties -ne $bases.gateway -and $bases.receiver -ne $bases.gateway) 'INDEPENDENT_TARGET_MISSING' 'Parties and receiver must have independent target origins.'
foreach ($name in @('service', 'human', 'approver', 'worker', 'party', 'receiver', 'control', 'observer')) {
    $reference = Require-Text $credentials[$name] "credentialReferences.$name"
    if ($reference -notmatch '^env:[A-Za-z_][A-Za-z0-9_]*$') { Block 'CREDENTIAL_REFERENCE_INVALID' "Credential $name must be an environment reference." }
    $variable = $reference.Substring(4)
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($variable))) {
        Block 'CREDENTIAL_UNAVAILABLE' "Credential reference $name is not installed."
    }
}
Assert-Live ($credentials.service -ne $credentials.approver -and $credentials.service -ne $credentials.worker -and
    $credentials.service -ne $credentials.human -and $credentials.service -ne $credentials.party -and
    $credentials.approver -ne $credentials.worker) 'ROLE_SEPARATION_MISSING' 'Service, human, approval, Parties and worker credentials must be distinct.'

$required = @('identity', 'party', 'c1Membership', 'c1ParticipantReadAfterFirst', 'c1MembershipRetry',
    'c1ParticipantRead', 'c1Post', 'c1MessageReadAfterFirst', 'c1PostRetry', 'c1MessageRead',
    'c1RosterRead', 'c1Catalogue', 'c1ZeroAgentSource', 'c1Count', 'c1CurrentRead', 'c1Edit', 'c1EditedRead',
    'c1Delete', 'c1DeletedRead', 'c2Approve', 'c2SourceBefore',
    'c2Dispatch', 'c2SourceAfter', 'c2ReceiverLookup', 'c3Approve', 'c3SourcePending', 'c3ReceiverOutage',
    'c3SourceOutage', 'c3Restart', 'c3SourceRestart', 'c3TargetRollover', 'c3Retry', 'c3SourceAcknowledged',
    'c3ReceiverLookup', 'c4CrossTenant', 'c4Revoked', 'c4DeletedRead', 'c4RemovedRead',
    'c4PoisonSourceBefore', 'c4PoisonInject',
    'c4PoisonSourceAfter', 'c4ReceiverLookup')
$manifestDirectory = Split-Path -Parent (Resolve-Path -LiteralPath $LiveManifest).Path
foreach ($name in $required) {
    $operation = $operations[$name]
    Assert-Live ($operation -is [System.Collections.IDictionary]) 'LIVE_OPERATION_MISSING' "Operation $name is required."
    $baseName = Require-Text $operation.base "operations.$name.base"
    $credentialName = Require-Text $operation.credential "operations.$name.credential"
    Assert-Live ($bases.Contains($baseName) -and $credentials.Contains($credentialName)) 'LIVE_OPERATION_INVALID' "Operation $name has no installed target or credential reference."
    Assert-Live ($operation.method -in @('GET', 'POST') -and $operation.path -is [string] -and
        $operation.path -match '^[A-Za-z0-9/_?&=.%~-]+$' -and -not $operation.path.StartsWith('/')) 'LIVE_OPERATION_INVALID' "Operation $name has an invalid exact HTTP request."
    if ($operation.method -eq 'POST') {
        $bodyFile = Require-Text $operation.bodyFile "operations.$name.bodyFile"
        $full = [IO.Path]::GetFullPath((Join-Path $manifestDirectory $bodyFile))
        Assert-Live ($full.StartsWith($manifestDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal) -and
            (Test-Path -LiteralPath $full -PathType Leaf)) 'LIVE_BODY_MISSING' "Operation $name requires a contained request body."
        $operation.bodyFile = $full
    }
}

function Invoke-Live([string]$name) {
    $operation = $operations[$name]
    $tokenName = $credentials[$operation.credential].Substring(4)
    $token = [Environment]::GetEnvironmentVariable($tokenName)
    $uri = [Uri]::new([Uri]$bases[$operation.base], $operation.path)
    $parameters = @{ Uri = $uri; Method = $operation.method; Headers = @{ Authorization = "Bearer $token" };
        TimeoutSec = 30; SkipHttpErrorCheck = $true; ErrorAction = 'Stop' }
    if ($operation.method -eq 'POST') {
        $parameters.ContentType = 'application/json'
        $parameters.Body = Get-Content -LiteralPath $operation.bodyFile -Raw
    }
    try { $response = Invoke-WebRequest @parameters }
    catch { Block 'LIVE_TRANSPORT_UNAVAILABLE' "Authenticated operation $name could not reach its installed target." }
    $body = $null
    try { $body = $response.Content | ConvertFrom-Json -AsHashtable }
    catch { Block 'LIVE_RESPONSE_INVALID' "Operation $name returned invalid JSON." }
    $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($response.Content)))
    $evidence.Gates += @{ Code = $name; Result = 'OBSERVED'; Status = [int]$response.StatusCode;
        ResponseSha256 = $digest; Target = $operation.base }
    return @{ Name = $name; Status = [int]$response.StatusCode; Body = $body }
}
function Require-Success($response) {
    Assert-Live ($response.Status -ge 200 -and $response.Status -lt 300) 'LIVE_HTTP_FAILURE' "Operation $($response.Name) did not succeed."
    return (Payload $response)
}
function Record-Lane([string]$name, $facts) {
    $evidence.Lanes += @{ Name = $name; Result = 'PASS'; Facts = $facts }
    Save-Evidence
}

$identity = Require-Success (Invoke-Live 'identity')
Assert-Live ($identity.commit -eq $head -and $identity.tenantId -eq $manifest.tenantId -and
    $identity.namespaceInstallationId -eq $manifest.namespaceInstallationId -and
    $identity.receiverTargetVersion -eq $manifest.receiverTargetVersion -and
    $identity.storageIdentity -eq $manifest.storageIdentity -and -not [string]::IsNullOrWhiteSpace($identity.authorityRevision)) 'INSTALLED_TARGET_MISMATCH' 'Authenticated runtime identity did not match the exact clean target and installed source, authority, receiver and storage.'
$party = Require-Success (Invoke-Live 'party')
Assert-Live ($party.tenantId -eq $manifest.tenantId -and $party.partyId -eq $manifest.servicePartyId -and
    $party.partyType -eq 'Organization' -and $party.active -eq $true -and
    -not [string]::IsNullOrWhiteSpace($party.identityRevision)) 'PARTY_IDENTITY_UNVERIFIED' 'Independent Parties identity-by-id did not prove the active Branch B Organization Party.'

try {
    $membership = Assert-Outcome (Invoke-Live 'c1Membership') 'Available'
    $memberFirst = Assert-Outcome (Invoke-Live 'c1ParticipantReadAfterFirst') 'Available'
    $memberRetry = Assert-Outcome (Invoke-Live 'c1MembershipRetry') 'Available'
    $participant = Assert-Outcome (Invoke-Live 'c1ParticipantRead') 'Available'
    $post = Assert-Outcome (Invoke-Live 'c1Post') 'Available'
    $messageFirst = Assert-Outcome (Invoke-Live 'c1MessageReadAfterFirst') 'Available'
    $postRetry = Assert-Outcome (Invoke-Live 'c1PostRetry') 'Available'
    $message = Assert-Outcome (Invoke-Live 'c1MessageRead') 'Available'
    $roster = Assert-Outcome (Invoke-Live 'c1RosterRead') 'Available'
    $catalogue = Require-Success (Invoke-Live 'c1Catalogue')
    $zeroSource = Require-Success (Invoke-Live 'c1ZeroAgentSource')
    $count = Assert-Outcome (Invoke-Live 'c1Count') 'Available'
    $current = Assert-Outcome (Invoke-Live 'c1CurrentRead') 'Available'
    $null = Assert-Outcome (Invoke-Live 'c1Edit') 'Available'
    $edited = Assert-Outcome (Invoke-Live 'c1EditedRead') 'Available'
    $null = Assert-Outcome (Invoke-Live 'c1Delete') 'Available'
    $deleted = Assert-Outcome (Invoke-Live 'c1DeletedRead') 'Available'
    $from = [DateTimeOffset]::Parse($manifest.createdFromInclusive, [Globalization.CultureInfo]::InvariantCulture)
    $to = [DateTimeOffset]::Parse($manifest.createdToExclusive, [Globalization.CultureInfo]::InvariantCulture)
    Assert-Live ($from.Offset -eq [TimeSpan]::Zero -and $to.Offset -eq [TimeSpan]::Zero -and $from -lt $to -and
        $catalogue.complete -eq $true -and $catalogue.tenantId -eq $manifest.tenantId -and
        -not [string]::IsNullOrWhiteSpace($catalogue.checkpoint)) 'C1_CATALOGUE_UNAVAILABLE' 'The authenticated complete tenant catalogue and UTC half-open window are required.'
    $eligible = @($catalogue.entries | Where-Object { $_.active -eq $true -and
        [DateTimeOffset]$_.createdAt -ge $from -and [DateTimeOffset]$_.createdAt -lt $to })
    $zero = @($eligible | Where-Object { $_.conversationId -eq $manifest.zeroAgentConversationId })
    Assert-Live ($participant.agentParticipantPresent -eq $true -and [long]$participant.sourceRevision -gt 0 -and
        $memberFirst.sourceRevision -eq $participant.sourceRevision -and
        @($participant.participants | Where-Object { $_.participantPartyId -eq $manifest.servicePartyId }).Count -eq 1 -and
        $messageFirst.sourceRevision -eq $message.sourceRevision -and
        $message.messages.Count -eq 1 -and $message.messages[0].messageId -eq $manifest.messageId -and
        $post.messageId -eq $manifest.messageId -and $postRetry.messageId -eq $manifest.messageId -and
        $message.messages[0].provenance.aiGenerated -eq $true -and
        -not [string]::IsNullOrWhiteSpace($message.messages[0].provenance.agentCallTraceReference) -and
        @($roster.participants | Where-Object { $_.participantRole -eq 'Facilitator' }).Count -gt 0 -and
        @($current.messages | Where-Object { $_.messageId -eq $manifest.messageId }).Count -eq 1 -and
        $edited.messages.Count -eq 1 -and $edited.messages[0].messageId -eq $manifest.messageId -and
        $edited.messages[0].provenance.humanEdited -eq $true -and
        -not [string]::IsNullOrWhiteSpace($edited.messages[0].provenance.editedByPartyId) -and
        -not [string]::IsNullOrWhiteSpace($edited.messages[0].provenance.agentCallTraceReference) -and
        $deleted.messages.Count -eq 1 -and $deleted.messages[0].messageId -eq $manifest.messageId -and
        $deleted.messages[0].deleted -eq $true -and $null -eq $deleted.messages[0].text -and
        $zero.Count -eq 1 -and $zeroSource.conversationId -eq $manifest.zeroAgentConversationId -and
        $zeroSource.tenantId -eq $manifest.tenantId -and $zeroSource.isCreated -eq $true -and
        $zeroSource.isDeleted -eq $false -and $zeroSource.agentCallCount -eq 0 -and
        [long]$zeroSource.sourceRevision -gt 0 -and [long]$count.count -eq $eligible.Count -and
        $count.createdFromInclusive -eq $manifest.createdFromInclusive -and
        $count.createdToExclusive -eq $manifest.createdToExclusive -and
        $count.catalogCheckpoint -eq $catalogue.checkpoint) 'C1_PERSISTENCE_MISMATCH' 'Membership, deterministic posting, current content, facilitator or source count was not independently persisted.'
    Record-Lane 'C1' @{ MemberSourceRevision = $participant.sourceRevision; MessageId = $manifest.messageId; CountCheckpoint = $count.catalogCheckpoint }

    $null = Assert-Outcome (Invoke-Live 'c2Approve') 'Available'
    $before = Assert-Outcome (Invoke-Live 'c2SourceBefore') 'Available'
    $null = Require-Success (Invoke-Live 'c2Dispatch')
    $after = Assert-Outcome (Invoke-Live 'c2SourceAfter') 'Available'
    $remote = Assert-Outcome (Invoke-Live 'c2ReceiverLookup') 'Available'
    Assert-Live ((Same-Signal $before.signal $after.signal) -and $after.signal.sourceRevision -gt 0 -and
        $null -eq $before.acknowledgement -and
        $after.acknowledgedSourceRevision -eq $after.signal.sourceRevision -and
        $after.acknowledgement.signalId -eq $after.signal.conversationDeletionSignalId -and
        $after.acknowledgement.sourceRevision -eq $after.signal.sourceRevision -and
        $after.acknowledgement.protectedDeletionRevision -gt 0 -and
        (Same-Receipt $remote.acknowledgement $after.acknowledgement) -and
        $after.acknowledgement.targetVersion -eq $manifest.receiverTargetVersion) 'C2_RECEIPT_MISMATCH' 'Checkpoint or authenticated independent receiver receipt did not bind the exact stable deletion signal.'
    Record-Lane 'C2' @{ SignalId = $after.signal.conversationDeletionSignalId; SourceRevision = $after.signal.sourceRevision; ProtectedDeletionRevision = $after.acknowledgement.protectedDeletionRevision }

    $null = Assert-Outcome (Invoke-Live 'c3Approve') 'Available'
    $pending = Assert-Outcome (Invoke-Live 'c3SourcePending') 'Available'
    $outageCall = Invoke-Live 'c3ReceiverOutage'
    $outage = Assert-Outcome (Invoke-Live 'c3SourceOutage') 'Available'
    $null = Require-Success (Invoke-Live 'c3Restart')
    $restarted = Assert-Outcome (Invoke-Live 'c3SourceRestart') 'Available'
    $null = Require-Success (Invoke-Live 'c3TargetRollover')
    $null = Require-Success (Invoke-Live 'c3Retry')
    $retried = Assert-Outcome (Invoke-Live 'c3SourceAcknowledged') 'Available'
    $retryReceipt = Assert-Outcome (Invoke-Live 'c3ReceiverLookup') 'Available'
    Assert-Live ($outageCall.Status -ge 500 -and $null -eq $pending.acknowledgement -and
        (Same-Signal $pending.signal $outage.signal) -and (Same-Signal $pending.signal $restarted.signal) -and
        (Same-Signal $pending.signal $retried.signal) -and $outage.acknowledgement -eq $null -and
        $restarted.acknowledgement -eq $null -and -not [string]::IsNullOrWhiteSpace($outage.lastAttemptId) -and
        $outage.lastAttemptId -eq $restarted.lastAttemptId -and
        $outage.targetVersion -eq $restarted.targetVersion -and
        $retried.targetVersion -ne $restarted.targetVersion -and
        $retried.acknowledgement.signalId -eq $pending.signal.conversationDeletionSignalId -and
        $retried.acknowledgedSourceRevision -eq $pending.signal.sourceRevision -and
        (Same-Receipt $retryReceipt.acknowledgement $retried.acknowledgement) -and
        $retried.acknowledgement.protectedDeletionRevision -gt 0 -and
        $retried.acknowledgement.targetVersion -ne $manifest.receiverTargetVersion) 'C3_RETRY_MISMATCH' 'Outage, restart, rollover and retry did not retain one source signal through independent acknowledgement.'
    Record-Lane 'C3' @{ SignalId = $pending.signal.conversationDeletionSignalId; SourceRevision = $pending.signal.sourceRevision; TargetVersion = $retried.acknowledgement.targetVersion }

    $foreign = Assert-Outcome (Invoke-Live 'c4CrossTenant') 'Denied'
    $revoked = Assert-Outcome (Invoke-Live 'c4Revoked') 'Denied'
    $deletedRead = Assert-Outcome (Invoke-Live 'c4DeletedRead') 'ConversationDeleted'
    $removedRead = Assert-Outcome (Invoke-Live 'c4RemovedRead') 'PrincipalRemovedFromConversation'
    $poisonBefore = Assert-Outcome (Invoke-Live 'c4PoisonSourceBefore') 'Available'
    $poison = Invoke-Live 'c4PoisonInject'
    $poisonAfter = Assert-Outcome (Invoke-Live 'c4PoisonSourceAfter') 'Quarantined'
    $poisonRemote = Assert-Outcome (Invoke-Live 'c4ReceiverLookup') 'Conflict'
    Assert-Live ($null -eq $foreign.messages -and $null -eq $revoked.messages -and
        $null -eq $deletedRead.messages -and $null -eq $removedRead.messages -and
        $poison.Status -eq 409 -and (Same-Signal $poisonBefore.signal $poisonAfter.signal) -and
        $poisonAfter.acknowledgedSourceRevision -eq $poisonBefore.acknowledgedSourceRevision -and
        $null -eq $poisonAfter.acknowledgement -and -not [string]::IsNullOrWhiteSpace($poisonAfter.poisonCode) -and
        $null -eq $poisonRemote.acknowledgement) 'C4_DENIAL_POISON_MISMATCH' 'Cross-tenant/revoked denial or retained poison checkpoint was not independently proved.'
    Record-Lane 'C4' @{ PoisonCode = $poisonAfter.poisonCode; SourceRevision = $poisonAfter.signal.sourceRevision }
}
catch {
    if ($evidence.Gates.Count -gt 0 -and $evidence.Gates[-1].Result -in @('BLOCKED', 'FAIL')) { throw }
    Block 'LIVE_LANE_FAILURE' 'A C1-C4 authenticated operation or assertion failed.'
}

$evidence.Result = 'PASS'
Save-Evidence
Write-Output "C1-C4 Live verification passed against $head. Evidence: $runPath/live-evidence.json"
