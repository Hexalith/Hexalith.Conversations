from pathlib import Path
import hashlib,json,subprocess
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10';record_path=base/'verification.json';record=json.loads(record_path.read_bytes())
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
paths=[]
for row in record['artifacts']:
 path=Path(row['path']);assert not path.is_absolute() and '..' not in path.parts,row
 target=root/path;assert target.is_file() and not target.is_symlink(),row
 for parent in target.parents:
  if parent==root:break
  assert not parent.is_symlink(),str(parent)
 assert sha(target)==row['sha256'],row['path'];paths.append(row['path'])
assert len(paths)==len(set(paths)),'duplicate artifact rows'
ignored=subprocess.run(['git','check-ignore','--stdin','-z'],input=b'\0'.join(p.encode() for p in paths)+b'\0',capture_output=True)
assert ignored.returncode in (0,1) and not ignored.stdout,ignored.stdout.decode(errors='replace')
actual={str(p.relative_to(root)) for p in (base/'implementation').rglob('*') if p.is_file()}
assert set(paths)==actual,dict(missing=sorted(actual-set(paths)),extra=sorted(set(paths)-actual))
for key,ref in record.items():
 if isinstance(ref,dict) and 'path' in ref and 'sha256' in ref:assert sha(root/ref['path'])==ref['sha256'],key
frozen=json.loads((base/'implementation/final-source.json').read_bytes())
for path,value in frozen['files'].items():assert sha(root/path)==value,path
result=dict(verificationSha256=sha(record_path),artifactHashesVerified=len(paths),exactImplementationArtifactInventory=True,allArtifactsTrackable=True,frozenFilesVerified=len(frozen['files']),topLevelReferencesVerified=True,scope='Final prerequisite implementation evidence; no runtime qualification or owner approval.')
(base/'parent-artifact-audit.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
