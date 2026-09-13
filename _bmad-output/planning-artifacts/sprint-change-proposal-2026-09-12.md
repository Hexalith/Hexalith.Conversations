---
workflow: bmad-correct-course
status: approved
date: 2026-09-12
project: Conversations
mode: Batch
changeScope: Moderate
recommendedApproach: Direct Adjustment through additive V19 and V20 successor authorities
implementationHold: ACTIVE
approval: approved
approvedBy: Jerome
approvedOn: 2026-09-12
triggerStory: 7-1-define-the-final-record-schema-and-deterministic-generator-core
triggerArtifact: _bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md
affectedAuthority: V11 checkpoint scope, V17 scoped lift, and V18 active full-story hold
---

# Sprint Change Proposal — Story 7.1 Checkpoint Repair and Full-Story Entry Authority

## 1. Issue Summary

### Trigger

Story 7.1 is paused because the repository has two different unresolved
authority facts:

1. the historical `7.1-SCHEMAS` transaction at commit
   `b819a7c43a7024295abaabd418a74f5f64cb5af0` does not conform to V11's exact
   changed-path boundary; and
2. V17 and `implementation-hold-v1.json` lifted the hold only for
   `7.1-SCHEMAS`, while V18 explicitly left the full Story 7.1 hold `ACTIVE`
   and `story71CandidateBindingResolved: false`.

The approved Story 7.1 semantic specification correctly treats both facts as
blocking. It cannot safely infer checkpoint completion from the existing mixed
commit, and it cannot treat a slice-scoped release-owner decision as authority
to implement the full story.

### Exact historical transaction finding

V11 permits this exact ordered five-path checkpoint boundary, inventory
`V19-7.1-SCHEMAS-CHANGED-PATHS-v1`, whose NFC UTF-8 LF path-list SHA-256 is
`5137dfa15a6c28253898b7edac5969684abddc2c77b35c2f3299b07faa72cf9d`:

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/scripts/tests/test_generate_story_record.py
artifacts/v9/schema-slice/v2-schema-contract.xml
```

The exact raw changed-path list of `b819a7c`, in Git output order, has 15
entries and path-list SHA-256
`be68d84d11534579092215b1f8a758cefb4edfd56644b046cb6687468e5a8212`:

```text
_bmad-output/implementation-artifacts/deferred-work.md
_bmad-output/implementation-artifacts/epic-7-context.md
_bmad-output/implementation-artifacts/spec-7-1-schemas.md
_bmad/schemas/story-final-record-v2.schema.json
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/scripts/tests/test_generate_story_record.py
references/Hexalith.Builds
references/Hexalith.EventStore
references/Hexalith.Folders
references/Hexalith.FrontComposer
references/Hexalith.Memories
references/Hexalith.Parties
references/Hexalith.Projects
references/Hexalith.Tenants
```

Exact-set comparison therefore yields:

- common paths: `4`;
- missing required paths: `1` —
  `artifacts/v9/schema-slice/v2-schema-contract.xml`;
- unexpected paths: `11` — the three implementation-artifact paths and eight
  root gitlinks shown above; and
- verdict: `NONCONFORMING`, with blocker
  `CHANGED_PATH_SET_MISMATCH`.

The missing-path singleton has path-list SHA-256
`f0fbd24ece1085ca146f67193721007ab06309d2d025ee1c1bcde0e5046d279e`.
The ordered unexpected-path list has path-list SHA-256
`be3fb907bd3288d0b5d7df553aafa24be141e6b3f7c5012c2dad36e5eaf4b4db`.

An ignored working-tree file currently exists at the required JUnit path, with
raw SHA-256
`986c54c569e60e8a23744a3098e5f772cf053c69f204eddb8c08c028f2f576ab`
and five passing test cases. It is not present in `b819a7c`, so it is neither
committed checkpoint evidence nor a cure for the historical mismatch.

### Change category

This is a planning-authority and evidence-lifecycle correction within the
already approved Story 7.1 scope. It changes no PRD requirement, product or
runtime behavior, public contract, package, dependency, deployment topology,
UX disposition, release criterion, story number, or downstream predecessor.

### Non-goals

- Do not implement Story 7.1 or any part of Stories 7.2-7.4.
- Do not edit, replace, reinterpret, or regenerate V17, V18,
  `implementation-hold-v1.json`, or commit `b819a7c`.
- Do not amend, revert, squash, or otherwise rewrite Git history.
- Do not certify `b819a7c` or the current ignored JUnit XML as conforming.
- Do not change
  `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Do not create a story final record, acceptance-result sidecar, release
  decision, release/push authorization, or bmad-loop resolution marker.
- Do not edit the approved semantic specification named in the frontmatter.
- Do not let approval of this proposal substitute for V19 checkpoint evidence
  or a V20 release-owner decision.

## 2. Immutable Point-in-Time Evidence

The correction is forward-only. The following objects remain immutable and
retain their original meaning:

