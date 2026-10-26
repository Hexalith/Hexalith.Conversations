from pathlib import Path
from copy import deepcopy as copy
import sys,json,hashlib,re,subprocess
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'))
import epic16_domain as domain,epic16_relationships as relationships,epic16_schedules as schedules
from evidence_boundary_current import digest,canonical
p=root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json';o=json.loads(p.read_bytes())
def op(identity,scope,order,**extra):return dict(operationId=identity,scope=copy(scope),executed=True,completed=True,order=order,completedOrder=order+1,**extra)
data=o['AC-16.1-03'];checkpoint=data['checkpoint'];horizon=data['recovery']['horizon'];tenant=data['startingState']['tenantId']
for row in data['reverseSaves']:
 name=row['id'];final,history=domain.tenant_replay(data['startingState'],row['authoritativeReplay'],checkpoint,horizon)
 scope=dict(tenant=tenant,storeId='shared-tenant-store',recordId=tenant)
 baseline=dict(tenant=tenant,checkpoint=checkpoint,projection=copy(data['startingState']),version=20,etag='etag-before-'+name,health='Healthy',pending=[],horizon=checkpoint)
 saves=[];persisted=copy(baseline)
 for index,label in enumerate(['newer','older']):
  replica='replica-b' if label=='newer' else 'replica-a';process='process-'+replica;request=name+'-save-'+label;ls=dict(scope,replicaId=replica,processId=process,requestId=request)
  seq=horizon if label=='newer' else checkpoint+1
  proposed=dict(copy(baseline),checkpoint=seq,horizon=seq,projection=copy(final if label=='newer' else history[seq]),version=21,etag='etag-'+name+'-'+label)
  save=op(request+'-operation',ls,10+index*10,id=label,replicaId=replica,processId=process,requestId=request,casOrder=12+index*10,casExecuted=True,expected=dict(version=20,etag=baseline['etag']),proposed=proposed,storedBefore=copy(persisted),storedAfter=copy(proposed if label=='newer' else persisted),succeeded=label=='newer',transactionId='tx-'+name+'-newer' if label=='newer' else None)
  save['completedOrder']=15+index*10;saves.append(save);persisted=copy(save['storedAfter'])
 consumers=[]
 for index,save in enumerate(saves):
  request=name+'-read-'+save['replicaId'];ls=dict(scope,replicaId=save['replicaId'],processId=save['processId'],requestId=request);start=30+index*10
  read=op(request+'-consumer',ls,start,replicaId=save['replicaId'],processId=save['processId'],requestId=request,principal='actor-a',writes=[],recoveryOperations=[],storeRead=op(request+'-store',ls,start+2,method='ITenantProjectionStore.GetAsync',logicalKey=tenant,observedTransactionId=saves[0]['transactionId'],snapshot=copy(persisted)),response=op(request+'-response',ls,start+4,decision=copy(row['newer'])))
  read['completedOrder']=start+6;consumers.append(read)
 row['execution']=dict(captureVersion=1,scheduleId='reverse-save-'+name,scope=scope,saves=saves,consumers=consumers)
