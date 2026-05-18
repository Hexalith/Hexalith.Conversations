---
workflow: bmad-correct-course
date: 2026-05-17
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-17.md
status: approved-applied
scope_classification: Moderate
recommended_path: Direct Adjustment
approval_status: approved
approved_by: Jerome
approved_date: 2026-05-17
approval_reconfirmed_by: Jerome
approval_reconfirmed_date: 2026-05-18
---

# Sprint Change Proposal: Implementation Readiness Gates

**Date:** 2026-05-17
**Project:** Hexalith.Conversations
**Trigger:** Implementation readiness assessment returned `NEEDS WORK`.
**Recommended path:** Direct adjustment inside the existing epic/story set.
**Scope classification:** Moderate backlog correction.

## 1. Issue Summary

The implementation readiness assessment found that the Hexalith.Conversations planning set is strong enough to preserve: PRD, architecture, epics, UX specification, and UX requirement map are present; epics cover all 104 PRD functional requirements; all 52 UX-DR labels are mapped; and no critical structural violations were found.

The issue is not missing product intent. The issue is that several already-known decision blockers and checklist-shaped verification stories are not yet enforceable enough for sprint execution. If stories are pulled before these gates are converted into explicit Ready for Dev preconditions, implementers can accidentally overbuild v1 scope, hard-code undecided semantics, or close broad verification stories with incomplete evidence.

### Evidence

- `implementation-readiness-report-2026-05-17.md` overall status: `NEEDS WORK`.
- Major readiness issues:
  - Decision blockers must become story-level Ready for Dev preconditions.
  - Story 3.8 must be split or retained as an epic-level verification checklist with named evidence ownership.
  - Story 6.8 must be split or retained only with named ownership and an explicit evidence plan.
- Scope-protection warnings:
  - Temporal evidence anchors remain undecided.
  - Generate Evidence Bundle is deferred to v1.1.
  - Mobile governance mutation is blocked by default.
  - Future derived indexes and exports are ADR-gated unless promoted into active v1 scope.
  - FrontComposer generated baseline surfaces are insufficient for trust-critical components without custom evidence, redaction, audit, citation, freshness, and trust posture components.

## 2. Checklist Findings

| Checklist item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | No single implementation story triggered this. The trigger is the readiness assessment before sprint execution. |
| 1.2 Core problem | [x] Done | Known blockers and verification bundles are documented but not sufficiently enforceable as story preconditions and assignment gates. |
| 1.3 Supporting evidence | [x] Done | Readiness report, epics readiness gates, Story 3.8, Story 6.8, Story 2.4.3, PRD v1/v1.1 scope, UX responsive/mobile rules, and architecture open decisions. |
| 2.1 Current epic impact | [x] Done | Epics remain valid. Epics 3 and 6 need story assignment gates. Epic 2 needs v1 scope wording protection. |
| 2.2 Epic-level changes | [x] Done | No new epic required. Existing epics need story-level preconditions and split/owner-gate decisions. |
| 2.3 Remaining epic impact | [x] Done | Epics 1, 3, 4, 5, and 6 are affected by decision preconditions; Epic 2 is affected by redaction/export/index scope protection. |
| 2.4 Future epic validity | [x] Done | No planned epic is obsolete. No new epic is required. |
| 2.5 Priority/order impact | [x] Done | Sprint planning must sequence gated stories after their decisions; otherwise epic order remains intact. |
| 3.1 PRD conflicts | [x] Done | No PRD conflict. v1/v1.1 scope must be preserved around Evidence Bundle, exports, and future derived indexes. |
| 3.2 Architecture conflicts | [x] Done | No architecture conflict. Open decisions must block dependent stories before implementation. |
| 3.3 UX conflicts | [x] Done | No UX conflict. UX safety gates require named ownership for accessibility, responsive, clipboard, telemetry, and Leak Sentinel evidence. |
| 3.4 Other artifacts | [x] Done | Created readiness gate tracking. No Conversations sprint tracker exists yet. |
| 4.1 Direct adjustment | [x] Viable | Best path. Low-to-medium effort, low product disruption, preserves existing artifacts. |
| 4.2 Rollback | [x] Not viable | No completed implementation needs rollback. |
| 4.3 PRD MVP review | [x] Not viable | MVP scope is still viable; the risk is accidental scope promotion, not incorrect MVP definition. |
| 4.4 Recommended path | [x] Done | Direct adjustment with story preconditions, split/owner gates, and v1 scope notes. |
| 5.1 Issue summary | [x] Done | Captured in this proposal. |
| 5.2 Impact and adjustments | [x] Done | Captured below by artifact. |
| 5.3 Path rationale | [x] Done | Direct adjustment keeps momentum while preventing unsafe story pull. |
| 5.4 MVP impact | [x] Done | No MVP reduction. Add enforcement so v1 remains read-only where required and v1.1 work is not pulled forward accidentally. |
| 5.5 Handoff plan | [x] Done | PO/Developer update backlog; Architect/PM decide gates; TEA/SRE own evidence gates. |
| 6.1 Checklist completion | [x] Done | All applicable sections addressed. |
| 6.2 Proposal accuracy | [x] Done | Proposal is grounded in the readiness report and current planning artifacts. |
| 6.3 User approval | [x] Done | Jerome approved the proposal on 2026-05-17. |
| 6.4 Sprint status update | [N/A] Skip | No Conversations `sprint-status.yaml` exists yet. |
| 6.5 Handoff confirmation | [x] Done | Handoff route and responsibilities are recorded in this proposal. |

