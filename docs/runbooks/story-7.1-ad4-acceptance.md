# Story 7.1 AD-4 acceptance preparation

Stage 1 inspects committed inputs and preserves the implementation hold. It cannot
authenticate an owner, verify terminal acceptance, publish an authority, or unlock
Story 7.2. The user's **“I approve AD-4”** authorizes this bounded preparation, as
recorded in the [approval receipt](../../_bmad-output/implementation-artifacts/story-7-1-ad4-approval-2026-09-27.md).
It supplies no missing integration identity, gate result, signature, or host provenance.
The [approved proposal](../../_bmad-output/implementation-artifacts/story-7-1-terminal-acceptance-proposal-2026-09-27.md)
and [existing audit](../../_bmad-output/implementation-artifacts/story-7-2-completion-resume-2026-09-27.md)
remain the historical investigation; the inspector does not rerun archived scenarios.

## Implemented stage 1

Run the host-side inspector with the explicit full lowercase commit SHA to inspect:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/inspect_story_7_1_acceptance.py \
  --repository . \
  --candidate aefe4003cc94f49021e942adb9cbb8ca9ccf86cd \
  --check
```

The SHA above is the investigation revision, **not an accepted-main selection**.
`--repository` defaults to `.` and must name the repository root. `--candidate`
is required; symbolic names, abbreviated IDs, tags, and non-commit objects are
rejected. `--check` documents the read-only use and is optional; both forms have
identical behavior. There are no trust, approval, write-output, publication, or
submodule options. Unknown options, including `--help`, return the same JSON
failure envelope rather than mixing help text with machine output.

The command emits one JSON object on stdout, governed by the closed
[readiness result schema](../../_bmad/schemas/story-7.1-ad4-readiness-result-v1.schema.json):

| Outcome | Exit | Meaning |
| --- | --- | --- |
| `BLOCKED` | 2 | Evidence is absent, unavailable, or present but unverified; terminal tooling and provenance remain unsupported. |
| `FAIL` | 1 | Invalid CLI, candidate shape, JSON, file modes, gitlink inventory, or preserved record evidence was observed. |

There is no successful exit and no overall `PASS` state in stage 1. Every result
has `effectiveHold=ACTIVE`, `implementationHold=ACTIVE`, `ownerApprovalClaimed=false`,
`executionAllowed=false`, `releaseAuthorized=false`, `pushAuthorized=false`,
`terminalVerification=UNSUPPORTED`, `terminalPublication=UNSUPPORTED`,
`acceptance=UNESTABLISHED`, and `acceptedMainCommit=null`. A prerequisite containing
`PASS`, `ACCEPTED`, or an approval claim remains unverified. Presence never passes a gate.

Consumers must require a single schema-valid JSON result, matching process exit,
`result`, and diagnostic, and a nonempty assertion ledger. The diagnostic names
the first `FAIL` finding when one exists, otherwise the first `BLOCKED` finding;
it must occur in both the blockers and ledger with that state. Missing output,
malformed output, or any disagreement is unavailable evidence and retains the
hold. A wrapper must not turn exit 0 with malformed output into readiness.

The result binds the candidate commit, tree object ID, ordered parents, ordinal
raw root `(path, 160000, objectId)` gitlinks, `.gitmodules`, the fifteen prerequisite
paths, and both preserved pairs. Each inspected regular file reports its Git mode,
blob ID, and exact-byte SHA-256. `ABSENT` means no entry exists in the candidate
tree; `INVALID` means observed input failed a check; `UNAVAILABLE` means bytes
could not be read; `NOT_INSPECTED` means an earlier guard prevented inspection.
`PRESENT_UNVERIFIED` is an observation only. Early CLI/environment failures can
have no candidate or prerequisite inventory; the nonempty ledger states why.

The inspector loads helper code and schemas from its own host directory, reports
their exact-byte hashes as `HOST_WORKTREE_UNTRUSTED`, and never executes tools or
loads schemas from the candidate. These bindings identify the inspection
implementation; they do not adopt a protected trust source. It reuses the existing
entry helper's path, duplicate-key, gitlink, and canonical prerequisite definitions
through a private module instance with hardened Git execution. Each helper is
read once through directory descriptors that reject symlinked components and
nonregular leaves, then hash-bound before those exact source bytes are compiled
and executed. Cached bytecode is never loaded. Git runs with an
allowlisted environment, no object replacements or grafts, no lazy fetching,
disabled protocols, hooks and filesystem monitor, and no optional locks. Shallow
or partial clones and missing reachable objects block. Only root committed objects
are read: worktree decoys, index changes, and submodule contents supply no evidence.

The historic pairs are always listed, including when one is missing or invalid.
The host v2 schema checks their structure; `v2_verify_pair` checks existing
self-excluding and Markdown digests; the full-file hashes below additionally pin
the preserved bytes. No generator execution or record/result writes occur.

| Preserved path under `docs/release-evidence/` | Full-file SHA-256 |
| --- | --- |
| `story-7.1-final-record-v2.json` | `0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9` |
| `story-7.1-final-record-v2.md` | `044ac80a72a2f58587dbd9a53463d75567a75ac99797028fa4cc9a413c3dab43` |
| `story-7.2-final-record-v2.json` | `15212a33103bf36ccefb3778853d3e4a8875d1c9e82a0c3d3104dae54e361ef5` |
| `story-7.2-final-record-v2.md` | `0821f9b934506a87104279e67f51441ae3e9bbbad7a71cf3f3c1ab0786770e81` |

Pair verification proves preserved structure and bytes. It does not remeasure
their result artifacts, rerun tests, prove current candidate compatibility, or
establish integration provenance. Prior audit observations remain attributed to
that audit. A `VERIFIED` pair therefore never changes the overall blocked result.

## Exact prerequisite inventory

All fifteen paths were absent at the investigation revision. The planning paths
below are under `_bmad-output/planning-artifacts/`, except the explicit runbook path.
They are inventoried in ordinal path order in JSON.

| Required input | Evidence still required |
| --- | --- |
| `v23-story-7.1-entry-authority-v1.json` | Separate authenticated owner publication and architecture pointer after entry gates. Existing V23 can publish entry only. |
| `production-operational-envelope-v1.md` | AD-5 ownership, environment parity/providers, service responsibilities, recovery objectives, scaling/capacity and Release-owner waiver disposition. Historical contract requires a JSON object despite `.md`. |
| `docs/runbooks/story-7.1-production-recovery.md` | Bound DETECT/CONTAIN/RESTORE/VERIFY/ESCALATE procedures and recovery ownership. |
| `v23-story-7.1-preservation-gate-result-v1.json` | Fresh FR-20/SM-C1 evidence: denominator, seven category identities, source bytes, measured output, nonempty ledger. |
| `v23-story-7.1-performance-gate-result-v1.json` | Fresh SM-C2 evidence: HP-APPEND/CREATE/LIST/OPEN, at least 30 baseline/candidate samples each, recomputed P95, at most 5% regression. |
| `v23-story-7.1-landing-zone-gate-result-v1.json` | Effective OQ-1 owner authority, exact decision/source digests, zero unresolved decisions. |
| `oq-1-fr-10-approved-successor-v1.json` | Exact approved FR-10 successor. |
| `oq-1-fr-11-approved-successor-v1.json` | Exact approved FR-11 successor. |
| `oq-1-fr-12-approved-successor-v1.json` | Exact approved FR-12 successor. |
| `oq-1-fr-13-approved-successor-v1.json` | Exact approved FR-13 successor. |
| `oq-1-fr-14-approved-successor-v1.json` | Exact approved FR-14 successor. |
| `oq-1-fr-15-approved-successor-v1.json` | Exact approved FR-15 successor. |
| `oq-1-eventstore-change-grant-effective-successor-v1.json` | EventStore owner grant, repository/requirement scope, predecessor and current accepted raw gitlink binding. |
| `oq-1-commons-change-grant-effective-successor-v1.json` | Commons owner grant with the corresponding exact scope and gitlink binding. |
| `oq-1-conversations-consumer-grant-effective-successor-v1.json` | Conversations owner consumer grant, exact requirements and source snapshot. |

Stage 1 checks strict UTF-8 object JSON with no duplicate keys or non-JSON constants
for the fourteen JSON inputs, and UTF-8 for the recovery runbook. It does not
validate their future gate semantics, signatures, freshness, or authority chain.
The frozen V23 transaction's source, workflow, path and gitlink requirements do
not become valid on present descendants merely by creating these files.

## Required future stages — not implemented or adopted

The governing sequence remains `ACTIVE -> EXECUTION_ALLOWED ->
MERGE_CANDIDATE_VERIFIED -> ACCEPTED`. A failure can return the first three states
to `ACTIVE`. Terminal acceptance is immutable history; later Story 7.2 drift may
block Story 7.2 without reopening an accepted Story 7.1.

The following prospective record names and fields define the preparation target.
They are not live authority paths, generated outputs, adopted schemas, or a
terminal CLI. Closed schemas, implementations and negative tests must be reviewed
and adopted before any of these records can advance state. A future validator
must derive facts independently rather than trust record claims.

| Future record | Mandatory binding |
| --- | --- |
| `ad4-story-7.1-entry-decision-v1.json` in planning artifacts | `schemaVersion`, `storyId=7.1`, `state=EXECUTION_ALLOWED`, authenticated `ownerIdentity`, actual `decidedAtUtc`, `rationale`, `storyCandidate`, `evidenceSource` commit/tree, exact `checkpointBindings`, `inputBindings`, `gateBindings`, authenticated `recoveryProof`, `descendantPolicy`, `integrationPolicy` including owner-bound `integrationBaseline` commit/tree, `checkerBinding`, and independently verified `authentication` reference. Release/push remain false and the global hold remains ACTIVE. |
| `ad4-story-7.1-integration-proof-v1.json` in planning artifacts | Entry full-file digest; `storyCandidate`; approved `integrationBaseline` commit/tree; `integrationCandidate` commit/tree/ordered parents; `admissiblePathManifest` and digest; `integrationTreeManifest` and digest; all ordinal `gitlinks`; protected `checkerBinding`; exact-candidate `ciProvenance`; commands/exits/results and a nonempty all-PASS `assertionLedger`; state `MERGE_CANDIDATE_VERIFIED`. |
| `ad4-story-7.1-post-integration-proof-v1.json` in planning artifacts | Prior entry/integration full-file digests; actual `acceptedMainCandidate` commit/tree/ordered parents from authenticated protected-main provenance; exact integration action/approved tuple; protected checker rerun; independently derived in-scope tree manifest and root gitlinks equal to the verified integration proof; commands/exits/results and nonempty all-PASS ledger. This record alone does not grant terminal acceptance. |
| `story-7.1-ad4-final-binding-v1.json` and `.md` in `docs/release-evidence/` | A separately approved generated record contract: `storyCandidate`, `verifiedIntegrationTreeSha256`, post-integration proof digest, exact references to both preserved v2 pairs, measured scenario/result bindings, nonempty passing ledger, deterministic Markdown projection, content and full-file digest rules. Preserve existing pairs; a handwritten audit or supplement cannot serve as this generated final record. |
| `ad4-story-7.1-terminal-authority-v1.json` in planning artifacts | `state=ACCEPTED`, `statusAsOf`, exact prior transition records, entry/integration/post-integration digests, final-record path and full-file digest separately from content digest, actual `acceptedMainCommit`, in-scope tree/gitlink equality proof, authenticated decision, `recordedEffect`, and distinct `currentEvaluation`. Bind one atomic authority/pointer publication without embedding its own commit ID. |

Every future JSON record also requires `contentSha256`. A binding to a committed
file contains `sourceCommit`, `path`, `mode`, `objectId`, and `fileSha256`; a checker
binding contains its independently adopted source commit and all executable,
schema, dependency-lock and route-inventory path/digest bindings. Gitlinks always
come from raw root tree entries whose paths equal root `.gitmodules`; no nested
checkout supplies a substitute snapshot.

## Canonical bytes and integration manifests

For these prospective records, require strict UTF-8 without BOM, no literal CR
bytes, exactly one final LF, unique JSON keys, and compact separators `,` and `:`
with no other whitespace. Strings and keys must contain only Unicode scalar
values (U+0000–U+D7FF and U+E000–U+10FFFF) and be NFC. Reject lone surrogates and
non-NFC text rather than silently changing signed input. "Ordinal" ordering here
means lexicographic comparison of numerical Unicode scalar values, with a shorter
prefix first, independent of locale, case folding, UTF-16 code units or JSON escape
spellings. Apply that ordering to object keys and path inventories. Arrays retain
their specified order: parents in commit order, paths/gitlinks in scalar path order,
and ledger assertions in recorded execution order. Never sort parents.

String serialization must escape quotation mark as `\"` and backslash as `\\`.
Use exactly `\b`, `\t`, `\n`, `\f`, and `\r` for U+0008, U+0009, U+000A, U+000C,
and U+000D. Encode other U+0000–U+001F controls as `\u00xx` with lowercase hexadecimal
digits and exactly four digits after `u`. Emit every other scalar literally in
UTF-8, including solidus `/`, non-ASCII characters, U+2028 and U+2029; do not use
`\/`, unnecessary `\u` escapes or escaped surrogate pairs in canonical bytes.

