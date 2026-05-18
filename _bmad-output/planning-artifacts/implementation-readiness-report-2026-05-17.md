---
workflow: bmad-check-implementation-readiness
date: 2026-05-17
project: Hexalith.Conversations
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallReadinessStatus: NEEDS WORK
includedFiles:
  prd:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\prd.md
  architecture:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\architecture.md
  epics:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\epics.md
  ux:
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\ux-design-specification.md
    - D:\Hexalith.Conversations\_bmad-output\planning-artifacts\ux-requirement-map.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-17
**Project:** Hexalith.Conversations

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- `prd.md` (153,780 bytes, modified 2026-05-10 15:45:32)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `architecture.md` (80,751 bytes, modified 2026-05-14 11:58:36)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `epics.md` (176,459 bytes, modified 2026-05-17 14:38:23)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `ux-design-specification.md` (117,645 bytes, modified 2026-05-13 19:47:52)
- `ux-requirement-map.md` (8,825 bytes, modified 2026-05-16 10:45:26)

**Sharded Documents:**
- None found

### Discovery Issues

- No critical duplicate whole/sharded document formats found.
- No required document category is missing.

## Step 2: PRD Analysis

### Functional Requirements

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
- FR13: The system can attribute each conversation action to a stable Party identity.
- FR14: The system can model humans, AI agents, and LLMs as attributable participants.
- FR15: The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
- FR16: The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- FR17: The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
- FR18: The system can reconstruct who said or changed what, when, and under which tenant context.
- FR19: Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
- FR20: Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
- FR21: Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
- FR22: The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
- FR23: The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
- FR24: The system can keep conversations readable and attributable when upstream entities change lifecycle state.
- FR25: The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.
- FR26: The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
- FR27: The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- FR28: The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
- FR29: The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
- FR30: The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
- FR31: The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
- FR32: The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.
- FR33: The system can derive projections from ordered conversation events.
- FR34: The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
- FR35: The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
- FR36: The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- FR37: The system can expose projection lag or documented freshness behavior when read models are asynchronous.
- FR38: Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
- FR39: Published events can carry explicit schema and version metadata.
- FR40: The system can reject unsupported event, command, or projection schema versions with typed documented errors.
- FR41: The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.
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
- FR95: Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
- FR96: Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
- FR97: Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
- FR98: Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
- FR99: Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.
- FR100: The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
- FR101: The product can expose release-scope consequences when substrate-defining capabilities are deferred.
- FR102: The product can support buyer partial acceptance under the Option A v1 deal.
- FR103: The product can track second-adopter status and trigger downgrade-rule review milestones.
- FR104: The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

Total FRs: 104

### Non-Functional Requirements

