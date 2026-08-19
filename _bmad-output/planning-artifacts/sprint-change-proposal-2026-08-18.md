---
project: Conversations
date: 2026-08-18
trigger: "Implementation-readiness FAIL: Epic 6 actions A4-A6 have owners but no approved successor stories"
mode: batch
scope: moderate
status: approved
implementation_hold: ACTIVE
---

# Sprint Change Proposal — Map Epic 6 A4–A6 to Approved Successor Work

- **Author:** Dev workflow (`bmad-correct-course`), for Jerome
- **Trigger:** `_bmad-output/planning-artifacts/implementation-readiness.md` finding 3
- **Baseline:** root `HEAD` `29c56fa0b587636c00c72d44ebfc24b3cde35e34`; existing working-tree changes are preserved and are not part of this proposal
- **Mode:** Batch, continuing Jerome's same-day Correct Course preference
- **Scope classification:** **Moderate** — backlog and execution-graph reorganization with architecture coordination; no PRD, MVP, or UX-scope change
- **Status:** **APPROVED** by Jerome (release owner) on 2026-08-19. Approval authorizes CP-1–CP-6 as planning/backlog authority only and does not lift the implementation hold or authorize Story 16 implementation.
- **Implementation hold:** **ACTIVE — unchanged**
- **Lifecycle effect now:** none; this document does not regenerate `sprint-status.yaml`

## 1. Issue Summary

The 2026-08-18 implementation-readiness gate found that three open Epic 6 retrospective obligations have named owners and completion evidence, but no executable successor-story contracts:

| Action | Owner | Required outcome |
| --- | --- | --- |
| `A4` / `epic-6-retro-item-27-create-approved-successor-work-for-a-dur` | Architect / Runtime owner | Durable event-fed tenant access with freshness and gap detection; restart and two-replica convergence before removing the single-replica constraint. |
| `A5` / `epic-6-retro-item-28-create-approved-successor-work-for-deter` | Projection owner | Deterministic event-derived replay timestamps and truthful missing-index semantics, including exact replay and derived-state-deletion proof. |
| `A6` / `epic-6-retro-item-29-add-explicit-preflight-diagnostics-for-a` | Test / AppHost owner | Explicit endpoint-readiness and Dapr control-plane port-collision diagnostics plus live terminal reconciliation through `project/v2/reconcile`. |

The authoritative V12 overlay deliberately preserved these obligations outside `E6-REMEDIATION` and stated that it created no substitute successor story. That was correct for the pre-IR-0 checkpoint, but it leaves A4–A6 untraceable into the executable plan. Their implementation cannot be inferred from existing Stories 7.1–15.2.

### 1.1 Evidence

| ID | Evidence |
| --- | --- |
| `E-1` | `epics.md:4318-4333` preserves A4–A6 as open and requires separately approved successors. |
| `E-2` | `architecture.md:2348-2356` repeats that none of A4–A6 is authorized by E6-REMEDIATION. |
| `E-3` | `epic-6-retro-2026-08-03.md:134-136` defines the owners and measurable done conditions. |
| `E-4` | The retrospective records the concrete defects: in-memory per-replica tenant access, non-deterministic replay timestamps, falsely `Current` missing indexes, endpoint readiness races, fixed-port collisions, and direct-handler-only reconciliation tests. |
| `E-5` | `sprint-status.yaml` keeps all three action rows `open` and contains no story entry that names or closes them. |
| `E-6` | Existing Stories 12.1–12.4 and 13.1–13.3 prove performance and projection-evidence lifecycles, but do not authorize the runtime corrections required by A4–A6. |

### 1.2 Additional authority-chain discrepancy surfaced

The current architecture V13 marker names `epic-6-authority-2026-08-18-v13`, but `epics.md` ends at the V12 epic overlay. Application must not fabricate a historical V13 epic block. The new append-only authority will explicitly record that unresolved forward reference and supersede the last actually published epic authority, V12, while architecture V14 supersedes architecture V13 and binds the new epic authority.

## 2. Impact Analysis

### Epic impact

Add **Epic 16: Operational Projection Correctness and Recovery** with exactly three successor stories. The numeric ID is an append-only identity, not execution order. The effective dependency graph places Epic 16 after Story 7.4 and before the correctness-dependent portions of Epics 12, 13, 14, and 15.

