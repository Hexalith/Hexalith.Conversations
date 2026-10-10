---
title: 'Story 10.1: Neutral TestSupport helpers and safe Git facts'
type: 'feature'
created: '2026-10-10'
status: 'done'
baseline_commit: '2f2621aa4b40b0d44c62bf5e1e98727e7a182fec'
route: 'dispatch'
review_loop_iteration: 4
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
- [x] `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` and `RepositoryLocator.cs`, `GitFacts.cs`, `EvidenceManifest.cs`, `BoundaryAssertions.cs`, `AssertionLedger.cs` in that directory -- create the non-shipping project and five one-type-per-file helper surfaces; provide safe repository, process, revision, diff, tree, and blob facts and usable neutral manifest/assertion structures for later stories. Repository/path failures must be typed, including NUL, inaccessible paths, root symlinks and linked root ancestors, and escaping symlink parents; preserve valid path whitespace. Historical blob lookup must validate the committed tree rather than the current worktree. Strictly validate raw revision lines, diff status/mode/object relationships, tree records, and duplicates. Use deterministic rename behavior independent of repository config. A shallow repository may report a revision unavailable only when a reachable shallow boundary proves the missing ancestor; malformed or unrelated revisions are invalid. Propagate cancellation/timeout through fallback probes.
- [x] `GitFacts.cs` and related process code -- enforce a fixed output byte budget while draining both streams, including a success-path overflow check when all tasks finish normally. Use a 30-second execution limit and bounded cleanup waits; never block indefinitely after a failed termination and never return success with incomplete capture. Terminate descendants even if the Git parent already exited and a child inherited its pipes. Disable promisor lazy fetching and object replacements as part of hostile-environment isolation.
- [x] `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj`, `TestSupportSurfaceTests.cs`, and `GitFactsTests.cs` -- add xUnit v3 fixtures for surface, stream pressure, cancellation/timeout, hostile config, raw UTF-8 facts, and typed unavailable/invalid cases; restore fixture bytes. Prove a descendant started and exited after cancellation, output overflow never returns success, replacement refs cannot alter historical bytes, no lazy fetch occurs in a partial clone, a current symlink does not block a historical blob, and malformed Git records return typed failures. Include malformed revision/score/cross-field and rename-config fixtures. Make fixtures portable across Windows and Unix; use a literal-backslash path only where the platform permits it.
- [x] `Hexalith.Conversations.slnx` and `.github/workflows/ci.yml` -- include both projects in the solution and run the new tests in CI.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and focused generator tests -- bind Story 10.1 predecessor digests and project/assembly/result hashes without changing prior record meanings. Validate evaluated MSBuild pack/publish properties and all declared production assembly/package/project dependencies; reject a linked parent directory for hashed artifacts. Require each predecessor to be a valid committed PASS pair in the Story 10.1 candidate tree, with the exact story ID, resolvable candidate commit, and all four `epic`, `architecture`, `planningCandidate`, and `bundleDigest` authority fields matching current Story 10.1 authority; historical predecessor branches may diverge. Exercise a broken committed JSON/Markdown predecessor pair and helper package/project dependency as negative tests. Require exact distinct project/result artifact paths and equality between `sourceRevisionId` and `candidate.commit` in both generation and pair verification.
- [x] `docs/release-evidence/story-10.1-final-record-v2.{json,md}` -- generate the deterministic pair from a committed local candidate after AC-01 through AC-06 pass; preserve the 7.4/9.2 records.
- [x] Tighten Git-fact integrity: classify a shallow expression unavailable only if the relevant parent header exists at a marked boundary, including `HEAD^2` on a shallow merge; reject impossible M/T mode relationships, repeated-space tree headers, and all-zero tree object IDs; use path sets so valid bounded output cannot cause quadratic parse work. Resolve one historical blob with an exact path-specific tree query so unrelated tree size cannot exhaust the capture limit. Return typed invalid for null command arguments and preserve typed failure plus handle cleanup when Windows job enrollment fails.
- [x] Extend Story 10.1 fixtures to compare exact stdout/stderr bytes, exercise executable/symlink/gitlink/deletion/type-change raw modes, check shallow-root and shallow-merge parent distinctions, and prove a small historical blob succeeds even when an unrelated full-tree response would exceed the output budget. Run the contracted AC-02..06 selectors after these changes.
- [x] Check both Debug and Release evaluated TestSupport project graphs for pack/publish and neutral dependencies; verify predecessor PASS ledgers have only PASS assertion states. Re-derive Story 10.1 facts in the inserted-record verification route and add a negative fixture for a self-consistent pair whose artifact binding no longer matches the repository.
- [x] Preserve Story 10.1's candidate-bound record through the final sprint lifecycle transition: resolve its actual tracked sprint key and accept the tracker's current timestamp format while allowing only the story status and update timestamp to change.

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
- Given a release candidate and predecessor records, when generation or inserted-record verification runs, then committed-pair currency and authority compatibility, exact path/revision bindings, and evaluated non-shipping/dependency rules are rechecked; pair verification independently checks canonical bytes, digests, exact paths, and source revision equality.
- Given a shallow root or merge boundary, when a missing parent is requested, then only an actual parent header produces `HISTORY_UNAVAILABLE`; malformed or absent parents are invalid. A small historical blob remains readable despite a large unrelated tree.
- Given Story 10.1's inserted final record, when verification runs, then its predecessor and artifact facts are freshly re-derived; a self-consistent pair with stale bindings fails.
- Given Story 10.1 is moved to `review`, when inserted-record verification runs after the lifecycle commit, then the candidate is retained and the committed sprint change is accepted only for the tracked Story 10.1 row and update timestamp.

