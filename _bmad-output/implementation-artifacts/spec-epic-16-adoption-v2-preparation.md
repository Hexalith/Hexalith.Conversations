---
title: 'Refresh Epic 16 adoption prerequisites with accepted Platform CLI 4.35.2'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8925c9fef46026d53d15478e2a744b3604c38a98'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/module/README.md'
  - '{project-root}/_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md'
---

<frozen-after-approval reason="user approved the recommended additive prerequisite refresh on 2026-10-25">

## Intent

Refresh the Epic 16 preparation against current sources and Platform's accepted CLI 4.35.2 so owners can assess current enrollment results. Old checkers now fail source drift; the old APP_API_TOKEN schema defect has an upstream successor. Produce an additive review package and accurate observations, retaining the Story 16.1 runtime hold.

## Boundaries & Constraints

Preserve prerequisite/adoption v1 packages, schemas, verifier bytes, original observations, signed evidence, graph/assertions, manifest and hosting inventory. Merge only Platform's exact accepted tool entry, preserving unrelated tools. Use neutral contained reads, strict JSON, independent source/schema pins, exact inventories, and explicit PASS/FAIL/BLOCKED distinctions. Root dependencies stay at recorded revisions. Structure PASS never grants runtime authority; hold ACTIVE and all four authority flags false.

No runtime code, generator dispatch, measured runtime verifiers, deployment, host retirement, owner acceptance, historical authority publishers, staging, commits, pushes, submodule updates or nested initialization. Keep runtime story draft/backlog. Restore/version and read-only native validation are authorized. New native outputs go only under a fresh v2 directory; retain failures and disclose tracked-source-only scan coverage and working-tree provenance. This refresh does not qualify source/package/hosted parity, SM-2 or AD-5/AD-6.

## I/O & Edge-Case Matrix

| Input/state | Required behavior |
| --- | --- |
| Current preserved package | Nonempty structural PASS; entry BLOCKED, authority false |
| Native ordinary/adoption validation | Retain exact command, version, exit, outputs and failed rows; distinguish schema compatibility from adoption |
| Rehashed weakened carrier, truncated/duplicate inventory or granted authority | FAIL with stable diagnostic |
| Missing input/tool or unsupported secure reads | BLOCKED with named input; no fallback or execution |
| Malformed JSON, path/symlink escape, nonregular input | FAIL safely without outside reads/writes |
| V1 reproduction after current drift/tool alignment | Portable pinned historic fixture passes original assertions; current v1 CLI remains truthful |

</frozen-after-approval>

## Code Map

- `.config/dotnet-tools.json`: 4.35.1; direct Platform accepts 4.35.2 with rollForward false. Read ordinary `references/Hexalith.Platform/docs/domain-module-enrollment.md`; never load reference skills.
- `_bmad/scripts/verify_epic_16_adoption.py`: immutable v1 checker, strict parse/expected commands, source/artifact pins and preserved-contract comparisons. Reuse safe helpers, not its stale current-source bindings.
- `_bmad/scripts/contained_reader.py`: neutral descriptor-relative reader; `evidence_boundary_current.digest` and generator helpers provide hashes/Git facts. Avoid parallel home-grown infrastructure.
- `epic-16-adoption-v1/` under planning: full preservedContract wrappers for 18 scenarios; inventories have 32 requirements and graph 38 nodes/61 edges. V2 may change wrapper identity/command paths only; all inherited semantics stay equal.
- Existing adoption fixture copies three changed root sources live. Exact old bytes at revision `bb6ae6e6574377e8fc628266f16b51a301f9736e` match v1 pins: sprint status, current-change runbook, generator. Existing reference snapshot is portable. Add pinned historic root/tool fixture bytes rather than relaxing verifier pins or fault assertions.

## Tasks & Acceptance

- [x] `.config/dotnet-tools.json`, `module/validation/epic-16-adoption-v2/` — align accepted entry; restore/version and measure ordinary plus both adoption modes. Record accurate scoped provenance and native failures without rewriting old outputs.
- [x] `_bmad-output/planning-artifacts/epic-16-adoption-v2/` — add package, README, three wrappers, inventories and graph; independently bind current inspected sources, exact old predecessor, new observations and unchanged obligations/decisions. Describe remaining blockers and historical v1 drift. Exclude bundle from its own digest and mutable specs from artifact fingerprints.
- [x] `_bmad/schemas/epic-16-adoption-v2.schema.json`, `_bmad/scripts/verify_epic_16_adoption_v2.py` — add read-only strict structural validation with independently pinned schema/source semantics and observation bindings, no manifest-command execution or arbitrary writer. Current schema must accept the real SDK token key before claiming the old incompatibility resolved.
- [x] `_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py`, `tests/fixtures/` beneath the scripts directory, existing `test_verify_epic_16_adoption.py` — cover every matrix row, rehashed weakening, literal false flags, CLI exit meanings and submodule-free positive/fault fixtures. Restore historical v1 tests with exact pinned fixture inputs; keep real v1 checker unchanged.
- [x] `module/README.md`, runtime draft and this spec — point to additive results, preserve prior history and record remaining runtime/owner blockers. Leave sprint status untouched.

