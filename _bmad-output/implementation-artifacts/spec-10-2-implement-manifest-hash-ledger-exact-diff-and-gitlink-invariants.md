---
title: 'Story 10.2: Manifest, hash, ledger, exact-diff, and gitlink invariants'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
baseline_commit: 'bba1119988de4736e1dc526abd3c025a785eb3a7'
route: 'dispatch'
review_loop_iteration: 1
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
- [ ] `docs/release-evidence/story-10.2-final-record-v2.{json,md}` -- run the exact AC-01..08 commands on a committed candidate, then AC-09.

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

## Review Corrections — Iteration 1

- [x] `tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` and its tests — separate file and payload identities; reject malformed Unicode with strict UTF-8 encoding and retain the existing declared-order compatibility proof.
- [x] `tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs`, private supporting source parsing if needed, its tests, and `_bmad/scripts/generate_story_record.py` — recognize actual literal declarations outside comments and string/character literals, and conservatively reject roots inside preprocessor conditional regions. Keep real roots unconditional in their consuming source. Prove raw-string and inactive-branch decoys cannot pass in both implementations.
- [x] `_bmad/schemas/evidence-boundary-result-v1.schema.json` and its focused tests — PASS requires a nonempty ledger containing only PASS states and no blockers. Make candidate/hash/code/path/role syntax reject trailing newlines while preserving all four result states and sorted unique code arrays.
- [x] `_bmad/scripts/generate_story_record.py` and story102 tests — require the helper DLL copied beside the test executable to equal the bound helper assembly and carry the same candidate stamp. Missing/divergent copies fail. Reverify the root candidate and exact scenario commands from the committed predecessor/contract during repository-aware Story 10.2 pair consumption; keep repository-free pair checks as structural checks, with no undeclared root literals in Python.
- [x] `_bmad/scripts/tests/test_generate_story_record.py` — add real-generator inserted verification coverage for a valid pair and a changed runtime/bound artifact, without replacing `v2_generate`; mutation of the designated-spec route must make the test fail.
- [x] `.github/workflows/ci.yml` — give the Windows TestSupport checkout full root history with `fetch-depth: 0`.
- [ ] Regenerate the pair and inserted record from the corrected committed candidate after withdrawing the superseded Story 10.2 outputs. Preserve all earlier story record bytes.


## Implementation Notes

- Iteration 1 recognizes unconditional C# declaration tokens, including nested strings inside interpolations, and excludes every preprocessor conditional region. Repository-aware consumption rechecks committed predecessor roots and exact contract commands. The executed helper copy must equal the bound candidate-stamped assembly.
- The corrected source passed the serialized full Release solution build (zero warnings/errors), all 27 TestSupport tests, 51 story101/story102 generator tests, and 19 focused result-schema tests. Removing the designated-spec route made all three real-generator regressions fail; the original generator bytes were restored. The unqualified parallel solution build stalled without output; the documented `-m:1 -nr:false` fallback succeeded.

- The pinned xUnit v3 4.0.2 TRX writer emits marked test output in `Output/TextMessages/Message`. The parser accepts that measured transport and `Output/StdOut`, and requires exactly one marker in the sole passing acceptance row.
- JSON Schema enforces the closed result shape and unique code strings. Consumers must additionally check ordinal code-array ordering, which standard JSON Schema cannot compare; the focused schema test exercises both parts of that contract.


## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | high | patch | A file named `signable-payload` and the payload check both use `manifest:signable-payload`; the second `Record` throws. Separate file and payload identities. Combining multiple manifests is not a contracted requirement. |
| B2 | false | reject | An empty manifest evaluates its recomputed empty signable payload, so its ledger is not empty. The contract requires at least one evaluated assertion, and does not require a manifest file row or define an empty-manifest blocker. |
| B3 | medium | patch | `Encoding.UTF8` replaces lone surrogates, so two malformed signed roles can encode to the same bytes. Strict UTF-8 encoding must reject malformed declarations. |
| B4 | high | bad_spec | The comment-stripping regex accepts declarations in raw strings and inactive `#if` regions. Both the C# root checker and generator therefore accept inert text as a root. The non-frozen implementation design needs explicit lexical recognition. |
| B5 | high | patch | The result schema has no PASS condition; empty, failed, or skipped ledgers and nonempty blockers can accompany PASS. Add conditions matching the contracted final-ledger invariant. |
| B6 | low | reject | Whole-row uniqueness allows repeated keys. However, the ledger intentionally retains a skip and an attempted evaluation of the same identity for diagnostics, and this story ships no result producer or consumer. Requiring global key uniqueness would reject that diagnostic shape and add an uncontracted procedural rule. |
| B7 | medium | patch | Python's `$` accepts a trailing LF, so candidate/hash/code/path patterns need exact termination and digest/commit lengths. Add newline rejection regression cases. |
| B8 | medium | bad_spec | The repository-free pair check cannot independently inspect the predecessor candidate; `v2_verified_predecessor` currently accepts the pair without checking its root against the linked predecessor. Add repository-aware consumption checks so the candidate literal is reverified. |
| B9 | medium | patch | The pair's scenario commands are not compared with the committed contract by predecessor consumption. A re-finalized pair with a substituted command is accepted. Check the measured commands against the independently hash-bound contract when consuming Story 10.2. |
| B10 | high | patch | The test executable's deps.json resolves `TestSupport.dll` beside the executable. The generator hashes only the helper project's output. Compare the runtime copy's bytes and candidate stamp with that bound output, rejecting missing or divergent copies. |
| E1 | high | patch | The same assertion identity collision as B1 occurs for the legal file `signable-payload`. Group with B1. |
| E2 | high | bad_spec | The same inert root declaration defect as B4 affects raw strings and preprocessor branches in both validators. Group with B4. |
| E3 | medium | patch | The same replacement-encoding collision as B3 affects malformed UTF-16 role declarations. Group with B3. |
| E4 | maybe-false | defer | The locator checks symlinks before the subsequent read; no deterministic concurrent path-replacement probe was supplied. A probe that swaps and restores a link during the read would establish whether the signed evidence boundary can pass external bytes; possible severity is high, unverified. |
| E5 | false | reject | Design Notes expressly require AC-03 to prove declared-order compatibility against the real immutable v1 attestation. Reading that file satisfies this explicit instruction; replacing it with a temporary facsimile would lose the requested proof. |
| E6 | false | reject | Each mutation row hashes and restores its mutated control/evidence file bytes. The auxiliary Git repository supplies raw diff facts and is disposed after the test; the contract does not declare the Git commit history to be the restored file represented by that row. |
| V1 | medium | patch | The Story 10.2 inserted verification test mocks `v2_generate`. Removing the new designated-spec route still leaves all 18 tests green. Add a real-generator success and stale-artifact rejection test, as in Story 10.1. |
| V2 | high | patch | The Windows TestSupport job omits full history, while the new root test resolves the historical Story 10.1 candidate. The review's disposable depth-one clone reproduces failure at the history assertion. Set that job's checkout `fetch-depth: 0`; shared domain CI already uses full history. |

## Spec Change Log

- **2026-10-12, iteration 1:** B4/E2 and B8 exposed incomplete non-frozen source-recognition and predecessor-consumption designs. The known-bad implementation accepted inactive/string-root text and a root candidate unsupported by the linked predecessor. The correction tasks above require lexical root checks and independent committed predecessor/contract checks. All other verified findings are retained as explicit regression tasks for the same rederivation.
- **KEEP:** Reuse the reviewed implementation from commit `cc7b3b695bd77452b7402423fbc46fc515ef6304` for story-owned source/schema/tests as the starting point, then apply the correction tasks. Preserve role-bearing rows, canonical and declared signable ordering, containment/symlink checks, exact ordinal path equality including rename sources, raw mode gitlink detection, visible history skips, duplicate evaluation rejection, marked TRX transport, twelve restored mutations, closed output schemas, candidate stamping, prior-record verification, deterministic rendering, and current-record rederivation. Preserve Story 10.1 APIs/tests and all 7.4/9.2/10.1 pair bytes. The full reviewed diff is retained locally at `/tmp/story-10-2-diff-mcnhqaj0/changes.diff`; do not restore old generated Story 10.2 record outputs or old lifecycle metadata as source implementation.
