# OQ-1 Owner Record Gate Review

**Reviewed:** 2026-09-17  
**Verdict:** PASS WITH THREE MEDIUM, FAIL-CLOSED RECORD-QUALITY FINDINGS  
**Gate effect:** The current OQ-1 record chain is safe to retain as a blocked planning record. It does not make any grant effective, approve implementation or release, lift the implementation hold, approve FR-20/SM-C1, waive SM-C2, or activate FR-16. The three findings below should be remediated before any successor record claims technical closure.

## Scope

This gate reviewed:

- `oq-1-owner-authority-v1.json`;
- `oq-1-technical-evidence-v1.json`;
- `oq-1-eventstore-change-grant-v1.json`;
- `oq-1-commons-change-grant-v1.json`;
- `oq-1-conversations-consumer-grant-v1.json`;
- `oq-1-landing-zone-approval-v1.json`;
- the current `prd.md` and `addendum.md`;
- the ten XML-encoded TRX artifacts referenced by the technical-evidence record;
- the source documents, root commit, root gitlinks, submodule commits, tags, and implementation-provenance commits referenced by the record chain; and
- memlog entries M88 and M91 and their immediate record-creation sequence.

## Gate results

### 1. Authority stays within the user's attestation — PASS

M88 preserves `I Jérôme Piquot approve`. M91 preserves `I am the Owner, do the needed records` and records that it answered the request for the role and versioned authority governing EventStore, Commons, and Conversations. The Owner record reproduces both statements verbatim and limits their derived scope to OQ-1, FR-10 through FR-15, and those three repositories.

The record is explicitly `effective-self-attested`; `stableExternalIdentifier` remains null; external identity verification is `not-performed`; and the record disclaims external forge/package permissions, technical-evidence substitution, release authority, hold lift, FR-20/SM-C1 approval, SM-C2 waiver, and FR-16 activation. No additional person, owner, approver, waiver, external permission, or signature is invented.

The mapping choice and creation of the three records are within the user's contextual approval and direction to create the needed OQ-1 records. They are not represented as release approval or technical acceptance.

### 2. Existing hash and source bindings are correct — PASS

Mechanical verification produced the following results:

- all six JSON records parse;
- all seven aggregate `sourceBindings` SHA-256 values match their referenced bytes;
- all five aggregate `recordBindings` SHA-256 values match their referenced records;
- all three grants reference the current Owner-record hash and current technical-evidence hash;
- all ten passing-run SHA-256 values match their XML artifacts;
- the XML counters independently total 341 executed, 341 passed, and 0 failed across the ten passing runs;
- root `HEAD` equals `8bdf0268076761905107698c9d4e323d76f07f9e`;
- the root gitlinks and observed submodule heads equal EventStore `27cc17f37774dde958a4d5bf9ba1e9b8d04ce07e` and Commons `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6`;
- EventStore tag `v3.106.0` resolves to `76051c70cbf868c40edc00ca0344fa5bd8879b69` and is two commits behind the bound EventStore snapshot;
- Commons tag `v2.30.0` resolves to `426e1a196ed1b018d11500f43dc8d6740a5f4994` and is 21 commits behind the bound Commons snapshot; and
- every implementation-provenance commit exists in the named repository and is an ancestor of the corresponding accepted source snapshot.

The root worktree changes are confined to the current planning/evidence update. EventStore and Commons are at the bound commits with clean worktrees.

### 3. Technical status remains accurately fail-closed — PASS

The technical-evidence record is `result: BLOCKED`. It records 341 passing focused tests without converting them into compatibility or release acceptance. The opt-in AppHost runtime boundary is `FAIL` / `blocked-current-runtime-evidence`; the IdentityModel 8.19.2/8.22.0 assembly-version conflict remains unresolved; package publication and package-consumer proof are absent; the external NuGet population is expressly not covered; and rollback remains conditional because no known-good production identity, backward-reader proof, or data/key proof exists.

The aggregate is `1.0.0-draft.3`, `status: evidence-blocked`, and `result: BLOCKED`. Its grant, release, compatibility, and rollback ledger entries remain `BLOCKED`. The PRD remains `status: draft`; OQ-1 acceptance is BLOCKED; all three grants are described as issued but ineffective; and the addendum carries the same state.

