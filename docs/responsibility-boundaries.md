# Hexalith Conversations Responsibility Boundaries

This document defines responsibility boundaries for operators, buyer evaluators, and compliance stakeholders. It identifies what Conversations owns, what belongs to adjacent systems, and the operational, compliance, and evidence consequences of each boundary. For the adopter-developer integration perspective, see [Developer Integration Guide](integration-guide.md).

## Overview

Conversations is a tenant-scoped, event-sourced module that owns the conversation record substrate, durable identity, idempotent command processing, governance evidence production, and content-safe observability. It does not own the adjacent systems that it integrates with: chatbot orchestration, LLM provider sessions, legal-hold authority, attachment storage, identity infrastructure, tenant lifecycle, project and folder lifecycle, Party personal data, provider availability, or broader Hexalith platform controls.

This document satisfies FR104: the product must publish documentation distinguishing Conversations responsibilities from all adjacent systems, with operational, compliance, and evidence consequences explicitly stated for each boundary.

Audience: operators, buyer evaluators, compliance stakeholders, and release approvers who need formal boundary accountability for procurement, incident investigation, or audit purposes.

## What Conversations Owns

The following capabilities are owned by the Conversations module and backed by conformance evidence:

- **`ConversationId` — Durable tenant-scoped conversation identity.** Distinct from LLM provider IDs, external business identifiers, UI labels, and thread names. `ConversationId` is assigned at creation and never replaced by provider-issued identifiers.
- **`PartyId` — Stable participant attribution.** Personal data is never persisted in Conversations domain events. `PartyId` is the stable attribution identity; display names and contact details are hydrated at read time via the Parties adapter and are never stored as durable conversation content.
- **Idempotent command processing.** Resubmitting the same logical request with the same idempotency key yields stable outcomes. Duplicate detection uses typed error codes `idempotency_conflict` and `idempotency_outcome_unknown`. Conversations does not rely on transport deduplication or provider session authority for idempotency authority.
- **Projection freshness and trust state.** The `ProjectionTrustState` values `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` are owned and emitted by Conversations. Only `Current` is trust-bearing. Projection availability is enforced fail-closed: stale, ambiguous, or unavailable projections do not produce trust-bearing read results.
- **Governance with paired audit evidence.** Retention, sensitivity, and redaction governance commands require paired audit evidence. Audit recording unavailability causes fail-closed rejection. Conversations produces governance evidence artifacts as input to legal and compliance processes; it does not hold legal-hold decision authority.
- **Content-safe observability signals.** All emitted telemetry signals carry bounded-cardinality dimensions with no conversation content, tenant IDs, Party IDs, or provider session values. Observability signals classify operational state using closed vocabulary.
- **Typed sanitized errors.** `ConversationError` carries a stable `ConversationErrorCode`, `ConversationErrorCategory`, and `ConversationErrorClientAction`. No raw infrastructure internals, provider-owned response data, or personal data appear in error surfaces.
- **Module-level conformance evidence.** Conversations distinguishes its own controls from inherited platform controls. Conformance evidence identifies owner, scope, and evidence obligation for each module-level assertion (see `## Inherited Platform Controls`).
- **Tenant-scoped isolation enforcement.** Conversations enforces tenant isolation using a local fail-closed Tenants access projection. Cross-tenant access is rejected at the aggregate and projection boundary. Conversations never bypasses the fail-closed check regardless of caller context.
- **CORE preconditions catalog.** `ConversationCorePreconditionCatalog.All` exposes all preconditions required for trust-bearing operation: projection freshness, audit availability, supported schema versions, contract compatibility, participant identity validation, idempotency behavior, projection health, and required configuration.

## Responsibility Boundaries

This section describes the 10 adjacent-system boundaries required by FR104. For each boundary with operational, compliance, or evidence consequences, the fields Owner, Source of Truth, Failure Semantics, Evidence Obligation, and Handoff Path are stated explicitly.

### Chatbot and Agent Orchestration

Conversations owns the conversation record substrate only. No chatbot logic, prompt design, or agent routing resides in Conversations.

**Owner:** Caller application (chatbot or agent orchestration layer)  
**Source of Truth:** Caller application  
**Failure Semantics:** Not applicable — chatbot failures occur in the caller application; Conversations continues to record conversation events when the caller recovers  
**Evidence Obligation:** None in Conversations; chatbot orchestration evidence is a caller responsibility  
**Handoff Path:** Caller application integrates via `Hexalith.Conversations.Client` or the contract package; Conversations does not invoke chatbot components

### LLM Provider Sessions

Conversations stores `ProviderCorrelationMetadata` as opaque correlation metadata only. Provider session authority, token allocation, and completion responses belong entirely to the LLM provider.

**Owner:** LLM provider (Dapr-mediated external service)  
**Source of Truth:** LLM provider  
**Failure Semantics:** Conversation history is recoverable from the EventStore-backed event log without provider-owned session authority (provider portability proof, Story 5.8 conformance); provider session loss does not corrupt conversation identity or history  
**Evidence Obligation:** Provider portability conformance evidence (Story 5.8 conformance suite); Conversations does not hold provider SLA evidence  
**Handoff Path:** Provider IDs are stored as opaque correlation metadata; durable conversation identity is `ConversationId` combined with `TenantId`, not provider-issued identifiers