## 3. Impact Analysis

### Epic Impact

**Epic 1: Tenant-Safe Conversation Record**

Epic 1 remains valid. Stories affected by projection freshness, EventStore envelope ownership/evolution, tenant projection freshness, and trust/freshness vocabulary must not enter development until their applicable preconditions are decided or waived.

Affected stories:

- Story 1.7
- Story 1.8
- Story 1.11

**Epic 2: Governed Retention, Redaction, and Audit**

Epic 2 remains valid. Story 2.4.3 needs stronger v1 scope wording to prevent future derived indexes and exports from being implemented unless promoted by active release scope and ADR coverage.

Affected story:

- Story 2.4.3

**Epic 3: Compliance Investigation Workspace**

Epic 3 remains valid. Story 3.8 must not be pulled as a normal implementation story unless split into separately owned verification stories. Temporal evidence anchor, command availability metadata, projection freshness semantics, and UX safety ownership must gate dependent work.

Affected stories:

- Story 3.1
- Story 3.2
- Story 3.4
- Story 3.8

**Epic 4: Adopter Integration and Developer Readiness**

Epic 4 remains valid. Story 4.2 depends on projection freshness blocking semantics and raw HTTP fallback approval. Story 4.4 depends on projection freshness semantics. Raw HTTP fallback must remain omitted unless buyer approval is recorded or diagnostics explicitly require it.

Affected stories:

- Story 4.2
- Story 4.4

**Epic 5: Conformance, Compatibility, and Release Evidence**

Epic 5 remains valid. Story 5.9 depends on EventStore envelope ownership/evolution. Performance evidence and GA release-gate closure depend on numeric thresholds or buyer-accepted unknowns.

Affected stories:

- Story 5.9
- Performance and release evidence stories tied to numeric NFR gates

**Epic 6: Operations, Observability, and Lifecycle Commitments**

Epic 6 remains valid. Story 6.8 must be split by default unless a named owner accepts both telemetry redaction and cardinality evidence with approved rules, dimensions, fixture set, evidence output, and pass/fail gates.

Affected stories:

- Story 6.2
- Story 6.8

### Artifact Impact

**PRD**

No PRD rewrite is required. The PRD already defines the v1/v1.1 boundary, including read-only v1 governance viewer and v1.1 Generate Evidence Bundle. The change is to preserve that boundary in implementation stories and sprint gates.

**Architecture**

No architecture rewrite is required. Architecture already identifies open decisions around temporal evidence anchor, projection freshness behavior, command availability, EventStore envelope exposure/evolution, and numeric thresholds. The change is to enforce those decisions as story preconditions.

