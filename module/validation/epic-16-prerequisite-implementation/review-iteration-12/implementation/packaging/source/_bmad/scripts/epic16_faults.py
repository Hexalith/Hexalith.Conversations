"""Typed conformance fault measurements; never injects or runs product faults."""
from epic16_domain import require, shape, text, tenant_replay, tenant_decision, TENANT_STATE_FIELDS as domain_fields, sequence, same, ticks, tenant_state
from epic16_native import strict_json
from evidence_boundary_current import canonical, digest

# These settings reproduce the existing positive experiment, not an operational SLO.
CONTROL_POLICY = dict(attemptLimit=3, timeoutMs=1000, cancelBoundMs=100,
    endpoints=[dict(id='conversations-http', owner='apphost', kind='http'),
               dict(id='dapr-placement', owner='dapr', kind='tcp'),
               dict(id='dapr-scheduler', owner='dapr', kind='tcp')])


def endpoint_inventory(rows):
    require(isinstance(rows,list) and len(rows)==3,'exact effective endpoint inventory')
    for row, expected in zip(rows,CONTROL_POLICY['endpoints']):
        shape(row,('id','owner','kind','port'))
        require({k:row[k] for k in expected}==expected,'safe bound endpoint identity')
        require(type(row['port']) is int and 1<=row['port']<=65535,'representable endpoint port')
    require(len({r['port'] for r in rows})==len(rows),'unique effective bindings')
    return {r['id']:r for r in rows}


def retry_advice(attempts, elapsed):
    require(type(attempts) is int and type(elapsed) is int and 0<attempts<=CONTROL_POLICY['attemptLimit'] and 0<elapsed<=CONTROL_POLICY['timeoutMs'] and readiness_terminal(attempts,elapsed), 'retry advice belongs to measured terminal bounded control')
    return dict(action='retry-preflight-after-endpoint-ready',attemptLimit=CONTROL_POLICY['attemptLimit'],
        timeoutMs=CONTROL_POLICY['timeoutMs'], attemptsUsed=attempts, elapsedMs=elapsed,
        terminalReason='attempt-limit' if attempts==CONTROL_POLICY['attemptLimit'] else 'timeout',
        automaticRetriesRemaining=0)


def diagnostics(rows, inventory, expected, *, elapsed=None):
    by=endpoint_inventory(inventory)
    require(isinstance(rows,list) and len(rows)==len(expected),'retained structured diagnostics inventory')
    for row, (code, endpoint, attempts) in zip(rows,expected):
        wanted=dict(code=code,endpointId=endpoint,owner=by[endpoint]['owner'],port=by[endpoint]['port'],attempts=attempts)
        if code=='APPHOST_ENDPOINT_NOT_READY':wanted['retryAdvice']=retry_advice(attempts,elapsed)
        shape(row,tuple(wanted))
        require(same(row,wanted),'safe emitted diagnostic contents and bounded retry advice')


def readiness_terminal(attempts, elapsed):
    return attempts == CONTROL_POLICY['attemptLimit'] or elapsed == CONTROL_POLICY['timeoutMs']


def readiness(data):
    require(data['attemptLimit']==CONTROL_POLICY['attemptLimit'] and data['timeoutMs']==CONTROL_POLICY['timeoutMs'],'pinned readiness test controls')
    endpoint_inventory(data['endpointInventory'])
    require(data['endpoint']=='conversations-http','readiness endpoint belongs to inventory')
    require(type(data['attempts']) is int and 1<=data['attempts']<=CONTROL_POLICY['attemptLimit'] and 0<data['elapsedMs']<=CONTROL_POLICY['timeoutMs'],'bounded measured readiness')
    require(readiness_terminal(data['attempts'], data['elapsedMs']), 'readiness reached retry or timeout terminal condition')
    diagnostics(data['diagnostics'],data['endpointInventory'],[('APPHOST_ENDPOINT_NOT_READY',data['endpoint'],data['attempts'])],elapsed=data['elapsedMs'])