### Legal-Hold Authority

Conversations produces governance event evidence — retention records, redaction records, and audit records — as input to legal processes. It does not hold legal-hold decision authority, define hold boundaries, or trigger export actions.

**Owner:** Legal compliance system  
**Source of Truth:** Legal compliance system  
**Failure Semantics:** Conversations preserves governance evidence and marks governance state fail-closed; legal-hold decisions are out of Conversations scope and must be made by the legal compliance system consuming Conversations evidence  
**Evidence Obligation:** Conversations provides governance evidence artifacts including retention, redaction, and audit records; the legal compliance system holds decision authority over hold boundaries and export triggers  
**Handoff Path:** Compliance operators run governance verification against Conversations evidence; the legal system consumes Conversations governance artifacts to inform legal-hold decisions

### Attachment Storage

Conversations stores stable `FileId` references in domain events and projections. Binary content, file lifecycle, and storage availability belong to the file or blob storage system.

**Owner:** File and blob storage system  
**Source of Truth:** File and blob storage system  
**Failure Semantics:** Conversations does not surface attachment unavailability as a Conversations error; `FileId` references remain stable in events and projections regardless of storage system availability  
**Evidence Obligation:** None in Conversations; file existence and availability are outside Conversations scope  
**Handoff Path:** Adopters query attachment storage directly using the stable `FileId` stored in Conversations events and projections; Conversations does not proxy or cache binary content

### Identity (Authentication)

Conversations receives authenticated claims at the API boundary and enforces tenant scope from those claims combined with the local Tenants projection. Authentication, token issuance, and identity lifecycle belong to the identity provider.

**Owner:** Identity provider (OIDC platform service)  
**Source of Truth:** Identity provider  
**Failure Semantics:** Invalid or missing authentication fails at the API boundary before any Conversations logic runs; Conversations does not store authentication tokens and does not issue identity credentials  
**Evidence Obligation:** Authentication evidence is platform-level; Conversations does not produce identity evidence  
**Handoff Path:** Server validates claims at the API boundary; Conversations uses `PartyId` as stable participant attribution and never stores raw authentication tokens in domain events

### Tenant Lifecycle

Conversations enforces tenant access using a local fail-closed Tenants projection subscribed from `Hexalith.Tenants`. Tenant lifecycle decisions, tenant provisioning, and tenant disabling belong to `Hexalith.Tenants`.

**Owner:** `Hexalith.Tenants`  
**Source of Truth:** `Hexalith.Tenants`  
**Failure Semantics:** If the Tenants projection is unavailable, stale, ambiguous, disabled, lagging, or unknown, Conversations fails closed — no aggregate command or projection access proceeds; this enforces tenant lifecycle boundaries without bypassing the fail-closed check  
**Evidence Obligation:** Tenants projection availability is a platform-level obligation; Conversations records projection state and emits observability signals for projection freshness, but does not hold tenant lifecycle evidence  
**Handoff Path:** The Tenants projection is consumed via event subscription; Conversations never bypasses the fail-closed tenant access check; tenant lifecycle changes propagate through the event subscription path

### Project and Folder Lifecycle

Conversations stores stable `ProjectId` and `FolderId` references in events and projections. Project creation, archival, deletion, and ownership belong to upstream project and folder systems.

**Owner:** Upstream project and folder systems  
**Source of Truth:** Upstream project and folder systems  
**Failure Semantics:** Upstream lifecycle changes do not mutate conversation domain events; read-time hydration degrades safely to `unresolved` or `unavailable` states when the upstream entity is unavailable or has been archived  
**Evidence Obligation:** Project and folder lifecycle evidence is outside Conversations scope; upstream lifecycle is a caller responsibility  
**Handoff Path:** `ProjectId` and `FolderId` references remain stable in domain events; read-time adapters return degraded states when upstream entities are unavailable; callers are responsible for interpreting upstream lifecycle state

### Party Personal Data

Conversations stores `PartyId` stable references in domain events. Read-time display hydration is performed via the Parties adapter. Personal data ownership, Party lifecycle, and Party resolution belong to `Hexalith.Parties`.

**Owner:** `Hexalith.Parties`  
**Source of Truth:** `Hexalith.Parties`  
**Failure Semantics:** At command time, Parties adapter unavailability fails closed — participant validation cannot proceed. At read time, adapter degradation may degrade display data per policy. Personal data is never persisted in Conversations domain events and does not flow into durable event history  
**Evidence Obligation:** Parties adapter availability is a platform-level obligation; hydrated personal data is transient and does not appear in Conversations evidence artifacts  
**Handoff Path:** Conversations stores `PartyId`; the Parties adapter hydrates display data at read time; personal data does not flow into durable domain events; data subject rights (erasure, correction) are exercised through `Hexalith.Parties`, not through Conversations

### Provider Availability

Conversations maintains EventStore-backed conversation history that is recoverable without provider session authority. Provider SLA, session continuity, and provider-specific configuration belong to the LLM provider.

