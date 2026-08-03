---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
inputDocuments:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md
    - _bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
    - _bmad-output/planning-artifacts/ux-requirement-map.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** Conversations

## Document Discovery

### Authoritative Assessment Inputs

- PRD: `prds/prd-Conversations-2026-06-02/prd.md` and `addendum.md`
- Architecture: `architecture.md`
- Epics and stories: `prds/prd-Conversations-2026-06-02/epics.md` and `epic-6-current-execution-view-v1.md`
- UX: `ux-design-specification.md` and `ux-requirement-map.md`

### Discovery Notes

- All required document categories were found.
- No whole-versus-sharded duplicate document formats were found.
- The PRD directory has no `index.md`; the selected PRD and addendum are treated as whole authoritative inputs rather than a formally sharded document.
- Editorial reviews, decision and memory logs, reconciliation records, prior readiness reports, and `ux-design-directions.html` are supporting artifacts and are excluded from the authoritative assessment set.


## PRD Analysis

### Functional Requirements

#### Refactoring Requirements

#### FR-1: Canonical boilerplate inventory exists and is accepted

A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability.

**Consequences (testable):**
- Every top-level source area in `Hexalith.Conversations.*` appears in the inventory with exactly one classification.
- Each Consume/Promote entry names the technical-module capability it maps to (existing or to-be-promoted).
- The baseline plumbing-LOC figure used by SM-1 is derived from this artifact and recorded.

#### FR-2: Classification disagreements are resolvable, not silent

A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale.

**Consequences (testable):**
- Any area reclassified after first acceptance has a logged rationale (decision log or inventory note).
- No area is left unclassified or dual-classified at acceptance.

#### FR-3: Domain-service host adoption

Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing.

**Consequences (testable):**
- Conversations is discoverable and runnable through the platform host without a Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime-host project.
- All Conversations operations supported before the refactor remain available through the shared host.
- Existing hosting behavior is covered by integration evidence against the platform host; only tests tied solely to superseded local plumbing may be removed.

#### FR-4: Query handling via SDK query-handler + cursor seams

Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts.

**Consequences (testable):**
- Local domain-agnostic query-orchestration and pagination-token machinery is removed; conversation-specific query behavior remains.
- Accepted and rejected pagination tokens, page ordering, continuation, and response shapes remain contract-compatible.
- Cursor round-trip and pagination behavior remain identical in release-gate scenarios.

#### FR-5: Read-model persistence via shared store + write policy

Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics.

**Consequences (testable):**
- Local domain-agnostic persistence and conflict-resolution loops are removed.
- Observable concurrent-update behavior is preserved, including the absence of lost updates under the existing tested contention scenarios.

#### FR-6: Projection handling via SDK projection seam

Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits.

**Consequences (testable):**
- Local generic projection orchestration is removed from Conversations.
- Conversation-specific projection field selection, freshness formula, and evidence construction remain in the module and retain their observable behavior.
- Projection rebuild/freshness conformance tests pass.

#### FR-7: Aggregate scaffolding via base-class conventions

Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior.

**Consequences (testable):**
- Redundant local routing or state-reconstruction plumbing is removed where the platform already provides equivalent behavior.
- Aggregate command/state/event behavior is unchanged (pure aggregate tests green).

#### FR-8: Serialization via shared converters / type registration

Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules.

**Consequences (testable):**
- Local converters and registration code that carry no domain rule are removed; only conversation-specific serialization rules remain.
- Serialized contract shapes are byte/shape-compatible (round-trip tests green).

#### FR-9: Testing via shared assertions/fakes/defaults

Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers.

**Consequences (testable):**
- Duplicate in-module test infrastructure that re-implements shared platform capabilities is removed.
- Domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation scenarios) remain.

#### FR-10: Platform-owned shared ServiceDefaults

The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- Conversations owns no ServiceDefaults project or equivalent hosting-defaults implementation.
- Existing health, telemetry, resilience, and discovery behavior remains observable after adoption, and conversation-specific telemetry remains available with its established names and dimensions.

#### FR-11: Generic tenant-access projection handler + registration

A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules.

**Consequences (testable):**
- The copied Conversations tenant-access processing and registration infrastructure is replaced by the shared capability.
- Fail-closed behavior on missing/stale/unavailable/disabled/ambiguous/insufficient projection state is preserved (tenant-isolation conformance green).
- Duplicate/out-of-order/replay tolerance is preserved.

#### FR-12: Shared client registration

A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation.

**Consequences (testable):**
- Conversations client registration uses the shared capability and the superseded local registration code is removed.
- Invalid endpoint configuration continues to be rejected with contract-compatible behavior (client registration tests green).

