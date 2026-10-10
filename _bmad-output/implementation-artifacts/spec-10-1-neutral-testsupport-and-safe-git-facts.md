---
title: 'Story 10.1: Neutral TestSupport helpers and safe Git facts'
type: 'feature'
created: '2026-10-10'
status: 'in-progress'
baseline_commit: '2f2621aa4b40b0d44c62bf5e1e98727e7a182fec'
route: 'dispatch'
review_loop_iteration: 2
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-10-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Evidence tests duplicate repository plumbing and can mistake blocked Git history or unsafe process execution for usable facts. Story 10.1 needs a shared, test-only foundation before later Epic 10 stories can enforce the evidence boundary.

**Approach:** Add two TestSupport projects with the five named public helpers and a bounded Git-facts runner. Prove the Story 10.1 scenarios with isolated fixtures and publish its candidate-bound final record.

## Boundaries & Constraints

**Always:** The helper project is non-packable, non-publishable, and free of Conversations production assembly references. Expose `RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`, and `AssertionLedger` once each. Git execution drains both pipes concurrently, uses a 30-second limit plus cancellation and process-tree termination, isolates hostile ambient configuration, and reads UTF-8 NUL-delimited raw paths/modes and historical blobs. Unavailable or invalid history is typed and never successful. Bind the final record to `SC-10.1`, the 7.4 and 9.2 record digests, both project/assembly/result hashes, and the frozen inventory digest.

**Never:** Migrate or weaken either conformance tier or any frozen evidence reader. Do not implement Story 10.2 invariants in this story, ship the helpers as a package, or treat missing Git/history as an empty successful result. Preserve accepted historical records.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Raw facts | Committed Unicode and literal-backslash paths, raw diff/tree modes, historical blob | Exact decoded paths, modes, revisions, and blob SHA-256 | Malformed UTF-8 is invalid, never silently replaced |
| Hostile environment | Injected Git config, redirects, prompts, or replacement objects | Repository facts come from the requested repository only | Unsafe or unavailable authority returns a typed failure |
| Blocked history | Shallow/partial repository, missing Git, non-repository, bad revision, escaping root | No facts are reported as successful | Distinguish unavailable/invalid; root escape uses `REPOSITORY_PATH_ESCAPE` |
| Process pressure | Both streams exceed pipe capacity, hang, or caller cancellation | Complete output on success; bounded termination otherwise | Classify timeout/cancelled and terminate the process tree |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9/story-contracts/10.1.json` and `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (Story 10.1) -- exact AC commands, output pair, predecessor and rollback contract.
- `Directory.Build.props`, `tests/Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `Hexalith.Conversations.slnx` -- existing .NET 10 test conventions and explicit solution list; no root `.editorconfig` exists.
- `tests/Hexalith.Conversations.Conformance.Tests/RepositoryEvidencePathResolver.cs` and `PackageEnvironmentAuthorityV18ValidationTest.cs` -- repository-root intent and raw Git precedent; avoid their weaker process/config paths.
- `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and `_bmad/scripts/tests/test_generate_story_record.py` -- generic successor recording exists, but 10.1-specific predecessor digest and artifact bindings need support.
- `docs/release-evidence/story-{7.4,9.2}-final-record-v2.json` -- existing passing predecessor records; leave their bytes intact.
- `.github/workflows/ci.yml` -- unit project list is explicit; add the new test project without changing conformance lanes.

## Tasks & Acceptance