| Evidence | Exact identity | Raw SHA-256 or Git identity | Preserved interpretation |
| --- | --- | --- | --- |
| V17 authority | `_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json` | `1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50` | Valid point-in-time authority that lifted only `7.1-SCHEMAS` |
| V18 authority | `_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json` | `24891d990b399fd9526f864c3e8be2b188db728a398f09d9129731d3169f1d94` | Valid point-in-time package/tooling authority that kept full Story 7.1 `ACTIVE` and unresolved |
| Hold record | `_bmad-output/planning-artifacts/implementation-hold-v1.json` | `2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12` | Valid scoped decision with `unlocks: [7.1-SCHEMAS]` and `global: false` |
| Historical transaction | commit `b819a7c43a7024295abaabd418a74f5f64cb5af0` | parent `73bcee6f04479d4743d5a65ce929728e22687d7d`; tree `89baa263615d1e510d15410be7f47b09bff30aca` | Actual mixed transaction; preserved with a new additive nonconformance finding |

V19 must pin all four rows verbatim. It may add a finding about their relation;
it must not mutate their bytes, claims, decisions, candidates, or outcomes.
The new finding is about `b819a7c`'s conformance to V11, not about V17 or V18
validity in their original scopes.

For completeness, the raw mode-`160000` root gitlinks in `b819a7c` are:

| Path | Commit |
| --- | --- |
| `references/Hexalith.AI.Tools` | `5f93d2ec8239494852c97032c819cb1689939e36` |
| `references/Hexalith.Builds` | `a32cb422749352cce8dec948aa3e78c8f00eb4cf` |
| `references/Hexalith.Commons` | `6da79aed2daa4e199689331ee3196f7872c0988a` |
| `references/Hexalith.EventStore` | `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20` |
| `references/Hexalith.Folders` | `9038e04168fb08ea17ce6d6ad22a7f23fcb631c2` |
| `references/Hexalith.FrontComposer` | `1b3608c9b039dbba1be0884d92a2a6d54054e370` |
| `references/Hexalith.Memories` | `e18f51a9ee0e2c53fb17f6fba37b0c5646398acb` |
| `references/Hexalith.Parties` | `fa42398552fba1c80eb2760791517659d6d1313a` |
| `references/Hexalith.Projects` | `4f05a352edd67c4d5595913ee584539c1948dd58` |
| `references/Hexalith.Tenants` | `2fac18396ff11a4459de053b3ebb7ddfe7c13e30` |

These gitlinks describe the historical candidate only. They must not be copied
forward as the fresh checkpoint candidate's current gitlinks.

## 3. Impact Analysis

### Epic and story impact

| Unit | Impact | Required disposition |
| --- | --- | --- |
| Epics 1-6 | Completed history and retrospective evidence remain authoritative for their time basis | Preserve unchanged |
| Epic 7 | Outcome and four-story decomposition remain viable | Add authority gates before resuming Story 7.1 |
| `7.1-SCHEMAS` | Historical transaction is not trustworthy completion evidence | Reperform as a fresh exact-boundary checkpoint, then publish V19 |
| Story 7.1 | Approved scope and all six scenarios remain valid | V20 must explicitly unlock full-story execution after V19 passes |
| Stories 7.2-7.4 | Still require complete Story 7.1 | No change; V19 or V20 alone unlocks none of them |
| Epics 8-16 | Existing graph remains valid | No scope, sequence, or acceptance change |
| IR-0 | Existing V17 input remains immutable evidence | No rerun or reinterpretation in this proposal |
| RG-15 | Independent release closure remains unrelated | No change |

### Artifact conflict analysis

| Artifact | Finding | Proposed treatment |
| --- | --- | --- |
| PRD and addendum | No conflict | No change |
| Canonical Epic 7 and Story 7.1 | Six-scenario story contract remains correct | Preserve; add successor authority records only |
| Architecture V11/V14 | Exact candidate/path/gitlink/ledger semantics are not satisfied by `b819a7c` | Preserve the overlays; V19 applies them to a new candidate |
| V17 and hold record | Correct only for the schema slice | Preserve; do not widen their scope in place |
| V18 | Explicitly records unresolved full-story entry | Preserve; V20 is a later, separately owned successor for Story 7.1 only |
| `spec-7-1-schemas.md` and mixed commit | Useful historical work, not completion authority | Retain as provenance; record `NONCONFORMING` additively |
| Approved Story 7.1 semantic spec | Resolves output/error/inventory ambiguity but has no execution authority | Preserve exact bytes and bind its digest in V20 |
| Sprint status | Orchestrator state is not verification | Read-only; no transition in this workflow |
| UX specification/map | No UX activation or conflict | Preserve `52/28` disposition unchanged |

### Scope evaluation

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Additive evidence and authorities can resolve both gaps without altering product scope | Moderate | Medium until candidate binding and exact-path tests pass | **Selected** |
| Rollback | Cannot erase a published commit or make its evidence conforming | High integrity cost | High | Reject |
| Rewrite V17/V18/hold | Would destroy point-in-time meaning and conflate slice/full-story authority | Superficially low | Critical | Reject |
| Split or renumber Story 7.1 | Disturbs the Epic 7 graph and obligation ledger without solving evidence integrity | High | High | Reject |
| PRD/MVP review | Product intent, requirements, and UX are unchanged | Unnecessary | Solves the wrong problem | Reject |

**Change scope:** Moderate. The work is limited to new planning schemas,
machine-readable authorities, frozen inventory, publication/validation support,
and later release-owner action. Story implementation remains a separate task.

## 4. Recommended Forward-Only Authority Chain

