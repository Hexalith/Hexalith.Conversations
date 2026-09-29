# Story 7.1 terminal acceptance proposal — 2026-09-27

**Draft for owner review; no approval or authority publication.** Story 7.1's terminal `ACCEPTED` authority remains unestablished. The implementation hold remains `ACTIVE`; Story 7.2 remains `in-progress`. This proposal changes no lifecycle state, record, result artifact, gitlink, or authority pointer.

Investigation revision: `aefe4003cc94f49021e942adb9cbb8ca9ccf86cd`, on `main` with an initially clean working tree. It is the requested resume commit; no newer local commits were present. Continue from the existing tip, without reset or history rewriting. No push is authorized.

This proposal reuses the [Story 7.2 completion-resume audit](story-7-2-completion-resume-2026-09-27.md): its historical byte reproduction, 111-document inventory, V24 gitlink comparison, and 171-test workflow verification are not repeated. New checks below inspect the Story 7.1 acceptance inputs and evaluate the two existing diagnostics once at the resume revision. They do not regenerate either story's pair or rerun archived scenarios.

## Governing chain and the missing transition

The governing rule is [Architecture V15 AD-4](../planning-artifacts/architecture.md), lines 2887–2917: `ACTIVE -> EXECUTION_ALLOWED -> MERGE_CANDIDATE_VERIFIED -> ACCEPTED`. Only the first three states can return to `ACTIVE`. Terminal acceptance is immutable history; later drift can block Story 7.2 without reopening an accepted Story 7.1.

Read AD-4 with **V16's amendment and the final V22 marker**, not the older Epic 7 AR-15 wording in isolation. V16, lines 3206–3310, replaces the external ruleset/validator/nonce mechanism with repository-owner review, exact-SHA ordinary CI, authenticated approval of the parent/candidate tuple, owner fast-forward, and post-integration resolver `PASS`. V22 supersedes only the pending-recovery state. Neither amendment supersedes AD-4, AD-5, or AD-9. The final complete architecture marker is V22; the existence of V29 does not select V29 as terminal architecture authority.

| Committed link | What it establishes; what is still absent |
| --- | --- |
| V17 schema lift; V18 environment authority; [V19 checkpoint completion](../planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json), published at `3045060ce48acfed60b66bacd417de4dcfd1adb0` | Schema/checkpoint history. V19 retains `ACTIVE`, no full-story execution or completion. |
| [V20 release-owner authority](../planning-artifacts/v20-story-7.1-release-owner-authority-v1.json), `6fa990459dd72d6752e12bbc806d85bf607fff85`; [V21 correction](../planning-artifacts/v21-story-7.1-authority-correction-v1.json), `d297f96dc39b04008f391b404021899431585b6d` | Jerome Piquot's historical, Story-7.1-only lift and its corrected verification. Both explicitly withhold Story 7.2, completion without acceptance, release, and push. V21's recorded `LIFTED` is not current approval. |
| [V22 recovery](../planning-artifacts/v22-current-authority-recovery-v1.json), `cf82f8008d02b07d48338a545909d97faa302362` | Technical recovery candidate, still `ACTIVE`. Its marker requires separate owner review, exact-SHA CI, owner fast-forward, and post-merge `PASS`. V23 preserves the later two-parent `dcba5d4b1314eb67a95fa560b7cc0f88a9ab2607` result as `FAIL / CANDIDATE_GRAPH_DRIFT`; it does not cure it. |
| [V23 entry request](../planning-artifacts/v23-story-7.1-entry-candidate-v1.json), `5a7234b922371b5d0a12085a444d93783263f278` | `recordType=REQUEST`, `BLOCKED`, `ACTIVE`, all four authority flags false. Its five unresolved gates are operational envelope, FR-20/SM-C1, SM-C2, OQ-1, and owner approval. The separate `v23-story-7.1-entry-authority-v1.json` and V23 architecture marker were never published in the inspected history. |
| V24 correction `20e2cdd2b37e6387055b241c7ee45fc54e836742`; V25 preservation tooling `dbada954388430a6579b42508f51db806aebf3b8`; V26 committed-candidate correction `119c75172b501213307fab9346aa671a22bb18d2` | Technical/evidence corrections, all `ACTIVE`, with owner, execution, release, and push flags false. They do not approve entry or acceptance. |
| V27 lifecycle evidence `f2817ef168b20b16fb1e85f226fec9a9cb689ae7`; V28 gitlinks `e91e9da4796038a3499b8d0a79735649974a1603`; [V29 gitlinks](../planning-artifacts/v29-post-v28-root-gitlink-authority-v1.json) `f40cd883b52f8d0e52b8e6641d1cd88376ad5816` | Non-executable evidence routes, all `ACTIVE` with all four flags false. Their record `PASS` values grant no terminal authority; the audit already preserves V29's later historical failure. |