def port_collision(data):
    by=endpoint_inventory(data['endpointInventory'])
    require(data['endpoint'] in ('dapr-placement','dapr-scheduler') and by[data['endpoint']]['port']==data['occupiedPort'],'collision names affected owned binding')
    require(data['effectivePorts']==[by[k]['port'] for k in ('dapr-placement','dapr-scheduler')],'complete effective control-plane bindings')
    require(data['unrelatedPort'] not in [row['port'] for row in by.values()],'unrelated port is outside endpoint inventory')
    snapshots=data['occupancy']; shape(snapshots,('before','during','after'))
    expected_before=[dict(endpointId=row['id'],port=row['port'],owner=None) for row in by.values()]+[dict(endpointId=None,port=data['unrelatedPort'],owner='unrelated-process')]
    require(snapshots['before']==snapshots['after']==expected_before,'restore every affected port and unrelated owner')
    expected_during=[dict(row,owner='collision-fixture') if row['endpointId']==data['endpoint'] else row for row in expected_before]
    require(snapshots['during']==expected_during,'measured occupied owned port during injection')
    require(data['portsBefore']==data['portsAfter']==[r['port'] for r in expected_before if r['owner'] is not None],'occupancy arrays derive from measured owners')
    diagnostics(data['diagnostics'],data['endpointInventory'],[('DAPR_CONTROL_PLANE_PORT_COLLISION',data['endpoint'],1)])
    negative = data['negativePreflight']
    require(text(negative['operationId']) != text(data['collisionOperationId']) and negative['executed'] is True and negative['completed'] is True, 'separate executed unrelated-only preflight')
    require(same(negative['endpointInventory'],data['endpointInventory']) and same(negative['occupancy'],expected_before), 'unrelated-only preflight uses same inventory with every owned binding free')
    require(negative['result'] == dict(checkedEndpointIds=list(by), collisionPorts=[]) and negative['diagnostics'] == [] and negative['liveLaneStarted'] is False, 'measured unrelated-only result contains no collision classification')



def _typed_fault_state(value):
    """Raw retained JSON has the same strict primitive types as native observations."""
    boolean_keys = {'healthy','connected','available','authenticated','cleared','allowed','staleAccepted','fenceAccepted','acknowledged','laneStarted','accepted','applied'}
    integer_keys = {'sequence','checkpoint','horizon','version','lease','expiresAt','now','port','requestCount','requiredSequence','attempts','elapsedMs','writes','clearCount','duplicateClearCount','collisionPort','executed','status','redeliveries','invocations'}
    integer_arrays = {'versions','history','deliveries','pending'}
    if isinstance(value, dict):
        for key, item in value.items():
            if key == 'domain':
                tenant_state(item)
                continue
            if key in boolean_keys: require(type(item) is bool, 'typed fault boolean')
            if key in integer_keys and not (key=='status' and isinstance(item,str) or key=='attempts' and isinstance(item,list)): require(type(item) is int, 'typed fault integer')
            if key=='attempts' and isinstance(item,list): require(all(type(x) is str for x in item), 'typed retry attempt records')
            if key=='saveOrder': require(isinstance(item,list) and (all(type(x) is int for x in item) or all(type(x) is str for x in item)), 'typed raw save order')
            if key in integer_arrays:
                require(isinstance(item,list), 'typed fault sequence list')
                for number in item: sequence(number)
            if key in {'sequence','checkpoint','horizon','requiredSequence'}: sequence(item, checkpoint=key in {'sequence','checkpoint','horizon'})
            if key in {'lease','version'}: sequence(item)
            if key in {'expiresAt','now'}: ticks(item)
            if key in {'provider','process','payload','endpointId','route','catalogBytes','pendingId'}: text(item)
            _typed_fault_state(item)
    elif isinstance(value,list):
        for item in value: _typed_fault_state(item)


