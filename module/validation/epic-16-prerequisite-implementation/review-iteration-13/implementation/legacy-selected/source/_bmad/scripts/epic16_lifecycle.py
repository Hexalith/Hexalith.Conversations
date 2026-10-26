"""Controlled future-v2 lifecycle captures, never a runtime provider implementation.

Component names are logical capture identities. Only the existing detail/index
keys are physical keys; lifecycle generation is independent of v1 dispatch position.
"""
from copy import deepcopy

import epic16_domain as domain
from epic16_domain import require, same, text
from epic16_relationships import index_key, conversation_key
from evidence_boundary_current import canonical, digest


def commit_before_ack(proof, delivery, record, persisted, committed_at, acknowledged_at):
    """One captured schedule correlates durable success and later acknowledgement."""
    text(delivery); text(record)
    commit, ack = proof['commit'], proof['acknowledgement']
    require(proof['captureVersion'] == 1 and text(proof['scheduleId']), 'versioned acknowledgement operation schedule')
    for operation in (commit, ack):
        domain.sequence(operation['order']); domain.ticks(operation['utcTicks'])
        require(operation['executed'] is True and operation['deliveryId'] == delivery and operation['recordId'] == record, 'executed operations correlate delivery and durable record')
        require(operation['transactionId'] == text(commit['transactionId']), 'ack observes the successful durable transaction')
    require(commit['operation'] == 'commit' and ack['operation'] == 'acknowledge' and commit['succeeded'] is True, 'ack requires successful durable commit')
    require(same(commit['persisted'], persisted) and same(ack['observedRecord'], persisted), 'operation observations bind actual persisted identity and state')
    require(commit['order'] < ack['order'] and commit['utcTicks'] <= ack['utcTicks'], 'commit precedes ack by operation order even when clock ticks coincide')
    require(commit['utcTicks'] == committed_at and ack['utcTicks'] == acknowledged_at, 'summary timestamps derive from correlated operations')


def erase_reference_identity(deliveries):
    by_sequence, by_position, by_message = {}, {}, {}
    for delivery in deliveries:
        ref = delivery['reference']
        sequence = (ref['tenant'], ref['stream'], ref['sequence'])
        position = ref['position']
        message = ref['messageId']
        identity = (sequence, position, message)
        for inventory, key in ((by_sequence, sequence), (by_position, position), (by_message, message)):
            require(key not in inventory or inventory[key] == identity, 'immutable authoritative reference has one forward and reverse identity')
            inventory[key] = identity


def stream_boundaries(row):
    """Derive aggregate cutoffs from complete authoritative envelope/position pairs."""
    all_positions, all_events = set(), set()
    for stream in row['streams']:
        observations = stream['authoritativeEvents']
        require(observations, 'authoritative stream observations are nonempty')
        events, previous = [], 0
        for sequence, observed in enumerate(observations, 1):
            event = observed['event']
            binding, identity, _ = domain.conversation_envelope(event)
            domain.sequence(observed['position'])
            require(binding['tenant'] == stream['tenant'] == row['tenant'] and binding['conversation'] == stream['conversation'] and observed['stream'] == stream['id'], 'authoritative position belongs to exact tenant stream and conversation')
            require(event['sequence'] == sequence and observed['sequence'] == sequence and observed['eventId'] == identity and observed['eventSha256'] == digest(canonical(event)), 'position binds complete exact-next authoritative event identity')
            require(observed['position'] > previous and observed['position'] <= row['C'], 'authoritative positions increase sparsely through captured C')
            require(observed['position'] not in all_positions and (binding['tenant'], binding['conversation'], identity) not in all_events, 'unique global positions and selected event identities')
            all_positions.add(observed['position']); all_events.add((binding['tenant'], binding['conversation'], identity))
            previous = observed['position']; events.append(event)
        domain.conversation_replay(events)
        at_b = sum(observed['position'] <= row['B'] for observed in observations)
        at_c = sum(observed['position'] <= row['C'] for observed in observations)
        require(stream['boundarySequence'] == stream['replayedThroughB'] == at_b and stream['end'] == stream['replayedThroughC'] == at_c, 'B and C sequence cutoffs derive from authoritative global positions')


