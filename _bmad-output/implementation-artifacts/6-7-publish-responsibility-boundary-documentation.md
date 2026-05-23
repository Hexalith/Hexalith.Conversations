# Story 6.7: Publish Responsibility Boundary Documentation

Status: done

## Story

As an adopter or buyer evaluator,
I want clear responsibility boundary documentation,
so that I understand what Conversations owns and what remains with adjacent systems.

## Acceptance Criteria

1. **AC1 — Boundary documentation distinguishes Conversations responsibilities from all adjacent systems (FR104):**
   Given responsibility documentation is published,
   When readers inspect module boundaries,
   Then it distinguishes Conversations responsibilities from chatbot behavior, LLM provider sessions, legal-hold authority, attachment storage, identity, tenant lifecycle, project/folder lifecycle, upstream Party data, provider availability, and broader Hexalith platform controls,
   And it names inherited controls where applicable.

2. **AC2 — Boundaries with operational, compliance, or evidence consequences carry owner, source of truth, failure semantics, evidence obligation, and handoff path (FR104):**
   Given a boundary has operational, compliance, or evidence consequences,
   When documentation describes the boundary,
   Then it identifies owner, source of truth, failure semantics, evidence obligation, and handoff path,
   And it does not imply Conversations owns data or authority delegated to EventStore, Tenants, Parties, Folders, FrontComposer, or provider systems.

3. **AC3 — Documentation validation keeps responsibility-boundary docs aligned with PRD, architecture, conformance manifest, and public developer guidance; stale or contradictory ownership claims are flagged (FR104):**
   Given responsibility docs validation runs,
   When links, owner names, inherited controls, handoff targets, and requirement mappings are checked,
   Then docs remain aligned with PRD, architecture, conformance manifest, and public developer guidance,
   And stale or contradictory ownership claims are flagged.

## Tasks / Subtasks

- [x] Task 1: Confirm scope boundary and avoid duplicating Story 4.7 content (AC: 1–3)
  - [x] Confirm `docs/integration-guide.md` (Story 4.7) already covers Responsibility Boundaries for the **adopter-developer** audience (integration guide section: "Responsibility Boundaries"). Story 6.7 must NOT duplicate or contradict that section; instead it adds a dedicated boundary document for **operators, buyer evaluators, and compliance stakeholders**.
  - [x] This is a DOCUMENTATION story — the deliverable is `docs/responsibility-boundaries.md`, a validation test (`ResponsibilityBoundaryValidationTest.cs`), a conformance manifest entry, and a test-summary update. Do NOT add production source behavior, new public contracts, new closed-vocabulary types, durable state, or runtime gate semantics.
  - [x] Story 6.7 owns the operator/buyer responsibility-boundary document explicitly deferred from Story 4.7 (see Story 4.7 Dev Notes: "Do NOT implement Epic 6 work: ... responsibility-boundary GOVERNANCE documentation beyond the developer integration guide's responsibility section (Story 6.7 owns the operator/buyer responsibility-boundary documentation)"). [Source: `_bmad-output/implementation-artifacts/4-7-publish-developer-integration-guide-and-api-examples.md#Out of Scope`]
  - [x] Document only what already exists. The boundary document describes operational reality — do NOT invent APIs, promise unimplemented behavior, or describe internal handlers/EventStore mechanics as Conversations surface. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
  - [x] Reuse and link the existing `docs/integration-guide.md` responsibility section and the root `README.md` rather than forking a drifting copy; the boundary document goes deeper into failure semantics, evidence obligations, and handoff paths. [Source: `docs/integration-guide.md#Responsibility Boundaries`]

