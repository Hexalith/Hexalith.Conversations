# Reality And Repository Review — Architecture V16 Pragmatic AR-15 Recovery

## Final Re-Review Of The Corrected V16 Artifacts

- **Re-review date:** 2026-09-19
- **Corrected V16 block:** 7,022 marker-delimited bytes excluding the terminal
  LF; SHA-256
  `7d438ccb69391805973ff827f02457a4441b1e540f1fff0249e4eee0bb26b2ea`
- **Corrected Epic 7 block:** 2,997 marker-delimited bytes excluding the
  terminal LF; SHA-256
  `a05b382ba9d7bcec09d8b2e67e410c75979f41f544920396b84608550dc4179b`
- **Corrected Epic 7 context SHA-256:**
  `d3a75979662d5f1c0638fa09c4e94200d62c7822618af478b52a15a6633864ca`
- **Mode:** reviewer gate; no source artifact was edited

### Final Verdict

**PASS — the corrected Architecture V16, Epic 7 correction, and Epic 7 context
now match repository reality and form a coherent pragmatic AR-15 route. No
remaining reality mismatch was found in the corrected scope.**

The correction does not pretend that V22 already exists or that the currently
checked-in V21 workflow has already changed. It classifies the existing
ruleset-dependent workflow/publisher/tests as frozen historical verification,
requires the future V22 workflow and route inventory to stop invoking them,
and retains `PENDING_V22_RECOVERY` as a publication-dated fact. The six new V22
record/schema/resolver/test paths remain absent, so the implementation work is
still future work rather than an architecture-review failure.

### Requested Reconciliation Checks

| Check | Result | Repository-grounded conclusion |
| --- | --- | --- |
| Old ruleset tooling is historical | **PASS** | Architecture V16, the Epic correction, and context all identify the V21 `ci-trust` publisher/tests as frozen historical verification. V16 truthfully leaves those historical files unchanged and requires V22 to remove their invocation from the preflight workflow and current route inventory. The current focused historical lane still passes `9 passed, 185 deselected`, confirming that preserved history was not silently broken. |
| Ownership and approval sequence | **PASS** | V16 explicitly replaces V15's joint ownership, old blocked status, and deferred action. The repository owner is the sole AR-15 approval role. The resolver reports technical facts only; candidate-authored bytes cannot assert approval. After technical local/CI `PASS`, the owner records authenticated repository-host approval of the emitted parent/candidate tuple. |
| One-commit fast-forward integration | **PASS** | The candidate has exactly one parent equal to selected current `main`; approval stales if either SHA or `main` changes; integration is fast-forward only to the exact approved candidate. Merge commits, squash, and rebase are explicitly excluded. Post-merge `main` therefore has the approved candidate identity. |
| Publication-dated current state | **PASS** | Architecture, Epic, and context qualify absence and `PENDING_V22_RECOVERY` as facts “At V16 publication” and direct all live-state consumers to the selected resolver. Because Epic/context are outside the V22 allowlist, their prose remains true after V22 rather than becoming stale. |
| Result-schema ownership | **PASS** | Architecture names `_bmad/schemas/v22-current-authority-recovery-v1.schema.json` as the closed owner of `hexalith.conversations.current-planning-authority-result.v1` and requires direct validation of every `PASS`, `FAIL`, and `BLOCKED` envelope with a nonempty ledger. |
| Committed-marker/post-merge semantics | **PASS** | V22 becomes the selected marker immediately under the last-complete-marker rule, but failed or blocked post-merge resolution keeps recovery incomplete, preserves `ACTIVE`, and prohibits the later AD-4 request. A transient CI result does not make V21 current again. |
| Root-gitlink boundary | **PASS** | Parent, candidate, and committed `main` must have identical ordinal root-tree `(path, mode=160000, object-id)` tuples whose paths exactly equal `.gitmodules`, without traversing submodules. |
| Hold and lifecycle state | **PASS** | `implementationHold` remains `ACTIVE`; Story 7.1 remains unauthorized; the exact Epic 7 and Story 7.1-7.4 lifecycle rows remain singular and `backlog`; no product, dependency, submodule, release, push, or story-status authority is introduced. |

