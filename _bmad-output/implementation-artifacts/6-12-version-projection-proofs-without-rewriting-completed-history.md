---
story_key: '6-12-version-projection-proofs-without-rewriting-completed-history'
epic: 6
story_id: '6.12'
created: '2026-08-01'
status: 'ready-for-dev'
baseline_commit: '331ec28eeb403c2d62375c384842f3eb1b95d78a'
submodule_promotions: []
authority:
  overlay: 'epic-6-authority-2026-08-01-v8'
  architecture: 'conversations-architecture-2026-08-01-v8'
  proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-implementation-readiness-authority-correction.md'
  frozen_criteria_source: 'epic-6-authority-2026-08-01-v7'
  current_view: '_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-3-create-complete-preservation-traceability-manifest.md'
---

# Story 6.12: Version projection proofs without rewriting completed history

Status: ready-for-dev

> **Global hold:** `ready-for-dev` is a file-lifecycle label, not permission to
> begin. Story 6.12 remains non-startable until comprehensive v8 authority
> validation passes, a separate independent implementation-readiness assessment
> returns `READY`, and Story 6.8 is `done`.

## Story

As a release owner,
I want completed projection proofs validated at their recorded candidate and
current readiness represented by an explicit successor chain,
so that later approved platform work neither falsifies history nor inherits
stale assurance.

## Acceptance Criteria

The criteria and prohibitions below are frozen authority, quoted verbatim from the v7 amendment in
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:1426-1474`.
Do not paraphrase, renumber, weaken, or satisfy them with a backlog disposition.

1. Story 6.2 remains `done` and its story record, v2 JSON/Markdown, three bound
   xUnit results, generated final record, and immutable signed-v1 dependencies
   remain byte-identical. The v2 validator reads root-owned blobs from umbrella
   candidate `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` and platform-owned
   blobs from the root gitlinks recorded in that candidate; it proves every
   recorded hash, mode, gate result, and run binding at that time basis.
2. Historical validation no longer compares v2's recorded commit or hashes to
   the current worktree, and it does not prohibit later unrelated root gitlink
   or production-source movement. It remains strict against mutation or
   unresolvable recorded Git objects.
3. ADR 0004 defines an immutable predecessor-linked projection-proof lifecycle:
   full predecessor artifact hashes, exactly one approved current head, exact
   changed dependency identities, named owner and rationale, and no in-place
   evidence mutation.
4. `projection-read-store-population-proof-v3` is generated against the current
   candidate. It reruns deterministic dispatch, gateway/DAPR boundary,
   configured state-store end-state, production queries, derived-state
   deletion, and full-replay evidence; binds current in-scope source/test blobs
   and the EventStore gitlink; and links to the unchanged v2 hashes.
5. The current-readiness guard follows the approved chain head and compares
   only declared projection-proof dependencies. In-scope drift without a
   successor fails with `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; unrelated
   root gitlink movement does not invalidate historical proof.
6. Fault injection rejects a changed v2 byte, wrong historical
   candidate/gitlink/blob, broken predecessor hash, duplicate or forked chain
   head, stale v3 binding, missing/red/skipped/vacuous run, and undeclared
   in-scope drift, with byte-identical restoration after every mutation.
7. Story 6.3 binds v2 as historical evidence and v3 as the current chain head.
   Story 6.6 consumes both, reruns v3's functional gates, and cannot cite v2
   alone for current readiness.
8. The focused projection-proof class, Story 6.3 manifest validation class, and
   full Conformance project pass with zero failed, skipped, or not-run tests;
   Story 6.12's completion record is generated through Story 6.8.

**Prohibitions.** Story 6.12 does not modify production source, public
contracts, package versions, accepted baselines, Story 6.2's record or v2 proof
artifacts, signed-v1 evidence, or submodule content. It does not make a backlog
disposition stand in for executed current proof and does not weaken or delete a
projection assertion to make the suite green.

## V8 Internal Checkpoints

