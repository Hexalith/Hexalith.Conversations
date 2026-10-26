from pathlib import Path
import json,hashlib,shutil,subprocess,sys
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_native as native
base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14'
out=base/'implementation/parent-input-recheck';out.mkdir(exist_ok=False)
workspace,helper=native._helper();records=[]
for source in sorted((base/'parent-native').glob('**/stdin.json')):
 label=str(source.parent.relative_to(base/'parent-native')).replace('/','-');dest=out/label;dest.mkdir()
 request=source.read_bytes();(dest/'stdin.json').write_bytes(request)
 argv=['dotnet',str(helper)];run=subprocess.run(argv,input=request,cwd=workspace.name,capture_output=True,timeout=30)
 (dest/'stdout').write_bytes(run.stdout);(dest/'stderr').write_bytes(run.stderr)
 positive=label=='cycle-original'
 assert run.returncode==(0 if positive else 1),(label,run.returncode,run.stdout,run.stderr)
 if positive:assert json.loads(run.stdout)['method']=='Scenario01'
 else:assert run.stdout==b'' and run.stderr==b'Invalid native evidence.'
 record=dict(input=str(source.relative_to(root)),inputSha256=hashlib.sha256(request).hexdigest(),argv=argv,cwd=workspace.name,exitCode=run.returncode,expected='PASS' if positive else 'invalid evidence')
 (dest/'command.json').write_text(json.dumps(record,indent=2)+'\n');records.append(record)
retained=out/'fixed-helper';shutil.copytree(workspace.name,retained)
(out/'report.json').write_text(json.dumps(dict(checks=records,helperSha256=hashlib.sha256(helper.read_bytes()).hexdigest()),indent=2)+'\n')
print('All 5 exact retained parent native inputs match expected admission/rejection.')
