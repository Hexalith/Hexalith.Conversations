---
stepsCompleted: ['step-01-document-discovery', 'step-02-prd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
filesIncluded:
  - '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md'
  - '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/ux-design-specification.md'
  - '_bmad-output/planning-artifacts/ux-requirement-map.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-08
**Project:** Conversations

---

## Step 1 — Document Inventory

### Canonical document set selected for this assessment

| Type | File | Size | Modified |
|------|------|------|----------|
| PRD | `prds/prd-Conversations-2026-06-02/prd.md` | 31,637 B | 2026-06-02 |
| PRD addendum | `prds/prd-Conversations-2026-06-02/addendum.md` | 8,019 B | 2026-06-03 |
| Epics & Stories | `prds/prd-Conversations-2026-06-02/epics.md` | 55,029 B | 2026-06-02 |
| Architecture | `architecture.md` | 80,993 B | 2026-05-31 |
| UX Spec | `ux-design-specification.md` | 117,645 B | 2026-05-31 |
| UX Requirement Map | `ux-requirement-map.md` | 8,825 B | 2026-05-31 |
| Sprint status | `implementation-artifacts/sprint-status.yaml` | — | 2026-06-04 |

**Story specs (implementation-artifacts):** 13 files — Epic 1 (1-1→1-5), Epic 2 (2-1→2-7), Epic 3 (3-1) — plus 2 epic retros and a test summary.

### Duplicate resolution (CRITICAL)
- Two distinct PRDs were found. **Resolution:** the newer `prds/prd-Conversations-2026-06-02/prd.md` (2026-06-02) is treated as **canonical**; the older top-level `prd.md` (2026-05-31, 153,780 B) is **superseded and excluded** from this assessment (file not removed from disk — excluded by selection).

### Open alignment risks carried into later steps
- `epics.md` belongs to the newer `2026-06-02` PRD set, while `architecture.md` and the UX specs are dated `2026-05-31` (older). Architecture/UX↔PRD alignment must be verified rather than assumed.
- Decision context lives in `prds/prd-Conversations-2026-06-02/.decision-log.md` and `addendum.md`.

---

## Step 2 — PRD Analysis

> **Nature of this PRD:** This is a **refactoring/boilerplate-reduction initiative**, not a feature PRD. Its "users" are developers; its FRs describe plumbing that must be *consumed* from or *promoted* into shared technical modules, while behavior and contracts are held provably constant. The superseded `../../prd.md` is the *feature* PRD (what Conversations does); this one defines *how it is built cheaper*.

### Functional Requirements

**Group 4.1 — Boilerplate Inventory & Classification (baseline)**
- **FR-1: Canonical boilerplate inventory exists and is accepted.** A maintainer can read a single inventory artifact listing every Conversations source area with its Consume/Promote/Keep classification, evidence (paths, ~LOC), and target technical-module capability. *Testable:* every top-level `Hexalith.Conversations.*` area appears with exactly one classification; each Consume/Promote entry names its mapped capability; the SM-1 baseline plumbing-LOC is derived and recorded.
- **FR-2: Classification disagreements are resolvable, not silent.** A reviewer can challenge any C/P/K call and the resolution is recorded with rationale. *Testable:* reclassifications logged; no area unclassified or dual-classified at acceptance.

**Group 4.2 — Consume Existing Technical-Module Surface (UJ-1, gated by FR-20)**
- **FR-3: Domain-service host adoption.** Conversations runs on the shared two-line domain-service host (assembly-scan registration + endpoint mapping) instead of bespoke DI/host wiring. *Testable:* domain registered via shared SDK entrypoints; canonical endpoints (process/replay/query/project/admin metadata) resolve via shared host; host/registration tests pass or equivalently replaced.
- **FR-4: Query handling via SDK query-handler + cursor seams.** Implement query handlers against the SDK seam and use the SDK cursor codec instead of bespoke orchestration + hand-rolled HMAC cursor signing. *Testable:* hand-rolled cursor signing removed; conversation-specific filters/shapes remain; cursor round-trip/pagination re-resolves identically.
- **FR-5: Read-model persistence via shared store + write policy.** Persist/update read models through the shared store + optimistic-concurrency write policy instead of bespoke store/merge code. *Testable:* hand-written DAPR state-store calls + merge loops removed; optimistic-concurrency/retry preserved (no lost updates).
- **FR-6: Projection handling via SDK projection seam.** Implement projections against the SDK full-replay seam; delegate the generic event→model loop while keeping which fields/metadata a projection emits. *Testable:* generic orchestration delegated; field selection + freshness formula + evidence construction remain; rebuild/freshness conformance passes.
- **FR-7: Aggregate scaffolding via base-class conventions.** Rely on the EventStore aggregate base class's reflection command dispatch + state replay rather than redundant manual routing / status-bridge shims. *Testable:* routing/replay use base-class conventions; redundant dispatch/idempotency-bridge plumbing removed; pure aggregate behavior unchanged.
- **FR-8: Serialization via shared converters / type registration.** Use shared serialization helpers (type mapper / generic converters / source-gen context base) for generic patterns instead of bespoke per-type converters with no domain logic. *Testable:* generic converters replaced; only domain-rule converters remain; contract shapes byte/shape-compatible.
- **FR-9: Testing via shared assertions/fakes/defaults.** Test projects consume shared EventStore.Testing assertions/fakes + shared ServiceDefaults instead of duplicating them. *Testable:* duplicate in-module fakes/assertions removed; domain conformance fixtures (redaction, provider-portability, tenant-isolation) remain.

**Group 4.3 — Promote Common Boilerplate to Technical Modules (UJ-2, gated by FR-20; landing zone = OQ-1)**
- **FR-10: Shared ServiceDefaults.** Configure observability/health/resilience/service-discovery via a shared ServiceDefaults capability with module-specific hooks (Redis/DAPR) instead of a copied file. *Testable:* Conversations ServiceDefaults reduced to hooks; health/telemetry endpoint behavior preserved.
- **FR-11: Generic tenant-access projection handler + registration.** Wire fail-closed tenant-access projection handling + DI via a generic shared capability parameterized by event/projection types, instead of copied handler + `AddXxxTenantAccess()`. *Testable:* hand-written handler/registration replaced with generic + concrete type args; fail-closed on missing/stale/unavailable/disabled/ambiguous/insufficient preserved; duplicate/out-of-order/replay tolerance preserved.
- **FR-12: Generic typed-HttpClient client registration.** Register the typed HTTP client with options binding/validation via a shared domain-agnostic helper instead of a copied `AddXxxClient()` pair. *Testable:* client registration uses shared helper; options validation (missing/relative URL rejection) preserved.
- **FR-13: Shared Aspire/Dapr domain-module hosting base.** Attach AppHost/Aspire + DAPR sidecar topology (shared vs isolated modes) via a shared hosting capability parameterized by app-id/component names. *Testable:* Aspire wiring expressed through shared capability; state-store/pub-sub/sidecar resource wiring equivalent to today.
- **FR-14: Shared JSON-context base / polymorphic type registration.** Declare serializable type lists against a shared source-gen JSON-context base / polymorphic helper instead of hand-assembling resolver combination + type catalogs. *Testable:* JSON context declares only type lists; polymorphic (de)serialization of event/command hierarchies preserved.
- **FR-15: Diagnostics/telemetry scaffolding helper.** Instrument metrics via a shared meter/counter/classifier scaffolding helper, supplying only metric names + bounded dimension vocabularies. *Testable:* meters/counters registered through helper; emitted metric names + cardinality preserved.
- **FR-16: Compile-time command/event contract metadata *(CONDITIONAL — OQ-4)*.** Declare command/event domain+type metadata via shared contract interfaces (parallel to `IQueryContract`) instead of magic-string type names — **only if** Conversations consumes it in pilot scope. *Testable:* if built, magic-string type names removed; if deferred, recorded as backlog with rationale (does not block FR-20).

**Group 4.4 — Adopt, Prove & Templatize (UJ-3)**
- **FR-17: Conversations consumes every in-scope promotion.** Conversations depends on/uses each promoted capability (FR-10..FR-16 as built); no superseded local copy remains. *Testable:* each in-scope local implementation deleted (not bypassed); build + all conformance suites pass against promoted libraries.
- **FR-18: Documented thin authoring template.** A developer can follow a documented authoring template (minimal skeleton + checklist of shared capabilities) to stand up a new domain module. *Testable:* template enumerates shared host, aggregate base, handler seams, tenant-access, client, Aspire, serialization, telemetry with one-liners; validated against post-refactor Conversations.
- **FR-19: New-module authoring cost is measured.** Record the authoring cost (files/LOC for a minimal valid module) as the SM-2 baseline. *Testable:* a measured "minimal module" figure recorded and traceable to the template.

**Group 4.5 — Behavior-Preservation Conformance Gate**
- **FR-20: Behavior and contracts are provably preserved.** The full release-gate conformance suite passes on the refactored module and public contracts are shape-compatible; only plumbing-only tests are removed. *Testable:* all release-gate suites (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) pass; public contract shapes unchanged or explicitly approved+recorded; every removed test justified as plumbing-only.

**Total FRs: 20** (FR-16 conditional on OQ-4).

### Non-Functional Requirements

*(From §8 Cross-Cutting NFRs, §9 Constraints, §10 Developer-Product Surface, §7 counter-metrics.)*
- **NFR-1: Behavior preservation (dominant gate).** Release-gate conformance pass rate stays 100%; public contract shapes unchanged. Mirrors FR-20 / SM-C1 (inviolable).
- **NFR-2: Performance — no material hot-path regression.** Command/read hot-path latency must not regress; shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads (preserve snapshot/projection use). `[ASSUMPTION: "no material regression" ≈ within current benchmark noise — OQ-5.]` Mirrors SM-C2.
- **NFR-3: Fail-closed invariants.** Promoted tenant-access/authorization capabilities preserve fail-closed semantics *by construction*; cross-tenant access remains impossible and adversarially tested.
- **NFR-4: Observability preservation.** Metric names, dimensions, and health endpoints preserved through shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **NFR-5: Replay safety.** Promoted projection/event handling stays idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).
- **NFR-6: Additive cross-module compatibility.** Promotions edit sibling submodules (EventStore, Commons, Tenants, FrontComposer) additively/backward-compatibly; existing consumers (Folders/Projects/Memories/Parties/Tenants) keep compiling; dependent modules built in CI.
- **NFR-7: Public-surface stability.** Adopter-facing Conversations contracts and the EventStore-concept boundary (no raw envelopes leaked) preserved. Promoted technical-module APIs are new, additive, semver-versioned public surface.
- **NFR-8: Runtime targets unchanged.** net10.0, nullable, implicit usings, warnings-as-errors, Central Package Management.