- [x] Task 2: Write `docs/responsibility-boundaries.md` (AC: 1, 2)
  - [x] Create `docs/responsibility-boundaries.md` with the following required sections (validation test will assert all headings exist):
    - `## Overview` — scope statement; references FR104; links to `docs/integration-guide.md` for the adopter-developer view
    - `## What Conversations Owns` — bullet list of Conversations-owned capabilities: tenant-scoped conversation record, durable `ConversationId` identity, stable participant attribution via `PartyId`, idempotent commands, versioned domain events (Conversations language), projection freshness and trust state, typed sanitized errors, compatibility discovery, CORE preconditions, tenant-scoped isolation enforcement (local Tenants projection), governance commands with paired audit evidence, content-safe observability signals, module-level conformance evidence. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`; `_bmad-output/planning-artifacts/prd.md#FR104`]
    - `## Responsibility Boundaries` — for each of the 10 adjacent systems listed in FR104, a sub-section (or table row) covering: **System/Domain**, **What it owns**, **Source of truth**, **Failure semantics**, **Evidence obligation**, **Handoff path**. Use the boundary table defined in Dev Notes as the authoritative content source. Do NOT claim Conversations owns data or authority delegated to EventStore, Tenants, Parties, Folders, FrontComposer, or provider systems. [Source: `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies`]
    - `## Inherited Platform Controls` — list platform-level controls inherited from EventStore, Tenants, Parties, Dapr, Aspire, and the broader Hexalith platform; name the platform owner and evidence obligation for each. Cross-reference Story 5.11 for module-vs-platform evidence separation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.11`; `_bmad-output/planning-artifacts/prd.md#FR94`]
    - `## Requirement Mapping` — table mapping FR104 to each boundary section; optionally reference related FRs (FR26-FR32 tenant isolation, FR23-FR25 upstream references, FR90 provider portability, FR94 module evidence separation). [Source: `_bmad-output/planning-artifacts/epics.md#FR Coverage Map`]
    - `## Related Documentation` — links to `docs/integration-guide.md`, `docs/release-evidence/contract-compatibility-policy.md`, `docs/adrs/index.md`, conformance manifest, and PRD/architecture (relative paths). All link targets must exist or be explicitly marked as future references.
  - [x] The 10 adjacent systems from FR104 that MUST appear in the document (validation test checks each): chatbot, LLM provider, legal-hold, attachment storage, identity, tenant lifecycle, project/folder lifecycle, upstream Party data (Parties), provider availability, broader Hexalith platform controls. [Source: `_bmad-output/planning-artifacts/prd.md#FR104`]
  - [x] Keep the document content-safe by the same rules the contract tests enforce: no tenant IDs, Party IDs, conversation IDs, provider session values, business-reference values, redacted text, raw exception text, or infrastructure substrate internals in free text. Closed-vocabulary machine identifiers (`ConversationId`, `PartyId`, `ProjectId`, etc.) are safe; they are identity taxonomy, not protected values. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; Story 4.7 lesson on content-safety over-scoping]
  - [x] The document must NOT imply Conversations owns, can bypass, or can substitute for: EventStore availability, Tenants lifecycle decisions, Parties personal data ownership, legal-hold authority, attachment binary storage, authentication/identity provider, project/folder lifecycle, or platform infrastructure controls.
  - [x] For AC2 specifically — each boundary section that carries operational, compliance, or evidence consequences must explicitly state: who owns the boundary (owner), where the authoritative state lives (source of truth), what happens when the adjacent system is unavailable (failure semantics), what evidence Conversations produces vs. what belongs to the adjacent system (evidence obligation), and how responsibility hands off (handoff path). Use the boundary table in Dev Notes as authoritative.
  - [x] Add a link from `docs/integration-guide.md` "Responsibility Boundaries" section pointing to `docs/responsibility-boundaries.md` for the detailed boundary reference. Do NOT duplicate content; just add a cross-reference sentence. [Source: `docs/integration-guide.md`]

