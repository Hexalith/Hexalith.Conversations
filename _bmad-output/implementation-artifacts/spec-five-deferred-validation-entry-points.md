---
title: 'Close five deferred validation findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Make current validation runnable from a fresh checkout and clarify which historical observations can be reproduced. Restore the local npm test entry point after the machine-specific delivery packet test was retired. Verify the Epic 16 single-parent candidate rule and SDK source stamp in executable regressions. Explain how to locate the retained transcript evidence. Exercise the AppHost provenance check with matching and incomplete producer configurations.

</frozen-after-approval>

## Implementation Notes

- `package.json` selects the two maintained product unittest suites used by the current after-unit-tests action. The runbook explains their prerequisites and the archived delivery packet boundary.
- `_bmad/scripts/tests/test_epic16_tooling.py` exercises a real Git merge and compiled assemblies with the SDK informational source stamp and optional revision metadata.
- `docs/runbooks/current-change-validation.md` distinguishes retained transcript paths from capture-machine paths.
- `ConversationsAppHostRuntimeBoundaryTest` exercises the producer configuration rule through a pure check shared with both provenance paths.


## Review Triage Log

- Medium, fixed. The native regression now compiles the informational stamp from the SDK's `SourceRevisionId` property.
- Medium, fixed. The transcript guidance names the source inventories, command log and artifact digest list and accounts for receipts with a null candidate.
- Medium, fixed. Both fixture commits go through the test harness's pinned commitlint validation, including the commit that records the merge.
- Low, fixed. The runbook states the actual npm prerequisites. The ledger describes the AppHost guard without attributing it to an earlier implementation.
- Low, fixed. The verification section lists the executed checks and the local environment limits.

## Verification

- `npm test` passed all 12 tests with the pinned SDK, both with and without the producer mapping variables.
- `uv run --frozen pytest -q _bmad/scripts/tests/test_epic16_tooling.py -k 'candidate_requires_exactly_one_git_parent or optional_source_revision_metadata or sdk_informational_version_forms or stale_dirty_and_missing_history'` passed eight cases under SDK 11.0.100 and runtime 10.0.15 using the pinned Builds commitlint tools.
- `dotnet build Hexalith.Conversations.slnx --configuration Release --nologo -m:1` passed with zero warnings. The Release AppHost test assembly passed eight cases selected by `-method '*GatewayProvenanceShould*'`, including the owning-project metadata probe.
- All 42 distinct repository-relative transcript links in the Epic 16 prerequisite spec resolve. The Story 5.4 review did not identify the two other inputs it suspected were missing.
- `python3 scripts/check-root-submodules.py --repository .`, `git diff --check` and the pinned Builds commitlint CLI passed. Scoped evidence publication is not applicable.
- The standalone Aspire startup stopped when it tried to create its CLI state under the read-only home directory. The shared Builds checker could not acquire `delivered/Hexalith.Conversations` anonymously on the clean baseline. The direct GitHub connection also failed outside the command sandbox.
