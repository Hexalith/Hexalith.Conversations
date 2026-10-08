# Test Automation Summary

## Scope

Epic 5 final-record workflow verification: live final-tree reconciliation, historical Story 5.1–5.3 audit, exact dirty-state exclusion, and non-mutating public-contract-shape equality.

## Added Coverage

- `PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting` compares the full regenerated public contract serialization with the immutable Story 1.1 baseline without writing it.
- `tests/Test-StoryFinalRecord.Tests.ps1` exercises twelve disposable-repository scenarios: live pass, stale count, missing File List entry, evidence hash drift, changed untracked frozen state, changed tracked frozen state, new gitlink, contract drift, invalid input schema, listed-but-missing path, predecessor-record tamper, and unavailable historical Git objects.
- `tests/Test-StoryFinalRecord.ps1` checks schema validity, live and historical counts, exact paths, evidence identities/pairs, frozen state, input fingerprints, TRX contract-test outcome, contract-baseline state, and the byte-identical failed predecessor plus approved corrective amendment.

## Historical Audit Counts

- Story 5.1: 365 / 365 passed, 0 failed, 0 skipped.
- Story 5.2: 374 / 374 passed, 0 failed, 0 skipped; the omitted historical `test-summary.md` path is covered by its separate approved amendment.
- Story 5.3: 384 / 384 passed, 0 failed, 0 skipped.

## Current Validation

- PowerShell fault-injection fixtures: 12 / 12 scenarios passed.
- Release conformance build: 0 warnings, 0 errors.
- Broad Release conformance run: 453 total, 438 passed, 15 failed, 0 skipped.
- Focused non-mutating public-contract-shape comparison: 5 total, 5 passed, 0 failed, 0 skipped; expected diff state `empty`.
- Original 2026-07-14 final-record JSON/Markdown: preserved as the authoritative failed predecessor at SHA-256 `a6ec97c1fc3fb3e026d72ce5bd480561d71acf3c051f84ac73f9fd24671c65e1` / `0b8e1de3fcd132c2d0d226a38d9e7c94037a5b4db6c2448c5d418070f551a710`.
- 2026-08-22 successor live/historical record gate: `BLOCKED` by the 15 broad conformance failures; no release or action completion is claimed.

## Epic 5 final-record successor — 2026-10-08

Current policy conformance: 418 / 418 passed; portable 326 / 326, internal 92 / 92; 0 failed, 0 skipped. Both Release builds pass with 0 warnings/errors. The unchanged current CI class/method selection follows `docs/runbooks/current-change-validation.md`.

The independent unfiltered reproduction remains **FAIL**: portable 391 / 397 plus internal 92 / 92, or 483 / 489 combined; six failed, none skipped. Its raw XML and every failed identity are retained in `epic-5-final-record-successor-2026-10-08-check.json`; no earlier result is relabelled.

The PowerShell gate fixtures pass 25 / 25. The full current contract comparison passes against the 219-type v2 snapshot. The unchanged 196-type v1 delta exactly matches the existing October 7 user Quality approval (23 added types, four changed, none removed); the report records `approved-difference` and empty current-to-successor drift separately. Historical Epic 5 counts remain 365 / 365, 374 / 374 with its disclosed amendment, and 384 / 384.

The dated JSON is authoritative and its Markdown is rendered from the same facts. July's failed pair, August's blocked pair, signed evidence, and root gitlinks remain unchanged.

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
