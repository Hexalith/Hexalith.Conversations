---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
filesIncluded:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
    - _bmad-output/planning-artifacts/ux-requirement-map.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** Conversations

## Document Inventory

### PRD

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md` (85,089 bytes; modified 2026-07-14 20:36 CEST)
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md` (13,775 bytes; modified 2026-07-14 20:34 CEST)

The nested PRD documents are the confirmed assessment inputs. Reconciliation and editorial-review files in the same folder are supporting artifacts rather than selected PRD authority.

### Architecture

- `_bmad-output/planning-artifacts/architecture.md` (119,264 bytes; modified 2026-08-01 18:53 CEST)

### Epics and Stories

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (140,511 bytes; modified 2026-08-01 19:12 CEST)

The root `epic-6-current-execution-view-v1.md` is treated as a supporting execution view, not canonical epic authority.

### UX Design

- `_bmad-output/planning-artifacts/ux-design-specification.md` (118,861 bytes; modified 2026-08-01 18:53 CEST)
- `_bmad-output/planning-artifacts/ux-requirement-map.md` (15,824 bytes; modified 2026-08-01 18:55 CEST)

The HTML design-directions artifact is supporting material rather than a selected UX authority document.

### Discovery Notes

- No whole-versus-sharded document-format duplicates were found.
- No sharded `index.md` document sets were found.
- All four required document types are represented in the confirmed assessment set.

## PRD Analysis

The PRD deliberately separates the boilerplate-refactor scope from the preserved Conversations product contract. “Preserved” constrains FR-20/SM-C1; it does not mean implemented, shipped, accepted, or scheduled.

### Functional Requirements

#### Refactor requirements

##### FR-1: Canonical boilerplate inventory exists and is accepted

A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability.

**Consequences (testable):**
- Every top-level source area in `Hexalith.Conversations.*` appears in the inventory with exactly one classification.
- Each Consume/Promote entry names the technical-module capability it maps to (existing or to-be-promoted).
- The baseline plumbing-LOC figure used by SM-1 is derived from this artifact and recorded.

##### FR-2: Classification disagreements are resolvable, not silent

A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale.

**Consequences (testable):**
- Any area reclassified after first acceptance has a logged rationale (decision log or inventory note).
- No area is left unclassified or dual-classified at acceptance.

##### FR-3: Domain-service host adoption

Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing.

**Consequences (testable):**
- Conversations is discoverable and runnable through the platform host without a Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime-host project.
- All Conversations operations supported before the refactor remain available through the shared host.
- Existing hosting behavior is covered by integration evidence against the platform host; only tests tied solely to superseded local plumbing may be removed.

##### FR-4: Query handling via SDK query-handler + cursor seams

Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts.

**Consequences (testable):**
- Local domain-agnostic query-orchestration and pagination-token machinery is removed; conversation-specific query behavior remains.
- Accepted and rejected pagination tokens, page ordering, continuation, and response shapes remain contract-compatible.
- Cursor round-trip and pagination behavior remain identical in release-gate scenarios.

##### FR-5: Read-model persistence via shared store + write policy

Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics.

**Consequences (testable):**
- Local domain-agnostic persistence and conflict-resolution loops are removed.
- Observable concurrent-update behavior is preserved, including the absence of lost updates under the existing tested contention scenarios.

##### FR-6: Projection handling via SDK projection seam

Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits.

**Consequences (testable):**
- Local generic projection orchestration is removed from Conversations.
- Conversation-specific projection field selection, freshness formula, and evidence construction remain in the module and retain their observable behavior.
- Projection rebuild/freshness conformance tests pass.

##### FR-7: Aggregate scaffolding via base-class conventions

Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior.

**Consequences (testable):**
- Redundant local routing or state-reconstruction plumbing is removed where the platform already provides equivalent behavior.
- Aggregate command/state/event behavior is unchanged (pure aggregate tests green).

##### FR-8: Serialization via shared converters / type registration

Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules.

**Consequences (testable):**
- Local converters and registration code that carry no domain rule are removed; only conversation-specific serialization rules remain.
- Serialized contract shapes are byte/shape-compatible (round-trip tests green).

##### FR-9: Testing via shared assertions/fakes/defaults

Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers.

**Consequences (testable):**
- Duplicate in-module test infrastructure that re-implements shared platform capabilities is removed.
- Domain-specific conformance fixtures (redaction, provider-portability, tenant-isolation scenarios) remain.

##### FR-10: Platform-owned shared ServiceDefaults

The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- Conversations owns no ServiceDefaults project or equivalent hosting-defaults implementation.
- Existing health, telemetry, resilience, and discovery behavior remains observable after adoption, and conversation-specific telemetry remains available with its established names and dimensions.

##### FR-11: Generic tenant-access projection handler + registration

A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules.

**Consequences (testable):**
- The copied Conversations tenant-access processing and registration infrastructure is replaced by the shared capability.
- Fail-closed behavior on missing/stale/unavailable/disabled/ambiguous/insufficient projection state is preserved (tenant-isolation conformance green).
- Duplicate/out-of-order/replay tolerance is preserved.

##### FR-12: Shared client registration

A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation.

**Consequences (testable):**
- Conversations client registration uses the shared capability and the superseded local registration code is removed.
- Invalid endpoint configuration continues to be rejected with contract-compatible behavior (client registration tests green).

