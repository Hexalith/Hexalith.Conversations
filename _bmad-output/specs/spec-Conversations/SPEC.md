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

# Conversations V9 Execution Overlay Adoption

## Why

The finalized V9 execution overlay must become discoverable with the existing Conversations specification package while preserving the approved product contract, completed history, v8 technical invariants, and fail-closed implementation state without reinterpretation.

## Capabilities

- **CAP-1**
  - **intent:** Planning consumers can discover the finalized `ARCHITECTURE-EXECUTION-OVERLAY-V9` block as the companion governing the remaining-work execution projection.
  - **success:** A consumer reading this package reaches the final V9 marker in `architecture.md`, retains the unchanged product and UX contract, and observes `PC` as `UNBOUND` with the global implementation hold `ACTIVE`.

## Constraints

- V9 supersedes v8 only for the remaining-work execution projection. Every v8 technical invariant remains binding and read-only; `architecture.md` is the controlling text and this kernel does not narrow it.
- Preserve the canonical PRD and addendum; exactly 124/124 functional requirements comprising 20 initiative FRs and 104 `Feature-FR`s; all 77 `Feature-NFR`s; FR-16 as the sole deferred and non-activated initiative requirement; all 52 UX decisions; and all 28 UX acceptance IDs.
- Preserve Epics 1–5 and completed Stories 6.1, 6.2, and 6.7, including their completed records, accepted baselines, signed evidence, and submodule bindings, as immutable history.
- `PC` is `UNBOUND`, and the global implementation hold remains `ACTIVE`. No implementation is authorized unless the V9 conditions for candidate-bound mechanical validation, an independent IR-0 `READY` assessment, and an explicit release-owner hold lift are all satisfied.
- This adoption changes only the bmad-spec package. Product code, planning companions, completed records, signed evidence, submodules, and the immutable v8 execution view remain unchanged.
- Every file in `companions:` is adopted and read-only to bmad-spec; its original wording and authority remain intact.

## Non-goals

- Binding `PC`, lifting the implementation hold, or publishing the missing V9 authority bundle, successor story contracts, maps, projections, or validators.
- Changing product, technical, requirement, UX, evidence, or completed-history semantics.
- Starting or resuming implementation.

## Success signal

The specification package references the finalized V9 architecture overlay and the unchanged Conversations authority set, explicitly records `PC=UNBOUND` and the active global hold, and passes coherence and preservation validation with no protected artifact modified.
