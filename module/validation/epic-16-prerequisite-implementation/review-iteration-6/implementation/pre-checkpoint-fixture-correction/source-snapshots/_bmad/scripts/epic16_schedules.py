"""Closed measured schedules for the controlled Epic 16 evidence experiment.

These versioned capture DTOs describe observations; they do not implement a
projection store, worker, transport, or durable provider.
"""
from copy import deepcopy

import epic16_domain as domain
from epic16_domain import require, same, shape, text
from epic16_native import strict_json
from evidence_boundary_current import canonical, digest
from epic16_relationships import tenant_record


def future_deliveries(data):
    rows = data['futureSchedules']
    require([row['id'] for row in rows] == ['duplicate', 'conflict'], 'exact future repeat/conflict schedules')
    tenant = data['startingState']['tenantId']
    pending = data['merges'][-1]['pending']
    horizon = data['merges'][-1]['afterHorizon']
    baseline = rows[0]['before']
    for row in rows:
        before, after, cas = row['before'], row['after'], row['cas']
        require(same(before, baseline), 'same composite gap for both controlled future schedules')
        for snapshot in (before, after):
            domain.sequence(snapshot['version']); text(snapshot['etag'])
            require(snapshot['tenant'] == tenant and snapshot['projection']['tenantId'] == tenant, 'future schedule tenant binding')
        require(before['captureVersion'] == 1 and before['checkpoint'] == data['checkpoint'] and before['expectedSequence'] == data['checkpoint']+1 and before['horizon'] == horizon, 'future schedule exact checkpoint and horizon')
        require(same(before['pending'], pending) and same(before['projection'], data['startingState']) and before['health'] == 'Gapped' and before['allowed'] is False, 'future schedule binds complete gap inventory and projection')
        delivered = tenant_record(row['delivered'], tenant)
        require(before['expectedSequence'] < delivered['sequence'] <= horizon, 'repeated delivery remains future in measured gap')
        originals = [record for record in pending if record['sequence'] == delivered['sequence']]
        require(len(originals) == 1, 'future schedule addresses an existing pending sequence')
        original = tenant_record(originals[0], tenant)
        duplicate = row['id'] == 'duplicate'
        require(same(delivered, original) is duplicate, 'actual identical or conflicting native pending fingerprint')
        require(cas['expectedEtag'] == before['etag'] and cas['expectedVersion'] == before['version'], 'future CAS binds expected opaque etag and version')
        expected = deepcopy(before)
        commit_fields = ('commitId', 'projectionCommitId', 'checkpointCommitId', 'healthCommitId', 'pendingCommitId')
        if duplicate:
            require(cas['succeeded'] is None and all(cas[key] is None for key in commit_fields), 'identical future delivery causes no commit')
        else:
            expected.update(health='Corrupt', etag=after['etag'], version=before['version']+1)
            require(after['etag'] != before['etag'] and cas['succeeded'] is True and all(cas[key] == text(cas['commitId']) for key in commit_fields), 'future conflict durably CAS-marks composite corrupt state')
        require(same(after, expected), 'future delivery preserves pending evidence and projection/checkpoint atomically')
        require(data['duplicateFutureState' if duplicate else 'conflictingFutureState'] == after['health'], 'future outcome derives from persisted health')


