---
title: 'Synchronize and release current Conversations dependencies'
type: 'chore'
created: '2026-11-14'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'fc7200f84eb5a91ae331883f85a48ccb758c4302'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="user explicitly authorized updates, pushes, release and bypass of human validation">

## Intent

**Problem:** Current main fails CI during package preparation and compatibility checks. The module tool and active compatibility graph lag current published dependencies, and four direct submodule checkouts lag remote main.

**Approach:** Synchronize the direct repositories, use current compatible stable dependencies, repair the real Platform/catalog release pairing, pass automated CI, publish through the existing release workflow, and verify all five packages and symbols against official endpoints.

## Boundaries & Constraints

**Always:** Preserve user work and immutable historical evidence. Use the producer-owned source mapping and exact identity checks. Local compilation uses mapped source/Debug; CI uses package/Release. Retain commitlint and automated test/publication gates. The user's explicit instruction satisfies skill approval checkpoints and authorizes necessary dependency commits, pushes and genuine Platform source publication. Report package build acceptance separately from runtime deployment acceptance.

**Never:** Initialize nested submodules, use recursive or remote submodule updates, fabricate local release identities, disable automated gates, overwrite frozen fixture sources/locks or signed evidence, force-push, or claim that source preparation qualifies production runtime.

</frozen-after-approval>

## Code Map

- `global.json`: latest stable SDK 11.0.100 is already pinned; companion SDK 10.0.404 remains needed for retained tooling.
- `.gitmodules`: twelve root dependencies on main; synchronize selected public sources and preserve the qualified Builds/Platform pair. Tenants also advanced during execution.
- `.config/dotnet-tools.json`: replace stale module CLI 4.48.2 with genuinely published Builds 6.0.2, then align Platform's exact tool pin and range floor. Do not select an unpublished tool version.
- `package.json`, `package-lock.json`: update semantic-release GitHub plugin 12.0.10 to 12.0.11 and refresh compatible transitive dependencies.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: advance the default EventStore catalog from 5.0.1 to 5.1.0, published during execution; incorporate the intervening upstream FrontComposer 6.0.0 adoption; retain Commons 3.0.0, Aspire 13.6.5, Test SDK 18.12.0, xUnit 4.0.3. Keep compatible Dapr hosting 13.6.2-beta.959.
- `references/Hexalith.Platform/references/Hexalith.Builds`: Platform owns this gitlink; pair the exact genuinely published Builds source, currently published patch source 60ce7967607dc8b703fff11f4fc13cabb09c090a, without initializing the nested checkout. Refresh authenticated eng/builds-tuple.json publication metadata, current owner bindings and immutable historical companion descriptors with the supported helper. A genuine qualified Platform source release must tag the resulting source before root preparation can pass.
- `references/Hexalith.Platform/.github/workflows/source-release.yml`: automatically releases source after tuple and catalog workflows pass. Preserve its automated qualification gates.
- `tests/compatibility/MinimalModule/RegisteredCommand.targets`, `Framework.props`, external `locks/package/*.json`: active overlay selects stale EventStore 4.2.0. Upgrade to 5.1.0/net11.0 and current testing dependencies through external overlay/global properties; frozen fixture files stay intact. The early props import also covers SDK reference discovery, which removes the global TargetFramework property before re-evaluation.
- `.github/workflows/ci.yml`, `tests/tooling/test_minimal_module_compatibility_overlay.py`: select active framework consistently for inference, successor locks and native test execution. Use global TargetFramework on fixture commands because late targets cannot safely change SDK inference.
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs`: derive the actual gateway project target path and governed SDK before stamping/checking provenance; execute the new runtime-free selection Fact.
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`: require the actual current net11.0 packaged Contracts DLL.
- `scripts/verify-release-source.py`, `scripts/verify-release-state.py`, `tools/release-packages.json`: reuse exact green-main and five-package/symbol/source verification.

## Tasks & Acceptance

