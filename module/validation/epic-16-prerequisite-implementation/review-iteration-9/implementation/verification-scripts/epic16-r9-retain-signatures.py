from pathlib import Path
import json,hashlib,sys
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation';target=out/'signature-kind-fixtures';target.mkdir(exist_ok=True)
sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
frozen=json.loads((out/'final-source.json').read_bytes())['files']
for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
workspace=Path(json.loads((out/'pytest-workspaces.json').read_bytes())['combined']);fixtures=sorted(p for p in workspace.glob('test_round9_referenced_*') if p.is_dir() and not p.is_symlink());assert len(fixtures)==8,(len(fixtures),fixtures)
files=['global.json','external/External.csproj','external/Value.cs','api/Api.csproj','api/Surface.cs','api/Stamp.cs','build-command.json','build.stdout','build.stderr','metadata.json','api/bin/Debug/net10.0/Probe.Api.dll','api/bin/Debug/net10.0/External.dll'];rows=[]
for fixture in fixtures:
 evidence=[];metadata=[]
 for phase,revision in [('baseline','a'*40),('candidate','b'*40)]:
  folder=fixture/phase
  for name in files:
   raw=(folder/name).read_bytes();p=target/fixture.name/phase/(name+'.bytes');p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);evidence.append(dict(fixturePath=phase+'/'+name,path=str(p.relative_to(root)),sha256=hashlib.sha256(raw).hexdigest()))
  inspected=t.native_helper.inspect_product((folder/'api/bin/Debug/net10.0/Probe.Api.dll').read_bytes(),revision)
  assert inspected==json.loads((folder/'metadata.json').read_bytes());metadata.append(inspected)
 expected='PASS' if (fixture/'baseline/external/Value.cs').read_bytes()==(fixture/'candidate/external/Value.cs').read_bytes() else 'FAIL';result='PASS';blocker=None
 try:t.products.additive(metadata[0],metadata[1],t.need,fixture.name)
 except t.Stop as e:result='FAIL';blocker=e.code
 assert result==expected and (blocker is None if expected=='PASS' else blocker=='UPSTREAM_API_DRIFT'),(fixture,result,blocker)
 rows.append(dict(fixture=str(fixture),expected=result,blocker=blocker,apiDifferent=metadata[0]['api']!=metadata[1]['api'],declarationNamesUnprefixed='Probe.Surface' in metadata[1]['declaredTypes'],artifacts=evidence))
assert sum(row['expected']=='PASS' for row in rows)==2 and sum(row['expected']=='FAIL' for row in rows)==6
(target/'provenance.json').write_text(json.dumps(dict(scope='Exact compiled synthetic class/value signature fixtures from final combined run; no runtime product or owner authority.',argv=['uv','run','--frozen','--no-sync','python3',str(Path(__file__).relative_to(root))],sourceEpochSha256=hashlib.sha256((out/'final-source.json').read_bytes()).hexdigest(),cases=rows),indent=2)+'\n')
for path,value in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==value,path
print('Retained and re-inspected six breaking reference-kind fixtures and two same-kind additive positives, including exact API/dependency binaries and build streams')
