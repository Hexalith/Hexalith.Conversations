from pathlib import Path
import hashlib,json,re,sys
root=Path.cwd();sha=lambda p:hashlib.sha256((root/p).read_bytes()).hexdigest()
p=root/'_bmad/scripts/epic16_trust.py';s=p.read_text();contracts={f'_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/{story}.json':sha(f'_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/{story}.json') for story in ('16.1','16.2','16.3')};s=re.sub(r'^CONTRACT_PINS = .*$', 'CONTRACT_PINS = '+repr(contracts),s,flags=re.M);s=re.sub(r'^SCHEMA_PIN = .*$', 'SCHEMA_PIN = '+repr(sha('_bmad/schemas/epic-16-tooling-v1.schema.json')),s,flags=re.M);p.write_text(s)
sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
p=root/t.BINDING;b=json.loads(p.read_bytes());b['sources']=[dict(path=p,sha256=sha(p)) for p in sorted((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS))];p.write_bytes(t.json_bytes(b))
print('Refreshed',len(b['sources']),'bound inputs')
