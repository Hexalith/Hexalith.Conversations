---
title: "Sprint Change Proposal — Story 7.1 Final-Record Preservation-Evidence Successor"
date: "2026-09-21"
project: "Conversations"
mode: "Batch"
status: "APPROVED — READY FOR SPEC CREATION"
scope: "Moderate — additive preservation-evidence/tooling successor; no product or release authority"
changeOwner: "Story 7.1 Developer with Planning Tooling and Quality"
approvalOwner: "Repository/Release owner"
approvalRecord: "Explicit user response on 2026-09-21: continue and approve"
handoffStatus: "READY FOR SPEC CREATION; IMPLEMENTATION NOT STARTED"
implementationHold: "ACTIVE"
---

# Sprint Change Proposal — Story 7.1 Final-Record Preservation-Evidence Successor

## 1. Issue Summary

Story 7.1's V24 implementation publication is commit
`20e2cdd2b37e6387055b241c7ee45fc54e836742`. Its focused Python verification
passes 312 tests, its candidate-bound rebuild completes with zero warnings and
zero errors, and seven root test projects pass without skips. The root result is
2,023/2,026 because `Hexalith.Conversations.Conformance.Tests` remains 470/473.
No final-record bundle or completion-record commit exists.

The three failures have two different causes and must not be handled as one
blanket exception.

| Failure | Classification | Evidence | Required disposition |
| --- | --- | --- | --- |
| `FaultInjectedCandidatesShouldFailWithStableDiagnostics` | Linked-worktree fixture defect | `ValidateRealFilesystemFaults` creates `.git/rc2-evidence-fault-*`; in a linked worktree `.git` is a file, so setup fails before the intended regular-file, symlink, directory, and FIFO diagnostics run | Make the fixture repository-locality-independent in an isolated temporary Git repository; preserve every asserted diagnostic |
| `BindingsClosuresAndFrozenV1BytesShouldValidateIndependently` | Genuine frozen-evidence time-basis conflict | The rc.2 contract fixes base `2d2ae57db1fdcc164fe01ac4b1d99af15c31b324` and its exact historical overlay path inventory, while the test currently compares that base with the later V24 checkout | Validate the immutable rc.2 overlay at its recorded publication commit, not against the live descendant; add a separate successor transaction boundary for current changes |
| `CurrentControlsAndTierPrerequisiteShouldStayTruthful` | Genuine frozen-evidence identity conflict | The final-record gate requires the live Conformance assembly to embed candidate `20e2cdd...`; frozen rc.2 requires the historical candidate digest `1ba2f3e...`, producing `BUILD_RECEIPT_ASSEMBLY_MISMATCH` | Validate the frozen receipt/assembly chain at its historical identity and validate the live assembly against the actual current Git candidate as a distinct assertion |

The two identities are intentionally incompatible. A live assembly cannot
truthfully embed both the 40-character Story candidate commit and the
64-character rc.2 content-overlay digest. Suppressing the mismatch, accepting
either identity, rewriting the receipt, or widening the V24 commit would weaken
candidate binding and is prohibited.

### Immutable anchors

- Approved preservation predecessor rc.1 and its detached approval remain
  byte-identical.
- Unapproved preservation successor rc.2 and every JSON, Markdown, schema,
  digest, receipt, semantic result, XML, toolchain capture, and detached-index
  byte remain byte-identical.
- Rc.2 publication commit
  `5ad5d3c0fb57c6ce5f5bbd4567ae9b5a5397e60d`, base
  `2d2ae57db1fdcc164fe01ac4b1d99af15c31b324`, candidate digest
  `1ba2f3e23b56dc3c1caa05addb175461311e472487c5f2c4580317ee109d8840`,
  and its 473-test/969-obligation contract remain historical facts.
- V23 commit `5a7234b922371b5d0a12085a444d93783263f278` remains the immutable
  exact-nine request publication with its original BLOCKED/non-executable
  semantics.
- V24 commit `20e2cdd2b37e6387055b241c7ee45fc54e836742` remains the immutable
  direct-child exact-eight tooling correction with `implementationHold=ACTIVE`,
  `executionAllowed=false`, `releaseAuthorized=false`, and
  `pushAuthorized=false`.