KINDS = ('detail', 'dispatch', 'manifest', 'watermark', 'queue')
FENCES = ('version', 'etag', 'generation')


def lifecycle_contents(row):
    """Expected controlled states retain AD-7 entry, work and completion boundaries.

    Component keys except detail are logical capture slots, not physical Dapr keys.
    Absent slots retain their observation generation without claiming stored content.
    """
    erase = row['id']=='erase'
    sources = [item['sourceRecords'] for item in row['capturedObjects']] if erase else [[item['event'] for item in stream['authoritativeEvents']] for stream in row['streams']]
    states = [domain.conversation_replay(events)[0] for events in sources]
    require(len({state['conversationId'] for state in states}) == len(states), 'unique lifecycle resource inventory')
    resources=[state['conversationId'] for state in states]
    stages=['before','entry','work','quarantine','completion','post-quarantine'] if erase else ['before','entry','work','completion']
    snapshots=[]
    for stage in stages:
        initial=stage=='before';entered=not initial;worked=stage in ('work','quarantine','completion','post-quarantine')
        generation=row['afterGeneration'] if entered else row['beforeGeneration']
        lifecycle=('Ready' if initial else 'Erasing' if stage in ('entry','work','quarantine') else 'Erased') if erase else ('Erased' if initial else 'Ready' if stage=='completion' else 'Rebuilding')
        watermark=row['barrier'] if erase else row['watermarkAfter'] if worked else row['watermarkBefore']
        manifest=[] if initial else [target['key'] for target in row['deletionTargets']] if erase else row['manifestCompleted'] if worked else row['manifestExpected']
        quarantine=[] if not erase or stage not in ('quarantine','completion','post-quarantine') else [delivery['reference'] for delivery in row['deliveries'][:2 if stage=='post-quarantine' else 1]]
        summaries=resources if (erase and not worked or not erase and worked) else []
        resource_dispatch={state['conversationId']: [row['retiringDispatchId']] if erase and not worked else [] for state in states}
        dispatch=[reference for state in states for reference in resource_dispatch[state['conversationId']]]
        aggregates=[dict(conversation=state['conversationId'],sequence=len(events)) for state,events in zip(states,sources)] if summaries else []
        index=dict(key=index_key(row['tenant']),schemaVersion=2,generation=generation,lifecycle=lifecycle,watermark=watermark,
            metadataGenerations=dict(lifecycle=generation,manifest=generation,watermark=generation),tenantWatermark=0,aggregateWatermarks=aggregates,summaries=summaries,dispatchReferences=dispatch,
            barrier=None if initial else row['barrier'] if erase else row['B'],manifest=manifest,quarantineReferences=quarantine)
        components=[]
        def component(kind,resource,value,gen=generation):
            components.append(dict(kind=kind,tenant=row['tenant'],resource=resource,key=conversation_key(row['tenant'],resource) if kind=='detail' else None,generation=gen,value=deepcopy(value)))
        for state in states:
            retained=erase and not worked
            component('detail',state['conversationId'],state if retained or not erase and worked else None,row['beforeGeneration'] if retained else generation)
            component('dispatch',state['conversationId'],resource_dispatch[state['conversationId']],row['beforeGeneration'] if retained else generation)
        component('manifest',row['tenant'],manifest)
        component('watermark',row['tenant'],watermark)
        component('queue',row['tenant'],row['catchUp']['drain']['after'] if not erase and worked else [])
        component('quarantine',row['tenant'],quarantine)
        snapshots.append(dict(tenant=row['tenant'],index=index,components=components))
    return stages,snapshots