**Owner:** LLM provider  
**Source of Truth:** LLM provider SLA  
**Failure Semantics:** Conversation history remains recoverable from the EventStore-backed event log without provider-owned session authority; provider availability does not gate conversation history access or projection rebuild  
**Evidence Obligation:** Provider portability conformance evidence (Story 5.8 conformance suite) proves Conversations does not depend on provider availability for conversation continuity; the LLM provider holds provider SLA evidence  
**Handoff Path:** EventStore is authoritative for conversation history; Conversations does not depend on provider availability for replay, projection rebuild, or conversation continuity; provider availability is monitored by the provider and the Dapr integration layer

### Hexalith Platform Controls

Conversations owns module-level evidence that distinguishes its controls from inherited platform controls. EventStore availability, Dapr sidecar health, Aspire orchestration, and infrastructure lifecycle belong to the platform team.

**Owner:** Platform team (`Hexalith` platform)  
**Source of Truth:** Platform team  
**Failure Semantics:** Platform outages surface as content-safe typed errors (`tenant_projection_unavailable`, `projection_unavailable`, etc.); Conversations does not suppress platform failure and does not retry platform-level failures as if they were Conversations errors  
**Evidence Obligation:** Module-level evidence names inherited controls explicitly; the platform team holds decision authority for inherited platform controls (Story 5.11 evidence separation); Conversations evidence artifacts identify which assertions belong to the module vs. the platform  
**Handoff Path:** Platform failures are classified in Conversations observability signals with safe reason classes; detailed platform evidence belongs to the platform team; Conversations operators escalate platform-classified failures to the platform team

## Inherited Platform Controls

Conversations inherits the following controls from platform components. For each, the platform owner and Conversations evidence obligation are named. This section aligns with Story 5.11 module-vs-platform evidence separation (FR94).

| Platform Component | Inherited Control | Platform Owner | Conversations Evidence Obligation |
|---|---|---|---|
| EventStore | Event log durability, availability, ordering guarantees, and disaster recovery | Platform team | Module-level assertion that conversation history is EventStore-backed; provider portability conformance (Story 5.8) proves recovery without provider authority |
| `Hexalith.Tenants` | Tenant provisioning, disabling, lifecycle decisions, and tenant registry availability | Tenant platform team | Module-level assertion that Conversations enforces fail-closed against the local Tenants projection; projection availability evidence is platform-level |
| `Hexalith.Parties` | Party personal data lifecycle, data subject rights, Party registry availability | Parties platform team | Module-level assertion that personal data does not flow into Conversations domain events; Parties adapter hydration is transient |
| Dapr | Sidecar health, pub/sub delivery guarantees, binding availability, service invocation | Platform team | Module-level assertion that Conversations uses Dapr abstractions without depending on specific infrastructure topology |
| Aspire | Orchestration, service discovery, configuration, local environment lifecycle | Platform team | Module-level assertion that Conversations does not embed infrastructure lifecycle management |
| Hexalith platform | Cross-cutting security controls, authentication infrastructure, audit log infrastructure | Platform team | Module-level evidence explicitly names which controls are inherited from the platform rather than implemented by Conversations |

Conversations conformance evidence identifies each inherited control, names the platform owner, and distinguishes the module-level assertion from the platform-level obligation. Readers should not infer that Conversations produces evidence for inherited controls; each row above identifies where that evidence obligation lives.

## Requirement Mapping

| Requirement | Boundary Coverage |
|---|---|
| FR104 | This entire document: all 10 adjacent-system boundary sections, all 6 required document sections, and the inherited platform controls table |
| FR26–FR32 (tenant isolation) | Tenant Lifecycle boundary; `## What Conversations Owns` — tenant-scoped isolation enforcement |
| FR23–FR25 (upstream references) | Project and Folder Lifecycle boundary; Party Personal Data boundary |
| FR90 (provider portability) | LLM Provider Sessions boundary; Provider Availability boundary |
| FR94 (module evidence separation) | `## Inherited Platform Controls` table; Story 5.11 evidence separation |

## Related Documentation

- [Developer Integration Guide](integration-guide.md) — adopter-developer view of responsibility boundaries, client workflow, and typed errors
- [Contract Compatibility and Deprecation Policy](release-evidence/contract-compatibility-policy.md) — versioning, deprecation, and unsupported-version rules
- [Architecture Decision Records Index](adrs/index.md) — architectural decisions governing boundary contracts and implementation constraints
- [Conformance Manifest](release-evidence/conformance-manifest-v1-fixture.json) — release evidence entries including module-level conformance assertions, platform-evidence separation, telemetry validation, and the headless-v1 rendered UI waiver (Stories 5.11, 6.7, 6.8A, 6.8B, and the Story 3.8 waiver)
- [Product Requirements Document](../_bmad-output/planning-artifacts/prd.md) — FR104 and related requirements defining the responsibility boundary documentation obligation
- [Architecture Reference](../_bmad-output/planning-artifacts/architecture.md) — boundary contracts for external dependencies, implementation guardrails, and critical failure modes