Epics 1–6 and all completed records remain immutable. Existing Stories 7.1–15.2 retain their identifiers and acceptance criteria except for explicit predecessor additions described below.

### Story impact

Add:

1. Story 16.1 — durable tenant-access projection and multi-replica convergence (`A4`).
2. Story 16.2 — deterministic replay time and truthful missing-index semantics (`A5`).
3. Story 16.3 — AppHost diagnostics and live terminal reconciliation (`A6`).

A4–A6 remain `open` after planning publication. Each action closes only when its mapped story has a compatible passing final record; approval or backlog insertion alone is not completion.

### Artifact conflicts and changes

| Artifact | Current conflict | Proposed action after approval |
| --- | --- | --- |
| PRD | None; requirements already preserve tenant freshness, deterministic replay, truthful degraded state, and operational diagnostics | No change. |
| `epics.md` | A4–A6 are intentionally unmapped; latest actual epic overlay is V12 | Append V14 successor-authority overlay; do not edit frozen V1–V12 bytes. |
| `architecture.md` | V13 preserves A4–A6 as unowned successor implementation and references an unpublished epic V13 | Append architecture V14 with landing zones, graph, invariants, and the authority-reference correction. |
| V9 story contracts / graph / generated execution view | Exactly 27 successors and no Epic 16 | Regenerate through the deterministic publisher to 30 successor stories and the amended graph. |
| `sprint-status.yaml` | No Epic 16 keys; A4–A6 remain open | After approval/publication, add Epic 16 and three `backlog` stories; keep A4–A6 `open`. |
| UX spec / UX requirement map | No scope conflict | Preserve unchanged; runtime state must continue using the existing safe freshness/degraded vocabulary. |
| Product source / tests | Defects are documented but implementation is not authorized under the active hold | No change in Correct Course. Route later to `bmad-build` only after the hold is explicitly lifted. |

### Technical impact

- `A4` crosses Conversations and the tenant-domain client boundary. The preferred landing zone is an additive durable `ITenantProjectionStore` capability in `Hexalith.Tenants.Client`, consumed/configured by Conversations. Conversations must not duplicate generic Dapr-state projection plumbing.
- `A5` changes Conversations projection materialization/read-store semantics without changing public contract shape. Query reads remain side-effect free and must never repair durable state.
- `A6` changes test/AppHost preflight and live integration coverage. It consumes the production reconciliation route rather than calling the handler directly.
- No nested submodule initialization or recursive update is authorized. Any later Tenants promotion requires its own submodule commit, exact root gitlink binding, and the normal promotion/evidence gates.

### UX impact

No new screen or interaction is introduced. A4 and A5 strengthen the truth behind existing `Current`, `Stale`, `Rebuilding`, `Unavailable`, and fail-closed tenant states. Tests must prove the UI/API trust vocabulary is not weakened or silently defaulted to `Current`.

## 3. Recommended Approach

**Option 1 — Direct adjustment: selected.** Add one bounded epic and three stories, preserving all existing requirement IDs and completed history. Effort **Medium–High**; planning risk **Low**; implementation risk **Medium** because A4 includes durable distributed state and multi-replica proof.

**Option 2 — Rollback: rejected.** Reverting Story 6.2 or the V12 checkpoint would discard valid platform-hosting and remediation work without supplying the missing runtime guarantees.

**Option 3 — PRD/MVP review: not required.** A4–A6 implement already-preserved requirements and retrospective obligations; they do not change the product goal or activate new UX scope.

### Sequence rationale

Story 16.1 establishes the durable tenant lifecycle/freshness checkpoint needed to distinguish initialized empty state from unavailable state. Story 16.2 consumes that fact to make missing-index semantics trustworthy. Story 16.3 then proves the corrected runtime through the production terminal route. Performance and projection-proof stories must consume this corrected runtime rather than benchmark or attest the known-defective state.

## 4. Detailed Change Proposals

### CP-1 — Append Epic 16 successor authority to `epics.md`

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

**OLD (effective V12 disposition):**

```text
V12 assigns no product implementation and creates no substitute successor story.
A4-A6 remain open under separately approved successor authority.
```

