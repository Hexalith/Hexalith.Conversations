---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/prd.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/architecture.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/ux-design-specification.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/ux-requirement-map.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-15.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-15-readiness-follow-up.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16-readiness-gates-and-story-controls.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-17.md"
  - "D:/Hexalith.Conversations/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-17-readiness-assessment-follow-up.md"
---

# Hexalith.Conversations - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Conversations, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

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

### NonFunctional Requirements

NFR1: Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
NFR2: Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: pass, fail, waived, or unknown-accepted.
NFR3: Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
NFR4: GA implementation cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with named owner and review date.
NFR5: Numeric targets must be classified as Release blocker, Validation target, or Capacity discovery target before implementation kickoff.
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
NFR45: Projection freshness metadata must use a standard shape such as lastAppliedEventPosition, lastAppliedEventTimestamp, projectionGeneratedAt, isStale, and lagDuration, or document why an equivalent shape is not available.
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
NFR59: governance verify / conformance verification output must be machine-readable and suitable for CI and incident workflows.
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

### Additional Requirements

- Use the Composite Hexalith .NET/Aspire scaffold as the selected starter; the first implementation story should initialize the standard Conversations project shape.
- Scaffold `Contracts`, `Client`, domain module, `Server`, `Testing`, `AppHost`, `ServiceDefaults`, and focused test projects using .NET 10 and `.slnx`.
- Keep package versions in Central Package Management and align Aspire/Dapr/package decisions with sibling Hexalith modules unless an ADR records divergence.
- Use Hexalith.EventStore as the only durable write-side source of truth for v1 conversation state; do not create transcript tables or authoritative projections.
- Model `ConversationAggregate : EventStoreAggregate<ConversationState>` as the first aggregate boundary for v1 vertical slices.
- Keep projections, caches, exports, UI state, evidence bundles, and future Memories/RAG indexes derived and non-authoritative.
- Store stable upstream identifiers in events, including TenantId, ConversationId, PartyId, ProjectId, FolderId, and FileId; do not store Party personal data or provider-owned session authority.
- Validate command shape, schema version, idempotency key, tenant binding, and stable identifiers at boundaries before aggregate invocation.
- Enforce tenant authorization from a local Tenants projection before aggregate load, command dispatch, projection read, admin action, MCP/tool operation, export, verification detail access, or background work.
- Treat missing, stale, ambiguous, disabled, lagging, rolled-back, deleted, or unavailable tenant state as fail-closed.
- Wrap Parties access behind a Conversations-owned boundary; command-time participant validation fails closed, while read-time hydration may degrade only according to policy.
- Expose Conversations domain contracts and typed errors through REST and the .NET client; do not expose EventStore envelopes, snapshot mechanics, stream internals, SignalR groups, or raw projection internals as adopter APIs.
- Use EventStore/Dapr publication for domain events; pub/sub handlers must tolerate duplicates, replay, and out-of-order delivery.
- Use OpenAPI and README/API guidance for adopter workflows, with executable contract tests for commands, projections, events, errors, and version discovery.
- Use FrontComposer as the initial UI delivery mechanism for generated baseline admin surfaces, with custom trust components for evidence timeline, trust posture, redaction, audit trail, citation copy, temporal navigation, projection freshness, and degraded states.
- Use Aspire AppHost and ServiceDefaults for local orchestration, observability, and service composition; deployment target decisions remain ADR-gated.
- Keep observability content-safe and bounded-cardinality; do not emit raw conversation content, provider payloads, redacted text, or unbounded identifiers.
- Implement deterministic degradation behavior for tenant projection outages, Parties adapter outages, audit sink outages, append conflicts, pub/sub duplicate or replay, projection rebuild, and redaction policy changes.
- Create and satisfy the ADR backlog for EventStore authority, idempotency, tenant projection freshness, audit pairing, schema evolution, projection freshness, redaction replay, Parties hydration, FrontComposer trust boundaries, and retention/deletion/legal hold.
- Include a minimum v1 conformance pack covering aggregate invariants, tenant isolation, idempotency, audit pairing, projection freshness, replay determinism, derived-state quarantine, Party hydration, redaction non-disclosure, schema compatibility, adopter contract behavior, boundary contracts, and degradation scenarios.
- Produce machine-readable architecture/release evidence including test results, conformance versions, ADR coverage, schema versions, replay checksum, projection rebuild proof, tenant denial matrix, redaction verification, and known degradation modes.
- Make the first implementation slice prove buyer trust by persisting one chatbot exchange, enforcing tenant access, projecting tenant-safe state with freshness metadata, hydrating Parties at read time, replaying from EventStore, demonstrating append-only audit/redaction, returning typed results/errors, proving idempotency, adding at least one negative tenant-isolation test, and producing release-evidence placeholder or manifest entry.
- Every implementation story that introduces durable state, cache, export, memory write, cross-boundary contract, or privileged execution path must name its owning decision, failure semantics, and conformance evidence.
- Apply the approved sprint-change proposal corrections before implementation kickoff: Story 1.1 is scaffold support only, Story 1.4 is split into participants/messages/references, Story 2.4 is split into redaction command/projection/client-safety/operations-safety slices, and conformance proof obligations are independently closable.
- Enforce two-level evidence rules: CORE implementation stories close on minimum local evidence in the same story or epic, while Epic 5 owns release-gate aggregation, signed evidence, manifest rows, and waiver governance.
- Treat Story 3.8 and Story 6.8 as verification/support bundles unless they are split before assignment; do not assign them as ordinary single-owner implementation stories when the validation domains require separate ownership.
- Finalize shared trust/freshness vocabulary and UX safety-gate ownership before dependent API, Admin UI, client, diagnostics, telemetry, or conformance stories proceed.

### UX Design Requirements

UX-DR1: Use FrontComposer and Fluent UI Blazor as the baseline design system for shell, navigation, routes, generated projection views, command forms, inputs, validation, buttons, menus, tabs, dialogs, drawers, badges, lists, grids, filters, loading states, empty states, focus behavior, theme tokens, typography, density, and accessibility foundations.
UX-DR2: Add custom Conversations UI only where users must interpret evidence, trust, governance, redaction, freshness, citation, participant identity, or action safety.
UX-DR3: Render trust states, warnings, action enablement, freshness indicators, citation confidence, redaction status, and audit affordances only from Conversations-owned projections or command availability metadata.
UX-DR4: Prevent the admin UI from browsing raw EventStore streams as the primary experience; expose governed records, evidence timelines, decisions, and audit trails through Conversations projections.
UX-DR5: Implement reusable design tokens and visual treatments for current, stale, rebuilding, unavailable, denied, degraded, redacted, incomplete, audit-ready, and action-required states.
UX-DR6: Implement reusable redaction notices and visibility explanations that never expose original values through visible text, hidden DOM, tooltips, ARIA, clipboard, telemetry, browser title, or responsive duplicates.
UX-DR7: Implement audit markers, evidence anchors, and chain-of-custody cues that are copyable, stable, traceable, and accessible.
UX-DR8: Implement participant identity resolution and degraded hydration states without persisting or exposing unauthorized Parties personal data.
UX-DR9: Implement command availability, required permission, precondition, risk-level, and blocked-reason displays from server metadata, with unsafe actions disabled when metadata is missing, stale, ambiguous, malformed, unauthorized, or partially loaded.
UX-DR10: Implement citation and temporal reconstruction affordances using stable evidence metadata and safe copy behavior.
UX-DR11: Implement generated-first surfaces for conversation search and filtering, conversation summary list, tenant-scoped admin navigation, standard details panels, standard forms and command dialogs, pagination, sorting, empty states, and loading states.
UX-DR12: Implement custom trust-critical surfaces for Evidence Timeline, Citation Rendering, Redaction State and Redaction Previews, Audit Trail, Freshness and Projection-Lag Indicators, Gated Action Controls, Temporal Cursor Navigation, Degraded or Unresolved Participant Identity, and Authorization/Tenant-Boundary Warnings.
UX-DR13: Require custom trust-critical components to declare projection inputs, command metadata inputs, fail-closed behavior, accessibility behavior, degraded-state behavior, and tenant-isolation test coverage.
UX-DR14: Provide component tests proving gated actions disable when command availability is absent or projection freshness is stale.
UX-DR15: Provide component tests proving evidence timelines render only projection-provided events and citation components render missing or deleted evidence as degraded rather than trusted.
UX-DR16: Provide accessibility tests proving evidence timelines are keyboard navigable, audit entries expose timestamp, actor, action, and outcome to screen readers, and disabled gated actions expose reasons without hover.
UX-DR17: Implement trust primitives for Trust Fact, SafeReasonInline, SafeReasonDetail, Redaction Placeholder, Freshness Marker, Command Availability Marker, Citation Control, and Participant Identity Marker.
UX-DR18: Implement composite investigation components for Tenant-scoped Find Pane, Trust Preview Result Row, Governed Record Header, Trust Posture Strip, Evidence Completeness Indicator, Evidence Timeline Entry, Safe State Message, Evidence Detail Drawer, Command Gate, Permission-gated Forensic Timeline Mode, Evidence Acceptance Summary, and Waiver and Blocker Summary.
UX-DR19: Use a shared EvidenceTrustModel or equivalent contract for trust-bearing components covering tenant scope, permission state, redaction state, freshness state, citation state, participant resolution state, command eligibility, audit reference, confidence, and completeness.
UX-DR20: Ensure diagnostic and detail drawers perform independent authorization before rendering detail and close on permission downgrade.
UX-DR21: Ensure search result counts, facets, autocomplete, pagination, ordering, empty states, and response timing are permission-safe and do not reveal inaccessible records.
UX-DR22: Ensure trust-bearing components include explicit projection version, timestamp, or freshness source and do not cache stale trust labels after projection refresh.
UX-DR23: Implement telemetry for command blocked, citation opened, redaction displayed, waiver viewed, freshness warning shown, evidence accepted, forensic mode entered, and authorization denied, without protected content or unbounded sensitive identifiers.
UX-DR24: Implement Trust Primitives and Contracts before higher-order investigation components, including primitive snapshot tests, interaction tests, permission matrix tests, redaction leakage tests, stale projection command-blocking tests, responsive priority tests, and audit/telemetry event tests.
UX-DR25: Implement core investigation components for Find -> Open -> Verify -> Cite, Act, or Stop, with keyboard and screen-reader flows that expose tenant scope, trust posture, evidence completeness, and blocked-action reasons before timeline reliance.
UX-DR26: Implement evidence detail and governance components for citation, audit linkage, participant resolution, projection freshness, why-this-result, why-am-I-seeing-this, and full command-gate rationale.
UX-DR27: Implement review and enhancement components for forensic timeline mode, evidence acceptance summary, waiver and blocker summary, responsive find drawer, evidence acceptance, and waiver review flows.
UX-DR28: Define canonical trust-state fixtures for fully trusted record, redacted record, stale projection, missing citation, unresolved participant, blocked command, waived blocker, cross-tenant attempt, permission downgrade during active session, and partial evidence timeline.
UX-DR29: Implement deterministic trust precedence so blocked wins over available, stale wins over current, incomplete wins over complete, redacted wins over visible, and unknown wins over assumed.
UX-DR30: Implement safe empty, loading, denied, unavailable, stale, redacted, degraded, and no-access states that do not reveal protected existence, hidden participant data, inaccessible records, raw policy internals, or redacted content.
UX-DR31: Implement tenant-scoped, permission-filtered, trust-previewed search using business-safe filters such as date range, project or folder reference, participant reference, lifecycle state, redaction state, freshness state, audit readiness, and verification state.
UX-DR32: Implement a standard trust summary band before the timeline at every breakpoint, showing tenant scope, record identity, freshness, completeness, citation status, participant resolution, and command eligibility.
UX-DR33: Use evidence detail drawers for citation, audit linkage, participant resolution, projection freshness, why-this-result, and command reasoning; reserve dialogs for governance-changing confirmations with rationale and audit implications.
UX-DR34: Build governance forms that collect only operator intent; tenant identity, user identity, claims, tokens, and host authorization context must never be user-editable fields.
UX-DR35: Implement copy and export safety so copied citations, summaries, rows, timeline entries, or evidence details are built from permission-safe DTOs after authorization recheck, not from rendered text selection or full component models.
UX-DR36: Implement trust transitions for permission downgrade, metadata expiry, command availability change, and projection freshness change by closing gated details, clearing protected content, and preserving only safe operator-entered intent.
UX-DR37: Implement the safety acceptance criteria AC-SAFE-001 through AC-SAFE-008 covering fail-closed unauthorized conversation IDs, redaction absence from all UI surfaces, independent drawer authorization, command rechecks, projection-owned trust posture, deterministic trust precedence, leak-safe search, and distinct loading/empty/error/denied/redacted/stale snapshots.
UX-DR38: Implement four UX quality gates: leakage, tenant isolation, trust provenance, and command safety.
UX-DR39: Use a desktop-first responsive strategy for operator/admin governance workflows, with desktop supporting investigation and approval, tablet supporting constrained review, and mobile supporting safe read-only triage by default.
UX-DR40: Treat responsive layouts as independent disclosure surfaces; desktop tables, tablet split views, mobile cards, sticky headers, drawers, skeletons, and duplicated markup must pass authorization, redaction, clipboard, telemetry, and accessibility rules.
UX-DR41: Preserve tenant scope, record identity, trust posture, evidence completeness, and command eligibility before timeline reliance at every breakpoint.
UX-DR42: Default mobile governance-changing actions to blocked unless explicitly designed, authorized, confirmed, and tested for narrow screens.
UX-DR43: Use standard breakpoints of mobile 320-767px, tablet 768-1023px, desktop 1024px+, and wide desktop 1440px+, unless evidence shows a Conversations-specific need.
UX-DR44: Meet WCAG 2.1 AA for operator/admin web surfaces, including keyboard navigation, focus order, contrast, screen-reader-readable audit/redaction state, reduced-motion, high-contrast, and browser zoom behavior.
UX-DR45: Ensure assistive technology output obeys the same tenant, permission, and redaction rules as visible content, including accessible names, descriptions, live regions, headings, table summaries, browser titles, copied text, and focus order.
UX-DR46: Use consistent accessibility microcopy for Redacted, Unavailable, Restricted, Still loading, and Some events unavailable, without sensitive values in tooltips, ARIA labels, empty states, validation errors, live regions, or toast text.
UX-DR47: Ensure trust metadata loads before or with trust-bearing content; skeletons and placeholders must be generic, size-stable, and must not reveal protected length, counts, density, availability, timing, or ordering.
UX-DR48: Ensure virtualized timelines preserve chronological order, keyboard navigation, focus restoration, screen-reader position context, and redaction semantics without leaving protected content in hidden DOM.
UX-DR49: Implement mobile triage and handoff links using only permission-safe identifiers and temporal cursors, without protected titles, participant names, snippets, or redacted content in URLs.
UX-DR50: Run responsive and accessibility tests across desktop, tablet, mobile, and wide desktop for fully trusted, redacted, stale, missing citation, unresolved participant, blocked command, cross-tenant, permission downgrade, partial timeline, no accessible matches, unauthorized-existing, and nonexistent states.
UX-DR51: Implement Leak Sentinel checks across DOM text, attributes, ARIA properties, page title, clipboard output, telemetry envelopes, screenshots, and accessibility snapshots for desktop and mobile layouts.
UX-DR52: Use canonical responsive/accessibility fixtures including TenantA_Admin_FullTrust, TenantA_Reviewer_RedactedParticipants, TenantA_MobileTriage_ReadOnly, TenantB_NoAccess_CrossTenantPoison, MixedTimeline_PartialLoad_RedactedEvents, VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows, and AssistiveTech_RedactionAnnouncement.