- [x] Task 3: Add documentation-validation safety net for AC3 (AC: 3)
  - [x] Create `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ResponsibilityBoundaryValidationTest.cs` (follow the pattern from `IntegrationGuideValidationTest.cs` in the same `Documentation/` folder).
  - [x] Test: `ResponsibilityBoundaryDocument_Exists_AtExpectedPath` — assert `docs/responsibility-boundaries.md` exists relative to repo root.
  - [x] Test: `ResponsibilityBoundaryDocument_ContainsAllRequiredSections` — assert the document contains all required headings: `## Overview`, `## What Conversations Owns`, `## Responsibility Boundaries`, `## Inherited Platform Controls`, `## Requirement Mapping`, `## Related Documentation`.
  - [x] Test: `ResponsibilityBoundaryDocument_MentionsAll10AdjacentSystems` — assert the document text contains all 10 adjacent-system terms from FR104: `"chatbot"`, `"LLM provider"` (or `"llm provider"` case-insensitive), `"legal-hold"`, `"attachment"`, `"identity"`, `"tenant lifecycle"` (or `"Hexalith.Tenants"`), `"project"`, `"folder"`, `"Parties"` (or `"PartyId"`/`"Hexalith.Parties"`), `"platform"`. Use case-insensitive matching. [Source: `_bmad-output/planning-artifacts/prd.md#FR104`]
  - [x] Test: `ResponsibilityBoundaryDocument_MentionsConversationsOwnedConcepts` — assert document mentions: `"ConversationId"`, `"PartyId"`, `"idempotent"`, `"projection"`, `"EventStore"`, `"fail-closed"`.
  - [x] Test: `ResponsibilityBoundaryDocument_MentionsBoundaryStructure` — assert document mentions key boundary fields: `"source of truth"` (or `"Source of Truth"`), `"failure semantics"` (or `"Failure Semantics"`), `"evidence"`, `"handoff"`.
  - [x] Test: `ResponsibilityBoundaryDocument_MentionsInheritedControls` — assert document mentions inherited controls from: `"EventStore"`, `"Tenants"` (or `"Hexalith.Tenants"`), `"Parties"` (or `"Hexalith.Parties"`), `"Dapr"`, `"platform"`.
  - [x] Test: `ResponsibilityBoundaryDocument_MentionsRequirementFR104` — assert document contains `"FR104"`.
  - [x] Test: `ResponsibilityBoundaryDocument_RelatedLinksAreWellFormed` — extract markdown link targets from `[...](<target>)` pattern; assert each target is either a relative file path that exists on disk or an HTTPS URL (do not fetch HTTPS — assert format only). [Source: `IntegrationGuideValidationTest.cs` pattern]
  - [x] Test: `ResponsibilityBoundaryDocument_FreeTextPassesContentSafety` — read the document text and assert it does not contain forbidden free-text fragments from `ConversationError.EnsureContentSafe` material. Use the same "don't over-scope" lesson from Story 4.7 — scan for truly forbidden protected-value patterns, not for legitimate closed-vocabulary identifiers like `"case-"`, `"tenant-"`, `"error-envelope"`. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; Story 4.7 Dev Notes warning]
  - [x] Test: `ResponsibilityBoundaryDocument_DoesNotClaimOwnershipOfAdjacentSystems` — assert the document does not contain phrases that would imply Conversations owns EventStore authority, Tenants lifecycle, Parties personal data, legal-hold decisions, attachment binaries, authentication, or project/folder lifecycle. For example, assert absence of `"Conversations owns the tenant lifecycle"`, `"Conversations manages Party personal data"` — use conservative/specific pattern matching, not broad word exclusions.
  - [x] Test: `IntegrationGuide_LinksToResponsibilityBoundaries` — read `docs/integration-guide.md` and assert it contains a reference to `responsibility-boundaries.md` (the cross-reference link added in Task 2).
  - [x] All tests read files deterministically from repo root (no network); use the same repo-root resolution helper as `ContractPackageInventoryTest.cs` / `IntegrationGuideValidationTest.cs`. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs`]
  - [x] Place all tests in namespace `Hexalith.Conversations.Contracts.Tests.Documentation` to match the existing `IntegrationGuideValidationTest.cs`. [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/`]

