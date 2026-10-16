# Epic 10 Context: Unified Evidence Boundary

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give release-evidence maintainers and reviewers one non-shipping integrity boundary for evidence files, Git history, and governed development workflows. Frozen evidence readers and applicable review or completion routes must share checks that reject declared hashes, incomplete diffs, unavailable history, and empty assertions as passing proof. Epic completion requires all four stories done at compatible candidates.

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
- A machine-readable verifier must distinguish `PASS`, `FAIL`, `BLOCKED`, and `not-applicable`, emit stable unique blocker and warning codes, and record evaluated paths and assertions. Its explicit contract returns exit `0` for `PASS` or valid `not-applicable`, `1` for applicable failure, and `2` for unavailable authority/history. Applicable failure or unavailable authority blocks the scoped review and completion transition; `not-applicable` remains explicit.
- Prove negative cases for hash, path, generated-output, gitlink, subset, signed-allowlist, Git-unavailable, workflow-removal, malformed-marker, reader-strength, and gate-span defects, including the frozen chain-table mutations. Each injected fixture must trigger its exact blocker, evaluate a nonempty ledger, and restore its bytes.
- Publish guidance covering invariants, authoring, exemptions, fault injection, and known limitations with exact commands and codes. Verify resolved project-owned guidance for regular, one-shot, and unattended build and review; do not change installed customization defaults or claim unimplemented CI wiring.

## Technical Decisions

- The authoritative backlog is the V14 carrier at `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`; the root `epics.md` is a non-authoritative draft. Epic 10 retains its original reader, integrity, predecessor, and rollback obligations. Historical workflow inventories remain provenance; current explicitly scoped evidence work follows its approved effective contract.
- Shared TestSupport owns neutral repository, Git, manifest, boundary, and assertion-ledger primitives. The verifier owns cross-reader and workflow enforcement; its result follows the closed evidence-boundary schema. Frozen inventories and candidate-bound final records provide identity, digest, predecessor, and rollback evidence for each story.
- Story 10.3 is complete under `story-10.3-current-binding-v1.json` and `story-10.3-current-contract-v1.json`. Its approved current scope covers ten installed paths: five logical bodies mirrored across `.agents` and `.claude`, plus deterministic render parity. Four guidance inputs bind the three project-owned customizations and `docs/runbooks/current-change-validation.md`. Retired development aliases stay absent; restoring them fails the scoped check. Its original authority and predecessor records remain preserved provenance.
- Follow `docs/runbooks/current-change-validation.md` for current-tree work. Explicit evidence specs invoke their effective scoped verifier before review or completion and preserve all four result states. Missing or displaced gates, guidance drift, empty applicable scope, and parity mismatch fail closed. The final-record completion guard remains independently required.
- Story 10.4 retains its original migration and guidance obligations, including the separate promotion-span regression proof. Its historical twelve-route and authority-publisher commands require reconciliation with current policy before they can serve as current acceptance evidence. Story 10.3's additive binding does not silently amend Story 10.4 or authorize resurrecting retired gates. Preserve historical authority, accepted evidence, and prior failure results; ordinary work does not require historical publication or hold machinery.

## Cross-Story Dependencies

Stories 7.4 and 9.2 are the hard entry to Story 10.1. The Epic 10 chain runs strictly 10.1 → 10.2 → 10.3 → 10.4, with each story consuming its predecessor's compatible final record. Epic 11 and later preservation and release evidence depend on the completed boundary. The original Epic 5 action A5 requires compatible passing Story 10.4 evidence; a runbook or validator result alone cannot close it or authorize release.
