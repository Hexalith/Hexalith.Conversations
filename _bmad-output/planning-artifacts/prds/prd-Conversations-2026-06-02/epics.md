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

**Success-metric baselines to capture (drive specific stories):** SM-1 target ≥40% and SM-2 target ≥50% fewer hand-authored, module-owned files were confirmed by the 2026-07-14 OQ-2 decision. Both comparisons are inclusive; SM-2 file count is decisive, LOC is supporting evidence, and the current SM-2 result remains estimate-qualified. SM-3 is the duplication-eliminated count (one source of truth per promoted pattern); SM-4 is the qualitative maintainer signal.

**Open questions that gate or shape stories:** OQ-1 landing zone per promotion (existing module vs new shared module — for architecture); OQ-3 confirm governance/temporal/hydration classified-and-kept-now / promote-later boundary; OQ-4 FR-16 in-pilot or deferred; OQ-5 explicit hot-path performance budget vs "no regression". **Resolved:** OQ-2 confirmed the SM-1/SM-2 target interpretation on 2026-07-14; see `docs/release-evidence/oq-2-target-interpretation-decision-v1.json`.

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
**And** it is compared against the pre-initiative equivalent (the SM-2 target is confirmed at ≥50% fewer hand-authored, module-owned files, inclusive; the result remains estimate-qualified under the 2026-07-14 OQ-2 decision).

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
**Then** plumbing-LOC reduction is reported against the confirmed inclusive target of ≥40% of the frozen classified-plumbing baseline removed or externalized (OQ-2 resolved 2026-07-14).

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

<!-- EPIC-6-AUTHORITY-OVERLAY:BEGIN version=epic-6-authority-2026-07-15-v2 prefix-bytes=55536 prefix-sha256=bd437b802513591c4af299ff0997bb694ced40304e1a178c3d53e95f88f0e8a8 supersedes=epic-6-authority-2026-07-15-v1 -->

## Appendix: 2026-07-15 Append-Only Corrective Authority Overlay

**Overlay version:** `epic-6-authority-2026-07-15-v2`
**Architecture authority:** `conversations-architecture-2026-07-15-v2`
**Supersedes:** `epic-6-authority-2026-07-15-v1`
**Status:** active corrective plan; Epics 1-5 above remain immutable historical execution records.

This appendix applies the finalized initiative PRD/addendum and the approved July 15 correction proposals without rewriting completed history. The original 55,536-byte epic-plan prefix is preserved byte-for-byte. Its 24 stories, Epics 1-5, retrospectives, `done` states, and signed v1 evidence remain historical facts; this overlay alone assigns current corrective disposition.

**Overlay amendment log.** Every amendment to this overlay after its initial freeze must bump the overlay version and be recorded here, so a derived context can never claim correspondence with an overlay it does not match.

| Overlay version | Date | Amendment | Authority |
| --- | --- | --- | --- |
| `epic-6-authority-2026-07-15-v1` | 2026-07-15 | Initial append-only corrective authority overlay. | `sprint-change-proposal-2026-07-15.md` and `sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md` |
| `epic-6-authority-2026-07-15-v2` | 2026-07-21 | Added the mandatory production projection read-store population proof to Story 6.2 (AC 4-6) and its consumption to Story 6.6 (AC 4). | `sprint-change-proposal-2026-07-15-projection-read-store-population.md` and accepted `docs/adrs/0003-projection-read-store-population-proof.md` |

### Requirement Authority And Denominators

The initiative surface is exactly: `FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-9, FR-10, FR-11, FR-12, FR-13, FR-14, FR-15, FR-16, FR-17, FR-18, FR-19, FR-20`.

Pilot activation covers FR-1 through FR-15 and FR-17 through FR-20. **FR-16 is the only initiative non-activation** and remains deferred. The complete preservation denominator remains literal and cannot shrink: **all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria**. Preserved feature scope constrains behavior but is not activated for new delivery by this corrective epic.

