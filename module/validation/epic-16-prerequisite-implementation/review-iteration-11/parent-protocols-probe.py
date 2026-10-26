from pathlib import Path
import importlib.util,json,hashlib,subprocess,sys,tempfile,shutil
from copy import deepcopy
sys.dont_write_bytecode=True
root=Path.cwd();sp=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(sp);sp.loader.exec_module(t)
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-protocol-proof';out.mkdir(parents=True,exist_ok=True)
schema=json.loads((root/t.tooling.SCHEMA).read_bytes());results=[]
def retain(name,value):(out/name).write_text(json.dumps(value,indent=2)+'\n')
def validate(scenario,data):t.tooling.jsonschema.Draft202012Validator(schema['$defs'][scenario]).validate(data)
for scenario,claim,field in [('AC-16.1-04','R11B8','acknowledgements'),('AC-16.3-04','R11B9','concurrentRecovery')]:
 data=deepcopy(t.OBSERVATIONS[scenario]);validate(scenario,data);checks=t.tooling.measurements.evaluate(scenario,data);retain(claim+'-accepted-observations.json',data)
 report=dict(id=claim,schemaAccepted=True,semanticChecks=checks,capturedFields=sorted(data[field][0]) if isinstance(data[field],list) else {k:sorted(v) if isinstance(v,dict) else sorted(v[0]) if isinstance(v,list) and v and isinstance(v[0],dict) else None for k,v in data[field].items()})
 if claim=='R11B8':
  report['commitFields']=sorted(data[field][0]['durableCommit']);report['acknowledgementFields']=sorted(data[field][0]['acknowledgement'])
 else:report['wireOperationPhases']=[v['phase'] for v in data['wireObservation']['operations']]
 results.append(report)
# Each CLI swallows a typo import from an otherwise present declared entry module.
for story in ('16_1','16_2','16_3'):
 work=Path(tempfile.mkdtemp(prefix='epic16-r11-import-'));wrapper='verify_story_'+story+'.py';shutil.copyfile(root/'_bmad/scripts'/wrapper,work/wrapper)
 (work/'epic16_tooling.py').write_text('import epic16_typo\n')
 run=subprocess.run([sys.executable,str(work/wrapper)],cwd=work,capture_output=True)
 (out/(wrapper+'.stdout')).write_bytes(run.stdout);(out/(wrapper+'.stderr')).write_bytes(run.stderr)
 result=json.loads(run.stdout);assert run.returncode==2 and result['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE']
 results.append(dict(id='R11B6',wrapper=wrapper,cwd=str(work),argv=[sys.executable,str(work/wrapper)],exitCode=run.returncode,output=result))
# Multi-resource erase: repair the reviewer's omitted key and content digest first.
data=deepcopy(t.OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][0];lifecycle=t.tooling.measurements.lifecycle;relationships=t.tooling.measurements.relationships
added=json.loads(json.dumps(row['capturedObjects'][0]).replace('conversation-a','conversation-b'))
added['key']=relationships.conversation_key(row['tenant'],'conversation-b');added['storedMetadataSha256']=t.digest(added['storedMetadataBytes'].encode())
row['capturedObjects'].append(added);row['deletionTargets'].append(dict(row['deletionTargets'][0],key=added['key']));row['deletedTargets']=deepcopy(row['deletionTargets'])
relationships.erase_targets(row)
# The canonical fixture refresh completes the stages before reaching the defective query.
try:t.round8_refresh(row)
except ValueError as error:assert str(error)=='actual dispatch references match control index',str(error)
else:raise AssertionError('Expected the current helper to contradict itself')
for stage in row['atomicity']['stages']:
 stage['cas']['componentTransactions']=[dict(kind=p['kind'],resource=p['resource'],transactionId=stage['cas']['transactionId']) for p in stage['after']['components']]
 queries=[]
 for s in row['atomicity']['stages']:
  snapshot=s['after'];inputs=dict(tenantExists=True,authorized=True,providerState='Available',sourceGap=False)
  reads=[dict(kind='index',key=snapshot['index']['key'],generation=snapshot['index']['generation'])]+[dict(kind=p['kind'],key=p['key'],resource=p['resource'],generation=p['generation']) for p in snapshot['components']]
  queries.append(dict(captureVersion=1,stage=s['id'],requestId='query-'+s['id'],executed=True,completed=True,order=s['cas']['order']+2,tenant=snapshot['tenant'],observedTransactionId=s['cas']['transactionId'],snapshot=deepcopy(snapshot),inputs=inputs,returned=dict(status='Rebuilding',reason='rebuilding',content=[]),reads=reads,writes=[],recoveryCalls=[]))
 row['queries']=queries
validate('AC-16.2-04',data);relationships.erase_targets(row);lifecycle.lifecycle_transaction(row);t.tooling.measurements.erase_deliveries(row)
try:t.tooling.measurements.evaluate('AC-16.2-04',data)
except ValueError as error:assert str(error)=='actual dispatch references match control index',str(error)
else:raise AssertionError('Expected multi-resource entry-query failure')
retain('R11E2-closed-two-resource-capture.json',data)
results.append(dict(id='R11E2',schemaAccepted=True,eraseTargetsAccepted=True,allLifecycleTransactionsAccepted=True,eraseDeliveriesAccepted=True,evaluatorError='actual dispatch references match control index',indexDispatch=row['atomicity']['stages'][0]['after']['index']['dispatchReferences'],componentDispatch=[p['value'] for p in row['atomicity']['stages'][0]['after']['components'] if p['kind']=='dispatch']))
# Genuine pinned xUnit contradictory Fact+Theory discovery, no evidence assembly execution.
work=Path('/tmp/epic16-edge-fact-jdhey1nj');binary=work/'bin/Debug/net10.0/Probe.dll'
admitted=t.tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Probe.TestClass','Scenario01')
argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Probe.TestClass.Scenario01','-trx',str(work/'parent-result.trx')]
run=subprocess.run(argv,cwd=work,capture_output=True,timeout=30)
assert run.returncode==1 and b'multiple [Fact]-derived attributes' in run.stdout+run.stderr
for name,path in [('R11E1-Tests.cs',work/'Tests.cs'),('R11E1-Probe.csproj',work/'Probe.csproj'),('R11E1-Probe.dll.bytes',binary),('R11E1-result.trx',work/'parent-result.trx'),('R11E1-global.json',work/'global.json')]:shutil.copyfile(path,out/name)
(out/'R11E1-run.stdout').write_bytes(run.stdout);(out/'R11E1-run.stderr').write_bytes(run.stderr)
results.append(dict(id='R11E1',admitted=admitted,cwd=str(work),argv=argv,exitCode=run.returncode))
retain('results.json',results)
retain('manifest.json',dict(scope='Synthetic independent counterexamples; no runtime qualification or authority.',artifacts=[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']))
print(json.dumps(results,indent=2))
