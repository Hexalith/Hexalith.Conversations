from pathlib import Path
import json,hashlib,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';out=review/'implementation';prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12'
load=lambda p:json.loads(Path(p).read_bytes());sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def ref(p):
 p=Path(p);return dict(path=str(p.relative_to(root)),sha256=sha(p))
def counts(p):
 tree=ET.parse(p).getroot();suites=list(tree) if tree.tag=='testsuites' else [tree];return {k:sum(int(s.get(k,0)) for s in suites) for k in ('tests','failures','errors','skipped')}
def cases(p):return {(c.get('classname'),c.get('name')):c for c in ET.parse(p).getroot().iter('testcase')}
frozen=load(out/'combined-final-order/source.json')['files'];assert len(frozen)==86
for p,h in frozen.items():assert sha(root/p)==h,p
runs={}
for name in ('focused-final-order','combined-final-order','legacy-final-order'):
 command=load(out/name/'command.json');assert command['exitCode']==0 and command['sourceUnchanged'];assert load(out/name/'source.json')['files']==frozen
 assert sha(out/name/'source.json')==command['sourceSha256'];assert sha(out/name/'stdout')==command['stdoutSha256'] and sha(out/name/'stderr')==command['stderrSha256']
 for p,h in frozen.items():assert sha(out/name/'source'/(p+'.bytes' if p=='.gitignore' else p))==h,(name,p)
 result=counts(out/name/'result.xml');assert all(result[k]==0 for k in ('failures','errors','skipped'));runs[name]=dict(command=ref(out/name/'command.json'),counts=result,source=ref(out/name/'source.json'),sourceUnchanged=True)
assert runs['legacy-final-order']['counts']==dict(tests=72,failures=0,errors=0,skipped=0)
parent_controls=load(review/'parent-final-controls-order/report.json');assert parent_controls['count']==30 and parent_controls['sourceUnchanged'] and parent_controls['sourceHashes']==frozen
for row in parent_controls['rows']:
 if 'artifact' in row:assert sha(root/row['artifact'])==row['sha256'],row['label']
monitor_samples=[json.loads(line) for line in (out/'progress-final.jsonl').read_text().splitlines()];last_sample=monitor_samples[-1]
assert last_sample['exitCode']==0 and last_sample['sourceUnchanged'] and not last_sample['failures']
assert last_sample['observedCases']==last_sample['expectedCollected']==runs['combined-final-order']['counts']['tests']
old_cases=cases(prior/'implementation/combined/result.xml');current=cases(out/'combined-final-order/result.xml');assert len(old_cases)==2879 and set(old_cases)<=set(current)
old_legacy=cases(prior/'implementation/legacy/result.xml');assert len(old_legacy)==72 and set(old_legacy)==set(cases(out/'legacy-final-order/result.xml'))
packaging=cases(prior/'implementation/packaging/result.xml');assert len(packaging)==19 and set(packaging)<=set(current)
mutants=load(out/'mutations-final-order/mutation-probes.json');assert len(mutants)==23;failures=[]
for row in mutants:
 assert row['exitCode']==1 and row['tests']['failures']>0 and row['tests']['errors']==row['tests']['skipped']==0,row['name']
 assert row['originalSha256']==frozen[row['path']] and row['testSha256']==frozen['_bmad/scripts/tests/test_epic16_tooling.py']
 assert sha(out/'mutations-final-order'/row['name']/'original.bytes')==row['originalSha256'];assert sha(out/'mutations-final-order'/row['name']/'mutated.bytes')==row['mutatedSha256']
 for identity,case in cases(out/'mutations-final-order'/row['name']/'result.xml').items():
  if case.find('failure') is not None:
   assert identity in current and not any(current[identity].find(k) is not None for k in ('failure','error','skipped'));failures.append(dict(mutation=row['name'],classname=identity[0],test=identity[1]))
