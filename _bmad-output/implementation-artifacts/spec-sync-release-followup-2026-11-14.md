---
title: 'Complete current dependency synchronization and Conversations release'
type: 'bugfix'
created: '2026-11-14'
status: 'in-review'
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
- `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs`: linked into the portable test project. Legacy generic by-reference parameter names embed CLR assembly versions; render their element types recursively and normalize only legacy baseline parameter type slots in memory, with exact contract assembly identity and drift refusal controls. Preserve both frozen snapshot files.
- `.github/workflows/release.yml`: manual dispatch requires exact green main, production environment and current semantic plan; trusted NuGet OIDC publication plus post-publication verification are already implemented. Repository variable enables publication; production currently has no human reviewer requirement.
- `scripts/verify-release-source.py`, `scripts/verify-release-state.py`, `tools/release-packages.json`: authoritative exact-source and five-package/symbol checks. Keep destination collision refusal and symbol/source validation.
- Earlier `spec-sync-and-release-2026-11-14.md`: retained implementation context, previous results and incomplete release; do not rewrite its frozen intent or historical findings.

## Tasks & Acceptance

**Execution:**
- [x] Root dependency gitlinks -- inspect and fast-forward only the declared clean direct repositories, verify remote availability and no nested initialization.
- [x] SDK, catalog and npm pins -- compare official current versions and update supported active pins if needed; record compatibility constraints.
- [x] Current candidate -- run focused tooling, whitespace and root-submodule checks; qualify producer package preparation on the committed identity and inspect actual diff.
- [ ] CI fixes -- repair concrete failing behavior, preserve automated checks, and pass focused validation plus independent review before final push.
- [ ] Root and any owning dependency -- validate exact complete commit messages, retain logs, commit and push all authorized content and synchronized pointers.
- [ ] Release -- achieve green exact-source CI, dispatch existing release workflow without human validation pauses, and independently verify all five NuGet packages, symbols, version/tag and source.

**Acceptance Criteria:**
- Given official release metadata and current registry indexes, when active SDK and dependency pins are audited, then they are latest stable or a demonstrated compatibility requirement is recorded.
- Given the synchronized committed source, when producer preparation and full CI run, then every blocking lane succeeds without reducing assertions or skips.
- Given green exact-main CI and authorization, when Release runs, then official NuGet publication and GitHub assets match the exact released source for all five declared packages and symbols.

## Implementation Notes

The user authorized the dependency updates, commits, pushes and release, including bypass of human validation. The user narrowed CI repairs to Conversations and the dependencies required for its release. Automated build, integrity and publication gates remain enabled. Historical evidence-boundary reproduction is not part of this routine maintenance.

All twelve direct repositories began clean on main. Six had newer upstream source; the selected EventStore, Projects, Folders, Tenants and FrontComposer revisions were fast-forwarded, while Platform required a qualified source successor. All selected revisions are available remotely. All 118 nested declarations remain uninitialized.

Official release metadata confirms installed SDK `11.0.100` is the latest stable .NET 11 SDK. All seven direct npm pins and other consumed stable NuGet versions were current. Tenants was the stale consumed family, moving from `7.0.7` to `8.0.0`. The compatible Dapr integration remains `13.6.2-beta.959`; its version 17 beta requires an Aspire 17 preview graph.

Builds now publishes `6.0.3` at `99f7c86072ca7b35e3fbbaee4606a559a847805a`. Its catalog defaults to Tenants 8 while preserving the finite SDK 4.2 consumer cohort and explicit selections. The current default is the first XML property, so supported unmaterialized readers also observe version 8. Historical cold-start declaration fixtures explicitly select their accepted Tenants 7 catalog; their declarations and refusal assertions remain unchanged. The incremental audit changes only the five Tenants rows and family decision, preserving 156 other families. Catalog SHA-256 is `01219d064fc1c375879312f543aa649c6aaef344be054cf4c80571825ca59b41` for the committed LF Git blob; the CRLF worktree has a distinct digest.

Builds validation passed 249 catalog probes, 17 authority controls, the 317-package/157-family audit, 27 initial native cases, 6 focused review regressions and all 24 affected declaration tests. Debug compilation had zero warnings/errors. Exact-source CI `43625641362` and normal publisher `43628367692` succeeded. Both Module and Evidence 6.0.3 packages are listed and downloadable. Independent verification authenticated the original release/run/artifact, manifest, signed NuGet payload, catalog and Tooling: release `445516831`, artifact `14127172569`, SHA-256 `bd0d5cd4d2ef6a5ae5f17a8169d31d5a0c43f5985670f924a56f2f3a16a5f4ff`. Initial registry propagation 404s remain in separate logs.

Platform genuinely publishes `v2.1.4` at `9ccbd4c7f8f3b6143e7b4f7b20a0e192de932ac1`, with shared source `9832b65ea2666aa1081b108e20c1ac2d18f960ab`. It adopts the authenticated Builds 6.0.3 binding, exact source/range/catalog and all five Tenants pins, while preserving historical admissions and public provenance. The production probe now evaluates the real Composition project's property instead of assuming there is one physical Tenants XML property. Fresh remote companion commits are legacy `f7a8075a80a496c3bb9f4872d5b7d8bcf5bc3d06`, pre-extension `0f24819a379e312d8cebd80d7c8de2d243f65e95` and reader `b7cf5ec9dec861ed396966482c5e4cb2d052531f`.

