---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-01b-continue
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
nextStep: complete
completedAt: "2026-05-10"
sessionExitedAt: "2026-05-10"
releaseMode: phased
inputDocuments:
  - _bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md
  - _bmad-output/planning-artifacts/product-brief-Hexalith.Conversations-distillate.md
documentCounts:
  briefs: 2
  research: 0
  brainstorming: 0
  projectDocs: 0
workflowType: 'prd'
author: Jerome
projectName: Hexalith.Conversations
date: 2026-05-09
classification:
  projectType: api_backend
  domain: general
  domainCharacteristics:
    - audit_governed
    - event_sourced_substrate
    - multi_tenant_fail_closed
    - ai_participant_modeling
  complexity: high
  complexitySignature:
    - event_sourcing_semantics
    - multi_tenant_fail_closed
    - governance_lifecycle
    - cross_module_contracts
    - ai_party_modeling
  projectContext: greenfield_in_brownfield_ecosystem
  inheritedConstraints:
    - hexalith.eventstore
    - hexalith.tenants
    - hexalith.parties
    - hexalith.projects
    - hexalith.folders
    - hexalith.frontcomposer
carryForwardCallouts:
  - id: 1
    text: "Three first-class PRD chapters: Governance & Audit Invariants, Consumer Contracts & DX, Cross-Module Integration"
  - id: 2
    text: "Audit invariant catalog (property-based, not example-based)"
  - id: 3
    text: "Adversarial conformance tests — fail-closed matrix, cross-tenant zero-access as a gate"
  - id: 4
    text: "Redaction-replay correctness proof obligation"
  - id: 5
    text: "Idempotency property tests"
  - id: 6
    text: "Event schema evolution / upcasting strategy (promoted from sleeper to first-class)"
  - id: 7
    text: "Temporal correctness of governance state (highest-risk miss)"
  - id: 8
    text: "Audit-the-audit recursion — retention/redaction policy on the audit log itself"
  - id: 9
    text: "Platform commitment / SLA / deprecation policy"
  - id: 10
    text: "Strategic narrative anchor: shared memory backbone, governed by construction, with operational rigor as the proof"
  - id: 11
    text: "Buyer of v1 explicitly named with acceptance authority (separate from first adopter)"
  - id: 12
    text: "Audit invariant enforcement mechanism specified at code level (base type / analyzer / property test), not procedural"
  - id: 13
    text: "Second adopter named as candidate OR strategic framing softened to match v1 reality"
  - id: 14
    text: "v1-vs-vNext governance scope distinction — what is mandatory in v1, what is deferred"
  - id: 15
    text: "PRD acceptance gates — promote callouts 1-3 to PRD is not complete unless Section X with subsections Y exists"
  - id: 16
    text: "Carry-forward list is the binding contract — downstream steps must honor each callout as a question to answer"
  - id: 17
    text: "Buyer of v1 (working assumption): Hexalith platform owner (Jerome); bar = v1 makes credible commitments to future adopters (semver, contract package, deprecation policy, integration sample, conformance suite — concrete & testable)"
  - id: 18
    text: "Second adopter candidate (working assumption): none named yet → strategic framing softened to chatbot persistence + credible extensibility commitments. Explicit downgrade rule: if v1 ships without a candidate, ecosystem gravity claim drops to extensibility commitments in executive summary — not a moat claim"
  - id: 19
    text: "v1-specific vision (working assumption): success = chatbot end-to-end loop AND second-adopter integration path is credible (contracts published, ergonomics validated). Explicit anti-scope: no semantic memory, vector search, summarization, branching, multi-agent planning workflows, full compliance automation"
  - id: 20
    text: "Operator/admin differentiation moment (committed v1 deliverable): I can prove what the AI saw, when, and that we redacted it. Promoted from working assumption to required v1 deliverable — without it, governance is rhetorical"
  - id: 21
    text: "Value proposition (locked, two-beat): When AI agents help your team work, the conversation IS the record — not a chatbot artifact you'll lose. Hexalith.Conversations makes that record durable, tenant-safe, and auditable by construction, so every Hexalith AI surface — chatbot today, agents tomorrow — shares one memory you can prove."
  - id: 22
    text: "V1-staging principle: ship the substrate the chatbot needs + credible v2 hooks; don't ship v3. Add explicit what's-NOT-in-v1 section to PRD"
  - id: 23
    text: "Operator-facing v1 deliverable: at least minimal FrontComposer-driven retention/redaction view in v1 — proves the audit invariant is real, not just architectural"
  - id: 24
    text: "Provider-portable proof obligation: PRD must specify how portability is verified (migration test? schema commitment? something testable). Otherwise drop the claim"
  - id: 25
    text: "Hexalith Standard / ADR commitment in v1: Hexalith-level governance artifact saying use Conversations for any AI-assisted exchange — turns recommendation into default"
  - id: 26
    text: "Reference integration sample beyond the chatbot in v1: a second consumer pattern (even synthetic) shipped with v1 to prove the contract is consumer-agnostic"
  - id: 27
    text: "Adopter-facing vs. buyer-facing pitch separated: two value props, one vision — adopter cares about ergonomics + correctness; buyer cares about platform commitments + ecosystem gravity"
  - id: 28
    text: "Conformance suite definition: versioned manifest of test IDs, each mapped to a binding callout, explicit pass criteria (e.g., 0 leaks across N runs), signed CI artifact, named-waiver process — must gate release, not just run"
  - id: 29
    text: "P95 latency envelope: ≤ 500ms at conversation depth ≤ 500 msgs warm cache, ≤ 20 humans + 5 AI agents, 50 concurrent opens/sec/tenant; cold start separate target; latency without concurrency is microbenchmark, not product property"
  - id: 30
    text: "Missing technical metrics needed: throughput/load (events/sec, concurrent conversations, write-amplification budget), projection rebuild time at 1M/10M/100M events, schema migration SLO, pub/sub at-least-once+idempotency tested together with induced fault, audit-write failure mode (block fail-closed, never queue), tenant-events lag SLO, redaction latency SLO, DAPR sidecar restart + chaos tests, observability cardinality + retention SLAs"
  - id: 31
    text: "Property test enumeration (6 named suites with formal statements): idempotency, audit pairing, tenant isolation, redaction-replay, temporal monotonicity, schema upcasting; honest cost ~3 senior-engineer-months + 0.25 FTE ongoing for generator maintenance"
  - id: 32
    text: "Operator success criterion REWRITTEN as workflow not query: locate by external identifier, read reconstructed transcript with inline attributed redactions, time-travel view, view governance audit trail inline, generate signed evidence bundle (3 screens: Find → Read → Leave with)"
  - id: 33
    text: "Operator v1 vs v1.1 split: full evidence-bundle export (signed, hash-chained, defensible-via-email) deferred to v1.1; v1 ships read-only viewer (Find + Read screens) — v1 = governance dashboard defensible to on-site auditor; v1.1 = governance product defensible via email at 2:47pm"
  - id: 34
    text: "Audit invariant enforcement DECISION: aggregate base type (GovernanceAggregateRoot.Mutate(cmd, out auditEvent)) as PRIMARY (3-5 days), property test pairing assertion as SAFETY NET (1-2 days); Roslyn analyzer DROPPED from v1"
  - id: 35
    text: "Conformance suite v1 scope: ~30 enumerated cases (~5 weeks dedicated test engineering); enumeration owner = threat-model session with Architect + TEA + Hexalith.Tenants owner; PRD claiming 'release gate' without naming enumeration owner is process gap"
  - id: 36
    text: "Reference integration sample HONEST framing: synthetic sample = self-test, not portability evidence; real portability requires different repo / frozen contract package / version skew test"
  - id: 37
    text: "Conditional cut order if ship window tightens (most-cuttable first): (1) reference integration sample beyond chatbot, (2) Hexalith Standard ADR → v1.1, (3) operator FrontComposer view full → v1 read-only viewer, (4) provider portability migration test → v1.1, (5) UpdateRetentionPolicy merged into SetRetentionPolicy, (6) temporal correctness property test → example tests for 5-8 scenarios; do NOT cut: tenant isolation conformance, audit base type, idempotency property test, schema evolution strategy ADR, code-level governance enforcement"
  - id: 38
    text: "Schema evolution v1 scope: strategy ADR (3-5 days) + event envelope with schema version field (1 day) + 1 worked additive-change example (2 days) = ~1.5 weeks for v1; full upcasting framework → v1.1 (2-3 weeks + ongoing maintenance)"
  - id: 39
    text: "Engineering cliff acknowledged: ~12 weeks foundation (property test harness 3-4w + conformance suite 5w + audit base type 1w + redaction-replay comparer 4d + clock injection 1w + envelope+ADR 1.5w) + ~4-6 weeks features = ~16-18 weeks honest v1; PRD must commit to either expanded timeline or contracted criteria before kickoff"
  - id: 40
    text: "V1 deal pick — Option A (working assumption): chatbot ships at GA; extensibility commitments are v1.1 deliverable with hard date GA+90, gated by buyer not chatbot PM; substrate framing aspirational at GA, earned at GA+90. (Alternative Option B: extensibility blocking for GA, chatbot ships to production-pilot first — Hexalith Standard stamp at full GA)"
  - id: 41
    text: "Cross-module adoption target REVISED: 1 second adopter in active integration by GA+6mo, live by GA+12mo (replaces ≥ 2 modules in 6 months as wishful without named candidate); reverts to ≥ 2 modules at GA+6mo if a named second adopter with written 1-page integration intent is identified before GA"
  - id: 42
    text: "Downgrade rule TRIGGERABLE: trigger = GA+6mo with zero second adopters in production AND no active integration; consequence = docs/README/roadmap reframe from 'substrate backbone' to 'conversation persistence with extensibility primitives' within 30 days, ADR amended; owner = Jerome (platform owner) signs the framing change"
  - id: 43
    text: "MVP force-rank into CORE / SUFFICIENT / NICE-TO-HAVE — CORE: aggregate, chatbot-loop subset of commands (4-5 of 9, sequence to be mapped), chatbot-needed projections (2-3 of 9), EventStore+idempotency+pub/sub, fail-closed tenant isolation, sensitive-data policy, code-level governance enforcement; SUFFICIENT (v1.1 acceptable): remaining commands, remaining projections, FrontComposer metadata, operator viewer, schema evolution strategy doc only, semver contract package + deprecation, Hexalith Standard ADR; NICE-TO-HAVE: full conformance suite (skeleton in v1, fill when adopter #2 shows), reference integration sample beyond chatbot (only if real second adopter); honest MVP = 9-11 items, not 20"
  - id: 44
    text: "Business success cliffs ranked: Hard fail = compliance audit failure / tenant breach / sensitive-data leak post-GA → buyer pulls plug; Strategic fail = GA+12mo with zero second adopters in production AND no active integration → substrate claim dead, retitle as chatbot persistence library; Tactical fail = chatbot regression vs pre-Conversations baseline → first adopter walks; real cliff = strategic at GA+12mo"
  - id: 45
    text: "Success criteria RESTRUCTURED: Buyer acceptance is PRIMARY, first-adopter acceptance is instance proof (currently inverted — chatbot PM criteria explicit, buyer criteria implicit; reversed)"
  - id: 46
    text: "4 OPEN QUESTIONS to lock before sign-off — Q1 (Murat): commit conformance manifest + signed CI + named-waiver process in writing? Default YES (encoded in carry-forward 28); Q2 (Sally): is Generate Evidence Bundle in v1 scope? Default NO — defer to v1.1, v1 ships read-only viewer; Q3 (Amelia): chatbot ship deadline + is chatbot blocked? Default TBD — assume blocked, cut to CORE-only; Q4 (John): who commits to downgrade-rule public framing change? Default Jerome (platform owner per carry-forward 17) ✓ LOCKED"
  - id: 47
    text: "Citation affordances on Read view: every element has paste-ready citation block (audit ID, ISO timestamp, actor, hash, conversation ID, tenant ID) — not just URL copy"
  - id: 48
    text: "Permalink URLs encoding temporal cursor: Read view URL re-resolves identically forever, including time-travel cursor position"
  - id: 49
    text: "OPEN QUESTION (Architecture confirmation): does pre-UI-rollout attribution exist in event log for migrated tenants? Sarah's Wednesday branch promises it; cannot fudge. If no → drop claim, backfill (expensive), or restrict v1 to in-Conversations history. Decision-blocking before final lock"
  - id: 50
    text: "Cross-tenant SRE actions land as audit events in affected tenants: every SRE-tooling invocation that touches tenant data lands in that tenant's audit stream with structured justification field; frame inverts (SRE outside the frame, actions recorded into the frame from outside, with attribution); ~2-3 wks engineering, not in original cliff estimate"
  - id: 51
    text: "Self-serve demo path: 5-minute path with seeded data demonstrating one redaction + one time-travel + one citation copy + one cross-tenant denial; required for buyer GA acceptance to be evidence-based not faith-based"
  - id: 52
    text: "Marcus v1 SRE surface = CLI (`conformance verify` with --suite/--tenant/--since) + runbook page + structured (JSON) output + per-tenant audit-pairing health status endpoint (continuously-running monitoring projection — adds to projection count, ~1.5 wks)"
  - id: 53
    text: "TCP commitment trigger: Target Adopter Profile must be populated with at least one named candidate (real org, written 1-page integration intent or LOI) by GA−60 days; if unmet, buyer (Jerome) chooses (a) aggressive candidate-pursuit sprint, (b) accept downgrade-rule risk and proceed to GA, (c) push GA out 30 days; if GA proceeds without populated TCP, v1 GA docs carry explicit aspirational-framing annotation; owner Jerome"
  - id: 54
    text: "Voice register variation for Julian/Helen/Naomi/Daniel is described in journey summaries; full prose drafting must verify register differentiation on actual narrative; risk = same writer's voice in different costumes (Paige); review pass before architect agent consumes"
  - id: 55
    text: "Stable-ID indirection commitment: Conversations stores stable IDs to upstream-owned entities (Party, Project, Folder); v1 does NOT subscribe to upstream lifecycle events; read-time resolution uses upstream module's current canonical state; cross-module orchestration (Project move, Folder reorganization, Party deactivation cascade, agent runner following thread across products) is owned by upstream modules in v1, vNext for full orchestration"
