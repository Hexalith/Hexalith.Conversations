"""Bounded, source-attributed relationships for Epic 16 controlled observations.

These functions inspect evidence only. They neither implement storage nor invoke
production handlers. See the retained domain sources and protocol goldens.
"""
import base64
from copy import deepcopy
import json
import struct

import epic16_domain as domain
from epic16_domain import require, shape, text, same
from epic16_native import strict_json
from evidence_boundary_current import canonical, digest


def segment(value):
    return base64.urlsafe_b64encode(text(value).encode('utf-8')).decode('ascii').rstrip('=')


def conversation_key(tenant, resource):
    return 'projection:conversations:' + segment(tenant) + ':' + segment(resource)


def index_key(tenant):
    return 'projection:conversations-index:' + segment(tenant)


def isolation(data):
    """Same resource, two tenants, independently derived keys and both write orders."""
    proof = data['isolation']
    shape(proof, ('tenants','resource','before','writes','after','reads'))
    tenants = proof['tenants']
    require(len(tenants) == 2 and len(set(tenants)) == 2, 'two distinct measured tenants')
    keys = [conversation_key(tenant, proof['resource']) for tenant in tenants]
    require(data['tenantKeys'] == keys, 'actual base64url tenant/resource key derivation')
    before = proof['before']; require(set(before) == set(keys) and before[keys[0]] != before[keys[1]], 'distinct initial tenant contents')
    require(all(isinstance(v,str) and v for v in before.values()), 'controlled persisted contents')
    state = deepcopy(before)
    require(len(proof['writes']) == 2, 'write in each tenant')
    for tenant, key, row in zip(tenants, keys, proof['writes']):
        shape(row, ('tenant','resource','key','value','before','after'))
        require((row['tenant'],row['resource'],row['key']) == (tenant,proof['resource'],key), 'write scoped to measured tenant/resource')
        require(same(row['before'],state) and isinstance(row['value'],str) and row['value'] and row['value'] != state[key], 'measured before/write value')
        state[key] = row['value']
        require(same(row['after'],state), 'one write leaves other tenant unchanged')
    require(same(proof['after'],state) and state[keys[0]] != state[keys[1]], 'distinct final persisted tenant values')
    require(len(proof['reads']) == 4, 'both tenants read own and foreign resource')
    for row, (tenant, owner) in zip(proof['reads'], ((tenants[0],tenants[0]),(tenants[1],tenants[1]),(tenants[0],tenants[1]),(tenants[1],tenants[0]))):
        shape(row, ('tenant','resourceTenant','resource','key','result','value'))
        permitted = tenant == owner
        require((row['tenant'],row['resourceTenant'],row['resource']) == (tenant,owner,proof['resource']), 'controlled cross-tenant request identity')
        require(row['key'] == (conversation_key(tenant,proof['resource']) if permitted else None), 'authorization before foreign storage read')
        require(row['result'] == ('Current' if permitted else 'Forbidden') and same(row['value'],state[conversation_key(tenant,proof['resource'])] if permitted else None), 'correctly scoped query contents')
    return ['derived-storage-keys','two-tenant-write-isolation','scoped-own-and-foreign-reads']


