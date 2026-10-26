from pathlib import Path
import sys,json,shutil
from copy import deepcopy
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path[:0]=[str(root/'_bmad/scripts'),str(root/'_bmad/scripts/tests')]
import epic16_tooling as t
import test_epic16_tooling as f
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10';report=out/'parent-counterexamples.json';data=json.loads(report.read_text());work=Path(data['workspace']);repo=work/'correlation'
provenance=json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/native-capture-fixture/provenance.json').read_text())
if not repo.exists():shutil.copytree(provenance['sourceFixture'],repo)
candidate=provenance['candidate'];story='16.2';contract_path=t.contract_path(story)
t.CONTRACT_PINS=dict(t.CONTRACT_PINS,**{contract_path:t.digest((repo/contract_path).read_bytes())})
t.TRUSTED_ENTRIES={'fixture-entry.json':t.digest((repo/'fixture-entry.json').read_bytes())}
control=t.generate(repo,story,candidate,publication=True)[0];assert control['result']=='PASS',control
scenario='AC-16.2-05';path=repo/t.native_path('runtime',scenario);tree=ET.fromstring(path.read_bytes());node=next(x for x in tree.find('./{*}Results/{*}UnitTestResult/{*}Output').iter() if x.text and x.text.lstrip().startswith('{'));payload=json.loads(node.text)
capture_path=repo/t.capture.capture_path(story,scenario);capture=json.loads(capture_path.read_bytes())
changed=[]
for index in (0,2):
 product=t.products.PRODUCTS[index];old=payload['loadedProducts'][index];source=f.round9_product_source(product).replace('public string Existing(string value) => value;', 'public int Changed() => 42;')
 binary=f.round9_compile_product(product,old['sourceRevision'],source);meta=t.native_helper.inspect_product(binary,old['sourceRevision']);sha=t.digest(binary)
 assert sha!=old['sha256'];target=t.capture.binary_path(story,sha);(repo/target).write_bytes(binary)
 changed.append(dict(assemblyName=product['assemblyName'],before=old['sha256'],after=sha,sourceRevision=old['sourceRevision']))
 old.update(sha256=sha,mvid=meta['mvid']);capture['products'][index].update(path=target,sha256=sha,mvid=meta['mvid'])
node.text=json.dumps(payload);raw=ET.tostring(tree);path.write_bytes(raw);capture['native']['sha256']=t.digest(raw);capture_path.write_bytes(t.json_bytes(capture))
for n in range(1,6):
 ac=f'AC-{story}-0{n}';row=t.evaluate(repo,story,ac,candidate,'runtime',publication=True);assert row['result']=='PASS',row
 (repo/t.receipt_path('runtime',ac)).write_bytes(t.json_bytes(row))
result=t.generate(repo,story,candidate,publication=True)[0];assert result['result']=='PASS',result
(out/'mixed-products-final.json').write_bytes(t.json_bytes(result))
data['rows'].append(dict(finding='R10B2',control='PASS',mixedFinal='PASS',changed=changed,retainedAssemblies=len([r for r in result['retainedNativeArtifacts'] if r['path'].endswith('.dll')])))
observations={};f.round9_api_observations(repo,observations,scope='runtime');contract=json.loads((repo/contract_path).read_bytes())
with t.ContainedReader(repo) as reader:
 binary=t.native_binary(reader,raw,candidate,scenario,retained=True,facts=result['candidate'])
 comparison=t.products.upstream(repo,reader,result['candidate'],contract['nativeProtocol']['publicSourceBaseline'],observations,t.need)
name='Hexalith.Tenants.Client';loaded=next(r for r in binary['products'] if r['assemblyName']==name);compared=next(r['candidate'] for r in comparison if r['assemblyName']==name)
assert loaded['sha256']!=compared['sha256'] and loaded['api']!=compared['metadata']['api']
data['rows'].append(dict(finding='R10B1',loadedAccepted=True,upstreamPreservationAccepted=True,assembly=name,loadedSha256=loaded['sha256'],comparedSha256=compared['sha256'],apiDifferent=True,sourceRevision=loaded['sourceRevision']))
(out/'uncorrelated-upstream.json').write_bytes(t.json_bytes(dict(loaded=loaded,compared=compared)))
report.write_text(json.dumps(data,indent=2)+'\n');print(json.dumps(data['rows'][-2:],indent=2))