##### FR-13: Platform-owned Aspire/Dapr domain-service hosting

The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module.

**Consequences (testable):**
- No Conversations-local AppHost, Aspire, ServiceDefaults, or equivalent runtime-host module remains.
- The platform-hosted Conversations service retains its current dependency access, isolation mode, health behavior, and event/publication connectivity.

##### FR-14: Shared serialization metadata and polymorphic registration

A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition.

**Consequences (testable):**
- Conversations declares only its domain-specific serializable contract set; domain-agnostic registration and composition boilerplate is removed.
- Polymorphic (de)serialization of event/command hierarchies is preserved.

##### FR-15: Diagnostics/telemetry scaffolding helper

A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies.

**Consequences (testable):**
- Domain-agnostic instrumentation setup is removed from Conversations; only conversation-specific metric definitions and classification rules remain.
- Emitted metric names and cardinality are preserved.

##### FR-16: Compile-time command/event contract metadata *(deferred)*

Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative.

**Consequences (testable):**
- The pilot does not add shared command/event metadata interfaces or reshape current Conversations command/event contracts.
- The backlog record preserves the candidate and rationale without making it part of pilot acceptance or FR-20's change surface. `[OQ-4 resolved 2026-07-14.]`

**Notes:** Governance/verification orchestration, temporal query reconstruction, and reference hydration remain Conversations-owned during this pilot. The pilot may consume an already-demonstrated generic SDK seam without moving the domain behavior, but creating or extracting new shared capabilities for these areas is follow-on work requiring a separate decision. `[OQ-3 resolved 2026-07-14.]`

##### FR-17: Conversations consumes every in-scope shared capability

Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot.

**Consequences (testable):**
- For each in-scope shared capability, the corresponding Conversations local implementation is deleted (not merely bypassed).
- Conversations builds and all conformance suites pass against the platform libraries.

##### FR-18: Documented thin authoring template

A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module.

**Consequences (testable):**
- The template enumerates the platform-host integration contract and the shared aggregate, query, projection, tenant-access, client, serialization, and telemetry responsibilities, including the minimal domain-owned inputs; AppHost, Aspire, DAPR, and ServiceDefaults remain platform-owned.
- The template is validated against the post-refactor Conversations module (it describes what Conversations actually does).

##### FR-19: New-module authoring cost is measured

The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2.

**Consequences (testable):**
- A measured "minimal module" figure (files + LOC) is recorded and traceable to the template.
- Target attainment requires a reproducible minimal-module fixture and a versioned measurement artifact that records the frozen file/LOC inclusion rules, source paths, measurement command/tool versions, commit/build identity, results, and named acceptance.

##### FR-20: Behavior and contracts are provably preserved

Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change.

**Consequences (testable):**
- The versioned preservation manifest identifies every denominator test and contract baseline, with the accepted pre-refactor source commit/build identity and evidence that the listed tests passed.
- All manifested release-gate conformance tests (tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing) pass post-refactor: the required pass rate is 100% of the frozen manifest.
- Public/adopter-facing contract shapes match the manifested baselines unless an explicit, named approval records the intentional change and its compatibility evidence.
- Removing, replacing, or reclassifying any manifested test requires explicit named-owner approval, rationale, replacement evidence where applicable, and a versioned manifest update; no conformance test is silently dropped.

**Refactor requirement count:** 20 explicit FRs. FR-1 through FR-15 and FR-17 through FR-20 are in pilot scope (19); FR-16 is explicitly deferred (1).

#### Preserved product functional requirements

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

**Preserved product functional requirement count:** 104 explicit Feature-FRs.

**Total explicit functional requirements extracted:** 124 across the two source-defined namespaces.

### Non-Functional Requirements

#### Refactor cross-cutting requirements (source-unnumbered)

- **Behavior preservation:** FR-20 / SM-C1 are authoritative for the dominant NFR and its frozen denominator.
- **Performance:** SM-C2 is authoritative. Shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads; snapshot/projection behavior is preserved.
- **Fail-closed invariants:** promoted tenant-access and authorization capabilities must preserve fail-closed semantics by construction; cross-tenant access remains impossible and adversarially tested.
- **Observability:** metric names, dimensions, and health endpoints are preserved through platform-owned shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **Replay safety:** promoted projection/event handling must remain idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

#### Preserved product non-functional requirements

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

**Preserved product NFR count:** 77 explicit Feature-NFRs. The PRD also contains five source-unnumbered refactor cross-cutting NFRs listed above.

### Additional Requirements

#### Scope and release boundaries

- The initiative is a refactor, not feature delivery; it must introduce no new Conversations domain behavior or external-contract semantic change.
- The pilot includes FR-1 through FR-15 and FR-17 through FR-20. FR-16, fleet migration, and promotions Conversations does not consume are excluded.
- Governance orchestration, temporal reconstruction, and upstream hydration remain Conversations-owned during this pilot; only already-demonstrated generic SDK seams may be consumed without a separate follow-on decision.
- No new persistence model, transport, or provider is introduced. EventStore/Dapr remains the substrate.
- FrontComposer-generated admin behavior is preserved; no UX redesign is authorized.
- Delivery is phased: baseline, consume, promote, then adopt/prove.
- Plumbing-only tests may be removed only with the superseded plumbing; the frozen release-gate preservation denominator cannot be silently reduced.