## Implementation Notes

- Review loop 2: candidate `4f57a68a08b842b160c6b9c12fca6e82134da78f` and its pair at `40d0168` passed AC-01..07, exact solution restore and Release build, focused TestSupport 5/5, focused generator 4/4, and pair/schema checks. All five TRX files matched record digests; the 7.4/9.2 predecessor bytes were unchanged. That candidate was withdrawn from the working tree because the second review found further safeguards to derive. Its local commits remain for traceability.
- Review loop 3: candidate `e4fc5af961947c0d7e321e240f7d07c131d1d89d` and pair commit `4f8b77e96a4172d840479146d43f7cee143d6dd1` passed exact solution restore/build and AC-01..07, focused generator 5/5, schema/pair/digest checks, and summary `7/7/0/0/0/0`. The second review identified further integrity cases; those files were withdrawn from the working tree for re-derivation. The commits remain local for traceability. Keep predecessor bytes and CRLF C# formatting.
- The full generator test file on that candidate was interrupted after 153 passes and one unrelated historical workflow assertion failure against an untouched skill file. The earlier broad tooling-directory pytest run was also interrupted after historical workflow failures; those are not Story 10.1 acceptance gates under the current-change runbook.
- The broad tooling-directory pytest run on the earlier candidate was interrupted after 92 failures and 251 passes (exit 130); shown failures were historical workflow assertions against untouched skill files. The Story 10.1 focused generator test, Release solution build, and AC commands passed on that earlier candidate, but must be rerun on the new one.
- The `583b7d9` source candidate and `f6aa0e2` record passed AC-01..07 and inserted verification before the final sprint transition. Lifecycle verification then exposed a sprint-key and timestamp mismatch in the generator; retract that pair and regenerate it from a corrected committed source candidate.

- Review loop 4 patch: tightened impossible raw Git record rejection, capped shallow boundary reads, propagated parse cancellation, cross-checked the Story 10.1 pair and predecessor scenario counts, and added timeout, hostile object-directory, and Windows cleanup checks. The prior record was retracted so a new candidate can bind these changes.

## Spec Change Log

