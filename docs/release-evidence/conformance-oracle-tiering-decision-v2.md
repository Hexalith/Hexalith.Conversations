# Conformance Oracle Tiering Decision v2

**Artifact:** `conformance-oracle-tiering-decision`
**Version:** 2
**Status:** approved — decided, not yet executed
**Decision date:** 2026-07-28
**Approved by:** Jerome
**Approval reference:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md`
**Authority:** `epic-6-authority-2026-07-28-v5` / `conversations-architecture-2026-07-28-v5`
**Machine-readable companion:** [`conformance-oracle-tiering-decision-v2.json`](conformance-oracle-tiering-decision-v2.json)

---

## What this artifact is

This records the long-term disposition of the
`tests/Hexalith.Conversations.Conformance.Tests -> src/Hexalith.Conversations.Server`
project reference. It closes the Epic 5 action item *"Decide the long-term path for
residual Conformance.Tests to Server coupling,"* which was opened by Story 1.1 AC3 and
deferred through Stories 1.3, 3.3, and 5.2.

It is a **decision record, not a completion record.** `triageResults` is `null` until
Story 6.9 fills it. A reader can always distinguish decided from executed by that field.

## Decision

**Tier the oracle.** The residual coupling is a mislabeling, not a defect.

Story 1.1 recorded the reference as an oracle-survivability risk on a precise and, at the
time, correct basis: the oracle compiled against the plumbing assembly the refactor was
about to move. Epics 2 and 3 moved that plumbing to `Hexalith.EventStore.DomainService`,
`Hexalith.Commons.*`, and platform deployment. What remains in
`Hexalith.Conversations.Server` — tenant-access guards, the idempotent command executor,
the governance audit sink, the projection materializer, the diagnostics classifiers — is
exactly what the Epic 6 corrected ownership spine assigns to Conversations.

The premise expired. The action item did not. The oracle asserts two different contracts
and has never distinguished them; the fix is to make the distinction real and mechanically
checkable, not to keep chasing removal of a reference whose justification no longer holds.

## The two tiers

| Tier | Project | Binds | Property |
|---|---|---|---|
| **Portable** | `tests/Hexalith.Conversations.Conformance.Tests` | Contracts, Client, Testing | References no non-packable module assembly — asserted by a test over the resolved compile surface |
| **Module-internal** | `tests/Hexalith.Conversations.Conformance.Server.Tests` | `Hexalith.Conversations.Server` | Coupling is declared and correct, not a defect scheduled for removal |

Both tiers are release-gate.

## Invariants

1. Tier membership governs what an assertion may bind, never whether it runs.
2. **Widening the public contract to relocate an assertion is prohibited.** Test
   reachability is not a reason to expose a domain implementation type.
3. **Weakening an assertion to relocate it is a conformance failure.** A check that cannot
   be re-expressed at full strength belongs in the module-internal tier, and recording it
   there is a correct outcome, not a deferral.
4. Executed conformance test count is monotonic across both tiers.
5. The pre-split count is derived from a machine-readable result artifact, never
   transcribed.

Invariants 2 and 3 are the load-bearing ones. Without them, "tier the oracle" degrades
into either public-contract bloat or quiet assertion weakening — and both would read as
success in the artifacts.

## Supersession

This artifact supersedes one field of an immutable predecessor. **No v1 artifact is
edited.**

> **Superseded:** `at-risk-test-register-v1.projectReferenceDisposition.targetEndState`
> — *"Public-surface suites no longer transitively depend on the Server plumbing assembly."*

That end-state was written against the pre-Epic-2 assembly. It is no longer the correct
target, and retaining it would keep an unmeetable promise on the record.

## Alternatives rejected

| Option | Why rejected |
|---|---|
| Publicize Server seams, delete the reference | Widens the 196-type public contract solely for test reachability, triggering FR-20 contract-shape approval and permanently worsening the adopter surface to solve a test-structure problem. |
| Accept permanently, no structural change | Leaves the adopter-verifiability intent unmeetable for the whole oracle and keeps the distinction as prose — the exact form that allowed three deferrals. |
| Defer to post-initiative backlog | Story 6.6 is last and must attest on current evidence. An open Architect/Quality decision at attestation time is the failure mode Epic 6 exists to correct. |

## FR-20 reclassification

`prd.md` FR-20 consequence 4 requires named-owner approval to *reclassify* a manifested
test. Three of the thirteen coupled files are in the frozen denominator per
`release-baseline-v1.json`:

- `TelemetryCardinalityConformanceSuiteTest`
- `TelemetryRedactionConformanceSuiteTest`
- `ConformanceStatusConformanceSuiteTest`

**Approval granted by Jerome, 2026-07-28.** Denominator membership is unchanged; only the
recorded tier changes. Tests removed: 0. Tests weakened: 0. A versioned manifest update is
required.

The remaining ten coupled files are post-baseline additions and are not in the frozen
denominator.

## Execution

Story 6.9 triages the 13 files listed in
`removed-test-justification-ledger-reconciliation-v1.json#projectReferenceDisposition.residualCouplingInventory`,
attempting public re-expression first and assigning to the module-internal tier only what
cannot move at unchanged strength.

**A single portable project with the reference removed is a valid successful outcome** if
the triage proves the assertions re-express cleanly. This decision commits to tiering the
oracle, not to producing two projects.

## Non-claims

- Does not assert the split has been executed.
- Does not assert any file is re-expressible; the triage decides.
- Does not waive, weaken, or reduce the FR-20 denominator.
- Does not authorize any production source change.
- Does not edit, mutate, or reinterpret any immutable v1 evidence artifact.
