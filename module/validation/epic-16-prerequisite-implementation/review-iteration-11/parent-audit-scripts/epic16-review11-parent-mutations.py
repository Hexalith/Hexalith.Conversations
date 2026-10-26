from pathlib import Path
import json,hashlib,re,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
runs=[]
for path in sorted((base/'implementation').glob('mutations*/mutation-probes.json')):
 runs.extend((path.parent,r) for r in json.loads(path.read_bytes()))
assert runs
control=base/'implementation/combined/result.xml'
final_cases={x.attrib['name']:x for x in ET.parse(control).iter('testcase')} if control.exists() else None
rows=[];latest={}
for directory,run in runs:
 name=run['name'];source=root/run['path'];assert sha(source)==run['originalSha256'],(name,'source epoch')
 for stream in ('stdout','stderr'):assert sha(directory/(name+'.'+stream+'.txt'))==run[stream+'Sha256'],(name,stream)
 cases=list(ET.parse(directory/(name+'.xml')).iter('testcase'));counts=dict(tests=len(cases),failures=sum(x.find('failure') is not None for x in cases),errors=sum(x.find('error') is not None for x in cases),skipped=sum(x.find('skipped') is not None for x in cases))
 assert counts==run['tests'] and counts['errors']==counts['skipped']==0,(name,counts)
 demonstrated=run['exitCode']==1 and counts['failures']>0
 if not demonstrated:assert name=='recovery-execution-caller' and run['exitCode']==5 and counts['tests']==0,(name,'undemonstrated')
 mutant=Path(run['workspace'])/run['path'];assert sha(mutant)==run['mutatedSha256'],name
 if run.get('schemaMaps'):
  assert name=='native-domain-map-openness'
  expected=json.loads(source.read_bytes());properties=expected['$defs']['AC-16.1-04']['properties']['acknowledgements']['items']['properties'];p=properties['durableEvidence']['anyOf'][0]['properties']
  states=[p['sourceState'],p['before']['properties']['projection'],p['after']['properties']['projection'],properties['durableCommit']['anyOf'][0]['properties']['persisted']['properties']['projection'],properties['acknowledgement']['anyOf'][0]['properties']['observedRecord']['properties']['projection']]
  for state in states:
   for field,key in [('members','actor-a'),('configuration','conversations.enabled')]:
    state['properties'][field]['additionalProperties']=False;state['properties'][field]['properties']={key:dict(type='string')}
  assert (json.dumps(expected,indent=2,ensure_ascii=False)+'\n').encode()==mutant.read_bytes()
  trust='_bmad/scripts/epic16_trust.py';expected_trust=re.sub(r"SCHEMA_PIN = '[0-9a-f]+'", "SCHEMA_PIN = '"+run['mutatedSha256']+"'", (root/trust).read_text())
  assert (Path(run['workspace'])/trust).read_text()==expected_trust
 else:
  a,b=run['old'].encode(),run['new'].encode()
  if run['path'].endswith('.cs'):a=a.replace(b'\n',b'\r\n');b=b.replace(b'\n',b'\r\n')
  assert source.read_bytes().count(a)==1 and source.read_bytes().replace(a,b)==mutant.read_bytes(),(name,'exact mutation')
 if final_cases is not None:
  for case in cases:
   current=final_cases[case.attrib['name']];assert not any(c.tag in ('failure','error','skipped') for c in current),(name,case.attrib['name'])
 row=dict(epoch=directory.name,name=name,counts=counts,demonstrated=demonstrated,exactFinalSourceAndMutantBytes=True,allSelectedCasesPassInFinalControl=final_cases is not None);rows.append(row);latest[name]=row
if final_cases is not None:assert all(row['demonstrated'] for row in latest.values()),latest
report=dict(scope='Final source mutation audit' if final_cases else 'Early mutation audit; complete final control run still pending',runs=len(rows),demonstratedRuns=sum(r['demonstrated'] for r in rows),distinctGuards=len(latest),demonstratedDistinctGuards=sum(r['demonstrated'] for r in latest.values()),assertionFailures=sum(r['counts']['failures'] for r in rows),unselectedInitialRuns=sum(r['counts']['tests']==0 for r in rows),errors=0,skipped=0,allMutantCasesPassInFinalControl=final_cases is not None,rows=rows)
target=base/('parent-mutation-audit.json' if final_cases else 'parent-mutation-audit-early.json');target.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))
