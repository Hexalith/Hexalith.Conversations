from pathlib import Path
import json,hashlib,subprocess,xml.etree.ElementTree as ET,shutil
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-7';out=review/'implementation'
def digest(data):return hashlib.sha256(data).hexdigest()
def load(p):return json.loads(Path(p).read_bytes())
def ref(p):
 p=Path(p);return dict(path=str(p.relative_to(root)),sha256=digest(p.read_bytes()))
def counts(p):
 s=ET.parse(p).getroot();s=s if s.tag=='testsuite' else s.find('testsuite');return {key:int(s.attrib.get(key,0)) for key in ['tests','failures','errors','skipped']}
frozen=load(out/'final-source.json')['files']
for path,sha in frozen.items():assert digest((root/path).read_bytes())==sha,path
commands=load(out/'test-commands.json');assert len(commands)==2 and all(c['exitCode']==0 for c in commands)
combined=counts(out/'combined.xml');legacy=counts(out/'legacy.xml');assert combined==dict(tests=1467,failures=0,errors=0,skipped=0) and legacy==dict(tests=72,failures=0,errors=0,skipped=0)
for name,command in zip(['combined','legacy'],commands):
 for key,file in [('stdoutSha256',name+'.stdout.txt'),('stderrSha256',name+'.stderr.txt'),('junitSha256',name+'.xml')]:assert digest((out/file).read_bytes())==command[key]
mutation_paths=[out/'mutations/mutation-probes.json',out/'mutations-final-sdk/mutation-probes.json',out/'mutations-version-type/mutation-probes.json']
mutations=sum([load(p) for p in mutation_paths],[])
assert len(mutations)==38 and len({row['name'] for row in mutations})==35
assert all(row['exitCode']==1 and row['tests']['failures']>0 and row['tests']['errors']==0 for row in mutations)
assert sum(row['tests']['failures'] for row in mutations)==159
held=load(out/'held-commands.json')['commands'];assert len(held)==18 and all(row['exitCode']==2 and row['result']=='BLOCKED' and row['blockers']==['ENTRY_QUALIFICATION_PENDING'] for row in held)
completion=load(out/'completion-commands.json');assert len(completion)==2 and all(row['exitCode']==0 and row['priorBytesRestored'] for row in completion)
runtime=load(out/'runtime-absence.json');assert len(runtime['absentPaths'])==54 and all(not(root/p).exists() for p in runtime['absentPaths'])
assert runtime['entry'] is None and runtime['trustedEntries']=={} and runtime['acceptedCliVersion']=='4.35.2'
parent=load(review/'parent-after-corrections-final.json');assert parent['passed']==parent['total']==29
for path,sha in parent['sourceHashes'].items():assert digest((root/path).read_bytes())==sha,path
policy=load(review/'parent-malformed-policy-final.json');assert len(policy['results'])==3 and all(r['correct'] for r in policy['results'])
assert policy['helperSha256']==digest((root/'_bmad/scripts/epic16_native.py').read_bytes())
for name in ['epic16-r7-retain-native.py','epic16-r7-final-evidence.py']:
 shutil.copyfile(Path('/tmp')/name,out/'verification-scripts'/name)
