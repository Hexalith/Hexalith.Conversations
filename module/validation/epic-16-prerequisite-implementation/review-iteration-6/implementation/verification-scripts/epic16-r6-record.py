from pathlib import Path
import hashlib,json,shutil,subprocess,sys,xml.etree.ElementTree as ET

root=Path('/home/administrator/projects/hexalith/conversations')
sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_tooling as tooling

base=root/'module/validation/epic-16-prerequisite-implementation'
review=base/'review-iteration-6';out=review/'implementation'
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def ref(path):return dict(path=path.relative_to(root).as_posix(),sha256=digest(path))
def load(path):return json.loads(path.read_bytes())
def write(path,data):path.write_text(json.dumps(data,indent=2)+'\n')
def git(repo,*args):return subprocess.run(['git',*args],cwd=repo,capture_output=True,check=True).stdout.decode().strip()
def counts(path):
    suites=ET.parse(path).getroot().iter('testsuite')
    rows=list(suites)
    return {key:sum(int(row.get(key,0)) for row in rows) for key in ('tests','failures','errors','skipped')}

commands=load(out/'test-commands.json');assert len(commands)==2 and all(row['exitCode']==0 for row in commands)
source=load(out/'verification-source.json')
assert digest(root/tooling.BINDING)==source['bindingSha256']
for row in [*source['binding']['sources'],*source['tests']]:assert digest(root/row['path'])==row['sha256'],row['path']
held=load(out/'held-commands.json');assert held['bindingSha256']==source['bindingSha256']
assert len(held['commands'])==18 and all(row['exitCode']==2 and row['blockers']==['ENTRY_QUALIFICATION_PENDING'] for row in held['commands'])
assert len(load(out/'completion-commands.json'))==2 and all(row['exitCode']==0 for row in load(out/'completion-commands.json'))
assert counts(out/'combined.xml')==dict(tests=1213,failures=0,errors=0,skipped=0)
assert counts(out/'legacy.xml')==dict(tests=72,failures=0,errors=0,skipped=0)

# Retain only the final run's genuine test fixture artifacts, with a path map.
# The .bytes suffix avoids repository-wide *.trx/artifacts ignore rules without
# altering the captured bytes or creating real runtime publications in this root.
candidates=[p for p in Path('/tmp/pytest-of-administrator').glob('pytest-*/test_round6_genuine_native_cap*/fresh-checkout') if p.stat().st_mtime>(out/'verification-source.json').stat().st_mtime]
assert candidates,'final fresh-checkout fixture missing'
fixture=max(candidates,key=lambda p:p.stat().st_mtime)
native_out=out/'native-capture-fixture';native_out.mkdir(exist_ok=True)
record=load(fixture/tooling.final_paths('runtime','16.2')[0]);candidate=record['candidate']['commit']
contract_path=tooling.contract_path('16.2');contract=load(fixture/contract_path)
publication=sorted([*tooling.final_paths('runtime','16.2'),*[fn('runtime',f'AC-16.2-0{n}') for fn in (tooling.native_path,tooling.receipt_path) for n in range(1,6)],*[row['path'] for row in record['retainedNativeArtifacts']]])
assert len(publication)==18 and len(set(publication))==18
transaction=git(fixture,'rev-parse','HEAD^');lifecycle=git(fixture,'rev-parse','HEAD')
assert set(git(fixture,'diff-tree','--no-commit-id','--name-only','-r',transaction).splitlines())==set(publication)
paths=publication+[contract['nativeProtocol']['runtimeTestSource'],contract_path,tooling.BINDING,'fixture-entry.json',tooling.spec_path('runtime','16.2')]
retained=[]
for path in paths:
    target=native_out/(path.replace('/','__')+'.bytes')
    target.write_bytes((fixture/path).read_bytes())
    retained.append(dict(fixturePath=path,**ref(target)))
for n in range(1,6):
    native=tooling.native_path('runtime',f'AC-16.2-0{n}')
    assert (fixture/native).read_bytes()==(fixture.parent/'original-unavailable'/native).read_bytes()
    captured=load(fixture/tooling.capture.capture_path('16.2',f'AC-16.2-0{n}'))
    assert captured['native']['sha256']==digest(fixture/native)
    assert not Path(captured['originalRepositoryRoot']).exists()
