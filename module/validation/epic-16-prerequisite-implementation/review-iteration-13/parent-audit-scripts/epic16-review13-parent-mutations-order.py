from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET,sys
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';impl=base/'implementation';directory=impl/'mutations-final-order';records=json.loads((directory/'mutation-probes.json').read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();rows=[];failures=[]
final_run=len(sys.argv)>1 and sys.argv[1]=='final';control=impl/('combined-final-order' if final_run else 'focused-final-order')/'result.xml';passing={n.attrib['name']:n for n in ET.parse(control).iter('testcase')};assert not any(any(c.tag in ('failure','error','skipped') for c in n) for n in passing.values())
for r in records:
 folder=directory/r['name'];a,b=r['old'].encode(),r['new'].encode()
 if r['path'].endswith('.cs'):a=a.replace(b'\n',b'\r\n');b=b.replace(b'\n',b'\r\n')
 original=folder/'original.bytes';mutant=folder/'mutated.bytes';assert sha(original)==r['originalSha256'] and sha(mutant)==r['mutatedSha256'];raw=original.read_bytes();assert raw.count(a)==1 and raw.replace(a,b)==mutant.read_bytes(),r['name'];assert sha(root/r['path'])==r['originalSha256'];assert sha(Path(r['cwd'])/r['path'])==r['mutatedSha256'];assert sha(root/'_bmad/scripts/tests/test_epic16_tooling.py')==r['testSha256'];assert json.loads((folder/'command.json').read_bytes())==r
 cases=list(ET.parse(folder/'result.xml').iter('testcase'));counts=dict(tests=len(cases),failures=sum(n.find('failure') is not None for n in cases),errors=sum(n.find('error') is not None for n in cases),skipped=sum(n.find('skipped') is not None for n in cases));assert counts==r['tests'] and counts['failures']>0 and counts['errors']==counts['skipped']==0 and r['exitCode']==1
 missing=[]
 for n in cases:
  f=n.find('failure')
  if f is not None:failures.append(dict(guard=r['name'],test=n.attrib['name'],message=f.get('message'),type=f.get('type')))
  if n.attrib['name'] not in passing:missing.append(n.attrib['name'])
 assert not final_run or not missing,(r['name'],missing)
 rows.append(dict(name=r['name'],counts=counts,exactFinalTargetAndTestHashes=True,exactOriginalAndMutatedBytes=True,selectedCasesMissingFromControl=missing,streams={s:sha(folder/s) for s in ('stdout','stderr')}))
assert len(rows)==23 and len(failures)==53
report=dict(scope='Final mutation/control audit' if final_run else 'Exact final mutation audit; full combined passing control pending',guards=23,assertionFailures=53,setupErrors=0,skips=0,allFinalTargetsMatch=True,allSelectedCasesPassInFinalCombined=final_run,control=str(control.relative_to(root)),rows=rows,failureMessages=failures)
(base/('parent-mutation-audit-order.json' if final_run else 'parent-mutation-audit-order-early.json')).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('rows','failureMessages')},indent=2));print('Missing from focused control:',sum(len(r['selectedCasesMissingFromControl']) for r in rows))
