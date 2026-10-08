# Epic 5 Final-Record Check

- **Generated:** 2026-10-08T12:50:47Z
- **Overall result:** fail
- **Mechanical result:** FAIL
- **Authority:** The adjacent JSON artifact is authoritative; this Markdown is rendered from it.

## Preserved Predecessor Disposition

- Source result: FAIL
- Successor disposition: pass-with-approved-amendment
- Source commit: 8f8f14fd6e842eeb19b7410554366a93f8a93ce5
- Corrective amendment: _bmad-output/implementation-artifacts/tests/epic-5-final-record-corrective-amendment-2026-08-22.md
- Limitation: The predecessor failure and exact bytes are preserved. The amendment disposes the named historical discrepancy; it does not reconstruct the former uncommitted working tree.

## Live Final Working Tree

- Result: fail
- Conformance: 418 / 418 passed; 0 failed; 0 skipped.
- Changed paths: 26 observed, 0 missing, 9 unexpected.
- Frozen pre-existing entries: 10 checked.
- Public-contract-shape diff: approved-difference.
- Approved historical contract drift: 3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1; reference: docs/release-evidence/conformance-oracle-tiering-migration-approval-v3.json.
- Current successor contract shape: empty; 219 types.
- Failures:
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

## Unfiltered Historical Reproduction

- Result: FAIL; 483 / 489 passed; 6 failed; 0 skipped.
- Current validation policy: docs/runbooks/current-change-validation.md. The existing CI selection retires historical authority subjects and original single-assembly Story 9.1 controls. Both unfiltered assemblies were executed without exclusions; all six reproduced failures remain named and FAIL. The canonical current run uses the pre-existing CI class/method exclusions without adding exclusions.
- Limitation: This unfiltered reproduction remains FAIL. The current policy selection is reported separately; no historical result is reclassified as PASS.

- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.AssertionInventoryShouldMatchPreSplitResultAndSource
- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.DenominatorSuitesShouldRemainUnchanged
- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ServerBoundAssertionsShouldHaveExactDisposition
- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.StrengthDigestsShouldRemainEqual
- Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.TieringShouldNotWidenPublicContracts
- Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest.PublicSurfacesAndConformanceAssertionsShouldHaveZeroGap

## Historical Epic 5 Audit

| Story | Result | Passed / Total | File List | Contract baseline |
| --- | --- | ---: | --- | --- |
| 5.1 | pass | 365 / 365 | pass | baseline-unchanged-and-recorded-diff-empty |
| 5.2 | pass-with-approved-amendment | 374 / 374 | pass | baseline-unchanged-and-recorded-diff-empty |
| 5.3 | pass | 384 / 384 | pass | baseline-unchanged-and-recorded-diff-empty |

Historical mode proves committed path, artifact, and count-claim consistency. It does not claim to reconstruct a former uncommitted working tree.
