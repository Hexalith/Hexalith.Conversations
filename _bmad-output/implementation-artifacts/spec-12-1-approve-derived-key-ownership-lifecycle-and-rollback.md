---
title: 'Story 12.1: Approve derived-key ownership, lifecycle, and rollback'
type: 'feature'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8da7f2bc8997c03a6aea3d72464369496c7b9717'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Derived-key lifecycle lacks the approved decision required before optimization.

**Approach:** Produce ADR 0005, deterministic evidence, independent validation and the six-scenario record contracted by `12.1.json`.

**Decision (owner, 2026-11-16):** Story 16.3 is done (`8da7f2bc`), so entry is open. Proof is native: AC-12.1-02..05 run as contracted. AC-12.1-01 (the candidate-verifier route) and AC-12.1-06 (the v2 final pair) are waived and recorded as waived, never PASS. Story 12.2 binds 12.1 by its done spec, sprint row and commit.

## Boundaries & Constraints

**Always:** EventStore alone owns domain writes. Cover seven families, including resolution metadata/selection/proof/fence; define ownership, ordering, compatibility, rebuild, backfill, deletion, expiry and rollback. Preserve isolation and fail-closed recovery; reads never repair. Label proposed behavior. Obtain named owner approval of the presented decision's exact digest.

**Never:** Change runtime, benchmarks, dependencies, gitlinks or history; imply 16.3 completion/approval. Story rollback removes 12.1 additions; runtime rollback rebuilds from EventStore.

## I/O & Edge-Case Matrix

