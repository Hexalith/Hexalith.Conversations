---
title: 'Story 10.2: Manifest, hash, ledger, exact-diff, and gitlink invariants'
type: 'feature'
created: '2026-10-12'
status: 'in-review'
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

- 2026-10-14 third-pass review corrections: the shared lexer fixture now has 33 cases, including a `'"'` character literal before a comment-hidden root and before an active root. The allowlist loop rejects `docs/release-evidence` and `DOCS\Release-Evidence` themselves. Generator tests mutate the test-assembly binding and the committed Story 10.1 predecessor's passing and resolvability facts, including a real `--depth 2` clone. The result schema's assertion states are PASS, FAIL and SKIPPED, matching `AssertionLedgerRow`. Removing each guarded clause made its new case fail in both lexers and in the generator. All 31 TestSupport tests and 355 focused Python tests passed. The candidate includes the EventStore gitlink `e88553d`.

- 2026-10-13 review corrections: completed all thirteen accepted patches. Both lexers use the same 21-case embedded fixture; disabled text, verbatim literals, escaped quotes, BOM, and Roslyn whitespace are covered. Allowlist ancestors/globs, uncoded history skips, resolved output segments, nonpassing schema conditions, real raw-diff object-ID decoys, distinct artifact hashes, committed-pair compatibility, and generator trust-rule negatives are verified. The serialized Release solution build passed with zero warnings/errors, all 29 TestSupport tests passed, and the focused Python suite passed 335 tests. Deliberate check-removal probes failed as expected. Windows trailing-dot/space regressions are included for the Windows CI job and were not exercised on this Linux host.

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

