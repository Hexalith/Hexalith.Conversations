"""Semantic checks for native Epic 16 observations, without executing supplied commands.

These predicates consume measured values, never a caller's PASS flag. The preserved
contracts retain the full obligations; these are their machine-checkable evidence
protocol. Owner decisions and environmental parity require independent entry pins.
"""
from __future__ import annotations

import json
import epic16_domain as domain
import epic16_faults as faults
import epic16_relationships as relationships
import epic16_schedules as schedules
import epic16_lifecycle as lifecycle
import epic16_execution as execution
from evidence_boundary_current import canonical, digest
from epic16_native import canonicalize, strict_json

EVENTSTORE_CANONICALIZER_SHA256 = '1f9430e33cefaa51757c36f27d57ed509a31d08aa5f35b05a4159b7d806362f9'
CANONICAL_GOLDEN_INPUT = '{"\ue000":1.0,"𐀀":"<é&>"}'
CANONICAL_GOLDEN_OUTPUT = '{"\\uD800\\uDC00":"\\u003C\\u00E9\\u0026\\u003E","\\uE000":1.0}'

MAX_UTC_TICKS = 3155378975999999999


def utc_ticks(value):
    return type(value) is int and 1 <= value <= MAX_UTC_TICKS


def port(value):
    return type(value) is int and 1 <= value <= 65535


STORIES = ('16.1', '16.2', '16.3')
SCENARIOS = tuple(f'AC-{story}-0{n}' for story in STORIES for n in range(1, 6))
TENANT_STATES = ('Healthy', 'Unknown', 'Stale', 'Gapped', 'Corrupt', 'Unavailable')
QUERY_CASES = {
    'ready-empty': ('Current', 'current'), 'v1': ('Rebuilding', 'rebuilding'),
    'Erasing': ('Rebuilding', 'rebuilding'), 'Erased': ('Rebuilding', 'rebuilding'),
    'Rebuilding': ('Rebuilding', 'rebuilding'), 'gap': ('Rebuilding', 'gap_detected'),
    'missing': ('Unavailable', 'unavailable'), 'provider': ('Unavailable', 'unavailable'),
    'corrupt': ('Unavailable', 'metadata_contradictory'),
    'mixed': ('Unavailable', 'metadata_contradictory'),
    'pending-dispatch': ('Rebuilding', 'rebuilding'),
    'surviving-details': ('Unavailable', 'metadata_contradictory'),
    'unauthorized': ('Forbidden', 'forbidden'), 'nonexistent': ('Forbidden', 'forbidden'),
}
TENANT_FAULTS = ('reverse-save', 'equal-sequence-conflict', 'three-future-deliveries',
    'horizon-extension', 'retry-exhaustion', 'lease-expiry', 'restart',
    'missing-history', 'conflicting-history', 'provider-unavailable', 'recovery-success', 'two-replicas')
LIVE_FAULTS = ('route-removal', 'fingerprint-drift', 'duplicate-retry', 'endpoint-race',
               'port-collision', 'unavailable-dapr', 'empty-selection', 'unauthorized')
BINDING_FIELDS = ('tenant', 'conversation', 'eventType', 'sequence', 'serializationFormat',
                  'payloadSha256', 'selectedUtcTicks')


def require(condition: bool, detail: str) -> None:
    if not condition:
        raise domain.PredicateError(detail)


def exact_rows(rows: list, ids: tuple | dict, key: str = 'id') -> dict:
    require(isinstance(rows, list) and len(rows) == len(ids), 'observation inventory cardinality')
    require(all(isinstance(r, dict) and key in r for r in rows), 'observation row shape')
    require([r[key] for r in rows] == list(ids), 'ordered observation inventory')
    return {r[key]: r for r in rows}


def integer(value, minimum=0):
    return type(value) is int and minimum <= value <= domain.MAX_SEQUENCE


def tenant_authorization(rows, tenant):
    by = exact_rows(rows, TENANT_STATES, 'state')
    for state, row in by.items():
        schedules.tenant_access_capture(row, tenant)


def healthy_restarts(rows, tenant):
    for name, row in exact_rows(rows, ('authorized', 'denied')).items():
        schedules.healthy_restart_identity(row, tenant)
        require(row['redeliveries'] == 0 and row['processBefore'] != row['processAfter'], 'healthy restart without redelivery')
        before, after = row['beforeRestart'], row['afterRestart']
        require(domain.same(before, after), 'healthy persisted snapshot survives restart')
        require(before['state'] == 'Healthy' and integer(before['sequence'], 1), 'healthy persisted sequence')
        freshness = before['freshness']
        require(all(utc_ticks(freshness[key]) for key in ('lastAppliedUtcTicks', 'generatedUtcTicks')), 'representable restart freshness')
        require(freshness['generatedUtcTicks'] >= freshness['lastAppliedUtcTicks'] and freshness['lagTicks'] == freshness['generatedUtcTicks'] - freshness['lastAppliedUtcTicks'], 'persisted freshness consistency')
        allowed = name == 'authorized'
        require(before['allowed'] is allowed and before['tenantEnabled'] is allowed, 'healthy persisted access decision')
        for phase in ('Before', 'After'):
            decision = row['consumer' + phase]
            require(decision['process'] == row['process' + phase] and decision['allowed'] is allowed, 'consumer decision from restarted process')
            require(decision['sequence'] == before['sequence'] and decision['freshness'] == freshness, 'consumer sees persisted sequence and freshness')


