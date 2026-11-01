---
title: 'Synchronize current dependencies and publish a verified Conversations release'
type: 'chore'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'fe530bf4f1fe8f5f387ca6b91b873f621464cc63'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="user explicitly requested updates, pushes and release without human validation">

## Intent

**Problem:** Conversations main fails CI before compilation because package preparation selects an untagged Platform identity and a different Builds catalog. The workspace dependencies also lag their remote main branches.

**Approach:** Synchronize the twelve root-declared submodules, consume current stable package/tool versions through the shared catalog, repair the exact Platform/catalog pairing, verify CI, then publish the four Conversations packages through the existing release workflow and confirm official NuGet publication.

## Boundaries & Constraints

**Always:** Preserve existing user work and accepted historical evidence. Use the existing source mapping producer, package references and Release in CI; local compilation uses source references and Debug. Keep commitlint, automated CI and publication checks. Treat the user's explicit instruction to bypass human validation as approval to continue through skill checkpoints and publish. Required dependency commits, pushes and a real Platform catalog-identity release are authorized prerequisites. Describe build/catalog identity separately from runtime deployment qualification.

**Never:** Initialize nested submodules, use recursive/remote submodule updates, fabricate local release tags, disable tests to obtain green CI, overwrite signed evidence, force-push, or claim runtime acceptance from package-build preparation.

</frozen-after-approval>

## Code Map