#### Success and counter-metrics

- **SM-1:** remove or externalize at least 40% of the frozen accepted classified-plumbing LOC. The PRD records 70.43% as current evidence and target met.
- **SM-2:** reduce hand-authored, module-owned files for a minimal valid domain module by at least 50% within the frozen Story 4.1 boundary. LOC reduction is mandatory supporting evidence. Current 50.00% file and 67.95% LOC figures remain provisional until FR-19 produces the reproducible fixture and versioned accepted measurement artifact.
- **SM-3:** every in-scope promoted pattern has exactly one source of truth.
- **SM-4:** maintainers qualitatively report that Conversations reads as mostly domain logic.
- **SM-C1:** 100% of the frozen pre-refactor preservation manifest must pass post-refactor, with no unapproved public-contract change or silent denominator reduction.
- **SM-C2:** every identified command/read hot path must remain within 5% of the frozen pre-refactor P95 under the same reproducible envelope. Feature-NFR9 and Feature-NFR12 block only if separately activated by the current release plan.

#### Ownership and integration constraints

- Hosting, persistence, AppHost, Aspire, DAPR, ServiceDefaults, projection/query runtime, telemetry scaffolding, and event-subscription plumbing are platform/domain-service SDK responsibilities, never Conversations-owned hosting projects.
- Conversations retains domain contracts and behavior: validation, aggregate behavior, events/state, projection field selection, domain freshness/evidence semantics, governance orchestration, temporal reconstruction, and upstream hydration within this pilot.
- Shared-module changes must be additive and backward-compatible for existing consumers.
- Hexalith.Tenants is a domain dependency/consumer, not a landing zone for generic hosting or runtime plumbing.
- Public Conversations contracts remain stable and public clients must not expose raw EventStore mechanics.
- Root-level submodule coordination is authorized for the initiative, but nested submodule recursion remains prohibited.
- The accepted SM-1 baseline is 13,289 plumbing LOC (37.15%) out of exactly 35,769 source LOC, recorded in the canonical FR-2-governed inventory; the earlier approximately 18,000 LOC discovery estimate is provenance only.
- The thin-template integration contract must cover shared aggregate, query, projection, tenant-access, client, serialization, telemetry, and platform-host responsibilities.
- The addendum maps current platform seams to EventStore.DomainService, EventStore.Client, EventStore.ServiceDefaults, EventStore.Aspire, EventStore.Testing, Commons, and FrontComposer. Conversations must extend generic behavior in its platform-owned home when a required seam is incomplete.

#### Open dependencies and dispositions

- **OQ-1 remains open and implementation-blocking per slice:** the platform architect must choose the technical landing zone for each FR-10 through FR-15 before that requirement’s implementation story begins.
- OQ-2 through OQ-5 are resolved, but SM-2 attainment evidence remains provisional until FR-19 is satisfied.
- Legacy technical questions TQ1 through TQ5 remain open: supported transport, idempotency-key origin, stale-tenant status/retry semantics, pub/sub topic naming, and audit-pairing health semantics.
- Legacy-TQ6 remains an explicit release exception: raw HTTP may replace the supported .NET client only with buyer acceptance.
- Legacy-TQ7 is resolved for this refactor: the EventStore envelope is inherited and unchanged.
- Legacy-PQ1 through Legacy-PQ5 and Legacy-RQ1, RQ3 through RQ6 remain open product/release dispositions. Legacy-RQ2’s old feature estimate is superseded.
- Preserved numeric targets do not become current release blockers unless classified and activated through the current release plan, with the measurement and waiver discipline defined by Feature-NFR1 through Feature-NFR8.
- Product owner, platform architect, technical lead, pilot acceptance owner, and release owner revisit the assumptions at the triggers named in PRD §13.

#### Addendum gap dispositions

- Build in-pilot: the generic tenant-access projection handler, generic typed-HTTP-client registration, and the required public polymorphic registration support.
- Consume or extend in the platform-owned module: shared ServiceDefaults/telemetry hooks and Aspire/Dapr domain-service topology.
- Deferred backlog: compile-time command/event metadata, Tier-3 end-to-end harness, snapshot/upcasting hook, command-level authorization/validator discovery, and dead-letter/poison-pill domain hook.
- Cross-module candidates must be promoted only when Conversations consumes them; otherwise they remain follow-on backlog.

### PRD Completeness Assessment

The PRD is unusually thorough and internally explicit about scope, preservation semantics, measurement, ownership, and open-decision handling. All 20 refactor FRs, 104 preserved Feature-FRs, and 77 preserved Feature-NFRs are identifiable and source-numbered. Testable consequences accompany every refactor FR, and the addendum grounds the implementation seams and duplication evidence.

Readiness is conditional rather than absolute. OQ-1 must be resolved per FR-10 through FR-15 slice before implementation of that slice; FR-19/SM-2 lacks final reproducible acceptance evidence; several legacy product/release and technical-how decisions remain deliberately open; and many preserved numeric NFRs require separate release activation, classification, measurement envelopes, or accepted-unknown treatment. These do not automatically expand or block the refactor, but architecture, epics, and release records must preserve the dispositions exactly and must not present preserved requirements as delivered scope.


## Epic Coverage Validation

