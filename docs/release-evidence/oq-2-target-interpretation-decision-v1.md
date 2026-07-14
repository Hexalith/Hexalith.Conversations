# OQ-2 Target Interpretation Decision v1

**Machine-readable authority:** `docs/release-evidence/oq-2-target-interpretation-decision-v1.json`
**Status:** approved
**OQ-2 status:** resolved-confirmed
**Decision date:** 2026-07-14
**Approved by:** Jerome
**Approval reference:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-oq-2-target-interpretation.md`

The JSON artifact is authoritative. This document summarizes the approved target interpretation for human review.

## Decision

OQ-2 is resolved with the originally proposed numeric targets confirmed. Both comparisons are inclusive because the PRD uses `>=`.

### SM-1 - Conversations Plumbing Reduction

- Target: **at least 40%** of the accepted classified-plumbing baseline removed or externalized.
- Formula: `removedOrExternalizedPlumbingLoc / baselinePlumbingLoc * 100`.
- Frozen denominator: **13,289 LOC** from the accepted Story 1.4 inventory.
- Removed or externalized: **9,360 LOC**.
- Current reduction: **70.43%**.
- Result: **met**.

The accepted baseline is not re-estimated after implementation. Residual or retained plumbing remains in the denominator and current-module-owned value rather than being reclassified to improve the result.

### SM-2 - New-Module Authoring Cost

- Target: **at least 50% fewer hand-authored, module-owned files** within the frozen Story 4.1 minimal-module boundary.
- Formula: `(preInitiativeFileCount - templateMinimalFileCount) / preInitiativeFileCount * 100`.
- Template minimal: **29 files / 468 LOC**.
- Pre-initiative equivalent: **58 files / 1,460 LOC**.
- Current reduction: **50.00% files / 67.95% LOC**.
- Result: **met-on-accepted-estimate**.

File-count reduction is the decisive numeric dimension. LOC reduction remains mandatory supporting evidence, not a second pass/fail threshold, because the PRD pre-specified only a numeric file-count target. Exactly 50.00% meets the inclusive threshold.

The result remains estimate-qualified:

- the template-minimal value is an accepted manifest baseline, not a committed throwaway-module build;
- the pre-initiative equivalent is a low-confidence estimate because no accepted artifact reconstructs an exact buildable pre-template skeleton;
- `met-on-accepted-estimate` must not be presented as unconditional or high-confidence proof.

## Historical Evidence Boundary

The accepted SM-2 baseline, Story 5.3 success report, and signed release-owner decision remain byte-identical. Their OQ-2 `unconfirmed` wording records the state at measurement and signature time. This decision prospectively supersedes that unresolved target interpretation without rewriting history or invalidating the earlier signed decision.

| Historical artifact | Bound SHA-256 |
| --- | --- |
| `minimal-module-authoring-cost-sm2-baseline-v1.json` | `14de3c86628cb4f900008be42d086a7699ae0869b3b791f92c810ab921192e03` |
| `minimal-module-authoring-cost-sm2-baseline-v1.md` | `1cb37b3ddbc9186748c7556a87e5c08feb15d8dee30f4a1b0362bcd5536297fd` |
| `success-metric-report-and-attestation-v1.json` | `062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe` |
| `success-metric-report-and-attestation-v1.md` | `aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd` |
| `success-metric-report-and-attestation-v1-release-owner-decision.json` | `8091f6c26251420242a491cad100472dc1604a7163cc9d8df51bb1c742844856` |
| `success-metric-report-and-attestation-v1-release-owner-decision.md` | `a73077c0b5416c5085796c2e808a45efe09f5eb6a4ddf852214ecc93a9209e0b` |

## Non-Claims

This decision does not re-sign, replace, or expand the earlier release-owner attestation. It does not claim high-confidence or build-reconstructed SM-2 comparison evidence. It does not approve inherited platform controls, security certifications, penetration testing, key management, external audit, or platform compliance.
