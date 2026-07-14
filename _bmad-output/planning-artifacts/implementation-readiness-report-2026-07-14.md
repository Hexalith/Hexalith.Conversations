---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallStatus: NOT_READY
assessmentCompletedAt: 2026-07-14
includedDocuments:
  prd:
    - prds/prd-Conversations-2026-06-02/prd.md
    - prds/prd-Conversations-2026-06-02/addendum.md
  architecture:
    - architecture.md
  epics:
    - prds/prd-Conversations-2026-06-02/epics.md
  ux:
    - ux-design-specification.md
    - ux-requirement-map.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-14
**Project:** Conversations

## Document Discovery

### PRD

- `prds/prd-Conversations-2026-06-02/prd.md` — 85,089 bytes — modified 2026-07-14
- `prds/prd-Conversations-2026-06-02/addendum.md` — 13,775 bytes — modified 2026-07-14

### Architecture

- `architecture.md` — 81,471 bytes — modified 2026-07-06

### Epics and Stories

- `prds/prd-Conversations-2026-06-02/epics.md` — 55,536 bytes — modified 2026-07-14

### UX Design

- `ux-design-specification.md` — 117,645 bytes — modified 2026-05-31
- `ux-requirement-map.md` — 8,825 bytes — modified 2026-05-31

### Discovery Resolution

- The superseded root PRD was reconciled and archived at `_bmad-output/archive/conversations-product-contract-2026-05-31.md`.
- The active PRD embeds the preserved product-contract baseline and links to the archive for provenance.
- No active duplicate formats or missing required document classes remain.

## PRD Analysis

### Functional Requirements

#### Initiative Refactor Requirements

##### FR-1: Canonical boilerplate inventory exists and is accepted

A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability.

##### FR-2: Classification disagreements are resolvable, not silent

A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale.

##### FR-3: Domain-service host adoption

Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing.

##### FR-4: Query handling via SDK query-handler + cursor seams

Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts.

##### FR-5: Read-model persistence via shared store + write policy

Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics.

##### FR-6: Projection handling via SDK projection seam

Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits.

##### FR-7: Aggregate scaffolding via base-class conventions

Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior.

##### FR-8: Serialization via shared converters / type registration

Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules.

##### FR-9: Testing via shared assertions/fakes/defaults

Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers.

##### FR-10: Platform-owned shared ServiceDefaults

The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

##### FR-11: Generic tenant-access projection handler + registration

A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules.

##### FR-12: Shared client registration

A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation.

##### FR-13: Platform-owned Aspire/Dapr domain-service hosting

The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

##### FR-14: Shared serialization metadata and polymorphic registration

A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition.

##### FR-15: Diagnostics/telemetry scaffolding helper

A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies.

##### FR-16: Compile-time command/event contract metadata *(deferred)*

Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative.

##### FR-17: Conversations consumes every in-scope shared capability

Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot.

##### FR-18: Documented thin authoring template

A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module.

##### FR-19: New-module authoring cost is measured

The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2.

##### FR-20: Behavior and contracts are provably preserved

Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change.

**Initiative FR count:** 20 total: FR-1 through FR-15 and FR-17 through FR-20 are active; FR-16 is explicitly deferred to backlog.

#### Preserved Product-Contract Requirements

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
- **Feature-FR13:** The system can attribute each conversation action to a stable Party identity.
- **Feature-FR14:** The system can model humans, AI agents, and LLMs as attributable participants.
- **Feature-FR15:** The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
- **Feature-FR16:** The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- **Feature-FR17:** The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
- **Feature-FR18:** The system can reconstruct who said or changed what, when, and under which tenant context.
- **Feature-FR19:** Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
- **Feature-FR20:** Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
- **Feature-FR21:** Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
- **Feature-FR22:** The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
- **Feature-FR23:** The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
- **Feature-FR24:** The system can keep conversations readable and attributable when upstream entities change lifecycle state.
- **Feature-FR25:** The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.
- **Feature-FR26:** The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
- **Feature-FR27:** The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- **Feature-FR28:** The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
- **Feature-FR29:** The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
- **Feature-FR30:** The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
- **Feature-FR31:** The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
- **Feature-FR32:** The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.
- **Feature-FR33:** The system can derive projections from ordered conversation events.
- **Feature-FR34:** The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
- **Feature-FR35:** The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
- **Feature-FR36:** The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-FR37:** The system can expose projection lag or documented freshness behavior when read models are asynchronous.
- **Feature-FR38:** Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
- **Feature-FR39:** Published events can carry explicit schema and version metadata.
- **Feature-FR40:** The system can reject unsupported event, command, or projection schema versions with typed documented errors.
- **Feature-FR41:** The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.
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
- **Feature-FR95:** Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
- **Feature-FR96:** Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
- **Feature-FR97:** Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
- **Feature-FR98:** Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
- **Feature-FR99:** Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.
- **Feature-FR100:** The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
- **Feature-FR101:** The product can expose release-scope consequences when substrate-defining capabilities are deferred.
- **Feature-FR102:** The product can support buyer partial acceptance under the Option A v1 deal.
- **Feature-FR103:** The product can track second-adopter status and trigger downgrade-rule review milestones.
- **Feature-FR104:** The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

**Preserved Feature-FR count:** 104.

**Total functional requirements extracted:** 124 numbered requirements, comprising 20 initiative FRs and 104 preserved product-contract Feature-FRs.

### Non-Functional Requirements

#### Initiative Counter-Metric Gates

- **SM-C1 — Behavior/contract stability (inviolable).** The post-refactor pass rate must remain 100% of the versioned pre-refactor preservation manifest, and public contract shapes must match its baselines unless a named approval records an intentional compatible change. Any manifested-test removal or reclassification requires explicit approval, rationale, replacement evidence where applicable, and a versioned manifest update. LOC reduction must **never** be bought by silently dropping conformance tests or reshaping contracts. Counterbalances SM-1, SM-2.
- **SM-C2 — Hot-path performance.** For every identified command/read hot path, post-refactor P95 latency must be no more than 5% worse than the frozen pre-refactor P95 under the same reproducible benchmark envelope. The versioned evidence records workload/data shape, concurrency, environment and runtime, tool versions, warm/cold classification, repetitions, raw results, and baseline/post-refactor commit identities. Preserved absolute targets `Feature-NFR9` (warm full-context open P95 ≤500 ms under its defined envelope) and `Feature-NFR12` (defined operator investigation ≤90 seconds) remain product obligations; they block this refactor only when the current release plan separately activates them. Counterbalances over-abstraction from promotions. `[OQ-5 resolved 2026-07-14.]`

#### Initiative Cross-Cutting NFRs

