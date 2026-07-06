---
title: "Conversations Boilerplate Reduction — A Thinner Domain-Authoring Surface"
status: draft
created: "2026-06-02"
updated: "2026-06-02"
---

# PRD: Conversations Boilerplate Reduction
*Working title — confirm.*

## 0. Document Purpose

This PRD is for the engineering owners of Hexalith.Conversations and the maintainers of the shared technical modules (EventStore, Commons, FrontComposer, Tenants), plus the downstream `bmad-create-architecture` and `bmad-create-epics-and-stories` workflows. It defines a **refactoring initiative**, not a feature: *what plumbing must leave the Conversations domain module*, the *success criteria*, and the *scope and sequencing*. It deliberately stays at capability altitude — which package owns what, the exact extracted API shapes, and migration mechanics are technical-how and live in the companion [addendum.md](addendum.md), which also carries the grounded consume/promote inventory and cross-module duplication evidence. This is a separate artifact from the existing Conversations *feature* PRD ([../../prd.md](../../prd.md)); that PRD defines what Conversations does, this one makes how it is built cheaper to author and maintain. Vocabulary is Glossary-anchored; functional requirements are grouped by feature with globally-numbered FR IDs; inferences are tagged `[ASSUMPTION]` inline and indexed in §13.

## 1. Vision

Hexalith.Conversations is roughly 35,800 lines of source, and about half of it is plumbing — DI ceremony, query-handler and cursor machinery, read-model store wiring, projection orchestration, tenant-access scaffolding, serialization converters, telemetry boilerplate, Aspire/Dapr topology. None of it is *about conversations*. Worse, the same plumbing is copy-pasted across the sibling domain modules (Folders, Projects, Memories, Parties): the same 80-line tenant-access handler, the same ServiceDefaults file with the service name swapped, the same typed-HttpClient registration. Every new business-domain module pays this tax again before it writes a single line of domain logic.

This initiative removes that tax. Conversations becomes the pilot: anything in it that is *not specific to conversations* either gets **consumed** from a technical module that already offers it (the EventStore SDK already exposes a two-line domain host, query/projection handler seams, a read-model store, and a cursor codec that Conversations hand-rolled around), or gets **promoted** — extracted, generalized, and lifted into a technical module so every domain module inherits it instead of re-implementing it. What stays in Conversations is conversation logic: the validation rules, the aggregate behavior, the events and the read-model shapes.

The payoff is two-sided and both sides matter equally. Conversations itself sheds a large, measurable share of its plumbing. And the *next* business-domain module — and every retrofit of an existing one — starts from a thin, documented authoring template instead of a blank file and a tradition of copy-paste. The whole exercise is held to a hard line: external contracts and the release-gate behaviors (tenant isolation, governance/audit pairing, idempotency, redaction replay, projection freshness) are preserved and proven by conformance tests. We are making the module cheaper to build, not changing what it does.

## 2. Target User

The "users" of this initiative are developers, not end customers. Stakes are an **internal developer-platform** effort. `[ASSUMPTION: internal developer-platform stakes; no external/customer-facing surface in scope.]`

### 2.1 Jobs To Be Done

- **As a domain-module author**, I want to stand up a new Hexalith business-domain module by writing domain logic — aggregate, events, validation, read-model shapes — without re-implementing host wiring, query/projection plumbing, tenant-access scaffolding, or serialization ceremony.
- **As a Conversations maintainer**, I want the module's surface area to be dominated by conversation logic so that bugs, reviews, and changes are about the domain, not the plumbing.
- **As a technical-module maintainer**, I want common boilerplate to have one home with one set of tests, so a fix or hardening lands once for every domain module instead of N times.
- **As a release owner**, I want confidence that a large refactor changed *no* externally-observable behavior — contracts and conformance behavior are provably intact.

### 2.2 Non-Users (v1)

- Authors of the *other* domain modules (Folders, Projects, Memories, Parties, Tenants) as a *migration* audience — they benefit from the promoted libraries but their migration is an explicit follow-on, not in this PRD's scope.
- End customers / tenant operators of the Conversations product — they should observe nothing.

### 2.3 Key User Journeys

*Developer journeys; lighter form per scope dial. FRs reference these inline.*

