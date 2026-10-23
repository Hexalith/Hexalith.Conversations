---
title: 'Close five deferred metadata, recovery and replay gaps'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five still-open items in `deferred-work.md`: validate the final native tenant sample timestamp before metadata disclosure (BH-F09); repair missing or corrupt native owner selections when completed metadata dispatches are redelivered (EC-F01); prove explicit null-label reclassification through replay, persisted completeness and a successful query (VG-R01); reject explicitly supplied default or pre-2000 matching membership timestamps before no-op recognition; and exercise public registered replay plus serialized-state rehydration in the external minimal-module compatibility assertion.

Preserve current public contracts, valid exact retries after later events, supported timestamp offsets, source identities and immutable generation times. Recovery remains producer-owned through the existing EventStore write policy and batch protocol; queries perform no writes. Preserve the frozen minimal-module fixture, locks and accepted historical evidence. Record the five resolutions additively. The user requested five deferred items and authorized committing and pushing the completed changes on main. Apply routine current-change validation; historical evidence reproduction is not applicable.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `0f179a21ad30995eb8ce350c39127926b5c35c25`; main and origin/main agree and the tree is clean after the user's push. Repository baseline, state/Git instructions, build configuration, attributes, architecture runtime decisions and current-change/evidence runbooks were read. No applicable `.editorconfig` exists in this checkout or its parent directories.
- Planning found localized guards and regression gaps with no migrations, new external API or unresolved user intent. Use the one-shot route. The already-covered successful native authorization/query item is excluded from these five; default fixture composition and its current positive test already cover it.
- `Queries/ConversationResolutionMetadataAuthorization.cs` already owns an injectable clock. Reuse that clock for future-sample validation at disclosure, including final samples after resource I/O. `GetConversationResolutionMetadataDomainQueryHandler.cs` must withhold metadata for future-dated native evidence. Add final-sample fault controls and a valid advancing-clock control in the existing query test seam.
- `Projections/ConversationAsyncProjectionHandler.cs::GenerationIsDurableAsync` currently checks proof, fence, metadata, detail, index and ledger but omits owner selection. Require matching native selection and a stable ETag for native generations; use the existing native owner-proof seam where available to detect receipt/batch identity corruption. Returning false must enter the existing `ConversationResolutionMetadataPersistence.WriteMetadataAsync` repair path. Preserve legacy generations that cannot produce native selection and accepted rebuild owner receipts. Test persisted end state after deletion/corruption and retry, plus failure/repair convergence with the existing resolution fixtures.
- Parameterize `ConversationResolutionScoringLabelProjectionTest.ExplicitNativeMetadataReclassificationAdmitsALegacyLabelWithoutRewritingHistory` for null/non-null labels. Assert owner classification and purpose, complete persisted metadata, unchanged original event bytes, native proof, and successful result even with null safeLabel.
- `Aggregates/ConversationAggregate.Agents.cs::Handle(AddAgentParticipant)` must validate the occurrence year before membership no-op recognition while keeping monotonic-time validation after exact retry recognition. Add default/pre-2000 controls before append and after persisted membership; retain existing offset and late-retry controls.
- `tests/compatibility/MinimalModule/RegisteredCommandTests.cs` can call public `DomainServiceRequestRouter.ReplayAsync`, assert successful sequence/state output, then give detached serialized state to `ProcessAsync` in a fresh scope. Preserve the existing test identity and exact seven-test result guard. Update the adjacent README's coverage description without altering any frozen fixture path.
- Verify Debug Server.Tests and domain tests individually, using native xUnit assembly invocation with `-automated sync -failSkips -parallel none`; run the external compatibility suite through its existing opt-in targets import and exact result guard. Keep compatibility platform builds separate from product builds because they share dependency output directories. Run both Story 7.3 workflow checks and staged whitespace checks. Validate the exact commit message with the pinned commitlint CLI before committing; fetch and inspect divergence before the authorized main push, then inspect CI.
- Evidence-boundary and contracted generated-final-record gates: not-applicable to this routine unnumbered batch. No accepted receipts are regenerated, and no submodule or dependency updates are planned.

- Runtime baseline: isolated Aspire start succeeded; resource inspection showed security Running/Unhealthy and Conversations/EventStore waiting, with no telemetry entries. The session-started AppHost was stopped successfully. The initial stop command included unsupported `--format Json` (exit 1); retrying the exact targeted stop without that flag succeeded. No other running AppHost was stopped. This batch makes no live-topology acceptance claim.
- External compatibility locked Debug restore/build succeeded, followed by all seven native tests and its exact result verifier. The build retained its existing NU1902 warning for pinned MessagePack 2.5.302; no packages or frozen locks changed. Eight compatibility-result guard tests and both Story 7.3 workflow checks passed. Domain Debug build passed with zero warnings/errors and all 189 native tests passed with zero failures, skips, not-run cases or errors. Logs/XML are under `/tmp/conversations-five-deferred-*`.

