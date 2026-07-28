# Sprint Change Proposal — Conformance Oracle Tiering

**Date:** 2026-07-28
**Project:** Conversations (Boilerplate Reduction — Behavior-Preservation Refactor)
**Prepared for:** Jerome
**Trigger:** Standing Epic 5 action item — *"Decide the long-term path for residual
Conformance.Tests to Server coupling."* (`sprint-status.yaml`, owner `Architect/Quality`,
status `open`)
**Change scope classification:** **Moderate** — backlog reorganization (one new Epic 6
story via authority amendment), no production source change
**Status:** approved by Jerome, 2026-07-28

---

## 1. Issue Summary

### Problem statement

The conformance oracle — Conversations' release gate — has carried a project reference
from `tests/Hexalith.Conversations.Conformance.Tests` to `src/Hexalith.Conversations.Server`
since before the refactor began. It was recorded on day one as a risk to be resolved, and
it has been carried forward four times without a decision ever being made. The action item
asks for the long-term path. This proposal supplies it.

### How it was discovered, and why it is still open

This is not a newly discovered defect. It is a decision that was deferred until the
artifacts that were supposed to close it ran out.

| When | Where | What happened |
|---|---|---|
| 2026-06-02 | Story 1.1, AC3 | Recorded the reference as an **oracle-survivability risk** — the oracle compiles against the plumbing assembly the refactor is about to move. Explicitly *not* fixed; flagged to Story 1.3. |
| 2026-06-03 | Story 1.3 | Classified the coupled suites `coupled-by-design-retarget-in-owning-story`; retarget assigned to the last owning story of {2.2, 2.5, 3.2, 3.3}. |
| 2026-06-24 | Story 3.3 | Retargeted telemetry emitters, but **retained** the reference and ledgered the remaining live coupling. Epic 3 retro T2: *"Reframed honestly. Story 3.3 retained the reference and ledgered exact remaining live coupling instead of claiming closure."* |
| 2026-06-27 | Story 5.2 | Recorded `decision: "retain-residual-coupling"`, `safeRemovalAttempt.attempted: false`, and a `followUpPath` naming two options. **A disclosure, not a decision.** |
| **Now** | Epic 5 action item | The `followUpPath` is the open question. Epic 5 is `done` and immutable, so the item has no home. |

Each step was individually honest. The cumulative effect is a structural claim that has
been "about to be closed" for two months.

### Evidence

Verified against the working tree, not inherited from the artifacts:

- **The reference is live.** `tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` references `Contracts`, `Client`, `Testing`, **and `Server`**.
- **13 files carry live coupling** across five namespaces — `Server.Diagnostics`, `.TenantAccess`, `.Projections`, `.CommandHandlers`, `.Governance` — inventoried at `removed-test-justification-ledger-reconciliation-v1.json#projectReferenceDisposition.residualCouplingInventory`.
- **It is not internals access.** `Hexalith.Conversations.Server.csproj` grants `InternalsVisibleTo` to `Hexalith.Conversations.Server.Tests` only. The oracle binds *public* Server types. This is assembly coupling, not encapsulation leakage.
- **`Server` is never shipped.** `IsPackable=false`, `Microsoft.NET.Sdk.Web`. **The oracle is therefore non-portable by construction** — no consumer of the Conversations packages could reproduce it today.
- **`Testing` is already the clean seam.** `IsPackable=true`, references `Contracts` + domain only, no `Server`.
- **Three of the thirteen are in the frozen FR-20 denominator.** `release-baseline-v1.json` names `TelemetryCardinality`, `TelemetryRedaction`, and `ConformanceStatus`. The other ten are post-baseline additions (Story 1.2 blind-spot backfills and later work).

---

## 2. Impact Analysis

### The reframe that drives everything below

Story 1.1's concern was precise and, at the time, correct:

> the oracle compiles against the plumbing assembly being refactored

**That plumbing has left.** Epics 2–3 moved hosting, query/cursor, read-model write policy,
projection dispatch, serialization, tenant-access registration, and diagnostics scaffolding
out to `EventStore.DomainService`, `Hexalith.Commons.*`, and platform deployment. What
remains in `Server` — tenant-access guards, the idempotent command executor, the governance
audit sink, the projection materializer, the diagnostics classifiers — is precisely what the
Epic 6 corrected ownership spine assigns to Conversations:

