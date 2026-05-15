---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd: _bmad-output/planning-artifacts/prd.md
  architecture: _bmad-output/planning-artifacts/architecture.md
  epics: _bmad-output/planning-artifacts/epics.md
  ux: _bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-15
**Project:** Hexalith.Conversations

## Document Discovery

### PRD Files Found

**Whole Documents:**
- `prd.md` (153780 bytes, modified 2026-05-10 15:45:32)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `architecture.md` (80751 bytes, modified 2026-05-14 11:58:36)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `epics.md` (145907 bytes, modified 2026-05-15 08:19:04)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `ux-design-specification.md` (117645 bytes, modified 2026-05-13 19:47:52)

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document formats found.
- No required document type is missing.

## PRD Analysis

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

- Scope precedence: Project Scoping and Phased Development governs release timing; FRs/NFRs define the full capability contract, not automatic v1 scope.
- Canonical scope vocabulary must be used: Full Capability Contract, MVP / v1 Release Scope, Post-v1, Explicitly Out of Scope, and Open Question.
- Open questions must not be assumed closed by downstream planning.
- v1 CORE includes conversation aggregate, chatbot CORE commands, EventStore persistence with idempotency and pub/sub, chatbot read projections, fail-closed tenant isolation, sensitive-data classification/redaction policy, code-level governance enforcement, .NET client and contract package, read-only governance viewer, conformance suite, provider portability migration test, and semver/deprecation policy.
- Foundation Gates must be operationally treated as CI-passing required, named-waiver required to proceed without them, and explicit about blocking scope.
- Runtime Foundation Gates: tenant isolation conformance suite blocks all CORE commands; idempotency property test blocks AppendMessage; audit-write fail-closed behavior blocks governance commands.
- Test Foundation Gate: adopter-runnable conformance test pack blocks Diego journey validation.
- Migration Gate: schema evolution ADR, event envelope `schema_version`, and one worked additive-change example block v1.1 release readiness, not v1 runtime.
- Anti-scope for v1 includes branching/forked conversations, semantic memory, vector search, automatic summarization, chatbot UI/orchestration, LLM provider abstraction beyond correlation IDs, real-time collaborative editing/live streaming, multi-agent planning, attachment binary storage, full compliance automation, cryptographic redaction, multi-region failover, Roslyn analyzer, full upcasting framework, and Generate Evidence Bundle.
- Integration constraints require stable-ID indirection for Tenants, Parties, Projects, Folders, EventStore, FrontComposer, and LLM provider correlation metadata.
- Error semantics require typed, sanitized errors and must not leak target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references.
- Pre-kickoff buyer blockers include raw HTTP fallback decision, EventStore envelope stability, v1 event consumers, architect/second-engineer availability, named second adopter status, ratified Foundation Gate blocking definition, and whether `MarkSensitiveData` / `RedactMessageContent` are CORE.
- Documentation deliverables include a voice-register review checkpoint, contract package README with five-line happy path, generated API reference, integration guide, conformance test pack, and release/conformance evidence.
- Proposed carry-forward additions CF 56-58 must be resolved: .NET client + contract package as v1 CORE, adopter-runnable conformance test pack as Foundation Gate Test, and sensitive-data classification + redaction policy mechanism as single CORE artifact.

### PRD Completeness Assessment