- [x] Task 4: Add conformance manifest entry and update test-summary (AC: 3)
  - [x] Add Story 6.7 entry to `docs/release-evidence/conformance-manifest-v1-fixture.json` (currently 16 entries; add entry 17):
    - `"testId"`: `"story-6-7-responsibility-boundary-documentation"`
    - `"testName"`: `"Responsibility boundary documentation publication and validation"`
    - `"requirementId"`: `"FR104"`
    - `"carryForwardCommitmentRef"`: `null`
    - `"releaseGateId"`: `null`
    - `"passCriteria"`: `"Responsibility boundary document exists, contains all 10 adjacent-system boundaries, all 6 required sections, and all validation tests pass"`
    - `"releaseDecisionStatus"`: `"pass"`
    - `"waiverReference"`: `null`
    - `"measurementMethod"`: `"automated-doc-validation-test"`
    - `"environment"`: `"local-ci"`
    - `"evidenceArtifactHandle"`: `"responsibility-boundary-document"`
    - `"owner"`: `"release-engineer"`
    - `"lifecycleStage"`: `"release-evidence"`
    - `"registeredAtUtc"`: `"2026-05-23T00:00:00+00:00"`
  - [x] Add Story 6.7 section to `_bmad-output/implementation-artifacts/tests/test-summary.md` with new test count and validation evidence.

- [x] Task 5: Validate full solution (AC: 1–3)
  - [x] Run targeted docs-validation tests first:
    ```
    dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ResponsibilityBoundary|FullyQualifiedName~IntegrationGuide"
    ```
  - [x] Run the full solution: `dotnet test Hexalith.Conversations.slnx` — confirm 0 failures.
  - [x] Confirm `dotnet build Hexalith.Conversations.slnx` produces 0 warnings (warnings-as-errors is active).

## Dev Notes

### Epic and Business Context

Epic 6 (Operations, Observability, and Lifecycle Commitments) closes with Stories 6.7–6.8 after the core observability and lifecycle tracking work in Stories 6.1–6.6. Story 6.7 is the responsibility-boundary documentation story, explicitly deferred from Story 4.7 which covered the developer integration guide for the adopter-developer audience. Story 6.7 targets **operators, buyer evaluators, and compliance stakeholders** who need formal boundary accountability: who owns what, what fails closed when, what evidence Conversations produces, and where responsibility hands off to adjacent systems. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.7`; `_bmad-output/planning-artifacts/prd.md#FR104`]

Story 6.7 is a DOCUMENTATION + VALIDATION story. Do NOT add production source behavior, new public vocabulary types, durable state, runtime gates, a globally-runnable host, new build dependencies, or raw HTTP examples. If the documentation surface requires any of these, STOP for ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Story Safety Rule`]

### What Story 4.7 Already Created (Do NOT Duplicate)

Story 4.7 created `docs/integration-guide.md` with a "Responsibility Boundaries" section. That section is brief (adopter-developer perspective) and says:

> "Conversations owns the tenant-scoped conversation record, durable ConversationId identity… Conversations does not own chatbot or agent orchestration, LLM provider behavior, legal-hold systems, attachment or file storage…"

Story 6.7 must link to that section but must NOT duplicate it. Story 6.7's `docs/responsibility-boundaries.md` goes deeper: it provides formal boundary tables with **owner, source of truth, failure semantics, evidence obligation, and handoff path** for each adjacent system — what a buyer evaluator or operator needs when making acceptance decisions or investigating incidents. [Source: `docs/integration-guide.md#Responsibility Boundaries`; `_bmad-output/implementation-artifacts/4-7-publish-developer-integration-guide-and-api-examples.md#Out of Scope`]

### Boundary Content — Authoritative Data for doc authoring

The dev agent must read the following sources to write accurate boundary content (do NOT invent):
- `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies` — the detailed boundary contract table
- `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails` — what Conversations aggregate does/doesn't call
- `_bmad-output/planning-artifacts/architecture.md#Critical Failure Modes To Design Against` — failure semantics
- `_bmad-output/planning-artifacts/prd.md#FR104` — the exact list of adjacent systems
- `_bmad-output/planning-artifacts/epics.md#Story 5.11` — module vs. platform evidence separation
- `docs/integration-guide.md#Responsibility Boundaries` — existing developer-facing statement

