---
title: 'Story 10.2: Manifest, hash, ledger, exact-diff, and gitlink invariants'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
baseline_commit: 'bba1119988de4736e1dc526abd3c025a785eb3a7'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 10.1's `EvidenceManifest`, `BoundaryAssertions`, and `AssertionLedger` only store declarations; nothing recomputes evidence facts. A trusted hash, subset path comparison, text-matched gitlink, unavailable history, or empty ledger can therefore still look like a pass.

**Approach:** Extend those helpers with the contracted invariants and exact blocker codes, prove AC-10.2-01..08 with isolated byte-restored fixtures, and extend the generator so AC-10.2-09 publishes a candidate-bound `9/9/0/0/0/0` pair bound to Story 10.1.

## Boundaries & Constraints

**Always:** Recompute every fact. Manifest paths must be relative, contained via `RepositoryLocator` (symlinks rejected), existing, and outside any `bin`, `obj`, or `generated` directory segment (case-insensitive). File hashes are recomputed from bytes, and the signable-payload digest is recomputed from the manifest rows; a declared digest is never trusted. Changed paths use exact ordinal set equality against the Git raw diff, including rename sources. Gitlinks come only from raw mode `160000` fields. A non-success Git fact is a visible ledger skip, never a pass. A final pass needs at least one evaluated assertion, all passing, and no skipped identity recorded as passed. Roots of trust stay as literals in the consuming test source. Allowlist entries never resolve under `docs/release-evidence/` (case-insensitive, after separator normalization), and the real allowlist starts empty. Use exactly the contracted blocker codes. Keep Story 10.1's public API, tests, and record bytes intact. Helpers stay non-shipping and free of Conversations assemblies, with one type per `.cs` file in CRLF like 10.1. The record binds `SC-10.2` (HEAD at first generation), the Story 10.1 JSON record SHA-256, both TestSupport project/assembly hashes, AC-01..08 TRX hashes, root constants, the mutation ledger, and inventory digest `9195f98c…ca4c`.

**Decisions (owner, 2026-10-12):**
- Keep the full spec as one unit, at about 3,200 tokens.
- Story 10.2 creates the closed result schema with a focused pytest, and no C# serializer. The schema requires the nine contracted fields and allows `result` to be `PASS`, `FAIL`, `BLOCKED`, or `not-applicable`. `blockers` and `warnings` are ordinally sorted arrays of unique code strings, and ledger rows are `{id, subject, state}`. Story 10.3's verifier conforms to it.
- The signable payload defaults to canonical ordinal row order. A documented declared-order option exists only for payloads signed before canonical ordering, such as the v1 attestation, so Story 10.4 can migrate that reader at equal strength.