**Total NFRs: 8.**

### Additional Requirements / Constraints

- **Success Metrics:** SM-1 (Conversations plumbing reduction, assumed ≥40% — OQ-2); SM-2 (new-module authoring cost, assumed ≥50% fewer files — OQ-2); SM-3 (duplication eliminated → one source of truth per promoted pattern); SM-4 (maintainer qualitative signal). **Counter-metrics:** SM-C1 (behavior/contract stability, inviolable); SM-C2 (hot-path performance).
- **Phasing:** Phase 0 Baseline (FR-1/FR-2/FR-19 baseline) → Phase 1 Consume (FR-3..FR-9) → Phase 2 Promote (FR-10..FR-16) → Phase 3 Adopt & Prove (FR-17..FR-20).
- **Non-Goals:** no fleet migration of other modules; no new domain features/contract redesign; no new persistence/transport/provider; no building unconsumed promotions; no UI/UX redesign; not a perf-tuning project.
- **Open Questions (5):** OQ-1 landing zone per promotion (architecture); OQ-2 confirm SM-1/SM-2 numeric targets; OQ-3 governance/temporal/hydration kept-now vs promoted-later; OQ-4 is FR-16 in pilot scope; OQ-5 explicit hot-path perf budget vs "no regression."
- **Addendum grounding:** ~35,769 LOC total; first-pass plumbing ~18,000 (~50%), **measured & accepted at 13,289 LOC (37.15%)** in `docs/release-evidence/consume-promote-keep-inventory-v1.json` (Story 1.4, 2026-06-03) — that inventory, not the estimate, is the SM-1 baseline. Consume-target surface catalog (§B), cross-module duplication evidence (§C), capability gaps (§D), and deferred architecture decisions (§E) all live in the addendum.

