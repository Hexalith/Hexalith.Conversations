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
candidate unioned with the tracked working-tree delta; mode-`160000` root
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
   the derived file list blocks as stale rather than being carried forward.
3. The file list is derived, singular, and boundary-correct. A path inside a
   root-declared submodule blocks: it belongs to that repository's own record.
   Gitlink promotions appear in a separate labeled section with recorded commit
   and mode.
4. Submodule and gitlink state binds to the candidate that is actually final.
   The candidate must be an ancestor of the committed head with no declared
   gitlink movement after it, so a superseded binding goes red rather than
   stale.
5. The four completion surfaces generate rather than author, and generator
   blockers block `review` and `done` exactly as the promotion gate does.
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

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:BEGIN version=epic-6-authority-2026-07-31-v6 supersedes=epic-6-authority-2026-07-28-v5 -->

## Appendix: 2026-07-31 SM-C2 Threshold And Record-Contract Authority Amendment

**Overlay version:** `epic-6-authority-2026-07-31-v6`
**Architecture authority:** `conversations-architecture-2026-07-31-v6`
**Supersedes:** `epic-6-authority-2026-07-28-v5` only by amending Story 6.2's
AC1 SM-C2 pass rule, republishing four record-contract improvements that were
edited into v4 out of process, correcting Story 6.2's completion-process
disposition, and adding Story 6.11
**Status:** active corrective amendment; the v1, v2, v3, v4, and v5 overlays,
completed history, and signed evidence remain immutable historical records.

### Overlay Amendment Log — Continuation

The v2 overlay's amendment-log table requires every later amendment to record
itself there. That table sits inside the byte-range the conformance oracle pins
as immutable, so writing into it is a mutation of frozen authority and the
oracle correctly rejects it. That is why the v3, v4, and v5 amendments each
bumped the overlay version and appended a block but none appears in the table:
the rule and the immutability guard contradict each other. The log is continued
here, outside the pinned range, and every later amendment appends to this
continuation rather than reaching back into v2.

| Overlay version | Date | Amendment | Authority |
| --- | --- | --- | --- |
| `epic-6-authority-2026-07-27-v3` | 2026-07-27 | Corrected Story 6.2's AppHost acceptance: the module test-AppHost is retained as a non-shipping harness rather than removed. | `sprint-change-proposal-2026-07-27.md` |
| `epic-6-authority-2026-07-28-v4` | 2026-07-28 | Made the final story record a mechanical derivation from measured state, added Story 6.8, and bound the four completion surfaces to it. | `sprint-change-proposal-2026-07-28.md` |
| `epic-6-authority-2026-07-28-v5` | 2026-07-28 | Tiered the conformance oracle, added Story 6.9, and amended the acceptance of Stories 6.3 and 6.6 and the binding dependency order. | `sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` |
| `epic-6-authority-2026-07-31-v6` | 2026-07-31 | Amended Story 6.2's AC1 SM-C2 pass rule, republished four record-contract improvements that had been edited into v4 out of process, corrected Story 6.2's completion-process disposition, and added Story 6.11. | `sprint-change-proposal-2026-07-31-sm-c2-threshold-and-v4-restoration.md` |

### Story 6.2 SM-C2 Pass-Rule Amendment

AC1's frozen inventory, identical envelope, paired-measurement requirement, and
raw-sample retention are unchanged. Only the pass rule changes, and only where
it provably cannot decide. The measured basis, taken at Story 6.2's candidate
with the baseline reconstructed at preserved source commit `29def44` under the
byte-identical fixture:

| Row | Baseline p95 (us) | Post p95 (us) | Change | Published rule |
| --- | ---: | ---: | ---: | --- |
| HP-CREATE | 1.7566 | 0.8759 | -50.1% | pass |
| HP-APPEND | 18.8690 | 22.6453 | +20.0% | fail |
| HP-LIST | 746.9148 | 3210.6471 | +329.9% | fail |
| HP-OPEN | 22.0662 | 410.6236 | +1760.9% | fail |

From this amendment forward, for Story 6.2, a row is gated at
`post P95 <= 1.05 x baseline P95` only when **both** conditions hold: the row's
cost change is not attributable to an approved correctness change, and the row
carries usable signal at that threshold.

1. **HP-LIST and HP-OPEN** fail the first condition. Their cost is the price of
   the fail-closed cross-key validation AC6 makes mandatory: a detail read that
   was one store read is now that read plus a full tenant-index read plus a
   dispatch-ledger read, and each list page additionally bulk-reads one detail
   record and one ledger record per returned row. The published rule compares a
   fast incorrect read path against a slower correct one and reports the
   correctness as a regression. For these two rows the +-5% rule is replaced by
   an **approved-cost ceiling** — the measured post p95 plus 10% headroom,
   recorded numerically in the release artifact. The approved factor does not
   block; exceeding the ceiling does, so a later change that makes these paths
   slower still goes red.
2. **HP-CREATE and HP-APPEND** fail the second condition. HP-APPEND spans
   3.797750 to 26.665300 microseconds within a single 30-sample run, and across
   two rounds on byte-identical code HP-APPEND flipped pass to fail while
   HP-CREATE flipped the other way, HP-CREATE's baseline p95 alone moving by a
   factor of 2.3. A +-5% threshold cannot adjudicate a statistic whose own
   dispersion is two orders of magnitude wider. These two rows are recorded as
   measured, disclosed, and not gated in Story 6.2.
3. The disclosure is **mandatory** and belongs in the artifact a reader relies
   on, not only in a test comment: which rows are gated under which rule, why,
   and that a `pass` on an ungated row may not be cited as evidence of no
   regression.
4. Story 6.6 re-measures under this same amended rule when it issues the
   superseding attestation. The ceiling values are Story 6.2's measurement, not
   a permanent performance budget.

This amendment does **not** relax AC6, and it does not authorize repairing
cross-key inconsistency on read in exchange for speed. It records a cost the
epic's own correctness requirement produced.

### Republished Record-Contract Improvements

The v4 amendment block was edited in place on 2026-07-29 by commit `1b7a06b`,
after v5 had declared v4 immutable, with no amendment and no disclosure. The v4
bytes are restored to their published state. The four substantive improvements
in that edit are genuine and are republished here; they take effect from this
amendment forward and amend, not rewrite, the v4 contract:

1. **Derivation sources.** The second source is the git-derived path set between
   the work baseline and the committed candidate, **with source-tree dirt
   blocked outside record outputs and declared TRX inputs** — replacing the
   union with the tracked working-tree delta. A record must describe a committed
   tree, not a tree plus whatever else was open at the time.
2. **Invariant 2.** The root `.slnx` defines the required root-owned test
   projects; failures block, and skips require exact versioned
   identity-and-reason policy.
3. **Invariant 4.** After the bound candidate, only record-output paths may
   change and no gitlink may move, so a superseded binding goes red rather than
   stale.
4. **Invariant 5.** The four completion surfaces generate one
   document-and-Markdown bundle rather than author it, verify the inserted
   Markdown digest, and let generator blockers block `review` and `done` exactly
   as the promotion gate does.

Story 6.8 is `in-progress` and quotes this contract verbatim in its acceptance
criteria; its quotes bind to v6 for these four items and to v4 as published for
everything else.

### Story 6.2 Completion-Process Disposition Correction

The v4 disposition stated that Story 6.2 "completes under the pre-6.8 process
and is afterwards verified read-only in historical mode". That is superseded:
Story 6.2 completes on the generated-record path, because the mandatory
`dev-story` completion surface requires the Final Record Generation Gate and
forbids hand-authoring any count, path, or commit. The pre-6.8 process is not
available to a run of the surface that owns completion. The historical-mode
verification remains required afterwards.

### Story Dispositions Amended By This Overlay

| Story | v6 disposition |
| --- | --- |
| 6.2 | AC1's pass rule is amended as above; AC2-AC7 unchanged. Completes on the generated-record path. |
| 6.6 | Re-measures SM-C2 under the amended rule and consumes the recorded ceilings; otherwise unchanged. |
| 6.8 | Quotes the four republished record-contract items from v6; otherwise unchanged. |
| 6.11 | New. Owns making cross-key projection validation cheap enough to re-gate HP-LIST and HP-OPEN at +-5%, at which point the approved-cost ceiling is retired. |

Every other story disposition and the preservation denominators are unchanged.
This amendment introduces one new story identifier and one sprint-status entry.

### Binding Dependency Order

The v5 spine is preserved exactly:
`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`, with `6.9 -> 6.3`
and `6.9 -> 6.6` as parallel constraints. The SM-C2 baseline remains a pre-change
gate for 6.2, now under the amended pass rule, and Story 6.6 remains last.

Story 6.11 is a parallel constraint, not a new link in the spine: it may start
once 6.2 records the approved-cost ceiling and must complete before Story 6.6
issues the superseding attestation if that attestation is to reinstate the +-5%
rule for HP-LIST and HP-OPEN. If 6.11 has not landed, Story 6.6 re-measures
against the recorded ceiling and says so. Story 6.2 does not wait for 6.11;
serializing them would block the epic on work whose success is not assured,
which is the outcome this amendment exists to avoid.

The frozen FR-20 denominator is unchanged.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:END version=epic-6-authority-2026-07-31-v6 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:BEGIN version=epic-6-authority-2026-08-01-v7 supersedes=epic-6-authority-2026-07-31-v6 -->

## Appendix: 2026-08-01 Projection-Proof Evidence-Lifecycle Authority Amendment

**Overlay version:** `epic-6-authority-2026-08-01-v7`
**Architecture authority:** `conversations-architecture-2026-08-01-v7`
**Supersedes:** `epic-6-authority-2026-07-31-v6` only by separating immutable
candidate-bound projection proof from current release assurance, adding Story
6.12, amending the acceptance of Stories 6.3 and 6.6, and adding one dependency
constraint
**Status:** active corrective amendment approved by Jerome on 2026-08-01; the
v1-v6 overlays, completed history, Story 6.2 evidence, and signed evidence
remain immutable historical records.

### Overlay Amendment Log — Continuation

The continuation remains outside every earlier immutable block. Later
amendments append a new continuation and never edit this table in place.

| Overlay version | Date | Amendment | Authority |
| --- | --- | --- | --- |
| `epic-6-authority-2026-07-27-v3` | 2026-07-27 | Corrected Story 6.2's AppHost acceptance: the module test-AppHost is retained as a non-shipping harness rather than removed. | `sprint-change-proposal-2026-07-27.md` |
| `epic-6-authority-2026-07-28-v4` | 2026-07-28 | Made the final story record a mechanical derivation from measured state, added Story 6.8, and bound the four completion surfaces to it. | `sprint-change-proposal-2026-07-28.md` |
| `epic-6-authority-2026-07-28-v5` | 2026-07-28 | Tiered the conformance oracle, added Story 6.9, and amended the acceptance of Stories 6.3 and 6.6 and the binding dependency order. | `sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` |
| `epic-6-authority-2026-07-31-v6` | 2026-07-31 | Amended Story 6.2's AC1 SM-C2 pass rule, republished the record contract, corrected Story 6.2's completion-process disposition, and added Story 6.11. | `sprint-change-proposal-2026-07-31-sm-c2-threshold-and-v4-restoration.md` |
| `epic-6-authority-2026-08-01-v7` | 2026-08-01 | Separated immutable candidate-bound projection proof from current readiness, added Story 6.12, and amended Stories 6.3 and 6.6. | `sprint-change-proposal-2026-08-01.md` |

### Projection-Proof Evidence Lifecycle

Story 6.2's `projection-read-store-population-proof-v2` is immutable
point-in-time evidence for umbrella candidate
`856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` and EventStore gitlink
`e645901928eed9759e28e1086f23dc96875c3ac3`. Its recorded platform binding for
`EventStoreDomainServiceExtensions.cs` is SHA-256
`a297324ab709ce3fbc744a47640c326ebca13001ed4d479132f74154b0f334b1`,
which is the blob at that recorded gitlink. Historical validation resolves
root-owned blobs from the recorded umbrella candidate and platform-owned blobs
from its recorded submodule commits. It never substitutes current `HEAD` or a
current submodule worktree.

The rule is deliberately two-part:

1. **Historical truth.** Every completed proof remains byte-identical and is
   validated at the candidate and dependency identities it declares. Later
   approved work does not retroactively falsify that proof.
2. **Current assurance.** Exactly one approved successor-chain head represents
   the current release candidate. Each successor links its predecessor by full
   artifact hashes, names the changed dependency identities and approving
   owner/rationale, and carries fresh machine-readable production-boundary
   evidence. In-scope drift without a successor fails with stable code
   `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; unrelated root gitlink movement
   cannot invalidate historical proof.

No completed Story 6.2 record or v2 evidence byte is rewritten. A backlog entry
or this amendment alone is not current projection proof; Story 6.12 must execute
the boundary lanes and publish the additive successor before Stories 6.3 or 6.6
may complete.

### Story 6.12: Version projection proofs without rewriting completed history

As a release owner,
I want completed projection proofs validated at their recorded candidate and
current readiness represented by an explicit successor chain,
so that later approved platform work neither falsifies history nor inherits
stale assurance.

**Acceptance criteria.**

1. Story 6.2 remains `done` and its story record, v2 JSON/Markdown, three bound
   xUnit results, generated final record, and immutable signed-v1 dependencies
   remain byte-identical. The v2 validator reads root-owned blobs from umbrella
   candidate `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` and platform-owned
   blobs from the root gitlinks recorded in that candidate; it proves every
   recorded hash, mode, gate result, and run binding at that time basis.
2. Historical validation no longer compares v2's recorded commit or hashes to
   the current worktree, and it does not prohibit later unrelated root gitlink
   or production-source movement. It remains strict against mutation or
   unresolvable recorded Git objects.
3. ADR 0004 defines an immutable predecessor-linked projection-proof lifecycle:
   full predecessor artifact hashes, exactly one approved current head, exact
   changed dependency identities, named owner and rationale, and no in-place
   evidence mutation.
4. `projection-read-store-population-proof-v3` is generated against the current
   candidate. It reruns deterministic dispatch, gateway/DAPR boundary,
   configured state-store end-state, production queries, derived-state
   deletion, and full-replay evidence; binds current in-scope source/test blobs
   and the EventStore gitlink; and links to the unchanged v2 hashes.
5. The current-readiness guard follows the approved chain head and compares
   only declared projection-proof dependencies. In-scope drift without a
   successor fails with `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; unrelated
   root gitlink movement does not invalidate historical proof.
6. Fault injection rejects a changed v2 byte, wrong historical
   candidate/gitlink/blob, broken predecessor hash, duplicate or forked chain
   head, stale v3 binding, missing/red/skipped/vacuous run, and undeclared
   in-scope drift, with byte-identical restoration after every mutation.
7. Story 6.3 binds v2 as historical evidence and v3 as the current chain head.
   Story 6.6 consumes both, reruns v3's functional gates, and cannot cite v2
   alone for current readiness.
8. The focused projection-proof class, Story 6.3 manifest validation class, and
   full Conformance project pass with zero failed, skipped, or not-run tests;
   Story 6.12's completion record is generated through Story 6.8.

**Prohibitions.** Story 6.12 does not modify production source, public
contracts, package versions, accepted baselines, Story 6.2's record or v2 proof
artifacts, signed-v1 evidence, or submodule content. It does not make a backlog
disposition stand in for executed current proof and does not weaken or delete a
projection assertion to make the suite green.

### Story 6.3 Amended Acceptance

In addition to its v2, v5, and v6 acceptance, the preservation traceability
manifest distinguishes immutable candidate-bound projection history from
current release assurance. It binds the complete predecessor chain, identifies
exactly one approved current proof head, records v2 as historical and v3 as
current, and fails if historical evidence is represented as proof for a later
candidate. Story 6.3 remains `in-progress` until Story 6.12 and this gate pass.

### Story 6.6 Amended Evidence

In addition to all prior obligations, the superseding attestation validates the
unchanged v2 predecessor at its recorded candidate, consumes the latest approved
projection-proof chain head, reruns that head's functional evidence, and reports
the head identity. It cannot cite v2 alone for current readiness and cannot
rewrite any predecessor to align it with the release candidate.

### Story Dispositions Amended By This Overlay

| Story | v7 disposition |
| --- | --- |
| 6.2 | No status, acceptance, record, or evidence change. Its v2 proof is immutable candidate-bound history. |
| 6.3 | Completion now requires Story 6.12 and exactly one approved current projection-proof chain head. |
| 6.6 | Validates the immutable predecessor chain and reruns the latest approved head; v2 alone is insufficient for current readiness. |
| 6.12 | New. Owns proof-lifecycle ADR 0004, candidate-aware historical validation, and the additive v3 successor proof. |

Every other story disposition, preservation denominator, ownership decision,
projection-correctness rule, and SM-C2 rule is unchanged. This amendment
introduces one new story identifier and one sprint-status entry.

### Binding Dependency Order

The v6 spine is preserved:
`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`, with
`6.9 -> 6.3` and `6.9 -> 6.6` as existing parallel constraints.

This amendment adds `6.8 -> 6.12 -> 6.3 completion` and `6.12 -> 6.6`.
Story 6.3 implementation may remain in progress while Story 6.12 executes, but
it cannot return to review or done before the successor proof and manifest
bindings pass. Story 6.6 remains last. Stories 6.4, 6.5, 6.9, 6.10, and 6.11
retain their approved scope and ordering.

The frozen FR-20 denominator is unchanged.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:END version=epic-6-authority-2026-08-01-v7 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN version=epic-6-authority-2026-08-01-v8 supersedes=epic-6-authority-2026-08-01-v7 -->

## Appendix: 2026-08-01 Implementation Readiness Authority Correction

**Overlay version:** `epic-6-authority-2026-08-01-v8`
**Architecture authority:** `conversations-architecture-2026-08-01-v8`
**Supersedes:** `epic-6-authority-2026-08-01-v7` only by publishing one complete
current Epic 6 execution contract, restoring PRD SM-C2 authority, repairing UX
planning provenance, and imposing the global implementation hold
**Correction authority:**
`sprint-change-proposal-2026-08-01-implementation-readiness-authority-correction.md`,
approved by Jerome on 2026-08-01
**Supporting provenance:**
`sprint-change-proposal-2026-08-01-stories-6-10-6-11-authority.md`; its
unapplied publication plan is superseded by this single comprehensive v8
**Status:** `AUTHORITY CORRECTION ONLY — NOT READY`

No remaining Epic 6 implementation work may start or resume until this v8 set
is published, mechanical authority validation passes, and a new independent
implementation-readiness assessment returns `READY`. A story's file-lifecycle
status is not permission to work while this hold is active.

Every v1-v7 byte above remains immutable historical authority. Completed Epics
1-5, completed Stories 6.1, 6.2, and 6.7, their records, retrospectives,
accepted baselines, signed evidence, and completed-state evidence are not
reopened, re-evaluated, or rewritten by this amendment. The definitions below
are the single complete current execution view; completed criteria are
read-only historical facts, while active and backlog criteria remain future
completion gates.

### Overlay Amendment Log — Continuation

This continuation is appended outside every earlier immutable block. It does
not edit any earlier log table.

| Overlay version | Date | Amendment | Authority |
| --- | --- | --- | --- |
| `epic-6-authority-2026-07-27-v3` | 2026-07-27 | Corrected the module AppHost to a non-shipping test harness. | `sprint-change-proposal-2026-07-27.md` |
| `epic-6-authority-2026-07-28-v4` | 2026-07-28 | Added mechanical final-record generation and Story 6.8. | `sprint-change-proposal-2026-07-28.md` |
| `epic-6-authority-2026-07-28-v5` | 2026-07-28 | Tiered the conformance oracle and added Story 6.9. | `sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` |
| `epic-6-authority-2026-07-31-v6` | 2026-07-31 | Recorded Story 6.2's historical SM-C2 disposition, restored the record contract, and reserved Story 6.11. | `sprint-change-proposal-2026-07-31-sm-c2-threshold-and-v4-restoration.md` |
| `epic-6-authority-2026-08-01-v7` | 2026-08-01 | Added the projection-proof successor lifecycle and Story 6.12. | `sprint-change-proposal-2026-08-01.md` |
| `epic-6-authority-2026-08-01-v8` | 2026-08-01 | Published the complete current story set, restored universal SM-C2 authority, repaired UX planning authority, and imposed the readiness hold. | `sprint-change-proposal-2026-08-01-implementation-readiness-authority-correction.md` |

### Current Metric Authority

The finalized PRD SM-C2/OQ-5 rule is the sole current performance authority:

`post P95 <= 1.05 x baseline P95`

It applies to all four frozen rows—HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN—
under the identical reproducible envelope. The v6 ceiling and disclosure model
is preserved only as immutable context explaining how completed Story 6.2
reached `done`; it is not a current Story 6.6 pass option. Correctness cost,
owner acceptance, a disclosed miss, an approved-cost ceiling, or unusable
signal cannot substitute for the current gate. Changing the target requires a
separate approved PRD-level change proposal.

### Story 6.1: Rebaseline architecture and planning authority

**Status:** `done` — read-only completed history.

As a platform architect, I want architecture and epic authority reconciled to
the finalized PRD, so corrective implementation starts from one ownership and
decision model.

**Effective acceptance criteria (historical):**

1. Architecture distinguishes 20 initiative FRs from 104 Feature-FRs, 77
   Feature-NFRs, 52 UX decisions, and every UX acceptance criterion; preserves
   the accepted 13,289-LOC baseline; and defers only FR-16.
2. FR-10 through FR-15 have verified public platform landing zones; OQ-1
   through OQ-5 each have one resolved row; the canonical host pair is
   `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()`.
3. A nonempty versioned hot-path inventory is frozen before baseline capture
   and records the PRD rule `post P95 <= 1.05 x baseline P95` under an identical
   reproducible envelope.
4. The module owns no reusable production AppHost, Aspire, ServiceDefaults, or
   equivalent runtime capability. The retained Conversations AppHost is only a
   non-packable, non-publishable local user/E2E test harness; platform deployment
   owns production composition.
5. The append-only authority preserves completed history, the full
   preservation denominator, the promotion-completion invariant, and signed v1
   evidence byte-for-byte.

**Direct dependencies:** none. The completed record remains authoritative and
is not changed by v8.

### Story 6.2: Migrate Conversations to platform-owned hosting

**Status:** `done` — read-only completed history.

As a Conversations maintainer, I want Conversations composed through public
platform capability while retaining only a module test harness, so the domain
module contains no reusable platform-owned hosting boilerplate.

**Effective acceptance criteria (historical):**

1. The frozen SM-C2 baseline and candidate evidence were captured under the
   same versioned envelope. The v6 approved-cost/disclosure disposition is
   preserved as Story 6.2 completion context only and does not govern current
   release readiness.
2. `Hexalith.Conversations.AppHost` and its tests remain mechanically
   non-packable and non-publishable, limited to Conversations surfaces plus
   required platform dependencies, and never become production deployment
   composition.
3. Generic ServiceDefaults, Aspire, DAPR, publication, health, telemetry,
   projection/query, and subscription capability lives on approved public
   platform surfaces; Story 6.7 validated every promotion in scope.
4. The canonical named `IAsyncDomainProjectionHandler` route reuses the domain
   materializer and durably writes both tenant-scoped per-conversation and
   tenant-index read models through the shared write policy and store.
5. Immutable `projection-read-store-population-proof-v2` evidence binds the
   accepted append/replay path through production named dispatch, actual
   integration state-store end state, and production query results without
   calling the writer directly.
6. Focused integration evidence covers duplicate delivery, partial-write
   retry, tenant isolation, bounded failure, derived-state deletion, and full
   replay equivalence; DI resolution, mock calls, legacy projection output, and
   HTTP acceptance alone are insufficient.
7. Completion used the mechanical final-record path. The record, v2 proof,
   bound xUnit results, accepted baselines, and signed-v1 dependencies remain
   byte-identical.

**Direct dependencies:** completed Stories 6.1 and 6.7. No v8 work item may
reopen or re-evaluate this completed story.

### Story 6.3: Create the complete preservation traceability manifest

**Status:** `in-progress`, but paused by the global readiness hold.

As a release owner, I want a frozen, versioned preservation manifest with
complete requirement dispositions, so preservation claims are exact and
resistant to denominator drift.

**Effective acceptance criteria:**

1. The manifest covers all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs,
   52 UX decisions, every UX acceptance-criterion identifier, current controls,
   and preserved public contracts with zero gaps or duplicates.
2. Every obligation has evidence or named-owner approved non-activation with
   rationale; delivered-to-inactive and compatible changes include approval and
   compatibility evidence.
3. Source/build/test/baseline hashes, versioned mutation governance, and
   module/platform control separation are recorded and mechanically validated.
4. The manifest binds `conformance-oracle-tiering-decision-v2`, records every
   assertion's tier, and treats the portable tier's resolved-compile-surface
   test as evidence rather than author prose.
5. Projection proof is represented as an immutable predecessor chain: v2 is
   historical evidence, the Story 6.12 successor is the one approved current
   head, and historical evidence cannot stand in for a later candidate.
6. Completion binds v8, the exact UX preservation disposition identity, the
   current proof head, and a Story 6.8-generated final record at one compatible
   candidate.

**Direct dependencies:** Stories 6.9, 6.10, and 6.12 before completion; Story
6.8 governs the final record.

### Story 6.4: Repair UX provenance and preservation governance

**Status:** `backlog` and non-startable under the global readiness hold.

As a UX governance owner, I want the UX specification treated as a
preservation reference with reliable evidence mappings, so it constrains
behavior without silently authorizing UI delivery.

**Effective acceptance criteria:**

1. UX planning cites the canonical PRD and addendum and opens with a prominent
   preservation-only/non-activation banner. Historical Phase 0-3 language is
   labeled as future activation sequence, not the active Epic 6 plan.
2. The story produces exactly
   `docs/release-evidence/ux-preservation-disposition-v1.schema.json`,
   `docs/release-evidence/ux-preservation-disposition-v1.json`,
   `docs/release-evidence/ux-preservation-disposition-v1.md` as its
   deterministic projection, plus `UxPreservationDispositionValidationTest`
   in the conformance test project.
3. The JSON binds canonical source paths, versions, and hashes; inventories
   UX-DR1-52 and every UX acceptance-criterion identifier exactly once; and
   records `preserved-not-activated`, owner, rationale, evidence/control or
   explicit non-activation, historical provenance, compatibility, and
   disclosure-safety obligations for every item.
4. Historical story mappings remain labeled non-current provenance and cannot
   become implementation ownership. No inactive UX item points to a nonexistent
   current story.
5. Validation fails on missing, duplicate, unknown, unowned, unhashed,
   source-drifted, JSON/Markdown-drifted, reordered-without-regeneration, or
   activated-without-authority entries.
6. No production UI change or preserved-scope activation is authorized.

**Direct dependencies:** Story 6.1 for start and Story 6.8 for completion.

### Story 6.5: Correct the thin authoring template and reproduce SM-2

**Status:** `backlog` and non-startable under the global readiness hold.

As a domain-module author, I want a platform-hosted thin template with
reproducible authoring-cost evidence, so SM-2 measures only code a domain module
owns.

**Effective acceptance criteria:**

1. The template contains one non-packable, non-publishable module test AppHost
   for local user/E2E tests and no reusable module-owned Aspire library,
   ServiceDefaults facade, DAPR implementation, projection/query runtime,
   publication, health, telemetry, or subscription plumbing.
2. Checkpoint 6.5-A publishes corrected thin-module authoring guidance with
   ownership and prohibited-capability rules, versioned validation, and an
   explicit reviewer decision.
3. Checkpoint 6.5-B publishes a reproducible non-packable/non-publishable
   minimal fixture using live public platform APIs, with clean build/tests and
   an exact source inventory.
4. Checkpoint 6.5-C generates versioned SM-2 v2 evidence from frozen inclusion
   rules and the preserved baseline, including source paths, commands/tool
   versions, candidate identity, file/LOC evidence, confidence, and named
   acceptance.
5. The accepted 13,289-LOC SM-1 baseline remains unchanged; validators reject
   prohibited target ownership, vacuous evidence, and JSON/Markdown drift.
6. All three checkpoints pass at one compatible candidate. A checkpoint alone
   cannot complete the story.

**Direct dependencies:** Story 6.2 for start; Stories 6.8 and 6.10 before
completion.

### Story 6.6: Revalidate and issue superseding attestation

**Status:** `backlog`, last, and non-startable under the global readiness hold.

As a release owner, I want the corrected implementation independently
revalidated against the complete preservation contract, so a release decision
rests on current evidence rather than a prescribed verdict.

**Effective acceptance criteria:**

1. Every frozen row—HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN—has one usable,
   comparable candidate result and satisfies
   `post P95 <= 1.05 x baseline P95` under the identical reproducible envelope.
   The v6 ceiling/disclosure exception is not a current pass option.
2. The complete manifest passes; public contracts are equal or carry approved
   compatible-change evidence; topology, security, health, publication, admin
   composition, SM-1, reproducible SM-2, SM-3, and every preservation gate are
   evidenced.
3. The v2 attestation and supersession record preserve signed v1 evidence,
   consume accepted ADR 0003, bind the unchanged projection-proof predecessor
   plus its single approved current head, and rerun the head's functional gates.
4. Both conformance tiers run and are reported separately and summed; Story 6.8
   records for every predecessor and Story 6.10 evidence-boundary validation
   are current, non-vacuous, and green.
5. A fresh independent implementation-readiness assessment runs against the
   exact committed candidate and current authority/evidence identities. Its
   complete actual result is published unchanged; the assessor is not
   instructed or modified to return a particular verdict.
6. Release closure is a separate decision and remains blocked unless the
   preserved assessment result is `READY`. `NOT READY` or an incomplete
   assessment leaves Story 6.6 and Epic 6 open.

**Direct dependencies:** completion of Stories 6.3, 6.4, 6.5, 6.8, 6.9,
6.10, 6.11, and 6.12. This story always runs last.

### Story 6.7: Mechanically block incomplete submodule promotions from completion

**Status:** `done` — read-only completed history.

As a Hexalith development-workflow maintainer, I want promotion-bearing work to
pass a mechanical submodule completion gate, so dirty submodules and uncaptured
umbrella gitlinks cannot reach `done`.

**Effective acceptance criteria (historical):**

1. Promotion-bearing work declares exact root `references/...` paths and
   availability policy; affected scope also includes gitlinks changed since the
   baseline.
2. Each affected submodule is initialized, clean including untracked files,
   satisfies its availability policy, and is represented by the exact raw
   mode-`160000` gitlink in the committed umbrella revision.
3. Stable blockers prevent review/completion; unrelated state warns without
   blocking; an empty or unevaluated scope cannot report a pass.
4. Discovery uses root `.gitmodules` only and never initializes or traverses
   nested submodules; isolated fixtures prove success, failure, displacement,
   and concurrency cases.

**Direct dependency:** completed Story 6.1. The completed record is not changed
by v8.

### Story 6.8: Generate the final story record mechanically from measured state

**Status:** `in-progress`, but paused by the global readiness hold.

As a workflow maintainer, I want final story records generated from measured
repository state, so completion facts cannot drift through hand-authored prose.

**Effective acceptance criteria:**

1. One generator emits a versioned document-and-Markdown bundle whose fields
   derive from machine-readable test results, the committed candidate path set,
   raw root gitlinks, and the embedded Story 6.7 promotion result. Counts,
   paths, and commits are not caller-authored.
2. The root `.slnx` defines required root-owned test projects; a missing, red,
   stale, skipped-without-exact-policy, or not-run result blocks. Totals are
   computed, not transcribed.
3. The file list is singular and exact. Source-tree dirt is blocked outside
   record outputs and declared TRX inputs; paths inside root submodules block
   and gitlink promotions appear only in their labeled section.
4. Candidate, test binary, submodule, and gitlink identities bind the final
   committed state. After the candidate only record-output paths may change and
   no gitlink may move.
5. All completion surfaces generate the same bundle, verify its inserted
   Markdown digest, and let blockers prevent `review` and `done`.