| Checkpoint | Frozen criteria | Review and rollback boundary |
| --- | --- | --- |
| 6.12-A Historical validity and lifecycle contract | AC1-AC3 | Protected-byte inventory, candidate-aware historical validation, ADR 0004, and closed successor-chain schema; no v3 current-head claim. |
| 6.12-B Successor generation and current guard | AC4-AC5 | Deterministic v3 projection, fresh functional lanes, exact approval, one current head, and drift guard; discardable without changing v2 history. |
| 6.12-C Fault injection, manifest handoff, and closure | AC6-AC8 | Complete mutation matrix, Story 6.3 and 6.6 handoff, full Conformance, and Story 6.8-generated final record. |

Checkpoint success does not advance the story to `done`; all eight frozen v7
criteria must pass at one compatible final candidate.

## Tasks / Subtasks

- [ ] **Entry gate — finish Story 6.8 before beginning implementation** (AC: 8)
  - [ ] Confirm Story 6.8 is `done`, has a current generated final record, has a populated
    `file_list_commit`, and its generator/checker passes. Its present `in-progress` status does not
    satisfy the dependency `6.8 -> 6.12`.
  - [ ] Re-read the live v8 overlay, architecture amendment, current execution view, proposal, Epic 6 context, Story 6.3
    spec, repository guidance, and current working-tree state. Preserve concurrent work.
  - [ ] Confirm v8 still requires `6.8 -> 6.12`, with 6.12 gating completion of 6.3 and 6.6, and Story 6.6 last.
  - [ ] Capture the implementation baseline only after the entry gate passes; do not reuse the
    story-creation baseline as the v3 candidate.

- [ ] **Freeze and continuously guard the historical byte set** (AC: 1, 2, 6)
  - [ ] Before any change, recompute and record the protected SHA-256 set in the test/generator
    fixture. Treat the Story 6.2 record, its embedded generated final record, v2 JSON/Markdown,
    three v2 xUnit XML results, and all four signed-v1 dependencies referenced by v2 as read-only.
  - [ ] Add before/after assertions around every fault-injection lane and the final completion gate.
    A mutation test must use an isolated copy when possible; otherwise restore exact original bytes
    in `finally` and assert the post-test hash equals the pre-test hash.
  - [ ] Never try to read the finalized v2 proof itself from candidate `856ee997...`: the protected
    v2 declaration was finalized later. Read the protected current v2 artifact as the declaration,
    verify its protected hash, and resolve the source/test/platform bindings it declares at their
    recorded time basis.

- [ ] **Author ADR 0004 and the closed successor-chain contract** (AC: 3, 5, 7)
  - [ ] Add `docs/adrs/0004-projection-proof-evidence-lifecycle.md` and register it in
    `docs/adrs/index.md`. The v7 reservation controls despite an older architecture backlog use of
    the label “ADR-004”.
  - [ ] Define immutable proof nodes, full SHA-256 predecessor bindings, candidate identity,
    declared dependency identity, exact predecessor-to-successor dependency changes, named owner,
    rationale, approval identity/time, and a single approved current head.
  - [ ] Define deterministic rejection of missing predecessors, broken links, cycles, forks,
    duplicate heads, multiple/no approved heads, stale head bindings, and mutation of any historical
    node. Do not infer currentness from filename ordering or mutable `HEAD`.
  - [ ] Define roles explicitly: v2 is immutable historical evidence for its recorded candidate;
    v3 is current assurance only after its exact bytes are reviewed and approved; later proofs must
    supersede additively. Story 6.6 must traverse and rerun the approved head.
  - [ ] Define dependency scoping: only explicitly declared projection-proof dependencies determine
    head freshness. Other root gitlinks and unrelated sources are not implicit dependencies.

