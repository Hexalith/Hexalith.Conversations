---
title: "Conversations Boilerplate Reduction — A Thinner Domain-Authoring Surface"
status: final
created: "2026-06-02"
updated: "2026-07-14"
---

# PRD: Conversations Boilerplate Reduction

## 0. Document Purpose

This PRD defines a **refactoring initiative**, not a feature. Its audience includes the owners of Hexalith.Conversations and shared technical modules, as well as those responsible for genuinely affected `Hexalith.Tenants` contracts, architecture, epics/stories, and implementation. It states what plumbing leaves Conversations, what remains, scope, sequencing, and acceptance evidence. Package ownership, extracted API shapes, migration mechanics, the grounded inventory, and duplication evidence live in the companion [addendum.md](addendum.md). §14 embeds the authoritative preserved product-contract baseline: it constrains the refactor without adding feature-delivery scope or asserting that legacy roadmap items shipped. Refactoring requirements use stable `FR-*` IDs; preserved product requirements use `Feature-FR*` and `Feature-NFR*`; `[ASSUMPTION]` entries are indexed in §13.

## 1. Vision

Hexalith.Conversations is roughly 35,800 lines of source, and about half of it is plumbing — DI ceremony, query-handler and cursor machinery, read-model store wiring, projection orchestration, tenant-access scaffolding, serialization converters, telemetry boilerplate, and wrappers around platform-owned Aspire/Dapr hosting. None of it is *about conversations*. Worse, the same plumbing is copy-pasted across the sibling domain modules (Folders, Projects, Memories, Parties): the same 80-line tenant-access handler, the same wrappers around platform-owned service defaults, the same typed-HttpClient registration. Every new business-domain module pays this tax again before it writes a single line of domain logic.

This initiative removes that tax. Conversations becomes the pilot: anything in it that is *not specific to conversations* either gets **consumed** from a technical module that already offers it, **extended in its platform-owned home** when an existing capability is partial, or **promoted** — extracted, generalized, and lifted into a technical module so every domain module inherits it instead of re-implementing it. What stays in Conversations is conversation logic: the validation rules, the aggregate behavior, the events and the read-model shapes. Exact SDK seams and migration mechanics are cataloged in the [addendum](addendum.md).

The payoff is two-sided and both sides matter equally. Conversations itself sheds a large, measurable share of its plumbing. And the *next* business-domain module — and every retrofit of an existing one — starts from a thin, documented authoring template instead of a blank file and a tradition of copy-paste. The whole exercise is held to a hard line: external contracts and the release-gate behaviors (tenant isolation, governance/audit pairing, idempotency, redaction replay, projection freshness) are preserved and proven by conformance tests. We are making the module cheaper to build, not changing what it does.

## 2. Implementation Decision and Readiness Snapshot

| Decision area | Approved state |
|---|---|
| Outcome | Use Conversations as the pilot for consuming, extending, and promoting shared platform plumbing while leaving conversation-specific behavior in the domain module. |
| Pilot scope | FR-1 through FR-15 and FR-17 through FR-20 are in scope; FR-16 is deferred. Fleet migration and unconsumed promotions remain follow-on work. |
| Preservation gate | FR-20 and SM-C1 are authoritative: 100% of the frozen, versioned pre-refactor preservation manifest must pass, with no unapproved public-contract change or silent denominator reduction. |
| Performance gate | SM-C2 is authoritative: post-refactor P95 command/read latency may be no more than 5% worse than the frozen reproducible pre-refactor baseline. Preserved absolute product targets block only when separately activated by the current release plan. |
| Remaining dependency | OQ-1 is delegated to architecture and does not block PRD completion; the platform architect must resolve each FR-10 through FR-15 landing zone before its implementation story starts. |
| Preserved legacy dispositions | §14 remains the normative product-contract baseline. Its unresolved product/release dispositions do not add refactor scope and require separate release decisions where activation is needed. |

## 3. Target User

The "users" of this initiative are developers, not end customers. This is an **internal developer-platform** effort. `[ASSUMPTION: internal developer-platform stakes; no external/customer-facing surface in scope.]`

### 3.1 Jobs To Be Done

- **As a domain-module author**, I want to stand up a new Hexalith business-domain module by writing domain logic — aggregate, events, validation, read-model shapes — without re-implementing host wiring, query/projection plumbing, tenant-access scaffolding, or serialization ceremony.
- **As a Conversations maintainer**, I want the module's surface area to be dominated by conversation logic so that bugs, reviews, and changes are about the domain, not the plumbing.
- **As a technical-module maintainer**, I want common boilerplate to have one home with one set of tests, so a fix or hardening lands once for every domain module instead of N times.
- **As a release owner**, I want confidence that a large refactor changed *no* externally-observable behavior — contracts and conformance behavior are provably intact.

### 3.2 Non-Users (v1)

- Authors of the *other* domain modules (Folders, Projects, Memories, Parties, Tenants) as a *migration* audience — they benefit from the promoted libraries but their migration is an explicit follow-on, not in this PRD's scope.
- End customers / tenant operators of the Conversations product — they should observe nothing.

### 3.3 Key User Journeys

*Developer journeys; lighter form per scope dial. FRs reference these inline.*

- **UJ-1. Maya retires hand-rolled plumbing from Conversations.** Maya, a Conversations maintainer, removes bespoke query and pagination infrastructure after confirming that the platform already supplies the generic capability. She retains only conversation-specific filters and response shapes, removes plumbing-only tests with their superseded implementation, and proves the public query behavior remains identical through the conformance gate. *Realizes FR-3..FR-9, gated by FR-20; technical mapping in addendum §D.*

- **UJ-2. Sam promotes the tenant-access handler everyone copied.** Sam notices that the tenant-access projection behavior is duplicated in Folders and Projects and re-implemented in Conversations. He moves the domain-agnostic behavior into a shared technical capability with its own tests, then has Conversations supply only its domain-specific contracts. The Conversations copy disappears; the shared implementation is the single source of truth. *Realizes FR-11; technical mapping in addendum §E.*

- **UJ-3. Priya stands up a brand-new domain module on the thin template.** Priya needs a new business-domain module. She follows the documented authoring template, supplies the required domain contracts and behavior, and consumes the platform-owned hosting and runtime capabilities. She reaches a working module with a fraction of the files Conversations originally needed. The template, proven by Conversations, is what makes this trivial. *Realizes FR-17, FR-18, FR-19; technical grounding in addendum §§D–F.*

## 4. Glossary

