from pathlib import Path
from copy import deepcopy
from unittest.mock import patch
import sys,json,hashlib,subprocess,shutil
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
import epic16_measurements as measurements
import jsonschema
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-proofs';out.mkdir(parents=True,exist_ok=True)
observations=json.loads((root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes());schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes())
results=[]
def save(name,value):
 (out/name).write_text(json.dumps(value,indent=2)+'\n')
def accept(scenario,data):
 jsonschema.Draft202012Validator(schema['$defs'][scenario]).validate(data)
 return measurements.evaluate(scenario,data)
for claim,scenario,field in [('R12B4','AC-16.1-03','reverseSaves'),('R12B5','AC-16.1-01','authorization'),('R12B5','AC-16.1-02','authorization'),('R12B6','AC-16.1-04','acknowledgements')]:
 data=deepcopy(observations[scenario]);checks=accept(scenario,data);save(claim+'-'+scenario+'.json',data)
 rows=data[field][4:] if claim=='R12B6' else data[field]
 results.append(dict(id=claim,scenario=scenario,schemaAccepted=True,checks=checks,fields=[sorted(r) for r in rows],note='Closed capture accepts the complete scenario without the challenged execution/state/consumer relationship. Source-derived replay and unrelated other scenario observations remain intact.'))
# Reused lifecycle transaction, without changing existing correlated records.
for index in range(3):
 data=deepcopy(observations['AC-16.2-04']);row=data['lifecycle'][1]
 delivery=next(d for d in row['catchUp']['deliveries'] if d['enqueue']['transactionId'] is not None)
 transaction=row['atomicity']['stages'][index]['cas']['transactionId'];enqueue=delivery['enqueue']
 enqueue['transactionId']=enqueue['indexTransactionId']=transaction
 for part in enqueue['componentTransactions']:part['transactionId']=transaction
 delivery['operations']['commit']['transactionId']=delivery['operations']['acknowledgement']['transactionId']=transaction
 checks=accept('AC-16.2-04',data);save(f'R12E1-stage-{index}.json',data)
 results.append(dict(id='R12E1',stage=index,schemaAccepted=True,checks=checks,transactionId=transaction,stageOrder=row['atomicity']['stages'][index]['cas']['order'],enqueueOrder=enqueue['order']))
# Retain and independently inspect the exact compiled reviewer counterexamples.
probes=[('R12B1',Path('/tmp/epic16-review12-native-pu9vua9v'),Path('bin/Debug/net10.0/Probe.dll')),('R12B2',Path('/tmp/epic16-review12-return-l8clbmcc'),Path('probe/bin/Debug/net10.0/Probe.dll')),('R12B3',Path('/tmp/epic16-review12-params-557y69en'),Path('probe/bin/Debug/net10.0/Probe.dll'))]
for claim,work,rel in probes:
 target=out/claim;target.mkdir(exist_ok=True)
 selected=[p for p in work.rglob('*') if p.is_file() and 'obj' not in p.parts and ('bin' not in p.parts or p.name in ('Probe.dll','System.Threading.Tasks.dll','External.dll','Fake.Attributes.dll','Probe.deps.json','Probe.runtimeconfig.json'))]
 for p in selected:
  dest=target/p.relative_to(work)
  if dest.suffix=='.dll':dest=dest.with_suffix('.dll.bytes')
  dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
 admitted=native.inspect_assembly((work/rel).read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')
 result=dict(id=claim,assembly=str(work/rel),assemblySha256=hashlib.sha256((work/rel).read_bytes()).hexdigest(),admitted=admitted)
 if claim=='R12B1':
  try:result['productAdmitted']=native.inspect_product((work/rel).read_bytes(),'a'*40)
  except ValueError as error:result['productRejected']=str(error)
 if claim in ('R12B2','R12B3'):
  argv=['dotnet',str(work/rel),'-automated','sync','-method','Synthetic.TestClass.Scenario01']
  run=subprocess.run(argv,cwd=work/rel.parent,capture_output=True,timeout=30)
  (target/'parent-run.stdout').write_bytes(run.stdout);(target/'parent-run.stderr').write_bytes(run.stderr)
  result.update(argv=argv,cwd=str(work/rel.parent),runnerExit=run.returncode)
  if claim=='R12B3':assert run.returncode==1 and b'constructor parameters did not have matching fixture data' in run.stdout+run.stderr
  else:assert run.returncode==0
 results.append(result)
# Failure diagnostics with a real reachable subprocess failure shape.
for stage in ('sdk','build'):
 native._helper.cache_clear();calls=[]
 def run(argv,**kw):
  calls.append(argv)
  if argv==['dotnet','--version']:
   return subprocess.CompletedProcess(argv,41 if stage=='sdk' else 0,b'' if stage=='sdk' else b'10.0.402\n',b'SDK resolver diagnostic on stderr' if stage=='sdk' else b'')
  assert argv[:2]==['dotnet','build']
  return subprocess.CompletedProcess(argv,42,b'',b'Compiler diagnostic exclusively on stderr')
 with patch.object(native.subprocess,'run',side_effect=run):
  try:native._helper()
  except native.HelperUnavailable as error:message=str(error)
  else:raise AssertionError('Expected prerequisite failure')
 results.append(dict(id='R12B9',stage=stage,calls=calls,message=message,stderrOmitted='diagnostic' not in message,exitOmitted=str(41 if stage=='sdk' else 42) not in message))
 native._helper.cache_clear()
save('results.json',results)
shutil.copyfile('/tmp/epic16-review12-parent-probes.py',out/'probe.py')
save('manifest.json',dict(scope='Independent synthetic parent counterexamples, no runtime authority.',artifacts=[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']))
print(json.dumps([{k:v for k,v in r.items() if k not in ('admitted','productAdmitted','fields')} for r in results],indent=2))
