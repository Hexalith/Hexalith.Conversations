---
title: 'Synchronize dependencies and publish the next Conversations release'
type: 'bugfix'
created: '2026-11-03'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8a48e562a9ecb47de11bcb8b3bcde0c44fab8a01'
context: []
---

<frozen-after-approval reason="user authorized upgrades, pushes and unattended release in the invocation">

## Intent

**Problem:** Current main fails CI because published dependencies lag the source and the active compatibility overlay. The owner requested all existing changes pushed, root submodules synchronized, the latest stable SDK and packages selected, CI repaired, a release created and NuGet publication verified.

**Approach:** Fast-forward clean root submodules to their remote main branches, select current stable dependencies, repair incompatibilities, validate locally and through CI, then dispatch the existing unattended release workflow and verify its official publication endpoints.

## Boundaries & Constraints

**Always:** Preserve the existing context-note edit and include it in the requested push. Validate every exact commit message with pinned commitlint. Keep linear main history. Use producer-generated mapping imports for builds. Verify five declared NuGet packages plus symbols, release source and tag.

**Never:** Initialize nested submodules, load skills from references, overwrite unrelated work, weaken tests, fabricate release qualification or remove enforced GitHub protections. Frozen historical fixture/evidence bytes stay unchanged. Human workflow checkpoints are waived by the invocation; actual runtime/publishing checks remain required.

</frozen-after-approval>

## Code Map

- `global.json`: already pins latest stable SDK 10.0.403, verified against Microsoft's release index on 2026-11-03.
- `.gitmodules`: declares twelve root reference submodules; clean branches can fast-forward without nested updates.
- `Directory.Packages.props`: imports Builds' shared catalog. Current source requires Tenants 7.0.0; select it in the owning Builds catalog, since producer validation rejects consumer overrides.
- `.config/dotnet-tools.json`: select the verified published Builds module CLI 4.43.4, retaining rollForward=false. Its source includes the Tenants 7 release authorities required by package composition.
- `package.json` and `package-lock.json`: audit npm direct dependencies against registry latest; update together if needed, preserving pinned versions and scripts.
- `references/Hexalith.Builds/Github/prepare-module-build/prepare_module_build.py`: public runtime-free package preparation exports verified MSBuild imports through GITHUB_ENV; reuse it for local CI reproduction.
- `tests/compatibility/MinimalModule`: active overlay and successor locks lag EventStore; update to 4.0.4. Preserve original frozen files under tests/fixtures.
- `.github/workflows/ci.yml`: main build, portable/internal conformance, tooling, browser, Windows and fixture lanes must pass.
- `.github/workflows/release.yml`, `.releaserc.json`, `tools/release-packages.json`: existing manually dispatched unattended workflow validates exact green main, publishes using NuGet OIDC and polls packages/symbols/tag/source. Production has branch policy and no reviewer gate.

## Tasks & Acceptance

**Execution:**
- [x] Root references: inspect state, fetch without recursion and fast-forward clean main branches.
- [x] Builds catalog, audit and runtime declaration authorities: select Tenants 7.0.0, qualify the production package authority maps and publish the successor producer. Root package versions continue to come from that catalog.
- [x] Platform declaration snapshots, provenance and tuple: select the exact released EventStore/Tenants/Memories cohort, bind the verified published producer, run tuple CI and release its source identity. The user explicitly approved the required tracked Builds gitlink update without nested checkout initialization.
- [x] `.config/dotnet-tools.json`, npm manifests: select the qualified exact tool and latest compatible stable npm direct packages. Parent owns orchestration.
- [x] `tests/compatibility/MinimalModule`: refresh active overlay and successor locks; tooling investigator owns these files and related Python tests.
- [x] `tests` and `src`: resolve any remaining package API incompatibility; CI investigator owns necessary C# fixes. Prefer dependency alignment when published APIs already match source.
- [ ] Root changes and context note: run focused verification, independent review, commitlint, stage checks and push main with synchronized gitlinks.
- [ ] Existing release workflow: require green exact main, dispatch release and confirm all five published packages and symbols against official endpoints.

**Acceptance Criteria:**
- Given the current workspace, when synchronization finishes, then all root reference main branches match fetched origin/main without loss of local changes or nested initialization.
- Given the official stable SDK and package listings, when dependency selection completes, then all direct packages/tooling are current or a concrete compatibility constraint is documented.
- Given the final pushed SHA, when CI completes, then every required product/repository job succeeds.
- Given green current main and configured publishing credentials, when release runs, then the release tag resolves to that source and all declared exact package versions and symbols are present.

## Implementation Notes