- **Behavior preservation:** FR-20 / SM-C1 are authoritative for the dominant NFR and its frozen denominator.
- **Performance:** SM-C2 is authoritative. Shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads; snapshot/projection behavior is preserved.
- **Fail-closed invariants:** promoted tenant-access and authorization capabilities must preserve fail-closed semantics by construction; cross-tenant access remains impossible and adversarially tested.
- **Observability:** metric names, dimensions, and health endpoints are preserved through platform-owned shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **Replay safety:** promoted projection/event handling must remain idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

#### Preserved Product-Contract NFRs

- **Feature-NFR1:** Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
- **Feature-NFR2:** Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
- **Feature-NFR3:** Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
- **Feature-NFR4:** Implementation for GA cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with a named owner and review date.
- **Feature-NFR5:** Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
- **Feature-NFR6:** Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
- **Feature-NFR7:** A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
- **Feature-NFR8:** Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.
- **Feature-NFR9:** Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- **Feature-NFR10:** The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
- **Feature-NFR11:** Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
- **Feature-NFR12:** Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
- **Feature-NFR13:** Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
- **Feature-NFR14:** Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
- **Feature-NFR15:** Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stages into one ambiguous number.
- **Feature-NFR16:** Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
- **Feature-NFR17:** Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
- **Feature-NFR18:** Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
- **Feature-NFR19:** Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
- **Feature-NFR20:** Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
- **Feature-NFR21:** Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.
- **Feature-NFR22:** The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
- **Feature-NFR23:** Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
- **Feature-NFR24:** Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
- **Feature-NFR25:** DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
- **Feature-NFR26:** A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
- **Feature-NFR27:** Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
- **Feature-NFR28:** The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
- **Feature-NFR29:** Backup restore and tenant-scoped recovery procedures must be tested before production release.
- **Feature-NFR30:** The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
- **Feature-NFR31:** Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
- **Feature-NFR32:** Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- **Feature-NFR33:** Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
- **Feature-NFR34:** Tenant-events lag must have an SLO and a defined request behavior during lag windows.
- **Feature-NFR35:** Redaction propagation latency must have an SLO covering all materialization surfaces listed in Feature-NFR21.
- **Feature-NFR36:** The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
- **Feature-NFR37:** Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.
- **Feature-NFR38:** v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
- **Feature-NFR39:** Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
- **Feature-NFR40:** Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
- **Feature-NFR41:** Event schema evolution must include one worked additive-change example before GA.
- **Feature-NFR42:** Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
- **Feature-NFR43:** Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.
- **Feature-NFR44:** Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
- **Feature-NFR45:** Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`; otherwise, the system must document why an equivalent shape is not available.
- **Feature-NFR46:** The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-NFR47:** Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
- **Feature-NFR48:** During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.
- **Feature-NFR49:** Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
- **Feature-NFR50:** Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
- **Feature-NFR51:** Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
- **Feature-NFR52:** Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
- **Feature-NFR53:** The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
- **Feature-NFR54:** Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.
- **Feature-NFR55:** Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
- **Feature-NFR56:** Operational signals must be tenant-safe and content-safe by default.
- **Feature-NFR57:** Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
- **Feature-NFR58:** Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
- **Feature-NFR59:** Output from `governance verify` and other conformance verification must be machine-readable and suitable for CI and incident workflows.
- **Feature-NFR60:** Privileged operational actions must include structured justification and produce reviewable audit records.
- **Feature-NFR61:** Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.
- **Feature-NFR62:** Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
- **Feature-NFR63:** Every release must produce a signed conformance artifact and a versioned manifest that maps tests to FRs, NFRs, carry-forward commitments, and pass criteria and records waiver status, measurement method, and environment.
- **Feature-NFR64:** Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
- **Feature-NFR65:** Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
- **Feature-NFR66:** The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
- **Feature-NFR67:** Retention behavior must be tenant-aware and produce verifiable evidence.
- **Feature-NFR68:** Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.
- **Feature-NFR69:** Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
- **Feature-NFR70:** Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
- **Feature-NFR71:** Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
- **Feature-NFR72:** Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
- **Feature-NFR73:** Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
- **Feature-NFR74:** Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
- **Feature-NFR75:** Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
- **Feature-NFR76:** Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
- **Feature-NFR77:** User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.

**Preserved Feature-NFR count:** 77.

**Total explicit NFR extraction:** two initiative counter-metric gates, five initiative cross-cutting NFRs, and 77 preserved product-contract Feature-NFRs. Additional guardrails and qualitative constraints are captured below.

### Additional Requirements

#### Developer Journeys and Use-Case Obligations

*Developer journeys; lighter form per scope dial. FRs reference these inline.*

- **UJ-1. Maya retires hand-rolled plumbing from Conversations.** Maya, a Conversations maintainer, removes bespoke query and pagination infrastructure after confirming that the platform already supplies the generic capability. She retains only conversation-specific filters and response shapes, removes plumbing-only tests with their superseded implementation, and proves the public query behavior remains identical through the conformance gate. *Realizes FR-3..FR-9, gated by FR-20; technical mapping in addendum §D.*

- **UJ-2. Sam promotes the tenant-access handler everyone copied.** Sam notices that the tenant-access projection behavior is duplicated in Folders and Projects and re-implemented in Conversations. He moves the domain-agnostic behavior into a shared technical capability with its own tests, then has Conversations supply only its domain-specific contracts. The Conversations copy disappears; the shared implementation is the single source of truth. *Realizes FR-11; technical mapping in addendum §E.*

- **UJ-3. Priya stands up a brand-new domain module on the thin template.** Priya needs a new business-domain module. She follows the documented authoring template, supplies the required domain contracts and behavior, and consumes the platform-owned hosting and runtime capabilities. She reaches a working module with a fraction of the files Conversations originally needed. The template, proven by Conversations, is what makes this trivial. *Realizes FR-17, FR-18, FR-19; technical grounding in addendum §§D–F.*

#### Scope, Non-Goals, and Phasing

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

#### Constraints, Guardrails, and Developer-Product Surface

- **Cross-submodule coordination:** shared-capability work may edit sibling technical-module submodules (EventStore, Commons, FrontComposer). `Hexalith.Tenants` is a domain module and dependency/consumer, not a technical-module landing zone; coordinate with it only for genuinely required tenant-domain contract changes, and never place generic runtime or hosting boilerplate there. Authorized shared-module changes must remain additive/backward-compatible for existing consumers. `[ASSUMPTION: existing consumers of the technical modules must not break; promotions are additive.]` Honor the repo submodule rule: never recurse into nested submodules.
- **Greenfield latitude:** Conversations is treated as greenfield/pre-release, so plumbing-only tests may be removed with their code; but release-gate conformance is still inviolable. `[ASSUMPTION: Conversations not yet in production for external tenants.]`
- **Public-surface stability:** adopter-facing Conversations contracts and the EventStore-concept boundary (no raw envelopes leaked) are preserved.

## 10. Developer-Product Surface

- **Public surface / breaking-change policy:** promoted technical-module APIs are new public surface; they must be designed additive and versioned so existing domain modules compile unchanged. Conversations' own public contracts are unchanged.
- **Versioning & deprecation:** any Conversations-local type that is superseded by a promoted capability is removed within this initiative (greenfield); for the technical modules, additions follow normal semver-additive rules. `[ASSUMPTION: no deprecation window needed inside Conversations because it is the pilot consumer.]`
- **Language/runtime targets:** unchanged — net10.0, nullable, implicit usings, warnings-as-errors, Central Package Management through the shared Hexalith.Builds package-version baseline, with module-local package versions treated as explicit exceptions.
- **Performance budgets:** enforce SM-C2 and separately report whether the current release activates `Feature-NFR9` or `Feature-NFR12`.

#### Decision Dependencies, Assumptions, Owners, and Revisit Triggers

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

#### Preserved Qualitative Constraints and Ownership Boundaries

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

#### Unresolved Preserved Product and Release Dispositions

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

#### Addendum Baseline and Implementation Guardrail

**Authoritative SM-1 baseline:** Story 1.4 measured and accepted **13,289 LOC (37.15%)** on 2026-06-03 in the canonical, FR-2-governed `docs/release-evidence/consume-promote-keep-inventory-v1.json`. Its `sourceTotalLoc` verifies exactly 35,769 LOC. Under OQ-3, governance and hydration were classified as Keep now. The classification of the Contracts/Testing domain surface as Keep moved ≈4.7k LOC out of plumbing. This inventory is the baseline Story 5.3 references.

**Historical Discovery estimate:** Total source ≈ 35,769 LOC; plumbing (Consume + Promote) ≈ 18,000 LOC (~50%); domain logic (Keep) ≈ 17,000 LOC. This first-pass estimate is preserved as provenance, not as the accepted baseline.

**Implementation guardrail:** Hosting, AppHost, Aspire, DAPR, ServiceDefaults, runtime projections/queries, telemetry scaffolding, and event subscriptions must land in and remain owned by the platform/domain-service SDK, never the Conversations domain module.

#### Addendum Architecture and Release Decisions

### Open architecture decisions (OQ-1)

- Landing zone per promotion: existing module (Commons vs EventStore.*) vs a new dedicated shared abstractions module.
- Additive/backward-compatible API design so Folders/Projects/Memories/Parties/Tenants keep compiling.
- Whether governance/temporal/hydration orchestration (areas 2, 3, and 7) generalizes cleanly enough to be promoted in a follow-on phase.

### Legacy technical-how provenance

**Provenance:** May 2026 legacy root feature PRD, carried through `reconcile-legacy-root-prd.md` on 2026-07-14. These questions are retained here because they concern protocol, mechanism, platform wiring, or technical release fallback. They do not expand refactor scope, and legacy defaults are not current approvals.

### Open legacy technical-how questions

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ1 | Is the supported transport HTTP only or HTTP plus gRPC? | **Open.** Requires an explicit contract/architecture decision; the preserved product baseline remains transport-neutral. |
| Legacy-TQ2 | Is the idempotency key consumer-supplied or service-derived? | **Open.** The mechanism is undecided; `Feature-FR6`, `Feature-FR88`, and `Feature-NFR22` preserve stable externally observable idempotent behavior. |
| Legacy-TQ3 | What exact status and retry semantics apply to stale tenant projections? | **Open.** Mapping remains an architecture/API decision; fail-closed behavior and typed, sanitized errors remain mandatory. |
| Legacy-TQ4 | What pub/sub topic naming is used, and is the EventStore convention sufficient? | **Open.** The platform/domain-service SDK owns topic conventions and subscription plumbing; Conversations must not introduce module-owned runtime naming machinery. |
| Legacy-TQ5 | Is audit-pairing health exposed through pull or push semantics? | **Open.** The platform operational contract and architecture must decide; governance mutations still fail closed when audit recording is unavailable. |

### Open release exception

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ6 | May a release use raw HTTP if the supported .NET client misses GA? | **Open release exception.** `Feature-FR71` permits this only through explicit buyer acceptance; no exception is inferred. |

### Resolved for this refactor

| ID | Legacy technical-how question | Current disposition |
|---|---|---|
| Legacy-TQ7 | Is the EventStore envelope inherited as stable or changed by this initiative? | **Resolved for this refactor: inherited and unchanged.** Envelope redesign is out of scope, public clients must not leak EventStore mechanics, and compatibility remains gated by FR-20/SM-C1. |

#### Addendum Gap Catalog and Pilot Dispositions

Build only capabilities Conversations consumes in-pilot; all others remain follow-on backlog.

| # | Capability or gap | Current disposition | FR |
|---|---|---|---|
| 1 | `ICommandContract` / `IEventContract` compile-time metadata, parallel to existing `IQueryContract` | **Backlog.** Explicitly deferred from the pilot on 2026-07-14 because contract reshaping is unnecessary for the core boilerplate-reduction proof. | FR-16 |
| 2 | Polymorphic JSON registration helper / source-gen catalog | Publicize `TypeMapper` for in-pilot consumption. | FR-14 |
| 3 | Generic tenant-access projection handler | Build for in-pilot consumption. | FR-11 |
| 4 | Generic observability/health hook | **Consume/extend.** `EventStore.ServiceDefaults` already supplies `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and `MapDefaultEndpoints`; `EventStore.DomainService` supplies `AddEventStoreDomainTelemetry`. Consume these. If Conversations requires a generic hook that the platform-owned surface does not yet support, extend that surface; do not create a Conversations ServiceDefaults or hosting module. | FR-10 |
| 5 | Generic typed-HttpClient registration | Build for in-pilot consumption. | FR-12 |
| 6 | Generic naming, mode, component, or sidecar behavior for Aspire/DAPR topology | **Consume/extend.** `EventStore.Aspire` already supplies `AddHexalithEventStore` and `AddEventStoreDomainModule` for platform-owned shared/isolated DAPR topology. Consume these. If required generic behavior is unsupported, extend `EventStore.Aspire`; do not create a Conversations AppHost/Aspire/hosting module. | FR-13 |
| 7 | Tier-3 integration test harness (command→event→projection→query) | **Backlog.** | — |
| 8 | Snapshot/event-upcasting hook on `EventStoreAggregate<TState>` | **Backlog.** | — |
| 9 | Command-level authorization/validator discovery convention | **Backlog.** | — |
| 10 | Deadletter/poison-pill domain hook | **Backlog.** | — |

