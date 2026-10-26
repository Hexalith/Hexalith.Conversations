from pathlib import Path
from copy import deepcopy
import sys,json,hashlib
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
import epic16_measurements as m
import jsonschema
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12'
schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());obs=json.loads((root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes());results=[]
source_paths=['_bmad/scripts/epic16_native.py','_bmad/scripts/epic16_schedules.py','_bmad/scripts/epic16_measurements.py','_bmad/scripts/epic16_lifecycle.py','_bmad/scripts/epic16_native_helper/Program.cs','_bmad/scripts/epic16_native_helper/ProductMetadata.cs','_bmad/scripts/epic16_native_helper/TestConstruction.cs','_bmad/schemas/epic-16-tooling-v1.schema.json','_bmad/scripts/tests/fixtures/epic16-observations-v1.json']
source_hashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in source_paths}
for name,operation,path,positive in [
 ('local-test-stamp','test',base/'parent-proofs/R12B1/bin/Debug/net10.0/Probe.dll.bytes',False),
 ('fake-task','test',base/'parent-proofs/R12B2/probe/bin/Debug/net10.0/Probe.dll.bytes',False),
 ('foreign-params','test',base/'parent-proofs/R12B3/probe/bin/Debug/net10.0/Probe.dll.bytes',False),
 ('external-product-stamp','product',base/'parent-product-stamp-proof/retained/product/bin/Debug/net10.0/Product.dll.bytes',False),
 ('genuine-test','test',base/'implementation/exploratory-b/native-r12/genuine/probe/bin/Debug/net10.0/Evidence.dll.bytes',True),
 ('genuine-product','product',base/'implementation/exploratory-b/native-r12/genuine/probe/bin/Debug/net10.0/Evidence.dll.bytes',True)]:
 try:
  result=native.inspect_product(path.read_bytes(),'a'*40) if operation=='product' else native.inspect_assembly(path.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')
  assert positive,(name,result);result='accepted'
 except ValueError as error:
  assert not positive,(name,str(error));result=str(error)
 results.append(dict(name=name,result=result,artifact=str(path.relative_to(root)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
def evaluate(name,scenario,data,positive):
 jsonschema.Draft202012Validator(schema['$defs'][scenario]).validate(data)
 try:checks=m.evaluate(scenario,data);assert positive,(name,checks);result='accepted'
 except ValueError as error:assert not positive,(name,str(error));result=str(error)
 results.append(dict(name=name,result=result,schemaAccepted=True))
for scenario in ('AC-16.1-01','AC-16.1-02','AC-16.1-03','AC-16.1-04','AC-16.2-04'):evaluate('baseline-'+scenario,scenario,deepcopy(obs[scenario]),True)
evaluate('actual-native-unknown-role','AC-16.1-01',json.loads((base/'parent-access-parity-proof/unknown-role.json').read_bytes()),True)
evaluate('native-control-principal','AC-16.1-01',json.loads((base/'parent-access-parity-proof/control-principal.json').read_bytes()),False)
evaluate('native-malformed-tenant','AC-16.1-01',json.loads((base/'parent-access-tenant-proof/accepted-capture.json').read_bytes()),False)
for stage in range(3):evaluate('lifecycle-transaction-'+str(stage),'AC-16.2-04',json.loads((base/f'parent-proofs/R12E1-stage-{stage}.json').read_bytes()),False)
for name,scenario,path in [('reverse-save','AC-16.1-03',('reverseSaves',2,'execution','saves',1)),('access-response','AC-16.1-02',('authorization',2,'capture','response')),('failed-attempt','AC-16.1-04',('acknowledgements',5,'failureEvidence','attempts',2))]:
 data=deepcopy(obs[scenario]);v=data
 for key in path:v=v[key]
 v['executed']=False;evaluate(name+'-unexecuted',scenario,data,False)
changed=[p for p,h in source_hashes.items() if hashlib.sha256((root/p).read_bytes()).hexdigest()!=h]
report=dict(scope='Parent independent corrected caller/native controls; no runtime authority',controls=results,count=len(results),sourceHashes=source_hashes,changedDuringRun=changed)
(base/'parent-final-controls.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(count=len(results),changedDuringRun=changed,results=[dict(name=r['name'],result=r['result']) for r in results]),indent=2))