**Never:** Migrate or edit frozen evidence readers or conformance tiers (Story 10.4). Build the workflow verifier or workflow insertions (Story 10.3). Never add exemptions, trust declared digests, hand-author record facts, or change the accepted 7.4/9.2/10.1 record bytes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Clean manifest | Contained files, matching lowercase hashes, roles, recomputed signable digest | `PASS`; one ledger assertion per row plus the payload | — |
| Manifest fault | `../x`, absent file, flipped byte, `bin/`/`obj/`/`generated/` segment, tampered or stale signable digest | Not `PASS` | `EVIDENCE_PATH_ESCAPE` / `EVIDENCE_FILE_MISSING` / `EVIDENCE_HASH_MISMATCH` / `EVIDENCE_OUTPUT_FORBIDDEN` / `SIGNABLE_PAYLOAD_MISMATCH` |
| Path sets | Expected vs Git raw diff: equal, one missing, one unexpected | Equal passes | `CHANGED_PATH_SET_MISMATCH` naming missing and unexpected paths |
| Gitlink | Raw `160000` entry plus decoy path, content, and object ID containing `160000` | Only the raw-mode entry is flagged | `EVIDENCE_BOUNDARY_GITLINK` |
| History | Unresolvable revision | Check visibly skipped; ledger stays at 0; never `PASS` | A pass recorded for a skipped check gives `UNAVAILABLE_HISTORY_PASSED`; zero evaluated gives `ASSERTION_LEDGER_EMPTY` |
| Roots | Root literal absent from consuming source; allowlist entry under `docs/release-evidence/` | Rejected | `ROOT_OF_TRUST_NOT_PINNED` / `SIGNED_EVIDENCE_ALLOWLISTED` |
| Fault suite | 12 mutations with before-hashes | Each exact blocker occurs; bytes restored | `FAULT_NOT_DETECTED` / `FIXTURE_NOT_RESTORED` |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Conversations.TestSupport/{EvidenceManifest,BoundaryAssertions,AssertionLedger}.cs` -- 10.1 stubs to extend. Keep `Add(path, sha256)`, `Digests`, `Record`, `Results`, `Equal`, and `True`; `TestSupportSurfaceTests` (AC-10.1-02) uses them.
- `.../RepositoryLocator.cs` `ResolveContainedPath` -- containment and symlink rule; map its `REPOSITORY_PATH_ESCAPE` to `EVIDENCE_PATH_ESCAPE`.
- `.../GitFacts.cs` -- `ReadDiffAsync` (`--raw -z --find-renames=50% --ignore-submodules=none`), `ResolveRevisionAsync`, `GitDiffEntry`, `GitFact<T>`, `GitFactStatus`.
- `tests/Hexalith.Conversations.TestSupport.Tests/GitFixture.cs`; `GitFactsTests.cs` (real gitlink at :451, shallow fixtures at :494-:549) -- fixture patterns to reuse.
- `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` -- semantic precedent only; do not edit. Row format :398-416, pinned roots :32-47, allowlist :68-71 and :275-288.
- `_bmad/scripts/generate_story_record.py` -- mirror or generalize the 10.1 path:
  - `v2_10_1_facts` :5515 (inventory digest :5544, binding rule :5595), called at :8098.
  - `v2_verified_predecessor` :5418, `v2_10_1_artifact_bytes` :5443, `v2_10_1_project_boundary` :5460.
  - Markdown :4497 and fault table :4517; `v2_verify_pair` :4603 and `v2_10_1_exact_paths` :4645.
  - Inserted rederive :7905/:8367; `V2_CODES` around :3030; `parse_trx` :873 (no StdOut parsing yet); xUnit route :6146/:6302.
  - `v2_retention_config` :5076 takes the spec stem as the sprint key, and this filename already matches.
- `_bmad/schemas/story-final-record-v2.schema.json` -- 10.1 conditional :430, `testSupportExecution` :537/:4524, fault rows :1078/:2297. Top level is `additionalProperties: false`.
- `_bmad/scripts/tests/test_generate_story_record.py` -- story101 tests (:7622, :10076-:10616) to mirror as story102.
- `_bmad/scripts/verify_evidence_boundary.py` -- historical; leave it for 10.3.
- `.github/workflows/ci.yml` -- already runs TestSupport.Tests and `_bmad/scripts/tests`; no change expected.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Conversations.TestSupport/` -- add one-type-per-file public types (for example `EvidenceManifestRow`, `BoundaryBlocker`). Extend the helpers with role-bearing rows, recomputing verification, canonical signable payload, exact changed-path and raw-mode gitlink assertions, history skip, root-of-trust and signed-allowlist assertions, and ledger skip tracking with final evaluation -- these are the shared invariants.
- [x] `tests/Hexalith.Conversations.TestSupport.Tests/{EvidenceManifestTests,BoundaryAssertionsTests,AssertionLedgerTests,EvidenceBoundaryFaultTests}.cs` -- the eight contracted methods plus focused edge tests, using temp fixtures only. The exception is AC-07's real roots: the pinned Story 10.1 record SHA-256 and candidate commit, with an empty allowlist, found from the repository root. AC-07 and AC-08 each write one marked JSON line to test output, holding the root constants and the 12-row mutation ledger.
- [x] `_bmad/schemas/evidence-boundary-result-v1.schema.json` and `_bmad/scripts/tests/test_evidence_boundary_result_schema.py` -- create the closed schema described under Decisions. Validate one valid instance, and show that a missing field, an extra field, an unknown state, and a duplicate code are each rejected.
- [x] Generator, record schema, and story102 tests -- 10.2 facts parse the marked TRX output. They re-verify the roots: the 10.1 digest must equal the independently verified predecessor, and the literals must be present in the committed consuming source. They require the exact 12 mutation rows, each detected and restored. Add rendering, pair verification, and inserted rederivation; the 10.1 pair must still validate unchanged.
- [x] `docs/release-evidence/story-10.2-final-record-v2.{json,md}` -- run the exact AC-01..08 commands on a committed candidate, then AC-09.

