---
title: 'Story 11.4: Restore package qualification and maintainer commands'
type: 'chore'
created: '2026-11-27'
status: 'in-progress'
baseline_commit: 'fe303008ca2d548a05f665b3840b5c1724442db7'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Give release maintainers a clean-checkout path to qualify the fixture and install all five Conversations candidate packages. Ten of the fixture's 17 files differ from checkpoint B, the package smoke installs four packages, the workspace CLI conflicts with the owned producer, and the root tooling command omits maintained tests.

## Boundaries & Constraints

**Always** restore the 17 checkpoint B files, including the original four locks, to byte identity and qualify the current graph through the external overlay and its four successor locks. Keep the catalog drift guard, dependency audits, and Stories 11.1–11.3 evidence. Use the published feed for dependency restores and the candidate feed for the exact five installed packages.

**Scope** covers local build tooling and maintained instructions. Hexalith.CiCd and Builds own shared release workflows and tooling. Release signing, publication, deployment, product dependency upgrades, and submodule changes are outside this story.

| Scenario | Expected behavior |
| --- | --- |
| Overlay and direct catalog disagree | Refuse before restoring, with both selections in the diagnostic. |
| Fixture bytes or file inventory drift | Fail the focused check against the frozen proof. |
| Published and candidate feeds contain the same Conversations ID and version | Restore that ID from the candidate feed. |

</frozen-after-approval>

## Code Map

- `docs/release-evidence/thin-module-fixture-proof-v1.json` identifies checkpoint `aeca17f120aa731d0d47b79b7d1b54fb63144bb8` and gives the 13 authored file and four original lock hashes.
- `tests/compatibility/MinimalModule/` owns the isolated Release adapter, `Framework.props`, `RegisteredCommand.targets`, lock selection, and the seven-case native runner. The pre-`b998a24c` targets show the external project-to-package conversion for the frozen inputs.
- `scripts/validate-built-packages.py` runs packing, payload/symbol validation, and the isolated consumer smoke from `tools/release-packages.json`. The public smoke lacks EventStoreExtensions.
- `pyproject.toml` defines the maintained `tests/tooling` suite. Portable surface checks use actual mapped Release assets.
- `.config/dotnet-tools.json` pins 6.8.5, which fails source and package preparation with `HXW009`. Builds 6.9.1 matches the direct catalog and owned CI producer and prepares both modes. The catalog and overlay select EventStore 5.6.0. The SDK is `11.0.100` in `global.json`.

## Tasks & Acceptance

### Execution

- [x] `tests/fixtures/Hexalith.Conversations.MinimalModule/` — restore the frozen bytes from checkpoint B.
- [x] `tests/compatibility/MinimalModule/{Framework.props,RegisteredCommand.targets,locks/package/,README.md}` — adapt the frozen project references and package metadata externally to net11.0 and the direct catalog. Regenerate the successor locks with NuGet and document reproducible qualification.
- [x] `tests/tooling/test_minimal_module_compatibility_overlay.py` — assert exact fixture inventory/hashes, external lock selection, resolved package references, and drift rejection.
- [x] `scripts/validate-consumer-package-references.py` and `tests/tooling/test_package_contract.py` — include EventStoreExtensions in the package-only smoke. Pin all five candidate IDs to the candidate feed and exact version, and cover that mapping.
- [x] `.config/dotnet-tools.json` — align the local manifest with the published 6.9.1 producer.
- [x] `package.json`, `README.md`, `module/{README.md,platform-handoff.md}`, and `docs/runbooks/current-change-validation.md` — wire `npm test` to locked pytest discovery and document the current mapping and build prerequisites. Retire the stale release-plan alias and legacy release description following the CiCd transfer.
- [x] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — record commands, exact inputs, counts, and failures or blockers. Apply the owned lifecycle transition after validation and review.

### Acceptance criteria

- Given a fresh checkout with the direct Builds catalog, when NuGet restores the external fixture graph from nuget.org into one empty package and HTTP cache and then restores in locked mode into a second empty cache, then all four successor locks match byte-for-byte, the Release fixture and AppHost build, the runner passes all seven named assertions, and the 17 fixture files match checkpoint B.
- Given a producer-mapped Release build, when `python3 scripts/validate-built-packages.py` runs, then five candidate packages and their symbol archives validate and the independent, empty-cache consumers restore, compile, and exercise all five candidate packages.
- Given a fresh checkout with only direct submodules, when a maintainer follows the documented mapped restore, build, and focused native test, then the producer selects Debug and `PublicQueryCompatibilityTests` passes on `net11.0`. Given the documented Release prerequisites, when `npm test` runs, then it executes the full maintained tooling suite.