### PRD Completeness Assessment (initial)

- **Strengths:** Exceptionally well-structured for traceability — global stable FR IDs, each FR carries testable consequences, FRs map to user journeys (UJ-1..3) and success metrics, explicit phasing, explicit non-goals, an assumptions index, and a grounded evidence addendum. This is a strong base for epic↔FR coverage validation.
- **Watch-items carried into later steps:**
  1. **FR-16 is conditional (OQ-4 open).** Coverage analysis must treat it as in-scope-if-built and verify the decision is recorded, not silently dropped.
  2. **Numeric success targets are assumptions (OQ-2).** SM-1 ≥40% / SM-2 ≥50% are unconfirmed; the addendum already shows measured plumbing at 37.15% (< the assumed 40% baseline denominator) — epics must reference the *accepted inventory*, not the estimate.
  3. **Promote-later boundary (OQ-3).** Governance/temporal/hydration (areas 2,3,7) are "Keep-now"; epics must not implicitly promote them.
  4. **Architecture/UX docs predate this PRD** (2026-05-31 vs 2026-06-02) — alignment verified in later steps. Note this is a *refactoring* PRD, so heavy UX coverage is not expected (Non-Goal: no UI/UX change).

---

## Step 3 — Epic Coverage Validation

The epics document (`prds/prd-Conversations-2026-06-02/epics.md`) is **PRD-aligned by construction**: it re-states FR-1→FR-20 and NFR1→NFR8 verbatim from the canonical PRD and carries an explicit **FR Coverage Map**. 5 epics, 19 stories.