> Conversations owns contracts, aggregate/domain behavior, validators, handlers,
> projections/read-model semantics, domain adapters, domain telemetry definitions…

So the residual coupling is **not a survivability defect awaiting removal**. It is an
oracle that asserts two different contracts and has never distinguished them. The problem
is a missing label, and the fix is to make the label real and mechanically checkable.

### Epic impact

| Epic | Impact |
|---|---|
| Epics 1–5 | `done` and immutable. The action item outlives its epic; Epic 6 must absorb it. |
| **Epic 6** | **Gains Story 6.9** via authority-overlay amendment v5 — the same mechanism that added Story 6.8. |
| Story 6.3 | Manifest must record oracle tiering as a current control (new AC4). |
| Story 6.6 | v2 attestation must consume the decision and rerun both tiers (new AC5). Remains last. |
| Story 6.8 | No edit. Its declared-project list is the mechanism; 6.9 AC6 obligates the declaration, and 6.8 AC2 turns a forgotten one into a blocker. |

No epic is invalidated. No epic becomes obsolete. Story 6.9 is sequenced parallel-after-6.1,
not inside the `6.1 → 6.7 → 6.2 → 6.8` spine — it touches only test projects and evidence
artifacts, and serializing it behind the migration is what stalled it three times before.

### Artifact conflicts

| Artifact | Conflict |
|---|---|
| **PRD FR-20** (`prd.md:324`) | *"Removing, replacing, or **reclassifying** any manifested test requires explicit named-owner approval, rationale… and a versioned manifest update."* Three coupled files are manifested → reclassification needs approval. **This proposal's approval discharges it.** |
| **PRD FR-20** (`prd.md:323`) | Publicizing Server seams would change the 196-type contract-shape baseline → approval + compatibility evidence. This is what makes Option B expensive. |
| **architecture.md:319** | *"Adopters and extensions should be able to verify…"* — an intent today's oracle cannot satisfy for any of its content. |
| **architecture.md:1173, 1273** | Project tree shows one conformance project. |
| **at-risk-test-register-v1.json** | Carries `targetEndState: "Public-surface suites no longer transitively depend on the Server plumbing assembly."` A standing promise that is now the *wrong* target. Immutable → must be **superseded, not edited**. |
| UX specification | **N/A** — no UI surface. |

### Technical impact

Test-project structure and evidence artifacts only. **No production source under `src/`
changes. No public contract changes. No submodule promotion. No deployment impact.**

---

## 3. Recommended Approach

### Options evaluated

| Option | Viable | Effort | Risk | Verdict |
|---|---|---|---|---|
| **A — Tier the oracle** (portable + declared module-internal) | Yes | Medium-low | Low | **Selected** |
| **B — Publicize Server seams, delete the reference** | Marginal | High | High | Rejected |
| **C — Accept permanently, no structural change** | Yes | Minimal | Medium | Rejected |
| **D — Defer to post-initiative backlog** | No | — | — | Rejected |

**Why B is rejected.** It widens the Conversations public contract solely so tests can reach
implementation types — trading a permanent adopter-surface regression for a test-structure
convenience, and triggering FR-20 contract-shape approval to do it. Test reachability is
never a good reason to make something public.

**Why C is rejected.** It leaves `architecture.md:319`'s verifiability intent permanently
unmeetable for the whole oracle, and keeps the portable/coupled distinction as prose — which
is the exact form that allowed three deferrals.

**Why D is rejected.** Story 6.6 is last and must attest on current evidence. An open
Architect/Quality decision at attestation time is the failure mode Epic 6 exists to correct.

### Selected path: Hybrid A — decide *tier*, triage before splitting

The decision is to tier. But Story 6.9 does not split blindly: for each of the 13 coupled
files it **first attempts public re-expression at unchanged assertion strength**, and only
what survives that attempt moves to the module-internal tier. This honors Story 5.2's
recorded `followUpPath` — *"move behind public surfaces, **or** split"* — **in order**,
rather than picking one and discarding the other.

Two prohibitions make the outcome trustworthy in either direction:

