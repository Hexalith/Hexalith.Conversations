from pathlib import Path
import sys,json,hashlib,tempfile,subprocess,shutil,os
from copy import deepcopy
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path[:0]=[str(root/'_bmad/scripts'),str(root/'_bmad/scripts/tests')]
import epic16_tooling as t
import test_epic16_tooling as f
import jsonschema
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10';out.mkdir(exist_ok=True)
work=Path(tempfile.mkdtemp(prefix='epic16-r10-parent-'));rows=[]
def record(name,**values):rows.append(dict(finding=name,**values));(out/'parent-counterexamples.json').write_text(json.dumps(dict(workspace=str(work),rows=rows),indent=2)+'\n')
def command(argv,cwd,label):
 r=subprocess.run(argv,cwd=cwd,capture_output=True,timeout=120)
 (cwd/(label+'.stdout')).write_bytes(r.stdout);(cwd/(label+'.stderr')).write_bytes(r.stderr)
 (cwd/(label+'.command.json')).write_text(json.dumps(dict(argv=argv,exitCode=r.returncode),indent=2)+'\n')
 return r
p=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/native-capture-fixture/artifacts__epic16-runtime__16.2__AC-16.2-02.trx.bytes'
raw=p.read_bytes();payload=json.loads(t.records.parse_trx(raw)['results'][0]['output'])
def parse(raw):return t.parse_native(raw,payload['scenarioId'],payload['candidate'],payload['sources'],payload['contractSha256'])
parse(raw)
for kind in ('TestMethod','Execution','both'):
 tree=ET.fromstring(raw);definition=tree.find('./{*}TestDefinitions/{*}UnitTest')
 if kind in ('TestMethod','both'):
  node=deepcopy(definition.find('./{*}TestMethod'));node.set('className','Wrong.Class');node.set('name','WrongMethod');definition.append(node)
 if kind in ('Execution','both'):
  node=deepcopy(definition.find('./{*}Execution'));node.set('id','conflicting-execution');definition.append(node)
 changed=ET.tostring(tree);(out/('conflicting-'+kind+'.trx')).write_bytes(changed)
 _,counts=parse(changed);record('R10B5',mutation=kind,accepted=True,counts=counts)
directory=work/'import';directory.mkdir();shutil.copy2(root/'_bmad/scripts/generate_story_record.py',directory)
(directory/'epic16_tooling.py').write_text('raise ImportError("demonstrated internal import defect")\n')
r=command([sys.executable,str(directory/'generate_story_record.py'),'--contract',t.contract_path('16.1')],directory,'internal-import')
record('R10B9',exitCode=r.returncode,stdout=json.loads(r.stdout),stderr=r.stderr.decode())
data=deepcopy(f.OBSERVATIONS['AC-16.2-03']);schema=json.loads((root/t.SCHEMA).read_bytes())['$defs']['AC-16.2-03'];jsonschema.validate(data,schema);t.measurements.evaluate('AC-16.2-03',data)
record('R10B8',acceptedControl=True,authorizationRows=[r for r in data['queries'] if r['id'] in ('unauthorized','nonexistent')],returnedResponseCaptured=any('response' in k.lower() or k=='returned' for r in data['queries'] for k in r))
prior=Path('/tmp/epic16-review10-constraint-3rf7ll_p');metas=[]
for phase in ('before','after'):
 saved=json.loads((prior/phase/'metadata.json').read_bytes());meta=t.native_helper.inspect_product((prior/phase/'Probe.Constraint.dll').read_bytes(),saved['sourceRevision']);metas.append(meta)
 assert meta==saved