A delivered-to-inactive disposition or compatible public-contract change requires all three: **named owner approval, recorded rationale, and compatibility evidence**. The accepted **13,289-LOC SM-1 baseline** and all signed v1 evidence remain immutable. Current corrected evidence is versioned separately and cannot substitute this workflow baseline for historical v1 provenance.

### Exact Historical Story Dispositions

| Story | Current finding | Epic 6 disposition |
| --- | --- | --- |
| 1.1 | Critical: no finalized frozen manifest contract. | Superseded for current readiness by 6.3. |
| 1.2 | Major: blind-spot analysis is unbounded. | Retain historical output; register bounded evidence/dispositions through 6.3. |
| 1.3 | Structurally sound. | Retain and map its register into the v2 manifest. |
| 1.4 | Major: story wording retains obsolete ~18,000-LOC uncertainty. | Preserve accepted 13,289-LOC baseline; add historical correction note. |
| 1.5 | Structurally sound. | Retain; apply its approval model to v2 manifest governance. |
| 2.1 | Critical: local host framing. | Superseded by platform-host migration in 6.2. |
| 2.2 | Mostly sound. | Revalidate under 6.6. |
| 2.3 | Mostly sound. | Revalidate under 6.6. |
| 2.4 | Major: no measurable SM-C2 gate. | Revalidate with the reproducible <=5% P95 gate in 6.6. |
| 2.5 | Mostly sound. | Revalidate under 6.6. |
| 2.6 | Critical: forward dependency on 3.6. | Record historical closure; current surface fixed by 6.1 and revalidated by 6.6. |
| 2.7 | Major: ambiguous ServiceDefaults dependency. | Remove local defaults scope in 6.2; revalidate testing evidence in 6.6. |
| 3.1 | Mostly sound. | Retain; reflect its landing zone in corrected architecture. |
| 3.2 | Major: oversized cross-repository slice. | Retain delivered capability; revalidate fail-closed behavior through 6.6. |
| 3.3 | Mostly sound. | Retain; bind metric-name/cardinality evidence through 6.3 and 6.6. |
| 3.4 | Critical: Conversations-owned ServiceDefaults facade/project. | Superseded by 6.2. |
| 3.5 | Critical: Conversations-owned AppHost/topology. | Superseded by 6.2. |
| 3.6 | Major: uncertain extension boundary. | Record exact shared public surface in 6.1; revalidate in 6.6. |
| 3.7 | Critical: FR-16 remained conditionally executable. | Mark deferred/non-adopted for Conversations; additive platform metadata remains outside pilot acceptance. |
| 4.1 | Critical: template teaches prohibited project ownership. | Superseded by 6.5. |
| 4.2 | Major: evidence lacks a reproducible fixture and full metadata. | Superseded by 6.5 v2 evidence. |
| 5.1 | Critical: final proof lacks finalized manifest governance. | Preserve v1; supersede through 6.6. |
| 5.2 | Critical: loose removal-ledger model. | Preserve v1; supersede through v2 manifest mutation governance. |
| 5.3 | Critical: missing SM-C2 threshold and complete traceability. | Preserve v1; supersede through 6.6. |

Exactly one row exists for each historical story; no row changes the historical story text or status.

### Corrective Initiative-FR Coverage

| Initiative requirement | Current corrective landing |
| --- | --- |
| FR-1, FR-2 | Accepted inventory and 13,289-LOC baseline remain frozen; 6.3 carries them into the complete manifest. |
| FR-3 | 6.1 establishes platform-owned host authority; 6.2 migrates; 6.6 revalidates. |
| FR-4 through FR-9 | Delivered surfaces remain historical and are revalidated through 6.3/6.6 without feature expansion. |
| FR-10 | 6.1 fixes EventStore ServiceDefaults/DomainService ownership; 6.2 removes local defaults drift; 6.6 verifies. |
| FR-11, FR-12, FR-14, FR-15 | 6.1 fixes public landing zones; 6.3 binds evidence; 6.6 revalidates. |
| FR-13 | 6.1 fixes platform AppHost/EventStore.Aspire ownership; 6.2 migrates topology; 6.6 verifies. |
| FR-16 | Deferred and non-activated; no Conversations adoption. |
| FR-17 | 6.2 establishes the platform-hosted module; 6.5 corrects the authoring template. |
| FR-18 | 6.5 produces the corrected thin template and reproducible fixture. |
| FR-19 | 6.5 produces versioned, reproducible SM-2 evidence; 6.6 reports final results. |
| FR-20 | 6.3 defines the complete preservation denominator; 6.6 runs it and issues superseding evidence. |