def lifecycle_transaction(row):
    proof=row['atomicity'];names,contents=lifecycle_contents(row)
    require(proof['captureVersion']==1 and [stage['id'] for stage in proof['stages']]==names[1:], 'distinct lifecycle entry work and completion capture inventory')
    require(row['afterGeneration']==row['beforeGeneration']+1, 'one lifecycle generation advance for the complete operation')
    previous=None;previous_order=0;transactions=[]
    for stage,expected_before,expected_after in zip(proof['stages'],contents,contents[1:]):
        before,after,plan,cas=(stage[key] for key in ('before','after','plan','cas'))
        if row['id']=='rebuild' and stage['id']=='work':
            expected_before=deepcopy(expected_before)
            next(part for part in expected_before['components'] if part['kind']=='queue')['value']=row['catchUp']['drain']['before']
            previous=row['catchUp']['deliveries'][-1]['enqueue']['storedAfter']
        for snapshot,expected in ((before,expected_before),(after,expected_after)):
            index=snapshot['index'];domain.sequence(index['version']);text(index['etag'])
            value=deepcopy(snapshot);del value['index']['version'];del value['index']['etag']
            require(same(value,expected), 'source-derived complete lifecycle state and component presence at each AD-7 boundary')
        require(previous is None or same(before,previous), 'next lifecycle plan observes preceding durable state')
        require(after['index']['version']==before['index']['version']+1 and after['index']['etag']!=before['index']['etag'], 'each executed lifecycle CAS advances version and opaque etag')
        expected={key:before['index'][key] for key in FENCES}
        require(plan['author']==row['planAuthor'] and plan['tenant']==row['tenant'] and same(plan['expected'],expected) and same(plan['indexAfter'],after['index']) and same(plan['componentsAfter'],after['components']), 'writer plan binds complete expected fence and resulting components')
        require(cas['executed'] is True and cas['executor']==row['executor']=='EventStore' and same(cas['expected'],expected) and cas['succeeded'] is True, 'actual EventStore CAS attempt accepts current lifecycle plan')
        domain.sequence(cas['order']);require(cas['order']>previous_order, 'lifecycle commits execute in captured order')
        transaction=text(cas['transactionId']);require(transaction not in transactions, 'distinct entry work and completion durable commits');transactions.append(transaction)
        require(cas['indexTransactionId']==transaction and cas['componentTransactions']==[dict(kind=part['kind'],resource=part['resource'],transactionId=transaction) for part in after['components']], 'one transaction atomically commits each planned index and component boundary')
        previous=after;previous_order=cas['order']
    if row['id']=='erase':
        for delivery,stage in zip(row['deliveries'],(proof['stages'][2],proof['stages'][4])):
            commit=delivery['operations']['commit'];ack=delivery['operations']['acknowledgement']
            require(commit['transactionId']==stage['cas']['transactionId'] and commit['order']==stage['cas']['order'], 'quarantine commit is the measured generation-scoped lifecycle write')
            require(stage['before']['index']['lifecycle']==delivery['lifecycle'] and delivery['reference'] in stage['after']['index']['quarantineReferences'], 'Erasing and Erased deliveries durably retain reference-only quarantine')
            if delivery['lifecycle']=='Erasing':require(ack['order']<proof['stages'][3]['cas']['order'], 'Erasing acknowledgement occurs before terminal tombstone')
    stale=proof['staleAttempt'];entry=proof['stages'][0]
    require(stale['executed'] is True and stale['executor']=='EventStore' and same(stale['plan'],entry['plan']) and same(stale['expected'],entry['plan']['expected']), 'stale writer actually attempts its earlier captured entry plan')
    domain.sequence(stale['order']);require(stale['order']>previous_order, 'stale plan is attempted after newer persisted lifecycle state')
    require(same(stale['storedBefore'],previous) and same(stale['storedAfter'],previous), 'stale attempt preserves every newer component index generation and watermark')
    require(not same(stale['expected'],{key:previous['index'][key] for key in FENCES}) and stale['succeeded'] is False and stale['transactionId'] is None and stale['writtenComponents']==[], 'newer fence rejects stale lifecycle plan without partial writes')
    require(row['lifecycleCasVersion']==row['indexCasVersion']==previous['index']['version'] and row['staleWrites']==int(stale['succeeded']) and row['componentGenerations']==[part['generation'] for part in previous['components']], 'outer lifecycle summaries derive from complete executed stage observations')