**Execution:**
- [ ] `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` and `RepositoryLocator.cs`, `GitFacts.cs`, `EvidenceManifest.cs`, `BoundaryAssertions.cs`, `AssertionLedger.cs` in that directory -- create the non-shipping project and five one-type-per-file helper surfaces; provide safe repository, process, revision, diff, tree, and blob facts and usable neutral manifest/assertion structures for later stories. Repository/path failures must be typed, including NUL, inaccessible paths, root symlinks and linked root ancestors, and escaping symlink parents; preserve valid path whitespace. Historical blob lookup must validate the committed tree rather than the current worktree. Strictly validate raw revision lines, diff status/mode/object relationships, tree records, and duplicates. Use deterministic rename behavior independent of repository config. A shallow repository may report a revision unavailable only when a reachable shallow boundary proves the missing ancestor; malformed or unrelated revisions are invalid. Propagate cancellation/timeout through fallback probes.
- [ ] `GitFacts.cs` and related process code -- enforce a fixed output byte budget while draining both streams, including a success-path overflow check when all tasks finish normally. Use a 30-second execution limit and bounded cleanup waits; never block indefinitely after a failed termination and never return success with incomplete capture. Terminate descendants even if the Git parent already exited and a child inherited its pipes. Disable promisor lazy fetching and object replacements as part of hostile-environment isolation.
- [ ] `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj`, `TestSupportSurfaceTests.cs`, and `GitFactsTests.cs` -- add xUnit v3 fixtures for surface, stream pressure, cancellation/timeout, hostile config, raw UTF-8 facts, and typed unavailable/invalid cases; restore fixture bytes. Prove a descendant started and exited after cancellation, output overflow never returns success, replacement refs cannot alter historical bytes, no lazy fetch occurs in a partial clone, a current symlink does not block a historical blob, and malformed Git records return typed failures. Include malformed revision/score/cross-field and rename-config fixtures. Make fixtures portable across Windows and Unix; use a literal-backslash path only where the platform permits it.
- [ ] `Hexalith.Conversations.slnx` and `.github/workflows/ci.yml` -- include both projects in the solution and run the new tests in CI.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and focused generator tests -- bind Story 10.1 predecessor digests and project/assembly/result hashes without changing prior record meanings. Validate evaluated MSBuild pack/publish properties and all declared production assembly/package/project dependencies; reject a linked parent directory for hashed artifacts. Require each predecessor to be a valid committed PASS pair in the Story 10.1 candidate tree, with the exact story ID, resolvable candidate commit, and all four `epic`, `architecture`, `planningCandidate`, and `bundleDigest` authority fields matching current Story 10.1 authority; historical predecessor branches may diverge. Exercise a broken committed JSON/Markdown predecessor pair and helper package/project dependency as negative tests. Require exact distinct project/result artifact paths and equality between `sourceRevisionId` and `candidate.commit` in both generation and pair verification.
- [ ] `docs/release-evidence/story-10.1-final-record-v2.{json,md}` -- generate the deterministic pair from a committed local candidate after AC-01 through AC-06 pass; preserve the 7.4/9.2 records.

**Acceptance Criteria:**
- Given the resolved TestSupport graph, when AC-10.1-01 builds it, then it passes and remains non-packable, non-publishable, and Conversations-assembly-free.
- Given the helper surface, when AC-10.1-02 inspects it, then all five required public names occur exactly once.
- Given full concurrent streams or a hung/cancelled process, when AC-10.1-03/04 run, then output is complete or the tree is bounded and classified.
- Given hostile Git configuration and unusual paths, when AC-10.1-05 runs, then raw facts and hashes match fixture bytes.
- Given unavailable or invalid Git inputs, when AC-10.1-06 runs, then no fixture reports success and root escape is explicit.
- Given six passing results and compatible 7.4/9.2 records, when AC-10.1-07 generates the pair, then it is `PASS` with summary `7/7/0/0/0/0` and all required bindings.
- Given a bad revision in a shallow repository or malformed raw Git output, when a helper reads it, then it returns typed invalid rather than successful or unavailable facts. Only a provably missing shallow ancestor is unavailable. Given cancelled fallback, its cancellation status survives.
- Given a valid historical blob whose current worktree path is a symlink, when read by revision, then committed bytes and SHA-256 are returned. Given a root or artifact parent symlink that escapes the requested repository, it is rejected.
- Given ordinary child processes and large concurrent output, when the process runner stops, then no started descendant remains and capture stays within the stated byte limit; startup and cleanup waits are bounded even if termination fails.
- Given a release candidate and predecessor records, when generation or pair verification runs, then committed-pair currency and authority compatibility, exact path/revision bindings, and evaluated non-shipping/dependency rules are checked; invalid bindings fail.

## Implementation Notes

