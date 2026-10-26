from pathlib import Path
import sys,subprocess,tempfile,shutil,json,hashlib
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-decimal-sign';out.mkdir(exist_ok=True)
work=Path(tempfile.mkdtemp(prefix='epic16-r13-decimal-sign-'));shutil.copyfile(root/'global.json',work/'global.json')
signs=[0,1,127,128,255]
source='namespace Synthetic;\n'+''.join('public sealed class Sign'+str(sign)+'([System.Runtime.CompilerServices.DecimalConstant(0,'+str(sign)+',0u,0u,1u)] decimal value) { [Xunit.Fact] public void Scenario01() { if (value != '+('1m' if sign==0 else '-1m')+') throw new System.Exception("Unexpected trusted decimal default"); } }\n' for sign in signs)
(work/'Tests.cs').write_text(source);(work/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]')
(work/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
commands=[]
for label,argv in [('build',['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']),('runner',['dotnet',str(work/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips'])]:
 p=subprocess.run(argv,cwd=work,capture_output=True,timeout=120);(out/(label+'.stdout')).write_bytes(p.stdout);(out/(label+'.stderr')).write_bytes(p.stderr);commands.append(dict(argv=argv,cwd=str(work),exitCode=p.returncode));(out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n');assert p.returncode==0,(label,p.stdout,p.stderr)
for name in ('Tests.cs','Stamp.cs','Evidence.csproj','global.json'):shutil.copyfile(work/name,out/name)
binary=(work/'bin/Debug/net10.0/Evidence.dll').read_bytes();(out/'Evidence.dll.bytes').write_bytes(binary)
events=[json.loads(line) for line in p.stdout.decode().splitlines() if line.startswith('{')];summary=next(e for e in events if e.get('$type')=='test-assembly-finished');assert summary['TestsTotal']==5 and summary['TestsFailed']==0,summary
rows=[]
for sign in signs:
 try:metadata=native.inspect_assembly(binary,'a'*40,'Synthetic.Sign'+str(sign),'Scenario01');rows.append(dict(sign=sign,inspector='PASS',metadata=metadata))
 except Exception as e:rows.append(dict(sign=sign,inspector=type(e).__name__,message=str(e)))
record=dict(scope='Source-authored trusted DecimalConstantAttribute values; no IL mutation and no runtime authority',workspace=str(work),commands=commands,summary=summary,inspections=rows,binarySha256=hashlib.sha256(binary).hexdigest())
(out/'manifest.json').write_text(json.dumps(record,indent=2)+'\n');shutil.copyfile('/tmp/epic16-review13-decimal-sign.py',out/'parent-probe.py');print(json.dumps(dict(actualPinnedRunnerPassed=5,inspections=[{k:v for k,v in r.items() if k!='metadata'} for r in rows])))