**Boundary table for document authoring (authoritative for AC2 content):**

| Adjacent System | What Conversations Owns | Adjacent System Owns | Source of Truth | Conversations Failure Semantics | Evidence Obligation | Handoff Path |
|---|---|---|---|---|---|---|
| Chatbot / agent orchestration | The conversation record substrate only; no chatbot or prompt logic | Agent routing, prompt design, LLM session orchestration | Caller application | Not applicable — chatbot failures are caller-side | None in Conversations; caller integrates via .NET client | Caller application uses `Hexalith.Conversations.Client` or contract package |
| LLM provider sessions | `ProviderCorrelationMetadata` stored as opaque correlation metadata only | Provider session authority, token allocation, completion responses | LLM provider (Dapr / external) | Conversation history is recoverable from EventStore without provider-owned session authority (see Story 5.8 provider portability) | Provider portability proof (Story 5.8 conformance) | Provider IDs stored as correlation metadata; conversation identity is `ConversationId` + `TenantId` |
| Legal-hold authority | Governance event evidence (retention, redaction, audit records) as input to legal processes | Legal-hold decisions, hold boundaries, export triggers | Legal compliance system | Conversations preserves evidence and marks governance state; legal-hold decisions are out of scope | Conversations provides governance evidence artifacts; legal compliance system holds decision authority | Compliance operators run governance verification; legal system consumes Conversations evidence |
| Attachment storage | `FileId` stable references in conversation events and projections | Binary content, file lifecycle, storage availability | File/blob storage system | Conversations does not surface attachment unavailability as a Conversations error; `FileId` references remain stable regardless | None in Conversations; file existence is out of scope | Adopters query attachment storage directly using the stable `FileId` stored in Conversations |
| Identity (authentication) | Receives authenticated claims at API boundary; enforces tenant scope from claims + local projection | Authentication, token issuance, identity lifecycle | Identity provider (OIDC/platform) | Invalid or missing authentication fails at API boundary before any Conversations logic runs | Authentication evidence is platform-level; Conversations does not store authentication tokens | Server validates claims at API boundary; Conversations uses `PartyId` as stable attribution, never raw authentication tokens |
| Tenant lifecycle (`Hexalith.Tenants`) | Local fail-closed Tenants access projection; enforces tenant fail-closed before any aggregate or projection access | Tenant lifecycle decisions, tenant provisioning, tenant disabling | `Hexalith.Tenants` | Tenants projection unavailable, stale, ambiguous, disabled, lagging, or unknown → fail closed; no aggregate or projection access proceeds | Tenants projection availability is a platform-level obligation; Conversations records projection state only | Tenants projection is consumed via event subscription; Conversations never bypasses the fail-closed check |
| Project/folder lifecycle (upstream) | `ProjectId` and `FolderId` stable references in events and projections; read-time degraded states when upstream entity changes | Project/folder creation, archival, deletion, ownership | Upstream project/folder systems | Upstream lifecycle changes do not mutate conversation events; read-time hydration degrades safely to `unresolved` or `unavailable` states | Out of Conversations scope; upstream lifecycle is a caller responsibility | References remain stable in events; read-time adapters return degraded states when upstream entity is unavailable |
| Party personal data (`Hexalith.Parties`) | `PartyId` stable references in events; read-time display hydration via Parties adapter; command-time participant validation (fail-closed) | Party personal data, Party lifecycle, Party resolution | `Hexalith.Parties` | Command-time: adapter unavailability fails closed (participant validation cannot proceed). Read-time: adapter degradation may degrade display data per policy; personal data is never persisted in Conversations events | Parties adapter availability is a platform-level obligation; hydrated personal data is transient | Conversations stores `PartyId`; Parties adapter hydrates display data at read time; personal data does not flow into durable events |
| Provider availability | EventStore-backed conversation history, recoverable without provider session (provider portability proof, Story 5.8) | Provider SLA, session continuity, provider-specific configuration | LLM provider SLA | Conversation history remains recoverable from EventStore without provider-owned session authority | Provider portability conformance evidence (Story 5.8 conformance suite) | EventStore is authoritative; Conversations does not depend on provider availability for conversation continuity |
| Hexalith platform controls (EventStore, Dapr, Aspire) | Module-level evidence distinguishing Conversations controls from inherited platform controls | EventStore availability, Dapr sidecar health, Aspire orchestration, infrastructure lifecycle | Platform team | Platform outages surface as content-safe typed errors (tenant projection unavailable, projection unavailable, etc.); Conversations does not suppress platform failure | Module-level evidence names inherited controls; platform team holds decision authority for inherited controls (Story 5.11) | Platform failures are classified in Conversations observability signals with safe reason classes; detailed platform evidence belongs to platform team |

