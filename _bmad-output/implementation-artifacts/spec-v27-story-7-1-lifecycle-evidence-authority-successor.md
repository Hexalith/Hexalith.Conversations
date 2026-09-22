---
title: 'Publish the V27 Story 7.1 lifecycle-evidence authority successor'
type: 'bugfix'
created: '2026-09-22'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '57fc0f19e85a953dcac7d77eb19b277ad59a7ca7'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-v26-story-7-1-committed-candidate-test-correction.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** V24 correction `20e2cdd2b37e6387055b241c7ee45fc54e836742` makes its route sticky, but both protected hosts reject every marker-free descendant. The existing V23 escape requires signed owner approval and executable Story 7.1 authority, which is absent.

**Approach:** Publish an evidence-only V27 successor as a pinned seven-path C1 tooling commit plus a record-only C2 commit. Authenticate V24–V26 before loading V27 code; exact V27 and unchanged descendants may then return nonvacuous `PASS` while `ACTIVE` and all authority flags remain false.

## Boundaries & Constraints

**Always:** Preserve V23–V26 and frozen evidence byte-exact. Bind the exact predecessor, C1/C2 commits and trees, seven-path C1, record-only C2, mode `100644`, blobs, ten raw gitlinks, sticky full history, closed result semantics, and a nonempty all-PASS ledger. Both hosts independently authenticate the same route.

**Never:** Rewrite V23–V26; weaken any V24 descendant, deletion/reversion, mode, scope, blob, history, or downgrade guard; publish a V23 marker; claim owner evidence; set an authority flag true; lift `ACTIVE`; change product code, dependencies, workflows, frozen evidence, sprint status, submodules, or gitlinks; or advance existing lifecycle status before a nonvacuous evidence-boundary `PASS`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Exact C1/C2 or preserved descendant | Bound blobs and gitlinks unchanged | Both hosts return nonvacuous, non-executable `PASS` | No lifecycle grant |
| V24 descendant without V27 | No exact successor publication | Existing descendant-authority blocker | No fallback |
| V27 drift | Topology, scope, mode, blob, history, ledger, flag, or gitlink mismatch | Stable `V27_*` result before import | Preserve `FAIL` / `BLOCKED` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/verify_evidence_boundary.py` and its test -- add sticky V27 precedence and `V27-SCOPE-01`; preserve V24.
- `_bmad/scripts/resolve_current_planning_authority.py` and its test -- mirror V27 and return the existing non-executable envelope.
- `_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py:249-318,568-636` -- reuse authentication, exact-scope, raw-gitlink, and false-flag patterns; do not edit.
- New V27 publisher, schema, publisher test, and planning record at their canonical versioned paths -- seven C1 paths plus record-only C2. The workflow remains unchanged.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/v27-story-7.1-lifecycle-evidence-authority-v1.schema.json`, `_bmad/scripts/publish_story_7_1_lifecycle_evidence_authority.py`, and `_bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py` -- implement closed C1/C2 generation, historical authentication, exact sets, quarantine, sticky history, gitlinks, false flags, and ledger faults.
- [ ] `_bmad/scripts/resolve_current_planning_authority.py`, `_bmad/scripts/verify_evidence_boundary.py`, `_bmad/scripts/tests/test_resolve_current_planning_authority.py`, and `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- select authenticated V27 before V24 and reject hostile, missing, deleted, reverted, re-added, drifted, or vacuous successors before import.
- [ ] `_bmad-output/planning-artifacts/v27-story-7.1-lifecycle-evidence-authority-v1.json` -- generate last and publish alone as C2, binding all seven C1 paths and the one C2 path.
- [ ] `_bmad/scripts/verify_evidence_boundary.py` -- run at exact C2 before any separate status write; keep Story 7.1 and V26 `in-progress`.

**Acceptance Criteria:**
- Given exact V27 or an unchanged descendant, when either host evaluates it, then the result is nonempty-ledger `PASS`, `ACTIVE`, and four false authority flags.
- Given absent or malformed V27, when evaluated, then a stable blocker occurs without fallback, unauthenticated import, or lifecycle change.
- Given V27 passes while broader Story 7.1 gates do not, when handoff completes, then hold, sprint, review, done, release, and push state remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py _bmad/scripts/tests/test_publish_story_7_1_committed_candidate_test_correction.py` -- expected: skip-free PASS.
- `python3 _bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py --root . --verify 119c75172b501213307fab9346aa671a22bb18d2` then `python3 _bmad/scripts/publish_story_7_1_lifecycle_evidence_authority.py --root . --verify "$(git rev-parse HEAD)"` -- expected: preserved and non-executable PASS.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 57fc0f19e85a953dcac7d77eb19b277ad59a7ca7 --candidate "$(git rev-parse HEAD)"` -- expected at C2: nonvacuous `PASS` before status write.
- Run `git diff --check` and raw parent/path/mode/blob/gitlink inspection -- expected: canonical C1/C2 and no unexpected path.
