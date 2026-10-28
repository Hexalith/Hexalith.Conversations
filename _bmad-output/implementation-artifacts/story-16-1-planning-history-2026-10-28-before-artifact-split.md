# Story 16.1 planning history before the artifact qualification split

Historical snapshot retained when the user selected the separate prerequisite plan
on 2026-10-28. The embedded draft and open question below are historical; the
active runtime draft and separate qualification spec govern subsequent work.
The source draft text below is preserved verbatim.

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

**2026-10-27 follow-up decision:** The user selected option 1: prepare a separate tooling-qualification and source-binding-refresh plan. This authorizes planning only; runtime entry remains held.

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

1. With the tooling prerequisite complete and runtime entry still unqualified, which next scope should proceed? Prepare a separate published-artifact/Platform tuple qualification and adoption-disposition plan (keeps runtime held), or validate existing qualified owner records supplied by the user (checks evidence without granting authority). Neither choice alone lifts the hold. This draft exceeds the preferred token range; the separate-plan route also separates the remaining prerequisite detail, while retaining the full draft requires accepting that length.

## Code Map

[Post-tooling investigation](story-16-1-entry-investigation-post-tooling-2026-10-28.md): current candidate declaration PASS, adoption six PASS/sixteen FAIL. All 81 v2 source hashes match; six v2 commands reach BLOCKED/ENTRY_QUALIFICATION_PENDING. V1 retains seven source mismatches and six FAIL/SOURCE_DRIFT results. Entry remains null, trust empty. The earlier investigation's twelve-failure inventory predates the completed prerequisite.

The [approved prerequisite](spec-epic-16-tooling-qualification-and-binding-refresh.md) now supplies additive tooling-v2 with exact package selection. [Fresh evidence](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261028-implementation-03/) records source preparation/readiness and held commands: v1 FAIL/SOURCE_DRIFT; v2 BLOCKED/ENTRY_QUALIFICATION_PENDING. Both routes measure zero runtime tests and leave all seven outputs absent. Runtime entry remains null, independent trust empty and the hold ACTIVE.

## Tasks & Acceptance

**Prerequisites; runtime remains held:**

- [x] `module/`, planning `epic-16-adoption-v2/`, successor tooling — preserve completed enrollment preparation.
- [ ] Platform/root `.config/dotnet-tools.json` — qualify a published release/source/catalog tuple before alignment; disposition all sixteen current adoption failures, including four newly scanned retained imports.
- [x] Planning `epic-16-tooling-v2/`, shared consumers — implement the approved additive refresh; preserve v1 and its source-drift checks.
- [ ] Planning `production-operational-envelope-v1.md`, `module/sm-2-checklist.md` — qualify operations, parity, human SM-2 and trusted entry.

**After qualified entry; upstream paths belong to their root-declared repositories:**

- [ ] EventStore Client `Subscriptions/`, `Projections/` — raw binding before deduplication, atomic mutation, pending evidence, fenced recovery and durable acknowledgements.
- [ ] Tenants Client `Projections/`, handler/registration — durable coherent snapshots and pure transitions; preserve signatures.
- [ ] Conversations `Server/TenantAccess/ConversationTenantProjectionSnapshotAdapter.cs`, access/signal — one persisted snapshot and fail-closed health. Preserve configured delivery and Program.cs disclosure until proofs pass.
- [ ] Owner Client tests, Conversations `Server.Tests/TenantAccess/`, new `IntegrationTests/Epic16/Story16_1EvidenceTests.cs` — frozen matrix/concurrency/recovery/provider faults, persisted end-state/acknowledgements and native loaded-product capture.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading another replica, then persisted decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and owner commits, when six scenarios run, then generate `6/6/0/0/0/0` with compatibility, safe diagnostics and exact gitlink/rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

2026-10-23: three owner repositories plus enrollment/evidence affected; no runtime changes. Approved prerequisite preparation is tracked separately in `spec-epic-16-adoption-preparation.md`; this runtime story remains draft/backlog.

2026-10-26: prerequisite implementation is done. Refreshed planning and retained ten command observations; no runtime implementation, dependency update or lifecycle transition. Entry route remains unresolved.

2026-10-27: the user selected the separate remediation-planning route. See [the new draft](spec-epic-16-enrollment-adoption-remediation.md); implementation and runtime entry remain unapproved.

2026-10-27 later investigation: the separate remediation is now done; producer preparation is present. Fresh commands fail source binding before entry. Runtime remains held.

2026-10-28: retained baseline readiness passed; current declaration validates, adoption and source binding still fail. Runtime entry remains held.

2026-10-28 scope decision: The user selected option 1: resume the approved tooling prerequisite. Runtime implementation remains held.

2026-10-28 post-tooling resumption: the prerequisite is done. Fresh committed-candidate diagnostics confirm the working held v2 route and expanded scanner inventory. No qualified owner entry exists; the next prerequisite scope is an open question, not an inferred approval.

## Spec Change Log

2026-10-25: refreshed investigation; frozen intent unchanged. History remains in `spec-epic-16-adoption-v2-preparation.md`.

2026-10-26: replaced stale tooling tasks with completed prerequisite facts and remaining entry work; preserved frozen intent verbatim.

2026-10-27: reconciled committed remediation/CI changes and measured drift; preserved frozen intent and draft/backlog.

2026-10-28: refreshed pins, baseline and failure inventory; preserve accepted evidence, frozen intent and draft/backlog.

2026-10-28 post-tooling: refresh current scanner/source-binding facts and add the remaining scope question; KEEP frozen intent, completed prerequisite evidence and draft/backlog unchanged.

## Review Triage Log

## Verification

2026-10-28 prerequisite execution: mapped Debug readiness and cleanup PASS; 525 producer-source and nine action tests PASS. Current Python verification PASS: 60 focused cases and 4,694 affected cases, using the complete source environment and existing retirement policy. Explicit v2 held commands reach entry BLOCKED; v1 retains source FAIL. Published artifact/Platform/native-host/operational/human owner qualification remains BLOCKED; no runtime lifecycle advancement.

[Fresh measured results](story-16-1-entry-investigation-post-tooling-2026-10-28.md#verification): declaration PASS; adoption six PASS/sixteen FAIL; v1 six FAIL/SOURCE_DRIFT; v2 six BLOCKED/ENTRY_QUALIFICATION_PENDING. Zero runtime tests, seven absent outputs; runtime publication not-applicable. Bound hashes, nonempty ledgers, stream integrity and preserved repository/dependency state checked.

After entry: individual Debug builds, xUnit and six successor commands; missing runtime results cannot pass.
