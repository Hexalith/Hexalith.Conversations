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

These nested documents are the confirmed PRD assessment inputs. The reconciliation, editorial-review, decision-log, and memory-log files in the same folder are supporting artifacts rather than selected PRD authority.

### Architecture

- `_bmad-output/planning-artifacts/architecture.md` (113,510 bytes; modified 2026-08-01 10:21 CEST)

No sharded architecture version was found.

### Epics and Stories

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (107,483 bytes; modified 2026-08-01 10:21 CEST)

This nested file is the confirmed canonical epic and story plan. The filename-matched `sprint-change-proposal-2026-07-14-epic-5-final-record-check.md` is not selected as a competing epic plan.

### UX Design

- `_bmad-output/planning-artifacts/ux-design-specification.md` (117,645 bytes; modified 2026-05-31 09:30 CEST)
- `_bmad-output/planning-artifacts/ux-requirement-map.md` (8,825 bytes; modified 2026-05-31 09:30 CEST)

These files are confirmed as complementary UX inputs.

### Discovery Resolution

- Whole-versus-sharded duplicates: none.
- Missing required document types: none after confirmation of the nested PRD and epic files.
- Unresolved document-selection conflicts: none.
## PRD Analysis

### Functional Requirements

The PRD defines two requirement namespaces. FR-1 through FR-20 are the active boilerplate-refactoring requirements. Feature-FR1 through Feature-FR104 are preserved product-contract requirements that constrain FR-20 and SM-C1; preservation does not by itself mean implemented, shipped, accepted, scheduled, or activated for this initiative.

#### Initiative Refactoring Requirements

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

**Initiative functional requirements: 20**

#### Preserved Product Functional Requirements

##### Conversation Lifecycle

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

##### Participant Attribution

- **Feature-FR13:** The system can attribute each conversation action to a stable Party identity.
- **Feature-FR14:** The system can model humans, AI agents, and LLMs as attributable participants.
- **Feature-FR15:** The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth.
- **Feature-FR16:** The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- **Feature-FR17:** The system can preserve multi-provider attribution when a conversation crosses provider boundaries.
- **Feature-FR18:** The system can reconstruct who said or changed what, when, and under which tenant context.

##### Business Context And References

- **Feature-FR19:** Adopter systems can attach file references to a conversation without storing file binaries in Conversations.
- **Feature-FR20:** Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier.
- **Feature-FR21:** Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery.
- **Feature-FR22:** The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities.
- **Feature-FR23:** The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state.
- **Feature-FR24:** The system can keep conversations readable and attributable when upstream entities change lifecycle state.
- **Feature-FR25:** The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available.

##### Tenant Access And Isolation

- **Feature-FR26:** The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record.
- **Feature-FR27:** The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- **Feature-FR28:** The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists.
- **Feature-FR29:** The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure.
- **Feature-FR30:** The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling.
- **Feature-FR31:** The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail.
- **Feature-FR32:** The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results.

##### Event Sourcing, Projections, And Publication

- **Feature-FR33:** The system can derive projections from ordered conversation events.
- **Feature-FR34:** The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state.
- **Feature-FR35:** The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version.
- **Feature-FR36:** The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-FR37:** The system can expose projection lag or documented freshness behavior when read models are asynchronous.
- **Feature-FR38:** Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version.
- **Feature-FR39:** Published events can carry explicit schema and version metadata.
- **Feature-FR40:** The system can reject unsupported event, command, or projection schema versions with typed documented errors.
- **Feature-FR41:** The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events.

##### Governance And Audit

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

##### Operator And Compliance Workflows

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

##### Consumer Contracts And Developer Experience

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

##### Compatibility, Evidence, And Release Gates

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

##### Observability And Operations

- **Feature-FR95:** Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data.
- **Feature-FR96:** Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data.
- **Feature-FR97:** Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data.
- **Feature-FR98:** Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content.
- **Feature-FR99:** Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates.

##### Scope Boundaries And Lifecycle Commitments

- **Feature-FR100:** The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release.
- **Feature-FR101:** The product can expose release-scope consequences when substrate-defining capabilities are deferred.
- **Feature-FR102:** The product can support buyer partial acceptance under the Option A v1 deal.
- **Feature-FR103:** The product can track second-adopter status and trigger downgrade-rule review milestones.
- **Feature-FR104:** The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities.

**Preserved product functional requirements: 104**

**Total functional requirements extracted: 124**

### Non-Functional Requirements

#### Current Refactor Cross-Cutting NFRs

- **Behavior preservation:** FR-20 / SM-C1 are authoritative for the dominant NFR and its frozen denominator.
- **Performance:** SM-C2 is authoritative. Shared capabilities must not introduce synchronous cross-service calls on hot paths or unbounded history loads; snapshot/projection behavior is preserved.
- **Fail-closed invariants:** promoted tenant-access and authorization capabilities must preserve fail-closed semantics by construction; cross-tenant access remains impossible and adversarially tested.
- **Observability:** metric names, dimensions, and health endpoints are preserved through platform-owned shared telemetry/ServiceDefaults so existing dashboards/alerts keep working.
- **Replay safety:** promoted projection/event handling must remain idempotent and tolerant of duplicate/out-of-order delivery (Dapr at-least-once).

#### Preserved Product Non-Functional Requirements

Numeric targets below preserve their target definitions but do not assert that evidence currently passes, that the target has been classified as a release blocker, or that a waiver exists.

##### Measurement, Evidence, And Waiver Discipline

- **Feature-NFR1:** Each NFR must identify its verification artifact type and responsible lifecycle stage: design review, automated test, load/performance test, operational drill, release evidence, or accessibility validation.
- **Feature-NFR2:** Every release-gated NFR must map to at least one automated verification artifact, one evidence file, and one release decision status: `pass`, `fail`, `waived`, or `unknown-accepted`.
- **Feature-NFR3:** Every NFR with a numeric target must name the measurement method, test environment class, and pass/fail interpretation before it can be used as a release gate.
- **Feature-NFR4:** Implementation for GA cannot begin until unresolved capacity and latency targets are converted into explicit numeric thresholds or marked as buyer-accepted unknowns with a named owner and review date.
- **Feature-NFR5:** Numeric targets must be classified as `Release blocker`, `Validation target`, or `Capacity discovery target` before implementation kickoff.
- **Feature-NFR6:** Any missed numeric threshold or untested risk requires named approver, expiry date, compensating control, and buyer acceptance if customer-facing.
- **Feature-NFR7:** A shared NFR measurement envelope must define data volume, tenant count, concurrent users, event count per conversation, projection state, cache state, deployment shape, storage backend, and network locality. Latency and capacity NFRs must reference this envelope.
- **Feature-NFR8:** Conformance evidence must include test environment identity, dataset scale, tool versions, build hash, schema/event versions, timestamped evidence links, and release manifest reference.

##### Performance

