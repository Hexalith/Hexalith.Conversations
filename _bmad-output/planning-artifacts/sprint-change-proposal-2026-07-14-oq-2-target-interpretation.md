# Sprint Change Proposal - Resolve OQ-2 Target Interpretation

**Date:** 2026-07-14T14:19:25+02:00
**Project:** Conversations
**Requested by:** Jerome
**Workflow:** bmad-correct-course
**Mode:** Batch
**Status:** Approved and implemented
**Approved by:** Jerome on 2026-07-14

## 1. Issue Summary

The Conversations Boilerplate Reduction PRD left OQ-2 open: confirm the numeric targets and interpretation rules for SM-1 and SM-2. Story 5.3 therefore reported both measurements without a target-met claim, and the Epic 5 retrospective carried an explicit Product/Release Owner action to decide OQ-2.

The decision is now actionable because the authoritative evidence exists:

- SM-1: accepted classified-plumbing baseline **13,289 LOC**; removed or externalized **9,360 LOC**; reduction **70.43%**.
- SM-2: template minimal **29 files / 468 LOC**; pre-initiative equivalent **58 files / 1,460 LOC**; reduction **50.00% files / 67.95% LOC**.
- SM-2 evidence limitation: the template value is an accepted manifest baseline, while the pre-initiative equivalent is a low-confidence estimate rather than an exact reconstructed buildable skeleton.
- The PRD pre-specified assumed thresholds before the final measurements: SM-1 **at least 40%** and SM-2 **at least 50% fewer files**.

A same-day release-owner decision already binds `success-metric-report-and-attestation-v1.{json,md}` by hash and accepts OQ-2 as unresolved at the time of signature. Its invalidation rule requires a new release-owner decision if either bound file changes. This correction must therefore preserve those historical, signed artifacts byte-for-byte and record OQ-2 as a subsequent decision addendum.

Core problem: the numeric evidence is accepted, but the project lacks an approved rule that says which threshold applies, how equality is treated, which SM-2 dimension is decisive, and how low-confidence comparison evidence affects the result label.

Issue type: unresolved original requirement / stakeholder decision, not a technical implementation failure.

## 2. Impact Analysis

### Epic and Story Impact

All five epics and Story 5.3 remain complete. No story needs rollback, reopening, resequencing, or replacement.

- **Epic 1:** The frozen SM-1 denominator and inventory governance remain unchanged.
- **Epics 2-3:** Implementation and row dispositions remain unchanged.
- **Epic 4:** The accepted SM-2 measurement boundary and numeric evidence remain unchanged.
- **Epic 5:** The signed release-owner record remains historically valid. A subsequent OQ-2 decision closes one follow-up action without altering the earlier signature or overall release decision.
- **Future epics:** None are invalidated and no new epic is required.

### Artifact Impact

- **Boilerplate-reduction PRD:** Update Success Metrics, Open Questions, Assumptions Index, and document metadata so OQ-2 is closed explicitly.
- **Epic breakdown:** Replace remaining "assumed/to be confirmed" OQ-2 language with the approved interpretation and annotate completed Story 4.2/5.3 criteria.
- **Decision log:** Append the dated OQ-2 decision and its evidence-confidence rule.
- **New release evidence:** Add an authoritative JSON decision and a human-readable Markdown summary.
- **Validation:** Add a focused conformance/documentation test that proves the decision math, thresholds, evidence labels, and immutable-source bindings.
- **Sprint status:** Mark the open Epic 5 OQ-2 action `done` and add a concise completion record.
- **Historical evidence:** Do not modify the SM-2 baseline v1, Story 5.3 success report v1, or the signed release-owner decision v1. Their `unconfirmed` wording remains a point-in-time statement and their hashes remain valid.
- **Implementation-readiness reports and retrospectives:** Do not rewrite them; they are point-in-time records.

### Architecture, UX, and Technical Impact

- **Architecture:** No component, API, data model, topology, package, or deployment change.
- **UX:** N/A. The initiative explicitly has no UI/UX scope.
- **Runtime/public contracts:** No change.
- **Source/generated/submodule content:** No change.
- **Test impact:** One focused evidence-validation test; no product or conformance behavior changes.

