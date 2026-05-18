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
supersedes_or_extends:
  - sprint-change-proposal-2026-05-15-readiness-follow-up.md
---

# Sprint Change Proposal: Remove Forward Closure Dependencies Before Kickoff

## 1. Issue Summary

The implementation readiness assessment completed on 2026-05-16 found the planning set substantially complete but still not safe for implementation kickoff.

The trigger is a sequencing defect introduced by the current backlog mechanics:

- PRD, architecture, epics, and UX artifacts all exist.
- PRD extraction found 104 FRs and 77 NFRs.
- Epic coverage remains complete at 104/104 FRs.
- UX and architecture remain aligned.
- The blocker is that early implementation stories currently cannot close until later Epic 5 conformance stories pass or receive waivers.

This makes Epic 1 and Epic 2 dependent on a future epic for closure, even though Epic 5 is sequenced later as release evidence and aggregation work. That violates the no-forward-dependency rule for implementation epics and can stall execution or create false closure pressure.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains the right tenant-safe foundation epic. Stories 1.5, 1.6, and 1.11 already include strong local test expectations for tenant isolation, idempotency, replay, rebuild, and version-aware behavior. They should not depend on Stories 5.5, 5.6, or 5.9 to close.

Epic 2 remains the right governance epic. Stories 2.4, 2.4.1, 2.4.2, and 2.4.3 already split redaction command behavior, projection behavior, client-visible safety, and operational safety. They should close on local minimum redaction evidence, while Story 5.7 later aggregates release-gate redaction replay evidence.

Epic 4 remains valid. Story 4.5 should close when the adopter-facing conformance package and CORE fixture exist and pass locally. Later release manifest signing should stay in Epic 5.

Epic 5 remains valid, but its role must be narrowed: it owns release packaging, signed artifacts, versioned manifest rows, waiver workflow, cross-suite aggregation, and final release-gate classification. It must not be a closure prerequisite for earlier epics.

Epic 6 remains valid. Story 6.8 is still too broad for ordinary assignment unless one owner can handle both telemetry redaction and telemetry cardinality validation.

### Story Impact

Affected forward closure references:

- Story 1.5 currently cannot close until Story 5.5 tenant isolation conformance is passing or waived.
- Story 1.6 currently cannot close until Story 5.6 idempotent command conformance is passing or waived.
- Story 1.11 currently cannot close until Story 5.9 event schema evolution proof is passing or waived.
- Story 2.4 and split redaction stories currently cannot close until Story 5.7 redaction replay conformance is passing or waived.
- Story 4.5 currently cannot close until adopter conformance pack and CORE adopter fixture are passing or waived.

These should become local evidence requirements plus carry-forward release evidence obligations, not future-story closure dependencies.

### Artifact Impact

PRD: No FR or MVP change is required. The PRD already supports both local proof and release-gate evidence.

Architecture: No architecture rewrite is required. Architecture already states that conformance evidence is a release concern and that implementation slices need automated evidence. The backlog wording needs to reflect that two-level evidence model.

UX: No UX rewrite is required. The Leak Sentinel, accessibility, clipboard, responsive duplicate, and command-gate requirements remain binding and should be preserved when Story 3.8 is split or assigned as a verification checklist.

Epics: `epics.md` needs a targeted correction to replace "Foundation Gate Closure Rules" with "Two-Level Evidence Rules".

Sprint status: No sprint-status artifact was found during prior correction; update only if one is created later.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Scope classification: Moderate backlog reorganization.

Rationale: The product direction and planning artifacts are sound. The issue is backlog sequencing language. Moving minimum proof into the introducing story while keeping Epic 5 responsible for release aggregation preserves governance without making early epics unclosable.

Effort estimate: Low to medium planning effort before kickoff.

Risk if not corrected: High. Early stories will either stall behind future evidence stories or be closed prematurely despite wording that says they cannot close.

