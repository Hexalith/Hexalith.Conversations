---
title: 'Publish Conversations NuGet packages'
type: 'chore'
created: '2026-09-18'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'af3bfe369c40b634b7c7f45b3d05b079a47a4aa8'
context:
  - 'references/Hexalith.Builds/.github/workflows/ci-cd-standards.md'
  - 'docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** None of the four packable Conversations packages is published on NuGet, and the repository has no CI or Release workflow. Its current Release build also mixes source and package references, producing a `Hexalith.Commons.Serialization` 1.0.0/2.30.0 assembly conflict and a candidate dependency on the nonexistent `Hexalith.Commons.Http` 1.0.0.

**Approach:** Adopt the shared Hexalith CI/CD method used by Tenants and EventStore: package-reference Release builds, an exact four-package manifest with local validation, shared CI, an operator-dispatched release pinned to Builds commit `b93e9889e9e7b67036837015b4b2b115e326c4da`, and explicit post-publication verification. Publish the first semantic release only after its exact `main` SHA passes the new CI.

## Boundaries & Constraints

**Always:** Preserve Debug source-reference development; use NuGet packages for external Hexalith dependencies in Release; enforce exactly `Hexalith.Conversations`, `.Contracts`, `.Client`, and `.Testing`; keep NuGet audit enabled; pin the release reusable workflow and `builds-execution-sha` to the same reviewed SHA; require live-current-`main`, exact-SHA successful push CI, destination absence, and a semantic plan of `1.0.0`; set repository freeze variable `HEXALITH_RELEASE_PUBLISH_ENABLED=true` only for the release window and restore it to `false` afterward; configure `production` as main-only without reviewers for this explicitly authorized bypass; use `require-publication-authority: false` with all reservation inputs empty. The user's bypass instruction explicitly overrides the tracked historical `releaseAllowed=false` hold for this package publication, but does not rewrite or represent that evidence as satisfied.

**Never:** Publish containers, source dependencies, duplicate/unknown packages, or an occupied version; bypass commitlint, exact-source CI, source freshness, package validation, or destination-absence checks; use `secrets: inherit`; edit signed/historical release evidence or the unrelated planning-authority workflow to make this release look approved; retry blindly after any partial publication.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| First release | Four IDs absent; exact green `main`; plan is 1.0.0 | Four `.nupkg` plus symbols are published, tag/release `v1.0.0` targets the dispatched SHA | Verify GitHub assets and poll NuGet until all four exact versions appear |
| Frozen or stale | Freeze is not exactly `true`, source moved, or exact CI proof is absent | No publication side effect | Green skip only when frozen; otherwise fail before release credentials/publication |
| Invalid boundary | Inventory/count/dependencies/version differ or any destination exists | No tag or package write | Fail closed and report the mismatched package/version |
| Partial publication | A write succeeds before a later write fails | Stop and preserve evidence | Do not rerun; reconcile immutable NuGet state before recovery |

</frozen-after-approval>

## Code Map

- `Directory.Build.props`, `Hexalith.Conversations.slnx`, `src/**/*.csproj`, `tests/**/*.csproj` -- Debug/source versus Release/package dependency boundary and audit policy.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml` -- shared CI and operator-controlled exact-source release.
- `.github/workflows/codeql.yml`, `.github/workflows/dependency-review.yml`, `.github/dependabot.yml` -- standard security automation.
- `tools/release-packages.json`, `scripts/` -- authoritative inventory, pack/validate/publish verification, and source/release checks.
- `.releaserc.json`, `package.json`, `package-lock.json` -- Conventional Commit release planning and GitHub/NuGet orchestration.
- `tests/tooling/` -- fail-closed tests for manifests, archives, source proof, and NuGet/GitHub verification.

## Tasks & Acceptance

**Execution:**
- [ ] Build a clean Release/package-reference graph while retaining Debug source mode; add all required central package entries and remove external projects from the solution build surface.
- [ ] Add the four-package manifest, deterministic pack/validation/consumer checks, semantic-release tooling, and tooling tests.
- [ ] Add shared CI, security workflows, and the pinned manual Release workflow with supported authorization bypass and post-publication assertions.
- [ ] Run local workflow, npm, Release build, unit/conformance, pack, archive, and tooling validations; validate the exact Conventional Commit message with the pinned commitlint CLI.
- [ ] Commit and push the implementation, wait for exact-source CI success, create the main-only unreviewed `production` environment, temporarily unfreeze publication, dispatch Release, monitor it to completion, verify all four NuGet packages plus `v1.0.0`, then refreeze.

**Acceptance Criteria:**
- Given Release package mode, when the solution builds and packages are inspected, then no external Hexalith project assembly is mixed with its NuGet equivalent and all dependency versions resolve to the Builds catalog.
- Given the exact current `main` SHA has successful CI, when Release is dispatched with the approved bypass posture, then it publishes exactly four version-1.0.0 package and symbol pairs and creates a matching GitHub tag/release at that SHA.
- Given Release completes, when official NuGet endpoints and GitHub are queried, then all four exact versions/assets are present and the repository publication variable is `false` again.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `actionlint .github/workflows/*.yml` -- all workflow syntax and expressions pass.
- `npm ci --ignore-scripts && npm audit signatures && npm test` -- release toolchain and tooling tests pass.
- `dotnet restore Hexalith.Conversations.slnx -p:UseHexalithProjectReferences=false && dotnet build Hexalith.Conversations.slnx -c Release --no-restore -warnaserror -p:UseHexalithProjectReferences=false` -- package-mode solution succeeds.
- `python3 scripts/pack-release-packages.py ./nupkgs 1.0.0 && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` -- exact package boundary and consumers pass.
- `dotnet test <each configured CI test project> -c Release --no-build` -- every blocking shard passes.
- `gh run watch <release-run-id> --exit-status` plus official NuGet flat-container checks -- release succeeds and all four 1.0.0 versions exist.