def acknowledgements(rows, fault_rows):
    by = exact_rows(rows, ('accepted', 'duplicate', 'pending', 'corrupt', 'provider-failure', 'retry-exhaustion'))
    require(len({row['deliveryId'] for row in rows}) == len(rows), 'distinct acknowledgement deliveries')
    records = []
    for name, row in by.items():
        domain.text(row['deliveryId'])
        durable = name in ('accepted', 'duplicate', 'pending', 'corrupt')
        require(row['acknowledged'] is durable and row['durable'] is durable, 'ack only durable terminal outcome')
        require(row['attempts'] == (3 if name == 'retry-exhaustion' else 1), 'three total attempts')
        commit, ack = row['durableCommit'], row['acknowledgement']
        if not durable:
            require(commit is None and ack is None and row['durableEvidence'] is None, 'provider failure and exhausted retries cannot acknowledge')
            schedules.failed_delivery(row, fault_rows)
            continue
        require(row['failureEvidence'] is None, 'durable outcomes carry no failed processing capture')
        require(commit is not None and ack is not None, 'measured durable commit and acknowledgement')
        for operation in (commit, ack):
            domain.text(operation['deliveryId']); domain.text(operation['recordId'])
        require(commit['deliveryId'] == ack['deliveryId'] == row['deliveryId'] and commit['recordId'] == ack['recordId'], 'commit and acknowledgement identify same delivery outcome')
        require(commit['outcome'] == name, 'committed outcome identity')
        require(utc_ticks(commit['utcTicks']) and utc_ticks(ack['utcTicks']) and commit['utcTicks'] <= ack['utcTicks'] and 0 < commit['order'] < ack['order'], 'durable commit before acknowledgement')
        persisted = schedules.tenant_durable_outcome(row)
        lifecycle.commit_before_ack(dict(captureVersion=1, scheduleId=commit['recordId'], commit=commit, acknowledgement=ack), row['deliveryId'], commit['recordId'], persisted, commit['utcTicks'], ack['utcTicks'])
        records.append(commit['recordId'])
    require(len(set(records)) == len(records), 'distinct durable outcome records')


def fingerprints(rows, expected_sequence=None):
    require(bool(rows), 'nonempty fingerprint observations')
    require(len({r['sequence'] for r in rows}) == len(rows), 'unique pending sequence')
    for row in rows:
        require(expected_sequence is None or row['sequence'] > expected_sequence, 'pending sequence is future to exact next')
        require(integer(row['sequence'], 1) and bool(row['messageId']) and bool(row['eventType']), 'immutable event identity')
        require(row['payloadSha256'] == digest(row['persistedPayload'].encode()), 'exact persisted payload fingerprint')


def fault_matrix(rows, expected):
    faults.fault_matrix(rows, expected)


def public_surface(data):
    require(data['beforeContractSha256'] == data['afterContractSha256'] and len(data['beforeContractSha256']) == 64, 'public contract shape')
    require(data['eventPersonalData'] == [] and data['logPersonalData'] == [], 'tenant data must stay absent')
    require(data['genericRuntimeOwner'] == 'EventStore' and data['queryWrites'] == [], 'platform ownership and read purity')
    relationships.isolation(data)
    relationships.tenant_data_safety(data['tenantDataExperiment'])



def reverse_schedules(rows, checkpoint, horizon):
    fields = {'disable': 'tenantEnabled', 'membership': 'member', 'role': 'roleAllowed', 'configuration': 'configurationAllowed'}
    by = exact_rows(rows, fields)
    for name, row in by.items():
        require(row['saveOrder'] == ['newer', 'older'] and row['staleAccepted'] is False, 'reverse-save stale write rejected')
        older, newer = row['older'], row['newer']
        require(older['sequence'] == checkpoint + 1 and newer['sequence'] == horizon, 'reverse-save sequence observations')
        for label, state in (('older', older), ('newer', newer)):
            require(state['state'] == 'Healthy', 'recovered schedule state')
            expected = {field: label == 'older' or field != fields[name] for field in fields.values()}
            require(all(state[field] is value for field, value in expected.items()), 'schedule changes its measured access input')
            require(state['allowed'] is all(state[field] for field in fields.values()), 'decision derived from measured access inputs')
        require(row['replicaABefore'] == older and row['replicaBBefore'] == newer, 'measured interleaved replica states')
        require(row['replicaAAfter'] == row['replicaBAfter'] == newer, 'both replicas converge without regression')



def authorization_responses(data, queries):
    """Executed ListAsync/ConversationListResult.Hidden observations, no alternate public DTO."""
    rows = exact_rows(data['authorizationQueries'], ('unauthorized', 'nonexistent'))
    results = []
    correlations = []
    for name, row in rows.items():
        require(row['captureVersion'] == 1 and type(row['captureVersion']) is int, 'authorization capture version')
        request, response = row['request'], row['response']
        query = request['query']
        domain.text(query['callerPrincipalId']); domain.text(query['correlationId'])
        require(request['executed'] is True and response['executed'] is True and request['method'] == 'ConversationQueryHandler.ListAsync', 'executed public query and response')
        require(request['tenant'] == response['tenant'] == query['tenantId'] == data['tenant']
                and response['correlationId'] == query['correlationId'], 'same tenant and query response')
        require(integer(request['order'], 1) and integer(response['order'], 1) and request['order'] < response['order'], 'request precedes response')
        require(row['authorized'] is queries[name]['authorized'] and row['tenantExists'] is queries[name]['tenantExists'], 'authorization input correlation')
        require(row['indexReads'] == [] and row['writes'] == [], 'authorization rejects before index access without repair')
        require(type(query['schemaVersion']) is int and query['schemaVersion'] == 1, 'supported query version')
        require(query['filter'] == dict.fromkeys(('businessReference','projectId','folderId','lifecycleState','projectedAtFrom','projectedAtTo','recentActivityAfter','participantPartyId','redactionState','freshnessState','auditReadiness','verificationState'))
                and domain.same(query['page'], dict(pageSize=25, continuationCursor=None)), 'controlled native default list request')
        expected = dict(schemaVersion=query['schemaVersion'], freshnessState='Forbidden', reasonCode='forbidden', conversations=[],
                        page=dict(returnedCount=0, continuationCursor=None), safeNextAction='No accessible matches.')
        require(domain.same(response['result'], expected), 'actual public response is non-disclosing Hidden result')
        results.append(response['result']); correlations.append(query['correlationId'])
    require(domain.same(results[0], results[1]) and len(set(correlations)) == 2, 'separate authorization experiments have identical non-disclosing results')