- User explicitly requested unattended execution, push and release. No additional human checkpoint is needed for these authorized operations. Historical evidence-boundary reproduction is not applicable to this routine maintenance task.
- Source investigation identified missing Tenants snapshot APIs in 6.0.0; published 7.0.0 includes both and depends on EventStore 4.0.4. Existing current-source behavior needs no redesign.
- Producer catalog validation rejects consumer version overrides. The provisional root Tenants default was removed; the fix belongs in the directly declared Builds repository. Its catalog and additive release identity map were committed as 088d43b817872d075dd3b592752a052b8ee782da after focused validation, enabling the audit generator to bind real committed catalog bytes.
- Latest Platform development main is untagged and pins an earlier Builds catalog. Completing the coherent release tuple requires a Platform Builds gitlink update and official producer publication. The repository baseline forbids nested gitlink updates without explicit permission; a narrowly scoped exception is pending, with no nested checkout initialization proposed.
- The user approved that exact gitlink update and release through the pending question on 2026-11-03. Continue the complete dependency/release sequence without another permission checkpoint. No nested checkout may be initialized.
- Full npm tooling tests pass 98/98; fresh-cache fixture qualification passes locked restore, Release build and all seven no-skip native assertions. npm direct versions are current and signatures pass (500 signatures, 132 attestations). Bundled undici 6.29.0 in npm 11.21.1 has no compatible automatic fix; npm update and audit-fix dry-run change nothing.

## Spec Change Log

## Review Triage Log

| Layer / finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 1: incompatible untagged tuple | high | Confirmed by PlatformIdentityResolver and producer HXW008/HXW009 results. Completion remains pending the coherent upstream release tasks; no root push or release is claimed. |
| Blind 2: old Platform snapshots | high | Confirmed provenance still selects EventStore 3.129.0, Tenants 5.8.0 and Memories 3.0.12. Add upstream snapshot/provenance refresh to the tuple repair; retain immutable release bytes. |
| Blind 3: installed tool lacks new authorities | high | Official 4.43.3 is built from fc743715f7d0993392dee34b2ef9c4eb85cdd399. A successor publication and pin are required before package runtime qualification. |
| Blind 4: audit absent from gitlink | false | This was an interim review snapshot; the audit is now committed as 3d8fbbb4a131c1541b10bae714383e4d37ad5687. Final parent gitlink is staged only after producer completion. |
| Blind 5: mixed-generation wording | medium | The existing user-authored context note contains both Rebuilding and Unavailable descriptions. It predates this task; preserve it and defer clarification to its owning story. |
| Blind 6: Stale/Redacted omitted | medium | The existing user note's table says Render only but omits supported states. Preserve the pre-existing edit and defer its scope clarification. |
| Blind 7: healthy nonempty index omitted | medium | The existing user note lists only current-empty. Preserve the pre-existing edit and defer the missing context case. |
| Blind 8: entry-qualification summary abbreviated | medium | The user's rewritten context omits earlier explicit gate details while retaining ACTIVE authority. Preserve that edit and defer a reference to the complete owning-story authority. |
| Blind 9: outdated spec Code Map | low | Corrected the non-frozen implementation map to the producer-owned catalog approach. This internal planning correction changes no product behavior. |
| Blind 10: production authorities lack regression | medium | Existing package tests replace production maps with fixture identities. Add a focused released-Tenants package closure test that retains production Tenants entries. |
| Verification 1: production authorities lack regression | medium | Same demonstrated gap as Blind 10; retain this distinct row and implement the shared focused qualification. |
| Edge cases | false | Reviewer returned an empty finding list; no additional issue was asserted. |
| Final blind 1: Platform tag not published | high | Confirmed. This is the pending external publication task, not a completed qualification claim. Tuple CI now passes; finish the catalog matrix, publish the next available tag and fetch it before root package preparation. |
| Final blind 2: npm loader guard not retained | medium | Patched with a permanent real-loader regression covering all configured plugin lifecycles and a deliberate forbidden npm selection. Focused execution passes; the full tooling lane is running. |
| Final blind 3: mixed-generation wording | medium | Carried from Blind 5; the same pre-existing user context bytes remain unchanged. Preserve the existing deferral without adding another. |
| Final blind 4: Stale/Redacted omitted | medium | Carried from Blind 6; the same user-authored table remains unchanged. Preserve the existing deferral. |
| Final blind 5: healthy nonempty case omitted | medium | Carried from Blind 7; the same existing user table remains unchanged. Preserve the existing deferral. |
| Final blind 6: entry-qualification summary | medium | Carried from Blind 8; the same user-authored summary remains unchanged. Preserve the existing deferral. |
| Final blind 7: deferred rebuild/erase conflict | false | The context describes the complete epic. The concurrent Story 16.2 draft explicitly records the user's narrower rebuild/erase deferrals in its dated decisions; the ledger is consistent with that story scope. Maintenance changes no projection lifecycle behavior. |
| Final blind 8: earlier generation timestamp | medium | The public freshness constructor unconditionally rejects generation before last-applied time, while the concurrent story draft requests that same timestamp pair. Defer a representable failure response to its owning Story 16.2; preserve its frozen intent and current release contracts. |
| Final blind 9: canonical-state schema | false | The context is an epic summary, and the draft assigns the new internal replay type and ordinal ordering to one implementation. Its acceptance requires repeatability for the selected implementation, not a shared hash across independently chosen unreleased schemas. No maintenance serializer or schema is introduced. |
| Final blind 10: journal progress wording | false | Earlier observations are retained in the append-only journal; later verified facts and task states record publication and approval. Final completion facts will be appended after root publication, which is still pending. This finding proposes only editing the build spec. |
| Final verification 1: missing Platform tag | high | Same pending publication prerequisite as Final blind 1. Do not run root package preparation or claim completion until the exact consumer identity has its official tag. |
| Final edge cases | false | The second independent edge-case review returned no findings. |
| Repair blind 1: isolated authorization read assertion | medium | Verified the isolated host uses a distinct SDK fake, so inspecting the shared fake does not detect its reads. Patch the current migration to compare the actual isolated client's recorded reads before/after authorization. |
| Repair blind 2: late duplicate request shape | medium | The unconditional SDK page removed the old fixture's scope tripwire. First recovery already asserts the exact scope/range; extend the late-rejection case to retain that protection. |
| Repair blind 3: read-only gateway tripwire | medium | Published fake default command/query results differ from the old throwing double. Retain read-only recovery assertions against SubmittedCommands/SubmittedQueries. |
| Repair blind 4: add HTTP outage coverage | false | No error-handling behavior changed in the module; the published fake exercises retryable failure and persisted denial, and the SDK owns/tests HTTP error translation. The success path now additionally exercises the real HTTP client. A new transport-failure case is not required to preserve the module behavior covered here. |
| Repair blind 5: paginate fixture history | false | The old fixture also returned the complete bounded range. Current domain histories are tiny and fit the requested page size; this migration neither changes production paging nor claims a pagination proof. No reachable fixture truncation or skipped event was demonstrated. |
| Repair blind 6: HTTP fixture disposal | low | Replica disposal could throw before History.Dispose. A direct finally around owned resource cleanup ensures the new client is disposed even on that path. |
| Repair verification | false | Reviewer found no verification gaps after tracing the unchanged boundary tests and fixture coverage. |
| Repair edge cases | false | Reviewer returned an empty finding list. |