**NEW (append-only V14 override; V12 bytes remain unchanged):**

```text
Epic authority: epic-6-authority-2026-08-18-v14
Architecture authority: conversations-architecture-2026-08-18-v14
Supersedes: epic-6-authority-2026-08-04-v12 only for A4-A6 successor ownership,
effective successor count, and the enumerated graph/predecessor additions.
Hold: ACTIVE.

A4 -> Story 16.1
A5 -> Story 16.2
A6 -> Story 16.3

Approval creates executable contracts but does not start them. Global successor execution
still requires valid IR-0 READY plus an explicit release-owner LIFTED decision.
```

The overlay must record that architecture V13's `epic-6-authority-2026-08-18-v13` was an unpublished forward reference, not a published authority to be reconstructed or silently treated as current.

### CP-2 — Add Epic 16 and its three canonical story contracts

#### Epic 16: Operational Projection Correctness and Recovery

**Outcome:** Operators and authorized callers receive durable tenant authorization, deterministic replay-derived projection truth, and diagnosable/recoverable live reconciliation across restart and multi-replica execution.

**Hard entry:** Story 7.4 complete; independent IR-0 `READY`; explicit release-owner hold decision `LIFTED` for successor execution.

**Bounded exit:** Stories 16.1–16.3 are `done` at compatible candidates; A4–A6 action rows transition to `done` only from their respective final records.

**No new requirement:** This epic implements existing `Feature-NFR16`–`Feature-NFR18`, `Feature-NFR22`–`Feature-NFR27`, `Feature-NFR38`–`Feature-NFR48`, and the preserved architecture/runtime invariants. It does not activate a new FR or UX decision.

#### Story 16.1: Persist tenant-access projection state and prove convergence

As an Architect / Runtime owner,
I want tenant-access state durably projected with explicit freshness and sequence/gap semantics,
so an acknowledged tenant event remains authoritative across restart and multiple replicas.

**Maps:** Epic 6 `A4` / action item 27.

**Exact predecessors:** `7.4`, `IR-0`, and the explicit hold-lift decision.

**Frozen inventory:** `V14-16.1-ENTRY-v1`, in order:

```text
E6-A4-DURABLE-TENANT-ACCESS
E6-A4-FRESHNESS-GAP-DETECTION
E6-A4-RESTART-CONVERGENCE
E6-A4-MULTI-REPLICA-CONVERGENCE
E6-A4-SINGLE-REPLICA-WARNING-REMOVAL-GATE
```

SHA-256: `eeb9eee87de7bc646cdf09acaf3f6e65351c71472a55f2d8b65de2e12b44511f`.

**Bounded outcome:** an additive tenant-domain durable-store capability, Conversations registration, explicit freshness/sequence/gap contract, restart and two-replica proof, and removal of the single-replica warning only after those proofs pass.

**Landing zone:** additive `Hexalith.Tenants.Client` capability behind `ITenantProjectionStore`; Conversations supplies only configuration and its access adapter. If the Tenants owner rejects this landing zone, implementation halts for a revised architecture decision rather than placing generic Dapr state plumbing in Conversations.

**Rollback boundary:** revert only the new durable provider/configuration, Conversations adoption, tests/evidence, Tenants promotion commit, and root gitlink update. Preserve tenant events, public Conversations contracts, completed records, other gitlinks, and all accepted evidence.

**Acceptance scenarios:**

| ID | Required proof | Pass condition |
| --- | --- | --- |
| `AC-16.1-01` | Closed architecture/contract test for storage key scope, freshness, monotonic sequence, duplicate handling, gap/regression detection, and fail-closed reads | Nonzero assertions; no unknown/stale/gapped state authorizes access. |
| `AC-16.1-02` | Durable integration lane applies tenant events, restarts the Conversations process without event redelivery, and rechecks access | Previously authorized and denied states reload identically; freshness/sequence metadata is unchanged. |
| `AC-16.1-03` | Two Conversations replicas share the durable projection while Dapr delivers each event to only one consumer-group member | Both replicas converge to the same access decision and sequence; no replica remains permanently empty. |
| `AC-16.1-04` | Duplicate, out-of-order, missing-sequence, store-unavailable, corrupt-record, and tenant-disable faults | Duplicate is idempotent; gap/regression/corruption/unavailability is visible and fail-closed with stable safe diagnostics. |
| `AC-16.1-05` | Tenants and Conversations focused projects build/test; public contract and tenant-safety conformance diff | Zero failed/skipped/not-run; additive upstream surface; no personal tenant data copied into Conversations events or logs. |
| `AC-16.1-06` | Canonical story-record generation | Final record summary `6/6/0/0/0/0`, exact root/Tenants gitlinks, inputs, outputs, fault ledger, and rollback binding. |