6. A pass requires nonempty derived scope and executed assertions; workflow
   invocation removal or displacement fails.
7. Read-only historical mode verifies closed records without mutating them or
   pretending to reconstruct an uncommitted former worktree.
8. Fault injection proves every guard can fail and restores every mutated
   fixture byte-identically.

**Direct dependency:** completed Story 6.2. Story 6.8 governs the final record
for every later completion.

### Story 6.9: Tier the conformance oracle and make the portable tier structural

**Status:** `backlog` and non-startable under the global readiness hold.

As a test-governance owner, I want the conformance oracle split by legitimate
binding, so consumer-portable assertions stay portable without weakening
module-internal checks.

**Effective acceptance criteria:**

1. Every conformance file binding a Server namespace is triaged into a
   versioned record: re-expressed against public Contracts, Client, or Testing
   surfaces at unchanged strength, or assigned to the module-internal tier with
   exact type and reason. Public contract widening is unavailable.
2. The portable tier has no non-packable module reference, proven from the
   resolved compile surface rather than project text.
3. No manifested test is removed, skipped, renamed away, or weakened; the
   executed total across both tiers is monotonic from a machine-readable
   pre-split result.
4. Reclassification of the three manifested denominator suites records named
   owner approval, rationale, and a versioned manifest update; FR-20 membership
   is unchanged.
5. A v2 disposition artifact supersedes v1 without editing v1.
6. Every tier is present in the solution and declared to the Story 6.8
   generator, so neither can be silently unrun.

**Direct dependency:** Story 6.1. Completion unlocks Story 6.10 and contributes
to Stories 6.3 and 6.6.

### Story 6.10: Consolidate the evidence-boundary validation pattern

**Status:** `backlog` and non-startable under the global readiness hold.

As a release-evidence maintainer, I want evidence validation consolidated
behind one enforced, non-shipping helper, so evidence cannot pass through
trusted declarations, incomplete diffs, unavailable history, or vacuous
assertions.

**Effective acceptance criteria:**

1. A non-packable `Hexalith.Conversations.TestSupport` project supplies
   `RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`,
   and `AssertionLedger`, references no Conversations assembly, and does not
   alter either oracle tier's membership.
2. Its Git runner has bounded execution, concurrent stdout/stderr draining,
   explicit UTF-8 decoding, `core.quotepath=false`, unavailable-history
   handling, revision/diff resolution, raw modes, and historical blob hashing.
3. Manifest integrity is recomputed: repository-relative contained paths,
   existing files, canonical lowercase SHA-256, rejected generated/build
   output, recomputed signable payload, and a nonempty assertion ledger.
   Supersession allowlists cannot cover signed evidence.
4. Changed-file validation uses exact set equality. Gitlinks are detected from
   raw mode `160000`, never substring matching.
5. Unavailable history is an explicit skip, never a pass; zero executed
   assertions fail; roots of trust remain pinned in consuming test source.
6. `_bmad/scripts/verify_evidence_boundary.py` enforces blocker codes
   `EVIDENCE_HELPER_NOT_USED`, `ADHOC_GIT_RUNNER`,
   `ADHOC_REPOSITORY_ROOT`, `ADHOC_HASH_HELPER`,
   `EVIDENCE_ARTIFACT_UNVALIDATED`, `EXEMPTION_EXPIRED`,
   `SCOPE_NOT_EVALUATED`, and `BASELINE_NOT_PROVIDED`, while retaining warnings
   `EXEMPTION_ACTIVE` and `EVIDENCE_TEST_OUTSIDE_CONFORMANCE`.
7. The gate is mandatory in the five governed workflow bodies in both active
   agent trees and both generated quick-dev render twins; mirrored bodies stay
   equivalent and dev-story definition-of-done/checklist forbid ad-hoc
   equivalents.
8. All 24 approved baseline evidence readers plus any reader added before
   implementation migrate with zero day-one exemptions, unchanged assertion
   strength, pinned constants, and preserved counts. Projection-proof adoption
   does not absorb or weaken Story 6.12.
9. The runbook documents invariants, authoring, exemptions, and limitations;
   fault injection covers hashes, escaping paths, generated evidence, gitlinks,
   subset comparison, signed allowlisting, unavailable Git, removed workflow
   calls, and malformed authority markers.
10. Story 6.7's inherited gate-span coupling is repaired so adding the evidence
    gate cannot leave a displaced positive guard green.

**Direct dependencies:** Stories 6.8 and 6.9. Completion is required by Stories
6.3, 6.5, and 6.6. Story 6.10 is independent of Story 6.12.

### Story 6.11: Restore the universal SM-C2 gate without weakening projection correctness

**Status:** `backlog` and non-startable under the global readiness hold.

As a release owner, I want all frozen hot paths to have usable comparable
signal and remain within the PRD regression budget, so current readiness uses
one performance rule without weakening fail-closed behavior.

**Effective acceptance criteria:**

1. Before production implementation, an ADR defines per-conversation
   index-entry key families, derived-state ownership, write ordering,
   compatibility transition, rebuild/backfill, deletion, expiry, and rollback;
   EventStore remains the only write authority.
2. HP-LIST/HP-OPEN validation removes unnecessary full-index or per-row fan-out
   only where an explicit proof permits it. Missing, duplicate, stale,
   advanced, malformed, misfiled, pending, or inconsistent state remains fail
   closed and reads never repair durable state.
3. Tenant isolation, retries/idempotency, delayed/out-of-order delivery,
   equal-position conflict, deletion, replay, and interrupted rebuild remain
   deterministic and non-disclosing across every derived key family.
4. Public query contracts, filtering, ordering, cursors, freshness vocabulary,
   forbidden/nonexistent indistinguishability, and response shapes remain
   unchanged.
5. A versioned measurement-method decision fixes repetitions, raw-sample
   retention, warm/cold classification, environment controls, and a predeclared
   signal-quality rule for all four rows; it cannot change the PRD threshold or
   discard adverse samples after observation.
6. HP-CREATE and HP-APPEND obtain usable comparable signal under the same
   frozen envelope; missing or unusable signal fails.
7. HP-LIST and HP-OPEN use the preserved Story 6.2 baseline fixture and satisfy
   the universal gate with every correctness test green; performance work may
   not weaken or reclassify correctness.
8. Unit, integration, and real DAPR state-store lanes fault-inject partial
   writes, latency, unavailable stores, poison records, retries, concurrency,
   tenant collisions, and replay.
9. One candidate-bound additive evidence set records every baseline/candidate
   raw sample, environment fact, calculation, signal verdict, and exact
   code/test identity for all four rows; JSON is authoritative and Markdown is
   deterministic.
10. Story 6.11 reaches `done` only when every frozen row satisfies
    `post P95 <= 1.05 x baseline P95` and every correctness gate is green. Any
    miss, unusable signal, red/skip/not-run/vacuous test, or stale binding keeps
    the story incomplete and release closure blocked.

**Direct dependency:** completed Story 6.2. Story 6.11 is independent of
Stories 6.10 and 6.12 and is mandatory before Story 6.6.

### Story 6.12: Version projection proofs without rewriting completed history

**Status:** `ready-for-dev`, but non-startable under the global readiness hold
and its existing Story 6.8 entry gate.

As a release owner, I want completed projection proofs validated at their
recorded candidate and current readiness represented by an explicit successor
chain, so approved later work neither falsifies history nor inherits stale
assurance.

**Effective acceptance criteria:**

1. Story 6.2 remains `done`; its record, v2 JSON/Markdown, three bound xUnit
   results, generated final record, and signed-v1 dependencies remain
   byte-identical. Historical validation reads root and submodule blobs from
   the recorded candidate/gitlinks and proves every bound hash, mode, gate, and
   run identity at that time basis.
2. Historical validation does not compare v2 to the current worktree or forbid
   later unrelated movement; mutation or unresolvable recorded Git objects
   still fail.
3. ADR 0004 defines an immutable predecessor-linked lifecycle with full
   predecessor hashes, exactly one approved current head, exact changed
   dependencies, named owner/rationale, and no in-place evidence mutation.
4. Generated `projection-read-store-population-proof-v3` reruns deterministic
   dispatch, gateway/DAPR, configured state-store, production query, deletion,
   and replay evidence against the current candidate and links unchanged v2.
5. The current guard compares only declared proof dependencies; undeclared
   in-scope drift fails `PROJECTION_PROOF_SUPERSESSION_REQUIRED`, while
   unrelated gitlink movement does not invalidate history.
6. Fault injection rejects changed v2 bytes, wrong historical identities,
   broken predecessor hashes, duplicate/forked heads, stale v3, missing/red/
   skipped/vacuous runs, and undeclared drift, restoring fixtures exactly.
7. Story 6.3 binds v2 as history and v3 as current; Story 6.6 consumes both and
   reruns v3. V2 alone cannot prove current readiness.
8. Focused proof, manifest, and full Conformance lanes pass without failed,
   skipped, or not-run tests; Story 6.8 generates the final record.

**Internal checkpoints:**

| Checkpoint | Criteria | Review and rollback boundary |
| --- | --- | --- |
| 6.12-A Historical validity and lifecycle contract | AC1-AC3 | Protected-byte inventory, candidate-aware historical validation, ADR 0004, and a closed successor-chain schema; no v3 current-head claim. |
| 6.12-B Successor generation and current guard | AC4-AC5 | Deterministic v3 projection, fresh functional lanes, exact approval, one current head, and drift guard; may be discarded without changing v2 history. |
| 6.12-C Fault injection, manifest handoff, and closure | AC6-AC8 | Mutation matrix, Story 6.3/6.6 handoff, full conformance, and Story 6.8-generated final record. |

Checkpoint success does not advance the story to `done`; all eight criteria
must pass at one compatible final candidate.

**Direct dependency:** Story 6.8. Story 6.12 is independent of Stories 6.10
and 6.11 and precedes completion of Stories 6.3 and 6.6.

### Current Story Dispositions

| Story | Status | Current authority disposition |
| --- | --- | --- |
| 6.1 | done | Completed history; preserve record and evidence unchanged. |
| 6.2 | done | Completed history; preserve record, historical SM-C2 disposition, and evidence unchanged. |
| 6.3 | in-progress | Paused; resume only after readiness `READY`; 6.9, 6.10, and 6.12 gate completion. |
| 6.4 | backlog | Preservation-governance work; no product UI activation; 6.8 gates completion. |
| 6.5 | backlog | Three ordered checkpoints; 6.2 gates start and 6.8/6.10 gate completion. |
| 6.6 | backlog | Last; preserves independent assessment result and cannot use the v6 SM-C2 exception. |
| 6.7 | done | Completed history; preserve record and evidence unchanged. |
| 6.8 | in-progress | Paused; mechanical final-record owner for every later completion. |
| 6.9 | backlog | Oracle-tiering authority; gates 6.10 and contributes to 6.3/6.6. |
| 6.10 | backlog | Evidence-boundary helper; independent of 6.12; gates 6.3/6.5/6.6. |
| 6.11 | backlog | Universal four-row SM-C2 restoration; mandatory before 6.6. |
| 6.12 | ready-for-dev | Non-startable until readiness `READY` and 6.8 is done; gates 6.3/6.6. |

### Topological Dependency Plan

| Gate or wave | Work | Entry condition | Completion unlocks |
| --- | --- | --- | --- |
| Authority Gate | Publish and validate comprehensive v8; then rerun readiness separately | Approved comprehensive correction | Remaining work only if the independent result is `READY` |
| Completed spine | 6.1 -> 6.7 -> 6.2 | Immutable historical fact | Existing prerequisites satisfied |
| Wave 1 | Resume 6.8; execute 6.4, 6.5-A/B, 6.9, and 6.11 | Readiness `READY` plus local prerequisites | 6.8/6.9 unlock 6.10; 6.8 unlocks 6.12 |
| Wave 2 | 6.10 and 6.12 in parallel; finish 6.5-C when its gates permit | Direct predecessors done | Completion paths for 6.3/6.5/6.6 |
| Wave 3 | Complete 6.3, 6.4, and 6.5 | Exact dependencies and evidence pass | Capstone eligibility |
| Wave 4 | 6.6 only | Every predecessor done and universal SM-C2 green | Independent assessment and possible Epic 6 closure |

Direct dependency edges:

```text
6.1 -> 6.7
6.7 -> 6.2
6.2 -> 6.8
6.1 -> 6.4
6.2 -> 6.5
6.8 -> completion of 6.4
6.8 -> completion of 6.5
6.1 -> 6.9
6.8 -> 6.10
6.9 -> 6.10
6.8 -> 6.12
6.9 -> completion of 6.3
6.10 -> completion of 6.3
6.12 -> completion of 6.3
6.10 -> completion of 6.5
6.2 -> 6.11
6.3 -> 6.6
6.4 -> 6.6
6.5 -> 6.6
6.8 -> 6.6
6.9 -> 6.6
6.10 -> 6.6
6.11 -> 6.6
6.12 -> 6.6
```

The graph is acyclic. Stories 6.10, 6.11, and 6.12 are mutually independent
after their stated predecessors, although each must preserve compatible edits
on shared validation surfaces.

### High-Risk BDD Scenario Catalogue

```gherkin
Scenario: Cross-tenant derived key is presented during an authorized read
  Given tenant A is authorized and an otherwise valid record is stored under tenant B's key
  When the list or detail query validates the derived state
  Then the query fails closed without disclosing tenant B existence or content

Scenario: Evidence content changes after candidate binding
  Given a generated evidence artifact is bound by path, mode, hash, candidate, and test binary
  When any bound byte or identity changes
  Then validation fails with a stable blocker and no stale evidence is reused

Scenario: Historical proof is valid but the current dependency set drifted
  Given v2 validates at its recorded candidate and an approved current head exists
  When an in-scope current dependency changes without an approved successor
  Then current readiness fails with PROJECTION_PROOF_SUPERSESSION_REQUIRED
  And historical v2 validity remains unchanged

Scenario: A required test is skipped or the assertion ledger is empty
  Given an evidence lane is required by a story completion gate
  When the result is skipped, not run, missing, stale, or records zero executed assertions
  Then the gate fails and cannot be reported as not-applicable or passing

Scenario: A frozen SM-C2 row has unusable signal or exceeds the threshold
  Given its baseline and candidate use the frozen identical envelope
  When signal quality is unusable or post P95 exceeds 1.05 times baseline P95
  Then Story 6.11 remains incomplete and Story 6.6 cannot close

Scenario: Readiness returns NOT READY
  Given Story 6.6 executes an independent assessment and preserves the complete report
  When the result is NOT READY
  Then the report remains unchanged and release closure stays blocked
```

### UX Preservation Planning Contract

`ux-design-specification.md` and `ux-requirement-map.md` are preservation-only
planning inputs. Their current rows cannot activate product UI or assign current
implementation ownership to historical/nonexistent stories. Story 6.4 owns the
versioned disposition deliverables and zero-gap validator; v8 publication
repairs planning provenance and inventories identifiers but does not implement
those deliverables.

### V8 Publication Boundary

This amendment changes planning authority, its deterministic projections, UX
planning provenance/mapping, sprint hold prose, and planning-authority
validation only. It does not implement Stories 6.3-6.6 or 6.8-6.12; change
production source, public contracts, packages, deployment topology, signed
evidence, accepted baselines, completed story records, or submodule content; or
run/predetermine the implementation-readiness assessment.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END version=epic-6-authority-2026-08-01-v8 -->

<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN version=epic-6-authority-2026-08-02-v9 architecture-authority=conversations-architecture-2026-08-02-v9 supersedes=epic-6-authority-2026-08-01-v8 v8-prefix-bytes=140511 v8-prefix-sha256=37b85c3e6af62f8a5968480939783aa6bbb7558bebc61f57f4ebca1c44bd1908 candidate=UNBOUND hold=ACTIVE -->

## Appendix: 2026-08-02 V9 Successor Execution Authority

**Epic authority:** `epic-6-authority-2026-08-02-v9`
**Architecture authority:** `conversations-architecture-2026-08-02-v9`
**Supersedes:** `epic-6-authority-2026-08-01-v8` only for unfinished-work
execution definitions
**Approved source:**
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`
**Adopted specification companion:**
`_bmad-output/specs/spec-Conversations/SPEC.md`
**Planning candidate (`PC`):** `UNBOUND`
**Global implementation hold:** `ACTIVE`
**Publication status:** `planning-authority-only; implementation-prohibited`

This is the sole canonical v9 epic-and-story authority block. The exact
140,511-byte prefix ending at the v8 end marker has SHA-256
`37b85c3e6af62f8a5968480939783aa6bbb7558bebc61f57f4ebca1c44bd1908` and
is immutable v1-v8 history. A validator must hash the prefix bytes, not a
normalized rendering. Any mismatch is `V8_PREFIX_DRIFT` and blocks publication.

### Confirmed Requirement Extraction

The approved v9 correction changes execution decomposition only. It preserves
without reinterpretation:

- exactly 20 initiative FRs and 104 `Feature-FR`s, hence 124/124 functional
  requirements, with FR-16 the sole deferred and non-activated initiative FR;
- all 77 `Feature-NFR`s;
- all 52 UX decisions and all 28 UX acceptance identifiers at
  `preserved-not-activated`;
- Epics 1-5 and completed Stories 6.1, 6.2, and 6.7, including their records,
  accepted baselines, signed evidence, and submodule bindings; and
- every v8 technical invariant, prohibition, performance rule, privacy and
  tenant boundary, evidence rule, and completed-history protection.

The unfinished v8 execution inventory contains Stories 6.3, 6.4, 6.5, 6.6,
6.8, 6.9, 6.10, 6.11, and 6.12; 66 effective acceptance criteria; checkpoints
6.5-A through 6.5-C and 6.12-A through 6.12-C; the unfinished-work dependency
edges in the v8 topological plan; the six high-risk v8 BDD scenarios; and every
embedded prohibition, evidence obligation, rollback condition, and completion
gate. The obligation ledger later in this block is the zero-gap disposition of
that frozen inventory. A story-row-only mapping is invalid.

V9 publishes Epic 6 as the immutable historical corrective foundation and
publishes outcome Epics 7-15 with exactly 27 topologically ordered successor
stories. Each successor owns one bounded outcome, one rollback boundary, one
generated final record, exact predecessors, atomic acceptance scenarios,
candidate-bound inputs and outputs, frozen inventories and digests, exact
commands and result semantics, stable blocker codes, and named fault injection.

### Non-Goals And Active Hold

This block does not implement a successor story, bind `PC`, accept partial v8
work, change product or UX scope, rewrite evidence, mutate a completed record,
move a gitlink, change runtime code, or authorize release closure. The effective
hold is `ACTIVE` when its record is missing, invalid, candidate-mismatched, or
not backed by validator `PASS`, independent candidate-matched IR-0 `READY`, and
an explicit release-owner `LIFTED` decision. Normal story statuses, partial
implementation, reviewer opinion, or generated projections cannot lift it.

**Workflow publication log:** requirement extraction was confirmed by Jerome
on 2026-08-02. Epic and story design follow in this same append-only block.

### V9 Epic List And Outcome Coverage

V9 adds no functional requirement. The v1-v8 FR coverage map remains immutable
and authoritative for all 124 functional requirements. The successor epics
dispose unfinished execution obligations and produce current preservation
evidence; they do not remap, renumber, activate, or narrow a product requirement.

#### Epic 6: Immutable Historical Corrective Foundation

Maintainers retain the accepted platform-hosting and promotion-control
foundation without reopening completed work. Epics 1-5 are its historical
entry; its bounded exit is the immutable `done` state of Stories 6.1, 6.2, and
6.7. Unfinished v8 definitions remain provenance only.

#### Epic 7: Reliable Mechanical Completion Records

Developers receive deterministic candidate-bound completion records that
cannot pass from hand-authored facts, vacuous scope, stale tests, displaced
workflow calls, or dirty/moved submodules. Story 6.2 is the hard entry; Stories
7.1-7.4 done are the bounded exit.

#### Epic 8: Preserved UX Governance

Product and release owners receive deterministic zero-gap dispositions for all
preserved UX obligations without activating UI scope. Epic 7 is the hard entry;
Stories 8.1-8.2 done are the bounded exit.

#### Epic 9: Portable Conformance Oracle

Domain authors receive an objectively tiered oracle whose consumer-portable
surface is structural and whose complete execution remains monotonic. Epic 7
is the hard entry; Stories 9.1-9.2 done are the bounded exit.

#### Epic 10: Unified Evidence Boundary

Reviewers receive one hardened non-shipping evidence-integrity boundary across
every governed workflow and frozen reader. Epics 7 and 9 are hard entries;
Stories 10.1-10.4 done are the bounded exit.

#### Epic 11: Thin-Module Authoring Proof

Domain authors receive corrected platform-hosted guidance, a minimal live
fixture, and reproducible authoring-cost evidence. Story 6.2 and Epics 7 and 10
are hard entries; Stories 11.1-11.3 done are the bounded exit.

#### Epic 12: Universal Performance Restoration

Operators receive correctness-preserving projection performance under the one
universal SM-C2 rule for all four frozen hot paths. Story 6.2 is the hard entry;
Stories 12.1-12.4 done are the bounded exit.

#### Epic 13: Current Projection-Proof Lifecycle

Release owners receive immutable historical proof validation and one
predecessor-bound current assurance head without rewriting accepted evidence.
Epics 7 and 9 are hard entries; Stories 13.1-13.3 done are the bounded exit.

#### Epic 14: Complete Preservation Manifest

Release owners receive a zero-gap, candidate-bound manifest across every
requirement, public contract, test, UX obligation, control, and evidence chain.
Epics 8, 9, 10, and 13 are hard entries; Stories 14.1-14.3 done are the bounded
exit.

#### Epic 15: Superseding Release Attestation

Release owners receive bounded revalidation evidence and a signable
superseding attestation without predetermining either independent assessment or
release closure. Epics 7-14 are hard entries; Stories 15.1-15.2 done and a
recorded RG-15 decision are the bounded exit.

### V8-To-V9 Outcome Coverage Map

| Frozen v8 execution unit | V9 outcome owner | Disposition |
| --- | --- | --- |
| Stories 6.1, 6.2, and 6.7 | Epic 6 | Immutable completed foundation; no successor execution. |
| Story 6.8 | Epic 7 | Superseded; partial work is unaccepted input to Stories 7.1-7.4. |
| Story 6.4 | Epic 8 | Superseded by Stories 8.1-8.2; UX remains non-activated. |
| Story 6.9 | Epic 9 | Superseded by Stories 9.1-9.2. |
| Story 6.10 | Epic 10 | Superseded by Stories 10.1-10.4. |
| Story 6.5 | Epic 11 | Superseded by Stories 11.1-11.3. |
| Story 6.11 | Epic 12 | Superseded by Stories 12.1-12.4. |
| Story 6.12 | Epic 13 | Superseded; prepared story remains provenance for Stories 13.1-13.3. |
| Story 6.3 | Epic 14 | Superseded; partial work is unaccepted input to Stories 14.1-14.3. |
| Story 6.6 | Epic 15 and Gate RG-15 | Executable attestation work moves to Stories 15.1-15.2; independent release decision is not a story. |

Every obligation in an unfinished row maps exactly once in the obligation
ledger below. Cross-cutting denominators close through Epic 14 and are
revalidated by Epic 15: 124/124 functional requirements, 77 Feature-NFRs, 52
UX decisions, and 28 UX acceptance IDs. Earlier epics retain their original FR
assignments; this map is an execution-supersession map, not a replacement FR
coverage map.

### V9 Epic Dependency Graph

```text
6.2 -> 7
7 -> 8
7 -> 9
7 -> 10
9 -> 10
6.2 -> 11
7 -> 11
10 -> 11
6.2 -> 12
7 -> 13
9 -> 13
8 -> 14
9 -> 14
10 -> 14
13 -> 14
7 -> 15
8 -> 15
9 -> 15
10 -> 15
11 -> 15
12 -> 15
13 -> 15
14 -> 15
```

The graph is acyclic and lower-numbered. Each epic produces a complete
stakeholder outcome; a later epic may consume it but no epic requires a future
epic to make its own bounded exit true.

**Workflow publication log:** Jerome approved the v9 epic structure on
2026-08-02. The 27 successor story contracts follow in this block.

### Canonical Successor-Story Contract

The following contract applies to every Story 7.1-15.2 and is incorporated by
reference into every atomic scenario below.

**Candidate binding.** `PC` is the root commit recorded by
`_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`; until that file
contains a resolvable commit and matching bundle digest, every successor result
is `BLOCKED` with `PC_UNBOUND`. `SC-<story>` is `HEAD^{commit}` at final-record
generation. It binds the `PC` commit and bundle digest, the story baseline and
root commit, every input/output SHA-256, every predecessor final-record digest,
and all root-declared gitlinks. The gitlink inventory is exactly these ordinally
sorted paths, each resolved from `SC-<story>` by `git ls-tree -z` with raw mode
`160000`: `references/Hexalith.AI.Tools`, `references/Hexalith.Builds`,
`references/Hexalith.Commons`, `references/Hexalith.EventStore`,
`references/Hexalith.Folders`, `references/Hexalith.FrontComposer`,
`references/Hexalith.Memories`, `references/Hexalith.Parties`,
`references/Hexalith.Projects`, and `references/Hexalith.Tenants`. Missing,
extra, non-`160000`, unresolved, or moved bindings block. After `SC-<story>` is
frozen, only that story's declared record outputs and declared machine-result
inputs may differ; no source or gitlink may move.

**Schemas.** Exact schema identities and canonical paths are:

| Identity | Canonical path | Required top-level fields |
| --- | --- | --- |
| `hexalith.conversations.story-contract.v1` | `_bmad/schemas/v9-story-contract-v1.schema.json` | `schemaVersion`, `storyId`, `authority`, `predecessors`, `outcome`, `rollback`, `inventory`, `scenarios`, `finalRecord` |
| `hexalith.conversations.acceptance-result.v1` | `_bmad/schemas/v9-acceptance-result-v1.schema.json` | `schemaVersion`, `storyId`, `scenarioId`, `command`, `exitCode`, `result`, `blockers`, `candidate`, `inputs`, `outputs` |
| `hexalith.conversations.frozen-inventory.v1` | `_bmad/schemas/v9-frozen-inventory-v1.schema.json` | `schemaVersion`, `inventoryId`, `digestAlgorithm`, `canonicalization`, `items`, `sha256` |
| `hexalith.conversations.story-final-record.v2` | `_bmad/schemas/story-final-record-v2.schema.json` | `schemaVersion`, `storyId`, `authority`, `candidate`, `predecessors`, `inventory`, `scenarios`, `faultInjection`, `outputs`, `rollback`, `summary`, `renderedMarkdownSha256` |

Schema versions are literal `1`, `1`, `1`, and `2` respectively. Unknown
properties fail unless the owning schema explicitly lists them. Inventories are
SHA-256 over the displayed obligation IDs encoded as NFC UTF-8, one ID plus LF
per line, in displayed order. JSON is authoritative; Markdown is a deterministic
projection. Paths are repository-relative, slash-separated, and ordinally
sorted wherever order is not semantic.

**Result semantics.** V9 commands return `0` only for `PASS`, `1` for `FAIL`,
and `2` for `BLOCKED`. For direct pytest commands, exit `0` is `PASS`, exit `1`
is `FAIL`, exits `2`, `3`, or `4` are `BLOCKED` with `TEST_ENVIRONMENT_BLOCKED`,
and exit `5` is `FAIL` with `TEST_NOT_RUN`. A required scenario with a missing
result, nonzero failed/blocked/skipped/not-run count, empty assertion ledger,
schema drift, stale input, or candidate mismatch prevents final-record `PASS`.
Blocker arrays are unique and ordinally sorted. A story-specific blocker does
not replace applicable common blockers: `PC_UNBOUND`, `AUTHORITY_MISMATCH`,
`CANDIDATE_MISMATCH`, `GITLINK_SCOPE_MISMATCH`, `INPUT_DIGEST_MISMATCH`,
`OUTPUT_SCHEMA_INVALID`, `TEST_ENVIRONMENT_BLOCKED`, `TEST_NOT_RUN`,
`ASSERTION_LEDGER_EMPTY`, or `FINAL_RECORD_NOT_GENERATED`.

**Final-record rule.** Each story has exactly one authoritative JSON final
record and one deterministic Markdown rendering at the paths declared by that
story. The final generator command is part of an atomic scenario. Its summary
must equal `required=number-of-scenarios`, `passed=required`, `failed=0`,
`blocked=0`, `skipped=0`, and `notRun=0`. Hand-authored facts are forbidden.

## Epic 7: Reliable Mechanical Completion Records

**Outcome:** Developers receive deterministic, candidate-bound completion
records.
**Hard entry:** completed Story 6.2.
**Bounded exit:** Stories 7.1-7.4 are `done` at compatible candidates.
**V8 source owner:** superseded Story 6.8; partial work remains unaccepted input.

### Story 7.1: Define the final-record schema and deterministic generator core

As a workflow maintainer,
I want one closed schema and deterministic generator core,
so that no caller can author the facts used to declare completion.

**Bounded outcome:** the four schemas above and the v2 generator core produce
schema-valid authoritative JSON plus digest-bound deterministic Markdown.
**Exact predecessors:** `6.2`.
**Frozen inventory:** `V9-7.1-ENTRY-v1` contains, in order,
`V8-6.8-AC1`, `V8-6.8-AC6-ANTI-VACUITY`, and
`V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY`; SHA-256
`5fb79e8d9251c3187f2a2de7d4ae3766ab962015e628d345f8033bf14ba8e36e`.
**Candidate binding:** `SC-7.1` under the canonical candidate rule.
**Rollback boundary:** remove only new v2 schemas, generator-core changes,
Story 7.1 tests/results, and Story 7.1 final-record outputs; preserve the v1
generator, Story 6.8 provenance, all completed records, and the v1-v8 prefix.
**Generated final record:**
`docs/release-evidence/story-7.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-7.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-7.1-01` | Both v9 authorities, bound `PC`, `V9-7.1-ENTRY-v1`, and the four exact schema paths above | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/7.1/AC-7.1-01.xml` | Exit `0`; `PASS`; the schema metaschemas and required/closed fields validate. Missing or permissive fields produce exit `1`, `OUTPUT_SCHEMA_INVALID`; output is JUnit XML. |
| `AC-7.1-02` | One hermetic fixture, fixed timestamps, identical ordered inputs, schema v2, and `SC-7.1` fixture bindings | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_deterministic_bundle --junitxml=artifacts/v9/7.1/AC-7.1-02.xml` | Exit `0`; `PASS`; two JSON byte streams and two Markdown byte streams are identical and `renderedMarkdownSha256` matches. Drift is exit `1`, `RECORD_CONTENT_DRIFT`. |
| `AC-7.1-03` | A fixture attempts to pass counts, paths, commits, or verdict as caller text | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_rejects_caller_authored_facts --junitxml=artifacts/v9/7.1/AC-7.1-03.xml` | Exit `0`; the negative test proves generator exit `1`, result `FAIL`, blocker `CALLER_AUTHORED_FACT`, and no passing record. |
| `AC-7.1-04` | A valid contract yields no parsed result, resolved candidate, derived path, or executed assertion | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_rejects_empty_derivation --junitxml=artifacts/v9/7.1/AC-7.1-04.xml` | Exit `0`; the negative test proves generator exit `1`, `FAIL`, `RECORD_NOT_DERIVED` and `ASSERTION_LEDGER_EMPTY`. |
| `AC-7.1-05` | Malformed JSON, unknown schema identity, and invalid CLI arguments are separate fixtures | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_malformed_input_is_schema_valid_failure --junitxml=artifacts/v9/7.1/AC-7.1-05.xml` | Exit `0`; each fixture produces schema-valid machine output, generator exit `1`, `FAIL`, and exact blocker `INPUT_SCHEMA_INVALID` or `ARGUMENT_INVALID`; no traceback or payload is emitted. |
| `AC-7.1-06` | AC-7.1-01 through AC-7.1-05 are current, passing, nonempty, and bound to `SC-7.1` | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.1.json --format bundle --output-json docs/release-evidence/story-7.1-final-record-v2.json --output-markdown docs/release-evidence/story-7.1-final-record-v2.md` | Exit `0`; `PASS`; schema `hexalith.conversations.story-final-record.v2`; both declared outputs exist and bind `PC`, bundle digest, `SC-7.1`, all ten gitlinks, inventory digest, five predecessor scenario results, rollback boundary, and summary `6/6/0/0/0/0`. Any missing binding is exit `1` with the applicable common blocker. |

**Fault injection coverage:** caller-fact injection, empty derivation, malformed
input, unknown schema, and deterministic-render drift are mandatory and restore
their fixture directories byte-identically. `FAULT_NOT_DETECTED` or
`FIXTURE_NOT_RESTORED` blocks the final record.

### Story 7.2: Derive test, path, candidate, submodule, and gitlink facts

As a developer closing a story,
I want every completion fact measured from test artifacts and Git objects,
so that stale, dirty, or superseded state cannot be reported as final.

**Bounded outcome:** v2 derives required test totals, the singular path set,
candidate identity, and exact root-gitlink state without traversing a submodule.
**Exact predecessors:** `7.1`.
**Frozen inventory:** `V9-7.2-ENTRY-v1` contains, in order,
`V8-6.8-AC2`, `V8-6.8-AC3`, `V8-6.8-AC4`, and
`V8-6.8-PROHIBITIONS-SUBMODULE-READONLY`; SHA-256
`7a35b9cb705f8a2a7559ef8216d77c8a90c78b96c24626cf98036db550fca842`.
**Candidate binding:** `SC-7.2`, `SC-7.1` final-record digest, and the canonical
candidate rule.
**Rollback boundary:** remove only Story 7.2 fact extractors, fixtures, results,
and records; keep Story 7.1 and every historical artifact unchanged.
**Generated final record:**
`docs/release-evidence/story-7.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-7.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-7.2-01` | Root `.slnx`, one current machine result per required root-owned test project, and exact allowed-skip policy | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_derives_required_test_totals --junitxml=artifacts/v9/7.2/AC-7.2-01.xml` | Exit `0`; `PASS`; per-project and summed counts are derived, result schema is acceptance-result v1, and no caller total is accepted. |
| `AC-7.2-02` | One required project has no result artifact | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_missing_result --junitxml=artifacts/v9/7.2/AC-7.2-02.xml` | Exit `0`; the negative fixture proves generator exit `1`, `FAIL`, `TEST_RESULTS_MISSING`. |
| `AC-7.2-03` | One result predates the newest bound source input | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_stale_result --junitxml=artifacts/v9/7.2/AC-7.2-03.xml` | Exit `0`; generator exit `1`, `FAIL`, `TEST_RESULTS_STALE`; the stale count is not reused. |
| `AC-7.2-04` | One required machine result contains a failed test | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_failed_test --junitxml=artifacts/v9/7.2/AC-7.2-04.xml` | Exit `0`; generator exit `1`, `FAIL`, `TEST_FAILED`. |
| `AC-7.2-05` | One required result contains an unapproved skip | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_unapproved_skip --junitxml=artifacts/v9/7.2/AC-7.2-05.xml` | Exit `0`; generator exit `1`, `FAIL`, `TEST_SKIPPED`. |
| `AC-7.2-06` | A declared test project ran zero matching tests | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_not_run --junitxml=artifacts/v9/7.2/AC-7.2-06.xml` | Exit `0`; generator exit `1`, `FAIL`, `TEST_NOT_RUN`. |
| `AC-7.2-07` | Baseline, candidate, one exact committed path set, allowed result/record paths, and injected unrelated source dirt | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_derives_singular_file_list_and_blocks_dirt --junitxml=artifacts/v9/7.2/AC-7.2-07.xml` | Exit `0`; exact set equality passes for the clean case; injected dirt proves `SOURCE_TREE_DIRTY`; a second/different list proves `FILE_LIST_DRIFT`. |
| `AC-7.2-08` | A derived path is beneath one root `.gitmodules` path | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_submodule_internal_path --junitxml=artifacts/v9/7.2/AC-7.2-08.xml` | Exit `0`; generator exit `1`, `FAIL`, `SUBMODULE_INTERNAL_PATH`; no submodule traversal or initialization occurs. |
| `AC-7.2-09` | All ten root gitlinks plus a decoy filename containing `160000` | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_resolves_raw_gitlinks --junitxml=artifacts/v9/7.2/AC-7.2-09.xml` | Exit `0`; `PASS`; only raw mode-`160000` entries are recorded; missing/extra/mismatched entries produce `GITLINK_SCOPE_MISMATCH` or `GITLINK_DRIFT`. |
| `AC-7.2-10` | Resolvable baseline and `SC-7.2`, followed by a source commit or gitlink move | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocks_superseded_candidate --junitxml=artifacts/v9/7.2/AC-7.2-10.xml` | Exit `0`; valid ancestry passes; an invalid baseline proves `BASELINE_NOT_TRUSTWORTHY`; post-candidate movement proves `CANDIDATE_NOT_FINAL`. |
| `AC-7.2-11` | AC-7.2-01 through AC-7.2-10 and Story 7.1's record are current and candidate-compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.2.json --format bundle --output-json docs/release-evidence/story-7.2-final-record-v2.json --output-markdown docs/release-evidence/story-7.2-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-7.2`, all ten gitlinks, the Story 7.1 record, inventory digest, all ten scenario results, and summary `11/11/0/0/0/0`. |