def query_case(row):
    query_domains(row)
    if not row['tenantExists']: return 'nonexistent'
    if not row['authorized']: return 'unauthorized'
    if row['providerState'] != 'Available': return 'provider'
    if not row['indexPresent']: return 'missing'
    if row['corrupt']: return 'corrupt'
    if any(g != row['generation'] for g in row['controlMetadata'].values()): return 'mixed'
    if row['sourceGap']: return 'gap'
    if row['indexVersion'] == 1: return 'v1'
    if row['lifecycle'] in ('Erasing', 'Erased', 'Rebuilding'): return row['lifecycle']
    if row['pendingDispatch']: return 'pending-dispatch'
    if row['survivingDetails'] and not row['summaries']: return 'surviving-details'
    return 'ready-empty'


def query_mapping(row):
    query_domains(row)
    if not row['tenantExists'] or not row['authorized']: return ('Forbidden', 'forbidden')
    if row['providerState'] != 'Available' or not row['indexPresent']: return ('Unavailable', 'unavailable')
    if row['corrupt']: return ('Unavailable', 'metadata_contradictory')
    require(set(row['componentGenerations'])==set(row['controlMetadata'].values()), 'generation summary derives from named fields inside the same control record')
    internal = lifecycle.generation_mapping(row['generation'], row['controlMetadata'].values(), [])
    if internal: return internal
    if row['sourceGap']: return ('Rebuilding', 'gap_detected')
    if row['indexVersion'] == 1 or row['lifecycle'] in ('Erasing', 'Erased', 'Rebuilding') or row['pendingDispatch']: return ('Rebuilding', 'rebuilding')
    if row['survivingDetails'] and not row['summaries']: return ('Unavailable', 'metadata_contradictory')
    require(row['indexVersion'] == 2 and row['lifecycle'] == 'Ready' and not row['rebuildPending'] and not row['summaries'] and not row['pendingDispatch'] and not row['survivingDetails'], 'proven current-empty control index')
    return ('Current', 'current')


def query_domains(row):
    require(row['providerState'] in ('Available', 'Unavailable'), 'known query provider state')
    require(row['lifecycle'] in ('Absent', 'Ready', 'Erasing', 'Erased', 'Rebuilding'), 'known query lifecycle')


TRANSITIONS = {
    'tenant-created': ('Absent', 'TenantCreated', 'Ready', 0, 2),
    'absent-rebuild': ('Absent', 'AuthoritativeDiscovery', 'Rebuilding', 0, 2),
    'v1-rebuild': ('Ready', 'VersionUpgrade', 'Rebuilding', 1, 2),
    'erase-start': ('Ready', 'EraseIntent', 'Erasing', 2, 2),
    'erase-complete': ('Erasing', 'DeletionComplete', 'Erased', 2, 2),
    'erased-delivery': ('Erased', 'Delivery', 'Erased', 2, 2),
    'erased-tenant-created': ('Erased', 'TenantCreated', 'Erased', 2, 2),
    'unauthorized-rebuild': ('Erased', 'RebuildIntent', 'Erased', 2, 2),
    'owner-rebuild': ('Erased', 'RebuildIntent', 'Rebuilding', 2, 2),
    'rebuild-complete': ('Rebuilding', 'ManifestComplete', 'Ready', 2, 2),
}


def lifecycle_transitions(rows):
    for name, row in exact_rows(rows, TRANSITIONS).items():
        require(tuple(row[k] for k in ('before', 'cause', 'after', 'beforeVersion', 'afterVersion')) == TRANSITIONS[name], 'preserved lifecycle cause and state')
        require(row['ownerAuthorized'] is (name == 'owner-rebuild'), 'explicit owner authorization to leave Erased')
        require(row['initializedEmpty'] is (name == 'tenant-created'), 'only TenantCreated plus Absent initializes empty')
        require(row['manifestComplete'] is (name == 'rebuild-complete'), 'rebuild completes only with manifest')


