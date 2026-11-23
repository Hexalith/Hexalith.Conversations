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

Make repository validation report only outcomes supported by the run that produced them. Close five deferred findings by removing success measurements from failed Epic 16 receipt publication, requiring explicit core native counters, testing the reconciliation checkpoint boundary, making the v3 held-runtime check independent of live source drift, and bounding the Contracts package inventory commands and their cancellation cleanup.

</frozen-after-approval>

## Implementation Notes

- Failed receipt publication keeps the candidate and assertion diagnostics. It clears test counts, bindings and runtime qualification.
- Native TRX checks require the five core counters. Reconciliation rejects a checkpoint with no preceding or recovery position.
- The v3 held-runtime tests select the original contract path in a disposable repository. The current-change runbook records the published v3 binding as historical. It reports `SOURCE_DRIFT` against live `main`.
- The Release Contracts inventory bounds both `dotnet` commands at three minutes with a separate five-second stop allowance. It disables reusable build processes. Source-mode packing continues to use the Builds runner.

## Verification

- The Python regressions use the repository's locked Python environment, the .NET 11 SDK and a .NET 10 runtime for the native helper. Install the Builds guard npm dependencies from `references/Hexalith.Builds/Github/guard/package-lock.json` in a scratch directory with `npm ci --ignore-scripts`. Point `HEXALITH_GUARD_TOOLS` to that directory before running the fixture tests.
- The full tooling-v3 suite and focused Epic 16 regressions passed 135 cases. A native xUnit TRX confirmed the five required counter fields.
- The mapped Release build and all 638 Contracts tests passed. A Linux probe cancelled a stalled `dotnet msbuild` command and observed its shell and child stop.

## Review Triage Log

- Medium, patched. Added a true runtime-qualification reset control and an exact recovery-index boundary failure.
- Medium, patched. Marked the drifting v3 source binding historical and retained the existing drift guard.
- Low, patched. The fixture reports how to install the pinned Builds validator when the CLI is missing.
- Unverified outside the supported SDK commands. A child whose parent exits before cancellation may require process ownership beyond the SDK's tree-kill API. The inventory disables reusable build processes and preserves cleanup failures on the original exception. The Linux probe verified cancellation while the command was active.
