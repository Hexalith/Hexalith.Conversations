import os,json,time,sys,subprocess
from pathlib import Path
base=Path('/tmp/conversations-catalog-review-fixes'); prep=json.loads((base/'platform-prepare.json').read_text());env=os.environ.copy();env.update(prep['environment']);env['HEXALITH_CONVERSATIONS_CATALOG_INPUTS']=str(base/'platform-inputs.json')
args=sys.argv[1:];path=base/('platform-'+time.strftime('%H%M%S')+'.log');print(path,flush=True)
with path.open('w') as out:
 out.write('START '+time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())+'\nCOMMAND '+repr(args)+'\n');out.flush();ret=subprocess.run(args,env=env,stdout=out,stderr=subprocess.STDOUT)
 out.write('\nEND '+time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())+' EXIT '+str(ret.returncode)+'\n')
print(path.read_text()[-18000:]);sys.exit(ret.returncode)
