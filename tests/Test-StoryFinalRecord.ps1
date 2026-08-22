[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [string]$TestResultPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-GitLines {
    param(
        [string]$Root,
        [string[]]$GitArguments
    )

    $output = @(& git -C $Root @GitArguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return @($output | ForEach-Object { "$_" })
}

function Get-OptionalProperty {
    param(
        [object]$Object,
        [string]$Name,
        [object]$Default = $null
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return $property.Value
}

function ConvertTo-NormalizedPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -cne $Path.Trim()) {
        throw "Path '$Path' is not a normalized repository-relative path."
    }

    $normalized = $Path.Replace('\', '/').Normalize([Text.NormalizationForm]::FormC)
    $segments = @($normalized.Split('/', [StringSplitOptions]::None))
    if ($Path -cne $Path.Normalize([Text.NormalizationForm]::FormC) -or
        [System.IO.Path]::IsPathRooted($normalized) -or
        $normalized.StartsWith('./', [StringComparison]::Ordinal) -or
        $segments.Count -eq 0 -or
        @($segments | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Path '$Path' is not a normalized repository-relative path."
    }

    return $normalized
}

function Resolve-RepositoryPath {
    param(
        [string]$Root,
        [string]$Path,
        [switch]$MustExist
    )

    $normalized = ConvertTo-NormalizedPath $Path
    $canonicalRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $canonicalRoot $normalized))
    $prefix = "$canonicalRoot$([System.IO.Path]::DirectorySeparatorChar)"
    if (-not $candidate.StartsWith($prefix, [StringComparison]::Ordinal)) {
        throw "Path '$Path' escapes repository root '$canonicalRoot'."
    }

    if ($MustExist) {
        if (-not (Test-Path -LiteralPath $candidate)) {
            throw "Repository path '$normalized' does not exist."
        }
        $resolved = (Resolve-Path -LiteralPath $candidate).Path
        if ($resolved -cne $canonicalRoot -and
            -not $resolved.StartsWith($prefix, [StringComparison]::Ordinal)) {
            throw "Path '$Path' resolves outside repository root '$canonicalRoot'."
        }
    }

    return $candidate
}

function Assert-JsonSchema {
    param(
        [string]$JsonPath,
        [string]$SchemaPath,
        [string]$Label
    )

    $schemaErrors = @()
    $valid = Test-Json -LiteralPath $JsonPath -SchemaFile $SchemaPath -ErrorAction SilentlyContinue -ErrorVariable schemaErrors
    if (-not $valid) {
        $details = @($schemaErrors | ForEach-Object { $_.Exception.Message }) -join '; '
        throw "$Label schema validation failed: $details"
    }
}

function Get-Sha256ForText {
    param([string]$Text)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-Sha256ForFile {
    param([string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    [void]$Failures.Add($Message)
}

function Get-DiffEntries {
    param(
        [string]$Root,
        [string]$Range,
        [switch]$IncludeUntracked
    )

    $entries = [ordered]@{}
    foreach ($line in Invoke-GitLines -Root $Root -GitArguments @('diff', '--name-status', '--no-renames', $Range, '--')) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $parts = $line -split "`t", 2
        if ($parts.Count -ne 2) {
            throw "Unexpected git diff --name-status line: $line"
        }

        $path = ConvertTo-NormalizedPath $parts[1]
        $entries[$path] = $parts[0]
    }

    if ($IncludeUntracked) {
        foreach ($line in Invoke-GitLines -Root $Root -GitArguments @('ls-files', '--others', '--exclude-standard')) {
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }

            $path = ConvertTo-NormalizedPath $line
            $entries[$path] = '?'
        }
    }

    return $entries
}

function Get-RawDiffEntries {
    param(
        [string]$Root,
        [string]$Range
    )

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($line in Invoke-GitLines -Root $Root -GitArguments @('-c', 'core.quotePath=false', 'diff', '--raw', '--no-abbrev', '--no-renames', $Range, '--')) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -notmatch '^:(?<oldMode>\d{6}) (?<newMode>\d{6}) (?<oldOid>[0-9a-f]+) (?<newOid>[0-9a-f]+) (?<status>[^\t]+)\t(?<path>.+)$') {
            throw "Unexpected git diff --raw line: $line"
        }

        [void]$entries.Add([ordered]@{
            oldMode = $Matches.oldMode
            newMode = $Matches.newMode
            oldOid = $Matches.oldOid
            newOid = $Matches.newOid
            status = $Matches.status
            path = ConvertTo-NormalizedPath $Matches.path
        })
    }

    return @($entries)
}

function Get-StoryFileList {
    param([string]$Markdown)

    $paths = [System.Collections.Generic.List[string]]::new()
    $inside = $false
    foreach ($line in $Markdown -split "`r?`n") {
        if ($line -eq '### File List') {
            $inside = $true
            continue
        }

        if ($inside -and $line -match '^#{1,3}\s+') {
            break
        }

        if ($inside -and $line -match '^-\s+`(?<path>[^`]+)`') {
            [void]$paths.Add((ConvertTo-NormalizedPath $Matches.path))
        }
    }

    return @($paths)
}

function Get-ContentAtRevision {
    param(
        [string]$Root,
        [string]$Revision,
        [string]$Path
    )

    $lines = Invoke-GitLines -Root $Root -GitArguments @('show', "$Revision`:$Path")
    return $lines -join "`n"
}

function Get-JsonPathValue {
    param(
        [object]$Object,
        [string]$JsonPath
    )

    $current = $Object
    foreach ($segment in $JsonPath.Split('.', [StringSplitOptions]::RemoveEmptyEntries)) {
        $property = $current.PSObject.Properties[$segment]
        if ($null -eq $property) {
            throw "JSON path '$JsonPath' does not exist at '$segment'."
        }

        $current = $property.Value
    }

    return $current
}