def erase_deliveries(row):
    require(row['barrier'] >= 0 and bool(row['tenant']), 'captured erase barrier')
    targets = row['deletionTargets']
    require(targets and len({x['key'] for x in targets}) == len(targets), 'unique deletion targets')
    require(all(x['tenant'] == row['tenant'] and x['generation'] == row['beforeGeneration'] for x in targets), 'tenant retiring-generation deletion targets')
    require(row['deletedTargets'] == targets, 'exact captured deletion manifest')
    relationships.erase_targets(row)
    deliveries = row['deliveries']
    require([x['lifecycle'] for x in deliveries] == ['Erasing', 'Erased'], 'deliveries during both erased phases')
    require(row['durableReferences'] == len(deliveries), 'durable reference inventory')
    require(len({x['deliveryId'] for x in deliveries}) == len(deliveries), 'unique delivery identities')
    lifecycle.erase_reference_identity(deliveries)
    for delivery in deliveries:
        reference = delivery['reference']
        require(reference == delivery['durableReference'], 'persist exact authoritative non-payload reference')
        require(reference['tenant'] == row['tenant'] and reference['stream'] and reference['sequence'] > 0 and reference['position'] > row['barrier'], 'authoritative delivery location')
        require(reference['generation'] == row['afterGeneration'] and reference['messageId'] == delivery['deliveryId'], 'generation-bound delivery identity')
        require(utc_ticks(delivery['durableAt']) and utc_ticks(delivery['acknowledgedAt']) and delivery['durableAt'] <= delivery['acknowledgedAt'], 'durable reference precedes acknowledgement')
        lifecycle.commit_before_ack(delivery['operations'], delivery['deliveryId'], delivery['deliveryId'], reference, delivery['durableAt'], delivery['acknowledgedAt'])
        require(delivery['derivedBefore'] == delivery['derivedAfter'] == [], 'erase deliveries cannot recreate content')


def rebuild_streams(row):
    streams = row['streams']
    inventory = row['manifestExpected']
    require(inventory and len(set(inventory)) == len(inventory) and sorted(x['id'] for x in streams) == sorted(inventory), 'exact unique authoritative stream manifest')
    require(row['discoveredStreams'] == inventory and row['discoveryAt'] == row['B'], 'authoritative discovery at captured B')
    require(row['progressGenerationBefore'] == row['progressGenerationAfter'] == row['afterGeneration'], 'same-generation progress merge')
    lifecycle.stream_boundaries(row)
    for stream in streams:
        require(stream['start'] == 0, 'full authoritative replay includes creation prefix without a checkpoint snapshot')
        require(stream['tenant'] == row['tenant'] and stream['authoritative'] is True and stream['boundaryAt'] == row['B'] and stream['capturedThrough'] == row['C'], 'tenant-scoped authoritative B/C boundaries')
        require(stream['start'] < stream['boundarySequence'] <= stream['end'], 'per-stream sequence boundary at B')
        applied = stream['applied']
        require(stream['end'] > stream['start'] and len(applied) == stream['end'] - stream['start'] and all(value == stream['start'] + offset for offset, value in enumerate(applied, 1)), 'exact-next aggregate stream')
        require(stream['replayedThroughB'] == stream['boundarySequence'] and stream['replayedThroughC'] == stream['end'], 'complete replay through B then captured C')


def pending_proof(data):
    before, _ = domain.conversation_replay(data['precedingEvents'])
    binding, identity, payload = domain.conversation_envelope(data['pendingEvent'])
    require(data['pendingBinding'] == binding and data['pendingSequence'] == binding['sequence'] == len(data['precedingEvents'])+1, 'pending full binding and sequence')
    require(before['tenantId'] == data['tenant'] == binding['tenant'] and before['conversationId'] == binding['conversation'], 'same tenant conversation pending recovery')
    require(domain.same(data['beforeProjection'], before), 'retained preceding domain state derives from history')
    return before, binding


