"""Independent final controls using retained pre-fix inputs and corrected fixtures."""
from pathlib import Path
from copy import deepcopy
import json,hashlib,subprocess,sys,tempfile,shutil,importlib.util
sys.dont_write_bytecode=True;root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11'
sp=importlib.util.spec_from_file_location('reviewtests',root/'_bmad/scripts/tests/test_epic16_tooling.py');t=importlib.util.module_from_spec(sp);sp.loader.exec_module(t)
schema=json.loads((root/t.tooling.SCHEMA).read_bytes());results=[]
def validate(name,data):t.tooling.jsonschema.Draft202012Validator(schema['$defs'][name]).validate(data)
for finding,scenario in [('R11B8','AC-16.1-04'),('R11B9','AC-16.3-04')]:
 old=json.loads((base/'parent-protocol-proof'/(finding+'-accepted-observations.json')).read_bytes())
 try:validate(scenario,old)
 except t.tooling.jsonschema.ValidationError:results.append(dict(id=finding,oldIncompleteCaptureRejectedByClosedSchema=True))
 else:raise AssertionError(finding+' incomplete old capture remains schema-admitted')
 current=deepcopy(t.OBSERVATIONS[scenario]);validate(scenario,current)
 checks=t.tooling.measurements.evaluate(scenario,current);results.append(dict(id=finding,correctedCompleteCaptureAdmitted=True,checks=checks))
data=json.loads((base/'parent-protocol-proof/R11E2-closed-two-resource-capture.json').read_bytes());row=data['lifecycle'][0]
t.round8_refresh(row)
for stage in row['atomicity']['stages']:
 stage['cas']['componentTransactions']=[dict(kind=p['kind'],resource=p['resource'],transactionId=stage['cas']['transactionId']) for p in stage['after']['components']]
validate('AC-16.2-04',data);checks=t.tooling.measurements.evaluate('AC-16.2-04',data)
results.append(dict(id='R11E2',correctedTwoResourceCaptureAdmitted=True,checks=checks,resources=[p['resource'] for p in row['atomicity']['stages'][0]['after']['components'] if p['kind']=='detail']))
for story in ('16_1','16_2','16_3'):
 for kind,stub,code in [('missing',None,2),('typo','import epic16_typo\n',1),('internal-declared','raise ModuleNotFoundError("internal defect", name="epic16_tooling")\n',1)]:
  work=Path(tempfile.mkdtemp(prefix='epic16-r11-final-import-'));wrapper='verify_story_'+story+'.py';shutil.copyfile(root/'_bmad/scripts'/wrapper,work/wrapper)
  if stub is not None:(work/'epic16_tooling.py').write_text(stub)
  run=subprocess.run([sys.executable,str(work/wrapper)],cwd=work,capture_output=True)
  assert run.returncode==code,(story,kind,run.returncode,run.stdout,run.stderr)
  if kind=='missing':assert json.loads(run.stdout)['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE']
  else:assert run.stdout==b'' and b'ModuleNotFoundError' in run.stderr
  results.append(dict(id='R11B6',story=story,control=kind,exitCode=run.returncode,stdout=run.stdout.decode(),stderr=run.stderr.decode(),cwd=str(work)))
sources={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in [t.tooling.SCHEMA,'_bmad/scripts/epic16_measurements.py','_bmad/scripts/epic16_schedules.py','_bmad/scripts/epic16_lifecycle.py','_bmad/scripts/epic16_execution.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','_bmad/scripts/tests/test_epic16_tooling.py',*[f'_bmad/scripts/verify_story_{story}.py' for story in ('16_1','16_2','16_3')]]}
(base/'parent-protocol-recheck.json').write_text(json.dumps(dict(results=results,sources=sources,scope='Synthetic independent protocol controls, no runtime qualification.'),indent=2)+'\n')
print(json.dumps(dict(controls=len(results),allPassed=True),indent=2))
