---
date: 2026-05-16
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-16.md
workflow: bmad-correct-course
status: approved
recommended_scope: moderate
recommended_path: direct-adjustment
mode: batch
approved_on: 2026-05-16
extends:
  - sprint-change-proposal-2026-05-16.md
---

# Sprint Change Proposal: Close Readiness Gates and Split Validation Controls

## 1. Issue Summary

The implementation readiness assessment completed on 2026-05-16 reports overall status `NEEDS WORK`.

This is not a functional coverage failure. The assessment found that all 104 PRD functional requirements are covered by the epics. The remaining issue is kickoff safety: the backlog is close enough to implement, but several preconditions still need explicit ownership, decision state, or assignment control before broad story execution begins.

The triggering concerns are:

- Pre-kickoff decisions and ADRs are still gating dependent implementation stories.
- Story 3.8 is too broad for ordinary single-owner implementation assignment.
- Story 6.8 is too broad unless one owner explicitly owns both telemetry redaction and cardinality validation.
- UX traceability uses `UX-DR` labels in `epics.md`, while the UX source document does not visibly preserve those identifiers.
- Story 1.1 is acceptable only if it remains scaffold support and does not smuggle domain behavior.
- Epic 5 and Epic 6 must preserve release-owner/operator value framing and not collapse into generic technical tasks.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains valid as the tenant-safe foundation epic. Story 1.1 can proceed first, but only as scaffold support. It must create project structure, ADR folders/templates, and decision-tracker links without deciding unresolved ADRs or implementing partial conversation behavior.

Epic 3 remains valid. Story 3.8 is useful as a verification bundle, but it crosses responsive layout, accessibility, disclosure-surface leakage, clipboard, browser-title, telemetry, and mobile read-only safety. That scope should be split before ordinary assignment or retained as an explicitly owned epic-level verification checklist.

Epic 5 remains valid as release evidence and compatibility governance. Its stories should remain framed around release-owner outcomes: signed evidence, manifest rows, waiver governance, portability proof, schema proof, and requirement traceability.

Epic 6 remains valid as operations and lifecycle governance. Story 6.8 should be split unless a single owner and evidence plan can responsibly cover both telemetry redaction and telemetry cardinality gates.

No epic needs to be removed or resequenced. No PRD MVP change is required.

### Story Impact

Affected stories and controls:

- Story 1.1: tighten scaffold-only scope and make ADR tracker creation explicitly non-decisional.
- Stories gated by pre-kickoff decisions: must link to readiness-gate records before implementation starts.
- Story 3.8: split into focused validation stories or keep as a named epic-level verification checklist.
- Story 6.8: split into redaction and cardinality validation stories unless single-owner coverage is explicitly approved.
- Epic 5 and Epic 6 implementation stories: preserve actor/value outcome statements during story-file generation.

### Artifact Impact

PRD: No requirement change is needed. The PRD already supports the current direction and FR coverage is complete.

Architecture: No architecture rewrite is needed. The architecture already names ADR-gated decisions for idempotency, tenant projection freshness, audit pairing, schema evolution, redaction replay, Party hydration, FrontComposer trust boundaries, retention/deletion lifecycle, trust/freshness vocabulary, and temporal evidence.

UX: Add or maintain a stable UX requirement map so the `UX-DR` labels used by epics remain reproducible after UX document edits.

Epics: Add explicit readiness-gate record requirements, split/assignment mechanics for Stories 3.8 and 6.8, and scaffold-scope guardrails for Story 1.1.

Implementation artifacts: Create a readiness gate tracker before broad story implementation begins.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Scope classification: Moderate backlog reorganization.

Rationale: The product direction and artifact set are strong. The remaining problems are controls around kickoff decisions, story size, and traceability stability. These can be fixed by adding explicit tracker artifacts and tightening story assignment rules without changing PRD scope or architecture direction.

Effort estimate: Low to medium planning effort before kickoff.

Risk if not corrected: High. Agents may start blocked stories without decisions, close scaffold work with hidden feature behavior, assign oversized validation bundles as ordinary stories, or lose UX traceability as documents evolve.