### FR Coverage Map

FR1: Epic 1 - Tenant-safe conversation record creation.
FR2: Epic 1 - Stable tenant-scoped conversation identity.
FR3: Epic 1 - Conversation lifecycle state and transitions.
FR4: Epic 1 - Ordered message append.
FR5: Epic 1 - Participant addition for humans, AI agents, and LLMs.
FR6: Epic 1 - Idempotent command submission.
FR7: Epic 1 - Typed command rejection semantics.
FR8: Epic 1 - Conversation retrieval with timeline, participants, governance state, and freshness.
FR9: Epic 1 - Tenant-scoped conversation listing by business context.
FR10: Epic 1 - Release-scoped title or metadata updates.
FR11: Epic 1 - Release-scoped close or archive behavior.
FR12: Epic 1 - Conversation continuity across provider expiry, restart, or failover.
FR13: Epic 1 - Stable Party attribution for actions.
FR14: Epic 1 - Human, AI agent, and LLM participant modeling.
FR15: Epic 1 - Provider correlation identifiers as metadata.
FR16: Epic 1 - Versioned provider-specific extension data.
FR17: Epic 1 - Multi-provider attribution.
FR18: Epic 1 - Reconstruction of actor, action, time, and tenant context.
FR19: Epic 1 - File references without binary storage.
FR20: Epic 1 - Upstream business entity association.
FR21: Epic 1 - External business identifiers for tenant-scoped discovery.
FR22: Epic 1 - Distinction between external identifiers and business references.
FR23: Epic 1 - Read-time upstream reference resolution.
FR24: Epic 1 - Readability when upstream entities change lifecycle state.
FR25: Epic 1 - Migration-boundary guidance for out-of-coverage records.
FR26: Epic 1 - Tenant context for commands, events, projections, queries, pub/sub, and audit records.
FR27: Epic 1 - Fail-closed tenant binding before aggregate or projection access.
FR28: Epic 1 - Cross-tenant enumeration prevention.
FR29: Epic 1 - Indistinguishable unauthorized, nonexistent, and cross-tenant records.
FR30: Epic 1 - Typed tenant-isolation and tenant-binding errors.
FR31: Epic 1 - Tenant audit attribution for operator actions affecting tenant data.
FR32: Epic 1 - Tenant-aware publication without cross-tenant metadata leakage.
FR33: Epic 1 - Projection derivation from ordered conversation events.
FR34: Epic 1 - Read-model metadata for replay position, projection version, or freshness.
FR35: Epic 1 - v1 projection rebuild equivalence.
FR36: Epic 1 - Projection consistency and freshness semantics.
FR37: Epic 1 - Projection lag or freshness behavior exposure.
FR38: Epic 1 - Downstream domain event consumption.
FR39: Epic 1 - Published event schema and version metadata.
FR40: Epic 1 - Unsupported schema version rejection.
FR41: Epic 1 - Compatible event, command, and projection evolution rules.
FR42: Epic 2 - Retention policy setting or replacement with rationale.
FR43: Epic 2 - Sensitive content marking.
FR44: Epic 2 - Redaction with actor, timestamp, rationale, and policy attribution.
FR45: Epic 2 - Distinction among archival, retention, redaction, legal hold, and audit history.
FR46: Epic 2 - Audit stream preservation while redacting projections or display.
FR47: Epic 2 - Paired audit event for each governance mutation.
FR48: Epic 2 - Governance rejection when audit recording is unavailable.
FR49: Epic 2 - Non-governance activity behavior during audit degradation.
FR50: Epic 2 - Point-in-time message and governance reconstruction.
FR51: Epic 2 - Citeable audit records.
FR52: Epic 2 - Retention and redaction treatment for governance audit records.
FR53: Epic 2 - Allowed and denied audit-record actions.
FR54: Epic 2 - Structured justification for privileged tenant-data operations.
FR55: Epic 2 - Coherent review of privileged-action justification and audit outcome.
FR56: Epic 3 - Tenant-scoped search by external identifiers.
FR57: Epic 3 - Search filtering by date range and business context.
FR58: Epic 3 - Reconstructed transcript review with governance and freshness context.
FR59: Epic 3 - Inline redaction attribution.
FR60: Epic 3 - Inline governance audit trail.
FR61: Epic 3 - Historical conversation state review.
FR62: Epic 3 - Citation-ready transcript and audit references.
FR63: Epic 3 - Stable temporal evidence links.
FR64: Epic 3 - Read-only operator and compliance workflows.
FR65: Epic 3 - Classification and separate audit for privileged operator mutations.
FR66: Epic 3 - Governance verification execution.
FR67: Epic 3 - Structured verification results.
FR68: Epic 3 - Distinction between governance verification and infrastructure failures.
FR69: Epic 3 - Self-serve buyer acceptance demo.
FR70: Epic 4 - Published contract package for commands, projections, events, and typed errors.
FR71: Epic 4 - Supported .NET client integration path.
FR72: Epic 4 - Minimal create, append, and read happy path.
FR73: Epic 4 - Adopter-facing conformance tests.
FR74: Epic 4 - Documented tenant binding, Party identity, idempotency, errors, freshness, publication, and governance behavior.
FR75: Epic 4 - Active contract version and compatibility discovery.
FR76: Epic 4 - Caller-supplied metadata for attribution, audit, projections, and composition.
FR77: Epic 4 - Onboarding diagnostics for missing CORE preconditions and configuration gaps.
FR78: Epic 4 - Remediation guidance with machine-readable error codes.
FR79: Epic 4 - Adopter-facing CORE preconditions.
FR80: Epic 4 - Sanitized typed error responses with safe audit handle and documentation pointer.
FR81: Epic 5 - Compatibility policy.
FR82: Epic 5 - Signed conformance artifact.
FR83: Epic 5 - Versioned release-specific conformance manifest.
FR84: Epic 5 - Test-to-requirement traceability.
FR85: Epic 5 - Named-waiver process.
FR86: Epic 5 - Blocking and non-blocking release-gate failure classification.
FR87: Epic 5 - Adversarial tenant-isolation verification.
FR88: Epic 5 - Idempotent command verification.
FR89: Epic 5 - Redaction-replay correctness verification.
FR90: Epic 5 - Provider portability proof.
FR91: Epic 5 - Event schema evolution proof.
FR92: Epic 5 - Executable contract tests before v1 release.
FR93: Epic 5 - Adopter-style CORE fixture.
FR94: Epic 5 - Module-level versus platform compliance evidence.
FR95: Epic 6 - Content-safe command rejection observability.
FR96: Epic 6 - Content-safe projection lag, rebuild, and availability observability.
FR97: Epic 6 - Content-safe publication failure and contract issue observability.
FR98: Epic 6 - Content-safe tenant isolation denial and privileged access observability.
FR99: Epic 6 - Conformance outcome and verification status observability.
FR100: Epic 6 - Release capability scope classification.
FR101: Epic 6 - Release-scope consequence exposure.
FR102: Epic 6 - Buyer partial acceptance support.
FR103: Epic 6 - Second-adopter status and downgrade-rule review milestones.
FR104: Epic 6 - Responsibility boundary documentation.

## Epic List

### Epic 1: Tenant-Safe Conversation Record
Adopter teams can create, append to, retrieve, list, and replay tenant-scoped conversation records with stable identity, participant attribution, business references, idempotent command behavior, projection freshness, event publication, and version-safe contracts.
**FRs covered:** FR1-FR41

### Epic 2: Governed Retention, Redaction, and Audit
Authorized users can apply retention, sensitivity, redaction, archival, and privileged-action governance with paired audit evidence and fail-closed audit behavior.
**FRs covered:** FR42-FR55

### Epic 3: Compliance Investigation Workspace
Compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios.
**FRs covered:** FR56-FR69

### Epic 4: Adopter Integration and Developer Readiness
Developer adopters can integrate through published contracts, a .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, and CORE precondition documentation.
**FRs covered:** FR70-FR80

### Epic 5: Conformance, Compatibility, and Release Evidence
Platform owners can publish compatibility policy, run release-gating conformance, manage waivers, trace tests to requirements, prove portability/schema evolution, and distinguish module evidence from platform evidence.
**FRs covered:** FR81-FR94

### Epic 6: Operations, Observability, and Lifecycle Commitments
Operators and product owners can observe tenant-safe operational health, conformance outcomes, privileged access attempts, and release-scope/lifecycle commitments without leaking protected conversation data.
**FRs covered:** FR95-FR104

## Implementation Readiness Gates

### Pre-Kickoff Decisions Required

Dependent implementation stories must not begin until each applicable decision has a row in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`. A waived gate must name owner, approver, expiry, compensating control, buyer impact, and review date.

- EventStore envelope stability and evolution ownership.
- .NET client versus raw HTTP fallback policy.
- Whether any module consumes Conversations events in v1.
- Whether `MarkSensitiveData` and `RedactMessageContent` are CORE.
- Two-level evidence semantics are approved and enforced: implementation stories close on minimum local evidence, while Epic 5 owns release-gate aggregation, signed artifacts, manifest rows, and waiver governance.
- Architect and second-engineer availability for trust/freshness and governance decisions.
- Named second-adopter candidate or review milestone.

### Two-Level Evidence Rules

Implementation stories close on minimum local evidence in the same story or same epic. Release-gate evidence closes later through Epic 5 release packaging, signed conformance artifacts, manifest rows, and waiver governance.

Minimum local evidence for story closure:

- Story 1.5 closes when tenant access and fail-closed behavior pass local automated tests for the scenarios named in the story acceptance criteria.
- Story 1.6 closes when idempotent command behavior passes local automated tests for duplicate, reordered, conflicting, unknown-outcome, and tenant-mismatched submissions.
- Story 1.11 closes when replay, projection rebuild, unsupported-version handling, and at least one schema-version compatibility path pass local automated tests or ADR-approved fixtures.
- Story 2.4 and Stories 2.4.1-2.4.3 close when the relevant redaction command, projection, client-surface, and operational-surface evidence passes in their own story scope.
- Story 2.5 closes when governance mutations fail closed on audit-write unavailability and paired audit evidence tests pass in the story scope.
- Story 4.5 closes when the adopter-facing conformance package and CORE fixture run locally or in CI and produce machine-readable safe results.

Release manifest carry-forward:

- Story 5.5 consumes Story 1.5 local evidence and adds release-gating tenant isolation manifest coverage.
- Story 5.6 consumes Story 1.6 local evidence and adds release-gating idempotency manifest coverage.
- Story 5.7 consumes Story 2.4 local evidence and adds release-gating redaction replay manifest coverage.
- Story 5.9 consumes Story 1.11 local evidence and adds release-gating event schema evolution manifest coverage.
- Story 5.10 consumes Story 4.5 local evidence and adds release-gating contract validation and CORE fixture manifest coverage.

Waivers apply to release-gate evidence, not ordinary story closure, unless the affected story explicitly cannot meet its own minimum local evidence. Any waiver must name owner, approver, expiry, compensating control, buyer impact, and review date.

### ADR-Gated Story Stop Conditions

Dependent stories must stop before implementation when these decisions are missing:

- Temporal evidence anchor: blocks Story 3.4 and point-in-time evidence link behavior.
- Command availability metadata: blocks UI command gates and any client-side command eligibility rendering.
- Projection freshness blocking semantics: blocks Stories 1.7, 1.8, 3.1, 3.2, 4.2, 4.4, and 6.2.
- EventStore envelope ownership and evolution: blocks Story 1.11 and Story 5.9.
- Raw HTTP fallback approval: blocks raw HTTP fallback scope in Story 4.2.
- Numeric capacity/performance thresholds or buyer-accepted unknowns: blocks GA release-gate closure and performance evidence stories.

### Shared Trust/Freshness Vocabulary Gate

Trust-bearing API responses, admin UI, .NET client errors, diagnostics, and conformance output must use one approved trust/freshness vocabulary and metadata shape before implementation diverges across surfaces.

First affected stories: 1.7, 1.8, 3.1, 3.2, 3.4, 4.2, 4.4, and 6.2.

### UX Safety Gate Ownership

The backlog must assign implementation and verification ownership for Leak Sentinel, accessibility-tree leakage checks, clipboard safety checks, responsive duplicate checks, command reauthorization fixtures, permission-safe DTO tests, and FrontComposer generated-versus-custom component boundary checks.

## Epic 1: Tenant-Safe Conversation Record

Adopter teams can create, append to, retrieve, list, and replay tenant-scoped conversation records with stable identity, participant attribution, business references, idempotent command behavior, projection freshness, event publication, and version-safe contracts.

### Story 1.1: Set Up Initial Project from Starter Template

As an adopter developer,
I want a buildable Hexalith.Conversations module scaffold from the selected starter template,
So that future conversation features can be implemented inside the approved Hexalith architecture without reworking project boundaries.

**Requirements Covered:** Architecture starter-template requirement; supports FR1-FR41 foundation only. Behavioral FR1-FR41 implementation coverage is delivered by Stories 1.2-1.11.

**Scope Control:** Story 1.1 may create buildable projects, smoke tests, ADR folders/templates, and readiness tracker links only. It must not decide ADRs, implement conversation persistence, tenant authorization, provider integration, FrontComposer runtime behavior, projections, workers, governance commands, or partial domain behavior.

**Acceptance Criteria:**

**Given** the Hexalith.Conversations repository
**When** the scaffold is created
**Then** the solution contains the approved `.NET 10` project structure for `Contracts`, `Client`, domain module, `Server`, `Testing`, `AppHost`, `ServiceDefaults`, and focused test projects
**And** project files use central package management without inline package versions.

**Given** the scaffold exists
**When** dependency references are added
**Then** dependencies follow the approved boundary direction: contracts remain infrastructure-free, server/application code can use EventStore integration points, and client-facing contracts do not expose EventStore internals
**And** no sibling module source is copied into Conversations.

**Given** the first scaffold validation runs
**When** restore/build/test smoke checks execute
**Then** the scaffold builds without requiring Aspire runtime, Dapr sidecars, tenant seed data, production secrets, provider credentials, or nested submodule initialization
**And** root-level submodule policy is documented or preserved.

**Given** future stories will implement domain behavior
**When** placeholder files or test fixtures are added
**Then** they remain non-operative and fail closed at runtime
**And** they do not smuggle partial conversation persistence, tenant authorization, provider, UI, or worker behavior ahead of later stories.

**Given** pre-kickoff ADRs are required before dependent implementation
**When** the scaffold documentation is created
**Then** the repository contains the approved ADR folder, ADR template, and decision tracker links for idempotency, tenant projection freshness, audit pairing, schema evolution, redaction replay, Party hydration, FrontComposer trust boundaries, and retention/deletion lifecycle
**And** dependent stories can link to recorded or explicitly waived decisions before implementation starts.

### Story 1.2: Define Conversation Identity, Command, Event, and Error Contracts

As an adopter developer,
I want clear Conversations contracts for identity, commands, events, projections, and typed errors,
So that I can integrate without learning EventStore internals or depending on unstable implementation details.

**Requirements Covered:** FR2, FR6, FR7, FR13-FR22, FR26, FR30, FR39-FR41.

**Acceptance Criteria:**

**Given** the Contracts project exists
**When** conversation identity contracts are defined
**Then** contracts include tenant-scoped `ConversationId` concepts distinct from provider identifiers, UI labels, external business identifiers, and thread names
**And** stable reference concepts are available for `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, and provider correlation metadata.