## Epic 6: PRD Alignment And Preservation Reconciliation

Epic 6 is the only active corrective epic. It does not activate preserved feature scope or rewrite prior delivery. Architecture status remains `READY FOR CORRECTIVE IMPLEMENTATION ONLY` until all Epic 6 gates pass and readiness is reassessed.

### Story 6.1: Rebaseline architecture and planning authority

As a platform architect, I want architecture and epic authority reconciled to the finalized PRD, so corrective implementation starts from one ownership and decision model.

**Acceptance Criteria:**

1. Architecture distinguishes 20 initiative FRs from 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria; preserves the 13,289-LOC baseline; and defers only FR-16.
2. FR-10 through FR-15 have verified public platform landing zones; OQ-1 through OQ-5 each have one resolved row; the canonical host pair is `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()`.
3. A nonempty versioned hot-path inventory is frozen before baseline capture and enforces `post P95 <= 1.05 x baseline P95` under an identical reproducible envelope.
4. The target tree contains no Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime project; current local projects are labeled pre-6.2 migration input only.
5. This overlay preserves the original epic prefix and signed v1 evidence byte-for-byte, carries all 24 dispositions, Stories 6.1-6.7, and the promotion-completion invariant.

### Story 6.2: Migrate Conversations to platform-owned hosting

As a Conversations maintainer, I want Conversations composed exclusively by the platform host, so the domain module contains no platform-owned hosting boilerplate.

**Acceptance Criteria:**

1. The versioned pre-correction SM-C2 benchmark is captured before topology changes, or reproducibly reconstructed from the preserved source commit.
2. Local AppHost/ServiceDefaults projects, tests, and solution entries are removed only in this story; platform-host registration preserves topology, security, health, publication, admin composition, and public contracts.
3. Generic gaps are implemented in their owning platform public surface, not a Conversations facade, and all affected promotions pass Story 6.7's completion gate.
4. Conversations exposes a canonical named `IAsyncDomainProjectionHandler` route that reuses the existing materializer and persists both the tenant-scoped per-conversation summary/detail model and tenant index through `ConversationProjectionReadModelWriter`, `ReadModelWritePolicy`, and the configured `IReadModelStore`; completion is reported only after both writes are durable.
5. Versioned `projection-read-store-population-proof-v2` evidence demonstrates an accepted append or authorized replay crossing the production EventStore named-dispatch boundary into the Conversations handler, asserts the actual integration state-store end state and production query result, and does not call the writer directly.
6. Focused integration tests prove duplicate delivery, retry after partial write, tenant isolation, bounded failure outcomes, derived-state deletion, and full replay converge to an equivalent per-conversation record and duplicate-free tenant index. The legacy opaque projection response, DI resolution, mock calls, and HTTP acceptance alone are insufficient proof.

### Story 6.3: Create the complete preservation traceability manifest

As a release owner, I want a frozen, versioned preservation manifest with complete requirement dispositions, so preservation claims are exact and resistant to denominator drift.

**Acceptance Criteria:**

1. The manifest covers all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, all UX acceptance criteria, current controls, and preserved public contracts with zero gaps.
2. Every obligation has evidence or named-owner approved non-activation with rationale; delivered-to-inactive and compatible changes include required approval and compatibility evidence.
3. Source/build/test/baseline hashes, versioned mutation governance, module/platform control separation, and automated zero-gap validation are recorded.

### Story 6.4: Repair UX provenance and preservation governance

As a UX governance owner, I want the UX specification treated as a preservation reference with reliable evidence mappings, so it constrains behavior without silently authorizing UI delivery.

**Acceptance Criteria:**

