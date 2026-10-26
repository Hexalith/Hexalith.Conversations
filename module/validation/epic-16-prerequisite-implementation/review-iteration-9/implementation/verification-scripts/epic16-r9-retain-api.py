from pathlib import Path
import json,hashlib,subprocess,sys,re
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation';target=out/'upstream-api-fixtures';target.mkdir(exist_ok=True)
sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
frozen=json.loads((out/'final-source.json').read_bytes())['files']
for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
workspaces=json.loads((out/'pytest-workspaces.json').read_bytes());repos=sorted((p for p in Path(workspaces['combined']).glob('test_round9_upstream_additiv*/fixture') if not p.parent.is_symlink() and re.search(r'(\d+)$',p.parent.name)),key=lambda p:int(re.search(r'(\d+)$',p.parent.name).group(1)))
changes=['addition','removed-member','signature','type','wrong-revision','wrong-owner','forged-digest','missing-assembly'];assert len(repos)==len(changes),(len(repos),repos)
def git(repo,*args):return subprocess.run(['git',*args],cwd=repo,check=True,capture_output=True).stdout
def keep(raw,path):
 p=target/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);return dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(raw).hexdigest())
rows=[]
for change,repo in zip(changes,repos):
 sid='AC-16.1-05';native_path=tooling.native_path('tooling-fixture',sid);raw=(repo/native_path).read_bytes();payload=json.loads(tooling.records.parse_trx(raw)['results'][0]['output'])
 result=tooling.evaluate(repo,'16.1',sid,scope='tooling-fixture');assert result['result']==('PASS' if change=='addition' else 'FAIL'),(change,result)
 baseline=git(repo,'rev-parse','HEAD^').decode().strip();candidate=git(repo,'rev-parse','HEAD').decode().strip();evidence=[]
 for product in tooling.products.PRODUCTS:
  owner=product['owner']
  if owner=='.':continue
  for phase,revision in [('baseline',baseline),('candidate',candidate)]:
   tree=git(repo,'ls-tree',revision,'--',owner);source_revision=tree.decode().split()[2]
   source='fixture-products/'+product['assemblyName']+'.cs';source_bytes=git(repo/owner,'show',source_revision+':'+source)
   evidence.append(dict(assemblyName=product['assemblyName'],owner=owner,phase=phase,rootRevision=revision,sourceRevision=source_revision,rawGitlink=keep(tree,change+'/'+phase+'-'+product['assemblyName']+'.gitlink.txt'),source=keep(source_bytes,change+'/'+phase+'-'+product['assemblyName']+'.cs.bytes')))
 binaries=[]
 for surface in payload['observations']['upstreamSurfaces']:
  for phase in ('baseline','candidate'):
   artifact=surface[phase];content=(repo/artifact['path']).read_bytes();actual=hashlib.sha256(content).hexdigest();binaries.append(dict(assemblyName=surface['assemblyName'],phase=phase,observation=artifact,actualSha256=actual,retained=keep(content,'binaries/'+actual+'.dll.bytes')))
 rows.append(dict(change=change,test='test_round9_upstream_additivity_uses_raw_gitlinks_and_actual_public_metadata['+change+']',fixtureRepository=str(repo),baseline=baseline,candidate=candidate,native=keep(raw,change+'/AC-16.1-05.trx.bytes'),result=keep(tooling.json_bytes(result),change+'/derived-result.json'),expectedResult=result['result'],blockers=result['blockers'],ownerSourceEvidence=evidence,binaries=binaries))
(target/'provenance.json').write_text(json.dumps(dict(scope='Exact synthetic disposable API fixtures from final combined run. Read-only revalidation derives PASS for addition and FAIL for seven controls. No runtime entry or owner authority.',argv=['uv','run','--frozen','--no-sync','python3',str(Path(__file__).relative_to(root))],sourceEpochSha256=hashlib.sha256((out/'final-source.json').read_bytes()).hexdigest(),cases=rows),indent=2)+'\n')
for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
print('Retained all eight final API fixtures with exact binary/source bytes, raw baseline/candidate gitlinks and read-only derived results')