def healthy_restart_identity(row, tenant):
    """Logical ITenantProjectionStore tenant key, not an invented physical key."""
    before, after = row['beforeRestart'], row['afterRestart']
    require(before['captureVersion'] == 1 and before['tenant'] == tenant == before['logicalStoreKey'], 'healthy capture binds exact tenant logical key')
    require(row['startingState']['tenantId'] == tenant, 'healthy restart source state belongs to tenant')
    state, _ = domain.tenant_replay(row['startingState'], row['sourceEvents'], row['sourceCheckpoint'], before['sequence'])
    decision = domain.tenant_decision(state, before['sequence'])
    require(same(before['domain'], state), 'healthy persisted full domain state derives from retained events')
    require(before['allowed'] is decision['allowed'] and before['tenantEnabled'] is decision['tenantEnabled'] and decision['allowed'] is (row['id'] == 'authorized'), 'healthy decision derives from membership role configuration and status')
    for phase, snapshot in (('Before', before), ('After', after)):
        raw = row['snapshot'+phase+'Bytes']; sha = digest(raw.encode())
        require(same(strict_json(raw.encode()), snapshot) and sha == row['snapshot'+phase+'Sha256'], 'raw immutable healthy snapshot capture')
        lookup = row['lookup'+phase]
        require(lookup == dict(process=row['process'+phase], tenantId=tenant, logicalKey=tenant, snapshotSha256=sha), 'same-tenant projection-store lookup and returned snapshot')
        consumer = row['consumer'+phase]
        require(consumer['tenantId'] == tenant and consumer['userId'] == 'actor-a' and text(consumer['requestId']) == row['consumerBefore']['requestId'], 'same-tenant consumer request identity')
    require(row['snapshotBeforeBytes'] == row['snapshotAfterBytes'], 'restart retains exact persisted snapshot bytes')


SCOPE_FIELDS = ('pendingId', 'tenant', 'conversation', 'runtimeId', 'catalogSha256', 'catalogFingerprint')
FENCE_FIELDS = ('lease', 'version', 'etag', 'generation', 'checkpoint', 'watermark')
STATE_FIELDS = ('captureVersion', 'scope', *FENCE_FIELDS, 'leaseUntilUtcTicks', 'projection', 'pending')