- **Review loop 1 (2026-10-10).** Trigger: B01-B02, B05, B09-B10, B12, V03, E01, E03-E04, and E10 exposed gaps in process-tree and capture limits, historical blob path authority, predecessor and artifact binding, evaluated project graph checks, and cross-platform fixtures. Amended the non-frozen execution tasks and acceptance criteria to require bounded descendant cleanup, committed-tree blob validation, ancestry and exact candidate/path checks, evaluated MSBuild/dependency checks, and portable fixtures; folded the other verified review findings into the same re-derivation. Avoid the known-bad state where AC-01..07 pass while a descendant survives, lazy fetch changes facts, a production dependency ships, or a contradictory final record verifies. **KEEP:** the five public helper names, two neutral test-only projects, CI and solution wiring, 30-second bound and strict UTF-8 raw facts, typed status model, AC-02..06 fixture coverage, deterministic candidate-bound record pair and predecessor digests, unchanged 7.4/9.2 records, and untouched conformance tiers/readers.
- **Compatibility correction (2026-10-10).** The accepted 7.4 record's candidate `ba1b47622ad8d72e0ea70f01e4e695e49413c1ee` is on a divergent historical branch (`git merge-base --is-ancestor` exits 1 against the current candidate), although its validated pair is committed in the current tree. The Story 10.1 contract requires current compatible records and exact digests, not linear candidate ancestry. Replaced the ancestry task with committed-pair, PASS, exact-ID, resolvable-candidate, and authority-field checks. This preserves the accepted 7.4/9.2 bytes while rejecting an unrelated record with mismatched authority.
- **Review loop 2 (2026-10-10).** Trigger: L1-B03, L1-B09-B10, and L1-E04 exposed unbounded cleanup, raw diff relations/config drift, and unsupported shallow-history classifications; L1-E01 found a normal-completion overflow path. Amended the non-frozen tasks and acceptance criteria to bound startup/cleanup, check overflow on every completion path, validate related raw fields and deterministic rename behavior, classify unavailable history only from proven shallow ancestry, compare the full authority bundle digest, and exercise broken-pair and dependency faults. Avoid the known-bad state where a truncated command returns success, invalid Git bytes become facts, a random object ID becomes unavailable history, or a predecessor from another authority bundle passes. **KEEP:** the five public helpers in two test-only projects, solution/CI wiring, CRLF C# files, the 16 MiB aggregate capture limit, 30-second execution limit, strict UTF-8/NUL raw decoding, typed failures, process group/job cleanup for ordinary children, hostile-config and partial-clone fixtures, evaluated project and exact artifact checks, the deterministic 7/7 candidate-bound pair, unchanged 7.4/9.2 bytes, and untouched conformance tiers/readers. The accepted 7.4 branch divergence remains permitted by the compatibility correction.
- **Review loop 3 (2026-10-10).** Trigger: L2-B02, L2-B07-B08, and L2-B12 exposed missing shallow merge parents, configuration-specific project shipping, an incomplete inserted-record verification route, and whole-tree blob lookup; other verified patch findings refine the same Git and evidence surfaces. Amended the non-frozen execution tasks and acceptance criteria to require parent-header proof for shallow expressions, exact path-specific blob lookup, both Debug and Release boundary evaluation, full 10.1 inserted-record rederivation, strict raw grammar and linear duplicate checks, and stronger fixture evidence. Avoid the known-bad state where an absent parent is called unavailable, a small blob is blocked by unrelated tree size, a Debug packable helper receives PASS, or a stale 10.1 pair verifies. **KEEP:** all prior logged preservation instructions, especially the five helper names, two neutral projects, tested 30-second process execution and 16 MiB capture limits, candidate-bound 7/7 pair, 7.4/9.2 compatibility including divergent 7.4 ancestry, untouched predecessor bytes and conformance tiers/readers, and CRLF C# files.
- **Review loop 4 (2026-10-10).** Trigger: the final lifecycle check found that generic successor retention inferred a sprint key from this spec's filename and parsed only date-only timestamps, while Story 10.1's tracker uses a different key and a date-time value. Require the actual Story 10.1 tracker key and the existing timestamp format in retained-candidate validation, with a focused negative fixture. Avoid a PASS pair that becomes unverifiable when the story moves to `review`. **KEEP:** the complete reviewed helper implementation and tests, deterministic candidate-bound pair, unchanged 7.4/9.2 records, and all earlier preservation instructions.

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
| L2-B01 shallow root with no parent | medium | patch | `IsProvenShallowAncestorAsync` returns true for `HEAD~1` at a marked one-commit root without checking that the root commit has any parent. |
| L2-B02 shallow merge second parent | medium | bad_spec | The fallback recognizes only first-parent `HEAD~n`/caret chains, so a missing `HEAD^2` parent of a marked shallow merge is classified invalid despite a visible parent header. |
| L2-B03 raw M/T mode relation | medium | patch | `IsDiffRelation` accepts a `T` record with unchanged modes and an `M` record changing regular file to gitlink, both impossible relationships. |
| L2-B04 repeated tree header spaces | medium | patch | `Split(..., RemoveEmptyEntries)` accepts repeated separators in a forged tree header, contrary to strict raw-output parsing. |
| L2-B05 quadratic duplicate detection | medium | patch | `entries.Any` scans every prior diff/tree entry; a legal 16 MiB response with many paths can consume quadratic parse time after the process deadline. |
| L2-B06 Windows job termination failure | low | rejected | `TerminateJobObject` return is ignored and a failed OS termination can leave a child alive. The runner already reports timeout/cancellation, and recovery from a denied or invalid job handle requires platform-specific fallback beyond normal Git behavior. |
| L2-B07 Debug pack/publish override | medium | bad_spec | The generator evaluates `IsPackable` and `IsPublishable` only under Release, allowing a Debug-only override to violate the neutral project boundary while AC-01/07 pass. |
| L2-B08 inserted-record rederivation | medium | bad_spec | The generic inserted-record verifier rederives 8.2/9.2 but not the new 10.1 record; a self-consistent inserted pair can pass without fresh 10.1 artifact checks. |
| L2-B09 predecessor ledger state | medium | patch | `v2_10_1_facts` checks predecessor summary and scenario results but not each passing scenario's assertion-ledger states, so a canonical pair with a FAIL assertion can be accepted. |
| L2-B10 concurrent stream byte content | medium | patch | AC-03 checks only stdout/stderr lengths; equal-length corruption or swapped streams would preserve the test result. |
| L2-B11 raw mode fixture breadth | medium | patch | AC-05 proves `100644` but does not cover executable, symlink, gitlink, deletion, or type-change modes used by later Git-fact readers. |
| L2-B12 blob lookup scans full tree | medium | bad_spec | `ReadBlobAsync` calls recursive `ReadTreeAsync`, so a small historical blob becomes unavailable when unrelated tree output exceeds the 16 MiB cap. |
| L2-B13 Python on Windows | low | rejected | The fixtures require Python on Windows, but the Story 10.1 generator and contract already require Python as a project test-tool prerequisite; supplying a second embedded process fixture is more than a direct correction. |
| L2-E01 null command arguments | low | patch | Public `RunAsync` can receive a runtime-null argument sequence and `foreach` throws instead of returning a typed invalid result. |
| L2-E02 failed Windows job assignment cleanup | medium | patch | When `AssignProcessToJobObject` fails after the process exits, `process.Kill` can throw before `CloseHandle`, leaking the job handle and interrupting typed failure handling. |
| L2-E03 raw T mode relation | medium | patch | Same malformed type-change relationship as L2-B03; identical source and destination modes are accepted for `T`. |
| L2-E04 all-zero tree object | medium | patch | `IsObjectId` accepts an all-zero hash in a non-empty tree entry, allowing an impossible raw tree record to succeed. |
| L2-E05 shallow root with no parent | medium | patch | Same missing-parent check as L2-B01; `HEAD~1` after a shallow root is marked unavailable without a parent header. |
| L3-B01 blocking argument iterator | low | rejected | `ToArray()` precedes the execution timer and a caller-supplied blocking enumerable can hang, but normal callers use finite arrays; bounding arbitrary synchronous iterator code requires another thread and cannot stop that iterator. |
| L3-B02 zero revision object ID | medium | patch | `ResolveRevisionAsync` accepts a forged all-zero full hash because `IsObjectId` checks shape only, though zero cannot name a commit. |
| L3-B03 blob object hash mismatch | low | rejected | The fake executable can return bytes unrelated to a tree object, but real Git `cat-file blob <oid>` reads that OID from its object database; a malicious replacement executable is outside the hostile-config boundary and duplicate hash verification adds format-specific machinery. |
| L3-B04 no-op M diff | medium | patch | `IsDiffRelation` permits equal source/destination mode and object ID under `M`, so a forged unchanged path is reported as changed. |
| L3-B05 rename score relation | medium | patch | `R0`..`R49` pass despite `--find-renames=50%`, and `R100` with different object IDs passes though exact similarity requires equal content. |
| L3-B06 rename limit config | medium | patch | The command pins `diff.renames` and `--find-renames=50%` but leaves `diff.renameLimit` to repository config, which can change a large rename set into D/A records. |
| L3-B07 general shallow expressions | low | rejected | A non-HEAD ref or composite expression reaching missing history is typed invalid rather than unavailable; it never yields success, and full Git revision-expression proof exceeds the narrowly demonstrated HEAD and boundary forms. |
| L3-B08 CI-conditioned packability | low | rejected | Debug and Release are checked in the generator's actual environment, and the current project declares both properties unconditionally false; an arbitrary future environment-specific override is not a present violation and cannot be exhaustively enumerated. |
| L3-B09 contract/predecessor paths | medium | patch | `v2_10_1_exact_paths` fixes project/result paths but accepts a self-consistent pair with a foreign contract path or predecessor-record path. |
| L3-B10 top-level predecessor mismatch | medium | patch | Pair verification does not compare top-level `predecessors` with the 7.4/9.2 entries in `testSupportExecution`, so contradictory pair fields can verify. |
| L3-B11 Windows symlink fixture | medium | patch | The linked-parent Python test creates a symlink without accounting for Windows hosts that lack symlink privilege, so it can fail before checking containment. |
| L3-E01 linked root in raw RunAsync | low | rejected | Direct `RunAsync` can execute from a symlink alias, but high-level fact methods call `ResolveRepositoryAsync`, which rejects linked roots/ancestors before returning repository facts. |
| L3-E02 Windows enrollment race | low | rejected | carried: Same location and outcome as L1-B01; Git starts before job assignment and a fast custom child could escape, while the raw fact commands do not spawn children. |
| L3-E03 octopus shallow parent | medium | patch | `IsProvenShallowAncestorAsync` recognizes only `HEAD^2`; a marked octopus merge with an actual third parent is incorrectly classified invalid. |
| L3-E04 no-op M diff | medium | patch | Same forged unchanged `M` record as L3-B04; mode/object equality currently passes. |
| L3-E05 tree file ancestor conflict | medium | patch | `ParseTree` rejects duplicates but accepts a blob path and its descendant, an impossible tree shape, as two successful entries. |
| L3-E06 blob object hash mismatch | low | rejected | Same fake-executable mismatch as L3-B03; real Git resolves `cat-file` by the requested object ID. |
| L3-E07 mutable assertion results | medium | patch | `AssertionLedger.Results` exposes the backing `Dictionary` as `IReadOnlyDictionary`; a caller can cast it and rewrite a recorded failure. |
| L3-E08 mutable manifest digests | medium | patch | `EvidenceManifest.Digests` likewise exposes its backing dictionary, allowing a caller to bypass duplicate checks and replace declared hashes. |
| L3-V01 recursive tree fixture | medium | patch | The verification-gap reviewer confirmed the real AC-05 repository contains only top-level files, so removing `-r` could break nested tree reads without a test failure. |
| L3-V02 successful parent leaves child | medium | patch | When a successful parent exits and a child closes inherited pipes, `RunAsync` returns without calling `TerminateTree`; `GitProcessJob.Dispose` only closes the handle, leaving that child alive. |
| L4-P01 lifecycle sprint key and timestamp | high | bad_spec | The retained-candidate route derives `10-1-neutral-testsupport-and-safe-git-facts` from the spec filename although the tracker row is `10-1-provide-neutral-testsupport-helpers-and-a-safe-git-facts-runner`; its `last_updated` parser also rejects the tracker's `MM-DD-YYYY HH:MM` value. A lifecycle commit would invalidate the otherwise passing pair. |
| L4-B01 unexpected copy status | medium | patch | `ReadDiffAsync` requests rename detection without copy detection, yet `IsDiffStatus` and `IsDiffRelation` accept a forged `C` record as a successful fact. |
| L4-B02 same-path rename | medium | patch | A forged `R` record with equal source and destination paths passes the raw parser, although it cannot represent a rename. |
| L4-B03 partial rename with equal objects | medium | patch | `R75` with equal object IDs passes; identical content requires score `R100`. |
| L4-B04 mixed object-ID widths | low | rejected | Real Git emits IDs in its repository object format for both revision and tree commands. A forged executable could emit a foreign-width tree ID, but probing object format throughout the parser adds machinery for a case outside ordinary Git behavior. |
| L4-B05 unbounded shallow file | medium | patch | `IsProvenShallowAncestorAsync` reads `.git/shallow` with `File.ReadAllBytes` outside the command output cap; a large local file can exhaust memory. |
| L4-B06 cancellation during parse | medium | patch | After a bounded Git command completes, diff/tree parsing can inspect many paths without checking a newly cancelled token. |
| L4-B07 production-to-helper reference | low | rejected | No production project in the committed candidate references TestSupport. A later source change would require a new candidate and review; scanning every production target for a hypothetical future inverse reference expands this story's neutral helper boundary. |
| L4-B08 altered predecessor pair | low | rejected | The candidate preserves the accepted 7.4/9.2 bytes, and the generator verifies their committed pairs, authority, and digests. Hardcoding their current digests would forbid an independently approved future record revision. |
| L4-B09 forged assembly stamp text | low | rejected | The scanner can find a revision string in a fake binary, but the exact AC builds and test selectors produce the candidate assemblies before generation. Malicious replacement of an output binary is outside the ordinary local-build evidence model, as with the previously rejected fake Git executable. |
| L4-B10 execution-time binary digest | medium | defer | The generic pre-existing xUnit/TRX route binds the current candidate-stamped binary by path, revision, and time, while TRX itself carries no binary digest captured during execution. A trustworthy execution-time receipt would require a separate runner protocol. |
| L4-B11 inventory cross-field mismatch | medium | patch | A self-consistent pair can disagree between top-level `inventory.sha256` and `testSupportExecution.inventorySha256`; `v2_verify_pair` does not compare them. |
| L4-B12 PASS with nonzero exit | medium | patch | A self-consistent Story 10.1 pair can contain a `PASS` scenario with `exitCode: 1`; the pair verifier checks neither field relation. |
| L4-B13 scenario/result path mismatch | medium | patch | The five exact paths in `testSupportExecution.results` can disagree with their scenarios' `resultFile.path` while pair verification succeeds. |
| L4-E01 argument enumeration | low | rejected | carried: L3-B01 already recorded the same pre-start blocking enumerable claim and its low verdict; normal callers provide finite arrays. |
| L4-E02 Windows enrollment race | low | rejected | carried: L3-E02 already recorded the same pre-enrollment race; raw Git fact commands do not spawn descendants, and the ordinary process fixture is covered after enrollment. |
| L4-E03 zero-count predecessor summary | medium | patch | The predecessor check accepts `required == passed == 0` despite a nonempty scenario list; it must require positive counts matching the scenarios. |
| L4-E04 target-added production reference | low | rejected | The current helper has no target that adds a production reference, and its declared evaluated dependencies are neutral. A later imported target that mutates references after evaluation would be a new candidate requiring its own boundary review. |
| L4-V01 default timeout coverage | medium | patch | The test checks the 30-second constant and an explicit two-second timeout, but no hanging call exercises the default deadline used by fact readers. |
| L4-V02 Windows cleanup CI | medium | patch | The successful-parent descendant test runs in Ubuntu CI while `GitProcessJob.Terminate` is Windows-only; a Windows smoke job is needed to keep that branch checked. |
| L4-V03 object-directory isolation fixture | medium | patch | The hostile environment fixture sets `GIT_DIR` but not `GIT_OBJECT_DIRECTORY`; removal of the latter from the runner's ambient cleanup would go undetected. |