**Given** adopter systems need to create and evolve conversations
**When** initial command contracts are defined
**Then** the contract package includes create-conversation, append-message, add-participant, attach-file-reference, update-metadata, and close/archive command shapes where release-scoped
**And** each command includes schema version, tenant binding, correlation/causation metadata, and idempotency support where applicable.

**Given** Conversations persists meaningful domain changes
**When** initial event contracts are defined
**Then** events use Conversations language, carry schema/version metadata, and store stable IDs rather than Party personal data, provider session authority, raw upstream records, or file binaries
**And** provider-specific payload metadata is represented only as opaque, tenant-isolated, explicitly versioned extension data.

**Given** adopter systems must handle failures consistently
**When** typed error contracts are defined
**Then** invalid, unauthorized, conflicting, duplicate, unsupported-version, tenant-mismatched, stale-projection, and hidden-by-tenant-isolation outcomes have documented machine-readable failure semantics
**And** error shapes are content-safe and do not reveal target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.

**Given** contracts are public integration surface
**When** contract tests and documentation checks run
**Then** contract types are serialization-friendly, nullable-clean, centrally packaged, and infrastructure-free
**And** no public contract exposes raw EventStore envelopes, snapshot mechanics, stream internals, SignalR group names, or projection implementation details.

### Story 1.3: Create Tenant-Safe Conversation Aggregate

As an adopter system,
I want to create a tenant-scoped conversation through the Conversations domain model,
So that every conversation begins as a replayable, authorized, EventStore-backed business record.

**Requirements Covered:** FR1-FR3, FR6, FR7, FR12, FR15, FR16, FR20-FR22.

**Acceptance Criteria:**

**Given** a valid create-conversation command with tenant context, actor Party ID, schema version, idempotency metadata, and optional business references
**When** the application handler dispatches the command
**Then** `ConversationAggregate` emits a versioned conversation-created event using Conversations language
**And** the event stores stable identifiers and metadata only, not Party personal data, provider session authority, raw upstream records, or file binaries.

**Given** a conversation-created event exists
**When** aggregate state is rehydrated from the event stream
**Then** the resulting `ConversationState` contains the tenant-scoped conversation identity, lifecycle state, creator attribution, business references, provider correlation metadata where supplied, and creation timestamp
**And** the result is deterministic for the same ordered event history.

**Given** a create-conversation command is invalid, malformed, unsupported-version, or missing required stable identifiers
**When** the command is handled
**Then** the aggregate or boundary validator returns a typed rejection outcome
**And** no successful conversation-created event is emitted.

**Given** the command references provider identifiers or external business identifiers
**When** the conversation identity is assigned
**Then** the internal `ConversationId` remains distinct from provider IDs, external identifiers, labels, and thread names
**And** provider/external IDs are stored only as correlation or business-reference metadata.

**Given** aggregate unit tests run
**When** valid and invalid create-conversation scenarios are executed
**Then** tests prove emitted event shape, replayed state, rejection behavior, schema version handling, and absence of forbidden Party/provider/file payload data
**And** tests do not require Dapr, Aspire, tenant seed data, provider credentials, or initialized nested submodules.

### Story 1.4: Add Conversation Participants with Stable Party Attribution

As an adopter system,
I want to add human users, AI agents, and LLMs as conversation participants,
So that participant membership is attributable through stable Party identities without storing Party personal data.

**Requirements Covered:** FR5, FR13-FR18.

**Acceptance Criteria:**

**Given** an existing active conversation and a valid add-participant command
**When** the command is handled
**Then** the aggregate emits a participant-added event for a stable Party ID and participant role/type such as human, AI agent, or LLM
**And** the event does not store mutable Party personal data, contact values, names, or upstream person/organization details.

**Given** a participant command targets a closed, archived, unsupported, malformed, missing, or incompatible conversation state
**When** the command is handled
**Then** the system returns a typed documented rejection outcome
**And** no successful participant-added event is emitted.

**Given** aggregate tests replay participant events
**When** the conversation state is rehydrated
**Then** the state reconstructs participant membership and attribution metadata deterministically
**And** tests prove human, AI agent, and LLM participants can be represented without treating provider IDs as source-of-truth identity.

### Story 1.4.1: Append Ordered Messages with Author Attribution

As an adopter system,
I want to append ordered messages to an existing active conversation,
So that the conversation record preserves who contributed what, when, and under which tenant context.

**Requirements Covered:** FR4, FR6, FR7, FR13-FR18.

**Acceptance Criteria:**

**Given** an existing active conversation with participants
**When** a valid append-message command is handled
**Then** the aggregate emits an ordered message-appended event with stable author attribution, tenant scope, timestamp, message identity, schema version, and allowed metadata
**And** provider correlation identifiers are preserved only as metadata, not as durable conversation identity.

**Given** a message command targets a closed, archived, unsupported, malformed, missing, or incompatible conversation state
**When** the command is handled
**Then** the system returns a typed documented rejection outcome
**And** no successful message-appended event is emitted.

**Given** aggregate tests replay message events
**When** the conversation state is rehydrated
**Then** the state reconstructs ordered message timeline metadata, author attribution, and provider correlation metadata deterministically
**And** tests prove multi-provider attribution can be preserved without treating provider IDs as source-of-truth identity.

### Story 1.4.2: Attach File and Upstream Business References

As an adopter system,
I want to associate messages and conversations with file, project, folder, provider, and external business references,
So that downstream discovery and governance can use stable references without storing upstream records or file binaries.

**Requirements Covered:** FR15-FR22.

**Acceptance Criteria:**

**Given** a command includes file or upstream business references
**When** the message or conversation reference is recorded
**Then** the event stores only stable reference IDs and reference types
**And** file binaries, raw upstream records, Party personal data, and provider-owned payloads are not persisted in Conversations events.

**Given** external business identifiers or provider correlation identifiers are supplied
**When** the reference metadata is recorded
**Then** external identifiers remain tenant-scoped discovery keys and provider identifiers remain correlation metadata
**And** neither replaces the internal `ConversationId`, message identity, Party identity, or upstream stable reference identity.

**Given** a reference command targets unsupported, malformed, missing, cross-tenant, or incompatible reference metadata
**When** the command is handled
**Then** the system returns a typed documented rejection outcome
**And** no successful reference event is emitted.

**Given** aggregate and projection tests replay reference events
**When** conversation state and read models are rehydrated
**Then** the system reconstructs business references, file references, provider correlation metadata, and external identifiers deterministically
**And** tests prove upstream records and file binaries are never stored in Conversations events.

### Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections

As an adopter system,
I want every conversation command and read to pass tenant access checks before touching conversation state,
So that cross-tenant access, enumeration, and stale authorization cannot leak or mutate protected records.

**Requirements Covered:** FR26-FR32.

**Acceptance Criteria:**

**Given** a command or query arrives with tenant context and caller context
**When** the application boundary handles the request
**Then** it checks the local Tenants access projection before aggregate load, command dispatch, projection read, publication detail access, or audit-sensitive metadata access
**And** missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, disabled, unavailable, or unknown tenant state fails closed.

**Given** a request targets a conversation from another tenant or an inaccessible tenant scope
**When** the request is evaluated
**Then** the response is typed and content-safe
**And** unauthorized, nonexistent, and cross-tenant records are indistinguishable to non-privileged callers unless policy explicitly permits disclosure.

**Given** tenant authorization fails before a write command
**When** the command is rejected
**Then** no aggregate state is loaded, no domain event is emitted, no projection mutation is performed, and no tenant-crossing metadata is published
**And** the rejection result maps to documented tenant-binding or tenant-isolation error semantics.

**Given** tenant authorization fails before a read or list operation
**When** the read boundary responds
**Then** it does not reveal conversation title, participant names, snippets, timestamps, counts, pagination gaps, business references, provider correlation metadata, or whether a protected record exists
**And** it returns a safe failure or no-access result suitable for adopter handling.

**Given** tenant access tests run
**When** positive and adversarial cases execute
**Then** tests cover missing tenant, malformed tenant, stale projection, unavailable projection store, disabled tenant, non-member caller, insufficient role, cross-tenant ID guessing, mixed-tenant command metadata, and projection poisoning
**And** failures are verified before aggregate or projection access.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate tenant isolation evidence is carried forward into Story 5.5 for manifest aggregation and signing.

### Story 1.6: Add Idempotent Command Handling

As an adopter system,
I want duplicate conversation commands to return stable outcomes,
So that retries, client timeouts, and at-least-once delivery do not create duplicate conversations, messages, participants, or references.

**Requirements Covered:** FR6, FR7.

**Acceptance Criteria:**

**Given** a create, append-message, add-participant, attach-reference, update-metadata, or close/archive command includes an idempotency key
**When** the same tenant, conversation scope, command type, idempotency key, and equivalent payload are submitted more than once
**Then** the system returns the same logical outcome without emitting duplicate successful domain events
**And** the response is stable enough for adopter retry handling.

**Given** an idempotency key is reused with a different payload or incompatible command context
**When** the command is evaluated
**Then** the system returns a typed idempotency-conflict rejection
**And** no conversation state mutation or publication occurs.

**Given** a command outcome is unknown to the caller because of timeout, retry, duplicate delivery, or publication lag
**When** the caller resubmits the same idempotent command
**Then** the system resolves the stored or replayed command outcome consistently
**And** the result does not depend on provider-owned session IDs.

**Given** duplicate or reordered command delivery occurs
**When** aggregate state, idempotency records, and projections are evaluated
**Then** projections remain deterministic and no duplicate business effects appear in read models
**And** content-safe diagnostics distinguish duplicate, conflict, unsupported-version, and infrastructure uncertainty.

**Given** idempotency tests run
**When** duplicate equivalent commands, duplicate non-equivalent commands, reordered delivery, unknown client outcome retry, and tenant-mismatched key reuse are exercised
**Then** tests prove stable outcomes, conflict rejection, tenant scoping, no duplicate events, no projection divergence, and no cross-tenant leakage.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate idempotency evidence is carried forward into Story 5.6 for manifest aggregation and signing.

### Story 1.7: Project Conversation Read Models with Freshness Metadata

As an adopter system,
I want tenant-safe conversation read models with explicit freshness metadata,
So that consumers can read conversation state without confusing stale, rebuilding, unavailable, or hidden data for current truth.

**Requirements Covered:** FR33-FR37.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Shared trust/freshness vocabulary is approved before read boundary, UI-facing contract, or diagnostic implementation starts.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle events are persisted
**When** projection handlers process the ordered event stream
**Then** they derive tenant-scoped read models for conversation summary and conversation detail
**And** handlers tolerate duplicate, replayed, and out-of-order delivery according to documented projection behavior.

**Given** a projection read model is returned
**When** the consumer inspects it
**Then** it includes freshness metadata such as projection version or cursor, last applied event position or timestamp, projection generated timestamp, stale indicator, lag duration where available, and freshness state
**And** freshness states distinguish current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.

**Given** projection metadata is missing, contradictory, stale, or unavailable
**When** a read boundary or UI-facing contract formats the result
**Then** it downgrades trust to unknown, stale, rebuilding, unavailable, or hidden rather than presenting the read model as current
**And** governed actions depending on current projection state are blocked or marked unavailable.

**Given** projection handlers materialize conversation timelines
**When** messages, participants, file references, provider correlation metadata, and business references are projected
**Then** the read model contains only tenant-authorized, content-safe fields and stable IDs
**And** it does not persist Party personal data, raw upstream records, file binaries, raw provider payloads, or EventStore internals.

**Given** projection tests run
**When** ordered replay, duplicate delivery, projection deletion/rebuild, stale metadata, unavailable store, and mixed-tenant poison events are exercised
**Then** tests prove deterministic read-model reconstruction, freshness-state behavior, duplicate tolerance, and fail-closed tenant isolation.

### Story 1.8: Retrieve and List Conversations by Tenant Business Context

As an adopter system,
I want to retrieve and list conversations within an authorized tenant scope,
So that applications and operators can find the right conversation records without leaking inaccessible records or relying on provider session state.

**Requirements Covered:** FR8-FR12, FR21, FR22, FR28-FR30, FR36, FR37.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Shared trust/freshness vocabulary is approved before retrieve/list contracts expose freshness, hidden, stale, rebuilding, unavailable, or degraded states.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** an authorized caller requests a conversation by `ConversationId`
**When** the read boundary evaluates tenant access and reads the projection
**Then** it returns conversation detail with participant set, ordered message timeline metadata, attachment/file references, governance state placeholders where available, business references, provider correlation metadata, and freshness context
**And** the response exposes Conversations contracts rather than EventStore stream, snapshot, or projection internals.

**Given** an authorized caller lists conversations for a tenant
**When** filters such as external business identifier, project reference, folder reference, lifecycle state, date range, recent activity, or participant reference are supplied
**Then** the result contains tenant-scoped conversation summaries and permission-safe pagination metadata
**And** external business identifiers are treated as correlation/search keys distinct from internal `ConversationId`.

**Given** a caller is unauthorized, tenant binding is invalid, or a requested conversation is nonexistent or cross-tenant
**When** retrieve or list is evaluated
**Then** the response is content-safe and does not reveal titles, participant names, snippets, timestamps, counts, ordering gaps, business references, provider metadata, or existence of protected records
**And** the result maps to documented tenant-isolation or hidden/not-found semantics.

**Given** projections are stale, rebuilding, unavailable, or hidden by tenant isolation
**When** retrieve or list results are returned
**Then** freshness state and safe next-action metadata are included where authorized
**And** the read boundary does not silently present stale or incomplete data as current.

**Given** retrieve/list tests run
**When** authorized reads, filtered lists, cross-tenant ID guessing, inaccessible records, stale projections, unavailable projections, and provider-session loss scenarios are exercised
**Then** tests prove correct filtering, freshness signaling, content-safe denial, and conversation recoverability without provider-owned session authority.

### Story 1.9: Resolve Parties and Upstream References at Read Time

As an adopter system,
I want conversation reads to hydrate participant and upstream reference display data from canonical sources,
So that stored conversation events remain stable and privacy-safe while users still see current authorized context.

**Requirements Covered:** FR23-FR25.

**Acceptance Criteria:**

**Given** a conversation read model contains stable `PartyId`, `ProjectId`, `FolderId`, and `FileId` references
**When** an authorized read request is composed
**Then** the read boundary uses Conversations-owned adapters to hydrate authorized display/status data from upstream canonical sources
**And** durable conversation events and projections remain based on stable IDs rather than mutable upstream display data.

**Given** a Party can be resolved for the caller and tenant scope
**When** participant display data is hydrated
**Then** the response includes only authorized participant display/status fields allowed by policy
**And** it does not persist or expose unauthorized Parties personal data, contact values, identifiers, person details, or organization details.

**Given** an upstream Party, Project, Folder, or File reference is deleted, inaccessible, stale, unavailable, or policy-filtered
**When** the conversation is read
**Then** the response uses a safe degraded, unresolved, redacted, or unavailable state
**And** it does not mutate historical events or imply that inaccessible upstream data exists unless policy allows disclosure.

**Given** upstream hydration is slow or partially unavailable
**When** a read response is composed
**Then** the system avoids N+1 behavior through batching or documented bounded calls where available
**And** authorized reads may degrade display hydration while command-time participant validation remains fail-closed.

