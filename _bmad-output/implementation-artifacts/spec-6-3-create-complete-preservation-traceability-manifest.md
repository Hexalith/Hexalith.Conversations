---
title: 'Create the complete preservation traceability manifest'
type: 'feature'
created: '2026-08-01T08:31:49+02:00'
status: 'in-progress'
review_loop_iteration: 0
followup_review_recommended: false
baseline_commit: 'e480c3f3176cdc3d911baf91eb3e7a8cd38874aa'
baseline_revision: 'e480c3f3176cdc3d911baf91eb3e7a8cd38874aa'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/docs/runbooks/story-final-record-generation.md'
warnings: ['oversized']
submodule_promotions: []
---

<intent-contract>

## Intent

**Problem:** Release evidence has no authoritative, zero-gap mapping for the preserved initiative, feature, UX, public-contract, conformance, and current-control denominators. Existing v1 evidence is partial and immutable, so release owners cannot distinguish proven preservation from approved non-activation.

**Approach:** Add a standalone v2 JSON manifest, schema, generated Markdown projection, and deterministic generator/validator. Derive obligation identities from hash-bound authority, attach evidence or a governed non-activation disposition to every row, and make tampering, gaps, stale evidence, ownership drift, or unsupported tier changes fail with stable diagnostics.

## Boundaries & Constraints

**Always:** Preserve every existing v1 artifact byte-for-byte. Enumerate exactly `FR-1..FR-20`, `Feature-FR1..Feature-FR104`, `Feature-NFR1..Feature-NFR77`, `UX-DR1..UX-DR52`, and every acceptance row under the four normative UX acceptance sections; assign unlabeled UX rows deterministic section/ordinal/text-hash IDs. Cover public Contracts and Client surfaces, routes/wire behavior, current architecture controls, and every conformance assertion. Each obligation must have repository-relative source provenance, full SHA-256 bindings, module/platform control ownership, and exactly one closure: hash-valid evidence or named-owner-approved non-activation with rationale; delivered-to-inactive and compatible-change rows also need approval and compatibility/replacement evidence. Story 6.9's final triage must place every assertion in exactly one release-gated tier, and Story 6.8's final-record path must mechanically produce completion facts. If only a human approval remains after all agent-verifiable work is committed, leave the strict gate reporting `APPROVAL_PENDING` and finalize this spec as `awaiting-operator` with imperative `operator_actions`; never use `blocked` for that condition.

**Block If:** An authoritative denominator cannot be derived unambiguously from the named source sections; a required Story 6.8/6.9 artifact claims completion but is missing or hash-invalid; completing the work would require changing public contracts, production source, signed v1 evidence, or a root submodule; or repository state contains unrelated changes that cannot be preserved and excluded from the candidate.

**Never:** Extend `ConformanceManifestV1`, rewrite `manifest.schema.json`, treat source prose as behavioral proof, infer approval from preservation language, accept `0 missing from 0 expected`, trust counts or hashes declared only by the artifact under test, weaken/remove/reclassify a test without governed evidence, hand-author final-record counts/file lists/gitlinks, or traverse nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Valid candidate | Complete authority, evidence, approvals, and final 6.9 tier data | Canonical JSON and byte-matching Markdown projection; strict validation passes with zero unresolved rows | Exit 0 and stable summary |
| Human approval pending | Structurally complete draft dispositions without operator approval | Agent-verifiable generation/tests pass; strict release gate refuses completion | `APPROVAL_PENDING`; hand off as `awaiting-operator` |
| Denominator drift | Missing, duplicate, unknown, empty, or source-text-changed obligation | No zero-gap claim is emitted | Stable category-specific error and nonzero exit |
| Evidence or governance drift | Escaping/missing path, bad hash, stale result, wrong owner/tier, or unapproved disposition/change | Affected row and aggregate validation fail | Stable diagnostic names the row and violated rule |

</intent-contract>

## Code Map

