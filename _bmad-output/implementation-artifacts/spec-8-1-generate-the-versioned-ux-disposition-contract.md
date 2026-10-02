---
title: 'Generate the versioned UX disposition contract'
type: 'feature'
created: '2026-10-02'
status: 'done'
baseline_commit: '92638b8a2d48f12626db13afcb1f45554cbf3683'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No candidate-bound bundle exposes the canonical UX sources' 52 decisions and 28 acceptance criteria. The final-record gate rejects Story 8.1's Python and xUnit commands.

**Approach:** Extend the existing gate, generate a closed schema/JSON/Markdown bundle from the two UX sources, and prove inventory, provenance, and non-activation before recording Story 8.1.

## Boundaries & Constraints

**Always:** Use the exact `8.1.json` commands and frozen inventory. Bind `SC-8.1`, Story 7.4's digest, both source paths/versions/hashes, and ordered 52/28 IDs. Use schema `hexalith.conversations.ux-preservation-disposition.v1` with the PRD's required fields. Set every status to `preserved-not-activated`, label historical mappings non-current, and pass the current-change and insertion gates.

**Never:** Edit UX sources, historical mappings, production UI/runtime, gitlinks, accepted Story 7 records, published contracts, or the v1–v8 prefix. Do not activate UX, claim a historical hold lift or release authorization, or import Story 8.2's mutation matrix.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected behavior | Blocker |
| --- | --- | --- | --- |
| Valid | Bound sources, 52/28 rows, Story 7.4 | Digest-matched bundle; record `7/7/0/0/0/0` | None |
| Drift | Missing/changed source or ID | Reject | Source or inventory drift code |
| Activation | Missing banner, activated row, invalid current story | Reject | `UX_ACTIVATION_UNAUTHORIZED` / `UX_CURRENT_STORY_INVALID` |
| UI change | Production UI path in candidate | Reject | `UX_PRODUCTION_CHANGE_FORBIDDEN` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json`, `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2550`: frozen contract and blockers.
- `_bmad-output/planning-artifacts/ux-requirement-map.md:25`, `_bmad-output/planning-artifacts/ux-design-specification.md:1323`: ordered inventory and source text; read only.
- `_bmad/scripts/publish_v9_planning_authority.py:187`, `_bmad/scripts/generate_preservation_traceability_manifest.py:432`: frozen IDs/parity and extraction patterns.
- `_bmad/scripts/generate_story_record.py:2799,5532,5830`: replace Story 7 retention and Python/xUnit rejection with contract-derived behavior; `_bmad/schemas/story-final-record-v2.schema.json` owns record shape.
- `.github/workflows/ci.yml:65` and governed routes contain the gate and post-`done` verification. Tooling lane passed 572 tests; A-2/A-3 tracking awaits proof.
- `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs:247` forbids future outputs; scope the assertion to its historical candidate.
- `docs/release-evidence/story-7.4-final-record-v2.json`: immutable predecessor.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — finish A-1: contract-derived retention/facts, Python/xUnit support, candidate/build and output binding, gitlink-only staleness fault.
- [x] `_bmad/scripts/generate_ux_preservation_disposition.py`, `docs/release-evidence/ux-preservation-disposition-v1.schema.json` — derive closed, deterministic, source-bound dispositions and blockers.
- [x] `_bmad/scripts/tests/test_generate_ux_preservation_disposition.py` — cover deterministic output, drift, activation, and fixture restoration.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs`, `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs` — implement AC-02–06 and preserve historical assertions.
- [ ] `docs/release-evidence/{ux-preservation-disposition-v1,story-8.1-final-record-v2}.{json,md}`, `_bmad-output/implementation-artifacts/spec-8-1-generate-the-versioned-ux-disposition-contract.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` — produce evidence, insert the record, and close A-1–A-3 tracking only on proof.

**Acceptance Criteria:**

- Given bound sources, when AC-8.1-01 runs twice, then the closed schema/JSON/Markdown bytes and digest match.
- Given frozen inventories, when AC-8.1-02–04 run, then source bindings and ordered 52/28 complete rows pass.
- Given preserved rows and `SC-8.1`, when AC-8.1-05–06 run, then mappings stay non-current and UI changes fail.
- Given six passing results and Story 7.4, when AC-8.1-07 runs, then the record binds all required inputs with `7/7/0/0/0/0`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests` — current tooling lane passes without failed or errored tests.
- CI's `verify_story_completion_workflows.py` AC-7.3-01/02 commands — both pass; post-`done` route checks remain present.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release` — new selectors compile.
- The seven exact commands in `8.1.json` — each exits `0` with nonempty passing evidence; run the generator twice and compare output bytes.
- `python3 scripts/check-root-submodules.py --repository .` — root declaration and gitlink invariants pass.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 8.1 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `8.1`
- Candidate: `d59fe911a1238462719a5ba1c8d359dc64947ecf`
- JSON content SHA-256 (all three digest fields zeroed): `c10664ee2feb324e34490479c9e68de82d8a00c45852d97bd534c77c94831555`

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
| `references/Hexalith.Builds` | `160000` | `3734acbb14d30d06cea401bb26f40b0017702620` |
| `references/Hexalith.Commons` | `160000` | `c13dc6679aa91144b6d541078f3f20019d79c2eb` |
| `references/Hexalith.EventStore` | `160000` | `2c58ffda41759e895ace4b9625c9bd931a217672` |
| `references/Hexalith.Folders` | `160000` | `e88aca956ff24cd6371a5fe3446034c45c70e644` |
| `references/Hexalith.FrontComposer` | `160000` | `bad341fe2b02ed11f982b3dadfbf516d011870f3` |
| `references/Hexalith.Memories` | `160000` | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` |
| `references/Hexalith.Parties` | `160000` | `937cb2a343aaa74963db9bb867a2c3a01ff48677` |
| `references/Hexalith.Projects` | `160000` | `10aba2537c097b6d60906f279775a533f2a72f3a` |
| `references/Hexalith.Tenants` | `160000` | `9bad98d93f3fff34351ed95fe07c7bc0eb7c54be` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-8.1-ENTRY-v1` | `6c61eb92078755496c73506419112026e3e9b7f63bb314b1028d4e9c7bb41ef9` |

