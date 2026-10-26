from pathlib import Path
import sys,os,json,hashlib,subprocess,time
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation';label=sys.argv[1];directory=base/label;directory.mkdir(exist_ok=False)
files=sys.argv[3:] or ['_bmad/scripts/tests/test_epic16_tooling.py'];argv=['uv','run','--frozen','--no-sync','python3','-m','pytest','-q',*files]
if sys.argv[2]!='ALL':argv+=['-k',sys.argv[2]]
if os.environ.get('EPIC16_BASETEMP'):argv+=['--basetemp='+os.environ['EPIC16_BASETEMP']]
argv+=['--junitxml='+str(directory/'result.xml')]
env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',EPIC16_COMMITLINT_EVIDENCE=str(directory/'commitlint.json'),EPIC16_R12_NATIVE_EVIDENCE=str(directory/'native-r12'),EPIC16_R11_NATIVE_EVIDENCE=str(directory/'native'),EPIC16_R10_NATIVE_EVIDENCE=str(directory/'native-r10'))
paths=set(json.load(open('module/validation/epic-16-prerequisite-implementation/review-iteration-11/implementation/final-source.json'))['files']);paths.update(x['path'] for x in json.load(open('_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json'))['sources']);hashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in sorted(paths)}
(directory/'source-hashes.json').write_text(json.dumps(hashes,indent=2)+'\n')
for p in paths:
 target=directory/'source'/(p+'.bytes' if p=='.gitignore' else p);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((root/p).read_bytes())
start=time.time();print(json.dumps(dict(label=label,argv=argv)),flush=True)
with (directory/'stdout').open('wb') as out,(directory/'stderr').open('wb') as err:r=subprocess.run(argv,cwd=root,env=env,stdout=out,stderr=err)
unchanged=all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in hashes.items());record=dict(argv=argv,cwd=str(root),environment={k:env[k] for k in env if k.startswith('EPIC16_') or k=='PYTHONDONTWRITEBYTECODE'},exitCode=r.returncode,durationSeconds=time.time()-start,sourceUnchanged=unchanged,files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in directory.iterdir() if p.is_file()});(directory/'command.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record),flush=True);print((directory/'stdout').read_text()[-14000:]);sys.exit(r.returncode)