Numbers are arbitrary-precision finite integers with canonical lexical form
`0|-?[1-9][0-9]*`. Zero has exactly the spelling `0`: `-0`, leading zeros, plus
signs, decimal points and exponents are not canonical. Booleans and null use the
exact lowercase tokens `true`, `false`, and `null`. Measurements that require
decimals remain in their separately validated bound evidence format. Validation
must reject noncanonical stored bytes, not merely parse them to equivalent values.

`contentSha256` is SHA-256 of this canonical serialization after removing only
the top-level `contentSha256` field. No other field is excluded. `fileSha256` is
SHA-256 of the complete exact stored file bytes including its final LF and
content digest; consumers store it in the *next* record. The final-binding JSON
hashes its Markdown's complete bytes; the Markdown renders the unsigned payload
and contains neither the JSON's digest nor its own digest, avoiding a cycle.
Existing v2 records keep their original self-excluding algorithm unchanged.

These small canonical-value examples specify exact bytes for future conformance;
they are not complete records or a terminal serializer implementation/adoption.
Each hexadecimal sequence includes the required final `0a` LF. SHA-256 is over
exactly the bytes shown, with no digest field or other exclusions in these examples.

| Example | Exact UTF-8 bytes (hexadecimal) | SHA-256 |
| --- | --- | --- |
| `{"a":0}` plus LF | `7b2261223a307d0a` | `c881142937432d073f20dbadce70f77cf5cb62dfaa9eb13c4f5ec2259158f69b` |
| `{"é":"雪"}` plus LF | `7b22c3a9223a22e99baa227d0a` | `f706355d2808259871d9a973a367759e7f7370eecd4e54ba60c3b5feef302ce1` |
| Key `s`; value NUL, backspace, tab, LF, form feed, CR, quotation mark, backslash, solidus | `7b2273223a225c75303030305c625c745c6e5c665c725c225c5c2f227d0a` | `4b2a87abcaf21a285cccc88a3939554b3f686ca7738c9a98204ec1f85e1a6c1e` |
| Key U+E000 with value 1 precedes key U+10000 with value 2 | `7b22ee8080223a312c22f0908080223a327d0a` | `63772babd8d75bb8623d9e867eb9c5212003e5dc0e844e048af591e1073afe5e` |

