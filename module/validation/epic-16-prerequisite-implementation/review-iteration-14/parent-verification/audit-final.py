from pathlib import Path
import hashlib,json,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14';runs=base/'parent-verification';old=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();load=lambda p:json.loads(p.read_bytes())
def passing(p):
 cases=list(ET.parse(p).iter('testcase'));assert cases
 assert not any(x.tag in ('error','failure','skipped') for c in cases for x in c)
 ids={(c.attrib['classname'],c.attrib['name']) for c in cases};assert len(ids)==len(cases)
 return ids
combined=passing(runs/'combined/result.xml');legacy=passing(runs/'legacy/result.xml');previous=passing(old/'implementation/combined-final-order/result.xml');previous_legacy=passing(old/'implementation/legacy-final-order/result.xml')
assert previous<=combined and len(previous)==3032;assert legacy==previous_legacy and len(legacy)==72
frozen=load(runs/'combined/source.json')['files'];assert len(frozen)==86
for name,cases in [('combined',combined),('legacy',legacy)]:
 d=runs/name;c=load(d/'command.json');assert c['exitCode']==0 and c['sourceUnchanged'] and c['sourceSha256']==sha(d/'source.json')
 assert c['stdoutSha256']==sha(d/'stdout') and c['stderrSha256']==sha(d/'stderr');assert load(d/'source.json')['files']==frozen
 for p,h in frozen.items():
  assert sha(root/p)==h,p
  archived=d/'source'/p
  if archived.name=='.gitignore':archived=archived.with_name('.gitignore.bytes')
  assert sha(archived)==h,p
binding=load(root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json')
assert len(binding['sources'])==71 and binding['entry'] is None
for row in binding['sources']:assert sha(root/row['path'])==row['sha256']==frozen[row['path']]
prior=load(base/'prior-evidence-inventory.json')['files']
for row in prior:assert sha(root/row['path'])==row['sha256'],row['path']
matrix=load(old/'parent-matrix-audit.json');names={x[1] for x in combined}
for key,value in matrix['matrixRows'].items():
 if 'passedExecutedTests' in value:assert set(value['passedExecutedTests'])<=names,key
assert len(matrix['matrixRows'])==8
for filename in ['package-lock.json','commitlint.config.mjs']:
 assert (root/filename).read_bytes()==subprocess.check_output(['git','show','46ca7bf39b25318418b7145b7ce75bb306471cd0:'+filename],cwd=root)
messages={};refs=0
for p in base.rglob('commitlint.json'):
 for message,result in load(p).items():
  assert result['exitCode']==0,(p,message);messages[message]=result;refs+=1
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)
mutations=load(runs/'mutations/mutation-probes.json');assert len(mutations)==8
mutation_failures=0
for mutation in mutations:
 d=runs/'mutations'/mutation['name'];assert mutation['exitCode']==1 and mutation['tests']['failures']>0 and mutation['tests']['errors']==mutation['tests']['skipped']==0
 assert mutation['originalSha256']==sha(root/mutation['path'])==sha(d/'original.bytes')
 assert mutation['mutatedSha256']==sha(d/'mutated.bytes') and mutation['testSha256']==sha(root/'_bmad/scripts/tests/test_epic16_tooling.py')
 for case in ET.parse(d/'result.xml').iter('testcase'):assert (case.attrib['classname'],case.attrib['name']) in combined
 mutation_failures+=mutation['tests']['failures']
worker=load(base/'implementation/patch-verification.json')
for row in worker['artifacts']:assert sha(root/row['path'])==row['sha256']
focused=passing(base/'implementation/focused-final/result.xml');assert focused<=combined and len(focused)==43
assert load(base/'implementation/focused-final/source.json')['files']==frozen
report=dict(combinedPassed=len(combined),legacyPassed=len(legacy),priorCombinedPreserved=len(previous),newCombinedCases=len(combined-previous),newMutationChecks=len(mutations),targetedMutationFailures=mutation_failures,focusedPassed=len(focused),workerArtifactsVerified=len(worker['artifacts']),allNewCases=sorted(x[1] for x in combined-previous),frozenFiles=len(frozen),boundSources=len(binding['sources']),sourceUnchanged=True,allSourceArchivesVerified=True,priorEvidenceFilesPreserved=len(prior),allEightMatrixRowsPreserved=True,uniqueCommitlintMessages=len(messages),successfulCommitlintReferences=refs,indexEmpty=True,runtimeAuthority='none; held-state audit separate')
(base/'parent-final-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='allNewCases'}))
