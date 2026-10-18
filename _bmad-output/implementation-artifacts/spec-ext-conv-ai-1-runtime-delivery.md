---
title: 'Complete EXT-CONV-AI-1 runtime delivery'
type: 'feature'
created: '2026-10-18'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '476369f83d8fbdcecaa3b2b9ef33bc310d4131a6'
context:
  - '/home/administrator/projects/hexalith/conversations/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="user-authorized delivery intent">

## Intent

**Problem:** Agents Story 5.4 has a Committed EXT-CONV-AI-1 source target, but its Live command stops before execution. Production still uses unavailable authority and source bindings; no current worker enrollment, approval issuer, or Agents 8.3 receipt endpoint was established in the handoff.

**Approach:** Complete the six Conversations seams using the existing contracts, EventStore source readers, catalogue, and deletion worker. Make C1–C4 Live verification executable only against authenticated persisted source and exact installed targets. Produce Local evidence and a replacement immutable-target/exact-command packet; retain the availability and Agents 8.3 gates.

## Boundaries & Constraints

**Always:** Verify the immutable Branch B provisioned Organization Party by tenant and id; use a dedicated Conversations service Party with current worker and tenant enrollment; require exact source and conditional append authority. Preserve typed absence, deletion, removal, denial, conflict and unavailable outcomes, source revisions, stable MessageId and deletion SignalId, ordered retry/checkpoint behavior and independent receiver receipt lookup. Count open undeleted Conversations created in `[from,to)` including zero Agent Calls. Authenticate source and target before dispatch. Keep Local and Live evidence distinct.

**Never:** Infer enrollment or credentials from fixtures, a named human, an operator kubeconfig, or a Dapr control plane. Treat submission as an accepted deletion receipt, skip poison entries, execute a Committed seam through consumer qualification, or fold the separately gated Agents 8.3 receiver into this owner delivery.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Membership and posting | Current enrolled service, exact Party and tenant, repeated membership or deterministic MessageId | One participant and one message with exact id and provenance; exact retries are no-ops | Changed role/type or post is typed conflict; revoked/foreign authority denies |
| Current reads and count | Exact authenticated source, creation window, edited/deleted message | Complete current roster/content and open count independent of Agent Calls | Distinct ConversationDeleted, PrincipalRemoved and Unavailable |
| Approved deletion | Atomic approval source and ordered publication | Same signal/revision across attempts; checkpoint only after authenticated exact receipt and independent lookup | Outage/rollover retries; altered same-id signal quarantines and blocks |
| Live qualification | Exact clean target plus installed authority, worker, receiver and storage | C1–C4 persisted lanes emit nonempty evidence | Missing input or failed lane blocks without claiming Available |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs`, `Program.cs` -- production defaults and host binding; retain fail-closed fallback.
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentAdmissionStage.cs` -- restricted current authorization and source proof; do not replace with projection authority.
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentSourceReader.cs`, `EventStoreConversationTenantCatalogue.cs` -- existing complete source folds and creation-window count.
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.Agents.cs`, `ConversationAgentQueryService.cs` -- existing membership, posting, deletion, roster and lookup folds.
- `src/Hexalith.Conversations.Server/Agents/ConversationDeletionDeliveryPump.cs`, `ConfiguredConversationDeletionWorker.cs`, `ConfiguredConversationDeletionReceiptVerifier.cs` -- existing ordered worker and receipt rules.
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs` -- opt-in participant POST; no production handler is currently registered.
- `eng/verify-ext-conv-ai-1.ps1` -- Local simulation; Live currently throws before calls.
- `../agents/_bmad-output/planning-artifacts/external-dependency-register.md`, `../agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/owner-inputs.md` -- accepted six-seam contract and missing runtime references.

## Tasks & Acceptance

**Execution:**
- [ ] Bind the production authority, Party identity, complete source/catalogue, conditional command proof, worker enrollment and receiver lookup through explicit verified runtime configuration; preserve unavailable defaults.
- [x] Expose and exercise the participant POST and all six typed client/service seams with focused persisted-state tests.
- [x] Implement C1–C4 Live lanes in `eng/verify-ext-conv-ai-1.ps1` with exact target, credential-reference, persistence, restart, retry, denial, poison and independent acknowledgement assertions.
- [ ] Run focused Local validation and prepare an immutable replacement target with exact command and explicit cross-owner qualification status.

**Acceptance Criteria:**
- Given a clean exact target and installed bindings, when the Live command executes, then C1–C4 each records nonempty persisted authenticated evidence and fails on any missing gate.
- Given unavailable owner runtime references or Agents 8.3 receiver, when qualification is requested, then no Available claim or consumer execution occurs.

## Implementation Notes

- The thin Conversations domain-service host remains fail closed. An external composition host can opt in only after supplying current authority, source, publication, receiver and client providers. The accepted handoff supplies no enrolled service Party, worker machine binding, approval issuer or independent receiver lookup; these cannot be manufactured by repository configuration.
- The Local matrix exercises serialized persisted-state simulations. The C1–C4 Live runner has fixed assertions and an accepted-target gate, but no installed manifest or cross-owner qualification cohort exists, so no Live lane has run or passed.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Local -ArtifactsPath /tmp/ext-conv-ai-1-local` -- passing current-source Local evidence.
- `pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Live -ArtifactsPath /tmp/ext-conv-ai-1-live` -- C1–C4 pass only after exact installed authority and receiver qualification; otherwise blocked.
