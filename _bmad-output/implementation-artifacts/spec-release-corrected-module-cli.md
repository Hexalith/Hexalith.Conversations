---
title: 'Release and verify the corrected module CLI'
type: 'bugfix'
created: '2026-10-25'
status: 'in-progress'
route: 'dispatch'
baseline_commit: '957df69d50f4ab56c6c924b7a61237338413c21c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — authorized release scope in the invocation">

## Intent

Publish the corrected Hexalith.Builds Module CLI through the existing governed
release workflow and verify the actual feed artifact against the unchanged
Conversations declaration. Prepare the exact artifact handoff for Platform.

## Boundaries & Constraints

**Always:** Preserve concurrent `/pushall` work and all previous observations.
Validate exact commit messages with Builds' pinned commitlint. Require successful
push CI at the exact current release SHA and complete release qualification;
honor protected-environment approval. Let semantic-release choose the version.
Restore from the publication feed into a fresh isolated cache and record all
artifact/source/schema identities. Retain every adoption failure additively.

**Never:** Deploy, edit Platform or Conversations pins/gitlinks, claim new
Platform qualification, weaken release gates, or lift Story 16.1's runtime hold.
Historical local packages cannot substitute for published-artifact measurements.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Correct declaration | Unchanged Conversations manifest | Ordinary human/JSON validation succeeds | Retain streams and exits |
| Invalid keys | Both configuration-key fields, malformed variants | Exact-field HXP002 rejection | A false acceptance blocks completion |
| Adoption | Current tracked Conversations snapshot | Run human/JSON and preserve all failures | Expected incomplete adoption remains explicit |
| Release gate | Current main and exact-SHA CI | Protected full qualification and hashed publisher | Stop on any failed gate |
| Publication identity | Actual signed feed package | NuSpec and informational source match release SHA; loose/embedded/source schema equality | Mismatch blocks handoff |

</frozen-after-approval>

## Code Map

- Builds `docs/module-cli-configuration-key-compatibility.md` and linked
  `evidence/g4/module-configuration-keys-20261025/`: immutable predecessor facts.
- Builds `schemas/hexalith.module-manifest.v2.json`: correction already committed.
- Builds `Tools/test-g4-tool-package-contracts.ps1`: official source, installed
  tool, SDK and malformed-key qualification; preserve its gates.
- Builds `.github/workflows/build-release.yml`, `Tools/verify-g4-release-source.sh`,
  `Tools/verify-g4-tool-release.ps1`, `Tools/publish-g4-tool-packages.ps1`:
  existing governed manual release, protected approval and hashed publication.
- Builds `package.json`, `package-lock.json`, `commitlint.config.mjs`:
  semantic release effects and pinned message validation.
- Platform `.config/dotnet-tools.json`, `hexalith-platform.json`, `README.md`:
  read-only handoff constraints; accepted pin remains 4.35.2.
- Conversations `module/hexalith.module-manifest.v2.json` and prior adoption
  evidence: unchanged inputs and comparison only.

## Tasks & Acceptance

**Execution:**

- [x] Builds `docs/module-cli-configuration-key-release-plan.md`: record authorized
  release scope and create the necessary validated semantic fix commit.
- [ ] Builds release workflow: push, obtain successful exact-SHA CI, dispatch
  protected release and require full qualification and published inventory.
- [ ] Builds new `evidence/g4/module-configuration-key-release-20261025/`:
  retain baseline, commands, CI/release identities and published package probes.
- [ ] Builds new released-artifact handoff document: record exact version,
  content/catalog identities, validation results and all remaining blockers.
- [ ] This spec: record verification and independent review without changing
  historical story records or protected input bytes.

**Acceptance Criteria:**

- Given committed corrected source, when governed release completes, then the
  exact SHA has successful push CI and release-eligible full qualification.
- Given its actual published successor, when restored in isolation, then package,
  source, DLL and schemas are recorded and ordinary validation passes in both modes.
- Given malformed keys and adoption probes, when both formats execute, then
  malformed keys remain rejected and every outstanding adoption failure survives.
- Given the final workspace, when compared with entry inventories, then existing
  observations, manifests, pins, indexed gitlinks and Story 16.1 hold are preserved.

## Implementation Notes

- No unresolved intent questions. Release/push side effects are explicitly
  authorized by the user. The spec records that scope without another approval.
