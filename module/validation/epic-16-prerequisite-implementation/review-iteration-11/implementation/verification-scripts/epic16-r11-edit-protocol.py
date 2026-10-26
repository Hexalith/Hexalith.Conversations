from pathlib import Path
p=Path('_bmad/scripts/epic16_schedules.py');s=p.read_text();at=s.index('\ndef reconciliation(data):')
s=s[:at]+'''
def recovery_execution(row, data, *, duplicate=False):
    """Bind each independently reset negative branch to its executed CAS and completion."""
    proof = row['execution']; current = data['concurrentRecovery']['current']
    require(proof['captureVersion'] == 1 and text(proof['operationId']), 'versioned recovery execution identity')
    require(proof['branchId'] == ('current-clearance' if duplicate else row['id']) and same(proof['scope'], row['scope']), 'execution belongs to exact recovery branch and scope')
    operations = proof['operations']
    require([op['phase'] for op in operations] == ['request', 'attempt', 'completion'], 'complete recovery execution schedule')
    previous = data['wireObservation']['operations'][-1]['order'] if duplicate else 0
    for operation in operations:
        domain.sequence(operation['order'])
        require(operation['order'] > previous, 'recovery request attempt completion occur in order')
        previous = operation['order']
        require(operation['operationId'] == proof['operationId'] and operation['executed'] is True and operation['completed'] is True, 'same recovery operation actually executed and completed')
    request, attempt, completion = operations
    before, after = (row['before'], row['after']) if duplicate else (row['storedBefore'], row['storedAfter'])
    expected = {key:before[key] for key in FENCE_FIELDS} if duplicate else row['expected']
    proposed = before if duplicate else row['proposed']
    require(request['expectedSha256'] == digest(canonical(expected)) and request['proposedSha256'] == digest(canonical(proposed)), 'execution request binds exact expected and proposed state')
    require(attempt['storedBeforeSha256'] == digest(canonical(before)) and attempt['kind'] == ('observe-completion' if duplicate else 'compare-exchange'), 'attempt observes exact scoped stored state')
    require(attempt['succeeded'] is row['succeeded'] and attempt['transactionId'] == row['commitId'], 'executed attempt records actual CAS or no-write outcome')
    require(completion['storedAfterSha256'] == digest(canonical(after)) and completion['cleared'] == row['cleared'] and completion['outcome'] == ('AlreadyCompleted' if duplicate else 'FenceRejected'), 'completion binds exact preserved state and actual outcome')
    require(proof['afterClearanceTransactionId'] == (current['commitId'] if duplicate else None)
            and proof['wireRequestId'] == (data['wireObservation']['requestId'] if duplicate else None), 'duplicate execution follows the same current wire clearance')


def tenant_durable_outcome(row):
    """Derive a controlled AD-6 persisted outcome from native source and delivered events."""
    proof = row['durableEvidence']; name = row['id']
    require(proof['captureVersion'] == 1, 'versioned tenant durable outcome capture')
    tenant = text(proof['tenant']); before, after = proof['before'], proof['after']
    source = proof['sourceRecord']; accepted = tenant_record(source, tenant)
    state, _ = domain.tenant_replay(proof['sourceState'], [source], source['sequence']-1, source['sequence'])
    require(proof['sourceState']['tenantId'] == tenant, 'source state belongs to durable tenant')
    expected_before = dict(tenant=tenant, checkpoint=source['sequence'], projection=state,
        lastAccepted=accepted, health='Healthy', pending=[], horizon=source['sequence'], etag=before['etag'])
    text(before['etag']); text(after['etag'])
    require(same(before, expected_before), 'complete durable starting state derives from source event')
    delivered = tenant_record(proof['delivered'], tenant)
    expected = deepcopy(before)
    if name == 'accepted':
        require(delivered['sequence'] == before['checkpoint']+1, 'acknowledged accepted event is exact next')
        projection, _ = domain.tenant_replay(state, [proof['delivered']], before['checkpoint'], delivered['sequence'])
        expected.update(checkpoint=delivered['sequence'], projection=projection, lastAccepted=delivered, horizon=delivered['sequence'])
    elif name == 'duplicate':
        require(same(delivered, accepted), 'acknowledged duplicate matches actual committed fingerprint')
    elif name == 'pending':
        require(delivered['sequence'] > before['checkpoint']+1, 'acknowledged pending event is genuinely future')
        expected.update(health='Gapped', pending=[delivered], horizon=delivered['sequence'])
    else:
        require(name == 'corrupt' and delivered['sequence'] == before['checkpoint'] and not same(delivered, accepted), 'acknowledged corruption derives from conflicting native fingerprint')
        expected['health'] = 'Corrupt'
    mutation = name != 'duplicate'
    require(proof['writeCount'] == int(mutation) and proof['expectedEtag'] == before['etag'], 'outcome proves current CAS or previously committed duplicate without reapplication')
    if mutation:
        require(after['etag'] != before['etag'], 'durable outcome mutation advances opaque etag')
        expected['etag'] = after['etag']
    require(same(after, expected), 'persisted tenant outcome is the exact source-derived state')
    require(row['durableCommit']['transactionId'] == text(proof['transactionId']), 'actual outcome transaction is the acknowledged durable transaction')
    return after

''' + s[at:]
s=s.replace("    for attempt in attempts:\n        expected =", "    for attempt in attempts:\n        recovery_execution(attempt, data)\n        expected =")
s=s.replace("    retry = proof['duplicateRetry']\n", "    retry = proof['duplicateRetry']\n    recovery_execution(retry, data, duplicate=True)\n")
p.write_text(s)
p=Path('_bmad/scripts/epic16_measurements.py');s=p.read_text()
s=s.replace("require(commit is None and ack is None, 'provider failure and exhausted retries cannot acknowledge')", "require(commit is None and ack is None and row['durableEvidence'] is None, 'provider failure and exhausted retries cannot acknowledge')")
s=s.replace("        records.append(commit['recordId'])", "        persisted = schedules.tenant_durable_outcome(row)\n        lifecycle.commit_before_ack(dict(captureVersion=1, scheduleId=commit['recordId'], commit=commit, acknowledgement=ack), row['deliveryId'], commit['recordId'], persisted, commit['utcTicks'], ack['utcTicks'])\n        records.append(commit['recordId'])")
p.write_text(s)
