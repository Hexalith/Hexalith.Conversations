---
date: 2026-05-15
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-15.md
workflow: bmad-correct-course
status: approved
recommended_scope: moderate
recommended_path: direct-adjustment
mode: batch
approved_on: 2026-05-15
supersedes_or_extends:
  - sprint-change-proposal-2026-05-15.md
---

# Sprint Change Proposal: Foundation Gate and Story Split Corrections

## 1. Issue Summary

The implementation readiness assessment completed on 2026-05-15 found the planning artifacts strong but still not ready for implementation kickoff.

The trigger is execution packaging, not product incoherence:

- All required planning documents exist.
- PRD FR1-FR104 have explicit epic/story coverage.
- UX aligns with PRD and architecture.
- The remaining blockers are Foundation Gate sequencing and story sizing.

Proceeding as-is would allow earlier CORE stories to appear complete before the release-gating proof obligations are in place. It would also hand implementation agents several stories that bundle too many independently testable surfaces into one increment.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains the correct foundation epic, but Story 1.4 is too broad. It currently combines participant addition, message append, file references, upstream business references, lifecycle rejection behavior, replay, and multi-provider attribution. This should be split into smaller increments.

Epic 2 remains the correct governance epic, but Story 2.4 is too broad. It currently combines redaction command behavior with projection/read-model/search/evidence/cache/export/accessibility/clipboard/log/trace/error behavior. This should be split into redaction implementation and redaction disclosure/verification slices.

Epic 3 is structurally valid after the prior proposal clarified Story 3.8 as verification support. No new epic is required, but Story 3.8 should stay as a validation bundle only if implementation agents are not expected to deliver it as one ordinary feature story.

Epic 5 is structurally valid after the prior proposal split tenant isolation, idempotency, redaction replay, provider portability, and schema evolution into separate conformance stories. The remaining problem is sequencing: those release-gating stories must block relevant CORE story closure even if they stay in Epic 5.

Epic 6 is structurally valid after the prior proposal clarified Story 6.8 as validation support. If one owner cannot complete telemetry redaction and cardinality together, split Story 6.8 into two validation stories.

### Artifact Impact

PRD: No FR changes are required. The PRD already names the relevant Foundation Gates and pre-kickoff blockers.

Architecture: No architecture rewrite is required, but story stop conditions must explicitly reference ADR-gated choices: temporal evidence anchor, command availability metadata, projection freshness blocking semantics, EventStore envelope ownership, raw HTTP fallback, and numeric capacity/performance thresholds.

UX: No UX rewrite is required. Story 3.8 and related conformance stories must retain explicit ownership for disclosure-surface safety.

Epics: `epics.md` requires backlog edits after approval:

- Add explicit Foundation Gate closure rules.
- Split Story 1.4 into smaller implementation stories.
- Split Story 2.4 into smaller implementation and verification stories.
- Keep Story 3.8 and Story 6.8 as verification/support unless separately split.
- Preserve the existing FR traceability baseline.

Implementation artifacts: No generated Conversations implementation story files need rollback because implementation has not started from this corrected backlog.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Scope classification: Moderate backlog reorganization.

Rationale: The product direction, PRD, UX, architecture, and epic sequence are sound. The fix is to tighten execution packaging before sprint work begins.

Effort estimate: Medium planning effort before kickoff.

Risk if not corrected: High. Agents may close CORE stories without Foundation Gate proof, and large stories may create false progress or unreviewable changes.