Coverage is evaluated against the active v8 append-only authority, not against superseded historical wording in Epics 1–5 or earlier overlays. A mapped story path proves planning coverage only; it does not imply that a backlog, paused, or in-progress story is complete.

### Epic FR Coverage Extracted

- FR-1 and FR-2: frozen inventory/baseline carried into Story 6.3 and validated by Story 6.6.
- FR-3: Stories 6.1, 6.2, and 6.6.
- FR-4 through FR-9: delivered historical surfaces revalidated through Stories 6.3 and 6.6.
- FR-10: Stories 6.1, 6.2, and 6.6.
- FR-11, FR-12, FR-14, and FR-15: Stories 6.1, 6.3, and 6.6.
- FR-13: Stories 6.1, 6.2, and 6.6.
- FR-16: explicitly deferred/non-activated through Story 6.3, validated by Story 6.6.
- FR-17: Stories 6.2, 6.5, and 6.6.
- FR-18: Story 6.5, validated by Story 6.6.
- FR-19: Stories 6.5 and 6.6.
- FR-20: Stories 6.3–6.6 plus mechanical/evidence/performance controls in Stories 6.8–6.12; Story 6.6 is final validation.
- Feature-FR1 through Feature-FR104: Story 6.3 must record each exactly once with evidence or approved non-activation; Story 6.6 validates the complete preservation contract.

**Total PRD functional requirements represented in current epic authority:** 124.

### Coverage Matrix

#### Initiative FRs

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Canonical boilerplate inventory exists and is accepted — A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability. | Stories 6.3 and 6.6; accepted inventory/baseline remains frozen | ✓ Covered |
| FR-2 | Classification disagreements are resolvable, not silent — A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale. | Stories 6.3 and 6.6; accepted inventory/baseline remains frozen | ✓ Covered |
| FR-3 | Domain-service host adoption — Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing. | Stories 6.1, 6.2, and 6.6 | ✓ Covered |
| FR-4 | Query handling via SDK query-handler + cursor seams — Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-5 | Read-model persistence via shared store + write policy — Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-6 | Projection handling via SDK projection seam — Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-7 | Aggregate scaffolding via base-class conventions — Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-8 | Serialization via shared converters / type registration — Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-9 | Testing via shared assertions/fakes/defaults — Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers. | Stories 6.3 and 6.6 revalidate the delivered historical surface | ✓ Covered |
| FR-10 | Platform-owned shared ServiceDefaults — The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Stories 6.1, 6.2, and 6.6 | ✓ Covered |
| FR-11 | Generic tenant-access projection handler + registration — A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules. | Stories 6.1, 6.3, and 6.6 | ✓ Covered |
| FR-12 | Shared client registration — A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation. | Stories 6.1, 6.3, and 6.6 | ✓ Covered |
| FR-13 | Platform-owned Aspire/Dapr domain-service hosting — The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Stories 6.1, 6.2, and 6.6 | ✓ Covered |
| FR-14 | Shared serialization metadata and polymorphic registration — A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition. | Stories 6.1, 6.3, and 6.6 | ✓ Covered |
| FR-15 | Diagnostics/telemetry scaffolding helper — A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies. | Stories 6.1, 6.3, and 6.6 | ✓ Covered |
| FR-16 | Compile-time command/event contract metadata *(deferred)* — Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative. | Story 6.3 records non-activation; Story 6.6 validates the disposition | ✓ Covered — deferred per PRD |
| FR-17 | Conversations consumes every in-scope shared capability — Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot. | Stories 6.2, 6.5, and 6.6 | ✓ Covered |
| FR-18 | Documented thin authoring template — A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module. | Story 6.5, validated by Story 6.6 | ✓ Covered |
| FR-19 | New-module authoring cost is measured — The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2. | Stories 6.5 and 6.6 | ✓ Covered |
| FR-20 | Behavior and contracts are provably preserved — Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change. | Stories 6.3–6.6 plus controls 6.8–6.12; Story 6.6 is final validation | ✓ Covered |

