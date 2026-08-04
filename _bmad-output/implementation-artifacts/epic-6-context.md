# Epic 6 Context: Immutable Historical Corrective Foundation

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Preserve the accepted planning rebaseline, platform-hosting migration, and submodule-promotion completion control as an immutable corrective foundation. This epic matters because successor work depends on trustworthy, candidate-bound hosting and promotion evidence without reopening completed decisions, records, baselines, signed evidence, or submodule bindings. Its bounded exit is the existing `done` state of Stories 6.1, 6.2, and 6.7; unfinished former Epic 6 definitions are provenance only and execute, if authorized, through successor Epics 7-15.

## Stories

- Story 6.1: Rebaseline architecture and planning authority
- Story 6.2: Migrate Conversations to platform-owned hosting
- Story 6.7: Mechanically block incomplete submodule promotions from completion

## Requirements & Constraints

- Treat all three stories and their completion records as read-only history. Preserve their accepted baselines, signed dependencies, test evidence, projection proof, root candidate, and recorded submodule gitlinks byte-for-byte.
- Preserve the initiative boundary: 20 initiative requirements, 104 preserved feature requirements, 77 preserved feature non-functional requirements, 52 UX decisions, and 28 explicit UX acceptance identifiers. FR-16 remains the sole deferred and non-activated initiative requirement.
- Keep this as a behavior-preserving refactor foundation. It authorizes no new Conversations behavior, public-contract change, package-version change, production topology change, or product implementation.
- Conversations owns domain contracts, aggregate behavior, validators, handlers, projection/read-model semantics, domain adapters, telemetry definitions, and client/testing assets. Shared platform libraries own reusable runtime capabilities, while platform deployment owns production composition.
- Retain the Conversations AppHost only as a module-scoped user/end-to-end test fixture. It must remain non-packable, non-publishable, and limited to Conversations surfaces plus required platform dependencies; it is neither a production composition root nor reusable hosting capability.
- Production projection evidence must prove named dispatch through the real platform path, durable tenant-scoped conversation and tenant-index writes, production query results, retry convergence, tenant isolation, bounded failure, deletion, and replay equivalence. Direct writer calls, dependency resolution, mock counts, legacy projection output, or HTTP acceptance alone are insufficient.
- Promotion-bearing work must declare an exact, nonempty set of root `references/...` paths and an availability policy. Every affected or baseline-changed root gitlink must be initialized, clean including untracked files, policy-compliant, and represented by the exact raw mode-`160000` entry in the committed umbrella candidate. Unrelated state warns but does not block.
- Completed Epic 6 does not lift the global implementation hold. Successor implementation requires mechanically valid current authority, an independent candidate-matched readiness result of `READY`, and an explicit release-owner hold-lift decision.

## Technical Decisions

- The canonical domain-host integration is `AddEventStoreDomainService(...)` followed by `UseEventStoreDomainService()`. Generic ServiceDefaults, Aspire/DAPR topology, health, telemetry wiring, query/projection runtime, publication, and subscription plumbing belong on approved public platform surfaces, never behind a Conversations facade.
- Hexalith.EventStore remains the sole write-side and durable conversation-state authority. Projections and query models are derived state; queries must not replay, materialize, or silently repair missing projection data.
- A scoped named `IAsyncDomainProjectionHandler` owns production population of the persisted query store. A projection completes only when both the per-conversation record and tenant index are durable; uncertain partial writes remain incomplete and converge by idempotent retry.
- Submodule discovery reads only the root `.gitmodules`. The promotion gate never initializes, updates, or traverses nested submodules, rejects empty or unevaluated affected scope, and uses stable blockers for review/completion failures.
- Candidate-bound evidence is authoritative. Current worktrees must not replace recorded commits or gitlinks when validating historical records. Story 6.2's mechanical final record and projection proof are immutable inputs; changes to final-record generation belong to successor Epic 7, not this epic.

## UX & Interaction Patterns

UX obligations are preservation-only and remain `preserved-not-activated`. Epic 6 authorizes no product screen, component, navigation, interaction, or visual implementation; activation requires separate approved release authority.

## Cross-Story Dependencies

Epics 1-5 are the historical entry. The immutable completion spine is Story 6.1, then Story 6.7, then Story 6.2: the authority rebaseline enables the promotion gate, and that gate enables acceptance of the platform-hosting migration. Story 6.2 is the hard entry for successor Epic 7 and other dependent successor outcomes. Former Stories 6.3-6.6 and 6.8-6.12 are superseded execution definitions and must not be resumed under Epic 6 identifiers.