## Verification

- Official .NET release index, NuGet flatcontainer indexes and npm registry latest versions.
- `npm ci --ignore-scripts`, `npm audit signatures`, `npm test`.
- Producer-prepared mapping environment: solution Release restore/build and affected native xUnit assemblies; run fixture compatibility with locked restore and full no-skip results.
- `git diff --check`, pinned commitlint against exact message files and `python3 scripts/check-root-submodules.py --repository .` after staging gitlinks.
- GitHub exact-SHA CI results, existing release workflow and `scripts/verify-release-state.py` official endpoint checks.

- Builds source 26991e8c542704be44cd8d9f58084d56fa627adb was pushed to main after 32/32 focused native checks and clean Debug build. CI run: https://github.com/Hexalith/Hexalith.Builds/actions/runs/41623088263. Exact full commit messages, including the two reused upstream PR154 fixes, passed owning pinned commitlint; validation logs are under /tmp/conversations-builds-*.

- Reused Builds’ scoped exclusion for semantic-release’s inactive npm publisher in root package.json/lock. Source inspection and a resolver guard prove configured analyzer/notes/exec/github lifecycles never load it. This resolves the earlier bundled-npm constraint: npm audit now reports zero vulnerabilities; clean install, 317 signatures/58 attestations, 98 tooling tests and seven compatibility assertions pass. Direct dependency pins and all retained dependency records are unchanged. Release planning selects 1.5.0. Logs: /tmp/conversations-config-npm-override-*.log.

- Platform tuple CI now retains the original 153 required cases and adds the seven focused package snapshot/provenance cases (160 total). This ensures the new release-boundary checks execute against the authenticated published Builds DLLs, in addition to the passing local source build. Profile structure/count validation passes; actual published-artifact execution remains pending producer publication.