### Final Assertion Ledger

| ID | State | Assertion |
| --- | --- | --- |
| R16-R1 | PASS | The immutable V15 block still matches its declared 35,772-byte length and SHA-256 `85c7418ca55b1c67be90eed78e280c792ce92360e0cf2817237e41a3bcdfccbe`. |
| R16-R2 | PASS | The selected V21 sidecar still matches SHA-256 `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`. |
| R16-R3 | PASS | Exactly one complete V16 marker pair and one complete Epic 7 correction marker pair exist. |
| R16-R4 | PASS | The workflow and architecture paths exist as regular files; both V22 records, both schemas, the resolver, and its direct test remain absent. |
| R16-R5 | PASS | The source-pinned V21 diagnostic exits `1` with `result=FAIL`, `effectiveHold=ACTIVE`, one ledger row, and blocker `V21_DESCENDANT_GITLINK_DRIFT`. |
| R16-R6 | PASS | The historical ruleset-focused direct lane remains green and is explicitly excluded from current V22 recovery authority. |
| R16-R7 | PASS | The owner approval occurs after technical validation, is external to candidate-authored bytes, and is stale on parent/candidate/main drift. |
| R16-R8 | PASS | Exact fast-forward integration makes approved candidate and committed `main` identical. |
| R16-R9 | PASS | Present-state prose is publication-dated and live state is resolver-derived. |
| R16-R10 | PASS | The result schema has one named repository owner path and nonvacuous tri-state requirements. |
| R16-R11 | PASS | `uv run --frozen --no-sync python3` is supported by the current pinned repository environment. |
| R16-R12 | PASS | `git diff --check` passes for the corrected architecture, Epic, and context artifacts. |
| R16-R13 | PASS | No new ruleset, external validator, nonce, release/push authority, or Story 7.1 implementation authority was reintroduced. |

### Final Gate Statement

The corrected V16 review gate passes. The next work remains the separately
implemented eight-path V22 transaction and its local/ordinary-CI validation;
this review does not claim those absent artifacts have passed. After AR-15
actually passes, the next prompt is the separate owner-approved AD-4
`EXECUTION_ALLOWED` authority request—not Story 7.1 implementation.

## Initial Review — Superseded By The Final Re-Review Above

- **Review date:** 2026-09-19
- **Target:** Architecture V16, the matching Epic 7 correction, and the current
  Epic 7 context
- **Repository:** `Hexalith/Hexalith.Conversations` at committed `HEAD`
  `dce1231e66856bf5fcb26b0c4dc258b9c5da58ba`, with the reviewed changes still
  in the working tree
- **Purpose:** help Architecture, Release, Quality, and the repository owner
  decide whether V16 truthfully describes current repository state and gives a
  coherent, pragmatic route for the later V22 recovery
- **Mode:** review only; no source, workflow, product, submodule, sprint, or
  authority artifact was changed

### Initial Verdict

**FAIL — the direction is correct, but the current repository does not yet
match two central V16 claims and the approval/merge sequence is not executable
as written.**

V16 correctly removes GitHub rulesets, an external validator, no-bypass proof,
external check identity, and the nonce from the intended AR-15 route. It also
correctly preserves `implementationHold=ACTIVE`, leaves all five Epic 7
lifecycle rows at `backlog`, keeps Story 7.1 unauthorized, and requires a later
AD-4 `EXECUTION_ALLOWED` request rather than implementation.

The blocking mismatch is local and concrete: the current preflight workflow
still calls the GitHub rulesets API and exits `2` when active ruleset evidence
is unavailable, while the current publisher and nine passing direct tests still
treat that behavior as the active CI trust contract. V22 may replace the
workflow because it is in the eight-path allowlist, but the publisher and its
tests are outside that allowlist and are not presently labeled historical.
That conflicts with V16's claim that rulesets are “neither required nor used”
and with inherited AD-9's rule that active ruleset references must fail the
future route inventory unless explicitly historical.