assert not (fixture.parent/'portable-build').exists() and not (fixture.parent/'payloads').exists()
assert not (fixture/'artifacts/measurement-only-runner').exists()
(native_out/'README.md').write_text('''# Disposable native capture interoperability evidence

These bytes come from the final combined pytest run's genuine xUnit 4.0.2 fixture.
Five deliberately generated Facts emit synthetic controlled observations. The
test injects its own entry/contract trust and public source baseline. This is
evidence of native capture, publication and relocation interoperability only;
it is not product runtime execution, owner approval or real runtime qualification.

The test executed its generated fixture assembly once to produce each raw TRX.
The consumer only inspected retained assemblies with its fixed trusted helper.
It committed exactly eighteen evidence paths, committed the separate insertion,
cloned a fresh checkout and direct dependency, hid the original repository and
deleted external build/payload outputs. Generation then reproduced the exact final
pair and the actual insertion verifier returned PASS. A captured-byte mutation
failed. The final passing JUnit case and source/test hashes bind those assertions.

Files use a `.bytes` suffix to preserve exact bytes without the workspace's
global TRX/artifacts ignore patterns. `provenance.json` maps every retained file
back to its disposable repository path and hashes it. No file is installed into
this workspace's actual runtime evidence or final-record locations.
''')
write(native_out/'provenance.json',dict(scope='synthetic observation payloads; genuine native fixture runner; test-only trust; no product runtime qualification',sourceFixture=str(fixture),test='test_round6_genuine_native_capture_publication_survives_fresh_checkout',testSource=ref(root/'_bmad/scripts/tests/test_epic16_tooling.py'),candidate=candidate,publicationCommit=transaction,lifecycleCommit=lifecycle,publicationPaths=publication,retainedAssemblyCount=1,captureCount=5,rawTrxCount=5,retained=retained,originalRawNativeBytesUnchanged=True,originalMeasurementDirectoryUnavailableToVerifier=True,externalBuildAndPayloadDirectoriesDeleted=True,fixtureBuildOutputsAbsentFromFreshCheckout=True))

absent=[]
for story in ('16.1','16.2','16.3'):
    paths=[*tooling.final_paths('runtime',story),*[fn('runtime',f'AC-{story}-0{n}') for fn in (tooling.native_path,tooling.receipt_path) for n in range(1,6)],*[tooling.capture.capture_path(story,f'AC-{story}-0{n}') for n in range(1,6)],tooling.root_path('runtime',story)+'/binaries']
    for path in paths:assert not (root/path).exists(),path
    absent+=paths
assert len(absent)==54
for path,value in tooling.PREDECESSOR_PINS.items():assert digest(root/path)==value,path
binding=load(root/tooling.BINDING);assert binding['entry'] is None and tooling.TRUSTED_ENTRIES=={}
assert json.loads((root/'.config/dotnet-tools.json').read_bytes())['tools']['hexalith.builds.module.cli']['version']=='4.35.2'
write(out/'runtime-absence.json',dict(entry=None,trustedEntries={},authority=binding['authority'],absentPaths=absent,predecessorPinsUnchanged=True,acceptedCliVersion='4.35.2',publication='not-applicable: real runtime remains held'))

matrix=[];requirements=set()
for story in ('16.1','16.2','16.3'):
    contract=load(root/tooling.contract_path(story));old=load(root/contract['predecessor'])
    assert contract['preservedContract']==old['preservedContract']
    for scenario in contract['preservedContract']['scenarios']:
        requirements.update(scenario['requirementIds'])
        native_scenario=not scenario['id'].endswith('06')
        matrix.append(dict(storyId=story,scenarioId=scenario['id'],requirementIds=scenario['requirementIds'],preservedObligationEqual=True,native=tooling.native_path('runtime',scenario['id']) if native_scenario else None,receipt=tooling.receipt_path('runtime',scenario['id']) if native_scenario else None,capture=tooling.capture.capture_path(story,scenario['id']) if native_scenario else None,finalRecord=tooling.final_paths('runtime',story)))
graph=load(root/'_bmad-output/planning-artifacts/epic-16-prerequisites-v1/execution-graph.json')
assert len(matrix)==18 and len(requirements)==32 and len(graph['nodes'])==38 and len(graph['edges'])==61
write(out/'preserved-scenario-matrix.json',dict(scenarios=matrix,uniqueRequirementCount=len(requirements),graphNodeCount=len(graph['nodes']),graphEdgeCount=len(graph['edges']),retainedPredecessorPins=tooling.PREDECESSOR_PINS,scope='Static preservation and I/O inventory audit. Final combined suite exercises all fifteen native positives and three deterministic final-record paths; actual runtime qualification remains held.'))

