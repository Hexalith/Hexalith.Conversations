---
title: 'Restore current Client public API coverage'
type: 'chore'
created: '2026-10-06'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 8 retrospective A-1 identifies a missing current Client export inventory after CI retired the composite rc.2 conformance fact. The committed Agents changes add public methods, so a type-name-only check would miss relevant API changes.

**Approach:** Capture the current Client assembly's complete exported type inventory and declared public CLR member signatures in a deterministic reviewed baseline. Compare the built Client with that baseline in the existing Client unit-test CI lane, retain the conformance exclusion guard, and prove an added export and an added method fail before exact restoration passes. Complete A-1 only after review and focused checks pass. A-2 remains open because its first applicable successor implementations and outputs do not exist yet; this work does not substitute synthetic compatibility claims for those outputs.

</frozen-after-approval>

## Implementation Notes

- Planning facts: no unresolved intent choice; no irreversible operation; small test/evidence change using the existing Client unit-test CI lane. No commit, push, dependency update, production source change, or historical record generation is required.
- Existing edits were authorized for preservation. On resumption the workspace was clean because another session committed them; the observed Agents commit is `ca9ff63849f47515624022c9781f78eefb28c4dd`. Review the current Client declarations and capture the full canonical source revision at baseline creation, accounting for concurrent commits.
- Code map: `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs` supplies the assembly marker and current weak boundary checks; `PublicContractShapeSnapshotGenerationTest.cs` provides the deterministic reflection/comparison pattern but its private helpers and immutable Contracts baseline remain untouched. All Client exports are included without a namespace filter.
- Planned files: a single-type Client snapshot test, `docs/release-evidence/client-public-api-baseline-v1.json` and its maintenance note, the existing release-tooling CI guard to pin Client unit-lane enrollment, and the exact A-1 status entry in `sprint-status.yaml` after successful review. Preserve the retrospective report as historical analysis.
- Validation: build and run the Client test project in Debug with local project references; run the existing release-tooling CI/exclusion check; exercise temporary source mutations in an isolated copy and verify baseline hashes remain unchanged. Run all Client facts after restoration.
- A-2 investigation: contracts 9.1/9.2 require missing conformance-tiering generators and tests; 11.2 requires a missing minimal-module fixture and proof generators; 13.1 requires missing history/chain generators. Corresponding sprint stories remain backlog. Leave item 39 and all six compatibility deferrals unchanged until real gate output can settle them.
- Implemented the single-type `PublicClientApiSnapshotTest` in the existing Client test project. Its observed output stays under ignored build output; ordinary execution only reads the reviewed baseline. The final JSON captures seven exported types and all 83 declared public members, including both copies of the eight Agents methods. Its source declarations were checked byte-for-byte against `e6611b2c6a2f5e3270122bd3515d43f7645be9c4` before capture. Baseline SHA-256: `982579a6300a2c5e9106974f9f66a558118a7d29b043cf688abd3aae8bfe8dc1`.
- Added the maintenance note and pinned exact enrollment of `tests/Hexalith.Conversations.Client.Tests` inside the CI unit-project block. No CI exclusion, historical Contracts baseline, accepted story record, production source, dependency, or gitlink was changed by this task.
- Both focused builds passed with zero warnings/errors: `dotnet build tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --configuration Debug -m:1 -p:UseSharedCompilation=false -nr:false` and the same command with `--configuration Release`. Both full Client assemblies passed all 35 facts, zero errors/failures/skips/not-run: `dotnet tests/Hexalith.Conversations.Client.Tests/bin/{Debug,Release}/net10.0/Hexalith.Conversations.Client.Tests.dll -noLogo -failSkips -result-trx /tmp/epic-8-client-tests{,-release}.trx` (two separate invocations).
- Focused release-tooling check passed: `python3 -m pytest -q tests/tooling/test_release_tooling.py -k ci_and_security_automation_cover_release_boundary` (1 passed, 21 deselected). A separate isolated workflow fixture renamed the Client project to `.Client.Tests.Archive`; the actual check rejected it and passed after exact restoration. Fixture: `/tmp/epic8-client-ci-8xv6lq2a`.
- Final real-source mutation evidence is `/tmp/epic8-client-api-r07le7ot/final-probe-results.json`, with the exact commands and logs beside it. An isolated copy ran the actual snapshot class using a test project compiling only that class, so compiler-visible API mutations could reach the comparison without unrelated consumer compilation failing first. Added export in an unexpected namespace, added method, null-to-empty constructor default, removed extension annotation, init-to-set property change, and added error-level Obsolete metadata each built successfully, made the comparison exit 1, and passed with exit 0 after exact source restoration. The final baseline hash remained unchanged through all six probes.
- The first isolated build command, `dotnet build tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --configuration Debug -m:1 -p:UseSharedCompilation=false -nr:false`, failed with CS0006 for a symlink-relative Commons reference assembly. Re-running in that copy with `-p:HexalithCommonsRoot=/home/administrator/projects/hexalith/conversations/references/Hexalith.Commons` and `-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/conversations/references/Hexalith.EventStore` resolved the environment issue; all final mutation builds passed. No submodule initialization or source mutation in the real workspace was needed.
- The configured Blind Hunter review reported seven findings. Six code corrections and a clarification about local evidence are recorded below. The reviewer verified the fixes and reported no new concrete defect. The source revision remains a snapshot binding, not approval of the Agents integration's still-incomplete external/runtime acceptance record.
- `python3 scripts/check-root-submodules.py --repository .` passed. The task completes retrospective item 38 only; item 39 and earlier retrospective actions retain their current statuses. Concurrent Epic 9 and `docs/implementation/` work is preserved. All task-owned changes remain uncommitted by this run.
- Final concurrent-work recheck: another session added private Client cancellation handling and four unit facts with two fixtures. The first Debug build returned CS0246 while those fixture files were still being created. Running Debug and Release together also interfered with their shared restore files, and the Release attempt returned missing Commons converter/reference errors. The same focused commands were then run sequentially after both fixtures existed: each build passed with zero warnings/errors, and each full Client assembly passed **39 facts, zero errors/failures/skips/not-run**. Those final TRX files replace the earlier 35-fact outputs at the paths above. The concurrent implementation changes do not alter the exported API, and both newly built snapshots still equal the reviewed baseline.

