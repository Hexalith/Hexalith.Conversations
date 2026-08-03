---
title: "Sprint Change Proposal — Restore Epic 6 Implementation Readiness Authority"
project: "Conversations"
date: "2026-08-01"
status: "approved-for-planning-authority-implementation"
changeScope: "major"
mode: "batch"
triggerReport: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01.md"
currentAuthority: "epic-6-authority-2026-08-01-v7 / conversations-architecture-2026-08-01-v7"
proposedAuthority: "epic-6-authority-2026-08-01-v8 / conversations-architecture-2026-08-01-v8"
implementationGate: "NOT READY — authority correction and a successful readiness rerun are required before any remaining Epic 6 implementation work starts or resumes"
approval: "approved by Jerome on 2026-08-01"
approvedScope: "planning-authority correction, validation, and readiness rerun only; no remaining Epic 6 story implementation"
---

# Sprint Change Proposal — Restore Epic 6 Implementation Readiness Authority

## 1. Issue Summary

### Trigger

The implementation-readiness assessment dated 2026-08-01 evaluated the finalized PRD and
addendum, the append-only Epic 6 authority chain, architecture, UX specifications, UX requirement
mapping, and the active sprint plan. It found the initiative **NOT READY** even though all 124
functional requirements are traceable.

No implementation story introduced a new product requirement. The trigger is a planning-authority
failure discovered while Epic 6 is in progress: remaining work is described by overlapping
amendments and derived records that do not form one complete, internally consistent implementation
contract.

### Core problem

Epic 6 cannot safely continue because its current authority is incomplete and contradictory:

1. Stories 6.10 and 6.11 are tracked and referenced but lack canonical definitions in the applied
   Epic 6 authority chain.
2. Effective acceptance criteria are fragmented across v2-v7 amendments, so no self-contained
   current Story 6 execution view exists.
3. Dependencies are expressed largely by forward references and prose rather than one validated
   topological plan.
