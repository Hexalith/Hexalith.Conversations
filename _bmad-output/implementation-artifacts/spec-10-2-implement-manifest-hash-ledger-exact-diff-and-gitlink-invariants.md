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

## Review Corrections — Iteration 1

- [x] `tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs` and its tests — separate file and payload identities; reject malformed Unicode with strict UTF-8 encoding and retain the existing declared-order compatibility proof.
- [x] `tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs`, private supporting source parsing if needed, its tests, and `_bmad/scripts/generate_story_record.py` — recognize actual literal declarations outside comments and string/character literals, and conservatively reject roots inside preprocessor conditional regions. Keep real roots unconditional in their consuming source. Prove raw-string and inactive-branch decoys cannot pass in both implementations.
- [x] `_bmad/schemas/evidence-boundary-result-v1.schema.json` and its focused tests — PASS requires a nonempty ledger containing only PASS states and no blockers. Make candidate/hash/code/path/role syntax reject trailing newlines while preserving all four result states and sorted unique code arrays.
- [x] `_bmad/scripts/generate_story_record.py` and story102 tests — require the helper DLL copied beside the test executable to equal the bound helper assembly and carry the same candidate stamp. Missing/divergent copies fail. Reverify the root candidate and exact scenario commands from the committed predecessor/contract during repository-aware Story 10.2 pair consumption; keep repository-free pair checks as structural checks, with no undeclared root literals in Python.
- [x] `_bmad/scripts/tests/test_generate_story_record.py` — add real-generator inserted verification coverage for a valid pair and a changed runtime/bound artifact, without replacing `v2_generate`; mutation of the designated-spec route must make the test fail.
- [x] `.github/workflows/ci.yml` — give the Windows TestSupport checkout full root history with `fetch-depth: 0`.
- [x] Regenerate the pair and inserted record from the corrected committed candidate after withdrawing the superseded Story 10.2 outputs. Preserve all earlier story record bytes.


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

| F-B1 | high | patch | Compiled probes show CR, NEL, LS and PS can delimit directives, but both tokenizers recognize LF only; inactive roots are accepted. Handle C# line terminators consistently in directive and line-comment scanning. |
| F-B2 | high | patch | C# directive splitting fails on NBSP, VT and FF accepted by the compiler. Read the directive's identifier after C# whitespace instead of splitting on a fixed separator list. |
| F-B3 | false | reject | The probe still contains the exact unconditional named literal in the consuming source file, which is the API's declared scope. The generator separately compares the measured values to the independently verified predecessor. A differently valued runtime initializer therefore cannot substitute a different trusted fact. Field/type ownership beyond that source-file contract is not asserted by this API. |
| F-B4 | high | patch | Repository-aware consumption accepts a re-finalized project hash that differs from the claimed candidate's project blob. Recompute both committed project hashes before accepting the predecessor pair. |
| F-B5 | high | patch | Repository-aware consumption accepts a substituted candidate gitlink commit. Compare the whole recorded gitlink inventory with raw tree facts at the claimed candidate. |
| F-B6 | high | patch | Both helper copies can change after all eight receipts while retaining their candidate stamp, and facts still pass. Require every receipt to be at least as new as both helper assemblies; add deterministic timestamp regressions. |
| F-B7 | medium | patch | The schema permits PASS without a resolved candidate. Restrict PASS's candidate to a string while retaining null for unavailable/nonpassing results. |
| F-B8 | medium | patch | PASS can carry recorded mode-160000 gitlinks, despite the shared no-gitlinks assertion rejecting those entries. Restrict PASS's gitlink array to empty and retain diagnostic rows for nonpassing states. |
| F-B9 | low | reject | The shape schema accepts paths whose evidence facts would fail. The current manifest verifier resolves containment, rejects NUL/escapes/outputs, and hashes real files; no result producer consumes this schema in Story 10.2. Duplicating those filesystem rules in regex facets adds complexity without fixing a demonstrated current false pass; actual evidence facts remain the verifier's responsibility. |
| F-B10 | low | reject | The shape schema accepts roles that the strict manifest API rejects before adding rows. No Story 10.2 producer serializes such rows. A second procedural Unicode/whitespace validator would duplicate that guard without a demonstrated current caller failure. |
| F-E1 | high | patch | Compiled `#if(false)` is a valid directive but whitespace splitting misses its keyword in both implementations. Group with F-B2's directive-identifier extraction. |
| F-E2 | high | patch | The same valid non-LF directive boundary defect as F-B1 is reproduced in both implementations. Group with F-B1. |
| F-E3 | high | patch | Omitting conditional tokens concatenates a literal prefix and semicolon across a conditional initializer suffix; the compiled value differs. Insert an opaque conditional-region barrier so declaration sequences cannot join across it. |
| F-V1 | medium | patch | An in-memory removal of the predecessor authority comparison leaves all Story 10.2 tests green and accepts an incompatible re-finalized pair. Add a mismatched authority case to the repository-consumption regression. |