1. Finalized provenance, a preservation-only banner, non-activated component roadmap, and reliable manifest/evidence/disposition mappings replace stale current story-number authority.
2. Historical mappings remain labeled provenance; preserved absolute targets activate only through separate approval; current FrontComposer, Fluent UI V5, Fluent 2 token, reuse-first, and page-section accordion rules remain binding.
3. No production UI change is authorized.

### Story 6.5: Correct the thin authoring template and reproduce SM-2

As a domain-module author, I want a platform-hosted thin template with reproducible authoring-cost evidence, so SM-2 measures only code a domain module owns.

**Acceptance Criteria:**

1. The template/fixture contains no domain-owned AppHost, Aspire, ServiceDefaults, or equivalent project and uses verified live public platform APIs.
2. A reproducible minimal fixture and versioned v2 measurement record frozen inclusion rules, source paths, commands/tool versions, commit/build identity, file count, LOC, evidence confidence, and named acceptance.
3. The 13,289-LOC SM-1 baseline remains unchanged and validators reject prohibited target ownership.

### Story 6.6: Revalidate and issue superseding attestation

As a release owner, I want the corrected implementation revalidated against the complete preservation contract, so a new readiness decision rests on current evidence.

**Acceptance Criteria:**

1. One post result exists for every frozen SM-C2 row and each satisfies `post P95 <= 1.05 x baseline P95` under the same evidence envelope.
2. The v2 manifest passes 100%; public contracts are equal or have approved compatible-change evidence; topology/security/health/publication/admin composition, SM-1, reproducible SM-2, and SM-3 are evidenced.
3. Versioned v2 attestation, separate supersession record, and new release-owner decision preserve signed v1 evidence unchanged; the readiness rerun returns `READY`. This story is last.
4. The v2 attestation consumes and hash-validates accepted ADR 0003 and the Story 6.2 `projection-read-store-population-proof-v2` artifacts, reruns their focused conformance and rebuild gates, and does not inherit the signed v1 projection-population deferral as proof or as a waiver for current readiness.

### Story 6.7: Mechanically block incomplete submodule promotions from completion

As a Hexalith development-workflow maintainer, I want promotion-bearing work to pass a mechanical submodule completion gate, so dirty submodules and uncaptured umbrella gitlinks cannot reach `done`.

**Acceptance Criteria:**

1. Promotion-bearing work declares exact root `references/...` paths and whether remote commit availability is required; affected scope also includes gitlinks changed since baseline.
2. Each affected submodule is initialized, clean including untracked files, satisfies its availability policy, and is represented by the exact mode-`160000` gitlink in the committed umbrella revision.
3. Stable machine-readable blockers prevent review/completion workflows from writing `review`/`done`; unrelated state warns without blocking.
4. Discovery uses root `.gitmodules` only and never initializes or traverses nested submodules; isolated Git fixtures prove success, failure, and concurrency cases.

### Binding Dependency Order

`6.1 -> 6.7 -> 6.2`

- Story 6.1 establishes authority before any other corrective completion.
- Story 6.7 and the frozen SM-C2 benchmark both precede Story 6.2 completion.
- Stories 6.3 and 6.4 may proceed after 6.1 where dependencies allow.
- Story 6.2 precedes Story 6.5.
- Story 6.6 remains last and triggers readiness reassessment.

<!-- EPIC-6-AUTHORITY-OVERLAY:END version=epic-6-authority-2026-07-15-v2 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN version=epic-6-authority-2026-07-27-v3 supersedes=epic-6-authority-2026-07-15-v2 -->

## Appendix: 2026-07-27 Module Test-AppHost Authority Amendment

**Overlay version:** `epic-6-authority-2026-07-27-v3`
**Architecture authority:** `conversations-architecture-2026-07-27-v3`
**Supersedes:** `epic-6-authority-2026-07-15-v2` only for the ownership and
treatment of `Hexalith.Conversations.AppHost`
**Status:** active corrective amendment; the v1/v2 overlays, completed history,
and signed evidence remain immutable historical records.

### Corrected Ownership Decision