- **Never widen the public contract** to relocate an assertion into the portable tier.
- **Never weaken an assertion** to make it portable. A check that cannot move at full
  strength belongs in the module-internal tier, and **that is a passing outcome, not a
  deferral.**

Without those, "tier the oracle" degrades into either contract bloat or quiet weakening —
and both would look like success in the artifacts.

### Effort, risk, timeline

- **Effort:** medium-low. 13 files triaged, one new test project, one structural guard test, evidence regeneration, two AC additions.
- **Risk:** low. No test removed, no assertion weakened, no contract changed, no production source touched. The principal risk — a count silently shrinking across the split — is closed by monotonicity across both tiers, measured from a **generated** figure.
- **Timeline:** does not extend the critical path. 6.9 runs parallel-after-6.1 and must complete before 6.3 and 6.6.

### One deliberate imprecision

The monotonicity baseline is **not** transcribed into this proposal. The `sprint-status.yaml`
entries for Story 6.2 record the conformance count as both `418` and `417/418` on consecutive
days — the hand-transcription failure that motivated Story 6.8. Story 6.9 derives its
pre-split figure from a machine-readable result artifact at execution time. Citing a number
here would reintroduce exactly the defect Epic 6 is correcting.

---

## 4. Detailed Change Proposals

All six were reviewed and approved individually.

### 4.1 — Epics: authority-overlay amendment v5 + Story 6.9

`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` — append
amendment block `epic-6-authority-2026-07-28-v5`, superseding v4 **only** by adding Story 6.9
and amending the binding order. Declares the two-tier oracle and the two prohibitions.

**Story 6.9 acceptance criteria:**

1. Every file binding a `Hexalith.Conversations.Server` namespace is triaged in a versioned
   record — re-expressed against public surfaces at unchanged assertion strength, or assigned
   to the module-internal tier with the exact type and reason it cannot move. Widening the
   public contract is not an available resolution.
2. The portable tier carries no reference to a non-packable module assembly, asserted **from
   the resolved compile surface**, not from csproj text.
3. Nothing is removed, skipped, renamed away, or weakened. Executed count is monotonic
   against the pre-split figure, computed across both tiers.
4. Reclassification of the three manifested denominator suites records named-owner approval,
   rationale, and a versioned manifest update per FR-20. Denominator membership is unchanged;
   only recorded tier changes.
5. A versioned v2 disposition artifact supersedes the v1 `projectReferenceDisposition` target
   end-state. v1 artifacts are not edited.
6. Both tiers are declared to the Story 6.8 generator and the solution file, so neither is
   silently unrun.

**Amended binding order:** `6.1 → 6.7 → 6.2 → 6.8`, plus `6.9 → 6.3` and `6.9 → 6.6`.
6.9 may proceed after 6.1. 6.6 remains last.

### 4.2 — Architecture: version v5 + tiering amendment + project tree

`_bmad-output/planning-artifacts/architecture.md` — `authorityVersion` to
`conversations-architecture-2026-07-28-v5`, v4 moved to `supersededAuthorityVersions`, this
proposal appended to `correctionAuthority`. New section *"2026-07-28 Conformance Oracle
Tiering Amendment"* declaring both tiers, the binding consequences, and the two prohibitions.
Project tree at lines 1173 and 1273 shows both projects with tier comments.

The portable tier keeps the **existing project name** — renaming it would churn every
historical reference in the immutable v1 evidence. The coupled tier is
`Hexalith.Conversations.Conformance.Server.Tests`, mirroring the existing `Server.Tests`
convention so the coupling reads as intentional.

### 4.3 — Epic 6 developer context: regenerate to v5

`_bmad-output/implementation-artifacts/epic-6-context.md` — `overlay_version` and
`architecture_version` to v5, `supersedes_overlay_version` to v4. New `### 6.9` story block.
Binding Sequence gains the 6.9 constraints. New **Conformance Oracle Tier Invariant**
section — the place a dev agent actually looks mid-story.

### 4.4 — New evidence artifact: `conformance-oracle-tiering-decision-v2`

`docs/release-evidence/conformance-oracle-tiering-decision-v2.{json,md}` — authored by this
decision, completed by Story 6.9. Records: the `supersedes` block quoting the v1
`targetEndState` **verbatim** with the reason it is no longer correct; `decision:
"tier-the-oracle"`; `rejectedAlternatives` with rationale for each; both tier definitions;
the four invariants; the `fr20Reclassification` block naming the three manifested suites,
`denominatorMembershipChanged: false`, `testsRemoved: 0`, `testsWeakened: 0`, and the
named-owner approval; the triage input pointer; and `nonClaims`.

