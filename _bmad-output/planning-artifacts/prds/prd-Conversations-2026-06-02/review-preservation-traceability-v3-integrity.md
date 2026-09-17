# Preservation Traceability v3 Integrity Review

## Gate result

**PASS for presentation as a pending Owner-review candidate; RELEASE/ACCEPTANCE remains FAIL-CLOSED.** The final `3.0.0-rc.1` bytes preserve the complete additive denominator, distinguish preservation from release activation, give every obligation a validated closure or an explicit pending governed disposition, enforce the critical invariants in the schema, and do not invent approval or authority. Owner approval must remain pending because broad Conformance is 456/473 and the other recorded gates have not genuinely passed.

Reviewed canonical JSON SHA-256: `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`.

## Findings ordered by severity

### HIGH gate condition — Full conformance remains red

This is correctly recorded by the manifest and is not an integrity defect:

- clean-HEAD run: 472 total, 456 passed, 16 failed;
- final Owner-review overlay run: 473 total, 456 passed, 17 failed;
- final Owner-review XML SHA-256: `e918617c10d1b2722e7b72fc7e441429b068ef56699f1d8d6ef33a436c051549`;
- the restored historical denominator test is the additional candidate failure.

Consequently FR-20 and SM-C1 remain `PENDING`, SM-C2 remains `FAILED` under the universal every-path `<=5%` rule, OQ-1 remains `BLOCKED`, and the implementation hold remains `ACTIVE`. Digest confirmation cannot be interpreted as release approval, test waiver, or gate closure.

### No remaining critical/high/medium manifest-integrity defect

The initial review found unsafe activation inference, asserted rather than computed orphan status, a contradictory OQ-1 reason, and an under-constrained schema. The final candidate remediates them:

- all obligations use `preservationState: required` and a separate `releaseActivationState`;
- 19 initiative FRs are marked active initiative scope, FR-16 is `deferred-not-active`, all 181 Feature requirements and 104 UX obligations are `pending-not-inferred`, and no legacy requirement is release-activated;
- all 969 obligations have computed closures; 277 unresolved items bind one-to-one to explicit pending governed dispositions whose approval evidence is null and whose activation effect is `none`;
- OQ-1 names the actual remaining blockers while retaining the recorded Owner authority;
- the tightened schema rejects denominator removal, false gate closure, prohibition removal, orphan-proof deletion, obligation shrinkage, duplicate-category substitution, missing build bindings, an orphaned row, a missing disposition, legacy activation, non-requirement activation, and FR-16 activation;
- toolchain identity is now captured in a separate hash-bound artifact and required by both build bindings, the artifact inventory, the command inventory, and the schema;
- Conformance was rerun after the final PRD/addendum edits, so the final overlay bindings and result XML refer to the same reviewed source bytes.

## Independent evidence checks

### Source and Git binding

- `HEAD`, tree, and parent are exactly `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49`, `85efb51da9e790550f0991c5c59312ae86dcc838`, and `c0abd5cb73d420bb2f4b5d04827461ad82c4528c`.
- `HEAD == origin/main` at review time.
- The parent-to-HEAD remediation set contains exactly 50 unique paths. Every non-gitlink byte hash and byte count in `sourceBinding.committedRemediation.changedPaths` recomputed successfully.
- All ten recorded gitlinks equal both the root commit's tree entries and the checked-out clean submodule HEADs.
- The successor manifest, generator, schema, review evidence, restored historical test, PRD, addendum, and memlog remain uncommitted. No commit was created.

### Additive test denominator

- The v1 floor independently reconstructs to 14 suites and 214 unique fully qualified Fact IDs. Its exact newline-delimited ID digest is `31a3a21b58aa3d704458a242adf87a41b95cc69b5f9b2b38994354735d3fb4bc`.
- The decision-bound source at `c6670fac7347ecd7240f7bab7e5e23147c8dfc65` contains 368 Facts and two Theories expanding to 16 cases. The approved cumulative set therefore contains 384 unique IDs with digest `ec0d374dc6d947c662165de79eae9911acaa60bfaf4735e314923bbd9f0e6c61`.
- The set difference between the 384-case approved floor and the 214-case v1 floor is exactly 170 unique approved additions with digest `93632988fa4c409847cd0976e11d5deea8c5a7cef01eaea9e8889e5bcbb01180`.
- The final Owner-review XML contains 473 unique runtime display IDs with digest `282fb65c9637e6688fd8bb9a22fe228f6546c74b56c1d8fc387abae10291a332`.
- Set inclusion is exact: `214 ⊆ 384 ⊆ 473`; there are zero missing v1 IDs, zero missing approved cumulative IDs, 170 approved additions, and 89 pending additions. No duplicate current runtime ID exists.
- The restored `SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBeRepositoryRelativeExistingFilesWithHashes` method body is byte-for-byte identical to the method at the signed source commit, and its exact fully qualified ID appears in the 473-case XML.

