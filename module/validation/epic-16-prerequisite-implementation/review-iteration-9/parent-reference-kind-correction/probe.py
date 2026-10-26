from pathlib import Path
import sys,tempfile,subprocess,json
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_native as n,epic16_products as p,epic16_tooling as t
work=Path(tempfile.mkdtemp(prefix='epic16-parent-reference-kind-'));results=[]
for i,kind in enumerate(('class','struct')):
 phase=work/kind;ext=phase/'external';api=phase/'api';ext.mkdir(parents=True);api.mkdir()
 (phase/'global.json').write_bytes((root/'global.json').read_bytes())
 (ext/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>External</AssemblyName></PropertyGroup></Project>')
 (ext/'Value.cs').write_text('namespace External; public '+kind+' Value {}')
 (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Probe.Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" /></ItemGroup></Project>')
 (api/'Surface.cs').write_text('namespace Probe; public class Surface { public External.Value Echo(External.Value value) => value; }')
 revision=('a' if i==0 else 'b')*40;(api/'Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
 command=['dotnet','build','Api.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];r=subprocess.run(command,cwd=api,capture_output=True,timeout=120)
 (phase/'build.stdout').write_bytes(r.stdout);(phase/'build.stderr').write_bytes(r.stderr);assert r.returncode==0,r.stdout.decode()
 binary=api/'bin/Debug/net10.0/Probe.Api.dll';metadata=n.inspect_product(binary.read_bytes(),revision);results.append(dict(kind=kind,metadata=metadata))
accepted=True;error=None
try:p.additive(results[0]['metadata'],results[1]['metadata'],t.need,'external value kind')
except t.Stop as e:accepted=False;error=e.code
report=dict(workspace=str(work),apiEqual=results[0]['metadata']['api']==results[1]['metadata']['api'],accepted=accepted,error=error,results=results)
(work/'result.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(workspace=str(work),apiEqual=report['apiEqual'],accepted=accepted,error=error,methods=results[0]['metadata']['api']['types']['Probe.Surface']['members']),indent=2))
