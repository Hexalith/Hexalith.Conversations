from pathlib import Path
import json,hashlib,subprocess,xml.etree.ElementTree as ET,shutil
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12';out=review/'implementation';prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11'
def sha(data):return hashlib.sha256(data).hexdigest()
def load(p):return json.loads(Path(p).read_bytes())
def ref(p):
 p=Path(p);return dict(path=str(p.relative_to(root)),sha256=sha(p.read_bytes()))
def counts(p):
 xml=ET.parse(p).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];return {k:sum(int(s.get(k,0)) for s in suites) for k in ('tests','failures','errors','skipped')}
def cases(p):return {(c.get('classname'),c.get('name')):c for c in ET.parse(p).getroot().iter('testcase')}
originalFrozen=load(out/'final-source.json')['files'];frozen=load(out/'packaging-source.json')['files'];guardsOut=out/'packaging-guards'
for path,value in frozen.items():assert sha((root/path).read_bytes())==value,path
originalCommands=load(out/'test-commands.json');packagingCommands=load(out/'packaging-test-commands.json');assert len(originalCommands)==1 and originalCommands[0]['exitCode']==0 and originalCommands[0]['sourceUnchanged'] is False
assert len(packagingCommands)==2 and all(c['exitCode']==0 and c['sourceUnchanged'] for c in packagingCommands)
commands=[originalCommands[0],packagingCommands[1]];packaging=counts(out/'packaging/result.xml');assert all(packaging[k]==0 for k in ('failures','errors','skipped'))
combined=counts(out/'combined/result.xml');legacy=counts(out/'legacy/result.xml')
assert combined['tests']>=2416 and all(combined[k]==0 for k in ('failures','errors','skipped'))
assert legacy==dict(tests=72,failures=0,errors=0,skipped=0)
for name,command,expected in [('combined',commands[0],originalFrozen),('packaging',packagingCommands[0],frozen),('legacy',commands[1],frozen)]:
 for file,value in command['files'].items():assert sha((out/name/file).read_bytes())==value,(name,file)
 assert load(out/name/'source-hashes.json')==expected
 for path,value in expected.items():assert sha((out/name/'source'/(path+'.bytes' if path=='.gitignore' else path)).read_bytes())==value,(name,path)
assert [p for p in originalFrozen if originalFrozen[p]!=frozen[p]]==['.gitattributes','_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json']
old_cases=cases(prior/'implementation/combined/result.xml');current=cases(out/'combined/result.xml');assert set(old_cases)<=set(current)
old_legacy=cases(prior/'implementation/legacy/result.xml');assert set(old_legacy)==set(cases(out/'legacy/result.xml'))
mutations=[];failures=[]
for directory in ('mutations','mutations-notice'):
 for row in load(out/directory/'mutation-probes.json'):
  mutations.append(row);assert row['tests']['errors']==row['tests']['skipped']==0
  assert row['tests']['failures']>0 and row['exitCode']==1
  assert row['originalSha256'] in (frozen[row['path']],load(out/'pre-notice-source.json')['files'][row['path']])
  assert row['testSha256']==frozen['_bmad/scripts/tests/test_epic16_tooling.py']
  assert sha((out/directory/row['name']/'original.bytes').read_bytes())==row['originalSha256']
  assert sha((out/directory/row['name']/'mutated.bytes').read_bytes())==row['mutatedSha256']
  for identity,case in cases(out/directory/row['name']/'result.xml').items():
   if case.find('failure') is not None:
    assert identity in current and current[identity].find('failure') is None and current[identity].find('error') is None and current[identity].find('skipped') is None,identity
    failures.append(dict(directory=directory,mutation=row['name'],classname=identity[0],test=identity[1]))
