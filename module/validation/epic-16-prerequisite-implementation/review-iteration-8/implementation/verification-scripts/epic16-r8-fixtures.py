from pathlib import Path
from copy import deepcopy as cp
import json,sys
sys.path.insert(0,str(Path.cwd()/'_bmad/scripts'))
import epic16_lifecycle as life
from evidence_boundary_current import canonical,digest
P=Path('_bmad/scripts/tests/fixtures/epic16-observations-v1.json')
o=json.loads(P.read_text())
# Synthetic test-only future-v2 restart schedule.
d=o['AC-16.1-02'];native=d['beforeRestart'];before=dict(captureVersion=2,tenant=d['tenant'],logicalStoreKey=d['tenant'],projection=cp(d['healthyRestarts'][0]['startingState']),freshness=dict(lastAppliedUtcTicks=1000,generatedUtcTicks=1200,lagTicks=200),checkpoint=native['lastContiguousSequence'],expectedSequence=native['expectedSequence'],horizon=native['recoveryThrough'],pending=cp(native['pending']),health='Gapped',allowed=False,lease=d['oldFence'],version=7,etag='restart-old',leaseUntilUtcTicks=2000,worker='worker-old',process=d['processBefore'])
after=cp(before);after.update(lease=d['newFence'],version=8,etag='restart-acquired',leaseUntilUtcTicks=3000,worker='worker-new',process=d['processAfter'])
fence=lambda s:{k:s[k] for k in ('lease','version','etag','checkpoint','horizon')}
acq=dict(executed=True,executor='EventStore',before=cp(before),after=cp(after),expected=fence(before),succeeded=True,transactionId='tx-restart-acquire',leaseTransactionId='tx-restart-acquire',recordTransactionId='tx-restart-acquire',order=10,utcTicks=2100)
resume=dict(executed=True,order=20,operation='resume-recovery',executor='EventStore',process=after['process'],worker=after['worker'],tenant=d['tenant'],logicalStoreKey=d['tenant'],observedTransactionId=acq['transactionId'],storedBefore=cp(after),storedAfter=cp(after),trigger='persisted-pending',sourceDeliveries=[],fromSequence=after['expectedSequence'],throughSequence=after['horizon'],state='Gapped',allowed=False,pending=cp(after['pending']))
proposed=cp(after);proposed.update(version=9,etag='restart-renewed',leaseUntilUtcTicks=3010)
attempts=[]
for name in ('old-worker','lease-only','version-only','checkpoint-only','horizon-only'):
 expected=fence(after)
 if name=='old-worker':expected=fence(before)
 else:expected[name.removesuffix('-only')]-=1
 attempts.append(dict(id=name,expected=expected,executed=True,executor='EventStore',tenant=d['tenant'],logicalStoreKey=d['tenant'],process=before['process'],worker=before['worker'],observedTransactionId=acq['transactionId'],order=30,utcTicks=2200,proposed=cp(proposed),succeeded=False,transactionId=None,cleared=[],storedBefore=cp(after),storedAfter=cp(after)))
renew=dict(executed=True,executor='EventStore',tenant=d['tenant'],logicalStoreKey=d['tenant'],process=after['process'],worker=after['worker'],expected=fence(after),before=cp(after),after=cp(proposed),order=30,utcTicks=2200,succeeded=True,transactionId='tx-renew',leaseTransactionId='tx-renew',recordTransactionId='tx-renew',cleared=[])
d['restartSchedule']=dict(captureVersion=1,scheduleId='controlled-restart',before=before,acquisition=acq,resumption=resume,rejectedAttempts=attempts,currentRenewal=renew)
for q in o['AC-16.2-03']['queries']:
 q['controlMetadata']=dict(zip(('index','lifecycle','manifest','watermark','dispatch'),q['componentGenerations']))