- `global.json`: already pins Microsoft's current stable SDK 10.0.403; retain the exact pin unless official metadata changes.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: sole version authority; Commons now selects published 2.30.10. Other 27 active external references are current after synchronization.
- `.config/dotnet-tools.json`: module CLI now pins latest published stable 4.41.1, with roll-forward disabled.
- `package.json` / `package-lock.json`: seven direct npm dependencies already current; refresh compatible transitive pins if necessary and verify signatures.
- `references/Hexalith.Platform/references/Hexalith.Builds`: gitlink belongs to Platform. Synchronize this pointer using Git index operations only, without initializing that nested checkout. The identity resolver requires it to match Conversations' root Builds.
- `references/Hexalith.Platform/hexalith-platform.json`: permits CLI [4.38.3,5.0.0-0); no range change needed.
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Workspace/PlatformIdentityResolver.cs`: package mode checks committed Platform identity, staged composition, tag, clean catalog checkout and tool range. Preserve these guards.
- `.github/workflows/ci.yml` and `.github/workflows/release.yml`: preparation must receive the real matching Platform tag/catalog. Ensure initialized Platform tags are available; retain complete history where release or conformance needs it.
- `scripts/verify-release-source.py` / `scripts/verify-release-state.py`: existing exact-source CI and official package/symbol/tag checks.
- `tools/release-packages.json`: exact four-package release inventory.

## Tasks & Acceptance

**Execution:**
- [x] `.gitmodules` root paths -- fetch and fast-forward all twelve clean direct repositories -- synchronize remote changes without nested initialization.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- update the Commons family to the latest stable version present for all consumed IDs; inspect and run catalog validators -- keep versions centralized.
- [x] `.config/dotnet-tools.json` -- select latest published module CLI, preserving exact pin and disabled roll-forward.
- [x] `references/Hexalith.Platform` -- update its recorded Builds gitlink to the selected catalog without materializing nested files; preserve qualification records -- allow a genuine matching catalog identity release.
- [x] `.github/workflows/ci.yml`, `.github/workflows/release.yml`, relevant tooling tests -- fix concrete preparation/version regressions and tag fetching only where required -- keep package/Release validation intact.
- [x] Current source/config changes -- run focused tooling and available source/Debug checks; independently inspect staged diffs and validate exact commit messages.
- [x] Dependency repositories and root -- commit dependency content before parent pointers and push linearly; establish real Platform release identity, verify exact-main CI, dispatch Conversations Release and verify all four packages plus symbols at official endpoints.

**Acceptance Criteria:**
- Given current stable upstream versions, when dependency selection is inspected, then SDK and active package/tool pins are current or a concrete compatibility limitation is reported.
- Given the committed selected tuple, when package preparation runs in CI, then the real Platform release tag and matching catalog satisfy identity checks without guard bypasses.
- Given successful exact-source CI, when the authorized Release workflow runs, then the four declared NuGet packages, symbols and matching GitHub tag/source are independently verified.

## Implementation Notes

The user explicitly authorized publishing and bypassing human validation. Skill approval pauses are therefore already satisfied. Historical evidence-boundary reproduction is not applicable to this routine maintenance request. Initial root and all twelve submodules were clean; root main equaled origin/main. All twelve direct repositories were fast-forwarded successfully.

Verification before publication: root tooling 98 PASS; shared preparation helper 16 PASS;
Builds Debug compilation has zero warnings/errors and 43 native Platform identity tests PASS;
central catalog validation covers 314 packages, 154 families and one official source;
generator fixtures 115 PASS. The local broader audit-validator fixture run ended without a terminal result when its worker returned; its CI rerun is required before release.
An independent empty-cache package-only locked restore of the active compatibility graph passed.
Npm verifies 500 registry signatures and 132 attestations. Compatible update attempts made no
lockfile changes; latest npm 11.21.1 bundles 38 reported advisories including one high.
The obsolete root committed tuple blocks local source preparation with HXW009: its Builds
catalog object 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604 is unavailable from origin. This is
pending replacement by the actual committed matching catalog identity, not bypassed.

Builds content commits are eca2c60f7ceac2eeb7f3a8db0a1b8ae6da9b1267 and
2fd09e1f71a452935d092f6f68cc44aa888149c7. Both exact messages passed pinned commitlint.
The shared helper also preserves producer-owned self-Platform identity. The active compatibility
overlay deliberately advances to EventStore 3.130.2; original measured fixture files stay unchanged.
Historical evidence-boundary reproduction is recorded as not-applicable for routine maintenance.

## Spec Change Log

## Review Triage Log

- Blind 1 — medium, patch: native Git reproduces parent discovery through an empty child `.git` directory. Require direct repository ownership before fetching.
- Blind 2 — medium, patch: a deleted tracked self-Platform marker takes a different helper branch from the producer's committed marker. Use committed identity metadata and preserve producer authority.
- Blind 3 — low, patch: `fetch --tags origin` also applies configured branch refspecs. An explicit tag refspec removes these unrelated updates without adding public API.
- Blind 4 — medium, patch: command mocks do not establish actual tag acquisition, annotated-tag handling or refusal against a real parent repository. Add native temporary-origin coverage.
- Blind 5 — medium, patch (publication follow-up): staged Platform metadata is not a committed tagged identity. Complete the already-planned Platform commit/release and exact root pointer before package CI/release can pass.
- Blind 6 — false: generated audit consumer provenance reflects the producer's tracked declarations; the validator accepts that schema. No runtime/family consumer acceptance is claimed. Actual consumer qualification is the required exact-source CI, and no synthetic acceptance rows will be inserted.
- Blind 7 — medium, patch (CI follow-up): the updated compatibility graph has locked-restore evidence only so far. Its exact committed CI lane must build Release and pass all seven native assertions before publication.
- Blind 8 — low, rejected: requested remedy edits this build's spec, excluded by review triage rules. Progress metadata will be maintained as work actually completes.
- Blind 9 — low, rejected: requested remedy edits this build's spec. The exact source preparation command and unavailable-object result are retained below as ordinary progress evidence.
- Blind 10 — medium, patch (publication follow-up): the new real Platform Release must bind its tag/source, tracked tool manifest and committed catalog gitlink. That publication is already required; the old unqualified runtime tuple stays unchanged.
- Edge 1 — medium, patch (publication follow-up): committed root/Platform identities still describe the old tuple until the planned dependency commit, real tag and root pointer are published. Resolve before running the release gate.
- Verification gap 1 — medium, patch: native identity tests check preexisting tags, while helper tests mock acquisition. Add a temporary local origin and assert the real helper fetch restores the missing tag to its exact commit; mock only dotnet.


## Verification

- `python3 scripts/check-root-submodules.py --repository .` after staging root gitlinks.
- `npm test` and focused changed Python tooling tests.
- Shared catalog validators selected from Builds `Tools/README.md`.
- Individual mapped source/Debug test project builds and native xUnit runs when available.
- Pinned commitlint CLI validates every full candidate message with retained output.
- GitHub exact-source CI followed by existing Release and publication verifier.

The blocked pre-fix source invocation was `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/conversations-maintenance-source-2 --output json` from the root. It returned exit 2, status failed, rule HXW009: pinned Builds `Props/Directory.Packages.props` at `74c1bc5ca68bc9856a5fa7d06323f60a0b48b604` cannot be read. `git fetch --no-recurse-submodules origin 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604` from Builds returned exit 128 (`upload-pack: not our ref`). Raw preparation output is retained in `/tmp/conversations-maintenance-source-result-2.json`.

Review patches resolved: 16 helper tests now pass against real temporary origins, including missing annotated tags, unchanged branch refs, invalid child metadata, committed self-identity and untouched nested dependencies. The self-CI contract test passes. Builds final content is `0b8c3f20e99d26a1562dd477bfb0162d23aebf2c`; its committed audit validates 314 packages/154 families. Platform source is `064a2e1561f2a2b9ba22df4bb14224e338b8a1ec`, recording that exact catalog and CLI 4.41.1. Both dependency pushes and their CI are being verified; no runtime acceptance is claimed.


CI follow-up for root `1e1d891c1a828873c70eb66953a80ba7fb3e9941`: run 41311066253 passed all package preparation, Windows cleanup, repository tooling, release tooling and the seven native compatibility assertions. Its solution build failed CS0103 on the stale DaprTestPrerequisites reference, and portable conformance failed the unchanged duplicate-gateway-fake scanner. Published integration API inspection found that type absent in both 3.130.1 and 3.130.2. The fixture now relies on mandatory real sidecar startup/health failures rather than an unavailable precheck; no tests are skipped. Integration Debug build passes with zero warnings/errors.

Both gateway implementations were replaced with centrally versioned NSubstitute. Command responses, recording/order and all three failure classes remain covered; wire lookups assert zero gateway calls, and receiver teardown rejects unexpected gateway operations. Single-type files preserve fourteen extracted server helper bodies. Local mapped Debug builds have zero warnings/errors; receiver 62, deletion 35, host 55 and unchanged duplicate guard 3 tests pass. Original accepted proof and fixture evidence bytes remain unchanged. The gateway fixture follows the baseline CRLF convention; its semantic diff only replaces the unavailable precheck.

Shared Builds exact-source CI 41310488998 is fully successful, including the fresh audit-validator fixtures, .NET suite, G-4 source/package qualification and CodeQL. Platform catalog CI 41310551435 passed; real release v1.1.0 binds source 064a2e1561f2a2b9ba22df4bb14224e338b8a1ec, catalog 0b8c3f20e99d26a1562dd477bfb0162d23aebf2c and the tracked CLI 4.41.1 manifest. Root mapped source preparation now succeeds, all 648 Contracts tests pass with zero skips, and actual shared package preparation also succeeds and exports the seven expected mapping keys. Semantic-release plans 1.4.0. Conversations CI must pass on the final fix commit before the authorized NuGet release.


CI 41312381310 on `16f2ebf4976001acad596d95652ea6e2cbd272ec` built the full solution and all standalone lanes passed. The final blocking result nevertheless recorded one stale Commons 2.30.3 package assertion, one stale SDK 10.0.402 assertion, and seven live cases sharing an unconfigured catalog authority. Workflow step success was not sufficient: its shard collector continues and reports failures at the end. The package test now queries the effective project `HexalithCommonsVersion` through MSBuild (actual package-mode query returns 2.30.10); the SDK guard now requires the selected 10.0.403. All three source inventory tests pass.

The isolated live fixture now declares only its actual create/projection operations and projects the accepted ADR 0001 fingerprint/scope into the SDK Mutation/24-hour intent policy. Actual DI handlers, resolver registrations, endpoint workload policies, sidecar AppId/loaded Redis component, retained component/ACL and executing assembly hashes are independently observed. Public SDK prepare/ready/commit persists the dedicated Redis generation; readback and direct security probes bind its root/epoch. Production declarations, historical owner acceptance, and held evidence remain unchanged. New semantic cases reject scope/business drift and unallocated identities while preserving Unicode-equivalent transport retries.

Final mapped Debug build has zero warnings/errors. Eighteen focused Integration cases pass with zero skips: all seven original live admission/projection/security cases, three policy cases and eight Scaffold checks. Owned sidecars, hosts and temporary inputs are disposed. A separate read-only check found no blocking defect. Published NuGet 3.130.2 API inspection removed the source-only ExecutableOwners member; executing assembly digests participate through its supported TopologyDocuments instead. Command operation schemas intentionally model owner payload/response shapes as scoped test metadata, not production schema acceptance. Full CI remains required for the next commit and for the authorized 1.4.0 publication.

CI 41316233269 on `4c7fe22f26d8f40a66dc6e393c4e319a98d9bda9` passed the full solution build, all 2,496 unit/integration cases, conformance, browser, Windows cleanup, tooling and compatibility lanes. Its final AppHost lane passed eight topology cases but exposed a separate executable pairing defect: the synchronized EventStore entrypoint uses TechnicalHttpRouteCatalogReceiver, absent from the selected published 3.130.2 libraries. The package/Release runtime harness must select the existing Builds-owned Module.EventStoreHost rather than compile a newer EventStore source entrypoint against older published libraries. Source/Debug retains the source-owned host; provenance must describe the actual selected owner and SDK. No test or production-boundary assertion is disabled.

Published 3.130.2 inspection also establishes the current un-enrolled AppHost contract: SubmitCommandHandler acquires catalog authority before admission or domain dispatch. Missing local identity returns the SDK's precise HTTP 503 problem (`https://hexalith.io/problems/service-unavailable`, detail `Catalog authority cannot safely admit this operation.`). The old 422 domain-rejection expectation cannot be reached without enrolling both production hosts. The retained runtime test therefore proves the actual earlier refusal, preserves authentication checks, and corroborates absent command state through the real sidecar rather than enrolling held production capabilities or accepting generic server errors. The separate live fixture still exercises admitted commands and projections. Obsolete administrative projection cutover and terminal-rejection polling are removed from this un-enrolled-host test. CodeQL on the prior source is successful.

