---
title: 'Close five deferred verification follow-ups'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Make the Epic 16 verifier and its operating instructions reflect the inputs actually checked. Detect replaced prerequisite results on every scenario dependency, pin the retained test binary reread with regression coverage, preserve bounded helper subprocess diagnostics, and report unavailable generator dependencies even for invocations without a contract argument. Describe the current CI, default pytest selection, and focused local workflow separately.

</frozen-after-approval>

## Implementation Notes

- Scenario dependencies still require successful evaluation. The caller also rereads each native prerequisite and compares its path and digest to the successful evaluation. Reconciliation uses those same bytes for its pending-identity comparison.
- The retained inventory regression replaces a test DLL after successful admission and exercises the existing digest and link checks alongside the compiled-baseline cases.
- Helper build errors identify the SDK and preserve separate bounded output and error tails for both process exits and timeouts.
- The generator emits a structured blocker for missing declared dependencies, including scope-only invocations and version-specific consumers imported after parsing.
- The runbook describes the shared CI and gives the explicit local pytest command for focused BMad tooling checks.

The first four findings came from the [Story 5.4 installed-delivery review](spec-5-4-ext-conv-ai-1-installed-delivery.md). The [Epic 16 prerequisite implementation review](spec-epic-16-prerequisite-implementation.md) recorded the runbook discrepancy as R9B1.

## Review Triage Log

- Medium, fixed. The generator uses schema-valid diagnostics for missing dependencies. The tests distinguish an absent declared dependency from an undeclared internal import error.
- Medium, fixed. The helper bounds the escaped diagnostic tails. Timeout output is captured for SDK and build prerequisites, while execution timeouts keep their fixed message.
- Low, fixed. Capture regressions assert each specific blocker and artifact path. The five dependency-edge regressions exercise tooling-fixture receipts.
- Low, fixed. The runbook gives a runnable pytest example and identifies the root npm script as a local command.

## Verification

- `pytest _bmad/scripts/tests/test_epic16_tooling.py` with the focused helper, dependency, capture, and CLI selectors passed 42 cases under SDK 11.0.100 and runtime 10.0.15. The isolated fault cases assert `SCENARIO_DEPENDENCY_DRIFT`, `NATIVE_CAPTURE_DRIFT`, `NATIVE_CAPTURE_ALIAS`, `PATH_ESCAPE`, and schema-valid `SCHEMA_UNAVAILABLE`.
- The `selected_lazy_imports` selection in `test_epic16_tooling_v2.py` and `test_epic16_tooling_v3.py` passed all eight cases.
- The two unittest discovery commands from the Conversations after-unit-tests action passed 12 cases, and the root-submodule check passed.
- The broader default pytest run also selected two portable-surface tests that require restored build assets. The legacy root npm test list refers to modules that are absent in the current tree. The CI hook uses the two verified unittest commands directly.
- The pinned Builds commitlint CLI accepted the exact commit message. Scoped evidence publication is not applicable to this routine change.