The PRD is unusually complete in its explicit FR/NFR inventory, release-scope vocabulary, risk framing, and evidence expectations. Its main readiness risk is not missing product intent; it is unresolved pre-kickoff governance and delivery decisions that downstream epics must not smooth over: numeric capacity targets, Foundation Gate ratification, EventStore envelope ownership, raw HTTP fallback, second-adopter status, redaction command scope, and staffing/parallelization assumptions. The epic validation step should treat those as traceability blockers if they are absent from epics or turned into vague implementation notes.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- FR1: Epic 1 - Tenant-safe conversation record creation.
- FR2: Epic 1 - Stable tenant-scoped conversation identity.
- FR3: Epic 1 - Conversation lifecycle state and transitions.
- FR4: Epic 1 - Ordered message append.
- FR5: Epic 1 - Participant addition for humans, AI agents, and LLMs.
- FR6: Epic 1 - Idempotent command submission.
- FR7: Epic 1 - Typed command rejection semantics.
- FR8: Epic 1 - Conversation retrieval with timeline, participants, governance state, and freshness.
- FR9: Epic 1 - Tenant-scoped conversation listing by business context.
- FR10: Epic 1 - Release-scoped title or metadata updates.
- FR11: Epic 1 - Release-scoped close or archive behavior.
- FR12: Epic 1 - Conversation continuity across provider expiry, restart, or failover.
- FR13: Epic 1 - Stable Party attribution for actions.
- FR14: Epic 1 - Human, AI agent, and LLM participant modeling.
- FR15: Epic 1 - Provider correlation identifiers as metadata.
- FR16: Epic 1 - Versioned provider-specific extension data.
- FR17: Epic 1 - Multi-provider attribution.
- FR18: Epic 1 - Reconstruction of actor, action, time, and tenant context.
- FR19: Epic 1 - File references without binary storage.
- FR20: Epic 1 - Upstream business entity association.
- FR21: Epic 1 - External business identifiers for tenant-scoped discovery.
- FR22: Epic 1 - Distinction between external identifiers and business references.
- FR23: Epic 1 - Read-time upstream reference resolution.
- FR24: Epic 1 - Readability when upstream entities change lifecycle state.
- FR25: Epic 1 - Migration-boundary guidance for out-of-coverage records.
- FR26: Epic 1 - Tenant context for commands, events, projections, queries, pub/sub, and audit records.
- FR27: Epic 1 - Fail-closed tenant binding before aggregate or projection access.
- FR28: Epic 1 - Cross-tenant enumeration prevention.
- FR29: Epic 1 - Indistinguishable unauthorized, nonexistent, and cross-tenant records.
- FR30: Epic 1 - Typed tenant-isolation and tenant-binding errors.
- FR31: Epic 1 - Tenant audit attribution for operator actions affecting tenant data.
- FR32: Epic 1 - Tenant-aware publication without cross-tenant metadata leakage.
- FR33: Epic 1 - Projection derivation from ordered conversation events.
- FR34: Epic 1 - Read-model metadata for replay position, projection version, or freshness.
- FR35: Epic 1 - v1 projection rebuild equivalence.
- FR36: Epic 1 - Projection consistency and freshness semantics.
- FR37: Epic 1 - Projection lag or freshness behavior exposure.
- FR38: Epic 1 - Downstream domain event consumption.
- FR39: Epic 1 - Published event schema and version metadata.
- FR40: Epic 1 - Unsupported schema version rejection.
- FR41: Epic 1 - Compatible event, command, and projection evolution rules.
- FR42: Epic 2 - Retention policy setting or replacement with rationale.
- FR43: Epic 2 - Sensitive content marking.
- FR44: Epic 2 - Redaction with actor, timestamp, rationale, and policy attribution.
- FR45: Epic 2 - Distinction among archival, retention, redaction, legal hold, and audit history.
- FR46: Epic 2 - Audit stream preservation while redacting projections or display.
- FR47: Epic 2 - Paired audit event for each governance mutation.
- FR48: Epic 2 - Governance rejection when audit recording is unavailable.
- FR49: Epic 2 - Non-governance activity behavior during audit degradation.
- FR50: Epic 2 - Point-in-time message and governance reconstruction.
- FR51: Epic 2 - Citeable audit records.
- FR52: Epic 2 - Retention and redaction treatment for governance audit records.
- FR53: Epic 2 - Allowed and denied audit-record actions.
- FR54: Epic 2 - Structured justification for privileged tenant-data operations.
- FR55: Epic 2 - Coherent review of privileged-action justification and audit outcome.
- FR56: Epic 3 - Tenant-scoped search by external identifiers.
- FR57: Epic 3 - Search filtering by date range and business context.
- FR58: Epic 3 - Reconstructed transcript review with governance and freshness context.
- FR59: Epic 3 - Inline redaction attribution.
- FR60: Epic 3 - Inline governance audit trail.
- FR61: Epic 3 - Historical conversation state review.
- FR62: Epic 3 - Citation-ready transcript and audit references.
- FR63: Epic 3 - Stable temporal evidence links.
- FR64: Epic 3 - Read-only operator and compliance workflows.
- FR65: Epic 3 - Classification and separate audit for privileged operator mutations.
- FR66: Epic 3 - Governance verification execution.
- FR67: Epic 3 - Structured verification results.
- FR68: Epic 3 - Distinction between governance verification and infrastructure failures.
- FR69: Epic 3 - Self-serve buyer acceptance demo.
- FR70: Epic 4 - Published contract package for commands, projections, events, and typed errors.
- FR71: Epic 4 - Supported .NET client integration path.
- FR72: Epic 4 - Minimal create, append, and read happy path.
- FR73: Epic 4 - Adopter-facing conformance tests.
- FR74: Epic 4 - Documented tenant binding, Party identity, idempotency, errors, freshness, publication, and governance behavior.
- FR75: Epic 4 - Active contract version and compatibility discovery.
- FR76: Epic 4 - Caller-supplied metadata for attribution, audit, projections, and composition.
- FR77: Epic 4 - Onboarding diagnostics for missing CORE preconditions and configuration gaps.
- FR78: Epic 4 - Remediation guidance with machine-readable error codes.
- FR79: Epic 4 - Adopter-facing CORE preconditions.
- FR80: Epic 4 - Sanitized typed error responses with safe audit handle and documentation pointer.
- FR81: Epic 5 - Compatibility policy.
- FR82: Epic 5 - Signed conformance artifact.
- FR83: Epic 5 - Versioned release-specific conformance manifest.
- FR84: Epic 5 - Test-to-requirement traceability.
- FR85: Epic 5 - Named-waiver process.
- FR86: Epic 5 - Blocking and non-blocking release-gate failure classification.
- FR87: Epic 5 - Adversarial tenant-isolation verification.
- FR88: Epic 5 - Idempotent command verification.
- FR89: Epic 5 - Redaction-replay correctness verification.
- FR90: Epic 5 - Provider portability proof.
- FR91: Epic 5 - Event schema evolution proof.
- FR92: Epic 5 - Executable contract tests before v1 release.
- FR93: Epic 5 - Adopter-style CORE fixture.
- FR94: Epic 5 - Module-level versus platform compliance evidence.
- FR95: Epic 6 - Content-safe command rejection observability.
- FR96: Epic 6 - Content-safe projection lag, rebuild, and availability observability.
- FR97: Epic 6 - Content-safe publication failure and contract issue observability.
- FR98: Epic 6 - Content-safe tenant isolation denial and privileged access observability.
- FR99: Epic 6 - Conformance outcome and verification status observability.
- FR100: Epic 6 - Release capability scope classification.
- FR101: Epic 6 - Release-scope consequence exposure.
- FR102: Epic 6 - Buyer partial acceptance support.
- FR103: Epic 6 - Second-adopter status and downgrade-rule review milestones.
- FR104: Epic 6 - Responsibility boundary documentation.

