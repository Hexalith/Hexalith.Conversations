---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md"
  - "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.decision-log.md"
---

# Hexalith.Conversations Boilerplate Reduction - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for the **Conversations Boilerplate Reduction** initiative — a refactoring effort (not a feature) that thins the Conversations domain module by *consuming* existing technical-module surface, *promoting* duplicated boilerplate into shared technical modules, and proving the result as a reusable thin authoring template. It decomposes the requirements from the PRD and the technical-how evidence in the companion addendum into implementable stories. There is no UI/UX scope (explicit Non-Goal) and no separate architecture document yet (landing-zone decisions are deferred to a downstream `bmad-create-architecture` run, OQ-1); the addendum stands in for architecture-level technical input.

## Requirements Inventory

### Functional Requirements

**Feature group 4.1 — Boilerplate Inventory & Classification (baseline)**

FR-1: Canonical boilerplate inventory exists and is accepted — a single maintained artifact lists every `Hexalith.Conversations.*` source area with exactly one Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and (for Consume/Promote) the target technical-module capability; the baseline plumbing-LOC figure for SM-1 is derived from it and recorded.

FR-2: Classification disagreements are resolvable, not silent — any Consume/Promote/Keep call can be challenged and the resolution recorded with rationale; no area is left unclassified or dual-classified at acceptance; reclassifications after acceptance carry a logged rationale.

**Feature group 4.2 — Consume Existing Technical-Module Surface**

FR-3: Domain-service host adoption — Conversations runs on the shared two-line domain-service host (assembly-scanning registration + endpoint mapping) instead of bespoke DI/host wiring; all canonical endpoints (process/replay/query/project/admin metadata) resolve via the shared host; hand-written per-feature `ServiceCollectionExtensions` that merely re-implement SDK discovery are removed.

FR-4: Query handling via SDK query-handler + cursor seams — Conversations implements query handlers against the SDK query-handler seam and uses the SDK cursor codec; hand-rolled HMAC cursor signing/validation is removed; conversation-specific filters/response shapes remain; cursor round-trip/pagination re-resolves identically.

FR-5: Read-model persistence via shared store + write policy — Conversations persists/updates read models through the shared read-model store and optimistic-concurrency write policy; hand-written Dapr state-store calls and merge-on-write loops are removed; no lost updates under concurrent writes.

FR-6: Projection handling via SDK projection seam — Conversations implements projections against the SDK full-replay projection seam; generic replay/dispatch orchestration is delegated; conversation-specific field selection, freshness formula, and evidence construction remain; rebuild/freshness conformance passes.

FR-7: Aggregate scaffolding via base-class conventions — Conversations relies on the `EventStoreAggregate<TState>` reflection-based command dispatch and state replay; redundant manual routing / idempotency-bridge shims are removed; aggregate command/state/event behavior is unchanged.

FR-8: Serialization via shared converters / type registration — Conversations uses shared serialization helpers (type mapper / generic converters / source-generated context base) for generic patterns; generic value converters with no domain rules are replaced; only converters encoding genuine domain rules remain; serialized contract shapes are byte/shape-compatible.

FR-9: Testing via shared assertions/fakes/defaults — Conversations test projects consume shared EventStore.Testing assertions/fakes and shared ServiceDefaults; duplicate in-module fakes/assertions are removed; domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation) remain.

**Feature group 4.3 — Promote Common Boilerplate to Technical Modules**

FR-10: Shared ServiceDefaults — a domain module configures observability, health checks, resilience, and service discovery through a shared ServiceDefaults capability with extension hooks for module-specific instrumentation (e.g. Redis/Dapr); Conversations' ServiceDefaults reduces to module-specific hooks; health/telemetry endpoint behavior preserved.

FR-11: Generic tenant-access projection handler + registration — a domain module wires fail-closed tenant-access projection handling and DI via a generic shared `TenantAccessProjectionHandler<TEvent,TProjection>` (+ registration) parameterized by domain types; the hand-written Conversations handler and bespoke registration are replaced; fail-closed behavior (missing/stale/unavailable/disabled/ambiguous/insufficient) and duplicate/out-of-order/replay tolerance preserved.

FR-12: Generic typed-HttpClient client registration — a domain module registers its typed HTTP client with options binding/validation via a shared domain-agnostic helper instead of a copied `AddXxxClient()` pair; options validation (missing/relative URL rejection) preserved.

FR-13: Shared Aspire/Dapr domain-module hosting base — a domain module attaches AppHost/Aspire + Dapr sidecar topology (shared vs isolated modes) via a shared hosting capability parameterized by app-id/component names; Conversations' Aspire wiring expressed through the shared capability; resource wiring (state-store, pub/sub, sidecar) equivalent to today.

FR-14: Shared JSON-context base / polymorphic type registration — a domain module declares serializable type lists against a shared source-generated JSON-context base / polymorphic registration helper; resolver combination/registration boilerplate is provided by the shared base; polymorphic (de)serialization of event/command hierarchies preserved.

FR-15: Diagnostics/telemetry scaffolding helper — a domain module instruments metrics via a shared meter/counter/classifier scaffolding helper, supplying only metric names and bounded dimension vocabularies; only domain metric names/dimension enums remain in the module; emitted metric names and cardinality preserved.

FR-16: Compile-time command/event contract metadata *(conditional)* — a domain module declares command/event domain+type metadata via shared contract interfaces (`ICommandContract`/`IEventContract`, parallel to `IQueryContract`) instead of magic-string type names — **only if** Conversations consumes it within pilot scope; if deferred, recorded as backlog with rationale (does not block FR-20).

**Feature group 4.4 — Adopt, Prove & Templatize (Conversations as pilot)**

FR-17: Conversations consumes every in-scope promotion — Conversations depends on and uses each promoted capability (FR-10..FR-16 as built); for each, the superseded local implementation is deleted (not merely bypassed); Conversations builds and all conformance suites pass against the promoted libraries.

FR-18: Documented thin authoring template — a developer can follow a documented authoring template (minimal module skeleton + checklist of shared capabilities to wire: host, aggregate base, handler seams, tenant-access, client, Aspire, serialization, telemetry, each with adoption one-liners); validated against the post-refactor Conversations module.

