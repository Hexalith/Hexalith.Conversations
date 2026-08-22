[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Utf8File {
    param(
        [string]$Path,
        [string]$Content
    )

    $parent = Split-Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-FixtureGit {
    param(
        [string]$Root,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$GitArguments
    )

    $output = @(& git -C $Root @GitArguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }
    return @($output | ForEach-Object { "$_" })
}

function Get-TextHash {
    param([string]$Text)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-FileHashValue {
    param([string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Invoke-CheckerScenario {
    param(
        [string]$CheckerPath,
        [string]$InputPath,
        [bool]$ShouldPass,
        [string]$ExpectedText
    )

    $output = @(& pwsh -NoProfile -File $CheckerPath -InputPath $InputPath 2>&1)
    $exitCode = $LASTEXITCODE
    $combined = $output -join [Environment]::NewLine
    if ($ShouldPass -and $exitCode -ne 0) {
        throw "Expected checker success, got exit $exitCode.`n$combined"
    }
    if (-not $ShouldPass -and $exitCode -eq 0) {
        throw "Expected checker failure containing '$ExpectedText', but it passed."
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedText) -and $combined -notlike "*$ExpectedText*") {
        throw "Checker output did not contain '$ExpectedText'.`n$combined"
    }
}

$checkerPath = (Resolve-Path (Join-Path $PSScriptRoot 'Test-StoryFinalRecord.ps1')).Path
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-final-record-$([Guid]::NewGuid().ToString('N'))"
$scenarioCount = 0

try {
    New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
    Invoke-FixtureGit $fixtureRoot init | Out-Null
    Invoke-FixtureGit $fixtureRoot config user.email fixture@example.invalid | Out-Null
    Invoke-FixtureGit $fixtureRoot config user.name 'Final Record Fixture' | Out-Null

    $dependencyPath = Join-Path $fixtureRoot 'dependency'
    New-Item -ItemType Directory -Path $dependencyPath -Force | Out-Null
    Invoke-FixtureGit $dependencyPath init | Out-Null
    Invoke-FixtureGit $dependencyPath config user.email fixture@example.invalid | Out-Null
    Invoke-FixtureGit $dependencyPath config user.name 'Dependency Fixture' | Out-Null
    Write-Utf8File (Join-Path $dependencyPath 'state.txt') 'one'
    Invoke-FixtureGit $dependencyPath add state.txt | Out-Null
    Invoke-FixtureGit $dependencyPath commit -m 'state one' | Out-Null
    $dependencyBaseline = @(Invoke-FixtureGit $dependencyPath rev-parse HEAD)[0]

    Write-Utf8File (Join-Path $fixtureRoot 'contract.json') '{"shape":"stable"}'
    Write-Utf8File (Join-Path $fixtureRoot 'concurrent.txt') 'baseline'
    Invoke-FixtureGit $fixtureRoot add contract.json concurrent.txt dependency | Out-Null
    Invoke-FixtureGit $fixtureRoot commit -m baseline | Out-Null
    $historicalBaselineCommit = @(Invoke-FixtureGit $fixtureRoot rev-parse HEAD)[0]

    $historicalStory = @'
# Historical Fixture Record

Final conformance: 1 / 1 passed, 0 failed, 0 skipped.

### File List

- `historical-evidence.json`
- `historical-story.md`
- `prior.json`
- `prior.md`
'@
    Write-Utf8File (Join-Path $fixtureRoot 'historical-story.md') $historicalStory
    Write-Utf8File (Join-Path $fixtureRoot 'historical-evidence.json') '{"total":1,"passed":1,"failed":0,"skipped":0}'
    $predecessorJson = @{
        schemaVersion = 1
        status = 'fail'
        live = @{
            failures = @('original path mismatch', 'original fingerprint mismatch')
        }
    } | ConvertTo-Json -Depth 10
    Write-Utf8File (Join-Path $fixtureRoot 'prior.json') ($predecessorJson + "`n")
    Write-Utf8File (Join-Path $fixtureRoot 'prior.md') "# Preserved failed record`n"
    Invoke-FixtureGit $fixtureRoot add historical-story.md historical-evidence.json prior.json prior.md | Out-Null
    Invoke-FixtureGit $fixtureRoot commit -m 'historical final' | Out-Null
    $historicalFinalCommit = @(Invoke-FixtureGit $fixtureRoot rev-parse HEAD)[0]
    $baselineCommit = $historicalFinalCommit
    $concurrentBaselineBlob = @(Invoke-FixtureGit $fixtureRoot rev-parse "$baselineCommit`:concurrent.txt")[0]
    $historicalEvidenceBlob = @(Invoke-FixtureGit $fixtureRoot rev-parse "$historicalFinalCommit`:historical-evidence.json")[0]
    $priorJsonHash = Get-FileHashValue (Join-Path $fixtureRoot 'prior.json')
    $priorMarkdownHash = Get-FileHashValue (Join-Path $fixtureRoot 'prior.md')

    Write-Utf8File (Join-Path $dependencyPath 'state.txt') 'two'
    Invoke-FixtureGit $dependencyPath add state.txt | Out-Null
    Invoke-FixtureGit $dependencyPath commit -m 'state two' | Out-Null
    $dependencyWorktree = @(Invoke-FixtureGit $dependencyPath rev-parse HEAD)[0]

    $unrelatedPath = Join-Path $fixtureRoot 'unrelated.txt'
    Write-Utf8File $unrelatedPath 'preserve me'
    $unrelatedHash = Get-FileHashValue $unrelatedPath
    $concurrentPath = Join-Path $fixtureRoot 'concurrent.txt'
    Write-Utf8File $concurrentPath 'concurrent task state'
    $concurrentHash = Get-FileHashValue $concurrentPath

    $storyContent = @'
# Fixture Completion Record

Final conformance: 1 / 1 passed, 0 failed, 0 skipped.

### File List

- `code.ps1`
- `corrective-amendment.md`
- `evidence.json`
- `input.json`
- `out.json`
- `out.md`
- `result.trx`
- `snapshot.json`
- `story.md`
'@
    Write-Utf8File (Join-Path $fixtureRoot 'story.md') $storyContent
    Write-Utf8File (Join-Path $fixtureRoot 'code.ps1') 'Write-Output fixture'
    Write-Utf8File (Join-Path $fixtureRoot 'evidence.json') '{"total":1,"passed":1,"failed":0,"skipped":0}'
    Write-Utf8File (Join-Path $fixtureRoot 'out.json') '{}'
    Write-Utf8File (Join-Path $fixtureRoot 'out.md') '# pending'
    $amendment = @"
# Corrective Amendment

Marker: FIXTURE-CORRECTIVE-AMENDMENT

Source commit: $historicalFinalCommit
JSON SHA-256: $priorJsonHash
Markdown SHA-256: $priorMarkdownHash

This successor does not reconstruct the former uncommitted working tree.
"@
    Write-Utf8File (Join-Path $fixtureRoot 'corrective-amendment.md') $amendment

    $trx = @'
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="Fixture.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting" outcome="Passed" />
  </Results>
  <ResultSummary outcome="Completed">
    <Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="1" inProgress="0" pending="0" />
  </ResultSummary>
</TestRun>
'@
    Write-Utf8File (Join-Path $fixtureRoot 'result.trx') $trx

    $snapshot = [ordered]@{
        schemaVersion = 1
        capturedAtUtc = '2026-07-14T00:00:00Z'
        baselineCommit = $baselineCommit
        excludedEntries = @(
            [ordered]@{
                path = 'unrelated.txt'
                kind = 'untracked-file'
                sha256 = $unrelatedHash
            },
            [ordered]@{
                path = 'concurrent.txt'
                kind = 'tracked-file'
                status = 'M'
                baselineBlobOid = $concurrentBaselineBlob
                indexBlobOid = $concurrentBaselineBlob
                sha256 = $concurrentHash
            },
            [ordered]@{
                path = 'dependency'
                kind = 'gitlink'
                baselineCommit = $dependencyBaseline
                indexCommit = $dependencyBaseline
                worktreeCommit = $dependencyWorktree
                internalStatusSha256 = Get-TextHash ''
            }
        )
    }
    $snapshotPath = Join-Path $fixtureRoot 'snapshot.json'
    Write-Utf8File $snapshotPath (($snapshot | ConvertTo-Json -Depth 20) + "`n")

    $codeHash = Get-FileHashValue (Join-Path $fixtureRoot 'code.ps1')
    $inputFingerprint = Get-TextHash "code.ps1`:$codeHash"
    $evidenceHash = Get-FileHashValue (Join-Path $fixtureRoot 'evidence.json')
    $expectedPaths = @('code.ps1', 'corrective-amendment.md', 'evidence.json', 'input.json', 'out.json', 'out.md', 'result.trx', 'snapshot.json', 'story.md')
    $input = [ordered]@{
        schemaVersion = 1
        approvedProposal = 'fixture'
        preexistingStatePath = 'snapshot.json'
        output = [ordered]@{ json = 'out.json'; markdown = 'out.md' }
        predecessor = [ordered]@{
            id = 'failed-predecessor'
            sourceCommit = $historicalFinalCommit
            jsonPath = 'prior.json'
            jsonSha256 = $priorJsonHash
            markdownPath = 'prior.md'
            markdownSha256 = $priorMarkdownHash
            expectedStatus = 'fail'
            expectedFailures = @('original path mismatch', 'original fingerprint mismatch')
            amendmentPath = 'corrective-amendment.md'
            amendmentMarker = 'FIXTURE-CORRECTIVE-AMENDMENT'
        }
        live = [ordered]@{
            id = 'fixture'
            baselineCommit = $baselineCommit
            recordPath = 'story.md'
            expectedChangedPaths = $expectedPaths
            testResultPath = 'result.trx'
            expectedCounts = [ordered]@{ total = 1; passed = 1; failed = 0; skipped = 0 }
            contractTestName = 'CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting'
            contractBaselinePath = 'contract.json'
            executableInputs = @('code.ps1')
            executableInputRoots = @()
            executableInputFingerprint = $inputFingerprint
            countClaims = @(
                [ordered]@{ path = 'story.md'; kind = 'regex'; pattern = '1 / 1 passed'; revision = 'WORKTREE' },
                [ordered]@{ path = 'evidence.json'; kind = 'json'; jsonPath = 'total'; expected = 1; revision = 'WORKTREE' }
            )
            evidencePaths = @([ordered]@{ path = 'evidence.json'; sha256 = $evidenceHash })
            evidencePairs = @(, @('out.json', 'out.md'))
        }
        historical = @(
            [ordered]@{
                id = 'historical-fixture'
                baselineCommit = $historicalBaselineCommit
                finalCommit = $historicalFinalCommit
                storyPath = 'historical-story.md'
                expectedCounts = [ordered]@{ total = 1; passed = 1; failed = 0; skipped = 0 }
                fileListAmendments = @()
                amendmentRecordPath = $null
                amendmentPattern = $null
                contractBaselinePath = 'contract.json'
                countClaims = @(
                    [ordered]@{ path = 'historical-story.md'; kind = 'regex'; pattern = '1 / 1 passed' },
                    [ordered]@{ path = 'historical-evidence.json'; kind = 'json'; jsonPath = 'total'; expected = 1 }
                )
                evidence = @(
                    [ordered]@{ path = 'historical-evidence.json'; blobOid = $historicalEvidenceBlob }
                )
            }
        )
    }
    $inputPath = Join-Path $fixtureRoot 'input.json'
    $baseInput = ($input | ConvertTo-Json -Depth 30) + "`n"
    Write-Utf8File $inputPath $baseInput

    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $true -ExpectedText 'Final-record verification passed'
    $scenarioCount++

    $invalidSchema = $baseInput | ConvertFrom-Json -Depth 30
    $invalidSchema.live.PSObject.Properties.Remove('contractTestName')
    Write-Utf8File $inputPath (($invalidSchema | ConvertTo-Json -Depth 30) + "`n")
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText 'Final-record input schema validation failed'
    $scenarioCount++
    Write-Utf8File $inputPath $baseInput

    $stale = $baseInput | ConvertFrom-Json -Depth 30
    $stale.live.expectedCounts.total = 2
    $stale.live.expectedCounts.passed = 2
    Write-Utf8File $inputPath (($stale | ConvertTo-Json -Depth 30) + "`n")
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText 'TRX total count is 1; expected 2'
    $scenarioCount++
    Write-Utf8File $inputPath $baseInput

    Write-Utf8File (Join-Path $fixtureRoot 'story.md') ($storyContent.Replace("- ``evidence.json```n", ''))
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "unexpected path 'evidence.json'"
    $scenarioCount++
    Write-Utf8File (Join-Path $fixtureRoot 'story.md') $storyContent

    Write-Utf8File (Join-Path $fixtureRoot 'story.md') ($storyContent + "`n- ``ghost.txt```n")
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "is missing 'ghost.txt'"
    $scenarioCount++
    Write-Utf8File (Join-Path $fixtureRoot 'story.md') $storyContent

    Write-Utf8File (Join-Path $fixtureRoot 'evidence.json') '{"total":2,"passed":2,"failed":0,"skipped":0}'
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "evidence.json' SHA-256"
    $scenarioCount++
    Write-Utf8File (Join-Path $fixtureRoot 'evidence.json') '{"total":1,"passed":1,"failed":0,"skipped":0}'

    Write-Utf8File (Join-Path $fixtureRoot 'prior.md') "# Altered failed record`n"
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "Predecessor artifact 'prior.md'"
    $scenarioCount++
    Write-Utf8File (Join-Path $fixtureRoot 'prior.md') "# Preserved failed record`n"

    Write-Utf8File $unrelatedPath 'changed by another task'
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "Frozen entry 'unrelated.txt'"
    $scenarioCount++
    Write-Utf8File $unrelatedPath 'preserve me'

    Write-Utf8File $concurrentPath 'changed after refreeze'
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "Frozen entry 'concurrent.txt'"
    $scenarioCount++
    Write-Utf8File $concurrentPath 'concurrent task state'

    $newDependencyPath = Join-Path $fixtureRoot 'new-dependency'
    New-Item -ItemType Directory -Path $newDependencyPath -Force | Out-Null
    Invoke-FixtureGit $newDependencyPath init | Out-Null
    Invoke-FixtureGit $newDependencyPath config user.email fixture@example.invalid | Out-Null
    Invoke-FixtureGit $newDependencyPath config user.name 'New Dependency Fixture' | Out-Null
    Write-Utf8File (Join-Path $newDependencyPath 'state.txt') 'one'
    Invoke-FixtureGit $newDependencyPath add state.txt | Out-Null
    Invoke-FixtureGit $newDependencyPath commit -m initial | Out-Null
    Invoke-FixtureGit $fixtureRoot add new-dependency | Out-Null
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "non-excluded gitlink 'new-dependency'"
    $scenarioCount++
    Invoke-FixtureGit $fixtureRoot rm --cached -f new-dependency | Out-Null
    Remove-Item -LiteralPath $newDependencyPath -Recurse -Force

    Write-Utf8File (Join-Path $fixtureRoot 'contract.json') '{"shape":"drift"}'
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText "contract.json"
    $scenarioCount++
    Write-Utf8File (Join-Path $fixtureRoot 'contract.json') '{"shape":"stable"}'

    $missingHistory = $baseInput | ConvertFrom-Json -Depth 30
    $missingHistory.historical[0].finalCommit = 'ffffffffffffffffffffffffffffffffffffffff'
    Write-Utf8File $inputPath (($missingHistory | ConvertTo-Json -Depth 30) + "`n")
    Invoke-CheckerScenario -CheckerPath $checkerPath -InputPath $inputPath -ShouldPass $false -ExpectedText 'Required historical commit'
    $scenarioCount++

    Write-Host "Test-StoryFinalRecord fixtures passed: $scenarioCount scenarios."
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
