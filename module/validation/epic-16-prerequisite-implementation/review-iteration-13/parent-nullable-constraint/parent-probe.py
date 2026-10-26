from pathlib import Path
import sys,subprocess,tempfile,shutil,json,hashlib,re
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-default-controls'
out=prior.parent/'parent-nullable-constraint';out.mkdir(exist_ok=True)
manifest=json.loads((prior/'manifest.json').read_text());original=(prior/'Evidence.dll.bytes').read_bytes()
matches=list(re.finditer(rb'\x09\x20\x01\x01\x15\x11[\x80-\xbf].\x01\x08',original));assert len(matches)==1,len(matches)
offset=matches[0].end()-1
work=Path(tempfile.mkdtemp(prefix='epic16-r13-nullable-proof-'))
rows=[]
for label,value in [('valid',0x08),('string',0x0e),('object',0x1c)]:
 binary=original[:offset]+bytes([value])+original[offset+1:]
 (out/(label+'.dll.bytes')).write_bytes(binary)
 row=dict(label=label,sha256=hashlib.sha256(binary).hexdigest(),mutationOffset=offset,oldByte=8,newByte=value)
 try:row['inspection']=dict(result='PASS',metadata=native.inspect_assembly(binary,'a'*40,'Synthetic.NullableNull','Scenario01'))
 except Exception as e:row['inspection']=dict(result=type(e).__name__,message=str(e))
 folder=work/label;shutil.copytree(Path(manifest['workspace'])/'bin/Debug/net10.0',folder);(folder/'Evidence.dll').write_bytes(binary)
 argv=['dotnet',str(folder/'Evidence.dll'),'-automated','sync','-failSkips','-method','Synthetic.NullableNull.Scenario01']
 try:
  p=subprocess.run(argv,cwd=folder,capture_output=True,timeout=20);stdout=p.stdout;stderr=p.stderr;result=dict(exitCode=p.returncode)
 except subprocess.TimeoutExpired as e:stdout=e.stdout or b'';stderr=e.stderr or b'';result=dict(timeoutSeconds=20)
 (out/(label+'.stdout')).write_bytes(stdout);(out/(label+'.stderr')).write_bytes(stderr)
 row['runner']=dict(argv=argv,cwd=str(folder),**result);rows.append(row)
 (out/'manifest.json').write_text(json.dumps(dict(scope='Independent exact same-size compiled Nullable generic argument mutation; pinned xUnit actual runner; no runtime authority',workspace=str(work),sourceManifest=str(prior/'manifest.json'),rows=rows),indent=2)+'\n')
 print(json.dumps(row),flush=True)
for name in ('Tests.cs','Stamp.cs','Evidence.csproj','global.json'):shutil.copyfile(prior/name,out/name)
shutil.copyfile('/tmp/epic16-review13-nullable-proof.py',out/'parent-probe.py')