Platform local validation passed 81 Python, 28 native, 25 source-propagation and 52 descriptor controls, plus three actual-source comparisons. Hosted tuple `43632289434` passed all five jobs, including current 478/97/9 and historical 412/349 inventories; catalog `43632289366` passed all six rows. Normal publisher `43636012066` created release `445560428`. Downloaded artifacts matched official digests and receipts had no failures, errors, skips or unrun cases. The main push reported bypassing the pull-request requirement; this was reported to the user under their explicit instruction. Native, candidate and Story acceptance flags remain false: package qualification does not assert runtime/deployment acceptance.

Root source `523a1a5ac7e2646b35c32ea53ef735ee671abc6c` aligns the final producer pointers and module tool 6.0.3. Its exact full message passed pinned commitlint. Root tooling 100/100, compatibility 7/7, Windows checkout 4/4, whitespace and index-aware submodule checks passed. Fresh fetches verified every owner clean, selected HEAD available remotely and zero ahead/behind. Strict package preparation passed on the committed identity in Release mode, mapping hash `a530e0cf0e5f0c11b76b79bf575fec99d41492351ab703c4a028ac9ee2c7bb13`.

Hosted root run `43636569003` passed every active lane except two portable snapshot comparisons; the full build, unit, integration and Aspire checks passed. CodeQL `43636569193` and commit policy `43636568853` also passed. The portable lane ran 296 cases and exposed CLR-version-qualified generic by-reference names in the frozen snapshots. The repair renders semantic element types recursively and normalizes only legacy parameter slots in memory. Both immutable baselines remain byte-identical. Initial mapped-source Debug validation passed all 28 snapshot-class cases with zero warnings, errors or skips. Independent review identified the live assembly-identity and nested-type/array edge cases recorded below. All corrections now pass 39/39 focused tests with zero warnings, errors, skips or unrun cases. The first review patch exposed an overly broad check of plain System.Uri&; its failed 38-case receipt is retained separately, and the final guard preserves simple byref spellings. Tested source SHA-256 is `ece287d366d87fc8753d075a468eaf76659bd79b33314a41f7d1ddb6097ed075`. Exact-source hosted requalification remains required.

The initial semantic release dry run resolved to `1.6.1`. Inspection of the actual `v1.6.0` tag then confirmed its packages target `net10.0`, while existing commit `bba2023039065db0fb0a799d64157999514f5d93` retargets current packages to `net11.0`. The README now states this runtime requirement and preserves wire schema v1. A validated breaking-change commit records the framework compatibility break so the planned publication is `2.0.0`. Release and independent verification of all five official packages and symbol archives remain pending. Useful command logs, exact source hashes, XML receipts, bindings and publication checks are retained under `/home/administrator/.codex/task-logs/conversations-release-followup-20261114`.

## Spec Change Log

- Release compatibility inspection found the previously committed target-framework change requires a major package version. Document the .NET 11 requirement and encode the breaking change in Conventional Commit metadata; preserve all current source, wire contracts and frozen evidence.

- Root CI at `523a1a5ac7e2646b35c32ea53ef735ee671abc6c` exposed runtime-version-dependent generic by-reference type names in the snapshot probe. Added the precise repair location to the code map. Keep immutable baseline bytes, full member comparisons and refusal of custom assembly/type/ref/parameter drift; do not regenerate accepted evidence.

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


- Root blind1 — medium, patch: the live formatter can erase a foreign generic argument assembly identity. The isolated runtime probe produces the genuine baseline spelling from a foreign same-name type. Preserve or validate actual argument assemblies and add a live negative control.
- Root blind2 — low, patch: malformed version/token strings can throw FileLoadException outside the intended unchanged-name branch. The gate already fails closed; handle this known resolution failure for consistent comparison diagnostics and test it.
- Root blind3 — low, patch: generic/nullable arrays resolve but fail the immediate IsGenericType guard. No current frozen entry has this shape, but the renderer now explicitly covers arrays; remove the unnecessary guard and test the paired comparison.
- Root blind4 — medium, patch: recursive byref rendering exposes the existing nested generic name truncation. Reproduced KeyCollection/ValueCollection spellings collide. Preserve both nested names with a focused negative control.
- Root blind5 — false: the current Contracts assembly and both frozen baselines contain no generic contract root definitions. The scratch probe invents such a root in a substitute assembly. This private legacy adapter need not admit hypothetical new historical baseline shapes; unknown roots continue to fail the exact comparison.
- Root blind6 — low, rejected: the implementation notes contain compressed prose, but the proposed change edits this build's spec, which the review route explicitly excludes as a code finding. Completion reporting will use readable prose.
- Root edge1 — medium, patch: independently reproduces the KeyCollection/ValueCollection collision; same correction as blind4, retaining declaring/nested names.
- Root edge2 — low, patch: independently reproduces the generic-array normalization gap; same direct guard correction and paired regression as blind3.
- Root edge3 — false: same invented generic Contracts root as blind5. Neither actual baseline nor current Contracts has a reachable generic root, and refusal keeps unknown legacy shapes unequal.
- Root verification — no findings: focused tests cover the changed behavior and the source remains linked into the portable CI project. Full exact-source hosted qualification remains required after the corrections.

## Verification

- Wrapped focused repository and release-tooling tests; root submodule checker and whitespace check must pass.
- Supported exact CLI prepare-build must accept the committed candidate and export verified environment imports.
- Final exact-source GitHub CI, CodeQL and commit policy must succeed.
- Existing Release verification plus independent official package/symbol endpoint checks must pass.
