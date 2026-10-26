from pathlib import Path
import sys,tempfile,subprocess,json,shutil,importlib.util,hashlib
sys.dont_write_bytecode=True;root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-framework-identity-proof';base.mkdir(parents=True,exist_ok=True)
sp=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(sp);sp.loader.exec_module(t)
work=Path(tempfile.mkdtemp(prefix='epic16-r11-framework-'));(work/'global.json').write_bytes((root/'global.json').read_bytes())
lib=work/'attributes';lib.mkdir();core=Path('/tmp/epic16-edge-fact-jdhey1nj/bin/Debug/net10.0/xunit.v3.core.dll');assert core.exists()
(lib/'Attributes.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="xunit.v3.core"><HintPath>'+str(core)+'</HintPath></Reference></ItemGroup></Project>')
(lib/'Attributes.cs').write_text('namespace System.Custom { public class HiddenFactAttribute : Xunit.FactAttribute {} } namespace System.Reflection { public class AssemblyFileVersionAttribute : System.Attribute { public string Contract { get; } public AssemblyFileVersionAttribute(string value) { Contract = value; } } }')
commands=[]
def run(argv,cwd,label):
 r=subprocess.run(argv,cwd=cwd,capture_output=True,timeout=120);(base/(label+'.stdout')).write_bytes(r.stdout);(base/(label+'.stderr')).write_bytes(r.stderr);commands.append(dict(argv=argv,cwd=str(cwd),exitCode=r.returncode));return r
r=run(['dotnet','build','Attributes.csproj','--nologo','-p:NuGetAudit=false'],lib,'attributes-build');assert r.returncode==0,r.stdout
test=work/'test';test.mkdir()
(test/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /><Reference Include="Attributes"><HintPath>../attributes/bin/Debug/net10.0/Attributes.dll</HintPath></Reference></ItemGroup></Project>')
(test/'Tests.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] namespace Probe; public class TestClass { [Xunit.Fact, System.Custom.HiddenFact] public void Scenario01() {} }')
r=run(['dotnet','build','Probe.csproj','--nologo','-p:NuGetAudit=false','-p:RunAnalyzers=false'],test,'test-build');assert r.returncode==0,r.stdout
binary=test/'bin/Debug/net10.0/Probe.dll';admitted=t.tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Probe.TestClass','Scenario01')
r=run(['dotnet',str(binary),'-automated','sync','-failSkips','-method','Probe.TestClass.Scenario01','-trx',str(test/'result.trx')],test,'test-run');assert r.returncode==1 and b'multiple [Fact]-derived attributes' in r.stdout+r.stderr
metadata=[]
for phase,value in [('before','one'),('after','two')]:
 api=work/phase;api.mkdir()
 (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><Reference Include="Attributes"><HintPath>../attributes/bin/Debug/net10.0/Attributes.dll</HintPath><Aliases>custom</Aliases></Reference></ItemGroup></Project>')
 (api/'Api.cs').write_text('extern alias custom; [assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] [assembly:custom::System.Reflection.AssemblyFileVersion("'+value+'")] namespace Probe; public class Api {}')
 r=run(['dotnet','build','Api.csproj','--nologo','-p:NuGetAudit=false'],api,phase+'-build');assert r.returncode==0,r.stdout
 binary=api/'bin/Debug/net10.0/Api.dll';m=t.tooling.native_helper.inspect_product(binary.read_bytes(),'a'*40);metadata.append(m);(base/(phase+'.metadata.json')).write_text(json.dumps(m,indent=2)+'\n')
assert metadata[0]['api']==metadata[1]['api'];t.tooling.products.additive(*metadata,t.tooling.need,'custom assembly attribute contract')
for path in work.rglob('*'):
 if path.is_file() and ('obj' not in path.parts) and (path.suffix in ('.cs','.csproj','.trx') or path.name in ('global.json','Attributes.dll','Api.dll','Probe.dll')):
  target=base/path.relative_to(work);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
result=dict(workspace=str(work),discoveryAdmitted=admitted,pinnedRunnerExit=1,nonFrameworkAttributeValuesCollapsed=True,apiAdditiveAccepted=True,commands=commands)
(base/'result.json').write_text(json.dumps(result,indent=2)+'\n')
(base/'manifest.json').write_text(json.dumps(dict(artifacts=[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(base.rglob('*')) if p.is_file()]),indent=2)+'\n')
print(json.dumps(result,indent=2))