- [x] [Review][Patch] A non-PASS result can name no blocker code. `Record(id, false)` and the 10.1 `Equal`/`True` helpers give `FAIL` with `Blockers = []`, and the result schema accepts that. Decided 2026-10-13: tighten the schema now. `FAIL` requires at least one blocker and `BLOCKED` requires a `SKIPPED` ledger row; no new code is added, so Story 10.3's verifier must give every failure a contracted code. Add focused pytest cases. [_bmad/schemas/evidence-boundary-result-v1.schema.json:191]
- [x] [Review][Patch] The signed-evidence allowlist check rejects only entries under `docs/release-evidence`, not entries that cover it. `docs`, `docs/**` and `docs/release-*/proof.json` all pass. Decided 2026-10-13: harden now. Return `SIGNED_EVIDENCE_ALLOWLISTED` for any entry that is an ancestor of `docs/release-evidence` or contains glob characters (`*`, `?`, `[`), in the C# helper and its tests. [tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs:103]
- [x] [Review][Patch] The root lexers still read quotes, apostrophes and `/*` inside inactive `#if` regions, so a nested `#if` can be hidden and the depth goes out of sync. Input `#if false` / `it's` / `#if X` / `'` / `#endif` / root / `#endif` is accepted (Python probe confirmed; the C# lexer has the same structure). In disabled text, recognize directive lines only, as Roslyn does. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:16; _bmad/scripts/generate_story_record.py:5710]
- [x] [Review][Patch] The root lexers treat a verbatim `@""""` (an escaped quote) as the start of a raw string. The text inside a later raw string is then read as code, and a real root after it is swallowed; the probe confirmed both effects. Set `raw` to false when the literal is verbatim, in both implementations. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:93; _bmad/scripts/generate_story_record.py:5772]
- [x] [Review][Patch] The root lexers do not count U+FEFF and U+001A as whitespace before `#`, though Roslyn does. A root inside `﻿#if false` or `\u001A#if false` is accepted. Python also decodes with `utf-8`, not `utf-8-sig`, so a file-leading BOM hides a line-1 directive. Probes confirmed this in both implementations. [tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:78; _bmad/scripts/generate_story_record.py:5700,5864]
- [x] [Review][Patch] The Python and C# lexer tests use different decoy lists, and escape handling is unverified. Python lacks the verbatim, nested-`#if`, `#else` and escaped decoys. The C# escaped decoy still rejects a lexer with its escape branch removed, and deleting the Python escape branch leaves all 236 tests green. Share one decoy list and add `var s = "\"";` followed by a raw string that contains the root. [tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs:67; _bmad/scripts/tests/test_generate_story_record.py:10983]
- [x] [Review][Patch] Several generator trust rules have no negative test, so deleting any one leaves all 236 tests green. They are: the predecessor authority comparison in `v2_10_2_facts`; the `inventory`, `predecessors` and mutation-ledger restoration/order rules in `v2_10_2_pair_errors` (changed consistently in both ledgers); and the committed-source `SupersessionAllowlist = []` sequence in `v2_10_2_root_errors`. Add fault cases for each. [_bmad/scripts/tests/test_generate_story_record.py:10789,10857,10928]
- [x] [Review][Patch] No test pins the committed Story 10.2 pair, and the synthetic mutation rows don't match the real record. Each synthetic row has one observed blocker, but the published `AC-10.2-06-HISTORY` row has two. Tightening the rule to `observed != [expected]` keeps every test green but breaks verification of the published pair. Add `test_story102_committed_pair_still_verifies`, mirroring the 10.1 test. [_bmad/scripts/tests/test_generate_story_record.py:10763]
- [x] [Review][Patch] `HistoryAvailable` reports a skip with no code as a false pass. `fact.Code ?? "UNAVAILABLE_HISTORY_PASSED"` labels a plain skip with the false-pass code, and the public `GitFact` constructor allows a null `Code`. Fall back to the existing GitFacts code `HISTORY_UNAVAILABLE` instead. [tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs:30]
- [x] [Review][Patch] On Windows, a trailing dot or space on a path segment bypasses `EVIDENCE_OUTPUT_FORBIDDEN`. For `bin./proof.json`, `GetFullPath` reads `bin\proof.json`, but the forbidden-segment test looks at the unnormalized segment `bin.`. Check the segments of `Path.GetRelativePath(root, contained.Value)` instead. The Windows TestSupport CI job runs this helper. [tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs:67]
- [x] [Review][Patch] The AC-10.2-05 object-ID decoy is a hand-built `GitDiffEntry` and never goes through `ReadDiffAsync`. Commit a decoy blob whose real object ID contains `160000` (content found offline), so the raw-diff decoy row in the I/O matrix is proven end to end. [tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs:57]
- [x] [Review][Patch] The gitlink rules in the result schema are never really tested. The `gitlink-path` trailing-newline case runs under `PASS`, which already requires empty `gitlinks`, so it passes without the path pattern. No case checks the `anyOf` that requires a `160000` mode. Run both cases under `FAIL`. [_bmad/scripts/tests/test_evidence_boundary_result_schema.py:83]
- [x] [Review][Patch] The story102 generator fixture gives both `.csproj` files the same bytes and both DLLs the same bytes, so a swapped or misattributed project/assembly hash stays green. Use distinct bytes for each file. [_bmad/scripts/tests/test_generate_story_record.py:10719]

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

### Workflow review — corrected candidate, 2026-10-13

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R2-B1 | high | patch | A compiler-verified interpolated format containing an apostrophe exposes a later raw-string root; both scanners return a false pass. Keep format text opaque inside the existing interpolation scanner and add shared positive/negative cases. |
| R2-B2 | low | reject | Escaped identifiers are valid C#, but the real consuming roots use ordinary non-keyword names and the contract matches their canonical declaration tokens. Supporting alternative identifier spellings adds parsing complexity for an unlikely caller. |
| R2-B3 | low | reject | Verbatim/raw constant initializers are valid, but current consuming roots use ordinary quoted digest/commit literals. Supporting alternative initializer encodings adds parsing complexity without a current consumer failure. |
| R2-B4 | false | reject | The retained Equal/True outcomes are BoundaryEvaluation values, not nine-field evidence-result documents. The owner explicitly chose schema-only enforcement and required Story 10.3 to attach contracted codes; Story 10.2 has no serializer that claims those outcomes conform. |
| R2-B5 | low | reject | The shape schema permits mixed FAIL/SKIPPED rows under BLOCKED, but the actual ledger always gives evaluated failures precedence and there is no result producer in this story. Another schema guard adds complexity without a demonstrated caller masking a failure or producing PASS. |
| R2-B6 | low | reject | A FIFO can hang verification, but all current manifest consumers verify controlled regular fixture files. This non-shipping helper has no caller that supplies special files; a portable regular-file guard adds platform-specific complexity for an unlikely input. |
| R2-B7 | low | patch | Reading entire large files allocates an avoidable artifact-sized buffer. Hash a disposed read stream directly; this preserves the measured byte digest and public API. |
| R2-B8 | low | patch | The a/z ordering fixture cannot distinguish ordinal from culture-sensitive ordering. Add Unicode paths whose comparer orders differ to protect the explicit canonical ordering rule. |
| R2-B9 | low | patch | Skip rejects repeated identities, but no regression exercises that behavior. Add a duplicate-skip rejection test that preserves the original diagnostic. |
| R2-B10 | low | patch | Synthetic consumption tests do not exercise the complete Git-backed consumer. Add a real committed-pair consumption check and a real unavailable-history case. |
| R2-E1 | low | reject | The independently reproduced FIFO result is the same rare special-file input as R2-B6. Current manifest callers supply controlled regular files; adding platform-specific guards is not justified for this non-shipping caller set. |
| R2-V1 | medium | patch | The pre-verified caller mutation accepts a forbidden production Reference when the new MSBuild boundary call is removed. Add a facts-level regression with actual project XML and the real reused boundary checker. |