### Review Findings

- [ ] [Review][Patch] Strip missing ambient MSBuild/NuGet environment variables (`CustomAfterMicrosoftCommonTargets`, `RestoreSources`, `RestoreAdditionalProjectSources`, `RestoreConfigFile`, `RestoreLockedMode`, `RestorePackagesWithLockFile`, `ManagePackageVersionsCentrally`, `DirectoryPackagesPropsPath`, `NUGET_SCRATCH`, `NUGET_PLUGINS_CACHE_PATH`) in `isolated_consumer_environment()` [scripts/validate-consumer-package-references.py:209]
- [ ] [Review][Patch] Add negative unit tests for all four `verify_candidate_packages()` refusal branches (extra `packageFolders`, non-candidate `source`, wrong version/type/path, missing candidate package) [tests/tooling/test_package_contract.py:226]
- [ ] [Review][Patch] Align runbook Python prerequisite to 3.12+ (matching mandatory `references/Hexalith.Builds/check.py`) and switch `mapped()` from heredoc `python3 -` to `python3 -c` so child commands preserve caller stdin [docs/runbooks/current-change-validation.md:47]
- [ ] [Review][Patch] Resolve `candidate_feed` with `.resolve()` before comparing against `.nupkg.metadata` `source` in `verify_candidate_packages()` [scripts/validate-consumer-package-references.py:249]
- [ ] [Review][Patch] Add negative unit test verifying `package_version()` rejects invalid SemVer strings via `validate_semver()` [tests/tooling/test_package_contract.py:184]
- [ ] [Review][Patch] Detect symlinked `bin`/`obj` directories before pruning them in `verify_frozen_fixture()` and cover symlink injection in `test_frozen_fixture_rejects_byte_and_inventory_drift()` [tests/tooling/test_minimal_module_frozen_fixture.py:33]
- [ ] [Review][Patch] Add `if __name__ == "__main__": unittest.main()` to `test_minimal_module_frozen_fixture.py` so direct `python3` execution runs the suite [tests/tooling/test_minimal_module_frozen_fixture.py:74]
- [ ] [Review][Patch] Resolve `root = Path(temporary).resolve()` in `test_consumer_entry_point_restores_from_candidate_feed_into_two_empty_caches` to prevent symlinked `TMPDIR` mismatches [tests/tooling/test_package_contract.py:228]
- [ ] [Review][Patch] Resolve `BUILDS_ROOT / "Props/Directory.Packages.props"` when asserting equality against resolved `DirectoryPackagesPropsPath` [tests/tooling/test_minimal_module_compatibility_overlay.py:114]
- [ ] [Review][Patch] Remove broken `commitlint` script and dead `semantic-release` override from `package.json` [package.json:9]
- [ ] [Review][Patch] Add a Story 11.4 section heading above its deferral entry in `deferred-work.md` so it is not attributed to Story 12.3 [_bmad-output/implementation-artifacts/deferred-work.md:1625]

#### Rejected