def conflicts(rows):
    """One changed independent binding input per duplicate conflict, plus invalid inputs."""
    for row in rows:
        if 'malformedVariants' in row:
            require(len(row['malformedVariants']) == (1 if row['id'] in ('missing-id','invalid-time') else 0), 'exact malformed counterpart inventory')
        original, reused, field = row['original'], row['reused'], row['id']
        binding, identity, _ = domain.conversation_envelope(original)
        require(original != reused, 'conflict contains distinct original/reused inputs')
        # Position-only payloads isolate each binding component from contract metadata.
        if field in domain.BINDING_FIELDS:
            expected = deepcopy(original)
            source_key = {'payloadSha256':'payload','selectedUtcTicks':'timestampUtcTicks'}.get(field,field)
            expected[source_key] = reused[source_key]
            require(same(reused,expected), 'only named authoritative binding input changes')
            if field == 'serializationFormat':
                require(reused[field] != 'json' and domain.nonblank(reused[field]), 'unsupported changed codec fails closed')
            else:
                other, other_id, _ = domain.conversation_envelope(reused)
                require(other_id == identity and [key for key in domain.BINDING_FIELDS if not same(binding[key],other[key])] == [field], 'derive the exact differing duplicate binding field')
        else:
            require(field in ('missing-id','invalid-time'), 'known malformed input case')
            # Contract and position-only cases both carry their actual failing value.
            original_payload = strict_json(original['payload'].encode())
            reused_payload = strict_json(reused['payload'].encode())
            metadata = reused_payload.get('metadata')
            require(('metadata' in original_payload) == (metadata is not None), 'malformed input preserves identity selection mode')
            expected=deepcopy(original)
            if metadata is not None:
                expected_payload=deepcopy(original_payload)
                selected_field='eventId' if field=='missing-id' else 'occurredAt'
                expected_payload['metadata'][selected_field]=metadata[selected_field]
                require(same(reused_payload,expected_payload), 'only selected contract identity/time is malformed')
                expected['payload']=reused['payload']
            else:
                selected_field='messageId' if field=='missing-id' else 'timestampUtcTicks'
                expected[selected_field]=reused[selected_field]
            require(same(expected,reused), 'malformed case changes only selected authoritative input')
            if field == 'missing-id':
                value = metadata['eventId'] if metadata is not None else reused['messageId']
                require(value is None or isinstance(value,str) and not domain.nonblank(value), 'actually missing selected identity')
            else:
                value = metadata['occurredAt'] if metadata is not None else reused['timestampUtcTicks']
                try:
                    domain.timestamp(value,conversation=True) if metadata is not None else domain.ticks(value)
                except (ValueError,TypeError): pass
                else: raise ValueError('supposed malformed selected time is valid')
            try: domain.conversation_envelope(reused)
            except (ValueError,TypeError): pass
            else: raise ValueError('malformed authoritative input must fail validation')
        if row.get('malformedVariants'):
            require(field in ('missing-id','invalid-time') and len(row['malformedVariants'])==1, 'contract malformed counterpart inventory')
            variant=row['malformedVariants'][0]
            require('metadata' in strict_json(variant['original']['payload'].encode()), 'malformed counterpart uses contract identity')
            conflicts([dict(id=field,original=variant['original'],reused=variant['reused'],state=row['state'],reason=row['reason'],applied=row['applied'])])
        require(row['state'] == 'Unavailable' and row['reason'] == 'metadata_contradictory' and row['applied'] is False, 'derived conflict fails closed without application')


def transport_time(proof):
    shape(proof, ('original','reused','state','applied'))
    original, reused = proof['original'],proof['reused']
    expected=deepcopy(original);expected['timestampUtcTicks']=reused['timestampUtcTicks']
    require(same(expected,reused) and original['timestampUtcTicks'] != reused['timestampUtcTicks'], 'only transport clock differs')
    first, identity, payload=domain.conversation_envelope(original)
    second, other, _=domain.conversation_envelope(reused)
    require('metadata' in payload and same(first,second) and identity==other and proof['state']=='Healthy' and proof['applied'] is False, 'valid winning contract time remains idempotent')


def tenant_record(record, tenant):
    domain.sequence(record['sequence'])
    # Reuse the actual transition oracle to validate raw payload shape/type/tenant.
    start=dict(tenantId=tenant,name='Seed',description=None,status='Active',members={},configuration={})
    domain.tenant_replay(start,[record],record['sequence']-1,record['sequence'])
    return {k:record[k] for k in ('sequence','messageId','eventType','payloadSha256')}


