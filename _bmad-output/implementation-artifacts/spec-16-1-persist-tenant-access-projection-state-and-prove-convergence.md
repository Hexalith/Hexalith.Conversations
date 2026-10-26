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

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy. On 2026-10-23 the user selected Platform enrollment and successor tooling preparation for review; runtime remains held. On 2026-10-25 the user approved additive prerequisite refresh with accepted CLI 4.35.2; runtime entry remains held. On 2026-10-25 the user authorized corrected CLI qualification and successor-tooling implementation under the separate prerequisite scope; runtime entry remains held.

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

The [entry investigation](story-16-1-entry-investigation-2026-10-25.md) records seams, tests and commands. Tenants lacks durable snapshots; Conversations separates state/health. Published CLI 4.38.0 fixes token validation; Platform accepts 4.35.2.

## Tasks & Acceptance

**Prerequisites; implementation authorized in [the prerequisite spec](spec-epic-16-prerequisite-implementation.md); runtime remains held:**

- [x] `module/`, planning `epic-16-adoption-v2/` — retain preparation.
- [ ] Platform/root `.config/dotnet-tools.json` — qualify a corrected CLI and address review findings before alignment; retain observations.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/`, `_bmad/scripts/verify_story_16_{1,2,3}.py` — implement successor consumption and fifteen measured scenarios. Preserve eighteen ACs, completion guards and missing-measurement denial; use additive bindings.
- [ ] Planning `production-operational-envelope-v1.md`, `module/sm-2-checklist.md` — resolve adoption findings and obtain owner-qualified parity, recovery/operator, human SM-2 evidence and separate scoped entry.

**After qualified entry; upstream paths belong to their root-declared repositories:**

- [ ] EventStore Client `Subscriptions/`, `Projections/` — validate raw binding before deduplication; add atomic mutation, pending evidence, fenced recovery and durable acknowledgement.
- [ ] Tenants Client `Projections/`, `Handlers/TenantProjectionEventHandler.cs`, `Registration/TenantServiceCollectionExtensions.cs` — add durable coherent snapshots and pure transitions, preserving signatures.
- [ ] Conversations `Server/TenantAccess/ConversationTenantProjectionSnapshotAdapter.cs`, access/signal/registration — evaluate one persisted snapshot, deny unproven health and bind injected options. Retain Program.cs disclosure until proofs pass.
- [ ] Owner Client tests and Conversations `Server.Tests/TenantAccess/`, `IntegrationTests/TenantAccess/` — cover the frozen matrix, reverse saves, three interleaved future deliveries, horizon extension, lease expiry/restart and provider exhaustion; assert persisted end-state/acknowledgements.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading another replica, then decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and committed owner revisions, when six adopted scenarios run, then the generated record reports `6/6/0/0/0/0`, compatibility, safe diagnostics and exact gitlink/rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

2026-10-23: three owner repositories plus enrollment/evidence affected; no runtime changes. Approved prerequisite preparation is tracked separately in `spec-epic-16-adoption-preparation.md`; this runtime story remains draft/backlog.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

## Review Triage Log

## Verification

[Exact current commands/results](story-16-1-entry-investigation-2026-10-25.md#verification): v2 verifier FAIL on three upstream source changes; accepted CLI adoption exit 1, six PASS/eighteen FAIL; 162 verifier tests pass. Entry remains BLOCKED. No runtime was started.

After entry: individual Debug builds, focused xUnit, affected suites and scoped verification.