**Acceptance Criteria:**

- Given accepted 4.35.2, when the native commands run, then owners see exact current results and whether the APP_API_TOKEN defect is resolved, with no invented adoption success.
- Given intact v2 sources, when verification runs, then structure passes with a nonempty ledger while entry remains BLOCKED and runtime publication is explicitly not-applicable.
- Given altered inputs, when the focused tests run without initialized references/history, then each required fault fails or blocks distinctly, and old preparation bytes remain unchanged.

## Implementation Notes

2026-10-25: Merged only the exact direct Platform accepted CLI entry (4.35.2,
rollForward false). Restore/version passed with
`4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. Fresh ordinary and both
adoption observations still exit 1 (`UsageOrManifest`): the published binary
rejects both real `APP_API_TOKEN` fields with HXP002. Current pinned Builds
source schema accepts the unchanged manifest/token; these are separate facts.
Published-tool compatibility remains BLOCKED. All 24 adoption rows are retained:
six PASS, eighteen FAIL (declaration, fourteen selectors, three Dapr findings).

The [v2 review package](../planning-artifacts/epic-16-adoption-v2/README.md)
binds 63 current/preserved source inputs, 25 artifacts and a 30-path review
boundary. Full v1 carrier equality permits only new wrapper identity/command
paths; all eighteen scenarios, thirty-two requirements, graph and seven pending
decisions remain unchanged. The neutral contained reader and strict JSON parser
are reused. No old checker is run as current authority and no generator dispatch
is added. The package excludes itself and mutable specs from artifact hashes.

The [native provenance](../../module/validation/epic-16-adoption-v2/provenance.json)
captures observation-time HEAD/status, twelve unchanged direct gitlinks/checkouts
and 1,047 tracked source candidates. This is bounded tracked-source coverage and
a working-tree snapshot preceding the completed additions; owners must rerun
both modes on the intended reviewed/tracked candidate. Source/package/hosted
parity, AD-5/AD-6, SM-2, runtime and owner entry remain unqualified.

Historical fixture repair uses exact sprint/runbook/generator bytes from
`bb6ae6e6574377e8fc628266f16b51a301f9736e` and original root tool/README bytes,
all checked against unchanged v1 pins. Current reference inputs use a separate
portable snapshot. No v1 verifier, schema, package or observation bytes changed.
Pre-existing runtime draft edits are preserved; it stays draft and sprint
status is unchanged. Scoped runtime-publication evidence is not-applicable.

## Spec Change Log

2026-10-25: Recorded the observed published-tool/source-schema mismatch without
changing the approved frozen scope or granting runtime authority.

2026-10-25 review fixes: Bound scoped checkout policies in `.gitattributes`
without changing old artifact bytes; LF inputs and raw native/CRLF bytes now
survive automatic conversion. Escaped CLI diagnostics for surrogate inputs,
exposed observation root-schema validation, and documented locked Python
bootstrap plus exact provenance digest serialization. Native measurements remain
unchanged.

## Review Triage Log


| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| Blind 1: newline conversion of pinned files | medium / patch | Reproduced checkout with core.autocrlf=true changes tool, module README, new schema and native result bytes. Add scoped byte-preserving attributes and a round-trip regression; include policy in the exact reviewed/bound package. |
| Blind 2: invalid Unicode diagnostic | medium / patch | A package artifact path containing an unpaired surrogate returns exit 1 with empty stdout and UnicodeEncodeError. Escape JSON diagnostics and cover the real CLI. |
| Blind 3: fresh environment bootstrap | low / patch | Documented bare Python and uv --no-sync assume dependencies already installed. Add locked bootstrap and a consistent pinned-environment invocation. |
| Blind 4: observation schema root | medium / patch | The checker admits results.json through its observation definition, but the published root rejects that same supported input. Expose the existing definition at the root and test it. |
| Blind 5: provenance serialization | low / patch | The captured path digest uses sorted UTF-8 paths joined with LF and a trailing LF; document that procedure for owners reproducing the snapshot convention. |
| Blind 6: native artifact/DLL hash | false / reject | This preparation claims the exact restored version and raw observed outputs, not package/DLL or embedded-schema qualification. Those facts are retained; no incorrect binary identity is demonstrated. Runtime/package qualification remains explicitly pending. |
| Blind 7: current scan applicability | false / reject | Reports label the data observedStaticValidation, retain sourceRevision and LIVE_TOOL_RECHECK_REQUIRED, and docs explicitly disclose observation-time tracked-only coverage before later additions. Structural PASS is not claimed to certify current scanner coverage or runtime entry. |
| Blind 8: provenance semantics | false / reject | Independently recomputed 1,047 unique sorted candidate paths, count and trailing-LF UTF-8 digest; all match the captured facts. Twelve raw gitlinks/checkouts were independently checked. No incorrect capture is demonstrated, and fixed pins reject altered provenance. |
| Blind 9: timestamp validation | false / reject | Every recorded timestamp is valid UTC with +00:00; independently pinned observation bytes reject fabricated dates even after declared rehashing. No malformed date reaches structural PASS; general timestamp policy is not required for this fixed snapshot. |
| Blind 10: output report schema | false / reject | No consumer-facing output schema was promised and no failing consumer is identified. The documented JSON/exit meanings, nonempty ledger and literal authority flags have direct real-CLI tests. A new general output contract would expand this preparation. |

The edge-case reviewer returned no findings. The verification-gap reviewer returned no gaps.

## Verification

Run the individual native commands above and the v2 verifier. Run `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py`; exercise a portable copy with references/history absent. Verify historic artifact equality, exact actual change scope and `git diff --check`. No .NET runtime build is needed for tool-pin/preparation-only work. Scoped runtime-publication evidence is not-applicable; do not invoke retired publishers/final records. Baseline focused results were 134 passed/34 failed before this refresh.

2026-10-25 implementation verification before review fixes:

- The five exact native commands in [results.json](../../module/validation/epic-16-adoption-v2/results.json)
  returned 0/0/1/1/1. Separate stdout/stderr, hashes and UTC start times are
  retained in the fresh v2 directory; all failures remain failures.
- `uv run --frozen --no-sync python3 _bmad/scripts/verify_epic_16_adoption_v2.py --repository .`
  returned exit 0, structural PASS with 130 assertions; entry BLOCKED, hold ACTIVE,
  all four authority flags literal false, runtime-publication not-applicable.
- The exact focused three-suite pytest command above returned exit 0:
  **326 passed** (168 preserved suites plus 158 v2 cases). Coverage includes
  preserved/current facts, rehashed weakening, exact inventories/graph,
  false-like authority rejection, safe-read failures and CLI exits 0/1/2.
- A portable copy at `/tmp/epic16-portable-fvo2yjix` had no `.git` or live
  `references/`. Running the same three explicit test files with the existing
  pinned environment's Python returned exit 0: **326 passed**. Fixture creation
  reads only root-owned files and checked-in byte snapshots.
- Independent comparison with HEAD blobs proved byte equality for 30 preserved
  package/schema/checker/observation/enrollment/sprint/generator/fixture files.
  Exact changed plus untracked path equality matched all 29 review-boundary
  paths; twelve root gitlinks/checkouts and the accepted tool entry matched.
- The unchanged historical checkers still truthfully return FAIL against current
  sources: prerequisite 75 assertions (`SOURCE_BYTES_DRIFT`), adoption 45
  assertions (`SOURCE_BYTES_DRIFT`, `SOURCE_BINDING_INVALID`). This is distinct
  from their passing historical positive/fault fixtures.
- `git diff --check` returned exit 0. No runtime build/start, deployment,
  historical publisher, final record, staging, commit, push or submodule update
  was performed.

2026-10-25 targeted review-fix verification:

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py`
  returned exit 0: **162 passed**. Only this edited suite was rerun; the complete
  and portable full-suite checks above remain the earlier observations.
