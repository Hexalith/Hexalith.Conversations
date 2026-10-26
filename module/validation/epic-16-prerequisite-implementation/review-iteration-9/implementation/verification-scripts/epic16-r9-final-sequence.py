from pathlib import Path
import json,hashlib,subprocess,sys,time
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation';frozen=json.loads((out/'final-source.json').read_bytes())['files'];rows=[]
def freeze():
 for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
for name in ('verify','held','provenance','state','retain-native'):
 freeze();argv=['uv','run','--frozen','--no-sync','python3',str((out/'verification-scripts'/('epic16-r9-'+name+'.py')).relative_to(root))];start=time.monotonic()
 with (out/('final-'+name+'.stdout.txt')).open('wb') as stdout,(out/('final-'+name+'.stderr.txt')).open('wb') as stderr:result=subprocess.run(argv,cwd=root,stdout=stdout,stderr=stderr)
 row=dict(name=name,argv=argv,exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),stdoutSha256=hashlib.sha256((out/('final-'+name+'.stdout.txt')).read_bytes()).hexdigest(),stderrSha256=hashlib.sha256((out/('final-'+name+'.stderr.txt')).read_bytes()).hexdigest());rows.append(row);(out/'final-sequence-commands.json').write_text(json.dumps(rows,indent=2)+'\n');print(name,result.returncode,flush=True);freeze()
 if result.returncode:sys.exit(result.returncode)
print('Final sequential verification passed with all frozen inputs unchanged',flush=True)
