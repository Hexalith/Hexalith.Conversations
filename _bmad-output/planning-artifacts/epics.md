---
stepsCompleted: [1]
currentStep: 2
extractionStatus: confirmed
status: draft-non-authoritative
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epic-6-current-execution-view-v2.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-19.md
---

# Conversations - Epic Breakdown

## Overview

This draft inventories requirements for a five-story parallelization analysis. It does not amend the existing V14 epic authority, change the active implementation hold, activate preserved product or UX scope, or authorize story execution.

## Requirements Inventory

### Functional Requirements

#### Active refactoring initiative requirements

FR-1: A canonical, accepted inventory lists every Conversations source area with one Consume/Promote/Keep classification, evidence, approximate LOC, and target capability.

FR-2: Classification disagreements are recorded and resolved with rationale; no area is unclassified or dual-classified at acceptance.

FR-3: Conversations operates through the platform-owned domain-service host while preserving all existing operations.

FR-4: Conversations delegates generic query execution and cursor protection to SDK seams while preserving domain filters, authorization, pagination, ordering, and response contracts.

FR-5: Conversations delegates generic read-model persistence and concurrency handling to the shared store and write policy without losing updates.

FR-6: Conversations delegates generic projection execution and rebuild coordination while retaining domain fields, freshness semantics, and evidence behavior.

FR-7: Conversations uses platform aggregate routing and reconstruction while preserving deterministic command, state, event, and invariant behavior.

FR-8: Conversations consumes shared serialization registration and converters while retaining only domain-specific serialization rules and compatible wire shapes.

FR-9: Conversations consumes shared test assertions, fakes, defaults, and fixtures while retaining domain-specific conformance scenarios.

FR-10: Platform-owned ServiceDefaults provide health, observability, resilience, and discovery; Conversations owns only bounded domain telemetry definitions.

FR-11: Domain modules consume a shared tenant-access projection handler and registration surface while supplying domain-specific contracts and fail-closed rules.

FR-12: Domain modules consume shared typed-client registration and configuration validation.

FR-13: Platform-owned Aspire/Dapr hosting provides supported topology and connectivity; Conversations retains only a non-shipping module test AppHost.

FR-14: Domain modules declare only domain-specific serializable contracts and consume shared polymorphic registration and composition.

FR-15: Domain modules consume shared observability instrumentation support while supplying bounded domain metric definitions and classification rules.

FR-16: Shared compile-time command/event metadata is deferred and must not reshape current Conversations contracts in this initiative.

FR-17: Conversations consumes every in-scope capability from FR-10 through FR-15 and removes every superseded local copy.

FR-18: A documented thin authoring template maps the minimal module skeleton and shared responsibilities to post-refactor Conversations.

FR-19: A reproducible minimal-module fixture and versioned artifact measure hand-authored file and LOC cost under frozen inclusion rules.

FR-20: The refactor preserves 100% of the frozen conformance denominator and public contract baseline with controlled, approved manifest changes only.

#### Preserved product-contract requirements