- [ ] **Replace mutable-checkout historical validation with recorded Git-object validation**
  (AC: 1, 2, 6)
  - [ ] Refactor
    `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs`
    so historical validation never substitutes current root `HEAD`, submodule `HEAD`, worktree
    bytes, cleanliness, remote containment, or current mechanical-gate output for recorded facts.
  - [ ] Resolve every root-owned binding from umbrella commit
    `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58`; require the exact tree mode/type/path, read the
    blob bytes from the object database, and compare their SHA-256 with the declaration.
  - [ ] Resolve every platform-owned binding in two steps: read the mode-`160000` gitlink from the
    umbrella candidate, then read the declared platform path from that recorded commit in the
    corresponding submodule object database. Do not read the submodule checkout file.
  - [ ] Fail closed with stable diagnostics when a commit, gitlink, tree entry, blob, mode, bound
    result, or run object is absent, ambiguous, malformed, or unresolvable.
  - [ ] Keep existing deterministic-dispatch, gateway/DAPR, configured-store, query, deletion,
    replay, performance, production-boundary, and anti-vacuity assertions at equal or greater
    strength. Separate historical proof validity from current readiness instead of deleting the
    assertions that expose the time-basis defect.
  - [ ] Harden Git execution: explicit argument lists; UTF-8; concurrent stdout/stderr draining;
    bounded timeout and process-tree termination; non-interactive, non-colored, non-paged output;
    cleared ambient `GIT_DIR`/`GIT_WORK_TREE`-style overrides; machine-safe `-z` parsing where path
    output is consumed.

- [ ] **Create one deterministic generator and schema for the additive v3 proof** (AC: 3, 4, 5)
  - [ ] Add a repository-owned generator under `_bmad/scripts/` with focused tests under
    `_bmad/scripts/tests/`; generate authoritative JSON plus a byte-exact Markdown projection. No
    candidate, hash, test count, path, status, or approval may be caller-authored prose.
  - [ ] Add a closed v3 schema. Reject unknown fields, path escape, duplicate dependency identities,
    incomplete run bindings, partial predecessor hashes, and a chain head without exact approval.
  - [ ] Bind the committed v3 candidate, current in-scope root source/test blobs with modes and
    SHA-256, the candidate's `references/Hexalith.EventStore` mode-`160000` gitlink, every run
    artifact and test binary/project identity required by the run, and both unchanged v2 artifact
    hashes.
  - [ ] Derive the changed-dependency list by comparing normalized v2 and v3 declarations. Each
    changed identity needs its previous/current value, owner, and rationale; unchanged and unrelated
    dependencies must not be fabricated as changes.
  - [ ] Keep generation reproducible: canonical ordering and serialization, stable diagnostics,
    JSON as authority, Markdown generated from JSON, and a verify mode that rejects projection drift.

- [ ] **Execute current functional proof lanes and generate v3** (AC: 4, 6)
  - [ ] Freeze an immutable committed candidate after the implementation tree and declared
    dependencies are final. A dirty worktree, later candidate movement, or a different EventStore
    gitlink invalidates the run and requires regeneration.
  - [ ] Rebuild the relevant projects cleanly with `-t:Rebuild` and
    `-p:SourceRevisionId=<candidate>`; do not reuse stale `--no-build` outputs.
  - [ ] Run fresh machine-readable evidence for deterministic dispatch, gateway/DAPR boundary,
    configured state-store end state, production query detail/list behavior, derived-state deletion,
    and full replay. Evidence must exercise the production path; mock/DI-registration prose is not a
    substitute.
  - [ ] Require all named tests to exist and actually run, with zero failed, skipped, and not-run
    cases. Bind each result to its candidate-built binary/project and verify the result is newer than
    the binary. Reject empty, missing, red, skipped, vacuous, duplicated, or mismatched runs.
  - [ ] Generate `projection-read-store-population-proof-v3.json`, its deterministic Markdown
    projection, and fresh versioned run artifacts without changing any v2 byte.
  - [ ] Obtain named release-owner approval for the final exact v3/head bytes only after generation
    and review. The planning approval of 2026-08-01 is not evidence pre-approval.