| Scenario | Expected result |
| --- | --- |
| Complete lifecycle and approved digest | Deterministic, independently verified pair |
| Omitted family/field | `DERIVED_KEY_ADR_INCOMPLETE` |
| Second write authority or read repair | `EVENTSTORE_AUTHORITY_VIOLATED` |
| Unsafe transition or rollback | `DERIVED_KEY_TRANSITION_UNSAFE` / `ROLLBACK_PLAN_INVALID` |
| Absent or stale approval | `ADR_APPROVAL_MISSING` |
| Unavailable history | BLOCKED; never PASS |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs:45-100`: seven Conversations families.
  - Conversation key: `projection:conversations:<seg(t)>:<seg(c)>`.
  - Tenant index key: `…-index:<seg(t)>`. It is tenant-wide.
  - Dispatch ledger key: `…-dispatch:<sha256(dispatchId)>`. It has no tenant segment, and isolation is checked on every access (`ConversationAsyncProjectionHandler.cs:1014-1031`).
  - Resolution metadata, selection, proof and fence keys: `…-resolution[-selection|-proof|-fence]:<sha256(t)>:<sha256(c)>`.
  - DC-7 (`architecture.md:2538-2557`) ratifies only the first three families and the store name `statestore`. The store name is now configurable (`ConversationProjectionStoreOptions.cs`).
- `ConversationAsyncProjectionHandler.cs:133-289`: `ProjectAsync` write order.
  - Rejected input writes a proof barrier, then writer and generation recovery runs.
  - The ledger acquire is create-only, with TTL = `RedeliveryWindow` (:75).
  - Proof and fence are marked pending, then the index gets a pending reference, then the conversation is written and merged into the index (`ConversationProjectionReadModelWriter.cs:98-197`).
  - The metadata/selection/fence batch runs, all families are verified, fence then proof complete, and the ledger is marked Completed.
  - Writes are CAS or newest-wins, so retries are idempotent.
- `ConversationAsyncProjectionHandler.cs:298-527`: `PrepareRebuildAsync` returns one ETag or create-only batch plan and writes nothing itself. It rejects state newer than the plan (PartialRetry) and a reused ledger with a different identity.
- Writers outside the projection route:
  - TenantCreated index initializer (`TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs:102-106`, Writer:323-334).
  - SDK batch recovery.
  - Tenants-owned `tenants-projection:v1:*` keys share the store and are out of scope.
- `ConversationProjectionReadStore.cs:237-239`, `ConversationProjectionReadService.cs:68-117`, `ConversationQueryHandler.cs:253-538`: reads only Get or GetMany and map bad state to trust states. Proof: `ConversationTenantIndexReadMatrixTest.QueryPathsShouldNeverWriteInAnyState`.
- Absent today, so the ADR labels these "proposed":
  - migration and backfill
  - any production delete
  - expiry other than the ledger
  - rollback
  - an in-code grammar version (index `LifecycleSchemaVersion = 2` is the only in-record version)
- `tests/Hexalith.Conversations.Server.Tests/Projections/`: evidence for the ADR to cite.
  - `ConversationAsyncProjectionHandlerTest`: `AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable`, `SecondWriteFailureShouldBeRetryableAndRetryShouldConverge`, `FullReplayPlanShouldContainExactGenerationAndLedgerKeysWithoutMutatingLiveState`.
  - `ConversationResolutionMetadataProjectionTest.LedgerExpiryAndRestartRecoverOriginalGenerationAndRejectIdentityReuse`.
  - Integration test `DerivedStateDeletionAndFullReplayShouldRestoreEquivalentKeysAndQueries`.
- Tooling to reuse:
  - `_bmad/scripts/generate_thin_authoring_guidance.py`: `canonical` :139; `build_signable` :276-310, which takes its candidate from the contract and PC, not HEAD; approval outside the payload :359-429.
  - `verify_thin_authoring_guidance.py`: :25-109, `FAULT_EXPECTATIONS`, exit codes 0/1/2.
  - `authoring_outputs.write_outputs` :30 and `evidence_boundary_current.bridge` :76.
- `tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs`: const pins and `ReleaseEvidenceArtifactCollection`. A new `.cs` file with no csproj edit compiles internal-only and can see Server types.
- Predecessor digests for the decision payload:
  - PC `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`.
  - Story 6.2 record `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md`, with candidate `2971ab79…`. Do not use the disposition's `b261fe20…`, which is a deletion proof only.
  - Baseline v1: `docs/release-evidence/sm-c2-hot-path-baseline-v1.{json,md,xunit.xml}`.
  - The contract's inventory rows recompute to its sha.
- Leave `verify_story_candidate.py` and `generate_story_record.py` untouched. `verify_story_candidate.py` is a source of the 11.3 record.

## Tasks & Acceptance

**Execution:**
- [x] `docs/adrs/0005-conversation-derived-key-lifecycle.md`, `docs/adrs/index.md`: for each family, state the owner, key, writer, order, compatibility, rebuild, backfill, deletion, expiry and rollback in one machine-readable lifecycle table. Mark which behavior is current and which is proposed, and cite evidence tests.
- [x] `docs/release-evidence/derived-key-lifecycle-decision-v1.schema.json`: a closed schema for the candidate, families, lifecycle, approval and digests.
- [x] `_bmad/scripts/{generate,verify}_derived_key_decision.py`:
  - Parse the ADR table into a canonical signable payload. Approval stays outside it, set via `--approve-reviewer`/`--decided-at-utc` and bound to the payload digest.
  - The payload binds the predecessor digests listed in the Code Map.
  - The verifier recomputes everything independently.
  - Unresolvable PC or Git returns BLOCKED, exit 2.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/DerivedKeyLifecycleAdrValidationTest.cs`: test `EventStoreShouldRemainOnlyWriteAuthority`.
  - Pin the ADR, decision and family set as consts.
  - Each family's key prefix from the runtime key builders must equal the decision's prefix.
  - Each family must name the projection route as its sole writer, with read repair `false`.
  - File reads only, with no Git history, so the test is safe in CI.
- [x] `_bmad/scripts/tests/test_derived_key_decision.py`: `lifecycle_fault_matrix` through the real CLIs. Cover every family × field, the matrix rows, and unavailable history. Each case needs an exact blocker, a nonempty ledger and restored bytes.
- [x] `.gitattributes`: add LF rules for byte-bound outputs if they are not already covered.
- [x] Generate the pair, present its digest, and HALT for owner approval. Then regenerate with the approval, verify with `--require-approval`, and commit after commitlint.
- [x] This spec, `sprint-status.yaml` and `deferred-work.md`:
  - Transition the 12.1 row and replace the stale 16.3 comment at :116 with "AC-01/06 waived (owner, 2026-11-16)".
  - Close the deferred verifier-generalization entry as waived.

**Acceptance Criteria:**
- Given ADR 0005, when AC-12.1-02 runs twice, then it exits 0, the pairs are byte-identical, and every family × field is nonempty.
- Given the decision, when the Release AC-12.1-03 runs, then exactly one test passes and runtime key prefixes equal the decided families.
- Given approval bound to the digest, when AC-12.1-04 runs, then it reports PASS. Any later ADR edit yields `ADR_APPROVAL_MISSING`.
- Given each fault, when AC-12.1-05 runs, then the exact blockers occur and the fixture bytes restore.
- Given the change, when diffed against baseline, then `src/`, benchmarks, packages and gitlinks are untouched.

