from pathlib import Path
from copy import deepcopy as copy
import json,sys,hashlib
sys.path.insert(0,str(Path.cwd()/'_bmad/scripts'))
import epic16_domain as domain
import epic16_relationships as relationships
from evidence_boundary_current import canonical,digest
p=Path('_bmad/scripts/tests/fixtures/epic16-observations-v1.json');o=json.loads(p.read_bytes())
transitions=o['AC-16.1-01']['transitions'];base=transitions[0]['proof'];source=base['accepted'];state=base['before']['projection']
for row in o['AC-16.1-04']['acknowledgements']:
 name=row['id'];row['durableEvidence']=None
 if row['durableCommit'] is None:continue
 before=dict(tenant='tenant-a',checkpoint=10,projection=copy(state),lastAccepted=relationships.tenant_record(source,'tenant-a'),health='Healthy',pending=[],horizon=10,etag='ack-before-'+name)
 delivered=copy(base['delivered'] if name in ('accepted','pending') else transitions[1 if name=='duplicate' else 2]['proof']['delivered'])
 if name=='pending':delivered['sequence']=12
 fingerprint=relationships.tenant_record(delivered,'tenant-a');after=copy(before)
 if name=='accepted':after.update(checkpoint=11,projection=domain.tenant_replay(state,[delivered],10,11)[0],lastAccepted=fingerprint,horizon=11)
 elif name=='pending':after.update(health='Gapped',pending=[fingerprint],horizon=12)
 elif name=='corrupt':after.update(health='Corrupt')
 if name!='duplicate':after['etag']='ack-after-'+name
 tx='durable-transaction-'+name
 row['durableEvidence']=dict(captureVersion=1,tenant='tenant-a',sourceState=copy(state),sourceRecord=copy(source),delivered=delivered,before=before,after=after,expectedEtag=before['etag'],transactionId=tx,writeCount=int(name!='duplicate'))
 row['durableCommit'].update(operation='commit',executed=True,succeeded=True,transactionId=tx,persisted=copy(after))
 row['acknowledgement'].update(operation='acknowledge',executed=True,transactionId=tx,observedRecord=copy(after))
data=o['AC-16.3-04'];proof=data['concurrentRecovery']
for row in [*proof['staleAttempts'],proof['duplicateRetry']]:
 duplicate='id' not in row;name='duplicate-retry' if duplicate else row['id'];identity='recovery-operation-'+name
 before=row['before'] if duplicate else row['storedBefore'];after=row['after'] if duplicate else row['storedAfter']
 expected={k:before[k] for k in ('lease','version','etag','generation','checkpoint','watermark')} if duplicate else row['expected'];proposed=before if duplicate else row['proposed']
 start=data['wireObservation']['operations'][-1]['order']+1 if duplicate else 1
 common=dict(operationId=identity,executed=True,completed=True)
 row['execution']=dict(captureVersion=1,operationId=identity,branchId='current-clearance' if duplicate else name,scope=copy(row['scope']),afterClearanceTransactionId=proof['current']['commitId'] if duplicate else None,wireRequestId=data['wireObservation']['requestId'] if duplicate else None,operations=[
  dict(common,phase='request',order=start,expectedSha256=digest(canonical(expected)),proposedSha256=digest(canonical(proposed))),
  dict(common,phase='attempt',order=start+1,kind='observe-completion' if duplicate else 'compare-exchange',storedBeforeSha256=digest(canonical(before)),succeeded=row['succeeded'],transactionId=row['commitId']),
  dict(common,phase='completion',order=start+2,storedAfterSha256=digest(canonical(after)),cleared=copy(row['cleared']),outcome='AlreadyCompleted' if duplicate else 'FenceRejected')])
p.write_text(json.dumps(o,indent=2,ensure_ascii=False)+'\n')
p=Path('_bmad/schemas/epic-16-tooling-v1.schema.json');s=json.loads(p.read_bytes());defs=s['$defs']
def obj(props):return dict(type='object',required=list(props),additionalProperties=False,properties=props)
text=dict(type='string',minLength=1);integer=dict(type='integer',minimum=0,maximum=9223372036854775807);boolean=dict(type='boolean');null=dict(type='null');sha=dict(type='string',pattern='^[0-9a-f]{64}$')
def nullable(value):return dict(anyOf=[copy(value),null])
def extend(schema, props):schema['properties'].update(copy(props));schema['required']+=list(props)
r=defs['AC-16.1-01']['properties']['transitions']['items']['properties']['proof']['properties']
snapshot=copy(r['before']);extend(snapshot,dict(tenant=text,pending=dict(type='array',items=copy(r['before']['properties']['lastAccepted'])),horizon=integer))
evidence=obj(dict(captureVersion=dict(type='integer',const=1),tenant=text,sourceState=copy(r['before']['properties']['projection']),sourceRecord=copy(r['delivered']),delivered=copy(r['delivered']),before=copy(snapshot),after=copy(snapshot),expectedEtag=text,transactionId=text,writeCount=integer))
ack=defs['AC-16.1-04']['properties']['acknowledgements']['items'];extend(ack,dict(durableEvidence=nullable(evidence)))
commit=ack['properties']['durableCommit']['anyOf'][0];extend(commit,dict(operation=text,executed=boolean,succeeded=boolean,transactionId=text,persisted=copy(snapshot)))
acknowledgement=ack['properties']['acknowledgement']['anyOf'][0];extend(acknowledgement,dict(operation=text,executed=boolean,transactionId=text,observedRecord=copy(snapshot)))
recovery=defs['AC-16.3-04']['properties']['concurrentRecovery']['properties']
common=dict(phase=text,operationId=text,order=integer,executed=boolean,completed=boolean)
phases=[obj(dict(common,phase=dict(const='request'),expectedSha256=sha,proposedSha256=sha)),obj(dict(common,phase=dict(const='attempt'),kind=text,storedBeforeSha256=sha,succeeded=nullable(boolean),transactionId=nullable(text))),obj(dict(common,phase=dict(const='completion'),storedAfterSha256=sha,cleared=dict(type='array',items=text),outcome=text))]
execution=obj(dict(captureVersion=dict(type='integer',const=1),operationId=text,branchId=text,scope=copy(recovery['scope']),afterClearanceTransactionId=nullable(text),wireRequestId=nullable(text),operations=dict(type='array',minItems=3,maxItems=3,prefixItems=phases,items=False)))
for schema in (recovery['staleAttempts']['items'],recovery['duplicateRetry']):extend(schema,dict(execution=execution))
p.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n')
