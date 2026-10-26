from pathlib import Path
from concurrent.futures import ThreadPoolExecutor,as_completed
from xml.etree import ElementTree as ET
import os,sys,json,shutil,tempfile,subprocess,hashlib,time,ast,difflib
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/mutations-nullable';out.mkdir(parents=True,exist_ok=False)
files=set((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs']);files.update(json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/exploratory-4/source.json').read_bytes())['files']);mutants=[]
construction='_bmad/scripts/epic16_native_helper/TestConstruction.cs';program='_bmad/scripts/epic16_native_helper/Program.cs';native='_bmad/scripts/epic16_native.py';capture='_bmad/scripts/epic16_capture.py';product='_bmad/scripts/epic16_products.py'
def add(name,path,old,new,selector):mutants.append(dict(name=name,path=path,old=old,new=new,selector=selector))
add('nullable-underlying-constraint',construction,'if (!NonNullableValueType(reader, argument))','if (false)','round13_nullable_constraint or (round13_runtime_caller and nullable-constraint)')

def run(mutant):
 name,path=mutant['name'],mutant['path'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r13-mutant-'+name+'-'));directory=out/name;directory.mkdir()
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 (workspace/'node_modules').symlink_to(root/'node_modules',target_is_directory=True)
 target=workspace/path;content=target.read_bytes();old=mutant['old'].encode();new=mutant['new'].encode()
 if path.endswith('.cs'):old=old.replace(b'\n',b'\r\n');new=new.replace(b'\n',b'\r\n')
 assert content.count(old)==1,(name,content.count(old));changed=content.replace(old,new);target.write_bytes(changed)
 (directory/'original.bytes').write_bytes(content);(directory/'mutated.bytes').write_bytes(changed);(directory/'change.diff').write_text(''.join(difflib.unified_diff(content.decode().splitlines(True),changed.decode().splitlines(True),fromfile=path,tofile=path)))
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',mutant['selector'],'--junitxml='+str(directory/'result.xml')]
 started=time.monotonic();r=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=700,env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',EPIC16_COMMITLINT_EVIDENCE=str(directory/'commitlint.json')))
 (directory/'stdout').write_bytes(r.stdout);(directory/'stderr').write_bytes(r.stderr);xml=ET.parse(directory/'result.xml').getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,cwd=str(workspace),exitCode=r.returncode,elapsedSeconds=time.monotonic()-started,originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(changed).hexdigest(),testSha256=hashlib.sha256((workspace/'_bmad/scripts/tests/test_epic16_tooling.py').read_bytes()).hexdigest(),tests=counts)
 (directory/'command.json').write_text(json.dumps(row,indent=2)+'\n');return row
results=[]
with ThreadPoolExecutor(max_workers=2) as pool:
 futures=[pool.submit(run,m) for m in mutants]
 for f in as_completed(futures):
  row=f.result();results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(row['name'],row['exitCode'],row['tests'],flush=True)
assert all(r['exitCode']==1 and r['tests']['failures']>0 and r['tests']['errors']==r['tests']['skipped']==0 for r in results),[(r['name'],r['tests']) for r in results if r['exitCode']!=1 or r['tests']['failures']==0 or r['tests']['errors'] or r['tests']['skipped']]