**Review patch verification:** The six accepted follow-ups are implemented. Both scanners keep top-level interpolation format text opaque while preserving grouped/nested expression parsing. The shared fixture now contains 31 cases, including the exact compiler-verified false-pass source. File hashing uses a disposed stream; ordinal culture-sensitive ordering and duplicate-skip behavior have explicit regressions. Actual MSBuild-caller tests reject forbidden production references in both projects, and a disposable local Git repository verifies candidate-bound pair consumption and rejects missing candidate history. Focused C# and Python checks passed; deleting the MSBuild caller made its regression fail as intended.

**Final verification:** Candidate `7d281e613d75eb5ed94e7d8b476971e4debc6e20` passed the serialized Release solution build with zero warnings/errors, all 31 TestSupport tests without skips, 348 focused Python tests, and the exact AC-10.2-01..09 commands with summary `9/9/0/0/0/0`. The six accepted review follow-ups are complete. No new work was deferred. Windows-only path-normalization cases remain for the Windows CI lane; all earlier accepted evidence pairs and the unrelated EventStore checkout were preserved.

### Review Findings — third pass, 2026-10-13

Code review of `bba1119..494df3e`, code only. Out of scope: the generated record pair, this spec, sprint/deferred bookkeeping, and the later EventStore bump `f440a07`. Four layers ran (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor), and none failed. Triage covered 45 findings: 1 decision, 5 patches, 2 defers, 37 rejected. Defers stay here until done, because a post-candidate change to `deferred-work.md` breaks candidate retention.