function Test-CountClaims {
    param(
        [string]$Root,
        [object[]]$Claims,
        [string]$DefaultRevision,
        [System.Collections.Generic.List[string]]$Failures
    )

    $results = [System.Collections.Generic.List[object]]::new()
    foreach ($claim in $Claims) {
        $path = ConvertTo-NormalizedPath ([string]$claim.path)
        $revision = [string](Get-OptionalProperty -Object $claim -Name 'revision' -Default $DefaultRevision)
        try {
            if ($revision -eq 'WORKTREE') {
                $absolutePath = Resolve-RepositoryPath -Root $Root -Path $path
                if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
                    throw "Count-claim file '$path' does not exist in the working tree."
                }

                $content = Get-Content -LiteralPath $absolutePath -Raw
            }
            else {
                $content = Get-ContentAtRevision -Root $Root -Revision $revision -Path $path
            }

            $kind = [string]$claim.kind
            if ($kind -eq 'json') {
                $document = $content | ConvertFrom-Json -Depth 100
                $actual = Get-JsonPathValue -Object $document -JsonPath ([string]$claim.jsonPath)
                $expected = $claim.expected
                if ("$actual" -cne "$expected") {
                    throw "Expected $($claim.jsonPath)=$expected but found $actual."
                }
            }
            elseif ($kind -eq 'regex') {
                if ($content -notmatch [string]$claim.pattern) {
                    throw "Required pattern '$($claim.pattern)' was not found."
                }
                $actual = 'matched'
                $expected = 'matched'
            }
            else {
                throw "Unsupported count claim kind '$kind'."
            }

            [void]$results.Add([ordered]@{
                path = $path
                kind = $kind
                claim = [string](Get-OptionalProperty -Object $claim -Name 'jsonPath' -Default (Get-OptionalProperty -Object $claim -Name 'pattern' -Default ''))
                status = 'pass'
            })
        }
        catch {
            Add-Failure -Failures $Failures -Message "Count claim failed for '$path': $($_.Exception.Message)"
            [void]$results.Add([ordered]@{
                path = $path
                kind = [string]$claim.kind
                status = 'fail'
                message = $_.Exception.Message
            })
        }
    }

    return @($results)
}

function Test-PathSets {
    param(
        [string[]]$Expected,
        [string[]]$Observed,
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Label
    )

    $normalizedExpected = @($Expected | ForEach-Object { ConvertTo-NormalizedPath $_ })
    $normalizedObserved = @($Observed | ForEach-Object { ConvertTo-NormalizedPath $_ })
    $expectedDuplicates = @($normalizedExpected | Group-Object -CaseSensitive | Where-Object Count -gt 1 | ForEach-Object Name)
    $observedDuplicates = @($normalizedObserved | Group-Object -CaseSensitive | Where-Object Count -gt 1 | ForEach-Object Name)
    $expectedSet = @($normalizedExpected | Sort-Object -Unique)
    $observedSet = @($normalizedObserved | Sort-Object -Unique)
    $missing = @($expectedSet | Where-Object { $_ -notin $observedSet })
    $unexpected = @($observedSet | Where-Object { $_ -notin $expectedSet })

    foreach ($path in $expectedDuplicates) {
        Add-Failure -Failures $Failures -Message "$Label has duplicate expected path '$path'."
    }
    foreach ($path in $observedDuplicates) {
        Add-Failure -Failures $Failures -Message "$Label has duplicate observed path '$path'."
    }
    foreach ($path in $missing) {
        Add-Failure -Failures $Failures -Message "$Label is missing '$path'."
    }
    foreach ($path in $unexpected) {
        Add-Failure -Failures $Failures -Message "$Label contains unexpected path '$path'."
    }

    return [ordered]@{
        expected = $expectedSet
        observed = $observedSet
        duplicateExpected = $expectedDuplicates
        duplicateObserved = $observedDuplicates
        missing = $missing
        unexpected = $unexpected
        status = if ($missing.Count -eq 0 -and $unexpected.Count -eq 0 -and $expectedDuplicates.Count -eq 0 -and $observedDuplicates.Count -eq 0) { 'pass' } else { 'fail' }
    }
}