### Review Findings

Code review of `bba1119..674ce1b` on 2026-10-13. It ran four layers (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor), and none failed. This subsection sits inside `## Review Triage Log`, so committing it keeps the published pair's candidate retention.

- [ ] [Review][Patch] A non-PASS result can name no blocker code. `Record(id, false)` and the 10.1 `Equal`/`True` helpers give `FAIL` with `Blockers = []`, and the result schema accepts that. Decided 2026-10-13: tighten the schema now. `FAIL` requires at least one blocker and `BLOCKED` requires a `SKIPPED` ledger row; no new code is added, so Story 10.3's verifier must give every failure a contracted code. Add focused pytest cases. [_bmad/schemas/evidence-boundary-result-v1.schema.json:191]
- [ ] [Review][Patch] The signed-evidence allowlist check rejects only entries under `docs/release-evidence`, not entries that cover it. `docs`, `docs/**` and `docs/release-*/proof.json` all pass. Decided 2026-10-13: harden now. Return `SIGNED_EVIDENCE_ALLOWLISTED` for any entry that is an ancestor of `docs/release-evidence` or contains glob characters (`*`, `?`, `[`), in the C# helper and its tests. [tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs:103]
- [ ] [Review][Patch] The root lexers still read quotes, apostrophes and `/*` inside inactive `#if` regions, so a nested `#if` can be hidden and the depth goes out of sync. Input `#if false` / `it's` / `#if X` / `'` / `#endif` / root / `#endif` is accepted (Python probe confirmed; the C# lexer has the same structure). In disabled text, recognize directive lines only, as Roslyn does. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:16; _bmad/scripts/generate_story_record.py:5710]
- [ ] [Review][Patch] The root lexers treat a verbatim `@""""` (an escaped quote) as the start of a raw string. The text inside a later raw string is then read as code, and a real root after it is swallowed; the probe confirmed both effects. Set `raw` to false when the literal is verbatim, in both implementations. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:93; _bmad/scripts/generate_story_record.py:5772]
- [ ] [Review][Patch] The root lexers do not count U+FEFF and U+001A as whitespace before `#`, though Roslyn does. A root inside `﻿#if false` or `\u001A#if false` is accepted. Python also decodes with `utf-8`, not `utf-8-sig`, so a file-leading BOM hides a line-1 directive. Probes confirmed this in both implementations. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:78; _bmad/scripts/generate_story_record.py:5700,5864]
- [ ] [Review][Patch] The Python and C# lexer tests use different decoy lists, and escape handling is unverified. Python lacks the verbatim, nested-`#if`, `#else` and escaped decoys. The C# escaped decoy still rejects a lexer with its escape branch removed, and deleting the Python escape branch leaves all 236 tests green. Share one decoy list and add `var s = "\"";` followed by a raw string that contains the root. [tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs:67; _bmad/scripts/tests/test_generate_story_record.py:10983]
- [ ] [Review][Patch] Several generator trust rules have no negative test, so deleting any one leaves all 236 tests green. They are: the predecessor authority comparison in `v2_10_2_facts`; the `inventory`, `predecessors` and mutation-ledger restoration/order rules in `v2_10_2_pair_errors` (changed consistently in both ledgers); and the committed-source `SupersessionAllowlist = []` sequence in `v2_10_2_root_errors`. Add fault cases for each. [_bmad/scripts/tests/test_generate_story_record.py:10789,10857,10928]
- [ ] [Review][Patch] No test pins the committed Story 10.2 pair, and the synthetic mutation rows don't match the real record. Each synthetic row has one observed blocker, but the published `AC-10.2-06-HISTORY` row has two. Tightening the rule to `observed != [expected]` keeps every test green but breaks verification of the published pair. Add `test_story102_committed_pair_still_verifies`, mirroring the 10.1 test. [_bmad/scripts/tests/test_generate_story_record.py:10763]
- [ ] [Review][Patch] `HistoryAvailable` reports a skip with no code as a false pass. `fact.Code ?? "UNAVAILABLE_HISTORY_PASSED"` labels a plain skip with the false-pass code, and the public `GitFact` constructor allows a null `Code`. Fall back to the existing GitFacts code `HISTORY_UNAVAILABLE` instead. [tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs:30]
- [ ] [Review][Patch] On Windows, a trailing dot or space on a path segment bypasses `EVIDENCE_OUTPUT_FORBIDDEN`. For `bin./proof.json`, `GetFullPath` reads `bin\proof.json`, but the forbidden-segment test looks at the unnormalized segment `bin.`. Check the segments of `Path.GetRelativePath(root, contained.Value)` instead. The Windows TestSupport CI job runs this helper. [tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs:67]
- [ ] [Review][Patch] The AC-10.2-05 object-ID decoy is a hand-built `GitDiffEntry` and never goes through `ReadDiffAsync`. Commit a decoy blob whose real object ID contains `160000` (content found offline), so the raw-diff decoy row in the I/O matrix is proven end to end. [tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs:57]
- [ ] [Review][Patch] The gitlink rules in the result schema are never really tested. The `gitlink-path` trailing-newline case runs under `PASS`, which already requires empty `gitlinks`, so it passes without the path pattern. No case checks the `anyOf` that requires a `160000` mode. Run both cases under `FAIL`. [_bmad/scripts/tests/test_evidence_boundary_result_schema.py:83]
- [ ] [Review][Patch] The story102 generator fixture gives both `.csproj` files the same bytes and both DLLs the same bytes, so a swapped or misattributed project/assembly hash stays green. Use distinct bytes for each file. [_bmad/scripts/tests/test_generate_story_record.py:10719]