#### FR-13: Platform-owned Aspire/Dapr domain-service hosting

The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- No Conversations-local AppHost, Aspire, ServiceDefaults, or equivalent runtime-host module remains.
- The platform-hosted Conversations service retains its current dependency access, isolation mode, health behavior, and event/publication connectivity.

#### FR-14: Shared serialization metadata and polymorphic registration

A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition.

**Consequences (testable):**
- Conversations declares only its domain-specific serializable contract set; domain-agnostic registration and composition boilerplate is removed.
- Polymorphic (de)serialization of event/command hierarchies is preserved.

#### FR-15: Diagnostics/telemetry scaffolding helper

A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies.

**Consequences (testable):**
- Domain-agnostic instrumentation setup is removed from Conversations; only conversation-specific metric definitions and classification rules remain.
- Emitted metric names and cardinality are preserved.

#### FR-16: Compile-time command/event contract metadata *(deferred)*

Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative.

**Consequences (testable):**
- The pilot does not add shared command/event metadata interfaces or reshape current Conversations command/event contracts.
- The backlog record preserves the candidate and rationale without making it part of pilot acceptance or FR-20's change surface. `[OQ-4 resolved 2026-07-14.]`

**Notes:** Governance/verification orchestration, temporal query reconstruction, and reference hydration remain Conversations-owned during this pilot. The pilot may consume an already-demonstrated generic SDK seam without moving the domain behavior, but creating or extracting new shared capabilities for these areas is follow-on work requiring a separate decision. `[OQ-3 resolved 2026-07-14.]`

#### FR-17: Conversations consumes every in-scope shared capability

Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot.

**Consequences (testable):**
- For each in-scope shared capability, the corresponding Conversations local implementation is deleted (not merely bypassed).
- Conversations builds and all conformance suites pass against the platform libraries.

#### FR-18: Documented thin authoring template

A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module.

**Consequences (testable):**
- The template enumerates the platform-host integration contract and the shared aggregate, query, projection, tenant-access, client, serialization, and telemetry responsibilities, including the minimal domain-owned inputs; AppHost, Aspire, DAPR, and ServiceDefaults remain platform-owned.
- The template is validated against the post-refactor Conversations module (it describes what Conversations actually does).

#### FR-19: New-module authoring cost is measured

The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2.

**Consequences (testable):**
- A measured "minimal module" figure (files + LOC) is recorded and traceable to the template.
- Target attainment requires a reproducible minimal-module fixture and a versioned measurement artifact that records the frozen file/LOC inclusion rules, source paths, measurement command/tool versions, commit/build identity, results, and named acceptance.

#### FR-20: Behavior and contracts are provably preserved

Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change.

**Consequences (testable):**
- The versioned preservation manifest identifies every denominator test and contract baseline, with the accepted pre-refactor source commit/build identity and evidence that the listed tests passed.
- All manifested release-gate conformance tests (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) pass post-refactor: the required pass rate is 100% of the frozen manifest.
- Public/adopter-facing contract shapes match the manifested baselines unless an explicit, named approval records the intentional change and its compatibility evidence.
- Removing, replacing, or reclassifying any manifested test requires explicit named-owner approval, rationale, replacement evidence where applicable, and a versioned manifest update; no conformance test is silently dropped.

**Refactoring requirement count:** 20 total; FR-16 is explicitly deferred, leaving 19 requirements in pilot scope.

#### Preserved Product Functional Requirements

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

**Preserved product functional requirement count:** 104.

**Total functional requirements extracted:** 124 numbered requirements (20 refactoring requirements plus 104 preserved product requirements). The preserved requirements constrain FR-20 and SM-C1; they are not assertions that the corresponding product features are implemented, shipped, accepted, or scheduled.

### Non-Functional Requirements

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

**Total numbered non-functional requirements extracted:** 77.

### Additional Requirements

