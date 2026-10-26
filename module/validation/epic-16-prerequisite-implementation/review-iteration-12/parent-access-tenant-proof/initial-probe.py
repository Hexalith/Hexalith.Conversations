from pathlib import Path
import sys,json,hashlib,subprocess,shutil
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_measurements as m
import jsonschema
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-access-tenant-proof';out.mkdir(parents=True,exist_ok=True)
prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-access-parity-proof/retained'
import tempfile
work=Path(tempfile.mkdtemp(prefix='epic16-r12-access-tenant-'))
for p in prior.glob('*'):
 if p.is_file():shutil.copyfile(p,work/p.name)
p=work/'Program.cs';s=p.read_text();s=s.replace('(Id:"unknown-role", Principal:"actor-a", Role:TenantRole.Unknown),(Id:"control-principal", Principal:"actor-\\u001c", Role:TenantRole.TenantOwner)', '(Id:"invalid-tenant", Principal:"actor-a", Role:TenantRole.TenantOwner)');s=s.replace('new[]{"tenant-a"}','new[]{"tenant/a"}');p.write_text(s)
commands=[]
for name,argv in [('build',['dotnet','build','Probe.csproj','--nologo','-m:1','-p:NuGetAudit=false']),('run',['dotnet',str(work/'bin/Debug/net10.0/Probe.dll')])]:
 r=subprocess.run(argv,cwd=work,capture_output=True,timeout=120);(out/(name+'.stdout')).write_bytes(r.stdout);(out/(name+'.stderr')).write_bytes(r.stderr);commands.append(dict(argv=argv,cwd=str(work),exit=r.returncode));assert r.returncode==0,r.stdout+r.stderr
actual=json.loads(r.stdout);assert actual['Denial']=='MalformedTenant' and actual['Reads']==0
before=json.loads((root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes())['AC-16.1-01']
data=json.loads(json.dumps(before).replace('tenant-a','tenant/a').replace(m.relationships.segment('tenant-a'),m.relationships.segment('tenant/a')))
replacements={}
def fingerprints(old,new):
 if isinstance(old,dict):
  if 'persistedPayload' in old:
   replacements[old['payloadSha256']]=hashlib.sha256(new['persistedPayload'].encode()).hexdigest()
  for k in old:fingerprints(old[k],new[k])
 elif isinstance(old,list):
  for a,b in zip(old,new):fingerprints(a,b)
fingerprints(before,data)
def refresh(v):
 if isinstance(v,str):return replacements.get(v,v)
 if isinstance(v,list):return [refresh(x) for x in v]
 if isinstance(v,dict):return {k:refresh(x) for k,x in v.items()}
 return v
data=refresh(data)
schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());jsonschema.Draft202012Validator(schema['$defs']['AC-16.1-01']).validate(data);checks=m.evaluate('AC-16.1-01',data)
(out/'accepted-capture.json').write_text(json.dumps(data,indent=2)+'\n')
for p in work.rglob('*'):
 if not p.is_file() or 'obj' in p.parts:continue
 if 'bin' in p.parts and p.name not in ('Probe.dll','Probe.runtimeconfig.json'):continue
 dest=out/'retained'/p.relative_to(work)
 if dest.suffix=='.dll':dest=dest.with_suffix('.dll.bytes')
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
result=dict(id='R12P2',schemaAccepted=True,checks=checks,nativeActual=actual,capturedAllowed=data['authorization'][0]['allowed'],commands=commands,sourceHashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in ['_bmad/scripts/epic16_schedules.py','_bmad/scripts/epic16_measurements.py','_bmad/schemas/epic-16-tooling-v1.schema.json','_bmad/scripts/tests/fixtures/epic16-observations-v1.json']})
(out/'result.json').write_text(json.dumps(result,indent=2)+'\n');shutil.copyfile('/tmp/epic16-review12-access-tenant.py',out/'probe.py')
(out/'manifest.json').write_text(json.dumps({'artifacts':[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']},indent=2)+'\n');print(json.dumps(result,indent=2))