def operations(delivery,record,persisted,transaction,order):
 return dict(captureVersion=1,scheduleId='schedule-'+delivery,commit=dict(operation='commit',executed=True,deliveryId=delivery,recordId=record,transactionId=transaction,order=order,utcTicks=2200,succeeded=True,persisted=cp(persisted)),acknowledgement=dict(operation='acknowledge',executed=True,deliveryId=delivery,recordId=record,transactionId=transaction,order=order+1,utcTicks=2200,observedRecord=cp(persisted)))

def query(snapshot,stage,transaction,order):
 inputs=dict(tenantExists=True,authorized=True,providerState='Available',sourceGap=False)
 reads=[dict(kind='index',key=snapshot['index']['key'],generation=snapshot['index']['generation'])]+[dict(kind=p['kind'],key=p['key'],resource=p['resource'],generation=p['generation']) for p in snapshot['components']]
 return dict(captureVersion=1,stage=stage,requestId='query-'+stage,executed=True,completed=True,order=order,tenant=snapshot['tenant'],observedTransactionId=transaction,snapshot=cp(snapshot),inputs=inputs,returned=life.query_result(snapshot,inputs),reads=reads,writes=[],recoveryCalls=[])

for row in o['AC-16.2-04']['lifecycle']:
 erase=row['id']=='erase'
 if not erase:
  observations=cp(row['streams'][0]['authoritativeEvents'])
  event=cp(o['AC-16.2-02']['authoritativeEvents'][3])
  extra=dict(stream='stream-a',sequence=event['sequence'],eventId=json.loads(event['payload'])['metadata']['eventId'],position=21,event=event,eventSha256=digest(canonical(event)))
  # Deliberate reordered arrivals and identical repeated delivery, plus at-B observation.
  arrivals=[observations[1],extra,observations[2],extra]
  deliveries=[];pending=[]
  for n,obs in enumerate(arrivals):
   ref=life.queue_reference(obs,row['afterGeneration']);identifier=digest(canonical(ref));queue=obs['position']>row['B'];duplicate=ref in pending
   following=life.queue_inventory(pending+([ref] if queue and not duplicate else []));transaction='tx-enqueue-'+str(n) if queue else None;order=20+n*10
   deliveries.append(dict(deliveryId='arrival-'+str(n),streamIdentity=dict(id=obs['stream'],tenant=obs['event']['tenant'],conversation=obs['event']['conversation']),authoritative=obs,reference=ref,referenceId=identifier,enqueue=dict(executed=True,order=order,tenant=row['tenant'],generation=row['afterGeneration'],before=cp(pending),after=cp(following),duplicate=duplicate,transactionId=transaction,succeeded=True if queue else None),acknowledged=queue,operations=operations('arrival-'+str(n),identifier,ref,transaction,order) if queue else None))
   pending=following
  retained=[r for r in pending if r['position']>row['C']]
  row['catchUp']=dict(captureVersion=1,scheduleId='controlled-catch-up',deliveries=deliveries,drain=dict(executed=True,succeeded=True,order=100,transactionId='tx-rebuild-work',before=cp(pending),drained=[r for r in pending if r['position']<=row['C']],after=cp(retained)))
  row['queuedAfterC']=[r['position'] for r in retained]
 names,contents=life.lifecycle_contents(row);stages=[];before=cp(contents[0]);before['index'].update(version=7,etag=row['id']+'-initial')
 for n,(name,value) in enumerate(zip(names[1:],contents[1:])):
  if not erase and name=='work':
   for delivery in row['catchUp']['deliveries']:
    enqueue=delivery['enqueue'];enqueue['storedBefore']=cp(before);enqueue['expected']={k:before['index'][k] for k in life.FENCES}
    following=cp(before);next(p for p in following['components'] if p['kind']=='queue')['value']=cp(enqueue['after'])
    transaction=enqueue['transactionId'];enqueue['indexTransactionId']=transaction
    if transaction:following['index'].update(version=before['index']['version']+1,etag=transaction)
    enqueue['storedAfter']=cp(following);enqueue['componentTransactions']=[dict(kind=p['kind'],resource=p['resource'],transactionId=transaction) for p in following['components']] if transaction else []
    before=following
  after=cp(value);after['index'].update(version=before['index']['version']+1,etag=row['id']+'-'+name)
  expected={k:before['index'][k] for k in life.FENCES};transaction='tx-'+row['id']+'-'+name
  order=(n+1)*100 if erase else [10,100,200][n]
  plan=dict(author=row['planAuthor'],tenant=row['tenant'],expected=cp(expected),indexAfter=cp(after['index']),componentsAfter=cp(after['components']))
  cas=dict(executed=True,executor='EventStore',order=order,expected=cp(expected),succeeded=True,transactionId=transaction,indexTransactionId=transaction,componentTransactions=[dict(kind=p['kind'],resource=p['resource'],transactionId=transaction) for p in after['components']])
  stages.append(dict(id=name,before=cp(before),after=cp(after),plan=plan,cas=cas));before=after
 stale=dict(executed=True,executor='EventStore',plan=cp(stages[0]['plan']),expected=cp(stages[0]['plan']['expected']),storedBefore=cp(before),storedAfter=cp(before),succeeded=False,transactionId=None,writtenComponents=[],order=1000)
 row['atomicity']=dict(captureVersion=1,stages=stages,staleAttempt=stale)
 row['lifecycleCasVersion']=row['indexCasVersion']=before['index']['version']
 row['componentGenerations']=[p['generation'] for p in before['components']]
 if erase:
  for delivery,stage in zip(row['deliveries'],(stages[2],stages[4])):
   delivery['operations']['commit']['transactionId']=delivery['operations']['acknowledgement']['transactionId']=stage['cas']['transactionId']
   delivery['operations']['commit']['order']=stage['cas']['order'];delivery['operations']['acknowledgement']['order']=stage['cas']['order']+1
 row['queries']=[query(stage['after'],stage['id'],stage['cas']['transactionId'],stage['cas']['order']+2) for stage in stages]

