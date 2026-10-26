from pathlib import Path
import json,hashlib,re,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation'
load=lambda p:json.loads(Path(p).read_bytes());sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def count(label):
 p=base/label;cmd=load(p/'command.json');assert cmd['exitCode']==0 and cmd['sourceUnchanged'];x=ET.parse(p/'result.xml').getroot();s=list(x) if x.tag=='testsuites' else [x];d={k:sum(int(v.get(k,0)) for v in s) for k in ('tests','failures','errors','skipped')};assert all(d[k]==0 for k in ('failures','errors','skipped'));return d['tests'],float(re.search(r'\b\d+ passed.*? in ([0-9.]+)s',(p/'stdout').read_text()).group(1))
combined,combined_time=count('combined-final-order');legacy,legacy_time=count('legacy-final-order');focused,_=count('focused-final-order');assert legacy==72 and focused==131
mutations=load(base/'mutations-final-order/mutation-probes.json');assert len(mutations)==23 and all(m['exitCode']==1 and m['tests']['failures']>0 and m['tests']['errors']==m['tests']['skipped']==0 for m in mutations);failures=sum(m['tests']['failures'] for m in mutations)
native=load(base/'native-capture-fixture-final-order/provenance.json');helper=load(base/'helper-toolchain-final-order/provenance.json')
text=f'''

### Worker verification — iteration 13

The iteration-13 implementation is complete for parent audit and acceptance.
Runtime entry and owner approval remain held. Evidence is indexed in
`module/validation/epic-16-prerequisite-implementation/review-iteration-13/verification.json`;
its run entries explicitly name each completed source epoch.

- Restored the 42 owned files from the hash-verified preservation manifest before
  editing. All 12,245 pre-existing evidence files and all 3,746 indexed iteration-12
  artifacts remain unchanged. The concurrent user commit/gitlinks, five protected
  files and `-whitespace` rule are preserved; the real index remains empty.
- Constructor admission validates selected constant kinds/encodings, genuine
  Decimal/DateTime defaults and complete fixture-provider constructor/value blobs.
  The 13 parent constructor positives remain valid; the nonnull nullable-enum
  default rejects as actual xUnit requires. Parent R13P2 corrected an invented
  decimal sign restriction: every nonzero sign is negative. Parent R13P3 corrected
  invalid Nullable<String>/Object admission through one central supported
  non-nullable value-type constraint, including optional/nonnull paths and
  array/nested/byref-like exclusions. Actual-runner positives preserve supported
  primitive, enum, local value, Decimal and DateTime forms. The inspector remains
  a bounded metadata protocol and never executes supplied evidence.
- Task/ValueTask return signatures retain their required class/value encoding.
  All five scenarios now bind nine loaded implementation owners, including
  EventStore.Server's reconciler/retry worker and Commons.TenantAccess's evaluator.
  Byte digest, MVID, owner revision/gitlink, required declarations and shared
  five-scenario inventory are checked. The original five upstream API preservation
  seams remain separate from implementation ownership. All three contracts and
  producer instructions match the nine-product capture schema.
- Final generation rereads exact raw TRX bytes and rejects digest drift or aliases
  before writing output. Native paths remain separately required publication
  paths. The fixed helper's exact evidence-rejection response remains FAIL;
  host failures become HelperUnavailable/BLOCKED with bounded real streams/exit.
  Scoped binary Git attributes cover retained apphosts/PDB/key/cache bytes and
  earlier DLL shapes while source/schema/TRX remain readable. Nine actual archived
  binary shapes produce standard normal Git diffs with exact bytes preserved.
- Final frozen verification: **{combined:,} combined passes** in
  **{combined_time:.2f}s**, **72 legacy passes** in **{legacy_time:.2f}s**, and
  **131 focused passes**, all without failures/errors/skips. All 2,879 prior
  combined case identities, 72 legacy identities and 19 packaging controls are
  preserved. All 86 source/test/policy inputs remain unchanged, with 71 bound
  source paths. Final **23 guard removals** cause **{failures} assertion failures**
  with zero setup errors/skips; every failed case passes on final exact source.
- Genuine pinned xUnit publication retains {native['publicationPathCount']} exact
  publication paths, {native['retainedAssemblyCount']} PE binaries, five captures,
  five untouched raw TRX files and {len(native['retained'])} retained committed
  files in total. All captures bind the same nine synthetic product stand-ins.
  Fresh-checkout regeneration/insertion succeeds after original build/measurement
  paths disappear. Exact candidate/publication/lifecycle commit and tree bytes use
  reversible base64 JSON, with raw hashes/Git object IDs verified; retained blob
  files match their Git objects. Ordinary diffs remain readable. These synthetic
  fixtures confer no actual product/runtime authority. Helper provenance retains
  SDK {helper['sdk']} and runtime {helper['runtime']['frameworkDescription']}.
- Exploratory and interrupted epochs are retained honestly. Early mutation setup
  used a wrong decimal blob length and ambiguous byte offsets; metadata offsets
  replaced those guesses. A schema-helper lookup error was corrected. The first
  trailing-signature mutation shared a source-stamp signature and survived; the
  corrected Type-constructor probe fails the intended assertion. Pre-R13P2/P3
  green selections retain their incomplete protocol interpretation. The initial
  combined run was stopped at the parent's request after 104 passes for R13P3;
  an accidentally overbroad historical-generator selection was stopped after
  199 passes, then the exact 72-case selector passed. The first nested-nullable
  mutation needed a two-byte coded handle; its final equal-width probe now yields
  the intended rejection assertion. None of these partial/pre-fix runs is
  presented as final verification. The next full run retained1127 passes and one
  obsolete expected-gitlink-order failure before interruption. The isolated case
  confirmed production evaluation passed; only the test tuple was corrected to
  Commons/EventStore/Tenants lexical order. Its prior130 focused/native/parent
  epochs remain unchanged, with the one-line source delta explicitly recorded.
  No prior artifact was rewritten.
- All eighteen actual held runtime/final-record commands still return
  **BLOCKED / exit 2 / ENTRY_QUALIFICATION_PENDING**; both completion guards and
  whitespace checks pass with operational bytes restored. The 54 runtime paths
  remain absent; 18 scenarios, 32 requirements, 38 nodes, 61 edges and rollback/
  predecessor obligations are unchanged. Entry is null, trust empty, accepted CLI
  4.35.2, Story16.1 draft/backlog and hold ACTIVE. Exact disposable commit messages
  passed pinned commitlint before use. No actual repository staging/commit/push,
  dependency update, runtime publication, child agent or workflow invocation ran.
  The initial accidental skill-manifest read remains documented in restoration.json.

Parent acceptance/review/completion remains outstanding. The prohibited broad
repository lane was not rerun; REFRESH_UV_DRIFT and Story10.4 guidance binding remain
unresolved and unweakened. Actual Platform integration, the seventeen adoption
findings, provider/environment parity, recovery drills, operator/deputy coverage,
capacity/waiver decisions, human SM-2 and separate owner entry remain required.
'''
paths=['_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md','_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md'];rows=[]
for name in paths:
 p=root/name;before=p.read_bytes();assert b'### Worker verification \xe2\x80\x94 iteration 13' not in before;
 with p.open('ab') as stream:stream.write(text.encode())
 rows.append(dict(path=name,beforeSha256=hashlib.sha256(before).hexdigest(),afterSha256=sha(p)))
(base/'handoff-update.json').write_text(json.dumps(dict(scope='Additive observed worker results; parent acceptance remains outstanding',documents=rows,combined=combined,legacy=legacy,focused=focused,mutationGuards=len(mutations),targetedFailures=failures),indent=2)+'\n');print(json.dumps(rows,indent=2))