#### Preserved Product Feature-FRs

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| Feature-FR1 | Adopter systems can create a tenant-scoped conversation record. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR2 | Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR3 | The system can represent conversation lifecycle state and allowed transitions, including active, archived, or closed states and any release-approved behavior for reopening or sealing. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR4 | Adopter systems can append ordered messages to an existing conversation. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR5 | Adopter systems can add human users, AI agents, and LLMs as conversation participants. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR6 | Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR7 | The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR8 | Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR9 | Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR10 | Adopter systems can update conversation title or metadata when that capability is included in the active release scope. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR11 | Adopter systems can close or archive a conversation when that capability is included in the active release scope. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR12 | The system can preserve a complete conversation record across provider session expiry, restart, or failover. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR13 | The system can attribute each conversation action to a stable Party identity. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR14 | The system can model humans, AI agents, and LLMs as attributable participants. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR15 | The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR16 | The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR17 | The system can preserve multi-provider attribution when a conversation crosses provider boundaries. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR18 | The system can reconstruct who said or changed what, when, and under which tenant context. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR19 | Adopter systems can attach file references to a conversation without storing file binaries in Conversations. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR20 | Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR21 | Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR22 | The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR23 | The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR24 | The system can keep conversations readable and attributable when upstream entities change lifecycle state. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR25 | The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR26 | The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR27 | The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR28 | The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR29 | The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR30 | The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR31 | The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR32 | The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR33 | The system can derive projections from ordered conversation events. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR34 | The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR35 | The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR36 | The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR37 | The system can expose projection lag or documented freshness behavior when read models are asynchronous. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR38 | Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR39 | Published events can carry explicit schema and version metadata. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR40 | The system can reject unsupported event, command, or projection schema versions with typed documented errors. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR41 | The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR42 | Authorized systems can set or replace a conversation retention policy with rationale. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR43 | Authorized systems can mark conversation content as sensitive. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR44 | Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR45 | The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR46 | The system can preserve the audit event stream while redacting projected or displayed content. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR47 | The system can require every governance mutation to have a paired audit event. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR48 | The system can reject governance mutations when audit recording is unavailable. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR49 | The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR50 | The system can reconstruct message state and governance state as they existed at a prior point in time. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR51 | The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR52 | The system can apply retention and redaction policy treatment to governance audit records themselves. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR53 | The system can define which actions on audit records are allowed or denied and when the records can be redacted, exported, or separately logged. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR54 | The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR55 | Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR56 | Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR57 | Compliance operators can filter or narrow conversation search by date range and business context. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR58 | Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR59 | Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR60 | Compliance operators can view a conversation's governance audit trail inline. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR61 | Compliance operators can view conversation state as of a selected historical time. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR62 | Compliance operators can copy citation-ready references for transcript and audit elements. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR63 | Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR64 | Operator and compliance workflows marked read-only cannot mutate conversation aggregate state. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR65 | Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR66 | Operators can run governance verification for a conversation, tenant, suite, or time window. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR67 | Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR68 | Verification results can distinguish governance verification failures from infrastructure or execution failures. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR69 | The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR70 | Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR71 | Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR72 | Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR73 | Adopter developers can run adopter-facing conformance tests before deployment. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR74 | Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR75 | Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR76 | The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR77 | The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR78 | The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR79 | The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR80 | The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR81 | The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR82 | The product can produce a signed conformance artifact for release gating. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR83 | The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR84 | The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR85 | The product can support a named-waiver process for release-gate exceptions. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR86 | The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR87 | The product can verify tenant isolation using adversarial positive and negative cases. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR88 | The product can verify idempotent command behavior under duplicate or reordered commands. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR89 | The product can verify redaction-replay correctness across projections, logs, traces, and errors. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR90 | The product can verify provider portability by proving recoverability without provider-owned session authority. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR91 | The product can verify event schema evolution through version-aware records and at least one worked additive-change example. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR92 | The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR93 | The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR94 | The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR95 | Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR96 | Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR97 | Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR98 | Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR99 | Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR100 | The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR101 | The product can expose release-scope consequences when substrate-defining capabilities are deferred. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR102 | The product can support buyer partial acceptance under the Option A v1 deal. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR103 | The product can track second-adopter status and trigger downgrade-rule review milestones. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |
| Feature-FR104 | The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities. | Story 6.3 zero-gap preservation disposition; Story 6.6 final validation | ✓ Covered — preservation path |

### Missing Requirements

No PRD functional requirement lacks an epic/story traceability path under the v8 authority.

No unknown FR identifier appears in current authority. Stories 6.7–6.12 are enabling control/evidence work rather than new PRD FR namespaces. The older epic prefix’s derived NFR1–NFR8 labels are not additional functional requirements and do not replace Feature-NFR1–Feature-NFR77.

### Coverage Statistics

- Total PRD FRs: 124
- Initiative FRs covered: 20 of 20
- Preserved Feature-FRs with explicit disposition/validation paths: 104 of 104
- FRs missing from current epic authority: 0
- Coverage percentage: 100%
- Implementation completion percentage: not assessed in this step

## UX Alignment Assessment

### UX Document Status

**Found and fully reviewed:**

- `_bmad-output/planning-artifacts/ux-design-specification.md`
- `_bmad-output/planning-artifacts/ux-requirement-map.md`

The UX map inventories 52 decisions (`UX-DR1` through `UX-DR52`) and 28 explicit acceptance-criterion identifiers: 8 safety, 15 responsive, 2 accessibility, 1 leakage, 1 mobile, and 1 performance-safety criterion. Both documents identify their current disposition as `preserved-not-activated`; separate approved release authority is required to activate product UI implementation.

### UX ↔ PRD Alignment

| Concern | PRD authority | UX authority | Assessment |
| --- | --- | --- | --- |
| Current initiative scope | Refactor only; no UX redesign; Feature-FRs/NFRs are preservation constraints under FR-20 | Prominent preservation-only banner and per-item `preserved-not-activated` disposition | Aligned |
| Tenant-safe discovery and non-enumeration | Feature-FR26–32; Feature-NFR16–19 | UX-DR20–21, UX-DR31, UX-DR35–36, UX-DR40–51; AC-SAFE/AC-RESP/AC-LEAK | Aligned |
| Freshness and trust visibility | Feature-FR34–37; Feature-NFR44–48 | UX-DR3, 5, 9, 14, 22, 29–32, 47; trust primitives and precedence rules | Aligned |
| Governance, audit, redaction, evidence, and temporal reconstruction | Feature-FR42–69; Feature-NFR20–21, 38–48, 60–68 | UX-DR6–7, 10, 12–19, 25–38; evidence timeline, citation, redaction, command-gate contracts | Aligned |
| Accessibility and human trust | Feature-NFR69–77 | UX-DR39–52 plus AC-A11Y, AC-RESP, AC-MOB, AC-LEAK, and AC-PERF | Aligned |
| Performance targets | SM-C2 governs the refactor; Feature-NFR9/12 remain preserved unless separately activated | 90-second investigation goal and safe loading/virtualization requirements are preserved, not currently activated | Aligned; authorities remain distinct |
| Front-end ownership | FrontComposer-generated behavior preserved; no client-side trust inference | Generated-first FrontComposer/Fluent foundation with custom UI only for trust interpretation | Aligned |