**Final record:** `docs/release-evidence/story-16.1-final-record-v2.json` plus deterministic Markdown.

#### Story 16.2: Make replay time deterministic and missing-index state truthful

As a Projection owner,
I want projection timestamps derived from immutable event inputs and empty-index state backed by an explicit lifecycle fact,
so identical history replays identically and deleted derived state cannot masquerade as a current empty tenant.

**Maps:** Epic 6 `A5` / action item 28.

**Exact predecessor:** `16.1`.

**Frozen inventory:** `V14-16.2-ENTRY-v1`, in order:

```text
E6-A5-EVENT-DERIVED-TIMESTAMPS
E6-A5-EXACT-REPLAY
E6-A5-MISSING-INDEX-SEMANTICS
E6-A5-DERIVED-STATE-DELETION
```

SHA-256: `64403ee626140f90094caf804a1d0e1d98475054e6627cf9c5b282f32b6c5abe`.

**Bounded outcome:** deterministic domain/freshness timestamps, no wall-clock fallback in replay, a durable event-fed initialization/watermark fact, and read semantics that distinguish initialized-empty from missing/erased/unavailable state without writes from a query path.

**Required architecture rule:** event time, ordering metadata, fallback behavior, and the initialization/watermark lifecycle are approved before code enters review. Missing required event time fails or degrades with a typed state; it never substitutes `UtcNow`/`Now`/`TimeProvider.GetUtcNow()` during replay.

**Rollback boundary:** revert only the timestamp derivation, projection-lifecycle/watermark addition, read semantics, tests/evidence, and Story 16.2 record. Preserve event history, public contract shape, Story 16.1 durable tenant state, and prior proof history.

**Acceptance scenarios:**

| ID | Required proof | Pass condition |
| --- | --- | --- |
| `AC-16.2-01` | Materializer unit lane over fixed histories, including participant/file evidence timestamps and events with absent/invalid time | All persisted/projected time is event-derived and deterministic; invalid input produces the approved typed failure/degraded state. |
| `AC-16.2-02` | Two clean rebuilds from byte-identical ordered history | Projection JSON and every domain/freshness timestamp are byte-identical; runtime clock changes do not affect output. |
| `AC-16.2-03` | Never-used tenant, initialized-empty tenant, missing index with surviving details, and erased derived-state fixtures | Only proven initialized-empty is `Current` with zero rows; missing/erased/ambiguous state is `Rebuilding` or `Unavailable`, never authoritative empty `Current`. |
| `AC-16.2-04` | Query-side-effect guard and state-store write ledger | Reads perform zero repair writes; initialization/watermark changes occur only in the event-fed projection path. |
| `AC-16.2-05` | Public query/freshness, redaction replay, tenant-isolation, and projection-rebuild conformance | Zero shape drift and zero failed/skipped/not-run; existing safe UX/API vocabulary remains authoritative. |
| `AC-16.2-06` | Canonical story-record generation | Final record summary `6/6/0/0/0/0` with exact replay hashes, deletion faults, candidate/gitlinks, and rollback binding. |

**Final record:** `docs/release-evidence/story-16.2-final-record-v2.json` plus deterministic Markdown.

#### Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

As a Test / AppHost owner,
I want preflight failures classified before the live lane starts and terminal reconciliation exercised through the production route,
so environment faults are actionable and durable pending projection work is proven recoverable end to end.

**Maps:** Epic 6 `A6` / action item 29.

**Exact predecessor:** `16.2`.

**Frozen inventory:** `V14-16.3-ENTRY-v1`, in order:

```text
E6-A6-ENDPOINT-READINESS-DIAGNOSTIC
E6-A6-DAPR-PORT-COLLISION-DIAGNOSTIC
E6-A6-LIVE-TERMINAL-RECONCILIATION
E6-A6-DURABLE-RETRY-CLEARANCE
```