# Final whitespace check follows the spec/handoff update; do not rewrite prior logs.
r=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True)
(out/'handoff-diff-check.stdout.txt').write_bytes(r.stdout);(out/'handoff-diff-check.stderr.txt').write_bytes(r.stderr);assert r.returncode==0
state=load(out/'runtime-and-user-state.json')
for path,sha in state['unrelatedFilesUnchangedAtHead'].items():assert digest((root/path).read_bytes())==sha,path
matrix=load(out/'preserved-scenario-matrix.json');assert len(matrix['scenarios'])==18 and matrix['uniqueRequirementCount']==32 and matrix['graphNodeCount']==38 and matrix['graphEdgeCount']==61
artifacts=[ref(p) for p in sorted(out.rglob('*')) if p.is_file()]
record=dict(scope='authorized prerequisite implementation only; no actual runtime or owner authority',reviewIteration=7,rootHead=state['rootHead'],baseline='8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3',preservation=ref(out/'restoration.json'),sourceEpoch=dict(name='final iteration-7 implementation',frozenSource=ref(out/'final-source.json'),sourceBinding=ref(out/'verification-source.json'),fileCount=len(frozen),unchangedDuringFinalCombinedLegacyAndHeldChecks=True,epochDisposition=ref(out/'source-epoch-disposition.json')),combined=dict(command=commands[0],counts=combined,successorTests=1305,historicalAdoptionV2Tests=162,retainedSuccessorTests=1051,newSuccessorTests=254),legacy=dict(command=commands[1],counts=legacy),mutationProbes=dict(records=[ref(p) for p in mutation_paths],uniqueGuards=35,runs=38,targetedFailures=159,setupErrors=0,epochNote='33 initial guards: 147 targeted failures on frozen-mutation-source.json. Final SDK reruns plus invalid-policy guard: four runs/eight failures. Final integer-schema guard: one run/four failures. Only SDK preflight and integer-schema guards changed after the initial source epoch; corresponding probes reran. Earlier mutation records remain historical, not new rerun claims.',priorEvidence=ref(root/'module/validation/epic-16-prerequisite-implementation/review-iteration-6/verification.json')),held=dict(record=ref(out/'held-commands.json'),count=18,result='BLOCKED',exitCode=2,blocker='ENTRY_QUALIFICATION_PENDING'),completion=dict(record=ref(out/'completion-commands.json'),passed=2,operationalBytesRestored=True),whitespace=dict(argv=['git','diff','--check'],exitCode=r.returncode,stdoutSha256=digest(r.stdout),stderrSha256=digest(r.stderr)),parentCounterexamples=[ref(p) for p in sorted(review.glob('parent-*.json'))],finalIndependentParentChecks=dict(semanticSchema=dict(passed=29,total=29,record=ref(review/'parent-after-corrections-final.json')),actualMalformedPolicySetup=dict(passed=3,total=3,record=ref(review/'parent-malformed-policy-final.json')),finalSourceHashesMatch=True),helperToolchain=ref(out/'helper-toolchain/provenance.json'),nativeCapture=ref(out/'native-capture-fixture/provenance.json'),runtime=ref(out/'runtime-absence.json'),runtimeAbsentFileCount=51,runtimeAbsentBinaryDirectoryCount=3,preservedScenarioMatrix=ref(out/'preserved-scenario-matrix.json'),implementationInventory=ref(out/'implementation-inventory.json'),runtimeAndUserState=ref(out/'runtime-and-user-state.json'),commitlint=ref(out/'commitlint/validation.json'),broadLane=dict(rerun=False,reason='spec prohibits rerunning broad lane',retained=ref(root/'module/validation/epic-16-prerequisite-implementation/verification-final.json'),unresolved=['REFRESH_UV_DRIFT package-refresh baseline','Story 10.4 guidance-binding baseline'],gateWeakened=False),orchestration=dict(parentRender='conversations-f0fc90326eb5/afbdeefb80a1404d78bf',deviation='Worker read bmad-build SKILL.md and rendered it once before reaching the no-secondary-workflow handoff instruction; disclosed to parent. No secondary workflow step, subagent, independent review orchestration or completion orchestration followed.',actualRepositoryStageCommitPush=False,dependencyUpdates=False),remaining=['Parent independent review and completion orchestration','Actual Platform published integration qualification and seventeen adoption findings','Provider/environment parity and recovery drills','Operator/deputy coverage and capacity/waiver decisions','Human SM-2 and separate owner entry; runtime story remains draft/backlog and ACTIVE hold'],artifacts=artifacts)
(review/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
print(json.dumps(dict(combined=combined,legacy=legacy,mutationRuns=len(mutations),uniqueMutationGuards=35,targetedFailures=159,artifacts=len(artifacts),sourceFiles=len(frozen),verification=ref(review/'verification.json'))))