### PRD Completeness Assessment

The finalized PRD is complete and clear for the boilerplate-reduction initiative at the PRD level:

- All 20 initiative FR identifiers are present with testable consequences; FR-16 is unambiguously deferred, leaving 19 active initiative FRs.
- The preserved product-contract baseline contains a contiguous 104 Feature-FRs and 77 Feature-NFRs. These constrain FR-20/SM-C1 preservation evidence and do not automatically authorize legacy feature delivery.
- FR-20/SM-C1 define a frozen, versioned pre-refactor preservation denominator with an explicit approval rule for test removal or reclassification.
- SM-C2 defines a reproducible P95 regression threshold of no more than 5%; Feature-NFR9 and Feature-NFR12 retain their absolute product-contract definitions and require separate release activation.
- Scope, non-goals, module ownership, deferred work, assumptions, owners, and revisit triggers are explicit.
- The addendum separates technical-how from normative product requirements and identifies consume, extend, promote, and backlog dispositions.

Items requiring downstream traceability rather than further PRD authoring:

- OQ-1 remains an architecture-owned landing-zone decision for FR-10 through FR-15 and must be resolved before the corresponding implementation story begins.
- Preserved legacy release/product questions and Legacy-TQ1 through Legacy-TQ6 remain open provenance; they are not pilot scope unless separately activated.
- Epic coverage must distinguish active initiative FRs from preserved Feature-FR/Feature-NFR conformance obligations so the baseline is not misread as 181 new feature-delivery requirements.
- The five initiative NFR bullets and qualitative ownership constraints do not carry independent numeric IDs in the source PRD; coverage validation must trace them by their source labels and sections.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document claims coverage for all 20 initiative FR identifiers across Epics 1–5. It does not enumerate the preserved `Feature-FR1`–`Feature-FR104` baseline or map those obligations to individual preservation evidence.

