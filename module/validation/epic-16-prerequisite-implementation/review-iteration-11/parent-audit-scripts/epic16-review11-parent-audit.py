from pathlib import Path
import hashlib,json,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();base=Path('module/validation/epic-16-prerequisite-implementation/review-iteration-11');directory=base/'implementation'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
frozen=json.loads((directory/'final-source.json').read_text());binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_text());sources=dict(binding=binding,tests=[dict(path=p,sha256=h) for p,h in frozen['files'].items() if '/tests/test_' in p])
for p,h in frozen['files'].items():assert sha(p)==h,p
for row in sources['binding']['sources']+sources['tests']:assert sha(row['path'])==row['sha256'],row['path']
assert sources['binding']['entry'] is None
cases={};counts={}
for name in ('combined','legacy'):
 nodes=list(ET.parse(directory/name/'result.xml').iter('testcase'));assert nodes
 assert not any(any(c.tag in ('failure','error','skipped') for c in node) for node in nodes)
 cases[name]={node.attrib['name']:node for node in nodes};counts[name]=len(nodes)
previous_cases={node.attrib['name'] for node in ET.parse(base.parent/'review-iteration-10/implementation/combined.xml').iter('testcase')}
assert previous_cases <= set(cases['combined']),sorted(previous_cases-set(cases['combined']))
assert counts['combined']>2152 and counts['legacy']==72,counts
old_matrix=json.loads(Path('module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-matrix-audit.json').read_text())['matrixRows']
for name,row in old_matrix.items():
 for test in row.get('passedExecutedTests',[]):assert test in cases['combined'] or test in cases['legacy'],test
new_cases=[name for name in cases['combined'] if name.startswith('test_round11_')]
assert new_cases and 'test_round6_genuine_native_capture_publication_survives_fresh_checkout' in cases['combined']
held=json.loads((directory/'held-commands.json').read_text());assert len(held['commands'])==18
for i,row in enumerate(held['commands']):
 assert row['exitCode']==2 and row['result']=='BLOCKED' and row['blockers']==['ENTRY_QUALIFICATION_PENDING']
 for stream in ('stdout','stderr'):assert sha(directory/f'held-{i:02}.{stream}.txt')==row[stream+'Sha256']
guards=json.loads((directory/'completion-commands.json').read_text());assert len(guards)==2
for i,row in enumerate(guards,1):
 assert row['exitCode']==0 and row['priorBytesRestored']
 for stream in ('stdout','stderr'):assert sha(directory/f'completion-{i}.{stream}.txt')==row[stream+'Sha256']
runtime=json.loads((directory/'runtime-absence.json').read_text());assert runtime['entry'] is None and runtime['trustedEntries']=={}
assert len(runtime['absentPaths'])==54 and not any((root/p).exists() for p in runtime['absentPaths'])
restoration=json.loads((directory/'runtime-and-user-state.json').read_text())
for p,h in restoration['protectedUnchanged'].items():assert sha(p)==h,p
lint=json.loads((directory/'commitlint-validation.json').read_text())
for message,refs in lint['messages'].items():
 assert refs
 for ref in refs:assert json.loads((root/ref).read_text())[message]['exitCode']==0,(message,ref)
for key in ('policy','packageLock'):assert sha(root/lint[key]['path'])==lint[key]['sha256'],key
assert not subprocess.check_output(['git','diff','--cached','--name-only'])
assert subprocess.run(['git','diff','--check'],capture_output=True).returncode==0
matrix={'verificationEpoch':'review-iteration-11 final stable source','matrixRows':old_matrix,'newExecutedPassedCases':new_cases,'newCaseCount':len(new_cases),'combinedTests':counts['combined'],'legacyTests':counts['legacy'],'sourceHashesVerified':True}
(base/'parent-matrix-audit.json').write_text(json.dumps(matrix,indent=2)+'\n')
report={'frozenFilesVerified':len(frozen['files']),'sourceHashesVerified':len(sources['binding']['sources']),'testHashesVerified':len(sources['tests']),'counts':counts,'round11ExecutedTests':len(new_cases),'previousCombinedCasesPreserved':len(previous_cases),'newCombinedCases':len(set(cases['combined'])-previous_cases),'allEightMatrixRowsCoveredByExecutedEvidence':True,'freshCheckoutNativeCaptureTestExecuted':True,'heldRuntimeCommands':18,'completionGuards':2,'absentRuntimePaths':54,'disposableCommitMessagesValidated':len(lint['messages']),'protectedFilesPreserved':list(restoration['protectedUnchanged']),'indexEmpty':True,'diffCheckPassed':True,'runtimeHold':'ACTIVE','explicitRuntimePublication':'not-applicable'}
(base/'parent-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
