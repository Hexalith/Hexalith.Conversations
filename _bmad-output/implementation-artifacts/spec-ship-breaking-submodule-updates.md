---
title: 'Ship breaking submodule updates with green delivery gates'
type: 'chore'
created: '2026-07-14T00:00:00+02:00'
status: 'in-review'
review_loop_iteration: 0
baseline_commit: 'c029b34e1848e6afaf7ac2f5dedd54357229e25c'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Conversations must advance the confirmed FrontComposer, Memories, and Tenants gitlinks in a major-release Conventional Commit, but the exact upstream commits currently have failed Quality, Release, deployment, or optional Aspire checks. Shipping red dependency baselines would make the root commit unverifiable.

**Approach:** Repair deterministic failures in each owning repository through its branch/PR workflow, re-run the authoritative delivery gates, advance the root gitlinks to the resulting green commits, validate Conversations, then push `chore(deps)!: advance platform submodules` with a `BREAKING CHANGE:` footer and inspect the resulting remote state.

## Boundaries & Constraints

**Always:** Preserve the user-confirmed three-submodule scope and unrelated worktree files; make submodule fixes and commits inside their owning repositories; use Conventional Commits, commitlint, typed branches, reviewed PRs, and exact-head check validation; initialize root-declared submodules only; keep failure diagnostics and release evidence support-safe.

**Ask First:** Changing registry credentials/permissions or repository secrets; changing public APIs or production authorization behavior; force-pushing, rewriting published commits/tags, or proceeding with a known-red dependency head.

**Never:** Hide failures with longer timeouts, weakened assertions, `continue-on-error`, skipped gates, warning suppression, or `--no-verify`; expose secrets; recursively initialize nested submodules; hand-edit generated outputs or semantic-release-owned changelogs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| FrontComposer delivery | Windows checkout fails on malformed dependency symlinks; v3.1 pack is blocked by expired v3.0 suppressions | Windows job materializes dependency symlinks safely; package baseline rolls to published 3.0.0 with no stale suppressions | Keep guardrails active and fail packing on real ApiCompat drift |
| Memories deployment | kind surge pod is unschedulable on the one-node verifier | Fault rollout remains schedulable and production replica/strategy state is restored | Capture pod/events evidence and fail on missing health proof |
| Memories release | Zot accepts login but rejects both image pushes | Re-run only after registry permission is corrected; reconcile immutable 2.6.1 artifacts | Halt for user authorization; retain issue #23 and partial-publish evidence |
| Tenants hosted UI | Three network-backed routes repeatedly return HTTP 200 without unauthorized markers | Diagnostics expose the actual rendered state; fix hydration/auth wiring and all three fail-closed states render | Do not accept shell-only markup or raise the retry timeout |
| Root release | Green submodule heads and clean Conversations validation | Exactly three gitlinks plus this spec are committed and pushed as a breaking change | Stop on divergence, unexpected files, or missing remote validation |

</frozen-after-approval>

## Code Map

- `references/Hexalith.FrontComposer/.github/workflows/quality.yml` -- Windows accessibility submodule initialization.
- `references/Hexalith.FrontComposer/Directory.Build.targets`, `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`, `docs/diagnostics/compatibility-suppressions.json`, `eng/pack_release_packages.py` -- v3.1 package-baseline rollover and suppression governance.
- `references/Hexalith.Memories/tools/verify-production-deployment.ps1` -- disposable kind rollout/fault restoration.
- `references/Hexalith.Tenants/tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- failing hosted-route diagnostics and fail-closed assertions.
- `references/Hexalith.FrontComposer`, `references/Hexalith.Memories`, `references/Hexalith.Tenants` -- root gitlinks.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.FrontComposer/.github/workflows/quality.yml`, package-baseline/suppression files in the Code Map, and their focused governance tests -- disable symlink creation only for the Windows dependency checkout; roll validation to published 3.0.0; allow an empty current ledger; keep real ApiCompat drift blocking.
- [x] `references/Hexalith.Memories/tools/verify-production-deployment.ps1`, `tests/tooling/production_deployment_evidence/*`, and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- use a capacity-preserving fault rollout (`maxSurge: 0`, `maxUnavailable: 1`) and restore original strategy/replicas after every fault path.
- [x] `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` and its focused gateway tests -- make anonymous list/detail/audit reads fail closed before any downstream query, matching the existing user/global-admin gateway pattern without changing the hosted assertions or retry timeout.
- [x] `references/Hexalith.FrontComposer`, `references/Hexalith.Memories`, `references/Hexalith.Tenants`, and this spec -- preserve the exact three-gitlink root scope and all unrelated worktree files while preparing the local changes for review.

**Acceptance Criteria:**
- Given the FrontComposer repair, when its changed local gates run, then the pack-plan fixtures, diagnostic registry, package boundary, and changed CI governance assertions pass without weakening package validation.
- Given the Memories repair, when its parser, evidence fixtures, inventory tests, and disposable-kind verifier run, then the fault rollout remains schedulable and the exact deployment replica/strategy state plus Server/MCP health are restored.
- Given an anonymous Tenants principal, when list, detail, or audit is requested, then the gateway returns its existing unauthorized state without submitting an EventStore query; the package-mode build is warning-free and the focused gateway/UI suite passes.
- Given the parent worktree, when local execution ends, then only the confirmed gitlinks and this spec are prepared for the eventual root commit; unrelated files remain untouched and excluded.

## Spec Change Log

## Design Notes

Upstream failures are release blockers even when the changed range is non-product code or a job is marked non-blocking. The root repository has Actions enabled but no workflow files, no runs, no releases, and no branch protection; therefore its evidence is the documented local Release gate plus exact pushed-head verification, while the submodule repositories supply the live CI/CD proof.

The quick-dev execution phase intentionally stops before commits, pushes, PRs, or remote check reruns. Those delivery actions follow the adversarial review. Tenants' authoritative 152-test Aspire lane requires its repository-owned nested dependency graph and will therefore run in hosted CI; the parent workspace validation uses the documented NuGet dependency mode.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet build references/Hexalith.FrontComposer/Hexalith.FrontComposer.slnx -c Release -m:1` plus packer/governance lanes -- expected: v3.1 plan and package set pass.
- Memories publisher/evidence Python fixtures, CLI inventory class, and CI deployment job -- expected: rollout/evidence succeed.
- Tenants focused `TenantsUiRouteSmokeTests` executable, then non-performance IntegrationTests -- expected: 152/152.
- `dotnet restore Hexalith.Conversations.slnx && dotnet build Hexalith.Conversations.slnx -c Release --no-restore` plus each `tests/**/*.csproj` -- expected: zero failures.
