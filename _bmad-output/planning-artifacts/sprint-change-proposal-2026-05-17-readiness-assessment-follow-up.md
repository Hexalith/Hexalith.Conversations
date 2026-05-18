---
date: 2026-05-17
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-17.md
workflow: bmad-correct-course
status: approved
recommended_scope: minor-to-moderate
recommended_path: direct-adjustment
mode: batch
extends:
  - sprint-change-proposal-2026-05-16-readiness-gates-and-story-controls.md
---

# Sprint Change Proposal: May 17 Implementation Readiness Follow-Up

## 1. Issue Summary

The May 17 implementation readiness assessment reports overall status `NEEDS WORK`.

This is not a PRD, architecture, UX, or epic-coverage failure. Required planning documents exist; PRD functional requirements are covered 104/104; all 52 UX-DR labels are mapped into epics; no critical structural violations were found; and the epic/story set is actor-framed and traceable.

The remaining issue is implementation-control readiness. Known blockers and broad verification bundles must be made enforceable before teams pull dependent stories into sprint execution.

The report identified 11 attention items:

- 5 UX warnings.
- 3 major story/readiness issues.
- 3 minor quality concerns.

Primary action: turn known blockers into explicit story preconditions, and split or owner-gate Story 3.8 and Story 6.8 before implementation kickoff.

## 2. Impact Analysis

### Epic Impact

No epic needs removal, replacement, or resequencing.

Epic 1 remains the safe starting point, but stories blocked by projection freshness, EventStore envelope ownership/evolution, or other open gates must not begin until those gates are `decided` or `waived`.

Epic 2 remains valid, with a v1 scope watch on Story 2.4.3 so future derived indexes and exports are not accidentally promoted without ADR or release-scope approval.

Epic 3 remains valid, but Story 3.8 must stay an epic-level verification checklist unless a named evidence owner exists, or it must split into 3.8A, 3.8B, and 3.8C before ordinary implementation assignment.

Epic 4 remains valid, with raw HTTP fallback scope blocked until buyer approval or equivalent policy decision is recorded.

Epic 5 remains valid as release-gate aggregation, with EventStore envelope evolution, provider portability, schema evolution, numeric thresholds, and waiver discipline still requiring explicit gate handling.

Epic 6 remains valid, but Story 6.8 must either have one named owner and evidence plan covering telemetry redaction plus cardinality, or split into 6.8A and 6.8B.

### Story Impact

- Story 3.4 remains blocked for point-in-time evidence links until the temporal evidence anchor is decided.
- UI command-gate work remains blocked until command availability metadata is decided.
- Stories 1.7, 1.8, 3.1, 3.2, 4.2, 4.4, and 6.2 remain blocked where projection freshness blocking semantics are required.
- Stories 1.11 and 5.9 remain blocked where EventStore envelope ownership/evolution is required.
- Story 4.2 raw HTTP fallback scope remains blocked until buyer-approved fallback policy is decided.
- GA release-gate closure and performance evidence stories remain blocked until numeric capacity/performance thresholds are defined or buyer-accepted unknowns are recorded.
- Story 3.8 and Story 6.8 remain assignment-gated.
- Story 2.4.3 needs v1 scope discipline around exports, future indexes, logs, traces, errors, and operational evidence surfaces.

### Artifact Impact

PRD: No rewrite required. Existing scope boundaries already defer Generate Evidence Bundle to v1.1, constrain mobile governance mutation, and require numeric target discipline.

Architecture: No rewrite required. The open ADR and decision backlog remains binding implementation input.

UX: No rewrite required. Existing UX warnings should be treated as implementation gates: temporal evidence anchor, Generate Evidence Bundle deferral, mobile mutation default-block, FrontComposer trust-component boundary, and Story 3.8 verification ownership.

Epics: No structural rewrite required. The current epics already include stop conditions and assignment rules for Story 3.8 and Story 6.8. Sprint planning must enforce them.

Implementation artifacts: `_bmad-output/implementation-artifacts/readiness-gates.md` needed updates so every May 17 blocker appears as a trackable gate. These tracker updates have been applied in this proposal pass.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Scope classification: Minor-to-moderate backlog control correction.

Rationale: The artifact set is mature and coherent. The right move is not a PRD rewrite or epic replan. It is to enforce the already-known gates at story-entry time and prevent oversized verification bundles from closing without named ownership and evidence.

Effort estimate: Low planning effort; medium enforcement importance.

Risk if not corrected: Medium to high. Stories could be pulled before blocking decisions exist, and broad verification stories could be treated as ordinary implementation work without the evidence needed for release confidence.