## 3. Recommended Approach

**Selected path:** Direct Adjustment.
**Scope:** Minor, after Product/Release Owner approval.
**Effort:** Low.
**Risk:** Low to medium because evidence wording must not invalidate the signed attestation boundary.

### Recommended OQ-2 Decision

OQ-2 is **resolved-confirmed** with these rules:

1. **SM-1 target**
   - Threshold: **at least 40%** of the accepted classified-plumbing baseline removed or externalized.
   - Formula: `removedOrExternalizedPlumbingLoc / baselinePlumbingLoc * 100`.
   - Denominator: the frozen Story 1.4 accepted baseline, **13,289 LOC**; it is not re-estimated after implementation.
   - Comparison: inclusive; equality meets the target because the operator is `>=`.
   - Current result: **met** at **70.43%** (`9,360 / 13,289`).

2. **SM-2 target**
   - Threshold: **at least 50% fewer hand-authored, module-owned files** for the minimal valid module within the frozen Story 4.1 measurement boundary.
   - Formula: `(preInitiativeFileCount - templateMinimalFileCount) / preInitiativeFileCount * 100`.
   - Comparison: inclusive; exactly 50.00% meets the target.
   - Decisive dimension: file-count reduction. LOC reduction remains mandatory supporting evidence but is not a second pass/fail threshold because the PRD only pre-specified a numeric file target.
   - Current result: **met-on-accepted-estimate** at **50.00% files** (`29` versus estimated `58`), supported by **67.95% LOC** reduction.
   - Confidence rule: `met-on-accepted-estimate` is not upgraded to an unconditional or high-confidence pass. The low-confidence pre-initiative estimate and manifest-only template limitation remain visible wherever the result is reported.

This preserves the pre-measurement thresholds rather than choosing new thresholds after seeing the outcomes. It also separates three concepts that were previously collapsed: target approval, mathematical target result, and evidence confidence.

### Alternatives Considered

- **Rollback:** Not viable or useful; no implementation defect caused the open decision.
- **MVP review:** Not needed; all PRD capabilities and evidence already exist.
- **Leave OQ-2 unresolved:** Rejected because it perpetuates the explicit Epic 5 follow-up and prevents stable target interpretation.
- **Treat SM-2 as an unconditional pass:** Rejected because it would hide the low-confidence estimated comparator.
- **Require both SM-2 files and LOC to meet separate 50% thresholds:** Rejected because the PRD pre-specified only the file-count threshold; adding a retroactive LOC threshold would change the target after measurement.

Timeline impact: none on completed implementation; one bounded documentation/evidence validation change.

## 4. Detailed Change Proposals

### OQ-2 Decision Evidence

Artifacts:

- `docs/release-evidence/oq-2-target-interpretation-decision-v1.json`
- `docs/release-evidence/oq-2-target-interpretation-decision-v1.md`

OLD:

```text
No dedicated OQ-2 decision artifact exists. Consumers must infer unresolved status from the PRD, SM-2 baseline, Story 5.3 report, and sprint action.
```

NEW:

```text
A subsequent Product/Release Owner decision records:
- OQ-2 status: resolved-confirmed
- SM-1 target: >=40%, inclusive; current result met at 70.43%
- SM-2 target: >=50% fewer files, inclusive; current result met-on-accepted-estimate at 50.00%
- SM-2 LOC reduction: mandatory supporting evidence, not a second numeric threshold
- SM-2 confidence: low because the comparator is estimated and the template is manifest-only
- historical report/baseline/release-owner-decision hashes and the rule that those files remain unchanged
- approval reference: this approved Sprint Change Proposal
```

Rationale: A dedicated decision artifact makes current interpretation machine-readable while preserving the signed, point-in-time release evidence.

### PRD Success Metrics