- **UJ-1. Maya retires hand-rolled plumbing from Conversations.** Maya, a Conversations maintainer, opens the module to remove the bespoke query handler and HMAC cursor code. She finds the EventStore SDK already exposes `IDomainQueryHandler` and a cursor codec seam, deletes the local machinery and the plumbing-only tests that covered it, implements the thin query handler with conversation-specific filters, and the conformance suite stays green. Net: a few hundred lines gone, behavior identical. *Realizes FR-3..FR-9, gated by FR-20.*

- **UJ-2. Sam promotes the tenant-access handler everyone copied.** Sam notices the ~80-line tenant-access projection handler is byte-for-byte duplicated in Folders and Projects and re-implemented in Conversations. He lifts a generic `TenantAccessProjectionHandler<TEvent, TProjection>` into the shared technical module with its own tests, then has Conversations register it with its concrete types. The Conversations copy disappears; the shared one is the single source of truth. *Realizes FR-11.*

- **UJ-3. Priya stands up a brand-new domain module on the thin template.** Priya needs a new business-domain module. She follows the documented authoring template: a two-line host, the aggregate base class, the handler seams, the registration one-liners. She writes only domain logic and reaches a working module in a fraction of the files Conversations originally needed. The template, proven by Conversations, is what makes this trivial. *Realizes FR-17, FR-18, FR-19.*

## 3. Glossary

- **Business-domain module** — a Hexalith module that owns a bounded domain (e.g. Conversations, Folders, Projects). Should contain domain logic, not infrastructure plumbing.
- **Technical module** — a shared Hexalith infrastructure module that domain modules depend on: `Hexalith.EventStore` (+ its Client/DomainService/ServiceDefaults/Aspire/Testing packages), `Hexalith.Commons`, `Hexalith.FrontComposer`, `Hexalith.Tenants`.
- **Boilerplate** — code that is **not specific to the Conversations domain, or that can be generalized for reuse** (user's definition). The target of this initiative.
- **Consume** — replace hand-rolled code in Conversations with an existing technical-module capability the module already exposes. No new shared code is created.
- **Promote** — extract boilerplate that is duplicated across domain modules (or a needed-but-missing helper) into a technical module, generalize it, give it its own tests, then consume it from Conversations.
- **Keep** — genuine Conversations domain logic that stays in the module (validation rules, aggregate `Handle` behavior, domain events/state, which fields a projection exposes).
- **Authoring surface** — the code a developer must write/own to stand up or maintain a domain module. Reducing it is the goal.
- **Thin authoring template** — the documented, minimal skeleton + checklist for a new domain module, proven by the Conversations pilot.
- **Promotion landing zone** — the technical module a promoted capability is moved into. `[ASSUMPTION: existing technical modules unless architecture proves a new shared module is warranted — Open Question OQ-1.]`
- **Release-gate behavior** — the externally-observable behaviors that must be preserved: tenant isolation (fail-closed), governance/audit-pairing, command idempotency, redaction replay/auditability, projection freshness/degraded-state signaling, public contract shape.
- **Conformance suite** — the existing tests that prove release-gate behavior (tenant isolation, idempotency, contract validation, redaction, provider portability, etc.).
- **Plumbing-only test** — a test that exists solely to cover hand-rolled infrastructure being removed; may be deleted with the code it covers.

## 4. Features

*Grouped by the three work-types plus the proof/measurement and the behavior gate. FR IDs are global and stable.*

### 4.1 Boilerplate Inventory & Classification (baseline)

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

### 4.2 Consume Existing Technical-Module Surface

**Description:** Conversations hand-rolled a substantial amount of machinery the EventStore SDK and Commons already expose. This feature deletes that machinery and adopts the existing libraries, keeping only the conversation-specific hooks. Each FR targets one area; the *mapping to specific library types* is in [addendum.md](addendum.md) (technical-how). Realizes UJ-1, gated by FR-20. `[ASSUMPTION: each consumed capability is functionally sufficient for Conversations' current behavior; gaps surface as Promote items instead.]`

**Functional Requirements:**

#### FR-3: Domain-service host adoption

Conversations runs on the shared two-line domain-service host (assembly-scanning registration + endpoint mapping) instead of bespoke DI and host wiring.

**Consequences (testable):**
- The Conversations server host registers the domain via the shared SDK entrypoints; per-feature hand-written `ServiceCollectionExtensions` that merely re-implement SDK discovery are removed.
- All canonical domain endpoints (process / replay / query / project / admin metadata) resolve via the shared host.
- Existing host/registration integration tests pass or are replaced by equivalent assertions against the shared host.

#### FR-4: Query handling via SDK query-handler + cursor seams

Conversations implements query handlers against the SDK's query-handler seam and uses the SDK cursor codec, instead of a bespoke query-handler orchestration and hand-rolled HMAC cursor signing.

**Consequences (testable):**
- The hand-rolled cursor signing/validation code is removed; pagination uses the SDK cursor seam.
- Conversation-specific query filters and response shapes remain; only the orchestration/cursor plumbing is delegated.
- Cursor round-trip and pagination behavior is preserved (re-resolves identically per release-gate behavior).

#### FR-5: Read-model persistence via shared store + write policy

Conversations persists and updates read models through the shared read-model store and optimistic-concurrency write policy instead of bespoke store/merge code.

**Consequences (testable):**
- Hand-written DAPR state-store calls and merge-on-write loops are removed in favor of the shared store/policy.
- Optimistic-concurrency / retry behavior is preserved (no lost updates under concurrent writes — existing tests green).

#### FR-6: Projection handling via SDK projection seam

Conversations implements projections against the SDK's full-replay projection seam; the generic event-to-model orchestration loop is delegated, while *which fields/metadata* a projection emits stays in Conversations.

**Consequences (testable):**
- Generic projection orchestration (replay loop, dispatch registration) is delegated to the SDK seam.
- Conversation-specific projection field selection, freshness formula, and evidence construction remain in the module.
- Projection rebuild/freshness conformance tests pass.

#### FR-7: Aggregate scaffolding via base-class conventions

Conversations relies on the EventStore aggregate base class's reflection-based command dispatch and state replay rather than any redundant manual routing or status-bridging shims.

**Consequences (testable):**
- Aggregate command routing and state replay use the base-class conventions; redundant manual dispatch/idempotency-bridge plumbing is removed where the base class or SDK already provides it.
- Aggregate command/state/event behavior is unchanged (pure aggregate tests green).

#### FR-8: Serialization via shared converters / type registration

Conversations uses the shared serialization helpers (type mapper / generic converters / source-generated context base) for generic patterns instead of bespoke per-type converters that carry no domain logic.

**Consequences (testable):**
- Generic string/int/value-object converters with no domain rules are replaced by the shared mechanism; only converters encoding genuine domain rules remain.
- Serialized contract shapes are byte/shape-compatible (round-trip tests green).

#### FR-9: Testing via shared assertions/fakes/defaults

Conversations test projects consume the shared EventStore.Testing assertions and fakes and shared ServiceDefaults instead of duplicating equivalent fakes and assertion helpers.

**Consequences (testable):**
- Duplicate in-module fakes/assertions that re-implement shared EventStore.Testing capabilities are removed.
- Domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation scenarios) remain.