def queue_reference(observation, generation):
    binding, identity, _ = domain.conversation_envelope(observation['event'])
    require(observation['eventId']==identity and observation['sequence']==binding['sequence'] and observation['eventSha256']==digest(canonical(observation['event'])), 'queue authoritative location binds exact envelope identity')
    domain.sequence(observation['position']);text(observation['stream'])
    return dict(tenant=binding['tenant'],stream=observation['stream'],sequence=binding['sequence'],position=observation['position'],eventId=identity,messageId=observation['event']['messageId'],generation=generation)


def queue_inventory(references):
    return sorted(references,key=lambda ref:(ref['position'],ref['stream'].encode('utf-16-be'),ref['sequence']))


def catch_up_queue(row):
    """Retain immutable authoritative references through enqueue, drain and cutover.

    Arrival order is measured, not sorted; duplicate arrivals retain one reference.
    The reference hash is a capture deduplication identity, not a physical store key.
    """
    proof=row['catchUp'];require(proof['captureVersion']==1 and text(proof['scheduleId']), 'versioned durable catch-up schedule')
    streams={stream['id']:stream for stream in row['streams']}
    authoritative={observed['position']:observed for stream in row['streams'] for observed in stream['authoritativeEvents']}
    require(proof['deliveries'], 'executed rebuild delivery inventory')
    pending=[];by_position={};by_sequence={};by_identity={};deliveries=set();transactions={stage['cas']['transactionId'] for stage in row['atomicity']['stages']};last_order=0
    entry,work,completion=row['atomicity']['stages']
    persisted=entry['after']
    def remember(reference):
        for index,key in ((by_position,reference['position']),(by_sequence,(reference['tenant'],reference['stream'],reference['sequence'])),(by_identity,(reference['tenant'],reference['eventId'])),(by_identity,('message',reference['messageId']))):
            require(key not in index or same(index[key],reference), 'queue immutable identity cannot conflict at reused authoritative location');index[key]=reference
    for observed in authoritative.values():
        remember(queue_reference(observed,row['afterGeneration']))
    for delivery in proof['deliveries']:
        text(delivery['deliveryId']);require(delivery['deliveryId'] not in deliveries, 'distinct transport delivery observations');deliveries.add(delivery['deliveryId'])
        observed=delivery['authoritative'];reference=queue_reference(observed,row['afterGeneration']);stream=streams.get(reference['stream'])
        require(reference['tenant']==row['tenant'] and delivery['streamIdentity']==dict(id=observed['stream'],tenant=row['tenant'],conversation=observed['event']['conversation']), 'queued authoritative delivery binds independent tenant stream identity')
        if stream is not None:require(observed['event']['conversation']==stream['conversation'], 'queued existing stream retains discovered conversation identity')
        else:require(reference['position']>row['C'], 'new streams outside captured B manifest remain queued after C')
        if reference['position']<=row['C']:
            require(same(observed,authoritative.get(reference['position'])), 'through-C queue delivery matches retained authoritative replay')
        require(same(delivery['reference'],reference) and delivery['referenceId']==digest(canonical(reference)), 'canonical durable reference and deduplication identity derive from immutable event location')
        remember(reference)
        enqueue=delivery['enqueue'];domain.sequence(enqueue['order'])
        require(enqueue['executed'] is True and enqueue['order']>last_order and entry['cas']['order']<enqueue['order']<work['cas']['order'], 'enqueue executes after Rebuilding entry before completed catch-up')
        require(same(enqueue['before'],pending), 'enqueue observes exact preceding durable reference inventory')
        should_queue=reference['position']>row['B'];duplicate=reference in pending
        expected=queue_inventory(pending+([reference] if should_queue and not duplicate else []))
        require(same(enqueue['after'],expected) and enqueue['duplicate'] is duplicate, 'enqueue retains exact immutable union without arrival-order constraints')
        require(enqueue['tenant']==row['tenant'] and enqueue['generation']==row['afterGeneration'], 'enqueue uses current tenant lifecycle generation')
        require(same(enqueue['storedBefore'],persisted) and same(enqueue['expected'],{key:persisted['index'][key] for key in FENCES}), 'enqueue CAS observes actual preceding complete persisted control and queue state')
        text(enqueue['storedAfter']['index']['etag'])
        expected_snapshot=deepcopy(persisted)
        next(part for part in expected_snapshot['components'] if part['kind']=='queue')['value']=expected
        if should_queue:
            expected_snapshot['index'].update(version=persisted['index']['version']+1,etag=enqueue['storedAfter']['index']['etag'])
            require(expected_snapshot['index']['etag']!=persisted['index']['etag'], 'queue commit advances opaque etag and record version')
            transaction=text(enqueue['transactionId']);require(transaction not in transactions, 'each enqueue outcome has its own measured durable transaction');transactions.add(transaction)
            require(enqueue['indexTransactionId']==transaction and enqueue['componentTransactions']==[dict(kind=part['kind'],resource=part['resource'],transactionId=transaction) for part in expected_snapshot['components']], 'one enqueue transaction commits complete captured index and component state')
            require(enqueue['succeeded'] is True and delivery['acknowledged'] is True and delivery['operations'] is not None, 'post-B delivery requires durable queue outcome before ack')
            operations=delivery['operations'];commit=operations['commit'];ack=operations['acknowledgement']
            commit_before_ack(operations,delivery['deliveryId'],delivery['referenceId'],reference,commit['utcTicks'],ack['utcTicks'])
            require(commit['transactionId']==transaction and commit['order']==enqueue['order'] and ack['order']<work['cas']['order'], 'queue ack binds actual enqueue commit before drain')
            last_order=ack['order']
        else:
            require(enqueue['indexTransactionId'] is None and enqueue['componentTransactions']==[], 'before-B delivery causes no component commit')
            require(enqueue['succeeded'] is None and enqueue['transactionId'] is None and delivery['acknowledged'] is False and delivery['operations'] is None, 'at-or-before-B observation requires no new catch-up queue acknowledgement')
            last_order=enqueue['order']
        require(same(enqueue['storedAfter'],expected_snapshot), 'enqueue complete after-state preserves every non-queue field')
        persisted=enqueue['storedAfter'];pending=expected
    require(same(work['before'],persisted), 'drain work CAS starts from last durable enqueue snapshot')
    drain=proof['drain'];domain.sequence(drain['order'])
    retained=[ref for ref in pending if ref['position']>row['C']];drained=[ref for ref in pending if ref['position']<=row['C']]
    require(drain['executed'] is True and drain['succeeded'] is True and drain['order']>last_order and drain['order']==work['cas']['order'] and drain['transactionId']==work['cas']['transactionId'], 'executed durable drain shares captured rebuild work transaction')
    require(same(drain['before'],pending) and same(drain['drained'],drained) and same(drain['after'],retained), 'drain verifies exactly through C and retains every later reference')
    require(row['queuedAfterC']==[ref['position'] for ref in retained], 'post-C summary derives from exact unique surviving references')
    for stage in (work,completion):
        component=next(part for part in stage['after']['components'] if part['kind']=='queue')
        require(component['generation']==row['afterGeneration'] and same(component['value'],retained), 'Ready cutover preserves durable generation-scoped queue component')