guards=len({r['name'] for r in mutations});assert guards==39 and len(mutations)==42 and len(failures)>0
(out/'retained-tests-and-mutation-audit.json').write_text(json.dumps(dict(priorCombined=len(old_cases),retainedCombined=len(set(old_cases)&set(current)),currentCombined=len(current),newCases=sorted([list(i) for i in set(current)-set(old_cases)]),priorLegacy=len(old_legacy),identicalLegacy=True,combinedSourceUnchanged=False,currentCodeTestsIdenticalToCombined=True,mutantFailuresPassingOnFinalSource=failures),indent=2)+'\n')
held=load(guardsOut/'held-commands.json')['commands'];assert len(held)==18 and all(r['exitCode']==2 and r['result']=='BLOCKED' and r['blockers']==['ENTRY_QUALIFICATION_PENDING'] for r in held)
completion=load(guardsOut/'completion-commands.json');assert len(completion)==2 and all(r['exitCode']==0 and r['priorBytesRestored'] for r in completion)
state=load(guardsOut/'runtime-and-user-state.json')
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==state['rootHead']
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)
import sys
sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
assert sha((root/tooling.spec_path('runtime','16.1')).read_bytes())==state['runtimeSpecSha256']
assert sha((root/tooling.SPRINT).read_bytes())==state['sprintSha256']
for path,value in state['protectedUnchanged'].items():assert sha((root/path).read_bytes())==value,path
runtime=load(guardsOut/'runtime-absence.json');assert len(runtime['absentPaths'])==54 and all(not(root/p).exists() for p in runtime['absentPaths']);assert runtime['entry'] is None and runtime['trustedEntries']=={} and runtime['acceptedCliVersion']=='4.35.2'
matrix=load(guardsOut/'preserved-scenario-matrix.json');assert len(matrix['scenarios'])==18 and (matrix['uniqueRequirementCount'],matrix['graphNodeCount'],matrix['graphEdgeCount'])==(32,38,61)
retained=load(prior/'verification.json')['artifacts']
for artifact in retained:assert sha((root/artifact['path']).read_bytes())==artifact['sha256'],artifact['path']
assert len(retained)==2854
previous=load(out/'restoration.json')['priorEvidence']
for path,value in previous.items():assert sha((root/path).read_bytes())==value,path
(out/'retained-history.json').write_text(json.dumps(dict(unchanged=True,preexistingCount=len(previous),priorIndexedArtifactCount=len(retained),preexisting=previous),indent=2)+'\n')
notes={
 'exploratory-a':'428 passes and two old nullable-description positives failed because their new reverse-save execution snapshots had not been refreshed. The captures were correlated with the same admitted native replay; the expected positive remained unchanged.',
 'exploratory-b':'100 selected passes, including genuine framework stamps and parameters, native custom Task runner success with inspector rejection, foreign parameter-array runner rejection, and preserved nullable/opaque-ID positives.',
 'exploratory-c':'21 selected passes. This source epoch incorrectly expected UnmappedRole for the defined Unknown role; source audit corrected it to native InsufficientRole before final verification. Parent later independently confirmed the correction.',
 'exploratory-d':'12 selected passes after the Unknown-role correction, including the complete ParamArray attribute blob control.',
 'exploratory-e':'34 selected passes after native tenant/principal input validation, including exact compiled actual Commons evaluator parity and legitimate Unicode controls.',
 'exploratory-f':'6 selected passes for coherent same-process rejection and genuinely shortened failed-attempt inventories.'}