### Initiative FR Coverage Matrix

| FR | PRD requirement | Epic coverage | Status |
|---|---|---|---|
| FR-1 | A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability. | Epic 1, Story 1.4 | ✓ Fully aligned |
| FR-2 | A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale. | Epic 1, Story 1.5 | ✓ Fully aligned |
| FR-3 | Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing. | Epic 2, Story 2.1 | ⚠ Partial — story retains Conversations server-host framing |
| FR-4 | Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts. | Epic 2, Story 2.3 | ✓ Fully aligned |
| FR-5 | Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics. | Epic 2, Story 2.4 | ✓ Fully aligned |
| FR-6 | Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits. | Epic 2, Story 2.5 | ✓ Fully aligned |
| FR-7 | Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior. | Epic 2, Story 2.2 | ✓ Fully aligned |
| FR-8 | Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules. | Epic 2, Story 2.6 | ✓ Fully aligned |
| FR-9 | Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers. | Epic 2, Story 2.7 | ✓ Fully aligned |
| FR-10 | The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 3, Story 3.4 | ⚠ Partial — proposes promoting a new base instead of consuming/extending the existing platform surface |
| FR-11 | A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules. | Epic 3, Story 3.2 | ✓ Fully aligned |
| FR-12 | A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation. | Epic 3, Story 3.1 | ✓ Fully aligned |
| FR-13 | The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 3, Story 3.5 | ⚠ Partial — proposes a promoted hosting base instead of consuming/extending EventStore.Aspire |
| FR-14 | A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition. | Epic 3, Story 3.6 | ✓ Fully aligned |
| FR-15 | A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies. | Epic 3, Story 3.3 | ✓ Fully aligned |
| FR-16 | Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative. | Epic 3, Story 3.7 | ✓ Covered as deferred — wording is stale and still conditional |
| FR-17 | Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot. | Epic 3 across Stories 3.1–3.7 | ⚠ Partial — inherits stale FR-10/FR-13 semantics and conditional FR-16 |
| FR-18 | A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module. | Epic 4, Story 4.1 | ⚠ Partial/conflicting — template includes domain-owned AppHost/Aspire/ServiceDefaults |
| FR-19 | The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2. | Epic 4, Story 4.2 | ⚠ Partial — lacks reproducible fixture and versioned measurement-artifact metadata |
| FR-20 | Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change. | Epic 1 baseline stories and Epic 5 attestation | ⚠ Partial — lacks the finalized frozen-manifest governance contract |

### Preserved Product-Contract FR Coverage Matrix

These Feature-FRs constrain FR-20/SM-C1 preservation. They are not automatically authorized as 104 new feature-delivery requirements, but each needs a traceable disposition in the frozen preservation manifest.

| FR | Preserved PRD requirement | Epic coverage | Status |
|---|---|---|---|
| Feature-FR1 | Adopter systems can create a tenant-scoped conversation record. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR2 | Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR3 | The system can represent conversation lifecycle state and allowed transitions, including active, archived, or closed states and any release-approved behavior for reopening or sealing. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR4 | Adopter systems can append ordered messages to an existing conversation. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR5 | Adopter systems can add human users, AI agents, and LLMs as conversation participants. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR6 | Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR7 | The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR8 | Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR9 | Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR10 | Adopter systems can update conversation title or metadata when that capability is included in the active release scope. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR11 | Adopter systems can close or archive a conversation when that capability is included in the active release scope. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR12 | The system can preserve a complete conversation record across provider session expiry, restart, or failover. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR13 | The system can attribute each conversation action to a stable Party identity. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR14 | The system can model humans, AI agents, and LLMs as attributable participants. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR15 | The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR16 | The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR17 | The system can preserve multi-provider attribution when a conversation crosses provider boundaries. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR18 | The system can reconstruct who said or changed what, when, and under which tenant context. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR19 | Adopter systems can attach file references to a conversation without storing file binaries in Conversations. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR20 | Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR21 | Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR22 | The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR23 | The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR24 | The system can keep conversations readable and attributable when upstream entities change lifecycle state. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR25 | The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR26 | The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR27 | The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR28 | The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR29 | The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR30 | The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR31 | The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR32 | The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR33 | The system can derive projections from ordered conversation events. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR34 | The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR35 | The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR36 | The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR37 | The system can expose projection lag or documented freshness behavior when read models are asynchronous. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR38 | Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR39 | Published events can carry explicit schema and version metadata. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR40 | The system can reject unsupported event, command, or projection schema versions with typed documented errors. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR41 | The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR42 | Authorized systems can set or replace a conversation retention policy with rationale. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR43 | Authorized systems can mark conversation content as sensitive. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR44 | Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR45 | The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR46 | The system can preserve the audit event stream while redacting projected or displayed content. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR47 | The system can require every governance mutation to have a paired audit event. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR48 | The system can reject governance mutations when audit recording is unavailable. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR49 | The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR50 | The system can reconstruct message state and governance state as they existed at a prior point in time. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR51 | The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR52 | The system can apply retention and redaction policy treatment to governance audit records themselves. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR53 | The system can define which actions on audit records are allowed or denied and when the records can be redacted, exported, or separately logged. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR54 | The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR55 | Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR56 | Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR57 | Compliance operators can filter or narrow conversation search by date range and business context. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR58 | Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR59 | Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR60 | Compliance operators can view a conversation's governance audit trail inline. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR61 | Compliance operators can view conversation state as of a selected historical time. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR62 | Compliance operators can copy citation-ready references for transcript and audit elements. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR63 | Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR64 | Operator and compliance workflows marked read-only cannot mutate conversation aggregate state. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR65 | Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR66 | Operators can run governance verification for a conversation, tenant, suite, or time window. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR67 | Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR68 | Verification results can distinguish governance verification failures from infrastructure or execution failures. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR69 | The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR70 | Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR71 | Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR72 | Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR73 | Adopter developers can run adopter-facing conformance tests before deployment. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR74 | Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR75 | Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR76 | The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR77 | The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR78 | The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR79 | The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR80 | The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR81 | The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR82 | The product can produce a signed conformance artifact for release gating. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR83 | The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR84 | The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR85 | The product can support a named-waiver process for release-gate exceptions. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR86 | The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR87 | The product can verify tenant isolation using adversarial positive and negative cases. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR88 | The product can verify idempotent command behavior under duplicate or reordered commands. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR89 | The product can verify redaction-replay correctness across projections, logs, traces, and errors. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR90 | The product can verify provider portability by proving recoverability without provider-owned session authority. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR91 | The product can verify event schema evolution through version-aware records and at least one worked additive-change example. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR92 | The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR93 | The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR94 | The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR95 | Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR96 | Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR97 | Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR98 | Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR99 | Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR100 | The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR101 | The product can expose release-scope consequences when substrate-defining capabilities are deferred. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR102 | The product can support buyer partial acceptance under the Option A v1 deal. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR103 | The product can track second-adopter status and trigger downgrade-rule review milestones. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |
| Feature-FR104 | The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities. | Epic 1 oracle plus Epic 5 FR-20 attestation only; no direct Feature-FR mapping | ❌ Missing direct preservation traceability |