**Execution:**
- [x] `.gitmodules` direct paths -- fast-forward clean repositories without nested initialization; verify remote availability.
- [x] `.config/dotnet-tools.json`, npm manifests -- update stale published tooling and lock dependencies; verify signatures.
- [ ] Platform owning gitlink -- align the current Builds catalog, inspect guidance, validate exact commit message, push and obtain genuine automated source release.
- [x] Active compatibility overlay, external locks, CI commands and tooling assertions -- qualify EventStore 5.1.0/net11.0 with Test SDK 18.12.0/xUnit 4.0.3; retain seven native assertions without skips.
- [x] Current changes -- run focused tooling, workflow checks and mapped Debug checks where relevant; inspect staged diff and independent review findings.
- [ ] Root -- validate exact commit messages, commit and push; fix concrete CI failures until exact-source CI passes; dispatch Release and independently verify five packages, symbols, tag and source.

**Acceptance Criteria:**
- Given official current release metadata, when pins are inspected, then SDK and consumed package/tool versions are current or a concrete compatibility constraint is recorded.
- Given aligned published Platform, Builds and tool identities, when package preparation runs, then its guards pass without weakening them.
- Given the active compatibility graph, when locked restore, Release build and native tests run, then all seven tests pass with no skips and original fixture bytes remain unchanged.
- Given successful exact-main CI, when the authorized release runs, then all five declared NuGet packages and symbols are published and verified against the same release source.

## Implementation Notes

Initial root and all twelve submodules were clean. Routine maintenance makes historical evidence-boundary reproduction not-applicable. No human checkpoints are required under the user's explicit instruction; automated checks remain mandatory. Investigation evidence is retained in `/home/administrator/.codex/task-logs/conversations-release-20261114`.

## Spec Change Log

- 2026-11-14: Builds fd9c4de2b528507921734aae72ff1d1f27ae5abe passed full CI 43588770508. The pre-release main guard found two intervening upstream FrontComposer 6.0.0 adoption/qualification commits. Preserved and fast-forwarded them to 1ebc5b84ff2c8afd21a87d80ef3d9c9048eaf5e5; its exact CI 43590052534 is now the release gate.

- 2026-11-14: EventStore 5.1.0 was published at 10:54:55 UTC during execution. The latest-dependency instruction requires deliberately requalifying that published version in the producer catalog and active compatibility graph. Added the external early framework import after the real SDK build showed reference discovery reverting to the frozen net10.0 default; fixture bytes remain frozen.

## Review Triage Log

- Blind 1 — false: the root Git pointers reference already-published upstream commits. Owning source diffs and release/API comparisons are inspected separately; the aggregate review diff includes our actual Builds content, and final CI covers the selected product graph.
- Blind 2 — false: no mode/name condition defect was demonstrated. Explicit package CI and SDK reference-discovery evaluations plus the native seven-test build exercise the changed import; adding speculative negative cases is unnecessary.
- Blind 3 — false: real clean-cache Release build and native execution exercise reference negotiation across all four projects; evaluated framework checks are supplementary evidence rather than the sole qualification.
- Blind 4 — false: supported commands explicitly pass net11.0 and the early import. A conflicting caller override is outside those commands and still receives normal SDK/package incompatibility diagnostics; no silent incorrect result was demonstrated.
- Blind 5 — false: the explicit compatibility overlay is passed only to the four fixture projects and its catalog query; the latter has no affected testing references. No unrelated package change occurs in the supported graph.
- Blind 6 — false: frozen fixture preservation was measured for this change. The current-change runbook does not require a new permanent hash inventory gate for routine maintenance, and original tracked fixture bytes remain unchanged.
- Blind 7 — false: locked restore and native qualification passed, and successor locks were checked unchanged during independent qualification. No CI mutation was demonstrated; another guard is not required by the routine policy.
- Blind 8 — low, rejected: exact workflow commands reproduce the active graph; isolated clean-cache qualification is additionally described and retained in task logs. A new isolation script would add maintenance surface without an observed product defect.
- Blind 9 — false: current-change policy requires focused checks and truthful outcomes, not immutable evidence publication. Detailed raw logs, native results and frozen-file hashes are retained outside disposable directories; exact source CI remains required before release.
- Blind 10 — false as a claim of missing acceptance: the audit explicitly distinguishes metadata discovery from consumer acceptance. Full Builds CI and exact-source Conversations CI remain required; no other module or runtime acceptance is asserted.
- Edge-case reviewer — no findings.
- Verification-gap reviewer — no findings.
- Parent package follow-up — medium, patch: the active Contracts package inventory still required a net10.0 DLL even though actual Contracts now targets net11.0. Correct the current package assertion to net11.0; retained historical conformance paths remain unchanged. Its real pack/native test is required in the final mapped checks.
- Parent follow-up — medium, patch: the AppHost runtime test still hardcodes the gateway output to net10.0 and equates the root default SDK with the packaged-host catalog view. Current projects target net11.0 and Builds deliberately retains SDK 4.2 for that named host; derive actual project target/SDK before stamping and verifying provenance.