def erase_targets(row):
    """Captured persisted pair metadata, keyed by its source-derived identities.

    The versioned controlled v2 capture carries lifecycle generation separately
    from existing pair freshness/dispatch metadata. It is an evidence DTO for the
    future runtime, not an assertion that a v2 storage implementation exists.
    """
    targets, captured = row['deletionTargets'], row['capturedObjects']
    require(len(captured) == len(targets) and len({x['key'] for x in captured}) == len(captured), 'exact unique captured stored-object inventory')
    require([x['key'] for x in captured] == [x['key'] for x in targets], 'capture and deletion inventories agree')
    for target, observation in zip(targets, captured):
        shape(observation, ('key','storedMetadataBytes','storedMetadataSha256','sourceRecords'))
        raw=observation['storedMetadataBytes']; require(digest(raw.encode())==observation['storedMetadataSha256'], 'immutable captured stored metadata bytes')
        stored=strict_json(raw.encode()); shape(stored, ('version','lifecycleGeneration','summary','detail','dispatchId'))
        require(type(stored['version']) is int and stored['version']==2, 'versioned controlled v2 stored capture')
        state,_=domain.conversation_replay(observation['sourceRecords'])
        require(state['tenantId']==row['tenant'] and target['tenant']==state['tenantId'], 'source-derived retiring tenant')
        position=len(observation['sourceRecords'])
        domain.sequence(stored['lifecycleGeneration'])
        require(stored['lifecycleGeneration']==row['beforeGeneration']==target['generation'], 'captured retiring lifecycle generation')
        require(position<=row['barrier'], 'source position is covered by erase barrier')
        expected=dict(tenantId='tenant:'+state['tenantId'],conversationId='conv:'+state['conversationId'],freshness=dict(lastAppliedEventPosition=position))
        require(same(stored['summary'],expected) and same(stored['detail'],expected), 'captured summary/detail identity and generation location')
        require(text(stored['dispatchId'])==row['retiringDispatchId'], 'captured retiring dispatch identity')
        require(observation['key']==target['key']==conversation_key(state['tenantId'],state['conversationId']), 'stored-object key derives from actual tenant/resource metadata')
    require(same(row['deletedTargets'],targets), 'exact captured/deleted target inventory')


def tenant_transitions(rows):
    for row in rows:
        name=row['id']; proof=row['proof']
        shape(proof,('tenant','delivered','accepted','historyState','history','before','after','cas'))
        delivered=tenant_record(proof['delivered'],proof['tenant'])
        before,after,cas=proof['before'],proof['after'],proof['cas']
        for state in (before,after):
            shape(state,('etag','checkpoint','projection','lastAccepted','health'))
            text(state['etag']);domain.sequence(state['checkpoint'],checkpoint=True);domain.tenant_state(state['projection'])
            require(state['projection']['tenantId']==proof['tenant'],'same-tenant composite state')
        require(before['health']=='Healthy','controlled transition starts from Healthy composite state')
        require(before['checkpoint']==row['beforeSequence'] and after['checkpoint']==row['afterSequence'],'composite checkpoint binds measured transition')
        shape(cas,('expectedEtag','succeeded','commitId','projectionCommitId','checkpointCommitId'))
        require(cas['expectedEtag']==before['etag'],'opaque CAS expectation identifies pre-state')
        accepted=tenant_record(proof['accepted'],proof['tenant']) if proof['accepted'] is not None else None
        require(same(before['lastAccepted'],accepted) and (accepted is None or accepted['sequence']==before['checkpoint']),'last-accepted fingerprint retained at the composite checkpoint')
        expected=deepcopy(before)
        if name=='next':
            require(delivered['sequence']==before['checkpoint']+1 and proof['historyState']=='NotRequired' and proof['history'] is None,'exact-next source event')
            projection,_=domain.tenant_replay(before['projection'],[proof['delivered']],before['checkpoint'],delivered['sequence'])
            expected.update(checkpoint=delivered['sequence'],projection=projection,lastAccepted=delivered,health='Healthy',etag=after['etag'])
            require(after['etag']!=before['etag'] and cas['succeeded'] is True and text(cas['commitId'])==cas['projectionCommitId']==cas['checkpointCommitId'],'single successful composite projection-checkpoint CAS')
        else:
            if name in ('duplicate','conflict'):
                require(delivered['sequence']==before['checkpoint'] and accepted is not None and accepted['sequence']==before['checkpoint'] and proof['historyState']=='NotRequired' and proof['history'] is None,'accepted sequence identity comparison')
                require(same(delivered,accepted) is (name=='duplicate'),'identical accepted duplicate versus conflicting input')
            else:
                require(delivered['sequence']<before['checkpoint'],'strictly older delivered sequence')
                history=proof['history']
                if name=='older-unavailable': require(proof['historyState']=='Unavailable' and history is None,'unavailable authoritative history')
                elif name=='older-missing': require(proof['historyState']=='Available' and history is None,'missing authoritative history')
                else:
                    require(proof['historyState']=='Available' and history is not None,'available authoritative history record')
                    recorded=tenant_record(history,proof['tenant'])
                    require(recorded['sequence']==delivered['sequence'] and same(recorded,delivered) is (name=='older-match'),'actual older authoritative identity match/conflict')
        expected_health={'next':'Healthy','duplicate':'Healthy','conflict':'Corrupt','older-match':'Healthy','older-missing':'Corrupt','older-conflict':'Corrupt','older-unavailable':'Unavailable'}[name]
        if name not in ('next','duplicate','older-match'):
            expected.update(health=expected_health,etag=after['etag'])
        if name in ('duplicate','older-match'):
            require(cas['succeeded'] is None and all(cas[k] is None for k in ('commitId','projectionCommitId','checkpointCommitId')), 'identical verified input causes no write')
        else:
            require(after['etag']!=before['etag'] and cas['succeeded'] is True and text(cas['commitId'])==cas['projectionCommitId']==cas['checkpointCommitId'], 'single successful durable composite CAS including fail-closed health')
        require(same(after,expected), 'complete composite persisted end-state without split write or old reapplication')
        require(row['state']==expected_health and row['applied'] is (name=='next'),'transition result derives from source/history')


