---
title: "Product Brief Distillate: Hexalith.Conversations"
type: llm-distillate
source: "product-brief-Hexalith.Conversations.md"
created: "2026-05-09T00:00:00.0000000+02:00"
purpose: "Token-efficient context for downstream PRD creation"
---

# Hexalith.Conversations — Detail Pack for PRD

## What it is (one line)

A tenant-isolated, event-sourced **conversation aggregate + projections** module that owns the durable business record of AI-assisted exchanges (human users, AI agents, LLMs) inside the Hexalith ecosystem. Not a chatbot; not a transcript table.

## Strategic framing

- **Primary unblock:** Hexalith chatbot needs durable, governed conversation persistence — near-term gating dependency.
- **Strategic framing:** build the shared memory layer once, before more Hexalith AI agents proliferate and each invents its own storage / tenant filtering / audit model.
- **Non-goal:** competing with LLM provider chat history. Goal is owning the *application/business* record of AI collaboration.

## Domain model hints (for PRD/architecture)

- **Aggregate:** `Conversation`, tenant-scoped identity.
- **Granularity:** linear (ordered) thread for MVP; no branching/forking.
- **Members of state:** title/metadata, ordered messages, participants (parties), attachment refs, retention policy, redaction state, sensitive-data flags, optional project link, optional folder link, correlation IDs, provider session/response IDs (as metadata only).
- **Commands (MVP set):**
  - `CreateConversation`
  - `AddParticipant` (party reference)
  - `AppendMessage` (with actor role, timestamp, optional correlation ID)
  - `AttachFileReference` (folder/file ref from `Hexalith.Folders`)
  - `UpdateTitleOrMetadata`
  - `CloseOrArchiveConversation`
  - `SetRetentionPolicy` / `UpdateRetentionPolicy`
  - `MarkSensitiveData`
  - `RedactMessageContent` (with rationale)
- **Projections (MVP set):** conversation list, conversation detail, participant list, message timeline, attachment list, retention state, redaction state, sensitive-data flags, recent activity.
- **Events:** every meaningful state change emits a domain event; commands must be idempotent; events published via pub/sub per Hexalith conventions.

## Integration map (who owns what)

| Concern | Owner | Conversations consumes via |
|---|---|---|
| Tenant lifecycle, membership, roles | `Hexalith.Tenants` | local projection of tenant events; **fail closed** on missing/stale state |
| Identity of human users, AI agents, LLMs | `Hexalith.Parties` | participant references by stable party ID |
| Project context | `Hexalith.Projects` | optional project link by stable ID |
| Folder organization + attachment binaries | `Hexalith.Folders` | attachment references only; binaries stay there |
| Persistence | `Hexalith.EventStore` | aggregate persistence + event sourcing |
| Admin UI | `Hexalith.FrontComposer` | command/projection metadata feeds generated/composed views |

## Governance model (a stated differentiator — treat as core)

- Retention, redaction, and sensitive-data handling are **part of the conversation lifecycle**, not external scripts.
- Audit invariant: governance state changes always emit a domain event; **no path mutates governance state without an audit record**.
- Redaction model: preserve audit event stream; redact projected/displayed content when required.
- Binary file content: out of scope for this module (lives in `Hexalith.Folders`).
- MVP explicitly does **not** automate full compliance for every retention or classification regime — module provides hooks + audit trail only.

## Tenant isolation rules

- All conversation reads/writes are tenant-scoped.
- Tenant decisions are not local — they're consumed from `Hexalith.Tenants` projections.
- **Fail-closed** semantics on missing, stale, or unknown tenant projection state.
- Conformance tests must verify zero cross-tenant access (one of the 7 success signals).

## Success signals (measurable)

1. Chatbot end-to-end loop works (create → append user/LLM messages → attach via Folders → resume).
2. ≥ 2 Hexalith modules consume Conversations within 6 months of GA (chatbot + 1 more).
3. Zero cross-tenant access in conformance tests; fail-closed verified.
4. 100% of governance state changes emit auditable events with rationale.
5. Single-request retrieval of conversation with full participants, messages, and attachments.
6. Conversation history fully recoverable independent of any LLM provider's session/response IDs.
7. New consumer integrates via contracts/clients without learning EventStore internals or duplicating tenant checks.