## Verification

- Local implementation: wrapped `npm test` passed all 100 tests; focused release/result tooling passed 37 tests; active compatibility overlay tests passed all four cases. The synthetic drift test now changes every EventStore declaration so retained conditional catalog views cannot hide a missing drift assertion.
- Isolated official-nuget-only compatibility qualification passed generation, independent empty-cache locked restore, Release build (zero warnings/errors), and all seven native assertions (zero failures/skips/errors). Four external successor locks were refreshed; SHA-256 verification preserved all 17 frozen fixture files. Logs and native XML are retained in the task-log directory above.
- `npm ci --ignore-scripts` and `npm audit signatures` passed: 317 registry signatures, 60 attestations, zero vulnerabilities. `actionlint .github/workflows/ci.yml .github/workflows/release.yml` and `git diff --check` passed.

- Wrapped `npm test`, focused compatibility tooling and release tooling; actionlint when available.
- Wrapped isolated fixture restore, locked restore, Release build and native xUnit with `-automated sync -failSkips -result-xml`; `verify_results.py` requires seven assertions.
- Staged `python3 scripts/check-root-submodules.py --repository .` and `git diff --cached --check`.
- Owning repository pinned commitlint validates every full candidate before use, with retained evidence.
- Platform source qualification and real source release; exact-source Conversations CI, existing Release workflow and official publication verifier.

Local implementation review started with producer publication and final tool identity still pending. Root local checks pass: 100 npm tooling tests, 37 focused release/result checks, seven native compatibility assertions, 17 unchanged frozen fixture hashes, actionlint and npm signatures (317 packages/60 attestations; zero vulnerabilities).

Gateway patch independently reviewed with no concrete findings. Runtime-free owner evaluation confirms net11.0 outputs, source SDK 5.1.0 and the named package host SDK 4.2.0. Genuine mapped compilation and the new Fact are still pending the published tuple; package/Release AppHost CI remains mandatory.

Preserved upstream Platform fixes by fast-forwarding to de3ce6797865e31256b1293d0589a31f041f6def. They already update the EventStore owner binding to 49084f66f8d6545cae442875355a7066d5f5e73d and publish fresh exact source companions. The remaining producer pairing changes only allowed tuple metadata and the Builds gitlink; retain existing immutable companions if the strict comparator confirms equivalence. No duplicate owner-binding or companion publication is needed.

Builds computed the genuine next release as 6.0.0 because unreleased commit 5f8ae823fb74e116c7dec5289fb0c91805f092e6 changes reusable-CI permissions with a BREAKING CHANGE footer. The payload still targets net11.0. Platform currently admits only major 5 for its NET11 archive extraction/current-suite dispatch and only majors 4/5 for publication/version descriptors. Add finite major 6 support, preserve 4 legacy and 5 NET11 support and all publication/signature/source guards, reject future 7, and cover archive selection/current suite/owner range. These non-tuple source changes require fresh exact historical companion trees and descriptors after the real 6.0.0 publication is bound; prior companion reuse is no longer applicable.


