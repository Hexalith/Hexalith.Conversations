---
title: 'Story 11.3: Generate authoritative SM-2 v2 evidence and decide OQ-2'
type: 'feature'
created: '2026-10-20'
status: 'draft'
baseline_commit: '984daa891ad6afafa378bff305748bf3e62bc04c'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The proven minimal fixture lacks measured authoring-cost evidence and a current OQ-2 decision.

**Approach:** Generate and independently verify SM-2 v2 JSON/Markdown and the seven-scenario final record.

## Boundaries & Constraints

**Always:** Follow `11.3.json`. Classify all 13 authored paths once, including both AppHost files. Recompute hashes and physical LOC; record nonblank lines separately. Explain generated/platform exclusions. Use the inclusive 50% file threshold before rounding. Bind provenance, confidence, metric limitations, tool versions, single-file sensitivity and canonical payload digest. Preserve PASS/FAIL/BLOCKED and nonempty assertion ledgers.

**Never:** Alter fixture, guidance, predecessors, SM-2 v1, SM-1 `13,289`, dependencies or submodules; trust declared fixture counts; upgrade estimate confidence; restore retired authority routes. Rollback removes only 11.3 additions.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior | Blocker |
| --- | --- | --- |
| Exact fixture and compatible predecessors | Generate and verify deterministic evidence | None |
| Missing/extra authored source | Fail exact inventory comparison | `SM2_SOURCE_INVENTORY_DRIFT` |
| AppHost omitted; duplicate/unclassified path | Fail classification | `SM2_APPHOST_COST_OMITTED`; `SM2_INCLUSION_RULE_VIOLATION` |
| Declared counts disagree with bytes | Fail measurement | `SM2_MEASUREMENT_INVALID` |
| Historical baseline changed | Fail independent pinned checks | `SM1_BASELINE_MUTATED`; `SM2_V1_HISTORY_MUTATED` |
| Arithmetic/confidence/decision or format tampered | Fail independent verification | `SM2_MEASUREMENT_INVALID`; `OQ2_DECISION_INVALID`; `EVIDENCE_FORMAT_DRIFT` |
| Incompatible A–C source chain | Fail candidate binding | `CHECKPOINT_CANDIDATE_MISMATCH` |
| Fault undetected or restoration differs | Fail matrix | `FAULT_NOT_DETECTED`; `FIXTURE_NOT_RESTORED` |

</frozen-after-approval>

## Open Questions

1. Reference policy: retain the approved 58-file / 1,460-LOC estimate, label fixture measurement high confidence and comparison low, and qualify numeric `accepted` as estimate-based (recommended); or require a new measured reference for high comparison confidence (needs expanded scope/source inventory)? No historical source bodies support physical LOC remeasurement.

## Code Map

- `_bmad/scripts/verify_minimal_module_fixture.py`: reuse inventory/graph/proof checks at B's original candidate.
- `_bmad/scripts/generate_story_record.py`: reuse predecessor, receipt and pair checks, plus 11.2 publication/insertion pattern.
- `_bmad/scripts/evidence_boundary_current.py`: reuse neutral TestSupport bridge for paths/hashes/Git.
- `docs/release-evidence/oq-2-target-interpretation-decision-v1.json`: baseline hashes/denominator; SM-1 source is adjacent `consume-promote-keep-inventory-v1.json`.

## Tasks & Acceptance

**Execution:**
- [ ] `docs/release-evidence/minimal-module-authoring-cost-sm2-v2.schema.json` — closed shape/digests.
- [ ] `_bmad/scripts/verify_story_candidate.py` — AC-05: committed candidate, A/B ancestry, unchanged inputs, predecessor pairs/proof, raw gitlinks; unavailable history blocks.
- [ ] `_bmad/scripts/generate_sm2_evidence.py`, `_bmad/scripts/verify_sm2_evidence.py` — AC-01/04: independent measurement, classification, arithmetic, history pins and format parity.
- [ ] `tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs` — AC-02/03: retain v1 tests; add contract-named methods, independent history pins and TestSupport measurements; preserve CRLF.
- [ ] `_bmad/scripts/tests/test_sm2_evidence.py` — matrix rows, prohibited ownership, threshold boundaries, exact blockers/restoration.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — closed 11.3 route binding receipts/metrics/predecessors/mutations; reject stale/vacuous evidence.
- [ ] `docs/release-evidence/minimal-module-authoring-cost-sm2-v2.{json,md}`, `docs/release-evidence/story-11.3-final-record-v2.{json,md}`, `artifacts/v9/11.3/` — measure committed sources; atomic evidence publication after commitlint.
- [ ] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — verbatim record insertion, review/guards, owned transition only.

**Acceptance Criteria:**
- Given committed sources, when seven contract commands run, then summary is `7/7/0/0/0/0` with compatible A–C bindings.
- Given unchanged inputs, when generation repeats, then pairs match byte-for-byte and historical hashes stay unchanged.
- Given each mutation, when verification runs, then its exact blocker appears and original bytes restore.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Run seven exact commands in `_bmad-output/planning-artifacts/v9/story-contracts/11.3.json`; focused Debug builds before required Release receipts.
- Run `python3 -m pytest -q _bmad/scripts/tests/test_sm2_evidence.py _bmad/scripts/tests/test_generate_story_record.py` and current repository lane.
- Run 7.3 guards, effective scoped verifier and insertion check before completion; FAIL/BLOCKED halts; record valid not-applicable distinctly.
