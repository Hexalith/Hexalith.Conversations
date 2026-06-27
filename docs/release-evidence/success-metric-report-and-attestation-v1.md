# Success Metric Report and Signable Attestation v1

**Artifact:** `success-metric-report-and-attestation-v1.json`
**Status:** ready-for-signature
**Generated:** 2026-06-26T16:01:21Z
**Story:** 5.3

The JSON artifact is authoritative. This Markdown summarizes the same facts for human review and signature preparation.

## Source Bundle

- Source artifact count: 32
- Signable payload hash: `331e92a1fbc8492b08586d7b785e38c536c640a24a20f16ec2ea8f24cf22710f`
- Baseline commit: `2ab26d8ab3b61186c82ebe5d4776f6c223817126`
- Containing commit: read from the Git commit that contains this artifact; the self-referential final commit SHA is intentionally not embedded.

## SM-1 Boilerplate / Plumbing Reduction

| Field | Value |
| --- | ---: |
| Source total LOC | 35,769 |
| Baseline plumbing LOC | 13,289 |
| Consume LOC | 7,037 |
| Promote LOC | 6,252 |
| Keep LOC | 22,480 |
| Current module-owned plumbing LOC | 3,929 |
| Removed or externalized plumbing LOC | 9,360 |
| Directional reduction | 70.43% |

Target interpretation remains `unknown-accepted` because OQ-2 is not confirmed.

## SM-1 Row Dispositions

| Area | Baseline LOC | Current LOC | Reduced LOC | Disposition | Evidence |
| --- | ---: | ---: | ---: | --- | --- |
| `query-cursor-orchestration` | 2,076 | 0 | 2,076 | consumed | Story 2.3 |
| `shared-host-api` | 906 | 0 | 906 | consumed | Story 2.1 |
| `server-command-dispatch-idempotency` | 1,893 | 1,893 | 0 | residual | Story 2.2 |
| `core-aggregate-dispatch-replay-idempotency` | 1,896 | 1,896 | 0 | residual | Story 2.2 |
| `generic-serialization-converters` | 215 | 47 | 168 | reduced-to-thin-facade | Story 2.6 |
| `duplicate-test-fakes` | 51 | 51 | 0 | retained | Story 2.7 |
| `projection-orchestration` | 1,800 | 0 | 1,800 | consumed | Story 2.5 |
| `diagnostics-telemetry-scaffolding` | 2,442 | 0 | 2,442 | promoted-adopted | Story 3.3 |
| `tenant-access-projection` | 1,086 | 0 | 1,086 | promoted-adopted | Story 3.2 |
| `publication-transport-marshaling` | 422 | 0 | 422 | promoted-adopted | Story 3.5 |
| `typed-client-registration` | 479 | 42 | 437 | reduced-to-thin-facade | Story 3.1 |
| `service-defaults-greenfield` | 13 | 0 | 13 | promoted-adopted | Story 3.4 |
| `apphost-greenfield` | 10 | 0 | 10 | promoted-adopted | Story 3.5 |

## SM-2 New Module Authoring Cost

| Field | Value |
| --- | ---: |
| Template minimal files | 29 |
| Template minimal LOC | 468 |
| Pre-initiative equivalent files | 58 |
| Pre-initiative equivalent LOC | 1460 |
| Directional file reduction | 50.00% |
| Directional LOC reduction | 67.95% |

OQ-2 status is `unconfirmed`. The pre-initiative equivalent is estimated and low confidence, so this report does not claim the SM-2 target is met.

## Behavior Preservation

- Story 5.1 final conformance: 365/365 passed, 0 errors, 0 failed, 0 skipped, 0 not run.
- Release-gate suite classes: 14 unique classes.
- Public contract type count: 196 baseline / 196 final.
- Contract diff status: empty; public-contract-shape baseline git diff: empty.
- Story 5.2 conformance after ledger validation: 374/374 passed.
- Removed-test ledger status: pass-with-residual-coupling; actual dead-plumbing removals: 1.
- No release-gate suite class is recorded as missing.
- Residual Conformance.Tests -> Server coupling is retained and disclosed.

## Residual Risks

| Risk | Status | Decision |
| --- | --- | --- |
| `oq-2-target-confirmation` | unconfirmed | Carry as unresolved; SM-1 and SM-2 values are accepted measurement evidence but not a target-pass approval. |
| `projection-read-store-population` | deferred-for-explicit-architecture-decision | Not proven by this evidence story; final sign-off must either accept the deferral or require a separate production projection-write proof. |
| `conformance-tests-server-coupling` | residual-coupling-retained | Retain and disclose; Story 5.2 showed safe removal would weaken or break live release-gate assertions. |
| `inherited-platform-controls` | outside-module-scope | Do not claim Conversations module approval for SOC2, ISO, vulnerability disclosure, key management, penetration testing, or platform CISO approval. |
| `environment-limitations` | mitigated-for-local-validation | Preferred dotnet test runner may be socket-blocked in this sandbox; use Release build plus compiled xUnit executable fallback and record both attempts. |

## Attestation

- Signature status: `ready-for-signature`
- Decision: `pending`
- Signer: pending
- Approval reference: pending

No human approval, security approval, external audit approval, or platform compliance approval is claimed by this implementation artifact.

## Validation Status

- Focused validation test added and red phase confirmed before artifact generation.
- Release build passed with 0 warnings and 0 errors.
- Preferred `dotnet test` runner aborted before test execution due socket permission in this sandbox.
- Focused Story 5.3 validation passed: 10 total, 10 passed.
- Full conformance fallback passed: 384 total, 384 passed, 0 errors, 0 failed, 0 skipped, 0 not run.
- Public-contract-shape baseline diff: empty.