## 2. Impact Analysis

### Epic and story impact

Epic 7 remains **Reliable Mechanical Completion Records**. No epic or story is
added, removed, renumbered, or reordered. Story 7.1 remains `in-progress`; Story
7.2 stays locked. The repair is a corrective implementation slice inside Story
7.1 because the final-record gate cannot pass truthfully at V24.

The V24 publication remains the implementation/tooling anchor. A later V25
successor becomes the candidate used by the final-record generator because the
candidate-bound Conformance assembly must contain the successor fix. This does
not widen V24: raw Git verification must continue to show exactly the original
eight V24 paths at commit `20e2cdd...`; V25 is a separate commit with a separate
path manifest and identity.

### PRD impact

No PRD requirement or MVP scope changes. The proposal enforces rather than
relaxes FR-20/SM-C1's additive-only rule:

- all 473 ordered test identities remain;
- all 969 obligation identities/classifications, seven categories, 214/384
  floors, 89 pending additions, 277 dispositions, and zero-orphan result remain;
- no approved or frozen evidence is rewritten;
- no approval transfers from rc.1 to rc.2 or V25;
- FR-20 and SM-C1 remain `PENDING`, SM-C2 remains `FAILED`, OQ-1 remains
  `BLOCKED`, and the implementation hold remains `ACTIVE`.

### Architecture and authority impact

No product/runtime architecture changes. The authority chain gains one
append-only, non-executable evidence/tooling successor:

```text
V23 request publication (immutable exact nine, BLOCKED/false)
  -> V24 tooling correction (immutable exact eight, ACTIVE/non-executable)
    -> V25 preservation-evidence/tooling successor (new exact scope,
       ACTIVE/non-executable)
      -> candidate-bound final record
        -> separately approved terminal authority, if all other gates pass
```

V25 must authenticate V23 and V24 from raw Git objects before its own code is
trusted. Once V25 appears in ancestry, its route is sticky: deletion, reversion,
mode drift, path drift, or fallback to V24-only handling blocks. Existing V24
publisher/resolver/verifier bytes remain unchanged; their visible rejection of
an unknown descendant remains correct until the repository owner selects the
reviewed V25 host. No candidate-authored record may claim that owner action.

V25 becomes only the current preservation-validation tooling anchor. It does
not reinterpret V23, replace the V24 correction, approve rc.2, lift the hold,
authorize release/push, or accept Story 7.1.

### UX impact

None. UX remains `preserved-not-activated`; no screen, component, flow,
accessibility contract, or UX requirement map changes.

### Secondary artifact impact

| Artifact area | Impact |
| --- | --- |
| Preservation Conformance validator | One existing C# file changes under an authenticated successor; public test method identities remain exact |
| Planning tooling | One new V25 publisher, schema, record, and focused test establish the independent trust boundary |
| Frozen preservation artifacts | Read-only; no path may be edited or regenerated |
| V23/V24 tooling and records | Read-only; no path may be edited, replaced, or re-emitted |
| Sprint status | Remains `in-progress`; no status change until the final-record gate passes |
| Product code, public contracts, packages, dependencies, submodules, gitlinks, deployment | No change |

## 3. Recommended Approach

Use a **Direct Adjustment** within Story 7.1: publish a separate V25
preservation-evidence/tooling successor with a closed five-path source
transaction. Do not create a full replacement preservation manifest; the
failure requires a durable time-basis separation, not a new denominator or a
new approval claim.

The successor keeps each assertion strong:

1. Frozen rc.2 is validated from its committed publication objects and retained
   evidence chain.
2. The live Conformance assembly is independently required to contain exactly
   one 40-character `SourceRevisionId` equal to the evaluated candidate commit.
3. The filesystem fault matrix runs in an isolated temporary Git repository and
   therefore covers normal and linked worktrees identically.
4. The V25 record proves that the one modified frozen-source consumer was
   changed only through an explicit additive successor.