Artifact: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md`, §7

OLD:

```md
- **SM-1 — Conversations plumbing reduction.** ... `[ASSUMPTION: target ≥ 40% of classified plumbing LOC removed/delegated; confirm the number.]`
- **SM-2 — New-module authoring cost.** ... `[ASSUMPTION: target ≥ 50% fewer files for a minimal module; confirm.]`
```

NEW:

```md
- **SM-1 — Conversations plumbing reduction.** Target: ≥40% of the frozen accepted classified-plumbing LOC removed or externalized, computed inclusively against the Story 1.4 baseline. Current evidence: 70.43%; target met.
- **SM-2 — New-module authoring cost.** Target: ≥50% fewer hand-authored, module-owned files within the frozen Story 4.1 minimal-module boundary, computed inclusively. LOC remains supporting evidence, not a second threshold. Current evidence: 50.00% files / 67.95% LOC; target met on an accepted low-confidence estimate.
```

Rationale: Closes the assumption without changing the originally proposed numbers and makes the SM-2 confidence limitation inseparable from the result.

### PRD Open Question and Assumptions Index

Artifact: same PRD, §§12-13

OLD:

```md
2. **OQ-2:** Confirm SM-1/SM-2 numeric targets (currently assumed ≥40% plumbing LOC, ≥50% fewer files).
...
- §7 — SM-1 ≥40% plumbing LOC removed/delegated; SM-2 ≥50% fewer files for a minimal module; SM-4 light qualitative check.
```

NEW:

```md
2. **OQ-2 — Resolved 2026-07-14:** Confirmed SM-1 ≥40% and SM-2 ≥50% fewer files, both inclusive. SM-2's file count is decisive; LOC is supporting evidence; the current result is estimate-qualified.
...
- §7 — OQ-2 resolved: SM-1 and SM-2 targets are confirmed under the recorded interpretation; SM-4 remains a light qualitative check.
```

Also update PRD `updated` metadata to `2026-07-14`.

### Epic/Story Clarifications

Artifact: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Representative OLD:

```md
SM-1 plumbing-LOC reduction target [ASSUMPTION ≥40%, OQ-2]; SM-2 ... [ASSUMPTION ≥50% fewer files, OQ-2]
```

Representative NEW:

```md
SM-1 target ≥40% and SM-2 target ≥50% fewer files are confirmed by the 2026-07-14 OQ-2 decision; both comparisons are inclusive, and SM-2 remains estimate-qualified.
```

Update the Story 4.2 and Story 5.3 OQ-2 parentheticals similarly. Do not rewrite story completion facts or measured values.

### Decision Log

Artifact: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.decision-log.md`

Append a dated entry that records the exact thresholds, inclusive comparison, SM-2 decisive/supporting dimensions, evidence-confidence label, current results, and preservation of the earlier signed evidence boundary.

### Focused Validation

Artifact: `tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs`

Add focused assertions that:

- the JSON and Markdown decision pair exists and agrees;
- SM-1 recomputes to 70.43% from the accepted report values and exceeds the confirmed inclusive 40% threshold;
- SM-2 recomputes to 50.00% from 29 and 58 and meets the inclusive 50% threshold;
- SM-2 uses file count as the decisive threshold and preserves LOC as supporting evidence;
- the low-confidence/estimated/manifest-only limitations remain explicit;
- the historical SM-2 baseline, success report, and signed release-owner decision hashes match the decision's bindings;
- the decision does not claim an overall release re-signature, platform-control approval, or high-confidence SM-2 proof.

Rationale: The decision is part of release evidence and needs a mechanical guard against semantic drift.

### Sprint Status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- epic: 5
  action: "Make an explicit OQ-2 decision for SM-1 and SM-2 target interpretation."
  owner: "Product/Release owner"
  status: open
```

NEW:

```yaml
- epic: 5
  action: "Make an explicit OQ-2 decision for SM-1 and SM-2 target interpretation."
  owner: "Product/Release owner"
  status: done