Residual risk after correction: Low to medium. Remaining risk depends on ratifying the ADR-gated decisions before dependent stories begin.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | Trigger is readiness report issue set: Foundation Gate sequencing plus Story 1.4, 2.4, 3.8, and 6.8 sizing. |
| 1.2 Core problem | Done | Story packaging and closure semantics do not yet enforce PRD Foundation Gates. |
| 1.3 Evidence | Done | Evidence captured in `implementation-readiness-report-2026-05-15.md`, especially major issues 1-5. |
| 2.1 Current epic impact | Done | Epic 1 still valid; Story 1.4 needs split. |
| 2.2 Epic-level changes | Done | Modify Epic 1, Epic 2, and readiness-gate wording; keep Epic 5 conformance stories as gates. |
| 2.3 Remaining epics | Done | Epic 3 and Epic 6 stay verification-support focused unless ownership requires split. |
| 2.4 New/obsolete epics | N/A | No new or removed epic is required. |
| 2.5 Order/priority | Action-needed | Approval needed to enforce Foundation Gate blocking before CORE story closure. |
| 3.1 PRD conflicts | Done | No PRD conflict; the epics need to obey existing PRD gate semantics. |
| 3.2 Architecture conflicts | Done | ADR-gated decisions need explicit story stop conditions. |
| 3.3 UX conflicts | Done | UX safety gates remain valid; implementation ownership must stay explicit. |
| 3.4 Other artifacts | Done | Traceability tables and future story files need update after backlog edits. |
| 4.1 Direct adjustment | Viable | Preferred path. |
| 4.2 Rollback | Not viable | No completed Conversations implementation needs rollback. |
| 4.3 MVP review | Not viable | MVP remains achievable if backlog is corrected. |
| 4.4 Path selected | Done | Direct adjustment with moderate backlog reorganization. |
| 5.1 Issue summary | Done | Included in this proposal. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Direct adjustment. |
| 5.4 MVP action plan | Done | MVP unchanged; correct gate sequencing and story size first. |
| 5.5 Handoff plan | Done | Included below. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Reconciled to the current readiness report. |
| 6.3 User approval | Action-needed | Pending explicit approval before editing `epics.md`. |
| 6.4 sprint-status.yaml | N/A | No Conversations sprint status artifact was found. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval. |

## 5. Detailed Change Proposals

### Proposal A: Add Foundation Gate Closure Rules

Artifact: `epics.md`

Section: Implementation Readiness Gates

OLD:

```markdown
- Foundation Gate blocking semantics.
```

NEW:

```markdown
- Foundation Gate blocking semantics are approved and enforced as story closure rules.
```

Add this section under Implementation Readiness Gates:

```markdown
### Foundation Gate Closure Rules

CORE implementation stories may start only when their direct ADR prerequisites are recorded or explicitly waived. They may not close until the matching Foundation Gate evidence is passing in CI or has an approved named waiver.

Minimum closure dependencies:

- Story 1.5 cannot close until Story 5.5 tenant isolation conformance is passing or explicitly waived.
- Story 1.6 cannot close until Story 5.6 idempotent command conformance is passing or explicitly waived.
- Story 1.11 cannot close until Story 5.9 event schema evolution proof is passing or explicitly waived.
- Story 2.4 and its split redaction stories cannot close until Story 5.7 redaction replay conformance is passing or explicitly waived.
- Story 2.5 cannot close until audit-write fail-closed behavior is verified by the governance/audit gate or explicitly waived.
- Story 4.5 cannot close until the adopter conformance pack and CORE adopter fixture are passing or explicitly waived.

Any waiver must name owner, approver, expiry, compensating control, buyer impact, and review date.
```

Rationale: This fixes the sequencing conflict without forcing all release-evidence work to move physically into Epic 1 or Epic 2.

### Proposal B: Split Story 1.4

Story: 1.4 Append Messages and Participants with Attribution Metadata

Section: Story body

OLD:

```markdown
### Story 1.4: Append Messages and Participants with Attribution Metadata

As an adopter system,
I want to add participants and append ordered messages to an existing conversation,
So that the conversation record can preserve who contributed what, when, and under which tenant context.

**Requirements Covered:** FR4, FR5, FR13-FR22.
```

NEW:

