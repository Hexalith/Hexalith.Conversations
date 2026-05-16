---
date: 2026-05-15
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-15.md
workflow: bmad-correct-course
status: approved
recommended_scope: moderate
recommended_path: direct-adjustment
---

# Sprint Change Proposal: Readiness Corrections Before Implementation Kickoff

## 1. Issue Summary

The implementation readiness assessment completed on 2026-05-15 found the planning set substantially complete but not cleanly ready for sprint implementation.

The trigger is not missing PRD coverage. All required planning documents exist, all FR1-FR104 are mapped, and no critical blocker was found. The issue is that several stories and pre-kickoff decisions are not implementation-ready enough for safe handoff.

The readiness report identified six actionable issue groups:

1. Story 1.1 overclaims FR1-FR41 coverage.
2. Several conformance and verification stories are too broad.
3. Some stories mix primary implementation coverage with support or validation coverage.
4. Pre-kickoff decisions still need locking.
5. Shared trust/freshness vocabulary needs to be finalized.
6. UX safety gates need explicit implementation ownership.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains the correct foundation epic. Story 1.1 should remain a scaffold story, but its requirement coverage wording must stop counting FR1-FR41 as direct behavioral implementation coverage.

Epic 3 remains structurally valid. Story 3.8 should be treated as responsive, accessibility, and disclosure-surface verification support for Stories 3.1-3.7 rather than canonical implementation coverage for FR56-FR69.

Epic 5 needs backlog refinement. Story 5.5 combines tenant isolation conformance, idempotency conformance, and redaction replay conformance. Story 5.6 combines provider portability proof and event schema evolution proof. These are separate release-gate domains and should be independently closable before implementation assignment.

Epic 6 remains valid, but Story 6.8 mixes operational FR coverage with telemetry redaction and cardinality validation. It should distinguish observability feature implementation from validation/support coverage and should be split if ownership differs.

### Artifact Impact

PRD: No FR changes are required. MVP scope remains intact if the backlog is corrected before kickoff.

Architecture: Existing architecture already names the relevant decisions and constraints, including EventStore envelope stability, schema evolution, idempotency, tenant projection freshness, audit pairing, FrontComposer trust boundaries, and disclosure-surface controls. The issue is that the stories need clearer stop conditions and ownership for those decisions.

UX Design: The UX specification is aligned with the PRD and architecture, but safety gates such as Leak Sentinel, accessibility-tree leakage checks, clipboard safety, responsive duplicate checks, command reauthorization, and permission-safe DTO tests need explicit implementation ownership.

Implementation Artifacts: No Conversations implementation stories have been generated from the corrected backlog yet, so no rollback is required.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Scope classification: Moderate backlog reorganization.

Rationale: The product direction, PRD, epic sequence, architecture, and UX strategy are sound. The safest correction is to patch story traceability, split or sub-slice oversized validation stories, make decision prerequisites visible, and assign UX safety gates explicitly before implementation agents start closing stories.

Estimated effort: Medium planning effort before kickoff.

Residual risk after correction: Low to medium, mainly around whether the pre-kickoff decisions are ratified before dependent stories begin.

Timeline impact: Small pre-kickoff delay, likely reducing implementation churn and premature story closure.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | Readiness report identifies Story 1.1, Stories 5.5/5.6/6.8, Story 3.8, and open pre-kickoff decisions. |
| 1.2 Core problem | Done | Story quality and decision-readiness risk despite complete numeric FR mapping. |
| 1.3 Evidence | Done | Evidence captured in `implementation-readiness-report-2026-05-15.md`. |
| 2.1 Current epic impact | Done | Epic 1 needs traceability correction only. |
| 2.2 Epic-level changes | Done | Modify Epic 1 wording; split/sub-slice Epic 5 and possibly Epic 6 verification stories. |
| 2.3 Remaining epics | Done | Epic 3 and Epic 6 need support/validation coverage labeling. |
| 2.4 New/obsolete epics | N/A | No new epic or removed epic is required. |
| 2.5 Order/priority | Done | Lock decisions and safety-gate ownership before dependent implementation starts. |
| 3.1 PRD conflicts | Done | No PRD requirement change required. |
| 3.2 Architecture conflicts | Done | Decision prerequisites must be made explicit in story handoff. |
| 3.3 UX conflicts | Done | UX safety gates need owners/test assets. |
| 3.4 Other artifacts | Done | Traceability and story index updates required after backlog edits. |
| 4.1 Direct adjustment | Viable | Preferred path. |
| 4.2 Rollback | Not viable | No implemented Conversations stories require rollback. |
| 4.3 MVP review | Not viable | MVP remains achievable. |
| 4.4 Path selected | Done | Direct adjustment with moderate backlog reorganization. |
| 5.1 Issue summary | Done | Included in this proposal. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Direct adjustment. |
| 5.4 MVP action plan | Done | MVP unchanged; correct backlog before kickoff. |
| 5.5 Handoff plan | Done | PO/Story Manager, Architect, Developer, and Test Architect responsibilities listed below. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Reconciled to the current readiness report. |
| 6.3 User approval | Action-needed | Approval is pending before backlog edits. |
| 6.4 sprint-status.yaml | N/A | No sprint status artifact was found for Conversations. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval. |