**Rejected:**

- `false` — The mutation ledger lacks a mutated hash (Blind Hunter). `EvidenceBoundaryFaultTests` asserts that the mutated hash differs, and the record requires a passing receipt, so a no-op mutation cannot publish.
- `false` — Ledger `id`/`subject` accept trailing newlines (Blind Hunter). The contract leaves those fields free-form. Iteration 1 hardened only candidate/hash/code/path/role.
- `false` — Spec `done` vs sprint `review` (Blind Hunter, Verification Gap). `review` is the expected state before code review, and this workflow syncs it.
- `false` — story102 tests placed after the `__main__` guard (Blind Hunter). `pytest.main([__file__])` re-imports the module and collects every test. The guard was at the end at baseline.
- `false` — A missing contract is hashed as empty bytes (Blind Hunter). `v2_generate` validates the committed contract blob at the same candidate before `v2_10_2_facts` runs.
- `false` — A successful `GitFact` with a null value throws (Edge Case Hunter). It fails loudly, and `GitFacts` never returns `Ok(null)`.
- `false` — Copy-status sources must be declared (Edge Case Hunter). `GitFacts` forces `diff.renames=true` with no `--find-copies`, so status `C` cannot occur.
- `rejected` — The result class depends on unrelated rows (Blind Hunter). A skip-only ledger returning `ASSERTION_LEDGER_EMPTY` is what the I/O matrix specifies; changing it means renegotiating the spec.
- `rejected` — Spec-only defects: the triage table broken by a blank line, stale test counts, missing iteration-2 correction tasks and `review_loop_iteration`, the `/tmp` KEEP path, candidate-rule wording, and the claim about code ordering (Blind Hunter, Acceptance Auditor, Verification Gap). The fix would edit the spec under review.
- `low` — The same file declared twice through a `/` vs `\` spelling (Blind Hunter, Edge Case Hunter). Nobody writes this in practice, it gives no false pass for a bad file, and dedupe would change the 10.1 `Digests` keys.
- `low` — The Markdown shows Python `True` and fixed allowlist text (Blind Hunter). This is cosmetic: the pair check requires an empty allowlist, and a renderer change would force the published pair to be republished.
- `low` — mtime-based receipt freshness (Blind Hunter). Same-tick rebuilds need coarse timestamps, and the stale case fails closed.
- `low` — Windows 8.3 short names under `docs/release-evidence` (Edge Case Hunter). This is unlikely, and the fix would mean expanding names on the filesystem.
- `low` — `Verify` with an undefined `SignablePayloadOrder` leaves a partly filled ledger (Edge Case Hunter). This is caller misuse, and it already throws.
- `low` — `Block` accepts a null or blank code (Edge Case Hunter). Callers pass literal codes, and the ledger still evaluates to `FAIL`.
- `low` — The schema allows duplicate manifest paths (Edge Case Hunter). The `EvidenceManifest.Add` producer rejects duplicates, and a fix needs a procedural consumer rule.
- `rejected` — The `96ddb61` commit message names candidate `8da53e2` (Edge Case Hunter). That is history; the published record and spec consistently bind `ea1aecd`.

## Spec Change Log

- **2026-10-12, iteration 1:** B4/E2 and B8 exposed incomplete non-frozen source-recognition and predecessor-consumption designs. The known-bad implementation accepted inactive/string-root text and a root candidate unsupported by the linked predecessor. The correction tasks above require lexical root checks and independent committed predecessor/contract checks. All other verified findings are retained as explicit regression tasks for the same rederivation.
- **KEEP:** Reuse the reviewed implementation from commit `cc7b3b695bd77452b7402423fbc46fc515ef6304` for story-owned source/schema/tests as the starting point, then apply the correction tasks. Preserve role-bearing rows, canonical and declared signable ordering, containment/symlink checks, exact ordinal path equality including rename sources, raw mode gitlink detection, visible history skips, duplicate evaluation rejection, marked TRX transport, twelve restored mutations, closed output schemas, candidate stamping, prior-record verification, deterministic rendering, and current-record rederivation. Preserve Story 10.1 APIs/tests and all 7.4/9.2/10.1 pair bytes. The full reviewed diff is retained locally at `/tmp/story-10-2-diff-mcnhqaj0/changes.diff`; do not restore old generated Story 10.2 record outputs or old lifecycle metadata as source implementation.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 10.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `10.2`
- Candidate: `ea1aecd2752ccd544c86d76a5ac9000087b26a90`
- JSON content SHA-256 (all three digest fields zeroed): `e085de463cf95f7b51f03ea98a50a03e098846498893790b0607a17c2fc902cd`

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
| `AC-10.2-01` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-01.trx` | `987b641c6e31580786790334132f711bd9cf79cb8776867e4ebcf086c3728904` |
| `AC-10.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-02.trx` | `4eb2bb009a48a04bc55548edc4fa3eb8ebdc42d73d77084a68313b79e2859d10` |
| `AC-10.2-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-03.trx` | `89df29ddfd84474cbcf77968a5870fdec636963e1af6e49ebe31ebf6690b33bb` |
| `AC-10.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-04.trx` | `e84e0b5060f0324a7ab3a8340bad0bf93ab5d12ae35e01285c3684aec92e2a61` |
| `AC-10.2-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-05.trx` | `270b69c1a692047659a4ee1b6b133a3c59117a5b360b0d22b30ea31d190d4932` |
| `AC-10.2-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-06.trx` | `b652046930b95f04cc7242211fa014d26e1da69c37b540a6c6079b25d296f6de` |
| `AC-10.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-07.trx` | `db93360407606d471fb3aedc99804588a6d522b173df00bb2d6921e345fc7782` |
| `AC-10.2-08` | `0` | `PASS` | `none` | `1` | `artifacts/v9/10.2/AC-10.2-08.trx` | `a89c18706be58a204e1ef4f7a2387125d13d31aa7b6f3aa9334a2b4a2daf36f9` |
| `AC-10.2-09` | `0` | `PASS` | `none` | `9` | none | none |

