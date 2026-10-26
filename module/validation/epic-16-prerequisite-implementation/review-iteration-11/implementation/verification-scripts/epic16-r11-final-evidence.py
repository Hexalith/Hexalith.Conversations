from pathlib import Path
import json,hashlib,subprocess,xml.etree.ElementTree as ET,shutil
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11';out=review/'implementation';prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10'
def sha(data):return hashlib.sha256(data).hexdigest()
def load(p):return json.loads(Path(p).read_bytes())
def ref(p):
 p=Path(p);return dict(path=str(p.relative_to(root)),sha256=sha(p.read_bytes()))
def counts(p):
 xml=ET.parse(p).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];return {k:sum(int(s.get(k,0)) for s in suites) for k in ('tests','failures','errors','skipped')}
def cases(p):return {(c.get('classname'),c.get('name')):c for c in ET.parse(p).getroot().iter('testcase')}
frozen=load(out/'final-source.json')['files']
for path,value in frozen.items():assert sha((root/path).read_bytes())==value,path
commands=load(out/'test-commands.json');assert len(commands)==2 and all(c['exitCode']==0 and c['sourceUnchanged'] for c in commands)
combined=counts(out/'combined/result.xml');legacy=counts(out/'legacy/result.xml')
assert combined['tests']>=2152 and all(combined[k]==0 for k in ('failures','errors','skipped'))
assert legacy==dict(tests=72,failures=0,errors=0,skipped=0)
for name,command in zip(('combined','legacy'),commands):
 for file,value in command['files'].items():assert sha((out/name/file).read_bytes())==value,(name,file)
old_cases=cases(prior/'implementation/combined.xml');current=cases(out/'combined/result.xml');assert set(old_cases)<=set(current)
old_legacy=cases(prior/'implementation/legacy.xml');assert set(old_legacy)==set(cases(out/'legacy/result.xml'))
mutations=[];failures=[]
for directory in ('mutations','mutations-supplement'):
 for row in load(out/directory/'mutation-probes.json'):
  mutations.append(row);assert row['tests']['errors']==row['tests']['skipped']==0
  if row['tests']['failures']==0:assert directory=='mutations' and row['name']=='recovery-execution-caller' and row['exitCode']==5 and row['tests']['tests']==0
  else:assert row['exitCode']==1
  assert frozen[row['path']]==row['originalSha256']
  for identity,case in cases(out/directory/(row['name']+'.xml')).items():
   if case.find('failure') is not None:
    assert identity in current and current[identity].find('failure') is None and current[identity].find('error') is None and current[identity].find('skipped') is None,identity
    failures.append(dict(mutation=row['name'],classname=identity[0],test=identity[1]))
assert len(mutations)==36 and len({r['name'] for r in mutations if r['tests']['failures']})==35 and len(failures)==162
(out/'retained-tests-and-mutation-audit.json').write_text(json.dumps(dict(priorCombined=len(old_cases),retainedCombined=len(set(old_cases)&set(current)),currentCombined=len(current),newCases=sorted([list(i) for i in set(current)-set(old_cases)]),priorLegacy=len(old_legacy),identicalLegacy=True,mutantFailuresPassingOnFinalSource=failures),indent=2)+'\n')
held=load(out/'held-commands.json')['commands'];assert len(held)==18 and all(r['exitCode']==2 and r['result']=='BLOCKED' and r['blockers']==['ENTRY_QUALIFICATION_PENDING'] for r in held)
completion=load(out/'completion-commands.json');assert len(completion)==2 and all(r['exitCode']==0 and r['priorBytesRestored'] for r in completion)
state=load(out/'runtime-and-user-state.json')
for path,value in state['protectedUnchanged'].items():assert sha((root/path).read_bytes())==value,path
runtime=load(out/'runtime-absence.json');assert len(runtime['absentPaths'])==54 and all(not(root/p).exists() for p in runtime['absentPaths']);assert runtime['entry'] is None and runtime['trustedEntries']=={} and runtime['acceptedCliVersion']=='4.35.2'
matrix=load(out/'preserved-scenario-matrix.json');assert len(matrix['scenarios'])==18 and (matrix['uniqueRequirementCount'],matrix['graphNodeCount'],matrix['graphEdgeCount'])==(32,38,61)
retained=load(prior/'verification.json')['artifacts']
for artifact in retained:assert sha((root/artifact['path']).read_bytes())==artifact['sha256'],artifact['path']
assert len(retained)==1623
notes={
 'exploratory-a':'245 passed, three intentionally conflicting Fact fixture builds rejected by xUnit analyzer before runner execution. Only those deliberately invalid fixture builds subsequently disable analyzers; inspector admission was not weakened. Exact source snapshot and original failing run retained.',
 'exploratory-b':'279 passed, three new external attribute library fixture builds failed because xunit.v3 requires executable projects. The synthetic library uses the same pinned 4.0.2 extensibility-core package instead; actual test runner remains xunit.v3. This epoch includes P1 framework owner and P2 native map corrections.',
 'exploratory-c':'All three actual external attribute/discovery controls passed after the fixture library reference correction. No implementation guard changed after exploratory-b; final full source verification supersedes all exploratory selections.'}