The approved `admissiblePathManifest` explicitly lists each admissible changed
path with expected before/after states, including additions/deletions, modes and
object IDs; missing states are null, and renames are deletion plus addition.
The owner must bind `integrationBaseline.commit` and `integrationBaseline.tree`
in the entry's integration policy; the proof repeats that exact binding, and the
checker independently verifies the commit's tree. The baseline is the explicitly
approved protected-main tip before integration. It is never inferred from a merge
base, HEAD, a convenient parent, or a prior record.

For a merge candidate, the policy binds the complete ordered parent tuple and
requires its first parent to equal that baseline commit; every other parent must
equal its separately approved identity. Compare the baseline tree (before) with
the integration candidate's committed tree (after), not a combined merge diff or
the diff against an arbitrary other parent. For fast-forward integration, the
policy instead binds the baseline as the exact pre-integration protected-main tip,
proves it is an ancestor of the exact approved candidate, and binds that candidate's
complete actual ordered parents independently; the baseline need not be its
immediate parent. Compare the same baseline-tree-to-candidate-tree endpoints even
when the fast-forward spans several commits. No real baseline, parents or candidate
are selected by this preparatory contract.

For both routes, enumerate the complete before/after root-tree entries without
rename detection and require the changed path/mode/object set to equal the exact
approved admissible manifest. Require every out-of-scope entry equal at those
endpoints and verify any additional per-commit restrictions in the approved
descendant policy. A different protected-main tip makes the approval stale.
Its `integrationTreeManifest` enumerates every file in the approved
in-scope tree with path, mode, blob ID and exact-byte SHA-256; gitlink rows carry
their mode/object ID and null blob digest. Hash each manifest using the same
canonical JSON rule with no exclusions. The verified integration-tree digest is
this manifest's SHA-256, not the Git tree object ID. The policy must require all
out-of-scope entries unchanged and explicitly state allowable gitlink changes;
no unrestricted descendant exception is admissible.

