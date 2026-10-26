from pathlib import Path
from copy import deepcopy
import hashlib,json,sys,subprocess,tempfile,shutil
sys.dont_write_bytecode=True
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'))
import jsonschema,epic16_measurements as measurements,epic16_tooling as tooling
from evidence_boundary_current import digest
fixture=root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json';schema_path=root/'_bmad/schemas/epic-16-tooling-v1.schema.json'
paths=[root/p for p in tooling.TOOL_SOURCES]+[fixture,schema_path]
def hashes():return {str(p.relative_to(root)):digest(p.read_bytes()) for p in paths}
before=hashes();values=json.loads(fixture.read_bytes());schema=json.loads(schema_path.read_bytes());rows=[]
Validator=jsonschema.validators.extend(jsonschema.Draft202012Validator,type_checker=jsonschema.Draft202012Validator.TYPE_CHECKER.redefine('integer',lambda checker,value:type(value)is int))
def check(name,scenario,data,expected):
 if 'compiledContracts' in data:
  data['compiledContracts']={phase:dict(path='repository-dependent',sha256='repository-dependent',revision='repository-dependent') for phase in ('baseline','candidate')}
 row=dict(name=name,scenario=scenario,expectedAccepted=expected)
 try:Validator(schema['$defs'][scenario]).validate(data);row['schema']='PASS'
 except jsonschema.ValidationError as error:row.update(schema='FAIL',schemaReason=error.message)
 try:row['checks']=measurements.evaluate(scenario,data);row['semantics']='PASS'
 except (ValueError,TypeError,KeyError,IndexError) as error:row.update(semantics='FAIL',semanticReason=str(error))
 row['accepted']=row['schema']==row['semantics']=='PASS';row['correct']=row['accepted']==expected;rows.append(row)
def case(name,scenario,mutate,expected=False):
 data=deepcopy(values[scenario]);mutate(data);check(name,scenario,data,expected)
for scenario,data in values.items():
 if scenario!='AC-16.1-05':check('retained pure observation positive',scenario,deepcopy(data),True)
for index in range(3):
 case('rejected operation actually executes '+str(index),'AC-16.2-02',lambda d,i=index:d['rejectedOperations'][i].update(executed=False))
 case('rejected operation completes '+str(index),'AC-16.2-02',lambda d,i=index:d['rejectedOperations'][i].update(completed=False))
 def alter_after(d,i=index):
  row=d['rejectedOperations'][i];row['afterBytes']+=' ';row['afterSha256']=digest(row['afterBytes'].encode())
 case('rejected operation preserves exact state '+str(index),'AC-16.2-02',alter_after)
 if index<2:case('invalid input cannot be replaced with valid replay '+str(index),'AC-16.2-02',lambda d,i=index:d['rejectedOperations'][i].update(attemptedJson=d['rebuilds'][0]['canonicalJson']))
 else:case('generation rejection requires earlier generation','AC-16.2-02',lambda d:d['rejectedOperations'][2].update(generatedTicks=json.loads(d['rebuilds'][0]['canonicalJson'])['anchorUtcTicks']))
for field in ('lifecycle','providerState'):
 def unknown(d,field=field):
  row=next(q for q in d['queries'] if q['id']=='v1')
  row[field]='UnsupportedValue'
 case('known query domain '+field,'AC-16.2-03',unknown)
def dispatch_fault(d,mode):
 proof=d['generationQueries'];snapshots=[proof['baseline']]+[q['snapshot'] for q in proof['queries']]
 if mode=='missing':
  for snapshot in snapshots:snapshot['components']=[p for p in snapshot['components'] if p['kind']!='dispatch']
  for q in proof['queries']:q['reads']=[p for p in q['reads'] if p['kind']!='dispatch']
 else:
  for snapshot in snapshots:
   for part in snapshot['components']:
    if part['kind']=='dispatch':
     if mode=='foreign':part['tenant']='foreign-tenant'
     else:part['value']=['unindexed-pending']