**UX**

No UX rewrite is required. UX already defines mobile read-only triage, responsive disclosure surfaces, accessibility safety, clipboard safety, telemetry safety, and Leak Sentinel coverage. The change is to assign evidence ownership and split verification work where needed.

**Sprint/Implementation Artifacts**

Action is required after approval:

- Add or update `_bmad-output/implementation-artifacts/readiness-gates.md`.
- Add Ready for Dev precondition blocks to affected stories or story records.
- Add split/owner-gate decisions for Story 3.8 and Story 6.8 before assignment.
- Update sprint tracking only if story statuses, split story IDs, or assignment gates are represented there.

## 4. Recommended Approach

Use **Direct Adjustment**.

This is the lowest-risk path because the existing planning artifacts are coherent. The correction should add enforcement where the artifacts already identify risk, rather than rewriting requirements or replanning the product.

### Alternatives Considered

**Potential rollback:** Not recommended. No implementation rollback is needed because the problem was found before sprint execution.

**PRD MVP review:** Not recommended. MVP scope remains valid. The PRD already states that full Evidence Bundle export, richer retention editing, and future derived indexes are outside or gated from v1.

### Effort and Risk

- Effort: Medium.
- Risk if implemented: Low.
- Risk if skipped: High. Teams may pull blocked stories too early, merge semantics before ADRs are ready, or accidentally promote v1.1 scope into v1.

## 5. Detailed Change Proposals

### Proposal A: Add Story-Level Ready for Dev Preconditions

**Artifact:** `epics.md` or sprint-ready story records.

**Current behavior:**

The epics document has an `Implementation Readiness Gates` section and an `ADR-Gated Story Stop Conditions` section. These gates are useful, but the readiness assessment found they may not stop individual stories from being pulled unless each affected story carries explicit Ready for Dev preconditions.

**Proposed addition pattern:**

Add this block to each affected story, customized to the gates that apply:

```markdown
**Ready for Dev Preconditions:**

- The applicable readiness gate row exists in `_bmad-output/implementation-artifacts/readiness-gates.md`.
- Each applicable gate is `decided` or `waived` before implementation starts.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.
- The story owner records evidence output and pass/fail criteria before implementation starts.
```

**Apply to:**

- Story 1.7 and Story 1.8: projection freshness blocking semantics and shared trust/freshness vocabulary.
- Story 1.11: EventStore envelope ownership/evolution.
- Story 3.1 and Story 3.2: projection freshness blocking semantics and trust/freshness vocabulary.
- Story 3.4: temporal evidence anchor, citation/temporal link behavior, command availability metadata where applicable.
- Story 4.2: projection freshness blocking semantics; raw HTTP fallback approval if raw HTTP examples are included.
- Story 4.4: projection freshness blocking semantics.
- Story 5.9: EventStore envelope ownership/evolution.
- Story 6.2: projection freshness semantics and operational freshness vocabulary.
- Performance or release evidence stories: numeric thresholds or buyer-accepted unknown status.

**Rationale:**

The gate already exists at document level. The change makes it executable at story pull time.

### Proposal B: Split or Owner-Gate Story 3.8

**Story:** Story 3.8: Verify Responsive and Accessible Investigation Experience.

**Current text:**

```markdown
**Assignment Rule:** Story 3.8 is an epic-level verification checklist by default. If assigned as ordinary implementation work, split before kickoff into:

- Story 3.8A: Verify Responsive Layout and Mobile Safe Triage.
- Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety.
- Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety.

Each split story must name owner, fixture set, evidence output, and pass/fail gate before implementation starts.
```

**Proposed replacement:**

```markdown
**Assignment Rule:** Story 3.8 remains an epic-level verification checklist unless the sprint plan explicitly splits it before assignment.

Ready for Dev requires one of these two decisions:

- Checklist mode: a named evidence owner accepts Story 3.8 as an epic-level verification checklist and records fixture set, evidence output, pass/fail gate, and review date.
- Split mode: create the following independently closable stories before kickoff:
  - Story 3.8A: Verify Responsive Layout and Mobile Safe Triage.
  - Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety.
  - Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety.

Each split story must name owner, fixture set, evidence output, and pass/fail gate before implementation starts. Story 3.8 must not be assigned as ordinary single-owner implementation work.
```

