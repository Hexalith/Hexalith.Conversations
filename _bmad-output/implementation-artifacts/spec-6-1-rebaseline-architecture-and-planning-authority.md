---
title: 'Story 6.1: Rebaseline Architecture and Planning Authority'
type: 'chore'
created: '2026-07-15'
status: 'in-progress'
baseline_revision: 'f31aa5ada2e37e1ec5f3e4b8e907525b37da863f'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** The architecture and epic plan still authorize Conversations-owned hosting and treat superseded feature-planning assumptions as current, so corrective implementation lacks one PRD-aligned ownership, evidence, and sequencing model.

**Approach:** Rebaseline architecture to the finalized initiative PRD/addendum and approved July 15 corrections, append Epic 6 authority without rewriting completed history, regenerate the derived Epic 6 context, and enforce the resulting planning contract with focused conformance tests.

## Boundaries & Constraints

**Always:** Treat `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md` plus `addendum.md` as initiative authority; distinguish its 20 initiative FRs from the preserved 104 Feature-FRs and 77 Feature-NFRs; preserve the accepted 13,289-LOC SM-1 baseline; use `READY FOR CORRECTIVE IMPLEMENTATION ONLY`; keep the 24 completed stories, Epics 1-5, retrospectives, `done` states, and signed v1 evidence immutable; append approved corrections only; include the later-approved Story 6.7 and the order `6.1 -> 6.7 -> 6.2`; never initialize or traverse nested submodules.

**Block If:** The approved PRD/addendum and July 15 proposals yield contradictory ownership, OQ, performance, or Story 6.7 decisions; a required FR landing zone cannot be verified on an existing public platform surface; or the historical epic prefix / signed v1 evidence cannot be preserved byte-for-byte.

**Never:** Modify the finalized PRD/addendum, historical Epics 1-5 text, retrospectives, signed v1 evidence, production/runtime source, solution membership, submodule contents or gitlinks; remove AppHost/ServiceDefaults projects (Story 6.2); change UX governance (6.4), the thin template (6.5), release evidence (6.6), or implement the promotion gate (6.7); activate FR-16 or preserved feature scope.

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/architecture.md` -- stale architecture authority, module/platform boundary, target tree, workflow, performance gate, and readiness conclusion.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` -- historical Epic 1-5 plan requiring an EOF-only corrective authority amendment and Epic 6.
- `_bmad-output/implementation-artifacts/epic-6-context.md` -- derived developer context that must include Story 6.7 and corrected dependency order.
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` -- new executable contract for architecture/epic authority and append-only preservation.
- `tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs` -- existing byte-integrity guard for frozen SM-2/v1 evidence.
- `src/Hexalith.Conversations.AppHost/` and `src/Hexalith.Conversations.ServiceDefaults/` -- current pre-6.2 drift to describe as migration input, not target ownership.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.{DomainService,ServiceDefaults,Aspire}/` and `references/Hexalith.Commons/src/libraries/Hexalith.Commons.{TenantAccess,Http,Serialization,Diagnostics}/` -- read-only public landing-zone evidence.

## Tasks & Acceptance

**Execution:**
- `_bmad-output/planning-artifacts/architecture.md` -- amend the existing document in place: rebaseline provenance, scope, starter, ownership, FR-10-FR-16 landing-zone register, OQ-1-OQ-5 states, SM-C2, target tree, verification/workflow, promotion-completion invariant, and readiness language; label local hosting projects as pre-6.2 drift rather than target architecture. Preserve all unaffected, still-binding domain/runtime decisions, including versioned events and mixed-stream replay/upcasting, EventStore precedence with quarantine/rebuild, fail-closed Parties writes and policy-defined read degradation, idempotency payload-mismatch/unknown-outcome handling, and approved legal-policy exceptions to immutable history. Prescribe the canonical `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()` pair; never teach direct `MapEventStoreDomainService()` use.
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` -- append the approved authority overlay, exact 24-row historical disposition table, corrective FR coverage, Epic 6 Stories 6.1-6.7, and dependency order at EOF without changing the existing prefix. Preserve the approved denominator literally: all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria; FR-16 is the only initiative non-activation. A delivered-to-inactive transition or compatible contract change requires named owner approval, rationale, and evidence.
- `_bmad-output/implementation-artifacts/epic-6-context.md` -- regenerate from the amended epic plan so Story 6.7, root-only submodule rules, and `6.1 -> 6.7 -> 6.2` are carried forward.
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` -- parse and assert canonical frontmatter authority, exact table keys and nonempty semantics, full FR surface lists, deferred FR-16, exactly one resolved row per OQ, a nonempty versioned hot-path inventory frozen before baseline with one-to-one post dispositions, reproducible `post P95 <= 1.05 x baseline` semantics, target ownership/tree, corrective-only readiness, append-only historical preservation, version-bound Epic 6 overlay/context correspondence, Story 6.7 sequencing, and the declared-promotion gitlink invariant. Verify named platform APIs are public through signature-aware source checks or compile-time/reflection evidence; raw substring presence is insufficient.
- `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` -- replace current-file equality for superseded v1 inputs with a generic, shallow-clone-safe historical binding anchored to the immutable signed report/decision and its declared source identity; never substitute this workflow's baseline revision as v1 provenance. Current corrected artifacts remain governed by the new authority test.

