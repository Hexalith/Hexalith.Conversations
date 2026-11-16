---
title: 'Story 12.2: Freeze the benchmark method and signal-quality algorithm'
type: 'feature'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '07b7bca399f3cf68dbc7eb07f2f6c29a744a4206'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** SM-C2 has no predeclared measurement method or signal-quality rule. A row's verdict can therefore still be shaped after its data is seen. The v1 run declared 30 repetitions and no half-width method, and its rows are unusable under DC-1.

**Approach:** Generate one owner-approved, deterministic method bundle `sm-c2-measurement-method-v2.{schema.json,json,md}` under contract `12.2.json`. It freezes the four rows, warmup, repetitions, retention, warm/cold classification, environment, nearest-rank P95, comparability, the signal algorithm and the exact 1.05 threshold. An independent verifier, a C# test that executes the algorithm, and a restore-checked fault matrix validate it.

**Decisions (owner, 2026-11-16):**
- **Comparability.** The method declares envelope revision `sm-c2-envelope-v2`. Story 12.4 re-measures two sides as a pair: the baseline source (`29def441408becfbbbdc5c59b9af14a7717cb21f` with EventStore `b2d3402552fbadf529c220fcc739da9d06d285fe`) and the candidate.
  - Both sides run on one host, with SDK 11.0.100, runtime 11.0.0, Release, and the fixture basis `f02c4b5d…`.
  - The two sides differ only in their repetition constants.
  - v1 stays immutable provenance, not the comparison input.
  - The owner rules that this runtime change, applied identically to both sides, is not a target change.
- **Repetitions per side.** HP-CREATE 1200, HP-APPEND 400, HP-LIST 200, HP-OPEN 200.
  - They are split evenly over four alternating sessions, B,C,B,C,B,C,B,C. Each session runs 5 warmups, and each sample is 2,000 operations.
  - Samples are pooled per side in session order. No session is dropped or re-run, and a failed session voids the pair.
- **AC-12.2-05 waived.** It is recorded as waived, never PASS. Story 12.3 binds 12.2 by its done spec, its done sprint row, its commit and the approved method pair.
- **AC-12.2-03.** It runs from Debug/net11.0, as disclosed for 12.1.
- **Size.** The spec is kept whole.

## Boundaries & Constraints

**Always:**
- Rows are `HP-APPEND`, `HP-CREATE`, `HP-LIST`, `HP-OPEN`, in that ordinal order. Each row's factor is exactly the decimal `1.05`.
- Every raw sample is retained. There is no outlier rule, trimming, adaptive stopping or re-run selection.
- A cold result never substitutes for a warm one.
- The method is declared before observation. The approval names the owner, binds the presented payload digest, and is never dated after the method's publication.
- Bind Story 12.1 by its done spec, its sprint row reading `done`, commit `455b8dea499fef5b0e5e6f8426badbb047ef3edf`, and the approved decision pair.
- Bind the immutable baseline v1 JSON/MD/xUnit files and the fixture blob `1a43bacc…` at the planning candidate. The baseline's Markdown/JSON fixture-hash disagreement (`4838a5a1…` against `1a43bacc…`) is recorded as an open provenance blocker. It is never repaired.
- Unresolvable history is BLOCKED (exit 2), never PASS.

**Never:**
- Change `src/`, the benchmark fixture, packages, gitlinks, v1 evidence or ADR 0005.
- Compute or alter any verdict.
- Add a cost ceiling, disclosure, approval, correctness rationale or recorded-only substitute.
- Wire CI. CI is BMAD-agnostic.

## I/O & Edge-Case Matrix

| Scenario | Expected result |
| --- | --- |
| Complete method and approved digest | Byte-identical pair on regeneration; verifier PASS |
| Row omitted, reordered or added; required field missing | `SMC2_METHOD_INCOMPLETE` |
| Factor ≠ `1.05`, or a ceiling/disclosure/approval/recorded-only key | `SMC2_THRESHOLD_MUTATED` |
| Trimming, outlier rule, adaptive count or sample deletion | `SMC2_SAMPLE_DISCARDED` |
| Runtime, configuration, concurrency or host drift; cold for warm | `SMC2_ENVIRONMENT_UNCONTROLLED` |
| Percentile, bootstrap, seed or level altered | `SMC2_SIGNAL_RULE_MUTATED` |
| Approval missing or stale | `SMC2_APPROVAL_MISSING` |
| Approval postdated | `SMC2_APPROVAL_POSTDATED` |

</frozen-after-approval>

## Code Map

- `docs/release-evidence/sm-c2-hot-path-baseline-v1.json` (immutable). It holds the rows in CREATE, APPEND, LIST, OPEN order, with `rawMicrosecondsPerOperation` (30 each) and `p95Microseconds`. It also holds `fixture` (5 warmups, 30 repetitions, 2,000 operations per sample, concurrency 1), `environment`, `workloadManifest.commandPaths`, `sourceCommit 29def441…` and `reconstruction.baselineEventStoreCommit b2d34025…`.
- `sm-c2-hot-path-post-v1.json` holds 30 immutable post samples. With the baseline, it is the source of the algorithm self-check vectors.
- `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs`: the constants are at :44-46, nearest-rank `Math.Ceiling(0.95*n)-1` at :445-448, and rows are measured sequentially in one test. The blob is `1a43bacc` at the planning candidate and `f02c4b5d` at HEAD (16.2, `eea3a881`, v2 Ready index seeding).
- Do not touch the fixture. `SmC2BaselineReconstructionValidationTest.cs:68-72` still pins the working tree to `1a43bacc` and has been red since `eea3a881`. It is pre-existing; log it in `deferred-work.md`.
- Reuse from Story 12.1:
  - `_bmad/scripts/generate_derived_key_decision.py`: `canonical` :293, `entry_inventory` :528 (same epics regex and digest), `bind_candidate` :541 (authority-bundle row, planning-candidate blob equality, done-commit spec and sprint check), `finalize`/`approval_valid` :755-783, `FAULT_EXPECTATIONS` and the exit codes 0/1/2, `main` :788. Copy these patterns, and import only stable helpers.
  - `verify_derived_key_decision.py`: `refuse_foreign_output()`, the ledger and report schema.
  - `_bmad/scripts/tests/test_derived_key_decision.py`: the CLI fault harness, test-owned literal blocker maps, and byte restoration.
  - `DerivedKeyLifecycleAdrValidationTest.cs`: the const pins and file-only reads. It compiles internal-only, without a csproj edit.
- Planning candidate `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`. The contract must equal its row in `v9-authority-bundle-v1.json`. Inventory `V9-12.2-ENTRY-v1` = `V8-6.11-AC5`, `…-PROHIBITION-NO-THRESHOLD-CHANGE` and `…-PROHIBITION-NO-ADVERSE-SAMPLE-DISCARD` (`be973a60…`).
- `.gitattributes:15-16` already gives the release-evidence outputs LF. `artifacts/` is git-ignored.

## Tasks & Acceptance

