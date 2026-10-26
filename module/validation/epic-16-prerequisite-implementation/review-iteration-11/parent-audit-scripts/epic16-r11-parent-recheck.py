"""Recheck retained compiled review counterexamples against the corrected helper."""
from pathlib import Path
import hashlib,json,sys,importlib.util
sys.dont_write_bytecode=True
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11'
spec=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
results=[]
for case in sorted((base/'parent-metadata-proof').iterdir()):
 if not case.is_dir():continue
 metadata=[];unsupported=[]
 for phase in ('baseline','candidate'):
  try:metadata.append(t.tooling.native_helper.inspect_product((case/(phase+'.dll.bytes')).read_bytes(),'a'*40));unsupported.append(None)
  except ValueError as error:metadata.append(None);unsupported.append(str(error))
 if any(m is None for m in metadata):
  assert case.name.startswith('R11B3-'),(case.name,unsupported)
  results.append(dict(id=case.name,explicitUnsupported=unsupported));continue
 try:t.tooling.products.additive(*metadata,t.tooling.need,case.name)
 except t.tooling.Stop as error:
  results.append(dict(id=case.name,rejected=True,code=error.code if hasattr(error,'code') else str(error),apiDifferent=metadata[0]['api']!=metadata[1]['api']))
 else:raise AssertionError(case.name+' still accepted')
binary=(base/'parent-protocol-proof/R11E1-Probe.dll.bytes').read_bytes()
try:t.tooling.native_helper.inspect_assembly(binary,'a'*40,'Probe.TestClass','Scenario01')
except ValueError as error:results.append(dict(id='R11E1',rejected=True,diagnostic=str(error)))
else:raise AssertionError('Fact+Theory still admitted')
sources={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [root/'_bmad/scripts/epic16_products.py',root/'_bmad/scripts/epic16_native.py',*sorted((root/'_bmad/scripts/epic16_native_helper').glob('*.cs'))]}
(base/'parent-native-recheck.json').write_text(json.dumps(dict(scope='Retained synthetic pre-fix counterexamples, independently re-inspected without executing evidence assemblies; no runtime authority.',results=results,sources=sources),indent=2)+'\n')
print(json.dumps(results,indent=2))