Residual risk after correction: Low to medium. The main residual risk is execution discipline: generated story files must preserve these controls.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | Trigger is readiness report `NEEDS WORK`; no single implementation story caused it. Story 1.1, 3.8, and 6.8 are specifically affected. |
| 1.2 Core problem | Done | Backlog readiness controls are not yet concrete enough for broad implementation kickoff. |
| 1.3 Evidence | Done | Evidence comes from `implementation-readiness-report-2026-05-16.md`, current `epics.md` readiness gates, and architecture ADR/open-question sections. |
| 2.1 Current epic impact | Done | Epic 1 can start with scaffold only; Epic 3 and 6 need validation assignment control. |
| 2.2 Epic-level changes | Done | Add readiness tracker and split/assignment controls; preserve Epic 5/6 actor-value framing. |
| 2.3 Remaining epics | Done | No other epic requires structural change. |
| 2.4 New/obsolete epics | N/A | No new or removed epic required. |
| 2.5 Order/priority | Done | Existing order can remain; blocked stories must wait on gate records. |
| 3.1 PRD conflicts | Done | No PRD conflict or MVP reduction. |
| 3.2 Architecture conflicts | Done | Architecture already supports the needed ADR gates and evidence controls. |
| 3.3 UX conflicts | Done | UX content aligns; traceability labels need stable mapping. |
| 3.4 Other artifacts | Done | Add readiness tracker and UX requirement map. Update sprint status only after proposal approval if story entries are changed. |
| 4.1 Direct adjustment | Viable | Preferred path. |
| 4.2 Rollback | Not viable | No implementation rollback is needed. |
| 4.3 MVP review | Not viable | MVP remains achievable. |
| 4.4 Path selected | Done | Direct adjustment with moderate backlog organization. |
| 5.1 Issue summary | Done | Included in this proposal. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Direct adjustment. |
| 5.4 MVP action plan | Done | MVP unchanged; add gates before broad kickoff. |
| 5.5 Handoff plan | Done | Included below. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Reconciled against readiness report, epics, architecture, PRD, and UX alignment notes. |
| 6.3 User approval | Action-needed | Approval required before applying backlog edits. |
| 6.4 sprint-status.yaml | Action-needed | Update only if approved changes add/remove/renumber story entries. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval. |

## 5. Detailed Change Proposals

### Proposal A: Add a Readiness Gate Tracker

Artifact: `_bmad-output/implementation-artifacts/readiness-gates.md`

Create a tracker with one row per gate:

```markdown
# Hexalith.Conversations Readiness Gates

| Gate | Blocks | Owner | State | Decision / Waiver Link | Expiry / Review Date | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| EventStore envelope stability and evolution ownership | Story 1.11, Story 5.9, event publication contracts | Architect | undecided | TBD | TBD | Decide stable inherited envelope vs Conversations-owned evolution. |
| .NET client versus raw HTTP fallback policy | Story 4.2, Story 4.5 | PM / Architect | undecided | TBD | TBD | Decide supported v1 integration path and fallback conditions. |
| v1 Conversations event consumers | Story 1.10, Story 5.8, contract tests | Architect / PO | undecided | TBD | TBD | Name consumers or record no v1 consumer dependency. |
| CORE status for MarkSensitiveData and RedactMessageContent | Stories 2.3, 2.4, 2.4.1-2.4.3, Story 5.7 | PM / PO | undecided | TBD | TBD | Decide v1 CORE, v1.1, conditional, or deferred status. |
| Two-level evidence semantics | Early implementation stories, Epic 5 | PO / Test Architect | decided | sprint-change-proposal-2026-05-16.md | N/A | Preserve local evidence closure plus Epic 5 release aggregation. |
| Architect and second-engineer availability | Trust/freshness, governance, UX safety stories | PO / Architect | undecided | TBD | TBD | Name reviewer availability before dependent stories start. |
| Second-adopter candidate or review milestone | Story 6.7, lifecycle commitments, downgrade review | PM / PO | undecided | TBD | TBD | Name candidate or milestone. |
| Temporal evidence anchor | Story 3.4, evidence links, temporal reconstruction | Architect | undecided | TBD | TBD | Decide event position, projection version, timestamp, or composite. |
| Projection freshness blocking semantics | Stories 1.7, 1.8, 3.1, 3.2, 4.2, 4.4, 6.2 | Architect / Test Architect | undecided | TBD | TBD | Decide block vs warn states. |
| Party hydration degraded states | Stories 1.3, 1.8, 3.2, export/read surfaces | Architect | undecided | TBD | TBD | Decide acceptable read degradation and write fail-closed rules. |
| Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle | Epic 2, Story 6.10, future indexes | PM / Architect | undecided | TBD | TBD | Decide active release behavior and explicit anti-scope. |
```

Rationale: The readiness report asks for tracked gates. A single artifact lets story creation and implementation agents check `decided`, `waived`, or `blocked` without inferring from prose.

### Proposal B: Tighten Epics Readiness Gate Wording

Artifact: `epics.md`

Section: `### Pre-Kickoff Decisions Required`

OLD:

```markdown
Dependent implementation stories must not begin until these decisions are recorded or explicitly waived:
```

NEW:

```markdown
Dependent implementation stories must not begin until each applicable decision has a row in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`. A waived gate must name owner, approver, expiry, compensating control, buyer impact, and review date.
```

Rationale: This turns the readiness gate from a prose instruction into a checkable artifact dependency.

### Proposal C: Keep Story 1.1 Scaffold-Only

Artifact: `epics.md`

Section: `### Story 1.1: Set Up Initial Project from Starter Template`

Add this scope-control note after `Requirements Covered`:

```markdown
**Scope Control:** Story 1.1 may create buildable projects, smoke tests, ADR folders/templates, and readiness tracker links only. It must not decide ADRs, implement conversation persistence, tenant authorization, provider integration, FrontComposer runtime behavior, projections, workers, governance commands, or partial domain behavior.
```