### Coverage Matrix

| FR | PRD Requirement (short) | Epic / Story | Status |
|----|--------------------------|--------------|--------|
| FR-1 | Canonical Consume/Promote/Keep inventory accepted + baseline LOC | Epic 1 / Story 1.4 | ✓ Covered |
| FR-2 | Classification dispute-resolution + reclassification escape hatch | Epic 1 / Story 1.5 | ✓ Covered |
| FR-3 | Domain-service host adoption (two-line shared host) | Epic 2 / Story 2.1 *(greenfield-adopt)* | ✓ Covered |
| FR-4 | SDK query-handler + cursor codec; remove HMAC cursor | Epic 2 / Story 2.3 | ✓ Covered |
| FR-5 | Read-model store + write policy | Epic 2 / Story 2.4 | ✓ Covered |
| FR-6 | Projection seam (delegate orchestration, keep logic) | Epic 2 / Story 2.5 | ✓ Covered |
| FR-7 | Aggregate base-class conventions | Epic 2 / Story 2.2 | ✓ Covered |
| FR-8 | Shared serialization converters / type registration | Epic 2 / Story 2.6 | ✓ Covered |
| FR-9 | Shared EventStore.Testing assertions/fakes | Epic 2 / Story 2.7 | ✓ Covered |
| FR-10 | Shared ServiceDefaults base | Epic 3 / Story 3.4 *(greenfield-adopt; FR-17 delete N/A)* | ✓ Covered |
| FR-11 | Generic tenant-access projection handler + registration | Epic 3 / Story 3.2 *(promote-then-delete; differential cross-tenant test)* | ✓ Covered |
| FR-12 | Generic typed-HttpClient registration | Epic 3 / Story 3.1 *(tracer-bullet, promote-then-delete)* | ✓ Covered |
| FR-13 | Shared Aspire/Dapr hosting base | Epic 3 / Story 3.5 *(greenfield-adopt; FR-17 delete N/A)* | ✓ Covered |
| FR-14 | Shared JSON-context base / polymorphic registration | Epic 3 / Story 3.6 *(greenfield-adopt; Commons `NameTypeMapper` micro-promote)* | ✓ Covered |
| FR-15 | Diagnostics/telemetry scaffolding helper | Epic 3 / Story 3.3 *(promote-then-delete)* | ✓ Covered |
| FR-16 | Compile-time command/event contract metadata | Epic 3 / Story 3.7 | ⚠️ Covered (CONDITIONAL — gated by OQ-4) |
| FR-17 | Conversations consumes every in-scope promotion (delete locals) | Epic 3 — folded into 3.1/3.2/3.3 delete steps; N/A for greenfield 3.4/3.5/3.6 | ✓ Covered (no standalone story by design) |
| FR-18 | Documented thin authoring template | Epic 4 / Story 4.1 | ✓ Covered |
| FR-19 | New-module authoring cost measured (SM-2 baseline) | Epic 4 / Story 4.2 | ✓ Covered |
| FR-20 | Behavior & contracts provably preserved | Epic 5 / Stories 5.1–5.3 + standing per-story conformance gate across Epics 2–4 | ✓ Covered |

### Missing Requirements