The second blocker is the approval sequence. The fixed resolver command has no
approval input, yet success is said to require an approved parent/candidate and
to detect stale approval. Approval occurs only after the resolver passes, and
“ordinary/normal merge” does not say whether the approved candidate SHA must be
the committed `main` SHA. This can be fixed without a new service: keep owner
approval as a manual merge precondition after technical `PASS`, require the
owner to approve the emitted parent/candidate tuple, and select one integration
identity rule.

## Repository Facts Confirmed

| Assertion | Result | Independently checked evidence |
| --- | --- | --- |
| V15 predecessor pin | **PASS** | The marker-delimited V15 block is 35,772 bytes excluding its terminal LF and hashes to `85c7418ca55b1c67be90eed78e280c792ce92360e0cf2817237e41a3bcdfccbe`, exactly matching V16. |
| V21 sidecar pin | **PASS** | `_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json` hashes to `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`. |
| V22 path reality | **PASS** | The workflow and `architecture.md` exist as regular files. Both V22 records, both V22 schemas, the generic resolver, and its direct test are absent. `PENDING_V22_RECOVERY` is accurate at V16 publication. |
| Current diagnostic | **PASS** | The source-pinned V21 verifier blob matches its declared SHA-256 and, against both `2c6a4af775eaa969deb4bbbfc294be1c472afd7d` and current committed `HEAD`, exits `1` with `result=FAIL`, `effectiveHold=ACTIVE`, a nonempty ledger, and blocker `V21_DESCENDANT_GITLINK_DRIFT`. |
| Production operational envelope | **PASS** | `_bmad-output/planning-artifacts/production-operational-envelope-v1.md` is absent. |
| PRD/evidence state | **PASS** | Current tracked artifacts consistently retain FR-20/SM-C1 `PENDING`, SM-C2 `FAILED`, OQ-1 `BLOCKED`, and the implementation hold `ACTIVE`. |
| Sprint status | **PASS** | `sprint-status.yaml` contains exactly one `epic-7` row and one row for each Story 7.1-7.4; all five values are `backlog`. The reviewed diff does not modify that file. |
| Proposed `uv` command form | **PASS, conditional** | `uv run --frozen --no-sync python3` works in the pinned repository environment, and the same option pattern is already used by the current workflow. The resolver itself is intentionally absent, so its CLI and output cannot yet be executed. |
| History availability | **PASS** | The repository is not shallow, so the reviewed history checks are available. |
| Reviewed document hygiene | **PASS** | `git diff --check` passes for `architecture.md`, `epics.md`, and `epic-7-context.md`. |

## Findings

### REAL16-01 — The checked-in workflow still actively requires a GitHub ruleset

`planning-authority-preflight.yml:3-5` describes an active organization
ruleset as enforcement authority. Its first job reads `RULESET_TOKEN`, calls
`/repos/Hexalith/Hexalith.Conversations/rulesets`, and invokes the `ci-trust`
route. Running that route with no usable ruleset evidence returns
`V21_CI_RULESET_ACTIVATION_UNVERIFIED`, `result=BLOCKED`, a nonempty assertion
ledger, and exit `2`.

V22 can and should replace this workflow step. Until that happens, V16 should
say that rulesets are not part of the **new AR-15 route**, while the currently
committed V21 workflow is obsolete historical tooling that remains blocking.
Saying they are not “used for this repository” is false for the checked-in
workflow today.

### REAL16-02 — The ruleset contract survives in active code and passing tests outside the V22 allowlist

`publish_story_7_1_successor_authorities.py` still defines
`CI_RULESET_BLOCKER`, includes
`protected-default-branch-ruleset-source-workflow-contract` in the V21
assertion subjects, and implements `ci_trust_boundary_result` as an active
organization-ruleset check. Its direct tests still require this behavior; the
focused lane completed with `9 passed, 185 deselected`.