- **Preservation denominator:** FR-20 and SM-C1 require 100% of the frozen, versioned pre-refactor preservation manifest to pass. Public/adopter-facing contract baselines cannot change, and manifested tests cannot be removed, replaced, or reclassified without explicit named-owner approval, rationale, replacement evidence where applicable, and a versioned manifest update.
- **Performance preservation:** SM-C2 allows no more than a 5% post-refactor P95 latency regression for every identified command/read hot path against a frozen reproducible baseline under the same benchmark envelope.
- **In-scope refactor:** FR-1 through FR-15 and FR-17 through FR-20; coordinated changes to relevant technical modules are authorized when needed.
- **Explicitly deferred:** FR-16, fleet migration of sibling domain modules, unconsumed promotions, governance orchestration extraction, temporal reconstruction extraction, upstream hydration extraction, new persistence/transport/providers, and UI/UX redesign.
- **Ownership boundary:** hosting, persistence, AppHost, Aspire, DAPR, ServiceDefaults, projection/query runtime, telemetry scaffolding, and event-subscription plumbing remain platform/domain-service SDK responsibilities. Conversations retains its domain contracts and behavior.
- **Domain-module boundary:** Hexalith.Tenants is a domain dependency/consumer, never a landing zone for generic hosting or runtime boilerplate.
- **Additive compatibility:** shared technical-module changes must be additive and backward compatible for existing consumers.
- **Fail-closed behavior:** tenant access and authorization remain fail closed; cross-tenant access must remain structurally impossible and adversarially tested.
- **Replay and delivery:** projection/event handling must remain idempotent and tolerate duplicate and out-of-order delivery.
- **Public-surface boundary:** Conversations public contracts remain stable and must not expose raw EventStore mechanics.
- **Measurement evidence:** SM-1 uses the accepted 13,289 LOC classified-plumbing baseline. SM-2 requires a reproducible minimal-module fixture and versioned measurement artifact; current file/LOC figures remain provisional.
- **Architecture dependency:** OQ-1 requires the platform architect to resolve the landing zone for each FR-10 through FR-15 before its implementation story starts.
- **Open technical decisions:** transport choice, idempotency-key source, stale-tenant-projection status/retry semantics, pub/sub naming sufficiency, audit-pairing health semantics, and any raw-HTTP release exception remain unresolved.
- **Open product/release dispositions:** the preserved legacy contract retains unresolved decisions concerning historical attribution coverage, signed conformance commitment, evidence-bundle slicing, chatbot dependency timing, downgrade authority, consumer status, second-adopter evidence, Foundation Gate ratification, CORE redaction scope, and absolute performance-gate activation.
- **Assumption controls:** the initiative assumes internal developer-platform scope, phased delivery, additive promotions, no external production use, and no required in-module deprecation window; each assumption has an owner or stated revisit trigger in the PRD.

### PRD Completeness Assessment

The PRD and addendum are structurally comprehensive and explicit about refactor scope, preservation boundaries, requirement namespaces, success metrics, and supporting evidence. All numbered FRs and NFRs are extractable and stable.

Completeness is conditional in three areas. First, OQ-1 leaves the FR-10 through FR-15 landing zones unresolved until architecture decides them. Second, SM-2 attainment remains provisional until FR-19 produces its reproducible fixture and versioned measurement artifact. Third, preserved legacy product and release requirements remain constraints rather than activated delivery scope, with several release, numeric-gate, and technical-mechanism decisions still open. These limitations do not make the PRD internally incomplete, but they must remain visible during epic coverage, story-quality, and implementation-readiness validation.


## Epic Coverage Validation

### Epic FR Coverage Extracted

