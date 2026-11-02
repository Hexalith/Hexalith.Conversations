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

**2026-10-30 current-tuple decision:** The user selected the recommended separate 4.38.3 qualification and binding-refresh plan. This authorizes preparation; implementation requires approval of that spec. Runtime entry remains held.

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

The [fresh 2026-10-31 qualification and held v3 attempt](../../module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/README.md)
passes the repaired complete mapped Debug baseline, exact signed 4.38.3 artifact
identity and all 36 controls. It records 29 adoption rows (7 PASS/22 FAIL),
Platform's recorded/prepared source inequality, and 55 unavailable declared
producer evidence files. The prerequisite remains incomplete because that
required evidence is BLOCKED. Explicitly selected tooling v3 refreshes source
binding while retaining null entry, empty trust and ACTIVE hold. It confers no
runtime readiness, acceptance or Story 16.1 implementation authority. This story
remains draft/backlog; all runtime tasks below remain untouched.
The [measured qualification ledger](../../module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/collection-measured/qualification-ledger.json) and
[30-command held-route ledger](../../module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/held-command-ledger.json) preserve distinct candidate and entry outcomes.
The full broad regression run timed out; focused current results do not claim
completion of that broader run.

The retained [entry investigation](story-16-1-entry-investigation-2026-10-30.md) measured root 4.38.2 versus Platform's prepared 4.38.3, six PASS/twenty-one FAIL adoption rows and SOURCE_DRIFT in both tooling versions. Those measurements precede the external dependency update; the separate prerequisite captures fresh execution inputs. Nine owner qualifications, trusted entry and compatible predecessors remain required. Runtime entry stays BLOCKED; preserve draft/backlog.

The selected [separate prerequisite](spec-epic-16-4-38-3-qualification-and-binding-refresh.md) owns current candidate measurement and held source-binding preparation. Its approval does not qualify runtime entry.

The prerequisite's [2026-10-31 baseline attempt](../../module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-01/README.md)
is BLOCKED before AppHost launch: source preparation exits 2/HXW009 because
Platform's recorded Builds object is unavailable in the local Builds database.
No collector/v3 implementation or 4.38.3 artifact/control measurement followed.
Resolve the owner source-identity/local-object prerequisite and retry the normal
mapped Debug baseline in a fresh attempt. Runtime entry, draft/backlog, null
entry, empty trust and ACTIVE hold remain unchanged.

## Code Map

The [2026-10-31 prerequisite resume](../../module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-02/README.md)
retrieved the exact recorded Builds commit from its canonical remote. Fresh
source preparation now passes. The mapped Debug AppHost baseline remains
BLOCKED: Admin Web failed to bind `http://127.0.0.1:5000` (process exit 134;
health-wait exit 18), although Conversations and the gateway became healthy.
Only the created instance was stopped. Resolve that hosting/environment blocker
and repeat the full baseline before prerequisite implementation; this supplies
no runtime entry qualification. Draft/backlog and the ACTIVE hold stay in force.

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

2026-10-30 decision: recorded the user's prerequisite-planning choice and removed the answered scope question. KEEP runtime tasks, draft/backlog and ACTIVE hold.

## Review Triage Log

## Verification

Current planning checks rehash captured streams, compare exact commands/adoption rows, verify protected state and run `git diff --check`. See the investigation and observation's `planning-verification.json`.

After entry: use individual mapped Debug owner/consumer builds/tests and the six exact tooling-v2 contract commands. Require native persisted-state proof and the derived summary; FAIL/BLOCKED halts. Historical evidence replay and runtime publication are not-applicable to this planning refresh.