## Predecessors

- `7.4`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-8.1-01` | `0` | `PASS` | `none` | `7` | `docs/release-evidence/ux-preservation-disposition-v1.json` | `1d2515fd1c39b0777bbd1bb2cd9274578af707b8784bd21e0a17e235d5a72d38` |
| `AC-8.1-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-02.trx` | `c3719727cf095620054b0e6739a1459db3a70e3161778a5d0dab41c6e4a5e928` |
| `AC-8.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-03.trx` | `37147ead503d677e116858cff6e65718f68c91f32f0731efe3f37437ec1cd272` |
| `AC-8.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-04.trx` | `ee913f3a083cdf34ee12fd6e32962f0b270b8599518f0261c5d8441c4b872c76` |
| `AC-8.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-05.trx` | `882614921a9c65b2d881f8f2c17bc5f5a026e5f32f0115ef9771a36bbc048f44` |
| `AC-8.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.1/AC-8.1-06.trx` | `ab11cb325e8f38a6c5ddc83f59f9e10ccfcb92afe4c40d506ffc7be777ca51c7` |
| `AC-8.1-07` | `0` | `PASS` | `none` | `13` | none | none |

### `AC-8.1-01`

Command: `python3 _bmad/scripts/generate_ux_preservation_disposition.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --output-schema docs/release-evidence/ux-preservation-disposition-v1.schema.json --output-json docs/release-evidence/ux-preservation-disposition-v1.json --output-markdown docs/release-evidence/ux-preservation-disposition-v1.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-01#0001` | `schema::closed-valid` | `PASS` |
| `AC-8.1-01#0002` | `sources::path-version-hash` | `PASS` |
| `AC-8.1-01#0003` | `inventory::52-28` | `PASS` |
| `AC-8.1-01#0004` | `status::preserved` | `PASS` |
| `AC-8.1-01#0005` | `provenance::non-current` | `PASS` |
| `AC-8.1-01#0006` | `markdown::digest` | `PASS` |
| `AC-8.1-01#0007` | `predecessor::7.4` | `PASS` |

### `AC-8.1-02`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes -trx artifacts/v9/8.1/AC-8.1-02.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-02#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes` | `PASS` |

### `AC-8.1-03`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-03.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-03#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory` | `PASS` |

### `AC-8.1-04`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory -trx artifacts/v9/8.1/AC-8.1-04.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-04#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory` | `PASS` |

### `AC-8.1-05`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical -trx artifacts/v9/8.1/AC-8.1-05.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-05#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical` | `PASS` |

### `AC-8.1-06`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange -trx artifacts/v9/8.1/AC-8.1-06.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-06#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange` | `PASS` |

### `AC-8.1-07`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json --format bundle --output-json docs/release-evidence/story-8.1-final-record-v2.json --output-markdown docs/release-evidence/story-8.1-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.1-07#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-8.1-07#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-8.1-07#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-8.1-07#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-8.1-07#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-8.1-07#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-8.1-07#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-8.1-07#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-8.1-07#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-8.1-07#0010` | `generator::ux-disposition-schema-sources-and-output-digests` | `PASS` |
| `AC-8.1-07#0011` | `generator::story-7.4-predecessor-pair-verified` | `PASS` |
| `AC-8.1-07#0012` | `generator::five-exact-xunit-selectors-passed` | `PASS` |
| `AC-8.1-07#0013` | `generator::candidate-build-and-production-scope-bound` | `PASS` |

## Story 8.1 UX disposition

- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/8.1.json`
- Contract SHA-256: `51daf31859c8bf01c6bb7960b1ff3ba7c3e47eb121ed6da7966e59a285d3ed28`
- Story 7.4 record SHA-256: `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290`
- Build SourceRevisionId: `d59fe911a1238462719a5ba1c8d359dc64947ecf`
- Test assembly SHA-256: `d9f532b9c9df9c84df8aa3dceebaaa2aeea336b8449f25ad63513ff40b370b25`

| Bound input/output | Path | SHA-256 |
| --- | --- | --- |
| Source | `_bmad-output/planning-artifacts/ux-design-specification.md` | `948a5ac40a05fce510bffdd6818e3fcf3c871874b8779468954de57e452d8f18` |
| Source | `_bmad-output/planning-artifacts/ux-requirement-map.md` | `5965394e662a3b708896f5df85d2b981798bc590ea68feb3a974f66300c2751f` |
| `schema` | `docs/release-evidence/ux-preservation-disposition-v1.schema.json` | `0fa2494b76fe8b0f87e1bb4b16554e3e4fcbefe3d8c9437b2bf6063aad8905ab` |
| `json` | `docs/release-evidence/ux-preservation-disposition-v1.json` | `1d2515fd1c39b0777bbd1bb2cd9274578af707b8784bd21e0a17e235d5a72d38` |
| `markdown` | `docs/release-evidence/ux-preservation-disposition-v1.md` | `91e202f743486150b1191b6fc7a645afdd3084cc5cc716789f7ab03955449495` |

## Fault injection

No fault-injection result is bound to this record.

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-8.1-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-8.1-final-record-v2.md` |

## Rollback boundary

remove only the Story 8.1 generator, schema, results, three disposition outputs, and final record; preserve both UX sources, historical mappings, product/UI code, and the v1-v8 prefix.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `7` | `7` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
