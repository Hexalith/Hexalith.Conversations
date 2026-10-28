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

## Code Map

Use the [runtime source map](story-16-1-entry-investigation-2026-10-28.md#runtime-code-map-after-qualified-entry). The [separate prerequisite](spec-epic-16-published-artifact-platform-qualification.md) owns artifact/Platform qualification and adoption dispositions. Entry remains BLOCKED.

The [published 4.38.1 observation](../../module/validation/epic-16-published-artifact-qualification/parent-review-20261028/README.md)
now measures artifact/source/CI/catalog identities and all 36 malformed-key
controls successfully. Its overall qualification remains FAIL: the sixteen
adoption failures and Platform catalog identity disagreement remain explicit.
Missing declared producer qualification files are separately BLOCKED. Published package preparation
and action export are BLOCKED by the untagged direct Platform checkout; the
reviewed tuple, registration, native hosts, operational envelope, provider parity
and human acceptance remain pending. No runtime task is qualified to start.

## Tasks & Acceptance

**After qualified entry only:**

- [ ] EventStore Client `Subscriptions/`, `Projections/` — implement AD-6 raw binding, atomic mutation, pending evidence, fenced recovery and durable acknowledgement.
- [ ] Tenants Client `Projections/`, `Handlers/TenantProjectionEventHandler.cs` — implement coherent durable snapshots and pure transitions, preserving signatures.
- [ ] Conversations `Server/TenantAccess/` — add the snapshot adapter and consume one persisted snapshot per invocation; unsafe state denies.
- [ ] Owner Client tests, `Server.Tests/TenantAccess/`, `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` — prove the frozen matrix, persisted end-state, concurrency, restart and replica convergence.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading another replica, then persisted decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and owner commits, when six contracted scenarios run, then derive `6/6/0/0/0/0` with compatibility and exact rollback/gitlink scope.

## Implementation Notes

[Prior planning history](story-16-1-planning-history-2026-10-28-before-artifact-split.md) preserves all earlier notes verbatim. On 2026-10-28 the user selected the separate prerequisite plan; no runtime task started.

2026-10-28 artifact prerequisite implementation: retain the measured
[qualification ledger](../../module/validation/epic-16-published-artifact-qualification/attempt-20261028-implementation-01/collection-05/qualification-ledger.json)
and [held-command matrix](../../module/validation/epic-16-published-artifact-qualification/attempt-20261028-implementation-01/held-command-matrix-v2.json).
The fresh Debug AppHost readiness observation grants no runtime-entry credit.
V1 remains FAIL/SOURCE_DRIFT; v2 remains BLOCKED/ENTRY_QUALIFICATION_PENDING.
Entry is null, independent trust empty, all seven outputs absent and all four
authority flags false. This draft and its backlog/runtime tasks remain held.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

2026-10-26: replaced stale tooling tasks with completed prerequisite facts and remaining entry work; preserved frozen intent verbatim.

2026-10-27: reconciled committed remediation/CI changes and measured drift; preserved frozen intent and draft/backlog.

2026-10-28: refreshed pins, baseline and failure inventory; preserve accepted evidence, frozen intent and draft/backlog.

2026-10-28 post-tooling: refresh current scanner/source-binding facts and add the remaining scope question; KEEP frozen intent, completed prerequisite evidence and draft/backlog unchanged.

2026-10-28 split: regenerate this draft around runtime work; archive prior notes, retain the complete prior change log and record the human's prerequisite-planning choice. KEEP accepted evidence, runtime hold and draft/backlog.

## Review Triage Log

## Verification

After entry, run individual Debug owner/consumer tests and six tooling-v2 commands. Require native persisted-state evidence and the exact final summary. Planning checks preserve intent except the user decision, bound sources and repository state.