Rationale: This preserves the readiness report's finding that Story 1.1 is acceptable only as scaffold support.

### Proposal D: Split or Checklist Story 3.8 Before Assignment

Artifact: `epics.md`

Section: `### Story 3.8: Verify Responsive and Accessible Investigation Experience`

Replace the assignment rule with:

```markdown
**Assignment Rule:** Story 3.8 is an epic-level verification checklist by default. If assigned as ordinary implementation work, split before kickoff into:

- Story 3.8A: Verify Responsive Layout and Mobile Safe Triage.
- Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety.
- Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety.

Each split story must name owner, fixture set, evidence output, and pass/fail gate before implementation starts.
```

Rationale: This keeps the current safety intent but prevents one overloaded work item from hiding partial completion.

### Proposal E: Split Story 6.8 Unless Single-Owner Coverage Is Explicit

Artifact: `epics.md`

Section: `### Story 6.8: Validate Operational Telemetry Redaction and Cardinality`

Replace the assignment rule with:

```markdown
**Assignment Rule:** Story 6.8 is an epic-level validation checklist by default. It may remain one story only when a named owner and evidence plan cover both telemetry redaction and telemetry cardinality gates. Otherwise split before kickoff into:

- Story 6.8A: Validate Operational Telemetry Redaction.
- Story 6.8B: Validate Operational Telemetry Cardinality Gates.

Each split story must name owner, fixture set, approved dimensions or redaction rules, evidence output, and pass/fail gate before implementation starts.
```

Rationale: Redaction safety and cardinality control have different evidence needs, fixtures, and failure modes.

### Proposal F: Stabilize UX Traceability

Artifact: `_bmad-output/planning-artifacts/ux-requirement-map.md`

Create a durable map from UX source sections/acceptance criteria to the `UX-DR1` through `UX-DR52` labels used in `epics.md`.

Minimum format:

```markdown
# UX Requirement Map

| UX-DR | Source Section | Summary | Primary Epics / Stories | Notes |
| --- | --- | --- | --- | --- |
| UX-DR1 | Design system / generated baseline | FrontComposer and Fluent UI baseline design system | Stories 3.1-3.8 | Generated-first UI foundation. |
| UX-DR2 | Trust-critical customization | Custom Conversations UI only where trust demands it | Stories 3.1-3.8 | Evidence, redaction, freshness, citation, and command safety. |
```

Rationale: The readiness report says traceability is currently usable through `epics.md`, but future UX edits could drift because the source UX document does not visibly carry the same labels.

### Proposal G: Preserve Epic 5 and Epic 6 Actor/Value Framing

Artifact: story generation instructions or `epics.md` implementation notes.

Add this note near Epic 5 and Epic 6 story-generation guidance:

```markdown
**Story Generation Guardrail:** Epic 5 and Epic 6 stories must preserve release-owner, platform-owner, operator, SRE, or product-owner value framing. Do not rewrite them as generic technical tasks such as "write tests" or "add metrics"; generated story files must keep the actor, evidence outcome, decision consequence, and requirement traceability.
```

Rationale: The readiness report accepts Epic 5 and Epic 6 only with caution because their user value depends on release and operations actors.

## 6. Implementation Handoff

Change scope: Moderate.

Recommended recipients:

- Product Owner or Story Manager: Approve and apply the epics wording changes; create the readiness gate tracker.
- Architect: Own architecture decision rows for EventStore envelope, temporal anchor, projection freshness, Party hydration, and lifecycle decisions.
- Product Manager: Own v1/v1.1/deferred decisions for buyer-facing scope, second-adopter milestone, and CORE governance command status.
- Test Architect: Confirm that split validation stories have fixture sets, evidence outputs, and pass/fail gates.
- UX Designer or FrontComposer owner: Maintain the UX requirement map and generated-versus-custom trust-component boundary.
- Developer agent: Start only Story 1.1 before unresolved gates are decided, and keep it scaffold-only.

Success criteria:

1. `_bmad-output/implementation-artifacts/readiness-gates.md` exists and every pre-kickoff gate has owner, state, and decision or waiver link.
2. `epics.md` points dependent stories to the readiness gate tracker.
3. Story 1.1 explicitly remains scaffold-only.
4. Story 3.8 and Story 6.8 are split before ordinary assignment or retained as named verification checklists with owners.
5. `_bmad-output/planning-artifacts/ux-requirement-map.md` exists and maps `UX-DR` labels to UX source sections.
6. Epic 5 and Epic 6 generated stories preserve release-owner/operator value framing.
7. Implementation readiness can be rerun with these six concerns closed or reduced to accepted waivers.

## 7. Approval Status

Status: Approved.

Approved on 2026-05-16.

Post-approval action completed: `epics.md` was patched with readiness-gate tracker dependency wording, Story 1.1 scaffold-only scope control, Story 3.8 and Story 6.8 split/checklist assignment controls, and Epic 5/Epic 6 actor-value story-generation guardrails. `_bmad-output/implementation-artifacts/readiness-gates.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md` were created as binding implementation inputs.
