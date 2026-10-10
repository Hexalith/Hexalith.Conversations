---
title: 'Story 10.1: Neutral TestSupport helpers and safe Git facts'
type: 'feature'
created: '2026-10-10'
status: 'in-progress'
baseline_commit: '2f2621aa4b40b0d44c62bf5e1e98727e7a182fec'
route: 'dispatch'
review_loop_iteration: 1
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
- [ ] `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` and `RepositoryLocator.cs`, `GitFacts.cs`, `EvidenceManifest.cs`, `BoundaryAssertions.cs`, `AssertionLedger.cs` in that directory -- create the non-shipping project and five one-type-per-file helper surfaces; provide safe repository, process, revision, diff, tree, and blob facts and usable neutral manifest/assertion structures for later stories. Repository/path failures must be typed, including NUL, inaccessible paths, root symlinks, and escaping symlink parents; preserve valid path whitespace. Historical blob lookup must validate the committed tree rather than the current worktree. Reject malformed/duplicate raw records with typed invalid status, distinguish a bad revision from unavailable shallow history, and propagate cancellation/timeout through fallback probes.
- [ ] `GitFacts.cs` and related process code -- enforce a fixed output byte budget while draining both streams, return typed failure on overflow, and terminate descendants within the 30-second bound even if the Git parent already exited and a child inherited its pipes. Disable promisor lazy fetching and object replacements as part of hostile-environment isolation.
- [ ] `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj`, `TestSupportSurfaceTests.cs`, and `GitFactsTests.cs` -- add xUnit v3 fixtures for surface, stream pressure, cancellation/timeout, hostile config, raw UTF-8 facts, and typed unavailable/invalid cases; restore fixture bytes. Prove a descendant started and exited after cancellation, replacement refs cannot alter historical bytes, no lazy fetch occurs in a partial clone, a current symlink does not block a historical blob, and malformed Git records return typed failures. Make fixtures portable across Windows and Unix; use a literal-backslash path only where the platform permits it.
- [ ] `Hexalith.Conversations.slnx` and `.github/workflows/ci.yml` -- include both projects in the solution and run the new tests in CI.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and focused generator tests -- bind Story 10.1 predecessor digests and project/assembly/result hashes without changing prior record meanings. Validate evaluated MSBuild pack/publish properties and all declared production assembly/package/project dependencies; reject a linked parent directory for hashed artifacts. Require each predecessor to be a valid committed PASS pair in the Story 10.1 candidate tree, with the exact story ID, resolvable candidate commit, and `epic`, `architecture`, and `planningCandidate` authority fields matching the Story 10.1 contract; historical predecessor branches may diverge. Require exact distinct project/result artifact paths and equality between `sourceRevisionId` and `candidate.commit` in both generation and pair verification.
- [ ] `docs/release-evidence/story-10.1-final-record-v2.{json,md}` -- generate the deterministic pair from a committed local candidate after AC-01 through AC-06 pass; preserve the 7.4/9.2 records.

**Acceptance Criteria:**
- Given the resolved TestSupport graph, when AC-10.1-01 builds it, then it passes and remains non-packable, non-publishable, and Conversations-assembly-free.
- Given the helper surface, when AC-10.1-02 inspects it, then all five required public names occur exactly once.
- Given full concurrent streams or a hung/cancelled process, when AC-10.1-03/04 run, then output is complete or the tree is bounded and classified.
- Given hostile Git configuration and unusual paths, when AC-10.1-05 runs, then raw facts and hashes match fixture bytes.
- Given unavailable or invalid Git inputs, when AC-10.1-06 runs, then no fixture reports success and root escape is explicit.
- Given six passing results and compatible 7.4/9.2 records, when AC-10.1-07 generates the pair, then it is `PASS` with summary `7/7/0/0/0/0` and all required bindings.
- Given a bad revision in a shallow repository or malformed raw Git output, when a helper reads it, then it returns typed invalid rather than successful or unavailable facts. Given cancelled fallback, its cancellation status survives.
- Given a valid historical blob whose current worktree path is a symlink, when read by revision, then committed bytes and SHA-256 are returned. Given a root or artifact parent symlink that escapes the requested repository, it is rejected.
- Given adversarial child processes and large concurrent output, when the process runner stops, then no started descendant remains and capture stays within the stated byte limit.
- Given a release candidate and predecessor records, when generation or pair verification runs, then committed-pair currency and authority compatibility, exact path/revision bindings, and evaluated non-shipping/dependency rules are checked; invalid bindings fail.

## Implementation Notes

- Review loop 1: the earlier candidate and record passed the contracted commands but were withdrawn from the working tree after review exposed missing safeguards. Their commits remain in local history for traceability. Re-derive from the amended tasks and produce a new candidate-bound pair. Preserve the 7.4 and 9.2 record bytes.
- The broad tooling-directory pytest run on the earlier candidate was interrupted after 92 failures and 251 passes (exit 130); shown failures were historical workflow assertions against untouched skill files. The Story 10.1 focused generator test, Release solution build, and AC commands passed on that earlier candidate, but must be rerun on the new one.

## Spec Change Log

- **Review loop 1 (2026-10-10).** Trigger: B01-B02, B05, B09-B10, B12, V03, E01, E03-E04, and E10 exposed gaps in process-tree and capture limits, historical blob path authority, predecessor and artifact binding, evaluated project graph checks, and cross-platform fixtures. Amended the non-frozen execution tasks and acceptance criteria to require bounded descendant cleanup, committed-tree blob validation, ancestry and exact candidate/path checks, evaluated MSBuild/dependency checks, and portable fixtures; folded the other verified review findings into the same re-derivation. Avoid the known-bad state where AC-01..07 pass while a descendant survives, lazy fetch changes facts, a production dependency ships, or a contradictory final record verifies. **KEEP:** the five public helper names, two neutral test-only projects, CI and solution wiring, 30-second bound and strict UTF-8 raw facts, typed status model, AC-02..06 fixture coverage, deterministic candidate-bound record pair and predecessor digests, unchanged 7.4/9.2 records, and untouched conformance tiers/readers.
- **Compatibility correction (2026-10-10).** The accepted 7.4 record's candidate `ba1b47622ad8d72e0ea70f01e4e695e49413c1ee` is on a divergent historical branch (`git merge-base --is-ancestor` exits 1 against the current candidate), although its validated pair is committed in the current tree. The Story 10.1 contract requires current compatible records and exact digests, not linear candidate ancestry. Replaced the ancestry task with committed-pair, PASS, exact-ID, resolvable-candidate, and authority-field checks. This preserves the accepted 7.4/9.2 bytes while rejecting an unrelated record with mismatched authority.

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

## Verification

**Commands:**
- `dotnet restore Hexalith.Conversations.slnx` and `dotnet build Hexalith.Conversations.slnx --configuration Release` -- both new projects build.
- Run the exact AC-10.1-01 through AC-10.1-07 commands in `_bmad-output/planning-artifacts/v9/story-contracts/10.1.json` -- each exits `0` with the contracted evidence.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- focused generator behavior passes.
