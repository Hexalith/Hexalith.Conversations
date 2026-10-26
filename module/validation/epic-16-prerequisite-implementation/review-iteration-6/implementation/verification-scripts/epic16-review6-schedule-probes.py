import sys,json,pathlib,copy
sys.path.insert(0,str(pathlib.Path.cwd()/'_bmad/scripts'))
import jsonschema,epic16_measurements as m
v=json.loads(pathlib.Path('_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes());schema=json.loads(pathlib.Path('_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes());rows=[]
def check(name,s,d,expected):
 row={'case':name,'scenario':s,'expected':expected}
 try:
  jsonschema.Draft202012Validator(schema['$defs'][s]).validate(d);m.evaluate(s,d);row['result']='accepted'
 except (ValueError,TypeError,KeyError,jsonschema.ValidationError) as e:row['result']='rejected';row['reason']=str(e).splitlines()[0]
 row['matched']=row['result']==expected;rows.append(row)
for s,d in v.items():check('unchanged positive',s,d,'accepted')
cases=[
('AC-16.1-03','duplicate removes pending',lambda d:d['futureSchedules'][0]['after'].update(pending=[])),
('AC-16.1-03','conflict leaves gap healthy',lambda d:d['futureSchedules'][1]['after'].update(health='Gapped')),
('AC-16.1-03','split corruption commit',lambda d:d['futureSchedules'][1]['cas'].update(healthCommitId='different-commit')),
('AC-16.1-02','foreign consumer',lambda d:d['healthyRestarts'][0]['consumerBefore'].update(tenantId='tenant-b')),
('AC-16.1-02','foreign store key',lambda d:d['healthyRestarts'][1]['lookupAfter'].update(logicalKey='tenant-b')),
('AC-16.3-02','unexecuted negative',lambda d:d['negativePreflight'].update(executed=False)),
('AC-16.3-02','owned negative occupancy',lambda d:d['negativePreflight']['occupancy'][1].update(owner='occupied-owned')),
('AC-16.3-02','negative emits collision',lambda d:d['negativePreflight'].update(diagnostics=d['diagnostics'])),
('AC-16.3-04','stale worker writes',lambda d:d['concurrentRecovery']['staleAttempts'][0].update(succeeded=True)),
('AC-16.3-04','stale worker clears',lambda d:d['concurrentRecovery']['staleAttempts'][1].update(cleared=[d['persistedPendingId']])),
('AC-16.3-04','stale worker regresses checkpoint',lambda d:d['concurrentRecovery']['staleAttempts'][2]['storedAfter'].update(checkpoint=d['beforeCheckpoint'])),
('AC-16.3-04','current split commit',lambda d:d['concurrentRecovery']['current'].update(watermarkCommitId='split')),
('AC-16.3-04','duplicate mutates persisted version',lambda d:d['concurrentRecovery']['duplicateRetry']['after'].update(version=99)),
('AC-16.3-04','foreign global-position event',lambda d:d['concurrentRecovery']['authoritativePositions'][2].update(eventSha256='0'*64)),
]
for s,name,mutate in cases:
 d=copy.deepcopy(v[s]);mutate(d);check(name,s,d,'rejected')
out=pathlib.Path('module/validation/epic-16-prerequisite-implementation/review-iteration-6/parent-schedule-probes.json');out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'cases':len(rows),'matched':sum(r['matched'] for r in rows),'unexpected':[r for r in rows if not r['matched']]},indent=2))