AUDIT_WARNING = 'No audit availability signal is registered for onboarding diagnostics; reporting audit unavailable (fail-closed).'


def tenant_data_safety(proof):
    shape(proof,('tenant','tenantEvents','canaries','conversationEvents','emittedEvents','auditProbe','emittedLogs'))
    tenant=text(proof['tenant']);canaries=proof['canaries']
    shape(canaries,('name','description','configuration'))
    require(len(set(canaries.values()))==3 and all(isinstance(v,str) and len(v)>12 for v in canaries.values()),'distinct synthetic tenant personal canaries')
    events=proof['tenantEvents'];require(len(events)==2,'controlled tenant input inventory')
    start=dict(tenantId=tenant,name='Seed',description=None,status='Active',members={},configuration={})
    state,_=domain.tenant_replay(start,events,0,2)
    require([r['eventType'] for r in events]==['TenantUpdated','TenantConfigurationSet'] and state['name']==canaries['name'] and state['description']==canaries['description'] and state['configuration']=={'personal-control':canaries['configuration']},'canaries occur in authoritative tenant fields')
    require(len(proof['conversationEvents'])==2 and len(proof['emittedEvents'])==2,'retained emitted Conversations event inventory')
    for event,emitted in zip(proof['conversationEvents'],proof['emittedEvents']):
        binding,_,_=domain.conversation_envelope(event)
        require(binding['tenant']==tenant,'experiment tenant binds legitimate Conversations data')
        # Emitted bytes must have the genuine source contract shape and exact values.
        require(emitted==event['payload'],'emitted public event equals the controlled authoritative event')
        domain.conversation_envelope(dict(event,payload=emitted))
    require(proof['auditProbe']==dict(tenant=tenant,provider='DefaultConversationAuditAvailabilitySignal',result='AuditUnavailable'),'actual controlled tenant diagnostic invocation')
    expected=dict(category='Hexalith.Conversations.Server.Diagnostics.DefaultConversationAuditAvailabilitySignal',level='Warning',eventId=0,message=AUDIT_WARNING,state={'{OriginalFormat}':AUDIT_WARNING})
    require(len(proof['emittedLogs'])==1 and same(strict_json(proof['emittedLogs'][0].encode()),expected),'actual allowed structured warning fields and values')
    for raw in [*proof['emittedEvents'],*proof['emittedLogs']]:
        decoded=strict_json(raw.encode())
        # Search parsed recursive content as well as literal bytes (escaped JSON included).
        visible=json.dumps(decoded,ensure_ascii=False)
        require(all(canary not in raw and canary not in visible for canary in canaries.values()),'controlled tenant canaries absent from emitted bytes including nested values')