- [x] [Review][Decision] The Story 10.2 record no longer verifies at HEAD — `f440a07` moved `references/Hexalith.EventStore` from `3d0b738` to `e88553d` after candidate `7d281e6`. `generate_story_record.py --repository . --contract …/10.2.json --verify-inserted-record <this spec>` exits 1 with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT`; both name only that commit (reproduced). Repository-aware consumption by Story 10.3 reads the candidate tree, so 10.3 is not blocked; the completion gate at HEAD is. Every patch below is also a post-candidate source change. Decided 2026-10-14: rebind. Apply the patches, then regenerate the pair on a new candidate that includes the bump.
- [ ] [Review][Patch] Rebind the record: after the patches below, withdraw the `7d281e6` pair, commit the candidate, run the exact AC-10.2-01..09 commands on it, and publish the regenerated pair and inserted record, binding the `e88553d` EventStore gitlink. Preserve all 7.4, 9.2 and 10.1 pair bytes. [docs/release-evidence/story-10.2-final-record-v2.{json,md}]
- [x] [Review][Patch] The top-level character-literal branch of both root lexers is untested. Deleting it leaves all tests green and accepts `char c = '"'; /* "; const string Root = "abcdef"; /* */` as pinned. Add a hidden-in-comment negative case and an active-root positive case to the shared fixture (Verification Gap). [tests/Hexalith.Conversations.TestSupport.Tests/Fixtures/story102-root-declarations.json; lexers at tests/Hexalith.Conversations.TestSupport/CSharpSourceDeclarations.cs:57, _bmad/scripts/generate_story_record.py:5755]
- [x] [Review][Patch] The allowlist's exact-directory clause is untested. Removing `Equals("docs/release-evidence")` keeps the tests green and lets the entries `docs/release-evidence` and `DOCS\Release-Evidence` PASS. Add both to the `exempted` loop (Verification Gap). [tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs:160]
- [x] [Review][Patch] The scenario `outputFiles` and test-assembly binding is untested. Deleting the clause keeps all 316 story101/story102 tests green, and a re-finalized pair whose scenario rows and execution binding name different assemblies then verifies. Add mutations of `projects[1].assembly.sha256` and of one scenario's `outputFiles[1].sha256` (Verification Gap). [_bmad/scripts/tests/test_generate_story_record.py:10893; clause at _bmad/scripts/generate_story_record.py:4732]
- [x] [Review][Patch] The predecessor-resolvability and predecessor-passing clauses of repository-aware consumption are untested. Deleting either keeps the tests green, and a `--depth 2` clone in which the 10.2 candidate resolves but the 10.1 candidate does not is then accepted. Add a `--depth 2` real-Git case and a `predecessor-fail` consumption fault (Verification Gap). [_bmad/scripts/tests/test_generate_story_record.py:10950,11186; clause at _bmad/scripts/generate_story_record.py:5920]
- [x] [Review][Patch] The result schema's assertion `state` enum allows `BLOCKED`, a row state that `AssertionLedgerRow` never produces and that has no defined meaning. Align it with PASS, FAIL and SKIPPED and adjust the `blocked-ledger` schema test (Acceptance Auditor). [_bmad/schemas/evidence-boundary-result-v1.schema.json:184]
- [x] [Review][Defer] The allowlist glob guard rejects `*`, `?` and `[` but not brace expansion such as `{docs,x}/release-evidence/proof.json` [tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs:105] — deferred, maybe-false (unverified medium). No allowlist consumer exists yet; settle when Story 10.3 defines its allowlist matcher. If it expands braces, `{` must be rejected (Edge Case Hunter).
- [x] [Review][Defer] Symlink swap between the containment check and `File.OpenRead` [tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs:82] — deferred, maybe-false. This is the same claim as E4, already in `deferred-work.md`; no new entry was added (Edge Case Hunter).

**Rejected:**

- `rejected` — Uncontracted skip codes (`REVISION_INVALID`, `HISTORY_UNAVAILABLE`) appear in blockers and the HISTORY ledger row (Acceptance Auditor). The `HISTORY_UNAVAILABLE` fallback was decided in pass 1. Defining a contracted skip code would edit this spec.
- `low` — Ordinal blocker ordering is not schema-enforced (Acceptance Auditor). Standard JSON Schema cannot express it, Implementation Notes record that consumers must check it, and the procedural check belongs to Story 10.3's verifier.
- `low` — No test uses a stale, as opposed to all-zero, signable digest (Acceptance Auditor). Both go through the same single ordinal comparison against the recomputed digest, and no plausible regression separates them.
- `false` — The two-argument `Add` changed Story 10.1 semantics (Acceptance Auditor). The 10.1 doc already promised lowercase SHA-256 values, 10.1 surface tests pass, and the default role affects only the new payload API.
- `false` — A decoy `const` in another type pins the root (Blind Hunter, Edge Case Hunter). This is F-B3. Any predecessor drift still requires editing a committed literal that the generator compares with the independently verified digest.
- `false` — The allowlist leg is self-referential and decoy-able (Blind Hunter). The decoy part is F-B3. The contract defines the real allowlist as the empty literal in the consuming source, and its consumer is Story 10.3/10.4.
- `false` — The mutation ledger lacks a mutated hash (Blind Hunter). Rejected in pass 1: the test asserts that the mutated bytes differ.
- `false` — `restored` overstates restoration because GitFixture commits persist (Blind Hunter). This is E6. HISTORY resolves fixed revisions that later commits cannot change.
- `low` — Markdown shows `True`, the heading is shared, and the allowlist text is fixed (Blind Hunter). Rejected in pass 1 as cosmetic.
- `false` — A missing contract is hashed as empty bytes (Blind Hunter). Rejected in pass 1: `v2_generate` validates the committed contract blob first.
- `low` — Schema `not-applicable` is unconstrained, and FAIL/BLOCKED row rules are incomplete (Blind Hunter, Edge Case Hunter ×2). Story 10.2 has no producer, the owner set the FAIL/BLOCKED rules in pass 1, and R2-B5 applies.
- `low` — Duplicate ledger or manifest keys, and no id/subject pattern (Blind Hunter). Rejected in pass 1 (B6, and the trailing-newline and duplicate-path items).
- `rejected` — Skip-only ledgers give FAIL rather than BLOCKED (Blind Hunter, Edge Case Hunter). Rejected in pass 1 per the I/O matrix.
- `low` — `Block` does not validate codes (Blind Hunter, Edge Case Hunter). Rejected in pass 1: callers pass literals and the result still FAILs.
- `low` — The same file is accepted under `/` and `\` spellings (Blind Hunter, Edge Case Hunter). Rejected in pass 1.
- `low` — Generator logic and literals are duplicated (Blind Hunter). A refactor adds no protection, and divergence would surface through the shared 10.1 predecessor checks.
- `false` — The fixture is underused and the `depth` guards are dead (Blind Hunter). The guards are unreachable but change no behavior; no caller diverges.
- `false` — Pytest functions sit after the `__main__` guard (Blind Hunter). Rejected in pass 1.
- `false` — The cross-binding tests use only truthy assertions (Blind Hunter). Probe: with `v2_10_2_pair_errors` disabled, all five tests fail, so generic checks do not mask the 10.2 bindings.
- `low` — `.GetAwaiter().GetResult()` in an async test (Blind Hunter). No analyzer fires under warnings-as-errors and there is no deadlock under xUnit v3. The fix changes the helper signature.
- `low` — A `null` marker payload crashes generation (Edge Case Hunter). It fails closed and is reachable only by editing the test's serializer.
- `false` — `GateError` escapes predecessor consumption (Edge Case Hunter). `main` converts it into a parseable FAIL document, and the predecessor's unguarded blob reads already raise it.
- `low` — Receipt mtime equal to helper mtime (Edge Case Hunter). Rejected in pass 1.
- `false` — A blank `GitFact.Code` throws in `Skip` (Edge Case Hunter). This is a loud failure, and GitFacts never emits blank codes.
- `false` — A successful `GitFact` with a null value (Edge Case Hunter). Rejected in pass 1.
- `false` — A null allowlist entry throws (Edge Case Hunter). This is a loud failure on input that violates the non-nullable `string[]` signature.
- `low` — Windows 8.3 short names (Edge Case Hunter). Rejected in pass 1.
- `false` — An empty manifest passes (Edge Case Hunter). This is B2.
- `false` — Reusing a ledger across `Verify` calls leaves it half-written (Edge Case Hunter). It throws on caller misuse, and combining manifests is not contracted (B1).
- `low` — The macOS `/var` symlinked temp root fails the fixtures (Edge Case Hunter). This is 10.1's linked-ancestor rule, and CI runs only Linux and Windows.
- `low` — Schema paths allow `../x`, absolute paths and backslashes (Edge Case Hunter). This is F-B9.
- `low` — The helper assembly digest `projects[0].assembly` has no cross-binding (Verification Gap, other). Build outputs are not committed, so no independent post-hoc fact exists; binding it would change the scenario output route.

## Spec Change Log

- **2026-10-12, iteration 1:** B4/E2 and B8 exposed incomplete non-frozen source-recognition and predecessor-consumption designs. The known-bad implementation accepted inactive/string-root text and a root candidate unsupported by the linked predecessor. The correction tasks above require lexical root checks and independent committed predecessor/contract checks. All other verified findings are retained as explicit regression tasks for the same rederivation.
- **KEEP:** Reuse the reviewed implementation from commit `cc7b3b695bd77452b7402423fbc46fc515ef6304` for story-owned source/schema/tests as the starting point, then apply the correction tasks. Preserve role-bearing rows, canonical and declared signable ordering, containment/symlink checks, exact ordinal path equality including rename sources, raw mode gitlink detection, visible history skips, duplicate evaluation rejection, marked TRX transport, twelve restored mutations, closed output schemas, candidate stamping, prior-record verification, deterministic rendering, and current-record rederivation. Preserve Story 10.1 APIs/tests and all 7.4/9.2/10.1 pair bytes. The full reviewed diff is retained locally at `/tmp/story-10-2-diff-mcnhqaj0/changes.diff`; do not restore old generated Story 10.2 record outputs or old lifecycle metadata as source implementation.
