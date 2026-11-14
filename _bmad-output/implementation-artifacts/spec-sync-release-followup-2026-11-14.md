---
title: 'Complete current dependency synchronization and Conversations release'
type: 'bugfix'
created: '2026-11-14'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'e06ce67da68d655970727606aba92ac0c8704e07'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="user explicitly authorizes dependency updates, pushes, release and bypass of human validation">

## Intent

**Problem:** Current Conversations main fails CI before compilation because its Builds and Platform source selections disagree. The previous dependency maintenance left a release unfinished, and six direct dependency repositories have newer published main commits.

**Approach:** Synchronize the root-declared dependencies, retain or update the latest stable .NET 11 and consumed package/tool versions using their owning catalog, repair actual automated failures, push verified changes, release through the existing trusted-publishing workflow and verify the exact five packages and symbols.

## Boundaries & Constraints

**Always:** Preserve user work, exact source identities, immutable historical evidence and frozen fixtures. Follow owning repository guidance and pinned commitlint. Use run-with-temp for disposable builds/checkouts and save useful logs under /home/administrator/.codex/task-logs/conversations-release-followup-20261114. The user's explicit instruction satisfies human approval checkpoints and authorizes the necessary synchronization, fixes, commits, pushes and release. Automated build, integrity and publication checks remain required. Local compilation uses mapped source/Debug; package/Release verification uses an isolated CI-equivalent job. Work remains on main.

**Scope decision:** User explicitly confirmed CI repairs cover Conversations and its release dependencies; independent workflows in the other direct submodule repositories are outside the repair scope.

**Never:** Initialize nested submodules, use recursive or remote submodule updates, force-push, fabricate release tags or authority, weaken product tests to conceal a failure, rewrite signed evidence or create a manual Release that bypasses package publication. Do not claim runtime/deployment qualification from package acceptance.

</frozen-after-approval>

## Code Map

- `.gitmodules`: twelve root-declared dependencies; all initially clean and on main. Fetch without recursive submodule operations. Inspect then fast-forward EventStore, Projects, Folders, Tenants, FrontComposer and Platform. Other direct dependencies currently equal remote main.
- `global.json`: stable SDK 11.0.100 is installed and matches official November 10 release metadata; net11.0 is already active. SDK 10.0.404 is retained for historical tooling where required.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: owner of centralized versions; inspect actual used packages and intentional named consumer cohorts before updating. `.config/dotnet-tools.json` pins published CLI 6.0.2. Preserve compatible package-mode imports and producer identity verification.
- `references/Hexalith.Platform`: latest main 50fb0462924a765e38d830e42af242ccfaf5982f pins the same Builds 12475add3f14b06709a5ffe74f413d27d4111d75 as root. Current root Platform d77f741d11bf01352a11fa6df2f1a14313c1328a still pins 60ce7967607dc8b703fff11f4fc13cabb09c090a. This causes HXW009, plus secondary shallow-history HXW007, in CI 43618918082.
- `references/Hexalith.Builds/Github/prepare-module-build/prepare_module_build.py`: supported package environment preparation; reads committed HEAD identity, requiring a committed candidate for honest qualification. Use temporary candidate commits only in disposable checkouts when necessary.
- `package.json`, `package-lock.json`: pinned release tooling; verify official npm versions and signatures and update only when newer compatible releases exist.
- `.github/workflows/ci.yml`: full package-mode build, per-project tests, real Dapr/Aspire checks, Windows cleanup, browser, conformance and tooling lanes. Reuse these automated checks after focused local verification.
- `.github/workflows/release.yml`: manual dispatch requires exact green main, production environment and current semantic plan; trusted NuGet OIDC publication plus post-publication verification are already implemented. Repository variable enables publication; production currently has no human reviewer requirement.
- `scripts/verify-release-source.py`, `scripts/verify-release-state.py`, `tools/release-packages.json`: authoritative exact-source and five-package/symbol checks. Keep destination collision refusal and symbol/source validation.
- Earlier `spec-sync-and-release-2026-11-14.md`: retained implementation context, previous results and incomplete release; do not rewrite its frozen intent or historical findings.