SHA-256: `0a9c0dc63e95a76f7b6cc2a089fd866296965a09f64feed4ada6fe872e367458`.

**Bounded outcome:** stable preflight classifications for endpoint-connect readiness and effective Dapr control-plane port collisions, plus a live AppHost lane that induces a projection failure, observes durable pending work, invokes `project/v2/reconcile` through the production coordinator/route, and proves terminal clearance and query visibility.

**Rollback boundary:** remove only new preflight diagnostics, AppHost fixtures/tests/results, route-coverage assertions, and the Story 16.3 record. Preserve the production route, platform hosting ownership, Stories 16.1/16.2, public contracts, and unrelated AppHost behavior.

**Acceptance scenarios:**

| ID | Required proof | Pass condition |
| --- | --- | --- |
| `AC-16.3-01` | Endpoint is resource-healthy but not connect-ready | Preflight fails before the test lane with stable `APPHOST_ENDPOINT_NOT_READY`, endpoint identity, bounded retry advice, and no content/tenant leakage. |
| `AC-16.3-02` | One effective Dapr control-plane port is occupied | Preflight reports stable `DAPR_CONTROL_PLANE_PORT_COLLISION` and the owning endpoint/port; unrelated port use does not false-positive. |
| `AC-16.3-03` | Live AppHost with a controlled first projection failure | Durable pending work is observable; direct handler invocation is forbidden by the fixture. |
| `AC-16.3-04` | Production `project/v2/reconcile` request through the EventStore coordinator/module route | Retry reaches Conversations, clears the durable terminal item exactly once, and the corrected projection becomes query-visible. |
| `AC-16.3-05` | Route removal/fingerprint drift, retry duplication, endpoint race, port collision, unavailable Dapr, and vacuous-lane faults | Each produces its stable blocker; no skip/not-run state passes; fixtures and occupied ports restore. |
| `AC-16.3-06` | Canonical story-record generation | Final record summary `6/6/0/0/0/0` with AppHost/runtime identities, route proof, diagnostics, candidate/gitlinks, and rollback binding. |

**Final record:** `docs/release-evidence/story-16.3-final-record-v2.json` plus deterministic Markdown.

### CP-3 — Amend the effective dependency graph

**OLD:** no A4–A6 successor nodes; Epics 12 and 13 can run without these runtime corrections; Story 15.1 has eight predecessor records.

**NEW graph deltas:**

```text
IR-0 -> 16.1
7.4 -> 16.1
16.1 -> 16.2
16.2 -> 16.3
16.3 -> 12.1
16.3 -> 13.1
16.3 -> 14.1
16.3 -> 15.1
```

The V9 numeric-order sentence is superseded only for Epic 16: append-only numbering must not imply execution after release attestation. The graph remains acyclic. Story 15.1's predecessor set gains Story 16.3 explicitly so release revalidation cannot omit A4–A6 even if an intermediate dependency is later refactored.

### CP-4 — Append architecture V14

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**OLD:** architecture V13 preserves V12's statement that A4–A6 need separately approved successor authority and names an unpublished epic V13.

**NEW:** append `conversations-architecture-2026-08-18-v14`, binding `epic-6-authority-2026-08-18-v14`, with these normative decisions:

1. Story 16.1 owns tenant-domain durable access projection infrastructure in the Tenants client capability; Conversations configures/consumes it.
2. Tenant access remains local on the request hot path and fail-closed for missing, stale, gapped, corrupt, or unavailable state.
3. Story 16.2 derives replay-visible time from immutable event inputs and uses an event-fed lifecycle/watermark fact; reads never repair state.
4. Story 16.3 owns diagnostic and live-route proof, not a second reconciliation implementation.
5. Epic 16 is downstream of the hold gate and upstream of performance, current projection proof, preservation closure, and release revalidation.
6. The unpublished epic V13 forward reference is recorded as invalid provenance and is not reconstructed.

### CP-5 — Regenerate tracking only after approval and authority publication

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:** no Epic 16 entries; A4–A6 open.

**NEW:** deterministic generation adds:

```yaml
  epic-16: backlog
  16-1-persist-tenant-access-projection-state-and-prove-convergence: backlog
  16-2-make-replay-time-deterministic-and-missing-index-state-truthful: backlog
  16-3-diagnose-apphost-preflight-failures-and-prove-terminal-reconciliation-live: backlog
  epic-16-retrospective: optional
```

The three Epic 6 action rows remain `open` and gain only an execution reference when the format permits:

```text
A4 -> Story 16.1 final record
A5 -> Story 16.2 final record
A6 -> Story 16.3 final record
```

No story becomes `ready-for-dev` or `in-progress`; no hold value changes. The existing duplicate Story 13.1 key is outside this proposal and must be reported by validation rather than silently repaired here.

### CP-6 — Deterministic publication and validation

After approval, the implementation handoff must update the publisher/schema tests and regenerate the complete managed set atomically. Required validations include:

- exact 30-story successor inventory and graph parity;
- exact A4/A5/A6-to-16.1/16.2/16.3 mapping;
- story-contract schema validity, inventory digests, candidate binding, and canonical final-record paths;
- architecture V14 / epic V14 cross-pointer equality;
- unchanged V1–V12 epic bytes and unchanged V1–V13 architecture bytes;
- `implementationHold: ACTIVE`, no IR-0 result, no hold-lift record, and all new stories `backlog`;
- zero skipped/not-run tests and nonempty assertion ledgers;
- no product implementation, dependency update, submodule content, or gitlink change during planning publication.

## 5. Implementation Handoff

**Scope:** Moderate.

| Sequence | Work | Owner / tool | Completion condition |
| ---: | --- | --- | --- |
| 1 | Approve, reject, or revise this proposal | Jerome, release owner | Explicit decision recorded in this file. |
| 2 | Publish CP-1–CP-6 as planning authority only | Product owner + Architect + Dev workflow / `bmad-build` | V14 overlays, contracts, graph, generated companions, and sprint projection validate; hold remains active. |
| 3 | Complete the already-approved F-10 repair and A2/A3 closure | Dev workflow + Architecture/Quality / `bmad-build` | Approved AC-10/AC-11 handoff and A2/A3 lifecycle gates pass with zero skips. |
| 4 | Rerun implementation readiness | `bmad-sprint-planning` readiness intent | Gate reports the remaining exact hold/IR-0 decision state; tracking is refreshed only on PASS. |
| 5 | Run independent IR-0 and obtain the separate hold decision | Independent assessor + Jerome | Candidate-matched `READY` and explicit `LIFTED`, or successors remain non-executable. |
| 6 | Execute Stories 16.1–16.3 in graph order | Named story owners / `bmad-build` | Each canonical final record passes; mapped A4–A6 row closes only then. |
| 7 | Continue Epics 12–15 | Existing owners | They consume Story 16.3 and cannot attest the prior known-defective runtime. |

### Success criteria for this course correction

1. A4–A6 each map exactly once to an approved canonical successor story.
2. Owners, bounded outcomes, predecessors, rollback boundaries, evidence, and stable fault semantics are explicit.
3. The effective graph is acyclic and places runtime correctness before performance/projection/release proof.
4. PRD and UX scope remain unchanged.
5. Planning publication changes no product source, dependencies, submodule content, gitlinks, lifecycle status, hold state, or action-item status.
6. Successor execution remains prohibited until IR-0 and the separate release-owner hold decision both permit it.

## 6. Explicit Non-Authorizations

This proposal does not:

- lift or weaken the global implementation hold;
- authorize or perform IR-0;
- close A2, A3, A4, A5, or A6;
- start Epic 16 or any Epic 7–15 story;
- mark any new story `ready-for-dev`, `in-progress`, `review`, or `done`;
- authorize release or create a hold-lift/release decision;
- implement tenant storage, projection semantics, AppHost diagnostics, or reconciliation;
- modify PRD requirements or activate UX scope;
- rewrite completed stories, frozen overlay bytes, accepted evidence, or published Git history;
- modify dependencies, submodule content, nested submodules, or gitlinks;
- include or revert the existing user-owned working-tree changes.