The [current-change policy](../../docs/runbooks/current-change-validation.md), introduced at `0db6207a4b1466371bedde7d09caa6c686f44ff1`, retires V23–V29 as routine change gates. It does not establish the explicitly required Story 7.1 acceptance. That commit also removes `.github/workflows/planning-authority-preflight.yml`. Ordinary CI's repository check is not an AD-4 terminal checker. Do not restore an old workflow or invent a V30 correction just to turn V24 drift green.

## Exact missing approval and prerequisite evidence

**Approval:** the missing decision is a separately authenticated, candidate-scoped **AD-4 Story 7.1 authority**, followed by the distinct terminal acceptance after integration checks. A request to investigate, the V20 lift, or a new `done` row cannot supply either decision. The extant entry publisher pins `Jerome Piquot <jpiquot@itaneo.com>`, SSH principal `jpiquot@itaneo.com`, fingerprint `SHA256:8XlNQvE3ucPf/e509wU4qtNgiyWA+TKmLei7F7+TCvk`; it requires identity, actual UTC decision time, rationale, `EXECUTION_ALLOWED`, and a signature on the separate publication commit. This identifies the committed trust contract, not an instruction to sign as that owner. Any replacement identity/authentication contract needs explicit owner adoption.

The owner must also explicitly retire or replace V21's temporary descendant restriction. Its implementation is `STORY_DESCENDANT_PATHS` and the per-commit path/gitlink checks in [the V21 publisher](../../_bmad/scripts/publish_story_7_1_successor_authorities.py), lines 259, 3018–3027 and 3144–3153. Replacing it requires an exact admissible integration-path/mode manifest, root-gitlink manifest, and drift policy, not an unrestricted descendant exception.

These **15 canonical paths are absent from the committed resume tree**. Paths beginning `v23-`, `oq-1-`, and `production-` in this table are under `_bmad-output/planning-artifacts/`:

| Missing input | Owner/evidence required under the existing contract |
| --- | --- |
| `v23-story-7.1-entry-authority-v1.json` | Separate signed owner publication with architecture pointer, after all entry gates. The current V23 publisher can produce only entry authority; it cannot publish `MERGE_CANDIDATE_VERIFIED` or `ACCEPTED`. |
| `production-operational-envelope-v1.md`; `docs/runbooks/story-7.1-production-recovery.md` | Architect/Runtime's AD-5 ownership, local/CI/staging/production parity and providers, state/pubsub/secrets/identity/health/telemetry responsibilities, recovery objectives/procedures, runbook binding, scaling limits, and waiver disposition. Only the Release owner accepts a current capacity/parity waiver. The historical V23 parser expects a closed JSON envelope even at the `.md` path and binds the ordered DETECT/CONTAIN/RESTORE/VERIFY/ESCALATE procedures. |
| `v23-story-7.1-preservation-gate-result-v1.json` | Current FR-20/SM-C1 passing disposition; canonical preservation denominator, seven category identities, source bytes, measured output and nonempty ledger recomputed, not copied from a historical pass. |
| `v23-story-7.1-performance-gate-result-v1.json` | Current SM-C2 passing disposition; exact HP-APPEND/CREATE/LIST/OPEN inventory, at least 30 baseline and candidate samples per path, recomputed P95 and no regression above 5% under the committed entry checker. |
| `v23-story-7.1-landing-zone-gate-result-v1.json`; `oq-1-fr-10-approved-successor-v1.json` through `oq-1-fr-15-approved-successor-v1.json` (six paths) | OQ-1 approving authority and all six exact FR-10–FR-15 decisions, source/decision hashes, zero unresolved decisions. Preserve the blocked predecessor. |
| `oq-1-eventstore-change-grant-effective-successor-v1.json`; `oq-1-commons-change-grant-effective-successor-v1.json`; `oq-1-conversations-consumer-grant-effective-successor-v1.json` | Effective grants from the respective owners, exact repository and requirement scopes, predecessor bindings, current accepted EventStore/Commons raw gitlink snapshots and Conversations source snapshot. An ineffective old grant cannot become approved by copying it. |