`triageResults` is **`null` at authoring time** — so a reader can always distinguish
*decided* from *executed*. Story 6.9 fills it.

### 4.5 — Sprint status

`_bmad-output/implementation-artifacts/sprint-status.yaml` — `6-9-…: backlog` added to
`development_status`; the Epic 5 action item moves to `done` **with a `note` recording that
the action was to *decide*, and that the physical tiering is tracked in Story 6.9, not
closed on the basis of work performed**; a story-creation comment carrying the decision,
both prohibitions, the FR-20 constraint, and the declaration requirements.

That `note` is load-bearing: without it a future reader sees a closed item and concludes the
coupling was resolved.

### 4.6 — Downstream ripples

- **Story 6.6 gains AC5** — the v2 attestation consumes and hash-validates the tiering
  decision and triage record, reruns both tiers, reports counts separately and summed, states
  the portable tier's property as a test result, and does **not** inherit the superseded v1
  end-state as met or as a waiver. Mirrors the existing AC4 treatment of ADR 0003.
- **Story 6.3 gains AC4** — the manifest records each assertion's tier and binds the decision
  artifact by hash; the portable tier's property is recorded as a validated test outcome.
- **`Hexalith.Conversations.slnx`** — new project entry (executed by 6.9).
- **Story 6.8** — no edit required; checked, not overlooked.

**Deliberately not changed:** `at-risk-test-register-v1.*` and
`removed-test-justification-ledger-reconciliation-v1.*` (immutable; the latter's
`residualCouplingInventory` is the triage input and must stay verbatim);
`release-baseline-v1.json` (frozen denominator, unchanged by tiering); PRD FR-20 (being
complied with, not amended); `architecture.md:319` (the portable tier now satisfies it —
amending would read as retconning); UX artifacts (no UI surface); `project-context.md`
(consistent with existing testing rules).

---

## 5. Implementation Handoff

**Scope classification: Moderate** — backlog reorganization plus planning-artifact
amendments. No production source change, so this is not Major; more than a direct code edit,
so it is not Minor.

| Recipient | Responsibility |
|---|---|
| **Architect** (authority amendments) | Apply 4.1, 4.2, 4.3. Keep v5 semantically identical across epics, architecture, and derived context — drift between them is a conformance failure by Epic 6's own rule. |
| **Product Owner / Dev workflow** | Apply 4.5 and the 4.6 AC additions. Create the Story 6.9 file. |
| **Developer** (Story 6.9) | Execute the triage and split. Author 4.4's artifact skeleton at start, fill `triageResults` at completion. |
| **Release owner** (Story 6.6) | Consume the decision and triage record in the v2 attestation. |

### Success criteria

1. Story 6.9 completes with the portable tier proven free of non-packable module bindings **by a test**, not by prose.
2. Every one of the 13 files has a recorded outcome — re-expressed, or coupled with a named type and reason.
3. Executed conformance count is monotonic across both tiers against a **generated** pre-split figure.
4. Zero tests removed, zero weakened, zero public-contract additions.
5. `conformance-oracle-tiering-decision-v2` has non-null `triageResults` and is hash-bound by the v2 manifest and attestation.
6. The Epic 5 action item is closed with its decision record, and the v1 target end-state is superseded without any v1 artifact being edited.

### What would falsify this decision

If the triage finds that **most or all** of the 13 files re-express publicly without weakening,
the split is unnecessary and the honest outcome is a single portable project with the
reference deleted — Option B's *result* reached without Option B's cost. Story 6.9 AC1 is
written to permit that outcome. The decision is to *tier*, not to *guarantee two projects*.

---

## Approval

**Approved by:** Jerome, 2026-07-28
**Named-owner approval for FR-20 reclassification** of `TelemetryCardinalityConformanceSuiteTest`,
`TelemetryRedactionConformanceSuiteTest`, and `ConformanceStatusConformanceSuiteTest`:
**granted** — no denominator membership change, no test removed, no assertion weakened.
