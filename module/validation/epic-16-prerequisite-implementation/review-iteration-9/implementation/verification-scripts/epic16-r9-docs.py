from pathlib import Path
import json,re
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation'
commands=json.loads((out/'test-commands.json').read_bytes());assert len(commands)==2 and all(r['exitCode']==0 for r in commands)
sequence=json.loads((out/'final-sequence-commands.json').read_bytes());assert len(sequence)==5 and all(r['exitCode']==0 for r in sequence)
native=json.loads((out/'native-capture-fixture/provenance.json').read_bytes());assert native['loadedProductCountPerCapture']==7
combined=re.search(r'(2038 passed[^\n]*)',(out/'combined.stdout.txt').read_text()).group(1)
legacy=re.search(r'(72 passed[^\n]*)',(out/'legacy.stdout.txt').read_text()).group(1)
summary=f'''

### Re-derived implementation verification — review iteration 9

All authorized prerequisite implementation tasks are implemented and verified.
The parent retains final artifact review and completion orchestration. Exactly
36 owned preservation files were hash-verified before restoration. All five
protected inputs, including global.json and the concurrent Story 5.4/deferred
ledger work, remain byte-identical. The frozen final epoch binds 84 source,
test and policy files; none changed during final verification.

- Required rejected replay operations now retain executed invalid native inputs,
  attempted writers and complete before/after persisted bytes. Missing stable keys,
  wrong order and generation before the event-derived anchor fail without commits
  or clamping. Valid zero lag and coincident generation instants remain supported.
- Warning removal derives from actual baseline/candidate Program.cs bytes after
  restart and convergence pass. Both the actual active warning and historical
  release disclosure remain untouched; future removal is tested in disposable
  candidate repositories.
- Portable capture version 2 binds all seven required loaded Conversations,
  Tenants and EventStore product assemblies to actual test output, exact retained
  PE bytes, managed name/MVID, full source revision and independently pinned owner.
  Root assemblies match the candidate; dependencies match raw direct gitlinks.
  Fixed PEReader/MetadataReader inspection follows the actual SDK informational
  version convention, without loading evidence assemblies. Source/version, route,
  policy and contract declarations are retained with hashes. Helper execution uses
  unchanged approved SDK 10.0.402 and actual .NET 10.0.13.
- Baseline/candidate public metadata for all five upstream assemblies derives from
  the owning revisions and actual bound binaries. Existing types and supported
  member/parameter/return/property/generic contracts must survive. Concrete and
  default-interface additions pass; new abstract implementor obligations fail.
  An ordinary member name containing Abstract remains valid. This conservative
  declaration comparison does not claim general binary compatibility. All eight
  final API fixtures retain exact native binaries, source and raw gitlinks; unsupported
  native marshalling/PInvoke/RVA/forwarded shapes fail explicitly.
- Reconciliation retains an ordered executed POST /project/v2/reconcile request,
  EventStoreWorkload:domain-service:project authorization, hosted ReconcileAsync
  dispatch, durable result and actual ProjectionDispatchResponse outcomes.
  HTTP 200 alone is insufficient: expected Completed/AlreadyCompleted outcomes
  must match the projection route and committed clearance. Transaction/clearance
  instrumentation is separate from response members. Wire diagnostics contain no
  event/state content; their state digest is recomputed from the separately
  validated complete recovery snapshot. Readiness emits closed bounded retry
  advice tied to the measured terminal condition and existing experiment policy.
- Query lifecycle/provider domains are closed before mapping. Component tenant,
  resource and dispatch references match the control index; missing required
  cross-key inventory yields structured failure while initialized-empty queries
  remain valid. All wrappers emit nonempty BLOCKED/exit 2 for missing declared
  Python dependencies. Final artifact derivation rereads retained capture/binary
  bytes and rechecks containment/no-alias constraints against admitted hashes.
- Final combined result: **{combined}** — 1,876 successor tests (all 1,588
  retained plus 288 new) and 162 historical adoption-v2 tests. Final legacy result:
  **{legacy}**. Both runs have zero failures/errors/skips; exact commands,
  separate streams, JUnit and hashes are retained in test-commands.json.
- **41 distinct guards across 43 removal runs produced 192 targeted assertion
  failures and zero setup errors/skips.** The initial 39 runs produced 175 failures.
  After R9P5, both affected API comparison guards and the two new class/value-kind
  guards produced 17 failures on final source. All original round9 control/helper
  function ASTs remain identical. Four older fixtures were also adapted across
  earlier epochs; their corrected selectors pass 5/5 and 4/4. Parent frozen-source
  probes and compiled reference-kind verification remain independently retained.
- Parent R9P5 exposed an actual decoder defect: a referenced parameter/return type
  changing from class to struct produced identical API metadata. SignatureNames
  now retains native class/value markers for definitions, references and specified
  signatures while declaration/attribute names remain separate and unprefixed.
  Ten added compiled cases cover six breaking parameter/return/property/field/
  generic/by-reference changes, two valid same-kind additions and two local-type
  declaration-name controls. The focused API selector passes 23/23. The original
  parent counterexample and all eight final external-type fixtures retain exact
  source/binary/build evidence. This is an existing requirement correction, not
  expanded runtime authority or a general binary-compatibility claim.
- The genuine pinned xUnit test loads seven explicitly synthetic source-stamped
  product stand-ins and retains untouched raw TRX. Its current publication is
  derived from capture contents: {len(native['publicationPaths'])} paths including
  {native['retainedAssemblyCount']} distinct PE binaries. Fresh-checkout regeneration
  and inserted-record verification pass after the original measurement root/build/
  payload locations disappear. Exact committed fixture bytes and candidate,
  publication and lifecycle revisions are retained. These fixtures grant no real
  product qualification. The historical 18-file proof remains unchanged.
- Intermediate epochs remain visible: the first expanded selector had 225 passes
  and five fixture failures, followed by 249 passes. The retained exploratory run
  was deliberately interrupted after 966 passes/three known fixture failures
  (278 deselected; exit 2); its progress percentages used all 1,588 selected cases.
  The first frozen combined then passed 2,027 with one nullable fixture failure;
  it is preserved in full. The fixture now recomputes its safe wire-state digest
  after changing the valid persisted nullable values. The next epoch passed
  2,028 combined and 72 legacy checks, then was superseded by R9P5. Its evidence
  audit script initially expected uppercase in the unchanged lowercase historical
  disclosure; that assertion was corrected and the failure retained. The final
  signature-aware combined JUnit is authoritative. Each correction and earlier
  failure is described in source-epoch-disposition.json, with recovered exact
  intermediate invocation lines and separate streams.
- All eighteen actual commands remain **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths are absent (51 files and
  three binary directories). Both exact Story 7.3 workflow guards and git diff
  --check pass, with previous operational bytes restored. All eighteen scenarios,
  thirty-two requirements, thirty-eight nodes, sixty-one edges, rollback obligations
  and predecessor pins remain preserved. Entry is null, trust empty, accepted CLI
  pins 4.35.2, and Story 16.1 remains draft/backlog with its hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-9/verification.json`.
  Disposable commit messages were checked by the pinned commitlint before use.
  The worker's initial extra bmad-build render, performed before reaching this
  handoff restriction, remains an explicit orchestration deviation; the parent's
  existing render stays authoritative. No secondary workflow steps, child workers,
  completion orchestration, actual repository staging/commit/push, dependency
  updates, application start or runtime publication followed.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4
  guidance-binding failures remain unresolved and unweakened. Actual Platform
  integration qualification, seventeen adoption findings, provider/environment
  parity, recovery drills, operator/deputy coverage, capacity/waiver decisions,
  human SM-2 and separate owner entry remain outstanding and outside this build.
'''
p=root/'_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md';s=p.read_text();assert '### Re-derived implementation verification — review iteration 9' not in s
for n in (2,3,4,5):s=s.replace(str(n)+'. [ ]',str(n)+'. [x]',1)
p.write_text(s+summary)
p=root/'_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md';s=p.read_text();assert '## Iteration-9 implementation handoff' not in s
p.write_text(s+f'''

## Iteration-9 implementation handoff

All seven amendment groups and parent source corrections are implemented. The
final frozen epoch contains 84 unchanged source/test/policy files. The required
replay negatives execute actual invalid inputs and preserve full persisted state.
The warning gate reads candidate Program.cs while actual active and historical
warnings remain unchanged. Capture v2 binds seven actual loaded product identities
and retained PE bytes to the candidate/direct gitlinks using a fixed non-executing
metadata inspector and the actual SDK source-stamp convention.

All five upstream API seams use independently bound native baseline/candidate
metadata. Concrete additions pass; breaking declarations and new abstract
implementor obligations fail. Names containing Abstract are ordinary names.
Parent R9P5 also proved that referenced class-to-struct signatures were collapsed.
The fixed decoder retains class/value kind separately from plain declaration names.
Six compiled breaking changes, two valid additions and two local-definition
controls now pass, with exact native source/build/binary fixtures retained.
The comparison is deliberately conservative and rejects unsupported native or
forwarded shapes; it is not a general binary-compatibility guarantee.

Reconciliation captures the real request route, authorization policy, hosted
dispatch and actual successful per-projection response outcomes, correlated with
durable clearance. Safe wire diagnostics carry only a recomputed persisted-state
digest and structural metadata; transaction/clearance instrumentation is identified
separately. Readiness emits measured bounded retry advice. Query domains/identities,
partial-installation imports and final retained artifact rereads/aliases are checked.

Final verification: **{combined}** (1,876 successor, including all 1,588 retained
plus 288 new, and 162 historical); **{legacy}**. Zero failures/errors/skips.
**41 distinct guards across 43 runs produce 192 assertion failures with zero
setup errors/skips**. Four final-source runs cover the two affected API comparison
guards and new definition/reference class/value-kind guards. Original round9
control/helper ASTs are unchanged; all intermediate source epochs remain explicit.
Parent probes and compiled reference-kind verification remain independent.
All intermediate failures and the deliberately interrupted 966-pass/three-failure
retained run and first frozen 2,027-pass/one-failure combined run remain separate
from the corrected final results.

Genuine pinned xUnit execution loads seven explicit synthetic product stand-ins.
Raw TRX survives unchanged, and the capture-derived {len(native['publicationPaths'])}-path
publication includes {native['retainedAssemblyCount']} retained assemblies. Fresh-checkout
regeneration/insertion succeeds after original measurement/build/payload locations
disappear. Exact committed bytes/revisions are retained. Fixture trust and products
remain synthetic and provide no actual runtime or owner authority.

All eighteen actual commands remain BLOCKED/exit 2/ENTRY_QUALIFICATION_PENDING;
51 files and three binary directories remain absent. Both exact Story 7.3 guards
and git diff --check pass. All protected user inputs, accepted pins 4.35.2,
predecessors and 18/32/38/61 scenario/requirement/graph obligations are preserved.
Story 16.1 remains draft/backlog with ACTIVE hold. Root HEAD/index are unchanged.

The [iteration-9 verification record](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/verification.json)
binds exact commands, streams, JUnit, epochs, mutations, source/toolchain evidence,
fixture artifacts, pinned commitlint validation and preservation checks. The extra
worker workflow render remains disclosed; the parent's original render is
authoritative. No secondary steps/workers/completion orchestration, real repository
stage/commit/push, dependency update, application start or runtime publication
occurred. The parent retains final artifact review and completion orchestration.

The prohibited broad lane was not rerun. REFRESH_UV_DRIFT, Story 10.4 guidance,
actual Platform integration/seventeen adoption findings, parity/recovery/coverage/
capacity/waiver decisions, human SM-2 and separate owner entry remain outstanding.
''')
print('Spec task acceptance and additive handoff updated from final observed evidence; lifecycle status remains parent-owned')
