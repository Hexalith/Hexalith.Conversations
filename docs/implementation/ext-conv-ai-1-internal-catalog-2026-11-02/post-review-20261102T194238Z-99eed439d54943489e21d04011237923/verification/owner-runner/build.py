import json,os,subprocess,time,sys
from pathlib import Path
p=json.load(open('/tmp/conversations-catalog-20261102/prepare.json'))
env=os.environ.copy(); env.update(p['buildPreparation']['environment'])
args=sys.argv[1:]
path=Path('/tmp/conversations-catalog-20261102')/('build-'+time.strftime('%H%M%S')+'.log')
print(str(path),flush=True)
with path.open('w') as out:
 out.write('START '+time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())+'\nCOMMAND '+repr(args)+'\n');out.flush()
 ret=subprocess.run(args,env=env,stdout=out,stderr=subprocess.STDOUT)
 out.write('\nEND '+time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())+' EXIT '+str(ret.returncode)+'\n')
print(path.read_text()[-17000:]);sys.exit(ret.returncode)
