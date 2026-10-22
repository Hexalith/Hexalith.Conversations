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

## Open Questions

1. **Entry sequence:** complete 16.1/16.2 and entry decisions first; or explicitly authorize out-of-order 16.3 preparation with acceptance blocked. Missing decisions include reconciled AD-5–AD-8 carriers, operational envelope and scoped execution authorization. The recorded lift covers only `7.1-SCHEMAS`; both predecessors remain backlog. Current-change policy supplies no runtime approval.
2. **AppHost ownership:** named baseline-owner/Architect/Runtime exemption retaining the non-packable local fixture; or platform relocation amendment. Architecture requires this before entry. Relocation requires revising this draft's paths before approval.

## Code Map

- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs` — reuse gateway/Dapr/Redis patterns; its tenant bypass cannot prove 16.1 readiness.
- `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs` — preserve `ReconcileAsync` and contracts.
- EventStore `DomainService/EventStoreDomainServiceExtensions.cs` maps the route; `Server/Projections/{NamedProjectionDispatchCoordinator,DaprProjectionDeliveryRetryScheduler,ProjectionDeliveryRetryWorker}.cs` owns recovery. Observe public scheduler state and hosted execution; `RunOnceAsync` is internal.
- `_bmad/scripts/{evidence_boundary_current,authoring_outputs}.py` — reuse neutral Git bridge, ledgers and contained publication.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprDiagnostics.cs` and owning repository's `tests/Hexalith.EventStore.Testing.Integration.Tests/DaprDiagnosticsTests.cs` — extend shared readiness/binding diagnostics with positive/negative tests; preserve callers.
- [ ] `tests/Hexalith.Conversations.AppHost.Tests/{ConversationsAppHostRuntimeBoundaryTest,ConversationTerminalReconciliationLiveTest}.cs` and its `.csproj` — consume preflight; launch production hosts; induce failure; observe durable terminal work, coordinator HTTP route, clearance and persisted query state.
- [ ] `_bmad/scripts/verify_story_16_3.py` and `_bmad/scripts/tests/test_verify_story_16_3.py` — AC-01–05 runner, closed exits/results, fault identities, restoration digests, anti-vacuity and candidate/runtime/route binding.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — independently derive predecessor/receipt bindings; add mutation tests.
- [ ] `artifacts/v14/16.3/` and `docs/release-evidence/story-16.3-final-record-v2.{json,md}` — measure committed sources with exact effective commands; regenerate identically; publish atomically without replacing accepted evidence.
- [ ] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — review, scoped verification, verbatim insertion, then owned transition.

**Acceptance Criteria:**
- Given authorized entry and predecessors, when contracted commands run, then six scenarios pass: `6/6/0/0/0/0`.
- Given identical sources, when regenerated, then both records match bytes and bind runtime, route, faults, candidate/gitlinks and rollback.
- Given faults/unavailable dependencies, when verified, then FAIL/BLOCKED remains explicit and fixtures restore.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Build/run affected projects individually in Debug; use built xUnit assemblies with `-class`/`-method` and `-failSkips`; require executed live tests.
- Run focused Python suites and exact effective contract commands; scoped evidence and Story 7.3 guards before review/done. FAIL/BLOCKED halt; preserve not-applicable.
- `git diff --check`; validate commitlint before separately authorized commits. No push.