### Missing and Partial Requirements

#### Critical: Feature-FR1–Feature-FR104 have no direct preservation mapping

The full preserved product-contract baseline is present in the finalized PRD, but the epics do not trace any individual Feature-FR to a frozen pre-refactor test, contract baseline, other evidence, or explicit non-activated disposition.

- **Impact:** Epic-level completion could claim preservation without demonstrating which product obligations are protected.
- **Recommendation:** Extend the Epic 1 preservation-manifest work and Epic 5 attestation so every Feature-FR maps to frozen evidence or to an explicit, owner-approved non-activated disposition. This is traceability work, not authorization to build all legacy features.

#### Critical: FR-20 is only partially covered

The current stories require a green suite, snapshots, a behavior ledger, and final attestation, but they do not require the finalized PRD's versioned manifest to bind commit/build identity, every denominator test, every contract baseline, and accepted green evidence. They also omit approval-controlled removal, replacement, and reclassification.

- **Impact:** The preservation denominator can drift during the refactor.
- **Recommendation:** Rewrite the relevant Epic 1 and Epic 5 acceptance criteria around the frozen, versioned manifest and named-owner exception workflow in FR-20/SM-C1.

#### Critical: platform ownership drifts in FR-3, FR-10, FR-13, and FR-18

Stories 2.1, 3.4, 3.5, and 4.1 still describe Conversations-owned hosting projects or promotion of new bases. The finalized PRD requires consume-first adoption of the existing platform host, `EventStore.ServiceDefaults`, `EventStore.DomainService`, and `EventStore.Aspire`, extending those platform surfaces only where a required generic hook is actually absent.

- **Impact:** Implementing the current stories would recreate the boilerplate and ownership boundary the initiative is intended to remove.
- **Recommendation:** Rewrite these stories to prohibit Conversations-owned AppHost, Aspire, ServiceDefaults, hosting, or equivalent runtime projects.

#### High-priority partial alignment

- **FR-3:** Story 2.1 preserves a Conversations server-host shape instead of requiring operation exclusively through the platform host.
- **FR-10:** Story 3.4 promotes a new ServiceDefaults base rather than consuming the existing platform surface and extending it only if necessary.
- **FR-13:** Story 3.5 promotes hosting topology rather than consuming `EventStore.Aspire`.
- **FR-17:** Epic 3's adoption proof inherits the stale FR-10/FR-13 semantics and treats deferred FR-16 as conditional.
- **FR-18:** Story 4.1's thin template includes AppHost/Aspire/ServiceDefaults as domain-owned projects, contradicting the finalized boundary.
- **FR-19:** Story 4.2 does not require a reproducible minimal-module fixture or the versioned artifact metadata needed to establish SM-2.
- **FR-20:** The preservation stories omit frozen-denominator governance and individual Feature-FR dispositions.

#### Decision-state drift

- The epics still ask OQ-3, OQ-4, and OQ-5 even though the finalized PRD resolves all three.
- The overview states that no architecture document exists, while `architecture.md` is included in the current planning set.
- FR-16 remains worded as conditional in the epics even though it is explicitly deferred.

### Coverage Statistics

- **Initiative FR identifiers present in epics:** 20/20 (100%)
- **Initiative FRs fully aligned with the finalized PRD:** 13/20 (65%)
- **Initiative FRs partially aligned:** 7/20 (35%)
- **Preserved Feature-FRs with direct evidence-level traceability:** 0/104 (0%)
- **Total PRD functional requirements assessed:** 124
- **Fully aligned direct traceability:** 13/124 (10.5%)
- **Interpretation:** the 104 Feature-FRs are preservation constraints, not 104 newly authorized delivery requirements; the gap is the missing trace from each constraint to frozen evidence or an explicit disposition.

## UX Alignment Assessment

### UX Document Status

**Found:**

- `ux-design-specification.md` — complete legacy product UX specification (1,546 lines, completed 2026-05-13).
- `ux-requirement-map.md` — 52 UX decision requirements (`UX-DR1`–`UX-DR52`) mapped to the former product-delivery epic structure.

The UX specification remains valuable as a **preservation and conformance reference** for the existing adopter/operator experience. It is not a current delivery specification for the boilerplate-reduction pilot unless a UX capability is separately activated.

### UX ↔ PRD Alignment

#### Aligned

- The UX's governed-case-file model, tenant-scoped search, fail-closed disclosure behavior, redaction safety, trust/freshness presentation, audit evidence, citation behavior, participant hydration, command availability, responsive disclosure controls, and WCAG 2.1 AA expectations are represented in the finalized PRD's preserved `Feature-FR` and `Feature-NFR` baseline.
- The UX's business continuity and developer create/append/read journeys remain consistent with preserved product behavior and therefore constrain FR-20/SM-C1 evidence.
- The initiative PRD does not authorize new adopter-facing UX behavior. Its active journeys concern maintainers, SDK owners, and domain-module authors; the absence of new UI flows for FR-1–FR-20 is therefore appropriate.