`src/Hexalith.Conversations.AppHost/` remains in the module solely as a
non-packable, non-publishable composition harness for Conversations-limited
local user and end-to-end tests. It is not a production or deployment
composition root. Platform deployment owns production composition, and public
EventStore/Commons surfaces own reusable hosting, DAPR, health, telemetry,
projection/query, publication, and subscription capability.

This amendment does not authorize a Conversations-owned reusable Aspire library
or generic ServiceDefaults facade. `Hexalith.Conversations.ServiceDefaults`
remains removable when it only wraps shared platform defaults. The canonical
runtime host remains `AddEventStoreDomainService(...)` plus
`UseEventStoreDomainService()`.

### Superseding Story Dispositions

| Story | v3 disposition |
| --- | --- |
| 6.1 | Remains completed historical authority. AC 4 is superseded only to permit the non-shipping module test AppHost; reusable runtime capability and production composition remain platform-owned. |
| 6.2 | Retain and constrain the existing AppHost as test-only, remove generic hosting drift, and preserve every production projection/evidence gate. Do not select or modify FrontComposer.AppHost or EventStore.AppHost. |
| 6.3 | No semantic change; bind the v3 architecture and corrected topology evidence in the preservation manifest. |
| 6.4 | No change. |
| 6.5 | Include one non-shipping module test AppHost in the thin fixture and count its hand-authored files/LOC in SM-2. |
| 6.6 | Verify the test-only AppHost boundary and the unchanged production runtime/projection evidence. |
| 6.7 | No change; it remains prerequisite authority for promotion-bearing work. |

### Story 6.2 Corrected Acceptance

Story 6.2 keeps the title **Migrate Conversations to platform-owned hosting**:
production runtime capability and deployment composition remain platform-owned,
while the local AppHost is retained only as test infrastructure.

1. Capture the versioned pre-correction SM-C2 benchmark before runtime,
   projection, or topology changes, or reproducibly reconstruct it from the
   preserved source commit.
2. Retain `src/Hexalith.Conversations.AppHost/`,
   `tests/Hexalith.Conversations.AppHost.Tests/`, and their solution entries.
   Make the project mechanically non-packable and non-publishable, and limit it
   to Conversations Server/Admin Web plus required platform dependencies for
   local module user and end-to-end testing.
3. Remove `Hexalith.Conversations.ServiceDefaults` when it has no independently
   justified domain responsibility. Do not introduce a Conversations Aspire,
   DAPR, publication, health, telemetry, projection/query, or subscription
   facade. Generic gaps land on approved public platform surfaces and every
   affected promotion passes Story 6.7.
4. Preserve v2 AC 4-6 and ADR 0003 unchanged: the scoped named asynchronous
   projection handler must durably populate both tenant-scoped read-model keys,
   and production-boundary append/replay, state-store, query, retry, isolation,
   deletion, and rebuild proof remains mandatory.
5. AppHost composition tests prove the harness consumes public platform helpers,
   cannot be published or packed, and exercises production Server/EventStore
   boundaries without becoming a deployment artifact.

### Story 6.5 Corrected Acceptance

The thin authoring template contains one non-packable, non-publishable
module-scoped AppHost for local user/end-to-end tests and includes its
hand-authored files and LOC in SM-2. It contains no reusable module-owned Aspire
library, generic ServiceDefaults facade, DAPR implementation, projection/query
runtime, publication, health, telemetry, or subscription plumbing. Validators
reject those duplicated capabilities and reject a publishable module AppHost.

### Story 6.6 Corrected Evidence

Final evidence proves the Conversations AppHost is test-only and uses public
platform capability; production deployment remains platform-owned. All v2
manifest, contract, topology behavior, projection-population, SM-C1, SM-C2,
SM-2, SM-3, promotion, signed-v1 immutability, supersession, and readiness gates
remain unchanged.

### Binding Dependency Order

`6.1 authority correction -> 6.7 -> 6.2 -> 6.5 -> 6.6`