base=cp(o['AC-16.2-04']['lifecycle'][1]['atomicity']['stages'][-1]['after']);qs=[]
for n,kind in enumerate(('matching','index','detail','dispatch','internal')):
 snap=cp(base)
 if kind=='index':snap['index']['generation']+=1;snap['index']['metadataGenerations']={k:snap['index']['generation'] for k in snap['index']['metadataGenerations']}
 elif kind in ('detail','dispatch'):next(p for p in snap['components'] if p['kind']==kind)['generation']+=1
 elif kind=='internal':snap['index']['metadataGenerations']['manifest']+=1
 qs.append(query(snap,kind,'tx-controlled-query',20+n))
o['AC-16.2-05']['generationQueries']=dict(captureVersion=1,baseline=base,queries=qs,transactionId='tx-controlled-query',commitOrder=10,endOrder=30,authoritativeEvents=[cp(x['event']) for x in o['AC-16.2-04']['lifecycle'][1]['streams'][0]['authoritativeEvents']])
d=o['AC-16.3-05'];operation='cancel-live-fixture';pre=[dict(id='preexisting-control',kind='port',endpointId='dapr-placement',port=50001,owner='preexisting-process')];owned=[dict(id='owned-http',kind='port',endpointId='conversations-http',port=50000,owner=operation),dict(id='owned-process',kind='process',endpointId=None,port=None,owner=operation)]
ops=[]
for n,phase in enumerate(('start','request','observed','cleanup','completion')):
 ops.append(dict(phase=phase,executed=True,operationId=operation,clockId='monotonic-1',order=n+1,monotonicMs=[0,10,15,25,30][n],resources=cp(pre if n>=3 else pre+owned),cancellationRequested=n>=1,cancellationObserved=n>=2,completed=n==4,outcome='Cancelled' if n==4 else None,released=[r['id'] for r in owned] if n==3 else []))
d['cancellation']=dict(captureVersion=1,operationId=operation,clockId='monotonic-1',preexisting=pre,owned=owned,operations=ops)
P.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n')
print('Updated synthetic fixtures')
