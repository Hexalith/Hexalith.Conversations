from pathlib import Path
import subprocess,hashlib,json,sys,time,shutil
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';scripts=base/'parent-audit-scripts';scripts.mkdir(exist_ok=True);logs=base/'parent-audit-commands';logs.mkdir(exist_ok=True);log=base/'parent-audit-commands.json';records=json.loads(log.read_bytes()) if log.exists() else [];sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for label,name,args in [('acceptance','acceptance',[]),('artifacts','artifacts',[]),('mutations','mutations-order',['final']),('native','native-retention-order',[]),('state','state-order',[])]:
 source=Path('/tmp/epic16-review13-parent-'+name+'.py');retained=scripts/source.name
 if retained.exists():assert retained.read_bytes()==source.read_bytes(),str(retained)
 else:shutil.copyfile(source,retained)
 attempt=1+sum(r['label']==label for r in records);folder=logs/(label+'-'+str(attempt));folder.mkdir();argv=['python3',str(retained),*args];started=time.time()
 with (folder/'stdout').open('wb') as stdout,(folder/'stderr').open('wb') as stderr:result=subprocess.run(argv,cwd=root,stdout=stdout,stderr=stderr)
 row=dict(label=label,attempt=attempt,argv=argv,cwd=str(root),exitCode=result.returncode,elapsedSeconds=time.time()-started,script=dict(path=str(retained.relative_to(root)),sha256=sha(retained)),stdout=dict(path=str((folder/'stdout').relative_to(root)),sha256=sha(folder/'stdout')),stderr=dict(path=str((folder/'stderr').relative_to(root)),sha256=sha(folder/'stderr')));records.append(row);log.write_text(json.dumps(records,indent=2)+'\n');print(json.dumps(dict(label=label,attempt=attempt,exitCode=result.returncode)),flush=True)
 if result.returncode:
  print((folder/'stdout').read_text()[-4000:]);print((folder/'stderr').read_text()[-4000:]);sys.exit(result.returncode)
