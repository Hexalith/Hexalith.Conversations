from pathlib import Path
import sys,json,hashlib,subprocess,tempfile,shutil
from unittest.mock import patch
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_tooling as t
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13'
out=base/'parent-final-controls';out.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest()
source=json.loads((base/'implementation'/sys.argv[2]/'source.json').read_bytes())['files']
assert all(sha((root/p).read_bytes())==h for p,h in source.items())
rows=[]
def save(name,value):
 p=out/name;p.write_bytes(value if isinstance(value,bytes) else (json.dumps(value,indent=2)+'\n').encode());return dict(path=str(p.relative_to(root)),sha256=sha(p.read_bytes()))
def inspect(label,path,name,positive):
 content=path.read_bytes()
 try:
  result=t.native_helper.inspect_assembly(content,'a'*40,name,'Scenario01');assert positive,(label,result);status='accepted'
 except ValueError as exc:assert not positive,(label,str(exc));status=str(exc)
 rows.append(dict(label=label,artifact=str(path.relative_to(root)),sha256=sha(content),expectedAccepted=positive,result=status))
for label in ('constructor-default','return-kind','collection-blob'):
 for kind,positive in [('valid',True),('mutated',False)]:inspect(label+'-'+kind,base/'reviewer-proofs'/label/(kind+'.dll.bytes'),'Synthetic.TestClass',positive)
controls=json.loads((base/'parent-default-controls/manifest.json').read_bytes())
for row in controls['rows']:inspect(row['test'],base/'parent-default-controls/Evidence.dll.bytes',row['test'].rsplit('.',1)[0],row['result']=='test-passed')
for sign in (0,1,127,128,255):inspect('decimal-sign-'+str(sign),base/'parent-decimal-sign/Evidence.dll.bytes','Synthetic.Sign'+str(sign),True)
for label in ('valid','string','object'):inspect('nullable-'+label,base/'parent-nullable-constraint'/(label+'.dll.bytes'),'Synthetic.NullableNull',label=='valid')
fixture=Path(sys.argv[1]);work=Path(tempfile.mkdtemp(prefix='epic16-r13-parent-final-'));repo=work/'fixture';shutil.copytree(fixture,repo)
binding=json.loads((repo/t.BINDING).read_bytes());pin=binding['entry']['sha256'];contract=t.contract_path('16.2');spec=t.spec_path('runtime','16.2')
record=json.loads((repo/t.final_paths('runtime','16.2')[0]).read_bytes());candidate=record['candidate']['commit'];observed=[]
with patch.object(t,'TRUSTED_ENTRIES',{'fixture-entry.json':pin}),patch.object(t,'CONTRACT_PINS',dict(t.CONTRACT_PINS,**{contract:sha((repo/contract).read_bytes())})):
 result,j,m=t.generate(repo,'16.2',candidate,'runtime',publication=True,verify_spec=spec)
 save('baseline.json',result);assert result['result']=='PASS',result
 pairs=t.final_paths('runtime','16.2');saved_pair=tuple((repo/p).read_bytes() for p in pairs);original=t.capture.inventory
 def changed(reader,story,scenarios,need):
  path=repo/scenarios[0]['bindings']['native']['path'];before=path.read_bytes();path.write_bytes(before+b'\n<!-- parent final actual TRX drift -->\n');observed.append(dict(path=str(path.relative_to(repo)),oldSha256=sha(before),newSha256=sha(path.read_bytes()),receiptSha256=scenarios[0]['bindings']['native']['sha256']));save('native-before.trx',before);save('native-after.trx',path.read_bytes());return original(reader,story,scenarios,need)
 with patch.object(t.capture,'inventory',changed):result,j,m=t.generate(repo,'16.2',candidate,'runtime',write=True,publication=True,verify_spec=spec)
 save('drift-rejected.json',result);assert result['result']=='FAIL' and 'NATIVE_CAPTURE_DRIFT' in result['blockers'],result
 assert j is None and m is None and tuple((repo/p).read_bytes() for p in pairs)==saved_pair
 rows.append(dict(label='real-generator-final-trx-drift',result='FAIL',blockers=result['blockers'],finalPairUnchanged=True,observations=observed))
 (repo/observed[0]['path']).write_bytes((out/'native-before.trx').read_bytes())
 workspace,assembly=t.native_helper._helper();real=t.native_helper.subprocess.run
 def host_failure(argv,**kwargs):
  if argv==['dotnet',str(assembly)]:return subprocess.CompletedProcess(argv,150,stdout=b'',stderr=b'You must install or update .NET to run this application.')
  return real(argv,**kwargs)
 with patch.object(t.native_helper.subprocess,'run',host_failure):
  try:t.native_helper._run(dict(operation='provenance'));raise AssertionError('host failure accepted')
  except t.native_helper.HelperUnavailable as exc:message=str(exc)
  t.native_helper.inspect_product.cache_clear();evaluated=t.evaluate(repo,'16.2','AC-16.2-01',candidate,'runtime',publication=True,verify_spec=spec)
 save('host-blocked.json',evaluated);assert evaluated['result']=='BLOCKED',evaluated
 rows.append(dict(label='real-runtime-helper-host',hostExit=150,exception='HelperUnavailable',message=message,result=evaluated['result'],blockers=evaluated['blockers']))
assert all(sha((root/p).read_bytes())==h for p,h in source.items())
save('report.json',dict(scope='Parent independent final native and real caller controls; disposable fixture only and no runtime authority',rows=rows,count=len(rows),sourceHashes=source,sourceUnchanged=True,fixture=str(fixture),workspace=str(work),candidate=candidate))
shutil.copyfile('/tmp/epic16-review13-parent-final-controls.py',out/'parent-probe.py');print(json.dumps(dict(count=len(rows),sourceUnchanged=True,results=[dict(label=r['label'],result=r['result']) for r in rows]),indent=2))
