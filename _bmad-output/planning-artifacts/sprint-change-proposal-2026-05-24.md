---
date: 2026-05-24
project: Hexalith.Conversations
source_report: epic-2-retro-2026-05-24.md, epic-3-retro-2026-05-24.md, epic-4-retro-2026-05-24.md, epic-5-retro-2026-05-24.md, epic-6-retro-2026-05-24.md
workflow: bmad-correct-course
status: approved
recommended_scope: minor-to-moderate
recommended_path: direct-adjustment
mode: batch
extends:
  - sprint-change-proposal-2026-05-17-readiness-assessment-follow-up.md
---

# Sprint Change Proposal: May 24 Retrospective Findings Remediation

## 1. Issue Summary

The Epic 2-6 retrospectives (generated 2026-05-24) surfaced a small set of cross-epic findings after the automated build cycle completed all 51 planned stories (1.1-6.7). None of these is a PRD, architecture, UX, or epic-coverage failure. The build delivered the planned scope; the findings are sprint-execution completeness and status-hygiene items, plus two intentional-scope caveats that should be made explicit.

The triggering findings:

1. **Five verification/validation stories were never built.** The automated orchestration ran stories 1.1 through 6.7 only. The split verification stories created by the May 17 proposal — `3.8A`, `3.8B`, `3.8C` (Compliance Investigation Workspace) and `6.8A`, `6.8B` (Operations/Observability) — remained `backlog`.
2. **Epic status hygiene.** Every epic showed `in-progress` in `sprint-status.yaml` even though all of its delivered stories were `done`. Epic 1's retrospective first flagged this; it had propagated to all six epics.
3. **A discovered planning gap blocks 3.8A/B/C.** These stories verify a rendered investigation-workspace UI (responsive layout, accessibility tree, screen-reader, clipboard, browser-title, screenshots). The Conversations module has **no Blazor/web/UI host project** — Epic 3 delivered server/contract DTOs only. The May 17 split assumed a workspace UI would exist by verification time; it never did.
4. **Named release-waiver runtime workflow is out of scope (Story 5.4).** Story 5.4 shipped the waiver contract, validator, schema, and fixture only. Runtime approval/issuance, durable storage, an admin/review surface, and cross-story aggregation into the release-gate decision are not implemented.
5. **Release evidence is infrastructure validated against synthetic fixtures**, not a produced signed GA certification run; PKI signing is ADR-gated out for v1.
6. **Content-safety blocklist friction** recurred across Epics 4-6 (domain terms such as `store`, `exception`, `sequence`, `stream`, `tenant-` collide with the scanner), handled ad hoc per story.

## 2. Impact Analysis

### Epic Impact

No epic needs removal, replacement, or resequencing.

- **Epics 1, 2, 4, 5, 6** are story-complete and retrospected. With Stories 6.8A/6.8B now built (see §4), Epic 6 has no remaining stories. All five transition `in-progress -> done`.
- **Epic 3** remains `in-progress`. Stories 3.1-3.7 are `done`, but 3.8A/B/C cannot be implemented as written without a UI host. Epic 3 cannot reach `done` until those stories are built, re-scoped, or waived.

### Story Impact

- **Stories 6.8A, 6.8B** — implemented and validated in this remediation (telemetry redaction + cardinality conformance suites; 32 tests, green; no regressions). Moved `backlog -> done`.
- **Stories 3.8A, 3.8B** — blocked. They require a rendered UI that does not exist. No fabricated UI tests were created.
- **Story 3.8C** — mostly blocked. Its non-UI subset (permission-safe DTO disclosure, telemetry-envelope safety) overlaps the DTO-level non-disclosure already proven in 3.1-3.7 and the new 6.8A telemetry-redaction suite. The rendered-surface portion (DOM/ARIA/clipboard/browser-title/screenshots) is blocked.
- **Story 5.4** — story-complete as a contract; runtime workflow recorded as explicit deferred work, not a defect.

### Artifact Conflicts

- `sprint-status.yaml` — epic statuses and the two 6.8 story statuses updated.
- `readiness-gates.md` — Story 3.8 assignment gate re-stated as `blocked`; a new `Investigation workspace UI host` gate added; Story 6.8 gate marked closed (stories built).
- `deferred-work.md` — four residual deferrals recorded.
- `epics.md` — no change required; 3.8A/B/C definitions remain valid pending the UI-host decision.
- PRD / architecture / UX — no change required.

### Technical Impact

- New test code only, additive, in `tests/Hexalith.Conversations.Conformance.Tests/`. No production source, no manifest fixtures, and no existing test counts were modified. The pre-existing green baseline is preserved (Conformance.Tests: 216 -> 248 passing).

## 3. Recommended Approach

**Direct Adjustment**, executed in this session:

- **Build what is buildable now (6.8A, 6.8B).** The telemetry they validate already exists; the assignment gate was already `decided`. Done and verified.
- **Reconcile epic status** for the five completed epics; keep Epic 3 honest at `in-progress`.
- **Re-gate 3.8A/B/C rather than fake it.** Record the missing-UI-host blocker as an explicit gate and a deferred item with a named compensating control (DTO-level non-disclosure from 3.1-3.7). Escalate the UI-host scope decision (build a UI epic, or formally waive 3.8A-3.8C for v1) — this is the one item that needs a product/architecture decision rather than direct implementation.
- **Record the intentional-scope caveats** (waiver runtime, release-evidence-vs-certification, content-safety helper) as deferred work so they are not mistaken for completed GA capabilities.