The exact gate shapes and recomputation are in [the entry publisher](../../_bmad/scripts/publish_story_7_1_entry_authority.py), constants at lines 53–64 and 157–198, `validate_operational_envelope`, `validate_pass_evidence`, and `render_authority`. It requires the fourteen evidence paths in a separate exact-scope transaction, immutable request bindings, unchanged gitlinks, then a signed two-path authority/architecture publication. Its five historical blockers are not a freshly measured product verdict. All fifteen absences above are fresh committed-tree observations.

Also required: authenticated AR-15 integration evidence or an owner-approved recovery amendment with a validated implementation addressing the recorded graph failure; a current AD-9 workflow inventory/resolver and Quality conformance result; and digest-valid, candidate-compatible Story 6.2, `7.1-SCHEMAS`, IR-0, V19, V20 input inventory, and Story 7.1 contract bindings. An owner disposition alone cannot convert a failed graph check to `PASS`. Historical rows do not establish current gate success. The frozen V23 transaction cannot simply be published on today's descendants: its scope, source, workflow, and gitlink bindings no longer match. This proposal does not repair or waive those historical checks.

## Concrete binding gap

The following are measured observations for reuse, **not an accepted-main selection**:

| Existing observation | Exact value |
| --- | --- |
| Story 7.1 candidate in the preserved record | `99479e205df64d711abf2117329b5dc2ad37b972` |
| Candidate Git tree object | `7ad4ed1a72dcab7e582190064a8c7266128e1763` |
| Latest publication of the preserved pair | `c69334cb13a981c9112ad687427b1f43fafc2988`, sole parent equal to that candidate |
| Publication Git tree object | `1685ccbfaff6774df171605dbf70eb904f453b6f` |
| Complete JSON file SHA-256 | `0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9` |
| JSON's self-excluding content digest | `85a48932459a72846577271834a9ee4608376121a9b609fbc9ffbb85c8c338af` |
| Markdown file SHA-256 | `044ac80a72a2f58587dbd9a53463d75567a75ac99797028fa4cc9a413c3dab43` |

`c69334c…` changes exactly the Story 7.1 `-3.md` spec, sprint status, and the JSON/Markdown pair; its ten raw gitlinks equal the recorded candidate's. This graph fact does not prove protected integration, prior owner approval, admissibility of those four paths, or a post-integration `PASS`. Neither that commit nor current `HEAD` can fill the missing accepted-main identity.

The [v2 final-record schema](../../_bmad/schemas/story-final-record-v2.schema.json) closes `candidate` to `commit` and `gitlinks`; the preserved pair has no verified merge-tree digest or merge-parent proof. Its six passing scenarios and intact five JUnit hashes are useful historical evidence, not that missing binding. A Git tree object ID is also not a SHA-256 integration-manifest digest. Adding either field to the historical JSON would violate preservation and its closed schema.

AD-4 therefore still needs all of the following, derived and cross-checked by protected-source tooling:

1. **Entry and integration proof:** owner-approved Story candidate; exact protected integration candidate, ordered parent identities and tree; exact admissible paths/modes; all raw `(path, 160000, object-id)` root gitlinks with paths equal to `.gitmodules`; protected checker source commit/path/digest; exact-candidate CI and its nonempty passing assertion ledger. Missing or drifted inputs retain `ACTIVE`.
2. **Post-integration proof:** actual protected-`main` commit and authenticated integration provenance; checker rerun against that committed result; proof that its in-scope tree and every root gitlink equal the verified integration result. Local ancestry alone supplies no host provenance.
3. **Final-record binding:** a candidate-matched generated final record binding the Story candidate and verified merge-tree digest, with its exact digest consumed by terminal authority. Preserve both existing pairs. The present Story 7.1 v2 pair cannot alone meet this additional requirement.
4. **Terminal publication:** one atomic authority/pointer transaction after both the post-integration result and final record pass, binding their exact bytes, the accepted-main commit, and the prior state transitions. `ACCEPTED` must never be inferred from a generic resolver `PASS` or reopened by later drift.

## Smallest proposed next change

**Proposed owner decision, not granted:** approve preparation of one narrowly scoped AD-4 acceptance contract for the existing Story 7.1 work, retaining all historical bytes and the current hold. Approve neither terminal acceptance nor execution by approving this proposal. Require the owner to identify the integration revision/path policy and supply the missing evidence before any state advance.

The minimal implementation to review next is an **additive acceptance contract and protected checker**, with its focused fault tests and exact invocation documented before any authority publication. It must provide these concrete records/fields; names and serialization become part of that reviewed contract, not improvised values in a live authority file:

| Contract component | Required binding/behavior |
| --- | --- |
| Entry decision | Authenticated owner identity, actual decision time and rationale, exact evidence-source commit/tree, checkpoint/input/gate digests, explicit descendant-policy replacement, Story-7.1-only scope. Preserve release/push false and the global hold. |
| Integration proof | Story commit; integration candidate commit/tree/ordered parents; exact path/mode manifest and digest; ordinal raw gitlinks; checker identity/digest; commands, exits, results and nonempty ledger. Canonical bytes and digest exclusions must be specified and tested. |
| Additive generated final-record contract | Preserve and hash-reference the existing v2 pair; bind the evaluated Story candidate and verified integration-tree digest in a separately versioned output at new paths. This requires owner approval of the new record contract; do not relabel an audit or handwritten supplement as a final record. Never overwrite or regenerate either historical pair. |
| Terminal authority and pointer | Prior entry/integration proofs, final-record path and full-file digest (separate from its content digest), actual accepted-main commit, in-scope tree/gitlink equality proof, terminal `ACCEPTED`, `statusAsOf`, recorded effect and separately evaluated current state. Publish the new authority and one complete append-only architecture pointer together, binding the preserved prefix and authority digest. Derive the publication's own identity externally to avoid a self-hash cycle. |

First land/review the non-authorizing schema/checker/route inventory under an owner-approved trust contract; AD-9 assigns tooling to Planning Tooling and the conformance gate to Quality. The decision cannot introduce and authorize its own validator. Next obtain valid prerequisite dispositions and a separate authenticated entry decision; then verify the integration candidate, integrate through the explicitly approved path, rerun the checker on committed main, generate the separately approved final binding, and only then publish terminal authority/pointer atomically. Do not combine these phases into an immediate `ACTIVE -> ACCEPTED` declaration or backdate missing approval.

The owner must explicitly reconcile the AD-4 protected integration/checker route with today's direct-push routine policy. No automatic ruleset installation, old V21 external-validator resurrection, or push is part of this proposal. If the owner instead supplies an already published terminal authority and all its supporting evidence, validate those exact committed objects before implementing anything new. A bare approval to “accept 7.1” cannot replace absent proof or tooling.

The checker must reject unsigned/mismatched approval, unavailable history, missing state transitions, wrong parents/tree/path sets/gitlinks, stale or mismatched result artifacts, missing post-integration proof, wrong final-record bytes, split/dangling pointers, substituted validator bytes, empty ledgers, malformed/schema-invalid output, and drift. Focused tests must prove each guard and that a terminal historical acceptance remains immutable when a later Story 7.2 candidate fails. Exit `0` plus schema-valid, scoped `PASS` is necessary; every other result keeps the relevant current execution hold `ACTIVE`. Record the exact checker command and schema in the contract; neither the existing entry publisher nor the resolver provides an AD-4 terminal command.