Total FRs in epics: 104

### Coverage Matrix

| FR Range | PRD Requirements | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1-FR41 | Conversation lifecycle, participant attribution, business context, tenant access, event sourcing, projection, publication, and schema evolution requirements | Epic 1: Tenant-Safe Conversation Record; stories 1.1-1.11 | Covered |
| FR42-FR55 | Retention, sensitivity, redaction, audit pairing, point-in-time governance state, audit record governance, and privileged operational justification | Epic 2: Governed Retention, Redaction, and Audit; stories 2.1-2.8 | Covered |
| FR56-FR69 | Compliance search, governed transcript review, redaction attribution, audit trail, temporal evidence, read-only workflows, verification, and buyer demo | Epic 3: Compliance Investigation Workspace; stories 3.1-3.8 | Covered |
| FR70-FR80 | Contract package, .NET client, happy path, adopter conformance tests, compatibility discovery, metadata, onboarding diagnostics, remediation, and sanitized errors | Epic 4: Adopter Integration and Developer Readiness; stories 4.1-4.7 | Covered |
| FR81-FR94 | Compatibility policy, signed conformance artifact, manifest traceability, named waivers, invariant verification, provider portability, schema evolution, contract tests, and module/platform evidence split | Epic 5: Conformance, Compatibility, and Release Evidence; stories 5.1-5.8 | Covered |
| FR95-FR104 | Observability, conformance status, release scope classification, partial acceptance, second-adopter tracking, and responsibility documentation | Epic 6: Operations, Observability, and Lifecycle Commitments; stories 6.1-6.8 | Covered |

