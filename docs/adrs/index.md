# Architecture Decision Records

This tracker lists decisions required before dependent Conversations behavior is implemented. Story 1.1 creates the decision scaffolding only; future stories must link to an accepted ADR, an approved readiness-gate decision, or an explicit waiver before implementing dependent behavior.

## Readiness Sources

- [Readiness Gates](../../_bmad-output/implementation-artifacts/readiness-gates.md)
- [Readiness Gate Decisions, 2026-05-17](../../_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md)

## Decision Topics

| Topic | Current source | Status |
| --- | --- | --- |
| Idempotency contract | [ADR 0001](0001-idempotency-contract.md) | Accepted |
| Tenant projection freshness | Readiness gate decision plus ADR required for production durability | Proposed |
| Governance audit pairing | ADR required before governance commands | Proposed |
| Event schema evolution | ADR required before versioned events | Proposed |
| Redaction replay | ADR required before redaction behavior | Proposed |
| Party hydration degraded states | Readiness gate decision plus ADR required before read hydration | Proposed |
| FrontComposer trust boundaries | ADR required before admin UI contracts | Proposed |
| Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle | Readiness gate decision plus ADR required before governance lifecycle behavior | Proposed |
| EventStore envelope ownership | Readiness gate decision exists | Decided externally |
| Command availability metadata | Readiness gate decision exists | Decided externally |
| Temporal evidence anchor | Readiness gate decision exists | Decided externally |
| Projection freshness blocking semantics | Readiness gate decision exists | Decided externally |

## Template

Use [ADR 0000](0000-template.md) for new decision records.
