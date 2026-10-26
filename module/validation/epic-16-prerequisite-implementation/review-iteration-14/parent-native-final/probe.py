from pathlib import Path
import base64,hashlib,json,shutil,subprocess,sys
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14';out=base/'parent-native-final';out.mkdir(exist_ok=False);sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_native as n
workspace,helper=n._helper();shutil.copytree(workspace.name,out/'fixed-helper');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();results=[]
for label,path,valid in [('vararg','vararg/inputs/Evidence.dll',False),('optional','optional/inputs/Evidence.dll',False),('null','null/inputs/Evidence.dll',False),('nested','cycle/inputs/original.dll',True),('cyclic','cycle/inputs/mutant.dll',False)]:
 p=base/'parent-native'/path;request=dict(operation='metadata',assembly=base64.b64encode(p.read_bytes()).decode(),revision='a'*40,type='Synthetic.TestClass',method='Scenario01');data=json.dumps(request).encode();r=subprocess.run(['dotnet',str(helper)],input=data,cwd=workspace.name,capture_output=True,timeout=30)
 (out/(label+'.stdout')).write_bytes(r.stdout);(out/(label+'.stderr')).write_bytes(r.stderr);(out/(label+'.input.json')).write_bytes(data)
 assert r.returncode==(0 if valid else 1),(label,r.stdout,r.stderr)
 if not valid:assert r.stdout==b'' and r.stderr==b'Invalid native evidence.'
 try:value=n.inspect_assembly(p.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01');assert valid
 except ValueError as e:assert not valid;value=dict(exception=type(e).__name__,message=str(e))
 results.append(dict(label=label,path=str(p.relative_to(root)),sha256=sha(p),argv=['dotnet',str(helper)],cwd=workspace.name,exitCode=r.returncode,classification=value))
(out/'report.json').write_text(json.dumps(dict(results=results,helperSources={p:sha(root/p) for p in n.HELPER_FILES}),indent=2)+'\n');shutil.copy2(__file__,out/'probe.py');print('Five unchanged reviewer inputs have the corrected classifications; invalid bytes are FAIL, valid nested control passes.')