This is an AD-4 completion proposal, not a gitlink-drift successor. No trusted-host argument is available in this session. For the archived V27–V29 routes only an actual protected `push`/`pull_request_target` event's base is legitimate provenance; `HEAD`, `HEAD^`, a convenient historical host, or an invented event is not.

## Focused validation and preservation

Commands at the exact resume revision, with no trusted-host argument:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate aefe4003cc94f49021e942adb9cbb8ca9ccf86cd --check
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --candidate aefe4003cc94f49021e942adb9cbb8ca9ccf86cd --baseline c69334cb13a981c9112ad687427b1f43fafc2988
python3 scripts/check-root-submodules.py --repository .
```

Resolver: exit `2`, `BLOCKED`, `V24_ROOT_GITLINK_DRIFT`. Boundary: exit `2`, `BLOCKED`, `EVIDENCE_V24_ROOT_GITLINK_DRIFT`. Each has one evaluated `BLOCKED` row, detail/message `V23, V24, and evaluated gitlinks differ`; subjects are `current-authority-resolution` and `evidence-boundary`, respectively. Both return `effectiveHold=ACTIVE`, `implementationHold=ACTIVE`, and all four authority flags false. Their observed candidate fields are null because the guard exits before populating them; the command arguments above bind these diagnostics to the evaluated revision. Root-submodule check: exit `0`, `root submodules: PASS`. It does not clear the historical blockers.

Read-only input inspection:

```bash
uv run --frozen --no-sync python3 /tmp/story-7-1-acceptance-3x3zibco/inspect_inputs.py
```

Exit `0`: both pairs equal their committed blobs, validate against the v2 schema, and pass the existing `v2_verify_pair` digest check; all 23 bound result hashes match (5 for Story 7.1, 18 for Story 7.2). Raw Git checks confirm the Story 7.1 graph/four-path publication, unchanged candidate/publication gitlinks, the fifteen absent paths, and the V23–V29 control fields described above. These checks do not claim fresh scenario execution, protected provenance, or terminal acceptance.

The before/after preservation check passed for both pairs and all existing `artifacts/v9` files (45 files): SHA-256 and nanosecond modification times are unchanged. The documentation check passed for all twelve local links and confirmed the sole new path is this proposal, `HEAD`/tracked files/index/gitlinks are unchanged, and both Story 7.2 status locations remain `in-progress`. `git diff --check` exited `0`. The separate untracked-file command `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/story-7-1-terminal-acceptance-proposal-2026-09-27.md` exited `1` with no whitespace diagnostics (an added-file diff); a direct byte check confirmed LF endings, final newline, and no trailing whitespace, exit `0`. No product tests or archived result-producing commands were run for this documentation-only proposal.

A possible later documentation commit message was validated against installed, locked, and pinned `@commitlint/cli` **21.2.2**; no commit or staging is performed by this proposal:

```text
docs(evidence): propose story 7.1 terminal acceptance

Trace AD-4 authority and missing owner evidence without changing historical records, gitlinks, the active hold, or Story 7.2 status.
```

```text
node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/story-7-1-acceptance-3x3zibco/commit-message.txt --verbose
Exit 0: found 0 problems, 0 warnings.
```

**Remaining decision:** the repository/Release owner must adopt the prospective AD-4 integration and final-record/checker contract, identify the exact integration scope, and provide the missing authenticated entry/recovery decisions and prerequisite evidence above. Planning Tooling/Quality must then establish the protected proof route. Until that route proves integration and final binding and the separate terminal authority/pointer is published and verified, Story 7.1 acceptance is unestablished. Even after that prerequisite passes, the audit's independent Story 7.2 current-candidate/gitlink and boundary gates still need to pass; its historical pair and `in-progress` state remain preserved.
