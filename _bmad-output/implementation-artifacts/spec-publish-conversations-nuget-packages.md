---
title: 'Publish Conversations NuGet packages'
type: 'chore'
created: '2026-09-18'
status: 'in-review'
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
- [x] Build a clean Release/package-reference graph while retaining Debug source mode; add all required central package entries and remove external projects from the solution build surface.
- [x] Add the four-package manifest, deterministic pack/validation/consumer checks, semantic-release tooling, and tooling tests.
- [x] Add shared CI, security workflows, and the pinned manual Release workflow with supported authorization bypass and post-publication assertions.
- [x] Run local workflow, npm, Release build, unit/conformance, pack, archive, and tooling validations; validate the exact Conventional Commit message with the pinned commitlint CLI.
- [ ] Commit and push the implementation, wait for exact-source CI success, create the main-only unreviewed `production` environment, temporarily unfreeze publication, dispatch Release, monitor it to completion, verify all four NuGet packages plus `v1.0.0`, then refreeze.

**Acceptance Criteria:**
- Given Release package mode, when the solution builds and packages are inspected, then no external Hexalith project assembly is mixed with its NuGet equivalent and all dependency versions resolve to the Builds catalog.
- Given the exact current `main` SHA has successful CI, when Release is dispatched with the approved bypass posture, then it publishes exactly four version-1.0.0 package and symbol pairs and creates a matching GitHub tag/release at that SHA.
- Given Release completes, when official NuGet endpoints and GitHub are queried, then all four exact versions/assets are present and the repository publication variable is `false` again.

## Implementation Notes

- Release builds now select package references for external Hexalith dependencies while local Debug builds retain source references. The solution no longer builds external submodule projects, and package metadata produces deterministic `.nupkg`/`.snupkg` pairs.
- `tools/release-packages.json` is the closed four-package inventory. The Python validators enforce its exact IDs/order, dependency resolution, archive contents, consumer-only restores, destination absence/presence, exact release assets, source freshness, and partial-publication stop condition.
- CI, CodeQL, dependency review, Dependabot, and the operator-dispatched Release workflow use the shared Builds contracts. Release is pinned to `b93e9889e9e7b67036837015b4b2b115e326c4da`, has no container path, never inherits secrets, and skips the reusable release job unless the freeze variable is exactly `true`.
- The full preservation conformance suite remains red rather than rewriting historical evidence: 470/473 pass, while the fixed rc.2 overlay rejects the current post-baseline path set, its stored build receipt rejects the rebuilt assembly, and the SM-C2 reconstruction rejects the changed package-mode project bytes. Package CI runs the other 470 conformance checks and excludes exactly those three frozen historical assertions.
- The live Dapr integration fixture is also locally blocked because `daprd` exits during its health check. The non-live integration scaffold lane passes after teaching its dependency guard that Release-only package fallbacks are inactive in default Debug/source mode.
- Review hardened the exact-source lane with commit/title linting, newest-run proof, a write-free local Semantic Release plan, final pre-tag freshness validation, explicit package versions, runtime consumer execution, Portable-PDB checks, the Admin.Web and real AppHost boundaries, and post-publication package-content continuity. NuGet.org's repository signature is excluded only from the canonical content comparison; the raw GitHub assets remain bound to their API SHA-256 digests.
- No commit, push, GitHub environment/variable change, workflow dispatch, package publication, tag, or release was performed during implementation.

## Spec Change Log

- 2026-09-19: Implemented the local Release/package graph, exact package tooling, CI/security automation, and pinned manual release path; recorded remaining conformance, Dapr-runtime, and remote-operator gates.

## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | high | patch | `semantic-release` 25 calls `git push --dry-run` even in dry-run mode, while `verify-source` has `contents: read` and checkout credentials disabled; the semantic-plan gate can therefore fail before authorization. |
| BH-02 | medium | patch | A separate `commitlint.yml` exists, but Release proves only `ci.yml`; therefore a green package CI can authorize a commit whose separate commitlint run failed. Put the pinned commit/title policy inside exact-source CI. |
| BH-03 | medium | patch | `Hexalith.Conversations.Admin.Web.Tests` is in the solution and contains current rendering/accessibility tests, but it is absent from the blocking unit-test list. |
| BH-04 | medium | patch | The workflow excludes the whole 473-test conformance project even though only three frozen historical-evidence tests fail; a focused MTP exclusion can retain the other 470 checks without rewriting evidence. |
| BH-05 | medium | patch | The only production AppHost boundary test is skipped unless `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`; the reusable Aspire lane sets no such value and therefore proves only the other eight tests. |
| BH-06 | high | patch | `verify_source()` asks GitHub only for successful runs and accepts `any(...)`, so it cannot observe a later failed attempt for the same SHA. Query all exact-SHA runs and require the newest attempt to succeed. |
| BH-07 | high | patch | Semantic Release pushes its tag before invoking publish plugins, while the current publish preflight rechecks live `main`; a source movement during prepare can therefore strand a tag. Revalidate at the end of prepare and keep publish-phase checks limited to the exact tag and still-absent write destinations. |
| BH-08 | high | patch | `validate_packages()` derives its expected version from candidate archives, so a consistently wrong version passes even though preflight checks `1.0.0`. Require the caller-supplied planned version. |
| BH-09 | medium | patch | Shared CI builds without the `0.0.0-ci-test` version and `pack-release-packages.py` then uses `--no-build`; package metadata can diverge from contained assembly metadata. Build during pack with the candidate version. |
| BH-10 | medium | patch | Foreign DLL rejection scans only `lib/net10.0`; a foreign assembly under another `lib`, `ref`, or `runtimes` path is currently accepted. Extend the archive boundary. |
| BH-11 | medium | patch | Symbol validation checks only that the expected path exists, so an empty/corrupt or extra unrelated PDB passes. Require exactly the expected PDB and Portable-PDB magic. |
| BH-12 | medium | defer | `dotnet nuget push` uploads the adjacent `.snupkg` and fails synchronously on upload rejection, but NuGet symbol validation/indexing is asynchronous and the verifier has no symbol-server retrieval proof. Add a later PDB-signature-based `symbols.nuget.org` consumption probe. |
| BH-13 | high | patch | GitHub asset verification reduces the API objects to a name set; malformed duplicates, pending uploads, zero-byte assets, or wrong bytes are not rejected. Require exact objects, uploaded/nonzero state, and usable digests. |
| BH-14 | high | patch | A NuGet HTTP 200 is presence-only. Download each published `.nupkg` and bind its SHA-256 to the corresponding GitHub release asset digest so the verified publication is the validated release byte stream. |
| BH-15 | medium | patch | Twenty 15-second attempts allow under five minutes, while NuGet documents symbol/package validation can take materially longer. Expand the bounded post-publication window so a successful immutable publication is not prematurely reported failed. |
| BH-16 | false | reject | Freeze mutation is an operator action in the accepted spec, not a workflow responsibility; the default workflow token has no configured repository-variable write authority, and the remote execution procedure has a mandatory `finally` refreeze and verification. |
| BH-17 | medium | patch | `verify-semantic-release-plan.mjs` duplicates branches, tag format, repository, and analyzer instead of deriving the planning subset from `.releaserc.json`; configuration drift can split the gate from publication. |
| BH-18 | false | reject | The Folders and Projects gitlinks were already committed and pushed by the user in `32a0928` before this implementation resumed. They are evidence-boundary warnings, not editable implementation changes, and must be preserved. |
| EC-01 | medium | patch | A transient timeout, 429, or 5xx aborts `verify_present()` on the first response even though the method already has a bounded polling loop. Retry transient publication reads inside that budget. |
| EC-02 | medium | patch | `verify_present()` checks `draft` but not `prerelease`, so a prerelease object can satisfy stable `v1.0.0` verification. |
| EC-03 | medium | patch | Non-string asset names are discarded before set comparison, so an extra malformed asset can escape the exact-inventory check. Require eight well-formed unique asset objects. |
| EC-04 | medium | patch | The generated public executable contains runtime assembly-load assertions but is built with `run_test=False`, so those assertions never execute. Run it after build. |
| EC-05 | low | patch | The local feed path is interpolated raw into XML. Paths with `&`, quotes, or angle brackets are reachable through the CLI and break `NuGet.Config`; XML-escape the attribute. |
| EC-06 | medium | patch | Same verified defect as BH-11: the expected PDB entry can contain arbitrary bytes. The shared symbol-boundary patch must reject it. |
| EC-07 | false | reject | Same claim as BH-16: the accepted release procedure owns the temporary repository-variable mutation externally and verifies the final false value. |
| VG-01 | medium | patch | Pre-verified gap: the public consumer is never run and no tooling regression test proves a nonzero generated executable fails validation. |
| VG-02 | medium | patch | Pre-verified gap: tests call `verify_publishable()` directly but never execute the shell phase router, so a verify/publish state-routing regression remains green. |
| VG-03 | medium | patch | Pre-verified gap: `verify_present()` has only a happy path; missing and unexpected asset rejection has no regression test. |
| VG-04 | medium | patch | Pre-verified gap: XML inspection does not evaluate `UseHexalithProjectReferences`; add focused MSBuild checks for local Debug, local Release, and Debug under CI. |

