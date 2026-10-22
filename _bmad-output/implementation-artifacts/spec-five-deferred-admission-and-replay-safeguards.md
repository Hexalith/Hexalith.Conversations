---
title: 'Close five deferred admission and replay safeguards'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close five outstanding package-publication review findings from `deferred-work.md`: BH-R2-01 (bind legacy command actors to authenticated Parties), BH-R2-02 (reject legacy mutations after source deletion), BH-R2-03 (bind restricted membership/posting wrapper timestamps to public OperationTimestamp), BH-R2-06 (reject blank message edits during replay), and BH-R2-07 (preserve immutable deletion delivery attempt targets during replay).

Preserve existing public contracts, content-safe refusals, exact command retries and valid deletion quarantine events. Add focused behavioral regression coverage through the existing serialized-source fixture and domain validation tests. Record the five resolutions additively in the ledger. Follow routine current-change validation; historical evidence-boundary reproduction is not applicable. The user authorized pushing completed changes to main and confirmed that the twelve pre-existing staged Epic 16 files must remain untouched and excluded from these commits.

</frozen-after-approval>

## Implementation Notes

- Planning found five localized missing guards with no public API additions, migrations or new authority decisions. BH-R2-04 and BH-R2-05 remain separate work.
- Legacy admission currently accepts general commands before examining payload metadata. Recognize the six implemented legacy wrappers and bind their tenant, conversation and actor to the envelope and current human authority; reject malformed or unsupported aliases safely.
- Five legacy validators use only the Open lifecycle gate; include IsDeleted without changing lifecycle meaning.
- Validate portable timestamp presence/UTC/equality before restricted membership and posting no-op branches; retain monotonic occurrence checks after exact retry recognition.
- Validate retained edit text before mutating replay state. Refuse delivery target changes for ordinary attempt/acknowledgement events while preserving command-generated quarantine and the original target mapping.
- Reuse ConversationAgentLocalFixture and focused domain test patterns. Keep production EventStore persistence, accepted evidence, submodule pointers and pre-existing staged files unchanged.
- Baseline: 60f18214d7a4565d28096628af95d867ddd5e874. Local Aspire started successfully before implementation; runtime inspection and focused tests follow.

- Implemented eight production files, updated the shared membership fixture and added two focused regression classes. Legacy command recognition now uses the same alias normalization as restricted commands and safely refuses unknown conversation mutations.
- Validation: Debug builds of Server.Tests, domain Tests and Client.Tests passed with zero warnings/errors. Direct xUnit runs with `-automated sync -failSkips -parallel none -result-xml <receipt>` passed: Server.Tests 1278/1278 (including 127 admission/timestamp and 16 deletion/replay regressions), domain Tests 189/189, Client.Tests 39/39; zero failures, skips, not-run cases or errors. Logs and raw XML are retained under `/tmp/conversations-five-deferred-*` for this run.
- Workflow checks: both exact Story 7.3 `verify_story_completion_workflows.py` commands passed (AC-7.3-01 and AC-7.3-02). Output must use the contract's exact repository-relative receipt paths; initial attempts with other output locations were rejected as ARGUMENT_INVALID before evaluation, then corrected. `git diff --check` passed.
- The exact build command was `dotnet build tests/Hexalith.Conversations.<project>/Hexalith.Conversations.<project>.csproj --configuration Debug --nologo -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` (the incremental final Server.Tests build also used `--no-restore`). Test commands invoked each resulting `bin/Debug/net10.0/*.dll` separately. Builds used local source references; dependency versions were unchanged.
- Runtime baseline: Aspire start succeeded, Conversations and its sidecar were healthy; a later resource inspection showed EventStore had exited. These changes are verified by deterministic in-process tests and make no live end-to-end claim. The session-started AppHost was stopped successfully after inspection.
- Evidence-boundary and contracted generated-final-record gates: not-applicable to this routine, unnumbered fix under the current-change policy. Existing accepted evidence was not regenerated. The twelve pre-existing staged paths retain their entry working-file hashes and index entries.

- Independent review identified an unintended UTC-only restriction in the first timestamp implementation. Removed it: binding compares DateTimeOffset instants and accepts supported nonzero offsets, including equivalent instants represented with different offsets. Eight positive append/retry controls replace the offset-refusal cases. The earlier UTC planning note is superseded by this compatibility correction; the frozen preserve-contract intent remains unchanged.

## Review Triage Log

- R1 — medium, patch: The first HasBoundOccurrence required offset zero, although the public membership/posting contracts and client accept other offsets. Confirmed from both contracts, client forwarding and the guard. Changed to instant equality and added positive offset compatibility regressions.
- R2 — low, defer: Existing membership no-op recognition precedes occurrence-year validation, so matching explicitly supplied default/pre-2000 timestamps can return NoOp for an already-present participant. This branch pre-existed the selected fixes and appends no business event. Recorded as a separate follow-up; it does not prevent the five selected safeguards from working.
- R3 — medium, patch: The new legacy matrix initially exercised admission only. Add SDK router coverage of forged legacy actors, refusal without business events and valid authenticated event attribution through registered processing.
- R4 — medium, patch: The legacy matrix initially used one well-formed human authority response. Add malformed/mismatched authority cases for missing Party/revision, wrong principal/tenant and organization identity to protect fail-closed general admission.
- R5 — medium, patch: Immediate membership retries alone would not detect moving monotonic-time validation ahead of retry recognition. Add an exact membership retry after a later posting event.
- R6 — medium, patch: Historical same-target Attempt coverage alone would not detect incorrectly rejecting a valid older attempt's acknowledgement. Add acknowledgement/replay coverage preserving the original binding, receipt and checkpoint.
- R7 — medium, patch: Pure aggregate deletion tests alone do not protect governance pre-audit ordering. Add handler regression coverage proving deleted governance commands are refused before audit calls.


## Final Verification

- Review R1 and R3–R7 are patched. SDK routing now proves business-event refusal and authenticated attribution; five malformed authority controls pass; late membership retries, older acknowledgements and all three pre-audit handler refusals are covered. R2 remains a separately recorded, pre-existing low-impact follow-up.
- Final Debug Server.Tests build passed with zero warnings/errors. Final native runs passed 1293 server tests, 189 domain tests and 39 client tests (1521 total), with zero failures, skips, not-run cases or errors. The new coverage comprises 137 admission/timestamp cases, 18 replay cases and three governance-handler cases (158 total). Earlier counts above describe intermediate validation before review patches.
- Both Story 7.3 workflow verifications and final whitespace checks passed. The exact full commit message passed the pinned commitlint CLI with zero problems or warnings; validation output is preserved at `/tmp/conversations-five-deferred-commitlint.log`.
- User-authorized publication is limited to these owned changes on main. Existing staged Epic 16 files remain excluded, with their entry bytes and index entries preserved.
