"""Executed negative and transport observations for controlled future-v2 captures.

This oracle does not write product state, host an endpoint or grant runtime entry.
Wire fields retain content-safe metadata from the existing EventStore route;
operation ordinals/transaction links are versioned future capture fields.
"""
from copy import deepcopy

import epic16_domain as domain
from epic16_domain import require, same, text
from epic16_native import strict_json, canonicalize
from evidence_boundary_current import canonical, digest

WARNING_PATH = 'src/Hexalith.Conversations.Server/Program.cs'
WARNING_MARKER = 'SINGLE-REPLICA-ONLY'
WIRE_POLICY = dict(method='POST', path='/project/v2/reconcile',
    requestType='Hexalith.EventStore.Contracts.Projections.ProjectionDispatchRequest',
    responseType='Hexalith.EventStore.Contracts.Projections.ProjectionDispatchResponse',
    dispatcher='Hexalith.EventStore.DomainService.DomainProjectionDispatcher.ReconcileAsync',
    policy='EventStoreWorkload:domain-service:project')


def replay_rejections(data):
    """Derive invalid inputs, executed rejection and unchanged complete stored value."""
    state,times=domain.conversation_replay(data['authoritativeEvents'])
    expected=dict(version=2,anchorUtcTicks=max(times),items=domain.collection_items(state))
    rows=data['rejectedOperations']
    require([row['id'] for row in rows]==['unstable-key','wrong-order','generation-before-anchor'], 'exact required executed replay negative controls')
    require(len({row['operationId'] for row in rows})==len(rows), 'distinct rejected operations')
    for row in rows:
        require(row['captureVersion']==1 and text(row['operationId']) and row['executed'] is True and row['completed'] is True, 'negative writer operation executed and completed')
        require(row['tenant']==state['tenantId'] and row['conversation']==state['conversationId'] and row['writer']=='ConversationProjectionReadModelWriter', 'negative operation binds actual tenant resource and writer')
        before=strict_json(row['beforeBytes'].encode());after=strict_json(row['afterBytes'].encode())
        require(same(before,expected) and same(after,before) and row['afterBytes']==row['beforeBytes']==data['rebuilds'][0]['canonicalJson'], 'rejection preserves complete relevant persisted state')
        require(row['beforeSha256']==row['afterSha256']==digest(row['beforeBytes'].encode()), 'rejected persisted snapshot bytes')
        attempted=strict_json(row['attemptedJson'].encode())
        require(set(attempted)==set(expected) and attempted['version']==2 and attempted['anchorUtcTicks']==expected['anchorUtcTicks'], 'negative input uses same native replay shape and anchor')
        invalid=deepcopy(expected)
        if row['id']=='unstable-key':
            item=next(item for item in invalid['items'] if not item['key'].startswith('conversation:'))
            collection=item['key'].split(':',1)[0]
            field={'participants':'participantPartyId','messages':'messageId','fileReferences':'fileId'}[collection]
            item['value'][field]='';item['key']=collection+':'
            require(same(attempted,invalid) and not domain.nonblank(item['value'][field]), 'actual native identity cannot supply stable collection key')
        elif row['id']=='wrong-order':
            invalid['items'].reverse()
            require(same(attempted,invalid) and [item['key'] for item in attempted['items']]!=data['collectionKeys'], 'actual ordinal collection order is invalid')
        else:
            require(same(attempted,expected), 'freshness control retains valid deterministic input')
        domain.ticks(row['generatedTicks'])
        bad_time=row['generatedTicks']<expected['anchorUtcTicks']
        require(bad_time is (row['id']=='generation-before-anchor'), 'negative generation is actually earlier than replay anchor')
        require(row['observedGeneratedTicks']==row['generatedTicks'], 'contradictory generation is never clamped')
        require(row['rejected'] is True and row['writes']==[] and row['committed'] is False, 'invalid operation rejected before any persistence')
        expected_result=dict(status='Unavailable',reason='metadata_contradictory') if bad_time else None
        require(same(row['publicResult'],expected_result), 'negative freshness retains existing fail-closed mapping')
    require(data['unorderedCollectionPersisted'] is False and data['generationBeforeAnchorState']==rows[-1]['publicResult']['status'], 'negative summaries derive from executed outcomes')


def reconciliation_wire(data):
    """Content-safe route observations correlate authorization, dispatch and commit."""
    proof=data['wireObservation'];current=data['concurrentRecovery']['current']
    require(proof['captureVersion']==1 and proof['policy']==WIRE_POLICY, 'fixed existing wire contract and versioned capture vocabulary')
    scope=dict(pendingId=data['persistedPendingId'],tenant=data['tenant'],runtimeId=data['runtimeId'],
        catalogFingerprint=data['catalogFingerprint'],projectionTypes=[data['pendingLocation']['projectionType']])
    expected_phases=['request','authorization','dispatch','durable-result','response']
    operations=proof['operations'];require([row['phase'] for row in operations]==expected_phases, 'complete observed production wire request response schedule')
    previous=0
    for row in operations:
        domain.sequence(row['order']);require(row['order']>previous, 'authorization precedes hosted dispatch commit and response');previous=row['order']
        require(row['executed'] is True and row['completed'] is True and row['requestId']==text(proof['requestId']) and same(row['scope'],scope), 'same executed request response pending runtime and route scope')
        require(row['method']==WIRE_POLICY['method'] and row['path']==WIRE_POLICY['path'], 'actual request method and canonical route')
    request,authorization,dispatch,commit,response=operations
    scheduled=proof['scheduledWork']
    require(scheduled['pendingId']==scope['pendingId'] and scheduled['tenant']==scope['tenant'] and scheduled['runtimeId']==scope['runtimeId'] and scheduled['catalogFingerprint']==scope['catalogFingerprint'], 'wire request binds same durable scheduled pending work')
    require(request['contractType']==WIRE_POLICY['requestType'] and request['dispatchId']==text(scheduled['dispatchId']), 'request uses measured durable dispatch identity')
    require(authorization['authenticated'] is True and authorization['allowed'] is True and authorization['policy']==WIRE_POLICY['policy'], 'wire authorization succeeds before dispatch')
    require(dispatch['coordinator']=='EventStore' and dispatch['target']==WIRE_POLICY['dispatcher'] and dispatch['directHandlerCalls']==0, 'hosted coordinator dispatch without handler bypass')
    require(current['succeeded'] is True and current['cleared']==[scope['pendingId']], 'wire success requires actual durable clearance')
    require(commit['transactionId']==response['instrumentation']['transactionId']==current['commitId'] and commit['persistedAfterSha256']==digest(canonical(current['after'])), 'wire durable outcome binds exact measured transaction and complete persisted state')
    require(response['contractType']==WIRE_POLICY['responseType'] and response['body']['version']==2 and response['statusCode']==200 and response['instrumentation']['cleared']==current['cleared'], 'response corresponds to committed clearance')
    outcomes=response['body']['outcomes']
    require([row['projectionType'] for row in outcomes]==scope['projectionTypes'] and all(type(row['status']) is int and row['status'] in (0,1) and row['reasonCode'] is None for row in outcomes), 'actual response outcomes prove Completed or AlreadyCompleted for each pending projection')
    require(request['requestScope']==dict(tenantId=data['tenant'],domain=data['pendingLocation']['domain'],aggregateId=data['pendingLocation']['conversation']), 'observed request body metadata binds actual aggregate scope')
    require(data['wireRoute']==request['path'].lstrip('/') and data['authenticated'] is authorization['authenticated'] and data['coordinator']==dispatch['coordinator'], 'wire summaries derive from actual observations')
