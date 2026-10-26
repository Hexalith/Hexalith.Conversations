from pathlib import Path
import json,hashlib,subprocess,tempfile,time,os,datetime
from xml.etree import ElementTree as ET
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10/implementation';frozen=json.loads((out/'final-source.json').read_bytes())['files'];base=Path(tempfile.mkdtemp(prefix='epic16-r10-final-'));rows=[]
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def check():
 for p,h in frozen.items():assert sha(root/p)==h,p
for name,args in [('combined',['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py']),('legacy',['_bmad/scripts/tests/test_generate_story_record.py','_bmad/scripts/tests/test_epic16_tooling.py','-k','story_111 or story_112 or standalone_generator'])]:
 check();argv=['uv','run','--frozen','--no-sync','python3','-m','pytest','-q',*args,'--maxfail=3','--basetemp='+str(base/name),'--junitxml='+str(out/(name+'.xml'))];env={'EPIC16_COMMITLINT_EVIDENCE':str(out/(name+'-commitlint.json')),'EPIC16_R10_NATIVE_EVIDENCE':str(out/('native-'+name))};started=datetime.datetime.now(datetime.timezone.utc).isoformat();start=time.monotonic()
 print('START',name,started,flush=True)
 with (out/(name+'.stdout.txt')).open('wb') as stdout,(out/(name+'.stderr.txt')).open('wb') as stderr:
  result=subprocess.run(argv,cwd=root,stdout=stdout,stderr=stderr,env=dict(os.environ,**env))
 check();xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(s.get(k,0)) for s in suites) for k in ('tests','failures','errors','skipped')};row=dict(argv=argv,cwd=str(root),environment=env,startedAt=started,elapsedSeconds=round(time.monotonic()-start,3),exitCode=result.returncode,counts=counts,sourceUnchanged=True,sourceSnapshotSha256=sha(out/'final-source.json'),stdoutSha256=sha(out/(name+'.stdout.txt')),stderrSha256=sha(out/(name+'.stderr.txt')),junitSha256=sha(out/(name+'.xml')));rows.append(row);(out/'test-commands.json').write_text(json.dumps(rows,indent=2)+'\n');print('END',name,json.dumps(row),flush=True)
 assert result.returncode==0 and counts['failures']==counts['errors']==counts['skipped']==0