## Tasks & Acceptance

**Execution:**
- [x] Root dependency gitlinks -- inspect and fast-forward only the declared clean direct repositories, verify remote availability and no nested initialization.
- [x] SDK, catalog and npm pins -- compare official current versions and update supported active pins if needed; record compatibility constraints.
- [ ] Current candidate -- run focused tooling, whitespace and root-submodule checks; qualify producer package preparation on the committed identity and inspect actual diff.
- [ ] CI fixes -- repair concrete failing behavior, preserve automated checks, and pass focused validation plus independent review before final push.
- [ ] Root and any owning dependency -- validate exact complete commit messages, retain logs, commit and push all authorized content and synchronized pointers.
- [ ] Release -- achieve green exact-source CI, dispatch existing release workflow without human validation pauses, and independently verify all five NuGet packages, symbols, version/tag and source.

**Acceptance Criteria:**
- Given official release metadata and current registry indexes, when active SDK and dependency pins are audited, then they are latest stable or a demonstrated compatibility requirement is recorded.
- Given the synchronized committed source, when producer preparation and full CI run, then every blocking lane succeeds without reducing assertions or skips.
- Given green exact-main CI and authorization, when Release runs, then official NuGet publication and GitHub assets match the exact released source for all five declared packages and symbols.

## Implementation Notes

The implementation handoff performs the direct dependency synchronization and focused local qualification first, then reports to the parent, which owns hosted CI, release and final publication orchestration.

User requested bypass of human validation; no additional approval is needed. Historical evidence-boundary reproduction is not applicable to this routine maintenance. External effects are authorized but depend on passing automated checks. Initial root baseline and all direct repositories are clean.

The official registry audit found one stale consumed family: Tenants7.0.7, latest8.0.0. All seven npm pins and other consumed stable NuGet versions are current. Aspire Dapr13.6.2-beta.959 is the latest compatible13.x integration;17 beta requires an Aspire17 preview graph and is not selected.

Latest Platform50fb still binds the genuine Builds6.0.2 publication at60ce while its sync pointer moved to12475add. Its hosted tuple lane fails its exact gitlink equality guard and no genuine source release exists. Repair requires a genuine Builds successor for the Tenants update, authenticated Platform binding and exact admission controls, fresh immutable historical companions, then normal full hosted qualification and source publication.

The Builds catalog patch preserves7.0.7 for the same finite SDK4.2 consumer identities and explicit Tenants selections. Its249 positive/negative catalog probes and17 authority controls pass. The affected native test project builds Debug with zero warnings/errors. First relocated-output native execution could not locate the repository root; this environmental failure is recorded separately and tests are rerunning in a private clone with repository-contained outputs.

Root synchronization completed for all six advanced direct owners; every selected revision is available remotely. All118 nested declarations remain uninitialized. Root tooling100/100, compatibility7/7, Windows checkout4/4, index-aware submodule and whitespace checks pass.

Final source and audit committed in Builds as b8f67c392a165887119543113e1f0aa0b0218f0d and6de5ce0a7194321828a1ede119a98c5d31d05914. Exact complete messages passed pinned commitlint. Both are pushed on main; no protected-branch bypass was printed. Final catalog249/authority17 and generated audit317packages/157families pass. Initial27 native cases passed; final6 focused cases also pass without failures/skips/errors, with zero-warning Debug build and tested-file hashes equal committed source. Audit changes only the five Tenants current rows and family decision while preserving156other families.

A further real-catalog cross-check reproduced8/8 old cold-start fixture failures: its SDK4.1/4.2 and Memories3 fixtures declared accepted Tenants7.0.7 but imported generic8.0.0. Added one ordinary explicitTenants7.0.7 property inside the existing historical !consumerViews fixture branch; declaration bytes and all refusal assertions remain unchanged. Full affected class24/24 passes with zero failures/skips/errors and clean Debug build. Exact message passed pinned CLI; test-only source99f7c860 changes no catalog/declaration bytes. The resulting final source needs a fresh exact CI run before release. The initial hosted Python3.12/runner, workflow lint/API and both sample platform lanes are green.

