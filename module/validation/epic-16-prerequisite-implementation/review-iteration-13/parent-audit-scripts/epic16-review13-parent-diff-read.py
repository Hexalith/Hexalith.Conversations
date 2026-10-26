from pathlib import Path
import json,hashlib,re,difflib,shutil
root=Path.cwd();new=Path('/tmp/epic16-review-next-1ry5kka_');old=Path('/tmp/epic16-review-next-4q9n86rc');out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';sha=lambda b:hashlib.sha256(b).hexdigest()
def read(path):
 raw=path.read_bytes();raw.decode('utf-8');chunks=re.split(rb'(?=^diff --git )',raw,flags=re.M);sections={}
 for c in chunks:
  if not c:continue
  header=c.splitlines()[0].decode();m=re.fullmatch(r'diff --git a/(.+) b/(.+)',header)
  assert m and m[1]==m[2],header
  assert m[1] not in sections,m[1]
  sections[m[1]]=c
 return raw,sections
raw,current=read(new/'complete.diff');oldraw,prior=read(old/'complete.diff');meta=json.loads((new/'manifest.json').read_bytes());assert sha(raw)==meta['sha256'];assert not meta['binaryRerenders'];changed=[p for p in current if current[p]!=prior.get(p)];unchanged=[p for p in current if p in prior and current[p]==prior[p]];missing=sorted(set(prior)-set(current));assert not missing,missing
non_evidence=[p for p in changed if not p.startswith('module/validation/epic-16-prerequisite-implementation/')];lines=[]
for p in non_evidence:
 lines.extend(difflib.unified_diff(prior.get(p,b'').decode().splitlines(True),current[p].decode().splitlines(True),fromfile='prior-reviewed-section/'+p,tofile='current-section/'+p))
(out/'parent-final-semantic-delta.diff').write_text(''.join(lines))
index=[dict(path=p,bytes=len(c),sha256=sha(c),binary=b'GIT binary patch' in c,previouslyReviewedUnchanged=p in unchanged) for p,c in current.items()];(out/'parent-staged-diff-inventory.json').write_text(json.dumps(index,indent=2)+'\n')
report=dict(baseline=meta['baseline'],head=meta['head'],diff=meta['diff'],diffSha256=meta['sha256'],bytes=meta['bytes'],sections=len(current),untracked=meta['untrackedFiles'],unchangedReviewedSections=len(unchanged),changedSections=len(changed),changedNonEvidence=non_evidence,allPriorSectionsPreserved=True,normalGitDiffUtf8=True,binaryRerenders=0,indexUnchanged=True,newSemanticDeltaLines=len(lines),parentCommitlintAuditIncluded='module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-commitlint-audit.json' in current)
(out/'parent-staged-diff-read.json').write_text(json.dumps(report,indent=2)+'\n');shutil.copyfile(new/'complete.diff',new/'step03.diff');shutil.copyfile(new/'manifest.json',new/'step03-manifest.json');print(json.dumps(report,indent=2))
