---
title: "Product Brief: Hexalith.Conversations"
status: "complete"
created: "2026-05-07T14:13:00.5716618+02:00"
updated: "2026-05-09T00:00:00.0000000+02:00"
inputs:
  - "User discovery notes from 2026-05-07"
  - "Hexalith.EventStore/_bmad-output/planning-artifacts/product-brief-Hexalith.EventStore-2026-02-11.md"
  - "Hexalith.Tenants/_bmad-output/planning-artifacts/product-brief-Hexalith.Tenants-2026-03-06.md"
  - "Hexalith.Parties/_bmad-output/planning-artifacts/product-brief-Hexalith.Parties-2026-03-01.md"
  - "Hexalith.FrontComposer/docs/skills/frontcomposer"
  - "Hexalith.Parties/docs/tenant-access-projection.md"
  - "Hexalith.Tenants/docs/event-contract-reference.md"
---

# Product Brief: Hexalith.Conversations

## Executive Summary

Hexalith.Conversations is a tenant-isolated, event-sourced conversation service for the Hexalith ecosystem. It manages durable exchanges between business users, AI agents, and large language models, preserving the ordered history, participants, attachments, retention state, redaction state, and operational metadata needed to make AI-assisted work auditable, governable, resumable, and reusable across Hexalith applications.

The first consumer is the Hexalith chatbot, but the module is intentionally broader than a transcript store. It is the shared conversation backbone other modules attach to: a conversation can belong to a `Hexalith.Projects` project, be organized through `Hexalith.Folders`, and include every human user, AI agent, and LLM as a participant via `Hexalith.Parties`. Persistence is handled by `Hexalith.EventStore`, tenant access by local projections of `Hexalith.Tenants` events, and administration views by `Hexalith.FrontComposer`.

**Why now:** the Hexalith chatbot needs durable, governed conversation persistence as a near-term unblock, and the broader Hexalith AI agent strategy requires a shared memory layer *before* additional agents proliferate and each invents its own storage, tenant filtering, and audit model. Building this once now prevents a future round of fragmentation.

## The Problem

AI-enabled applications need conversation state that survives beyond a single model call. Business users expect to return to a prior exchange, see what happened, attach supporting documents, and continue work without losing context. AI agents need the same history to coordinate, inspect prior decisions, and produce grounded follow-up actions. Operators need a trustworthy record when something goes wrong.

Today this capability is fragmented. Each chatbot stores transcripts locally, each agent framework keeps provider-specific sessions, and applications bolt on file references and audit fields. The result:

- History becomes provider-coupled, blocking provider switches and mixed history patterns.
- Tenant isolation is reimplemented per surface, increasing cross-tenant leak risk in a sensitive domain.
- Attachments are disconnected from the conversation that gives them meaning.
- Records lack stable actor identity, project/folder context, and an auditable event history.
- Retention, redaction, and sensitive-data handling are bolted on as afterthoughts.
- Every new Hexalith AI surface pays the same integration tax.

## The Solution

Hexalith.Conversations provides a conversation aggregate and supporting projections for managing AI-assisted exchanges as first-class business records. The MVP focuses on linear conversations: participants append messages and attachment references to an ordered thread, and the service preserves the event history needed to replay, inspect, and project the conversation.

Each conversation captures: tenant context (enforced via `Hexalith.Tenants`); party/actor context for human users, AI agents, and LLMs (via `Hexalith.Parties`); optional project link (`Hexalith.Projects`); optional folder and attachment references (`Hexalith.Folders`); message sequence with actor role, timestamps, and correlation identifiers; and retention, redaction, and sensitive-data metadata so administrators can govern records without destroying the audit trail.

The Hexalith chatbot remains out of scope and consumes the service rather than owning persistence. This keeps the chatbot focused on interaction experience while Conversations owns the durable record.

## What Makes This Different