- The historical epic coverage map explicitly maps FR-1 through FR-20.
- The active v8 Epic 6 authority defines the preservation denominator as all 20 initiative FRs, all 104 Feature-FRs, all 77 Feature-NFRs, all 52 UX decisions, and every UX acceptance criterion.
- Story 6.3 AC1–2 provides the traceable planning path for every preserved Feature-FR through evidence or a governed non-activation disposition.
- Story 6.6 AC2 validates the completed manifest and preservation gates.
- FR-16 is covered by an explicit deferred/non-activated disposition; it is not implementation scope.
- No functional requirement identifiers appear in the active epic authority without a corresponding PRD requirement.

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Canonical boilerplate inventory exists and is accepted — A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability. | Epic 1 historical delivery; Story 6.3 carries the frozen inventory into the complete manifest. | ✓ Covered |
| FR-2 | Classification disagreements are resolvable, not silent — A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale. | Epic 1 historical delivery; Story 6.3 preserves its governed disposition. | ✓ Covered |
| FR-3 | Domain-service host adoption — Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing. | Epic 2 historical delivery; Stories 6.1, 6.2, and 6.6 establish, migrate, and revalidate platform-host authority. | ✓ Covered |
| FR-4 | Query handling via SDK query-handler + cursor seams — Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts. | Epic 2 historical delivery; Stories 6.3 and 6.6 bind and revalidate the preserved query/cursor behavior. | ✓ Covered |
| FR-5 | Read-model persistence via shared store + write policy — Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics. | Epic 2 historical delivery; Stories 6.3 and 6.6 bind and revalidate shared read-model persistence. | ✓ Covered |
| FR-6 | Projection handling via SDK projection seam — Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits. | Epic 2 historical delivery; Stories 6.2, 6.3, 6.6, and 6.12 cover projection execution and current proof. | ✓ Covered |
| FR-7 | Aggregate scaffolding via base-class conventions — Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior. | Epic 2 historical delivery; Stories 6.3 and 6.6 bind and revalidate aggregate behavior. | ✓ Covered |
| FR-8 | Serialization via shared converters / type registration — Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules. | Epic 2 historical delivery; Stories 6.3 and 6.6 bind and revalidate serialization compatibility. | ✓ Covered |
| FR-9 | Testing via shared assertions/fakes/defaults — Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers. | Epic 2 historical delivery; Stories 6.3 and 6.6 bind and revalidate test-infrastructure preservation. | ✓ Covered |
| FR-10 | Platform-owned shared ServiceDefaults — The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 3 historical delivery; Stories 6.1, 6.2, and 6.6 fix ownership, remove drift, and verify. | ✓ Covered |
| FR-11 | Generic tenant-access projection handler + registration — A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules. | Epic 3 historical delivery; Stories 6.1, 6.3, and 6.6 bind landing-zone and fail-closed evidence. | ✓ Covered |
| FR-12 | Shared client registration — A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation. | Epic 3 historical delivery; Stories 6.1, 6.3, and 6.6 bind landing-zone and compatibility evidence. | ✓ Covered |
| FR-13 | Platform-owned Aspire/Dapr domain-service hosting — The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 3 historical delivery; Stories 6.1, 6.2, and 6.6 fix topology ownership, migrate, and verify. | ✓ Covered |
| FR-14 | Shared serialization metadata and polymorphic registration — A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition. | Epic 3 historical delivery; Stories 6.1, 6.3, and 6.6 bind serialization landing-zone evidence. | ✓ Covered |
| FR-15 | Diagnostics/telemetry scaffolding helper — A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies. | Epic 3 historical delivery; Stories 6.1, 6.3, and 6.6 bind telemetry landing-zone and continuity evidence. | ✓ Covered |
| FR-16 | Compile-time command/event contract metadata *(deferred)* — Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative. | Explicitly deferred/non-activated by PRD and v8; Story 6.3 records the governed non-activation. | ✓ Covered |
| FR-17 | Conversations consumes every in-scope shared capability — Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot. | Epic 3 historical adoption; Stories 6.2 and 6.5 establish the platform-hosted module and corrected template. | ✓ Covered |
| FR-18 | Documented thin authoring template — A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module. | Epic 4 historical delivery superseded for current authority by Story 6.5. | ✓ Covered |
| FR-19 | New-module authoring cost is measured — The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2. | Epic 4 historical delivery superseded for current evidence by Stories 6.5 and 6.6. | ✓ Covered |
| FR-20 | Behavior and contracts are provably preserved — Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change. | Epic 5 historical attestation superseded for current authority by Stories 6.3 and 6.6. | ✓ Covered |
| Feature-FR1 | Adopter systems can create a tenant-scoped conversation record. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR2 | Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR3 | The system can represent conversation lifecycle state and allowed transitions, including active, archived, or closed states and any release-approved behavior for reopening or sealing. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR4 | Adopter systems can append ordered messages to an existing conversation. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR5 | Adopter systems can add human users, AI agents, and LLMs as conversation participants. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR6 | Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR7 | The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR8 | Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR9 | Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR10 | Adopter systems can update conversation title or metadata when that capability is included in the active release scope. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR11 | Adopter systems can close or archive a conversation when that capability is included in the active release scope. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR12 | The system can preserve a complete conversation record across provider session expiry, restart, or failover. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR13 | The system can attribute each conversation action to a stable Party identity. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR14 | The system can model humans, AI agents, and LLMs as attributable participants. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR15 | The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR16 | The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR17 | The system can preserve multi-provider attribution when a conversation crosses provider boundaries. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR18 | The system can reconstruct who said or changed what, when, and under which tenant context. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR19 | Adopter systems can attach file references to a conversation without storing file binaries in Conversations. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR20 | Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR21 | Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR22 | The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR23 | The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR24 | The system can keep conversations readable and attributable when upstream entities change lifecycle state. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR25 | The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR26 | The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR27 | The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR28 | The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR29 | The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR30 | The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR31 | The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR32 | The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR33 | The system can derive projections from ordered conversation events. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR34 | The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR35 | The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR36 | The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR37 | The system can expose projection lag or documented freshness behavior when read models are asynchronous. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR38 | Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR39 | Published events can carry explicit schema and version metadata. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR40 | The system can reject unsupported event, command, or projection schema versions with typed documented errors. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR41 | The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR42 | Authorized systems can set or replace a conversation retention policy with rationale. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR43 | Authorized systems can mark conversation content as sensitive. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR44 | Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR45 | The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR46 | The system can preserve the audit event stream while redacting projected or displayed content. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR47 | The system can require every governance mutation to have a paired audit event. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR48 | The system can reject governance mutations when audit recording is unavailable. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR49 | The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR50 | The system can reconstruct message state and governance state as they existed at a prior point in time. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR51 | The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR52 | The system can apply retention and redaction policy treatment to governance audit records themselves. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR53 | The system can define which actions on audit records are allowed or denied and when the records can be redacted, exported, or separately logged. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR54 | The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR55 | Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR56 | Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR57 | Compliance operators can filter or narrow conversation search by date range and business context. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR58 | Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR59 | Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR60 | Compliance operators can view a conversation's governance audit trail inline. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR61 | Compliance operators can view conversation state as of a selected historical time. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR62 | Compliance operators can copy citation-ready references for transcript and audit elements. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR63 | Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR64 | Operator and compliance workflows marked read-only cannot mutate conversation aggregate state. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR65 | Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR66 | Operators can run governance verification for a conversation, tenant, suite, or time window. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR67 | Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR68 | Verification results can distinguish governance verification failures from infrastructure or execution failures. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR69 | The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR70 | Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR71 | Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR72 | Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR73 | Adopter developers can run adopter-facing conformance tests before deployment. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR74 | Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR75 | Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR76 | The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR77 | The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR78 | The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR79 | The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR80 | The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR81 | The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR82 | The product can produce a signed conformance artifact for release gating. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR83 | The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR84 | The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR85 | The product can support a named-waiver process for release-gate exceptions. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR86 | The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR87 | The product can verify tenant isolation using adversarial positive and negative cases. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR88 | The product can verify idempotent command behavior under duplicate or reordered commands. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR89 | The product can verify redaction-replay correctness across projections, logs, traces, and errors. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR90 | The product can verify provider portability by proving recoverability without provider-owned session authority. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR91 | The product can verify event schema evolution through version-aware records and at least one worked additive-change example. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR92 | The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR93 | The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR94 | The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR95 | Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR96 | Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR97 | Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR98 | Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR99 | Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR100 | The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR101 | The product can expose release-scope consequences when substrate-defining capabilities are deferred. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR102 | The product can support buyer partial acceptance under the Option A v1 deal. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR103 | The product can track second-adopter status and trigger downgrade-rule review milestones. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |
| Feature-FR104 | The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities. | Epic 6 Story 6.3 AC1–2 (complete evidence/non-activation disposition) and Story 6.6 AC2 (manifest validation/attestation). | ✓ Covered |