Feature-FR1: Adopter systems can create a tenant-scoped conversation record.
Feature-FR2: Each conversation has a stable tenant-scoped internal identity distinct from external, provider, and display identifiers.
Feature-FR3: The system represents conversation lifecycle states and allowed transitions.
Feature-FR4: Adopter systems can append ordered messages to a conversation.
Feature-FR5: Adopter systems can add human, AI-agent, and LLM participants.
Feature-FR6: Adopter systems can submit idempotent commands and receive stable duplicate outcomes.
Feature-FR7: Invalid, unauthorized, conflicting, duplicate, unsupported-version, and tenant-mismatched commands produce typed documented failures.
Feature-FR8: Adopters can retrieve a conversation with participants, ordered messages, attachments, governance state, and freshness context.
Feature-FR9: Adopters can list tenant conversations by business context and recent activity.
Feature-FR10: Adopters can update conversation title or metadata when activated by release scope.
Feature-FR11: Adopters can close or archive a conversation when activated by release scope.
Feature-FR12: Conversation records survive provider-session expiry, restart, and failover.
Feature-FR13: Each conversation action is attributable to a stable Party identity.
Feature-FR14: Humans, AI agents, and LLMs are attributable participant types.
Feature-FR15: Provider correlation identifiers remain attribution metadata, never authority.
Feature-FR16: Provider payload metadata is opaque, tenant-isolated, and explicitly versioned.
Feature-FR17: Attribution survives provider boundaries.
Feature-FR18: The system reconstructs who acted, what changed, when, and under which tenant.
Feature-FR19: File references can be attached without storing binaries in Conversations.
Feature-FR20: Conversations link to Projects and Folders by stable identifier.
Feature-FR21: Conversations support tenant-scoped discovery by external business identifier.
Feature-FR22: External correlation keys remain distinct from upstream business references.
Feature-FR23: Party, Project, Folder, and attachment state is resolved from upstream owners at read time.
Feature-FR24: Conversations remain readable and attributable across upstream lifecycle changes.
Feature-FR25: Out-of-coverage records expose an explicit migration boundary or handoff.
Feature-FR26: Tenant context is required on every command, event, projection, query, publication, and audit record.
Feature-FR27: Invalid or untrustworthy tenant binding is rejected before aggregate or projection access.
Feature-FR28: Cross-tenant enumeration and existence disclosure are prevented.
Feature-FR29: Unauthorized, nonexistent, and cross-tenant records are indistinguishable unless policy permits disclosure.
Feature-FR30: Tenant-binding failures use typed adopter-safe errors.
Feature-FR31: Privileged actions affecting tenant data are attributed in each affected tenant audit trail.
Feature-FR32: Published events and projection notifications do not leak cross-tenant metadata.
Feature-FR33: Projections derive from ordered conversation events.
Feature-FR34: Read models expose replay position, version, or equivalent freshness evidence.
Feature-FR35: Projection rebuilds reproduce equivalent read models from the persisted stream.
Feature-FR36: Projection consistency distinguishes current, stale, rebuilding, unavailable, and tenant-hidden states.
Feature-FR37: Asynchronous read models expose lag or documented freshness behavior.
Feature-FR38: Downstream systems consume versioned domain events for meaningful changes.
Feature-FR39: Published events carry explicit schema and version metadata.
Feature-FR40: Unsupported command, event, and projection versions return typed errors.
Feature-FR41: Compatibility, migration, and upcaster boundaries are defined.
Feature-FR42: Authorized systems can set or replace retention policy with rationale.
Feature-FR43: Authorized systems can mark content sensitive.
Feature-FR44: Authorized systems can redact content with actor, time, rationale, and policy attribution.
Feature-FR45: Archival, retention, redaction, legal-hold deferral, and immutable history remain distinct.
Feature-FR46: Audit history survives projected/display redaction.
Feature-FR47: Every governance mutation has paired audit evidence.
Feature-FR48: Governance mutations fail when audit recording is unavailable.
Feature-FR49: Non-governance activity may continue during audit degradation only when it has no governance mutation.
Feature-FR50: Message and governance state can be reconstructed at a prior time.
Feature-FR51: Audit records have stable citeable identity, time, actor, tenant, conversation, and integrity metadata.
Feature-FR52: Governance audit records receive defined retention and redaction treatment.
Feature-FR53: Allowed and denied audit-record actions, redaction, export, and separate logging are defined.
Feature-FR54: Privileged tenant-data actions record structured justification.
Feature-FR55: Operators review privileged justification, actor, time, tenant, conversation, policy, and resulting event coherently.
Feature-FR56: Compliance operators find tenant conversations by external identifiers.
Feature-FR57: Compliance operators filter by date and business context.
Feature-FR58: Operators read reconstructed transcripts with attribution, references, redaction, governance, tenant, policy, and freshness state.
Feature-FR59: Operators inspect inline redaction attribution.
Feature-FR60: Operators view the governance audit trail inline.
Feature-FR61: Operators view conversation state at a selected historical time.
Feature-FR62: Operators copy citation-ready transcript and audit references.
Feature-FR63: Stable temporal links resolve to the contract-defined prior state and anchor.
Feature-FR64: Read-only operator workflows cannot mutate aggregate state.
Feature-FR65: Privileged metadata, visibility, policy, audit, or governance changes are classified and separately audited.
Feature-FR66: Operators run governance verification by conversation, tenant, suite, or time window.
Feature-FR67: Verification returns structured audit, isolation, replay, rebuild, portability, and conformance results.
Feature-FR68: Verification distinguishes invariant failures from infrastructure or execution failures.
Feature-FR69: A seeded self-serve acceptance demo exercises redaction, time travel, citation copy, and cross-tenant denial.
Feature-FR70: Adopters integrate through published command, projection, event, and error contracts.
Feature-FR71: Adopters use a supported .NET client unless an explicit raw-HTTP exception is accepted.
Feature-FR72: Adopters can create, append, and read through a minimal happy path.
Feature-FR73: Adopters can run conformance tests before deployment.
Feature-FR74: Tenant binding, identity, idempotency, errors, freshness, publication, and governance behavior are documented.
Feature-FR75: Active contract versions and compatibility status are discoverable.
Feature-FR76: Caller-supplied origin/composer metadata supports attribution, audit, projection, and composition.
Feature-FR77: Onboarding diagnostics identify missing CORE preconditions and configuration or schema failures.
Feature-FR78: Machine-readable failures include adopter remediation guidance.
Feature-FR79: CORE preconditions expose tenant freshness, audit availability, schemas, and compatibility.
Feature-FR80: Typed sanitized errors provide safe audit and documentation handles without sensitive disclosure.
Feature-FR81: Compatibility policy covers additive/breaking changes, deprecation, and minimum supported versions.
Feature-FR82: Releases can produce signed conformance artifacts.
Feature-FR83: Releases maintain versioned conformance manifests with tests, criteria, and traceability.
Feature-FR84: Every manifest test maps to the requirement or release obligation it verifies.
Feature-FR85: Release-gate exceptions use a named-waiver process.
Feature-FR86: Release failures are classified as blocking or non-blocking across the governed dimensions.
Feature-FR87: Tenant isolation is verified with adversarial positive and negative cases.
Feature-FR88: Duplicate and reordered command idempotency is verified.
Feature-FR89: Redaction replay is verified across projections and disclosure surfaces.
Feature-FR90: Provider portability is verified without provider-session authority.
Feature-FR91: Schema evolution is verified with version-aware records and an additive example.
Feature-FR92: Executable contract tests validate commands, queries, events, errors, and version discovery.
Feature-FR93: Executable tests include an adopter-style CORE fixture.
Feature-FR94: Evidence distinguishes Conversations controls from inherited platform controls.
Feature-FR95: Operators observe rejection counts without content or cross-tenant disclosure.
Feature-FR96: Operators observe projection lag, rebuild, and availability safely.
Feature-FR97: Operators observe publication and subscriber-contract failures safely.
Feature-FR98: Operators observe isolation denials and privileged attempts without sensitive disclosure.
Feature-FR99: Conformance outcomes are usable by incident workflows and CI gates.
Feature-FR100: Release scope identifies v1, v1.1, vNext, deferred, waived, and conditional capabilities.
Feature-FR101: Release-scope consequences of deferred substrate capabilities are exposed.
Feature-FR102: Buyer partial acceptance remains preserved historical scope and requires a current release decision.
Feature-FR103: Second-adopter status and downgrade-review milestones can be tracked.
Feature-FR104: Documentation distinguishes Conversations from chatbot, provider, legal-hold, storage, identity, tenant, and upstream responsibilities.

