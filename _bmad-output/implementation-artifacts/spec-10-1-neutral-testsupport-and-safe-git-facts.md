---
title: 'Story 10.1: Neutral TestSupport helpers and safe Git facts'
type: 'feature'
created: '2026-10-10'
status: 'in-progress'
baseline_commit: '2f2621aa4b40b0d44c62bf5e1e98727e7a182fec'
route: 'dispatch'
review_loop_iteration: 0
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
- [ ] `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` and `RepositoryLocator.cs`, `GitFacts.cs`, `EvidenceManifest.cs`, `BoundaryAssertions.cs`, `AssertionLedger.cs` in that directory -- create the non-shipping project and five one-type-per-file helper surfaces; provide safe repository, process, revision, diff, tree, and blob facts and usable neutral manifest/assertion structures for later stories.
- [ ] `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj`, `TestSupportSurfaceTests.cs`, and `GitFactsTests.cs` -- add xUnit v3 fixtures for surface, stream pressure, cancellation/timeout, hostile config, raw UTF-8 facts, and typed unavailable/invalid cases; restore fixture bytes.
- [ ] `Hexalith.Conversations.slnx` and `.github/workflows/ci.yml` -- include both projects in the solution and run the new tests in CI.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and focused generator tests -- bind Story 10.1 predecessor digests and project/assembly/result hashes without changing prior record meanings.
- [ ] `docs/release-evidence/story-10.1-final-record-v2.{json,md}` -- generate the deterministic pair from a committed local candidate after AC-01 through AC-06 pass; preserve the 7.4/9.2 records.

**Acceptance Criteria:**
- Given the resolved TestSupport graph, when AC-10.1-01 builds it, then it passes and remains non-packable, non-publishable, and Conversations-assembly-free.
- Given the helper surface, when AC-10.1-02 inspects it, then all five required public names occur exactly once.
- Given full concurrent streams or a hung/cancelled process, when AC-10.1-03/04 run, then output is complete or the tree is bounded and classified.
- Given hostile Git configuration and unusual paths, when AC-10.1-05 runs, then raw facts and hashes match fixture bytes.
- Given unavailable or invalid Git inputs, when AC-10.1-06 runs, then no fixture reports success and root escape is explicit.
- Given six passing results and compatible 7.4/9.2 records, when AC-10.1-07 generates the pair, then it is `PASS` with summary `7/7/0/0/0/0` and all required bindings.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet restore Hexalith.Conversations.slnx` and `dotnet build Hexalith.Conversations.slnx --configuration Release` -- both new projects build.
- Run the exact AC-10.1-01 through AC-10.1-07 commands in `_bmad-output/planning-artifacts/v9/story-contracts/10.1.json` -- each exits `0` with the contracted evidence.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- focused generator behavior passes.