### Missing Requirements

No PRD FRs are missing from the epics coverage map. No extra epic FR numbers outside PRD FR1-FR104 were found.

### Coverage Statistics

- Total PRD FRs: 104
- FRs covered in epics: 104
- FRs missing from epics: 0
- FRs in epics but not in PRD: 0
- Coverage percentage: 100%

### Coverage Caveats For Later Steps

- Story 1.1 claims broad `FR1-FR41 foundation` coverage. This is acceptable for Step 3 numbering coverage, but later story-quality validation should ensure it does not blur implementation responsibilities across Epic 1.
- Some release-scoped or conditional PRD capabilities, especially FR10, FR11, FR31, FR38, FR41, FR65, FR100, and FR101, are covered by epics but will still need later validation against architecture, release scope, and acceptance criteria precision.
- FR coverage is complete by number; implementation readiness still depends on NFR coverage, UX alignment, architecture alignment, and story-level acceptance criteria quality.

## UX Alignment Assessment

### UX Document Status

Found: `_bmad-output/planning-artifacts/ux-design-specification.md` (117645 bytes, modified 2026-05-13 19:47:52).

### UX To PRD Alignment

- The UX defining experience, Find -> Read -> Trust, aligns directly with PRD journeys for Sarah, Diego, Marcus, Julian, Helen, and Daniel.
- UX requirements for tenant-scoped search, governed transcript review, inline redaction attribution, audit trail, citations, temporal evidence links, read-only investigation workflows, governance verification, and buyer demo align with PRD FR56-FR69 and Epic 3.
- UX requirements for developer trust, typed errors, onboarding diagnostics, CORE preconditions, and conformance tests align with PRD FR70-FR80 and Epic 4.
- UX requirements for signed evidence, waiver/blocker review, release evidence, and non-developer approver views align with PRD FR81-FR94 and Epic 5.
- UX requirements for content-safe observability, projection freshness, degraded states, trust transitions, and safe operational messages align with PRD FR95-FR104 and NFR44-NFR77.
- UX accessibility and responsive requirements align with PRD NFR69-NFR77 and are represented in Epic 3 Story 3.8.

### UX To Architecture Alignment

- Architecture supports the UX platform strategy: FrontComposer generates baseline admin surfaces, while custom trust-bearing components handle evidence timeline, trust posture, redaction, audit trail, citation copy, temporal navigation, projection freshness, and degraded states.
- Architecture explicitly states that the UI renders server-owned trust states and command availability rather than inferring trust client-side, matching the UX FrontComposer Contract and safety acceptance criteria.
- Architecture supports the UX leakage model by treating DOM text, hidden DOM, responsive duplicates, ARIA labels, live regions, tooltips, clipboard payloads, browser titles, telemetry, exports, logs, traces, and derived indexes as disclosure surfaces.
- Architecture supports responsive and accessibility needs at the principle level: WCAG 2.1 AA, keyboard/screen-reader parity, clipboard safety, responsive duplicate safety, and mobile safe-triage defaults are named.
- Architecture supports the UX performance posture by requiring ordinary open-conversation reads to use projection models rather than raw event streams, with temporal reconstruction, export, verification, and rebuild as bounded asynchronous workflows.
- Architecture maps admin UI responsibilities into `Admin/FrontComposer` and `Admin/TrustComponents`, which is consistent with the UX design-system foundation.

