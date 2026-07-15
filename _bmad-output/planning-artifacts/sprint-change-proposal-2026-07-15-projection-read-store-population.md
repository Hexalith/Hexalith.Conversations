---
title: "Require production proof of projection read-store population"
date: 2026-07-15
status: proposed
change_scope: moderate
workflow: bmad-correct-course
mode: batch
decision: require-proof
approval: requested
reconciles_with: sprint-change-proposal-2026-07-15.md
---

# Sprint Change Proposal: Require Production Proof of Projection Read-Store Population

## 1. Issue Summary

The Conversations query read-store writer is implemented and registered, but no
production projection path invokes it. The legacy full-replay handler returns an opaque
gateway projection and explicitly leaves the separate query store unwired.

Epic 5 handled this honestly as an accepted residual risk for the signed v1 attestation.
That decision is scope-bound and states that population is not proven. Epic 6 now changes
the production host and must issue superseding v2 evidence, so continuing the deferral
would carry an unproven active read-path requirement into a new readiness claim.

### Trigger and evidence

- Story 2.4 created `ConversationProjectionReadModelWriter` over `IReadModelStore` and
  `ReadModelWritePolicy`.
- Story 2.5 created the legacy `ConversationProjectionHandler` but documented that it
  does not drive the query-side writer.
- Writer tests invoke the writer directly; composition tests prove only DI resolution.
- The Epic 5 retrospective closed the earlier action as an explicit deferral and opened
  action A3 for an architecture decision or production-path proof.
- The signed v1 release-owner decision accepted the named residual risk for its bound
  release evidence while expressly declining to represent population as proven.
- The current PRD and architecture require persisted, rebuildable projections and make
  unwaived projection-determinism gaps release blocking.

### Immediate control

Do not treat writer unit tests, handler response tests, DI resolution, or the July 14 v1
risk acceptance as current production-population proof. Preserve all signed v1 artifacts
and completed Epic 1-5 records byte-for-byte.

## 2. Impact Analysis

### PRD impact

No PRD change. This closes existing FR-5/FR-6 and Feature FR33-FR37 verification debt;
it does not add product scope.

### Epic and story impact

No new epic and no completed-story rewrite. Amend only the active Epic 6 authority
overlay:

| Story | Impact | Corrective disposition |
| --- | --- | --- |
| 6.1 | Decision registration | Register ADR 0003 as architecture authority once approved. |
| 6.2 | Direct implementation and proof | Wire the canonical named asynchronous projection path and produce state-store end-state evidence. |
| 6.6 | Release consumption | Validate the Story 6.2 proof and prohibit inherited v1 deferral from satisfying v2 readiness. |

Story 6.2 remains after 6.7 and before 6.5. Story 6.6 remains last.

### Artifact impact

| Artifact | Impact | Required change after approval |
| --- | --- | --- |
| Product PRD | None | Preserve current requirements and denominators. |
| ADR tracker | Direct | Accept ADR 0003 and link the approval evidence. |
| Architecture | Direct | Add the population ownership, proof boundary, and v1-deferral scope rule. |
| Epic plan | Direct | Extend Stories 6.2 and 6.6 acceptance criteria append-only inside the Epic 6 overlay. |
| Sprint status | Direct | Add a dated reconciliation note; keep the Epic 5 A3 action open until the proof decision is approved, then mark the decision action done while Story 6.2 tracks implementation. |
| Release evidence | New | Add versioned `projection-read-store-population-proof-v2.{json,md}` under Story 6.2. |
| Tests | Direct | Add production-composition integration and rebuild proof; retain existing focused tests. |
| UX | None | Existing freshness and trust behavior is preserved; no UI work is authorized. |
| Signed v1 evidence | None | Preserve byte-for-byte. |

### Technical impact

Conversations adds a scoped named `IAsyncDomainProjectionHandler` route that reuses the
existing materializer and writer. The handler reports completion only after both the
per-conversation model and tenant index are durable. The legacy synchronous handler may
remain for v1 protocol compatibility but is not the persisted-query-store path.

The proof crosses the production EventStore named-projection dispatch boundary and
asserts the configured state-store end state plus query results. It also proves duplicate,
retry-after-partial-write, tenant-isolation, failure, deletion, and replay behavior.