### `AC-10.2-01`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.ManifestShouldRecomputeContainedFilesAndHashes -trx artifacts/v9/10.2/AC-10.2-01.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-01.trx` | `987b641c6e31580786790334132f711bd9cf79cb8776867e4ebcf086c3728904` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-01#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.ManifestShouldRecomputeContainedFilesAndHashes` | `PASS` |

### `AC-10.2-02`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.GeneratedAndBuildOutputsShouldBeRejected -trx artifacts/v9/10.2/AC-10.2-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-02.trx` | `4eb2bb009a48a04bc55548edc4fa3eb8ebdc42d73d77084a68313b79e2859d10` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-02#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.GeneratedAndBuildOutputsShouldBeRejected` | `PASS` |

### `AC-10.2-03`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.SignablePayloadShouldBeRecomputed -trx artifacts/v9/10.2/AC-10.2-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-03.trx` | `89df29ddfd84474cbcf77968a5870fdec636963e1af6e49ebe31ebf6690b33bb` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-03#0001` | `Hexalith.Conversations.TestSupport.Tests.EvidenceManifestTests.SignablePayloadShouldBeRecomputed` | `PASS` |

### `AC-10.2-04`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.ChangedPathsShouldUseExactSetEquality -trx artifacts/v9/10.2/AC-10.2-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-04.trx` | `e84e0b5060f0324a7ab3a8340bad0bf93ab5d12ae35e01285c3684aec92e2a61` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-04#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.ChangedPathsShouldUseExactSetEquality` | `PASS` |

