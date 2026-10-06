---
title: 'Freeze the conformance assertion inventory, tier decisions, digest, and approvals'
type: 'feature'
created: '2026-10-06'
status: 'draft'
baseline_commit: 'e6611b2c6a2f5e3270122bd3515d43f7645be9c4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-9-context.md'
  - '_bmad-output/planning-artifacts/v9/story-contracts/9.1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 9.2 lacks a frozen assertion disposition, current pre-split result, and digest-bound membership approvals.

**Approach:** Freeze source/machine identities, propose tiers, obtain Quality-owner digest approval, and generate the disposition and final record.

## Boundaries & Constraints

**Always:** Honor the exact nine `9.1.json` scenarios and `V9-9.1-ENTRY-v1`. Bind source, result, build, decision, predecessor, and candidate digests. Preserve the three telemetry/status suites and accumulated FR-20 membership. Strength material is the canonical triple of bound assemblies, behavior identity, and negative-case count. Every row needs a justified public replacement or exact internal type/reason and genuine approval evidence.

**Never:** Split projects (Story 9.2), change production/public APIs or dependencies, weaken/rename/remove assertions, edit protected v1 or accepted records, or treat historical approval as approval of unseen rows. Historical checks excluded from CI cannot be counted as currently passed.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Complete approved inventory | Bound source/result and approved row digest | Deterministic closed schema, JSON, Markdown | PASS, exit 0 |
| Assertion drift | Missing, duplicate, renamed, weakened row | Reject exact observed defect | Corresponding `CONFORMANCE_ASSERTION_*` or `ASSERTION_STRENGTH_WEAKENED` |
| Incomplete disposition | Missing tier, reason, or approval | No accepted bundle | `TIER_UNASSIGNED`, `TIER_REASON_MISSING`, `TIER_APPROVAL_MISSING` |
| Preservation violation | Denominator, public shape, or v1 mutation | Reject; restore isolated fixtures exactly | `FR20_DENOMINATOR_DRIFT`, `PUBLIC_CONTRACT_WIDENED`, `V1_ARTIFACT_DRIFT` |

</frozen-after-approval>

## Open Questions

1. Which baseline should the inventory bind? **Current lane:** supported CI execution plus complete source/discovery identities and explicit historical exclusions. **Full historical lane:** every conformance case, including retired validators whose failures block completion. The contract requires every pre-split identity; current policy excludes historical validators. Neither filtered TRX nor old full results represent all current definitions.

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2651` — canonical Story 9.1 fields, commands, and ten required fault categories; architecture DC-9 adds Quality ownership.
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` — approved decision with null triage; proposal approves three suite reclassifications, not new individual rows.
- `docs/release-evidence/release-baseline-v1.json` and `preservation-traceability-manifest-v3-rc2.json` — original and accumulated exact denominator identities. V2 manifest supplies protected v1 hashes.
- `tests/Hexalith.Conversations.Conformance.Tests/` — analyze transitive fixture/helper bindings, theories, and assertion sites. Four generation methods overwrite evidence; capture in isolation.
- `_bmad/scripts/generate_ux_preservation_disposition.py` — deterministic schema/rendering and read-only verification precedent.
- `_bmad/scripts/generate_story_record.py` — reuse `v2_successor_command`, result parsing, and predecessor-pair validation; add tiering facts and retention.
- `docs/release-evidence/story-7.4-final-record-v2.json` — valid pair outside current ancestry; verify required content/digests without rewriting history.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/scripts/generate_conformance_tiering.py` — reconcile machine cases and assertion sites, derive strength/transitive bindings, propose tiers, and generate/verify deterministically. Unsupported discovery must fail.
- [ ] `artifacts/v9/9.1/` — retain source/discovery, pre-split execution, assembly hashes, command/candidate receipts, and byte-restored fault observations. Freeze before adding validation cases; label those additions separately.
- [ ] `docs/release-evidence/conformance-oracle-tiering-approvals-v2.json` — prepare digest-bound proposed membership for Quality review; record the actual owner decision only after approval. Keep historical suite approval distinct.
- [ ] `docs/release-evidence/conformance-oracle-tiering-disposition-v2.{schema.json,json,md}` — generate required closed fields, complete rows, versioned denominator membership, and digest-bound supersession after approval; preserve decision/v1 bytes.
- [ ] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceOracleTieringValidationTest.cs` — implement the seven exact AC-9.1-02 through -08 methods with independent positive checks and safe read-only verification.
- [ ] `_bmad/scripts/tests/test_conformance_tiering.py` — prove source/theory/helper completeness, deterministic output, all ten defect categories, and exact restoration through real CLI fixtures.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and `_bmad/scripts/tests/test_generate_story_record.py` — bind Story 9.1 measured facts, approvals, predecessor compatibility, outputs, fault evidence, and nonvacuous receipts; retain prior story behavior.
- [ ] `docs/runbooks/story-final-record-generation.md` — document capture selection, approval, commands, blockers, and candidate retention.
- [ ] `docs/release-evidence/story-9.1-final-record-v2.{json,md}` — generate and verify insertion; update this spec and `sprint-status.yaml` only after completion gates pass.

**Acceptance Criteria:**
- Given approved bound inputs, when AC-9.1-01 runs, then the required disposition schema and deterministic pair verify.
- Given frozen source/result identities, when AC-9.1-02 through -04 run, then every assertion occurs once with exact justified tier and unchanged strength.
- Given denominator, approvals, public baseline, and protected evidence, when AC-9.1-05 through -08 run, then membership, genuine approvals, public shape, and v1 bytes verify.
- Given passing current scenario and fault receipts, when AC-9.1-09 runs, then the derived final record binds all required facts and reports `9/9/0/0/0/0`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Run the exact nine commands in `9.1.json`; preserve declared result paths and measured summaries.
- Run focused tiering/record pytest suites and direct xUnit class checks. Use Debug locally; retain distinct Release evidence for frozen acceptance commands.
- Verify determinism, fault restoration, protected inputs, and `git diff --check`.