base=o['AC-16.1-01']['transitions'][0]['proof'];source=copy(base['accepted']);source_state=copy(base['before']['projection']);accepted=relationships.tenant_record(source,'tenant-a');projection=domain.tenant_replay(source_state,[source],9,10)[0]
for scenario in ['AC-16.1-01','AC-16.1-02']:
 for row in o[scenario]['authorization']:
  state=row['state'];identity=scenario+'-'+state;tenant='tenant-a';scope=dict(tenant=tenant,storeId='shared-tenant-store',recordId=tenant,processId='tenant-access-process',requestId=identity,principal='actor-a')
  snapshot=dict(tenant=tenant,checkpoint=10,projection=copy(projection),lastAccepted=copy(accepted),health=state,pending=[],horizon=10,etag='auth-etag',freshness=dict(lastAppliedUtcTicks=1000,generatedUtcTicks=1100,lagTicks=100))
  conflict=None;observed=1100
  if state=='Gapped':
   future=copy(base['delivered']);future['sequence']=12;snapshot['pending']=[future];snapshot['horizon']=12
  elif state=='Corrupt':conflict=copy(o['AC-16.1-01']['transitions'][2]['proof']['delivered'])
  elif state=='Stale':observed=1201
  missing=state in ('Unknown','Unavailable');seq=None if missing else 10;row['lastContiguousSequence']=seq
  reason={'Healthy':'None','Unknown':'TenantAccessUnavailable','Stale':'TenantAccessStale','Gapped':'TenantAccessGapDetected','Corrupt':'TenantProjectionPoisoned','Unavailable':'TenantAccessUnavailable'}[state]
  response=dict(isAllowed=state=='Healthy',requirement='Write',tenantId=tenant,callerPrincipalId='actor-a',denialReason=reason,isRetryable=state not in ('Healthy','Corrupt'),projectionVersion=seq,projectionWatermark=str(seq) if seq is not None else None)
  row['capture']=dict(captureVersion=1,control=copy(schedules.TENANT_ACCESS_CONTROL),scope=scope,observedAtUtcTicks=observed,sourceState=None if missing else copy(source_state),sourceRecord=None if missing else copy(source),conflict=conflict,writes=[],recoveryOperations=[],request=op(identity+'-request',scope,1,method='ConversationTenantAccessService.CheckAccessAsync',requirement='Write'),storeRead=op(identity+'-read',scope,3,method='ITenantProjectionStore.GetAsync',logicalKey=tenant,outcome='Unavailable' if state=='Unavailable' else 'Missing' if state=='Unknown' else 'Available',error='ProviderUnavailable' if state=='Unavailable' else None,snapshot=None if missing else snapshot),response=op(identity+'-response',scope,5,observedReadId=identity+'-read',decision=response))
for row in o['AC-16.1-04']['acknowledgements']:
 row['failureEvidence']=None
 if row['id'] not in ('provider-failure','retry-exhaustion'):continue
 name=row['id'];fault_id='provider-unavailable' if name=='provider-failure' else name;fault=next(f for f in o['AC-16.1-04']['faults'] if f['id']==fault_id)
 scope=dict(tenant='tenant-a',storeId='shared-tenant-store',recordId='tenant-a',deliveryId=row['deliveryId'],processId='delivery-process',operationId='processing-'+name)
 before=dict(tenant='tenant-a',checkpoint=10,projection=copy(projection),lastAccepted=copy(accepted),health='Healthy',pending=[],horizon=10,etag='failure-before-'+name)
 delivered=copy(base['delivered']);fingerprint=relationships.tenant_record(delivered,'tenant-a');proposed=dict(copy(before),checkpoint=11,horizon=11,projection=domain.tenant_replay(projection,[delivered],10,11)[0],lastAccepted=fingerprint)
 attempts=[op(name+'-attempt-'+str(n),scope,5+3*(n-1),number=n,method='compare-exchange',expectedEtag=before['etag'],proposed=copy(proposed),provider='Unavailable',result='ProviderUnavailable',succeeded=False,transactionId=None,observedBefore=None,observedAfter=None) for n in range(1,row['attempts']+1)]
 end=attempts[-1]['completedOrder']+2;completion=op(name+'-completion',scope,end,outcome='Unavailable',retryable=True,attemptIds=[a['operationId'] for a in attempts],commits=[])
 row['failureEvidence']=dict(captureVersion=1,scope=scope,faultId=fault_id,faultDigests={key:digest(fault[key].encode()) for key in ('beforeBytes','injectedBytes','afterBytes')},sourceState=copy(source_state),sourceRecord=copy(source),delivered=delivered,beforeRead=op(name+'-before',scope,1,outcome='Available',logicalKey='tenant-a',snapshot=copy(before)),processing=op(name+'-start',scope,3,phase='start',delivery=copy(delivered)),attempts=attempts,completion=completion,acknowledgements=op(name+'-ack-inventory',scope,end+3,processingCompletionId=completion['operationId'],deliveries=[]),restoration=op(name+'-restore',scope,end+6,provider='Available',faultAfterSha256=fault['afterSha256']),afterRead=op(name+'-after',scope,end+9,logicalKey='tenant-a',outcome='Available',snapshot=copy(before)))
