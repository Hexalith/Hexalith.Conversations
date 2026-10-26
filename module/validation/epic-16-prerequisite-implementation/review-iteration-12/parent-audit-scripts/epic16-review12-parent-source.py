from pathlib import Path
import ast,json,hashlib,difflib,re
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12';backup=Path('/tmp/epic16-review12-preservation-mdzqxec0');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
m=json.loads((backup/'manifest.json').read_bytes());f=json.loads((base/'implementation/final-source.json').read_bytes());assert sha(base/'implementation/final-source.json')=='ed4e8a100abdd4ca22f2cc5df225d69da6275ccde4b4e9e4e865c4a1a6c27573'
assert all(sha(root/p)==h for p,h in f['files'].items())
archive=base/'implementation/combined/source';mapping={'.gitignore':'.gitignore.bytes'};assert all(sha(archive/mapping.get(p,p))==h for p,h in f['files'].items())
binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes());assert binding['entry'] is None
for row in binding['sources']:assert sha(root/row['path'])==row['sha256']
sections=[];changed=[]
for name,h in m['sha256'].items():
 if sha(root/name)==h:continue
 changed.append(name)
 if name.endswith(('.py','.cs','.md','.txt')):
  sections.extend(difflib.unified_diff((backup/name).read_text().splitlines(True),(root/name).read_text().splitlines(True),fromfile='before/'+name,tofile='after/'+name))
diff=base/'parent-final-source.diff';diff.write_text(''.join(sections))
p='_bmad/scripts/tests/test_epic16_tooling.py';old=ast.parse((backup/p).read_text());new=ast.parse((root/p).read_text());types=(ast.FunctionDef,ast.AsyncFunctionDef,ast.ClassDef);oldf={n.name:n for n in old.body if isinstance(n,types)};newf={n.name:n for n in new.body if isinstance(n,types)}
modified=[n for n in oldf if ast.dump(oldf[n])!=ast.dump(newf[n])];assert modified==['test_round5_recovered_tenant_description_preserves_native_nullable_domain','test_round8_acknowledgement_native_identity_semantics'],modified
for name in modified:
 node=newf[name]
 if name.startswith('test_round5'):node.body=[n for n in node.body if not(isinstance(n,ast.Expr) and isinstance(n.value,ast.Call) and isinstance(n.value.func,ast.Name) and n.value.func.id=='round12_refresh_reverse_states')]
 else:node.body=[n for n in node.body if not(isinstance(n,ast.If) and "row['failureEvidence']" in ast.unparse(n.test))]
 assert ast.dump(node)==ast.dump(oldf[name]),name
for name,h in m['protected'].items():assert sha(root/name)==h
assert sha(root/'.gitignore')==m['sha256']['.gitignore']
prior=json.loads((base.parent/'review-iteration-11/verification.json').read_bytes());assert all(sha(root/r['path'])==r['sha256'] for r in prior['artifacts'])
contracts=[]
for story in ('16.1','16.2','16.3'):
 name='_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/'+story+'.json';a=json.loads((backup/name).read_bytes());b=json.loads((root/name).read_bytes());control=b['nativeProtocol'].pop('tenantAccessControl');assert control==dict(id='tenant-access-observation-v1',maxAgeTicks=100) and a==b;contracts.append(story)
schema=json.loads((root/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());prior_schema=json.loads((backup/'_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());openmaps=[];unchanged_native_shapes=[]
def previous_at(path):
 value=prior_schema
 try:
  for key in path.lstrip('/').split('/'):
   value=value[int(key)] if isinstance(value,list) else value[key]
 except (KeyError,IndexError,TypeError,ValueError):return None
 return value
def walk(v,p):
 if isinstance(v,dict):
  if v.get('type')=='object' and set(v.get('required',[]))=={'configuration','description','members','name','status','tenantId'}:
   if v==previous_at(p):
    unchanged_native_shapes.append(p);return
   for k in ('configuration','members'):
    x=v['properties'][k];assert not x.get('required') and isinstance(x.get('additionalProperties'),dict),(p,k,x);openmaps.append(p+'/'+k)
  for k,c in v.items():walk(c,p+'/'+k)
 elif isinstance(v,list):
  for i,c in enumerate(v):walk(c,p+'/'+str(i))
walk(schema,'');assert len(openmaps)>=10
actual=(root/'references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs').read_text();helper=(root/'_bmad/scripts/epic16_native_helper/Program.cs').read_text()
def method(text,name):
 pos=text.index('private static bool '+name+'(');start=text.index('{',pos);count=1;end=start+1
 while count:
  count+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[pos:end]
for name in ('TryValidateTenantValue','TryValidateCallerPrincipalId'):assert method(actual,name)==method(helper,name)
license=(root/'references/Hexalith.Commons/LICENSE').read_text().strip();assert license in (root/'_bmad/scripts/tests/fixtures/epic16-domain-sources-LICENSE.txt').read_text();assert 'src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs' in helper
controls=json.loads((base/'parent-final-controls.json').read_bytes());assert controls['count']==20 and not controls['changedDuringRun'];assert all(sha(root/p)==h for p,h in controls['sourceHashes'].items())
report=dict(scope='Parent final structural/source acceptance; combined/legacy execution remains in progress.',frozenFilesVerified=len(f['files']),frozenSourceSha256=sha(base/'implementation/final-source.json'),archivedSourceCopiesVerified=True,archiveLocation=str(archive.relative_to(root)),archiveMapping=mapping,initialAuditDisposition='Expected prior final-source layout before locating the actual combined/source snapshot; every final hash verified against that snapshot. Initial all-schema map audit included unchanged older controlled shapes; corrected scope checks every new/modified native map and explicitly counts preserved prior shapes.',boundSourcesVerified=len(binding['sources']),ownedFiles=len(m['sha256']),changedOwnedFiles=changed,codeDocumentationLicenseDiff=dict(path=str(diff.relative_to(root)),lines=len(sections),sha256=sha(diff)),unchangedPriorFunctions=len(oldf)-len(modified),changedPriorFunctions=modified,priorAssertionsUnchanged=True,priorFunctionChanges='Only correlated newly-required snapshots/identity refreshed; deleting those additions recovers exact previous AST.',contractsOnlyAddFixedNativeTestControl=contracts,protectedFilesVerified=list(m['protected']),evidencePackagingUnchanged=True,priorArtifactHashesVerified=len(prior['artifacts']),newOrChangedNativeMapSchemasVerified=openmaps,unchangedPriorNativeShapeCount=len(unchanged_native_shapes),actualCommonsMethodsByteEquivalent=True,actualCommonsMITNoticeRetained=True,independentFinalControls=dict(count=20,sha256=sha(base/'parent-final-controls.json'),sourceHashesMatchFrozen=True))
(base/'parent-source-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('newOrChangedNativeMapSchemasVerified','protectedFilesVerified','changedOwnedFiles')},indent=2))
