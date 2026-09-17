# Preservation Traceability Manifest v3 — Detached Owner-Approval Integrity Review

- **Review date:** 2026-09-17
- **Review scope:** Detached formal approval only: the self-attested authority record, detached approval record and sidecar, memlog decisions M147–M149, and the exact bound manifest.
- **Gate result:** **PASS WITH ONE NON-BLOCKING DURABILITY LIMITATION**
- **Approval effect:** The exact traceability contract for manifest `3.0.0-rc.1` is formally approved through the detached record. This does **not** make the release eligible, satisfy conformance, waive SM-C2, close OQ-1, or lift the implementation hold.

## Findings by severity

### Critical — 0

No critical findings.

### High — 0

No high findings.

### Medium — 0

No medium findings.

### Low — 1

#### L1 — The detached approval chain is not yet commit-bound

The authority record, detached approval record, approval sidecar, and relevant memlog additions exist only in the current working tree. `git status --short --branch` reports the authority and approval files as untracked and `.memlog.md` as modified at `HEAD d956c9b`.

This does not invalidate the directly attested Owner decision or any verified content/hash binding, and no commit is authorized by this review. It does mean the approval evidence cannot yet be cited by a durable repository commit. Preserve the exact reviewed bytes when an authorized commit is eventually made.

## Authority and user-statement support

**PASS.** The principal and authority fields are supported without inventing an external identity, signature, or broader authority:

- The principal name `Jérôme Piquot` is supported by the user's exact statement recorded at current memlog file line 92: “I Jérôme Piquot approve”.
- The `Owner` role is supported by the user's exact statement recorded at current memlog file line 95: “I am the Owner, do the needed records”.
- Logical memlog M147 confirms the exact candidate identity: version `3.0.0-rc.1` and canonical JSON SHA-256 `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`.
- Logical memlog M148 records the user's explicit `formal` approval intent.
- Logical memlog M149 records that the user's Owner authority covers approval of this exact FR-20/SM-C1 manifest effective `2026-09-17`.
- The authority and approval artifacts correctly use calendar-day precision: `effectiveAtUtc` is `null`; no unsupported time-of-day is inferred.
- Both principal records set `stableExternalIdentifier` to `null` and `externalIdentityVerification` to `not-performed`.
- The detached approval sets `attestation.signature` to `null` and describes the mechanism as direct repository-recorded statements with no cryptographic-signature claim.

The M149 scope fence is preserved: the record does not approve failed tests, waive the 100% rule, waive SM-C2, approve OQ-1, activate legacy requirements or grants, authorize release, or lift the implementation hold.

## Scope review

**PASS.** Authority is limited to requirements `FR-20` and `SM-C1` and to the exact traceability contract:

- exact versioned manifest approval;
- immutable/additive test denominator approval;
- seven-category test-to-requirement mapping approval; and
- preservation-obligation closure and activation-classification approval.

The action concerning activation classifications approves only the classifications contained in the hash-bound traceability contract. Explicit non-claims prevent it from activating FR-16, legacy Feature-FR/Feature-NFR/UX obligations, repository grants, package publication, release, or deployment.

The approval binds, without widening, the exact contract values in the manifest:

| Contract element | Approval | Bound manifest | Result |
|---|---:|---:|---|
| Original v1 floor | 214 | 214 | Match |
| Later approved additions | 170 | 170 | Match |
| Approved cumulative floor | 384 | 384 | Match |
| Pending additions retained | 89 | 89 | Match |
| Current candidate denominator | 473 | 473 | Match |
| Preservation categories | 7 | 7 | Match |
| Required obligations | 969 | 969 | Match |
| Computed closures | 969 | 969 | Match |
| Orphans | 0 | 0 | Match |
| Release-activated legacy requirements | 0 | 0 | Match |

The seven categories are exactly `tenant-isolation`, `idempotency`, `contract-validation`, `redaction-replay`, `provider-portability`, `projection-freshness`, and `governance-audit-pairing`. The approval and bound manifest contain the same seven prohibited operations: removal, replacement, merging, reclassification, waiver, substitution, and denominator shrinkage.

## Hash and binding verification

**PASS.** All hashes were recomputed from current bytes.

| Artifact | Recomputed SHA-256 | Binding result |
|---|---|---|
| Authority JSON | `735bea27888f85477a64efc81b0a400f65657e85f9d525a5efddaa9964578cf0` | Matches approval and approval sidecar |
| Detached approval JSON | `23e07e8339d8822c35ef665d44a60238e237c4c29e37560d6f475585dd27df5a` | Matches approval sidecar |
| Detached approval sidecar | `bde327c4c73bd598f41f56007da76648cc8080f654a87d931230e40cb87e9bda` | Independently recorded; `sha256sum --check` passes both listed entries |
| Manifest JSON | `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc` | Matches detached approval |
| Manifest Markdown | `c3dad90b6f0feb567bc1fa26a677dc3d88e23b112fd9767b30dfaecadf0e84f8` | Matches detached approval |
| Manifest schema | `3abf8f0d90c2257e865353743ca612b71b0d425d53196769a57dcb66525fbdfb` | Matches detached approval |
| Manifest digest sidecar | `d88f1ccad166046cf17e3ae499dffb26cf3e8bfe9f7f24ac26d9190e67533979` | Matches detached approval; its three manifest-family entries verify |
| Owner-review Conformance XML | `e918617c10d1b2722e7b72fc7e441429b068ef56699f1d8d6ef33a436c051549` | Matches approval snapshot and manifest binding |

The approved source commit also matches the manifest exactly: `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49`.

## Detached-record consistency

**PASS.** The bound manifest remains byte-identical and internally records `status: pending-owner-approval` with null approver, approval reference, timestamp, and signature. The detached approval explicitly discloses that immutable internal state and identifies itself as the effective approval source. This is not a contradiction: the external record supplies the formal decision without changing the exact bytes the Owner approved.

The approval's `manifestApprovalPrerequisite: satisfied-by-this-record` is limited to the approval prerequisite. It does not claim that the full FR-20/SM-C1 acceptance gates pass.

## Fail-closed invariant verification

**PASS.** Independent XML parsing found exactly 473 test records: 456 `Pass` and 17 `Fail`. The approval snapshot, bound manifest, and non-claims remain aligned:

| Invariant | Required state | Approval state | Manifest state |
|---|---|---|---|
| FR-20 | `PENDING` | `PENDING` | `PENDING` |
| SM-C1 | `PENDING` | `PENDING` | `PENDING` |
| SM-C2 | `FAILED` under the universal `<=5%` rule | `FAILED` | `FAILED` |
| OQ-1 | `BLOCKED` | `BLOCKED` | `BLOCKED` |
| Implementation hold | `ACTIVE` | `ACTIVE` | `ACTIVE` |

Formal approval therefore closes only the exact-manifest Owner-approval prerequisite. Conformance remains `456/473`, so FR-20 and SM-C1 remain PENDING; SM-C2 remains FAILED, OQ-1 remains BLOCKED, and the implementation hold remains ACTIVE.

## Final decision

**PASS.** The detached formal approval is supported by direct user statements, binds the exact approved manifest and authority bytes, is narrowly limited to the traceability contract, and contains no invented signature or external-identity proof. No release or broader governance gate changes. The sole residual limitation is that the new approval evidence is still worktree-only and awaits an explicitly authorized commit for durable repository retention.