- **Business-domain module** — a Hexalith module that owns a bounded domain (e.g. Conversations, Folders, Projects, Tenants). It should contain domain logic, not infrastructure plumbing.
- **Technical module** — a shared Hexalith infrastructure module that domain modules depend on, including `Hexalith.EventStore` (+ its Client/DomainService/ServiceDefaults/Aspire/Testing packages), `Hexalith.Commons`, and `Hexalith.FrontComposer`.
- **Domain dependency** — another business-domain module whose contracts or behavior are consumed. `Hexalith.Tenants` is the multi-tenancy domain module and a dependency/consumer, never a landing zone for generic hosting or runtime boilerplate.
- **Boilerplate** — code that is **not specific to the Conversations domain, or that can be generalized for reuse** (user's definition). The target of this initiative.
- **Consume** — replace hand-rolled code in Conversations with an existing technical-module capability the module already exposes. No new shared code is created.
- **Promote** — extract boilerplate that is duplicated across domain modules (or a needed-but-missing helper) into a technical module, generalize it, give it its own tests, then consume it from Conversations.
- **Keep** — genuine Conversations domain logic that stays in the module (validation rules, aggregate `Handle` behavior, domain events/state, which fields a projection exposes).
- **Authoring surface** — the code a developer must write/own to stand up or maintain a domain module. Reducing it is the goal.
- **Thin authoring template** — the documented, minimal skeleton + checklist for a new domain module, proven by the Conversations pilot.
- **Promotion landing zone** — the technical module a promoted capability is moved into. `[ASSUMPTION: existing technical modules unless architecture proves a new shared module is warranted — Open Question OQ-1.]`
- **Release-gate behavior** — the externally-observable behaviors that must be preserved: tenant isolation (fail-closed), governance/audit-pairing, command idempotency, redaction replay/auditability, projection freshness/degraded-state signaling, public contract shape.
- **Conformance suite** — the existing tests that prove release-gate behavior (tenant isolation, idempotency, contract validation, redaction, provider portability, etc.).
- **CORE** — the minimum non-cuttable capability and Foundation Gate set required for credible substrate behavior across all eight preserved acceptance journeys.
- **Plumbing-only test** — a test that exists solely to cover hand-rolled infrastructure being removed; may be deleted with the code it covers.

## 5. MVP Scope and Boundaries

### 5.1 In Scope

- The classified boilerplate inventory (FR-1, FR-2).
- Consuming existing technical-module surface in Conversations (FR-3..FR-9).
- Consuming or extending platform capabilities and promoting duplicated/needed-but-missing capabilities Conversations consumes (FR-10..FR-15).
- Conversations adopting the promotions, the documented thin authoring template, and the authoring-cost measurement (FR-17..FR-19).
- The behavior-preservation conformance gate (FR-20).
- Coordinated changes into the relevant technical-module submodules (authorized for this initiative); `Hexalith.Tenants` participates only as a domain dependency/consumer when a genuine tenant-domain contract change is required.

### 5.2 Out of Scope and Non-Goals

- Fleet migration of Folders, Projects, Memories, Parties, or Tenants onto the promoted libraries is a named follow-on. **Owner:** product/platform owner. **Revisit:** after the Conversations pilot passes FR-20, when selecting a second adopter to validate reusability ROI.
- No new Conversations domain behavior or external-contract semantic change is authorized; the refactor does not redesign contracts for its own sake.
- No new persistence model, transport, or provider is introduced; the EventStore/Dapr substrate is unchanged.
- Promotions Conversations does not consume are cataloged as follow-on backlog, not built here. Governance orchestration, temporal reconstruction, and upstream hydration remain Conversations-owned during this pilot; an already-demonstrated generic SDK seam may be consumed without moving the domain behavior (§6.3 Notes).
- FR-16 shared compile-time command/event contract metadata remains backlog and is excluded from pilot acceptance.
- A dedicated shared module is not introduced if architecture determines existing technical modules are sufficient (OQ-1).
- FrontComposer-generated admin behavior is preserved; this initiative does not redesign UI/UX.
- This is not a performance-tuning project beyond preserving existing hot-path characteristics under SM-C2.

### 5.3 Phasing *(release approach)*

`[ASSUMPTION: phased delivery.]`
1. **Phase 0 — Baseline:** accept the inventory, record baseline LOC, freeze the versioned pre-refactor preservation manifest from a green build, and capture the reproducible pre-refactor P95 command/read benchmark (FR-1, FR-2, FR-19 baseline, FR-20 denominator, SM-C2 baseline).
2. **Phase 1 — Consume:** adopt existing surface (FR-3..FR-9). Low risk, Conversations-internal, conformance-gated.
3. **Phase 2 — Promote:** extract/generalize the needed shared capabilities with their own tests (FR-10..FR-15); FR-16 remains deferred.
4. **Phase 3 — Adopt & Prove:** Conversations consumes promotions; template + measurement; final gate (FR-17..FR-20).

## 6. Features

*Grouped by the three work-types plus the proof/measurement and the behavior gate. FR IDs are global and stable.*

### 6.1 Boilerplate Inventory & Classification (baseline)

**Description:** Before anything moves, the initiative establishes a canonical, evidence-backed inventory of Conversations source classified as Consume / Promote / Keep, with rough line counts and target landing zones. This is the spine every downstream story traces to and the baseline the success metrics measure against. The grounded first-pass inventory already exists in [addendum.md](addendum.md); this feature makes it an accepted, maintained artifact. Realizes the measurement basis for SM-1..SM-3.

**Functional Requirements:**

#### FR-1: Canonical boilerplate inventory exists and is accepted

A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability.

**Consequences (testable):**
- Every top-level source area in `Hexalith.Conversations.*` appears in the inventory with exactly one classification.
- Each Consume/Promote entry names the technical-module capability it maps to (existing or to-be-promoted).
- The baseline plumbing-LOC figure used by SM-1 is derived from this artifact and recorded.

#### FR-2: Classification disagreements are resolvable, not silent

A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale.

**Consequences (testable):**
- Any area reclassified after first acceptance has a logged rationale (decision log or inventory note).
- No area is left unclassified or dual-classified at acceptance.

### 6.2 Consume Existing Technical-Module Surface

**Description:** Conversations hand-rolled a substantial amount of machinery the EventStore SDK and Commons already expose. This feature deletes that machinery and adopts the existing libraries, keeping only the conversation-specific hooks. Each FR targets one area; the *mapping to specific library types* is in [addendum.md](addendum.md) (technical-how). Realizes UJ-1, gated by FR-20. `[ASSUMPTION: each consumed capability is functionally sufficient for Conversations' current behavior; gaps surface as Promote items instead.]`

| Requirements | Technical mapping |
|---|---|
| FR-3 through FR-9 | [Addendum §D — Existing technical-module surface to consume](addendum.md#d-existing-technical-module-surface-to-consume-fr-3fr-9) |

**Functional Requirements:**

#### FR-3: Domain-service host adoption

Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing.

**Consequences (testable):**
- Conversations is discoverable and runnable through the platform host without a Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime-host project.
- All Conversations operations supported before the refactor remain available through the shared host.
- Existing hosting behavior is covered by integration evidence against the platform host; only tests tied solely to superseded local plumbing may be removed.

#### FR-4: Query handling via SDK query-handler + cursor seams

Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts.

**Consequences (testable):**
- Local domain-agnostic query-orchestration and pagination-token machinery is removed; conversation-specific query behavior remains.
- Accepted and rejected pagination tokens, page ordering, continuation, and response shapes remain contract-compatible.
- Cursor round-trip and pagination behavior remain identical in release-gate scenarios.

#### FR-5: Read-model persistence via shared store + write policy

Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics.

**Consequences (testable):**
- Local domain-agnostic persistence and conflict-resolution loops are removed.
- Observable concurrent-update behavior is preserved, including the absence of lost updates under the existing tested contention scenarios.

#### FR-6: Projection handling via SDK projection seam

Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits.

**Consequences (testable):**
- Local generic projection orchestration is removed from Conversations.
- Conversation-specific projection field selection, freshness formula, and evidence construction remain in the module and retain their observable behavior.
- Projection rebuild/freshness conformance tests pass.

#### FR-7: Aggregate scaffolding via base-class conventions

Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior.

**Consequences (testable):**
- Redundant local routing or state-reconstruction plumbing is removed where the platform already provides equivalent behavior.
- Aggregate command/state/event behavior is unchanged (pure aggregate tests green).

#### FR-8: Serialization via shared converters / type registration

Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules.

**Consequences (testable):**
- Local converters and registration code that carry no domain rule are removed; only conversation-specific serialization rules remain.
- Serialized contract shapes are byte/shape-compatible (round-trip tests green).

#### FR-9: Testing via shared assertions/fakes/defaults

Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers.

**Consequences (testable):**
- Duplicate in-module test infrastructure that re-implements shared platform capabilities is removed.
- Domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation scenarios) remain.

### 6.3 Extend Platform Capabilities and Promote Common Boilerplate

**Description:** Some patterns are already partially or fully available from the platform; others are duplicated across domain modules or are needed-but-missing shared helpers. Conversations consumes the existing platform surface, any missing generic behavior is extended in its platform-owned technical module, and only genuinely absent reusable behavior is promoted with shared tests. Scope is bounded to **shared capabilities Conversations actually needs**; items Conversations does not consume are cataloged as follow-on backlog in [addendum.md](addendum.md), not built here. The addendum distinguishes existing surface from gaps, and the landing zone for any unresolved promotion remains an architecture decision (OQ-1). Realizes UJ-2, gated by FR-20.

| Requirements | Technical mapping |
|---|---|
| FR-10, FR-13, FR-16 | [Addendum §F — Gap catalog and current disposition](addendum.md#f-gap-catalog-and-current-disposition) |
| FR-11, FR-12, FR-14, FR-15 | [Addendum §E — Cross-module duplication and shared-capability candidates](addendum.md#e-cross-module-duplication--shared-capability-candidates-fr-10fr-15) |

FR-3, FR-10, and FR-13 are separate acceptance slices and must not produce duplicate stories:

| Requirement | Acceptance slice | Boundary |
|---|---|---|
| FR-3 | Domain-service host adoption | Conversations removes its domain-agnostic host implementation and runs through the platform host. |
| FR-10 | Shared ServiceDefaults | The platform supplies health, observability, resilience, and service-discovery defaults; Conversations supplies only domain telemetry definitions. |
| FR-13 | Aspire/Dapr topology | The platform AppHost owns resource topology, infrastructure-mode wiring, dependency connectivity, and publication connectivity. |

**Functional Requirements:**

#### FR-10: Platform-owned shared ServiceDefaults

The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- Conversations owns no ServiceDefaults project or equivalent hosting-defaults implementation.
- Existing health, telemetry, resilience, and discovery behavior remains observable after adoption, and conversation-specific telemetry remains available with its established names and dimensions.

#### FR-11: Generic tenant-access projection handler + registration

A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules.

**Consequences (testable):**
- The copied Conversations tenant-access processing and registration infrastructure is replaced by the shared capability.
- Fail-closed behavior on missing/stale/unavailable/disabled/ambiguous/insufficient projection state is preserved (tenant-isolation conformance green).
- Duplicate/out-of-order/replay tolerance is preserved.

#### FR-12: Shared client registration

A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation.

**Consequences (testable):**
- Conversations client registration uses the shared capability and the superseded local registration code is removed.
- Invalid endpoint configuration continues to be rejected with contract-compatible behavior (client registration tests green).

#### FR-13: Platform-owned Aspire/Dapr domain-service hosting

The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- No Conversations-local AppHost, Aspire, ServiceDefaults, or equivalent runtime-host module remains.
- The platform-hosted Conversations service retains its current dependency access, isolation mode, health behavior, and event/publication connectivity.

#### FR-14: Shared serialization metadata and polymorphic registration

A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition.

**Consequences (testable):**
- Conversations declares only its domain-specific serializable contract set; domain-agnostic registration and composition boilerplate is removed.
- Polymorphic (de)serialization of event/command hierarchies is preserved.

#### FR-15: Diagnostics/telemetry scaffolding helper

A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies.

**Consequences (testable):**
- Domain-agnostic instrumentation setup is removed from Conversations; only conversation-specific metric definitions and classification rules remain.
- Emitted metric names and cardinality are preserved.

#### FR-16: Compile-time command/event contract metadata *(deferred)*

Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative.

**Consequences (testable):**
- The pilot does not add shared command/event metadata interfaces or reshape current Conversations command/event contracts.
- The backlog record preserves the candidate and rationale without making it part of pilot acceptance or FR-20's change surface. `[OQ-4 resolved 2026-07-14.]`

**Notes:** Governance/verification orchestration, temporal query reconstruction, and reference hydration remain Conversations-owned during this pilot. The pilot may consume an already-demonstrated generic SDK seam without moving the domain behavior, but creating or extracting new shared capabilities for these areas is follow-on work requiring a separate decision. `[OQ-3 resolved 2026-07-14.]`

### 6.4 Adopt, Prove & Templatize (Conversations as pilot)

**Description:** The promotions only count if Conversations actually consumes them and the result is captured as a reusable, documented template. This feature is the proof and the deliverable that makes the reusability mandate real. Realizes UJ-3.

**Functional Requirements:**

#### FR-17: Conversations consumes every in-scope shared capability

Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot.

**Consequences (testable):**
- For each in-scope shared capability, the corresponding Conversations local implementation is deleted (not merely bypassed).
- Conversations builds and all conformance suites pass against the platform libraries.

#### FR-18: Documented thin authoring template

A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module.

**Consequences (testable):**
- The template enumerates the platform-host integration contract and the shared aggregate, query, projection, tenant-access, client, serialization, and telemetry responsibilities, including the minimal domain-owned inputs; AppHost, Aspire, DAPR, and ServiceDefaults remain platform-owned.
- The template is validated against the post-refactor Conversations module (it describes what Conversations actually does).

#### FR-19: New-module authoring cost is measured

The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2.

**Consequences (testable):**
- A measured "minimal module" figure (files + LOC) is recorded and traceable to the template.
- Target attainment requires a reproducible minimal-module fixture and a versioned measurement artifact that records the frozen file/LOC inclusion rules, source paths, measurement command/tool versions, commit/build identity, results, and named acceptance.

### 6.5 Behavior-Preservation Conformance Gate

**Description:** The non-negotiable acceptance gate for the whole initiative. Realizes the counter-metric SM-C1 and the preservation contract chosen by the owner.

**Functional Requirements:**

#### FR-20: Behavior and contracts are provably preserved

Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change.

**Consequences (testable):**
- The versioned preservation manifest identifies every denominator test and contract baseline, with the accepted pre-refactor source commit/build identity and evidence that the listed tests passed.
- All manifested release-gate conformance tests (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) pass post-refactor: the required pass rate is 100% of the frozen manifest.
- Public/adopter-facing contract shapes match the manifested baselines unless an explicit, named approval records the intentional change and its compatibility evidence.
- Removing, replacing, or reclassifying any manifested test requires explicit named-owner approval, rationale, replacement evidence where applicable, and a versioned manifest update; no conformance test is silently dropped.

## 7. Success Metrics

*Both headline outcomes weighted equally per owner decision.*

**Primary**
- **SM-1 — Conversations plumbing reduction.** Target: ≥40% of the frozen accepted classified-plumbing LOC removed or externalized, computed inclusively against the Story 1.4 baseline. Current evidence: 70.43%; target met. Validates FR-3..FR-17. `[OQ-2 resolved 2026-07-14.]`
- **SM-2 — New-module authoring cost.** Target: ≥50% fewer hand-authored, module-owned files for a minimal valid domain module within the frozen Story 4.1 measurement boundary, computed inclusively against the pre-initiative equivalent. LOC reduction remains mandatory supporting evidence, not a second numeric threshold. Current figures (50.00% files / 67.95% LOC) are **provisional** because they come from an accepted low-confidence estimate and do not establish target attainment. SM-2 is evidenced only when the reproducible minimal-module fixture and versioned measurement artifact required by FR-19 record the frozen inclusion rules, source paths, measurement command/tool versions, commit/build identity, results, and named acceptance. Validates FR-18, FR-19. `[OQ-2 threshold interpretation resolved 2026-07-14; attainment evidence remains provisional.]`

**Secondary**
- **SM-3 — Duplication eliminated.** Count of boilerplate patterns that now have a single shared home instead of N copies (from the cross-module duplication set). Target: every in-scope promoted pattern has exactly one source of truth. Validates FR-10..FR-15.
- **SM-4 — Maintainer signal (qualitative).** Conversations maintainers report the module reads as "mostly domain logic." `[ASSUMPTION: light qualitative check, not a survey instrument.]` Validates the Vision.

**Counter-metrics (do not optimize)**
- **SM-C1 — Behavior/contract stability (inviolable).** The post-refactor pass rate must remain 100% of the versioned pre-refactor preservation manifest, and public contract shapes must match its baselines unless a named approval records an intentional compatible change. Any manifested-test removal or reclassification requires explicit approval, rationale, replacement evidence where applicable, and a versioned manifest update. LOC reduction must **never** be bought by silently dropping conformance tests or reshaping contracts. Counterbalances SM-1, SM-2.
- **SM-C2 — Hot-path performance.** For every identified command/read hot path, post-refactor P95 latency must be no more than 5% worse than the frozen pre-refactor P95 under the same reproducible benchmark envelope. The versioned evidence records workload/data shape, concurrency, environment and runtime, tool versions, warm/cold classification, repetitions, raw results, and baseline/post-refactor commit identities. Preserved absolute targets `Feature-NFR9` (warm full-context open P95 ≤500 ms under its defined envelope) and `Feature-NFR12` (defined operator investigation ≤90 seconds) remain product obligations; they block this refactor only when the current release plan separately activates them. Counterbalances over-abstraction from promotions. `[OQ-5 resolved 2026-07-14.]`

## 8. Cross-Cutting NFRs

- **Behavior preservation:** FR-20 / SM-C1 are authoritative for the dominant NFR and its frozen denominator.
- **Performance:** SM-C2 is authoritative. Shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads; snapshot/projection behavior is preserved.
- **Fail-closed invariants:** promoted tenant-access and authorization capabilities must preserve fail-closed semantics by construction; cross-tenant access remains impossible and adversarially tested.
- **Observability:** metric names, dimensions, and health endpoints are preserved through platform-owned shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **Replay safety:** promoted projection/event handling must remain idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

## 9. Constraints & Guardrails

- **Cross-submodule coordination:** shared-capability work may edit sibling technical-module submodules (EventStore, Commons, FrontComposer). `Hexalith.Tenants` is a domain module and dependency/consumer, not a technical-module landing zone; coordinate with it only for genuinely required tenant-domain contract changes, and never place generic runtime or hosting boilerplate there. Authorized shared-module changes must remain additive/backward-compatible for existing consumers. `[ASSUMPTION: existing consumers of the technical modules must not break; promotions are additive.]` Honor the repo submodule rule: never recurse into nested submodules.
- **Greenfield latitude:** Conversations is treated as greenfield/pre-release, so plumbing-only tests may be removed with their code; but release-gate conformance is still inviolable. `[ASSUMPTION: Conversations not yet in production for external tenants.]`
- **Public-surface stability:** adopter-facing Conversations contracts and the EventStore-concept boundary (no raw envelopes leaked) are preserved.

## 10. Developer-Product Surface

- **Public surface / breaking-change policy:** promoted technical-module APIs are new public surface; they must be designed additive and versioned so existing domain modules compile unchanged. Conversations' own public contracts are unchanged.
- **Versioning & deprecation:** any Conversations-local type that is superseded by a promoted capability is removed within this initiative (greenfield); for the technical modules, additions follow normal semver-additive rules. `[ASSUMPTION: no deprecation window needed inside Conversations because it is the pilot consumer.]`
- **Language/runtime targets:** unchanged — net10.0, nullable, implicit usings, warnings-as-errors, Central Package Management through the shared Hexalith.Builds package-version baseline, with module-local package versions treated as explicit exceptions.
- **Performance budgets:** enforce SM-C2 and separately report whether the current release activates `Feature-NFR9` or `Feature-NFR12`.

## 11. Risks & Mitigations

- **R1 — Over-abstraction.** Generalizing too eagerly produces awkward shared APIs worse than the duplication. *Mitigation:* promote only patterns with ≥2 real consumers or a confirmed Conversations need; follow the existing proven generic pattern (`AddEventStore<TAggregate>` style); Conversations is the forcing-function consumer.
- **R2 — Hidden behavior coupling.** Hand-rolled code may encode subtle behavior the library doesn't replicate. *Mitigation:* conformance gate FR-20; consume per-area behind the gate, not big-bang.
- **R3 — Breaking other modules via promotion.** Editing shared modules could break Folders/Projects/etc. *Mitigation:* additive-only changes; build the dependent modules in CI.
- **R4 — Scope creep into fleet migration.** *Mitigation:* explicit §5.2 scope boundary; follow-on backlog.
- **R5 — Domain/plumbing boundary disputes.** Classification is judgment. *Mitigation:* FR-2 resolution rule + decision log.

## 12. Decision and Dependency Register

| ID | Status and decision | Owner / revisit |
|---|---|---|
| OQ-1 | **Architecture dependency; non-blocking for PRD.** Determine whether the landing zone for each of FR-10 through FR-15 is Commons, EventStore.*, FrontComposer, or an explicitly justified new shared technical module. Host, AppHost, Aspire, DAPR, ServiceDefaults, projection/query runtime, and subscription plumbing remain platform/domain-service SDK owned, never Conversations. | Platform architect, before the corresponding implementation story starts. |
| OQ-2 | **Resolved 2026-07-14.** SM-1 is ≥40% classified-plumbing LOC removed or externalized; SM-2 is ≥50% fewer hand-authored, module-owned files within the frozen boundary. Both comparisons are inclusive; file count decides SM-2 and LOC supports it. Current SM-2 evidence remains provisional until the FR-19 reproducible fixture and artifact exist. See `docs/release-evidence/oq-2-target-interpretation-decision-v1.json`. | Pilot acceptance owner reviews the versioned FR-19 artifact at pilot close. |
| OQ-3 | **Resolved 2026-07-14.** Governance orchestration, temporal reconstruction, and upstream hydration remain Conversations-owned. Only already-demonstrated generic SDK seams may be consumed; new extraction is follow-on work requiring a separate decision. | Reopen only through a separately approved follow-on decision. |
| OQ-4 | **Resolved 2026-07-14.** FR-16 shared compile-time command/event metadata is backlog and excluded from pilot scope and acceptance. | Reopen only through a separately approved initiative. |
| OQ-5 | **Resolved 2026-07-14.** SM-C2 permits at most a 5% post-refactor P95 regression against the frozen reproducible baseline under the same envelope. `Feature-NFR9` and `Feature-NFR12` remain product obligations and block only when separately activated by the current release plan. | Release owner identifies any separately activated absolute gate. |

## 13. Assumptions and Revisit Triggers

| Source | Current assumption | Owner / revisit |
|---|---|---|
| §3 | Internal developer-platform stakes; no external/customer-facing surface is in scope. | Product owner validates before any external-tenant or customer-facing release claim. |
| §4 / §9 | Promotions land in existing technical modules unless architecture proves a new module is needed. | The platform architect resolves OQ-1 before the implementation story for each of FR-10 through FR-15 starts. |
| §6.2 | Each consumed capability is functionally sufficient; shortfalls become Promote items. | Technical lead verifies during architecture and records any shortfall before implementation. |
| §5.3 | Delivery is phased. | Product/platform owner confirms sequencing during sprint planning; scope gates remain authoritative if sequencing changes. |
| §7 / SM-4 | Maintainer signal is a light qualitative check, not a survey instrument. | Pilot acceptance owner reviews the maintainer signal at pilot close. |
| §9 | Existing technical-module consumers must not break; promotions are additive. Conversations is not yet in external production. | Release owner verifies consumer compatibility and production status before any shared-package or external release. |
| §10 | No in-Conversations deprecation window is needed because Conversations is the pilot consumer. | Release owner revalidates before removing any package-visible type or if an external consumer is discovered. |


## 14. Preserved Conversations Product Contract Baseline

### 14.1 Authority, namespace, and disposition

This section is the normative product-contract baseline reconciled from the [archived May 2026 feature contract](../../../archive/conversations-product-contract-2026-05-31.md). It replaces the former live dependency on that legacy root document. Refactoring requirements FR-1 through FR-20 remain the scope of this initiative; the preserved product requirements use the distinct Feature-FR1 through Feature-FR104 and Feature-NFR1 through Feature-NFR77 namespaces.

Every Feature-FR and Feature-NFR below has the disposition **preserved** as a behavioral or quality constraint on FR-20 and SM-C1. “Preserved” does not mean implemented, shipped, accepted, or scheduled. Any requirement whose text is conditional on an active release remains conditional, and the current delivery state of every legacy v1/v1.1/vNext item is **open pending evidence or an explicit release decision**. This baseline does not expand the boilerplate-refactor scope, authorize customer-visible work, or override the initiative's non-goals.

### 14.2 Product intent

- Conversations owns the durable, tenant-scoped, event-sourced **business record of AI-assisted exchanges** among humans, AI agents, and LLMs. It is not a chatbot, transcript table, provider session store, or LLM orchestration layer.
- The record belongs to the business and remains usable across tools and provider session lifetimes. Provider IDs are attribution metadata, never authority.
- The first intended adopter is the Hexalith chatbot, and the Hexalith platform owner is the acceptance authority. A second adopter is evidence for the broader substrate claim, not a prerequisite for the basic product contract.
- The differentiating promise is **governance by construction**: fail-closed tenant isolation, paired audit events for governance mutations, idempotent behavior, deterministic replay/redaction behavior, time-correct evidence, and executable release evidence.
- Conversations links to upstream-owned Party, Project, Folder, and file identities by stable ID. Upstream modules own entity state and lifecycle orchestration; Conversations owns the conversation record and resolves current canonical references at read time.
- An AI conversation is treated as a durable business artifact comparable to a ticket, contract, or invoice: one memory that can be resumed and proved.

These obligations define what the refactor may not delete or weaken as “plumbing.”

### 14.3 Actors and preserved acceptance journeys

| Actor / journey | Preserved acceptance behavior |
|---|---|
| Maya, business user | Resume a multi-day conversation with participants, ordered messages, attachments, and business context intact; prevent cross-tenant enumeration; retain history after provider-session expiry; preserve the Feature-NFR9 warm-open target definition. |
| Atlas, AI agent | Recover full context after provider failure or provider switch; keep provider correlation IDs as metadata; reconstruct Party identity and both providers' attribution. |
| Sarah, compliance operator | Find by tenant-scoped external identifier, date, and business context; read attributed transcript and redactions; inspect inline audit; reconstruct prior state; copy citation-ready evidence; receive explicit migration-boundary and empty-state semantics; preserve the Feature-NFR12 investigation target definition. |
| Diego, adopter developer | Integrate through typed contracts and a supported .NET client without EventStore leakage; receive stable, sanitized typed errors; run adopter-facing conformance tests; retain semver and deprecation expectations. |
| Marcus, SRE | During audit-sink degradation, fail governance writes closed while eligible non-governance work continues; consume machine-readable verification; attach justification to privileged tenant-touching actions and record those actions in affected tenant audit trails. |
| Julian, platform owner | Accept or reject using a self-serve seeded demo, signed conformance artifact, versioned manifest, and explicit waiver state; see partial acceptance and downgrade triggers rather than having them hidden. |
| Helen, security reviewer | Independently run adversarial tenant-isolation, stale/missing projection, audit-pairing, redaction-replay, and release-gate checks; distinguish Conversations evidence from inherited platform controls. |
| Naomi, cross-product owner | Preserve stable-ID indirection across upstream lifecycle changes; keep cross-module lifecycle orchestration upstream rather than in Conversations. |
| Daniel, operations leader | Recover an immutable, time-ordered, attributed record and governance state after harm. Conversations provides provable testimony, not prevention, AI-grounding remediation, or automatic legal hold. |

### 14.4 Scope, boundaries, and legacy release slices

The preserved product capability baseline covers tenant-scoped conversation lifecycle, ordered messages, attributable participants, business references, event-sourced projections, governance/redaction/audit, operator evidence workflows, adopter contracts, compatibility evidence, and tenant-safe observability.

| Legacy slice | Preserved content | Current disposition |
|---|---|---|
| v1 floor | Conversation aggregate; chatbot command/read subset; EventStore persistence; fail-closed tenant isolation; sensitive-data/redaction policy; code-level governance enforcement; typed contract/client surface; read-only operator Find/Read/time-travel view; conformance evidence; provider-portability proof; compatibility/deprecation behavior. | **Open / status unverified.** Preserved for traceability only; this refactor does not assert delivery or authorize missing feature work. |
| v1.1 candidates | Evidence-bundle export; operator retention editor; full upcasting framework; broader governance analyzer; full temporal property testing; remaining commands/projections; richer FrontComposer metadata; audit-pairing status endpoint; non-chatbot reference integration. | **Open / status unverified.** Candidate ordering is not adopted as a current commitment. |
| vNext / anti-scope | Semantic/vector memory; summaries; branching; multi-agent planning; attachment binary storage; provider orchestration; real-time collaboration/streaming UI; cryptographic erasure; full compliance automation/legal-hold orchestration; cross-module lifecycle orchestration; cross-region replication. | **Preserved as non-authorized scope.** No work is added to this initiative. |

The archived contract also carried the following release-governance terms. They are retained as **historical provenance only**: they do not authorize feature delivery, select a release deal, establish a current date, or expand this refactor.

| Legacy release-governance item | Historical term preserved from May 2026 | Current disposition |
|---|---|---|
| Option A / Option B and GA+90 | Option A was a working assumption: chatbot at GA, extensibility commitments at GA+90 under buyer gating, and substrate framing aspirational until then. Option B remained switchable: extensibility blocks full GA while the chatbot may enter production-pilot first. | **Open / historical assumption, not current approval.** Neither option nor GA+90 is selected by this PRD. |
| Candidate ADR / Hexalith Standard | Under Option A, a `Hexalith.Conversations` Candidate ADR v0.1 was planned for GA+90. Promotion to Hexalith Standard required at least two independent production adopters covered by the conformance suite, or a six-month observation window with a named platform-owner waiver, whichever was later. The reference integration sample was paired with that review, and a fired downgrade rule revoked Candidate status. | **Open / historical criteria, not current approval.** No Candidate or Standard status is inferred. |
| Target Adopter Profile trigger | By GA−60 days, the legacy plan required at least one named real-organization candidate with a written one-page integration intent or LOI. If absent, the buyer chose a candidate-pursuit sprint, acceptance of downgrade risk with GA, or a 30-day GA delay; proceeding without a candidate required aspirational-framing disclosure. | **Open / historical trigger, not current approval.** Candidate, date, and current decision authority require revalidation. |
| Adoption and downgrade milestones | The historical target was one second adopter in active integration by GA+6 months and live by GA+12 months; a named pre-GA adopter with written intent changed the GA+6 target to at least two production modules. At GA+6, zero production adopters and no active integration triggered a public reframe within 30 days and ADR amendment. At GA+12, the same absence was the strategic-fail milestone for retiring the substrate claim. | **Open / historical milestones, not current approval.** No clock is running and no current owner is inferred. |

### 14.5 Preserved functional requirements

#### Conversation Lifecycle

- **Feature-FR1:** Adopter systems can create a tenant-scoped conversation record.
- **Feature-FR2:** Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names.
- **Feature-FR3:** The system can represent conversation lifecycle state and allowed transitions, including active, archived, or closed states and any release-approved behavior for reopening or sealing.
- **Feature-FR4:** Adopter systems can append ordered messages to an existing conversation.
- **Feature-FR5:** Adopter systems can add human users, AI agents, and LLMs as conversation participants.
- **Feature-FR6:** Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions.
- **Feature-FR7:** The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics.
- **Feature-FR8:** Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context.
- **Feature-FR9:** Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity.
- **Feature-FR10:** Adopter systems can update conversation title or metadata when that capability is included in the active release scope.
- **Feature-FR11:** Adopter systems can close or archive a conversation when that capability is included in the active release scope.
- **Feature-FR12:** The system can preserve a complete conversation record across provider session expiry, restart, or failover.

#### Participant Attribution

- **Feature-FR13:** The system can attribute each conversation action to a stable Party identity.
- **Feature-FR14:** The system can model humans, AI agents, and LLMs as attributable participants.
- **Feature-FR15:** The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
- **Feature-FR16:** The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- **Feature-FR17:** The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
- **Feature-FR18:** The system can reconstruct who said or changed what, when, and under which tenant context.

#### Business Context And References

- **Feature-FR19:** Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
- **Feature-FR20:** Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
- **Feature-FR21:** Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
- **Feature-FR22:** The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
- **Feature-FR23:** The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
- **Feature-FR24:** The system can keep conversations readable and attributable when upstream entities change lifecycle state.
- **Feature-FR25:** The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.

#### Tenant Access And Isolation

- **Feature-FR26:** The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
- **Feature-FR27:** The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- **Feature-FR28:** The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
- **Feature-FR29:** The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
- **Feature-FR30:** The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
- **Feature-FR31:** The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
- **Feature-FR32:** The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.

#### Event Sourcing, Projections, And Publication

- **Feature-FR33:** The system can derive projections from ordered conversation events.
- **Feature-FR34:** The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
- **Feature-FR35:** The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
- **Feature-FR36:** The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-FR37:** The system can expose projection lag or documented freshness behavior when read models are asynchronous.
- **Feature-FR38:** Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
- **Feature-FR39:** Published events can carry explicit schema and version metadata.
- **Feature-FR40:** The system can reject unsupported event, command, or projection schema versions with typed documented errors.
- **Feature-FR41:** The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.

#### Governance And Audit

- **Feature-FR42:** Authorized systems can set or replace a conversation retention policy with rationale.
- **Feature-FR43:** Authorized systems can mark conversation content as sensitive.
- **Feature-FR44:** Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution.
- **Feature-FR45:** The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history.
- **Feature-FR46:** The system can preserve the audit event stream while redacting projected or displayed content.
- **Feature-FR47:** The system can require every governance mutation to have a paired audit event.
- **Feature-FR48:** The system can reject governance mutations when audit recording is unavailable.
- **Feature-FR49:** The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state.
- **Feature-FR50:** The system can reconstruct message state and governance state as they existed at a prior point in time.
- **Feature-FR51:** The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata.
- **Feature-FR52:** The system can apply retention and redaction policy treatment to governance audit records themselves.
- **Feature-FR53:** The system can define which actions on audit records are allowed or denied and when the records can be redacted, exported, or separately logged.
- **Feature-FR54:** The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data.
- **Feature-FR55:** Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record.

#### Operator And Compliance Workflows

- **Feature-FR56:** Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID.
- **Feature-FR57:** Compliance operators can filter or narrow conversation search by date range and business context.
- **Feature-FR58:** Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness.
- **Feature-FR59:** Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy.
- **Feature-FR60:** Compliance operators can view a conversation's governance audit trail inline.
- **Feature-FR61:** Compliance operators can view conversation state as of a selected historical time.
- **Feature-FR62:** Compliance operators can copy citation-ready references for transcript and audit elements.
- **Feature-FR63:** Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract.
- **Feature-FR64:** Operator and compliance workflows marked read-only cannot mutate conversation aggregate state.
- **Feature-FR65:** Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited.
- **Feature-FR66:** Operators can run governance verification for a conversation, tenant, suite, or time window.
- **Feature-FR67:** Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks.
- **Feature-FR68:** Verification results can distinguish governance verification failures from infrastructure or execution failures.
- **Feature-FR69:** The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial.

#### Consumer Contracts And Developer Experience

- **Feature-FR70:** Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors.
- **Feature-FR71:** Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback.
- **Feature-FR72:** Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline.
- **Feature-FR73:** Adopter developers can run adopter-facing conformance tests before deployment.
- **Feature-FR74:** Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior.
- **Feature-FR75:** Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages.
- **Feature-FR76:** The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces.
- **Feature-FR77:** The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities.
- **Feature-FR78:** The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues.
- **Feature-FR79:** The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility.
- **Feature-FR80:** The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.

#### Compatibility, Evidence, And Release Gates

- **Feature-FR81:** The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages.
- **Feature-FR82:** The product can produce a signed conformance artifact for release gating.
- **Feature-FR83:** The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability.
- **Feature-FR84:** The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies.
- **Feature-FR85:** The product can support a named-waiver process for release-gate exceptions.
- **Feature-FR86:** The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior.
- **Feature-FR87:** The product can verify tenant isolation using adversarial positive and negative cases.
- **Feature-FR88:** The product can verify idempotent command behavior under duplicate or reordered commands.
- **Feature-FR89:** The product can verify redaction-replay correctness across projections, logs, traces, and errors.
- **Feature-FR90:** The product can verify provider portability by proving recoverability without provider-owned session authority.
- **Feature-FR91:** The product can verify event schema evolution through version-aware records and at least one worked additive-change example.
- **Feature-FR92:** The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release.
- **Feature-FR93:** The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests.
- **Feature-FR94:** The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable.

#### Observability And Operations

- **Feature-FR95:** Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
- **Feature-FR96:** Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
- **Feature-FR97:** Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
- **Feature-FR98:** Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
- **Feature-FR99:** Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.

#### Scope Boundaries And Lifecycle Commitments

- **Feature-FR100:** The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
- **Feature-FR101:** The product can expose release-scope consequences when substrate-defining capabilities are deferred.
- **Feature-FR102:** The product can support buyer partial acceptance under the Option A v1 deal.
- **Feature-FR103:** The product can track second-adopter status and trigger downgrade-rule review milestones.
- **Feature-FR104:** The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

### 14.6 Preserved non-functional requirements

Numeric targets below preserve their target definitions but do not assert that evidence currently passes, that the target has been classified as a release blocker, or that a waiver exists.

#### Measurement, Evidence, And Waiver Discipline

- **Feature-NFR1:** Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
- **Feature-NFR2:** Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
- **Feature-NFR3:** Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
- **Feature-NFR4:** Implementation for GA cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with a named owner and review date.
- **Feature-NFR5:** Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
- **Feature-NFR6:** Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
- **Feature-NFR7:** A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
- **Feature-NFR8:** Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.

#### Performance

- **Feature-NFR9:** Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- **Feature-NFR10:** The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
- **Feature-NFR11:** Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
- **Feature-NFR12:** Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
- **Feature-NFR13:** Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
- **Feature-NFR14:** Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
- **Feature-NFR15:** Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stages into one ambiguous number.

#### Security And Privacy

- **Feature-NFR16:** Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
- **Feature-NFR17:** Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
- **Feature-NFR18:** Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
- **Feature-NFR19:** Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
- **Feature-NFR20:** Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
- **Feature-NFR21:** Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.

#### Reliability, Resilience, And Recovery

- **Feature-NFR22:** The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
- **Feature-NFR23:** Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
- **Feature-NFR24:** Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
- **Feature-NFR25:** DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
- **Feature-NFR26:** A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
- **Feature-NFR27:** Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
- **Feature-NFR28:** The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
- **Feature-NFR29:** Backup restore and tenant-scoped recovery procedures must be tested before production release.

#### Scalability, Capacity, And Cost

- **Feature-NFR30:** The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
- **Feature-NFR31:** Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
- **Feature-NFR32:** Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- **Feature-NFR33:** Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
- **Feature-NFR34:** Tenant-events lag must have an SLO and a defined request behavior during lag windows.
- **Feature-NFR35:** Redaction propagation latency must have an SLO covering all materialization surfaces listed in Feature-NFR21.
- **Feature-NFR36:** The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
- **Feature-NFR37:** Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.

#### Data Integrity And Event Sourcing

- **Feature-NFR38:** v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
- **Feature-NFR39:** Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
- **Feature-NFR40:** Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
- **Feature-NFR41:** Event schema evolution must include one worked additive-change example before GA.
- **Feature-NFR42:** Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
- **Feature-NFR43:** Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.

#### Projection Freshness

- **Feature-NFR44:** Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
- **Feature-NFR45:** Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`; otherwise, the system must document why an equivalent shape is not available.
- **Feature-NFR46:** The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-NFR47:** Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
- **Feature-NFR48:** During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.

#### Integration And Compatibility

- **Feature-NFR49:** Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
- **Feature-NFR50:** Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
- **Feature-NFR51:** Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
- **Feature-NFR52:** Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
- **Feature-NFR53:** The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
- **Feature-NFR54:** Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.

#### Operability And Observability

- **Feature-NFR55:** Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
- **Feature-NFR56:** Operational signals must be tenant-safe and content-safe by default.
- **Feature-NFR57:** Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
- **Feature-NFR58:** Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
- **Feature-NFR59:** Output from `governance verify` and other conformance verification must be machine-readable and suitable for CI and incident workflows.
- **Feature-NFR60:** Privileged operational actions must include structured justification and produce reviewable audit records.
- **Feature-NFR61:** Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.

#### Compliance, Retention, And Release Evidence

- **Feature-NFR62:** Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
- **Feature-NFR63:** Every release must produce a signed conformance artifact and a versioned manifest that maps tests to FRs, NFRs, carry-forward commitments, and pass criteria and records waiver status, measurement method, and environment.
- **Feature-NFR64:** Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
- **Feature-NFR65:** Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
- **Feature-NFR66:** The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
- **Feature-NFR67:** Retention behavior must be tenant-aware and produce verifiable evidence.
- **Feature-NFR68:** Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.

#### Accessibility And Human Trust

- **Feature-NFR69:** Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
- **Feature-NFR70:** Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
- **Feature-NFR71:** Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
- **Feature-NFR72:** Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
- **Feature-NFR73:** Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
- **Feature-NFR74:** Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
- **Feature-NFR75:** Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
- **Feature-NFR76:** Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
- **Feature-NFR77:** User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.

### 14.7 Preserved qualitative constraints and ownership boundaries

- Fail closed before data access, not after a query has revealed existence.
- Tenant scoping is structural and persistent; privileged tools do not gain a hidden cross-tenant bypass.
- Governance audit pairing is enforced by code, platform runtime, and test mechanisms, not reviewer procedure alone.
- Redaction preserves immutable audit history while preventing redacted payload rematerialization anywhere user- or operator-visible.
- Event-sourced replay, schema evolution, and temporal evidence are product semantics, not merely implementation details.
- Provider portability is a tested recoverability property, not a provider abstraction claim.
- Public clients hide EventStore mechanics and use typed, sanitized, actionable failures.
- Stable-ID indirection preserves attribution across upstream lifecycle changes; upstream modules own current identity/entity state and lifecycle orchestration.
- Operator evidence is citeable and temporally stable, with visible freshness and degraded-state trust signals.
- Conversations promises honest records and evidence; it does not promise correct AI advice, harm prevention, chatbot orchestration, automatic legal hold, or full regulatory automation.
- Attachment binaries remain owned by Hexalith.Folders; tenant identity and roles remain owned by Hexalith.Tenants; Party identity remains owned by Hexalith.Parties.
- Hosting, persistence, AppHost, Aspire, DAPR, ServiceDefaults, projection/query runtime, telemetry scaffolding, and event-subscription plumbing are owned by the platform/domain-service SDK. Conversations owns domain contracts and behavior and consumes those platform capabilities; it does not ship module-local hosting projects.

### 14.8 Unresolved product and release dispositions

All entries below retain provenance from the legacy feature PRD and remain unresolved unless explicitly marked superseded. Legacy defaults are not approvals.

| ID | Legacy question or claim | Current disposition |
|---|---|---|
| Legacy-PQ1 | Does migrated or pre-UI-rollout history contain sufficient attribution? | **Open.** Restrict the coverage claim, backfill, or document the coverage boundary before acceptance. |
| Legacy-PQ2 | Is the signed conformance manifest plus named-waiver process an explicit release commitment? | **Open.** Feature-FR82 through Feature-FR85 and Feature-NFR62 through Feature-NFR64 remain preserved; commitment and gate classification require explicit buyer approval. |
| Legacy-PQ3 | Is Generate Evidence Bundle outside v1 and in v1.1, with read-only Find/Read in v1? | **Open.** Legacy slicing is not a current release decision. |
| Legacy-PQ4 | What chatbot deadline constrains delivery, and is chatbot release blocked on Conversations? | **Open.** No current deadline or dependency gate is inferred. |
| Legacy-PQ5 | Who owns and signs any public downgrade from “substrate backbone” framing? | **Open.** The legacy claim naming Jerome is unvalidated; a current named approval authority is required. |
| Legacy-RQ1 | Does another module consume Conversations events in the relevant release? | **Open.** Consumer and evidence status require current verification. |
| Legacy-RQ2 | Is the old 16–18-week feature estimate still relevant and is staffing sufficient? | **Superseded as a planning estimate.** It has no authority over this refactor; any feature-delivery estimate requires replanning. |
| Legacy-RQ3 | Is there a named second-adopter candidate and what evidence qualifies? | **Open.** A second adopter supports the broader substrate claim but is not a prerequisite for the baseline contract. |
| Legacy-RQ4 | Is the Foundation Gate blocking/waiver definition ratified? | **Open.** Ratification and named-waiver authority require an explicit release decision. |
| Legacy-RQ5 | Are sensitive-data marking and redaction commands mandatory in the chatbot CORE path? | **Open.** Feature-FR43 through Feature-FR49 remain preserved; CORE release inclusion requires an explicit decision. |
| Legacy-RQ6 | What evidence and gate status apply to the Feature-NFR9 warm-open and Feature-NFR12 operator targets? | **Open.** The target definitions are preserved and cannot be replaced silently by a generic no-regression criterion. |

Technical-how questions from the same legacy source are intentionally tracked with provenance and current disposition in the companion addendum, not in this product contract.
