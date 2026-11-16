---
title: 'Story 12.2: Freeze the benchmark method and signal-quality algorithm'
type: 'feature'
created: '2026-11-16'
status: 'in-progress'
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
- [ ] Generate the pair, present its digest, and HALT for owner approval. Then regenerate with the approval, verify, and commit after commitlint.
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
- AC-12.2-04 passed 71/71 through the real CLIs, with exact literal blocker expectations, nonempty ledgers and byte restoration. The strengthened generator-approval follow-up also passed (1/1). Both pytest jobs used `TMPDIR=/var/tmp run-with-temp` and system `python3`; receipts are in `artifacts/v9/12.2/`. `python3 scripts/check-root-submodules.py --repository .` and `git diff --check` passed. The protected-path diff is empty. The real index remains untouched until approval regeneration; rerun the staged checks before the eventual commit.

## Spec Change Log

## Review Triage Log