**Rationale:**

This keeps the existing intent and makes the assignment decision enforceable. Responsive safety, accessibility safety, and disclosure/telemetry safety have different fixtures and failure modes.

### Proposal C: Split or Owner-Gate Story 6.8

**Story:** Story 6.8: Validate Operational Telemetry Redaction and Cardinality.

**Current text:**

```markdown
**Assignment Rule:** Story 6.8 is an epic-level validation checklist by default. It may remain one story only when a named owner and evidence plan cover both telemetry redaction and telemetry cardinality gates. Otherwise split before kickoff into:

- Story 6.8A: Validate Operational Telemetry Redaction.
- Story 6.8B: Validate Operational Telemetry Cardinality Gates.

Each split story must name owner, fixture set, approved dimensions or redaction rules, evidence output, and pass/fail gate before implementation starts.
```

**Proposed replacement:**

```markdown
**Assignment Rule:** Story 6.8 remains an epic-level validation checklist unless a named owner accepts both validation domains or the sprint plan splits it before assignment.

Ready for Dev requires one of these two decisions:

- Checklist mode: a named SRE/test owner accepts both telemetry redaction and telemetry cardinality gates, with approved dimensions, redaction rules, fixture set, evidence output, pass/fail gate, and review date.
- Split mode: create the following independently closable stories before kickoff:
  - Story 6.8A: Validate Operational Telemetry Redaction.
  - Story 6.8B: Validate Operational Telemetry Cardinality Gates.

Story 6.8 must not be assigned as ordinary single-owner implementation work without the checklist-mode evidence plan.
```

**Rationale:**

Telemetry redaction and telemetry cardinality are separate quality risks. They can share a story only when one owner explicitly accepts both evidence obligations.

### Proposal D: Add v1 Scope Protection to Story 2.4.3

**Story:** Story 2.4.3: Verify Operational, Export, Log, Trace, and Error Redaction Safety.

**Current text:**

```markdown
**Scope Note:** Future derived indexes remain ADR-gated unless promoted into active release scope.
```

**Proposed replacement:**

```markdown
**Scope Note:** v1 verification covers only operational and evidence surfaces active in v1. Future derived indexes, export workflows, and evidence-bundle behavior remain ADR-gated and out of implementation scope unless promoted into the active release scope by an approved ADR or sprint change proposal. Tests may assert that missing ADR coverage blocks implementation; they must not implement implicit index, export, or evidence-bundle semantics.
```

**Rationale:**

The current wording is defensible, but the revised wording blocks accidental implementation of v1.1 export or future indexing behavior.

### Proposal E: Create a Compact Readiness Gate Tracker

**Artifact:** `_bmad-output/implementation-artifacts/readiness-gates.md`.

**Proposed tracker columns:**