### Missing Requirements

None. Every PRD functional requirement has a traceable epic/story disposition.

Coverage does not imply feature activation or implementation. The 104 preserved Feature-FRs constrain FR-20 and are covered through explicit evidence/non-activation governance in Stories 6.3 and 6.6.

### Coverage Statistics

- Total PRD functional requirements: 124
- Initiative FRs covered: 20 of 20
- Preserved Feature-FRs covered: 104 of 104
- Total FRs covered in epics: 124
- Missing FRs: 0
- Epic-only FR identifiers: 0
- Coverage percentage: 100%


## UX Alignment Assessment

### UX Document Status

**Found.** The authoritative UX inputs are:

- `ux-design-specification.md`, authority version `ux-preservation-planning-2026-08-01-v1`
- `ux-requirement-map.md`, with 52 UX decisions and 28 explicit acceptance-criterion identifiers

Both documents state `preserved-not-activated`. They preserve product UX obligations but do not authorize product UI implementation in the corrective initiative.

### UX ↔ PRD Alignment

- The UX operator journey—Find → Open → Verify → Cite, Act, or Stop—aligns with preserved Feature-FR56 through Feature-FR69.
- Tenant-scoped, permission-safe discovery and cross-tenant non-enumeration align with Feature-FR26 through Feature-FR32 and Feature-NFR16 through Feature-NFR19.
- Evidence timelines, audit linkage, redaction, temporal reconstruction, and citation behavior align with Feature-FR42 through Feature-FR68 and Feature-NFR20 through Feature-NFR21, Feature-NFR38 through Feature-NFR48, and Feature-NFR62 through Feature-NFR68.
- Developer and adopter UX aligns with Feature-FR70 through Feature-FR80 and Feature-NFR49 through Feature-NFR54.
- WCAG 2.1 AA, keyboard, screen-reader, non-color, degraded-state, and human-trust obligations align with Feature-NFR69 through Feature-NFR77.
- The PRD explicitly makes UI/UX redesign a non-goal for this refactor. The UX documents match this by preserving, not activating, their component roadmap and acceptance obligations.
- The PRD’s active SM-C2 no-regression gate remains distinct from preserved absolute product targets. UX’s 90-second operator goal is preserved but activates only through separate release authority.

### UX ↔ Architecture Alignment

