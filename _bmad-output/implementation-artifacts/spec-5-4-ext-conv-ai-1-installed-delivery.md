---
title: 'Complete EXT-CONV-AI-1 installed delivery'
type: 'feature'
created: '2026-10-25'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'a5cea5629e01803e0cd09e985cb2ee46264d6dcb'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/implementation/ext-conv-ai-1-runtime-qualification.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agents Story 5.4 still has EXT-CONV-AI-1 Committed and unavailable at `87e8b4a6ec3224b20a560b0e83de857805925572`. The October 25 reconciliation found no installed manifest, no twelve frozen-input hashes, and no actual Party, worker, approval, source, publication, transport, receiver, or lookup authority. Published receiver source is not installation.

**Approach:** Install those Conversations-owned bindings for real, prove the conformance receiver is separately authenticated and contains no Agents package or runtime, and return one complete replacement packet for acceptance. Keep all six seams and C1–C4. Do not run qualification before the accepted prerequisites.

## Boundaries & Constraints

**Always:** Follow the register's six-seam artifact and OQ-5.4-1. Branch B identity is verified by immutable tenant and Party id. The receiver is a separate EventStore workload for `DeletionSignalAccepted`, Level 4 only, with its own authentication. Recompute every hash from installed bytes. Pin one canonical absolute attempt-journal directory and do not reserve an attempt. The packet names the clean canonical target, one explicit integration date, and the exact Live command. Historical evidence-boundary reproduction is not applicable; use current-change validation and focused checks.

**Never:** Treat a build, publish, or source review as installed authority. Manufacture credentials, enrollments, anchors, or a host `passPath: true` result. Reuse `ext-secrets-1-isolated` or the sample EventStore AppHost. Overwrite `delivery-20261022-71fab44` or `delivery-20261025-ed53473`, or any existing attempt journal. Execute C1–C4, reserve a cohort attempt, enable Story 8.3, or claim Available. Edit the Agents register before this packet is accepted. Revert `ffbb947` or `a5cea56`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Installed binding | Enrolled service Party, current worker, approval issuer, source and publication authority, host transport, separate receiver and lookup | Manifest plus twelve raw file hashes, journal path, canonical target, integration date, and exact command | A missing or named-only input stays null and the packet stays NotSubmitted |
| Receiver separation | Installed conformance process and its restore graph | Distinct receiver and lookup principals; zero `Hexalith.Agents` packages or runtime paths | Closure or shared-principal failure blocks the packet |
| Qualification gate | Protection still Uncommitted, or host Live evidence absent | No HTTP cohort and no attempt reservation; original attempts untouched | Refusal evidence is additive and does not claim Available |

</frozen-after-approval>

## Open Questions

- Target integration date — options: `2026-10-25`, the UTC date of this request, as the fresh promise while `2026-10-08` stays historical (the packet commits to today) / a different date you name (the packet uses only that date)
- Installation locus — options: deploy a new `ext-conv-ai-1` qualification namespace for the Conversations-owned receiver, transport, source/publication authority, and service-Party enrollment, leaving host Live evidence and protection null until those owners supply real bytes (no C1–C4; packet complete only for bindings this install actually proves) / you supply the existing bases, nine credential references, Party and worker records, approval issuer, and the twelve input files (implementation verifies those bytes and does not deploy a second stack)

## Code Map

- `src/Hexalith.Conversations.Server/Program.cs` — thin host stays fail closed. Do not opt it in.
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs` — reuse `AddConversationDeletionRuntime`; registration already rejects unavailable providers and does not prove enrollment.
- `src/Hexalith.Conversations.Server/Agents/` — reuse `ConversationDeletionDeliveryPump`, `ConfiguredConversationDeletionWorker`, and `ConfiguredConversationDeletionReceiptVerifier`. Ports `IConversationDeletionReceiver`, `IConversationAgentAuthority`, `IConversationDeletionApprovalVerifier`, and `IConversationCommandSourceVerifier` still have only `Unavailable*` defaults. Add adapters only over installed SDK/EventStore providers.
- `tools/Hexalith.Conversations.DeletionConformance/` — reuse the Agents-free receipt domain. Publish is not installation. Its host must be non-Development and separately enrolled.
- `references/Hexalith.EventStore` `AddEventStoreSourcePublicationAuthority`, `AddEventStoreSourcePublicationAuthorityJournal`, and `AddEventStoreCompleteCommandSourceVerification` — source providers exist; no host calls them. Do not edit that submodule unless the chosen locus requires an EventStore install change owned there.
- `eng/verify-ext-conv-ai-1-live.ps1` and `eng/ext-conv-ai-1-qualification.ps1` — `Get-OqQualification` and `Reserve-OqAttempt` already refuse before HTTP. Do not weaken them.
- `docs/implementation/ext-conv-ai-1-runtime-qualification.md` — manifest, twelve files, and operation inventory. Correct only statements that contradict the new installed evidence.
- `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/` — append a new delivery directory. Do not write the two retained deliveries.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Conversations.Server/Agents/` — add the host delivery transport and installed command-source, approval, and Party-authority adapters — the pump currently has no production receiver.
- [ ] `tools/Hexalith.Conversations.DeletionConformance/` — install and authenticate the receiver and independent lookup on the chosen locus — source publish leaves `receiverInstalled` false.
- [ ] Chosen installation — enroll the service Party and current worker, and bind source/publication authority where the locus actually provides them — string references are not enrollment.
- [ ] New evidence directory — write the manifest, recompute all twelve raw SHA-256 values, record the journal path, canonical target, integration date, and exact command — nulls stay null.
- [ ] `tests/Hexalith.Conversations.DeletionConformance.Tests/` and the receiver restore graph — prove separate principals and no Agents package — published source alone is insufficient.

**Acceptance Criteria:**
- Given the chosen installed bytes, when the replacement packet is produced, then every non-null hash matches a recomputed file and the exact command names that canonical target.
- Given unmet protection or host Live prerequisites, when packaging finishes, then no cohort attempt exists and C1–C4 remains `NOT_RUN`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Focused Debug builds and the Deletion Conformance test assembly — expected: receiver cases pass.
- Restore-graph inspection of the installed receiver — expected: no `Hexalith.Agents` package.
- Recomputed SHA-256 of the manifest and twelve files — expected: equality with the packet, or an explicit null.
- Do not run `eng/verify-ext-conv-ai-1.ps1 -Mode Live` before accepted prerequisites.

Historical Story 10.4 evidence-boundary verification is not applicable to this new delivery. `FAIL` and `BLOCKED` are not expected from that retired route.