**Fault injection coverage:** missing/stale/failed/skipped/not-run tests, dirty
paths, a submodule-internal path, missing/moved gitlink, false `160000` filename,
bad baseline, and superseded candidate are mandatory; each fixture is restored
byte-identically.

### Story 7.3: Integrate generation into every blocking completion transition

As a workflow maintainer,
I want every completion workflow to invoke and verify the same generator,
so that no review or done transition can bypass measured final records.

**Bounded outcome:** all governed workflow bodies and their generated twins use
one parity-checked invocation before lifecycle transition.
**Exact predecessors:** `7.2`.
**Frozen inventory:** `V9-7.3-ENTRY-v1` contains, in order,
`V8-6.8-AC5`, `V8-6.8-AC6-NON-DELETABILITY`, and
`V8-6.8-PROHIBITIONS-NO-CI-CLAIM`; SHA-256
`ca106f6ad40f3a2ca580358d74a1565c34611821bc1d25e51694972d46ae8ca2`.
**Candidate binding:** `SC-7.3` and final-record digests for 7.1 and 7.2.
**Rollback boundary:** remove only Story 7.3 workflow invocations, parity guard,
fixtures, results, and records as one unit; retain Stories 7.1-7.2.
**Generated final record:**
`docs/release-evidence/story-7.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-7.3-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-7.3-01` | Frozen surface inventory `bmad-dev-story/step-09`, `bmad-quick-dev/step-05-present`, `bmad-quick-dev/step-oneshot`, and `bmad-code-review/step-04-present` in `.agents` and `.claude`, plus both quick-dev render twins | `python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-01 --output artifacts/v9/7.3/AC-7.3-01.json` | Exit `0`; `PASS`; acceptance-result v1 proves every frozen body invokes the same generator before `review`/`done`. Missing invocation is `WORKFLOW_INTEGRATION_MISSING`. |
| `AC-7.3-02` | The exact command, blocker branch, halt behavior, output paths, and inserted-marker contract for every surface | `python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-02 --output artifacts/v9/7.3/AC-7.3-02.json` | Exit `0`; `PASS`; normalized bodies and render twins are equivalent. Difference is `SURFACE_PARITY_DRIFT`. |
| `AC-7.3-03` | Generated JSON, rendered Markdown, inserted Markdown, and declared digest for a fixture story | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_workflow_verifies_inserted_digest --junitxml=artifacts/v9/7.3/AC-7.3-03.xml` | Exit `0`; matching bytes pass; altered insertion proves generator exit `1`, `RECORD_CONTENT_DRIFT`. |
| `AC-7.3-04` | One workflow invocation is removed | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_removed_workflow_invocation --junitxml=artifacts/v9/7.3/AC-7.3-04.xml` | Exit `0`; verifier exit `1`, `FAIL`, `WORKFLOW_INTEGRATION_MISSING`; transition stays unchanged. |
| `AC-7.3-05` | The positive span is displaced while decoy generator text remains elsewhere | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_displaced_workflow_invocation --junitxml=artifacts/v9/7.3/AC-7.3-05.xml` | Exit `0`; verifier exit `1`, `FAIL`, `WORKFLOW_INTEGRATION_DISPLACED`; whole-file vocabulary cannot pass. |
| `AC-7.3-06` | Generator result is `FAIL` or `BLOCKED` on each completion surface | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_blocker_prevents_state_transition --junitxml=artifacts/v9/7.3/AC-7.3-06.xml` | Exit `0`; each surface preserves/returns to its pre-review state, emits the exact blockers, and does not claim CI integration. |
| `AC-7.3-07` | AC-7.3-01 through AC-7.3-06 and predecessor records are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --format bundle --output-json docs/release-evidence/story-7.3-final-record-v2.json --output-markdown docs/release-evidence/story-7.3-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-7.3`, records 7.1-7.2, inventory digest, six results, workflow body hashes, and summary `7/7/0/0/0/0`. |

**Fault injection coverage:** removed invocation, displaced positive span,
parity drift, altered inserted Markdown, and blocking generator result are
mandatory and byte-restored.

### Story 7.4: Verify historical mode and required fault-injection blockers

As a release-evidence maintainer,
I want closed records verified read-only and every generator guard proven red,
so that history remains honest and completion protection is non-vacuous.

**Bounded outcome:** historical validation proves committed facts without
rewriting history, and the complete frozen fault matrix detects every mutation.
**Exact predecessors:** `7.3`.
**Frozen inventory:** `V9-7.4-ENTRY-v1` contains, in order,
`V8-6.8-AC7`, `V8-6.8-AC8`, `V8-HR-BOUND-CONTENT-DRIFT`, and
`V8-HR-REQUIRED-TEST-SKIPPED-OR-EMPTY`; SHA-256
`4d6bb01942d41d315ad4f3b08070a3cfe8ddcc2cae1b8981fee9b64d55ebe911`.
**Candidate binding:** `SC-7.4` and final-record digests for 7.1-7.3.
**Rollback boundary:** remove only Story 7.4 historical/fault fixtures,
results, and records; retain Stories 7.1-7.3 and never mutate a closed record.
**Generated final record:**
`docs/release-evidence/story-7.4-final-record-v2.json` and deterministic
`docs/release-evidence/story-7.4-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-7.4-01` | Stories 6.1, 6.2, and 6.7 with recorded root commits, gitlinks, bound blobs, modes, gates, and run identities | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.4.json --historical --format json --output-json artifacts/v9/7.4/AC-7.4-01.json` | Exit `0`; `PASS`; acceptance-result v1 verifies committed facts read-only. Unresolvable objects are exit `1`, `HISTORICAL_BLOB_UNRESOLVED`; changed closed bytes are `HISTORICAL_RECORD_DRIFT`. |
| `AC-7.4-02` | A pre-generator record whose former working-tree state was never committed | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_historical_mode_states_worktree_limit --junitxml=artifacts/v9/7.4/AC-7.4-02.xml` | Exit `0`; `PASS`; the result states that former uncommitted state is not reconstructed and makes no false completeness claim. |
| `AC-7.4-03` | Frozen mutation IDs `COUNT`, `SUBMODULE_PATH`, `CANDIDATE`, `GITLINK`, `RESULT_MISSING`, `RESULT_STALE`, `RESULT_FAILED`, `RESULT_SKIPPED`, `RESULT_NOT_RUN`, `LEDGER_EMPTY`, `WORKFLOW_REMOVED`, `WORKFLOW_DISPLACED`, and `MARKDOWN_DIGEST` | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_complete_fault_matrix --junitxml=artifacts/v9/7.4/AC-7.4-03.xml` | Exit `0`; `PASS`; fault-injection-result entries show every mutation produced its exact Story 7.1-7.3 blocker. Undetected mutation is `FAULT_NOT_DETECTED`. |
| `AC-7.4-04` | SHA-256 captured before every mutation in AC-7.4-03 | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_fixtures_restore_byte_identically --junitxml=artifacts/v9/7.4/AC-7.4-04.xml` | Exit `0`; every before/after hash is equal. Difference is exit `1`, `FIXTURE_NOT_RESTORED`. |
| `AC-7.4-05` | A required lane is missing, skipped, not run, or records zero assertions | `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_required_lane_cannot_pass_vacuously --junitxml=artifacts/v9/7.4/AC-7.4-05.xml` | Exit `0`; generator result is `FAIL` with `TEST_RESULTS_MISSING`, `TEST_SKIPPED`, `TEST_NOT_RUN`, or `ASSERTION_LEDGER_EMPTY`; none becomes not-applicable. |
| `AC-7.4-06` | AC-7.4-01 through AC-7.4-05 and predecessor records are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.4.json --format bundle --output-json docs/release-evidence/story-7.4-final-record-v2.json --output-markdown docs/release-evidence/story-7.4-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-7.4`, records 7.1-7.3, historical results, mutation ledger, restoration hashes, inventory digest, and summary `6/6/0/0/0/0`. |

**Fault injection coverage:** the 13 named mutation IDs are the frozen minimum;
zero executed mutations, missing blocker evidence, or non-restored fixtures
blocks with `ASSERTION_LEDGER_EMPTY`, `FAULT_NOT_DETECTED`, or
`FIXTURE_NOT_RESTORED`.

## Epic 8: Preserved UX Governance

**Outcome:** Product and release owners receive deterministic zero-gap UX
dispositions without product-UI activation.
**Hard entry:** Epic 7, concretely Story 7.4.
**Bounded exit:** Stories 8.1-8.2 are `done` at compatible candidates.
**V8 source owner:** superseded Story 6.4.

### Story 8.1: Generate the versioned UX disposition contract

As a UX governance owner,
I want one versioned schema with authoritative JSON and deterministic Markdown,
so that preserved UX obligations have explicit non-activating dispositions.

**Bounded outcome:** exactly one schema/JSON/Markdown bundle projects canonical
UX sources, decisions, acceptance IDs, and historical provenance.
**Exact predecessors:** `7.4`.
**Frozen inventory:** `V9-8.1-ENTRY-v1` contains, in order,
`V8-6.4-AC1`, `V8-6.4-AC2`, `V8-6.4-AC3`, `V8-6.4-AC4`, and
`V8-6.4-AC6`; SHA-256
`6c61eb92078755496c73506419112026e3e9b7f63bb314b1028d4e9c7bb41ef9`.
**Schema:** `hexalith.conversations.ux-preservation-disposition.v1` at
`docs/release-evidence/ux-preservation-disposition-v1.schema.json`; required
top-level fields are `schemaVersion`, `authority`, `candidate`, `sources`,
`status`, `decisions`, `acceptanceCriteria`, `historicalProvenance`, and
`renderedMarkdownSha256`. Each decision/acceptance row requires `id`, `status`,
`owner`, `rationale`, `sourcePath`, `sourceSha256`, `evidenceOrControl`,
`historicalMappings`, `compatibility`, and `disclosureSafety`.
**Candidate binding:** `SC-8.1`, Story 7.4 final-record digest, and the canonical
candidate rule.
**Rollback boundary:** remove only the Story 8.1 generator, schema, results,
three disposition outputs, and final record; preserve both UX sources,
historical mappings, product/UI code, and the v1-v8 prefix.
**Generated outputs:**
`docs/release-evidence/ux-preservation-disposition-v1.json` and deterministic
`docs/release-evidence/ux-preservation-disposition-v1.md`.
**Generated final record:**
`docs/release-evidence/story-8.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-8.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-8.1-01` | Both v9 authorities, bound `PC`, `V9-8.1-ENTRY-v1`, canonical UX specification/map, and closed UX-disposition schema | `python3 _bmad/scripts/generate_ux_preservation_disposition.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --output-schema docs/release-evidence/ux-preservation-disposition-v1.schema.json --output-json docs/release-evidence/ux-preservation-disposition-v1.json --output-markdown docs/release-evidence/ux-preservation-disposition-v1.md` | Exit `0`; `PASS`; schema `hexalith.conversations.ux-preservation-disposition.v1`; all three outputs exist and Markdown digest matches. Invalid shape is `UX_SCHEMA_INVALID`; nondeterminism is `UX_RENDER_DRIFT`. |
| `AC-8.1-02` | Source inventory is exactly `_bmad-output/planning-artifacts/ux-design-specification.md` and `_bmad-output/planning-artifacts/ux-requirement-map.md` | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes -trx artifacts/v9/8.1/AC-8.1-02.trx` | Exit `0`; `PASS`; each source path, version, and current SHA-256 is bound. Missing source is `UX_SOURCE_UNBOUND`; changed source is `UX_SOURCE_DRIFT`. |
| `AC-8.1-03` | Frozen decision IDs are the closed numeric range `UX-DR1` through `UX-DR52` | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-03.trx` | Exit `0`; `PASS`; all 52 IDs occur once in source order with required row fields. Count or identity drift is `UX_DECISION_INVENTORY_DRIFT`. |
| `AC-8.1-04` | Frozen acceptance IDs are `AC-SAFE-001`-`008`, `AC-RESP-001`-`015`, `AC-A11Y-001`-`002`, `AC-LEAK-001`, `AC-MOB-001`, and `AC-PERF-001` | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-04.trx` | Exit `0`; `PASS`; all 28 IDs occur once in source order with required row fields. Drift is `UX_ACCEPTANCE_INVENTORY_DRIFT`. |
| `AC-8.1-05` | Every row, top-level status, preservation banner, and historical story mapping | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical -trx artifacts/v9/8.1/AC-8.1-05.trx` | Exit `0`; `PASS`; status is exactly `preserved-not-activated`; historical mappings are labeled non-current and cannot own implementation. Activation is `UX_ACTIVATION_UNAUTHORIZED`; current invalid ownership is `UX_CURRENT_STORY_INVALID`. |
| `AC-8.1-06` | `SC-8.1` path set and allowed outputs limited to Story 8.1 planning/tests/evidence | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange -trx artifacts/v9/8.1/AC-8.1-06.trx` | Exit `0`; `PASS`; no `src/**/*.razor`, UI CSS, product UI contract, navigation, or runtime path changed. Any such path is `UX_PRODUCTION_CHANGE_FORBIDDEN`. |
| `AC-8.1-07` | AC-8.1-01 through AC-8.1-06 and Story 7.4 record are current and candidate-compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --format bundle --output-json docs/release-evidence/story-8.1-final-record-v2.json --output-markdown docs/release-evidence/story-8.1-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-8.1`, Story 7.4, both UX source hashes, disposition output hashes, inventory digest, six scenario results, and summary `7/7/0/0/0/0`. |

**Fault injection coverage:** delete the preservation banner, change one status
to `activated`, assign one inactive item to a historical/nonexistent current
story, drift one source byte, and add one production UI path. Each mutation must
produce its exact blocker and restore every fixture byte-identically.

### Story 8.2: Enforce the 52-decision/28-acceptance zero-gap validator

As a release owner,
I want mechanical UX coverage validation,
so that missing, duplicated, drifted, or silently activated obligations cannot pass.

**Bounded outcome:** one conformance validator proves exact denominator,
identity, ownership, hash, ordering, rendering, and non-activation parity.
**Exact predecessors:** `8.1`.
**Frozen inventory:** `V9-8.2-ENTRY-v1` contains, in order,
`V8-6.4-AC5`, `V8-UX-DENOMINATOR-52-28`,
`V8-UX-NO-CURRENT-INACTIVE-STORY`, and
`V8-UX-JSON-MARKDOWN-PARITY`; SHA-256
`d07cee1556fa039169ca2cfa6cfecbd123909b9da56ea729866c7a0591fd26e6`.
**Validator:**
`Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest`.
**Candidate binding:** `SC-8.2`, Story 8.1 final-record and disposition digests,
and the canonical candidate rule.
**Rollback boundary:** remove only Story 8.2 validator/fault fixtures/results and
final record; retain accepted Story 8.1 outputs and all source UX bytes.
**Generated final record:**
`docs/release-evidence/story-8.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-8.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-8.2-01` | Story 8.1 output with frozen 52/28 inventories and bound sources | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest -trx artifacts/v9/8.2/AC-8.2-01.trx` | Exit `0`; `PASS`; exactly 52 decisions and 28 acceptance IDs, zero skipped/not-run tests, nonempty assertions. |
| `AC-8.2-02` | One frozen decision is removed | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k missing_decision --junitxml=artifacts/v9/8.2/AC-8.2-02.xml` | Exit `0`; negative fixture proves validator exit `1`, `UX_DECISION_MISSING`. |
| `AC-8.2-03` | One frozen decision is duplicated | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k duplicate_decision --junitxml=artifacts/v9/8.2/AC-8.2-03.xml` | Exit `0`; validator exit `1`, `UX_DECISION_DUPLICATE`. |
| `AC-8.2-04` | One unknown decision ID is inserted | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k unknown_decision --junitxml=artifacts/v9/8.2/AC-8.2-04.xml` | Exit `0`; validator exit `1`, `UX_DECISION_UNKNOWN`. |
| `AC-8.2-05` | Missing, duplicate, and unknown acceptance-ID fixtures are run separately | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k acceptance_identity_faults --junitxml=artifacts/v9/8.2/AC-8.2-05.xml` | Exit `0`; each fixture yields only its exact `UX_ACCEPTANCE_MISSING`, `UX_ACCEPTANCE_DUPLICATE`, or `UX_ACCEPTANCE_UNKNOWN` blocker. |
| `AC-8.2-06` | One row lacks owner and a separate row lacks source SHA-256 | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k ownership_and_hash_faults --junitxml=artifacts/v9/8.2/AC-8.2-06.xml` | Exit `0`; separate fixtures prove `UX_OWNER_MISSING` and `UX_HASH_MISSING`; neither is accepted as N/A. |
| `AC-8.2-07` | One canonical source byte changes after generation | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k source_drift --junitxml=artifacts/v9/8.2/AC-8.2-07.xml` | Exit `0`; validator exit `1`, `UX_SOURCE_DRIFT`. |
| `AC-8.2-08` | JSON is unchanged while Markdown or order is changed without regeneration | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k rendering_and_order_drift --junitxml=artifacts/v9/8.2/AC-8.2-08.xml` | Exit `0`; separate fixtures prove `UX_RENDER_DRIFT` and `UX_ORDER_DRIFT`. |
| `AC-8.2-09` | One row is activated or assigned current ownership to a historical/nonexistent story | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k activation_and_story_binding_faults --junitxml=artifacts/v9/8.2/AC-8.2-09.xml` | Exit `0`; separate fixtures prove `UX_ACTIVATION_UNAUTHORIZED` and `UX_CURRENT_STORY_INVALID`. |
| `AC-8.2-10` | SHA-256 is captured before every AC-8.2-02 through AC-8.2-09 mutation | `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k fixtures_restore_byte_identically --junitxml=artifacts/v9/8.2/AC-8.2-10.xml` | Exit `0`; every after-hash equals its before-hash; otherwise `FIXTURE_NOT_RESTORED`. |
| `AC-8.2-11` | AC-8.2-01 through AC-8.2-10 and Story 8.1 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.2.json --format bundle --output-json docs/release-evidence/story-8.2-final-record-v2.json --output-markdown docs/release-evidence/story-8.2-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-8.2`, Story 8.1, 52/28 inventories, source/output hashes, mutation ledger, inventory digest, and summary `11/11/0/0/0/0`. |

**Fault injection coverage:** every listed missing/duplicate/unknown, ownership,
hash, source, rendering, order, activation, and story-binding mutation is
mandatory; `FAULT_NOT_DETECTED` or `FIXTURE_NOT_RESTORED` blocks closure.

## Epic 9: Portable Conformance Oracle

**Outcome:** Domain authors receive an objectively tiered oracle whose portable
tier is structural and whose complete execution preserves FR-20.
**Hard entry:** Epic 7, concretely Story 7.4.
**Bounded exit:** Stories 9.1-9.2 are `done` at compatible candidates.
**V8 source owner:** superseded Story 6.9.

### Story 9.1: Freeze the conformance assertion inventory, tier decisions, digest, and approvals

As a test-governance owner,
I want every conformance assertion assigned through a frozen approved inventory,
so that tiering cannot remove, rename, weaken, or hide coverage.

**Bounded outcome:** one deterministic v2 disposition assigns every frozen
pre-split assertion to a justified tier with strength and approval evidence.
**Exact predecessors:** `7.4`.
**Frozen inventory:** `V9-9.1-ENTRY-v1` contains, in order,
`V8-6.9-AC1`, `V8-6.9-AC4`, `V8-6.9-AC5`, and
`V8-6.9-PROHIBITION-NO-PUBLIC-WIDENING`; SHA-256
`31e18fed38706bbb44e5a8059ff3ea30f00400708902694af697954b889bcdf1`.
**Decision input:**
`docs/release-evidence/conformance-oracle-tiering-decision-v2.json`.
**Disposition schema:**
`hexalith.conversations.conformance-oracle-tiering-disposition.v2` at
`docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json`;
required top-level fields are `schemaVersion`, `authority`, `candidate`,
`decision`, `preSplitResult`, `assertions`, `denominatorSuites`, `approvals`,
`supersedes`, and `renderedMarkdownSha256`. Each assertion requires `id`,
`sourcePath`, `sourceSha256`, `preSplitResultIdentity`, `strengthMaterial`,
`strengthSha256`, `serverBindings`, `tier`, `publicReplacement` or
`internalTypeAndReason`, `owner`, `approval`, and `rationale`.
**Candidate binding:** `SC-9.1`, Story 7.4 final-record digest, decision-v2
digest, and the canonical candidate rule.
**Rollback boundary:** remove only Story 9.1 generator, new v2 disposition,
fixtures/results, and final record; preserve v1 artifacts, public-contract
baselines, existing tests, and Story 6.9 partial work.
**Generated outputs:**
`docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` and
deterministic
`docs/release-evidence/conformance-oracle-tiering-disposition-v2.md`.
**Generated final record:**
`docs/release-evidence/story-9.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-9.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-9.1-01` | Both v9 authorities, bound `PC`, decision-v2, pre-split machine result, source tree, and `V9-9.1-ENTRY-v1` | `python3 _bmad/scripts/generate_conformance_tiering.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.1.json --decision docs/release-evidence/conformance-oracle-tiering-decision-v2.json --output-schema docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json --output-json docs/release-evidence/conformance-oracle-tiering-disposition-v2.json --output-markdown docs/release-evidence/conformance-oracle-tiering-disposition-v2.md` | Exit `0`; `PASS`; schema `hexalith.conversations.conformance-oracle-tiering-disposition.v2`; JSON and Markdown exist and bind the source/result/decision digests. |
| `AC-9.1-02` | Every pre-split test case and executable assertion site | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.AssertionInventoryShouldMatchPreSplitResultAndSource -trx artifacts/v9/9.1/AC-9.1-02.trx` | Exit `0`; `PASS`; every identity occurs once with source and strength digest. Missing, duplicate, or renamed identity yields `CONFORMANCE_ASSERTION_MISSING`, `CONFORMANCE_ASSERTION_DUPLICATE`, or `CONFORMANCE_ASSERTION_RENAMED`. |
| `AC-9.1-03` | Each assertion whose source binds a Server namespace or non-packable type | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ServerBoundAssertionsShouldHaveExactDisposition -trx artifacts/v9/9.1/AC-9.1-03.trx` | Exit `0`; `PASS`; each row has tier `portable` with exact public replacement at equal strength or `module-internal` with exact type and reason. Missing data is `TIER_UNASSIGNED` or `TIER_REASON_MISSING`. |
| `AC-9.1-04` | Before/after canonical assertion material | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.StrengthDigestsShouldRemainEqual -trx artifacts/v9/9.1/AC-9.1-04.trx` | Exit `0`; `PASS`; every before/after strength digest matches. Difference is `ASSERTION_STRENGTH_WEAKENED`. |
| `AC-9.1-05` | The three frozen manifested denominator suites and FR-20 manifest membership | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.DenominatorSuitesShouldRemainUnchanged -trx artifacts/v9/9.1/AC-9.1-05.trx` | Exit `0`; `PASS`; identities and membership are unchanged. Difference is `FR20_DENOMINATOR_DRIFT`. |
| `AC-9.1-06` | Every reclassified assertion and denominator-suite row | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ReclassificationsShouldBindApprovals -trx artifacts/v9/9.1/AC-9.1-06.trx` | Exit `0`; `PASS`; named owner, approval identity, rationale, and versioned manifest update are present. Missing evidence is `TIER_APPROVAL_MISSING`. |
| `AC-9.1-07` | Public-contract baseline before `PC` and candidate public surface | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.TieringShouldNotWidenPublicContracts -trx artifacts/v9/9.1/AC-9.1-07.trx` | Exit `0`; `PASS`; public shape digest is unchanged. Any widening is `PUBLIC_CONTRACT_WIDENED`. |
| `AC-9.1-08` | Frozen v1 tiering artifacts and v2 `supersedes` link | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.V2ShouldSupersedeWithoutEditingV1 -trx artifacts/v9/9.1/AC-9.1-08.trx` | Exit `0`; `PASS`; v1 hashes match their protected inventory and v2 links them. Mutation is `V1_ARTIFACT_DRIFT`. |
| `AC-9.1-09` | AC-9.1-01 through AC-9.1-08 and Story 7.4 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.1.json --format bundle --output-json docs/release-evidence/story-9.1-final-record-v2.json --output-markdown docs/release-evidence/story-9.1-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-9.1`, Story 7.4, decision/disposition/pre-split digests, assertion and strength inventories, approvals, inventory digest, and summary `9/9/0/0/0/0`. |

**Fault injection coverage:** missing, duplicate, renamed, weakened, unassigned,
unreasoned, unapproved, denominator-drifted, public-widened, and v1-mutated
fixtures are mandatory and byte-restored.

### Story 9.2: Make the portable tier structural and prove complete monotonic tier execution

As a domain author,
I want consumer-portable assertions isolated from module-internal checks,
so that adopters can execute them without non-packable server dependencies.

