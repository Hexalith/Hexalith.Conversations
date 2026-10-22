# Pure preflight checks plus an exclusive evidence reservation. No runtime calls occur here.
function Get-OqCredential([string]$Reference, [switch]$AllowPublishedAssertion) {
    if ($Reference -cmatch '^env:([A-Za-z_][A-Za-z0-9_]*)$') {
        $value = [Environment]::GetEnvironmentVariable($Matches[1])
    }
    elseif ($AllowPublishedAssertion -and $Reference.StartsWith('file:', [StringComparison]::Ordinal) -and
        [IO.Path]::IsPathRooted($Reference.Substring(5))) {
        $file = Get-Item -LiteralPath $Reference.Substring(5) -Force -ErrorAction Stop
        if ($file -isnot [IO.FileInfo] -or $file.Length -gt 16384) { throw 'OQ_CREDENTIAL_UNAVAILABLE' }
        # The separately enrolled SDK provider publishes current assertions atomically; never mint one here.
        $value = [IO.File]::ReadAllText($file.FullName).Trim()
    }
    else { throw 'OQ_CREDENTIAL_REFERENCE_INVALID' }
    if ([string]::IsNullOrWhiteSpace($value)) { throw 'OQ_CREDENTIAL_UNAVAILABLE' }
    return $value
}

function Read-OqPayload($Body, [switch]$RequireSdk) {
    if ($Body -isnot [System.Collections.IDictionary]) { throw 'OQ_QUERY_INVALID' }
    if (-not $Body.Contains('success')) {
        if ($RequireSdk) { throw 'OQ_QUERY_INVALID' }
        return $Body
    }
    if ($Body.success -isnot [bool] -or -not $Body.success) { throw 'OQ_QUERY_UNAVAILABLE' }
    if ($Body.Contains('payload') -and $null -ne $Body.payload) {
        if ($RequireSdk -or $Body.payload -isnot [System.Collections.IDictionary] -or
            $Body.Contains('payloadBytes') -and -not [string]::IsNullOrEmpty($Body.payloadBytes)) { throw 'OQ_QUERY_INVALID' }
        return $Body.payload
    }
    try {
        $bytes = [Convert]::FromBase64String($Body.payloadBytes)
        $json = [Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        $decoded = $json | ConvertFrom-Json -AsHashtable -ErrorAction Stop
    }
    catch { throw 'OQ_QUERY_INVALID' }
    if ($decoded -isnot [System.Collections.IDictionary]) { throw 'OQ_QUERY_INVALID' }
    return $decoded
}

function Assert-OqNoLinks([string]$Path) {
    $cursor = Get-Item -LiteralPath $Path -Force
    while ($null -ne $cursor) {
        if ($cursor.LinkType) { throw 'OQ_FILE_LINK_FORBIDDEN' }
        $cursor = if ($cursor -is [IO.FileInfo]) { $cursor.Directory } else { $cursor.Parent }
    }
}

function Get-OqRecord([string]$Register, [string]$Id) {
    $pattern = '(?ms)^### ' + [Regex]::Escape($Id) + ' [^\r\n]*\r?\n(.*?)(?=^### |^## |\z)'
    $matches = [Regex]::Matches($Register, $pattern)
    if ($matches.Count -ne 1) { throw "OQ_RECORD_MISSING:$Id" }
    return $matches[0].Value
}

function Get-OqField([string]$Record, [string]$Field) {
    $matches = [Regex]::Matches($Record, '(?m)^\| `' + [Regex]::Escape($Field) + '` \| (.*?) \|\s*$')
    if ($matches.Count -ne 1) { throw "OQ_FIELD_MISSING:$Field" }
    return $matches[0].Groups[1].Value.Trim('`')
}

function Get-OqFile([string]$Root, [string]$Relative, [string]$Digest, [switch]$IncludeBytes) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or
        $Digest -cnotmatch '^[0-9a-f]{64}$') { throw 'OQ_FILE_BINDING_INVALID' }
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $full = [IO.Path]::GetFullPath((Join-Path $rootPath $Relative))
    if (-not $full.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal) -or
        -not (Test-Path -LiteralPath $full -PathType Leaf)) { throw 'OQ_FILE_MISSING_OR_ESCAPED' }
    # Resolve lexical containment AND reject link traversal, including directory links.
    Assert-OqNoLinks $full
    $bytes = [IO.File]::ReadAllBytes($full)
    if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant() -cne $Digest) {
        throw 'OQ_FILE_HASH_MISMATCH'
    }
    if ($IncludeBytes) { return @{ Path = $full; Bytes = $bytes } }
    return $full
}