- Builds v4.43.4 is officially published from 26991e8c542704be44cd8d9f58084d56fa627adb. Both CLI packages are listed on nuget.org; release run 41625705717 and exact-source CI 41623088263 pass. The signed Module CLI, executable version/source, catalog, inventory and all 55 qualified evidence members independently verify. Proof: /tmp/conversations-builds-publication-ixjlknfy.
- Upstream Platform v1.1.4 was published during this run. Rebased the prepared declarations onto current main 0635826905d88049bc8a9531206178e289ebb316, preserved its Works changes, and pushed final source bf101b9d5f19ba741852b0230e526d1f01422022 with the verified artifact binding. Nineteen tuple-control tests pass. Tuple CI 41634849427 requires 160 native cases; catalog CI 41634850883 runs the published SDK matrix. The next planned Platform tag is v1.1.5, subject to destination availability.

### Current verified state — 2026-11-03

The earlier pending observations above are historical journal entries. Builds 4.43.4 is published and independently verified; Platform v1.1.5 points to bf101b9d5f19ba741852b0230e526d1f01422022 after 160 tuple cases and the entire SDK matrix pass. All production release-authority regressions are present and pass. The permanent npm loader guard and full 99-test tooling suite pass, as do all seven compatibility assertions; npm audit reports zero vulnerabilities. SDK 10.0.403 is the current stable SDK across every pinned root. Root source commit a421f5ce1f26137849ebfdcca0125080323ebd7f contains the dependency maintenance. Root package preparation, product CI and Conversations 1.5.0 publication remain pending. Concurrent Story 16.2 planning is local-only user work and is preserved outside these maintenance commits.


### Root CI repairs and explicit publication scope — 2026-11-03

The user created commit 047819fd46d5f4a722e7bacbcd728f87d6c1adfe while maintenance was running. It includes the CI repairs and the concurrent Story 16.2 planning. The user then explicitly authorized including that entire commit in this release, superseding the earlier local-only planning constraint for publication of these files. No user commit was rewritten. Its exact full message passes the repository's pinned commitlint.

The public contract remains byte-identical to the approved 225-type snapshot. Three technical catalog/query plumbing types added after the published 1.4.0 source are internal, with narrow friend-assembly access. The original snapshot, approvals and assertions are unchanged. Unit history fixtures consume the published EventStore fake; the live fixture uses the real SDK gateway client over its shared HTTP handler.

Local package-mode Release solution build passes with zero warnings/errors. All 1,586 Server cases, 297 portable conformance cases, 90 internal conformance cases, and seven live Dapr/Redis cases pass without skips. Logs: /tmp/conversations-root-package-xwh_b9be/final-build.log, final-server-tests.log, final-conformance.log, and /tmp/conversations-sdk-history-live-tests.log. Root release planning remains exactly 1.5.0. Root CI and publication remain pending.


### Hosted checkout repair — 2026-11-03

The authorized combined root commit reached origin/main as 047819fd. CI 41642044482 passed repository checks, Windows cleanup, release tooling and the minimal-module fixture. Hosted conformance exposed two historical proof failures because initialize-build checks out root submodules at depth 1; the required preserved EventStore commit e645901928eed9759e28e1086f23dc96875c3ac3 was absent. The conformance job now fetches complete EventStore main history without recursion or moving its pinned checkout. Assertions and signed proof bytes remain unchanged. YAML parsing, Bash syntax and actionlint pass.

Review assertion patches pass their two affected Server cases and all seven live fixture cases. Authorization inspects its own host's fake, recovery asserts read-only requests and exact duplicate bounds, and history-client disposal survives replica cleanup failure.

Fresh shallow-clone reproduction also passes: the exact workflow fetch makes the preserved EventStore commit reachable from refs/remotes/origin/main and restores its bound DomainProjectionDispatcher.cs tree entry. Proof: /tmp/conversations-shallow-proof-azux0icg/validation.log. No nested submodule or checkout was initialized.


The follow-up hosted conformance run on bf7f01ad fixed the EventStore assertions and then reached a second dependency-history requirement in the same preserved proof: Builds e85a319ecd80f82c3090c49979d4580f07697742. Tracing the complete test establishes exactly three declared remote-containment roots: Builds, EventStore and Tenants (625061bd4858d34263c2deef6a705742ac68ed37). The fetch now covers all three. Fresh depth-1 clones followed by the exact workflow script pass all three original remote-containment requirements and all eight dependency-side historical binding tree entries. Logs: /tmp/conversations-shallow-proof-azux0icg/all-histories.log and /tmp/conversations-all-history-proof-result.txt. YAML, Bash and actionlint checks pass. No assertion, exclusion, signed proof, package selection, checkout or gitlink changed in this follow-up.