vision:
  realProblem: "AI-assisted work has no defensible business record — every AI surface invents its own storage, tenant filtering, and audit story, fragmenting at the substrate layer"
  futureState: "Business users continue work in full context (via adopters); AI agents recover decisions and cite attachments; LLMs participate as attributable parties; operators can prove what the AI saw, when, and that redactions happened — by construction, not by audit retrofit"
  whyNow: "Chatbot persistence unblock + strategic window before AI surfaces proliferate and fragment the substrate"
  coreInsight: "Conversations with AI are business artifacts — like invoices, contracts, support tickets — owned by the business, attached to work, durable across the tools that touch them"
  differentiators:
    - "Event-sourced + tenant-isolated by construction (fail-closed, not opt-in) — provable, ordered, replayable history"
    - "Business-context-native (attaches to projects/folders/parties as first-class)"
    - "Provider-portable AI history — app owns the record; provider IDs are metadata; portability is a proof obligation, not a claim"
    - "Governance built into the lifecycle (retention/redaction/sensitive-data as domain commands, with operator-facing v1 deliverable that proves the invariant)"
    - "Ecosystem gravity productized — Hexalith Standard + conformance suite + reference integration sample + contract stability commitments"
  valueProposition: "When AI agents help your team work, the conversation IS the record — not a chatbot artifact you'll lose. Hexalith.Conversations makes that record durable, tenant-safe, and auditable by construction, so every Hexalith AI surface — chatbot today, agents tomorrow — shares one memory you can prove."
  visionLevelRisks:
    - "Over-specification risk: event-sourcing edge cases (schema evolution, projection rebuild, redaction-replay) swallow v1 ship-window"
    - "Operator-substrate-trap: governance substrate ≠ governance product unless operator can use it in v1"
    - "Contract-fork risk: without explicit contract stability commitments in v1, shared protocol becomes shared starting point that everyone forks"
---

# Product Requirements Document - Hexalith.Conversations

**Author:** Jerome
**Date:** 2026-05-09

## How to Read This PRD

This PRD serves two audiences: human reviewers making product and release decisions, and downstream LLM agents creating architecture, epics, stories, tests, and documentation. Use the following precedence rules when sections appear to overlap:

1. **Project Scoping & Phased Development** governs release timing and delivery sequencing.
2. **Functional Requirements** and **Non-Functional Requirements** define the full capability contract, not automatic v1 scope.
3. **Success Criteria** and **User Journeys** explain why the capabilities matter and how they will be recognized by stakeholders.
4. **Anti-scope lists** intentionally repeat boundaries from different planning angles; they apply unless a later approved PRD revision explicitly promotes an item.
5. **Open questions** are unresolved and must not be assumed closed by downstream planning.

Canonical scope vocabulary: **Full Capability Contract**, **MVP / v1 Release Scope**, **Post-v1**, **Explicitly Out of Scope**, and **Open Question**. If wording conflicts, prefer the canonical vocabulary and the phased plan.

## Executive Summary

Hexalith.Conversations is a tenant-isolated, event-sourced conversation aggregate and projections module that owns the durable business record of AI-assisted exchanges — between humans, AI agents, and LLMs — inside the Hexalith ecosystem. It is not a chatbot, not a transcript table, and not an attempt to compete with LLM provider chat history; it is the shared substrate other modules attach to so each new AI surface stops reinventing storage, tenant filtering, and audit.

The first adopter is the Hexalith chatbot, which needs durable, governed conversation persistence as a near-term unblock. The strategic motivation is broader: build the shared memory layer once, before more Hexalith AI agents proliferate and each invents its own model. v1 succeeds when the chatbot completes a full **create → append → attach → govern → resume** loop end-to-end, *and* v1 ships with the artifacts that make a second adopter's path credible — published contracts, a conformance suite, a reference integration sample beyond the chatbot, and explicit contract-stability commitments — even if the second adopter is not yet live. The buyer of v1 is the Hexalith platform owner, betting on a shared substrate; the first adopter is the chatbot product owner, who has a deadline.

**Target users.** Primary: business users (resume work in full conversation context via adopter UIs) and AI agents (recover decisions, cite attachments, coordinate without restating context). Critical secondary: platform operators and compliance stakeholders, who must be able to prove **what the AI saw, when, and that redactions happened** — by construction, not by audit retrofit. Adopters: chatbot, application, and agent-runner developers, who integrate via reusable contracts and client packages without learning EventStore internals or duplicating tenant checks.

**The problem.** AI-enabled applications today fragment conversation state. Each chatbot stores transcripts locally; each agent framework keeps provider-specific sessions; every application bolts on file references and audit fields. The result: provider-coupled history, per-surface tenant isolation, decontextualized attachments, no stable actor identity, retention/redaction as afterthoughts. Every new AI surface pays the integration tax. Hexalith.Conversations dissolves that tax by owning the conversation as a tenant-scoped, event-sourced business record that every Hexalith AI surface can attach to.

### What Makes This Special

The load-bearing insight is a reframe: **conversations with AI are business artifacts — like invoices, contracts, and support tickets — owned by the business, attached to work, and durable across the tools that touch them.** That single shift makes governance, party modeling, cross-module integration, and provider-portability natural rather than bolted-on.

Five concrete differentiators follow from the reframe:

1. **Event-sourced and tenant-isolated by construction.** Every state change is a domain event; tenant decisions are consumed from `Hexalith.Tenants` projections with fail-closed semantics on missing or stale state. Provable, ordered, replayable history is a property of the design, not a feature to enable.
2. **Business-context native.** Conversations attach to projects (`Hexalith.Projects`), folders and files (`Hexalith.Folders`), and parties (`Hexalith.Parties`) as first-class links with stable IDs and clear ownership. They are not isolated transcripts.
3. **Provider-portable AI history.** The application owns the durable record; LLM provider session and response IDs are stored as metadata, not as authority. Portability is treated as a proof obligation in this PRD, not a marketing claim.
4. **Governance built into the lifecycle.** Retention, redaction, and sensitive-data handling are domain commands with paired audit events — not external scripts. The audit invariant *no path mutates governance state without an audit record* is enforced at the code level (e.g., aggregate base type / analyzer / property test), not by procedure. v1 ships an operator-facing surface — a FrontComposer-driven retention/redaction view — that proves the invariant is real, not architectural rhetoric.
5. **Ecosystem extensibility productized in v1.** Reusable contract and client packages with explicit semver and deprecation policy; a published conformance suite consumable by adopters; a Hexalith Standard ADR committing *use Conversations for any AI-assisted exchange*; and a reference integration sample beyond the chatbot. These are deliverables, not slogans — they are the mechanism by which the second adopter's path stays credible.

**Why a stranger should care, in two beats.** When AI agents help your team work, the conversation IS the record — not a chatbot artifact you'll lose. Hexalith.Conversations makes that record durable, tenant-safe, and auditable by construction, so every Hexalith AI surface — chatbot today, agents tomorrow — shares one memory you can prove.

**What v1 explicitly does NOT include.** Branching or forked conversations; semantic memory, vector search, automatic summarization; chatbot UI and orchestration; LLM provider abstraction beyond storing correlation IDs; real-time collaborative editing or live streaming; multi-agent planning workflows; attachment binary storage (owned by `Hexalith.Folders`); full compliance automation per retention or classification regime. These remain in the long-term vision but are out of v1 scope to preserve the chatbot ship-window and avoid spec'ing v3 invariants in v1.

## Project Classification

| Field | Value |
|---|---|
| **Project Type** | `api_backend` — domain service module exposing commands, projections, and pub/sub events; consumed by other Hexalith modules via reusable contracts and client packages. The "Consumer Contracts & DX" surface is treated as a first-class PRD chapter, not an NFR. |
| **Domain** | `general` (closest CSV match) with load-bearing characteristics: `audit_governed`, `event_sourced_substrate`, `multi_tenant_fail_closed`, `ai_participant_modeling`. The classification is metadata; the carry-forward list is the binding contract. |
| **Complexity** | `high` — signature: event-sourcing semantics, multi-tenant fail-closed isolation, governance lifecycle, cross-module contracts, AI-party modeling. |
| **Project Context** | `greenfield_in_brownfield_ecosystem` — new module conforming to existing Hexalith conventions, with inherited constraints from `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.Projects`, `Hexalith.Folders`, `Hexalith.FrontComposer`. |

The classification is metadata. The binding contract for the rest of this PRD is the **carry-forward callout list (55 recorded items, captured in document frontmatter, with proposed CF 56-58 noted later)**, which downstream sections must honor as questions to answer — most critically the three first-class chapters required by design: **Governance & Audit Invariants**, **Consumer Contracts & DX**, and **Cross-Module Integration**.

## Success Criteria

### User Success

**For business users (via adopter UIs).**

A returning user retrieves a prior conversation with full participants, message timeline, and attachment context in a single request, without searching across chat windows, file systems, or notes.

- **Measurable:** P95 round-trip time for "open conversation" returning full context ≤ **500ms** under defined envelope: conversation depth ≤ 500 messages, ≤ 20 human participants + ≤ 5 AI agents, warm cache, at 50 concurrent opens/sec/tenant. Cold-start has a separate target. Latency without concurrency is a microbenchmark, not a product property.
- **Qualitative:** zero "where's the rest of the conversation?" support tickets traceable to substrate fragmentation.

**For AI agents.**

An agent recovers prior decisions, cites attachments, and continues a workflow without asking the user to restate context.

- **Measurable:** agent integration tests demonstrate state recovery across restart/resume scenarios without user prompting; correlation IDs preserved across LLM provider boundaries.

**For operators / compliance stakeholders (defining differentiation moment).**

A compliance operator can, within **90 seconds and without engineering help**, locate a specific tenant's conversation by **external identifier** (customer email / account / case ID — not internal GUID), read the **reconstructed transcript** with all redactions visibly marked and attributed (who, when, why, under which policy), view the complete governance audit trail inline, and **reconstruct the conversation's state at any prior point in time** — all within a tenant-scoped UI surface that makes cross-tenant errors structurally impossible. Every governance state change has a paired audit event with rationale, verified by property test.

The v1 operator surface is a **read-only governance viewer** (Find + Read screens, time-travel, attributed redactions). The full **Generate Evidence Bundle** workflow (signed, hash-chained, cryptographically provenanced export — the "Leave with" screen) is **deferred to v1.1**. v1 makes the governance claim *defensible to an on-site auditor*; v1.1 makes it *defensible via email at 2:47pm without engineering help*.

### Business Success

**Buyer acceptance (PRIMARY — Hexalith platform owner = Jerome):**

- **Audit-governed controls operational:** audit invariant enforced at code level (aggregate base type as primary mechanism, property test as safety net); 100% of governance state changes paired with audit events with same correlation_id, same tenant, causally adjacent, within transaction boundary.
- **Tenant isolation verified:** zero cross-tenant access in adversarial conformance tests; fail-closed verified for missing / stale / lagging / rolled-back tenant projection state; conformance suite signed by CI as a release gate.
- **Sensitive-data policy enforced:** redact projected/displayed content; preserve audit event stream; binary content remains in `Hexalith.Folders`.
- **Extensibility commitments shipped per agreed deal** (see V1 Deal below).
- **Hexalith Standard ADR** ratified.
- **Deprecation policy** published.

**First adopter acceptance (instance proof — chatbot product owner):**

- v1 unblocks chatbot ship-window: chatbot completes **create → append → attach → govern → resume** end-to-end in production.
- No regression vs pre-Conversations baseline.
- Persistence reliable under load.
- Chatbot integration feature-complete by chatbot v1 GA *(date TBD)*.

**V1 Deal — Option A (working assumption).**

Chatbot ships at v1 GA. Extensibility commitments are a v1.1 deliverable with a hard date (GA + 90 days), gated by the buyer not the chatbot PM. Substrate framing is *aspirational* at GA, *earned* at GA + 90. Alternative Option B (extensibility blocking for GA, chatbot ships to production-pilot first) is recorded as a switchable path if buyer prefers earlier extensibility certainty.

**Cross-module adoption target.**

- **1 second adopter in active integration by GA + 6 months, live by GA + 12 months.** (Replaces the prior "≥ 2 modules within 6 months" target as wishful without a named candidate.)
- If a named second adopter (with a written 1-page integration intent) is identified before GA, the target reverts to ≥ 2 modules in production by GA + 6 months.

**Downgrade rule (triggerable).**

- **Trigger:** GA + 6 months with zero second adopters in production AND no active integration in flight.
- **Consequence:** public framing in docs, README, and roadmap changes from *"substrate backbone for Hexalith conversations"* to *"conversation persistence with extensibility primitives"* within 30 days; ADR amended; marketing pages updated.
- **Owner:** Jerome (platform owner) signs the framing change.

**Business success cliffs (ranked).**

| Severity | Trigger | Outcome |
|---|---|---|
| **Hard fail** | Compliance audit failure post-GA / tenant boundary breach / sensitive-data leak | Buyer pulls the plug. Audit-governed means there is no choice. |
| **Strategic fail** | GA + 12mo, zero second adopters in production AND no active integration | Substrate claim dead; product survives as "chatbot persistence library." **Real cliff.** |
| **Tactical fail** | Chatbot regression vs pre-Conversations baseline | First adopter walks. Buyer's confidence collapses. Recoverable but expensive. |

### Technical Success

| Property | v1 Target | Verification |
|---|---|---|
| **Tenant isolation** | 0 cross-tenant access events; fail-closed on missing / stale / lagging / rolled-back tenant projection state | Conformance suite (~30 enumerated cases, signed CI artifact, named-waiver process) — **release gate** |
| **Audit invariant** | 100% of governance mutations paired with audit events with same correlation_id, tenant, transaction boundary | Aggregate base type (primary) + property test pairing assertion (safety net); Roslyn analyzer dropped from v1 |
| **Idempotency** | ∀ command c, apply(c) ∘ apply(c) ≡ apply(c) on every projection (per command, v1 scope) | Stateful property test with causality-aware generator |
| **Tenant isolation property** | ∀ event e in tenant T, e is invisible to ¬T across all projections, queries, snapshots | Property test, ≥ 2 tenants, N ≥ 1000 runs |
| **Redaction-replay correctness** | ∀ redaction r at time t, rebuild from t₀ never materializes redacted payload anywhere (projection, log, trace, error message) | Property test with redaction-aware projection comparer |
| **Temporal monotonicity (v1)** | Governance state queries reflect only events committed ≤ t (no time-travel leak) — 5–8 known temporal scenarios | Example tests in v1; full property test → v1.1 (requires deterministic clock injection architecture) |
| **Schema upcasting** | ∀ event e@v_n, upcast(e) round-trips through projection identical to native v_{n+1} event | Schema-rev test demonstrating older events project correctly |
| **Provider portability** | Conversation history fully recoverable independent of LLM provider session/response IDs | Migration test that swaps provider correlation schema |

**Additional technical success criteria (to be sized in NFR step):**

- **Throughput / load:** target events/sec, concurrent-conversations target, write-amplification budget for projections.
- **Projection rebuild time** at production volume (1M / 10M / 100M events) with explicit target — silent killer of ES systems.
- **Schema migration cost / SLO** for deploying a new projection on existing tenants.
- **Pub/sub data-integrity:** at-least-once + idempotency tested **together** with induced duplicates / reordering / dedup-window expiry.
- **Audit-write failure mode:** block governance writes (fail-closed) — never queue. Tested.
- **Tenant-events lag SLO** with defined behavior for requests during lag window.
- **Redaction latency SLO** — how fast redacted content disappears from queries / projections / logs / traces.
- **Resilience / chaos:** DAPR sidecar restart, event-store partition, projection-rebuilder crash-resume tests.
- **Observability:** trace / log / audit cardinality + retention SLAs.