## 3. Change Navigation Checklist

### Section 1 — Trigger and context

- [x] Triggering issue identified: built-but-unwired projection read-store population.
- [x] Current behavior reproduced from code and test evidence.
- [x] Historical v1 deferral and its signed boundary identified.
- [x] Current Epic 6 readiness conflict identified.

### Section 2 — Epic impact

- [x] Active corrective epic remains viable.
- [x] No new epic is required.
- [x] Completed Epics 1-5 and signed evidence remain immutable.
- [x] Story 6.2 is the earliest safe implementation owner; Story 6.6 consumes proof last.

### Section 3 — Artifact conflict and impact

- [x] PRD remains valid; no scope or priority change.
- [x] Architecture requires an explicit decision and proof invariant.
- [x] Epic 6 acceptance criteria require targeted additions.
- [x] UX requires no change.
- [x] Integration verification must assert state-store end state.

### Section 4 — Path evaluation

- [x] Direct adjustment is viable and selected.
- [x] Rollback is unnecessary and would risk valid delivered behavior.
- [x] MVP reduction is unnecessary because the affected projection behavior remains active.
- [x] Continued generic deferral is rejected for v2 readiness.

### Section 5 — Proposal quality

- [x] Scope is moderate and implementation-ready after approval.
- [x] Ownership, sequencing, proof artifacts, and release gate are named.
- [x] Historical evidence preservation and non-goals are explicit.
- [x] Approval remains pending.

## 4. Recommended Approach

Select direct adjustment and require production proof.

This is a moderate correction within the approved major Epic 6 plan. It uses the platform
seam that already exists, keeps Conversations-specific projection rules in Conversations,
and creates the missing evidence before the final attestation.

### Alternatives considered

| Option | Decision | Reason |
| --- | --- | --- |
| Require production-path proof in Story 6.2 | Selected | Closes active requirements through the canonical platform boundary before v2 attestation. |
| Carry the v1 deferral into Epic 6 | Rejected | The signed decision is scope-bound and explicitly does not prove population. |
| Accept direct writer or DI tests as proof | Rejected | They bypass delivery and do not prove state-store end state. |
| Populate on query | Rejected | Creates read-side mutation and masks freshness/availability failures. |
| Add a separate new story | Rejected | Story 6.2 already owns platform-host migration and is the earliest production-composition point. |

### Estimate and risk

- **Effort:** Medium.
- **Risk:** Medium; the principal risk is correctly reconciling partial two-key writes.
- **Timeline impact:** Story 6.2 gains a focused implementation/proof slice before Story 6.5 and the final Story 6.6.
- **Mitigation:** use the existing idempotent full-replace/index-merge writer and prove retry convergence through the real dispatch boundary.

## 5. Detailed Change Proposals

### 5.1 ADR 0003

**Artifact:** `docs/adrs/0003-projection-read-store-population-proof.md`

Create the proposed ADR supplied with this change proposal. On approval:

- change `Status: Proposed` to `Status: Accepted`;
- record the approval reference and approver;
- change the ADR tracker row from `Proposed` to `Accepted`.

The selected decision is mandatory production-path proof. The signed v1 deferral remains
valid only for its immutable bound scope and cannot satisfy Epic 6 readiness.

### 5.2 Story 6.2 acceptance criteria

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Keep existing criteria 1-3 and append:

> 4. Conversations exposes a canonical named `IAsyncDomainProjectionHandler` route that
> reuses the existing materializer and persists both the tenant-scoped per-conversation
> summary/detail model and tenant index through `ConversationProjectionReadModelWriter`,
> `ReadModelWritePolicy`, and the configured `IReadModelStore`; completion is reported
> only after both writes are durable.
>
> 5. Versioned `projection-read-store-population-proof-v2` evidence demonstrates an
> accepted append or authorized replay crossing the production EventStore named-dispatch
> boundary into the Conversations handler, asserts the actual integration state-store
> end state and production query result, and does not call the writer directly.
>
> 6. Focused integration tests prove duplicate delivery, retry after partial write,
> tenant isolation, bounded failure outcomes, derived-state deletion, and full replay
> converge to an equivalent per-conversation record and duplicate-free tenant index.
> The legacy opaque projection response, DI resolution, mock calls, and HTTP acceptance
> alone are insufficient proof.