**Given** hydration tests run
**When** Party rename, deleted Party, inaccessible Party, unavailable Parties adapter, stale upstream reference, and unauthorized upstream reference scenarios are exercised
**Then** tests prove read-time display updates without event rewrites, safe degradation, no Party personal-data persistence, and no cross-tenant disclosure.

### Story 1.10: Publish Versioned Conversation Domain Events

As a downstream Hexalith system,
I want to consume tenant-aware conversation domain events with explicit schema metadata,
So that projections and integrations can react to meaningful conversation changes without depending on internal EventStore mechanics.

**Requirements Covered:** FR32, FR38-FR40.

**Acceptance Criteria:**

**Given** a meaningful conversation state change occurs
**When** the command succeeds and EventStore persists the domain event
**Then** Conversations publishes tenant-aware domain events for supported changes such as conversation-created, participant-added, message-appended, reference-attached, metadata-updated, and lifecycle-changed
**And** published contracts use Conversations language rather than EventStore envelope or stream internals.

**Given** a published event is emitted
**When** downstream consumers inspect it
**Then** the event includes schema version, event type, tenant scope, conversation identity, correlation/causation metadata, and stable references needed by the active contract
**And** it excludes Party personal data, raw provider payloads, file binaries, raw upstream records, redacted content, and cross-tenant metadata.

**Given** publication is delivered through Dapr/EventStore publication paths
**When** duplicate, replayed, or reordered delivery occurs
**Then** downstream handlers can identify event type/version and process idempotently according to documented semantics
**And** projection notifications are treated as hints rather than source-of-truth state.

**Given** an event, command, or projection schema version is unsupported
**When** publication or consumption is validated
**Then** unsupported versions fail with typed documented errors or compatibility diagnostics
**And** no consumer is required to understand internal aggregate snapshots, stream names, or SignalR group implementation details.

**Given** publication tests run
**When** successful events, rejected commands, duplicate delivery, unsupported versions, tenant mismatch, and content leakage cases are exercised
**Then** tests prove correct event shape, no publication on rejected commands, bounded metadata, tenant isolation, schema metadata, and absence of forbidden payloads.

### Story 1.11: Prove Replay, Schema Versioning, and Projection Rebuild Behavior

As a platform owner,
I want proof that conversation records can be replayed, rebuilt, and evolved safely,
So that the first conversation substrate is trustworthy before governance and compliance workflows build on it.

**Requirements Covered:** FR12, FR33-FR37, FR40, FR41.

**Scope Note:** This story proves the Epic 1 foundation hooks for replay, rebuild, and schema-version handling. Release-gating provider portability and event schema evolution evidence remain owned by Stories 5.8 and 5.9.

**Ready for Dev Preconditions:**

- EventStore envelope ownership and evolution are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Projection freshness blocking semantics are decided or waived before rebuild and replay freshness behavior is implemented.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** a tenant-scoped conversation event stream exists
**When** aggregate state is rehydrated from the ordered events
**Then** the reconstructed state matches the expected conversation identity, lifecycle, participants, messages, business references, provider correlation metadata, and attribution
**And** replay is deterministic for the same event history and contract version.

**Given** v1 projections are deleted or marked rebuilding
**When** the projection rebuild process replays persisted events
**Then** it produces functionally equivalent summary and detail read models for the same tenant, conversation, event history, and contract version
**And** rebuild progress, stale state, unavailable state, and completion are surfaced through freshness metadata.

**Given** old, mixed, additive, or unsupported event versions exist in a stream
**When** replay and projection handlers process them
**Then** supported versions replay through documented compatibility or upcaster behavior
**And** unsupported versions fail with typed documented errors rather than being skipped silently.

**Given** derived state disagrees with replayed EventStore state
**When** verification detects the disagreement
**Then** EventStore history wins, the derived artifact is marked stale, invalid, quarantined, or rebuilding, and content-safe diagnostics are emitted
**And** governed disclosure actions remain blocked unless a later ADR explicitly permits action on stale state.

**Given** replay and rebuild tests run
**When** projection deletion, duplicate events, mixed-version streams, unsupported versions, stale derived state, tenant mismatch, and provider correlation changes are exercised
**Then** tests prove deterministic replay, rebuild equivalence, version-aware behavior, provider portability, tenant isolation, and safe diagnostics
**And** the output can feed the release-evidence placeholder or manifest entry for Epic 1.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate event schema evolution evidence is carried forward into Story 5.9 for manifest aggregation and signing.

## Epic 2: Governed Retention, Redaction, and Audit

Authorized users can apply retention, sensitivity, redaction, archival, and privileged-action governance with paired audit evidence and fail-closed audit behavior.

### Story 2.1: Define Governance Policy and Audit Contracts

As a compliance integrator,
I want explicit governance and audit contracts for retention, sensitivity, redaction, and privileged actions,
So that governance behavior is enforceable, testable, and safe before mutation workflows are implemented.

**Requirements Covered:** FR42-FR49, FR51-FR53.

**Acceptance Criteria:**

**Given** governance contracts are added
**When** retention, sensitivity, redaction, archival, legal-hold deferral, and privileged-action concepts are modeled
**Then** each contract includes tenant scope, conversation identity, actor attribution, rationale, policy reference, timestamp, schema version, and correlation/causation metadata
**And** contracts avoid raw message content, Party personal data, provider payloads, and unauthorized upstream details.

**Given** audit contracts are added
**When** a governance mutation contract is defined
**Then** a corresponding audit evidence shape exists for the same operation
**And** the contract can represent success, denial, audit-unavailable failure, and policy-blocked outcomes.

**Given** governance state is projected or displayed later
**When** redaction, retention, and legal-hold semantics are documented
**Then** the contracts distinguish event history, projected/displayed content, audit records, derived materializations, archival, logical deletion, retention enforcement, and legal-hold deferral
**And** they do not imply irreversible source-event deletion unless an approved ADR exists.

**Given** contract tests run
**When** governance and audit contract payloads are serialized and validated
**Then** required rationale, actor, tenant, policy, schema version, and correlation fields are enforced
**And** forbidden content and personal-data fields are absent from contract shapes.

### Story 2.2: Set Conversation Retention Policy with Rationale

As an authorized governance operator,
I want to set or replace a conversation retention policy with rationale,
So that conversation retention is explicit, auditable, and tenant-scoped.

**Requirements Covered:** FR42, FR47-FR49.

**Acceptance Criteria:**

**Given** an authorized operator submits a set-retention-policy command with tenant scope, conversation identity, policy reference, actor attribution, rationale, schema version, and correlation metadata
**When** the command passes tenant, role, policy, and audit-precondition checks
**Then** the aggregate emits a retention-policy-set or retention-policy-replaced domain event
**And** the event stores policy identifiers, rationale metadata, actor ID, tenant scope, and timestamps without storing unnecessary content or Party personal data.

**Given** an existing retention policy is replaced
**When** the replacement command succeeds
**Then** the resulting governance state identifies the active policy, prior policy reference where appropriate, rationale, actor, timestamp, and policy basis
**And** replay reconstructs the same active retention state from the event stream.

**Given** the operator lacks permission, tenant state is missing or stale, policy reference is invalid, rationale is missing, schema version is unsupported, or the conversation is unavailable
**When** the command is handled
**Then** a typed documented rejection is returned
**And** no retention policy mutation event is emitted.

**Given** audit recording is required for retention changes
**When** the command succeeds
**Then** paired audit evidence is recorded or emitted in the same governed operation boundary
**And** the response includes a safe audit handle where policy allows.

**Given** retention policy tests run
**When** set, replace, replay, unauthorized, missing rationale, invalid policy, stale tenant projection, unsupported version, and audit-required scenarios are exercised
**Then** tests prove fail-closed behavior, audit pairing, deterministic replay, tenant isolation, and safe event payloads.

### Story 2.3: Mark Conversation Content as Sensitive

As an authorized governance operator,
I want to mark conversation content as sensitive with policy attribution,
So that downstream projections, UI, exports, and evidence workflows can treat sensitive material safely.

**Requirements Covered:** FR43, FR47-FR49.

**Acceptance Criteria:**

**Given** an authorized operator submits a mark-sensitive command for a conversation, message, attachment reference, participant attribution, or defined content segment
**When** tenant, role, policy, target, rationale, and audit-precondition checks pass
**Then** the aggregate emits a sensitivity-marked event with tenant scope, target reference, sensitivity category, policy reference, actor attribution, rationale, timestamp, and schema version
**And** the event does not store raw sensitive content, Party personal data, provider payloads, or file binaries.

**Given** content has been marked sensitive
**When** projections rebuild or update
**Then** read models expose authorized sensitivity state and safe category metadata needed for later redaction, display, citation, export, and command gating
**And** unauthorized consumers receive safe hidden, restricted, or unavailable states without protected details.

**Given** a sensitivity mark targets missing, cross-tenant, already-incompatible, unsupported-version, or unauthorized content
**When** the command is handled
**Then** the system returns a typed documented rejection
**And** no sensitivity-marked event or audit success record is emitted.

**Given** audit evidence is required
**When** sensitivity marking succeeds
**Then** paired audit evidence records actor, timestamp, tenant, conversation, target reference, policy basis, and rationale
**And** the audit payload remains content-safe.

**Given** sensitivity tests run
**When** authorized marks, repeated marks, invalid target references, unauthorized targets, stale tenant projection, audit unavailable, projection rebuild, and hidden-state reads are exercised
**Then** tests prove tenant isolation, audit pairing, content-safe events, projection behavior, and no sensitive value leakage.

### Story 2.4: Redact Message Content with Audit Attribution

As an authorized governance operator,
I want to record redaction intent as an audited domain event,
So that protected content can be removed from governed surfaces while auditability remains intact.

**Requirements Covered:** FR44-FR47, FR51.

**Scope Note:** This story covers redaction command, domain event, typed rejection, and paired audit behavior only. Projection/read-model behavior is covered by Story 2.4.1, client-visible disclosure safety by Story 2.4.2, and operational/export/log/trace safety by Story 2.4.3.

**Acceptance Criteria:**

**Given** an authorized operator submits a redact-message command with tenant scope, conversation identity, message or content target reference, redaction category, policy reference, rationale, actor attribution, schema version, and correlation metadata
**When** tenant, role, policy, target, and audit-precondition checks pass
**Then** the aggregate emits an append-only redaction event or approved tombstone event
**And** the event records redaction metadata without storing original redacted content, Party personal data, provider payloads, or file binaries.

**Given** a redaction target is missing, already redacted, cross-tenant, unauthorized, unsupported-version, or blocked by policy
**When** the command is handled
**Then** the system returns a typed documented rejection or idempotent no-op outcome according to policy
**And** no misleading successful redaction event is emitted.

**Given** audit evidence is required for redaction
**When** the redaction succeeds
**Then** paired audit evidence records actor, timestamp, tenant, conversation, target reference, policy basis, rationale, and redaction category
**And** the audit record is citeable without exposing the redacted content.

**Given** redaction command tests run
**When** authorized redactions, duplicate commands, invalid targets, unsupported versions, stale tenant projection, and audit unavailable scenarios are exercised
**Then** tests prove command/event behavior, audit pairing, typed rejection semantics, and absence of original redacted content from domain events.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate redaction replay evidence is carried forward into Story 5.7 for manifest aggregation and signing.

### Story 2.4.1: Apply Redaction to Projections and Read Models

As a compliance operator,
I want projections, read models, temporal views, and search materializations to apply redaction state consistently,
So that protected content does not reappear during normal reads, rebuilds, or point-in-time reconstruction.

**Requirements Covered:** FR44-FR46, FR50, FR58-FR61.

**Acceptance Criteria:**

**Given** a redaction event exists
**When** projections, read models, search materializations, temporal views, evidence views, caches, and rebuild paths update or replay
**Then** redacted content is replaced with authorized redaction placeholders or safe unavailable states
**And** original protected values do not appear in projected or reconstructed state.

**Given** an authorized operator reads a redacted conversation or temporal reconstruction
**When** projection freshness, redaction state, governance state, and audit trail are returned
**Then** the response includes safe redaction attribution and citeable audit handles where policy allows
**And** it does not expose the original redacted content.

**Given** projection redaction tests run
**When** normal projection update, full rebuild, point-in-time reconstruction, cache refresh, stale projection, and tenant-isolated read scenarios are exercised
**Then** tests prove redaction determinism, tenant isolation, projection freshness signaling, and no redacted-value reintroduction.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate redaction replay evidence is carried forward into Story 5.7 for manifest aggregation and signing.

### Story 2.4.2: Verify UI, Accessibility, Clipboard, and Citation Redaction Safety

As a compliance operator using visual, keyboard, screen-reader, and clipboard workflows,
I want redacted content to stay absent from every client-observable surface,
So that investigation workflows remain safe across DOM, ARIA, tooltips, titles, screenshots, citation copy, and responsive duplicates.

**Requirements Covered:** FR44-FR46, FR59, FR62, FR63; UX-DR6, UX-DR7, UX-DR10, UX-DR35, UX-DR44-UX-DR52; NFR21, NFR69-NFR77.

**Acceptance Criteria:**

**Given** redacted content is present in an authorized investigation workspace
**When** visible UI, hidden DOM, ARIA labels, live regions, tooltips, browser title, breadcrumbs, screenshots, responsive duplicates, and clipboard output are rendered or copied
**Then** only approved placeholders, policy labels, citation handles, and safe redaction attribution are exposed
**And** original redacted values remain absent from every client-observable surface.

**Given** citation copy or temporal evidence links are used for redacted content
**When** the operator copies or opens citation material
**Then** the citation remains stable and audit-citeable
**And** it includes no original redacted value, Party personal data, provider payload, or unauthorized tenant metadata.

**Given** UI redaction safety tests run
**When** desktop, tablet, mobile, keyboard-only, screen-reader, browser zoom, high-contrast, clipboard, tooltip, denied, stale, and responsive duplicate cases are exercised
**Then** tests prove WCAG 2.1 AA-compatible redaction communication and Leak Sentinel absence checks across client surfaces.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate redaction replay evidence is carried forward into Story 5.7 for manifest aggregation and signing.

### Story 2.4.3: Verify Operational, Export, Log, Trace, and Error Redaction Safety

As an SRE or release owner,
I want redaction safety verified across exports, logs, traces, errors, diagnostics, caches, and future derived indexes,
So that operational and release evidence cannot leak protected content.

**Requirements Covered:** FR44-FR47, FR89 validation support; NFR19, NFR21, NFR55-NFR62.

**Scope Note:** v1 verification covers only operational and evidence surfaces active in v1. Future derived indexes, export workflows, and evidence-bundle behavior remain ADR-gated and out of implementation scope unless promoted into the active release scope by an approved ADR or sprint change proposal. Tests may assert that missing ADR coverage blocks implementation; they must not implement implicit index, export, or evidence-bundle semantics.

**Acceptance Criteria:**

**Given** redacted content exists
**When** exports, logs, traces, errors, diagnostics, conformance evidence, observability signals, caches, screenshots, and future derived indexes are produced or rebuilt
**Then** redacted content is absent from all operational and evidence surfaces
**And** diagnostics remain useful through safe reason classes, audit handles, policy identifiers, and bounded correlation metadata.

**Given** future derived indexes or exports are in scope
**When** redaction propagation, rebuild, delete/re-index, or evidence export runs
**Then** behavior follows the active ADR and release scope
**And** missing ADR coverage blocks implementation rather than allowing implicit indexing or export semantics.