result=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True)
for label,content in [('stdout',result.stdout),('stderr',result.stderr)]:(out/f'whitespace.{label}.txt').write_bytes(content)
assert result.returncode==0,result.stderr
whitespace=dict(argv=['git','diff','--check'],exitCode=result.returncode,stdoutSha256=digest(out/'whitespace.stdout.txt'),stderrSha256=digest(out/'whitespace.stderr.txt'))
write(out/'whitespace.json',whitespace)
mutant_path=out/'pre-checkpoint-fixture-correction/mutations/mutation-probes.json'
mutants=load(mutant_path)
assert len(mutants)==25 and all(row['exitCode']==1 and row['tests']['failures']>0 and row['tests']['errors']==0 for row in mutants)
assert sum(row['tests']['failures'] for row in mutants)==173
value=dict(scope='authorized prerequisite implementation only; no actual runtime or owner authority',reviewIteration=6,rootHead=git(root,'rev-parse','HEAD'),preservation=ref(out/'preservation-restoration.json'),sourceEpoch=dict(name='final iteration-6 implementation',sourceBinding=ref(out/'verification-source.json'),unchangedDuringFinalCombinedLegacyAndHeldChecks=True,mutationEpochDisposition=ref(out/'source-epoch-disposition.json')),combined=dict(command=commands[0],counts=counts(out/'combined.xml'),successorTests=1051,historicalAdoptionV2Tests=162,retainedSuccessorTests=862,newSuccessorTests=189),legacy=dict(command=commands[1],counts=counts(out/'legacy.xml')),mutationProbes=dict(record=ref(mutant_path),mutants=25,targetedFailures=173,setupErrors=0,retainedPriorGuards=62,priorEvidence=ref(base/'review-iteration-5/verification.json'),note='New guard mutations retain the pre-description/checkpoint-negative source epoch. Only schema/helper descriptions, their consequent trust pin and the checkpoint-negative test input changed afterward; no functional guard changed. Earlier 47+15 observations remain historical, not rerun mutation claims. Final combined tests retain all previous regressions.'),held=ref(out/'held-commands.json'),completion=ref(out/'completion-commands.json'),whitespace=whitespace,parentCounterexamples=[ref(review/name) for name in ('parent-after-corrections.json','parent-schedule-probes.json','parent-capture-path-probes.json')],nativeCapture=ref(native_out/'provenance.json'),runtime=ref(out/'runtime-absence.json'),broadLane=dict(rerun=False,reason='spec prohibits rerunning broad lane',retained=ref(base/'verification-final.json'),unresolved=['REFRESH_UV_DRIFT package-refresh baseline','Story 10.4 guidance-binding baseline'],gateWeakened=False),intermediateObservations=[ref(out/path) for path in ('intermediate-observations.json','interrupted-before-native-hash-control/disposition.json','pre-checkpoint-fixture-correction/disposition.json','source-epoch-disposition.json')],disposableCommitValidation=ref(out/'disposable-commitlint.json'),orchestration='Retained existing parent workflow; no workflow render, secondary orchestration or subagent spawn in this iteration.',remainingOwnerWork=['Platform exact-pin/integration qualification','seventeen adoption findings','provider and operational parity','recovery drills','operator/deputy coverage','capacity or waiver decisions','human SM-2','separate trusted runtime entry'],unrelatedUserWork=['_bmad-output/implementation-artifacts/spec-5-4-ext-conv-ai-1-installed-delivery.md','docs/implementation/ext-conv-ai-1-runtime-qualification.md','tests/tooling/test_ext_conv_ai_1_installed_delivery_packet.py'])
value['artifacts']=[ref(path) for path in sorted(out.rglob('*')) if path.is_file()]
value['preservedScenarioMatrix']=ref(out/'preserved-scenario-matrix.json')
write(review/'verification.json',value)
print(json.dumps(dict(combined=value['combined']['counts'],legacy=value['legacy']['counts'],mutants=25,mutationFailures=173,sourceUnchanged=True,absentRuntimePaths=len(absent),verification=ref(review/'verification.json')),indent=2))
