# Hexalith.Conversations Readiness Gate Decisions

Date: 2026-05-17
Approved by: Jerome
Purpose: Convert pre-kickoff readiness blockers into implementation-entry decisions.

These decisions are intentionally conservative. They unblock sprint planning by choosing the narrowest v1-safe behavior already supported by the PRD, architecture, epics, and approved sprint change proposals. Any broader behavior requires an ADR, buyer approval, or explicit release-scope promotion.

## Decisions

### EventStore envelope stability and evolution ownership

Decision: Treat the Hexalith.EventStore envelope as stable inherited infrastructure for v1. Conversations owns its domain event names, schemas, versioned public contracts, and compatibility tests, but does not evolve the EventStore envelope in this project.

Implementation rule: Do not expose raw EventStore envelopes, stream internals, snapshot mechanics, or projection internals as adopter APIs. If EventStore envelope changes become necessary, stop and create an ADR before implementation continues.

### .NET client versus raw HTTP fallback policy

Decision: The v1 supported integration path is the .NET client plus shared contract package. Raw HTTP fallback examples are omitted from normal v1 adopter guidance unless a buyer approval or diagnostics-only exception is recorded later.

Implementation rule: Story 4.2 implements the .NET happy path first. Raw HTTP parity tests apply only to approved fallback or diagnostics surfaces and must not encourage bypassing the contract package.

### v1 Conversations event consumers

Decision: No cross-module v1 event consumer dependency is assumed. v1 Conversations events are internal/publication-ready but not a committed cross-module integration contract for another Hexalith module.

Implementation rule: Story 1.10 and Story 5.8 prove publication/provider-portability behavior without inventing a named downstream consumer. Cross-module event consumption requires v1.1 scope or an explicit ADR.

### CORE status for MarkSensitiveData and RedactMessageContent

Decision: `MarkSensitiveData` and `RedactMessageContent` are v1 CORE governance commands because redaction and sensitivity classification are compliance-gating substrate behavior.

Implementation rule: Both commands require tenant authorization, Party attribution, rationale where applicable, typed rejection, paired audit behavior, and redaction replay evidence.

### Architect and second-engineer availability

Decision: Plan v1 as single-threaded through the Architect with no guaranteed second engineer capacity. Do not rely on parallel execution for trust/freshness, governance, UX safety, or release-gate evidence unless a named reviewer is added to the sprint plan.

Implementation rule: Sprint planning may sequence dependent stories, but cannot assume concurrent review capacity for gate-heavy stories.

### Second-adopter candidate or review milestone

Decision: No named second-adopter candidate is available for v1 kickoff. Ship the Conversations ADR as Candidate v0.1, record the second-adopter gap, review at GA+90, and open the downgrade-rule trigger window at GA+6 months.

Implementation rule: Story 6.6 must track status, owner, milestone date, downgrade trigger, and buyer acceptance evidence without implying that a real second adopter already exists.

### Temporal evidence anchor

Decision: Use a composite temporal cursor for v1: EventStore event position plus projection version. Timestamp is supporting display/correlation metadata, not the legal anchor by itself.

Implementation rule: Temporal evidence links must carry tenant scope, conversation identity, event position, projection version, contract version, and authorization recheck behavior. Re-resolution must be deterministic for the same event history and projection version.

### Command availability metadata

Decision: Command availability is server-owned metadata. Clients and FrontComposer surfaces render command eligibility, disabled state, required permission, precondition, risk level, freshness requirement, audit requirement, and blocked reason from server metadata.

Implementation rule: Missing, stale, ambiguous, malformed, unauthorized, or partially loaded command metadata disables unsafe actions. Mobile governance-changing actions remain blocked unless explicitly designed, authorized, confirmed, and tested.

### Projection freshness blocking semantics

Decision: Use the canonical freshness vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`.

Implementation rule: If a story does not explicitly declare accepted freshness states, only `Current` is acceptable. Trust-bearing decisions, governance mutation, export, verification, privileged background work, and command eligibility must block on `Stale`, `Rebuilding`, or `Unavailable` unless an ADR grants a narrower exception.

### Party hydration degraded states

Decision: Command-time participant validation fails closed when Parties cannot validate a new participant. Authorized read surfaces may degrade Party display hydration when policy allows, but must preserve stable Party IDs, attribution, tenant isolation, and safe unresolved/unavailable display state.

Implementation rule: Do not persist Party display names, contact values, identifiers, person details, organization details, or raw upstream problem details in conversation events. Hydration caching must be bounded and approved.

### Numeric capacity and performance thresholds

Decision: Use the PRD numeric thresholds already defined for v1 kickoff and classify unresolved capacity/cost numbers as accepted discovery targets, not hidden release blockers.

Release-blocking targets:

- Open conversation with full context: P95 <= 500 ms for up to 500 messages, 20 human participants, 5 AI agents, warm cache, and 50 concurrent opens/sec/tenant.
- Operator/admin investigation workflow: <= 90 seconds for defined Find -> Read -> Trust scenarios, including user interaction.
- Projection rebuild: 1M-event rebuild is MVP-required.
- Tenant isolation failures are release blockers.

Validation or discovery targets:

- 10M-event rebuild is pre-scale validation.
- 100M-event rebuild is capacity evidence unless the buyer explicitly requires it as a release blocker.
- Events/sec, concurrent conversations, write-amplification budget, and numeric cost thresholds are accepted discovery targets for v1 kickoff and must be reviewed before GA release-gate closure.

Implementation rule: Performance evidence must state environment, dataset scale, cache state, tenant count, tool versions, build hash, and whether authorization, projection read, redaction filtering, temporal lookup, and provenance metadata are included.

### Story 3.8 assignment plan

Decision: Split Story 3.8 by default before ordinary assignment.

Implementation rule: Create 3.8A responsive layout/mobile safe triage, 3.8B accessibility tree/keyboard/screen-reader safety, and 3.8C leakage/clipboard/browser/telemetry disclosure safety unless a named evidence owner explicitly accepts the combined epic-level checklist.

### Story 6.8 assignment plan

Decision: Split Story 6.8 by default before ordinary assignment.

Implementation rule: Create 6.8A telemetry redaction and 6.8B telemetry cardinality gates unless a named SRE/test owner explicitly accepts both fixture sets, redaction rules, approved dimensions, evidence outputs, and pass/fail gates.

### Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle

Decision: Keep v1 narrow. v1 supports governed sensitivity/redaction behavior and explicit audit treatment. Full Generate Evidence Bundle export, full retention editor, automatic legal-hold automation, future derived indexes, and broad lifecycle automation are out of v1 unless promoted by ADR and release-scope approval.

Implementation rule: Story 2.4.3 verifies only active v1 surfaces plus safety around logs, traces, errors, diagnostics, caches, screenshots, and conformance evidence. Future exports and derived indexes remain blocked without ADR coverage.