function Test-PreexistingState {
    param(
        [string]$Root,
        [object]$State,
        [System.Collections.Specialized.OrderedDictionary]$ObservedEntries,
        [System.Collections.Generic.List[string]]$Failures
    )

    $results = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($State.excludedEntries)) {
        $path = ConvertTo-NormalizedPath ([string]$entry.path)
        $absolutePath = Resolve-RepositoryPath -Root $Root -Path $path
        $entryFailures = [System.Collections.Generic.List[string]]::new()

        if ([string]$entry.kind -eq 'untracked-file') {
            if (-not $ObservedEntries.Contains($path) -or $ObservedEntries[$path] -ne '?') {
                Add-Failure -Failures $entryFailures -Message 'entry is no longer an untracked non-ignored file'
            }
            elseif (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
                Add-Failure -Failures $entryFailures -Message 'file is missing'
            }
            else {
                $actualHash = Get-Sha256ForFile $absolutePath
                if ($actualHash -cne [string]$entry.sha256) {
                    Add-Failure -Failures $entryFailures -Message "SHA-256 changed from $($entry.sha256) to $actualHash"
                }
            }
        }
        elseif ([string]$entry.kind -eq 'tracked-file') {
            if (-not $ObservedEntries.Contains($path) -or $ObservedEntries[$path] -cne [string]$entry.status) {
                Add-Failure -Failures $entryFailures -Message "tracked status changed from $($entry.status) to $($ObservedEntries[$path])"
            }
            if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
                Add-Failure -Failures $entryFailures -Message 'file is missing'
            }
            else {
                $baselineBlob = @(Invoke-GitLines -Root $Root -GitArguments @('rev-parse', "$($State.baselineCommit)`:$path"))[0]
                $indexBlob = @(Invoke-GitLines -Root $Root -GitArguments @('rev-parse', ":$path"))[0]
                $actualHash = Get-Sha256ForFile $absolutePath
                if ($baselineBlob -cne [string]$entry.baselineBlobOid) {
                    Add-Failure -Failures $entryFailures -Message "baseline blob changed from $($entry.baselineBlobOid) to $baselineBlob"
                }
                if ($indexBlob -cne [string]$entry.indexBlobOid) {
                    Add-Failure -Failures $entryFailures -Message "index blob changed from $($entry.indexBlobOid) to $indexBlob"
                }
                if ($actualHash -cne [string]$entry.sha256) {
                    Add-Failure -Failures $entryFailures -Message "SHA-256 changed from $($entry.sha256) to $actualHash"
                }
            }
        }
        elseif ([string]$entry.kind -eq 'gitlink') {
            $baseline = @(Invoke-GitLines -Root $Root -GitArguments @('rev-parse', "$($State.baselineCommit)`:$path"))[0]
            $indexLine = @(Invoke-GitLines -Root $Root -GitArguments @('ls-files', '-s', '--', $path))[0]
            $indexParts = $indexLine -split '\s+'
            $worktree = @(Invoke-GitLines -Root $Root -GitArguments @('-C', $path, 'rev-parse', 'HEAD'))[0]
            $internalStatus = (Invoke-GitLines -Root $Root -GitArguments @('-C', $path, 'status', '--porcelain=v2', '--untracked-files=all')) -join "`n"
            $internalHash = Get-Sha256ForText $internalStatus

            if ($baseline -cne [string]$entry.baselineCommit) {
                Add-Failure -Failures $entryFailures -Message "baseline gitlink changed from $($entry.baselineCommit) to $baseline"
            }
            if ($indexParts[0] -cne '160000' -or $indexParts[1] -cne [string]$entry.indexCommit) {
                Add-Failure -Failures $entryFailures -Message "index gitlink changed from $($entry.indexCommit) to $($indexParts[1])"
            }
            if ($worktree -cne [string]$entry.worktreeCommit) {
                Add-Failure -Failures $entryFailures -Message "worktree gitlink changed from $($entry.worktreeCommit) to $worktree"
            }
            if ($internalHash -cne [string]$entry.internalStatusSha256) {
                Add-Failure -Failures $entryFailures -Message "submodule internal status hash changed from $($entry.internalStatusSha256) to $internalHash"
            }
        }
        else {
            Add-Failure -Failures $entryFailures -Message "unsupported pre-existing entry kind '$($entry.kind)'"
        }

        if ($entryFailures.Count -eq 0) {
            [void]$ObservedEntries.Remove($path)
        }
        else {
            foreach ($message in $entryFailures) {
                Add-Failure -Failures $Failures -Message "Frozen entry '$path' changed: $message."
            }
        }

        [void]$results.Add([ordered]@{
            path = $path
            kind = [string]$entry.kind
            status = if ($entryFailures.Count -eq 0) { 'unchanged-excluded' } else { 'changed-fail' }
            failures = @($entryFailures)
        })
    }

    return @($results)
}

function Get-InputFingerprint {
    param(
        [string]$Root,
        [string[]]$Paths,
        [object[]]$Roots
    )

    $resolvedPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @($Paths)) {
        [void]$resolvedPaths.Add((ConvertTo-NormalizedPath $path))
    }
    foreach ($rootEntry in @($Roots)) {
        $relativeRoot = ConvertTo-NormalizedPath ([string]$rootEntry.path)
        $absoluteRoot = Resolve-RepositoryPath -Root $Root -Path $relativeRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot -PathType Container)) {
            throw "Executable/test input root '$relativeRoot' is missing."
        }
        $extensions = @($rootEntry.extensions | ForEach-Object { "$_".ToLowerInvariant() })
        foreach ($file in Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|TestResults)[\\/]' -and $_.Extension.ToLowerInvariant() -in $extensions
        }) {
            $relativePath = [System.IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
            [void]$resolvedPaths.Add((ConvertTo-NormalizedPath $relativePath))
        }
    }

    $rows = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @($resolvedPaths | Sort-Object -Unique)) {
        $absolutePath = Resolve-RepositoryPath -Root $Root -Path $path
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
            throw "Executable/test input '$path' is missing."
        }
        [void]$rows.Add("$path`:$((Get-Sha256ForFile $absolutePath))")
    }

    return Get-Sha256ForText ($rows -join "`n")
}

function Test-TrxResult {
    param(
        [string]$Path,
        [object]$ExpectedCounts,
        [string]$ContractTestName,
        [System.Collections.Generic.List[string]]$Failures
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-Failure -Failures $Failures -Message "TRX result '$Path' does not exist."
        return [ordered]@{ status = 'fail'; path = $Path }
    }

    [xml]$trx = Get-Content -LiteralPath $Path -Raw
    $counters = $trx.SelectSingleNode("/*[local-name()='TestRun']/*[local-name()='ResultSummary']/*[local-name()='Counters']")
    if ($null -eq $counters) {
        Add-Failure -Failures $Failures -Message "TRX result '$Path' has no Counters element."
        return [ordered]@{ status = 'fail'; path = $Path }
    }

    $actual = [ordered]@{
        total = [int]$counters.total
        executed = [int]$counters.executed
        passed = [int]$counters.passed
        failed = [int]$counters.failed
        skipped = [int]$counters.notExecuted
    }
    $unitTestResults = @($trx.SelectNodes("//*[local-name()='UnitTestResult']"))
    $failedTests = @($unitTestResults |
        Where-Object { [string]$_.outcome -cne 'Passed' } |
        ForEach-Object { [string]$_.testName } |
        Sort-Object -Unique)
    foreach ($name in @('total', 'passed', 'failed', 'skipped')) {
        if ([int]$actual[$name] -ne [int]$ExpectedCounts.$name) {
            Add-Failure -Failures $Failures -Message "TRX $name count is $($actual[$name]); expected $($ExpectedCounts.$name)."
        }
    }
    if ($actual.executed -ne $actual.total -or $actual.passed -ne $actual.total) {
        Add-Failure -Failures $Failures -Message "TRX is not fully green: total=$($actual.total), executed=$($actual.executed), passed=$($actual.passed)."
    }

    $contractResult = $unitTestResults |
        Where-Object { $_.testName -like "*$ContractTestName*" } |
        Select-Object -First 1
    if ($null -eq $contractResult) {
        Add-Failure -Failures $Failures -Message "TRX does not contain contract comparison test '$ContractTestName'."
        $contractStatus = 'missing'
    }
    else {
        $contractStatus = [string]$contractResult.outcome
        if ($contractStatus -cne 'Passed') {
            Add-Failure -Failures $Failures -Message "Contract comparison test '$ContractTestName' outcome is '$contractStatus'."
        }
    }

    return [ordered]@{
        status = if ($actual.failed -eq 0 -and $actual.skipped -eq 0 -and $contractStatus -eq 'Passed') { 'pass' } else { 'fail' }
        path = $Path
        counts = $actual
        failedTests = $failedTests
        contractTest = $ContractTestName
        contractTestOutcome = $contractStatus
        sha256 = Get-Sha256ForFile $Path
    }
}