## Verification

**Commands:**
- `dotnet restore Hexalith.Conversations.slnx` and `dotnet build Hexalith.Conversations.slnx --configuration Release` -- both new projects build.
- Run the exact AC-10.1-01 through AC-10.1-07 commands in `_bmad-output/planning-artifacts/v9/story-contracts/10.1.json` -- each exits `0` with the contracted evidence.
- `uv run --frozen --no-sync python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_generate_story_record.py -k story101` -- focused Story 10.1 generator behavior passes.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 10.1 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `10.1`
- Candidate: `5a36e01a4094c6bb4ac1d02afaeb8807e441aaa6`
- JSON content SHA-256 (all three digest fields zeroed): `75a1fb2305a9fd595883f387cafbcec43f82649168eb8430a2b0cfcd706ade0d`

## Authority

| Field | Value |
| --- | --- |
| Epic | `epic-6-authority-2026-08-03-v10` |
| Architecture | `conversations-architecture-2026-08-03-v10` |
| Planning candidate | `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` |
| Bundle digest | `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055` |

## Root gitlinks

| Path | Mode | Commit |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `160000` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `160000` | `9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b` |
| `references/Hexalith.Commons` | `160000` | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| `references/Hexalith.EventStore` | `160000` | `244ea8469ad37556968811e87fea916965346e8d` |
| `references/Hexalith.Folders` | `160000` | `4d2d7d820431cb354804583ee5c217ec9ce11fb6` |
| `references/Hexalith.FrontComposer` | `160000` | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| `references/Hexalith.Memories` | `160000` | `76894f03a5a58e2d2e975dc0b127d7ee20681bfa` |
| `references/Hexalith.Parties` | `160000` | `56cb4401c4179d431385ad3d931fefa402e4fc5a` |
| `references/Hexalith.Projects` | `160000` | `fb25a85fe3099fc49b17afcb188197678f4eb130` |
| `references/Hexalith.Tenants` | `160000` | `66072cc410834e2c34f7d30f121e447940ea6fc6` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-10.1-ENTRY-v1` | `74c4c5e9e70cb86648dc753c5edc97bdb8d598ad2a4aba55d3670794e990ca84` |

## Predecessors

- `7.4`
- `9.2`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-10.1-01` | `0` | `PASS` | `none` | `1` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `d506c33ccb285883416fa7577b5adee905b497dfcb34b85630811e1c58bba322` |
| `AC-10.1-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-02.trx` | `7f8eec101c1637f8bc711846705850fc0b1e7ece63ef6f1cb1691436cfb1c4b1` |
| `AC-10.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-03.trx` | `8aacb0a374d41d71d68f5c579a4ac42139764855886651e50f4fa77e6ed7bb85` |
| `AC-10.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-04.trx` | `2f089689f2f3d77f97f654f2b411dcd7db7baaf4369d90938928f42a67254adb` |
| `AC-10.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-05.trx` | `4538879ab3ec6de453b1879903a3e06987bc331ae53f9d4712a18c0f496d1e8f` |
| `AC-10.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-06.trx` | `0b3742b50ded52b8cb3416fed8bc4b85cc8ada8c07c6198c7307fe65c23f0c66` |
| `AC-10.1-07` | `0` | `PASS` | `none` | `9` | none | none |