- Architecture v8 explicitly includes all 52 UX decisions and every explicit UX acceptance criterion in the preservation denominator.
- Architecture assigns FrontComposer and Fluent UI Blazor to baseline UI composition and reserves custom components for trust-bearing evidence, redaction, freshness, audit, citation, temporal, and command-safety behavior, matching the UX component strategy.
- Both documents require server-owned projection and command-availability metadata; the UI may render but never infer authorization, trust, freshness, redaction, completeness, or action eligibility.
- Both require fail-closed disclosure behavior across visible text, hidden DOM, accessibility output, clipboard, URLs, telemetry, responsive duplicates, search counts, facets, pagination, ordering, and timing.
- Both preserve WCAG 2.1 AA, keyboard-only workflows, screen-reader trust order, safe responsive behavior, and mobile read-only triage by default.
- Architecture’s corrected target permits an optional domain UI composition surface while keeping production hosting and reusable runtime capability platform-owned, consistent with the UX’s generated-first boundary.
- Story 6.4 is the explicit current owner for the UX preservation disposition schema, JSON, deterministic Markdown projection, and zero-gap validator. It does not authorize product UI changes.

### Alignment Issues

No material semantic conflict was found between current PRD, UX, and architecture authority.

Historical implementation mappings and Phase 0–3 UI sequencing remain in the UX corpus only as labeled provenance. They are not current story ownership and cannot activate delivery.

### Warnings

- Story 6.4’s required UX preservation disposition artifacts and `UxPreservationDispositionValidationTest` are planned but not implemented. Alignment is therefore specified but not yet mechanically evidenced.
- The global architecture v8 implementation hold remains active. No UI work may start merely because the UX specification is complete.
- If product UI scope is later activated, component and token choices must be revalidated against the then-current FrontComposer and Fluent UI V5 contracts; preserved design intent does not override current platform conformance rules.


## Epic Quality Review

### Review Scope

The active review applies create-epics-and-stories standards to the current v8 execution contract. Completed Epics 1–5 and completed Stories 6.1, 6.2, and 6.7 are immutable historical records; their defects are noted only where they affect current planning. Remaining work is evaluated as an implementation plan, not as implementation status.

### 🔴 Critical Violations

#### 1. Epic 6 is an oversized, multi-outcome technical epic

**Epic:** “PRD Alignment And Preservation Reconciliation”

Epic 6 combines at least ten distinct outcomes:

- planning and architecture authority
- platform-host migration
- preservation traceability
- UX preservation governance
- thin-template correction
- final release attestation
- submodule-promotion completion enforcement
- mechanical final-record generation
- conformance-oracle tiering
- evidence-boundary infrastructure
- performance/data-layout optimization
- projection-proof lifecycle management

These outcomes serve different users, have different rollback boundaries, modify different repositories/surfaces, and can be reviewed independently. The epic is therefore a program-sized technical container rather than a cohesive user-value increment.

**Impact:** Scope, ownership, sequencing, and release risk are obscured. Story independence cannot be assessed cleanly because unrelated work shares one epic and one capstone.

**Required remediation:** Preserve completed Stories 6.1, 6.2, and 6.7 as historical foundation, then re-plan remaining work into outcome-focused epics, for example:

1. Release owners can trust mechanically derived completion evidence.
2. Consumers can run a structurally portable conformance oracle.
3. Release evidence is validated through one non-vacuous boundary.
4. Domain authors can use and measure the corrected thin template.
5. Operators retain correctness while all hot paths meet SM-C2.
6. Release owners can validate current projection assurance.
7. Release owners can sign a complete preservation attestation.

#### 2. Current story numbering violates forward-dependency rules

The active dependency plan contains these lower-to-later story dependencies:

| Story | Later-numbered dependency |
| --- | --- |
| 6.2 | 6.7 (historical; already completed) |
| 6.3 | 6.9, 6.10, 6.12 |
| 6.4 | 6.8 for completion |
| 6.5 | 6.8 and 6.10 for completion |
| 6.6 | 6.8, 6.9, 6.10, 6.11, and 6.12 |
| Historical 2.6 | Historical 3.6, explicitly recorded as a former critical defect |

The graph is acyclic, but acyclicity is insufficient: numbered story plans must communicate implementation order without requiring future-numbered stories.

**Impact:** A developer selecting the next numbered story encounters unavailable prerequisites, and “ready-for-dev”/“in-progress” states become misleading.

**Required remediation:** Supersede—not rewrite—remaining story identifiers with a topological numbering scheme. The final attestation must receive the final number. Maintain a versioned old-to-new ID map for historical traceability.

#### 3. Five stories are epic-sized and require decomposition

**Story 6.5** bundles three explicit checkpoints: authoring contract, executable fixture, and measurement/acceptance. Each produces a separately reviewable artifact and rollback boundary.

