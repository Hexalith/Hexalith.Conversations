---
project: Hexalith.Conversations
date: 2026-05-15
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md
  architecture:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\architecture.md
  epicsAndStories:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\epics.md
  uxDesign:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-15
**Project:** Hexalith.Conversations

## Document Discovery

### PRD Files Found

**Whole Documents:**
- prd.md (153,780 bytes, modified 2026-05-10 15:45:32)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- architecture.md (80,751 bytes, modified 2026-05-14 11:58:36)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- epics.md (150,875 bytes, modified 2026-05-15 16:01:29)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- ux-design-specification.md (117,645 bytes, modified 2026-05-13 19:47:52)

**Sharded Documents:**
- None found

### Discovery Issues

- No duplicate whole/sharded document formats found.
- No required document category is missing.

## PRD Analysis

### Functional Requirements

FR1: Adopter systems can create a tenant-scoped conversation record.
FR2: Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names.
FR3: The system can represent conversation lifecycle state and allowed transitions, including active, archived or closed, and any release-approved reopening or sealing behavior.
FR4: Adopter systems can append ordered messages to an existing conversation.
FR5: Adopter systems can add human users, AI agents, and LLMs as conversation participants.
FR6: Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions.
FR7: The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics.
FR8: Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context.
FR9: Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity.
FR10: Adopter systems can update conversation title or metadata when that capability is included in the active release scope.
FR11: Adopter systems can close or archive a conversation when that capability is included in the active release scope.
FR12: The system can preserve a complete conversation record across provider session expiry, restart, or failover.
FR13: The system can attribute each conversation action to a stable Party identity.
FR14: The system can model humans, AI agents, and LLMs as attributable participants.
FR15: The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
FR16: The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
FR17: The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
FR18: The system can reconstruct who said or changed what, when, and under which tenant context.
FR19: Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
FR20: Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
FR21: Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
FR22: The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
FR23: The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
FR24: The system can keep conversations readable and attributable when upstream entities change lifecycle state.
FR25: The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.
FR26: The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
FR27: The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
FR28: The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
FR29: The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
FR30: The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
FR31: The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
FR32: The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.
FR33: The system can derive projections from ordered conversation events.
FR34: The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
FR35: The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
FR36: The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
FR37: The system can expose projection lag or documented freshness behavior when read models are asynchronous.
FR38: Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
FR39: Published events can carry explicit schema and version metadata.
FR40: The system can reject unsupported event, command, or projection schema versions with typed documented errors.
FR41: The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.
FR42: Authorized systems can set or replace a conversation retention policy with rationale.
FR43: Authorized systems can mark conversation content as sensitive.
FR44: Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution.
FR45: The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history.
FR46: The system can preserve the audit event stream while redacting projected or displayed content.
FR47: The system can require every governance mutation to have a paired audit event.
FR48: The system can reject governance mutations when audit recording is unavailable.
FR49: The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state.
FR50: The system can reconstruct message state and governance state as they existed at a prior point in time.
FR51: The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata.
FR52: The system can apply retention and redaction policy treatment to governance audit records themselves.
FR53: The system can define which actions on audit records are allowed, denied, redacted, exported, or separately logged.
FR54: The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data.
FR55: Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record.
FR56: Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID.
FR57: Compliance operators can filter or narrow conversation search by date range and business context.
FR58: Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness.
FR59: Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy.
FR60: Compliance operators can view a conversation's governance audit trail inline.
FR61: Compliance operators can view conversation state as of a selected historical time.
FR62: Compliance operators can copy citation-ready references for transcript and audit elements.
FR63: Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract.
FR64: Operator and compliance workflows marked read-only cannot mutate conversation aggregate state.
FR65: Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited.
FR66: Operators can run governance verification for a conversation, tenant, suite, or time window.
FR67: Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks.
FR68: Verification results can distinguish governance verification failures from infrastructure or execution failures.
FR69: The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial.
FR70: Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors.
FR71: Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback.
FR72: Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline.
FR73: Adopter developers can run adopter-facing conformance tests before deployment.
FR74: Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior.
FR75: Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages.
FR76: The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces.
FR77: The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities.
FR78: The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues.
FR79: The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility.
FR80: The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.
FR81: The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages.
FR82: The product can produce a signed conformance artifact for release gating.
FR83: The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability.
FR84: The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies.
FR85: The product can support a named-waiver process for release-gate exceptions.
FR86: The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior.
FR87: The product can verify tenant isolation using adversarial positive and negative cases.
FR88: The product can verify idempotent command behavior under duplicate or reordered commands.
FR89: The product can verify redaction-replay correctness across projections, logs, traces, and errors.
FR90: The product can verify provider portability by proving recoverability without provider-owned session authority.
FR91: The product can verify event schema evolution through version-aware records and at least one worked additive-change example.
FR92: The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release.
FR93: The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests.
FR94: The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable.
FR95: Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
FR96: Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
FR97: Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
FR98: Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
FR99: Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.
FR100: The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
FR101: The product can expose release-scope consequences when substrate-defining capabilities are deferred.
FR102: The product can support buyer partial acceptance under the Option A v1 deal.
FR103: The product can track second-adopter status and trigger downgrade-rule review milestones.
FR104: The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

