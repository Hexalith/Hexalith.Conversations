"""Disposable measured-input fixtures cannot qualify a runtime checkout."""
from __future__ import annotations

from copy import deepcopy
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from xml.etree import ElementTree as ET

import jsonschema
import pytest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / '_bmad/scripts'))
import epic16_tooling as tooling
from evidence_boundary_current import digest
import generate_story_record as records

SCOPE = 'tooling-fixture'
OBSERVATIONS = json.loads((ROOT / '_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes())


def git(repo, *args):
    return subprocess.run(['git', '-C', str(repo), *args], check=True, capture_output=True).stdout.decode().strip()


def dump(repo, path, doc):
    target = repo / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(tooling.json_bytes(doc))


def bind(repo):
    paths = sorted((*tooling.TOOL_SOURCES, *tooling.CONTRACT_PINS, *tooling.PREDECESSOR_PINS))
    dump(repo, tooling.BINDING, dict(schemaVersion='hexalith.conversations.epic16-tooling-binding.v1',
        purpose='prerequisite-tooling-only', authority=tooling.AUTHORITY,
        sources=[dict(path=p, sha256=digest((repo / p).read_bytes())) for p in paths], entry=None))


def native(repo, scenario, observations=None, **changes):
    story = scenario[3:7]
    candidate = git(repo, 'rev-parse', 'HEAD')
    payload = dict(scenarioId=scenario, candidate=candidate,
        sources=[dict(path=tooling.FIXTURE_SOURCE, sha256=digest((repo / tooling.FIXTURE_SOURCE).read_bytes()))],
        contractSha256=tooling.CONTRACT_PINS[tooling.contract_path(story)],
        observations=deepcopy(OBSERVATIONS[scenario]) if observations is None else observations)
    payload.update(changes)
    root = ET.Element('TestRun')
    results = ET.SubElement(root, 'Results')
    case = ET.SubElement(results, 'UnitTestResult', testName='Hexalith.Conversations.IntegrationTests.Epic16.Story'+story.replace('.', '_')+'EvidenceTests.Scenario'+scenario[-2:], testId=scenario, executionId=scenario+'-run', outcome='Passed')
    ET.SubElement(ET.SubElement(case, 'Output'), 'StdOut').text = json.dumps(payload)
    defs = ET.SubElement(root, 'TestDefinitions')
    definition = ET.SubElement(defs, 'UnitTest', id=scenario)
    ET.SubElement(definition, 'TestMethod', codeBase='fixture.dll', className='Hexalith.Conversations.IntegrationTests.Epic16.Story'+story.replace('.', '_')+'EvidenceTests', name='Scenario'+scenario[-2:])
    ET.SubElement(definition, 'Execution', id=scenario+'-run')
    entries = ET.SubElement(root, 'TestEntries')
    ET.SubElement(entries, 'TestEntry', testId=scenario, executionId=scenario+'-run')
    summary = ET.SubElement(root, 'ResultSummary', outcome='Completed')
    ET.SubElement(summary, 'Counters', total='1', executed='1', passed='1', failed='0', notExecuted='0')
    path = repo / tooling.native_path(SCOPE, scenario)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(ET.tostring(root))
    return path


@pytest.fixture
def repository(tmp_path):
    repo = tmp_path / 'fixture'; repo.mkdir()
    for path in {*tooling.TOOL_SOURCES, *tooling.CONTRACT_PINS, *tooling.PREDECESSOR_PINS}:
        target = repo / path; target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / path, target)
    # This is a disposable protocol fixture, not a claimed runtime test or result.
    source = repo / tooling.FIXTURE_SOURCE; source.parent.mkdir(parents=True, exist_ok=True)
    source.write_text('# Disposable native-result protocol fixture; no runtime authority.\n')
    (repo / 'fixture-spec.md').write_text("---\nstatus: in-progress\n---\n\nFixture only.\n\n## Tasks & Acceptance\n\n- [ ] Fixture task.\n\n## Implementation Notes\n")
    dump(repo, 'fixture-predecessor.json', {'result': 'PASS', 'scope': 'fixture-only'})
    sprint = repo / tooling.SPRINT; sprint.parent.mkdir(parents=True, exist_ok=True)
    sprint.write_text('last_updated: 2026-10-25\ndevelopment_status:\n  '+tooling.STORY_KEYS['16.1']+': in-progress\n')
    (repo / '.gitignore').write_text('artifacts/\n')
    (repo / '.gitmodules').write_text('')
    git(repo, 'init', '-q'); git(repo, 'config', 'user.name', 'Disposable Fixture'); git(repo, 'config', 'user.email', 'fixture@example.invalid')
    contract_source = repo/'src/Hexalith.Conversations.Contracts/Contract.cs'; contract_source.parent.mkdir(parents=True)
    contract_source.write_text('namespace Fixture; public sealed record Contract(string Value);\n')
    git(repo, 'add', 'src/Hexalith.Conversations.Contracts', '.gitignore', '.gitmodules'); git(repo, 'commit', '-qm', 'test: initialize disposable fixture')
    bind(repo)
    git(repo, 'add', '.'); git(repo, 'commit', '-qm', 'test: bind disposable fixture sources')
    for scenario in tooling.measurements.SCENARIOS: native(repo, scenario)
    return repo


def evaluate(repo, scenario='AC-16.1-01'):
    return tooling.evaluate(repo, scenario[3:7], scenario, scope=SCOPE)


def rejected(result, code, state='FAIL'):
    assert result['result'] == state, result
    assert code in result['blockers'], result
    assert result['assertionLedger']
    assert result['authority'] == dict(implementationHold='ACTIVE', executionAllowed=False,
        ownerApprovalClaimed=False, releaseAuthorized=False, pushAuthorized=False)


@pytest.mark.parametrize('scenario', tooling.measurements.SCENARIOS)
def test_all_fifteen_measured_routes_pass_only_as_disposable_fixtures(repository, scenario):
    result = evaluate(repository, scenario)
    assert result['result'] == 'PASS', result
    assert result['runtimeQualified'] is False
    assert result['counts'] == dict(total=1, passed=1, failed=0, skipped=0)
    assert len(result['assertionLedger']) >= 4
    schema = json.loads((repository / tooling.SCHEMA).read_bytes())
    jsonschema.validate(result, schema['$defs']['result'])


@pytest.mark.parametrize('story', tooling.measurements.STORIES)
def test_final_record_derives_sixth_after_five_and_is_deterministic(repository, story):
    for n in range(1, 6):
        scenario = f'AC-{story}-0{n}'
        dump(repository, tooling.receipt_path(SCOPE, scenario), evaluate(repository, scenario))
    result, j, m = tooling.generate(repository, story, scope=SCOPE, write=True)
    assert result['result'] == 'PASS', result
    assert result['summary'] == dict(required=6, passed=6, failed=0, blocked=0, skipped=0, notRun=0)
    assert result['runtimeQualified'] is False
    assert (j, m) == tooling.generate(repository, story, scope=SCOPE)[1:]
    assert (repository / result['outputs'][0]).read_bytes() == j
    assert (repository / result['outputs'][1]).read_bytes() == m
    jsonschema.validate(result, json.loads((repository / tooling.SCHEMA).read_bytes())['$defs']['record'])


MUTATIONS = {
    'AC-16.1-01': lambda d: d['authorization'][1].update(allowed=True),
    'AC-16.1-02': lambda d: d['afterRestart'].update(recoveryThrough=0),
    'AC-16.1-03': lambda d: d['merges'][2].update(afterHorizon=11),
    'AC-16.1-04': lambda d: d['acknowledgements'][-1].update(acknowledged=True),
    'AC-16.1-05': lambda d: d.update(afterContractSha256='f'*64),
    'AC-16.2-01': lambda d: d['events'][0]['binding'].update(payloadSha256='f'*64),
    'AC-16.2-02': lambda d: d['rebuilds'][0].update(stateHash='f'*64),
    'AC-16.2-03': lambda d: d['queries'][-1].update(indexReads=1),
    'AC-16.2-04': lambda d: d['lifecycle'][1].update(drainedThrough=19),
    'AC-16.2-05': lambda d: d.update(mixedGenerationResult='Current'),
    'AC-16.3-01': lambda d: d.update(liveLaneStarted=True),
    'AC-16.3-02': lambda d: d.update(occupiedPort=50003),
    'AC-16.3-03': lambda d: d.update(acknowledgedAt=10),
    'AC-16.3-04': lambda d: d.update(clearCount=2),
    'AC-16.3-05': lambda d: d['faults'][0].update(observedCodes=['PASS']),
}


@pytest.mark.parametrize('scenario', tooling.measurements.SCENARIOS)
def test_each_route_rejects_semantic_mutation_even_when_native_test_claims_pass(repository, scenario):
    data = deepcopy(OBSERVATIONS[scenario]); MUTATIONS[scenario](data)
    native(repository, scenario, data)
    rejected(evaluate(repository, scenario), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario', tooling.measurements.SCENARIOS)
def test_each_route_blocks_missing_measurement(repository, scenario):
    (repository / tooling.native_path(SCOPE, scenario)).unlink()
    rejected(evaluate(repository, scenario), 'INPUT_UNAVAILABLE', 'BLOCKED')


@pytest.mark.parametrize('scenario', tooling.measurements.SCENARIOS)
def test_each_route_rejects_unknown_measurement_fields(repository, scenario):
    data = deepcopy(OBSERVATIONS[scenario]); data['forgedPass'] = True
    native(repository, scenario, data)
    rejected(evaluate(repository, scenario), 'MEASUREMENT_SCHEMA_INVALID')


@pytest.mark.parametrize('value', [None, 0, '', [], {}, 'false', True])
def test_false_like_authority_is_never_normalized(repository, value):
    path = repository / tooling.BINDING; doc = json.loads(path.read_bytes())
    doc['authority']['executionAllowed'] = value; dump(repository, tooling.BINDING, doc)
    rejected(evaluate(repository), 'AUTHORITY_INVALID')


@pytest.mark.parametrize('field', ['sources', 'contractSha256', 'candidate', 'scenarioId'])
def test_native_bindings_cannot_be_relabelled(repository, field):
    value = [] if field == 'sources' else 'forged'
    native(repository, 'AC-16.1-01', **{field: value})
    rejected(evaluate(repository), 'NATIVE_BINDING_INVALID')


@pytest.mark.parametrize('mutation,code', [
    (lambda r: r.find('ResultSummary/Counters').set('passed', '99'), 'NATIVE_COUNTS_INVALID'),
    (lambda r: r.find('Results/UnitTestResult').set('outcome', 'NotExecuted'), 'NATIVE_COUNTS_INVALID'),
    (lambda r: r.find('Results/UnitTestResult').set('outcome', 'Failed'), 'NATIVE_COUNTS_INVALID'),
    (lambda r: r.find('Results').append(deepcopy(r.find('Results/UnitTestResult'))), 'NATIVE_COUNTS_INVALID'),
    (lambda r: r.find('Results').clear(), 'NATIVE_COUNTS_INVALID'),
    (lambda r: r.find('Results/UnitTestResult').set('testName', 'wrong'), 'NATIVE_IDENTITY_DRIFT'),
    (lambda r: r.find('TestEntries/TestEntry').set('executionId', 'wrong'), 'NATIVE_IDENTITY_DRIFT'),
])
def test_native_counts_and_executed_test_identity_are_recomputed(repository, mutation, code):
    path = native(repository, 'AC-16.1-01'); root = ET.fromstring(path.read_bytes()); mutation(root); path.write_bytes(ET.tostring(root))
    rejected(evaluate(repository), code)


@pytest.mark.parametrize('mutation', [
    lambda d: d['sources'].pop(),
    lambda d: d['sources'].append(d['sources'][0]),
    lambda d: d['sources'].reverse(),
])
def test_binding_inventory_requires_exact_order_and_cardinality(repository, mutation):
    doc = json.loads((repository / tooling.BINDING).read_bytes()); mutation(doc); dump(repository, tooling.BINDING, doc)
    rejected(evaluate(repository), 'INVENTORY_DRIFT')


def test_stale_dirty_and_missing_history_cannot_pass(repository):
    parent = git(repository, 'rev-parse', 'HEAD^')
    rejected(tooling.evaluate(repository, '16.1', 'AC-16.1-01', parent, SCOPE), 'CANDIDATE_STALE')
    (repository / 'unbound.txt').write_text('dirty')
    rejected(evaluate(repository), 'CANDIDATE_DIRTY', 'BLOCKED')
    (repository / 'unbound.txt').unlink()
    (repository / '.git/shallow').write_text(git(repository, 'rev-parse', 'HEAD')+'\n')
    rejected(evaluate(repository), 'HISTORY_UNAVAILABLE', 'BLOCKED')


def test_runtime_and_generator_remain_blocked_even_with_all_fixture_proofs(repository):
    for scenario in tooling.measurements.SCENARIOS:
        rejected(tooling.evaluate(repository, scenario[3:7], scenario), 'ENTRY_QUALIFICATION_PENDING', 'BLOCKED')
    result, j, m = tooling.generate(repository, '16.1')
    rejected(result, 'ENTRY_QUALIFICATION_PENDING', 'BLOCKED'); assert j is m is None
    assert not (repository / 'docs/release-evidence/story-16.1-final-record-v2.json').exists()


def test_artifact_cannot_nominate_its_own_entry_trust(repository):
    doc = json.loads((repository / tooling.BINDING).read_bytes()); doc['entry'] = dict(path='owner.json',sha256='f'*64)
    dump(repository, tooling.BINDING, doc)
    rejected(tooling.evaluate(repository, '16.1', 'AC-16.1-01'), 'ENTRY_UNTRUSTED')
    rejected(evaluate(repository), 'FIXTURE_AUTHORITY_FORBIDDEN')


@pytest.mark.parametrize('kind', ['symlink', 'fifo'])
def test_native_inputs_are_contained_regular_files(repository, kind):
    path = repository / tooling.native_path(SCOPE, 'AC-16.1-01'); content = path.read_bytes(); path.unlink()
    if kind == 'symlink':
        target = repository.parent / 'external.trx'; target.write_bytes(content); path.symlink_to(target)
        code = 'PATH_ESCAPE'
    else: os.mkfifo(path); code = 'INPUT_NOT_REGULAR'
    rejected(evaluate(repository), code)


def test_contract_schema_predecessor_and_source_pins_are_independent(repository):
    cases = [(tooling.contract_path('16.1'), 'CONTRACT_DRIFT'), (tooling.SCHEMA, 'SCHEMA_DRIFT'),
             (next(iter(tooling.PREDECESSOR_PINS)), 'PREDECESSOR_DRIFT'), (tooling.TOOL_SOURCES[0], 'SOURCE_DRIFT')]
    for path, code in cases:
        target = repository/path; before = target.read_bytes(); target.write_bytes(before+b' ')
        rejected(evaluate(repository), code); target.write_bytes(before)


def test_duplicate_json_keys_and_nonfinite_numbers_fail(repository):
    target = repository/tooling.BINDING; original=target.read_bytes()
    for raw in (b'{"sources":[],"sources":[]}',b'{"sources":NaN}'):
        target.write_bytes(raw); rejected(evaluate(repository), 'JSON_INVALID')
    target.write_bytes(original)


def test_receipt_forgery_and_output_aliases_preserve_sources(repository):
    for n in range(1,6):
        scenario=f'AC-16.1-0{n}'; dump(repository,tooling.receipt_path(SCOPE,scenario),evaluate(repository,scenario))
    path=tooling.receipt_path(SCOPE,'AC-16.1-01'); receipt=json.loads((repository/path).read_bytes())
    receipt['counts']['passed']=99;dump(repository,path,receipt)
    rejected(tooling.generate(repository,'16.1',scope=SCOPE)[0],'RECEIPT_DRIFT')
    dump(repository,path,evaluate(repository))
    target=repository/tooling.final_paths(SCOPE,'16.1')[0]
    source=repository/tooling.FIXTURE_SOURCE; before=source.read_bytes()
    os.link(source,target)
    rejected(tooling.generate(repository,'16.1',scope=SCOPE,write=True)[0],'OUTPUT_ALIAS')
    assert source.read_bytes()==before
    target.unlink(); target.symlink_to(source)
    rejected(tooling.generate(repository,'16.1',scope=SCOPE,write=True)[0],'OUTPUT_FORBIDDEN')
    assert source.read_bytes()==before


def test_protected_and_escaping_outputs_never_write_even_while_held(repository):
    for output in ('../escape.json','.git/config',tooling.contract_path('16.1')):
        result=tooling.generate(repository,'16.1',paths=[output,'else.md'])[0]
        rejected(result,'OUTPUT_FORBIDDEN')


def test_final_record_requires_every_native_receipt_not_just_pass_flags(repository):
    for n in range(1,6):
        scenario=f'AC-16.1-0{n}';dump(repository,tooling.receipt_path(SCOPE,scenario),evaluate(repository,scenario))
    (repository/tooling.native_path(SCOPE,'AC-16.1-03')).unlink()
    result,j,m=tooling.generate(repository,'16.1',scope=SCOPE)
    rejected(result,'INPUT_UNAVAILABLE','BLOCKED');assert j is m is None


def test_inserted_record_is_exact_and_cannot_hide_intervening_sources(repository):
    for n in range(1,6):
        scenario=f'AC-16.1-0{n}';dump(repository,tooling.receipt_path(SCOPE,scenario),evaluate(repository,scenario))
    _,j,m=tooling.generate(repository,'16.1',scope=SCOPE,write=True)
    spec=repository/'fixture-spec.md'; original=spec.read_bytes()
    spec.write_bytes(original+b'\n<!-- STORY-FINAL-RECORD:BEGIN -->\n'+m+b'<!-- STORY-FINAL-RECORD:END -->\n')
    result=tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE)
    assert result['result']=='PASS',result
    spec.write_bytes(spec.read_bytes().replace(b'Summary: 6/6',b'Summary: 9/9'))
    rejected(tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE),'INSERTED_RECORD_DRIFT')


def test_three_identical_future_rows_and_vacuous_rebuild_cannot_pass(repository):
    data=deepcopy(OBSERVATIONS['AC-16.1-03']);data['merges']=[data['merges'][0]]*3
    native(repository,'AC-16.1-03',data);rejected(evaluate(repository,'AC-16.1-03'),'MEASUREMENT_INVALID')
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);data['lifecycle'][1]['streams'][0].update(start=5,end=1,applied=[])
    native(repository,'AC-16.2-04',data);assert evaluate(repository,'AC-16.2-04')['result']=='FAIL'
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);data['lifecycle'][1]['queuedAfterC']=[]
    native(repository,'AC-16.2-04',data);rejected(evaluate(repository,'AC-16.2-04'),'MEASUREMENT_SCHEMA_INVALID')


def test_dispatch_rejects_other_story_and_runs_no_artifact_commands(repository, capsys):
    code=tooling.main(['--repository',str(repository),'--contract',tooling.contract_path('16.2'),'--scenario','AC-16.2-01'],story='16.1')
    assert code==1;rejected(json.loads(capsys.readouterr().out),'CONTRACT_STORY_MISMATCH')


def publish_fixture(repository, *, corrupt=None):
    story='16.1'
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}';dump(repository,tooling.receipt_path(SCOPE,scenario),evaluate(repository,scenario))
    result,j,m=tooling.generate(repository,story,scope=SCOPE,write=True)
    assert result['result']=='PASS',result
    paths=tooling.final_paths(SCOPE,story)+[fn(SCOPE,f'AC-{story}-0{n}') for n in range(1,6) for fn in (tooling.receipt_path,tooling.native_path)]
    original=None
    if corrupt:
        target=repository/corrupt;original=target.read_bytes();target.write_bytes(original+b' ')
    git(repository,'add','-f','--',*paths);git(repository,'commit','-qm','test: publish disposable measured fixtures')
    if corrupt: (repository/corrupt).write_bytes(original)
    spec=repository/'fixture-spec.md';spec.write_bytes(spec.read_bytes()+b'\n<!-- STORY-FINAL-RECORD:BEGIN -->\n'+m+b'<!-- STORY-FINAL-RECORD:END -->\n')
    return result


def test_atomic_committed_publication_and_lifecycle_pass(repository):
    publish_fixture(repository)
    assert tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE)['result']=='PASS'
    git(repository,'add','fixture-spec.md');git(repository,'commit','-qm','test: insert disposable final record')
    assert tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE)['result']=='PASS'


@pytest.mark.parametrize('path', [tooling.receipt_path(SCOPE,'AC-16.1-01'),tooling.native_path(SCOPE,'AC-16.1-01'),*tooling.final_paths(SCOPE,'16.1')])
def test_invalid_committed_publication_cannot_be_repaired_only_in_worktree(repository,path):
    publish_fixture(repository,corrupt=path)
    rejected(tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE),'PUBLICATION_BYTES_DRIFT')


def test_intervening_source_change_and_revert_remain_visible(repository):
    publish_fixture(repository)
    git(repository,'add','fixture-spec.md');git(repository,'commit','-qm','test: insert disposable final record')
    path=repository/tooling.FIXTURE_SOURCE;before=path.read_bytes();path.write_bytes(before+b'# source drift\n')
    git(repository,'add',tooling.FIXTURE_SOURCE);git(repository,'commit','-qm','test: change disposable source')
    path.write_bytes(before);git(repository,'add',tooling.FIXTURE_SOURCE);git(repository,'commit','-qm','test: restore disposable source')
    rejected(tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE),'LIFECYCLE_DRIFT')


