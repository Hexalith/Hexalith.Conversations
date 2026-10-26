from pathlib import Path
import sys,json,hashlib,subprocess,tempfile,shutil
from copy import deepcopy
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_measurements as m
import jsonschema
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-access-parity-proof';out.mkdir(parents=True,exist_ok=True)
work=Path(tempfile.mkdtemp(prefix='epic16-r12-access-parity-'))
(work/'global.json').write_bytes((root/'global.json').read_bytes())
(work/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup></Project>')
sourcebase=root/'references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess'
files=['TenantAccessEvaluator.cs','TenantAccessEvaluation.cs','TenantAccessDenialKind.cs','TenantAccessState.cs','TenantAccessProjectionHealth.cs','ITenantAccessStateStore.cs','ITenantAccessProjectionHealthProvider.cs']
for n in files:shutil.copyfile(sourcebase/n,work/n)
shutil.copyfile(root/'references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs',work/'TenantRole.cs')
shutil.copyfile(root/'references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs',work/'TenantStatus.cs')
shutil.copyfile(root/'references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Serialization/TenantStatusJsonConverter.cs',work/'TenantStatusJsonConverter.cs')
(work/'Program.cs').write_text('''using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
foreach (var c in new[]{(Id:"unknown-role", Principal:"actor-a", Role:TenantRole.Unknown),(Id:"control-principal", Principal:"actor-\\u001c", Role:TenantRole.TenantOwner)}) {
 var store=new Store(c.Principal,c.Role);var health=new Health();
 var result=await TenantAccessEvaluator.EvaluateAsync("Read",new[]{"tenant-a"},c.Principal,store,health,
 static requirement=>requirement=="Read", static status=>Enum.IsDefined((TenantStatus)status),static status=>(TenantStatus)status==TenantStatus.Active,static status=>(TenantStatus)status==TenantStatus.Disabled,
 static role=>Enum.IsDefined((TenantRole)role), static (role,requirement)=>(TenantRole)role is TenantRole.TenantReader or TenantRole.TenantContributor or TenantRole.TenantOwner && requirement=="Read",NullLogger.Instance);
 Console.WriteLine(JsonSerializer.Serialize(new{c.Id,result.IsAllowed,Denial=result.DenialKind.ToString(),store.Reads,health.ReadsHealth}));
}
sealed class Store(string principal,TenantRole role):ITenantAccessStateStore {public int Reads;public Task<TenantAccessState?> GetAsync(string tenantId,CancellationToken ct=default){Reads++;return Task.FromResult<TenantAccessState?>(new(tenantId,(int)TenantStatus.Active,new Dictionary<string,int>{{principal,(int)role}}));}}
sealed class Health:ITenantAccessProjectionHealthProvider {public int ReadsHealth;public ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(string tenantId,CancellationToken ct=default){ReadsHealth++;return ValueTask.FromResult<TenantAccessProjectionHealth?>(new(10,"10",false,false,false,false));}}
''')
commands=[]
for name,argv in [('build',['dotnet','build','Probe.csproj','--nologo','-m:1','-p:NuGetAudit=false']),('run',['dotnet',str(work/'bin/Debug/net10.0/Probe.dll')])]:
 r=subprocess.run(argv,cwd=work,capture_output=True,timeout=120);(out/(name+'.stdout')).write_bytes(r.stdout);(out/(name+'.stderr')).write_bytes(r.stderr);commands.append(dict(argv=argv,cwd=str(work),exit=r.returncode));assert r.returncode==0,r.stdout+r.stderr
native=[json.loads(line) for line in r.stdout.splitlines()]
assert native[0]['Denial']=='InsufficientRole' and native[1]['Denial']=='MissingCaller' and native[1]['Reads']==0
obs=json.loads((root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes());schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());results=[]
for mode in ('unknown-role','control-principal'):
 data=deepcopy(obs['AC-16.1-01']);row=data['authorization'][0];p=row['capture'];snap=p['storeRead']['snapshot'];decision=p['response']['decision'];p['request']['requirement']=decision['requirement']='Read'
 if mode=='unknown-role':
  p['sourceState']['members']['actor-a']='Unknown';row['allowed']=decision['isAllowed']=False;decision['denialReason']='InsufficientRole';decision['projectionVersion']=decision['projectionWatermark']=None
 else:
  principal='actor-\x1c';p['sourceState']['members'][principal]=p['sourceState']['members'].pop('actor-a')
  for scope in (p['scope'],p['request']['scope'],p['storeRead']['scope'],p['response']['scope']):scope['principal']=principal
  decision['callerPrincipalId']=principal
 snap['projection']=m.domain.tenant_replay(p['sourceState'],[p['sourceRecord']],9,10)[0]
 jsonschema.Draft202012Validator(schema['$defs']['AC-16.1-01']).validate(data)
 try:checks=m.evaluate('AC-16.1-01',data);rejected=None
 except ValueError as error:checks=[];rejected=str(error)
 assert (rejected is None) if mode=='unknown-role' else (rejected is not None)
 (out/(mode+'.json')).write_text(json.dumps(data,indent=2)+'\n');results.append(dict(mode=mode,schemaAccepted=True,semanticRejection=rejected,checks=checks,capturedDecision=decision,actualNativeResult=next(n for n in native if n['Id']==mode)))
for p in work.rglob('*'):
 if not p.is_file() or 'obj' in p.parts:continue
 if 'bin' in p.parts and p.name not in ('Probe.dll','Probe.runtimeconfig.json'):continue
 dest=out/'retained'/p.relative_to(work)
 if dest.suffix=='.dll':dest=dest.with_suffix('.dll.bytes')
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
(out/'results.json').write_text(json.dumps(dict(results=results,commands=commands,sourceHashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in ['_bmad/scripts/epic16_schedules.py','_bmad/scripts/epic16_measurements.py','_bmad/schemas/epic-16-tooling-v1.schema.json','_bmad/scripts/tests/fixtures/epic16-observations-v1.json']}),indent=2)+'\n');shutil.copyfile('/tmp/epic16-review12-access-parity.py',out/'probe.py')
(out/'manifest.json').write_text(json.dumps({'artifacts':[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']},indent=2)+'\n')
print(json.dumps(results,indent=2))