**Acceptance Criteria:**
- Given a Release build at a committed candidate, when the exact AC-10.2-01..08 commands run, then each exits `0` with exactly one passing TRX row.
- Given those results and the compatible Story 10.1 pair, when AC-10.2-09 runs, then it reports `PASS` with summary `9/9/0/0/0/0` and every binding listed under Always.
- Given a missing marked line, a mutation row not detected or not restored, or a root that differs from the verified 10.1 digest, when the generator runs, then it does not return `PASS`.
- Given the complete TestSupport suite and the story101 generator tests, when they run after this change, then all pass and the 10.1 pair verifies unchanged.

## Design Notes

- **Signable payload:** one row per manifest entry, `path\tsha256\trole\n`, over the declared rows, UTF-8 without BOM, SHA-256 in lowercase hex. It proves the signed rows were not altered. File drift is the separate `EVIDENCE_HASH_MISMATCH` invariant, so a stale row still prevents `PASS`. Reject tab, CR, and LF in a path or role when the row is added. The manifest keeps rows in the order they were added. The payload method takes an order option that defaults to canonical ordinal. AC-10.2-03 also proves the declared-order option on the real v1 attestation: declared order reproduces its signed `signablePayloadHash`, and ordinal order does not.
- **Mutation IDs, each followed by its expected blocker:**
  - `AC-10.2-01-HASH`, `-PATH`, `-MISSING`: `EVIDENCE_HASH_MISMATCH`, `EVIDENCE_PATH_ESCAPE`, `EVIDENCE_FILE_MISSING`.
  - `AC-10.2-02-OUTPUT`: `EVIDENCE_OUTPUT_FORBIDDEN`.
  - `AC-10.2-03-SIGNABLE`: `SIGNABLE_PAYLOAD_MISMATCH`.
  - `AC-10.2-04-SUBSET`, `-SUPERSET`: `CHANGED_PATH_SET_MISMATCH`.
  - `AC-10.2-05-GITLINK`: `EVIDENCE_BOUNDARY_GITLINK`.
  - `AC-10.2-06-HISTORY`, `-EMPTY`: `UNAVAILABLE_HISTORY_PASSED`, `ASSERTION_LEDGER_EMPTY`.
  - `AC-10.2-07-ROOT`, `-ALLOWLIST`: `ROOT_OF_TRUST_NOT_PINNED`, `SIGNED_EVIDENCE_ALLOWLISTED`.

  Every fault mutates the bytes of one fixture file. HISTORY and EMPTY also run a deliberately non-compliant consumer, so the ledger itself rejects the false pass. A row records its ID, expected and observed codes, and before and after SHA-256.
- **Transport:** hash-bound TRX output, not a side file. The pinned `xunit.v3.runner.common` 4.0.2 TRX writer emits `StdOut`; confirm this at implementation.

## Verification

**Commands:**
- `dotnet build Hexalith.Conversations.slnx --configuration Release` -- succeeds.
- `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips` -- all tests pass, including 10.1's.
- The exact AC-10.2-01..09 commands from `_bmad-output/planning-artifacts/v9/story-contracts/10.2.json` -- each exits `0`.
- `TMPDIR=/var/tmp uv run --frozen --no-sync python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_generate_story_record.py -k "story101 or story102"` -- passes.

## Implementation Notes

- The pinned xUnit v3 4.0.2 TRX writer emits marked test output in `Output/TextMessages/Message`. The parser accepts that measured transport and `Output/StdOut`, and requires exactly one marker in the sole passing acceptance row.
- JSON Schema enforces the closed result shape and unique code strings. Consumers must additionally check ordinal code-array ordering, which standard JSON Schema cannot compare; the focused schema test exercises both parts of that contract.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 10.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `10.2`
- Candidate: `cc7b3b695bd77452b7402423fbc46fc515ef6304`
- JSON content SHA-256 (all three digest fields zeroed): `537377cc38a846e231d1e7f252db57a9601eeee94f4dfc72b63a545f75057791`

## Authority

| Field | Value |
| --- | --- |
| Epic | `epic-6-authority-2026-08-03-v10` |
| Architecture | `conversations-architecture-2026-08-03-v10` |
| Planning candidate | `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` |
| Bundle digest | `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055` |

## Root gitlinks

