---
title: 'Measure the published CLI/Platform tuple and disposition adoption blockers'
type: 'feature'
created: '2026-10-28'
status: 'done'
baseline_commit: 'c9c55d5a4033bfd7b22a4cf2298467b8e3549dac'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="separate prerequisite; user selected planning only">

## Intent

**Problem:** Completed source/tooling preparation supplies no accepted published CLI/Platform tuple. Root pins 4.38.1, Platform pins 4.35.2, adoption has sixteen failures, and runtime entry is still unqualified.

**Approach:** Produce one evidence-backed qualification handoff: bind the actual candidate artifact to release/source/catalog/Platform identities, measure its controls, and disposition every adoption failure. Investigate root's exact 4.38.1 as a candidate; historical 4.38.0 proof supplies procedure only.

**Decision:** The user selected option 1 on 2026-10-28: prepare this separate prerequisite plan. Implementation and runtime entry are not approved by that choice.

## Boundaries & Constraints

**Always:** Follow the [detailed plan](epic-16-published-artifact-platform-qualification-plan.md). Preserve prior work/evidence, exact inventories, native results and PASS/FAIL/BLOCKED/not-applicable distinctions. Restore only into isolated tool/cache/configuration paths. Local compilation stays Debug; consume producer CI's matched Release package evidence. Keep null entry, empty trusted entries, ACTIVE hold and all four authority flags false.

**Never:** Align pins/gitlinks, update dependencies, rewrite accepted bindings/fixtures, suppress scanner paths, repair upstream runtime, retire hosts, publish, deploy, send owner messages or execute Story 16.1–16.3. Operational/provider parity, recovery/operator/capacity, native-host and human SM-2 acceptance remain separate owner gates.

## I/O & Edge-Case Matrix

| Input | Required result |
| --- | --- |
| Exact artifact and complete source/CI identity | Measure only that tuple; retain distinct signed/unsigned identities |
| Missing package, provenance, native host or owner evidence | BLOCKED for that gate; no inferred approval |
| Proven identity/control mismatch | FAIL with retained diagnostics and nonempty ledger |
| Historical/generated/live adoption findings | Inventory every row, owner and next action; classification does not waive failure |
| Native skip or synthetic/source-only success | No published/platform/runtime qualification credit |

</frozen-after-approval>

## Code Map

[Plan](epic-16-published-artifact-platform-qualification-plan.md) binds inputs, commands and controls. Reuse Builds' artifact handoff, review ledger and package qualifier; preserve its historical probe. Use `contained_reader.py` and existing output guards for bounded collection. Platform documentation supplies tuple/registration requirements; tooling-v2 stays unchanged.

## Tasks & Acceptance

**Execution after approval, in dependency order:**

- [x] `module/validation/epic-16-published-artifact-qualification/<fresh-attempt>/` — capture protected entry state and a healthy mapped Debug AppHost baseline before collector code edits; preserve other sessions and clean up only the created instance.
- [x] `_bmad/scripts/qualify_epic16_published_artifact.py`, `tests/tooling/test_epic16_published_artifact_qualification.py` — add a bounded, read-only qualification collector and fault tests; explicit roots/version/source/inventory/fresh output; no version fallback or trust writer.
- [x] `module/validation/epic-16-published-artifact-qualification/<fresh-attempt>/` — capture implementation-entry state, preserve earlier observations, obtain exact producer-CI package/source evidence and isolated restoration; independently compare signed archive, NuSpec, loaded/package assemblies, schemas/catalog and Platform pair. Record unavailable gates explicitly.
- [x] Same attempt, `commands.json` and native/raw reports — run ordinary/adoption validation in both formats, all 36 malformed-key controls and package preparation/action checks; bind source/native review evidence to the actual artifact. Never replace that evidence with source tests.
- [x] Same attempt, `adoption-dispositions.json`, `qualification-ledger.json` — compare the exact sixteen-failure entry inventory plus all additions/removals; assign owner scope, evidence, proposed remedy and acceptance state to every row. Carry every retained release finding and unavailable owner gate.
- [x] `module/platform-handoff.md`, `spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md` — link measured results and remaining actions; keep runtime held. Upstream fixes and pin alignment require separate scope.

**Acceptance Criteria:**

- Given candidate 4.38.1, when collection completes, then each artifact/source/catalog/Platform claim is independently measured or explicitly BLOCKED, and identity disagreement is FAIL.
- Given the real manifest and temporary malformed copies, when both formats run, then positive controls pass and all 36 negatives yield exit 1 with one exact-field HXP002; real inputs stay unchanged.
- Given current and fresh adoption rows, when dispositions are emitted, then exact identity/delta comparison includes every failure and owner action without converting classifications into acceptance.
- Given absent owner/native acceptance, when writing the local handoff, then those gates stay BLOCKED and runtime outputs, entry, pins, dependencies and authority stay unchanged. Missing required artifact/control evidence blocks completion.

## Implementation Notes

2026-10-28 implementation verified: bounded collector and fault tests are implemented; actual 4.38.1 artifact/source/CI/schema/catalog and all 36 controls pass. Adoption and Platform catalog acceptance remain FAIL; package preparation/action and owner gates remain BLOCKED. Parent independently passed 31 tests, rehashed 7,540 retained files, verified all 81 bindings, unchanged dependencies/index, twelve held commands and seven absent outputs. Historical evidence-boundary replay and runtime publication are not-applicable. This prerequisite is entering code review, not qualifying runtime entry.

