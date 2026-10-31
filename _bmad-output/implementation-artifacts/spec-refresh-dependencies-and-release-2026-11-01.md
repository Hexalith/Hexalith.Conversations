---
title: 'Refresh dependencies and publish the next verified Conversations release'
type: 'bugfix'
created: '2026-11-01'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9e1d97ae46a0f84d34f92ff53f3c362b539c7ace'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="User explicitly authorizes updates, pushes, release and bypassing human validation in the invocation">

## Intent

Update Conversations to the latest stable SDK and active dependencies, synchronize every root-declared submodule, repair CI failures, push the work, and publish and verify the next release. The user explicitly authorizes external Git and release actions and requests no human validation checkpoints.

## Boundaries & Constraints

Preserve user changes, linear history, commitlint enforcement, automatic product checks, frozen fixture source and original locks, signed historical evidence, and existing release-source and publication verification. Synchronize direct submodules only; never initialize or update nested submodules. Use latest stable dependencies; preserve a newer existing preview where replacing it with stable would downgrade the dependency. Existing SDK 10.0.403, npm manifest versions and module CLI 4.39.0 already satisfy the latest available versions.

The Platform producer owns dependency mapping. Do not invent a release tag, override its checks, hand-write mapping imports, claim a green CI without execution, or publish packages when the configured green-source release prerequisite fails. Report a precise upstream blocker if no coherent released Platform/catalog tuple exists.

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml`: Windows TestSupport checkout excludes historical HTTP caches but Git rejects colon-containing paths before checkout; configure NTFS path validation before checkout while preserving the exclusion and long-path setting.
- `tests/tooling/test_release_tooling.py`: existing Windows sparse checkout regression and workflow assertions; extend them to verify configuration order and preserve source coverage.
- `tests/compatibility/MinimalModule/RegisteredCommand.targets`: external qualified EventStore selection currently 3.128.3. Direct Builds main now selects 3.130.1; catalog comparison deliberately rejects drift.
- `tests/compatibility/MinimalModule/locks/package/`: four external successor locks; regenerate explicitly with the package overlay, then use locked restore. Never change `tests/fixtures/Hexalith.Conversations.MinimalModule/` tracked files.
- `tests/compatibility/MinimalModule/README.md`: describes current compatibility selection and qualification.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: imported active versions; latest main selects EventStore 3.130.1, Tenants 6.0.0, FrontComposer 4.7.4, Memories 3.0.13, Projects 1.2.1.
- `references/Hexalith.Builds/Github/prepare-module-build/`: producer-owned preparation. It requires Platform identity read from committed parent HEAD, an exact Platform release tag, and root Builds matching Platform's recorded gitlink.
- `.github/workflows/release.yml`, `scripts/verify-release-source.py`, `scripts/verify-release-state.py`: existing manual-dispatch release, green-main prerequisite, exact package/symbol verification. Production has publishing enabled and no reviewer gate.

## Tasks & Acceptance

**Implementation handoff:** the implementation agent owns the Windows workflow repair, its focused regression checks, and the external compatibility overlay/locks/documentation qualification. The parent owns submodule synchronization, commits, pushes, remote CI and release execution. Do not invoke build skills recursively, stage files, commit, push, edit submodules, or modify this spec in the implementation handoff.

**Execution:**

- [x] `.github/workflows/ci.yml` and `tests/tooling/test_release_tooling.py` — repair Windows checkout and verify configuration precedes checkout without broadening source exclusions.
- [x] `tests/compatibility/MinimalModule/RegisteredCommand.targets`, four external locks and README — qualify EventStore 3.130.1, locked restore/build and all seven fixture assertions while preserving original measured files.
- [x] Root `references/` gitlinks — synchronize all direct repositories to fetched main with fast-forward merges, preserving content and nested submodule boundaries.
- [x] Root SDK/package manifests — verify official latest versions and install pinned local npm tooling; update source only if newer versions exist.
- [ ] Root repository and GitHub Actions — validate exact commit messages, commit repairs and pointer updates separately, push and inspect current CI results.
- [ ] Release workflow and official NuGet endpoints — publish only after current source CI passes; verify all four declared packages and symbols at the exact release version and source.

**Acceptance Criteria:**

- Given current root dependency catalogs, when package compatibility runs, then locked restore and all seven original and registered-command assertions pass with no skips.
- Given archived colon-containing filenames, when Windows checkout runs, then NTFS configuration occurs before checkout and historical cache files remain excluded while current source remains available.
- Given a successful exact-source CI, when the authorized release is dispatched, then the four packages, symbols, GitHub release and source identity verify; otherwise preserve the actual failure and report its exact cause.

## Implementation Notes

- The invocation explicitly authorizes continued execution without human validation; no additional planning approval is needed. Historical evidence-boundary verification is not applicable to this routine maintenance change.
- Initial CI run 41249461335 fails before compilation: Windows path validation, HXW011 untagged Platform, and HXW009 mismatched Builds catalog. Latest Platform main is also untagged and pins an older catalog, so synchronization alone cannot prove package build readiness.
- Root submodule synchronization committed as `82cfafbbc68e363e0c8f1d64f5bff9fa10c2685b`; all twelve direct repositories match fetched origin/main. Eleven gitlinks advanced; AI.Tools was current.
- SDK 10.0.403 is the official latest stable SDK. Root direct npm versions and module CLI 4.39.0 were current; refreshed 21 permitted transitive npm package entries, including bundled dependencies. `npm ci --ignore-scripts`, `npm audit signatures` (500 signed packages, 132 attestations), actionlint and release planning pass. The next planned version is 1.4.0.
- Package preparation command: `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/conversations-package-preparation-20261101 --output json`. It exits 2 with HXW011, HXW009 and HXW007. Platform `a397de8e4d9a700b43cb372fa6b1e59c997c6054` has no release tag and pins Builds `74c1bc5ca68bc9856a5fa7d06323f60a0b48b604`, while available root Builds is `0c13c8980475f01c9a66eda8be1ffac4931bcfdd`.
- `git -C references/Hexalith.Builds fetch origin 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604` exits 128: `fatal: remote error: upload-pack: not our ref 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604`. Source-mode preparation also exits 2/HXW009 because the pinned catalog cannot be read. `dotnet restore Hexalith.Conversations.slnx` exits 1 because producer-selected imports are unavailable. Product compilation remains blocked; no build gate was weakened.
- Compatibility qualification independently restored four external locks from nuget.org with empty caches, built Release with zero warnings/errors and passed all seven native assertions without skips. All seventeen frozen fixture files and original locks retain their SHA-256 hashes. Evidence is in `/tmp/conversations-refresh-compatibility-zpdih2y5/logs/`.
- Existing release verification: `python3 scripts/verify-release-state.py present 1.3.1 --repository Hexalith/Hexalith.Conversations --source-sha 986875bfcb5471953378b600710b5d859f3eaba4 --attempts 1` passes, including all four packages, symbols and GitHub assets. Official NuGet indexes show no 1.4.0 publication.

- Final `npm test` passes all 98 current repository tests, including the seven-test fixture result verification. Log: `/tmp/conversations-maintenance-npm-test.log`. All three automatic review layers completed; edge-case and verification-gap reviewers returned no findings. The Windows scope improvement was applied: NTFS protection is restored immediately after sparse checkout and before initializing submodules; focused regression and actionlint passed again.
- Final npm vulnerability audit reports 38 findings (37 moderate, one high), reduced from 40 before the compatible lock refresh. Signature verification and semantic release planning pass; vulnerability audit results are distinct from provenance checks. No force upgrades or invented compatibility exceptions were applied.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| Platform/catalog prerequisite | Confirmed operational blocker: producer preparation rejects the untagged Platform and unavailable catalog; isolated tests do not prove the product build. This is the explicit failure path retained in the accepted intent. | Preserve blocker; leave work in progress and withhold publication. |
| Global NTFS setting after checkout | Low: the workaround unnecessarily persisted into submodule operations on the disposable runner. | Patched by restoring protection immediately after checkout; ordering regression and actionlint pass. |
| Additional NTFS path inventory guard | False as a current defect: the executed sparse regression proves all excluded tracked paths belong exactly to the historical cache and contain colons; no unsafe tracked path outside that set was identified. A hypothetical new-input guard would add unrequested complexity. | Rejected. |
| Effective configuration rather than substring assertions | False as an observed regression: the current commands are executable, ordered, and uncommented. Removal of the required command fails the existing check; no conflicting later configuration precedes checkout. | Rejected. |
| Additional default-mode Dapr regression | False as an observed defect: the override is inside the existing package-mode condition; omitted package mode does not select it, and no frozen fixture bytes changed. | Rejected. |
| Separate AppHost startup qualification | Maybe-false, low: no runtime failure was demonstrated; this deliberately compile-only fixture has no startup contract. Its AppHost package stays 13.6.1 while Aspire.Hosting advances from 13.6.3 to 13.6.4. Product runtime qualification remains visibly blocked. | Rejected as an unproven additional qualification requirement. |
| Durable local compatibility evidence | False as a current requirement: routine maintenance does not require historical evidence publication; CI already uploads compatibility logs/results. Local logs and all measured outcomes are identified, without claiming durable historical acceptance. | Rejected. |
| Dated discovery references in this spec | Low editorial improvement whose fix only edits the build spec; official metadata/registry discovery ran during investigation. | Rejected under review rules for spec-only findings. |
| Vulnerability audit distinct from signatures | False as missing execution: npm installation and update already reported vulnerability counts; the final JSON audit confirms 38 residual findings separately from signatures. | Recorded actual audit outcome in implementation notes. |
| Executed and pending spec checks | False as a misleading completion claim: checked tasks and implementation notes distinguish PASS, pending remote validation and blocked product preparation; status remains in-progress. | Rejected. |

## Verification

- `npm test`: current repository tooling, including release and compatibility regression checks.
- `actionlint .github/workflows/*.yml`: workflow syntax.
- Fixture restore with explicit package overlay and `--force-evaluate` once, then `--locked-mode`; Release build; native xUnit execution with `-automated sync -failSkips -result-xml`; `verify_results.py` requires seven tests.
- `python3 scripts/check-root-submodules.py --repository .`: staged root declarations and gitlinks match exactly.
- Pinned commitlint CLI validates each exact complete message before commit.
- Producer `prepare-build --mode package`: preserve exact success or diagnostic codes from a committed candidate.
- GitHub CI and release status, followed by `scripts/verify-release-state.py` when a new release exists.