| Path | Mode | Commit |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `160000` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `160000` | `608c164c3637240fed2dd92dbfdaaf581ba9846d` |
| `references/Hexalith.Commons` | `160000` | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| `references/Hexalith.EventStore` | `160000` | `3d0b738d23e51ff765004e3f76ee7863e83b5ee6` |
| `references/Hexalith.Folders` | `160000` | `47e51c13c124a3a1b93d00c5f90aaecad62f182d` |
| `references/Hexalith.FrontComposer` | `160000` | `985d3e70ac8e9d5492fa857bc3a31208a9543670` |
| `references/Hexalith.Memories` | `160000` | `f4c7d68f619f376debca440387428139a61080cc` |
| `references/Hexalith.Parties` | `160000` | `35fd52186209bfade0d43bda30dd1c684eba4085` |
| `references/Hexalith.Projects` | `160000` | `89ea0861343e9bedd90c598e33cb87fecdd4f39c` |
| `references/Hexalith.Tenants` | `160000` | `68682ee0515161bce0f206f98e671336369048d1` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-10.2-ENTRY-v1` | `9195f98c90362ce4035ef2223fcf6eab1cc705d0c7cc7ace47b0de9dc809ca4c` |

## Predecessors

- `10.1`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-10.2-01` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-01.trx` | `0b1b0205472e9a76285e37daf3399ce97a7e0a9248cfb659d5a1334d4dbc3d96` |
| `AC-10.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-02.trx` | `13cc5ee678a167aef94bda9981a74b20bfb3b375290b383c5d9c5a73499c7e79` |
| `AC-10.2-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-03.trx` | `01fabcfc345f74c086b713fc9820a3df006751fcc36d3e0c9000804def3d55c5` |
| `AC-10.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-04.trx` | `50c4f1377d9d7e7f1ea2d3f48e12f8701fffa30634677e408e0e1b6a8d772866` |
| `AC-10.2-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-05.trx` | `82257a53e08209f55e10bf227dfbc1ca8f578c21a73390a2400e79e90cdf114b` |
| `AC-10.2-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-06.trx` | `cdcc866f576a611686017d219c5c100824303d1e89ba67b3a77748f61833fa3e` |
| `AC-10.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-07.trx` | `654e2d57ff534c50dbe85e6a9a927af145abb80d3d456b62461df55bc3704aa5` |
| `AC-10.2-08` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-08.trx` | `27a562e1b66b2c79ef69f349ed4d7599644418c038f11991781dbe2b978dde33` |
| `AC-10.2-09` | `0` | `PASS` | `none` | `9` | none | none |

### `AC-10.2-01`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.ManifestShouldRecomputeContainedFilesAndHashes -trx artifacts/v9/10.2/AC-10.2-01.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-01.trx` | `0b1b0205472e9a76285e37daf3399ce97a7e0a9248cfb659d5a1334d4dbc3d96` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-01#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.ManifestShouldRecomputeContainedFilesAndHashes` | `PASS` |

### `AC-10.2-02`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.GeneratedAndBuildOutputsShouldBeRejected -trx artifacts/v9/10.2/AC-10.2-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-02.trx` | `13cc5ee678a167aef94bda9981a74b20bfb3b375290b383c5d9c5a73499c7e79` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-02#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.GeneratedAndBuildOutputsShouldBeRejected` | `PASS` |

### `AC-10.2-03`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.SignablePayloadShouldBeRecomputed -trx artifacts/v9/10.2/AC-10.2-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-03.trx` | `01fabcfc345f74c086b713fc9820a3df006751fcc36d3e0c9000804def3d55c5` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-03#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.SignablePayloadShouldBeRecomputed` | `PASS` |

### `AC-10.2-04`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.ChangedPathsShouldUseExactSetEquality -trx artifacts/v9/10.2/AC-10.2-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-04.trx` | `50c4f1377d9d7e7f1ea2d3f48e12f8701fffa30634677e408e0e1b6a8d772866` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-04#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.ChangedPathsShouldUseExactSetEquality` | `PASS` |

### `AC-10.2-05`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.GitlinksShouldBeDetectedFromRawModes -trx artifacts/v9/10.2/AC-10.2-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-05.trx` | `82257a53e08209f55e10bf227dfbc1ca8f578c21a73390a2400e79e90cdf114b` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-05#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.GitlinksShouldBeDetectedFromRawModes` | `PASS` |

### `AC-10.2-06`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.AssertionLedgerTests.UnavailableHistoryShouldSkipAndNeverPass -trx artifacts/v9/10.2/AC-10.2-06.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-06.trx` | `cdcc866f576a611686017d219c5c100824303d1e89ba67b3a77748f61833fa3e` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-06#0001` | `Hexalith.Conversations.TestSupport.Tests.AssertionLedgerTests.UnavailableHistoryShouldSkipAndNeverPass` | `PASS` |