## Implementation Notes

2026-11-16 implementation, **halted for owner approval** at the "present its digest" task. Nothing is
staged or committed.

- ADR 0005 states 7 families x 11 fields in one machine-readable table (77 rows). The 11 fields are the 10
  listed plus `readRepair`, which AC-12.1-03 asserts. Each row has a `current`/`proposed` status, one rule
  token, a statement and evidence IDs. A legend `E01`-`E23` maps each ID to a test that must be declared in
  its cited file. Proposed rows follow the Code Map list (compatibility, backfill, deletion, rollback, and
  expiry except the ledger).
- The generator applies these rules: one approved token per field, the frozen v2 key templates, the status
  labels, resolvable evidence, and 11 named write-order invariants (`ORDER_INVARIANTS`) over the
  accepted-append path.
  - The ledger is acquired first and completed last.
  - The chain runs proof claim, fence claim, index pending reference, conversation write, summary merge,
    then the shared batch.
  - The fence completes after its batch and before the proof.
  - Three rebuild invariants (each family once, proof first, then the decided order of the rest) jointly pin
    proof 1 ... selection 7.
  - "The proof claim precedes every rich write" was dropped as a named invariant: it follows from the chain
    and could never fail alone.
- The candidate binding:
  - The candidate is the contract plus the planning candidate, never HEAD. The contract digest must equal its
    `v9-authority-bundle-v1.json` row.
  - The entry inventory rows are parsed from `epics.md` and recompute to the contract digest.
  - The Story 6.2 record is `done` at `file_list_commit` `2971ab79...`, which is an ancestor of the PC. The
    record and the three SM-C2 baseline v1 files are byte-identical to their PC blobs.
  - Story 16.3 is bound at `8da7f2bc...`: the spec blob and the sprint row there must both be `done`.
- Approval stays outside the payload. On top of `--approve-reviewer`/`--decided-at-utc`, approval requires
  `--approved-payload-sha256`, which must equal the recomputed digest. This binds the approval to the digest
  that was presented. Any later ADR edit makes the verifier report `ADR_APPROVAL_MISSING`.
- Deviations:
  - Git facts come from the shared `generate_story_record.run_git`, not `evidence_boundary_current.bridge`.
    The bridge rebuilds the TestSupport runner on every call. That build needs the source-mapped environment
    and failed with `HELPER_FAILURE` here, and the fault matrix makes about 340 CLI calls.
  - The contracted AC-12.1-03 path `bin/Release/net10.0` cannot be produced. The TFM has been `net11.0` since
    `bba20230` (2026-11-14). Source mode rejects Release with `HXW006: Source mapping property 'Configuration'
    expected 'Debug' but was 'Release'`. Package mode is blocked by HXW011/HXW009/HXW010 (retro item 50). The
    same method therefore ran from `bin/Debug/net11.0`, with `< /dev/null`: a failing run otherwise waits on
    its inherited stdin socket.
  - The C# test pins the decision's `signablePayloadSha256`, not the JSON file hash, so the approval rerun
    leaves the pin valid. The runtime key builders are `internal` and no `src/` change is allowed, so the test
    reaches them by reflection.
  - The AC receipts under `artifacts/v9/12.1/` stay untracked (git-ignored) because the final pair is waived.
- Results (system `python3` 3.14.4, `TMPDIR=/var/tmp` for pytest only):
  - AC-12.1-02 ran twice: exit 0 both times, byte-identical. JSON `e110b6e2...`, MD `bd922818...`.
  - AC-12.1-03: 1/1 passed, 0 skipped.
  - AC-12.1-04: `FAIL`, `ADR_APPROVAL_MISSING`. This is expected until the approval is recorded.
  - AC-12.1-05: 198 passed, 0 skipped, including the 11 isolated order-invariant cases. The full file passed
    199.
  - Neighbouring suites (anti-skip guard, authoring outputs, minimal module, thin guidance): 217 passed.
  - `check-root-submodules`: PASS. Tampering the decision's writer or a key prefix fails the C# test.
  - The Epic 16 tooling v2/v3 suites, which mutate `.gitattributes`, gave 178 passed and 1 failed. The failure
    is the pre-existing `test_held_selected_v3_emits_no_runtime_outputs`, a `SOURCE_DRIFT` on the unchanged
    `.config/dotnet-tools.json`, last moved by committed `523a1a5a`.