## 5. Detailed Change Proposals

### Proposal A: Correct Story 1.1 FR Coverage

Story: 1.1 Set Up Initial Project from Starter Template

Section: Requirements Covered

OLD:

```markdown
**Requirements Covered:** FR1-FR41 foundation; Architecture starter-template requirement.
```

NEW:

```markdown
**Requirements Covered:** Architecture starter-template requirement; supports FR1-FR41 foundation only. Behavioral FR1-FR41 implementation coverage is delivered by Stories 1.2-1.11.
```

Rationale: Story 1.1 creates a buildable module scaffold and validates boundaries. It does not implement the behavioral requirements represented by FR1-FR41.

### Proposal B: Split or Sub-Slice Story 5.5

Story: 5.5 Verify Tenant Isolation, Idempotency, and Redaction Replay

Recommendation: Replace with three independently closable stories, or keep the story only if it contains separately assigned sub-slices with separate evidence outputs.

NEW story slices:

```markdown
### Story 5.5: Verify Tenant Isolation Conformance

**Requirements Covered:** FR87.

Acceptance focus: authorized access, cross-tenant ID guessing, stale/unavailable tenant projection, disabled or deleted tenants, mixed-tenant rebuild attempts, poisoned projection events, malformed metadata, query enumeration, diagnostics, export, and admin/tool access.
```

```markdown
### Story 5.6: Verify Idempotent Command Conformance

**Requirements Covered:** FR88.

Acceptance focus: duplicate equivalent commands, duplicate non-equivalent commands, reordered delivery, unknown client outcome retry, replayed delivery, tenant-mismatched key reuse, stable outcomes, conflict rejection, no duplicate business effects, and no projection divergence.
```

```markdown
### Story 5.7: Verify Redaction Replay Conformance

**Requirements Covered:** FR89.

Acceptance focus: projections, temporal views, logs, traces, errors, exports, accessibility output, clipboard payloads, caches, screenshots, telemetry, and derived indexes do not reintroduce redacted content while audit evidence remains citeable.
```

Rationale: Tenant isolation, idempotency, and redaction replay are separate release-blocking domains with distinct fixtures, failure modes, owners, and evidence.

### Proposal C: Split or Sub-Slice Story 5.6

Story: 5.6 Prove Provider Portability and Event Schema Evolution

Recommendation: Replace with two independently closable stories, or keep the story only if provider portability and schema evolution have separate assigned sub-slices and evidence outputs.

NEW story slices:

```markdown
### Story 5.8: Prove Provider Portability

**Requirements Covered:** FR90.

Acceptance focus: provider-owned correlation identifiers can be stripped, changed, unavailable, migrated, duplicated, or inconsistent without losing recoverability from Conversations identity, stable references, and EventStore history.
```

```markdown
### Story 5.9: Prove Event Schema Evolution

**Requirements Covered:** FR91.

Acceptance focus: old versions, mixed-version streams, unsupported versions, and at least one additive-change example produce documented compatibility behavior or typed unsupported-version failures.
```

Rationale: Provider portability and schema evolution are different proof obligations. Bundling them makes release-gate closure harder to review.

### Proposal D: Clarify Story 6.8 Support/Validation Coverage

Story: 6.8 Validate Operational Telemetry Redaction and Cardinality

Section: Requirements Covered

OLD:

```markdown
**Requirements Covered:** FR95-FR99; NFR55-NFR61.
```

NEW:

```markdown
**Requirements Covered:** FR95-FR99 validation support; NFR55-NFR61.
**Scope Note:** This story validates telemetry redaction and cardinality behavior across operational signals. Primary observability implementation remains in Stories 6.1-6.3 unless explicitly reassigned.
```

Optional split if ownership differs:

```markdown
### Story 6.8: Validate Operational Telemetry Redaction
### Story 6.9: Validate Operational Telemetry Cardinality Gates
```

Rationale: Story 6.8 is valuable, but it should not be treated as the canonical feature implementation story for all FR95-FR99.

### Proposal E: Clarify Story 3.8 Support/Validation Coverage

Story: 3.8 Verify Responsive and Accessible Investigation Experience

Section: Requirements Covered

OLD:

```markdown
**Requirements Covered:** FR56-FR69 support; UX-DR39-UX-DR52; NFR69-NFR77.
```

NEW:

```markdown
**Requirements Covered:** FR56-FR69 verification support; UX-DR39-UX-DR52; NFR69-NFR77.
**Scope Note:** This story verifies responsive, accessibility, and disclosure-surface safety for the investigation workspace. Primary feature implementation remains in Stories 3.1-3.7.
```

Rationale: Story 3.8 should verify that the workspace remains safe and accessible across devices and assistive technology. It should not become a catch-all implementation story for the full investigation workspace.

### Proposal F: Lock Pre-Kickoff Decisions