Potential rollback is not the primary route: reverting V24 would discard a
green, reviewed correction and would not make the frozen rc.2/live-candidate
identity contradiction valid. MVP review is not applicable because product
scope and acceptance thresholds are unchanged.

- Estimated effort: 1–2 focused engineering days plus independent Quality and
  repository-owner review.
- Technical risk: medium; the main risk is accidentally converting historical
  receipt validation into a permissive current check.
- Authority risk: high if V25 is allowed to rewrite history or execute; mitigated
  by exact paths, immutable digest checks, raw-Git predecessor authentication,
  sticky routing, and closed non-executable result fields.
- Timeline impact: Story 7.1 remains `in-progress` until V25 and the regenerated
  candidate-bound final record pass. No downstream story is unlocked by V25
  alone.
- Scope classification: **Moderate**, requiring Developer, Planning Tooling,
  Quality, and repository-owner coordination but no product replan.

## 4. Detailed Change Proposals

### 4.1 Frozen overlay validation time basis

**Artifact:**
`tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs`

**OLD:** `BindingsClosuresAndFrozenV1BytesShouldValidateIndependently` computes
the exact rc.2 path set from `BaseCommit` to the live checkout and unions live
untracked/generated paths. Any legitimate post-rc.2 descendant therefore looks
like an illicit expansion of the historical overlay.

**NEW:** Keep `BaseCommit`, the rc.2 path inventory, bindings, and diagnostics
unchanged, but evaluate them at the committed rc.2 publication
`5ad5d3c0fb57c6ce5f5bbd4567ae9b5a5397e60d`. Validate every historical blob and
mode using `git ls-tree`, `git show`, and bound hashes at that revision. Validate
the live V25 transaction separately from its own record.

**Rationale:** Immutable evidence is revalidated at its recorded time basis;
current candidate scope is governed by a successor rather than silently folded
into rc.2.

### 4.2 Historical receipt and live candidate assembly separation

**Artifact:** the same C# validator; preserve the public method name
`CurrentControlsAndTierPrerequisiteShouldStayTruthful`.

**OLD:** The method validates the frozen rc.2 receipt and then requires
`Assembly.GetExecutingAssembly()` to end in the frozen 64-character candidate
digest. Final-record generation separately requires the same assembly to embed
the live 40-character Git candidate.

**NEW:**

- Load the frozen rc.2 manifest, build receipt, run receipt, semantic result,
  and related bindings from their committed publication and verify their exact
  hashes, sizes, closed shapes, cross-references, counters, and recorded
  `1ba2f3e...` identity without substituting the live binary.
- Resolve the evaluated candidate as the exact result of
  `git rev-parse --verify HEAD^{commit}` in the test repository. Require the
  executing Conformance assembly to expose exactly one 40-character source
  revision equal to that commit; require V24 commit `20e2cdd...` to be its
  ancestor and require the candidate tree to contain the exact immutable
  V23/V24 objects.
- Preserve stable failure diagnostics with distinct historical-receipt and
  current-candidate codes. A historical mismatch must never be reported as a
  current-candidate mismatch or ignored because current tests are green.

**Rationale:** This maintains both claims instead of forcing one file to
impersonate two source identities.

### 4.3 Linked-worktree-safe fault fixture

**Artifact:** the same C# validator; preserve the public method name
`FaultInjectedCandidatesShouldFailWithStableDiagnostics`.

**OLD:** The real-filesystem fixture creates a directory beneath literal
`.git/`, which is invalid when `.git` is the linked-worktree gitfile.

**NEW:** Create an isolated temporary Git repository, place the regular,
symlink, directory, FIFO, index-mode, staged-removal, staged-symlink, and
gitlink fixtures beneath that repository root, pass the explicit repository
root to the existing validators, and delete the fixture in `finally`.

**Rationale:** The test now exercises its intended file and Git-mode rules in
both primary and linked worktrees without touching real repository metadata.

### 4.4 V25 closed successor publication

Create one atomic, regular-file-only V25 transaction with exactly these source
paths, all mode `100644`, and no rename, symlink, dependency, submodule, or
gitlink change:

1. `_bmad-output/planning-artifacts/v25-story-7.1-preservation-evidence-tooling-successor-v1.json`
2. `_bmad/schemas/v25-story-7.1-preservation-evidence-tooling-successor-v1.schema.json`
3. `_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py`
4. `_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py`
5. `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs`

The record is generated last and self-excludes its own blob from the manifest.
It must bind:

- the exact V23 and V24 commits, trees, record/schema/publisher identities,
  path sets, modes, and root gitlinks;
- the rc.1, rc.1 approval, and complete rc.2 artifact-set digests;
- rc.2 publication commit, base, candidate digest, and unchanged 473/969
  identity summaries;
- the exact four non-record V25 blobs and the exact five-path transaction;
- a nonempty assertion ledger;
- `implementationHold=ACTIVE`, `executionAllowed=false`,
  `ownerApprovalClaimed=false`, `releaseAuthorized=false`, and
  `pushAuthorized=false`.

The new publisher is an independent pre-execution validator. It may reuse data
formats, but must not import candidate code before authenticating the pinned V23
and V24 objects. It must reject wrong parent/ancestry, hidden duplicate V25
publications, ninth-path scope, mode drift, record deletion/reversion, V23/V24
byte drift, gitlink drift, denominator drift, and any executable claim with
stable nonempty diagnostics.

### 4.5 Existing artifacts that remain unchanged

No edit is proposed to:

- `_bmad-output/implementation-artifacts/spec-v24-story-7-1-entry-authority-tooling-successor.md`;
- `_bmad-output/implementation-artifacts/sprint-status.yaml` before successful
  final-record generation;
- the PRD, architecture, epics, UX artifacts, V23/V24 records/schemas/tooling,
  any rc.1/rc.2 evidence, product/runtime source, package/dependency files,
  submodule worktrees, or root gitlinks.

## 5. Ownership and Handoff

| Owner | Responsibility | Completion evidence |
| --- | --- | --- |
| Story 7.1 Developer | Implement the three validator corrections and V25 publisher/schema/record within the exact path allowlist | Candidate diff, focused tests, clean rebuild, and root test artifacts |
| Preservation/Quality owner | Confirm historical/current time-basis separation preserves all 473/969 identities and every negative diagnostic | Independent review plus frozen-artifact and Conformance results |
| Planning Tooling owner | Own V25 schema, raw-Git authentication, sticky route, exact-scope checks, and future host integration contract | V25 publisher tests and successor result envelope |
| Repository/Release owner | Independently verify the V25 candidate commit/tree/path modes/gitlinks and select any V25-aware trusted host | External approval bound to the exact candidate; never candidate-authored |
| Story 7.1 lifecycle owner | Generate and verify the final record only after all required checks pass; keep lifecycle `in-progress` on any blocker | Passing final-record bundle and digest verification |

Handoff is to the Developer, Planning Tooling, and Quality roles. Repository
owner review is required before V25 can be treated as the selected successor.
This proposal itself grants no implementation, merge, release, or push
authority.

## 6. Acceptance Tests

### 6.1 Immutable-history tests

- Raw Git objects prove V23 remains its original exact-nine transaction and V24
  remains its original direct-child exact-eight transaction, with all paths
  mode `100644` and root gitlinks unchanged.
- SHA-256 verification passes for rc.1, rc.1 approval, rc.2 JSON/Markdown/schema/
  digest, detached index/digest, build/restore/run receipts, semantic results,
  XML, logs, and toolchain capture without regenerating any file.
- Rc.2 overlay validation passes at commit `5ad5d3c...`; adding unrelated
  descendant paths does not alter that historical result.
- Mutating the frozen base, publication, path inventory, blob, mode, receipt,
  assembly metadata, test identity, obligation, category, disposition, or gate
  state still fails with a stable nonempty diagnostic.

### 6.2 V25 authority-boundary tests

- Valid V25 is accepted only when V23 and V24 authenticate first and the
  transaction contains exactly the five declared mode-`100644` paths.
- V25 generation is bound to the exact selected predecessor `HEAD`; wrong base,
  extra path, duplicate publication, record self-inclusion, symlink, mode drift,
  root-gitlink drift, or post-write input mutation blocks and quarantines only a
  record created by that invocation.
