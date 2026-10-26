from pathlib import Path
import json
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10/implementation';commands=json.loads((base/'test-commands.json').read_bytes());assert len(commands)==2 and all(r['exitCode']==0 for r in commands);native=json.loads((base/'native-capture-fixture/provenance.json').read_bytes());combined=commands[0]['counts']['tests'];legacy=commands[1]['counts']['tests'];assert combined==2152 and legacy==72
paragraphs=f'''### Re-derived implementation verification — review iteration 10

All authorized prerequisite implementation tasks are implemented and verified.
The parent retains independent review and completion orchestration. Exactly 40
owned files were hash-verified before restoration; the new TestConstruction.cs
brings the implementation inventory to 41. The parent-owned narrow .gitignore
packaging rule brings the task inventory to 42 files. All five protected inputs and all 879
prior accepted artifact hashes remain unchanged. The final epoch freezes 85 files
and 71 bound sources; none changed during the final verification sequence.

- Candidate upstream API evidence now identifies the exact product bytes/MVID and
  revision admitted from the same native execution. All five measured scenarios
  must use one consistent inventory of seven owned implementations. Positive
  fixtures deliberately reuse retained artifacts, including after compiler-cache
  clearing. Root Contracts source equality remains strict; both public-surface
  scenarios additionally bind baseline/current compiled declarations and candidate
  loaded bytes. Actual external Compile removal and generated-source additions
  fail despite unchanged complete Contracts source inventories.
- Supported signatures retain assembly name, version, culture, public key/token
  and flags, alongside class/value kind and generic-constraint custom attributes.
  Native signed-reference and nullable-constraint controls retain exact binaries,
  build streams and actual caller diagnostics. Comparisons remain conservative
  declaration preservation, with explicit unsupported-shape failures; they do not
  claim general binary or implementation-behavior compatibility.
- Metadata-only constructor admission resolves pinned xUnit output/context helpers,
  local class/collection/assembly fixtures, supported inheritance and default,
  optional and param-array arguments. Missing required providers reject. Actual
  pinned-runner controls cover 29 supported and failing shapes. Parent R10P1 found
  that distinct inherited collection attribute types survived reflection despite
  the inspector's nearest-declaration shortcut. The correction suppresses only
  the same concrete attribute type; inherited distinct types reject, while valid
  overrides and inherited-only providers pass. Discovery-failure controls retain
  bounded eight-second diagnostics, never a claimed successful runner exit. Static
  metadata does not prove arbitrary constructor-body behavior.
- Unauthorized/nonexistent query captures retain actual executed requests and
  correlated public ConversationListResult.Hidden content. Both results must be
  identical and non-disclosing, with the existing Forbidden/forbidden vocabulary,
  empty summaries/page, expected safe action, zero index reads and zero repairs.
  Correlation, execution, order, content leakage and equal malicious-response
  controls fail. Five retained current contract sources anchor the closed protocol.
- Native test stamps accept both genuine SDK informational-version forms. TRX
  definitions require exactly one TestMethod and Execution. Lazy generator imports
  classify only genuinely absent declared modules as unavailable; internal import
  defects propagate. Historical and standalone behavior remains intact.
- Final combined verification: **{combined:,} passed** in
  **{commands[0]['elapsedSeconds']:.2f}s**, including all 2,038 prior cases plus
  114 new cases (1,990 successor and 162 historical adoption-v2). Final legacy
  verification: **{legacy} passed** in **{commands[1]['elapsedSeconds']:.2f}s**.
  Both runs have zero failures/errors/skips. Exact commands, environment, separate
  streams, JUnit and source hashes are retained in test-commands.json.
- **20 distinct guards are demonstrated across 24 recorded mutation runs**:
  **54 targeted assertion failures and zero setup errors/skips**. One intermediate
  inherited-ambiguity removal is explicitly insensitive because differing fixture
  providers still triggered the independent missing-provider check. Three genuine
  conflicts with a shared provider isolate the ambiguity rule and all fail under
  its removal. The affected constructor guards were rerun after R10P1; unchanged
  guards retain their exact prior source epoch. Every failed mutation case passes
  on final source. Parent pure controls pass 79/79 plus nine import boundaries;
  original same-level/inherited conflict PEs independently reject.
- Failed exploratory epochs remain intact: 17 passes/15 source-binding failures
  before adding the new C# file's byte-preserving Git attribute; then 80 passes/six
  namespace-mismatched fixture failures before correcting test namespaces. The
  corrected native selector passed 27; the affected selector passed 124; final
  pre-R10P1 controls passed eight. The first frozen combined run was intentionally
  interrupted for R10P1 after 578 completed passes, with actual uv exit 1 and no
  assertion failures. That incomplete run and its original 85-file snapshot remain
  archived. The corrected 26 native cases and three shared-provider controls pass.
  Exact exploratory commands were recovered from this task's execution transcript;
  the first two exploratory epochs have no complete frozen source inventory and
  are not presented as final-source evidence.
- Genuine xUnit 4.0.2 execution retains untouched raw TRX and seven explicitly
  synthetic product stand-ins. The final publication derives **{native['publicationPathCount']} paths**,
  including **{native['retainedAssemblyCount']} PE binaries**, from the captured inventory and root
  baseline/current compiled proof. Fresh-checkout regeneration and insertion pass
  after original measurement/build/payload locations disappear. All
  **{len(native['retained'])} exact committed fixture artifacts** and their candidate/publication/
  lifecycle revisions are retained. Fixture trust and products convey no real
  runtime or owner authority. Final helper provenance confirms approved SDK
  10.0.402 and actual .NET 10.0.13, with unchanged global.json.
- All eighteen actual runtime/final-record commands remain nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent
  (51 files and three binary directories). Both exact Story 7.3 guards and
  git diff --check pass, with prior operational bytes restored. All eighteen
  scenarios, thirty-two requirements, thirty-eight graph nodes, sixty-one edges,
  rollback obligations and predecessor pins remain preserved. Entry is null,
  trust empty, accepted CLI 4.35.2, Story 16.1 draft/backlog and hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-10/verification.json`.
  Exact disposable commit messages were validated with the pinned commitlint
  before use, including commit-tree fixtures. No skill was read or rendered, no
  secondary workflow, subagent or completion orchestration ran, and no actual
  repository staging/commit/push, dependency update, application start or runtime
  publication occurred. Parent review remains separate.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration qualification,
  seventeen adoption findings, provider/environment parity, recovery drills,
  operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate owner
  entry remain outside this prerequisite implementation.
'''
spec=root/'_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md';s=spec.read_text();assert '### Re-derived implementation verification — review iteration 10' not in s
for number in (2,3,4,5):s=s.replace(f'{number}. [ ] ',f'{number}. [x] ',1)
spec.write_text(s+'\n\n'+paragraphs)
handoff=root/'_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md';s=handoff.read_text();title,body=s.split('\n',1)
current=f'''\n\n**Current disposition: iteration-10 prerequisite implementation and verification
complete; parent independent review and completion remain separate.**

Final verification passes **{combined:,} combined tests** and **{legacy} legacy checks**,
with zero failures/errors/skips and 85 unchanged frozen files. All prior 2,038/72
cases and 879 retained artifact hashes are preserved. Twenty distinct mutation
guards are demonstrated across 24 runs, with 54 expected assertion failures; one
insensitive intermediate probe is retained explicitly. Parent controls pass 79/79
and nine import boundaries. The interrupted pre-R10P1 epoch remains historical.

The consumer now correlates admitted product bytes across API proof and all five
scenarios, verifies unchanged root compiled declarations, preserves complete
supported reference/constraint identity, resolves supported native constructor
providers and concrete attribute inheritance, captures actual non-disclosing
unauthorized/nonexistent query responses, and tightens TRX/import admission.
Actual signed/nullable/build-input, constructor and portable-publication fixtures
are retained. These checks establish supported evidence declarations; they do not
prove arbitrary constructor behavior, general binary compatibility or product
runtime qualification.

All eighteen real commands remain BLOCKED/ENTRY_QUALIFICATION_PENDING, all 54
runtime paths are absent, both workflow guards pass, entry is null and trust empty.
Story 16.1 remains draft/backlog with ACTIVE hold; accepted CLI stays 4.35.2.
The [iteration-10 verification record](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/verification.json)
binds exact commands, source epochs, streams/JUnit, native artifacts and mutation,
parent, commitlint and preservation evidence. No skill/workflow/subagent or
completion orchestration ran in this handoff; no real Git mutation, dependency
update, application start or runtime publication occurred.

The prohibited broad lane was not rerun. Its REFRESH_UV_DRIFT and Story 10.4
binding failures remain unresolved. Actual Platform integration, seventeen adoption
findings, parity/recovery/coverage/capacity/waiver decisions, human SM-2 and separate
owner entry remain outstanding.

## Historical iteration-8 summary
'''
handoff.write_text(title+current+body+'\n\n## Iteration-10 implementation handoff\n\n'+paragraphs.split('\n\n',1)[1])
print('Updated prerequisite spec tasks 2–5 and additive handoff; runtime spec/sprint untouched.')