def generation_mapping(generation, internal_generations, component_generations):
    """AD-7 separates contradiction inside one control record from cross-key drift."""
    if any(value!=generation for value in internal_generations):return ('Unavailable','metadata_contradictory')
    if any(value!=generation for value in component_generations):return ('Rebuilding','mixed_generation')
    return None


def query_result(snapshot, inputs):
    """Public values derived from the captured index, components and access inputs."""
    require(inputs['providerState'] in ('Available','Unavailable'), 'known query provider state')
    require(snapshot['index']['lifecycle'] in ('Ready','Erasing','Erased','Rebuilding'), 'known stored query lifecycle')
    query_component_identity(snapshot)
    if not inputs['tenantExists'] or not inputs['authorized']:return dict(status='Forbidden',reason='forbidden',content=[])
    if inputs['providerState']!='Available':return dict(status='Unavailable',reason='unavailable',content=[])
    index=snapshot['index'];parts=snapshot['components']
    internal=index['metadataGenerations']
    require(set(internal)=={'lifecycle','manifest','watermark'}, 'complete internal control-record generation fields')
    mismatch=generation_mapping(index['generation'],internal.values(),[part['generation'] for part in parts if part['value'] is not None])
    if mismatch and mismatch[0]=='Unavailable':return dict(status=mismatch[0],reason=mismatch[1],content=[])
    if index['lifecycle'] in ('Erasing','Erased','Rebuilding'):return dict(status='Rebuilding',reason='rebuilding',content=[])
    if mismatch:return dict(status=mismatch[0],reason=mismatch[1],content=[])
    if inputs['sourceGap']:return dict(status='Rebuilding',reason='gap_detected',content=[])
    if index['dispatchReferences']:return dict(status='Rebuilding',reason='rebuilding',content=[])
    require(index['schemaVersion']==2 and index['lifecycle']=='Ready','current query requires explicit v2 Ready')
    content=[part['value'] for part in parts if part['kind']=='detail' and part['value'] is not None]
    require([item['conversationId'] for item in content]==index['summaries'], 'current content exactly matches surviving index summaries')
    return dict(status='Current',reason='current',content=content)


