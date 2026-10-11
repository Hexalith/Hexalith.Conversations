---
title: 'Story 10.1: Neutral TestSupport helpers and safe Git facts'
type: 'feature'
created: '2026-10-10'
status: 'in-review'
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

### Review Findings

Code review of `2f2621a..fb2426a` (2026-10-11), full mode, all four layers completed: 0 decision-needed, 18 patch, 1 defer, 43 rejected.

- [x] [Review][Patch] (high) Windows CI job never reaches its test: `git submodule update --init -- references/Hexalith.Builds` fails with "Filename too long" (CI run 38120973437); its single `-method` selector also exits 0 when it matches no test, and that test cannot distinguish job-object cleanup from `Kill(entireProcessTree)`. Enable `core.longpaths` before the submodule init, run the whole TestSupport test project on Windows, and add an orphaned-grandchild cleanup test [.github/workflows/ci.yml:47]
- [x] [Review][Patch] (medium) Git-backed fixtures cannot clean up on Windows: Git for Windows writes read-only object files and `Directory.Delete(recursive: true)` throws `UnauthorizedAccessException` (reproduced on .NET 10.0.12); clear read-only attributes before deleting in `GitFixture.Dispose` and the clone cleanups [tests/Hexalith.Conversations.TestSupport.Tests/GitFixture.cs:35]
- [x] [Review][Patch] (medium) Legacy `info/grafts` still rewrites history under the runner's isolation: a one-line grafts file changed `HEAD~1` with `GIT_NO_REPLACE_OBJECTS=1` (Git 2.53), and `GIT_GRAFT_FILE` set to the null device restored the real parent; add it to the isolated environment and add an AC-05 grafts fixture [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:83]
- [x] [Review][Patch] (medium) Valid `R100` renames with changed content are rejected: Git emitted `R100` for a renamed file with reordered lines and different object IDs, but `IsDiffRelation` requires `R100` if and only if the IDs are equal, so the whole diff becomes `GIT_OUTPUT_INVALID`; keep only "equal IDs imply R100" and make the different-ID `R100` fixture valid [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:562]
- [x] [Review][Patch] (medium) Raw status grammar accepts forms Git never emits: Git always prints three-digit scores (`R072`), but `IsDiffStatus` accepts `R75` and scored `M` records although `-B` is never passed, and the AC-05 valid-score fixture asserts `R75`; require `R` plus exactly three digits, drop scored `M`, and use `R075` in the fixture [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:543]
- [x] [Review][Patch] (medium) Missing Git on Unix is not `GIT_UNAVAILABLE`: `setsid` exits 127 ("failed to execute"), `RunAsync` returns success with that exit code, and `ResolveRepositoryAsync` reports `REPOSITORY_UNAVAILABLE`, so AC-06's missing-Git assertion passes on status alone; resolve the executable before launching through `setsid` and assert the code [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:66]
- [x] [Review][Patch] (medium) Hostile ambient-config fixture cannot detect a regression: the preamble's `-c core.quotepath=false` overrides the injected `GIT_CONFIG_COUNT` value (verified) and Git ignores a missing-file `GIT_CONFIG_GLOBAL`, so removing the runner's config overrides keeps AC-05 green; assert the child's exact isolated `GIT_*` environment through a Python fake [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:121]
- [x] [Review][Patch] (medium) Pair verification compares duplicated Story 10.1 bindings by path only: `v2_verify_pair` never compares `results[*].sha256` with scenario `resultFile.sha256` or `projects[*].assembly.sha256` with the scenarios' bound assemblies, and two reviewers re-finalized a contradictory pair that verified and schema-validated; compare the digests too [_bmad/scripts/generate_story_record.py:4619]
- [x] [Review][Patch] (medium) Overflow check after normal completion is never exercised: the 17 MiB fixture always takes the cancellation branch, and deleting the post-completion check keeps every test green while a parent-exits-then-descendant-writes case returns truncated success; add that fixture [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:168]
- [x] [Review][Patch] (medium) Test-project boundary rules are untested: every direct `v2_10_1_project_boundary` call passes `helper=True`, and the "evaluated project differs from the committed candidate" path has no negative case; add `helper=False` fixtures for an added production `ProjectReference` and an extra or missing package, plus a differing worktree project [_bmad/scripts/tests/test_generate_story_record.py:10164]
- [x] [Review][Patch] (medium) The committed Story 10.1 pair is not pinned by a test: nothing runs `v2_verify_pair` and schema validation on `docs/release-evidence/story-10.1-final-record-v2.{json,md}`, so a render or schema change breaks it silently until Story 10.2 rejects the predecessor; add a committed-pair check like the preserved-pairs test [_bmad/scripts/tests/test_generate_story_record.py:9144]
- [x] [Review][Patch] (medium) Raw diff duplicate-path and containment rejection are untested: no malformed diff repeats a destination path or uses `../`, empty-segment, or absolute paths, and no tree fixture escapes, so deleting those conditions keeps tests green; add them to the existing malformed lists [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:198]
- [x] [Review][Patch] (medium) The inserted-record stale-binding test replaces the re-derivation it proves: `v2_generate` is swapped for a hand-written `rederive`, so the real Story 10.1 route never runs in the negative case, and `v2_retention_config` is mocked with the superseded key `10-1-neutral-testsupport-and-safe-git-facts`; drive the real route, stubbing only artifact bytes, with the tracked key [_bmad/scripts/tests/test_generate_story_record.py:10339]
- [x] [Review][Patch] (low) `HEAD~` and uppercase full object IDs beyond a shallow boundary are typed `REVISION_INVALID`: Git treats `HEAD~` as `HEAD~1` and accepts uppercase hex (verified), but the shallow proof rejects an empty count and requires lowercase; treat an empty count as 1 and lowercase the ID first [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:450]
- [x] [Review][Patch] (low) Missing or NUL repository inputs are classified differently per entry point: `ReadBlobAsync` validates the path before resolving the repository, so a missing root is `Invalid`/`REPOSITORY_PATH_ESCAPE` where the other readers return `Unavailable`/`REPOSITORY_UNAVAILABLE`, and `RunAsync` returns `Unavailable` for a NUL path that `ResolveRepositoryAsync` calls `Invalid`; resolve the revision first and return `Invalid` for NUL [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:411]
- [x] [Review][Patch] (low) Git's stderr is dropped from unavailable-repository and history failures: "Git could not identify an exact worktree" hides a dubious-ownership refusal or corrupt repository; append the decoded stderr to these messages [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:235]
- [x] [Review][Patch] (low) A Windows `core.autocrlf=true` checkout breaks the generator's project comparison: both `.csproj` files are committed LF without an eol rule, so the worktree differs from the committed blob and raises `TEST_SUPPORT_BOUNDARY_INVALID`; pin them to `eol=lf` in `.gitattributes` [_bmad/scripts/generate_story_record.py:5544]
- [x] [Review][Patch] (low) The Story 10.1 runbook section is incomplete: it omits package, assembly, and framework references and Debug plus Release evaluation from `TEST_SUPPORT_BOUNDARY_INVALID`, omits `AUTHORITY_BINDING_INVALID`, `TEST_RESULTS_MISSING`, and `ASSERTION_INVENTORY_DRIFT`, and does not say inserted-record verification needs the local `bin/Release` and `artifacts/v9/10.1` outputs [docs/runbooks/story-final-record-generation.md:1869]
- [x] [Review][Defer] (medium) Successor sprint keys are still derived from the spec filename for every story except 10.1 [_bmad/scripts/generate_story_record.py:5076] — deferred: pre-existing generic `removeprefix("spec-")` derivation; this change special-cased only 10.1, so Stories 10.2–10.4 will hit the same lifecycle failure unless the tracker row is resolved by story-number prefix in `sprint-status.yaml`.

#### Rejected

