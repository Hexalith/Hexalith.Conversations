---
title: 'Epic 11 A-3: registered commands and platform compatibility'
type: 'chore'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The frozen minimal-module tests instantiate the aggregate directly, and routine CI does not compile or execute the fixture against the real platform. Broken keyed command registration or public API drift can escape validation.

**Approach:** Add an opt-in external compatibility assertion exercising `CreateConversation` through registered public in-process dispatch, including emitted state and replay behavior. Add a blocking CI lane that checks out only required root-declared dependencies at the committed gitlinks, performs locked restore and Release build of the frozen fixture solution, and requires its six tests plus the registered-command assertion to pass with zero skips. Preserve the frozen fixture, lock files, accepted evidence and publications; retain the no-live-Dapr scope and document toolchain/dependency requirements.

</frozen-after-approval>

## Implementation Notes

- Route facts: no human-only intent gaps, no irreversible action (no push or deployment), bounded test/CI/documentation change with no new runtime API or dependency updates.
- Preserve the unrelated EventStore checkout at `19b673af1f09019ccf06201d17a4c92738f54cdd`; validate in an isolated checkout of the root gitlink `7442485b72bc016cd486f6a2aab527069854987e` using root Builds pin `359fee24aeca4d9eabed4b425d4cfa2520b2adda`. Initialize no nested submodules.
- Use the SDK `CustomAfterMicrosoftCommonTargets` hook, scoped to the exact fixture test project, to include the external test without changing any of the 13 frozen paths or lock files. Keep Contracts and Server as separate assemblies. Use the existing public `DomainServiceRequestRouter.ProcessAsync` and keyed `IAsyncDomainProcessor` registrations; no HTTP server or sidecar starts.
- This explicit source compatibility lane builds the fixture's existing platform project references; normal package-based product CI remains governed by its existing configuration. Scoped historical evidence and numbered final-record gates are not applicable to this maintenance action under `docs/runbooks/current-change-validation.md`.
- Implemented `tests/compatibility/MinimalModule/{RegisteredCommandTests.cs,RegisteredCommand.targets,verify_results.py,README.md}` and a dedicated `minimal-module-compatibility` CI job. Added eight stdlib unittest fault controls for missing/duplicate checks, incorrect outcomes/counters, wrong document shape, extra assemblies and absent/malformed results; CI runs them before the platform lane.
- Isolated validation root: `/tmp/epic11-a3-compatibility-3bh_3cs2`. Locked Release restore/build and native xUnit execution succeeded: exactly seven tests passed, zero failures/skips/errors. All 17 tracked fixture/lock paths match `HEAD` and the isolated checkout byte for byte. The existing MessagePack NU1902 advisory remains visible; no package or lock updates were made.
- Workflow validation: `actionlint .github/workflows/ci.yml` and Bash syntax checks passed. The attempted PyYAML validation could not run (`ModuleNotFoundError: No module named 'yaml'`); repeated with the repository-installed `js-yaml` parser successfully without adding dependencies.
- Live regression controls in the isolated tree: omitting the external import produced six passing tests and a result-guard exit 1; deleting the domain-keyed async processor registration produced one failed registered-command test (exit 1, missing keyed service). Restored the external test bytes and reran locked restore/build and all seven tests with `CI=true`: pass. No fault mutation touched the working repository or frozen fixture source.
- Independent one-shot Blind Hunter completed (same model, context-free); no other review layers are configured. Applied its small changes: second dispatch uses a fresh service scope, all CI tool streams and verification output are captured, and result mismatches identify missing/extra/duplicate names, outcomes and counters. Broader public replay/serialized-state coverage is a separate pre-existing gap recorded in the ledger and disclosed in the compatibility README.

## Review Triage Log

- BH-1 — low, defer: the assertion intentionally applies the emitted event into typed state; public `ReplayAsync` and JSON state rehydration are untested by the original suite and remain beyond A-3's keyed-command requirement. Recorded a separate coverage follow-up; no public replay or persistence proof is claimed.
- BH-2 — low, patch: the second command reused a scope, so it did not represent a later DI request lifetime. It now dispatches through a new scope while supplying replayed state.
- BH-3 — medium, patch: pipelines captured stdout only and did not save verifier output, leaving downloaded artifacts incomplete on stderr failures. All four commands now capture both streams and the verifier has its own artifact log.
- BH-4 — low, patch: a generic result error made missing discovery and bad counters harder to distinguish. The error now includes document/assembly shape, missing/extra/duplicate names, unsuccessful outcomes and counters; fault controls assert those diagnostics.
- BH-5 — false as a premature completion defect: the workflow requires `in-progress` until review and verification finish. Finalization will update the spec/action and append a scoped resolution after passing checks, without rewriting earlier historical deferrals.

- Final post-review verification passed: locked Release restore/build with `CI=true`, 7/7 native compatibility tests, 8/8 result-guard fault tests, and 330/330 existing fixture/current-workflow checks (`uv run --frozen --no-sync python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_minimal_module_fixture.py tests/tooling/test_evidence_boundary_current.py --junitxml=/tmp/epic11-a3-focused.xml`). Zero failures/errors/skips. Existing pytest/JUnit property warnings do not alter the result. `actionlint` and Bash/YAML parsing pass after review.
- Exact candidate commit message validated successfully with `node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/epic11-a3-commit-message.txt --verbose`; evidence: `/tmp/epic11-a3-commitlint.log` (0 problems, 0 warnings).
- Action 47 is complete; appended a resolution superseding only the existing registered-command and fixture-CI deferrals. Historical publications were not regenerated. The additional BH-1 replay/rehydration follow-up and A-4 remain open. GitHub execution will occur when these local changes are pushed; no remote run or deployment is claimed.