The UX user journeys remain broader than the active boilerplate-refactor delivery, but the preservation banner and requirement map correctly prevent those historical/future journeys from becoming current feature scope.

### UX ↔ Architecture Alignment

- Architecture v8 cites the canonical PRD/addendum and both UX documents, preserves the same 52 decisions and 28 acceptance criteria, and assigns their zero-gap disposition contract to Story 6.4.
- FrontComposer owns generated baseline administration; custom components are reserved for evidence, citation, redaction, audit, freshness, temporal navigation, participant hydration, and command safety.
- Architecture requires server-owned permission-safe projections and command metadata, fail-closed tenant access, read-time Parties hydration, explicit freshness states, independent drawer authorization, and non-disclosure across DOM, ARIA, clipboard, telemetry, URLs, titles, responsive duplicates, and exports.
- Architecture supports UX performance needs through projection-shaped reads, batched/bounded Party hydration, precomputed trust posture where possible, and asynchronous paths for temporal reconstruction, export, verification, and rebuild.
- WCAG 2.1 AA, keyboard navigation, screen-reader trust ordering, high contrast, reduced motion, safe mobile triage, and responsive leak testing are explicit architectural requirements.

### Alignment Issues

No unplanned blocking UX/PRD/architecture contradiction was found.

The following planned gaps remain:

1. **Story 6.4 deliverables are not implemented.** The required `ux-preservation-disposition-v1` schema, authoritative JSON, deterministic Markdown projection, and `UxPreservationDispositionValidationTest` remain backlog work. Until they exist, the planning map supplies alignment but not final mechanically validated preservation evidence.
2. **Trust vocabulary must be normalized before any future UI activation.** UX uses several related dimensions—freshness, completeness, redaction, permission, citation, audit, participant resolution, and command eligibility—while architecture lists a candidate canonical trust-state vocabulary. Activation must produce shared contracts rather than collapse these dimensions or let components invent synonyms.
3. **Visual tokens require implementation-time conformance.** UX examples of conversation semantic tokens and an 8px spacing foundation must be implemented through FrontComposer/Fluent UI V5 parameters and Fluent 2 tokens; they must not become a parallel theme or recreate component-provided typography, color, or spacing.

### Warnings

- None of the 52 UX decisions or 28 acceptance criteria currently authorizes product UI implementation. Historical story references are provenance only.
- Story 6.4 must preserve every identifier exactly once and keep inactive UX work free of current implementation ownership.
- Preserved absolute UX/performance targets must not be substituted for, or silently merged with, the active SM-C2 refactor gate.
- Architecture’s global v8 implementation hold remains a broader readiness constraint; UX alignment alone does not lift it.

## Epic Quality Review

### Review Scope

The active v8 authority is reviewed as the current execution plan. Epics 1–5 and completed Stories 6.1, 6.2, and 6.7 are immutable history; defects in those records are noted only where they affect current execution. This review does not reopen completed implementation or evidence.

### Epic Structure Validation

| Epic | User-value focus | Independence | Current assessment |
| --- | --- | --- | --- |
| Epic 1 — Boilerplate Baseline & Behavior-Preservation Oracle | Release-owner safety value exists, but title/scope is a technical gate | Standalone and correctly precedes Epic 2 | Historical; technically framed but coherent |
| Epic 2 — Consume Existing Technical-Module Surface | Maintainer value is reduced module plumbing; title remains technical | Depends only on Epic 1 | Historical; sequence valid |
| Epic 3 — Promote → Adopt | Developer-platform reuse value exists; primarily a technical pipeline | Depends on Epic 2; original Story 2.6 forward dependency on Story 3.6 is explicitly recorded as historical | Historical; known structural defect superseded by Epic 6 |
| Epic 4 — Thin Authoring Template & Authoring-Cost Proof | Clear domain-author value | Depends on delivered platform adoption | Historical; user-value framing is acceptable |
| Epic 5 — Behavior-Preservation Attestation & Sign-off | Clear release-owner decision value | Capstone dependency on prior epics is intentional | Historical; acceptable capstone structure |
| Epic 6 — PRD Alignment And Preservation Reconciliation | Release-owner and maintainer safety value exists, but is buried beneath authority, tooling, UX governance, performance, projection, and evidence work | Depends on completed Epics 1–5 and contains a dense intra-epic graph | **Non-compliant technical mega-epic** |

### 🔴 Critical Violations

#### 1. Epic 6 is a multi-outcome technical mega-epic

