---
title: 'Mechanize Epic 5 final-record verification'
type: 'chore'
created: '2026-07-14'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'c029b34e1848e6afaf7ac2f5dedd54357229e25c'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-epic-5-final-record-check.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 5 reviews repeatedly corrected stale test counts, incomplete File Lists, evidence-boundary drift, and gitlink mistakes because no single mechanical check derived the completion record from the final tree.

**Approach:** Add a reusable PowerShell gate with live-working-tree and historical-commit modes, bind it to normalized test results and a non-mutating full contract-shape comparison, then issue an auditable Story 5.1–5.3 report and close the existing workflow action.

## Boundaries & Constraints

**Always:** Include tracked, staged, baseline-relative committed, and untracked non-ignored paths; compare exact normalized repo-relative paths; freeze pre-existing status, content hashes, and gitlink commits; fail when frozen state changes; bind counts to the executable/test inputs exercised; keep JSON authoritative over Markdown; preserve signed evidence; report historical proof as record consistency rather than reconstructed working-tree proof.

**Ask First:** Any newly discovered historical discrepancy; any non-empty public-contract diff; editing hash-bound/signed evidence; changing runtime/public API/package/AppHost behavior; modifying a submodule, gitlink, or unrelated pre-existing file. The already-disclosed Story 5.2 `test-summary.md` omission may receive a dated factual amendment.

**Never:** Reset, clean, checkout, or recursively initialize submodules; use wildcard/path-only dirty-tree exclusions; overwrite the contract baseline during comparison; weaken/skip tests; claim today’s tree was a historical story tree; mark the sprint action done on an unexplained failure.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Live pass | Final tree, frozen baseline, green TRX, exact claims | Versioned passing JSON/Markdown with exact counts, paths, evidence hashes, and empty shape diff | Exit 0 |
| Record drift | Stale count or missing/extra File List path | Name every mismatched source/path | Exit non-zero |
| Frozen dirt | Pre-existing file/gitlink is byte-for-byte unchanged | Report and exclude it from work-item changes | Fail if status/hash/commit changes |
| Contract drift | Regenerated full shape differs or baseline changed | Report non-empty/unapproved state | Exit non-zero; ask first |
| Historical audit | Recorded baseline/final commits and artifacts | Verify commit path set, count claims, hashes, and contract-baseline stability | Label limits; never infer former uncommitted state |

</frozen-after-approval>

## Code Map