### `AC-10.1-01`

Command: `dotnet build tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj --configuration Release --no-restore`

| Bound output | SHA-256 |
| --- | --- |
| `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `d506c33ccb285883416fa7577b5adee905b497dfcb34b85630811e1c58bba322` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-01#0001` | `build::tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `PASS` |

### `AC-10.1-02`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.TestSupportSurfaceTests.RequiredHelpersShouldExistExactlyOnce -trx artifacts/v9/10.1/AC-10.1-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-02.trx` | `7f8eec101c1637f8bc711846705850fc0b1e7ece63ef6f1cb1691436cfb1c4b1` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-02#0001` | `Hexalith.Conversations.TestSupport.Tests.TestSupportSurfaceTests.RequiredHelpersShouldExistExactlyOnce` | `PASS` |

### `AC-10.1-03`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldDrainStdoutAndStderrConcurrently -trx artifacts/v9/10.1/AC-10.1-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-03.trx` | `8aacb0a374d41d71d68f5c579a4ac42139764855886651e50f4fa77e6ed7bb85` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-03#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldDrainStdoutAndStderrConcurrently` | `PASS` |

### `AC-10.1-04`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldBoundAndCancelExecution -trx artifacts/v9/10.1/AC-10.1-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-04.trx` | `2f089689f2f3d77f97f654f2b411dcd7db7baaf4369d90938928f42a67254adb` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-04#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldBoundAndCancelExecution` | `PASS` |