def redaction(proof):
    shape(proof,('sourceBefore','sourceAfter','redactionEvent','persistedBefore','persistedAfter','queryAfter','targetMessage','controlMessage'))
    before,_=domain.conversation_replay(proof['sourceBefore'])
    require(same(proof['persistedBefore'],before),'pre-redaction persisted source-derived content')
    event=proof['redactionEvent'];binding,identity,payload=domain.conversation_envelope(event)
    require(identity not in before['processedEventIds'], 'redaction introduces a new authoritative event identity')
    require(binding['eventType']=='MessageContentRedacted' and binding['sequence']==len(proof['sourceBefore'])+1 and binding['tenant']==before['tenantId'] and binding['conversation']==before['conversationId'],'same conversation append-only redaction')
    require(same(proof['sourceAfter'],[*proof['sourceBefore'],event]),'append-only governance preserves original authoritative source events')
    target=domain.identifier(payload['target']['messageId'],'message')
    require(target==proof['targetMessage'] and target!=proof['controlMessage'],'exact targeted and unaffected control messages')
    messages={m['messageId']:m for m in before['messages']}
    require(target in messages and proof['controlMessage'] in messages,'redaction target and control exist in history')
    after=deepcopy(before)
    for message in after['messages']:
        if message['messageId']==target: message['text']='[redacted]'
    after['processedEventIds']=sorted([*after['processedEventIds'],payload['metadata']['eventId']],key=lambda x:x.encode('utf-16-be'))
    attribution=dict(target=payload['target'],category=payload['category'],policyReference=payload['policyReference'],reasonClass=payload['rationale'],actorPartyId=payload['metadata']['actorPartyId'],redactedAt=payload['metadata']['occurredAt'],auditEvidence=payload['auditEvidence'],trustState='Redacted',placeholder='[redacted]')
    expected=dict(state=after,redactions=[attribution],messageTrust=[dict(messageId=m['messageId'],trustState='Redacted' if m['messageId']==target else 'Current') for m in after['messages']])
    require(same(proof['persistedAfter'],expected) and same(proof['queryAfter'],expected),'source-derived masking, safe attribution and trust preserved in persisted/production-query state')

# Bounded test catalog: one named Conversations route with no optional source-guard
# enrollment. The full authoritative algorithm is retained with attribution; a
# different inventory/enrollment needs a separately reviewed protocol successor.
CATALOG_CONTROL = dict(appId='conversation', serviceVersion='v1',
    routes=[dict(domain='conversation',projectionType='conversation-read-model')],
    wireRoutes=['/project/v2','/project/v2/reconcile'])


def catalog_fingerprint(app_id, service_version, routes, ordinary):
    """Exact EventStore UTF-8 length-prefixed framing for this unguarded catalog."""
    ordered=sorted(routes,key=lambda r:(r['domain'].encode('utf-16-be'),r['projectionType'].encode('utf-16-be')))
    chunks=[]
    def append(value):
        raw=text(value).encode('utf-8');chunks.extend((struct.pack('>i',len(raw)),raw))
    append('named-projection-dispatch-v2');append('2');append(app_id);append(service_version)
    chunks.append(struct.pack('>i',len(ordered)))
    for row in ordered:
        shape(row,('domain','projectionType'));append(row['domain']);append(row['projectionType'])
    subset=sorted(ordinary,key=lambda r:(r['domain'].encode('utf-16-be'),r['projectionType'].encode('utf-16-be')))
    require(len({(r['domain'],r['projectionType']) for r in routes})==len(routes),'unique named route inventory')
    require(len({(r['domain'],r['projectionType']) for r in subset})==len(subset) and all(r in ordered for r in subset),'unique ordinary subset')
    if subset!=ordered:
        append('ordinary-delivery-v1');chunks.append(struct.pack('>i',len(subset)))
        for row in subset: append(row['domain']);append(row['projectionType'])
    return digest(b''.join(chunks))