- **Event-sourced and tenant-isolated by design** — every change is a domain event; tenant decisions stay owned by `Hexalith.Tenants`, consumed via local projections that fail closed.
- **Business context native** — conversations attach to projects, folders, files, and parties instead of living as isolated transcripts.
- **Provider-portable AI history** — the application-owned record persists even when a model provider also stores session or response IDs.
- **Governance built in** — retention, redaction, and sensitive-data handling are part of the lifecycle, not external cleanup scripts.
- **Agent- and FrontComposer-ready** — agents inspect/append/resume conversations as structured domain data; command and projection metadata feed generated admin views.

## Who This Serves

**Primary — business users and AI agents.** Business users return to a project conversation, see relevant messages and attachments in context, and continue work without searching across chat windows, file systems, and notes. AI agents recover decisions, cite attachments, coordinate with another actor, and continue a workflow without asking the user to restate context. LLMs participate as parties, giving each model a stable identity for attribution and governance.

**Secondary — chatbot developers, application developers, platform administrators.** Developers integrate via reusable contracts/clients instead of bespoke transcript code. Administrators get tenant-scoped browse, inspect, and governance views through FrontComposer.

## Success Criteria

MVP success is measured by whether Conversations becomes the default persistence and context backbone for Hexalith AI interactions.

| # | Signal | Target |
|---|--------|--------|
| 1 | Chatbot integration | The Hexalith chatbot creates a conversation, appends user/LLM messages, attaches files via `Hexalith.Folders`, and resumes the same linear thread end-to-end |
| 2 | Cross-module adoption | At least 2 Hexalith modules consume Conversations within 6 months of GA (chatbot + one additional consumer) |
| 3 | Tenant isolation | Zero cross-tenant access in conformance tests; fail-closed verified for missing or stale tenant projection state |
| 4 | Governance auditability | 100% of retention and redaction state changes emit domain events with rationale; no path mutates governance state without an audit record |
| 5 | Conversation continuity | A user retrieves a prior conversation with full participant, message, and attachment context in a single request |
| 6 | Provider portability | Conversation history is fully recoverable independent of any LLM provider's session or response identifiers |
| 7 | Developer ergonomics | A new consumer integrates via contracts/client packages without learning EventStore internals or duplicating tenant checks |

## MVP Scope

**In scope:**

- Conversation aggregate with tenant-scoped identity and ordered linear message history.
- Commands: create conversation; add participant/actor reference; append message; attach file reference; close/archive; update title/metadata; set/update retention policy; mark sensitive data; redact message content with rationale.
- Events and projections: conversation list, detail, participant list, message timeline, attachment list, retention state, redaction state, sensitive-data flags, recent activity.
- Stable-identifier links to `Hexalith.Projects`, `Hexalith.Folders`, `Hexalith.Parties`; ownership remains in those modules.
- EventStore persistence, idempotent command handling, pub/sub event publication.
- FrontComposer-compatible command/projection metadata.
- Tenant access enforced via local projections of `Hexalith.Tenants` events; fail-closed on missing/stale state.
- Sensitive-data policy: preserve audit event stream; redact projected/displayed content when required; binary file content remains owned by `Hexalith.Folders`.

**Out of scope:** branching/forked conversations; semantic memory, vector search, automatic summarization; chatbot UI and orchestration; LLM provider abstraction beyond storing correlation IDs; real-time collaborative editing or live streaming UX; multi-agent planning workflows; attachment binary storage; full compliance automation for every retention or classification regime.

## Vision

If successful, Hexalith.Conversations becomes the shared memory and audit layer for AI-assisted business workflows across Hexalith. Chatbots, autonomous agents, project assistants, and administration tools all use the same conversation record. Business users gain continuity, AI agents gain structured context, operators gain a tenant-isolated audit trail.

Over time, the module can add summaries, decision and action extraction, semantic recall, branching, advanced retention automation, compliance workflows, and MCP tools for agents. The long-term goal is not to compete with LLM provider chat history but to own the *business record* of AI collaboration inside Hexalith.