### `AC-10.1-05`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldUseHardenedEncodingAndRawFacts -trx artifacts/v9/10.1/AC-10.1-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-05.trx` | `4538879ab3ec6de453b1879903a3e06987bc331ae53f9d4712a18c0f496d1e8f` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-05#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldUseHardenedEncodingAndRawFacts` | `PASS` |

### `AC-10.1-06`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.UnavailableHistoryShouldBeTypedNotSuccessful -trx artifacts/v9/10.1/AC-10.1-06.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-06.trx` | `0b3742b50ded52b8cb3416fed8bc4b85cc8ada8c07c6198c7307fe65c23f0c66` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-06#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.UnavailableHistoryShouldBeTypedNotSuccessful` | `PASS` |

### `AC-10.1-07`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.1.json --format bundle --output-json docs/release-evidence/story-10.1-final-record-v2.json --output-markdown docs/release-evidence/story-10.1-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-07#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-10.1-07#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-10.1-07#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-10.1-07#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-10.1-07#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-10.1-07#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-10.1-07#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-10.1-07#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-10.1-07#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |

## Story 10.1 TestSupport execution

- Candidate: `5a36e01a4094c6bb4ac1d02afaeb8807e441aaa6`
- Candidate rule: SC-10.1 is HEAD^{commit} at first final-record generation
- Frozen inventory SHA-256: `74c4c5e9e70cb86648dc753c5edc97bdb8d598ad2a4aba55d3670794e990ca84`
- Contract SHA-256: `7db86e53754b7f917435663639a65b3953f0cf9083d436334d67f7f14c105331`