Residual risk after correction: Low to medium, depending on whether sprint planning actively checks gate state before assignment.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | No single trigger story; trigger is the May 17 readiness report status `NEEDS WORK`. |
| 1.2 Core problem | Done | Implementation-readiness gates are known but must be enforced before story execution. |
| 1.3 Evidence | Done | `implementation-readiness-report-2026-05-17.md`, `epics.md`, `ux-design-specification.md`, `ux-requirement-map.md`, `architecture.md`, and `readiness-gates.md`. |
| 2.1 Current epic impact | Done | Epics remain valid; dependent stories need gate enforcement. |
| 2.2 Epic-level changes | Done | No epic scope changes; enforce readiness and assignment controls. |
| 2.3 Remaining epics | Done | Epic 3 and Epic 6 are the main assignment-gate watch areas. |
| 2.4 New/obsolete epics | N/A | No new or obsolete epic. |
| 2.5 Order/priority | Done | Existing order remains valid; blocked stories wait on gates. |
| 3.1 PRD conflicts | Done | No PRD conflict. |
| 3.2 Architecture conflicts | Done | Open architecture decisions remain readiness gates. |
| 3.3 UX conflicts | Done | No UX mismatch; UX warnings become gate enforcement concerns. |
| 3.4 Other artifacts | Done | Updated readiness gate tracker. |
| 4.1 Direct adjustment | Viable | Recommended. |
| 4.2 Rollback | Not viable | No implementation rollback is needed. |
| 4.3 MVP review | Not viable | MVP remains achievable under gate control. |
| 4.4 Path selected | Done | Direct adjustment. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Included above. |
| 5.4 MVP action plan | Done | MVP unchanged; add and enforce readiness gates. |
| 5.5 Handoff plan | Done | Included below. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Reconciled against the May 17 report. |
| 6.3 User approval | Action-needed | Approval required before additional backlog text edits beyond tracker alignment. |
| 6.4 sprint-status.yaml | N/A | No story IDs are added, removed, or renumbered by this proposal. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval. |

## 5. Detailed Change Proposals

### Proposal A: Add Missing Readiness Gate Rows

Artifact: `_bmad-output/implementation-artifacts/readiness-gates.md`

Status: Applied.

OLD:

```markdown
No explicit rows existed for command availability metadata, numeric capacity/performance thresholds, Story 3.8 assignment plan, or Story 6.8 assignment plan.
```

NEW:

```markdown
| Command availability metadata | UI command gates, client-side command eligibility rendering, mobile governance-action blocking | Architect / UX / PO | undecided | TBD | TBD | Decide server-owned metadata shape for availability, required permission, precondition, risk level, and blocked reason. |
| Numeric capacity and performance thresholds | GA release-gate closure, performance evidence stories, NFR9-NFR17, NFR30, NFR37 | PM / Architect / Test Architect | undecided | TBD | TBD | Define numeric thresholds or buyer-accepted unknowns with owner, measurement method, environment, and review date. |
| Story 3.8 assignment plan | Story 3.8 or split stories 3.8A-3.8C | PO / Test Architect / UX | undecided | TBD | TBD | Keep as epic-level verification checklist with named evidence owner, or split into responsive/mobile, accessibility, and leakage/disclosure safety stories before ordinary assignment. |
| Story 6.8 assignment plan | Story 6.8 or split stories 6.8A-6.8B | PO / Test Architect / SRE | undecided | TBD | TBD | Keep as one story only with named owner and evidence plan for telemetry redaction and cardinality; otherwise split before ordinary assignment. |
```

Rationale: These are explicit May 17 blockers and must be visible in the tracker sprint planners use.

### Proposal B: Correct Raw HTTP Fallback Gate Scope

Artifact: `_bmad-output/implementation-artifacts/readiness-gates.md`

Status: Applied.

OLD:

```markdown
| .NET client versus raw HTTP fallback policy | Story 4.2, Story 4.5 | PM / Architect | undecided | TBD | TBD | Decide supported v1 integration path and fallback conditions. |
```

NEW:

```markdown
| .NET client versus raw HTTP fallback policy | Story 4.2 raw HTTP fallback scope, Story 4.5 | PM / Architect | undecided | TBD | TBD | Decide supported v1 integration path and buyer-approved raw HTTP fallback conditions. .NET client remains the supported v1 path unless approved otherwise. |
```

Rationale: The readiness report names raw HTTP fallback approval as a story-entry blocker. The gate now points to the blocked scope explicitly.

### Proposal C: Fix Stale Story 6.10 Reference

Artifact: `_bmad-output/implementation-artifacts/readiness-gates.md`

Status: Applied.

OLD:

```markdown
| Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle | Epic 2, Story 6.10, future indexes | PM / Architect | undecided | TBD | TBD | Decide active release behavior and explicit anti-scope. |
```

NEW:

```markdown
| Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle | Epic 2 governance stories, Story 2.4.3, Story 2.7, Story 6.4, future indexes/export surfaces | PM / Architect | undecided | TBD | TBD | Decide active release behavior, explicit anti-scope, legal-hold boundary, export handling, and derived-index treatment. |
```

Rationale: `Story 6.10` does not exist. The corrected gate points to actual governance, redaction/export safety, audit policy, release-scope, and future-surface controls.

### Proposal D: Enforce Story 3.8 Assignment Rule

Artifact: `_bmad-output/planning-artifacts/epics.md`

Status: Already present; enforce during sprint planning.

Current rule:

```markdown
Story 3.8 is an epic-level verification checklist by default. If assigned as ordinary implementation work, split before kickoff into:

- Story 3.8A: Verify Responsive Layout and Mobile Safe Triage.
- Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety.
- Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety.
```

Rationale: The rule is sufficient in the epics document; the new tracker row makes it visible as a pre-kickoff planning gate.

### Proposal E: Enforce Story 6.8 Assignment Rule

Artifact: `_bmad-output/planning-artifacts/epics.md`

Status: Already present; enforce during sprint planning.

Current rule:

```markdown
Story 6.8 is an epic-level validation checklist by default. It may remain one story only when a named owner and evidence plan cover both telemetry redaction and telemetry cardinality gates. Otherwise split before kickoff into:

- Story 6.8A: Validate Operational Telemetry Redaction.
- Story 6.8B: Validate Operational Telemetry Cardinality Gates.
```

Rationale: The rule is sufficient in the epics document; the new tracker row makes it visible as a pre-kickoff planning gate.

### Proposal F: Keep Story 2.4.3 V1 Scope Boundaries Explicit

Artifact: `_bmad-output/planning-artifacts/epics.md`

Status: Already present; no edit required unless the team wants stronger wording.

Current scope note:

```markdown
Future derived indexes remain ADR-gated unless promoted into active release scope.
```

Current acceptance criteria also state that missing ADR coverage blocks implicit indexing or export semantics.

Rationale: This satisfies the May 17 minor concern. Sprint planning should keep the scope note visible when creating the story file.

## 6. Implementation Handoff

Change scope: Minor-to-moderate.

Recommended recipients:

- Product Owner or Story Manager: Approve the proposal and enforce gate checks before assigning dependent stories.
- Architect: Own temporal evidence anchor, command availability metadata, projection freshness semantics, EventStore envelope ownership/evolution, and relevant ADR decisions.
- Product Manager: Own buyer-facing raw HTTP fallback approval, release-scope decisions, second-adopter milestone, and buyer-accepted unknowns.
- Test Architect: Own split/evidence planning for Story 3.8, Story 6.8, numeric threshold evidence, and release-gate closure discipline.
- UX: Own responsive/accessibility/disclosure-surface fixture expectations for Story 3.8.
- SRE: Own telemetry redaction/cardinality evidence if Story 6.8 remains a single story.
- Developer agent: Start only non-blocked work and check `readiness-gates.md` before implementing dependent stories.

Success criteria:

1. Every May 17 decision blocker has a row in `readiness-gates.md`.
2. Dependent stories are pulled only when their applicable gate is `decided` or `waived`.
3. Story 3.8 is either retained as an epic-level verification checklist with named evidence owner or split into 3.8A-3.8C.
4. Story 6.8 is either retained with one named owner and full evidence plan or split into 6.8A-6.8B.
5. Story 2.4.3 does not promote future derived indexes or exports without active release scope and ADR coverage.
6. A rerun of implementation readiness should no longer report missing explicit precondition gates for the May 17 blockers.

## 7. Approval Status

Status: Approved by Jerome on 2026-05-17.

The proposal is approved for implementation handoff. Gate owners must still decide or waive their individual readiness gates before dependent stories begin.

## 8. Workflow Completion

Issue addressed: May 17 implementation readiness assessment status `NEEDS WORK`.

Change scope: Minor-to-moderate backlog control correction.

Artifacts modified:

- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-17-readiness-assessment-follow-up.md`

Routed to:

- Product Owner or Story Manager for story-entry gate enforcement.
- Architect for technical readiness decisions.
- Product Manager for buyer-facing scope and accepted-unknown decisions.
- Test Architect, UX, and SRE for Story 3.8 and Story 6.8 evidence ownership or split planning.
- Developer agent for non-blocked implementation only.

Next success criteria:

1. Decide or waive each applicable readiness gate before pulling dependent stories.
2. Keep Story 3.8 as an owned verification checklist or split it into 3.8A-3.8C before ordinary assignment.
3. Keep Story 6.8 as an owned validation checklist or split it into 6.8A-6.8B before ordinary assignment.
4. Rerun implementation readiness after gate ownership and assignment plans are recorded.