def test_insertion_target_is_owned(repository):
    rejected(tooling.verify_inserted(repository,'16.1','another-spec.md',SCOPE),'SPEC_PATH_INVALID')


@pytest.mark.parametrize('scenario,path,value', [
    ('AC-16.1-01',('transitions',0,'checkpointVersion'),99),
    ('AC-16.1-01',('transitions',1,'applied'),True),
    ('AC-16.1-01',('storeOwner',),'Conversations'),
    ('AC-16.1-02',('newFence',),1),
    ('AC-16.1-02',('redeliveries',),1),
    ('AC-16.1-03',('checkpoint',),9),
    ('AC-16.1-03',('conflictingFutureState',),'Gapped'),
    ('AC-16.1-03',('staleWorkerWrites',),1),
    ('AC-16.1-03',('replicaA','allowed'),True),
    ('AC-16.1-04',('acknowledgements',5,'attempts'),4),
    ('AC-16.1-04',('faults',0,'afterSha256'),'f'*64),
    ('AC-16.1-05',('restartReceipt',),'AC-16.1-03'),
    ('AC-16.2-01',('events',0,'selectedId'),'other-id'),
    ('AC-16.2-01',('events',0,'validatedBeforeDeduplication'),False),
    ('AC-16.2-01',('events',0,'binding','selectedUtcTicks'),2000),
    ('AC-16.2-01',('clockReads',),1),
    ('AC-16.2-02',('rebuilds',0,'generatedTicks'),1000),
    ('AC-16.2-02',('rebuilds',0,'roundTripJson'),'{}'),
    ('AC-16.2-02',('canonicalizerType',),'Python'),
    ('AC-16.2-02',('goldenOutput',),'{}'),
    ('AC-16.2-03',('queries',0,'indexVersion'),1),
    ('AC-16.2-03',('queries',0,'writes'),1),
    ('AC-16.2-04',('lifecycle',0,'componentGenerations',0),2),
    ('AC-16.2-04',('lifecycle',0,'deletedGenerations',0),3),
    ('AC-16.2-04',('lifecycle',1,'watermarkAfter'),9),
    ('AC-16.2-04',('lifecycle',1,'streams',0,'applied',1),3),
    ('AC-16.3-01',('attempts',),4),
    ('AC-16.3-02',('portsAfter',0),50002),
    ('AC-16.3-03',('handlerInvocations',),1),
    ('AC-16.3-03',('route',),'mock/reconcile'),
    ('AC-16.3-04',('authenticated',),False),
    ('AC-16.3-04',('afterWatermark',),1),
    ('AC-16.3-04',('queryProjection','sequence'),1),
    ('AC-16.3-05',('environmentOutcome',),'PASS'),
    ('AC-16.3-05',('cancelElapsedMs',),101),
])
def test_named_semantic_boundaries_turn_red_under_independent_mutations(repository,scenario,path,value):
    data=deepcopy(OBSERVATIONS[scenario]);target=data
    for key in path[:-1]:target=target[key]
    target[path[-1]]=value;native(repository,scenario,data)
    rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


def test_native_fixture_cannot_substitute_python_canonical_json(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-02'])
    from evidence_boundary_current import canonical
    data['goldenOutput']=canonical(json.loads(data['goldenInput'])).decode()
    native(repository,'AC-16.2-02',data)
    rejected(evaluate(repository,'AC-16.2-02'),'MEASUREMENT_INVALID')


def test_native_boolean_count_cannot_masquerade_as_integer(repository):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);data['transitions'][0]['projectionVersion']=True
    native(repository,'AC-16.1-01',data)
    rejected(evaluate(repository),'MEASUREMENT_SCHEMA_INVALID')


def test_security_facility_unavailable_is_blocked(repository,monkeypatch):
    import contained_reader
    monkeypatch.setattr(contained_reader,'secure_open_supported',lambda:False)
    rejected(evaluate(repository),'SECURE_OPEN_UNAVAILABLE','BLOCKED')


def test_native_source_is_required_and_committed(repository):
    path=repository/tooling.FIXTURE_SOURCE;path.unlink();git(repository,'add','-u');git(repository,'commit','-qm','test: remove disposable native source')
    rejected(evaluate(repository),'NATIVE_TESTS_UNAVAILABLE','BLOCKED')


def test_package_preparation_preserves_all_obligations_and_has_no_entry(repository):
    with tooling.ContainedReader(repository) as reader:
        contracts=[tooling.validate_package(reader,s)[0] for s in tooling.measurements.STORIES]
    assert sum(len(c['preservedContract']['scenarios']) for c in contracts)==18
    inv=json.loads((repository/'_bmad-output/planning-artifacts/epic-16-adoption-v2/inventories.json').read_bytes())
    graph=json.loads((repository/'_bmad-output/planning-artifacts/epic-16-adoption-v2/execution-graph.json').read_bytes())
    assert len(inv['requirements'])==32
    assert (len(graph['nodes']),len(graph['edges']))==(38,61)
    assert all(c['authority']['implementationHold']=='ACTIVE' for c in contracts)


def trusted_entry_fixture(repository, monkeypatch, mutation=None):
    candidate=git(repository,'rev-parse','HEAD')
    predecessor=dict(path='fixture-predecessor.json',sha256=digest((repository/'fixture-predecessor.json').read_bytes()))
    doc=dict(authorizationRevision=candidate,storyId='16.1',qualifications={k:'PASS' for k in reversed(tooling.ENTRY_REQUIREMENTS)},
        executionAllowed=True,ownerApprovalClaimed=True,predecessors=[dict(identity=i,files=[predecessor]) for i in ('7.4','IR-0')])
    if mutation:mutation(doc)
    dump(repository,'fixture-entry.json',doc)
    pin=digest((repository/'fixture-entry.json').read_bytes())
    # Test-only trust seam. No owner pin is admitted to the real consumer.
    monkeypatch.setattr(tooling,'TRUSTED_ENTRIES',{'fixture-entry.json':pin})
    return candidate,dict(entry=dict(path='fixture-entry.json',sha256=pin))


def test_qualified_entry_map_order_is_irrelevant_but_predecessor_order_is_exact(repository,monkeypatch):
    candidate,binding=trusted_entry_fixture(repository,monkeypatch)
    with tooling.ContainedReader(repository) as reader:
        result=tooling.entry_evidence(reader,binding,'16.1',candidate,'runtime')
    assert [r['identity'] for r in result['predecessorFiles']]==['7.4','IR-0']
    candidate,binding=trusted_entry_fixture(repository,monkeypatch,lambda d:d['predecessors'].reverse())
    with tooling.ContainedReader(repository) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.entry_evidence(reader,binding,'16.1',candidate,'runtime')
    assert failure.value.code=='ENTRY_PREDECESSOR_DRIFT'


@pytest.mark.parametrize('mutation,code',[
    (lambda d:d.update(authorizationRevision='0'*40),'ENTRY_REVISION_UNAVAILABLE'),
    (lambda d:d['qualifications'].pop('sm2'),'ENTRY_INVENTORY_DRIFT'),
    (lambda d:d['qualifications'].update(sm2='BLOCKED'),'ENTRY_QUALIFICATION_PENDING'),
    (lambda d:d.update(executionAllowed=1),'ENTRY_AUTHORITY_INVALID'),
    (lambda d:d['predecessors'][0]['files'][0].update(sha256='0'*64),'ENTRY_PREDECESSOR_DRIFT'),
])
def test_independently_pinned_entry_still_validates_candidate_qualification_and_predecessors(repository,monkeypatch,mutation,code):
    candidate,binding=trusted_entry_fixture(repository,monkeypatch,mutation)
    with tooling.ContainedReader(repository) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.entry_evidence(reader,binding,'16.1',candidate,'runtime')
    assert failure.value.code==code


def test_entry_pinned_bytes_cannot_drift(repository,monkeypatch):
    candidate,binding=trusted_entry_fixture(repository,monkeypatch)
    path=repository/'fixture-entry.json';path.write_bytes(path.read_bytes()+b' ')
    with tooling.ContainedReader(repository) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.entry_evidence(reader,binding,'16.1',candidate,'runtime')
    assert failure.value.code=='ENTRY_DRIFT'


def test_native_test_binary_identity_is_required_and_bound(repository):
    path=native(repository,'AC-16.1-01');root=ET.fromstring(path.read_bytes())
    root.find('TestDefinitions/UnitTest/TestMethod').set('codeBase','')
    with tooling.ContainedReader(repository) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.native_binary(reader,ET.tostring(root),git(repository,'rev-parse','HEAD'),'AC-16.1-01')
    assert failure.value.code=='NATIVE_BINARY_UNAVAILABLE'
    (repository/'fixture.dll').write_bytes(b'1.0.0+'+b'0'*40)
    with tooling.ContainedReader(repository) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.native_binary(reader,path.read_bytes(),git(repository,'rev-parse','HEAD'),'AC-16.1-01')
    assert failure.value.code=='NATIVE_BINARY_DRIFT'


def test_native_method_alias_and_negative_counters_are_rejected(repository):
    path=native(repository,'AC-16.1-01');root=ET.fromstring(path.read_bytes())
    root.find('TestDefinitions/UnitTest/TestMethod').set('name','OtherTest')
    path.write_bytes(ET.tostring(root));rejected(evaluate(repository),'NATIVE_IDENTITY_DRIFT')
    path=native(repository,'AC-16.1-01');root=ET.fromstring(path.read_bytes())
    root.find('ResultSummary/Counters').set('failed','-1');root.find('ResultSummary/Counters').set('error','1')
    path.write_bytes(ET.tostring(root));rejected(evaluate(repository),'NATIVE_COUNTS_INVALID')


def test_owned_sprint_transition_preserves_other_stories(repository):
    publish_fixture(repository)
    path=repository/tooling.SPRINT;path.write_text(path.read_text().replace(': in-progress',': done'))
    git(repository,'add','fixture-spec.md',tooling.SPRINT);git(repository,'commit','-qm','test: complete disposable fixture lifecycle')
    result=tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE)
    assert result['result']=='PASS',result
    path.write_text(path.read_text()+'  unrelated-story: done\n')
    rejected(tooling.verify_inserted(repository,'16.1','fixture-spec.md',SCOPE),'LIFECYCLE_DRIFT')


def test_unavailable_generator_repository_emits_nonempty_blocker(tmp_path):
    result,j,m=tooling.generate(tmp_path/'missing','16.1')
    assert result['result']!='PASS' and result['blockers'] and result['assertionLedger']
    assert j is m is None


def test_direct_dependency_commit_dirty_and_unavailable_states_are_distinct(tmp_path):
    child=tmp_path/'references/Dependency';child.mkdir(parents=True)
    git(child,'init','-q');git(child,'config','user.name','Fixture');git(child,'config','user.email','fixture@example.invalid')
    (child/'source').write_text('original');git(child,'add','source');git(child,'commit','-qm','test: create direct dependency fixture')
    links=[dict(path='references/Dependency',commit=git(child,'rev-parse','HEAD'))]
    tooling.verify_dependencies(tmp_path,links)
    (child/'untracked').write_text('dirty')
    with pytest.raises(tooling.Stop) as failure:tooling.verify_dependencies(tmp_path,links)
    assert failure.value.code=='DEPENDENCY_DIRTY' and failure.value.blocked
    (child/'untracked').unlink();(child/'source').write_text('changed')
    git(child,'add','source');git(child,'commit','-qm','test: advance direct dependency fixture')
    with pytest.raises(tooling.Stop) as failure:tooling.verify_dependencies(tmp_path,links)
    assert failure.value.code=='DEPENDENCY_COMMIT_DRIFT' and not failure.value.blocked
    child.rename(tmp_path/'moved')
    with pytest.raises(tooling.Stop) as failure:tooling.verify_dependencies(tmp_path,links)
    assert failure.value.code=='DEPENDENCY_UNAVAILABLE' and failure.value.blocked


@pytest.mark.parametrize('scenario',['\ud800',''])
def test_invalid_unicode_or_empty_scenario_emits_schema_valid_failure(repository,capsys,scenario):
    exit_code=tooling.main(['--repository',str(repository),'--contract',tooling.contract_path('16.1'),'--scenario',scenario])
    assert exit_code==1
    raw=capsys.readouterr().out
    document=json.loads(raw);rejected(document,'SCENARIO_INVALID')
    jsonschema.validate(document,json.loads((repository/tooling.SCHEMA).read_bytes())['$defs']['result'])


@pytest.mark.parametrize('contract',[
    '_bmad-output/planning-artifacts/v9/story-contracts/7.3.json',
    '_bmad-output/planning-artifacts/v9/story-contracts/11.1.json',
    '_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/16.1.json',
])
@pytest.mark.parametrize('inline',[False,True])
def test_standalone_generator_preserves_legacy_dispatch_without_successor_modules(tmp_path,contract,inline):
    script=tmp_path/'generate_story_record.py';shutil.copyfile(ROOT/'_bmad/scripts/generate_story_record.py',script)
    argument=['--contract='+contract] if inline else ['--contract',contract]
    observed=subprocess.run([sys.executable,str(script),'--repository',str(tmp_path),*argument,
        '--format','bundle','--output-json','result.json','--output-markdown','result.md'],capture_output=True,timeout=30)
    assert b'ModuleNotFoundError' not in observed.stderr
    report=json.loads(observed.stdout)
    assert report['schemaVersion']=='hexalith.conversations.story-record-generator-failure.v1'
    assert report['blockers']
    if contract in tooling.CONTRACT_PINS:
        assert report['result']=='BLOCKED' and report['exitCode']==observed.returncode==2
        assert report['blockers']==['SCHEMA_UNAVAILABLE']
    else:
        # Preserve the actual historical malformed-invocation result, including
        # FAIL versus BLOCKED, instead of imposing a new normalization rule.
        import gzip
        baseline=tmp_path/'retained_generator.py'
        baseline.write_bytes(gzip.decompress((ROOT/'_bmad/scripts/tests/fixtures/epic16-adoption-v2-generator.py.gz').read_bytes()))
        historical=subprocess.run([sys.executable,str(baseline),'--repository',str(tmp_path),*argument,
            '--format','bundle','--output-json','result.json','--output-markdown','result.md'],capture_output=True,timeout=30)
        assert (observed.returncode,observed.stdout,observed.stderr)==(historical.returncode,historical.stdout,historical.stderr)