def evaluate(scenario: str, data: dict) -> list[str]:
    """Evaluate a closed scenario and return the independently evaluated proof names."""
    require(scenario in SCENARIOS and isinstance(data, dict), 'unknown scenario or observations')
    checks = []
    def checked(name, action):
        action(); checks.append(name)
    story, number = scenario[3:].rsplit('-', 1)
    if story == '16.1':
        if number == '01':
            checked('fail-closed-states', lambda: tenant_authorization(data['authorization'], data['isolation']['tenants'][0]))
            rows = exact_rows(data['transitions'], ('next', 'duplicate', 'conflict', 'older-match', 'older-missing', 'older-conflict', 'older-unavailable'))
            for key, outcome in zip(rows, ('Healthy', 'Healthy', 'Corrupt', 'Healthy', 'Corrupt', 'Corrupt', 'Unavailable')):
                r = rows[key]
                require(r['state'] == outcome, 'sequence transition state')
                require(r['afterSequence'] == r['beforeSequence'] + (1 if key == 'next' else 0), 'no old event reapplication')
                require(r['projectionVersion'] == r['checkpointVersion'] and integer(r['projectionVersion'], 1), 'atomic projection/checkpoint CAS')
                require(r['applied'] is (key == 'next'), 'duplicate suppression follows verification')
            require(data['storeOwner'] == 'EventStore' and data['transitionOwner'] == 'Tenants' and data['consumerOwner'] == 'Conversations', 'seam ownership')
            relationships.isolation(data)
            relationships.tenant_transitions(data['transitions'])
            checks += ['atomic-sequence-identity', 'tenant-segmentation', 'seam-ownership']
        elif number == '02':
            require(domain.same(data['beforeRestart'], data['afterRestart']), 'durable restart snapshot equality')
            snap = data['afterRestart']; domain.sequence(snap['lastContiguousSequence'], checkpoint=True); domain.sequence(snap['expectedSequence']); domain.sequence(snap['recoveryThrough']); fingerprints(snap['pending'], snap['expectedSequence'])
            for snapshot in (data['beforeRestart'], data['afterRestart']):
                require(snapshot['tenant'] == data['tenant'], 'restart snapshot belongs to measured tenant')
                for record in snapshot['pending']:
                    relationships.tenant_record(record, data['tenant'])
            require(snap['state'] == 'Gapped' and snap['authorized'] is False, 'pending restart denies access')
            tenant_authorization(data['authorization'], data['tenant'])
            healthy_restarts(data['healthyRestarts'], data['tenant'])
            require(data['redeliveries'] == 0 and data['processBefore'] != data['processAfter'], 'restart without redelivery')
            require(snap['expectedSequence'] == snap['lastContiguousSequence'] + 1 and snap['recoveryThrough'] >= max(r['sequence'] for r in snap['pending']), 'durable gap horizon')
            schedules.restart_resumption(data)
            checks += ['durable-restart', 'pending-fingerprints', 'fail-closed-restart', 'fenced-resumption']
        elif number == '03':
            rows = data['merges']; require(len(rows) >= 3, 'three future deliveries')
            require(len({r['deliveredSequence'] for r in rows}) >= 3, 'three distinct interleaved future deliveries')
            horizon = rows[0]['beforeHorizon']; pending = {}
            expected = rows[0]['expectedSequence']
            require(expected == data['checkpoint'] + 1, 'gap begins at exact next sequence')
            schedules.future_deliveries(data)
            schedules.horizon_extension(data)
            for r in rows:
                require(r['deliveredSequence'] > expected and r['expectedSequence'] == expected and r['beforeHorizon'] == horizon, 'immutable gap origin')
                horizon = max(horizon, r['deliveredSequence'])
                require(r['afterHorizon'] == horizon, 'monotonic max horizon')
                fingerprints(r['pending'], expected)
                now = {x['sequence']: x for x in r['pending']}
                require(all(sequence <= horizon for sequence in now), 'every pending sequence fits recovery horizon')
                require(all(now.get(k) == v for k, v in pending.items()), 'retain every pending fingerprint')
                require(r['deliveredSequence'] in now, 'persist each future delivery')
                pending = now
            require(data['replicaA'] == data['replicaB'] and data['replicaA']['state'] == 'Healthy' and data['replicaA']['tenantEnabled'] is False and data['replicaA']['allowed'] is False, 'replica convergence')
            require(data['replicaA']['sequence'] >= horizon and data['pendingAtHealthy'] == [], 'recover complete enlarged horizon')
            recovery = data['recovery']
            require(recovery['state'] == 'Gapped' and recovery['allowed'] is False and recovery['lease'] > 0 and recovery['version'] > 0, 'measured recovery lease/version')
            require(recovery['checkpoint'] == data['replicaA']['sequence'] == horizon and recovery['horizon'] == horizon, 'measured recovered checkpoint and enlarged horizon')
            measured_cas = {key: recovery[key] for key in ('lease', 'version', 'checkpoint', 'horizon')}
            require(data['staleWorkerWrites'] == 0 and data['healthyCas']['expected'] == data['healthyCas']['observed'] == measured_cas, 'fenced completion CAS')
            recovered, states = domain.tenant_replay(data['startingState'], data['authoritativeReplay'], data['checkpoint'], horizon)
            for merge in rows:
                for item in merge['pending']:
                    require(item == data['authoritativeReplay'][item['sequence']-data['checkpoint']-1], 'pending fingerprints match authoritative replay')
            require(data['recoveredState'] == recovered, 'recovered tenant domain values')
            derived = domain.tenant_decision(recovered, horizon)
            require(data['replicaA'] == {key: derived[key] for key in data['replicaA']}, 'access derived from authoritative tenant events')
            reverse_schedules(data['reverseSaves'], data['checkpoint'], horizon)
            for schedule in data['reverseSaves']:
                schedules.reverse_save_execution(data, schedule)
                end, history = domain.tenant_replay(data['startingState'], schedule['authoritativeReplay'], data['checkpoint'], horizon)
                require(schedule['older'] == domain.tenant_decision(history[data['checkpoint']+1], data['checkpoint']+1) and schedule['newer'] == domain.tenant_decision(end, horizon), 'reverse-save decisions derive from actual event semantics')
            require(data['replicaA'] == {key: data['reverseSaves'][0]['replicaAAfter'][key] for key in data['replicaA']}, 'disable schedule binds recovered replica')
            require(set(data['healthyCas']['expected']) == {'lease', 'version', 'checkpoint', 'horizon'}, 'complete fence tuple')
            require(data['deliveryCopies'] == [1] * len(rows), 'single consumer-group delivery')
            checks += ['monotonic-horizon', 'immutable-pending-evidence', 'two-replica-convergence', 'fenced-cutover']
        elif number == '04':
            expected = dict(zip(TENANT_FAULTS, ('CAS_RETRY', 'Corrupt', 'Gapped', 'Gapped', 'Unavailable', 'FENCE_REJECTED', 'Gapped', 'Corrupt', 'Corrupt', 'Unavailable', 'Healthy', 'Healthy')))
            fault_matrix(data['faults'], expected)
            acknowledgements(data['acknowledgements'], data['faults'])
            require(data['queryRecoveryOperations'] == [] and data['consumerClears'] == [], 'queries and consumer cannot recover')
            checks += ['tenant-fault-matrix', 'durable-acknowledgement', 'bounded-retry', 'query-purity']
        else:
            public_surface(data)
            require(data['restartReceipt'] == 'AC-16.1-02' and data['replicaReceipt'] == 'AC-16.1-03', 'warning gated by both proofs')
            require(data['warningRemoved'] is True, 'single replica warning removal')
            checks += ['additive-contracts', 'tenant-safety', 'platform-ownership', 'warning-proof-dependencies']
    elif story == '16.2':
        if number == '01':
            rows = exact_rows(data['events'], ('contract', 'position-only'))
            for name, r in rows.items():
                authoritative, selected_id, payload = domain.conversation_envelope(r['authoritativeEnvelope'])
                require(r['binding'] == authoritative and r['selectedId'] == selected_id and r['persistedPayload'] == r['authoritativeEnvelope']['payload'], 'full binding matches independent authoritative envelope')
                require((name == 'contract') is ('metadata' in payload), 'contract versus position-only metadata')
                require(r['messageId'] == r['authoritativeEnvelope']['messageId'] and r['timestampTicks'] == r['authoritativeEnvelope']['timestampUtcTicks'], 'authoritative transport identity/time')
                if name == 'contract': require(r['eventId'] == payload['metadata']['eventId'] and r['occurredAtTicks'] == authoritative['selectedUtcTicks'], 'actual contract identity/time')
                require(set(r['binding']) == set(BINDING_FIELDS), 'full duplicate binding')
                require(r['binding']['payloadSha256'] == digest(r['persistedPayload'].encode()), 'raw payload bytes')
                require(utc_ticks(r['binding']['selectedUtcTicks']) and r['binding']['selectedUtcTicks'] == r['occurredAtTicks' if name == 'contract' else 'timestampTicks'], 'selected authoritative time')
                require(r['selectedId'] == r['eventId' if name == 'contract' else 'messageId'] and domain.nonblank(r['selectedId']), 'selected authoritative identity')
                require(r['duplicateApplied'] is False and r['validatedBeforeDeduplication'] is True, 'validate before deduplication')
            conflicts = exact_rows(data['conflicts'], (*BINDING_FIELDS, 'missing-id', 'invalid-time'))
            relationships.conflicts(list(conflicts.values()))
            relationships.transport_time(data['transportTimeProof'])
            require(data['clockReads'] == 0 and data['envelopeTimeDifferenceState'] == 'Healthy', 'no clock substitution or envelope corruption')
            checks += ['selected-identity-time', 'full-duplicate-binding', 'pre-dedup-validation', 'no-runtime-clock']
        elif number == '02':
            rows = data['rebuilds']; require(len(rows) == 2, 'two rebuilds')
            require(rows[0]['clockTicks'] != rows[1]['clockTicks'], 'different rebuild clocks')
            expected_state, event_ticks = domain.conversation_replay(data['authoritativeEvents'])
            expected_items = domain.collection_items(expected_state)
            require(data['contributingEventTicks'] == event_ticks, 'contributing times derive from authoritative events')
            require(data['collectionKeys'] == [item['key'] for item in expected_items], 'keys derive from complete domain values')
            for r in rows:
                value = strict_json(r['canonicalJson'].encode())
                require(isinstance(value, dict) and set(value) == {'version', 'anchorUtcTicks', 'items'} and type(value['version']) is int and value['version'] == 2, 'closed version 2 replay state')
                require(domain.same(value['items'], expected_items), 'serialized domain values match authoritative replay')
                require(utc_ticks(value['anchorUtcTicks']) and value['anchorUtcTicks'] == r['anchorTicks'], 'hashed replay anchor matches measured state')
                require(all(utc_ticks(tick) for tick in data['contributingEventTicks']) and all(utc_ticks(r[key]) for key in ('clockTicks', 'anchorTicks', 'generatedTicks', 'lastAppliedTicks', 'completionTicks')), 'representable authoritative replay time')
                require(canonicalize(r['canonicalJson']) == r['canonicalJson'] and canonicalize(r['roundTripJson']) == r['roundTripJson'], 'actual EventStore canonical bytes')
                # EventStore owns serialization; Python's evidence JSON encoder is not its oracle.
                require(data['canonicalizerType'] == 'Hexalith.EventStore.Client.Projections.ReadModelBatchCanonicalJson' and data['canonicalizerSourceSha256'] == EVENTSTORE_CANONICALIZER_SHA256, 'bound EventStore canonicalizer')
                require(data['goldenInput'] == CANONICAL_GOLDEN_INPUT and data['goldenOutput'] == CANONICAL_GOLDEN_OUTPUT and canonicalize(data['goldenInput']) == data['goldenOutput'], 'EventStore Unicode/HTML/ordinal/number golden vector')
                require(r['stateHash'] == digest(r['canonicalJson'].encode()), 'canonical replay hash')
                require(r['roundTripJson'] == r['canonicalJson'], 'canonical round trip')
                require(r['anchorTicks'] == max(data['contributingEventTicks']), 'maximum validated replay anchor')
                require(r['generatedTicks'] >= r['anchorTicks'] and r['lagTicks'] == r['generatedTicks'] - r['anchorTicks'], 'observation-time freshness')
                require(r['lastAppliedTicks'] == r['anchorTicks'] and r['generatedTicks'] == r['completionTicks'], 'actual completion instant')
                require(not set(value) & {'projectionGeneratedAt', 'etag', 'ttl', 'batchId', 'batchFingerprint', 'freshness'}, 'operational metadata excluded')
            require(data['collectionKeys'] == sorted(set(data['collectionKeys']), key=lambda value: value.encode('utf-16-be')) and len(data['collectionKeys']) >= 2, 'ordinal unique collection keys')
            require(rows[0]['canonicalJson'] == rows[1]['canonicalJson'] and rows[0]['anchorTicks'] == rows[1]['anchorTicks'], 'deterministic replay')
            execution.replay_rejections(data)
            checks += ['canonical-replay-bytes', 'hash-and-roundtrip', 'event-derived-anchor', 'observation-freshness']
        elif number == '03':
            rows = exact_rows(data['queries'], QUERY_CASES)
            for name, r in rows.items():
                expected = query_mapping(r)
                require((r['status'], r['reason']) == expected == QUERY_CASES[name], 'safe measured query mapping')
                require(query_case(r) == name, 'case identity reflects measured control index')
                require(r['writes'] == 0 and r['indexReads'] == (0 if name in ('unauthorized', 'nonexistent') else 1), 'no repair on read and measured index access')
                require(r['rebuildPending'] is (name in ('v1', 'Rebuilding')), 'measured rebuild state')
                if name in ('unauthorized', 'nonexistent'): require(r['indexReads'] == 0, 'non-disclosing tenant authorization')
            authorization_responses(data, rows)
            lifecycle_transitions(data['transitions'])
            empty = rows['ready-empty']
            require(empty['indexVersion'] == 2 and empty['summaries'] == [] and empty['pendingDispatch'] == [], 'proven current empty')
            require(data['authorityKey'] == relationships.index_key(data['tenant']) and data['authorityCount'] == 1, 'sole control index')
            checks += ['query-mapping-matrix', 'current-empty-proof', 'authorization-before-read', 'sole-control-index']
        elif number == '04':
            rows = exact_rows(data['lifecycle'], ('erase', 'rebuild'))
            for r in rows.values():
                require(r['afterGeneration'] == r['beforeGeneration'] + 1, 'generation increment')
                lifecycle.lifecycle_transaction(r)
                require(r['planAuthor'] == 'ConversationProjectionReadModelWriter' and r['executor'] == 'EventStore', 'single writer/CAS executor')
                require(r['lifecycleCasVersion'] == r['indexCasVersion'] and r['staleWrites'] == 0, 'one CAS boundary and stale rejection')
            erase = rows['erase']
            require(erase['deletedGenerations'] and set(erase['deletedGenerations']) == {erase['beforeGeneration']}, 'delete only retired generation')
            require(erase['tombstoneRetained'] is True and erase['quarantinedPayloads'] == [] and erase['durableReferences'] > 0, 'durable non-payload quarantine')
            erase_deliveries(erase)
            rebuild = rows['rebuild']
            require(rebuild['B'] <= rebuild['C'] and rebuild['drainedThrough'] == rebuild['C'], 'captured B/C cutover')
            require(rebuild['manifestExpected'] == rebuild['manifestCompleted'] and rebuild['manifestCompleted'], 'complete rebuild manifest')
            require(rebuild['watermarkAfter'] == max(rebuild['watermarkBefore'], rebuild['C']) and bool(rebuild['queuedAfterC']) and all(x > rebuild['C'] for x in rebuild['queuedAfterC']), 'retain later progress')
            rebuild_streams(rebuild)
            lifecycle.catch_up_queue(rebuild)
            for row in rows.values(): lifecycle.lifecycle_queries(row)
            require(rebuild['streams'] and data['queryWrites'] == [] and data['unauthorizedRebuildWrites'] == 0, 'no read repair or unauthorized rebuild')
            checks += ['writer-CAS-boundary', 'generation-fences', 'erase-quarantine', 'authoritative-rebuild-cutover']
        else:
            public_surface(data)
            relationships.redaction(data['redactionProof'])
            lifecycle.cross_key_queries(data['generationQueries'])
            require(data['mixedGenerationResult'] == 'Rebuilding' and data['canonicalReceipt'] == 'AC-16.2-02' and data['queryReceipt'] == 'AC-16.2-03', 'replay/query conformance dependencies')
            checks += ['public-surface', 'tenant-isolation', 'generation-mismatch', 'replay-query-dependencies']
    else:
        if number == '01':
            faults.readiness(data)
            require(data['resourceHealth'] == 'Healthy' and data['connectionSucceeded'] is False, 'healthy resource unavailable endpoint')
            require(data['code'] == 'APPHOST_ENDPOINT_NOT_READY' and bool(data['endpoint']), 'stable endpoint diagnostic')
            require(integer(data['attempts'], 1) and data['attempts'] <= data['attemptLimit'] and 0 < data['elapsedMs'] <= data['timeoutMs'], 'bounded readiness')
            require(data['liveLaneStarted'] is False and data['diagnosticPayloadFields'] == [], 'preflight precedes safe live lane')
            checks += ['effective-endpoint', 'bounded-preflight', 'stable-diagnostic', 'content-safe']
        elif number == '02':
            faults.port_collision(data)
            require(all(port(value) for value in [data['occupiedPort'], data['unrelatedPort'], *data['effectivePorts'], *data['portsBefore'], *data['portsAfter']]), 'valid endpoint port domain')
            require(data['occupiedPort'] in data['effectivePorts'] and data['unrelatedPort'] not in data['effectivePorts'], 'effective binding inventory')
            require(data['code'] == 'DAPR_CONTROL_PLANE_PORT_COLLISION' and data['unrelatedCode'] is None and bool(data['endpoint']), 'specific collision classification')
            require(data['portsBefore'] == data['portsAfter'] and data['laneStarted'] is False, 'port restoration before lane')
            checks += ['effective-port', 'unrelated-negative', 'bounded-preflight', 'port-restoration']
        elif number == '03':
            pending_proof(data)
            relationships.catalog(data)
            persisted = dict(location=data['pendingLocation'], binding=data['pendingBinding'])
            lifecycle.commit_before_ack(data['pendingOperations'], data['pendingEvent']['messageId'], data['persistedPendingId'], persisted, data['persistedAt'], data['acknowledgedAt'])
            require(data['transport'] == 'production-delivery' and data['coordinator'] == 'EventStore' and data['handlerInvocations'] == 0, 'production hosted delivery')
            require(data['pendingBefore'] == 0 and data['pendingAfter'] == 1 and data['persistedPendingId'], 'durable first failure')
            require(utc_ticks(data['acknowledgedAt']) and utc_ticks(data['persistedAt']) and data['acknowledgedAt'] >= data['persistedAt'] and data['tenantAllowed'] is False, 'durable ack and fail closed tenant')
            require(data['route'] == 'project/v2/reconcile' and len(data['catalogSha256']) == 64 and data['runtimeId'] and data['tenant'], 'runtime route catalog binding')
            require(data['queryRecoveryCalls'] == 0 and data['generationMatches'] is True, 'no bypass of lifecycle')
            checks += ['production-delivery', 'durable-pending', 'ack-ordering', 'runtime-route-binding']
        elif number == '04':
            before, pending = pending_proof(data)
            relationships.catalog(data)
            require(data['beforeCheckpoint'] == len(data['precedingEvents']), 'preceding state checkpoint')
            recovered, _ = domain.conversation_replay(data['recoveryEvents'], before=before, checkpoint=data['beforeCheckpoint'])
            require(data['recoveryEvents'][0] == data['pendingEvent'] and data['afterCheckpoint'] == data['beforeCheckpoint'] + len(data['recoveryEvents']), 'recovery begins with correlated pending event')
            require(domain.same(data['persistedProjection'], dict(sequence=data['afterCheckpoint'], state=recovered)), 'corrected domain values from authoritative recovery')
            require(data['wireRoute'] == 'project/v2/reconcile' and data['authenticated'] is True and data['coordinator'] == 'EventStore', 'authenticated production wire request')
            require(data['pendingBefore'] == 1 and data['pendingAfter'] == 0 and data['clearCount'] == 1 and data['duplicateClearCount'] == 0, 'durable idempotent clearance')
            require(domain.same(data['persistedProjection'], data['queryProjection']) and bool(data['queryProjection']), 'persisted end state visible through query')
            require(data['persistedProjection']['sequence'] == data['afterCheckpoint'] and data['afterCheckpoint'] >= data['pendingSequence'], 'corrected projection covers recovered checkpoint and pending item')
            require(data['afterCheckpoint'] >= data['beforeCheckpoint'] and data['afterGeneration'] >= data['beforeGeneration'] and data['afterWatermark'] >= data['beforeWatermark'], 'no newer-progress regression')
            require(data['staleWorkerWrites'] == 0 and data['directHandlerCalls'] == 0, 'hosted fenced recovery')
            require(all(data[key] for key in ('persistedPendingId', 'tenant', 'runtimeId', 'catalogSha256')), 'clearance measured identity')
            require(data['pendingReceipt'] == 'AC-16.3-03', 'first failure dependency')
            schedules.reconciliation(data)
            execution.reconciliation_wire(data)
            checks += ['production-wire-route', 'durable-idempotent-clearance', 'query-visible-endstate', 'newer-progress-fences']
        else:
            expected = dict(zip(LIVE_FAULTS, ('ROUTE_UNAVAILABLE', 'ROUTE_FINGERPRINT_DRIFT', 'DUPLICATE_NOOP', 'APPHOST_ENDPOINT_NOT_READY', 'DAPR_CONTROL_PLANE_PORT_COLLISION', 'PROVIDER_UNAVAILABLE', 'EMPTY_TEST_SELECTION', 'Forbidden')))
            fault_matrix(data['faults'], expected)
            faults.cancellation(data)
            require(data['cancelBoundMs'] == faults.CONTROL_POLICY['cancelBoundMs'] and 0 <= data['cancelElapsedMs'] <= faults.CONTROL_POLICY['cancelBoundMs'], 'pinned cancellation control')
            readiness_attempts = next(row for row in data['faults'] if row['id'] == 'endpoint-race')['observed']['attempts']
            readiness_elapsed = next(row for row in data['faults'] if row['id'] == 'endpoint-race')['observed']['elapsedMs']
            faults.diagnostics(data['diagnostics'], data['endpointInventory'], [('APPHOST_ENDPOINT_NOT_READY', 'conversations-http', readiness_attempts), ('DAPR_CONTROL_PLANE_PORT_COLLISION', 'dapr-placement', 1)], elapsed=readiness_elapsed)
            collision = next(row for row in data['faults'] if row['id'] == 'port-collision')
            collision_port = strict_json(collision['beforeBytes'].encode())['state']['port']
            require(collision_port == faults.endpoint_inventory(data['endpointInventory'])['dapr-placement']['port'], 'fault collision uses effective endpoint port')
            require(data['portsBefore'] == data['portsAfter'] == [collision_port], 'cleanup inventory names measured affected port')
            require(data['environmentOutcome'] == 'BLOCKED' and data['productOutcome'] == 'FAIL', 'environment/product distinction')
            require(all(port(value) for value in [*data['portsBefore'], *data['portsAfter']]), 'valid restored port domain')
            require(data['portsBefore'] == data['portsAfter'] and data['cancelElapsedMs'] <= data['cancelBoundMs'] and data['diagnosticPayloadFields'] == [], 'bounded safe cleanup')
            require(data['staleWorkerWrites'] == 0 and data['unauthorizedWrites'] == 0, 'fences and authorization')
            checks += ['live-fault-matrix', 'environment-distinction', 'safe-cleanup', 'fenced-authorization']
    return checks