#### Gaps and drift

- **Critical — stale UX story traceability:** `ux-requirement-map.md` maps all 52 UX-DRs to the former product-delivery story numbering. In the current epics, Story 3.1 is typed-HttpClient registration, Story 3.2 is tenant-access projection promotion, and Stories 3.3–3.7 are other shared-capability refactors. Story 3.8, Story 4.4, Story 6.8, and Story 2.4.2 do not exist. Identical story numbers now mean unrelated work, so the map can produce false implementation claims.
- **Critical — no preservation-evidence mapping:** none of `UX-DR1`–`UX-DR52`, `AC-SAFE-001`–`AC-SAFE-008`, the responsive acceptance criteria, or the canonical leakage fixtures are mapped to the current FR-20 preservation manifest or Epic 5 attestation.
- **Warning — stale provenance:** both UX artifacts were authored against the former root `prd.md`, which is now archived. Their frontmatter and introductory traceability text do not identify the finalized 2026-07-14 PRD or clarify preservation-only status.
- **Warning — scope ambiguity:** the UX document contains a phased component implementation roadmap. Without an explicit preservation-only banner, an implementer could incorrectly treat trust components, mobile work, forensic views, and acceptance UI as pilot delivery scope.

### UX ↔ Architecture Alignment

#### Supported architecture decisions

- Architecture strongly supports the UX trust contract: server-owned projections and command-availability metadata, permission-safe DTOs, no client-inferred trust, no raw EventStore UX, tenant-safe search, independently authorized drawers, fail-closed states, and content-safe observability.
- FrontComposer plus Fluent UI Blazor is consistently identified as the generated-first UI foundation, with custom-reviewed trust components for evidence timeline, redaction, audit, citation, freshness, temporal navigation, and command safety.
- Architecture addresses responsive and accessibility disclosure surfaces, Party hydration degradation, projection freshness, batching/caching, async heavy workflows, redaction replay, and cross-tenant non-enumeration.

#### Architectural misalignments affecting UX delivery

- **Critical — platform ownership conflict:** architecture explicitly scaffolds and assigns runtime responsibility to `Hexalith.Conversations.AppHost` and `Hexalith.Conversations.ServiceDefaults`. The finalized PRD requires Conversations to own neither and to consume the existing platform host, `EventStore.ServiceDefaults` / `EventStore.DomainService`, and `EventStore.Aspire`, extending platform-owned surfaces only for demonstrated generic gaps. The architecture's starter commands, project tree, infrastructure section, development workflow, and readiness conclusion are therefore invalid for the current initiative.
- **High — stale release activation:** architecture treats the legacy P95 open-conversation target as a primary architectural driver. The finalized PRD preserves the absolute Feature-NFR9/Feature-NFR12 definitions but activates them only through an explicit release plan; the pilot's active performance gate is SM-C2's reproducible no-more-than-5% P95 regression.
- **High — architecture status is obsolete:** the architecture concludes `READY FOR IMPLEMENTATION` with no critical gaps, despite the now-material host-ownership conflict and the finalized decisions made on 2026-07-14.
- **Medium — UX performance proof is incomplete:** architecture provides suitable mechanisms (projection-shaped reads, precomputed trust posture, batched Party hydration, bounded async workflows) but does not trace the UX's 90-second investigation goal, three-second orientation goal, or trust-metadata loading expectations to current measurement stories or evidence.

### Warnings and Required Corrections

1. Mark the UX specification as a preserved product-experience/conformance reference for this pilot; do not execute its component roadmap by default.
2. Replace the stale UX story-number map with mappings from each UX-DR and safety criterion to FR-20 manifest evidence, an explicit non-activated disposition, or a separately approved delivery story.
3. Update architecture before implementation: remove Conversations-owned AppHost/ServiceDefaults/Aspire/hosting projects and rewrite composition around existing platform-owned capabilities.
4. Reconcile architecture performance language with SM-C2 and explicit release activation of absolute legacy targets.
5. Re-run UX ↔ architecture traceability after the architecture and epics are updated; the current documents cannot support reliable story-level UX coverage claims.

## Epic Quality Review

### Review Scope

Reviewed all five epics and all 24 stories against user-value focus, epic independence, story sizing, forward dependencies, BDD acceptance criteria, brownfield integration, starter-template consistency, and FR traceability.

### Epic Structure and Independence

| Epic | User-value assessment | Dependency assessment | Quality result |
|---|---|---|---|
| Epic 1 — Boilerplate Baseline & Behavior-Preservation Oracle | Provides legitimate release-owner and maintainer safety value, although framed as a technical gate. | No forward epic dependency. Stories progress from baseline to gap analysis, test decoupling, inventory, and dispute handling. | ⚠ Major update required: baseline semantics predate the finalized manifest and accepted inventory. |
| Epic 2 — Consume Existing Technical-Module Surface | Provides maintainability value while preserving adopter behavior, but is predominantly a technical refactor milestone. | Correctly depends on Epic 1; no circular epic dependency. Story 2.6 and Story 2.7 introduce conditional future-capability dependencies. | ❌ Not independently executable as written. |
| Epic 3 — Promote → Adopt | Provides SDK-maintainer reuse value, but is a technical pipeline rather than a standalone product outcome. | Correctly follows Epic 2, but every story is gated by unresolved OQ-1; stale Story 3.7 remains conditional after OQ-4 was resolved. | ❌ Not implementation-ready. |
| Epic 4 — Thin Authoring Template & Cost Proof | Clear domain-module-author outcome. | Correctly depends only on earlier epics. | ❌ Acceptance criteria encode the wrong project ownership and incomplete measurement evidence. |
| Epic 5 — Preservation Attestation | Clear release-owner outcome. | Correctly depends on completed prior epics; no forward/circular dependency. | ❌ Final gate does not implement the finalized FR-20/SM-C1/SM-C2 contract. |

The epic sequence itself is backward-only: Epic 1 → Epic 2 → Epic 3 → Epic 4 → Epic 5. No circular epic dependency was found. The repeated `Standalone: yes` claims are overstated for Epics 2–5 because each explicitly requires prior-epic outputs, though this is acceptable sequential independence under the workflow rule.

### Story-by-Story Quality Assessment