def catalog(data):
    raw=data['catalogBytes'];value=strict_json(raw.encode())
    shape(value,('dispatchVersion','dispatchCapability','appId','serviceVersion','catalogFingerprint','domains'))
    require(type(value['dispatchVersion']) is int and value['dispatchVersion']==2 and value['dispatchCapability']=='named-projection-dispatch-v2','actual catalog protocol identity')
    require(value['appId']==CATALOG_CONTROL['appId'] and value['serviceVersion']==CATALOG_CONTROL['serviceVersion'],'independently pinned controlled runtime identity')
    routes=[];ordinary=[]
    require(len(value['domains'])==1,'exact controlled domain catalog inventory')
    for entry in value['domains']:
        shape(entry,('domain','eventTypes','rejectionEventTypes','commandTypes','aggregateTypes','projectionNames','queryTypes','namedProjectionTypes','ordinaryProjectionTypes','fullReplayProjectionTypes','mappedOnlyStreamedProjectionTypes','incrementalReplayProjectionTypes','sharedEpochProjectionTypes','singleEventProjectionTypes'))
        require(all(isinstance(entry[k],list) and all(isinstance(v,str) for v in entry[k]) for k in ('eventTypes','rejectionEventTypes','commandTypes','aggregateTypes','projectionNames','queryTypes','namedProjectionTypes','ordinaryProjectionTypes','fullReplayProjectionTypes','mappedOnlyStreamedProjectionTypes','incrementalReplayProjectionTypes','sharedEpochProjectionTypes','singleEventProjectionTypes')),'typed native catalog metadata')
        named=set(entry['namedProjectionTypes'])
        subsets=('ordinaryProjectionTypes','fullReplayProjectionTypes','mappedOnlyStreamedProjectionTypes','incrementalReplayProjectionTypes','sharedEpochProjectionTypes','singleEventProjectionTypes')
        require(all(len(set(entry[key]))==len(entry[key]) and set(entry[key])<=named for key in subsets), 'unique native capability subsets belong to named catalog')
        require(set(entry['mappedOnlyStreamedProjectionTypes'])<=set(entry['fullReplayProjectionTypes']) and set(entry['incrementalReplayProjectionTypes'])<=set(entry['fullReplayProjectionTypes']) and not set(entry['singleEventProjectionTypes']) & set(entry['fullReplayProjectionTypes']), 'actual full-replay and single-event subset relationships')
        require(entry['fullReplayProjectionTypes']==['conversation-read-model'] and all(entry[key]==[] for key in subsets[2:]), 'existing controlled handler has only full-replay capability')
        routes.extend(dict(domain=entry['domain'],projectionType=p) for p in entry['namedProjectionTypes'])
        ordinary.extend(dict(domain=entry['domain'],projectionType=p) for p in entry['ordinaryProjectionTypes'])
    require(routes==CATALOG_CONTROL['routes'] and ordinary==routes,'exact pinned route and ordinary-delivery inventories')
    fingerprint=catalog_fingerprint(value['appId'],value['serviceVersion'],routes,ordinary)
    require(value['catalogFingerprint']==data['catalogFingerprint']==fingerprint and data['catalogSha256']==digest(raw.encode()),'actual framed catalog fingerprint and raw byte digest')
    require(data['runtimeId']==value['appId']+'@'+value['serviceVersion'],'measured runtime matches catalog app and version')
    require(data['wireRoutes']==CATALOG_CONTROL['wireRoutes'],'unique complete controlled project/reconcile transport inventory')
    route=data.get('route',data.get('wireRoute'))
    require('/'+route=='/project/v2/reconcile' and '/'+route in data['wireRoutes'],'selected authenticated reconcile endpoint belongs to measured inventory')
    pending=data['pendingLocation'];shape(pending,('id','tenant','conversation','sequence','domain','projectionType','appId','serviceVersion','catalogFingerprint','route'))
    require(same(pending,dict(id=data['persistedPendingId'],tenant=data['tenant'],conversation=data['pendingBinding']['conversation'],sequence=data['pendingSequence'],domain=routes[0]['domain'],projectionType=routes[0]['projectionType'],appId=value['appId'],serviceVersion=value['serviceVersion'],catalogFingerprint=fingerprint,route='/project/v2/reconcile')),'same pending item scoped to actual runtime/catalog/route')