Residual risk after correction: Low to medium. Remaining risk is mainly story sizing for broad verification bundles and ADR-gated decisions.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | Trigger is readiness report issue: forward closure dependencies from early stories into Epic 5. |
| 1.2 Core problem | Done | Backlog closure semantics incorrectly require future release-evidence stories before earlier implementation stories can close. |
| 1.3 Evidence | Done | Evidence captured in `implementation-readiness-report-2026-05-16.md` and current `epics.md` Foundation Gate Closure Rules. |
| 2.1 Current epic impact | Done | Epic 1 can remain first only if Stories 1.5, 1.6, and 1.11 close on local minimum evidence. |
| 2.2 Epic-level changes | Done | Replace future closure dependencies with local evidence plus release-manifest carry-forward. |
| 2.3 Remaining epics | Done | Epic 2 and Epic 4 need the same two-level evidence treatment. Epic 5 keeps release aggregation. |
| 2.4 New/obsolete epics | N/A | No new or removed epic is required. |
| 2.5 Order/priority | Done | Epic order can remain if closure dependencies are removed. |
| 3.1 PRD conflicts | Done | No PRD change required. |
| 3.2 Architecture conflicts | Done | Architecture supports local tests and release evidence as separate concerns. |
| 3.3 UX conflicts | Done | UX safety obligations remain intact; Story 3.8 still needs split-or-checklist handling. |
| 3.4 Other artifacts | Done | Future generated story files and any sprint-status artifact must mirror the corrected closure rules. |
| 4.1 Direct adjustment | Viable | Preferred path. |
| 4.2 Rollback | Not viable | No implementation rollback is needed. |
| 4.3 MVP review | Not viable | MVP remains achievable. |
| 4.4 Path selected | Done | Direct adjustment with moderate backlog reorganization. |
| 5.1 Issue summary | Done | Included in this proposal. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Direct adjustment. |
| 5.4 MVP action plan | Done | MVP unchanged; fix sequencing before kickoff. |
| 5.5 Handoff plan | Done | Included below. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Reconciled against the current readiness report and affected story sections. |
| 6.3 User approval | Action-needed | Approval is required before patching `epics.md`. |
| 6.4 sprint-status.yaml | N/A | No sprint-status artifact was found in the prior correction. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval and backlog patch. |

## 5. Detailed Change Proposals

### Proposal A: Replace Foundation Gate Closure Rules with Two-Level Evidence Rules

Artifact: `epics.md`

Section: `## Implementation Readiness Gates`

OLD:

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

NEW:

```markdown
### Two-Level Evidence Rules

Implementation stories close on minimum local evidence in the same story or same epic. Release-gate evidence closes later through Epic 5 release packaging, signed conformance artifacts, manifest rows, and waiver governance.

Minimum local evidence for story closure:

- Story 1.5 closes when tenant access and fail-closed behavior pass local automated tests for the scenarios named in the story acceptance criteria.
- Story 1.6 closes when idempotent command behavior passes local automated tests for duplicate, reordered, conflicting, unknown-outcome, and tenant-mismatched submissions.
- Story 1.11 closes when replay, projection rebuild, unsupported-version handling, and at least one schema-version compatibility path pass local automated tests or ADR-approved fixtures.
- Story 2.4 and Stories 2.4.1-2.4.3 close when the relevant redaction command, projection, client-surface, and operational-surface evidence passes in their own story scope.
- Story 2.5 closes when governance mutations fail closed on audit-write unavailability and paired audit evidence tests pass in the story scope.
- Story 4.5 closes when the adopter-facing conformance package and CORE fixture run locally or in CI and produce machine-readable safe results.

Release manifest carry-forward:

- Story 5.5 consumes Story 1.5 local evidence and adds release-gating tenant isolation manifest coverage.
- Story 5.6 consumes Story 1.6 local evidence and adds release-gating idempotency manifest coverage.
- Story 5.7 consumes Story 2.4 local evidence and adds release-gating redaction replay manifest coverage.
- Story 5.9 consumes Story 1.11 local evidence and adds release-gating event schema evolution manifest coverage.
- Story 5.10 consumes Story 4.5 local evidence and adds release-gating contract validation and CORE fixture manifest coverage.

Waivers apply to release-gate evidence, not ordinary story closure, unless the affected story explicitly cannot meet its own minimum local evidence. Any waiver must name owner, approver, expiry, compensating control, buyer impact, and review date.
```