- Review loop 2: candidate `4f57a68a08b842b160c6b9c12fca6e82134da78f` and its pair at `40d0168` passed AC-01..07, exact solution restore and Release build, focused TestSupport 5/5, focused generator 4/4, and pair/schema checks. All five TRX files matched record digests; the 7.4/9.2 predecessor bytes were unchanged. That candidate was withdrawn from the working tree because the second review found further safeguards to derive. Its local commits remain for traceability.
- The full generator test file on that candidate was interrupted after 153 passes and one unrelated historical workflow assertion failure against an untouched skill file. The earlier broad tooling-directory pytest run was also interrupted after historical workflow failures; those are not Story 10.1 acceptance gates under the current-change runbook.
- The broad tooling-directory pytest run on the earlier candidate was interrupted after 92 failures and 251 passes (exit 130); shown failures were historical workflow assertions against untouched skill files. The Story 10.1 focused generator test, Release solution build, and AC commands passed on that earlier candidate, but must be rerun on the new one.

## Spec Change Log

- **Review loop 1 (2026-10-10).** Trigger: B01-B02, B05, B09-B10, B12, V03, E01, E03-E04, and E10 exposed gaps in process-tree and capture limits, historical blob path authority, predecessor and artifact binding, evaluated project graph checks, and cross-platform fixtures. Amended the non-frozen execution tasks and acceptance criteria to require bounded descendant cleanup, committed-tree blob validation, ancestry and exact candidate/path checks, evaluated MSBuild/dependency checks, and portable fixtures; folded the other verified review findings into the same re-derivation. Avoid the known-bad state where AC-01..07 pass while a descendant survives, lazy fetch changes facts, a production dependency ships, or a contradictory final record verifies. **KEEP:** the five public helper names, two neutral test-only projects, CI and solution wiring, 30-second bound and strict UTF-8 raw facts, typed status model, AC-02..06 fixture coverage, deterministic candidate-bound record pair and predecessor digests, unchanged 7.4/9.2 records, and untouched conformance tiers/readers.
- **Compatibility correction (2026-10-10).** The accepted 7.4 record's candidate `ba1b47622ad8d72e0ea70f01e4e695e49413c1ee` is on a divergent historical branch (`git merge-base --is-ancestor` exits 1 against the current candidate), although its validated pair is committed in the current tree. The Story 10.1 contract requires current compatible records and exact digests, not linear candidate ancestry. Replaced the ancestry task with committed-pair, PASS, exact-ID, resolvable-candidate, and authority-field checks. This preserves the accepted 7.4/9.2 bytes while rejecting an unrelated record with mismatched authority.
- **Review loop 2 (2026-10-10).** Trigger: L1-B03, L1-B09-B10, and L1-E04 exposed unbounded cleanup, raw diff relations/config drift, and unsupported shallow-history classifications; L1-E01 found a normal-completion overflow path. Amended the non-frozen tasks and acceptance criteria to bound startup/cleanup, check overflow on every completion path, validate related raw fields and deterministic rename behavior, classify unavailable history only from proven shallow ancestry, compare the full authority bundle digest, and exercise broken-pair and dependency faults. Avoid the known-bad state where a truncated command returns success, invalid Git bytes become facts, a random object ID becomes unavailable history, or a predecessor from another authority bundle passes. **KEEP:** the five public helpers in two test-only projects, solution/CI wiring, CRLF C# files, the 16 MiB aggregate capture limit, 30-second execution limit, strict UTF-8/NUL raw decoding, typed failures, process group/job cleanup for ordinary children, hostile-config and partial-clone fixtures, evaluated project and exact artifact checks, the deterministic 7/7 candidate-bound pair, unchanged 7.4/9.2 bytes, and untouched conformance tiers/readers. The accepted 7.4 branch divergence remains permitted by the compatibility correction.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B01 exited parent, live child | high | bad_spec | `GitFacts.RunAsync` skips `Kill` when `HasExited` is true although inherited pipes can keep `CopyToAsync` open until timeout; the descendant survives. |
| B02 unlimited capture | medium | bad_spec | Both streams copy into unbounded `MemoryStream`s; a large blob or hostile fixture can exhaust memory before the 30-second limit. |
| B03 path exceptions | medium | patch | `RepositoryLocator` passes NUL and inaccessible path cases into `Path.GetFullPath`/`LinkTarget`, which can throw instead of returning typed failure. |
| B04 symlink root | medium | patch | `RepositoryLocator` checks descendants only; calling it with a symlinked root returns a path outside the physical root. |
| B05 historical blob/current symlink | medium | bad_spec | `ReadBlobAsync` calls the worktree link check before reading a committed tree; a current symlink rejects a valid historical blob. |
| B06 shallow typo | medium | patch | Any unresolved revision in a shallow repository becomes `HISTORY_UNAVAILABLE`, even a non-object name that is invalid. |
| B07 raw diff grammar | medium | patch | `TrimStart(':')` accepts a missing/extra colon, and status/path are not validated, allowing malformed output to become a fact. |
| B08 raw tree grammar | medium | patch | Tree parsing accepts arbitrary octal mode/type combinations and empty paths from malformed output. |
| B09 predecessor ancestry | high | bad_spec | `v2_verified_predecessor` checks pair/schema/digest but not authority compatibility. The suggested ancestry check is invalid for the accepted divergent 7.4 record; current-tree pair, PASS, and matching authority provide the required compatibility. |
| B10 evaluated project graph | high | bad_spec | The generator reads literal first-group properties and `ProjectReference` only; later MSBuild overrides or other references can defeat the boundary. |
| B11 symlinked artifact parent | medium | patch | `assembly_file.is_file()` follows parent links and `.is_symlink()` checks only the final DLL, so a linked `bin` directory can escape. |
| B12 record schema bindings | medium | bad_spec | The new schema permits arbitrary/repeated artifact paths and a `sourceRevisionId` differing from `candidate.commit`; pair verification alone cannot reject that contradiction. |
| V01 replacement-object fixture | medium | patch | Verified test gap: AC-05 never creates a replacement ref; removing `GIT_NO_REPLACE_OBJECTS` would preserve its pass while historical bytes change. |
| V02 child-start fixture | medium | patch | Verified test gap: AC-04 checks only absence of a delayed marker and can pass when cancellation occurs before the child starts. |
| V03 declared production references | high | bad_spec | Verified test gap: an unused `<Reference>` or `<PackageReference>` can be absent from compiled assembly metadata and evade the generator's `ProjectReference` check. |
| V04 trailing-space root | low | patch | `root.Trim()` removes a valid trailing space from `git rev-parse` output before exact-root comparison. |
| E01 exited parent, live child | high | bad_spec | The edge-case claim matches B01: `HasExited` prevents descendant termination after parent exit. |
| E02 promisor lazy fetch | medium | patch | A local partial clone with reachable promisor remote returned missing blob bytes; setting `GIT_NO_LAZY_FETCH=1` made `cat-file` fail. |
| E03 pack/publish override | high | bad_spec | The edge-case claim matches B10: later property groups can override the literal first-group values. |
| E04 predecessor ancestry | high | bad_spec | The accepted 7.4 branch diverges, so ancestry is not required; the underlying compatibility gap is missing authority-field validation after pair verification. |
| E05 fallback cancellation | medium | patch | `ResolveRevisionAsync` converts a cancelled/timed-out shallow-probe call into `REVISION_INVALID` instead of forwarding its status. |
| E06 missing raw header | medium | patch | The edge-case claim matches B07: `TrimStart(':')` accepts a header with no colon. |
| E07 trailing-space root | low | patch | The edge-case claim matches V04: `.Trim()` removes valid path whitespace. |
| E08 NUL path | medium | patch | The edge-case claim matches B03: NUL reaches `Path.GetFullPath` and throws. |
| E09 duplicate tree path | medium | patch | `ReadBlobAsync` uses `SingleOrDefault`; a malformed tree with duplicate paths throws instead of returning typed invalid output. |
| E10 Windows fixtures | medium | bad_spec | AC-03/04 use Unix-executable Python scripts and AC-05 uses a literal backslash filename, so those tests fail before checking Git behavior on Windows. |
| L1-B01 Windows job enrollment race | low | rejected | `Process.Start` precedes `AssignProcessToJobObject`, so a custom executable could spawn before enrollment; the raw-fact Git commands do not spawn a child and closing this tiny fixture-only race requires native suspended startup. |
| L1-B02 detached Unix descendant | low | rejected | A child can create a new session and evade the original process group; the raw-fact Git commands do not detach children and tracking intentionally detached processes requires substantial platform-specific machinery. |
| L1-B03 deadline during cleanup | medium | bad_spec | The 30-second timer starts after `Process.Start`; after cancellation, `WaitForExitAsync(None)` can wait indefinitely if termination fails, then pipe draining gets two more seconds. |
| L1-B04 external setsid dependency | low | rejected | On Unix `ProcessStartInfo` launches `setsid`; a minimal host lacking it cannot run raw facts, but the governed CI and supported Linux test hosts include the utility, and a replacement needs platform-specific process launch code. |
| L1-B05 linked root ancestor | medium | patch | `RepositoryLocator` rejects a link at `fullRoot` but accepts an alias through a linked ancestor, so a caller can bypass the physical-root rule. |
| L1-B06 malformed shallow revision | medium | patch | `HEAD~bogus` enters the `StartsWith("HEAD~")` fallback in a shallow repository and is reported `HISTORY_UNAVAILABLE` despite invalid syntax. |
| L1-B07 revision output whitespace | medium | patch | `resolved.Trim()` turns a response with leading spaces or extra newlines into a valid object ID, accepting malformed output. |
| L1-B08 impossible status score | medium | patch | `IsDiffStatus` accepts `R999` and `M123` because it checks digit count only; these cannot be valid similarity scores. |
| L1-B09 raw diff field relations | medium | bad_spec | Independent field checks accept an `A` status with a nonzero source mode/object; a forged Git response becomes a fact. |
| L1-B10 rename configuration | medium | bad_spec | `ReadDiffAsync` does not set rename behavior, so `diff.renames` from repository config can change an R record into D/A records for the same commits. |
| L1-B11 predecessor bundle digest | medium | patch | The predecessor's `bundleDigest` is present but omitted from the compatibility comparison, allowing a different authority bundle with matching triple fields. |
| L1-B12 pair versus repository facts | false | rejected | `v2_verify_pair` explicitly verifies canonical JSON/Markdown digest projection without repository context; generation remeasures predecessor and artifacts. It does not claim to be a repository-facts verifier. |
| L1-V01 predecessor pair fault test | medium | patch | The verification-gap reviewer confirmed the Story 10.1 test stubs `v2_verified_predecessor`; no malformed committed predecessor pair is exercised. |
| L1-V02 dependency negative tests | medium | patch | The verification-gap reviewer confirmed that package and project dependency rejection have no negative fixture, though pack and assembly references are tested. |
| L1-V03 detached descendant test | low | rejected | This is the same intentionally detached Unix descendant as L1-B02; the ordinary started-child fixture covers the process group behavior Git can use. |
| L1-E01 overflow after normal completion | high | patch | `CaptureAsync` sets `overflow` and returns; if `WaitForExitAsync` and the peer capture have already completed, `Task.WhenAll` succeeds and the runner returns truncated output without checking the flag. |
| L1-E02 Windows job enrollment race | low | rejected | Same startup race and scope as L1-B01; closing it requires native suspended process creation beyond the raw-fact Git commands. |
| L1-E03 detached Unix descendant | low | rejected | Same new-session descendant as L1-B02; it is outside the process behavior of raw Git fact commands. |
| L1-E04 unknown object in shallow repository | medium | bad_spec | A random 40-digit hex revision is classified `HISTORY_UNAVAILABLE` in a shallow clone even when it is not a known missing ancestor; the current fallback cannot distinguish it from an actually missing parent. |
| L1-E05 linked root ancestor | medium | patch | Same linked-ancestor bypass as L1-B05; `fullRoot` alone is checked for a link. |
| L1-E06 artifact parent replacement race | low | rejected | The symlink-parent check and stable file snapshot are separate, allowing a coordinated swap during the small interval; the scenario requires a concurrent hostile local writer and a platform-specific handle-based redesign. |
| L1-E07 pair versus repository facts | false | rejected | Same claim as L1-B12; pair verification measures internal digests and exact 10.1 paths/revision, while generator validates repository facts. |

## Verification

**Commands:**
- `dotnet restore Hexalith.Conversations.slnx` and `dotnet build Hexalith.Conversations.slnx --configuration Release` -- both new projects build.
- Run the exact AC-10.1-01 through AC-10.1-07 commands in `_bmad-output/planning-artifacts/v9/story-contracts/10.1.json` -- each exits `0` with the contracted evidence.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- focused generator behavior passes.