The post-integration checker independently derives the same complete in-scope
manifest and all root gitlinks from the actual committed main result. Both must
equal the integration proof. Local ancestry, a two-parent merge, HEAD, a done row,
historical PASS, or a convenient tree match cannot supply authenticated host
provenance or the accepted-main identity.

## Trust adoption and atomic publication

Before entry, the repository/Release owner must adopt the integration policy,
exact candidate/path/mode/gitlink scope and drift rules; explicitly retire or
replace V21's temporary descendant restriction; authenticate the entry decision;
and supply valid AR-15 evidence or adopt a recovery amendment whose implementation
actually passes the previously failed graph check. A decision alone cannot turn
that failure into PASS. Current compatible Story 6.2, `7.1-SCHEMAS`, IR-0, V19,
V20 input inventory and Story 7.1 contract bindings remain mandatory.

Planning Tooling must establish the checker and AD-9 workflow/route inventory;
Quality must independently establish its conformance result. The owner must
adopt the checker source and its provenance mechanism through a separate,
previously trusted decision. A candidate cannot introduce and authorize its own
validator. The existing V23 publisher pins Jerome Piquot's SSH identity as
documented in the proposal; adopting any replacement authentication contract is
an explicit owner decision, never permission for an agent to sign as that owner.
Decision times and host approval IDs must be real observed values, never backdated
or synthesized. Protected source substitution or missing authentication blocks.