function Get-OqQualification([string]$Register, [string]$ManifestPath, $Manifest, [string]$Head, [byte[]]$ManifestBytes) {
    $cohort = Get-OqRecord $Register 'OQ-5.4-1'
    if ($cohort -notmatch 'Accepted by the project owner' -or
        $cohort -notmatch 'contract-conformance receiver' -or $cohort -notmatch 'Level 4') {
        throw 'OQ_COHORT_NOT_ACCEPTED'
    }
    $targets = [ordered]@{}
    foreach ($id in @('EXT-PROTECTION-1', 'EXT-HOST-1', 'EXT-SECRETS-1')) {
        $record = Get-OqRecord $Register $id
        $status = Get-OqField $record 'AcceptedStatus'
        $target = Get-OqField $record 'TargetVersionOrCommit'
        if ($status -cnotin @('Committed', 'Available') -or $target -cnotmatch '^[0-9a-f]{40}$') {
            throw "OQ_DEPENDENCY_NOT_COMMITTED:$id"
        }
        $targets[$id] = $target
    }
    if ($targets['EXT-HOST-1'] -cne $targets['EXT-SECRETS-1']) { throw 'OQ_PLATFORM_TARGETS_DIFFER' }
    $own = Get-OqRecord $Register 'EXT-CONV-AI-1'
    try { $binding = (Get-OqField $own 'RuntimeQualificationBinding') | ConvertFrom-Json -AsHashtable -ErrorAction Stop }
    catch { throw 'OQ_ACCEPTED_BINDING_MISSING' }
    if ($binding -isnot [System.Collections.IDictionary] -or $binding.schemaVersion -ne 1 -or
        $binding.cohortId -cne 'OQ-5.4-1' -or $binding.manifestSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        $binding.files -isnot [System.Collections.IDictionary]) { throw 'OQ_ACCEPTED_BINDING_INVALID' }
    $root = Split-Path -Parent ([IO.Path]::GetFullPath($ManifestPath))
    $null = Get-OqFile $root (Split-Path -Leaf $ManifestPath) $binding.manifestSha256
    if ($null -eq $ManifestBytes -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($ManifestBytes)).ToLowerInvariant() -cne
        $binding.manifestSha256) { throw 'OQ_CONSUMED_MANIFEST_HASH_MISMATCH' }
    $required = @('configuration', 'plan', 'hostProfile', 'hostVerifierEvidence', 'sourceAnchor', 'partyAnchor',
        'approvalAnchor', 'workerAnchor', 'receiverAnchor', 'lookupAnchor', 'observerAnchor', 'controlAnchor')
    $paths = @{}
    foreach ($name in $required) {
        $item = $binding.files[$name]
        if ($item -isnot [System.Collections.IDictionary]) { throw "OQ_FILE_BINDING_MISSING:$name" }
        $paths[$name] = Get-OqFile $root $item.path $item.sha256 -IncludeBytes
    }
    if ($binding.files.Count -ne $required.Count) { throw 'OQ_FILE_SET_MISMATCH' }
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    $configuration = $utf8.GetString($paths.configuration.Bytes) | ConvertFrom-Json -AsHashtable
    $journalDirectory = $configuration.attemptJournalDirectory
    if ($journalDirectory -isnot [string] -or -not [IO.Path]::IsPathRooted($journalDirectory) -or
        [IO.Path]::GetFullPath($journalDirectory) -cne $journalDirectory) { throw 'OQ_JOURNAL_ROOT_NOT_PINNED' }
    foreach ($name in @('tenantId', 'servicePartyId', 'workerPrincipalId', 'namespaceInstallationId',
        'receiverPrincipalId', 'lookupPrincipalId', 'receiverStorageIdentity')) {
        if ($configuration[$name] -isnot [string] -or [string]::IsNullOrWhiteSpace($configuration[$name]) -or
            $configuration[$name] -cne $Manifest[$name]) { throw "OQ_CONFIGURATION_MISMATCH:$name" }
    }
    if ($configuration.receiverPrincipalId -ceq $configuration.lookupPrincipalId -or
        $configuration.receiverPrincipalId -ceq $configuration.workerPrincipalId -or
        $configuration.lookupPrincipalId -ceq $configuration.workerPrincipalId) { throw 'OQ_RECEIVER_ROLE_SEPARATION_MISSING' }
    $oqHostEvidence = $utf8.GetString($paths.hostVerifierEvidence.Bytes) | ConvertFrom-Json -AsHashtable
    if ($oqHostEvidence.targetCommit -cne $targets['EXT-HOST-1'] -or $oqHostEvidence.verificationMode -cne 'Live' -or
        $oqHostEvidence.passPath -isnot [bool] -or -not $oqHostEvidence.passPath -or
        $oqHostEvidence.profileSha256 -cne $binding.files.hostProfile.sha256) { throw 'OQ_HOST_PASS_PATH_NOT_PINNED' }
    $plan = $utf8.GetString($paths.plan.Bytes) | ConvertFrom-Json -AsHashtable
    if ($plan.schemaVersion -ne 1 -or $plan.bodyFiles -isnot [System.Collections.IDictionary]) { throw 'OQ_PLAN_INVALID' }
    foreach ($operation in $Manifest.operations.Values) {
        if ($operation.method -cnotin @('GET', 'POST')) { throw 'OQ_METHOD_INVALID' }
    }
    $bodyFiles = @($Manifest.operations.Values | Where-Object { $_.method -ceq 'POST' } |
        ForEach-Object { $_.bodyFile } | Sort-Object -Unique -CaseSensitive)
    if ($bodyFiles.Count -ne $plan.bodyFiles.Count) { throw 'OQ_PLAN_BODY_SET_MISMATCH' }
    $bodies = @{}
    foreach ($relative in $bodyFiles) {
        if (-not $plan.bodyFiles.Contains($relative)) { throw 'OQ_PLAN_BODY_SET_MISMATCH' }
        $body = Get-OqFile $root $relative $plan.bodyFiles[$relative] -IncludeBytes
        $bodies[$relative] = $body.Bytes
        $parsedBody = $utf8.GetString($body.Bytes) | ConvertFrom-Json -AsHashtable
        if ($parsedBody -isnot [System.Collections.IDictionary]) { throw 'OQ_BODY_INVALID' }
    }
    # Fixed ordering makes property-order-only changes unable to reset the one-attempt identity.
    $parts = @('OQ-5.4-1', $Head, $binding.manifestSha256)
    foreach ($name in $targets.Keys) { $parts += "$name=$($targets[$name])" }
    foreach ($name in $required) { $parts += "$name=$($binding.files[$name].sha256)" }
    $fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes(($parts -join "`n")))).ToLowerInvariant()
    return @{ Fingerprint = $fingerprint; Binding = $binding; Directory = $root; JournalDirectory = $journalDirectory;
        Targets = $targets; Bodies = $bodies }
}

function Reserve-OqAttempt($Qualification, [string]$EvidencePath) {
    $directory = $Qualification.JournalDirectory
    $null = [IO.Directory]::CreateDirectory($directory)
    Assert-OqNoLinks $directory
    $path = Join-Path $directory ($Qualification.Fingerprint + '.json')
    $bytes = [Text.Encoding]::UTF8.GetBytes((@{ schemaVersion = 1; cohortId = 'OQ-5.4-1';
        targetSetSha256 = $Qualification.Fingerprint; evidencePath = $EvidencePath;
        reservedAt = [DateTimeOffset]::UtcNow.ToString('O') } | ConvertTo-Json))
    try {
        $file = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $file.Write($bytes); $file.Flush($true) } finally { $file.Dispose() }
    }
    catch [IO.IOException] { throw 'OQ_ATTEMPT_ALREADY_RECORDED_OR_JOURNAL_UNAVAILABLE' }
    return $path
}