epochs=[]
for name,note in notes.items():epochs.append(dict(label=name,command=ref(out/name/'command.json'),source=ref(out/name/'source-hashes.json'),counts=counts(out/name/'result.xml'),note=note))
(out/'source-epoch-disposition.json').write_text(json.dumps(dict(finalSource=ref(out/'final-source.json'),unchangedDuringFinalVerification=True,intermediate=epochs,mutationDisposition=ref(out/'mutation-disposition.json'),archiveMapping={'.gitignore':'.gitignore.bytes'},initialLibraryReproduction=dict(argv=['dotnet','build','Attributes.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd='/tmp/pytest-of-administrator/pytest-610/test_round11_framework_ownersh0/attributes',exitCode=1,diagnostic='xUnit.net v3 test projects must be executable; use xunit.v3.extensibility.core for a non-test library'),retainedHistoryArtifactCount=1623),indent=2)+'\n')
messages={}
for path in sorted(out.rglob('*commitlint.json')):
 for message,result in load(path).items():assert result['exitCode']==0,(path,message);messages.setdefault(message,[]).append(str(path.relative_to(root)))
(out/'commitlint-validation.json').write_text(json.dumps(dict(argv=[str(root/'node_modules/.bin/commitlint'),'--config',str(root/'commitlint.config.mjs')],stdin='Exact full message plus newline before first use in each test process',scope='Disposable fixture commits only; no actual repository staging or commit',uniqueMessageCount=len(messages),messages=messages,policy=ref(root/'commitlint.config.mjs'),packageLock=ref(root/'package-lock.json')),indent=2)+'\n')
owned=load(out/'restoration.json')['restored'];assert len(owned)==42
(out/'implementation-inventory.json').write_text(json.dumps(dict(restoredOwnedCount=len(owned),currentOwnedCount=len(owned),currentOwnedFiles=[dict(path=p,sha256=sha((root/p).read_bytes()),changedSincePreservation=h!=sha((root/p).read_bytes())) for p,h in sorted(owned.items())]),indent=2)+'\n')
scripts=out/'verification-scripts';scripts.mkdir(exist_ok=True)
for name in ('edit-metadata','edit-protocol','captures','docs-bind','bind','parent-fixes','run','mutations','mutations-supplement','final-tests','helper','state-checks','retain-native','final-evidence','update-docs'):
 p=Path('/tmp/epic16-r11-'+name+'.py');shutil.copyfile(p,scripts/p.name)
parent={name:ref(review/(name+'.json')) for name in ('parent-native-recheck','parent-protocol-recheck','parent-P1-P2-recheck','parent-source-audit','parent-data-source-audit')}
parent['finalSourceDiff']=ref(review/'parent-final-source.diff')
for path,value in frozen.items():assert sha((root/path).read_bytes())==value,path
check=subprocess.run(['git','diff','--check'],capture_output=True);assert check.returncode==0,(check.stdout,check.stderr)
(out/'final-diff-check.stdout.txt').write_bytes(check.stdout);(out/'final-diff-check.stderr.txt').write_bytes(check.stderr)
artifacts=[ref(p) for p in sorted(out.rglob('*')) if p.is_file()]
ignored=subprocess.run(['git','check-ignore','-z','--stdin'],input=('\0'.join(r['path'] for r in artifacts)+'\0').encode(),capture_output=True)
assert ignored.returncode==1 and not ignored.stdout and not ignored.stderr,ignored.stdout
record=dict(scope='Authorized prerequisite implementation only; no actual runtime or owner authority',reviewIteration=11,verificationCommand=['python3','/tmp/epic16-r11-final-evidence.py'],rootHead=state['rootHead'],baseline='8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3',preservation=ref(out/'restoration.json'),retainedHistory=ref(out/'retained-history.json'),sourceEpoch=dict(frozenSource=ref(out/'final-source.json'),fileCount=len(frozen),boundSourceCount=71,unchangedDuringFinalVerification=True,disposition=ref(out/'source-epoch-disposition.json')),combined=dict(command=commands[0],counts=combined,successorTests=combined['tests']-162,historicalAdoptionV2Tests=162,retainedPriorCombined=len(old_cases),newSuccessorTests=len(current)-len(old_cases)),legacy=dict(command=commands[1],counts=legacy),mutationProbes=dict(records=[ref(out/d/'mutation-probes.json') for d in ('mutations','mutations-supplement')],runs=len(mutations),demonstratedGuards=35,targetedFailures=162,setupErrors=0,disposition=ref(out/'mutation-disposition.json'),finalPassingAudit=ref(out/'retained-tests-and-mutation-audit.json')),held=dict(record=ref(out/'held-commands.json'),count=18,result='BLOCKED',exitCode=2,blocker='ENTRY_QUALIFICATION_PENDING'),completion=dict(record=ref(out/'completion-commands.json'),passed=2,operationalBytesRestored=True),whitespace=dict(argv=['git','diff','--check'],exitCode=0),parentChecks=parent,helperToolchain=ref(out/'helper-toolchain/provenance.json'),nativeCapture=ref(out/'native-capture-fixture/provenance.json'),nativeControls=dict(path=str((out/'combined/native').relative_to(root)),scope='Synthetic compiled API and native discovery fixtures with retained exact build/runner streams; no actual product qualification'),runtime=ref(out/'runtime-absence.json'),preservedScenarioMatrix=ref(out/'preserved-scenario-matrix.json'),implementationInventory=ref(out/'implementation-inventory.json'),runtimeAndUserState=ref(out/'runtime-and-user-state.json'),commitlint=ref(out/'commitlint-validation.json'),broadLane=dict(rerun=False,reason='Spec prohibits repeating the established broad baseline lane',retained=ref(root/'module/validation/epic-16-prerequisite-implementation/verification-final.json'),unresolved=['REFRESH_UV_DRIFT package-refresh baseline','Story 10.4 guidance-binding baseline'],gateWeakened=False),orchestration=dict(skillRead=False,workflowRendered=False,secondaryWorkflowInvoked=False,childAgentSpawned=False,completionOrchestration=False,actualRepositoryStageCommitPush=False,dependencyUpdates=False),remaining=['Parent independent audit, review and completion orchestration','Actual Platform integration qualification and seventeen adoption findings','Provider/environment parity, recovery drills, operator/deputy coverage and capacity/waiver decisions','Human SM-2 and separate owner entry; Story 16.1 remains draft/backlog with ACTIVE hold'],artifacts=artifacts)
(review/'verification.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(dict(combined=combined,legacy=legacy,mutationGuards=35,targetedFailures=162,artifacts=len(artifacts),commitMessages=len(messages),verification=ref(review/'verification.json'))))