- `_bmad/scripts/generate_preservation_traceability_manifest.py` -- deterministic extraction, generation, Markdown projection, and strict validation entry point.
- `_bmad/scripts/tests/test_generate_preservation_traceability_manifest.py` -- fault-injection coverage for extraction, path/hash trust, approvals, tiers, ownership, and Markdown parity.
- `docs/release-evidence/preservation-traceability-manifest-v2.schema.json` -- closed v2 machine contract; separate from the frozen v1 schema.
- `docs/release-evidence/preservation-traceability-manifest-v2.json` -- authoritative generated preservation record.
- `docs/release-evidence/preservation-traceability-manifest-v2.md` -- generated reviewer projection of the JSON.
- `docs/release-evidence/preservation-non-activation-disposition-v2.json` -- exact-ID disposition/approval input for obligations without behavioral evidence.
- `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` -- repository-surface zero-gap and anti-tamper gate.
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` -- read-only Story 6.9 decision/triage input; do not manufacture missing triage.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- synchronize Story 6.3 state without copying generated measurements.

## Tasks & Acceptance

**Execution:**
- `docs/release-evidence/preservation-traceability-manifest-v2.schema.json` -- define closed vocabularies, row requirements, binding metadata, prior-version/mutation rules, and nonempty aggregate summaries.
- `docs/release-evidence/preservation-non-activation-disposition-v2.json` -- generate the exact unresolved-ID decision draft with distinct evidence owner, control owner, approver, rationale, scope, date/status, and compatibility fields; never mark it approved without operator evidence.
- `_bmad/scripts/generate_preservation_traceability_manifest.py` -- derive canonical requirement/UX/control/contract/assertion inventories from pinned sources, validate governed closure and repository-contained SHA-256 evidence, render deterministic JSON/Markdown, and expose structural versus strict validation modes with stable error codes.
- `_bmad/scripts/tests/test_generate_preservation_traceability_manifest.py` -- cover exact denominators and fault-inject deletion, duplication, source/hash mutation, path escape, empty sets, stale evidence, ownership reversal, missing approval/compatibility evidence, tier omission, and projection drift.
- `docs/release-evidence/preservation-traceability-manifest-v2.json` and `docs/release-evidence/preservation-traceability-manifest-v2.md` -- generate one authoritative record and navigable parity projection, preserving v1 and explicitly stating v2's supersession boundary.
- `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs` -- independently recompute authority sets and hashes, validate the manifest/schema/projection, bind final Story 6.9 tier evidence, protect immutable v1 bytes, and prove fault-injected invalid states are rejected.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move 6.3 through implementation/review or `awaiting-operator`; do not mark done until strict validation, Story 6.8 record generation, and Story 6.9 tier evidence all pass.

**Acceptance Criteria:**
- Given the hash-bound PRD and UX sources, when expected obligations are independently enumerated, then all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and every row in all four normative UX acceptance sections appear exactly once with stable IDs and source-text hashes.
- Given any obligation row, when closure is validated, then it references repository-contained hash-valid evidence or an approved named-owner non-activation with rationale, and delivered-to-inactive or compatible-change dispositions additionally reference compatibility/replacement evidence.
- Given architecture v6 and its cumulative Epic 6 authority, when current controls and public surfaces are reconciled, then ownership, canonical host shape, test-only AppHost, projection proof, SM-C2 v6 rule, promotion/final-record gates, immutable v1 evidence, Contracts, Client, routes, wire/events/errors, and package/version behavior have no gap or ownership reversal.
- Given architecture v7 and the approved projection-proof lifecycle, when projection-population closure is reconciled, then the manifest binds the complete immutable predecessor chain, records Story 6.2 v2 as candidate-bound historical evidence, identifies exactly one approved current successor head, and fails if historical evidence is represented as proof for a later candidate. Story 6.3 cannot return to review before Story 6.12 passes.
- Given completed Story 6.9 evidence, when conformance assertions are enumerated, then each assertion has exactly one approved tier, both tiers remain release-gated, the decision and triage bytes are SHA-256-bound, and portable-tier freedom is supported by executed structural evidence rather than manifest prose.
- Given any source, baseline, evidence, build, test, contract, or mutation binding, when current bytes and Git/build identities are recomputed independently, then full hashes and identities match; missing, stale, escaping, generated-output, submodule-internal, self-attested, or vacuous evidence fails closed.
- Given a valid manifest candidate, when a row/source/hash/owner/tier/approval is deleted, duplicated, altered, or made empty, then automated fault injection produces a stable nonzero diagnostic and byte-identical restoration returns green.
- Given a later manifest change, when mutation governance is checked, then it creates a new immutable version linked by predecessor hash with exact changed IDs, rationale, approver, and replacement/compatibility evidence, while all frozen v1 bytes remain unchanged.
- Given only human approval is outstanding after all agent-verifiable files and tests are committed, when the run is finalized, then the spec and sprint state are `awaiting-operator`, `operator_actions` is a nonempty YAML list of imperative instructions, and no blocked status is used for that condition.
- Given all strict gates and prerequisite evidence pass, when Story 6.8 generates the candidate-bound completion bundle, then the story record embeds that bundle verbatim, verifies its digest, and contains no parallel hand-maintained counts, paths, commits, tests, or gitlink facts.

## Spec Change Log

## Review Triage Log

## Design Notes

The JSON is authoritative; Markdown is a deterministic projection checked for parity. The generator may accept a structurally complete pending-approval disposition so agent work remains testable and committable, but strict validation must stay red with `APPROVAL_PENDING` until the named operator approves the exact hashed decision bytes. UX acceptance IDs are derived from normalized section name, one-based row ordinal, and a source-text hash so unlabeled criteria cannot disappear silently while Story 6.4 later governs any permanent-ID migration.

## Verification

**Commands:**
- `uv run --with pytest pytest _bmad/scripts/tests/test_generate_preservation_traceability_manifest.py -q` -- all generator and fault-injection cases pass.
- `python3 _bmad/scripts/generate_preservation_traceability_manifest.py --check --allow-pending-operator` -- generated JSON/Markdown and all agent-verifiable bindings are current.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release --no-restore /nr:false /m:1 -p:UseHexalithProjectReferences=true` -- conformance assembly builds warning-free.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest -noLogo` -- focused repository-surface checks pass.
- `python3 _bmad/scripts/generate_preservation_traceability_manifest.py --check` -- strict completion gate passes, or reports only `APPROVAL_PENDING` before operator handoff.
- `git diff --check` -- no whitespace errors.