### `AC-10.2-05`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.GitlinksShouldBeDetectedFromRawModes -trx artifacts/v9/10.2/AC-10.2-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-05.trx` | `270b69c1a692047659a4ee1b6b133a3c59117a5b360b0d22b30ea31d190d4932` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-05#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.GitlinksShouldBeDetectedFromRawModes` | `PASS` |

### `AC-10.2-06`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.AssertionLedgerTests.UnavailableHistoryShouldSkipAndNeverPass -trx artifacts/v9/10.2/AC-10.2-06.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-06.trx` | `b652046930b95f04cc7242211fa014d26e1da69c37b540a6c6079b25d296f6de` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-06#0001` | `Hexalith.Conversations.TestSupport.Tests.AssertionLedgerTests.UnavailableHistoryShouldSkipAndNeverPass` | `PASS` |

### `AC-10.2-07`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.RootsOfTrustAndSignedEvidenceShouldRemainPinned -trx artifacts/v9/10.2/AC-10.2-07.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-07.trx` | `db93360407606d471fb3aedc99804588a6d522b173df00bb2d6921e345fc7782` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-10.2-07#0001` | `Hexalith.Conversations.TestSupport.Tests.BoundaryAssertionsTests.RootsOfTrustAndSignedEvidenceShouldRemainPinned` | `PASS` |

### `AC-10.2-08`

