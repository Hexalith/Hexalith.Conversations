from pathlib import Path
import json,hashlib,subprocess,time,sys
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation';sequence=json.loads((out/'final-sequence-commands.json').read_bytes());assert len(sequence)==5 and all(r['exitCode']==0 for r in sequence)
frozen=json.loads((out/'final-source.json').read_bytes())['files'];rows=[]
def freeze():
 for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
for name in ('retain-api','retain-signatures','docs'):
 freeze();argv=['uv','run','--frozen','--no-sync','python3',str((out/'verification-scripts'/('epic16-r9-'+name+'.py')).relative_to(root))];started=time.monotonic()
 with (out/('wrapup-'+name+'.stdout.txt')).open('wb') as stdout,(out/('wrapup-'+name+'.stderr.txt')).open('wb') as stderr:result=subprocess.run(argv,cwd=root,stdout=stdout,stderr=stderr)
 rows.append(dict(name=name,argv=argv,exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-started,3),stdoutSha256=hashlib.sha256((out/('wrapup-'+name+'.stdout.txt')).read_bytes()).hexdigest(),stderrSha256=hashlib.sha256((out/('wrapup-'+name+'.stderr.txt')).read_bytes()).hexdigest()));(out/'wrapup-commands.json').write_text(json.dumps(rows,indent=2)+'\n');print(name,result.returncode,flush=True);freeze()
 if result.returncode:sys.exit(result.returncode)
