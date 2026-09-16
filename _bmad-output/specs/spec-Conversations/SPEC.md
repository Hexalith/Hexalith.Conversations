---
id: SPEC-Conversations
companions:
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  - ../../planning-artifacts/ux-design-specification.md
  - ../../planning-artifacts/ux-requirement-map.md
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md
  - ../../planning-artifacts/architecture.md
  - ../../project-context.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract. The companions remain owned by their originating workflows and are not rewritten by this package.

# Conversations V15 Current Authority And Runtime Convergence

## Why

The finalized V15 architecture overlay must govern current technical and execution-authority discovery while the accepted Conversations product scope, completed history, and fail-closed execution state remain intact. Consumers need one current contract that distinguishes immutable publication evidence from live authority and prevents stale planning state from authorizing work.

## Capabilities

- **CAP-1**
  - **intent:** Planning and execution consumers can determine the current Conversations technical authority, effective hold state, AR-15 recovery readiness, and Epic 16 entry readiness.
  - **success:** A consumer selects the last complete V15 architecture marker, treats the V9 bundle and V15-V21 checkpoint sidecars as point-in-time evidence, resolves the current execution hold through AD-3, observes AR-15 as `BLOCKED_BOOTSTRAP_AUTHORITY`, and rejects the V14 Epic 16 carriers as execution-sufficient.

## Constraints

- V15 is the current technical authority together with every earlier invariant it does not explicitly supersede. `architecture.md` is controlling and this kernel does not narrow AD-1 through AD-10 or inherited security, privacy, authority, and fail-closed rules.
- Preserve the canonical PRD and addendum; exactly 124/124 functional requirements comprising 20 initiative FRs and 104 `Feature-FR`s; all 77 `Feature-NFR`s; FR-16 as the sole deferred and non-activated initiative requirement; all 52 UX decisions; and all 28 UX acceptance IDs.
- Preserve Epics 1–5 and completed Stories 6.1, 6.2, and 6.7, including their completed records, accepted baselines, signed evidence, and submodule bindings, as immutable history.
- AD-3 controls live execution state: the selected protected checker must return a validated, explicitly scoped `PASS` at the evaluated commit before a scoped lift can apply. Missing, drifted, blocked, failed, or nonzero results resolve to `ACTIVE`; at the V15 evaluated state, `V21_DESCENDANT_GITLINK_DRIFT` keeps the effective execution hold `ACTIVE`. This is a recomputed fail-closed result, not a new global hold record.
- AR-15 is `BLOCKED_BOOTSTRAP_AUTHORITY` until the AD-3 no-bypass ruleset and identity-and-digest-pinned external one-time validator exist. Candidate-owned workflow or validator bytes cannot supply that authority, and recovery preserves `ACTIVE`.
- The V14 Epic 16 and Stories 16.1–16.3 carriers remain immutable backlog evidence but are not execution-sufficient. No Epic 16 story may enter `ready-for-dev`, implementation, or review until append-only successor epic authority, story contracts, inventories, graph, and validators cite AD-5 through AD-8 and pass the current marker-driven resolver.
- Story 16.1 requires the AD-5 production operational envelope and AD-6 atomic tenant-projection mutation and recovery contract. Story 16.2 remains downstream of 16.1 and requires AD-7 lifecycle/watermark authority plus AD-8 replay identity, time, and canonical-byte rules; AD-8 replaces V14 whole-document timestamp byte equality with deterministic replay-state equality and semantic freshness checks.
- Story 16.3 remains downstream of 16.2; its successor carrier must align with AD-5 through AD-8 and satisfy the V15 AppHost-baseline revisit condition before entry.
- This refresh changes only the bmad-spec package. Architecture, PRD, epics, UX artifacts, story status, product code, dependencies, submodules, gitlinks, completed records, signed evidence, and public value sets remain unchanged.
- Every file in `companions:` is adopted and read-only to bmad-spec; its original wording and authority remain intact.

## Non-goals

- Lifting any hold, unblocking or executing AR-15, or publishing successor authority, Epic 16 carriers, inventories, graphs, or validators.
- Changing product, technical, requirement, UX, evidence, or completed-history semantics.
- Changing story status; starting implementation; accepting a story; releasing; or pushing.

## Success signal

The specification package selects V15, preserves accepted scope and completed history, records the current scoped `ACTIVE` execution hold and AR-15 blocker, qualifies Epic 16 against AD-5 through AD-8, contains no stale candidate-state or universal-hold claim, and passes coherence and preservation validation with only the spec workspace changed.
