---
title: 'Publish v11 Story 7.1 schema-checkpoint authority'
type: 'feature'
created: '2026-08-04'
status: 'done'
baseline_commit: '2a2387727323d96b6bc493e28ca6570488e64263'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 7.1 has legitimate schema-first implementation order, but v10 exposes only its complete six-scenario outcome; the publisher also regresses committed Epic 6 retrospective evidence. No bounded checkpoint may execute while the global hold is active.

**Approach:** Append v11 planning and architecture authority for non-story checkpoint `7.1-SCHEMAS`, publish a closed sidecar and matching projections from one committed planning candidate, and preserve Story 7.1's v10 completion contract plus fail-closed hold semantics.

## Boundaries & Constraints

**Always:** Preserve v1-v10 authority bytes, the v9 story-contract schema, all 27 v10 story-contract shapes, Story 7.1's six ACs/two final-record paths, Story 7.2's predecessor, `implementationHold: ACTIVE`, and Epic 6's `done` retrospective with six ordered open actions. Publish atomically with exact paths/digests, graph acyclicity, non-vacuity, and one-way sidecar-to-bundle digest order.

**Ask First:** Any departure from the approved sidecar shape, checkpoint graph, committed-candidate sequence, or exact retrospective inventory; any need to rewrite historical authority/evidence or the user-modified Story 7.1 spec.