Command: `dotnet tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.TestSupport.Tests.EvidenceBoundaryFaultTests.AllInvariantFaultsShouldFailAndRestore -trx artifacts/v9/10.2/AC-10.2-08.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-08.trx` | `a89c18706be58a204e1ef4f7a2387125d13d31aa7b6f3aa9334a2b4a2daf36f9` |
| `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

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

- Candidate: `ea1aecd2752ccd544c86d76a5ac9000087b26a90`
- Candidate rule: SC-10.2 is HEAD^{commit} at first final-record generation
- Frozen inventory SHA-256: `9195f98c90362ce4035ef2223fcf6eab1cc705d0c7cc7ace47b0de9dc809ca4c`
- Contract SHA-256: `42d040c19e781efede0fd8105b039cf8ecc92eaf6bec6c2e9c2b7d7fe75dcf44`

| Predecessor | Record SHA-256 |
| --- | --- |
| `10.1` | `334858d730c96c0a5a6647c514e631cfe4907756846bf2e0147e11937e221404` |

| Project | Project SHA-256 | Assembly | Assembly SHA-256 |
| --- | --- | --- | --- |
| `tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj` | `9f885c50d869285e6b280b0c135b2c1449cda6f8e863271ff6420924443be925` | `tests/Hexalith.Conversations.TestSupport/bin/Release/net10.0/Hexalith.Conversations.TestSupport.dll` | `ec365588c823235c0777219371173b6cc96f61f81429f3d0b585da25effb0173` |
| `tests/Hexalith.Conversations.TestSupport.Tests/Hexalith.Conversations.TestSupport.Tests.csproj` | `6d93f6ea2dbca4fb89047836f9f10ea633f2996e257830c46eaba2aaef646bf2` | `tests/Hexalith.Conversations.TestSupport.Tests/bin/Release/net10.0/Hexalith.Conversations.TestSupport.Tests.dll` | `b13d92ac8abab764e64244b78fc252378596606d8accd00554f5e5d72cf444a5` |

| Result | SHA-256 |
| --- | --- |
| `artifacts/v9/10.2/AC-10.2-01.trx` | `987b641c6e31580786790334132f711bd9cf79cb8776867e4ebcf086c3728904` |
| `artifacts/v9/10.2/AC-10.2-02.trx` | `4eb2bb009a48a04bc55548edc4fa3eb8ebdc42d73d77084a68313b79e2859d10` |
| `artifacts/v9/10.2/AC-10.2-03.trx` | `89df29ddfd84474cbcf77968a5870fdec636963e1af6e49ebe31ebf6690b33bb` |
| `artifacts/v9/10.2/AC-10.2-04.trx` | `e84e0b5060f0324a7ab3a8340bad0bf93ab5d12ae35e01285c3684aec92e2a61` |
| `artifacts/v9/10.2/AC-10.2-05.trx` | `270b69c1a692047659a4ee1b6b133a3c59117a5b360b0d22b30ea31d190d4932` |
| `artifacts/v9/10.2/AC-10.2-06.trx` | `b652046930b95f04cc7242211fa014d26e1da69c37b540a6c6079b25d296f6de` |
| `artifacts/v9/10.2/AC-10.2-07.trx` | `db93360407606d471fb3aedc99804588a6d522b173df00bb2d6921e345fc7782` |
| `artifacts/v9/10.2/AC-10.2-08.trx` | `a89c18706be58a204e1ef4f7a2387125d13d31aa7b6f3aa9334a2b4a2daf36f9` |

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
| `AC-10.2-06-HISTORY` | `UNAVAILABLE_HISTORY_PASSED` | `REVISION_INVALID, UNAVAILABLE_HISTORY_PASSED` | `67419136e136ce0d705271405063ecd32fdc28045c218814c934c4d830467510` | `67419136e136ce0d705271405063ecd32fdc28045c218814c934c4d830467510` | `True` | `True` |
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