**Given** redaction replay tests run
**When** projection rebuild, temporal reconstruction, cache refresh, export generation, accessibility rendering, clipboard copy, duplicate command, and log/trace/error scenarios are exercised
**Then** tests prove redacted content does not reappear, audit evidence remains available, and replay is deterministic under tenant isolation.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate redaction replay evidence is carried forward into Story 5.7 for manifest aggregation and signing.

### Story 2.5: Enforce Audit Pairing and Audit-Unavailable Fail-Closed Behavior

As a compliance owner,
I want every governance mutation to require paired audit evidence,
So that no retention, sensitivity, redaction, archival, or privileged governance action can silently change state.

**Requirements Covered:** FR47-FR49.

**Acceptance Criteria:**

**Given** a governance mutation command is evaluated
**When** audit recording is available and all policy checks pass
**Then** the system records or emits the domain mutation and paired audit evidence as one governed operation boundary
**And** the response exposes a safe audit handle where policy allows.

**Given** audit recording is unavailable, ambiguous, stale, denied, or fails validation
**When** a governance mutation command is submitted
**Then** the command fails closed with a typed audit-unavailable or audit-required rejection
**And** no governance domain mutation event, projection mutation, publication, or success audit record is produced.

**Given** non-governance conversation activity occurs during audit degradation
**When** the command does not mutate governance state
**Then** the system follows the active ADR or policy for whether the activity may continue
**And** the response clearly distinguishes non-governance allowance from governance mutation denial.

**Given** audit pairing is enforced
**When** retention, sensitivity, redaction, archival, privileged metadata mutation, and audit-record action paths are exercised
**Then** every successful governance mutation has a corresponding audit evidence record with tenant, conversation, actor, timestamp, policy basis, rationale, operation, and outcome
**And** missing or mismatched audit evidence is treated as a release-blocking verification failure.

**Given** audit enforcement tests run
**When** successful governance mutations, audit sink outage, partial audit failure, duplicate governance command, rejected governance command, and non-governance command during audit degradation are exercised
**Then** tests prove fail-closed governance behavior, paired evidence, no silent mutation paths, typed errors, tenant isolation, and content-safe diagnostics.

### Story 2.6: Reconstruct Point-in-Time Governance State

As a compliance operator,
I want to reconstruct conversation and governance state as it existed at a prior point in time,
So that audits and investigations can rely on stable historical evidence.

**Requirements Covered:** FR50.

**Acceptance Criteria:**

**Given** a tenant-scoped conversation has message, participant, retention, sensitivity, redaction, archival, and audit events
**When** an authorized point-in-time reconstruction is requested for a timestamp, event position, projection version, or contract-defined temporal cursor
**Then** the system reconstructs message state and governance state as of that anchor
**And** the response identifies the authoritative temporal anchor used.

**Given** redaction or retention changes occurred after the requested point
**When** historical state is reconstructed
**Then** the output follows the active redaction, retention, and disclosure policy for historical views
**And** it does not reveal content that is redacted or unavailable under current authorization and policy.

**Given** the temporal cursor is malformed, unsupported, cross-tenant, stale, unavailable, or outside retained coverage
**When** reconstruction is requested
**Then** the system returns a typed content-safe failure or migration-boundary response
**And** it does not reveal whether protected records or events exist.

**Given** reconstruction is projection-backed or replay-backed
**When** freshness or rebuild state affects the result
**Then** the response exposes freshness, completeness, and confidence metadata
**And** it does not present incomplete historical state as authoritative.

**Given** point-in-time tests run
**When** valid cursor, timestamp, event position, redacted content, retention changes, cross-tenant cursor, unsupported cursor, projection rebuild, and out-of-coverage scenarios are exercised
**Then** tests prove deterministic reconstruction, tenant isolation, redaction safety, safe failure semantics, and stable temporal evidence behavior.

### Story 2.7: Govern Audit Record Access, Retention, and Redaction

As a compliance owner,
I want audit records to have explicit access, retention, export, and redaction behavior,
So that audit evidence remains reviewable without becoming an unmanaged disclosure surface.

**Requirements Covered:** FR51-FR53.

**Acceptance Criteria:**

**Given** audit records are created for governance and privileged actions
**When** audit access policy is evaluated
**Then** the system can classify audit-record actions as allowed, denied, redacted, exported, separately logged, or policy-blocked
**And** each action remains tenant-scoped and actor-attributed.

**Given** retention and redaction policy applies to governance audit records
**When** audit records are projected, exported, viewed, or rebuilt
**Then** the system applies the approved retention and redaction treatment for audit evidence
**And** audit handling remains distinguishable from conversation message redaction and source event history.

**Given** an unauthorized or insufficiently scoped user requests audit details
**When** the audit read or export boundary evaluates access
**Then** the response is content-safe and does not leak protected tenant, Party, conversation, policy, redacted content, or operational details
**And** access denial itself is auditable where policy requires.

**Given** an audit record is redacted or partially withheld
**When** an authorized reviewer inspects the record
**Then** the visible audit view preserves actor, timestamp, action class, outcome, policy basis, and rationale where allowed
**And** withheld fields are represented with safe redaction or unavailable states.

**Given** audit-record governance tests run
**When** allowed access, denied access, export, redaction, retention expiry, tamper attempt, tenant mismatch, and rebuild scenarios are exercised
**Then** tests prove tenant isolation, citeable audit evidence, policy treatment, redaction safety, and no silent audit mutation paths.

### Story 2.8: Record and Review Privileged Operational Justification

As a compliance reviewer,
I want privileged operational actions that touch tenant-scoped conversation data to include structured justification and reviewable audit evidence,
So that operator access is accountable and tenant-visible where policy requires.

**Requirements Covered:** FR54, FR55.

**Acceptance Criteria:**

**Given** an operator performs a privileged action that reads, rebuilds, repairs, exports, verifies, changes visibility, changes metadata, or otherwise touches tenant conversation data
**When** the action is requested
**Then** the system requires structured justification with tenant scope, affected conversation or scope, actor, operation class, policy basis, rationale, timestamp, and correlation metadata
**And** the action cannot proceed when required justification is missing or invalid.

**Given** a privileged action succeeds, fails, is denied, or is partially completed
**When** audit evidence is recorded
**Then** the audit record links justification, actor, timestamp, tenant, affected conversation or scope, policy basis, result, and resulting domain or operational evidence
**And** the audit payload remains content-safe.

**Given** a reviewer opens privileged-action history
**When** the reviewer is authorized for the tenant and audit scope
**Then** the reviewer can inspect justification, actor, timestamp, tenant, affected conversation, policy basis, outcome, and audit handle as one coherent record
**And** redacted or unavailable fields are clearly distinguished from missing fields.

**Given** a privileged action is unauthorized, cross-tenant, stale, unsupported, or missing audit availability
**When** the operation is evaluated
**Then** the system returns a typed content-safe denial or audit-unavailable result
**And** no privileged mutation or disclosure occurs.

**Given** privileged-action tests run
**When** approved access, missing justification, stale justification, unauthorized operator, cross-tenant target, audit unavailable, partial failure, and review-history scenarios are exercised
**Then** tests prove structured justification enforcement, tenant-visible audit evidence, reviewability, typed failure semantics, and content-safe diagnostics.

## Epic 3: Compliance Investigation Workspace

Compliance operators can find, inspect, time-travel, cite, and verify governed conversation evidence through read-only workflows and buyer acceptance scenarios.

### Story 3.1: Find Tenant-Scoped Conversations Safely

As a compliance operator,
I want to search for tenant-scoped conversations by external identifiers and business context,
So that I can find relevant governed records without leaking inaccessible records.

**Requirements Covered:** FR56, FR57; UX-DR11, UX-DR21, UX-DR30, UX-DR31.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Shared trust/freshness vocabulary is approved before result trust previews, freshness filters, or safe empty states are implemented.
- UX safety gate ownership is recorded before responsive, timing, metadata, or count-leakage verification begins.

**Acceptance Criteria:**

**Given** an authorized operator enters a tenant scope and business search criteria
**When** the search executes by customer, account, case ID, date range, project reference, folder reference, participant reference, lifecycle state, redaction state, freshness state, audit readiness, or verification state
**Then** results include only accessible conversation summaries for that tenant
**And** result rows include source-owned trust preview metadata needed to choose safely before opening a record.

**Given** inaccessible, nonexistent, or cross-tenant records could match the search
**When** results, counts, facets, ordering, autocomplete, pagination, recent searches, empty states, and response timing are rendered
**Then** the workspace does not reveal protected existence, titles, snippets, participants, timestamps, business references, or sort gaps
**And** safe empty copy such as no accessible matches is used where existence cannot be disclosed.

**Given** a result row is displayed
**When** the operator inspects why it is visible
**Then** the row can explain authorized scope, match source, freshness, redaction state, participant resolution state, and citation availability without exposing inaccessible records or redacted content
**And** missing trust metadata downgrades the row to unknown, stale, unavailable, incomplete, or degraded.

**Given** search workspace tests run
**When** authorized search, no accessible matches, unauthorized-existing records, cross-tenant poison data, stale results, pagination, facets, autocomplete, and timing-sensitive cases are exercised
**Then** tests prove permission-safe discovery, trust-preview behavior, tenant isolation, and no leakage through counts or metadata.

### Story 3.2: Read Governed Conversation Evidence

As a compliance operator,
I want to open a governed conversation record with trust posture before timeline content,
So that I can decide whether the evidence is safe to rely on.

**Requirements Covered:** FR58; UX-DR1-UX-DR5, UX-DR12, UX-DR13, UX-DR18, UX-DR19, UX-DR22, UX-DR24, UX-DR25, UX-DR29, UX-DR32.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics and shared trust/freshness vocabulary are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Command availability metadata is decided or waived before command eligibility appears in the governed record view.
- UX safety gate ownership is recorded before trust-critical component, disclosure-surface, accessibility, or Leak Sentinel work begins.

**Acceptance Criteria:**

**Given** an authorized operator opens a conversation
**When** the governed record view loads
**Then** it displays tenant scope, record identity, temporal cursor, trust posture, evidence completeness, projection freshness, participant resolution, citation status, and command eligibility before timeline reliance
**And** these trust claims come only from Conversations projections or command availability metadata.

**Given** the evidence timeline is displayed
**When** participants, messages, attachments, governance states, redactions, and freshness metadata are rendered
**Then** entries appear as evidence records rather than casual chat bubbles
**And** each entry preserves chronological order, actor attribution, timestamp, citation/audit anchors where available, and safe degraded states.

**Given** trust metadata is missing, contradictory, stale, unavailable, or partially loaded
**When** the record view renders
**Then** it shows an explicit unknown, stale, unavailable, incomplete, blocked, or degraded state
**And** it never presents the record as current, complete, cite-ready, or action-ready by default.

**Given** governed record tests run
**When** fully trusted, stale projection, missing citation, unresolved participant, partial evidence, unavailable projection, and cross-tenant attempts are exercised
**Then** tests prove trust ordering, projection-owned state, fail-closed rendering, and absence of raw EventStore internals.

### Story 3.3: Inspect Redaction Attribution and Governance Audit Trail

As a compliance operator,
I want inline redaction attribution and audit trail access,
So that I can understand why evidence changed and who authorized governance actions.

**Requirements Covered:** FR59, FR60; UX-DR6-UX-DR8, UX-DR12, UX-DR15-UX-DR17, UX-DR20, UX-DR26, UX-DR33.

**Acceptance Criteria:**

**Given** a conversation contains redacted or sensitive evidence
**When** an authorized operator views the timeline
**Then** redaction placeholders show authorized category, policy reason class, actor attribution where allowed, timestamp, and audit reference
**And** original redacted content is absent from visible text, hidden DOM, tooltips, accessible names, copied values, telemetry, logs, and responsive duplicates.

**Given** a governance audit trail exists
**When** the operator opens inline audit details
**Then** the view displays authorized audit entries with timestamp, actor, action, outcome, policy basis, rationale, and evidence anchors
**And** audit detail access is independently authorized from the parent timeline view.

**Given** an audit or redaction detail is unavailable, unauthorized, stale, or partially withheld
**When** the detail drawer renders
**Then** it uses safe unavailable, restricted, redacted, or incomplete states
**And** it does not briefly render, focus, announce, or cache protected content during transitions.

**Given** redaction and audit UI tests run
**When** authorized audit, unauthorized audit, redacted evidence, missing audit anchor, permission downgrade, and screen-reader scenarios are exercised
**Then** tests prove audit readability, independent drawer authorization, redaction non-disclosure, accessibility safety, and safe focus behavior.

### Story 3.4: Copy Citations and Open Stable Temporal Evidence Links

As a compliance operator,
I want citation-ready references and stable temporal links,
So that I can cite conversation and audit evidence without exporting unsafe content.

**Requirements Covered:** FR62, FR63; UX-DR7, UX-DR10, UX-DR26, UX-DR35.

**Ready for Dev Preconditions:**

- Temporal evidence anchor is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Command availability metadata is decided or waived before citation or temporal-link behavior depends on command eligibility.
- UX safety gate ownership is recorded before clipboard, URL, browser-title, accessibility-tree, or responsive persistence verification begins.

**Acceptance Criteria:**

**Given** an operator is authorized to cite a transcript or audit element
**When** citation copy is requested
**Then** the copied value is built from a permission-safe citation DTO after authorization recheck
**And** it omits redacted content, unauthorized participant details, raw provider payloads, hidden fields, and rendered-text-only values.

**Given** a temporal evidence link is opened
**When** the link resolves by event position, projection version, timestamp, temporal cursor, or contract-defined business-record reference
**Then** it resolves to the same legally meaningful conversation state for the authorized tenant and policy scope
**And** the response states the authoritative anchor and freshness/completeness metadata.

**Given** the citation target is missing, deleted, redacted, stale, unavailable, cross-tenant, or unauthorized
**When** citation or temporal resolution is requested
**Then** the workspace renders broken, unavailable, redacted, hidden, or denied states safely
**And** it does not hide broken evidence or reveal protected existence.

**Given** citation and temporal tests run
**When** copy, clipboard, malformed cursor, stale projection, redacted target, deleted evidence, cross-tenant link, and responsive persistence scenarios are exercised
**Then** tests prove safe citation output, stable temporal resolution, tenant isolation, and no leakage through URLs, clipboard, browser title, or accessibility tree.

### Story 3.5: Preserve Read-Only Compliance Workflows and Safe Command Gates

As a compliance operator,
I want read-only investigation workflows and clearly gated privileged actions,
So that investigation cannot accidentally mutate conversation state.

**Requirements Covered:** FR64, FR65; UX-DR3, UX-DR9, UX-DR14, UX-DR20, UX-DR33, UX-DR34, UX-DR36-UX-DR38.

**Acceptance Criteria:**

**Given** an operator uses a workflow marked read-only
**When** they search, open, inspect, cite, time-travel, or review evidence
**Then** no conversation aggregate state is mutated
**And** any privileged or governance-changing action is absent, disabled, or explicitly routed through command availability metadata and policy checks.

**Given** a privileged action could mutate metadata, visibility, policy state, audit records, or governance state
**When** the action appears in the workspace
**Then** it is classified separately, displays a safe blocked or available reason from server metadata, and rechecks tenant, role, trust state, and command availability immediately before execution
**And** missing metadata is treated as governed unavailable, not an optional disabled action.