### 4.3 Promote Common Boilerplate to Technical Modules

**Description:** These patterns are duplicated across domain modules (or are needed-but-missing helpers Conversations must hand-roll today). Each is extracted into a technical module, generalized, given its own tests, then consumed by Conversations. Scope is bounded to **promotions Conversations actually needs**; promotions Conversations doesn't consume are cataloged as follow-on backlog in [addendum.md](addendum.md), not built here. The landing zone per promotion is an architecture decision (OQ-1). Realizes UJ-2, gated by FR-20.

**Functional Requirements:**

#### FR-10: Shared ServiceDefaults

A domain module configures observability, health checks, resilience, and service discovery through a shared ServiceDefaults capability with extension hooks for module-specific instrumentation (e.g. Redis/DAPR), instead of a copied per-module ServiceDefaults file.

**Consequences (testable):**
- Conversations' ServiceDefaults is reduced to module-specific hooks over the shared base.
- Health/telemetry endpoint behavior is preserved (registration tests green).

#### FR-11: Generic tenant-access projection handler + registration

A domain module wires fail-closed tenant-access projection handling and its DI registration via a generic shared capability parameterized by the domain's event/projection types, instead of a copied handler and copied `AddXxxTenantAccess()` extension.

**Consequences (testable):**
- The hand-written Conversations tenant-access handler and bespoke registration are replaced by the generic shared capability with concrete type arguments.
- Fail-closed behavior on missing/stale/unavailable/disabled/ambiguous/insufficient projection state is preserved (tenant-isolation conformance green).
- Duplicate/out-of-order/replay tolerance preserved.

