from pathlib import Path
import hashlib,json,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/implementation'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
load=lambda p:json.loads(p.read_bytes())
before=load(out/'before.json')['files'];final=load(out/'focused-final/source.json')['files']
assert all(sha(root/p)==h for p,h in final.items())
changed=[p for p,h in before.items() if sha(root/p)!=h]
assert len(changed)==9,changed
run=load(out/'focused-final/command.json');assert run['exitCode']==0 and run['sourceUnchanged']
junit=ET.parse(out/'focused-final/result.xml').getroot();cases=junit.findall('.//testcase')
assert len(cases)==43 and not junit.findall('.//failure') and not junit.findall('.//error') and not junit.findall('.//skipped')
assert sum('test_round14_' in c.attrib['name'] for c in cases)==15
parent=load(out/'parent-input-recheck/report.json');assert len(parent['checks'])==5
binding=load(root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json');assert len(binding['sources'])==71 and binding['entry'] is None
assert all(sha(root/r['path'])==r['sha256'] for r in binding['sources'])
old_binding=load(out/'before/_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json');old_hashes={r['path']:r['sha256'] for r in old_binding['sources']}
changed_bindings=[r['path'] for r in binding['sources'] if r['sha256']!=old_hashes[r['path']]]
assert len(changed_bindings)==6
prior_path=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/prior-evidence-inventory.json';prior=load(prior_path)['files']
assert all(sha(root/r['path'])==r['sha256'] for r in prior)
commands=[]
for name,argv in [('diff-check',['git','diff','--check','--',*changed]),('index-empty',['git','diff','--cached','--quiet']),('head',['git','rev-parse','HEAD'])]:
 p=subprocess.run(argv,cwd=root,capture_output=True);(out/(name+'.stdout')).write_bytes(p.stdout);(out/(name+'.stderr')).write_bytes(p.stderr)
 commands.append(dict(argv=argv,cwd=str(root),exitCode=p.returncode));assert p.returncode==0
(out/'checks.json').write_text(json.dumps(dict(commands=commands,changedPaths=changed,changedBindings=changed_bindings,sourceCount=len(final),sourceUnchanged=True,boundSourceCount=71,entry=None,priorEvidence=dict(path=str(prior_path.relative_to(root)),sha256=sha(prior_path),count=len(prior),unchanged=True)),indent=2)+'\n')
artifacts=[dict(path=str(p.relative_to(root)),sha256=sha(p)) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='patch-verification.json']
report=dict(scope='Iteration 14 patch-only focused verification; full verification remains with parent.',changedPaths=changed,focused=dict(path=str((out/'focused-final').relative_to(root)),passed=43,newCases=15,existingDefaultControls=28,exitCode=0,sourceSha256=sha(out/'focused-final/source.json'),sourceUnchanged=True),exactParentNativeRecheck=dict(path=str((out/'parent-input-recheck/report.json').relative_to(root)),checks=5),priorEvidenceUnchanged=len(prior),earlierAttempt=dict(path=str((out/'focused').relative_to(root)),passed=42,failed=1,correction='Test expected the word Vararg; corrected to the actual pinned runner exception type System.NotSupportedException. Production unchanged.'),artifacts=artifacts)
(out/'patch-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(passed=43,exactParentNativeInputs=5,priorEvidenceUnchanged=len(prior),artifactCount=len(artifacts),indexSha256=sha(out/'patch-verification.json'),sourceSha256=report['focused']['sourceSha256'],changedPaths=changed)))