### NonFunctional Requirements

#### Active refactoring quality gates

NFR-1: Behavior and public contracts must remain at 100% of the frozen FR-20/SM-C1 denominator; no silent denominator reduction is permitted.

NFR-2: Every HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN row must use a comparable frozen envelope and satisfy post P95 no greater than 105% of baseline P95.

NFR-3: Tenant access must fail closed, and cross-tenant access must remain impossible and adversarially tested.

NFR-4: Health, metric names, bounded dimensions, and operational signals must remain compatible and content-safe.

NFR-5: Projection and event handling must remain replay-safe, idempotent, and tolerant of duplicate/out-of-order at-least-once delivery.

#### Preserved product non-functional requirements

Feature-NFR1: Each NFR identifies its verification artifact and lifecycle stage.
Feature-NFR2: Each release-gated NFR maps to automated evidence, an evidence file, and a decision status.
Feature-NFR3: Numeric gates define method, environment, and pass/fail interpretation.
Feature-NFR4: Unresolved GA capacity and latency targets require thresholds or named accepted-unknown disposition.
Feature-NFR5: Numeric targets are classified as blockers, validation targets, or discovery targets.
Feature-NFR6: Missed or untested thresholds require approver, expiry, compensating control, and buyer acceptance when applicable.
Feature-NFR7: A shared measurement envelope defines scale, load, state, deployment, storage, and locality.
Feature-NFR8: Evidence records environment, scale, tools, build, schemas, time, links, and manifest.
Feature-NFR9: Warm full-context open targets P95 at most 500 ms for the preserved defined workload.
Feature-NFR10: The open target explicitly scopes authorization, projection, redaction, temporal evidence, and provenance costs.
Feature-NFR11: Cold-start load has a separate measured target.
Feature-NFR12: Defined operator investigation workflows target completion within 90 seconds.
Feature-NFR13: Supporting query, freshness, and explainability thresholds are separately defined.
Feature-NFR14: Append latency includes defined tenant, persistence, idempotency, audit, and publication boundaries.
Feature-NFR15: Append timing reports accepted, persisted, audited, enqueued, and visible stages separately.
Feature-NFR16: Tenant isolation failures are release blockers and untrustworthy context fails closed before data access.
Feature-NFR17: Isolation tests include cross-tenant guesses, replay, poisoned events, malformed metadata, and mixed-tenant rebuild.
Feature-NFR18: Every cross-tenant surface fails closed with content-safe responses.
Feature-NFR19: Errors and telemetry disclose no inaccessible identity, existence, content, or upstream payload.
Feature-NFR20: Governance mutations fail closed when audit writing is unavailable.
Feature-NFR21: Redacted content never rematerializes on any projection, cache, export, temporal, replay, log, trace, error, or observability surface.
Feature-NFR22: Duplicate, reordered, and retried commands do not diverge projections or duplicate effects.
Feature-NFR23: Pub/sub tests cover at-least-once delivery, duplication, reordering, replay, idempotency, and deduplication expiry.
Feature-NFR24: Publication failures define retry, dead-letter, replay, and subscriber notification.
Feature-NFR25: Dapr, EventStore, projection, publication, audit, and redaction failure modes receive operational drills unless waived.
Feature-NFR26: A failure-mode matrix binds dependency failure to behavior, retry, dead letter, signal, and recovery proof.
Feature-NFR27: Verification distinguishes invariant failures from infrastructure/execution failures.
Feature-NFR28: Event, projection, audit, and replay configuration RPO/RTO targets are defined and verified.
Feature-NFR29: Backup restore and tenant recovery are tested before production.
Feature-NFR30: Pre-kickoff throughput, concurrency, amplification, and open-rate targets are set or accepted as unknowns.
Feature-NFR31: Projection rebuild is measured at 1M, 10M, and 100M events with defined thresholds.
Feature-NFR32: Rebuild evidence is tiered into MVP, pre-scale, and capacity obligations.
Feature-NFR33: Long rebuilds expose progress, resume, and tenant-safe cancellation/isolation.
Feature-NFR34: Tenant-event lag has an SLO and defined request behavior.
Feature-NFR35: Redaction propagation has an SLO across all materializations.
Feature-NFR36: Capacity signals expose storage, amplification, rebuild, pub/sub, and per-tenant activity costs.
Feature-NFR37: Numeric cost thresholds are set or explicitly accepted as unknowns.
Feature-NFR38: Persisted streams rebuild functionally equivalent v1 read models.
Feature-NFR39: Deterministic rebuild reproduces state and evidence from the same event order.
Feature-NFR40: Persisted and published events carry versions; unsupported versions fail with typed errors.
Feature-NFR41: Schema evolution includes a worked additive example.
Feature-NFR42: Temporal evidence declares its authoritative anchor.
Feature-NFR43: Temporal links deterministically resolve to the same legally meaningful state.
Feature-NFR44: Freshness metadata is consistent across APIs, UI, diagnostics, and verification.
Feature-NFR45: Freshness uses a standard explicit metadata shape or documents an equivalent.
Feature-NFR46: Consistency distinguishes current, stale, rebuilding, unavailable, and tenant-hidden state.
Feature-NFR47: Operator surfaces distinguish trust states with tenant scope, freshness, and next action.
Feature-NFR48: Degraded processing surfaces last-known-good state, completeness, progress, and required action.
Feature-NFR49: Executable compatibility tests cover contracts, events, errors, discovery, and a CORE adopter fixture.
Feature-NFR50: Provider portability survives stripped or changed provider correlation IDs.
Feature-NFR51: Portability proof covers contracts, persistence, pub/sub, rebuild, and observability.
Feature-NFR52: Tenant isolation, idempotency, ordering, auditability, and replay remain provider-invariant.
Feature-NFR53: Client/package typed errors and compatibility match the raw service contract.
Feature-NFR54: Front-end composition metadata remains provenance, not UI coupling.
Feature-NFR55: Operators observe rejections, lag, publication failures, denials, privileged access, and conformance outcomes.
Feature-NFR56: Operational signals are tenant-safe and content-safe by default.
Feature-NFR57: Metrics and logs use bounded-cardinality dimensions.
Feature-NFR58: Observability excludes conversation IDs, free text, raw business IDs, content fragments, and unbounded errors.
Feature-NFR59: Governance/conformance output is machine-readable for CI and incidents.
Feature-NFR60: Privileged actions carry structured justification and auditable records.
Feature-NFR61: Privileged access receives periodic review and stale/unexplained access is an audit finding.
Feature-NFR62: Isolation, audit, redaction, schema, rebuild, and contract failures automatically block release unless validly waived.
Feature-NFR63: Every release produces signed conformance evidence and a versioned traceable manifest.
Feature-NFR64: Evidence identifies Conversations-owned and inherited platform controls.
Feature-NFR65: Audit access, export, redaction, tampering, and privileged views have explicit tests.
Feature-NFR66: Retention, archival, deletion, legal hold, audit, redaction, and derived materialization behavior is defined.
Feature-NFR67: Retention is tenant-aware and verifiably evidenced.
Feature-NFR68: Non-developer approvers can navigate release evidence while machine-readable artifacts remain authoritative.
Feature-NFR69: Operator/admin UI meets WCAG 2.1 AA for keyboard, focus, contrast, and screen-reader trust states.
Feature-NFR70: Accessibility scope applies to rendered operator/admin web surfaces.
Feature-NFR71: Trust, redaction, temporal, degraded, empty, and review states do not rely on color alone.
Feature-NFR72: Citation, evidence, audit, verification, degraded, and error workflows work without pointer-only interaction.
Feature-NFR73: Accessibility validation combines automation, keyboard walkthrough, and screen-reader review.
Feature-NFR74: Screen readers announce meaningful error, degraded, evidence, and audit-search changes.
Feature-NFR75: Operators diagnose delayed/blocked projections and failed release evidence within the preserved 90-second usability target.
Feature-NFR76: Fail-closed failures return safe explanations with class, operation, retryability, and escalation.
Feature-NFR77: Degraded/compliance messages clearly distinguish safe, stale, hidden, unavailable, and awaiting-governance states.