Platform review patches now observe and verify the exact five Tenants package items plus the property in Platform default/collecting, unknown and owner contexts. Three focused retained controls pass, as do committed-source component checks for36default/36collecting/7owner observations and120mutated refusals. These component results grant no publication or candidate acceptance;603 binding/fixtures and complete native qualification remain pending genuine publication. Existing native inventories are unchanged.

Exact-source Builds CI43625641362 is fully green at99f7c86072ca7b35e3fbbaee4606a559a847805a, including full.NET tests, both Python interpreters, sampleLinux/Windows, API/workflow checks and the complete package catalog/audit/exception/Dapr contracts. Current main and clean owner tree were checked before dispatching normal builds-release.yml for the expected6.0.3 patch. No human review pause is configured; automated publication remains required.

Builds6.0.3 is genuinely published by43628367692 at source99f7. OfficialModule/Evidence indexes list603 and both blobs return200 as of19:12UTC. Independent signedModule and original artifact/manifest/catalog/Tooling verification passed; release445516831 binds originalartifact14127172569 SHA256bd0d5cd4d2ef6a5ae5f17a8169d31d5a0c43f5985670f924a56f2f3a16a5f4ff. InitialNuGet404 propagation observations remain in separate logs.

Root module tool now pins603; wrapped roottooling100/100 and compatibility7/7 pass. Parent verified Platform activebinding and offlineJSON exactly equal authenticatedbinding; rawcatalogfixture equals the exact99f7Gitblob/01219hash. Platform preserved intervening upstream7f33changes beforeadoption. ActualPython81passed, sourceprop25passed, initialnative27/28found a production-closure probe's physicalSingle-Tenants-property assumption. The test now queries the real Composition project's evaluatedproperty against the exactexportedcatalog, preserving all signedServer707/assets/audit/output assertions. Narrowfailedcase passes1/1. Reviewedfinal source9832b65ea2666aa1081b108e20c1ac2d18f960ab supersedes266f; freshcompaniontrees and full28/source25requalification are in progress, before finalmainpush.

Platform finalmain9ccbd4c7f8f3b6143e7b4f7b20a0e192de932ac1 is pushed and remotelyverified. Sharedsource9832; freshremoteverifiedcompanionslegacyf7a8075a80a496c3bb9f4872d5b7d8bcf5bc3d06, pre-extension0f24819a379e312d8cebd80d7c8de2d243f65e95, readerb7cf5ec9dec861ed396966482c5e4cb2d052531f. Python81, finalnative28/28, finalsourceprop25/25, descriptor52 andactualthree-sourcecomparisons pass. GitHub printed “Bypassed rule violations for refs/heads/main” and “Changes must be made through a pull request”; reported to user under their explicit bypass instruction. Automated gates remain unchanged. Allsixcatalogmatrixrows in43632289366 aregreen; tuple43632289434 currentand412/349historical executions are stillrunning. Root must await its automatic genuine source tag before package preparation/push.

ExactPlatform9cc tuple43632289434 succeeded allfivejobs, including current478/97/9, historical349/412 andaggregate verdict; catalog43632289366 succeeded allsixrows. Normal source-release43636012066 is inprogress. Officialcurrentartifact14129828181 is598307535bytes withSHA256a1ff740d6c2b4aa02d45aeb661f00801ea4c21098d5ef82997ffac3d85ef2585. These passed hosted qualifications supersede the earlier pending status; rootpackagepreparation still waits for the genuine automatic tag.

