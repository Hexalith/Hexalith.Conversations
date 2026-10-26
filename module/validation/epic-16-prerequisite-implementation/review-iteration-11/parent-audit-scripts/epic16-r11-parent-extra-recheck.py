from pathlib import Path
import json,hashlib,sys,importlib.util
sys.dont_write_bytecode=True;root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11'
sp=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(sp);sp.loader.exec_module(t)
proof=base/'parent-framework-identity-proof';results=[]
try:t.tooling.native_helper.inspect_assembly((proof/'test/bin/Debug/net10.0/Probe.dll').read_bytes(),'a'*40,'Probe.TestClass','Scenario01')
except ValueError:results.append(dict(id='R11P1-external-System-Fact',rejected=True))
else:raise AssertionError('External System.* Fact conflict remains admitted')
metadata=[t.tooling.native_helper.inspect_product((proof/phase/'bin/Debug/net10.0/Api.dll').read_bytes(),'a'*40) for phase in ('before','after')]
assert metadata[0]['api']!=metadata[1]['api']
try:t.tooling.products.additive(*metadata,t.tooling.need,'external System.Reflection contract attribute')
except t.tooling.Stop:results.append(dict(id='R11P1-external-build-name',rejected=True,apiDifferent=True))
else:raise AssertionError('Non-framework attribute remains excluded')
capture=json.loads((base/'parent-domain-map-proof/valid-capture.json').read_bytes());schema=json.loads((root/t.tooling.SCHEMA).read_bytes())['$defs']['AC-16.1-04']
t.tooling.jsonschema.Draft202012Validator(schema).validate(capture);checks=t.tooling.measurements.evaluate('AC-16.1-04',capture)
results.append(dict(id='R11P2-domain-maps',schemaAndSemanticsAdmitted=True,checks=checks))
sources={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in [t.tooling.SCHEMA,'_bmad/scripts/epic16_schedules.py','_bmad/scripts/epic16_measurements.py','_bmad/scripts/epic16_products.py','_bmad/scripts/epic16_native_helper/ProductMetadata.cs','_bmad/scripts/epic16_native_helper/Program.cs']}
(base/'parent-P1-P2-recheck.json').write_text(json.dumps(dict(results=results,sources=sources,scope='Retained independent synthetic counterexamples; no product or runtime qualification.'),indent=2)+'\n')
print(json.dumps(results,indent=2))