(out/'retained-tests-and-mutation-audit.json').write_text(json.dumps(dict(priorCombined=len(old_cases),retainedCombined=len(old_cases),currentCombined=len(current),newCases=sorted([list(i) for i in set(current)-set(old_cases)]),priorLegacy=len(old_legacy),identicalLegacy=True,preservedPackagingCases=len(packaging),mutantFailuresPassingOnFinalSource=failures),indent=2)+'\n')
first=load(out/'mutations/mutation-probes.json');survivors=[r for r in first if r['tests']['failures']==0];assert len(survivors)==1 and survivors[0]['name']=='provider-constructor-trailing'
(out/'mutation-disposition.json').write_text(json.dumps(dict(originalRun=ref(out/'mutations/mutation-probes.json'),finalRun=ref(out/'mutations-final-order/mutation-probes.json'),originalSurvival='Collection(string) shares its constructor signature blob with the independently validated AssemblyInformationalVersion constructor. The first probe was rejected by the retained source-stamp guard; it did not demonstrate sensitivity of the new trailing-signature check.',correction='The same production guard removal was repeated against the unique Collection(Type) constructor signature. That case failed its expected-rejection assertion. All23 removals were repeated on the final exact test/source bytes after native build-stream retention and parent decimal-sign/nullable-constraint corrections.',originalSurvivors=[r['name'] for r in survivors],demonstratedGuards=len(mutants),finalAssertionFailures=len(failures),setupErrors=0),indent=2)+'\n')
guards=out/'guards-final-order';held=load(guards/'held-commands.json')['commands'];assert len(held)==18 and all(r['exitCode']==2 and r['result']=='BLOCKED' and r['blockers']==['ENTRY_QUALIFICATION_PENDING'] for r in held)
completion=load(guards/'completion-commands.json');assert len(completion)==2 and all(r['exitCode']==0 and r['priorBytesRestored'] for r in completion)
state=load(guards/'runtime-and-user-state.json');assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==state['rootHead']=='46ca7bf39b25318418b7145b7ce75bb306471cd0';assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)
for p,h in state['protectedUnchanged'].items():assert sha(root/p)==h,p
import sys
sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
assert sha(root/tooling.spec_path('runtime','16.1'))==state['runtimeSpecSha256']
assert sha(root/tooling.SPRINT)==state['sprintSha256']
assert tooling.TRUSTED_ENTRIES=={}
assert subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip()==state['branch']
runtime=load(guards/'runtime-absence.json');assert len(runtime['absentPaths'])==54 and all(not(root/p).exists() for p in runtime['absentPaths']);assert runtime['entry'] is None and runtime['trustedEntries']=={} and runtime['acceptedCliVersion']=='4.35.2'
matrix=load(guards/'preserved-scenario-matrix.json');assert len(matrix['scenarios'])==18 and (matrix['uniqueRequirementCount'],matrix['graphNodeCount'],matrix['graphEdgeCount'])==(32,38,61)
indexed=load(prior/'verification.json')['artifacts'];assert len(indexed)==3746
for artifact in indexed:assert sha(root/artifact['path'])==artifact['sha256'],artifact['path']
previous=load(out/'restoration.json')['priorEvidence'];assert len(previous)==12245
for p,h in previous.items():assert sha(root/p)==h,p
(out/'retained-history.json').write_text(json.dumps(dict(unchanged=True,preexistingCount=len(previous),priorIndexedArtifactCount=len(indexed),preexisting=previous),indent=2)+'\n')
notes={
'exploratory-1':'69 passes and five test-preparation failures: decimal blob-length prefix was incorrectly assumed16 rather than18; broad signature search found two candidate signatures. These were corrected with exact MetadataReader heap offsets. Original results retained.',
'exploratory-2':'92 iteration13 controls passed with actual pinned constructor/provider controls and structured runtime/publication/host checks.',
'exploratory-3':'43 passes and four new-test AttributeErrors: schema validator helper name did not exist. Replaced with the existing jsonschema Draft202012Validator API; expectations unchanged.',
'exploratory-4':'113 selected controls passed, including genuine9-owner native publication and both new-owner mixed-implementation rejections. Later test-only retention changes add separate native build stdout/stderr and exact build inputs without changing production sources.',
'focused-final':'113 selected controls passed with native build-stream retention. Parent R13P2 later disproved the decimal-sign restriction; this pre-correction source and its green results are retained as an incomplete protocol interpretation.',
'focused-corrected':'117 selected controls passed after the decimal-sign correction. Parent R13P3 later disproved Nullable<String>/Object admission; this remains a pre-constraint-correction epoch.',
'nullable-focused':'37 selected controls passed and one nested-nullable preparation assertion failed because a TypeRef required a two-byte coded handle. The equal-width control was corrected without changing production behavior.',
'focused-final-constraint':'130 selected controls passed after nullable corrections. The later combined run exposed only an outdated lexical Gitlink expected order in a separate preserved positive test.',
'focused-final-order':'131 selected controls pass on final source, adding the corrected old positive to all130 preceding focused cases.',
'combined-final-constraint':'Stopped after1127 passes and one failing obsolete expected-gitlink-order assertion; production runtime evaluation passed. The isolated diagnostic confirms the one-line test expectation correction. Exit2 and unchanged source are retained.',
'legacy-final-constraint':'72 exact legacy cases passed on the pre-order-correction source; retained separately from the final repeated legacy lane.',
'diagnostic-qualified-runtime':'The isolated old positive reproduced the Commons-first lexical-order mismatch, with all preceding runtime/binary/canonicalizer assertions passing.',
'combined':'Parent stopped the initial combined run pending R13P3 proof after104 passes. Exit2 and stable source retained; no completed combined claim.',
'legacy':'Initial command accidentally selected the whole historical generator module; stopped after199 passes when selection error was detected. No completed legacy claim.',
'legacy-selected':'The exact earlier selector passed72 cases before R13P3; preserved as a pre-fix epoch, rerun on final source separately.'}
(out/'source-epoch-disposition.json').write_text(json.dumps(dict(epochs=[dict(label=n,command=ref(out/n/'command.json'),source=ref(out/n/'source.json'),counts=counts(out/n/'result.xml'),note=note) for n,note in notes.items()],finalSource=ref(out/'combined-final-order/source.json'),originalIteration12Source=ref(prior/'implementation/final-source.json'),currentIteration12Source=ref(prior/'implementation/packaging-source.json'),archiveMapping={'.gitignore':'.gitignore.bytes'},sourceFileCount=len(frozen),boundSourceCount=71,preparationNotes=['The first final state-check launch raced creation of the combined source snapshot and stopped before any guard command. Its FileNotFoundError streams are retained; the sequential retry passed.'],restoration=ref(out/'restoration.json'),parentDefaults=ref(review/'parent-default-controls/manifest.json'),parentDecimalSign=ref(review/'parent-decimal-sign/manifest.json'),parentNullableConstraint=ref(review/'parent-nullable-constraint/manifest.json'),interruptedCombined=ref(out/'combined-parent-audit-hold.json'),legacySelectionCorrection=ref(out/'legacy-selection-correction.json'),gitlinkOrderCorrection=ref(out/'gitlink-order-source-disposition.json'),interruptedOrderEpoch=ref(out/'combined-order-correction-hold.json')),indent=2)+'\n')
messages={}
for p in sorted(out.rglob('*commitlint.json')):
 for message,result in load(p).items():assert result['exitCode']==0,(p,message);messages.setdefault(message,[]).append(str(p.relative_to(root)))