- Entry: Conversations `957df69d50f4ab56c6c924b7a61237338413c21c`, Builds
  `8eb5646fac69c5d3f42257c6bfdcd04158ac50ea`, Platform
  `1b7a00381794bb7ecf40caf643c63c50758a81eb`; all three main trees were clean.
- Scope is a freeform Builds release continuation, not Story 16.1 execution.
  Historical runtime evidence-boundary/final-record gates are not-applicable;
  the governed Builds release inventory is the applicable release gate.
- Entry inventories are retained at
  `/tmp/module-cli-release-20261025-2gbedle5/baseline.json`; copy them into the
  new Builds evidence directory. The new release-plan document is already
  prepared in Builds and not committed. `npm ci --ignore-scripts` installs the
  pinned validator only; do not update dependencies. No commit/push/release has
  occurred in this continuation yet. The caller's explicit release authorization
  overrides the skill's generic no-push/no-remote-operation restriction.

- Progress through 2026-10-25 09:59 UTC: the entry inventory was copied to the
  new Builds evidence directory. Exact initial and successor commit messages
  passed the pinned validator. The initial direct main push at `c81e691` was
  classified as bypass by rule suite 4709439980; this was a deviation and is
  retained explicitly. No release was dispatched from it.
- PR 134 passed required exact-head Codacy, SonarCloud and commitlint checks,
  then normally squash-merged to `36e7e485`; rule suite 4709465424 passed.
  Its exact-SHA push CI 40181805558 was cancelled after separate work advanced
  main. Separate changes to runtime retention/cleanup were preserved, and 372
  release-bound inputs were verified byte-identical.
- PR 135 passed its required exact-head app checks, but concurrent `/pushall`
  merged its branch at `80a810de` before this continuation's normal merge.
  The main-tip precheck failed before the merge API call. Rule suite 4709538966
  reports bypass because merge commits are disallowed; required statuses passed.
- Latest observed Builds remote/local main is
  `a9013c541a3cb8e127d84a3723966dfe11eeab82`; push CI 40182696227 is queued.
  The previous `c81e691`, `36e7e485`, `0ade5ec` and `80a810de` push CI runs
  are cancelled, not successful full runs. Await confirmation that concurrent
  `/pushall` has stopped before further remote mutations or source stabilization.
- Conversations was externally committed to
  `3fa50842227ecbbe68a77f10ce68743ac000e26a`, changing its indexed Builds
  gitlink from entry to `657bcfaa0addd4f8ca94448bc3d8b9eed5193a00`.
  Platform checkout is externally advanced to
  `b9cf0e78c42ca8bfe1aa5488cc4340c238887acb`, with an externally changed
  indexed gitlink. These changes were not made by this continuation and are
  preserved rather than reset. The protected file bytes and old observations
  still match entry. The final indexed-gitlink preservation criterion is not
  claimed against the entry snapshot.
- Local-only progress document:
  `references/Hexalith.Builds/docs/module-cli-configuration-key-release-progress.md`.
  Timestamped progress/preservation observations and prepared feed harness are
  under `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/`.
  The harness is syntax checked only. No published-artifact measurement,
  release-eligible inventory or actual Platform artifact handoff is claimed.

- Resumed after the user's instruction to continue. Fresh entry inventories at
  `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/baseline.json.gz`
  bind Conversations `8444207001d6ad3cd197c2ffba519317effa8b37`, Builds
  `142b06e6d05c94b55ac35e587450c35fcac87590`, and Platform
  `b9cf0e78c42ca8bfe1aa5488cc4340c238887acb`, all initially clean.
- PR 136 exact-head required app checks passed after preserving an inventory
  representation correction: Sonar first reported 509 `json:S6418` findings on
  hash values under auth/token-shaped path keys; original bytes remain in gzip,
  with equivalent labelled path/sha256 rows and no gate change. Normal squash
  source `7144b8031b95a65982d78b3eb381e10c00b55cbe` passed rule suite
  4709709500. Exact-source push CI 40184369318 was later cancelled after 34
  completed steps; no complete push CI success or release credit is claimed.
- At 10:44:01 UTC, separate `/pushall` work advanced remote main to
  `6e40ea5f6243391bea80084b8ddd695df58fb1ff` (19 runtime/test/README files);
  rule suite 4709861214 reports bypass and push CI 40185980339 started.
  Complete Git-tree comparison found all 372 release-bound input blobs unchanged.
  Old CI logs retain only the cancellation error, with no recorded test failure.
  Remote writes are held pending explicit confirmation that other Builds writers
  are paused through CI and protected release. No release/feed step has run.
