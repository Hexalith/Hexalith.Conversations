from pathlib import Path
import tempfile,subprocess,json,hashlib,shutil
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation';out=base/'binary-diffs';out.mkdir(exist_ok=False);history=json.loads((base/'restoration.json').read_bytes())['priorEvidence'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r13-diffs-'));shutil.copyfile(root/'.gitattributes',workspace/'.gitattributes');commands=[]
def run(argv):
 r=subprocess.run(argv,cwd=workspace,capture_output=True);commands.append(dict(argv=argv,cwd=str(workspace),exitCode=r.returncode,stdout=r.stdout.decode('utf-8'),stderr=r.stderr.decode('utf-8')));assert r.returncode==0;return r.stdout
run(['git','init','-q']);records=[]
for shape in ('Evidence','Goldens','Epic16.NativeHelper','apphost','.pdb','.snk','.cache','.dll','.dll.bytes'):
 candidates=[p for p in history if (Path(p).name.endswith(shape) if shape.startswith('.') else Path(p).name==shape) and b'\0' in (root/p).read_bytes()];assert candidates,shape
 path=candidates[0];source=root/path;target=workspace/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
 attrs=run(['git','check-attr','text','diff','whitespace','--',path]).decode();assert all(path+': '+key+': unset' in attrs for key in ('text','diff','whitespace')),attrs
 run(['git','add','-f','--',path]);diff=run(['git','diff','--cached','--',path]);assert b'Binary files ' in diff and b'GIT binary patch' not in diff
 digest=hashlib.sha256(source.read_bytes()).hexdigest();assert digest==history[path]==hashlib.sha256(target.read_bytes()).hexdigest();(out/(shape.replace('.','dot')+'.diff')).write_bytes(diff)
 records.append(dict(shape=shape,sourcePath=path,sourceSha256=digest,byteLength=source.stat().st_size,normalDiff='standard Binary files representation',sourceUnchanged=True))
(out/'manifest.json').write_text(json.dumps(dict(scope='Exact previously retained binary bytes in disposable Git index; no real index mutation',attributeSha256=hashlib.sha256((root/'.gitattributes').read_bytes()).hexdigest(),workspace=str(workspace),records=records,commands=commands),indent=2)+'\n');print(len(records),'actual binary shapes checked')
