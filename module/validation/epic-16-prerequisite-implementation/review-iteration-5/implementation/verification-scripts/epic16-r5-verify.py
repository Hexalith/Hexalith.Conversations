from pathlib import Path
import subprocess,json,hashlib,time,sys
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-5/implementation';out.mkdir(parents=True,exist_ok=True)
source=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes())
extra=['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py']
(out/'verification-source.json').write_text(json.dumps(dict(bindingSha256=hashlib.sha256((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes()).hexdigest(),binding=source,tests=[dict(path=p,sha256=hashlib.sha256((root/p).read_bytes()).hexdigest()) for p in extra]),indent=2)+'\n')
rows=[]
for name,files,select in [('combined',['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py'],[]),('legacy',['_bmad/scripts/tests/test_generate_story_record.py','_bmad/scripts/tests/test_epic16_tooling.py'],['-k','story_111 or story_112 or standalone_generator'])]:
 argv=['uv','run','--frozen','--no-sync','python3','-m','pytest','-q',*files,*select,'--maxfail=3','--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic()
 with (out/(name+'.stdout.txt')).open('wb') as stdout,(out/(name+'.stderr.txt')).open('wb') as stderr:
  result=subprocess.run(argv,cwd=root,stdout=stdout,stderr=stderr)
 row=dict(argv=argv,exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),stdoutSha256=hashlib.sha256((out/(name+'.stdout.txt')).read_bytes()).hexdigest(),stderrSha256=hashlib.sha256((out/(name+'.stderr.txt')).read_bytes()).hexdigest(),junitSha256=hashlib.sha256((out/(name+'.xml')).read_bytes()).hexdigest());rows.append(row);(out/'test-commands.json').write_text(json.dumps(rows,indent=2)+'\n');print(name,result.returncode,(out/(name+'.stdout.txt')).read_text()[-1800:],flush=True)
 if result.returncode:sys.exit(result.returncode)