**Bounded outcome:** two declared projects implement the approved disposition,
resolved portable compile surface is clean, and summed execution is monotonic.
**Exact predecessors:** `9.1`.
**Frozen inventory:** `V9-9.2-ENTRY-v1` contains, in order,
`V8-6.9-AC2`, `V8-6.9-AC3`, `V8-6.9-AC6`, and
`V8-HR-REQUIRED-TEST-SKIPPED-OR-EMPTY`; SHA-256
`647944fb901bd4b5c1ed6c96867a7a46420dfd0f8e56f71032e9f95474a8f3a9`.
**Tier projects:** portable
`tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj`;
module-internal
`tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj`.
**Candidate binding:** `SC-9.2`, Story 9.1 record/disposition digests, and the
canonical candidate rule.
**Rollback boundary:** remove the portable project and restore every migrated
assertion from the before-inventory as one unit; retain Story 9.1 and never
change a public contract to simplify rollback.
**Generated final record:**
`docs/release-evidence/story-9.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-9.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-9.2-01` | Restored dependency assets and the portable project at `SC-9.2` | `dotnet build tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj --configuration Release --no-restore` | Exit `0`; `PASS`; warnings-as-errors build succeeds. Failure is `PORTABLE_TIER_BUILD_FAILED`. |
| `AC-9.2-02` | Evaluated MSBuild graph and resolved compile assets for the portable project | `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Portable.Tests.PortableCompileSurfaceValidationTest.ResolvedSurfaceShouldContainNoNonPackableModuleReference -trx artifacts/v9/9.2/AC-9.2-02.trx` | Exit `0`; `PASS`; no Server/non-packable module assembly or transitive compile asset exists. Violation is `PORTABLE_TIER_NONPORTABLE_REFERENCE` or `RESOLVED_COMPILE_SURFACE_INVALID`. |
| `AC-9.2-03` | Restored dependency assets and module-internal project at `SC-9.2` | `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` | Exit `0`; `PASS`; warnings-as-errors build succeeds. Failure is `INTERNAL_TIER_BUILD_FAILED`. |
| `AC-9.2-04` | Story 9.1 before-inventory and both post-split projects | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.PostSplitAssertionInventoryShouldEqualApprovedDisposition -trx artifacts/v9/9.2/AC-9.2-04.trx` | Exit `0`; `PASS`; exact identities and strength digests equal the approved before-inventory. Drift is `ASSERTION_INVENTORY_DRIFT` or `ASSERTION_STRENGTH_WEAKENED`. |
| `AC-9.2-05` | Root `.slnx`, Story 7 required-project inventory, and both exact tier project paths | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.BothTiersShouldBeDeclaredEverywhere -trx artifacts/v9/9.2/AC-9.2-05.trx` | Exit `0`; `PASS`; both are present once in solution and completion inventory. Missing project is `TIER_PROJECT_MISSING`; missing declaration is `TIER_NOT_DECLARED`. |
| `AC-9.2-06` | Built portable assembly and exact Story 9.1 portable identities | `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -trx artifacts/v9/9.2/portable.trx` | Exit `0`; `PASS`; zero failed/skipped/not-run and nonzero executed assertions. |
| `AC-9.2-07` | Built module-internal assembly and exact Story 9.1 internal identities | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -trx artifacts/v9/9.2/internal.trx` | Exit `0`; `PASS`; zero failed/skipped/not-run and nonzero executed assertions. |
| `AC-9.2-08` | Pre-split machine result plus AC-9.2-06 and AC-9.2-07 TRX files | `python3 _bmad/scripts/verify_conformance_tiering.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.2.json --portable-result artifacts/v9/9.2/portable.trx --internal-result artifacts/v9/9.2/internal.trx --output artifacts/v9/9.2/AC-9.2-08.json` | Exit `0`; `PASS`; portable plus internal identity set equals the pre-split set and summed executed total is not lower. Regression is `EXECUTED_COUNT_REGRESSION`; empty execution is `ASSERTION_LEDGER_EMPTY`. |
| `AC-9.2-09` | Fault fixtures add Server reference, delete/rename/weaken assertion, omit tier, skip test, or run zero tests | `python3 -m pytest -q _bmad/scripts/tests/test_conformance_tiering.py -k structural_and_execution_faults --junitxml=artifacts/v9/9.2/AC-9.2-09.xml` | Exit `0`; every mutation produces its exact blocker from AC-9.2-01 through AC-9.2-08 and restores byte-identically. Undetected or unrestored fault is `FAULT_NOT_DETECTED` or `FIXTURE_NOT_RESTORED`. |
| `AC-9.2-10` | AC-9.2-01 through AC-9.2-09 and Story 9.1 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.2.json --format bundle --output-json docs/release-evidence/story-9.2-final-record-v2.json --output-markdown docs/release-evidence/story-9.2-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-9.2`, Story 9.1, both project/assembly/result hashes, before/after identity and strength digests, summed counts, inventory digest, and summary `10/10/0/0/0/0`. |

**Fault injection coverage:** nonportable resolved reference, assertion
deletion/rename/weakening, missing tier/project/declaration, skipped/not-run/zero
execution, and count regression are mandatory and byte-restored.

## Epic 10: Unified Evidence Boundary

**Outcome:** Reviewers receive one hardened non-shipping evidence-integrity
boundary across every governed workflow and frozen evidence reader.
**Hard entry:** Epics 7 and 9, concretely Stories 7.4 and 9.2.
**Bounded exit:** Stories 10.1-10.4 are `done` at compatible candidates.
**V8 source owner:** superseded Story 6.10.

### Story 10.1: Provide neutral TestSupport helpers and a safe Git-facts runner

As an evidence-test author,
I want neutral shared boundary helpers and a safe Git runner,
so that evidence checks do not duplicate unsafe repository plumbing.

**Bounded outcome:** one non-packable, Conversations-assembly-free helper
project exposes five exact helper types and safe bounded Git facts.
**Exact predecessors:** `7.4`, `9.2`.
**Frozen inventory:** `V9-10.1-ENTRY-v1` contains, in order,
`V8-6.10-AC1` and `V8-6.10-AC2`; SHA-256
`74c4c5e9e70cb86648dc753c5edc97bdb8d598ad2a4aba55d3670794e990ca84`.
**Projects:**
`tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj`
and
`tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj`.
**Candidate binding:** `SC-10.1`, final-record digests for 7.4 and 9.2, and the
canonical candidate rule.
**Rollback boundary:** remove only both TestSupport projects, Story 10.1
results, and record; preserve both conformance tiers and every evidence reader.
**Generated final record:**
`docs/release-evidence/story-10.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-10.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-10.1-01` | TestSupport project, resolved project graph, and pack/publish policy | `dotnet build tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj --configuration Release --no-restore` | Exit `0`; `PASS`; project is non-packable/non-publishable and references no Conversations assembly. Violation is `TEST_SUPPORT_BOUNDARY_INVALID`. |
| `AC-10.1-02` | Required public helper names `RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`, and `AssertionLedger` | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.TestSupportSurfaceTests.RequiredHelpersShouldExistExactlyOnce -trx artifacts/v9/10.1/AC-10.1-02.trx` | Exit `0`; `PASS`; five types exist once with no production dependency. Missing/extra type is `TEST_SUPPORT_SURFACE_DRIFT`. |
| `AC-10.1-03` | Git fixture emits full concurrent stdout/stderr beyond pipe capacity | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldDrainStdoutAndStderrConcurrently -trx artifacts/v9/10.1/AC-10.1-03.trx` | Exit `0`; `PASS`; both streams drain without deadlock and retain complete bytes. Failure is `GIT_RUNNER_DEADLOCK`. |
| `AC-10.1-04` | Exact timeout `30s`, cancellation token, and a hung fixture process | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldBoundAndCancelExecution -trx artifacts/v9/10.1/AC-10.1-04.trx` | Exit `0`; `PASS`; process tree terminates and classifies `timeout` or `cancelled`. Escape is `GIT_RUNNER_UNBOUNDED`. |
| `AC-10.1-05` | UTF-8/non-ASCII and backslash paths, hostile ambient Git config, raw modes, and `core.quotepath=false` | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.RunnerShouldUseHardenedEncodingAndRawFacts -trx artifacts/v9/10.1/AC-10.1-05.trx` | Exit `0`; `PASS`; decoded paths, revisions, diff entries, raw modes, and historical blob hashes match fixture bytes. Drift is `GIT_FACTS_INVALID`. |
| `AC-10.1-06` | Shallow, partial, non-repository, missing-Git, invalid-revision, and escaping-root fixtures | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.GitFactsTests.UnavailableHistoryShouldBeTypedNotSuccessful -trx artifacts/v9/10.1/AC-10.1-06.trx` | Exit `0`; each fixture is typed `unavailable`/`invalid`, never success; escape is `REPOSITORY_PATH_ESCAPE`; false success is `UNAVAILABLE_HISTORY_PASSED`. |
| `AC-10.1-07` | AC-10.1-01 through AC-10.1-06 and both predecessor records are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.1.json --format bundle --output-json docs/release-evidence/story-10.1-final-record-v2.json --output-markdown docs/release-evidence/story-10.1-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-10.1`, records 7.4/9.2, both project/assembly/result hashes, inventory digest, and summary `7/7/0/0/0/0`. |

**Fault injection coverage:** deadlock-shaped streams, missing Git, timeout,
cancellation, hostile config, malformed UTF-8, invalid revision, and root escape
are mandatory and byte-restored.

### Story 10.2: Implement manifest, hash, ledger, exact-diff, and gitlink invariants

As a release-evidence maintainer,
I want shared assertions to recompute every boundary fact,
so that trusted declarations, subset comparisons, and vacuous checks cannot pass.

**Bounded outcome:** shared helpers enforce manifest, signable-payload, exact
path, gitlink, history, root-of-trust, and anti-vacuity invariants.
**Exact predecessors:** `10.1`.
**Frozen inventory:** `V9-10.2-ENTRY-v1` contains, in order,
`V8-6.10-AC3`, `V8-6.10-AC4`, `V8-6.10-AC5`, and
`V8-HR-BOUND-CONTENT-DRIFT`; SHA-256
`9195f98c90362ce4035ef2223fcf6eab1cc705d0c7cc7ace47b0de9dc809ca4c`.
**Result schema:** `hexalith.conversations.evidence-boundary-result.v1` at
`_bmad/schemas/evidence-boundary-result-v1.schema.json`; required fields are
`schemaVersion`, `result`, `candidate`, `manifest`, `changedPaths`, `gitlinks`,
`assertionLedger`, `warnings`, and `blockers`.
**Candidate binding:** `SC-10.2`, Story 10.1 final-record digest, and the
canonical candidate rule.
**Rollback boundary:** remove only Story 10.2 invariant code/tests/results and
record; retain Story 10.1 intact.
**Generated final record:**
`docs/release-evidence/story-10.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-10.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-10.2-01` | Manifest paths, declared lowercase SHA-256 values, roles, repository root, and real files | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.ManifestShouldRecomputeContainedFilesAndHashes -trx artifacts/v9/10.2/AC-10.2-01.trx` | Exit `0`; `PASS`; paths are relative/contained/existing and recomputed hashes equal declarations. Escape, absence, or mismatch has exact blocker `EVIDENCE_PATH_ESCAPE`, `EVIDENCE_FILE_MISSING`, or `EVIDENCE_HASH_MISMATCH`. |
| `AC-10.2-02` | Manifest includes a `bin/`, `obj/`, or `/generated/` path | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.GeneratedAndBuildOutputsShouldBeRejected -trx artifacts/v9/10.2/AC-10.2-02.trx` | Exit `0`; negative fixtures prove `EVIDENCE_OUTPUT_FORBIDDEN`. |
| `AC-10.2-03` | Canonical ordinal `(path,sha256,role)` rows and declared signable payload digest | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.SignablePayloadShouldBeRecomputed -trx artifacts/v9/10.2/AC-10.2-03.trx` | Exit `0`; `PASS`; recomputed digest matches. Trusted declaration or mismatch is `SIGNABLE_PAYLOAD_MISMATCH`. |
| `AC-10.2-04` | Expected and Git-derived changed-path sets with one missing and one unexpected path fixture | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.ChangedPathsShouldUseExactSetEquality -trx artifacts/v9/10.2/AC-10.2-04.trx` | Exit `0`; clean equality passes; subset/superset fixtures prove `CHANGED_PATH_SET_MISMATCH`. |
| `AC-10.2-05` | Raw diff contains one mode-`160000` entry and decoy text/hash containing `160000` | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.GitlinksShouldBeDetectedFromRawModes -trx artifacts/v9/10.2/AC-10.2-05.trx` | Exit `0`; only the raw-mode entry is rejected with `EVIDENCE_BOUNDARY_GITLINK`; decoys do not trigger. |
| `AC-10.2-06` | Unresolvable history and a consuming test assertion ledger | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.AssertionLedgerTests.UnavailableHistoryShouldSkipAndNeverPass -trx artifacts/v9/10.2/AC-10.2-06.trx` | Exit `0`; consuming check visibly skips and ledger remains zero; any success is `UNAVAILABLE_HISTORY_PASSED`; any final pass with zero is `ASSERTION_LEDGER_EMPTY`. |
| `AC-10.2-07` | Signed source constants in consuming test and a supersession allowlist | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.RootsOfTrustAndSignedEvidenceShouldRemainPinned -trx artifacts/v9/10.2/AC-10.2-07.trx` | Exit `0`; roots remain in consuming source and allowlist cannot contain `docs/release-evidence/`; violation is `ROOT_OF_TRUST_NOT_PINNED` or `SIGNED_EVIDENCE_ALLOWLISTED`. |
| `AC-10.2-08` | Each AC-10.2-01 through AC-10.2-07 fault plus before-hashes | `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceBoundaryFaultTests.AllInvariantFaultsShouldFailAndRestore -trx artifacts/v9/10.2/AC-10.2-08.trx` | Exit `0`; every exact blocker occurs and after-hashes equal before-hashes. Undetected/unrestored is `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-10.2-09` | AC-10.2-01 through AC-10.2-08 and Story 10.1 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.2.json --format bundle --output-json docs/release-evidence/story-10.2-final-record-v2.json --output-markdown docs/release-evidence/story-10.2-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-10.2`, Story 10.1, invariant assembly/results, root constants, mutation ledger, inventory digest, and summary `9/9/0/0/0/0`. |

**Fault injection coverage:** hash/path/generated-output/signable-payload/set/
gitlink/history/ledger/root-of-trust/signed-allowlist faults are mandatory and
byte-restored.

### Story 10.3: Provide the evidence-boundary verifier and integrate every workflow surface

As a workflow owner,
I want one verifier enforced before review and done,
so that evidence changes cannot bypass shared validation.

**Bounded outcome:** one machine verifier governs twelve frozen workflow files
with stable blocker/warning semantics and parity-checked transitions.
**Exact predecessors:** `10.2`.
**Frozen inventory:** `V9-10.3-ENTRY-v1` contains, in order,
`V8-6.10-AC6` and `V8-6.10-AC7`; SHA-256
`1f3c33f610fcb1ef4d748ec6862edb1fa6b945bbbc475b9f06522cdedffb1a4a`.
**Workflow inventory:** `V9-EVIDENCE-WORKFLOWS-v1` is the following NFC UTF-8
LF list; SHA-256
`479f007dbaf77a45fe0c60934e22a9bc9def53d79023315ba77c25085bc4656d`:

```text
.agents/skills/bmad-dev-story/SKILL.md
.agents/skills/bmad-dev-auto/step-04-review.md
.agents/skills/bmad-quick-dev/step-05-present.md
.agents/skills/bmad-quick-dev/step-oneshot.md
.agents/skills/bmad-code-review/steps/step-04-present.md
.claude/skills/bmad-dev-story/SKILL.md
.claude/skills/bmad-dev-auto/step-04-review.md
.claude/skills/bmad-quick-dev/step-05-present.md
.claude/skills/bmad-quick-dev/step-oneshot.md
.claude/skills/bmad-code-review/steps/step-04-present.md
_bmad/render/bmad-quick-dev/step-05-present.md
_bmad/render/bmad-quick-dev/step-oneshot.md
```

**Verifier:** `_bmad/scripts/verify_evidence_boundary.py`; machine output uses
`hexalith.conversations.evidence-boundary-result.v1`.
**Candidate binding:** `SC-10.3`, Story 10.2 final-record digest, workflow
inventory/digest, and the canonical candidate rule.
**Rollback boundary:** remove verifier and all twelve workflow insertions as
one unit, plus Story 10.3 fixtures/results/record; retain Stories 10.1-10.2.
**Generated final record:**
`docs/release-evidence/story-10.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-10.3-final-record-v2.md`.

**Stable blocker codes:** `EVIDENCE_HELPER_NOT_USED`, `ADHOC_GIT_RUNNER`,
`ADHOC_REPOSITORY_ROOT`, `ADHOC_HASH_HELPER`,
`EVIDENCE_ARTIFACT_UNVALIDATED`, `EXEMPTION_EXPIRED`,
`SCOPE_NOT_EVALUATED`, and `BASELINE_NOT_PROVIDED`. Stable warnings are
`EXEMPTION_ACTIVE` and `EVIDENCE_TEST_OUTSIDE_CONFORMANCE`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-10.3-01` | Bound `PC`, `SC-10.3`, baseline resolved from bundle, evidence/test diff, and stable code registry | `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --candidate HEAD --format json --output artifacts/v9/10.3/AC-10.3-01.json` | Exit `0` only for `PASS` or explicit `not-applicable`; schema-valid result records evaluated paths/assertions. Applicable failure returns `1`; missing authority/history returns `2`. |
| `AC-10.3-02` | One fixture per stable blocker and warning code | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k stable_code_contract --junitxml=artifacts/v9/10.3/AC-10.3-02.xml` | Exit `0`; every fixture produces its exact code and no synonym; blockers/warnings are ordinally unique. |
| `AC-10.3-03` | All twelve paths and exact insertion anchor after promotion gate/before state transition | `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --candidate HEAD --verify-workflows --format json --output artifacts/v9/10.3/AC-10.3-03.json` | Exit `0`; `PASS`; each path contains one invocation at the exact span. Missing call is `EVIDENCE_GATE_NOT_USED`; displaced call is `EVIDENCE_GATE_DISPLACED`. |
| `AC-10.3-04` | Five logical bodies in `.agents` and `.claude`, plus two quick-dev render twins | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k workflow_tree_and_render_parity --junitxml=artifacts/v9/10.3/AC-10.3-04.xml` | Exit `0`; normalized logical twins are identical. Difference is `EVIDENCE_WORKFLOW_PARITY_DRIFT`. |
| `AC-10.3-05` | No evidence/test path changed between valid baseline and candidate | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k not_applicable_is_not_pass --junitxml=artifacts/v9/10.3/AC-10.3-05.xml` | Exit `0`; verifier reports `not-applicable`, evaluated `0`, never `PASS`; if evidence changed with zero evaluation it fails `SCOPE_NOT_EVALUATED`. |
| `AC-10.3-06` | Each workflow receives verifier `FAIL`, `BLOCKED`, and `not-applicable` fixtures | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k lifecycle_transition_semantics --junitxml=artifacts/v9/10.3/AC-10.3-06.xml` | Exit `0`; `FAIL`/`BLOCKED` prevent review/done and preserve blockers; valid `not-applicable` continues but is recorded as such. |
| `AC-10.3-07` | One invocation removed and another displaced while decoy vocabulary remains | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k invocation_removal_and_displacement --junitxml=artifacts/v9/10.3/AC-10.3-07.xml` | Exit `0`; exact `EVIDENCE_GATE_NOT_USED` and `EVIDENCE_GATE_DISPLACED` blockers occur and fixtures restore. |
| `AC-10.3-08` | AC-10.3-01 through AC-10.3-07 and Story 10.2 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.3.json --format bundle --output-json docs/release-evidence/story-10.3-final-record-v2.json --output-markdown docs/release-evidence/story-10.3-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-10.3`, Story 10.2, verifier/workflow hashes, exact code registry, inventory digests, seven results, and summary `8/8/0/0/0/0`. |

**Fault injection coverage:** every blocker/warning code, invocation removal,
span displacement, parity drift, vacuous scope, and lifecycle result is
mandatory and byte-restored.

### Story 10.4: Migrate frozen readers, repair gate spans, publish the runbook, and prove fault injection

As a release-evidence maintainer,
I want all frozen readers using the shared boundary and every inherited guard still red-capable,
so that consolidation closes real defects without weakening prior gates.

**Bounded outcome:** all frozen readers migrate at equal strength, the runbook
is complete, fault coverage is exhaustive, and promotion-gate span protection
remains independently effective.
**Exact predecessors:** `10.3`.
**Frozen inventory:** `V9-10.4-ENTRY-v1` contains, in order,
`V8-6.10-AC8`, `V8-6.10-AC9`, and `V8-6.10-AC10`; SHA-256
`fbcf160a9d4beb407c33da4aa5cbdc12040fc71591a9bd9720a89f591d90cc34`.
**Frozen reader inventory:** `V9-EVIDENCE-READERS-v1` contains exactly these 27
paths in ordinal order; SHA-256
`247cd610f7fd162f3e01f1db713f16328b2d009081da14a468e767411209a3bc`:

```text
tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs
tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs
tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs
tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs
tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs
tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs
tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs
tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs
tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs
tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs
tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs
```

This resolves v8's dynamic “24 plus any reader added before implementation” to
the exact 27-reader planning inventory. No later reader is silently absorbed;
adding one requires an approved inventory successor.
**Candidate binding:** `SC-10.4`, Story 10.3 record, before/after reader
identity/strength digests, both frozen inventory digests, and the canonical
candidate rule.
**Rollback boundary:** restore all 27 readers from the before-inventory and
remove only Story 10.4 runbook/fault fixtures/results/record; retain TestSupport
and verifier.
**Runbook:** `docs/runbooks/evidence-boundary-validation.md`.
**Generated final record:**
`docs/release-evidence/story-10.4-final-record-v2.json` and deterministic
`docs/release-evidence/story-10.4-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-10.4-01` | The exact 27-path inventory and before identity/strength material | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.ReaderInventoryShouldMatchFrozenSet -trx artifacts/v9/10.4/AC-10.4-01.trx` | Exit `0`; `PASS`; exact set and inventory digest match. Missing/extra reader is `EVIDENCE_READER_INVENTORY_DRIFT`. |
| `AC-10.4-02` | Before/after assertion identities, pinned constants, and strength material for all 27 readers | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.MigrationsShouldUseHelperAtEqualStrength -trx artifacts/v9/10.4/AC-10.4-02.trx` | Exit `0`; `PASS`; every reader uses TestSupport and identity/strength digests match. Violation is `EVIDENCE_HELPER_NOT_USED` or `ASSERTION_STRENGTH_WEAKENED`. |
| `AC-10.4-03` | Exemption inventory at Story 10.4 entry | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.DayOneExemptionInventoryShouldBeEmpty -trx artifacts/v9/10.4/AC-10.4-03.trx` | Exit `0`; `PASS`; zero exemptions. Any entry is `DAY_ONE_EXEMPTION_FORBIDDEN`. |
| `AC-10.4-04` | Deadlock-shaped Git output and unavailable-history fixtures applied through migrated readers | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.KnownGitDefectsShouldBeClosedWithoutChangingProjectionProof -trx artifacts/v9/10.4/AC-10.4-04.trx` | Exit `0`; concurrent draining and visible skip pass; Story 6.12/projection-proof identity and strength digest remain unchanged. Regression is `GIT_RUNNER_DEADLOCK`, `UNAVAILABLE_HISTORY_PASSED`, or `PROJECTION_PROOF_SCOPE_ABSORBED`. |
| `AC-10.4-05` | Runbook path and required sections `Invariants`, `Authoring`, `Exemptions`, `Fault injection`, and `Known limitations` | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.EvidenceBoundaryAdoptionValidationTest.RunbookShouldDocumentTheExactContract -trx artifacts/v9/10.4/AC-10.4-05.trx` | Exit `0`; `PASS`; every section binds exact codes/commands and does not claim CI wiring. Missing/false guidance is `EVIDENCE_RUNBOOK_INVALID`. |
| `AC-10.4-06` | Mutation IDs `HASH`, `PATH_ESCAPE`, `GENERATED_OUTPUT`, `GITLINK`, `SUBSET`, `SIGNED_ALLOWLIST`, `GIT_UNAVAILABLE`, `WORKFLOW_REMOVED`, `MARKER_MALFORMED`, and three frozen chain-table mutations | `python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py -k complete_fault_matrix --junitxml=artifacts/v9/10.4/AC-10.4-06.xml` | Exit `0`; every mutation yields its exact Story 10.2/10.3 blocker, assertion ledger is nonempty, and fixtures restore. |
| `AC-10.4-07` | Story 6.7 promotion positive span, follower marker, evidence insertion, and displaced-span fixture | `python3 -m pytest -q _bmad/scripts/tests/test_verify_submodule_promotion.py -k evidence_gate_span_coupling --junitxml=artifacts/v9/10.4/AC-10.4-07.xml` | Exit `0`; positive guard passes only at exact span and displaced guard fails. False green is `PROMOTION_GATE_SPAN_WEAKENED`. |
| `AC-10.4-08` | AC-10.4-01 through AC-10.4-07 and Story 10.3 record are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.4.json --format bundle --output-json docs/release-evidence/story-10.4-final-record-v2.json --output-markdown docs/release-evidence/story-10.4-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-10.4`, Story 10.3, 27 reader before/after digests, runbook, mutation ledger, gate-span proof, both inventory digests, and summary `8/8/0/0/0/0`. |

**Fault injection coverage:** all ten named boundary mutations, three chain-table
mutations, both Git defects, reader drift, strength drift, exemption, and gate
span displacement are mandatory and byte-restored.

## Epic 11: Thin-Module Authoring Proof

**Outcome:** Module authors receive a corrected platform-owned thin-module
recipe, a reproducible minimal fixture, and decision-grade SM-2 evidence rather
than an estimate-qualified historical claim.
**Hard entry:** Story 6.2 plus Epics 7 and 10, concretely Stories 7.4 and 10.4.
**Bounded exit:** Stories 11.1-11.3 are `done` at one compatible candidate
chain and Checkpoints A-C are independently evidenced.
**V8 source owner:** superseded Story 6.5.

### Story 11.1: Correct and validate platform-hosted thin-module authoring guidance

As a module author,
I want one versioned recipe that assigns infrastructure to the platform,
so that a module does not recreate runtime capabilities or ship its test host.

**Bounded outcome:** Checkpoint A publishes one generated guidance bundle and
reviewer decision covering exact ownership and prohibition rules; it does not
create the fixture or calculate SM-2.
**Exact predecessors:** `6.2`, `7.4`, `10.4`.
**Frozen inventory:** `V9-11.1-ENTRY-v1` contains, in order,
`V8-6.5-AC1`, `V8-6.5-AC2`, and `V8-6.5-CHECKPOINT-A`; SHA-256
`b6c050f6eb387a645fb85bff6d98be90ef9286227c33974c93269f8acbde56ac`.
**Guidance source:** `docs/domain-module-authoring-template.md`.
**Generated evidence:**
`docs/release-evidence/thin-authoring-guidance-v2.schema.json`,
`docs/release-evidence/thin-authoring-guidance-v2.json`, and deterministic
`docs/release-evidence/thin-authoring-guidance-v2.md`; schema identifier
`hexalith.conversations.thin-authoring-guidance.v2`. Required fields are
`schemaVersion`, `candidate`, `source`, `moduleOwned`, `platformOwned`,
`prohibited`, `testAppHost`, `validation`, `reviewDecision`, and `digests`.
**Exact ownership:** module-owned inventory is contracts, domain behavior,
module registration, and module tests. Platform-owned inventory is
`ServiceDefaults`, Dapr implementation/wiring, event subscription,
projection/query runtime, publication, health, and telemetry. A module may
contain exactly one repository-only test AppHost; it must set
`IsPackable=false` and `IsPublishable=false` and is included in authoring cost.
A reusable module Aspire library or module-owned implementation of any
platform-owned capability is prohibited.
**Candidate binding:** `SC-11.1`, predecessor final-record digests, source and
generated-bundle digests, reviewer identity/decision time, and the canonical
candidate rule.
**Rollback boundary:** restore only the Story 11.1 guidance source and remove
its generator, validator, generated v2 bundle, results, and final record;
preserve all predecessor records and historical guidance evidence.
**Generated final record:**
`docs/release-evidence/story-11.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-11.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-11.1-01` | Bound authority bundle, `PC`, `SC-11.1`, and all three predecessor records | `python3 _bmad/scripts/verify_story_candidate.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --candidate HEAD --output artifacts/v9/11.1/AC-11.1-01.json` | Exit `0`; `PASS`; candidate and exact predecessor record digests are compatible. Missing or mixed binding is `CANDIDATE_UNBOUND` or `PREDECESSOR_CANDIDATE_MISMATCH`. |
| `AC-11.1-02` | Guidance source and exact ownership inventory | `python3 _bmad/scripts/generate_thin_authoring_guidance.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --schema docs/release-evidence/thin-authoring-guidance-v2.schema.json --output-json docs/release-evidence/thin-authoring-guidance-v2.json --output-markdown docs/release-evidence/thin-authoring-guidance-v2.md` | Exit `0`; `PASS`; JSON/Markdown deterministically encode the exact module/platform ownership inventory. Missing, additional, or reassigned capability is `THIN_GUIDANCE_OWNERSHIP_INVALID`. |
| `AC-11.1-03` | Generated bundle and project rules for the repository-only test AppHost | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.GuidanceShouldDefineOneNonShippingTestAppHost -trx artifacts/v9/11.1/AC-11.1-03.trx` | Exit `0`; `PASS`; exactly one test AppHost is allowed, is non-packable/non-publishable, remains repository-only, and is counted in SM-2. Violation is `APPHOST_BOUNDARY_INVALID`. |
| `AC-11.1-04` | Guidance bundle and prohibited inventory | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.GuidanceShouldProhibitPlatformCapabilityOwnership -trx artifacts/v9/11.1/AC-11.1-04.trx` | Exit `0`; `PASS`; reusable Aspire/ServiceDefaults, Dapr implementation, projection/query runtime, publication, health, telemetry, and subscription ownership are explicitly forbidden. Violation is `PROHIBITED_CAPABILITY_PRESENT`. |
| `AC-11.1-05` | Schema-valid bundle and a reviewer disposition bound to its signable-payload digest | `python3 _bmad/scripts/verify_thin_authoring_guidance.py --repository . --evidence docs/release-evidence/thin-authoring-guidance-v2.json --markdown docs/release-evidence/thin-authoring-guidance-v2.md --require-review-decision --output artifacts/v9/11.1/AC-11.1-05.json` | Exit `0`; `PASS`; decision is `approved`, names reviewer and UTC instant, and binds exact source/evidence digests. Absence is `REVIEW_DECISION_MISSING`; drift is `GUIDANCE_SOURCE_DRIFT` or `EVIDENCE_FORMAT_DRIFT`. |
| `AC-11.1-06` | Fixtures reassign each platform capability, add a reusable Aspire library, make AppHost packable/publishable, omit its cost rule, alter source after generation, or remove review | `python3 -m pytest -q _bmad/scripts/tests/test_thin_authoring_guidance.py -k checkpoint_a_fault_matrix --junitxml=artifacts/v9/11.1/AC-11.1-06.xml` | Exit `0`; every mutation yields its exact blocker from AC-11.1-02 through AC-11.1-05 and restores byte-identically. Undetected/unrestored mutation is `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-11.1-07` | AC-11.1-01 through AC-11.1-06 are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --format bundle --output-json docs/release-evidence/story-11.1-final-record-v2.json --output-markdown docs/release-evidence/story-11.1-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-11.1`, all predecessor records, source/generated/reviewer digests, entry inventory, mutation ledger, and summary `7/7/0/0/0/0`. |

**Fault injection coverage:** every prohibited ownership transfer, reusable
Aspire library, AppHost count/pack/publish/cost boundary, source/evidence drift,
and missing reviewer decision are mandatory and byte-restored.

### Story 11.2: Build the reproducible minimal-module fixture against live platform APIs

As a module author,
I want a clean minimal module that uses the public platform surface,
so that the thin-authoring recipe is executable rather than aspirational.

**Bounded outcome:** Checkpoint B produces one non-shipping fixture that
restores, builds, and tests cleanly against live public platform APIs; it does
not calculate or decide SM-2.
**Exact predecessors:** `11.1`.
**Frozen inventory:** `V9-11.2-ENTRY-v1` contains, in order,
`V8-6.5-AC3` and `V8-6.5-CHECKPOINT-B`; SHA-256
`2aa59728bdc866263bf31a5f258fb80935fa07485459f795f07f6eb60eb123ac`.
**Fixture root:** `tests/fixtures/Hexalith.Conversations.MinimalModule/`.
**Frozen fixture source inventory:** `V9-MINIMAL-MODULE-FIXTURE-v1` contains
exactly the following NFC UTF-8 LF paths in ordinal order; SHA-256
`4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c`:

```text
tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props
tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx
tests/fixtures/Hexalith.Conversations.MinimalModule/README.md
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs
tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs
tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj
tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs
tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj
tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs
```