- **Feature-NFR9:** Opening a conversation with full context must complete at P95 <= 500ms for conversations up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- **Feature-NFR10:** The P95 open-conversation target must explicitly include or exclude authorization, projection read, redaction filtering, temporal evidence lookup, and provenance metadata before it becomes release-gated.
- **Feature-NFR11:** Cold-start conversation load must have a separately measured target before GA and must not be reported under warm-cache benchmarks.
- **Feature-NFR12:** Operator/admin search workflows must complete within 90 seconds for defined investigation scenarios, including user interaction steps.
- **Feature-NFR13:** Backend query latency, projection freshness, and result explainability thresholds that support the 90-second operator workflow must be defined separately.
- **Feature-NFR14:** Append-message latency must be benchmarked under duplicate/idempotent command load with tenant validation, persistence, audit behavior where applicable, and publication boundary included as defined by architecture.
- **Feature-NFR15:** Append timing must distinguish command accepted, event persisted, audit recorded, publication enqueued, and projection visible rather than collapsing all stages into one ambiguous number.

##### Security And Privacy

- **Feature-NFR16:** Tenant isolation failures are release blockers; missing, stale, ambiguous, mismatched, or unknown tenant context must fail closed before aggregate or projection access.
- **Feature-NFR17:** Tenant isolation must be tested with positive and adversarial negative cases, including cross-tenant ID guessing, replayed commands from another tenant, poisoned projection events, malformed metadata, and mixed-tenant rebuild attempts.
- **Feature-NFR18:** Cross-tenant reads, writes, replay, rebuild, search, diagnostics, audit access, and admin operations must fail closed with content-safe responses.
- **Feature-NFR19:** Error messages, logs, metrics, traces, diagnostics, and conformance output must not leak target tenant IDs, inaccessible Party IDs, conversation existence, redacted content, provider payloads, or cross-tenant business references.
- **Feature-NFR20:** Governance mutations must fail closed when audit writing is unavailable; queued unaudited governance writes are not allowed.
- **Feature-NFR21:** Redacted content must not reappear in primary projections, search indexes if any, audit views, caches, exported reports, temporal views, replay/rebuild outputs, logs, traces, errors, or observability payloads where content may appear.

##### Reliability, Resilience, And Recovery

- **Feature-NFR22:** The system must tolerate duplicate, reordered, and retried commands without producing divergent projections or duplicate business effects.
- **Feature-NFR23:** Pub/sub behavior must be tested with at-least-once delivery, induced duplicates, reordering, subscriber-visible replay, idempotency expectations, and deduplication-window expiry.
- **Feature-NFR24:** Pub/sub publication failures must define retry, dead-letter, replay, and subscriber notification behavior before GA.
- **Feature-NFR25:** DAPR sidecar restart, EventStore partition/degradation, projection-rebuilder crash/resume, projection lag breach, dead-letter replay, audit-sink degradation, and redaction propagation failure must be covered by operational drills before GA unless explicitly waived.
- **Feature-NFR26:** A failure-mode matrix must cover dependency failure, expected command behavior, retry policy, dead-letter behavior, operator signal, and recovery validation for DAPR, EventStore, projections, pub/sub, tenant projection, and audit sink failures.
- **Feature-NFR27:** Verification tooling must distinguish product invariant failures from infrastructure or execution failures.
- **Feature-NFR28:** The system must define and verify RPO/RTO targets for conversation event storage, projection stores, audit evidence, and configuration/state required for replay.
- **Feature-NFR29:** Backup restore and tenant-scoped recovery procedures must be tested before production release.

##### Scalability, Capacity, And Cost

- **Feature-NFR30:** The PRD must define pre-kickoff numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification budget, and concurrent opens/sec/tenant.
- **Feature-NFR31:** Projection rebuild time must be measured at 1M, 10M, and 100M events with pass/fail thresholds set before implementation kickoff.
- **Feature-NFR32:** Projection rebuild requirements are tiered: 1M-event rebuild is MVP-required, 10M-event rebuild is pre-scale validation, and 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- **Feature-NFR33:** Long-running projection rebuilds must support progress reporting, resumability, and safe tenant-scoped cancellation or isolation.
- **Feature-NFR34:** Tenant-events lag must have an SLO and a defined request behavior during lag windows.
- **Feature-NFR35:** Redaction propagation latency must have an SLO covering all materialization surfaces listed in Feature-NFR21.
- **Feature-NFR36:** The system must expose cost-relevant capacity indicators, including storage growth per event, projection write amplification, rebuild resource usage, pub/sub throughput, and per-tenant activity distribution.
- **Feature-NFR37:** Pre-kickoff numeric cost thresholds must be defined or explicitly accepted as unknowns.

##### Data Integrity And Event Sourcing

- **Feature-NFR38:** v1 projections must be rebuildable from the persisted event stream and produce functionally equivalent read models for the same tenant, conversation, event history, and contract version.
- **Feature-NFR39:** Deterministic rebuild must reproduce projection state and evidence references from the same ordered event stream, excluding non-deterministic runtime metadata unless explicitly persisted.
- **Feature-NFR40:** Persisted and published events must carry schema/version metadata, and unsupported versions must fail with typed documented errors.
- **Feature-NFR41:** Event schema evolution must include one worked additive-change example before GA.
- **Feature-NFR42:** Temporal evidence links must state which anchor is authoritative: event position, projection version, timestamp, or contract-defined composite.
- **Feature-NFR43:** Temporal reconstruction must be deterministic enough that temporal evidence links resolve to the same legally meaningful state.

##### Projection Freshness