- [ ] **Implement current-head readiness and supersession behavior** (AC: 3, 5, 6)
  - [ ] Prefer a separate focused lifecycle validation type/file rather than continuing to enlarge the
    existing 937-line historical validator; share helpers only where doing so does not merge the
    historical and current time bases.
  - [ ] Traverse and validate the complete predecessor chain, then identify exactly one explicitly
    approved current head.
  - [ ] Compare the approved head only with its declared projection dependency set at the candidate
    being assessed. In-scope drift without a valid approved successor must fail exactly with
    `PROJECTION_PROOF_SUPERSESSION_REQUIRED`.
  - [ ] Prove unrelated root-gitlink or source movement does not invalidate v2 history and is not
    silently pulled into the current dependency set.
  - [ ] Add positive and negative tests for unchanged head, legitimate successor, declared drift,
    undeclared in-scope drift, unrelated drift, stale successor, duplicate/forked head, broken
    predecessor, and missing approval.

- [ ] **Coordinate the Story 6.3 manifest handoff without claiming its completion** (AC: 7, 8)
  - [ ] Supply Story 6.3's owner with the stable chain contract and final v2/v3 identities. The
    Story 6.3 owner regenerates the preservation manifest and binds v2 as historical and v3 as the
    one current head; generated JSON/Markdown must never be hand-edited.
  - [ ] Preserve Story 6.3's `in-progress` ownership and do not mark it complete from this story.
    Story 6.12 completion nevertheless waits until the regenerated manifest and
    `PreservationTraceabilityManifestValidationTest` enforce the v7 rule and pass.
  - [ ] Record the downstream Story 6.6 contract: validate the complete chain, consume both roles,
    and rerun v3's functional gates; v2 alone is never current evidence for a later candidate.

- [ ] **Fault-inject every lifecycle and time-basis boundary** (AC: 1, 2, 5, 6)
  - [ ] Reject changed protected v2/signed-v1 bytes; wrong historical root candidate; wrong/missing
    gitlink or submodule commit; wrong blob/path/mode/hash; and an unresolvable Git object.
  - [ ] Reject truncated/wrong predecessor hashes, missing predecessor artifact, cycles,
    duplicate/forked/no current head, multiple approved heads, approval for different bytes, stale v3
    candidate/dependency/run binding, and forged changed-dependency metadata.
  - [ ] Reject missing/red/skipped/not-run/zero-test/vacuous functional evidence and result/binary
    candidate mismatch.
  - [ ] Reject undeclared in-scope drift with `PROJECTION_PROOF_SUPERSESSION_REQUIRED`, while proving
    an unrelated gitlink move is non-interfering.
  - [ ] Assert exact restoration of every mutation fixture; fail the test itself if restoration does
    not return byte-identical content.

- [ ] **Run the completion gates and generate the final record mechanically** (AC: 1, 7, 8)
  - [ ] Recompute the full protected-byte hash set and prove it is unchanged.
  - [ ] Build the affected Release projects with zero warnings and zero errors using repository
    settings and `-p:UseHexalithProjectReferences=true`.
  - [ ] Run the focused projection-proof class, the Story 6.3 manifest-validation class, and the
    complete Conformance project with zero failed, skipped, or not-run tests. If Story 6.9 has landed,
    run both declared conformance tiers rather than inventing a hidden `6.9 -> 6.12` dependency.
  - [ ] Run any generator/schema unit tests and Markdown parity checks, plus all test projects made
    relevant by the final changed-file set.
  - [ ] At one immutable committed final candidate, execute Story 6.8's clean rebuild and fresh-test
    pipeline for every root-owned test project; generate one candidate-bound completion bundle,
    insert its Markdown block verbatim, set `file_list_commit`, and verify its digest. Never hand-copy
    totals, file paths, or commit identities into another status surface.
  - [ ] Confirm the final diff contains no production/public/package/baseline/v2/signed-v1/submodule
    content change and `submodule_promotions` remains `[]` unless separately approved authority
    explicitly changes scope.

## Dev Notes

### Authority and sequencing

The active authority is the append-only v7 overlay, not superseded Epic 6 prose. Story 6.2 remains
closed history; Story 6.12 repairs the validator's time basis and adds new current evidence. The
binding order is `6.8 -> 6.12 -> 6.3 completion` and `6.12 -> 6.6`; Story 6.6 remains last.
Story 6.9 independently precedes Stories 6.3 and 6.6, not 6.12. Do not invent dependencies on
Stories 6.9 or 6.10, though concurrent changes in shared validation files must be preserved.