for mode in ('missing','foreign','pending'):case('cross-key dispatch '+mode,'AC-16.2-05',lambda d,mode=mode:dispatch_fault(d,mode))
for index in range(5):case('wire operation executes '+str(index),'AC-16.3-04',lambda d,i=index:d['wireObservation']['operations'][i].update(executed=False))
for status in (2,3,4):case('HTTP 200 rejects non-completing outcome '+str(status),'AC-16.3-04',lambda d,status=status:d['wireObservation']['operations'][-1]['body']['outcomes'][0].update(status=status))
case('AlreadyCompleted response remains valid','AC-16.3-04',lambda d:d['wireObservation']['operations'][-1]['body']['outcomes'][0].update(status=1),True)
case('wire cannot contain projection content','AC-16.3-04',lambda d:d['wireObservation']['operations'][-2].update(persistedAfter=d['concurrentRecovery']['current']['after']))
case('wire response binds actual transaction','AC-16.3-04',lambda d:d['wireObservation']['operations'][-1]['instrumentation'].update(transactionId='unrelated'))
case('wire response binds state digest','AC-16.3-04',lambda d:d['wireObservation']['operations'][-2].update(persistedAfterSha256='f'*64))
case('wire authorization precedes dispatch','AC-16.3-04',lambda d:d['wireObservation']['operations'][1].update(order=99))
case('wire request binds actual aggregate','AC-16.3-04',lambda d:d['wireObservation']['operations'][0]['requestScope'].update(aggregateId='unrelated'))
for scenario in ('AC-16.3-01','AC-16.3-05'):
 def advice(d,key,value):
  diagnostic=next(x for x in d['diagnostics'] if x['code']=='APPHOST_ENDPOINT_NOT_READY');diagnostic['retryAdvice'][key]=value
 for key,value in [('automaticRetriesRemaining',99),('attemptLimit',999),('token','canary')]:case('bounded safe advice '+key,scenario,lambda d,key=key,value=value:advice(d,key,value))
for case_name in ('unauthorized','nonexistent'):
 for field,value in [('schemaVersion',True),('safeNextAction','cached content'),('freshnessState','Current'),('reasonCode','current')]:
  def alter_auth(d,case_name=case_name,field=field,value=value):
   row=next(r for r in d['authorizationQueries'] if r['id']==case_name);row['response']['result'][field]=value
  case('authorization '+case_name+' '+field,'AC-16.2-03',alter_auth)
 for path,value in [(('request','query','callerPrincipalId'),' '),(('request','query','correlationId'),' '),(('request','query','schemaVersion'),1.0),(('request','order'),True),(('response','executed'),False),(('response','order'),1),(('response','tenant'),'foreign'),(('response','correlationId'),'foreign')]:
  def alter_auth_path(d,case_name=case_name,path=path,value=value):
   current=next(r for r in d['authorizationQueries'] if r['id']==case_name)
   for k in path[:-1]:current=current[k]
   current[path[-1]]=value
  case('authorization '+case_name+' '+'.'.join(path),'AC-16.2-03',alter_auth_path)
for field,value in [('safeNextAction','same cached content'),('freshnessState','Current'),('reasonCode','current')]:
 def both(d,field=field,value=value):
  for row in d['authorizationQueries']:row['response']['result'][field]=value
 case('identical unauthorized replies still require native hidden '+field,'AC-16.2-03',both)
def duplicate_correlation(d):
 first,second=d['authorizationQueries'];second['request']['query']['correlationId']=first['request']['query']['correlationId'];second['response']['correlationId']=first['response']['correlationId']
case('authorization experiment correlation uniqueness','AC-16.2-03',duplicate_correlation)
partial=[]
with tempfile.TemporaryDirectory(prefix='epic16-review10-parent-imports-') as folder:
 directory=Path(folder)
 for missing in ('epic16_capture.py','epic16_products.py','epic16_execution.py'):
  target=directory/missing;target.mkdir()
  for name in (root/'_bmad/scripts').glob('*.py'):shutil.copyfile(name,target/name.name)
  (target/missing).unlink()
  for story in ('16_1','16_2','16_3'):
   command=[sys.executable,str(target/f'verify_story_{story}.py'),'--repository',str(target)]
   result=subprocess.run(command,capture_output=True,timeout=30)
   document=json.loads(result.stdout)
   partial.append(dict(missing=missing,story=story,exitCode=result.returncode,result=document['result'],blockers=document['blockers'],nonemptyLedger=bool(document['assertionLedger']),traceback=b'Traceback' in result.stderr))
assert hashes()==before,'source changed during parent probes'
assert all(r['exitCode']==2 and r['result']=='BLOCKED' and r['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE'] and r['nonemptyLedger'] and not r['traceback'] for r in partial)
report=dict(command='uv run --frozen --no-sync python3 /tmp/epic16-review10-parent-final.py',sourceHashes=before,sourceStable=True,scope='Independent pure schema/semantic cases and subprocess import boundaries. Repository-dependent warning/API/native binding audited against full executed fixtures separately. compiledContracts placeholders in pure semantic cases are not API provenance evidence.',results=rows,partialImports=partial,passed=sum(r['correct'] for r in rows),total=len(rows))
(root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-final-probes.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(passed=report['passed'],total=report['total'],partialImports=len(partial),unexpected=[r for r in rows if not r['correct']]),indent=2));raise SystemExit(0 if all(r['correct'] for r in rows) else 1)