**None.** All 20 PRD FRs trace to an epic and story. No FR appears in the epics that is absent from the PRD (the epics' FR/NFR inventory is a verbatim restatement of the canonical PRD).

**Two structural notes (not gaps):**
1. **FR-16 (Story 3.7) is conditional** — its build depends on OQ-4. The epic correctly handles both branches (build vs record-as-backlog) and explicitly states the deferral path does **not** block FR-20. Acceptable, but OQ-4 must be resolved before Epic 3 reaches Story 3.7.
2. **FR-17 has no standalone story** — by deliberate design it is folded into each Epic 3 adopt story's delete step (3.1/3.2/3.3) and marked N/A for the three greenfield-adopt capabilities (3.4/3.5/3.6, which have no local copy to delete). This is a sound traceability decision, not a hole.

**Coverage beyond the FR list (strength):** Epic 1 adds non-FR "gate-zero oracle" work (Stories 1.1–1.3: pin conformance suite, measure blind spots, decouple internal-coupled tests) that FR-20 depends on. This *exceeds* the FR list and materially de-risks the refactor — a notable planning strength.

### Coverage Statistics

- **Total PRD FRs:** 20
- **FRs covered in epics:** 20 (FR-16 conditional on OQ-4)
- **Coverage percentage:** **100%**
- **NFR coverage:** NFR1–NFR8 ride as cross-cutting acceptance criteria on relevant stories (NFR1/NFR3 on Epic 2/3; NFR6 on every Epic 3 promote story; NFR2 on read-path stories; NFR4/NFR5 on telemetry/projection/tenant-access stories).
- **Epics:** 5 | **Stories:** 19 (1.1–1.5, 2.1–2.7, 3.1–3.7, 4.1–4.2, 5.1–5.3)

---

## Step 4 — UX Alignment Assessment

### UX Document Status

**Found on disk, but out-of-scope for the canonical initiative.** Three UX artifacts exist in planning-artifacts — `ux-design-specification.md` (117 KB), `ux-requirement-map.md` (8.8 KB), `ux-design-directions.html` — all dated **2026-05-31**, the same vintage as the **superseded feature PRD** (`../../prd.md`). They describe the *Conversations product UI/UX*, not this refactoring.

The **canonical PRD** (Boilerplate Reduction, 2026-06-02) explicitly excludes UX:
- PRD §5 Non-Goals: *"Not a UI/UX change — FrontComposer-generated admin surface behavior is preserved, not redesigned."*
- Epics §"UX Design Requirements": *"N/A — no UI/UX scope … the only 'users' are developers authoring Hexalith domain modules."*

### Is UX implied (and missing)?

**No.** This is an internal developer-platform refactoring. There is no user-facing surface in scope; the "users" are domain-module authors. UX documentation is therefore **not required** for this initiative, and its absence-from-scope is intentional, not a gap. Any existing UI behavior (FrontComposer-generated admin surface) is **preserved by construction** under FR-20's conformance gate — not redesigned — so it needs no separate UX↔PRD/Architecture alignment pass here.

### Alignment Issues

- **None blocking.** UX↔PRD and UX↔Architecture alignment checks are **not applicable** to the canonical refactoring PRD.
- ℹ️ **Informational (housekeeping):** The on-disk UX artifacts belong to the older *feature* initiative. Keeping them in the same planning folder as the refactoring PRD risks future confusion (mirrors the PRD-duplication issue from Step 1). Consider archiving the 2026-05-31 feature artifacts (`prd.md`, `ux-*`, possibly `architecture.md`) under a clearly-labeled folder once the refactoring is the active stream.

### Warnings

- ⚠️ **Carried into Step 5 (architecture alignment):** `architecture.md` (2026-05-31) is the **feature** architecture, same vintage as the superseded feature PRD. The canonical PRD states *"no separate architecture document yet … landing-zone decisions are deferred to a downstream `bmad-create-architecture` run (OQ-1); the addendum stands in for architecture-level technical input."* So for this initiative there is **no refactoring-specific architecture doc** — the addendum (§B–§E) is the architecture-level input, and **OQ-1 (landing zones) is an open architectural decision that gates Epic 3**. This is the most material readiness item and is examined next.

---

## Step 5 — Epic Quality Review

> **Framing — applied lens.** The classic best-practice rule *"epics must deliver user value, not technical milestones"* assumes a feature product. This is an internal **developer-platform refactoring**; the PRD names the "users" as domain-module authors / maintainers / release owners (UJ-1..3). I therefore judge each epic against *"does it deliver a coherent, releasable, independently-valuable increment to that audience"* — not against an end-customer UI. Epics that look "technical" are evaluated on that basis, and false positives are not raised as defects.

> **Critical context — this review is mid-flight, not pre-implementation.** Per `sprint-status.yaml` (last updated 2026-06-04): **Epic 1 = done (5/5), Epic 2 = done (7/7), Epic 3 = in-progress (Story 3.1 in *review*; 3.2–3.7 *backlog*), Epic 4 = backlog, Epic 5 = backlog.** Readiness findings below therefore split into *(a)* validation of the already-executed plan (Epics 1–2) and *(b)* go-forward readiness of the remaining work (Epic 3 tail, 4, 5).

### A. Epic Structure — User Value & Independence

| Epic | Audience outcome (developer-as-user) | Value verdict | Independence |
|------|--------------------------------------|---------------|--------------|
| 1 — Baseline & Behavior-Preservation Oracle | Trusted conformance oracle + accepted classification spine to refactor against | ✓ Load-bearing (you cannot safely refactor without it) | Stands alone; blocks Epic 2 |
| 2 — Consume Existing Technical-Module Surface | Conversations sheds hand-rolled plumbing with provably identical behavior | ✓ Coherent increment | Depends only on Epic 1 (backward) |
| 3 — Promote → Adopt (per-capability pipeline) | Each duplicated capability gets one tested shared home; Conversations adopts it | ✓ Each story independently shippable/revertable | Depends on Epic 2 + external OQ-1 (backward + external gate) |
| 4 — Thin Authoring Template & Cost Proof | New-module authors get a documented thin template + measured cost (the reusability payoff) | ✓ Clearest end-user value (UJ-3) | Depends on Epics 2–3 (backward) |
| 5 — Behavior-Preservation Attestation & Sign-off | Release owner's single signable attestation | ✓ Valid capstone deliverable (borderline-but-justified given inviolable gate) | Depends on all prior (backward) |

**No epic requires a later epic.** No forward epic dependencies, no circular dependencies. Epic ordering matches the PRD phasing (Phase 0→3). ✓

### B. Story Quality — Sizing & Acceptance Criteria

- **Sizing:** 19 stories, each scoped to a single capability/concern (one FR or one oracle concern). No "do everything" stories; no epic-sized stories. ✓
- **Acceptance criteria:** Consistently authored in **Given/When/Then BDD** form, testable and specific. Error/edge paths are genuinely covered — enumerated fail-closed states (missing/stale/unavailable/disabled/ambiguous/insufficient), adversarial cross-tenant differential tests (3.2), duplicate/out-of-order tolerance, byte-identical contract-shape diffs, and explicit *deferred-path* ACs for conditional work (3.7). This AC quality is **well above typical** and is a clear planning strength. ✓
- **Standing conformance gate** is attached as an AC to every Epic 2–4 story, with Story 5.1 even self-checking "if the full suite goes green here for the first time, the per-story gate failed" — strong defensive design. ✓

### C. Dependency Analysis

- **Within-epic dependencies are backward-only and well-formed** (e.g., 1.5←1.4, 2.x←Epic 1 oracle, 3.x←Epic 2 consume counterpart, 4.2←4.1, 5.3←5.1+5.2). ✓
- **Non-blocking forward *handoffs* (acceptable, by design):** 1.1 flags coupled suites *into* 1.3; 2.6 surfaces the `NameTypeMapper` micro-promote *consumed later* by 3.6; 3.1 (tracer-bullet) produces the runbook *for* 3.2–3.7. In each case the originating story completes on its own (with a documented fallback). These are handoffs, not blocking forward dependencies. 🟡
- **Database/entity timing:** N/A — refactoring, no new persistence model (PRD Non-Goal). Rule does not apply.
- **Greenfield/brownfield:** Brownfield ecosystem with **greenfield slots** (empty `Program.cs`/`ServiceDefaults`/`AppHost`, no JSON context). The epics correctly distinguish **greenfield-adopt** (no local copy; FR-17 delete N/A) from **remove-and-replace** (real code to delete) — sophisticated, correct handling. No starter-template story needed (module already exists). ✓

### D. Best-Practices Compliance Checklist

| Check | Verdict |
|-------|---------|
| Epic delivers (developer) value | ✓ all 5 |
| Epic functions independently | ✓ all 5 (backward-only) |
| Stories appropriately sized | ✓ |
| No blocking forward dependencies | ✓ (only non-blocking handoffs) |
| DB tables created when needed | N/A (no new persistence) |
| Clear, testable acceptance criteria | ✓ (strong, BDD, error paths) |
| Traceability to FRs maintained | ✓ 100% (Step 3) |

### Findings by Severity

#### 🔴 Critical Violations
**None.** No technical-epic-without-value (under the correct lens), no forward/circular epic dependency, no un-completable epic-sized story, no missing FR coverage.

#### 🟠 Major Issues (go-forward readiness)

1. **OQ-1 (landing zones) was never resolved by the planned architecture run — it is being ratified *inline, per story*.** The decision log still lists OQ-1 as "open question for `bmad-create-architecture`," and the Epic-2 retro explicitly carried "OQ-1 landing zones (hard pre-kickoff gate)" into Epic 3. Story 3.1 ratified *its own* zone at dev time (`Hexalith.Commons.Http`). The epic's gate is per-capability ("the landing zone for *its* capability"), so this is *consistent with the epic design* — but it means **each remaining Epic 3 story (3.2–3.7) still carries an unratified architecture decision that must be resolved before it starts.** *Remediation:* either run `bmad-create-architecture` to resolve OQ-1 holistically for 3.2–3.7, or keep enforcing the per-story zone-ratification precondition explicitly. Do not let 3.2 start "into the dark."

2. **Demonstrated high disposition-correction rate on story classifications.** Per `sprint-status` + the Epic-2 retro, **≥4 of 7 Epic-2 stories required their Consume/Promote/greenfield classification corrected at dev time** (FR-5 "remove-and-replace"→greenfield-adopt; FR-6 "Promote"→Consume; FR-8's named shared target didn't exist → deferred to FR-14/3.6; FR-9 duplicate-fakes → misclassification, Keep). The good news: the **Story 1.5 reclassification escape-hatch (FR-2) absorbed all of them with logged rationale** — the planning mechanism *worked*. The risk: the **remaining Epic 3 dispositions (promote-then-delete-local vs greenfield-adopt) are unverified estimates with an empirical ~50%+ correction rate.** *Remediation:* treat each 3.2–3.7 story as **verify-disposition-first** (confirm the local copy actually exists / the shared target actually exists) before committing to its promote/delete plan.

3. **HIGH open functional thread carried from Epic 2 — projection read-store population.** Story 2.4 built `ConversationProjectionReadModelWriter` but Story 2.5 explicitly left "production read-store-population a FLAGGED open thread" (no sync-over-async), and the Epic-2 retro carries "read-store population gap (2.4 writer built-but-unwired, HIGH)." If this ships unwired, projection **freshness/read behavior (FR-6/FR-20/NFR5)** is at risk. *Remediation:* assign an explicit owner/story for wiring the projection write path before the Epic 5 attestation; it is currently nobody's acceptance criterion.

4. **Open questions OQ-2/OQ-4/OQ-5 still gate downstream acceptance thresholds.**
   - **OQ-4** decides whether Story **3.7** (FR-16) is built at all — must be resolved before Epic 3 drains.
   - **OQ-2** sets the SM-1/SM-2 numeric targets that Stories **4.2** and **5.3** report against. ⚠️ Latent conflict: the accepted inventory measured plumbing at **37.15%**, while SM-1's assumed target is "**≥40%**." These are different denominators (≥40% *of classified plumbing removed* vs 37.15% *of total source classified as plumbing*), but the framing must be reconciled and the target confirmed before Story 5.3, or the success metric is unfalsifiable.
   - **OQ-5** (explicit hot-path perf budget vs "no regression") sets how NFR2/SM-C2 is verified.

#### 🟡 Minor Concerns

1. **Epic titles read as "technical"** (Epics 1/2/3/5) by classic feature-PRD standards. Correct and intentional for a developer-platform refactoring; noted only to pre-empt a misread by a reviewer applying the feature-product rubric literally.
2. **Non-blocking forward handoffs** (1.1→1.3, 2.6→3.6, 3.1→3.2–3.7) as described in §C — by design, each origin story self-completes.
3. **FR-8 ↔ FR-14 entanglement.** FR-8's substantive serialization consolidation was deferred out of Story 2.6 into Story 3.6 (FR-14). Both FRs are covered, but FR-8's real payload now rides in 3.6 — the FR-8/FR-14 boundary is blurred; ensure 3.6's ACs explicitly close FR-8's intent.
4. **Process state:** Story 3.1 sits in `review` **pending user approval** for the Commons submodule commit + root gitlink bump (external, out-of-band approval). Epic 3 cannot fully drain its first story without it. Also a recurring "Dev Agent Record count-drift" and "submodule gitlink drift" hazard is noted 7/7 in Epic 2 — a process-hygiene risk for Epic 3, not a planning defect.

---

## Summary and Recommendations

### Overall Readiness Status

**✅ READY — CONDITIONAL (proceed with the named gates enforced).**

The planning artifacts for the *Conversations Boilerplate Reduction* initiative are **complete, internally consistent, and traceable**: 100% FR coverage (20/20), NFRs carried as cross-cutting ACs, clean backward-only epic/story dependencies, BDD acceptance criteria of unusually high rigor, and correct brownfield/greenfield handling. Execution is **already mid-flight and healthy** — Epics 1 & 2 are done (12 stories), with conformance monotonically green (348→360) and an empty public-contract-shape diff on every story, demonstrating the behavior-preservation machinery (FR-20) actually works. This is **not a "not-ready" planning package**; it is a sound plan in active, disciplined execution.

The "conditional" qualifier reflects **0 Critical / 4 Major / 4 Minor** open items — none of which are planning *defects*; they are **gates the plan itself names** that must be actively closed before the corresponding upcoming stories (Epic 3 tail → 4 → 5).

### Critical Issues Requiring Immediate Action

There are **no 🔴 Critical (blocking) defects.** The 🟠 Major items below must be managed before the stories they gate:

1. **OQ-1 landing zones for Epic 3 Stories 3.2–3.7 are not architecture-ratified.** The planned `bmad-create-architecture` run never happened; OQ-1 is being resolved inline per story (3.1 → `Hexalith.Commons.Http`). Each remaining promote story carries an unresolved architecture decision as a precondition.
2. **OQ-4 must decide FR-16 / Story 3.7 (build vs defer-to-backlog)** before Epic 3 drains.
3. **HIGH open functional thread: projection read-store population** (2.4 writer built-but-unwired; 2.5 left production population a "flagged open thread") — currently nobody's acceptance criterion; risks FR-6/FR-20/NFR5.
4. **OQ-2 numeric targets unreconciled with measured reality** (accepted plumbing = 37.15% vs assumed SM-1 "≥40%"; differing denominators) — must be settled before Story 5.3's success-metric report; **OQ-5** (perf budget) likewise gates SM-C2 verification.

### Recommended Next Steps

1. **Resolve OQ-1 before Story 3.2 starts** — either run `bmad-create-architecture` to ratify landing zones holistically for 3.2–3.7 (tenant-access, telemetry, ServiceDefaults, Aspire, JSON-context, contract-metadata), or formally keep the per-story zone-ratification precondition and record each decision in `.decision-log.md`. Do not let 3.2 begin "into the dark."
2. **Make OQ-2, OQ-4, OQ-5 explicit decisions now** (they gate 3.7, 4.2, 5.3 and NFR2/SM-C2). At minimum, confirm/replace the assumed SM-1 ≥40% / SM-2 ≥50% targets against the accepted 13,289-LOC (37.15%) baseline so the metrics are falsifiable.
3. **Assign an owner/story for projection read-store wiring** before the Epic 5 attestation — close the Epic-2 carry-forward "read-store population gap (HIGH)."
4. **Run each Epic 3 story (3.2–3.7) verify-disposition-first** — confirm the local copy / shared target actually exists before committing to its promote-vs-greenfield plan; Epic 2 corrected ≥4/7 dispositions at dev time. The FR-2 / Story-1.5 escape-hatch is the right channel; keep using it.
5. **Approve (or reject) the pending Story 3.1 Commons submodule commit + root gitlink bump** so Epic 3's tracer-bullet can close; verify gitlinks pre-build (the recurring drift hazard).
6. **Ensure Story 3.6's ACs explicitly close FR-8's deferred serialization intent** (FR-8↔FR-14 entanglement).
7. **Housekeeping:** archive/relabel the superseded 2026-05-31 *feature* artifacts (`prd.md`, `ux-*`, `architecture.md`) out of the active planning folder to prevent the duplication confusion flagged in Step 1.

### Final Note

This assessment reviewed 4 canonical artifacts (PRD + addendum, epics, plus the inventory/decision-log/sprint-status) and identified **8 issues across 2 severity bands (0 Critical, 4 Major, 4 Minor)** spanning open architectural questions, open success-metric targets, one HIGH functional carry-forward, and document-hygiene. **None block continued implementation of the in-flight plan**; each Major item is a gate the plan already anticipates and must be closed before the story it governs. The recommended path is to **proceed with Epic 3 while resolving OQ-1/OQ-2/OQ-4/OQ-5 and the read-store gap as explicit, logged decisions.** These findings can be used to harden the artifacts, or you may proceed as-is with the gates enforced.

---

**Assessed by:** Implementation Readiness workflow (Product Manager role) · **Assessor:** Jerome · **Date:** 2026-06-08
**Canonical set:** `prds/prd-Conversations-2026-06-02/{prd,addendum,epics}.md` · `architecture.md` (feature, reference-only) · `ux-*` (feature, out-of-scope) · `implementation-artifacts/sprint-status.yaml`