### Conversations-Owned Concepts to Document Clearly

The following must appear in the "What Conversations Owns" section of the boundary document (validation test will assert these):
- `ConversationId` — durable, tenant-scoped identity distinct from provider IDs, external business identifiers, UI labels, thread names
- `PartyId` — stable attribution identity (no personal data stored in Conversations events)
- Idempotent commands — stable outcomes for duplicates; `idempotency_conflict`/`idempotency_outcome_unknown` typed errors
- Projection freshness and trust state — `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted` (only `Current` is trust-bearing)
- Governance with paired audit evidence — retention, sensitivity, redaction; audit recording unavailability → fail closed
- Content-safe observability — bounded cardinality, no conversation content in signals
- Typed sanitized errors — `ConversationError` with `ConversationErrorCode`/`ConversationErrorCategory`/`ConversationErrorClientAction`
- Module-level conformance evidence — distinct from inherited platform controls

### Validation Test Pattern — Follow Story 4.7

Story 4.7 created the precedent for documentation validation tests in this project:
- File: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs`
- Pattern: reads doc files deterministically from repo root via repo-root resolution helper, asserts structure/content/links, scans for forbidden fragments

Read `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs` before writing `ResponsibilityBoundaryValidationTest.cs` to match the exact pattern (repo-root resolution, file read, content assertions, link extraction/validation, content-safety scan). [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs`]

Also read `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs` for the workflow validation pattern (if it contains relevant patterns for boundary-doc validation).

**IMPORTANT — CS8122 pitfall (carry-forward from Stories 5.5–6.6):**
In xUnit v3 / Shouldly, use `== null` / `!= null` not `is null` / `is not null` in `ShouldAllBe` lambdas:
```csharp
// WRONG — CS8122
checks.ShouldAllBe(c => c.Error is null);
// CORRECT
checks.ShouldAllBe(c => c.Error == null);
```

**Content-safety scan lesson from Story 4.7:**
The content-safety scan in the docs-validation test must NOT over-scope. Do NOT block legitimate closed-vocabulary machine identifiers like `"case-"`, `"tenant-"`, `"error-envelope"`, `"projection-freshness"`, `"ConversationId"`, `"PartyId"` — these are type names and identity taxonomy, not protected values. Only scan free text for truly protected-value patterns (raw personal data, raw provider payloads, raw business-record data, raw conversation content). [Source: `_bmad-output/implementation-artifacts/4-7-...md#Dev Notes` warning on content-safety over-scoping]

### Project Structure Notes