def _relations(name, before, injected, observed):
    """Return independently evaluated assertions, each rooted in typed native facts."""
    if name=='reverse-save':
        shape(before,('versions','saveOrder'));shape(injected,('versions','saveOrder'));shape(observed,('version','staleAccepted'))
        require(before['versions']==injected['versions'] and len(before['versions'])==2 and 0<before['versions'][0]<before['versions'][1],'distinct ordered versions')
        return dict(injection=before['saveOrder']==before['versions'] and injected['saveOrder']==list(reversed(before['versions'])), outcome=observed==dict(version=max(before['versions']),staleAccepted=False))
    if name in ('equal-sequence-conflict','conflicting-history'):
        shape(before,('sequence','payload'));shape(injected,('sequence','payload'));shape(observed,('state','applied'))
        return dict(injection=before['sequence']==injected['sequence'] and before['sequence']>0 and before['payload']!=injected['payload'],outcome=observed==dict(state='Corrupt',applied=False))
    if name in ('three-future-deliveries','horizon-extension'):
        shape(before,('checkpoint','horizon','deliveries'));shape(injected,('checkpoint','horizon','deliveries'));shape(observed,('state','horizon','pending'))
        values=injected['deliveries']; future=all(type(v)is int and v>before['checkpoint']+1 for v in values)
        condition=len(values)==3 and len(set(values))==3 if name=='three-future-deliveries' else len(values)>len(before['deliveries']) and max(values)>before['horizon'] and set(before['deliveries'])<=set(values)
        return dict(injection=before['checkpoint']==injected['checkpoint'] and future and condition and injected['horizon']==max(before['horizon'], *values) and set(before['deliveries'])<=set(values),outcome=observed==dict(state='Gapped',horizon=max(before['horizon'], *values),pending=values))
    if name=='retry-exhaustion':
        shape(before,('provider','attempts'));shape(injected,('provider','attempts'));shape(observed,('state','acknowledged'))
        return dict(injection=before==dict(provider='Available',attempts=[]) and injected==dict(provider='Unavailable',attempts=['failed']*3),outcome=observed==dict(state='Unavailable',acknowledged=False))
    if name=='lease-expiry':
        shape(before,('lease','version','expiresAt'));shape(injected,('lease','version','now'));shape(observed,('writes','fenceAccepted'))
        return dict(injection=before['lease']>0 and before['version']>0 and injected['lease']==before['lease'] and injected['version']==before['version'] and injected['now']>before['expiresAt']>0,outcome=observed==dict(writes=0,fenceAccepted=False))
    if name=='restart':
        shape(before,('process','snapshot'));shape(injected,('process','snapshot'));shape(observed,('state','allowed','redeliveries'))
        shape(before['snapshot'],('sequence','pending','state'))
        return dict(injection=text(before['process'])!=text(injected['process']) and same(before['snapshot'],injected['snapshot']) and before['snapshot']['state']=='Gapped' and before['snapshot']['pending'] and all(v>before['snapshot']['sequence']+1 for v in before['snapshot']['pending']),outcome=observed==dict(state='Gapped',allowed=False,redeliveries=0))
    if name=='missing-history':
        shape(before,('requiredSequence','history'));shape(injected,('requiredSequence','history'));shape(observed,('state','applied'))
        return dict(injection=before['requiredSequence']==injected['requiredSequence'] and before['requiredSequence'] in before['history'] and injected['history']==[v for v in before['history'] if v!=before['requiredSequence']],outcome=observed==dict(state='Corrupt',applied=False))
    if name in ('provider-unavailable','unavailable-dapr'):
        shape(before,('provider','available'));shape(injected,('provider','available'));shape(observed,('state','writes'))
        provider='tenant-store' if name=='provider-unavailable' else 'dapr'
        return dict(injection=before==dict(provider=provider,available=True) and injected==dict(provider=provider,available=False),outcome=observed==dict(state='Unavailable',writes=0))
    if name=='recovery-success':
        shape(before,('checkpoint','horizon','state','domain'));shape(injected,('records',));shape(observed,('state','checkpoint','pending','domain'))
        state,_=tenant_replay(before['domain'],injected['records'],before['checkpoint'],before['horizon'])
        return dict(injection=before['state']=='Gapped',outcome=observed==dict(state='Healthy',checkpoint=before['horizon'],pending=[],domain=state))
    if name=='two-replicas':
        shape(before,('replicaA','replicaB'));shape(injected,('saveOrder',));shape(observed,('replicaA','replicaB'))
        shape(before['replicaA'],('sequence','allowed','domain'));shape(before['replicaB'],('sequence','allowed','domain'))
        for replica in before.values():
            shape(replica['domain'], domain_fields)
            require(replica['allowed'] is tenant_decision(replica['domain'],replica['sequence'])['allowed'], 'replica decision from full measured domain state')
        return dict(injection=before['replicaA']['sequence']<before['replicaB']['sequence'] and injected['saveOrder']==['newer','older'],outcome=observed['replicaA']==observed['replicaB']==before['replicaB'] and observed['replicaA']['allowed'] is False)
    if name=='route-removal':
        shape(before,('routes',));shape(injected,('routes',));shape(observed,('invocations',))
        return dict(injection=before['routes']==['project/v2/reconcile'] and injected['routes']==[],outcome=observed['invocations']==0)
    if name=='fingerprint-drift':
        shape(before,('route','catalogBytes'));shape(injected,('route','catalogBytes'));shape(observed,('expectedSha256','observedSha256','invocations'))
        return dict(injection=before['route']==injected['route']=='project/v2/reconcile' and before['catalogBytes']!=injected['catalogBytes'],outcome=observed==dict(expectedSha256=digest(before['catalogBytes'].encode()),observedSha256=digest(injected['catalogBytes'].encode()),invocations=0))
    if name=='duplicate-retry':
        shape(before,('pendingId','cleared'));shape(injected,('pendingId','requestCount'));shape(observed,('clearCount','duplicateClearCount','pending'))
        return dict(injection=text(before['pendingId'])==injected['pendingId'] and before['cleared'] is True and injected['requestCount']==2,outcome=observed==dict(clearCount=1,duplicateClearCount=0,pending=[]))
    if name=='endpoint-race':
        shape(before,('endpointId','healthy','connected'));shape(injected,('endpointId','healthy','connected'));shape(observed,('attempts','elapsedMs','laneStarted'))
        return dict(injection=before==dict(endpointId='conversations-http',healthy=True,connected=True) and injected==dict(endpointId='conversations-http',healthy=True,connected=False),outcome=observed['laneStarted'] is False and 0<observed['attempts']<=CONTROL_POLICY['attemptLimit'] and 0<observed['elapsedMs']<=CONTROL_POLICY['timeoutMs'] and readiness_terminal(observed['attempts'], observed['elapsedMs']))
    if name=='port-collision':
        shape(before,('endpointId','port','owner'));shape(injected,('endpointId','port','owner'));shape(observed,('laneStarted','collisionPort'))
        return dict(injection=before['endpointId']==injected['endpointId']=='dapr-placement' and before['port']==injected['port'] and 1<=before['port']<=65535 and before['owner'] is None and injected['owner']=='collision-fixture',outcome=observed==dict(laneStarted=False,collisionPort=before['port']))
    if name=='empty-selection':
        shape(before,('selected',));shape(injected,('selected',));shape(observed,('executed','accepted'))
        return dict(injection=before['selected']==['Scenario01'] and injected['selected']==[],outcome=observed==dict(executed=0,accepted=False))
    if name=='unauthorized':
        shape(before,('route','authenticated'));shape(injected,('route','authenticated'));shape(observed,('status','writes'))
        return dict(injection=before==dict(route='project/v2/reconcile',authenticated=True) and injected==dict(route='project/v2/reconcile',authenticated=False),outcome=observed==dict(status=403,writes=0))
    raise ValueError('unknown typed fault')


