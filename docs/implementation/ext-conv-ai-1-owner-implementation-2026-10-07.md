# Conversations owner implementation evidence — 2026-10-07

The approved [Conversations owner spec](../../../agents/_bmad-output/implementation-artifacts/spec-5-4-conversations-owner-implementation.md) already had portable contracts, aggregate intents, client methods, SDK queries and admission ports for the six seams. This follow-up fixes an acknowledgement-state defect and makes the local source build reproducible. The complete dependency remains unavailable. Neither acceptance/register fields nor the original specification baseline were changed.

The observed Conversations HEAD is `e5b0a1ea2e76452e8a5a73e672ed33893172bfce`; the following changes are uncommitted. The original spec baseline remains `9938aa8aa5cfc082de6be3d28eede22957b14a26`. No staging, commits, remote operations, submodule initialization, deployment or live provider consumption was performed.

## Changed files and behavior

- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.Agents.cs`: a delivery Attempt carrying an acknowledgement now emits a durable quarantine fact with safe poison metadata. It stores no supplied receipt and advances no acknowledgement checkpoint.
- `src/Hexalith.Conversations/State/ConversationState.Agents.cs`: replay rejects receipts attached to non-acknowledgement transitions, changed accepted receipts, unknown actions and missing attempt/target identities. Malformed retained events produce Unavailable through the existing source reader.
- `tests/Hexalith.Conversations.Server.Tests/Agents/ConversationAgentSixSeamTests.cs`: the new regression exercises admitted malicious attempts, serialized quarantine, malformed retained attempts and changed accepted receipts. Existing membership coverage now also checks changed type and concurrent candidates evaluated against the same prefix. Existing acceptance expectations were preserved.
- `eng/verify-ext-conv-ai-1.ps1`: every Local execution has a unique child directory under the requested isolated artifact root. The verifier resolves the selected EventStore source version once and forwards it throughout the local graph. This prevents Tenants' older package observation from producing incompatible assembly versions in the same source output directory. It changes no dependency pin or repository configuration. Live still rejects before any calls.
- This note and `docs/implementation/ext-conv-ai-1-source-evidence-2026-10-07.json`: separate current evidence, preserving the earlier owner note and source manifest.

## Commands and measured results

Evidence is retained under `/tmp/hexalith-agents54-conversations-artifacts/run-20261007T1500191806094Z`. [The machine evidence](ext-conv-ai-1-source-evidence-2026-10-07.json) records exact commands, source and dependency revisions, dirty dependency observations, executed test names, logs/XML and built assembly hashes.

```bash
set -e
pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Local -ArtifactsPath /tmp/hexalith-agents54-conversations-artifacts
```

All four individual Debug/source-reference builds passed with zero warnings and zero errors. The seven required Local lanes executed: Membership, Posting, Facilitator, ActiveCount, CurrentReads, ApprovedDeletionDelivery and CrossTenant. The exact focused class is `Hexalith.Conversations.Server.Tests.Agents.ConversationAgentSixSeamTests`. All 15 tests passed, including `UnexpectedAcknowledgementCannotPoisonAttemptOrReplaceAcceptedReceipt`.

| Executed unit evidence | Passed | Failed | Skipped/errors/not-run |
| --- | ---: | ---: | ---: |
| Focused six-seam class | 15 | 0 | 0 |
| Contracts assembly | 618 | 0 | 0 |
| Domain assembly | 185 | 0 | 0 |
| Server assembly | 698 | 1 | 0 |
| Client assembly, independently executed | 39 | 0 | 0 |

The full Local command exits **1**. Its exact failing command is:

```bash
dotnet /home/administrator/projects/hexalith/conversations/.artifacts/ext-conv-ai-1-tests/Hexalith.Conversations.Server.Tests/debug/run/Hexalith.Conversations.Server.Tests.dll -result-xml /tmp/hexalith-agents54-conversations-artifacts/run-20261007T1500191806094Z/Hexalith.Conversations.Server.Tests.xml
```

`ConversationsDomainServiceHostCompositionTest.CanonicalDomainEndpointShouldResolve(route: "/")` fails because the current sibling EventStore SDK maps `/process`, `/replay-state`, `/query` and the other SDK routes but omits `/`. The required root endpoint expectation and sibling source are unchanged. This is an SDK compatibility blocker; the Local verifier does not report overall success.

After that failure, the Client assembly was independently staged from the exact fresh built bytes under the ignored repository artifacts directory and executed with XML output; matching DLL hashes prove staging did not alter it. Full assembly results were captured after the acknowledgement fix. The additional membership assertions were subsequently built with the same Debug/source properties and the entire exact six-seam class reran successfully into `final-six-seam.xml`; the broader assemblies were not repeated for those test-only assertions. The final Server build again had zero warnings and zero errors.

The Live negative command exits **1** with `Live gate closed before any calls`; its requested artifact directory was never created. No provider or receiver was invoked. Selected whitespace and CRLF checks pass.

## Remaining complete-record gaps

The current six portable seams and fail-closed local behaviors are implemented. They establish serialized local persisted-event simulation, including source positions, deterministic intent, current content, authorization ordering and manual deletion delivery. They establish no live storage/restart, replica or concurrent-writer qualification.

1. Resolve the SDK root-route compatibility failure before the full Local verifier can pass.
2. Supply real current operation authority and exact immutable Organization Party evidence, authenticated SDK complete-prefix/owning-actor compare-and-append proof, and current authorization at append. The default production ports remain closed.
3. Supply an authenticated complete tenant catalogue and accept the proposed creation-window semantics. Missing catalogue evidence remains Unavailable and never produces an inferred zero or grants unjoined content access.
4. Supply independently governed current deletion approval and real source/policy-bound audit evidence. Membership authority cannot mint that approval.
5. Complete C4 publication discovery, ordered tenant feed/backfill, automatic delivery pump/worker binding, a real exact authenticated Agents receiver and independent remote acknowledgement lookup. The existing source-atomic approval event and manually tested durable delivery transitions do not deliver that complete workflow.
6. Accept the complete immutable owner target, integration date, executable compatibility command and prerequisite/qualification cohort, then obtain the required real storage, loss/restart/outage/rollover and live evidence. The sibling EventStore checkout contains pre-existing uncommitted source edits, so its observed HEAD is not an immutable tested delivery target.

Production payload protection, retention, physical erasure and full owner qualification remain outside these local fixtures. Story 5.4 and dependency availability remain blocked under the original complete record.

## Spec task disposition

| Task | Current disposition |
| --- | --- |
| 1: Six portable/service seams | Portable methods and local behaviors implemented; complete C4 workflow remains partial as listed above. |
| 2: Pure aggregate/state and exhaustive replay | Complete within the approved local scope; current matrix and malformed-receipt replay checks pass. |
| 3: Current scope/immutable Party and independent evidence | Distinct fail-closed ports and local admission verified; real production provider qualification remains gated. |
| 4: Source positions and uncertified snapshot rejection | Complete within the approved local scope; original positions and certified-prefix validation verified. Production actor attestation/compare-append remains gated. |
| 5: Matrix, replay and concurrent intent tests | Complete local evidence; the SDK compatibility failure remains a separate required-check blocker. No live storage claim. |
| 6: Local verifier and full Live gate | Implemented; all named local lanes pass and Live refuses before calls. Overall Local verification remains blocked by the preserved SDK route failure. |
| 7: Separate owner evidence | Complete in this note and its machine evidence. Acceptance and register fields unchanged. |