Add a short "Pre-Kickoff Decisions Required" note to the epic/story handoff or architecture decision tracker before sprint implementation starts:

```markdown
Pre-kickoff decisions required before dependent stories begin:

- EventStore envelope stability and evolution ownership.
- .NET client versus raw HTTP fallback policy.
- Whether any module consumes Conversations events in v1.
- Whether `MarkSensitiveData` and `RedactMessageContent` are CORE.
- Foundation Gate blocking semantics.
- Architect and second-engineer availability for trust/freshness and governance decisions.
- Named second-adopter candidate or review milestone.
```

Rationale: These decisions already appear in the readiness report and planning artifacts. Implementation agents need them as explicit stop conditions rather than implicit context.

### Proposal G: Finalize Shared Trust/Freshness Vocabulary

Add an implementation prerequisite to trust-bearing stories:

```markdown
**Decision Prerequisite:** Shared trust/freshness vocabulary must be finalized before implementation. API responses, admin UI, client errors, diagnostics, and conformance output must use the same approved states and metadata shape.
```

Recommended first affected areas:

- Story 1.7 projection freshness metadata.
- Story 1.8 retrieve/list freshness signaling.
- Story 3.1 trust-preview search.
- Story 3.2 governed read trust posture.
- Story 3.4 temporal evidence links and citation copy.
- Story 4.2 .NET client freshness/error semantics.
- Story 4.4 CORE preconditions.
- Story 6.2 projection lag/rebuild/availability observability.

Rationale: Architecture proposes candidate states, but the final contract shape needs to be locked before Admin UI, client, diagnostics, and conformance output diverge.

### Proposal H: Assign UX Safety Gate Ownership

Add explicit ownership to Story 3.8, Epic 5 conformance stories, or a test artifact checklist:

```markdown
UX safety gate ownership:

- Leak Sentinel helper.
- Accessibility-tree leakage checks.
- Clipboard safety checks.
- Responsive duplicate checks.
- Command reauthorization fixtures.
- Permission-safe DTO tests.
- FrontComposer generated-versus-custom component boundary checks.
```

Recommended acceptance criterion addition:

```markdown
**Given** Leak Sentinel and canonical disclosure fixtures are prepared
**When** desktop, tablet, mobile, screen-reader, clipboard, tooltip, browser-title, telemetry, loading, empty, denied, redacted, stale, and responsive-duplicate states are exercised
**Then** forbidden strings and structured forbidden values are absent from rendered DOM text, attributes, ARIA properties, page title, clipboard output, telemetry envelopes, screenshots, and accessibility snapshots where available
**And** the evidence is traceable from the conformance manifest or release evidence bundle.
```

Rationale: The UX spec already defines these safety gates. The backlog must identify who builds and verifies them.

## 6. Renumbering Guidance

If Story 5.5 is split into three stories and Story 5.6 is split into two stories:

- Current Story 5.5 becomes Stories 5.5, 5.6, and 5.7.
- Current Story 5.6 becomes Stories 5.8 and 5.9.
- Current Story 5.7 becomes Story 5.10.
- Current Story 5.8 becomes Story 5.11.

If Story 6.8 is split:

- Story 6.8 becomes telemetry redaction validation.
- New Story 6.9 becomes telemetry cardinality validation.

After renumbering, update cross-references, FR traceability tables, and any generated story indexes.

## 7. Implementation Handoff

Change scope: Moderate.

Recommended recipients:

- Product Owner or Story Manager: Apply backlog edits to `epics.md`, including Story 1.1 wording, story splits/sub-slices, support/validation labels, scope notes, renumbering, and traceability map updates.
- Architect: Ratify pre-kickoff decisions and the shared trust/freshness vocabulary before dependent implementation begins.
- Test Architect: Turn UX safety gates and split conformance domains into named test artifacts and release evidence entries.
- Developer agent: Generate and implement stories only after the corrected backlog and decision prerequisites are approved.

Success criteria:

1. Story 1.1 no longer claims behavioral FR1-FR41 coverage.
2. Story 5.5 is split or sub-sliced into tenant isolation, idempotency, and redaction replay conformance.
3. Story 5.6 is split or sub-sliced into provider portability and event schema evolution proof.
4. Story 6.8 marks FR95-FR99 as validation support, and splits telemetry redaction/cardinality if ownership differs.
5. Story 3.8 marks FR56-FR69 as verification support and clearly points primary implementation to Stories 3.1-3.7.
6. Pre-kickoff decisions are recorded as stop conditions for dependent stories.
7. Shared trust/freshness vocabulary is finalized before API, Admin UI, client, diagnostics, or conformance implementation diverges.
8. UX safety gates have explicit implementation and verification ownership.
9. Readiness can be rerun with the six issue groups closed or explicitly waived.

## 8. Approval Status

Approved on 2026-05-15.

Approved path: Direct adjustment.

Post-approval action completed: `_bmad-output/planning-artifacts/epics.md` was patched according to the approved proposals. Rerun implementation readiness or at least the epic coverage and epic quality sections before sprint kickoff.
