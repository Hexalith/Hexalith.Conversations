from pathlib import Path
import tempfile,shutil,subprocess,json,hashlib,base64
root=Path.cwd();backup=Path('/tmp/epic16-review12-preservation-mdzqxec0');work=Path(tempfile.mkdtemp(prefix='epic16-r12-product-stamp-'));out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-product-stamp-proof';out.mkdir(parents=True,exist_ok=True)
(work/'global.json').write_bytes((root/'global.json').read_bytes())
helper=work/'helper';helper.mkdir()
for p in (backup/'_bmad/scripts/epic16_native_helper').iterdir():
 if p.is_file():shutil.copyfile(p,helper/p.name)
external=work/'external';external.mkdir();product=work/'product';product.mkdir()
(external/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup></Project>')
(external/'Stamp.cs').write_text('namespace System.Reflection; public class AssemblyInformationalVersionAttribute(string value) : System.Attribute {}')
(product/'Product.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" Aliases="custom" /></ItemGroup></Project>')
(product/'Api.cs').write_text('extern alias custom; [assembly:custom::System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] namespace Example; public class Api { public string Read()=>"value"; }')
commands=[]
def run(name,argv,cwd,stdin=None):
 r=subprocess.run(argv,cwd=cwd,input=stdin,capture_output=True,timeout=120)
 (out/(name+'.stdout')).write_bytes(r.stdout);(out/(name+'.stderr')).write_bytes(r.stderr)
 commands.append(dict(name=name,argv=argv,cwd=str(cwd),exit=r.returncode));return r
for name,path in [('helper',helper/'Epic16.NativeHelper.csproj'),('product',product/'Product.csproj')]:
 r=run(name,['dotnet','build',str(path),'-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],work);assert r.returncode==0,r.stdout+r.stderr
binary=product/'bin/Debug/net10.0/Product.dll';request=dict(operation='productMetadata',assembly=base64.b64encode(binary.read_bytes()).decode(),revision='a'*40)
r=run('inspect',['dotnet',str(helper/'bin/Debug/net10.0/Epic16.NativeHelper.dll')],work,json.dumps(request).encode());assert r.returncode==0,r.stderr
result=json.loads(r.stdout);assert result['sourceRevision']=='a'*40
for p in work.rglob('*'):
 if not p.is_file() or 'obj' in p.parts:continue
 if 'bin' in p.parts and p.name not in ('Product.dll','External.dll','Epic16.NativeHelper.dll','Epic16.NativeHelper.runtimeconfig.json','Epic16.NativeHelper.deps.json'):continue
 dest=out/'retained'/p.relative_to(work)
 if dest.suffix=='.dll':dest=dest.with_suffix('.dll.bytes')
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
(out/'result.json').write_text(json.dumps(dict(id='R12P1',work=str(work),admitted=True,sourceRevision=result['sourceRevision'],informationalVersion=result['informationalVersion'],commands=commands,sourceEpoch='Exact iteration-11 verified helper restored from the immutable iteration-12 preservation copy.',scope='Synthetic external stamp; no runtime qualification'),indent=2)+'\n')
shutil.copyfile('/tmp/epic16-review12-product-stamp.py',out/'probe.py')
(out/'manifest.json').write_text(json.dumps({'artifacts':[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']},indent=2)+'\n')
print(json.dumps(dict(admitted=True,sourceRevision=result['sourceRevision'],work=str(work),commands=commands),indent=2))
