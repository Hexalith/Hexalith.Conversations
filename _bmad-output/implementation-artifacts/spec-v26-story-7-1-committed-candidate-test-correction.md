---
title: 'Publish the V26 Story 7.1 committed-candidate test correction'
type: 'bugfix'
created: '2026-09-21'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'dbada954388430a6579b42508f51db806aebf3b8'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-v25-story-7-1-preservation-evidence-tooling-successor.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Immutable V25 commit `dbada954388430a6579b42508f51db806aebf3b8` verifies correctly, but two focused tests run its predecessor-only generator against committed V25 `HEAD` and fail with `V25_GENERATION_BASELINE_DRIFT`. Amending V25 or relaxing the generator would violate its preservation contract.

**Approach:** Publish an additive, non-executable V26 successor that authenticates V25 at its historical publication, replaces only the V25 focused-test blob with fixture-based generation tests, and freezes that correction behind a new exact-scope verifier.

## Boundaries & Constraints

**Always:** Publish the spec/blocker notes separately and pin that commit as V26's predecessor. Preserve V25 `dbada954388430a6579b42508f51db806aebf3b8`, tree `247843b255634a0c414f94eaf023c02cfdfc300f`, its five blobs, prior evidence, 473/969 denominators, ten raw gitlinks, and a nonempty ledger. Keep the hold `ACTIVE` and all authority flags false.

**Never:** Rewrite V25; weaken its predecessor guard; require its verifier to accept the authorized descendant; change Conformance/product code, dependencies, sprint status, submodules, gitlinks, or frozen evidence; claim completion or owner authority.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Prospective generation | Primary or linked fixture at V25 predecessor | Deterministic V25 document and schema validation | Stable V25 failure; clean fixture |
| Historical V25 | Exact `dbada954…` commit | V25 verifier passes at that revision before V26 code is trusted | Stable `V26_V25_*` blocker |
| Valid V26 | Exact predecessor and five mode-`100644` paths | V26 passes with a nonempty ledger and false authority flags | No approval or hold lift |
| Malformed publication | Parent, scope, mode, blob, history, or gitlink drift | Fail before candidate import | Stable `V26_*` state |
| Committed candidate | V25 suite from primary and linked V26 worktrees | Skip-free pass using predecessor fixtures | Failure keeps `in-progress` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py:121-160` -- move both `generate_document(ROOT)` calls to cloned predecessor fixtures and add linked-generation coverage.
- `_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py:671-795` -- immutable V25 generation and verification contract; authenticate its committed blob before loading it, and verify only historical `dbada954…`.
- `_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py` plus its schema/tests -- new closed V26 trust boundary and fault coverage.
- `_bmad-output/planning-artifacts/v26-story-7.1-committed-candidate-test-correction-v1.json` -- generate last and bind the four non-record blobs plus V25 identities.

## Tasks & Acceptance

**Execution:**
- [ ] This spec and the V25 blocker notes -- publish separately and pin the resulting V26 predecessor.
- [ ] `_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py` -- move prospective generation to isolated predecessor fixtures and add the missing linked-generation matrix cell.
- [ ] `_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py` and schema -- authenticate historical V25, authorize only the test replacement, and enforce exact scope, raw gitlinks, sticky history, snapshots, quarantine, and stable diagnostics.
- [ ] `_bmad/scripts/tests/test_publish_story_7_1_committed_candidate_test_correction.py` -- cover each matrix row, identity/stale/self-inclusion/empty-ledger faults, CLI states, and both worktree forms.
- [ ] `_bmad-output/planning-artifacts/v26-story-7.1-committed-candidate-test-correction-v1.json` -- generate last, commit exactly five mode-`100644` paths, and verify parent, scope, blobs, history, and gitlinks.

**Acceptance Criteria:**
- Given immutable V25, when V26 authenticates it, then V25 passes at `dbada954…` and all five original blobs remain exact.
- Given exact committed V26, when the V25 and V26 focused suites run in primary and linked worktrees, then every test passes without skips or xfails and both worktrees remain clean.
- Given V26 verification, when any governed path or identity changes, then a stable nonempty diagnostic blocks before authority flags can change.
- Given broader Story 7.1 gates, when rerun, then result states stay distinct and unrelated failures keep the story `in-progress`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

V26 is not a V25 repair. Its exact transaction is the new record, schema, publisher, publisher tests, and corrected V25 test file. Other V25 blobs remain exact; V25 verifies historically and V26 owns the live route.

## Verification

**Commands:**
- Run `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true` for both V25 and V26 focused test files in primary and linked worktrees -- expected: skip-free PASS.
- Verify V25 at `dbada954388430a6579b42508f51db806aebf3b8` and V26 at `HEAD` -- expected: nonvacuous PASS.
- Build/run the Conformance preservation class and eight root assemblies -- expected: preservation stays 7/7; broad results remain truthful.
- Run `git diff --check` plus raw Git parent/scope/mode/gitlink checks -- expected: exact V26 transaction and no unexpected path.