function Test-PredecessorRecord {
    param(
        [string]$Root,
        [object]$Predecessor
    )

    if ($null -eq $Predecessor) {
        return [ordered]@{
            status = 'not-applicable'
            mechanicalResult = 'not-applicable'
            failures = @()
        }
    }

    $failures = [System.Collections.Generic.List[string]]::new()
    $jsonPath = ConvertTo-NormalizedPath ([string]$Predecessor.jsonPath)
    $markdownPath = ConvertTo-NormalizedPath ([string]$Predecessor.markdownPath)
    $amendmentPath = ConvertTo-NormalizedPath ([string]$Predecessor.amendmentPath)
    $jsonAbsolutePath = Resolve-RepositoryPath -Root $Root -Path $jsonPath
    $markdownAbsolutePath = Resolve-RepositoryPath -Root $Root -Path $markdownPath
    $amendmentAbsolutePath = Resolve-RepositoryPath -Root $Root -Path $amendmentPath

    foreach ($pathEntry in @(
        [ordered]@{ path = $jsonPath; absolutePath = $jsonAbsolutePath; sha256 = [string]$Predecessor.jsonSha256 },
        [ordered]@{ path = $markdownPath; absolutePath = $markdownAbsolutePath; sha256 = [string]$Predecessor.markdownSha256 }
    )) {
        if (-not (Test-Path -LiteralPath $pathEntry.absolutePath -PathType Leaf)) {
            Add-Failure -Failures $failures -Message "Predecessor artifact '$($pathEntry.path)' is missing."
            continue
        }

        $actualHash = Get-Sha256ForFile $pathEntry.absolutePath
        if ($actualHash -cne $pathEntry.sha256) {
            Add-Failure -Failures $failures -Message "Predecessor artifact '$($pathEntry.path)' SHA-256 is $actualHash; expected $($pathEntry.sha256)."
        }
        $changed = @(Invoke-GitLines -Root $Root -GitArguments @('diff', '--name-only', [string]$Predecessor.sourceCommit, '--', $pathEntry.path))
        if ($changed.Count -gt 0) {
            Add-Failure -Failures $failures -Message "Predecessor artifact '$($pathEntry.path)' differs from source commit $($Predecessor.sourceCommit)."
        }
    }

    if (Test-Path -LiteralPath $jsonAbsolutePath -PathType Leaf) {
        try {
            $source = Get-Content -LiteralPath $jsonAbsolutePath -Raw | ConvertFrom-Json -Depth 100
            if ([string]$source.status -cne [string]$Predecessor.expectedStatus) {
                Add-Failure -Failures $failures -Message "Predecessor status is '$($source.status)'; expected '$($Predecessor.expectedStatus)'."
            }

            $expectedFailures = @($Predecessor.expectedFailures | ForEach-Object { "$_" })
            $actualFailures = @($source.live.failures | ForEach-Object { "$_" })
            foreach ($failure in @($expectedFailures | Where-Object { $_ -notin $actualFailures })) {
                Add-Failure -Failures $failures -Message "Predecessor failure inventory is missing '$failure'."
            }
            foreach ($failure in @($actualFailures | Where-Object { $_ -notin $expectedFailures })) {
                Add-Failure -Failures $failures -Message "Predecessor failure inventory contains unexpected failure '$failure'."
            }
        }
        catch {
            Add-Failure -Failures $failures -Message "Predecessor JSON '$jsonPath' could not be evaluated: $($_.Exception.Message)"
        }
    }

    if (-not (Test-Path -LiteralPath $amendmentAbsolutePath -PathType Leaf)) {
        Add-Failure -Failures $failures -Message "Corrective amendment '$amendmentPath' is missing."
    }
    else {
        $amendment = Get-Content -LiteralPath $amendmentAbsolutePath -Raw
        foreach ($requiredText in @(
            [string]$Predecessor.amendmentMarker,
            [string]$Predecessor.jsonSha256,
            [string]$Predecessor.markdownSha256,
            [string]$Predecessor.sourceCommit,
            'does not reconstruct the former uncommitted working tree'
        )) {
            if (-not $amendment.Contains($requiredText, [StringComparison]::Ordinal)) {
                Add-Failure -Failures $failures -Message "Corrective amendment '$amendmentPath' is missing required binding '$requiredText'."
            }
        }
    }

    return [ordered]@{
        id = [string]$Predecessor.id
        status = if ($failures.Count -eq 0) { 'pass-with-approved-amendment' } else { 'fail' }
        mechanicalResult = if ($failures.Count -eq 0) { 'PASS' } else { 'FAIL' }
        sourceResult = 'FAIL'
        sourceCommit = [string]$Predecessor.sourceCommit
        jsonPath = $jsonPath
        jsonSha256 = [string]$Predecessor.jsonSha256
        markdownPath = $markdownPath
        markdownSha256 = [string]$Predecessor.markdownSha256
        amendmentPath = $amendmentPath
        failures = @($failures)
        limitations = 'The predecessor failure and exact bytes are preserved. The amendment disposes the named historical discrepancy; it does not reconstruct the former uncommitted working tree.'
    }
}