def query_component_identity(snapshot):
    """Admit component identities before evaluating visibility or selecting a kind."""
    index=snapshot['index'];tenant=snapshot['tenant'];parts=snapshot['components']
    require(index['key']==index_key(tenant), 'query control key binds tenant')
    keys=[part['key'] for part in parts if part['key'] is not None]
    require(len(set(keys))==len(keys), 'unique physical query component keys')
    require(len({(part['kind'],part['resource']) for part in parts})==len(parts), 'unique logical query components')
    dispatch=[]
    for part in parts:
        require(part['tenant']==tenant, 'every query component belongs to control tenant')
        require(part['resource'] in {detail['resource'] for detail in parts if detail['kind']=='detail'} if part['kind'] in ('detail','dispatch') else part['resource']==tenant, 'query component resource belongs to controlled detail inventory')
        if part['kind']=='detail':
            require(part['key']==conversation_key(tenant,part['resource']), 'query detail key binds resource')
            if part['value'] is not None:
                require(part['value']['tenantId']==tenant and part['value']['conversationId']==part['resource'], 'query detail value binds resource')
        if part['kind']=='dispatch' and part['value'] is not None:
            dispatch.extend(part['value'])
    require(same(dispatch,index['dispatchReferences']), 'actual dispatch references match control index')