**Generated evidence:**
`docs/release-evidence/thin-module-fixture-proof-v1.schema.json`,
`docs/release-evidence/thin-module-fixture-proof-v1.json`, and deterministic
`docs/release-evidence/thin-module-fixture-proof-v1.md`; schema identifier
`hexalith.conversations.thin-module-fixture-proof.v1`. Required fields are
`schemaVersion`, `candidate`, `guidanceDigest`, `fixtureInventory`,
`resolvedPublicApis`, `restore`, `build`, `tests`, `appHost`, and `digests`.
**Candidate binding:** `SC-11.2`, Story 11.1 final-record/guidance digests,
exact fixture inventory, resolved platform project/assembly hashes, and the
canonical candidate rule.
**Rollback boundary:** remove only the frozen fixture, fixture proof generator,
validator, results, generated proof, and Story 11.2 record; retain Story 11.1.
**Generated final record:**
`docs/release-evidence/story-11.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-11.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-11.2-01` | Story 11.1 record and exact frozen path inventory at `SC-11.2` | `python3 _bmad/scripts/verify_minimal_module_fixture.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --fixture tests/fixtures/Hexalith.Conversations.MinimalModule --inventory-only --output artifacts/v9/11.2/AC-11.2-01.json` | Exit `0`; `PASS`; exact set/digest matches and every path is contained, source-controlled, hand-authored, and LF-normalized. Drift is `MINIMAL_FIXTURE_INVENTORY_DRIFT`. |
| `AC-11.2-02` | Exact fixture solution and repository lock files | `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode` | Exit `0`; `PASS`; all assets resolve from repository-approved sources. Failure is `MINIMAL_FIXTURE_RESTORE_FAILED`. |
| `AC-11.2-03` | Restored fixture at `SC-11.2` | `dotnet build tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore` | Exit `0`; `PASS`; warnings-as-errors clean build uses live public platform APIs. Failure is `MINIMAL_FIXTURE_BUILD_FAILED`; private/internal API reference is `NONPUBLIC_PLATFORM_API_USED`. |
| `AC-11.2-04` | Built minimal-module tests | `dotnet tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll -automated sync -failSkips -trx artifacts/v9/11.2/minimal-module.trx` | Exit `0`; `PASS`; nonzero tests execute with zero failed/skipped/not-run and prove registration, command handling, event production, and platform-hosted subscription/projection/query/publication paths. Empty/vacuous execution is `MINIMAL_FIXTURE_TESTS_VACUOUS`. |
| `AC-11.2-05` | Evaluated fixture project graph and AppHost properties | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary -trx artifacts/v9/11.2/AC-11.2-05.trx` | Exit `0`; `PASS`; exactly one test AppHost is non-packable/non-publishable and no reusable module Aspire/ServiceDefaults project exists. Violation is `APPHOST_BOUNDARY_INVALID` or `REUSABLE_ASPIRE_LIBRARY_PRESENT`. |
| `AC-11.2-06` | Build/test outputs, resolved public API graph, exact inventory, and guidance digest | `python3 _bmad/scripts/generate_minimal_module_fixture_proof.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --fixture tests/fixtures/Hexalith.Conversations.MinimalModule --test-result artifacts/v9/11.2/minimal-module.trx --output-json docs/release-evidence/thin-module-fixture-proof-v1.json --output-markdown docs/release-evidence/thin-module-fixture-proof-v1.md` | Exit `0`; `PASS`; schema-valid JSON/Markdown bind exact commands, tool versions, candidate, public API/assembly hashes, AppHost facts, inventory, results, and signable-payload digest. Drift is `FIXTURE_PROOF_DRIFT`. |
| `AC-11.2-07` | Fixtures add/remove AppHost, make it shipping, add reusable Aspire/ServiceDefaults or platform runtime ownership, use an internal API, remove tests, or change inventory | `python3 -m pytest -q _bmad/scripts/tests/test_minimal_module_fixture.py -k checkpoint_b_fault_matrix --junitxml=artifacts/v9/11.2/AC-11.2-07.xml` | Exit `0`; every mutation produces its exact blocker from AC-11.2-01 through AC-11.2-06 and restores byte-identically. Undetected/unrestored mutation is `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-11.2-08` | AC-11.2-01 through AC-11.2-07 and Story 11.1 are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.2.json --format bundle --output-json docs/release-evidence/story-11.2-final-record-v2.json --output-markdown docs/release-evidence/story-11.2-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-11.2`, Story 11.1, fixture/project/public-API/result/proof digests, both inventories, mutation ledger, and summary `8/8/0/0/0/0`. |

**Fault injection coverage:** fixture path drift, missing/extra/shipping AppHost,
reusable Aspire/ServiceDefaults, platform-capability ownership, nonpublic API,
empty/skipped tests, and proof drift are mandatory and byte-restored.

### Story 11.3: Generate authoritative SM-2 v2 evidence and decide OQ-2

As a release reviewer,
I want authoring cost recomputed from the proved fixture and preserved baseline,
so that OQ-2 is decided from reproducible facts.

**Bounded outcome:** Checkpoint C publishes one authoritative SM-2 v2 bundle
and OQ-2 disposition; it does not alter either fixture or historical baseline.
**Exact predecessors:** `11.2`.
**Frozen inventory:** `V9-11.3-ENTRY-v1` contains, in order,
`V8-6.5-AC4`, `V8-6.5-AC5`, `V8-6.5-AC6`, and
`V8-6.5-CHECKPOINT-C`; SHA-256
`6e6c4f5c91c045274222652dbf43400eab1ddbd9641c2fd06b56977ecbf107f5`.
**Preserved baselines:** the SM-1 compatibility surface remains exactly
`13,289`;
`docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json` and
`docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md` remain
immutable historical evidence.
**Generated evidence:**
`docs/release-evidence/minimal-module-authoring-cost-sm2-v2.schema.json`,
`docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json`, and
deterministic
`docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md`; schema
identifier `hexalith.conversations.minimal-module-authoring-cost-sm2.v2`.
Required fields are `schemaVersion`, `candidate`, `fixtureProofDigest`,
`inclusionRules`, `sourceInventory`, `baseline`, `actual`, `reductions`,
`confidence`, `oq2Decision`, `toolVersions`, and `digests`.
**Decision rule:** all 13 frozen fixture paths, including the test AppHost, are
counted when hand-authored; generated/build outputs are excluded. File-count
reduction against the preserved reference is decisive and must be at least
`50.00%`; LOC reduction is supporting evidence. `confidence` must be `high`
only when every source path and line is reproducibly counted.
**Candidate binding:** `SC-11.3`, Story 11.2 record/fixture-proof digests,
historical baseline digests, exact source inventory and tool versions, and the
canonical candidate rule. All Checkpoints A-C must resolve to this compatible
candidate chain.
**Rollback boundary:** remove only the SM-2 v2 generator, validator, results,
generated v2 bundle, and Story 11.3 record; preserve the fixture, guidance,
SM-1 `13,289`, and every v1 historical byte.
**Generated final record:**
`docs/release-evidence/story-11.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-11.3-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-11.3-01` | Story 11.2 fixture proof, exact source inventory, preserved reference baseline, and `SC-11.3` | `python3 _bmad/scripts/generate_sm2_evidence.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.3.json --fixture-proof docs/release-evidence/thin-module-fixture-proof-v1.json --baseline docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json --output-json docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json --output-markdown docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md` | Exit `0`; `PASS`; generator records exact files/physical LOC, inclusion/exclusion reasons, candidate, command/tool versions, reductions, confidence, and signable-payload digest. Missing source is `SM2_SOURCE_INVENTORY_DRIFT`; count failure is `SM2_MEASUREMENT_INVALID`. |
| `AC-11.3-02` | Generated SM-2 v2 bundle and frozen inclusion rules | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest.SmTwoV2ShouldCountEveryHandAuthoredFixtureFileIncludingAppHost -trx artifacts/v9/11.3/AC-11.3-02.trx` | Exit `0`; `PASS`; all 13 paths are classified exactly once and the test AppHost is included. Omission is `SM2_APPHOST_COST_OMITTED`; double/unclassified path is `SM2_INCLUSION_RULE_VIOLATION`. |
| `AC-11.3-03` | Preserved SM-1 value and historical SM-2 v1 files plus their recorded hashes | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.MinimalModuleAuthoringCostBaselineValidationTest.HistoricalBaselinesShouldRemainByteIdentical -trx artifacts/v9/11.3/AC-11.3-03.trx` | Exit `0`; `PASS`; SM-1 is exactly `13,289` and both v1 files retain recorded hashes. Mutation is `SM1_BASELINE_MUTATED` or `SM2_V1_HISTORY_MUTATED`. |
| `AC-11.3-04` | Recomputed file/LOC reductions and OQ-2 target | `python3 _bmad/scripts/verify_sm2_evidence.py --repository . --evidence docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json --markdown docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md --require-oq2-decision --output artifacts/v9/11.3/AC-11.3-04.json` | Exit `0` only when schema, candidate, hashes, arithmetic, confidence, JSON/Markdown parity, and decision are valid. `oq2Decision` is `accepted` only for file reduction `>=50.00%`; otherwise it is `rejected` without hiding the result. Invalid acceptance is `OQ2_DECISION_INVALID`; format drift is `EVIDENCE_FORMAT_DRIFT`. |
| `AC-11.3-05` | Story 11.1, 11.2, and current SM-2 candidate fields | `python3 _bmad/scripts/verify_story_candidate.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --contract _bmad-output/planning-artifacts/v9/story-contracts/11.3.json --candidate HEAD --output artifacts/v9/11.3/AC-11.3-05.json` | Exit `0`; `PASS`; Checkpoints A-C share one compatible `PC`/successor chain. Mismatch is `CHECKPOINT_CANDIDATE_MISMATCH`. |
| `AC-11.3-06` | Faults add prohibited module ownership, omit AppHost or another source from cost, trust declared counts, mutate `13,289`/v1, alter arithmetic/confidence/decision, or drift JSON/Markdown | `python3 -m pytest -q _bmad/scripts/tests/test_sm2_evidence.py -k checkpoint_c_fault_matrix --junitxml=artifacts/v9/11.3/AC-11.3-06.xml` | Exit `0`; every mutation yields its exact blocker from AC-11.3-01 through AC-11.3-05, no vacuous evidence passes, and fixtures restore byte-identically. Undetected/unrestored mutation is `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-11.3-07` | AC-11.3-01 through AC-11.3-06 and Stories 11.1-11.2 are current and compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.3.json --format bundle --output-json docs/release-evidence/story-11.3-final-record-v2.json --output-markdown docs/release-evidence/story-11.3-final-record-v2.md` | Exit `0`; `PASS`; final-record v2 binds `SC-11.3`, the A-C chain, guidance/fixture/baseline/SM-2 digests, actual metrics, OQ-2 decision, inventory, mutation ledger, and summary `7/7/0/0/0/0`. |

**Fault injection coverage:** prohibited ownership, missing/double-counted source,
omitted AppHost cost, trusted declaration, vacuous result, baseline/history
mutation, arithmetic/confidence/decision error, candidate mismatch, and
JSON/Markdown drift are mandatory and byte-restored.

## Epic 12: Universal Performance Restoration

**Outcome:** Operators receive correctness-preserving projection performance
under the one universal SM-C2 rule for all four frozen hot paths.
**Hard entry:** completed immutable Story 6.2.
**Bounded exit:** Stories 12.1-12.4 are `done`; all four rows have usable,
comparable evidence and individually satisfy the universal threshold.
**V8 source owner:** superseded Story 6.11.

### Story 12.1: Approve derived-key ownership, lifecycle, and rollback

As a release owner,
I want the derived-key lifecycle decided before production changes,
so that optimization cannot create a second write authority.

**Bounded outcome:** one approved ADR and generated decision bundle fixes key
ownership and lifecycle; no production source or benchmark method changes.
**Exact predecessors:** `6.2`.
**Frozen inventory:** `V9-12.1-ENTRY-v1` contains, in order,
`V8-6.11-AC1`, `V8-6.11-DEPENDENCY-6.2`, and
`V8-6.11-PROHIBITION-EVENTSTORE-ONLY-WRITE-AUTHORITY`; SHA-256
`b8b755088071d62e36f8e6829d90a8fef3dc8eb54e8f521297ef22b3bfdebf0b`.
**ADR:** `docs/adrs/0005-conversation-derived-key-lifecycle.md`.
**Generated evidence:**
`docs/release-evidence/derived-key-lifecycle-decision-v1.schema.json`,
`docs/release-evidence/derived-key-lifecycle-decision-v1.json`, and
deterministic
`docs/release-evidence/derived-key-lifecycle-decision-v1.md`; schema
`hexalith.conversations.derived-key-lifecycle-decision.v1` requires
`candidate`, `keyFamilies`, `ownership`, `writeOrdering`, `compatibility`,
`rebuild`, `backfill`, `deletion`, `expiry`, `rollback`, `approval`, `digests`.
**Candidate binding:** `SC-12.1`, immutable Story 6.2 candidate/record and
baseline hashes, ADR/decision/approval digests, and the canonical rule.
**Rollback boundary:** remove only ADR 0005, its validator/generated decision,
fixtures/results, and Story 12.1 record; Story 6.2 and SM-C2 v1 stay immutable.
**Generated final record:**
`docs/release-evidence/story-12.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-12.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-12.1-01` | Story 6.2 record, baseline v1, `PC`, and `SC-12.1` | `python3 _bmad/scripts/verify_story_candidate.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --contract _bmad-output/planning-artifacts/v9/story-contracts/12.1.json --candidate HEAD --output artifacts/v9/12.1/AC-12.1-01.json` | Exit `0`; `PASS`; exact immutable predecessor identities resolve. Mismatch is `PREDECESSOR_CANDIDATE_MISMATCH`. |
| `AC-12.1-02` | ADR 0005 and the frozen lifecycle fields | `python3 _bmad/scripts/generate_derived_key_decision.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.1.json --adr docs/adrs/0005-conversation-derived-key-lifecycle.md --output-json docs/release-evidence/derived-key-lifecycle-decision-v1.json --output-markdown docs/release-evidence/derived-key-lifecycle-decision-v1.md` | Exit `0`; `PASS`; every per-conversation key family and lifecycle field is nonempty and deterministic. Omission is `DERIVED_KEY_ADR_INCOMPLETE`. |
| `AC-12.1-03` | Generated decision and ownership/write-order rules | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.DerivedKeyLifecycleAdrValidationTest.EventStoreShouldRemainOnlyWriteAuthority -trx artifacts/v9/12.1/AC-12.1-03.trx` | Exit `0`; `PASS`; derived keys remain replaceable projection state and no read repairs durable state. Violation is `EVENTSTORE_AUTHORITY_VIOLATED`. |
| `AC-12.1-04` | Compatibility, interrupted rebuild, deletion/expiry, rollback, and approval sections | `python3 _bmad/scripts/verify_derived_key_decision.py --repository . --evidence docs/release-evidence/derived-key-lifecycle-decision-v1.json --markdown docs/release-evidence/derived-key-lifecycle-decision-v1.md --require-approval --output artifacts/v9/12.1/AC-12.1-04.json` | Exit `0`; `PASS`; transitions fail closed and named owner approval binds digest. Unsafe transition is `DERIVED_KEY_TRANSITION_UNSAFE`; missing approval/rollback is `ADR_APPROVAL_MISSING`/`ROLLBACK_PLAN_INVALID`. |
| `AC-12.1-05` | Faults omit each lifecycle field, assign write authority, permit read repair, break interrupted rebuild/rollback, or remove approval | `python3 -m pytest -q _bmad/scripts/tests/test_derived_key_decision.py -k lifecycle_fault_matrix --junitxml=artifacts/v9/12.1/AC-12.1-05.xml` | Exit `0`; exact blockers occur and fixtures restore; otherwise `FAULT_NOT_DETECTED` or `FIXTURE_NOT_RESTORED`. |
| `AC-12.1-06` | AC-12.1-01 through AC-12.1-05 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.1.json --format bundle --output-json docs/release-evidence/story-12.1-final-record-v2.json --output-markdown docs/release-evidence/story-12.1-final-record-v2.md` | Exit `0`; final record binds candidate, Story 6.2, ADR/decision/approval and inventory/mutation digests, summary `6/6/0/0/0/0`. |

### Story 12.2: Freeze the benchmark method and signal-quality algorithm

As a performance reviewer,
I want the measurement and signal rules fixed before observation,
so that adverse data cannot be selected away.

**Bounded outcome:** one method bundle freezes measurement, comparability, and
signal-quality rules; it changes neither production code nor measured verdicts.
**Exact predecessors:** `12.1`.
**Frozen inventory:** `V9-12.2-ENTRY-v1` contains, in order,
`V8-6.11-AC5`, `V8-6.11-PROHIBITION-NO-THRESHOLD-CHANGE`, and
`V8-6.11-PROHIBITION-NO-ADVERSE-SAMPLE-DISCARD`; SHA-256
`be973a601eee4a029fb225a144a7c4dbbfa4a4ca9966a83e372eb2821ceeb649`.
**Frozen rows:** ordinal `HP-APPEND`, `HP-CREATE`, `HP-LIST`, `HP-OPEN` under
`sm-c2-hot-path-inventory-v1`; every row uses
`postP95 <= 1.05 * baselineP95`. Cost acceptance is forbidden.
**Generated evidence:**
`docs/release-evidence/sm-c2-measurement-method-v2.schema.json`,
`docs/release-evidence/sm-c2-measurement-method-v2.json`, and deterministic
`docs/release-evidence/sm-c2-measurement-method-v2.md`; schema
`hexalith.conversations.sm-c2-measurement-method.v2` requires
`candidate`, `inventoryVersion`, `rows`, `warmup`, `repetitions`, `retention`,
`classification`, `environment`, `p95Algorithm`, `comparability`,
`signalAlgorithm`, `threshold`, `approval`, and `digests`.
**Candidate binding:** `SC-12.2`, Story 12.1 record, immutable baseline fixture,
project/result hashes, method and approval digests, canonical candidate rule.
**Rollback boundary:** remove only the v2 method generator/validator/bundle,
fixtures/results, and Story 12.2 record; retain ADR and all v1 evidence.
**Generated final record:**
`docs/release-evidence/story-12.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-12.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-12.2-01` | Frozen four-row inventory, baseline fixture, and ADR decision | `python3 _bmad/scripts/generate_sm_c2_method.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.2.json --baseline docs/release-evidence/sm-c2-hot-path-baseline-v1.json --output-json docs/release-evidence/sm-c2-measurement-method-v2.json --output-markdown docs/release-evidence/sm-c2-measurement-method-v2.md` | Exit `0`; all four exact rows and every required method field are generated. Drift is `SMC2_METHOD_INCOMPLETE`. |
| `AC-12.2-02` | Method threshold and all legacy cost/record-only variants | `python3 _bmad/scripts/verify_sm_c2_method.py --repository . --evidence docs/release-evidence/sm-c2-measurement-method-v2.json --markdown docs/release-evidence/sm-c2-measurement-method-v2.md --output artifacts/v9/12.2/AC-12.2-02.json` | Exit `0`; every row uses exactly factor `1.05`; no ceiling, disclosure, approval, or recorded-only substitute exists. Violation is `SMC2_THRESHOLD_MUTATED`. |
| `AC-12.2-03` | Fixed repetitions, raw retention, warm/cold split, environment, nearest-rank P95, and predeclared signal algorithm | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.SmC2MeasurementMethodValidationTest.MethodShouldBePredeclaredAndDeterministic -trx artifacts/v9/12.2/AC-12.2-03.trx` | Exit `0`; `PASS`; fields are executable and cannot discard samples after observation. Violation is `SMC2_SAMPLE_DISCARDED`, `SMC2_ENVIRONMENT_UNCONTROLLED`, or `SMC2_SIGNAL_RULE_MUTATED`. |
| `AC-12.2-04` | Reordered/omitted rows, threshold variants, adverse-sample deletion, warm/cold substitution, environment drift, altered percentile/signal rule, or postdated approval | `python3 -m pytest -q _bmad/scripts/tests/test_sm_c2_method.py -k method_fault_matrix --junitxml=artifacts/v9/12.2/AC-12.2-04.xml` | Exit `0`; each exact blocker occurs and fixtures restore; otherwise `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-12.2-05` | AC-12.2-01 through AC-12.2-04 and Story 12.1 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.2.json --format bundle --output-json docs/release-evidence/story-12.2-final-record-v2.json --output-markdown docs/release-evidence/story-12.2-final-record-v2.md` | Exit `0`; record binds method, four rows, baseline/fixture, ADR, approval and mutation digests, summary `5/5/0/0/0/0`. |

### Story 12.3: Implement correctness-preserving list/open optimization and migration behavior

As an operator,
I want list and open paths to avoid unnecessary fan-out,
so that performance improves without weakening fail-closed projection behavior.

**Bounded outcome:** production derived-key and migration/replay behavior is
implemented with all correctness lanes green; no final SM-C2 evidence is issued.
**Exact predecessors:** `12.1`, `12.2`.
**Frozen inventory:** `V9-12.3-ENTRY-v1` contains, in order,
`V8-6.11-AC2`, `V8-6.11-AC3`, `V8-6.11-AC4`, `V8-6.11-AC8`,
`V8-6.11-PROHIBITION-READS-NEVER-REPAIR-DURABLE-STATE`, and
`V8-6.11-ROLLBACK-CORRECTNESS-PRESERVING`; SHA-256
`90b74a649cd827494f199bb722c3e1708306cc212f5a12df7325218dc4c31546`.
**Generated evidence:**
`docs/release-evidence/derived-key-correctness-proof-v1.schema.json`,
`docs/release-evidence/derived-key-correctness-proof-v1.json`, and deterministic
`docs/release-evidence/derived-key-correctness-proof-v1.md`; schema
`hexalith.conversations.derived-key-correctness-proof.v1` requires
candidate, ADR/method digests, production path inventory, public-contract
digest, unit/integration/Dapr results, mutation ledger, replay and rollback.
**Candidate binding:** `SC-12.3`, both predecessor records, affected production
paths and EventStore gitlink, public contract snapshot, result/proof digests.
**Rollback boundary:** revert the exact Story 12.3 production migration as one
unit and remove its tests/proof/record; derived state is rebuilt from EventStore
under ADR rollback, never reverse-written. Retain Stories 12.1-12.2.
**Generated final record:**
`docs/release-evidence/story-12.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-12.3-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-12.3-01` | ADR key families and production list/open path inventory | `dotnet tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.ListAndOpenShouldUseApprovedBoundedReads -trx artifacts/v9/12.3/AC-12.3-01.trx` | Exit `0`; list/open remove only explicitly proved full-index/per-row fan-out. Unexpected read is `DERIVED_KEY_FANOUT_UNBOUNDED`. |
| `AC-12.3-02` | Missing, duplicate, stale, advanced, malformed, misfiled, pending, and inconsistent derived-state fixtures | `dotnet tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.InvalidDerivedStateShouldFailClosedWithoutRepair -trx artifacts/v9/12.3/AC-12.3-02.trx` | Exit `0`; every case fails closed and performs zero durable repair. Violation is `DERIVED_STATE_FAIL_CLOSED_VIOLATION` or `READ_SIDE_REPAIR_ATTEMPTED`. |
| `AC-12.3-03` | Tenant collisions, retries/idempotency, delayed/out-of-order delivery, equal-position conflict, deletion, replay, and interrupted rebuild | `dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -class Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests -trx artifacts/v9/12.3/integration.trx` | Exit `0`; nonzero execution is deterministic/non-disclosing. Failure is `TENANT_DISCLOSURE`, `REPLAY_NONDETERMINISTIC`, or `DERIVED_KEY_TRANSITION_UNSAFE`. |
| `AC-12.3-04` | Real Dapr state-store lane with partial writes, latency, unavailable store, poison records, retries, and concurrency faults | `dotnet tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests -trx artifacts/v9/12.3/dapr.trx` | Exit `0`; all faults fail closed/recover deterministically with zero cross-tenant disclosure. Red/skip/not-run is `DAPR_CORRECTNESS_LANE_RED`. |
| `AC-12.3-05` | Public query snapshot, filters, ordering, cursors, freshness vocabulary, forbidden/nonexistent equivalence, and response shapes | `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.PublicQueryCompatibilityTests.DerivedKeyOptimizationShouldPreserveQueryContract -trx artifacts/v9/12.3/AC-12.3-05.trx` | Exit `0`; exact snapshot unchanged; drift is `PUBLIC_QUERY_CONTRACT_DRIFT`. |
| `AC-12.3-06` | Three lane results, path/contract inventories, ADR/method, and rollback rehearsal | `python3 _bmad/scripts/generate_derived_key_correctness_proof.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.3.json --unit-result artifacts/v9/12.3/AC-12.3-02.trx --integration-result artifacts/v9/12.3/integration.trx --dapr-result artifacts/v9/12.3/dapr.trx --output-json docs/release-evidence/derived-key-correctness-proof-v1.json --output-markdown docs/release-evidence/derived-key-correctness-proof-v1.md` | Exit `0`; proof recomputes identities/results and rollback; any empty/red lane is `CORRECTNESS_GATE_RED`. |
| `AC-12.3-07` | Every invalid-state and Dapr fault plus tenant/replay/public-contract/read-repair mutations | `python3 -m pytest -q _bmad/scripts/tests/test_derived_key_correctness.py -k complete_fault_matrix --junitxml=artifacts/v9/12.3/AC-12.3-07.xml` | Exit `0`; exact blockers occur and all fixtures restore. |
| `AC-12.3-08` | AC-12.3-01 through AC-12.3-07 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.3.json --format bundle --output-json docs/release-evidence/story-12.3-final-record-v2.json --output-markdown docs/release-evidence/story-12.3-final-record-v2.md` | Exit `0`; record binds source, predecessors, three lanes, proof/rollback/mutations/inventory, summary `8/8/0/0/0/0`. |

### Story 12.4: Produce candidate-bound evidence and enforce universal SM-C2

As a release owner,
I want one additive four-row verdict at the optimized candidate,
so that no row or correctness failure can be waived by narrative.

**Bounded outcome:** one v2 bundle decides the universal four-row gate; it does
not change production behavior, the method, baseline, or historical evidence.
**Exact predecessors:** `12.3`.
**Frozen inventory:** `V9-12.4-ENTRY-v1` contains, in order,
`V8-6.11-AC6`, `V8-6.11-AC7`, `V8-6.11-AC9`, `V8-6.11-AC10`,
`V8-6.11-EVIDENCE-RAW-SAMPLES-ENVIRONMENT-CALCULATION-SIGNAL-IDENTITY`, and
`V8-6.11-COMPLETION-GATE-UNIVERSAL-SM-C2`; SHA-256
`676ba1d04025defd4d1494d78e5c6e7c4fcad695707ada896ebf3476dc5f3577`.
**Generated evidence:**
`docs/release-evidence/sm-c2-universal-gate-v2.schema.json`,
`docs/release-evidence/sm-c2-universal-gate-v2.json`, and deterministic
`docs/release-evidence/sm-c2-universal-gate-v2.md`; schema
`hexalith.conversations.sm-c2-universal-gate.v2` requires candidate, four rows,
all baseline/candidate raw samples, environment, binary/source/gitlink hashes,
P95 calculations, signal verdicts, threshold verdicts, correctness results,
overall result, tool versions, and digests. JSON is authoritative.
**Candidate binding:** `SC-12.4`, Story 12.3 record/proof, method/ADR, immutable
baseline, exact root/EventStore gitlink, benchmark binary/source and raw results.
**Rollback boundary:** remove only v2 run outputs, generator/validator, bundle,
fault fixtures, and Story 12.4 record; retain optimized source and all prior
story/historical evidence for separate review or Story 12.3 rollback.
**Generated final record:**
`docs/release-evidence/story-12.4-final-record-v2.json` and deterministic
`docs/release-evidence/story-12.4-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-12.4-01` | Built IntegrationTests at `SC-12.4`, frozen method, and controlled environment | `dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -class Hexalith.Conversations.IntegrationTests.Performance.SmC2HotPathBenchmark -parallel none -maxThreads 1 -showLiveOutput -reporter verbose -noColor -trx artifacts/v9/12.4/sm-c2-candidate.trx` | Exit `0`; exactly four nonempty raw-sample rows execute under the method. Missing/unusable sample is `SMC2_RAW_SAMPLE_MISSING`/`SMC2_SIGNAL_UNUSABLE`. |
| `AC-12.4-02` | Immutable baseline, candidate TRX, method, and Story 12.3 proof | `python3 _bmad/scripts/generate_sm_c2_universal_gate.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.4.json --baseline docs/release-evidence/sm-c2-hot-path-baseline-v1.json --candidate-result artifacts/v9/12.4/sm-c2-candidate.trx --correctness-proof docs/release-evidence/derived-key-correctness-proof-v1.json --output-json docs/release-evidence/sm-c2-universal-gate-v2.json --output-markdown docs/release-evidence/sm-c2-universal-gate-v2.md` | Exit `0` only if inputs are comparable and every row has usable signal; mismatch is `SMC2_ENVELOPE_MISMATCH`. |
| `AC-12.4-03` | Generated four-row bundle | `python3 _bmad/scripts/verify_sm_c2_universal_gate.py --repository . --evidence docs/release-evidence/sm-c2-universal-gate-v2.json --markdown docs/release-evidence/sm-c2-universal-gate-v2.md --output artifacts/v9/12.4/AC-12.4-03.json` | Exit `0`; each row independently satisfies `postP95 <= 1.05 * baselineP95`, all correctness lanes are green/nonvacuous, identities are current, and JSON/Markdown agree. A miss is `SMC2_ROW_REGRESSION`; red lane `CORRECTNESS_GATE_RED`; stale identity `SMC2_STALE_BINDING`. |
| `AC-12.4-04` | Four row IDs tested separately against the verified result | `python3 -m pytest -q _bmad/scripts/tests/test_sm_c2_universal_gate.py -k 'hp_create or hp_append or hp_list or hp_open' --junitxml=artifacts/v9/12.4/AC-12.4-04.xml` | Exit `0`; four independent assertions prove no cost/disclosure/approval/record-only substitute. |
| `AC-12.4-05` | Missing/adverse sample, unusable signal, environment drift, arithmetic/threshold change, stale candidate/binary/gitlink, red/skip/not-run/vacuous correctness, and JSON/Markdown drift | `python3 -m pytest -q _bmad/scripts/tests/test_sm_c2_universal_gate.py -k complete_fault_matrix --junitxml=artifacts/v9/12.4/AC-12.4-05.xml` | Exit `0`; every mutation yields its exact blocker and restores; undetected/unrestored is `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-12.4-06` | AC-12.4-01 through AC-12.4-05 and Story 12.3 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/12.4.json --format bundle --output-json docs/release-evidence/story-12.4-final-record-v2.json --output-markdown docs/release-evidence/story-12.4-final-record-v2.md` | Exit `0`; record binds all four raw/calculated/signal/verdict rows, method, correctness, identities, mutations, inventory, summary `6/6/0/0/0/0`; otherwise Story 12.4 remains incomplete. |

**Fault injection coverage:** every row, adverse/missing/unusable samples,
environment/comparability, arithmetic/threshold, stale identities, all
correctness result failures, prohibited substitute verdicts, and format drift
are mandatory and byte-restored.

## Epic 13: Current Projection-Proof Lifecycle

**Outcome:** Release owners receive immutable historical validation plus one
predecessor-bound current projection proof.
**Hard entry:** Epics 7 and 9, concretely Stories 7.4 and 9.2.
**Bounded exit:** Stories 13.1-13.3 are `done` at one compatible chain; v2 is
historical and v3 is the sole approved current head.
**V8 source owner:** superseded Story 6.12; its prepared story is provenance,
not accepted implementation.

### Story 13.1: Validate historical proof and approve predecessor-chain ADR/schema

As a release owner,
I want the completed projection proof checked at its recorded time basis,
so later valid work cannot falsify or silently replace history.

**Bounded outcome:** Checkpoint A validates v2 historically and approves ADR
0004 plus a closed chain schema; it creates no v3 current-head claim.
**Exact predecessors:** `7.4`, `9.2`.
**Frozen inventory:** `V9-13.1-ENTRY-v1` contains, in order,
`V8-6.12-AC1`, `V8-6.12-AC2`, `V8-6.12-AC3`,
`V8-6.12-CHECKPOINT-A`, `V8-6.12-PROHIBITION-NO-V2-MUTATION`, and
`V8-6.12-EVIDENCE-HISTORICAL-CANDIDATE-BLOBS`; SHA-256
`e050f1cba4f2dbcb83cfd40f6ae4f855c7458503415623b28cc390a0946220b6`.
**Protected history:** Story 6.2 record, v2 JSON/Markdown, its three xUnit
results, generated final record, signed-v1 dependencies, recorded root commit,
and recorded gitlinks are frozen by their existing hashes.
**ADR/schema:** `docs/adrs/0004-projection-proof-supersession-lifecycle.md`
and `docs/release-evidence/projection-proof-chain-v1.schema.json`.
**Generated evidence:**
`docs/release-evidence/projection-proof-history-validation-v1.schema.json`,
`docs/release-evidence/projection-proof-history-validation-v1.json`, and
deterministic
`docs/release-evidence/projection-proof-history-validation-v1.md`; schema
`hexalith.conversations.projection-proof-history-validation.v1`, with
candidate, recordedCandidate, protectedInventory, blob/hash/mode/run checks,
chainSchema, approval, result, blockers, and digests.
**Candidate binding:** `SC-13.1`, 7.4/9.2 records, recorded Story 6.2 root and
gitlinks, protected blob hashes, ADR/schema/approval digests.
**Rollback boundary:** remove only ADR/schema approval additions, historical
validator/results/generated validation and Story 13.1 record; mutate no v2 byte.
**Generated final record:**
`docs/release-evidence/story-13.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-13.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-13.1-01` | Story 6.2 record and protected-byte inventory | `python3 _bmad/scripts/verify_projection_proof_history.py --repository . --story-record docs/release-evidence/story-6.2-final-record-v2.json --proof docs/release-evidence/projection-read-store-population-proof-v2.json --format json --output artifacts/v9/13.1/AC-13.1-01.json` | Exit `0`; `PASS`; every root/submodule blob, hash, raw mode, gitlink, gate, and run identity resolves at the recorded candidate. Mutation/mismatch is `HISTORICAL_PROOF_MUTATED`/`HISTORICAL_IDENTITY_MISMATCH`; unavailable object returns `2` with `HISTORICAL_OBJECT_UNRESOLVABLE`. |
| `AC-13.1-02` | Valid history plus unrelated later root/gitlink movement | `python3 -m pytest -q _bmad/scripts/tests/test_projection_proof_history.py -k current_worktree_independence --junitxml=artifacts/v9/13.1/AC-13.1-02.xml` | Exit `0`; historical pass is unchanged and never compares v2 to current bytes. |
| `AC-13.1-03` | ADR 0004, full predecessor hashes, changed-dependency and owner/rationale rules | `python3 _bmad/scripts/verify_projection_proof_chain.py --repository . --adr docs/adrs/0004-projection-proof-supersession-lifecycle.md --schema docs/release-evidence/projection-proof-chain-v1.schema.json --no-current-head --output artifacts/v9/13.1/AC-13.1-03.json` | Exit `0`; immutable predecessor-linked lifecycle is closed and exactly-zero current head is required at Checkpoint A. Invalid contract is `PROOF_CHAIN_SCHEMA_INVALID`. |
| `AC-13.1-04` | ADR/schema signable payload and named owner disposition | `python3 _bmad/scripts/generate_projection_proof_history_validation.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.1.json --history-result artifacts/v9/13.1/AC-13.1-01.json --output-json docs/release-evidence/projection-proof-history-validation-v1.json --output-markdown docs/release-evidence/projection-proof-history-validation-v1.md` | Exit `0`; approved bundle binds exact digests; missing approval is `PROOF_LIFECYCLE_APPROVAL_MISSING`. |
| `AC-13.1-05` | Changed v2 byte, wrong root/gitlink/blob/mode/run identity, missing object, open schema, or approval removal | `python3 -m pytest -q _bmad/scripts/tests/test_projection_proof_history.py -k checkpoint_a_fault_matrix --junitxml=artifacts/v9/13.1/AC-13.1-05.xml` | Exit `0`; exact blockers occur and fixtures restore; otherwise `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-13.1-06` | AC-13.1-01 through AC-13.1-05 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.1.json --format bundle --output-json docs/release-evidence/story-13.1-final-record-v2.json --output-markdown docs/release-evidence/story-13.1-final-record-v2.md` | Exit `0`; record binds historical validation, ADR/schema/approval, protected inventory and mutation ledger, summary `6/6/0/0/0/0`. |