- **Feature-NFR44:** Projection freshness metadata must be exposed consistently across consumer APIs, operator views, diagnostics, and verification output.
- **Feature-NFR45:** Projection freshness metadata must use a standard shape such as `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, and `lagDuration`; otherwise, the system must document why an equivalent shape is not available.
- **Feature-NFR46:** The system must define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation.
- **Feature-NFR47:** Operator/admin surfaces must clearly distinguish normal, delayed, degraded, blocked, redacted, replaying, and partially rebuilt states without requiring log access. Each state must expose tenant scope, freshness timestamp, and recommended next action.
- **Feature-NFR48:** During projection lag, rebuild, replay, retry, dead-letter, or audit-sink degradation, the system must show stable trust signals: last known good state, current processing status, whether user-visible data is complete, and whether operator action is required.

##### Integration And Compatibility

- **Feature-NFR49:** Contract compatibility must be validated with executable tests covering commands, queries/projections, emitted events, errors, version discovery, and at least one adopter-style CORE fixture.
- **Feature-NFR50:** Provider portability must be verified by stripping or changing provider-owned correlation identifiers without losing recoverable conversation history.
- **Feature-NFR51:** Provider portability tests must cover contract-level behavior, persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence.
- **Feature-NFR52:** Provider-specific operational configuration may vary, but tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism must remain invariant.
- **Feature-NFR53:** The .NET client and contract package must expose the same typed error semantics and compatibility status as the raw service contract.
- **Feature-NFR54:** Front-end composition metadata must remain provenance metadata, not a required coupling to one UI implementation.

##### Operability And Observability

- **Feature-NFR55:** Operators must be able to observe command rejection counts by reason, projection lag, event publication failures, tenant isolation denials, privileged access attempts, and conformance outcomes.
- **Feature-NFR56:** Operational signals must be tenant-safe and content-safe by default.
- **Feature-NFR57:** Observability cardinality must be bounded so tenant, conversation, Party, provider, and error dimensions do not create unbounded metrics or logs.
- **Feature-NFR58:** Observability dimensions must not include conversation ID, user free-text, raw business record identifiers, prompt/content fragments, or unbounded error strings. Tenant ID may be used only when approved by privacy/governance policy.
- **Feature-NFR59:** Output from `governance verify` and other conformance verification must be machine-readable and suitable for CI and incident workflows.
- **Feature-NFR60:** Privileged operational actions must include structured justification and produce reviewable audit records.
- **Feature-NFR61:** Privileged operational access must be reviewed periodically, with stale justifications or unexplained access attempts treated as audit findings.

##### Compliance, Retention, And Release Evidence

- **Feature-NFR62:** Tenant isolation, audit integrity, redaction non-leakage, unsupported schema rejection, projection rebuild determinism, and contract breakage are automatic release blockers unless explicitly waived through the named-waiver process.
- **Feature-NFR63:** Every release must produce a signed conformance artifact and a versioned manifest that maps tests to FRs, NFRs, carry-forward commitments, and pass criteria and records waiver status, measurement method, and environment.
- **Feature-NFR64:** Module-level compliance evidence must clearly identify which controls belong to Conversations and which are inherited from Hexalith platform controls.
- **Feature-NFR65:** Audit-record access, export, redaction, tamper attempts, and privileged-view behavior must be covered by explicit tests.
- **Feature-NFR66:** The system must define retention, archival, deletion, and legal-hold behavior for conversation events, projections, audit records, redaction records, and derived materializations.
- **Feature-NFR67:** Retention behavior must be tenant-aware and produce verifiable evidence.
- **Feature-NFR68:** Release and conformance evidence must be navigable by non-developer approvers. Machine-readable artifacts remain authoritative, but admin evidence views must summarize pass/fail status, blocker reason, scope, timestamp, signer, and linked verification output.

##### Accessibility And Human Trust

- **Feature-NFR69:** Operator/admin web surfaces generated or composed through Hexalith UI mechanisms must meet WCAG 2.1 AA expectations for keyboard navigation, focus order, contrast, and screen-reader-readable audit/redaction state.
- **Feature-NFR70:** Accessibility scope applies to operator/admin web surfaces only; machine APIs, raw logs, and exported raw evidence are excluded unless rendered in UI.
- **Feature-NFR71:** Redaction, temporal state, tenant scope, warning states, degraded states, empty states, and evidence review status must not rely on color alone.
- **Feature-NFR72:** Citation copy, evidence navigation, audit search, verification result review, degraded-mode banners, and error-state workflows must be usable without pointer-only interactions.
- **Feature-NFR73:** Accessibility verification must include automated checks plus manual keyboard-only walkthrough and screen-reader pass.
- **Feature-NFR74:** Screen-reader announcements must cover meaningful state changes in error, degraded, evidence review, and audit search workflows.
- **Feature-NFR75:** Usability verification must include at least one scenario where an operator diagnoses a delayed or blocked conversation projection and one scenario where an admin reviews failed release evidence. Target: correct diagnosis and next action within 90 seconds without developer assistance.
- **Feature-NFR76:** Fail-closed authorization, governance, redaction, audit, and publication failures must return content-safe explanations that identify failure class, affected operation, retryability, and escalation path.
- **Feature-NFR77:** User-facing degraded-mode and compliance-blocker messages must avoid ambiguous or panic-inducing language. Users must be able to identify whether data is safe, stale, hidden, unavailable, or awaiting governance action.

**Preserved numbered non-functional requirements: 77**

**Additional current cross-cutting NFR statements: 5**

### Additional Requirements

- The initiative is a behavior-preserving refactor, not a feature release. No new Conversations behavior, persistence model, transport, provider, or customer-visible semantic change is authorized.
- FR-16 is explicitly deferred. Fleet migration and any promoted capability that Conversations does not consume remain follow-on work.
- OQ-1 remains an architecture dependency: the platform architect must resolve the landing zone for each FR-10 through FR-15 capability before its implementation story starts.
- Shared-module changes must be additive and backward-compatible for existing consumers. Generic hosting, AppHost, Aspire, DAPR, ServiceDefaults, projection/query runtime, telemetry scaffolding, and subscription plumbing remain platform/domain-service SDK responsibilities.
- The accepted SM-1 baseline is 13,289 classified plumbing LOC out of 35,769 source LOC. The addendum's other discovery figures are approximate and require architecture-stage confirmation.
- FR-20 freezes the pre-refactor preservation denominator, contract baselines, source/build identity, and named approval process. Tests cannot be silently removed, replaced, or reclassified.
- SM-C2 permits no more than a 5% P95 regression on identified hot paths under the same reproducible benchmark envelope.
- The preserved product baseline contains unresolved product, release, and technical dispositions. These remain constraints or decision inputs, but do not expand the active refactor scope.
- Legacy technical questions remain open for transport choice, idempotency-key mechanism, stale-tenant-projection status/retry semantics, pub/sub topic naming, audit-pairing health exposure, and the possible raw-HTTP release exception.
- Root-declared submodule coordination is authorized only where required by this initiative; nested recursive submodule work remains prohibited.

### PRD Completeness Assessment

The PRD is unusually strong in requirement identity, scope separation, testable consequences, preservation policy, measurement definitions, and ownership boundaries. It clearly separates 20 active initiative requirements from 104 preserved product requirements and 77 preserved product NFRs, which prevents the legacy product contract from silently becoming refactor delivery scope.

The principal readiness risks are decision and evidence risks rather than missing requirement text: OQ-1 landing zones must be resolved before affected stories start; SM-2 evidence remains provisional pending the reproducible FR-19 fixture and measurement artifact; preserved numeric NFRs still require activation and release-gate classification where applicable; and multiple legacy product/release/technical dispositions remain explicitly open. Epic coverage must therefore distinguish active initiative delivery from preservation evidence and conditional legacy obligations.
## Epic Coverage Validation

### Coverage Basis

The active v7 corrective overlay supersedes the original Epic 1-5 plan where stated. It declares the initiative surface as exactly FR-1 through FR-20, activates FR-1 through FR-15 and FR-17 through FR-20, and keeps FR-16 as the sole deferred initiative requirement. It also freezes all 104 Feature-FRs as preservation obligations without activating them for new feature delivery.

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Canonical boilerplate inventory exists and is accepted — A maintainer can read a single inventory artifact that lists every Conversations source area with its Consume/Promote/Keep classification, evidence (file paths, approximate LOC), and — for Promote/Consume — its target technical-module capability. | Epic 1 historical delivery; Story 6.3 current manifest | ✓ Covered |
| FR-2 | Classification disagreements are resolvable, not silent — A reviewer can challenge any Consume/Promote/Keep call, and the resolution is recorded with rationale. | Epic 1 historical delivery; Story 6.3 current manifest | ✓ Covered |
| FR-3 | Domain-service host adoption — Conversations operates through the platform-owned shared domain-service hosting capability instead of owning domain-agnostic runtime-host plumbing. | Epic 6, Stories 6.1, 6.2, 6.6 | ✓ Covered |
| FR-4 | Query handling via SDK query-handler + cursor seams — Conversations delegates domain-agnostic query execution and pagination-token protection to shared platform capabilities while retaining conversation-specific filters, authorization, and response contracts. | Epic 2 historical delivery; Epic 6, Stories 6.3, 6.6 | ✓ Covered |
| FR-5 | Read-model persistence via shared store + write policy — Conversations delegates domain-agnostic read-model persistence, concurrency control, and update coordination to the shared platform capability while retaining conversation-specific read-model contents and update semantics. | Epic 2 historical delivery; Epic 6, Stories 6.3, 6.6 | ✓ Covered |
| FR-6 | Projection handling via SDK projection seam — Conversations delegates domain-agnostic projection execution and rebuild coordination to the shared platform capability while retaining which fields, metadata, freshness semantics, and evidence each projection emits. | Epic 2 historical delivery; Epic 6, Stories 6.2, 6.3, 6.6 | ✓ Covered |
| FR-7 | Aggregate scaffolding via base-class conventions — Conversations delegates domain-agnostic aggregate command routing and state reconstruction to the shared platform aggregate capability while retaining all conversation command, state, event, and invariant behavior. | Epic 2 historical delivery; Epic 6, Stories 6.3, 6.6 | ✓ Covered |
| FR-8 | Serialization via shared converters / type registration — Conversations delegates domain-agnostic serialization registration and conversion to shared platform capabilities while retaining converters and metadata that encode conversation-specific rules. | Epic 2 historical delivery; Epic 6, Stories 6.3, 6.6 | ✓ Covered |
| FR-9 | Testing via shared assertions/fakes/defaults — Conversations test projects consume shared platform test infrastructure instead of duplicating equivalent hosting fixtures, fakes, and assertion helpers. | Epic 2 historical delivery; Epic 6, Stories 6.3, 6.6 | ✓ Covered |
| FR-10 | Platform-owned shared ServiceDefaults — The platform host provides shared observability, health, resilience, and service-discovery behavior. Conversations consumes that existing platform capability and supplies only conversation-specific telemetry definitions; if generic behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 6, Stories 6.1, 6.2, 6.6 | ✓ Covered |
| FR-11 | Generic tenant-access projection handler + registration — A domain module consumes a shared tenant-access projection capability for domain-agnostic processing and registration while supplying only its domain-specific contracts and rules. | Epic 3 historical delivery; Epic 6, Stories 6.1, 6.3, 6.6 | ✓ Covered |
| FR-12 | Shared client registration — A domain module consumes a shared, domain-agnostic client-registration capability instead of copying equivalent registration and configuration validation. | Epic 3 historical delivery; Epic 6, Stories 6.1, 6.3, 6.6 | ✓ Covered |
| FR-13 | Platform-owned Aspire/Dapr domain-service hosting — The platform AppHost hosts Conversations through the existing platform-owned domain-service hosting capability in each supported infrastructure mode. Conversations supplies only its domain identity and configuration; if generic topology behavior required by Conversations is absent, it is added to the platform capability, never to a Conversations-owned hosting module. | Epic 6, Stories 6.1, 6.2, 6.6 | ✓ Covered |
| FR-14 | Shared serialization metadata and polymorphic registration — A domain module declares only its domain-specific serializable contract set and consumes shared platform support for registration and composition. | Epic 3 historical delivery; Epic 6, Stories 6.1, 6.3, 6.6 | ✓ Covered |
| FR-15 | Diagnostics/telemetry scaffolding helper — A domain module consumes shared observability instrumentation support while supplying only its domain metric contract, including established metric names and bounded dimension vocabularies. | Epic 3 historical delivery; Epic 6, Stories 6.1, 6.3, 6.6 | ✓ Covered |
| FR-16 | Compile-time command/event contract metadata *(deferred)* — Shared compile-time command/event contract metadata is deferred from this pilot. It remains a backlog candidate for replacing duplicated domain/type identity declarations in a future, separately approved initiative. | Epic 3 Story 3.7 and Epic 6 authority: explicitly deferred/non-activated; Story 6.3 disposition | ✓ Covered by explicit deferred disposition |
| FR-17 | Conversations consumes every in-scope shared capability — Conversations depends on and uses each in-scope shared capability added or extended under FR-10..FR-15; no superseded local copy remains. Deferred FR-16 is excluded from this pilot. | Epic 6, Stories 6.2 and 6.5 | ✓ Covered |
| FR-18 | Documented thin authoring template — A developer can follow a documented authoring template — minimal module skeleton + a checklist of the shared capabilities to wire — to stand up a new domain module. | Epic 6, Story 6.5 | ✓ Covered |
| FR-19 | New-module authoring cost is measured — The initiative records the authoring cost of a minimal domain module on the template (file count / LOC for a do-nothing-but-valid module) as the baseline for SM-2. | Epic 6, Stories 6.5 and 6.6 | ✓ Covered |
| FR-20 | Behavior and contracts are provably preserved — Before the first refactor change, the initiative produces and versions a preservation manifest from an accepted green pre-refactor build. The manifest binds the source commit/build identity, the public/adopter-facing contract baselines, and the exact set of passing release-gate conformance tests that form the preservation denominator. The refactored module must pass 100% of that frozen denominator with no unapproved public-contract shape change. | Epic 6, Stories 6.3 and 6.6, with Stories 6.8, 6.9, and 6.12 controls | ✓ Covered |
| Feature-FR1 | Adopter systems can create a tenant-scoped conversation record. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR2 | Each conversation has a stable tenant-scoped internal identity distinct from external business identifiers, provider identifiers, UI labels, or thread names. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR3 | The system can represent conversation lifecycle state and allowed transitions, including active, archived, or closed states and any release-approved behavior for reopening or sealing. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR4 | Adopter systems can append ordered messages to an existing conversation. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR5 | Adopter systems can add human users, AI agents, and LLMs as conversation participants. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR6 | Adopter systems can submit idempotent commands and receive stable outcomes for duplicate submissions. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR7 | The system can reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with typed documented failure semantics. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR8 | Adopter systems can retrieve a conversation with its participant set, ordered message timeline, attachment references, governance state, and read-model freshness context. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR9 | Adopter systems can list conversations within a tenant using business context such as project, external identifier, or recent activity. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR10 | Adopter systems can update conversation title or metadata when that capability is included in the active release scope. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR11 | Adopter systems can close or archive a conversation when that capability is included in the active release scope. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR12 | The system can preserve a complete conversation record across provider session expiry, restart, or failover. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR13 | The system can attribute each conversation action to a stable Party identity. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR14 | The system can model humans, AI agents, and LLMs as attributable participants. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR15 | The system can preserve provider correlation identifiers as attribution metadata without treating them as the source of truth. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR16 | The system can preserve provider-specific payload metadata only as opaque, tenant-isolated, explicitly versioned extension data. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR17 | The system can preserve multi-provider attribution when a conversation crosses provider boundaries. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR18 | The system can reconstruct who said or changed what, when, and under which tenant context. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR19 | Adopter systems can attach file references to a conversation without storing file binaries in Conversations. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR20 | Adopter systems can associate a conversation with upstream business entities such as projects and folders by stable identifier. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR21 | Adopter systems can associate conversations with external business identifiers that support later tenant-scoped discovery. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR22 | The system can distinguish external business identifiers, used as stable correlation keys, from business references, used as domain links to upstream-owned entities. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR23 | The system can resolve upstream Party, Project, Folder, and attachment references at read time using upstream canonical state. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR24 | The system can keep conversations readable and attributable when upstream entities change lifecycle state. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR25 | The system can provide explicit migration-boundary guidance when records fall outside Conversations coverage, including known coverage start date or handoff target when available. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR26 | The system can require tenant context for every command, event, projection, query, pub/sub message, and audit record. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR27 | The system can reject requests before aggregate or projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR28 | The system can prevent cross-tenant enumeration and avoid revealing whether another tenant's conversation exists. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR29 | The system can make unauthorized, nonexistent, and cross-tenant records indistinguishable to non-privileged consumers unless policy explicitly permits disclosure. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR30 | The system can return typed tenant-isolation and tenant-binding errors suitable for adopter handling. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR31 | The system can ensure SRE or operator actions that affect tenant data are attributed and recorded into each affected tenant's audit trail. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR32 | The system can publish tenant-aware conversation events and projection notifications without leaking cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR33 | The system can derive projections from ordered conversation events. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR34 | The system can expose enough read-model metadata for consumers and operators to understand replay position, projection version, or equivalent freshness state. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR35 | The system can rebuild v1 projections from the persisted event stream and produce functionally equivalent read models for the same event history, tenant scope, conversation scope, and contract version. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR36 | The system can define projection consistency and freshness semantics, including current, stale, rebuilding, unavailable, and intentionally hidden by tenant isolation. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR37 | The system can expose projection lag or documented freshness behavior when read models are asynchronous. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR38 | Downstream systems can consume published conversation domain events for meaningful state changes according to the active contract version. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR39 | Published events can carry explicit schema and version metadata. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR40 | The system can reject unsupported event, command, or projection schema versions with typed documented errors. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR41 | The system can define compatible evolution rules, unsupported-version behavior, and migration or upcaster boundaries for persisted and published events. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR42 | Authorized systems can set or replace a conversation retention policy with rationale. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR43 | Authorized systems can mark conversation content as sensitive. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR44 | Authorized systems can redact message content with actor, timestamp, rationale, and policy attribution. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR45 | The system can distinguish logical deletion or archival, retention policy enforcement, redaction of sensitive content, legal-hold deferral, and immutable audit or event history. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR46 | The system can preserve the audit event stream while redacting projected or displayed content. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR47 | The system can require every governance mutation to have a paired audit event. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR48 | The system can reject governance mutations when audit recording is unavailable. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR49 | The system can allow non-governance conversation activity to continue during audit degradation only when the command does not mutate governance state. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR50 | The system can reconstruct message state and governance state as they existed at a prior point in time. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR51 | The system can make audit records citeable with stable identifiers, timestamps, actor attribution, tenant identity, conversation identity, and integrity metadata. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR52 | The system can apply retention and redaction policy treatment to governance audit records themselves. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR53 | The system can define which actions on audit records are allowed or denied and when the records can be redacted, exported, or separately logged. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR54 | The system can record structured justification for privileged operational actions that touch tenant-scoped conversation data. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR55 | Operators can review privileged-action justification, actor, timestamp, tenant, affected conversation, policy basis, and resulting audit event as one coherent record. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR56 | Compliance operators can find tenant-scoped conversations by external identifiers such as customer, account, or case ID. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR57 | Compliance operators can filter or narrow conversation search by date range and business context. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR58 | Compliance operators can read a reconstructed transcript with participants, messages, attachments, redactions, governance state, tenant scope, policy outcomes, and projection freshness. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR59 | Compliance operators can inspect inline redaction attribution for who redacted content, when, why, and under which policy. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR60 | Compliance operators can view a conversation's governance audit trail inline. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR61 | Compliance operators can view conversation state as of a selected historical time. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR62 | Compliance operators can copy citation-ready references for transcript and audit elements. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR63 | Compliance operators can open stable temporal evidence links that resolve to the same conversation state, time-travel cursor, projection version, event position, timestamp, or business-record reference as defined by the contract. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR64 | Operator and compliance workflows marked read-only cannot mutate conversation aggregate state. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR65 | Any privileged operator action that mutates metadata, visibility, policy state, audit records, or governance state can be explicitly classified and separately audited. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR66 | Operators can run governance verification for a conversation, tenant, suite, or time window. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR67 | Operators can receive structured verification results for audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, and related conformance checks. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR68 | Verification results can distinguish governance verification failures from infrastructure or execution failures. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR69 | The product can provide a self-serve buyer acceptance demo using seeded data that exercises redaction, time-travel, citation copy, and cross-tenant denial. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR70 | Adopter developers can integrate through a published contract package that defines commands, projections, events, and typed errors. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR71 | Adopter developers can use a supported .NET client for the v1 integration path unless the buyer explicitly accepts raw HTTP fallback. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR72 | Adopter developers can execute a minimal happy path to create a conversation, append a message, and read the timeline. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR73 | Adopter developers can run adopter-facing conformance tests before deployment. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR74 | Adopter developers can rely on documented tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, and governance behavior. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR75 | Adopter systems can discover the active contract version and compatibility status for commands, projections, events, and client packages. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR76 | The system can expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR77 | The product can provide actionable onboarding diagnostics for missing CORE preconditions, unsupported contracts, missing tenant context, provider configuration gaps, projection subscription failures, and schema incompatibilities. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR78 | The product can provide adopter-facing remediation guidance alongside machine-readable error codes for unsupported schemas, failed verification, missing preconditions, and configuration issues. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR79 | The product can provide adopter-facing preconditions for CORE behavior, including tenant projection freshness, audit sink availability, supported schema versions, and required contract compatibility. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR80 | The system can expose typed, sanitized error responses that include an audit handle and documentation pointer without leaking target tenant, Party, conversation existence, redacted content, provider payload, or cross-tenant business references. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR81 | The product can publish compatibility policy for additive changes, breaking changes, deprecation windows, and minimum supported contract versions across commands, projections, events, and client packages. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR82 | The product can produce a signed conformance artifact for release gating. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR83 | The product can maintain a versioned, release-specific conformance manifest with test identifiers, pass criteria, and requirement traceability. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR84 | The product can map each conformance test in the release manifest to the functional requirement, carry-forward commitment, or release-gate status it verifies. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR85 | The product can support a named-waiver process for release-gate exceptions. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR86 | The product can classify verification and release-gate failures as blocking or non-blocking across tenant isolation, audit integrity, contract compatibility, projection rebuild, provider portability, documentation evidence, and supported schema behavior. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR87 | The product can verify tenant isolation using adversarial positive and negative cases. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR88 | The product can verify idempotent command behavior under duplicate or reordered commands. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR89 | The product can verify redaction-replay correctness across projections, logs, traces, and errors. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR90 | The product can verify provider portability by proving recoverability without provider-owned session authority. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR91 | The product can verify event schema evolution through version-aware records and at least one worked additive-change example. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR92 | The product can validate command contracts, query contracts, emitted events, error semantics, and version discovery using executable contract tests before v1 release. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR93 | The product can include at least one adopter-style fixture using CORE preconditions in executable contract tests. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR94 | The product can distinguish module-level evidence from broader Hexalith platform compliance evidence and name inherited platform controls where applicable. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR95 | Operators can observe command rejection counts by reason without exposing conversation content or cross-tenant data. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR96 | Operators can observe projection lag, rebuild state, and projection availability without exposing conversation content or cross-tenant data. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR97 | Operators can observe event publication failures and subscriber-facing contract issues without exposing conversation content or cross-tenant data. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR98 | Operators can observe tenant isolation denials and privileged access attempts without exposing target tenant, Party, conversation existence, or redacted content. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR99 | Operators can observe conformance check outcomes and verification status in a form suitable for incident workflows and CI gates. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR100 | The product can explicitly identify capabilities that are v1, v1.1, vNext, deferred, waived, or conditional for a given release. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR101 | The product can expose release-scope consequences when substrate-defining capabilities are deferred. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR102 | The product can support buyer partial acceptance under the Option A v1 deal. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR103 | The product can track second-adopter status and trigger downgrade-rule review milestones. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |
| Feature-FR104 | The product can publish documentation that distinguishes Conversations responsibilities from chatbot, LLM provider, legal-hold, attachment storage, identity, tenant, project, folder, and upstream lifecycle responsibilities. | Epic 6, Stories 6.3 and 6.6 | ✓ Covered as preserved/non-activated obligation |

### Missing Requirements

No PRD functional requirement lacks an epic-level traceability path.

- Critical missing FRs: none.
- High-priority missing FRs: none.
- Epic-only FR identifiers not present in the PRD: none.

FR-16 is not a coverage gap: the finalized PRD explicitly defers it, and the epic authority records the same non-activation. Feature-FR1 through Feature-FR104 are covered as preservation/disposition obligations through Stories 6.3 and 6.6; this does not claim those product capabilities are implemented or scheduled.

### Coverage Statistics

- Total PRD functional requirements: 124.
- Active/deferred initiative FRs represented in the epic plan: 20 of 20.
- Preserved product FRs represented in the corrective denominator: 104 of 104.
- Total FRs covered in epics: 124.
- Missing FRs: 0.
- Coverage percentage: 100%.

This statistic measures traceability coverage only. It does not establish that the associated stories are complete, internally coherent, or backed by sufficient acceptance evidence; those questions belong to later workflow steps.
## UX Alignment Assessment

### UX Document Status

Found and fully reviewed:

- `_bmad-output/planning-artifacts/ux-design-specification.md` — completed UX specification with 52 UX decisions and explicit safety, responsive, accessibility, leakage, mobile, and performance acceptance criteria.
- `_bmad-output/planning-artifacts/ux-requirement-map.md` — 52-row UX-DR mapping artifact.

The UX specification is a preserved product-design reference. The current boilerplate-reduction PRD does not activate production UI work and explicitly preserves, rather than redesigns, FrontComposer-generated behavior.

### UX to PRD Alignment

The substantive UX is strongly aligned with the preserved product contract:

- Find → Read → Trust and Find → Open → Verify → Cite, Act, or Stop align with Feature-FR56 through Feature-FR69.
- Tenant-scoped, permission-safe search and non-enumeration align with Feature-FR26 through Feature-FR32 and Feature-NFR16 through Feature-NFR21.
- Evidence timelines, redaction, audit linkage, citations, and temporal reconstruction align with Feature-FR42 through Feature-FR68.
- Projection freshness and degraded trust states align with Feature-FR33 through Feature-FR41 and Feature-NFR44 through Feature-NFR48.
- FrontComposer/Fluent UI, WCAG 2.1 AA, keyboard/screen-reader parity, safe responsive surfaces, and non-color status semantics align with Feature-NFR69 through Feature-NFR77.

The scope relationship is also explicit: these UX obligations constrain FR-20 preservation but do not authorize new UI delivery in the active refactor.

### UX to Architecture Alignment

| UX need | Architecture support | Alignment |
| --- | --- | --- |
| FrontComposer and Fluent UI foundation | FrontComposer is the generated baseline; trust-bearing custom components are architecture-reviewed | Aligned |
| Server-owned trust and command state | Permission-safe DTOs, projection freshness, shared trust vocabulary, and command metadata remain server-owned | Aligned |
| Tenant-safe search and non-enumeration | Fail-closed tenant access applies before reads and across counts, facets, timing, URLs, DOM, ARIA, clipboard, and telemetry | Aligned |
| Evidence timeline, citations, redaction, audit, and temporal views | Custom component boundary and disclosure-surface conformance requirements are explicit | Aligned |
| WCAG and responsive leak safety | WCAG 2.1 AA plus keyboard, screen-reader, responsive-duplicate, browser-title, clipboard, and telemetry controls are architectural constraints | Aligned |
| Read responsiveness and trust freshness | Projection-shaped reads, explicit freshness metadata, batched Party hydration, and bounded asynchronous heavy workflows are specified | Aligned |
| Preservation governance | Architecture v7 freezes all 52 UX decisions and all UX acceptance criteria into the FR-20 denominator | Aligned in intent; traceability repair still open |

### Alignment Issues

1. **Stale UX provenance and story mapping — major.** The UX specification frontmatter points to a superseded root PRD rather than the finalized initiative PRD/addendum. The UX requirement map assigns UX-DR1 through UX-DR52 to feature-story identifiers such as Stories 3.1-3.8 and 4.4. The canonical epic plan contains no UX-DR references, has no Story 3.8, and has no Story 4.4; its current 3.x stories are platform-promotion work.
2. **Contradictory historical epic statement — major.** The immutable original epic prefix says no UX document applies. The active corrective overlay instead freezes 52 UX decisions and all UX acceptance criteria as preservation obligations. Consumers must use the overlay, but the contradictory historical text makes provenance unsafe without an explicit current mapping.
3. **Implementation roadmap must be marked non-activated — major.** The UX specification describes a multi-phase production component roadmap and primary v1 admin surface. Under the current PRD those items are preserved design intent, not authorized delivery scope. Without a preservation-only banner, an implementation agent could mistakenly activate product UI work.
4. **Traceability repair is planned but not yet evidenced — major.** Epic 6 Story 6.4 correctly requires finalized provenance, a preservation-only banner, a non-activated component roadmap, and reliable manifest/evidence/disposition mappings. Until its acceptance evidence exists, UX preservation traceability is incomplete.

### Warnings

- Do not implement or redesign production UI from the UX roadmap during this refactor. Story 6.4 authorizes governance/provenance repair only.
- Do not use `_bmad-output/planning-artifacts/ux-requirement-map.md` as current story authority until its mappings are replaced or explicitly dispositioned under Story 6.4.
- If future release scope activates any preserved UX capability, the FrontComposer trust-component ADR, canonical trust vocabulary, permission-safe DTOs, and leakage/accessibility gates must be completed before that UI story starts.

### UX Alignment Conclusion

The product UX intent and the architecture are substantively compatible, and no missing architectural platform capability was identified for the preserved UX. Implementation readiness is nevertheless constrained by stale provenance and broken story mappings. This is a planning-governance gap, already assigned to Story 6.4, rather than authorization for production UI work.
## Epic Quality Review

### Review Scope

Epics 1-5 and their 24 stories are immutable historical execution records. Their known defects are relevant only where they affect current authority. Epic 6 and its amendments are the active corrective plan and are assessed as implementation instructions.

### Epic-Level Assessment

| Epic | User/internal stakeholder value | Independence and sequencing | Quality result |
| --- | --- | --- | --- |
| Epic 1 | Release-owner preservation oracle and maintainer classification baseline | Standalone historical gate-zero | Historical; generally coherent |
| Epic 2 | Maintainer-facing reduction of local plumbing | Depends only on Epic 1, but Story 2.6 contained a forward dependency on Epic 3/FR-14 | Historical defect explicitly recognized by corrective authority |
| Epic 3 | Platform maintainer promotion/adoption value | Depends on Epics 1-2 and OQ-1; no dependency on Epic 4-5 | Historical; several stories were later superseded or reclassified |
| Epic 4 | Domain-author template and measurable authoring-cost outcome | Depends on completed platform adoption | Historical output superseded by Story 6.5 |
| Epic 5 | Release-owner attestation outcome | Correctly capstone-dependent on prior work | Historical evidence retained but superseded for current readiness |
| Epic 6 | Release-owner and platform-maintainer value through corrected authority and evidence | Internally non-independent, amendment-heavy, and not executable in story-number order | Fails current story-quality standard |

The epic titles are solution/evidence-centric rather than customer-facing, but the PRD explicitly defines developers, maintainers, architects, and release owners as the initiative users. That internal stakeholder value is legitimate. The defect is not the absence of an end-customer feature; it is that the active epic cannot be consumed as a clear, self-contained, correctly ordered implementation plan.

### Active Story Assessment

| Story | Outcome and sizing | Dependency quality | Acceptance quality | Finding |
| --- | --- | --- | --- | --- |
| 6.1 | Cohesive authority rebaseline | Entry point | Specific numbered criteria | Acceptable historical prerequisite |
| 6.2 | Combines performance baseline, hosting migration, platform-gap promotion, production projection implementation, state-store proof, and a broad convergence suite | Requires higher-numbered 6.7; its final contract is split across v2, v3, and v6 | Testable in parts but epic-sized as a whole | Critical oversizing and non-self-contained authority |
| 6.3 | Builds a zero-gap manifest across 20 FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, all UX ACs, contracts, controls, evidence, and mutation governance | Completion requires higher-numbered 6.9 and 6.12; an unapplied proposal also requires missing 6.10 | Precise outcome, but latest criteria are scattered across v2, v5, and v7 | Critical forward dependency and fragmented definition |
| 6.4 | Cohesive UX provenance/preservation correction | Higher-numbered 6.8 must precede completion | Terms such as reliable mappings and current rules remain binding lack an exact zero-gap artifact/test definition | Major dependency and specificity gap |
| 6.5 | Combines template correction, fixture construction, measurement, evidence record, and ownership validators | Requires 6.2 and higher-numbered 6.8; the unapplied proposal adds missing 6.10 | Measurable, but too many independently verifiable deliverables | Major sizing and dependency issue |
| 6.6 | Appropriate capstone attestation | Must be last and depends on 6.9, 6.12, prior spine, and potentially missing 6.10/6.11 outcomes | Criteria are split across four authority versions; requiring the readiness rerun to return READY is circular | Critical non-self-contained and outcome-bias defect |
| 6.7 | Cohesive mechanical promotion-completion gate | Must precede lower-numbered 6.2 | Specific, fault-injectable criteria | Content is strong; numbering creates a forward dependency |
| 6.8 | One generator plus workflow enforcement, historical verification, binding checks, and fault injection | Must precede lower-numbered 6.3-6.6 completion | Detailed and testable | Large but bounded; forward numbering remains a defect |
| 6.9 | Cohesive oracle-tiering and structural enforcement | Must precede lower-numbered 6.3 and 6.6 | Detailed prohibitions and measurable criteria | Content is strong; forward numbering remains a defect |
| 6.10 | Referenced by v7 as retaining approved scope/order | No canonical definition or criteria exist in selected epic authority | None | Critical missing story |
| 6.11 | Introduced by v6 and referenced by v7 | Only a terse disposition/performance intent exists; no canonical story heading or complete criteria | None | Critical missing story |
| 6.12 | Combines ADR 0004, historical validator changes, successor-proof generation, current guard, extensive fault injection, and three test lanes | Must precede lower-numbered 6.3 and 6.6 | Detailed and testable, but epic-sized | Critical oversizing and forward dependency |

### Critical Violations

1. **Stories 6.10 and 6.11 are dangling authority.** The v7 epic text says they retain approved scope and ordering, but `epics.md` contains neither `### Story 6.10:` nor `### Story 6.11:`. A separate `sprint-change-proposal-2026-08-01-stories-6-10-6-11-authority.md` proposes canonical definitions and v8 changes, but it is not listed in architecture correction authority, is not appended to the epic plan, and was not selected as canonical input. It cannot be used as implementation authority.
2. **The active stories are not self-contained.** Story 6.2 must be reconstructed from v2 plus v3 and v6; Story 6.3 from v2 plus v5 and v7; Story 6.6 from v2 plus v5, v6, and v7. An implementer cannot open one story definition and obtain its current acceptance contract.
3. **Forward dependencies are pervasive.** The binding graph includes 6.7 → 6.2, 6.8 → completion of 6.3/6.4/6.5/6.6, 6.9 → 6.3/6.6, and 6.12 → 6.3/6.6. This violates the rule that a story may use only prior story outputs. Append-only amendment history explains the numbering but does not make the implementation plan independently consumable.
4. **Story 6.2 and Story 6.12 are epic-sized.** Each combines multiple separable deliverables, cross-boundary changes, evidence generation, and broad fault-injection suites. Their failure and rollback boundaries are not story-sized.
5. **SM-C2 acceptance authority conflicts.** The finalized PRD and architecture OQ-5 state `post P95 <= 1.05 × baseline P95` for the frozen hot-path inventory. The v6 amendments replace that rule for HP-LIST/HP-OPEN with approved-cost ceilings and leave HP-CREATE/HP-APPEND measured but ungated. Story 6.11 is intended to restore stricter gating but lacks canonical acceptance criteria. The current plan therefore has contradictory release gates.