2026-10-28: The user approved this prerequisite and selected implementation in this session. The earlier dated planning decision remains historical; runtime entry stays held. Preserve the existing uncommitted planning work and obtain the required fresh normal AppHost baseline before code edits.

2026-10-28 review closeout: three independent layers produced eighteen findings, all addressed in thirteen internal patch groups; none deferred. Parent verification passes all 71 focused tests and the fresh final-code collection-05 retains artifact/CI/schema/catalog identity and all 36 passing controls. Publisher binding, native summaries and unavailable declared qualification files now have distinct states. Adoption and Platform catalog acceptance remain FAIL; preparation/action and owner gates remain BLOCKED. All twelve held commands retain zero runtime tests and seven absent outputs. See the [reviewed handoff](../../module/validation/epic-16-published-artifact-qualification/parent-review-20261028/README.md). No dotted v9 completion contract applies to this separate prerequisite; runtime publication and historical boundary replay are not-applicable.

## Spec Change Log

## Review Triage Log

- BH-01 — **medium / patch**. Premature HTTP EOF is reachable with a declared Content-Length larger than the received body; the worker currently labels its own shortened count complete. Require framing completeness and retain BLOCKED partial evidence.
- BH-02 — **high / patch**. Restore invokes the candidate CLI before independent artifact/CI payload verification, and collection continues after failed provenance/unsigned gates. Gate candidate execution on the existing verified prerequisites and move its version invocation accordingly; no new public surface is needed.
- BH-03 — **medium / patch**. Signature verification passes through `Runner.checked`, which maps a demonstrably invalid signature exit to BLOCKED. Preserve proven signature/integrity failures as FAIL while retaining genuine inspection/network unavailability as BLOCKED.
- BH-04 — **medium / patch**. A successful leader can leave an owned same-group child alive after redirecting its streams. Terminate remaining owned group members on ordinary completion, preserving the leader's native exit.
- BH-05 — **medium / patch**. The watched-tree loop performs unbounded per-entry work between deadline checks; byte caps do not bound an empty-file tree. Check the existing deadline during enumeration and bound traversal work.
- BH-06 — **medium / patch**. Restoration is inside the normal try body and is skipped by cancellation/internal exceptions. Run captured-input verification in finalization; a proven preservation failure must outrank cancellation.
- BH-07 — **medium / patch**. Human negative parsing counts only HXP lines, so HXP002 plus an HXC diagnostic is accepted. Count every diagnostic family and require exactly the contracted diagnostic.
- BH-08 — **medium / patch**. The resource inspector uses arbitrary JSON resource names as file paths before containment validation. Reject separators/traversal and enforce the owned embedded-directory boundary before creating output.
- BH-09 — **low / patch**. The owned temporary unsigned helper is never removed after success/failure/cancellation. Use managed cleanup while preserving the already-retained proof inputs.
- BH-10 — **medium / patch**. A publisher-hash-only log can pass producer-native with zero native summaries, and declared qualificationEvidence files remain unverified. Retain payload binding separately; require nonempty native evidence and explicitly BLOCKED missing declared evidence instead of claiming qualification.
- EC-01 — **medium / patch**. Independently reproduces BH-01's premature-EOF completeness defect; same framing guard and fault cover this finding.
- EC-02 — **medium / patch**. Independently reproduces BH-04's surviving same-group child; same owned-group finalization covers this finding.
- EC-03 — **medium / patch**. Independently reproduces BH-06: cancellation after a protected-input change skips verification and conceals FAIL behind BLOCKED. Finalization must execute the existing preservation check.
- EC-04 — **medium / patch**. Confirms BH-03's invalid-signature classification at the native checked wrapper; retain its specific proven-defect versus unavailability distinction.
- VG-01 — **medium / patch**. Pre-verified mutation forcing adoption acceptance PASS leaves all 31 tests green. Add caller-level adoption failure/disposition assertions.
- VG-02 — **medium / patch**. Pre-verified mutation forcing the Platform catalog caller PASS leaves all 31 tests green. Exercise the caller with both catalog disagreement and missing tag, retaining both states.
- VG-03 — **medium / patch**. Pre-verified mutation disabling loose/embedded/source schema equality leaves all 31 tests green. Exercise artifact qualification with actual substituted schema bytes and deny artifact validation.
- VG-04 — **medium / patch**. Independently reproduces BH-01's truncated response; its self-reported worker hash/count cannot establish HTTP message completeness.

Patch verification: HTTP framing/deadlines, successful-group cleanup, bounded traversal, interruption preservation, signature classification, diagnostic multiplicity, resource paths, temporary cleanup and caller-level adoption/Platform/schema faults all pass. Current owner/native failures remain qualification inputs, not waived review findings.

## Design Notes

Owner/native gates may remain BLOCKED; actual artifact identity and 36 controls are required. The normal AppHost baseline grants no runtime-entry credit; no offline exception is granted.

## Verification

Run `uv run --frozen --no-sync python3 -m pytest -q tests/tooling/test_epic16_published_artifact_qualification.py`, the plan's commands and `git diff --check`. Verify hashes, restoration, exact changed paths, held commands and seven absent runtime outputs. Collector tests must pass; unavailable external qualifications remain BLOCKED. Historical authority replay is not applicable.