**Execution:**
- [x] `docs/release-evidence/sm-c2-measurement-method-v2.schema.json`: a closed schema, `additionalProperties:false` throughout, with the 14 required fields. `threshold.factor` is const `"1.05"`.
- [x] `_bmad/scripts/generate_sm_c2_method.py`:
  - Frozen method constants, following the Design Notes.
  - The candidate binding from the Boundaries.
  - Row metadata comes from the baseline.
  - The self-check vectors come from the declared algorithm.
  - Deterministic JSON/MD, with approval outside the payload (`--approve-reviewer`, `--decided-at-utc`, `--approved-payload-sha256`).
  - Every Matrix code.
  - `candidate.waivedScenarios`, listing AC-12.2-05.
- [x] `_bmad/scripts/verify_sm_c2_method.py`: recompute everything, check JSON/MD parity, scan for substitute keys, validate the approval, and enforce the postdated rule. Output goes through `refuse_foreign_output`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/SmC2MeasurementMethodValidationTest.cs`, test `MethodShouldBePredeclaredAndDeterministic`:
  - Pin the frozen values and the payload digest.
  - Re-implement P95 and the bootstrap in C# and reproduce the self-check vectors exactly.
  - Assert retain-all and non-adaptive repetitions.
- [x] `_bmad/scripts/tests/test_sm_c2_method.py`: `method_fault_matrix` through the real CLIs. It covers every Matrix row, a postdated approval and unavailable history. Each case checks the exact literal blocker, a nonempty ledger and restored bytes.
- [x] Generate the pair, present its digest, and HALT for owner approval. Then regenerate with the approval, verify, and commit after commitlint.
- [x] Update this spec, `sprint-status.yaml` and `deferred-work.md`:
  - Log the stale v1 fixture pin.
  - Log the 12.4 handoff: the old source must build for net11.0, the fixture constants must follow the method, and the contracted `--baseline` v1 input is provenance only.
  - Record the AC-12.2-05 waiver.

**Acceptance Criteria:**
- Given the contract, when AC-12.2-01 runs twice, then it exits 0 with byte-identical pairs.
- Given the approved pair, when AC-12.2-02 runs, then it reports PASS. Any later payload edit yields `SMC2_APPROVAL_MISSING`.
- Given the pair, when the AC-12.2-03 method runs, then exactly one test passes and the C# self-check equals Python.
- Given each fault, when AC-12.2-04 runs, then the exact blocker occurs and the bytes restore.
- Given the diff, then `src/`, the fixture, v1 evidence, ADR 0005, packages and gitlinks are unchanged.

## Design Notes

- The P95 is nearest-rank: ascending order, index `ceil(0.95n)-1`, no interpolation.
- Signal uses a percentile bootstrap with B=10,000.
  - Each row has its own SplitMix64 seed: the first 8 bytes, big-endian, of SHA-256(`sm-c2-measurement-method-v2|<row>`).
  - For each resample, draw `next() mod n` indices: first nBase from the baseline, then nCand from the candidate.
  - The statistic is d = P95(cand\*) − P95(base\*). Take the nearest-rank 2.5th and 97.5th percentiles of the sorted d. The half-width is (hi−lo)/2.
  - A row is usable iff half-width ≤ 0.05 × baselineP95.
- The verdict stays `postP95 ≤ 1.05 × baselineP95` on the full samples, and it requires usable signal.
- Self-check vectors run v1 baseline against v1 post. They are labeled "algorithm conformance, not a verdict".
- An approval is postdated when its `decidedAtUtc` falls after the committer time (UTC) of the commit that first adds the approved JSON. Before that commit exists, it is postdated when it falls after the time of verification. Story 12.4 must additionally show that the approval precedes its first observation.
- The environment block lists:
  - the host: kernel, CPU model and logical processors;
  - the SDK, runtime and configuration;
  - the runner flags `-parallel none -maxThreads 1`, with concurrency 1;
  - no `DOTNET_*` or `COMPlus_*` overrides;
  - no concurrent dotnet, Aspire or daprd process.

  Story 12.4 records these before it observes anything.

## Verification

- Run the AC-12.2-01, -02 and -04 commands with system `python3`, and `TMPDIR=/var/tmp` for pytest. Expected: exit 0.
- Build Conformance Debug/net11.0, then run the AC-12.2-03 method. The contracted `Release/net10.0` path is unproducible, as disclosed for 12.1.
- Stage the changes, then run `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`.

## Implementation Notes

- Added the closed 14-field method schema, deterministic JSON/Markdown generator, independent source/Git-recomputing verifier, real-CLI fault matrix and file-only C# conformance test. The four rows remain ordinal APPEND, CREATE, LIST, OPEN, each with exact decimal factor `"1.05"` and usable-signal required. No performance verdict is computed.
- The canonical payload binds the v9 contract row and exact entry inventory, immutable planning-candidate v1 JSON/Markdown/xUnit and fixture blob, the done Story 12.1 spec/sprint/commit and approved decision pair, and the `f02c4b5d…` fixture basis. The baseline fixture-hash disagreement remains an explicitly open provenance blocker. AC-12.2-05 is recorded as **waived**, never PASS.
- Python implements SplitMix64 with wrapping unsigned 64-bit arithmetic and exact integer units for the finite decimal input samples. C# independently uses `ulong` and `decimal`; it recomputes canonical payload bytes and all four P95/bootstrap vectors exactly. Those historical vectors are labeled **algorithm conformance, not a verdict**.
- Approval stays outside the payload. The verifier always requires a bound named approval on its contracted invocation. `--allow-unapproved` is explicitly a draft-only validation path. The generator checks the presented digest, preserves a still-bound approval on regeneration and rejects a postdated approval. Unavailable or shallow history returns BLOCKED (exit 2).
- The owner approved the presented payload in this session ("yes"), recorded under the existing owner identity Jerome Piquot at `2026-11-16T16:42:29Z`. The signable payload remains `7d6b2d7a6049a80eda5a578ddf097fa5a0eb0a910327dc9227791f2080174bff`. The approved JSON/Markdown pair regenerated successfully and the contracted AC-12.2-02 passed with a nonempty ledger.
- The fresh mapped-source Conformance Debug/net11.0 build passed with 0 warnings/errors. AC-12.2-03 executed exactly one test: 1 passed, 0 failed, 0 skipped. As already approved in this spec, the contracted Release/net10.0 path is not produced. Build preparation used the pinned CLI `dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --workspace "$TMPDIR/mapping" --mode source --output json`; every generated environment entry was passed to the build and test. The build used `--artifacts-path "$TMPDIR/build"`, `-m:1`, `-p:NuGetAudit=false`, and `-p:MinVerVersionOverride=1.0.0`.
- The xUnit native runner changes its working directory to the assembly directory. Private-output test execution therefore sets the test-only discovery origin `SMC2_METHOD_REPOSITORY_ROOT` to the repository root and passes it to the existing shared `RepositoryLocator`. Normal output-directory discovery remains the default. The initial failed root-discovery run is preserved in `artifacts/v9/12.2/attempt-1-*`; the successful build/test, TRX and exact generated environment/commands are retained alongside it. All disposable workspaces and build outputs used `run-with-temp`.
- The stale v1 fixture pin, Story 12.4 paired net11.0/repetition handoff, provenance-only role of `--baseline` and AC-12.2-05 waiver are appended to `deferred-work.md`. No production source, benchmark fixture, v1 evidence, ADR 0005, package configuration or gitlink is changed.

- Before owner approval, AC-12.2-01 ran twice with system `python3`, exited 0 both times and retained byte-identical JSON/Markdown. The draft-only verifier passed with a nonempty ledger. The unsigned contracted AC-12.2-02 initially exited 1 with exactly `SMC2_APPROVAL_MISSING`; after owner approval and regeneration it passed. The initial failure is not counted as a pass.
- AC-12.2-04 passed 71/71 through the real CLIs, with exact literal blocker expectations, nonempty ledgers and byte restoration. The strengthened generator-approval follow-up also passed (1/1). Both pytest jobs used `TMPDIR=/var/tmp run-with-temp` and system `python3`; receipts are in `artifacts/v9/12.2/`. `python3 scripts/check-root-submodules.py --repository .` and `git diff --check` passed. The protected-path diff is empty. After owner approval, the exact ten story paths were staged; the staged root-submodule and whitespace checks passed before the implementation commit.

- Post-approval: AC-12.2-01 regenerated the approved pair twice with byte-identical results. The full source commit message passed the pinned commitlint CLI (successful evidence retained at `artifacts/v9/12.2/source-commitlint.log`). The implementation is committed locally; no remote operation was performed.
- Evidence-boundary transition: **not-applicable**. This routine story has its own scoped method contract and does not request the retired historical evidence-boundary verifier; focused checks follow the current-change runbook.

## Spec Change Log

## Review Triage Log

### Review pass 1 — 2026-11-16

Each row is a verdict on the original finding before grouping.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-01 — older epic field names | medium | The older epic lists `inventoryVersion`, `p95Algorithm`, `signalAlgorithm` and top-level `threshold`; the presented bundle instead uses `schemaVersion`, `percentile`, `signalQuality`, `selfChecks`, and per-row thresholds, with inventory identity bound in `candidate.inventory`. This shape is visible in the exact payload the owner approved in this session. Its semantics and digest stay frozen. Reject the proposed spec-only amendment as prescribed by this workflow; disclose the historical field-name difference here rather than silently claiming those older names exist. |
| BH-02 — permissive schema accepted | medium | `schema_closed({})` returns true and approval metadata is excluded from the payload digest. A valid but relaxed schema can therefore make the verifier assert closure incorrectly. Patch: independently pin the current schema bytes in consuming tooling and retain metaschema validation. Group with EC-01. |
| BH-03 — invalid generator schema crashes | medium | The generator instantiates a validator without `check_schema`; invalid types or a null root can escape the CLI result protocol. Patch: validate shape/metaschema and translate invalid input into `INPUT_UNAVAILABLE`/BLOCKED. Group with EC-04. |
| BH-04 — authority artifact scalar crashes | medium | The artifact comprehension calls `.get` on every entry, while its exception handler does not cover `AttributeError`. Patch: check the object/list/item shapes before member access; preserve the structured nonempty-ledger BLOCKED result. Group with EC-02. |
| BH-05 — verifier reuses calculations | false | The verifier recomputes the expected values from bound raw inputs rather than trusting generated fields, following the predecessor verifier pattern. The file-only C# test separately pins the canonical digest and independently implements nearest-rank and SplitMix64/bootstrap. The claimed common calculation error has no demonstrated surviving outcome across the required gates; duplicating all candidate-binding and algorithm code in another Python module is not needed for this contract. |
| BH-06 — legacy gate values use generic code | medium | A row with `gate: recorded-only` is rejected as incomplete but is a demonstrated legacy substitute deserving `SMC2_THRESHOLD_MUTATED`. Patch: classify those demonstrated gate values and test their literal blocker. |
| BH-07 — bool/integer equality hides classification | medium | Python dictionary equality equates `true` with `1`; the schema rejects it later with the generic incomplete code. Patch: use type-sensitive canonical comparisons and test threshold, retention and environment examples. |
| BH-08 — C# excludes approval | low | The C# lane deliberately verifies payload/algorithm conformance and can run before owner approval. The mandatory separate AC-12.2-02 rejects missing, unnamed, stale and postdated approval. No combined acceptance bypass was shown. Reject adding another approval/time/history gate to this file-only algorithm test; its publication-time implementation would add unnecessary complexity. |
| BH-09 — no committed approval regression | medium | The existing future-year case uses an unborn fixture repository, so it never exercises first publication. Patch: use real publication history and reject a later approval that is already before verification time through both CLIs. Group with VG-01. |
| BH-10 — runner/session comparability | maybe-false | The approved method fixes fixture basis, same-host/runtime/configuration/concurrency, runner flags and no concurrent processes, but does not enumerate runner version or process lifecycle separately. No paired Story 12.4 observation exists to demonstrate divergent behavior. Defer as medium/unverified: Story 12.4 should bind the actual runner binary/version and session invocation/process lifecycle and prove both sides use the same choices before observation. |
| EC-01 — empty approval schema accepted | medium | Same verified schema-closure defect as BH-02; patch the consuming schema trust pin and cover the empty approval/digest subschemas. |
| EC-02 — nonobject authority row | medium | Same verified authority shape defect as BH-04; guard null/scalar/array rows and check real-CLI reports. |
| EC-03 — nonobject predecessor decision | medium | `decision.items()` runs even after predecessor byte drift has been recorded, so an array/scalar causes `AttributeError` and prevents a report. Patch: validate the decision's object shape in its existing evaluated-input handler and test the exact incomplete blocker with restoration. |
| EC-04 — invalid generator schema root | medium | Same invalid-schema CLI defect as BH-03; cover null root and invalid schema type in both CLIs. |
| VG-01 — publication lookup can regress undetected | medium | Pre-verified reviewer mutation removed publication lookup while the future-year test remained green. Patch the historical real-CLI fixture as in BH-09. |
| VG-02 — foreign-output test exercises another guard | medium | Pre-verified reviewer mutation removed `refuse_foreign_output` while the existing protected-input case stayed green because `write_outputs` separately protects `POST_PATH`. Patch: test unrelated untracked text and foreign-schema JSON outputs and byte preservation. |

Surviving patches are eight groups: schema trust, malformed schema, authority shapes, predecessor shape, legacy gate classification, type-sensitive comparisons, historical approval regression, and foreign-output regression. Their fixes alter tooling/tests only and preserve the approved payload/schema bytes. No frozen intent change or specification re-derivation is needed.

### Review resolution and completion

- All eight patch groups are fixed in the generator, verifier and CLI tests. Both consuming tools pin the exact approved schema bytes (`73238d2795ea20f19f0918269e6333e160fb9e3894e9ddeaa003f7fa0dae61d8`), reject unusable schemas through the BLOCKED protocol, and preserve semantic blocker classification for legacy gates and type lookalikes. Malformed authority and predecessor inputs now produce structured reports.
- The 37 focused review regressions passed; the complete final AC-12.2-04 matrix passed **108/108**, with no failures or skips. Every applicable matrix row ran through real CLIs, checked literal blocker identities and a nonempty verifier ledger, and restored the fixture bytes. Publication regressions use the existing implementation commit and test approval on both sides of publication time, including a postdated approval already before verification time. Foreign-output regressions preserve unrelated untracked text and foreign-schema JSON.
- The approved JSON, Markdown and schema remain byte-identical to source publication `0cb5cf651e24ac69b9c60335cb0b058355b4c76f`. AC-12.2-01 regenerated the approved pair twice after review fixes; AC-12.2-02 passed against the committed publication and actual approval timestamp. The C# source and its consumed signable payload/v1 inputs did not change during review, so the previously measured exact 1/1 Debug/net11.0 result remains applicable without another native build.
- Owner approval in this session binds the presented bundle, including its explicit `candidate.waivedScenarios` entry for **AC-12.2-05**. That existing owner waiver controls completion: no final-record pair is generated, inserted or reported PASS. Stories 12.3/12.4 consume the approved pair and this lifecycle state as already specified. Evidence-boundary verification remains **not-applicable** under the current-change runbook.
- The runner/session comparability question is recorded as medium/unverified deferred work for Story 12.4. The existing v1 fixture pin and provenance disagreement remain open as already disclosed. No production source, benchmark fixture, v1 evidence, ADR, package configuration or gitlink changed.
- Completion guards: AC-7.3-01 and AC-7.3-02 passed with their exact contracted output paths. Fresh observations were copied into the 12.2 operational logs and the prior 7.3 operational receipt bytes restored. An initial custom-output invocation returned `ARGUMENT_INVALID`/BLOCKED and was corrected to the declared commands before completion. Staged whitespace and root-submodule checks passed. The spec is done; the sprint row moves to `review` as required by the build workflow.

### Review Findings (review pass 2, 2026-11-16)

Diff `07b7bca3..dbf1a1c1`, reviewed by Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor; no layer failed. The 50 normalized findings triaged to 3 decision-needed, 9 patch, 2 defer and 24 rejected; the owner resolved the three decisions on 2026-11-16 (one patch, two defers), leaving 10 patch and 4 defer entries. Numbers in parentheses are the normalized finding IDs.

- [x] [Review][Defer] Epic-required method fields and bindings are absent from the approved payload [`docs/release-evidence/sm-c2-measurement-method-v2.json`] — deferred: owner decision 2026-11-16 keeps the approved payload rather than reopen its approval. The epic's Story 12.2 schema requires `inventoryVersion` (and names `p95Algorithm`, `signalAlgorithm`, a top-level `threshold`, `SC-12.2` and project/result hashes). The approved payload never names the hot-path inventory `sm-c2-hot-path-inventory-v1`: it has 0 occurrences in the method JSON, schema, generator and verifier, while both v1 evidence files carry it. The pass-1 BH-01 verdict says inventory identity is bound in `candidate.inventory`, but that field is the entry inventory `V9-12.2-ENTRY-v1`. The hot-path inventory is bound only through the immutable v1 baseline digest and the ordinal rows; Story 12.4 must bind `sm-c2-hot-path-inventory-v1` explicitly. (blind-hunter 4, acceptance-auditor 39; medium)
- [x] [Review][Patch] The new 12.2 evidence test, and 12.1's `DerivedKeyLifecycleAdrValidationTest`, run in GitHub CI's internal conformance step despite its "BMAD agent work only" comment; add `-class-` exclusions for both (owner decision 2026-11-16) [`.github/workflows/ci.yml:335`] (acceptance-auditor 42; medium)
- [x] [Review][Defer] The comparability block under-declares what differs between the paired sides [`docs/release-evidence/sm-c2-measurement-method-v2.json`] — deferred: owner decision 2026-11-16; the existing ruling already applies the runtime change identically to both sides, so Story 12.4 declares and binds the overlay instead of reopening the approval. `comparability.fixtureOverlayDifference` says "Only repetition constants differ between the two sides", but `repetitions.perSide` is identical for both sides. The baseline source `29def441` pins SDK 10.0.302 with no `test.runner`, while HEAD pins SDK 11.0.100 and `"test": {"runner": "Microsoft.Testing.Platform"}`; `Directory.Build.props`, `Directory.Packages.props` and the IntegrationTests project also differ. The baseline side therefore needs a retarget and runner overlay that the method neither declares nor binds, and the tool/version envelope member is absent. This verifies the pass-1 BH-10 concern. (blind-hunter 12, acceptance-auditor 45 and 46; medium)
- [x] [Review][Patch] Unguarded UTF-8 decodes escape the result protocol: a non-UTF-8 live 12.1 spec or sprint file crashes the verifier (traceback, exit 1, no report) and makes the generator report `OUTPUT_WRITE_FAILED` for an input fault [`_bmad/scripts/generate_sm_c2_method.py:193`] (blind-hunter 1, edge-case-hunter 18 and 19, verification-gap 36, acceptance-auditor 48; medium)
- [x] [Review][Patch] No test verifies the committed approved JSON/Markdown pair; its approval block and Markdown can drift with every lane green [`_bmad/scripts/tests/test_sm_c2_method.py:310`] (blind-hunter 8, verification-gap 31; medium)
- [x] [Review][Patch] Binding guards that leave no payload trace have no fault case: the live 12.1 sprint row, the authority-row digest, the epics' declared inventory digest and planning/basis commit replacement [`_bmad/scripts/generate_sm_c2_method.py:158`] (verification-gap 32; medium)
- [x] [Review][Patch] `json:canonical` and the `markdownSha256` part of `markdown:parity` are never the deciding condition in any test [`_bmad/scripts/verify_sm_c2_method.py:158`] (verification-gap 34; medium)
- [x] [Review][Patch] The `ARGUMENT_INVALID` path guards and the draft-only `--allow-unapproved` condition are untested [`_bmad/scripts/verify_sm_c2_method.py:112`] (verification-gap 35; medium)
- [x] [Review][Patch] `FIXTURE_BASIS_COMMIT` is the abbreviation `eea3a881`; use the full `eea3a88146d5d910be2fa17a5f71cdf7aecef978` [`_bmad/scripts/generate_sm_c2_method.py:42`] (blind-hunter 15, edge-case-hunter 21, verification-gap 37, acceptance-auditor 49; low)
- [x] [Review][Patch] The test helper can accept a previous test's report when the verifier writes none [`_bmad/scripts/tests/test_sm_c2_method.py:36`] (edge-case-hunter 27; low)
- [x] [Review][Patch] The verifier docstring and ledger subject claim independence it lacks: it imports the generator's constants and calculations, and algorithm independence comes from the C# lane [`_bmad/scripts/verify_sm_c2_method.py:2`] (blind-hunter 16, acceptance-auditor 41; low)
- [x] [Review][Patch] The AC-12.2-03 TRX (16:08:49Z) predates the owner approval (16:42:29Z) and publication `0cb5cf65` (16:44:12Z); rerun it at the final commit [`tests/Hexalith.Conversations.Conformance.Tests/SmC2MeasurementMethodValidationTest.cs:30`] (blind-hunter 6; low)
- [x] [Review][Defer] The `f02c4b5d…` fixture basis seeds a version 2 tenant index (`SchemaVersion`, `Lifecycle`, a 100-entry `Watermarks` map) that the baseline source `29def441` never models [`tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs`] — deferred: medium, unverified (blind-hunter 13). Settle it in Story 12.4 by compiling the basis fixture against `29def441`, confirming the baseline HP-LIST/HP-OPEN paths read the same documents, and measuring the extra watermark deserialization or showing it immaterial before observation.
- [x] [Review][Defer] The environment rules are literal where Story 12.4 must interpret them [`docs/release-evidence/sm-c2-measurement-method-v2.json`] — deferred: medium, unverified (blind-hunter 14). `DOTNET_ROOT` is set on this host and matches the banned `DOTNET_` prefix, `dotnet` also names the runner and build servers, and no process is named `Aspire`. Settle it in Story 12.4 by running a paired session with `DOTNET_ROOT` unset and build servers shut down, recording the exact process set; if the runner cannot start, the owner must interpret the rule before observation.

#### Rejected

- 2, 24 (low): no lower bound on `decidedAtUtc`. The spec defines only the postdated bound; a lower bound is a new rule with no matrix code and needs a mistyped or falsified timestamp.
- 3, 43 (low): any non-blank approver passes. The committed approval names the owner; pinning an identity adds a rule, as 12.1 also does not.
- 5, 30, 38 (false): the `review` sprint row is the lifecycle stage awaiting this review, which moves it to `done`; 12.1 was bound the same way, by its done-marking commit `455b8dea`.
- 7 (false): `artifacts/v9/12.2/` is ignored, but AC-12.2-05, the final-record vehicle for durable receipts, is owner-waived; the routine runbook needs no tracked receipts, and 12.1 under the same waiver tracks none.
- 9 (low): live 12.1 bytes must equal `455b8dea`. This is the spec's "done spec … sprint row reading `done`" binding copied from 12.1; going stale after done is the project's accepted freeze-check behavior.
- 10 (low): the listed field-level gaps are enforced by whole-group canonical equality plus the pinned all-const schema; its CLI items are carried by the patch for finding 35.
- 11 (false): the method encodes the epic's frozen rule: half-width within 5% and a point-estimate verdict. `requiresUsableSignal: true` means an unusable row cannot pass, and verdict mapping belongs to 12.4; this story must not compute verdicts.
- 17 (false): `ReadBoundSamples` paths come from the signable payload, and its canonical digest is asserted (line 45) before the reads (line 114). The repository-root variable is a documented, test-only discovery origin.
- 20 (low): a reformatted live 12.1 row fails the regex. The known `sprint_status.py` wrap is avoided with the documented width-pinned wrapper, and a tolerant parser adds a branch.
- 22 (false): the Design Note's limit is the commit that first adds the approved JSON; on linear history the reversed walk is topological, so the selected commit is the first-adding one.
- 23 (false): the repository keeps linear history; with no merge commits, path-limited simplification never prunes a commit that changes the path.
- 25 (low): `RecursionError` requires pathologically nested committed evidence.
- 26 (low): an unlisted gate synonym still fails, with the row-metadata `SMC2_METHOD_INCOMPLETE`; only the code differs from the classified matrix values.
- 28 (false): no `init.defaultRefFormat` is configured, so the fixture uses a loose ref that `restore` deletes; a leaked ref would also not change any later expectation.
- 29 (low): `SMC2_METHOD_REPOSITORY_ROOT` must be deliberately set to an empty string.
- 33 (low): first versus last publication matters only with two approved publications of one digest; exactly one exists, and the test would need constructed history.
- 40 (low): in the tracked repository `write_outputs` refuses to replace the approved JSON, so a stale approval is not lost silently; the verifier is the approval gate, and generator constant drift fails the pinned schema.
- 44 (low): ADR 0005 integrity is enforced by 12.1's lane (`DerivedKeyLifecycleAdrValidationTest` and the committed-decision test); 12.2 binds 12.1 by the decision pair as specified.
- 47 (low): AC-12.2-03 passes or fails by exit code; blocker codes in Shouldly messages add structure without changing an outcome.
- 50 (rejected): `review_loop_iteration: 0`; the fix edits the spec under review.


### Review pass 2 resolution — 2026-11-16

- Resolved all ten patch entries. The owner-authorized internal CI exclusions now keep `DerivedKeyLifecycleAdrValidationTest` and `SmC2MeasurementMethodValidationTest` in BMAD agent work. No CI evidence gate was added. Malformed UTF-8 in the predecessor spec or sprint now produces `INPUT_UNAVAILABLE`/BLOCKED (exit 2), with a fresh structured verifier report and nonempty ledger. The fixture basis uses full commit `eea3a88146d5d910be2fa17a5f71cdf7aecef978`. The verifier documentation and ledger accurately state that Python shares generator calculations and C# supplies algorithm independence.
- Added 22 focused regressions. The complete AC-12.2-04 matrix passed **130/130**, with no failures or skips. New cases validate the actual repository-approved JSON/Markdown pair, the live predecessor sprint row, authority and declared inventory digests, and planning/basis commit replacement through real CLIs. Isolated mutations make `json:canonical` and the Markdown digest guard the sole failing assertions. All six contracted input/output argument guards and draft-only approval handling are exercised. The helper removes the previous report before each invocation and rejects an invocation that produces none. Fault cases restore fixture bytes and Git replacement/publication refs. Receipts: `artifacts/v9/12.2/pass2-focused.xml` and `AC-12.2-04.xml`.
- Reran AC-12.2-03 after approval and publication, against the final implementation working tree at HEAD `a321b0f04624cb1707e2b982b4cdd992402b895b`. A fresh mapped-source Debug/net11.0 build completed with **0 warnings and 0 errors**; the native run at `2026-11-16T20:02:01Z` executed exactly **1 test, 1 passed, 0 failed, 0 skipped**. The approved Debug/net11.0 path disclosure still applies. `artifacts/v9/12.2/pass2-AC-12.2-03-command.json` records the full HEAD, input and assembly digests, all commands, the complete generated mapping environment and UTC timestamps; the adjacent build log, native log and TRX retain the observations. All private mapping/build outputs were removed by `run-with-temp` after child work finished.
- AC-12.2-01 regenerated the approved pair twice with byte-identical results. The schema and pair remain byte-identical to publication `0cb5cf651e24ac69b9c60335cb0b058355b4c76f`; signable payload `7d6b2d7a6049a80eda5a578ddf097fa5a0eb0a910327dc9227791f2080174bff` and owner approval are unchanged. Contracted AC-12.2-02 passed with required approval and **30 assertion-ledger entries**. The scoped CI release-boundary regression passed **1/1**. `pass2-regeneration.log`, `pass2-python-acceptance.json` and `pass2-ci-policy.xml` retain these results under `artifacts/v9/12.2/`.
- Protected-path, whitespace and root-submodule checks passed. No production source, fixture, v1 evidence, ADR 0005, dependency configuration or gitlink changed. AC-12.2-05 remains **waived**, never PASS. The four owner-dispositioned defers remain in `deferred-work.md` for Story 12.4; the stale v1 fixture pin and immutable provenance disagreement remain open. Evidence-boundary verification remains **not-applicable** under the current-change runbook.

- During this run, a separate local change advanced HEAD to `f1a2b3da468760d4188f604045deb098af8ead52`. That committed change is preserved outside the five Story 12.2 patch paths. Repeated the fresh mapped-source Debug/net11.0 build and exact AC-12.2-03 run against this HEAD: build passed with 0 warnings/errors; exactly 1 test passed, with 0 failed/skipped. The latest `pass2-AC-12.2-03-command.json`, build/native logs and TRX identify this current candidate; earlier observations are preserved under `artifacts/v9/12.2/pass2-before-concurrent-commit/`.

### Build verification review — 2026-11-16

All three required layers completed. The saved-baseline diff includes the separate bounded-read commit `f1a2b3da468760d4188f604045deb098af8ead52`; its production hunks are preserved and are not Story 12.2 patches. Each finding is classified before grouping below.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-01 — restricted client accepts a rich legacy response | medium | `ConversationClient.GetConversationAsync` sends the mode but delegates response acceptance to generic `SendAsync`, without checking purpose or bounded fields. Defer the compatibility failure to the separately committed bounded-read change. |
| BH-02 — invalid or repeated restriction parameters select rich detail | medium | `ConversationReadApi` enables each flag only for one exact `true` value; malformed/repeated values therefore reach ordinary detail. Defer to the separately committed bounded-read change; group with EC-03. |
| BH-03 — advisory detail depends on native detail availability | medium | `ConversationQueryHandler.GetAsync` reads and gates native detail before selecting the advisory branch, so healthy isolated advisory metadata cannot overcome a missing/rebuilding native result. Defer to the separately committed bounded-read change. |
| BH-04 — advisory metadata receives unrelated native freshness | medium | The advisory handler accepts Stale metadata and returns metadata without its freshness, while the detail mapper supplies `source.Freshness` from another read. Defer to the separately committed bounded-read change; group with EC-02. |
| BH-05 — legacy HTTP detail lacks verified caller authority | maybe-false | The dispatcher permits authority-free envelopes and the new detail adapter rejects them when HTTP context exists. Whether an actual supported production legacy caller still uses that combination requires the bounded-read owner's transport contract and a live HTTP dispatch fixture. Defer as medium/unverified. |
| BH-06 — global administrators lose ordinary detail access | maybe-false | The adapter explicitly rejects `IsGlobalAdmin` outside its direct-call bypass. Whether an administrator with normal tenant Read permission must arrive with that assertion requires the owner's authenticated-caller policy and an admission test; defer as medium/unverified. |
| BH-07 — schema-only production regression coverage | medium | The separate commit adds the catalog payload-shape check; its existing runtime fixtures do not select the new response modes or active HTTP authority branch. Defer its native/advisory/authority coverage gaps with VG-01–04. |
| BH-08 — escaped lone surrogate crashes verifier serialization | medium | Reproduced through the real CLI in a disposable fixture: an escaped surrogate in `approval.approver` exits 1 with `UnicodeEncodeError` and no report. Patch the existing result boundary and add a real-CLI fault case; group with EC-01. |
| BH-09 — deleted approval adds a rendering blocker | medium | Reproduced through the real CLI: deleting `approval` yields `SMC2_METHOD_INCOMPLETE` as a consequence of unavailable rendering metadata, plus the correct `SMC2_APPROVAL_MISSING`. Patch absent-metadata classification while retaining complete-pair parity checks. |
| BH-10 — hot-path inventory identity is implicit | medium | Carried: pass-2 owner decision keeps the exact approved payload and defers explicit `sm-c2-hot-path-inventory-v1` binding to Story 12.4. The entry inventory remains distinct. No second patch or deferral is created. |
| BH-11 — bootstrap assumes independent pooled samples | maybe-false | The implementation exactly executes the owner-approved sample bootstrap. A synthetic clustered/tied distribution alone does not establish that future paired observations violate its assumptions; session independence and the statistical effect require a representative session/process experiment before Story 12.4 observation. Defer as medium/unverified without altering the approved algorithm. |
| EC-01 — escaped surrogate prevents a structured result | medium | Same real-CLI reproduction as BH-08. Group with that protocol patch. |
| EC-02 — Stale advisory metadata can receive Current trust | medium | Same independently read freshness combination as BH-04. Group with that separate-commit deferral. |
| EC-03 — malformed bounded-mode flags expand the response | medium | Same exact-value parser behavior as BH-02. Group with that separate-commit deferral. |
| EC-04 — Story 12.2 changes production source | false | Every production hunk in the baseline-to-tree diff belongs to the independently committed `f1a2b3da468760d4188f604045deb098af8ead52`. Story 12.2's staged implementation paths contain no production source, fixture, dependency or gitlink change; the other commit is preserved. |
| VG-01 — native membership restrictions lack runtime tests | medium | Pre-verified: current rich-detail/default dispatch tests never select native mode or assert its purpose and withheld fields. Defer the separate commit's missing bounded-response coverage. |
| VG-02 — advisory detail mapping lacks consumer tests | medium | Pre-verified: separate advisory-handler tests observe `Incomplete`, but no checked detail consumer enables advisory mode and verifies rejection or mapping. Defer the separate commit's missing mapping coverage. |
| VG-03 — dispatch fixtures bypass delegated authority checks | medium | Pre-verified: the existing detail dispatch fixture has no HTTP context, so `trustedDirectCall` bypasses the new Projects/scope restrictions. Defer the separate commit's missing active-HTTP authority coverage. |
| VG-04 — HTTP identity conflicts lack rejection tests | medium | Pre-verified: existing API fixtures supply one canonical identity and no conflicting headers/aliases. Defer the separate commit's missing conflict and matching-alias cases. |

The two verifier protocol entries route to patches. The unrelated production behavior and coverage entries, and the unverified sampling concern, route to deferred work. No intent or approved payload change is required.

### Final build verification and completion — 2026-11-16

- Fixed both additional protocol groups. Whole-bundle UTF-8 encoding is checked immediately after the JSON-object check, before any canonical comparisons or hashing; escaped surrogates in approval or payload strings now produce a fresh `SMC2_METHOD_INCOMPLETE` report with a nonempty ledger. Missing approval metadata produces `SMC2_APPROVAL_MISSING` without a secondary inability-to-render blocker. Complete bundles still require canonical JSON and Markdown parity. Three real-CLI regressions cover these conditions.
- Final AC-12.2-04 passed **133/133**, with **0 errors, 0 failures and 0 skips**. AC-12.2-01 regenerated the approved pair twice byte-identically; AC-12.2-02 passed with all **30 ledger entries** passing and no blockers. The schema and pair remain byte-identical to publication `0cb5cf651e24ac69b9c60335cb0b058355b4c76f`. The focused CI policy test passed **1/1**. Final Python commands, output and byte checks are retained in `artifacts/v9/12.2/final-acceptance.log` and `final-python-acceptance.json`.
- After the verifier patches, repeated the mapped-source Debug/net11.0 build and exact C# conformance run against `f1a2b3da468760d4188f604045deb098af8ead52`: **0 build warnings/errors; exactly 1 test passed, 0 failed/skipped**. The native command record, input/assembly hashes, logs and TRX remain under `artifacts/v9/12.2/`; preceding native observations are preserved in `native-before-final-review/`. Disposable outputs were removed after all child work finished.
- All three review layers completed; both Story 12.2 protocol patch groups are resolved. Findings belonging to the separate bounded-read commit and the unverified sampling assumption are recorded in `deferred-work.md`; the earlier owner-dispositioned Story 12.4 deferrals remain unchanged. No approved method semantics, production code, fixture, v1 evidence, ADR, dependency or gitlink was changed by this run.
- Both independent completion guards, **AC-7.3-01 and AC-7.3-02**, returned **PASS/0**, with no blockers. Their exact commands and observations are retained as `final-AC-7.3-*.{log,json}`; previous operational receipt bytes were restored. The existing owner waiver of **AC-12.2-05** controls the final-record step: no pair is generated or inserted, and the waived scenario is never represented as PASS. Historical evidence-boundary verification remains **not-applicable** under the current-change runbook. The spec is done and the owned sprint row moves to `review` as the build workflow requires.

### Review Findings (review pass 3, 2026-11-16)

Diff `dbf1a1c1..2b8a8648`: the six Story 12.2 paths changed after review pass 2, without the separate commit `f1a2b3da`. Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor all completed. The 33 normalized findings triaged to 2 decision-needed entries (4 findings), 3 patch entries (6 findings), 0 defer and 23 rejected findings. The owner chose the recommended option 1 for both decisions on 2026-11-16, which turned them into patches, and asked for every patch to be applied. Numbers in parentheses are the normalized finding IDs.

- [x] [Review][Decision] The `DerivedKeyLifecycleAdrValidationTest` CI exclusion also removes CI's only literal check of the runtime derived-key formats — the class does more than pin the ADR and decision digests. `EventStoreShouldRemainOnlyWriteAuthority` reflects `ConversationProjectionReadModelKeys` and requires each family's runtime key to equal its decided template, tenant isolation and no undeclared string builder (`DerivedKeyLifecycleAdrValidationTest.cs:132-155`). CI-run Server tests derive their expected keys from the same builders. A mutation such as an uppercase `ResolutionStorageToken` or a changed fence prefix therefore passes CI green, orphaning existing derived state against ADR 0005's identity compatibility. The exclusion also leaves the open Story 12.1 entry "Run `DerivedKeyLifecycleAdrValidationTest.EventStoreShouldRemainOnlyWriteAuthority` in CI once the `ci / conformance` job builds again" (`deferred-work.md:1242`) and the class remark "safe in CI" contradicted. The pass-2 owner decision excluded the class as BMAD evidence work and did not address this runtime product guard. [`.github/workflows/ci.yml:337`] (verification-gap 1, blind-hunter 9, edge-case-hunter 21; medium) — resolved: owner decision 2026-11-16, option 1. The exclusion stays; a CI-run Server.Tests key-format test was added and the 12.1 entry superseded.
- [x] [Review][Decision] Evidence Git reads honor `refs/replace`, so a same-tree replacement can defeat the postdated-approval rule invisibly — `records.run_git` sets no `--no-replace-objects` or `GIT_NO_REPLACE_OBJECTS`. That is unlike the project's other evidence tooling, e.g. `verify_evidence_boundary.py:464,477` and the Story 7.1 and V28 publishers. Blind Hunter reproduced it in a disposable fixture: replacing publication `0cb5cf65` with a same-tree commit whose committer date is later changed `approval_postdated`'s `%cI` limit and turned `SMC2_APPROVAL_POSTDATED` into PASS. The pass-2 `planning-history-replaced` and `fixture-history-replaced` cases fail only because their chosen replacements change file content. The fix is a choice: ignore replacement objects (the project convention, which turns those two fault cases into "replacement ignored"), or refuse with BLOCKED when any replacement ref exists. [`_bmad/scripts/generate_sm_c2_method.py:96`] (blind-hunter 4; medium) — resolved: owner decision 2026-11-16, option 1. Both tools ignore replacement objects, following the project convention.
- [x] [Review][Patch] The generator still reports an unencodable approval as `OUTPUT_WRITE_FAILED`/BLOCKED, while the verifier reports `SMC2_METHOD_INCOMPLETE` for the same input. Two inputs trigger it: an existing approval whose `approver` is `"\ud800"`, which `approval_valid` accepts, and a non-UTF-8 `--approve-reviewer` argv byte, which Python turns into a lone surrogate. `finalize` → `markdown.encode()` raises `UnicodeEncodeError`, a `ValueError` subclass, and `main` labels it as an output-write failure. The new `approval-surrogate` case runs only the verifier. Classify the encoding failure as `SMC2_METHOD_INCOMPLETE`/FAIL in the generator too, and assert the generator's code in that case. [`_bmad/scripts/generate_sm_c2_method.py:442`] (verification-gap 2, blind-hunter 3, edge-case-hunter 22, acceptance-auditor 26; medium)
- [x] [Review][Patch] `test_method_fault_matrix_repository_approved_pair` checks only that the verifier passes, so the committed approval can still be rewritten with every lane green. Acceptance Auditor reproduced it: approver `"Not The Owner"` and `decidedAtUtc` `2020-01-01T00:00:00Z`, re-rendered with `gen.finalize`, still pass the contracted verifier, and regeneration keeps the rewrite. The C# lane pins only the signable payload. Also pin the repository JSON/Markdown bytes to their publication `0cb5cf65` blobs (`bdb9601a…`/`e80361fc…`); the spec already records them as byte-identical. This is a byte pin, not the identity rule rejected in pass 2 (3, 43). [`_bmad/scripts/tests/test_sm_c2_method.py:455`] (acceptance-auditor 25; medium)
- [x] [Review][Patch] The test's text writes depend on the locale. The new epics write at line 493 (186 non-ASCII characters) and `write_bundle`'s JSON/Markdown writes use `write_text` without an encoding, while the inputs are decoded as UTF-8 and other `_bmad/scripts/tests` files pass `encoding=`. Under a non-UTF-8 locale on Python 3.14, the inventory-digest case raises `UnicodeEncodeError` before reaching its guard. Pass `encoding="utf-8"`. [`_bmad/scripts/tests/test_sm_c2_method.py:493`] (edge-case-hunter 20; low)

#### Rejected

- 5, 19, 30 (low): when the approval key is missing, the Markdown parity check is skipped (and under `--allow-unapproved` with an earlier semantic blocker, the deleted key goes unreported). The result is FAIL either way, so this only under-reports; reporting it needs a new branch for a deliberately deleted key.
- 6 (low): the UTF-8 encodability check reuses the `json:canonical` ledger ID. The row's subject ("JSON strings must encode as UTF-8.") already tells it apart. The build-verification review deliberately classified escaped surrogates anywhere in the bundle as `SMC2_METHOD_INCOMPLETE`, and splitting the classification needs another branch.
- 7, 18, 27 (low): a non-UTF-8 Story 12.1 spec is reported as BLOCKED/`INPUT_UNAVAILABLE` even though its byte drift was already detected. Both outcomes block with a nonzero exit and name the undecodable input. This is not met in everyday use, and reporting FAIL here needs a new branch around the pass-2 classification.
- 8, 29 (low): the helper now deletes the report before each run, so the verifier's overwrite of its own earlier report is no longer exercised. A regression that refuses every existing output would fail loudly on the next real AC-12.2-02 rerun, and under-refusal stays covered by the foreign-output cases.
- 10 (false): the overlay entry states that it upgrades "the BH-10 runner entry above from unverified", so no reader is left with conflicting states, and the ledger convention leaves original entries unchanged.
- 11 (false): the cross-pass references are disambiguated in place ("pass-1 BH-01", "the BH-10 runner entry above"). The build-verification BH-10 created no ledger entry.
- 12, 32 (false): the ledger header defines an entry's source as "where it came from", which is the 12.2 build-verification review. Every bounded-read entry names the separate commit `f1a2b3da` as being outside this story, and that commit names no story. AC-5 was already settled by build-verification EC-04.
- 13, 23 (rejected): the pass-2 triage counts mix entries and normalized IDs. The fix edits the spec under review.
- 14, 33 (rejected): the spec names `f1a2b3da` and `native-before-final-review/` for the latest native receipt. The retained `pass2-AC-12.2-03-command.json` actually binds the final commit `2b8a8648` (started 20:30:03Z), with the earlier run in `native-before-local-completion/` and `post-commit-validation.json` alongside, so the evidence is stronger than the text says. The fix edits the spec under review.
- 15 (low): the verifier's `--output` is optional and its path unguarded. Omitting it prints the result without a report, and refusal of foreign outputs stays enforced. Guarding the path adds a check for a deviation from the contracted command that no one makes in practice.
- 16 (low): the source-binding cases assert the blocker code, not which guard fired. Each fault still fails closed with the exact code, and asserting messages adds coupling for a masked-guard case no one meets in practice.
- 17 (rejected): the spec has extra blank lines. The fix edits the spec under review.
- 24 (false): eight merge commits do exist, contradicting the "no merge commits" wording of rejections 22 and 23. However, none comes after publication `0cb5cf65`, and the runbook's linear-history requirement bars future merges on `main`, so the claimed pruning cannot occur. Fixing the wording would edit the spec under review.
- 28 (rejected): the Implementation Notes still say "independent … verifier". Pass-1 BH-05 adjudicated the independence claim, and the fix edits the spec under review.
- 31 (false): `test_method_fault_matrix_report_must_be_fresh` is a harness guard for the freshness every fault case relies on, not a fault case. Every real fault case still asserts its literal blocker.

### Review pass 3 resolution — 2026-11-16

- **Derived-key product guard (decision 1, option 1).** `DerivedKeyLifecycleAdrValidationTest` stays excluded from CI. `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelKeysTest.cs` runs in the CI unit lane. It pins all seven families to literal keys, which were computed from the decided templates rather than by the builders, for identities that exercise every base64url rule. It also checks that the six tenant-scoped families differ across tenants. It reads no BMAD artifact. The ADR and decision digest pins and the undeclared-builder reflection check stay in the BMAD-only conformance lane. A closure in `deferred-work.md` supersedes the Story 12.1 "run it in CI" entry. The Server project and its key builders are unchanged.
- **Replacement objects (decision 2, option 1).** `generate_sm_c2_method.ignore_replace_objects()` sets `GIT_NO_REPLACE_OBJECTS=1` at the start of both CLIs' `main`. It is set in `main`, not at import, so the pytest process is unaffected. Every Git read of the generator and verifier therefore uses true history. The former `planning-history-replaced` and `fixture-history-replaced` fault cases now assert that replacement is ignored: generator PASS with byte-identical outputs, then verifier PASS. Pass-2 "planning/basis commit replacement" coverage therefore now means "replacement cannot alter a binding". The Story 12.1 generator has the same Git helper exposure; that is out of this story's scope.
- **Patches.**
  - The generator now reports an unencodable approval as `SMC2_METHOD_INCOMPLETE`/FAIL, as the verifier does. The `approval-surrogate` case asserts the generator's result, and a new case covers a non-UTF-8 `--approve-reviewer` argument.
  - `test_method_fault_matrix_repository_approved_pair` requires the repository JSON and Markdown to equal their publication `0cb5cf651e24ac69b9c60335cb0b058355b4c76f` blobs.
  - Every `write_text` call in the test file passes `encoding="utf-8"`.
- **Discrimination check.** With replacement isolation and the new encoding handler temporarily reverted, the four new or changed cases failed (4/4). The generator was then restored byte-identical.
- **Verification.**
  - AC-12.2-01 regenerated the approved pair twice, byte-identically.
  - AC-12.2-02 passed, with all **30 ledger entries** passing and no blockers.
  - AC-12.2-04 passed **134/134**, with **0 errors, 0 failures and 0 skips**. The count is 133 − 2 moved replacement cases + 2 replacement-ignored cases + 1 argument case.
  - The focused CI release-boundary policy test passed **1/1**.
  - The schema and pair stay byte-identical to publication `0cb5cf65`. The signable payload `7d6b2d7a6049a80eda5a578ddf097fa5a0eb0a910327dc9227791f2080174bff` and the owner approval are unchanged.
  - A fresh mapped-source Debug/net11.0 Server.Tests build at HEAD `2b8a8648ce01e8390fbe60f6b4788eed1f81d82a`, with the pass-3 working tree, finished with **0 warnings and 0 errors**. The new class ran **2 tests: 2 passed, 0 failed, 0 skipped**.
  - The C# SM-C2 conformance test and its inputs did not change, so the existing AC-12.2-03 result still applies.
  - Receipts: `artifacts/v9/12.2/pass3-acceptance.log`, `pass3-python-acceptance.json`, `AC-12.2-04.xml`, `AC-12.2-02.json`, `pass3-ci-policy.xml`, and `pass3-server-keys-{command.json,build.log,.log,.trx}`. The earlier receipts are preserved in `before-review-pass3/`.
- **Scope.** No production source, fixture, v1 evidence, ADR 0005, dependency configuration, gitlink or approved artifact changed. AC-12.2-05 remains **waived**, never PASS, and its owner waiver still controls the final-record step. No workflow route changed. Evidence-boundary verification remains **not-applicable** under the current-change runbook. All decision and patch entries are resolved, and the sprint row moves to `done`.
