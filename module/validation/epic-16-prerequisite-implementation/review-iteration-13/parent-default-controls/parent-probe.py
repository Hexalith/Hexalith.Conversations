from pathlib import Path
import json,tempfile,shutil,subprocess,hashlib
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-default-controls';out.mkdir(exist_ok=True)
work=Path(tempfile.mkdtemp(prefix='epic16-r13-default-controls-'));shutil.copyfile(root/'global.json',work/'global.json')
cases=[
 ('IntDefault','int value = 8675309','value == 8675309'),
 ('LongDefault','long value = 1','value == 1L'),
 ('StringDefault','string value = ""','value == ""'),
 ('NullReference','object value = null','value is null'),
 ('NullableValue','int? value = 7','value == 7'),
 ('NullableNull','int? value = null','value is null'),
 ('EnumDefault','Choice value = Choice.One','value == Choice.One'),
 ('NullableEnum','Choice? value = Choice.One','value == Choice.One'),
 ('DecimalDefault','decimal value = 1.5m','value == 1.5m'),
 ('DateTimeDefault','System.DateTime value = default','value == default(System.DateTime)'),
 ('OptionalString','[System.Runtime.InteropServices.Optional] string value','value is null'),
 ('OptionalInt','[System.Runtime.InteropServices.Optional] int value','value == 0'),
 ('ParamsArray','params string[] value','value.Length == 0'),
 ('AttributeDefault','[System.Runtime.InteropServices.DefaultParameterValue(3)] int value','value == 3'),
]
source='namespace Synthetic; public enum Choice { Zero, One }\n'+''.join('public sealed class '+name+'('+parameter+') { [Xunit.Fact] public void Scenario01() { if (!('+condition+')) throw new System.Exception("Unexpected actual xUnit default"); } }\n' for name,parameter,condition in cases)
(work/'Tests.cs').write_text(source);(work/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]')
(work/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
commands=[]
for label,args in [('build',['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']),('runner',['dotnet',str(work/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips'])]:
 p=subprocess.run(args,cwd=work,capture_output=True,timeout=120);(out/(label+'.stdout')).write_bytes(p.stdout);(out/(label+'.stderr')).write_bytes(p.stderr);commands.append(dict(label=label,argv=args,cwd=str(work),exitCode=p.returncode));(out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
 if p.returncode:raise RuntimeError((label,p.returncode,p.stdout.decode(errors='replace')[-2000:]))
for name in ('Tests.cs','Stamp.cs','Evidence.csproj','global.json'):shutil.copyfile(work/name,out/name)
binary=(work/'bin/Debug/net10.0/Evidence.dll').read_bytes();(out/'Evidence.dll.bytes').write_bytes(binary)
events=[json.loads(line) for line in p.stdout.decode().splitlines() if line.startswith('{')]
summary=next(e for e in events if e.get('$type')=='test-assembly-finished');assert summary['TestsTotal']==len(cases) and summary['TestsFailed']==summary['TestsSkipped']==summary['TestsNotRun']==0,summary
report=dict(scope='Independent genuine pinned xUnit positive constructor-default controls; synthetic and no runtime authority',revision='a'*40,method='Scenario01',classes=['Synthetic.'+c[0] for c in cases],workspace=str(work),commands=commands,summary=summary,binarySha256=hashlib.sha256(binary).hexdigest())
(out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(passed=len(cases),manifest=str(out/'manifest.json'))))