### Major Issues

1. Story 6.5 combines three independently completable outcomes: corrected authoring guidance, a buildable minimal fixture, and reproducible SM-2 measurement/evidence. Split or explicitly stage these deliverables.
2. Story 6.4 does not identify the exact replacement UX traceability artifact, its version, or the automated zero-gap check that proves all 52 UX-DR rows and all UX acceptance criteria are dispositioned.
3. Current Story 6.x acceptance criteria use numbered declarative lists rather than Given/When/Then scenarios. Most are measurable, but high-risk failure semantics—tenant denial, evidence mutation, stale bindings, skipped/vacuous tests, performance gate selection, and authorization downgrade—should be expressed as explicit scenarios.
4. The immutable historical prefix still contains obsolete statements such as no UX document applies, no architecture exists yet, FR-16 is conditional, and old local-host ownership. Although later overlays supersede them, the active reading path requires extensive manual authority merging.
5. Story 6.6 requires the independent readiness workflow to return READY. The story should require execution and preservation of the assessment result; the result must remain free to be READY, NOT READY, or conditionally blocked based on evidence.

### Minor Concerns

- Story titles are heavily implementation-centric. For this internal developer-platform initiative that is tolerable, but each canonical story should retain its named maintainer, architect, or release-owner outcome.
- The Epic 6 dependency graph is repeated across amendments instead of published once as a current normalized graph, increasing drift risk.
- Acceptance formatting and terminology vary between the historical BDD stories and corrective numbered criteria.