```

Add a dated status comment summarizing `resolved-confirmed`, SM-1 `met`, and SM-2 `met-on-accepted-estimate`.

### Explicit No-Change Boundary

The following remain byte-identical:

- `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.{json,md}`
- `docs/release-evidence/success-metric-report-and-attestation-v1.{json,md}`
- `docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.{json,md}`

Rationale: They are accepted or signed point-in-time evidence. The new decision supersedes OQ-2 interpretation prospectively without rewriting history or invalidating the signed release-owner record.

## 5. Implementation Handoff

**Classification:** Minor.
**Recipients:** Product/Release Owner for approval; Developer for artifact/test implementation; Release reviewer for ongoing interpretation.

Responsibilities:

- **Product/Release Owner:** Approve the target thresholds, inclusive rules, SM-2 decisive dimension, and estimate-qualified result label.
- **Developer:** Create the decision pair, update current planning artifacts and sprint status, add focused validation, and preserve the no-change boundary.
- **Release reviewer:** Treat the new OQ-2 artifact as the current target-interpretation authority while retaining the earlier signed attestation as historical release evidence.

Success criteria:

- OQ-2 has one authoritative, approved, machine-readable decision.
- SM-1 is reported as `met` at 70.43% against an inclusive 40% threshold.
- SM-2 is reported as `met-on-accepted-estimate` at 50.00% files, with 67.95% LOC supporting evidence and low confidence explicit.
- Planning artifacts no longer describe OQ-2 as currently open.
- The sprint action is `done`.
- Historical accepted/signed evidence remains byte-identical.
- Focused validation passes with no runtime/public-contract/submodule changes.

## 6. Change Navigation Checklist

### Trigger and Context

- [x] 1.1 Trigger identified: Story 5.3 residual risk and Epic 5 open Product/Release Owner action.
- [x] 1.2 Problem classified: unresolved original target requirement, not implementation failure.
- [x] 1.3 Evidence collected: PRD assumptions, accepted SM-1/SM-2 values, confidence limitations, signed attestation boundary.

### Epic Impact

- [x] 2.1 Epic 5 remains complete.
- [N/A] 2.2 No epic scope or acceptance change is required.
- [x] 2.3 All completed epics reviewed; only current interpretation references need annotation.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No resequencing or priority change is needed.

### Artifact Conflict and Impact

- [x] 3.1 Boilerplate-reduction PRD requires explicit target and open-question updates; MVP remains achieved.
- [N/A] 3.2 The initiative has no dedicated architecture artifact and no architecture change.
- [N/A] 3.3 The initiative has no UI/UX scope.
- [x] 3.4 Release evidence, validation, decision log, epic annotations, and sprint status impacts are defined.

### Path Forward

- [x] 4.1 Direct Adjustment is viable; effort low, risk low to medium.
- [N/A] 4.2 Rollback is not viable or useful.
- [N/A] 4.3 MVP review is unnecessary.
- [x] 4.4 Direct Adjustment selected with an immutable historical-evidence boundary.

### Proposal and Handoff

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended decision and alternatives documented.
- [x] 5.4 MVP is unaffected; action plan is bounded.
- [x] 5.5 Approval, implementation, and review responsibilities defined.
- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked for numerical and evidence-boundary consistency.
- [x] 6.3 Jerome explicitly approved the proposal on 2026-07-14.
- [N/A] 6.4 No epic/story topology update is needed; only an existing sprint action changes status after approval.
- [x] 6.5 Handoff and success criteria defined.

## 7. Approval and Completion

Jerome approved this Sprint Change Proposal on 2026-07-14. The bounded implementation is complete within the explicit scope and no-change boundary above.

## 8. Completion Record

Implemented artifacts:

- Added `docs/release-evidence/oq-2-target-interpretation-decision-v1.{json,md}`.
- Updated the boilerplate-reduction PRD, epic annotations, and decision log.
- Added `OqTwoTargetInterpretationDecisionValidationTest`.
- Marked the Epic 5 OQ-2 action `done` in `sprint-status.yaml`.
- Preserved the six accepted/signed historical evidence files byte-identically.

Validation:

- Release conformance test-project build: passed, 0 warnings, 0 errors.
- Focused OQ-2 decision validation: 4/4 passed.
- Full conformance executable: 388/388 passed, 0 errors, failures, skips, or not-run tests.

Handoff completed to the Developer implementation path for this Minor change. The Product/Release Owner owns the approved target interpretation, and release reviewers should use the OQ-2 decision artifact as the current authority while retaining the earlier signed attestation as historical evidence.