t.products.additive(*metas,t.need,'nullable constraint')
record('R10E1',accepted=True,equalApi=metas[0]['api']==metas[1]['api'],independentlyReinspected=True,callerEvidence=json.loads((prior/'result.json').read_bytes()))
source='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_1EvidenceTests { [Xunit.Fact] public void Scenario01() {} }'
directory=work/'stamp';binary=f.compile_evidence(directory,'existing.'+'a'*40,source)
try:t.native_helper.inspect_assembly(binary,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01');test_accepted=True
except ValueError:test_accepted=False
product=t.native_helper.inspect_product(binary,'a'*40)
record('R10B4',testInspectorAccepted=test_accepted,productInspectorAccepted=True,informationalVersion=product['informationalVersion'])
directory=work/'constructor';binary=f.compile_evidence(directory,'a'*40,source.replace('{ [Xunit.Fact]','{ public Story16_1EvidenceTests(string unavailable) {} [Xunit.Fact]'))
meta=t.native_helper.inspect_assembly(binary,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')
record('R10B6',inspectorAccepted=True,metadata=meta,scope='Metadata admission only; actual runner dependency resolution is not executed by the consumer.')
# Two identical contract-source snapshots, with an external target removing a public type.
repo=work/'contracts';contracts=repo/'src/Hexalith.Conversations.Contracts';contracts.mkdir(parents=True)
(repo/'global.json').write_bytes((root/'global.json').read_bytes());(repo/'.gitignore').write_text('**/bin/\n**/obj/\n*.stdout\n*.stderr\n*.command.json\n')
(repo/'Directory.Build.targets').write_text('<Project />')
(contracts/'Contracts.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Hexalith.Conversations.Contracts</AssemblyName></PropertyGroup></Project>')
(contracts/'Stable.cs').write_text('namespace Hexalith.Conversations.Contracts.Projections; public class ConversationDetailProjectionV1 {}')
(contracts/'Removed.cs').write_text('namespace Hexalith.Conversations.Contracts; public class PreviouslyPublic { public string Existing(string x) => x; }')
os.environ['EPIC16_COMMITLINT_EVIDENCE']=str(out/'parent-commitlint.json')
f.git(repo,'init','-q');f.git(repo,'config','user.name','Disposable Fixture');f.git(repo,'config','user.email','fixture@example.invalid');f.git(repo,'add','.');f.git(repo,'commit','-qm','test: probe contract build baseline');before=f.git(repo,'rev-parse','HEAD')
build=['dotnet','build','src/Hexalith.Conversations.Contracts/Contracts.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']
assert command(build+['-p:SourceRevisionId='+before],repo,'before').returncode==0
before_bytes=(contracts/'bin/Debug/net10.0/Hexalith.Conversations.Contracts.dll').read_bytes();before_meta=t.native_helper.inspect_product(before_bytes,before)
(repo/'Directory.Build.targets').write_text('<Project><ItemGroup><Compile Remove="Removed.cs" /></ItemGroup></Project>')
f.git(repo,'add','Directory.Build.targets');f.git(repo,'commit','-qm','test: probe contract build removal');after=f.git(repo,'rev-parse','HEAD')
assert command(build+['-p:SourceRevisionId='+after],repo,'after').returncode==0
after_bytes=(contracts/'bin/Debug/net10.0/Hexalith.Conversations.Contracts.dll').read_bytes();after_meta=t.native_helper.inspect_product(after_bytes,after)
manifest=[dict(path=str(p.relative_to(repo)),sha256=t.digest(p.read_bytes())) for p in sorted(contracts.glob('*')) if p.is_file()]
data=dict(contractSourcesBefore=manifest,contractSourcesAfter=manifest,beforeContractSha256=t.digest(t.canonical(manifest)),afterContractSha256=t.digest(t.canonical(manifest)))
with t.ContainedReader(repo) as reader:proof=t.verify_public_surface(repo,reader,dict(parent=before,commit=after),'tooling-fixture',data,{})
(repo/'before.dll.bytes').write_bytes(before_bytes);(repo/'after.dll.bytes').write_bytes(after_bytes)
record('R10B7',sourcePreservationAccepted=True,proof=proof,beforeTypes=before_meta['declaredTypes'],afterTypes=after_meta['declaredTypes'],compiledPublicApiChanged=before_meta['api']!=after_meta['api'])
print(json.dumps(dict(workspace=str(work),findings=[r['finding'] for r in rows]),indent=2))
