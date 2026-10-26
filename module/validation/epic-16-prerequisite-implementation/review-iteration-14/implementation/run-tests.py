from pathlib import Path
import subprocess,sys,json,hashlib,time,os,shutil
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/implementation';label=sys.argv[1];out=base/label;out.mkdir(parents=True,exist_ok=False)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
paths=set(json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/combined-final-order/source.json').read_bytes())['files'])
paths.update(x['path'] for x in json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes())['sources'])
files={p:sha(root/p) for p in sorted(paths)}
for path in paths:
 dest=out/'source'/path
 if dest.name=='.gitignore':dest=dest.with_name('.gitignore.bytes')
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/path,dest)
(out/'source.json').write_text(json.dumps(dict(files=files),indent=2)+'\n')
env=dict(os.environ,EPIC16_COMMITLINT_EVIDENCE=str(out/'commitlint.json'),EPIC16_R11_NATIVE_EVIDENCE=str(out/'native'),EPIC16_R10_NATIVE_EVIDENCE=str(out/'native-r10'),EPIC16_R12_NATIVE_EVIDENCE=str(out/'native-r12'))
command=['uv','run','--frozen','--no-sync','python3','-m','pytest','-q',*sys.argv[2:],'--junitxml='+str(out/'result.xml')]
record=dict(argv=command,cwd=str(root),environment={k:v for k,v in env.items() if k.startswith('EPIC16_')},started=time.time(),sourceSha256=sha(out/'source.json'))
(out/'command.json').write_text(json.dumps(record,indent=2)+'\n')
with (out/'stdout').open('wb') as stdout,(out/'stderr').open('wb') as stderr:
 result=subprocess.run(command,cwd=root,env=env,stdout=stdout,stderr=stderr)
record.update(exitCode=result.returncode,elapsedSeconds=time.time()-record['started'],sourceUnchanged=all(sha(root/p)==h for p,h in files.items()),stdoutSha256=sha(out/'stdout'),stderrSha256=sha(out/'stderr'))
(out/'command.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record,indent=2));print((out/'stdout').read_text()[-14000:]);sys.exit(result.returncode)