GenuinePlatformv2.1.4 is published by successfulnormal43636012066; release445560428/tag peels exactlyto9ccbd4c7f8f3b6143e7b4f7b20a0e192de932ac1 andbinds successful43632289434/43632289366attempt1. Freshfetchofrootandall12directowners confirms0ahead/0behind, everyselectedsubmoduleheadavailableonorigin/main, andeachownerworktreeclean. RootalonecontainsthisplannedCLI/pointer/specchange. ModuleCLIrestore/--version independentlyreturns6.0.3+99f7c86072ca7b35e3fbbaee4606a559a847805a. Rootmetadata candidateisreadyforindexchecks/localcommit; strictpackagepreparation follows the committedidentity beforepush.

## Spec Change Log

## Review Triage Log

- Platform blind1 — medium, planned patch: the two referenced603 fixtures are absent and the new tests currently fail before verification. They must be created only from the genuine published binding/catalog, which is already pending Builds CI and release. No fake publication data is accepted.
- Platform blind2 — medium, patch: default/collecting Platform Tenants observations do not reject wrong property values. Add the corresponding expected7/8 check alongside the new family verification and fault controls.
- Platform blind3 — medium, patch: the evaluator records no actual Tenants PackageVersion items, so property-only checks cannot corroborate their complete family. Record and validate the five exact items and reject missing, duplicate or mismatched pins, preserving prior6.0.2 expectations.
- Platform blind4 — false: explicitSDK5.1 Platform selection is already executed by Builds OwningHostViewPreservesExplicitHistoricalSdkSelectionAsync for Platform.EventStoreHost, with the materialized Tenants8 assertion, plus the evaluated selector probes. Current Platform default/collecting contexts themselves deliberately select SDK4.2; another Python-only duplicate is not a demonstrated new gap.
- Platform blind5 — false: the existing CurrentPrivateDependencyVersionsRemainExact real fixture invokes AssertCurrentConsumerCatalog, which checks actual current source/binding and then PlatformDeclarationSet.Load, all four server owner versions, MCP resolution and actual cohort selection together. That existing positive path runs with the active genuine603 adoption in the planned28-case native qualification; the extra selector loop is not its sole acceptance proof.
- Platform edge1 — medium, planned patch: independently confirms the two603 fixture files are missing. Same genuine-publication dependency and required creation/validation as blind1.
- Platform verification — no findings: current tuple CI selects both changed native classes(17+3cases) under NET11/SDK4.2; Python suite imports discover the catalog checks. Genuine fixture and active tuple qualification remain pending.

- Builds blind1 — medium, planned patch: the revision-bound audit still contains7.0.7 and its old catalog digest. Refresh from committed reviewed source through the official generator before push/release; the currently stale result is not accepted evidence.
- Builds blind2 — medium, patch: supported unmaterialized XML lookup reads the first Tenants property and would return7.0.7. Put the current default first with the complementary non-retained condition and cover real-catalog unmaterialized reads; preserve finite consumer selection and public APIs.
- Builds blind3 — false: these existing historical-SDK metadata tests assert explicit selection preservation, not successful restore or runtime acceptance. Explicit incompatible SDK/family combinations remain subject to NuGet dependency refusal. The finite automatically retained SDK4.2 cohort is covered independently; no new historical runtime acceptance is claimed.
- Builds blind4 — medium, patch: command-line global properties cannot prove preservation of ordinary pre-import values. Add real materialized ordinary Tenants override coverage analogous to the existing Memories fixture.
- Builds edge1 — medium, patch: same independently confirmed first-property fallback defect as blind2; apply the default-first condition and unmaterialized regression.
- Builds verification1 — medium, patch: mutation evidence proves the global-property-only Tenants probes miss an overwritten ordinary property. Add a focused pinned-catalog snapshot regression while preserving existing checks.

## Verification

- Wrapped focused repository and release-tooling tests; root submodule checker and whitespace check must pass.
- Supported exact CLI prepare-build must accept the committed candidate and export verified environment imports.
- Final exact-source GitHub CI, CodeQL and commit policy must succeed.
- Existing Release verification plus independent official package/symbol endpoint checks must pass.