Stories 6.3 and 6.4 may still proceed after 6.1 where dependencies allow. The
SM-C2 baseline remains a pre-change gate for 6.2. No sprint-status change or new
story identifier is introduced by this amendment.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:END version=epic-6-authority-2026-07-27-v3 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:BEGIN version=epic-6-authority-2026-07-28-v4 supersedes=epic-6-authority-2026-07-27-v3 -->

## Appendix: 2026-07-28 Mechanical Final-Record Authority Amendment

**Overlay version:** `epic-6-authority-2026-07-28-v4`
**Architecture authority:** `conversations-architecture-2026-07-28-v4`
**Supersedes:** `epic-6-authority-2026-07-27-v3` only by adding Story 6.8 and
amending the binding dependency order
**Status:** active corrective amendment; the v1, v2, and v3 overlays, completed
history, and signed evidence remain immutable historical records.

### Mechanical Final-Record Decision

A story completion record states four families of fact: final test counts, the
file list, affected submodule state, and root gitlink state. Every one of them
is observable from the repository at completion time, and one of them is already
produced mechanically by the Story 6.7 promotion checker. From this amendment
forward, those facts are **derived outputs of a generator**, not prose composed
by the agent that performed the work.

A completion record whose counts, paths, or gitlink binding are typed by hand,
carried forward from an earlier pass, or restated in a second hand-maintained
list is a conformance failure. Narrative prose may surround a generated record;
it may not restate the generated numbers.

Derivation sources are exactly four: parsed machine-readable test-result
artifacts; the git-derived path set between the work baseline and the committed
candidate with source-tree dirt blocked outside record outputs and declared TRX
inputs; mode-`160000` root
gitlink entries resolved from the committed candidate; and the Story 6.7
promotion-checker document embedded verbatim. A record that could not derive any
of them reports a blocker rather than a pass.

### Story 6.8: Generate the final story record mechanically from measured state

**Goal.** Close the standing action item "make final story record generation
mechanical from final test counts, file list, submodule state, and root gitlink
state" for every Epic 6 story that completes after Story 6.2.

**Acceptance criteria.**

1. One generator emits a versioned final-record document whose every field is
   derived from the four sources named above. No count, path, or commit may be
   supplied as caller-authored text.
2. Test counts come only from machine-readable result artifacts. A declared test
   project with no artifact is recorded as not run and blocks; totals are
   computed rather than transcribed; an artifact older than the newest file in
   the derived file list blocks as stale rather than being carried forward. The
   root `.slnx` defines required root-owned test projects; failures block and
   skips require exact versioned identity-and-reason policy.
3. The file list is derived, singular, and boundary-correct. A path inside a
   root-declared submodule blocks: it belongs to that repository's own record.
   Gitlink promotions appear in a separate labeled section with recorded commit
   and mode.
4. Submodule and gitlink state binds to the candidate that is actually final.
   The candidate must be an ancestor of the committed head; only record-output
   paths may change after it and no gitlink may move, so a superseded binding
   goes red rather than stale.
5. The four completion surfaces generate one document-and-Markdown bundle rather
   than author, verify the inserted Markdown digest, and let generator blockers
   block `review` and `done` exactly as the promotion gate does.
6. The generator cannot report a pass having derived nothing, and the
   invocation cannot be silently removed from a completion workflow.
7. A read-only historical mode verifies already-closed records without mutating
   them, and does not claim to reconstruct a former uncommitted working tree.
8. Every guard is fault-injected and proven able to fail, with each mutated
   artifact restored byte-identically.

**Prohibitions.** Story 6.8 does not modify production source, public contracts,
package versions, generated output, accepted baselines, signed evidence, or
sibling submodule source. It does not rewrite closed story records. It does not
initialize, update, fetch, or traverse submodules. It does not claim to have
wired any gate into continuous integration; automatic execution of the planning,
promotion, and final-record gates remains a single recorded deferred item.

### Superseding Story Dispositions

