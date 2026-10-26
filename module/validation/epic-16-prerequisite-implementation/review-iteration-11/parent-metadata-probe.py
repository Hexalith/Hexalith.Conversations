from pathlib import Path
import importlib.util,json,hashlib,subprocess,sys,tempfile
root=Path.cwd();sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-metadata-proof';out.mkdir(parents=True,exist_ok=True)
cases={
 'R11B1-interface-attributes':('#nullable enable\nnamespace Probe; public interface IBase<T> {} public interface Api : IBase<string> {}','#nullable enable\nnamespace Probe; public interface IBase<T> {} public interface Api : IBase<string?> {}'),
 'R11B2-attribute-constructor':tuple('using System; namespace Probe; public class StampAttribute : Attribute { public string Selected { get; } public StampAttribute(int value) { Selected="int"; } public StampAttribute(uint value) { Selected="uint"; } } [Stamp('+value+')] public class Api {}' for value in ('1','1u')),
 'R11B3-preserve-sig':tuple('namespace Probe; [System.Runtime.InteropServices.ComImport][System.Runtime.InteropServices.Guid("F0000000-0000-0000-0000-000000000001")] public interface Api { '+attribute+' int Method(); }' for attribute in ('','[System.Runtime.InteropServices.PreserveSig]')),
 'R11B4-assembly-attributes':tuple('[assembly:System.CLSCompliant('+value+')] namespace Probe; public class Api {}' for value in ('true','false')),
 'R11B5-owning-flags':('namespace Probe; public class Api {}','[assembly:System.Reflection.AssemblyFlags(System.Reflection.AssemblyNameFlags.Retargetable)] namespace Probe; public class Api {}'),
}
reports=[]
for name,sources in cases.items():
 directory=out/name;directory.mkdir(exist_ok=True);metadata=[]
 for phase,source in zip(('baseline','candidate'),sources):
  binary=t.round9_compile_product(dict(assemblyName='Probe'),'a'*40,source);meta=t.tooling.native_helper.inspect_product(binary,'a'*40);metadata.append(meta)
  (directory/(phase+'.cs')).write_text(source);(directory/(phase+'.dll.bytes')).write_bytes(binary);(directory/(phase+'.metadata.json')).write_text(json.dumps(meta,indent=2)+'\n')
 accepted=True
 try:t.tooling.products.additive(*metadata,t.tooling.need,name)
 except t.tooling.Stop:accepted=False
 report=dict(id=name,apiEqual=metadata[0]['api']==metadata[1]['api'],additiveAccepted=accepted)
 assert report['apiEqual'] and accepted,report
 if name.startswith(('R11B1','R11B4')):
  results=[]
  for phase in ('baseline','candidate'):
   work=Path(tempfile.mkdtemp(prefix='epic16-r11-consumer-'));(work/'global.json').write_bytes((root/'global.json').read_bytes());(work/'Probe.dll').write_bytes((directory/(phase+'.dll.bytes')).read_bytes())
   (work/'Consumer.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><Reference Include="Probe"><HintPath>Probe.dll</HintPath></Reference></ItemGroup></Project>')
   code='#nullable enable\npublic class Surface { public Probe.IBase<string> Echo(Probe.Api value) => value; }' if name.startswith('R11B1') else '[assembly:System.CLSCompliant(true)] public class Surface { public Probe.Api Echo(Probe.Api value) => value; }'
   (work/'Consumer.cs').write_text(code);argv=['dotnet','build','Consumer.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];run=subprocess.run(argv,cwd=work,capture_output=True,timeout=120)
   for suffix,value in (('consumer.cs',code.encode()),('consumer.csproj',(work/'Consumer.csproj').read_bytes()),('consumer.stdout',run.stdout),('consumer.stderr',run.stderr)):(directory/(phase+'.'+suffix)).write_bytes(value)
   row=dict(phase=phase,cwd=str(work),argv=argv,exitCode=run.returncode);results.append(row)
  assert [r['exitCode'] for r in results]==[0,1],results
  report['actualConsumerBuilds']=results
 reports.append(report);print(json.dumps(report),flush=True)
(out/'results.json').write_text(json.dumps(reports,indent=2)+'\n')
manifest=[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.iterdir()) if p.is_file()]
manifest=[dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file()]
(out/'manifest.json').write_text(json.dumps(dict(scope='Independent compiled counterexamples against iteration-10 source; observed acceptance is incorrect and is not qualification.',artifacts=manifest),indent=2)+'\n')