FR-19: New-module authoring cost is measured — a measured "minimal valid domain module" figure (files + LOC for a do-nothing-but-valid module on the template) is recorded and traceable to the template, as the SM-2 baseline.

**Feature group 4.5 — Behavior-Preservation Conformance Gate**

FR-20: Behavior and contracts are provably preserved — the full release-gate conformance suite (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) passes on the refactored module; public/adopter-facing contract shapes are unchanged (or changes explicitly approved and recorded); every removed test is justified as plumbing-only in the change record — no conformance test silently dropped.

### NonFunctional Requirements

NFR1: **Behavior preservation (dominant gate)** — the full release-gate conformance suite must pass and contracts remain shape-compatible; LOC reduction must never be bought by dropping conformance tests or reshaping contracts (FR-20 / SM-C1, inviolable).

NFR2: **Performance — no material hot-path regression** — post-refactor command and read hot-paths must not regress materially (≈ within current benchmark noise); shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads (snapshot/projection use preserved) (SM-C2).

NFR3: **Fail-closed invariants by construction** — promoted tenant-access and authorization capabilities must preserve fail-closed semantics; cross-tenant access remains impossible by construction and is adversarially tested.

NFR4: **Observability continuity** — metric names, dimensions, and health endpoints are preserved through the shared telemetry/ServiceDefaults so existing dashboards and alerts keep working.

NFR5: **Replay safety** — promoted projection/event handling remains idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

NFR6: **Additive, versioned shared APIs** — promoted technical-module APIs are new public surface designed additive and semver-additive so existing domain modules (Folders/Projects/Memories/Parties/Tenants) compile unchanged; dependent modules are built in CI to prove it.

NFR7: **Language/runtime targets unchanged** — net10.0, nullable enabled, implicit usings, warnings-as-errors, Central Package Management through the shared Hexalith.Builds package-version baseline, with module-local package versions treated as explicit exceptions.

NFR8: **Public-surface stability** — adopter-facing Conversations contracts and the EventStore-concept boundary (no raw envelopes leaked) are preserved.

### Additional Requirements

*Technical-how / architecture-level input from the addendum, plus constraints and phasing from the PRD. These shape sequencing and story scope.*