### 5.3 Story 6.6 acceptance criteria

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Keep existing criteria 1-3 and append:

> 4. The v2 attestation consumes and hash-validates the accepted ADR 0003 and Story 6.2
> `projection-read-store-population-proof-v2` artifacts, reruns their focused conformance
> and rebuild gates, and does not inherit the signed v1 projection-population deferral as
> proof or as a waiver for current readiness.

### 5.4 Architecture additions

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

Add ADR 0003 to the decision authority and record these invariants:

- EventStore owns ordered delivery, dispatch identity, and named-projection routing.
- Conversations owns materialization, tenant-scoped read-model keys, persistence policy
  consumption, freshness, and query semantics.
- The named asynchronous handler is the population owner for the persisted query store;
  the legacy synchronous handler is v1 compatibility only.
- Queries never materialize or backfill projection state.
- A durable completed outcome requires both per-conversation and tenant-index writes.
- Epic 6 readiness requires production dispatch, actual store end-state, query-read,
  retry/replay, isolation, failure, and rebuild proof.
- The July 14 v1 residual-risk acceptance is historical and cannot be inherited by v2.

### 5.5 Sprint tracking

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

After approval, add a dated reconciliation comment linking this proposal and ADR 0003.
Mark only the Epic 5 decision action A3 `done`; its done-when condition is then satisfied
by an accepted decision with a named proof boundary and owner. Keep Story 6.2 `backlog`
until normal story creation and implementation. Story 6.6 remains `backlog` and last.

### 5.6 Verification and evidence

**New artifacts under Story 6.2:**

- `docs/release-evidence/projection-read-store-population-proof-v2.json`
- `docs/release-evidence/projection-read-store-population-proof-v2.md`

The machine-readable record must bind source/build/test identity, named route, dispatch
identity, state-store adapter/component, exact tenant-scoped keys, before/after state,
query result, duplicate/retry/replay outcomes, negative tenant, and evidence hashes.

Tests must exercise production composition and read back the configured integration
store. Existing direct writer and DI tests remain useful unit/composition evidence but
cannot be relabeled as the production proof.

## 6. Implementation Handoff

### Scope classification

**Moderate.** No product, UX, or public-contract scope change. The active Epic 6 backlog,
architecture authority, server projection adapter, integration tests, and v2 evidence are
affected.

### Responsibilities

| Role | Responsibility |
| --- | --- |
| Administrator / Architect | Approve ADR 0003 and this correction. |
| Product Owner | Apply the Story 6.2/6.6 criteria and sprint reconciliation. |
| Developer | Implement the named handler path and versioned proof in Story 6.2. |
| Test Architect | Independently verify state-store, retry/replay, tenant, and rebuild evidence. |
| Release owner | Consume the proof in Story 6.6 and issue the v2 decision. |

### Sequence

1. Approve ADR 0003 and this proposal.
2. Apply the append-only Epic 6 and architecture edits; close retrospective decision A3.
3. Complete Story 6.7 and freeze the Story 6.2 pre-correction SM-C2 benchmark as already ordered.
4. Implement and verify projection read-store population within Story 6.2.
5. Continue Story 6.5 and the remaining corrective work.
6. Run Story 6.6 last and consume the accepted proof in the superseding v2 attestation.

### Success criteria

- No production query projection remains built-but-unwired.
- Both tenant-scoped store records are proven after real named dispatch.
- Duplicate, partial failure, retry, replay, isolation, and rebuild behavior converge safely.
- The v1 signed evidence remains byte-identical and honestly scoped.
- Story 6.6 and readiness cannot pass by citing the historical deferral.

## 7. Approval Request

**Recommendation:** approve the proposal and ADR 0003, selecting mandatory production
proof and rejecting continued generic deferral for Epic 6/v2 readiness.

No implementation, epic-plan, architecture-authority, sprint-status, or signed-evidence
change is authorized until explicit approval is recorded.
