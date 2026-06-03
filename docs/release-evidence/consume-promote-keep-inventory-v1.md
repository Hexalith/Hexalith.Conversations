# Consume / Promote / Keep Inventory (v1)

**Artifact:** `consume-promote-keep-inventory-v1.json` (machine-readable; this `.md` is the human header/summary)
**Status:** `accepted`
**Accepted date:** 2026-06-03
**Baseline commit:** `bf3d052`
**Owning story:** 1.4 — *Accept the canonical Consume/Promote/Keep inventory and record baseline plumbing-LOC*
**Source total:** **35,769 LOC** (`src/Hexalith.Conversations.*`, 8 projects)
**Plumbing baseline (SM-1):** **13,289 LOC (37.15% of source)** — see [Plumbing derivation](#plumbing-derivation)
**Counting method:** `find <paths> -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | xargs wc -l`

## Role (what this artifact is)

This is the **decision spine** of the Conversations Boilerplate Reduction initiative: the single **accepted** inventory that classifies every `Hexalith.Conversations.*` top-level source subtree as **Consume / Promote / Keep**, with evidence (paths, approximate LOC), the target technical-module capability (for Consume/Promote), and the cross-reference to the governing FR + owning Epic 2/3 story. It also records the **baseline plumbing-LOC** figure that Story 5.3 measures SM-1 reduction against.

- **The artifact FR-2 governs.** This is the single inventory whose rows **Story 1.5**'s dispute-resolution + reclassification escape-hatch amends. Challenges and post-acceptance reclassifications are logged as entries in the JSON `changeLog`, **never** by silently editing an accepted row.
- **Every Epic 2/3 move story** traces its "what am I moving and where" to a row here (`fr` + `owningStory` + `targetCapability`).
- **Story 5.3** reads `plumbingBaselineLoc` to compute SM-1 reduction (assumed-target ≥40% — **OQ-2 stays open**; only the *baseline* is fixed here).

**This story moves no code.** Zero `src/` changes, zero `tests/` behavior changes. It *names and classifies* the plumbing later epics move.

## Classifications

| Class | Meaning | In plumbing baseline? |
|---|---|---|
| **Consume** | Plumbing that maps to an **existing** technical-module capability the owning Epic 2 story adopts in place of hand-rolled local code. | Yes |
| **Promote** | Plumbing with **no existing** shared capability; the owning Epic 3 story extracts/promotes a generic capability and Conversations adopts it. | Yes |
| **Keep** | Domain logic, not plumbing to move in this initiative. *Keep ≠ frozen* — it may still change for domain reasons. | No |

## Inventory (26 areas; every subtree exactly once, single label)

### Consume (6 rows · 7,037 LOC)

| Area | Paths | LOC | Target capability | FR / Story |
|---|---|---:|---|---|
| query-cursor-orchestration | `Server/Queries` | 2,076 | `IDomainQueryHandler` + `IQueryCursorCodec` + `QueryCursorScope` | FR-4 / 2.3 |
| shared-host-api | `Server/Api` + `Server/EventStore` + Server root | 906 | `AddEventStoreDomainService` shared host | FR-3 / 2.1 |
| server-command-dispatch-idempotency | `Server/CommandHandlers` | 1,893 | `EventStoreAggregate<TState>` dispatch + idempotency-bridge | FR-7 / 2.2 |
| core-aggregate-dispatch-replay-idempotency | `Replay` + `Idempotency` (core) | 1,896 | `EventStoreAggregate<TState>` reflection dispatch | FR-7 / 2.2 |
| generic-serialization-converters | 5 generic converter files (Contracts/Serialization) | 215 | Commons `TypeMapper` + generic converters | FR-8 / 2.6 |
| duplicate-test-fakes | `Testing/Fixtures/RepositoryTestContext.cs` | 51 | `Hexalith.EventStore.Testing` fakes/assertions | FR-9 / 2.7 |

### Promote (7 rows · 6,252 LOC)

| Area | Paths | LOC | Target capability | FR / Story |
|---|---|---:|---|---|
| projection-orchestration | `Server/Projections` | 1,800 | `IDomainProjectionHandler` projection seam | FR-6 / 2.5 |
| diagnostics-telemetry-scaffolding | `Server/Diagnostics` + `Contracts/Diagnostics` | 2,442 | shared meter/counter/classifier scaffolding | FR-15 / 3.3 |
| tenant-access-projection | `Server/TenantAccess` | 1,086 | generic `TenantAccessProjectionHandler<TEvent,TProjection>` | FR-11 / 3.2 |
| publication-transport-marshaling | 6 transport files (Server/Publication) | 422 | shared Aspire/Dapr hosting + publication transport | FR-13 / 3.5 |
| typed-client-registration | client impl + DI (Client) | 479 | generic typed-HttpClient registration | FR-12 / 3.1 |
| service-defaults-greenfield | `ServiceDefaults` | 13 | shared ServiceDefaults base | FR-10 / 3.4 |
| apphost-greenfield | `AppHost` | 10 | shared Aspire/Dapr hosting base | FR-13 / 3.5 |

### Keep (13 rows · 22,480 LOC)

| Area | Paths | LOC | Note |
|---|---|---:|---|
| query-filters-response-shapes | `Contracts/Queries` | 3,251 | domain query DTOs |
| projection-field-selection-freshness-shape | `Contracts/Projections` | 1,175 | projection read-model shape |
| **governance-evidence-vocabulary** | `Server/Governance` + `Contracts/Governance` | 4,337 | **promoteLaterCandidate** (OQ-3) |
| **hydration-reference-resolution** | `Server/Hydration` | 629 | **promoteLaterCandidate** (OQ-3) |
| validation-business-rules | `Validation` (core) | 2,101 | conversation business rules |
| aggregate-state-event-domain | `Aggregates`+`State`+`Events`+`Commands`+root (core) | 1,769 | domain aggregate shape |
| domain-rule-serialization-converters | ClosedVocabulary + 2 domain converters | 432 | domain closed-vocabulary rules |
| publication-failure-taxonomy | 3 outcome/result/diagnostic files | 131 | Conversations-safe status mapping |
| conformance-contract-types | `Contracts/Conformance` | 2,196 | release/conformance surface |
| domain-contract-types | Commands/Events/Errors/Identifiers/Participants/Results/TrustStates/Versioning + root | 3,738 | public domain DTOs |
| client-surface-dtos | `IConversationClient` + `ConversationClientResult` | 140 | adopter-facing client surface |
| domain-conformance-fixtures | `Testing` minus RepositoryTestContext | 1,704 | domain conformance fixtures |
| admin-web-frontcomposer | `Admin.Web` | 877 | FrontComposer-generated admin |

> **Two `promoteLaterCandidate: true` areas** (governance, hydration) are classified **Keep now** with the OQ-3 boundary note: classified-and-kept-now, **not** silently promoted. They do **not** become Promote rows in this pilot.

## Resolved mixed (addendum) areas — split by real source boundary

The addendum deliberately used mixed labels where a subtree splits. FR-1 requires **exactly one** classification per row, so each is split into two single-label rows whose LOC sum to the measured folder total:

| Addendum mixed area | Promote/Consume row | Keep row | Folder total |
|---|---|---|---|
| Queries (1) | query-cursor-orchestration (Consume, 2,076) | query-filters-response-shapes (1,175→**3,251**) | 5,327 |
| Projections (3) | projection-orchestration (Promote, 1,800) | projection-field-selection-freshness-shape (1,175) | 2,975 |
| Aggregate (12) | core-aggregate-dispatch-replay-idempotency (Consume, 1,896) | aggregate-state-event-domain (1,769) | 3,665 |
| Serialization (10) | generic-serialization-converters (Consume, 215) | domain-rule-serialization-converters (432) | 647 |
| Testing (11) | duplicate-test-fakes (Consume, 51) | domain-conformance-fixtures (1,704) | 1,755 |
| Publication (8) | publication-transport-marshaling (Promote, 422) | publication-failure-taxonomy (131) | 553 |
| Client | typed-client-registration (Promote, 479) | client-surface-dtos (140) | 619 |

## Plumbing derivation

**`plumbingBaselineLoc = Σ(Consume) + Σ(Promote) = 7,037 + 6,252 = 13,289 LOC` (37.15% of 35,769).** Keep (22,480) is domain logic, excluded.

**Addendum first-pass ≈18,000 (~50%) — CORRECTED → 13,289 (37.15%).**
The addendum's first pass treated the governance/verification/audit area (4,337) and read-time hydration (629) as promotable plumbing and folded more of Contracts into the plumbing total. Resolving governance + hydration as **Keep-now per OQ-3** (promoteLaterCandidate, not moved in this pilot) and attributing the Contracts query-filter / projection-shape / conformance / domain DTOs and the Testing domain conformance fixtures as **domain surface** moves ~4.7k LOC from plumbing to Keep. The lower figure is the **honest SM-1 denominator**: it counts only what is actually a move target in this pilot. Story 5.3 measures reduction against **13,289**.

## Reconciliation

`Σ(area LOC) = Consume 7,037 + Promote 6,252 + Keep 22,480 = **35,769** = sourceTotalLoc`. Unattributed remainder: **0**. Every `.cs`-bearing top-level subtree of all 8 projects is attributed to exactly one area — no area unclassified, dual-classified, or double-counted (FR-2 invariant holds at acceptance, asserted by `ConsumePromoteKeepInventoryValidationTest`).

## Versioning & governance

`-v1` is **immutable once accepted**. Post-acceptance reclassifications / dispute resolutions (Story 1.5) **append** a logged entry to the JSON `changeLog`; they never rewrite an accepted row. A structurally new inventory would be `-v2`. This mirrors the sibling `docs/release-evidence/*-v1.*` artifacts (`release-baseline`, `oracle-blind-spot-analysis`, `at-risk-test-register`, `public-contract-shape-baseline`).

### changeLog

| entryId | type | areaId | by | resolution | summary |
|---|---|---|---|---|---|
| `CL-shared-host-api-challenge-1` | challenge | `shared-host-api` | Story 2.2 (FR-7) | upheld | Story 2.2 consumes (deletes) the dead command-status idempotency-bridge shim — the sole `.cs` file under this area's `Server/EventStore/**` glob. **Upheld:** the area stays **Consume**; consuming a Consume-classified file realizes the accepted call (it is not a reclassification), so `classification`, `approxLoc` (906), and `paths` are byte-unchanged. The append records that the now-empty glob is an accounted-for consumption, not a stale path. Cross-ref: `at-risk-test-register-v1` `story22StructuralDispositions`. |

## Open questions NOT resolved here

- **OQ-1** per-promotion landing zone — downstream architecture run (gates Epic 3).
- **OQ-2** SM-1/SM-2 numeric *targets* — only the *baseline* is fixed here.
- **OQ-3** governance/hydration promote-later boundary — recorded as Keep-now candidates, not decided here.