### Story 13.2: Generate the current successor proof and enforce drift/current-head guards

As a release owner,
I want current assurance represented by one explicit v3 successor,
so v2 alone cannot be mistaken for current readiness.

**Bounded outcome:** Checkpoint B generates and approves one v3 current head
and its dependency guard; closure fault/handoff work remains outside scope.
**Exact predecessors:** `13.1`.
**Frozen inventory:** `V9-13.2-ENTRY-v1` contains, in order,
`V8-6.12-AC4`, `V8-6.12-AC5`, `V8-6.12-CHECKPOINT-B`,
`V8-6.12-PROHIBITION-V2-ALONE-NOT-CURRENT`, and
`V8-6.12-ROLLBACK-DISCARD-V3-PRESERVE-V2`; SHA-256
`864d1caf3c429ecea78b7143f8bb63cc46a727ff1e9dd35dd0613e28a4b37067`.
**Generated evidence:**
`docs/release-evidence/projection-read-store-population-proof-v3.schema.json`,
`docs/release-evidence/projection-read-store-population-proof-v3.json`, and
deterministic
`docs/release-evidence/projection-read-store-population-proof-v3.md`; schema
`hexalith.conversations.projection-read-store-population-proof.v3`, plus exact
result files `dispatch.trx`, `gateway-dapr.trx`, `state-store.trx`,
`production-query.trx`, `deletion.trx`, and `replay.trx`. Required proof fields
include candidate, predecessor path/hash, changedDependencies, approval,
currentHead, six lanes, public/test identities, and digests.
**Candidate binding:** `SC-13.2`, Story 13.1 record/history/chain digests, v2
predecessor, root and relevant gitlinks, six binary/result identities.
**Rollback boundary:** discard only v3, six new result files, guard/tests and
Story 13.2 record; retain v2 and Story 13.1 byte-identically.
**Generated final record:**
`docs/release-evidence/story-13.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-13.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-13.2-01` | Built current proof lanes at `SC-13.2` | `dotnet tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests.dll -automated sync -failSkips -class Hexalith.Conversations.IntegrationTests.Projections.ProjectionReadStorePopulationProofV3Tests -trx artifacts/v9/13.2/projection-v3.trx` | Exit `0`; nonzero deterministic dispatch, gateway/Dapr, state-store, production-query, deletion and replay assertions all pass. Missing/red lane is `PROJECTION_PROOF_LANE_RED`. |
| `AC-13.2-02` | v2 predecessor, declared changed dependencies, approval, and current candidate | `python3 _bmad/scripts/generate_projection_proof_v3.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.2.json --predecessor docs/release-evidence/projection-read-store-population-proof-v2.json --result artifacts/v9/13.2/projection-v3.trx --output-json docs/release-evidence/projection-read-store-population-proof-v3.json --output-markdown docs/release-evidence/projection-read-store-population-proof-v3.md` | Exit `0`; deterministic v3 records full predecessor hash, exact changed dependencies, owner/rationale, six lanes, and candidate identities. Bad link is `PROJECTION_PROOF_PREDECESSOR_INVALID`. |
| `AC-13.2-03` | Chain schema, v2 and generated v3 | `python3 _bmad/scripts/verify_projection_proof_chain.py --repository . --adr docs/adrs/0004-projection-proof-supersession-lifecycle.md --schema docs/release-evidence/projection-proof-chain-v1.schema.json --current docs/release-evidence/projection-read-store-population-proof-v3.json --output artifacts/v9/13.2/AC-13.2-03.json` | Exit `0`; exactly one approved current head exists. Zero/duplicate/fork is `PROJECTION_PROOF_HEAD_INVALID`; missing approval is `PROJECTION_PROOF_APPROVAL_MISSING`. |
| `AC-13.2-04` | Declared proof dependencies at predecessor and `SC-13.2` | `python3 _bmad/scripts/verify_projection_proof_current.py --repository . --proof docs/release-evidence/projection-read-store-population-proof-v3.json --candidate HEAD --output artifacts/v9/13.2/AC-13.2-04.json` | Exit `0`; hashes/runs are fresh; undeclared in-scope drift is `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; stale declared state is `PROJECTION_PROOF_STALE`; unrelated gitlink movement is ignored. |
| `AC-13.2-05` | Missing/red/skipped/vacuous lane, broken predecessor, duplicate/fork head, undeclared drift, stale proof, and unrelated-gitlink control | `python3 -m pytest -q _bmad/scripts/tests/test_projection_proof_v3.py -k checkpoint_b_fault_matrix --junitxml=artifacts/v9/13.2/AC-13.2-05.xml` | Exit `0`; exact blockers occur, control passes, fixtures restore. |
| `AC-13.2-06` | Rollback rehearsal from v3 to Checkpoint A | `python3 -m pytest -q _bmad/scripts/tests/test_projection_proof_v3.py -k rollback_preserves_v2 --junitxml=artifacts/v9/13.2/AC-13.2-06.xml` | Exit `0`; v3 outputs disappear and every protected v2 hash is unchanged. |
| `AC-13.2-07` | AC-13.2-01 through AC-13.2-06 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.2.json --format bundle --output-json docs/release-evidence/story-13.2-final-record-v2.json --output-markdown docs/release-evidence/story-13.2-final-record-v2.md` | Exit `0`; record binds predecessor/current chain, six lanes, guard, approval, rollback and inventory, summary `7/7/0/0/0/0`. |

### Story 13.3: Prove fault injection and bind manifest, conformance, handoff, and final record

As a release owner,
I want the complete proof chain closed mechanically,
so downstream preservation and attestation consume explicit current assurance.

**Bounded outcome:** Checkpoint C closes mutation, conformance, manifest and
handoff evidence; it makes no release-readiness decision.
**Exact predecessors:** `13.2`.
**Frozen inventory:** `V9-13.3-ENTRY-v1` contains, in order,
`V8-6.12-AC6`, `V8-6.12-AC7`, `V8-6.12-AC8`,
`V8-6.12-CHECKPOINT-C`, `V8-6.12-COMPLETION-GATE-ALL-EIGHT-SAME-CANDIDATE`,
`V8-6.12-DEPENDENCY-6.8`, and `V8-6.12-HANDOFF-6.3-6.6`; SHA-256
`bf16171b2a6b2dd4870c11b2a160d2c8090efa3e42b2eca7e5aae900a0fb7666`.
**Generated evidence:**
`docs/release-evidence/projection-proof-closure-v1.schema.json`,
`docs/release-evidence/projection-proof-closure-v1.json`, and deterministic
`docs/release-evidence/projection-proof-closure-v1.md`; schema
`hexalith.conversations.projection-proof-closure.v1` requires candidate,
v2History, v3Current, mutationLedger, focused/manifest/portable/internal lane
results, handoffs, assertion counts, and digests.
**Candidate binding:** `SC-13.3`, full 13.1-13.2 chain, exact tier binaries and
results, manifest/current-proof and mutation digests.
**Rollback boundary:** remove only closure generator/bundle, fault fixtures,
handoff declarations/results and Story 13.3 record; retain approved v3 and all
history.
**Generated final record:**
`docs/release-evidence/story-13.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-13.3-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-13.3-01` | Mutations of v2 byte/identity, predecessor hash, head count/fork, v3 freshness, lane result, and dependency declaration | `python3 -m pytest -q _bmad/scripts/tests/test_projection_proof_closure.py -k complete_mutation_matrix --junitxml=artifacts/v9/13.3/AC-13.3-01.xml` | Exit `0`; each yields `HISTORICAL_PROOF_MUTATED`, `HISTORICAL_IDENTITY_MISMATCH`, `PROJECTION_PROOF_PREDECESSOR_INVALID`, `PROJECTION_PROOF_CHAIN_FORKED`, `PROJECTION_PROOF_STALE`, `PROJECTION_PROOF_RUN_INVALID`, or `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; fixtures restore. |
| `AC-13.3-02` | Built focused current-proof lane | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest -trx artifacts/v9/13.3/focused.trx` | Exit `0`; nonzero current v3 and historical v2 assertions pass. |
| `AC-13.3-03` | Built portable and internal conformance tiers | `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -trx artifacts/v9/13.3/portable.trx` | Exit `0`; portable tier nonzero/all green. |
| `AC-13.3-04` | Built internal conformance tier | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -trx artifacts/v9/13.3/internal.trx` | Exit `0`; internal tier nonzero/all green. |
| `AC-13.3-05` | v2 history, v3 current, mutation and three lane results, plus Epic 14/15 handoff IDs | `python3 _bmad/scripts/generate_projection_proof_closure.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.3.json --history docs/release-evidence/projection-proof-history-validation-v1.json --current docs/release-evidence/projection-read-store-population-proof-v3.json --output-json docs/release-evidence/projection-proof-closure-v1.json --output-markdown docs/release-evidence/projection-proof-closure-v1.md` | Exit `0`; handoffs bind v2 as history and v3 as current; v2 alone cannot satisfy them. Missing handoff is `PROJECTION_PROOF_HANDOFF_MISSING`; mixed chain is `PROJECTION_PROOF_CANDIDATE_MISMATCH`. |
| `AC-13.3-06` | AC-13.3-01 through AC-13.3-05 and all three checkpoints are compatible | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/13.3.json --format bundle --output-json docs/release-evidence/story-13.3-final-record-v2.json --output-markdown docs/release-evidence/story-13.3-final-record-v2.md` | Exit `0`; final record binds all eight v8 criteria, three checkpoints, proof chain, lane counts, handoffs, inventories/mutations, summary `6/6/0/0/0/0`. |

**Fault injection coverage:** changed history, wrong identities, broken/forked
chain, stale successor, undeclared drift, every invalid lane state, missing
handoff, candidate mismatch, and non-restoration are mandatory.

## Epic 14: Complete Preservation Manifest

**Outcome:** Release owners receive one zero-gap, candidate-bound preservation
manifest across every requirement, contract, test, UX and evidence obligation.
**Hard entry:** Epics 8, 9, 10 and 13, concretely Stories 8.2, 9.2, 10.4 and
13.3.
**Bounded exit:** Stories 14.1-14.3 are `done` at one compatible candidate and
the generated v3 manifest has no missing, duplicate, orphaned or stale binding.
**V8 source owner:** superseded Story 6.3; its partial work and v2 manifest are
unaccepted provenance and cannot satisfy a v9 acceptance scenario.

### Story 14.1: Freeze requirement, contract, test, UX and evidence denominators

As a release owner,
I want every preservation denominator frozen before dispositions are assigned,
so denominator drift cannot manufacture completeness.

**Bounded outcome:** one generated denominator inventory freezes exact
identities and source hashes; it assigns no obligation disposition.
**Exact predecessors:** `8.2`, `9.2`, `10.4`, `13.3`.
**Frozen inventory:** `V9-14.1-ENTRY-v1` contains, in order,
`V8-6.3-AC1`, `V8-6.3-PROHIBITION-DENOMINATOR-DRIFT`,
`V8-6.3-PROHIBITION-FR16-ACTIVATION`, and
`V8-6.3-PARTIAL-WORK-UNACCEPTED-INPUT`; SHA-256
`6a6101ccdd2e4fea72fb6d7004dc649d18a418fdbae8884a6d4da3af7f3d4cdb`.
**Exact denominators:** 20 `FR-*`, 104 `Feature-FR*`, 77 `Feature-NFR*`, 52
`UX-DR*`, 28 UX AC identities, plus exact public-contract, current-control,
portable/internal assertion, 27-reader evidence and projection-proof-chain
inventories derived from their accepted predecessor records. Functional
coverage denominator is exactly `124`; FR-16 alone is deferred/non-activated.
**Generated evidence:**
`docs/release-evidence/preservation-denominator-inventory-v3.schema.json`,
`docs/release-evidence/preservation-denominator-inventory-v3.json`, and
deterministic
`docs/release-evidence/preservation-denominator-inventory-v3.md`; schema
`hexalith.conversations.preservation-denominator-inventory.v3` requires
candidate, sourceAuthorities, seven named denominator groups, identity/path/
sourceHash rows, predecessor inventory digests, counts, deferred identities,
result, blockers and digests.
**Candidate binding:** `SC-14.1`, four predecessor records and their frozen
inventories, exact authority/source blobs and relevant gitlinks.
**Rollback boundary:** remove only v3 denominator generator/validator/bundle,
fault fixtures/results and Story 14.1 record; preserve all sources and v2 files.
**Generated final record:**
`docs/release-evidence/story-14.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-14.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-14.1-01` | Canonical PRD/addendum and bound `SC-14.1` | `python3 _bmad/scripts/generate_preservation_denominators.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.1.json --output-json docs/release-evidence/preservation-denominator-inventory-v3.json --output-markdown docs/release-evidence/preservation-denominator-inventory-v3.md` | Exit `0`; exactly 20 initiative plus 104 Feature functional identities are unique and total `124`. Missing/extra/duplicate is `PRESERVATION_IDENTITY_MISSING`, `PRESERVATION_DENOMINATOR_DRIFT`, or `PRESERVATION_IDENTITY_DUPLICATE`. |
| `AC-14.1-02` | Generated denominator inventory | `python3 _bmad/scripts/verify_preservation_denominators.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --group feature-nfr --expected 77 --output artifacts/v9/14.1/AC-14.1-02.json` | Exit `0`; exact `77/77` unique Feature-NFRs resolve to pinned source hashes. |
| `AC-14.1-03` | Story 8.2 UX record/inventories | `python3 _bmad/scripts/verify_preservation_denominators.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --group ux --expected-decisions 52 --expected-acceptance 28 --output artifacts/v9/14.1/AC-14.1-03.json` | Exit `0`; exact `52/52` and `28/28`, with preserved-not-activated state. Drift or activation is `PRESERVATION_DENOMINATOR_DRIFT` or `UX_SCOPE_ACTIVATED`. |
| `AC-14.1-04` | Stories 9.2, 10.4 and 13.3 inventories | `python3 _bmad/scripts/verify_preservation_denominators.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --group contracts-controls-tests-evidence-proofs --output artifacts/v9/14.1/AC-14.1-04.json` | Exit `0`; public-contract/control/assertion/reader/proof sets and predecessor digests match exactly. Drift is `PRESERVATION_DENOMINATOR_DRIFT`. |
| `AC-14.1-05` | FR-16 and every other initiative identity | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationDenominatorV3ValidationTest.OnlyFr16ShouldBeDeferredAndNonActivated -trx artifacts/v9/14.1/AC-14.1-05.trx` | Exit `0`; only FR-16 has deferred/non-activated classification. Violation is `FR16_ACTIVATED` or `UNAUTHORIZED_REQUIREMENT_DEFERRED`. |
| `AC-14.1-06` | Existing partial v2 manifest and v3 inventory | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationDenominatorV3ValidationTest.PartialV2ShouldRemainUnacceptedProvenance -trx artifacts/v9/14.1/AC-14.1-06.trx` | Exit `0`; v2 hashes are recorded only as provenance and never as a completed input. False inheritance is `PARTIAL_MANIFEST_TREATED_AS_ACCEPTED`. |
| `AC-14.1-07` | Missing/extra/duplicate/orphan identity, count drift, source-hash drift, FR-16 activation, other deferral, UX activation and accepted-v2 mutations | `python3 -m pytest -q _bmad/scripts/tests/test_preservation_denominators.py -k complete_fault_matrix --junitxml=artifacts/v9/14.1/AC-14.1-07.xml` | Exit `0`; each exact blocker occurs and every fixture restores; otherwise `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-14.1-08` | AC-14.1-01 through AC-14.1-07 and four predecessor records are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.1.json --format bundle --output-json docs/release-evidence/story-14.1-final-record-v2.json --output-markdown docs/release-evidence/story-14.1-final-record-v2.md` | Exit `0`; final record binds every group count/digest, sources, predecessors, mutation ledger, summary `8/8/0/0/0/0`. |

### Story 14.2: Bind dispositions, approvals, evidence, tiers, proof chains and candidate identity

As a release owner,
I want one evidence-bearing disposition for every frozen identity,
so delivered, inactive, compatible and non-activated claims are reviewable.

**Bounded outcome:** one generated binding map assigns exactly one disposition
per Story 14.1 identity; it does not issue the final zero-gap verdict.
**Exact predecessors:** `14.1`.
**Frozen inventory:** `V9-14.2-ENTRY-v1` contains, in order,
`V8-6.3-AC2`, `V8-6.3-AC3`, `V8-6.3-AC4`, `V8-6.3-AC5`,
`V8-6.3-EVIDENCE-APPROVAL-COMPATIBILITY-HASH-TIER-PROOF`, and
`V8-6.3-PROHIBITION-HISTORY-AS-CURRENT`; SHA-256
`351a30fdc23b8951a19ff94d903b0e5ab5b25903fb47b43bfa079914c2b698c7`.
**Generated evidence:**
`docs/release-evidence/preservation-binding-map-v3.schema.json`,
`docs/release-evidence/preservation-binding-map-v3.json`, and deterministic
`docs/release-evidence/preservation-binding-map-v3.md`; schema
`hexalith.conversations.preservation-binding-map.v3` requires for
each frozen identity exactly one disposition, owner, rationale, candidate,
evidence path/hash/role, source/build/test/baseline hashes, tier/control owner,
approval/compatibility fields when applicable, proof-chain role and digests.
**Disposition enum:** `delivered-active`, `delivered-inactive-approved`,
`compatible-change-approved`, `preserved-not-activated`,
`deferred-non-activated`, or `immutable-history`; no null/catch-all value.
**Candidate binding:** `SC-14.2`, Story 14.1 inventory/record, 8.2/9.2/10.4/
13.3 evidence digests and exact referenced root/gitlink blobs.
**Rollback boundary:** remove only binding generator/validator/map, fixtures,
results and Story 14.2 record; retain frozen denominator and all source evidence.
**Generated final record:**
`docs/release-evidence/story-14.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-14.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-14.2-01` | Story 14.1 inventory and candidate-bound evidence registry | `python3 _bmad/scripts/generate_preservation_bindings.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.2.json --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --output-json docs/release-evidence/preservation-binding-map-v3.json --output-markdown docs/release-evidence/preservation-binding-map-v3.md` | Exit `0`; one and only one enumerated disposition exists per frozen identity. Missing/duplicate is `PRESERVATION_DISPOSITION_MISSING`/`PRESERVATION_DISPOSITION_DUPLICATE`. |
| `AC-14.2-02` | Every binding and its evidence path/hash/role | `python3 _bmad/scripts/verify_preservation_bindings.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --bindings docs/release-evidence/preservation-binding-map-v3.json --check evidence --output artifacts/v9/14.2/AC-14.2-02.json` | Exit `0`; referenced evidence is contained, current for its declared time basis, hash-valid and nonvacuous. Failure is `PRESERVATION_EVIDENCE_INVALID`. |
| `AC-14.2-03` | Delivered-inactive and compatible-change rows | `python3 _bmad/scripts/verify_preservation_bindings.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --bindings docs/release-evidence/preservation-binding-map-v3.json --check approvals-compatibility --output artifacts/v9/14.2/AC-14.2-03.json` | Exit `0`; each names owner, rationale, UTC decision, signable digest and compatibility evidence. Missing data is `PRESERVATION_APPROVAL_MISSING` or `PRESERVATION_COMPATIBILITY_MISSING`. |
| `AC-14.2-04` | Conformance assertions and Story 9.2 tier records | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationBindingMapV3ValidationTest.AssertionsShouldBindApprovedTierAndStrength -trx artifacts/v9/14.2/AC-14.2-04.trx` | Exit `0`; every assertion tier and strength digest matches 9.2. Violation is `PRESERVATION_TIER_MISMATCH` or `ASSERTION_STRENGTH_WEAKENED`. |
| `AC-14.2-05` | Projection v2 history, v3 current head and complete predecessor chain | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationBindingMapV3ValidationTest.ProjectionRolesShouldDistinguishHistoryAndCurrent -trx artifacts/v9/14.2/AC-14.2-05.trx` | Exit `0`; v2 is immutable history and exactly one v3 is current. Substitution is `HISTORICAL_PROOF_USED_AS_CURRENT`; broken chain is `PROJECTION_PROOF_ROLE_INVALID`. |
| `AC-14.2-06` | Module/platform controls, source/build/test/baseline hashes and Story 10 boundary result | `python3 _bmad/scripts/verify_preservation_bindings.py --repository . --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --bindings docs/release-evidence/preservation-binding-map-v3.json --check controls-hashes-candidate --output artifacts/v9/14.2/AC-14.2-06.json` | Exit `0`; ownership is separated and all identities bind one compatible chain. Failure is `PRESERVATION_CONTROL_OWNER_INVALID`, `PRESERVATION_HASH_INVALID`, or `PRESERVATION_CANDIDATE_MISMATCH`. |
| `AC-14.2-07` | Missing/duplicate disposition, bad/stale/vacuous evidence, missing approval/compatibility, tier/strength drift, history-current swap, owner/hash/candidate fault | `python3 -m pytest -q _bmad/scripts/tests/test_preservation_bindings.py -k complete_fault_matrix --junitxml=artifacts/v9/14.2/AC-14.2-07.xml` | Exit `0`; each exact blocker occurs and fixtures restore byte-identically. |
| `AC-14.2-08` | AC-14.2-01 through AC-14.2-07 and Story 14.1 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.2.json --format bundle --output-json docs/release-evidence/story-14.2-final-record-v2.json --output-markdown docs/release-evidence/story-14.2-final-record-v2.md` | Exit `0`; record binds map, inventory, evidence/approval/tier/proof/control digests, mutations, summary `8/8/0/0/0/0`. |

### Story 14.3: Run zero-gap validation and generate the manifest final record

As a release owner,
I want the complete binding set validated and published mechanically,
so preservation completeness is a reproducible fact.