```markdown
| Gate | Affected stories | State | Owner | Approver | Decision link | Waiver expiry | Compensating control | Buyer impact | Review date |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

**Initial gates:**

- Temporal evidence anchor.
- Command availability metadata.
- Projection freshness blocking semantics.
- EventStore envelope ownership/evolution.
- Raw HTTP fallback approval.
- Numeric capacity/performance thresholds or buyer-accepted unknowns.
- Shared trust/freshness vocabulary.
- UX safety gate ownership.
- Story 3.8 split/owner-gate decision.
- Story 6.8 split/owner-gate decision.
- Story 2.4.3 v1 export/index scope protection.

**Rationale:**

The report recommends making blockers visible to sprint planning. A compact tracker gives the Developer, PO, Architect, TEA, and SRE a single state source.

## 6. MVP and Scope Impact

No MVP reduction is proposed.

The proposal protects the existing MVP by keeping v1 limited to the currently approved scope:

- v1 governance viewer remains read-only where the PRD requires it.
- Full Generate Evidence Bundle remains v1.1 unless explicitly promoted.
- Mobile governance-changing actions remain blocked by default unless explicitly designed, authorized, confirmed, and tested.
- Future derived indexes and exports remain ADR-gated.
- FrontComposer generated baseline surfaces remain acceptable only when custom trust-critical components cover evidence timeline, redaction, audit, citation, freshness, trust posture, and disclosure safety.

## 7. Implementation Handoff

### Scope Classification

Moderate.

The change requires backlog/story updates and gate tracking before sprint execution. It does not require PRD rewrite, architecture rewrite, or epic replacement.

### Handoff Recipients

**Product Owner / Developer**

- Add Ready for Dev precondition blocks to affected story records.
- Split Story 3.8 and Story 6.8 if ordinary implementation assignment is planned.
- Update sprint tracker/status after approval.

**Architect**

- Own or approve decisions for temporal evidence anchor, command availability metadata, projection freshness semantics, EventStore envelope ownership/evolution, and shared trust/freshness vocabulary.

**Product Manager / Buyer-facing owner**

- Confirm raw HTTP fallback policy.
- Confirm buyer-accepted unknowns for numeric capacity/performance thresholds where targets are not yet known.
- Preserve Option A v1/v1.1 scope language.

**TEA / Test Architect**

- Define evidence output and pass/fail gates for Story 3.8, redaction replay, accessibility, Leak Sentinel, and conformance manifest carry-forward.

**SRE / Operations owner**

- Own Story 6.8 telemetry redaction/cardinality evidence plan or split validation stories.
- Approve telemetry dimensions, redaction rules, fixtures, and pass/fail gates.

## 8. Success Criteria

This correction is complete when:

- Each affected story has explicit Ready for Dev preconditions or references a sprint tracker gate.
- `_bmad-output/implementation-artifacts/readiness-gates.md` exists and tracks the identified gates.
- Story 3.8 is either retained as an epic-level checklist with a named evidence owner or split into 3.8A/3.8B/3.8C.
- Story 6.8 is either retained with a named SRE/test owner and evidence plan or split into 6.8A/6.8B.
- Story 2.4.3 explicitly protects v1 scope around future derived indexes, exports, and evidence-bundle behavior.
- Sprint execution cannot pull blocked stories without a `decided` or properly documented `waived` gate.

## 9. Approval Request

Approved by Jerome on 2026-05-17.

Recommended approval wording:

```text
Approved: apply the Implementation Readiness Gates Sprint Change Proposal dated 2026-05-17.
```

## 10. Approval and Handoff Completion

**Approval:** Jerome approved this Sprint Change Proposal on 2026-05-17.

**Approval reconfirmed:** Jerome reconfirmed approval for the split-mode readiness correction on 2026-05-18.

**Applied artifact changes:**

- Updated `epics.md` with story-level Ready for Dev preconditions for the affected stories.
- Split Story 3.8 into Story 3.8A responsive/mobile safety, Story 3.8B accessibility safety, and Story 3.8C disclosure-surface safety so the work is assignment-safe by default.
- Split Story 6.8 into Story 6.8A telemetry redaction and Story 6.8B telemetry cardinality gates so the work is assignment-safe by default.
- Updated Story 2.4.3 scope wording to protect v1 from implicit future derived indexes, export workflows, and evidence-bundle behavior.
- Created `_bmad-output/implementation-artifacts/readiness-gates.md` as the active blocker tracker.
- Updated `_bmad-output/implementation-artifacts/sprint-status.yaml` to track the split Story 3.8A-3.8C and Story 6.8A-6.8B backlog entries.

**Sprint status:** `_bmad-output/implementation-artifacts/sprint-status.yaml` now tracks the split verification backlog entries. No existing story was moved out of `backlog`.

**Handoff route:** Moderate scope. Product Owner/Developer should use the readiness gate tracker during sprint planning; Architect, Product Manager, TEA, and SRE owners should fill pending gate rows before dependent stories are pulled.