function Test-LiveRecord {
    param(
        [string]$Root,
        [object]$Configuration,
        [object]$PreexistingState,
        [string]$ResolvedTestResultPath
    )

    $failures = [System.Collections.Generic.List[string]]::new()
    $live = $Configuration.live
    $observedEntries = Get-DiffEntries -Root $Root -Range ([string]$live.baselineCommit) -IncludeUntracked
    $frozen = Test-PreexistingState -Root $Root -State $PreexistingState -ObservedEntries $observedEntries -Failures $failures

    foreach ($outputName in @('json', 'markdown')) {
        $outputPath = ConvertTo-NormalizedPath ([string]$Configuration.output.$outputName)
        if (-not $observedEntries.Contains($outputPath)) {
            $observedEntries[$outputPath] = 'generated'
        }
    }

    $recordPath = ConvertTo-NormalizedPath ([string]$live.recordPath)
    $recordAbsolutePath = Resolve-RepositoryPath -Root $Root -Path $recordPath
    if (-not (Test-Path -LiteralPath $recordAbsolutePath -PathType Leaf)) {
        Add-Failure -Failures $failures -Message "Live completion record '$recordPath' is missing."
        $declaredPaths = @()
    }
    else {
        $declaredPaths = @(Get-StoryFileList (Get-Content -LiteralPath $recordAbsolutePath -Raw))
        if ($declaredPaths.Count -eq 0) {
            Add-Failure -Failures $failures -Message "Live completion record '$recordPath' has no populated '### File List' section."
        }
    }
    $configuredPathCheck = Test-PathSets -Expected @($live.expectedChangedPaths) -Observed $declaredPaths -Failures $failures -Label 'Configured-vs-declared File List'
    $pathCheck = Test-PathSets -Expected $declaredPaths -Observed @($observedEntries.Keys) -Failures $failures -Label 'Live declared-vs-observed path inventory'

    foreach ($path in @($observedEntries.Keys)) {
        $modeLine = @(Invoke-GitLines -Root $Root -GitArguments @('ls-files', '-s', '--', $path))
        if ($modeLine.Count -gt 0 -and $modeLine[0] -match '^160000\s+') {
            Add-Failure -Failures $failures -Message "Live work item contains non-excluded gitlink '$path'."
        }
    }

    $validationFailures = [System.Collections.Generic.List[string]]::new()
    $trx = Test-TrxResult -Path $ResolvedTestResultPath -ExpectedCounts $live.expectedCounts -ContractTestName ([string]$live.contractTestName) -Failures $validationFailures
    $blockedValidationConfiguration = Get-OptionalProperty -Object $live -Name 'blockedValidation'
    if ($null -eq $blockedValidationConfiguration) {
        foreach ($message in $validationFailures) {
            Add-Failure -Failures $failures -Message $message
        }
        $blockedValidation = [ordered]@{
            status = 'not-applicable'
            mechanicalResult = 'not-applicable'
        }
    }
    else {
        $blockerFailures = [System.Collections.Generic.List[string]]::new()
        $expectedFailedTests = @($blockedValidationConfiguration.expectedFailedTests | ForEach-Object { "$_" } | Sort-Object -Unique)
        $actualFailedTests = @($trx.failedTests | ForEach-Object { "$_" } | Sort-Object -Unique)
        foreach ($testName in @($expectedFailedTests | Where-Object { $_ -notin $actualFailedTests })) {
            Add-Failure -Failures $blockerFailures -Message "Blocked validation is missing expected failed test '$testName'."
        }
        foreach ($testName in @($actualFailedTests | Where-Object { $_ -notin $expectedFailedTests })) {
            Add-Failure -Failures $blockerFailures -Message "Blocked validation contains unexpected failed test '$testName'."
        }

        $focusedPath = Resolve-RepositoryPath -Root $Root -Path ([string]$blockedValidationConfiguration.focusedTestResultPath)
        $focusedFailures = [System.Collections.Generic.List[string]]::new()
        $focusedTrx = Test-TrxResult -Path $focusedPath -ExpectedCounts $blockedValidationConfiguration.focusedExpectedCounts -ContractTestName ([string]$live.contractTestName) -Failures $focusedFailures
        foreach ($message in $focusedFailures) {
            Add-Failure -Failures $blockerFailures -Message "Focused validation failed: $message"
        }
        if ($validationFailures.Count -eq 0) {
            Add-Failure -Failures $blockerFailures -Message 'Blocked validation unexpectedly contains no broad-run failure.'
        }

        foreach ($message in $blockerFailures) {
            Add-Failure -Failures $failures -Message $message
        }
        $blockedValidation = [ordered]@{
            status = if ($blockerFailures.Count -eq 0) { 'blocked' } else { 'fail' }
            mechanicalResult = if ($blockerFailures.Count -eq 0) { 'BLOCKED' } else { 'FAIL' }
            code = [string]$blockedValidationConfiguration.code
            rationale = [string]$blockedValidationConfiguration.rationale
            broadTestRun = $trx
            broadFailures = @($validationFailures)
            expectedFailedTests = $expectedFailedTests
            focusedTestRun = $focusedTrx
            failures = @($blockerFailures)
        }
    }
    $claims = Test-CountClaims -Root $Root -Claims @($live.countClaims) -DefaultRevision 'WORKTREE' -Failures $failures

    try {
        $inputFingerprint = Get-InputFingerprint -Root $Root -Paths @($live.executableInputs) -Roots @($live.executableInputRoots)
        if ($inputFingerprint -cne [string]$live.executableInputFingerprint) {
            Add-Failure -Failures $failures -Message "Executable/test input fingerprint is stale: expected $($live.executableInputFingerprint), found $inputFingerprint."
        }
    }
    catch {
        $inputFingerprint = ''
        Add-Failure -Failures $failures -Message $_.Exception.Message
    }

    $contractBaselinePath = ConvertTo-NormalizedPath ([string]$live.contractBaselinePath)
    $contractBaselineDiff = @(Invoke-GitLines -Root $Root -GitArguments @('diff', '--name-only', [string]$live.baselineCommit, '--', $contractBaselinePath))
    if ($contractBaselineDiff.Count -gt 0) {
        Add-Failure -Failures $failures -Message "Public-contract baseline '$contractBaselinePath' changed in the live tree."
    }

    $evidence = [System.Collections.Generic.List[object]]::new()
    foreach ($evidenceItem in @($live.evidencePaths)) {
        if ($evidenceItem -is [string]) {
            $path = ConvertTo-NormalizedPath ([string]$evidenceItem)
            $expectedHash = $null
        }
        else {
            $path = ConvertTo-NormalizedPath ([string]$evidenceItem.path)
            $expectedHash = Get-OptionalProperty -Object $evidenceItem -Name 'sha256'
        }
        $absolutePath = Resolve-RepositoryPath -Root $Root -Path $path
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
            Add-Failure -Failures $failures -Message "Changed documentation/evidence '$path' is missing."
            [void]$evidence.Add([ordered]@{ path = $path; status = 'missing' })
        }
        else {
            $actualHash = Get-Sha256ForFile $absolutePath
            $status = 'present'
            if ($null -ne $expectedHash -and $actualHash -cne [string]$expectedHash) {
                Add-Failure -Failures $failures -Message "Changed documentation/evidence '$path' SHA-256 is $actualHash; expected $expectedHash."
                $status = 'hash-mismatch'
            }
            [void]$evidence.Add([ordered]@{ path = $path; status = $status; sha256 = $actualHash })
        }
    }
    foreach ($pair in @($live.evidencePairs)) {
        foreach ($pathValue in @($pair)) {
            $path = ConvertTo-NormalizedPath ([string]$pathValue)
            if ($path -notin @($live.expectedChangedPaths)) {
                Add-Failure -Failures $failures -Message "Evidence pair path '$path' is not declared in expectedChangedPaths."
            }
        }
    }

    return [ordered]@{
        id = [string]$live.id
        mode = 'live-working-tree'
        baselineCommit = [string]$live.baselineCommit
        evaluatedHead = @(Invoke-GitLines -Root $Root -GitArguments @('rev-parse', 'HEAD'))[0]
        status = if ($failures.Count -gt 0) { 'fail' } elseif ($blockedValidation.status -eq 'blocked') { 'blocked' } else { 'pass' }
        mechanicalResult = if ($failures.Count -gt 0) { 'FAIL' } elseif ($blockedValidation.status -eq 'blocked') { 'BLOCKED' } else { 'PASS' }
        failures = @($failures)
        testRun = $trx
        blockedValidation = $blockedValidation
        countClaims = $claims
        executableInputFingerprint = $inputFingerprint
        changedPaths = $pathCheck
        configuredFileList = $configuredPathCheck
        frozenPreexistingState = $frozen
        changedDocumentationAndEvidence = @($evidence)
        publicContractShape = [ordered]@{
            baselinePath = $contractBaselinePath
            baselineSha256 = Get-Sha256ForFile (Resolve-RepositoryPath -Root $Root -Path $contractBaselinePath -MustExist)
            comparisonTest = [string]$live.contractTestName
            diffState = if ($contractBaselineDiff.Count -eq 0 -and $trx.contractTestOutcome -eq 'Passed') { 'empty' } else { 'non-empty-or-unproven' }
        }
    }
}