### Database and Entity Timing

No upfront database/table-creation violation was found. EventStore remains authoritative, and the required read-model writes are introduced with the production projection story that needs them. No transcript table or parallel write authority is planned.

### Starter and Brownfield Checks

The architecture's May 14 starter scaffold is explicitly superseded. This is a brownfield corrective initiative, so no new-project starter story is required at the beginning of Epic 6. Brownfield integration, compatibility, migration, preserved-history, submodule, and evidence concerns are extensively represented.

### Required Remediation

1. Apply an approved authority amendment that publishes complete canonical definitions for Stories 6.10 and 6.11 and adds that amendment to architecture and epic authority.
2. Publish one normalized current Story 6 execution view—without rewriting immutable history—that contains each story's complete effective acceptance criteria and a single topologically ordered dependency graph.
3. Resolve SM-C2 in one authority source: either restore the PRD's universal +5% gate or obtain an explicit PRD-level target amendment with named approval and measurable replacement gates.
4. Split Stories 6.2 and 6.12 into independently completable slices, or supply explicit internal checkpoints with separate rollback/evidence boundaries and prohibit partial completion claims.
5. Replace Story 6.6's required READY result with a requirement to run and preserve an unbiased readiness decision, blocking release unless that independent decision is READY.
6. Give Story 6.4 an exact versioned UX disposition artifact and automated zero-gap validation covering all 52 UX decisions and every UX acceptance criterion.