- To present: ADR `2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068`, signable payload
  `13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe`. After approval, rerun AC-12.1-02 with
  `--approve-reviewer "<owner>" --decided-at-utc <UTC> --approved-payload-sha256 <digest>`. Then rerun
  AC-12.1-04 (expect PASS) and AC-12.1-03, flip the ADR index row to Accepted, transition the sprint row and
  commit after commitlint. Do not commit the unapproved pair first: the contained writer refuses to rewrite a
  tracked publication.

- Completion (2026-11-16):
  - The owner reviewed and approved the post-review digests. The approval records `Jerome Piquot` at
    `2026-11-16T12:13:38Z`, bound to ADR `2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068` and
    payload `13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe`. ADR 0005 is Accepted in
    `docs/adrs/index.md`.
  - AC-12.1-02: exit 0, byte-identical, approval preserved. JSON `cddf2b9b…`, MD `c3d6ac60…`.
  - AC-12.1-03: 1/1 passed (Debug/net11.0, as disclosed above).
  - AC-12.1-04: PASS, exit 0.
  - AC-12.1-05: 198 passed, 0 skipped, exit 0.
  - Receipts are in the git-ignored `artifacts/v9/12.1/`.
  - Waived, observed, never PASS:
    - AC-12.1-01 exit 1, FAIL `CHECKPOINT_CANDIDATE_MISMATCH`: the candidate verifier accepts only 11.3.
    - AC-12.1-06 exit 1, FAIL `ASSERTION_LEDGER_EMPTY`, `RECORD_NOT_DERIVED`, `SCENARIO_COMMAND_UNSUPPORTED`,
      `WORKTREE_NOT_CLEAN`.
    - Neither wrote a file.
  - The story completion gate's v2 final-record steps and `--verify-inserted-record` are waived by the same owner
    decision.
  - The sprint row moves to `review`, per the bmad-build completion step.

## Spec Change Log

## Review Triage Log

Review pass 1 (2026-11-16): Blind Hunter (BH), Edge Case Hunter (ECH) and Verification Gap (VG) reviewed the full diff from `8da7f2bc` (untracked files included). No layer was skipped. No finding routed to intent_gap or bad_spec, so there is no loopback.