The selected host is now stamped with its actual owner revision and selected SDK identity, its exact project path and suppressed rebuild are checked, and package mode verifies the Gateway dependency in its real `.deps.json`. The existing Builds host has an explicit HTTP endpoint, covered by a model assertion. Independent inspection found no remaining mismatch with the exact published SDK. Source/Debug compilation reports zero warnings/errors, and the real runtime case passes without skips: gateway 401, valid-channel/no-workload Conversations 401, exact catalog 503, exact authenticated status 404, actual sidecar metadata and strong-consistency 204 for both canonical status/archive keys. Owned AppHost/sidecars are disposed; shared Dapr services remain running. CRLF raw-YAML comparisons normalize both sides without changing profile tokens.


## Final Result

Completed 2026-11-01. All twelve direct submodules and the root are synchronized and pushed. Microsoft's current stable SDK remains 10.0.403. Active package/tool updates are selected through the shared catalog; seven direct npm pins are current. The reported upstream npm 11.21.1 bundled advisories remain as documented above.

Released source is `6269c71200bd5f8646f0a96159f7c67e9a28a5dc`. [Exact-source CI 41319564221](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/41319564221) passed every blocking lane: 2,496 unit/integration cases plus ten AppHost cases, with zero failures; conformance, browser, Windows cleanup, tooling and compatibility jobs also passed. [CodeQL 41319564197](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/41319564197) and commit policy 41319564037 passed.

