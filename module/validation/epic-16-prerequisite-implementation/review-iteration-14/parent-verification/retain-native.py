from pathlib import Path
import subprocess,json,hashlib,base64
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/parent-verification';out=base/'native-capture-fixture';out.mkdir(exist_ok=False)
command=json.loads((base/'combined/command.json').read_bytes());temporary=Path(next(a.split('=',1)[1] for a in command['argv'] if a.startswith('--basetemp=')));candidates=sorted({p.resolve() for p in temporary.glob('test_round6_genuine_native_cap*/fresh-checkout')});assert len(candidates)==1,candidates;fixture=candidates[0]
commands=[]
def git(*args):
 argv=['git',*args];r=subprocess.run(argv,cwd=fixture,check=True,capture_output=True);commands.append(dict(argv=argv,cwd=str(fixture),exitCode=r.returncode,stdoutSha256=hashlib.sha256(r.stdout).hexdigest(),stderrSha256=hashlib.sha256(r.stderr).hexdigest()));return r.stdout
def sha(data):return hashlib.sha256(data).hexdigest()
life=git('rev-parse','HEAD').decode().strip();publication=git('rev-parse','HEAD^').decode().strip();paths=git('diff-tree','--no-commit-id','--name-only','-r',publication).decode().splitlines()
record=json.loads(git('show',life+':docs/release-evidence/story-16.2-final-record-v2.json'));candidate=record['candidate']['commit']
extra=['tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs','_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/16.2.json','_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json','fixture-entry.json','_bmad-output/implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md'];retained=[]
for path in paths+extra:
 data=git('show',life+':'+path);target=out/(path.replace('/','__')+'.bytes');target.write_bytes(data);oid=git('rev-parse',life+':'+path).decode().strip();assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==oid;retained.append(dict(fixturePath=path,path=str(target.relative_to(root)),sha256=sha(data),gitObjectId=oid,gitObjectType='blob'))
assert not (fixture.parent/'portable-build').exists() and not (fixture.parent/'payloads').exists() and not (fixture/'artifacts/measurement-only-runner').exists()
captures=[p for p in paths if p.endswith('.capture.json')];binaries=[p for p in paths if p.endswith('.dll')];required=set()
for p in captures:
 c=json.loads(git('show',life+':'+p));assert len(c['products'])==9;required.add(c['binary']['path']);required.update(r['path'] for r in c['products'])
assert required<=set(binaries)
objects={}
for commit in (candidate,publication,life):
 tree=git('rev-parse',commit+'^{tree}').decode().strip();treeids=[tree]+[line.split()[2].decode() for line in git('ls-tree','-r','-t',commit).splitlines() if line.split()[1]==b'tree']
 for kind,oid in [('commit',commit),*[('tree',oid) for oid in treeids]]:
  raw=git('cat-file',kind,oid);assert hashlib.sha1(kind.encode()+b' '+str(len(raw)).encode()+b'\0'+raw).hexdigest()==oid;objects[oid]=dict(type=kind,rawBase64=base64.b64encode(raw).decode(),sha256=sha(raw))
(out/'git-objects.json').write_text(json.dumps(dict(scope='Exact candidate/publication/lifecycle commit and tree objects; publication blobs are the retained exact files',objects=objects,blobs=retained),indent=2)+'\n')
proof=dict(scope='Synthetic observations and nine source-stamped stand-ins; genuine xUnit 4.0.2 fixture runner; test-only trust; no product runtime qualification',sourceFixture=str(fixture),test='test_round6_genuine_native_capture_publication_survives_fresh_checkout',verificationRun='review-14 completed parent combined run',testSource=dict(path='_bmad/scripts/tests/test_epic16_tooling.py',sha256=sha((root/'_bmad/scripts/tests/test_epic16_tooling.py').read_bytes())),candidate=candidate,publicationCommit=publication,lifecycleCommit=life,publicationPaths=paths,publicationPathCount=len(paths),retainedAssemblyCount=len(binaries),captureCount=len(captures),rawTrxCount=sum(p.endswith('.trx') for p in paths),retained=retained,gitObjects='git-objects.json',extraction='git show lifecycleCommit:path; exact committed fixture bytes',originalRawNativeBytesUnchanged=True,originalMeasurementDirectoryUnavailableToVerifier=True,externalBuildAndPayloadDirectoriesDeleted=True,fixtureBuildOutputsAbsentFromFreshCheckout=True)
assert proof['captureCount']==proof['rawTrxCount']==5
(out/'provenance.json').write_text(json.dumps(proof,indent=2)+'\n');(out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n');print(json.dumps(dict(candidate=candidate,publicationCommit=publication,lifecycleCommit=life,publicationPathCount=len(paths),retainedFiles=len(retained),retainedAssemblyCount=len(binaries))))