| ID | Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- | --- |
| BH-01 | `order_violations` never requires the proof claim before the fence claim, and only checks rebuild positions for "once" and "proof first". | medium | The `checks` dict (generator :374-390) has no proof<fence entry, and nothing pins rebuild positions 2–7. ADR lines 85-86 and 101-103 fix both sequences. | patch (with ECH-01, ECH-02) |
| BH-02 | The "ledger acquired first" invariant ignores the pre-acquire proof barrier and recovery writes, and `ReconcileAsync` writes have no decided order. | low | `ReconcileAsync` (Handler:88-130) writes through `TryReconcileTerminalDispatchAsync` and `TryRestoreTerminalAsync`, with no ADR order row. The ADR prose mentions recovery and the barrier but does not limit the numbered invariants to the accepted-append path. | patch (ADR prose; direct correction) |
| BH-03 | The C# test only checks that decided families exist at runtime, so an eighth runtime `*Key` builder would pass. | medium | `RuntimeKey` looks builders up by decided name and never lists `ConversationProjectionReadModelKeys`. It has 7 `*Key` builders plus 2 helpers (:45-95). A second writer in `src/` is outside this decision-check test, as specified. | patch |
| BH-04 | The C# test skips the expiry, compatibility and order sections, and its "independent" digest pin only compares the recorded string. | medium | Line 79 compares `digests.signablePayloadSha256` with a const. The section loop (:81-94) omits expiry, compatibility and writeOrdering. A hand edit there passes CI while the Python verifier would catch it. | patch (with ECH-16, VG-O2) |
| BH-05 | A schema validation failure in the generator is reported as BLOCKED `TOOLING_UNAVAILABLE`. | low | One `except Exception` (:795-800) wraps both import and validation. An evidence row citing a non-`tests/` path passes the earlier checks and exits 2. | patch (with ECH-05; direct) |
| BH-06 | `ROLLBACK_PLAN_INVALID` is never produced for a missing rollback, and grammar, rule and rollback prose are script constants. | low | Rejected. The frozen matrix maps an omitted field to `DERIVED_KEY_ADR_INCOMPLETE`. `ROLLBACK_PLAN_INVALID` fires on unsafe rollback tokens (the `reverse-write-*` and `rollback-repeats-side-effects` faults). A prose/payload contradiction is visible to the owner, who approves the ADR and the payload digests together. A prose parser adds complexity for an unlikely case. | reject |
| BH-07 | The contract is never compared with its authority-bundle row, and the 16.3 predecessor is unbound. | medium | `bind_candidate` hashes the working-tree contract but never reads `v9-authority-bundle-v1.json`. Neither 16.3 nor `8da7f2bc` appears in the payload, although the epic context makes 16.3 an entry requirement. | patch (with ECH-08) |
| BH-08 | The fault matrix misses some existing detection paths: the baseline `.md` file, parse faults, `json:parse`, `ARGUMENT_INVALID`, and a resigned key prefix. | low | `SOURCE_FAULTS` and `EVIDENCE_FAULTS` contain none of these cases. The code paths exist (`parse_adr` errors, `json:parse`, `payload:recompute`) but no test runs them. | patch (tests only, bounded subset) |
| BH-09 | Evidence checking only greps for the class and method names anywhere in the file, and the cited tests never ran. | low | Rejected for code: a wrong-class or commented citation is unlikely, and requiring the method inside the class with `[Fact]` adds guards. Instead, the cited Server tests run as verification of this review. | reject (verification follow-up) |
| BH-10 | Spec contradictions: the Release wording, the `created` date, status vs sprint row, wording inside the frozen block, and removed log sections. | low | Rejected: every fix edits this build's spec. The Spec Change Log and Review Triage Log headings are restored as workflow structure. The Debug/net11.0 deviation is disclosed in the Implementation Notes. | reject |
| BH-11 | ADR 0005 can never record its own acceptance, and approval provenance is weak. | low | Rejected by design. The ADR states it takes effect through the digest-bound approval in the bundle, and the index row flips to Accepted. The frozen intent's named approver is `approval.approver`, bound to both digests. | reject |
| BH-12 | The downstream cost of the AC-06 waiver for Story 12.2 is unrecorded, and the receipts stay ignored. | low | The 12.2 contract's `candidateBinding` requires the "Story 12.1 record", which is waived, and no 12.2-facing note exists. The receipts part is false: AC-02..05 can be reproduced by rerunning the committed tools. | patch (deferred-work handoff entry) |
| BH-13 | A stale approval is dropped silently, and approval flags are ignored without `--approve-reviewer`. | low | Rejected. stdout prints `approval: pending`. The approval is one supervised run. The fix adds argument guards. | reject (with ECH-09) |
| BH-14 | The schema approval definition uses bare strings, the three baseline entries share one id, and the 12.1 scripts have no LF rule. | low | The approval `$def` has no const, pattern or minLength, and the ids repeat `sm-c2-hot-path-baseline-v1`. Both are direct schema/constant edits. The LF part is false: no record hashes the 12.1 script bytes because the final pair is waived. | patch (approval def + distinct ids) |
| BH-15 | ADR deletion is not reconciled with the open governance retention/deletion and redaction-replay topics, and the story rollback list is incomplete. | medium | `docs/adrs/index.md` still lists "Retention, deletion, tombstoning, legal hold, export, and derived-index lifecycle" and "Redaction replay" as Proposed. ADR rollback (:125-127) omits the index row, the C# test, the pytest and the `.gitattributes` lines. | patch (ADR prose) |
| ECH-01 | The fence pending claim can be placed at or before the proof pending claim. | medium | Same defect as BH-01. | patch (BH-01) |
| ECH-02 | Rebuild-plan positions 2–7 can be permuted. | medium | Same defect as BH-01. | patch (BH-01) |
| ECH-03 | A cited method in another class or in a comment still resolves. | low | Same as BH-09. | reject |
| ECH-04 | An absolute or `..` evidence path can satisfy evidence with files outside the repository. | low | Rejected. `tests/../..` would match the schema pattern, but the ADR is owner-reviewed and the case is unlikely. A containment guard adds a branch. | reject |
| ECH-05 | A schema failure is reported as a blocked environment. | low | Same as BH-05. | patch (BH-05) |
| ECH-06 | Non-UTF-8 `epics.md` or missing inventory keys raise an uncaught traceback. | low | Rejected: unlikely, and the fix adds guards. A traceback exits non-zero, never PASS. | reject |
| ECH-07 | A deleted predecessor exits BLOCKED, not `PREDECESSOR_CANDIDATE_MISMATCH`. | low | Rejected. A missing input is a visible `INPUT_UNAVAILABLE` non-PASS. The case is unlikely, and the fix adds a branch. | reject |
| ECH-08 | 16.3 and any other contract predecessor are never checked. | medium | Same as BH-07. | patch (BH-07) |
| ECH-09 | `--decided-at-utc` without `--approve-reviewer` is silently ignored. | low | Same as BH-13. | reject |
| ECH-10 | A future `decidedAtUtc` is accepted. | low | Rejected. The owner supplies the timestamp in one supervised run, and the fix adds a guard. | reject |
| ECH-11 | Control characters in the approver name could forge Markdown lines. | low | Rejected: the value is an owner-supplied CLI argument, the case is unlikely, and the fix adds a guard. | reject |
| ECH-12 | The verifier reports `ADR_APPROVAL_MISSING` for an unapproved stale bundle, and payload drift goes unreported. | false | The `adr:digest` ledger row fails with "ADR 0005 changed after the decision was generated", and `adr:recompute` reports rule errors, so the drift is reported. The frozen matrix maps a stale approval to `ADR_APPROVAL_MISSING`. | reject |
| ECH-13 | `--output` can overwrite an untracked cited evidence test. | low | `readonly_inputs()` is called without the evidence tests (verifier :166). Passing `bundle["evidenceTests"]` is a direct correction. | patch |
| ECH-14 | The test's `verify()` helper hides stderr when the verifier crashes before writing its report. | low | `json.loads` on a missing `out/result.json` raises FileNotFoundError. Asserting the file exists first, with stderr in the message, is a direct correction. | patch |
| ECH-15 | The verifier is not independent: it imports the generator. | false | It recomputes from source bytes and Git and never trusts declared values. 11.1's `verify_thin_authoring_guidance.py` uses the same pattern. The independent roots are the C# const pins. | reject |
| ECH-16 | The C# test pins only the recorded digest string and skips sections. | medium | Same as BH-04. | patch (BH-04) |
| ECH-17 | The Spec Change Log and Review Triage Log sections were removed. | low | Same as BH-10. Headings restored. | reject |
| VG-01 | Six of the ten write-order invariants have no isolating fault, so removing one leaves AC-12.1-05 green. | medium | Pre-verified gap. Only 4 order faults exist, each tripping other invariants, and the name-level test is outside the `-k lifecycle_fault_matrix` receipt. | patch |
| VG-O1 | The new C# test runs nowhere in CI: `ci / conformance` fails at "Prepare package build imports" (run 43831430224, baseline `8da7f2bc`). | medium | Pre-existing package-mode CI breakage (epic-16 retro item 50), not caused by this diff. | defer |
| VG-O2 | The C# remark overstates what it pins. | medium | Same as BH-04. | patch (BH-04) |