```markdown
### Story 1.4: Add Conversation Participants with Stable Party Attribution

As an adopter system,
I want to add human users, AI agents, and LLMs as conversation participants,
So that participant membership is attributable through stable Party identities without storing Party personal data.

**Requirements Covered:** FR5, FR13-FR18.

### Story 1.4.1: Append Ordered Messages with Author Attribution

As an adopter system,
I want to append ordered messages to an existing active conversation,
So that the conversation record preserves who contributed what, when, and under which tenant context.

**Requirements Covered:** FR4, FR6, FR7, FR13-FR18.

### Story 1.4.2: Attach File and Upstream Business References

As an adopter system,
I want to associate messages and conversations with file, project, folder, provider, and external business references,
So that downstream discovery and governance can use stable references without storing upstream records or file binaries.

**Requirements Covered:** FR15-FR22.
```

Rationale: Participant membership, message append, and reference attachment are independently testable command surfaces. Splitting them reduces implementation and review risk while preserving FR coverage.

### Proposal C: Split Story 2.4

Story: 2.4 Redact Message Content with Audit Attribution

Section: Story body

OLD:

```markdown
### Story 2.4: Redact Message Content with Audit Attribution

As an authorized governance operator,
I want to redact message content with actor, rationale, and policy attribution,
So that protected content is removed from display and derived surfaces while auditability remains intact.

**Requirements Covered:** FR44-FR47, FR51.
```

NEW:

```markdown
### Story 2.4: Redact Message Content with Audit Attribution

As an authorized governance operator,
I want to record redaction intent as an audited domain event,
So that protected content can be removed from governed surfaces while auditability remains intact.

**Requirements Covered:** FR44-FR47, FR51.

### Story 2.4.1: Apply Redaction to Projections and Read Models

As a compliance operator,
I want projections, read models, temporal views, and search materializations to apply redaction state consistently,
So that protected content does not reappear during normal reads, rebuilds, or point-in-time reconstruction.

**Requirements Covered:** FR44-FR46, FR50, FR58-FR61.

### Story 2.4.2: Verify UI, Accessibility, Clipboard, and Citation Redaction Safety

As a compliance operator using visual, keyboard, screen-reader, and clipboard workflows,
I want redacted content to stay absent from every client-observable surface,
So that investigation workflows remain safe across DOM, ARIA, tooltips, titles, screenshots, citation copy, and responsive duplicates.

**Requirements Covered:** FR44-FR46, FR59, FR62, FR63; NFR21, NFR69-NFR77.

### Story 2.4.3: Verify Operational, Export, Log, Trace, and Error Redaction Safety

As an SRE or release owner,
I want redaction safety verified across exports, logs, traces, errors, diagnostics, caches, and future derived indexes,
So that operational and release evidence cannot leak protected content.

**Requirements Covered:** FR44-FR47, FR89 validation support; NFR19, NFR21, NFR55-NFR62.

**Scope Note:** Future derived indexes remain ADR-gated unless promoted into active release scope.
```

Rationale: Redaction command/event behavior, projection behavior, client disclosure safety, and operational disclosure safety are separate implementation and verification concerns.

### Proposal D: Keep Story 3.8 as Verification Support or Split Before Assignment

Story: 3.8 Verify Responsive and Accessible Investigation Experience

Current correction from the earlier proposal is acceptable:

```markdown
**Requirements Covered:** FR56-FR69 verification support; UX-DR39-UX-DR52; NFR69-NFR77.

**Scope Note:** This story verifies responsive, accessibility, and disclosure-surface safety for the investigation workspace. Primary feature implementation remains in Stories 3.1-3.7.
```

Additional assignment rule:

```markdown
Story 3.8 must not be assigned as a single ordinary feature implementation story. Either assign it as an epic-level verification checklist or split it into responsive layout safety, accessibility tree and keyboard flow safety, and leakage/clipboard/browser/telemetry safety stories.
```

