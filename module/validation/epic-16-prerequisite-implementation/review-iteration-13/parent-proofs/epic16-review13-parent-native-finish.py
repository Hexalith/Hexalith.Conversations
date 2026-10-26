from pathlib import Path
import sys,json,hashlib,subprocess,shutil
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';proof=base/'reviewer-proofs';out=base/'parent-native';rows=[]
for name in ('constructor-default','return-kind','collection-blob'):
 p=json.loads((proof/name/'provenance.json').read_bytes());row=dict(probe=name,inspections={})
 for kind in ('valid','mutated'):
  content=(proof/name/(kind+'.dll.bytes')).read_bytes();row['inspections'][kind]=dict(sha256=hashlib.sha256(content).hexdigest(),metadata=native.inspect_assembly(content,p['revision'],p['type'],p['method']))
 rows.append(row)
work=Path('/tmp/epic16-r13-collection-njzpqbe8');binary=work/'bin/Debug/net10.0/Evidence.dll';assert binary.read_bytes()==(out/'collection-mutated.dll.bytes').read_bytes()
admitted=native.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')
argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01']
try:
 p=subprocess.run(argv,cwd=work,capture_output=True,timeout=8);code=p.returncode;stdout=p.stdout;stderr=p.stderr;timed_out=False
except subprocess.TimeoutExpired as e:
 code=None;stdout=e.stdout or b'';stderr=e.stderr or b'';timed_out=True
(out/'collection-mutated-repeat.stdout').write_bytes(stdout);(out/'collection-mutated-repeat.stderr').write_bytes(stderr)
for p in ('Tests.cs','Stamp.cs','Evidence.csproj','global.json'):shutil.copyfile(work/p,out/p)
report=dict(metadataControls=rows,collectionRunner=dict(workspace=str(work),argv=argv,positiveExit=0,initialNegativeTimeoutSeconds=120,repeatExit=code,repeatTimeoutSeconds=8,repeatTimedOut=timed_out,mutatedInspectorAccepted=True,metadata=admitted,mutation=dict(oldHex='0100085a5a756e697175650000',newHex='0100085a5a756e697175650100')),scope='Actual pinned xUnit runner; same exact mutated bytes reused for bounded diagnostic retention after the original120-second timeout')
(out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(metadataControls=len(rows),positiveExit=0,initialTimeoutSeconds=120,repeatTimedOut=timed_out,repeatExit=code,stdoutBytes=len(stdout),stderrBytes=len(stderr))))
