import sys,subprocess,tempfile,json,base64
from pathlib import Path
sys.path.insert(0,str(Path.cwd()/'_bmad/scripts'))
import epic16_native as native
p=Path(tempfile.mkdtemp(prefix='epic16-blind-cycle-'));commands=[]
def run(label,argv,cwd):
 r=subprocess.run(argv,cwd=cwd,capture_output=True,timeout=120)
 (p/(label+'.stdout')).write_bytes(r.stdout);(p/(label+'.stderr')).write_bytes(r.stderr)
 commands.append(dict(argv=argv,cwd=str(cwd),exitCode=r.returncode));(p/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
 assert r.returncode==0,(r.stdout,r.stderr)
 return r
p.joinpath('global.json').write_bytes(Path('global.json').read_bytes())
p.joinpath('Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo><RunAnalyzers>false</RunAnalyzers></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
p.joinpath('Tests.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] namespace Synthetic; public class TestClass { [Xunit.Fact] public void Scenario01() {} } public class Outer { public class Inner {} }')
run('build',['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],p)
b=p/'bin/Debug/net10.0/Evidence.dll';original=b.read_bytes();(p/'original.dll').write_bytes(original)
locator=p/'locator';locator.mkdir();(locator/'global.json').write_bytes(Path('global.json').read_bytes())
(locator/'Locator.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
(locator/'Program.cs').write_text('''using System.Reflection.Metadata; using System.Reflection.Metadata.Ecma335; using System.Reflection.PortableExecutable; using System.Text.Json; using var pe=new PEReader(File.OpenRead(args[0])); var r=pe.GetMetadataReader(); Console.Write(JsonSerializer.Serialize(new { offset=pe.PEHeaders.MetadataStartOffset+r.GetTableMetadataOffset(TableIndex.NestedClass), size=r.GetTableRowSize(TableIndex.NestedClass), rows=r.GetTableRowCount(TableIndex.NestedClass), target=r.TypeDefinitions.Where(h=>r.GetString(r.GetTypeDefinition(h).Name)=="Inner").Select(h=>MetadataTokens.GetRowNumber(h)).Single() }));''')
run('locator-build',['dotnet','build','Locator.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],locator)
location=json.loads(run('locator-run',['dotnet',str(locator/'bin/Debug/net10.0/Locator.dll'),str(b)],p).stdout)
mutant=bytearray(original);width=location['size']//2
for row in range(location['rows']):
 start=location['offset']+row*location['size']
 if int.from_bytes(mutant[start:start+width],'little')==location['target']:
  mutant[start+width:start+width*2]=location['target'].to_bytes(width,'little');break
else:raise Exception('no target')
(p/'mutant.dll').write_bytes(mutant);(p/'mutation.json').write_text(json.dumps(location,indent=2)+'\n')
workspace,helper=native._helper()
for label,content in [('original',original),('mutant',bytes(mutant))]:
 request=dict(operation='metadata',assembly=base64.b64encode(content).decode(),revision='a'*40,type='Synthetic.TestClass',method='Scenario01');(p/(label+'.request.json')).write_text(json.dumps(request))
 r=subprocess.run(['dotnet',str(helper)],input=json.dumps(request).encode(),cwd=workspace.name,capture_output=True,timeout=30)
 (p/(label+'.stdout')).write_bytes(r.stdout);(p/(label+'.stderr')).write_bytes(r.stderr)
 commands.append(dict(argv=['dotnet',str(helper)],cwd=workspace.name,stdin=str(p/(label+'.request.json')),exitCode=r.returncode))
 print(label,r.returncode,r.stdout.decode()[:200],r.stderr.decode()[:150],flush=True)
(p/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
(p/'helper.dll').write_bytes(helper.read_bytes())
for f in native.HELPER_FILES:
 dst=p/'helper-source'/Path(f).name;dst.parent.mkdir(exist_ok=True);dst.write_bytes(Path(f).read_bytes())
print(p,flush=True)