**Existing technical-module surface to CONSUME (supports FR-3..FR-9):**
- EventStore.DomainService — `AddEventStoreDomainService([options][,assemblies])` + `UseEventStoreDomainService()` two-line host (FR-3).
- EventStore.Client — `EventStoreAggregate<TState>` reflection dispatch/replay + `OnConfiguring` (FR-7); `IDomainQueryHandler`, `IQueryCursorCodec`, `QueryCursorScope` (FR-4); `IDomainProjectionHandler` full-replay (FR-6); `IReadModelStore` (+ETag) and `ReadModelWritePolicy` reload-merge/optimistic-concurrency/retries (FR-5); `IEventStoreGatewayClient` + `AddEventStoreGatewayClient()`; `AddEventStore([options][,assemblies])` discovery.
- EventStore.ServiceDefaults — `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, `MapDefaultEndpoints` (FR-9/FR-10).
- EventStore.Aspire — `AddHexalithEventStore`, `AddEventStoreDomainModule` shared-vs-isolated Dapr (FR-13).
- EventStore.Testing — `DomainResultAssertions`, envelope/sequence/isolation assertions, `FakeEventStoreGatewayClient`, `InMemoryStateManager`, terminatable compliance (FR-9).
- Commons — `TypeMapper`/`NameTypeMapper`, `FluentValidateOptions<T>`, `IEquatableObject`/`EquatableHelper`, `UniqueIdHelper`/Ulid, `ISettings`/`SettingsHelper` (FR-8/FR-14).
- FrontComposer — `FrontComposerGenerator` source-gen + `FrontComposerTestBase`/host builder (preserve generated behavior).

**Cross-module duplication → PROMOTE targets (supports FR-10..FR-15):** ServiceDefaults extensions (FR-10); ~80-LOC tenant-access projection handler (FR-11); tenant-access DI `AddXxxTenantAccess` (FR-11); client typed-HttpClient registration (FR-12); Aspire/Dapr module topology (FR-13); JsonContext setup (FR-14); domain-processor `TryAddEnumerable` registration `AddDomainProcessor<T>()` (FR-3/FR-10); shared health-check/client registration test fixtures (FR-9). Emulate the proven local standard: EventStore's generic `AddEventStore<TAggregate>()` template-method extension style.

**Confirmed capability GAPS to build only if Conversations consumes them in-pilot:** `ICommandContract`/`IEventContract` metadata (FR-16, conditional); polymorphic JSON registration helper / publicize `TypeMapper` (FR-14); generic tenant-access projection handler (FR-11); generic ServiceDefaults base with hooks (FR-10); generic typed-HttpClient registration (FR-12); generic Aspire/Dapr module hosting base (FR-13). Backlog (NOT in pilot): tier-3 integration test harness; snapshot/upcasting hook; command-level authz/validator discovery; deadletter/poison-pill hook.

**Constraints & guardrails (PRD §9):**
- Cross-submodule coordination is *explicitly authorized* for this initiative (overrides the default "scope changes to Conversations" rule): promotions edit EventStore/Commons/Tenants/FrontComposer submodules, but changes there must be **additive/backward-compatible** so existing consumers keep compiling. Honor the repo rule: never recurse into nested submodules; initialize/update only root-level submodules.
- Greenfield latitude: Conversations is treated as greenfield/pre-release, so plumbing-only tests may be removed with their code; release-gate conformance remains inviolable.
- Public-surface stability: adopter-facing Conversations contracts and the EventStore-concept boundary preserved.

**Phasing (PRD §6.3) — story sequencing should respect this:** Phase 0 Baseline (FR-1, FR-2, FR-19 baseline) → Phase 1 Consume (FR-3..FR-9, low-risk, Conversations-internal, conformance-gated) → Phase 2 Promote (FR-10..FR-16, extract/generalize with own tests) → Phase 3 Adopt & Prove (FR-17..FR-20, template + measurement + final gate).

**Success-metric baselines to capture (drive specific stories):** SM-1 plumbing-LOC reduction target [ASSUMPTION ≥40%, OQ-2]; SM-2 minimal-module file/LOC reduction [ASSUMPTION ≥50% fewer files, OQ-2]; SM-3 duplication-eliminated count (one source of truth per promoted pattern); SM-4 qualitative maintainer signal.

**Open questions that gate or shape stories:** OQ-1 landing zone per promotion (existing module vs new shared module — for architecture); OQ-2 confirm SM-1/SM-2 numeric targets; OQ-3 confirm governance/temporal/hydration classified-and-kept-now / promote-later boundary; OQ-4 FR-16 in-pilot or deferred; OQ-5 explicit hot-path performance budget vs "no regression".

### UX Design Requirements

**N/A — no UI/UX scope.** This is an internal developer-platform refactoring initiative; PRD §5 explicitly lists "Not a UI/UX change — FrontComposer-generated admin surface behavior is preserved, not redesigned" as a Non-Goal. The only "users" are developers authoring Hexalith domain modules. No UX design document applies; FrontComposer-generated admin surface behavior is preserved under FR-20, not redesigned here.

### FR Coverage Map

| FR | Epic | Mapping note |
|----|------|--------------|
| FR-1  | Epic 1 | Canonical Consume/Promote/Keep inventory accepted + baseline plumbing-LOC. |
| FR-2  | Epic 1 | Classification dispute-resolution + reclassification escape hatch. |
| FR-3  | Epic 2 | Wire Conversations onto the shared two-line domain host (greenfield: `Server/Program.cs` is currently unimplemented). |
| FR-4  | Epic 2 | Adopt SDK query-handler + cursor codec; remove hand-rolled HMAC cursor (remove-and-replace). |
| FR-5  | Epic 2 | Adopt shared read-model store + write policy; remove bespoke store/merge (remove-and-replace). |
| FR-6  | Epic 2 | Adopt SDK projection seam; keep field selection/freshness (remove-and-replace orchestration). |
| FR-7  | Epic 2 | Lean on `EventStoreAggregate<TState>` dispatch/replay; remove redundant shims (remove-and-replace). |
| FR-8  | Epic 2 | Adopt shared serialization helpers; keep domain-rule converters (remove-and-replace; may need `NameTypeMapper` micro-promote — see FR-14). |
| FR-9  | Epic 2 | Consume EventStore.Testing assertions/fakes; remove duplicate in-module fakes. |
| FR-10 | Epic 3 | Shared ServiceDefaults base — **greenfield-adopt** (Conversations ServiceDefaults is an empty marker; FR-17 delete N/A). |
| FR-11 | Epic 3 | Generic `TenantAccessProjectionHandler<TEvent,TProjection>` + registration — **promote-then-delete-local** (real ~80-LOC copy exists). Differential adversarial cross-tenant test required. |
| FR-12 | Epic 3 | Generic typed-HttpClient registration — **promote-then-delete-local** (Client pkg isolated, cleanly separable). |
| FR-13 | Epic 3 | Shared Aspire/Dapr hosting base — **greenfield-adopt** (no AspireModule exists yet; FR-17 delete N/A). |
| FR-14 | Epic 3 | Shared JSON-context base / polymorphic registration — **greenfield-adopt** (no `[JsonSerializable]` context exists yet); includes `NameTypeMapper` public micro-promote in Commons if needed. |
| FR-15 | Epic 3 | Telemetry/diagnostics scaffolding helper — **promote-then-delete-local** (real diagnostics extensions exist in Server). |
| FR-16 | Epic 3 | `ICommandContract`/`IEventContract` metadata — **conditional**; in-pilot only if Conversations consumes it, else logged backlog (OQ-4). |
| FR-17 | Epic 3 | Folded into each Epic 3 adopt story's delete step; **N/A for greenfield-adopt capabilities** (FR-10/13/14). |
| FR-18 | Epic 4 | Documented thin authoring template, validated against post-refactor Conversations. |
| FR-19 | Epic 4 | Measured minimal-module authoring cost (files + LOC) = SM-2 baseline. |
| FR-20 | Epic 5 | Behavior-preservation **attestation** (aggregate proof). Per-story conformance AC across Epics 2–4 is the actual gate. |

*Coverage check: FR-1 … FR-20 all mapped. NFR1–NFR8 ride as cross-cutting acceptance criteria on the relevant stories (NFR1/NFR3 especially on Epic 2/3 stories; NFR6 on every Epic 3 promote story).*

## Epic List

### Epic 1: Boilerplate Baseline & Behavior-Preservation Oracle *(Phase 0 — gate-zero)*
Before any code moves, establish a *trusted oracle* to refactor against and an accepted classification spine. Maintainers get the golden conformance baseline (the 14 `*ConformanceSuite` public-surface tests pinned and green on unmodified `main`, public-contract-shape snapshot, blind-spot measurement on the five release-gate behaviors), the three internal-coupled tests decoupled/re-expressed against public surface **before** any refactor (with `GovernanceAuditPairingSafetyNet` explicitly classified *re-express, never delete*), the accepted Consume/Promote/Keep inventory + baseline plumbing-LOC, and a live reclassification escape hatch with logged rationale.
**FRs covered:** FR-1, FR-2 (plus the non-FR gate-zero oracle/decoupling work that FR-20 depends on)
**Standalone:** yes — delivers the safety net + decision spine everything else leans on. Blocks Epic 2.

### Epic 2: Consume Existing Technical-Module Surface *(Phase 1)*
Conversations adopts the EventStore SDK / Commons surface it hand-rolled around (or never wired): host, query/cursor, read-model store, projection seam, aggregate base, serialization, testing. Conversations-internal, low-risk, **no EventStore/Commons backward-compat edits required** (confirmed). Stories distinguish *remove-and-replace* (FR-4/5/6/7/8/9, real hand-rolled code exists) from *greenfield-adopt* (FR-3 host — `Program.cs` is unimplemented). **Every story carries the FR-20 conformance AC.**
**FRs covered:** FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-9 (one story each)
**Standalone:** yes — Conversations sheds/wires plumbing with provably identical behavior. Depends on Epic 1.

### Epic 3: Promote → Adopt (per-capability pipeline) *(merged Phase 2 + Phase 3-adopt)*
Each capability is one self-contained story: **promote → generalize → test in technical module → Conversations adopts → (delete local copy if one exists) → conformance green.** The first story is a deliberate **tracer-bullet** to validate pipeline mechanics. Capabilities split by reality: *promote-then-delete-local* (FR-11 tenant-access, FR-12 client, FR-15 telemetry — real copies exist) vs *greenfield-adopt* (FR-10 ServiceDefaults, FR-13 Aspire, FR-14 JSON-context — no copy; FR-17 delete N/A). Sequenced so each capability's consume (Epic 2) is upstream of its adopt. **Gate: OQ-1 landing zones resolved for in-scope capabilities before the first promote story** (don't promote into the dark). Every promote story carries the NFR6 additive/backward-compatible AC (sibling modules built in CI). FR-11 carries a differential adversarial cross-tenant test (identical denial pre- vs post-promotion).
**FRs covered:** FR-10, FR-11, FR-12, FR-13, FR-14, FR-15, FR-16 *(conditional)*, FR-17 *(folded into each adopt story's delete step)*
**Standalone:** each capability story is independently shippable & revertable. Depends on Epic 2 (and OQ-1).

### Epic 4: Thin Authoring Template & Authoring-Cost Proof *(Phase 3 remainder)*
Runs after the Epic 3 pipeline drains. The reusability payoff made real: a documented thin authoring template validated against post-refactor Conversations, plus the measured minimal-module authoring cost (files + LOC) that becomes the SM-2 baseline.
**FRs covered:** FR-18, FR-19
**Standalone:** yes — produces the template + measurement future module authors consume. Depends on Epics 2–3 (template describes what post-refactor Conversations actually does).

### Epic 5: Behavior-Preservation Attestation & Sign-off *(capstone — attestation, not gate)*
**Not** where conformance first goes green — that happens per-story in Epics 2–4. This assembles the irreducibly whole-system artifacts: full-module conformance green, consolidated public-contract-shape diff (vs the Epic 1 snapshot), the removed-test justification ledger (captured at deletion time per-story, aggregated here), and the success-metric report (SM-1 plumbing-LOC reduction, SM-3 duplication eliminated, SM-4 qualitative maintainer signal). Produces the release owner's signable attestation.
**FRs covered:** FR-20
**Standalone:** yes — the release owner's deliverable. Depends on all prior epics.

## Epic 1: Boilerplate Baseline & Behavior-Preservation Oracle

Establish a *trusted oracle* to refactor against and an accepted classification spine before any code moves. This epic delivers the safety net (pinned conformance suite + measured blind spots + decoupled at-risk tests) and the decision spine (accepted Consume/Promote/Keep inventory + baseline LOC + reclassification rule). It is the gate-zero that blocks Epic 2. Covers FR-1, FR-2, and the non-FR oracle work FR-20 depends on. Relevant NFRs: NFR1 (behavior preservation), NFR3 (fail-closed by construction — adversarial coverage).

### Story 1.1: Pin the conformance oracle green on `main` and snapshot the public contract shape

As a release owner,
I want the existing release-gate conformance suite pinned and proven green on unmodified `main` with a captured public-contract-shape snapshot,
So that every later refactor has a trusted, unchanging behavioral baseline to diff against.

**Acceptance Criteria:**

**Given** unmodified `main`
**When** the 14 `*ConformanceSuite` tests under `tests/Hexalith.Conversations.Conformance.Tests` are run
**Then** all pass (100% green)
**And** the run is recorded as the named behavior-preservation baseline referenced by FR-20.

**Given** the baseline run
**When** the public/adopter-facing contract surface is enumerated (Contracts assembly exported types; public command/query/event/projection shapes)
**Then** a contract-shape snapshot artifact is captured and stored for later diffing
**And** it covers all six release-gate behavior areas' public envelopes (tenant isolation, governance/audit, idempotency, redaction, projection freshness, contract validation).

**Given** the conformance suites
**When** their dependencies are inspected
**Then** they are confirmed public-surface-only (no dependency on internal plumbing types) so the oracle survives the refactor
**And** any suite found to be internally coupled is flagged into Story 1.3.

### Story 1.2: Measure the oracle's blind spots and backfill characterization tests

As a maintainer,
I want the conformance oracle's coverage gaps on the five release-gate behaviors measured and any uncovered behavior pinned by characterization tests before refactoring,
So that a silent fail-open regression cannot pass green through an unexercised path.

**Acceptance Criteria:**

**Given** the pinned suite from Story 1.1
**When** coverage/mutation analysis is run against the five release-gate behaviors (tenant fail-closed, governance audit-pairing, idempotency, redaction replay, projection freshness)
**Then** uncovered or weakly-asserted paths are identified and recorded.

**Given** an identified blind spot
**When** a characterization test is written that asserts current observable behavior on that path
**Then** it runs green on unmodified `main`
**And** it is added to the conformance oracle.

**Given** tenant fail-closed specifically (NFR3)
**When** the blind-spot pass runs
**Then** an adversarial cross-tenant denial path is exercised such that a fail-open mutation is *caught*, not assumed impossible
**And** any remaining accepted coverage gap on a release-gate behavior is logged with explicit rationale.

### Story 1.3: Decouple the internal-coupled tests that would break under refactor

As a maintainer,
I want the internal-coupled test files re-expressed against public surface (or correctly classified) before any plumbing moves,
So that the refactor does not produce false-negative test failures that mask real behavior preservation.

**Acceptance Criteria:**

**Given** `GovernanceAuditPairingSafetyNetTest.cs` (reflection on `ConversationAggregate` internals)
**When** it is re-expressed
**Then** it asserts governance audit-pairing via public commands/events/`DomainResult`
**And** it is explicitly classified "re-express, never delete" because it is a safety net, NOT plumbing-only.

**Given** `ConversationProjectionMaterializerTest.cs` and `IdempotentConversationCommandExecutorTest.cs` (direct concrete instantiation)
**When** they are triaged
**Then** each is either re-expressed to assert behavior through public projection queries / `DomainResult` idempotency surface, OR documented as plumbing-only to be retired with its code in the specific Epic 2/3 story that removes that plumbing.

**Given** each re-expressed test
**When** it runs on unmodified `main`
**Then** it passes (proving it captures current behavior, not the refactor target).

**Given** the triage
**Then** a register maps each at-risk test to `{re-express | plumbing-only-retire}` with rationale
**And** that register seeds the FR-20 removed-test justification ledger.

### Story 1.4: Accept the canonical Consume/Promote/Keep inventory and record baseline plumbing-LOC

As a maintainer,
I want a single accepted inventory artifact classifying every `Hexalith.Conversations.*` source area as Consume/Promote/Keep with evidence and target capability,
So that every downstream story traces to one baseline and SM-1 has a recorded starting figure.

**Acceptance Criteria:**

**Given** the Conversations source
**When** the inventory is assembled
**Then** every top-level source area appears exactly once with a single Consume/Promote/Keep classification, file paths, and approximate LOC.

**Given** each Consume/Promote entry
**Then** it names the technical-module capability it maps to (existing for Consume, to-be-promoted for Promote), consistent with the addendum.

**Given** the inventory
**When** the plumbing baseline is computed
**Then** the baseline plumbing-LOC figure used by SM-1 is derived from it and recorded (addendum first-pass ≈18,000 plumbing LOC confirmed or corrected).

**Given** the completed inventory
**Then** it is marked accepted (status + date)
**And** it is the artifact FR-2 governs.

### Story 1.5: Establish classification dispute-resolution and reclassification escape hatch

As a reviewer,
I want any Consume/Promote/Keep call to be challengeable with the resolution recorded, and any later reclassification to carry a logged rationale,
So that the baseline stays honest when reality disagrees mid-refactor.

**Acceptance Criteria:**

**Given** an accepted classification (Story 1.4)
**When** a reviewer challenges it
**Then** the resolution (uphold or reclassify) is recorded with rationale in the decision log or an inventory note.

**Given** a later story discovers a misclassification (e.g., a Consume item the SDK cannot satisfy becomes a Promote)
**When** it reclassifies the item
**Then** the inventory is updated and the rationale logged, with no silent change.

**Given** the inventory at acceptance
**Then** no area is unclassified or dual-classified.

**And** the escape-hatch procedure is documented so Epic 2/3 stories know how to feed reclassifications back into the inventory.

## Epic 2: Consume Existing Technical-Module Surface

Conversations adopts the EventStore SDK / Commons surface it hand-rolled around (or never wired). Confirmed low-risk and Conversations-internal — no EventStore/Commons backward-compat edits required. Each story is either *remove-and-replace* (real hand-rolled code exists) or *greenfield-adopt* (the slot is unbuilt). Every story carries the FR-20 conformance gate. Covers FR-3…FR-9. Depends on Epic 1 (oracle pinned). Relevant NFRs: NFR1 (behavior preservation), NFR2 (no hot-path regression — preserve snapshot/projection use), NFR8 (public-surface/EventStore-concept boundary preserved).

> **Standing conformance gate (applies to every story in Epics 2–4):** the full conformance suite is 100% green on the story branch; the public contract-shape diff vs the Story 1.1 snapshot is empty or explicitly approved & recorded; the local copy (where one exists) is deleted; no test is deleted without a recorded justification in the FR-20 ledger.

### Story 2.1: Wire Conversations onto the shared two-line domain-service host *(greenfield-adopt)*

As a Conversations maintainer,
I want the server wired onto the shared EventStore domain-service host instead of bespoke DI/host wiring,
So that the module's host is two lines and all canonical endpoints resolve via the SDK.

**Acceptance Criteria:**

**Given** `Hexalith.Conversations.Server/Program.cs` currently throws `NotImplementedException` (greenfield slot)
**When** the host is wired
**Then** it uses `AddEventStoreDomainService(...)` + `UseEventStoreDomainService()` with assembly-scanning registration of Conversations domain processors/handlers.

**Given** the shared host
**When** the app starts
**Then** all canonical domain endpoints (process / replay / query / project / admin metadata) resolve via the shared host.

**Given** any per-feature `ServiceCollectionExtensions` that merely re-implement SDK discovery
**Then** they are removed or never introduced.

**And** the standing conformance gate holds (suite green; contract shape unchanged; ledger updated for any removed test).

### Story 2.2: Adopt `EventStoreAggregate<TState>` base-class conventions *(remove-and-replace)*

As a Conversations maintainer,
I want `ConversationAggregate` to rely on the `EventStoreAggregate<TState>` base-class reflection dispatch and state replay,
So that redundant manual routing and idempotency-bridge shims disappear.

**Acceptance Criteria:**

**Given** `ConversationAggregate`
**When** it is refactored onto the base class
**Then** command routing uses `Handle(TCommand, TState?)` reflection dispatch and state replay uses `Apply(TEvent)`, with redundant manual dispatch / idempotency-bridge shims removed where the base class or SDK already provides them.

**Given** the pure aggregate command/state/event tests
**When** they run
**Then** aggregate behavior is unchanged (green).

**And** the standing conformance gate holds.

### Story 2.3: Adopt the SDK query-handler + cursor codec, remove hand-rolled HMAC cursor *(remove-and-replace)*

As a Conversations maintainer,
I want query handlers implemented against the SDK query-handler seam and the SDK cursor codec,
So that the bespoke query orchestration and hand-rolled HMAC cursor signing are deleted while query behavior is identical.

**Acceptance Criteria:**

**Given** the bespoke query-handler orchestration and hand-rolled HMAC cursor signing/validation
**When** query handlers are implemented against `IDomainQueryHandler` and pagination uses `IQueryCursorCodec` / `QueryCursorScope`
**Then** the hand-rolled cursor signing/validation code is removed
**And** conversation-specific query filters and response shapes remain in the module.

**Given** a paginated query
**When** a cursor is round-tripped
**Then** pagination re-resolves identically (temporal cursors / permalinks re-resolve to the same position — release-gate behavior preserved).

**And** the standing conformance gate holds.

### Story 2.4: Persist read models via the shared store + write policy *(remove-and-replace)*

As a Conversations maintainer,
I want read models persisted and updated through the shared read-model store and optimistic-concurrency write policy,
So that bespoke Dapr state-store calls and merge-on-write loops are removed without changing concurrency behavior.

**Acceptance Criteria:**

**Given** hand-written Dapr state-store calls and merge-on-write loops
**When** read-model persistence uses `IReadModelStore` (+ ETag) and `ReadModelWritePolicy`
**Then** the bespoke store/merge code is removed.

**Given** concurrent writers to the same read model
**When** they race
**Then** optimistic-concurrency / retry behavior is preserved (no lost updates — existing tests green).

**And** the standing conformance gate holds, including no hot-path read regression (NFR2).

### Story 2.5: Implement projections against the SDK projection seam *(remove-and-replace orchestration, keep logic)*

As a Conversations maintainer,
I want projections implemented against the SDK full-replay projection seam with only the domain logic retained,
So that the generic replay/dispatch orchestration is delegated while field selection and freshness stay in Conversations.

**Acceptance Criteria:**

**Given** the generic projection orchestration (replay loop, dispatch registration)
**When** projections implement `IDomainProjectionHandler`
**Then** the orchestration is delegated to the SDK seam
**And** conversation-specific field selection, freshness formula, and evidence construction remain in the module.

**Given** projection rebuild and freshness/degraded-state signaling
**When** the conformance tests run
**Then** they pass (rebuild-safe, replay-tolerant per NFR5).

**Given** the `ConversationProjectionMaterializerTest` triaged in Story 1.3
**When** the materializer plumbing is removed
**Then** any test classified plumbing-only there is retired with a recorded justification; re-expressed behavior assertions stay green.

**And** the standing conformance gate holds.

### Story 2.6: Adopt shared serialization helpers for generic converters *(remove-and-replace)*

As a Conversations maintainer,
I want generic, domain-ruleless serialization converters replaced by shared helpers,
So that only converters encoding genuine domain rules remain and contract shapes stay byte-compatible.

**Acceptance Criteria:**

**Given** generic string/int/value-object converters that carry no domain rules
**When** serialization adopts the shared `TypeMapper` / generic converters / source-generated context base
**Then** those generic converters are replaced and only domain-rule converters remain.

**Given** the `NameTypeMapper` type in Commons is currently `internal` (only `TypeMapper.GetMap()` is public)
**When** a polymorphic registry beyond the public surface is needed
**Then** a public micro-promote in Commons is opened and tracked as a dependency of Epic 3 / FR-14 (otherwise only the public `TypeMapper.GetMap()` is used) — reclassification logged per Story 1.5.

**Given** serialized command/event/projection contracts
**When** round-trip tests run
**Then** shapes are byte/shape-compatible (green).

**And** the standing conformance gate holds.

### Story 2.7: Consume shared EventStore.Testing assertions and fakes *(remove-and-replace, test-only)*

As a Conversations maintainer,
I want test projects to consume the shared EventStore.Testing assertions/fakes and shared ServiceDefaults,
So that duplicate in-module fakes and assertion helpers are removed without weakening the oracle.

**Acceptance Criteria:**

**Given** duplicate in-module fakes/assertions that re-implement `DomainResultAssertions`, `FakeEventStoreGatewayClient`, or `InMemoryStateManager`
**When** the tests adopt the shared EventStore.Testing equivalents
**Then** the in-module duplicates are removed.

**Given** the domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation scenarios)
**Then** they remain unchanged in the module.

**Given** this is a test-only change
**When** the suite runs
**Then** the conformance oracle is still 100% green and its assertion strength is not reduced (no oracle weakening — verified against the Story 1.1 baseline).

## Epic 3: Promote → Adopt (per-capability pipeline)

Each capability is one self-contained story: **promote → generalize → test in the technical module → Conversations adopts → delete local copy (if one exists) → conformance green.** Stories are ordered tracer-bullet-first (lowest-risk, cleanest promote) so pipeline mechanics surface early. Covers FR-10…FR-16 plus FR-17 (folded into each adopt/delete step). The standing conformance gate (defined in Epic 2) applies to every story. Relevant NFRs on every story: NFR1 (behavior preservation), NFR3 (fail-closed by construction — FR-11), NFR4 (observability continuity — FR-15/FR-10), NFR5 (replay safety — FR-11), NFR6 (additive/backward-compatible shared APIs).

> **Epic-level gate (OQ-1):** no promote story starts until the landing zone for *its* capability (existing technical module vs new shared module) is resolved by the downstream architecture workflow. Don't promote into the dark. Each story below carries this as an explicit precondition.
>
> **Per-promote-story standing additions:** the promoted API is additive/backward-compatible (NFR6) and the dependent sibling modules (Folders/Projects/Memories/Parties/Tenants) are built green in CI to prove no break; each promotion is a separate technical-module submodule commit + root-level pointer bump (never recurse into nested submodules).

### Story 3.1: *(Tracer-bullet)* Promote & adopt the generic typed-HttpClient registration

As a technical-module maintainer,
I want the duplicated typed-HttpClient registration extracted into a shared domain-agnostic helper and adopted by Conversations as the pilot tracer-bullet,
So that the per-capability promote→adopt→delete pipeline mechanics are proven on the lowest-risk, cleanly-isolated capability first.

**Acceptance Criteria:**

**Given** the landing zone for client registration is resolved (OQ-1) and the `AddXxxClient()` pattern is identical/domain-agnostic across Folders and Projects
**When** it is promoted into a shared registration helper with options binding and validation, with its own tests
**Then** the helper lives in the chosen technical module.

**Given** the shared helper
**When** Conversations registers its typed client through it
**Then** the hand-rolled `AddHexalithConversationsClient()` pair in `Hexalith.Conversations.Client` is deleted (FR-17)
**And** options validation (missing/relative URL rejection) behavior is preserved.

**Given** this is the tracer-bullet
**Then** the pipeline mechanics (promote → test → adopt → delete → conformance → additive-CI-build → submodule pointer bump) are documented as the reusable runbook for stories 3.2–3.7.

**And** NFR6 holds (Folders/Projects compile green in CI) and the standing conformance gate holds.

### Story 3.2: Promote & adopt the generic tenant-access projection handler + registration

As a technical-module maintainer,
I want the ~80-LOC tenant-access projection handler and its DI registration promoted into a generic `TenantAccessProjectionHandler<TEvent,TProjection>` (+ registration) and adopted by Conversations,
So that fail-closed tenant access has one tested home instead of N copies.

**Acceptance Criteria:**

**Given** the landing zone is resolved (OQ-1) and the handler is byte-for-byte duplicated in Folders/Projects and re-implemented in Conversations
**When** it and `AddXxxTenantAccess()` are promoted into a generic capability parameterized by `<TEvent,TProjection>`, with its own tests
**Then** the generic capability and registration live in the chosen technical module.

**Given** Conversations
**When** it registers the generic capability with concrete types
**Then** the hand-written `ConversationTenantAccessService` handler and bespoke `ConversationTenantAccessServiceCollectionExtensions` are deleted (FR-17).

**Given** fail-closed semantics (NFR3)
**When** tenant state is missing / stale / unavailable / disabled / ambiguous / insufficient
**Then** access fails closed — preserved *by construction* (the API shape makes fail-open unrepresentable), not by convention.

**Given** the differential adversarial test
**When** the same hostile cross-tenant inputs are run against both the pre-promotion inline implementation and the post-promotion shared module
**Then** both deny identically (cross-tenant access impossible by construction).

**Given** duplicate / out-of-order / replayed tenant events (NFR5)
**Then** tolerance is preserved.

**And** NFR6 holds (siblings compile green in CI) and the tenant-isolation conformance suite is green.

### Story 3.3: Promote & adopt the diagnostics/telemetry scaffolding helper

As a technical-module maintainer,
I want the meter/counter/classifier scaffolding promoted into a shared helper and adopted by Conversations,
So that a domain module supplies only metric names and bounded dimension vocabularies.

**Acceptance Criteria:**

**Given** the landing zone is resolved (OQ-1) and the meter-factory / classifier ceremony is repeated per signal
**When** it is promoted into a shared scaffolding helper with its own tests
**Then** the helper lives in the chosen technical module.

**Given** Conversations' `Diagnostics/*` extensions (projection / conformance / rejection / onboarding telemetry)
**When** they adopt the helper
**Then** only domain metric names and dimension enums remain in the module
**And** the bespoke scaffolding is deleted (FR-17).

**Given** observability continuity (NFR4)
**When** metrics are emitted
**Then** metric names and cardinality are preserved so existing dashboards/alerts keep working.

**And** NFR6 holds (siblings compile green in CI), the `TelemetryRedactionConformanceSuite` is green, and the standing conformance gate holds.

### Story 3.4: Promote & adopt the shared ServiceDefaults base *(greenfield-adopt — FR-17 N/A)*

As a technical-module maintainer,
I want a shared ServiceDefaults base with module-specific extension hooks, adopted by Conversations into its currently-empty ServiceDefaults slot,
So that observability/health/resilience/discovery has one home instead of a copied per-module file.

**Acceptance Criteria:**

**Given** the landing zone is resolved (OQ-1) and the near-identical ServiceDefaults files across Folders/Memories/Tenants/Parties (name-swap; Memories adds Redis, Parties adds Dapr health)
**When** a shared ServiceDefaults capability with extension hooks is promoted with its own tests
**Then** the capability lives in the chosen technical module.

**Given** Conversations' ServiceDefaults is currently an empty assembly marker (greenfield slot)
**When** it adopts the shared base
**Then** its ServiceDefaults is reduced to module-specific hooks over the shared base
**And** FR-17 delete is N/A (recorded — there is no local copy to remove).

**Given** health/telemetry endpoints (NFR4)
**When** registration runs
**Then** endpoint behavior is preserved (health/telemetry registration tests green).

**And** NFR6 holds (siblings compile green in CI) and the standing conformance gate holds.

### Story 3.5: Promote & adopt the shared Aspire/Dapr domain-module hosting base *(greenfield-adopt — FR-17 N/A)*

As a technical-module maintainer,
I want a shared Aspire/Dapr hosting base parameterized by app-id/component names and shared-vs-isolated mode, adopted by Conversations,
So that AppHost/Aspire + Dapr sidecar topology is attached via one capability instead of a copied per-module Aspire module.

**Acceptance Criteria:**

**Given** the landing zone is resolved (OQ-1) and the structurally-similar `*AspireModule.cs` files across Folders/Projects
**When** a shared hosting base (shared vs isolated infrastructure modes) is promoted with its own tests
**Then** the capability lives in the chosen technical module.

**Given** Conversations' AppHost is an 11-line placeholder (greenfield slot)
**When** Aspire wiring is expressed through the shared capability with Conversations' app-id / component names / mode
**Then** resource wiring (state-store, pub/sub, sidecar) is equivalent to the sibling topology
**And** FR-17 delete is N/A (recorded).

**And** NFR6 holds (siblings compile green in CI) and the standing conformance gate holds.

### Story 3.6: Promote & adopt the shared JSON-context base / polymorphic registration *(greenfield-adopt — FR-17 N/A)*

As a technical-module maintainer,
I want a shared source-generated JSON-context base / polymorphic registration helper adopted by Conversations, with the polymorphic registry made public as needed,
So that a domain module declares only its serializable type lists instead of hand-assembling resolver combination and type catalogs.

**Acceptance Criteria:**

**Given** the landing zone is resolved (OQ-1) and the identical JsonContext setup pattern (`[JsonSerializable]` lists + resolver combine, seen in Memories)
**When** a source-generated context base / polymorphic registration helper is promoted with its own tests
**Then** the capability lives in the chosen technical module.

**Given** the `NameTypeMapper`-internal gap surfaced in Story 2.6
**When** a polymorphic registry beyond the current public surface is required
**Then** `NameTypeMapper` (or an equivalent public surface) is exposed in Commons additively and the reclassification is logged per Story 1.5.

**Given** Conversations has no JSON serialization context yet (greenfield slot)
**When** it declares its serializable type lists against the shared base
**Then** resolver combination/registration boilerplate is provided by the base
**And** polymorphic (de)serialization of the event/command hierarchies is preserved
**And** FR-17 delete is N/A (recorded).

**And** NFR6 holds (Memories and other siblings compile green in CI) and the contract-serialization conformance tests are green.

### Story 3.7: *(Conditional — OQ-4)* Promote & adopt compile-time command/event contract metadata

As a technical-module maintainer,
I want `ICommandContract` / `IEventContract` metadata interfaces (parallel to the existing `IQueryContract`) promoted and adopted by Conversations,
So that command/event domain+type metadata is declared via shared interfaces instead of magic-string type names — but only if it cuts boilerplate without risky contract reshaping.

**Acceptance Criteria:**

**Given** OQ-4 decides FR-16 is in pilot scope and the landing zone is resolved (OQ-1)
**When** `ICommandContract`/`IEventContract` are promoted with their own tests
**Then** Conversations commands/events declare metadata via the shared interfaces
**And** magic-string type names for those contracts are removed.

**Given** OQ-4 defers FR-16
**When** the decision is recorded
**Then** it is logged as backlog in the addendum with rationale, this story is closed as deferred, and it does **not** block FR-20.

**Given** the in-pilot path
**When** adopting the interfaces would require reshaping a public contract in a way that risks behavior preservation (NFR1/FR-20)
**Then** the story defers instead of reshaping.

**And** if built: NFR6 holds (siblings compile green in CI) and the standing conformance gate holds.

## Epic 4: Thin Authoring Template & Authoring-Cost Proof

The reusability payoff made real. After the Epic 3 pipeline drains, capture the post-refactor Conversations module as a documented thin authoring template and measure the authoring cost of a minimal module on it. Covers FR-18, FR-19. Depends on Epics 2–3 (the template must describe what post-refactor Conversations actually does). This is the SM-2 measurement basis.

### Story 4.1: Document the thin authoring template, validated against post-refactor Conversations

As a domain-module author,
I want a documented authoring template — a minimal module skeleton plus a checklist of the shared capabilities to wire —
So that I can stand up a new Hexalith business-domain module by writing only domain logic.

**Acceptance Criteria:**

**Given** the post-refactor Conversations module
**When** the template is authored
**Then** it enumerates each shared capability with the one-liner to adopt it: shared host (FR-3), aggregate base (FR-7), query + projection handler seams (FR-4/FR-6), read-model store + write policy (FR-5), tenant-access (FR-11), client registration (FR-12), Aspire/Dapr hosting base (FR-13), serialization + JSON-context (FR-8/FR-14), ServiceDefaults (FR-10), and telemetry scaffolding (FR-15).

**Given** the template
**When** it is validated against the real module
**Then** the skeleton + checklist match what post-refactor Conversations actually does (no aspirational or dead steps).

**Given** the minimal skeleton
**Then** it reflects the Hexalith project shape (Contracts / Client / Server / Aspire / AppHost / ServiceDefaults / Testing + focused test project) per project-context conventions.

**And** the template includes the release-gate checklist a new module must satisfy (fail-closed tenant access, governance audit-pairing, idempotency, redaction replay, projection freshness) so a new module inherits the conformance obligations, not just the wiring.

### Story 4.2: Measure and record the minimal-module authoring cost (SM-2 baseline)

As a release owner,
I want the authoring cost of a minimal valid domain module on the template measured and recorded,
So that SM-2 has a concrete baseline proving the authoring surface is thinner.

**Acceptance Criteria:**

**Given** the template from Story 4.1
**When** a minimal "do-nothing-but-valid" domain module is stood up on it
**Then** its file count and LOC are measured and recorded.

**Given** the measurement
**Then** it is traceable to the template
**And** it is compared against the pre-initiative equivalent (the SM-2 target — assumed ≥50% fewer files, to be confirmed via OQ-2).

**And** the figure is recorded as the SM-2 baseline referenced by Epic 5's success-metric report.

## Epic 5: Behavior-Preservation Attestation & Sign-off

The release owner's deliverable. This is **not** where conformance first goes green — that happened per-story across Epics 2–4. This epic assembles the irreducibly whole-system artifacts and produces a single signable attestation. Covers FR-20. Depends on all prior epics. Affirms the inviolable counter-metrics SM-C1 (100% conformance, contracts unchanged) and SM-C2 (no hot-path regression — NFR2/NFR7).

### Story 5.1: Final full-module conformance run + consolidated public-contract-shape diff

As a release owner,
I want the full release-gate conformance suite run on the final refactored module and a consolidated public-contract-shape diff against the Story 1.1 snapshot,
So that I have whole-system proof that no externally-observable behavior or contract shape changed.

**Acceptance Criteria:**

**Given** the fully refactored module (Epics 2–4 complete)
**When** the full release-gate conformance suite runs (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing)
**Then** all pass (100% green).

**Given** the Story 1.1 contract-shape snapshot
**When** the final public/adopter-facing contract surface is diffed against it
**Then** the diff is empty, OR every difference is explicitly approved and recorded.

**Given** the per-story conformance gate held throughout
**Then** this run *confirms* green was never lost — it is confirmation, not first discovery (if it is the first time the full suite goes green, the per-story gate failed and that is flagged).

### Story 5.2: Reconcile the removed-test justification ledger

As a release owner,
I want every removed test reconciled against the FR-20 ledger,
So that I can prove no conformance test was silently dropped under the greenfield test-deletion latitude.

**Acceptance Criteria:**

**Given** the per-story removed-test justifications (captured at deletion time across Epics 1–3) and the at-risk register from Story 1.3
**When** the ledger is reconciled
**Then** every removed test is justified as plumbing-only with rationale.

**Given** any test classified "re-express, never delete" (e.g., `GovernanceAuditPairingSafetyNet`)
**Then** it is confirmed re-expressed and present (not deleted).

**Given** the release-gate conformance suite
**Then** no conformance test was removed, and any net reduction in test count is fully accounted for in the ledger.

### Story 5.3: Assemble the success-metric report and signable attestation

As a release owner,
I want a success-metric report and a signable attestation assembling all behavior-preservation evidence,
So that I can ship the initiative with documented confidence.

**Acceptance Criteria:**

**Given** the Story 1.4 baseline plumbing-LOC and the post-refactor module
**When** SM-1 is computed
**Then** plumbing-LOC reduction is reported against target (assumed ≥40% of classified plumbing removed/delegated — confirm via OQ-2).

**Given** the in-scope promoted patterns (Epic 3)
**When** SM-3 is computed
**Then** each has exactly one shared source of truth (duplication-eliminated count)
**And** the Story 4.2 minimal-module figure is included by reference as SM-2.

**Given** SM-4
**Then** a light qualitative maintainer signal ("the module reads as mostly domain logic") is captured.

**Given** Stories 5.1 (conformance green + contract diff) and 5.2 (reconciled ledger)
**When** the attestation is assembled
**Then** it is a single signable artifact the release owner signs off
**And** SM-C1 (100% conformance pass rate, public contract shapes unchanged) and SM-C2 (no hot-path latency regression) are explicitly affirmed.