4. The v6 SM-C2 exception conflicts with the finalized PRD's universal `post P95 <= 1.05 x
   baseline P95` rule and with unchanged architecture sections.
5. Story 6.12 is oversized; completed Story 6.2 also lacks an explicit retrospective checkpoint
   model for readers of the current plan.
6. Story 6.4 does not name a versioned UX disposition artifact or a zero-gap validator.
7. Story 6.5 combines authoring guidance, fixture construction, measurement, and evidence into one
   broad completion boundary.
8. Several high-risk conditions are stated as prose instead of executable Given/When/Then
   scenarios.
9. Story 6.6 prescribes a `READY` assessment result rather than requiring an independent assessment
   whose actual result is preserved.
10. UX provenance points at an obsolete PRD path, its roadmap can be mistaken for activated scope,
    and its mapping assigns decisions to obsolete or nonexistent story identifiers.

This is an authority and backlog-governance defect, not an FR coverage gap, product pivot, or
request to weaken preservation obligations.

### Evidence

| Finding | Evidence |
| --- | --- |
| Requirements remain covered | Readiness traceability is 124/124: 20 initiative FRs plus 104 preserved Feature-FRs. |
| Missing story authority | Applied `epics.md` authority ends at v7 and contains no complete `Story 6.10` or `Story 6.11` definition. |
| Fragmented authority | Effective criteria for Stories 6.2, 6.3, and 6.6 are spread across multiple amendments; v7 says 6.10/6.11 retain scope without publishing it. |
| Conflicting performance rule | PRD sections 2, 7, 8, and OQ-5 require the universal +5% P95 gate. Epic/architecture v6 prose substitutes approved-cost ceilings or disclosure for some rows while other architecture sections retain the universal gate. |
| Invalid UX governance | `ux-design-specification.md` references `_bmad-output/planning-artifacts/prd.md`, which is not the canonical PRD, and `ux-requirement-map.md` maps UX-DR1–52 to obsolete/nonexistent stories such as 3.8 and 4.4. |
| Unsafe readiness state | Sprint status shows Story 6.12 as `ready-for-dev`, while the readiness report explicitly prohibits starting it or any other remaining implementation work. |

An existing separate proposal,
`sprint-change-proposal-2026-08-01-stories-6-10-6-11-authority.md`, contains useful complete
definitions for those two stories but is not applied authority and addresses only one subset of
the readiness findings. This comprehensive proposal preserves it as input. If this proposal is
approved, its single v8 publication supersedes that narrower proposal's unapplied publication plan
so only one v8 authority block is created.

### Required outcome

Publish one append-only, preservation-safe authority correction that makes Epic 6 self-contained,
topologically executable, metric-consistent, and mechanically checkable. Then rerun implementation
readiness. A `READY` result is required to lift the implementation hold, but is not predetermined by
this proposal.

## 2. Impact Analysis

### Epic impact

Only Epic 6 requires active-plan correction. Epics 1–5, their 24 completed stories,
retrospectives, accepted baselines, and signed evidence remain immutable history. No new epic is
needed, no completed epic becomes obsolete, and no product capability is added or removed.

Epic 6 remains viable after a major authority replan. Existing v1-v7 bytes are preserved. A new v8
amendment republishes the complete effective Epic 6 story set, explicit dependencies, checkpoint
boundaries, and outcome-neutral readiness semantics.

### Story impact

| Story | Current state | Proposed impact |
| --- | --- | --- |
| 6.1 | done | Preserve unchanged as completed authority history. |
| 6.2 | done | Preserve record and evidence byte-for-byte. Add only a retrospective checkpoint projection to the current execution view; do not split, renumber, reopen, or re-evaluate the completed story. Its v6 performance disposition remains historical completion context, not the current release gate. |
| 6.3 | in-progress | Keep status. Bind the normalized v8 authority, exact upstream dependencies, UX disposition identity, and current projection-proof head before completion. |
| 6.4 | backlog | Name exact versioned UX disposition artifacts, their required fields, deterministic Markdown projection, and a zero-gap validator. |
| 6.5 | backlog | Retain scope but divide delivery into three independently reviewable checkpoints with separate evidence boundaries. |
| 6.6 | backlog | Keep last. Replace the predetermined `readiness READY` criterion with an independent assessment, preserved actual result, and a separate release rule that blocks closure unless the result is `READY`. Enforce the PRD SM-C2 gate. |
| 6.7 | done | Preserve unchanged. |
| 6.8 | in-progress | Keep status but pause implementation under the global readiness hold. Retain its generated-record role. |
| 6.9 | backlog | Preserve scope; publish its place in the topological plan and BDD verification catalogue. |
| 6.10 | backlog | Publish the complete evidence-boundary helper story from the approved-but-unapplied source proposal, rebased into comprehensive v8. |
| 6.11 | backlog | Publish a complete correctness and measurement contract. Make it a mandatory pre-6.6 restoration of the universal SM-C2 gate for all four frozen hot paths. |
| 6.12 | ready-for-dev | Preserve the story and status label, but keep it non-startable under the readiness hold and its existing `6.8 -> 6.12` entry gate. Add three explicit internal checkpoints without weakening or renumbering its eight v7 criteria. |

### Artifact conflicts and required adjustments

| Artifact | Conflict | Required adjustment |
| --- | --- | --- |
| Initiative PRD/addendum | None in functional scope. PRD already defines the controlling universal SM-C2 rule. | Preserve requirements and MVP unchanged; identify PRD SM-C2/OQ-5 as sole metric authority. No PRD requirement edit is proposed. |
| `epics.md` | Missing 6.10/6.11 definitions; fragmented AC; incomplete dependency view; conflicting v6 performance semantics. | Append one comprehensive v8 block. Do not edit v1-v7. |
| `architecture.md` | v6 performance exception conflicts with OQ-5 and later universal-gate sections; status suggests corrective implementation can proceed. | Publish architecture v8, defer metric authority to PRD, and set `AUTHORITY CORRECTION ONLY / NOT READY` until rerun. |
| `epic-6-context.md` | Derived v7 view omits 6.10/6.11 and carries the conflicting SM-C2 rule. | Regenerate only after v8 is approved and published. |
| UX specification | Obsolete PRD provenance and apparent implementation roadmap activation. | Correct provenance and add a prominent preservation-only/non-activation banner. |
| UX requirement map | UX-DR1–52 point to obsolete or nonexistent story identifiers. | Convert the map to preservation governance and labeled historical provenance; do not assign current feature implementation ownership. |
| Story records/specs | Current authority identities and dependencies would become stale. | Regenerate or amend affected active/backlog guidance after v8; never rewrite completed records. |
| Sprint status | Status labels do not express the global readiness hold. | Add a chronological hold/publication note; preserve schema-valid story statuses. Do not start or resume work until readiness returns `READY`. |
| Planning-authority tests | Existing checks permit dangling/special-cased story definitions and semantic drift. | Enforce v8 completeness, one current view, exact story set, topological dependencies, metric authority, and generated-view parity. |

### Technical and delivery impact

This proposal itself changes planning and governance artifacts only. It authorizes no product UI,
production runtime, public contract, package, deployment topology, infrastructure, signed evidence,
completed story record, or submodule content change.

After readiness returns `READY`, implementation will affect test/evidence tooling, projection-proof
governance, internal projection validation performance, UX preservation evidence, and release
attestation. The existing ownership spine and no-new-product-UI boundary remain binding.

### Timeline and sprint impact

Remaining Epic 6 work pauses immediately. The schedule impact is the time to publish and validate
v8, rerun readiness, and only then execute the corrected topological plan. This avoids spending
implementation effort against authority that may subsequently be invalidated.

No story status is advanced by approving this proposal. In particular, `ready-for-dev` on Story
6.12 remains a file-lifecycle label, not permission to begin.

## 3. Recommended Approach

### Option evaluation

| Option | Viability | Effort | Risk | Assessment |
| --- | --- | --- | --- | --- |
| 1. Direct adjustment inside Epic 6 | **Viable and recommended** | High planning / medium implementation | Medium | Repairs authority without changing product scope or history. |
| 2. Roll back completed stories | Not viable | High | High | Would destroy valid completed history and does not repair UX, metric, or authority fragmentation. |
| 3. Review/reduce PRD MVP | Not required | Medium | High business risk | All 124 FRs are covered; no evidence shows the MVP or initiative goal is invalid. |

### Selected path

Use **Option 1: Direct Adjustment**, classified as a **major authority replan** because it changes
the cross-artifact execution contract and requires Product Manager/Solution Architect ownership.
It is not a product-scope expansion.

The correction has two gates:

1. **Authority gate:** approve, publish, and mechanically validate the comprehensive v8 artifact
   set.
2. **Readiness gate:** rerun implementation readiness against the published set and preserve the
   actual result. Implementation remains held unless that result is `READY`.

### Rationale

- It preserves completed work and append-only provenance.
- It restores one metric authority instead of negotiating performance policy inside evidence.
- It gives implementers one complete current plan while retaining historical amendments.
- It makes dependencies, checkpoint boundaries, and high-risk failure behavior mechanically
  reviewable.
- It repairs UX traceability without activating preserved UI scope.
- It separates an independent readiness assessment from the release decision that consumes it.

### Risk controls

- Never edit Epic 6 v1-v7 authority bytes or completed Story 6.2 evidence.
- Never publish two competing v8 amendments.
- Treat the normalized current view as a generated projection of v8, not an independent authority.
- Require exact parity and topological-validation tests before readiness is rerun.
- Preserve all current user-owned worktree changes while applying planning corrections.
- Require a separate approved PRD-level change proposal if the universal SM-C2 target is ever to
  change; implementation evidence cannot amend it.

## 4. Detailed Change Proposals

### 4.1 Authority publication model

**Artifacts:** `epics.md`, `architecture.md`, new normalized execution view

**OLD:**

- Applied authority ends at Epic 6 v7 / architecture v7.
- Effective current stories must be reconstructed across v2-v7.
- A separate approved-but-unapplied proposal claims a partial v8 for Stories 6.10/6.11.
- Derived context omits those story definitions and carries conflicting performance semantics.

**NEW:**

1. Append exactly one `EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8` block after the v7 end marker, with
   identity `epic-6-authority-2026-08-01-v8` and matching architecture identity
   `conversations-architecture-2026-08-01-v8`.
2. Preserve every v1-v7 byte. V8 republishes the **complete effective definitions** for Stories
   6.1–6.12, including status context, acceptance criteria, prohibitions, checkpoints, and direct
   dependencies. Historical done states remain facts, not mutable acceptance targets.
3. Add `_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md` as a deterministic,
   non-amending projection of v8. Its frontmatter records the exact source marker, authority
   versions, source hashes, generation command/version, and status source.
4. The view contains all twelve stories once, their complete effective criteria, BDD scenarios,
   a topological dependency table, current status labels, hold state, and completion gates.
5. Mechanical validation rejects missing/duplicate stories, unresolved dependency identifiers,
   cycles, source/version/hash drift, criteria drift, or a current view not generated from the
   active v8 block.
6. On approval, this comprehensive proposal supersedes only the **unapplied publication plan** in
   `sprint-change-proposal-2026-08-01-stories-6-10-6-11-authority.md`. Its story analysis remains
   supporting provenance. The already applied projection-proof proposal remains authoritative
   history.

**Rationale:** One canonical append-only amendment plus one validated projection resolves
fragmentation without rewriting history or creating a second authority source.

### 4.2 SM-C2 authority reconciliation

**Artifacts:** PRD (retained), `epics.md` v8, `architecture.md` v8, Epic 6 current view, Stories 6.6
and 6.11

**OLD — controlling PRD text:**

> For every identified command/read hot path, post-refactor P95 latency must be no more than 5%
> worse than the frozen pre-refactor P95 under the same reproducible benchmark envelope.

**OLD — conflicting v6 execution rule:**

- HP-LIST and HP-OPEN use an approved-cost ceiling because correctness work added cost.
- HP-CREATE and HP-APPEND are disclosed but not gated because measured dispersion exceeds the
  threshold.
- Story 6.6 may remeasure under that exception.

**NEW:**

- Retain the PRD text unchanged and declare PRD SM-C2/OQ-5 the sole current metric authority.
- Preserve the v6 exception as immutable historical context explaining how Story 6.2 reached
  `done`; it does not control current release readiness or Story 6.6.
- All four frozen rows—HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN—must have comparable evidence and
  satisfy `post P95 <= 1.05 x baseline P95` under the identical envelope at the release candidate.
- Story 6.11 owns both signal remediation for noisy command rows and correctness-preserving
  optimization for read rows. It is mandatory before Story 6.6.
- A ceiling, disclosure, changed correctness cost, unusable signal, or owner acceptance cannot
  substitute for the current gate. If a row cannot be validly measured or misses the threshold,
  Story 6.11 remains incomplete and release closure stays blocked.
- Changing the target requires a separate, explicit PRD-level change proposal and approval. It
  cannot be accomplished through benchmark evidence, an ADR, or a story disposition.

**MVP impact:** none. This restores the finalized requirement rather than adding one.

### 4.3 Canonical Story 6.10

**Story key:** `6-10-consolidate-the-evidence-boundary-validation-pattern`

**OLD:** Story 6.10 is referenced and tracked but has no complete definition in applied authority.

**NEW:** Publish the complete ten-criterion story already captured by the narrower proposal,
including:

1. A non-packable `Hexalith.Conversations.TestSupport` helper with no Conversations assembly
   reference.
2. Bounded, UTF-8, machine-safe Git execution with unavailable-history handling.
3. Recomputed manifest integrity, repository containment, canonical SHA-256, and signable-payload
   validation.
4. Exact changed-file equality and raw mode-`160000` gitlink detection.
5. Explicit skip-not-pass behavior for unavailable history and failure on zero executed assertions.
6. The stable blocker/warning contract for `_bmad/scripts/verify_evidence_boundary.py`.
7. Mandatory adoption by the governed workflow bodies and synchronized render twins.
8. Migration of all baseline evidence readers at unchanged assertion strength, coordinating but
   not absorbing Story 6.12.
9. Runbook plus fault injection for every boundary and anti-vacuity guard.
10. Repair of Story 6.7's gate-span coupling so displacement cannot remain falsely green.

**Prerequisites:** Stories 6.8 and 6.9.

**Completion dependencies:** Story 6.10 precedes completion of Stories 6.3, 6.5, and 6.6.

**Non-goals:** no production source, public contract, package, AppHost topology, CI wiring, signed
evidence, completed Story 6.2 artifact, or Story 6.12 lifecycle decision change.

**Rationale:** The already-reviewed definition is retained, but published only as part of the one
comprehensive v8 authority set.

### 4.4 Canonical Story 6.11

**Story key:** `6-11-make-cross-key-projection-validation-cheap-enough-to-re-gate-sm-c2`

**OLD:** A one-line scope and an unapplied ten-criterion draft cover only HP-LIST/HP-OPEN and make
Story 6.11 conditional for Story 6.6.

**NEW:**

#### Story 6.11: Restore the universal SM-C2 gate without weakening projection correctness

As a release owner,
I want all frozen hot paths to have usable comparable signal and remain within the PRD's regression
budget,
So that current readiness is based on one performance rule without weakening fail-closed behavior.

**Prerequisite:** Story 6.2. The story is independent of Stories 6.10 and 6.12, but is mandatory
before Story 6.6.

**Acceptance Criteria:**

1. Before production implementation, an ADR defines the per-conversation index-entry key family,
   derived-state ownership, write ordering, compatibility transition, rebuild/backfill, deletion,
   expiry, and rollback behavior. EventStore remains the only write authority.
2. HP-LIST/HP-OPEN validation removes unnecessary full-index or per-row fan-out only where an
   explicit correctness proof permits it. Missing, duplicated, stale, advanced, malformed,
   misfiled, pending, or mutually inconsistent state remains fail closed; reads never repair
   durable state.
3. Tenant isolation, retry/idempotency, delayed/out-of-order delivery, equal-position conflict,
   independent deletion, full replay, and interrupted rebuild remain deterministic and
   non-disclosing across every derived key family.
4. Public query contracts, filtering, ordering, cursors, freshness vocabulary,
   forbidden/nonexistent indistinguishability, and response shapes remain unchanged.
5. A versioned measurement-method decision defines sufficient repetitions, raw-sample retention,
   warm/cold classification, environment controls, and a predeclared signal-quality rule for all
   four rows. The method may reduce noise but may not change the PRD threshold or discard adverse
   samples after observation.
6. HP-CREATE and HP-APPEND obtain usable comparable P95 signal under the same frozen envelope;
   missing or unusable signal is a failed gate, not a disclosure-only pass.
7. HP-LIST and HP-OPEN use the same preserved Story 6.2 baseline fixture and satisfy the gate with
   all correctness tests green. Performance work may not weaken or reclassify correctness.
8. Unit, integration, and real Dapr state-store lanes fault-inject partial writes, latency,
   unavailable stores, poison records, retries, concurrency, tenant collisions, and replay.
9. One candidate-bound additive evidence set records every baseline/candidate raw sample,
   environment fact, calculation, signal verdict, and the exact code/test identities for all four
   rows. Generated JSON is authoritative and Markdown is deterministic.
10. Story 6.11 reaches `done` only when every frozen row satisfies
    `post P95 <= 1.05 x baseline P95` and all correctness gates are green. Story 6.6 independently
    reconfirms the result. Any miss, unusable signal, red/skip/not-run/vacuous test, or stale binding
    keeps the story incomplete and release closure blocked.

**Non-goals:** no public API, UI, deployment topology, package, signed-v1 evidence, completed Story
6.2 evidence, or EventStore authority change.

**Rationale:** This resolves the cross-artifact conflict at the PRD authority level and prevents a
conditional ceiling from becoming an undeclared requirements amendment.

### 4.5 Story 6.2 completed-history checkpoints

**OLD:** Story 6.2 is a large completed story whose effective criteria and evidence are spread
across amendments and its final record.

**NEW:** Do not split or rewrite Story 6.2. In the generated current execution view, project its
completed work into read-only retrospective checkpoints:

| Checkpoint | Boundary | Historical result |
| --- | --- | --- |
| 6.2-H1 Baseline and authority | Frozen inventory, benchmark, ownership, promotion declarations | Preserved from completed record. |
| 6.2-H2 Runtime and projection migration | Hosting, platform surfaces, population path, correctness lanes | Preserved from completed record. |
| 6.2-H3 Candidate evidence and closure | Candidate binding, generated record, promotion gate, historical SM-C2 disposition | Preserved from completed record. |

The view links each checkpoint to the immutable Story 6.2 record and evidence hashes. It explicitly
states that these are navigation aids, not new work items or independent completion claims.

**Rationale:** This answers the readiness sizing concern without falsifying completed history.

### 4.6 Story 6.4 exact UX preservation contract

**OLD:**

> Treat UX as preservation-only unless separately activated; repair provenance/mappings while
> retaining historical mappings as labeled provenance.

The criterion does not identify the output artifact, schema, version, or zero-gap test.

**NEW:**

Story 6.4 produces exactly:

- `docs/release-evidence/ux-preservation-disposition-v1.schema.json`
- `docs/release-evidence/ux-preservation-disposition-v1.json`
- `docs/release-evidence/ux-preservation-disposition-v1.md` as a deterministic projection
- `UxPreservationDispositionValidationTest` in the conformance test project

The JSON binds the canonical PRD/addendum and UX source path/hash, inventories UX-DR1–52 and every
UX acceptance-criterion identifier exactly once, and gives each item:

- `currentDisposition: preserved-not-activated` unless separately approved authority says otherwise;
- named preservation owner and rationale;
- evidence/control reference or explicit preservation-only non-activation record;
- `historicalProvenance` references that are labeled non-current and cannot become implementation
  ownership;
- compatibility and disclosure-safety obligations where applicable.

The validator fails on missing, duplicate, unknown, reordered-without-regeneration, unhashed,
unowned, or activated-without-authority entries; invalid source hashes; JSON/Markdown drift; and
any UX item mapped to a nonexistent current story.

**Rationale:** Story 6.4 becomes objectively complete and closes the UX zero-gap requirement.

### 4.7 Story 6.5 delivery checkpoints

**OLD:** Story 6.5 combines template rules, a live fixture, SM-2 measurement, versioned evidence,
and preservation of the 13,289-LOC baseline in one broad boundary.

**NEW:** Retain one Story 6.5 identifier and all existing criteria, but require three ordered,
independently reviewable checkpoints:

| Checkpoint | Deliverable | Exit evidence |
| --- | --- | --- |
| 6.5-A Authoring contract | Corrected thin-module guidance with ownership and prohibited-capability rules | Versioned guidance validation and reviewer decision. |
| 6.5-B Minimal fixture | Reproducible non-packable/non-publishable module test AppHost fixture using live public platform APIs | Clean build/tests and exact source inventory. |
| 6.5-C Measurement and conclusion | Generated SM-2 v2 measurement using frozen inclusions and preserved baseline | Schema-valid JSON/Markdown, tool/commit bindings, independent validator, named acceptance. |

Story 6.5 cannot complete on a checkpoint alone. Story 6.8 governs its final record, Story 6.10
governs its evidence boundary, and all three checkpoints must pass at one compatible final
candidate.

**Rationale:** Checkpoints reduce review and rollback radius without multiplying story identifiers.

### 4.8 Story 6.12 internal checkpoints

**OLD:** Eight acceptance criteria and many interdependent task groups form one oversized delivery
boundary.

**NEW:** Preserve all eight v7 criteria verbatim and add three ordered internal checkpoints:

| Checkpoint | Criteria | Deliverable and rollback boundary |
| --- | --- | --- |
| 6.12-A Historical validity and lifecycle contract | AC1–AC3 | Protected-byte inventory; candidate-aware historical validation; ADR 0004; closed successor-chain schema. No v3 current-head claim. |
| 6.12-B Successor generation and current guard | AC4–AC5 | Deterministic v3 generator/projection; fresh functional lanes; exact approval; one current head; drift guard. May be discarded without changing v2 history. |
| 6.12-C Fault injection, manifest handoff, and closure | AC6–AC8 | Complete mutation matrix; Story 6.3 binding; Story 6.6 consumption contract; full conformance; Story 6.8-generated final record. |

Each checkpoint has its own review note and machine-readable evidence inventory. Checkpoint success
does not advance the story status to `done`; only all eight criteria at the compatible final
candidate do. Story 6.8 remains a hard entry prerequisite.

**Rationale:** This provides reviewable and reversible boundaries without weakening the proof chain
or inventing more canonical story identifiers.

### 4.9 Story 6.6 outcome-neutral readiness rule

**OLD:**

> Run last and require readiness `READY` before release closure.

This wording can be read as prescribing the assessor's output.

**NEW:**

> Run Story 6.6 last. Execute a fresh, independent implementation-readiness assessment against the
> exact committed candidate and current authority/evidence identities, publish the complete report
> unchanged, and preserve its actual result. The assessment must not be instructed or modified to
> return a particular verdict. Release closure is a separate decision and remains blocked unless
> the preserved result is `READY`; `NOT READY` or an incomplete assessment leaves Story 6.6 and
> Epic 6 open.

Story 6.6 also consumes the universal PRD SM-C2 rule for all four rows and cannot substitute the v6
historical exception.

**Rationale:** Independent evidence determines the verdict; the release gate consumes it without
outcome bias.

### 4.10 High-risk BDD scenario catalogue

**OLD:** High-risk denial, mutation, stale-binding, performance, and anti-vacuity behavior is
distributed as prose across stories and amendments.

**NEW:** V8 and the generated current view include at least these executable scenario contracts,
linked to owning criteria and tests:

```gherkin
Scenario: Cross-tenant derived key is presented during an authorized read
  Given tenant A is authorized and an otherwise valid record is stored under tenant B's key
  When the list or detail query validates the derived state
  Then the query fails closed without disclosing tenant B existence or content