- Any V25 occurrence in ancestry is sticky. Deletion/reversion, marker-free
  fallback, or restoration of V24-only tooling blocks rather than downgrades.
- Valid V25 remains non-executable and makes no approval, completion, release,
  or push claim.

### 6.3 Conformance and linked-worktree tests

- The unchanged 473 Conformance test identities run 473/473 with zero failed,
  skipped, or not-run checks in both the primary checkout and an isolated linked
  worktree.
- The fault-injection test reaches every regular-file, symlink, directory, FIFO,
  index-only execute-bit, staged-removal, staged-symlink, and staged-gitlink
  assertion in both layouts and leaves neither repository dirty.
- The live Conformance assembly carries exactly the evaluated 40-character V25
  candidate commit. A stale V24 assembly, a 64-character rc.2 digest, no source
  revision, two revisions, or another commit fails.
- The historical rc.2 receipt continues to carry `1ba2f3e...`; replacing it
  with the live V25 candidate fails rather than being accepted as migration.

### 6.4 Full completion gate

- Existing focused V24 Python lanes remain at least 312/312; all new V25 tests
  pass with no skip or xfail.
- Candidate-bound Release rebuild completes with zero warnings and zero errors.
- The authoritative eight root test projects pass 2,026/2,026 with zero skips;
  Conformance is 473/473.
- `generate_story_record.py` returns `PASS`, binds the exact V25 candidate in
  every TRX assembly, derives the complete changed-path set, and verifies its
  inserted Markdown digest. It must still identify V24 commit `20e2cdd...` as
  the immutable exact-eight ancestor rather than representing the V25 paths as
  part of V24.
- `git diff --check`, the submodule-promotion gate, V24 historical validation,
  and V25 successor validation pass. No root gitlink moves and no nested
  submodule is initialized.

## 7. Migration Order

1. Freeze and re-hash V23, V24, rc.1, rc.1 approval, rc.2, current sprint state,
   and all ten root gitlinks. Record the current 2,023/2,026 and 470/473 results
   as the pre-successor baseline.
2. Create, approve, and publish the next implementation spec named in §9 before
   selecting the V25 implementation baseline. The spec/publishing commit is not
   part of the five-path V25 transaction. The spec pins its exact successor
   predecessor, requires V24 in ancestry, repeats the five-path allowlist, and
   does not edit V24.
3. Implement the linked-worktree fixture fix and the historical/current
   time-basis split in the existing C# test without adding, removing, or
   renaming a Fact/Theory.
4. Implement the independent V25 publisher and focused tests. Generate the V25
   record last, after the other four blobs are stable.
5. Commit the five paths as one exact-scope successor transaction. Independently
   verify its raw parent/ancestry, tree, modes, path manifest, frozen hashes,
   and gitlinks before any trusted-host selection.
6. From the exact committed V25 candidate, perform a clean candidate-stamped
   rebuild and run the focused Python, Conformance, and all root test lanes.
7. Generate the Story 7.1 final-record bundle against V25, insert its Markdown
   verbatim, and verify the digest. Any failure returns the story to or keeps it
   at `in-progress`.
8. Only after the final record passes may a later output-only lifecycle commit
   update the Story 7.1 record and sprint projection. Story 7.2 remains locked
   until the separately governed terminal authority reaches `ACCEPTED`.

## 8. Rollback Behavior

- Before V25 commit: remove or quarantine only files created by the attempted
  V25 invocation and restore the exact pre-attempt worktree/index. Do not touch
  V23, V24, rc.1, or rc.2.
- After V25 commit but before owner selection: do not amend, rebase, or delete
  the publication. Record it as blocked/abandoned through an additive successor
  decision and keep Story 7.1 `in-progress`.
- If V25 tests or final-record generation fail: discard only regenerable build,
  TRX, and final-record outputs; preserve the committed V25 failure evidence and
  stable diagnostics. There is no fallback that treats V24 alone as complete.
- If owner selection is withdrawn or candidate drift occurs: the V25 route
  resolves to `ACTIVE`; rc.1/rc.2 remain historical evidence, V23/V24 remain
  immutable, and no downstream story is unlocked.