- BH4 (low): The 30-second limit is per Git process. Each fact call starts a bounded number of processes, and AC-04 contracts a per-execution 30-second timeout; one deadline across all processes would need threading through every reader.
- BH5 (low): Per-entry root revalidation is linear, at a few `lstat` calls per entry, not quadratic. Removing it needs a separate lexical validator.
- BH6 / ECH13 (low): A full object ID behind a non-first-parent boundary in a merge clone deeper than 1 fails closed as invalid. It is rare, and proving it needs one `cat-file` per shallow boundary (same rationale as L3-B07).
- BH7 / AA8 (low): The contract's per-scenario failure codes are not emitted. A failing AC cannot yield a PASS pair and is diagnosed from its TRX, mapping each code adds per-scenario branches, and Story 9.2 also emitted its contract codes only partially.
- BH9 (low): Binding gitignored local TRX and DLL outputs is the established v2 evidence model; the 7.4 record binds `artifacts/v9/7.4/*` the same way. The missing execution-time receipt is already deferred as L4-B10.
- BH10 / AA2 (low): A packable or production-dependent helper stops AC-07 with `TEST_SUPPORT_BOUNDARY_INVALID`, so no PASS pair can exist. Listing that check in AC-01's ledger would need a generator restructure.
- BH12 and AA7, short-path part / ECH10 (false): Reproduced on Windows. .NET 10 `Path.GetFullPath` expands `JEROME~1` short names, and the result equals Git's `--show-toplevel`.
- BH13, inventory part (false): `ASSERTION_INVENTORY_DRIFT` is tested at `test_generate_story_record.py:10062`. Its dotnet-marker part (low): the boundary test evaluates a temporary project outside `global.json`, so any preinstalled SDK works.
- BH16 / ECH22 (low): `GitFact`'s positional constructor and the mutable `byte[]` members can only be misused by a caller against its own results. Locking them down changes the public record shape.
- BH17 / ECH24 / ECH25 (low): Manifest path and digest validation and ledger completeness are Story 10.2's manifest, hash, and ledger invariants, which this spec forbids implementing. `BoundaryAssertions.Equal` on arrays fails closed, and evidence digests are strings.
- BH18 (rejected, spec edit): The status and notes mismatch would be fixed only by editing the spec under review; the lifecycle transition sets the status.
- ECH1 (low, carried L1-B04): Hosts without `setsid` fail closed with `GIT_UNAVAILABLE`; the supported Linux hosts ship it.
- ECH3 (low): Global `safe.directory` is deliberately unavailable under config isolation. Adding `-c safe.directory` would bypass Git's ownership protection against hostile repository config.
- ECH4 / AA11 (low): Git older than 2.44 ignores `GIT_NO_LAZY_FETCH`. The hosts run 2.53 and 2.55, and the partial-clone fixture would catch a regression.
- ECH5 (low): PID reuse between reaping and `kill(-pid)` needs PID wrap-around within microseconds, and a live process group keeps its ID reserved.
- ECH6 (low): A Windows Git process that exits before job assignment yields a typed `GIT_UNAVAILABLE` failure, never success. It is rare, and fixing it needs a new guard.
- ECH7 (low, carried L1-B01/L3-E02/L4-E02): The Windows pre-enrollment race was already rejected in those earlier reviews.
- ECH8 (false): After cancellation, capture reads and writes observe the cancelled token and throw `OperationCanceledException`, which is caught, and a killed writer yields EOF. No `IOException` path was shown.
- ECH9 (low): Late writes after the 2-second drain become unobserved task exceptions, which .NET swallows with no functional effect.
- ECH11 (low): Rejecting linked root ancestors is spec-mandated. macOS `/var` temp roots are not a supported target, and a real-path temp helper is more than a direct correction.
- ECH15 (low): A directory path is still typed `Invalid`, only under `GIT_OUTPUT_INVALID` instead of `BLOB_INVALID`.
- ECH16 (low): For a committed symlink, the blob bytes are the link text, which is the faithful raw Git fact. Rejecting symlinked evidence is Story 10.2 policy.
- ECH19 (low): Committed backslash paths read on Windows fail closed. They are rare, and the fixture already scopes them to Unix.
- ECH20 (low, carried L1-E06): A path-swap TOCTOU needs a concurrent hostile local writer.
- ECH21 (false): A null or empty executable is a caller programming error, and the loud exception is correct behavior.
- ECH23 (low): `AssertionLedger` is a single-test helper like `Dictionary`, and no concurrent use was shown.
- ECH28 (low): A missing `dotnet` cannot reach the boundary check after AC-01..06 were measured, and the 30-second MSBuild timeout is speculative.
- ECH29 (low): CI checks out with `fetch-depth: 0`, so the predecessor candidate resolves. A shallow local generation fails closed.
- ECH30 (false): A rebuild at a lifecycle commit changes the DLL bytes, so the recorded assembly digest no longer matches on either path.
- ECH32 / ECH33 (false): Every `v2_verify_pair` caller validates the record schema first (lines 5102, 5153, 5390, 5418, 7246, 8117, and 8332), and the schema rejects those malformed or missing fields.
- VG8 (low): `Process.Kill(entireProcessTree)` throws `AggregateException` only for non-access-denied termination errors, which were not shown, and the fix adds handlers at two sites.
- AA5 (low): The spec requires the five helpers "once each", which the test enforces. Pinning the whole exported set would break when Story 10.2 adds TestSupport types.
- AA6 (low): `includeGitPreamble` is a fixture hook with a safe default, and environment isolation still applies when it is false.
- AA9 (low): Each mandatory fault is a fixture inside AC-03..06, which the record binds. The contract defines no separate fault-injection results.
- AA10 (low): Synthetic raw records use Git's exact `--raw` grammar for every mode, and the missing-Git fixture already exercises the non-zero `rev-parse` branch.
- AA12 (false): `validate_repository` returns the resolved root, so the root branch is harmless defense-in-depth. The in-repository component check is tested.

#### Second review pass

Code review of `2f2621a..0687017` (2026-10-11), full mode, all four layers completed. By owner choice the diff excluded this spec and the generated record pair, which remain a separate follow-up chunk. Result: 1 decision-needed (resolved to patch by the owner), 17 patch, 1 defer, 31 rejected. Commit `da73f49` landed after the diff was built; it is outside the range and is cited only by the first patch below.