def fault_matrix(rows, expected):
    require([r['id'] for r in rows]==list(expected),'exact typed fault inventory')
    for row in rows:
        name=row['id'];target=row['target'];shape(target,('kind','id'))
        expected_target=dict(kind='tenant-projection',id='tenant-a') if name in ('reverse-save','equal-sequence-conflict','three-future-deliveries','horizon-extension','retry-exhaustion','lease-expiry','restart','missing-history','conflicting-history','provider-unavailable','recovery-success','two-replicas') else dict(kind='live-control',id={'endpoint-race':'conversations-http','port-collision':'dapr-placement','unavailable-dapr':'dapr','empty-selection':'Scenario01'}.get(name,'project/v2/reconcile'))
        require(target==expected_target,'fault target identity')
        before=strict_json(row['beforeBytes'].encode()); injected=strict_json(row['injectedBytes'].encode()); after=strict_json(row['afterBytes'].encode())
        for value in (before,injected,after):
            shape(value,('target','state'));require(value['target']==target,'restoration bytes identify actual fault target')
        require(before==after and row['beforeBytes']==row['afterBytes'] and row['beforeSha256']==digest(row['beforeBytes'].encode()) and row['afterSha256']==digest(row['afterBytes'].encode()),'target-bound byte restoration')
        _typed_fault_state(before['state']); _typed_fault_state(injected['state']); _typed_fault_state(row['observed'])
        if name=='recovery-success': require(before['state']['domain']['tenantId']==target['id'],'recovery restoration target tenant')
        if name=='two-replicas': require(all(replica['domain']['tenantId']==target['id'] for replica in before['state'].values()),'replica restoration target tenant')
        checks=_relations(name,before['state'],injected['state'],row['observed'])
        require(all(checks.values()),'typed injected condition and measured outcome')
        checks['restoration']=True
        entries=[dict(id=key,actual=bool(value),expected=True,target=target) for key,value in checks.items()]
        require(row['assertionEntries']==entries and row['assertions']==len(entries),'derived validated fault assertion count')
        require(row['observedCodes']==[expected[name]],'exact derived stable fault code')


