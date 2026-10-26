from pathlib import Path
import json,hashlib,re,sys
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_schedules as schedules
sha=lambda p:hashlib.sha256((root/p).read_bytes()).hexdigest()
p=root/'_bmad/scripts/epic16_tooling.py';s=p.read_text();needle="    need(contract['nativeProtocol']['conformanceControls'] == measurements.faults.CONTROL_POLICY, 'CONTRACT_DRIFT', path)"
if "contract['nativeProtocol']['tenantAccessControl']" not in s:s=s.replace(needle,needle+"\n    need(contract['nativeProtocol']['tenantAccessControl'] == measurements.schedules.TENANT_ACCESS_CONTROL, 'CONTRACT_DRIFT', 'independent tenant freshness test control')");p.write_text(s)
contracts={}
for story in ['16.1','16.2','16.3']:
 path=f'_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/{story}.json';p=root/path;v=json.loads(p.read_bytes());v['nativeProtocol']['tenantAccessControl']=schedules.TENANT_ACCESS_CONTROL;p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n');contracts[path]=sha(path)
p=root/'_bmad/scripts/epic16_trust.py';s=p.read_text();s=re.sub(r"SCHEMA_PIN = '[0-9a-f]+'","SCHEMA_PIN = '"+sha('_bmad/schemas/epic-16-tooling-v1.schema.json')+"'",s);s=re.sub(r'^CONTRACT_PINS = .*$', 'CONTRACT_PINS = '+repr(contracts),s,flags=re.M);p.write_text(s)
import epic16_tooling as t
p=root/t.BINDING;v=json.loads(p.read_bytes());v['sources']=[dict(path=x,sha256=sha(x)) for x in sorted((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS))];p.write_bytes(t.json_bytes(v));print('Bound',len(v['sources']),'sources, entry',v['entry'])
