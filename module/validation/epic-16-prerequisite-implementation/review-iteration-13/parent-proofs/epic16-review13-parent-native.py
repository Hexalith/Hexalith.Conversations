from pathlib import Path
import sys,json,hashlib,tempfile,shutil,subprocess
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';proof=base/'reviewer-proofs';out=base/'parent-native';out.mkdir(exist_ok=True)
results=[]
for name in ('constructor-default','return-kind','collection-blob'):
 p=json.loads((proof/name/'provenance.json').read_bytes());row=dict(probe=name,inspections={})
 for kind in ('valid','mutated'):
  content=(proof/name/(kind+'.dll.bytes')).read_bytes();value=native.inspect_assembly(content,p['revision'],p['type'],p['method']);row['inspections'][kind]=dict(sha256=hashlib.sha256(content).hexdigest(),metadata=value)
 results.append(row)
for name in ('private-struct-layout','interface-method-mapping'):
 comparison=json.loads((proof/name/'comparison.json').read_bytes());results.append(dict(probe=name,reportedComparison=comparison))
work=Path(tempfile.mkdtemp(prefix='epic16-r13-collection-'));shutil.copyfile(root/'global.json',work/'global.json')
for name in ('Tests.cs','Stamp.cs'):shutil.copyfile(proof/'collection-blob'/name,work/name)
(work/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
commands=[]
def run(label,args):
 p=subprocess.run(args,cwd=work,capture_output=True,timeout=120);(out/(label+'.stdout')).write_bytes(p.stdout);(out/(label+'.stderr')).write_bytes(p.stderr);commands.append(dict(label=label,argv=args,cwd=str(work),exitCode=p.returncode));return p
build=run('collection-build',['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']);assert build.returncode==0,build.stdout
binary=work/'bin/Debug/net10.0/Evidence.dll';original=binary.read_bytes();(out/'collection-valid.dll.bytes').write_bytes(original)
argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01']
positive=run('collection-valid',argv);assert positive.returncode==0,(positive.stdout,positive.stderr)
old=bytes.fromhex('0100085a5a756e697175650000');new=bytes.fromhex('0100085a5a756e697175650100');assert original.count(old)==1
mutated=original.replace(old,new);binary.write_bytes(mutated);(out/'collection-mutated.dll.bytes').write_bytes(mutated)
admitted=native.inspect_assembly(mutated,'a'*40,'Synthetic.TestClass','Scenario01')
negative=run('collection-mutated',argv)
for p in ('Tests.cs','Stamp.cs','Evidence.csproj','global.json'):shutil.copyfile(work/p,out/p)
report=dict(metadataControls=results,collectionRunner=dict(workspace=str(work),positiveExit=positive.returncode,mutatedExit=negative.returncode,mutatedInspectorAccepted=True,metadata=admitted,mutation=dict(oldHex=old.hex(),newHex=new.hex())),commands=commands)
(out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(metadataControls=len(results),collectionPositiveExit=positive.returncode,collectionMutatedExit=negative.returncode,report=str(out/'manifest.json'))))