- New regressions cover disposable Git index/checkout conversion with
  `core.autocrlf=true` and `core.safecrlf=true` (including preserved CRLF),
  independently pinned checkout policy, a real-CLI surrogate-path FAIL/exit 1,
  and `results.json` as a published root-schema entry point. The policy adds two
  structural assertions, bringing the current passing ledger to 132 and the
  exact inventories to 25 artifacts and 30 review paths. Git staging in this
  regression is confined to its disposable fixture, with no commits.


2026-10-25 final verification after review patches:

- Five retained findings were corrected: byte-preserving checkout policy, escaped
  Unicode diagnostics, locked dependency bootstrap, observation schema entry,
  and documented provenance serialization. Five findings were rejected with
  evidence above; no work was deferred.
- The exact three-suite command returned exit 0, **330 passed**. The separate
  portable copy `/tmp/epic16-v2-final-portable-8075pz7r`, without Git history or
  live references, also returned exit 0, **330 passed**.
- V2 structural validation returns PASS/exit 0 with **132 passing assertions**,
  **25 bound artifacts** and exact **30-path** actual change equality. The
  original native observations and preserved root source bytes remain intact;
  all twelve raw-mode gitlinks and dependency checkouts match captured revisions.
- The disposable Git regression stages only its temporary fixture and proves
  clean/smudge byte equality with automatic conversion enabled, including the
  retained CRLF server entry point. Real workspace staging remains empty.
- Entry remains BLOCKED, hold ACTIVE, all authority flags false. Runtime-publication
  evidence is explicitly not-applicable, and this preparation has no dotted story
  contract/final-record transition. The Story 16.1 draft/backlog and runtime hold
  remain unchanged. Completion is limited to the approved prerequisite refresh.
- `git diff --check` passes. Changes remain uncommitted under the approved frozen
  preparation scope; no push, deployment, or runtime qualification is claimed.