Rationale: The readiness report correctly identifies Story 3.8 as a large validation bundle. It is acceptable only if treated as verification orchestration, not as one implementation slice.

### Proposal E: Keep Story 6.8 as Verification Support or Split Before Assignment

Story: 6.8 Validate Operational Telemetry Redaction and Cardinality

Current correction from the earlier proposal is acceptable:

```markdown
**Requirements Covered:** FR95-FR99 validation support; NFR55-NFR61.

**Scope Note:** This story validates telemetry redaction and cardinality behavior across operational signals. Primary observability implementation remains in Stories 6.1-6.3 unless explicitly reassigned.
```

Additional assignment rule:

```markdown
Story 6.8 must not be assigned as a single ordinary feature implementation story unless one owner can complete both validation domains. Otherwise split it into:

- Story 6.8: Validate Operational Telemetry Redaction
- Story 6.9: Validate Operational Telemetry Cardinality Gates
```

Rationale: Telemetry redaction and cardinality gates have different fixtures, failure modes, and likely review responsibilities.

### Proposal F: Add ADR-Gated Stop Conditions to Dependent Stories

Artifact: `epics.md`

Add this note under Implementation Readiness Gates:

```markdown
### ADR-Gated Story Stop Conditions

Dependent stories must stop before implementation when these decisions are missing:

- Temporal evidence anchor: blocks Story 3.4 and point-in-time evidence link behavior.
- Command availability metadata: blocks UI command gates and any client-side command eligibility rendering.
- Projection freshness blocking semantics: blocks Stories 1.7, 1.8, 3.1, 3.2, 4.2, 4.4, and 6.2.
- EventStore envelope ownership and evolution: blocks Story 1.11 and Story 5.9.
- Raw HTTP fallback approval: blocks raw HTTP fallback scope in Story 4.2.
- Numeric capacity/performance thresholds or buyer-accepted unknowns: blocks GA release-gate closure and performance evidence stories.
```

Rationale: The readiness report flags these as open choices that should not become silent implementation assumptions.

## 6. Renumbering Guidance

Preferred low-churn approach:

- Keep existing Story 1.5-1.11 IDs unchanged.
- Insert Story 1.4.1 and Story 1.4.2 after Story 1.4.
- Keep existing Story 2.5-2.8 IDs unchanged.
- Insert Story 2.4.1, Story 2.4.2, and Story 2.4.3 after Story 2.4.

If the team requires strictly sequential one-decimal story IDs, perform a separate renumbering pass and update all FR coverage references, cross-references, story filenames, and future sprint-status entries in one controlled edit.

## 7. Implementation Handoff

Change scope: Moderate.

Recommended recipients:

- Product Owner or Story Manager: Apply approved backlog edits to `epics.md`, including gate rules, Story 1.4 split, Story 2.4 split, assignment rules for Story 3.8 and Story 6.8, and traceability updates.
- Architect: Ratify ADR-gated stop conditions and confirm Foundation Gate blocking semantics.
- Test Architect: Map gate dependencies to concrete conformance evidence and CI lanes.
- Developer agent: Generate implementation story files only after the corrected backlog and prerequisite decisions are approved.

Success criteria:

1. CORE stories have explicit Foundation Gate closure dependencies.
2. Story 1.4 is no longer one combined participant/message/reference story.
3. Story 2.4 is no longer one combined command/projection/UI/ops redaction story.
4. Story 3.8 and Story 6.8 are verification support or split before assignment.
5. ADR-gated choices are explicit stop conditions.
6. FR traceability remains complete for FR1-FR104.
7. Readiness can be rerun with Foundation Gate sequencing and story-sizing findings closed or explicitly waived.

## 8. Approval Status

Approved on 2026-05-15.

Approved path: Direct adjustment.

Post-approval action completed: `_bmad-output/planning-artifacts/epics.md` was patched according to the approved proposals. Rerun implementation readiness or at least the epic quality review before sprint kickoff.
