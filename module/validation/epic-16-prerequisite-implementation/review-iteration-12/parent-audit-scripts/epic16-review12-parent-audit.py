from pathlib import Path
import hashlib,json,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();base=Path('module/validation/epic-16-prerequisite-implementation/review-iteration-12');directory=base/'implementation'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
original=json.loads((directory/'final-source.json').read_text())
frozen=json.loads((directory/'packaging-source.json').read_text());binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_text());sources=dict(binding=binding,tests=[dict(path=p,sha256=h) for p,h in frozen['files'].items() if '/tests/test_' in p])
assert set(original['files'])==set(frozen['files'])
changed={p for p,h in original['files'].items() if frozen['files'][p]!=h}
binding_path='_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json'
assert changed=={'.gitattributes',binding_path},changed
archive=directory/'combined/source'
for p,h in original['files'].items():assert sha(archive/(p+'.bytes' if p=='.gitignore' else p))==h,p
old_attributes=(archive/'.gitattributes').read_bytes();new_attributes=(root/'.gitattributes').read_bytes()
assert old_attributes.replace(b'module/validation/epic-16-prerequisite-implementation/** -text diff\n',b'module/validation/epic-16-prerequisite-implementation/** -text diff -whitespace\n')==new_attributes
old_binding=json.loads((archive/binding_path).read_bytes())
for row in old_binding['sources']:
 if row['path']=='.gitattributes':row['sha256']=sha(root/'.gitattributes')
assert old_binding==binding
assert json.loads((directory/'combined/command.json').read_bytes())['sourceUnchanged'] is False
for name in ('packaging','legacy'):
 command=json.loads((directory/name/'command.json').read_bytes());assert command['exitCode']==0 and command['sourceUnchanged'] is True,name
 assert json.loads((directory/name/'source-hashes.json').read_bytes())==frozen['files'],name
for p,h in frozen['files'].items():assert sha(p)==h,p
for row in sources['binding']['sources']+sources['tests']:assert sha(row['path'])==row['sha256'],row['path']
assert sources['binding']['entry'] is None
cases={};counts={}
for name in ('combined','packaging','legacy'):
 nodes=list(ET.parse(directory/name/'result.xml').iter('testcase'));assert nodes
 assert not any(any(c.tag in ('failure','error','skipped') for c in node) for node in nodes)
 cases[name]={node.attrib['name']:node for node in nodes};counts[name]=len(nodes)
previous_cases={node.attrib['name'] for node in ET.parse(base.parent/'review-iteration-11/implementation/combined/result.xml').iter('testcase')}
assert previous_cases <= set(cases['combined']),sorted(previous_cases-set(cases['combined']))
assert counts['combined']>2416 and counts['legacy']==72,counts
old_matrix=json.loads(Path('module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-matrix-audit.json').read_text())['matrixRows']
for name,row in old_matrix.items():
 for test in row.get('passedExecutedTests',[]):assert test in cases['combined'] or test in cases['legacy'],test
new_cases=[name for name in cases['combined'] if name.startswith('test_round12_')]
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
matrix={'verificationEpoch':'review-iteration-12 original combined source plus stable current packaging/legacy epoch; implementation and tests unchanged','matrixRows':old_matrix,'newExecutedPassedCases':new_cases,'newCaseCount':len(new_cases),'combinedTests':counts['combined'],'packagingTests':counts['packaging'],'legacyTests':counts['legacy'],'sourceHashesVerified':True,'originalCombinedSourceUnchanged':False,'changedPaths':sorted(changed)}
(base/'parent-matrix-audit.json').write_text(json.dumps(matrix,indent=2)+'\n')
report={'frozenFilesVerified':len(frozen['files']),'sourceHashesVerified':len(sources['binding']['sources']),'testHashesVerified':len(sources['tests']),'counts':counts,'round12ExecutedTests':len(new_cases),'previousCombinedCasesPreserved':len(previous_cases),'newCombinedCases':len(set(cases['combined'])-previous_cases),'allEightMatrixRowsCoveredByExecutedEvidence':True,'freshCheckoutNativeCaptureTestExecuted':True,'heldRuntimeCommands':18,'completionGuards':2,'absentRuntimePaths':54,'disposableCommitMessagesValidated':len(lint['messages']),'protectedFilesPreserved':list(restoration['protectedUnchanged']),'indexEmpty':True,'diffCheckPassed':True,'runtimeHold':'ACTIVE','explicitRuntimePublication':'not-applicable','originalCombinedSourceUnchanged':False,'currentPackagingSourceStable':True,'changedPaths':sorted(changed),'codeAndTestBytesUnchanged':True,'original86ArchivedSourceHashesVerified':True}
(base/'parent-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