#### FR-12: Generic typed-HttpClient client registration

A domain module registers its typed HTTP client with options binding and validation through a shared, domain-agnostic registration helper instead of a copied `AddXxxClient()` pair.

**Consequences (testable):**
- Conversations client registration uses the shared helper; the hand-rolled pair is removed.
- Options validation (missing/relative URL rejection) behavior is preserved (client registration tests green).

#### FR-13: Shared Aspire/Dapr domain-module hosting base

A domain module attaches its AppHost/Aspire + DAPR sidecar topology (shared vs isolated infrastructure modes) via a shared hosting capability parameterized by app-id/component names, instead of a copied per-module Aspire module.

**Consequences (testable):**
- Conversations Aspire wiring is expressed through the shared capability with its names/mode.
- Resource wiring (state-store, pub/sub, sidecar) is equivalent to today's topology.

#### FR-14: Shared JSON-context base / polymorphic type registration

A domain module declares its serializable type lists against a shared source-generated JSON-context base / polymorphic registration helper, instead of hand-assembling resolver combination and type catalogs.

**Consequences (testable):**
- Conversations JSON context declares only its type lists; resolver combination/registration boilerplate is provided by the shared base.
- Polymorphic (de)serialization of event/command hierarchies is preserved.

#### FR-15: Diagnostics/telemetry scaffolding helper

A domain module instruments metrics via a shared meter/counter/classifier scaffolding helper, supplying only metric names and bounded dimension vocabularies, instead of repeating meter-factory and classifier ceremony per signal.

**Consequences (testable):**
- Conversations telemetry registers meters/counters through the shared helper; only domain metric names/dimension enums remain in the module.
- Emitted metric names and cardinality are preserved.

#### FR-16: Compile-time command/event contract metadata *(conditional)*

A domain module declares command/event domain+type metadata via shared contract interfaces (parallel to the existing query-contract interface) instead of hand-rolled string type names — **only if** Conversations consumes it within pilot scope.

**Consequences (testable):**
- If built: Conversations commands/events declare metadata via the shared interface; magic-string type names for those contracts are removed.
- If deferred: recorded as backlog in the addendum with rationale (does not block FR-20). `[ASSUMPTION: FR-16 is in-scope only if it reduces Conversations boilerplate without forcing contract reshaping that risks behavior preservation.]`

**Notes:** `[NOTE FOR PM]` The heaviest, most domain-entangled areas — governance/verification orchestration (~4.3k LOC), temporal query reconstruction, and reference hydration — are *promotion candidates* but are **not** promoted in this pilot unless they generalize cleanly; first-pass they are classified and Conversations keeps the domain logic, with generic orchestration flagged for a follow-on promotion. Confirm this boundary.

### 4.4 Adopt, Prove & Templatize (Conversations as pilot)

**Description:** The promotions only count if Conversations actually consumes them and the result is captured as a reusable, documented template. This feature is the proof and the deliverable that makes the reusability mandate real. Realizes UJ-3.

**Functional Requirements:**

#### FR-17: Conversations consumes every in-scope promotion

Conversations depends on and uses each promoted capability (FR-10..FR-16 as built); no superseded local copy remains.

**Consequences (testable):**
- For each in-scope promotion, the corresponding Conversations local implementation is deleted (not merely bypassed).
- Conversations builds and all conformance suites pass against the promoted libraries.

#### FR-18: Documented thin authoring template

A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module.

**Consequences (testable):**
- The template enumerates the shared host, aggregate base, handler seams, tenant-access, client, Aspire, serialization, and telemetry capabilities with the one-liners to adopt each.
- The template is validated against the post-refactor Conversations module (it describes what Conversations actually does).

#### FR-19: New-module authoring cost is measured

The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2.

**Consequences (testable):**
- A measured "minimal module" figure (files + LOC) is recorded and traceable to the template.

### 4.5 Behavior-Preservation Conformance Gate

**Description:** The non-negotiable acceptance gate for the whole initiative. Realizes the counter-metric SM-C1 and the preservation contract chosen by the owner.

**Functional Requirements:**

#### FR-20: Behavior and contracts are provably preserved

The full release-gate conformance suite passes on the refactored module and public contracts are shape-compatible; only plumbing-only tests are removed.

**Consequences (testable):**
- All release-gate conformance suites (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) pass post-refactor.
- Public/adopter-facing contract shapes are unchanged (or changes are explicitly approved and recorded).
- Every removed test is justified as plumbing-only in the change record; no conformance test is silently dropped.