- NFR1: Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
- NFR2: Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
- NFR3: Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
- NFR4: GA implementation cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with named owner and review date.
- NFR5: Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
- NFR6: Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
- NFR7: A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
- NFR8: Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.
- NFR9: Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- NFR10: The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
- NFR11: Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
- NFR12: Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
- NFR13: Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
- NFR14: Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
- NFR15: Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stops into one ambiguous number.
- NFR16: Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
- NFR17: Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
- NFR18: Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
- NFR19: Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
- NFR20: Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
- NFR21: Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.
- NFR22: The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
- NFR23: Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
- NFR24: Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
- NFR25: DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
- NFR26: A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
- NFR27: Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
- NFR28: The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
- NFR29: Backup restore and tenant-scoped recovery procedures must be tested before production release.
- NFR30: The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
- NFR31: Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
- NFR32: Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- NFR33: Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
- NFR34: Tenant-events lag must have an SLO and a defined request behavior during lag windows.
- NFR35: Redaction propagation latency must have an SLO covering all materialization surfaces listed in NFR21.
- NFR36: The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
- NFR37: Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.
- NFR38: v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
- NFR39: Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
- NFR40: Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
- NFR41: Event schema evolution must include one worked additive-change example before GA.
- NFR42: Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
- NFR43: Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.
- NFR44: Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
- NFR45: Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`, or document why an equivalent shape is not available.
- NFR46: The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- NFR47: Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
- NFR48: During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.
- NFR49: Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
- NFR50: Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
- NFR51: Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
- NFR52: Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
- NFR53: The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
- NFR54: Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.
- NFR55: Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
- NFR56: Operational signals must be tenant-safe and content-safe by default.
- NFR57: Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
- NFR58: Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
- NFR59: `governance verify` / conformance verification output must be machine-readable and suitable for CI and incident workflows.
- NFR60: Privileged operational actions must include structured justification and produce reviewable audit records.
- NFR61: Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.
- NFR62: Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
- NFR63: Every release must produce a signed conformance artifact and versioned manifest mapping tests to FRs, NFRs, carry-forward commitments, pass criteria, waiver status, measurement method, and environment.
- NFR64: Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
- NFR65: Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
- NFR66: The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
- NFR67: Retention behavior must be tenant-aware and produce verifiable evidence.
- NFR68: Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.
- NFR69: Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
- NFR70: Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
- NFR71: Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
- NFR72: Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
- NFR73: Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
- NFR74: Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
- NFR75: Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
- NFR76: Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
- NFR77: User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.

Total NFRs: 77

### Additional Requirements

- The project is explicitly classified as an audit-governed, event-sourced, multi-tenant, fail-closed API/backend substrate for AI-assisted business records.
- v1 compliance scope supports tenant-scoped lookup, reconstructed transcripts with attributed redactions, time-travel views, and audit trails for governance events; full GDPR automation, module-level SOC2/ISO/HIPAA/PCI commitments, legal-hold automation, and cryptographic redaction are outside v1.
- All conversation reads and writes are tenant-scoped and must fail closed on missing, stale, lagging, rolled-back, ambiguous, mismatched, or unknown tenant projection state.
- Governance mutations require paired audit events with same correlation, same tenant, causal adjacency, and transaction-boundary enforcement through an aggregate base type plus property-test safety net.
- Commands are tenant-scoped and idempotent; the PRD lists 9 command candidates and assigns the chatbot CORE loop to `CreateConversation`, `AppendMessage`, `AddParticipant`, and 1-2 of `AttachFileReference`, `RedactMessageContent`, or `MarkSensitiveData`.
- Projections are tenant-scoped and read-time resolved through stable upstream IDs; v1 chatbot read path is expected to use 2-3 of 9 projections.
- Integration dependencies are explicit: Hexalith.Tenants, Parties, Projects, Folders, EventStore, FrontComposer, and LLM provider metadata, with Conversations storing stable IDs and not upstream-owned personal or binary data.
- Error handling requires a typed, sanitized error envelope with audit handle and documentation pointer; error hygiene forbids leaking target tenant IDs, cross-tenant Party IDs, inaccessible conversation existence, redacted content, or provider payloads.
- v1 GA is phased as a Platform MVP with 12 CORE items, 5 Foundation Gates, a defined cut order, and red lines for non-cuttable substrate requirements.
- Pre-kickoff buyer questions remain binding: .NET client/raw HTTP fallback, EventStore envelope stability, v1 event consumption, architecture capacity, second-adopter candidate, Foundation Gate blocking semantics, and whether sensitive-data/redaction commands are CORE.

### PRD Completeness Assessment

The PRD is unusually complete for traceability: it has explicit FR/NFR numbering, scoped release phases, named integration boundaries, failure modes, governance invariants, and measurable acceptance gates. The main readiness risks are not missing requirement text; they are unresolved pre-kickoff decisions and NFRs that deliberately require numeric thresholds, evidence artifacts, owner decisions, or buyer-accepted unknowns before implementation kickoff.

## Step 3: Epic Coverage Validation

### Epic FR Coverage Extracted

Story-level `Requirements Covered` mappings were extracted from [epics.md](</D:/Hexalith.Conversations/_bmad-output/planning-artifacts/epics.md>). Story 1.1 claims architectural starter-template foundation support for FR1-FR41, but behavioral implementation coverage for those FRs is provided by Stories 1.2-1.11 and is counted separately below.

- Story 1.2: FR2, FR6, FR7, FR13-FR22, FR26, FR30, FR39-FR41.
- Story 1.3: FR1-FR3, FR6, FR7, FR12, FR15, FR16, FR20-FR22.
- Story 1.4: FR5, FR13-FR18.
- Story 1.4.1: FR4, FR6, FR7, FR13-FR18.
- Story 1.4.2: FR15-FR22.
- Story 1.5: FR26-FR32.
- Story 1.6: FR6, FR7.
- Story 1.7: FR33-FR37.
- Story 1.8: FR8-FR12, FR21, FR22, FR28-FR30, FR36, FR37.
- Story 1.9: FR23-FR25.
- Story 1.10: FR32, FR38-FR40.
- Story 1.11: FR12, FR33-FR37, FR40, FR41.
- Story 2.1: FR42-FR49, FR51-FR53.
- Story 2.2: FR42, FR47-FR49.
- Story 2.3: FR43, FR47-FR49.
- Story 2.4: FR44-FR47, FR51.
- Story 2.4.1: FR44-FR46, FR50, FR58-FR61.
- Story 2.4.2: FR44-FR46, FR59, FR62, FR63.
- Story 2.4.3: FR44-FR47, FR89 validation support.
- Story 2.5: FR47-FR49.
- Story 2.6: FR50.
- Story 2.7: FR51-FR53.
- Story 2.8: FR54, FR55.
- Story 3.1: FR56, FR57.
- Story 3.2: FR58.
- Story 3.3: FR59, FR60.
- Story 3.4: FR62, FR63.
- Story 3.5: FR64, FR65.
- Story 3.6: FR66-FR68.
- Story 3.7: FR69.
- Story 3.8: FR56-FR69 verification support.
- Story 4.1: FR70, FR75.
- Story 4.2: FR71, FR72, FR74.
- Story 4.3: FR78, FR80.
- Story 4.4: FR77, FR79.
- Story 4.5: FR73, FR74.
- Story 4.6: FR76.
- Story 4.7: FR74, FR78, FR79.
- Story 5.1: FR81.
- Story 5.2: FR82, FR86.
- Story 5.3: FR83, FR84.
- Story 5.4: FR85, FR86.
- Story 5.5: FR87.
- Story 5.6: FR88.
- Story 5.7: FR89.
- Story 5.8: FR90.
- Story 5.9: FR91.
- Story 5.10: FR92, FR93.
- Story 5.11: FR94.
- Story 6.1: FR95, FR98.
- Story 6.2: FR96, FR97.
- Story 6.3: FR99.
- Story 6.4: FR100, FR101.
- Story 6.5: FR102.
- Story 6.6: FR103.
- Story 6.7: FR104.
- Story 6.8: FR95-FR99 validation support.

### Coverage Matrix

The full PRD requirement text for FR1-FR104 is captured in Step 2. The traceability comparison found the following coverage status:

| FR Range | Primary Epic Coverage | Status |
| -------- | --------------------- | ------ |
| FR1-FR41 | Epic 1: Tenant-Safe Conversation Record | Covered |
| FR42-FR55 | Epic 2: Governed Retention, Redaction, and Audit | Covered |
| FR56-FR69 | Epic 3: Compliance Investigation Workspace, with supporting redaction stories in Epic 2 | Covered |
| FR70-FR80 | Epic 4: Adopter Integration and Developer Readiness | Covered |
| FR81-FR94 | Epic 5: Conformance, Compatibility, and Release Evidence | Covered |
| FR95-FR104 | Epic 6: Operations, Observability, and Lifecycle Commitments | Covered |

### Missing Requirements

No PRD FRs are missing from the epics and stories document.

### FRs in Epics but Not in PRD

No extra FR numbers outside PRD FR1-FR104 were found in story-level `Requirements Covered` mappings.

### Coverage Statistics

- Total PRD FRs: 104
- FRs covered in epics: 104
- Coverage percentage: 100%

## Step 4: UX Alignment Assessment

### UX Document Status

UX documentation exists and was reviewed:

- [ux-design-specification.md](</D:/Hexalith.Conversations/_bmad-output/planning-artifacts/ux-design-specification.md>)
- [ux-requirement-map.md](</D:/Hexalith.Conversations/_bmad-output/planning-artifacts/ux-requirement-map.md>)

The UX requirement map defines UX-DR1 through UX-DR52. All 52 UX-DR identifiers are referenced in story-level coverage in [epics.md](</D:/Hexalith.Conversations/_bmad-output/planning-artifacts/epics.md>), with no extra or unmapped UX-DR identifiers found.

### UX to PRD Alignment

- The UX definition of `Find -> Read -> Trust` aligns with PRD operator and compliance workflows FR56-FR69.
- Trust posture, freshness, redaction, degraded states, denied states, citation, and temporal reconstruction align with PRD governance/audit requirements FR42-FR69 and NFR44-NFR48, NFR69-NFR77.
- Developer-facing confidence through contracts, diagnostics, conformance tests, and safe typed errors aligns with PRD developer experience requirements FR70-FR80.
- UX disclosure-surface safety aligns with PRD tenant isolation and privacy requirements FR26-FR32, FR80, NFR16-NFR21, and NFR55-NFR61.
- Responsive and accessibility requirements align with PRD accessibility and human-trust NFRs NFR69-NFR77.

### UX to Architecture Alignment

- Architecture supports FrontComposer as the generated-first admin baseline and reserves custom components for evidence timeline, trust posture, redaction, audit trail, citation copy, temporal navigation, projection freshness, and degraded states.
- Architecture explicitly treats trust, freshness, redaction, tenant isolation, and provenance as domain outputs rendered by UI, not client-side inference.
- Architecture requires permission-safe DTOs per disclosure surface and names visible UI, hidden DOM, ARIA, live regions, tooltips, clipboard, browser titles, responsive duplicates, telemetry, screenshots, logs, traces, exports, and evidence artifacts as disclosure surfaces.
- Architecture supports desktop-first admin workflows, mobile safe triage, WCAG 2.1 AA, keyboard/screen-reader/clipboard safety, and custom-reviewed trust components.
- Architecture maps FR56-FR69 operator workflows to `Admin/TrustComponents`, `Admin/EvidenceTimeline`, and `Admin/TemporalNavigation`.

### Alignment Issues

- No blocking UX/PRD/architecture misalignment found.
- Watch item: UX-DR51 names a concrete `Leak Sentinel` helper, while architecture describes the underlying disclosure-surface and redaction non-disclosure tests more generally. Preserve the named helper or explicitly document the equivalent architecture-owned test mechanism before implementation closes Story 3.8.

### Warnings

- No warning for missing UX documentation.
- UX and architecture both imply UI implementation risk is high because disclosure surfaces include responsive duplicate markup, hidden DOM, accessibility text, clipboard output, telemetry, and screenshots. Story 3.8 and related tests should remain release-gating, not optional polish.

## Step 5: Epic Quality Review

### Review Scope

Reviewed [epics.md](</D:/Hexalith.Conversations/_bmad-output/planning-artifacts/epics.md>) against create-epics-and-stories standards.

- Epics reviewed: 6
- Stories reviewed: 58
- Stories with `Requirements Covered`: 58 of 58
- Stories with Given/When/Then acceptance criteria: 58 of 58
- External readiness gate tracker found: [readiness-gates.md](</D:/Hexalith.Conversations/_bmad-output/implementation-artifacts/readiness-gates.md>)

### Epic Structure Validation

| Epic | User Value Focus | Independence | Assessment |
| ---- | ---------------- | ------------ | ---------- |
| Epic 1: Tenant-Safe Conversation Record | Strong adopter value: create, append, retrieve, list, replay tenant-scoped records. | Stands alone as foundation and usable first slice. | Pass |
| Epic 2: Governed Retention, Redaction, and Audit | Strong governance value: apply retention/redaction/sensitive-data controls with audit evidence. | Uses Epic 1 conversation record output; no forward dependency found. | Pass |
| Epic 3: Compliance Investigation Workspace | Strong operator value: find, inspect, time-travel, cite, verify governed evidence. | Uses Epic 1 and 2 outputs; no dependency on later epics required for core function. | Pass with watch on Story 3.8 split. |
| Epic 4: Adopter Integration and Developer Readiness | Strong developer value: contracts, client, diagnostics, conformance tests. | Can use prior contracts and runtime behavior; no dependency on Epic 5 release packaging to deliver developer integration. | Pass |
| Epic 5: Conformance, Compatibility, and Release Evidence | Strong platform-owner/release-owner value: signed evidence, waivers, manifest, compatibility. | Correctly consumes prior local evidence and adds release-gate aggregation; no forward dependency found. | Pass |
| Epic 6: Operations, Observability, and Lifecycle Commitments | Strong operator/product-owner value: safe operational health and lifecycle commitments. | Can use prior signals and evidence; no dependency on future epics found. | Pass with watch on Story 6.8 split. |

### Critical Violations

None found.

### Major Issues

1. Story 3.8 remains a bundled validation/checklist story unless split before assignment.
   - Evidence: Story 3.8 states it remains an epic-level verification checklist unless the sprint plan splits it into Story 3.8A, 3.8B, and 3.8C.
   - Impact: If assigned as one ordinary implementation story, it violates independent story sizing and mixes responsive layout, accessibility tree, keyboard/screen-reader safety, leakage, clipboard, browser, and telemetry disclosure domains.
   - Current mitigation: [readiness-gates.md](</D:/Hexalith.Conversations/_bmad-output/implementation-artifacts/readiness-gates.md>) marks the Story 3.8 assignment plan as `decided` and says to split by default unless a named evidence owner accepts the combined checklist.
   - Recommendation: Materialize Story 3.8A/3.8B/3.8C before sprint assignment, or record the named evidence owner, fixture set, evidence output, pass/fail gate, and review date.

2. Story 6.8 remains a bundled telemetry validation/checklist story unless split before assignment.
   - Evidence: Story 6.8 states it must not be assigned as ordinary single-owner implementation work without checklist-mode evidence ownership; split mode creates Story 6.8A and Story 6.8B.
   - Impact: If assigned as-is, it mixes telemetry redaction and telemetry cardinality validation, which may require different owners and fixture sets.
   - Current mitigation: [readiness-gates.md](</D:/Hexalith.Conversations/_bmad-output/implementation-artifacts/readiness-gates.md>) marks the Story 6.8 assignment plan as `decided` and says to split by default unless a named SRE/test owner accepts both surfaces.
   - Recommendation: Materialize Story 6.8A/6.8B before sprint assignment, or record the named owner and combined checklist evidence plan.

### Minor Concerns

1. Story 1.1 is a technical setup story, but it is allowed by the workflow because architecture specifies an initial starter/scaffold requirement.
   - Evidence: Story 1.1 is titled `Set Up Initial Project from Starter Template` and includes strict scope control.
   - Recommendation: Keep Story 1.1 limited to scaffold, smoke tests, ADR folders/templates, and readiness tracker links. Do not let it absorb conversation persistence, tenant authorization, projections, governance commands, or partial domain behavior.

2. Several stories are intentionally gated by external decisions in `readiness-gates.md`.
   - Evidence: Stories 1.7, 1.8, 1.11, 3.1, 3.2, 3.4, 4.2, 4.4, 5.9, 6.2, 3.8, and 6.8 include Ready for Dev preconditions.
   - Current state: the referenced readiness gates are present and marked `decided`.
   - Recommendation: When story files are generated, copy the relevant gate link into each story so implementation agents do not miss the stop condition.

### Dependency Analysis

- No forward epic dependency found. Epic N does not require Epic N+1 to deliver its core value.
- Epic 5 deliberately consumes local evidence from Stories 1.5, 1.6, 1.11, 2.4, and 4.5; this is a valid backward dependency for release-gate aggregation.
- Pre-kickoff gates are modeled as stop conditions, not hidden forward dependencies. The gate tracker exists and is decided.
- No database/entity creation timing violation found. The architecture and stories preserve EventStore authority and derived projections rather than creating all tables up front.

### Best Practices Compliance Checklist

| Check | Result |
| ----- | ------ |
| Epics deliver user or stakeholder value | Pass |
| Epics can function independently in sequence | Pass |
| Stories are appropriately sized | Pass with required split/remediation for Stories 3.8 and 6.8 |
| No forward dependencies | Pass |
| Database/state created only when needed | Pass |
| Clear acceptance criteria | Pass |
| Traceability to FRs maintained | Pass |

### Quality Assessment Summary

The epic set is implementation-ready in structure, with two assignment-control defects to resolve before sprint execution. The backlog has already anticipated both defects through explicit assignment rules and readiness-gate decisions, but the actual split or named-owner decision must be carried into generated story files before developers receive those stories.

## Summary and Recommendations

### Overall Readiness Status

**NEEDS WORK**

The planning artifacts are close to implementation-ready, but not cleanly ready for story assignment until the two bundled validation stories are split or assigned under explicit checklist-mode ownership.

### Critical Issues Requiring Immediate Action

No critical blockers were found.

### Major Issues Requiring Resolution Before Story Assignment

1. Story 3.8 must be split into independently closable verification stories or assigned to a named evidence owner with an approved checklist-mode evidence plan.
2. Story 6.8 must be split into telemetry redaction and telemetry cardinality validation stories or assigned to a named SRE/test owner with an approved combined evidence plan.

### Minor Issues and Watch Items

1. Story 1.1 is technical by nature but acceptable because it satisfies the architecture starter/scaffold requirement. Keep its scope strictly limited to scaffolding and readiness links.
2. Story generation must copy applicable readiness-gate links into story files so implementation agents do not miss stop conditions.
3. UX-DR51 names `Leak Sentinel` concretely while architecture states the equivalent disclosure-surface testing need more generally. Preserve the named helper or explicitly document the equivalent mechanism before Story 3.8 closes.

### Recommended Next Steps

1. Generate split stories for 3.8A, 3.8B, 3.8C, 6.8A, and 6.8B, or record named checklist-mode owners with fixture sets, evidence outputs, pass/fail gates, and review dates.
2. When creating implementation story files, embed the relevant rows from [readiness-gates.md](</D:/Hexalith.Conversations/_bmad-output/implementation-artifacts/readiness-gates.md>) into each story's Ready for Dev section.
3. Preserve the two-level evidence model: implementation stories close on local evidence, while Epic 5 owns release-gate aggregation, signed artifacts, manifest rows, and waiver governance.
4. Keep the first implementation slice focused on scaffold plus the foundation path: tenant-scoped conversation aggregate, idempotent create/append, fail-closed tenant access, projection freshness, Party hydration boundary, and conformance placeholders.
5. Treat UX safety and disclosure testing as release-gating work, especially responsive duplicates, accessibility tree leakage, clipboard output, telemetry, screenshots, browser titles, and hidden DOM.

### Final Note

This assessment identified 4 issues across 3 categories: 2 major assignment-control issues, 1 minor scaffold-scope watch item, 1 readiness-gate propagation watch item, and 1 UX/architecture terminology watch item. The artifacts are strong and internally consistent, but the assignment-control fixes should be completed before implementation stories are handed to developers.

**Assessment Date:** 2026-05-17
**Assessor:** Codex using `bmad-check-implementation-readiness`
