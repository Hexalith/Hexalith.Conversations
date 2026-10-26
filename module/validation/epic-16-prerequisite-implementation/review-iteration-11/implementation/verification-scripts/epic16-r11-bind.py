from pathlib import Path
import json,hashlib,re,sys
root=Path.cwd();sha=lambda p:hashlib.sha256((root/p).read_bytes()).hexdigest()
p=root/'_bmad/scripts/epic16_trust.py';s=p.read_text();s=re.sub(r"SCHEMA_PIN = '[0-9a-f]+'", "SCHEMA_PIN = '"+sha('_bmad/schemas/epic-16-tooling-v1.schema.json')+"'", s);p.write_text(s)
sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
p=root/t.BINDING;value=json.loads(p.read_bytes());value['sources']=[dict(path=x,sha256=sha(x)) for x in sorted((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS))];p.write_bytes(t.json_bytes(value));print('Bound',len(value['sources']),'sources; entry',value['entry'])