This is evidence/test infrastructure only. It has no UX, interface, accessibility, deployment,
topology, IaC, production-source, public-contract, package-version, accepted-baseline, or submodule
content scope. Binding the existing EventStore gitlink is evidence, not a promotion; therefore the
faithful initial declaration is `submodule_promotions: []`.

### Non-negotiable historical identities

Keep these identities distinct:

| Meaning | Immutable identity |
| --- | --- |
| Story 6.2 accepted baseline | `29def441408becfbbbdc5c59b9af14a7717cb21f` |
| v2 source/test proof candidate | `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` |
| EventStore gitlink recorded by that candidate | `e645901928eed9759e28e1086f23dc96875c3ac3` |
| Story 6.2 final-record candidate / `file_list_commit` | `2971ab79efcf3ef11d4fba7b9139d7cae457a3f9` |
| Story 6.2 done commit | `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa` |
| Story 6.12 creation baseline | `331ec28eeb403c2d62375c384842f3eb1b95d78a` |

The finalized v2 JSON/Markdown bytes do **not** equal the proof-path blobs at candidate `856ee997...`
because those evidence documents were finalized in later commits. That is expected. Verify today's
protected v2 declaration bytes, then use the candidate and gitlinks declared inside it to resolve
the historical source/test/platform objects.

Protected SHA-256 values at story creation:

| Protected artifact | SHA-256 |
| --- | --- |
| Story 6.2 record, including generated final record | `1b87966f2b48d18c1f1d642e679febca26bbc591e8f270e6deb96393ea39034e` |
| v2 JSON | `b4bcdb5b181be66780f251ad8a3b563b7554e34e0fb4d255d46a3a17addfaf7c` |
| v2 Markdown | `58a26b85d7812ad513e8260a40f012f8de8214364db1e2a2ab78ffa407348c40` |
| v2 deterministic xUnit XML | `c8cbdb09e25548652735535d1b55e86d0f2ccc1c424747c305b339f6068d8f0d` |
| v2 gateway xUnit XML | `1286dfd3a70fc7e813ef691a64c066e2b86c14fbd730f6083253f235370d18cf` |
| v2 population xUnit XML | `755faa6623fad04e0930662f143a9197abc58efd795a8a67e4f1db91b804df7f` |
| signed-v1 report JSON | `062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe` |
| signed-v1 report Markdown | `aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd` |
| release-owner decision JSON | `8091f6c26251420242a491cad100472dc1604a7163cc9d8df51bb1c742844856` |
| OQ-2 decision JSON | `06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06` |

Recompute these values at implementation start. A mismatch is a blocker to investigate, never a
reason to rewrite the expected value casually.

### Candidate-aware Git-object model

For a root path, prove the candidate commit exists, resolve the exact tree entry with machine-safe
output, require its expected mode and object type, and hash the bytes read from the recorded blob.
Git's `<revision>:<path>` syntax addresses the object at that revision; it is not the current file.

For a platform path, first resolve the root's mode-`160000` entry at the recorded umbrella commit.
The entry's object ID is the only valid platform commit for that historical proof. Then resolve the
platform-relative path from that commit through the corresponding submodule object database. The
submodule worktree may be on a later commit and is irrelevant. Missing local objects are a strict
validation failure; do not fetch, initialize, update, or switch submodules implicitly from a test.

Use current worktree/candidate comparison only in the separate current-head guard and only for the
declared projection dependency set. Current readiness is never inferred by applying historical v2
hashes to today's whole repository.

### Current v3 and chain data model

Use the v2 structure as historical input, not as a file to edit. The v3 schema should make at least
these concepts explicit and closed: artifact/schema version; story; committed candidate; generation
time; predecessor artifact paths and full SHA-256; declared production/test/platform dependencies
with path/mode/blob hash or gitlink commit; normalized changed-dependency identities; functional run
artifacts with candidate-built binary/project bindings; result; and exact current-head approval.

