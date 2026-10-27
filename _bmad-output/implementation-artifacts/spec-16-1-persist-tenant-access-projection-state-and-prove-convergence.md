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

**2026-10-27 decision:** Prepare a separate enrollment/adoption-remediation plan. This selects prerequisite planning; runtime entry remains held.

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

[Current investigation](story-16-1-entry-investigation-2026-10-26.md): exact seams, commands and owner gates. Tenants lacks durable snapshots; Conversations separates state/health. Successor tooling exists, entry null/trust empty. Isolated CLI 4.38.0 remains unqualified for enrollment; accepted pins are 4.35.2.

## Tasks & Acceptance

**Prerequisites; runtime remains held:**

- [x] `module/`, planning `epic-16-adoption-v2/` — retain preparation.
- [x] `module/validation/epic-16-prerequisite-implementation/qualification.json` — retain bounded 4.38.0 observations.
- [x] `_bmad/scripts/generate_story_record.py`, `verify_story_16_{1,2,3}.py`, planning `epic-16-tooling-v1/` — successor consumption/fifteen scenarios implemented under the done prerequisite spec.
- [ ] Platform/root `.config/dotnet-tools.json`, build/Dapr mappings — separately scope adoption remediation and qualify release/source pairing before alignment.
- [ ] Planning `production-operational-envelope-v1.md`, `module/sm-2-checklist.md` — obtain parity, recovery/operator, capacity disposition, human SM-2 and trusted owner entry.

**After qualified entry; upstream paths belong to their root-declared repositories:**

- [ ] EventStore Client `Subscriptions/`, `Projections/` — raw binding before deduplication, atomic mutation, pending evidence, fenced recovery and durable acknowledgements.
- [ ] Tenants Client `Projections/`, handler/registration — durable coherent snapshots and pure transitions; preserve signatures.
- [ ] Conversations `Server/TenantAccess/ConversationTenantProjectionSnapshotAdapter.cs`, access/signal/registration — one persisted snapshot, fail-closed health and injected options. Retain Program.cs disclosure until proofs pass.
- [ ] Owner Client tests, Conversations `Server.Tests/TenantAccess/`, new `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` — frozen matrix/concurrency/recovery/provider faults, persisted end-state/acknowledgements and native loaded-product capture.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading another replica, then decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and committed owner revisions, when six adopted scenarios run, then the generated record reports `6/6/0/0/0/0`, compatibility, safe diagnostics and exact gitlink/rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

2026-10-23: three owner repositories plus enrollment/evidence affected; no runtime changes. Approved prerequisite preparation is tracked separately in `spec-epic-16-adoption-preparation.md`; this runtime story remains draft/backlog.

2026-10-26: prerequisite implementation is done. Refreshed planning and retained ten command observations; no runtime implementation, dependency update or lifecycle transition. Entry route remains unresolved.

2026-10-27: the user selected the separate remediation-planning route. See [the new draft](spec-epic-16-enrollment-adoption-remediation.md); implementation and runtime entry remain unapproved.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

2026-10-26: replaced stale tooling tasks with completed prerequisite facts and remaining entry work; preserved frozen intent verbatim.

## Review Triage Log

## Verification

[Current commands/results](story-16-1-entry-investigation-2026-10-26.md#verification): six story commands BLOCKED/exit 2; accepted CLI adoption exit 1, six PASS/24 FAIL; historical v2 checker FAIL/four source mismatches. Runtime publication not-applicable; prior tests not rerun.

After entry: individual Debug builds, focused xUnit, affected suites and six successor contract commands; no missing runtime result may pass.