<!-- STORY-FINAL-RECORD:BEGIN -->

**Final record** — `story-final-record-v1`, result **PASS**, mode `live`. The JSON document is authoritative; this Markdown is rendered from it.

Derived: test results **yes**, candidate **yes**, record section **yes** · 8 test artifact(s) parsed · 43 file-list path(s) · 3 gitlink promotion(s) evaluated.

Baseline `af3bfe369c40b634b7c7f45b3d05b079a47a4aa8` → candidate `c98b37958c6ed49bc82badc701b5a3d4f9b76229`.

### File List

- `.github/dependabot.yml` (new)
- `.github/workflows/ci.yml` (new)
- `.github/workflows/codeql.yml` (new)
- `.github/workflows/dependency-review.yml` (new)
- `.github/workflows/release.yml` (new)
- `.gitignore` (modified)
- `.releaserc.json` (new)
- `Directory.Build.props` (modified)
- `Directory.Packages.props` (modified)
- `Hexalith.Conversations.slnx` (modified)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified)
- `_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore.log` (new)
- `_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-build-remediated.log` (new)
- `_bmad-output/implementation-artifacts/spec-publish-conversations-nuget-packages.md` (new)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `global.json` (modified)
- `package-lock.json` (modified)
- `package.json` (modified)
- `scripts/pack-release-packages.py` (new)
- `scripts/release_contract.py` (new)
- `scripts/release_state.py` (new)
- `scripts/validate-consumer-package-references.py` (new)
- `scripts/validate-nuget-packages.py` (new)
- `scripts/validate-publication-preflight.sh` (new)
- `scripts/validate-release-secrets.sh` (new)
- `scripts/verify-release-source.py` (new)
- `scripts/verify-release-state.py` (new)
- `scripts/verify-semantic-release-plan.mjs` (new)
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` (modified)
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj` (modified)
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` (modified)
- `src/Hexalith.Conversations/Hexalith.Conversations.csproj` (modified)
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs` (modified)
- `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` (modified)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` (modified)
- `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` (modified)
- `tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` (modified)
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` (modified)
- `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` (modified)
- `tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj` (modified)
- `tests/install-playwright.ps1` (modified)
- `tests/tooling/test_release_tooling.py` (new)
- `tools/release-packages.json` (new)

### Gitlink Promotions

| Path | Declared | Recorded mode | Recorded commit | Baseline commit |
| --- | --- | --- | --- | --- |
| `references/Hexalith.EventStore` | yes | `160000` | `2d680d7d08e00baef63f5b2aca98c6ad6fcc178d` | `2d680d7d08e00baef63f5b2aca98c6ad6fcc178d` |
| `references/Hexalith.Folders` | no | `160000` | `e2881d901b3f12e3f9023617fe622c5f11254916` | `5245ed9febd54455f505657dcb478f30698140e5` |
| `references/Hexalith.Projects` | no | `160000` | `64e0f4f63c21a47a254565a5ce9797d71f12e9ad` | `1b154fde25a33b7af6dfcac75b26f1694d9bd3fb` |

### Test Results