The chain validator must derive graph properties from content. A standalone “current” string or the
highest filename is insufficient. Approval binds the exact head bytes and is recorded after the
evidence exists. The generator must not copy today's unrelated root gitlinks into the dependency
set merely because they are visible in `git ls-tree`.

### Current implementation pressure points

`ProjectionReadStorePopulationProofValidationTest.cs` currently has eight facts. Its historical
fact compares the v2 EventStore commit with `HEAD:references/Hexalith.EventStore`, another fact
requires the old candidate to describe current root gitlinks/source movement, and `ValidateBinding`
hashes live worktree paths. Those are the defects to replace. Its route/scenario, run-artifact,
performance, production-boundary, deletion, and replay assertions are valuable and must remain.

Story 6.3 already owns the preservation-manifest generator, schema, JSON/Markdown, Python tests, and
independent C# validation. Its v7 spec explicitly requires the full chain, v2 historical role, and
one v3 current head. Coordinate at that ownership boundary: Story 6.12 produces and validates the
chain; Story 6.3's owner regenerates its manifest from that chain. Do not hand-edit the generated
manifest or claim Story 6.3 completion.

### Expected file map

Exact names may be adjusted by the developer only when repository conventions demand it; preserve
the ownership and generated/read-only boundaries.

| Disposition | Path / area | Purpose |
| --- | --- | --- |
| UPDATE | `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` | Candidate-aware v2 history while preserving existing proof semantics |
| NEW (recommended) | a focused lifecycle validation type/file in the Conformance project | Chain/head/current drift and lifecycle fault injection without mixing time bases |
| NEW | `docs/adrs/0004-projection-proof-evidence-lifecycle.md` | Lifecycle and supersession decision |
| UPDATE | `docs/adrs/index.md` | Register ADR 0004 |
| NEW | `_bmad/scripts/generate_projection_read_store_population_proof.py` | Deterministic v3 JSON/Markdown generation and verification |
| NEW | `_bmad/scripts/tests/test_generate_projection_read_store_population_proof.py` | Generator/schema/Git-object/fault-injection coverage |
| NEW | `docs/release-evidence/projection-read-store-population-proof-v3.schema.json` | Closed v3 contract |
| NEW | `docs/release-evidence/projection-read-store-population-proof-v3.json` | Authoritative successor proof |
| NEW | `docs/release-evidence/projection-read-store-population-proof-v3.md` | Deterministic reviewer projection |
| NEW | versioned v3 machine-readable run artifacts under `docs/release-evidence/` | Fresh functional lane results |
| POSSIBLE UPDATE | `tests/README.md` or an existing evidence runbook | Reproducible generation/validation commands only if no current location covers them |
| COORDINATED 6.3 OUTPUT | preservation-manifest generator/schema/JSON/Markdown/C# validation | Regenerated by Story 6.3 owner from the finished chain; never hand-edited |
| READ ONLY | Story 6.2 record, ADR 0003, all v2 proof/run artifacts, all signed-v1 evidence | Protected historical bytes |
| OUT OF SCOPE | `src/**`, public contracts, package files, accepted baselines, `references/**` content | Explicitly prohibited |

Do not add a separate mutable “current head” pointer unless ADR 0004 proves why it is necessary and
how its exact approval is hash-bound. Prefer deriving the one current head from closed, immutable
chain records and the generated preservation manifest.

### Testing and completion commands

Use the repository's xUnit v3 executable pattern, not a VSTest filter that may silently discover
nothing. After a Release build, the focused shape is:

```bash
tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests \
  -class Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest \
  -noLogo

tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests \
  -class Hexalith.Conversations.Conformance.Tests.PreservationTraceabilityManifestValidationTest \
  -noLogo

tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests \
  -noLogo
```

Use `-trx <repo-contained-path>` for fresh machine-readable completion evidence. Build with
`-p:UseHexalithProjectReferences=true`, `/nr:false`, and `/m:1`; at the final candidate also pass
`-p:SourceRevisionId=<candidate>`. The Story 6.8 generator, rather than this prose, determines the
complete root-owned project set and final measured totals.

### Libraries and standards