| Story | Result | Principal finding |
|---|---|---|
| 1.1 | ❌ Critical | Pins a hard-coded set of 14 tests and a contract snapshot, but does not create the finalized versioned preservation manifest binding commit/build identity, every denominator test, every contract baseline, and accepted green evidence. |
| 1.2 | ⚠ Major | “Coverage/mutation analysis” and “weakly asserted” have no named tool, command, threshold, bounded output, or completion rule; the story can expand indefinitely. |
| 1.3 | ✓ Structurally sound | Clear test-level outcomes and backward-only dependency; must ultimately register results in the versioned manifest governance model, not only a loose ledger. |
| 1.4 | ⚠ Major | Treats the ≈18,000 LOC discovery estimate as pending confirmation even though the accepted Story 1.4 baseline is now 13,289 LOC (37.15%) in the canonical versioned inventory. Story status and AC are stale. |
| 1.5 | ✓ Structurally sound | Clear reviewer value, testable reclassification behavior, and proper dependency on Story 1.4; should reference named approval and versioned artifact update where FR-20 evidence is affected. |
| 2.1 | ❌ Critical | Preserves a Conversations server-host project and two-line local host wiring; finalized FR-3 requires operation through the platform host with no Conversations-owned AppHost/Aspire/ServiceDefaults/runtime-host project. |
| 2.2 | ✓ Mostly sound | Small remove-and-replace slice with behavior tests and no forward dependency. Manifested idempotency evidence must remain explicit. |
| 2.3 | ✓ Mostly sound | Cohesive query/cursor slice with round-trip behavior and contract preservation. |
| 2.4 | ⚠ Major | “No hot-path read regression” is not measurable; it omits the reproducible pre/post fixture and finalized ≤5% P95 threshold. |
| 2.5 | ✓ Mostly sound | Cohesive projection-orchestration slice with backward dependency on Story 1.3 and explicit retained domain logic. |
| 2.6 | ❌ Critical | Contains a conditional forward dependency on Epic 3/Story 3.6 if the public `TypeMapper` surface is insufficient. A Story 2.x cannot require future Epic 3 work to finish. Split/resequence the gap or make Story 2.6 consume only the already-demonstrated surface. |
| 2.7 | ⚠ Major | Story intent includes consuming “shared ServiceDefaults” while Story 3.4 is supposed to create/promote that capability, producing an ambiguous forward dependency. Test-only scope and assertion-strength evidence are otherwise sound. |
| 3.1 | ✓ Mostly sound | Good tracer-bullet sequencing and independently revertible capability slice; still blocked on OQ-1 landing-zone resolution. |
| 3.2 | ⚠ Major | Safety AC are strong, but the story combines cross-repository generic design, promotion, adoption, deletion, differential adversarial tests, sibling CI, conformance, and pointer updates. Confirm it fits one iteration or split promotion from pilot adoption without weakening end-to-end acceptance. |
| 3.3 | ✓ Mostly sound | Cohesive telemetry helper slice; should name the frozen metric-name/cardinality baseline artifact. Blocked on OQ-1. |
| 3.4 | ❌ Critical | Creates and adopts a new shared ServiceDefaults base plus a Conversations ServiceDefaults hook project, contrary to the consume/extend existing platform-owned surface decision. |
| 3.5 | ❌ Critical | Creates/adopts a shared hosting base into a Conversations AppHost placeholder, contrary to the `EventStore.Aspire` consume-first and no Conversations-owned hosting decision. |
| 3.6 | ⚠ Major | Conditional “as needed” publicization and dependency on the gap from Story 2.6 make the boundary uncertain; OQ-1 is unresolved. The story must state the exact existing surface consumed or exact approved additive extension. |
| 3.7 | ❌ Critical | OQ-4 is resolved: FR-16 is deferred. Keeping an implementable conditional branch creates unauthorized pilot scope. Close the story as deferred/backlog and remove it from the active sequence and runbook. |
| 4.1 | ❌ Critical | Template requires domain-owned Aspire/AppHost/ServiceDefaults projects and stale promoted-base wiring, directly contradicting finalized FR-18. |
| 4.2 | ⚠ Major | Measures files/LOC but omits the required reproducible fixture, frozen inclusion rules, source paths, measurement commands/tool versions, commit/build identity, versioned artifact, and named acceptance. |
| 5.1 | ❌ Critical | Allows contract differences to be merely “approved and recorded” and lacks the frozen manifest, named approver, rationale, compatibility evidence, denominator governance, and Feature-FR dispositions. |
| 5.2 | ❌ Critical | Uses a loose removed-test ledger and only “plumbing-only” rationales; FR-20 requires approval-controlled removal/replacement/reclassification with replacement evidence where applicable and a versioned manifest update. |
| 5.3 | ❌ Critical | Affirms generic “no hot-path latency regression” instead of SM-C2 ≤5%, and assembles no manifest-version identity or individual preserved-requirement traceability. |

### Dependency Defects

#### Forward dependencies

1. **Story 2.6 → future Epic 3/Story 3.6:** the story can discover that it needs a promoted polymorphic registry and then declares that future work as its dependency. Resolve the prerequisite before Story 2.6 or move the affected adoption into the same later capability story.
2. **Story 2.7 → Story 3.4 ambiguity:** Story 2.7 says it consumes shared ServiceDefaults while Story 3.4 creates the shared base. Rewrite Story 2.7 to use only already-existing test infrastructure and platform defaults.

#### External/precondition blockers

- Every active Epic 3 story depends on OQ-1, which remains unresolved. This is an explicit architecture decision blocker, not a future-story dependency, but those stories are not ready for implementation until their exact platform landing zones are recorded.
- Story 3.7 depends on OQ-4 even though the decision is already resolved as deferred; this is stale, not a legitimate blocker.

#### Valid backward dependencies

- Stories 1.2/1.3 use the Story 1.1 oracle; Story 1.5 uses the Story 1.4 inventory.
- Story 2.5 uses the Story 1.3 test disposition.
- Story 3.1 establishes the runbook for later Epic 3 stories.
- Story 4.2 uses Story 4.1; Epic 5 uses prior baseline, implementation, measurement, and conformance outputs.

No circular dependency was found.

### Acceptance-Criteria Quality

Strengths:

- All 24 stories have an explicit persona, desired outcome, rationale, and acceptance criteria.
- Most ACs use Given/When/Then and identify observable artifacts or test results.
- Brownfield deletion, compatibility, test retention, sibling-module build checks, and per-story conformance are generally explicit.
- No up-front database/entity creation defect was found; database timing is not applicable to this refactor.

Defects:

- The inherited standing conformance gate is stale and cannot substitute for the versioned FR-20 manifest rules.
- Several ACs rely on vague comparators: “weakly asserted,” “equivalent,” “preserved,” “green,” or “no regression” without frozen fixtures and exact thresholds.
- Failure/error coverage is strong for tenant access and conformance, but thin for host adoption, platform-extension fallback, measurement reproducibility, manifest mutation, and performance execution failure.
- Conditional phrases such as “if needed,” “otherwise,” and “if built” leave implementation scope unresolved inside executable stories.