### Measurable Outcomes

| Outcome | Target | Verification |
|---|---|---|
| Chatbot end-to-end loop | Feature-complete by chatbot GA | Chatbot integration test green |
| Cross-tenant zero-access | 0 access events in 100% adversarial runs (~30 cases, N ≥ 1000) | Conformance suite (release gate) |
| Governance audit invariant | 100% of governance mutations paired with audit events | Property test + aggregate base type |
| Single-request conversation retrieval | P95 ≤ 500ms under defined envelope | Benchmark test |
| Provider portability | 100% recoverability with provider IDs stripped | Migration test |
| Redaction replay correctness | Live projection ≡ rebuilt projection | Property test |
| Operator workflow (v1) | Compliance operator locates conversation by external ID, reads reconstructed transcript with attributed redactions, in ≤ 90 seconds without engineering help | Operator UX test |
| Buyer extensibility commitments (GA + 90, v1.1) | Published contract package + conformance suite + reference integration sample beyond chatbot + Hexalith Standard ADR + deprecation policy | Buyer sign-off |
| Cross-module adoption | 1 second adopter in active integration by GA + 6mo, live by GA + 12mo | Adopter inventory at GA + 12mo |
| Engineering timeline commitment | Either ≥ ~16–18 weeks honest v1 OR success criteria contracted to CORE-only MVP — picked **before** kickoff | Pre-kickoff timeline commitment |

## Product Scope

### MVP — Minimum Viable Product

This section preserves the discovery-stage tiering of the capability set. **Project Scoping & Phased Development** is the authoritative release plan; when this section and the phased plan appear to differ, use the phased plan for delivery timing and this section for rationale.

The MVP is **force-ranked** into three tiers. CORE is non-negotiable. SUFFICIENT may slip to v1.1 if the chatbot ship-window tightens. NICE-TO-HAVE is admitted as such.

**CORE (cut = no product):**