[Release workflow 41321273607](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/41321273607) successfully published [v1.4.0](https://github.com/Hexalith/Hexalith.Conversations/releases/tag/v1.4.0) through trusted publishing, without a human approval pause. Its publication verifier passed. The real tag resolves to the exact released source. Independent official-endpoint checks downloaded and verified all four packages and all four symbols against the eight GitHub assets, including canonical package contents, nuspec identities/versions and PDB presence:

| Package | Official version |
| --- | --- |
| Hexalith.Conversations | [1.4.0](https://www.nuget.org/packages/Hexalith.Conversations/1.4.0) |
| Hexalith.Conversations.Contracts | [1.4.0](https://www.nuget.org/packages/Hexalith.Conversations.Contracts/1.4.0) |
| Hexalith.Conversations.Client | [1.4.0](https://www.nuget.org/packages/Hexalith.Conversations.Client/1.4.0) |
| Hexalith.Conversations.Testing | [1.4.0](https://www.nuget.org/packages/Hexalith.Conversations.Testing/1.4.0) |

Local verification evidence is retained in `/tmp/conversations-maintenance-20261101/verify-publication-final.log`, `verify-symbols-final.log` and `nuget-symbols/verification.json`; downloaded official symbol archives are retained beside that manifest. Every maintenance commit candidate passed its owning repository's pinned commitlint; successful logs are retained in the same maintenance directory.

The root release and required Builds/Platform prerequisites are green. A separate read-only audit found pre-existing failures in independent upstream workflows. Scope clarification was offered while the Conversations release continued; these independent pipelines were not part of its exact-source qualification:

| Independent repository | Existing failed workflow |
| --- | --- |
| EventStore | [CI startup](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/41305489008), [published compatibility](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/41305488708) |
| Projects | [CI and runtime qualification](https://github.com/Hexalith/Hexalith.Projects/actions/runs/41298330907) |
| Folders | [CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/41306394415), [contract gates](https://github.com/Hexalith/Hexalith.Folders/actions/runs/41306394168) |
| Tenants | [Source-reference integration](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/41290951833); its main CI passed |
| FrontComposer | [Dependency governance](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41289068887), [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41289068705) |
| Memories | [Publication fixtures](https://github.com/Hexalith/Hexalith.Memories/actions/runs/41297849598), [earlier Release](https://github.com/Hexalith/Hexalith.Memories/actions/runs/41297849173) |

The full read-only audit is retained at `/tmp/conversations-maintenance-20261101/submodule-ci-audit.json`. These failures are reported separately and no claim is made that every upstream repository is green. Historical held runtime/deployment records remain unchanged; this publication establishes the current package release only.