- Recovery never rewrites a frozen receipt, changes an expected hash, reduces a
  test/obligation denominator, skips a test, or disables candidate binding.

## 9. Next Implementation Spec

Create, review, and approve:

`_bmad-output/implementation-artifacts/spec-v25-story-7-1-preservation-evidence-tooling-successor.md`

That spec should use this proposal as context, pin the exact implementation
baseline selected at creation time, carry the five-path transaction unchanged,
define the stable diagnostic vocabulary, and remain `in-progress` until the
candidate-bound final-record gate passes. Do not modify the V24 spec to absorb
this work.

## 10. Change Navigation Checklist Result

| Checklist area | Status | Finding |
| --- | --- | --- |
| Trigger and evidence | [x] Done | Story 7.1 final-record gate; exact 470/473 and 2,023/2,026 evidence supplied and traced to code/contracts |
| Current epic impact | [x] Done | Epic 7 remains viable; Story 7.1 needs one corrective successor slice |
| Other epics/order | [x] Done | No epic, story, or dependency reorder; Story 7.2 remains locked |
| PRD/MVP | [x] Done | No scope or target change; FR-20/SM-C1 additive-only constraints govern |
| Architecture | [!] Action-needed | Publish the V25 non-executable authority-chain link and require owner-selected trusted-host migration |
| UX | [N/A] Skip | Preserved-not-activated UX has no implementation impact |
| Testing/tooling/docs | [!] Action-needed | Five source paths plus generated test/final-record outputs described above |
| Direct adjustment | [x] Viable | Moderate effort and medium technical risk; recommended |
| Rollback | [x] Not preferred | Reverting V24 does not resolve the identity contradiction |
| MVP review | [N/A] Skip | Product scope remains achievable and unchanged |
| Handoff | [x] Done | Developer, Planning Tooling, Quality, repository owner, and lifecycle owner assigned |
| User approval | [x] Done | Explicit user response on 2026-09-21: `continue and approve` |
| Sprint-status update | [N/A] Skip for proposal | Existing `in-progress` state is already truthful; update only after a passing final record |

## 11. Success Criteria

The course correction succeeds when all of the following are true:

- the fixture defect is fixed without weakening any fault assertion;
- frozen rc.2 validates at its historical publication while the live assembly
  validates against the current Git candidate;
- V23, V24, rc.1, and rc.2 remain byte- and identity-exact;
- V25 is an exact-scope, non-executable, additive successor with a nonempty
  fail-closed ledger;
- Conformance passes 473/473 and the root passes 2,026/2,026 with zero skips;
- the candidate-bound final record is generated and digest-verified against the
  V25 candidate; and
- no approval, hold lift, release, push, or Story 7.2 authorization is inferred
  from the V25 evidence/tooling correction.

## 12. Approval and Implementation Handoff

**Decision:** Approved on 2026-09-21 by explicit user response, `continue and
approve`.

**Scope classification:** Moderate. The correction does not change product
scope or create/reorder backlog items, but it requires coordinated preservation,
planning-tooling, test, and repository-owner work before Story 7.1 can complete.

**Routed to:**

- Product/Story owner — retain this proposal as the approved scope boundary and
  keep Story 7.1 `in-progress` until its final record passes.
- Developer — create the V25 implementation spec and implement only its exact
  five-path transaction after that spec is reviewed.
- Planning Tooling — review the successor schema, publisher trust boundary,
  sticky lineage, and stable diagnostics.
- Preservation/Quality — independently verify frozen/current time-basis
  separation and all 473/969 invariants.
- Repository/Release owner — approve only an exact candidate-bound V25
  transaction after independent raw-Git and test verification.

**Immediate next deliverable:**
`_bmad-output/implementation-artifacts/spec-v25-story-7-1-preservation-evidence-tooling-successor.md`.

Approval completes the course-correction decision and authorizes the handoff to
spec creation. It does not itself implement V25, change sprint status, select a
trusted host, lift the hold, accept Story 7.1, unlock Story 7.2, or authorize a
release or push.