### Alignment Issues

- Temporal evidence anchor remains an open architecture question. UX depends on stable temporal links and time-travel evidence, so implementation of UX-DR temporal-link behavior should wait for the authoritative anchor decision: event position, projection version, timestamp, or composite.
- Projection freshness blocking semantics remain open. UX requires distinct current, stale, rebuilding, unavailable, forbidden, redacted, incomplete, and unknown states with deterministic precedence; architecture names the shared vocabulary but still asks which states block reliance versus warn.
- Redaction/delete/re-index behavior for future derived indexes is unresolved. UX requires redacted content to be absent from DOM, accessibility tree, clipboard, telemetry, exports, screenshots, hidden markup, and responsive duplicates; architecture supports the rule but still carries open questions about future derived indexes such as Memories.
- Retention, legal hold, deletion, tombstoning, exports, projection rebuild, and derived-index interaction is still open. UX surfaces must not imply evidence completeness or exportability until this policy boundary is settled.
- UX includes detailed responsive breakpoints, canonical fixtures, Leak Sentinel expectations, and accessibility test fixtures. Architecture supports the categories but should ensure these are converted into test assets or conformance checklist items before frontend implementation starts.

### Warnings

- No missing UX document warning is needed.
- The UX is architecture-aligned enough to proceed with planning, but trust-bearing UI stories should be blocked on ADRs or decisions for temporal anchor, freshness blocking states, redaction propagation, and disclosure-surface test fixtures.
- Generated FrontComposer-only UI is insufficient for the trust-bearing surfaces; architecture and epics both acknowledge custom-reviewed components are required.

## Epic Quality Review

### Epic Structure Validation

| Epic | User Value Focus | Independence | Quality Result |
| --- | --- | --- | --- |
| Epic 1: Tenant-Safe Conversation Record | Strong: adopter teams can create, append, retrieve, list, replay, and publish tenant-safe records. | Stands alone as the foundational conversation capability. | Pass with caveat on Story 1.1 traceability overclaim. |
| Epic 2: Governed Retention, Redaction, and Audit | Strong: authorized governance operators can apply and review governed changes. | Depends only on Epic 1 conversation record and command path, which is acceptable. | Pass. |
| Epic 3: Compliance Investigation Workspace | Strong: compliance operators can find, inspect, time-travel, cite, and verify governed evidence. | Depends on Epic 1 records and Epic 2 governance evidence, which is acceptable sequencing. | Pass with sizing concern for Story 3.8. |
| Epic 4: Adopter Integration and Developer Readiness | Strong: developer adopters can integrate through contracts, client, diagnostics, and conformance. | Can use outputs from Epics 1-3. No forward dependency found. | Pass. |
| Epic 5: Conformance, Compatibility, and Release Evidence | Strong for platform owner/release approver, not a technical milestone: signed evidence, waivers, compatibility, portability, schema proof. | Depends on earlier product surfaces, which is acceptable for a verification epic. | Pass with major story-sizing concerns. |
| Epic 6: Operations, Observability, and Lifecycle Commitments | Strong for operators/product owners: safe signals, status, lifecycle commitments, partial acceptance. | Depends on earlier product/evidence outputs and completes lifecycle/operations path. | Pass. |

### Critical Violations

No critical epic-level violations found. There are no purely technical epics masquerading as value epics, no circular epic dependencies, and no evidence that Epic N requires Epic N+1 to function.

### Major Issues

