from pathlib import Path
import hashlib,json,subprocess,re
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14';path='_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md';spec=root/path
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
s=spec.read_bytes();head=subprocess.check_output(['git','show','46ca7bf39b25318418b7145b7ce75bb306471cd0:'+path]);pattern=br'<frozen-after-approval[^>]*>.*?</frozen-after-approval>'
frozen=re.search(pattern,s,re.S).group();assert frozen==re.search(pattern,head,re.S).group()
a=json.loads((base/'parent-final-audit.json').read_bytes());assert a['combinedPassed']==3047 and a['legacyPassed']==72 and a['indexEmpty'] and a['sourceUnchanged']
old=json.loads((base/'parent-verification/guards/runtime-and-user-state.json').read_bytes());runtime=root/'_bmad-output/implementation-artifacts/spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md';sprint=root/'_bmad-output/implementation-artifacts/sprint-status.yaml'
assert sha(runtime)==old['runtimeSpecSha256'] and sha(sprint)==old['sprintSha256']
assert "1. [x]" in s.decode() and "6. [x]" in s.decode()
assert b"status: 'in-review'" in s.split(b'---',2)[1]
s=s.replace(b"status: 'in-review'",b"status: 'done'",1);spec.write_bytes(s)
assert re.search(pattern,s,re.S).group()==frozen
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)
record=dict(scope='Completion of the separate authorized Epic 16 prerequisite tooling spec',spec=path,specStatus='done',storyKey='',sprintStatusUpdated=False,storyCompletionGate=dict(applicable=False,reason='This separate prerequisite spec has no dotted story ID or V9 story contract. Runtime Story 16.1 has its own contract and is not transitioned or completed.',runtimeStoryContractExists=(root/'_bmad-output/planning-artifacts/v9/story-contracts/16.1.json').is_file()),evidenceBoundary=dict(result='not-applicable',reason='Explicit runtime publication is outside prerequisite scope; routine focused checks govern this transition. The actual held commands remain BLOCKED and provide no runtime acceptance.'),actualRuntime=dict(status='draft/backlog',hold='ACTIVE',result='BLOCKED',exitCode=2,blockers=['ENTRY_QUALIFICATION_PENDING'],specSha256=sha(runtime),sprintSha256=sha(sprint)),frozenIntentSha256=hashlib.sha256(frozen).hexdigest(),frozenIntentMatchesCommittedCopy=True,commit=dict(created=False,reason='Explicit user-approved frozen scope excludes staging, commits and pushes; it overrides the skill default local commit instruction.'),indexEmpty=True,review=dict(reports=3,filedFindings=12,patchRoots=6,resolved=True,newDeferred=0,reviewLoopIteration=13),sourceSnapshotSha256=sha(base/'parent-verification/combined/source.json'))
(base/'completion-disposition.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record,indent=2))