**Required split:**

- Correct and validate platform-hosted authoring guidance.
- Build and verify the minimal fixture against live public APIs.
- Generate and accept reproducible SM-2 evidence.

**Story 6.8** bundles generator schema/output, test-result discovery, Git/file/gitlink derivation, candidate binding, workflow enforcement, historical verification, and fault injection.

**Required split:**

- Define the final-record schema and deterministic generator core.
- Derive test, path, candidate, submodule, and gitlink facts.
- Integrate the generator with all completion surfaces and blocking transitions.
- Add historical read-only verification and the fault-injection matrix.

**Story 6.10** bundles a new TestSupport project, process-safe Git execution, manifest integrity, diff/gitlink semantics, a Python policy gate, five workflow integrations across mirrored trees, migration of 24+ evidence readers, documentation, fault injection, and inherited gate-span repair.

**Required split:**

- Build the non-shipping evidence-boundary helper and safe Git facts layer.
- Implement manifest, hash, assertion-ledger, exact-diff, and gitlink invariants.
- Implement the policy verifier and integrate every governed workflow surface.
- Migrate evidence readers, repair gate-span coupling, document the runbook, and execute fault injection.

**Story 6.11** combines a data-layout ADR, derived-state schema/transition design, production read optimization, fail-closed correctness, benchmark-method redesign, signal-quality proof, four hot-path performance gates, integration/real-DAPR fault injection, and release evidence.

**Required split:**

- Decide derived-key ownership, compatibility, rebuild/backfill, deletion, expiry, and rollback.
- Implement correctness-preserving list/open read optimization with migration and replay proof.
- Establish the frozen measurement and signal-quality method for all four hot paths.
- Produce candidate-bound performance/correctness evidence and enforce the universal SM-C2 gate.

**Story 6.12** already exposes three independent checkpoints but retains one all-or-nothing story: historical validation/lifecycle contract, successor generation/current guard, and fault injection/manifest handoff.

**Required split:**

- Validate immutable historical proof and publish the predecessor-chain ADR/schema.
- Generate the current successor proof and enforce the drift/current-head guard.
- Complete fault injection, manifest/attestation handoff, conformance runs, and generated final record.

### 🟠 Major Issues

#### 4. Acceptance criteria are not atomic BDD contracts

The current effective definitions for Stories 6.3–6.6 and 6.8–6.12 use numbered prose rather than Given/When/Then scenarios. Many single criteria contain multiple independently failing assertions.

Examples:

- Story 6.3 AC2 combines evidence presence, non-activation governance, owner approval, rationale, and compatibility evidence.
- Story 6.5 AC3 combines fixture creation, package/publish restrictions, live API use, build, tests, and exact inventory.
- Story 6.8 AC8 says every guard is fault-injected and restored without defining the required mutation catalogue or expected blocker per mutation.
- Story 6.9 AC1 requires “assertion strength preserved,” which is not machine-decidable as written.
- Story 6.10 AC8 combines migration of 24+ readers, zero exemptions, unchanged assertion strength, pinned constants, and preserved counts.
- Story 6.11 AC10 combines four performance verdicts, all correctness gates, signal quality, test status, and stale-binding checks.
- Story 6.12 AC8 combines three test lanes, zero failed/skipped/not-run status, and final-record generation.
- Story 6.6 AC2 aggregates the manifest, contract compatibility, topology, security, health, publication, admin composition, three success metrics, and every preservation gate.

**Required remediation:** Give every criterion a stable identifier and one observable outcome. Express it as Given/When/Then with:

- exact input artifact/version or fixture
- exact command/test/generator
- expected output path/schema/field
- expected exit code or test result
- exact zero-gap/zero-skip/zero-failure rule
- stable blocker code for each failure class
- explicit authority and candidate binding

Subjective phrases such as “unchanged assertion strength,” “clean tests,” “compatible candidate,” and “every preservation gate” need mechanical definitions or named, separately recorded human approval contracts.

#### 5. Story completion and dependency boundaries are mixed

Stories 6.3, 6.4, and 6.5 can begin before prerequisites needed for completion. This creates long-lived partial work and weakens the meaning of a story as an independently completable unit. Checkpoints inside 6.5 and 6.12 explicitly acknowledge this condition while forbidding checkpoint completion from advancing story state.

**Required remediation:** Convert independently reviewable checkpoints into stories ordered after their prerequisites. A story should enter implementation only when every dependency needed for its own completion is available.

#### 6. Capstone story numbering obscures its intended role

Story 6.6 is correctly described as last, but its identifier appears before five of its prerequisites. Its purpose as the final release-owner outcome is sound; its placement is not.