def reconciliation(data):
    """Measure competing progress, rejected CAS attempts and current clearance."""
    proof = data['concurrentRecovery']
    require(proof['captureVersion'] == 1, 'versioned controlled reconciliation schedule')
    scope = dict(pendingId=data['persistedPendingId'], tenant=data['tenant'], conversation=data['pendingBinding']['conversation'], runtimeId=data['runtimeId'], catalogSha256=data['catalogSha256'], catalogFingerprint=data['catalogFingerprint'])
    require(proof['scope'] == scope, 'concurrent recovery exact pending runtime catalog and tenant scope')
    events = data['precedingEvents'] + data['recoveryEvents']
    positions = proof['authoritativePositions']
    require(len(positions) == len(events), 'complete authoritative global-position observations')
    previous = 0
    for observed, event in zip(positions, events):
        domain.sequence(observed['position'])
        require(observed['position'] > previous and observed['sequence'] == event['sequence'] and observed['eventSha256'] == digest(canonical(event)), 'global position bound to exact authoritative envelope')
        previous = observed['position']
    initial, newer, final = proof['oldWorker']['snapshot'], proof['newerStored'], proof['current']['after']
    for snapshot in (initial, newer, final):
        shape(snapshot, STATE_FIELDS)
        require(snapshot['captureVersion'] == 1 and snapshot['scope'] == scope, 'complete persisted recovery scope')
        for key in ('lease','version','generation','checkpoint','watermark'): domain.sequence(snapshot[key])
        domain.ticks(snapshot['leaseUntilUtcTicks']);text(snapshot['etag'])
    before_checkpoint = data['beforeCheckpoint']
    require(initial['checkpoint'] == before_checkpoint and initial['generation'] == data['beforeGeneration'] and initial['watermark'] == positions[before_checkpoint-1]['position'] == data['beforeWatermark'] and same(initial['projection'],data['beforeProjection']) and initial['pending'] == [scope['pendingId']], 'old worker captures actual preceding checkpoint watermark and pending state')
    require(proof['oldWorker']['expected'] == {key:initial[key] for key in FENCE_FIELDS}, 'old worker complete measured expectation')
    require(newer['checkpoint'] == data['afterCheckpoint'] and newer['generation'] == data['afterGeneration'] and newer['watermark'] == positions[-1]['position'] == data['afterWatermark'] and same(newer['projection'],data['persistedProjection']['state']) and newer['pending'] == initial['pending'], 'intervening newer progress from authoritative events and global positions')
    require(newer['checkpoint'] > initial['checkpoint'] and newer['generation'] > initial['generation'] and newer['watermark'] > initial['watermark'] and newer['version'] > initial['version'] and newer['lease'] > initial['lease'] and newer['etag'] != initial['etag'], 'measured concurrent newer progress and worker fence')
    prefix = data['recoveryEvents'][:1]
    stale, _ = domain.conversation_replay(prefix, before=data['beforeProjection'], checkpoint=before_checkpoint)
    stale_proposal = deepcopy(initial)
    stale_proposal.update(checkpoint=before_checkpoint+1, watermark=positions[before_checkpoint]['position'], projection=stale, pending=[])
    attempts = proof['staleAttempts']
    require([row['id'] for row in attempts] == ['stale-version','expired-lease','newer-generation','newer-watermark'], 'complete stale worker schedule inventory')
    for attempt in attempts:
        expected = {key:newer[key] for key in FENCE_FIELDS}
        if attempt['id'] == 'stale-version': expected.update(version=initial['version'], etag=initial['etag'])
        elif attempt['id'] == 'newer-generation': expected['generation'] = initial['generation']
        elif attempt['id'] == 'newer-watermark': expected['watermark'] = initial['watermark']
        require(attempt['scope'] == scope and same(attempt['expected'],expected) and same(attempt['proposed'],stale_proposal), 'stale CAS uses measured worker expectation and recovery-derived proposal')
        domain.ticks(attempt['observedAtUtcTicks'])
        expired = attempt['observedAtUtcTicks'] >= newer['leaseUntilUtcTicks']
        require(expired is (attempt['id'] == 'expired-lease'), 'expired versus live measured worker lease')
        admissible = same(expected,{key:newer[key] for key in FENCE_FIELDS}) and not expired
        require(admissible is False and attempt['succeeded'] is False and attempt['commitId'] is None and attempt['cleared'] == [], 'stale or expired CAS cannot write or clear')
        require(same(attempt['storedBefore'],newer) and same(attempt['storedAfter'],newer), 'stale attempt preserves complete newer stored state')
    current = proof['current']
    require(current['scope'] == scope and current['expected'] == {key:newer[key] for key in FENCE_FIELDS}, 'current worker expectation matches surviving newer state')
    domain.ticks(current['observedAtUtcTicks'])
    require(current['observedAtUtcTicks'] < newer['leaseUntilUtcTicks'], 'current recovery lease remains live')
    expected_final = deepcopy(newer);expected_final.update(pending=[],version=newer['version']+1,etag=final['etag'])
    require(final['etag'] != newer['etag'] and same(final,expected_final) and same(current['before'],newer), 'current clearance preserves complete newer domain progress')
    require(current['succeeded'] is True and current['cleared'] == [scope['pendingId']] and all(current[key] == text(current['commitId']) for key in ('projectionCommitId','checkpointCommitId','pendingCommitId','watermarkCommitId')), 'one durable current-worker composite clearance')
    retry = proof['duplicateRetry']
    require(retry['scope'] == scope and same(retry['before'],final) and same(retry['after'],final) and retry['succeeded'] is None and retry['commitId'] is None and retry['cleared'] == [], 'duplicate retry observes durable clearance and preserves newer state')
    require(data['clearCount'] == len(current['cleared']) and data['duplicateClearCount'] == len(retry['cleared']) and data['staleWorkerWrites'] == sum(row['succeeded'] for row in attempts), 'clearance and stale-write counts derive from measured schedule')
    require(same(data['queryProjection'],dict(sequence=final['checkpoint'],state=final['projection'])), 'query observes final current-worker state')
