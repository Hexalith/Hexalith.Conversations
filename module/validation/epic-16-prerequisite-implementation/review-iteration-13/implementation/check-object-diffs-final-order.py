from pathlib import Path
import subprocess,json,hashlib,base64,tempfile,shutil
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/native-capture-fixture-final-order';path=str((base/'git-objects.json').relative_to(root));raw=(root/path).read_bytes();doc=json.loads(raw);assert b'\0' not in raw
for oid,row in doc['objects'].items():
 data=base64.b64decode(row['rawBase64'],validate=True);assert hashlib.sha256(data).hexdigest()==row['sha256'];assert hashlib.sha1(row['type'].encode()+b' '+str(len(data)).encode()+b'\0'+data).hexdigest()==oid
for row in doc['blobs']:
 data=(root/row['path']).read_bytes();assert hashlib.sha256(data).hexdigest()==row['sha256'];assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==row['gitObjectId']
workspace=Path(tempfile.mkdtemp(prefix='epic16-r13-object-diffs-'));shutil.copyfile(root/'.gitattributes',workspace/'.gitattributes');target=workspace/path;target.parent.mkdir(parents=True);target.write_bytes(raw);commands=[]
def run(argv):
 r=subprocess.run(argv,cwd=workspace,capture_output=True);assert r.returncode==0;commands.append(dict(argv=argv,cwd=str(workspace),exitCode=r.returncode,stdoutSha256=hashlib.sha256(r.stdout).hexdigest(),stderrSha256=hashlib.sha256(r.stderr).hexdigest()));return r.stdout
run(['git','init','-q']);attrs=run(['git','check-attr','diff','--',path]);assert b': diff: set' in attrs;run(['git','add','-f','--',path]);diff=run(['git','diff','--cached','--',path]);diff.decode('utf-8');assert b'Binary files ' not in diff and b'\0' not in diff and b'rawBase64' in diff
(base/'git-objects.normal.diff').write_bytes(diff);(base/'object-diff-proof.json').write_text(json.dumps(dict(scope='Exact commit/tree bytes reversibly base64 encoded in readable JSON; existing publication blobs retain their previous file-type representation',commands=commands,gitObjects=len(doc['objects']),verifiedBlobs=len(doc['blobs']),jsonSha256=hashlib.sha256(raw).hexdigest(),normalDiffSha256=hashlib.sha256(diff).hexdigest(),allRawGitObjectIdsVerified=True,allRawSha256Verified=True,normalDiffUtf8=True),indent=2)+'\n');print(len(doc['objects']),'objects and',len(doc['blobs']),'blobs verified')