Platform major-6 review triage:
- Blind 1 — medium, patch: Helm campaign lifecycle test import still rejects major 6 and selects NET10 for non-5. Extend its finite source-generation check and NET11 selector; verify collection with the actual new pin.
- Blind 2 — medium, planned patch: helper/test changes are non-tuple changes, so fresh immutable historical companions and descriptors are required after committing the complete current source. This is already a prerequisite to the upcoming source release.
- Blind 3 — false: structural NuGet semantic-version parsing and official stable-release authentication are separate gates. Existing 4/5 prerelease parsing does not grant publication acceptance; pre-extension and handoff policies still require stable trusted releases. This update does not change that policy.
- Blind 4 — low, patch: make the synthetic major 6 archive control use a structurally valid shared-publication binding and validate it before mocking transport. This improves the intended component coverage without claiming real publication.
- Blind 5 / verification gap — medium, patch: add direct major 6 `--artifacts` receipt reuse coverage through run_tuple_ci, including NET11 root reaching graph evaluation and refusal of wrong framework, binding and extracted bytes.
- Blind 6 — false as a current production defect: preparation explicitly records artifact identity only and candidateAcceptance:false, not host/runtime readiness; graph and runtime consumers check their required assemblies. No actual published major 6 layout failure was observed. The positive synthetic fixture will include the retained host to model the complete archive more faithfully; no speculative new production guard is added.
- Edge-case reviewer — no findings.


Builds 6.0.0 is published at source 1ebc5b84ff2c8afd21a87d80ef3d9c9048eaf5e5 by successful release 43591439615. Both official NuGet blobs are indexed; the module archive signature and exact release/tag/source/run/artifact/manifest closure were independently verified. Platform was rebased onto upstream b17d2ca8147029b6bfb84deec954f2dc4d66bcad, retaining its independent major 6 adoption and NET11 validation-runtime correction. Forty focused merged tests pass; final controls covered 86 cases before that merge, and the actual major 6 Helm import selects NET11. The unavailable upstream EventStore gitlink 3c3a8c70f83cc8e0ddde9f5d8eb0a1ff860eea6f returned 128 (upload-pack:not our ref); it now records published f712c08e5cfacd0e340fe7ec571385e72fc0177f, and the owner binding was generated from the selected Git tree.

Combined Platform source b5fe7f82a03aed8903524d927d4ff8e740556bdc has new immutable remotely verified companions: legacy ae10aff9a4e9be8e2f39f2f5ab3809d3415ee880, pre-extension 843fb8e3f59ad15e479e5ad1bed97e9e040816fe and reader 12878399593930026d08dadd98f4dce0f0cbbd66. Descriptor commit 966df4205994e60b1d0eb31e3c717df719e8c254 is pushed. Exact tuple CI 43596550433 and catalog CI 43596550425 are pending. GitHub reported the authorized PR-requirement maintainer bypass; this was reported to the user. Root remains uncommitted, with tool 6.0.0 selected pending this genuine Platform source release.


Platform catalog qualification 43596550425 passed all six cases. Tuple CI 43596550433 exposed one shared stale catalog guard in all three lanes: the genuine current catalog defaults to SDK 5.1, while the qualified SDK 4.2/OpenAPI 2 view is explicitly selected. Correction ce8a176b999becab69b537555ba9df6c2d14e590 independently checks both contexts, preserves the exact 33 names and three negative consumers, retains the old automatic SDK 4.2 catalog profile, and grants no runtime/candidate acceptance. An immutable real Builds 6 catalog fixture is verified by Git blob and SHA-256 and runs before dependency initialization. Wrapped focused checks passed 44 tests; independent review found no regression. Fresh remotely verified legacy/pre-extension/reader companions are a8993767e1e34865016bf6d390eacbc152d3ac5f, 507f17f6134c442da95437de6b3d2b8fe8a50f18 and 3e6674ef026fd7903d5b07f9defaab3150a7cbf8. Final descriptor source 7f87f99bca88af6ce8e58ab390f532752d3526d2 is pushed; exact tuple CI 43598054171 and catalog CI 43598054204 are running.

The final direct-submodule audit found every selected source on its remote, no nested initialized modules, and one further public Tenants commit f3b0e85f68c0ef0515d567502f59257916ac18b6. It was inspected and fast-forwarded, preserving its recovery-conformance implementation/tests and evidence.