### Best-Practices Compliance Summary

- User/internal stakeholder value: pass with implementation-centric naming concerns.
- Epic independence: fail for active Epic 6.
- Story sizing: fail for Stories 6.2 and 6.12; concern for 6.3, 6.5, 6.8, and 6.9.
- No forward dependencies: fail.
- Database/entity timing: pass.
- Clear and testable acceptance criteria: partial; missing entirely for 6.10 and 6.11.
- FR traceability: pass at 124/124.
- Brownfield integration and compatibility: pass.
## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The planning set has complete functional-requirement traceability (124 of 124), strong substantive PRD/architecture/UX alignment, and no missing document category. It is not safe to begin or resume remaining corrective implementation from the current v7 authority because the active story contract is incomplete, fragmented, and internally inconsistent.

### Critical Issues Requiring Immediate Action

1. Stories 6.10 and 6.11 are referenced by active authority but have no canonical story definitions or complete acceptance criteria in `epics.md`.
2. Effective acceptance for Stories 6.2, 6.3, and 6.6 is scattered across multiple append-only amendments, so no single story definition is complete or independently consumable.
3. The current dependency graph violates forward-dependency rules: later-numbered Stories 6.7, 6.8, 6.9, and 6.12 are prerequisites for lower-numbered stories.
4. SM-C2 has contradictory acceptance authority: the finalized PRD/OQ-5 retains a universal +5% P95 rule while the v6 architecture/epic amendment substitutes ceilings or ungated disclosure for all four rows.
5. Stories 6.2 and 6.12 are epic-sized. Story 6.2 is preserved completed history; Story 6.12 still needs independently verifiable delivery and rollback boundaries before implementation.

