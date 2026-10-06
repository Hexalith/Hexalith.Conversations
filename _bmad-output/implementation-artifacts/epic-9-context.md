---
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-20-v22'
---

# Epic 9 Context: Portable Conformance Oracle

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give domain authors an objectively tiered conformance oracle that they can exercise through shipped Conversations packages while retaining complete verification of module-owned behavior. Portability must be demonstrated from resolved dependencies, and the split must preserve every approved assertion, its strength, and the frozen FR-20 denominator. Epic completion requires compatible accepted records for both stories.

## Stories

- Story 9.1: Freeze the conformance assertion inventory, tier decisions, digest, and approvals
- Story 9.2: Make the portable tier structural and prove complete monotonic tier execution

## Requirements & Constraints

- Freeze every pre-split test-case identity and executable assertion site against machine-readable execution evidence and source bytes. Every assertion must occur exactly once with its source identity, source hash, result identity, canonical strength material, and strength digest. Missing, duplicate, renamed, weakened, or unassigned assertions block acceptance.
- Tier membership governs permitted bindings and never makes an assertion optional. Both tiers remain release gates, execute nonzero coverage, and report zero failed, skipped, or not-run checks. Their combined identities must equal the frozen pre-split set and their combined executed count cannot decrease. Derive the baseline count from a machine result; a transcribed total is insufficient.
- Preserve the accumulated FR-20 denominator and public contract baseline. The immutable pre-refactor floor contains 14 suites and 214 tests, with seven required behavior categories: tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, and governance audit-pairing. Tiering cannot delete, replace, aggregate away, waive, or substitute denominator coverage.
- Retain unchanged identities and manifest membership for `TelemetryCardinalityConformanceSuiteTest`, `TelemetryRedactionConformanceSuiteTest`, and `ConformanceStatusConformanceSuiteTest`. Any tier reclassification must bind a named owner, approval identity, rationale, and versioned manifest update without reducing membership.
- Do not widen public contracts to reach an implementation assertion. If public Contracts, Client, or Testing cannot express an assertion at full strength, assign it to the module-internal tier with the exact internal type and reason. The Quality owner decides tier membership through a digest-bound per-assertion inventory.
- Preserve immutable v1 evidence, accepted baselines, existing conformance tests, and superseded Story 6.9 partial work. A v2 disposition links the protected predecessor by digest and never edits it. This work authorizes test structure and evidence changes; production source, package versions, public APIs, signed evidence, and submodule content retain their existing boundaries.

## Technical Decisions

- The governing backlog is Epic V14 and the governing technical architecture is V22, derived from the current authority state and last complete architecture marker. The complete Epic 9 definitions are carried by the relocated canonical backlog; the root draft has unresolved epic placeholders. The immutable atomic contracts separately retain `epic-6-authority-2026-08-03-v10` and `conversations-architecture-2026-08-03-v10`. Story 9.1 binds its exact source section by SHA-256 `5b62af3b04bff71cdf05c71a88c71ccf1c084c25ca87a506747bb4b9e0cd5358`. Carried DC-9 adds the Quality-owner strength discipline without replacing either story or activating product scope.
- Distinguish two contracts. The portable tier binds only the shipped `Hexalith.Conversations.Contracts`, `Hexalith.Conversations.Client`, and `Hexalith.Conversations.Testing` surfaces. The module-internal tier may bind `Hexalith.Conversations.Server` for domain-owned guards, execution, governance, materialization, and diagnostics. Prove portability from evaluated MSBuild dependencies and resolved transitive compile assets, including the absence of any non-packable module assembly.
- Generate one closed versioned disposition schema, authoritative JSON, and deterministic digest-bound Markdown. Bind authority, story candidate, decision digest, pre-split result, assertion rows, denominator suites, approvals, protected predecessor, and rendered output. Each assertion carries either an exact public replacement or an exact internal type and rationale; an overall approval cannot conceal missing row evidence.
- Preserve canonical strength material and its hash across tier migration. The carried strength definition includes the bound-assembly set, asserted-behavior identity, and negative-case count. Record justified public re-expression explicitly; never treat a changed or absent strength hash as equivalent proof.
- Use separate committed story candidates, distinct from the frozen planning candidate. Final records derive facts from current machine results and Git evidence and bind the predecessor chain, inventories, input/output hashes, approvals, and scenario outcomes. Both tier projects must be declared in the solution and completion inventory so neither can be silently omitted.
- Mandatory negative fixtures cover assertion deletion, duplication, rename and weakening; absent tier, reason, owner approval, project or completion declaration; denominator drift; public widening; v1 mutation; nonportable compile references; skipped, empty or regressed execution. Each fixture must trigger its specific blocker and restore bytes exactly.
- Routine work follows `docs/runbooks/current-change-validation.md`: focused current-tree validation and relevant tests govern new changes. Earlier authority and hold records remain immutable historical evidence and do not create an operational prerequisite for this invoked story. Generated final records remain required here because the Epic 9 contracts explicitly require them; their evidence must satisfy the story-specific contract.

## Cross-Story Dependencies

Story 7.4 is the hard entry to Story 9.1 and supplies the candidate-compatible completion-record machinery. Story 9.2 requires Story 9.1's accepted frozen disposition and final-record digests before relocating assertions. Superseded Story 6.9 supplies preserved obligations only. Story 9.2's accepted identities, tier proofs, strength digests, and execution results feed Epic 10's evidence boundary, Epic 13's projection-proof validation, Epic 14's preservation manifest, and Epic 15's release revalidation.
