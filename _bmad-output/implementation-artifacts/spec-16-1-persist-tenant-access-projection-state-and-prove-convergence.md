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

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy. On 2026-10-23 the user selected Platform enrollment and successor tooling preparation for review; runtime remains held.

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

## Code Map

- Planning `architecture.md` AD-5/AD-6 and successor `story-contracts/16.1.json` govern six scenarios.
- Platform `docs/domain-module-enrollment.md` supplies enrollment rules.
- Reuse EventStore `IReadModelStore`, `ReadModelWritePolicy`, `IAuthoritativeEventStreamReader`; epoch fencing lacks AD-6 recovery.

## Tasks & Acceptance

**Entry preparation; runtime requires qualified owner entry:**

- [x] `module/{hexalith.module-manifest.v2.json,hosting-inventory.json,sm-2-checklist.md}` — prepared the candidate enrollment, recovery inventory and injected stores/feed for review; native adoption remains blocked.
- [ ] `_bmad-output/planning-artifacts/epic-16-prerequisites-v1/`, `_bmad/schemas/`, `_bmad/scripts/generate_story_record.py` — prepare executable successor adoption and scoped checks; preserve pinned drafts/history.

**After entry:** upstream paths below are relative to their root-declared repositories.

- [ ] EventStore `src/Hexalith.EventStore.Client/Subscriptions/` — extend `EventStoreDomainEventContext.cs` and `EventStoreDomainEventProcessor.cs` with raw binding validation before deduplication.
- [ ] EventStore `src/Hexalith.EventStore.Client/Projections/` — implement AD-6 atomic mutation, pending evidence and fenced recovery over CAS/retry/history.
- [ ] Tenants `src/Hexalith.Tenants.Client/Projections/`, `Handlers/TenantProjectionEventHandler.cs`, `Registration/TenantServiceCollectionExtensions.cs` — add durable snapshot/checkpoint/health store and pure transitions; preserve public signatures.
- [ ] `src/Hexalith.Conversations.Server/TenantAccess/` — add `ConversationTenantProjectionSnapshotAdapter.cs`; update access, registration and default signal to consume one snapshot and deny absent health. Change `Program.cs` disclosure only after proofs pass.
- [ ] Owner Client test projects and Conversations Server/IntegrationTests `TenantAccess/` — test the matrix/AD-6 schedules through production composition; assert persisted state and acknowledgements.
- [ ] `_bmad/scripts/verify_story_16_1.py` and tooling tests — measure AC-01–05 with neutral helpers; generate AC-06 from committed-source receipts.

**Acceptance Criteria:**

- Given acknowledged events, when a host restarts without redelivery or a replica reads shared storage, then decisions and contiguous checkpoints agree.
- Given concurrent mutation/recovery, when authorization runs, then one coherent snapshot controls access and unsafe state denies.
- Given qualified entry and committed owner revisions, when six adopted scenarios run, then the generated record reports `6/6/0/0/0/0`, compatible contracts, safe diagnostics, raw gitlinks and preserved rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

2026-10-23: three owner repositories plus enrollment/evidence affected; no runtime changes. Approved prerequisite preparation is tracked separately in `spec-epic-16-adoption-preparation.md`; this runtime story remains draft/backlog.

## Spec Change Log

## Review Triage Log

## Verification

- `python3 _bmad/scripts/verify_epic_16_prerequisites.py --repository .` — exit 0, draft PASS; entry BLOCKED, hold ACTIVE, authority flags false.
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py` — 37 passed.
- `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-01 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-01.json` — exit 2, file missing; acceptance BLOCKED.
- After entry: individual Debug builds, xUnit assembly `-class`/`-method`, affected suites and adopted scoped checks before review/completion.

2026-10-23 prerequisite handoff: `spec-epic-16-adoption-preparation.md` is complete for review-package preparation only. Both structural checkers pass; 168 focused tests pass, including a portable 131-test adoption suite. Correct SDK APP_API_TOKEN exposes accepted-tool HXP002 format incompatibility; native adoption retains 15 failing rows. Candidate wrappers remain unsupported by the final-record generator; runtime qualification and scoped entry are pending. Runtime tasks remain unchecked and sprint status stays backlog.