**Never:** Implement Story 7.1 schemas, generator/tests/results/final records; run or bias IR-0; change a hold decision; start/complete Story 7.1; unlock Story 7.2; alter product/runtime/UX/dependencies/submodules/gitlinks, historical evidence, or push changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid v11 publication | Committed PC contains approved sources and retrospective inventory | All companions reproduce; check reports `V9_PLANNING_AUTHORITY_OK` | Any drift fails atomically |
| Invalid slice authority | Closed field or exact checkpoint edge is mutated | Schema/parity validation rejects publication | Stable failure; fixture restores |
| Retrospective regression | Status or one of six ordered open rows is missing, duplicated, reordered, or changed | Publication is rejected rather than deleting evidence | Stable projection blocker |
| Gate evidence absent | IR-0 or owner lift is missing/stale/mismatched | Publication remains `ACTIVE`; checkpoint stays non-executable | Never infer an exception |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:3997` and `_bmad-output/planning-artifacts/architecture.md:2127` -- immutable v10 blocks; append mutually bound v11 amendments.
- `_bmad/scripts/publish_v9_planning_authority.py:24` -- split v10 base-story/current v11 authority; extend markers, sidecar, graph/view/sprint/bundle, schemas, managed scope, and atomic publication. At `:949`, preserve retrospective evidence and emit ISO `2026-08-04`.
- `_bmad/schemas/v11-story-slice-authority-v1.schema.json` -- new recursively closed Draft 2020-12 planning schema.
- `_bmad/schemas/v9-{execution-graph,authority-bundle,inventory,supersession-map}-v1.schema.json` -- v11 current-authority constants; graph permits `checkpoint`.
- `_bmad/scripts/tests/test_publish_v9_planning_authority.py:36` and `tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs:18` -- deterministic mutation, checkpoint, sprint, bundle, and hold coverage.
- `_bmad/schemas/v9-story-contract-v1.schema.json` -- read-only SHA-256 `33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0`.
- `_bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core.md` -- user-modified blocked evidence; preserve byte-identically.

## Tasks & Acceptance

**Execution:**
- [x] Canonical Markdown -- append mutually bound v11 amendments while pinning prior blocks.
- [x] Schemas and publisher -- generate the sidecar, checkpoint projections, and self-excluding ACTIVE bundle from one committed PC.
- [x] Python/C# tests -- prove closed shapes, parity, base-story and retrospective preservation, restoration, and non-vacuity.
- [x] Managed companions -- regenerate the complete candidate-bound set atomically; do not hand-edit generated projections.

**Acceptance Criteria:**
- Given a committed v11 PC, when focused tests and `--check` run, then outputs reproduce and identify one PC/bundle digest.
- Given the graph and sidecar, when parity is checked, then one `7.1-SCHEMAS` follows `6.2`/`IR-0`, Story 7.1 follows it, and Story 7.2 still follows Story 7.1.
- Given publication-time evidence, when authority is inspected, then the bundle remains `ACTIVE`, no IR-0/hold verdict is embedded, and neither sprint lifecycle nor final-record output exists for the checkpoint.
- Given preserved inputs, when scope is audited, then the frozen schema and dirty Story 7.1 spec are byte-identical and no forbidden path changed.

## Spec Change Log

- `2026-08-04` -- Implemented and published the complete v11 source/companion set from committed planning candidate `9c7d8e62753e3c126c32b9f1d038331d83310868`; bundle digest `251eed538283a939e674ebbf44c7393fb39baeabd345dd285b46ce3c09804477`.
- `2026-08-04` -- Hardened the v11 marker, graph, sidecar, sprint, view, managed-namespace, role, and bundle-inventory checks; republished from successor planning candidate `1e72e63cbf2b556b8dc6fe732428c66f51985ac7`; bundle digest `2c5e45b07b3d58dca1f1baca18dea6e98ba1ad37db682517a6f09ed15caaa3c4`.

## Design Notes

Keep two explicit authority layers: v10 base-story contracts rebound to the new PC, and v11 current publication companions. Digest direction is canonical amendment plus regenerated 7.1 base contract -> sidecar -> bundle; the sidecar records only the bundle path, preventing self-reference.

The publisher pins both v9 and v10 canonical marker bytes, keeps the frozen story-contract schema at SHA-256 `33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0`, and validates the completed Epic 6 retrospective plus its six ordered open action rows as one exact byte inventory before and after sprint rendering.

## Verification

**Commands:**
- `python3 -m pytest -q _bmad/scripts/tests/test_publish_v9_planning_authority.py` -- expected: non-vacuous PASS including mutation restoration.
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- expected: exit 0 with `V9_PLANNING_AUTHORITY_OK PC=<sha> BUNDLE=<sha256>`.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release` -- expected: clean build.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest` -- expected: all focused tests pass.
- `git diff --check` -- expected: no whitespace errors; changed-path audit contains no forbidden scope.

**Observed results:**

- `uv run --no-cache --with pytest --with jsonschema python3 -m pytest -q _bmad/scripts/tests/test_publish_v9_planning_authority.py` -- `PASS`, 21 passed. The literal system-Python command is environment-blocked because `/usr/bin/python3` has no `pytest` module.
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` -- `PASS`; planning candidate `1e72e63cbf2b556b8dc6fe732428c66f51985ac7` and bundle digest `2c5e45b07b3d58dca1f1baca18dea6e98ba1ad37db682517a6f09ed15caaa3c4` reproduced exactly.
- Serialized Release build with environment pins -- `PASS`, 0 warnings and 0 errors. The literal broad build again stalled at `Determining projects to restore...` and was cancelled after two minutes; the focused compiled-test fallback remained available.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest` -- `PASS`, 6 passed, 0 failed/skipped/not-run.
- `git diff --check`, exact 53-path scope audit, and protected-byte audit -- `PASS`; the frozen story-contract schema and user-modified Story 7.1 specification retain their entry SHA-256 digests.

## Suggested Review Order

**Authority model**

- Start with the approved checkpoint, boundaries, digest order, and unchanged hold.
  [`sprint-change-proposal-2026-08-04.md:195`](../planning-artifacts/sprint-change-proposal-2026-08-04.md#L195)

- The append-only epic amendment defines checkpoint scope and completion effects.
  [`epics.md:4162`](../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#L4162)

- The architecture overlay owns sidecar, graph parity, and fail-closed interpretation.
  [`architecture.md:2196`](../planning-artifacts/architecture.md#L2196)

- The closed schema makes every slice-authority field machine-verifiable.
  [`v11-story-slice-authority-v1.schema.json:24`](../../_bmad/schemas/v11-story-slice-authority-v1.schema.json#L24)

- The generated sidecar binds v10 completion authority beneath v11 execution authority.
  [`v11-story-7.1-schema-slice-v1.json:2`](../planning-artifacts/v11-story-7.1-schema-slice-v1.json#L2)

**Deterministic publication**

- Separate base-story identities prevent v11 from rewriting v10 contracts.
  [`publish_v9_planning_authority.py:24`](../../_bmad/scripts/publish_v9_planning_authority.py#L24)

- Exact v11 marker pins reject uncoordinated canonical-source drift.
  [`publish_v9_planning_authority.py:374`](../../_bmad/scripts/publish_v9_planning_authority.py#L374)

- Sidecar rendering and full semantic comparison enforce the approved closed document.
  [`publish_v9_planning_authority.py:674`](../../_bmad/scripts/publish_v9_planning_authority.py#L674)

- Exact checkpoint graph parity rejects arbitrary nodes and predecessors.
  [`publish_v9_planning_authority.py:768`](../../_bmad/scripts/publish_v9_planning_authority.py#L768)

- Sprint rendering preserves retrospective evidence before normalizing publication metadata.
  [`publish_v9_planning_authority.py:1227`](../../_bmad/scripts/publish_v9_planning_authority.py#L1227)

- Bundle construction pins exact paths and complementary base/slice roles.
  [`publish_v9_planning_authority.py:1363`](../../_bmad/scripts/publish_v9_planning_authority.py#L1363)

**Published projections**

- The graph exposes one checkpoint while retaining complete-story successor gates.
  [`v9-execution-graph-v1.json:174`](../planning-artifacts/v9-execution-graph-v1.json#L174)

- Sprint projection keeps the hold active and retrospective complete.
  [`sprint-status.yaml:41`](sprint-status.yaml#L41)

- The bundle remains publication-time ACTIVE and self-excluding.
  [`v9-authority-bundle-v1.json:9`](../planning-artifacts/v9-authority-bundle-v1.json#L9)

**Verification and follow-up**

- Python mutations cover sidecar, graph, retrospective, view, and bundle boundaries.
  [`test_publish_v9_planning_authority.py:188`](../../_bmad/scripts/tests/test_publish_v9_planning_authority.py#L188)

- Root-pinned conformance tests independently verify all published identities.
  [`PlanningAuthorityV9ValidationTest.cs:41`](../../tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs#L41)

- Crash-atomic publication redesign remains explicitly deferred beyond this correction.
  [`deferred-work.md:228`](deferred-work.md#L228)