1. Story 1.1 overclaims FR coverage.
   - Evidence: Story 1.1 is a scaffold/setup story with `Requirements Covered: FR1-FR41 foundation; Architecture starter-template requirement.`
   - Why it matters: The acceptance criteria create project structure, dependency direction, smoke checks, and placeholder behavior. They do not actually deliver FR1-FR41. This can hide traceability gaps by treating scaffold presence as coverage for behavior.
   - Recommendation: Change Story 1.1 coverage to "architecture starter-template requirement and implementation foundation for FR1-FR41" rather than claiming FR1-FR41 coverage. Keep it as the required starter-template story.

2. Story 5.5 is too large for one independently completable story.
   - Evidence: Story 5.5 covers FR87-FR89 and requires tenant isolation verification, idempotency verification, and redaction replay verification.
   - Why it matters: These are three separate release-blocking conformance suites with different fixtures, generators, failure modes, and owners. Combining them risks producing a shallow test harness or an epic-sized story.
   - Recommendation: Split into separate stories: tenant isolation conformance, idempotency conformance, and redaction replay conformance.

3. Story 5.6 bundles two separate proof obligations.
   - Evidence: Story 5.6 covers FR90 and FR91: provider portability proof and event schema evolution proof.
   - Why it matters: Provider portability and schema evolution have different architecture decisions, fixtures, and acceptance evidence. They can be delivered independently.
   - Recommendation: Split into provider portability verification and event schema evolution verification.

4. Story 1.11 is broad enough to become an epic-sized foundation story.
   - Evidence: Story 1.11 covers replay, schema versioning, projection rebuild, unsupported versions, stale derived state, provider correlation changes, and release-evidence output.
   - Why it matters: Replay determinism, schema compatibility, and projection rebuild are all load-bearing. If kept together, story closure may be ambiguous.
   - Recommendation: Either split into replay determinism, projection rebuild/freshness, and schema version compatibility, or make acceptance criteria explicitly phased inside the story with separate test artifacts.

5. Early CI/release-evidence setup is not clearly placed in the first implementation slice.
   - Evidence: Story 1.1 includes restore/build/test smoke checks, but the signed CI artifact, manifest, and release evidence appear later in Epic 5.
   - Why it matters: The PRD makes signed conformance evidence and named-waiver mechanics foundational. If CI/evidence scaffolding arrives late, early stories may close without the gates that define readiness.
   - Recommendation: Add an early story or expand Story 1.1 to establish baseline CI workflow, test result artifact publication, and placeholder conformance manifest structure, without implementing full conformance suites.

6. Several stories depend on unresolved ADR decisions but do not state decision prerequisites.
   - Evidence: Temporal anchor, freshness blocking states, redaction/re-index behavior, and retention/legal-hold interactions are open in architecture while Stories 1.11, 2.6, 3.4, 3.8, and 5.6 require those semantics.
   - Why it matters: Stories may look implementable while still depending on decisions not yet made.
   - Recommendation: Add explicit "decision prerequisite" or "blocked until ADR" notes to affected stories, or create ADR stories before the implementation stories that depend on them.

### Minor Concerns

1. Some acceptance criteria are testable but dense.
   - Examples: Stories 3.8, 4.5, 5.2, 5.3, 6.8.
   - Recommendation: During story creation, keep each story's automated test artifact narrow and named.

2. Story ordering is generally valid but contract-first stories should avoid designing all future command shapes too early.
   - Example: Story 1.2 includes update-metadata and close/archive command shapes "where release-scoped."
   - Recommendation: Confirm release-scoped commands are represented as stubs/contracts only when needed, with no implied runtime behavior until the relevant story.

3. Story 3.8 covers support for FR56-FR69 plus UX-DR39-UX-DR52 plus NFR69-NFR77.
   - Recommendation: Treat it as an accessibility/responsive verification story for the workspace rather than a catch-all UX implementation story; split if implementation work is more than test hardening and fixture coverage.

### Dependency Analysis