| Predecessor | Record SHA-256 |
| --- | --- |
| `7.4` | `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290` |
| `9.2` | `46d2e2df3ca74e1dad7eafa782ac1e294e1eafcd4e43b14177ddf8a411bf0453` |

| Project | Project SHA-256 | Assembly | Assembly SHA-256 |
| --- | --- | --- | --- |
| `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `9f885c50d869285e6b280b0c135b2c1449cda6f8e863271ff6420924443be925` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `d506c33ccb285883416fa7577b5adee905b497dfcb34b85630811e1c58bba322` |
| `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj` | `6d93f6ea2dbca4fb89047836f9f10ea633f2996e257830c46eaba2aaef646bf2` | `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `6ac38022f525d6598d10578b61a43854a937d0ced14020b0866bed28d93623a0` |

| Result | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-02.trx` | `7f8eec101c1637f8bc711846705850fc0b1e7ece63ef6f1cb1691436cfb1c4b1` |
| `artifacts/v9/10.1/AC-10.1-03.trx` | `8aacb0a374d41d71d68f5c579a4ac42139764855886651e50f4fa77e6ed7bb85` |
| `artifacts/v9/10.1/AC-10.1-04.trx` | `2f089689f2f3d77f97f654f2b411dcd7db7baaf4369d90938928f42a67254adb` |
| `artifacts/v9/10.1/AC-10.1-05.trx` | `4538879ab3ec6de453b1879903a3e06987bc331ae53f9d4712a18c0f496d1e8f` |
| `artifacts/v9/10.1/AC-10.1-06.trx` | `0b3742b50ded52b8cb3416fed8bc4b85cc8ada8c07c6198c7307fe65c23f0c66` |

## Fault injection

No fault-injection result is bound to this record.

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-10.1-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-10.1-final-record-v2.md` |

## Rollback boundary

remove only both TestSupport projects, Story 10.1 results, and record; preserve both conformance tiers and every evidence reader.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `7` | `7` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
