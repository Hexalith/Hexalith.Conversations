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

- [ ] Builds `docs/module-cli-configuration-key-release-plan.md`: record authorized
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

## Spec Change Log

## Review Triage Log

## Verification

- Pinned `node_modules/.bin/commitlint --config commitlint.config.mjs --edit`:
  success for every exact candidate before commit.
- Governed exact-SHA CI and full protected release: success, releaseEligible true.
- Fresh feed install: version/source identities and byte hashes match the release.
- Published CLI ordinary/adoption human and JSON probes plus invalid-key controls:
  expected exit, diagnostic and preservation checks with retained raw streams.