## 7. Change Navigation Checklist Record

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story/context | [x] Done | Epic 6 retrospective A4–A6, exposed as blocking finding 3 by readiness. |
| 1.2 Core problem | [x] Done | Required runtime work has owners/done conditions but no executable successor authority. |
| 1.3 Evidence | [x] Done | V12 overlays, retrospective, sprint ledger, current code/test findings, and readiness report agree. |
| 2.1 Current epic completable | [x] Done | Epic 6 remains immutable/done historically; successors belong outside it. |
| 2.2 Epic-level changes | [!] Action-needed | Append Epic 16 authority after approval. |
| 2.3 Remaining epics | [x] Done | Epics 12–15 must consume corrected runtime evidence. |
| 2.4 New epic needed | [x] Done | One bounded Epic 16 avoids reopening Epic 6 or diluting existing epic outcomes. |
| 2.5 Order/priority | [x] Done | Story 7.4 → Epic 16 → Epics 12/13/14/15 correctness-dependent gates. |
| 3.1 PRD conflicts | [N/A] | Existing preserved requirements already govern the work. |
| 3.2 Architecture conflicts | [!] Action-needed | V14 must define landing zones, time/watermark invariants, graph, and cross-pointer correction. |
| 3.3 UX conflicts | [N/A] | No UX activation; preserve trust-state vocabulary and safe rendering. |
| 3.4 Other artifacts | [!] Action-needed | Publisher, graph, story contracts, generated view, sprint status, schema/tests after approval. |
| 4.1 Direct adjustment | [x] Viable | Selected; Medium–High implementation effort, Medium implementation risk. |
| 4.2 Rollback | [x] Not viable | Removes valid work and does not create successors. |
| 4.3 MVP review | [x] Not viable | No product-goal or MVP change. |
| 4.4 Recommended path | [x] Done | Append-only Epic 16 with three mapped stories. |
| 5.1–5.5 Proposal components | [x] Done | Sections 1–5. |
| 6.1 Checklist completion | [x] Done | Applicable analysis is recorded here. |
| 6.2 Proposal accuracy | [x] Done | Mappings preserve exact retrospective owners and done conditions. |
| 6.3 User approval | [x] Done | Approved by Jerome on 2026-08-19; planning/backlog authority only, with the hold unchanged. |
| 6.4 Sprint status update | [!] Action-needed | Only after approval and deterministic authority publication. |
| 6.5 Next steps/handoff | [x] Done | Section 5. |

## 8. Approval Record

**Decision:** APPROVED by Jerome (release owner) on 2026-08-19.

Approval means: authorize CP-1–CP-6 as planning/backlog authority while keeping the global implementation hold active. It does **not** authorize Story 16 implementation or lift the hold.

## 9. Approved Publication Clarification — 2026-08-19

Jerome approved the implementation specification
`_bmad-output/implementation-artifacts/spec-epic-16-planning-authority-publication.md`
on 2026-08-19. That approval resolves the publication details discovered while
mapping CP-1–CP-6 onto the deterministic publisher:

1. Epic V14, not an invented Epic V13, carries architecture V13 decisions
   DC-9, DC-10, and DC-11 into the canonical epic authority.
2. The already-published `v14-current-candidate-authority-v1.json` remains a
   point-in-time checkpoint head and is pinned unchanged. No checkpoint sidecar
   is minted or rebound by this publication.
3. Graph parity composes the 30 story nodes, the six inherited non-story nodes,
   and `E6-CURRENT-PROOF` plus `E6-CURRENT-CANDIDATE`: exactly 38 nodes and 61
   edges.
4. Story 16.1 has graph predecessors `7.4` and `IR-0`. The separately governed
   release-owner hold decision remains a global execution predicate, not a new
   graph node; while the hold is `ACTIVE`, Story 16.1 is non-executable.
5. The V14 story rows carry one exact repository-root command per acceptance
   scenario. AC-01 through AC-05 route through the story-specific
   `verify_story_16_*.py` verifier; AC-06 uses `generate_story_record.py`.
6. Publication validation compares the complete baseline-to-publication changed
   path set to a candidate-bound allowlist and rejects every raw mode-160000
   change.
7. The previously reported duplicate Story 13.1 tracking key is absent from the
   approved baseline and is not a repair target.

This clarification changes no requirement, product scope, lifecycle state,
action status, hold state, submodule, or gitlink.