**Given** the operator loses permission or switches tenant during review
**When** the workspace receives the trust transition
**Then** it closes gated drawers, clears protected content, preserves only safe operator-entered intent, and announces a safe state change
**And** it does not leave recent-item traces, route labels, browser titles, or layout gaps that imply protected records.

**Given** read-only and command-gate tests run
**When** read-only inspection, blocked command, available command, missing command metadata, stale projection, permission downgrade, and tenant switch scenarios are exercised
**Then** tests prove no mutation in read-only paths, pre-execution recheck, command safety, and content-safe transition behavior.

### Story 3.6: Run Governance Verification and Return Structured Results

As a compliance operator,
I want to run governance verification for conversations, tenants, suites, or time windows,
So that I can distinguish product invariant failures from infrastructure execution failures.

**Requirements Covered:** FR66-FR68; UX-DR23, UX-DR26, UX-DR27, UX-DR38.

**Acceptance Criteria:**

**Given** an authorized operator requests governance verification for a conversation, tenant, suite, or time window
**When** the verification runs
**Then** it checks audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, schema compatibility, and related conformance expectations within the requested scope
**And** verification detail access remains tenant-scoped and fail-closed.

**Given** verification completes
**When** results are returned
**Then** the response is structured, machine-readable, and suitable for CI and incident workflows
**And** it distinguishes governance verification failures from infrastructure, dependency, unavailable data, stale projection, unsupported version, or execution failures.

**Given** verification cannot safely inspect a target
**When** tenant access, projection freshness, audit availability, or permission checks fail
**Then** the result uses typed content-safe failure semantics
**And** it does not reveal protected conversation existence, Party identifiers, redacted content, raw provider payload, or cross-tenant business references.

**Given** verification tests run
**When** passing verification, invariant failure, infrastructure failure, stale projection, missing audit pair, redaction replay failure, cross-tenant poison, and unauthorized scope scenarios are exercised
**Then** tests prove structured outcomes, failure classification, tenant isolation, and release-gate suitability.

### Story 3.7: Provide Self-Serve Buyer Acceptance Demo

As a buyer evaluator,
I want a seeded acceptance demo for governed conversation evidence,
So that I can validate the module's trust story without requiring production data.

**Requirements Covered:** FR69; UX-DR28, UX-DR37, UX-DR38, UX-DR52.

**Acceptance Criteria:**

**Given** seeded demo data is loaded for an authorized demo tenant
**When** the buyer opens the acceptance scenario
**Then** the demo exercises find, read, redaction, audit trail, time-travel, citation copy, projection freshness, and cross-tenant denial
**And** seeded records are clearly identified as demo data without weakening tenant isolation.

**Given** the demo includes redacted, stale, missing citation, unresolved participant, blocked command, and cross-tenant poison fixtures
**When** the buyer follows the guided scenario
**Then** each state displays safe trust posture, evidence completeness, and next safe action
**And** cross-tenant poison sentinel values never appear in any client-observable surface.

**Given** the demo is used for acceptance evidence
**When** the scenario completes
**Then** the system can produce or link to a content-safe evidence summary showing pass/fail status, scope, timestamp, signer or runner, and verification output
**And** the summary distinguishes module-level evidence from inherited platform controls.

**Given** demo tests run
**When** seeded data setup, guided flow, citation copy, time travel, redaction, tenant denial, stale projection, and evidence summary scenarios are exercised
**Then** tests prove repeatable demo behavior, safe fixture handling, no production dependency, and acceptance-readiness.

### Story 3.8A: Verify Responsive Layout and Mobile Safe Triage

As a compliance operator using different viewport sizes,
I want the investigation workspace to preserve trust ordering and safe read behavior across layouts,
So that I can find, read, cite, and stop safely without desktop-only assumptions.

**Requirements Covered:** FR56-FR69 verification support; UX-DR39-UX-DR43, UX-DR51, UX-DR52; NFR69-NFR72, NFR75, NFR77.

**Scope Note:** This story verifies responsive layout and mobile safe-triage behavior for the investigation workspace. Primary feature implementation remains in Stories 3.1-3.7. Accessibility-tree depth is covered by Story 3.8B; disclosure leakage, clipboard, browser, and telemetry safety are covered by Story 3.8C.

**Ready for Dev Preconditions:**

- Story 3.8 assignment plan is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- The story file names owner, fixture set, evidence output, pass/fail gate, and review date before implementation starts.
- UX safety gate ownership is recorded for responsive duplicate checks, command reauthorization fixtures, permission-safe DTO tests, and FrontComposer generated-versus-custom component boundary checks.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** the investigation workspace renders on desktop, tablet, mobile, and wide desktop breakpoints
**When** layout adapts
**Then** tenant scope, record identity, trust posture, evidence completeness, and command eligibility appear before timeline reliance at every breakpoint
**And** mobile remains safe read-only triage unless a governance action is explicitly designed, authorized, confirmed, and tested for narrow screens.

**Given** responsive layout creates cards, sticky headers, drawers, condensed summaries, skeletons, hidden regions, or duplicated markup
**When** protected, redacted, unauthorized, or stale content is present
**Then** every surface uses permission-safe DTOs before rendering
**And** CSS hiding, viewport-only hiding, and visually hidden text are not used as authorization controls.

**Given** responsive fixtures exercise fully trusted, redacted, stale, missing citation, unresolved participant, blocked command, cross-tenant attempt, permission downgrade, partial timeline, unauthorized-existing, nonexistent, high-contrast, reduced-motion, and browser zoom states
**When** desktop, tablet, mobile, and wide desktop evidence is generated
**Then** tests prove trust-order preservation, responsive duplicate safety, mobile safe triage, and viewport-specific safe telemetry labels
**And** the evidence output is traceable from the conformance manifest or release evidence bundle.

### Story 3.8B: Verify Accessibility Tree, Keyboard, and Screen-Reader Safety

As a compliance operator using keyboard navigation or assistive technology,
I want investigation trust, citation, redaction, and command-gate states to be exposed safely,
So that accessible workflows preserve the same evidence ordering and non-disclosure guarantees as visual workflows.

**Requirements Covered:** FR56-FR69 verification support; UX-DR44-UX-DR50, UX-DR52; NFR69-NFR77.

**Scope Note:** This story verifies focus order, screen-reader announcements, accessible names/descriptions, and keyboard-only flows. Responsive layout is covered by Story 3.8A; disclosure leakage, clipboard, browser, and telemetry safety are covered by Story 3.8C.

**Ready for Dev Preconditions:**

- Story 3.8 assignment plan is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- The story file names owner, fixture set, evidence output, pass/fail gate, and review date before implementation starts.
- UX safety gate ownership is recorded for accessibility-tree leakage checks, keyboard-only walkthroughs, screen-reader verification, redaction announcement safety, and command-gate announcement safety.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** a keyboard-only or screen-reader user completes Find -> Read -> Trust
**When** they navigate search, trust summary, timeline, citation, audit drawer, redaction placeholder, and command gate flows
**Then** focus order and announcements expose tenant scope, trust posture, evidence completeness, blocked-action reasons, and safe next actions before sensitive content
**And** redacted or unauthorized content is absent from accessible names, descriptions, live regions, headings, table summaries, focus announcements, and clipboard output.

**Given** accessibility verification runs
**When** no accessible matches, denied, redacted, stale, unresolved participant, blocked command, high-contrast, reduced-motion, browser zoom, and permission downgrade scenarios are exercised
**Then** automated checks plus manual keyboard-only and screen-reader evidence prove WCAG 2.1 AA expectations and safe state announcements
**And** failures identify the affected component, disclosure surface, scenario, expected safe output, actual output, and remediation owner.

**Given** accessible evidence is captured
**When** snapshots, transcripts, focus traces, or assistive-technology notes are stored
**Then** the evidence itself remains content-safe and tenant-safe
**And** it links to the fixture set, pass/fail gate, and conformance manifest or release evidence bundle.

### Story 3.8C: Verify Leakage, Clipboard, Browser, and Telemetry Disclosure Safety

As a compliance operator and release owner,
I want forbidden values absent from every investigation disclosure surface,
So that protected content, tenant boundaries, and governance state remain safe across rendered UI, browser surfaces, clipboard output, telemetry, and evidence artifacts.

**Requirements Covered:** FR56-FR69 verification support; UX-DR12, UX-DR35, UX-DR44-UX-DR52; NFR19-NFR21, NFR55-NFR61, NFR69-NFR77.

**Scope Note:** This story verifies Leak Sentinel, clipboard, browser-title, tooltip, screenshot, telemetry, loading/empty/denied, and responsive-duplicate disclosure safety. Responsive layout is covered by Story 3.8A; accessibility-tree navigation is covered by Story 3.8B.

**Ready for Dev Preconditions:**

- Story 3.8 assignment plan is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- The story file names owner, fixture set, evidence output, pass/fail gate, and review date before implementation starts.
- UX safety gate ownership is recorded for Leak Sentinel, clipboard safety checks, browser-title checks, screenshot checks, telemetry redaction checks, permission-safe DTO tests, and responsive duplicate checks.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** Leak Sentinel and canonical disclosure fixtures are prepared
**When** desktop, tablet, mobile, screen-reader, clipboard, tooltip, browser-title, telemetry, loading, empty, denied, redacted, stale, and responsive-duplicate states are exercised
**Then** forbidden strings and structured forbidden values are absent from rendered DOM text, attributes, ARIA properties, page title, clipboard output, telemetry envelopes, screenshots, and accessibility snapshots where available
**And** the evidence is traceable from the conformance manifest or release evidence bundle.

**Given** command availability, tenant isolation, redaction, and projection freshness states change
**When** browser titles, route metadata, tooltips, toasts, empty states, loading states, telemetry events, and evidence screenshots are emitted
**Then** each surface uses permission-safe DTOs and approved bounded identifiers only
**And** unauthorized, nonexistent, cross-tenant, redacted, hidden, stale, and unavailable states do not leak target tenant, Party, conversation, provider, file, business-reference, prompt, or content values.

**Given** disclosure safety tests fail
**When** the evidence is reported
**Then** failures identify the exact surface, forbidden value class, fixture, owner, and blocking/non-blocking classification
**And** the story cannot close until the unsafe output is fixed or an approved waiver records owner, approver, expiry, compensating control, buyer impact, and review date.

## Epic 4: Adopter Integration and Developer Readiness

Developer adopters can integrate through published contracts, a .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, and CORE precondition documentation.

### Story 4.1: Publish Conversations Contract Package and Compatibility Metadata

As an adopter developer,
I want a published contract package with version and compatibility metadata,
So that I can integrate against stable Conversations commands, projections, events, and typed errors.

**Requirements Covered:** FR70, FR75.

**Acceptance Criteria:**

**Given** the Conversations contract package is built
**When** package contents are inspected
**Then** it exposes commands, projections, domain events, typed errors, schema/version metadata, and compatibility status for the active contract version
**And** it excludes server infrastructure, EventStore envelopes, snapshot mechanics, stream internals, SignalR group names, and UI implementation details.

**Given** an adopter needs to discover compatibility
**When** they query version or compatibility metadata through the package or service contract
**Then** the response identifies active command, projection, event, and client package versions
**And** unsupported or deprecated versions are represented with machine-readable status and safe remediation pointers.

**Given** contract package validation runs
**When** serialization, nullable, dependency direction, schema version, and package inventory checks execute
**Then** public contracts remain serialization-friendly, infrastructure-free, centrally versioned, and documented enough for adopter use
**And** no forbidden Party personal data or provider payload fields are exposed.

**Given** compatibility tests run
**When** supported, deprecated, unsupported, additive, and malformed contract-version scenarios are exercised
**Then** tests prove discoverability, typed compatibility status, safe failure semantics, and no leakage of internal EventStore implementation.

### Story 4.2: Provide Supported .NET Client Happy Path

As an adopter developer,
I want a supported .NET client for the core create, append, and read workflow,
So that I can integrate Conversations without hand-coding raw HTTP or EventStore details.

**Requirements Covered:** FR71, FR72, FR74.

**Scope Note:** The supported v1 integration path is the .NET client. Raw HTTP fallback examples are omitted unless buyer approval is recorded or diagnostics explicitly require them.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Raw HTTP fallback approval is recorded before any raw HTTP fallback examples, parity tests, or fallback documentation are implemented.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** the .NET client is configured with tenant context, caller metadata, correlation metadata, and endpoint settings
**When** an adopter calls create conversation, append message, and read timeline methods
**Then** the client sends Conversations contract commands and queries using the supported v1 integration path
**And** it returns typed results, freshness metadata, and typed errors without exposing EventStore mechanics.

**Given** the adopter repeats a command after a timeout or unknown outcome
**When** the .NET client resubmits with the same idempotency metadata
**Then** it surfaces stable duplicate outcomes or idempotency conflicts consistently with server semantics
**And** it does not treat provider session IDs as durable conversation identity.

**Given** raw HTTP fallback is buyer-accepted or required for diagnostics
**When** fallback guidance is used
**Then** raw HTTP examples preserve the same tenant binding, idempotency, error, freshness, and schema-version behavior as the .NET client
**And** fallback guidance does not encourage bypassing the contract package.

**Given** client tests run
**When** happy path, timeout retry, unsupported schema, stale projection, tenant denial, sanitized error, and raw HTTP parity scenarios are exercised
**Then** tests prove the client maps requests and responses correctly, preserves typed errors, and remains tenant-safe.

### Story 4.3: Expose Typed Sanitized Errors and Remediation Guidance

As an adopter developer,
I want typed sanitized errors with actionable remediation guidance,
So that I can handle failures safely without exposing protected conversation data.

**Requirements Covered:** FR78, FR80.

**Acceptance Criteria:**

**Given** a command, query, client call, or compatibility check fails
**When** the error response is created
**Then** it includes machine-readable code, category, retryability, client action, safe message, correlation ID, audit handle where allowed, and documentation pointer
**And** it excludes target tenant identifiers, inaccessible Party IDs, conversation existence, redacted content, provider payloads, raw business references, and protected operational details.

**Given** failures are caused by unsupported schemas, missing preconditions, failed verification, tenant binding, stale projection, audit unavailability, provider configuration gaps, or projection subscription failure
**When** remediation guidance is returned
**Then** the guidance identifies the failure class and next safe action without leaking protected details
**And** machine-readable codes allow adopter applications to branch predictably.

**Given** the same failure can occur through REST, .NET client, or conformance tooling
**When** the failure is surfaced
**Then** typed error semantics remain consistent across integration paths
**And** documentation examples use the same codes and safe message shape.

**Given** error tests run
**When** invalid command, unauthorized access, nonexistent or cross-tenant record, unsupported version, stale projection, audit unavailable, provider configuration gap, and onboarding failure scenarios are exercised
**Then** tests prove typed semantics, remediation mapping, content-safe responses, and no leakage through logs, traces, diagnostics, or client exceptions.

### Story 4.4: Define CORE Preconditions and Onboarding Diagnostics

As an adopter developer,
I want explicit CORE preconditions and onboarding diagnostics,
So that I can know whether my environment is ready before relying on Conversations behavior.

**Requirements Covered:** FR77, FR79.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Shared trust/freshness vocabulary is approved before CORE preconditions or onboarding diagnostics expose freshness, degraded, unavailable, hidden, or unknown states.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** an adopter prepares an integration
**When** they review CORE preconditions
**Then** documentation identifies required tenant projection freshness, audit sink availability, supported schema versions, contract compatibility, Party identity validation, idempotency key behavior, projection subscription health, and required configuration
**And** each precondition explains the safe failure behavior when unmet.