```text
immutable V11 + V17 + hold-v1 + V18 + b819a7c
                         |
                         v
fresh exact five-path 7.1-SCHEMAS candidate
                         |
                         v
V19 candidate-bound checkpoint-completion authority
                         |
                         v
V20 independent release-owner full-Story-7.1 authority
                         |
                         v
Story entry baseline  !=  eventual committed SC-7.1
                         |
                         v
AC-7.1-01..06 and final record (future Story implementation)
```

The V19 and V20 authorities have different owners and effects:

- V19 answers only, "Did a fresh `7.1-SCHEMAS` transaction conform and produce
  committed nonvacuous evidence?"
- V20 answers only, "Does the release owner now authorize implementation of
  the complete Story 7.1 against these exact inputs and entry state?"

V19 cannot lift the full-story hold. V20 cannot manufacture checkpoint
completion. Neither authority marks Story 7.1 done or unlocks Story 7.2.

## 5. Detailed Change Proposals

### 5.1 V19 candidate-bound checkpoint-completion authority

**New schema path:**
`_bmad/schemas/v19-story-7.1-checkpoint-completion-authority-v1.schema.json`

**Schema identity:**
`hexalith.conversations.story-7.1-checkpoint-completion-authority.v1`

**New authority path:**
`_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json`

The schema must be Draft 2020-12, recursively closed, and require at least:

```yaml
schemaVersion: hexalith.conversations.story-7.1-checkpoint-completion-authority.v1
authorityId: V19-STORY-7.1-CHECKPOINT-COMPLETION
predecessor:
  authorityId: V18-PACKAGE-ENVIRONMENT-AUTHORITY
  path: _bmad-output/planning-artifacts/v18-package-environment-authority-v1.json
  sha256: 24891d990b399fd9526f864c3e8be2b188db728a398f09d9129731d3169f1d94
preservedEvidence: <the exact four immutable rows in section 2>
planningAuthority:
  planningCandidate: 1e9a61126d3b7a55b514b7c7c8942d5af03355e5
  bundlePath: _bmad-output/planning-artifacts/v9-authority-bundle-v1.json
  bundleRawSha256: 8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3
  bundleDigest: 159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055
v11Authority:
  epic: epic-6-authority-2026-08-04-v11
  architecture: conversations-architecture-2026-08-04-v11
  sidecarPath: _bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json
  sidecarRawSha256: 14e95c44149594b87e5337b45fd546fdd48d58407fa0b61f3d4b94cba59da82d
historicalTransaction:
  commit: b819a7c43a7024295abaabd418a74f5f64cb5af0
  parent: 73bcee6f04479d4743d5a65ce929728e22687d7d
  tree: 89baa263615d1e510d15410be7f47b09bff30aca
  expectedChangedPaths: <the exact V11 five-path list>
  actualChangedPaths: <the exact 15-path list>
  missingPaths: [artifacts/v9/schema-slice/v2-schema-contract.xml]
  unexpectedPaths: <the exact eleven-path list>
  result: NONCONFORMING
  blockers: [CHANGED_PATH_SET_MISMATCH]
freshCheckpoint:
  baselineCommit: <concrete 40-hex committed root resolved at publication>
  candidateCommit: <concrete single-parent 40-hex committed root>
  changedPaths: <the exact V11 five-path list>
  changedPathInventorySha256: 5137dfa15a6c28253898b7edac5969684abddc2c77b35c2f3299b07faa72cf9d
  changedGitlinkPaths: []
  rootGitlinks: <exact ten raw mode-160000 entries from candidateCommit>
  machineResult:
    path: artifacts/v9/schema-slice/v2-schema-contract.xml
    mode: '100644'
    sha256: <raw committed candidate-blob SHA-256>
    result: PASS
    tests: <positive integer>
    failures: 0
    errors: 0
    skipped: 0
  assertionLedger: <nonempty ordered ledger derived from committed JUnit testcases>
result: PASS
authorityEffect:
  checkpointComplete: true
  implementationHold: ACTIVE
  fullStoryExecutionAllowed: false
  storyDoneAllowed: false
  successorUnlocked: false
  releaseAuthorized: false
  pushAuthorized: false
```

Angle-bracketed values are not authoring placeholders that may survive
publication. They identify candidate-dependent facts that cannot truthfully be
known in this proposal. V19 publication must replace each with a concrete fact
resolved from the new committed candidate; the schema and validator reject a
placeholder, current-worktree substitution, zero count, empty ledger, absent
blob, or non-`100644` result.

The fresh checkpoint must meet all of these gates:

1. `candidateCommit` has exactly one parent, equal to `baselineCommit`.
2. `git diff-tree` exact-set equality is the V11 five-path list—no subset,
   superset, renamed path, or additional documentation/gitlink path.
3. All five changes are substantive checkpoint outputs; a touch-only or
   normalization-only rewrite is not acceptable evidence.
4. The result XML is force-tracked at the V11 path despite the existing
   `artifacts/` ignore rule; `.gitignore` itself does not change.
5. The exact V11 checkpoint command exits `0` and the committed XML is the
   byte snapshot produced by that run.
6. The candidate tree contains exactly ten root `.gitmodules` paths as raw
   mode-`160000` entries. Their path inventory is, in the order below,
   `d1e434be15361351b1aa12b0e22619060761bd2d4b6e1a56f4bcf1f6e8978c20`:

```text
references/Hexalith.AI.Tools
references/Hexalith.Builds
references/Hexalith.Commons
references/Hexalith.EventStore
references/Hexalith.Folders
references/Hexalith.FrontComposer
references/Hexalith.Memories
references/Hexalith.Parties
references/Hexalith.Projects
references/Hexalith.Tenants
```

7. V19 records the candidate's concrete gitlink commits; it never copies the
   V9, `b819a7c`, current `HEAD`, or working-tree values by assumption.
8. V19 is published only after the checkpoint candidate is committed, using
   the established candidate-before-record direction. Publishing V19 is a
   planning/evidence transaction separate from the five-path checkpoint.

### 5.2 Additive nonconformance record

V19 is the authoritative location for the `b819a7c` finding. It must preserve
both ordered path lists and their digests, plus the missing and unexpected
partitions. It must not use weaker language such as "partially conforming",
"accepted with exceptions", or "effectively complete".

The exact new interpretation is:

> Commit `b819a7c43a7024295abaabd418a74f5f64cb5af0` remains immutable actual
> history. As a `7.1-SCHEMAS` completion transaction it is `NONCONFORMING`
> because its changed-path set is not exactly V11's five-path boundary and the
> required machine result is not a committed blob in that candidate.

This finding does not invalidate unrelated work in the commit and does not
authorize reverting any of it. The fresh checkpoint supersedes only the claim
of schema-checkpoint completion; it does not supersede Git history.

### 5.3 V20 release-owner full-story successor authority

**New schema path:**
`_bmad/schemas/v20-story-7.1-release-owner-authority-v1.schema.json`

**Schema identity:**
`hexalith.conversations.story-7.1-release-owner-authority.v1`

**New authority path:**
`_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json`

V20 is a separate, release-owner-authored decision after a committed, valid
V19. It must be recursively closed and require at least:

```yaml
schemaVersion: hexalith.conversations.story-7.1-release-owner-authority.v1
authorityId: V20-STORY-7.1-RELEASE-OWNER-AUTHORITY
predecessor:
  authorityId: V19-STORY-7.1-CHECKPOINT-COMPLETION
  path: _bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json
  sha256: <raw committed V19 SHA-256>
ownerDecision:
  owner: Release owner
  identity: <nonempty human decision identity>
  decidedAtUtc: <concrete UTC instant>
  rationale: <nonempty rationale bound to V19 and the frozen inventory>
  decision: LIFTED
scope:
  unlocks: ['7.1']
  global: false
  checkpointReauthorized: false
  storyDoneAllowedWithoutAcceptance: false
  successorUnlocked: false
candidateRoles:
  planningCandidate: 1e9a61126d3b7a55b514b7c7c8942d5af03355e5
  checkpointCandidate: <exact V19 fresh candidate>
  entryCandidate: <exact committed full-story entry candidate>
  eventualStoryCandidate: SC-7.1
  rolesAreInterchangeable: false
  scKnownAtDecisionTime: false
  scMustDescendFromEntry: true
  scMustBindCurrentRawGitlinks: true
storyWorkBaseline:
  resolution: unique-single-parent-authority-publication
  parent: <the exact entryCandidate above>
  exactChangedPaths:
    - _bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json
  resolvedAtSc: true
authorityBundle:
  path: _bmad-output/planning-artifacts/v9-authority-bundle-v1.json
  rawSha256: 8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3
  bundleDigest: 159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055
v11Authority: <the exact V11 binding from V19>
frozenInputs:
  inventoryPath: _bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json
  inventorySha256: <raw committed inventory SHA-256>
  schemaPath: _bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json
  schemaSha256: <raw committed schema SHA-256>
semanticSource:
  path: _bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md
  sha256: 90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d
  mode: '100644'
  preservedUnchanged: true
assertionLedger: <nonempty release-owner/authority/candidate/scope checks>
result: PASS
authorityEffect:
  implementationHold: LIFTED
  fullStoryExecutionAllowed: true
  storyDoneAllowedWithoutAcceptance: false
  story71CandidateBindingResolved: true
  story72Unlocked: false
  releaseAuthorized: false
  pushAuthorized: false
```

V20's `entryCandidate` is the fixed pre-implementation root selected after V19
completion and before any full-story path changes. It already contains the V20
schema, frozen inventory, publisher validation, and the committed semantic
source, but not the V20 authority record. V20 is then committed in a
single-parent, exact-one-path post-entry authority transaction, following the
candidate-before-record convention. At `SC-7.1`, the generator resolves that
unique child commit from `entryCandidate` and the exact changed path above; that
commit is the Git-diff work baseline for Story 7.1. Full Story implementation
may begin only after the one-path V20 record is committed and validated.

`SC-7.1` is deliberately not filled with `entryCandidate`, `HEAD`, or the V9
planning candidate. It is the later committed implementation candidate from
which all five scenario results are freshly produced. It must descend from the
V20 publication baseline and bind its own ten raw gitlinks, changed paths,
input blob digests, machine results, and nonempty ledgers. A candidate move
after result generation makes the results stale.

The effective hold rule after V20 is fail-closed:

- a valid latest V20 `PASS` with `decision: LIFTED` unlocks only full Story
  `7.1` implementation;
- a missing, invalid, stale, candidate-mismatched, V19-mismatched, revoked, or
  non-`LIFTED` successor evaluates to `ACTIVE`;
