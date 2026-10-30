---
title: 'Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live'
type: 'feature'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** AppHost readiness errors lack stable classifications, and live coverage does not prove recovery of durable terminal projection work.

**Approach:** Add shared preflight diagnostics and a live AppHost proof of production reconciliation, then derive the six-scenario record from measured results.

**2026-10-30 decision:** The user selected prerequisite-first execution: qualify entry, complete 16.1, then 16.2, then 16.3. Pursue a narrow exemption for the existing AppHost test fixture; named baseline-owner/Architect/Runtime acceptance remains pending. Separate early preparation and Platform relocation are deferred.

## Boundaries & Constraints

**Always:** EventStore owns writes, hosting and the authenticated `project/v2/reconcile` coordinator route. Shared helpers belong there. Require durable pending/end-state observations, production queries, bounded cancellation, safe diagnostics and isolated fixture cleanup. Bind evidence to committed sources, runtime/route/catalog identities, raw gitlinks and rollback. Preserve V14 history; consume authorized successor contracts.

**Never:** Direct-handler live proof, reflective worker access, another reconciler, read repair, weaker authorization, vacuous PASS or fabricated approvals. Rollback removes new diagnostics/fixtures/evidence; preserve production routes and 16.1/16.2 state.

## I/O & Edge-Case Matrix

| State | Required result |
| --- | --- |
| Healthy resource, endpoint unavailable | `APPHOST_ENDPOINT_NOT_READY`, endpoint identity, bounded retry advice; no tenant leakage |
| Effective control-plane binding occupied | `DAPR_CONTROL_PLANE_PORT_COLLISION`, owning endpoint/port; unrelated ports do not fail |
| Controlled first projection failure | Durable pending/terminal work observable before recovery |
| Production coordinator retry | Reconciliation observed on wire; one terminal item cleared; persisted projection query-visible |
| Duplicate retry or concurrent progress | Idempotent clearance; newer state preserved |
| Removed/drifted route, endpoint race, missing Dapr, empty test selection | Stable failure/blocker; nonempty ledger; ports/files restored |

</frozen-after-approval>

## Entry Requirements

The [handoff](../../module/platform-handoff.md#selected-sequence-and-apphost-exemption-proposal--2026-10-30) records prerequisite actions and exemption terms. Runtime entry remains BLOCKED; retain draft/backlog until qualification and predecessor acceptance.

## Code Map

The [entry investigation](story-16-3-entry-investigation-2026-10-29.md) maps diagnostics, routing, durable scheduling and fixtures. Reuse shared validators/generator; the gateway fixture's tenant bypass cannot prove production admission.

Select `_bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json` explicitly for native identities/output paths; preserve V14 history.

## Tasks & Acceptance

**After qualified entry only, in dependency order:**

- [ ] `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprDiagnostics.cs` — add bounded, safe endpoint/binding diagnostics; extend the existing owning `DaprTestPrerequisiteDiagnosticsTests.cs` and `GeneratedApiSmokePreflightDiagnosticsTests.cs` suites.
- [ ] `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs` and its `.csproj` — consume shared preflight through mapped references; preserve authentication.
- [ ] `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3EvidenceTests.cs` — implement contract-bound `Scenario01`–`Scenario05`: real production hosts, durable pending work, authenticated coordinator traffic, persisted/query-visible recovery, duplicate/concurrent progress and fault restoration. Observe hosted workers through public scheduler seams.
- [ ] `artifacts/epic16-runtime/16.3/` and `docs/release-evidence/story-16.3-final-record-v2.{json,md}` — capture committed native sources/binaries, run the six selected commands and verify deterministic generation; publish only under separately authorized scope.
- [ ] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — review, scoped verification, verbatim record insertion and owned transition after acceptance.

**Acceptance Criteria:**

- Given qualified entry and accepted predecessors, when all six selected commands run, then the derived summary is `6/6/0/0/0/0`.
- Given identical sources, when regenerated, then both records match bytes and bind runtime, route, faults, candidate/gitlinks and rollback.
- Given faults or unavailable dependencies, when verified, then FAIL/BLOCKED stays explicit, required execution is nonempty and fixtures restore.

## Implementation Notes

## Spec Change Log

2026-10-29: refreshed entry facts, existing-tooling reuse, native test identity and output paths. KEEP frozen intent, draft/backlog, predecessor chain, accepted evidence and ACTIVE hold unchanged.

2026-10-30: recorded the user's selected sequence/exemption direction and removed answered questions. KEEP prior intent, pending owner acceptance, draft/backlog and ACTIVE hold.

## Review Triage Log

## Verification

2026-10-29 entry check: all six investigation commands returned BLOCKED/exit 2, `ENTRY_QUALIFICATION_PENDING`, zero runtime tests and no outputs. Ten owner qualifications, trusted entry and accepted predecessors remain required.

After entry: use Platform-materialized mappings, individual Debug builds and built xUnit `-class`/`-method` with `-failSkips`. Run the selected contract commands, focused shared-tooling regressions, scoped evidence verifier and Story 7.3 guards before review/done; FAIL/BLOCKED halts. Historical evidence replay is not-applicable to this planning refresh. Run `git diff --check`; commits/publication need separate authorization and pinned commitlint validation. No push.