epochs=[]
for name,note in notes.items():epochs.append(dict(label=name,command=ref(out/name/'command.json'),source=ref(out/name/'source-hashes.json'),counts=counts(out/name/'result.xml'),note=note))
(out/'source-epoch-disposition.json').write_text(json.dumps(dict(originalCombinedSource=ref(out/'final-source.json'),currentPackagingSource=ref(out/'packaging-source.json'),concurrentDisposition=ref(out/'packaging-epoch/disposition.json'),preNoticeSource=ref(out/'pre-notice-source.json'),preNoticeHelper=ref(out/'helper-toolchain/provenance.json'),originalCombinedSourceUnchanged=False,currentPackagingSourceUnchanged=True,intermediate=epochs,mutationDisposition=ref(out/'mutation-disposition.json'),archiveMapping={'.gitignore':'.gitignore.bytes'},snapshotCounts=dict(exploratory=80,final=86),preparationNotes=['The initial native access source-attribution script guessed Enums/TenantStatusJsonConverter.cs and stopped after helper edits; the actual Serialization/TenantStatusJsonConverter.cs was then retained. This was a preparation lookup error, not a test result.','Concurrent user/Cursor /pushall commit 46ca7bf39b25318418b7145b7ce75bb306471cd0 changed only the frozen evidence whitespace attribute during combined execution and advanced root/submodule commits. All 2879 passes and sourceUnchanged=false are preserved. Only the attribute binding hash was refreshed; affected packaging/checkout/source checks and 72 legacy ran on stable current 86 source. No full combined rerun is claimed.','Three Program.cs mutations were repeated after the notice-only source correction so every guard has a run against the final exact target bytes. Earlier source epochs and helper/state checks are retained.','The initial batched read included bmad-build/SKILL.md before reaching the spec-only handoff instruction. No render, invocation, workflow, child agent or completion orchestration followed.'],retainedHistoryArtifactCount=2854,allPreexistingEvidenceCount=len(previous),currentPackagingCommand=ref(out/'packaging/command.json'),currentLegacyCommand=ref(out/'legacy/command.json')),indent=2)+'\n')
messages={}
for path in sorted(out.rglob('*commitlint.json')):
 for message,result in load(path).items():assert result['exitCode']==0,(path,message);messages.setdefault(message,[]).append(str(path.relative_to(root)))
(out/'commitlint-validation.json').write_text(json.dumps(dict(argv=[str(root/'node_modules/.bin/commitlint'),'--config',str(root/'commitlint.config.mjs')],stdin='Exact full message plus newline before first use in each test process',scope='Disposable fixture commits only; no actual repository staging or commit',uniqueMessageCount=len(messages),messages=messages,policy=ref(root/'commitlint.config.mjs'),packageLock=ref(root/'package-lock.json')),indent=2)+'\n')
owned=load(out/'restoration.json')['restored'];assert len(owned)==42
(out/'implementation-inventory.json').write_text(json.dumps(dict(restoredOwnedCount=len(owned),currentOwnedCount=len(owned),currentOwnedFiles=[dict(path=p,sha256=sha((root/p).read_bytes()),changedSincePreservation=h!=sha((root/p).read_bytes())) for p,h in sorted(owned.items())]),indent=2)+'\n')
scripts=out/'verification-scripts';scripts.mkdir(exist_ok=True)
for p in sorted(Path('/tmp').glob('epic16-r12-*.py')):
 if 'parent' not in p.name:shutil.copyfile(p,scripts/p.name)