Rationale: This removes forward closure dependencies while preserving strict release evidence.

### Proposal B: Update the Readiness Summary Bullet

Artifact: `epics.md`

Section: readiness summary near the requirements inventory.

OLD:

```markdown
- Enforce Foundation Gate closure rules: CORE implementation stories may start only when direct ADR prerequisites are recorded or explicitly waived, and they may not close until the matching Foundation Gate evidence is passing in CI or covered by a named waiver.
```

NEW:

```markdown
- Enforce two-level evidence rules: CORE implementation stories close on minimum local evidence in the same story or epic, while Epic 5 owns release-gate aggregation, signed evidence, manifest rows, and waiver governance.
```

Rationale: This keeps the top-level guidance consistent with the corrected implementation readiness gates.

### Proposal C: Add Story-Level Carry-Forward Notes Where Needed

Artifact: `epics.md`

Affected stories: 1.5, 1.6, 1.11, 2.4, 2.4.1, 2.4.2, 2.4.3, 4.5.

Add this note to each affected story after acceptance criteria:

```markdown
**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate evidence is carried forward into the relevant Epic 5 conformance or contract validation story for manifest aggregation and signing.
```

Use specific carry-forward mapping where useful:

- Story 1.5 -> Story 5.5.
- Story 1.6 -> Story 5.6.
- Stories 2.4-2.4.3 -> Story 5.7.
- Story 1.11 -> Story 5.9.
- Story 4.5 -> Story 5.10.

Rationale: If stories are later sharded into individual files, the distinction survives the copy.

### Proposal D: Split Broad Verification Bundles Before Assignment

Artifact: `epics.md`

Story 3.8 is acceptable as an epic-level verification checklist, but if it is assigned as implementation work, split it into:

- Story 3.8: Verify Responsive Layout and Mobile Safe Triage.
- Story 3.9: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety.
- Story 3.10: Verify Leakage, Clipboard, Browser, and Viewport Telemetry Safety.

Story 6.8 is acceptable only if one owner can handle both validation domains. Otherwise split it into:

- Story 6.8: Validate Operational Telemetry Redaction.
- Story 6.9: Validate Operational Telemetry Cardinality Gates.

Story 2.4.3 may stay as a validation checklist if assigned to a release/test owner. If assigned as implementation work, split it into:

- Operational log, trace, error, and diagnostic redaction safety.
- Export and evidence redaction safety.
- Cache and screenshot redaction safety.
- Future derived-index ADR placeholder.

Rationale: These are valuable verification scopes, but too broad for ordinary single-owner implementation stories.

## 6. Implementation Handoff

Change scope: Moderate.

Recommended recipients:

- Product Owner or Story Manager: Apply approved backlog edits to `epics.md`, especially Proposal A and Proposal B.
- Architect: Confirm that story closure and release-gate evidence are deliberately separated.
- Test Architect: Map each local evidence artifact to its Epic 5 manifest consumer.
- Developer agent: Do not generate implementation story files until `epics.md` no longer contains forward closure dependencies into Epic 5.

Success criteria:

1. `epics.md` no longer says Story 1.5, 1.6, 1.11, 2.4, or 4.5 cannot close until future Epic 5 stories pass.
2. Each affected early story has minimum local evidence required for story closure.
3. Epic 5 stories consume local evidence for release aggregation rather than blocking earlier epic closure.
4. Story 3.8, Story 6.8, and Story 2.4.3 are split or explicitly assigned as verification checklists.
5. FR coverage remains 104/104 after edits.
6. Implementation readiness can be rerun with the forward dependency finding closed.

## 7. Approval Status

Status: Approved.

Approved on 2026-05-16.

Post-approval action completed: `_bmad-output/planning-artifacts/epics.md` was patched to replace forward closure dependencies with two-level evidence rules and story-level carry-forward evidence notes.