p.write_text(json.dumps(o,indent=2,ensure_ascii=False)+'\n')
# Extend the existing expanded schema representation, preserving native open maps.
p=root/'_bmad/schemas/epic-16-tooling-v1.schema.json';s=json.loads(p.read_bytes());defs=s['$defs']
text=dict(type='string',minLength=1);integer=dict(type='integer',minimum=0,maximum=domain.MAX_SEQUENCE);boolean=dict(type='boolean');null=dict(type='null')
def nullable(value):return dict(anyOf=[copy(value),null])
def infer(value):
 if value is None:return copy(null)
 if type(value)is bool:return copy(boolean)
 if type(value)is int:return copy(integer)
 if isinstance(value,str):return dict(type='string')
 if isinstance(value,list):return dict(type='array',items=infer(value[0])) if value else dict(type='array',maxItems=0,items=False)
 if set(value)==set(domain.TENANT_STATE_FIELDS):return copy(defs['AC-16.1-03']['properties']['startingState'])
 return dict(type='object',required=list(value),additionalProperties=False,properties={k:infer(v) for k,v in value.items()})
def extend(schema,props):schema['properties'].update(copy(props));schema['required']+=list(props)
def walk(schema):
 if isinstance(schema,dict):
  if schema.get('type')=='object':
   props=schema.get('properties',{})
   if 'transactionId' in props:props['transactionId']=nullable(text)
   if set(props)==set(domain.TENANT_STATE_FIELDS):
    props['description']=nullable(dict(type='string'));props['members']=dict(type='object',additionalProperties=dict(type='string',enum=['Unknown','TenantOwner','TenantContributor','TenantReader']));props['configuration']=dict(type='object',additionalProperties=dict(type='string'))
  for v in schema.values():walk(v)
 elif isinstance(schema,list):
  for v in schema:walk(v)
reverse=infer(o['AC-16.1-03']['reverseSaves'][0]['execution']);walk(reverse);extend(defs['AC-16.1-03']['properties']['reverseSaves']['items'],dict(execution=reverse))
auth=infer(o['AC-16.1-01']['authorization'][0]['capture']);props=auth['properties'];record_schema=infer(source)
props['sourceState']=nullable(props['sourceState']);props['sourceRecord']=nullable(record_schema);props['conflict']=nullable(record_schema)
read=props['storeRead']['properties'];read['error']=nullable(text);snapshot=read['snapshot'];snapshot['properties']['pending']=dict(type='array',items=copy(record_schema));read['snapshot']=nullable(snapshot)
decision=props['response']['properties']['decision']['properties'];decision['projectionVersion']=nullable(integer);decision['projectionWatermark']=nullable(text);walk(auth)
for scenario in ['AC-16.1-01','AC-16.1-02']:
 row=defs[scenario]['properties']['authorization']['items'];row['properties']['lastContiguousSequence']=nullable(integer);extend(row,dict(capture=auth))
failure=infer(o['AC-16.1-04']['acknowledgements'][4]['failureEvidence']);walk(failure);extend(defs['AC-16.1-04']['properties']['acknowledgements']['items'],dict(failureEvidence=nullable(failure)))
p.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n')
# Attribute the current native service decision semantics without runtime source changes.
p=root/'_bmad/scripts/tests/fixtures/epic16-domain-sources.json';v=json.loads(p.read_bytes());seen={(r['repository'],r['path']) for r in v['sources']}
for repo,path in [('.', 'src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs'),('.', 'src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessDecision.cs'),('.', 'src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantProjectionHealth.cs'),('references/Hexalith.Commons','src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs')]:
 if (repo,path) in seen:continue
 data=(root/repo/path).read_bytes();revision=subprocess.check_output(['git','-C',str(root/repo),'rev-parse','HEAD']).decode().strip();v['sources'].append(dict(path=path,repository=repo,revision=revision,sha256=digest(data),source=data.decode()))
p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n')