- `tests/Test-StoryFinalRecord.ps1` -- reusable fail-closed live/historical checker and JSON/Markdown renderer.
- `tests/Test-StoryFinalRecord.Tests.ps1` -- temporary-repository adversarial fixtures for counts, paths, dirt, gitlinks, evidence, and contract state.
- `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` -- deterministic shape builder; add full non-mutating baseline equality.
- `_bmad-output/implementation-artifacts/5-1-final-full-module-conformance-run-consolidated-public-contract-shape-diff.md`, `5-2-reconcile-the-removed-test-justification-ledger.md`, and `5-3-assemble-the-success-metric-report-and-signable-attestation.md` -- historical count and File List claims.
- `docs/release-evidence/final-conformance-contract-diff-v1.json`, `removed-test-justification-ledger-reconciliation-v1.json`, and `success-metric-report-and-attestation-v1.json` -- authoritative historical count/contract claims; inspect without editing.
- `_bmad-output/implementation-artifacts/tests/` -- frozen input, authoritative audit JSON, Markdown rendering, TRX, and test summary.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` and `epic-5-retro-2026-06-27.md` -- action closure after a passing or explicitly amended audit.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Test-StoryFinalRecord.ps1`, `_bmad-output/implementation-artifacts/tests/epic-5-final-record-input.json`, and `epic-5-final-record-preexisting-state.json` -- implement schema-validated live/historical inventory, claim, hash, TRX, gitlink, and contract-result checks.
- [x] `tests/Test-StoryFinalRecord.Tests.ps1` -- prove pass plus stale-count, missing/extra path, evidence-hash, altered-frozen-dirt, new-gitlink, and contract-drift failures in disposable repositories.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs` -- compare the full regenerated serialization with the committed baseline without writing it; emit actionable drift.
- [x] `_bmad-output/implementation-artifacts/tests/story-5-2-final-record-amendment.md` -- record the dated known `test-summary.md` File List amendment separately so the hash-bound Story 5.2 record remains byte-identical.
- [x] `_bmad-output/implementation-artifacts/tests/epic-5-final-record-check.json`, `epic-5-final-record-check.md`, and `test-summary.md` -- record historical 365/374/384 consistency and the new live final run from one authoritative result.
- [ ] `tests/README.md`, `epic-5-retro-2026-06-27.md`, `sprint-status.yaml`, and the approved proposal/spec -- document the gate, record approval/completion, and close the action only after verification.

**Acceptance Criteria:**
- Given a final work item tree, when the gate runs, then every count-bearing record, exact changed path, changed evidence hash/pair, frozen exclusion, and full contract shape agrees or the command fails with named mismatches.
- Given the three Epic 5 commit ranges, when historical mode runs, then their recorded counts and committed path sets are verified, Story 5.2’s disclosed amendment is explicit, and historical limitations are stated.
- Given current pre-existing worktree changes, when live mode completes, then unchanged unrelated files/gitlinks remain untouched and excluded while any new/altered out-of-scope state fails.
- Given successful final verification, when tracking artifacts are updated, then the Epic 4 workflow action and Epic 5 follow-up are resolved without reopening Epic 5 or modifying signed release evidence.

## Spec Change Log

- 2026-08-22: The user approved preserving the failed July 14 record and adding a dated corrective amendment plus successor audit. The successor remains blocked on 15 current-tree conformance failures; the spec and Epic 4 action stay in progress.

- 2026-10-08: The existing Story 9.2 user Quality approval covers exactly the measured v1-to-current drift. The current-policy conformance selection is taken unchanged from tracked CI; the unfiltered failing reproduction and older failed/blocked audits remain explicit and immutable. A separate dated successor binds the current clean boundary and both tier receipts.

## Design Notes

Live mode reads the current index/worktree relative to `baseline_commit` and subtracts only exact frozen entries. Historical mode reads bytes and path modes from Git objects. The final conformance TRX is authoritative for current counts; historical counts are cross-record consistency claims. Contract equality is proven by a dedicated non-mutating xUnit fact whose TRX result is consumed by the gate.

The 2026-08-22 successor separately verifies the byte-identical failed predecessor and approved amendment. It records the current broad conformance failure as `BLOCKED`, preserves focused contract-comparison evidence, and does not reconstruct the former July 14 uncommitted tree.

## Verification

**Commands:**
- `pwsh -NoProfile -File tests/Test-StoryFinalRecord.Tests.ps1` -- expected: every positive fixture passes and every injected drift fails for the intended reason.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release --no-restore /nr:false /m:1` -- expected: 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release --no-build --logger trx /nr:false` -- expected: all discovered tests pass, including full non-mutating contract equality.
- `pwsh -NoProfile -File tests/Test-StoryFinalRecord.ps1 -InputPath _bmad-output/implementation-artifacts/tests/epic-5-final-record-input.json` -- expected: live and historical results pass and render identical JSON/Markdown facts.
- `git diff --check` -- expected: no whitespace errors; unrelated files and frozen gitlinks remain byte-identical.

## Validation Results — 2026-08-22 historical

- PowerShell disposable-repository fault injection: 13 / 13 scenarios passed.
- Release conformance build: 0 warnings and 0 errors.
- Release conformance run: 438 / 453 passed, 15 failed, 0 skipped — `BLOCKED` by workflow-authority, projection-proof, preservation-proof, and retained workflow-skill guards.
- Focused non-mutating public-contract-shape comparison: 5 / 5 passed; baseline working-tree diff empty.
- Story 5.2 source record SHA-256 remains `ab6d4970d1e7cc78738435b09e0777d0a2eefe473ba91ca2310c26aa4d220b21`, matching the signed Story 5.3 source manifest.
- The original failed JSON/Markdown remain byte-identical at SHA-256 `a6ec97c1fc3fb3e026d72ce5bd480561d71acf3c051f84ac73f9fd24671c65e1` / `0b8e1de3fcd132c2d0d226a38d9e7c94037a5b4db6c2448c5d418070f551a710`.
- The August 22 successor remains `BLOCKED` as recorded; its bytes are preserved by the later audit below.

## Validation Results — 2026-10-08 successor

- Current policy conformance: 418 / 418 passed; portable 326 / 326 and internal 92 / 92, 0 failed, 0 skipped. Both Release project builds completed with 0 warnings and 0 errors.
- Unfiltered reproduction: 483 / 489 passed, 6 failed, 0 skipped. Five original single-assembly Story 9.1 controls and the retired preservation zero-gap assertion remain `FAIL`; the new authoritative JSON names all six. The canonical run uses the pre-existing selection in `.github/workflows/ci.yml` under `docs/runbooks/current-change-validation.md`, with no new exclusions.
- PowerShell adversarial fixtures: 25 / 25, including counter/result disagreement, empty results, both-tier aggregation, duplicate paths/cases, exact approved drift, and unapproved or altered approval rejection.
- The non-mutating full current shape comparison passes against the 219-type Story 9.2 successor. The immutable 196-type v1 shape remains byte-identical. Its 23 added types and four changed types reproduce approved drift `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`, bound to the existing user Quality decision `story-9.2-user-quality-approval-2026-10-07`; the report labels this `approved-difference`, and current-to-successor drift is `empty`.
- The July failed record, August blocked record, historical Story 5.2 source, and signed release evidence remain unchanged. Historical 365 / 365, 374 / 374 (with the existing amendment), and 384 / 384 claims remain commit-record consistency checks.
- A fresh live boundary uses commit `8793d26306f91d2cfe00116dc0b8eec41ec8900f`, a clean initial root tree, and exact frozen identities for all ten root gitlinks. It does not attribute intervening committed work to this task or reconstruct a former uncommitted tree.
- The dated successor includes non-ignored raw TRX XML, exact canonical commands, receipt hashes, executed binary/source/dependency fingerprints, and an authoritative JSON/Markdown pair. Final verification is required before action closure.

## Current closure blocker — 2026-10-08 interim run

The disposable-repository checker suite passed **25 / 25**. Final completion remains blocked because concurrent dependency, CI, and production work introduced out-of-scope changes after the captured clean boundary. The input fingerprint is explicitly `PENDING_FINAL_RUN`; the earlier August sealed digest is not reused.

```powershell
pwsh -NoProfile -File tests/Test-StoryFinalRecord.ps1 -InputPath _bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-input.json
```

Recorded result at `2026-10-08T12:49:49Z`: **exit 1**, JSON `status: fail`, `mechanicalResult: FAIL`. The checker reported these exact failures:

- Frozen entry 'references/Hexalith.Builds' changed: submodule internal status hash changed from e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 to d662a78b7d1574a61a91b521584d1e3152917d1ca7d74548c5074e02d7b64216.
- Live declared-vs-observed path inventory contains unexpected path '.github/workflows/ci.yml'.
- Live declared-vs-observed path inventory contains unexpected path '_bmad-output/implementation-artifacts/spec-update-all-packages.md'.
- Live declared-vs-observed path inventory contains unexpected path '_bmad/scripts/check_package_refresh_environment.py'.
- Live declared-vs-observed path inventory contains unexpected path 'package-lock.json'.
- Live declared-vs-observed path inventory contains unexpected path 'package.json'.
- Live declared-vs-observed path inventory contains unexpected path 'references/Hexalith.Builds'.
- Live declared-vs-observed path inventory contains unexpected path 'src/Hexalith.Conversations.Server/Agents/ConversationDeletionPublicationProjector.cs'.
- Live declared-vs-observed path inventory contains unexpected path 'tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionPublicationProjectorTests.cs'.
- Live declared-vs-observed path inventory contains unexpected path 'uv.lock'.
- Live work item contains non-excluded gitlink 'references/Hexalith.Builds'.
- Executable/test input fingerprint is stale: expected PENDING_FINAL_RUN, found 6cdae800bb1ef7253d778279f5ae0e6cbaea7b817b717b79ea372e14c89d5ea9.
- Changed documentation/evidence '.github/workflows/ci.yml' SHA-256 is a3e797dd1af943eb32cda4224e2aa82dc0b773c03dd8c9d27b2d0c3a9bf033a9; expected 134b264eae0c986f132c0fcdf55648613d4c28947652d94b84a1a904e71365b5.

The authoritative interim JSON and rendered Markdown preserve the failed gate result. The 418 / 418 current-policy receipts describe the tree exercised before these concurrent edits; they do not certify the changing tree. The independent 483 / 489 unfiltered reproduction remains `FAIL` with all six historical failures. Earlier audit pairs and signed evidence remain unchanged.

The spec and Epic 4 A1 remain `in-progress`. After the external owner finishes, capture a fresh current boundary and exact unrelated state, rerun the affected builds/tests, bind the final executable inputs, and regenerate the live audit. Do not retroactively freeze the intervening changes at the original start or close the action from this interim failure.

### File List

- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-27.md`
- `_bmad-output/implementation-artifacts/spec-epic-5-final-record-check.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-check.json`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-check.md`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-input.json`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-internal.xml`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-portable.xml`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-preexisting-state.json`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-unfiltered-internal.xml`
- `_bmad-output/implementation-artifacts/tests/epic-5-final-record-successor-2026-10-08-unfiltered-portable.xml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-epic-5-final-record-check.md`
- `tests/README.md`
- `tests/Test-StoryFinalRecord.Input.schema.json`
- `tests/Test-StoryFinalRecord.Tests.ps1`
- `tests/Test-StoryFinalRecord.ps1`