- Latest resumed progress and preservation observations:
  `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/progress-at-supersession-20261025T104747Z.json` and
  `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/preservation-at-supersession-20261025T104747Z.json`.
  Local Builds checkout remains 7144b803; incoming remote work is not reset.

- The latest 372-input equality proof covers only the module-key schema and
  workflow/qualification/publisher/fixture/catalog/lock/SDK/attribute inputs.
  External source 6e40ea5 also changes the compiled
  `ModuleRunEvidenceArtifactValidator.cs` policy by dropping removed-disposition
  and empty-remaining-resource requirements for completed selected-test evidence.
  Its exact patch is retained at
  `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/external-runtime-validator-delta-20261025T105102Z.json`.
  This continuation does not edit or independently qualify that runtime policy;
  no Platform or runtime qualification credit is claimed.

- The user explicitly confirmed other Builds writes are paused and instructed
  continuation. Rechecked current main at
  `6e40ea5f6243391bea80084b8ddd695df58fb1ff` with push CI 40185980339
  running. Safely fast-forwarded local Builds without source edits. The existing
  governed source guard requires exact current allowed branch and successful
  exact-SHA push CI, not a new source-only PR after every incoming baseline.
  Existing committed fix/feature work provides the release effect, so no new
  semantic trigger or CI-resetting commit is needed. Preserve and disclose
  external rule suite 4709861214 as bypass without claiming historical branch
  compliance; this continuation changes or bypasses no rule. Full current-source
  qualification remains mandatory. Decision evidence is
  `references/Hexalith.Builds/evidence/g4/module-configuration-key-release-20261025/resumed-20261025T101341Z/stable-source-decision-20261025T105248Z.json`.

- Source advanced through normal PR137 at 11:13:56 UTC to
  `68e65f2023eca117a2aa08a022ee33d8f4c34a44`; rule suite 4710053318 passed.
  It changes only CompositionEngine.cs to release the provisioning lock and
  remove pre-start state/diagnostics after an HXW006 refusal. Earlier current
  CI40185980339 was cancelled by supersession; its full metadata and logs are
  retained. Local Builds safely fast-forwarded and current push CI40187992999
  is running. Existing writer-pause authorization continues; no extra source-only
  PR, trigger commit or permission request is required by the governed route.
- Old PR135/136 full CI logs were retained: each had one failure in
  `WorkspaceCommandModeTests.DescriptorModuleProjectOutsideMappingStopsEngineAsync`
  at line1151 (2097 total,2093 succeeded,1 failed,3 Platform-context skips).
  Incoming PR137 addresses the same refusal-state defect, subject to successful
  current-source CI and full qualification. No obsolete release PR CI remained
  active, so routine cleanup required no cancellation or gate change.

## Spec Change Log

- 2026-10-25: recorded implementation progress and the concurrent-source blocker;
  frozen intent, protected input bytes and historical story records are unchanged.

## Review Triage Log

## Verification

- Pinned `node_modules/.bin/commitlint --config commitlint.config.mjs --edit`:
  every exact candidate used by this continuation passed; raw streams and message
  hashes are retained in the new Builds evidence directory.
- Required PR checks and merge governance: PR 134 normal squash passed rule suite
  4709465424. PR 135 required app checks passed but external `/pushall` consumed
  its branch through a disallowed merge commit; the local tip assertion stopped
  the planned merge API call. Both bypass observations remain explicit.
- Entry preservation: protected Conversations manifest/tool pin, Platform tool
  pin/range, Story 16.1 spec/sprint bytes, 19 prior Conversations observation
  files and 53 Builds predecessor files match entry. External checkout/content
  and indexed-gitlink advances are recorded separately in timestamped snapshots.
- Exact-current-SHA successful full push CI: pending. The resumed normal-source
  run 40184369318 was cancelled after external supersession; current remote
  source 68e65f2 has run 40187992999 in progress; 6e40ea5 was also superseded
  and cancelled. Cancelled runs provide no
  complete success credit.
- Protected release dispatch/approval, full qualification, semantic-release version
  selection, publication/inventory and fresh feed installation: not run.
- Published CLI ordinary/adoption human and JSON probes, invalid-key controls and
  actual artifact/source/DLL/schema/catalog handoff: not run. Prepared harness
  passed Python syntax compilation only and was not run on a substitute package.
- Independent review: pending; implementation is incomplete and remains in-progress.