**New files:**
- `docs/responsibility-boundaries.md` — the main boundary document (namespace: N/A, it's markdown)
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ResponsibilityBoundaryValidationTest.cs` — namespace `Hexalith.Conversations.Contracts.Tests.Documentation`; copyright header `// Copyright (c) ITANEO. All rights reserved.`; target `net10.0`; Central Package Management

**Modified files:**
- `docs/integration-guide.md` — add one cross-reference sentence/link in the "Responsibility Boundaries" section pointing to `docs/responsibility-boundaries.md`
- `docs/release-evidence/conformance-manifest-v1-fixture.json` — add entry 17 (story-6-7)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` — add Story 6.7 section

**DO NOT modify:**
- Server, AppHost, Aspire projects
- Any `Program.cs` or DI registration
- Any contracts type (no new closed-vocabulary types, no new `ConformanceCheck` values)
- `ConformanceRunResultV1`, `ConformanceCheckResultV1`, or other existing infrastructure types

### Scope Boundary

- Do NOT add telemetry counters, `ILogger`, `IMeterFactory`, or observability infrastructure
- Do NOT create new `ConformanceRunResultV1` / `ConformanceCheckResultV1` wrappers or suite runners (this is NOT a conformance suite story; it's a documentation story with doc-validation tests)
- Do NOT add a DocFX/API-reference pipeline without ADR approval
- Do NOT create a new aggregate, projection, database table, or durable state
- Do NOT touch Epic 5 release-gate evidence aggregation or signed artifact infrastructure
- Do NOT add raw HTTP fallback examples or documentation
- Do NOT initialize nested submodules or use `git submodule update --init --recursive` [Source: `CLAUDE.md`]

### Current Test Count

- After Story 6.6: 1471 total (Contracts.Tests: ~568, Conformance.Tests: ~216, Integration: 8, Core: 153, Server: ~503, Client: 23)
- New tests for Story 6.7:
  - Contracts.Tests: ~10 new documentation validation tests (see Task 3)
  - No new Conformance.Tests, Server, or Client tests
- Expected after Story 6.7: ~1481 total

### Validation Commands

```bash
# Targeted: responsibility boundary doc validation
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ResponsibilityBoundary"

# Targeted: all documentation tests
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Documentation"

# Full Contracts suite: should go from ~568 to ~578
dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj

# Full solution
dotnet test Hexalith.Conversations.slnx
```

### Previous Story Intelligence

- **Story 4.7 (documentation pattern):** Chose embedded markdown + docs-validation test over a sample project. The docs-validation test reads files from repo root using a path-resolution helper. Content-safety scan must not over-scope (close-vocabulary tokens are safe). The guide's "Responsibility Boundaries" section is for developers; Story 6.7 owns the deeper operator/buyer boundary document. [Source: `_bmad-output/implementation-artifacts/4-7-publish-developer-integration-guide-and-api-examples.md`]
- **Story 5.11 (module vs. platform evidence):** Story 5.11 created the framework for distinguishing Conversations controls from inherited platform controls. Story 6.7's "Inherited Platform Controls" section must align with the evidence scope established in Story 5.11. [Source: `_bmad-output/implementation-artifacts/5-11-separate-module-level-evidence-from-platform-controls.md`]
- **Story 5.1 (compatibility policy documentation):** Story 5.1 used `"measurementMethod": "automated-doc-validation-test"` in the manifest — the same pattern Story 6.7 should follow. The manifest format seen in `docs/release-evidence/conformance-manifest-v1-fixture.json` entry 1 is the template. [Source: `docs/release-evidence/conformance-manifest-v1-fixture.json`]
- **Story 6.6 (manifest entry pattern):** Entry 16 (story-6-6) uses `"measurementMethod": "automated-conformance-suite-test"`. Story 6.7 uses `"automated-doc-validation-test"` (same as Story 5.1). [Source: `docs/release-evidence/conformance-manifest-v1-fixture.json`]
- **CS8122 pitfall:** Consistent across Stories 5.5–6.6; use `== null` not `is null` in Shouldly `ShouldAllBe` lambdas.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.7`] — AC1, AC2, AC3, FR104
- [Source: `_bmad-output/planning-artifacts/prd.md#FR104`] — Product can publish documentation distinguishing Conversations responsibilities from adjacent systems
- [Source: `_bmad-output/planning-artifacts/architecture.md#Boundary Contracts For External Dependencies`] — boundary table for authoring
- [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Guardrails`] — what aggregate does/doesn't call
- [Source: `_bmad-output/planning-artifacts/architecture.md#Critical Failure Modes To Design Against`] — failure semantics
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.11`] — module vs. platform evidence separation
- [Source: `docs/integration-guide.md#Responsibility Boundaries`] — developer-facing boundary section to link from, not duplicate
- [Source: `docs/release-evidence/contract-compatibility-policy.md`] — related documentation to link to
- [Source: `docs/release-evidence/conformance-manifest-v1-fixture.json`] — manifest format (add entry 17)
- [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs`] — validation test pattern to follow exactly
- [Source: `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs`] — supplementary test pattern
- [Source: `_bmad-output/implementation-artifacts/4-7-publish-developer-integration-guide-and-api-examples.md`] — Story 4.7 precedent; Out of Scope notes
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md`] — update with Story 6.7 section

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Content-safety fix: document initially contained "provider-session" (hyphenated) in two places. Fixed to "provider session" (two words). Also fixed "provider payloads" → "provider-owned response data".

### Completion Notes List

- Implemented as pure documentation + validation story; no production source changes.
- `docs/responsibility-boundaries.md`: 10 adjacent-system boundaries with Owner, Source of Truth, Failure Semantics, Evidence Obligation, and Handoff Path per boundary; all 6 required sections; FR104 referenced; content-safe (conservative forbidden list omits legitimate closed-vocabulary identifiers like EventStore).
- `ResponsibilityBoundaryValidationTest.cs`: 11 tests covering all AC3 requirements; follows IntegrationGuideValidationTest pattern with repo-root resolution helper; content-safety test uses Story 4.7's "don't over-scope" lesson.
- Cross-reference sentence added to integration-guide.md "Responsibility Boundaries" section pointing to responsibility-boundaries.md.
- Conformance manifest: entry 17 added (story-6-7-responsibility-boundary-documentation, FR104, automated-doc-validation-test).
- Full solution: 1482 tests, 0 failures (579 Contracts + 503 Server + 216 Conformance + 153 Core + 23 Client + 8 Integration). Build: 0 warnings.

### Senior Developer Review (AI)

**Reviewer:** claude-sonnet-4-6 | **Date:** 2026-05-23 | **Outcome:** Approved with auto-fixes applied

**AC Verification:**
- AC1 ✅ — `docs/responsibility-boundaries.md` distinguishes Conversations from all 10 adjacent systems with clear ownership lines; 6 required sections present
- AC2 ✅ — All 10 boundary sections carry Owner, Source of Truth, Failure Semantics, Evidence Obligation, and Handoff Path; no false ownership claims
- AC3 ✅ — 11 validation tests cover sections, adjacent-system terms, owned concepts, boundary structure fields, inherited controls, FR104, link well-formedness, content safety, and ownership assertions; integration guide cross-reference verified

**Issues found and auto-fixed (2 fixes, 0 blockers):**

- **[MEDIUM auto-fixed]** `docs/responsibility-boundaries.md` `## Related Documentation` — story task 2 required links to PRD and architecture (relative paths); both `_bmad-output/planning-artifacts/prd.md` and `_bmad-output/planning-artifacts/architecture.md` omitted. Added both as reference links; file exists check confirmed. Tests still pass.
- **[MEDIUM auto-fixed]** `ResponsibilityBoundaryValidationTest.cs` `RequiredInheritedControlSystems` — `"Aspire"` absent from assertion array despite boundary doc including Aspire in the Inherited Platform Controls table (story task 3 requires inherited controls from "EventStore, Tenants, Parties, Dapr, Aspire, and the broader Hexalith platform"). Added `"Aspire"` to the array. Tests still pass.

**Post-fix test results:** 18 targeted tests pass; full solution 1482 tests, 0 failures.

### File List

- `docs/responsibility-boundaries.md` (new; modified by review — PRD/architecture links added to Related Documentation)
- `docs/integration-guide.md` (modified — cross-reference added)
- `docs/release-evidence/conformance-manifest-v1-fixture.json` (modified — entry 17 added)
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ResponsibilityBoundaryValidationTest.cs` (new; modified by review — "Aspire" added to RequiredInheritedControlSystems)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified — Story 6.7 section added)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status → done)
- `_bmad-output/implementation-artifacts/6-7-publish-responsibility-boundary-documentation.md` (modified — story file)