Neither that publisher nor its test is in V16's exact eight-path V22
transaction. The future route inventory cannot simultaneously obey AD-9's
“active references fail unless explicitly historical” rule and treat this code
as current. The minimal resolution is to state that the V21 `ci-trust` route and
its tests are frozen historical verification only, and require the V22
workflow/inventory to stop invoking or classifying them as current recovery
gates. If they must be edited instead, the eight-path allowlist is incomplete.

### REAL16-03 — V15's old joint ownership remains binding under V16's own inheritance rule

V16 says only the old *mechanism* is superseded and every other V15 decision
remains binding. V15 separately assigns AR-15 jointly to Release,
Architecture, Quality, and the organization-ruleset administrator and repeats
that ownership in its deferred-work table. V16 then assigns approval to one
repository owner.

Ownership is not merely a ruleset mechanism. Without an explicit replacement,
the unwanted ruleset-administrator dependency survives as a valid reading.
The narrow fix is to say that V16 replaces V15's AR-15 ownership, old
`BLOCKED_BOOTSTRAP_AUTHORITY` status, and deferred action as well as its
ruleset/validator mechanism.

### REAL16-04 — Approval cannot be validated by the command in the stated order

The resolver command accepts only `--repository`, `--candidate`, and `--check`.
No approval record, identity, parent, or approval reference is an input.
Nevertheless, resolver success is said to require an “approved” parent and
candidate, and stale approval is classified as `BLOCKED`; the owner approval
happens only after local and CI `PASS`.

Keep this pragmatic: the resolver should report technical facts and the exact
parent/candidate tuple without claiming approval. The repository owner then
approves that tuple as a manual merge precondition. If current `main` or the
candidate changes, the owner approval is stale and the technical checks and
approval repeat. No external validator, nonce, ruleset, or new approval service
is needed.

### REAL16-05 — “Normal merge” does not preserve the exact approved candidate identity

A fast-forward preserves the candidate SHA. A merge commit, squash, or rebase
does not. V16 and the Epic correction require exact approved commit/tree
identities but do not define which relation the post-merge `main` result must
have to the approved candidate.

The smallest deterministic rule is one V22 commit whose sole parent is the
approved `main` commit, followed by an identity-preserving fast-forward so
committed `main` equals the approved candidate SHA. If another merge method is
intended, its exact parent and tree relation must be stated; “normal merge” is
not enough.

### REAL16-06 — The prior Epic 7 numbered gate remains literally current-looking

The earlier Epic 7 correction still requires a no-bypass organization-ruleset
validator, nonce consumption, and `BLOCKED_BOOTSTRAP_AUTHORITY`. The appended
correction says those requirements are superseded, but it does not restate the
complete replacement for that numbered gate or declare those exact earlier
clauses historical/non-evaluable.

Human last-append readers can resolve the conflict, but a literal gate parser
or context compiler can still enforce the obsolete condition. The new append
should name the prior gate/status clauses it replaces, without rewriting their
historical bytes.

### REAL16-07 — Epic and context “current state” wording becomes stale immediately after V22

The exact V22 allowlist excludes `epics.md` and `epic-7-context.md`, while both
say the V22 artifacts are absent and AR-15 is currently pending. Those claims
are true now but will become false when the allowed V22 transaction lands.

Qualify them as “at V16 publication” or “at planning status commit
`2c6a4af...`,” and direct readers to the resolver for live state. This avoids
expanding V22 merely to refresh prose.

### REAL16-08 — The named result schema has no pinned schema path

V16 requires output schema
`hexalith.conversations.current-planning-authority-result.v1`, but the allowlist
names only recovery-record and route-inventory schema files. Because all six
new artifacts are absent, the review cannot determine which file owns the
result contract or whether it is closed against contradictory authority
fields.

