import datetime,hashlib,json,os,pathlib,subprocess,sys
root=pathlib.Path(__file__).resolve().parent
mode,label,*command=sys.argv[1:]
prep_path=pathlib.Path('/tmp/conversations-catalog-20261102/prepare.json' if mode=='owner' else '/tmp/conversations-catalog-review-fixes/platform-prepare.json')
prep=json.loads(prep_path.read_text());values=prep.get('buildPreparation',prep)['environment'];env=os.environ.copy();env.update(values)
if mode=='platform': env['HEXALITH_CONVERSATIONS_CATALOG_INPUTS']='/tmp/conversations-catalog-review-fixes/platform-inputs.json'
record=dict(command=command,mode=mode,preparationPath=str(prep_path),preparationSha256=hashlib.sha256(prep_path.read_bytes()).hexdigest(),environmentKeys=sorted(values),startedAt=datetime.datetime.now(datetime.timezone.utc).isoformat())
with (root/(label+'.log')).open('w') as log: result=subprocess.run(command,env=env,stdout=log,stderr=subprocess.STDOUT)
record.update(exitCode=result.returncode,completedAt=datetime.datetime.now(datetime.timezone.utc).isoformat());(root/(label+'.json')).write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record));raise SystemExit(result.returncode)