**Bounded outcome:** one current v3 manifest and validation result are
generated from Stories 14.1-14.2; no product or release decision is made.
**Exact predecessors:** `14.2`.
**Frozen inventory:** `V9-14.3-ENTRY-v1` contains, in order,
`V8-6.3-AC6`, `V8-6.3-DEPENDENCY-6.8`, `V8-6.3-DEPENDENCY-6.9`,
`V8-6.3-DEPENDENCY-6.10`, `V8-6.3-DEPENDENCY-6.12`,
`V8-6.3-COMPLETION-GATE-SAME-CANDIDATE`,
`V8-6.3-ROLLBACK-VERSIONED-MANIFEST`, and
`V8-6.3-PROHIBITION-UI-ACTIVATION`; SHA-256
`96fa64e4dda2a3fe0775ff96306562c290bbbd002092bbdd785ebb7400465a17`.
**Generated evidence:**
`docs/release-evidence/preservation-traceability-manifest-v3.schema.json`,
`docs/release-evidence/preservation-traceability-manifest-v3.json`, and
deterministic
`docs/release-evidence/preservation-traceability-manifest-v3.md`, plus
`docs/release-evidence/preservation-manifest-validation-v3.schema.json` and
`docs/release-evidence/preservation-manifest-validation-v3.json`; manifest schema
`hexalith.conversations.preservation-traceability-manifest.v3` embeds exact
denominator/binding digests and rows; validation schema requires per-group
expected/actual/missing/duplicate/orphan/stale counts, candidate, result,
blockers and digests. JSON is authoritative and Markdown deterministic.
**Candidate binding:** `SC-14.3`, Stories 14.1-14.2 and predecessor records,
all referenced source/evidence/test binaries/results and relevant gitlinks.
**Rollback boundary:** remove only v3 manifest/validation generators and
outputs, faults/results and Story 14.3 record; v2 and Stories 14.1-14.2 remain.
**Generated final record:**
`docs/release-evidence/story-14.3-final-record-v2.json` and deterministic
`docs/release-evidence/story-14.3-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-14.3-01` | v3 denominator and binding bundles at compatible candidates | `python3 _bmad/scripts/generate_preservation_manifest_v3.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.3.json --inventory docs/release-evidence/preservation-denominator-inventory-v3.json --bindings docs/release-evidence/preservation-binding-map-v3.json --output-json docs/release-evidence/preservation-traceability-manifest-v3.json --output-markdown docs/release-evidence/preservation-traceability-manifest-v3.md` | Exit `0`; generated rows are derived, ordinal and schema-valid; mismatch is `PRESERVATION_CANDIDATE_MISMATCH`. |
| `AC-14.3-02` | Generated manifest | `python3 _bmad/scripts/verify_preservation_manifest_v3.py --repository . --manifest docs/release-evidence/preservation-traceability-manifest-v3.json --markdown docs/release-evidence/preservation-traceability-manifest-v3.md --format json --output docs/release-evidence/preservation-manifest-validation-v3.json` | Exit `0`; `124/124`, `77/77`, `52/52`, `28/28`, and all control/contract/test/evidence/proof identities have zero missing/duplicate/orphan/stale. Failure is `PRESERVATION_ZERO_GAP_FAILED`; format drift `EVIDENCE_FORMAT_DRIFT`. |
| `AC-14.3-03` | FR-16 and UX rows | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestV3ValidationTest.DeferredAndUxStateShouldRemainNonActivated -trx artifacts/v9/14.3/AC-14.3-03.trx` | Exit `0`; FR-16 is sole deferred identity and UX is preserved-not-activated. Violation is `FR16_ACTIVATED` or `UX_SCOPE_ACTIVATED`. |
| `AC-14.3-04` | Tier, evidence-boundary and projection-chain rows | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestV3ValidationTest.CurrentAssuranceBindingsShouldBeValid -trx artifacts/v9/14.3/AC-14.3-04.trx` | Exit `0`; tiers/strength, boundary evidence and v2-history/v3-current roles match predecessors. Failure is `PRESERVATION_TIER_MISMATCH`, `EVIDENCE_BOUNDARY_INVALID`, or `PROJECTION_PROOF_ROLE_INVALID`. |
| `AC-14.3-05` | Missing/duplicate/orphan/stale binding, count drift, candidate mix, FR/UX activation, tier/boundary/proof fault and JSON/Markdown drift | `python3 -m pytest -q _bmad/scripts/tests/test_preservation_manifest_v3.py -k complete_fault_matrix --junitxml=artifacts/v9/14.3/AC-14.3-05.xml` | Exit `0`; exact blockers occur and fixtures restore; otherwise `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-14.3-06` | AC-14.3-01 through AC-14.3-05 and the complete predecessor chain are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/14.3.json --format bundle --output-json docs/release-evidence/story-14.3-final-record-v2.json --output-markdown docs/release-evidence/story-14.3-final-record-v2.md` | Exit `0`; record binds manifest/validation, every denominator and disposition count/digest, predecessor chain, mutations, summary `6/6/0/0/0/0`. |

**Fault injection coverage:** every denominator group, missing/duplicate/orphan/
stale mapping, evidence/approval/compatibility/tier/strength/control/proof role,
candidate mix, FR-16 or UX activation and format drift are mandatory and
byte-restored.

## Epic 15: Superseding Release Attestation

**Outcome:** Release owners receive bounded revalidation evidence and a
signable additive attestation without a prescribed release verdict.
**Hard entry:** Epics 7-14 at their exact bounded exits.
**Bounded exit:** Stories 15.1-15.2 are `done` at one release candidate; only
the separate non-story Gate RG-15 may decide release closure.
**V8 source owner:** executable portions of Story 6.6; external assessment and
release-decision semantics move exclusively to RG-15.

### Story 15.1: Revalidate all preservation, topology, correctness, and metric gates

As a release owner,
I want every completed predecessor gate rerun at one candidate,
so the attestation consumes current measured facts.

**Bounded outcome:** one revalidation bundle records all current gate results;
it creates neither an attestation nor a readiness/release decision.
**Exact predecessors:** `7.4`, `8.2`, `9.2`, `10.4`, `11.3`, `12.4`, `13.3`,
`14.3`.
**Frozen inventory:** `V9-15.1-ENTRY-v1` contains, in order,
`V8-6.6-AC1`, `V8-6.6-AC2`, `V8-6.6-AC4`,
`V8-6.6-DEPENDENCY-6.3`, `V8-6.6-DEPENDENCY-6.4`,
`V8-6.6-DEPENDENCY-6.5`, `V8-6.6-DEPENDENCY-6.8`,
`V8-6.6-DEPENDENCY-6.9`, `V8-6.6-DEPENDENCY-6.10`,
`V8-6.6-DEPENDENCY-6.11`, `V8-6.6-DEPENDENCY-6.12`, and
`V8-6.6-COMPLETION-GATE-LAST`; SHA-256
`996e98307e2db67aa3801dc401046d3d6216dc25c4ea3e9664ad5373dca55cd1`.
**Generated evidence:**
`docs/release-evidence/release-gate-revalidation-v2.schema.json`,
`docs/release-evidence/release-gate-revalidation-v2.json`, and deterministic
`docs/release-evidence/release-gate-revalidation-v2.md`; schema
`hexalith.conversations.release-gate-revalidation.v2` requires `RC`,
all predecessor record digests, four SM-C2 rows, manifest/public-contract/
topology/security/health/publication/admin/SM-1/SM-2/SM-3/preservation results,
both conformance tier results and sum, evidence-boundary result, v2 history,
v3 current head and rerun lanes, blockers and digests.
**Candidate binding:** `RC-15.1` is exact root commit plus canonical ten root
gitlinks, all eight predecessor final-record digests, test binary/source/result
hashes and frozen inventories. It is not `PC` or any prior `SC`.
**Rollback boundary:** remove only Story 15.1 rerun results, generator/
validator/bundle, faults and record; every predecessor remains immutable.
**Generated final record:**
`docs/release-evidence/story-15.1-final-record-v2.json` and deterministic
`docs/release-evidence/story-15.1-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-15.1-01` | Eight exact predecessor records and `RC-15.1` | `python3 _bmad/scripts/verify_release_predecessors.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --contract _bmad-output/planning-artifacts/v9/story-contracts/15.1.json --candidate HEAD --output artifacts/v9/15.1/AC-15.1-01.json` | Exit `0`; every record is schema-valid, current for its time basis, nonvacuous and candidate-compatible. Failure is `RELEASE_GATE_PREDECESSOR_INVALID` or `RELEASE_GATE_CANDIDATE_MISMATCH`. |
| `AC-15.1-02` | Story 12.4 universal SM-C2 evidence and benchmark identities | `python3 _bmad/scripts/verify_sm_c2_universal_gate.py --repository . --evidence docs/release-evidence/sm-c2-universal-gate-v2.json --markdown docs/release-evidence/sm-c2-universal-gate-v2.md --output artifacts/v9/15.1/AC-15.1-02.json` | Exit `0`; HP-CREATE/APPEND/LIST/OPEN each has usable comparable signal and satisfies `postP95 <= 1.05 * baselineP95`. Any row is `SMC2_ROW_REGRESSION` or `SMC2_SIGNAL_UNUSABLE`. |
| `AC-15.1-03` | Story 14.3 manifest and public-contract/approval bindings | `python3 _bmad/scripts/verify_preservation_manifest_v3.py --repository . --manifest docs/release-evidence/preservation-traceability-manifest-v3.json --markdown docs/release-evidence/preservation-traceability-manifest-v3.md --format json --output artifacts/v9/15.1/AC-15.1-03.json` | Exit `0`; complete manifest passes and every contract is equal or has approved compatibility evidence. Failure is `RELEASE_GATE_REVALIDATION_RED`. |
| `AC-15.1-04` | Topology, tenant security, health, publication, admin, SM-1/2/3 and preservation test inventories | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.ReleaseGateRevalidationV2Tests -trx artifacts/v9/15.1/revalidation.trx` | Exit `0`; every frozen lane executes nonzero with zero failed/skipped/not-run. Red/vacuous is `RELEASE_GATE_REVALIDATION_RED`/`RELEASE_GATE_RESULT_VACUOUS`. |
| `AC-15.1-05` | Built portable conformance tier | `dotnet tests/Hexalith.Conversations.Conformance.Portable.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Portable.Tests.dll -automated sync -failSkips -trx artifacts/v9/15.1/portable.trx` | Exit `0`; nonzero portable assertions all pass and retain tier digest. |
| `AC-15.1-06` | Built internal conformance tier | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -trx artifacts/v9/15.1/internal.trx` | Exit `0`; nonzero internal assertions all pass; sum with portable is monotonic. |
| `AC-15.1-07` | Story 10.4 boundary result and current release evidence diff | `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --candidate HEAD --format json --output artifacts/v9/15.1/AC-15.1-07.json` | Exit `0`; applicable evidence is current/nonvacuous/green. Failure is `EVIDENCE_BOUNDARY_INVALID`. |
| `AC-15.1-08` | Immutable v2 history and sole v3 head | `python3 _bmad/scripts/verify_projection_proof_current.py --repository . --proof docs/release-evidence/projection-read-store-population-proof-v3.json --candidate HEAD --rerun --output artifacts/v9/15.1/AC-15.1-08.json` | Exit `0`; history validates, one current head resolves and all six functional lanes rerun green. Drift is `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; invalid rerun `PROJECTION_PROOF_RUN_INVALID`. |
| `AC-15.1-09` | All results from AC-15.1-01 through AC-15.1-08 | `python3 _bmad/scripts/generate_release_revalidation.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/15.1.json --results artifacts/v9/15.1 --output-json docs/release-evidence/release-gate-revalidation-v2.json --output-markdown docs/release-evidence/release-gate-revalidation-v2.md` | Exit `0`; authoritative bundle recomputes separate/summed counts, identities and digests; any red/blocked/stale/vacuous input is `RELEASE_GATE_REVALIDATION_RED`. |
| `AC-15.1-10` | Mutations across every predecessor, row, manifest/contract, lane, tier, boundary, proof head/rerun and candidate/gitlink | `python3 -m pytest -q _bmad/scripts/tests/test_release_revalidation.py -k complete_fault_matrix --junitxml=artifacts/v9/15.1/AC-15.1-10.xml` | Exit `0`; exact owning blocker occurs and fixtures restore; otherwise `FAULT_NOT_DETECTED`/`FIXTURE_NOT_RESTORED`. |
| `AC-15.1-11` | AC-15.1-01 through AC-15.1-10 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/15.1.json --format bundle --output-json docs/release-evidence/story-15.1-final-record-v2.json --output-markdown docs/release-evidence/story-15.1-final-record-v2.md` | Exit `0`; final record binds RC, eight predecessors, every lane/count/digest, mutations and summary `11/11/0/0/0/0`. |

### Story 15.2: Generate the superseding attestation and predecessor-supersession record

As a release owner,
I want a signable additive attestation over the revalidated candidate,
so the independent release gate receives a complete immutable input.

**Bounded outcome:** one attestation and one supersession bundle are generated;
the story makes no readiness or release-closure decision.
**Exact predecessors:** `15.1`.
**Frozen inventory:** `V9-15.2-ENTRY-v1` contains, in order,
`V8-6.6-AC3`, `V8-6.6-EVIDENCE-SIGNED-V1-ADR3-PROOF-CHAIN-RERUN`,
`V8-6.6-PROHIBITION-NO-PREDECESSOR-REWRITE`, and
`V8-6.6-ROLLBACK-ADDITIVE-ATTESTATION`; SHA-256
`db7bafbf2a36d673a6ca9c331f2fdb3f679cd22f9b5f386206412f3fb82a2318`.
**Generated evidence:**
`docs/release-evidence/release-attestation-v2.schema.json`,
`docs/release-evidence/release-attestation-v2.json`, and deterministic
`docs/release-evidence/release-attestation-v2.md`, plus
`docs/release-evidence/release-attestation-supersession-v2.schema.json`,
`docs/release-evidence/release-attestation-supersession-v2.json`, and
deterministic
`docs/release-evidence/release-attestation-supersession-v2.md`; schemas
`hexalith.conversations.release-attestation.v2` and
`hexalith.conversations.release-attestation-supersession.v2`. They require RC,
authority bundle, revalidation and every predecessor digest, signed-v1/ADR0003/
v2/v3/rerun bindings, complete supersession edges, signable payload and no
decision field.
**Candidate binding:** `RC` is exact final root and ten gitlinks plus Story 15.1
record/revalidation, all predecessor records/evidence and test identities.
**Rollback boundary:** remove only the additive v2 attestation/supersession
generator/validator/bundles, faults/results and Story 15.2 record; rewrite no
predecessor, signed evidence, baseline, ADR or proof.
**Generated final record:**
`docs/release-evidence/story-15.2-final-record-v2.json` and deterministic
`docs/release-evidence/story-15.2-final-record-v2.md`.

**Acceptance Criteria:**

| ID | Given | When — exact command from repository root | Then |
| --- | --- | --- | --- |
| `AC-15.2-01` | Story 15.1 revalidation, signed-v1 evidence, ADR 0003, v2 history, v3 head and rerun result | `python3 _bmad/scripts/generate_release_attestation_v2.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/15.2.json --revalidation docs/release-evidence/release-gate-revalidation-v2.json --output-json docs/release-evidence/release-attestation-v2.json --output-markdown docs/release-evidence/release-attestation-v2.md` | Exit `0`; additive attestation binds exact RC/root/gitlinks and all required immutable/current identities. Invalid head/rerun is `ATTESTATION_PROOF_HEAD_INVALID`/`ATTESTATION_RERUN_INVALID`. |
| `AC-15.2-02` | All predecessor evidence and v1-to-v2 relation inventory | `python3 _bmad/scripts/generate_attestation_supersession.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/15.2.json --attestation docs/release-evidence/release-attestation-v2.json --output-json docs/release-evidence/release-attestation-supersession-v2.json --output-markdown docs/release-evidence/release-attestation-supersession-v2.md` | Exit `0`; every predecessor has one immutable/additive supersession edge and no orphan/fork. Missing edge is `ATTESTATION_SUPERSESSION_INCOMPLETE`. |
| `AC-15.2-03` | Generated attestation/supersession bundles and actual repository blobs | `python3 _bmad/scripts/verify_release_attestation_v2.py --repository . --attestation docs/release-evidence/release-attestation-v2.json --supersession docs/release-evidence/release-attestation-supersession-v2.json --output artifacts/v9/15.2/AC-15.2-03.json` | Exit `0`; hashes, RC/gitlinks, schemas, signable payload and JSON/Markdown parity recompute. Mismatch is `ATTESTATION_CANDIDATE_MISMATCH`, `ATTESTATION_PAYLOAD_INVALID`, or `EVIDENCE_FORMAT_DRIFT`. |
| `AC-15.2-04` | Protected predecessor-byte inventory before and after generation | `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ReleaseAttestationV2ValidationTest.PredecessorsShouldRemainByteIdentical -trx artifacts/v9/15.2/AC-15.2-04.trx` | Exit `0`; every protected byte/hash is unchanged. Mutation is `ATTESTATION_PREDECESSOR_MUTATED`. |
| `AC-15.2-05` | Candidate/head/rerun/payload/supersession/format mutations and a forbidden readiness/release-decision field | `python3 -m pytest -q _bmad/scripts/tests/test_release_attestation_v2.py -k complete_fault_matrix --junitxml=artifacts/v9/15.2/AC-15.2-05.xml` | Exit `0`; exact blockers occur, forbidden decision is `ATTESTATION_OUTCOME_PRESCRIBED`, and fixtures restore. |
| `AC-15.2-06` | AC-15.2-01 through AC-15.2-05 and Story 15.1 are current | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/15.2.json --format bundle --output-json docs/release-evidence/story-15.2-final-record-v2.json --output-markdown docs/release-evidence/story-15.2-final-record-v2.md` | Exit `0`; final record binds RC, attestation/supersession/signable digests, immutable inventory, mutation ledger, summary `6/6/0/0/0/0`; it contains no release verdict. |

## Gate RG-15: Independent Release Closure (Non-Story)

RG-15 runs only after Story 15.2 and cannot authorize earlier implementation.
Its frozen inventory `V9-RG-15-ENTRY-v1` contains, in order,
`V8-6.6-AC5`, `V8-6.6-AC6`,
`V8-6.6-GATE-INDEPENDENT-READINESS-AND-RELEASE-DECISION`, and
`V8-6.6-PROHIBITION-NO-PRESCRIBED-VERDICT`; SHA-256
`5882c68fbc694b84659d758274ebe67177af270b6871e676d573f9637fe65565`.

**Inputs:** Story 15.2 final record, attestation/supersession, exact `RC`, v9
authority bundle and all gate evidence.
**Generated records:**
`docs/release-evidence/rg-15-independent-review-v1.schema.json`,
`docs/release-evidence/rg-15-independent-review-v1.json`,
`docs/release-evidence/rg-15-release-decision-v1.schema.json`, and
`docs/release-evidence/rg-15-release-decision-v1.json`.
**Exact review command:**
`python3 _bmad/scripts/run_rg15_independent_review.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --attestation docs/release-evidence/release-attestation-v2.json --candidate HEAD --output docs/release-evidence/rg-15-independent-review-v1.json`.
**Exact decision command:**
`python3 _bmad/scripts/record_rg15_decision.py --repository . --review docs/release-evidence/rg-15-independent-review-v1.json --attestation docs/release-evidence/release-attestation-v2.json --owner-input artifacts/v9/rg-15/release-owner-decision.json --output docs/release-evidence/rg-15-release-decision-v1.json`.

The complete independent review is published byte-for-byte as returned. The
assessor is not instructed, prompted, patched or retried toward `READY`.
`READY` is necessary but does not predetermine the explicit release-owner
decision. `NOT_READY`, missing/incomplete/blocked review, evidence or candidate
drift, or absent owner decision leaves release closure `OPEN`. RG-15 has no
story status, final story record, implementation scope or rollback of evidence;
a successor review/decision is additive and preserves prior actual outcomes.

## Canonical V8-to-V9 Obligation Ledger

This is the obligation-level supersession authority. It contains every one of
the 66 effective acceptance criteria from unfinished v8 Stories 6.3-6.6 and
6.8-6.12, followed by every separately binding checkpoint, prohibition,
dependency, evidence obligation, rollback condition, completion gate, and
global hold/publication condition. A row occurs exactly once. A comma-separated
binding means the one compound v8 obligation requires the complete listed set
of atomic successor scenarios; it is not duplicate ownership.

Canonical digest input is one UTF-8 LF line
`<v8-obligation-id>|<exact-v9-binding><LF>` in the two table orders below,
excluding table syntax. Inventory version `V9-V8-OBLIGATION-LEDGER-v1` has
156 rows and SHA-256 `4dbffda456c4f40055985f303ed9d10d8e7839573e2486c4d01ca5508dca8f87`.

### Effective v8 acceptance-criterion mappings

| V8 obligation | Exact v9 atomic binding or disposition |
| --- | --- |
| `V8-6.3-AC1` | `AC-14.1-01,AC-14.1-02,AC-14.1-03,AC-14.1-04` |
| `V8-6.3-AC2` | `AC-14.2-01,AC-14.2-02,AC-14.2-03` |
| `V8-6.3-AC3` | `AC-14.2-02,AC-14.2-06` |
| `V8-6.3-AC4` | `AC-14.2-04` |
| `V8-6.3-AC5` | `AC-14.2-05` |
| `V8-6.3-AC6` | `AC-14.3-01,AC-14.3-02,AC-14.3-03,AC-14.3-04,AC-14.3-05,AC-14.3-06` |
| `V8-6.4-AC1` | `AC-8.1-02,AC-8.1-05` |
| `V8-6.4-AC2` | `AC-8.1-01` |
| `V8-6.4-AC3` | `AC-8.1-02,AC-8.1-03,AC-8.1-04,AC-8.1-05` |
| `V8-6.4-AC4` | `AC-8.1-05` |
| `V8-6.4-AC5` | `AC-8.2-01,AC-8.2-02,AC-8.2-03,AC-8.2-04,AC-8.2-05,AC-8.2-06,AC-8.2-07,AC-8.2-08,AC-8.2-09,AC-8.2-10` |
| `V8-6.4-AC6` | `AC-8.1-06` |
| `V8-6.5-AC1` | `AC-11.1-03,AC-11.1-04` |
| `V8-6.5-AC2` | `AC-11.1-02,AC-11.1-05` |
| `V8-6.5-AC3` | `AC-11.2-01,AC-11.2-02,AC-11.2-03,AC-11.2-04,AC-11.2-05,AC-11.2-06` |
| `V8-6.5-AC4` | `AC-11.3-01,AC-11.3-02,AC-11.3-04` |
| `V8-6.5-AC5` | `AC-11.3-03,AC-11.3-06` |
| `V8-6.5-AC6` | `AC-11.3-05,AC-11.3-07` |
| `V8-6.6-AC1` | `AC-15.1-02` |
| `V8-6.6-AC2` | `AC-15.1-03,AC-15.1-04` |
| `V8-6.6-AC3` | `AC-15.2-01,AC-15.2-02,AC-15.2-03,AC-15.2-04` |
| `V8-6.6-AC4` | `AC-15.1-01,AC-15.1-05,AC-15.1-06,AC-15.1-07` |
| `V8-6.6-AC5` | `NONSTORY:RG-15-INDEPENDENT-REVIEW` |
| `V8-6.6-AC6` | `NONSTORY:RG-15-RELEASE-DECISION` |
| `V8-6.8-AC1` | `AC-7.1-02,AC-7.2-02,AC-7.2-03,AC-7.2-04,AC-7.2-05,AC-7.2-06` |
| `V8-6.8-AC2` | `AC-7.2-01,AC-7.2-02,AC-7.2-03` |
| `V8-6.8-AC3` | `AC-7.2-04,AC-7.2-05` |
| `V8-6.8-AC4` | `AC-7.2-06,AC-7.2-07,AC-7.2-08,AC-7.2-09,AC-7.2-10,AC-7.2-11` |
| `V8-6.8-AC5` | `AC-7.3-01,AC-7.3-02,AC-7.3-03,AC-7.3-06` |
| `V8-6.8-AC6` | `AC-7.1-04,AC-7.3-04,AC-7.3-05,AC-7.3-06` |
| `V8-6.8-AC7` | `AC-7.4-01,AC-7.4-02` |
| `V8-6.8-AC8` | `AC-7.4-03,AC-7.4-04,AC-7.4-05` |
| `V8-6.9-AC1` | `AC-9.1-02,AC-9.1-03,AC-9.1-07` |
| `V8-6.9-AC2` | `AC-9.2-02` |
| `V8-6.9-AC3` | `AC-9.1-04,AC-9.2-04,AC-9.2-08` |
| `V8-6.9-AC4` | `AC-9.1-05,AC-9.1-06` |
| `V8-6.9-AC5` | `AC-9.1-08` |
| `V8-6.9-AC6` | `AC-9.2-05,AC-9.2-06,AC-9.2-07` |
| `V8-6.10-AC1` | `AC-10.1-01,AC-10.1-02` |
| `V8-6.10-AC2` | `AC-10.1-03,AC-10.1-04,AC-10.1-05,AC-10.1-06` |
| `V8-6.10-AC3` | `AC-10.2-01,AC-10.2-02,AC-10.2-03,AC-10.2-06,AC-10.2-07` |
| `V8-6.10-AC4` | `AC-10.2-04,AC-10.2-05` |
| `V8-6.10-AC5` | `AC-10.2-06,AC-10.2-07` |
| `V8-6.10-AC6` | `AC-10.3-01,AC-10.3-02` |
| `V8-6.10-AC7` | `AC-10.3-03,AC-10.3-04,AC-10.3-05,AC-10.3-06` |
| `V8-6.10-AC8` | `AC-10.4-01,AC-10.4-02,AC-10.4-03,AC-10.4-04` |
| `V8-6.10-AC9` | `AC-10.4-05,AC-10.4-06` |
| `V8-6.10-AC10` | `AC-10.4-07` |
| `V8-6.11-AC1` | `AC-12.1-02,AC-12.1-03,AC-12.1-04` |
| `V8-6.11-AC2` | `AC-12.3-01,AC-12.3-02` |
| `V8-6.11-AC3` | `AC-12.3-03,AC-12.3-04` |
| `V8-6.11-AC4` | `AC-12.3-05` |
| `V8-6.11-AC5` | `AC-12.2-01,AC-12.2-02,AC-12.2-03` |
| `V8-6.11-AC6` | `AC-12.4-01,AC-12.4-03,AC-12.4-04` |
| `V8-6.11-AC7` | `AC-12.3-06,AC-12.4-02,AC-12.4-03` |
| `V8-6.11-AC8` | `AC-12.3-02,AC-12.3-03,AC-12.3-04,AC-12.3-07` |
| `V8-6.11-AC9` | `AC-12.4-01,AC-12.4-02,AC-12.4-03` |
| `V8-6.11-AC10` | `AC-12.4-03,AC-12.4-05,AC-12.4-06` |
| `V8-6.12-AC1` | `AC-13.1-01` |
| `V8-6.12-AC2` | `AC-13.1-02` |
| `V8-6.12-AC3` | `AC-13.1-03,AC-13.1-04` |
| `V8-6.12-AC4` | `AC-13.2-01,AC-13.2-02` |
| `V8-6.12-AC5` | `AC-13.2-03,AC-13.2-04` |
| `V8-6.12-AC6` | `AC-13.3-01` |
| `V8-6.12-AC7` | `AC-13.3-05` |
| `V8-6.12-AC8` | `AC-13.3-02,AC-13.3-03,AC-13.3-04,AC-13.3-06` |

### Checkpoint, prohibition, dependency, evidence, rollback, and gate mappings

| V8 obligation | Exact v9 atomic binding or disposition |
| --- | --- |
| `V8-GLOBAL-HOLD` | `NONEXEC:HOLD-ACTIVE-UNTIL-VALIDATOR-PASS+IR0-READY+RELEASE-OWNER-LIFT` |
| `V8-GLOBAL-NO-IMPLEMENTATION` | `NONEXEC:V9-PUBLICATION-ONLY` |
| `V8-GLOBAL-V1-V8-HISTORY` | `IMMUTABLE:EPIC-6-HISTORICAL-FOUNDATION` |
| `V8-6.3-PROHIBITION-DENOMINATOR-DRIFT` | `AC-14.1-01,AC-14.1-02,AC-14.1-03,AC-14.1-04` |
| `V8-6.3-PROHIBITION-FR16-ACTIVATION` | `AC-14.1-05,AC-14.3-03` |
| `V8-6.3-PROHIBITION-HISTORY-AS-CURRENT` | `AC-14.2-05` |
| `V8-6.3-DEPENDENCY-6.8` | `AC-14.1-08` |
| `V8-6.3-DEPENDENCY-6.9` | `AC-14.1-08` |
| `V8-6.3-DEPENDENCY-6.10` | `AC-14.1-08` |
| `V8-6.3-DEPENDENCY-6.12` | `AC-14.1-08` |
| `V8-6.3-EVIDENCE-APPROVAL-COMPATIBILITY-HASH-TIER-PROOF` | `AC-14.2-02,AC-14.2-03,AC-14.2-04,AC-14.2-05,AC-14.2-06` |
| `V8-6.3-ROLLBACK-VERSIONED-MANIFEST` | `AC-14.3-05` |
| `V8-6.3-COMPLETION-GATE-SAME-CANDIDATE` | `AC-14.3-01,AC-14.3-06` |
| `V8-6.4-DEPENDENCY-6.1` | `IMMUTABLE:STORY-6.1-DONE` |
| `V8-6.4-DEPENDENCY-6.8` | `AC-8.1-07` |
| `V8-6.4-PROHIBITION-UI-ACTIVATION` | `AC-8.1-06` |
| `V8-6.4-EVIDENCE-UX-52-28-PARITY` | `AC-8.2-01,AC-8.2-08` |
| `V8-6.4-ROLLBACK-PRESERVE-UX-SOURCES` | `AC-8.2-10` |
| `V8-6.4-COMPLETION-GATE-ZERO-GAP` | `AC-8.2-11` |
| `V8-6.5-CHECKPOINT-A` | `AC-11.1-02,AC-11.1-03,AC-11.1-04,AC-11.1-05` |
| `V8-6.5-CHECKPOINT-B` | `AC-11.2-01,AC-11.2-02,AC-11.2-03,AC-11.2-04,AC-11.2-05,AC-11.2-06` |
| `V8-6.5-CHECKPOINT-C` | `AC-11.3-01,AC-11.3-02,AC-11.3-03,AC-11.3-04` |
| `V8-6.5-DEPENDENCY-6.2` | `AC-11.1-01` |
| `V8-6.5-DEPENDENCY-6.8` | `AC-11.1-01` |
| `V8-6.5-DEPENDENCY-6.10` | `AC-11.1-01` |
| `V8-6.5-PROHIBITION-PLATFORM-CAPABILITY-OWNERSHIP` | `AC-11.1-04` |
| `V8-6.5-EVIDENCE-SM2-FROZEN-INCLUSION` | `AC-11.3-01,AC-11.3-02,AC-11.3-04` |
| `V8-6.5-ROLLBACK-CHECKPOINTS` | `AC-11.1-06,AC-11.2-07,AC-11.3-06` |
| `V8-6.5-COMPLETION-GATE-ALL-CHECKPOINTS` | `AC-11.3-05,AC-11.3-07` |
| `V8-6.6-DEPENDENCY-6.3` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.4` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.5` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.8` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.9` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.10` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.11` | `AC-15.1-01` |
| `V8-6.6-DEPENDENCY-6.12` | `AC-15.1-01` |
| `V8-6.6-EVIDENCE-SIGNED-V1-ADR3-PROOF-CHAIN-RERUN` | `AC-15.2-01,AC-15.2-02,AC-15.2-03,AC-15.2-04` |
| `V8-6.6-PROHIBITION-V6-CEILING-OR-DISCLOSURE-PASS` | `AC-15.1-02` |
| `V8-6.6-PROHIBITION-NO-PRESCRIBED-VERDICT` | `NONSTORY:RG-15-INDEPENDENT-REVIEW` |
| `V8-6.6-ROLLBACK-ADDITIVE-ATTESTATION` | `AC-15.2-04,AC-15.2-05` |
| `V8-6.6-COMPLETION-GATE-LAST` | `AC-15.1-01,AC-15.2-06` |
| `V8-6.6-GATE-INDEPENDENT-READINESS-AND-RELEASE-DECISION` | `NONSTORY:RG-15-INDEPENDENT-REVIEW,RG-15-RELEASE-DECISION` |
| `V8-6.8-DEPENDENCY-6.2` | `AC-7.1-01` |
| `V8-6.8-EVIDENCE-FOUR-DERIVATION-SOURCES` | `AC-7.2-02,AC-7.2-04,AC-7.2-06,AC-7.2-09` |
| `V8-6.8-PROHIBITION-PROTECTED-SOURCE-CONTRACT-PACKAGE-BASELINE-EVIDENCE` | `AC-7.2-05,AC-7.4-01` |
| `V8-6.8-PROHIBITION-NO-CLOSED-RECORD-REWRITE` | `AC-7.4-01` |
| `V8-6.8-PROHIBITION-NO-SUBMODULE-MUTATION-OR-TRAVERSAL` | `AC-7.2-07,AC-7.2-08,AC-7.2-09,AC-7.2-10` |
| `V8-6.8-PROHIBITION-NO-CI-CLAIM` | `AC-7.3-06` |
| `V8-6.8-ROLLBACK-FAULT-BYTE-RESTORATION` | `AC-7.4-04` |
| `V8-6.8-COMPLETION-GATE-NONVACUOUS-MECHANICAL-RECORD` | `AC-7.4-05,AC-7.4-06` |
| `V8-6.9-DEPENDENCY-6.1` | `AC-9.1-01` |
| `V8-6.9-PROHIBITION-NO-PUBLIC-WIDENING` | `AC-9.1-07` |
| `V8-6.9-PROHIBITION-NO-REMOVE-SKIP-RENAME-WEAKEN` | `AC-9.1-02,AC-9.1-04,AC-9.2-08` |
| `V8-6.9-EVIDENCE-PRESPLIT-AND-SUMMED-EXECUTION` | `AC-9.2-04,AC-9.2-08` |
| `V8-6.9-ROLLBACK-RESTORE-MIGRATED-ASSERTIONS` | `AC-9.2-09` |
| `V8-6.9-COMPLETION-GATE-BOTH-TIERS` | `AC-9.2-06,AC-9.2-07,AC-9.2-10` |
| `V8-6.10-DEPENDENCY-6.8` | `AC-10.1-07` |
| `V8-6.10-DEPENDENCY-6.9` | `AC-10.1-07` |
| `V8-6.10-PROHIBITION-SIGNED-EVIDENCE-ALLOWLIST` | `AC-10.2-07` |
| `V8-6.10-PROHIBITION-UNAVAILABLE-HISTORY-PASS` | `AC-10.2-06` |
| `V8-6.10-PROHIBITION-VACUOUS-PASS` | `AC-10.2-06` |
| `V8-6.10-PROHIBITION-PROJECTION-PROOF-ABSORPTION` | `AC-10.4-04` |
| `V8-6.10-PROHIBITION-NO-CI-CLAIM` | `AC-10.4-05` |
| `V8-6.10-EVIDENCE-FROZEN-READERS-AND-STRENGTH` | `AC-10.4-01,AC-10.4-02` |
| `V8-6.10-ROLLBACK-FAULT-BYTE-RESTORATION` | `AC-10.4-06` |
| `V8-6.10-COMPLETION-GATE-BOUNDARY-AND-SPAN` | `AC-10.4-07,AC-10.4-08` |
| `V8-6.11-DEPENDENCY-6.2` | `AC-12.1-01` |
| `V8-6.11-PROHIBITION-EVENTSTORE-ONLY-WRITE-AUTHORITY` | `AC-12.1-03` |
| `V8-6.11-PROHIBITION-READS-NEVER-REPAIR-DURABLE-STATE` | `AC-12.3-02` |
| `V8-6.11-PROHIBITION-NO-PUBLIC-CONTRACT-DRIFT` | `AC-12.3-05` |
| `V8-6.11-PROHIBITION-NO-THRESHOLD-CHANGE` | `AC-12.2-02` |
| `V8-6.11-PROHIBITION-NO-ADVERSE-SAMPLE-DISCARD` | `AC-12.2-03` |
| `V8-6.11-PROHIBITION-NO-CORRECTNESS-WEAKENING` | `AC-12.3-06,AC-12.4-03` |
| `V8-6.11-PROHIBITION-NO-COST-DISCLOSURE-APPROVAL-SUBSTITUTE` | `AC-12.4-03,AC-12.4-04` |
| `V8-6.11-EVIDENCE-RAW-SAMPLES-ENVIRONMENT-CALCULATION-SIGNAL-IDENTITY` | `AC-12.4-01,AC-12.4-02,AC-12.4-03` |
| `V8-6.11-ROLLBACK-CORRECTNESS-PRESERVING` | `AC-12.3-07,AC-12.4-05` |
| `V8-6.11-COMPLETION-GATE-UNIVERSAL-SM-C2` | `AC-12.4-03,AC-12.4-06` |
| `V8-6.12-CHECKPOINT-A` | `AC-13.1-01,AC-13.1-02,AC-13.1-03,AC-13.1-04` |
| `V8-6.12-CHECKPOINT-B` | `AC-13.2-01,AC-13.2-02,AC-13.2-03,AC-13.2-04` |
| `V8-6.12-CHECKPOINT-C` | `AC-13.3-01,AC-13.3-02,AC-13.3-03,AC-13.3-04,AC-13.3-05` |
| `V8-6.12-DEPENDENCY-6.8` | `AC-13.1-01` |
| `V8-6.12-PROHIBITION-NO-V2-MUTATION` | `AC-13.1-01` |
| `V8-6.12-PROHIBITION-NO-PRODUCTION-CONTRACT-PACKAGE-BASELINE-SUBMODULE-CHANGE` | `AC-13.2-04` |
| `V8-6.12-PROHIBITION-NO-BACKLOG-AS-CURRENT-PROOF` | `AC-13.3-05` |
| `V8-6.12-PROHIBITION-NO-ASSERTION-WEAKENING` | `AC-13.3-02,AC-13.3-03,AC-13.3-04` |
| `V8-6.12-EVIDENCE-HISTORICAL-CANDIDATE-BLOBS` | `AC-13.1-01` |
| `V8-6.12-EVIDENCE-CURRENT-PROOF-CHAIN` | `AC-13.3-05` |
| `V8-6.12-ROLLBACK-CHECKPOINTS` | `AC-13.1-05,AC-13.2-06,AC-13.3-01` |
| `V8-6.12-COMPLETION-GATE-ALL-EIGHT-SAME-CANDIDATE` | `AC-13.3-06` |