def cancellation(data):
    """One monotonic operation schedule proves cancellation through final cleanup."""
    proof=data['cancellation'];require(proof['captureVersion']==1 and text(proof['operationId']), 'versioned controlled cancellation schedule')
    operation=proof['operationId'];inventory=endpoint_inventory(data['endpointInventory'])
    before,owned=proof['preexisting'],proof['owned'];require(before and owned, 'retained preexisting and operation-owned resource inventories')
    require(len({resource['id'] for resource in before+owned})==len(before+owned), 'distinct measured cleanup resources')
    for resource in before+owned:
        text(resource['id']);text(resource['owner'])
        if resource['kind']=='port':
            require(resource['endpointId'] in inventory and resource['port']==inventory[resource['endpointId']]['port'], 'cancellation port belongs to measured effective endpoint')
        else:require(resource['kind']=='process' and resource['port'] is None and resource['endpointId'] is None, 'controlled non-port resource identity')
    require(all(resource['owner']==operation for resource in owned) and all(resource['owner']!=operation for resource in before), 'cleanup ownership is correlated with cancelled operation')
    ports=[resource['port'] for resource in before+owned if resource['kind']=='port']
    require(len(ports)==len(set(ports)), 'captured cancellation resources have unique occupied ports')
    active=before+owned;operations=proof['operations']
    require([row['phase'] for row in operations]==['start','request','observed','cleanup','completion'], 'executed start cancellation request observation cleanup and terminal completion')
    previous_order=0;previous_time=0
    for row in operations:
        require(row['executed'] is True and row['operationId']==operation and row['clockId']==text(proof['clockId']), 'same executed operation and monotonic measurement clock')
        sequence(row['order']);sequence(row['monotonicMs'],checkpoint=True)
        require(row['order']>previous_order and row['monotonicMs']>=previous_time, 'cancellation operations ordered despite equal coarse clock ticks')
        previous_order=row['order'];previous_time=row['monotonicMs']
        require(same(row['resources'],before if row['phase'] in ('cleanup','completion') else active), 'every cancellation boundary retains actual owned and preexisting resource state')
        require(row['cancellationRequested'] is (row['phase']!='start') and row['cancellationObserved'] is (row['phase'] in ('observed','cleanup','completion')), 'request and observed cancellation are distinct executed boundaries')
        require(row['completed'] is (row['phase']=='completion') and row['outcome']==('Cancelled' if row['phase']=='completion' else None), 'terminal completion occurs only after observed cancellation and cleanup')
        require(row['released']==([resource['id'] for resource in owned] if row['phase']=='cleanup' else []), 'cleanup releases exactly this operation resources')
    require(data['portsBefore']==data['portsAfter']==[resource['port'] for resource in before if resource['kind']=='port'], 'outer restored ports derive from preexisting captured resources')
    elapsed=operations[-1]['monotonicMs']-operations[1]['monotonicMs']
    require(data['cancelElapsedMs']==elapsed and 0<=elapsed<=CONTROL_POLICY['cancelBoundMs'] and data['cancelBoundMs']==CONTROL_POLICY['cancelBoundMs'], 'cancellation bound measured from request through terminal cleanup completion')