- Initial full Server.Tests Debug build passed with zero warnings/errors; all 1,318 tests passed with zero failures, skips, not-run cases or errors. Combined native coverage at this point is 1,514 passing tests (server, domain and compatibility).
- Remote main advanced during verification to `b3962b007804aa47f5130417cd22da8436f79185`, with advisory authorization and null-label-absence changes overlapping the query fixture. Preserve that commit by rebasing the owned commit and retesting the combined tree before push.

## Review Triage Log

- R1 — medium, defer: Unreadable raw JSON predates this change and is not equivalent to a corrupt typed selection. SDK `ReadModelBatchEnvelope.IsEnvelope` treats malformed bytes as envelope-like; pending recovery refuses them before the typed durability check. The repair writer uses the same guarded store. A safe general recovery capability belongs in the SDK; deleting unknown envelope bytes would discard possible interrupted-batch evidence. This batch repairs missing/semantically corrupt rows and retains fail-closed behavior for unreadable storage. Recorded a separate follow-up.
- R2 — medium, patch: Initial interrupted repair coverage reused the application. Add a restart with only persisted backend and original public input before reconciliation.
- R3 — medium, patch: Add a deterministic selected-owner ETag interleaving to protect the confirming-read guard.
- R4 — medium, patch: Add damaged rebuild-origin selection redelivery coverage, preserving the original generation and proving replacement owner receipt.
- R5 — medium, patch: Add missing/corrupt actual receipt controls with unchanged selection, independently exercising SDK proof failure.
- R6 — medium, patch: Assert complete authorized metadata queries before corruption, while unavailable, and after repair, alongside persisted contents and owner proof.
- R7 — low, patch: The no-op observes rehydrated Created only; rereading immutable input JSON cannot prove Title hydration. Removed redundant post-command input assertions and documented exactly which boundaries observe Title and Created.
- R8 — low, patch: Capture an independent creation payload byte array before projection, then compare both original and subsequent replay input against it to avoid an aliased preservation baseline.

- Review patches add 19 native selection-recovery cases in total, with fresh application restart after interrupted repair, deterministic ETag-only interleaving, rebuild-origin selection recovery, actual receipt loss/corruption, and full metadata query assertions before/during/after recovery. Compatibility final rebuild and all seven exact tests passed after narrowing the hydration claim.

- All 89 focused post-review server cases passed, including 19 native recovery cases, with zero failures/skips/not-run/errors. Review R2–R8 are patched; R1 is retained as a separate pre-existing platform storage-corruption follow-up. The exact full candidate commit message passed the pinned commitlint CLI with zero problems/warnings; evidence is retained at `/tmp/conversations-five-deferred-commitlint.log`.

- Rebase resolution preserves both native fixture controls: the upstream timestamp offset and this batch’s per-sample override. The null-label reclassification test now accepts that current main already treats absent labels as complete; it still replays an explicit null classification and verifies sequence/source proof, classification/purpose, preserved bytes and successful query. No upstream production behavior was reverted. The combined tree is being revalidated before final completion.


## Final Verification

- The rebased code at `696288ed6fdf0140cc2809b93f99c40c38ddfdad` built in Debug with zero warnings/errors and passed all 1,350 Server.Tests, zero failures/skips/not-run/errors. Together with 189 domain tests and seven exact compatibility tests, 1,546 native tests passed. Eight compatibility-result guard tests also passed. The final amendment changes only this spec's status and verification notes.
- Final native command: `dotnet tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0/Hexalith.Conversations.Server.Tests.dll -automated sync -failSkips -parallel none -result-xml /tmp/conversations-five-deferred-server-final-results.xml`. Build command: `dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --configuration Debug --nologo -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`. Final logs and XML retain the same `/tmp/conversations-five-deferred-*` prefix.
- Both final Story 7.3 workflow checks passed with no blockers; whitespace checks passed. All ten non-overlapping upstream changed files were verified byte-identical to the fetched upstream commit, and the overlapping fixture merge retains both timestamp controls. The null absence behavior from upstream is preserved. No frozen fixture, accepted evidence, dependency version or gitlink changed.
- Review R2–R8 are patched and verified. R1 remains a separately recorded pre-existing SDK recovery boundary for unreadable raw storage bytes; typed selection loss/corruption, actual receipt loss/corruption, restart and interleaving recovery are covered. Historical evidence-boundary and contracted generated-final-record gates remain explicitly not-applicable to this routine unnumbered batch.