| Story | v4 disposition |
| --- | --- |
| 6.1 | No change. Completed historical authority; this amendment appends and does not rewrite it. |
| 6.2 | No change. It completes under the pre-6.8 process and is afterwards verified read-only in historical mode. |
| 6.3 | No semantic change; its completion record is generated rather than authored. |
| 6.4 | No semantic change; its completion record is generated rather than authored. |
| 6.5 | No semantic change; its completion record is generated rather than authored, and its SM-2 measurement remains story-owned evidence. |
| 6.6 | No semantic change; it consumes generated records for every prior Epic 6 story and reruns the final-record gate. |
| 6.7 | No change; its checker becomes an embedded input to the final-record generator instead of a separately transcribed step. |
| 6.8 | New corrective story, defined above. |

### Binding Dependency Order

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`

Story 6.8 follows Story 6.2 and precedes the completion of Stories 6.3, 6.4,
6.5, and 6.6: no story completing after Story 6.2 may reach `done` without a
mechanically generated final record. Stories 6.3 and 6.4 may still begin after
6.1 where dependencies allow. The SM-C2 baseline remains a pre-change gate for
6.2, and Story 6.6 remains last. This amendment introduces one new story
identifier and one sprint-status entry.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:END version=epic-6-authority-2026-07-28-v4 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:BEGIN version=epic-6-authority-2026-07-28-v5 supersedes=epic-6-authority-2026-07-28-v4 -->

## Appendix: 2026-07-28 Conformance Oracle Tiering Authority Amendment

**Overlay version:** `epic-6-authority-2026-07-28-v5`
**Architecture authority:** `conversations-architecture-2026-07-28-v5`
**Supersedes:** `epic-6-authority-2026-07-28-v4` only by adding Story 6.9,
amending the acceptance of Stories 6.3 and 6.6, and amending the binding
dependency order
**Status:** active corrective amendment; the v1, v2, v3, and v4 overlays,
completed history, and signed evidence remain immutable historical records.

### Conformance Oracle Tiering Decision

The conformance oracle asserts two different contracts and has never
distinguished them. One tier is reproducible by any consumer of the shipped
Conversations packages. The other drives Conversations-owned decision code —
tenant-access guards, the idempotent command executor, the governance audit
sink, the projection materializer, and the diagnostics classifiers — which has
no public surface and must not acquire one merely to be testable.

The `Hexalith.Conversations.Conformance.Tests -> Hexalith.Conversations.Server`
project reference was recorded by Story 1.1 as an oracle-survivability risk
because the oracle compiled against the plumbing assembly the refactor was about
to move. Epics 2 and 3 moved that plumbing to `Hexalith.EventStore.DomainService`,
`Hexalith.Commons.*`, and platform deployment. What remains in
`Hexalith.Conversations.Server` is the domain-owned behavior this epic's
corrected ownership spine assigns to Conversations. The original premise has
expired; the residual coupling is a mislabeling, not a defect awaiting removal.

From this amendment forward the oracle is **two declared tiers**:

- **Portable tier** — binds `Hexalith.Conversations.Contracts`,
  `Hexalith.Conversations.Client`, and `Hexalith.Conversations.Testing` only.
  It references no non-packable module assembly. This property is asserted by a
  test over the resolved compile surface, not claimed in prose.
- **Module-internal tier** — explicitly binds
  `Hexalith.Conversations.Server`. Its coupling is a declared and correct
  property, not a defect scheduled for removal.

Binding consequences:

- Widening the Conversations public contract so an assertion can move to the
  portable tier is **prohibited**. Test reachability is not a reason to expose a
  domain implementation type.
- Weakening or deleting an assertion so it can move to the portable tier is a
  **conformance failure**. An assertion that cannot be re-expressed at full
  strength belongs in the module-internal tier, and that is a correct outcome,
  not a deferral.
- Both tiers are release-gate. Tier membership governs what an assertion may
  bind, never whether it runs. The frozen FR-20 denominator is unchanged.

### Story 6.9: Tier the conformance oracle and make the portable tier structural

**Goal.** Close the standing Epic 5 action item "Decide the long-term path for
residual Conformance.Tests to Server coupling," open since the Epic 3
retrospective and deferred through Stories 3.3 and 5.2.

**Acceptance criteria.**

1. Every file in the conformance project that binds a
   `Hexalith.Conversations.Server` namespace is triaged in a versioned record.
   Each is either re-expressed against public Contracts, Client, or Testing
   surfaces with its assertion strength preserved, or assigned to the
   module-internal tier with the exact type and reason it cannot be
   re-expressed. Widening the public contract is not an available resolution.
2. The portable tier carries no project reference to a non-packable module
   assembly, and a test in that tier asserts this from the resolved compile
   surface rather than from project-file text.
3. No manifested test is removed, skipped, renamed away, or weakened. The
   executed conformance test count is monotonic against the pre-split figure,
   computed across both tiers. The pre-split figure is derived from a
   machine-readable result artifact, never transcribed.
4. Reclassification of the three manifested denominator suites
   (`TelemetryCardinalityConformanceSuiteTest`,
   `TelemetryRedactionConformanceSuiteTest`,
   `ConformanceStatusConformanceSuiteTest`) records named-owner approval,
   rationale, and a versioned manifest update per FR-20. Frozen denominator
   membership is unchanged; only the recorded tier changes.
5. A versioned v2 disposition artifact supersedes the v1
   `projectReferenceDisposition` target end-state. The v1 artifacts are not
   edited.
6. Both tiers are declared to the Story 6.8 final-record generator and to the
   solution file, so neither tier is silently unrun.

**Prohibitions.** Story 6.9 does not modify production source under `src/`, does
not add, remove, or change any public contract type, does not alter the frozen
FR-20 denominator membership, does not edit signed or immutable v1 evidence, and
does not perform any submodule promotion.

**Permitted outcome.** If the triage finds that the coupled assertions
re-express publicly at unchanged strength, a single portable project with the
reference removed is a valid and successful result. This amendment commits to
tiering the oracle, not to producing two projects.

### Superseding Story Dispositions

| Story | v5 disposition |
| --- | --- |
| 6.1 | No change. Completed historical authority; this amendment appends and does not rewrite it. |
| 6.2 | No change. Story 6.9 touches only test projects and evidence artifacts and does not constrain the hosting migration. |
| 6.3 | Amended acceptance below. Manifest must record oracle tiering as a current control. |
| 6.4 | No change. |
| 6.5 | No change. |
| 6.6 | Amended evidence below. The v2 attestation consumes the tiering decision and reruns both tiers. |
| 6.7 | No change. |
| 6.8 | No change. Its declared-project list is the mechanism by which Story 6.9 AC6 is enforced; a forgotten declaration blocks under existing Story 6.8 AC2. |
| 6.9 | New corrective story, defined above. |

### Story 6.3 Amended Acceptance

In addition to its v2 acceptance criteria, the preservation traceability
manifest records the declared tier of every conformance assertion and binds
`conformance-oracle-tiering-decision-v2` by hash. The portable tier's freedom
from non-packable module bindings is recorded as a validated test outcome, not
as an assertion of the manifest author. Tier structure is a current control;
omitting it is a zero-gap validation failure.

### Story 6.6 Amended Evidence

In addition to its v2 and v3 evidence obligations, the v2 attestation consumes
and hash-validates `conformance-oracle-tiering-decision-v2` and the Story 6.9
triage record, reruns both conformance tiers, and reports their counts
separately and summed. It states the portable tier's structural property as a
test result rather than as prose. It does not inherit the v1
`projectReferenceDisposition` target end-state as either a met obligation or a
waiver.

### Binding Dependency Order

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`, with
`6.9 -> 6.3` and `6.9 -> 6.6`.

Story 6.9 may proceed after Story 6.1 and precedes the completion of Stories 6.3
and 6.6. It is not placed inside the `6.1 -> 6.7 -> 6.2 -> 6.8` spine because it
changes no production source, performs no promotion, and has no dependency on
the hosting migration or the record generator; serializing it behind them is
what deferred this item three times. Story 6.6 remains last. This amendment
introduces one new story identifier and one sprint-status entry.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:END version=epic-6-authority-2026-07-28-v5 -->