- **Spec frontmatter `status: 'done'` vs `sprint-status.yaml` `review` and `review_loop_iteration: 0` (`acceptance-auditor`, `blind-hunter`):** rejected — fix edits the spec under review (`sprint-status.yaml` accurately remains `review` until review completes).
- **Spec Code Map / Execution checklist omits `test_minimal_module_frozen_fixture.py`, `MinimalModuleAuthoringCostBaselineValidationTest.cs`, and `epic-11-context.md` (`acceptance-auditor`, `blind-hunter`):** rejected — fix edits the spec under review.
- **No CI check guards the four frozen checkpoint-B `packages.lock.json` files (`blind-hunter`):** `false` — `AGENTS.md` (Hexalith.CiCd policy) forbids story-evidence checks in module CI; BH11 intentionally split frozen-proof verification into local `npm test` / focused tooling discovery while keeping `after-unit-tests` on product/compatibility checks.
- **Byte-rehash cached `.nupkg` and assert `.nuget/packages` absent in `validate_consumer()` (`blind-hunter`):** `false` / `low` — `main()` guarantees a brand-new `TemporaryDirectory` or non-existent `--work-directory`, so a pre-populated cache is unreachable via `main()`, and adding byte-rehash checks adds complexity for an undemonstrated defect.
- **`dotnet restore` lacks `-warnaserror` and `NU1901;NU1902` alignment (`blind-hunter`):** `false` — `dotnet build --no-restore -warnaserror` replays NuGet restore warnings from `obj/project.assets.json` as build errors, and standalone consumers intentionally run stricter without `Directory.Packages.props`.
- **`TestingConsumer` lacks `-failSkips` and functional API assertion (`blind-hunter`):** `false` — `PackageSmokeTests` has one unconditional `[Fact]` that cannot skip, and `TestingConsumer` smoke behavior is unchanged from baseline.
- **`RegisteredCommand.targets` global-property overrides, project-name guard, target execution coverage, flat dict overwrite, and minor untested refusal branches (`blind-hunter`):** `false` / `low` — empirically verified `test_static_restore_graph_contains_only_fixture_projects_and_eventstore_packages` runs `MinimalModuleCompatibilitySelectPackages` across the fixture solution; package-mode graph contains only the 4 fixture projects; each EventStore package is directly referenced by only one fixture project; global `-p:` override is standard MSBuild behavior.
- **Unexpected empty directories not tracked in `verify_frozen_fixture()` (`blind-hunter`):** `low` — unlikely in everyday use (Git does not track empty directories) and adding directory-set tracking adds complexity.
- **`tests/compatibility/MinimalModule/README.md` uncleaned env vars and `HEAD` lock comparison (`blind-hunter`):** `false` — lines 21–22 explicitly specify a disposable checkout in a fresh Bash session, and lines 63/67/69 pass `--packages` and `-p:RestorePackagesPath` explicitly.
- **`.nupkg.metadata` missing/null `source` raises `KeyError`/`TypeError` instead of `ValueError` (`edge-case-hunter`):** `false` — NuGet always emits a string `source` for restored packages, and `main()` already catches `Exception` and exits 1 with a concise error.
- **Solution restore with `RestoreUseStaticGraphEvaluation=true` skips `MinimalModuleCompatibilitySelectPackages` (`edge-case-hunter`):** `false` — empirically disproved: running `dotnet msbuild Hexalith.Conversations.MinimalModule.slnx -t:GenerateRestoreGraphFile -p:RestoreUseStaticGraphEvaluation=true` against a drifted catalog still triggers `RegisteredCommand.targets(35,5)` error and exits 1 with no drifted entries written.
- **`assert_catalog_refused` bare `else:` without explicit `elif` or removal assertion (`edge-case-hunter`):** `low` — unlikely in everyday use (only 3 literal callers directly above it, and an unremoved XML entry already fails the expected-diagnostic assertion) and fixing adds extra branches/guards.

## Implementation Notes