## 5. Non-Goals (Explicit)

- **Not** migrating Folders, Projects, Memories, Parties, or Tenants onto the promoted libraries (named follow-on).
- **Not** changing what Conversations does — no new domain features, no contract redesign for its own sake.
- **Not** introducing a new persistence model, transport, or provider; the EventStore/Dapr substrate is unchanged.
- **Not** building promotions Conversations does not consume in this pilot (cataloged as backlog only).
- **Not** a UI/UX change — FrontComposer-generated admin surface behavior is preserved, not redesigned.
- **Not** a performance-tuning project, beyond preserving existing hot-path characteristics (see NFRs).

## 6. MVP Scope

### 6.1 In Scope
- The classified boilerplate inventory (FR-1, FR-2).
- Consuming existing technical-module surface in Conversations (FR-3..FR-9).
- Promoting the duplicated/needed-but-missing capabilities Conversations consumes (FR-10..FR-15; FR-16 conditional).
- Conversations adopting the promotions, the documented thin authoring template, and the authoring-cost measurement (FR-17..FR-19).
- The behavior-preservation conformance gate (FR-20).
- Coordinated changes into the relevant technical-module submodules (authorized for this initiative).

### 6.2 Out of Scope for MVP
- Fleet migration of other domain modules — deferred follow-on. `[NOTE FOR PM: the reusability ROI is only fully realized once a second module adopts the promotions; keep this visible.]`
- Promotion of heavy domain-entangled areas (governance orchestration, temporal reconstruction, hydration) unless they generalize cleanly (§4.3 Notes).
- A new dedicated shared module if architecture deems existing modules sufficient (OQ-1).
- Any change to external contract *semantics*.

### 6.3 Phasing *(release approach)*
`[ASSUMPTION: phased delivery.]`
1. **Phase 0 — Baseline:** accept the inventory + record baseline LOC (FR-1, FR-2, FR-19 baseline).
2. **Phase 1 — Consume:** adopt existing surface (FR-3..FR-9). Low risk, Conversations-internal, conformance-gated.
3. **Phase 2 — Promote:** extract/generalize the needed shared capabilities with their own tests (FR-10..FR-16).
4. **Phase 3 — Adopt & Prove:** Conversations consumes promotions; template + measurement; final gate (FR-17..FR-20).

## 7. Success Metrics

*Both headline outcomes weighted equally per owner decision.*

**Primary**
- **SM-1 — Conversations plumbing reduction.** Plumbing LOC in Conversations (Consume+Promote categories per FR-1 baseline) drops by a target margin. `[ASSUMPTION: target ≥ 40% of classified plumbing LOC removed/delegated; confirm the number.]` Validates FR-3..FR-17.
- **SM-2 — New-module authoring cost.** Files + LOC for a minimal valid domain module on the template, vs. the pre-initiative equivalent. Target: a substantial reduction (template-driven). `[ASSUMPTION: target ≥ 50% fewer files for a minimal module; confirm.]` Validates FR-18, FR-19.

**Secondary**
- **SM-3 — Duplication eliminated.** Count of boilerplate patterns that now have a single shared home instead of N copies (from the cross-module duplication set). Target: every in-scope promoted pattern has exactly one source of truth. Validates FR-10..FR-15.
- **SM-4 — Maintainer signal (qualitative).** Conversations maintainers report the module reads as "mostly domain logic." `[ASSUMPTION: light qualitative check, not a survey instrument.]` Validates the Vision.

**Counter-metrics (do not optimize)**
- **SM-C1 — Behavior/contract stability (inviolable).** Release-gate conformance pass rate must remain 100% and public contract shapes unchanged. LOC reduction must **never** be bought by dropping conformance tests or reshaping contracts. Counterbalances SM-1, SM-2.
- **SM-C2 — Hot-path performance.** Command/read hot-path latency must not regress. Counterbalances over-abstraction from promotions.

## 8. Cross-Cutting NFRs

- **Behavior preservation (gate):** see FR-20 / SM-C1 — the dominant NFR.
- **Performance:** post-refactor command and read hot-paths must not regress materially; shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads (preserve snapshot/projection use). `[ASSUMPTION: "no material regression" ≈ within noise of current benchmarks; confirm if a numeric budget exists.]`
- **Fail-closed invariants:** promoted tenant-access and authorization capabilities must preserve fail-closed semantics by construction; cross-tenant access remains impossible and adversarially tested.
- **Observability:** metric names, dimensions, and health endpoints preserved through shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **Replay safety:** promoted projection/event handling must remain idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