- No forward epic dependencies found.
- Within-epic sequencing is mostly valid: foundation stories precede command behavior, command behavior precedes projections/read paths, governance contracts precede governance mutations, read-only workspace stories precede verification/demo, contract package precedes client/docs/conformance fixture, and compatibility policy precedes manifest/waiver/evidence stories.
- References to "future" work are mostly scope boundaries, not implementation dependencies. The main exception is unresolved ADR dependency language, which should be made explicit as prerequisites.
- No database/table upfront violation found. The epics explicitly avoid transcript tables and preserve EventStore as the write authority.

### Best Practices Compliance Checklist

| Check | Result |
| --- | --- |
| Epics deliver user value | Pass |
| Epics can function sequentially without forward dependencies | Pass |
| Stories appropriately sized | Mixed; several major sizing concerns |
| No forward dependencies | Pass with ADR prerequisite caveat |
| Database tables created only when needed | Pass; no table-first design found |
| Clear acceptance criteria | Mostly pass; dense but testable |
| Traceability to FRs maintained | Pass by number; Story 1.1 overclaims behavior coverage |

### Epic Quality Recommendation

The epic set is structurally sound enough to continue, but should not be considered implementation-ready until the major story-sizing and decision-prerequisite issues are corrected. The highest-priority fixes are: reduce Story 1.1 coverage overclaim, split Stories 5.5 and 5.6, clarify or split Story 1.11, add early CI/evidence scaffold placement, and mark ADR-dependent stories explicitly.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK.

The planning artifacts are strong enough to continue refinement, but they are not ready for Phase 4 implementation kickoff as-is. The project has complete document coverage, complete PRD FR extraction, and 100% FR-to-epic numeric coverage. The blockers are qualitative: major story-sizing issues, overclaimed scaffold traceability, unresolved ADR prerequisites, and UX trust decisions that must be made before trust-bearing implementation begins.

### Critical Issues Requiring Immediate Action

No critical violations were found in the narrow sense: no missing required documents, no duplicate document conflicts, no missing PRD FR coverage, no purely technical epics, and no forward epic dependency cycle.

The following major issues require action before implementation kickoff:

1. Correct Story 1.1 traceability. It is a valid starter-template story, but it should not claim behavioral coverage for FR1-FR41.
2. Split Story 5.5 into separate tenant isolation, idempotency, and redaction replay conformance stories.
3. Split Story 5.6 into provider portability verification and event schema evolution verification.
4. Split or explicitly phase Story 1.11 across replay determinism, projection rebuild/freshness, and schema compatibility.
5. Add early CI/evidence scaffold placement so signed evidence and manifest mechanics are not delayed until Epic 5.
6. Mark ADR-dependent stories as blocked or decision-prerequisite stories until temporal anchor, freshness blocking, redaction propagation, and retention/legal-hold interactions are resolved.
7. Convert UX Leak Sentinel, responsive/accessibility fixtures, and disclosure-surface checks into concrete test assets or conformance checklist items.

### Recommended Next Steps

1. Update `epics.md` to fix Story 1.1 coverage wording and split oversized proof stories.
2. Add ADR prerequisite stories or explicit blocking notes before implementation stories that depend on unresolved architecture decisions.
3. Add an early CI/evidence foundation story covering baseline workflow, test artifact publication, and placeholder conformance manifest.
4. Review UX trust decisions with architecture: temporal evidence anchor, freshness state blocking rules, redaction propagation, and disclosure fixtures.
5. Re-run this readiness check after the epic/story edits, then proceed to story-level implementation only if the major issues are closed or explicitly waived.

### Final Note

This assessment identified 14 issues or caveats requiring attention across UX/architecture alignment and epic/story quality: 5 UX alignment decision gaps, 6 major epic quality issues, and 3 minor epic quality concerns. Address the major issues before proceeding to implementation. The artifacts are unusually thorough; they need tightening, not reinvention.

**Assessment completed:** 2026-05-15  
**Assessor:** Codex using `bmad-check-implementation-readiness`