**Acceptance Criteria:**
- Given the finalized initiative authority, when a maintainer reads the architecture, then it distinguishes initiative from preserved requirements, assigns FR-10-FR-15 to the approved platform surfaces, defers FR-16, resolves OQ-1-OQ-5, and contains no target-state Conversations AppHost/Aspire/ServiceDefaults ownership.
- Given the frozen pre-refactor benchmark envelope, when SM-C2 is evaluated, then every identified command/read hot path requires post-refactor P95 no greater than 1.05 times baseline under identical workload, data, concurrency, environment/runtime, tooling, warm/cold classification, repetitions, and commit-bound raw evidence.
- Given the historical epic plan and signed v1 record, when Story 6.1 completes, then the original epic bytes and v1 evidence remain unchanged while exactly one append-only authority overlay contains all 24 dispositions, corrective FR-3/10/13/17-20 coverage, and Stories 6.1-6.7.
- Given promotion-bearing corrective work, when planning dependencies are evaluated, then 6.1 precedes 6.7, 6.7 and the frozen benchmark precede 6.2 completion, 6.2 precedes 6.5, and 6.6 remains last.
- Given the amended planning artifacts, when the focused conformance executable runs, then it rejects stale provenance, local-host target ownership, missing/incorrect landing zones or OQs, weakened SM-C2 evidence, historical mutation, missing Story 6.7, or an incomplete promotion-completion invariant.

## Spec Change Log

### 2026-07-15 — Review pass 1 re-derivation
- Trigger: the first implementation over-compressed the architecture, removed still-binding safety decisions, taught the lower-level mapper, weakened approved Epic 6 semantics, and added verification that could pass on token presence or the wrong historical provenance.
- Amendment: tasks now require in-place preservation of unaffected architecture constraints, the canonical domain-host pair, literal preservation denominators/approval rules, a frozen versioned hot-path inventory, semantic/version-bound authority checks, public-surface verification, and a shallow-clone-safe signed-v1 binding.
- Known-bad state avoided: a green planning gate that silently drops replay, projection, participant, idempotency, or legal safeguards; permits denominator drift; derives a host without default endpoints; or binds v1 evidence to the workflow baseline.
- KEEP: retain the corrected authority chain, exact FR landing-zone ownership, OQ decisions, SM-C2 formula/envelope, current-versus-target topology distinction, byte-identical Epic 1-5 prefix and signed v1 files, all 24 dispositions, Stories 6.1-6.7 with `6.1 -> 6.7 -> 6.2`, regenerated context, lifecycle synchronization, zero runtime/submodule changes, and the previously green focused/full conformance lanes.

## Review Triage Log

### 2026-07-15 — Review pass
- intent_gap: 0
- bad_spec: 9: (high 7, medium 2, low 0)
- patch: 0
- defer: 0
- reject: 8: (high 3, medium 4, low 1)
- addressed_findings:
  - `[high]` `[bad_spec]` Restore still-binding replay/upcasting, projection precedence/rebuild, Parties degradation, idempotency, and legal-policy safety decisions removed by the rewrite.
  - `[high]` `[bad_spec]` Require `AddEventStoreDomainService` plus `UseEventStoreDomainService` and prohibit the lower-level mapper as authoring guidance.
  - `[high]` `[bad_spec]` Preserve the approved manifest denominator and restrict non-activation so mandatory initiative requirements cannot disappear.
  - `[medium]` `[bad_spec]` Require named approval, rationale, and compatibility evidence for delivered-to-inactive or compatible-change dispositions.
  - `[high]` `[bad_spec]` Freeze a nonempty versioned hot-path inventory before baseline capture so SM-C2 cannot cherry-pick the denominator.
  - `[high]` `[bad_spec]` Bind Epic 6 overlay and generated context semantics/version rather than checking only identifiers and phrases.
  - `[high]` `[bad_spec]` Verify complete public landing-zone surfaces with signature-aware evidence instead of incomplete source substrings.
  - `[high]` `[bad_spec]` Anchor superseded v1 inputs to signed historical provenance without depending on this workflow baseline or shallow-clone object availability.
  - `[medium]` `[bad_spec]` Strengthen target-tree, provenance, OQ, and table validation so equivalent ownership or contradictory rows cannot pass outside one searched block.

## Design Notes

The architecture describes two states explicitly: current local AppHost/ServiceDefaults assets are pre-6.2 migration evidence, while the target domain module owns only contracts, behavior, handlers, projections, adapters, domain telemetry definitions, and optional domain UI. Missing generic capability is extended in its owning platform surface, never wrapped in a Conversations facade.

The promotion invariant is declarative here and implemented in Story 6.7: affected root-declared submodules must be clean, the declared commit must satisfy availability policy, and the committed umbrella revision must contain the exact mode-`160000` gitlink. Only declared promotions and changed gitlinks block; unrelated state warns.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release /nr:false /m:1` -- expected: zero warnings and errors.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest` -- expected: all authority and preservation checks pass.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.OqTwoTargetInterpretationDecisionValidationTest` -- expected: frozen SM-2 and signed v1 hashes remain valid.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests` -- expected: the full conformance suite passes.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.