The owner must reconcile this prospective protected acceptance route with the
[current direct-push routine policy](current-change-validation.md). That policy
and ordinary CI currently provide no terminal AD-4 route. This preparation adds
no workflow, ruleset, trusted-host argument, publication command, or push authority.
No old V21 external-validator route is restored. Archived V27–V29 event-base
rules cannot be satisfied by substituting HEAD or a chosen historical revision.

Only after entry, integration, post-integration checking and the new generated
final binding have independently passed may terminal publication occur. The
future publisher must atomically add the terminal authority and append exactly
one complete architecture pointer, changing exactly those two paths in the
publication transaction. The pointer binds the authority's path/full-file digest,
the exact preserved architecture prefix byte length and SHA-256, and prior state
bindings. A protected validator must prove the complete marker and authority
coexist in that commit, no prefix bytes changed, no competing/dangling pointer
exists, and all prior proof digests match. Determine publication commit identity
externally after the transaction; neither new file can include its own commit
hash. No pointer is appended by stage 1.

## Required negative acceptance cases

Future terminal conformance must reject unsigned/mismatched/stale owner decisions;
missing history or transitions; wrong parent order/tree/path sets/modes/gitlinks;
stale, substituted or candidate-mismatched test artifacts; absent protected-host
or post-integration proof; substituted checker/schema/dependency bytes; invalid
or changed final-record bytes/digests; empty ledgers; malformed/duplicate/schema-invalid
JSON; split, dangling or competing pointers; changed preserved prefixes; and
unapproved drift. It must also demonstrate immutable historical ACCEPTED when a
later Story 7.2 candidate fails. Only that future scoped, schema-valid PASS with
exit 0 can satisfy its gate. Stage 1 cannot produce such an outcome.

The implemented focused checks are:

```bash
uv run --frozen --no-sync python3 -m pytest -q tests/tooling/test_ad4_acceptance_readiness.py
python3 scripts/check-root-submodules.py --repository .
git diff --check
```

The readiness tests use temporary Git repositories outside the archived evidence
paths. They exercise real CLI results, absences and fake completeness, invalid
JSON/modes, pair drift, unsafe paths, missing/shallow/partial history, malicious
candidate tools, dirty decoys, parent order, schema controls, and unchanged hashes
and nanosecond modification times of the four mandatory historical pair files
plus every locally available file under `artifacts/v9`. Ignored result files need
not exist in a clean checkout, so the portable test assumes no workstation-specific
artifact count. The separate original 45-file session snapshot remains the
preservation check for this workspace, recorded in the spec. The tests do not
claim to implement or validate the future terminal authentication contract.
