---
title: 'Publish the V25 Story 7.1 preservation-evidence tooling successor'
type: 'bugfix'
created: '2026-09-21'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'e257c3f84bd3e88ea77e4cfb3032c4a15dd5dec5'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-21.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Three Conformance failures conflate immutable rc.2 evidence with the live candidate and assume `.git` is a directory, blocking a truthful final record.

**Approach:** Publish a non-executable V25 successor that authenticates V23/V24, validates rc.2 at historical time bases, binds the live assembly to the evaluated commit, and isolates fault fixtures from repository layout.

## Boundaries & Constraints

**Always:** Publish proposal/spec separately, then freeze that `HEAD` as V25's predecessor. Preserve V23 `5a7234b…`, V24 `20e2cdd…`, rc.1/approval, rc.2 `5ad5d3c…`, 473 tests, 969 obligations, ten gitlinks, and a nonempty ledger. Keep the hold `ACTIVE` and authority flags false.

**Never:** Rewrite frozen evidence; edit V23/V24; change public test identities, dependencies, product code, sprint status, submodules, or gitlinks; weaken candidate binding or claim owner/completion/release/push authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Valid V25 | Exact predecessor and five mode-`100644` paths | `PASS`, nonempty ledger, all authority flags false | No approval or hold lift |
| Frozen rc.2 | Publication plus later log provenance | Validate bytes at recorded revisions | `RC2_PUBLICATION_SCOPE_DRIFT` or `RC2_HISTORICAL_ARTIFACT_MISMATCH` |
| Live candidate | One 40-hex assembly revision equal to `HEAD`, with V24 ancestry | Current-candidate assertion passes independently | `CURRENT_CANDIDATE_*` diagnostic |
| Malformed lineage/scope | Wrong parent, duplicate, scope/mode/blob/gitlink drift, deletion/reversion | Fail before candidate import | Stable `V25_*` `FAIL`/`BLOCKED` |
| Fault fixture | Primary or linked worktree | Run file/index/gitlink faults in a temporary repository | Preserve diagnostics and clean up |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_story_7_1_entry_authority.py` -- copy safe Git/JSON/history/write patterns; never edit or import it.
- `_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py` -- independent V25 generator/verifier; authenticate V23/V24 before candidate data.
- `_bmad/schemas/v25-story-7.1-preservation-evidence-tooling-successor-v1.schema.json` -- closed Draft 2020-12 contract.
- `_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py` -- positive, fault, sticky-history, write, and CLI coverage.
- `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs:185-579,751-945,1096-1478` -- split historical/live checks and reuse the isolated-repository fixture.
- `_bmad-output/planning-artifacts/v25-story-7.1-preservation-evidence-tooling-successor-v1.json` -- generate last; bind the other four V25 blobs and frozen identities.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-21.md` and this spec -- publish separately; pin that commit as V25 predecessor.
- [ ] `_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py` and its V25 schema -- implement exit semantics, exact sets, raw gitlinks, sticky history, snapshots, quarantine, and stable `V25_*` codes.
- [ ] `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` -- check the 36-path publication, two logs at retained provenance, frozen receipts without the live DLL, and one live 40-hex revision equal to `HEAD` with V24 ancestry.
- [ ] `_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py` -- test bad ancestry, duplicates/ninth path, drift, deletion/reversion, stale inputs, self-inclusion, empty ledger, and identity swaps.
- [ ] `_bmad-output/planning-artifacts/v25-story-7.1-preservation-evidence-tooling-successor-v1.json` -- generate last; commit exactly five mode-`100644` paths and verify parent, scope, blobs, history, and gitlinks.

**Acceptance Criteria:**
- Given immutable rc.2, when checked from a descendant, then its transaction, retained logs, receipts, identities, counts, and gate states validate without mutable descendant scope.
- Given exact V25, when focused tests run in primary and linked worktrees, then all pass without skips and both remain clean.
- Given committed V25, when the stamped rebuild, eight root projects, boundary gates, and final-record generator run, then results are 2,026/2,026 and 473/473 with a verified digest; otherwise Story 7.1 stays `in-progress`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The rc.2 publication has 36 paths. Its two log files first become committed at `df482b4e652907e100f763615a8a8c4370565066`; bind their bytes and provenance separately.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py` -- expected: skip-free PASS.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release -p:SourceRevisionId=$(git rev-parse HEAD)` then execute the built assembly with the preservation test class filter -- expected: PASS.
- Run all eight root test assemblies and repeat Conformance in an isolated linked worktree -- expected: 2,026/2,026 and 473/473, zero failed/skipped/not-run.
- Run V24/V25 verifiers, submodule gate, final-record digest verifier, `git diff --check`, and raw Git scope/mode/gitlink checks -- expected: nonvacuous `PASS` and no unexpected path.