## Scope signals

**In (MVP):** linear conversations, the command/projection/event sets above, stable-ID links to Projects/Folders/Parties, EventStore persistence with idempotency, pub/sub publication, FrontComposer-compatible metadata, tenant fail-closed enforcement, sensitive-data preservation+redaction policy.

**Out (deferred — do not re-propose for MVP):**
- Branching / forked conversations.
- Long-term semantic memory, vector search, automatic summarization.
- Chatbot UI and chatbot orchestration logic.
- LLM provider abstraction (beyond storing correlation/session IDs as metadata).
- Real-time collaborative editing or live streaming UX.
- Multi-agent planning workflows.
- Attachment binary storage (owned by `Hexalith.Folders`).
- Full compliance automation per retention/classification regime.

## Future-vision capabilities (post-MVP, not for first PRD)

Conversation summaries, decision/action extraction, semantic recall, branching, advanced retention automation, compliance workflows, MCP tools for agents.

## Personas (PRD-grade)

- **Business user** — wants to return to a project conversation and continue work without searching across chat windows, file systems, and project notes. Success = full context restored in one place.
- **AI agent** — wants structured permissioned memory of prior interaction. Success = recovers decisions, cites attachments, coordinates with another actor without asking the user to restate context.
- **LLM (as participant)** — modeled as a party for stable identity; enables attribution, provider correlation, audit, governance.
- **Chatbot/application developer** — wants reusable client + contract packages, not bespoke transcript storage. Success = integrates without learning EventStore internals or reimplementing tenant checks.
- **Platform administrator** — wants tenant-scoped browse/inspect/governance views via FrontComposer. Success = retention/redaction actions happen with auditable trail.

## Architectural assumptions (carry into PRD)

- DDD + CQRS + event sourcing + DAPR-native — same model as the rest of Hexalith.
- Persistence path is `Hexalith.EventStore`; do not invent a parallel store.
- Admin UI path is `Hexalith.FrontComposer` — drive UI from command/projection metadata, do not hard-code domain UI behavior.
- Conversation aggregate boundary is the unit of consistency; cross-aggregate references (party, project, folder) are stable IDs only.

## Open questions (for PRD discovery)

- **Specific SLO/latency targets** for `AppendMessage` and `LoadConversationTimeline` — not yet committed; success metrics state capability, not numbers.
- **Identity of the second adopter** for the 6-month adoption signal — chatbot + ? (project assistant? agent runner? specific application?).
- **Retention policy schema** — what fields, what enforcement loop, manual vs. automatic application?
- **Redaction granularity** — per-message vs. per-field vs. per-attachment-ref?
- **Sensitive-data flag taxonomy** — single boolean vs. classification labels?
- **Consumer client packaging** — one client package per consumer language, or shared contracts + per-language clients?
- **LLM provider correlation metadata schema** — minimal common surface vs. provider-specific bag.
- **Failure mode for stale tenant projection** — exact behavior beyond "fail closed" (queue commands? reject? circuit-break?).
- **Conversation closure/archival semantics** — soft state vs. immutable; can a closed conversation be reopened?
- **Pub/sub event contract versioning** — handled by EventStore conventions or addressed at the Conversations contract layer?

## Sibling-module context (PRD shouldn't re-research)

Sibling product briefs already exist for:
- `Hexalith.EventStore` (2026-02-11)
- `Hexalith.Tenants` (2026-03-06)
- `Hexalith.Parties` (2026-03-01)

Reference docs cited as inputs:
- `Hexalith.FrontComposer/docs/skills/frontcomposer`
- `Hexalith.Parties/docs/tenant-access-projection.md`
- `Hexalith.Tenants/docs/event-contract-reference.md`

These should be loaded as supporting context for the PRD, not re-discovered.

## Discovery confidence

**Medium-high.** Brief was loaded from an existing draft and refined; no new web/artifact discovery was run in this session. Open questions above were surfaced from gaps in the existing draft, not from fresh research. PRD discovery should validate the open-questions list against current state of sibling modules.