**Required remediation:** Keep the capstone concept, assign it the last identifier after decomposition, and require one mechanically enumerated predecessor set rather than a prose list.

### 🟡 Minor Concerns

- Historical Epics 1–3 are technical-enablement epics rather than user-outcome epics. They are completed and should not be rewritten, but they should not be copied as the shape for the corrective re-plan.
- Current high-risk BDD scenarios are useful but are epic-level examples; they do not replace atomic story-level acceptance criteria or full failure catalogues.
- Story 6.4 is reasonably cohesive, but AC2 and AC5 should be separated into artifact-generation and validation outcomes.
- Story 6.9 is reasonably cohesive, but “unchanged strength” needs an objective assertion inventory/digest contract.
- Database/table timing is not a current violation. Story 6.11’s new derived-key families must, however, remain owned by the first implementation slice after its separate ADR story.
- The historical starter scaffold is explicitly superseded. This is a brownfield corrective initiative, so no new generic “project setup” story is required; current integration and migration boundaries are documented.

### Best-Practices Compliance Summary

| Area | Result |
| --- | --- |
| Epic delivers cohesive user value | ❌ Epic 6 fails |
| Epic dependency order | ✓ No dependency on a future epic |
| Story independence | ❌ Multiple stories cannot complete without later-numbered stories |
| Story sizing | ❌ Stories 6.5, 6.8, 6.10, 6.11, and 6.12 are oversized |
| Forward dependencies | ❌ Present |
| Atomic, testable acceptance criteria | ❌ Inconsistent and frequently compound |
| Functional-requirement traceability | ✓ 100% |
| Database/entity timing | ✓ No current upfront-database violation |
| Brownfield integration coverage | ✓ Explicit |
| Implementation permission | ❌ Architecture v8 hold remains active |

### Epic Quality Verdict

**FAIL.** Functional traceability is complete, but the implementation plan does not meet story independence, sizing, ordering, or atomic acceptance-criteria standards. The plan must be decomposed and topologically renumbered before implementation readiness can be granted.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The specifications provide complete functional traceability, and the PRD, UX, and Architecture are broadly aligned. Implementation must not start or resume, however, because the remaining Epic 6 plan is not independently executable and Architecture v8 explicitly keeps the implementation hold in force.

This assessment independently confirms the `NOT READY` verdict recorded by the 2026-08-01 rerun.

### Blocking Issues Requiring Resolution

1. **Epic structure:** Epic 6 is an oversized, multi-outcome technical epic rather than one cohesive deliverable. Its remaining work spans implementation, governance, evidence infrastructure, performance optimization, historical verification, and release attestation.
2. **Dependency and execution order:** Several stories require later-numbered stories to complete. The dependency graph may be acyclic, but the numbered execution plan is not topological and therefore does not support independent story selection.
3. **Story sizing:** Stories 6.5, 6.8, 6.10, 6.11, and 6.12 each combine multiple independently reviewable outcomes and require decomposition along the boundaries defined in this report.
4. **Acceptance contracts:** Many acceptance criteria are compound prose rather than atomic, machine-verifiable outcomes. They lack consistent stable IDs, exact commands or tests, expected artifacts and fields, exit/result semantics, blocker codes, and authority/candidate bindings.
5. **Implementation authority:** Architecture v8 states that the current plan is not implementation permission. That hold remains authoritative until a corrected plan passes an independent readiness review.

### Recommended Next Steps

1. Preserve completed history and issue an append-only v9 planning correction; do not rewrite Architecture v8 or completed story records.
2. Recast remaining Epic 6 work into cohesive outcome epics and decompose Stories 6.5, 6.8, 6.10, 6.11, and 6.12 using the split boundaries documented in the Epic Quality Review.
3. Topologically renumber all remaining work, publish a versioned old-to-new story mapping, and place the final attestation capstone last.
4. Rewrite acceptance criteria as stable, atomic Given/When/Then contracts with exact inputs, commands or tests, artifact schemas and locations, expected exit codes or results, zero-gap rules, blocker codes, and candidate/authority bindings.
5. Regenerate the deterministic current-execution view and sprint status, then run the mechanical authority and predecessor validations against the corrected plan.
6. Rerun Implementation Readiness independently. Keep the implementation hold active until that assessment returns `READY`.

### Final Note

This assessment identified **five blocking issue groups** across epic structure, dependency order, story sizing, acceptance-contract precision, and implementation authority. The plan has **124 of 124 functional requirements mapped (100%)**, with no missing or orphaned functional requirement, but coverage does not compensate for an execution plan that cannot yet be implemented safely as independently completable stories.

**Assessment date:** 2026-08-02  
**Assessor:** Codex using the BMad Implementation Readiness workflow
