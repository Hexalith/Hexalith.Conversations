---
title: 'Story 12.1: Approve derived-key ownership, lifecycle, and rollback'
type: 'feature'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Derived-key lifecycle lacks the approved decision required before optimization.

**Approach:** Produce ADR 0005, deterministic evidence, independent validation and the six-scenario record contracted by `12.1.json`.

## Boundaries & Constraints

**Always:** EventStore alone owns domain writes. Cover seven families, including resolution metadata/selection/proof/fence; define ownership, ordering, compatibility, rebuild, backfill, deletion, expiry and rollback. Preserve isolation and fail-closed recovery; reads never repair. Label proposed behavior. Obtain named owner approval of the presented decision's exact digest.

**Never:** Change runtime, benchmarks, dependencies, gitlinks or history; imply 16.3 completion/approval. Story rollback removes 12.1 additions; runtime rollback rebuilds from EventStore.

## I/O & Edge-Case Matrix

| Scenario | Expected result |
| --- | --- |
| Complete lifecycle and approved digest | Deterministic, independently verified pair |
| Omitted family/field | `DERIVED_KEY_ADR_INCOMPLETE` |
| Second write authority or read repair | `EVENTSTORE_AUTHORITY_VIOLATED` |
| Unsafe transition or rollback | `DERIVED_KEY_TRANSITION_UNSAFE` / `ROLLBACK_PLAN_INVALID` |
| Absent or stale approval | `ADR_APPROVAL_MISSING` |
| Unavailable history | BLOCKED; never PASS |

</frozen-after-approval>

## Open Questions

1. **Entry:** `12.1.json` requires `16.3`, still draft/backlog without scoped entry approval. **Keep the gate** (wait for predecessor/entry approval), or **authorize bounded preparation** (ADR/tooling now; disclose the missing predecessor; acceptance/completion blocked; digest approval later). Neither marks 16.3 done. The lift covers only `7.1-SCHEMAS`; current-change policy retires routine authority gates but grants no product safety approval.

## Code Map

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` — seven families: conversation, tenant index, dispatch ledger, resolution metadata, selection, proof and fence. Spell out the tenant-wide index and ledger exceptions; runtime stays read only.
- `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs` — `ProjectAsync` orders pending proof/index, rich writes, metadata/selection, durability checks, proof/fence completion, then ledger completion; `PrepareRebuildAsync` covers seven families. Only the ledger explicitly expires.
- `src/Hexalith.Conversations.Server/Projections/ConversationResolutionMetadataPersistence.cs` and `ConversationProjectionReadStore.cs` — writer recovery and read-side proof checks; lasting proof outlives the ledger. Label general migration/deletion/backfill as proposed.
- `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md` — final record; distinguish its candidate from `docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json`'s deletion candidate.
- `_bmad/scripts/{generate_thin_authoring_guidance,verify_thin_authoring_guidance,evidence_boundary_current,authoring_outputs}.py` — canonical pairs, excluded approval, neutral Git bridge and contained atomic writers. Native readers reuse `tests/Hexalith.Conversations.TestSupport/`; pin ownership independently in consuming test source.
- `tests/Hexalith.Conversations.Server.Tests/Projections/{ConversationAsyncProjectionHandlerTest,ConversationResolutionMetadataRecoveryTest,ConversationResolutionMetadataProjectionTest}.cs` — existing ordering/rebuild/recovery evidence.

## Tasks & Acceptance

**Execution:**
- [ ] `docs/adrs/0005-conversation-derived-key-lifecycle.md` — lifecycle, dispatch exception, versioning, recovery.
- [ ] `docs/release-evidence/derived-key-lifecycle-decision-v1.schema.json` — closed candidate/lifecycle/approval/digest schema.
- [ ] `_bmad/scripts/{generate,verify}_derived_key_decision.py` — deterministic generation and independent checks; approval outside signable payload.
- [ ] `_bmad/scripts/verify_story_candidate.py` — 12.1 route, immutable 6.2/baseline binding, `PREDECESSOR_CANDIDATE_MISMATCH`; preserve 11.3.
- [ ] `tests/Hexalith.Conversations.Conformance.Tests/DerivedKeyLifecycleAdrValidationTest.cs` — `EventStoreShouldRemainOnlyWriteAuthority`; independent pins; internal compilation only.
- [ ] `_bmad/scripts/tests/test_derived_key_decision.py` — `lifecycle_fault_matrix` covering every field/family and matrix row, exact blockers, nonempty ledgers and restored bytes.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — 12.1 bindings, receipts/mutations/insertion; reject stale/vacuous evidence.
- [ ] `docs/release-evidence/{derived-key-lifecycle-decision-v1,story-12.1-final-record-v2}.{json,md}`, `artifacts/v9/12.1/` — committed-source measurement, digest approval, deterministic regeneration and atomic publication after commitlint.
- [ ] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — review, verbatim insertion, gated transition.

**Acceptance Criteria:**
- Given committed authorized sources, when six commands run, then `6/6/0/0/0/0` truthfully binds predecessors/substitutions.
- Given identical inputs, when regenerated, then pairs match bytes and history stays unchanged.
- Given each fault, when checked, then exact blockers occur and bytes restore.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Debug checks, then exact `_bmad-output/planning-artifacts/v9/story-contracts/12.1.json` commands, including Release receipt.
- Derived-key/final-record Python suites, controls and disabled-guard probes.
- Story 7.3/insertion guards; scoped 12.1 verification before review/done. Preserve FAIL/BLOCKED; no retired publishers.