## Publication State And Hold

- Canonical epic authority remains
  `epic-6-authority-2026-08-02-v9` and architecture authority remains
  `conversations-architecture-2026-08-02-v9`.
- Epic 6 is immutable completed historical foundation only; Epics 7-15 contain
  exactly 27 topologically ordered successor stories.
- `PC=UNBOUND`; no story candidate or release candidate is bound by this
  publication.
- The global implementation hold is `ACTIVE`. Missing, invalid, or
  candidate-drifted validation, IR-0, or owner decision also evaluates to
  `ACTIVE`.
- This block specifies future work only. No story, product code, runtime,
  contract, dependency, submodule, evidence record, baseline, or gate has been
  implemented or executed by this publication.

<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END version=epic-6-authority-2026-08-02-v9 architecture-authority=conversations-architecture-2026-08-02-v9 candidate=UNBOUND hold=ACTIVE -->

<!-- EPIC-6-AUTHORITY-OVERLAY-V10:BEGIN version=epic-6-authority-2026-08-03-v10 architecture-authority=conversations-architecture-2026-08-03-v10 supersedes=epic-6-authority-2026-08-02-v9 v9-block-bytes=188677 v9-block-sha256=e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

## Appendix: 2026-08-03 V10 Evidence-Boundary Planning Correction

**Epic authority:** `epic-6-authority-2026-08-03-v10`

**Architecture authority:** `conversations-architecture-2026-08-03-v10`

**Supersedes:** `epic-6-authority-2026-08-02-v9` only for the Story 10.3 and
Story 10.4 workflow/guidance projection

**Approved source:**
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md`

**Planning candidate (`PC`):** bound by
`_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`

**Global implementation hold:** `ACTIVE`

The exact 188,677-byte v9 authority block has SHA-256
`e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d`
and remains immutable. This amendment creates no epic or story and changes no
product, runtime, public-contract, persistence, package, UX, completed-record,
accepted-evidence, or gitlink boundary. Epic 10, Stories 10.1-10.2, the 27-reader
inventory, every inherited v8/v9 obligation, and all predecessor/successor
relationships remain unchanged. Only Stories 10.3 and 10.4 are amended below.

### Story 10.3 V10 Amendment: Govern Current Workflow Routes And Guidance

**Bounded outcome:** one verifier governs every current or directly callable
development/review transition route, proves deprecated aliases forward exactly
once to a governed route, and validates resolved project-owned guidance with
stable blocker/warning semantics.

**Exact predecessors:** `10.2`.

**Frozen entry inventory:** `V9-10.3-ENTRY-v1` and its digest remain unchanged.

**Mechanical workflow inventory:** `V9-EVIDENCE-WORKFLOWS-v2` is the following
NFC UTF-8 LF list; SHA-256
`966745d95e24aeb95af58a2bbfab11de7b08b8ab9f2447aa6c90a99c444292d4`:

```text
.agents/skills/bmad-build/step-04-review.md
.agents/skills/bmad-build/step-05-present.md
.agents/skills/bmad-build/step-oneshot.md
.agents/skills/bmad-build-auto/step-04-review.md
.agents/skills/bmad-dev-story/SKILL.md
.agents/skills/bmad-code-review/steps/step-04-present.md
.claude/skills/bmad-build/step-04-review.md
.claude/skills/bmad-build/step-05-present.md
.claude/skills/bmad-build/step-oneshot.md
.claude/skills/bmad-build-auto/step-04-review.md
.claude/skills/bmad-dev-story/SKILL.md
.claude/skills/bmad-code-review/steps/step-04-present.md
```

The six logical bodies must remain byte-identical across `.agents` and
`.claude`. Generated render output is checked through deterministic render
parity and is not a tracked authority path. `bmad-dev-auto` and
`bmad-quick-dev` in both trees must forward exactly once to `bmad-build-auto`
and `bmad-build`, respectively, and must not contain a second gate body.

**Reusable guidance inventory:** `V9-EVIDENCE-GUIDANCE-v2` is the following
NFC UTF-8 LF list; SHA-256
`e0a9adf0319286763f44d586ac323203a4af3d7faa4005e23768ce4a7c8f335d`:

```text
_bmad/custom/bmad-build.toml
_bmad/custom/bmad-build-auto.toml
_bmad/custom/bmad-review.toml
docs/runbooks/evidence-boundary-validation.md
```

Validation binds both the raw inventory and the resolved `bmad-build`,
`bmad-build-auto`, and `bmad-review` customization results. Installed
`customize.toml` defaults remain read-only.

**Stable blocker-code additions:** `EVIDENCE_ALIAS_ROUTE_INVALID`,
`EVIDENCE_GUIDANCE_NOT_USED`, `EVIDENCE_GUIDANCE_DRIFT`, and
`EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED`. Every v9 code remains stable,
including `EVIDENCE_GATE_NOT_USED`, `EVIDENCE_GATE_DISPLACED`,
`EVIDENCE_WORKFLOW_PARITY_DRIFT`, `SCOPE_NOT_EVALUATED`, and
`BASELINE_NOT_PROVIDED`.

**Candidate binding:** `SC-10.3`, Story 10.2 final-record digest, both v2
inventory digests, resolved customization digests, current BMAD version/route
facts, and the canonical candidate rule.

**Rollback boundary:** remove the verifier and current gate insertions as one
unit, remove the three project-owned customization files, and restore the
pre-10.3 workflow bytes. Retain Stories 10.1-10.2 and do not restore absent
pre-6.10 workflow files.

**Acceptance Criteria:** the existing identities `AC-10.3-01` through
`AC-10.3-08` are retained with these effective contracts:

| ID | Effective v10 contract |
| --- | --- |
| `AC-10.3-01` | Run the verifier with bound `PC`, `SC-10.3`, baseline, evidence/test diff, and stable registry. Exit `0` only for `PASS` or explicit `not-applicable`; applicable failure is `1`; missing authority/history is `2`; the result is schema-valid and records evaluated paths/assertions. |
| `AC-10.3-02` | Every stable blocker/warning fixture produces its exact unique code with zero synonyms. |
| `AC-10.3-03` | Validate the twelve-path `V9-EVIDENCE-WORKFLOWS-v2` inventory and exact insertion spans. Missing or displaced calls are `EVIDENCE_GATE_NOT_USED` or `EVIDENCE_GATE_DISPLACED`. |
| `AC-10.3-04` | Validate six logical bodies across `.agents` and `.claude`, deterministic render parity, and no cross-tree drift. Difference is `EVIDENCE_WORKFLOW_PARITY_DRIFT`. |
| `AC-10.3-05` | `not-applicable` remains distinct from `PASS`; applicable changes require a nonempty evaluated ledger. Empty applicable execution is `SCOPE_NOT_EVALUATED`. |
| `AC-10.3-06` | `FAIL` and `BLOCKED` prevent every current review/done transition and unattended finalization; valid `not-applicable` continues but is recorded. |
| `AC-10.3-07` | Remove and displace current gates, break one alias route, and remove one resolved guidance binding. Each fixture emits its exact gate, alias, or guidance code and restores byte-identically. |
| `AC-10.3-08` | The generated final record binds both v2 inventory digests, resolved customization digests, current route facts, all seven updated results, and summary `8/8/0/0/0/0`. |

### Story 10.4 V10 Amendment: Publish Reusable Resolved Guidance

The Story 10.4 bounded outcome, predecessor, rollback boundary, exact
27-reader `V9-EVIDENCE-READERS-v1` inventory and digest, and identities
`AC-10.4-01` through `AC-10.4-08` remain unchanged. The final record now binds
the three resolved customization results and canonical runbook digest and has
summary `9/9/0/0/0/0`.

#### AC-10.4-09 — Current dev/review guidance is reusable and resolved

**Given:** the canonical runbook, three project-owned customization files,
`V9-EVIDENCE-GUIDANCE-v2`, current BMAD defaults, and the Story 10.3 verifier
result schema.

**When:** resolve `bmad-build`, `bmad-build-auto`, and `bmad-review`
customization and run
`python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check`
from the repository root.

**Then:**

1. Regular build, one-shot build, unattended build, and general code review
   receive the same canonical evidence-boundary guidance.
2. Guidance requires recomputed hashes, contained paths, canonical signable
   payload, exact changed-path equality, raw-mode gitlink exclusion, exact
   inventory identity, root-of-trust pinning, and anti-vacuity.
3. Applicable changes invoke the verifier and preserve
   `PASS`/`FAIL`/`BLOCKED`/`not-applicable` distinctions.
4. Missing or drifted guidance fails with `EVIDENCE_GUIDANCE_NOT_USED` or
   `EVIDENCE_GUIDANCE_DRIFT`.
5. No shipped `DO NOT EDIT` default customization file changes.
6. Removing an override, weakening exact equality to containment, trusting a
   declared hash, or redirecting the runbook turns validation red and restores
   the fixture byte-identically.

`AC-10.4-09` atomically realizes the already-mapped `V8-6.10-AC9` guidance
obligation; it creates no new v8 obligation.

### Publication, Hold, And Action State

The deterministic v9 companion publication binds one committed `PC` to the v10
authorities, schemas, 27 story contracts, v2 workflow/guidance inventories,
preserved reader inventory, execution graph, supersession map, v2 current view,
52/28 UX projection, sprint projection, resolved customization results, and
authority bundle. The bundle excludes itself and mutable decision/evidence
records from its digest.

The global implementation hold remains `ACTIVE`; IR-0 is neither run nor
biased by this publication. Epic 5 action A5 remains `open` and closes only
from a compatible passing Story 10.4 final record. No lifecycle status,
runbook, customization file, validator result, or planning publication alone
may close A5 or lift the hold.

<!-- EPIC-6-AUTHORITY-OVERLAY-V10:END version=epic-6-authority-2026-08-03-v10 architecture-authority=conversations-architecture-2026-08-03-v10 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

<!-- EPIC-6-AUTHORITY-OVERLAY-V11:BEGIN version=epic-6-authority-2026-08-04-v11 architecture-authority=conversations-architecture-2026-08-04-v11 supersedes=epic-6-authority-2026-08-03-v10 v10-block-bytes=8746 v10-block-sha256=3c33462d0bc28f9fec36e571d7dcf4a60c77d02c94bd3675528a05d704d07588 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

## Appendix: 2026-08-04 V11 Story 7.1 Schema-Checkpoint Authority

**Epic authority:** `epic-6-authority-2026-08-04-v11`

**Architecture authority:** `conversations-architecture-2026-08-04-v11`

**Supersedes:** `epic-6-authority-2026-08-03-v10` only for Story 7.1
implementation order, the current execution graph, and candidate-bound
publication companions

**Approved source:**
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md`

**Planning candidate (`PC`):** bound by
`_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`

**Global implementation hold:** `ACTIVE`

The exact 8,746-byte v10 epic block has SHA-256
`3c33462d0bc28f9fec36e571d7dcf4a60c77d02c94bd3675528a05d704d07588`
and remains immutable. The v9 story-contract schema and all 27 v10 base story
contract shapes remain unchanged. Story 7.1 retains its complete six-scenario
outcome, its two final-record paths, and `6.2` as its exact story predecessor.
Story 7.2 still requires complete Story 7.1.

### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice

Checkpoint `7.1-SCHEMAS` is a non-story execution slice. It establishes the
three missing closed Draft 2020-12 schemas and proves all four Story 7.1
schemas through `AC-7.1-01` before generator implementation.

**Entry conditions:** completed Story `6.2`; deterministic v11 publication and
check `PASS` at one committed `PC`; independent `IR-0` result `READY` for that
same candidate, bundle, and authority pair; and a separately governed,
candidate-matched release-owner `LIFTED` decision. Missing, invalid, stale,
mismatched, revoked, or non-ready evidence evaluates to `ACTIVE`.

**Exact execution predecessors:** `6.2`, `IR-0`.

**Writable implementation paths, in order:**

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/scripts/tests/test_generate_story_record.py
artifacts/v9/schema-slice/v2-schema-contract.xml
```

**Read-only inputs, in order:**

```text
_bmad/schemas/v9-story-contract-v1.schema.json
_bmad-output/planning-artifacts/v9/story-contracts/7.1.json
_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json
```

**Checkpoint command:**

```text
python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml
```

**Result semantics:** `PASS` with exit `0`; `FAIL` with exit `1` or `5`;
`BLOCKED` with exit `2`, `3`, or `4`. The result is nonempty, all four schemas
pass the Draft 2020-12 metaschema, representative valid instances pass, and
missing, extra, malformed, duplicate, non-normalized, or permissive mutations
fail with `OUTPUT_SCHEMA_INVALID` semantics and restore byte-identically.

**Prohibitions:** no edit to `_bmad/scripts/generate_story_record.py`; no
acceptance-result instance or Story 7.1 final record; no `AC-7.1-02` through
`AC-7.1-06`; no Story 7.2-7.4 work; no product/public-contract, package,
deployment, submodule, gitlink, completed-record, accepted-evidence, or
signed-evidence change; and no story `done` transition.

**Completion effect:** checkpoint evidence may support continued Story 7.1
implementation and may move Story 7.1 to `in-progress`. It never marks Story
7.1 `done`, never produces a final record, and never unlocks a successor.
`AC-7.1-01` must run again or be proven current at final candidate `SC-7.1`.

**Rollback boundary:** remove only the three new schemas, schema-specific test
changes, and checkpoint result. Preserve the existing story-contract schema,
planning authority, publisher, completed history, and all non-checkpoint work.

### V11 Publication, Hold, And Retrospective State

The generated
`_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` sidecar
binds this amendment-section digest, the regenerated v10 base Story 7.1
contract digest, the current authority pair, the exact ordered path sets,
closed prohibitions, result semantics, completion effect, and rollback
boundary. It records only the authority-bundle path. The self-excluding bundle
records the sidecar digest, so no source or sidecar claims its own final digest.

The execution graph contains exactly one `checkpoint` node named
`7.1-SCHEMAS`, with predecessors `6.2` and `IR-0`. Story 7.1 has the checkpoint
as an additional execution predecessor; Story 7.2 remains downstream of the
complete Story 7.1 node. The checkpoint has no sprint lifecycle key and no
final-record output.

Publication-time state remains `implementationHold: ACTIVE`. This publication
does not embed an IR-0 verdict or hold-decision verdict, run or bias IR-0, lift
the hold, start Story 7.1, or unlock Story 7.2. The sprint projection preserves
`epic-6-retrospective: done` and the six ordered open Epic 6 retrospective
actions byte-for-byte while keeping Epic 7 and Story 7.1 at `backlog`.

<!-- EPIC-6-AUTHORITY-OVERLAY-V11:END version=epic-6-authority-2026-08-04-v11 architecture-authority=conversations-architecture-2026-08-04-v11 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

<!-- EPIC-6-AUTHORITY-OVERLAY-V12:BEGIN version=epic-6-authority-2026-08-04-v12 architecture-authority=conversations-architecture-2026-08-04-v12 supersedes=epic-6-authority-2026-08-04-v11 v11-block-bytes=5474 v11-block-sha256=6c9bd7164ef35e4093d69226a5988fe73f5400aab3049911a3298b0987d79f19 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

## Appendix: 2026-08-04 V12 Pre-IR-0 Remediation Checkpoint

**Epic authority:** `epic-6-authority-2026-08-04-v12`

**Architecture authority:** `conversations-architecture-2026-08-04-v12`

**Supersedes:** `epic-6-authority-2026-08-04-v11` only for the additive
pre-IR-0 remediation checkpoint, its current-route inventory, and its
candidate-bound companion projection

**Approved source:**
`_bmad-output/implementation-artifacts/spec-v12-pre-ir-0-remediation-checkpoint.md`

**Planning candidate (`PC`):** bound by
`_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`

**Global implementation hold:** `ACTIVE`

The exact 5,474-byte v11 epic block has SHA-256
`6c9bd7164ef35e4093d69226a5988fe73f5400aab3049911a3298b0987d79f19`
and remains immutable. V1-V11 authority, every completed story record,
accepted or rejected retrospective evidence, signed evidence, and the prior
IR-0 result remain point-in-time provenance. This amendment never changes a
completed Story 6.7 or Story 6.2 status or byte and never treats current bytes
as historical completion evidence.

### E6-REMEDIATION: Own A1-A3 Before Independent IR-0

`E6-REMEDIATION` is a non-story correction checkpoint with exact predecessor
`PC-PUBLICATION` and exact successor `IR-0`. It has no sprint lifecycle key,
does not produce a story final record, and authorizes only the closed A1-A3
inventory below. The execution graph contains this node exactly once; IR-0
depends on it instead of depending directly on `PC-PUBLICATION`.

| ID | Remediation obligation | Owner | Required completion evidence |
| --- | --- | --- | --- |
| `A1` | Reconstruct the actual Story 6.7 and Story 6.2 done trees and obtain an independent completion-acceptance supersession decision. | Dev workflow produces evidence; Release owner independently decides. | The closed contract binds the two recorded candidates, two actual done commits, exact raw changed paths, and exactly ten root mode-`160000` gitlinks at each tree; every object is locally inspectable; exact done-tree promotion and rebuilt-test commands pass with zero skipped/not-run; the decision is `ACCEPTED` and supersedes acceptance evidence only. |
| `A2` | Restore the submodule-promotion and evidence-boundary gates on every current review/done transition. | Dev workflow. | Exactly twelve active route files in the `.agents` and `.claude` trees are byte-paired, invoke both gates before lifecycle writes, preserve `PASS`/`FAIL`/`BLOCKED`/`not-applicable`, require a nonempty ledger, and reject removal, displacement, decoy, parity, unavailable-history, skipped, and empty-ledger faults. |
| `A3` | Harden planning-authority history/signature/context validation and add automatic preflight. | Architecture / Quality. | Recorded submodule evidence never falls back to current checkout bytes; zero-parameter signatures are checked non-vacuously; generated Epic context carries exact governing version frontmatter; pytest/jsonschema run from the frozen lock; the root preflight runs Python publication/evidence checks and direct V9/V8/architecture validators. |

A1-A3 are all required before IR-0 may run. `PASS` is not inferred from an
empty set, an omitted command, a fallback lane, current-tree evidence, or an
unavailable object. Such states are `FAIL` or `BLOCKED` with stable diagnostics.
The A1 independent decision is a separate mutable planning result and is not
part of the self-excluding authority-bundle digest.

### Downstream Retrospective Ownership Preserved

The remaining Epic 6 retrospective actions stay open and outside this
checkpoint. V12 assigns no product implementation and creates no substitute
successor story:

| ID | Preserved owner | Execution authority after IR-0 |
| --- | --- | --- |
| `A4` | Architect / Runtime owner | Separately approved successor work for durable event-fed tenant access, freshness/gap detection, restart, and multi-replica convergence. |
| `A5` | Projection owner | Separately approved successor work for deterministic event-derived replay timestamps and trustworthy missing-index semantics. |
| `A6` | Test / AppHost owner | Separately approved successor work for endpoint/port diagnostics and live `project/v2/reconcile` terminal retry coverage. |

Their six committed sprint action rows and original owners remain unchanged.
The V12 publisher preserves their exact identifiers, order, text, and open
status. E6-REMEDIATION completion cannot close A4-A6, Epic 5 action A5, or any
successor outcome.

### V12 Publication And Completion Effect

The generated
`_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json`
sidecar binds the exact checkpoint inventory, ownership, current route set,
prohibitions, and completion effect. The V12 authority bundle includes the
sidecar, its recursively closed schema, the pinned verifier environment, the
supersession contract/schema/verifiers, the workflow/context/preflight guards,
and candidate-bound generated companions. Mutable reconstruction evidence,
the independent decision, IR-0 reports, hold decisions, final records, and
release evidence remain outside the bundle digest.

Checkpoint completion permits only an independent IR-0 rerun against the same
committed PC, bundle digest, V12 authority pair, and ten gitlinks. It does not
lift the hold, start Story 7.1 or `7.1-SCHEMAS`, unlock another successor,
authorize release, create `_bmad-output/planning-artifacts/implementation-hold-v1.json`,
or modify a product, package, submodule, or gitlink. Even when IR-0 records
`READY`, `implementationHold` remains `ACTIVE` until the separately governed
release-owner decision required by inherited authority exists.

<!-- EPIC-6-AUTHORITY-OVERLAY-V12:END version=epic-6-authority-2026-08-04-v12 architecture-authority=conversations-architecture-2026-08-04-v12 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

<!-- EPIC-6-AUTHORITY-OVERLAY-V14:BEGIN version=epic-6-authority-2026-08-18-v14 architecture-authority=conversations-architecture-2026-08-18-v14 supersedes=epic-6-authority-2026-08-04-v12 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->

## Epic 16: Operational Projection Correctness And Recovery

**Epic authority:** `epic-6-authority-2026-08-18-v14`

**Architecture authority:** `conversations-architecture-2026-08-18-v14`

**Supersedes:** V12 only for A4–A6 successor ownership, the effective story
inventory, and the graph/predecessor additions enumerated below. Architecture
V13's Epic V13 identity was an unpublished forward reference and is not
reconstructed.

**Implementation hold:** `ACTIVE`. Publication creates backlog contracts only.
Story execution still requires independent IR-0 `READY` and a separately
governed release-owner `LIFTED` decision.

**Action mapping:** A4 → Story 16.1; A5 → Story 16.2; A6 → Story 16.3. Each
action remains `open` until its compatible final record passes.

**Epic outcome:** Operators and authorized callers receive durable tenant
authorization, deterministic replay-derived projection truth, and diagnosable
live reconciliation across restart and multi-replica execution.

### Story 16.1: Persist tenant-access projection state and prove convergence

**Exact predecessors:** `7.4`, `IR-0`.

**Candidate binding:** The canonical contract, exact root/Tenants gitlinks,
inputs, outputs, fault ledger, and final record bind the committed planning
candidate through `v9-authority-bundle-v1.json`.

**Frozen inventory:** `V14-16.1-ENTRY-v1`; SHA-256
`eeb9eee87de7bc646cdf09acaf3f6e65351c71472a55f2d8b65de2e12b44511f`.

**Bounded outcome:** Add a tenant-domain durable-store capability behind
`ITenantProjectionStore`, configure its Conversations consumer, expose explicit
freshness/sequence/gap state, prove restart and two-replica convergence, and
remove the single-replica warning only after those proofs pass.

**Rollback boundary:** Revert only the durable provider/configuration,
Conversations adoption, tests/evidence, Tenants promotion commit, and root
gitlink update. Preserve tenant events, public contracts, completed records,
other gitlinks, and accepted evidence.

**Generated final record:**
`docs/release-evidence/story-16.1-final-record-v2.json` and
`docs/release-evidence/story-16.1-final-record-v2.md`.

| ID | Required proof | Command | Pass condition |
| --- | --- | --- | --- |
| `AC-16.1-01` | Closed storage/freshness/sequence contract | `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-01 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-01.json` | `PASS`; unknown, stale, gapped, or corrupt state never authorizes access. |
| `AC-16.1-02` | Restart without event redelivery | `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-02 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-02.json` | `PASS`; decisions and sequence metadata reload identically. |
| `AC-16.1-03` | Two replicas with one consumer-group delivery | `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-03 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-03.json` | `PASS`; replicas converge to one decision and sequence. |
| `AC-16.1-04` | Duplicate, ordering, gap, unavailable, and corrupt faults | `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-04 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-04.json` | `PASS`; duplicates are idempotent and every unsafe state fails closed. |
| `AC-16.1-05` | Focused builds, tests, safety, and contract diff | `python3 _bmad/scripts/verify_story_16_1.py --repository . --scenario AC-16.1-05 --candidate HEAD --output artifacts/v14/16.1/AC-16.1-05.json` | `PASS`; additive surface, zero failed/skipped/not-run, no personal data leakage. |
| `AC-16.1-06` | Canonical final record | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/16.1.json --format bundle --output-json docs/release-evidence/story-16.1-final-record-v2.json --output-markdown docs/release-evidence/story-16.1-final-record-v2.md` | `PASS`; summary `6/6/0/0/0/0` and rollback binding. |

### Story 16.2: Make replay time deterministic and missing-index state truthful

**Exact predecessors:** `16.1`.

**Candidate binding:** The contract binds exact replay inputs, outputs, hashes,
faults, and final record to the committed planning candidate.

**Frozen inventory:** `V14-16.2-ENTRY-v1`; SHA-256
`64403ee626140f90094caf804a1d0e1d98475054e6627cf9c5b282f32b6c5abe`.

**Bounded outcome:** Derive domain/freshness time from immutable event inputs,
eliminate replay clock fallbacks, add an event-fed lifecycle/watermark fact,
and distinguish initialized-empty state from missing, erased, or unavailable
state without query-side repair writes.

**Rollback boundary:** Revert only timestamp derivation, the lifecycle/watermark
addition, read semantics, tests/evidence, and the Story 16.2 record. Preserve
event history, public contract shape, Story 16.1 state, and prior proof history.

**Generated final record:**
`docs/release-evidence/story-16.2-final-record-v2.json` and
`docs/release-evidence/story-16.2-final-record-v2.md`.

| ID | Required proof | Command | Pass condition |
| --- | --- | --- | --- |
| `AC-16.2-01` | Fixed-history materialization including absent/invalid time | `python3 _bmad/scripts/verify_story_16_2.py --repository . --scenario AC-16.2-01 --candidate HEAD --output artifacts/v14/16.2/AC-16.2-01.json` | `PASS`; time is event-derived or produces the typed safe state. |
| `AC-16.2-02` | Two clean rebuilds | `python3 _bmad/scripts/verify_story_16_2.py --repository . --scenario AC-16.2-02 --candidate HEAD --output artifacts/v14/16.2/AC-16.2-02.json` | `PASS`; projection JSON and timestamps are byte-identical. |
| `AC-16.2-03` | Empty, missing, surviving-detail, and erased-state fixtures | `python3 _bmad/scripts/verify_story_16_2.py --repository . --scenario AC-16.2-03 --candidate HEAD --output artifacts/v14/16.2/AC-16.2-03.json` | `PASS`; only proven initialized-empty is current empty. |
| `AC-16.2-04` | Query write ledger | `python3 _bmad/scripts/verify_story_16_2.py --repository . --scenario AC-16.2-04 --candidate HEAD --output artifacts/v14/16.2/AC-16.2-04.json` | `PASS`; reads perform zero repair writes. |
| `AC-16.2-05` | Query/freshness, redaction, isolation, and rebuild conformance | `python3 _bmad/scripts/verify_story_16_2.py --repository . --scenario AC-16.2-05 --candidate HEAD --output artifacts/v14/16.2/AC-16.2-05.json` | `PASS`; no shape drift or failed/skipped/not-run result. |
| `AC-16.2-06` | Canonical final record | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/16.2.json --format bundle --output-json docs/release-evidence/story-16.2-final-record-v2.json --output-markdown docs/release-evidence/story-16.2-final-record-v2.md` | `PASS`; summary `6/6/0/0/0/0` and exact replay hashes. |

### Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live

**Exact predecessors:** `16.2`.

**Candidate binding:** The contract binds AppHost/runtime identities, route
proof, diagnostics, faults, gitlinks, and final record to the committed
planning candidate.

**Frozen inventory:** `V14-16.3-ENTRY-v1`; SHA-256
`0a9c0dc63e95a76f7b6cc2a089fd866296965a09f64feed4ada6fe872e367458`.

**Bounded outcome:** Add stable endpoint-readiness and Dapr control-plane
port-collision diagnostics, then prove durable pending work is cleared through
the live `project/v2/reconcile` production route and becomes query-visible.

**Rollback boundary:** Remove only the diagnostics, AppHost fixtures/tests,
route assertions, and Story 16.3 record. Preserve the production route,
platform hosting ownership, Stories 16.1/16.2, and public contracts.

**Generated final record:**
`docs/release-evidence/story-16.3-final-record-v2.json` and
`docs/release-evidence/story-16.3-final-record-v2.md`.

| ID | Required proof | Command | Pass condition |
| --- | --- | --- | --- |
| `AC-16.3-01` | Healthy resource with endpoint not connect-ready | `python3 _bmad/scripts/verify_story_16_3.py --repository . --scenario AC-16.3-01 --candidate HEAD --output artifacts/v14/16.3/AC-16.3-01.json` | `PASS`; stable `APPHOST_ENDPOINT_NOT_READY` without tenant leakage. |
| `AC-16.3-02` | Occupied effective Dapr control-plane port | `python3 _bmad/scripts/verify_story_16_3.py --repository . --scenario AC-16.3-02 --candidate HEAD --output artifacts/v14/16.3/AC-16.3-02.json` | `PASS`; stable `DAPR_CONTROL_PLANE_PORT_COLLISION` identifies the endpoint/port. |
| `AC-16.3-03` | Live first projection failure | `python3 _bmad/scripts/verify_story_16_3.py --repository . --scenario AC-16.3-03 --candidate HEAD --output artifacts/v14/16.3/AC-16.3-03.json` | `PASS`; durable pending work is visible and direct handler use is forbidden. |
| `AC-16.3-04` | Production reconciliation request | `python3 _bmad/scripts/verify_story_16_3.py --repository . --scenario AC-16.3-04 --candidate HEAD --output artifacts/v14/16.3/AC-16.3-04.json` | `PASS`; retry clears one terminal item and corrected state is query-visible. |
| `AC-16.3-05` | Route, duplication, race, port, Dapr, and vacuity faults | `python3 _bmad/scripts/verify_story_16_3.py --repository . --scenario AC-16.3-05 --candidate HEAD --output artifacts/v14/16.3/AC-16.3-05.json` | `PASS`; each stable blocker is observed and fixtures restore. |
| `AC-16.3-06` | Canonical final record | `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/16.3.json --format bundle --output-json docs/release-evidence/story-16.3-final-record-v2.json --output-markdown docs/release-evidence/story-16.3-final-record-v2.md` | `PASS`; summary `6/6/0/0/0/0` and rollback binding. |

### V14 Effective Graph And Carried Decisions

Graph deltas are `IR-0 -> 16.1`, `7.4 -> 16.1`, `16.1 -> 16.2`,
`16.2 -> 16.3`, and `16.3 -> 12.1, 13.1, 14.1, 15.1`. The composed graph
also includes `E6-CURRENT-PROOF <- E6-REMEDIATION` and
`E6-CURRENT-CANDIDATE <- E6-REMEDIATION, E6-CURRENT-PROOF`, yielding exactly
38 nodes and 61 edges.

- **DC-9:** Epic 9 contracts carry the Quality-owner tier-migration strength
  inventory; V14 adds no new Epic 9 story.
- **DC-10:** Epic 12 hard entry retains the repository-local non-packable
  AppHost interpretation until a contrary baseline-owner decision exists.
- **DC-11:** The v6 approved-cost ceiling and retirement obligation remain
  non-executable history; no successor owes a retirement artifact.

No V14 planning publication runs IR-0, changes the hold, closes A2–A6, starts a
story, authorizes release, or modifies product, dependency, submodule, or
gitlink state.

<!-- EPIC-6-AUTHORITY-OVERLAY-V14:END version=epic-6-authority-2026-08-18-v14 architecture-authority=conversations-architecture-2026-08-18-v14 candidate-binding=v9-authority-bundle-v1.json hold=ACTIVE -->