Name the schema path that defines the resolver result—preferably reuse the
listed recovery schema if that is its purpose—and require the direct test to
validate every `PASS`, `FAIL`, and `BLOCKED` envelope with a nonempty ledger.

### REAL16-09 — No current mechanical test recognizes V16 or the new route

Searches across `_bmad/scripts`, `.github/workflows`, and `tests` find no
reference to the V16 identity, the new Epic correction marker,
`PENDING_V22_RECOVERY`, or the proposed result schema. Existing direct tests
instead keep the old ruleset route green.

This is consistent with V22 being absent, but it means V16 is prose-only today.
The future V22 direct test must prove predecessor block/digest binding, exact
path/mode equality, ruleset-route retirement, result-schema validation,
anti-vacuity, root-gitlink equality, and post-merge fail-closed behavior.

### REAL16-10 — The current worktree cannot be published indiscriminately as the V16 decision

The worktree contains a root gitlink move for
`references/Hexalith.FrontComposer` from `84c68776...` to `16d820e7...`.
That is unrelated pre-existing work and must be preserved, but V16 expressly
authorizes no submodule or gitlink change. The V16/epic/context publication must
exclude that gitlink and the unrelated `.memlog.md` edit; otherwise its own
publication contradicts its boundary and leaves no clean parent for V22.

## Assertion Ledger

| ID | State | Assertion |
| --- | --- | --- |
| R16-C1 | PASS | V15 block bytes/digest match the V16 predecessor pin. |
| R16-C2 | PASS | V21 sidecar bytes match the selected digest. |
| R16-C3 | PASS | Six required V22 record/schema/resolver/test paths are absent; the two pre-existing paths are regular files. |
| R16-C4 | PASS | The source-pinned current diagnostic is nonvacuous and resolves `FAIL` / `ACTIVE` with `V21_DESCENDANT_GITLINK_DRIFT`. |
| R16-C5 | PASS | Epic 7 and Stories 7.1-7.4 each have exactly one lifecycle row and remain `backlog`. |
| R16-C6 | PASS | The proposed `uv run --frozen --no-sync python3` invocation pattern is supported by current repository tooling. |
| R16-C7 | FAIL | The committed workflow still invokes the organization-ruleset trust route. |
| R16-C8 | FAIL | Current publisher code and passing tests still classify the ruleset route as active, and V22 cannot edit those paths. |
| R16-C9 | FAIL | V15's joint AR-15 ownership remains inheritable despite V16's single-owner wording. |
| R16-C10 | FAIL | The shown resolver command cannot observe the post-validation owner approval it is required to validate. |
| R16-C11 | FAIL | The approved-candidate/post-merge identity relation is undefined across normal merge strategies. |
| R16-C12 | FAIL | Current-state wording in the Epic/context is not durable across the exact V22 allowlist. |
| R16-C13 | PASS | `implementationHold` remains `ACTIVE`; no Story 7.1, sprint-status, product, release, or push authorization is introduced. |
| R16-C14 | BLOCKED | The absent V22 resolver, schemas, records, and direct test cannot yet be executed or schema-validated. |

## Minimal Acceptance Condition

Do not restore GitHub rulesets or add another external trust system. Correct
V16 narrowly so that it:

1. explicitly replaces V15's AR-15 owners, status, and deferred action;
2. labels the old V21 ruleset workflow/publisher route as historical and makes
   the V22 workflow stop invoking it;
3. separates technical resolver `PASS` from the owner's later manual approval
   of the emitted parent/candidate tuple;
4. selects one post-merge identity rule, preferably a one-commit
   fast-forward;
5. qualifies present-tense pending/absence statements as V16-publication
   facts; and
6. names the repository schema file for the resolver result.

After those corrections and an actual V22 implementation pass, the next prompt
should request the separate owner-approved AD-4 `EXECUTION_ALLOWED` authority.
It should not request Story 7.1 implementation.
