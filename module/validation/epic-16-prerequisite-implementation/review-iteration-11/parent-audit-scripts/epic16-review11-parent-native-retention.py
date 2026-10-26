from pathlib import Path
import json,hashlib,subprocess,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11';source=base/'implementation/native-capture-fixture/provenance.json';p=json.loads(source.read_text());sha=lambda b:hashlib.sha256(b).hexdigest();retained={}
for row in p['retained']:
 content=(root/row['path']).read_bytes();assert sha(content)==row['sha256'];actual=subprocess.check_output(['git','-C',p['sourceFixture'],'show',p['lifecycleCommit']+':'+row['fixturePath']]);assert actual==content,row['fixturePath'];retained[row['fixturePath']]=content
assert len(retained)==len(p['retained'])
captures=[(name,json.loads(data)) for name,data in retained.items() if name.endswith('.capture.json')];assert len(captures)==5
products=[];native_paths=set();binary_paths=set();capture_paths=set()
for name,capture in captures:
 assert len(capture['products'])==7
 capture_paths.add(name);native_paths.add(capture['native']['path']);binary_paths.add(capture['binary']['path'])
 for binding in [capture['native'],capture['binary'],*capture['products']]:assert sha(retained[binding['path']])==binding['sha256']
 binary_paths.update(r['path'] for r in capture['products'])
 products.append([{k:r[k] for k in ('assemblyName','owner','sourceRevision','sha256','mvid')} for r in capture['products']])
assert all(v==products[0] for v in products)
record=json.loads(retained['docs/release-evidence/story-16.2-final-record-v2.json']);compiled=record['scenarios'][4]['bindings']['compiledPublicPreservation'];compiled_paths={compiled[k]['path'] for k in ('baseline','candidate')}
for phase in ('baseline','candidate'):assert sha(retained[compiled[phase]['path']])==compiled[phase]['sha256']
expected_artifacts=binary_paths|capture_paths|compiled_paths
assert {r['path'] for r in record['retainedNativeArtifacts']}==expected_artifacts
for row in record['retainedNativeArtifacts']:assert sha(retained[row['path']])==row['sha256']
publication=set(p['publicationPaths']);assert expected_artifacts|native_paths<=publication
assert len(binary_paths|compiled_paths)==p['retainedAssemblyCount']
assert p['captureCount']==len(captures) and p['rawTrxCount']==len(native_paths)
case=next(x for x in ET.parse(base/'implementation/combined/result.xml').iter('testcase') if x.attrib['name']==p['test']);assert not any(x.tag in ('failure','error','skipped') for x in case)
report=dict(source=str(source.relative_to(root)),sourceSha256=sha(source.read_bytes()),retainedArtifactHashesVerified=len(retained),allRetainedArtifactsMatchActualDisposableLifecycleGitObjects=True,publicationPaths=len(publication),retainedAssemblyCount=len(binary_paths|compiled_paths),compiledRootArtifactsRetained=len(compiled_paths),allFiveOwnedProductInventoriesEqual=True,loadedProductCountPerCapture=7,successfulFinalFreshCheckoutTestVerified=True,scope=p['scope'])
(base/'parent-native-retention-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