### Starter and Brownfield Checks

- The architecture specifies a composite scaffold and says project initialization must be the first implementation story; Epic 1 instead begins with conformance baselining. This is a **critical inter-document contradiction**.
- The current initiative is a brownfield refactor, and the finalized PRD rejects the architecture's Conversations-owned AppHost/ServiceDefaults scaffold. The correct remediation is to update the architecture and then decide whether any consume-first foundation wiring story is necessary—not to add the stale scaffold automatically.
- Brownfield integration and compatibility concerns are otherwise well represented through removal of local plumbing, adoption of existing SDK seams, sibling-module CI, contract diffs, conformance suites, and submodule pointer updates.

### Required Remediation Before Implementation

1. Update architecture ownership and resolve OQ-1.
2. Rewrite Stories 2.1, 3.4, 3.5, and 4.1 to consume existing platform hosting/defaults/topology and prohibit Conversations-owned runtime-host projects.
3. Close Story 3.7 as deferred.
4. Replace Story 1.1/5.1/5.2/5.3's snapshot-and-ledger model with the finalized versioned preservation manifest and approval workflow.
5. Add direct dispositions/evidence mappings for Feature-FR1–Feature-FR104 and UX safety obligations.
6. Remove the Story 2.6 and Story 2.7 forward dependencies by resequencing or narrowing scope.
7. Make Stories 2.4 and 5.3 use the reproducible SM-C2 ≤5% P95 gate; make Story 4.2 satisfy the full FR-19 artifact contract.
8. Re-evaluate the size of Story 1.2 and Story 3.2 after the exact tooling and landing zones are known.

## Summary and Recommendations

### Overall Readiness Status

## NOT READY

The finalized PRD is implementation-ready at the PRD quality level, but the planning set is not ready to hand to implementation. The architecture, epics, and UX traceability were authored against older product and ownership assumptions and have not been reconciled to the 2026-07-14 PRD decisions.

Proceeding now would create a high probability of:

- building Conversations-owned AppHost, Aspire, ServiceDefaults, or hosting code that the current PRD explicitly prohibits;
- treating deferred FR-16 as active work;
- claiming behavior preservation without a frozen, versioned denominator or direct preserved-requirement evidence;
- accepting unmeasurable performance and authoring-cost results;
- reporting UX coverage through story numbers that now refer to unrelated refactor work.

### Critical Issues Requiring Immediate Action

1. **Architecture ownership is incompatible with the PRD.** The starter commands, project tree, infrastructure decision, development workflow, and handoff create Conversations-owned AppHost and ServiceDefaults projects. FR-3, FR-10, FR-13, and FR-18 require platform-owned hosting/defaults/topology and consume-first use of existing EventStore capabilities.
2. **The preservation gate is underspecified downstream.** Epics use a green suite, snapshot, and loose removal ledger instead of FR-20/SM-C1's versioned manifest binding source commit/build, exact tests, contract baselines, accepted green evidence, named approvals, rationale, replacement evidence, and versioned mutation history.
3. **Preserved behavior has no direct traceability.** Feature-FR1–Feature-FR104 and UX-DR1–UX-DR52/safety criteria are not mapped to frozen evidence or explicit non-activated dispositions.
4. **Active stories implement superseded scope.** Stories 2.1, 3.4, 3.5, and 4.1 encode the wrong hosting/project boundary. Story 3.7 still allows FR-16 implementation even though OQ-4 deferred it.
5. **OQ-1 is unresolved.** Every active promote/extend story lacks an approved landing zone, so shared capability ownership and target modules are not implementation-safe.
6. **Performance and measurement gates are not testable as finalized.** Stories omit SM-C2's reproducible ≤5% P95 threshold and FR-19's reproducible fixture, frozen inclusion rules, command/tool versions, commit/build identity, versioned results, and named acceptance.
7. **UX traceability is actively misleading.** Current UX-DR mappings reuse story numbers whose meanings changed and reference nonexistent Stories 2.4.2, 3.8, 4.4, and 6.8.
8. **Forward story dependencies remain.** Story 2.6 can depend on future Story 3.6, and Story 2.7 ambiguously consumes a capability Story 3.4 is expected to create.

### Recommended Next Steps

1. **Update `architecture.md` first.** Rebase it on the finalized PRD; remove Conversations-owned runtime-host projects; document consumption of the existing platform host, `EventStore.ServiceDefaults` / `EventStore.DomainService`, and `EventStore.Aspire`; resolve OQ-1; replace the obsolete `READY FOR IMPLEMENTATION` conclusion.
2. **Regenerate or comprehensively update `epics.md`.** Rewrite Stories 2.1, 3.4, 3.5, and 4.1; close Story 3.7 as deferred; update OQ-3/OQ-4/OQ-5 state; eliminate forward dependencies; synchronize the accepted 13,289-LOC baseline.
3. **Make the preservation manifest the spine of Epics 1 and 5.** Map every denominator test, contract baseline, Feature-FR, applicable Feature-NFR, UX-DR, and safety criterion to evidence or an explicit approved non-activated disposition.
4. **Correct measurement stories.** Add the exact SM-C2 benchmark protocol and the complete FR-19 reproducibility artifact contract.
5. **Repair UX artifact governance.** Mark the UX specification preservation-only for this pilot, update its PRD provenance, and replace the stale story-number map with manifest/evidence dispositions or separately approved delivery stories.
6. **Re-run epic coverage and quality validation.** Confirm 20/20 initiative FRs are semantically aligned, all preserved obligations have traceable dispositions, no forward dependencies remain, and every active story has bounded testable AC.
7. **Re-run implementation readiness.** Implementation should begin only after architecture, epics, UX traceability, and manifest evidence agree with the finalized PRD.

### Consolidated Finding Count

- **15 consolidated issue clusters across five categories:** requirements traceability, architecture/ownership, scope/provenance, story dependencies/quality, and measurement/evidence.
- **Story-level quality:** 10 of 24 stories have critical defects, 7 have major defects, and 7 are structurally sound or need only targeted synchronization.
- **Initiative FR alignment:** 13 of 20 fully aligned; 7 partially aligned.
- **Preserved Feature-FR evidence traceability:** 0 of 104 directly mapped.
- **UX decision traceability:** 0 of 52 mappings are reliable against the current epic semantics.

### Final Note

The PRD reconciliation successfully established a coherent product contract and explicit pilot decisions. The blocker is downstream synchronization, not missing product intent. Address the critical issues before implementation; after those corrections, this should be a straightforward readiness re-check rather than another requirements-discovery cycle.

**Assessment date:** 2026-07-14
**Assessor:** Codex, applying the BMad Implementation Readiness workflow