### `AC-10.2-07`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.RootsOfTrustAndSignedEvidenceShouldRemainPinned -trx artifacts/v9/10.2/AC-10.2-07.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-07.trx` | `654e2d57ff534c50dbe85e6a9a927af145abb80d3d456b62461df55bc3704aa5` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-07#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.RootsOfTrustAndSignedEvidenceShouldRemainPinned` | `PASS` |

### `AC-10.2-08`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceBoundaryFaultTests.AllInvariantFaultsShouldFailAndRestore -trx artifacts/v9/10.2/AC-10.2-08.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-08.trx` | `27a562e1b66b2c79ef69f349ed4d7599644418c038f11991781dbe2b978dde33` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-08#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceBoundaryFaultTests.AllInvariantFaultsShouldFailAndRestore` | `PASS` |

### `AC-10.2-09`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/10.2.json --format bundle --output-json docs/release-evidence/story-10.2-final-record-v2.json --output-markdown docs/release-evidence/story-10.2-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-09#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-10.2-09#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-10.2-09#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-10.2-09#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-10.2-09#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-10.2-09#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-10.2-09#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-10.2-09#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-10.2-09#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |

## Story 10.2 TestSupport execution

- Candidate: `cc7b3b695bd77452b7402423fbc46fc515ef6304`
- Candidate rule: SC-10.2 is HEAD^{commit} at first final-record generation
- Frozen inventory SHA-256: `9195f98c90362ce4035ef2223fcf6eab1cc705d0c7cc7ace47b0de9dc809ca4c`
- Contract SHA-256: `42d040c19e781efede0fd8105b039cf8ecc92eaf6bec6c2e9c2b7d7fe75dcf44`

| Predecessor | Record SHA-256 |
| --- | --- |
| `10.1` | `334858d730c96c0a5a6647c514e631cfe4907756846bf2e0147e11937e221404` |

| Project | Project SHA-256 | Assembly | Assembly SHA-256 |
| --- | --- | --- | --- |
| `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `9f885c50d869285e6b280b0c135b2c1449cda6f8e863271ff6420924443be925` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `b27ca2bc142900542c33fa77da90af58baab6ebb876fc0f080e66a73613bf0c6` |
| `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj` | `6d93f6ea2dbca4fb89047836f9f10ea633f2996e257830c46eaba2aaef646bf2` | `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `38018b950f2f40b4d0d7cca1e22e594753bcf4eb37c0f32fe048f215a9561369` |

