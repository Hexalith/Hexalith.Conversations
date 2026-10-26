from pathlib import Path
import ast,json,hashlib,difflib
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11';backup=Path('/tmp/epic16-review11-preservation-9mpchdvv')
m=json.loads((backup/'manifest.json').read_bytes());f=json.loads((base/'implementation/final-source.json').read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
assert all(sha(root/p)==h for p,h in f['files'].items())
archive=base/'implementation/final-source';mapping=json.loads((archive/'archive-paths.json').read_bytes());assert mapping=={'.gitignore':'.gitignore.bytes'}
assert all(sha(archive/mapping.get(p,p))==h for p,h in f['files'].items())
binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes());assert binding['entry'] is None
for row in binding['sources']:assert sha(root/row['path'])==row['sha256']
sections=[];changed=[]
for name,h in m['sha256'].items():
 if sha(root/name)==h:continue
 changed.append(name)
 if name.endswith(('.py','.cs','.md')):
  a=(backup/name).read_text();b=(root/name).read_text();sections.extend(difflib.unified_diff(a.splitlines(True),b.splitlines(True),fromfile='before/'+name,tofile='after/'+name))
diff=base/'parent-final-source.diff';diff.write_text(''.join(sections))
path='_bmad/scripts/tests/test_epic16_tooling.py';old=ast.parse((backup/path).read_text());new=ast.parse((root/path).read_text());oldf={n.name:n for n in old.body if isinstance(n,(ast.FunctionDef,ast.AsyncFunctionDef,ast.ClassDef))};newf={n.name:n for n in new.body if isinstance(n,(ast.FunctionDef,ast.AsyncFunctionDef,ast.ClassDef))};modified=[n for n in oldf if ast.dump(oldf[n])!=ast.dump(newf[n])]
assert modified==['test_round7_native_nullable_creation_state_passes_schema_and_consumer','round9_compile_product'],modified
contracts=[]
for story in ('16.1','16.2','16.3'):
 name='_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/'+story+'.json';assert sha(root/name)==m['sha256'][name];contracts.append(story)
for name,h in m['protected'].items():assert sha(root/name)==h
assert sha(root/'_bmad/scripts/epic16_native_helper/TestConstruction.cs')==m['sha256']['_bmad/scripts/epic16_native_helper/TestConstruction.cs']
assert sha(root/'.gitignore')==m['sha256']['.gitignore']
prior=json.loads((base.parent/'review-iteration-10/verification.json').read_bytes());assert all(sha(root/r['path'])==r['sha256'] for r in prior['artifacts'])
schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());ack=schema['$defs']['AC-16.1-04']['properties']['acknowledgements']['items'];openmaps=[]
def walk(v,p):
 if isinstance(v,dict):
  if v.get('type')=='object' and set(v.get('required',[]))=={'configuration','description','members','name','status','tenantId'}:
   for k in ('configuration','members'):
    x=v['properties'][k];assert not x.get('required') and isinstance(x.get('additionalProperties'),dict),(p,k,x)
    openmaps.append(p+'/'+k)
  for k,c in v.items():walk(c,p+'/'+k)
 elif isinstance(v,list):
  for i,c in enumerate(v):walk(c,p+'/'+str(i))
walk(ack,'acknowledgements/items');assert len(openmaps)==10,len(openmaps)
probes={}
for name in ('parent-native-recheck.json','parent-protocol-recheck.json','parent-P1-P2-recheck.json'):
 p=base/name;record=json.loads(p.read_bytes());assert all(sha(root/path)==h for path,h in record['sources'].items()),name
 probes[name]=dict(sha256=sha(p),controls=len(record['results']),allSourceHashesMatchFrozen=True)
report=dict(scope='Parent structural/source acceptance; final execution evidence still pending.',frozenFilesVerified=len(f['files']),frozenSourceSha256=sha(base/'implementation/final-source.json'),archivedSourceCopiesVerified=True,archiveMapping=mapping,boundSourcesVerified=len(binding['sources']),ownedFiles=len(m['sha256']),changedOwnedFiles=changed,codeAndDocumentationDiff=dict(path=str(diff.relative_to(root)),lines=len(sections),sha256=sha(diff)),unchangedPriorFunctions=len(oldf)-len(modified),changedPriorFunctions=modified,priorFunctionChanges='Only refresh new safe recovery digests for the existing valid nullable fixture; retain exact product fixture source/build bytes. Existing assertions are preserved.',preservedContractBytes=contracts,protectedFilesVerified=list(m['protected']),R10P1TestConstructionUnchanged=True,evidencePackagingUnchanged=True,priorArtifactHashesVerified=len(prior['artifacts']),nativeMapSchemasVerified=openmaps,independentControls=probes,initialAuditDisposition='Initial inline audit assumed the archived .gitignore path; final audit honors its declared byte-preserving archive mapping.')
(base/'parent-source-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('nativeMapSchemasVerified','protectedFilesVerified')},indent=2))
