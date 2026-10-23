---
title: 'Story 16.1: Persist tenant-access projection state and prove convergence'
type: 'feature'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant authorization uses a process-local projection lost on restart. Consumer-group delivery cannot populate every replica, and missing freshness evidence currently defaults to healthy.

**Approach:** Consume an additive Tenants durable projection capability backed by EventStore mutation/recovery infrastructure. Prove restart recovery without redelivery and consistent authorization across replicas.

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy.

## Boundaries & Constraints

**Always:** Enforce architecture AD-5/AD-6. EventStore owns identity, CAS, retry, replay, fenced recovery and acknowledgements; Tenants owns transitions/checkpoints. Conversations evaluates one state/health/checkpoint snapshot per invocation. Only proven healthy state authorizes. Preserve compatibility and historical evidence.

**Never:** Direct Dapr persistence, unconditional saves, process-local concurrency, read repair, synthetic health, or early acknowledgement. Exclude 16.2/16.3, deployment and destructive migration. Rollback removes adoption/configuration; retain events, evidence and accepted records.

## I/O & Edge-Case Matrix

| State/delivery | Required behavior |
| --- | --- |
| Healthy, exact-next sequence | Atomically commit transition and checkpoint |
| Identical accepted duplicate | No-op; verify older sequences against authoritative history |
| Conflicting identity/payload | Persist terminal Corrupt; deny |
| Future sequence | Persist Gapped, immutable expected sequence, monotonic recoveryThrough and fingerprints; deny |
| CAS/provider/retry exhaustion | Retryable failure, no acknowledgement; authorization unavailable |
| Fenced recovery | Validate history/evidence; restore Healthy only with AD-6 final CAS and no pending evidence |
| Missing/stale/unavailable or malformed state | Deny without disclosing tenant existence |
| Restart or second replica | Reload the same persisted decision/checkpoint without redelivery |

</frozen-after-approval>

## Open Questions

1. **Entry/adoption:** qualify Platform enrollment and the successor contracts before scoped runtime execution. Platform already owns provider selection, operational roles, recovery targets and environment policy; remaining items concern qualification and entry approval.

## Code Map

- `_bmad-output/planning-artifacts/architecture.md:2941` — normative AD-6 mutation/recovery contract and mandatory fault schedules.
- `_bmad-output/planning-artifacts/epic-16-prerequisites-v1/epic.md` — draft envelope, successor coverage, graph and pending decisions.
- EventStore/Tenants paths below belong to their root-declared `references/Hexalith.*` repositories.
- EventStore `Projections/IReadModelStore.cs`, `ReadModelWritePolicy.cs`, `Streams/IAuthoritativeEventStreamReader.cs` supply CAS/retry/history, not fenced tenant recovery.
- Tenants `ITenantProjectionStore` provides unconditional Get/Save; its handler locks locally.
- Conversations `TenantAccess` reads health/state separately; `DefaultConversationTenantProjectionSignal` defaults to Healthy.

## Tasks & Acceptance

**Execution after entry is resolved:**

- [ ] EventStore `src/Hexalith.EventStore.Client/Subscriptions/` — add `ProjectionDeliveryFingerprint.cs` and `ProjectionDeliveryCoordinator.cs`; update `EventStoreDomainEventContext.cs` and `EventStoreDomainEventProcessor.cs` to validate raw identity before marker deduplication.
- [ ] EventStore `src/Hexalith.EventStore.Client/Projections/` — add AD-6 recovery/coordinator/lease types using existing CAS/retry/history; fence stale workers.
- [ ] Tenants `src/Hexalith.Tenants.Client/Projections/` — add `EventStoreTenantProjectionStore.cs`, snapshot/checkpoint/health/transition/options types; update handler and `Registration/TenantServiceCollectionExtensions.cs` for atomic mutation and recovery.
- [ ] `src/Hexalith.Conversations.Server/TenantAccess/` — configure durable adoption, fail closed without health, and add `ConversationTenantProjectionSnapshotAdapter.cs` shared by health/state evaluation within one invocation. Update `Program.cs` disclosure only after proofs pass.
- [ ] Owner `tests/Hexalith.EventStore.Client.Tests/`, `tests/Hexalith.Tenants.Client.Tests/`, and Conversations `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`, `tests/Hexalith.Conversations.IntegrationTests/TenantAccess/` — assert persisted end-state for the matrix and all AD-6 concurrency/recovery schedules through production composition.
- [ ] `_bmad/scripts/verify_story_16_1.py` and its tooling tests — implement non-vacuous AC-01–05 verification using neutral evidence helpers and the approved successor contract; generate AC-06 JSON/Markdown only from measured committed-source receipts.

**Acceptance Criteria:**

- Given acknowledged events, when one host restarts without redelivery or another reads shared storage, then authorization and sequence evidence agree.
- Given concurrent mutation, when authorization runs, then state and health come from one snapshot and unsafe evidence never authorizes.
- Given committed owner revisions, when all six contracted scenarios run, then the generated record reports `6/6/0/0/0/0`, compatible public contracts, safe diagnostics, raw gitlink identities and rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

## Spec Change Log

## Review Triage Log

## Verification

- Build owner test projects individually in Debug; run focused xUnit `-class` selections and affected suites.
- Use the successor contract's exact commands and scoped evidence requirements. Do not revive retired V19–V29 publications for routine gitlink drift.
- `python3 _bmad/scripts/verify_epic_16_prerequisites.py --repository .` — draft consistency only. Runtime AC-01 remains BLOCKED: its verifier is missing.
