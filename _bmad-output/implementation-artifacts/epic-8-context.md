---
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-20-v22'
---

# Epic 8 Context: Preserved UX Governance

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give product and release owners a deterministic, candidate-bound account of every preserved Conversations UX obligation. The account must expose missing, duplicated, drifted, or silently activated obligations while keeping product UI work outside the current refactoring scope. Epic 8 is complete only when both stories have compatible accepted records and the full UX inventory remains `preserved-not-activated`.

## Stories

- Story 8.1: Generate the versioned UX disposition contract
- Story 8.2: Enforce the 52-decision/28-acceptance zero-gap validator

## Requirements & Constraints

- Preserve exactly 52 decisions (`UX-DR1` through `UX-DR52`) and 28 acceptance IDs: eight safety, fifteen responsive, two accessibility, and one each for leakage, mobile, and performance. Each ID must appear once, in canonical source order, with explicit ownership and disposition.
- Bind the canonical UX specification and requirement map by path, version, and current SHA-256. A changed or missing source invalidates the disposition; historical story mappings remain labeled provenance and cannot become current implementation owners.
- Keep every row and the overall disposition at `preserved-not-activated`. UX accessibility and usability obligations remain traceability commitments pending separate release activation. The preservation contract neither proves that a feature shipped nor grants product UI implementation authority.
- Reject absent, duplicate, or unknown IDs; missing owners or source hashes; source drift; JSON/Markdown divergence or order drift; activation; invalid current-story ownership; and production UI changes. Negative fixtures must demonstrate the specific blocker and restore source bytes exactly.
- Require current, nonempty, passing conformance evidence with no skipped or not-run checks. Final records must bind the story candidate, predecessor record, frozen inventory, source and output digests, and scenario results; a generated rendering cannot replace authoritative JSON.
- The implementation hold remains `ACTIVE`. Architecture V22 completes only the current-authority recovery route and does not authorize story execution, UI scope, sprint transition, or release. Entry requires the separate candidate-matched readiness and owner hold-lift controls.

## Technical Decisions

- The governing backlog authority is Epic V14. Architecture V22 is the last complete architecture marker; it supersedes V16's pending-recovery state while retaining earlier UX preservation decisions. Resolve current authority through the committed marker and candidate-bound resolver, not a historical frontmatter value or a sidecar filename.
- The canonical disposition is one closed, versioned schema with authoritative JSON and digest-bound deterministic Markdown. Its rows carry identity, status, owner, rationale, source path and hash, evidence or control, historical mappings, compatibility, and disclosure safety. The schema, JSON, and Markdown are generated as one coherent bundle.
- The 52/28 inventory comes from the canonical UX specification and requirement map. The later design and experience spines are draft preservation guidance; they do not expand the closed source inventory or activate a screen.
- Story 8.1 and 8.2 use separate story candidates and generated final records. The second record must consume the first disposition and final-record digests. Existing UX sources, historical mappings, product code, accepted evidence, and the frozen planning prefix remain intact across rollback.

## UX & Interaction Patterns

Preserve the intended trust-first investigation flow and safety rules as documented obligations: tenant-safe discovery, source-owned trust and command state, independently authorized evidence detail, redaction across visible and assistive surfaces, distinct denied/stale/degraded states, keyboard and screen-reader access, and safe responsive behavior. These patterns guide disposition and traceability now; visual and interaction delivery requires separate activation authority.

## Cross-Story Dependencies

Epic 7, specifically Story 7.4, is the hard entry to Story 8.1. Story 8.2 requires Story 8.1's accepted, candidate-compatible disposition and final record. Superseded Story 6.4 supplies historical obligations only. Epic 14 later consumes Story 8.2's zero-gap UX evidence for the preservation manifest; Epic 15 revalidates it for release attestation.