- V20 does not rewrite V17 or `implementation-hold-v1.json` and does not claim
  that either previously unlocked the full story; and
- V20 cannot mark the story done, unlock 7.2, authorize release/push, or change
  sprint status.

### 5.4 Frozen authority and Story 7.1 paths

**New inventory schema path:**
`_bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json`

**Schema identity:**
`hexalith.conversations.story-7.1-input-inventory.v1`

**New inventory path:**
`_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json`

The inventory schema is recursively closed. It must encode the canonical NFC
UTF-8 LF rule—one repository-relative path per line, in listed order, with one
terminal LF—and require every scenario result to carry an ordered `(path,
sha256, mode, role)` binding. Raw SHA-256 is calculated over exact bytes from
the committed candidate or the declared machine-result snapshot; a declared
digest is never trusted without recomputation.

The exact Story-owned implementation candidate path inventory is
`V20-7.1-IMPLEMENTATION-PATHS-v1`, SHA-256
`4405332b49ec26ffff40c9b7424c858e7634e0d92313b2c0270011e29a132f4b`:

```text
_bmad/schemas/story-final-record-v2.schema.json
_bmad/schemas/story-record-generator-failure-v1.schema.json
_bmad/scripts/generate_story_record.py
_bmad/scripts/tests/test_generate_story_record.py
docs/runbooks/story-final-record-generation.md
```

These five paths must be the exact implementation delta between the V20 work
baseline and `SC-7.1`. Authority-record publication paths and generated final
record paths are separate roles and cannot be smuggled into this set.

The exact five machine-result paths are inventory
`V20-7.1-RESULT-PATHS-v1`, SHA-256
`f54595279fc201056604e56971f2e22f5483c505ddb1a2488b69e513f08b0fef`:

```text
artifacts/v9/7.1/AC-7.1-01.xml
artifacts/v9/7.1/AC-7.1-02.xml
artifacts/v9/7.1/AC-7.1-03.xml
artifacts/v9/7.1/AC-7.1-04.xml
artifacts/v9/7.1/AC-7.1-05.xml
```

The exact post-candidate record-output paths are inventory
`V20-7.1-RECORD-OUTPUT-PATHS-v1`, SHA-256
`80b1c47320b8f4ebb98e0dfb40d6777cded1e4e08541c7734678991ab5598b64`:

```text
docs/release-evidence/story-7.1-final-record-v2.json
docs/release-evidence/story-7.1-final-record-v2.md
```

No `artifacts/v9/7.1` XML is a caller-authored acceptance-result sidecar. The
generator runs each command, captures the real exit, snapshots and hashes its
ID-derived XML, and constructs the complete acceptance-result object itself.

### 5.5 Frozen current input digests

V20 must preserve or supersede these exact current bindings. A changed byte
requires a new approved additive authority; it is not silently accepted:

| Path | Role | Current raw SHA-256 |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` | V9 authority index | `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3` |
| `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json` | Six-scenario Story 7.1 contract | `548294d8e9752ff3354897efbfc30a1920bf8cea6a3187ac719c0ca9df618d2e` |
| `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` | Exact checkpoint scope | `14e95c44149594b87e5337b45fd546fdd48d58407fa0b61f3d4b94cba59da82d` |
| `_bmad/schemas/v9-story-contract-v1.schema.json` | Story-contract schema | `33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0` |
| `_bmad/schemas/v9-acceptance-result-v1.schema.json` | Acceptance-result schema, pre-V19 observation | `3a0f417edaf9979d3d3ce3f7f06095c89a78cd42b1df20b1529e10f339c67cf9` |
| `_bmad/schemas/v9-frozen-inventory-v1.schema.json` | Frozen-inventory schema, pre-V19 observation | `3741bc4455d34fb0a0b575321eb3d5e909320bb5e17f185ce5cac15d0f992970` |
| `_bmad/schemas/story-final-record-v2.schema.json` | Final-record schema, pre-V19 observation | `212bec3facdd2a68ace486be2919f8defdb01c4720afdb8b2fa77472fd45e5ec` |
| `_bmad/scripts/generate_story_record.py` | Legacy v1 generator baseline | `150a293948ef760173fb7cef7b386e6bd81bfcfdeb9862fa761e0d2286addd8c` |
| `_bmad/scripts/tests/test_generate_story_record.py` | Generator/schema tests, pre-V19 observation | `ef9c5c9d13d0d70f01b02f2f1093be7a11de5201cf4f1cd662cc809113f2155b` |
| `docs/runbooks/story-final-record-generation.md` | Generator runbook baseline | `3068c63dcc3f8cf517634c10dfba7eaf4dae7cc469a7143994bda448a2c213c1` |
| `_bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md` | Approved semantic source | `90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d` |

The three checkpoint-owned schema/test digests are observations before the
fresh V19 transaction and are expected to receive new candidate-bound values.
The generator and runbook remain unchanged through V19, then become
Story-owned candidate-derived inputs at `SC-7.1`. The auxiliary failure schema
does not yet exist; V20 freezes its exact path and required identity, while its
raw digest is intentionally `candidate-derived-at-SC-7.1`, never a fabricated
placeholder.

The existing Story entry obligation inventory remains unchanged:

- identity: `V9-7.1-ENTRY-v1`;
- ordered items: `V8-6.8-AC1`, `V8-6.8-AC6-ANTI-VACUITY`, and
  `V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY`; and
- SHA-256:
  `5fb79e8d9251c3187f2a2de7d4ae3766ab962015e628d345f8033bf14ba8e36e`.

### 5.6 Per-scenario input inventories

Every inventory below includes the same eight-path authority prefix:

```text
_bmad-output/planning-artifacts/v9-authority-bundle-v1.json
_bmad-output/planning-artifacts/v9/story-contracts/7.1.json
_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json
_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json
_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json
_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json
_bmad/schemas/v9-story-contract-v1.schema.json
_bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json
```

#### AC-7.1-01

Inventory `V20-7.1-AC01-INPUT-PATHS-v1` has 12 paths and SHA-256
`78a2fc7f6a53273a4c85ecc2a476cfc88c7896a86f5a6a8fd69eacc44c2540c7`.
After the eight-path prefix, append exactly:

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/scripts/tests/test_generate_story_record.py
```