def executed_query(query, snapshot, transaction, lower, upper):
    require(query['captureVersion']==1 and text(query['requestId']) and query['executed'] is True and query['completed'] is True, 'query actually executed and completed at lifecycle boundary')
    domain.sequence(query['order']);require(lower<query['order']<upper, 'query executes between exact committed lifecycle boundaries')
    require(query['tenant']==snapshot['tenant'] and query['observedTransactionId']==transaction and same(query['snapshot'],snapshot), 'query binds precise committed index and component snapshot')
    inputs=query['inputs'];require(all(type(inputs[k]) is bool for k in ('tenantExists','authorized','sourceGap')), 'typed query access and source inputs')
    require(inputs['providerState'] in ('Available','Unavailable'), 'known query provider observation')
    expected=query_result(snapshot,inputs)
    reads=[] if not inputs['tenantExists'] or not inputs['authorized'] else [dict(kind='index',key=snapshot['index']['key'],generation=snapshot['index']['generation'])]
    if reads and inputs['providerState']=='Available':reads += [dict(kind=part['kind'],key=part['key'],resource=part['resource'],generation=part['generation']) for part in snapshot['components']]
    require(same(query['returned'],expected), 'executed query returns only snapshot-derived public state reason and content')
    require(same(query['reads'],reads) and query['writes']==[] and query['recoveryCalls']==[], 'query captures exact reads with no repair recovery or mutation')


def lifecycle_queries(row):
    stages=row['atomicity']['stages'];queries=row['queries']
    require([query['stage'] for query in queries]==[stage['id'] for stage in stages], 'query execution at every entry deletion quarantine and cutover boundary')
    require(len({q['requestId'] for q in queries})==len(queries), 'distinct executed lifecycle queries')
    for index,(query,stage) in enumerate(zip(queries,stages)):
        require(query['inputs']==dict(tenantExists=True,authorized=True,providerState='Available',sourceGap=False), 'lifecycle boundary control executes an authorized provider read')
        upper=stages[index+1]['cas']['order'] if index+1<len(stages) else row['atomicity']['staleAttempt']['order']
        executed_query(query,stage['after'],stage['cas']['transactionId'],stage['cas']['order'],upper)


def cross_key_queries(proof):
    """Independent controlled reads isolate index/detail/dispatch generation drift."""
    require(proof['captureVersion']==1, 'versioned cross-key query experiment')
    baseline=proof['baseline'];queries=proof['queries']
    state,_=domain.conversation_replay(proof['authoritativeEvents'])
    require(baseline['tenant']==state['tenantId'] and baseline['index']['key']==index_key(state['tenantId']) and baseline['index']['summaries']==[state['conversationId']], 'cross-key query baseline binds authoritative tenant and resource')
    details=[part for part in baseline['components'] if part['kind']=='detail']
    require(len(details)==1 and len([part for part in baseline['components'] if part['kind']=='dispatch'])==1, 'cross-key experiment requires one detail and one dispatch observation')
    require(len(details)==1 and same(details[0]['value'],state) and details[0]['key']==conversation_key(state['tenantId'],state['conversationId']), 'cross-key source-derived complete detail state')
    require([q['stage'] for q in queries]==['matching','index','detail','dispatch','internal'], 'matching and each distinct generation consistency context')
    require(query_result(baseline,queries[0]['inputs'])['status']=='Current', 'cross-key control starts with internally valid Ready data')
    require(len({q['requestId'] for q in queries})==len(queries), 'distinct cross-key query requests')
    for query in queries:
        expected=deepcopy(baseline);kind=query['stage']
        if kind=='index':
            expected['index']['generation']+=1
            expected['index']['metadataGenerations']={key:expected['index']['generation'] for key in expected['index']['metadataGenerations']}
        elif kind in ('detail','dispatch'):
            part=next(part for part in expected['components'] if part['kind']==kind);part['generation']+=1
        elif kind=='internal':expected['index']['metadataGenerations']['manifest']+=1
        executed_query(query,expected,proof['transactionId'],proof['commitOrder'],proof['endOrder'])
        required='Current' if kind=='matching' else 'Unavailable' if kind=='internal' else 'Rebuilding'
        require(query['returned']['status']==required, 'internal contradiction and cross-key mismatch retain distinct non-disclosing mappings')