No dependency update is authorized. The repository pins .NET SDK `10.0.302`, xUnit v3 `3.2.2`,
Shouldly `4.3.0`, NSubstitute `6.0.0`, and Microsoft.NET.Test.Sdk `18.8.1`. Use the BCL and current
repository packages. `ProcessStartInfo.ArgumentList` provides explicit argument passing; retain the
repository's concurrent pipe-drain and timeout discipline when invoking Git.

The Git object-resolution design was checked against current official documentation: revision-path
object naming, `git ls-tree` mode/type/object output with `-z`, and `git cat-file` existence/blob
access all support validation without checking out history. These built-in capabilities are enough;
do not introduce a Git library or package-version change.

### Project structure notes

- `_bmad-output/**/*.md` and YAML use LF per `.gitattributes`.
- C# follows the shared `references/Hexalith.Builds/.editorconfig`; preserve copyright headers,
  namespace/style conventions, nullable settings, and analyzer-clean Release builds.
- Python tooling under `_bmad/scripts/` follows the adjacent deterministic generator pattern:
  standard library where practical, parseable diagnostics, canonical output, and focused tests.
- Preserve the unrelated untracked
  `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01.md`; it was created by
  another session and is not Story 6.12 work.

### Project Context Reference

Follow `_bmad-output/project-context.md` throughout implementation. In particular: target .NET 10,
use the repository-pinned xUnit v3/Shouldly/NSubstitute stack, keep tests deterministic and
fail-closed, preserve public/API and package boundaries, and require executable evidence rather than
claims. Repository and v7 authority are more specific where this story defines candidate-bound Git
object handling and proof lifecycle rules.

### Previous-story and Git intelligence

Story 6.8's record is the strongest adjacent implementation precedent, but it is not complete yet.
Its durable lessons are: committed candidates only; clean rebuilds; fresh result artifacts bound to
candidate binaries/projects; full SHA-256; generated records inserted verbatim; no hand-restated
counts/paths/commits; fault-injected guards; and no silent expansion of submodule promotion scope.

Recent history establishes the relevant spine:

- `331ec28` publishes v7 planning and Story 6.12 authority.
- `1123436` establishes the deterministic preservation-manifest generator/schema/test pattern.
- `e480c3f` closes Story 6.2 without reopening its evidence.
- `2971ab7` establishes candidate-bound test-binary evidence.
- `fde50d3` re-anchors generated final-record handling.

Concurrent root commits or gitlink moves can invalidate a proposed v3 candidate even when Story 6.12
did not cause them. Re-read the committed candidate and working tree immediately before generating
evidence and immediately before Story 6.8's completion record.

### References

- Story authority: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:1368-1518`
- Architecture v7: `_bmad-output/planning-artifacts/architecture.md:188-228`
- Approved correction: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md:108-136`,
  `:198-319`, `:420-442`
- Derived context: `_bmad-output/implementation-artifacts/epic-6-context.md`
- PRD FR-20 and contract boundary:
  `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md:310-324`, `:350-359`
- Story 6.3 contract:
  `_bmad-output/implementation-artifacts/spec-6-3-create-complete-preservation-traceability-manifest.md:63-86`
- Previous implementation record:
  `_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md`
- Current historical validator:
  `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs`
- Current manifest validator:
  `tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs`
- ADR conventions: `docs/adrs/0000-template.md`, `docs/adrs/0003-operator-delivery-mode-governance.md`,
  `docs/adrs/index.md`
- Git revision/path objects: https://git-scm.com/docs/gitrevisions
- Git tree modes and machine-safe output: https://git-scm.com/docs/git-ls-tree
- Git object access: https://git-scm.com/docs/git-cat-file
- .NET process argument handling:
  https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0
- xUnit v3 command line: https://xunit.net/docs/getting-started/v3/cmdline

## Dev Agent Record

### Agent Model Used

_To be populated by the implementation agent._

### Debug Log References

_To be populated by the implementation agent._

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created

### File List

_To be generated mechanically at the final committed candidate through Story 6.8. Do not hand-author
or hand-copy this list._
