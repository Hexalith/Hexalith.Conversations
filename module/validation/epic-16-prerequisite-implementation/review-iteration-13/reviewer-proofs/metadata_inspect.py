import argparse
import json
from pathlib import Path
import sys
sys.dont_write_bytecode = True
ROOT = Path('/home/administrator/projects/hexalith/conversations')
sys.path.insert(0, str(ROOT / '_bmad/scripts'))
import epic16_native
p = argparse.ArgumentParser()
p.add_argument('operation', choices=['test', 'product'])
p.add_argument('assembly')
p.add_argument('--revision', required=True)
p.add_argument('--type')
p.add_argument('--method')
a = p.parse_args()
b = Path(a.assembly).read_bytes()
if a.operation == 'test':
    result = epic16_native.inspect_assembly(b, a.revision, a.type, a.method)
else:
    result = epic16_native.inspect_product(b, a.revision)
print(json.dumps(result, indent=2, sort_keys=True))
