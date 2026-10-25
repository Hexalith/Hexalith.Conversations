---
title: 'Complete EXT-CONV-AI-1 installed delivery'
type: 'feature'
created: '2026-10-25'
status: 'done'
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

**Never:** Treat a build, publish, or source review as installed authority. Manufacture credentials, enrollments, anchors, or a host `passPath: true` result. Reuse `ext-secrets-1-isolated` or the sample EventStore AppHost. Deploy a new `ext-conv-ai-1` namespace or any second stack. Overwrite `delivery-20261022-71fab44` or `delivery-20261025-ed53473`, or any existing attempt journal. Execute C1–C4, reserve a cohort attempt, enable Story 8.3, or claim Available. Edit the Agents register before this packet is accepted. Revert `ffbb947` or `a5cea56`.

**Decisions (2026-10-25, apply recommended):** Keep this full spec. Its length, about 1,950 tokens, is accepted. The replacement `TargetIntegrationDate` is `2026-10-25` when the packet is sealed on that UTC date; `2026-10-08` stays the historical missed commitment, and no later date is named in advance. Installation uses existing inputs only. No bases, credential references, Party or worker records, approval issuer, or twelve input files were supplied. Verify bytes that exist. Every unsupplied binding stays null. Do not run C1–C4.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Installed binding | Enrolled service Party, current worker, approval issuer, source and publication authority, host transport, separate receiver and lookup | Manifest plus twelve raw file hashes, journal path, canonical target, integration date, and exact command | A missing or named-only input stays null and the packet stays NotSubmitted |
| Receiver separation | Installed conformance process and its restore graph | Distinct receiver and lookup principals; zero `Hexalith.Agents` packages or runtime paths | Closure or shared-principal failure blocks the packet |
| Qualification gate | Protection still Uncommitted, or host Live evidence absent | No HTTP cohort and no attempt reservation; original attempts untouched | Refusal evidence is additive and does not claim Available |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Program.cs` — thin host stays fail closed. Do not opt it in.
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs` — reuse `AddConversationDeletionRuntime`; registration already rejects unavailable providers and does not prove enrollment.
- `src/Hexalith.Conversations.Server/Agents/` — reuse `ConversationDeletionDeliveryPump`, `ConfiguredConversationDeletionWorker`, and `ConfiguredConversationDeletionReceiptVerifier`. Ports `IConversationDeletionReceiver`, `IConversationAgentAuthority`, `IConversationDeletionApprovalVerifier`, and `IConversationCommandSourceVerifier` still have only `Unavailable*` defaults. Add adapters only over installed SDK/EventStore providers.
- `tools/Hexalith.Conversations.DeletionConformance/` — reuse the Agents-free receipt domain. Publish is not installation. Do not deploy a new host.
- `references/Hexalith.EventStore` `AddEventStoreSourcePublicationAuthority`, `AddEventStoreSourcePublicationAuthorityJournal`, and `AddEventStoreCompleteCommandSourceVerification` — source providers exist; no host calls them. Do not edit that submodule.
- `eng/verify-ext-conv-ai-1-live.ps1` and `eng/ext-conv-ai-1-qualification.ps1` — `Get-OqQualification` and `Reserve-OqAttempt` already refuse before HTTP. Do not weaken them.
- `docs/implementation/ext-conv-ai-1-runtime-qualification.md` — manifest, twelve files, and operation inventory. Correct only statements that contradict the new installed evidence.
- `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/` — append a new delivery directory. Do not write the two retained deliveries.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Conversations.Server/Agents/` — bind a host delivery transport and command-source, approval, and Party-authority adapters only to an existing installed provider — no provider was supplied, so do not add a substitute.
- [x] `tools/Hexalith.Conversations.DeletionConformance/` — verify an existing separately authenticated receiver and independent lookup — no installation was supplied, so do not deploy one.
- [x] Existing enrollment records — verify the service Party, current worker, and source/publication authority from supplied bytes — none were supplied, so those bindings stay null.
- [x] New evidence directory — write the manifest, recompute every supplied raw SHA-256, and record the journal path, canonical target, `2026-10-25` only if sealed on that UTC date, and the exact command — unsupplied fields stay null.
- [x] `tests/Hexalith.Conversations.DeletionConformance.Tests/` and any supplied receiver restore graph — prove separate principals and no Agents package when a receiver exists — a source tree alone does not count.

**Acceptance Criteria:**
- Given the chosen installed bytes, when the replacement packet is produced, then every non-null hash matches a recomputed file and the exact command names that canonical target.
- Given unmet protection or host Live prerequisites, when packaging finishes, then no cohort attempt exists and C1–C4 remains `NOT_RUN`.

## Implementation Notes

Sealed on 2026-10-25 UTC. No installed provider, receiver, Party, worker, approval issuer, source or publication authority, bases, credential references, or twelve input files were present. No adapter, deployment, credential, enrollment, anchor, or host `passPath` was added. The thin host was not opted in. The EventStore submodule and the Agents register were not edited. `ffbb947` and `a5cea56` were not reverted. The two retained deliveries were not rewritten.

The replacement packet is `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261025-8039d2a/replacement-acceptance-packet.json`. It names canonical target `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`, `TargetIntegrationDate` `2026-10-25`, journal `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/attempt-journal`, and the exact Live command that pins that commit. Unsupplied bindings and the twelve file hashes are null. Status is `NotSubmitted`. The journal directory was created empty and no attempt file was written. C1–C4 remain `NOT_RUN`. `Available` is not claimed. The checkout was not clean at seal time; the command's clean-tree predicate records that fact and was not executed.

The deletion-conformance assembly passed 31 component cases. No installed receiver restore graph was supplied, so source and Debug outputs were not treated as separation proof. The packet's `packetComplete` flag is false. Its owner, six-seam artifact, evidence level, and consuming stories are copied from the retained October 22 packet. `tests/tooling/test_ext_conv_ai_1_installed_delivery_packet.py` passed 3 cases: null bindings stay `NotSubmitted`, an absent receiver makes no separation claim, and the journal has no reservation.

## Spec Change Log

## Review Triage Log

Evidence-boundary verification is not applicable. No review layer was skipped. The diff also contains concurrent Epic 16 work this delivery did not author.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind: Epic 16 `runtimeQualified` can disagree with held authority flags | maybe-false | `epic16_tooling.generate` was not re-executed here. If true, a runtime record could claim qualification while execution stays held. Unverified medium. Defer. |
| Blind: `failed()` keeps a successful measurement payload | maybe-false | The write-failure path was not executed. If true, a FAIL receipt would still carry PASS counts. Unverified medium. Defer. |
| Blind: AC-16.1-05 and AC-16.2-05 trust caller receipt strings | maybe-false | Dependency comparison was not re-read beyond the delivery files. If true, those scenarios can pass on drifted observations. Unverified medium. Defer. |
| Blind: capture inventory trusts in-memory hashes | maybe-false | `capture.inventory` was not executed. If true, a PASS can name bytes the final inventory rejects. Unverified medium. Defer. |
| Blind: TRX `Counters` and `Warning` are under-constrained | maybe-false | The TRX parser was not executed. If true, a partial counter element can pass. Unverified medium. Defer. |
| Blind/edge: missing `SourceRevisionId` still passes | medium | `Program.cs` rejects only when `revisions.Any` finds a mismatch, so an empty list passes. Concurrent Epic 16 helper. Defer. |
| Blind/edge: empty conversation attributes and absent tenant removals succeed | medium | `epic16_domain.py` assigns attributes only when the map is truthy, and `dict.pop(..., None)` ignores a missing member. Concurrent Epic 16 oracle. Defer. |
| Blind: native helper drops stderr and has no SDK pin | maybe-false | The helper build path was not run. If true, the oracle follows ambient `dotnet`. Unverified medium. Defer. |
| Blind/edge: successor import and `SystemExit` | medium | `generate_story_record.py` returns `epic16_tooling.main` with no `SystemExit` handler, and maps `ImportError` to `SCHEMA_UNAVAILABLE`. Concurrent Epic 16 dispatch. Defer. |
| Blind: delivery test and completed tasks | medium | `assertIn` accepts a longer `targetCommit`, and `glob("*")` ignores dotfiles. The claim that completed tasks required new adapters is false: those tasks forbid a substitute when no provider was supplied. Patch the test. |
| Blind: merge candidates are rejected | maybe-false | The parent-count check was not executed. If true, a merge cannot be the candidate. Unverified medium. Defer. |
| Blind: non-maximum schema errors use another code | maybe-false | The validator branch was not executed. If true, minimum and type errors are classified differently from `maximum`. Unverified low. Reject: everyday use of this delivery does not meet it, and the fix is not a direct correction here. |
| Blind: two-replicas and port-collision encodings | maybe-false | Those scenario tables were not executed. If true, one convergence encoding and one fault name are missing. Unverified medium. Defer. |
| Blind: zero checkpoint uses the last event | maybe-false | `reconciliation()` was not executed. If true, checkpoint 0 binds the final event. Unverified medium. Defer. |
| Blind: local validation transcripts and missing evidence sources | maybe-false | The transcript tree was not opened. If true, local paths are stored and two named sources are absent. Unverified medium. Defer. |
| Edge: capture `lstat` follows the read | medium | `no_alias` stats the path after the caller has read it, so a replaced inode can pass. Concurrent Epic 16 capture. Defer. |
| Edge: duplicate packet keys | low | `json.loads` keeps the last duplicate. A direct parser hook closes it. Patch. |
| Edge: extra installed-binding keys | medium | The test null-checks a fixed list and ignores any other key. Patch. |
| Edge: `targetCommit` substring | medium | `assertIn` passes when the canonical hash is only contained in a longer value. Patch. |
| Edge: journal dotfiles or missing directory | medium | `glob("*")` is empty for dotfiles and for a missing directory. Patch. |
| Edge: Story 16.1 dropped composition coverage | false | The tenant-access task is still present at the cited line. |
| Verification gap | none | That layer reported no gap. |

## Verification

**Commands:**
- Focused Debug builds and the Deletion Conformance test assembly — expected: receiver cases pass.
- Restore-graph inspection of the installed receiver — expected: no `Hexalith.Agents` package.
- Recomputed SHA-256 of the manifest and twelve files — expected: equality with the packet, or an explicit null.
- Do not run `eng/verify-ext-conv-ai-1.ps1 -Mode Live` before accepted prerequisites.

Historical Story 10.4 evidence-boundary verification is not applicable to this new delivery. `FAIL` and `BLOCKED` are not expected from that retired route.

**Observed 2026-10-25 UTC:**
- Server Debug build and deletion-conformance Debug build exited 0 with zero warnings. `dotnet` on `Hexalith.Conversations.DeletionConformance.Tests.dll` exited 0: Total 31, Failed 0, Skipped 0. Logs are under the new delivery directory's `component/` folder. These are component results.
- No installed receiver or restore graph was supplied. Source and Debug outputs were not inspected as an installed closure.
- The manifest SHA-256 in the packet matches a recomputation of `live-manifest.json`. All twelve file hashes are null. The exact command contains `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`.
- Live mode was not run. The attempt journal contains no reservation file. C1–C4 remain `NOT_RUN`.
- `python3 -m pytest -q -p no:cacheprovider tests/tooling/test_ext_conv_ai_1_installed_delivery_packet.py` exited 0: 3 passed. Evidence-boundary historical verification is not applicable.
