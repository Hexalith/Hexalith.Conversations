from pathlib import Path
import json,hashlib,subprocess,base64,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14';directory=base/'parent-verification/native-capture-fixture';source=directory/'provenance.json';p=json.loads(source.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest();retained={}
def git(*args):return subprocess.check_output(['git','-C',p['sourceFixture'],*args])
def oid(kind,data):return hashlib.sha1(kind.encode()+b' '+str(len(data)).encode()+b'\0'+data).hexdigest()
for row in p['retained']:
 content=(root/row['path']).read_bytes();assert sha(content)==row['sha256'];assert git('show',p['lifecycleCommit']+':'+row['fixturePath'])==content,row['fixturePath'];retained[row['fixturePath']]=content
assert len(retained)==33 and len(retained)==len(p['retained'])
captures=[(name,json.loads(data)) for name,data in retained.items() if name.endswith('.capture.json')];assert len(captures)==5
products=[];native_paths=set();binary_paths=set();capture_paths=set()
for name,capture in captures:
 assert len(capture['products'])==9;capture_paths.add(name);native_paths.add(capture['native']['path']);binary_paths.add(capture['binary']['path'])
 for binding in [capture['native'],capture['binary'],*capture['products']]:assert sha(retained[binding['path']])==binding['sha256']
 binary_paths.update(r['path'] for r in capture['products']);products.append([{k:r[k] for k in ('assemblyName','owner','sourceRevision','sha256','mvid')} for r in capture['products']])
assert all(v==products[0] for v in products)
assert {r['assemblyName'] for r in products[0]}=={'Hexalith.Conversations.Server','Hexalith.Conversations.Contracts','Hexalith.Tenants.Client','Hexalith.Tenants.Contracts','Hexalith.EventStore.Client','Hexalith.EventStore.Contracts','Hexalith.EventStore.DomainService','Hexalith.EventStore.Server','Hexalith.Commons.TenantAccess'}
record=json.loads(retained['docs/release-evidence/story-16.2-final-record-v2.json']);compiled=record['scenarios'][4]['bindings']['compiledPublicPreservation'];compiled_paths={compiled[k]['path'] for k in ('baseline','candidate')}
for phase in ('baseline','candidate'):assert sha(retained[compiled[phase]['path']])==compiled[phase]['sha256']
expected=binary_paths|capture_paths|compiled_paths;assert {r['path'] for r in record['retainedNativeArtifacts']}==expected
for row in record['retainedNativeArtifacts']:assert sha(retained[row['path']])==row['sha256']
publication=set(p['publicationPaths']);assert expected|native_paths<=publication;assert len(publication)==p['publicationPathCount']==28;assert len(binary_paths|compiled_paths)==p['retainedAssemblyCount']==11
objects=json.loads((directory/p['gitObjects']).read_bytes());raw_objects={}
for object_id,obj in objects['objects'].items():
 raw=base64.b64decode(obj['rawBase64'],validate=True);kind=obj['type'];assert kind in ('commit','tree');assert sha(raw)==obj['sha256'] and oid(kind,raw)==object_id;assert git('cat-file',kind,object_id)==raw;raw_objects[object_id]=(kind,raw)
assert len(raw_objects)==43
for row in objects['blobs']:
 raw=(root/row['path']).read_bytes();assert sha(raw)==row['sha256'] and oid('blob',raw)==row['gitObjectId'];assert row['gitObjectType']=='blob';assert git('cat-file','blob',row['gitObjectId'])==raw;assert retained[row['fixturePath']]==raw
assert len(objects['blobs'])==len(retained)
def tree(object_id,prefix=''):
 kind,raw=raw_objects[object_id];assert kind=='tree';result={};offset=0
 while offset<len(raw):
  end=raw.index(b'\0',offset);mode,name=raw[offset:end].split(b' ',1);child=raw[end+1:end+21].hex();offset=end+21;path=prefix+name.decode()
  if mode==b'40000':result.update(tree(child,path+'/'))
  else:result[path]=(mode.decode(),child)
 assert offset==len(raw)
 return result
trees={}
for key in ('candidate','publicationCommit','lifecycleCommit'):
 kind,raw=raw_objects[p[key]];assert kind=='commit';tree_id=raw.splitlines()[0].removeprefix(b'tree ').decode();trees[key]=tree(tree_id)
assert b'parent '+p['candidate'].encode()+b'\n' in raw_objects[p['publicationCommit']][1]
assert b'parent '+p['publicationCommit'].encode()+b'\n' in raw_objects[p['lifecycleCommit']][1]
changed={path for path in set(trees['candidate'])|set(trees['publicationCommit']) if trees['candidate'].get(path)!=trees['publicationCommit'].get(path)};assert changed==publication
lifecycle_changed={path for path in set(trees['publicationCommit'])|set(trees['lifecycleCommit']) if trees['publicationCommit'].get(path)!=trees['lifecycleCommit'].get(path)};assert lifecycle_changed=={'_bmad-output/implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md'}
for row in objects['blobs']:assert trees['lifecycleCommit'][row['fixturePath']][1]==row['gitObjectId']
case=next(n for n in ET.parse(base/'parent-verification/combined/result.xml').iter('testcase') if n.attrib['name']==p['test']);assert not any(c.tag in ('failure','error','skipped') for c in case)
frozen=json.loads((base/'parent-verification/combined/source.json').read_bytes())['files'];assert sha((root/p['testSource']['path']).read_bytes())==p['testSource']['sha256']==frozen[p['testSource']['path']]
assert json.loads((base/'parent-verification/combined/source.json').read_bytes())['files']==frozen
report=dict(source=str(source.relative_to(root)),sourceSha256=sha(source.read_bytes()),retainedArtifacts=33,publicationPaths=28,nativeAssemblies=11,captures=5,rawTrx=5,loadedProductsPerCapture=9,allProductInventoriesEqual=True,exactCommitAndTreeObjects=43,exactMappedBlobs=33,rawSha256AndGitObjectIdsRecomputed=True,allObjectsMatchActualDisposableGit=True,publicationAndLifecycleDiffDerivedFromRetainedRawTrees=True,completedCombinedTestVerified=True,scope=p['scope'])
(base/'parent-native-retention-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