parent={name:ref(review/name/'manifest.json') for name in ('parent-product-stamp-proof','parent-access-tenant-proof','parent-access-parity-proof')}
for path,value in frozen.items():assert sha((root/path).read_bytes())==value,path
check=subprocess.run(['git','diff','--check'],capture_output=True);assert check.returncode==0,(check.stdout,check.stderr)
(out/'final-diff-check.stdout.txt').write_bytes(check.stdout);(out/'final-diff-check.stderr.txt').write_bytes(check.stderr)
artifacts=[ref(p) for p in sorted(out.rglob('*')) if p.is_file()]
ignored=subprocess.run(['git','check-ignore','-z','--stdin'],input=('\0'.join(r['path'] for r in artifacts)+'\0').encode(),capture_output=True)
assert ignored.returncode==1 and not ignored.stdout and not ignored.stderr,ignored.stdout
record=dict(scope='Authorized prerequisite implementation only; no actual runtime or owner authority',reviewIteration=12,verificationCommand=['python3','/tmp/epic16-r12-final-evidence.py'],rootHead=state['rootHead'],baseline='8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3',preservation=ref(out/'restoration.json'),retainedHistory=ref(out/'retained-history.json'),sourceEpoch=dict(frozenSource=ref(out/'packaging-source.json'),originalCombinedSource=ref(out/'final-source.json'),concurrentDisposition=ref(out/'packaging-epoch/disposition.json'),fileCount=len(frozen),boundSourceCount=71,originalCombinedSourceUnchanged=False,currentPackagingSourceUnchanged=True,disposition=ref(out/'source-epoch-disposition.json')),combined=dict(command=commands[0],counts=combined,successorTests=combined['tests']-162,historicalAdoptionV2Tests=162,retainedPriorCombined=len(old_cases),newSuccessorTests=len(current)-len(old_cases),sourceUnchanged=False,epoch='original source snapshot with concurrent user Git attribute/HEAD/gitlink change; all 2879 observations preserved'),packaging=dict(command=packagingCommands[0],counts=packaging,currentSourceStable=True),legacy=dict(command=commands[1],counts=legacy,currentSourceStable=True),mutationProbes=dict(records=[ref(out/d/'mutation-probes.json') for d in ('mutations','mutations-notice')],runs=len(mutations),demonstratedGuards=guards,targetedFailures=len(failures),setupErrors=0,disposition=ref(out/'mutation-disposition.json'),finalPassingAudit=ref(out/'retained-tests-and-mutation-audit.json')),held=dict(record=ref(guardsOut/'held-commands.json'),count=18,result='BLOCKED',exitCode=2,blocker='ENTRY_QUALIFICATION_PENDING'),completion=dict(record=ref(guardsOut/'completion-commands.json'),passed=2,operationalBytesRestored=True),whitespace=dict(argv=['git','diff','--check'],exitCode=0),parentChecks=parent,helperToolchain=ref(out/'helper-toolchain-final/provenance.json'),nativeCapture=ref(out/'native-capture-fixture-packaging/provenance.json'),originalNativeCapture=ref(out/'native-capture-fixture/provenance.json'),nativeControls=dict(paths=[str((out/'combined'/p).relative_to(root)) for p in ('native','native-r10','native-r12')],scope='Synthetic compiled API/discovery fixtures and actual retained Commons evaluator input parity with exact build/runner streams; no actual product qualification'),runtime=ref(guardsOut/'runtime-absence.json'),preservedScenarioMatrix=ref(guardsOut/'preserved-scenario-matrix.json'),implementationInventory=ref(out/'implementation-inventory.json'),runtimeAndUserState=ref(guardsOut/'runtime-and-user-state.json'),commitlint=ref(out/'commitlint-validation.json'),broadLane=dict(rerun=False,reason='Spec prohibits repeating the established broad baseline lane',retained=ref(root/'module/validation/epic-16-prerequisite-implementation/verification-final.json'),unresolved=['REFRESH_UV_DRIFT package-refresh baseline','Story 10.4 guidance-binding baseline'],gateWeakened=False),orchestration=dict(skillRead=True,skillReadDisposition=load(out/'restoration.json')['deviation'],workflowRendered=False,secondaryWorkflowInvoked=False,childAgentSpawned=False,completionOrchestration=False,actualRepositoryStageCommitPush=False,dependencyUpdates=False),remaining=['Parent independent audit, review and completion orchestration','Actual Platform integration qualification and seventeen adoption findings','Provider/environment parity, recovery drills, operator/deputy coverage and capacity/waiver decisions','Human SM-2 and separate owner entry; Story 16.1 remains draft/backlog with ACTIVE hold'],artifacts=artifacts)
(review/'verification.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(dict(combined=combined,packaging=packaging,legacy=legacy,mutationGuards=guards,targetedFailures=len(failures),artifacts=len(artifacts),commitMessages=len(messages),verification=ref(review/'verification.json'))))
