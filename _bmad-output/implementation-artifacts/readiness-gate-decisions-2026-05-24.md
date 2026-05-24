# Hexalith.Conversations Readiness Gate Decisions

Date: 2026-05-24
Approved by: Jerome (Product Owner)
Purpose: Resolve the single open decision routed by `sprint-change-proposal-2026-05-24.md` and the Epic 2-6 retrospectives — whether to build a rendered investigation-workspace UI host or waive the rendered-UI verification stories (3.8A/3.8B/3.8C) for v1.

This decision follows the same conservative posture as `readiness-gate-decisions-2026-05-17.md`: choose the narrowest v1-safe outcome already supported by the delivered scope, and require an ADR / buyer approval / explicit release-scope promotion for anything broader.

## Decision

### Investigation workspace UI host and Story 3.8A/3.8B/3.8C scope

Decision: **v1 ships headless.** The Conversations module delivers a contract package plus a supported .NET client; it does not deliver a rendered operator UI. No `Hexalith.Conversations.Admin`, Blazor, or web host exists in the solution, and none is in scope for v1. Building one is recorded as a **future epic** to be planned (epics → architecture → UX) if and when a first-party operator UI is committed.

Consequence: Stories **3.8A** (responsive layout and mobile safe triage), **3.8B** (accessibility tree, keyboard, and screen-reader safety), and **3.8C** (leakage, clipboard, browser, and telemetry disclosure safety) verify a rendered surface that does not exist. They are **waived for v1** under the named waiver below rather than implemented against a fabricated UI. No rendered-UI tests were created.

Implementation rule: If a first-party investigation-workspace UI is later built, this waiver must be reviewed and 3.8A/3.8B/3.8C must run against the rendered surface before that UI ships. Until then, Epic 3 is closed for v1 with 3.1-3.7 `done` and 3.8A/B/C `waived`.

## Named Waiver

This waiver is recorded in the project's own `ReleaseWaiverV1` shape at `docs/release-evidence/waiver-story-3-8-investigation-workspace-ui-host.json` and carries every field the readiness-gate tracker requires (owner, approver, expiry, compensating control, buyer impact, review date).

| Field | Value |
| --- | --- |
| Waiver ID | `waiver-story-3-8-investigation-workspace-ui-host` |
| Owner | Architect (Winston) — single-threaded per the architect-availability gate |
| Approver | Product Owner (Jerome) |
| Affected requirement | NFR69 (anchor); covers FR56-FR69 verification support, UX-DR39-UX-DR52, NFR19-NFR21, NFR55-NFR61, NFR69-NFR77 |
| Affected gate | None — rendered-UI verification is not in the closed release-gate vocabulary |
| Affected stories | 3.8A, 3.8B, 3.8C |
| Is blocker | No — v1 ships no rendered surface; the NFR62 redaction-non-leakage blocker category is satisfied at the data layer (Story 5.7, decided/done) |
| Risk | Rendered investigation-workspace UI is unverified for responsive layout, accessibility/screen-reader safety, and disclosure leakage (DOM, clipboard, browser title, telemetry). Latent: materializes only if a rendered workspace is later built and shipped without first running 3.8A/B/C against it. |
| Compensating control | Data-layer non-disclosure proven across Stories 3.1-3.7 (774 passing tests: permission-safe read DTOs, fail-closed degradation, normalized forbidden-vocabulary checks) plus operational telemetry redaction and cardinality bounds from Stories 6.8A/6.8B (32 passing tests). v1 delivers a headless module; adopters render their own UI and inherit the documented non-disclosure read contract. |
| Buyer impact | Buyers integrating the headless v1 module receive contract-level non-disclosure guarantees and run their own rendered-surface verification for responsive, accessibility, and leakage safety against any UI they build. No operator UI is delivered or certified in v1. |
| Expiry (UTC) | 2027-05-24 |
| Review date (UTC) | 2026-11-24 |
| Lifecycle status | active |
| Created (UTC) | 2026-05-24 |

Review trigger: re-review at the review date **or** when a first-party investigation-workspace UI host is committed, whichever is first.

## Traceability

- Routed by: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-24.md` (§3, §4 "Stories (re-gated, not built)")
- Source findings: `epic-3-retro-2026-05-24.md` (Action Item "UI host decision"; owner Winston / Sally / Jerome)
- Gate records updated: `readiness-gates.md` — `Investigation workspace UI host` and `Story 3.8 assignment plan` → `waived`
- Sprint status: `sprint-status.yaml` — 3.8A/B/C `backlog -> waived`; `epic-3 in-progress -> done`
- Deferred work: `deferred-work.md` — 3.8 rendered-UI entry updated to closed-for-v1-by-waiver with the UI host recorded as a future epic
