import base64, hashlib, json, os, resource, shutil, subprocess, sys
from pathlib import Path
ROOT=Path.cwd();sys.path.insert(0,str(ROOT/'_bmad/scripts'))
import epic16_native as native
OUT=ROOT/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/parent-native';OUT.mkdir(parents=True,exist_ok=True)
def h(b):return hashlib.sha256(b).hexdigest()
def run(label,argv,cwd,stdin=None):
 p=OUT/label;p.mkdir(parents=True,exist_ok=True)
 r=subprocess.run(argv,cwd=cwd,input=stdin,capture_output=True,timeout=45,preexec_fn=lambda:resource.setrlimit(resource.RLIMIT_CORE,(0,0)))
 (p/'stdout').write_bytes(r.stdout);(p/'stderr').write_bytes(r.stderr)
 if stdin is not None:(p/'stdin.json').write_bytes(stdin)
 value=dict(argv=argv,cwd=str(cwd),exitCode=r.returncode,stdoutSha256=h(r.stdout),stderrSha256=h(r.stderr))
 (p/'command.json').write_text(json.dumps(value,indent=2)+'\n');return r,value
workspace,helper=native._helper();shutil.copytree(workspace.name,OUT/'fixed-helper',dirs_exist_ok=True)
cases=[('vararg',Path('/tmp/epic16-blind-vararg-6rskk2b5')),('optional',Path('/tmp/review14-refstruct-h7_v5woq/optional')),('null',Path('/tmp/review14-refstruct-h7_v5woq/null'))]
results=[]
for name,p in cases:
 saved=OUT/name/'inputs';saved.mkdir(parents=True,exist_ok=True)
 for f in p.iterdir():
  if f.is_file() and f.suffix not in ['.json'] or f.is_file() and f.name in ['commands.json','summary.json','global.json']:
   shutil.copy2(f,saved/f.name)
 binary=(p/'bin/Debug/net10.0/Evidence.dll').read_bytes();(saved/'Evidence.dll').write_bytes(binary)
 request=dict(operation='metadata',assembly=base64.b64encode(binary).decode(),revision='a'*40,type='Synthetic.TestClass',method='Scenario01')
 inspect,ic=run(name+'/inspection',['dotnet',str(helper)],workspace.name,json.dumps(request).encode());assert inspect.returncode==0,inspect.stderr
 actual,ac=run(name+'/runner',['dotnet',str(p/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(OUT/name/'actual.trx')],p)
 assert actual.returncode==1 and b'NotSupportedException' in actual.stdout+actual.stderr
 results.append(dict(case=name,binarySha256=h(binary),inspection=ic,runner=ac,accepted=True))
p=Path('/tmp/epic16-blind-cycle-g134dbww');saved=OUT/'cycle'/'inputs';saved.mkdir(parents=True,exist_ok=True)
for name in ['Tests.cs','Evidence.csproj','global.json','original.dll','mutant.dll','mutation.json','probe.py','build.stdout','build.stderr','commands.json']:shutil.copy2(p/name,saved/name)
a=(p/'original.dll').read_bytes();b=(p/'mutant.dll').read_bytes();diff=[i for i,(x,y) in enumerate(zip(a,b)) if x!=y];assert len(a)==len(b) and len(diff)==1
for label,data in [('original',a),('mutant',b)]:
 request=dict(operation='metadata',assembly=base64.b64encode(data).decode(),revision='a'*40,type='Synthetic.TestClass',method='Scenario01')
 actual,command=run('cycle/'+label,['dotnet',str(helper)],workspace.name,json.dumps(request).encode())
 assert actual.returncode==0 if label=='original' else actual.returncode!=0 and b'Stack overflow' in actual.stderr
 results.append(dict(case='cycle-'+label,binarySha256=h(data),command=command,byteChanges=diff))
try:native.inspect_assembly(b,'a'*40,'Synthetic.TestClass','Scenario01')
except native.HelperUnavailable as e: results.append(dict(case='cycle-python-classification',exception=type(e).__name__,detail=str(e)))
else:raise AssertionError('expected HelperUnavailable')
(OUT/'report.json').write_text(json.dumps(dict(results=results,helperSources={f:h((ROOT/f).read_bytes()) for f in native.HELPER_FILES}),indent=2)+'\n')
shutil.copy2(__file__,OUT/'probe.py')
files=[dict(path=str(f.relative_to(OUT)),sha256=h(f.read_bytes()),bytes=f.stat().st_size) for f in sorted(OUT.rglob('*')) if f.is_file() and f.name!='manifest.json']
(OUT/'manifest.json').write_text(json.dumps(files,indent=2)+'\n');print(json.dumps(dict(cases=len(results),artifacts=len(files),report=str(OUT/'report.json'))))
