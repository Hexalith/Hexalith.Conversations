---
title: 'Publish a successor Story 7.1 entry authority'
type: 'bugfix'
created: '2026-09-20'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** At `dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607`, the V22 resolver returns `FAIL / CANDIDATE_GRAPH_DRIFT`: protected `main` is a two-parent merge, but V22 accepts only its historical direct-child transaction and hard-codes `executionAllowed: false`.

**Approach:** Pre-land successor-aware tooling while preserving V22, then generate an exact request that fails closed on missing gates. Stop for authenticated human approval; only a separate validated decision publication may return `PASS` with Story 7.1 `executionAllowed: true`.

## Boundaries & Constraints

**Always:** Derive commits, trees, path/mode sets, digests, raw mode-`160000` gitlinks, inventories, and ledgers from committed objects. Bind historical V22 candidate `cf82f8008d02b07d48338a545909d97faa302362`, the exact current merge/tree/gitlinks, Story 6.2, `7.1-SCHEMAS`, IR-0, V19/V20, Story 7.1 path inventories, the operational envelope, and current FR-20/SM-C1, SM-C2, and OQ-1 dispositions. Keep PASS, FAIL, BLOCKED, and applicable `not-applicable` distinct and nonvacuous.

**Never:** Weaken V22; copy or prefill owner approval; combine resolver tooling with the decision; rewrite V1-V22; edit Story 7.1 files, final records, submodules, gitlinks, dependencies, sprint state, release state, or push state; authorize Story done, Story 7.2, release, push, or a missing gate.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current HEAD | Two-parent `dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607` under V22 | `FAIL`, `CANDIDATE_GRAPH_DRIFT`, ACTIVE/false | Preserve exact historical result |
| Technical request | Pre-landed tooling and committed evidence | Exact candidate request; execution false | Missing approval/gates are `BLOCKED` |
| Approved publication | Exact request, gates, and trusted decision | `PASS`, all-PASS ledger, Story 7.1 execution true | Drift is FAIL or BLOCKED/false |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/resolve_current_planning_authority.py` -- preserve V22 and add last-marker successor dispatch; its current result builder cannot emit execution true.
- `_bmad/scripts/publish_story_7_1_successor_authorities.py` -- reuse trusted-owner signature, exact-transaction, raw-Git, and atomic-write patterns read-only.
- `_bmad/scripts/verify_evidence_boundary.py` -- add successor routing and exact scope; its current legacy-route PASS does not validate V22/AD-4.
- `_bmad-output/planning-artifacts/architecture.md` -- preserve V1-V22; append the decision marker only after approval. The required operational envelope is currently absent.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/schemas/v23-story-7.1-entry-authority-v1.schema.json`, `_bmad/scripts/publish_story_7_1_entry_authority.py`, `_bmad-output/planning-artifacts/v23-story-7.1-entry-candidate-v1.json`, and `_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py` -- implement closed request/decision/results and deterministic trusted publication.
- [ ] `_bmad/scripts/resolve_current_planning_authority.py` and `_bmad/scripts/tests/test_resolve_current_planning_authority.py` -- retain V22; validate successor graph, scope, history, marker, gitlinks, inventories, gates, approval, and result semantics.
- [ ] `_bmad/scripts/verify_evidence_boundary.py` and `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- add exact successor route/scope and restoration/anti-vacuity faults.
- [ ] Commit only those tooling/request paths over `dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607`, validate them, then stop with exact evidence. After explicit approval and valid dispositions, publish `_bmad-output/planning-artifacts/v23-story-7.1-entry-authority-v1.json` plus the append-only architecture marker separately; do not push.

**Acceptance Criteria:**
- Given unchanged V22 history, `cf82f8008d02b07d48338a545909d97faa302362` remains PASS/false and current HEAD remains FAIL/`CANDIDATE_GRAPH_DRIFT`.
- Given missing approval or gate evidence, the successor returns BLOCKED or FAIL with execution false and a nonempty ledger.
- Given the exact separately approved publication and all gates, evidence-boundary and current-authority checks pass nonvacuously, only Story 7.1 has execution true, and no prohibited path changed.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_entry_authority.py` -- expected: nonzero, skip-free PASS with named PASS/FAIL/BLOCKED faults.
- `uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607 --candidate HEAD` -- expected for tooling: PASS, nonempty ledger.
- `uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate HEAD --check` -- expected: PASS/true only after the separate valid owner publication; otherwise FAIL or BLOCKED/false.
- `git diff --check` -- expected: no whitespace errors in the scoped candidate.