Total FRs: 104

### Non-Functional Requirements

NFR1: Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
NFR2: Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
NFR3: Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
NFR4: GA implementation cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with named owner and review date.
NFR5: Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
NFR6: Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
NFR7: A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
NFR8: Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.
NFR9: Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
NFR10: The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
NFR11: Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
NFR12: Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
NFR13: Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
NFR14: Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
NFR15: Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stops into one ambiguous number.
NFR16: Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
NFR17: Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
NFR18: Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
NFR19: Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
NFR20: Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
NFR21: Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.
NFR22: The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
NFR23: Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
NFR24: Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
NFR25: DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
NFR26: A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
NFR27: Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
NFR28: The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
NFR29: Backup restore and tenant-scoped recovery procedures must be tested before production release.
NFR30: The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
NFR31: Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
NFR32: Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
NFR33: Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
NFR34: Tenant-events lag must have an SLO and a defined request behavior during lag windows.
NFR35: Redaction propagation latency must have an SLO covering all materialization surfaces listed in NFR21.
NFR36: The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
NFR37: Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.
NFR38: v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
NFR39: Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
NFR40: Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
NFR41: Event schema evolution must include one worked additive-change example before GA.
NFR42: Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
NFR43: Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.
NFR44: Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
NFR45: Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`, or document why an equivalent shape is not available.
NFR46: The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
NFR47: Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
NFR48: During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.
NFR49: Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
NFR50: Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
NFR51: Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
NFR52: Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
NFR53: The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
NFR54: Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.
NFR55: Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
NFR56: Operational signals must be tenant-safe and content-safe by default.
NFR57: Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
NFR58: Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
NFR59: `governance verify` / conformance verification output must be machine-readable and suitable for CI and incident workflows.
NFR60: Privileged operational actions must include structured justification and produce reviewable audit records.
NFR61: Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.
NFR62: Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
NFR63: Every release must produce a signed conformance artifact and versioned manifest mapping tests to FRs, NFRs, carry-forward commitments, pass criteria, waiver status, measurement method, and environment.
NFR64: Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
NFR65: Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
NFR66: The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
NFR67: Retention behavior must be tenant-aware and produce verifiable evidence.
NFR68: Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.
NFR69: Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
NFR70: Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
NFR71: Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
NFR72: Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
NFR73: Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
NFR74: Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
NFR75: Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
NFR76: Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
NFR77: User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.

Total NFRs: 77

### Additional Requirements

- The release mode is phased: v1 GA, v1.1 at GA + 90 days under Option A, and vNext. Full PRD FR/NFR coverage is not automatically v1 scope unless assigned to the active phase, acceptance criteria, success criteria, or Foundation Gates.
- v1 GA CORE includes tenant-scoped conversation aggregate, chatbot CORE commands, EventStore persistence, idempotency, pub/sub publication, chatbot projection subset, fail-closed tenant isolation, sensitive-data/redaction policy mechanism, code-level governance enforcement, .NET client + contract package, operator read-only viewer, conformance suite, provider portability migration test, and semver/deprecation commitments.
- Non-cuttable foundation gates include tenant isolation conformance, idempotency property tests, audit-write fail-closed behavior, adopter-runnable conformance test pack, and schema-evolution ADR/envelope/worked additive-change example.
- Pre-kickoff blockers remain: commit to the 16-18 week v1 timeline or contract success criteria; decide raw HTTP fallback versus .NET client; confirm EventStore envelope stability or project-owned evolution; confirm v1 event consumption by other modules; confirm architect/second-engineer availability; ratify Foundation Gate blocking semantics; decide whether MarkSensitiveData/RedactMessageContent is CORE.
- Open questions remain for architecture and sign-off: migrated-tenants attribution coverage, conformance manifest and signed CI commitment, Generate Evidence Bundle timing, chatbot ship deadline/blocking status, and buyer authority for downgrade-rule framing.
- Technical constraints are binding: fail-closed tenant access via local Hexalith.Tenants projection, audit invariant enforced through aggregate base type plus property test, redaction-with-audit rather than cryptographic deletion in v1, idempotent command behavior, provider portability as testable proof obligation, additive event schema evolution, and P95 <= 500ms warm-cache open-conversation target under the defined envelope.
- Integration boundaries are explicit: Tenants owns tenant lifecycle; Parties owns stable participant identity; Projects/Folders own upstream business/file references; EventStore owns persistence and publication mechanics; FrontComposer supplies generated/composed admin surfaces; LLM provider IDs are metadata only.

### PRD Completeness Assessment

The PRD is unusually complete in breadth and gives strong downstream traceability: it defines 104 FRs, 77 NFRs, release phasing, CORE scope, foundation gates, integration boundaries, anti-scope, cut order, and buyer-facing acceptance evidence. It is ready for coverage validation against epics.

Implementation readiness is not automatically green, though. The PRD itself names pre-kickoff blockers and unresolved choices that must be closed or explicitly waived before sprint 1: timeline versus contracted criteria, EventStore envelope ownership, command/core redaction scope, capacity/cost numeric thresholds, and cross-module event-consumption status. These should be treated as readiness risks during the remaining validation steps.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- Epic 1 covers FR1-FR41 through setup, contracts, aggregate, tenant access, idempotency, projections, upstream reference resolution, domain events, replay, schema versioning, and projection rebuild behavior.
- Epic 2 covers FR42-FR55 through governance policy, retention, sensitive marking, redaction, audit pairing, point-in-time reconstruction, audit record governance, and privileged operational justification.
- Epic 3 covers FR56-FR69 through compliance investigation workflows, governed evidence reading, redaction and audit inspection, citations, temporal evidence links, read-only safeguards, governance verification, buyer demo, and accessibility/responsiveness support.
- Epic 4 covers FR70-FR80 through contract package, compatibility metadata, .NET client, typed sanitized errors, onboarding diagnostics, adopter conformance tests, caller metadata, and developer guidance.
- Epic 5 covers FR81-FR94 through compatibility/deprecation policy, signed conformance artifacts, traceability manifests, waivers, tenant isolation/idempotency/redaction/provider/schema verification, executable contract tests, and module/platform evidence separation.
- Epic 6 covers FR95-FR104 through operational observability, conformance status, release scope classification, partial acceptance, second-adopter tracking, downgrade milestones, responsibility documentation, and telemetry redaction/cardinality validation.

Total FRs in epics: 104

### Coverage Matrix

| FR Number | Epic Coverage | Status |
| --- | --- | --- |
| FR1 | Story 1.1, Story 1.3 | Covered |
| FR2 | Story 1.1, Story 1.2, Story 1.3 | Covered |
| FR3 | Story 1.1, Story 1.3 | Covered |
| FR4 | Story 1.1, Story 1.4 | Covered |
| FR5 | Story 1.1, Story 1.4 | Covered |
| FR6 | Story 1.1, Story 1.2, Story 1.3, Story 1.6 | Covered |
| FR7 | Story 1.1, Story 1.2, Story 1.3, Story 1.6 | Covered |
| FR8 | Story 1.1, Story 1.8 | Covered |
| FR9 | Story 1.1, Story 1.8 | Covered |
| FR10 | Story 1.1, Story 1.8 | Covered |
| FR11 | Story 1.1, Story 1.8 | Covered |
| FR12 | Story 1.1, Story 1.3, Story 1.8, Story 1.11 | Covered |
| FR13 | Story 1.1, Story 1.2, Story 1.4 | Covered |
| FR14 | Story 1.1, Story 1.2, Story 1.4 | Covered |
| FR15 | Story 1.1, Story 1.2, Story 1.3, Story 1.4 | Covered |
| FR16 | Story 1.1, Story 1.2, Story 1.3, Story 1.4 | Covered |
| FR17 | Story 1.1, Story 1.2, Story 1.4 | Covered |
| FR18 | Story 1.1, Story 1.2, Story 1.4 | Covered |
| FR19 | Story 1.1, Story 1.2, Story 1.4 | Covered |
| FR20 | Story 1.1, Story 1.2, Story 1.3, Story 1.4 | Covered |
| FR21 | Story 1.1, Story 1.2, Story 1.3, Story 1.4, Story 1.8 | Covered |
| FR22 | Story 1.1, Story 1.2, Story 1.3, Story 1.4, Story 1.8 | Covered |
| FR23 | Story 1.1, Story 1.9 | Covered |
| FR24 | Story 1.1, Story 1.9 | Covered |
| FR25 | Story 1.1, Story 1.9 | Covered |
| FR26 | Story 1.1, Story 1.2, Story 1.5 | Covered |
| FR27 | Story 1.1, Story 1.5 | Covered |
| FR28 | Story 1.1, Story 1.5, Story 1.8 | Covered |
| FR29 | Story 1.1, Story 1.5, Story 1.8 | Covered |
| FR30 | Story 1.1, Story 1.2, Story 1.5, Story 1.8 | Covered |
| FR31 | Story 1.1, Story 1.5 | Covered |
| FR32 | Story 1.1, Story 1.5, Story 1.10 | Covered |
| FR33 | Story 1.1, Story 1.7, Story 1.11 | Covered |
| FR34 | Story 1.1, Story 1.7, Story 1.11 | Covered |
| FR35 | Story 1.1, Story 1.7, Story 1.11 | Covered |
| FR36 | Story 1.1, Story 1.7, Story 1.8, Story 1.11 | Covered |
| FR37 | Story 1.1, Story 1.7, Story 1.8, Story 1.11 | Covered |
| FR38 | Story 1.1, Story 1.10 | Covered |
| FR39 | Story 1.1, Story 1.2, Story 1.10 | Covered |
| FR40 | Story 1.1, Story 1.2, Story 1.10, Story 1.11 | Covered |
| FR41 | Story 1.1, Story 1.2, Story 1.11 | Covered |
| FR42 | Story 2.1, Story 2.2 | Covered |
| FR43 | Story 2.1, Story 2.3 | Covered |
| FR44 | Story 2.1, Story 2.4 | Covered |
| FR45 | Story 2.1, Story 2.4 | Covered |
| FR46 | Story 2.1, Story 2.4 | Covered |
| FR47 | Story 2.1, Story 2.2, Story 2.3, Story 2.4, Story 2.5 | Covered |
| FR48 | Story 2.1, Story 2.2, Story 2.3, Story 2.5 | Covered |
| FR49 | Story 2.1, Story 2.2, Story 2.3, Story 2.5 | Covered |
| FR50 | Story 2.6 | Covered |
| FR51 | Story 2.1, Story 2.4, Story 2.7 | Covered |
| FR52 | Story 2.1, Story 2.7 | Covered |
| FR53 | Story 2.1, Story 2.7 | Covered |
| FR54 | Story 2.8 | Covered |
| FR55 | Story 2.8 | Covered |
| FR56 | Story 3.1, Story 3.8 | Covered |
| FR57 | Story 3.1, Story 3.8 | Covered |
| FR58 | Story 3.2, Story 3.8 | Covered |
| FR59 | Story 3.3, Story 3.8 | Covered |
| FR60 | Story 3.3, Story 3.8 | Covered |
| FR61 | Story 3.8 | Covered |
| FR62 | Story 3.4, Story 3.8 | Covered |
| FR63 | Story 3.4, Story 3.8 | Covered |
| FR64 | Story 3.5, Story 3.8 | Covered |
| FR65 | Story 3.5, Story 3.8 | Covered |
| FR66 | Story 3.6, Story 3.8 | Covered |
| FR67 | Story 3.6, Story 3.8 | Covered |
| FR68 | Story 3.6, Story 3.8 | Covered |
| FR69 | Story 3.7, Story 3.8 | Covered |
| FR70 | Story 4.1 | Covered |
| FR71 | Story 4.2 | Covered |
| FR72 | Story 4.2 | Covered |
| FR73 | Story 4.5 | Covered |
| FR74 | Story 4.2, Story 4.5, Story 4.7 | Covered |
| FR75 | Story 4.1 | Covered |
| FR76 | Story 4.6 | Covered |
| FR77 | Story 4.4 | Covered |
| FR78 | Story 4.3, Story 4.7 | Covered |
| FR79 | Story 4.4, Story 4.7 | Covered |
| FR80 | Story 4.3 | Covered |
| FR81 | Story 5.1 | Covered |
| FR82 | Story 5.2 | Covered |
| FR83 | Story 5.3 | Covered |
| FR84 | Story 5.3 | Covered |
| FR85 | Story 5.4 | Covered |
| FR86 | Story 5.2, Story 5.4 | Covered |
| FR87 | Story 5.5 | Covered |
| FR88 | Story 5.6 | Covered |
| FR89 | Story 5.7 | Covered |
| FR90 | Story 5.8 | Covered |
| FR91 | Story 5.9 | Covered |
| FR92 | Story 5.10 | Covered |
| FR93 | Story 5.10 | Covered |
| FR94 | Story 5.11 | Covered |
| FR95 | Story 6.1, Story 6.8 | Covered |
| FR96 | Story 6.2, Story 6.8 | Covered |
| FR97 | Story 6.2, Story 6.8 | Covered |
| FR98 | Story 6.1, Story 6.8 | Covered |
| FR99 | Story 6.3, Story 6.8 | Covered |
| FR100 | Story 6.4 | Covered |
| FR101 | Story 6.4 | Covered |
| FR102 | Story 6.5 | Covered |
| FR103 | Story 6.6 | Covered |
| FR104 | Story 6.7 | Covered |

### Missing Requirements

No missing PRD functional requirements were found. Every PRD FR1-FR104 has at least one explicit epic/story coverage reference.

### Coverage Statistics

- Total PRD FRs: 104
- FRs covered in epics: 104
- FRs missing from epics: 0
- FRs referenced in epics but not found in PRD: 0
- Coverage percentage: 100%

### Coverage Notes

- Story 1.1 claims starter-template/foundation support for FR1-FR41, while behavioral implementation coverage is provided by Stories 1.2-1.11. I treated Story 1.1 as supporting coverage and the later stories as the implementation path.
- Story 3.8 and Story 6.8 include validation/support coverage for FR ranges plus NFR ranges. NFR references were excluded from the FR coverage count.

## UX Alignment Assessment

### UX Document Status

Found: `D:\Hexalith.Conversations\_bmad-output\planning-artifacts\ux-design-specification.md`

The UX specification is complete and explicitly framed around the v1 administration/governance surface plus adopter and developer experience touchpoints. It defines the core Find -> Read -> Trust experience, FrontComposer/Fluent UI foundation, trust posture patterns, command gates, evidence timeline, citation, temporal navigation, responsive behavior, WCAG 2.1 AA expectations, and disclosure/leakage safety gates.

### UX to PRD Alignment

- The UX defining experience, Find -> Read -> Trust, aligns with PRD operator/compliance FR56-FR69 and the Sarah/Maya/Atlas/Diego/Marcus/Julian/Helen/Daniel journeys.
- UX trust posture, stale projection, redaction, degraded hydration, and audit-state patterns align with PRD FR33-FR37, FR42-FR55, FR58-FR68, FR95-FR99, and NFR44-NFR48.
- UX FrontComposer-first strategy aligns with PRD guidance to use contract-first FrontComposer annotations for admin commands/projections and not hand-build a separate portal.
- UX responsive and accessibility requirements align with PRD NFR69-NFR77, including WCAG 2.1 AA, keyboard/screen-reader parity, non-color-only state communication, safe degraded messages, and 90-second diagnosis scenarios.
- UX leakage and disclosure rules align with PRD NFR16-NFR21, NFR55-NFR62, and the fail-closed tenant isolation/non-enumeration requirements.

### UX to Architecture Alignment

- Architecture supports the UX surface by assigning FrontComposer to baseline admin composition and requiring custom trust components for evidence timeline, trust posture, redaction, audit trail, citation copy, temporal navigation, projection freshness, and degraded states.
- Architecture supports UX trust posture through server-owned trust/freshness states, permission-safe DTOs, command availability metadata, projection freshness metadata, and shared error/freshness/trust vocabulary.
- Architecture supports disclosure safety by requiring tenant/redaction rules across visible UI, hidden DOM, ARIA labels, live regions, tooltips, clipboard payloads, telemetry, screenshots, and release evidence.
- Architecture supports performance-sensitive UX by recognizing the P95 <= 500ms warm-cache open-conversation target and requiring read models shaped for Find -> Read -> Trust, batched/cached Party hydration, and separate paths for normal reads, temporal reconstruction, verification, export, and rebuild.
- Architecture supports accessibility and responsive concerns through WCAG 2.1 AA, mobile safe-triage defaults, and trust-state parity across visual, keyboard, screen-reader, clipboard, and responsive surfaces.

### Alignment Issues

- Temporal evidence is UX-critical, but the architecture still lists the authoritative temporal evidence anchor as an open question: event position, projection version, timestamp, or composite. This must be resolved before implementing stable temporal evidence links and time-travel UX.
- The UX depends on command gates backed by source-owned command availability metadata. Architecture supports this pattern, but dependent implementation should wait for the relevant ADR/contract decision so UI code does not infer action safety client-side.
- The UX requires disclosure/leakage checks such as Leak Sentinel across DOM, accessibility tree, clipboard, URLs, telemetry, screenshots, and responsive duplicates. Architecture supports the invariant, but the exact helper/test implementation still needs to be created in the implementation stories.
- UX partial-load behavior requires trust metadata before or with trust-bearing content. Architecture supports projection freshness, but dependent stories must explicitly test skeletons, lazy loading, virtualization, drawer authorization, and permission downgrade states.

### Warnings

- No UX document is missing; no missing-UX warning applies.
- UX scope is larger than simple generated admin CRUD. Implementers must not treat FrontComposer generation alone as sufficient for evidence timeline, redaction, trust posture, citation, temporal navigation, accessibility disclosure, or command safety.
- Architecture readiness is high for foundation implementation but medium for GA release evidence until ADRs, numeric envelopes, and conformance artifacts are completed. That caveat directly affects UX readiness for trust-bearing screens.

## Epic Quality Review

### Overall Quality Assessment

The epic/story document is substantially stronger than a normal first pass. It maintains full FR traceability, uses user-role story framing, includes BDD-style acceptance criteria, preserves tenant/redaction/audit safety concerns, and avoids the worst anti-pattern of building a transcript table or raw technical milestone plan.

However, implementation readiness is not clean. The main risk is sequencing: several release-gating conformance capabilities are placed in later epics even though the PRD says Foundation Gates block CORE story closure. A second risk is story size: some stories bundle multiple independently testable command surfaces or cross-surface safety obligations that are too large for reliable implementation and review.

### Critical Violations

None found.

No epic is purely a technical milestone with no user value. Epic 1 is foundation-heavy, but it directly produces the tenant-safe conversation record needed by adopters. Epic 5 and Epic 6 are platform-owner/operator focused rather than end-user focused, but they map directly to release evidence, conformance, observability, and lifecycle commitments in the PRD.

### Major Issues

1. Foundation gate sequencing is inconsistent with the PRD.

- Evidence: PRD says tenant isolation conformance, idempotency property tests, audit-write fail-closed behavior, adopter conformance pack, and schema evolution work are Foundation Gates with CI-passing required before CORE story closure.
- Epic document placement: tenant isolation conformance is Story 5.5, idempotent command conformance is Story 5.6, redaction replay conformance is Story 5.7, provider portability is Story 5.8, and schema evolution proof is Story 5.9.
- Impact: Earlier CORE implementation stories in Epic 1 and Epic 2 can appear complete before their release-gating proof exists. That creates a forward dependency from earlier CORE stories to later Epic 5 stories.
- Recommendation: Pull minimal gate implementations forward or mark the affected earlier stories as not closable until the corresponding conformance gate story is complete. At minimum, add explicit "cannot close without gate X" acceptance criteria to Stories 1.5, 1.6, 1.11, 2.5, and 2.4.

2. Story 1.4 combines multiple command surfaces.

- Evidence: Story 1.4 covers add participant, append message, file references, upstream business references, lifecycle rejection behavior, replay, and multi-provider attribution.
- Impact: This is likely too large for one implementation story and blurs the CORE command selection decision around `AttachFileReference`, `MarkSensitiveData`, and `RedactMessageContent`.
- Recommendation: Split into `AddParticipant`, `AppendMessage`, and `AttachFileReference` stories, or explicitly mark file/upstream reference handling as a follow-on story if it is not in the first CORE loop.

3. Story 2.4 is too broad for a single redaction implementation story.

- Evidence: Story 2.4 covers redaction command behavior plus projections, read models, search materializations, evidence views, caches, exports, accessibility output, clipboard payloads, logs, traces, errors, and future derived indexes.
- Impact: The story spans runtime command behavior, all disclosure surfaces, and future derived indexes. This is too broad to complete independently and makes review difficult.
- Recommendation: Split into redaction command/event behavior, projection/read-model redaction, UI/accessibility/clipboard disclosure safety, and operational/log/trace/export redaction verification. Future derived indexes should remain ADR-gated unless promoted.

4. Story 3.8 is a large validation bundle rather than a small completable story.

- Evidence: It covers desktop/tablet/mobile/wide desktop, permission-safe DTOs, keyboard and screen-reader flow, redaction/accessibility output, clipboard, tooltip, browser title, telemetry, loading, empty, denied, stale, and responsive-duplicate states.
- Impact: This is important work, but as a single story it risks becoming an open-ended QA epic.
- Recommendation: Split into responsive layout safety, accessibility tree/keyboard flow, and leakage/clipboard/browser/telemetry tests, or make it an epic-level validation checklist with smaller implementation stories underneath.

5. Story 6.8 is likely too broad for one observability validation story.

- Evidence: It validates metrics, logs, traces, diagnostics, dashboards, evidence summaries, bounded identifiers, correlation metadata, high-cardinality inputs, redaction events, cross-tenant denials, provider errors, malformed metadata, and privileged access.
- Impact: This is a cross-cutting conformance suite, not a single story-sized increment.
- Recommendation: Split by surface or gate: metrics cardinality, logs/traces redaction, diagnostics/evidence summaries, and privileged/cross-tenant operational scenarios.

### Minor Concerns

- Story 1.1 references "future stories" in acceptance criteria. This is acceptable because the architecture requires a starter-template setup story, but the story should remain strictly non-operative and should not become a placeholder dumping ground.
- Story 4.2 includes raw HTTP fallback when buyer-accepted or required for diagnostics. This is compatible with the PRD only if the buyer explicitly accepts it; otherwise the .NET client remains CORE.
- Several stories use very dense acceptance criteria. They are testable, but implementation agents will need story files with scoped tasks, explicit out-of-scope notes, and required test lanes to avoid overbuilding.

### Dependency Analysis

- Epic 1 can stand alone as the tenant-safe conversation record foundation.
- Epic 2 depends on Epic 1 output, which is valid.
- Epic 3 depends on Epic 1 and Epic 2 output, which is valid.
- Epic 4 depends on Epic 1 contracts and read/write behavior, which is valid, though client/happy-path work should not close before the relevant server contracts are stable.
- Epic 5 validates and packages release evidence for earlier behavior. This is valid as a release-evidence epic but conflicts with Foundation Gate timing if earlier CORE stories are allowed to close before the gate evidence exists.
- Epic 6 depends on prior operational signals and release evidence concepts, which is valid.

### Database / Entity Creation Timing

No upfront database/table anti-pattern was found. The epic plan preserves EventStore as write authority and treats projections, caches, exports, evidence, and UI state as derived or non-authoritative. Projection/read-model work appears where first needed rather than as a generic "create all tables" story.

### Starter Template And Greenfield/Brownfield Checks

- Architecture specifies a composite Hexalith .NET/Aspire scaffold.
- Epic 1 Story 1.1 correctly sets up the initial project from the approved starter/scaffold shape.
- The plan reflects greenfield-in-brownfield reality: new Conversations projects are scaffolded, while integration boundaries with EventStore, Tenants, Parties, FrontComposer, Dapr/Aspire, and sibling module conventions are explicit.
- Root-level submodule policy is preserved in Story 1.1 acceptance criteria.

### Best Practices Compliance Checklist

| Epic | User Value | Independent In Sequence | Story Sizing | No Forward Dependency | Clear ACs | FR Traceability |
| --- | --- | --- | --- | --- | --- | --- |
| Epic 1 | Pass | Pass | Needs splits in 1.4 | Conditional issue via gates | Pass | Pass |
| Epic 2 | Pass | Pass | Needs split in 2.4 | Conditional issue via gates | Pass | Pass |
| Epic 3 | Pass | Pass | Needs split in 3.8 | Pass | Pass | Pass |
| Epic 4 | Pass | Pass | Mostly pass | Buyer fallback caveat | Pass | Pass |
| Epic 5 | Pass | Pass as release evidence; timing issue as Foundation Gate | Some large validation stories | Major sequencing risk | Pass | Pass |
| Epic 6 | Pass | Pass | Needs split in 6.8 | Pass | Pass | Pass |

### Remediation Summary

- Reconcile Foundation Gate timing before sprint planning. Either move minimal conformance gate stories earlier or make earlier CORE stories explicitly unclosable until their corresponding gate stories pass.
- Split oversized stories before implementation, especially Story 1.4, Story 2.4, Story 3.8, and Story 6.8.
- Preserve the current traceability map while splitting stories so FR coverage does not regress.
- Add explicit story-level stop conditions for ADR-gated decisions: temporal evidence anchor, command availability metadata, projection freshness blocking semantics, EventStore envelope ownership, and raw HTTP fallback.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK.

The planning package is close and unusually well traced: all required documents exist, the PRD is complete, every PRD FR has epic coverage, UX is aligned with PRD and architecture, and the architecture gives a credible implementation shape.

It is not cleanly ready for implementation kickoff because the artifacts still contain major sequencing and sizing risks. In particular, Foundation Gates that the PRD says must block CORE story closure appear later in the epic plan, and several stories are too broad to implement, review, and test safely as single increments.

### Critical Issues Requiring Immediate Action

No critical document-discovery or FR-coverage gaps were found.

The highest-priority issues to resolve before sprint planning are:

1. Foundation Gate sequencing is inconsistent with the PRD. Tenant isolation conformance, idempotency conformance, audit-write fail-closed behavior, redaction replay, provider portability, and schema evolution proof must not arrive only after CORE stories appear complete.
2. Story 1.4 is too broad because it combines add participant, append message, file references, upstream references, lifecycle rejection, replay, and multi-provider attribution.
3. Story 2.4 is too broad because it combines redaction command behavior with projection/read-model/search/evidence/cache/export/accessibility/clipboard/log/trace/error behavior.
4. Story 3.8 and Story 6.8 are validation bundles large enough to behave like mini-epics.
5. ADR-gated choices still need explicit closure or stop conditions: temporal evidence anchor, command availability metadata, projection freshness blocking semantics, EventStore envelope ownership, raw HTTP fallback, and numeric capacity/performance thresholds.

### Recommended Next Steps

1. Rework the epic/story plan before implementation: pull minimal Foundation Gate work forward or mark affected CORE stories as unclosable until the corresponding gate passes.
2. Split oversized stories while preserving traceability: Story 1.4, Story 2.4, Story 3.8, and Story 6.8 should become smaller implementation and verification stories.
3. Add explicit stop conditions to story files for ADR-gated decisions so implementation agents do not make silent assumptions.
4. Resolve pre-kickoff buyer decisions from the PRD: timeline versus contracted criteria, EventStore envelope stability/evolution, v1 event consumption, architect/second-engineer availability, Foundation Gate blocking semantics, and whether redaction/sensitive-data commands are CORE.
5. Keep the current FR coverage matrix as the traceability baseline after story splitting and rerun readiness once the story plan is revised.

### Issue Count

This assessment identified 13 issues or concerns across 4 categories:

- PRD/pre-kickoff decision blockers: 1 category-level issue.
- UX/architecture alignment risks: 4 issues.
- Epic quality major issues: 5 issues.
- Epic quality minor concerns: 3 concerns.

### Final Note

The artifacts are strong enough to revise, not discard. The product thinking is coherent; the weak point is execution packaging. Address the Foundation Gate sequencing and story sizing before implementation begins. Proceeding as-is would likely produce false progress: stories could close while the proof obligations that make them safe are still downstream.

**Assessment Date:** 2026-05-15
**Assessor:** Codex using `bmad-check-implementation-readiness`