Final wrapped root tooling rerun passed all 100 tests; workflow lint and the staged direct-submodule check passed. Genuine supported source-mode preparation passed with the expected uncommitted identity warnings, then both changed C# test projects compiled in Debug. Native execution passed all three package-inventory cases (including a real Contracts pack) and the new gateway owner-selection case, with no skips/errors. Source warnings grant no package-mode acceptance; strict exact-source Release preparation remains required in CI. Logs, producer environment/mapping and native XML are retained outside the cleaned disposable workspace.

Exact Platform catalog CI 43598054204 passed all six cases. Current tuple lane 43598054171 passed catalog and pin alignment, then exposed NU1109: the explicit retained SDK 4.2 test collector was using catalog-default Memories 4.0, whose DomainService dependency requires SDK 5. The next repair scopes the compatible historical package cohort to that collector while preserving current production defaults and NuGet downgrade refusal.


Further inspection found that the real catalog materializer deliberately strips caller SDK overrides and offers only a named project context. Builds 6.0 therefore needs a producer repair for genuine Platform snapshots: the unchanged finite 33 names automatically select SDK 4.2 and Memories 3.0.27; unknown consumers retain current defaults SDK 5.1/Memories 4.0, and explicit SDK 5.1 or Memories overrides remain authoritative. This replaces the earlier assumption that test-level explicit SDK selection alone would suffice. Producer commits 58e1a5f4a8ced48d23c64fe51e266513344f79d3 and c91beec60ce5eaa8cd62bf055d07fa6ca3890e89 are pushed. All 141 native owning catalog/snapshot/declaration controls passed with no skips/errors; 202 catalog probes and 17 independent authority controls passed. The real incremental Memories audit was generated from committed source and validated for all 317 packages and 157 families. Exact-source Builds CI 43600169822 gates the patch release; no unpublished tool pin is used.

Both old Platform historical lanes are now green: 349 legacy controls and 412 pre-extension controls. Platform follow-up preserves those lanes, recognizes finite Builds 6 consumer selection and current default 5.1, refuses future 7, and scopes the explicit SDK 4.2 collector to its admitted Memories 3.0.27 cohort. The genuine existing producer component run passed all 25 source-propagation controls and compiled changed NET11 production/test code with zero warnings/errors. A separate actual-package regression reproduced NU1109, then passed compatible locked restore/build with exact restored dependencies. Native current-cohort acceptance remains pending the genuine successor publication and authenticated owner binding.


Builds patch release 6.0.1 succeeded in run 43602532457 after exact-source CI 43600169822 passed. Source c91beec60ce5eaa8cd62bf055d07fa6ca3890e89, public tag v6.0.1 and release 445339242 are authenticated; original artifact 14117540783 has SHA-256 4c8964727a14b48c78024d9eaf39819edb603256c4c701e466fd747a87536cb9. Official NuGet uploads propagated by 14:19:02 UTC: both CLI blobs return 200 and both indexes contain 6.0.1. Independent signature and complete tag/source/run/artifact/manifest/catalog/Tooling verification passed. Root tool is now 6.0.1. Platform source ef34c8ad6d404eead7a62652ae6bc86446fae50b records that genuine binding and producer gitlink; its exact full message passed pinned commitlint. Before publication, the remote audit found six related upstream Platform fixes through 2119b7ed; they are being preserved and reconciled, including their exact catalog profile and measured collector guards. Fresh companions will be generated only from the complete reconciled source.


The complete-owner genuine 6.0.1 collector run executed all 28 cases: 27 passed, one failed, zero skips/errors. Earlier sparse-workspace driver failures were retained separately and resolved by complete private checkouts. The remaining production guard correctly rejected actual private Parties assets carrying Memories.Client.Rest/Contracts 4.0 with SDK 4.2. Its actual project context is the finite Parties view, so the producer fix must include that view as well as Platform. No proof guard or authenticated snapshot was weakened.