| Test project | State | Total | Executed | Passed | Failed | Skipped | Artifact SHA-256 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Hexalith.Conversations.Admin.Web.Tests | PARSED | 14 | 14 | 14 | 0 | 0 | `43c2287c27ded380535da892bf5326762b003530ef0edf1b730e61a103700974` |
| Hexalith.Conversations.AppHost.Tests | PARSED | 9 | 9 | 9 | 0 | 0 | `db3cc99b19940f423a0b9c4bcc4cae2b603b52a53c3b6902d5f73e180913cd3d` |
| Hexalith.Conversations.Client.Tests | PARSED | 29 | 29 | 29 | 0 | 0 | `a40c972776f6fdcebcfbae383d8ec6cc1f79e3e3d5bcd78f5ab8b4a50cebbc52` |
| Hexalith.Conversations.Conformance.Tests | PARSED | 470 | 470 | 470 | 0 | 0 | `092744d39e33a2f09c1ebbf7d4cddaf87d309cd0955e5b3e058c6ab7896ef5b9` |
| Hexalith.Conversations.Contracts.Tests | PARSED | 618 | 618 | 618 | 0 | 0 | `dff506beff3d629f6418d9d02b2c8e1c49ff18d0e3721f37be751e91ddc8560c` |
| Hexalith.Conversations.IntegrationTests | PARSED | 14 | 14 | 14 | 0 | 0 | `066b0193555c71a0d4a7237ec91b3b7d8055ea3bac1bd8177a4ccca9e8fd9369` |
| Hexalith.Conversations.Server.Tests | PARSED | 684 | 684 | 684 | 0 | 0 | `15569e9b3dbe7a9ebc4a1483597b3d480fad8d724dbe62e245ccc1ee84a17e79` |
| Hexalith.Conversations.Tests | PARSED | 185 | 185 | 185 | 0 | 0 | `cc12e4e2c65d17c7c0b1d8c48ed6e8d3fb4ffeaaf83cb14163a021ddf9514a0d` |
| **Total (computed)** | **8 parsed** | **2023** | **2023** | **2023** | **0** | **0** | — |

### Test Build Manifest

Candidate `c98b37958c6ed49bc82badc701b5a3d4f9b76229` was clean-rebuilt; every test binary below embeds that SourceRevisionId.

#### Candidate-Bound Test Binaries

| Test project | Binary | Source revision | Binary SHA-256 |
| --- | --- | --- | --- |
| Hexalith.Conversations.Admin.Web.Tests | `tests/Hexalith.Conversations.Admin.Web.Tests/bin/Release/net10.0/Hexalith.Conversations.Admin.Web.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `e6d4750108a90e89822778a41c24d04382cced4611b7ecb6a9ff8b1543b4125c` |
| Hexalith.Conversations.AppHost.Tests | `tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `165ff1af1dd8b09f9713f2a026343fec8e3b888d5e694050834a909a3ff29733` |
| Hexalith.Conversations.Client.Tests | `tests/Hexalith.Conversations.Client.Tests/bin/Release/net10.0/Hexalith.Conversations.Client.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `ad6f0341f315b1da9c7245ffb03bcbad83f588c893d31b057c9823a3e3824a4d` |
| Hexalith.Conversations.Conformance.Tests | `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `e15052b8c4a633e24b1b16ba6dae245aa39f4e289bb7855a4a9c0fada34cba81` |
| Hexalith.Conversations.Contracts.Tests | `tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `bd4e176ad2c49e04cb97e40cb0107e399187bdb2d8e48ef8fecf217186de54bc` |
| Hexalith.Conversations.IntegrationTests | `tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `6cdec2370f44bfd2079cce7408d9f47abeb154f7d2e896e4c1c9e3dfcd973e6f` |
| Hexalith.Conversations.Server.Tests | `tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `574708ccc76e021dbc1d8a3ca50118ac4a90f41cbc10946a7789c2fba837e185` |
| Hexalith.Conversations.Tests | `tests/Hexalith.Conversations.Tests/bin/Release/net10.0/Hexalith.Conversations.Tests.dll` | `c98b37958c6ed49bc82badc701b5a3d4f9b76229` | `a05025d9f6337888f4f12806151d44b0c7dbba57cdb6f13291aae7585300bd99` |

### Candidate Binding

- Candidate `c98b37958c6ed49bc82badc701b5a3d4f9b76229` · committed head `c98b37958c6ed49bc82badc701b5a3d4f9b76229` · ancestor of head: **yes**
- Gitlinks moved after the candidate: none
- Paths changed after the candidate: none

### Promotion Completion Gate

- Result **PASS** · declared: references/Hexalith.EventStore · changed gitlinks: references/Hexalith.Folders, references/Hexalith.Projects · evaluated: references/Hexalith.EventStore, references/Hexalith.Folders, references/Hexalith.Projects
- WARNING `UNDECLARED_GITLINK_CHANGE`: gitlink changed between baseline and candidate without a declaration: references/Hexalith.Folders
- WARNING `UNDECLARED_GITLINK_CHANGE`: gitlink changed between baseline and candidate without a declaration: references/Hexalith.Projects

<!-- STORY-FINAL-RECORD:END -->