**Given** onboarding diagnostics run
**When** tenant context, contract version, provider configuration, projection subscription, schema compatibility, audit availability, and Parties integration checks are evaluated
**Then** diagnostics return actionable status with machine-readable codes, safe messages, and remediation pointers
**And** checks do not leak tenant data, Party data, conversation existence, provider payloads, or production secrets.

**Given** a CORE precondition is unknown, failing, stale, or unsupported
**When** an adopter attempts a dependent command or query
**Then** the system returns a typed safe precondition failure or degraded-read result as defined by policy
**And** it does not silently continue in a mode that weakens tenant isolation, audit pairing, freshness, or schema compatibility.

**Given** diagnostic tests run
**When** ready, missing tenant context, stale tenant projection, audit sink unavailable, unsupported contract, missing provider config, projection subscription failure, and schema incompatibility scenarios are exercised
**Then** tests prove accurate readiness signals, safe remediation guidance, and content-safe diagnostic output.

### Story 4.5: Provide Adopter-Facing Conformance Tests and CORE Fixture

As an adopter developer,
I want adopter-facing conformance tests and a representative CORE fixture,
So that I can prove my integration respects Conversations contracts before deployment.

**Requirements Covered:** FR73, FR74.

**Acceptance Criteria:**

**Given** an adopter installs or references the conformance test package
**When** they run the adopter-facing test suite
**Then** tests cover create conversation, append message, read timeline, tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, governance preconditions, and compatibility discovery
**And** results are machine-readable and suitable for CI use.

**Given** the CORE fixture is loaded
**When** contract tests execute against it
**Then** the fixture includes at least one tenant-scoped conversation happy path with participants, message attribution, business references, projection freshness, and typed failure cases
**And** fixture data is synthetic, content-safe, and does not require production tenant data or provider credentials.

**Given** a conformance test fails
**When** results are reported
**Then** the failure maps to the relevant requirement, precondition, or release-gate category
**And** output distinguishes product invariant failures from infrastructure, configuration, unavailable dependency, and execution failures.

**Given** conformance tests run in CI
**When** supported, unsupported, stale, cross-tenant, duplicate command, projection lag, and sanitized error scenarios are exercised
**Then** tests prove adopter-readiness, traceable failures, safe output, and no dependency on nested submodule initialization.

**Evidence Note:** This story must produce minimum local evidence for story closure. Release-gate contract validation and CORE fixture evidence are carried forward into Story 5.10 for manifest aggregation and signing.

### Story 4.6: Capture Caller Metadata for Attribution, Audit, and Composition

As an adopter system,
I want to pass caller-supplied client, composer, and origin metadata safely,
So that attribution, audit, downstream projection, and FrontComposer surfaces can preserve useful provenance.

**Requirements Covered:** FR76.

**Acceptance Criteria:**

**Given** an adopter submits commands or queries
**When** caller metadata such as client name, client version, composer source, origin, correlation ID, causation ID, and integration context is supplied
**Then** the system validates, bounds, and stores or forwards only approved metadata fields
**And** tenant identity, user identity, tokens, claims, provider payloads, raw prompts, and protected content are not accepted as user-editable metadata fields.

**Given** metadata is used for attribution, audit, projections, or FrontComposer composition
**When** it is rendered or published
**Then** metadata remains provenance data and does not become authorization, tenant truth, governance truth, or UI-inferred trust state
**And** every displayed trust claim still maps to projection or command availability metadata.

**Given** metadata is malformed, oversized, unbounded, sensitive, or unsupported
**When** a command or query boundary validates it
**Then** the system rejects, truncates by approved policy, or omits the metadata with typed safe diagnostics
**And** no logs, traces, metrics, events, or projections include unsafe values.

**Given** metadata tests run
**When** valid metadata, oversized metadata, token-like values, tenant spoofing attempts, unbounded business identifiers, FrontComposer composition metadata, and publication scenarios are exercised
**Then** tests prove safe validation, bounded telemetry, attribution usefulness, and no trust or authorization inference from caller-supplied values.

### Story 4.7: Publish Developer Integration Guide and API Examples

As an adopter developer,
I want concise integration guidance and examples,
So that I can use Conversations correctly without reverse-engineering architecture decisions.

**Requirements Covered:** FR74, FR78, FR79.

**Acceptance Criteria:**

**Given** developer documentation is published
**When** an adopter reads the integration guide
**Then** it explains Conversations responsibilities versus chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities
**And** it documents tenant binding, Party identity, idempotency, typed errors, projection freshness, event publication, governance behavior, compatibility discovery, and CORE preconditions.

**Given** examples are provided
**When** an adopter follows them
**Then** examples cover .NET client setup, create conversation, append message, read timeline, handle typed errors, retry idempotently, inspect freshness, discover compatibility, and run conformance tests
**And** examples avoid raw EventStore mechanics and unsafe provider-session identity assumptions.

**Given** documentation references operational or governance behavior
**When** guidance describes failure modes
**Then** it explains content-safe responses, audit handles where allowed, degraded reads, stale projections, unsupported schemas, and remediation paths
**And** it does not expose sensitive policy internals or suggest bypassing fail-closed gates.

**Given** documentation checks run
**When** links, examples, contract names, error codes, version metadata, and conformance commands are validated
**Then** docs remain aligned with the package and client contracts
**And** stale or unsafe examples fail validation.

## Epic 5: Conformance, Compatibility, and Release Evidence

Platform owners can publish compatibility policy, run release-gating conformance, manage waivers, trace tests to requirements, prove portability/schema evolution, and distinguish module evidence from platform evidence.

**Story Generation Guardrail:** Epic 5 stories must preserve release-owner and platform-owner value framing. Do not rewrite them as generic technical tasks such as "write tests"; generated story files must keep the actor, evidence outcome, decision consequence, and requirement traceability.

### Story 5.1: Publish Contract Compatibility and Deprecation Policy

As a platform owner,
I want a compatibility policy for Conversations contracts,
So that adopters understand additive changes, breaking changes, deprecation windows, and minimum supported versions.

**Requirements Covered:** FR81.

**Acceptance Criteria:**

**Given** the compatibility policy is published
**When** adopters inspect command, projection, event, error, and client package version guidance
**Then** the policy identifies additive-change rules, breaking-change rules, deprecation windows, minimum supported contract versions, unsupported-version behavior, and remediation expectations
**And** the policy distinguishes persisted event compatibility from published event, projection, command, and client compatibility.

**Given** a contract changes
**When** release evidence is generated
**Then** the change is classified as additive, breaking, deprecated, unsupported, or waiver-dependent
**And** unsupported behavior maps to typed documented errors and compatibility diagnostics.

**Given** compatibility policy tests or checks run
**When** supported, deprecated, additive, breaking, unsupported, and minimum-version scenarios are exercised
**Then** checks prove policy traceability, safe diagnostics, and alignment with contract package metadata.

### Story 5.2: Generate Signed Release Conformance Artifact

As a release owner,
I want each release to produce a signed conformance artifact,
So that release decisions have durable evidence rather than informal test claims.

**Requirements Covered:** FR82, FR86.

**Acceptance Criteria:**

**Given** a release candidate is evaluated
**When** the conformance artifact is generated
**Then** it includes build hash, schema/event versions, contract package versions, test environment identity, dataset scale, tool versions, timestamped evidence links, pass/fail/waiver status, signer or runner identity, and release manifest reference
**And** the artifact is machine-readable and content-safe.

**Given** release-gated checks complete
**When** results are summarized
**Then** tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, contract compatibility, and provider portability are classified as pass, fail, waived, or unknown-accepted
**And** automatic blockers remain blockers unless the named-waiver process explicitly applies.

**Given** artifact validation runs
**When** required evidence is missing, unsigned, stale, contradictory, or content-unsafe
**Then** validation fails with typed diagnostics
**And** unsafe evidence does not get published as release-ready.

### Story 5.3: Maintain Versioned Conformance Manifest with Traceability

As a release owner,
I want a versioned release-specific conformance manifest,
So that every release-gate test maps to requirements and acceptance criteria.

**Requirements Covered:** FR83, FR84.

**Acceptance Criteria:**

**Given** a release manifest is created
**When** conformance tests are registered
**Then** each test maps to functional requirements, non-functional requirements, carry-forward commitments, release-gate status, pass criteria, waiver status, measurement method, environment, and evidence artifact
**And** every FR and release-blocking NFR in scope has at least one traceable verification entry.

**Given** a conformance test changes
**When** the manifest is updated
**Then** version history preserves what changed, why, and which requirement or release gate is affected
**And** stale mappings or orphan tests are flagged.

**Given** manifest validation runs
**When** duplicate test IDs, missing FR mappings, missing pass criteria, missing waiver metadata, or untraceable evidence appears
**Then** validation fails with actionable diagnostics
**And** release evidence remains navigable by non-developer approvers.

**Given** a manifest entry represents a release gate or evidence obligation
**When** the entry is authored or validated
**Then** it includes requirement ID, gate status, evidence artifact, owner, lifecycle stage, release decision status, and waiver reference when applicable
**And** decorative evidence without requirement traceability or release-decision meaning is rejected.

### Story 5.4: Support Named Waivers for Release-Gate Exceptions

As a release approver,
I want a named-waiver process for release-gate exceptions,
So that accepted risks are explicit, owned, time-bound, and visible to buyers where needed.

**Requirements Covered:** FR85, FR86.

**Acceptance Criteria:**

**Given** a release gate is not green
**When** a waiver is requested
**Then** the waiver records owner, approver, affected requirement or gate, affected stories, risk, compensating control, expiry date, buyer impact, buyer acceptance status where customer-facing, evidence links, and review date
**And** automatic release blockers cannot be waived without explicit named approval.

**Given** a waiver is active, expired, rejected, or superseded
**When** release evidence is generated
**Then** the conformance artifact and admin evidence views distinguish pass, fail, waived, unknown-accepted, expired waiver, and blocker states
**And** stale or unexplained waivers are treated as findings.

**Given** waiver tests run
**When** active waiver, expired waiver, missing approver, missing compensating control, blocker waiver, buyer-facing waiver, and waiver review scenarios are exercised
**Then** tests prove governance traceability, release decision clarity, and content-safe evidence output.

### Story 5.5: Verify Tenant Isolation Conformance

As a platform owner,
I want release-gating tenant isolation conformance,
So that cross-tenant access is impossible by construction and tested adversarially before release.

**Requirements Covered:** FR87.

**Acceptance Criteria:**

**Given** the conformance suite runs tenant isolation tests
**When** positive and adversarial cases execute
**Then** it covers authorized access, cross-tenant ID guessing, stale tenant projection, unavailable tenant projection, disabled or deleted tenants, mixed-tenant rebuild attempts, poisoned projection events, malformed metadata, query enumeration, diagnostics, export, and admin or tool access
**And** any tenant isolation failure is an automatic release blocker unless explicitly waived through the named process.

**Given** tenant isolation evidence is generated
**When** conformance results are written to the release manifest
**Then** evidence identifies covered scenarios, pass criteria, blocking failures, waiver status, environment metadata, and content-safe diagnostics
**And** it does not expose conversation content, inaccessible tenant identity, Party personal data, provider payloads, or cross-tenant business references.

### Story 5.6: Verify Idempotent Command Conformance

As a platform owner,
I want release-gating idempotent command conformance,
So that duplicate or retried commands produce stable outcomes without duplicate business effects.

**Requirements Covered:** FR88.

**Acceptance Criteria:**

**Given** the conformance suite runs idempotency tests
**When** duplicate equivalent commands, duplicate non-equivalent commands, reordered delivery, unknown client outcome retry, replayed delivery, and tenant-mismatched key reuse execute
**Then** it proves stable outcomes, conflict rejection, no duplicate business effects, no projection divergence, and content-safe diagnostics.

**Given** idempotency evidence is generated
**When** conformance results are written to the release manifest
**Then** evidence maps command behavior to the approved idempotency semantics, failure categories, retry guidance, and release-gate status
**And** duplicate handling never depends on revealing protected tenant, Party, provider, or conversation data.

### Story 5.7: Verify Redaction Replay Conformance

As a platform owner,
I want release-gating redaction replay conformance,
So that redacted content never reappears through projections, logs, traces, errors, exports, accessibility output, clipboard payloads, caches, or derived indexes.

**Requirements Covered:** FR89.

**Acceptance Criteria:**

**Given** the conformance suite runs redaction replay tests
**When** projections, temporal views, logs, traces, errors, exports, accessibility output, clipboard payloads, caches, screenshots, telemetry, and derived indexes are checked
**Then** redacted content does not reappear
**And** audit evidence remains citeable without exposing redacted values.

**Given** redaction replay evidence is generated
**When** conformance results are written to the release manifest
**Then** evidence identifies covered disclosure surfaces, redaction policy basis, replay scope, pass/fail status, waiver status, and content-safe diagnostics
**And** it distinguishes redaction non-disclosure failures from infrastructure or test execution failures.

### Story 5.8: Prove Provider Portability

As a platform owner,
I want provider portability proof,
So that conversation history remains recoverable without provider-owned session authority.

**Requirements Covered:** FR90.

**Acceptance Criteria:**

**Given** provider portability verification runs
**When** provider-owned correlation identifiers are stripped, changed, unavailable, migrated, duplicated, or inconsistent
**Then** conversation history remains recoverable from Conversations identity, stable references, and EventStore history
**And** provider IDs remain correlation metadata rather than durable source-of-truth identity.

**Given** portability verification covers contract-level behavior
**When** persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence are evaluated
**Then** tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism remain invariant across provider configuration differences.

**Given** provider portability evidence is generated
**When** conformance results are written to the release manifest
**Then** evidence maps portability outcomes to release-gate status, blocking versus waiverable classification, evidence retention location, approving ADR or waiver reference, and affected requirements
**And** unsupported provider assumptions are recorded as findings or named waivers with owner, expiry, compensating control, and buyer impact.

**Given** provider portability release-gate automation runs
**When** unit, integration, contract, security, performance or load, and operational evidence classes are applicable to the release scope
**Then** the minimum automated evidence set is recorded in the manifest
**And** missing required evidence blocks gate closure unless an approved named waiver exists.

### Story 5.9: Prove Event Schema Evolution

As a platform owner,
I want event schema evolution proof,
So that persisted and published conversation events can evolve safely across supported contract versions.

**Requirements Covered:** FR91.

**Ready for Dev Preconditions:**

- EventStore envelope ownership and evolution are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- The approving ADR or waiver reference is available before schema evolution release-gate automation is accepted as complete.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** event schema evolution verification runs
**When** old event versions, mixed-version streams, unsupported versions, and at least one worked additive-change example are processed
**Then** supported versions replay through documented compatibility behavior
**And** unsupported versions fail with typed documented errors.

**Given** release evidence is generated
**When** schema evolution checks complete
**Then** evidence maps compatibility outcomes to the conformance manifest with blocking versus waiverable classification, evidence retention location, approving ADR or waiver reference, and affected requirements
**And** unsupported or missing-version behavior is flagged as a release-gate failure unless explicitly waived.

**Given** schema evolution release-gate automation runs
**When** unit, integration, contract, replay, projection rebuild, security, and performance or load evidence classes are applicable to the release scope
**Then** the minimum automated evidence set is recorded in the manifest
**And** missing required evidence blocks gate closure unless an approved named waiver exists.

### Story 5.10: Validate Commands, Queries, Events, Errors, and Version Discovery

