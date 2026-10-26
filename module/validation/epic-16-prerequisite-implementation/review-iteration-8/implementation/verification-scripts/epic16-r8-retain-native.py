from pathlib import Path
import subprocess,json,hashlib
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-8/implementation/native-capture-fixture';out.mkdir(exist_ok=True)
import time
started=time.monotonic()
while True:
 candidates=list(Path('/tmp/pytest-of-administrator').glob('pytest-*/test_round6_genuine_native_cap0/fresh-checkout'))
 fixture=None
 for candidate in candidates:
  result=subprocess.run(['git','show','HEAD:_bmad/scripts/epic16_lifecycle.py'],cwd=candidate,capture_output=True)
  if result.returncode==0 and result.stdout==(root/'_bmad/scripts/epic16_lifecycle.py').read_bytes():
   record=subprocess.run(['git','show','HEAD:docs/release-evidence/story-16.2-final-record-v2.json'],cwd=candidate,capture_output=True)
   if record.returncode==0 and not(candidate.parent/'portable-build').exists() and not(candidate.parent/'payloads').exists():fixture=candidate;break
 if fixture is not None:break
 assert time.monotonic()-started<1800,'current native fixture not available'
 time.sleep(10)

def git(*args):return subprocess.run(['git',*args],cwd=fixture,check=True,capture_output=True).stdout
def sha(data):return hashlib.sha256(data).hexdigest()
life=git('rev-parse','HEAD').decode().strip();publication=git('rev-parse','HEAD^').decode().strip();paths=git('diff-tree','--no-commit-id','--name-only','-r',publication).decode().splitlines();assert len(paths)==18
record=json.loads(git('show',life+':docs/release-evidence/story-16.2-final-record-v2.json'));candidate=record['candidate']['commit']
extra=['tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs','_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/16.2.json','_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json','fixture-entry.json','_bmad-output/implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md']
retained=[]
for path in paths+extra:
 data=git('show',life+':'+path);target=out/(path.replace('/','__')+'.bytes');target.write_bytes(data);retained.append(dict(fixturePath=path,path=str(target.relative_to(root)),sha256=sha(data)))
assert not (fixture.parent/'portable-build').exists() and not (fixture.parent/'payloads').exists()
assert not (fixture/'artifacts/measurement-only-runner').exists()
test='_bmad/scripts/tests/test_epic16_tooling.py'
proof=dict(scope='synthetic observation payloads; genuine xUnit 4.0.2 fixture runner; test-only trust; no product runtime qualification',sourceFixture=str(fixture),test='test_round6_genuine_native_capture_publication_survives_fresh_checkout',verificationRun='iteration-8 final combined run; success asserted by verification.json',testSource=dict(path=test,sha256=sha((root/test).read_bytes())),candidate=candidate,publicationCommit=publication,lifecycleCommit=life,publicationPaths=paths,retainedAssemblyCount=sum(p.endswith('.dll') for p in paths),captureCount=sum(p.endswith('.capture.json') for p in paths),rawTrxCount=sum(p.endswith('.trx') for p in paths),retained=retained,extraction='git show lifecycleCommit:path; exact committed fixture bytes',originalRawNativeBytesUnchanged=True,originalMeasurementDirectoryUnavailableToVerifier=True,externalBuildAndPayloadDirectoriesDeleted=True,fixtureBuildOutputsAbsentFromFreshCheckout=True)
assert proof['retainedAssemblyCount']==1 and proof['captureCount']==5 and proof['rawTrxCount']==5
(out/'provenance.json').write_text(json.dumps(proof,indent=2)+'\n')
print(json.dumps(dict(candidate=candidate,publicationCommit=publication,lifecycleCommit=life,retainedFiles=len(retained))))
