from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12';impl=base/'implementation';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();runs=[]
for p in sorted(impl.glob('mutations*/mutation-probes.json')):runs.extend((p.parent,r) for r in json.loads(p.read_bytes()))
control=impl/'combined/result.xml';final={x.attrib['name']:x for x in ET.parse(control).iter('testcase')} if control.exists() else None
rows=[];latest={};failures=[]
for directory,record in runs:
 name=record['name'];folder=directory/name;original=folder/'original.bytes';mutant=folder/'mutated.bytes';assert sha(original)==record['originalSha256'] and sha(mutant)==record['mutatedSha256']
 assert json.loads((folder/'command.json').read_bytes())==record
 a,b=record['old'].encode(),record['new'].encode()
 if record['path'].endswith('.cs'):a=a.replace(b'\n',b'\r\n');b=b.replace(b'\n',b'\r\n')
 raw=original.read_bytes();assert raw.count(a)==1 and raw.replace(a,b)==mutant.read_bytes(),(name,'exact mutation')
 assert sha(Path(record['cwd'])/record['path'])==record['mutatedSha256'],name
 assert sha(root/'_bmad/scripts/tests/test_epic16_tooling.py')==record['testSha256'],(name,'test source')
 cases=list(ET.parse(folder/'result.xml').iter('testcase'));counts=dict(tests=len(cases),failures=sum(x.find('failure') is not None for x in cases),errors=sum(x.find('error') is not None for x in cases),skipped=sum(x.find('skipped') is not None for x in cases))
 assert counts==record['tests'] and counts['failures']>0 and counts['errors']==counts['skipped']==0 and record['exitCode']==1,(name,counts)
 for case in cases:
  failure=case.find('failure')
  if failure is not None:failures.append(dict(guard=name,test=case.attrib['name'],message=failure.get('message'),type=failure.get('type')))
  if final is not None:
   current=final[case.attrib['name']];assert not any(c.tag in ('failure','error','skipped') for c in current),(name,case.attrib['name'])
 final_bytes=sha(root/record['path'])==record['originalSha256']
 if not final_bytes:assert directory.name=='mutations' and record['path'].endswith('/Program.cs') and name in ('test-source-owner','test-source-blob','async-return-owner')
 row=dict(epoch=directory.name,name=name,counts=counts,exactOriginalAndMutantBytes=True,originalMatchesFinalTarget=final_bytes,streams={s:sha(folder/s) for s in ('stdout','stderr')},allSelectedCasesPassInFinalControl=final is not None)
 rows.append(row);latest[name]=row
assert len(rows)==42 and len(latest)==39 and all(r['originalMatchesFinalTarget'] for r in latest.values())
assert len(failures)==199
report=dict(scope='Final mutation audit' if final else 'Early exact mutation audit; final passing control still running',runs=len(rows),distinctGuards=len(latest),assertionFailures=len(failures),errors=0,skipped=0,allGuardsHaveExactFinalTarget=True,allMutantCasesPassInFinalControl=final is not None,rows=rows,failureMessages=failures)
p=base/('parent-mutation-audit.json' if final else 'parent-mutation-audit-early.json');p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('rows','failureMessages')},indent=2));print('Distinct failure messages:');print('\n'.join(sorted({str(x['message']) for x in failures})[:12]))