As a release owner,
I want executable contract tests for all adopter-facing surfaces,
So that command, query, event, error, and version-discovery contracts are release-ready.

**Requirements Covered:** FR92, FR93.

**Acceptance Criteria:**

**Given** executable contract tests run before v1 release
**When** commands, queries/projections, emitted events, typed errors, version discovery, and compatibility status are validated
**Then** each surface matches the published contract package and documentation
**And** no test requires adopter knowledge of EventStore internals.

**Given** consumer-driven contract tests run
**When** redaction command/event/audit behavior and .NET client compatibility are validated for Stories 2.4 and 4.2
**Then** commands, emitted events, typed errors, audit handles, freshness metadata, idempotency outcomes, and compatibility status remain stable for adopters
**And** test failures identify whether the break is command, event, audit, client, versioning, or documentation behavior.

**Given** adopter-style CORE fixtures are used
**When** create, append, read, freshness, tenant denial, idempotency, and typed error paths are exercised
**Then** tests prove realistic adopter behavior and safe precondition handling
**And** fixture data is synthetic and tenant-safe.

**Given** project conformance invariants are validated
**When** EventStore authority, Tenants fail-closed access, Parties-owned personal data, and FrontComposer generated-first boundaries are checked
**Then** each invariant has traceable automated evidence or an approved waiver in the manifest
**And** boundary drift is treated as a release-gate failure rather than a documentation issue.

**Given** contract validation fails
**When** differences are reported
**Then** failures identify affected contract surface, version, requirement mapping, expected behavior, actual behavior, and remediation path
**And** diagnostics remain content-safe.

### Story 5.11: Separate Module-Level Evidence from Platform Controls

As a buyer evaluator,
I want release evidence to distinguish Conversations controls from inherited Hexalith platform controls,
So that acceptance decisions are clear and not overstated.

**Requirements Covered:** FR94.

**Acceptance Criteria:**

**Given** module-level compliance evidence is generated
**When** evidence is summarized
**Then** it identifies which controls are implemented and verified by Hexalith.Conversations and which are inherited from EventStore, Tenants, Parties, FrontComposer, Dapr, Aspire, or other platform components
**And** inherited controls include source, version, evidence link, and scope limitation where available.

**Given** a release gate depends on inherited control evidence
**When** inherited evidence is missing, stale, incompatible, or outside scope
**Then** the Conversations release evidence marks the dependency as blocked, unknown-accepted, waived, or not applicable according to policy
**And** it does not claim module-level proof for controls that belong elsewhere.

**Given** evidence views are rendered for non-developer approvers
**When** they inspect release status
**Then** views summarize pass/fail status, blocker reason, scope, timestamp, signer, waiver status, and linked machine-readable verification output
**And** raw logs or unsafe payloads are not required to understand the decision.

## Epic 6: Operations, Observability, and Lifecycle Commitments

Operators and product owners can observe tenant-safe operational health, conformance outcomes, privileged access attempts, and release-scope/lifecycle commitments without leaking protected conversation data.

**Story Generation Guardrail:** Epic 6 stories must preserve operator, SRE, product-owner, and release-lifecycle value framing. Do not rewrite them as generic technical tasks such as "add metrics"; generated story files must keep the actor, operational outcome, decision consequence, and requirement traceability.

### Story 6.1: Observe Command Rejections and Tenant Isolation Denials Safely

As an operator,
I want to observe command rejection counts, tenant isolation denials, and privileged access attempts by safe reason,
So that I can detect problems without exposing conversation content or protected tenant data.

**Requirements Covered:** FR95, FR98.

**Acceptance Criteria:**

**Given** commands are rejected for validation, authorization, tenant binding, unsupported schema, idempotency conflict, stale projection, audit unavailable, or policy reasons
**When** observability signals are emitted
**Then** metrics, logs, traces, and dashboards classify rejection reason with bounded cardinality
**And** they exclude conversation content, conversation IDs where not approved, Party personal data, provider payloads, raw business identifiers, redacted content, and inaccessible tenant details.

**Given** tenant isolation denials or privileged access attempts occur
**When** operator signals are inspected
**Then** signals identify safe reason class, operation class, retryability, correlation metadata, and escalation path
**And** they do not reveal target tenant, inaccessible Party, protected conversation existence, or cross-tenant business references.

**Given** observability tests run
**When** rejection, denial, privileged access, cross-tenant guessing, malformed metadata, and redaction cases are exercised
**Then** tests prove signal usefulness, bounded labels, content-safe output, and no leakage through logs, traces, metrics, or diagnostics.

### Story 6.2: Observe Projection Lag, Rebuild, Availability, and Publication Failures

As an operator,
I want to observe projection freshness and publication health safely,
So that I can respond to stale reads, rebuilds, and subscriber issues without inspecting protected content.

**Requirements Covered:** FR96, FR97.

**Ready for Dev Preconditions:**

- Projection freshness blocking semantics and shared trust/freshness vocabulary are recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Telemetry redaction/cardinality ownership is recorded before projection freshness, publication health, or subscriber diagnostics signals are finalized.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** projections are current, stale, rebuilding, unavailable, replaying, partially rebuilt, or hidden by tenant isolation
**When** observability signals are emitted
**Then** operators can see freshness state, lag class, rebuild state, availability state, last safe checkpoint where allowed, and recommended next action
**And** signals remain tenant-safe and content-safe by default.

**Given** event publication or subscriber-facing contract issues occur
**When** signals are emitted
**Then** the system classifies publication failure, dead-letter, retry, unsupported subscriber contract, and replay status without exposing event payloads or protected metadata
**And** subscriber diagnostics remain bounded-cardinality and safe for incident workflows.

**Given** projection and publication observability tests run
**When** lag breach, rebuild crash/resume, unavailable projection store, dead-letter replay, duplicate publication, unsupported subscriber version, and tenant-hidden projection scenarios are exercised
**Then** tests prove actionable signals, safe failure classification, and absence of content or cross-tenant leakage.

### Story 6.3: Surface Conformance and Verification Status for Incidents and CI

As an operator,
I want conformance outcomes and verification status in operational views,
So that release gates and incidents can use the same trustworthy evidence.

**Requirements Covered:** FR99.

**Acceptance Criteria:**

**Given** conformance verification runs in CI, release, or incident workflows
**When** status is published
**Then** operators can observe pass, fail, waived, unknown-accepted, infrastructure failure, stale evidence, and execution failure states
**And** each status links to safe machine-readable evidence where authorized.

**Given** verification status affects an incident or release decision
**When** operators inspect the status
**Then** the view identifies affected requirement, gate, scope, timestamp, runner or signer, blocker class, waiver status, and recommended next action
**And** it distinguishes product invariant failures from infrastructure or data availability failures.

**Given** conformance status tests run
**When** passing, failing, waived, expired-waiver, stale-evidence, infrastructure-failure, unauthorized-detail, and incident-link scenarios are exercised
**Then** tests prove operational usefulness, tenant safety, release-gate traceability, and content-safe evidence linking.

### Story 6.4: Classify Release Scope and Deferred Capability Consequences

As a product owner,
I want to identify which capabilities are v1, v1.1, vNext, deferred, waived, or conditional,
So that release scope and substrate-defining consequences are explicit.

**Requirements Covered:** FR100, FR101.

**Acceptance Criteria:**

**Given** release scope is defined
**When** capabilities are classified
**Then** each capability maps to v1, v1.1, vNext, deferred, waived, conditional, or explicitly out of scope
**And** each classification links to affected requirements, release gates, dependencies, owner, and review date where applicable.

**Given** a substrate-defining capability is deferred
**When** release scope is reviewed
**Then** the system exposes consequences for tenant isolation, audit pairing, idempotency, schema evolution, projection freshness, redaction replay, provider portability, or adopter compatibility
**And** consequences cannot be hidden behind generic deferred labels.

**Given** scope classification tests or validations run
**When** missing classification, contradictory classification, deferred substrate capability, expired conditional scope, and waived capability scenarios are exercised
**Then** validation flags incomplete or unsafe scope decisions before release evidence is accepted.

### Story 6.5: Support Buyer Partial Acceptance and Waiver Review

As a product owner,
I want to support buyer partial acceptance under the Option A v1 deal,
So that accepted scope, known gaps, and compensating controls are visible and reviewable.

**Requirements Covered:** FR102.

**Acceptance Criteria:**

**Given** a buyer partially accepts a release
**When** acceptance evidence is recorded
**Then** it identifies accepted capabilities, excluded capabilities, active waivers, unknown-accepted items, compensating controls, owners, expiry dates, buyer acknowledgement, and review milestones
**And** it links to signed conformance artifacts and release manifests.

**Given** a partial acceptance item affects a release blocker, substrate capability, or customer-facing behavior
**When** the acceptance record is created or reviewed
**Then** the system requires explicit named approval and buyer-visible rationale where appropriate
**And** the item is highlighted in evidence views for non-developer approvers.

**Given** partial acceptance tests run
**When** accepted, rejected, expired, missing buyer acknowledgement, blocker waiver, compensating control, and review-due scenarios are exercised
**Then** tests prove traceability, reviewability, safe evidence output, and no silent acceptance of release-blocking unknowns.

**Given** a partial acceptance record references a waiver, unknown-accepted item, or deferred substrate capability
**When** product owners review acceptance status
**Then** the record links directly to waiver entries, conformance manifest rows, affected stories, and release-scope consequence statements
**And** missing links block acceptance evidence from being marked complete.

### Story 6.6: Track Second-Adopter Status and Downgrade-Rule Milestones

As a product owner,
I want to track second-adopter status and downgrade-rule review milestones,
So that release commitments adjust deliberately as adoption broadens.

**Requirements Covered:** FR103.

**Acceptance Criteria:**

**Given** adopter status changes
**When** a second adopter is identified, qualified, deferred, or disqualified
**Then** the product record updates second-adopter status, affected requirements, review owner, milestone date, and downgrade-rule review trigger
**And** status changes are auditable and content-safe.

**Given** a downgrade-rule review milestone is reached
**When** product owners inspect lifecycle commitments
**Then** the system identifies which v1, v1.1, vNext, deferred, waived, or conditional capabilities require review
**And** it links to relevant conformance evidence, buyer acceptance records, and compatibility policy.

**Given** lifecycle tracking tests run
**When** second adopter added, milestone overdue, status reverted, waiver expired, and capability review scenarios are exercised
**Then** tests prove status traceability, safe audit output, and correct milestone triggers.

### Story 6.7: Publish Responsibility Boundary Documentation

As an adopter or buyer evaluator,
I want clear responsibility boundary documentation,
So that I understand what Conversations owns and what remains with adjacent systems.

**Requirements Covered:** FR104.

**Acceptance Criteria:**

**Given** responsibility documentation is published
**When** readers inspect module boundaries
**Then** it distinguishes Conversations responsibilities from chatbot behavior, LLM provider sessions, legal-hold authority, attachment storage, identity, tenant lifecycle, project/folder lifecycle, upstream Party data, provider availability, and broader Hexalith platform controls
**And** it names inherited controls where applicable.

**Given** a boundary has operational, compliance, or evidence consequences
**When** documentation describes the boundary
**Then** it identifies owner, source of truth, failure semantics, evidence obligation, and handoff path
**And** it does not imply Conversations owns data or authority delegated to EventStore, Tenants, Parties, Folders, FrontComposer, or provider systems.

**Given** responsibility docs validation runs
**When** links, owner names, inherited controls, handoff targets, and requirement mappings are checked
**Then** docs remain aligned with PRD, architecture, conformance manifest, and public developer guidance
**And** stale or contradictory ownership claims are flagged.

### Story 6.8A: Validate Operational Telemetry Redaction

As an SRE,
I want Conversations operational telemetry to redact unsafe values,
So that incidents can be diagnosed without exposing conversation content, tenant boundaries, provider payloads, or protected identifiers.

**Requirements Covered:** FR95-FR99 validation support; NFR55-NFR61.

**Scope Note:** This story validates telemetry redaction across operational signals. Telemetry cardinality and bounded dimensions are covered by Story 6.8B. Primary observability implementation remains in Stories 6.1-6.3 unless explicitly reassigned.

**Ready for Dev Preconditions:**

- Story 6.8 assignment plan is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Approved telemetry redaction rules, fixture set, evidence output, pass/fail gate, owner, and review date are recorded before validation starts.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** metrics, logs, traces, diagnostics, dashboards, and evidence summaries are emitted
**When** telemetry redaction validation runs
**Then** outputs exclude conversation content, user free text, raw business record identifiers, prompt/content fragments, unbounded error strings, provider payloads, redacted content, unauthorized identifiers, inaccessible conversation existence, and cross-tenant Party details
**And** tenant ID appears only when approved by privacy or governance policy for that surface.

**Given** an operational signal needs correlation
**When** correlation metadata is included
**Then** it uses approved bounded identifiers, audit handles, release evidence handles, or incident-safe correlation handles
**And** it does not include raw conversation IDs, Party IDs, provider IDs, file IDs, or business references unless explicitly approved for that surface.

**Given** telemetry redaction tests run
**When** normal operations, redaction events, cross-tenant denials, provider errors, malformed metadata, privileged access, stale projection, and audit unavailable scenarios are exercised
**Then** tests prove unsafe values are redacted from telemetry and evidence summaries
**And** failures identify the surface, forbidden value class, fixture, owner, and blocking/non-blocking classification.

### Story 6.8B: Validate Operational Telemetry Cardinality Gates

As an SRE,
I want Conversations telemetry dimensions to stay bounded and approved,
So that observability remains useful without creating cardinality cost, alert noise, or privacy risk.

**Requirements Covered:** FR95-FR99 validation support; NFR55-NFR61.

**Scope Note:** This story validates telemetry dimension cardinality and approval gates. Unsafe-value redaction is covered by Story 6.8A. Primary observability implementation remains in Stories 6.1-6.3 unless explicitly reassigned.

**Ready for Dev Preconditions:**

- Story 6.8 assignment plan is recorded in `_bmad-output/implementation-artifacts/readiness-gates.md` with state `decided` or `waived`.
- Approved telemetry dimensions, maximum cardinality expectations, fixture set, evidence output, pass/fail gate, owner, and review date are recorded before validation starts.
- Any waiver names owner, approver, expiry, compensating control, buyer impact, and review date.

**Acceptance Criteria:**

**Given** metrics, logs, traces, diagnostics, dashboards, alerts, and evidence summaries are emitted
**When** telemetry cardinality validation runs
**Then** dimensions are bounded and approved
**And** raw conversation IDs, Party IDs, provider IDs, file IDs, business references, prompt fragments, content fragments, raw error strings, and unbounded external identifiers are rejected as telemetry dimensions unless explicitly approved for a named surface.

**Given** high-cardinality and malformed operational inputs are processed
**When** telemetry is emitted for normal operations, duplicate commands, projection lag, rebuild states, subscriber failures, redaction events, cross-tenant denials, provider errors, privileged access, and configuration gaps
**Then** tests prove bounded cardinality, stable dimension names, approved value sets, useful incident diagnostics, and failure on unsafe or unapproved dimensions.

**Given** dashboards, alert rules, or release evidence consume telemetry dimensions
**When** dimension approvals change or a new operational signal is added
**Then** the evidence records owner, approved dimension set, pass/fail gate, affected stories, and review date
**And** the story cannot close if telemetry creates uncontrolled cardinality, content leakage risk, or incident-noise amplification without an approved waiver.