Epic 6 combines planning-authority correction, submodule promotion governance, production-host migration, projection proof, final-record tooling, oracle restructuring, UX preservation governance, authoring-template work, evidence-boundary consolidation, performance redesign, manifest generation, and release attestation. These are distinct outcomes for different users and cannot be evaluated as one independently valuable epic.

**Impact:** Scope and completion risk concentrate in one epic; a failure in performance or evidence tooling blocks unrelated UX-governance or authoring-template value. The epic cannot deliver a coherent user outcome incrementally.

**Recommendation:** Publish an append-only successor plan that groups remaining work into outcome-oriented epics, for example: reliable completion/evidence tooling; preserved UX governance; thin-module authoring proof; universal performance restoration; complete preservation manifest; and release attestation. Keep completed history immutable and map every existing Story 6.x disposition to exactly one successor.

#### 2. Current story numbering contains forbidden forward dependencies

The active graph is acyclic, but several stories require later-numbered stories:

- Story 6.3 completion requires 6.9, 6.10, and 6.12.
- Story 6.4 completion requires 6.8.
- Story 6.5 completion requires 6.8 and 6.10.
- Story 6.6 requires 6.8 through 6.12 and is intentionally last.

The topological wave plan prevents accidental execution, but it does not satisfy the story-independence rule that earlier stories may use only already-delivered outputs.

**Impact:** Story identifiers no longer communicate executable order, earlier `in-progress` state can coexist with unmet future prerequisites, and tools or humans that assume ordinal sequencing can select non-completable work.

**Recommendation:** Create new, topologically ordered successor story identifiers for unfinished work, or formally demote 6.3–6.6 to non-executable parent outcomes and place independently completable child stories beneath them. Do not rely solely on prose status holds.

#### 3. Several active stories are epic-sized

- **6.8** combines generator design, solution/test discovery, Git/file/gitlink derivation, four workflow integrations, historical mode, and fault injection.
- **6.10** creates a new test-support project, hardened Git runner, manifest/ledger framework, static enforcement script, five workflow integrations, migration of at least 24 evidence readers, documentation, and fault injection.
- **6.11** combines an architecture ADR, derived-key redesign, production optimization, compatibility/rebuild behavior, signal-method design, four-row performance work, multi-tier correctness tests, and final evidence.
- **6.12** combines ADR 0004, historical validator correction, successor-proof generation, current-head governance, manifest handoff, full fault injection, and conformance closure.
- **6.5** explicitly contains three independently reviewable checkpoints—authoring contract, minimal fixture, and measurement/conclusion—but retains one all-or-nothing story.

**Impact:** Review surfaces are too broad, rollback boundaries are mixed, and a story can remain open through multiple independently valuable deliverables. This increases integration, evidence-staleness, and parallel-edit risk.

**Recommendation:** Promote the existing checkpoints and technical phases into independently completable stories with their own final records. Separate ADR/contract decisions from implementation, migration, and final evidence when each can be validated independently.

### 🟠 Major Issues

#### 1. Active acceptance criteria are not expressed as story-level BDD

V8 provides detailed numbered criteria and a six-scenario high-risk BDD catalogue, but the catalogue is not a one-to-one acceptance contract for every active story. Many criteria contain several independent assertions joined in a single item.

**Examples:** Story 6.10 AC8 combines migration scope, exemption policy, assertion-strength preservation, pinned constants, count preservation, and a special projection-proof boundary. Story 6.11 AC10 combines four performance verdicts with every correctness and execution-state gate. Story 6.12 AC8 combines three test lanes, zero-failure/skip/not-run semantics, and final-record generation.

**Recommendation:** Convert each active story’s criteria into atomic Given/When/Then scenarios or bind each atomic assertion to a named automated test/evidence field. Retain the high-risk catalogue as cross-story scenarios rather than as a substitute for story ACs.

#### 2. Story 6.6 is a release program gate, not an independently completable story

Story 6.6 depends on every unfinished corrective workstream and on the outcome of another independent readiness assessment. Its closure semantics are appropriate for a release gate, but not for a normal user story.

**Recommendation:** Model 6.6 as an epic/release exit gate or milestone consuming completed stories. Keep the attestation-generation work as a bounded story and keep the external readiness verdict as a separate gate decision.

#### 3. Some criteria carry dynamic or subjective scope

Examples include “any reader added before implementation,” “unchanged assertion strength,” “usable comparable signal,” and “one compatible candidate.” Supporting controls narrow these phrases, but the story text should bind the exact baseline identity, inventory, and decision algorithm before work begins.

**Recommendation:** Freeze versioned inventories and machine-readable pass algorithms at story entry, then reference those immutable identities from the ACs.

### 🟡 Minor Concerns

- The epic document’s historical prefix still contains superseded statements such as “no separate architecture document yet,” “UX N/A,” conditional FR-16, and the old approximate performance rule. V8 labels them historical, and the deterministic current execution view mitigates the risk, but generic document scanners may still read stale text.
- Several active ACs are long compound paragraphs. Even where testable, they are harder to review and trace than atomic clauses.
- The current global hold correctly overrides `in-progress` and `ready-for-dev` labels, but those labels remain cognitively misleading until the hold is lifted.

### Dependency Analysis