Pass 1 resolution (2026-11-16): the implementation subagent applied all twelve patch entries. It made two invariant deviations. First, the rebuild order is pinned by three jointly exact invariants rather than one, so "once" and "proof first" can still fail alone. Second, "proof claim precedes every rich write" was dropped because the proof→fence→index→conversation→merge→batch chain implies it. VG-O1 was appended to `deferred-work.md`. Re-verification on the orchestrator side:
- `test_derived_key_decision.py`: 199/199 passed, 0 skipped. The AC-12.1-05 selector ran 198 cases, 11 of them isolated order-invariant cases.
- AC-12.1-02: byte-identical on regeneration.
- AC-12.1-03: 1/1 passed from a fresh Debug net11.0 build with 0 warnings.
- AC-12.1-04: FAIL `ADR_APPROVAL_MISSING`, as expected before approval.
- BH-09 verification follow-up: the 21 cited Server.Tests methods (E01–E18, E21–E23) ran 39/39 passed. E19, the live Dapr lane, was not run.
- New digests to present: ADR `2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068`, payload `13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe`.


- ADR 0005 ratifies `conversations-derived-keys-v2` as v1 plus the four as-built resolution families plus the configurable store name (default `statestore`). No key bytes change, so the migration treatment is identity. That satisfies DC-7's successor rule.
- The ADR scopes out Tenants keys and points to retro item 52 for duplicate-identity semantics, which stays open.
- Proposed policy:
  - Derived state is removed only by operator rebuild, meaning delete plus full replay from EventStore.
  - There is no backfill.
  - Proof outlives the ledger.
  - Rollback means redeploying prior binaries, then rebuilding. It never reverse-writes and never repeats side effects.

## Verification

- `dotnet build tests/Hexalith.Conversations.Conformance.Tests -c Release`, then run the AC-12.1-02..05 contract commands with system `python3`. Expected: exit 0.
- `python3 -m pytest -q _bmad/scripts/tests/test_derived_key_decision.py`, plus `python3 scripts/check-root-submodules.py --repository .` after staging.
