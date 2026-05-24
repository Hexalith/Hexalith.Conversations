# Hexalith.Conversations Readiness Gate Decisions

Date: 2026-05-24
Approved by: Jerome (Product Owner)
Purpose: Resolve the single open decision routed by `sprint-change-proposal-2026-05-24.md` and the Epic 2-6 retrospectives — whether to build a rendered investigation-workspace UI host or waive the rendered-UI verification stories (3.8A/3.8B/3.8C) for v1.

This decision follows the same conservative posture as `readiness-gate-decisions-2026-05-17.md`: choose the narrowest v1-safe outcome already supported by the delivered scope, and require an ADR / buyer approval / explicit release-scope promotion for anything broader.

## Reopen Addendum: Stories 3.8A, 3.8B, and 3.8C

Date: 2026-05-24
Approved by: Jerome (Product Owner)

Decision: Stories **3.8A**, **3.8B**, and **3.8C** are reopened for implementation and story creation. The previous headless-v1 waiver is superseded for the full rendered-UI verification split.

Implementation rule: Stories 3.8A/3.8B/3.8C must produce evidence against a real rendered investigation-workspace surface. The implementation may create or adopt the narrowest first-party UI host required for responsive layout, accessibility, and disclosure verification, but it must not pass by asserting DTO-level safety alone. If no rendered host exists after implementation, each affected story is blocked, not done.

Traceability update: `sprint-status.yaml` keeps `epic-3` in `in-progress` and moves `3-8a-verify-responsive-layout-and-mobile-safe-triage`, `3-8b-verify-accessibility-tree-keyboard-and-screen-reader-safety`, and `3-8c-verify-leakage-clipboard-browser-and-telemetry-disclosure-safety` to `ready-for-dev`. `readiness-gates.md` marks the Story 3.8 assignment plan and Investigation workspace UI host gates as `decided` for all three stories. `docs/release-evidence/waiver-story-3-8-investigation-workspace-ui-host.json` is retained as historical release evidence with lifecycle status `superseded`.

## Original Decision

### Investigation workspace UI host and Story 3.8A/3.8B/3.8C scope

Decision: **v1 ships headless.** The Conversations module delivers a contract package plus a supported .NET client; it does not deliver a rendered operator UI. No `Hexalith.Conversations.Admin`, Blazor, or web host exists in the solution, and none was in scope for the original v1 waiver decision. Building one was recorded as a **future epic** to be planned (epics -> architecture -> UX) if and when a first-party operator UI is committed.

Consequence: Stories **3.8A** (responsive layout and mobile safe triage), **3.8B** (accessibility tree, keyboard, and screen-reader safety), and **3.8C** (leakage, clipboard, browser, and telemetry disclosure safety) verify a rendered surface that did not exist at the time of the original decision. They were **waived for v1** under the named waiver below rather than implemented against a fabricated UI. This original waiver is now superseded by the reopen addendum above.

Implementation rule: The reopen addendum is now binding. Stories 3.8A/3.8B/3.8C must run against a real rendered surface before any first-party investigation-workspace UI ships.

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
| Risk | Superseded historical waiver. Stories 3.8A/3.8B/3.8C are reopened for rendered responsive, accessibility, and disclosure verification against a real first-party investigation workspace surface. |
| Compensating control | Data-layer non-disclosure proven across Stories 3.1-3.7 (774 passing tests: permission-safe read DTOs, fail-closed degradation, normalized forbidden-vocabulary checks) plus operational telemetry redaction and cardinality bounds from Stories 6.8A/6.8B (32 passing tests). v1 delivers a headless module; adopters render their own UI and inherit the documented non-disclosure read contract. |
| Buyer impact | Historical only. Buyers integrating the headless v1 module still receive contract-level non-disclosure guarantees, but any first-party rendered investigation workspace now requires completion or explicit replacement of Stories 3.8A/3.8B/3.8C before release. |
| Expiry (UTC) | 2027-05-24 |
| Review date (UTC) | 2026-11-24 |
| Lifecycle status | superseded |
| Created (UTC) | 2026-05-24 |

Review trigger: superseded by the 2026-05-24 reopen addendum for Stories 3.8A/3.8B/3.8C.

## Traceability

- Routed by: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-24.md` (§3, §4 "Stories (re-gated, not built)")
- Source findings: `epic-3-retro-2026-05-24.md` (Action Item "UI host decision"; owner Winston / Sally / Jerome)
- Gate records updated: `readiness-gates.md` — `Investigation workspace UI host` and `Story 3.8 assignment plan` -> `decided`
- Sprint status: `sprint-status.yaml` — 3.8A/B/C -> `ready-for-dev`; `epic-3` -> `in-progress`
- Deferred work: `deferred-work.md` — 3.8 rendered-UI entry updated to superseded-by-reopen; rendered responsive/accessibility/disclosure verification is no longer deferred