- [x] [Review][Patch] (high) Inserted-record verification no longer retains the Story 10.1 candidate: owner commit `da73f49` (`build: sync local changes via /pushall`) moved five root gitlinks (Builds, EventStore, Folders, Parties, Projects) after candidate `a7a6c51`, and `--verify-inserted-record` now exits 1 with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT` (reproduced). Owner decision: after the other patches land, regenerate the pair from a new committed candidate that includes `da73f49`, rerun AC-10.1-01 through AC-10.1-07, and reinsert the record [_bmad/scripts/generate_story_record.py:5205]
- [x] [Review][Patch] (high) Windows CI fails AC-10.1-06: the large-shallow and probe-cancellation fakes use Python `print()`, which writes `\r\n` on Windows; `ResolveRepositoryAsync` strips only `\n`, so the reported root keeps `\r` and resolves `REPOSITORY_UNAVAILABLE`, and line 461 sees `Unavailable` instead of `Invalid` (CI run 38125974793, `ci / TestSupport Windows descendant cleanup`, 8 passed and 1 failed; the job has not passed in any run). Write those outputs with `sys.stdout.buffer.write(...)` like the other fakes [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:455]
- [x] [Review][Patch] (medium) The runner trusts `objects/info/commit-graph`: with a forged HEAD tree entry in the graph, `git diff --raw` (the `ReadDiffAsync` command) reported the root commit's blob for HEAD, and a forged parent entry changed `rev-list --first-parent` and `log %P` (reproduced on Git 2.53 with the runner's environment and preamble); `-c core.commitGraph=false` restored the real facts. Add it to the preamble beside the graft and replace-object isolation, and pin it in AC-05 like the `diff.renameLimit` check [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:101]
- [x] [Review][Patch] (low) `GIT_GRAFT_FILE` set to the null device makes every successful command write a 311-byte grafts deprecation hint to stderr (verified with `rev-parse --verify`), so `GitCommandResult.Stderr` never holds only the command's own bytes and every `ErrorText` diagnostic starts with the hint; add `-c advice.graftFileDeprecated=false` to the preamble (verified to silence it) and assert empty stderr for a real successful command in AC-05 [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:96]
- [x] [Review][Patch] (medium) AC-05 compares only modes for real-repository diff and tree entries: `Status`, object IDs, and the exact entry set (`évidence.txt`, `nested/path.txt`, and the Unix backslash path) are never checked against the fixture, so a reader reporting a wrong status, a wrong object ID, or an extra entry stays green; assert the full entries against `git rev-parse <commit>:<path>` [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:197]
- [x] [Review][Patch] (medium) Story 10.1's read-only verify route is not pinned to `execute_commands=False`: changing `story_id in ("9.2", "10.1")` to `== "9.2"` at `generate_story_record.py:7996` keeps all 14 `story101` tests green, and the route would then rerun the AC-10.1-01 `dotnet build` and rewrite the bound helper assembly; record the kwargs in `measured_scenario` and assert `execute_commands is False` [_bmad/scripts/tests/test_generate_story_record.py:10540]
- [x] [Review][Patch] (medium) Pair verification's AC-10.1-01 assembly binding and scenario-ID set checks are unexercised: removing all four clauses at `generate_story_record.py:4631-4639` keeps every `story101` test green; add `projects[0].assembly.sha256` drift, a dropped `AC-10.1-07` row, and a duplicated scenario ID to the mutation list [_bmad/scripts/tests/test_generate_story_record.py:10361]
- [x] [Review][Patch] (medium) The `FrameworkReference` boundary rule is untested: deleting it at `generate_story_record.py:5483-5486` keeps every `story101` test green; add an extra `<FrameworkReference Include="Microsoft.AspNetCore.App" />` case that expects "framework dependency" [_bmad/scripts/tests/test_generate_story_record.py:10170]
- [x] [Review][Patch] (medium) The neutral helpers' failure and duplicate paths are untested: making `BoundaryAssertions.Equal` always record `true`, or letting `AssertionLedger.Record` overwrite an identity, keeps `RequiredHelpersShouldExistExactlyOnce` green; add a separate test method, outside the frozen AC-10.1-02 selector, proving that a mismatch records `false` and that a duplicate `Record` or `Add` throws [tests/Hexalith.Conversations.TestSupport.Tests/TestSupportSurfaceTests.cs:21]
- [x] [Review][Patch] (medium) The 30-second cap on caller timeouts is untested: dropping `|| timeout.Value > CommandLimit` lets `TimeSpan.FromMinutes(10)` run with every test green; assert that `TimeSpan.FromSeconds(31)` and `TimeSpan.Zero` return `Invalid` with `GIT_TIMEOUT_INVALID` [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:101]
- [x] [Review][Patch] (low) The partial-clone fixture asserts only `Unavailable`, so an earlier `REPOSITORY_UNAVAILABLE` would pass without reaching the no-lazy-fetch `cat-file` path; also assert `HISTORY_UNAVAILABLE` [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:544]
- [x] [Review][Patch] (low) AC-06 names a non-repository fixture, but the cases are an absent path and a directory nested in a repository, so the non-zero `rev-parse --show-toplevel` branch of `ResolveRepositoryAsync` is never reached; add a fresh temporary directory outside any repository that expects `REPOSITORY_UNAVAILABLE` [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:400]
- [x] [Review][Patch] (low) Missing TestSupport artifacts contradict the message and the runbook: a missing committed project emits `TEST_RESULTS_MISSING` saying "a TestSupport project or candidate assembly is missing", but a missing assembly raises "missing regular artifact" and becomes `TEST_SUPPORT_BOUNDARY_INVALID`, while the runbook maps an absent bound artifact to `TEST_RESULTS_MISSING`; narrow the message to the project and document that a missing or linked assembly reports `TEST_SUPPORT_BOUNDARY_INVALID` [_bmad/scripts/generate_story_record.py:5557]
- [x] [Review][Patch] (low) `summary = previous["summary"]` is assigned and never read in `v2_10_1_facts`; delete it [_bmad/scripts/generate_story_record.py:5533]
- [x] [Review][Patch] (low) The Story 10.1 L4-B10 deferred item sits under the Story 9.2 review heading; move it under the Story 10.1 heading that follows it [_bmad-output/implementation-artifacts/deferred-work.md:559]
- [x] [Review][Patch] (low) Descendant fixtures can read a created but unwritten PID file: `WaitForFileAsync` returns as soon as the file exists, while Python's `open(..., 'w').write(...)` creates it before writing, so `int.Parse` can see an empty string; in the grandchild test the parent's existence check also lets the runner kill the child mid-write. Write to a temporary name and `os.replace` it into place [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:572]
- [x] [Review][Patch] (low) The startup comment says one deadline covers startup, execution, and cleanup, but after the limit the cleanup adds two fresh 2-second waits, so `RunAsync` can return about 34 seconds after start; state that cleanup waits are bounded separately [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:121]
- [x] [Review][Defer] The Story 10.1 generator tests never run in CI [.github/workflows/ci.yml:117] — deferred: pre-existing. The `ci / repository` job fails at "Verify story completion gate surfaces" (AC-7.3-01 `WORKFLOW_INTEGRATION_MISSING` on untouched `.agents` and `.claude` skill surfaces) before "Test BMAD repository tooling", both in run 38125974793 and at baseline `2f2621a` (run 38074926653), so `test_story101_*`, including `test_story101_committed_pair_still_verifies`, run only locally. Settle by restoring the completion-gate blocks or by running the tooling tests independently of that step.

##### Rejected (second pass)

- R2-BH3 (low): The test host loads its own copy of the helper DLL, but an ordinary deterministic build from one candidate copies identical bytes. Binding execution-time binaries is the deferred L4-B10 receipt protocol.
- R2-BH4 / R2-AA4 (low): `329bfa1` is a routine owner gitlink sync, and the record faithfully binds the gitlinks the candidate had. Splitting pushed linear history would need a force push, which is banned.
- R2-BH6 (low): Python is already a repository-wide tooling prerequisite, and the missing-`setsid` case was rejected as L1-B04. The contract JSON is the single command authority, so copying its commands into the runbook would create a second source.
- R2-BH7 / R2-ECH20 (low, carried L3-B08): Environment-specific MSBuild overrides are a hypothetical future environment, not a present violation, and scrubbing them needs a new allow-list.
- R2-BH9 / R2-ECH14 (low): Windows trailing-dot, stream, and device spellings still resolve inside the root. Canonicalizing manifest keys is Story 10.2 policy, and the fix needs Windows-specific guards.
- R2-BH10 (low): The Windows job log carries the full xUnit failure, which is how the AC-06 failure above was diagnosed. The generator enforces the dependency rule.
- R2-BH11 (low): Processes and links leak only after a failing test, and the sleepers stop after 120 seconds. Adding `finally` cleanup is more than a direct correction.
- R2-BH12 (false): A renamed `CommandLimit` fails the test loudly. The 30-second default-timeout run was added deliberately by L4-V01.
- R2-ECH1 (low, carried L1-B01/L3-E02/L4-E02): Windows pre-enrollment race.
- R2-ECH2 (maybe-false, low if true): No exception path between job construction and `TerminateTree` was shown, so `KILL_ON_JOB_CLOSE` would guard an undemonstrated state.
- R2-ECH3 (false, carried ECH8): After cancellation, reads observe the cancelled token or a killed writer's EOF. No `IOException` path was shown.
- R2-ECH4 (low): An exec failure inside `setsid` still returns a typed `Unavailable` result with the stderr appended. Mapping exit codes 126 and 127 could misclassify real Git exits.
- R2-ECH5 (low, carried L1-B04): Hosts without `setsid`.
- R2-ECH6 (low, carried L3-B03): A planted executable in the host's current directory is outside the hostile-configuration boundary.
- R2-ECH7 (low, carried ECH5): PID reuse.
- R2-ECH8 (low, carried BH4): The 30-second limit applies per process.
- R2-ECH9 (low, carried ECH4/AA11): Git older than 2.44.
- R2-ECH10 (low, carried ECH3): Global `safe.directory` is deliberately unavailable.
- R2-ECH11 (false, carried BH12): 8.3 short names are expanded.
- R2-ECH12 (low, carried L3-B07/BH6): General shallow expressions.
- R2-ECH13 (low, carried ECH16): A committed symlink's blob is its link text.
- R2-ECH15 (low, carried BH17): Manifest digest validation is Story 10.2 scope.
- R2-ECH16 (low, carried BH17): `Equal` on arrays fails closed.
- R2-ECH18 (low): One second is ample for a single Python startup, and a slow host fails the test loudly rather than producing a false pass.
- R2-ECH19 (low, carried ECH28): A missing `dotnet` cannot reach the boundary check.
- R2-ECH21 (low, carried L5-E02): A multi-target conditional reference is a hypothetical future project change.
- R2-ECH22 (low, carried L1-E06): Path-swap TOCTOU.
- R2-ECH23 (false): The compatibility correction deliberately replaced ancestry with committed-pair and authority checks, because the accepted 7.4 record diverges.
- R2-ECH26 (low, carried L1-B02): Intentionally detached Unix descendants.
- R2-VG6 (low, carried BH13): The boundary tests evaluate a temporary project outside `global.json`, so any preinstalled SDK works; the step does not run today (see the deferred CI item).
- R2-AA7 (rejected, contract edit): Removing both projects includes their solution and CI wiring, and changing the rollback text would edit the frozen contract.

#### Third review pass

Code review of `2f2621a..0b9251d` (2026-10-11), full mode, all four layers completed. As in the second pass, by owner choice the diff excluded this spec and the generated record pair. Result: 1 decision-needed, 11 patch, 1 defer, 37 rejected. Every patch changes a bound candidate input, so landing any of them requires a new committed candidate and a regenerated Story 10.1 pair.

- [x] [Review][Decision] (medium, resolved: the owner pushed `a999560..dbb5ac2`, and run 38142617722, job 114479400469, `ci / TestSupport Windows descendant cleanup`, passed at `dbb5ac2` with 10 of 10 tests passed and 0 failed, skipped, or not run; no code change needed) The Windows TestSupport job has never passed, and the commits that fix its failures have never run in CI — run 38127833621 at `7eb1779`, the last pushed commit, failed `ci / TestSupport Windows descendant cleanup` with 2 of 9 tests failing: an `IOException` from the pid-file cleanup in `RunnerShouldBoundAndCancelExecution` (the old non-atomic `open(...).write` fixture) and `Unavailable` instead of `Invalid` at the large-shallow assertion (the `print()` CRLF fake). HEAD fixes both (`tmp` plus `os.replace`, and `sys.stdout.buffer.write`) only in the eight unpushed commits `a999560..0b9251d`. So the Windows `GitProcessJob` branch and the rest of AC-10.1-06 (root, merge, and octopus shallow clones, the partial clone, probe cancellation) have no passing Windows run, although the second pass marked the Windows patches done. Options: push the local commits and require a green Windows job before `done`, or accept the story with the Windows gap recorded [.github/workflows/ci.yml:36]
- [x] [Review][Patch] (medium) Real-Git raw modes and submodule-ignore isolation are untested: the AC-05 repository commits only `100644` files, so `ReadTreeAsync` never parses a real `160000 commit`, `120000`, or `100755` entry. Removing both `--ignore-submodules=none` and the preamble's `-c diff.ignoreSubmodules=none` keeps every test green, while a repository-config `diff.ignoreSubmodules=all` makes `git diff --raw` drop a gitlink bump (verified by the reviewer). Add a sibling `[Fact]` with a gitlink (`update-index --cacheinfo 160000,…`), an executable, and a symlink under hostile `diff.ignoreSubmodules=all`, asserting the tree entries and a `:160000 160000 … M` diff record [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:217]
- [x] [Review][Patch] (medium) The hostile-environment check pins 11 named keys rather than the child's whole `GIT_*` environment: replacing the runner's prefix strip at `GitFacts.cs:86` with a list of the seven injected names keeps AC-05 green while `GIT_CONFIG_PARAMETERS` (the `git -c` channel), `GIT_ALTERNATE_OBJECT_DIRECTORIES`, `GIT_WORK_TREE`, `GIT_INDEX_FILE`, or `GIT_SHALLOW_FILE` leak through. Also inject `GIT_CONFIG_PARAMETERS`, dump every `GIT_*` key, and compare with the exact expected dictionary [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:183]
- [x] [Review][Patch] (medium) Story 10.1 generator fail-closed branches have no negative test: the 10.1 `ASSERTION_INVENTORY_DRIFT` branch (the first pass's BH13 rejection cited `test_generate_story_record.py:10062`, which is a Story 9.2 test), the TRX "result digest differs" branch, the missing committed project `TEST_RESULTS_MISSING` branch, and `v2_10_1_predecessor_passes` with an empty `assertionLedger` or nonzero `skipped`/`notRun`. Deleting any of them keeps every `story101` test green, and the empty-ledger case is the epic's "zero assertions cannot pass" rule [_bmad/scripts/generate_story_record.py:5510]
- [x] [Review][Patch] (low) The schema's 10.1-only `testSupportExecution` rule and the block's closed shape are untested: deleting the `else` arm or `additionalProperties: false` keeps every committed pair valid, so a later story's record could carry a copied 10.1 block and render its section. Add the 8.2/9.2-style mutations (block removed from 10.1, block added to a prior record, extra property) [_bmad/schemas/story-final-record-v2.schema.json:435]
- [x] [Review][Patch] (low) The linked-artifact rule is tested only on the helper: replacing `v2_10_1_artifact_bytes(repository, assembly_path)` with a plain read keeps every test green. Add a symlinked `tests/Hexalith.Conversations.TestSupport.Tests/bin` case (Unix-only, like the helper test) to the bindings test, expecting `TEST_SUPPORT_BOUNDARY_INVALID` [_bmad/scripts/generate_story_record.py:5563]
- [x] [Review][Patch] (low) `ReadBlobAsync` mixes the validated root with the raw caller path: `ls-tree` runs on `root.Value`, but `ParseTree` and `cat-file` use `repository`, so a relative path with a working-directory change between steps could validate one directory and read another. Use `root.Value!` throughout the method [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:471]
- [x] [Review][Patch] (low) `RunAsync` returns `Success` for a nonzero Git exit, while `GitFactStatus.Success` says "the requested fact was established". The typed readers recheck `ExitCode`, but a Story 10.2 consumer trusting `IsSuccess` from `RunAsync` would read a failed command as a fact. Document on `RunAsync` that success means captured completion and that callers must check `ExitCode` [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:30]
- [x] [Review][Patch] (low) The schema pins project, assembly, and result paths with `const` but leaves `contract.path` and `predecessorRecords[].path` open, so only the Python `v2_10_1_exact_paths` rejects a foreign contract or predecessor file. Pin both [_bmad/schemas/story-final-record-v2.schema.json:4531]
- [x] [Review][Patch] (low) The Story 10.1 runbook understates the boundary rule and omits a prerequisite: `v2_10_1_artifact_bytes` rejects any linked path component and any linked repository ancestor, not only links "outside the repository", and inserted-record verification runs four `dotnet msbuild` evaluations (30 seconds each), so it needs the .NET SDK [docs/runbooks/story-final-record-generation.md:1878]
- [x] [Review][Patch] (low) The Windows CI pack/publish check evaluates the projects before any restore, so NuGet-imported props and targets are absent and the evaluated graph differs from the build two steps later. Move the step after "Build TestSupport tests" [.github/workflows/ci.yml:57]
- [x] [Review][Patch] (low) The Story 10.1 deferred-work heading follows the previous list item with no blank line, unlike every other section [_bmad-output/implementation-artifacts/deferred-work.md:559]
- [x] [Review][Defer] (low) The `sprint-status.yaml` header comment still says the global implementation hold is active, IR-0 was not run, and all successor stories remain backlog, while the body now has `epic-10: in-progress` and Story 10.1 in `review` [_bmad-output/implementation-artifacts/sprint-status.yaml:41] — deferred: pre-existing; this change did not touch that comment, which already lagged the IR-0 state before Story 10.1.

##### Rejected (third pass)

- R3-BH1 (low, carried BH7/AA8): Per-scenario contract failure codes are not emitted.
- R3-BH2 (low, carried AA5): Pinning the whole exported type set would break when Story 10.2 adds TestSupport types.
- R3-BH3 / R3-ECH21 (low, carried ECH29): An unresolvable predecessor candidate is classified `AUTHORITY_BINDING_INVALID` rather than a BLOCKED code. It still fails closed; CI checks out with `fetch-depth: 0`, and the local repository holds the `evidence/story-7.4-candidate` tag.
- R3-BH4 (low): The process-wide `GIT_*` and working-directory mutations run inside one class, and the only other class neither spawns Git nor uses relative paths. A non-parallel collection would guard future tests that do not exist yet.
- R3-BH6 / R3-ECH31 (low, carried R2-VG6): SDK availability for the boundary tests in the repository job.
- R3-ECH1 (low, carried L1-B04): Hosts without `setsid`.
- R3-ECH2 (low, carried L1-B02): Intentionally detached Unix descendants.
- R3-ECH3 (low, carried ECH5): PID reuse.
- R3-ECH4 (low, carried L1-B01): Windows pre-enrollment race.
- R3-ECH5 (low, carried ECH6): A Windows process that exits before job assignment yields typed `GIT_UNAVAILABLE`, never success.
- R3-ECH6 (false, carried ECH21/L5-B02): A null executable or prefix argument is caller misuse.
- R3-ECH7 (low): A NUL inside a raw `RunAsync` argument could truncate native argv, but every typed reader rejects NUL in its revision and path before calling `RunAsync`. Covering direct callers adds another guard.
- R3-ECH8 (low, carried ECH9): Late task exceptions after bounded cleanup.
- R3-ECH9 (false, carried BH12): 8.3 short names are expanded.
- R3-ECH10 (low, carried ECH3): Global `safe.directory` is deliberately unavailable.
- R3-ECH11 (low, carried L3-B07): General shallow expressions.
- R3-ECH12 (low, carried ECH16): A committed symlink's blob is its link text.
- R3-ECH13 (low, carried ECH19): Committed backslash paths on Windows.
- R3-ECH14 (low, carried R2-BH9): Windows stream and device spellings.
- R3-ECH15 / R3-ECH25 (low, carried L1-E06): Path-swap TOCTOU.
- R3-ECH16 (low, carried BH17): `Equal` on arrays fails closed.
- R3-ECH17 (low, carried BH17): Manifest digest validation is Story 10.2 scope.
- R3-ECH18 (low, carried ECH23): `AssertionLedger` is a single-test helper.
- R3-ECH19 (low, carried BH16): The public `GitFact` record shape.
- R3-ECH20 (low, carried ECH28): A missing `dotnet` cannot reach the boundary check after AC-01..06 were measured.
- R3-ECH22 (false, carried R2-ECH23): Ancestry is deliberately not required, because the accepted 7.4 record diverges.
- R3-ECH23 (false, carried ECH30): A lifecycle rebuild changes the bound assembly digest on both routes.
- R3-ECH24 (false): `v2_verify_pair` checks a pair's internal consistency for any result, so a valid FAIL pair must verify; predecessor PASS is enforced separately by `v2_10_1_predecessor_passes`.
- R3-ECH26 / R3-ECH27 (low, carried R2-ECH18): Fixture startup and poll windows fail loudly on a slow host instead of producing a false pass.
- R3-ECH28 (low): An unreaped zombie in an init-less container makes `WaitForExitAsync` fail loudly. CI and WSL reap their children, and handling it needs `/proc` state parsing.
- R3-ECH29 / R3-ECH30 (low, carried R2-BH11): Temporary directories and links leak only after a failing test.
- R3-AA4 (low): Leaders and descendants share one process-group or job termination path. The started-descendant assertions already catch a regression that skips termination, and a regression that spares only the leader would be contrived.

#### Fourth review pass

Code review of `0b9251d..f9f659a` (2026-10-11), full mode, all four layers completed. By owner choice the diff covered the third-pass delta, without this spec or the unrelated `references/*` gitlink syncs, plus the full current `story-10.1-final-record-v2.{json,md}` pair. That pair was last reviewed in the first pass. Result: 0 decision-needed, 5 patch, 1 defer, 8 rejected.

Checked at HEAD:
- `--verify-inserted-record` exits 0.
- All 12 bound project, assembly, TRX, contract, and predecessor digests match.
- Run 38155896856 at `f9f659a`: `ci / TestSupport Windows descendant cleanup` passed 11 of 11.
- `ci / repository` still fails at the deferred completion-gate step.

Every patch below changes a path outside the retained-candidate whitelist. Landing any of them requires a new committed candidate, AC-10.1-01 through AC-10.1-07, and a regenerated pair. Committing this findings section alone also breaks retention of the `a9497e9` pair, because the spec's lifecycle-only rule does not cover `### Review Findings`.

- [x] [Review][Patch] (medium) The new hostile-submodule fixture does not need `--ignore-submodules=none`. It plants only repository config `diff.ignoreSubmodules=all`, which the preamble's `-c diff.ignoreSubmodules=none` already defeats. A committed `.gitmodules` entry (or `submodule.<name>.ignore`) set to `ignore = all` drops the gitlink `M` record despite the preamble, and only the CLI flag at `GitFacts.cs:356` restores it (verified with Git 2.53 by three reviewers). So deleting that flag keeps every test green while repository content can hide gitlink bumps. Commit a `.gitmodules` mapping `deps/module` with `ignore = all` before the bump and keep the single-`M` assertion [tests/Hexalith.Conversations.TestSupport.Tests/GitFactsTests.cs:470]
- [x] [Review][Patch] (low) `ReadDiffAsync` and `ReadTreeAsync` still pass the raw caller path after validation. They validate through `ResolveRevisionAsync`, then hand the unvalidated `repository` to `RunAsync`, `ResolveHistoricalPath`, and `ParseTree`, the same relative-path and working-directory mix the third pass fixed only in `ReadBlobAsync`. Resolve the root once and use `root.Value!` throughout, mirroring `ReadBlobAsync` [tests/Hexalith.Conversations.TestSupport/GitFacts.cs:352]
- [x] [Review][Patch] (low) The third-pass negative tests mutate only one member of each multi-part rule:
  - Deleting `or contract["predecessors"] != ["7.4", "9.2"]` from the `ASSERTION_INVENTORY_DRIFT` check keeps every `story101` test green (verified by two reviewers).
  - Dropping `failed` or `blocked` from `v2_10_1_predecessor_passes` stays green, because the summary loop covers only `skipped` and `notRun`.
  - The schema test mutates only `predecessorRecords[0].path`, so the new 9.2 path `const` is unpinned.

  Add a reordered-predecessors case, loop over all four summary fields, and mutate `predecessorRecords[1].path` [_bmad/scripts/tests/test_generate_story_record.py:10131]
- [x] [Review][Patch] (low) The runbook states a repository-ancestor link rule that the generator never enforces. Both generator routes pass `validate_repository`'s resolved root into `v2_10_1_facts`, so the `repository.parents` symlink check in `v2_10_1_artifact_bytes` is unreachable, and deleting it keeps every test green. Drop the sentence "The repository path and its ancestors must also contain no links." [docs/runbooks/story-final-record-generation.md:1879]
- [x] [Review][Patch] (low) The runbook's Story 10.1 code definitions miss three generator paths:
  - An unavailable, timed-out (30 seconds), failed, or malformed Debug/Release MSBuild evaluation reports `TEST_SUPPORT_BOUNDARY_INVALID`.
  - A linked or non-regular `artifacts/v9/10.1/*.trx` reports `TEST_RESULTS_MISSING`.
  - AC-10.1-07 generation runs the same four evaluations, not only inserted-record verification.

  Add each to its definition [docs/runbooks/story-final-record-generation.md:1876]
- [x] [Review][Defer] (medium) This pass's schema path constants and the committed 10.1 pair are pinned only by pytest that CI never reaches. In run 38155896856 at `f9f659a`, `ci / repository` failed at "Verify story completion gate surfaces", and "Test BMAD repository tooling" was skipped [.github/workflows/ci.yml:117] — deferred: pre-existing and already tracked by the Story 10.1 ledger entry "The Story 10.1 generator tests, including the committed-pair pin, never run in CI" (`deferred-work.md:569`); no duplicate entry was added.

##### Rejected (fourth pass)

- R4-AA3 / R4-BH3 (low): The new gitlink `[Fact]` sits outside the AC-10.1-05 selector, so the bound TRX does not include it. The third-pass patch explicitly required a sibling `[Fact]`, and CI runs it (Windows 11 of 11, plus the Linux lane). Folding it into the frozen selector's method would restructure AC-05 for no named consumer.
- R4-BH2 (low, carried AA10): Real-Git `A` records for `100755` and `120000` are not asserted through `ReadDiffAsync`. Synthetic records already use Git's exact `--raw` grammar for every mode, and the new fixture covers the real `160000` diff record and all three tree modes.
- R4-BH4 (false): The finding says the linked-`bin` call site is still unpinned. Replacing `v2_10_1_artifact_bytes` with a plain read at the assembly call site now fails `test_story101_inserted_verification_runs_real_generator_for_stale_artifact`, because any read that bypasses the helper hits the disk. The helper's own test pins the `TestSupport.Tests/bin` link case.
- R4-BH8 (false): For `RunAsync`, the fact is a `GitCommandResult`, which its own docs define as complete raw output carrying an exit code. `IsSuccess` therefore means that capture was established, which is what the new `RunAsync` summary says.
- R4-BH11 (false): Every mutation starts from a state the test first proves valid, so each code can only come from that mutation. A failed assertion ends the pytest function before any unrestored state is read.
- R4-BH12 (low, carried R2-BH10): The CI step order is now what the third-pass patch required. Dependency neutrality is enforced by the generator, and its missing CI lane is the deferred item above.
- R4-BH13 (low): The new sprint-header deferral names the line and the contradiction. Recording the current IR-0 and hold facts would need research beyond a direct correction.
- R4-BH9, vacuity part (false): The schema mutations start from a record asserted valid, so an unrelated schema error cannot satisfy them. The unpinned 9.2 path is the patch above.

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
| L5-B01 blocking argument iterator | low | rejected | carried: L3-B01 and L4-E01 already reject bounding a caller's synchronous blocking enumerable; ordinary Git fact callers pass finite arrays. |
| L5-B02 null executable prefix | low | rejected | A constructor caller can supply a list containing null, but this is misuse of the test-only fixture prefix surface; adding another guard does not change normal Git facts. |
| L5-B03 non-executable Unix Git path | medium | patch | `ResolveUnixExecutable` accepts an existing non-executable file, then `setsid` exits 127 and repository resolution reports the wrong `REPOSITORY_UNAVAILABLE` code. |
| L5-B04 corrupt loose blob object | medium | patch | A real corrupt loose Git object can make `cat-file blob` exit zero with bytes different from the requested tree object ID; `ReadBlobAsync` must check the Git blob hash before reporting success. This differs from L3-B03's forged executable premise. |
| L5-B05 mixed object-ID width | low | rejected | carried: L4-B04 already considered foreign-width IDs in forged output; real Git emits its repository object format, and format probing throughout the parser adds disproportionate machinery. |
| L5-B06 success without value | low | rejected | carried: the prior BH16/ECH22 review rejected changing the public `GitFact` record shape for a caller-created inconsistent result; the runner uses `Ok` and typed failures. |
| L5-B07 mutable blob bytes | low | rejected | carried: the prior BH16/ECH22 review rejected changing the public `GitBlob` record shape for caller mutation after receipt; the runner computes the digest before returning. |
| L5-B08 Windows job termination failure | low | rejected | carried: L2-B06 already considered a failed OS job termination; the runner reports timeout/cancellation and ordinary Git behavior has no demonstrated recoverable fallback. |
| L5-B09 resolved compiler references | low | rejected | The accepted boundary checks declared project, package, assembly, and framework items. `ReferencePath` is a resolved compiler output, not another declared dependency; the current projects have no target that adds a production reference. |
| L5-B10 status regression retention | false | rejected | Lifecycle verification intentionally permits a review loop to move a story back to implementation while retaining its candidate; status alone is not a record assertion. |
| L5-B11 task checkbox regression retention | false | rejected | Review work may reopen tasks without changing the candidate facts; the lifecycle mask does not assert task completion and the final record binds measured scenarios. |
| L5-B12 Windows junction fixture | low | rejected | The Windows CI lane tests the Windows process-tree branch. Root/ancestor link containment is tested on Unix; adding privileged junction setup only for redundant platform coverage is more than a direct correction. |
| L5-B13 successor sprint key | medium | defer | carried: the same generic post-10.1 key issue is already recorded in deferred-work.md for Stories 10.2–10.4; it is pre-existing outside this candidate's tracked key. |
| L5-E01 filesystem-root repository | low | patch | `Path.TrimEndingDirectorySeparator` preserves a filesystem root, then the added separator yields `//` on Unix, rejecting a valid child path. |
| L5-E02 multi-target conditional reference | low | rejected | The checked-in TestSupport projects target one framework and declare no such conditional references. This is a hypothetical future project change, analogous to L3-B08/L4-E04, requiring its own candidate review. |
| L5-E03 predecessor output paths | medium | patch | `v2_verified_predecessor` validates the committed pair but does not require its declared JSON/Markdown output paths to match their committed locations. |
| L5-E04 PASS predecessor with blocker | medium | patch | `v2_10_1_predecessor_passes` checks PASS result and assertion states but allows a nonempty blockers list, a contradictory passing predecessor. |
| L5-V01 live CI project boundary | medium | patch | The focused generator tests evaluate temporary projects, while normal CI never evaluates both checked-in TestSupport projects' Debug and Release pack/publish properties. |
| L6-B01 Windows enrollment race | low | rejected | carried: L1-B01 covers the same pre-enrollment child race; raw Git fact commands do not spawn a child, and native suspended startup is beyond a direct correction. |
| L6-B02 late startup cleanup | low | rejected | A delayed `Process.Start` can finish after the timeout result and is killed by the continuation, but synchronously awaiting an arbitrarily hung start would violate the bounded startup requirement. Ordinary Git startup has no demonstrated late child. |
| L6-B03 detached Unix descendant | low | rejected | carried: L1-B02 covers the same new-session descendant; the raw Git commands do not intentionally detach children and the ordinary process group is covered. |
| L6-B04 blob path precedence | medium | patch | `ReadBlobAsync` resolves the revision before validating a caller path, so a bad revision masks `../escape` with `REVISION_INVALID` rather than the matrix's explicit `REPOSITORY_PATH_ESCAPE`. Resolve the repository, validate the historical path, then resolve the revision. |
| L6-B05 duplicate raw diff source | medium | patch | `ReadDiffAsync` tracks destinations only, so two `R` entries can consume the same old path and still return success. Track occupied source paths for source-bearing records without rejecting a valid rename swap. |
| L6-B06 malformed extra shallow line | false | rejected | In a real shallow clone, appending `not-an-oid` makes both `git rev-parse --is-shallow-repository` and `git rev-list --first-parent HEAD` exit 128 with `fatal: bad shallow line`; the helper fails before consulting its own boundary list. |
| L6-B07 forged assembly stamp | low | rejected | carried: L4-B09 covers arbitrary revision-shaped bytes in a substituted binary; exact candidate builds and selectors provide the ordinary execution evidence. |
| L6-B08 execution-time binary digest | medium | defer | carried: L4-B10 already defers the generic missing execution-time receipt; the current record binds candidate-stamped bytes, while a stronger receipt needs a shared runner protocol. |
| L6-B09 Windows conditional dependency | low | rejected | The checked-in projects declare no Windows-only production dependency and the generator evaluates their Debug and Release dependencies. A future conditional project change would be a new candidate, as in L3-B08. |
| L6-B10 CI tooling gate | medium | defer | carried: the second review already deferred the pre-existing completion-gate failure that prevents the Story 10.1 generator tests from running in CI. |
| L6-B11 successor sprint keys | medium | defer | carried: L5-B13 and deferred-work.md already record the generic 10.2–10.4 tracker-key mismatch. |
| L6-E01 Windows enrollment race | low | rejected | carried: L1-E02 covers the same process-start-before-job-assignment claim and its ordinary-Git limitation. |
| L6-E02 mixed diff object-ID widths | low | rejected | carried: L4-B04 covers foreign-width raw IDs; real Git emits one repository object format, and this requires a forged executable. |
| L6-E03 mixed tree object-ID widths | low | rejected | carried: L4-B04 and L5-B05 cover the same forged tree-width claim; real Git emits one repository object format. |
| L6-E04 execution-time binary digest | medium | defer | carried: L4-B10 already defers this generic receipt gap. |
| L6-E05 whitespace-only repository name | low | patch | `string.IsNullOrWhiteSpace(repository)` rejects an existing relative Unix directory named only spaces. The path contract preserves valid whitespace; use the existence check to distinguish it from a missing root. |
| L6-V01 spaced Git path coverage | medium | patch | The real-repository facts fixture has Unicode, nested, and literal-backslash names but no path with spaces. A space-rejecting regression would pass current tests, so assert a committed spaced path in diff, tree, and blob reads. |
| R4-B01 CI generator lane | medium | defer | carried: the second review already recorded the pre-existing completion-gate failure that stops CI before the Story 10.1 generator tests; local focused tests pass. |
| R4-B02 execution-time assembly digest | medium | defer | carried: L4-B10 records that TRX has no execution-time binary digest and that closing this generic evidence gap needs a shared receipt protocol. |
| R4-B03 ignored local evidence | low | rejected | carried: BH9 records that local TRX and DLL artifacts are the established v2 evidence model; the inserted-record route deliberately requires the bound local bytes. |
| R4-B04 successor sprint keys | medium | defer | carried: L5-B13 and deferred-work.md record the pre-existing 10.2–10.4 key mismatch; Story 10.1 resolves its tracked key. |
| R4-B05 stale sprint header | low | defer | carried: the third review records the pre-existing stale header comment separately from Story 10.1's correctly tracked status row. |
| R4-B06 resolved compiler references | low | rejected | carried: L5-B09 checked the declared dependency boundary; the current projects have no target that adds a production `ReferencePath` item. |
| R4-B07 raw runner root and options | low | rejected | High-level readers resolve and validate the root and pass fixed Git arguments. `RunAsync` is the lower-level command capture API, and its test fixture intentionally supplies raw `init` and `clone` arguments; no current fact reader passes caller-controlled `-C` options. |
| R4-B08 raw runner nonzero exit | false | rejected | `RunAsync` documents that success means captured completion and explicitly tells callers to check `ExitCode`; every high-level fact reader checks it before returning facts. |
| R4-B09 capture fault on cancellation | false | rejected | carried: R2-ECH3 checked this pipe-teardown claim; cancellation or killed-writer EOF is the demonstrated path, and no reachable `IOException` or `ObjectDisposedException` during the cleanup wait was shown. |
| R4-B10 SHA-256 repository fixture | low | rejected | The missing fixture does not show a bad result: 64-character object IDs are accepted and historical blob verification selects SHA-256 for them. A second repository-format fixture would broaden test setup without a demonstrated failure. |
| R4-E01 FIFO shallow metadata | false | rejected | A real FIFO at `.git/shallow` blocks the earlier `git rev-list --first-parent HEAD` probe, which the bounded process runner times out; the claimed synchronous `File.OpenRead` is not reached in that fixture. |
| R4-E02 Windows enrollment race | low | rejected | carried: L1-B01 and L6-B01 record the pre-enrollment child race; raw Git fact commands do not spawn descendants, and closing a fixture-only race needs native suspended startup. |
| R4-E03 successor sprint keys | medium | defer | carried: L5-B13 records the same generic later-story key issue, already present in deferred-work.md. |
| R4-V01 CI generator lane | medium | defer | carried: the second review recorded the same pre-existing CI completion-gate failure; the Story 10.1 generator tests pass locally but cannot run in that job until its earlier gate is repaired or separated. |
| R5-V01 CI generator lane | medium | defer | carried: the second and fourth reviews recorded the pre-existing completion-gate failure before the Python tooling lane. The focused Story 10.1 tests pass locally. |
| R5-E01 Windows enrollment race | low | rejected | carried: L1-B01 and R4-E02 cover process start before job enrollment; raw Git facts do not spawn descendants, and suspended native startup is more than a direct correction. |
| R5-E02 all-zero shallow ancestor | medium | patch | `IsProvenShallowAncestorAsync` accepts an all-zero `rev-list` line because `IsObjectId` checks shape only. A malformed probe can reach the boundary check and be classified as unavailable instead of invalid; reject zero IDs and test that output. |
| R5-B01 CI generator lane | medium | defer | carried: R5-V01 and earlier reviews record the same pre-existing completion-gate failure before the Python tooling lane. |
| R5-B02 successor sprint keys | medium | defer | carried: L5-B13 and deferred-work.md record the pre-existing 10.2–10.4 key mismatch; the 10.1 key is resolved. |
| R5-B03 execution-time assembly receipt | medium | defer | carried: L4-B10 records that the generic TRX route lacks an execution-time binary digest; closing it requires a shared receipt protocol. |
| R5-B04 copied helper assembly | low | rejected | carried: R2-BH3 records that tests load a copied helper DLL and a deterministic build copies the same candidate bytes; execution-time binding is covered by L4-B10. |
| R5-B05 blocking argument iterator | low | rejected | carried: L3-B01 records that synchronous caller enumeration precedes the timer, while ordinary callers pass finite arrays; stopping arbitrary iterator code requires a separate execution model. |
| R5-B06 Windows enrollment race | low | rejected | carried: R5-E01 and L1-B01 cover the same pre-enrollment race. |
| R5-B07 detached Unix descendant | low | rejected | carried: L1-B02 records that a child creating a new session can evade its original group; raw Git facts do not intentionally detach children, and tracking them needs platform-specific machinery. |
| R5-B08 repository path-swap race | low | rejected | carried: L1-E06 records that a concurrent hostile local writer can replace a checked parent before use; an atomic handle-based path protocol is more than a direct correction. |
| R5-B09 artifact path-swap race | low | rejected | carried: L1-E06 and R5-B08 cover the same concurrent parent replacement premise at the artifact reader; it is outside the ordinary local candidate flow. |
| R5-B10 general shallow expressions | low | rejected | carried: L3-B07 records that unsupported side-branch expressions remain invalid rather than successful; full revision-expression proof exceeds the demonstrated HEAD forms. |
| R5-B11 Windows junction coverage | low | rejected | carried: L5-B12 records that Unix tests cover link containment and Windows CI covers process cleanup; privileged junction setup is more than a direct test correction. |
| R5-B12 stale sprint header | low | defer | carried: the third review recorded the pre-existing stale comment separately from the correctly tracked Story 10.1 status row. |

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
- Candidate: `78642807961ffc326ab1b91359c4bcdea5b4e93e`
- JSON content SHA-256 (all three digest fields zeroed): `383860c6e197b05f06ea1e365545377ef0c0f92c91b2fc0cca5fa317afc07456`

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
| `references/Hexalith.Builds` | `160000` | `db20fea8b616dc0b0c57a72d65b0c785ff460f18` |
| `references/Hexalith.Commons` | `160000` | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| `references/Hexalith.EventStore` | `160000` | `25f063e9fee2da1f4e00acb512083c2ca223d12a` |
| `references/Hexalith.Folders` | `160000` | `6a160235b9de9099b4c6e8958a0cd38efefbd0df` |
| `references/Hexalith.FrontComposer` | `160000` | `97c160a0c7b1afe0c9e30c41b6eb943b687f2b1f` |
| `references/Hexalith.Memories` | `160000` | `499b7a838a36a4cd500ab517d86acf07b1d74ad4` |
| `references/Hexalith.Parties` | `160000` | `d8cdffec3a2143e3edaf007cade7e49fd0c5c072` |
| `references/Hexalith.Projects` | `160000` | `f1c15360c09f039c7b7a4a6eb280c0d096233756` |
| `references/Hexalith.Tenants` | `160000` | `1c165899457d9950bc8bc10cb2d29dff101ab386` |

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
| `AC-10.1-01` | `0` | `PASS` | `none` | `1` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `3ab30a8a89bc65700941f444a0ac384e83b492271b782d37b64be3d1d1f08c14` |
| `AC-10.1-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-02.trx` | `8892f4d832af74118923e66f0137a2885d34ff2a73650f36873e29f58eee356e` |
| `AC-10.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-03.trx` | `de8ffba694e9ea9a1b5e80714ea0c8ba8bd49279e615bd4f9200e7a7c614cf42` |
| `AC-10.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-04.trx` | `c5e0d343849c750f91a84248a7eba7fe6ff0b2531e5b0f0f7f5e9d68fdbb313c` |
| `AC-10.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-05.trx` | `9c1a97873489f92ebdf398ced2d494a6505a304c45dc51531390f2a0bccd71be` |
| `AC-10.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.1/AC-10.1-06.trx` | `8518d64767e0ab86685938958b7081b4fdee1067c8b11ee97ea68d111d0b2518` |
| `AC-10.1-07` | `0` | `PASS` | `none` | `9` | none | none |

### `AC-10.1-01`

Command: `dotnet build tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj --configuration Release --no-restore`

| Bound output | SHA-256 |
| --- | --- |
| `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `3ab30a8a89bc65700941f444a0ac384e83b492271b782d37b64be3d1d1f08c14` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-01#0001` | `build::tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `PASS` |

### `AC-10.1-02`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.TestSupportSurfaceTests.RequiredHelpersShouldExistExactlyOnce -trx artifacts/v9/10.1/AC-10.1-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-02.trx` | `8892f4d832af74118923e66f0137a2885d34ff2a73650f36873e29f58eee356e` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-02#0001` | `Hexalith.Conversations.TestSupport.Tests.TestSupportSurfaceTests.RequiredHelpersShouldExistExactlyOnce` | `PASS` |

### `AC-10.1-03`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldDrainStdoutAndStderrConcurrently -trx artifacts/v9/10.1/AC-10.1-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-03.trx` | `de8ffba694e9ea9a1b5e80714ea0c8ba8bd49279e615bd4f9200e7a7c614cf42` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-03#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldDrainStdoutAndStderrConcurrently` | `PASS` |

### `AC-10.1-04`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldBoundAndCancelExecution -trx artifacts/v9/10.1/AC-10.1-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-04.trx` | `c5e0d343849c750f91a84248a7eba7fe6ff0b2531e5b0f0f7f5e9d68fdbb313c` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-04#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldBoundAndCancelExecution` | `PASS` |

### `AC-10.1-05`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldUseHardenedEncodingAndRawFacts -trx artifacts/v9/10.1/AC-10.1-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-05.trx` | `9c1a97873489f92ebdf398ced2d494a6505a304c45dc51531390f2a0bccd71be` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.1-05#0001` | `Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldUseHardenedEncodingAndRawFacts` | `PASS` |

### `AC-10.1-06`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.UnavailableHistoryShouldBeTypedNotSuccessful -trx artifacts/v9/10.1/AC-10.1-06.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-06.trx` | `8518d64767e0ab86685938958b7081b4fdee1067c8b11ee97ea68d111d0b2518` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

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

- Candidate: `78642807961ffc326ab1b91359c4bcdea5b4e93e`
- Candidate rule: SC-10.1 is HEAD^{commit} at first final-record generation
- Frozen inventory SHA-256: `74c4c5e9e70cb86648dc753c5edc97bdb8d598ad2a4aba55d3670794e990ca84`
- Contract SHA-256: `7db86e53754b7f917435663639a65b3953f0cf9083d436334d67f7f14c105331`

| Predecessor | Record SHA-256 |
| --- | --- |
| `7.4` | `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290` |
| `9.2` | `46d2e2df3ca74e1dad7eafa782ac1e294e1eafcd4e43b14177ddf8a411bf0453` |

| Project | Project SHA-256 | Assembly | Assembly SHA-256 |
| --- | --- | --- | --- |
| `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `9f885c50d869285e6b280b0c135b2c1449cda6f8e863271ff6420924443be925` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `3ab30a8a89bc65700941f444a0ac384e83b492271b782d37b64be3d1d1f08c14` |
| `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj` | `6d93f6ea2dbca4fb89047836f9f10ea633f2996e257830c46eaba2aaef646bf2` | `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `4fc922d7fd5f36e767c5eed41af877e762e8cf6a4ccb81816c902022c0efd9d8` |

| Result | SHA-256 |
| --- | --- |
| `artifacts/v9/10.1/AC-10.1-02.trx` | `8892f4d832af74118923e66f0137a2885d34ff2a73650f36873e29f58eee356e` |
| `artifacts/v9/10.1/AC-10.1-03.trx` | `de8ffba694e9ea9a1b5e80714ea0c8ba8bd49279e615bd4f9200e7a7c614cf42` |
| `artifacts/v9/10.1/AC-10.1-04.trx` | `c5e0d343849c750f91a84248a7eba7fe6ff0b2531e5b0f0f7f5e9d68fdbb313c` |
| `artifacts/v9/10.1/AC-10.1-05.trx` | `9c1a97873489f92ebdf398ced2d494a6505a304c45dc51531390f2a0bccd71be` |
| `artifacts/v9/10.1/AC-10.1-06.trx` | `8518d64767e0ab86685938958b7081b4fdee1067c8b11ee97ea68d111d0b2518` |

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