### Seven category mappings

All seven required categories occur exactly once. Every mapped requirement ID exists in the obligation inventory, every mapped test ID exists in the final 473-case runtime inventory, and every per-category test list is duplicate-free:

| Category | Requirement IDs | Exact test IDs |
|---|---:|---:|
| tenant isolation | 4 | 35 |
| idempotency | 4 | 22 |
| contract validation | 4 | 22 |
| redaction replay | 3 | 30 |
| provider portability | 5 | 15 |
| projection freshness | 9 | 16 |
| governance audit-pairing | 5 | 56 |

### Obligations and zero-orphan proof

- The inventory contains 969 unique obligations: 20 initiative FRs, 104 Feature FRs, 77 Feature NFRs, 52 UX decisions, 52 UX acceptance obligations, 196 public contracts, 7 public clients, 15 current controls, and 446 conformance assertions.
- Independent closure validation found 446 runtime-test-identity closures, 28 exact-test-traceability closures, 218 source-bound evidence closures, and 277 pending governed-disposition closures: 969/969 total and zero invalid closures.
- All 969 source paths exist, their file hashes match, and their source line numbers fall within the referenced files.
- The 277 governed disposition IDs are unique, point back to the correct obligation, remain `pending`, have `approvalEvidence: null`, and carry `releaseActivationEffect: none`.
- There are zero orphan IDs, zero duplicate obligation IDs, zero missing source IDs, zero unmapped required categories, zero missing mapped tests or requirements, and zero missing denominator IDs.

### Hashes, schema, commands, and authority

- Every one of the 13 artifact bindings and all three Owner-review overlay bindings exists and matches its SHA-256 and byte count.
- The digest sidecar matches the final JSON, Markdown, and schema bytes.
- `python3 _bmad/scripts/generate_preservation_traceability_manifest_v3.py --check` passes byte-exactly.
- The schema passes Draft 2020-12 meta-validation, and the canonical JSON validates against it.
- Independent negative schema mutations for shrinkage, false gate states, activation inference, orphaning, and missing bindings/dispositions are rejected.
- The hash-bound toolchain capture records .NET SDK `10.0.401`, SDK commit `e34a38d2ae`, MSBuild `18.9.11+e34a38d2a`, host/runtime `10.0.12`, xUnit `4.0.1+8ed8aa354c`, and target framework `net10.0`; its SHA-256 is `c00637000ab6fe4b780b2d7a413501f690989734789003ffc9b02846eae2dbf8`.
- Both build bindings require that exact toolchain capture. Independent negative mutations that remove the toolchain artifact, command binding, or either build's capture are schema-rejected.
- The current Release build identity and 0-warning/0-error result match the manifest; the final conformance executable hash is `f4a84a293b143f9b11f9fc54ba1365a7ee1acfe6f6e54d7302454fe8f3d1ba96`.
- The v1 baseline hash, signed report hash at its source commit, signed Owner-decision hash, predecessor hashes, build-log hashes, and both XML hashes recompute.
- Historical AppHost evidence is accurately disclosed as `working-tree-validation-only`, bound to root base `c0abd5cb...`, EventStore `27cc17f...`, and `rootWorkingTreeCommitted=false`; it is not promoted to final-HEAD proof.
- Successor approval is `pending`; approver, approval time, approval reference, and signature are null. No new authority, ownership, waiver, or signature record is asserted.
- The immutable policy contains all seven prohibited operations: removal, replacement, merging, reclassification, waiver, substitution, and denominator shrinkage.

## Commands used

```text
git status --short --branch
git rev-parse HEAD HEAD^{tree} HEAD^ origin/main
git submodule status
git diff-tree --no-commit-id --name-status -r -M c0abd5cb... d956c9b...
sha256sum docs/release-evidence/preservation-traceability-manifest-v3.{json,md,schema.json}
python3 _bmad/scripts/generate_preservation_traceability_manifest_v3.py --check
dotnet --info
dotnet msbuild -version
Draft202012Validator.check_schema plus canonical instance validation
negative schema validation for shrinkage, activation, gate, orphan, category, binding, and disposition mutations
independent Git-object, SHA-256, set-inclusion, xUnit XML, category-map, source-line, activation-state, closure, and governed-disposition checks
```

## Owner-review disposition

The exact candidate that may be presented for confirmation is version `3.0.0-rc.1`, canonical JSON SHA-256 `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`. Confirmation must remain a pending manifest version/digest decision only; it cannot override the 17 failing conformance tests, the failed universal SM-C2 rule, blocked OQ-1 evidence chain, or active implementation hold.