(out/'commitlint-validation.json').write_text(json.dumps(dict(argv=[str(root/'node_modules/.bin/commitlint'),'--config',str(root/'commitlint.config.mjs')],stdin='Exact full message plus newline before use in each disposable test process',scope='Disposable fixture commits only; no real repository staging or commit',uniqueMessageCount=len(messages),messages=messages,policy=ref(root/'commitlint.config.mjs'),packageLock=ref(root/'package-lock.json')),indent=2)+'\n')
owned=load(out/'restoration.json')['restored'];assert len(owned)==42
(out/'implementation-inventory.json').write_text(json.dumps(dict(restoredOwnedCount=len(owned),currentOwnedCount=len(owned),currentOwnedFiles=[dict(path=p,sha256=sha(root/p),changedSincePreservation=h!=sha(root/p)) for p,h in sorted(owned.items())]),indent=2)+'\n')
check=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True);assert check.returncode==0,(check.stdout,check.stderr)
(out/'final-diff-check.stdout').write_bytes(check.stdout);(out/'final-diff-check.stderr').write_bytes(check.stderr)
for p,h in frozen.items():assert sha(root/p)==h,p
artifacts=[ref(p) for p in sorted(out.rglob('*')) if p.is_file()]
ignored=subprocess.run(['git','check-ignore','-z','--stdin'],input=('\0'.join(r['path'] for r in artifacts)+'\0').encode(),capture_output=True);assert ignored.returncode==1 and not ignored.stdout and not ignored.stderr,ignored.stdout
record=dict(scope='Authorized prerequisite implementation only; no actual runtime qualification or owner authority',reviewIteration=13,verificationCommand=['python3',str(out/'final-evidence.py')],cwd=str(root),rootHead=state['rootHead'],baseline='8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3',restoration=ref(out/'restoration.json'),sourceEpoch=ref(out/'source-epoch-disposition.json'),sourceFileCount=len(frozen),boundSourceCount=71,runs=runs,parentChecks=dict(nativeAndCaller=ref(review/'parent-final-controls-order/report.json'),count=30,sourceEpoch=ref(out/'focused-final-order/source.json'),sourceDisposition=ref(out/'gitlink-order-source-disposition.json'),binaryDiff=ref(review/'parent-binary-diff/report.json'),nativeRetention=ref(review/'parent-native-retention-audit-order.json'),state=ref(review/'parent-state-audit-order.json'),acceptance=ref(review/'parent-acceptance-audit.json'),matrix=ref(review/'parent-matrix-audit.json')),monitoring=dict(command=['python3',str(out/'monitor-final.py')],record=ref(out/'progress-final.jsonl'),lastSample=last_sample,writer='one-shot command exited after each sample; no background writer'),retainedPriorCombined=len(old_cases),newCombinedCases=len(current)-len(old_cases),retainedPackagingCases=len(packaging),mutationProbes=dict(record=ref(out/'mutations-final-order/mutation-probes.json'),runs=len(mutants),demonstratedGuards=len(mutants),targetedFailures=len(failures),setupErrors=0,disposition=ref(out/'mutation-disposition.json'),finalPassingAudit=ref(out/'retained-tests-and-mutation-audit.json')),held=dict(record=ref(guards/'held-commands.json'),count=18,result='BLOCKED',exitCode=2,blocker='ENTRY_QUALIFICATION_PENDING'),completion=dict(record=ref(guards/'completion-commands.json'),passed=2,operationalBytesRestored=True),binaryDiffs=ref(out/'binary-diffs/manifest.json'),gitObjectDiffs=ref(out/'native-capture-fixture-final-order/object-diff-proof.json'),nativeCapture=ref(out/'native-capture-fixture-final-order/provenance.json'),helperToolchain=ref(out/'helper-toolchain-final-order/provenance.json'),retainedHistory=ref(out/'retained-history.json'),runtime=ref(guards/'runtime-absence.json'),preservedScenarioMatrix=ref(guards/'preserved-scenario-matrix.json'),runtimeAndUserState=ref(guards/'runtime-and-user-state.json'),commitlint=ref(out/'commitlint-validation.json'),implementationInventory=ref(out/'implementation-inventory.json'),broadLane=dict(rerun=False,reason='Spec prohibits repeating established broad baseline lane',retained=ref(root/'module/validation/epic-16-prerequisite-implementation/verification-final.json'),unresolved=['REFRESH_UV_DRIFT package-refresh baseline','Story10.4 guidance-binding baseline'],gateWeakened=False),orchestration=dict(skillRead=True,skillReadDisposition=load(out/'restoration.json')['orchestration'],workflowRendered=False,secondaryWorkflowInvoked=False,childAgentSpawned=False,completionOrchestration=False,actualRepositoryStageCommitPush=False,dependencyUpdates=False),remaining=['Parent independent audit, review and completion orchestration','Actual Platform integration qualification and seventeen adoption findings','Provider/environment parity, recovery drills, operator/deputy coverage and capacity/waiver decisions','Human SM-2 and separate owner entry; Story16.1 remains draft/backlog with ACTIVE hold'],artifacts=artifacts)
(review/'verification.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(dict(runs=runs,mutations=len(mutants),targetedFailures=len(failures),artifacts=len(artifacts),verifiedHistory=len(previous),verification=ref(review/'verification.json')),indent=2))
