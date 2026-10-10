---
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-20-v22'
---

# Epic 10 Context: Unified Evidence Boundary

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give release-evidence maintainers and reviewers one non-shipping integrity boundary for evidence files, Git history, and governed development workflows. Every frozen evidence reader and current review or completion route must use the same checks, so declared hashes, incomplete diffs, unavailable history, or empty assertions cannot establish a passing result. Epic completion requires compatible records for all four stories.

## Stories

- Story 10.1: Provide neutral TestSupport helpers and a safe Git-facts runner
- Story 10.2: Implement manifest, hash, ledger, exact-diff, and gitlink invariants
- Story 10.3: Provide the evidence-boundary verifier and integrate every workflow surface
- Story 10.4: Migrate frozen readers, repair gate spans, publish the runbook, and prove fault injection

## Requirements & Constraints

- Keep the shared evidence helper non-packable, non-publishable, and independent of Conversations production assemblies. Its Git runner must bound execution, drain stdout and stderr concurrently, decode paths reliably, isolate hostile Git configuration, resolve raw modes and historical blobs, and classify missing history without a false pass.
- Recompute evidence-file hashes and canonical signable-payload digests from contained, existing repository-relative files. Reject generated or build outputs, path escapes, and any attempt to exempt signed release evidence. Keep roots of trust pinned in consuming test source.
- Compare the expected and Git-derived changed-path sets for exact equality. Detect submodule gitlinks from raw mode `160000`, never from text matches. An applicable check needs a nonempty evaluated assertion ledger; unavailable history and zero assertions cannot pass.
- Preserve all 27 frozen evidence readers with the same assertion identities, pinned constants, counts, and strength. Start with zero exemptions. Keep the projection-proof obligation separate, and do not reduce the approved FR-20 conformance denominator or alter public contracts to simplify validation.
- A machine-readable verifier must distinguish `PASS`, `FAIL`, `BLOCKED`, and `not-applicable`, emit stable unique blocker and warning codes, and record evaluated paths and assertions. Applicable failure or unavailable authority blocks review and completion; a valid `not-applicable` result remains explicit rather than being reported as a pass.
- Prove negative cases for hash, path, generated-output, gitlink, subset, signed-allowlist, Git-unavailable, workflow-removal, malformed-marker, reader-strength, and gate-span defects. Each injected fixture must trigger its expected blocker and restore its bytes.

## Technical Decisions

- The governing backlog is Epic V14 and the last complete architecture marker is V22. The original Epic 10 contract survives from V9; V10 supersedes only the workflow and reusable-guidance projection for Stories 10.3 and 10.4. Use the V10 twelve-path active workflow inventory and separate four-path guidance inventory; the V9 workflow-path list is superseded.
- Shared TestSupport owns neutral repository, Git, manifest, boundary, and assertion-ledger primitives. The verifier owns cross-reader and workflow enforcement; its result follows the closed evidence-boundary schema. Frozen inventories and candidate-bound final records provide identity, digest, predecessor, and rollback evidence for each story.
- Six logical workflow bodies are mirrored across `.agents` and `.claude`; insertion spans and deterministic render parity are checked. Deprecated development aliases forward once to current governed routes. Project-owned customization resolves the same guidance for regular, one-shot, and unattended build and for review; installed defaults remain read-only.
- Put the evidence gate after the promotion gate and before every governed review or done transition. A removed or displaced call, alias fork, guidance drift, empty applicable scope, or parity mismatch must fail closed. The inherited promotion-gate span guard remains independently effective.
- Follow `docs/runbooks/current-change-validation.md` for routine current-tree validation. Its historical authority and evidence-boundary machinery is not a general prerequisite for later work; the explicit Epic 10 story contracts still require their named results and final records.

## Cross-Story Dependencies

Stories 7.4 and 9.2 are the hard entry to Story 10.1. The Epic 10 chain then runs strictly 10.1 → 10.2 → 10.3 → 10.4, with each story consuming its predecessor's compatible final record. Epic 11 and later preservation and release evidence depend on Epic 10's completed boundary.