## 9. Constraints & Guardrails

- **Cross-submodule coordination:** promotions edit sibling technical-module submodules (EventStore, Commons, Tenants, FrontComposer). This is explicitly authorized for this initiative, overriding the default "scope changes to Conversations" rule — but changes there must be additive/backward-compatible for other modules already depending on them. `[ASSUMPTION: existing consumers of the technical modules must not break; promotions are additive.]` Honor the repo submodule rule: never recurse into nested submodules.
- **Greenfield latitude:** Conversations is treated as greenfield/pre-release, so plumbing-only tests may be removed with their code; but release-gate conformance is still inviolable. `[ASSUMPTION: Conversations not yet in production for external tenants.]`
- **Public-surface stability:** adopter-facing Conversations contracts and the EventStore-concept boundary (no raw envelopes leaked) are preserved.

## 10. Developer-Product Surface

- **Public surface / breaking-change policy:** promoted technical-module APIs are new public surface; they must be designed additive and versioned so existing domain modules compile unchanged. Conversations' own public contracts are unchanged.
- **Versioning & deprecation:** any Conversations-local type that is superseded by a promoted capability is removed within this initiative (greenfield); for the technical modules, additions follow normal semver-additive rules. `[ASSUMPTION: no deprecation window needed inside Conversations because it is the pilot consumer.]`
- **Language/runtime targets:** unchanged — net10.0, nullable, implicit usings, warnings-as-errors, Central Package Management through the shared Hexalith.Builds package-version baseline, with module-local package versions treated as explicit exceptions.
- **Performance budgets:** see NFRs / SM-C2.

## 11. Risks & Mitigations

- **R1 — Over-abstraction.** Generalizing too eagerly produces awkward shared APIs worse than the duplication. *Mitigation:* promote only patterns with ≥2 real consumers or a confirmed Conversations need; follow the existing proven generic pattern (`AddEventStore<TAggregate>` style); Conversations is the forcing-function consumer.
- **R2 — Hidden behavior coupling.** Hand-rolled code may encode subtle behavior the library doesn't replicate. *Mitigation:* conformance gate FR-20; consume per-area behind the gate, not big-bang.
- **R3 — Breaking other modules via promotion.** Editing shared modules could break Folders/Projects/etc. *Mitigation:* additive-only changes; build the dependent modules in CI.
- **R4 — Scope creep into fleet migration.** *Mitigation:* explicit Non-Goal; follow-on backlog.
- **R5 — Domain/plumbing boundary disputes.** Classification is judgment. *Mitigation:* FR-2 resolution rule + decision log.

## 12. Open Questions

1. **OQ-1 (architecture):** For each promoted capability, does it land in an existing technical module (Commons / EventStore.*) or a new dedicated shared module? — for `bmad-create-architecture`.
2. **OQ-2:** Confirm SM-1/SM-2 numeric targets (currently assumed ≥40% plumbing LOC, ≥50% fewer files).
3. **OQ-3:** Confirm the §4.3-Notes boundary: governance/temporal/hydration classified-and-kept now, promoted later?
4. **OQ-4:** Is FR-16 (command/event contract metadata) in pilot scope, or deferred to backlog?
5. **OQ-5:** Is there an explicit hot-path performance budget (SM-C2), or is "no regression vs current" sufficient?

## 13. Assumptions Index

- §2 — Internal developer-platform stakes; no external surface.
- §3 / §9 — Promotions land in existing technical modules unless architecture proves a new one needed (OQ-1).
- §4.2 — Each consumed capability is functionally sufficient; shortfalls become Promote items.
- §4.3 (FR-16) — Contract-metadata interfaces in scope only if they cut boilerplate without risky contract reshaping.
- §6.3 — Phased delivery.
- §7 — SM-1 ≥40% plumbing LOC removed/delegated; SM-2 ≥50% fewer files for a minimal module; SM-4 light qualitative check.
- §8 / §10 — "No material performance regression" ≈ within current benchmark noise.
- §9 — Existing technical-module consumers must not break (additive promotions); Conversations not yet in external production.
- §10 — No in-Conversations deprecation window needed (pilot consumer).