function Test-HistoricalRecord {
    param(
        [string]$Root,
        [object]$Record
    )

    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($revision in @([string]$Record.baselineCommit, [string]$Record.finalCommit)) {
        try {
            [void](Invoke-GitLines -Root $Root -GitArguments @('cat-file', '-e', "$revision`^{commit}"))
        }
        catch {
            return [ordered]@{
                id = [string]$Record.id
                mode = 'historical-commit-record-consistency'
                baselineCommit = [string]$Record.baselineCommit
                finalCommit = [string]$Record.finalCommit
                status = 'blocked'
                mechanicalResult = 'BLOCKED'
                limitations = 'Required Git history is unavailable; no historical assertion was treated as pass or not-applicable.'
                failures = @("Required historical commit '$revision' is unavailable: $($_.Exception.Message)")
                counts = [ordered]@{
                    total = [int]$Record.expectedCounts.total
                    passed = [int]$Record.expectedCounts.passed
                    failed = [int]$Record.expectedCounts.failed
                    skipped = [int]$Record.expectedCounts.skipped
                    proof = 'blocked-unavailable-history'
                }
                countClaims = @()
                changedPaths = [ordered]@{ expected = @(); observed = @(); missing = @(); unexpected = @(); status = 'blocked' }
                fileListAmendments = @($Record.fileListAmendments)
                evidence = @()
                publicContractShape = [ordered]@{
                    baselinePath = ConvertTo-NormalizedPath ([string]$Record.contractBaselinePath)
                    baselineChangedInRange = $null
                    diffState = 'blocked'
                }
            }
        }
    }

    $range = "$($Record.baselineCommit)..$($Record.finalCommit)"
    $observedEntries = Get-DiffEntries -Root $Root -Range $range
    $storyPath = ConvertTo-NormalizedPath ([string]$Record.storyPath)
    $storyMarkdown = Get-ContentAtRevision -Root $Root -Revision ([string]$Record.finalCommit) -Path $storyPath
    $declared = [System.Collections.Generic.List[string]]::new()
    foreach ($path in Get-StoryFileList $storyMarkdown) {
        [void]$declared.Add($path)
    }
    foreach ($pathValue in @($Record.fileListAmendments)) {
        [void]$declared.Add((ConvertTo-NormalizedPath ([string]$pathValue)))
    }
    if (@($Record.fileListAmendments).Count -gt 0) {
        $amendmentRecordPath = ConvertTo-NormalizedPath ([string]$Record.amendmentRecordPath)
        $amendmentAbsolutePath = Resolve-RepositoryPath -Root $Root -Path $amendmentRecordPath
        if (-not (Test-Path -LiteralPath $amendmentAbsolutePath -PathType Leaf)) {
            Add-Failure -Failures $failures -Message "Story $($Record.id) amendment record '$amendmentRecordPath' is missing."
        }
        else {
            $amendmentContent = Get-Content -LiteralPath $amendmentAbsolutePath -Raw
            if ($amendmentContent -notmatch [string]$Record.amendmentPattern) {
                Add-Failure -Failures $failures -Message "Story $($Record.id) amendment record does not contain the approved factual-amendment marker."
            }
        }
    }
    $pathCheck = Test-PathSets -Expected @($declared) -Observed @($observedEntries.Keys) -Failures $failures -Label "Story $($Record.id) committed path inventory"

    $rawDiff = Get-RawDiffEntries -Root $Root -Range $range
    if (@($rawDiff | Where-Object { $_.oldMode -eq '160000' -or $_.newMode -eq '160000' }).Count -gt 0) {
        Add-Failure -Failures $failures -Message "Story $($Record.id) commit range contains a gitlink change."
    }

    $claims = Test-CountClaims -Root $Root -Claims @($Record.countClaims) -DefaultRevision ([string]$Record.finalCommit) -Failures $failures
    $evidence = [System.Collections.Generic.List[object]]::new()
    foreach ($item in @($Record.evidence)) {
        $path = ConvertTo-NormalizedPath ([string]$item.path)
        try {
            $blobOid = @(Invoke-GitLines -Root $Root -GitArguments @('rev-parse', "$($Record.finalCommit)`:$path"))[0]
            if ($blobOid -cne [string]$item.blobOid) {
                throw "expected blob $($item.blobOid), found $blobOid"
            }
            [void]$evidence.Add([ordered]@{ path = $path; status = 'pass'; blobOid = $blobOid })
        }
        catch {
            Add-Failure -Failures $failures -Message "Story $($Record.id) evidence '$path' failed identity check: $($_.Exception.Message)."
            [void]$evidence.Add([ordered]@{ path = $path; status = 'fail'; message = $_.Exception.Message })
        }
    }

    $contractPath = ConvertTo-NormalizedPath ([string]$Record.contractBaselinePath)
    $contractDiff = @(Invoke-GitLines -Root $Root -GitArguments @('diff', '--name-only', $range, '--', $contractPath))
    if ($contractDiff.Count -gt 0) {
        Add-Failure -Failures $failures -Message "Story $($Record.id) changed public-contract baseline '$contractPath'."
    }

    return [ordered]@{
        id = [string]$Record.id
        mode = 'historical-commit-record-consistency'
        baselineCommit = [string]$Record.baselineCommit
        finalCommit = [string]$Record.finalCommit
        status = if ($failures.Count -eq 0 -and @($Record.fileListAmendments).Count -gt 0) { 'pass-with-approved-amendment' } elseif ($failures.Count -eq 0) { 'pass' } else { 'fail' }
        mechanicalResult = if ($failures.Count -eq 0) { 'PASS' } else { 'FAIL' }
        limitations = 'Committed bytes, path modes, and cross-record claims are verified. A former uncommitted working tree is not reconstructed or claimed.'
        failures = @($failures)
        counts = [ordered]@{
            total = [int]$Record.expectedCounts.total
            passed = [int]$Record.expectedCounts.passed
            failed = [int]$Record.expectedCounts.failed
            skipped = [int]$Record.expectedCounts.skipped
            proof = 'cross-record-consistency'
        }
        countClaims = $claims
        changedPaths = $pathCheck
        fileListAmendments = @($Record.fileListAmendments)
        evidence = @($evidence)
        publicContractShape = [ordered]@{
            baselinePath = $contractPath
            baselineChangedInRange = $contractDiff.Count -gt 0
            diffState = if ($contractDiff.Count -eq 0) { 'baseline-unchanged-and-recorded-diff-empty' } else { 'non-empty' }
        }
    }
}