The three grant records are each `effective: false`, have null effective timestamps, and expressly disclaim release, package publication, hold lift, and other out-of-scope authority. No implementation, package publication, release, waiver, or rollback execution is represented as approved.

### 4. Preservation and performance invariants remain intact — PASS

The PRD retains all required fail-closed invariants:

- implementation hold: **ACTIVE**;
- FR-20/SM-C1: **PENDING** until one approved, hash-bound manifest maps all seven closed categories to exact tests, retains every v1 denominator test and later approved addition, and has zero orphaned active obligations;
- denominator reduction, substitution, reclassification, merging, or waiver cannot establish acceptance; and
- SM-C2: **FAILED** under the universal rule that every identified command/read hot path must regress by no more than 5% P95 under the frozen comparable envelope.

The OQ-1 records explicitly disclaim any change to those states.

## Findings

### M1 — Failed runtime and build-warning observations are not raw-artifact-bound

The ten successful runs are path-and-hash bound. In contrast, `testEvidence.runtimeBoundary` contains only the command and a prose failure summary; it has no log/TRX path, SHA-256, start/end time, exit code, or environment identity. `buildObservations` likewise records MSB3277 without a bound build log. The record correctly remains BLOCKED, so this gap cannot create a false pass, but the precise Dapr/JWT and IdentityModel assertions are not independently replayable from the record set.

**Safe remediation:** add explicit `rawArtifact: null`, `hashBindingState: unavailable`, and `reproductionRequired: true` fields now; on the next run, preserve and hash-bind the complete runtime result and build log with command, exit code, time, and environment/source identity. Do not synthesize a missing prior log.

### M2 — The tracked-consumer scope statement is broader than the recorded matrix

`trackedConsumerMatrixScope` claims production, sample, **and test** project references in the root and initialized root-declared repositories. The matrix omits direct tracked test consumers and some direct in-repository consumers. Examples include `Hexalith.Commons.Serialization.Tests`, `Hexalith.Conversations.Contracts.Tests`, `Hexalith.EventStore.DomainService.Tests`, `Hexalith.EventStore.Sample.Tests`, `Hexalith.EventStore.AppHost.Tests`, `Hexalith.Folders.IntegrationTests`, and `Hexalith.Conversations.AppHost.Tests`; it also omits direct EventStore-internal consumers such as `Hexalith.EventStore.Server` and `Hexalith.EventStore.Testing.Integration` from the DomainService row.

The evidence already says package compatibility is unclaimed and technical acceptance is BLOCKED, so this does not authorize an unsafe release. It does make the matrix description inaccurate.

**Safe remediation:** either enumerate every direct tracked production/sample/test reference within the declared scan boundary, with an explicit rule for owning-project/self-test exclusions, or narrow `trackedConsumerMatrixScope` to the exact classes actually listed. Keep external/unknown consumers excluded and do not upgrade the compatibility state.

### M3 — Current grant activation wording could be read as self-activating

All three grant records are currently and correctly ineffective. Their `activationCondition` fields, however, say the grants become effective when evidence conditions close, without stating that immutable v1 records remain `effective: false` and require a separately versioned successor grant to record activation. A future consumer could incorrectly treat a successor evidence record alone as changing these existing records.

**Safe remediation:** state in every grant that v1 never auto-activates; technical closure is necessary but not sufficient; a hash-bound, versioned successor grant must explicitly supersede v1 and record `effective: true` plus its effective time. Preserve every current non-claim.

## Conclusion

The OQ-1 Owner authority is supported by the user's exact contextual attestation and does not extend into external identity, release, waiver, or unrelated-gate authority. Every existing record, source, grant, and successful-test hash binding checked by this review is correct. The record chain remains consistently BLOCKED and all grants remain ineffective.

The three medium findings concern audit completeness and future interpretation, not a current false authorization. Apply their safe clarifications without manufacturing missing evidence, changing the technical result, or weakening the ACTIVE hold, FR-20/SM-C1 denominator, or universal SM-C2 rule.