#### AC-7.1-02, AC-7.1-03, and AC-7.1-04

Each scenario has its own identity—`V20-7.1-AC02-INPUT-PATHS-v1`,
`V20-7.1-AC03-INPUT-PATHS-v1`, or
`V20-7.1-AC04-INPUT-PATHS-v1`—but the same exact 13-path list and SHA-256
`4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908`.
After the eight-path prefix, append exactly:

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/scripts/generate_story_record.py
_bmad/scripts/tests/test_generate_story_record.py
```

#### AC-7.1-05

Inventory `V20-7.1-AC05-INPUT-PATHS-v1` has 14 paths and SHA-256
`2ba87bf3475e3dc799fafbefb30a132c7950057abe98eb887a107f59f6016e26`.
After the eight-path prefix, append exactly:

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/schemas/story-record-generator-failure-v1.schema.json
_bmad/scripts/generate_story_record.py
_bmad/scripts/tests/test_generate_story_record.py
```

#### AC-7.1-06 aggregation

Inventory `V20-7.1-AC06-AGGREGATE-INPUT-PATHS-v1` has 20 paths and SHA-256
`ab3e8bb068cf5fc8b1ade9225f004d6b8dcbbccc15eb59657b1af4bfcfbfb91c`.
Its exact order is `.gitmodules`, then the eight-path authority prefix, then:

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/schemas/story-record-generator-failure-v1.schema.json
_bmad/scripts/generate_story_record.py
_bmad/scripts/tests/test_generate_story_record.py
artifacts/v9/7.1/AC-7.1-01.xml
artifacts/v9/7.1/AC-7.1-02.xml
artifacts/v9/7.1/AC-7.1-03.xml
artifacts/v9/7.1/AC-7.1-04.xml
artifacts/v9/7.1/AC-7.1-05.xml
```

For all six inventories, the path-list digest freezes membership and order.
Each emitted acceptance result additionally records the raw digest and mode of
every listed input resolved at `SC-7.1`; identical path-list digests do not
permit identical-content assumptions. The V19/V20/inventory raw digests must
already be committed and frozen before development starts. Story-owned schema,
generator, test, and XML digests are derived at the committed story candidate.

### 5.7 Approved Story 7.1 semantics preserved

The semantic source at
`_bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md`
remains byte-identical at SHA-256
`90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d`.
It is currently an untracked user file; this workflow neither adds, stages,
commits, nor edits it. Before V20 can become effective, the authority
publication must bind those exact bytes as a committed mode-`100644` source or
block rather than consuming an uncommitted working-tree substitute.

The following approved decisions are carried without reopening them:

1. The four canonical evidence contracts remain distinct from the auxiliary
   operational failure schema.
2. `outputs.json.sha256` is the self-excluding SHA-256 of the exact prescribed
   deterministic JSON bytes with only that property replaced by 64 ASCII
   zeroes; a downstream file binding uses the finished file's raw SHA-256.
3. `outputs.markdown.sha256` and `renderedMarkdownSha256` both bind the exact
   emitted Markdown bytes.
4. Each final-record scenario row for AC-7.1-01 through AC-7.1-05 embeds the
   complete validated acceptance-result v1 object.
5. Pre-identity v2 input/argument failures use the closed auxiliary schema
   `hexalith.conversations.story-record-generator-failure.v1` with exactly
   `schemaVersion`, `result`, and a nonempty ordinally unique `blockers` array;
   the only blockers are `INPUT_SCHEMA_INVALID` and `ARGUMENT_INVALID`.
6. Only exact `--contract` or `--contract=...` syntax selects the additive v2
   route; every legacy v1 behavior remains compatible.
7. AC-7.1-06 resolves one committed `SC-7.1`, executes AC-7.1-01 through
   AC-7.1-05 in frozen order, derives one ID-named JUnit path per scenario,
   captures actual exits and XML snapshots, and derives nonempty test-case
   ledgers in document order.
8. Caller-authored counts, paths, commits, verdicts, digests, exit codes,
   ledgers, result sidecars, and raw-XML-only discovery are not authoritative.
9. The JSON/Markdown final pair is generated atomically only after all five
   predecessor scenarios pass, and the final summary remains `6/6/0/0/0/0`.
10. V1-V18 authorities, completed evidence, submodules, packages, product
    behavior, Stories 7.2-7.4, and sprint status remain outside Story 7.1.

### 5.8 Authority publication boundary

The future authority implementation should use one dedicated publisher and
focused test surface, rather than rebinding or rewriting the V9 bundle:

```text
_bmad/scripts/publish_story_7_1_successor_authorities.py
_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py
```

That publisher may create only the new V19/V20 schemas and records and the V20
inventory named in this proposal. It consumes the V9 bundle read-only and
validates its raw SHA-256 and internal bundle digest. It must not regenerate
V9, V11, V17, V18, the hold record, the semantic spec, sprint status, or any
Story 7.1 implementation/output path.

Publication must be staged as distinct candidate-bound transactions:

1. fresh V11-exact checkpoint candidate;
2. V19 authority publication after that candidate is committed;
3. V20 schema/inventory, unchanged committed semantic source, and the concrete
   release-owner entry candidate, after valid V19;
4. exact-one-path V20 authority record publication and validation; and only then
5. a separately delegated Story 7.1 implementation.

The implementation must add mutation tests for path omission/addition,
gitlink substitution, candidate drift, uncommitted/ignored result substitution,
empty ledger, skipped/not-run result, V19/V20 conflation, PC/entry/SC
conflation, widened unlock scope, semantic-source drift, self-digest misuse,
and sprint-status or resolution-marker writes. Each mutation must fail with a
stable blocker and restore its fixture byte-identically.

## 6. Validation and Acceptance Gates

### V19 completion gates

- `V19-C1`: historical `b819a7c` is recorded exactly as `NONCONFORMING` with
  the expected, actual, missing, and unexpected inventories above.
- `V19-C2`: fresh baseline-to-candidate changed paths equal the V11 five-path
  inventory exactly.
- `V19-C3`: all ten candidate gitlinks are derived from raw mode-`160000` tree
  entries and the checkpoint changes none of them.
- `V19-C4`: the committed result path exists as mode `100644`; its raw digest
  matches V19; it reports positive tests and zero failures/errors/skips.
- `V19-C5`: the assertion ledger is nonempty, unique, ordered, and derived
  from the committed result.
- `V19-C6`: V9 PC, V9 bundle raw/internal digests, V11 authority, V17, V18,
  hold record, and historical transaction identities all match.
- `V19-C7`: authority effect completes only the checkpoint and keeps the
  full-story hold active.

### V20 entry gates

- `V20-C1`: V19 is a committed current `PASS` and its raw digest recomputes.
- `V20-C2`: release-owner identity, UTC decision, rationale, and exact
  `unlocks: [7.1]` scope are nonempty and current.
- `V20-C3`: PC, checkpoint candidate, entry candidate, and eventual
  `SC-7.1` roles are explicit and non-interchangeable.
- `V20-C4`: the V20 inventory schema/data and semantic source are committed,
  schema-valid, digest-valid, and unchanged.
- `V20-C5`: all implementation, result, output, and per-scenario path-list
  identities and digests match section 5.
- `V20-C6`: the assertion ledger is nonempty and proves V20 neither marks the
  story done nor unlocks 7.2/release/push.
- `V20-C7`: any missing/drifted input makes the effective Story 7.1 hold
  `ACTIVE`.

### Proposal-workflow validation

This correction workflow is complete only when:

```text
git diff --check -- _bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md
```

passes and a final worktree audit proves the proposal is the only new change
made by this workflow. The pre-existing modified
`references/Hexalith.Folders` gitlink and the untracked semantic specification
must remain untouched.

The future V19/V20 implementation must define and pass its exact publisher
commands before either authority becomes effective. No command in this
proposal runs the checkpoint, lifts the hold, implements Story 7.1, changes a
lifecycle row, or resolves bmad-loop.

## 7. Implementation and Handoff Plan

### Ownership

| Role | Responsibility |
| --- | --- |
| Product Manager | Approve the forward-only correction, unchanged Story 7.1 semantics, and exact inventories |
| Solution Architect | Approve V19/V20 separation, candidate roles, digest direction, and fail-closed hold composition |
| Quality/Test Architect | Validate exact changed paths, raw gitlinks, committed JUnit evidence, anti-vacuity, and mutation coverage |
| Release owner | Independently author V20 only after V19 passes; decide `LIFTED` or leave Story 7.1 `ACTIVE` |
| Developer | After effective V20 only, implement the five-path Story 7.1 delta and produce fresh scenario evidence |
| bmad-loop operator | Resume the paused story only after both committed authorities validate; write any resolution marker through the dedicated resolution workflow, not this proposal |

### Ordered handoff

1. Obtain explicit approval of this proposal.
2. Mark only this proposal approved; do not change sprint status or loop state.
3. Implement and validate the dedicated successor-authority publisher,
   schemas, and focused tests.
4. Execute a new substantive five-path `7.1-SCHEMAS` transaction from a fresh
   baseline, with the JUnit XML committed.
5. Publish V19 against that committed candidate and validate every V19 gate.
6. Freeze and commit the V20 inventory and the semantic source's existing bytes
   without editing their semantics.
7. Obtain an independent release-owner decision and publish V20 against a
   concrete entry candidate.
8. Validate V20 and confirm its publication baseline before any Story 7.1
   implementation path changes.
9. Route the now-authorized Story 7.1 work to the normal build workflow.
10. Keep Story 7.2 blocked until the eventual `SC-7.1`, all six acceptance
    scenarios, and the generated final record pass.

### Success criteria

- `SC-01`: V17, V18, `implementation-hold-v1.json`, and `b819a7c` retain exact
  point-in-time bytes and meaning.
- `SC-02`: V19 records the old transaction as `NONCONFORMING` using exact V11
  changed-path equality.
- `SC-03`: V19 binds a different, fresh checkpoint baseline/candidate, V9 PC
  and bundle, V11, five changed paths, ten raw gitlinks, committed result
  digest, and nonempty ledger.
- `SC-04`: V19 has no full-story lift, done, successor, release, or push effect.
- `SC-05`: V20 is a separate release-owner authority that unlocks only full
  Story 7.1 implementation after V19.
- `SC-06`: V20 distinguishes PC, checkpoint candidate, entry candidate, and
  eventual `SC-7.1`; it does not predeclare the final story commit.
- `SC-07`: Exact implementation/result/output and six per-scenario input
  inventories are schema-bound with the digests in this proposal.
- `SC-08`: The approved semantic source remains byte-identical and all ten
  decisions in section 5.7 remain binding.
- `SC-09`: Story 7.1 retains six scenarios, one generated JSON/Markdown final
  record, and summary `6/6/0/0/0/0`.
- `SC-10`: Story 7.2 remains locked until complete Story 7.1 evidence passes.
- `SC-11`: PRD, UX, product, package, dependency, submodule, completed
  evidence, release, and Git-history boundaries remain unchanged.
- `SC-12`: Sprint status and the bmad-loop resolution marker remain untouched
  by this correction workflow.

## 8. Change-Scope Checklist

### Trigger and context

- [x] Triggering unit identified: paused Story 7.1 and `7.1-SCHEMAS` evidence.
- [x] Issue classified: nonconforming checkpoint transaction plus missing
  full-story successor authority.
- [x] Exact V11 expected and `b819a7c` actual path sets captured from Git.
- [x] V17, V18, hold, V9, V11, contract, and semantic-source digests captured.

### Epic and story impact

- [x] Epic 7 remains viable without product-scope change.
- [x] Story 7.1 remains the correct owner and retains six scenarios.
- [x] Stories 7.2-7.4 and downstream epics remain valid and blocked by their
  existing predecessors.
- [N/A] No new epic or story is required.
- [x] No renumbering, resequencing, or MVP/PRD change is required.

### Artifact conflict analysis

- [x] PRD conflict: none.
- [!] Additive V19/V20 schemas, authorities, inventory, publisher, and tests
  are required after approval.
- [x] UX conflict: none; preserved-not-activated state remains unchanged.
- [x] Sprint status is read-only and not evidence.
- [x] Existing historical authorities and transaction remain immutable.

### Path evaluation

- [x] Direct Adjustment is viable and selected.
- [x] Rollback/rewrite is rejected because it would destroy evidence without
  creating conforming completion.
- [x] Story split/renumber and PRD/MVP review are rejected as unnecessary.
- [x] Effort: Moderate. Risk: Medium. Schedule remains authority-gated.

### Proposal components

- [x] Issue summary and exact evidence comparison included.
- [x] Epic/story/artifact/technical/schedule impact included.
- [x] V19 and V20 old/new authority effects separated.
- [x] Exact repository paths, schema identities, current digests, candidate
  digest rules, and per-scenario inventories included.
- [x] Rollback, validation, ownership, non-goals, and handoff specified.

### Review and handoff

- [x] Proposal drafted in Batch mode.
- [x] Explicit approval received from Jerome on 2026-09-12.
- [N/A] No sprint-status structural change is due.
- [!] Authority implementation/publication remains future work after approval.
- [!] Release-owner V20 decision remains an independent future gate.
- [!] Story 7.1 implementation remains blocked.

## 9. Approval Gate

**Status:** Approved by Jerome on 2026-09-12.

Approval authorizes only implementation and validation of the additive V19/V20
planning-authority correction described here. It does not certify the
checkpoint, supply the release-owner V20 decision, lift the current full-story
hold, start or complete Story 7.1, change sprint status, authorize a release or
push, commit current user changes, or write a bmad-loop resolution marker.

## 10. Workflow Execution Log

- `2026-09-12`: Required repository baseline, Git guidance, BMad workflow,
  PRD/addendum, canonical epics, architecture, and UX authorities reviewed.
- `2026-09-12`: V17, V18, hold, V9/V11, Story 7.1 contract, semantic spec,
  current schemas/generator/tests/runbook, and `b819a7c` Git objects inspected.
- `2026-09-12`: Exact V11-versus-`b819a7c` comparison classified the historical
  checkpoint transaction `NONCONFORMING`.
- `2026-09-12`: Direct Adjustment selected; rollback, history rewrite, story
  split, and PRD/MVP review rejected.
- `2026-09-12`: Pending-approval V19/V20 correction proposal created without
  Story implementation or orchestrator-state mutation.
- `2026-09-12`: Jerome explicitly approved the proposal for the additive
  V19/V20 planning-authority correction only.
- **Final scope:** Moderate.
- **Current effective state:** full Story 7.1 remains `ACTIVE`/blocked pending
  a valid V19 checkpoint-completion authority and separate V20 release-owner
  successor authority.