function ConvertTo-ReportMarkdown {
    param([object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Epic 5 Final-Record Check')
    [void]$lines.Add('')
    [void]$lines.Add("- **Generated:** $($Report.generatedAtUtc)")
    [void]$lines.Add("- **Overall result:** $($Report.status)")
    [void]$lines.Add("- **Mechanical result:** $($Report.mechanicalResult)")
    [void]$lines.Add('- **Authority:** The adjacent JSON artifact is authoritative; this Markdown is rendered from it.')
    [void]$lines.Add('')
    if ($Report.predecessor.status -ne 'not-applicable') {
        [void]$lines.Add('## Preserved Predecessor Disposition')
        [void]$lines.Add('')
        [void]$lines.Add("- Source result: $($Report.predecessor.sourceResult)")
        [void]$lines.Add("- Successor disposition: $($Report.predecessor.status)")
        [void]$lines.Add("- Source commit: $($Report.predecessor.sourceCommit)")
        [void]$lines.Add("- Corrective amendment: $($Report.predecessor.amendmentPath)")
        [void]$lines.Add("- Limitation: $($Report.predecessor.limitations)")
        [void]$lines.Add('')
    }
    [void]$lines.Add('## Live Final Working Tree')
    [void]$lines.Add('')
    [void]$lines.Add("- Result: $($Report.live.status)")
    [void]$lines.Add("- Conformance: $($Report.live.testRun.counts.passed) / $($Report.live.testRun.counts.total) passed; $($Report.live.testRun.counts.failed) failed; $($Report.live.testRun.counts.skipped) skipped.")
    [void]$lines.Add("- Changed paths: $($Report.live.changedPaths.observed.Count) observed, $($Report.live.changedPaths.missing.Count) missing, $($Report.live.changedPaths.unexpected.Count) unexpected.")
    [void]$lines.Add("- Frozen pre-existing entries: $($Report.live.frozenPreexistingState.Count) checked.")
    [void]$lines.Add("- Public-contract-shape diff: $($Report.live.publicContractShape.diffState).")
    if ($Report.live.blockedValidation.status -eq 'blocked') {
        [void]$lines.Add("- Completion blocker: $($Report.live.blockedValidation.code) — $($Report.live.blockedValidation.rationale)")
        [void]$lines.Add("- Focused contract comparison: $($Report.live.blockedValidation.focusedTestRun.counts.passed) / $($Report.live.blockedValidation.focusedTestRun.counts.total) passed.")
    }
    if ($Report.live.failures.Count -gt 0) {
        [void]$lines.Add('- Failures:')
        foreach ($failure in $Report.live.failures) {
            [void]$lines.Add("  - $failure")
        }
    }
    [void]$lines.Add('')
    [void]$lines.Add('## Historical Epic 5 Audit')
    [void]$lines.Add('')
    [void]$lines.Add('| Story | Result | Passed / Total | File List | Contract baseline |')
    [void]$lines.Add('| --- | --- | ---: | --- | --- |')
    foreach ($record in $Report.historical) {
        [void]$lines.Add("| $($record.id) | $($record.status) | $($record.counts.passed) / $($record.counts.total) | $($record.changedPaths.status) | $($record.publicContractShape.diffState) |")
    }
    [void]$lines.Add('')
    [void]$lines.Add('Historical mode proves committed path, artifact, and count-claim consistency. It does not claim to reconstruct a former uncommitted working tree.')
    [void]$lines.Add('')
    return $lines -join "`n"
}

$resolvedInputPath = (Resolve-Path -LiteralPath $InputPath).Path
$inputSchemaPath = Join-Path $PSScriptRoot 'Test-StoryFinalRecord.Input.schema.json'
$stateSchemaPath = Join-Path $PSScriptRoot 'Test-StoryFinalRecord.PreexistingState.schema.json'
Assert-JsonSchema -JsonPath $resolvedInputPath -SchemaPath $inputSchemaPath -Label 'Final-record input'
$configuration = Get-Content -LiteralPath $resolvedInputPath -Raw | ConvertFrom-Json -Depth 100

$root = @(Invoke-GitLines -Root (Split-Path $resolvedInputPath -Parent) -GitArguments @('rev-parse', '--show-toplevel'))[0]
$preexistingPath = Resolve-RepositoryPath -Root $root -Path ([string]$configuration.preexistingStatePath) -MustExist
Assert-JsonSchema -JsonPath $preexistingPath -SchemaPath $stateSchemaPath -Label 'Frozen pre-existing state'
$preexistingState = Get-Content -LiteralPath $preexistingPath -Raw | ConvertFrom-Json -Depth 100
if ([string]$preexistingState.baselineCommit -cne [string]$configuration.live.baselineCommit) {
    throw "Frozen pre-existing state baseline '$($preexistingState.baselineCommit)' does not match live baseline '$($configuration.live.baselineCommit)'."
}
$resolvedTrxPath = if ([string]::IsNullOrWhiteSpace($TestResultPath)) {
    Resolve-RepositoryPath -Root $root -Path ([string]$configuration.live.testResultPath)
}
else {
    (Resolve-Path -LiteralPath $TestResultPath).Path
}

$predecessorConfiguration = Get-OptionalProperty -Object $configuration -Name 'predecessor'
$predecessorResult = Test-PredecessorRecord -Root $root -Predecessor $predecessorConfiguration
$liveResult = Test-LiveRecord -Root $root -Configuration $configuration -PreexistingState $preexistingState -ResolvedTestResultPath $resolvedTrxPath
$historicalResults = [System.Collections.Generic.List[object]]::new()
foreach ($record in @($configuration.historical)) {
    [void]$historicalResults.Add((Test-HistoricalRecord -Root $root -Record $record))
}

$assertionLedger = [System.Collections.Generic.List[object]]::new()
[void]$assertionLedger.Add([ordered]@{ id = 'live-final-tree'; result = [string]$liveResult.mechanicalResult })
foreach ($historicalResult in $historicalResults) {
    [void]$assertionLedger.Add([ordered]@{ id = "historical-$($historicalResult.id)"; result = [string]$historicalResult.mechanicalResult })
}
if ($predecessorResult.status -ne 'not-applicable') {
    [void]$assertionLedger.Add([ordered]@{ id = 'predecessor-disposition'; result = [string]$predecessorResult.mechanicalResult })
}

$predecessorPassed = $predecessorResult.status -eq 'not-applicable' -or $predecessorResult.status -eq 'pass-with-approved-amendment'
$allPassed = $assertionLedger.Count -gt 0 -and
    $predecessorPassed -and
    $liveResult.status -eq 'pass' -and
    @($historicalResults | Where-Object { $_.status -notlike 'pass*' }).Count -eq 0
$allBlocked = $assertionLedger.Count -gt 0 -and
    $predecessorPassed -and
    $liveResult.status -eq 'blocked' -and
    @($historicalResults | Where-Object { $_.status -notlike 'pass*' }).Count -eq 0
$report = [ordered]@{
    schemaVersion = 1
    artifact = 'epic-5-final-record-check'
    generatedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    status = if ($allPassed) { 'pass' } elseif ($allBlocked) { 'blocked' } else { 'fail' }
    mechanicalResult = if ($allPassed) { 'PASS' } elseif ($allBlocked) { 'BLOCKED' } else { 'FAIL' }
    approvedProposal = [string]$configuration.approvedProposal
    predecessor = $predecessorResult
    live = $liveResult
    historical = @($historicalResults)
    assertionLedger = @($assertionLedger)
}

$jsonPath = Resolve-RepositoryPath -Root $root -Path ([string]$configuration.output.json)
$markdownPath = Resolve-RepositoryPath -Root $root -Path ([string]$configuration.output.markdown)
New-Item -ItemType Directory -Path (Split-Path $jsonPath -Parent) -Force | Out-Null
$markdown = ConvertTo-ReportMarkdown -Report $report
$report['renderedMarkdownSha256'] = Get-Sha256ForText $markdown
$json = $report | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText($jsonPath, "$json`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($markdownPath, $markdown, [System.Text.UTF8Encoding]::new($false))

if (-not $allPassed) {
    $messages = @($predecessorResult.failures) + @($liveResult.failures) + @($historicalResults | ForEach-Object { $_.failures })
    if ($allBlocked) {
        $messages += "$($liveResult.blockedValidation.code): $($liveResult.blockedValidation.rationale)"
        throw "Final-record verification blocked:`n- $($messages -join "`n- ")"
    }
    throw "Final-record verification failed:`n- $($messages -join "`n- ")"
}

Write-Host "Final-record verification passed. JSON: $($configuration.output.json); Markdown: $($configuration.output.markdown)"