Builds commits 590a56faea48c114adf370bff8d24a89cb883dbb and 60ce7967607dc8b703fff11f4fc13cabb09c090a preserve the existing finite 40 Parties, 33 Platform and 3 packaged-host names, selecting Memories 3.0.27 only when SDK 4.2 is selected and no explicit Memories override exists. General SDK 5.1/Memories 4 and explicit selections remain unchanged. Independent review found no defect. The actual six Parties projects independently evaluated to SDK 4.2 and all five Memories pins 3.0.27. All 141 native checks, 204 positive/negative view probes, 17 authority controls and the refreshed 317-package/157-family audit passed. Source 60ce is pushed; exact CI 43606901795 gates the expected 6.0.2 patch. Platform remains clean and unpushed at reconciled c3ad0a21b4ab89e98e9b2db4b8e49b758829c164 pending the genuine successor binding and final 28-case run.


Builds 6.0.2 is now genuinely published by release 43608949482 at source 60ce7967607dc8b703fff11f4fc13cabb09c090a after exact CI 43606901795 passed. Both official NuGet indexes and blobs passed at 15:31:19 UTC. The signed module archive and complete original tag/source/run/artifact/manifest/catalog/Tooling closure were independently verified. Release 445387362 binds artifact 14119845550, SHA-256 97cc2ab9fc5392a21de423a5dd4a6a90cc23df8cd06d2c25401717b8b81fe52a. Root CLI 6.0.2 restored successfully. Platform now receives that exact authenticated binding and producer gitlink; current artifact 28/source-propagation 25 qualification runs next with complete private source checkouts.


Final genuine 6.0.2 Platform source f7036c348b26c48e74d827f439fbf969966636bc passed all 28 native current artifact cases, all 25 source-propagation controls, and NET11 Debug build with zero warnings/errors. Corrected real private Parties assets contain Memories.Client.Rest/Contracts 3.0.27; the exact package guard is unchanged. Focused catalog17/SDK13/file-pin19/binding21/official locked-graph1 passed, and independent final metadata review found no defect.

Fresh immutable source companions were generated from exact f703 and remotely verified: legacy408a3aefd558aab6e6753721241ed0d6dba667a3, pre-extension5bc9850e067ddd2248f4643d5de658deb4c58818 and reader74e26c05303cebc16eb5bd8941ee3da2d9aa5f20. Descriptor commit08b0e047ba8836f660547acc413ccbb2c7bb76ad is pushed; exact-source tuple CI43611416111 and catalog CI43611416026 gate the genuine automatic Platform source release. The authorized maintainer PR requirement bypass was reported, with automated qualification preserved.

A final root submodule audit found additional public FrontComposer and Tenants test/CI/dependency synchronization commits. Inspected and fast-forwarded FrontComposer2671a152bcebffb8c00a02407adddd375cb02d48 and Tenants0cfe90e887e4d9d9f0168cba98fca1852cb80313, then synchronized only the twelve root-declared URLs; no nested submodules were initialized. Final mapped Debug checks against the complete selected 6.0.2 tuple are running in a new private workspace with separate retained logs.


Final root checks against staged CLI6.0.2/Builds60ce/Platform08b/FrontComposer2671/Tenants0cfe passed genuine supported source-mode preparation, both changed test-project Debug builds and all four native package/gateway cases with zero failures/skips/errors. Root staged submodule checker, whitespace check and workflow lint passed. Raw producer mapping, commands, native XML and summary are retained under tasklogs/root-final602. Strict package-mode acceptance remains gated by genuine Platform tag and exact-source root CI.


Platform08b completed every hosted qualification: current478/base97/extension9, historical349/412 and catalog6/6 all passed. Automatic source-release43615597595 correctly refused because main advanced while CI ran. Preserved the three descendant upstream commits (consumer-context guard/tests, reconciled merge and fresh descriptors) by fast-forwarding to d77f741d11bf01352a11fa6df2f1a14313c1328a. They retain the same genuine6.0.2 tuple and add exact source/range plus actual Parties consumer checks. Its catalog43613487451 and both historical lanes are green; current tuple43613487426 is still running. Root pointer now selects d77; staged direct-submodule verification passed. No tag or source-release acceptance was fabricated.