Scenario: Evidence content changes after candidate binding
  Given a generated evidence artifact is bound by path, mode, hash, candidate, and test binary
  When any bound byte or identity changes
  Then validation fails with a stable blocker and no stale evidence is reused

Scenario: Historical proof is valid but the current dependency set drifted
  Given v2 validates at its recorded candidate and an approved current head exists
  When an in-scope current dependency changes without an approved successor
  Then current readiness fails with PROJECTION_PROOF_SUPERSESSION_REQUIRED
  And historical v2 validity remains unchanged

Scenario: A required test is skipped or the assertion ledger is empty
  Given an evidence lane is required by a story completion gate
  When the result is skipped, not run, missing, stale, or records zero executed assertions
  Then the gate fails and cannot be reported as not-applicable or passing

Scenario: A frozen SM-C2 row has unusable signal or exceeds the threshold
  Given its baseline and candidate use the frozen identical envelope
  When signal quality is unusable or post P95 exceeds 1.05 times baseline P95
  Then Story 6.11 remains incomplete and Story 6.6 cannot close

Scenario: Readiness returns NOT READY
  Given Story 6.6 executes an independent assessment and preserves the complete report
  When the result is NOT READY
  Then the report remains unchanged and release closure stays blocked
```

**Rationale:** These scenarios make the most consequential failure behavior reviewable and
automatable without replacing the full acceptance criteria.

### 4.11 UX specification and mapping repair

**Artifacts:** `ux-design-specification.md`, `ux-requirement-map.md`

**OLD:**

- UX frontmatter references `_bmad-output/planning-artifacts/prd.md`.
- Phase 0–3 component-roadmap language appears as an active implementation plan.
- UX-DR1–52 map to historical, obsolete, or nonexistent feature-story identifiers.

**NEW:**

- Replace PRD provenance with the canonical PRD and addendum paths under
  `prds/prd-Conversations-2026-06-02/` and bind their current authority versions/hashes through the
  Story 6.4 disposition artifact.
- Add a prominent opening banner:

  > **Preservation-only UX authority.** This document preserves product UX decisions and
  > acceptance obligations. It does not activate product UI implementation in the current
  > corrective initiative. Activation requires separate approved release authority.

- Relabel Phase 0–3 as a preserved historical/future activation sequence, not the active Epic 6
  plan.
- Replace `Primary Epics / Stories` in the UX map with `Current disposition` and
  `Historical provenance`. Every current row points to the Story 6.4 disposition artifact and
  `preserved-not-activated`; old story references remain only in the labeled historical column.
- Add a generated acceptance-criterion inventory section so all UX criteria, not only UX-DR1–52,
  participate in zero-gap validation.

**User-experience impact:** none at runtime. This change prevents preserved design intent from being
mistaken for authorized UI work.

### 4.12 Architecture v8

**OLD:** Architecture v7 includes both the v6 exception and unchanged sections stating the
universal gate, omits complete 6.10/6.11 contracts, and declares readiness for corrective
implementation only.

**NEW:**

- Advance `authorityVersion` to `conversations-architecture-2026-08-01-v8`; add v7 to superseded
  versions and this approved proposal to `correctionAuthority`.
- Add a `2026-08-01 Implementation Readiness Authority Correction` amendment covering the complete
  v8 story set, normalized view, dependencies, checkpoint boundaries, UX artifact, BDD catalogue,
  and independent readiness semantics.
- State that PRD SM-C2/OQ-5 is controlling and the v6 exception is historical Story 6.2 completion
  context only.
- Replace the current readiness banner with:

  > **Overall Status: AUTHORITY CORRECTION ONLY — NOT READY.** No remaining Epic 6 implementation
  > work may start or resume until comprehensive v8 is published, mechanical authority validation
  > passes, and a new implementation-readiness assessment returns `READY`.

- Preserve the ownership spine, public contracts, production topology, EventStore write authority,
  fail-closed tenant rules, preservation denominators, and no-product-UI boundary.

### 4.13 Derived artifacts and sprint state

**Epic 6 context**

- Regenerate from v8 with all twelve story summaries, checkpoint tables, universal SM-C2 rule,
  dependency topology, hold state, and exact source marker/version.
- Validation must prove parity with v8; hand-edited semantic drift fails.

**Active/backlog story guidance**

- Update authority provenance and effective criteria in Stories 6.3, 6.4, 6.5, 6.6, 6.8, 6.9,
  6.10, 6.11, and 6.12 only after v8 publication.
- Do not rewrite completed story records. Story 6.2 receives links only from the derived view.
- Story 6.12's current untracked guide remains non-startable and is amended only after approval.

**Sprint status**

- Add a chronological note recording the readiness hold, approved v8 publication, and rerun
  requirement.
- Preserve the schema-valid statuses: 6.3/6.8 `in-progress`, 6.4/6.5/6.6/6.9/6.10/6.11 `backlog`,
  6.12 `ready-for-dev`, completed stories `done`.
- Status preservation does not authorize work. No transition occurs until the readiness gate is
  lifted and the owning workflow legitimately changes it.

### 4.14 Topological execution plan

**OLD:** Dependencies are spread across amendments and expressed partly as forward references.

**NEW:** The v8 current view publishes and mechanically validates this dependency plan:

| Gate/wave | Work | Entry condition | Completion unlocks |
| --- | --- | --- | --- |
| Authority Gate | Publish/validate v8 and rerun readiness | Proposal approval | Remaining work only if readiness is `READY` |
| Existing completed spine | 6.1 -> 6.7 -> 6.2 | Historical fact | Prerequisites already satisfied |
| Wave 1 | Resume 6.8; execute 6.4, 6.5-A/B, 6.9, 6.11 | Readiness `READY`; story-local prerequisites | 6.8/6.9 unlock 6.10; 6.8 unlocks 6.12 |
| Wave 2 | 6.10 and 6.12 in parallel; finish 6.5-C when 6.8/6.10 gates permit | Direct predecessors done | Completion paths for 6.3/6.5/6.6 |
| Wave 3 | Complete 6.3, 6.4, 6.5 | Their exact dependencies and evidence pass | Capstone eligibility |
| Wave 4 | 6.6 only | Every required predecessor done; universal SM-C2 green | Independent readiness assessment and possible Epic 6 closure |

Direct dependency edges are:

```text
6.1 -> 6.7 -> 6.2 -> 6.8
6.1 -> 6.4
6.2 -> 6.5
6.2 -> 6.11
6.8 -> completion of 6.4 and 6.5
6.1 -> 6.9
6.8 + 6.9 -> 6.10
6.8 -> 6.12
6.9 + 6.10 + 6.12 -> completion of 6.3
6.10 -> completion of 6.5
6.3 + 6.4 + 6.5 + 6.8 + 6.9 + 6.10 + 6.11 + 6.12 -> 6.6
```

Stories 6.10, 6.11, and 6.12 are mutually independent after their stated prerequisites, though
they must preserve compatible changes on shared validation surfaces.

**Rationale:** The view is ordered by executable prerequisites rather than story number and can be
cycle-checked mechanically.

### 4.15 Mechanical authority and readiness checks

Update or add planning conformance coverage so that it fails when:

- applied Epic 6 authority does not contain exactly one complete effective definition for every
  tracked Story 6.1–6.12;
- the generated current view is absent, stale, hand-edited, or differs semantically from v8;
- any dependency is unknown, cyclic, contradictory, or absent from the topological projection;
- sprint status contains an Epic 6 story not present in current authority, or vice versa;
- architecture/epics/current view state an SM-C2 rule other than the controlling PRD rule;
- the v6 exception appears as a current Story 6.6 pass option;
- UX source provenance is obsolete, any UX decision/criterion is missing or duplicated, or an
  inactive UX item is assigned to nonexistent current implementation authority;
- Story 6.6 requires an assessor to return `READY` rather than preserving the actual result;
- a remaining story starts or resumes while the global readiness hold is active.

The authority validator may inspect planning and sprint artifacts. It does not edit them, infer
approval, or turn a planning proposal into applied authority.

## 5. Implementation Handoff

### Scope classification

**Major** — the change is a cross-artifact authority replan requiring Product Manager and Solution
Architect ownership. It does not change the MVP, but it changes the binding execution contract.

### Handoff recipients and responsibilities

| Role | Responsibility |
| --- | --- |
| Product Manager | Confirm no PRD/MVP scope change; own the decision that PRD SM-C2 remains controlling; approve the comprehensive correction. |
| Solution Architect | Publish architecture v8 and the append-only Epic 6 v8 contract; preserve ownership, topology, and history invariants. |
| Product Owner / Scrum Master | Validate the topological backlog, checkpoint boundaries, global hold note, and unchanged schema-valid statuses. |
| UX owner | Correct UX provenance/banner/map and own the versioned preservation disposition inventory without activating UI scope. |
| Test Architect / Quality | Define zero-gap, parity, dependency, metric-authority, BDD, and outcome-neutral readiness validation. |
| Developer agents | Do not implement remaining stories until the new readiness report is `READY`; afterward execute only the normalized v8 plan. |
| Independent readiness assessor | Rerun the complete assessment after publication and preserve the actual result without target-outcome instruction. |

### Ordered action plan

1. Obtain explicit approval of this proposal and record any conditions.
2. Mark this proposal approved; mark the narrower unapplied 6.10/6.11 publication plan superseded
   by this comprehensive correction without deleting it.
3. Append Epic 6 v8 and publish architecture v8 atomically; preserve all earlier authority bytes.
4. Generate the normalized Epic 6 current execution view and update mechanical authority tests.
5. Repair UX provenance/mapping and publish the versioned UX preservation disposition contract.
6. Regenerate Epic 6 context and synchronize active/backlog story guidance and sprint hold notes.
7. Run planning-authority, parity, dependency, UX zero-gap, and metric-consistency checks.
8. Rerun the complete implementation-readiness assessment against the committed authority set.
9. If the result is `NOT READY`, preserve it and return to Correct Course. If `READY`, lift the
   implementation hold and execute the topological plan.

### Success criteria

- One applied Epic 6 v8 and matching architecture v8 exist; no competing v8 is published.
- V1-v7 authority, completed story records, accepted baselines, signed evidence, and submodule
  content remain unchanged.
- All twelve Epic 6 stories have one complete effective definition in the current v8 view.
- Stories 6.10 and 6.11 have canonical, implementation-ready contracts.
- The dependency graph is complete, acyclic, and mechanically identical across authority, current
  view, context, and story guidance.
- PRD SM-C2 is the only current performance rule; all four rows are gated at universal +5% for
  Story 6.6.
- Story 6.4 has exact v1 UX artifacts and a zero-gap validator covering UX-DR1–52 and every UX AC.
- Story 6.5 and Story 6.12 have explicit checkpoint/evidence/rollback boundaries.
- High-risk behaviors are represented by linked executable BDD scenarios.
- Story 6.6 preserves the assessor's actual result and separates it from the release decision.
- UX documentation is visibly preservation-only and contains no current mapping to obsolete or
  nonexistent stories.
- A new implementation-readiness report is generated. Remaining implementation work begins only
  if its actual result is `READY`.

## 6. Approval and Routing Record

**Decision:** Approved by Jerome on 2026-08-01 without additional conditions.

**Approved scope:** Publish and validate the planning-authority correction described in this
proposal, then rerun implementation readiness. Approval does not authorize Story 6.12 or any other
remaining Epic 6 story implementation.

**Classification and route:** Major authority replan, handed off to the Product Manager and
Solution Architect as joint owners. Product Owner/Scrum Master, UX owner, and Test Architect support
the artifact-specific changes and validation. Developer agents remain held until the independent
readiness rerun returns `READY`.

**Sprint-status disposition:** No epic or story is added, removed, renumbered, or advanced by this
approval, so no `development_status` value changes during Correct Course finalization. The approved
authority-publication handoff owns the chronological hold/publication note described in section
4.13.

## Appendix A — Change Navigation Checklist Record

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | Done | No causal implementation story; readiness assessment during active Epic 6 is the trigger. Story 6.12's `ready-for-dev` state makes the hold urgent. |
| 1.2 Core problem | Done | Planning-authority publication, consistency, and executability defect. |
| 1.3 Evidence | Done | Readiness report plus PRD/Epics/Architecture/UX/sprint cross-check. |
| 2.1 Current epic viability | Done | Epic 6 remains viable after comprehensive v8 correction. |
| 2.2 Epic-level changes | Action-needed | Publish v8/current view and corrected topology after approval. |
| 2.3 Remaining epics | Done | No future epic exists; all remaining impact is inside Epic 6. |
| 2.4 New/obsolete epics | N/A | No new or removed epic. |
| 2.5 Order/priority | Action-needed | Apply readiness hold and validated topological waves. |
| 3.1 PRD | Done | No FR/MVP change; retain universal SM-C2 as sole authority. |
| 3.2 Architecture | Action-needed | Publish v8, remove current conflict, set NOT READY state. |
| 3.3 UX | Action-needed | Repair provenance, preservation banner, mapping, and zero-gap artifact. |
| 3.4 Other artifacts | Action-needed | Regenerate context/story guidance/status notes and strengthen validation. |
| 4.1 Direct adjustment | Viable | High planning effort, medium delivery risk. |
| 4.2 Rollback | Not viable | Reopens valid history and fails to solve authority defects. |
| 4.3 MVP review | Not viable/needed | 124/124 FR coverage and unchanged product goals. |
| 4.4 Selected path | Done | Direct adjustment as a major authority replan. |
| 5.1–5.5 Proposal components | Done | Issue, impact, path, MVP effect, action plan, and handoff are documented here. |
| 6.1 Checklist review | Done | All findings are resolved in the proposal or explicitly handed off. |
| 6.2 Proposal accuracy | Done | Jerome reviewed the batch proposal and continued to formal approval. |
| 6.3 Explicit approval | Done | Approved by Jerome on 2026-08-01 without additional conditions. |
| 6.4 Sprint status update | N/A at finalization | No epic/story add, remove, renumber, or status transition is approved; publication handoff owns the chronological hold note. |
| 6.5 Handoff confirmation | Done | Major correction routed to Product Manager/Solution Architect with PO, UX, and Test Architect support. |

## Appendix B — Approval Boundary

Approval authorizes the **planning-authority correction, its validation, and the readiness rerun
only**. It does not authorize Story 6.12 or any other remaining implementation work. That work
remains blocked until the newly published readiness assessment returns `READY`.
