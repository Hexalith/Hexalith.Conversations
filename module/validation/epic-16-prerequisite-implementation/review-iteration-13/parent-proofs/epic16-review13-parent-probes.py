from pathlib import Path
import sys,shutil,tempfile,json,hashlib,subprocess
from unittest.mock import patch
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_tooling as t
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-proofs';base.mkdir(parents=True,exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest()
p=json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation/native-capture-fixture-packaging/provenance.json').read_bytes())
work=Path(tempfile.mkdtemp(prefix='epic16-r13-parent-'));repo=work/'fixture';shutil.copytree(p['sourceFixture'],repo)
binding=json.loads((repo/t.BINDING).read_bytes());pin=binding['entry']['sha256'];spec=t.spec_path('runtime','16.2')
def retain(name,value):
 target=base/name;target.write_bytes(value if isinstance(value,bytes) else (json.dumps(value,indent=2)+'\n').encode());return dict(path=str(target.relative_to(root)),sha256=sha(target.read_bytes()))
observed=[]
with patch.object(t,'TRUSTED_ENTRIES',{'fixture-entry.json':pin}), patch.object(t,'CONTRACT_PINS',dict(t.CONTRACT_PINS,**{t.contract_path('16.2'):sha((repo/t.contract_path('16.2')).read_bytes())})):
 result,j,m=t.generate(repo,'16.2',p['candidate'],'runtime',publication=True,verify_spec=spec)
 assert result['result']=='PASS',result
 retain('B4-baseline.json',result)
 original=t.capture.inventory
 def changed(reader,story,rows,need):
  path=repo/rows[0]['bindings']['native']['path'];before=path.read_bytes();path.write_bytes(before+b'\n<!-- changed after five scenario evaluations -->\n')
  observed.append(dict(path=str(path.relative_to(repo)),oldSha256=sha(before),newSha256=sha(path.read_bytes()),receiptSha256=rows[0]['bindings']['native']['sha256']))
  retain('B4-native-before.trx',before);retain('B4-native-after.trx',path.read_bytes())
  return original(reader,story,rows,need)
 with patch.object(t.capture,'inventory',changed):
  result,j,m=t.generate(repo,'16.2',p['candidate'],'runtime',write=True,publication=True,verify_spec=spec)
 assert result['result']=='PASS' and j and m,result
 assert observed[0]['oldSha256']==observed[0]['receiptSha256']!=observed[0]['newSha256']
 retain('B4-stale-native-final-record.json',result)
 report=retain('B4-proof.json',dict(fixture=str(repo),candidate=p['candidate'],originalNativeFixture=p['sourceFixture'],result=result['result'],pairWritten=all((repo/name).read_bytes()==raw for name,raw in zip(t.final_paths('runtime','16.2'),(j,m))),observations=observed,method='Change TRX between the real five scenario evaluations and real final inventory; all other caller guards run unmodified',scope='Disposable fixture only; no runtime authority'))
 print(json.dumps(report))
 workspace,assembly=t.native_helper._helper()
 real=t.native_helper.subprocess.run
 def host_failure(argv,**kwargs):
  if argv==['dotnet',str(assembly)]:return subprocess.CompletedProcess(argv,150,stdout=b'',stderr=b'You must install or update .NET to run this application.')
  return real(argv,**kwargs)
 helper=[]
 with patch.object(t.native_helper.subprocess,'run',host_failure):
  try:t.native_helper._run(dict(operation='provenance'))
  except Exception as exc:helper.append(dict(type=type(exc).__name__,message=str(exc)))
  # Restore changed TRX first; runtime classification must not fail an unrelated digest.
  (repo/observed[0]['path']).write_bytes((base/'B4-native-before.trx').read_bytes())
  t.native_helper.inspect_product.cache_clear()
  evaluated=t.evaluate(repo,'16.2','AC-16.2-01',p['candidate'],'runtime',publication=True,verify_spec=spec)
 assert helper==[dict(type='ValueError',message='invalid native evidence')],helper
 assert evaluated['result']=='FAIL',evaluated
 print(json.dumps(retain('B5-proof.json',dict(hostExit=150,hostStderr='You must install or update .NET to run this application.',helper=helper,actualRuntimeEvaluation=evaluated))))
 retain('B6-required-products.json',dict(requiredProducts=t.products.PRODUCTS,missingRelevantAssemblies=['Hexalith.EventStore.Server','Hexalith.Commons.TenantAccess'],successfulRealGeneratorControl='B4-baseline.json',scope='The current successful controlled runtime fixture inventory does not bind these two actual behavior owners; source caller ownership is checked separately.'))
retain('manifest.json',dict(workspace=str(work),artifacts=[dict(path=str(f.relative_to(root)),sha256=sha(f.read_bytes())) for f in sorted(base.iterdir()) if f.is_file() and f.name!='manifest.json']))