### Additional Requirements

- AR-1: Current semantic authority is the last complete V14 architecture overlay plus the V14 epic authority and candidate-bound sidecars; historical frontmatter is provenance only.
- AR-2: The global implementation hold is ACTIVE. No successor story, Epic 16 work, or product implementation may start or resume until candidate-matched mechanical validation, independent IR-0 READY, an explicit release-owner LIFTED decision, and a passing readiness rerun exist.
- AR-3: The latest approved change authorizes only CP-1 through CP-3; it explicitly adds no epic or story and does not reorder or amend the backlog.
- AR-4: CP-1 adds an AST-based static regression test that rejects actual `pytest.skip`, `skipif`, and ambient `verifier.worktree_dirt(ROOT)` calls under `_bmad/scripts/tests` with file/line diagnostics.
- AR-5: The anti-skip guard must run on clean and controlled-dirty trees with identical collected/passed counts and zero failed, skipped, or not-run checks.
- AR-6: A2 closes only after CP-1 and all required evidence lanes pass; its approved historical 28/28 note remains unchanged and fresh closure evidence is recorded separately.
- AR-7: A3 closes only after the complete candidate-bound fault matrix passes and A2 is formally done.
- AR-8: The mandated current sequence is shared A3 repair including CP-1, rerun evidence, close A2, close A3, run independent IR-0, record hold decision, then rerun readiness.
- AR-9: The two V14 publication commits, completed Epics 1-6, accepted baselines, signed evidence, and completed story records are immutable history.
- AR-10: No current closure work may modify product/runtime code, public contracts, packages, dependencies, submodules, gitlinks, deployment, PRD, architecture, or preserved UX scope.
- AR-11: Conversations owns domain behavior and contracts; platform libraries own reusable runtime capability; platform deployment owns production composition.
- AR-12: EventStore is the sole durable write authority. Query reads never repair derived state and all derived state remains rebuildable or explicitly classified.
- AR-13: Tenant access is local and fail-closed before every read, write, rebuild, export, tool, UI, worker, admin, or verification path.
- AR-14: Durable tenant projection storage belongs behind additive `ITenantProjectionStore` in `Hexalith.Tenants.Client`; Conversations configures and consumes it without duplicating generic Dapr state plumbing.
- AR-15: Replay-visible time derives only from immutable event inputs and an event-fed lifecycle/watermark fact; missing index state is reported truthfully.
- AR-16: AppHost diagnostics and live-route proof must not create a second reconciliation implementation.
- AR-17: The repository-local AppHost remains non-packable, non-publishable test infrastructure and never becomes production or reusable hosting.
- AR-18: The normative public trust/freshness vocabulary is `ProjectionTrustState` with `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.
- AR-19: The authoritative temporal evidence anchor is the `ProjectionFreshnessV1` composite of schema version, cursor, and last applied event position.
- AR-20: Non-governance commands and reads have no audit-sink dependency; governance mutations fail closed when audit evidence is unavailable.
- AR-21: Derived keys follow `conversations-derived-keys-v1`, including tenant-segmented summary/detail and index keys plus the dispatch-identity digest ledger exception.
- AR-22: Promotion-bearing work must keep root-declared submodules clean, bind exact mode-160000 gitlinks, and never initialize or traverse nested submodules.
- AR-23: Required evidence lanes pass with zero failed, skipped, and not-run checks; environmental inability is BLOCKED, never PASS.
- AR-24: Every future story has stable atomic AC IDs, exact non-interactive commands, explicit candidate/input/schema/digest bindings, blocker codes, rollback boundaries, and a generated final record.
- AR-25: Every mutation story contains a named negative or fault-injection scenario; migrated assertions bind before/after inventories and strength digests.
- AR-26: Final record facts come from the Epic 7 generator; hand-copied counts, commits, file lists, verdicts, submodule state, or gitlink state are prohibited.
- AR-27: Current mechanical coverage remains exactly 124/124 functional requirements plus 52 UX decisions and 28 UX acceptance IDs with zero missing, duplicate, or orphaned bindings.
- AR-28: UX remains preserved-not-activated; no product screen, component, interaction, navigation, or visual implementation is currently authorized.
- AR-29: The effective graph is acyclic and topological. After the hold lifts, Story 7.1 is the only immediate story start; after Story 7.4, Stories 8.1, 9.1, and 16.1 become eligible subject to candidate compatibility, and later parallelism remains constrained by the published predecessor sets.
- AR-30: Story 16.3 gates 12.1, 13.1, 14.1, and 15.1; Story 7.4 gates 8.1, 9.1, 10.1, 11.1, 13.1, 15.1, and 16.1 as further refined by their exact predecessors.
- AR-31: No story lifecycle state can override IR-0, the implementation hold, candidate drift, or the release gate.
- AR-32: Candidate-bound planning, story, and release identities are distinct and non-interchangeable.
- AR-33: Public APIs expose Conversations concepts and typed safe errors, never raw EventStore envelopes, stream identities, snapshots, or projection topology.
- AR-34: Party personal data remains upstream-owned and transiently hydrated at read time; durable events store stable identifiers only.
- AR-35: Metric/log dimensions are bounded and content-safe; message content, redacted text, Party data, raw provider payloads, secrets, and unauthorized existence never appear.
- AR-36: Each story names its decision owner, failure semantics, evidence obligation, exact source/test scope, and immutable rollback boundary.
- AR-37: Use `.slnx`, central package management, .NET 10/C# 14, nullable/implicit usings, warnings-as-errors, and individual xUnit v3 project/lane execution.

### UX Design Requirements

No UX implementation requirements are activated for this analysis. The UX specification and requirement map were explicitly excluded from the selected input set; current architecture nevertheless preserves their 52 decisions and 28 acceptance identifiers as non-activated denominator obligations.

### FR Coverage Map

{{requirements_coverage_map}}

## Epic List

{{epics_list}}
