from pathlib import Path
import sys,json,subprocess,hashlib,shutil,tempfile
root=Path.cwd();sys.path[:0]=[str(root/'_bmad/scripts'),str(root/'_bmad/scripts/tests')]
import epic16_tooling as t
import test_epic16_tooling as f
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10';report=out/'parent-counterexamples.json';data=json.loads(report.read_text());work=Path(data['workspace'])
def record(name,**kw):data['rows'].append(dict(finding=name,**kw));report.write_text(json.dumps(data,indent=2)+'\n')
def cmd(argv,cwd,label):
 r=subprocess.run(argv,cwd=cwd,capture_output=True,timeout=120)
 (cwd/(label+'.stdout')).write_bytes(r.stdout);(cwd/(label+'.stderr')).write_bytes(r.stderr);(cwd/(label+'.command.json')).write_text(json.dumps(dict(argv=argv,exitCode=r.returncode),indent=2)+'\n');return r
directory=work/'constructor';project=directory/'Evidence.csproj';s=project.read_text().replace('<TargetFramework>','<OutputType>Exe</OutputType><TargetFramework>').replace('xunit.v3.extensibility.core','xunit.v3');project.write_text(s)
assert cmd(['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],directory,'runner-build').returncode==0
r=cmd(['dotnet',str(directory/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips','-method','Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests.Scenario01','-trx',str(directory/'actual.trx')],directory,'runner')
meta=t.native_helper.inspect_assembly((directory/'bin/Debug/net10.0/Evidence.dll').read_bytes(),'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')
record('R10B6',actualPinnedRunnerExit=r.returncode,actualCounts=t.records.parse_trx((directory/'actual.trx').read_bytes())['recomputed'],metadataStillAccepted=True,stderr=r.stderr.decode(),stdout=r.stdout.decode())
keys=work/'keys';keys.mkdir();(keys/'global.json').write_bytes((root/'global.json').read_bytes())
(keys/'Keys.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
(keys/'Program.cs').write_text('using System.IO; using System.Security.Cryptography; for (int i=0;i<2;i++) { using var rsa=new RSACryptoServiceProvider(1024); File.WriteAllBytes(i+".snk",rsa.ExportCspBlob(true)); }')
assert cmd(['dotnet','run','--project','Keys.csproj','--no-launch-profile'],keys,'generate').returncode==0
metas=[];identities=[]
for i in range(2):
 d=work/('signed-'+str(i));external=d/'external';api=d/'api';external.mkdir(parents=True);api.mkdir();(d/'global.json').write_bytes((root/'global.json').read_bytes())
 (external/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>External</AssemblyName><PublicSign>true</PublicSign><SignAssembly>true</SignAssembly><AssemblyOriginatorKeyFile>'+str(keys/(str(i)+'.snk'))+'</AssemblyOriginatorKeyFile></PropertyGroup></Project>')
 (external/'Value.cs').write_text('namespace External; public class Value {}')
 (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Probe.Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" /></ItemGroup></Project>')
 (api/'Surface.cs').write_text('namespace Probe; public class Surface { public External.Value Echo(External.Value value) => value; }')
 revision=('a' if i==0 else 'b')*40;(api/'Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
 assert cmd(['dotnet','build','Api.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false','-p:SourceRevisionId='+revision],api,'build').returncode==0
 meta=t.native_helper.inspect_product((api/'bin/Debug/net10.0/Probe.Api.dll').read_bytes(),revision);metas.append(meta)
 identity=t.native_helper.inspect_product((api/'bin/Debug/net10.0/External.dll').read_bytes(),revision);identities.append(identity['publicKey'])
 (d/'metadata.json').write_text(json.dumps(meta,indent=2)+'\n')
assert identities[0]!=identities[1] and metas[0]['api']==metas[1]['api'];t.products.additive(*metas,t.need,'changed signed external type identity')
record('R10B3',accepted=True,equalApi=True,externalPublicKeysDifferent=True,externalPublicKeys=identities)
print(json.dumps(dict(workspace=str(work),constructorRunnerExit=r.returncode,externalIdentityChangeAccepted=True),indent=2))
