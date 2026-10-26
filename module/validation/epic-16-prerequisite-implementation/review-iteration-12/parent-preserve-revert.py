from pathlib import Path
import hashlib,json,subprocess,tempfile,shutil
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12'
old=json.loads(Path('/tmp/epic16-review11-preservation-9mpchdvv/manifest.json').read_text());tracked=old['tracked'];new=old['new'];owned=tracked+new
assert len(owned)==len(set(owned))==42 and len(tracked)==4 and len(new)==38
assert 'global.json' not in owned and '.gitignore' in tracked
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
frozen=json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/implementation/final-source.json').read_text())['files']
assert len(frozen)==86
for p,h in frozen.items():assert sha(root/p)==h,('frozen',p)
for p,h in old['protected'].items():assert sha(root/p)==h,('protected',p)
assert subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)==b''
tracked_set=set(subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0'))
assert all(p in tracked_set for p in tracked) and all(p not in tracked_set for p in new)
evidence_root=root/'module/validation/epic-16-prerequisite-implementation'
evidence={str(p.relative_to(root)):sha(p) for p in evidence_root.rglob('*') if p.is_file()}
backup=Path(tempfile.mkdtemp(prefix='epic16-review12-preservation-'))
hashes={}
for p in owned:
 dest=backup/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/p,dest);hashes[p]=sha(root/p);assert sha(dest)==hashes[p]
manifest=dict(tracked=tracked,new=new,baseline=old['baseline'],head=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),reviewedDiff='/tmp/epic16-review-next-5pl7ogai/complete.diff',reviewedDiffSha256=sha(Path('/tmp/epic16-review-next-5pl7ogai/complete.diff')),sha256=hashes,protected=old['protected'])
(backup/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');manifest_hash=sha(backup/'manifest.json')
(out/'preservation.json').write_text(json.dumps(dict(path=str(backup/'manifest.json'),sha256=manifest_hash,ownedFiles=42,trackedFiles=4,newFiles=38),indent=2)+'\n')
# Restore only the exact task-owned files to their original baseline, preserving index.
for p in tracked:
 (root/p).write_bytes(subprocess.check_output(['git','show',old['baseline']+':'+p],cwd=root))
for p in new:(root/p).unlink()
for p,h in evidence.items():assert sha(root/p)==h,('evidence',p)
for p,h in old['protected'].items():assert sha(root/p)==h,('protected',p)
assert subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)==b''
result=dict(backup=str(backup),manifestSha256=manifest_hash,ownedFiles=42,trackedFiles=4,newFiles=38,frozenVerified=86,protectedVerified=5,trackedRestored=4,newFilesRemoved=38,allEvidencePreserved=True,retainedEvidenceFiles=len(evidence),indexEmpty=True)
(out/'reversion.json').write_text(json.dumps(result,indent=2)+'\n');shutil.copyfile('/tmp/epic16-review12-preserve-revert.py',out/'parent-preserve-revert.py')
print(json.dumps(result,indent=2))