Rationale: this closes the genuinely-closable gap (telemetry validation) with real evidence, makes the project's true state visible, and prevents the one real risk — mistaking "story-complete" for "GA-certified" or pretending UI verification ran when no UI exists.

Risk assessment: low. All applied changes are additive or status reconciliations; the only open risk is the UI-host scope decision, which is surfaced for explicit ownership rather than silently closed.

## 4. Detailed Change Proposals

### Stories (built)

**6.8A — Validate Operational Telemetry Redaction** (new): `tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuite.cs` (+ test + shared fixtures). Drives the real `ConversationRejectionTelemetry`, `ConversationProjectionTelemetry`, and `ConversationConformanceTelemetry` through a `MeterListener` and proves: metric dimensions only ever carry closed-vocabulary class tokens + bounded booleans + the bounded `gate_id`; forbidden content fed through the only free-text-shaped parameter (correlation id, ILogger-bound, never a metric tag) is absent from all captured dimensions and log shapes; the `None` sentinel is rejected. 13 tests. Story record: `_bmad-output/implementation-artifacts/6-8a-validate-operational-telemetry-redaction.md`.

**6.8B — Validate Operational Telemetry Cardinality Gates** (new): `tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuite.cs` (+ test). Proves each telemetry dimension is bounded and approved: per-counter tag keys are a fixed approved set; under a >1000-emission load the distinct tag-value set stays within the closed vocabulary (≤64 ceiling, non-vacuous); `gate_id` is the only string dimension and is gated against an approved set. 19 tests. Story record: `_bmad-output/implementation-artifacts/6-8b-validate-operational-telemetry-cardinality-gates.md`.

Validation (independently re-run): `dotnet test ...Conformance.Tests --filter FullyQualifiedName~Telemetry` → **Passed: 32, Failed: 0**. Full project clean rebuild: **Passed: 248, Failed: 0**.

### Stories (re-gated, not built)

**3.8A / 3.8B / 3.8C** — left `backlog`. No rendered-UI tests were fabricated. Blocker recorded in `readiness-gates.md` and `deferred-work.md`. Decision required: (a) add an investigation-workspace UI host (new UI epic) and then run 3.8A/B/C, or (b) waive 3.8A-3.8C for v1 against the 3.1-3.7 DTO-level non-disclosure compensating control.

### sprint-status.yaml

```
epic-1: in-progress -> done
epic-2: in-progress -> done
epic-4: in-progress -> done
epic-5: in-progress -> done
epic-6: in-progress -> done            (6.8A/6.8B now done; no remaining stories)
epic-3: in-progress                    (unchanged; 3.8A/B/C blocked)
6-8a-validate-operational-telemetry-redaction: backlog -> done
6-8b-validate-operational-telemetry-cardinality-gates: backlog -> done
```

### readiness-gates.md

- `Story 3.8 assignment plan`: state `decided -> blocked`; notes point to the new UI-host gate.
- New gate `Investigation workspace UI host`: state `blocked`; owner PM / Architect / UX; blocks 3.8A/B/C rendered-UI verification; compensating control = DTO-level non-disclosure proven in 3.1-3.7.
- `Story 6.8 assignment plan`: notes updated to record 6.8A/6.8B built and validated 2026-05-24; expiry `Closed`.

### deferred-work.md

New section `Deferred from: correct-course findings remediation (2026-05-24)` with: (1) 3.8A/B/C rendered-UI verification blocked pending UI-host decision; (2) named release-waiver runtime workflow deferred; (3) release-evidence-infrastructure-vs-GA-certification caveat; (4) content-safety shared-check refactor.

## 5. Implementation Handoff

**Change scope classification: Minor-to-Moderate.**

- **Minor (done in this session):** 6.8A/6.8B implementation + validation, epic status reconciliation, deferred-work and readiness-gate records.
- **Moderate (routed):** the UI-host scope decision for 3.8A/B/C, and the residual deferred items (waiver runtime, GA release-gate execution, content-safety helper).

Handoff recipients:

- **PM / Architect / UX:** own the `Investigation workspace UI host` gate. Decide a UI-host epic vs. a formal v1 waiver of 3.8A-3.8C against the 3.1-3.7 compensating control. Until then, Epic 3 stays `in-progress`.
- **Developer / Test Architect:** if a UI host is approved, 3.8A/B/C become buildable; otherwise close them by waiver. Extract the shared content-safety check helper when the next conformance work lands.
- **PO / Test Architect (GA):** schedule the actual signed release-gate certification run (Epic 5 framework executed as a gate over real evidence) and the named-waiver runtime workflow before GA.

Success criteria:

- Telemetry validation evidence exists and is green (met).
- `sprint-status.yaml` no longer sends contradictory epic signals (met for epics 1/2/4/5/6; epic 3 intentionally open).
- The 3.8 blocker and the GA caveats are visible in binding artifacts rather than implicit (met).
- A named owner exists for the UI-host decision (routed).
