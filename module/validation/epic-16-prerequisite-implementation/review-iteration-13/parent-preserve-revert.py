from pathlib import Path
import hashlib,json,subprocess,tempfile,shutil
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13'
old=json.loads(Path('/tmp/epic16-review12-preservation-mdzqxec0/manifest.json').read_text());tracked=old['tracked'];new=old['new'];owned=tracked+new
assert len(owned)==len(set(owned))==42 and len(tracked)==4 and len(new)==38
assert 'global.json' not in owned and '.gitignore' in tracked
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
frozen=json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation/packaging-source.json').read_text())['files']
assert len(frozen)==86
for p,h in frozen.items():assert sha(root/p)==h,('frozen',p)
for p,h in old['protected'].items():assert sha(root/p)==h,('protected',p)
assert subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)==b''
head=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip();assert head=='46ca7bf39b25318418b7145b7ce75bb306471cd0'
tracked_set=set(subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0'));assert all(p in tracked_set for p in owned)
evidence_root=root/'module/validation/epic-16-prerequisite-implementation';evidence={str(p.relative_to(root)):sha(p) for p in evidence_root.rglob('*') if p.is_file()}
backup=Path(tempfile.mkdtemp(prefix='epic16-review13-preservation-'));hashes={}
for p in owned:
 dest=backup/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/p,dest);hashes[p]=sha(root/p);assert sha(dest)==hashes[p]
user_rule=b'module/validation/epic-16-prerequisite-implementation/** -text diff -whitespace\n';assert (root/'.gitattributes').read_bytes().count(user_rule)==1
manifest=dict(tracked=tracked,new=new,baseline=old['baseline'],head=head,reviewedDiff='/tmp/epic16-review-next-4q9n86rc/complete.diff',reviewedDiffSha256=sha(Path('/tmp/epic16-review-next-4q9n86rc/complete.diff')),sha256=hashes,protected=old['protected'],currentlyTrackedPaths=owned,partitionMeaning='tracked/new record original task-baseline ownership; all42 paths are now tracked by the concurrent user commit',preserveConcurrentUserRule=user_rule.decode().strip())
(backup/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');manifest_hash=sha(backup/'manifest.json')
(out/'preservation.json').write_text(json.dumps(dict(path=str(backup/'manifest.json'),sha256=manifest_hash,ownedFiles=42,originalBaselineTrackedFiles=4,originalTaskAddedFiles=38,currentlyTrackedFiles=42),indent=2)+'\n')
for p in tracked:
 raw=subprocess.check_output(['git','show',old['baseline']+':'+p],cwd=root)
 if p=='.gitattributes':raw+=b'\n'+user_rule
 (root/p).write_bytes(raw)
for p in new:(root/p).unlink()
for p,h in evidence.items():assert sha(root/p)==h,('evidence',p)
for p,h in old['protected'].items():assert sha(root/p)==h,('protected',p)
assert (root/'.gitattributes').read_bytes().count(user_rule)==1
assert subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)==b''
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==head
result=dict(backup=str(backup),manifestSha256=manifest_hash,ownedFiles=42,originalBaselineTrackedFiles=4,originalTaskAddedFiles=38,currentTrackedFiles=42,frozenVerified=86,protectedVerified=5,baselineRestored=4,taskAddedFilesRemoved=38,concurrentAttributeRulePreserved=True,concurrentHeadPreserved=head,allEvidencePreserved=True,retainedEvidenceFiles=len(evidence),indexEmpty=True)
(out/'reversion.json').write_text(json.dumps(result,indent=2)+'\n');shutil.copyfile('/tmp/epic16-review13-preserve-revert.py',out/'parent-preserve-revert.py');print(json.dumps(result,indent=2))