- Restored the ten drifted paths to checkpoint B `aeca17f120aa731d0d47b79b7d1b54fb63144bb8`. The focused check verifies the frozen proof's SHA-256 `01e384e6578d57e09d78e686a4cf6420123c0fbc69419f29946e6235d2fef0c3`, the exact fixture inventory, and all 17 hashes listed by that proof. It checks modified, missing, and extra files as refusal cases.
- The isolated overlay uses SDK `11.0.100`, the direct Builds catalog at `91cb7cba990ffdf82930f9bf8cb68aef41baba76`, target `net11.0`, and EventStore `5.6.0`. The frozen AppHost SDK is `13.6.1`. The current catalog selects Aspire hosting `17.0.0`, Toolkit Dapr hosting `17.0.0-preview.1.261120-0454`, Microsoft.NET.Test.Sdk `18.12.0`, and xUnit `4.0.3`.
- NuGet regenerated all four successor locks using the [published-feed commands](../../tests/compatibility/MinimalModule/README.md#regenerate-and-verify-the-successor-locks). The initial `--force-evaluate` and the second `--locked-mode --force` restores used independent empty package and HTTP caches. The regenerated bytes equal the four files already at the baseline. The Release fixture and AppHost built with zero warnings or errors, and `python3 tests/compatibility/MinimalModule/run_compatibility.py` passed 7/7.

| Successor lock | Resolved packages | SHA-256 |
| --- | ---: | --- |
| AppHost | 111 | `662611a82ed422f068775ad6ff951555584ea5ac1bb9af3b153405fc68263fe6` |
| Contracts | 3 | `68c870269d653049106743e270db1bc46146a05d3e4247238cb5aee1ccb46020` |
| Server | 63 | `e026f0658a947fbed2d183bd60cfe8044554a7b4a7a6c5e894414f8f8fc6b33a` |
| Tests | 90 | `75c38dc0eac31b6a5ad891bfcbd5384290bab9960067ee05a09270b2758169cc` |

- The focused tooling run, `run-with-temp uv run --frozen pytest tests/tooling/test_minimal_module_compatibility_overlay.py tests/tooling/test_package_contract.py`, passed 19/19. The duplicate-feed test restored all five candidate IDs from the candidate feed and the dependency under the published feed's wildcard mapping, then verified the selected package bytes. The catalog fault used `dotnet restore --locked-mode` and reported the overlay version and mutated catalog version before writing restore assets.
- The [clean-checkout commands](../../docs/runbooks/current-change-validation.md#mapped-build-and-tests) ran with the published CLI `6.9.1+91cb7cba990ffdf82930f9bf8cb68aef41baba76` and the root-declared submodules at their pinned commits. Source mode selected Debug and passed `PublicQueryCompatibilityTests` 1/1. Package mode selected Release and restored and built `Hexalith.Conversations.slnx` with zero warnings or errors. Each command inherited the complete producer JSON environment.
- The local exploratory command `dotnet restore tests/compatibility/MinimalModule/Hexalith.Conversations.MinimalModule.Compatibility.csproj -m:1` with the plain package mapping returned `HXW006`. The shared CI adapter requires managed authority bound to committed fixture bytes. Both shared managed preparation routes require a clean committed module tree. The documented local fixture build uses the standalone external overlay, and CI retains its managed adapter.
- Under the Release package mapping, `python3 scripts/validate-built-packages.py` validated five `1.0.0` package/symbol pairs. The two independent empty-cache consumers restored and compiled with zero warnings or errors. The public consumer loaded four candidate assemblies and exercised `ConversationsCatalogAdapters.Create()`. The Testing consumer's native assertion passed 1/1.
- Under the same Release mapping, `npm test` ran locked pytest 9.1.1 with Python 3.13.13 and passed 24/24, including the real SDK portable-surface checks.
- The first full Contracts run exposed `SM2_SOURCE_INVENTORY_DRIFT` because the native guard still pinned the intervening `b998a24c36ee409f717a41033e98c47fcbc8d6ce` source. The guard now checks the live fixture against checkpoint B and continues to verify the historical measurement against checkpoint C. Rebuilding and rerunning the entire mapped Release Contracts project passed 639/639.
- `pwsh -File tests/install-playwright.ps1` defaults to Debug and produced `HXW006` under the Release mapping. The documented Release invocation, `pwsh -File tests/install-playwright.ps1 -Configuration Release -SkipBuild`, succeeded and the native Admin Web project passed 14/14.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and route | Evidence |
| --- | --- | --- |
| BH1: Ambient EventStore version | medium · patch | The catalog probe strips the override while the fixture can still evaluate it. The global override probe passed the guard with an unqualified version. |
| BH2: Static restore graph | medium · patch | Static graph discovery observes the restored source references before the conversion target runs. The probe listed six EventStore projects. |
| BH3: Partial catalog drift | low · patch | The direct catalog probe can find other EventStore entries after removing an ID needed by this fixture. Check the evaluated entries for the fixture's package references. |
| BH4: Ambient fallback cache | medium · patch | A fresh candidate cache stayed empty while NuGet successfully restored from the warmed published fallback. |
| BH5: Repository-contained consumer workspace | medium · defer | A caller-selected descendant inherits the module's build configuration. The same behavior exists at baseline. The default workspace is outside the repository. |
| BH6: Assembly prefix assertion | low · patch | A sibling assembly would satisfy the prefix check. Assert the exact assembly for each of the four public references. |
| BH7: Routine lock qualification | low · patch | The documented checksum captures the state after regeneration. Add a comparison with the committed locks for routine qualification. |
| BH8: Managed CI run pending | false · rejected | The implementation notes already record the precise HXW006 observation and the committed-tree condition. Standalone overlay qualification and mapped solution verification are the agreed acceptance checks. |
| BH9: Release-plan owner disposition | low · patch | The legacy command is gone. Cite the repository's recorded CiCd transfer and deletion in the maintainer instructions. |
| BH10: AppHost opt-in command | low · patch | The standard native command skips the live AppHost assertion. Document the exact opt-in used to complete that lane. |
| BH11: Historical proof check in CI | medium · patch | The existing after-unit-tests action discovers the entire overlay test module. Move the proof digest and its mutation tests to the maintained local tooling discovery. |
| EC1: Ambient restore package path | medium · patch | An inherited `RestorePackagesPath` overrides `NUGET_PACKAGES`. The warmed published cache survived a successful restore without the candidate cache being populated. |
| EC2: Ambient path in fixture commands | low · patch | The two documented restores use environment variables that lose precedence to `RestorePackagesPath`. Pass the cache path explicitly. |
| EC3: Effective fixture version | medium · patch | The direct probe and the actual fixture version disagree when an override is set. Same cause as BH1. |
| EC4: Static graph timing | medium · patch | A real static restore walked an EventStore-shaped project that ordinary restore excluded. Same cause as BH2. |
| VG1: Default catalog discovery | low · patch | Both automated graph checks supply the root explicitly. Removing the fallback left zero catalog items on the documented standalone path. |
| VG2: Per-consumer cache isolation | medium · patch | The existing tests never observe both cache paths through the entry point, so a shared cache would still pass. |

## Verification

- Use `run-with-temp` for clean-checkout qualification. Pass the producer's JSON environment to each mapped restore, build, and test command.
- Run `python3 tests/compatibility/MinimalModule/run_compatibility.py`, `python3 scripts/validate-built-packages.py`, `npm test`, and `git diff --check`. Retain fixture and lock hashes with the results.
- Build `Hexalith.Conversations.slnx` and run each maintained native test project individually in the producer-selected configuration, including integration projects, before marking the story done.

The mapped Release solution rebuilt with zero warnings and errors after the
Contracts guard correction. The final `npm test` run passed 24/24 after the
feed-mapping test confirmed that an additional Conversations-named dependency
also uses the published feed. `git diff --check` passed. The disposable jobs
retained transcripts, including the 17 fixture hashes and the four successor
lock hashes, in the workspace's `.artifacts/story-11.4-*.log` files.

Each maintained project ran with the package mapping, `dotnet test --project
<project.csproj> --no-build --no-restore --stop-on-fail off`, on net11.0.
Totals use the successful full Contracts rerun. The standard AppHost run
passed 25 and skipped its locally opt-in live boundary case. A second
direct-submodule checkout restored and built the Release AppHost test project
under the package mapping. With `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`,
the built test assembly passed 1/1 using
`-method Hexalith.Conversations.AppHost.Tests.ConversationsAppHostRuntimeBoundaryTest.RetainedAppHostShouldRefuseCommandsWithoutCatalogAuthority -noLogo -failSkips`.

| Project suffix | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Admin.Web.Tests | 14 | 0 | 0 |
| AppHost.Tests | 26 | 0 | 0 |
| Client.Tests | 76 | 0 | 0 |
| Conformance.Portable.Tests | 308 | 0 | 0 |
| Conformance.Tests | 91 | 0 | 0 |
| Contracts.Tests | 639 | 0 | 0 |
| DeletionConformance.Tests | 35 | 0 | 0 |
| IntegrationTests | 59 | 0 | 0 |
| Server.Tests | 1816 | 0 | 0 |
| TestSupport.Tests | 44 | 0 | 0 |
| Tests | 189 | 0 | 0 |
| Total | 3297 | 0 | 0 |

### Post-review rerun

Both disposable reruns exited 0. The fixture run repeated the two audited
published-feed restores, matched the four checked-in successor locks and all
17 frozen hashes, built Release with zero warnings or errors, and passed 7/7
native assertions and 9/9 overlay and frozen-fixture checks. The static restore
graph contained exactly the four fixture projects. The effective-version and
missing-catalog-entry tests refused restore before writing assets.

The direct-submodule workspace repeated the mapped Debug focused test (1/1),
the Release solution build (zero warnings or errors), all five package and
symbol checks, and both isolated installed consumers. Package cache and feed
collision coverage ran under `npm test` (27/27). All 11 Release test projects
ran individually with the live AppHost boundary enabled. The final run matched
the table above with 3,297 passes, zero failures, and zero skips.
`python3 scripts/check-root-submodules.py --repository .` and
`git diff --check` passed.

Local transcripts are `.artifacts/story-11.4-fixture-post-review.log` and
`.artifacts/story-11.4-mapped-post-review.log`.