## Review Triage Log

| Finding | Verdict / disposition | Verified evidence |
| --- | --- | --- |
| Default JSON newlines vary by OS. | Medium / patched. | The pinned .NET 10 reference documentation defines the default as `Environment.NewLine`; explicit `NewLine = "\n"` produces an LF-only baseline. Windows execution was not claimed. |
| `init` and `set` have the same ordinary reflected signature. | Medium / patched. | Capture return/parameter/field modifiers including `IsExternalInit`; the isolated init-to-set mutation now fails and exact restoration passes. |
| Removing `this` leaves an ordinary static signature unchanged. | Medium / patched. | Capture compiler-visible API attributes on types and members; the real extension-removal mutation now fails and restoration passes. |
| Required-member and Obsolete annotations are not ordinary CLR signatures. | Medium / patched. | Capture relevant compiler and code-analysis attributes with their arguments. An added `Obsolete("API probe", error: true)` on a current exported property now fails the comparison and passes after restoration. |
| Null and empty-string defaults collide under string conversion. | Medium / patched. | Defaults use JSON encoding, with missing defaults represented separately. A current nullable constructor default changed from null to empty string fails and restoration passes. |
| Substring CI enrollment accepts a different suffixed project. | Medium / patched. | Compare complete stripped unit-project entries. The actual check rejects the `.Archive` fixture and passes the restored configuration. |
| The observed JSON is not uploaded by shared CI. | Low / documentation corrected. | The maintenance note explicitly describes the JSON as local build output and directs local reproduction; CI retains TRX. A shared Builds-wide artifact uploader is not needed for the current API comparison or its committed baseline. |
