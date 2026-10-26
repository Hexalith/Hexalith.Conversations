from pathlib import Path
import subprocess,json,hashlib
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10/implementation';out=base/'native-capture-fixture';out.mkdir(exist_ok=True)
commands=json.loads((base/'test-commands.json').read_bytes());temporary=Path(next(a.split('=',1)[1] for a in commands[0]['argv'] if a.startswith('--basetemp=')));candidates=sorted({p.resolve() for p in temporary.glob('test_round6_genuine_native_cap*/fresh-checkout')});assert len(candidates)==1,candidates;fixture=candidates[0]
def git(*args):return subprocess.run(['git',*args],cwd=fixture,check=True,capture_output=True).stdout
def sha(data):return hashlib.sha256(data).hexdigest()
life=git('rev-parse','HEAD').decode().strip();publication=git('rev-parse','HEAD^').decode().strip();paths=git('diff-tree','--no-commit-id','--name-only','-r',publication).decode().splitlines()
record=json.loads(git('show',life+':docs/release-evidence/story-16.2-final-record-v2.json'));candidate=record['candidate']['commit']
extra=['tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs','_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/16.2.json','_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json','fixture-entry.json','_bmad-output/implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md'];retained=[]
for path in paths+extra:
 data=git('show',life+':'+path);target=out/(path.replace('/','__')+'.bytes');target.write_bytes(data);retained.append(dict(fixturePath=path,path=str(target.relative_to(root)),sha256=sha(data)))
assert not (fixture.parent/'portable-build').exists() and not (fixture.parent/'payloads').exists() and not (fixture/'artifacts/measurement-only-runner').exists()
capture_paths=[p for p in paths if p.endswith('.capture.json')];binary_paths=[p for p in paths if p.endswith('.dll')];required=set()
for p in capture_paths:
 c=json.loads(git('show',life+':'+p));required.add(c['binary']['path']);required.update(r['path'] for r in c['products'])
assert required<=set(binary_paths)
proof=dict(scope='synthetic observation payloads and seven source-stamped stand-ins; genuine xUnit 4.0.2 fixture runner; test-only trust; no product runtime qualification',sourceFixture=str(fixture),test='test_round6_genuine_native_capture_publication_survives_fresh_checkout',verificationRun='iteration-10 final combined run; success asserted by verification.json',testSource=dict(path='_bmad/scripts/tests/test_epic16_tooling.py',sha256=sha((root/'_bmad/scripts/tests/test_epic16_tooling.py').read_bytes())),candidate=candidate,publicationCommit=publication,lifecycleCommit=life,publicationPaths=paths,publicationPathCount=len(paths),retainedAssemblyCount=len(binary_paths),captureCount=len(capture_paths),rawTrxCount=sum(p.endswith('.trx') for p in paths),retained=retained,extraction='git show lifecycleCommit:path; exact committed fixture bytes',originalRawNativeBytesUnchanged=True,originalMeasurementDirectoryUnavailableToVerifier=True,externalBuildAndPayloadDirectoriesDeleted=True,fixtureBuildOutputsAbsentFromFreshCheckout=True)
assert proof['captureCount']==proof['rawTrxCount']==5
(out/'provenance.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(dict(candidate=candidate,publicationCommit=publication,lifecycleCommit=life,publicationPathCount=len(paths),retainedFiles=len(retained),retainedAssemblyCount=len(binary_paths))))