1. **Conversation aggregate** — tenant-scoped identity, ordered linear message history.
2. **Subset of commands closing the chatbot loop** (4–5 of the 9; chatbot's actual command sequence to be mapped explicitly during the architecture step). Likely candidates: `CreateConversation`, `AddParticipant`, `AppendMessage`, `AttachFileReference`, plus `RedactMessageContent` or `MarkSensitiveData` if compliance gates v1.
3. **EventStore persistence** with idempotent command handling and pub/sub event publication.
4. **Subset of projections the chatbot actually reads** (2–3 of the 9). Likely candidates: conversation detail, message timeline, attachment list.
5. **Fail-closed tenant isolation** via local projections of `Hexalith.Tenants` events.
6. **Sensitive-data policy** — preserve audit event stream; redact projected/displayed content; binary content owned by `Hexalith.Folders`.
7. **Code-level governance enforcement** — aggregate base type (`GovernanceAggregateRoot.Mutate(cmd, out auditEvent)`) as primary mechanism; property test pairing assertion as safety net.

**SUFFICIENT (cut acceptable; v1.1 deliverable):**

8. Remaining commands not on chatbot loop — `CloseOrArchiveConversation`, `UpdateTitleOrMetadata`, `SetRetentionPolicy` (with `UpdateRetentionPolicy` merged in as set-or-replace), and any of `MarkSensitiveData` / `RedactMessageContent` not in CORE.
9. Remaining projections (6–7 not on chatbot read path).
10. **FrontComposer command/projection metadata.**
11. **Operator-facing v1 surface** — read-only governance viewer (Find + Read screens, time-travel, attributed redactions). Full Generate Evidence Bundle workflow → v1.1.
12. **Event schema evolution strategy** — ADR + event envelope with schema version field + 1 worked additive-change example. Full upcasting framework → v1.1.
13. **Semver'd contract package** with explicit deprecation policy.
14. **Hexalith Standard ADR** ("use Conversations for any AI-assisted exchange") — written at v1 ship after decisions are real, not aspirational.

**NICE-TO-HAVE (admit it):**

15. **Conformance suite (full ~30 cases)** — build skeleton in v1; complete enumeration filled when adopter #2 shows up. (CORE within this: tenant isolation cases. SUFFICIENT within this: governance, redaction, retention adversarial cases.)
16. **Reference integration sample beyond chatbot** — only if a real second adopter is named. Synthetic sample is *self-test, not portability evidence*.

**Conditional cut order if ship-window tightens** (most-cuttable first):

1. Reference integration sample beyond chatbot
2. Hexalith Standard ADR → v1.1
3. Operator FrontComposer view full → v1 read-only viewer; full retention editor → v1.1
4. Provider portability migration test → v1.1 (no second provider known)
5. `UpdateRetentionPolicy` merged into `SetRetentionPolicy`
6. Temporal correctness property test → example tests; full property → v1.1

**Do NOT cut:** tenant isolation conformance (CORE cases), audit invariant base type, idempotency property test (per-command scope), schema evolution strategy ADR, code-level governance enforcement.

**Engineering cliff acknowledged.** ~12 weeks of foundation work (property test harness 3–4w + conformance suite 5w + audit base type 1w + redaction-replay comparer 4d + clock injection arch 1w + envelope+ADR 1.5w) + ~4–6 weeks of feature work for commands and projections = **~16–18 weeks for an honest v1**. The PRD must commit to either expanded timeline or contracted criteria *before* kickoff, not during sprint 4.

**Explicitly NOT in v1 (anti-scope):**

This list intentionally restates scope boundaries for implementation clarity. If wording differs from the phased plan, **Project Scoping & Phased Development** governs release timing.

- Branching or forked conversations
- Semantic memory, vector search, automatic summarization
- Chatbot UI and chatbot orchestration logic
- LLM provider abstraction beyond storing correlation/session IDs as metadata
- Real-time collaborative editing or live streaming UX
- Multi-agent planning workflows
- Attachment binary storage (owned by `Hexalith.Folders`)
- Full compliance automation per retention/classification regime
- Cryptographic redaction / crypto-shredding
- Multi-region replication / cross-region failover
- Roslyn analyzer for governance enforcement (aggregate base type sufficient for v1)
- Full upcasting framework (strategy ADR sufficient for v1)
- Generate Evidence Bundle workflow (read-only viewer sufficient for v1; bundle in v1.1)

### Growth Features (Post-MVP / v1.1+)

- Generate Evidence Bundle workflow (signed, hash-chained, cryptographically provenanced)
- Full upcasting framework (versioned event types, registered upcasters, replay-time application)
- Roslyn analyzer for governance enforcement (cross-aggregate diagnostic)
- Conversation summaries — automatic, on-demand, governed
- Decision and action extraction — promotable from conversation events to first-class entities
- Branching / forked conversations — for "what-if" agent reasoning
- Cryptographic redaction (crypto-shredding for hard-delete with audit preservation)
- Advanced retention automation — policy-driven expiry with auditable events
- MCP tools for agents — agents read/write conversations via MCP
- Per-message granular redaction beyond message-level
- Cross-region replication for HA/DR

### Vision (Future)

Hexalith.Conversations becomes the shared memory and audit layer for AI-assisted business workflows across Hexalith. Every chatbot, autonomous agent, project assistant, and admin tool uses the same conversation record. Business users gain continuity, agents gain structured context, operators gain a tenant-isolated audit trail. Long-term capabilities include semantic recall, decision/action extraction, full compliance workflows for major regimes (GDPR, SOC2, HIPAA), MCP tooling, and multi-region/cross-cloud replication. The long-term goal is not to compete with LLM provider chat history but to own the *business record* of AI collaboration inside Hexalith.

## User Journeys

### The Stories Assume

The journeys below rest on bets made explicit elsewhere in this PRD: that the chatbot ship deadline tolerates the engineering cliff (~16–18 weeks honest v1, carry-forward 39); that a real Target Adopter candidate is identified by GA−60 days (carry-forward 53); that the buyer signs partial acceptance per the Option A v1 deal (carry-forward 40); that the four open questions from Step 3 (carry-forward 46) plus the architecture-confirmation question on pre-UI-rollout attribution (carry-forward 49) resolve to their working-assumption defaults. If those bets fail, the journey set still describes the substrate's capabilities — but the narrative urgency of those capabilities collapses with the assumption.

### Journey 1 — Maya, the Business User: Resuming a Conversation Across Days

*What this journey proves: substrate makes multi-day continuity invisible until it saves the user from rebuilding context.*

Maya, project lead at a mid-size consulting firm (a Hexalith tenant), uses an AI chatbot embedded in her project workspace.

Tuesday morning, 9:14am. She needs to finalize a proposal she started discussing with the chatbot last Friday. Her brain has half the context; her notes have the other half. She braces herself to paste her notes back into the chat.

She clicks the conversation labeled *"Acme Corp proposal — strategy review."* The full thread reopens *exactly where she left it*: Friday's brief, the AI's drafted positioning angles, her counter-objections, the attached competitor analysis PDF, the three follow-up questions the AI raised at end-of-day. The AI's next turn references the second positioning angle by name and asks whether the executive summary should still lead with cost given a mid-week update from Acme. Maya doesn't restate context. She doesn't re-upload the PDF. She continues.

She finishes the proposal in 40 minutes instead of 90. The substrate's continuity across the multi-day gap was invisible — she didn't notice it working, which is how she knows it works.

**Capabilities revealed:**

- Conversation list projection: locate prior conversations by external context (project name) within the tenant's scope, returning conversation summaries in a single query.
- Conversation detail projection: load full participant set + ordered message timeline + attachment references in a single request, P95 ≤ 500ms under defined envelope.
- Stable-ID links: conversation references a project (by Hexalith.Projects ID) and an attachment (by Hexalith.Folders file ID); attachments retrievable via reference resolution against Hexalith.Folders.
- Tenant scoping: cross-tenant enumeration returns no results without revealing target-tenant existence.
- Provider-portable history: conversation continuity holds regardless of LLM provider session-ID lifecycle (provider session may expire between Friday and Tuesday; the conversation does not).

### Journey 2 — Atlas, the AI Agent: Resuming After Provider Failover

*What this journey proves: provider portability holds without customer-visible disruption.*

Atlas is an AI agent — an LLM-powered automated assistant — operating inside Hexalith on behalf of a tenant. Mid-task: drafting a quarterly business review outline. The conversation has 47 messages, 3 attached files, decisions made by both Atlas and the human CSM over two weeks.

The LLM provider returns 503. Then 503 again.

The orchestrator fails over to a secondary provider. The new provider has no session ID, no cached context, no prior turns. In a naive system, Atlas restarts cold and the CSM gets a chatbot that has forgotten the QBR account, the prior decisions, and the half-drafted outline.

But Atlas is not the source of truth for its own memory. The substrate is. Atlas queries the conversation by ID, retrieves the full timeline (47 messages, 3 attachment refs, all participant context, both providers' correlation IDs), and re-prompts the new provider with the conversation as context. The new provider's first turn references prior decisions correctly. The CSM, watching, doesn't see a hiccup.

The provider failover is a footnote in the audit trail.

**Capabilities revealed:**

- Conversation history retrieval is independent of any LLM provider's session or response IDs; provider IDs are participant-attribution metadata, not authority.
- Migration test: replacing the provider correlation schema does not change the recoverable conversation state; verified by automated test that swaps schemas mid-run.
- Both providers' correlation IDs preserved in the audit trail for forensic reconstruction (multi-provider attribution).
- Single-request retrieval returns full participant + timeline + attachment context (same shape as Maya's path).
- LLM party-modeling: each LLM has a stable Party identity; provider switch does not alter the LLM's identity in the conversation record (verified property; LLM does not require its own narrative journey).

### Journey 3 — Sarah, the Compliance Operator: 2:47pm SAR (defining differentiation moment)

*What this journey proves: governance is auditable by construction, defensible to an on-site auditor in v1.*

Sarah, the compliance lead at a mid-size insurer (a Hexalith tenant). The responsible person under GDPR / her firm's data subject access request policy.

Tuesday, 2:47pm. Phone buzzes. Legal: *"We got a subject access request. The customer claims the AI told them something incorrect about their policy on March 14th. They want the full record of the conversation, plus anything redacted, plus who saw it. We have 30 days."* Her hands are slightly cold.

She opens the Hexalith admin surface. Her tenant's banner is pinned to the chrome — she cannot drift into another tenant's data. She searches by the customer's external account number. The Find screen returns three conversations matching that customer and date range, summarized with participant counts, message counts, redaction counts, retention state. She clicks the one from March 14th.

The Read screen reconstructs the conversation as a transcript: human turns, AI agent turns, LLM turns, all clearly labeled, in order. Two redacted spans appear inline, visibly marked. She hovers — *"Redacted by ops@insurer.com on March 17 under retention policy 'PII-minimization-v2', rationale: 'customer SSN exposed in chat.'"* The sidebar shows the full audit log of every governance event against this conversation. The time-slider lets her view the conversation as it appeared on March 14th, before the March 17th redaction — proving the redaction was policy-driven, not retroactive whitewashing. She copies the audit log entries (each with a citation block: ID, timestamp, actor, hash) into legal's review document and emails it to outside counsel.

She responds within 72 hours instead of 30 days. The conversation is defensible.

**Wednesday — the suspicion-driven hunt.**

A second SAR arrives the next day. Sarah suspects the architecture must crack somewhere. *Every* governance system has a hidden flaw. She wants to find the lie.

She opens a Q1 2026 conversation. Two messages are missing from the reconstructed transcript. They were redacted in February 2026, before her tenant's governance UI rollout in April 2026. The Read view surfaces them as *"redacted prior to UI rollout — see audit log entries 8821, 8822"* with full attribution. Temporal correctness held.

Suspicious, she runs `governance verify --conversation X`. Output: *"100% governance mutations paired with audit events. invariant intact."* She tries to mutate retention policy in a way that bypasses the audit pairing — the system rejects with `audit_pairing_required`. The substrate does not break.

A third inquiry: a Q4 2025 conversation, predating Conversations module deployment for this tenant. Search returns: *"No Conversations records prior to 2026-03-15. For records prior, see legacy chatbot SQL store via [migration bridge link]."*

Grudging acceptance.

**Capabilities revealed:**

- Operator Find screen: search by external identifier (customer ID, account number, case ID), date range, with results showing participant count, message count, redaction count, retention state per conversation.
- Operator Read screen: reconstructed transcript with role-labeled participants (human / AI agent / LLM), inline redaction display with hover/click revealing actor, timestamp, rationale, policy ID per redaction.
- Inline governance audit trail per conversation, time-ordered, attributed.
- Time-travel view: reconstruct conversation state as of any prior date; audit trail unchanged by view-time changes.
- Empty-state semantics: tenant-scoped negative results are evidence ("no records for this customer in Conversations module"); UI surfaces this rather than silently failing.
- Tenant scoping persistent in UI chrome (frame, not dropdown).
- `governance verify --conversation X`: command (CLI or API surface; final form deferred to Architecture step) running audit pairing property test against a specific conversation; returns pairing percentage and any violations.
- Citation affordances: every Read view element has paste-ready citation block (audit ID, ISO timestamp, actor, hash, conversation ID, tenant ID) — not just URL copy.
- Permalink URL encoding the temporal-cursor position; resolves identically forever including time-travel.
- Migration boundary: explicit messaging when query crosses the pre-Conversations / post-Conversations boundary, with link to bridging documentation.
- v1 = read-only governance viewer (Find + Read screens). Full Generate Evidence Bundle workflow (signed, hash-chained export) is v1.1.

### Journey 4 — Diego, the Chatbot Developer: Integrating in a Sprint

*What this journey proves: integration ergonomics hold without leaking EventStore internals to the consumer.*

Diego is a senior engineer on the Hexalith chatbot team. Sprint goal: cut the chatbot over from its current SQL-table-with-audit-columns persistence to Hexalith.Conversations. He has two weeks. He has not used Hexalith.EventStore directly before.

Monday, sprint kickoff. He clones the chatbot repo, opens the Conversations contract package README. The first paragraph: *"To integrate, you do not need to understand EventStore. You issue commands and read projections through the client package. Five lines of code create a conversation, append a message, and read the timeline."*

Five lines work. His tests pass. He attempts a cross-tenant read, expecting a generic 500; he gets a typed error: HTTP 403, body shape `{ "error": "tenant_isolation_violation", "audit_id": "..." }`, with the target tenant ID elided from the message. He attempts to skip a tenant header; he gets a structured 4xx, fail-closed, with a clear error code and a link to tenant-binding documentation. He runs the consumer-side conformance tests shipped with the contract package; all pass.

Day 9. He deploys the chatbot to staging. The chatbot regression suite is green — no regressions versus the SQL-backed version. One observable change: when QA tests resume-a-week-old-conversation, the AI now actually remembers. The compliance team's test (force a redaction, query the conversation) returns a redacted message with attribution, instead of placeholder text with no provenance.

He ships the cutover on time. He writes a one-paragraph internal Slack: *"Took less time than I budgeted. The contract package surfaces what I needed; the rest stays inside the substrate. No EventStore knowledge required."*

That paragraph reaches the buyer. It is the first concrete evidence the developer-ergonomics success criterion is real.

**Capabilities revealed:**

- Reusable contract / client package: typed commands and projections; five-line happy-path integration; no leakage of EventStore concepts (idempotency tokens, projection mechanics, snapshots, replay) into consumer code.
- Cross-tenant access attempts return a typed error envelope (`tenant_isolation_violation` with audit ID), tenant-id elided from the message, logged with correlation-id, never leaking target-tenant existence.
- Missing-tenant-header requests fail closed with a structured 4xx, clear error code, and documentation pointer.
- Adopter-callable conformance tests shipped with the contract package: verify the consumer's integration before deployment without needing the service.
- Semver'd contract package + explicit deprecation policy: breaking changes signaled in advance; minor changes do not break consumers.
- Chatbot's CORE command sequence (subset of the 9, mapped during architecture step) suffices for the chatbot loop.

### Journey 5 — Marcus, the On-Call Engineer: 03:14am Alarm

*What this journey proves: aggregate base type enforcement makes the audit invariant runtime-immune to bypass even under fault.*

Marcus is the on-call SRE for the Hexalith platform. Pager rotation, week 3 post-GA.

03:14am. Page: `audit_write_degradation` for tenant T's governance mutation audit-event sink. Marcus opens his laptop, eyes half-closed.

The runbook page (linked from the alert) explains: audit-write failure mode is *fail-closed*. Governance mutations block with `audit_sink_unavailable`; non-governance commands continue. He checks the dashboard. Conversations command rejection rate for tenant T is non-zero on `RetentionPolicy` and `RedactMessageContent`; `AppendMessage` is unaffected. He runs the audit-sink health check, identifies the underlying state-store partition primary as stuck, and fails over the partition. Audit-write latency drops back under SLO. The fail-closed path unwinds automatically — no manual intervention required to "let through" anything.

He runs the verification: `conformance verify --suite audit_pairing --tenant T --since 03:00`. Output: *"0 unpaired governance events. invariant intact."*

Marcus sees what he came to see. During the degradation, *zero* governance mutations succeeded without audit pairing. The aggregate base type made bypass impossible at runtime — not procedurally, but by construction. The audit chain remembered, even while the write path degraded. The covenant did not break.

He files a 3-line incident: degradation, partition failover, conformance re-verified. He goes back to bed. The sleep is earned not because the alarm cleared but because the substrate kept its promise to a future regulator at 3am with no one watching.

**Capabilities revealed:**

- Audit-write failure mode is fail-closed: governance commands block with `audit_sink_unavailable` structured error; non-governance commands continue. Tested.
- Conformance suite is runnable in production for incident verification (not just CI release gating); supports `--suite`, `--tenant`, `--since` parameters.
- Aggregate base type enforces audit pairing at runtime (`GovernanceAggregateRoot.Mutate(cmd, out auditEvent)` signature cannot return without emitting both); bypass is impossible at runtime, not just by code-review discipline.
- Tenant projection lag SLO and degradation behavior are runbook-documented.
- Marcus's `conformance verify --tenant T` invocation lands as an audit event in tenant T's stream, with structured justification field referencing the incident ID. Cross-tenant SRE actions are recorded *into* each affected tenant's audit log (frame inverts: SRE outside the frame, actions audited into the frame from outside, with attribution).
- v1 SRE surface: CLI + runbook page + structured (JSON) output + per-tenant audit-pairing health status endpoint (continuously-running monitoring projection).

### Journey 6 — Julian, the Platform Owner: GA Acceptance Review

*What this journey proves: buyer-acceptance is concrete and demonstrable; partial acceptance is honest; downgrade-rule trigger is explicitly owned.*

Julian, the Hexalith platform owner. Buyer of v1. Final acceptance authority. The check stops with him.[^julian]

GA day. Conference room. The team walks Julian through buyer acceptance. He has the success criteria from this PRD in front of him.

He works the checklist. **Audit-governed controls operational** — Diego runs `conformance verify --suite audit_pairing` against production-equivalent staging; 100% pairing across N=10000 generated cases; signed CI artifact attached. ✓ **Tenant isolation verified** — adversarial conformance suite (~30 cases) runs against staging; 0-leak; signed CI artifact attached; named-waiver list = empty. ✓ **Sensitive-data policy enforced** — Sarah's read-only viewer reconstructs a test conversation with redaction inline; binary content remains in Hexalith.Folders. ✓ **No chatbot regression** — chatbot regression suite green. ✓ **Extensibility commitments** — published contract package (semver'd) ✓; conformance suite (versioned, signed) ✓; reference integration sample beyond chatbot — synthetic, labeled honestly per policy ⚠; Hexalith Standard ADR — drafted, slipping to GA+90 per Option A ⚠; deprecation policy ✓.

Julian's discomfort: the synthetic reference sample is honestly labeled. It is not a real second-adopter integration. The Target Adopter Profile has not yet been populated with a named candidate. He sees the gap clearly. He commits to GA anyway, accepting the downgrade-rule risk on his own calendar — not because the gap is tolerable, but because the alternative (slipping GA for hypothetical extensibility certainty) would punish the chatbot's deadline for a problem the chatbot didn't create.

He signs partial acceptance: v1 GA approved with the explicit note that Hexalith Standard ADR ratification is a v1.1 deliverable due GA+90, gated by him. He records the second-adopter status as *"no real candidate identified yet; downgrade-rule trigger window opens at GA+6mo, owner = Julian."* He signs the framing language: substrate framing is *aspirational* at GA, *earned* at GA+90.

His calendar carries two reminders: GA+90 (ADR ratification + extensibility re-review) and GA+180 (downgrade-rule trigger date — does any second adopter exist?).

**Capabilities revealed:**

- Buyer-acceptance checklist is concrete and demonstrable on GA day from a self-serve demo path: 5-minute walk-through with seeded data demonstrating one redaction, one time-travel, one citation copy, one cross-tenant denial.
- Conformance suite produces a signed CI artifact that the release process refuses to ship without; named-waiver process exists; waiver list is auditable.
- Option A v1 deal (chatbot ships GA, extensibility v1.1 by GA+90) is explicitly a *partial-acceptance contract*, not a clean GA.
- Downgrade-rule trigger calendar is owned by Julian (= Jerome), with explicit dates: GA+90 and GA+180.

[^julian]: Julian is the in-journey representation of the Hexalith platform owner Jerome (the PRD author). Named distinctly to avoid confusion between author and acceptance authority. All Julian's decisions trace back to Jerome's actual sign-off authority.

### Journey 7 — Helen, the CISO: Pre-procurement Module-Level Review

*What this journey proves: the system passes the module-level subset of an enterprise security review; platform-level concerns are explicitly deferred to the Hexalith stack.*

Helen, the CISO and Security Review lead at a procuring enterprise customer evaluating Hexalith. The Conversations module sits inside her broader Hexalith evaluation.

She does not approve the module in isolation; B2B platform purchases happen at platform scope. But within her broader review, Conversations carries five module-level concerns she will evaluate on its own evidence:

1. **Tenant isolation evidence.** She runs the conformance suite herself against a staging instance, with deliberately forged tenant claims. Every case fails closed. Signed CI artifact matches. ✓
2. **Audit invariant enforcement.** She reads the architecture decision: aggregate base type as primary mechanism, property test as safety net. She runs `governance verify` against a seeded conversation. 100% pairing. ✓
3. **Redaction integrity.** She runs a redaction-replay test: redact a message, rebuild projections from event 0, confirm redacted content does not materialize in any projection, log, trace, or error message. ✓
4. **Fail-closed verification.** She induces stale tenant projection state. Conversations rejects with structured error and audit logging. She induces missing tenant projection. Conversations rejects. ✓
5. **Conformance suite as release gate.** She inspects the CI signing process. Suite is versioned, test list frozen per release, named-waiver process requires human approval, release pipeline mechanically refuses to ship if the artifact is missing. ✓

She approves the module-level review.

She also names what is *not* in this PRD's scope but matters for her platform-level approval: SOC2 / ISO 27001 attestation, vulnerability disclosure policy, pen-test report, encryption at rest and in transit (assumed inherited from Hexalith.EventStore), key management, identity and authentication (assumed inherited from Hexalith.Tenants), data residency. Her conditional approval: *"Conversations module passes module-level evidence-based review for the 5 concerns above. The 7 platform-level concerns above are addressed by the Hexalith ecosystem's compliance program; my full sign-off on Hexalith depends on those."*

**What Helen cannot do with v1:** serve as the platform-level CISO sign-off; demand SOC2 attestation from this module alone; secure a vulnerability disclosure policy specific to Conversations.

**Capabilities revealed:**

- Adversarial conformance suite is consumer-runnable against a staging instance.
- Fail-closed semantics are testable from outside: induce stale / missing tenant projection state and observe structured rejection.
- Redaction-replay correctness is testable: redact, rebuild from event 0, confirm non-materialization across projections, logs, traces, errors.
- Conformance suite signing + named-waiver process is auditable from outside the team that builds it.
- Module-level vs platform-level scope distinction is documented; deferral targets (other Hexalith modules, ecosystem compliance program) are named.

### Scenario Sketch — Naomi, the Cross-Product PM (protocol-fit demonstrator)

*What this proves: the strategic moat is in the indirection, not the orchestration.*

A team member leaves the insurer. Hexalith.Parties deactivates the Party. The conversations that party participated in remain intact, attributable, and readable — the conversation's stable Party reference survives the lifecycle change because Conversations holds Party IDs, not Party state. Same pattern when Hexalith.Projects merges Project A into Project B: the conversation's Project link continues to resolve, because identity is owned upstream and Conversations consumes the canonical resolution at read time.

**Protocol fit is in the indirection, not the orchestration.** Cross-module orchestration (Project move with conversation following, Folder reorganization with content rehydration, agent runners following threads across product boundaries) is owned by upstream modules in v1; multi-product orchestration is vNext.

**Capabilities revealed:**

- Stable-ID indirection: Conversations stores stable IDs to upstream-owned entities (Party, Project, Folder); v1 does NOT subscribe to upstream lifecycle events; read-time resolution uses upstream module's current canonical state.
- Cross-module orchestration is explicitly out of v1 scope; future scenarios that depend on it (agent runner following thread across products, Folder reorganization with content rehydration) are vNext.

### Journey 8 — Daniel, the Head of Customer Operations: The Tragedy

*What this journey proves: the substrate makes harm learnable even when not preventable; the covenant is provable testimony, not prevention or remediation.*

Daniel, head of customer operations at an insurer. A customer has been harmed by an AI's incorrect explanation of their policy. The customer has filed suit.

Daniel cannot undo the harm. He cannot update the AI's grounding — that is the chatbot or LLM provider's domain, outside this substrate's responsibility. He cannot automatically apply legal hold across the conversation lifecycle — full compliance automation is anti-scope for v1.

What v1 gives him: an immutable conversation timeline with per-turn attribution. The governance state in effect at the moment the advice was given. Time-correct reconstruction of what the customer saw. An unbroken chain of audit events for any subsequent governance changes against the conversation.

He responds to the subpoena. He cannot prove the AI was right. He can prove the record is honest. The substrate's covenant is *provable testimony*, not *prevention or remediation*.

The customer was harmed. The substrate did not save them. The substrate gave the organization the dignity of an honest record to face them with — and the integrity to learn from the failure rather than re-litigate it. Daniel's team identifies the systemic cause — the AI's explanation referenced a policy version superseded two months earlier — and refers the gap to the chatbot team, who file a fix in their own backlog. The AI grounding update happens elsewhere; the substrate's audit log made the diagnosis possible. The next harm of this shape is prevented — not by the substrate, but *because* of the substrate.

**What Daniel cannot do with v1:** prevent recurrence (chatbot or LLM layer); apply automatic legal hold (vNext); version policy decisions inside Conversations (vNext, may belong elsewhere); trace AI grounding to specific knowledge-base entries (LLM layer).

**Capabilities revealed:**

- Immutable, time-ordered conversation timeline with per-turn attribution (who said what, when, on which provider).
- Governance state at the moment of any historical message is reconstructable; time-travel applies to governance state, not just message content.
- Subpoena-defensible: the conversation log is the system of record; the audit trail of governance changes is non-mutable.
- Anti-scope explicit: prevention, remediation, AI grounding traceability, legal-hold automation are not Conversations' responsibilities; the substrate's contribution is *testimony*, downstream of which other modules and processes act.

### Journey Requirements Summary

| Journey | Capability area revealed |
|---|---|
| **Maya** — business user resuming | Conversation list/detail projections; single-request retrieval; stable-ID links; tenant scoping; provider portability |
| **Atlas** — AI agent failover | Provider portability proof obligation; correlation IDs as metadata; audit log preserves both providers' IDs; full timeline retrieval; LLM party-modeling |
| **Sarah** — compliance operator (defining moment) | Operator Find/Read screens; inline redaction display + attribution; inline audit trail; time-travel; tenant-scoped UI chrome; empty-state semantics; `governance verify` command; citation affordances; permalink with temporal cursor; migration boundary messaging |
| **Diego** — chatbot developer | Reusable contract/client package; structured tenant-isolation errors; fail-closed surfaces; consumer-side conformance tests; no EventStore leakage; semver |
| **Marcus** — on-call SRE | Audit-write fail-closed enforcement; conformance suite runnable in production; aggregate base type enforcement; cross-tenant SRE actions audited into each affected tenant; SRE surface = CLI + runbook + JSON output + status endpoint |
| **Julian** — platform owner | Buyer acceptance checklist + self-serve demo path; signed conformance artifact gates release; named-waiver process; partial acceptance contract; downgrade-rule trigger calendar |
| **Helen** — CISO (module-level) | Adversarial conformance suite consumer-runnable; fail-closed verifiable from outside; redaction-replay testable; signing + waiver process auditable from outside; module-level vs platform-level scope distinction |
| **Naomi** — protocol-fit sketch | Stable-ID indirection; cross-module orchestration is upstream-owned in v1 |
| **Daniel** — tragedy | Immutable timeline; per-turn attribution; governance state time-travel; subpoena-defensible record; anti-scope of prevention/remediation |

### Carry-Forward Coverage Matrix

| Carry-forward | Journey anchor |
|---|---|
| 28 Conformance manifest + signed CI | Helen, Julian |
| 29 P95 envelope | Maya (envelope assumption) |
| 30 Missing tech metrics (audit-write fail-closed) | Marcus |
| 31 Property test enumeration | Sarah Wed (`governance verify`); Marcus (`conformance verify`) |
| 32 Operator workflow criterion | Sarah |
| 33 Operator v1 / v1.1 split | Sarah (explicit deferral) |
| 34 Audit invariant: aggregate base type | Marcus (climax) |
| 35 Conformance suite scope | Helen, Julian |
| 36 Reference sample honest framing | Julian (sees synthetic labeled) |
| 37 Conditional cut order | meta — not journey-anchored |
| 38 Schema evolution v1 scope | property-level — no journey |
| 39 Engineering cliff (~16-18wk) | meta — not journey-anchored |
| 40 Option A v1 deal | Julian |
| 41 Adoption target revised | Julian (records candidate status) |
| 42 Downgrade rule triggerable | Julian (calendar GA+180) |
| 43 MVP force-rank | Diego (CORE subset suffices) |
| 44 Business success cliffs | Julian (calendar) |
| 45 Buyer acceptance primary | Julian |
| 46 4 open questions | meta |
| 47 Citation affordances on Read view | Sarah |
| 48 Temporal-cursor permalinks | Sarah |
| 49 Pre-UI-rollout attribution (open question) | Sarah Wed |
| 50 Cross-tenant SRE audit-write | Marcus |
| 51 Self-serve demo path | Julian |
| 52 Marcus v1 SRE surface | Marcus |
| 53 TCP commitment trigger | Julian (sees gap) |
| 54 Voice register verification commitment | meta — process |
| 55 Stable-ID indirection commitment | Naomi sketch |
| Tenant lifecycle (cross-cutting) | annotated as failure-mode within Maya/Atlas/Sarah |
| Migration boundary | Sarah Wed |
| LLM party-modeling | Atlas (verified property) |

## Domain-Specific Requirements

The CSV-vocabulary classification is `general` with load-bearing characteristics `audit_governed`, `event_sourced_substrate`, `multi_tenant_fail_closed`, `ai_participant_modeling`. The honest domain framing is **audit-governed event-sourced substrate for AI-assisted business records**. The complexity signature drives non-CRUD-shaped requirements that this section makes explicit, so the architecture step does not re-derive them from narrative.

### Compliance & Regulatory

- **GDPR — subject access requests (SAR) and right-to-erasure pressure on event-sourced storage.** v1 must support: tenant-scoped lookup by external identifier, reconstructed transcript with attributed redactions, time-travel view, audit trail of every governance event. Right-to-erasure in event-sourced systems is fundamentally hard; v1 supports **redaction-with-audit** (preserve event stream, redact projected/displayed content) and explicitly defers **cryptographic redaction** (crypto-shredding) to v1.1+. Full GDPR-compliance automation is anti-scope.
- **SOC2 / ISO 27001 / pen-test / vulnerability disclosure.** Out of v1 scope at the module level (per Helen's Journey 7); inherited from the Hexalith ecosystem's compliance program. The PRD does not commit to module-level attestations; it commits to *audit invariants enforceable at code level*.
- **HIPAA / PCI-DSS-adjacent regimes.** Not committed in v1. Sensitive-data flagging provides hooks; full regime automation is vNext.
- **Subpoena-defensibility (Daniel's scene).** Conversation log is immutable system of record; audit trail of governance changes is non-mutable; v1 is sufficient for subpoena response without legal-hold automation. Legal-hold lifecycle is vNext.

### Technical Constraints

- **Multi-tenant fail-closed isolation.** All conversation reads/writes are tenant-scoped. Tenant decisions are NOT local — consumed from `Hexalith.Tenants` projections. Fail-closed on missing / stale / lagging / rolled-back tenant projection state. Verified by adversarial conformance suite (~30 enumerated cases, signed CI artifact, named-waiver process, release gate).
- **Audit invariant by construction.** Every governance state change emits a paired audit event with same correlation_id, same tenant, causally adjacent, within transaction boundary. Enforced at code level via aggregate base type (`GovernanceAggregateRoot.Mutate(cmd, out auditEvent)`); property test pairing assertion as safety net. Roslyn analyzer dropped from v1.
- **Event sourcing + redaction tension.** Events are immutable. Redaction is mutation-by-another-name. v1 strategy: preserve event stream, redact projected/displayed content with audit trail of the redaction; binary content owned by `Hexalith.Folders`. Cryptographic redaction is vNext.
- **Idempotent commands.** Every command provably idempotent under arbitrary duplication and reordering. Stateful property test with causality-aware generator; per-command scope in v1.
- **Provider portability.** Conversation history fully recoverable independent of any LLM provider's session/response IDs. Provider IDs stored as participant-attribution metadata, not authority. Verified by migration test that swaps the provider correlation schema.
- **Temporal correctness of governance state.** Projections honor governance state *as of read time*; audit trail for governance changes themselves. v1 covers 5–8 known temporal scenarios as example tests; full property test (requires deterministic clock injection architecture) is vNext.
- **Event schema evolution.** Additive-only schema changes in v1; breaking changes require explicit upcaster. Event envelope carries schema-version field. One worked additive-change example in v1. Full upcasting framework vNext.
- **Performance envelope.** P95 ≤ 500ms for "open conversation" returning full context at conversation depth ≤ 500 messages, ≤ 20 humans + 5 AI agents, warm cache, 50 concurrent opens/sec/tenant; cold start separate target.

### Integration Requirements

| Concern | Owner | Conversations consumes via |
|---|---|---|
| Tenant lifecycle, membership, roles | `Hexalith.Tenants` | Local projection of tenant events; fail-closed on missing/stale/lagging/rolled-back state |
| Party identity (humans, AI agents, LLMs) | `Hexalith.Parties` | Participant references by stable Party ID; LLM modeled as Party for stable attribution |
| Project context | `Hexalith.Projects` | Optional project link by stable ID; v1 does NOT subscribe to lifecycle events; read-time resolution |
| Folder organization + attachment binaries | `Hexalith.Folders` | Attachment references only; binaries stay there; reference resolution at read time |
| Persistence | `Hexalith.EventStore` | Aggregate persistence + event sourcing; idempotent command handling; pub/sub publication |
| Admin UI | `Hexalith.FrontComposer` | Command/projection metadata feeds generated/composed views |
| LLM provider correlation | (any LLM provider) | Session/response IDs stored as metadata, not authority |

**Cross-cutting integration commitments:**

- **Stable-ID indirection** (carry-forward 55) — Conversations stores stable IDs to upstream-owned entities; read-time resolution against upstream's canonical state. Cross-module orchestration (Project move, Folder reorganization, Party deactivation cascade, agent runner following thread across products) is upstream-owned in v1; multi-product orchestration is vNext.
- **Pub/sub event publication** — every meaningful state change emits a domain event per Hexalith conventions for downstream consumers.

### Risk Mitigations

| Risk | Mitigation |
|---|---|
| Cross-tenant data leak | Conformance suite (~30 adversarial cases) as release gate; signed CI artifact; named-waiver process; tenant scoping persistent in UI chrome; cross-tenant SRE actions audited into affected tenants |
| Audit invariant violation in production | Aggregate base type enforcement (runtime-immune to bypass); property test pairing assertion as safety net; `governance verify` runnable in production for incident verification |
| Redaction-replay incorrectness | Property test with redaction-aware projection comparer (rebuild from event 0 after redaction yields semantically equivalent state to live projection) |
| Event schema evolution scar (silent killer of ES systems) | Strategy ADR + event envelope schema-version field + 1 worked additive-change example in v1; full framework vNext; projection rebuild time at production volume tracked as NFR |
| Audit-the-audit recursion | Audit log retention/redaction policy explicitly addressed in governance lifecycle; `governance verify` confirms audit pairing on demand |
| Temporal correctness of governance state | 5–8 known scenarios as example tests in v1; full property test vNext; deterministic clock injection architecture vNext |
| Audit-write degradation | Fail-closed: governance commands block with `audit_sink_unavailable`; non-governance commands continue; tested |
| Engineering cliff swallows chatbot deadline | Option A v1 deal (chatbot ships GA, extensibility v1.1 by GA+90); CORE / SUFFICIENT / NICE-TO-HAVE force-rank with conditional cut order; ~16-18 weeks honest v1 acknowledged |
| Strategic fail at GA+12mo (zero second adopters) | Triggerable downgrade rule (GA+6mo trigger; reframe within 30 days; owner = Jerome); TCP commitment trigger at GA−60 with three options |
| Custom domain label not propagated by downstream tooling | Carry-forward list (55 items) is the binding contract, not the labels |

### Domain-Specific Open Questions (pending decision before v1 sign-off)

- **Q-49 (Architecture confirmation):** does pre-UI-rollout attribution exist in event log for migrated tenants? If no, Sarah's Wednesday branch claim must be dropped, backfilled, or restricted to in-Conversations history.
- **Q1 (TEA — Murat):** commit conformance manifest + signed CI artifact + named-waiver process in writing before v1? *Default YES.*
- **Q2 (UX — Sally):** Generate Evidence Bundle in v1 scope? *Default NO — defer to v1.1.*
- **Q3 (Engineering — Amelia):** chatbot ship deadline + is chatbot blocked? *Default TBD — assume blocked, cut to CORE-only first.*
- **Q4 (PM — John):** downgrade-rule public framing change authority? *✓ LOCKED — Jerome.*

## Innovation & Novel Patterns

### Detected Innovation Areas

The genuine innovation in Hexalith.Conversations is not in any single technique — event sourcing, CQRS, DDD aggregates, multi-tenancy, and DAPR-native composition are all inherited Hexalith conventions, applied with rigor but not novel here. The novelty is in a single thesis carried through six concrete decisions:

**Anchor thesis: "Governance by construction."** Audit pairing, tenant isolation, redaction-replay correctness, and provider portability are properties of the type system + property tests + release gates — not of audit retrofit, vendor abstractions, or operational discipline. Most AI substrate tooling treats governance as an optional bolt-on; this module treats it as a compile-/runtime-/test-time invariant.

The thesis surfaces in six product-shaping decisions, all already load-bearing in this PRD:

| # | Innovation | Where it lives |
|---|---|---|
| 1 | **The reframe**: AI conversations are business artifacts (like invoices, contracts, support tickets), not chatbot artifacts | Premise-level; drives Exec Summary, integration map, journey set |
| 2 | **Audit invariant enforced at code level** via aggregate base type (`GovernanceAggregateRoot.Mutate(cmd, out auditEvent)`); bypass is runtime-impossible, not procedural | Carry-forward 34; Marcus's journey; Tech Success table |
| 3 | **Governance-as-domain-commands**: retention, redaction, sensitive-data flagging are first-class lifecycle commands with paired audit events, not external scripts or DBA chores | Domain Requirements; Sarah's journey |
| 4 | **Provider portability as a proof obligation**: verified by migration test that swaps the provider correlation schema; *not* a marketing claim | Carry-forward 24; Atlas's journey; Tech Success table |
| 5 | **LLM-as-Party**: LLMs modeled with stable identity for attribution and governance, alongside humans and AI agents | Integration map; Atlas's journey; carry-forward 21 |
| 6 | **Substrate productized**: Hexalith Standard ADR + conformance suite + reference integration sample + semver'd contracts + deprecation policy as concrete deliverables, not slogans | Buyer acceptance; carry-forwards 25–28 |

What is **not** the innovation, despite being load-bearing: event sourcing, CQRS, DDD aggregates, DAPR-native composition (inherited Hexalith conventions); the integration map across `Hexalith.{EventStore, Tenants, Parties, Projects, Folders, FrontComposer}` (sound architecture, not novel here); multi-tenancy with fail-closed semantics (well-established security practice). This honest scoping matters: the carry-forward register treats unverifiable claims as defects (carry-forwards 24, 36); this section adopts the same discipline.

### Market Context & Competitive Landscape

Three rough categories of prior art in the AI-conversation-persistence space, with where Hexalith.Conversations sits relative to each:

| Category | Examples | What they ship | Where they leave the work |
|---|---|---|---|
| **Vendor session APIs** | OpenAI threads/responses, Anthropic memory, Google Vertex AI conversation history | Provider-managed conversation state with vendor-specific session/response IDs | Tenant model, audit trail, redaction, retention, cross-vendor portability — left to the application |
| **Framework memory layers** | LangChain memory, LlamaIndex chat stores, Semantic Kernel chat history | In-process or pluggable backends behind a chat interface | Storage choice, tenant isolation, governance lifecycle, audit invariants — application's responsibility |
| **Generic transcript stores** | Databases with audit columns, log-retention systems, custom chatbot tables | Storage with timestamps and actor IDs | Domain semantics, redaction-with-audit, replay correctness, governance lifecycle, provider portability — bolt-on |

The differentiation pattern: vendor APIs solve **technical persistence** and leave **governance** to the application; framework memory layers solve **integration** and leave **substrate semantics** to the developer; generic transcript stores solve **storage** and leave **everything semantically meaningful** to the application. Hexalith.Conversations is the *substrate* layer — domain aggregate + governance commands + audit invariant + provider portability — that those three categories assume the application will provide.

**One-line check per category:**

- **vs. vendor APIs**: the application owns the durable record; provider session/response IDs are metadata, never authority. Verified by migration test (carry-forward 24).
- **vs. framework memory layers**: substrate semantics (tenant, audit, retention, redaction, attachment refs, project/folder context) are first-class, not application-developer responsibilities.
- **vs. generic transcript stores**: governance is a domain property enforced at code/runtime/test level; not audit columns + retention scripts.

**Honest discovery-confidence note.** No fresh competitive web research was run in this step; the comparisons above are grounded in awareness of the AI-substrate space as of late 2025/early 2026 and inherited from the brief's "Medium-high" discovery confidence. If a deeper landscape pass is required for buyer review, run targeted research as a follow-up — flagged but **not** a blocker for v1 sign-off.

### Validation Approach

Each innovation claim has a verifiable check; none rest on rhetoric. Cross-references to artifacts already specified in this PRD:

| Innovation claim | Verification | Status |
|---|---|---|
| 1. Reframe (business-artifact framing) | Adopter ergonomics (Diego's 2-week sprint) + operator workflow (Sarah's 90-second SAR) — both pass = framing translates | Journey-anchored; observable in adopter feedback at GA |
| 2. Audit invariant by construction | Aggregate base type as primary mechanism (3–5 days, carry-forward 34); property test pairing assertion (1–2 days) as safety net; `governance verify` runnable in production for incident verification | Tech Success table; Marcus's journey |
| 3. Governance-as-domain-commands | Property test: 100% of governance mutations paired with audit events (same correlation_id, tenant, transaction boundary) | Property test enumeration (carry-forward 31) |
| 4. Provider portability as proof obligation | Migration test that swaps provider correlation schema; Atlas's failover scenario | Tech Success table |
| 5. LLM-as-Party | Integration test where provider switch preserves LLM Party identity in audit trail; both providers' correlation IDs retained | Atlas's journey "capabilities revealed"; integration map |
| 6. Substrate productized | Buyer acceptance gate: published contract package (semver'd), conformance suite (signed CI artifact + named-waiver), reference integration sample (synthetic in v1, honest-labeled per carry-forward 36), Hexalith Standard ADR (GA+90 per Option A), deprecation policy | Buyer acceptance criteria; Julian's journey |

The conformance suite (~30 enumerated cases, carry-forward 35) and the property test enumeration (6 named suites, carry-forward 31) are the load-bearing verification mechanisms; this section does not propose new ones.

### Risk Mitigation

Each innovation claim has a documented fallback if the novel approach proves insufficient. None of the fallbacks introduce new scope; they all degrade to already-architected positions or trigger explicit downgrade rules.

| Innovation claim | Risk | Fallback |
|---|---|---|
| 1. Reframe | Reframe doesn't translate to consumer ergonomics → contracts bloat, adopters revert to per-surface transcript stores | Diego's adopter test fails → tighten contract package and consumer-side conformance tests; if still insufficient, drop "substrate" framing per downgrade rule (carry-forward 42) |
| 2. Audit invariant by base type | Aggregate base type proves leaky (e.g., subclasses bypass `Mutate`) | Property test pairing assertion catches violations; if both fail, escalate to Roslyn analyzer in v1.1 (originally dropped from v1, carry-forward 34) |
| 3. Governance-as-domain-commands | Command surface insufficient for some compliance regime (HIPAA, GDPR right-to-erasure beyond redaction) | Anti-scope acknowledged: full compliance automation and cryptographic redaction (crypto-shredding) are vNext; v1 commits only to redaction-with-audit |
| 4. Provider portability | Migration test reveals coupling in correlation metadata schema | Ship v1 with explicit "module-portable, payload-coupled" labeling; tighten metadata schema in v1.1; do **not** drop the portability claim silently |
| 5. LLM-as-Party | Per-model Party modeling becomes operational overhead at 50+ model variants per provider | Provider-family Parties with model-version metadata as a tier-2 attribute; preserves stable identity at provider granularity, sacrifices model-level attribution to avoid Party explosion (Step-6-introduced fallback; not in carry-forward register) |
| 6. Substrate productized | No second adopter materializes by GA+12mo | Triggerable downgrade rule (carry-forward 42): public framing reverts from "substrate backbone" to "conversation persistence with extensibility primitives" within 30 days; ADR amended; owner = Jerome |

**Cross-cutting innovation risk: engineering cliff.** The innovation cluster (audit base type + property test harness + conformance suite + redaction-replay comparer + clock injection + envelope+ADR) accounts for ~12 of the ~16–18-week honest v1 (carry-forward 39). If the chatbot ship-window tightens, the conditional cut order (carry-forward 37) preserves CORE-tier innovation claims (audit invariant base type, idempotency property test, schema evolution strategy ADR, code-level governance enforcement, tenant isolation conformance) and degrades SUFFICIENT/NICE-TO-HAVE-tier ones (full conformance suite to skeleton, reference sample beyond chatbot, operator FrontComposer view to read-only, provider-portability migration test deferred).

## API Backend Specific Requirements

### Project-Type Overview

Hexalith.Conversations is `api_backend` in Hexalith terms — a domain service module exposing commands, projections, and pub/sub events for consumption by other Hexalith modules and reusable contract/client packages. It is **not** a public REST/GraphQL endpoint, not consumer-facing, not a transcript table behind HTTP CRUD. The "API surface" is the contract package (commands + projections + events + typed errors) plus a thin HTTP/gRPC envelope inheriting Hexalith conventions. The "Consumer Contracts & DX" surface is treated as a first-class PRD chapter (carry-forward 1), not a non-functional concern.

CSV-driven discovery for `api_backend` raised: **Endpoints, Authentication, Data formats, Rate limits, Versioning, SDK**. Each is addressed below; sections marked **already locked** consolidate decisions made earlier in the PRD; sections marked **deferred to architecture** are genuinely open and named for the architect step. Skipped per CSV `skip_sections`: UI/UX, visual design, user journeys — already covered exhaustively in Step 4.

### Technical Architecture Considerations

The module inherits Hexalith conventions for transport, encoding, observability, and persistence:

| Concern | Inherited from | Module-specific commitment |
|---|---|---|
| Transport (commands, projections) | Hexalith conventions (HTTP/gRPC over DAPR) | Confirmed at architecture step; no module-specific deviation |
| Wire encoding | Hexalith conventions (JSON canonical) | Event envelope carries `schema_version` field (carry-forward 38) |
| Persistence | Hexalith.EventStore | Aggregate persistence + event sourcing; idempotent command handling; pub/sub publication |
| Identity / authentication | Hexalith.Tenants + caller-Party binding | See "Authentication & Authorization Model" below |
| Encryption at rest / transit | Hexalith.EventStore + ecosystem | Module assumes inheritance; verified by CISO module-level review (Helen's journey) |
| Observability (trace / log / audit) | Hexalith conventions + module audit invariant | Audit cardinality + retention SLAs sized in NFR step (carry-forward 30) |
| Pub/sub event distribution | Hexalith.EventStore conventions | Schema versioning per envelope; idempotency tested with at-least-once + duplicates + reordering (carry-forward 30) |

**What this module does NOT bring:** new transport protocols, new encoding formats, new identity providers, new orchestration runtime. The "innovation in the substrate" anchor (Step 6) is intentional: the contract surface is conventional Hexalith, so the novelty stays where it should — in governance, party modeling, and audit invariants — and adopters don't pay an integration tax to use it.

### Endpoint Specifications (`endpoint_specs`)

**Commands (9, all tenant-scoped, all idempotent):** `CreateConversation`, `AddParticipant`, `AppendMessage`, `AttachFileReference`, `UpdateTitleOrMetadata`, `CloseOrArchiveConversation`, `SetRetentionPolicy` (UpdateRetentionPolicy folded in as set-or-replace per carry-forward 37), `MarkSensitiveData`, `RedactMessageContent` (with rationale; emits paired audit event).

The chatbot CORE loop subset (4–5 of 9) is mapped during architecture (carry-forward 43); likely candidates `Create`, `AddParticipant`, `AppendMessage`, `AttachFileReference`, plus `RedactMessageContent` or `MarkSensitiveData` if compliance gates v1.

**Projections (9, all tenant-scoped, read-time stable-ID resolution):** Conversation list, conversation detail, participant list, message timeline, attachment list, retention state, redaction state, sensitive-data flags, recent activity. Chatbot read path is 2–3 of 9 (likely conversation detail, message timeline, attachment list). Operator viewer (Sarah's journey) consumes conversation detail + message timeline + redaction state + retention state with time-travel cursor.

**Pub/sub events:** every meaningful state change emits a domain event per Hexalith conventions; topic naming follows Hexalith.EventStore convention (architecture-confirmed). Event envelope: `event_id`, `event_type`, `schema_version`, `tenant_id`, `aggregate_id`, `correlation_id`, `causation_id`, `actor_party_id`, `committed_at`, `payload`.

**Operator surface (carry-forward 52):**

- `governance verify` CLI subcommand: `--suite {audit_pairing|tenant_isolation|...}`, `--tenant T`, `--since ISO8601`. Output: structured JSON. Runbook page linked from alerts.
- Per-tenant audit-pairing health status endpoint: continuously-running monitoring projection (~1.5 weeks). Exposes pairing percentage + recent unpaired event count.

**Read-only governance viewer (v1 release-critical, FrontComposer-driven):** Find + Read screens per Sarah's journey. Generate Evidence Bundle deferred to v1.1 (carry-forward 33).

**Adopter-runnable conformance tests (Diego's journey):** consumer-side test pack shipped with the contract package; verifies the consumer's integration before deployment.

### Authentication & Authorization Model (`auth_model`)

Module-specific bindings (the rest is inherited):

| Binding | Source | Behavior |
|---|---|---|
| Tenant ID | Request header / claim (Hexalith.Tenants convention) | Required for every command and projection request; fail-closed on missing, malformed, or unrecognized tenant ID |
| Caller Party ID | Request claim (Hexalith.Parties convention) | Required for every command; recorded as `actor_party_id` in event envelope; humans, AI agents, and LLMs all bind to a Party |
| Tenant projection state | Local projection of Hexalith.Tenants events | Fail-closed on missing, stale, lagging, or rolled-back state (carry-forward 30) |
| Operator (SRE) actions | Same tenant + Party binding, plus `justification` field for cross-tenant operations | Audited into each affected tenant's stream (carry-forward 50); not a separate auth path |

**Authorization model:** strictly tenant-scoped. No cross-tenant commands or projection reads from any caller, including SRE. Cross-tenant SRE tooling invokes per-tenant operations with structured justification; never "list all tenants' conversations" as a single call.

**Failure modes (Diego's journey + Marcus's journey):** missing tenant header → typed `tenant_binding_missing` 4xx; cross-tenant access → typed `tenant_isolation_violation` 403 with target ID elided; stale tenant projection → typed `tenant_projection_stale` (status code architecture-decided); audit-sink unavailable for governance command → typed `audit_sink_unavailable`; non-governance commands continue.

### Data Schemas & Wire Formats (`data_schemas`)

**Wire encoding:** JSON canonical for HTTP; protobuf for gRPC if both transports surface in v1 (architecture decision). Encoding choice does not change schema semantics.

**Command envelope:** `tenant_id`, `caller_party_id`, `correlation_id`, `idempotency_key` (consumer-supplied or service-derived — architecture decision per carry-forward 30), `command_type`, `payload`, `schema_version`.

**Event envelope:** as enumerated in Endpoint Specifications above; `schema_version` is the load-bearing field for upcasting (carry-forward 38).

**Projection schema:** per-projection typed contract; v1 ships *additive-only* breaking-change discipline. Breaking projection schema requires versioned read endpoint or explicit upcaster (architecture decision).

**Reference fields (stable-ID indirection, carry-forward 55):**

- Attachment references: stable Hexalith.Folders file ID + content-type metadata; no inline binary content.
- Party references: stable Hexalith.Parties Party ID; resolved at read time; v1 does NOT cache party display data inside conversation projections.
- Project & folder references: stable IDs only; resolved at read time against upstream's canonical state.

**Provider correlation metadata (for LLM providers):** provider type + provider session/response IDs as a metadata bag attached to message events; treated as opaque attribution, never as authority. Schema for this bag is the load-bearing definition for the provider portability migration test (carry-forward 24); architecture step finalizes the schema.

### Error Codes & Failure Modes (`error_codes`)

Uniform error envelope across commands and projection reads:

```json
{
  "error": "<typed_error_code>",
  "audit_id": "<correlation handle>",
  "documentation_url": "<link>",
  "details": { "field-level diagnostics if applicable" }
}
```

Typed error codes (v1 commitment, list expandable in vNext):

| Code | Returned for | Notes |
|---|---|---|
| `tenant_binding_missing` | Missing tenant header / claim | 4xx |
| `tenant_isolation_violation` | Cross-tenant access attempt | 403; target tenant ID elided |
| `tenant_projection_stale` | Local tenant projection missing/lagging/rolled-back | Fail-closed; status code architecture-decided |
| `audit_sink_unavailable` | Audit-event sink degraded | Governance commands fail-closed; non-governance continues |
| `audit_pairing_required` | Mutation attempt without paired audit event | Should be unreachable in production (aggregate base type prevents); surfaced for property test diagnostic |
| `idempotency_conflict` | Duplicate command with conflicting payload under same key | 409 |
| `aggregate_not_found` | Conversation does not exist in this tenant | Empty-state semantics — does NOT reveal existence in another tenant |
| `schema_version_unsupported` | Event/projection at schema version this consumer cannot read | 4xx; signals upcaster required |
| `command_validation_failed` | Command payload fails contract validation | 4xx; field-level diagnostics in `details` |

**Error message hygiene:** never leak target-tenant IDs, never leak Party identifiers across tenants, never leak existence of a sibling tenant's conversation. Adversarial conformance suite case (carry-forward 35): "verify cross-tenant lookup returns `aggregate_not_found`, not `tenant_isolation_violation`" to prove non-existence semantics.

### Rate Limits & Performance Envelope (`rate_limits`)

**Performance envelope (carry-forward 29 — already locked):** P95 ≤ 500ms for "open conversation" returning full context at depth ≤ 500 messages, ≤ 20 humans + 5 AI agents, warm cache, 50 concurrent opens/sec/tenant. Cold start is a separate target (sized in NFR step).

**Throttling / quotas (deferred to architecture or NFR step):**

- v1 does **not** commit to per-tenant rate limits as a product feature. Operational throttling (DOS protection) is inherited from Hexalith ecosystem (DAPR sidecar / API gateway).
- Pathological adopter load (e.g., a buggy consumer spamming `AppendMessage`) is handled operationally (alert, throttle at gateway, contact adopter), not by module-level rate limiting.

**To be sized in NFR step (carry-forward 30):** throughput target (events/sec, concurrent conversations, write-amplification budget); projection rebuild time at 1M / 10M / 100M events; schema migration SLO; pub/sub at-least-once + idempotency tested **together** with induced duplicates / reordering / dedup-window expiry; tenant-events lag SLO with defined behavior during lag window; redaction latency SLO.

### API Documentation & Versioning (`api_docs`)

**Contract package versioning (release-scoped per the phased plan and carry-forward 13):**

- Semver discipline: MAJOR for breaking, MINOR for additive, PATCH for fixes.
- Breaking changes (MAJOR bumps) require an upcaster + a deprecation window — minimum window architecture-decided (common practice: 2 minor versions or 6 months, whichever later).
- Deprecation policy: published as a separate document at v1 GA (or v1.1 per Option A); covers contract package, projection schemas, command schemas, event schemas.

**Event schema evolution strategy (carry-forward 38):** v1 ships strategy ADR (3–5 days) + event envelope with `schema_version` field (1 day) + 1 worked additive-change example (2 days) ≈ 1.5 weeks. Full upcasting framework → v1.1 (2–3 weeks + ongoing maintenance).

**Consumer client packaging — locked here:** **shared contract package + per-language thin clients.** The contract package is the source of truth (DTOs, command shapes, projection shapes, error envelope, event schema). Per-language clients are thin wrappers exposing language-idiomatic call sites. v1 ships at minimum the **.NET client + the contract package**; additional language clients are admitted on demand by adopters. *Decision is reversible if a real adopter requires a different model.* (Locks the open question from the brief; Step-7-introduced lock.)

**API documentation deliverables:** contract package README with 5-line happy path (Diego's journey requirement); generated API reference (DocFX or equivalent) for the contract package; integration guide consolidating tenant binding, Party identity, idempotency keys, error envelope; conformance test pack runnable by adopters.

### Implementation Considerations

**Engineering cliff alignment (carry-forward 39):** the api_backend surface itself is straightforward (~4–6 weeks for commands + projections + pub/sub wiring once foundation exists). The cliff is in the *foundation* (~12 weeks: property test harness + conformance suite + audit base type + redaction-replay comparer + clock injection + envelope + ADR), shared across the innovation cluster (Step 6) and the api_backend surface.

**Surface dependencies on the foundation:**

- Idempotent command handling depends on the idempotency property test (CORE-tier).
- Event envelope versioning depends on the schema-evolution ADR (Migration Gate / v1 foundation in the phased plan).
- Tenant fail-closed errors depend on the tenant isolation conformance suite (CORE-tier).
- Audit pairing errors depend on the aggregate base type (CORE-tier) + property test (CORE-tier).
- Operator `governance verify` endpoint depends on the property test harness running in production mode (release-scoped by Marcus's v1 journey; built on CORE foundation).

**Open questions for architect step (5):**

1. **Wire transports:** HTTP-only or HTTP + gRPC in v1? (default: HTTP-only; gRPC if a real adopter requires it)
2. **Idempotency-key derivation:** consumer-supplied vs service-derived from canonical command form? (default: consumer-supplied with service-derived fallback)
3. **HTTP status code mapping for `tenant_projection_stale`:** 503 with `Retry-After`, 5xx with no retry, or 4xx? (default: 503 with `Retry-After`)
4. **Pub/sub topic naming convention:** confirm Hexalith.EventStore convention applies as-is (default: yes; module-specific prefix only)
5. **Per-tenant audit-pairing health status endpoint:** pull (HTTP polling) or push (event stream)? (default: pull, lower operational complexity)

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP approach: Platform MVP** (in the four-axis framing of problem-solving / experience / platform / revenue MVPs).

The chosen philosophy is named in carry-forward 22: *"ship the substrate the chatbot needs + credible v2 hooks; don't ship v3."* Two consequences:

- **What we measure at GA:** chatbot integration ergonomics + governance invariant evidence (buyer-acceptance gate). We do *not* measure ecosystem adoption at GA — that's the GA+90 / GA+180 horizon.
- **What we don't ship in v1:** the substrate's full theoretical shape. Anti-scope is preserved.

The natural rejection of alternative philosophies is in carry-forward 18: without a named second adopter, the strategic narrative is *"chatbot persistence + credible extensibility commitments"*, not *"substrate backbone with ecosystem moat."* The MVP philosophy avoids two failure modes — Experience MVP (chatbot polish at the expense of governance) and Revenue MVP (premature monetization of the substrate before ecosystem signal exists).

**CORE definition** (resolved during Step-8 advanced elicitation): "credible substrate from all 8 journey perspectives," not "Diego's first-user experience." Buyer-acceptance is PRIMARY per CF 45; CORE definition follows from that.

**Resource Requirements (honest v1):**

| Resource | Sizing |
|---|---|
| Engineering | ~16–18 weeks honest v1 (CF 39): ~12 weeks foundation + ~4–6 weeks feature surface. Property-test maintenance ~0.25 FTE ongoing (CF 31) |
| Team composition | Architect (Winston), Senior Dev (Amelia), Test Architect (Murat), UX (Sally), PM (John), Tech Writer (Paige), Platform Owner (Jerome — buyer + downgrade-rule signoff) |
| External dependencies | Hexalith.{EventStore, Tenants, Parties, Projects, Folders, FrontComposer} — version pins → architecture step |
| Critical-path commitments (CF 46) | Q1 (Murat): conformance manifest + signed CI + named-waiver in writing — *default YES*; Q2 (Sally): operator-viewer scope — *default NO Generate Evidence Bundle in v1*; Q3 (Amelia): chatbot ship deadline — *default TBD, assume blocked*; Q4 (Jerome): downgrade-rule public framing authority — *✓ LOCKED* |

**Pre-kickoff commitment required (CF 39):** the PRD must commit to either the expanded ~16–18-week timeline OR contract success criteria. *Decision before sprint 1 kickoff.*

### Release Mode

**Phased.** Three phases: **v1 GA → v1.1 (GA + 90 hard date per Option A) → vNext.**

**Release-gate interpretation:** a capability listed in the FRs/NFRs is part of the full product contract; it is release-gating only when assigned to the current phase or explicitly named in that phase's acceptance criteria, success criteria, or Foundation Gates. Architecture should optimize for the v1 slice while preserving extension points for post-v1 capabilities named in the full contract; do not implement post-v1 workflows unless listed in the v1 phase.

### Phase-to-Evidence Map

| Phase | Primary scope signal | Primary journey evidence | Expected evidence type |
|---|---|---|---|
| v1 GA | v1 GA feature set, CORE items, and Foundation Gates | Maya, Atlas, Sarah, Diego, Marcus, Julian, Helen, Daniel; Naomi at capability level | Signed conformance artifact, API/contract tests, migration/provider-portability test, operator read-only viewer walkthrough, buyer self-serve demo |
| v1.1 | GA + 90 commitments under Option A | Sarah evidence-bundle extension, operator retention editor, reference integration sample beyond chatbot | Evidence bundle workflow test, FrontComposer operator workflow test, contract/versioning evidence, Candidate ADR review |
| vNext | Future capabilities only | Future journey extensions only | New PRD revision or approved follow-on planning artifact |
| Explicitly Out of Scope | Anti-scope lists and unresolved open questions | Not journey-committed unless later promoted | No implementation work; preserve extension points only when required by v1 architecture |

### v1 GA — Feature Set

CORE definition adopted: substrate-from-all-8-journeys. SUFFICIENT tier eliminated; substrate-defining items absorbed into CORE; adopter-polish items demoted to v1.1.

**Core user journeys supported in v1:**

| Journey | v1 capability level |
|---|---|
| Maya (business user resuming) | Full — depends on adopter UI; substrate ready |
| Atlas (AI agent provider failover) | Full |
| Sarah (compliance operator SAR) | v1 = read-only governance viewer (Find + Read screens, time-travel, attributed redactions). Generate Evidence Bundle → v1.1 |
| Diego (chatbot developer integration) | Full — contract package + .NET client + adopter conformance tests |
| Marcus (on-call SRE) | Full — `governance verify` CLI + runbook + cross-tenant audit-into-stream (full status endpoint deferred to v1.1) |
| Julian (platform owner GA acceptance) | Full — buyer acceptance gate + 5-min self-serve demo + signed CI artifact |
| Helen (CISO module-level review) | Full — adversarial conformance suite consumer-runnable, signed + waiver process |
| Naomi (cross-product PM) | Capability-level only — no orchestration; stable-ID indirection sufficient |
| Daniel (tragedy / subpoena) | Full — immutable timeline, time-travel governance state, attributed redactions |

**CORE (12 items).** Items 1–7 + all Foundation Gates are **non-cuttable**. Items 8–12 are CORE-aspirational with explicit cut-order consequences (see cut order below).

1. Conversation aggregate (tenant-scoped identity, ordered linear message history)
2. Chatbot CORE commands — non-negotiable: `CreateConversation` + `AppendMessage` + `AddParticipant`. Plus 1–2 from `{AttachFileReference, RedactMessageContent, MarkSensitiveData}` per chatbot loop spec finalized at architecture step
3. EventStore persistence with idempotent command handling and pub/sub publication
4. Chatbot CORE projection subset (2–3 of 9; likely conversation detail, message timeline, attachment list)
5. Fail-closed tenant isolation via local Hexalith.Tenants projection
6. Sensitive-data classification + redaction policy mechanism (single artifact serving both runtime policy and redaction-replay precondition; party/Step-8-derived merge of CORE-policy and party-surfaced classification mechanism)
7. Code-level governance enforcement (aggregate base type primary; property test pairing safety net)
8. **.NET client + contract package** *(party/Step-8-derived; not in original carry-forward register; proposed CF 56)* — 5-line happy path, typed errors with audit IDs, error message hygiene
9. **Operator read-only governance viewer** (Find + Read screens, time-travel, attributed redactions) — Sarah's defining moment
10. **Full conformance suite (~30 enumerated cases)** signed CI artifact + named-waiver process — Helen's CISO review
11. **Provider portability migration test** — Atlas's claim verification per CF 24
12. **Semver'd contract package + deprecation policy** — buyer acceptance gate per CF 13

**Foundation Gates (5 items, sub-labeled with operational "blocks" definition).**

A "Foundation Gate" is a precondition with three operational properties:

- **CI-passing required** before any CORE story can be closed
- **Named-waiver process** required to proceed without it (with auditable approval)
- **Per-item blocking scope explicit** (below)

*Runtime Gates (3) — block CORE story closing at v1 runtime:*

- Tenant isolation conformance suite (CORE subset cases; signed CI artifact) — blocks **all** CORE commands
- Idempotency property test (per-command scope) — blocks **AppendMessage** specifically
- Audit-write fail-closed behavior (governance commands block `audit_sink_unavailable`; non-governance continues) — blocks **all governance commands**

*Test Gate (1) — blocks Diego's journey validation:*

- Adopter-runnable conformance test pack shipped with the contract package *(party/Step-8-derived; proposed CF 57)*

*Migration Gate (1) — blocks v1.1 release readiness, not v1 runtime:*

- Schema evolution strategy ADR + event envelope `schema_version` field + 1 worked additive-change example

Per Paige's docs-discipline: the "Foundation Gate" tier name is internal sprint-tracking vocabulary. Adopter-facing documentation surfaces this as a **preconditions table** in the integration guide ("CORE behavior assumes the following preconditions are satisfied at integration time").

**NICE-TO-HAVE (1 item):**

- Full conformance suite case-by-case enumeration completed (build skeleton in v1, fill when adopter #2 shows)

**Conditional cut order if cliff bites (most-cuttable first, with named substrate consequences):**

| # | Cut | Substrate consequence |
|---|---|---|
| 1 | .NET client → raw-HTTP fallback | Diego works around; adopter-experience degrades |
| 2 | Provider portability migration test → v1.1 | Atlas's portability claim verified at v1.1, not GA |
| 3 | Semver+deprecation → v1.1 | Buyer-acceptance gate slips |
| 4 | Full conformance suite → CORE cases only (Foundation Gate alone) | Helen's CISO review becomes provisional |
| 5 | Operator read-only governance viewer → v1.1 | 🚨 **Red line** — Sarah's defining moment disappears from v1; substrate framing collapses at GA. Explicit Jerome signature required; alternative is slipping GA |

**Do NOT cut (red lines):** items 1–7 of CORE; all Foundation Gates; the Migration Gate's ADR. Cuts to substrate-defining CORE items (8–12) escalate by named consequence per the table above.

**Anti-scope (explicit NOT in v1):** branching/forked conversations; semantic memory, vector search, automatic summarization; chatbot UI and orchestration; LLM provider abstraction beyond storing correlation IDs; real-time collaborative editing or live streaming; multi-agent planning workflows; attachment binary storage (Hexalith.Folders); full compliance automation per regime; cryptographic redaction (crypto-shredding); multi-region replication / cross-region failover; Roslyn analyzer for governance enforcement; full upcasting framework; Generate Evidence Bundle workflow.

**Cross-module event publication contracts — conditional placement** (resolved before sprint 1 per Q2/Q3 of pre-kickoff open questions below):

- If Hexalith.EventStore envelope is **stable and documented today** → free-rider; named in integration guide as inherited dependency; **anti-scope** for this PRD with forward reference.
- If Hexalith.EventStore envelope is **being evolved as part of this project** → **v1 in-scope** with explicit ADR ownership and Pact (or equivalent) consumer-driven contract testing surface — at minimum the provider side. Adds 1–2 weeks; absorbs into cut-order target.
- If **no Hexalith module consumes Conversations events in v1** → **anti-scope** with explicit "v1 events are internal; cross-module consumption requires v1.1 contract" clause.

### v1.1 (GA + 90 days hard date under Option A)

- **Hexalith.Conversations Candidate ADR v0.1** (single-adopter baseline; promotion to "Hexalith Standard" requires ≥ 2 independent adopters in production with conformance suite coverage OR 6-month observation window with named-waiver from platform owner, whichever later; downgrade rule revokes the candidate state if it fires)
- Generate Evidence Bundle workflow (signed, hash-chained, cryptographically provenanced export)
- Operator FrontComposer view full retention editor
- Full upcasting framework (versioned event types, registered upcasters, replay-time application)
- Roslyn analyzer for governance enforcement (cross-aggregate diagnostic)
- Temporal correctness full property test (deterministic clock injection architecture)
- Per-tenant audit-pairing health status endpoint *(was v1 SUFFICIENT; demoted — `governance verify` CLI suffices in v1)*
- Remaining commands not on chatbot loop (`CloseOrArchiveConversation`, `UpdateTitleOrMetadata`, `SetRetentionPolicy`)
- Remaining projections (6–7 of 9 not on chatbot read path)
- FrontComposer command/projection metadata
- **Reference integration sample beyond chatbot** *(Tension 7 Option B — engineering-discipline contract-stress test, not portability evidence per CF 36; ships GA+90 paired with Candidate ADR ratification window)*

### vNext (Future) — capabilities, not commitments

Conversation summaries; decision and action extraction; branching / forked conversations; cryptographic redaction (crypto-shredding); advanced retention automation; MCP tools for agents; per-message granular redaction; cross-region replication; multi-agent planning workflows; full compliance automation per regime (GDPR, SOC2, HIPAA); cross-module orchestration (Project move, Folder reorganization, Party deactivation cascade, agent-runner cross-product threading).

### Risk Mitigation Strategy

**Technical Risks**

- *Engineering cliff swallows chatbot deadline.* Mitigation: Option A v1 deal (chatbot ships GA, extensibility v1.1 by GA+90); CORE non-cuttable items 1–7 + Foundation Gates as floor; CORE-aspirational items 8–12 with named cut-order consequences; pre-kickoff commitment to either ~16–18-week timeline or contracted criteria.
- *Audit invariant violation in production.* Mitigation: aggregate base type enforcement (runtime-immune to bypass); property test pairing as safety net; `governance verify` runnable in production for incident verification.
- *Redaction-replay incorrectness.* Mitigation: property test with redaction-aware projection comparer; sensitive-data classification mechanism (CORE #6) feeds the classification side.
- *Event schema evolution scar.* Mitigation: strategy ADR + envelope `schema_version` field + 1 worked example in v1 (Migration Gate); full upcasting framework vNext.
- *Temporal correctness of governance state.* Mitigation: 5–8 known scenarios as example tests in v1; deterministic clock injection architecture vNext.
- *Audit-write degradation in production.* Mitigation: fail-closed (Runtime Gate); tested via Marcus's incident reproduction.
- *Tenant projection lag / stale state.* Mitigation: fail-closed semantics tested by adversarial conformance suite (Runtime Gate); runbook documented.

**Market Risks**

- *No second adopter materializes by GA+12mo (strategic fail).* Mitigation: triggerable downgrade rule (CF 42) — public framing reverts within 30 days; ADR amended; Candidate ADR is revoked, never silently becomes "Standard"; reference integration sample at GA+90 (v1.1) provides a non-chatbot artifact for prospective adopters to read; owner = Jerome.
- *Substrate framing aspirational at GA without TCP candidate.* Mitigation: TCP commitment trigger at GA−60 (CF 53) — buyer chooses among (a) candidate-pursuit sprint, (b) accept downgrade-rule risk, (c) push GA out 30 days; if GA proceeds without populated TCP, v1 docs carry explicit aspirational-framing annotation.
- *Buyer of v1 not aligned with first adopter.* Mitigation: CF 17 explicit — buyer is platform owner; first adopter is chatbot product owner; success criteria explicitly distinguish buyer-acceptance (PRIMARY) from first-adopter-acceptance (instance proof).
- *Compliance audit failure / tenant breach / sensitive-data leak post-GA (hard fail).* Mitigation: conformance suite as release gate (CORE #10); aggregate base type enforcement; redaction-replay correctness property test; tenant isolation property test (Runtime Gate).

**Resource Risks**

- ***Single-thread execution risk on the Foundation block — no parallelization headroom across the Architect / TEA / Senior Dev disciplines.*** Mitigation focus: handoff-latency reduction + explicit decision-authority delegation + isolatable workstream identification (property test harness ~3–4w is the most isolatable piece, candidate for parallel ownership if a second engineer materializes).
- *Specialized testing capacity insufficient (TEA — Murat).* Mitigation: property test enumeration explicitly costed at ~3 senior-engineer-months + 0.25 FTE ongoing (CF 31).
- *Dependency module readiness.* Mitigation: stable-ID indirection (CF 55) reduces coupling — v1 does NOT subscribe to upstream lifecycle events; read-time resolution against upstream's canonical state.
- *Operator UX scope (Sarah's journey).* Mitigation: v1 ships read-only viewer (Find + Read); full retention editor + Generate Evidence Bundle → v1.1 — explicit by Q2 default (CF 46).

### Pre-kickoff Open Questions to the Buyer

These are pre-kickoff blockers (not architecture-step deferrable). Defaults silently lock if unanswered before sprint 1.

| # | Question | Origin | Default if no answer |
|---|---|---|---|
| 1 | Can Diego ship v1 with a raw-HTTP fallback (no .NET client at GA)? | John, Winston, Paige | NO → .NET client stays CORE |
| 2 | Is the Hexalith.EventStore envelope format stable and documented today, or being evolved as part of this project? | Winston, Murat | Stable → cross-module contracts free-rider; Evolved → cross-module contracts in v1 |
| 3 | Does any Hexalith module consume Conversations events in v1, or is v1 fully isolated? | John | Isolated → cross-module contracts anti-scope-with-forward-reference |
| 4 | Architect availability across the full 16 weeks? Second engineer on Foundation block (even part-time)? | Murat, Amelia | Single-thread, 1.0 FTE Architect → no parallelization |
| 5 | Named candidate second adopter (even internal) before GA+90? | Sally | None → ADR ships as Candidate v0.1 |
| 6 | Operational definition of "blocks" for Foundation Gates — written down and ratified? | Murat | Use the three-property definition above (CI-passing + named-waiver + per-item blocking scope) |
| 7 | Is `MarkSensitiveData` / `RedactMessageContent` in CORE #2 (compliance-gating v1) or chatbot-loop optional? | (cross-cutting) | Compliance-gates → CORE #2 includes one or both as non-negotiable |

### Documentation Deliverables — Voice-register Review Checkpoint

Per CF 54: explicit **voice-register review checkpoint** named in the sprint plan as a separate deliverable. The 8 journey personas (Maya/Atlas/Sarah/Diego/Marcus/Julian/Helen/Daniel) each have an adopter-trust implication — adopters in those roles will notice if the docs voice is undifferentiated. Trust calibration, not polish.

### Carry-forward Register Addendum (proposed CF 56–58)

To preserve CF 16's "carry-forward register is the binding contract" rule, the following Step-8/party-derived items are flagged for register addition (subject to platform-owner sign-off):

- **CF 56:** .NET client + contract package as v1 CORE (Diego's journey; party/Step-8-derived).
- **CF 57:** Adopter-runnable conformance test pack as Foundation Gate Test (Diego's journey; party/Step-8-derived).
- **CF 58:** Sensitive-data classification + redaction policy mechanism as single CORE artifact (party/Step-8-derived merge of original CORE-policy and party-surfaced classification mechanism, both serving redaction-replay precondition per CF 31).

## Functional Requirements

This section is the capability contract for downstream UX, architecture, epic planning, and test design. Each requirement describes a product capability or externally observable system behavior, not a specific implementation.

Release timing is governed by **Project Scoping & Phased Development**. The requirements below define the full capability contract; downstream planners must map each requirement to v1, v1.1, vNext, or Explicitly Out of Scope before treating it as implementation work.

### Conversation Lifecycle

- FR1: Adopter systems can create a tenant-scoped conversation record.
- FR2: Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names.
- FR3: The system can represent conversation lifecycle state and allowed transitions, including active, archived or closed, and any release-approved reopening or sealing behavior.
- FR4: Adopter systems can append ordered messages to an existing conversation.
- FR5: Adopter systems can add human users, AI agents, and LLMs as conversation participants.
- FR6: Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions.
- FR7: The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics.
- FR8: Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context.
- FR9: Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity.
- FR10: Adopter systems can update conversation title or metadata when that capability is included in the active release scope.
- FR11: Adopter systems can close or archive a conversation when that capability is included in the active release scope.
- FR12: The system can preserve a complete conversation record across provider session expiry, restart, or failover.

### Participant Attribution

- FR13: The system can attribute each conversation action to a stable Party identity.
- FR14: The system can model humans, AI agents, and LLMs as attributable participants.
- FR15: The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
- FR16: The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- FR17: The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
- FR18: The system can reconstruct who said or changed what, when, and under which tenant context.

### Business Context And References

- FR19: Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
- FR20: Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
- FR21: Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
- FR22: The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
- FR23: The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
- FR24: The system can keep conversations readable and attributable when upstream entities change lifecycle state.
- FR25: The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.

### Tenant Access And Isolation

- FR26: The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
- FR27: The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- FR28: The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
- FR29: The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
- FR30: The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
- FR31: The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
- FR32: The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.

### Event Sourcing, Projections, And Publication

- FR33: The system can derive projections from ordered conversation events.
- FR34: The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
- FR35: The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
- FR36: The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- FR37: The system can expose projection lag or documented freshness behavior when read models are asynchronous.
- FR38: Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
- FR39: Published events can carry explicit schema and version metadata.
- FR40: The system can reject unsupported event, command, or projection schema versions with typed documented errors.
- FR41: The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.

### Governance And Audit

- FR42: Authorized systems can set or replace a conversation retention policy with rationale.
- FR43: Authorized systems can mark conversation content as sensitive.
- FR44: Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution.
- FR45: The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history.
- FR46: The system can preserve the audit event stream while redacting projected or displayed content.
- FR47: The system can require every governance mutation to have a paired audit event.
- FR48: The system can reject governance mutations when audit recording is unavailable.
- FR49: The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state.
- FR50: The system can reconstruct message state and governance state as they existed at a prior point in time.
- FR51: The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata.
- FR52: The system can apply retention and redaction policy treatment to governance audit records themselves.
- FR53: The system can define which actions on audit records are allowed, denied, redacted, exported, or separately logged.
- FR54: The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data.
- FR55: Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record.

### Operator And Compliance Workflows

- FR56: Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID.
- FR57: Compliance operators can filter or narrow conversation search by date range and business context.
- FR58: Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness.
- FR59: Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy.
- FR60: Compliance operators can view a conversation's governance audit trail inline.
- FR61: Compliance operators can view conversation state as of a selected historical time.
- FR62: Compliance operators can copy citation-ready references for transcript and audit elements.
- FR63: Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract.
- FR64: Operator and compliance workflows marked read-only cannot mutate conversation aggregate state.
- FR65: Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited.
- FR66: Operators can run governance verification for a conversation, tenant, suite, or time window.
- FR67: Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks.
- FR68: Verification results can distinguish governance verification failures from infrastructure or execution failures.
- FR69: The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial.

### Consumer Contracts And Developer Experience

- FR70: Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors.
- FR71: Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback.
- FR72: Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline.
- FR73: Adopter developers can run adopter-facing conformance tests before deployment.
- FR74: Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior.
- FR75: Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages.
- FR76: The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces.
- FR77: The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities.
- FR78: The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues.
- FR79: The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility.
- FR80: The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.

### Compatibility, Evidence, And Release Gates

- FR81: The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages.
- FR82: The product can produce a signed conformance artifact for release gating.
- FR83: The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability.
- FR84: The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies.
- FR85: The product can support a named-waiver process for release-gate exceptions.
- FR86: The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior.
- FR87: The product can verify tenant isolation using adversarial positive and negative cases.
- FR88: The product can verify idempotent command behavior under duplicate or reordered commands.
- FR89: The product can verify redaction-replay correctness across projections, logs, traces, and errors.
- FR90: The product can verify provider portability by proving recoverability without provider-owned session authority.
- FR91: The product can verify event schema evolution through version-aware records and at least one worked additive-change example.
- FR92: The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release.
- FR93: The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests.
- FR94: The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable.

### Observability And Operations

- FR95: Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
- FR96: Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
- FR97: Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
- FR98: Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
- FR99: Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.

### Scope Boundaries And Lifecycle Commitments

- FR100: The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
- FR101: The product can expose release-scope consequences when substrate-defining capabilities are deferred.
- FR102: The product can support buyer partial acceptance under the Option A v1 deal.
- FR103: The product can track second-adopter status and trigger downgrade-rule review milestones.
- FR104: The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

## Non-Functional Requirements

NFRs define how well Hexalith.Conversations must behave, not new product capabilities. They are intentionally selective: the quality attributes below are included because they directly affect trust in a tenant-isolated, event-sourced, audit-governed AI conversation substrate.

### Measurement, Evidence, And Waiver Discipline

- NFR1: Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
- NFR2: Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
- NFR3: Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
- NFR4: GA implementation cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with named owner and review date.
- NFR5: Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
- NFR6: Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
- NFR7: A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
- NFR8: Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.

### Performance

- NFR9: Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- NFR10: The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
- NFR11: Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
- NFR12: Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
- NFR13: Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
- NFR14: Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
- NFR15: Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stops into one ambiguous number.

### Security And Privacy

- NFR16: Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
- NFR17: Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
- NFR18: Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
- NFR19: Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
- NFR20: Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
- NFR21: Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.

### Reliability, Resilience, And Recovery

- NFR22: The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
- NFR23: Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
- NFR24: Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
- NFR25: DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
- NFR26: A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
- NFR27: Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
- NFR28: The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
- NFR29: Backup restore and tenant-scoped recovery procedures must be tested before production release.

### Scalability, Capacity, And Cost

- NFR30: The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
- NFR31: Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
- NFR32: Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- NFR33: Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
- NFR34: Tenant-events lag must have an SLO and a defined request behavior during lag windows.
- NFR35: Redaction propagation latency must have an SLO covering all materialization surfaces listed in NFR21.
- NFR36: The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
- NFR37: Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.

### Data Integrity And Event Sourcing

- NFR38: v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
- NFR39: Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
- NFR40: Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
- NFR41: Event schema evolution must include one worked additive-change example before GA.
- NFR42: Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
- NFR43: Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.

### Projection Freshness

- NFR44: Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
- NFR45: Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`, or document why an equivalent shape is not available.
- NFR46: The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- NFR47: Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
- NFR48: During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.

### Integration And Compatibility

- NFR49: Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
- NFR50: Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
- NFR51: Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
- NFR52: Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
- NFR53: The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
- NFR54: Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.

### Operability And Observability

- NFR55: Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
- NFR56: Operational signals must be tenant-safe and content-safe by default.
- NFR57: Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
- NFR58: Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
- NFR59: `governance verify` / conformance verification output must be machine-readable and suitable for CI and incident workflows.
- NFR60: Privileged operational actions must include structured justification and produce reviewable audit records.
- NFR61: Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.

### Compliance, Retention, And Release Evidence

- NFR62: Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
- NFR63: Every release must produce a signed conformance artifact and versioned manifest mapping tests to FRs, NFRs, carry-forward commitments, pass criteria, waiver status, measurement method, and environment.
- NFR64: Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
- NFR65: Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
- NFR66: The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
- NFR67: Retention behavior must be tenant-aware and produce verifiable evidence.
- NFR68: Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.

### Accessibility And Human Trust

- NFR69: Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
- NFR70: Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
- NFR71: Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
- NFR72: Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
- NFR73: Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
- NFR74: Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
- NFR75: Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
- NFR76: Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
- NFR77: User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.