| Result | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-01.trx` | `0b1b0205472e9a76285e37daf3399ce97a7e0a9248cfb659d5a1334d4dbc3d96` |
| `artifacts/v9/10.2/AC-10.2-02.trx` | `13cc5ee678a167aef94bda9981a74b20bfb3b375290b383c5d9c5a73499c7e79` |
| `artifacts/v9/10.2/AC-10.2-03.trx` | `01fabcfc345f74c086b713fc9820a3df006751fcc36d3e0c9000804def3d55c5` |
| `artifacts/v9/10.2/AC-10.2-04.trx` | `50c4f1377d9d7e7f1ea2d3f48e12f8701fffa30634677e408e0e1b6a8d772866` |
| `artifacts/v9/10.2/AC-10.2-05.trx` | `82257a53e08209f55e10bf227dfbc1ca8f578c21a73390a2400e79e90cdf114b` |
| `artifacts/v9/10.2/AC-10.2-06.trx` | `cdcc866f576a611686017d219c5c100824303d1e89ba67b3a77748f61833fa3e` |
| `artifacts/v9/10.2/AC-10.2-07.trx` | `654e2d57ff534c50dbe85e6a9a927af145abb80d3d456b62461df55bc3704aa5` |
| `artifacts/v9/10.2/AC-10.2-08.trx` | `27a562e1b66b2c79ef69f349ed4d7599644418c038f11991781dbe2b978dde33` |

- Consuming source: `tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs`
- Supersession allowlist: empty

| Literal root | Value |
| --- | --- |
| `Story101CandidateCommit` | `79f4b126ca66d419a584855936ce56a1266cce67` |
| `Story101RecordSha256` | `334858d730c96c0a5a6647c514e631cfe4907756846bf2e0147e11937e221404` |

## Fault injection

| Fault | Expected blocker | Observed blockers | Before SHA-256 | After SHA-256 | Detected | Restored |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-10.2-01-HASH` | `EVIDENCE_HASH_MISMATCH` | `EVIDENCE_HASH_MISMATCH` | `50697a338ad3ccb5fa3868b9da1acc7806b37cd766b496c8f902eecff1dc8f68` | `50697a338ad3ccb5fa3868b9da1acc7806b37cd766b496c8f902eecff1dc8f68` | `True` | `True` |
| `AC-10.2-01-PATH` | `EVIDENCE_PATH_ESCAPE` | `EVIDENCE_PATH_ESCAPE` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `True` | `True` |
| `AC-10.2-01-MISSING` | `EVIDENCE_FILE_MISSING` | `EVIDENCE_FILE_MISSING` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `True` | `True` |
| `AC-10.2-02-OUTPUT` | `EVIDENCE_OUTPUT_FORBIDDEN` | `EVIDENCE_OUTPUT_FORBIDDEN` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `182a7b5d91524ee51194a237617d74c218e659ca040c78207136223e2677b2da` | `True` | `True` |
| `AC-10.2-03-SIGNABLE` | `SIGNABLE_PAYLOAD_MISMATCH` | `SIGNABLE_PAYLOAD_MISMATCH` | `aaaa66a2802a0bafdecfe58e767f98ca2e7c48b063ae102a1914959702804f41` | `aaaa66a2802a0bafdecfe58e767f98ca2e7c48b063ae102a1914959702804f41` | `True` | `True` |
| `AC-10.2-04-SUBSET` | `CHANGED_PATH_SET_MISMATCH` | `CHANGED_PATH_SET_MISMATCH` | `6d0c9146e13f4b8f82cd059a43f97056e1303f0dca08b35f26225f2c5f5af793` | `6d0c9146e13f4b8f82cd059a43f97056e1303f0dca08b35f26225f2c5f5af793` | `True` | `True` |
| `AC-10.2-04-SUPERSET` | `CHANGED_PATH_SET_MISMATCH` | `CHANGED_PATH_SET_MISMATCH` | `6d0c9146e13f4b8f82cd059a43f97056e1303f0dca08b35f26225f2c5f5af793` | `6d0c9146e13f4b8f82cd059a43f97056e1303f0dca08b35f26225f2c5f5af793` | `True` | `True` |
| `AC-10.2-05-GITLINK` | `EVIDENCE_BOUNDARY_GITLINK` | `EVIDENCE_BOUNDARY_GITLINK` | `7a771c644d6ce76e2c28abc833739f7e4a0d608c249cf22b89a372fe892ee8a1` | `7a771c644d6ce76e2c28abc833739f7e4a0d608c249cf22b89a372fe892ee8a1` | `True` | `True` |
| `AC-10.2-06-HISTORY` | `UNAVAILABLE_HISTORY_PASSED` | `REVISION_INVALID, UNAVAILABLE_HISTORY_PASSED` | `b99cce9b80a7517180719510dd3c6a5160597a5d9354a0f28e3349161a90cdc5` | `b99cce9b80a7517180719510dd3c6a5160597a5d9354a0f28e3349161a90cdc5` | `True` | `True` |
| `AC-10.2-06-EMPTY` | `ASSERTION_LEDGER_EMPTY` | `ASSERTION_LEDGER_EMPTY` | `b5bea41b6c623f7c09f1bf24dcae58ebab3c0cdd90ad966bc43a45b44867e12b` | `b5bea41b6c623f7c09f1bf24dcae58ebab3c0cdd90ad966bc43a45b44867e12b` | `True` | `True` |
| `AC-10.2-07-ROOT` | `ROOT_OF_TRUST_NOT_PINNED` | `ROOT_OF_TRUST_NOT_PINNED` | `c7827b2323833bde01cf2518ef646e51f12fd89b75c4230f084e971a62b59d2f` | `c7827b2323833bde01cf2518ef646e51f12fd89b75c4230f084e971a62b59d2f` | `True` | `True` |
| `AC-10.2-07-ALLOWLIST` | `SIGNED_EVIDENCE_ALLOWLISTED` | `SIGNED_EVIDENCE_ALLOWLISTED` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` | `True` | `True` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-10.2-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-10.2-final-record-v2.md` |

## Rollback boundary

remove only Story 10.2 invariant code/tests/results and record; retain Story 10.1 intact.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `9` | `9` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
