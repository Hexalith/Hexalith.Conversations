# Preservation Manifest v3 — Detached Owner-Approval Invariant Review

## Verdict

**PASS — the formal detached Owner approval is supported and narrowly scoped.** The authority record, approval record, digest sidecar, memlog entries M147–M149, bound manifest, and PRD acceptance rules form a consistent chain. The approval satisfies only the exact manifest-approval prerequisite for FR-20/SM-C1. It does not establish 100% conformance, pass FR-20 or SM-C1, change SM-C2, approve OQ-1, activate a legacy requirement or repository grant, authorize release, waive an obligation, or lift the implementation hold.

No invariant violations remain.

## Approval support chain

1. **M147 — exact identity confirmation.** The user confirmed manifest `3.0.0-rc.1` and canonical JSON SHA-256 `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`, while the entry explicitly denied any inference of formal approval or gate effect.
2. **M148 — formal intent.** The user explicitly stated that the preceding confirmation was intended as formal Owner approval. The entry correctly withheld effective approval until FR-20/SM-C1 authority was confirmed.
3. **M149 — authority scope and date.** The user explicitly confirmed Owner authority for approval of this exact FR-20/SM-C1 manifest, effective 2026-09-17. The entry expressly preserved failing tests, the 100% rule, SM-C2, legacy/grant activation, release authority, and the implementation hold.
4. **Authority record.** `PRESERVATION-MANIFEST-OWNER-AUTHORITY-001` reproduces those three statements, limits its effective scope to FR-20/SM-C1 approval of the exact version and digest, records calendar-day precision, and labels the basis `direct-owner-attestation`. External identity verification and stable external identity remain absent and unclaimed.
5. **Detached approval record.** `PRESERVATION-MANIFEST-V3-OWNER-APPROVAL-001` binds the exact authority record and immutable manifest package without changing the canonical manifest bytes. Its decision is `approved`, while the canonical manifest's internal state remains `pending-owner-approval`; the detached record is explicitly the effective approval source.

## Hash and identity verification

All recomputed bindings pass:

| Binding | SHA-256 / result |
|---|---|
| Owner authority record | `735bea27888f85477a64efc81b0a400f65657e85f9d525a5efddaa9964578cf0` |
| Detached approval record | `23e07e8339d8822c35ef665d44a60238e237c4c29e37560d6f475585dd27df5a` |
| Canonical manifest | `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc` |
| Manifest Markdown | `c3dad90b6f0feb567bc1fa26a677dc3d88e23b112fd9767b30dfaecadf0e84f8` |
| Manifest schema | `3abf8f0d90c2257e865353743ca612b71b0d425d53196769a57dcb66525fbdfb` |
| Manifest digest sidecar | `d88f1ccad166046cf17e3ae499dffb26cf3e8bfe9f7f24ac26d9190e67533979` |
| Current Conformance XML | `e918617c10d1b2722e7b72fc7e441429b068ef56699f1d8d6ef33a436c051549` |
| Approval digest sidecar check | approval and authority records both `OK` |

The approved contract matches the bound manifest exactly: source commit `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49`, 214 original tests, 170 later approved additions, 384 approved cumulative tests, 89 retained pending additions, 473 current candidate tests, seven categories, 969 preservation obligations, 969 closures, zero orphans, and zero release-activated legacy requirements.

## Gate-effect verification

| Invariant | Verified state |
|---|---|
| Manifest-approval prerequisite | `satisfied-by-this-record` |
| FR-20 | `PENDING` |
| SM-C1 | `PENDING` |
| SM-C2 | `FAILED` under the universal `<=5%` rule |
| OQ-1 | `BLOCKED` |
| Implementation hold | `ACTIVE` |
| Current candidate Conformance | `FAIL`, 456 passed / 17 failed / 473 total |
| Legacy release activation | 0 requirements |
| FR-16 | remains deferred and not active |
| Repository grants | not activated |
| Waiver or denominator mutation | none |
| Release/deployment authority | none |

The approval record's `gateEffect` and `nonClaims` enforce these boundaries directly. The authority record independently repeats the same limits, including no approval of failing Conformance, no change to the 100% rule, no SM-C2 waiver, no OQ-1 approval, no effective grants, no release authority, and no hold lift.

## PRD consistency

FR-20 and SM-C1 require both an approved hash-bound manifest and 100% passage of the approved denominator. The detached record satisfies the approval component only; the recorded 456/473 Conformance result leaves both gates pending. PRD §14 continues to separate preservation membership from release activation, and the approved manifest records zero release-activated legacy requirements.

The canonical PRD and manifest retain their pre-approval/internal `pending-owner-approval` wording by design so their approved hashes do not change. The detached record supplies the effective approval overlay and explicitly limits its effect to the approval prerequisite. That immutable-internal-state/detached-effective-state distinction is stated in the approval record and creates no release or activation inference.

## Findings

None. Formal approval is supported, exact, detached, and fail-closed outside its stated prerequisite.