def test_unicode_collection_keys_use_eventstore_utf16_ordinal_order(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-02'])
    event=deepcopy(data['authoritativeEvents'][3]);event['sequence']=6;event['messageId']='delivery-6'
    body=json.loads(event['payload']);body['fileId']='file:\ue000';body['metadata']['eventId']='event-6';body['metadata']['deduplicationKey']='tenant:tenant-a|conv:conversation-a|event-6|1'
    event['payload']=json.dumps(body);data['authoritativeEvents'].append(event)
    body=json.loads(data['authoritativeEvents'][3]['payload']);body['fileId']='file:\U00010000';data['authoritativeEvents'][3]['payload']=json.dumps(body)
    refresh_replay(data)
    assert [key for key in data['collectionKeys'] if key.startswith('fileReferences:')] == ['fileReferences:\U00010000','fileReferences:\ue000']
    native(repository,'AC-16.2-02',data)
    result=evaluate(repository,'AC-16.2-02');assert result['result']=='PASS',result
    data['collectionKeys'].reverse();native(repository,'AC-16.2-02',data)
    rejected(evaluate(repository,'AC-16.2-02'),'MEASUREMENT_INVALID')


def test_runtime_canonicalizer_pin_checks_actual_source_not_observation_claim(tmp_path):
    path=tmp_path/tooling.CANONICALIZER_SOURCE;path.parent.mkdir(parents=True)
    path.write_bytes((ROOT/'_bmad/scripts/epic16_native_helper/ReadModelBatchCanonicalJson.cs').read_bytes())
    with tooling.ContainedReader(tmp_path) as reader:
        measured=tooling.verify_canonicalizer_source(reader)
    assert measured['sha256']==tooling.measurements.EVENTSTORE_CANONICALIZER_SHA256
    path.write_bytes(path.read_bytes()+b'// changed serializer source\n')
    # The native observation still claims the original expected hash; actual
    # dependency source bytes independently make this check fail.
    assert OBSERVATIONS['AC-16.2-02']['canonicalizerSourceSha256']==tooling.measurements.EVENTSTORE_CANONICALIZER_SHA256
    with tooling.ContainedReader(tmp_path) as reader,pytest.raises(tooling.Stop) as failure:
        tooling.verify_canonicalizer_source(reader)
    assert failure.value.code=='CANONICALIZER_SOURCE_DRIFT'


def test_entry_can_be_committed_after_authorization_without_a_hash_cycle(repository, monkeypatch):
    authorization, binding = trusted_entry_fixture(repository, monkeypatch)
    dump(repository, 'fixture-entry-trust.json', binding['entry'])
    git(repository, 'add', 'fixture-entry.json', 'fixture-entry-trust.json')
    git(repository, 'commit', '-qm', 'test: pin prior fixture authorization')
    candidate = git(repository, 'rev-parse', 'HEAD')
    assert candidate != authorization
    with tooling.ContainedReader(repository) as reader:
        result = tooling.entry_evidence(reader, binding, '16.1', candidate, 'runtime')
    assert result['authorizationRevision'] == authorization
    with tooling.ContainedReader(repository) as reader, pytest.raises(tooling.Stop) as failure:
        tooling.entry_evidence(reader, binding, '16.2', candidate, 'runtime')
    assert failure.value.code == 'ENTRY_STORY_DRIFT'
    # A genuine sibling commit is available but not an ancestor of the candidate.
    sibling = git(repository, 'commit-tree', 'HEAD^{tree}', '-p', authorization, '-m', 'test: sibling authorization')
    document = json.loads((repository / 'fixture-entry.json').read_bytes())
    document['authorizationRevision'] = sibling
    dump(repository, 'fixture-entry.json', document)
    pin = digest((repository / 'fixture-entry.json').read_bytes())
    binding['entry']['sha256'] = pin
    monkeypatch.setattr(tooling, 'TRUSTED_ENTRIES', {'fixture-entry.json': pin})
    with tooling.ContainedReader(repository) as reader, pytest.raises(tooling.Stop) as failure:
        tooling.entry_evidence(reader, binding, '16.1', candidate, 'runtime')
    assert failure.value.code == 'ENTRY_ANCESTRY_INVALID'


@pytest.fixture(scope='session')
def managed_evidence(tmp_path_factory):
    directory = tmp_path_factory.mktemp('compiled-evidence')
    (directory / 'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3.extensibility.core" Version="4.0.2" /></ItemGroup></Project>')
    (directory / 'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+' + 'a' * 40 + '")]')
    (directory / 'Story16_1EvidenceTests.cs').write_text('namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_1EvidenceTests { [Xunit.Fact] public void Scenario01() { throw new System.Exception("Evidence assembly must never execute"); } public void NotATest() {} }')
    result = subprocess.run(['dotnet', 'build', 'Evidence.csproj', '-c', 'Debug', '--nologo', '-m:1', '-p:NuGetAudit=false'], cwd=directory, capture_output=True, timeout=120)
    assert result.returncode == 0, result.stdout.decode()
    return (directory / 'bin/Debug/net10.0/Evidence.dll').read_bytes()


@pytest.mark.parametrize('absolute', [False, True])
def test_genuine_managed_evidence_metadata_and_contained_absolute_codebase(repository, managed_evidence, absolute):
    binary = repository / 'fixture.dll'; binary.write_bytes(managed_evidence)
    path = native(repository, 'AC-16.1-01'); root = ET.fromstring(path.read_bytes())
    root.find('TestDefinitions/UnitTest/TestMethod').set('codeBase', str(binary) if absolute else 'fixture.dll')
    with tooling.ContainedReader(repository) as reader:
        result = tooling.native_binary(reader, ET.tostring(root), 'a'*40, 'AC-16.1-01')
    assert result['path'] == 'fixture.dll' and result['sourceRevision'] == 'a'*40
    assert result['method'] == 'Scenario01'
    with tooling.ContainedReader(repository) as reader, pytest.raises(tooling.Stop) as failure:
        tooling.native_binary(reader, ET.tostring(root), 'b'*40, 'AC-16.1-01')
    assert failure.value.code == 'NATIVE_BINARY_DRIFT'
    with tooling.ContainedReader(repository) as reader, pytest.raises(tooling.Stop) as failure:
        tooling.native_binary(reader, ET.tostring(root), 'a'*40, 'AC-16.1-02')
    assert failure.value.code == 'NATIVE_BINARY_DRIFT'


@pytest.mark.parametrize('content', [b'1.0.0+' + b'a'*40, b'MZ' + b'\0'*300, b'', b'\x7fELF' + b'a'*40])
def test_text_and_malformed_files_cannot_masquerade_as_source_stamped_test_binaries(repository, content):
    (repository/'fixture.dll').write_bytes(content)
    path = native(repository, 'AC-16.1-01')
    with tooling.ContainedReader(repository) as reader, pytest.raises(tooling.Stop) as failure:
        tooling.native_binary(reader, path.read_bytes(), 'a'*40, 'AC-16.1-01')
    assert failure.value.code == 'NATIVE_BINARY_DRIFT'


@pytest.mark.parametrize('kind', ['outside', 'dot', 'relative-dot', 'symlink', 'fifo'])
def test_native_binary_paths_reject_escape_traversal_symlink_and_nonregular(repository, kind, managed_evidence):
    binary = repository/'fixture.dll'; binary.write_bytes(managed_evidence)
    codebase = str(binary)
    if kind == 'outside': codebase = str(repository.parent/'fixture.dll')
    elif kind == 'dot': codebase = str(repository)+'/./fixture.dll'
    elif kind == 'relative-dot': codebase = 'sub/../fixture.dll'
    elif kind == 'symlink':
        binary.unlink(); binary.symlink_to(repository/tooling.FIXTURE_SOURCE)
    else:
        binary.unlink(); os.mkfifo(binary)
    path = native(repository, 'AC-16.1-01'); root = ET.fromstring(path.read_bytes())
    root.find('TestDefinitions/UnitTest/TestMethod').set('codeBase', codebase)
    with tooling.ContainedReader(repository) as reader, pytest.raises((tooling.Stop, tooling.ReadError)):
        tooling.native_binary(reader, ET.tostring(root), 'a'*40, 'AC-16.1-01')


@pytest.mark.parametrize('node', ['GenericTestResult', 'OtherResult', 'UnitTestResult'])
def test_every_extra_or_unknown_trx_result_is_rejected(repository, node):
    path = native(repository, 'AC-16.1-01'); root = ET.fromstring(path.read_bytes())
    ET.SubElement(root.find('Results'), node, outcome='Failed')
    path.write_bytes(ET.tostring(root))
    rejected(evaluate(repository), 'NATIVE_COUNTS_INVALID')


@pytest.mark.parametrize('field,value', [('state', 'Healthy'), ('authorized', True), ('expectedSequence', 14)])
def test_restart_snapshot_itself_must_remain_gapped_and_deny(repository, field, value):
    data = deepcopy(OBSERVATIONS['AC-16.1-02'])
    for name in ('beforeRestart', 'afterRestart'): data[name][field] = value
    native(repository, 'AC-16.1-02', data)
    rejected(evaluate(repository, 'AC-16.1-02'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('sequence', [10, 11])
def test_applied_or_exact_next_sequences_cannot_masquerade_as_durable_pending_gaps(repository, sequence):
    data = deepcopy(OBSERVATIONS['AC-16.1-02'])
    for name in ('beforeRestart', 'afterRestart'): data[name]['pending'][0]['sequence'] = sequence
    native(repository, 'AC-16.1-02', data)
    rejected(evaluate(repository, 'AC-16.1-02'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field', ['lease', 'version', 'checkpoint', 'horizon'])
def test_equal_completion_tuples_must_match_measured_recovery(repository, field):
    data = deepcopy(OBSERVATIONS['AC-16.1-03'])
    for name in ('expected', 'observed'): data['healthyCas'][name][field] = 0
    native(repository, 'AC-16.1-03', data)
    rejected(evaluate(repository, 'AC-16.1-03'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('schedule', range(4))
@pytest.mark.parametrize('fault', ['omit', 'duplicate', 'regress', 'allow', 'old-decision'])
def test_each_required_reverse_save_schedule_is_measured_and_nonregressing(repository, schedule, fault):
    data = deepcopy(OBSERVATIONS['AC-16.1-03']); rows = data['reverseSaves']
    if fault == 'omit': rows.pop(schedule)
    elif fault == 'duplicate': rows.append(deepcopy(rows[schedule]))
    elif fault == 'regress': rows[schedule]['replicaBAfter'] = deepcopy(rows[schedule]['older'])
    elif fault == 'allow': rows[schedule]['replicaAAfter']['allowed'] = True
    else: rows[schedule]['older']['allowed'] = False
    native(repository, 'AC-16.1-03', data)
    rejected(evaluate(repository, 'AC-16.1-03'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('value', [
    '{"anchorUtcTicks":2000,"items":["a","b"],"version":2,"version":2}',
    '{"anchorUtcTicks":2000,"items":["a","b"],"version":NaN}',
    '{"anchorUtcTicks":2000,"items":["a","b"],"version":Infinity}',
    '{"anchorUtcTicks":2000,"items":["a","b"],"version":1e999}',
    '{ "anchorUtcTicks":2000,"items":["a","b"],"version":2}',
    '{"version":2,"anchorUtcTicks":2000,"items":["a","b"]}',
    '{"anchorUtcTicks":2000,"items":["b","a"],"version":2}',
])
def test_replay_rejects_self_consistent_noncanonical_or_unbound_bytes(repository, value):
    data = deepcopy(OBSERVATIONS['AC-16.2-02'])
    for row in data['rebuilds']: row.update(canonicalJson=value, roundTripJson=value, stateHash=digest(value.encode()))
    native(repository, 'AC-16.2-02', data)
    rejected(evaluate(repository, 'AC-16.2-02'), 'MEASUREMENT_INVALID')


def test_actual_eventstore_oracle_preserves_web_escaping_utf16_order_and_number_tokens():
    import epic16_native
    raw = '{"z":-0.0,"\\ue000":1.0,"𐀀":"<é&>","a":1e+03}'
    assert epic16_native.canonicalize(raw) == '{"a":1e+03,"z":-0.0,"\\uD800\\uDC00":"\\u003C\\u00E9\\u0026\\u003E","\\uE000":1.0}'


@pytest.mark.parametrize('case,field,value', [
    ('v1', 'indexVersion', 2), ('missing', 'indexPresent', True),
    ('provider', 'providerState', 'Available'), ('corrupt', 'corrupt', False),
    ('mixed', 'componentGenerations', [3]*5), ('gap', 'sourceGap', False),
    ('unauthorized', 'authorized', True), ('nonexistent', 'tenantExists', True),
    ('Erasing', 'lifecycle', 'Ready'), ('Erased', 'lifecycle', 'Ready'),
    ('Rebuilding', 'lifecycle', 'Ready'), ('ready-empty', 'rebuildPending', True),
    ('ready-empty', 'survivingDetails', ['detail-a']),
    ('pending-dispatch', 'pendingDispatch', []),
])
def test_query_case_labels_cannot_replace_measured_control_index_inputs(repository, case, field, value):
    data = deepcopy(OBSERVATIONS['AC-16.2-03'])
    next(row for row in data['queries'] if row['id'] == case)[field] = value
    native(repository, 'AC-16.2-03', data)
    assert evaluate(repository, 'AC-16.2-03')['result'] == 'FAIL'


@pytest.mark.parametrize('transition', range(len(tooling.measurements.TRANSITIONS)))
def test_lifecycle_transition_causes_are_required(repository, transition):
    data = deepcopy(OBSERVATIONS['AC-16.2-03']); data['transitions'][transition]['cause'] = 'QueryRead'
    native(repository, 'AC-16.2-03', data)
    rejected(evaluate(repository, 'AC-16.2-03'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault', ['stage', 'foreign', 'reference', 'ack', 'content', 'target', 'generation'])
def test_erase_requires_durable_nonpayload_references_and_unchanged_absence(repository, fault):
    data = deepcopy(OBSERVATIONS['AC-16.2-04']); erase=data['lifecycle'][0]; delivery=erase['deliveries'][1]
    if fault == 'stage': delivery['lifecycle'] = 'Erasing'
    elif fault == 'foreign':
        delivery['reference']['tenant'] = delivery['durableReference']['tenant'] = 'foreign'
    elif fault == 'reference': delivery['durableReference']['messageId'] = 'other'
    elif fault == 'ack': delivery['acknowledgedAt'] = 1
    elif fault == 'content': delivery['derivedAfter'] = ['recreated-content']
    elif fault == 'target': erase['deletedTargets'][0]['key'] = 'uncaptured-target'
    else: erase['deletionTargets'][0]['generation'] = erase['deletedTargets'][0]['generation'] = 3
    native(repository, 'AC-16.2-04', data)
    assert evaluate(repository, 'AC-16.2-04')['result'] == 'FAIL'


@pytest.mark.parametrize('fault', ['missing', 'extra', 'foreign', 'duplicate', 'id', 'boundary', 'through-c', 'huge', 'generation'])
def test_rebuild_requires_exact_authoritative_tenant_streams_through_c_without_range_allocation(repository, fault):
    data = deepcopy(OBSERVATIONS['AC-16.2-04']); rebuild=data['lifecycle'][1]; stream=rebuild['streams'][0]
    if fault == 'missing': rebuild['streams'] = []
    elif fault == 'extra':
        extra=deepcopy(stream); extra['id']='unlisted'; rebuild['streams'].append(extra)
    elif fault == 'foreign': stream['tenant']='foreign'
    elif fault == 'duplicate': rebuild['streams'].append(deepcopy(stream))
    elif fault == 'id': stream['id']='other-stream'
    elif fault == 'boundary': stream['boundaryAt']=9
    elif fault == 'through-c': stream['capturedThrough']=19
    elif fault == 'huge': stream['end']=10**30
    else: rebuild['progressGenerationAfter']=4
    native(repository, 'AC-16.2-04', data)
    assert evaluate(repository, 'AC-16.2-04')['result'] == 'FAIL'


DEPENDENCIES = [('AC-16.1-05','AC-16.1-02'), ('AC-16.1-05','AC-16.1-03'),
    ('AC-16.2-05','AC-16.2-02'), ('AC-16.2-05','AC-16.2-03'), ('AC-16.3-04','AC-16.3-03')]


@pytest.mark.parametrize('scenario,dependency', DEPENDENCIES)
@pytest.mark.parametrize('fault', ['missing', 'invalid'])
def test_every_dependency_edge_rechecks_native_prerequisite_and_propagates_state(repository, scenario, dependency, fault):
    if fault == 'missing': (repository/tooling.native_path(SCOPE,dependency)).unlink()
    else:
        data=deepcopy(OBSERVATIONS[dependency]); MUTATIONS[dependency](data); native(repository,dependency,data)
    rejected(evaluate(repository,scenario), 'SCENARIO_DEPENDENCY_FAILED', 'BLOCKED' if fault=='missing' else 'FAIL')


@pytest.mark.parametrize('key', ['persistedPendingId','tenant','runtimeId','catalogSha256'])
def test_clearance_must_match_the_revalidated_first_failure_identity(repository,key):
    data=deepcopy(OBSERVATIONS['AC-16.3-04']);data[key]='other' if key!='catalogSha256' else '0'*64
    native(repository,'AC-16.3-04',data)
    rejected(evaluate(repository,'AC-16.3-04'),'RECONCILIATION_IDENTITY_DRIFT')


@pytest.mark.parametrize('story',tooling.measurements.STORIES)
def test_real_verifier_and_generator_entrypoints_publish_and_consume_exact_fixture_bytes(repository,story):
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}'; output=tooling.receipt_path(SCOPE,scenario)
        command=[sys.executable,str(repository/f'_bmad/scripts/verify_story_{story.replace(".","_")}.py'),
            '--repository',str(repository),'--contract',tooling.contract_path(story),
            '--scenario',scenario,'--scope',SCOPE,'--output',output]
        completed=subprocess.run(command,capture_output=True,timeout=120)
        assert completed.returncode==0,(completed.stdout,completed.stderr)
        expected=tooling.json_bytes(evaluate(repository,scenario))
        assert (repository/output).read_bytes()==expected
        assert json.loads(expected)['runtimeQualified'] is False
    paths=tooling.final_paths(SCOPE,story)
    command=[sys.executable,str(repository/'_bmad/scripts/generate_story_record.py'),'--repository',str(repository),
        '--contract',tooling.contract_path(story),'--scope',SCOPE,'--format','bundle',
        '--output-json',paths[0],'--output-markdown',paths[1]]
    completed=subprocess.run(command,capture_output=True,timeout=120)
    assert completed.returncode==0,(completed.stdout,completed.stderr)
    result,j,m=tooling.generate(repository,story,scope=SCOPE)
    assert (repository/paths[0]).read_bytes()==j and (repository/paths[1]).read_bytes()==m
    assert result['summary']['passed']==6 and result['runtimeQualified'] is False
    assert not any((repository/p).exists() for p in tooling.final_paths('runtime',story))


@pytest.mark.parametrize('extra', [99, 999])
def test_pending_inventory_cannot_exceed_merged_recovery_horizon(repository, extra):
    data = deepcopy(OBSERVATIONS['AC-16.1-03'])
    pending = deepcopy(data['merges'][0]['pending'][0]); pending['sequence'] = extra
    for merge in data['merges']: merge['pending'].append(deepcopy(pending))
    native(repository, 'AC-16.1-03', data)
    rejected(evaluate(repository, 'AC-16.1-03'), 'MEASUREMENT_INVALID')


def test_rebuild_watermark_must_cover_captured_cutover(repository):
    data = deepcopy(OBSERVATIONS['AC-16.2-04'])
    data['lifecycle'][1].update(watermarkBefore=10, watermarkAfter=10)
    native(repository, 'AC-16.2-04', data)
    rejected(evaluate(repository, 'AC-16.2-04'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('change', [dict(version=1), dict(version=True), dict(anchorUtcTicks=1000),
    dict(provider={'etag':'x'}), dict(projectionGeneratedAt=3000), dict(wrapper={}), dict(extra=0)])
def test_replay_state_protocol_is_closed_v2_and_hashes_the_measured_anchor(repository, change):
    import epic16_native
    data = deepcopy(OBSERVATIONS['AC-16.2-02'])
    value = json.loads(data['rebuilds'][0]['canonicalJson']); value.update(change)
    raw = epic16_native.canonicalize(json.dumps(value))
    for row in data['rebuilds']: row.update(canonicalJson=raw, roundTripJson=raw, stateHash=digest(raw.encode()))
    native(repository, 'AC-16.2-02', data)
    rejected(evaluate(repository, 'AC-16.2-02'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('sequence', [0, 2, 4])
def test_equal_persisted_and_query_state_must_equal_recovered_checkpoint(repository, sequence):
    data = deepcopy(OBSERVATIONS['AC-16.3-04'])
    for name in ('persistedProjection', 'queryProjection'): data[name]['sequence'] = sequence
    native(repository, 'AC-16.3-04', data)
    rejected(evaluate(repository, 'AC-16.3-04'), 'MEASUREMENT_INVALID')


def test_pending_sequence_is_correlated_to_the_first_failure(repository):
    data = deepcopy(OBSERVATIONS['AC-16.3-04']); data['pendingSequence'] = 2
    native(repository, 'AC-16.3-04', data)
    rejected(evaluate(repository, 'AC-16.3-04'), 'RECONCILIATION_IDENTITY_DRIFT')


def test_recovered_checkpoint_cannot_precede_pending_item(repository):
    data = deepcopy(OBSERVATIONS['AC-16.3-04']); data['afterCheckpoint'] = 2
    for key in ('persistedProjection', 'queryProjection'): data[key]['sequence'] = 2
    native(repository, 'AC-16.3-04', data)
    rejected(evaluate(repository, 'AC-16.3-04'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('row', range(4))
@pytest.mark.parametrize('fault', ['timestamp', 'order', 'same-order', 'delivery', 'record', 'outcome', 'missing-commit'])
def test_each_acknowledging_outcome_requires_correlated_durable_commit_before_ack(repository, row, fault):
    data = deepcopy(OBSERVATIONS['AC-16.1-04']); observed = data['acknowledgements'][row]
    if fault == 'timestamp': observed['acknowledgement']['utcTicks'] = observed['durableCommit']['utcTicks'] - 1
    elif fault == 'order': observed['acknowledgement']['order'] = 0
    elif fault == 'same-order': observed['acknowledgement']['order'] = observed['durableCommit']['order']
    elif fault == 'delivery': observed['acknowledgement']['deliveryId'] = 'other-delivery'
    elif fault == 'record': observed['acknowledgement']['recordId'] = 'other-outcome'
    elif fault == 'outcome': observed['durableCommit']['outcome'] = 'other'
    else: observed['durableCommit'] = None
    native(repository, 'AC-16.1-04', data)
    rejected(evaluate(repository, 'AC-16.1-04'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('row', [4, 5])
def test_failed_delivery_cannot_carry_an_acknowledgement_observation(repository, row):
    data = deepcopy(OBSERVATIONS['AC-16.1-04'])
    data['acknowledgements'][row]['acknowledgement'] = deepcopy(data['acknowledgements'][0]['acknowledgement'])
    native(repository, 'AC-16.1-04', data)
    rejected(evaluate(repository, 'AC-16.1-04'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('case', [0, 1])
@pytest.mark.parametrize('fault', ['persisted-sequence', 'persisted-freshness', 'consumer-sequence', 'consumer-freshness', 'decision', 'process', 'redelivery', 'same-process'])
def test_healthy_authorized_and_denied_restart_observations_preserve_consumer_metadata(repository, case, fault):
    data = deepcopy(OBSERVATIONS['AC-16.1-02']); row = data['healthyRestarts'][case]
    if fault == 'persisted-sequence': row['afterRestart']['sequence'] += 1
    elif fault == 'persisted-freshness': row['afterRestart']['freshness']['generatedUtcTicks'] += 1
    elif fault == 'consumer-sequence': row['consumerAfter']['sequence'] += 1
    elif fault == 'consumer-freshness': row['consumerAfter']['freshness']['generatedUtcTicks'] += 1
    elif fault == 'decision': row['consumerAfter']['allowed'] = not row['consumerAfter']['allowed']
    elif fault == 'process': row['consumerAfter']['process'] = row['processBefore']
    elif fault == 'redelivery': row['redeliveries'] = 1
    else: row['processAfter'] = row['processBefore']
    native(repository, 'AC-16.1-02', data)
    rejected(evaluate(repository, 'AC-16.1-02'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('ticks', [0, 3155378976000000000, 10**30])
def test_authoritative_selected_utc_ticks_are_representable(repository, ticks):
    data = deepcopy(OBSERVATIONS['AC-16.2-01'])
    data['events'][0]['occurredAtTicks'] = data['events'][0]['binding']['selectedUtcTicks'] = ticks
    native(repository, 'AC-16.2-01', data)
    rejected(evaluate(repository, 'AC-16.2-01'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('key', ['clockTicks', 'anchorTicks', 'generatedTicks', 'lastAppliedTicks', 'completionTicks'])
def test_replay_utc_ticks_are_representable(repository, key):
    data = deepcopy(OBSERVATIONS['AC-16.2-02']); data['rebuilds'][0][key] = 10**30
    native(repository, 'AC-16.2-02', data)
    rejected(evaluate(repository, 'AC-16.2-02'), 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('value', [0, 65536, 100000])
def test_endpoint_inventory_rejects_out_of_domain_ports(repository, value):
    data = deepcopy(OBSERVATIONS['AC-16.3-02'])
    data.update(occupiedPort=value, effectivePorts=[value, 50002], portsBefore=[value], portsAfter=[value])
    native(repository, 'AC-16.3-02', data)
    rejected(evaluate(repository, 'AC-16.3-02'), 'MEASUREMENT_INVALID')


def compile_evidence(directory, revision, source, *, fake=False, wrong_framework=False):
    directory.mkdir(parents=True, exist_ok=True)
    reference = '<PackageReference Include="xunit.v3.extensibility.core" Version="4.0.2" />' if not fake else ''
    assembly = '<AssemblyName>xunit.v3.core</AssemblyName><AssemblyVersion>4.0.2.0</AssemblyVersion>' if wrong_framework else ''
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo>'+assembly+'</PropertyGroup><ItemGroup>'+reference+'</ItemGroup></Project>')
    (directory/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
    (directory/'Tests.cs').write_text(source)
    if fake: (directory/'FactAttribute.cs').write_text('namespace Xunit; public sealed class FactAttribute : System.Attribute {}')
    result = subprocess.run(['dotnet', 'build', 'Evidence.csproj', '-c', 'Debug', '--nologo', '-m:1', '-p:NuGetAudit=false'], cwd=directory, capture_output=True, timeout=120)
    assert result.returncode == 0, result.stdout.decode()
    return (directory/('bin/Debug/net10.0/'+('xunit.v3.core' if wrong_framework else 'Evidence')+'.dll')).read_bytes()


def retain_capture(repo, scenario, binary, *, root=None, style='posix', original_relative=None):
    """Explicit disposable capture; never modifies the original native output."""
    path = tooling.native_path('runtime', scenario)
    raw = (repo/path).read_bytes()
    base = records.parse_trx(raw)['code_bases'][0]
    candidate = json.loads(records.parse_trx(raw)['results'][0]['output'])['candidate']
    sha = digest(binary)
    retained = tooling.capture.binary_path(scenario[3:7], sha)
    target = repo/retained; target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(binary)
    root = str(repo) if root is None else root
    relative = tooling.capture.original_relative(root, base, style) if original_relative is None else original_relative
    capture = dict(captureVersion=1, scenarioId=scenario, candidate=candidate,
        native=dict(path=path,sha256=digest(raw)), pathStyle=style,
        originalRepositoryRoot=root, originalCodeBase=base, originalRelativePath=relative,
        binary=dict(path=retained,sha256=sha))
    dump(repo, tooling.capture.capture_path(scenario[3:7], scenario), capture)
    return capture


def retained_binary(repo, scenario='AC-16.2-02'):
    capture = json.loads((repo/tooling.capture.capture_path(scenario[3:7],scenario)).read_bytes())
    return repo/capture['binary']['path']


@pytest.fixture(scope='session')
def runtime_template(tmp_path_factory):
    """Test-only qualified repository with real ancestry, a clean direct dependency and native metadata."""
    base = tmp_path_factory.mktemp('qualified-runtime-template')
    repo = repository.__wrapped__(base)
    story = '16.2'; scenario = 'AC-16.2-02'
    authorization = git(repo, 'rev-parse', 'HEAD')
    source = 'tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs'
    code = 'namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_2EvidenceTests { [Xunit.Fact] public void Scenario02() { throw new System.Exception("Do not execute evidence"); } }'
    target = repo/source; target.parent.mkdir(parents=True); target.write_text(code)
    dependency = repo/'references/Hexalith.EventStore'; dependency.mkdir(parents=True)
    canonicalizer = repo/tooling.CANONICALIZER_SOURCE; canonicalizer.parent.mkdir(parents=True)
    canonicalizer.write_bytes((ROOT/'_bmad/scripts/epic16_native_helper/ReadModelBatchCanonicalJson.cs').read_bytes())
    git(dependency, 'init', '-q'); git(dependency, 'config', 'user.name', 'Disposable Fixture'); git(dependency, 'config', 'user.email', 'fixture@example.invalid')
    git(dependency, 'add', '.'); git(dependency, 'commit', '-qm', 'test: retain canonicalizer fixture')
    link = git(dependency, 'rev-parse', 'HEAD')
    (repo/'.gitmodules').write_text('[submodule "references/Hexalith.EventStore"]\n\tpath = references/Hexalith.EventStore\n\turl = https://example.invalid/disposable\n')
    git(repo, 'update-index', '--add', '--cacheinfo', '160000,'+link+',references/Hexalith.EventStore')
    identities = json.loads((repo/tooling.contract_path(story)).read_bytes())['preservedContract']['predecessors']
    predecessor = dict(path='fixture-predecessor.json', sha256=digest((repo/'fixture-predecessor.json').read_bytes()))
    entry = dict(authorizationRevision=authorization, storyId=story, qualifications={key:'PASS' for key in tooling.ENTRY_REQUIREMENTS}, executionAllowed=True, ownerApprovalClaimed=True, predecessors=[dict(identity=i, files=[predecessor]) for i in identities])
    dump(repo, 'fixture-entry.json', entry)
    pin = digest((repo/'fixture-entry.json').read_bytes())
    binding = json.loads((repo/tooling.BINDING).read_bytes()); binding['entry'] = dict(path='fixture-entry.json',sha256=pin)
    dump(repo, tooling.BINDING, binding); dump(repo, 'fixture-entry-trust.json', binding['entry'])
    git(repo, 'add', '.'); git(repo, 'commit', '-qm', 'test: qualify disposable runtime consumer')
    candidate = git(repo, 'rev-parse', 'HEAD')
    binary = compile_evidence(base/'build', candidate, code)
    target = repo/'artifacts/native/evidence.dll'; target.parent.mkdir(parents=True); target.write_bytes(binary)
    path = native(repo, scenario); tree = ET.fromstring(path.read_bytes())
    output = tree.find('Results/UnitTestResult/Output/StdOut'); payload = json.loads(output.text)
    payload['sources'] = [dict(path=source,sha256=digest((repo/source).read_bytes()))]; output.text = json.dumps(payload)
    tree.find('TestDefinitions/UnitTest/TestMethod').set('codeBase', 'artifacts/native/evidence.dll')
    target = repo/tooling.native_path('runtime', scenario); target.parent.mkdir(parents=True); target.write_bytes(ET.tostring(tree))
    retain_capture(repo, scenario, binary)
    return repo, pin


@pytest.fixture
def qualified_runtime(tmp_path, runtime_template, monkeypatch):
    template, pin = runtime_template
    repo = tmp_path/'runtime'; shutil.copytree(template, repo)
    monkeypatch.setattr(tooling, 'TRUSTED_ENTRIES', {'fixture-entry.json': pin})
    return repo


def runtime_evaluate(repo):
    return tooling.evaluate(repo, '16.2', 'AC-16.2-02', scope='runtime')


def test_qualified_runtime_evaluate_binds_binary_dependency_canonicalizer_and_ancestor(qualified_runtime):
    result = runtime_evaluate(qualified_runtime)
    assert result['result'] == 'PASS', result
    assert result['runtimeQualified'] is True
    assert result['bindings']['entry']['authorizationRevision'] != result['candidate']['commit']
    assert result['bindings']['testBinary']['sha256'] == digest(retained_binary(qualified_runtime).read_bytes())
    assert result['bindings']['testBinary']['sourceRevision'] == result['candidate']['commit']
    assert result['bindings']['testBinary']['testFramework'] == 'xunit.v3.core, Version=4.0.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c'
    assert result['bindings']['canonicalizerSource'] == dict(path=tooling.CANONICALIZER_SOURCE,sha256=tooling.measurements.EVENTSTORE_CANONICALIZER_SHA256)
    assert result['candidate']['gitlinks'] == [dict(path='references/Hexalith.EventStore',commit=git(qualified_runtime/'references/Hexalith.EventStore','rev-parse','HEAD'))]
    assert not (qualified_runtime/tooling.receipt_path('runtime','AC-16.2-02')).exists()


@pytest.mark.parametrize('fault', ['absent', 'malformed', 'wrong-revision'])
def test_runtime_evaluator_admits_only_bound_native_binary(qualified_runtime, managed_evidence, fault):
    path = retained_binary(qualified_runtime)
    if fault == 'absent': path.unlink()
    else: retain_capture(qualified_runtime, 'AC-16.2-02', b'MZ invalid' if fault == 'malformed' else managed_evidence)
    rejected(runtime_evaluate(qualified_runtime), 'INPUT_UNAVAILABLE' if fault == 'absent' else 'NATIVE_BINARY_DRIFT', 'BLOCKED' if fault == 'absent' else 'FAIL')


def test_runtime_evaluator_calls_direct_dependency_cleanliness_guard(qualified_runtime):
    (qualified_runtime/tooling.CANONICALIZER_SOURCE).write_bytes(b'changed canonicalizer')
    rejected(runtime_evaluate(qualified_runtime), 'DEPENDENCY_DIRTY', 'BLOCKED')


def test_runtime_evaluator_calls_actual_canonicalizer_source_guard(qualified_runtime):
    dependency = qualified_runtime/'references/Hexalith.EventStore'
    (qualified_runtime/tooling.CANONICALIZER_SOURCE).write_bytes(b'wrong but clean source')
    git(dependency, 'add', '.'); git(dependency, 'commit', '-qm', 'test: alter disposable canonicalizer')
    git(qualified_runtime, 'add', 'references/Hexalith.EventStore'); git(qualified_runtime, 'commit', '-qm', 'test: bind changed disposable dependency')
    # Refresh genuine source-stamped binary and native candidate so the source guard is reached.
    candidate = git(qualified_runtime, 'rev-parse', 'HEAD')
    code = (qualified_runtime/'tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs').read_text()
    binary = compile_evidence(qualified_runtime.parent/'changed-build', candidate, code)
    (qualified_runtime/'artifacts/native/evidence.dll').write_bytes(binary)
    path = qualified_runtime/tooling.native_path('runtime','AC-16.2-02'); tree = ET.fromstring(path.read_bytes())
    output = tree.find('Results/UnitTestResult/Output/StdOut'); payload = json.loads(output.text); payload['candidate'] = candidate; output.text = json.dumps(payload); path.write_bytes(ET.tostring(tree))
    retain_capture(qualified_runtime, 'AC-16.2-02', binary)
    rejected(runtime_evaluate(qualified_runtime), 'CANONICALIZER_SOURCE_DRIFT')


@pytest.mark.parametrize('failure', ['temporary-directory', 'source-copy', 'build-spawn'])
def test_runtime_evaluate_turns_helper_setup_io_failures_into_structured_blocked(qualified_runtime, monkeypatch, failure):
    import epic16_native
    epic16_native._helper.cache_clear(); epic16_native.canonicalize.cache_clear()
    def unavailable(*args, **kwargs): raise OSError('injected helper setup failure')
    if failure == 'temporary-directory': monkeypatch.setattr(epic16_native.tempfile, 'TemporaryDirectory', unavailable)
    elif failure == 'source-copy': monkeypatch.setattr(Path, 'write_bytes', unavailable)
    else:
        original = epic16_native.subprocess.run
        def spawn(*args, **kwargs):
            if args[0][0] == 'dotnet': return unavailable()
            return original(*args, **kwargs)
        monkeypatch.setattr(epic16_native.subprocess, 'run', spawn)
    rejected(runtime_evaluate(qualified_runtime), 'NATIVE_HELPER_UNAVAILABLE', 'BLOCKED')


@pytest.mark.parametrize('path', ['_bmad/scripts/epic16_tooling.py', '_bmad/scripts/epic16_measurements.py', '_bmad/scripts/epic16_native_helper/Program.cs'])
def test_clean_different_repository_cannot_claim_unexecuted_bound_source_bytes(repository, path):
    target = repository/path; target.write_bytes(target.read_bytes()+b'\n# different bound source\n')
    bind(repository); git(repository, 'add', '.'); git(repository, 'commit', '-qm', 'test: bind different disposable source')
    rejected(evaluate(repository), 'EXECUTING_SOURCE_DRIFT')


@pytest.mark.parametrize('fault', ['missing', 'symlink'])
def test_partial_tooling_import_returns_structured_diagnostic(tmp_path, repository, fault):
    caller = tmp_path/'caller'
    for path in tooling._EXECUTABLE_PATHS:
        target = caller/path; target.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(ROOT/path,target)
    helper = caller/'_bmad/scripts/epic16_native_helper/Program.cs'; helper.unlink()
    if fault == 'symlink': helper.symlink_to(ROOT/'_bmad/scripts/epic16_native_helper/Program.cs')
    result = subprocess.run([sys.executable,str(caller/'_bmad/scripts/verify_story_16_1.py'),'--repository',str(repository),'--contract',tooling.contract_path('16.1'),'--scenario','AC-16.1-01','--scope',SCOPE],capture_output=True,timeout=30)
    document = json.loads(result.stdout)
    assert document['result'] == ('BLOCKED' if fault == 'missing' else 'FAIL'), document
    assert document['blockers'] and document['assertionLedger'] and b'Traceback' not in result.stderr


@pytest.mark.parametrize('shape', ['local-fact', 'internal-class', 'abstract-class', 'generic-class', 'private-method', 'static-method', 'generic-method', 'parameterized-method', 'not-test', 'skipped-fact', 'private-constructor'])
def test_metadata_rejects_fake_attributes_and_undiscoverable_protocol_shapes(tmp_path, shape):
    import epic16_native
    header = 'public sealed class Story16_1EvidenceTests'
    method = '[Xunit.Fact] public void Scenario01() {}'
    extra = ''
    if shape == 'internal-class': header = 'internal sealed class Story16_1EvidenceTests'
    elif shape == 'abstract-class': header = 'public abstract class Story16_1EvidenceTests'
    elif shape == 'generic-class': header = 'public class Story16_1EvidenceTests<T>'
    elif shape == 'private-method': method = '[Xunit.Fact] private void Scenario01() {}'
    elif shape == 'static-method': method = '[Xunit.Fact] public static void Scenario01() {}'
    elif shape == 'generic-method': method = '[Xunit.Fact] public void Scenario01<T>() {}'
    elif shape == 'parameterized-method': method = '[Xunit.Fact] public void Scenario01(int value) {}'
    elif shape == 'not-test': method = 'public void Scenario01() {}'
    elif shape == 'skipped-fact': method = '[Xunit.Fact(Skip="skipped")] public void Scenario01() {}'
    elif shape == 'private-constructor': extra = 'private Story16_1EvidenceTests() {}'
    content = compile_evidence(tmp_path/'build', 'a'*40, 'namespace Hexalith.Conversations.IntegrationTests.Epic16; '+header+' { '+extra+method+' }', fake=shape=='local-fact')
    with pytest.raises(ValueError): epic16_native.inspect_assembly(content,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')


@pytest.mark.parametrize('name,version', [('Fake.Framework','4.0.2.0'),('xunit.v3.core','3.0.0.0'),('xunit.v3.core','4.0.2.0')])
def test_external_fact_attribute_requires_pinned_framework_assembly_identity(tmp_path, name, version):
    import epic16_native
    framework = tmp_path/'framework'; framework.mkdir()
    (framework/'Framework.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>'+name+'</AssemblyName><AssemblyVersion>'+version+'</AssemblyVersion></PropertyGroup></Project>')
    (framework/'FactAttribute.cs').write_text('namespace Xunit; public sealed class FactAttribute(string path = "", int line = -1) : System.Attribute {}')
    directory = tmp_path/'evidence'; directory.mkdir()
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><ProjectReference Include="../framework/Framework.csproj" /></ItemGroup></Project>')
    (directory/'Tests.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] namespace Hexalith.Conversations.IntegrationTests.Epic16 { public sealed class Story16_1EvidenceTests { [Xunit.Fact] public void Scenario01() {} } }')
    result = subprocess.run(['dotnet','build','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=directory,capture_output=True,timeout=120)
    assert result.returncode == 0,result.stdout.decode()
    with pytest.raises(ValueError): epic16_native.inspect_assembly((directory/'bin/Debug/net10.0/Evidence.dll').read_bytes(),'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')


@pytest.mark.parametrize('path', ['_bmad/scripts/epic16_tooling.py', '_bmad/scripts/epic16_native_helper/Program.cs'])
def test_runtime_evaluator_rejects_clean_bound_sources_that_differ_from_executing_tree(qualified_runtime, path):
    target = qualified_runtime/path; target.write_bytes(target.read_bytes()+b'\n// differing source bytes\n')
    binding = json.loads((qualified_runtime/tooling.BINDING).read_bytes())
    for row in binding['sources']:
        if row['path'] == path: row['sha256'] = digest(target.read_bytes())
    dump(qualified_runtime, tooling.BINDING, binding)
    git(qualified_runtime, 'add', '.'); git(qualified_runtime, 'commit', '-qm', 'test: bind differing runtime consumer source')
    rejected(runtime_evaluate(qualified_runtime), 'EXECUTING_SOURCE_DRIFT')


def test_runtime_evaluator_requires_authorization_ancestry(qualified_runtime, monkeypatch):
    candidate = git(qualified_runtime, 'rev-parse', 'HEAD')
    entry = json.loads((qualified_runtime/'fixture-entry.json').read_bytes())
    entry['authorizationRevision'] = git(qualified_runtime, 'commit-tree', 'HEAD^{tree}', '-p', entry['authorizationRevision'], '-m', 'test: unrelated runtime authorization')
    dump(qualified_runtime, 'fixture-entry.json', entry)
    pin = digest((qualified_runtime/'fixture-entry.json').read_bytes())
    binding = json.loads((qualified_runtime/tooling.BINDING).read_bytes()); binding['entry']['sha256'] = pin; dump(qualified_runtime, tooling.BINDING, binding)
    monkeypatch.setattr(tooling, 'TRUSTED_ENTRIES', {'fixture-entry.json': pin})
    git(qualified_runtime, 'add', '.'); git(qualified_runtime, 'commit', '-qm', 'test: bind unrelated fixture entry')
    rejected(runtime_evaluate(qualified_runtime), 'ENTRY_ANCESTRY_INVALID')


@pytest.mark.parametrize('value', [1, 3155378975999999999])
def test_selected_utc_domain_includes_both_representable_boundaries(repository, value):
    data = deepcopy(OBSERVATIONS['AC-16.2-01'])
    # Position-only envelope has no Conversations contract's year-2000 lower bound.
    data['events'][1]['timestampTicks'] = data['events'][1]['binding']['selectedUtcTicks'] = value
    data['events'][1]['authoritativeEnvelope']['timestampUtcTicks'] = value
    native(repository, 'AC-16.2-01', data)
    assert evaluate(repository, 'AC-16.2-01')['result'] == 'PASS'


@pytest.mark.parametrize('value', [1, 65535])
def test_port_domain_includes_both_boundaries(repository, value):
    data = deepcopy(OBSERVATIONS['AC-16.3-02'])
    data.update(occupiedPort=value, effectivePorts=[value, 50002])
    data['endpointInventory'][1]['port']=value
    data['diagnostics'][0]['port']=value
    data['negativePreflight']['endpointInventory'][1]['port']=value
    data['negativePreflight']['occupancy'][1]['port']=value
    for phase in ('before','during','after'): data['occupancy'][phase][1]['port']=value
    native(repository, 'AC-16.3-02', data)
    assert evaluate(repository, 'AC-16.3-02')['result'] == 'PASS'


@pytest.mark.parametrize('shape', ['Task', 'ValueTask', 'output-constructor', 'class-fixture-constructor'])
def test_genuine_async_and_fixture_constructor_metadata_is_accepted_without_execution(tmp_path, shape):
    import epic16_native
    constructor = ''
    declaration = 'public sealed class Story16_1EvidenceTests'
    supporting = ''
    method = '[Xunit.Fact] public void Scenario01() { throw new System.Exception("Do not execute evidence"); }'
    if shape in ('Task','ValueTask'):
        method = '[Xunit.Fact] public async System.Threading.Tasks.'+shape+' Scenario01() { await System.Threading.Tasks.Task.Yield(); throw new System.Exception("Do not execute evidence"); }'
    elif shape == 'output-constructor':
        constructor = 'public Story16_1EvidenceTests(Xunit.ITestOutputHelper output) { throw new System.Exception("Do not execute evidence"); }'
    else:
        declaration += ' : Xunit.IClassFixture<Fixture>'
        constructor = 'public Story16_1EvidenceTests(Fixture fixture) { throw new System.Exception("Do not execute evidence"); }'
        supporting = 'public sealed class Fixture {}'
    binary = compile_evidence(tmp_path/'build', 'a'*40, 'namespace Hexalith.Conversations.IntegrationTests.Epic16; '+supporting+declaration+' { '+constructor+method+' }')
    result = epic16_native.inspect_assembly(binary,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')
    assert result['sourceRevision'] == 'a'*40 and result['method'] == 'Scenario01'


def refresh_replay(data):
    import epic16_native
    domain=tooling.measurements.domain
    state,times=domain.conversation_replay(data['authoritativeEvents'])
    items=domain.collection_items(state)
    data['contributingEventTicks']=times;data['collectionKeys']=[item['key'] for item in items]
    value=epic16_native.canonicalize(json.dumps(dict(version=2,anchorUtcTicks=max(times),items=items)))
    for row in data['rebuilds']: row.update(canonicalJson=value,roundTripJson=value,stateHash=digest(value.encode()),anchorTicks=max(times),lastAppliedTicks=max(times),lagTicks=row['generatedTicks']-max(times))


@pytest.mark.parametrize('encoding', ['utf-16', 'utf-16-be'])
def test_encoded_dtd_is_rejected_before_shared_native_reader(repository, encoding):
    path=native(repository,'AC-16.1-01')
    document=path.read_bytes().decode().replace('<TestRun>', '<!DOCTYPE TestRun [<!ENTITY injected "payload">]><TestRun>',1)
    path.write_bytes(('<?xml version="1.0" encoding="'+encoding+'"?>'+document).encode(encoding))
    rejected(evaluate(repository),'NATIVE_INVALID')
    command=[sys.executable,str(repository/'_bmad/scripts/verify_story_16_1.py'),'--repository',str(repository),'--contract',tooling.contract_path('16.1'),'--scenario','AC-16.1-01','--scope',SCOPE]
    run=subprocess.run(command,capture_output=True,timeout=30)
    assert run.returncode==1 and json.loads(run.stdout)['blockers']==['NATIVE_INVALID'] and b'Traceback' not in run.stderr


def test_unknown_native_encoding_is_structured_in_evaluator_and_cli(repository):
    path=native(repository,'AC-16.1-01');path.write_bytes(b'<?xml version="1.0" encoding="unknown-native-codec"?>'+path.read_bytes())
    rejected(evaluate(repository),'NATIVE_INVALID')
    run=subprocess.run([sys.executable,str(repository/'_bmad/scripts/verify_story_16_1.py'),'--repository',str(repository),'--contract',tooling.contract_path('16.1'),'--scenario','AC-16.1-01','--scope',SCOPE],capture_output=True,timeout=30)
    assert run.returncode==1 and json.loads(run.stdout)['blockers']==['NATIVE_INVALID'] and b'Traceback' not in run.stderr


def test_real_pinned_xunit_execution_emits_untouched_consumable_trx(repository,tmp_path):
    scenario='AC-16.1-01';path=native(repository,scenario)
    payload=json.loads(ET.fromstring(path.read_bytes()).find('Results/UnitTestResult/Output/StdOut').text)
    directory=tmp_path/'real-native';directory.mkdir()
    project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>'
    (directory/'Evidence.csproj').write_text(project)
    (directory/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+payload['candidate']+'")]')
    code='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_1EvidenceTests(Xunit.ITestOutputHelper output) { [Xunit.Fact] public void Scenario01() { output.WriteLine("""'+json.dumps(payload)+'"""); } }'
    (directory/'Tests.cs').write_text(code)
    build=subprocess.run(['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=directory,capture_output=True,timeout=120)
    assert build.returncode==0,build.stdout.decode()
    native_directory=repository/'artifacts/native-real';shutil.copytree(directory/'bin/Debug/net10.0',native_directory)
    result=subprocess.run(['dotnet',str(native_directory/'Evidence.dll'),'-automated','sync','-failSkips','-method','Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests.Scenario01','-trx',str(path)],capture_output=True,timeout=120)
    assert result.returncode==0,(result.stdout,result.stderr)
    untouched=path.read_bytes()
    with tooling.ContainedReader(repository) as reader:
        binary=tooling.native_binary(reader,untouched,payload['candidate'],scenario)
    assert binary['sourceRevision']==payload['candidate'] and binary['method']=='Scenario01'
    observed=evaluate(repository,scenario)
    assert observed['result']=='PASS',observed
    assert observed['bindings']['native']['sha256']==digest(untouched) and path.read_bytes()==untouched
    assert records.parse_trx(untouched)['results'][0]['test']=='Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests.Scenario01'
    # Keep the real runner's successful test/counters untouched while adding a
    # run-level cleanup failure: it must fail the actual consumer entry path.
    tree=ET.fromstring(untouched);summary=tree.find('./{*}ResultSummary')
    ET.SubElement(ET.SubElement(summary,'RunInfos'),'RunInfo',outcome='Error').text='Native cleanup failed'
    try:
        path.write_bytes(ET.tostring(tree));rejected(evaluate(repository,scenario),'NATIVE_RUN_FAILED')
    finally:path.write_bytes(untouched)


def test_generator_freezes_symbolic_candidate_before_all_evaluations(repository,monkeypatch):
    pinned=git(repository,'rev-parse','HEAD');seen=[];original=tooling.evaluate
    def advancing(repo,story,scenario,candidate,scope,**kwargs):
        seen.append(candidate)
        value=original(repo,story,scenario,candidate,scope,**kwargs)
        if len(seen)==1:
            git(repo,'commit','--allow-empty','-qm','test: advance disposable candidate')
            for ac in tooling.measurements.SCENARIOS:native(repo,ac)
        return value
    monkeypatch.setattr(tooling,'evaluate',advancing)
    result,j,m=tooling.generate(repository,'16.1',scope=SCOPE)
    assert result['result']=='FAIL' and j is m is None
    assert seen and all(value==pinned for value in seen)


def test_generator_rejects_any_mixed_candidate_return_even_with_passing_rows(repository,monkeypatch):
    original=tooling.evaluate
    def altered(*args,**kwargs):
        result=original(*args,**kwargs)
        if args[2]=='AC-16.1-03':result['candidate']['commit']='f'*40
        return result
    monkeypatch.setattr(tooling,'evaluate',altered)
    result,j,m=tooling.generate(repository,'16.1',scope=SCOPE)
    rejected(result,'CANDIDATE_DRIFT');assert j is m is None


@pytest.mark.parametrize('path,value', [((0,'value','label'),'changed content'),((1,'value','folderId'),'changed-folder'),((2,'value','text'),'different text'),((3,'value','participantRole'),'Observer')])
def test_same_id_changed_native_domain_values_cannot_pass_replay(repository,path,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-02']);state=json.loads(data['rebuilds'][0]['canonicalJson']);target=state['items']
    for part in path[:-1]:target=target[part]
    target[path[-1]]=value
    raw=tooling.native_helper.canonicalize(json.dumps(state))
    assert digest(raw.encode()) != data['rebuilds'][0]['stateHash']
    for row in data['rebuilds']:row.update(canonicalJson=raw,roundTripJson=raw,stateHash=digest(raw.encode()))
    native(repository,'AC-16.2-02',data);rejected(evaluate(repository,'AC-16.2-02'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('index,key,value',[(4,'label','new label'),(1,'participantRole','Observer'),(2,'text','new message'),(3,'folderId','folder:new-folder')])
def test_authoritative_domain_content_changes_hash_without_identity_changes(repository,index,key,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-02']);original=data['rebuilds'][0]['stateHash'];keys=data['collectionKeys'][:]
    event=data['authoritativeEvents'][index];body=json.loads(event['payload']);body[key]=value;event['payload']=json.dumps(body)
    refresh_replay(data)
    assert data['collectionKeys']==keys and data['rebuilds'][0]['stateHash']!=original
    native(repository,'AC-16.2-02',data);assert evaluate(repository,'AC-16.2-02')['result']=='PASS'


@pytest.mark.parametrize('field,value',[('tenant','other'),('conversation','other'),('eventType','Other'),('sequence',0),('serializationFormat','unknown'),('payloadSha256','f'*64),('selectedUtcTicks',2000)])
def test_every_duplicate_binding_component_matches_retained_envelope(repository,field,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);data['events'][0]['binding'][field]=value
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('mutation',['codec','metadata','event-type','payload-shape','deduplication-key','created-at'])
def test_authoritative_envelope_requires_actual_contract_shape(repository,mutation):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=data['events'][0];env=row['authoritativeEnvelope'];body=json.loads(env['payload'])
    if mutation=='codec':env['serializationFormat']=row['binding']['serializationFormat']='avro'
    elif mutation=='metadata':body['metadata']['tenantId']='foreign'
    elif mutation=='event-type':env['eventType']=row['binding']['eventType']='MessageCreated'
    elif mutation=='payload-shape':body['fakeField']=True
    elif mutation=='deduplication-key':body['metadata']['deduplicationKey']='forged'
    else:body['createdAt']='2026-10-25T13:00:01Z'
    row['persistedPayload']=env['payload']=json.dumps(body);row['binding']['payloadSha256']=digest(env['payload'].encode())
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['omit','conflict','relabel-pending','relabel-all','recovered-domain','starting-state','foreign'])
def test_convergence_derives_state_from_contiguous_authoritative_tenant_events(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-03'])
    if fault=='omit':data['authoritativeReplay'].pop(1)
    elif fault=='conflict':data['authoritativeReplay'][1]['sequence']=11
    elif fault=='recovered-domain':data['recoveredState']['status']='Active'
    elif fault=='starting-state':data['startingState']['configuration']['conversations.enabled']='false'
    elif fault=='foreign':
        row=data['authoritativeReplay'][1];body=json.loads(row['persistedPayload']);body['tenantId']='foreign';row['persistedPayload']=json.dumps(body);row['payloadSha256']=digest(row['persistedPayload'].encode())
    else:
        for merge in data['merges']:
            for row in merge['pending']:
                row['eventType']='TenantEnabled';row['persistedPayload']=json.dumps(dict(tenantId='tenant-a',enabledAt='2026-10-25T12:00:01Z'));row['payloadSha256']=digest(row['persistedPayload'].encode())
        if fault=='relabel-all':data['authoritativeReplay'][1:]=deepcopy(data['merges'][-1]['pending'])
    native(repository,'AC-16.1-03',data);rejected(evaluate(repository,'AC-16.1-03'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['same-stale-values','pending-values','prior-values','trace-omission','checkpoint-only'])
def test_reconciliation_requires_pending_event_derived_domain_values(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-04'])
    if fault=='same-stale-values':
        for key in ('persistedProjection','queryProjection'):data[key]['state']=deepcopy(data['beforeProjection'])
    elif fault=='pending-values':
        body=json.loads(data['pendingEvent']['payload']);body['label']='unrelated correction';data['pendingEvent']['payload']=json.dumps(body)
    elif fault=='prior-values':data['beforeProjection']['label']='different predecessor'
    elif fault=='trace-omission':data['recoveryEvents']=[]
    else:
        data['afterCheckpoint']+=1
        for key in ('persistedProjection','queryProjection'):data[key]['sequence']=data['afterCheckpoint']
    native(repository,'AC-16.3-04',data);rejected(evaluate(repository,'AC-16.3-04'),'RECONCILIATION_IDENTITY_DRIFT' if fault=='pending-values' else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('story',['16.1','16.2'])
@pytest.mark.parametrize('fault',['matching-forged','omit-inventory','candidate-source','extra-source','before-source'])
def test_public_preservation_reads_complete_independent_git_source_inventories(repository,story,fault):
    scenario=f'AC-{story}-05';data=deepcopy(OBSERVATIONS[scenario]);source=repository/'src/Hexalith.Conversations.Contracts/Contract.cs'
    if fault=='matching-forged':data.update(beforeContractSha256='f'*64,afterContractSha256='f'*64)
    elif fault=='omit-inventory':data['contractSourcesBefore']=data['contractSourcesAfter']=[]
    else:
        if fault=='extra-source':(source.parent/'Extra.cs').write_text('public class Extra {}\n')
        elif fault=='before-source':source.write_text('public class BeforeDrift {}\n')
        else:source.write_text('public class CandidateDrift {}\n')
        git(repository,'add','src');git(repository,'commit','-qm','test: alter disposable contract surface')
        if fault=='before-source':
            source.write_text('namespace Fixture; public sealed record Contract(string Value);\n');git(repository,'add','src');git(repository,'commit','-qm','test: restore candidate with drifted baseline')
        for ac in tooling.measurements.SCENARIOS:native(repository,ac)
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'PUBLIC_SURFACE_DRIFT')


@pytest.mark.parametrize('scenario,field,value',[('AC-16.3-01','timeoutMs',10**12),('AC-16.3-01','attemptLimit',10**12),('AC-16.3-05','cancelBoundMs',10**12)])
def test_observations_cannot_nominate_control_policy_limits(repository,scenario,field,value):
    data=deepcopy(OBSERVATIONS[scenario]);data[field]=value
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['unrelated-snapshots','wrong-port','wrong-owner','wrong-endpoint','during-unoccupied','unrelated-owner-changed'])
def test_port_collision_proves_affected_occupancy_and_unrelated_owner_restoration(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-02'])
    if fault=='unrelated-snapshots':data['portsBefore']=data['portsAfter']=[123]
    elif fault=='wrong-port':data['occupancy']['during'][1]['port']=123
    elif fault=='wrong-owner':data['occupancy']['during'][1]['owner']='other'
    elif fault=='wrong-endpoint':data['endpoint']='conversations-http'
    elif fault=='during-unoccupied':data['occupancy']['during']=deepcopy(data['occupancy']['before'])
    else:data['occupancy']['after'][-1]['owner']='replacement-process'
    native(repository,'AC-16.3-02',data);rejected(evaluate(repository,'AC-16.3-02'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario',['AC-16.3-01','AC-16.3-02','AC-16.3-05'])
@pytest.mark.parametrize('fault',['empty','tenant','token','extra','unbound-endpoint'])
def test_actual_structured_diagnostics_have_safe_exact_fields_and_bound_identity(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario])
    if fault=='empty':data['diagnostics']=[]
    elif fault=='extra':data['diagnostics'][0]['payload']='secret'
    elif fault=='unbound-endpoint':data['diagnostics'][0]['endpointId']='other-endpoint'
    else:data['diagnostics'][0]['endpointId']='tenant@example.invalid' if fault=='tenant' else 'Bearer confidential-token'
    native(repository,scenario,data);result=evaluate(repository,scenario)
    assert result['result']=='FAIL' and result['blockers'][0] in ('MEASUREMENT_INVALID','MEASUREMENT_SCHEMA_INVALID')


FAULT_IDENTITIES=[('AC-16.1-04',name) for name in tooling.measurements.TENANT_FAULTS]+[('AC-16.3-05',name) for name in tooling.measurements.LIVE_FAULTS]
@pytest.mark.parametrize('scenario,name',FAULT_IDENTITIES)
@pytest.mark.parametrize('fault',['no-injection','wrong-target','forged-ledger','common-restoration','wrong-outcome'])
def test_each_fault_requires_distinguishing_injection_target_measured_ledger_and_restoration(repository,scenario,name,fault):
    data=deepcopy(OBSERVATIONS[scenario]);row=next(r for r in data['faults'] if r['id']==name)
    if fault=='no-injection':row['injectedBytes']=row['beforeBytes']
    elif fault=='wrong-target':row['target']['id']='unrelated'
    elif fault=='forged-ledger':row['assertions']=1;row['assertionEntries']=row['assertionEntries'][:1]
    elif fault=='wrong-outcome':
        target=row['observed'];key=next(iter(target))
        if isinstance(target[key],dict):target=target[key];key=next(iter(target))
        target[key]=not target[key] if isinstance(target[key],bool) else target[key]+1 if isinstance(target[key],int) else 'unmeasured'
    else:row['beforeBytes']=row['afterBytes']='common bytes';row['beforeSha256']=row['afterSha256']=digest(b'common bytes')
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


def test_domain_source_attribution_is_retained_without_submodule_reads():
    rows=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-domain-sources.json').read_bytes())['sources']
    assert any(r['path'].endswith('/ConversationProjectionMaterializer.cs') for r in rows)
    assert any(r['path'].endswith('/TenantReadModel.cs') for r in rows)
    for row in rows: assert digest(row['source'].encode())==row['sha256'] and len(row['revision'])==40


@pytest.mark.parametrize('value',['2026-10-25T12:00:01+00:00','2026-10-25T12:00:01Z','2026-10-25T14:00:01+02:00','2026-10-25T12:00:01.0000000Z'])
def test_real_datetimeoffset_forms_convert_to_same_exact_utc_ticks(value):
    assert tooling.measurements.domain.timestamp(value)==tooling.measurements.domain.timestamp('2026-10-25T12:00:01Z')


def test_actual_serialized_contract_goldens_and_dates_are_consumed_without_wire_rewriting():
    goldens=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-contract-wire-goldens.json').read_bytes())
    events=OBSERVATIONS['AC-16.2-02']['authoritativeEvents']
    assert [row['payload'] for row in events]==[row['payload'] for row in goldens['events']]
    state,_=tooling.measurements.domain.conversation_replay(events)
    assert state['businessReference']==dict(system='case',value='b')
    assert state['participants'][0]['participantRole']=='Member'
    for row in goldens['dateTimes']:
        assert tooling.measurements.domain.timestamp(json.loads(row['json']))==row['utcTicks']


def test_fault_bytes_reject_boolean_integer_coercion(repository):
    data=deepcopy(OBSERVATIONS['AC-16.3-05']);row=data['faults'][0]
    row=next(r for r in data['faults'] if r['id']=='unauthorized')
    value=json.loads(row['injectedBytes']);value['state']['authenticated']=0;row['injectedBytes']=json.dumps(value)
    native(repository,'AC-16.3-05',data);rejected(evaluate(repository,'AC-16.3-05'),'MEASUREMENT_INVALID')


def test_coincident_rebuild_completion_instants_preserve_ad8_semantic_freshness(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-02'])
    for key in ('generatedTicks','completionTicks','lagTicks'):data['rebuilds'][1][key]=data['rebuilds'][0][key]
    native(repository,'AC-16.2-02',data)
    assert evaluate(repository,'AC-16.2-02')['result']=='PASS'


@pytest.mark.parametrize('schedule',[1,2,3])
def test_each_reverse_schedule_requires_its_actual_authoritative_event_effect(repository,schedule):
    data=deepcopy(OBSERVATIONS['AC-16.1-03'])
    data['reverseSaves'][schedule]['authoritativeReplay']=deepcopy(data['authoritativeReplay'])
    native(repository,'AC-16.1-03',data);rejected(evaluate(repository,'AC-16.1-03'),'MEASUREMENT_INVALID')


def test_live_cleanup_inventory_is_bound_to_the_measured_collision_target(repository):
    data=deepcopy(OBSERVATIONS['AC-16.3-05']);data['portsBefore']=data['portsAfter']=[123]
    native(repository,'AC-16.3-05',data);rejected(evaluate(repository,'AC-16.3-05'),'MEASUREMENT_INVALID')

@pytest.mark.parametrize('fault',['whitespace','property-order'])
def test_expanded_domain_state_still_calls_actual_eventstore_canonicalizer(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-02'])
    value=json.loads(data['rebuilds'][0]['canonicalJson'])
    if fault=='whitespace':raw='{ '+data['rebuilds'][0]['canonicalJson'][1:]
    else:raw=json.dumps(dict(version=value['version'],anchorUtcTicks=value['anchorUtcTicks'],items=value['items']),ensure_ascii=True,separators=(',',':'))
    assert json.loads(raw)==value
    for row in data['rebuilds']:row.update(canonicalJson=raw,roundTripJson=raw,stateHash=digest(raw.encode()))
    native(repository,'AC-16.2-02',data);rejected(evaluate(repository,'AC-16.2-02'),'MEASUREMENT_INVALID')

@pytest.mark.parametrize('fault',['port','attempts','replica-domain','foreign-recovery'])
def test_fault_measurements_correlate_complete_targets_and_emitted_observations(repository,fault):
    scenario='AC-16.3-05' if fault in ('port','attempts') else 'AC-16.1-04'
    data=deepcopy(OBSERVATIONS[scenario])
    name={'port':'port-collision','attempts':'endpoint-race','replica-domain':'two-replicas','foreign-recovery':'recovery-success'}[fault]
    row=next(r for r in data['faults'] if r['id']==name)
    if fault=='port':
        for key in ('beforeBytes','injectedBytes','afterBytes'):
            value=json.loads(row[key]);value['state']['port']=50004;row[key]=json.dumps(value)
        row['beforeSha256']=digest(row['beforeBytes'].encode());row['afterSha256']=digest(row['afterBytes'].encode());row['observed']['collisionPort']=50004;data['portsBefore']=data['portsAfter']=[50004]
    elif fault=='attempts':row['observed']['attempts']=1
    elif fault=='replica-domain':row['observed']['replicaA']['domain']['configuration']['conversations.enabled']='different'
    else:
        before=json.loads(row['beforeBytes']);before['state']['domain']['tenantId']='foreign';row['beforeBytes']=row['afterBytes']=json.dumps(before);row['beforeSha256']=row['afterSha256']=digest(row['beforeBytes'].encode())
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')

@pytest.mark.parametrize('value',[True,1.0])
def test_raw_reverse_save_order_rejects_boolean_and_float_integer_aliases(repository,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['faults'][0]
    for key in ('beforeBytes','injectedBytes','afterBytes'):
        body=json.loads(row[key]);body['state']['versions']=[1,2]
        body['state']['saveOrder']=[2,value] if key=='injectedBytes' else [value,2]
        row[key]=json.dumps(body)
    row['observed']['version']=2
    row['beforeSha256']=digest(row['beforeBytes'].encode());row['afterSha256']=digest(row['afterBytes'].encode())
    native(repository,'AC-16.1-04',data);rejected(evaluate(repository,'AC-16.1-04'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field,value',[('name',True),('description',False),('members',{'actor-a':True}),('configuration',{'conversations.enabled':True})])
def test_raw_fault_starting_tenant_domain_values_keep_actual_primitive_types(repository,field,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=next(r for r in data['faults'] if r['id']=='recovery-success')
    body=json.loads(row['beforeBytes']);body['state']['domain'][field]=value
    row['beforeBytes']=row['afterBytes']=json.dumps(body);row['beforeSha256']=row['afterSha256']=digest(row['beforeBytes'].encode())
    native(repository,'AC-16.1-04',data);rejected(evaluate(repository,'AC-16.1-04'),'MEASUREMENT_INVALID')

@pytest.mark.parametrize('value',['2026-01-02T03:04:05.١Z','2026-01-02T03:04:05+00:60','2026-01-02T03:04:05-01:99','2026-01-02T03:04:05+14:01','2026-01-02T03:04:05-15:00','9999-12-31T23:59:59.9999999-00:01'])
def test_native_datetimeoffset_rejects_invalid_offsets_and_utc_overflow_stably(repository,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=data['events'][0];env=row['authoritativeEnvelope'];body=json.loads(env['payload'])
    body['metadata']['occurredAt']=body['createdAt']=value
    row['persistedPayload']=env['payload']=json.dumps(body);row['binding']['payloadSha256']=digest(env['payload'].encode())
    native(repository,'AC-16.2-01',data)
    rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_INVALID')
    with pytest.raises(ValueError):tooling.measurements.domain.timestamp(value)


def test_datetimeoffset_offset_limits_preserve_exact_utc_arithmetic():
    oracle=tooling.measurements.domain.timestamp
    assert oracle('2026-01-02T14:00:00+14:00')==oracle('2026-01-02T00:00:00Z')
    assert oracle('2026-01-01T10:00:00-14:00')==oracle('2026-01-02T00:00:00Z')
    assert oracle('9999-12-31T23:59:59.9999999+14:00')==3155378975999999999-14*3600*10000000


@pytest.mark.parametrize('value', ['', ' ', '\t'])
def test_round4_tenant_configuration_values_preserve_actual_notnull_semantics(value):
    row=deepcopy(OBSERVATIONS['AC-16.1-03']['authoritativeReplay'][-1])
    row.update(sequence=1,eventType='TenantConfigurationSet',persistedPayload=json.dumps(dict(tenantId='tenant-a',key='configuration-control',value=value)))
    row['payloadSha256']=digest(row['persistedPayload'].encode())
    state,_=tooling.measurements.domain.tenant_replay(OBSERVATIONS['AC-16.1-03']['startingState'],[row],0,1)
    assert state['configuration']['configuration-control']==value


@pytest.mark.parametrize('value', ['0001-01-01T00:00:00Z','1999-12-31T23:59:59Z','2000-01-01T00:00:00Z'])
def test_round4_tenant_dates_use_general_native_datetimeoffset(value):
    row=deepcopy(OBSERVATIONS['AC-16.1-03']['authoritativeReplay'][0]);row['sequence']=1
    payload=json.loads(row['persistedPayload']);payload['updatedAt']=value;row['persistedPayload']=json.dumps(payload);row['payloadSha256']=digest(row['persistedPayload'].encode())
    tooling.measurements.domain.tenant_replay(OBSERVATIONS['AC-16.1-03']['startingState'],[row],0,1)


def test_round4_genuine_terminal_and_redaction_serialization_and_key_catalog_goldens():
    domain=tooling.measurements.domain;relations=tooling.measurements.relationships
    goldens=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-contract-wire-goldens.json').read_bytes())
    assert len(goldens['additionalEvents'])==5
    for row in goldens['additionalEvents']:
        envelope=dict(tenant='tenant-a',conversation='conversation-a',sequence=1,eventType=row['eventType'],messageId='transport',timestampUtcTicks=1,serializationFormat='json',payload=row['payload'])
        binding,_,payload=domain.conversation_envelope(envelope)
        assert binding['eventType']==row['eventType']
        if row['eventType']!='MessageContentRedacted': assert 'reasonCode' in payload
    vectors=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-protocol-goldens.json').read_bytes())
    for row in vectors['keys']:
        assert relations.conversation_key(row['tenant'],row['resource'])==row['key']
        assert relations.index_key(row['tenant'])==row['index']
    assert len({row['key'] for row in vectors['keys']})==len(vectors['keys'])
    for row in vectors['catalogs']:
        assert relations.catalog_fingerprint(row['appId'],row['serviceVersion'],row['routes'],row['ordinary'])==row['fingerprint']


@pytest.mark.parametrize('field',['occurredAtUtcTicks','sequence'])
@pytest.mark.parametrize('value_kind',['float','boolean'])
def test_round4_recursive_domain_values_cannot_alias_native_integer_types(repository,field,value_kind):
    scenario='AC-16.2-02' if field=='occurredAtUtcTicks' else 'AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario])
    if field=='occurredAtUtcTicks':
        for row in data['rebuilds']:
            value=json.loads(row['canonicalJson']);item=next(i['value'] for i in value['items'] if 'occurredAtUtcTicks' in i['value']);item[field]=float(item[field]) if value_kind=='float' else True
            row['canonicalJson']=tooling.measurements.canonicalize(json.dumps(value));row['roundTripJson']=row['canonicalJson'];row['stateHash']=digest(row['canonicalJson'].encode())
    else:
        for key in ('persistedProjection','queryProjection'):data[key][field]=float(data[key][field]) if value_kind=='float' else True
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_SCHEMA_INVALID' if field=='sequence' else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario',['AC-16.1-01','AC-16.1-05','AC-16.2-05'])
@pytest.mark.parametrize('fault',['keys','same-tenant','other-write','wrong-read','foreign-read','same-content','wrong-resource'])
def test_round4_measured_tenant_isolation_derives_keys_and_scoped_operations(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario]);p=data['isolation']
    if fault=='keys':data['tenantKeys']=['unsegmented-a','unsegmented-b']
    elif fault=='same-tenant':p['tenants'][1]=p['tenants'][0]
    elif fault=='other-write':p['writes'][0]['after'][data['tenantKeys'][1]]='changed other tenant'
    elif fault=='wrong-read':p['reads'][0]['value']=p['reads'][1]['value']
    elif fault=='foreign-read':p['reads'][2].update(result='Current',value=p['reads'][1]['value'],key=data['tenantKeys'][1])
    elif fault=='same-content':p['after'][data['tenantKeys'][1]]=p['after'][data['tenantKeys'][0]]
    else:p['writes'][0]['resource']='different-resource'
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('key',['projection:conversations-index:','projection:conversations-index:tenant-a','projection:conversations-index:dGVuYW50LWI'])
def test_round4_query_authority_is_the_complete_measured_tenant_key(repository,key):
    data=deepcopy(OBSERVATIONS['AC-16.2-03']);data['authorityKey']=key
    native(repository,'AC-16.2-03',data);rejected(evaluate(repository,'AC-16.2-03'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field',tooling.measurements.BINDING_FIELDS+('missing-id','invalid-time'))
@pytest.mark.parametrize('fault',['unchanged','wrong-field','omitted'])
def test_round4_conflict_labels_require_the_named_original_reused_inputs(repository,field,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=next(r for r in data['conflicts'] if r['id']==field)
    if fault=='unchanged':row['reused']=deepcopy(row['original'])
    elif fault=='omitted':row.pop('original')
    else:
        row['reused']=deepcopy(row['original']);row['reused']['sequence']+=1
        if field=='sequence':row['reused']['tenant']='foreign'
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_SCHEMA_INVALID' if fault=='omitted' else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field',['state','applied','timestamp','payload'])
def test_round4_contract_time_precedence_is_measured_from_both_deliveries(repository,field):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);p=data['transportTimeProof']
    if field=='state':p['state']='Unavailable'
    elif field=='applied':p['applied']=True
    elif field=='timestamp':p['reused']['timestampUtcTicks']=p['original']['timestampUtcTicks']
    else:p['reused']['payload']=p['original']['payload'].replace('Seed conversation','Different content')
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('name',['next','duplicate','conflict','older-match','older-missing','older-conflict','older-unavailable'])
@pytest.mark.parametrize('fault',['wrong-event','history','split-write','failed-cas','healthy-persisted'])
def test_round4_tenant_transition_inputs_and_composite_cas_are_measured(repository,name,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);row=next(r for r in data['transitions'] if r['id']==name);p=row['proof']
    if fault=='wrong-event':p['delivered']['messageId']='wrong-event' if name in ('duplicate','older-match') else p['accepted']['messageId'];p['delivered']['sequence']=p['before']['checkpoint']+4
    elif fault=='history':p['historyState']='Unavailable' if p['historyState']!='Unavailable' else 'Available'
    elif fault=='split-write':p['cas']['checkpointCommitId']='split-transaction'
    elif fault=='failed-cas':p['cas']['succeeded']=False
    else:
        if name in ('conflict','older-missing','older-conflict','older-unavailable'):p['after']['health']='Healthy'
        else:p['after']['projection']['name']='unmeasured change'
    native(repository,'AC-16.1-01',data);rejected(evaluate(repository,'AC-16.1-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['stale-text','missing-target','foreign-target','different-query','attribution','source-rewrite','control-mask','trust'])
def test_round4_redaction_preserves_source_history_masking_attribution_and_control(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-05']);p=data['redactionProof']
    if fault=='stale-text':
        for key in ('persistedAfter','queryAfter'):p[key]['state']['messages'][0]['text']='Controlled message'
    elif fault=='missing-target':p['targetMessage']='missing'
    elif fault=='foreign-target':p['redactionEvent']['tenant']='foreign'
    elif fault=='different-query':p['queryAfter']['state']['messages'][0]['text']='query-only'
    elif fault=='attribution':
        for key in ('persistedAfter','queryAfter'):p[key]['redactions'][0]['actorPartyId']='party:changed'
    elif fault=='source-rewrite':p['sourceAfter'][2]['payload']=p['sourceAfter'][2]['payload'].replace('Controlled message','[redacted]')
    elif fault=='control-mask':
        for key in ('persistedAfter','queryAfter'):p[key]['state']['messages'][1]['text']='[redacted]'
    else:
        for key in ('persistedAfter','queryAfter'):p[key]['messageTrust'][0]['trustState']='Current'
    native(repository,'AC-16.2-05',data);rejected(evaluate(repository,'AC-16.2-05'),'MEASUREMENT_INVALID')


def test_round4_general_replay_never_silently_ignores_redaction():
    with pytest.raises(ValueError,match='separate bounded redaction'):
        tooling.measurements.domain.conversation_replay(OBSERVATIONS['AC-16.2-05']['redactionProof']['sourceAfter'])


@pytest.mark.parametrize('scenario',['AC-16.1-05','AC-16.2-05'])
@pytest.mark.parametrize('fault',['event-leak','log-leak','nested-log','missing-logs','missing-events','wrong-tenant','missing-canary','wrong-event'])
def test_round4_emitted_events_logs_are_inspected_against_tenant_canary_experiment(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario]);p=data['tenantDataExperiment'];canary=p['canaries']['name']
    if fault=='event-leak':
        value=json.loads(p['emittedEvents'][0]);value['label']=canary;p['emittedEvents'][0]=json.dumps(value)
    elif fault in ('log-leak','nested-log'):
        value=json.loads(p['emittedLogs'][0]);value['message' if fault=='log-leak' else 'extra']=canary if fault=='log-leak' else {'nested':canary};p['emittedLogs'][0]=json.dumps(value)
    elif fault=='missing-logs':p['emittedLogs']=[]
    elif fault=='missing-events':p['emittedEvents']=[]
    elif fault=='wrong-tenant':p['tenant']='foreign'
    elif fault=='missing-canary':p['canaries']['name']='unrelated-canary-name'
    else:p['emittedEvents'][1]=p['emittedEvents'][0]
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario',['AC-16.3-03','AC-16.3-04'])
@pytest.mark.parametrize('fault',['zero-hash','changed-bytes','foreign-runtime','foreign-route','missing-route','duplicate-route','wire-route','pending-runtime','pending-sequence','fingerprint'])
def test_round4_catalog_bytes_frame_actual_runtime_route_and_pending_identity(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario]);catalog=json.loads(data['catalogBytes'])
    if fault=='zero-hash':data['catalogSha256']='0'*64
    elif fault=='changed-bytes':data['catalogBytes']+=' '
    elif fault in ('foreign-runtime','foreign-route','missing-route','duplicate-route'):
        if fault=='foreign-runtime':catalog['appId']='foreign';data['runtimeId']='foreign@1.0.0'
        elif fault=='foreign-route':catalog['domains'][0]['namedProjectionTypes']=['foreign'];catalog['domains'][0]['ordinaryProjectionTypes']=['foreign']
        elif fault=='missing-route':catalog['domains'][0]['namedProjectionTypes']=[];catalog['domains'][0]['ordinaryProjectionTypes']=[]
        else:catalog['domains'][0]['namedProjectionTypes']*=2
        data['catalogBytes']=json.dumps(catalog);data['catalogSha256']=digest(data['catalogBytes'].encode())
    elif fault=='wire-route':data['wireRoutes'][1]='/project/v2/foreign'
    elif fault=='pending-runtime':data['pendingLocation']['appId']='foreign'
    elif fault=='pending-sequence':data['pendingLocation']['sequence']+=1
    else:data['catalogFingerprint']='0'*64
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'RECONCILIATION_IDENTITY_DRIFT' if scenario=='AC-16.3-04' else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('name',['restart','three-future-deliveries','horizon-extension'])
@pytest.mark.parametrize('value',[-10,2**63,True,1.0])
def test_round4_raw_fault_sequence_domains_are_native_representable(repository,name,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=next(r for r in data['faults'] if r['id']==name)
    for key in ('beforeBytes','injectedBytes','afterBytes'):
        obj=json.loads(row[key]);state=obj['state']
        if name=='restart':state['snapshot']['sequence']=value;state['snapshot']['pending']=[value+2]
        else:state['checkpoint']=value
        row[key]=json.dumps(obj)
    row['beforeSha256']=digest(row['beforeBytes'].encode());row['afterSha256']=digest(row['afterBytes'].encode())
    native(repository,'AC-16.1-04',data);rejected(evaluate(repository,'AC-16.1-04'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('value',[-1,2**63,True,1.0])
def test_round4_envelope_sequence_uses_native_positive_int64(repository,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=data['events'][0];row['binding']['sequence']=row['authoritativeEnvelope']['sequence']=value
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_SCHEMA_INVALID' if type(value) is not int or value<0 else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('phase',[0,1])
@pytest.mark.parametrize('field',['durableAt','acknowledgedAt'])
def test_round4_erase_ack_ticks_are_representable_even_when_ordered(repository,phase,field):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);delivery=data['lifecycle'][0]['deliveries'][phase]
    delivery[field]=3155378976000000000
    if field=='durableAt':delivery['acknowledgedAt']=delivery[field]+1
    native(repository,'AC-16.2-04',data);rejected(evaluate(repository,'AC-16.2-04'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario',['AC-16.1-05','AC-16.2-05'])
@pytest.mark.parametrize('field',['label','businessReference'])
def test_round4_actual_canary_scan_cannot_be_replaced_by_matching_emitted_claims(repository,scenario,field):
    data=deepcopy(OBSERVATIONS[scenario]);p=data['tenantDataExperiment'];payload=json.loads(p['conversationEvents'][0]['payload']);canary=p['canaries']['configuration']
    if field=='label':payload[field]=canary
    else:payload[field]['value']=canary
    encoded=json.dumps(payload);p['conversationEvents'][0]['payload']=p['emittedEvents'][0]=encoded
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field',['missing-id','invalid-time'])
@pytest.mark.parametrize('fault',['valid','wrong-field','missing-variant'])
def test_round4_contract_malformed_counterparts_retain_actual_selected_bad_inputs(repository,field,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=next(r for r in data['conflicts'] if r['id']==field);variant=row['malformedVariants'][0]
    if fault=='valid':variant['reused']=deepcopy(variant['original'])
    elif fault=='missing-variant':row['malformedVariants']=[]
    else:variant['reused']['sequence']+=1
    native(repository,'AC-16.2-01',data);rejected(evaluate(repository,'AC-16.2-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['runtime','route'])
def test_round4_correctly_rehashed_foreign_catalogs_still_fail_independent_controls(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-03']);value=json.loads(data['catalogBytes'])
    if fault=='runtime':value['appId']='foreign-runtime';data['pendingLocation']['appId']=value['appId'];data['runtimeId']='foreign-runtime@1.0.0'
    else:value['domains'][0]['namedProjectionTypes']=value['domains'][0]['ordinaryProjectionTypes']=['foreign-route'];data['pendingLocation']['projectionType']='foreign-route'
    routes=[dict(domain=value['domains'][0]['domain'],projectionType=p) for p in value['domains'][0]['namedProjectionTypes']]
    fingerprint=tooling.measurements.relationships.catalog_fingerprint(value['appId'],value['serviceVersion'],routes,routes)
    value['catalogFingerprint']=data['catalogFingerprint']=data['pendingLocation']['catalogFingerprint']=fingerprint
    data['catalogBytes']=json.dumps(value);data['catalogSha256']=digest(data['catalogBytes'].encode())
    native(repository,'AC-16.3-03',data);rejected(evaluate(repository,'AC-16.3-03'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('name',['next','duplicate','conflict','older-match'])
def test_round4_last_accepted_event_is_the_persisted_checkpoint(repository,name):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);p=next(r for r in data['transitions'] if r['id']==name)['proof']
    p['accepted']['sequence']=9;p['before']['lastAccepted']['sequence']=9
    if name in ('duplicate','older-match'):p['after']['lastAccepted']['sequence']=9
    native(repository,'AC-16.1-01',data);rejected(evaluate(repository,'AC-16.1-01'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('value',[-10,2**63])
def test_round4_gapped_restart_checkpoint_has_native_nonnegative_domain(repository,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-02'])
    for key in ('beforeRestart','afterRestart'):
        snap=data[key];snap['lastContiguousSequence']=value;snap['expectedSequence']=value+1;snap['recoveryThrough']=value+4
        for n,item in enumerate(snap['pending'],2):item['sequence']=value+n
    native(repository,'AC-16.1-02',data)
    rejected(evaluate(repository,'AC-16.1-02'),'MEASUREMENT_SCHEMA_INVALID' if value<0 else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('key',[' ','\t','  '])
def test_round4_actual_configuration_whitespace_keys_set_and_remove(key):
    domain=tooling.measurements.domain;start=deepcopy(OBSERVATIONS['AC-16.1-03']['startingState'])
    rows=[]
    for number,kind,payload in [(1,'TenantConfigurationSet',dict(tenantId='tenant-a',key=key,value='')),(2,'TenantConfigurationRemoved',dict(tenantId='tenant-a',key=key))]:
        raw=json.dumps(payload);rows.append(dict(sequence=number,messageId='key-'+str(number),eventType=kind,persistedPayload=raw,payloadSha256=digest(raw.encode())))
    after,states=domain.tenant_replay(start,rows,0,2)
    assert states[1]['configuration'][key]==''
    assert key not in after['configuration']


def test_round4_actual_catalog_serializer_and_diagnostic_source_outputs_are_consumed(repository):
    goldens=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-protocol-goldens.json').read_bytes())
    value=json.loads(goldens['catalogBytes']);assert value['appId']=='conversation' and value['serviceVersion']=='v1'
    assert value['domains'][0]['fullReplayProjectionTypes']==['conversation-read-model']
    for scenario in ('AC-16.3-03','AC-16.3-04'):
        assert OBSERVATIONS[scenario]['catalogBytes']==goldens['catalogBytes']
        assert evaluate(repository,scenario)['result']=='PASS'
    for scenario in ('AC-16.1-05','AC-16.2-05'):
        assert [json.loads(v) for v in OBSERVATIONS[scenario]['tenantDataExperiment']['emittedLogs']]==goldens['emittedLogExperiment']['emitted']
    sources=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-domain-sources.json').read_bytes())['sources']
    assert any('ServiceVersion { get; set; } = "v1"' in row['source'] for row in sources)


@pytest.mark.parametrize('field',['fullReplayProjectionTypes','mappedOnlyStreamedProjectionTypes','incrementalReplayProjectionTypes','sharedEpochProjectionTypes','singleEventProjectionTypes'])
@pytest.mark.parametrize('fault',['missing','foreign','duplicate'])
def test_round4_actual_catalog_capability_subsets_are_complete_and_valid(repository,field,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-03']);value=json.loads(data['catalogBytes']);entry=value['domains'][0]
    if fault=='missing':entry.pop(field)
    elif fault=='foreign':entry[field]=['foreign-route']
    else:entry[field]=['conversation-read-model']*2
    data['catalogBytes']=json.dumps(value);data['catalogSha256']=digest(data['catalogBytes'].encode())
    native(repository,'AC-16.3-03',data);rejected(evaluate(repository,'AC-16.3-03'),'MEASUREMENT_INVALID')


def test_round4_catalog_identity_must_match_bound_actual_module_enrollment(repository):
    path='module/hexalith.module-manifest.v2.json';value=json.loads((repository/path).read_bytes());value['modules'][0]['identity']['servers'][0]['appId']='foreign'
    dump(repository,path,value);bind(repository);git(repository,'add','.');git(repository,'commit','-qm','test: alter disposable enrollment identity')
    native(repository,'AC-16.3-03');rejected(evaluate(repository,'AC-16.3-03'),'CONTRACT_DRIFT')


@pytest.mark.parametrize('field',['fullReplayProjectionTypes','sharedEpochProjectionTypes'])
def test_round4_catalog_capabilities_match_the_actual_controlled_handler(repository,field):
    data=deepcopy(OBSERVATIONS['AC-16.3-03']);value=json.loads(data['catalogBytes'])
    value['domains'][0][field]=[] if field=='fullReplayProjectionTypes' else ['conversation-read-model']
    data['catalogBytes']=json.dumps(value);data['catalogSha256']=digest(data['catalogBytes'].encode())
    native(repository,'AC-16.3-03',data);rejected(evaluate(repository,'AC-16.3-03'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('value',['2026-01-02T03:04:15+01:00','2026-01-02T03:04:15-01:00','9999-01-02T03:04:15Z'])
def test_round4_audit_governance_timestamp_matches_actual_constructor(value):
    domain=tooling.measurements.domain
    envelope=deepcopy(OBSERVATIONS['AC-16.2-05']['redactionProof']['redactionEvent'])
    payload=json.loads(envelope['payload']);payload['auditEvidence']['capturedAt']=value;envelope['payload']=json.dumps(payload)
    with pytest.raises(ValueError,match='actual governance audit timestamp'):
        domain.conversation_envelope(envelope)


@pytest.mark.parametrize('fault', ['foreign-key','record-tenant','record-resource','record-generation','record-position','dispatch','missing-capture','extra-capture','missing-delete','extra-delete','source-tenant','source-resource','source-digest'])
def test_round5_erase_targets_bind_captured_stored_identity_and_source(repository, fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][0];capture=row['capturedObjects'][0]
    stored=json.loads(capture['storedMetadataBytes'])
    if fault=='foreign-key':
        key=tooling.measurements.relationships.conversation_key('foreign',capture['sourceRecords'][0]['conversation'])
        row['deletionTargets'][0]['key']=row['deletedTargets'][0]['key']=capture['key']=key
    elif fault=='record-tenant':
        for part in ('summary','detail'):stored[part]['tenantId']='tenant:foreign'
    elif fault=='record-resource':
        for part in ('summary','detail'):stored[part]['conversationId']='conv:foreign'
    elif fault=='record-generation':stored['lifecycleGeneration']+=1
    elif fault=='record-position':
        for part in ('summary','detail'):stored[part]['freshness']['lastAppliedEventPosition']+=1
    elif fault=='dispatch':stored['dispatchId']='foreign-dispatch'
    elif fault=='missing-capture':row['capturedObjects']=[]
    elif fault=='extra-capture':row['capturedObjects'].append(deepcopy(capture))
    elif fault=='missing-delete':row['deletedTargets']=[]
    elif fault=='extra-delete':row['deletedTargets'].append(deepcopy(row['deletedTargets'][0]))
    elif fault in ('source-tenant','source-resource'):
        field='tenant' if fault=='source-tenant' else 'conversation'
        capture['sourceRecords'][0][field]='foreign'
    else:capture['storedMetadataSha256']='0'*64
    capture['storedMetadataBytes']=json.dumps(stored)
    if fault!='source-digest':capture['storedMetadataSha256']=digest(capture['storedMetadataBytes'].encode())
    native(repository,'AC-16.2-04',data)
    result=evaluate(repository,'AC-16.2-04')
    assert result['result']=='FAIL' and result['blockers'][0] in ('MEASUREMENT_INVALID','MEASUREMENT_SCHEMA_INVALID'),result


def test_round5_lifecycle_generation_is_distinct_from_native_projection_position(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][0]
    stored=json.loads(row['capturedObjects'][0]['storedMetadataBytes'])
    assert stored['lifecycleGeneration']==row['beforeGeneration']!=stored['summary']['freshness']['lastAppliedEventPosition']
    native(repository,'AC-16.2-04',data);assert evaluate(repository,'AC-16.2-04')['result']=='PASS'


def test_round5_rebuild_cannot_drop_creation_prefix_without_checkpoint_snapshot(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);stream=data['lifecycle'][1]['streams'][0]
    stream['start']=1;stream['applied']=stream['applied'][1:]
    native(repository,'AC-16.2-04',data);rejected(evaluate(repository,'AC-16.2-04'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['malformed','wrong-shape','wrong-type','wrong-tenant','wrong-snapshot-tenant','digest','after-drift'])
def test_round5_pending_restart_binds_actual_tenant_event_bytes(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-02'])
    for phase in ('beforeRestart','afterRestart'):
        row=data[phase]['pending'][0]
        if fault=='malformed':row['persistedPayload']='not JSON'
        elif fault=='wrong-shape':row['persistedPayload']='{"enabled":false}'
        elif fault=='wrong-type':row['eventType']='TenantEnabled'
        elif fault=='wrong-tenant':row['persistedPayload']=row['persistedPayload'].replace('tenant-a','foreign')
        elif fault=='wrong-snapshot-tenant':data[phase]['tenant']='foreign'
        elif fault=='after-drift' and phase=='afterRestart':row['persistedPayload']=row['persistedPayload'].replace('03:04:05','03:04:06')
        row['payloadSha256']='0'*64 if fault=='digest' else digest(row['persistedPayload'].encode())
    native(repository,'AC-16.1-02',data);rejected(evaluate(repository,'AC-16.1-02'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('field',['lease','version','expiresAt','now'])
@pytest.mark.parametrize('value',[-1,0,2**80,True,1.0])
def test_round5_raw_lease_version_and_utc_fields_have_native_domains(repository,field,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);fault=next(r for r in data['faults'] if r['id']=='lease-expiry')
    for raw_field in ('beforeBytes','injectedBytes','afterBytes'):
        raw=json.loads(fault[raw_field])
        if field in raw['state']:raw['state'][field]=value
        fault[raw_field]=json.dumps(raw)
    for phase in ('before','after'):fault[phase+'Sha256']=digest(fault[phase+'Bytes'].encode())
    native(repository,'AC-16.1-04',data);rejected(evaluate(repository,'AC-16.1-04'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('description',[None,'Retained description'])
def test_round5_recovered_tenant_description_preserves_native_nullable_domain(repository,description):
    data=deepcopy(OBSERVATIONS['AC-16.1-03']);domain=tooling.measurements.domain
    for events in [data['authoritativeReplay'],*[row['authoritativeReplay'] for row in data['reverseSaves']]]:
        for row in events:
            if row['eventType']=='TenantUpdated':
                payload=json.loads(row['persistedPayload']);payload['description']=description
                row['persistedPayload']=json.dumps(payload);row['payloadSha256']=digest(row['persistedPayload'].encode())
    by={r['sequence']:r for r in data['authoritativeReplay']}
    for merge in data['merges']:merge['pending']=[deepcopy(by[row['sequence']]) for row in merge['pending']]
    data['recoveredState'],_=domain.tenant_replay(data['startingState'],data['authoritativeReplay'],data['checkpoint'],data['recovery']['horizon'])
    native(repository,'AC-16.1-03',data);result=evaluate(repository,'AC-16.1-03');assert result['result']=='PASS',result


def test_round5_redaction_reused_identity_fails_even_with_refreshed_expected_state(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-05']);p=data['redactionProof'];event=p['redactionEvent'];payload=json.loads(event['payload'])
    old=payload['metadata']['eventId'];reused=p['persistedBefore']['processedEventIds'][0]
    payload['metadata']['eventId']=reused;payload['metadata']['deduplicationKey']=payload['metadata']['deduplicationKey'].replace(old,reused)
    event['payload']=json.dumps(payload);p['sourceAfter'][-1]=deepcopy(event)
    for name in ('persistedAfter','queryAfter'):
        ids=p[name]['state']['processedEventIds'];ids.remove(old);ids.append(reused);ids.sort()
    native(repository,'AC-16.2-05',data);rejected(evaluate(repository,'AC-16.2-05'),'MEASUREMENT_INVALID')


def test_round5_genuine_async_void_fact_is_rejected_without_execution(tmp_path):
    import epic16_native
    code='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_1EvidenceTests { [Xunit.Fact] public async void Scenario01() { await System.Threading.Tasks.Task.Yield(); throw new System.Exception("Never execute evidence"); } }'
    binary=compile_evidence(tmp_path/'async-void','a'*40,code)
    with pytest.raises(ValueError):epic16_native.inspect_assembly(binary,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')


@pytest.mark.parametrize('fault',['Error','Failed','Aborted','Timeout','NotExecuted','missing-outcome','cleanup-error'])
def test_round5_native_run_level_failure_cannot_hide_behind_passed_test(repository,fault):
    path=native(repository,'AC-16.1-01');tree=ET.fromstring(path.read_bytes());summary=tree.find('ResultSummary')
    if fault=='cleanup-error':ET.SubElement(ET.SubElement(summary,'Output'),'ErrorInfo')
    else:
        info=ET.SubElement(ET.SubElement(summary,'RunInfos'),'RunInfo')
        if fault!='missing-outcome':info.set('outcome',fault)
    path.write_bytes(ET.tostring(tree));rejected(evaluate(repository),'NATIVE_RUN_FAILED')


def test_round5_bound_inventory_and_native_outputs_survive_autocrlf_roundtrip(repository,tmp_path):
    paths=json.loads((repository/tooling.BINDING).read_text())['sources']
    extra={}
    for story in tooling.measurements.STORIES:
        contract=json.loads((repository/tooling.contract_path(story)).read_text())
        source=contract['nativeProtocol']['runtimeTestSource']
        extra[source]=b'// Bound native source uses CRLF.\r\nnamespace Fixture;\r\n'
        for n in range(1,6):
            scenario=f'AC-{story}-0{n}'
            extra[tooling.native_path('runtime',scenario)]=('<?xml version="1.0" encoding="utf-16"?>\r\n<TestRun />\r\n'.encode('utf-16') if n%2 else b'<TestRun>\r\n</TestRun>\r\n')
            extra[tooling.receipt_path('runtime',scenario)]=b'{"raw":"retained"}\r\n'
        for target in tooling.final_paths('runtime',story):extra[target]=b'fixed publication\r\n'
    extra['artifacts/native/evidence.dll']=b'MZ\x00\r\n\xff\x00'
    for path,raw in extra.items():
        target=repository/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
    expected={row['path']:(repository/row['path']).read_bytes() for row in paths};expected.update(extra)
    attrs=git(repository,'check-attr','text','eol','--',*expected)
    lines=attrs.splitlines();policies={path:dict(line.rsplit(': ',2)[1:] for line in lines if line.startswith(path+': ')) for path in expected}
    assert all(p['text']=='unset' or p['text']=='set' and p['eol']=='lf' for p in policies.values()),policies
    git(repository,'-c','core.autocrlf=true','add','-f','--',*extra)
    git(repository,'commit','-qm','test: retain disposable native checkout bytes')
    checkout=tmp_path/'autocrlf'
    subprocess.run(['git','clone','--shared','-q','-c','core.autocrlf=true',str(repository),str(checkout)],check=True,capture_output=True)
    assert all((checkout/path).read_bytes()==raw for path,raw in expected.items())
    git(checkout,'add','--renormalize','--',*expected)
    assert not git(checkout,'diff','--cached','--name-only')


def test_round5_party_identity_matches_genuine_native_wire_deserialization_goldens():
    import epic16_native
    goldens=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-protocol-goldens.json').read_bytes())
    assert len(goldens['partyIds'])>=7
    for row in goldens['partyIds']:
        assert epic16_native.normalize_party_id(row['input'])==row['constructed']==row['deserialized']
        assert tooling.measurements.domain.identifier(json.loads(row['inputJson']),'party')==row['deserialized']
        assert json.loads(row['serialized'])=='party:'+row['deserialized']
    # U+001C is Python whitespace but is not .NET whitespace. U+0130 also has
    # different Python lower() behavior. These goldens prohibit approximations.
    assert any(row['input'].strip().lower()!=row['constructed'] for row in goldens['partyIds'])


@pytest.mark.parametrize('party_field',['participantPartyId','authorPartyId','actorPartyId'])
def test_round5_materialized_party_ids_normalize_without_rewriting_raw_payload_hash(repository,party_field):
    data=deepcopy(OBSERVATIONS['AC-16.2-02']);events=data['authoritativeEvents'];domain=tooling.measurements.domain
    event=next(e for e in events if party_field=='actorPartyId' or party_field in json.loads(e['payload']))
    payload=json.loads(event['payload']);container=payload['metadata'] if party_field=='actorPartyId' else payload
    value=container[party_field];container[party_field]='party:  '+value.removeprefix('party:').upper()+'  '
    raw=json.dumps(payload);event['payload']=raw
    binding,_,parsed=domain.conversation_envelope(event)
    assert binding['payloadSha256']==digest(raw.encode()) and event['payload']==raw
    normalized=parsed['metadata'][party_field] if party_field=='actorPartyId' else parsed[party_field]
    assert normalized==(value if party_field=='actorPartyId' else value.removeprefix('party:'))
    refresh_replay(data);native(repository,'AC-16.2-02',data);assert evaluate(repository,'AC-16.2-02')['result']=='PASS'


def test_round5_participant_duplicate_after_native_normalization_is_rejected():
    domain=tooling.measurements.domain;events=deepcopy(OBSERVATIONS['AC-16.2-02']['authoritativeEvents'][:2]);event=deepcopy(events[-1]);event['sequence']=3
    payload=json.loads(event['payload']);payload['participantPartyId']='party:  PARTICIPANT-A  ';payload['metadata']['eventId']='new-identity';payload['metadata']['deduplicationKey']='tenant:tenant-a|conv:conversation-a|new-identity|1';event['payload']=json.dumps(payload);events.append(event)
    with pytest.raises(ValueError,match='duplicate entity'):domain.conversation_replay(events)


def test_round5_redaction_attribution_uses_normalized_actor_party_id(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-05']);proof=data['redactionProof'];event=proof['redactionEvent'];payload=json.loads(event['payload'])
    payload['metadata']['actorPartyId']='party:  ACTOR-A  ';event['payload']=json.dumps(payload);proof['sourceAfter'][-1]=deepcopy(event)
    # Persisted expected attribution stays native-normalized, while raw source bytes change.
    native(repository,'AC-16.2-05',data);result=evaluate(repository,'AC-16.2-05');assert result['result']=='PASS',result


def test_round5_opaque_nonparty_identifiers_keep_original_whitespace_and_case():
    for prefix in ('tenant','conv','message','file'):
        assert tooling.measurements.domain.identifier(prefix+':  MiXeD  ',prefix)=='  MiXeD  '


def test_round5_runtime_insertion_caller_requires_atomic_native_receipt_and_pair_publication(qualified_runtime,tmp_path,monkeypatch,capsys):
    """Only this disposable fixture injects entry and public-baseline contract trust.

    The real generator dispatcher and all runtime/source/native/publication checks
    execute. No production pins/trust entries are modified or runtime records made.
    """
    repo=qualified_runtime;story='16.2';contract_path=tooling.contract_path(story)
    contract=json.loads((repo/contract_path).read_bytes())
    # This test has a tiny genuine source inventory, so its explicit test-only
    # contract pins that pre-existing disposable baseline instead of real history.
    contract['nativeProtocol']['publicSourceBaseline']=git(repo,'rev-parse','HEAD')
    dump(repo,contract_path,contract)
    monkeypatch.setattr(tooling,'CONTRACT_PINS',dict(tooling.CONTRACT_PINS,**{contract_path:digest((repo/contract_path).read_bytes())}))
    binding=json.loads((repo/tooling.BINDING).read_bytes())
    for row in binding['sources']:
        if row['path']==contract_path:row['sha256']=tooling.CONTRACT_PINS[contract_path]
    dump(repo,tooling.BINDING,binding)
    source='tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_2EvidenceTests.cs'
    code='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_2EvidenceTests { '+''.join('[Xunit.Fact] public void Scenario0'+str(n)+'() { throw new System.Exception("Never execute evidence"); }' for n in range(1,6))+' }'
    (repo/source).write_bytes(code.replace('\n','\r\n').encode())
    spec=tooling.spec_path('runtime',story);(repo/spec).write_bytes((repo/'fixture-spec.md').read_bytes())
    git(repo,'add','.');git(repo,'commit','-qm','test: bind disposable runtime insertion source')
    candidate=git(repo,'rev-parse','HEAD');binary=compile_evidence(tmp_path/'insertion-native',candidate,code)
    retained_binary(repo).unlink()
    (repo/'artifacts/native/evidence.dll').write_bytes(binary)
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}';path=native(repo,scenario);tree=ET.fromstring(path.read_bytes())
        output=tree.find('Results/UnitTestResult/Output/StdOut');payload=json.loads(output.text)
        payload['sources']=[dict(path=source,sha256=digest((repo/source).read_bytes()))];output.text=json.dumps(payload)
        tree.find('TestDefinitions/UnitTest/TestMethod').set('codeBase','artifacts/native/evidence.dll')
        target=repo/tooling.native_path('runtime',scenario);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(ET.tostring(tree))
        retain_capture(repo, scenario, binary)
        args=['--repository',str(repo),'--contract',contract_path,'--scenario',scenario,'--output',tooling.receipt_path('runtime',scenario)]
        assert tooling.main(args,story=story)==0,json.loads(capsys.readouterr().out)
        capsys.readouterr()
    paths=tooling.final_paths('runtime',story)
    args=['--repository',str(repo),'--contract',contract_path,'--output-json',paths[0],'--output-markdown',paths[1]]
    assert records.main(args)==0,json.loads(capsys.readouterr().out)
    result=json.loads(capsys.readouterr().out);assert result['runtimeQualified'] is True
    markdown=(repo/paths[1]).read_bytes()
    (repo/spec).write_bytes((repo/spec).read_bytes()+b'\n<!-- STORY-FINAL-RECORD:BEGIN -->\n'+markdown+b'<!-- STORY-FINAL-RECORD:END -->\n')
    verify=['--repository',str(repo),'--contract',contract_path,'--verify-inserted-record',spec]
    assert records.main(verify)==2
    rejected(json.loads(capsys.readouterr().out),'PUBLICATION_MISSING','BLOCKED')
    publication=[*paths,*[fn('runtime',f'AC-{story}-0{n}') for fn in (tooling.native_path,tooling.receipt_path) for n in range(1,6)]]
    publication += [row['path'] for row in result['retainedNativeArtifacts']]
    git(repo,'add','-f','--',*publication);git(repo,'commit','-qm','test: atomically publish disposable runtime evidence')
    assert records.main(verify)==0,json.loads(capsys.readouterr().out)
    accepted=json.loads(capsys.readouterr().out);assert accepted['result']=='PASS' and accepted['candidate']['commit']==candidate
    assert set(git(repo,'diff-tree','--no-commit-id','--name-only','-r','HEAD').splitlines())==set(publication)
    assert tooling.TRUSTED_ENTRIES.keys()=={'fixture-entry.json'}


def test_round5_native_informational_warning_does_not_claim_run_failure(repository):
    path=native(repository,'AC-16.1-01');tree=ET.fromstring(path.read_bytes())
    ET.SubElement(ET.SubElement(tree.find('ResultSummary'),'RunInfos'),'RunInfo',outcome='Warning').text='Informational native warning'
    path.write_bytes(ET.tostring(tree));assert evaluate(repository)['result']=='PASS'


@pytest.mark.parametrize('name', ['duplicate','conflict'])
@pytest.mark.parametrize('fault', ['missing','tenant','sequence','native-payload','fingerprint','removed-pending','replaced-pending','checkpoint','horizon','projection','allowed','expected-version','expected-etag','split-commit','ignored-health'])
def test_round6_future_repeat_and_conflict_require_bound_composite_schedule(repository,name,fault):
    scenario='AC-16.1-03';data=deepcopy(OBSERVATIONS[scenario]);row=next(x for x in data['futureSchedules'] if x['id']==name)
    if fault=='missing':data['futureSchedules'].remove(row)
    elif fault=='tenant':row['before']['tenant']='other-tenant'
    elif fault=='sequence':row['delivered']['sequence']=data['checkpoint']
    elif fault=='native-payload':row['delivered']['persistedPayload']='not native JSON';row['delivered']['payloadSha256']=digest(b'not native JSON')
    elif fault=='fingerprint':
        row['delivered']=deepcopy(row['before']['pending'][0])
        if name=='duplicate':row['delivered']['messageId']='different'
    elif fault=='removed-pending':row['after']['pending'].pop()
    elif fault=='replaced-pending':row['after']['pending'][0]=deepcopy(row['delivered']);row['after']['pending'][0]['messageId']='replacement'
    elif fault=='checkpoint':row['after']['checkpoint']+=1
    elif fault=='horizon':row['after']['horizon']-=1
    elif fault=='projection':row['after']['projection']['name']='wrong value'
    elif fault=='allowed':row['after']['allowed']=True
    elif fault=='expected-version':row['cas']['expectedVersion']+=1
    elif fault=='expected-etag':row['cas']['expectedEtag']='foreign-etag'
    elif fault=='split-commit':row['cas']['pendingCommitId']='other-commit'
    elif name=='conflict':row['after']['health']='Gapped'
    else:row['cas'].update(succeeded=True,commitId='unnecessary-write')
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


def refresh_healthy_capture(row):
    for phase in ('Before','After'):
        raw=json.dumps(row['beforeRestart' if phase=='Before' else 'afterRestart'],separators=(',',':'))
        row['snapshot'+phase+'Bytes']=raw;row['snapshot'+phase+'Sha256']=digest(raw.encode())
        row['lookup'+phase]['snapshotSha256']=digest(raw.encode())


@pytest.mark.parametrize('case',[0,1])
@pytest.mark.parametrize('fault',['snapshot-tenant','snapshot-key','domain-tenant','source-tenant','source-event','lookup-tenant','lookup-key','lookup-process','consumer-tenant','consumer-user','consumer-request','allowed-from-role','raw-bytes'])
def test_round6_healthy_restart_binds_store_snapshot_source_and_consumer(repository,case,fault):
    scenario='AC-16.1-02';data=deepcopy(OBSERVATIONS[scenario]);row=data['healthyRestarts'][case]
    if fault.startswith('snapshot-'):
        key='tenant' if fault=='snapshot-tenant' else 'logicalStoreKey'
        for phase in ('beforeRestart','afterRestart'):row[phase][key]='foreign-tenant'
    elif fault=='domain-tenant':
        for phase in ('beforeRestart','afterRestart'):row[phase]['domain']['tenantId']='foreign-tenant'
    elif fault=='source-tenant':row['startingState']['tenantId']='foreign-tenant'
    elif fault=='source-event':
        record=row['sourceEvents'][0];payload=json.loads(record['persistedPayload']);payload['tenantId']='foreign-tenant';record['persistedPayload']=json.dumps(payload);record['payloadSha256']=digest(record['persistedPayload'].encode())
    elif fault.startswith('lookup-'):
        key={'lookup-tenant':'tenantId','lookup-key':'logicalKey','lookup-process':'process'}[fault];row['lookupAfter'][key]='foreign'
    elif fault.startswith('consumer-'):
        key={'consumer-tenant':'tenantId','consumer-user':'userId','consumer-request':'requestId'}[fault];row['consumerAfter'][key]='foreign'
    elif fault=='allowed-from-role':
        row['startingState']['members']['actor-a']='TenantReader'
        for phase in ('beforeRestart','afterRestart'):row[phase]['domain']['members']['actor-a']='TenantReader'
        # The denied case must demonstrate forged authorization too.
        for phase in ('beforeRestart','afterRestart','consumerBefore','consumerAfter'):row[phase]['allowed']=True
    refresh_healthy_capture(row)
    if fault=='raw-bytes':row['snapshotAfterBytes']+=' '
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


def test_round6_relabelling_gapped_tenant_cannot_leave_healthy_restart_from_another_tenant(repository):
    scenario='AC-16.1-02';data=deepcopy(OBSERVATIONS[scenario]);data['tenant']='other-tenant'
    for phase in ('beforeRestart','afterRestart'):
        data[phase]['tenant']=data['tenant']
        for row in data[phase]['pending']:
            payload=json.loads(row['persistedPayload']);payload['tenantId']=data['tenant'];row['persistedPayload']=json.dumps(payload);row['payloadSha256']=digest(row['persistedPayload'].encode())
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['missing','not-executed','not-completed','same-operation','owned-occupied','unrelated-free','inventory','result','diagnostic','lane-started'])
def test_round6_unrelated_port_requires_separate_executed_negative_operation(repository,fault):
    scenario='AC-16.3-02';data=deepcopy(OBSERVATIONS[scenario]);row=data['negativePreflight']
    if fault=='missing':data.pop('negativePreflight')
    elif fault=='not-executed':row['executed']=False
    elif fault=='not-completed':row['completed']=False
    elif fault=='same-operation':row['operationId']=data['collisionOperationId']
    elif fault=='owned-occupied':row['occupancy'][1]['owner']='collision-fixture'
    elif fault=='unrelated-free':row['occupancy'][-1]['owner']=None
    elif fault=='inventory':row['endpointInventory'][1]['port']+=10
    elif fault=='result':row['result']['checkedEndpointIds'].pop()
    elif fault=='diagnostic':row['diagnostics'].append('DAPR_CONTROL_PLANE_PORT_COLLISION')
    else:row['liveLaneStarted']=True
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_SCHEMA_INVALID' if fault=='missing' else 'MEASUREMENT_INVALID')


@pytest.mark.parametrize('case',range(4))
@pytest.mark.parametrize('fault',['scope','expected','proposal','accepted','clear','commit','checkpoint','generation','watermark','content','lease-time'])
def test_round6_stale_worker_attempts_preserve_complete_authoritative_newer_progress(repository,case,fault):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);proof=data['concurrentRecovery'];row=proof['staleAttempts'][case]
    if fault=='scope':row['scope']['pendingId']='other-pending'
    elif fault=='expected':row['expected']['lease']+=1
    elif fault=='proposal':row['proposed']['projection']['label']='unrelated content'
    elif fault=='accepted':row['succeeded']=True
    elif fault=='clear':row['cleared']=['pending-1']
    elif fault=='commit':row['commitId']='unexpected-write'
    elif fault in ('checkpoint','generation','watermark'):row['storedAfter'][fault]-=1
    elif fault=='content':row['storedAfter']['projection']['fileReferences']=[]
    else:row['observedAtUtcTicks']=proof['newerStored']['leaseUntilUtcTicks']-1 if row['id']=='expired-lease' else proof['newerStored']['leaseUntilUtcTicks']
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['missing-attempt','missing-position','foreign-event','nonmonotonic-position','sequence-as-watermark','forged-zero-watermarks','old-expectation','current-expectation','current-expired','current-split','current-content','retry-clears','retry-writes','retry-content'])
def test_round6_current_recovery_and_duplicate_retry_bind_surviving_progress(repository,fault):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);proof=data['concurrentRecovery']
    if fault=='missing-attempt':proof['staleAttempts'].pop()
    elif fault=='missing-position':proof['authoritativePositions'].pop()
    elif fault=='foreign-event':proof['authoritativePositions'][-1]['eventSha256']='f'*64
    elif fault=='nonmonotonic-position':proof['authoritativePositions'][-1]['position']=1
    elif fault=='sequence-as-watermark':proof['newerStored']['watermark']=data['afterCheckpoint']
    elif fault=='forged-zero-watermarks':data['beforeWatermark']=data['afterWatermark']=0
    elif fault=='old-expectation':proof['oldWorker']['expected']['version']+=1
    elif fault=='current-expectation':proof['current']['expected']['version']-=1
    elif fault=='current-expired':proof['current']['observedAtUtcTicks']=proof['newerStored']['leaseUntilUtcTicks']
    elif fault=='current-split':proof['current']['pendingCommitId']='split-commit'
    elif fault=='current-content':proof['current']['after']['projection']['fileReferences']=[]
    elif fault=='retry-clears':proof['duplicateRetry']['cleared']=['pending-1']
    elif fault=='retry-writes':proof['duplicateRetry']['succeeded']=True
    else:proof['duplicateRetry']['after']['projection']['label']='stale content'
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('scenario',['AC-16.3-01','AC-16.3-05'])
def test_round6_readiness_failure_must_reach_attempt_or_timeout_control(repository,scenario):
    data=deepcopy(OBSERVATIONS[scenario])
    if scenario.endswith('01'):data.update(attempts=1,elapsedMs=1)
    else:next(x for x in data['faults'] if x['id']=='endpoint-race')['observed'].update(attempts=1,elapsedMs=1)
    data['diagnostics'][0]['attempts']=1
    native(repository,scenario,data);rejected(evaluate(repository,scenario),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('attempts,elapsed',[(1,1000),(3,1)])
@pytest.mark.parametrize('scenario',['AC-16.3-01','AC-16.3-05'])
def test_round6_readiness_can_end_on_either_pinned_terminal_condition(repository,scenario,attempts,elapsed):
    data=deepcopy(OBSERVATIONS[scenario])
    if scenario.endswith('01'):data.update(attempts=attempts,elapsedMs=elapsed)
    else:next(x for x in data['faults'] if x['id']=='endpoint-race')['observed'].update(attempts=attempts,elapsedMs=elapsed)
    data['diagnostics'][0]['attempts']=attempts;native(repository,scenario,data);result=evaluate(repository,scenario);assert result['result']=='PASS',result


@pytest.mark.parametrize('preserve',[False,True])
def test_round6_future_fault_horizon_preserves_prior_larger_horizon(repository,preserve):
    scenario='AC-16.1-04';data=deepcopy(OBSERVATIONS[scenario]);row=next(x for x in data['faults'] if x['id']=='three-future-deliveries')
    before=json.loads(row['beforeBytes']);before['state']['horizon']=999
    row['beforeBytes']=row['afterBytes']=json.dumps(before);row['beforeSha256']=row['afterSha256']=digest(row['beforeBytes'].encode())
    if preserve:
        injected=json.loads(row['injectedBytes']);injected['state']['horizon']=999;row['injectedBytes']=json.dumps(injected);row['observed']['horizon']=999
    native(repository,scenario,data);result=evaluate(repository,scenario)
    if preserve:assert result['result']=='PASS',result
    else:rejected(result,'MEASUREMENT_INVALID')


def test_round6_native_whitespace_set_matches_all_utf16_code_units():
    native=tooling.native_helper._run(dict(operation='whitespace'))
    assert native['codePoints']==sorted(ord(c) for c in tooling.measurements.domain.NATIVE_WHITESPACE)


@pytest.mark.parametrize('value',['\x1c','\x1d','\x1e','\x1f',' \x1c '])
def test_round6_opaque_id_matches_native_without_python_strip_and_preserves_identity(repository,value):
    domain=tooling.measurements.domain
    assert tooling.native_helper._run(dict(operation='opaqueId',value=value))['value']==domain.text(value)==value
    for prefix in ('tenant','conv','message','file'):assert domain.identifier(prefix+':'+value,prefix)==value
    data=deepcopy(OBSERVATIONS['AC-16.2-01']);row=data['events'][1]
    row['selectedId']=row['messageId']=row['authoritativeEnvelope']['messageId']=value
    native(repository,'AC-16.2-01',data);result=evaluate(repository,'AC-16.2-01');assert result['result']=='PASS',result
    payload=json.loads(OBSERVATIONS['AC-16.2-02']['authoritativeEvents'][-1]['payload']);payload['attributes']={value:'value'}
    envelope=deepcopy(OBSERVATIONS['AC-16.2-02']['authoritativeEvents'][-1]);envelope['payload']=json.dumps(payload)
    assert domain.conversation_envelope(envelope)[2]['attributes']=={value:'value'}


@pytest.mark.parametrize('value',['',' ','\t\r\n','\u0085','\u00a0','\u2000','\u2028','\u3000'])
def test_round6_native_true_whitespace_remains_rejected(value):
    with pytest.raises(ValueError):tooling.native_helper._run(dict(operation='opaqueId',value=value))
    with pytest.raises(ValueError):tooling.measurements.domain.text(value)


@pytest.mark.parametrize('ownership',[None,'BLOCKED','PASS'])
def test_round6_story163_entry_requires_separate_apphost_ownership(repository,monkeypatch,ownership):
    story='16.3'
    def entry(doc):
        doc['storyId']=story
        ids=json.loads((repository/tooling.contract_path(story)).read_bytes())['preservedContract']['predecessors']
        doc['predecessors']=[dict(identity=i,files=deepcopy(doc['predecessors'][0]['files'])) for i in ids]
        if ownership is not None:doc['qualifications']['appHostOwnership']=ownership
    candidate,binding=trusted_entry_fixture(repository,monkeypatch,entry)
    with tooling.ContainedReader(repository) as reader:
        if ownership=='PASS':assert tooling.entry_evidence(reader,binding,story,candidate,'runtime')['runtimeQualified'] is True
        else:
            with pytest.raises(tooling.Stop) as error:tooling.entry_evidence(reader,binding,story,candidate,'runtime')
            assert error.value.code==('ENTRY_INVENTORY_DRIFT' if ownership is None else 'ENTRY_QUALIFICATION_PENDING')
            assert error.value.blocked is (ownership=='BLOCKED')


@pytest.mark.parametrize('fault',['version','extra-field','duplicate-key','scenario','candidate','native-path','native-hash','original-codebase','relative-mapping','traversal-root','traversal-codebase','foreign-original','retained-escape','retained-source','retained-outside','binary-hash','binary-byte','manifest-alias','binary-alias','binary-symlink','missing-manifest'])
def test_round6_capture_rejects_changed_ambiguous_or_aliased_native_bindings(qualified_runtime,fault):
    repo=qualified_runtime;scenario='AC-16.2-02';path=tooling.capture.capture_path('16.2',scenario);doc=json.loads((repo/path).read_bytes())
    if fault=='version':doc['captureVersion']=True
    elif fault=='extra-field':doc['command']='arbitrary'
    elif fault=='scenario':doc['scenarioId']='AC-16.2-01'
    elif fault=='candidate':doc['candidate']='f'*40
    elif fault=='native-path':doc['native']['path']='unrelated.trx'
    elif fault=='native-hash':doc['native']['sha256']='f'*64
    elif fault=='original-codebase':doc['originalCodeBase']='different.dll'
    elif fault=='relative-mapping':doc['originalRelativePath']='different.dll'
    elif fault=='traversal-root':doc['originalRepositoryRoot']+='/../other'
    elif fault=='traversal-codebase':doc['originalCodeBase']='artifacts/../evidence.dll'
    elif fault=='foreign-original':
        # Keep the original native result and manifest consistent, but uncontained.
        native_path=repo/tooling.native_path('runtime',scenario);tree=ET.fromstring(native_path.read_bytes());tree.find('./{*}TestDefinitions/{*}UnitTest/{*}TestMethod').set('codeBase','/outside/evidence.dll');native_path.write_bytes(ET.tostring(tree));doc['originalCodeBase']='/outside/evidence.dll';doc['native']['sha256']=digest(native_path.read_bytes())
    elif fault=='retained-escape':doc['binary']['path']='../evidence.dll'
    elif fault=='retained-source':doc['binary']['path']=tooling.TOOL_SOURCES[0]
    elif fault=='retained-outside':doc['binary']['path']='artifacts/native/evidence.dll'
    elif fault=='binary-hash':doc['binary']['sha256']='f'*64
    elif fault=='binary-byte':
        target=retained_binary(repo);target.write_bytes(target.read_bytes()+b'changed trailing native bytes')
    dump(repo,path,doc)
    if fault=='duplicate-key':(repo/path).write_bytes((repo/path).read_bytes().replace(b'"captureVersion": 1',b'"captureVersion": 1, "captureVersion": 1'))
    elif fault in ('manifest-alias','binary-alias'):
        target=repo/path if fault=='manifest-alias' else retained_binary(repo);os.link(target,repo/'artifacts/alias-file')
    elif fault=='binary-symlink':
        target=retained_binary(repo);raw=target.read_bytes();target.unlink();(repo/'artifacts/original.dll').write_bytes(raw);target.symlink_to(repo/'artifacts/original.dll')
    elif fault=='missing-manifest':(repo/path).unlink()
    result=runtime_evaluate(repo)
    assert result['result']==('BLOCKED' if fault=='missing-manifest' else 'FAIL'),result
    assert result['blockers'] and result['assertionLedger']


@pytest.mark.parametrize('style,root,base,relative',[
    ('posix','/measurement/source','/measurement/source/bin/Evidence.dll','bin/Evidence.dll'),
    ('posix','/measurement/source','bin/Evidence.dll','bin/Evidence.dll'),
    ('windows',r'C:\measurement\source',r'C:\measurement\source\bin\Evidence.dll','bin/Evidence.dll'),
    ('windows','D:/measurement/source','D:/measurement/source/bin/Evidence.dll','bin/Evidence.dll'),
    ('windows',r'C:\measurement\source','bin/Evidence.dll','bin/Evidence.dll'),
])
def test_round6_capture_original_platform_paths_are_lexical_metadata_only(qualified_runtime,style,root,base,relative):
    repo=qualified_runtime;scenario='AC-16.2-02';binary=retained_binary(repo).read_bytes();target=repo/tooling.native_path('runtime',scenario);tree=ET.fromstring(target.read_bytes());tree.find('./{*}TestDefinitions/{*}UnitTest/{*}TestMethod').set('codeBase',base);target.write_bytes(ET.tostring(tree))
    doc=retain_capture(repo,scenario,binary,root=root,style=style)
    assert doc['originalRelativePath']==relative
    result=runtime_evaluate(repo);assert result['result']=='PASS',result
    assert result['bindings']['testBinary']['path']==doc['binary']['path']


@pytest.mark.parametrize('root,base,style',[
    ('/repo','/repository/a.dll','posix'),('/repo','/repo/../a.dll','posix'),('/repo/.','/repo/./a.dll','posix'),
    ('/repo','/repo//a.dll','posix'),('/repo','//repo/a.dll','posix'),('/repo','a/./b.dll','posix'),
    (r'C:\repo',r'C:\repository\a.dll','windows'),(r'C:\repo',r'C:\repo\..\a.dll','windows'),
    (r'C:\repo',r'C:repo\a.dll','windows'),(r'C:\repo',r'C:/repo/a.dll','windows'),
    (r'\\server\repo',r'\\server\repo\a.dll','windows'),('C:/repo','D:/repo/a.dll','windows'),
])
def test_round6_capture_rejects_dot_traversal_ambiguous_and_foreign_original_paths(root,base,style):
    with pytest.raises((ValueError,tooling.ReadError)):tooling.capture.original_relative(root,base,style)


def test_round6_genuine_native_capture_publication_survives_fresh_checkout(qualified_runtime,tmp_path,monkeypatch,capsys):
    """Execute genuine fixture tests once, retain raw TRX, then lose build outputs."""
    repo=qualified_runtime;story='16.2';contract_path=tooling.contract_path(story)
    contract=json.loads((repo/contract_path).read_bytes());contract['nativeProtocol']['publicSourceBaseline']=git(repo,'rev-parse','HEAD');dump(repo,contract_path,contract)
    monkeypatch.setattr(tooling,'CONTRACT_PINS',dict(tooling.CONTRACT_PINS,**{contract_path:digest((repo/contract_path).read_bytes())}))
    binding=json.loads((repo/tooling.BINDING).read_bytes())
    for row in binding['sources']:
        if row['path']==contract_path:row['sha256']=tooling.CONTRACT_PINS[contract_path]
    dump(repo,tooling.BINDING,binding)
    source=contract['nativeProtocol']['runtimeTestSource']
    code='''namespace Hexalith.Conversations.IntegrationTests.Epic16;
public sealed class Story16_2EvidenceTests(Xunit.ITestOutputHelper output)
{
    private void Emit(string scenario) => output.WriteLine(System.IO.File.ReadAllText(System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("EPIC16_FIXTURE_PAYLOADS")!, scenario+".json")));
'''+''.join('[Xunit.Fact] public void Scenario0'+str(n)+'() => Emit("AC-16.2-0'+str(n)+'");\n' for n in range(1,6))+'}\n'
    (repo/source).write_bytes(code.replace('\n','\r\n').encode())
    spec=tooling.spec_path('runtime',story);(repo/spec).write_bytes((repo/'fixture-spec.md').read_bytes())
    git(repo,'add','.');git(repo,'commit','-qm','test: bind portable native capture fixture')
    candidate=git(repo,'rev-parse','HEAD');build=tmp_path/'portable-build';build.mkdir()
    (build/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
    (build/'Tests.cs').write_bytes((repo/source).read_bytes());(build/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+candidate+'")]')
    result=subprocess.run(['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=build,capture_output=True,timeout=120);assert result.returncode==0,result.stdout.decode()
    runner=repo/'artifacts/measurement-only-runner';shutil.copytree(build/'bin/Debug/net10.0',runner)
    retained_binary(repo).unlink()
    payloads=tmp_path/'payloads';payloads.mkdir()
    captured={}
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}';payload=dict(scenarioId=scenario,candidate=candidate,sources=[dict(path=source,sha256=digest((repo/source).read_bytes()))],contractSha256=tooling.CONTRACT_PINS[contract_path],observations=deepcopy(OBSERVATIONS[scenario]));dump(payloads,scenario+'.json',payload)
        target=repo/tooling.native_path('runtime',scenario);target.parent.mkdir(parents=True,exist_ok=True)
        command=['dotnet',str(runner/'Evidence.dll'),'-automated','sync','-failSkips','-method','Hexalith.Conversations.IntegrationTests.Epic16.Story16_2EvidenceTests.Scenario0'+str(n),'-trx',str(target)]
        result=subprocess.run(command,env=dict(os.environ,EPIC16_FIXTURE_PAYLOADS=str(payloads)),capture_output=True,timeout=120);assert result.returncode==0,(result.stdout,result.stderr)
        raw=target.read_bytes();assert records.parse_trx(raw)['code_bases']==[str(runner/'Evidence.dll')]
        retain_capture(repo,scenario,(runner/'Evidence.dll').read_bytes());assert target.read_bytes()==raw;captured[str(target.relative_to(repo))]=raw
        args=['--repository',str(repo),'--contract',contract_path,'--scenario',scenario,'--output',tooling.receipt_path('runtime',scenario)]
        assert tooling.main(args,story=story)==0,capsys.readouterr().out;capsys.readouterr()
    paths=tooling.final_paths('runtime',story);args=['--repository',str(repo),'--contract',contract_path,'--output-json',paths[0],'--output-markdown',paths[1]]
    assert records.main(args)==0,capsys.readouterr().out
    record=json.loads(capsys.readouterr().out);assert len(record['retainedNativeArtifacts'])==6
    original_pair=tuple((repo/path).read_bytes() for path in paths)
    conflicting=deepcopy(record['scenarios'][:5]);conflicting[-1]['bindings']['testBinary']['sha256']='f'*64
    with tooling.ContainedReader(repo) as reader,pytest.raises(tooling.Stop) as conflict:
        tooling.capture.inventory(reader,story,conflicting,tooling.need)
    assert conflict.value.code=='NATIVE_CAPTURE_CONFLICT'
    # Exercise real generation failure for an unconsumed retained binary.
    extra=repo/(tooling.root_path('runtime',story)+'/binaries/'+'f'*64+'.dll');extra.write_bytes((runner/'Evidence.dll').read_bytes())
    rejected(tooling.generate(repo,story)[0],'NATIVE_CAPTURE_INVENTORY');extra.unlink()
    # Missing or extra manifests likewise cannot be published by the generator.
    capture_name=tooling.capture.capture_path(story,'AC-16.2-01');saved=(repo/capture_name).read_bytes();(repo/capture_name).unlink()
    rejected(tooling.generate(repo,story)[0],'INPUT_UNAVAILABLE','BLOCKED');(repo/capture_name).write_bytes(saved)
    unknown=repo/tooling.root_path('runtime',story)/'extra.capture.json';unknown.write_bytes(saved)
    rejected(tooling.generate(repo,story)[0],'NATIVE_CAPTURE_INVENTORY');unknown.unlink()
    markdown=original_pair[1];(repo/spec).write_bytes((repo/spec).read_bytes()+b'\n<!-- STORY-FINAL-RECORD:BEGIN -->\n'+markdown+b'<!-- STORY-FINAL-RECORD:END -->\n')
    verify=['--repository',str(repo),'--contract',contract_path,'--verify-inserted-record',spec]
    assert records.main(verify)==2;rejected(json.loads(capsys.readouterr().out),'PUBLICATION_MISSING','BLOCKED')
    publication=[*paths,*[fn('runtime',f'AC-{story}-0{n}') for fn in (tooling.native_path,tooling.receipt_path) for n in range(1,6)],*[row['path'] for row in record['retainedNativeArtifacts']]]
    git(repo,'add','-f','--',*publication);git(repo,'commit','-qm','test: publish portable native capture fixture')
    assert set(git(repo,'diff-tree','--no-commit-id','--name-only','-r','HEAD').splitlines())==set(publication)
    git(repo,'add',spec);git(repo,'commit','-qm','test: retain portable native fixture insertion')
    relocated=tmp_path/'fresh-checkout'
    subprocess.run(['git','clone','--no-hardlinks','-q',str(repo),str(relocated)],capture_output=True,check=True)
    dependency='references/Hexalith.EventStore'
    subprocess.run(['git','clone','--no-hardlinks','-q',str(repo/dependency),str(relocated/dependency)],capture_output=True,check=True)
    # A fresh clone has only committed capture binaries, with no measurement runner.
    assert not (relocated/'artifacts/measurement-only-runner').exists()
    assert not (relocated/'artifacts/native/evidence.dll').exists()
    hidden=tmp_path/'original-unavailable';repo.rename(hidden);shutil.rmtree(build);shutil.rmtree(payloads)
    assert not repo.exists()
    assert all((relocated/path).read_bytes()==raw for path,raw in captured.items())
    assert tooling.generate(relocated,story,candidate,publication=True)[1:]==original_pair
    verify[1]=str(relocated);assert records.main(verify)==0,capsys.readouterr().out
    final=json.loads(capsys.readouterr().out);assert final['result']=='PASS' and final['candidate']['commit']==candidate
    assert final['retainedNativeArtifacts']==record['retainedNativeArtifacts']
    # Committed captured bytes cannot be repaired by mutable source/build output.
    artifact=relocated/final['retainedNativeArtifacts'][-1]['path'];saved=artifact.read_bytes();artifact.write_bytes(saved+b'changed')
    assert records.main(verify)==1;assert json.loads(capsys.readouterr().out)['result']=='FAIL'
    artifact.write_bytes(saved)