- The explicit graph is acyclic.
- No current dependency cycle was found.
- Completed spine `6.1 → 6.7 → 6.2` is valid.
- Stories 6.10, 6.11, and 6.12 are mutually independent after their stated predecessors, which supports parallel execution.
- Forward-numbered dependencies remain a critical structural violation despite the correct topological wave plan.
- No database/table-upfront violation exists; EventStore remains authoritative and Story 6.11 introduces derived keys only with an owning ADR and focused need.
- No starter-template setup story is required: this is a brownfield corrective initiative, not a greenfield repository bootstrap. Story 6.5 appropriately addresses the reusable thin template as a product outcome.

### Best-Practices Compliance Summary

| Check | Result |
| --- | --- |
| Epic delivers a coherent user outcome | ❌ Epic 6 mixes multiple technical/governance outcomes |
| Epic dependency direction | ✓ No future epic dependency |
| Story dependency direction | ❌ Several earlier-numbered stories depend on later-numbered stories |
| Stories appropriately sized | ❌ 6.5, 6.8, 6.10, 6.11, and 6.12 are oversized |
| Acceptance criteria atomic and BDD-oriented | ⚠ Detailed and testable in many places, but compound and not story-level BDD |
| Error and adversarial paths included | ✓ Strong failure injection and high-risk scenario coverage |
| Database/entity creation timed to need | ✓ No relational upfront-design violation |
| Brownfield integration and compatibility addressed | ✓ Platform boundaries, migrations, compatibility, and immutable history are explicit |
| FR traceability maintained | ✓ 124 of 124 PRD FRs have current paths |

### Required Remediation Before Implementation Readiness

1. Replace the unfinished portion of the Epic 6 mega-epic with an append-only, outcome-oriented and topologically ordered successor decomposition.
2. Split 6.5, 6.8, 6.10, 6.11, and 6.12 into independently completable stories or executable child stories with separate evidence and rollback boundaries.
3. Reclassify 6.6 as a release/epic exit gate and isolate bounded attestation-generation work.
4. Convert active story ACs into atomic BDD scenarios or exact machine-verifiable assertions bound to frozen inventories and algorithms.
5. Regenerate the deterministic current execution view and sprint-status projection from the corrected authority, then rerun implementation readiness.

## Summary and Recommendations

### Overall Readiness Status

## NOT READY

The planning set is strong on scope, traceability, preservation, architecture, UX alignment, and failure-mode rigor. All 124 PRD functional requirements have current traceability paths; architecture v8 resolves OQ-1 through OQ-5; UX authority consistently preserves 52 decisions and 28 acceptance criteria without activating product UI scope.

Implementation readiness nevertheless fails because the current executable work is not decomposed into independently completable, correctly ordered stories. The active v8 hold therefore remains in force. Proceeding as-is would violate both the epic-quality standard and the architecture’s explicit implementation prohibition.

### Critical Issues Requiring Immediate Action

1. **Epic 6 is a technical mega-epic.** It combines multiple independently valuable outcomes and unrelated delivery risks under one completion boundary.
2. **Earlier-numbered active stories depend on later-numbered stories.** The graph is acyclic, but Stories 6.3–6.6 are not independently executable in ordinal story order.
3. **Stories 6.5, 6.8, 6.10, 6.11, and 6.12 are oversized.** Several combine architectural decisions, production changes, broad migrations, tooling, evidence generation, and fault injection in one all-or-nothing unit.

### Major Issues

1. Active v8 acceptance criteria are detailed but compound and not expressed as story-level BDD or one-to-one machine-verifiable assertions.
2. Story 6.6 is a release-program exit gate, not an independently completable user story.
3. Several criteria depend on dynamic or subjective scope that must be frozen at story entry, including evidence-reader inventory, assertion strength, compatible-candidate identity, and signal-quality algorithms.

### UX And Governance Warnings

1. Story 6.4’s versioned UX disposition schema/JSON/Markdown and zero-gap validator remain planned rather than implemented.
2. Trust/freshness/completeness/redaction/permission dimensions require a normalized shared contract before any future UI activation.
3. UX semantic-token and spacing guidance must be implemented through FrontComposer, Fluent UI V5 parameters, and Fluent 2 tokens rather than a parallel theme.

### Recommended Next Steps

1. Publish an append-only authority correction that maps every unfinished Story 6.x obligation into outcome-oriented epics and topologically ordered successor stories while preserving completed history.
2. Split 6.5, 6.8, 6.10, 6.11, and 6.12 at their existing ADR, contract, implementation, migration, evidence, and fault-injection boundaries; give each executable unit separate acceptance evidence and rollback scope.
3. Reclassify Story 6.6 as a release/epic exit gate and isolate attestation generation as bounded implementation work.
4. Rewrite active ACs as atomic Given/When/Then scenarios or bind each assertion to a frozen machine-readable inventory, algorithm, test, and evidence field.
5. Regenerate and mechanically validate the canonical epic authority, deterministic execution view, UX map, and sprint-status projection from the corrected plan.
6. Rerun this readiness workflow against the corrected committed candidate. Only a fresh `READY` result may lift the v8 hold.

### Final Note

This assessment identifies 12 findings across two principal categories: 3 UX/governance execution gaps and 9 epic/story-quality issues (3 critical, 3 major, and 3 minor). The absence of missing FR coverage does not offset the critical execution-plan defects. Correct the decomposition before implementation resumes.

**Assessment date:** 2026-08-01  
**Assessor:** Codex — BMAD Implementation Readiness workflow
