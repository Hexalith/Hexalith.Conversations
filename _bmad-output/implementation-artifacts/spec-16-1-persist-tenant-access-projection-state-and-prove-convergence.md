---
title: 'Story 16.1: Persist tenant-access projection state and prove convergence'
type: 'feature'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant authorization uses a process-local projection lost on restart. Consumer-group delivery cannot populate every replica, and missing freshness evidence currently defaults to healthy.

**Approach:** Consume an additive Tenants durable projection capability backed by EventStore mutation/recovery infrastructure. Prove restart recovery without redelivery and consistent authorization across replicas.

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy. On 2026-10-23 the user selected Platform enrollment and successor tooling preparation for review; runtime remains held. On 2026-10-25 the user approved additive prerequisite refresh with accepted CLI 4.35.2; runtime entry remains held. On 2026-10-25 the user authorized corrected CLI qualification and successor-tooling implementation under the separate prerequisite scope; runtime entry remains held.

**2026-10-27 decision:** Prepare a separate enrollment/adoption-remediation plan. This selects prerequisite planning; runtime entry remains held.

**2026-10-27 follow-up decision:** The user selected option 1: prepare a separate tooling-qualification and source-binding-refresh plan. This authorizes planning only; runtime entry remains held.

**2026-10-28 post-tooling decision:** The user selected option 1: prepare a separate published-artifact/Platform qualification and adoption-disposition plan. This authorizes planning only; runtime remains held.

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

## Entry Requirements

Current [investigation](story-16-1-entry-investigation-2026-10-30.md): root 4.38.2 differs from Platform's prepared 4.38.3; adoption is six PASS/twenty-one FAIL. Both tooling versions fail SOURCE_DRIFT before native execution. Nine owner qualifications, trusted entry and compatible predecessors remain missing. Runtime entry stays BLOCKED; preserve draft/backlog.

## Open Questions

1. Current-tuple prerequisite scope — prepare a separate 4.38.3 candidate qualification/adoption/source-binding-successor plan (runtime held; approval before implementation or pin changes), or validate supplied accepted current tuple/owner records (provide their references; evaluate without inferring acceptance)? The October 30 prerequisite-first sequence is already selected.

## Code Map

Use the investigation's owner paths and reuse notes. EventStore owns atomic mutation/recovery; Tenants owns durable snapshots/transitions; Conversations consumes one snapshot. Existing implementation remains process-local. Preserve public signatures, accepted evidence and enrolled subscription configuration.

## Tasks & Acceptance

**After qualified entry only, in dependency order:**

- [ ] EventStore Client `Subscriptions/`, `Projections/` — add raw fingerprint binding, CAS outcomes, pending evidence, fenced recovery and durable acknowledgements.
- [ ] Tenants Client `Projections/`, `Handlers/TenantProjectionEventHandler.cs` — add coherent durable snapshots and pure transitions; preserve signatures.
- [ ] Conversations `Server/TenantAccess/` — add the snapshot adapter; evaluate one persisted snapshot per invocation; unsafe state denies.
- [ ] Owner Client tests, `Server.Tests/TenantAccess/`, `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` — prove the frozen fault matrix, persisted end-state, concurrency, restart and replica convergence.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading another replica, then persisted decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and owner commits, when six selected tooling-v2 scenarios run, then derive `6/6/0/0/0/0` with compatibility and exact rollback/gitlink scope.

## Implementation Notes

Earlier notes and planning are preserved verbatim in [history](story-16-1-planning-history-2026-10-30-before-tuple-refresh.md). This refresh runs no runtime task. Twelve current commands fail SOURCE_DRIFT; zero runtime measurements, seven absent outputs, null entry, empty trust and ACTIVE hold remain explicit.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

2026-10-26: replaced stale tooling tasks with completed prerequisite facts and remaining entry work; preserved frozen intent verbatim.

2026-10-27: reconciled committed remediation/CI changes and measured drift; preserved frozen intent and draft/backlog.

2026-10-28: refreshed pins, baseline and failure inventory; preserve accepted evidence, frozen intent and draft/backlog.

2026-10-28 post-tooling: refresh current scanner/source-binding facts and add the remaining scope question; KEEP frozen intent, completed prerequisite evidence and draft/backlog unchanged.

2026-10-28 split: regenerate this draft around runtime work; archive prior notes, retain the complete prior change log and record the human's prerequisite-planning choice. KEEP accepted evidence, runtime hold and draft/backlog.

2026-10-30: refreshed current tuple, exact adoption delta and source-binding failures; archived prior planning verbatim. KEEP frozen intent, accepted evidence, draft/backlog and runtime hold.

## Review Triage Log

## Verification

Current planning checks rehash captured streams, compare exact commands/adoption rows, verify protected state and run `git diff --check`. See the investigation and observation's `planning-verification.json`.

After entry: use individual mapped Debug owner/consumer builds/tests and the six exact tooling-v2 contract commands. Require native persisted-state proof and the derived summary; FAIL/BLOCKED halts. Historical evidence replay and runtime publication are not-applicable to this planning refresh.