### Major Issues Requiring Correction

1. UX-DR1 through UX-DR52 map to obsolete or nonexistent feature stories, and the UX specification lacks the required preservation-only authority banner.
2. Story 6.4 does not name an exact replacement UX traceability artifact and automated zero-gap validator.
3. Story 6.5 combines template correction, fixture construction, measurement, and validator work into one large completion unit.
4. High-risk corrective acceptance criteria are numbered declarations rather than explicit failure scenarios, despite mutation, authorization, stale-binding, performance, and non-vacuity risks.
5. Story 6.6 requires the independent readiness rerun to return READY instead of requiring an unbiased assessment whose actual result governs release.

### Recommended Next Steps

1. Formally apply an approved v8 authority amendment that publishes complete Story 6.10 and Story 6.11 definitions, lists the amendment in architecture correction authority, and appends it to the canonical epic plan. The existing proposal is not sufficient until applied.
2. Publish a normalized current execution view that preserves immutable history but gives every active Story 6.x one complete effective acceptance contract and one topologically ordered dependency graph.
3. Reconcile SM-C2 at PRD authority level. Either enforce the universal +5% rule or approve a versioned PRD target amendment with named owner, rationale, and measurable replacement gates. Then give Story 6.11 a complete canonical contract.
4. Complete Story 6.4's governance repair with a versioned preservation-only UX artifact and automated zero-gap validation for all 52 UX decisions and every UX acceptance criterion.
5. Split Story 6.12 or define explicit internal checkpoints with separate evidence, rollback, and completion boundaries. Do not rewrite completed Story 6.2 history.
6. Amend Story 6.6 to require execution and preservation of an independent readiness decision, not a predetermined READY result.
7. Rerun implementation readiness after the authority, performance, UX, and story-definition corrections are committed to the canonical planning artifacts.

### Positive Findings

- All required planning document categories exist.
- All 20 initiative FRs and all 104 preserved Feature-FRs have epic-level traceability.
- FR-16 is consistently intended to be deferred/non-activated.
- Architecture substantively supports preserved UX requirements through FrontComposer, Fluent UI, server-owned trust metadata, fail-closed tenant boundaries, permission-safe DTOs, accessibility, and disclosure-surface controls.
- EventStore authority, tenant isolation, redaction non-leakage, projection correctness, and preservation denominators are strongly specified.
- No database/table timing or parallel write-authority violation was found.

### Final Note

This assessment identified 10 material issues across authority/story structure, performance acceptance, and UX governance: five critical blockers and five major corrections, plus three minor consistency concerns. Correct the critical authority defects before proceeding with remaining implementation. The existing `sprint-change-proposal-2026-08-01-stories-6-10-6-11-authority.md` appears designed to address part of the problem, but it is currently an unapplied proposal and does not change the `NOT READY` decision.

**Assessment date:** 2026-08-01

**Assessor:** Codex, using the BMad Implementation Readiness workflow
