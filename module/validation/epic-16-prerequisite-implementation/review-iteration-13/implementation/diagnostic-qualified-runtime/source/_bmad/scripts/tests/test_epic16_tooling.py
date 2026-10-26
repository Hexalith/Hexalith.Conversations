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
    if args and args[0] in ('commit','commit-tree'):
        flag=next((i for i,arg in enumerate(args) if arg in ('-m','-qm')),None)
        assert flag is not None, 'Disposable commit must have exact validated message'
        message=args[flag+1]
        if message not in _COMMIT_MESSAGES:
            result=subprocess.run([str(ROOT/'node_modules/.bin/commitlint'),'--config',str(ROOT/'commitlint.config.mjs')],input=(message+'\n').encode(),cwd=ROOT,capture_output=True)
            assert result.returncode==0,(result.stdout,result.stderr)
            _COMMIT_MESSAGES[message]=dict(exitCode=result.returncode,stdout=result.stdout.decode(),stderr=result.stderr.decode())
            if os.environ.get('EPIC16_COMMITLINT_EVIDENCE'):
                Path(os.environ['EPIC16_COMMITLINT_EVIDENCE']).write_text(json.dumps(_COMMIT_MESSAGES,indent=2)+'\n')
    return subprocess.run(['git', '-C', str(repo), *args], check=True, capture_output=True,
        env=dict(os.environ,GIT_AUTHOR_DATE='2026-10-25T00:00:00Z',GIT_COMMITTER_DATE='2026-10-25T00:00:00Z')).stdout.decode().strip()


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
    scope = 'runtime' if (repo/'fixture-entry.json').exists() else SCOPE
    if scenario=='AC-16.1-05' and not payload['observations']['upstreamSurfaces']:round9_api_observations(repo,payload['observations'],scope)
    if scenario in ('AC-16.1-05','AC-16.2-05') and payload['observations'].get('compiledContracts') is None:
        round10_compiled_contracts(repo, scenario, payload['observations'], scope)
    if (repo/'fixture-entry.json').exists():payload['loadedProducts']=round9_loaded_products(repo,candidate)
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
    round9_dependencies(repo)
    warning=repo/tooling.measurements.execution.WARNING_PATH;warning.parent.mkdir(parents=True,exist_ok=True);warning.write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
    contract_source = repo/'src/Hexalith.Conversations.Contracts/Contract.cs'; contract_source.parent.mkdir(parents=True)
    contract_source.write_text('namespace Fixture; public sealed record Contract(string Value);\n')
    git(repo, 'add', 'src', 'fixture-products', '.gitignore', '.gitmodules'); git(repo, 'commit', '-qm', 'test: initialize disposable fixture')
    warning.write_bytes(b'// Synthetic disposable candidate with qualified durable projection.\n')
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


@pytest.mark.parametrize('sequence', [0, 2, 5])
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
    products=[]
    for row in json.loads(records.parse_trx(raw)['results'][0]['output'])['loadedProducts']:
        original=tooling.capture.original_relative(root,row['codeBase'],style)
        # The fixture author copies locally built bytes; the consumer never reads original locations.
        content=(repo/'artifacts/measurement-products'/str(row['assemblyName']+'.dll')).read_bytes()
        retained_product=tooling.capture.binary_path(scenario[3:7],digest(content));(repo/retained_product).write_bytes(content)
        products.append(dict(assemblyName=row['assemblyName'],owner=row['owner'],sourceRevision=row['sourceRevision'],mvid=row['mvid'],path=retained_product,sha256=digest(content),originalCodeBase=row['codeBase'],originalRelativePath=original))
    capture = dict(captureVersion=2, products=products, scenarioId=scenario, candidate=candidate,
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
    dependency = repo/'references/Hexalith.EventStore'
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
    assert result['candidate']['gitlinks'] == [dict(path=owner,commit=git(qualified_runtime/owner,'rev-parse','HEAD')) for owner in ('references/Hexalith.EventStore','references/Hexalith.Tenants','references/Hexalith.Commons')]
    assert not (qualified_runtime/tooling.receipt_path('runtime','AC-16.2-02')).exists()


@pytest.mark.parametrize('fault', ['absent', 'malformed', 'wrong-revision'])
def test_runtime_evaluator_admits_only_bound_native_binary(qualified_runtime, managed_evidence, fault):
    path = retained_binary(qualified_runtime)
    if fault == 'absent': path.unlink()
    else:
        original_root=json.loads((qualified_runtime/tooling.capture.capture_path('16.2','AC-16.2-02')).read_bytes())['originalRepositoryRoot']
        retain_capture(qualified_runtime, 'AC-16.2-02', b'MZ invalid' if fault == 'malformed' else managed_evidence,root=original_root)
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
    output = tree.find('Results/UnitTestResult/Output/StdOut'); payload = json.loads(output.text); payload['candidate'] = candidate
    payload['loadedProducts']=round9_loaded_products(qualified_runtime,candidate)
    output.text = json.dumps(payload); path.write_bytes(ET.tostring(tree))
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
    round9_rejections(data)


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
    proof=data['recoverySchedule'];old=proof['oldWorker']['snapshot']['horizon']
    proof['staleCompletion']['proposed']['projection'],_=domain.tenant_replay(data['startingState'],data['authoritativeReplay'][:old-data['checkpoint']],data['checkpoint'],old)
    for snapshot in (proof['replay']['after'],proof['currentCompletion']['before'],proof['currentCompletion']['after']):snapshot['projection']=deepcopy(data['recoveredState'])
    round12_refresh_reverse_states(data)
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
    round9_clear_capture(repo)
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
    """Each storedBefore is an independent reset/fork, not a cross-case timeline."""
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
    data['diagnostics'][0]['attempts']=attempts;data['diagnostics'][0]['retryAdvice']=tooling.measurements.faults.retry_advice(attempts,elapsed);native(repository,scenario,data);result=evaluate(repository,scenario);assert result['result']=='PASS',result


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
    if fault=='duplicate-key':(repo/path).write_bytes((repo/path).read_bytes().replace(b'"captureVersion": 2',b'"captureVersion": 2, "captureVersion": 2'))
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
    tree=ET.fromstring(target.read_bytes());output=tree.find('./{*}Results/{*}UnitTestResult/{*}Output/{*}StdOut');payload=json.loads(output.text)
    separator='\\' if style=='windows' and '\\' in root else '/'
    for row in payload['loadedProducts']:row['codeBase']=root.rstrip(separator)+separator+'artifacts'+separator+'measurement-products'+separator+row['assemblyName']+'.dll'
    output.text=json.dumps(payload);target.write_bytes(ET.tostring(tree))
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
    private void Emit(string scenario)
    {
        var payload = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("EPIC16_FIXTURE_PAYLOADS")!, scenario+".json")))!;
        System.Reflection.Assembly[] assemblies = [typeof(Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelWriter).Assembly, typeof(Hexalith.Conversations.Contracts.Projections.ConversationDetailProjectionV1).Assembly, typeof(Hexalith.Tenants.Client.Projections.ITenantProjectionStore).Assembly, typeof(Hexalith.Tenants.Contracts.Events.TenantDisabled).Assembly, typeof(Hexalith.EventStore.Client.Projections.IReadModelStore).Assembly, typeof(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchRequest).Assembly, typeof(Hexalith.EventStore.DomainService.DomainProjectionDispatcher).Assembly, typeof(Hexalith.EventStore.Server.Projections.ProjectionDeliveryReconciler).Assembly, typeof(Hexalith.Commons.TenantAccess.TenantAccessEvaluator).Assembly];
        string[] owners = [".",".","references/Hexalith.Tenants","references/Hexalith.Tenants","references/Hexalith.EventStore","references/Hexalith.EventStore","references/Hexalith.EventStore","references/Hexalith.EventStore","references/Hexalith.Commons"];
        var loaded = new System.Text.Json.Nodes.JsonArray();
        for (int i=0;i<assemblies.Length;i++)
        {
            var assembly=assemblies[i];
            string version=System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(assembly)!.InformationalVersion;
            loaded.Add(new System.Text.Json.Nodes.JsonObject { ["assemblyName"]=assembly.GetName().Name, ["owner"]=owners[i], ["sourceRevision"]=version[(version.LastIndexOf('+')+1)..], ["mvid"]=assembly.ManifestModule.ModuleVersionId.ToString(), ["sha256"]=System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(assembly.Location))).ToLowerInvariant(), ["codeBase"]=assembly.Location });
        }
        payload["loadedProducts"]=loaded;
        output.WriteLine(payload.ToJsonString());
    }
'''+''.join('[Xunit.Fact] public void Scenario0'+str(n)+'() => Emit("AC-16.2-0'+str(n)+'");\n' for n in range(1,6))+'}\n'
    (repo/source).write_bytes(code.replace('\n','\r\n').encode())
    spec=tooling.spec_path('runtime',story);(repo/spec).write_bytes((repo/'fixture-spec.md').read_bytes())
    git(repo,'add','.');git(repo,'commit','-qm','test: bind portable native capture fixture')
    candidate=git(repo,'rev-parse','HEAD');build=tmp_path/'portable-build';build.mkdir()
    product_rows=round9_loaded_products(repo,candidate)
    references=''.join('<Reference Include="'+row['assemblyName']+'"><HintPath>'+row['codeBase']+'</HintPath></Reference>' for row in product_rows)
    (build/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" />'+references+'</ItemGroup></Project>')
    (build/'Tests.cs').write_bytes((repo/source).read_bytes());(build/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+candidate+'")]')
    result=subprocess.run(['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=build,capture_output=True,timeout=120);assert result.returncode==0,result.stdout.decode()
    runner=repo/'artifacts/measurement-only-runner';shutil.copytree(build/'bin/Debug/net10.0',runner)
    round9_clear_capture(repo)
    payloads=tmp_path/'payloads';payloads.mkdir()
    captured={}
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}';payload=dict(scenarioId=scenario,candidate=candidate,sources=[dict(path=source,sha256=digest((repo/source).read_bytes()))],contractSha256=tooling.CONTRACT_PINS[contract_path],observations=deepcopy(OBSERVATIONS[scenario]))
        if n==5: round10_compiled_contracts(repo,scenario,payload['observations'],'runtime')
        dump(payloads,scenario+'.json',payload)
        target=repo/tooling.native_path('runtime',scenario);target.parent.mkdir(parents=True,exist_ok=True)
        command=['dotnet',str(runner/'Evidence.dll'),'-automated','sync','-failSkips','-method','Hexalith.Conversations.IntegrationTests.Epic16.Story16_2EvidenceTests.Scenario0'+str(n),'-trx',str(target)]
        result=subprocess.run(command,env=dict(os.environ,EPIC16_FIXTURE_PAYLOADS=str(payloads)),capture_output=True,timeout=120);assert result.returncode==0,(result.stdout,result.stderr)
        raw=target.read_bytes();assert records.parse_trx(raw)['code_bases']==[str(runner/'Evidence.dll')]
        retain_capture(repo,scenario,(runner/'Evidence.dll').read_bytes());assert target.read_bytes()==raw;captured[str(target.relative_to(repo))]=raw
        args=['--repository',str(repo),'--contract',contract_path,'--scenario',scenario,'--output',tooling.receipt_path('runtime',scenario)]
        assert tooling.main(args,story=story)==0,capsys.readouterr().out;capsys.readouterr()
    paths=tooling.final_paths('runtime',story);args=['--repository',str(repo),'--contract',contract_path,'--output-json',paths[0],'--output-markdown',paths[1]]
    assert records.main(args)==0,capsys.readouterr().out
    record=json.loads(capsys.readouterr().out)
    expected_retained={row['path'] for scenario in record['scenarios'][:5] for row in [scenario['bindings']['testBinary'],scenario['bindings']['testBinary']['capture'],*scenario['bindings']['testBinary']['products']]}
    expected_retained.update(record['scenarios'][4]['bindings']['compiledPublicPreservation'][phase]['path'] for phase in ('baseline','candidate'))
    assert {row['path'] for row in record['retainedNativeArtifacts']}==expected_retained
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
    for dependency in ('references/Hexalith.EventStore','references/Hexalith.Tenants','references/Hexalith.Commons'):
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


# Iteration 7: all protocol fixtures below remain synthetic; runtime entry stays held.
def round7_submit(repository, scenario, data, *, passes=False, schema=False):
    native(repository, scenario, data)
    result = evaluate(repository, scenario)
    if passes:
        assert result['result'] == 'PASS', result
    else:
        rejected(result, 'MEASUREMENT_SCHEMA_INVALID' if schema else 'MEASUREMENT_INVALID')
    return result


def test_round7_native_helper_propagates_exact_repository_policy_and_reports_actual_runtime():
    helper = tooling.native_helper
    workspace, assembly = helper._helper()
    assert (Path(workspace.name)/'global.json').is_file()
    assert (Path(workspace.name)/'global.json').read_bytes() == (ROOT/'global.json').read_bytes()
    facts = helper.provenance()
    selected = subprocess.run(['dotnet','--version'],cwd=ROOT,check=True,capture_output=True).stdout.decode().strip()
    assert facts['sdk'] == selected
    assert facts['policySha256'] == digest((ROOT/'global.json').read_bytes())
    assert facts['helperSha256'] == digest(assembly.read_bytes())
    assert facts['runtime']['targetFramework'] == '.NETCoreApp,Version=v10.0'
    assert facts['runtime']['frameworkDescription'] == '.NET '+facts['runtime']['runtimeVersion']
    assert facts['runtime']['coreLibraryIdentity'].startswith('System.Private.CoreLib, Version=10.0.0.0,')
    assert facts['runtimeConfig']['runtimeOptions']['framework']['name'] == 'Microsoft.NETCore.App'
    assert 'global.json' in tooling.TOOL_SOURCES and 'global.json' in tooling._EXECUTION_BYTES


@pytest.mark.parametrize('failure',['unavailable','wrong-feature-band','older-patch'])
def test_round7_runtime_consumer_blocks_unavailable_or_mismatched_helper_sdk(qualified_runtime,monkeypatch,failure):
    helper=tooling.native_helper;helper._helper.cache_clear();helper.canonicalize.cache_clear()
    original=helper.subprocess.run
    def run(args,**kwargs):
        if args==['dotnet','--version']:
            return subprocess.CompletedProcess(args,1 if failure=='unavailable' else 0,stdout=b'10.0.500\n' if failure=='wrong-feature-band' else b'10.0.401\n',stderr=b'injected policy result')
        return original(args,**kwargs)
    monkeypatch.setattr(helper.subprocess,'run',run)
    rejected(runtime_evaluate(qualified_runtime),'NATIVE_HELPER_UNAVAILABLE','BLOCKED')


@pytest.mark.parametrize('selected',['10.0.402','10.0.403','10.0.499'])
def test_round7_latest_patch_policy_permits_only_approved_feature_band(selected):
    tooling.native_helper.validate_sdk((ROOT/'global.json').read_bytes(),selected)


@pytest.mark.parametrize('policy',[b'not json',b'{"sdk":{"version":"10.0.402","rollForward":"latestMajor"}}',b'{"sdk":{"version":10,"rollForward":"latestPatch"}}'])
def test_round7_invalid_toolchain_policy_is_invalid_input(policy):
    with pytest.raises(ValueError):tooling.native_helper.validate_sdk(policy,'10.0.402')


def test_round7_global_policy_is_an_executing_source_binding(repository):
    policy=repository/'global.json';policy.write_bytes(policy.read_bytes()+b'\n')
    bind(repository);git(repository,'add','.');git(repository,'commit','-qm','test: bind disposable fixture sources')
    native(repository,'AC-16.1-01');rejected(evaluate(repository),'EXECUTING_SOURCE_DRIFT')


@pytest.mark.parametrize('path',[
 'module/validation/epic-16-prerequisite-implementation/review-iteration-7/sample.dll',
 'module/validation/epic-16-prerequisite-implementation/review-iteration-7/sample.dll.bytes',
 'artifacts/epic16-runtime/16.2/binaries/sample.dll',
 'artifacts/epic16-tooling-fixture/16.2/binaries/sample.dll',
 'artifacts/native/sample.dll',
])
def test_round7_retained_pe_has_binary_attributes_and_safe_normal_git_diff(repository,managed_evidence,path):
    target=repository/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(managed_evidence)
    attrs=git(repository,'check-attr','text','diff','--',path)
    assert path+': text: unset' in attrs and path+': diff: unset' in attrs
    git(repository,'add','-f','--',path)
    diff=subprocess.run(['git','-C',str(repository),'diff','--cached','--',path],check=True,capture_output=True).stdout
    assert 'Binary files ' in diff.decode('utf-8')
    assert managed_evidence not in diff
    assert target.read_bytes()==managed_evidence


def round7_nullable(data):
    """Creation permits these nulls; retained snapshots must reflect them everywhere."""
    def visit(item):
        if isinstance(item,dict):
            if {'tenantId','conversationId','lifecycle','processedEventIds'} <= item.keys():
                for field in ('label','businessReference','projectId','folderId'):item[field]=None
            if item.get('eventType')=='ConversationCreated' and 'payload' in item:
                payload=json.loads(item['payload'])
                for field in ('label','businessReference','projectId','folderId'):payload[field]=None
                item['payload']=json.dumps(payload)
            for value in item.values():visit(value)
        elif isinstance(item,list):
            for value in item:visit(value)
    visit(data)
    return data


@pytest.mark.parametrize('scenario',['AC-16.2-02','AC-16.3-03','AC-16.3-04'])
def test_round7_native_nullable_creation_state_passes_schema_and_consumer(repository,scenario):
    data=round7_nullable(deepcopy(OBSERVATIONS[scenario]))
    if scenario=='AC-16.2-02':refresh_replay(data)
    elif scenario=='AC-16.3-04':
        first=round7_nullable(deepcopy(OBSERVATIONS['AC-16.3-03']));native(repository,'AC-16.3-03',first)
        # The authoritative position hashes bind the altered, valid native history.
        from evidence_boundary_current import canonical
        events=data['precedingEvents']+data['recoveryEvents']
        for pos,event in zip(data['concurrentRecovery']['authoritativePositions'],events):pos['eventSha256']=digest(canonical(event))
        # The content-safe wire digest binds the same altered complete persisted state.
        durable=next(row for row in data['wireObservation']['operations'] if row['phase']=='durable-result')
        durable['persistedAfterSha256']=digest(canonical(data['concurrentRecovery']['current']['after']))
        round11_refresh_recovery(data)
    round7_submit(repository,scenario,data,passes=True)


@pytest.mark.parametrize('field',['label','businessReference','projectId','folderId'])
@pytest.mark.parametrize('value',[1,False,[],{'malformed':'value'}])
def test_round7_nullable_snapshot_rejects_malformed_nonnull_values(repository,field,value):
    data=deepcopy(OBSERVATIONS['AC-16.3-03']);data['beforeProjection'][field]=value
    round7_submit(repository,'AC-16.3-03',data,schema=True)


def round7_terminal(kinds):
    data=deepcopy(OBSERVATIONS['AC-16.2-02'])
    rows=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-contract-wire-goldens.json').read_bytes())['additionalEvents']
    for kind in kinds:
        row=next(row for row in rows if row['eventType']==kind)
        data['authoritativeEvents'].append(dict(tenant='tenant-a',conversation='conversation-a',eventType=kind,sequence=len(data['authoritativeEvents'])+1,serializationFormat='json',messageId='terminal-'+kind,timestampUtcTicks=1,payload=row['payload']))
    refresh_replay(data)
    for row in data['rebuilds']:
        row['generatedTicks']=row['completionTicks']=row['anchorTicks']+1000
        row['lagTicks']=1000
    return data


def round7_set_canonical_lifecycle(data,state):
    for row in data['rebuilds']:
        raw=json.loads(row['canonicalJson']);raw['items'][0]['value']['lifecycle']=state
        encoded=tooling.native_helper.canonicalize(json.dumps(raw))
        row.update(canonicalJson=encoded,roundTripJson=encoded,stateHash=digest(encoded.encode()))


@pytest.mark.parametrize('kinds,expected',[
 (['ConversationClosed'],'Closed'),(['ConversationArchived'],'Archived'),
 (['ConversationArchived','ConversationClosed'],'Archived')])
@pytest.mark.parametrize('stale',[False,True])
def test_round7_terminal_native_wire_histories_are_enforced_by_replay_consumer(repository,kinds,expected,stale):
    data=round7_terminal(kinds)
    # Independent expected lifecycle, not the production oracle's returned value.
    round7_set_canonical_lifecycle(data,'Open' if stale else expected)
    round7_submit(repository,'AC-16.2-02',data,passes=not stale)


@pytest.mark.parametrize('fault',['equal-global-cutover','wrong-event-id','wrong-envelope','wrong-stream','wrong-conversation','position-hash','beyond-b','missing-prefix','missing-history','reversed-positions','duplicate-position'])
def test_round7_rebuild_boundaries_derive_from_exact_authoritative_envelopes_and_global_positions(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1];stream=row['streams'][0];events=stream['authoritativeEvents']
    if fault=='equal-global-cutover':row['C']=row['B'];row['drainedThrough']=row['C'];stream['capturedThrough']=row['C']
    elif fault=='wrong-event-id':events[1]['eventId']='other-event'
    elif fault=='wrong-envelope':events[1]['event']['messageId']='other-message'
    elif fault=='wrong-stream':events[1]['stream']='other-stream'
    elif fault=='wrong-conversation':stream['conversation']='other-conversation'
    elif fault=='position-hash':events[1]['eventSha256']='0'*64
    elif fault=='beyond-b':events[1]['position']=11
    elif fault=='missing-prefix':events.pop(0);stream['start']=1;stream['applied']=[2,3]
    elif fault=='missing-history':events.pop(1)
    elif fault=='reversed-positions':events.reverse()
    else:events[1]['position']=events[0]['position']
    round7_submit(repository,'AC-16.2-04',data)


def test_round7_rebuild_accepts_sparse_global_positions_and_equal_b_c_without_advance(repository):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1];stream=row['streams'][0]
    row['B']=row['C'];row['discoveryAt']=row['B'];stream.update(boundaryAt=row['B'],boundarySequence=3,replayedThroughB=3)
    def rebind(value):
        if isinstance(value,dict):
            if value.get('barrier') is not None:value['barrier']=row['B']
            for child in value.values():rebind(child)
        elif isinstance(value,list):
            for child in value:rebind(child)
    rebind(row['atomicity'])
    round8_requeue(row)
    assert [event['position'] for event in stream['authoritativeEvents']]==[2,10,20]
    round7_submit(repository,'AC-16.2-04',data,passes=True)


@pytest.mark.parametrize('fault',['same-location','same-position','same-message'])
def test_round7_erase_reference_forward_reverse_identities_cannot_conflict(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);a,b=data['lifecycle'][0]['deliveries'];ref=b['reference'];original=deepcopy(ref)
    if fault=='same-location':ref.update(sequence=a['reference']['sequence'],position=a['reference']['position'])
    elif fault=='same-position':ref['position']=a['reference']['position']
    else:ref['messageId']=b['deliveryId']=a['deliveryId']
    b['durableReference']=deepcopy(ref)
    for operation in b['operations'].values():
        if isinstance(operation,dict):
            operation.update(deliveryId=b['deliveryId'],recordId=b['deliveryId'])
            operation['persisted' if operation['operation']=='commit' else 'observedRecord']=deepcopy(ref)
    def refresh(value):
        if isinstance(value,dict):
            if value==original:value.clear();value.update(deepcopy(ref))
            for child in value.values():refresh(child)
        elif isinstance(value,list):
            for child in value:refresh(child)
    refresh(data)
    round7_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('kind',range(2))
@pytest.mark.parametrize('phase',['entry','work','completion'])
@pytest.mark.parametrize('fault',['missing','no-execution','wrong-fence','zero-versions','partial','mixed-transaction','plan','after-content','after-index','foreign-tenant','component-identity','stale-unexecuted','stale-succeeds','stale-writes','stale-after','stale-fence'])
def test_round7_lifecycle_cas_requires_actual_atomic_commit_and_preserved_newer_state(repository,kind,phase,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][kind];proof=row['atomicity'];stage=next(stage for stage in proof['stages'] if stage['id']==phase)
    if fault=='missing':del row['atomicity']
    elif fault=='no-execution':stage['cas']['executed']=False
    elif fault=='wrong-fence':stage['cas']['expected']['generation']+=1
    elif fault=='zero-versions':row['lifecycleCasVersion']=row['indexCasVersion']=stage['after']['index']['version']=0
    elif fault=='partial':stage['cas']['componentTransactions'].pop()
    elif fault=='mixed-transaction':stage['cas']['componentTransactions'][0]['transactionId']='other'
    elif fault=='plan':stage['plan']['componentsAfter'][0]['generation']+=1
    elif fault=='after-content':stage['after']['components'][-2]['value']=[999]
    elif fault=='after-index':stage['after']['index']['watermark']-=1
    elif fault=='foreign-tenant':stage['before']['tenant']='tenant-b'
    elif fault=='component-identity':stage['after']['components'][0]['resource']='other'
    elif fault=='stale-unexecuted':proof['staleAttempt']['executed']=False
    elif fault=='stale-succeeds':proof['staleAttempt']['succeeded']=True
    elif fault=='stale-writes':proof['staleAttempt']['writtenComponents']=['detail']
    elif fault=='stale-after':proof['staleAttempt']['storedAfter']['index']['watermark']+=1
    else:proof['staleAttempt']['expected']['version']+=1
    round7_submit(repository,'AC-16.2-04',data,schema=fault=='missing')


@pytest.mark.parametrize('kind',range(2))
@pytest.mark.parametrize('fault',['collapse','entry-skipped','completion-advances-generation','early-deletion','early-ready','dispatch-survives','quarantine-lost','mixed-generation','commit-reuse','commit-order','stale-order'])
def test_round7_lifecycle_retains_ad7_entry_work_completion_and_quarantine(repository,kind,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][kind];proof=row['atomicity'];stages=proof['stages'];completion=next(stage for stage in stages if stage['id']=='completion')
    if fault=='collapse':stages[0]['after']['index']['lifecycle']='Erased' if kind==0 else 'Ready'
    elif fault=='entry-skipped':stages.pop(0)
    elif fault=='completion-advances-generation':completion['after']['index']['generation']+=1
    elif fault=='early-deletion':stages[0]['after']['components'][0]['value']=None if kind==0 else deepcopy(stages[1]['after']['components'][0]['value'])
    elif fault=='early-ready':stages[1]['after']['index']['lifecycle']='Erased' if kind==0 else 'Ready'
    elif fault=='dispatch-survives':completion['after']['components'][1]['value']=['stale-dispatch']
    elif fault=='quarantine-lost':completion['after']['index']['quarantineReferences']=[] if kind==0 else [deepcopy(data['lifecycle'][0]['deliveries'][0]['reference'])]
    elif fault=='mixed-generation':stages[1]['after']['components'][0]['generation']-=1
    elif fault=='commit-reuse':stages[1]['cas']['transactionId']=stages[0]['cas']['transactionId']
    elif fault=='commit-order':completion['cas']['order']=stages[0]['cas']['order']
    else:proof['staleAttempt']['order']=1
    round7_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('case',['superseded-lease','stale-checkpoint'])
@pytest.mark.parametrize('fault',['valid','missing','current-fence','wrong-other-fence','expired','accepted','changed-after'])
def test_round7_recovery_independently_isolates_superseded_lease_and_stale_checkpoint(repository,case,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-04']);proof=data['concurrentRecovery'];row=next(x for x in proof['staleAttempts'] if x['id']==case)
    field='lease' if case=='superseded-lease' else 'checkpoint'
    if fault=='missing':proof['staleAttempts'].remove(row)
    elif fault=='current-fence':row['expected'][field]=proof['newerStored'][field]
    elif fault=='wrong-other-fence':row['expected']['version']-=1
    elif fault=='expired':row['observedAtUtcTicks']=proof['newerStored']['leaseUntilUtcTicks']
    elif fault=='accepted':row['succeeded']=True
    elif fault=='changed-after':row['storedAfter']['pending']=[]
    round7_submit(repository,'AC-16.3-04',data,passes=fault=='valid')


@pytest.mark.parametrize('scenario',['AC-16.2-04','AC-16.3-03'])
@pytest.mark.parametrize('fault',['equal-valid','reversed','failed','unexecuted','transaction','delivery','record','persisted','observed','time','missing'])
def test_round7_commit_ack_order_correlates_success_identity_state_and_transaction(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario])
    if scenario=='AC-16.2-04':
        row=data['lifecycle'][0]['deliveries'][0];proof=row['operations'];row['acknowledgedAt']=row['durableAt'];proof['acknowledgement']['utcTicks']=row['durableAt']
    else:
        proof=data['pendingOperations'];data['acknowledgedAt']=data['persistedAt'];proof['acknowledgement']['utcTicks']=data['persistedAt']
    commit,ack=proof['commit'],proof['acknowledgement']
    if fault=='reversed':ack['order']=commit['order']
    elif fault=='failed':commit['succeeded']=False
    elif fault=='unexecuted':commit['executed']=False
    elif fault in ('transaction','delivery','record'):ack[fault+'Id']='unrelated'
    elif fault=='persisted':
        if scenario=='AC-16.2-04':commit['persisted']['generation']=0
        else:commit['persisted']['location']['id']='other'
    elif fault=='observed':ack['observedRecord']=deepcopy(commit['persisted']);ack['observedRecord']['messageId' if scenario=='AC-16.2-04' else 'binding']='unrelated'
    elif fault=='time':ack['utcTicks']+=1
    elif fault=='missing':del proof['commit']
    round7_submit(repository,scenario,data,passes=fault=='equal-valid',schema=fault=='missing' or fault=='observed' and scenario=='AC-16.3-03')


@pytest.mark.parametrize('fault',['preloaded','no-growth','missing-before','wrong-delivery','pending-replaced','pending-extra','horizon-regressed','split-merge','merge-unexecuted','old-worker-current','stale-accepted','stale-mutates','stale-no-attempt','incomplete-replay','replay-content','replay-split','completion-content','completion-fence','completion-split','completion-allowed','completion-pending','old-worker-unbound'])
def test_round7_recovery_requires_observed_extension_exact_merges_and_fenced_completion(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-03']);rows=data['merges'];proof=data['recoverySchedule']
    if fault=='preloaded':rows[0]['before']['pending']=deepcopy(rows[-1]['pending'])
    elif fault=='no-growth':
        for row in rows:
            row['beforeHorizon']=row['afterHorizon']=14;row['pending']=deepcopy(rows[-1]['pending'])
            for phase in ('before','after'):row[phase]['horizon']=14;row[phase]['pending']=deepcopy(rows[-1]['pending'])
    elif fault=='missing-before':del rows[0]['before']
    elif fault=='wrong-delivery':rows[0]['delivered']['messageId']='other'
    elif fault=='pending-replaced':rows[1]['after']['pending']=rows[1]['after']['pending'][1:]
    elif fault=='pending-extra':rows[0]['after']['pending'].append(deepcopy(rows[-1]['pending'][-1]))
    elif fault=='horizon-regressed':rows[-1]['after']['horizon']=13
    elif fault=='split-merge':rows[0]['cas']['pendingCommitId']='other'
    elif fault=='merge-unexecuted':rows[0]['cas']['executed']=False
    elif fault=='old-worker-current':proof['oldWorker']['snapshot']=deepcopy(rows[-1]['after'])
    elif fault=='old-worker-unbound':
        proof['oldWorker']['expected']['horizon']+=1
        proof['staleCompletion']['cas']['expected']=deepcopy(proof['oldWorker']['expected'])
    elif fault=='stale-accepted':proof['staleCompletion']['cas']['succeeded']=True
    elif fault=='stale-mutates':proof['staleCompletion']['after']['pending']=[]
    elif fault=='stale-no-attempt':proof['staleCompletion']['cas']['executed']=False
    elif fault=='incomplete-replay':data['authoritativeReplay'].pop()
    elif fault=='replay-content':proof['replay']['after']['projection']['status']='Active'
    elif fault=='replay-split':proof['replay']['cas']['checkpointCommitId']='other'
    elif fault=='completion-content':proof['currentCompletion']['after']['pending']=deepcopy(rows[-1]['pending'])
    elif fault=='completion-fence':proof['currentCompletion']['cas']['expected']['horizon']-=1
    elif fault=='completion-allowed':proof['currentCompletion']['after']['allowed']=True
    elif fault=='completion-pending':
        proof['replay']['after']['pending']=deepcopy(rows[-1]['pending'])
        proof['currentCompletion']['before']['pending']=deepcopy(rows[-1]['pending'])
    else:proof['currentCompletion']['cas']['healthCommitId']='other'
    round7_submit(repository,'AC-16.1-03',data,schema=fault=='missing-before')


@pytest.mark.parametrize('kind',range(2))
@pytest.mark.parametrize('fault',['coherent-content','coherent-version','coherent-transaction'])
def test_round7_lifecycle_self_consistent_claims_still_require_source_values_and_commit_progress(repository,kind,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][kind];proof=row['atomicity']
    old=proof['stages'][1]['cas']['transactionId'];new=proof['stages'][0]['cas']['transactionId']
    def visit(value):
        if isinstance(value,dict):
            if fault=='coherent-content' and value.get('kind')=='detail' and isinstance(value.get('value'),dict):value['value']['label']='forged consistent content'
            if fault=='coherent-version' and 'version' in value:value['version']*=2
            if fault=='coherent-transaction':
                for key,item in value.items():
                    if item==old:value[key]=new
            for item in value.values():visit(item)
        elif isinstance(value,list):
            for item in value:visit(item)
    visit(proof)
    if fault=='coherent-version':row['lifecycleCasVersion']*=2;row['indexCasVersion']*=2
    round7_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('scenario,path',[
 ('AC-16.2-04',['lifecycle',0,'atomicity','captureVersion']),
 ('AC-16.1-03',['recoverySchedule','captureVersion']),
 ('AC-16.3-03',['pendingOperations','captureVersion']),
 ('AC-16.2-04',['lifecycle',0,'atomicity','stages',0,'after','index','schemaVersion']),
])
@pytest.mark.parametrize('value',[1.0,True,'1'])
def test_round7_capture_versions_require_actual_integer_tokens(repository,scenario,path,value):
    data=deepcopy(OBSERVATIONS[scenario]);target=data
    for key in path[:-1]:target=target[key]
    target[path[-1]]=2.0 if path[-1]=='schemaVersion' and type(value) is float else value
    round7_submit(repository,scenario,data,schema=True)


@pytest.mark.parametrize('policy',[b'not json',b'[]',b'{"sdk":[]}',b'{"sdk":{"version":42,"rollForward":"latestPatch"}}'])
def test_round7_actual_isolated_helper_setup_rejects_invalid_policy_before_dotnet(repository,policy):
    (repository/'global.json').write_bytes(policy)
    # Import the actual isolated helper installation, including its own contained
    # root/policy lookup. Observe real fixed-command calls instead of a validator-only surrogate.
    script='''import sys,json
sys.path.insert(0,sys.argv[1])
import epic16_native as native
calls=[]
original=native.subprocess.run
def run(argv,**kwargs):
    calls.append(argv)
    return original(argv,**kwargs)
native.subprocess.run=run
try:
    native._helper()
    result='PASS';error=None
except native.HelperUnavailable as exception:
    result='BLOCKED';error=type(exception).__name__
except ValueError as exception:
    result='FAIL';error=type(exception).__name__
print(json.dumps(dict(result=result,error=error,commands=calls)))
'''
    result=subprocess.run([sys.executable,'-c',script,str(repository/'_bmad/scripts')],cwd=repository,capture_output=True,timeout=180)
    assert result.returncode==0,result.stderr.decode()
    report=json.loads(result.stdout)
    assert report['result']=='FAIL' and report['error'] in ('ValueError','JSONDecodeError'),report
    assert report['commands']==[],report


# Iteration 8 protocols are future-v2 synthetic evidence fixtures, not runtime providers.
def round8_submit(repository, scenario, data, *, passes=False):
    native(repository, scenario, data)
    result=evaluate(repository,scenario)
    assert result['result']==('PASS' if passes else 'FAIL'),result
    if not passes:assert set(result['blockers']) & {'MEASUREMENT_INVALID','MEASUREMENT_SCHEMA_INVALID'},result
    assert result['assertionLedger']
    return result


def round8_query(snapshot,stage,transaction,order):
    lifecycle=tooling.measurements.lifecycle
    inputs=dict(tenantExists=True,authorized=True,providerState='Available',sourceGap=False)
    reads=[dict(kind='index',key=snapshot['index']['key'],generation=snapshot['index']['generation'])]+[dict(kind=p['kind'],key=p['key'],resource=p['resource'],generation=p['generation']) for p in snapshot['components']]
    return dict(captureVersion=1,stage=stage,requestId='query-'+stage,executed=True,completed=True,order=order,tenant=snapshot['tenant'],observedTransactionId=transaction,snapshot=deepcopy(snapshot),inputs=inputs,returned=lifecycle.query_result(snapshot,inputs),reads=reads,writes=[],recoveryCalls=[])


def round8_refresh(row):
    """Refresh adjacent fixture claims; independent guards must still reject faults."""
    lifecycle=tooling.measurements.lifecycle;names,contents=lifecycle.lifecycle_contents(row)
    stages=row['atomicity']['stages'];previous=deepcopy(contents[0]);previous['index'].update(version=7,etag=row['id']+'-initial')
    for n,(stage,value) in enumerate(zip(stages,contents[1:])):
        if row['id']=='rebuild' and stage['id']=='work':
            for delivery in row['catchUp']['deliveries']:
                en=delivery['enqueue'];en['storedBefore']=deepcopy(previous);en['expected']={key:previous['index'][key] for key in lifecycle.FENCES}
                after=deepcopy(previous);next(p for p in after['components'] if p['kind']=='queue')['value']=deepcopy(en['after'])
                tx=en['transactionId']
                if tx:after['index'].update(version=previous['index']['version']+1,etag=tx)
                en['storedAfter']=deepcopy(after);en['indexTransactionId']=tx;en['componentTransactions']=[dict(kind=p['kind'],resource=p['resource'],transactionId=tx) for p in after['components']] if tx else []
                previous=after
        after=deepcopy(value);after['index'].update(version=previous['index']['version']+1,etag=row['id']+'-'+stage['id'])
        stage['before']=deepcopy(previous);stage['after']=deepcopy(after);stage['plan'].update(expected={key:previous['index'][key] for key in lifecycle.FENCES},indexAfter=deepcopy(after['index']),componentsAfter=deepcopy(after['components']))
        stage['cas']['expected']=deepcopy(stage['plan']['expected']);previous=after
    stale=row['atomicity']['staleAttempt'];stale.update(plan=deepcopy(stages[0]['plan']),expected=deepcopy(stages[0]['plan']['expected']),storedBefore=deepcopy(previous),storedAfter=deepcopy(previous))
    row['lifecycleCasVersion']=row['indexCasVersion']=previous['index']['version'];row['componentGenerations']=[p['generation'] for p in previous['components']]
    row['queries']=[round8_query(s['after'],s['id'],s['cas']['transactionId'],s['cas']['order']+2) for s in stages]


@pytest.mark.parametrize('before,after,passes',[(10,999,False),(25,25,True),(25,24,False),(25,999,False),(10,20,True)])
def test_round8_completed_watermark_derives_from_barrier_and_prior_progress(repository,before,after,passes):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1]
    row.update(watermarkBefore=before,watermarkAfter=after);round8_refresh(row)
    round8_submit(repository,'AC-16.2-04',data,passes=passes)


@pytest.mark.parametrize('name',['recovery-success','two-replicas'])
@pytest.mark.parametrize('key',['version','lease','allowed','history','pending','sequence','status','saveOrder'])
@pytest.mark.parametrize('invalid',[False,True])
def test_round8_domain_map_keys_are_opaque_but_native_values_remain_validated(repository,name,key,invalid):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=next(r for r in data['faults'] if r['id']==name)
    before=json.loads(row['beforeBytes']);observed=row['observed']
    states=[before['state']['domain'],observed['domain']] if name=='recovery-success' else [r['domain'] for r in before['state'].values()]+[r['domain'] for r in observed.values()]
    for state in states:
        state['configuration'][key]=42 if invalid else 'customer-setting'
        state['members'][key]='TenantReader'
    row['beforeBytes']=row['afterBytes']=json.dumps(before,separators=(',',':'));row['beforeSha256']=row['afterSha256']=digest(row['beforeBytes'].encode())
    round8_submit(repository,'AC-16.1-04',data,passes=not invalid)


@pytest.mark.parametrize('name,field',[('recovery-success','checkpoint'),('two-replicas','sequence')])
@pytest.mark.parametrize('bad',[True,1.0,'customer-setting',-1])
def test_round8_structural_types_still_reject_aliases(repository,name,field,bad):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=next(r for r in data['faults'] if r['id']==name);before=json.loads(row['beforeBytes'])
    target=before['state'] if name=='recovery-success' else before['state']['replicaA'];target[field]=bad
    row['beforeBytes']=row['afterBytes']=json.dumps(before);row['beforeSha256']=row['afterSha256']=digest(row['beforeBytes'].encode())
    round8_submit(repository,'AC-16.1-04',data)


@pytest.mark.parametrize('index,identity',[(i,'delivery') for i in range(6)]+[(i,'record') for i in range(4)])
@pytest.mark.parametrize('value',[' ', '\t', '\u00a0','\x1c','\x1d','\x1e','\x1f'])
def test_round8_acknowledgement_native_identity_semantics(repository,index,identity,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['acknowledgements'][index]
    if identity=='delivery':
        row['deliveryId']=value
        for key in ('durableCommit','acknowledgement'):
            if row[key] is not None:row[key]['deliveryId']=value
    elif row['durableCommit'] is not None:
        row['durableCommit']['recordId']=row['acknowledgement']['recordId']=value
    if row['failureEvidence'] is not None:
        proof=row['failureEvidence'];proof['scope']['deliveryId']=row['deliveryId']
        for operation in [proof['beforeRead'],proof['processing'],*proof['attempts'],proof['completion'],proof['acknowledgements'],proof['restoration'],proof['afterRead']]:operation['scope']['deliveryId']=row['deliveryId']
    round8_submit(repository,'AC-16.1-04',data,passes=value in ('\x1c','\x1d','\x1e','\x1f'))


@pytest.mark.parametrize('fault',['missing','summary','acquire-unexecuted','acquire-split','acquire-fence','acquire-pending','acquire-projection','acquire-version','resume-unexecuted','resume-process','resume-order','resume-transaction','resume-delivery','resume-sequence','resume-clears','resume-allows','stale-accepted','stale-mutates','stale-not-attempted','stale-wrong-scope','stale-wrong-order','current-failed','current-mutates','current-split','current-wrong-expectation'])
def test_round8_restart_requires_executed_acquisition_resumption_and_admissible_fenced_writes(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-02']);p=data['restartSchedule'];a=p['acquisition'];r=p['resumption'];old=p['rejectedAttempts'][0];current=p['currentRenewal']
    if fault=='missing':del data['restartSchedule']
    elif fault=='summary':data['newFence']=999
    elif fault=='acquire-unexecuted':a['executed']=False
    elif fault=='acquire-split':a['leaseTransactionId']='other'
    elif fault=='acquire-fence':a['expected']['lease']=999
    elif fault=='acquire-pending':a['after']['pending']=[]
    elif fault=='acquire-projection':a['after']['projection']['status']='Disabled'
    elif fault=='acquire-version':a['after']['version']=a['before']['version']
    elif fault=='resume-unexecuted':r['executed']=False
    elif fault=='resume-process':r['process']='unrelated'
    elif fault=='resume-order':r['order']=a['order']
    elif fault=='resume-transaction':r['observedTransactionId']='other'
    elif fault=='resume-delivery':r['sourceDeliveries']=['new-input']
    elif fault=='resume-sequence':r['fromSequence']+=1
    elif fault=='resume-clears':r['storedAfter']['pending']=[]
    elif fault=='resume-allows':r['allowed']=True
    elif fault=='stale-accepted':old['succeeded']=True
    elif fault=='stale-mutates':old['storedAfter']['projection']['name']='lost newer state'
    elif fault=='stale-not-attempted':old['executed']=False
    elif fault=='stale-wrong-scope':old['tenant']='foreign'
    elif fault=='stale-wrong-order':old['order']=a['order']
    elif fault=='current-failed':current['succeeded']=False
    elif fault=='current-mutates':current['after']['pending']=[]
    elif fault=='current-split':current['recordTransactionId']='other'
    else:current['expected']['horizon']-=1
    round8_submit(repository,'AC-16.1-02',data)


@pytest.mark.parametrize('index',range(1,5))
@pytest.mark.parametrize('fault',['accepted','confounded','no-attempt','mutated-state','no-proposal'])
def test_round8_each_restart_precondition_is_independently_measured(repository,index,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-02']);p=data['restartSchedule'];row=p['rejectedAttempts'][index]
    if fault=='accepted':row['succeeded']=True
    elif fault=='confounded':row['expected']['etag']='unrelated'
    elif fault=='no-attempt':row['executed']=False
    elif fault=='mutated-state':row['storedAfter']['pending']=[]
    else:del row['proposed']
    round8_submit(repository,'AC-16.1-02',data)


@pytest.mark.parametrize('fault',['missing','lost-ref','forged-ref','foreign','generation','event','identity-conflict','enqueue-unexecuted','enqueue-before','enqueue-after','enqueue-split','enqueue-snapshot','enqueue-fence','ack-order','ack-identity','drain-unexecuted','drain-before','premature-drain','lost-at-ready','drain-transaction','wrong-stream'])
def test_round8_catch_up_requires_authoritative_references_complete_enqueue_and_drain(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1];p=row['catchUp'];delivery=p['deliveries'][1];en=delivery['enqueue']
    if fault=='missing':del row['catchUp']
    elif fault=='lost-ref':p['drain']['after']=[];row['queuedAfterC']=[];round8_refresh(row)
    elif fault=='forged-ref':delivery['reference']['messageId']='forged';delivery['referenceId']=digest(tooling.canonical(delivery['reference']))
    elif fault=='foreign':delivery['reference']['tenant']='foreign'
    elif fault=='generation':delivery['reference']['generation']-=1
    elif fault=='event':delivery['authoritative']['eventId']='unrelated'
    elif fault=='identity-conflict':p['deliveries'][-1]['authoritative']['position']=22
    elif fault=='enqueue-unexecuted':en['executed']=False
    elif fault=='enqueue-before':en['before']=[deepcopy(delivery['reference'])]
    elif fault=='enqueue-after':en['after']=[]
    elif fault=='enqueue-split':en['indexTransactionId']='other'
    elif fault=='enqueue-snapshot':en['storedAfter']['index']['watermark']=999
    elif fault=='enqueue-fence':en['expected']['version']+=1
    elif fault=='ack-order':delivery['operations']['acknowledgement']['order']=en['order']
    elif fault=='ack-identity':delivery['operations']['acknowledgement']['recordId']='unrelated'
    elif fault=='drain-unexecuted':p['drain']['executed']=False
    elif fault=='drain-before':p['drain']['before']=[];round8_refresh(row)
    elif fault=='premature-drain':p['drain']['drained']+=p['drain']['after'];p['drain']['after']=[];row['queuedAfterC']=[];round8_refresh(row)
    elif fault=='lost-at-ready':next(x for x in row['atomicity']['stages'][-1]['after']['components'] if x['kind']=='queue')['value']=[]
    elif fault=='drain-transaction':p['drain']['transactionId']='unrelated'
    else:delivery['streamIdentity']['conversation']='other'
    round8_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('kind',range(2))
@pytest.mark.parametrize('fault',['missing','unexecuted','incomplete','wrong-snapshot','wrong-transaction','wrong-order','stale-content','false-current','false-empty','writes','recovery','missing-reads','unauthorized-control'])
def test_round8_lifecycle_queries_execute_between_precise_committed_boundaries(repository,kind,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][kind];q=row['queries'][1]
    if fault=='missing':row['queries']=[]
    elif fault=='unexecuted':q['executed']=False
    elif fault=='incomplete':q['completed']=False
    elif fault=='wrong-snapshot':q['snapshot']['index']['generation']-=1
    elif fault=='wrong-transaction':q['observedTransactionId']='other'
    elif fault=='wrong-order':q['order']=row['atomicity']['stages'][1]['cas']['order']
    elif fault=='stale-content':q['returned']['content']=[deepcopy(OBSERVATIONS['AC-16.3-03']['beforeProjection'])]
    elif fault=='false-current':q['returned'].update(status='Current',reason='current')
    elif fault=='false-empty':row['queries'][-1]['returned'].update(status='Current',reason='current',content=[])
    elif fault=='writes':q['writes']=['repair']
    elif fault=='recovery':q['recoveryCalls']=['recover']
    elif fault=='missing-reads':q['reads']=[]
    else:q['inputs']['authorized']=False;q['reads']=[];q['returned']=dict(status='Forbidden',reason='forbidden',content=[])
    round8_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('index',range(5))
@pytest.mark.parametrize('fault',['valid','wrong-state','wrong-generation','missing-read','no-call'])
def test_round8_generation_query_contexts_preserve_distinct_public_mappings(repository,index,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-05']);p=data['generationQueries'];q=p['queries'][index]
    if fault=='wrong-state':q['returned']['status']='Unavailable' if index in (1,2,3) else 'Rebuilding'
    elif fault=='wrong-generation':q['snapshot']['index']['metadataGenerations']['watermark']+=1
    elif fault=='missing-read':q['reads']=[]
    elif fault=='no-call':q['executed']=False
    round8_submit(repository,'AC-16.2-05',data,passes=fault=='valid')


@pytest.mark.parametrize('field',['index','lifecycle','manifest','watermark','dispatch'])
def test_round8_internal_control_record_contradiction_requires_named_metadata(repository,field):
    data=deepcopy(OBSERVATIONS['AC-16.2-03']);row=next(q for q in data['queries'] if q['id']=='mixed')
    row['controlMetadata']={k:3 for k in row['controlMetadata']};row['controlMetadata'][field]=2
    round8_submit(repository,'AC-16.2-03',data,passes=True)
    row['controlMetadata'][field]=3
    round8_submit(repository,'AC-16.2-03',data)


@pytest.mark.parametrize('fault',['valid','zero','bound','over-bound','missing-request','no-observation','unrelated-completion','backwards-order','backwards-time','unfinished','cleanup-missing','leaked-port','removed-preexisting','wrong-owner','wrong-resource','unrelated-release','summary-only','wrong-clock'])
def test_round8_cancellation_measures_request_through_cleanup_completion(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-05']);p=data['cancellation'];ops=p['operations']
    if fault=='zero':
        for row in ops:row['monotonicMs']=10
        data['cancelElapsedMs']=0
    elif fault in ('bound','over-bound'):ops[-1]['monotonicMs']=ops[1]['monotonicMs']+(100 if fault=='bound' else 101);data['cancelElapsedMs']=ops[-1]['monotonicMs']-ops[1]['monotonicMs']
    elif fault=='missing-request':ops.pop(1)
    elif fault=='no-observation':ops[2]['executed']=False
    elif fault=='unrelated-completion':ops[-1]['operationId']='other'
    elif fault=='backwards-order':ops[3]['order']=ops[1]['order']
    elif fault=='backwards-time':ops[3]['monotonicMs']=0
    elif fault=='unfinished':ops[-1]['completed']=False
    elif fault=='cleanup-missing':ops[3]['executed']=False
    elif fault=='leaked-port':ops[-1]['resources'].append(deepcopy(p['owned'][0]))
    elif fault=='removed-preexisting':ops[-1]['resources']=[]
    elif fault=='wrong-owner':p['owned'][0]['owner']='unrelated'
    elif fault=='wrong-resource':p['owned'][0]['port']=59999
    elif fault=='unrelated-release':ops[3]['released']=[p['preexisting'][0]['id']]
    elif fault=='summary-only':data['cancelElapsedMs']=0
    elif fault=='wrong-clock':ops[-1]['clockId']='other'
    round8_submit(repository,'AC-16.3-05',data,passes=fault in ('valid','zero','bound'))


def round8_requeue(row):
    """Rebuild adjacent queue fixture observations after changing delivery inputs."""
    lifecycle=tooling.measurements.lifecycle;pending=[]
    for n,delivery in enumerate(row['catchUp']['deliveries']):
        observed=delivery['authoritative'];reference=lifecycle.queue_reference(observed,row['afterGeneration']);delivery['reference']=reference;delivery['referenceId']=digest(tooling.canonical(reference))
        delivery['streamIdentity']=dict(id=observed['stream'],tenant=observed['event']['tenant'],conversation=observed['event']['conversation'])
        en=delivery['enqueue'];queue=reference['position']>row['B'];duplicate=reference in pending
        following=lifecycle.queue_inventory(pending+([reference] if queue and not duplicate else []));transaction='tx-reordered-'+str(n) if queue else None;order=20+10*n
        en.update(order=order,before=deepcopy(pending),after=deepcopy(following),duplicate=duplicate,transactionId=transaction,succeeded=True if queue else None)
        delivery['acknowledged']=queue
        if queue:
            operation=dict(executed=True,deliveryId=delivery['deliveryId'],recordId=delivery['referenceId'],transactionId=transaction,utcTicks=2200)
            delivery['operations']=dict(captureVersion=1,scheduleId='schedule-'+delivery['deliveryId'],commit=dict(operation,operation='commit',order=order,succeeded=True,persisted=deepcopy(reference)),acknowledgement=dict(operation,operation='acknowledge',order=order+1,observedRecord=deepcopy(reference)))
        else:delivery['operations']=None
        pending=following
    drain=row['catchUp']['drain'];drain.update(before=deepcopy(pending),drained=[r for r in pending if r['position']<=row['C']],after=[r for r in pending if r['position']>row['C']])
    row['queuedAfterC']=[r['position'] for r in drain['after']];round8_refresh(row)


@pytest.mark.parametrize('mode',['reordered','before-c','new-stream','duplicate','reuse-sequence','reuse-event','reuse-message','wrong-stream-map'])
def test_round8_queue_accepts_arrival_patterns_but_preserves_authoritative_identity(repository,mode):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1];deliveries=row['catchUp']['deliveries']
    if mode=='reordered':deliveries[:]=[deliveries[2],deliveries[1],deliveries[3],deliveries[0]]
    elif mode=='before-c':
        row['B']=5;row['discoveryAt']=5;stream=row['streams'][0];stream.update(boundaryAt=5,boundarySequence=1,replayedThroughB=1)
    elif mode=='duplicate':deliveries.append(deepcopy(deliveries[1]));deliveries[-1]['deliveryId']='extra-duplicate'
    elif mode=='new-stream':
        event=deepcopy(row['streams'][0]['authoritativeEvents'][0]['event']);event.update(conversation='conversation-new',messageId='delivery-new');payload=json.loads(event['payload']);metadata=payload['metadata'];metadata.update(eventId='event-new',conversationId='conv:conversation-new',deduplicationKey='tenant:tenant-a|conv:conversation-new|event-new|1');event['payload']=json.dumps(payload,separators=(',',':'))
        extra=deepcopy(deliveries[1]);extra['deliveryId']='arrival-new';extra['authoritative']=dict(stream='new-stream',sequence=1,eventId='event-new',position=50,event=event,eventSha256=digest(tooling.canonical(event)));deliveries.append(extra)
    else:
        # Reuse a creation-history identity that was not among original deliveries.
        extra=deepcopy(deliveries[1]);extra['deliveryId']='history-reuse';observed=extra['authoritative'];event=observed['event'];payload=json.loads(event['payload'])
        event.update(sequence=9,messageId='delivery-later-unique');observed.update(sequence=9,eventId='event-later-unique')
        payload['metadata'].update(eventId='event-later-unique',deduplicationKey='tenant:tenant-a|conv:conversation-a|event-later-unique|1');event['payload']=json.dumps(payload,separators=(',',':'))
        if mode=='reuse-sequence':event['sequence']=1;observed['sequence']=1
        elif mode=='reuse-message':event['messageId']=row['streams'][0]['authoritativeEvents'][0]['event']['messageId']
        elif mode=='reuse-event':payload['metadata']['eventId']='event-1';payload['metadata']['deduplicationKey']='tenant:tenant-a|conv:conversation-a|event-1|1';event['payload']=json.dumps(payload,separators=(',',':'));observed['eventId']='event-1'
        observed['position']=50;observed['eventSha256']=digest(tooling.canonical(event));deliveries.append(extra)
    round8_requeue(row)
    if mode=='wrong-stream-map':deliveries[-1]['streamIdentity']['id']='unrelated'
    round8_submit(repository,'AC-16.2-04',data,passes=mode in ('reordered','before-c','new-stream','duplicate'))


@pytest.mark.parametrize('scenario,path',[
 ('AC-16.1-02',['restartSchedule','captureVersion']),
 ('AC-16.2-04',['lifecycle',1,'catchUp','deliveries',1,'enqueue','order']),
 ('AC-16.2-04',['lifecycle',0,'queries',0,'executed']),
 ('AC-16.2-05',['generationQueries','baseline','index','metadataGenerations','manifest']),
 ('AC-16.3-05',['cancellation','operations',1,'monotonicMs']),
])
@pytest.mark.parametrize('value',[True,1.0,'1',None])
def test_round8_new_capture_schema_rejects_primitive_aliases(repository,scenario,path,value):
    if path[-1]=='executed' and value is True:value=1
    data=deepcopy(OBSERVATIONS[scenario]);target=data
    for key in path[:-1]:target=target[key]
    target[path[-1]]=value
    result=round8_submit(repository,scenario,data)
    assert 'MEASUREMENT_SCHEMA_INVALID' in result['blockers']


# Iteration 9 uses source-stamped, explicitly synthetic product stand-ins. No
# default test reads initialized submodules or grants product runtime authority.
_PRODUCT_BYTES = {}
_DEPENDENCY_TEMPLATES = None
_COMMIT_MESSAGES = {}


def round9_product_source(product):
    return '\n'.join('namespace '+name.rsplit('.',1)[0]+' { public class '+name.rsplit('.',1)[1]+' { public string Existing(string value) => value; } }' for name in product['surfaces'])+'\n'


def round9_compile_product(product, revision, source=None):
    import tempfile
    source=round9_product_source(product) if source is None else source
    key=(product['assemblyName'],revision,source)
    if key in _PRODUCT_BYTES:return _PRODUCT_BYTES[key]
    directory=Path(tempfile.mkdtemp(prefix='epic16-product-fixture-'))
    (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (directory/'Product.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><AssemblyName>'+product['assemblyName']+'</AssemblyName></PropertyGroup></Project>')
    (directory/'Product.cs').write_text(source)
    (directory/'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
    result=subprocess.run(['dotnet','build','Product.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=directory,capture_output=True,timeout=120)
    assert result.returncode==0,result.stdout.decode()
    binary=(directory/('bin/Debug/net10.0/'+product['assemblyName']+'.dll')).read_bytes()
    _PRODUCT_BYTES[key]=binary
    if os.environ.get('EPIC16_R11_NATIVE_EVIDENCE'):
        round11_retain('product-build-'+digest(json.dumps(key).encode()), {**{name:(directory/name).read_bytes() for name in ('Product.cs','Product.csproj','Stamp.cs','global.json')}, 'Product.dll.bytes':binary,'build.stdout':result.stdout,'build.stderr':result.stderr}, dict(argv=['dotnet','build','Product.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=str(directory),exitCode=result.returncode))
    shutil.rmtree(directory)
    return binary


def round9_dependencies(repo):
    global _DEPENDENCY_TEMPLATES
    import tempfile
    if _DEPENDENCY_TEMPLATES is None:
        _DEPENDENCY_TEMPLATES=Path(tempfile.mkdtemp(prefix='epic16-dependency-source-fixtures-'))
        for owner in ('references/Hexalith.EventStore','references/Hexalith.Tenants','references/Hexalith.Commons'):
            child=_DEPENDENCY_TEMPLATES/owner;child.mkdir(parents=True)
            (child/'.gitignore').write_text('artifacts/\n')
            for product in tooling.products.PRODUCTS:
                if product['owner']==owner:
                    path=child/'fixture-products'/str(product['assemblyName']+'.cs');path.parent.mkdir(exist_ok=True);path.write_text(round9_product_source(product))
            if owner.endswith('EventStore'):
                path=child/'src/Hexalith.EventStore.Client/Projections/ReadModelBatchCanonicalJson.cs';path.parent.mkdir(parents=True);path.write_bytes((ROOT/'_bmad/scripts/epic16_native_helper/ReadModelBatchCanonicalJson.cs').read_bytes())
            git(child,'init','-q');git(child,'config','user.name','Disposable Fixture');git(child,'config','user.email','fixture@example.invalid');git(child,'add','.');git(child,'commit','-qm','test: retain synthetic product source fixtures')
    declarations=[]
    for owner in ('references/Hexalith.EventStore','references/Hexalith.Tenants','references/Hexalith.Commons'):
        child=repo/owner;child.parent.mkdir(parents=True,exist_ok=True);shutil.copytree(_DEPENDENCY_TEMPLATES/owner,child)
        revision=git(child,'rev-parse','HEAD');git(repo,'update-index','--add','--cacheinfo','160000,'+revision+','+owner)
        declarations.append('[submodule "'+owner+'"]\n\tpath = '+owner+'\n\turl = https://example.invalid/disposable\n')
    (repo/'.gitmodules').write_text(''.join(declarations))
    for product in tooling.products.PRODUCTS:
        if product['owner']=='.':
            path=repo/'fixture-products'/str(product['assemblyName']+'.cs');path.parent.mkdir(exist_ok=True);path.write_text(round9_product_source(product))


def round9_api_observations(repo, data, scope=SCOPE):
    rows=[]
    for product in tooling.products.PRODUCTS:
        owner=product['owner']
        if product['assemblyName'] not in tooling.products.UPSTREAM_API_NAMES:continue
        revision=git(repo/owner,'rev-parse','HEAD')
        binary=round10_product_bytes(repo,product,revision)
        path=f'artifacts/epic16-{scope}/16.1/binaries/{digest(binary)}.dll';target=repo/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(binary)
        artifact=dict(path=path,sha256=digest(binary),revision=revision)
        rows.append(dict(assemblyName=product['assemblyName'],owner=owner,baseline=artifact,candidate=deepcopy(artifact)))
    data['upstreamSurfaces']=rows


def round9_loaded_products(repo,candidate):
    rows=[]
    for product in tooling.products.PRODUCTS:
        revision=candidate if product['owner']=='.' else git(repo/product['owner'],'rev-parse','HEAD')
        binary=round10_product_bytes(repo,product,revision)
        path=repo/'artifacts/measurement-products'/str(product['assemblyName']+'.dll');path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(binary)
        meta=tooling.native_helper.inspect_product(binary,revision)
        rows.append(dict(assemblyName=product['assemblyName'],owner=product['owner'],sourceRevision=revision,mvid=meta['mvid'],sha256=digest(binary),codeBase=str(path)))
    return rows


def round9_rejections(data):
    state=json.loads(data['rebuilds'][0]['canonicalJson']);rows=[]
    for i,name in enumerate(('unstable-key','wrong-order','generation-before-anchor')):
        invalid=deepcopy(state)
        if name=='unstable-key':
            item=next(x for x in invalid['items'] if not x['key'].startswith('conversation:'));kind=item['key'].split(':')[0]
            item['value'][dict(participants='participantPartyId',messages='messageId',fileReferences='fileId')[kind]]='';item['key']=kind+':'
        elif name=='wrong-order':invalid['items'].reverse()
        ticks=state['anchorUtcTicks']-1 if i==2 else state['anchorUtcTicks'];raw=data['rebuilds'][0]['canonicalJson'];conversation=state['items'][0]['value']
        rows.append(dict(captureVersion=1,id=name,operationId='negative-'+name,executed=True,completed=True,tenant=conversation['tenantId'],conversation=conversation['conversationId'],writer='ConversationProjectionReadModelWriter',beforeBytes=raw,afterBytes=raw,beforeSha256=digest(raw.encode()),afterSha256=digest(raw.encode()),attemptedJson=json.dumps(invalid,separators=(',',':')),generatedTicks=ticks,observedGeneratedTicks=ticks,rejected=True,writes=[],committed=False,publicResult=dict(status='Unavailable',reason='metadata_contradictory') if i==2 else None))
    data['rejectedOperations']=rows


def round9_clear_capture(repo):
    capture=json.loads((repo/tooling.capture.capture_path('16.2','AC-16.2-02')).read_bytes())
    for path in {row['path'] for row in [capture['binary'],*capture['products']]}:(repo/path).unlink()


def round9_reject(repo,scenario,data):
    native(repo,scenario,data)
    result=evaluate(repo,scenario)
    assert result['result']=='FAIL' and result['exitCode']==1 and result['blockers'] and result['assertionLedger'],result


@pytest.mark.parametrize('case',range(3))
@pytest.mark.parametrize('fault',['absent','execution','completion','tenant','resource','writer','valid-input','false-rejection','committed','writes','changed-state','wrong-hash','clamped'])
def test_round9_rejected_replay_requires_executed_invalid_input_and_unchanged_state(repository,case,fault):
    scenario='AC-16.2-02';data=deepcopy(OBSERVATIONS[scenario]);row=data['rejectedOperations'][case]
    if fault=='absent':data['rejectedOperations'].pop(case)
    elif fault=='execution':row['executed']=False
    elif fault=='completion':row['completed']=False
    elif fault=='tenant':row['tenant']='foreign'
    elif fault=='resource':row['conversation']='foreign'
    elif fault=='writer':row['writer']='unrelated'
    elif fault=='valid-input':
        if case==2:row['generatedTicks']=row['observedGeneratedTicks']=data['rebuilds'][0]['anchorTicks']
        else:row['attemptedJson']=data['rebuilds'][0]['canonicalJson']
    elif fault=='false-rejection':row['rejected']=False
    elif fault=='committed':row['committed']=True
    elif fault=='writes':row['writes']=['unsafe-state']
    elif fault=='changed-state':
        value=json.loads(row['beforeBytes']);value['items'][0]['value']['label']='unsafe persisted content';row['afterBytes']=tooling.native_helper.canonicalize(json.dumps(value));row['afterSha256']=digest(row['afterBytes'].encode())
    elif fault=='wrong-hash':row['beforeSha256']='f'*64
    else:row['observedGeneratedTicks']+=1
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('field',['providerState','lifecycle'])
@pytest.mark.parametrize('case',['v1','provider','unauthorized'])
def test_round9_query_domains_are_closed_before_mapping_precedence(repository,field,case):
    scenario='AC-16.2-03';data=deepcopy(OBSERVATIONS[scenario]);next(row for row in data['queries'] if row['id']==case)[field]='UnsupportedValue'
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('fault',['missing-dispatch','foreign-dispatch','foreign-resource','unindexed-dispatch','duplicate-dispatch','foreign-detail','wrong-detail-key'])
def test_round9_cross_key_admission_rejects_incomplete_or_foreign_components(repository,fault):
    scenario='AC-16.2-05';data=deepcopy(OBSERVATIONS[scenario]);proof=data['generationQueries']
    for snapshot in [proof['baseline'],*[q['snapshot'] for q in proof['queries']]]:
        part=next(p for p in snapshot['components'] if p['kind']=='dispatch')
        if fault=='missing-dispatch':snapshot['components'].remove(part)
        elif fault=='foreign-dispatch':part['tenant']='foreign'
        elif fault=='foreign-resource':part['resource']='foreign'
        elif fault=='unindexed-dispatch':part['value']=['pending-hidden']
        elif fault=='duplicate-dispatch':snapshot['components'].append(deepcopy(part))
        elif fault=='foreign-detail':next(p for p in snapshot['components'] if p['kind']=='detail')['tenant']='foreign'
        else:next(p for p in snapshot['components'] if p['kind']=='detail')['key']='projection:foreign'
    for q in proof['queries']:
        if fault=='missing-dispatch':q['reads']=[r for r in q['reads'] if r['kind']!='dispatch']
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('story',tooling.measurements.STORIES)
@pytest.mark.parametrize('missing',['epic16_capture.py','epic16_products.py','epic16_execution.py','epic16_domain.py','contained_reader.py'])
def test_round9_all_wrappers_report_missing_internal_imports_without_artifacts(tmp_path,story,missing):
    repo=tmp_path/'partial';(repo/'_bmad/scripts').mkdir(parents=True)
    for path in tooling.TOOL_SOURCES:
        if path.endswith('.py') and Path(path).name!=missing:
            target=repo/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((ROOT/path).read_bytes())
    output='artifacts/never-written.json'
    command=[sys.executable,str(repo/f'_bmad/scripts/verify_story_{story.replace(".","_")}.py'),'--repository',str(repo),'--contract',tooling.contract_path(story),'--scenario',f'AC-{story}-01','--output',output]
    result=subprocess.run(command,capture_output=True,timeout=30)
    assert result.returncode==2 and b'Traceback' not in result.stderr,(result.stdout,result.stderr)
    report=json.loads(result.stdout);assert report['result']=='BLOCKED' and report['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE'] and report['assertionLedger']
    assert not (repo/output).exists()


@pytest.mark.parametrize('case',range(5))
@pytest.mark.parametrize('fault',['missing','execution','completion','request-id','scope','route','method','order'])
def test_round9_wire_requires_same_executed_authorized_request_response(repository,case,fault):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);proof=data['wireObservation'];row=proof['operations'][case]
    if fault=='missing':proof['operations'].pop(case)
    elif fault=='execution':row['executed']=False
    elif fault=='completion':row['completed']=False
    elif fault=='request-id':row['requestId']='unrelated'
    elif fault=='scope':row['scope']['pendingId']='unrelated'
    elif fault=='route':row['path']='/project/v2'
    elif fault=='method':row['method']='GET'
    else:row['order']=0 if case==0 else proof['operations'][case-1]['order']
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('fault',['unauthenticated','denied','wrong-policy','handler-bypass','wrong-coordinator','wrong-dispatch','failed-result','uncommitted-response','wrong-state-hash','content-leak','wrong-contract','wrong-pending','wrong-dispatch-id'])
def test_round9_wire_rejects_uncorrelated_success_or_unsafe_diagnostics(repository,fault):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);proof=data['wireObservation'];request,auth,dispatch,commit,response=proof['operations']
    if fault=='unauthenticated':auth['authenticated']=False
    elif fault=='denied':auth['allowed']=False
    elif fault=='wrong-policy':auth['policy']='unrelated'
    elif fault=='handler-bypass':dispatch['directHandlerCalls']=1
    elif fault=='wrong-coordinator':dispatch['coordinator']='Conversations'
    elif fault=='wrong-dispatch':dispatch['target']='handler.Invoke'
    elif fault=='failed-result':response['statusCode']=500
    elif fault=='uncommitted-response':response['instrumentation']['transactionId']='unrelated'
    elif fault=='wrong-state-hash':commit['persistedAfterSha256']='f'*64
    elif fault=='content-leak':commit['persistedAfter']=data['concurrentRecovery']['current']['after']
    elif fault=='wrong-contract':response['contractType']='unrelated'
    elif fault=='wrong-pending':proof['scheduledWork']['pendingId']='unrelated'
    else:request['dispatchId']='unrelated'
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('scenario',['AC-16.3-01','AC-16.3-05'])
@pytest.mark.parametrize('fault',['missing','unbounded','used','elapsed','remaining','reason','unsafe','action'])
def test_round9_readiness_diagnostic_emits_only_bound_safe_retry_advice(repository,scenario,fault):
    data=deepcopy(OBSERVATIONS[scenario]);row=data['diagnostics'][0];advice=row['retryAdvice']
    if fault=='missing':row.pop('retryAdvice')
    elif fault=='unbounded':advice['attemptLimit']=999999
    elif fault=='used':advice['attemptsUsed']+=1
    elif fault=='elapsed':advice['elapsedMs']+=1
    elif fault=='remaining':advice['automaticRetriesRemaining']=1
    elif fault=='reason':advice['terminalReason']='unlimited'
    elif fault=='unsafe':advice['token']='secret-canary'
    else:advice['action']='retry-forever'
    round9_reject(repository,scenario,data)


@pytest.mark.parametrize('fault',['retained','missing','unrelated','before-hash','after-hash'])
def test_round9_warning_is_derived_from_candidate_surface_after_prerequisites(repository,fault):
    scenario='AC-16.1-05';data=deepcopy(OBSERVATIONS[scenario]);path=tooling.measurements.execution.WARNING_PATH
    if fault in ('retained','missing'):
        if fault=='retained':(repository/path).write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
        else:(repository/path).unlink()
        git(repository,'add',path);git(repository,'commit','-qm','test: exercise candidate warning surface')
        for ac in tooling.measurements.SCENARIOS:native(repository,ac)
        if fault=='retained':data['warningSurface']['afterSha256']=digest((repository/path).read_bytes())
    elif fault=='unrelated':data['warningSurface']['path']='docs/historical.md'
    else:data['warningSurface']['beforeSha256' if fault=='before-hash' else 'afterSha256']='f'*64
    native(repository,scenario,data);result=evaluate(repository,scenario)
    assert result['result']==('BLOCKED' if fault=='missing' else 'FAIL'),result
    assert result['blockers'] and result['assertionLedger']


@pytest.mark.parametrize('product_index',range(len(tooling.products.PRODUCTS)))
@pytest.mark.parametrize('fault',['bytes','managed-bytes','mvid','owner','revision','omitted','relabelled','reported-owner','capture-owner','reported-source','capture-source','substituted'])
def test_round9_loaded_products_are_independently_owned_and_bound(qualified_runtime,product_index,fault):
    repo=qualified_runtime;scenario='AC-16.2-02';path=repo/tooling.native_path('runtime',scenario);tree=ET.fromstring(path.read_bytes());output=tree.find('./{*}Results/{*}UnitTestResult/{*}Output/{*}StdOut');payload=json.loads(output.text)
    capture_path=repo/tooling.capture.capture_path('16.2',scenario);capture=json.loads(capture_path.read_bytes());row=payload['loadedProducts'][product_index];artifact=capture['products'][product_index]
    if fault=='bytes':(repo/artifact['path']).write_bytes(b'not managed product bytes')
    elif fault=='managed-bytes':
        binary=repo/artifact['path'];binary.write_bytes(binary.read_bytes()+b'measured-overlay-drift')
    elif fault=='reported-owner':row['owner']='foreign'
    elif fault=='capture-owner':artifact['owner']='foreign'
    elif fault=='reported-source':row['sourceRevision']='f'*40
    elif fault=='capture-source':artifact['sourceRevision']='f'*40
    elif fault=='substituted':
        other=capture['products'][(product_index+1)%len(tooling.products.PRODUCTS)]
        for field in ('path','sha256','mvid'):artifact[field]=other[field]
        for field in ('sha256','mvid'):row[field]=other[field]
    elif fault=='mvid':row['mvid']=artifact['mvid']='00000000-0000-0000-0000-000000000000'
    elif fault=='owner':row['owner']=artifact['owner']='references/Foreign'
    elif fault=='revision':row['sourceRevision']=artifact['sourceRevision']='f'*40
    elif fault=='omitted':payload['loadedProducts'].pop(product_index)
    else:row['assemblyName']=artifact['assemblyName']='Foreign.Assembly'
    output.text=json.dumps(payload);path.write_bytes(ET.tostring(tree));capture['native']['sha256']=digest(path.read_bytes());capture_path.write_bytes(tooling.json_bytes(capture))
    result=runtime_evaluate(repo);assert result['result']=='FAIL' and result['blockers'],result


@pytest.mark.parametrize('fault',['missing','extra','duplicate'])
def test_round9_native_product_inventory_cannot_be_selected_by_observations(qualified_runtime,fault):
    repo=qualified_runtime;scenario='AC-16.2-02';path=repo/tooling.native_path('runtime',scenario);tree=ET.fromstring(path.read_bytes());output=tree.find('./{*}Results/{*}UnitTestResult/{*}Output/{*}StdOut');payload=json.loads(output.text)
    if fault=='missing':payload.pop('loadedProducts')
    elif fault=='extra':payload['loadedProducts'].append(deepcopy(payload['loadedProducts'][0]))
    else:payload['loadedProducts'][-1]=deepcopy(payload['loadedProducts'][0])
    output.text=json.dumps(payload);path.write_bytes(ET.tostring(tree));capture_path=repo/tooling.capture.capture_path('16.2',scenario);capture=json.loads(capture_path.read_bytes());capture['native']['sha256']=digest(path.read_bytes());capture_path.write_bytes(tooling.json_bytes(capture))
    result=runtime_evaluate(repo);assert result['result']=='FAIL' and result['blockers'],result


@pytest.mark.parametrize('kind',['binary','capture','product'])
@pytest.mark.parametrize('fault',['altered','hardlink','symlink'])
def test_round9_final_inventory_rereads_admitted_bytes_and_aliases(qualified_runtime,kind,fault):
    repo=qualified_runtime;row=runtime_evaluate(repo);assert row['result']=='PASS',row
    binary=row['bindings']['testBinary'];entry=binary if kind=='binary' else binary['capture'] if kind=='capture' else binary['products'][0]
    path=repo/entry['path'];original=path.read_bytes()
    if fault=='altered':path.write_bytes(original+b'changed')
    else:
        path.unlink();alias=repo/'artifacts/alias-input';alias.parent.mkdir(exist_ok=True);alias.write_bytes(original)
        if fault=='hardlink':os.link(alias,path)
        else:path.symlink_to(alias)
    with tooling.ContainedReader(repo) as reader,pytest.raises((tooling.Stop,tooling.ReadError)) as error:
        tooling.capture.inventory(reader,'16.2',[row]*5,tooling.need)
    assert getattr(error.value,'code',None) in ('NATIVE_CAPTURE_DRIFT','NATIVE_CAPTURE_ALIAS','PATH_SYMLINK','INPUT_SYMLINK','PATH_INVALID','INPUT_UNSAFE','PATH_ESCAPE'),str(error.value)


@pytest.mark.parametrize('change',['addition','removed-member','signature','type','wrong-revision','wrong-owner','forged-digest','missing-assembly'])
def test_round9_upstream_additivity_uses_raw_gitlinks_and_actual_public_metadata(repository,change):
    repo=repository;scenario='AC-16.1-05';warning=repo/tooling.measurements.execution.WARNING_PATH
    owner='references/Hexalith.Tenants';product=next(p for p in tooling.products.PRODUCTS if p['assemblyName']=='Hexalith.Tenants.Client')
    previous=git(repo/owner,'rev-parse','HEAD');original=round9_product_source(product);old_binary=round9_compile_product(product,previous,original)
    # Create an actual earlier root baseline carrying the warning, then a child
    # dependency change and current source with warning removed. No trust is added.
    warning.write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
    git(repo,'add',str(warning.relative_to(repo)));git(repo,'commit','-qm','test: retain upstream API comparison baseline')
    source=original.replace('public string Existing(string value) => value;', 'public string Existing(string value) => value; public int Added() => 1;')
    if change=='removed-member':source=original.replace('Existing(', 'RemovedReplacement(')
    elif change=='signature':source=original.replace('string Existing(string value)','int Existing(int value)')
    elif change=='type':source=original.replace('public class','public struct')
    source_path=repo/owner/'fixture-products'/str(product['assemblyName']+'.cs');source_path.write_text(source)
    git(repo/owner,'add','.');git(repo/owner,'commit','-qm','test: change synthetic upstream public API')
    candidate=git(repo/owner,'rev-parse','HEAD');git(repo,'update-index','--cacheinfo','160000,'+candidate+','+owner)
    warning.write_bytes(b'// Synthetic disposable candidate with qualified durable projection.\n');git(repo,'add',str(warning.relative_to(repo)));git(repo,'commit','-qm','test: bind synthetic upstream API candidate')
    for ac in tooling.measurements.SCENARIOS:native(repo,ac)
    data=deepcopy(OBSERVATIONS[scenario]);round9_api_observations(repo,data)
    row=next(r for r in data['upstreamSurfaces'] if r['assemblyName']==product['assemblyName'])
    for baseline_product in tooling.products.PRODUCTS:
        if baseline_product['owner']!=owner:continue
        old_bytes=round9_compile_product(baseline_product,previous,round9_product_source(baseline_product))
        path=f'artifacts/epic16-tooling-fixture/16.1/binaries/{digest(old_bytes)}.dll';(repo/path).write_bytes(old_bytes)
        next(r for r in data['upstreamSurfaces'] if r['assemblyName']==baseline_product['assemblyName'])['baseline']=dict(path=path,sha256=digest(old_bytes),revision=previous)
    if change=='wrong-revision':row['candidate']['revision']='f'*40
    elif change=='wrong-owner':row['owner']='references/Hexalith.EventStore'
    elif change=='forged-digest':row['baseline']['sha256']=row['candidate']['sha256']='f'*64
    elif change=='missing-assembly':data['upstreamSurfaces'].pop()
    native(repo,scenario,data);result=evaluate(repo,scenario)
    assert result['result']==('PASS' if change=='addition' else 'FAIL'),result
    if change=='addition':
        proof=next(r for r in result['bindings']['upstreamApiPreservation'] if r['assemblyName']==product['assemblyName'])
        assert proof['baseline']['revision']==previous and proof['candidate']['revision']==candidate
        assert proof['baseline']['metadata']['api']!=proof['candidate']['metadata']['api']
    else:assert result['blockers'] and result['assertionLedger']


@pytest.mark.parametrize('shape',['return','parameter','parameter-name','nullable','property','generic','visibility','abstract-interface','abstract-class'])
def test_round9_native_api_comparison_preserves_supported_contracts(shape):
    product=dict(assemblyName='Synthetic.Api',surfaces=['Synthetic.Surface'])
    before='namespace Synthetic; public class Surface { public string Existing(string value) => value; public string Value { get; set; } = ""; public T Generic<T>(T value) where T : class => value; }'
    after=before
    if shape=='return':after=before.replace('string Existing(string value) => value','object Existing(string value) => value')
    elif shape=='parameter':after=before.replace('string Existing(string value) => value','string Existing(object value) => value.ToString()!')
    elif shape=='parameter-name':after=before.replace('string Existing(string value) => value','string Existing(string renamed) => renamed')
    elif shape=='nullable':after=before.replace('string Existing(string value)','string? Existing(string? value)')
    elif shape=='property':after=before.replace('get; set;', 'get;')
    elif shape=='generic':after=before.replace('where T : class', 'where T : class, new()')
    elif shape=='visibility':after=before.replace('public string Existing','protected string Existing')
    elif shape=='abstract-interface':
        before='namespace Synthetic; public interface Surface { string Existing(string value); }';after=before.replace('string Existing', 'void Added(); string Existing')
    else:
        before='namespace Synthetic; public abstract class Surface { public string Existing(string value) => value; }';after=before.replace('public string Existing', 'public abstract void Added(); public string Existing')
    b=tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,before),'a'*40)
    a=tooling.native_helper.inspect_product(round9_compile_product(product,'b'*40,after),'b'*40)
    with pytest.raises(tooling.Stop) as error:tooling.products.additive(b,a,tooling.need,shape)
    assert error.value.code=='UPSTREAM_API_DRIFT'


def test_round9_native_api_default_interface_member_addition_is_supported():
    product=dict(assemblyName='Synthetic.DefaultApi',surfaces=['Synthetic.Surface'])
    before='namespace Synthetic; public interface Surface { string Existing(string value); }'
    after=before.replace('string Existing', 'int Added() => 1; string Existing')
    b=tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,before),'a'*40)
    a=tooling.native_helper.inspect_product(round9_compile_product(product,'b'*40,after),'b'*40)
    tooling.products.additive(b,a,tooling.need,'default interface member')


@pytest.mark.parametrize('name',['Abstract','GetAbstractShape','AbstractProperty'])
def test_round9_concrete_addition_names_do_not_act_as_abstract_flags(name):
    product=dict(assemblyName='Synthetic.NamesApi',surfaces=['Synthetic.Surface'])
    before='namespace Synthetic; public class Surface { public int Existing() => 1; }'
    after=before.replace('public int Existing', 'public int '+name+'() => 2; public int Existing')
    b=tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,before),'a'*40)
    a=tooling.native_helper.inspect_product(round9_compile_product(product,'b'*40,after),'b'*40)
    tooling.products.additive(b,a,tooling.need,name)


@pytest.mark.parametrize('fault',['failed','retryable','indeterminate','missing','extra','foreign','unsafe-reason','state-content','scope','version','bool-status'])
def test_round9_http_success_requires_actual_safe_completed_projection_outcomes(repository,fault):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);ops=data['wireObservation']['operations'];body=ops[-1]['body'];outcome=body['outcomes'][0]
    if fault in ('failed','retryable','indeterminate'):outcome['status']={'failed':4,'retryable':2,'indeterminate':3}[fault]
    elif fault=='missing':body['outcomes']=[]
    elif fault=='extra':body['outcomes'].append(deepcopy(outcome))
    elif fault=='foreign':outcome['projectionType']='foreign'
    elif fault=='unsafe-reason':outcome['reasonCode']='secret-token'
    elif fault=='state-content':outcome['state']={'text':'private content'}
    elif fault=='scope':ops[0]['requestScope']['aggregateId']='foreign'
    elif fault=='version':body['version']=1
    else:outcome['status']=False
    round9_reject(repository,scenario,data)


def test_round9_already_completed_wire_outcome_can_clear_durable_pending_reference(repository):
    scenario='AC-16.3-04';data=deepcopy(OBSERVATIONS[scenario]);data['wireObservation']['operations'][-1]['body']['outcomes'][0]['status']=1
    native(repository,scenario,data);assert evaluate(repository,scenario)['result']=='PASS'


@pytest.mark.parametrize('story',tooling.measurements.STORIES)
def test_round9_missing_external_python_dependency_is_structured(repository,story):
    command=[sys.executable,'-S',str(repository/f'_bmad/scripts/verify_story_{story.replace(".","_")}.py'),'--repository',str(repository)]
    result=subprocess.run(command,capture_output=True,timeout=30)
    assert result.returncode==2 and b'Traceback' not in result.stderr
    report=json.loads(result.stdout);assert report['result']=='BLOCKED' and report['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE']


def test_round9_product_metadata_rejects_conflicting_optional_source_revision():
    product=dict(assemblyName='Synthetic.RevisionApi',surfaces=['Synthetic.Surface'])
    source='[assembly: System.Reflection.AssemblyMetadata("SourceRevisionId","'+('b'*40)+'")]\nnamespace Synthetic; public class Surface {}'
    with pytest.raises(ValueError):tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,source),'a'*40)


@pytest.mark.parametrize('function',['query_mapping','query_case'])
@pytest.mark.parametrize('field',['providerState','lifecycle'])
def test_round9_query_domain_semantics_reject_unknown_even_without_schema(function,field):
    row=deepcopy(OBSERVATIONS['AC-16.2-03']['queries'][1]);row[field]='UnsupportedValue'
    with pytest.raises(ValueError):getattr(tooling.measurements,function)(row)



def round9_referenced_product(directory, kind, revision, surface, *, added=False):
    external=directory/'external';api=directory/'api';external.mkdir(parents=True);api.mkdir()
    (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (external/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>External</AssemblyName></PropertyGroup></Project>')
    (external/'Value.cs').write_text('namespace External; public '+kind+' Value {}')
    (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Probe.Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo><Nullable>disable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" /></ItemGroup></Project>')
    (api/'Surface.cs').write_text('namespace Probe; public class Surface { '+surface+(' public int Added() => 1;' if added else '')+' }')
    (api/'Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
    argv=['dotnet','build','Api.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']
    result=subprocess.run(argv,cwd=api,capture_output=True,timeout=120)
    for name,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(directory/('build.'+name)).write_bytes(raw)
    (directory/'build-command.json').write_text(json.dumps(dict(argv=argv,cwd=str(api),exitCode=result.returncode),indent=2)+'\n')
    assert result.returncode==0,result.stdout.decode()
    binary=(api/'bin/Debug/net10.0/Probe.Api.dll').read_bytes();metadata=tooling.native_helper.inspect_product(binary,revision)
    (directory/'metadata.json').write_text(json.dumps(metadata,indent=2)+'\n')
    assert 'Probe.Surface' in metadata['declaredTypes'] and 'Probe.Surface' in metadata['api']['types']
    return metadata


@pytest.mark.parametrize('shape',[
    'public External.Value Get() => default;',
    'public void Consume(External.Value value) {}',
    'public External.Value Item { get; set; }',
    'public External.Value Item;',
    'public System.Collections.Generic.List<External.Value> Get() => default;',
    'public ref External.Value Echo(ref External.Value value) => ref value;',
])
def test_round9_referenced_class_value_signature_change_cannot_pass(tmp_path,shape):
    before=round9_referenced_product(tmp_path/'baseline','class','a'*40,shape)
    after=round9_referenced_product(tmp_path/'candidate','struct','b'*40,shape)
    assert before['api']!=after['api']
    with pytest.raises(tooling.Stop) as error:tooling.products.additive(before,after,tooling.need,'external class/value signature')
    assert error.value.code=='UPSTREAM_API_DRIFT'


@pytest.mark.parametrize('kind',['class','struct'])
def test_round9_referenced_same_kind_concrete_addition_remains_supported(tmp_path,kind):
    surface='public External.Value Echo(External.Value value) => value; public External.Value Item { get; set; } public class Nested {}'
    before=round9_referenced_product(tmp_path/'baseline',kind,'a'*40,surface)
    after=round9_referenced_product(tmp_path/'candidate',kind,'b'*40,surface,added=True)
    assert 'Probe.Surface+Nested' in after['declaredTypes']
    assert before['api']!=after['api']
    tooling.products.additive(before,after,tooling.need,'unchanged external kind with concrete addition')


@pytest.mark.parametrize('kind,marker',[('class','class'),('struct','valuetype')])
def test_round9_defined_signature_kind_keeps_declaration_lookup_plain(kind,marker):
    product=dict(assemblyName='Synthetic.Api',surfaces=['Synthetic.Surface'])
    source='namespace Synthetic; public class Container { public '+kind+' Value {} } public class Surface { public Container.Value Echo(Container.Value value) => value; }'
    metadata=tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,source),'a'*40)
    assert 'Synthetic.Surface' in metadata['declaredTypes'] and 'Synthetic.Container+Value' in metadata['declaredTypes']
    member=next(m for m in metadata['api']['types']['Synthetic.Surface']['members'] if m.startswith('method:Echo:'))
    assert member.count(marker+' Synthetic.Container+Value')==2


# These artifacts are deliberately reused on disk by native execution and API proof.
# Their equality does not depend on the session compiler cache or build path.
def round10_product_bytes(repo, product, revision):
    path = repo/'artifacts/product-builds'/revision/(product['assemblyName']+'.dll')
    if not path.exists():
        source = git(repo/product['owner'],'show',revision+':fixture-products/'+product['assemblyName']+'.cs')+'\n'
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_bytes(round9_compile_product(product,revision,source))
    return path.read_bytes()


def round10_compiled_contracts(repo, scenario, data, scope=SCOPE):
    story=scenario[3:7];candidate=git(repo,'rev-parse','HEAD')
    baseline=git(repo,'rev-parse','HEAD^') if scope==SCOPE else json.loads((repo/tooling.contract_path(story)).read_bytes())['nativeProtocol']['publicSourceBaseline']
    product=next(p for p in tooling.products.PRODUCTS if p['assemblyName']=='Hexalith.Conversations.Contracts')
    data['compiledContracts']={}
    for phase,revision in (('baseline',baseline),('candidate',candidate)):
        content=round10_product_bytes(repo,product,revision)
        path=f'artifacts/epic16-{scope}/{story}/binaries/{digest(content)}.dll'
        target=repo/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(content)
        data['compiledContracts'][phase]=dict(path=path,sha256=digest(content),revision=revision)


@pytest.mark.parametrize('child', ['TestMethod', 'Execution', 'both'])
def test_round10_trx_definition_children_are_exactly_one(repository, child):
    path=native(repository,'AC-16.1-01');tree=ET.fromstring(path.read_bytes());definition=tree.find('TestDefinitions/UnitTest')
    for name in ('TestMethod','Execution') if child=='both' else (child,):
        extra=deepcopy(definition.find(name));extra.set('id' if name=='Execution' else 'name','conflicting');definition.append(extra)
    path.write_bytes(ET.tostring(tree));rejected(evaluate(repository),'NATIVE_IDENTITY_DRIFT')


@pytest.mark.parametrize('defect', ['ImportError', 'ModuleNotFoundError', 'missing-declared', 'misspelled-local', 'available-declared'])
def test_round10_generator_classifies_only_missing_declared_dependencies(tmp_path, defect):
    shutil.copyfile(ROOT/'_bmad/scripts/generate_story_record.py',tmp_path/'generate_story_record.py')
    if defect!='missing-declared': (tmp_path/'epic16_tooling.py').write_text('raise '+defect+'("internal defect", name="unrelated_internal_dependency")\n' if defect=='ModuleNotFoundError' else 'raise ImportError("internal defect")\n')
    if defect in ('misspelled-local','available-declared'):
        name='epic16_misspelled' if defect=='misspelled-local' else 'epic16_capture'
        (tmp_path/'epic16_tooling.py').write_text('raise ModuleNotFoundError("internal defect", name='+repr(name)+')\n')
        if defect=='available-declared':(tmp_path/'epic16_capture.py').write_text('# available declared module\n')
    result=subprocess.run([sys.executable,str(tmp_path/'generate_story_record.py'),'--contract',tooling.contract_path('16.1')],capture_output=True)
    if defect=='missing-declared':
        assert result.returncode==2 and json.loads(result.stdout)['blockers']==['SCHEMA_UNAVAILABLE'] and not result.stderr
    else:
        assert result.returncode==1 and b'Traceback' in result.stderr and b'internal defect' in result.stderr
        assert b'SCHEMA_UNAVAILABLE' not in result.stdout


@pytest.mark.parametrize('version', ['1.0.0+', '1.0.0+existing.', '1.0.0+existing.more.'])
def test_round10_sdk_informational_version_forms_match_both_inspectors(tmp_path,version):
    binary=compile_evidence(tmp_path/'build','a'*40,'namespace Hexalith.Conversations.IntegrationTests.Epic16; public class Story16_1EvidenceTests { [Xunit.Fact] public void Scenario01() {} }')
    (tmp_path/'build/Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("'+version+'a'*40+'")]')
    result=subprocess.run(['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=tmp_path/'build',capture_output=True);assert result.returncode==0,result.stdout
    binary=(tmp_path/'build/bin/Debug/net10.0/Evidence.dll').read_bytes()
    assert tooling.native_helper.inspect_product(binary,'a'*40)['sourceRevision']=='a'*40
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')['sourceRevision']=='a'*40
    with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary,'b'*40,'Hexalith.Conversations.IntegrationTests.Epic16.Story16_1EvidenceTests','Scenario01')


@pytest.mark.parametrize('case', ['unauthorized','nonexistent'])
@pytest.mark.parametrize('fault', ['content','extra-member','page-count','cursor','action','version','state','reason','response-tenant','request-tenant','correlation','request-execution','response-execution','order','read','write','missing','auth-input','float-version'])
def test_round10_authorization_response_is_executed_correlated_and_nondisclosing(repository,case,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-03']);row=next(r for r in data['authorizationQueries'] if r['id']==case);response=row['response'];request=row['request'];value=response['result']
    if fault=='content':value['conversations']=[{'label':'synthetic cached secret'}]
    elif fault=='extra-member':value['cachedDetail']={'label':'synthetic cached secret'}
    elif fault=='page-count':value['page']['returnedCount']=1
    elif fault=='cursor':value['page']['continuationCursor']='synthetic cached cursor'
    elif fault=='action':value['safeNextAction']='synthetic cached secret'
    elif fault=='version':value['schemaVersion']=2
    elif fault=='float-version':value['schemaVersion']=1.0
    elif fault=='state':value['freshnessState']='Current'
    elif fault=='reason':value['reasonCode']='current'
    elif fault=='response-tenant':response['tenant']='other'
    elif fault=='request-tenant':request['query']['tenantId']='other'
    elif fault=='correlation':response['correlationId']='other'
    elif fault=='request-execution':request['executed']=False
    elif fault=='response-execution':response['executed']=False
    elif fault=='order':response['order']=request['order']
    elif fault=='read':row['indexReads']=['index']
    elif fault=='write':row['writes']=['repair']
    elif fault=='auth-input':row['tenantExists']=not row['tenantExists']
    else:data['authorizationQueries'].remove(row)
    native(repository,'AC-16.2-03',data);result=evaluate(repository,'AC-16.2-03')
    assert result['result']=='FAIL' and result['blockers'],result


def test_round10_authorization_response_positive_preserves_native_hidden_shape(repository):
    result=evaluate(repository,'AC-16.2-03');assert result['result']=='PASS',result
    values=[row['response']['result'] for row in OBSERVATIONS['AC-16.2-03']['authorizationQueries']]
    assert values[0]==values[1] and values[0]['conversations']==[] and values[0]['safeNextAction']=='No accessible matches.'


@pytest.fixture
def round10_runtime(repository,tmp_path,monkeypatch):
    """Every byte and approval here belongs to this disposable synthetic experiment."""
    repo=repository;story='16.1';warning=repo/tooling.measurements.execution.WARNING_PATH
    warning.write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
    git(repo,'add',str(warning.relative_to(repo)));git(repo,'commit','-qm','test: establish correlated runtime baseline')
    baseline=git(repo,'rev-parse','HEAD');contract_path=tooling.contract_path(story);contract=json.loads((repo/contract_path).read_bytes())
    contract['nativeProtocol']['publicSourceBaseline']=baseline;dump(repo,contract_path,contract)
    monkeypatch.setattr(tooling,'CONTRACT_PINS',dict(tooling.CONTRACT_PINS,**{contract_path:digest((repo/contract_path).read_bytes())}))
    source=contract['nativeProtocol']['runtimeTestSource'];target=repo/source;target.parent.mkdir(parents=True,exist_ok=True)
    code='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_1EvidenceTests { '+''.join('[Xunit.Fact] public void Scenario0'+str(n)+'() {}' for n in range(1,6))+' }'
    target.write_bytes(code.encode());spec=tooling.spec_path('runtime',story);(repo/spec).write_bytes((repo/'fixture-spec.md').read_bytes())
    warning.write_bytes(b'// Synthetic disposable candidate with qualified durable projection.\n')
    pred=dict(path='fixture-predecessor.json',sha256=digest((repo/'fixture-predecessor.json').read_bytes()))
    entry=dict(authorizationRevision=baseline,storyId=story,qualifications={key:'PASS' for key in tooling.ENTRY_REQUIREMENTS},executionAllowed=True,ownerApprovalClaimed=True,
               predecessors=[dict(identity=x,files=[pred]) for x in contract['preservedContract']['predecessors']])
    dump(repo,'fixture-entry.json',entry);pin=digest((repo/'fixture-entry.json').read_bytes());bind(repo)
    binding=json.loads((repo/tooling.BINDING).read_bytes());binding['entry']=dict(path='fixture-entry.json',sha256=pin);dump(repo,tooling.BINDING,binding)
    monkeypatch.setattr(tooling,'TRUSTED_ENTRIES',{'fixture-entry.json':pin})
    git(repo,'add','.');git(repo,'commit','-qm','test: bind correlated runtime product experiment')
    candidate=git(repo,'rev-parse','HEAD');binary=compile_evidence(tmp_path/'round10-native',candidate,code)
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}';path=native(repo,scenario);tree=ET.fromstring(path.read_bytes());out=tree.find('Results/UnitTestResult/Output/StdOut');payload=json.loads(out.text)
        payload['sources']=[dict(path=source,sha256=digest(target.read_bytes()))];out.text=json.dumps(payload)
        tree.find('TestDefinitions/UnitTest/TestMethod').set('codeBase','artifacts/round10-measurement.dll')
        runtime=repo/tooling.native_path('runtime',scenario);runtime.parent.mkdir(parents=True,exist_ok=True);runtime.write_bytes(ET.tostring(tree));retain_capture(repo,scenario,binary)
    return repo


def round10_rewrite_runtime(repo,scenario,mutate):
    path=repo/tooling.native_path('runtime',scenario);tree=ET.fromstring(path.read_bytes());output=tree.find('Results/UnitTestResult/Output/StdOut');payload=json.loads(output.text);mutate(payload);output.text=json.dumps(payload);path.write_bytes(ET.tostring(tree))
    manifest=json.loads((repo/tooling.capture.capture_path(scenario[3:7],scenario)).read_bytes());manifest['native']['sha256']=digest(path.read_bytes());dump(repo,tooling.capture.capture_path(scenario[3:7],scenario),manifest)


@pytest.mark.parametrize('which',['upstream','root'])
def test_round10_api_candidate_must_be_the_actual_loaded_product(round10_runtime,which):
    repo=round10_runtime;scenario='AC-16.1-05';initial=tooling.evaluate(repo,'16.1',scenario);assert initial['result']=='PASS',initial
    name='Hexalith.Tenants.Client' if which=='upstream' else 'Hexalith.Conversations.Contracts'
    product=next(p for p in tooling.products.PRODUCTS if p['assemblyName']==name);revision=next(p for p in initial['bindings']['testBinary']['products'] if p['assemblyName']==name)['sourceRevision']
    # A distinct implementation with the same declaration surface/revision still cannot stand in for the loaded bytes.
    content=round9_compile_product(product,revision,round9_product_source(product).replace('=> value;', '=> value + "different implementation";'))
    artifact=tooling.capture.binary_path('16.1',digest(content));(repo/artifact).write_bytes(content)
    def change(payload):
        api=next(r for r in payload['observations']['upstreamSurfaces'] if r['assemblyName']==name) if which=='upstream' else payload['observations']['compiledContracts']
        api['candidate']=dict(path=artifact,sha256=digest(content),revision=revision)
    round10_rewrite_runtime(repo,scenario,change)
    rejected(tooling.evaluate(repo,'16.1',scenario),'PRODUCT_API_CORRELATION_DRIFT')


@pytest.mark.parametrize('product_index',[0,2,7,8])
def test_round10_generator_rejects_different_owned_implementations_across_five_tests(round10_runtime,product_index):
    repo=round10_runtime;scenario='AC-16.1-04';name=tooling.products.PRODUCTS[product_index]['assemblyName'];product=tooling.products.PRODUCTS[product_index]
    path=repo/tooling.native_path('runtime',scenario);payload=json.loads(records.parse_trx(path.read_bytes())['results'][0]['output']);row=payload['loadedProducts'][product_index]
    content=round9_compile_product(product,row['sourceRevision'],round9_product_source(product).replace('=> value;', '=> value + "other implementation";'))
    artifact=tooling.capture.binary_path('16.1',digest(content));(repo/artifact).write_bytes(content);meta=tooling.native_helper.inspect_product(content,row['sourceRevision'])
    round10_rewrite_runtime(repo,scenario,lambda p:p['loadedProducts'][product_index].update(sha256=digest(content),mvid=meta['mvid']))
    cp=tooling.capture.capture_path('16.1',scenario);capture=json.loads((repo/cp).read_bytes());capture['products'][product_index].update(path=artifact,sha256=digest(content),mvid=meta['mvid']);dump(repo,cp,capture)
    for n in range(1,6):
        ac=f'AC-16.1-0{n}';result=tooling.evaluate(repo,'16.1',ac);assert result['result']=='PASS',result;dump(repo,tooling.receipt_path('runtime',ac),result)
    rejected(tooling.generate(repo,'16.1')[0],'PRODUCT_IMPLEMENTATION_DRIFT')


def test_round10_explicit_product_reuse_survives_empty_compiler_cache(round10_runtime):
    repo=round10_runtime;candidate=git(repo,'rev-parse','HEAD');before=round9_loaded_products(repo,candidate)
    _PRODUCT_BYTES.clear();after=round9_loaded_products(repo,candidate)
    assert before==after
    for n in range(1,6):
        scenario=f'AC-16.1-0{n}';result=tooling.evaluate(repo,'16.1',scenario);assert result['result']=='PASS',result;dump(repo,tooling.receipt_path('runtime',scenario),result)
    result=tooling.generate(repo,'16.1')[0];assert result['result']=='PASS',result
    compiled=result['scenarios'][4]['bindings']['compiledPublicPreservation'];retained={r['path'] for r in result['retainedNativeArtifacts']}
    assert all(compiled[p]['path'] in retained for p in ('baseline','candidate'))


@pytest.mark.parametrize('scenario',['AC-16.1-05','AC-16.2-05'])
@pytest.mark.parametrize('fault',['missing','revision','digest','removed-type','addition','unsupported'])
def test_round10_root_compiled_shape_is_required_in_addition_to_unchanged_sources(repository,scenario,fault):
    path=native(repository,scenario);tree=ET.fromstring(path.read_bytes());out=tree.find('Results/UnitTestResult/Output/StdOut');payload=json.loads(out.text);data=payload['observations'];row=data['compiledContracts']['candidate']
    if fault=='missing':del data['compiledContracts']
    elif fault=='revision':row['revision']='f'*40
    elif fault=='digest':row['sha256']='f'*64
    else:
        product=next(p for p in tooling.products.PRODUCTS if p['assemblyName']=='Hexalith.Conversations.Contracts')
        source=round9_product_source(product)
        if fault=='removed-type':source=source.replace('Existing(', 'Changed(')
        elif fault=='addition':source+='namespace Synthetic { public class Extra {} }'
        else:source=source.replace('public string Existing(string value) => value;', '[System.Runtime.InteropServices.DllImport("fixture-native")] public static extern int Unsupported();')
        binary=round9_compile_product(product,row['revision'],source);artifact=f'artifacts/epic16-tooling-fixture/{scenario[3:7]}/binaries/{digest(binary)}.dll';(repository/artifact).write_bytes(binary);row.update(path=artifact,sha256=digest(binary))
    out.text=json.dumps(payload);path.write_bytes(ET.tostring(tree));result=evaluate(repository,scenario)
    assert result['result']=='FAIL' and result['blockers'],result


@pytest.mark.parametrize('shape', ['plain','output','context','class','inherited-class','collection-name','collection-type','collection-generic','assembly','assembly-generic','fixture-hierarchy','default','optional','params','missing','missing-fixture-dependency','foreign-collection'])
def test_round10_construction_metadata_agrees_with_actual_pinned_runner(tmp_path,shape):
    """Execute only test-authored fixtures here; the evidence inspector never executes them."""
    support='';attributes='';declaration='';constructor='';assembly='';body=''
    if shape=='output':constructor='Xunit.ITestOutputHelper value';body='value.WriteLine("synthetic output");'
    elif shape=='context':constructor='Xunit.ITestContextAccessor value';body='System.ArgumentNullException.ThrowIfNull(value);'
    elif shape in ('class','inherited-class'):
        support='public class Fixture {}';declaration=' : Xunit.IClassFixture<Fixture>';constructor='Fixture value';body='System.ArgumentNullException.ThrowIfNull(value);'
        if shape=='inherited-class':support+='public class Base : Xunit.IClassFixture<Fixture> {}';declaration=' : Base'
    elif shape.startswith('collection') or shape=='foreign-collection':
        support='public class Fixture {} [Xunit.CollectionDefinition("named")] public class Group : Xunit.ICollectionFixture<Fixture> {}';constructor='Fixture value';body='System.ArgumentNullException.ThrowIfNull(value);'
        attributes={'collection-name':'[Xunit.Collection("named")]','collection-type':'[Xunit.Collection(typeof(Group))]','collection-generic':'[Xunit.Collection<Group>]','foreign-collection':'[Xunit.Collection("other")]'}[shape]
    elif shape in ('assembly','assembly-generic'):
        support='public class Fixture {}';constructor='Fixture value';body='System.ArgumentNullException.ThrowIfNull(value);'
        assembly='[assembly:Xunit.AssemblyFixture(typeof(Fixture))]' if shape=='assembly' else '[assembly:Xunit.AssemblyFixture<Fixture>]'
    elif shape=='fixture-hierarchy':
        support='public class AssemblyData {} public class CollectionData(AssemblyData value) {} public class ClassData(CollectionData value) {} [Xunit.CollectionDefinition("nested")] public class Group : Xunit.ICollectionFixture<CollectionData>, Xunit.IClassFixture<ClassData> {}'
        assembly='[assembly:Xunit.AssemblyFixture(typeof(AssemblyData))]';attributes='[Xunit.Collection("nested")]';constructor='ClassData value';body='System.ArgumentNullException.ThrowIfNull(value);'
    elif shape=='default':constructor='string value = "default"'
    elif shape=='optional':constructor='[System.Runtime.InteropServices.Optional] string value'
    elif shape=='params':constructor='params string[] values'
    elif shape=='missing':constructor='string unavailable'
    elif shape=='missing-fixture-dependency':support='public class Fixture(string missing) {}';declaration=' : Xunit.IClassFixture<Fixture>';constructor='Fixture value'
    directory=tmp_path/'runner';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
    code='using Synthetic;\n'+assembly+'\n[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]\nnamespace Synthetic;\n'+support+'\n'+attributes+' public class TestClass'+declaration+' { '+('public TestClass('+constructor+') { '+body+' }' if constructor else '')+' [Xunit.Fact] public void Scenario01() {} }'
    (directory/'Tests.cs').write_text(code)
    command=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(command,cwd=directory,capture_output=True,timeout=120);assert build.returncode==0,build.stdout
    binary=(directory/'bin/Debug/net10.0/Evidence.dll').read_bytes();trx=directory/'result.trx'
    runner=['dotnet',str(directory/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(trx)]
    run=subprocess.run(runner,capture_output=True,timeout=120)
    expected=shape not in ('missing','missing-fixture-dependency','foreign-collection')
    assert run.returncode==(0 if expected else 1),(shape,run.stdout,run.stderr)
    counts=records.parse_trx(trx.read_bytes())['recomputed'];assert counts['passed']==int(expected) and counts['failed']==int(not expected)
    if os.environ.get('EPIC16_R10_NATIVE_EVIDENCE'):
        retain=Path(os.environ['EPIC16_R10_NATIVE_EVIDENCE'])/('constructor-'+shape);retain.mkdir(parents=True,exist_ok=True)
        for name,data in [('Tests.cs',code.encode()),('Evidence.dll',binary),('result.trx',trx.read_bytes()),('build.stdout',build.stdout),('build.stderr',build.stderr),('run.stdout',run.stdout),('run.stderr',run.stderr)]: (retain/name).write_bytes(data)
        (retain/'commands.json').write_text(json.dumps(dict(build=command,run=runner,buildExit=build.returncode,runExit=run.returncode),indent=2)+'\n')
    if expected:assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    else:
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')


@pytest.mark.parametrize('change',['tighten-constraint','same-constraint-addition'])
def test_round10_constraint_attributes_preserve_native_nullable_callers(tmp_path,change):
    product=dict(assemblyName='Probe.Api',surfaces=['Probe.Surface'])
    baseline='#nullable enable\nnamespace Probe; public interface IFoo {} public class Surface { public T Existing<T>(T value) where T: IFoo? => value; }'
    candidate=baseline.replace('IFoo? =>', 'IFoo =>') if change=='tighten-constraint' else baseline.replace('public class Surface {', 'public class Surface { public int Added() => 1;')
    binaries=[round9_compile_product(product,'a'*40,source) for source in (baseline,candidate)]
    before,after=[tooling.native_helper.inspect_product(b,'a'*40) for b in binaries]
    results=[]
    for phase,binary in zip(('before','after'),binaries):
        directory=tmp_path/phase;directory.mkdir();(directory/'Probe.Api.dll').write_bytes(binary)
        (directory/'Consumer.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><Reference Include="Probe.Api"><HintPath>Probe.Api.dll</HintPath></Reference></ItemGroup></Project>')
        (directory/'Consumer.cs').write_text('using Probe; public class Caller { public IFoo? Invoke(IFoo? value) => new Surface().Existing<IFoo?>(value); }')
        command=['dotnet','build','Consumer.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];run=subprocess.run(command,cwd=directory,capture_output=True,timeout=120);results.append(run)
        if os.environ.get('EPIC16_R10_NATIVE_EVIDENCE'):
            retain=Path(os.environ['EPIC16_R10_NATIVE_EVIDENCE'])/('constraint-'+change)/phase;retain.mkdir(parents=True,exist_ok=True)
            for name in ('Probe.Api.dll','Consumer.csproj','Consumer.cs'):shutil.copyfile(directory/name,retain/name)
            (retain/'build.stdout').write_bytes(run.stdout);(retain/'build.stderr').write_bytes(run.stderr);(retain/'command.json').write_text(json.dumps(dict(command=command,exitCode=run.returncode),indent=2)+'\n')
    assert results[0].returncode==0,results[0].stdout
    if change=='tighten-constraint':
        assert results[1].returncode==1 and b'CS8631' in results[1].stdout
        with pytest.raises(tooling.Stop):tooling.products.additive(before,after,tooling.need,'nullable constraint')
    else:
        assert results[1].returncode==0,results[1].stdout
        tooling.products.additive(before,after,tooling.need,'same nullable constraint')


# Public signing keys from the retained synthetic parent proof. No private key or
# signing authority is involved: these disposable PublicSign identities differ.
_ROUND10_KEYS = (
    '0024000004800000940000000602000000240000525341310004000001000100dde40bcb9de6036779601242e44097fcd2800e17ccb145813de845b6403de3b71edbafe35eca13f5ffebb4dd03e79ff75149da59787bd7a6439f70b4d7f09cf0864585c58fe74aaa05abeb717e73acb44fb32d669ec3ef8357f56ee29a347fdcc9150d4d073dc0452bbd1076773fe1c66ebd229052821514faf6e638889174b9',
    '0024000004800000940000000602000000240000525341310004000001000100c5593f6305915bacb1f5ba56a9f1755d26c5ceccc80864e3433b8528a5ac152ab8d489491e81ff933b8db95caa6f0561205328cfe4700d66a8ede81773b9b415824540f4d24acfc4a9ee43fd80e0c108d2138af1550564ebc06dc9c4ba286a1ae53c3f20a856fee0ca6cd155fcf90a961e172e65604840b6be98781bcb1fb2d0')


@pytest.mark.parametrize('change',['key','version','culture','flags','same-identity-addition'])
def test_round10_external_signature_preserves_complete_assembly_identity(tmp_path,change):
    metadata=[]
    for phase in ('before','after'):
        directory=tmp_path/phase;external=directory/'external';api=directory/'api';external.mkdir(parents=True);api.mkdir()
        (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
        (external/'public.snk').write_bytes(bytes.fromhex(_ROUND10_KEYS[int(change=='key' and phase=='after')]))
        version='2.0.0.0' if change=='version' and phase=='after' else '1.0.0.0'
        culture='[assembly:System.Reflection.AssemblyCulture("fr-FR")]' if change=='culture' and phase=='after' else ''
        if change=='flags' and phase=='after':culture='[assembly:System.Reflection.AssemblyFlags(System.Reflection.AssemblyNameFlags.Retargetable)]'
        (external/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyVersion>'+version+'</AssemblyVersion><SignAssembly>true</SignAssembly><PublicSign>true</PublicSign><AssemblyOriginatorKeyFile>public.snk</AssemblyOriginatorKeyFile></PropertyGroup></Project>')
        (external/'Value.cs').write_text(culture+'namespace External { public class Value {} }')
        (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Probe.Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" /></ItemGroup></Project>')
        extra='public int Added() => 1;' if phase=='after' and change=='same-identity-addition' else ''
        (api/'Api.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] namespace Probe { public class Surface { public External.Value Echo(External.Value value) => value; '+extra+' } }')
        command=['dotnet','build','Api.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];run=subprocess.run(command,cwd=api,capture_output=True,timeout=120);assert run.returncode==0,run.stdout
        binary=(api/'bin/Debug/net10.0/Probe.Api.dll').read_bytes();metadata.append(tooling.native_helper.inspect_product(binary,'a'*40))
        if os.environ.get('EPIC16_R10_NATIVE_EVIDENCE'):
            retain=Path(os.environ['EPIC16_R10_NATIVE_EVIDENCE'])/('identity-'+change)/phase;retain.mkdir(parents=True,exist_ok=True)
            for source in (external/'External.csproj',external/'Value.cs',external/'public.snk',api/'Api.csproj',api/'Api.cs',api/'bin/Debug/net10.0/Probe.Api.dll',external/'bin/Debug/net10.0/External.dll'):shutil.copyfile(source,retain/source.name)
            (retain/'build.stdout').write_bytes(run.stdout);(retain/'build.stderr').write_bytes(run.stderr);(retain/'command.json').write_text(json.dumps(dict(command=command,exitCode=run.returncode),indent=2)+'\n')
    if change=='same-identity-addition':tooling.products.additive(*metadata,tooling.need,change)
    else:
        with pytest.raises(tooling.Stop):tooling.products.additive(*metadata,tooling.need,change)


@pytest.mark.parametrize('scenario',['AC-16.1-05','AC-16.2-05'])
@pytest.mark.parametrize('effect',['remove','generate'])
def test_round10_external_build_inputs_cannot_change_root_public_shape_behind_equal_sources(repository,tmp_path,scenario,effect):
    repo=repository;story=scenario[3:7];directory=repo/'src/Hexalith.Conversations.Contracts'
    (directory/'Contract.cs').write_text('namespace Hexalith.Conversations.Contracts.Projections; public class ConversationDetailProjectionV1 {}\n')
    (directory/'PreviouslyPublic.cs').write_text('namespace Hexalith.Conversations.Contracts; public class PreviouslyPublic {}\n')
    project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Hexalith.Conversations.Contracts</AssemblyName></PropertyGroup></Project>'
    (directory/'Contracts.csproj').write_text(project)
    targets=repo/'Directory.Build.targets';targets.write_text('<Project />\n')
    warning=repo/tooling.measurements.execution.WARNING_PATH;warning.write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
    git(repo,'add','.');git(repo,'commit','-qm','test: bind disposable compiled root baseline');baseline=git(repo,'rev-parse','HEAD')
    if effect=='remove':targets.write_text('<Project><ItemGroup><Compile Remove="PreviouslyPublic.cs" /></ItemGroup></Project>\n')
    else:targets.write_text('<Project><Target Name="GenerateFixtureContract" BeforeTargets="CoreCompile"><WriteLinesToFile File="$(IntermediateOutputPath)Generated.cs" Lines="namespace Synthetic { public class Generated {} }" Overwrite="true" /><ItemGroup><Compile Include="$(IntermediateOutputPath)Generated.cs" /></ItemGroup></Target></Project>\n')
    warning.write_bytes(b'// Synthetic disposable candidate with qualified durable projection.\n')
    git(repo,'add','.');git(repo,'commit','-qm','test: alter only external root compilation input');candidate=git(repo,'rev-parse','HEAD')
    for ac in tooling.measurements.SCENARIOS:native(repo,ac)
    data=deepcopy(OBSERVATIONS[scenario]);manifests=[];compiled={}
    for phase,revision in (('baseline',baseline),('candidate',candidate)):
        paths=git(repo,'ls-tree','-r','--name-only',revision,'--','src/Hexalith.Conversations.Contracts').splitlines()
        manifests.append([dict(path=p,sha256=digest(records.v2_committed_blob(repo,revision,p))) for p in paths])
        build=tmp_path/('external-build-'+phase);source=build/'src/Hexalith.Conversations.Contracts';source.mkdir(parents=True)
        (build/'global.json').write_bytes((ROOT/'global.json').read_bytes())
        (build/'Directory.Build.targets').write_bytes(records.v2_committed_blob(repo,revision,'Directory.Build.targets'))
        for path in paths:(build/path).write_bytes(records.v2_committed_blob(repo,revision,path))
        command=['dotnet','build','Contracts.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false','-p:SourceRevisionId='+revision]
        run=subprocess.run(command,cwd=source,capture_output=True,timeout=120);assert run.returncode==0,run.stdout
        binary=(source/'bin/Debug/net10.0/Hexalith.Conversations.Contracts.dll').read_bytes();retained=f'artifacts/epic16-tooling-fixture/{story}/binaries/{digest(binary)}.dll';(repo/retained).write_bytes(binary)
        compiled[phase]=dict(path=retained,sha256=digest(binary),revision=revision)
        if os.environ.get('EPIC16_R10_NATIVE_EVIDENCE'):
            out=Path(os.environ['EPIC16_R10_NATIVE_EVIDENCE'])/('root-build-'+story+'-'+effect)/phase;out.mkdir(parents=True,exist_ok=True)
            (out/'Contracts.dll').write_bytes(binary);(out/'Directory.Build.targets').write_bytes((build/'Directory.Build.targets').read_bytes())
            for path in paths:shutil.copyfile(build/path,out/Path(path).name)
            (out/'build.stdout').write_bytes(run.stdout);(out/'build.stderr').write_bytes(run.stderr);(out/'command.json').write_text(json.dumps(dict(command=command,exitCode=run.returncode,revision=revision),indent=2)+'\n')
    assert manifests[0]==manifests[1]
    data.update(compiledContracts=compiled,contractSourcesBefore=manifests[0],contractSourcesAfter=manifests[1],beforeContractSha256=digest(tooling.canonical(manifests[0])),afterContractSha256=digest(tooling.canonical(manifests[1])))
    native(repo,scenario,data);result=evaluate(repo,scenario);rejected(result,'ROOT_API_DRIFT')


def test_round10_runtime_admission_rejects_unprovided_constructor(qualified_runtime,tmp_path):
    repo=qualified_runtime;candidate=git(repo,'rev-parse','HEAD')
    code='namespace Hexalith.Conversations.IntegrationTests.Epic16; public sealed class Story16_2EvidenceTests(string unavailable) { [Xunit.Fact] public void Scenario02() {} }'
    binary=compile_evidence(tmp_path/'missing-provider',candidate,code)
    root=json.loads((repo/tooling.capture.capture_path('16.2','AC-16.2-02')).read_bytes())['originalRepositoryRoot']
    retain_capture(repo,'AC-16.2-02',binary,root=root)
    rejected(runtime_evaluate(repo),'NATIVE_BINARY_DRIFT')


@pytest.mark.parametrize('field,value',[('safeNextAction','synthetic hidden content'),('freshnessState','Current'),('reasonCode','current')])
def test_round10_equal_authorization_responses_still_must_be_native_hidden_results(repository,field,value):
    data=deepcopy(OBSERVATIONS['AC-16.2-03'])
    for row in data['authorizationQueries']:row['response']['result'][field]=value
    native(repository,'AC-16.2-03',data);rejected(evaluate(repository,'AC-16.2-03'),'MEASUREMENT_INVALID')


@pytest.mark.parametrize('fault',['bytes','hardlink','symlink'])
def test_round10_final_inventory_rechecks_compiled_baseline_bytes_and_aliases(round10_runtime,fault):
    repo=round10_runtime;rows=[tooling.evaluate(repo,'16.1',f'AC-16.1-0{n}') for n in range(1,6)]
    assert all(r['result']=='PASS' for r in rows),rows
    for row in rows:dump(repo,tooling.receipt_path('runtime',row['scenarioId']),row)
    with tooling.ContainedReader(repo) as reader:assert tooling.capture.inventory(reader,'16.1',rows,tooling.need)
    artifact=rows[4]['bindings']['compiledPublicPreservation']['baseline'];path=repo/artifact['path'];original=path.read_bytes()
    if fault=='bytes':path.write_bytes(b'not the admitted binary')
    else:
        other=repo/'artifacts/compiled-alias.dll';other.write_bytes(original);path.unlink()
        if fault=='hardlink':os.link(other,path)
        else:path.symlink_to(other)
    with tooling.ContainedReader(repo) as reader,pytest.raises((tooling.Stop,tooling.ReadError)):
        tooling.capture.inventory(reader,'16.1',rows,tooling.need)


@pytest.mark.parametrize('shape', ['inherited-name','inherited-generic','override-name','override-type','override-generic','mixed-name-generic','mixed-generic-name','mixed-generic-types','mixed-declared','mixed-shared-name-generic','mixed-shared-generic-name','mixed-shared-generic-types'])
def test_round10_inherited_collection_attributes_match_actual_runner(tmp_path,shape):
    """Actual reflection suppresses inherited attributes only by their concrete type."""
    choices={
        'inherited-name':('[Xunit.Collection("a")]','', 'A',True),
        'inherited-generic':('[Xunit.Collection<GroupA>]','', 'A',True),
        'override-name':('[Xunit.Collection("a")]','[Xunit.Collection("b")]', 'B',True),
        'override-type':('[Xunit.Collection("a")]','[Xunit.Collection(typeof(GroupB))]', 'B',True),
        'override-generic':('[Xunit.Collection<GroupA>]','[Xunit.Collection<GroupA>]', 'A',True),
        'mixed-name-generic':('[Xunit.Collection("a")]','[Xunit.Collection<GroupB>]', 'B',False),
        'mixed-generic-name':('[Xunit.Collection<GroupA>]','[Xunit.Collection("b")]', 'B',False),
        'mixed-generic-types':('[Xunit.Collection<GroupA>]','[Xunit.Collection<GroupB>]', 'B',False),
        'mixed-declared':('','[Xunit.Collection("a"), Xunit.Collection<GroupB>]', 'B',False),
        'mixed-shared-name-generic':('[Xunit.Collection("a")]','[Xunit.Collection<GroupB>]', 'A',False),
        'mixed-shared-generic-name':('[Xunit.Collection<GroupA>]','[Xunit.Collection("b")]', 'A',False),
        'mixed-shared-generic-types':('[Xunit.Collection<GroupA>]','[Xunit.Collection<GroupB>]', 'A',False),
    }
    inherited,declared,parameter,expected=choices[shape]
    directory=tmp_path/'runner';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
    code='[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]\nnamespace Synthetic;\npublic class A {} public class B {}\n[Xunit.CollectionDefinition("a")] public class GroupA : Xunit.ICollectionFixture<A> {}\n[Xunit.CollectionDefinition("b")] public class GroupB : Xunit.ICollectionFixture<B> {}\n'+inherited+' public class Base {}\n'+declared+' public class TestClass : Base { public TestClass('+parameter+' value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }\n'
    if shape.startswith('mixed-shared-'):code=code.replace('ICollectionFixture<B>', 'ICollectionFixture<A>')
    (directory/'Tests.cs').write_text(code)
    command=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(command,cwd=directory,capture_output=True,timeout=120);assert build.returncode==0,build.stdout
    binary=(directory/'bin/Debug/net10.0/Evidence.dll').read_bytes();trx=directory/'result.trx'
    runner=['dotnet',str(directory/'bin/Debug/net10.0/Evidence.dll'),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(trx)]
    timed_out=False
    with subprocess.Popen(runner,stdout=subprocess.PIPE,stderr=subprocess.PIPE) as process:
        try:stdout,stderr=process.communicate(timeout=120 if expected else 8)
        except subprocess.TimeoutExpired:
            timed_out=True;process.kill();stdout,stderr=process.communicate()
        exit_code=process.returncode
    if expected:
        assert not timed_out and exit_code==0,(shape,stdout,stderr)
        counts=records.parse_trx(trx.read_bytes())['recomputed'];assert counts['passed']==1 and counts['failed']==0
    else:
        assert b'More than one collection attribute was found on test class Synthetic.TestClass' in stdout,(shape,stdout,stderr)
        assert b'System.ArgumentException' in stdout and (timed_out or exit_code!=0)
    if os.environ.get('EPIC16_R10_NATIVE_EVIDENCE'):
        retain=Path(os.environ['EPIC16_R10_NATIVE_EVIDENCE'])/('inherited-collection-'+shape);retain.mkdir(parents=True,exist_ok=True)
        for name,data in [('Tests.cs',code.encode()),('Evidence.dll',binary),('build.stdout',build.stdout),('build.stderr',build.stderr),('run.stdout',stdout),('run.stderr',stderr)]: (retain/name).write_bytes(data)
        if trx.exists():(retain/'result.trx').write_bytes(trx.read_bytes())
        (retain/'commands.json').write_text(json.dumps(dict(build=command,run=runner,buildExit=build.returncode,runExit=exit_code,timedOut=timed_out,timeoutSeconds=120 if expected else 8,expectedRunnable=expected),indent=2)+'\n')
    if expected:assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    else:
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')


# Iteration 11: controlled declaration and execution proofs, never runtime authority.
ROUND11_METADATA = {
    'interface-nullability': ('namespace Probe { public interface IBase<T> {} public interface Api : IBase<string> {} }', 'namespace Probe { public interface IBase<T> {} public interface Api : IBase<string?> {} }'),
    'attribute-overload': tuple('namespace Probe { public class StampAttribute : System.Attribute { public StampAttribute(int value) {} public StampAttribute(uint value) {} } [Stamp('+value+')] public class Api {} }' for value in ('1','1u')),
    'preserve-sig': tuple('namespace Probe { [System.Runtime.InteropServices.ComImport][System.Runtime.InteropServices.Guid("F0000000-0000-0000-0000-000000000001")] public interface Api { '+attribute+' int Method(); } }' for attribute in ('','[System.Runtime.InteropServices.PreserveSig]')),
    'assembly-cls': tuple('[assembly:System.CLSCompliant('+value+')] namespace Probe { public class Api {} }' for value in ('true','false')),
    'module-cls': tuple('[module:System.CLSCompliant('+value+')] namespace Probe { public class Api {} }' for value in ('true','false')),
    'assembly-metadata': tuple('[assembly:System.Reflection.AssemblyMetadata("Contract", "'+value+'")] namespace Probe { public class Api {} }' for value in ('one','two')),
    'owning-flags': ('namespace Probe { public class Api {} }','[assembly:System.Reflection.AssemblyFlags(System.Reflection.AssemblyNameFlags.Retargetable)] namespace Probe { public class Api {} }'),
}


def round11_retain(name, artifacts, commands=None):
    if os.environ.get('EPIC16_R11_NATIVE_EVIDENCE'):
        directory=Path(os.environ['EPIC16_R11_NATIVE_EVIDENCE'])/name;directory.mkdir(parents=True,exist_ok=True)
        for path,data in artifacts.items():(directory/path).write_bytes(data)
        if commands is not None:(directory/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')


@pytest.mark.parametrize('change',ROUND11_METADATA)
@pytest.mark.parametrize('scenario',['AC-16.1-05','AC-16.2-05','upstream'])
def test_round11_native_contract_changes_reject_at_api_callers(repository,change,scenario):
    ac='AC-16.1-05' if scenario=='upstream' else scenario
    path=native(repository,ac);tree=ET.fromstring(path.read_bytes());output=tree.find('Results/UnitTestResult/Output/StdOut');payload=json.loads(output.text)
    if scenario=='upstream':
        product=tooling.products.PRODUCTS[2];row=payload['observations']['upstreamSurfaces'][0]
    else:product=tooling.products.PRODUCTS[1];row=payload['observations']['compiledContracts']
    for phase,source in zip(('baseline','candidate'),ROUND11_METADATA[change]):
        source=source+'\n'+round9_product_source(product)
        artifact=row[phase];binary=round9_compile_product(product,artifact['revision'],source)
        retained=f'artifacts/epic16-tooling-fixture/{ac[3:7]}/binaries/{digest(binary)}.dll'
        (repository/retained).write_bytes(binary);artifact.update(path=retained,sha256=digest(binary))
        round11_retain(change+'-'+scenario,{phase+'.cs':source.encode(),phase+'.dll.bytes':binary})
    output.text=json.dumps(payload);path.write_bytes(ET.tostring(tree))
    rejected(evaluate(repository,ac),'UPSTREAM_API_DRIFT' if scenario=='upstream' else 'ROOT_API_DRIFT')


@pytest.mark.parametrize('mode',['unchanged','addition','version','build-stamps'])
def test_round11_supported_declarations_preserve_revision_and_version_policy(mode):
    product=dict(assemblyName='Probe');before='namespace Probe { public class Api {} }';after=before
    if mode=='addition':after+='namespace Probe { public class Added {} }'
    elif mode=='version':before='[assembly:System.Reflection.AssemblyVersion("1.0.0.0")] '+before;after='[assembly:System.Reflection.AssemblyVersion("2.0.0.0")] '+after
    elif mode=='build-stamps':
        before='[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")][assembly:System.Reflection.AssemblyMetadata("SourceRevisionId","'+'a'*40+'")] '+before
        after='[assembly:System.Reflection.AssemblyFileVersion("2.0.0.0")][assembly:System.Reflection.AssemblyMetadata("SourceRevisionId","'+'b'*40+'")] '+after
    values=[]
    for phase,revision,source in [('baseline','a'*40,before),('candidate','b'*40,after)]:
        binary=round9_compile_product(product,revision,source);values.append(tooling.native_helper.inspect_product(binary,revision))
        round11_retain('positive-'+mode,{phase+'.cs':source.encode(),phase+'.dll.bytes':binary})
    tooling.products.additive(*values,tooling.need,mode)
    if mode!='addition':assert values[0]['api']==values[1]['api']


@pytest.mark.parametrize('index',range(4))
@pytest.mark.parametrize('fault',['positive','missing-evidence','missing-commit','commit-unexecuted','commit-failed','ack-unexecuted','transaction','record','delivery','persisted','observed','both-swapped','source','delivered','etag','write-count','early','equal-ticks'])
def test_round11_acknowledgement_derives_executed_durable_contents(repository,index,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['acknowledgements'][index];commit=row['durableCommit'];ack=row['acknowledgement'];proof=row['durableEvidence']
    if fault=='missing-evidence':del row['durableEvidence']
    elif fault=='missing-commit':row['durableCommit']=None
    elif fault=='commit-unexecuted':commit['executed']=False
    elif fault=='commit-failed':commit['succeeded']=False
    elif fault=='ack-unexecuted':ack['executed']=False
    elif fault=='transaction':ack['transactionId']='unrelated'
    elif fault=='record':ack['recordId']='unrelated'
    elif fault=='delivery':ack['deliveryId']='unrelated'
    elif fault=='persisted':commit['persisted']['projection']['name']='Wrong persisted content'
    elif fault=='observed':ack['observedRecord']['checkpoint']+=1
    elif fault=='both-swapped':
        for value in (proof['after'],commit['persisted'],ack['observedRecord']):value['projection']['name']='Equally wrong'
    elif fault=='source':proof['sourceRecord']['payloadSha256']='f'*64
    elif fault=='delivered':proof['delivered']=deepcopy(proof['sourceRecord'] if index!=1 else OBSERVATIONS['AC-16.1-04']['acknowledgements'][0]['durableEvidence']['delivered'])
    elif fault=='etag':proof['expectedEtag']='foreign'
    elif fault=='write-count':proof['writeCount']=1-proof['writeCount']
    elif fault=='early':ack['order']=commit['order']
    elif fault=='equal-ticks':ack['utcTicks']=commit['utcTicks']
    round8_submit(repository,'AC-16.1-04',data,passes=fault in ('positive','equal-ticks'))


@pytest.mark.parametrize('index',[4,5])
@pytest.mark.parametrize('fault',['ack','commit','evidence'])
def test_round11_terminal_failures_cannot_carry_durable_success(repository,index,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['acknowledgements'][index];source=data['acknowledgements'][0]
    field={'ack':'acknowledgement','commit':'durableCommit','evidence':'durableEvidence'}[fault];row[field]=deepcopy(source[field])
    round8_submit(repository,'AC-16.1-04',data)


@pytest.mark.parametrize('index',range(7))
@pytest.mark.parametrize('fault',['positive','missing','unexecuted','incomplete','identity','scope','order','expected','proposed','before','after','outcome','succeeded','transaction','clearance'])
def test_round11_recovery_operations_execute_complete_and_bind_exact_branch(repository,index,fault):
    data=deepcopy(OBSERVATIONS['AC-16.3-04']);p=data['concurrentRecovery'];row=p['duplicateRetry'] if index==6 else p['staleAttempts'][index];ex=row['execution'];request,attempt,completion=ex['operations']
    if fault=='missing':del row['execution']
    elif fault=='unexecuted':attempt['executed']=False
    elif fault=='incomplete':completion['completed']=False
    elif fault=='identity':completion['operationId']='unrelated'
    elif fault=='scope':ex['scope']['tenant']='foreign'
    elif fault=='order':completion['order']=attempt['order']
    elif fault=='expected':request['expectedSha256']='f'*64
    elif fault=='proposed':request['proposedSha256']='f'*64
    elif fault=='before':attempt['storedBeforeSha256']='f'*64
    elif fault=='after':completion['storedAfterSha256']='f'*64
    elif fault=='outcome':completion['outcome']='Completed'
    elif fault=='succeeded':attempt['succeeded']=True
    elif fault=='transaction':attempt['transactionId']='invented'
    elif fault=='clearance':ex['afterClearanceTransactionId']='unrelated'
    round8_submit(repository,'AC-16.3-04',data,passes=fault=='positive')


@pytest.mark.parametrize('story',['16_1','16_2','16_3'])
@pytest.mark.parametrize('defect',['absent','typo','internal','present-declared'])
def test_round11_launchers_classify_only_genuinely_missing_declared_modules(tmp_path,story,defect):
    wrapper='verify_story_'+story+'.py';shutil.copyfile(ROOT/'_bmad/scripts'/wrapper,tmp_path/wrapper)
    if defect=='typo':(tmp_path/'epic16_tooling.py').write_text('import epic16_typo\n')
    elif defect=='internal':(tmp_path/'epic16_tooling.py').write_text('raise ImportError("internal defect")\n')
    elif defect=='present-declared':(tmp_path/'epic16_tooling.py').write_text('raise ModuleNotFoundError("internal defect", name="epic16_tooling")\n')
    run=subprocess.run([sys.executable,str(tmp_path/wrapper)],cwd=tmp_path,capture_output=True)
    if defect=='absent':assert run.returncode==2 and json.loads(run.stdout)['blockers']==['TOOLING_DEPENDENCY_UNAVAILABLE']
    else:assert run.returncode==1 and b'Traceback' in run.stderr and not run.stdout


@pytest.mark.parametrize('fault',['positive','missing','extra','foreign','query-missing','query-foreign'])
def test_round11_multi_resource_erase_retains_dispatch_multiplicity_and_queries(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][0];relations=tooling.measurements.relationships
    added=json.loads(json.dumps(row['capturedObjects'][0]).replace('conversation-a','conversation-b'))
    added['key']=relations.conversation_key(row['tenant'],'conversation-b');added['storedMetadataSha256']=digest(added['storedMetadataBytes'].encode())
    row['capturedObjects'].append(added);row['deletionTargets'].append(dict(row['deletionTargets'][0],key=added['key']));row['deletedTargets']=deepcopy(row['deletionTargets'])
    round8_refresh(row)
    for stage in row['atomicity']['stages']:
        stage['cas']['componentTransactions']=[dict(kind=p['kind'],resource=p['resource'],transactionId=stage['cas']['transactionId']) for p in stage['after']['components']]
    entry=row['atomicity']['stages'][0]
    assert len(entry['after']['index']['dispatchReferences'])==2
    if fault in ('missing','extra','foreign'):
        for snapshot in (entry['before'],entry['after']):
            values=snapshot['index']['dispatchReferences']
            if fault=='missing':values.pop()
            elif fault=='extra':values.append(values[0])
            else:values[1]='foreign'
        entry['plan']['indexAfter']=deepcopy(entry['after']['index'])
    elif fault=='query-missing':row['queries'].pop(0)
    elif fault=='query-foreign':row['queries'][0]['snapshot']['components'][2]['resource']='foreign'
    round8_submit(repository,'AC-16.2-04',data,passes=fault=='positive')


@pytest.mark.parametrize('shape',['fact','task','value-task','theory-conflict','derived-conflict','indirect-conflict','ordinary-attribute'])
def test_round11_fact_discovery_agrees_with_actual_pinned_runner(tmp_path,shape):
    directory=tmp_path/'native';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
    attributes='[Xunit.Fact]';support='';body='public void Scenario01() {}'
    if shape=='theory-conflict':attributes+='[Xunit.Theory]'
    elif shape in ('derived-conflict','indirect-conflict'):
        support='public class OtherFact : Xunit.FactAttribute {} public class IndirectFact : OtherFact {}'
        attributes+='[OtherFact]' if shape=='derived-conflict' else '[IndirectFact]'
    elif shape=='ordinary-attribute':support='public class Ordinary : System.Attribute {}';attributes+='[Ordinary]'
    elif shape=='task':body='public System.Threading.Tasks.Task Scenario01() => System.Threading.Tasks.Task.CompletedTask;'
    elif shape=='value-task':body='public System.Threading.Tasks.ValueTask Scenario01() => System.Threading.Tasks.ValueTask.CompletedTask;'
    source='[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] namespace Synthetic; '+support+' public class TestClass { '+attributes+' '+body+' }'
    (directory/'Tests.cs').write_text(source)
    build_args=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']+(['-p:RunAnalyzers=false'] if shape.endswith('conflict') else []);build=subprocess.run(build_args,cwd=directory,capture_output=True,timeout=120);assert build.returncode==0,build.stdout
    binary=directory/'bin/Debug/net10.0/Evidence.dll';trx=directory/'result.trx'
    run_args=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(trx)];run=subprocess.run(run_args,cwd=directory,capture_output=True,timeout=30)
    positive=not shape.endswith('conflict');assert run.returncode==(0 if positive else 1),(run.stdout,run.stderr)
    if not positive:assert b'multiple [Fact]-derived attributes' in run.stdout+run.stderr
    round11_retain('discovery-'+shape,{'Tests.cs':source.encode(),'Evidence.csproj':(directory/'Evidence.csproj').read_bytes(),'Evidence.dll.bytes':binary.read_bytes(),'result.trx':trx.read_bytes(),'build.stdout':build.stdout,'build.stderr':build.stderr,'run.stdout':run.stdout,'run.stderr':run.stderr},dict(build=build_args,run=run_args,buildExit=build.returncode,runExit=run.returncode))
    if positive:assert tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    else:
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')


def round11_refresh_recovery(data):
    """Rebind operation observations after a deliberately valid source/state variation."""
    from evidence_boundary_current import canonical
    p=data['concurrentRecovery']
    for row in [*p['staleAttempts'],p['duplicateRetry']]:
        duplicate='id' not in row;before=row['before'] if duplicate else row['storedBefore'];after=row['after'] if duplicate else row['storedAfter']
        expected={k:before[k] for k in tooling.measurements.schedules.FENCE_FIELDS} if duplicate else row['expected'];proposed=before if duplicate else row['proposed']
        request,attempt,completion=row['execution']['operations'];request.update(expectedSha256=digest(canonical(expected)),proposedSha256=digest(canonical(proposed)));attempt['storedBeforeSha256']=digest(canonical(before));completion['storedAfterSha256']=digest(canonical(after))


@pytest.mark.parametrize('index',range(4))
@pytest.mark.parametrize('mode',['extra','alternate','empty','reserved','unknown-role','role-invalid','configuration-invalid'])
def test_round11_durable_native_maps_preserve_opaque_keys_and_validate_values(repository,index,mode):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['acknowledgements'][index];p=row['durableEvidence']
    members={'actor-extra':'TenantReader'};configuration={'customer.setting':'kept'}
    if mode=='extra':members['actor-a']='TenantContributor';configuration['conversations.enabled']='true'
    elif mode=='empty':members={};configuration={}
    elif mode=='reserved':members={'version':'TenantReader','allowed':'TenantOwner'};configuration={'version':'customer-setting','history':'','allowed':' '}
    elif mode=='unknown-role':members={'actor-extra':'Unknown'}
    elif mode=='role-invalid':members={'actor-extra':'Reader'}
    elif mode=='configuration-invalid':configuration={'customer.setting':True}
    for state in (p['sourceState'],p['before']['projection'],p['after']['projection'],row['durableCommit']['persisted']['projection'],row['acknowledgement']['observedRecord']['projection']):state.update(members=deepcopy(members),configuration=deepcopy(configuration))
    round8_submit(repository,'AC-16.1-04',data,passes=mode not in ('role-invalid','configuration-invalid'))


@pytest.mark.parametrize('control',['hidden-fact','foreign-build-attribute','external-constructor'])
def test_round11_framework_ownership_and_external_attribute_constructors_are_verified(tmp_path,control):
    """Only these test-authored assemblies execute under the real pinned runner."""
    directory=tmp_path/'attributes';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    source='namespace System.Custom { public class HiddenFactAttribute : Xunit.FactAttribute {} } namespace System.Reflection { public class AssemblyFileVersionAttribute : System.Attribute { public AssemblyFileVersionAttribute(string value) {} } } namespace Custom { public class StampAttribute : System.Attribute { public StampAttribute(int value) {} public StampAttribute(uint value) {} } }'
    project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Attributes</AssemblyName></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3.extensibility.core" Version="4.0.2" /></ItemGroup></Project>'
    (directory/'Attributes.cs').write_text(source);(directory/'Attributes.csproj').write_text(project)
    command=['dotnet','build','Attributes.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(command,cwd=directory,capture_output=True,timeout=120);assert build.returncode==0,build.stdout
    external=directory/'bin/Debug/net10.0/Attributes.dll';round11_retain('external-'+control,{'Attributes.cs':source.encode(),'Attributes.csproj':project.encode(),'Attributes.dll.bytes':external.read_bytes(),'attributes.stdout':build.stdout,'attributes.stderr':build.stderr},dict(argv=command,cwd=str(directory),exitCode=build.returncode))
    metadata=[]
    for phase,value in [('baseline','one' if control=='foreign-build-attribute' else '1'),('candidate','two' if control=='foreign-build-attribute' else '1u')]:
        work=tmp_path/phase;work.mkdir();(work/'global.json').write_bytes((ROOT/'global.json').read_bytes())
        is_test=control=='hidden-fact';name='Evidence' if is_test else 'Api'
        project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo><OutputType>'+('Exe' if is_test else 'Library')+'</OutputType></PropertyGroup><ItemGroup><Reference Include="Attributes"><HintPath>'+str(external)+'</HintPath><Aliases>custom</Aliases></Reference>'+('<PackageReference Include="xunit.v3" Version="4.0.2" />' if is_test else '')+'</ItemGroup></Project>'
        code='extern alias custom; [assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")] '
        if is_test:code+='namespace Synthetic; public class TestClass { [Xunit.Fact, custom::System.Custom.HiddenFact] public void Scenario01() {} }'
        elif control=='foreign-build-attribute':code+='[assembly:custom::System.Reflection.AssemblyFileVersion("'+value+'")] namespace Synthetic; public class Api {}'
        else:code+='namespace Synthetic; [custom::Custom.Stamp('+value+')] public class Api {}'
        (work/(name+'.csproj')).write_text(project);(work/'Code.cs').write_text(code)
        command=['dotnet','build',name+'.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']+(['-p:RunAnalyzers=false'] if is_test else []);build=subprocess.run(command,cwd=work,capture_output=True,timeout=120);assert build.returncode==0,build.stdout
        binary=work/f'bin/Debug/net10.0/{name}.dll';artifacts={'Code.cs':code.encode(),name+'.csproj':project.encode(),name+'.dll.bytes':binary.read_bytes(),'build.stdout':build.stdout,'build.stderr':build.stderr};commands=dict(build=command,cwd=str(work),buildExit=build.returncode)
        if is_test:
            trx=work/'result.trx';argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(trx)];run=subprocess.run(argv,cwd=work,capture_output=True,timeout=30);assert run.returncode==1 and b'multiple [Fact]-derived attributes' in run.stdout+run.stderr
            artifacts.update({'result.trx':trx.read_bytes(),'run.stdout':run.stdout,'run.stderr':run.stderr});commands.update(run=argv,runExit=run.returncode)
            with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')
        else:metadata.append(tooling.native_helper.inspect_product(binary.read_bytes(),'a'*40))
        round11_retain('external-'+control+'-'+phase,artifacts,commands)
        if is_test:break
    if not is_test:
        with pytest.raises(tooling.Stop) as error:tooling.products.additive(*metadata,tooling.need,control)
        assert error.value.code=='UPSTREAM_API_DRIFT' and metadata[0]['api']!=metadata[1]['api']


def round12_set(value, path, replacement):
    for key in path[:-1]:value=value[key]
    if replacement=='__remove__':value.pop(path[-1])
    else:value[path[-1]]=deepcopy(replacement)


ROUND12_REVERSE = [
 ('missing',('execution',),'__remove__'),
 ('unexecuted',('execution','saves',0,'executed'),False),
 ('incomplete',('execution','saves',1,'completed'),False),
 ('cas-unexecuted',('execution','saves',1,'casExecuted'),False),
 ('cas-order',('execution','saves',1,'casOrder'),99),
 ('scope',('execution','saves',1,'scope','tenant'),'foreign'),
 ('replica',('execution','saves',1,'replicaId'),'replica-b'),
 ('process',('execution','saves',1,'processId'),'process-replica-b'),
 ('request',('execution','saves',1,'requestId'),'unrelated'),
 ('expectation',('execution','saves',1,'expected','version'),21),
 ('proposal',('execution','saves',1,'proposed','projection','name'),'stale invention'),
 ('newer-result',('execution','saves',0,'succeeded'),False),
 ('stale-result',('execution','saves',1,'succeeded'),True),
 ('transaction',('execution','saves',1,'transactionId'),'unexpected'),
 ('before',('execution','saves',1,'storedBefore','checkpoint'),10),
 ('after',('execution','saves',1,'storedAfter','checkpoint'),11),
 ('read-unexecuted',('execution','consumers',0,'storeRead','executed'),False),
 ('response-incomplete',('execution','consumers',1,'response','completed'),False),
 ('read-scope',('execution','consumers',0,'storeRead','scope','tenant'),'foreign'),
 ('read-key',('execution','consumers',0,'storeRead','logicalKey'),'foreign'),
 ('read-state',('execution','consumers',0,'storeRead','snapshot','projection','name'),'wrong'),
 ('read-transaction',('execution','consumers',1,'storeRead','observedTransactionId'),'unrelated'),
 ('response',('execution','consumers',0,'response','decision','allowed'),True),
 ('read-early',('execution','consumers',0,'order'),1),
 ('repair',('execution','consumers',0,'writes'),['repair']),
]
@pytest.mark.parametrize('index',range(4))
@pytest.mark.parametrize('fault,path,value',[('positive',(),None),*ROUND12_REVERSE],ids=lambda x:x if isinstance(x,str) else None)
def test_round12_reverse_save_executes_actual_shared_cas_and_both_consumers(repository,index,fault,path,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-03'])
    if path:round12_set(data['reverseSaves'][index],path,value)
    round8_submit(repository,'AC-16.1-03',data,passes=fault=='positive')


ROUND12_ACCESS = [
 ('missing',('capture',),'__remove__'),('request-unexecuted',('capture','request','executed'),False),
 ('read-unexecuted',('capture','storeRead','executed'),False),('response-incomplete',('capture','response','completed'),False),
 ('scope',('capture','storeRead','scope','tenant'),'foreign'),('principal',('capture','request','scope','principal'),'unrelated'),
 ('process',('capture','response','scope','processId'),'unrelated'),('request',('capture','response','scope','requestId'),'unrelated'),
 ('record',('capture','scope','recordId'),'foreign'),('read-key',('capture','storeRead','logicalKey'),'foreign'),
 ('order',('capture','response','order'),3),('read-correlation',('capture','response','observedReadId'),'unrelated'),
 ('response',('capture','response','decision','tenantId'),'foreign'),('threshold',('capture','control','maxAgeTicks'),999999),
 ('allowed',('allowed',),True),('sequence',('lastContiguousSequence',),99),('repair',('capture','writes'),['repair'])]
@pytest.mark.parametrize('scenario',['AC-16.1-01','AC-16.1-02'])
@pytest.mark.parametrize('index',range(6))
@pytest.mark.parametrize('fault,path,value',[('positive',(),None),*ROUND12_ACCESS])
def test_round12_six_authorization_states_bind_executed_native_read_and_response(repository,scenario,index,fault,path,value):
    data=deepcopy(OBSERVATIONS[scenario]);row=data['authorization'][index]
    if fault=='allowed':value=not row['allowed']
    if path:round12_set(row,path,value)
    round8_submit(repository,scenario,data,passes=fault=='positive')


@pytest.mark.parametrize('case',['source','projection','freshness','float-freshness','future-clock','health','stale-current','gap-missing','gap-tenant','conflict-missing','conflict-identical','unknown-snapshot','unknown-metadata','unavailable-snapshot','unavailable-error'])
def test_round12_authorization_state_derives_from_actual_inputs(repository,case):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);index=2 if case=='stale-current' else 3 if case.startswith('gap') else 4 if case.startswith('conflict') else 1 if case.startswith('unknown') else 5 if case.startswith('unavailable') else 0
    row=data['authorization'][index];p=row['capture'];snapshot=p['storeRead']['snapshot']
    if case=='source':p['sourceRecord']['payloadSha256']='f'*64
    elif case=='projection':snapshot['projection']['name']='wrong'
    elif case=='freshness':snapshot['freshness']['lagTicks']=0
    elif case=='float-freshness':snapshot['freshness']['generatedUtcTicks']=1100.0
    elif case=='future-clock':p['observedAtUtcTicks']=1099
    elif case=='health':snapshot['health']='Unknown'
    elif case=='stale-current':p['observedAtUtcTicks']=1100
    elif case=='gap-missing':snapshot['pending']=[]
    elif case=='gap-tenant':snapshot['pending'][0]['persistedPayload']=snapshot['pending'][0]['persistedPayload'].replace('tenant-a','foreign');snapshot['pending'][0]['payloadSha256']=digest(snapshot['pending'][0]['persistedPayload'].encode())
    elif case=='conflict-missing':p['conflict']=None
    elif case=='conflict-identical':p['conflict']=deepcopy(p['sourceRecord'])
    elif case.endswith('snapshot'):p['storeRead']['snapshot']=deepcopy(data['authorization'][0]['capture']['storeRead']['snapshot'])
    elif case=='unknown-metadata':row['lastContiguousSequence']=10;p['response']['decision']['projectionVersion']=10
    elif case=='unavailable-error':p['storeRead']['error']=None
    round8_submit(repository,'AC-16.1-01',data)


@pytest.mark.parametrize('mode',['zero-lag','at-threshold','nullable','maps','reader-read','reader-write','missing-member','disabled','configuration-empty','unmapped-role','unsafe-member'])
def test_round12_authorization_native_values_and_control_boundaries(repository,mode):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);row=data['authorization'][0];p=row['capture'];snapshot=p['storeRead']['snapshot'];decision=p['response']['decision']
    if mode=='zero-lag':snapshot['freshness']['lastAppliedUtcTicks']=1100;snapshot['freshness']['lagTicks']=0
    elif mode=='at-threshold':p['observedAtUtcTicks']=1200
    else:
        if mode=='nullable':p['sourceState']['description']=None
        if mode=='maps':p['sourceState']['members']['customer-member']='TenantReader';p['sourceState']['configuration']['version']='customer-setting'
        if mode=='configuration-empty':p['sourceState']['configuration']={}
        if mode=='unmapped-role':p['sourceState']['members']['actor-a']='Unknown'
        if mode=='unsafe-member':p['sourceState']['members'][' user ']='TenantReader'
        if mode.startswith('reader'):p['sourceState']['members']['actor-a']='TenantReader'
        if mode=='reader-read':p['request']['requirement']=decision['requirement']='Read'
        if mode=='missing-member':p['sourceState']['members']={}
        if mode=='disabled':p['sourceState']['status']='Disabled'
        snapshot['projection']=tooling.measurements.domain.tenant_replay(p['sourceState'],[p['sourceRecord']],9,10)[0]
        if mode in ('reader-write','missing-member','disabled','unmapped-role','unsafe-member'):
            row['allowed']=decision['isAllowed']=False;decision['denialReason']={'reader-write':'InsufficientRole','missing-member':'MissingMember','disabled':'TenantDisabled','unmapped-role':'InsufficientRole','unsafe-member':'TenantProjectionPoisoned'}[mode];decision['projectionVersion']=decision['projectionWatermark']=None
    round8_submit(repository,'AC-16.1-01',data,passes=True)


ROUND12_FAILURE=[
 ('missing',(),None),('processing-unexecuted',('processing','executed'),False),('attempt-unexecuted',('attempts',0,'executed'),False),
 ('attempt-incomplete',('attempts',0,'completed'),False),('completion-incomplete',('completion','completed'),False),
 ('delivery',('scope','deliveryId'),'foreign'),('attempt-scope',('attempts',0,'scope','tenant'),'foreign'),
 ('attempt-count',('attempts',),'one'),('attempt-order',('attempts',0,'order'),1),('number',('attempts',0,'number'),2),
 ('expected',('attempts',0,'expectedEtag'),'foreign'),('proposed',('attempts',0,'proposed','checkpoint'),99),
 ('success',('attempts',0,'succeeded'),True),('transaction',('attempts',0,'transactionId'),'invented'),
 ('result',('attempts',0,'result'),'Completed'),('outcome',('completion','outcome'),'Healthy'),
 ('attempt-reference',('completion','attemptIds'),['foreign']),('commits',('completion','commits'),['invented']),
 ('ack-unexecuted',('acknowledgements','executed'),False),('ack-reference',('acknowledgements','processingCompletionId'),'foreign'),
 ('ack-present',('acknowledgements','deliveries'),['ack']),('restoration-unexecuted',('restoration','executed'),False),
 ('restoration',('restoration','provider'),'Unavailable'),('after',('afterRead','snapshot','checkpoint'),11),
 ('after-unexecuted',('afterRead','executed'),False),('fault',('faultDigests','injectedBytes'),'f'*64),
 ('source',('sourceRecord','payloadSha256'),'f'*64),('delivery-content',('delivered','payloadSha256'),'f'*64)]
@pytest.mark.parametrize('index',[4,5])
@pytest.mark.parametrize('fault,path,value',[('positive',(),None),*ROUND12_FAILURE])
def test_round12_failed_deliveries_execute_bounded_attempts_and_no_ack(repository,index,fault,path,value):
    data=deepcopy(OBSERVATIONS['AC-16.1-04']);row=data['acknowledgements'][index];p=row['failureEvidence']
    if fault=='missing':row['failureEvidence']=None
    elif fault=='attempt-count':
        p['attempts']=p['attempts'][:-1];p['completion']['attemptIds']=[a['operationId'] for a in p['attempts']]
    elif path:round12_set(p,path,value)
    round8_submit(repository,'AC-16.1-04',data,passes=fault=='positive')


@pytest.mark.parametrize('index',range(3))
def test_round12_enqueue_transaction_cannot_reuse_lifecycle_commit(repository,index):
    data=deepcopy(OBSERVATIONS['AC-16.2-04']);row=data['lifecycle'][1];delivery=next(d for d in row['catchUp']['deliveries'] if d['enqueue']['transactionId'] is not None);transaction=row['atomicity']['stages'][index]['cas']['transactionId'];enqueue=delivery['enqueue']
    enqueue['transactionId']=enqueue['indexTransactionId']=transaction
    for part in enqueue['componentTransactions']:part['transactionId']=transaction
    delivery['operations']['commit']['transactionId']=delivery['operations']['acknowledgement']['transactionId']=transaction
    round8_submit(repository,'AC-16.2-04',data)


@pytest.mark.parametrize('stage',['sdk','build'])
@pytest.mark.parametrize('stream',['stderr','stdout','both','oversized'])
def test_round12_helper_prerequisite_failure_retains_bounded_streams_and_exit(monkeypatch,stage,stream):
    native=tooling.native_helper;native._helper.cache_clear();code=41 if stage=='sdk' else 42
    def run(argv,**kwargs):
        if argv==['dotnet','--version'] and stage=='build':return subprocess.CompletedProcess(argv,0,b'10.0.402\n',b'')
        stdout=b'OUT-END' if stream in ('stdout','both') else b'X'*9000+b'OUT-END' if stream=='oversized' else b''
        stderr=b'ERR-END' if stream in ('stderr','both') else b'Y'*9000+b'ERR-END' if stream=='oversized' else b''
        return subprocess.CompletedProcess(argv,code,stdout,stderr)
    monkeypatch.setattr(native.subprocess,'run',run)
    with pytest.raises(native.HelperUnavailable) as error:native._helper()
    message=str(error.value);assert 'exit='+str(code) in message and 'stdout=' in message and 'stderr=' in message
    if stream in ('stdout','both','oversized'):assert 'OUT-END' in message
    if stream in ('stderr','both','oversized'):assert 'ERR-END' in message
    assert len(message)<4500
    native._helper.cache_clear()


def round12_refresh_reverse_states(data):
    """Rebind newly required native snapshots after an existing valid event variation."""
    for row in data['reverseSaves']:
        checkpoint=data['checkpoint'];horizon=data['recovery']['horizon'];final,history=tooling.measurements.domain.tenant_replay(data['startingState'],row['authoritativeReplay'],checkpoint,horizon)
        proof=row['execution'];newer,older=proof['saves']
        for save,projection in ((newer,final),(older,history[checkpoint+1])):save['proposed']['projection']=deepcopy(projection)
        newer['storedAfter']=deepcopy(newer['proposed']);older['storedBefore']=deepcopy(newer['storedAfter']);older['storedAfter']=deepcopy(newer['storedAfter'])
        for consumer in proof['consumers']:consumer['storeRead']['snapshot']=deepcopy(newer['storedAfter'])


def round12_retain_native(name,directory,commands):
    if not os.environ.get('EPIC16_R12_NATIVE_EVIDENCE'):return
    out=Path(os.environ['EPIC16_R12_NATIVE_EVIDENCE'])/name;out.mkdir(parents=True,exist_ok=True)
    for path in directory.rglob('*'):
        if not path.is_file() or 'obj' in path.parts:continue
        if 'bin' in path.parts and path.name not in ('Evidence.dll','External.dll','System.Threading.Tasks.dll','Evidence.deps.json','Evidence.runtimeconfig.json','Probe.dll','Probe.deps.json','Probe.runtimeconfig.json'):continue
        dest=out/path.relative_to(directory)
        if dest.suffix=='.dll':dest=dest.with_suffix('.dll.bytes')
        dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(path.read_bytes())
    (out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')


@pytest.mark.parametrize('shape',['genuine','metadata-version','local-stamp','external-stamp','external-metadata','custom-task','custom-params','genuine-params'])
def test_round12_native_framework_identity_matches_supported_source_return_and_params(tmp_path,shape):
    directory=tmp_path/'native';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes());external=directory/'external';external.mkdir();probe=directory/'probe';probe.mkdir();commands=[]
    source='';external_source='';assembly='External';prefix='';stamp='[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]'
    body='public void Scenario01() {}';constructor=''
    if shape=='local-stamp':source='namespace System.Reflection { public class AssemblyInformationalVersionAttribute(string value):System.Attribute {} }'
    elif shape=='external-stamp':external_source='namespace System.Reflection; public class AssemblyInformationalVersionAttribute(string value):System.Attribute {}';prefix='extern alias custom;';stamp='[assembly:custom::System.Reflection.AssemblyInformationalVersion("1.0.0+'+'a'*40+'")]'
    elif shape=='external-metadata':external_source='namespace System.Reflection; public class AssemblyMetadataAttribute(string key,string value):System.Attribute {}';prefix='extern alias custom;';stamp+='[assembly:custom::System.Reflection.AssemblyMetadata("SourceRevisionId","'+'a'*40+'")]'
    elif shape=='metadata-version':stamp='[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+existing.'+'a'*40+'")][assembly:System.Reflection.AssemblyMetadata("SourceRevisionId","'+'a'*40+'")]'
    elif shape=='custom-task':external_source='namespace System.Threading.Tasks; public class Task {}';assembly='System.Threading.Tasks';prefix='extern alias custom;';body='public custom::System.Threading.Tasks.Task Scenario01() => null;'
    elif shape=='custom-params':external_source='namespace System; [System.AttributeUsage(System.AttributeTargets.Parameter)] public class OtherArrayAttribute:System.Attribute {}';prefix='extern alias custom;';constructor='public TestClass([custom::System.OtherArray] string[] values) {}'
    elif shape=='genuine-params':constructor='public TestClass(params string[] values) {}'
    project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo><AssemblyName>'+assembly+'</AssemblyName></PropertyGroup></Project>'
    (external/'External.csproj').write_text(project);(external/'Attribute.cs').write_text(external_source or 'namespace Synthetic; public class Unused {}')
    reference='<ProjectReference Include="../external/External.csproj" Aliases="custom" />' if external_source else ''
    (probe/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo><RunAnalyzers>false</RunAnalyzers></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" />'+reference+'</ItemGroup></Project>')
    (probe/'Tests.cs').write_text(prefix+stamp+source+' namespace Synthetic { public class TestClass { '+constructor+' [Xunit.Fact] '+body+' } }')
    argv=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(argv,cwd=probe,capture_output=True,timeout=120);commands.append(dict(argv=argv,cwd=str(probe),exitCode=build.returncode));(directory/'build.stdout').write_bytes(build.stdout);(directory/'build.stderr').write_bytes(build.stderr);assert build.returncode==0,(build.stdout,build.stderr)
    binary=probe/'bin/Debug/net10.0/Evidence.dll'
    if shape=='custom-params':
        for path in [binary,probe/'bin/Debug/net10.0/External.dll',external/'bin/Debug/net10.0/External.dll']:
            content=path.read_bytes();assert content.count(b'OtherArrayAttribute')==1;path.write_bytes(content.replace(b'OtherArrayAttribute',b'ParamArrayAttribute'))
    positive=shape in ('genuine','metadata-version','genuine-params')
    if shape in ('custom-task','custom-params','genuine','genuine-params','metadata-version'):
        argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01','-trx',str(directory/'result.trx')];run=subprocess.run(argv,cwd=probe,capture_output=True,timeout=30);commands.append(dict(argv=argv,cwd=str(probe),exitCode=run.returncode));(directory/'run.stdout').write_bytes(run.stdout);(directory/'run.stderr').write_bytes(run.stderr)
        assert run.returncode==(1 if shape=='custom-params' else 0),(run.stdout,run.stderr)
        if shape=='custom-params':assert b'constructor parameters did not have matching fixture data' in run.stdout+run.stderr
    round12_retain_native(shape,directory,commands)
    if positive:assert tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')['sourceRevision']=='a'*40
    else:
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')
    if shape in ('local-stamp','external-stamp','external-metadata'):
        with pytest.raises(ValueError):tooling.native_helper.inspect_product(binary.read_bytes(),'a'*40)
    elif positive:assert tooling.native_helper.inspect_product(binary.read_bytes(),'a'*40)['sourceRevision']=='a'*40


@pytest.mark.parametrize('stage',['sdk','build'])
def test_round12_helper_prerequisite_diagnostics_remain_structured_blocked(qualified_runtime,monkeypatch,stage):
    native=tooling.native_helper;native._helper.cache_clear();real=native.subprocess.run
    def run(argv,**kwargs):
        if argv==['dotnet','--version']:return subprocess.CompletedProcess(argv,41,b'',b'SDK stderr detail') if stage=='sdk' else subprocess.CompletedProcess(argv,0,b'10.0.402\n',b'')
        if argv[:2]==['dotnet','build']:return subprocess.CompletedProcess(argv,42,b'',b'build stderr detail')
        return real(argv,**kwargs)
    monkeypatch.setattr(native.subprocess,'run',run)
    result=tooling.evaluate(qualified_runtime,'16.2','AC-16.2-02',scope='runtime')
    rejected(result,'NATIVE_HELPER_UNAVAILABLE','BLOCKED');assert result['assertionLedger'];native._helper.cache_clear()


@pytest.mark.parametrize('fault',['constructor-signature','named-blob','trailing-blob'])
@pytest.mark.parametrize('consumer',['test','product'])
def test_round12_source_stamp_requires_exact_native_constructor_and_blob(tmp_path,fault,consumer):
    source='namespace Synthetic; public class TestClass { [Xunit.Fact] public void Scenario01() {} }'
    binary=compile_evidence(tmp_path/'source-stamp','a'*40,source)
    if fault=='constructor-signature':
        pattern=bytes([4,0x20,1,1,0x0e]);assert binary.count(pattern)==1;mutated=binary.replace(pattern,bytes([4,0x20,1,1,8]))
    else:
        value=b'1.0.0+'+b'a'*40;pattern=bytes([1,0,len(value)])+value+bytes([0,0]);assert binary.count(pattern)==1
        changed=pattern[:-2]+bytes([1,0]) if fault=='named-blob' else bytes([1,0,len(value)-1])+value+bytes([0,0])
        mutated=binary.replace(pattern,changed)
    round11_retain('r12-stamp-'+consumer+'-'+fault,{'valid.dll.bytes':binary,'invalid.dll.bytes':mutated})
    with pytest.raises(ValueError):
        if consumer=='test':tooling.native_helper.inspect_assembly(mutated,'a'*40,'Synthetic.TestClass','Scenario01')
        else:tooling.native_helper.inspect_product(mutated,'a'*40)


@pytest.mark.parametrize('fault',['store-operation','response-operation','request-reuse','same-order'])
def test_round12_reverse_read_operations_are_distinct_and_ordered(repository,fault):
    data=deepcopy(OBSERVATIONS['AC-16.1-03']);proof=data['reverseSaves'][0]['execution'];read=proof['consumers'][0]
    if fault=='store-operation':read['storeRead']['operationId']=proof['saves'][0]['operationId']
    elif fault=='response-operation':read['response']['operationId']=read['storeRead']['operationId']
    elif fault=='request-reuse':read['requestId']=proof['saves'][0]['requestId'];read['scope']['requestId']=read['requestId']
    else:read['response']['order']=read['storeRead']['completedOrder']
    round8_submit(repository,'AC-16.1-03',data)


def test_round12_params_requires_complete_framework_attribute_blob(tmp_path):
    source='namespace Synthetic; public class TestClass { public TestClass(params string[] values) {} [Xunit.Fact] public void Scenario01() {} }'
    binary=compile_evidence(tmp_path/'params-blob','a'*40,source)
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    # CLI blob heap values are length-prefixed. The compiler interns this exact
    # four-byte parameterless attribute value, including its zero named count.
    pattern=bytes([4,1,0,0,0]);assert pattern in binary
    mutated=binary.replace(pattern,bytes([4,1,0,1,0]))
    round11_retain('r12-params-blob',{'valid.dll.bytes':binary,'invalid.dll.bytes':mutated})
    with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(mutated,'a'*40,'Synthetic.TestClass','Scenario01')


@pytest.mark.parametrize('principal',[' actor-a','actor-a ','\x1c','a\x1fb','a\x7fb','a\x85b','a\x9fb','actor-extra'])
def test_round12_native_access_principal_admission_precedes_read(repository,principal):
    data=deepcopy(OBSERVATIONS['AC-16.1-01']);row=data['authorization'][0];proof=row['capture'];proof['scope']['principal']=principal
    for key in ('request','storeRead','response'):proof[key]['scope']['principal']=principal
    proof['response']['decision']['callerPrincipalId']=principal
    proof['sourceState']['members'][principal]='TenantContributor'
    proof['storeRead']['snapshot']['projection']=tooling.measurements.domain.tenant_replay(proof['sourceState'],[proof['sourceRecord']],9,10)[0]
    round8_submit(repository,'AC-16.1-01',data,passes=principal=='actor-extra')


ROUND12_TENANTS = [('tenant/a',False),('tenant:a',False),(' tenant-a',False),('tenant a',False),('tenant\x1c',False),('tenant\x7f',False),('cafe\u0301',False),('tenant\u200b',False),('tenant\ue000',False),('tenant\u0378',False),('tenant\U0001f600',False),('café',True),('租户',True),('tenant-Å',True)]


def round12_rename_tenant(data,tenant):
    """Rename every retained native/key observation, including immutable raw hashes."""
    def rename(v):
        if isinstance(v,str):
            if v=='tenant-a':return tenant
            return v.replace('"tenant-a"',json.dumps(tenant,ensure_ascii=False)).replace(tooling.measurements.relationships.segment('tenant-a'),tooling.measurements.relationships.segment(tenant))
        if isinstance(v,list):return [rename(x) for x in v]
        if isinstance(v,dict):return {rename(k):rename(x) for k,x in v.items()}
        return v
    replaced=rename(data);hashes={}
    def collect(old,new):
        if isinstance(old,dict):
            if 'persistedPayload' in old:hashes[old['payloadSha256']]=digest(new['persistedPayload'].encode())
            for key,newkey in zip(old,new):collect(old[key],new[newkey])
        elif isinstance(old,list):
            for a,b in zip(old,new):collect(a,b)
    collect(data,replaced)
    def refresh(v):
        if isinstance(v,str):return hashes.get(v,v)
        if isinstance(v,list):return [refresh(x) for x in v]
        if isinstance(v,dict):return {k:refresh(x) for k,x in v.items()}
        return v
    return refresh(replaced)


@pytest.mark.parametrize('tenant,valid',ROUND12_TENANTS)
def test_round12_native_tenant_input_rules_admit_only_real_executed_requests(repository,tenant,valid):
    data=round12_rename_tenant(deepcopy(OBSERVATIONS['AC-16.1-01']),tenant)
    round8_submit(repository,'AC-16.1-01',data,passes=valid)


def test_round12_input_oracle_matches_compiled_actual_commons_evaluator(tmp_path):
    directory=tmp_path/'commons';directory.mkdir();(directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    sources=json.loads((ROOT/'_bmad/scripts/tests/fixtures/epic16-domain-sources.json').read_bytes())['sources']
    names=['TenantAccessEvaluator.cs','ITenantAccessStateStore.cs','ITenantAccessProjectionHealthProvider.cs','TenantAccessState.cs','TenantAccessProjectionHealth.cs','TenantAccessEvaluation.cs','TenantAccessDenialKind.cs','TenantRole.cs','TenantStatus.cs','TenantStatusJsonConverter.cs']
    for name in names:
        matches=[r for r in sources if r['path'].endswith('/'+name)];assert len(matches)==1
        row=matches[0];assert digest(row['source'].encode())==row['sha256'];(directory/name).write_text(row['source'])
    (directory/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup></Project>')
    (directory/'Program.cs').write_text('''using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
foreach (var c in JsonDocument.Parse(Console.In.ReadToEnd()).RootElement.EnumerateArray()) {
 string tenant=c.GetProperty("tenant").GetString()!,principal=c.GetProperty("principal").GetString()!;
 var store=new Store(principal);var health=new Health();
 var result=await TenantAccessEvaluator.EvaluateAsync("Write",new[]{tenant},principal,store,health,
 static r=>r=="Write",static s=>Enum.IsDefined((TenantStatus)s),static s=>(TenantStatus)s==TenantStatus.Active,static s=>(TenantStatus)s==TenantStatus.Disabled,
 static r=>Enum.IsDefined((TenantRole)r),static (r,q)=>(TenantRole)r is TenantRole.TenantContributor or TenantRole.TenantOwner,NullLogger.Instance);
 Console.WriteLine(JsonSerializer.Serialize(new{tenant,principal,result.IsAllowed,Denial=result.DenialKind.ToString(),store.Reads,health.ReadsHealth}));
}''')
    (directory/'Store.cs').write_text('''using Hexalith.Commons.TenantAccess; using Hexalith.Tenants.Contracts.Enums;
sealed class Store(string principal):ITenantAccessStateStore { public int Reads; public Task<TenantAccessState?> GetAsync(string id,CancellationToken ct=default) { Reads++; return Task.FromResult<TenantAccessState?>(new(id,(int)TenantStatus.Active,new Dictionary<string,int>{{principal,(int)TenantRole.TenantContributor}})); } }''')
    (directory/'Health.cs').write_text('''using Hexalith.Commons.TenantAccess;
sealed class Health:ITenantAccessProjectionHealthProvider { public int ReadsHealth; public ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(string id,CancellationToken ct=default) { ReadsHealth++; return ValueTask.FromResult<TenantAccessProjectionHealth?>(new(10,"10",false,false,false,false)); } }''')
    cases=[dict(tenant=t,principal='actor-a') for t,_ in ROUND12_TENANTS]+[dict(tenant='tenant-a',principal=p) for p in [' actor-a','actor-a ','a\x1cb','a\x85b','acteur-é','actor-extra']]
    argv=['dotnet','build','Probe.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(argv,cwd=directory,capture_output=True,timeout=120);assert build.returncode==0,(build.stdout,build.stderr)
    commands=[dict(argv=argv,cwd=str(directory),exitCode=build.returncode)];(directory/'build.stdout').write_bytes(build.stdout);(directory/'build.stderr').write_bytes(build.stderr)
    argv=['dotnet',str(directory/'bin/Debug/net10.0/Probe.dll')];run=subprocess.run(argv,cwd=directory,input=json.dumps(cases).encode(),capture_output=True,timeout=30);assert run.returncode==0,(run.stdout,run.stderr);commands.append(dict(argv=argv,cwd=str(directory),exitCode=run.returncode));(directory/'input.json').write_text(json.dumps(cases));(directory/'run.stdout').write_bytes(run.stdout);(directory/'run.stderr').write_bytes(run.stderr)
    actual=[json.loads(line) for line in run.stdout.splitlines()];assert len(actual)==len(cases)
    for case,row in zip(cases,actual):
        valid=tooling.native_helper.tenant_access_inputs(**case);allowed=valid['tenantValid'] and valid['principalValid'];assert row['IsAllowed'] is allowed,row
        assert row['Reads']==row['ReadsHealth']==int(allowed),row
        if not allowed:assert row['Denial']==('MalformedTenant' if not valid['tenantValid'] else 'MissingCaller'),row
    round12_retain_native('actual-commons-inputs',directory,commands)


@pytest.mark.parametrize('index',range(4))
def test_round12_reverse_save_rejects_coherent_shared_process(repository,index):
    data=deepcopy(OBSERVATIONS['AC-16.1-03']);proof=data['reverseSaves'][index]['execution'];process=proof['saves'][0]['processId'];older=proof['saves'][1]
    older['processId']=older['scope']['processId']=process
    consumer=next(c for c in proof['consumers'] if c['replicaId']==older['replicaId'])
    consumer['processId']=consumer['scope']['processId']=process
    consumer['storeRead']['scope']['processId']=consumer['response']['scope']['processId']=process
    round8_submit(repository,'AC-16.1-03',data)


# Iteration 13: all native execution below is test-authored disposable code. The
# fixed inspector never executes the supplied assemblies or their attributes.
def round13_build(directory, source, revision='a'*40):
    directory.mkdir(parents=True, exist_ok=True)
    (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (directory/'Evidence.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><GenerateAssemblyInfo>false</GenerateAssemblyInfo><EnableNETAnalyzers>false</EnableNETAnalyzers></PropertyGroup><ItemGroup><PackageReference Include="xunit.v3" Version="4.0.2" /></ItemGroup></Project>')
    (directory/'Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
    (directory/'Tests.cs').write_text(source)
    command=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']
    result=subprocess.run(command,cwd=directory,capture_output=True,timeout=120)
    (directory/'build.stdout').write_bytes(result.stdout);(directory/'build.stderr').write_bytes(result.stderr)
    commands=[dict(argv=command,cwd=str(directory),exitCode=result.returncode)]
    assert result.returncode==0,(result.stdout,result.stderr)
    return directory/'bin/Debug/net10.0/Evidence.dll', commands


ROUND13_DEFAULTS = [
 ('int','int value = 8675309','value == 8675309'),
 ('long','long value = 1','value == 1L'),
 ('string','string value = ""','value == ""'),
 ('null-reference','object value = null','value is null'),
 ('nullable-int','int? value = 7','value == 7'),
 ('nullable-null','int? value = null','value is null'),
 ('enum','Choice value = Choice.One','value == Choice.One'),
 ('nullable-enum','Choice? value = Choice.One','true'),
 ('decimal','decimal value = 1.5m','value == 1.5m'),
 ('datetime','System.DateTime value = default','value == default(System.DateTime)'),
 ('optional-string','[System.Runtime.InteropServices.Optional] string value','value is null'),
 ('optional-int','[System.Runtime.InteropServices.Optional] int value','value == 0'),
 ('params','params string[] value','value.Length == 0'),
 ('attribute-default','[System.Runtime.InteropServices.DefaultParameterValue(3)] int value','value == 3'),
 ('nullable-enum-null','Choice? value = null','value is null'),
 ('local-value','Value value = default','value.Number == 0'),
 ('nullable-decimal','decimal? value = 1.5m','value == 1.5m'),
 ('nullable-local-value','Value? value = null','value is null'),
 ('nullable-datetime','System.DateTime? value = null','value is null'),
 ('nullable-date-attribute','[System.Runtime.CompilerServices.DateTimeConstant(123)] System.DateTime? value','value.Value.Ticks == 123'),
 ('date-attribute','[System.Runtime.CompilerServices.DateTimeConstant(123)] System.DateTime value','value.Ticks == 123'),
 ('optional-enum','[System.Runtime.InteropServices.Optional] Choice value','value == Choice.Zero'),
 ('fixture-unused-default','Fixture value = null','value != null'),
 ('output-unused-default','Xunit.ITestOutputHelper value = null','value != null'),
 ('bool','bool value = true','value'),
 ('char','char value = \'é\'','value == \'é\''),
 ('byte','byte value = 2','value == 2'),
 ('double','double value = 1.25','value == 1.25'),
]


@pytest.fixture(scope='session')
def round13_defaults(tmp_path_factory):
    directory=tmp_path_factory.mktemp('round13-defaults')
    source='namespace Synthetic; public enum Choice { Zero, One } public struct Value { public int Number; } public class Fixture {}\n'
    for index,(name,parameter,condition) in enumerate(ROUND13_DEFAULTS):
        inheritance=' : Xunit.IClassFixture<Fixture>' if name=='fixture-unused-default' else ''
        source+=f'public class Default{index}({parameter}){inheritance} {{ [Xunit.Fact] public void Scenario01() {{ if (!({condition})) throw new System.Exception("Unexpected default"); }} }}\n'
    binary,commands=round13_build(directory,source)
    return directory,binary,commands


@pytest.mark.parametrize('index',range(len(ROUND13_DEFAULTS)),ids=[row[0] for row in ROUND13_DEFAULTS])
def test_round13_selected_typed_defaults_match_actual_pinned_runner(round13_defaults,index):
    directory,binary,commands=round13_defaults;shape=ROUND13_DEFAULTS[index][0];name=f'Synthetic.Default{index}'
    argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method',name+'.Scenario01']
    result=subprocess.run(argv,cwd=directory,capture_output=True,timeout=30)
    (directory/(shape+'.stdout')).write_bytes(result.stdout);(directory/(shape+'.stderr')).write_bytes(result.stderr)
    commands.append(dict(argv=argv,cwd=str(directory),exitCode=result.returncode))
    positive=shape!='nullable-enum'
    assert result.returncode==(0 if positive else 1),(shape,result.stdout,result.stderr)
    if positive:assert tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,name,'Scenario01')['method']=='Scenario01'
    else:
        assert b'cannot be converted' in result.stdout+result.stderr
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,name,'Scenario01')
    round12_retain_native('round13-defaults',directory,commands)


@pytest.mark.parametrize('fault',['wrong-type','truncated-constant','invalid-bool','invalid-null','decimal-scale','decimal-named','decimal-trailing','date-range','date-named'])
def test_round13_defaults_validate_selected_kind_encoding_and_complete_blob(tmp_path,fault):
    if fault.startswith('decimal'):
        parameter='decimal value = 1.23456789m';source='namespace Synthetic; public class TestClass('+parameter+') { [Xunit.Fact] public void Scenario01() {} }'
    elif fault.startswith('date'):
        source='namespace Synthetic; public class TestClass([System.Runtime.CompilerServices.DateTimeConstant(123456789)] System.DateTime value) { [Xunit.Fact] public void Scenario01() {} }'
    elif fault=='invalid-bool':source='namespace Synthetic; public class TestClass(bool value = true) { [Xunit.Fact] public void Scenario01() {} }'
    elif fault=='invalid-null':source='namespace Synthetic; public class TestClass(object value = null) { [Xunit.Fact] public void Scenario01() {} }'
    else:source='namespace Synthetic; public class TestClass(int value = 8675309, string other = "typed") { [Xunit.Fact] public void Scenario01() {} }'
    binary=round13_compile(tmp_path/fault,'a'*40,source)
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    if fault=='wrong-type':before=bytes.fromhex('05200201080e');after=bytes.fromhex('052002010e0e')
    elif fault=='truncated-constant':before=b'\x04'+(8675309).to_bytes(4,'little');after=b'\x03'+before[1:]
    elif fault=='invalid-bool':before=bytes([1,1]);after=bytes([1,2])
    elif fault=='invalid-null':before=bytes([4,0,0,0,0]);after=bytes([4,1,0,0,0])
    elif fault.startswith('decimal'):
        before=bytes([18,1,0,8,0])+bytes(8)+(123456789).to_bytes(4,'little')+bytes(2);after=bytearray(before)
        if fault=='decimal-scale':after[3]=29
        elif fault=='decimal-named':after[-2]=1
        else:after[0]=17
        after=bytes(after)
    else:
        before=bytes([12,1,0])+(123456789).to_bytes(8,'little')+bytes(2);after=bytearray(before)
        if fault=='date-range':after[3:11]=(2**63-1).to_bytes(8,'little')
        else:after[-2]=1
        after=bytes(after)
    locations=round13_locations(binary)
    if fault in ('wrong-type',):
        target=next(row for row in locations['methods'] if row['type']=='Synthetic.TestClass' and row['name']=='.ctor')['signatureOffset']
    elif fault in ('truncated-constant','invalid-bool','invalid-null'):
        target=next(row for row in locations['constants'] if row['type']=='Synthetic.TestClass' and row['parameter']=='value')['offset']
    else:
        target=next(row for row in locations['attributes'] if row['type'].endswith('DecimalConstantAttribute' if fault.startswith('decimal') else 'DateTimeConstantAttribute'))['valueOffset']
    assert binary[target:target+len(before)]==before,(fault,before.hex(),binary[target:target+len(before)].hex())
    mutant=binary[:target]+after+binary[target+len(before):]
    round11_retain('r13-default-'+fault,{'valid.dll.bytes':binary,'invalid.dll.bytes':mutant})
    with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(mutant,'a'*40,'Synthetic.TestClass','Scenario01')


@pytest.mark.parametrize('kind',['Task','ValueTask'])
def test_round13_async_return_kind_is_not_erased_by_signature_decoding(tmp_path,kind):
    source='namespace Synthetic; public class TestClass { [Xunit.Fact] public System.Threading.Tasks.'+kind+' Scenario01() => '+('System.Threading.Tasks.Task.CompletedTask' if kind=='Task' else 'default')+'; }'
    binary=round13_compile(tmp_path/kind,'a'*40,source)
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    encoded=0x12 if kind=='Task' else 0x11
    # Unique parameterless instance return signature; length includes the handle.
    offsets=[i for i in range(1,len(binary)-5) if binary[i:i+3]==bytes([0x20,0,encoded]) and binary[i-1] in (4,5)]
    assert len(offsets)==1,offsets
    mutant=bytearray(binary);mutant[offsets[0]+2]=0x11 if kind=='Task' else 0x12;mutant=bytes(mutant)
    round11_retain('r13-return-'+kind,{'valid.dll.bytes':binary,'invalid.dll.bytes':mutant})
    with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(mutant,'a'*40,'Synthetic.TestClass','Scenario01')


@pytest.mark.parametrize('shape',['assembly','assembly-generic','collection-string','collection-type','collection-generic','definition-string','definition-type','definition-named'])
@pytest.mark.parametrize('fault',['positive','named-count','trailing','constructor','signature-trailing'])
def test_round13_provider_attributes_require_complete_supported_metadata(tmp_path,shape,fault):
    generic=shape in ('assembly-generic','collection-generic')
    assembly='[assembly:Xunit.AssemblyFixture'+('<Synthetic.Fixture>]' if generic else '(typeof(Synthetic.Fixture))]') if shape.startswith('assembly') else ''
    definition='[Xunit.CollectionDefinition("UniqueCollectionR13"'+(', DisableParallelization=true' if shape=='definition-named' else '')+')]'
    if shape=='definition-type':definition='[Xunit.CollectionDefinition(DisableParallelization=true)]'
    collection='[Xunit.Collection("UniqueCollectionR13")]'
    if shape in ('collection-type','definition-type'):collection='[Xunit.Collection(typeof(Group))]'
    elif shape=='collection-generic':collection='[Xunit.Collection<Group>]'
    support='public class Fixture {} '+definition+' public class Group : Xunit.ICollectionFixture<Fixture> {}'
    source='using Synthetic; '+assembly+' namespace Synthetic { '+support+' '+('' if assembly else collection)+' public class TestClass(Fixture value) { [Xunit.Fact] public void Scenario01() { System.ArgumentNullException.ThrowIfNull(value); } } }'
    directory=tmp_path/'native';binary_path,commands=round13_build(directory,source);binary=binary_path.read_bytes()
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    if fault=='positive':
        argv=['dotnet',str(binary_path),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01'];run=subprocess.run(argv,cwd=directory,capture_output=True,timeout=30)
        (directory/'run.stdout').write_bytes(run.stdout);(directory/'run.stderr').write_bytes(run.stderr);commands.append(dict(argv=argv,cwd=str(directory),exitCode=run.returncode))
        assert run.returncode==0,(run.stdout,run.stderr)
    else:
        locations=round13_locations(binary)
        expected='Xunit.AssemblyFixtureAttribute' if assembly else 'Xunit.CollectionDefinitionAttribute' if shape.startswith('definition') else 'Xunit.CollectionAttribute'
        if generic:expected+='`1'
        attribute=next(row for row in locations['attributes'] if row['type'].split(']')[-1].split('<')[0]==expected)
        target=attribute['signatureOffset'] if fault in ('constructor','signature-trailing') else attribute['valueOffset']
        original=bytes.fromhex(attribute['signature'] if fault in ('constructor','signature-trailing') else attribute['value'])
        assert len(original)<128
        before=bytes([len(original)])+original;after=bytearray(before)
        if fault=='constructor':
            # Keep all other member-reference identity intact; the ctor's return
            # type must be Void, including generic parameterless constructors.
            after[3]=0x08
        elif fault=='signature-trailing':after[0]+=1
        elif fault=='trailing':after[0]+=1
        else:
            if shape in ('definition-type','definition-named'):after[-1]=2
            else:after[-2]=1
        assert binary[target:target+len(before)]==before
        mutant=binary[:target]+bytes(after)+binary[target+len(before):]
        (directory/'invalid.dll.bytes').write_bytes(mutant)
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(mutant,'a'*40,'Synthetic.TestClass','Scenario01')
    round12_retain_native('round13-provider-'+shape+'-'+fault,directory,commands)


@pytest.mark.parametrize('fault',['changed','hardlink','symlink'])
def test_round13_generator_rereads_native_after_actual_evaluation_before_writing(round10_runtime,monkeypatch,fault):
    repo=round10_runtime;story='16.1'
    for n in range(1,6):
        row=tooling.evaluate(repo,story,f'AC-{story}-0{n}');assert row['result']=='PASS',row
        dump(repo,tooling.receipt_path('runtime',row['scenarioId']),row)
    original=tooling.capture.inventory
    def mutate(reader,actual_story,rows,need):
        path=repo/rows[0]['bindings']['native']['path'];raw=path.read_bytes()
        if fault=='changed':path.write_bytes(raw+b'\n<!-- drift after admission -->')
        else:
            path.unlink();alias=repo/'artifacts/native-alias';alias.write_bytes(raw)
            if fault=='hardlink':os.link(alias,path)
            else:path.symlink_to(alias)
        return original(reader,actual_story,rows,need)
    monkeypatch.setattr(tooling.capture,'inventory',mutate)
    result,j,m=tooling.generate(repo,story,write=True)
    assert result['result']=='FAIL' and result['assertionLedger'],result
    assert result['blockers'] and j is None and m is None
    assert not any((repo/path).exists() for path in tooling.final_paths('runtime',story))


@pytest.mark.parametrize('exit_code',[1,150,137,-11])
def test_round13_host_failures_are_blocked_with_bounded_real_diagnostics(qualified_runtime,monkeypatch,exit_code):
    native=tooling.native_helper;workspace,assembly=native._helper();real=native.subprocess.run
    def run(argv,**kwargs):
        if argv==['dotnet',str(assembly)]:return subprocess.CompletedProcess(argv,exit_code,stdout=b'x'*5000+b'host-out',stderr=b'y'*5000+b'You must install or update .NET to run this application.')
        return real(argv,**kwargs)
    monkeypatch.setattr(native.subprocess,'run',run)
    with pytest.raises(native.HelperUnavailable) as error:native._run(dict(operation='provenance'))
    message=str(error.value);assert 'exit='+str(exit_code) in message and 'host-out' in message and 'install or update .NET' in message and len(message)<4200
    native.inspect_product.cache_clear();rejected(runtime_evaluate(qualified_runtime),'NATIVE_HELPER_UNAVAILABLE','BLOCKED')


def test_round13_documented_helper_rejection_remains_invalid_evidence(monkeypatch):
    native=tooling.native_helper;workspace,assembly=native._helper()
    monkeypatch.setattr(native.subprocess,'run',lambda *a,**k:subprocess.CompletedProcess(a[0],1,stdout=b'',stderr=b'Invalid native evidence.'))
    with pytest.raises(ValueError):native._run(dict(operation='metadata'))


@pytest.mark.parametrize('name',['Evidence','Goldens','Epic16.NativeHelper','apphost','sample.pdb','public.snk','Product.assets.cache','sample.dll','sample.dll.bytes'])
def test_round13_opaque_native_evidence_has_binary_normal_diffs(repository,name):
    path='module/validation/epic-16-prerequisite-implementation/review-iteration-13/build/bin/Debug/'+name
    raw=b'\x7fELF\0\x90\xff\r\nopaque-native\0';target=repository/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
    assert all(path+': '+key+': unset' in git(repository,'check-attr',key,'--',path) for key in ('text','diff','whitespace'))
    git(repository,'add','-f','--',path)
    diff=subprocess.run(['git','-C',str(repository),'diff','--cached','--',path],capture_output=True,check=True).stdout
    assert 'Binary files ' in diff.decode('utf-8') and raw not in diff and target.read_bytes()==raw


@pytest.mark.parametrize('name',['Tests.cs','capture.json','raw.trx','build.stdout','report.md'])
def test_round13_evidence_source_and_raw_text_stay_readable(repository,name):
    path='module/validation/epic-16-prerequisite-implementation/review-iteration-13/build/'+name
    attrs=git(repository,'check-attr','text','diff','whitespace','--',path)
    assert path+': text: unset' in attrs and path+': diff: set' in attrs and path+': whitespace: unset' in attrs


def test_round13_implementation_owners_do_not_expand_upstream_compatibility_scope(round10_runtime):
    result=tooling.evaluate(round10_runtime,'16.1','AC-16.1-05');assert result['result']=='PASS',result
    implementations={p['assemblyName'] for p in result['bindings']['testBinary']['products']}
    assert {'Hexalith.EventStore.Server','Hexalith.Commons.TenantAccess'}<=implementations
    assert [p['assemblyName'] for p in result['bindings']['upstreamApiPreservation']]==list(tooling.products.UPSTREAM_API_NAMES)
    assert not {'Hexalith.EventStore.Server','Hexalith.Commons.TenantAccess'} & set(tooling.products.UPSTREAM_API_NAMES)


_ROUND13_LOCATOR = None


def round13_locations(binary):
    """Locate exact metadata heap bytes for mutations without loading the PE."""
    global _ROUND13_LOCATOR
    import tempfile
    if _ROUND13_LOCATOR is None:
        directory=Path(tempfile.mkdtemp(prefix='epic16-metadata-locations-'))
        (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
        (directory/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>')
        (directory/'SignatureNames.cs').write_bytes((ROOT/'_bmad/scripts/epic16_native_helper/SignatureNames.cs').read_bytes())
        (directory/'Program.cs').write_text('''using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Hexalith.Conversations.Tooling;
using var pe = new PEReader(File.OpenRead(args[0])); var reader=pe.GetMetadataReader(); var names=new SignatureNames();
int Offset(BlobHandle h) => pe.PEHeaders.MetadataStartOffset+reader.GetHeapMetadataOffset(HeapIndex.Blob)+MetadataTokens.GetHeapOffset(h);
var attributes=new List<object>(); var constants=new List<object>(); var methods=new List<object>();
foreach(var h in reader.CustomAttributes) { var a=reader.GetCustomAttribute(h); if(a.Constructor.Kind!=HandleKind.MemberReference) continue; var c=reader.GetMemberReference((MemberReferenceHandle)a.Constructor); attributes.Add(new{type=names.Name(reader,c.Parent),signature=Convert.ToHexString(reader.GetBlobBytes(c.Signature)),signatureOffset=Offset(c.Signature),value=Convert.ToHexString(reader.GetBlobBytes(a.Value)),valueOffset=Offset(a.Value)}); }
foreach(var h in reader.TypeDefinitions) { var t=reader.GetTypeDefinition(h); foreach(var mh in t.GetMethods()) { var m=reader.GetMethodDefinition(mh); methods.Add(new{type=names.Name(reader,h),name=reader.GetString(m.Name),signature=Convert.ToHexString(reader.GetBlobBytes(m.Signature)),signatureOffset=Offset(m.Signature)}); foreach(var ph in m.GetParameters()) { var p=reader.GetParameter(ph); var ch=p.GetDefaultValue(); if(ch.IsNil) continue; var c=reader.GetConstant(ch); constants.Add(new{type=names.Name(reader,h),parameter=reader.GetString(p.Name),kind=c.TypeCode.ToString(),offset=Offset(c.Value),value=Convert.ToHexString(reader.GetBlobBytes(c.Value))}); } } }
var types=reader.TypeDefinitions.Select(h=>new{name=names.Name(reader,h),coded=MetadataTokens.GetRowNumber(h)*4}).Concat(reader.TypeReferences.Select(h=>new{name=names.Name(reader,h),coded=MetadataTokens.GetRowNumber(h)*4+1})).ToArray();
Console.Write(JsonSerializer.Serialize(new{attributes,constants,methods,types}));''')
        argv=['dotnet','build','Probe.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];run=subprocess.run(argv,cwd=directory,capture_output=True,timeout=120)
        (directory/'build.stdout').write_bytes(run.stdout);(directory/'build.stderr').write_bytes(run.stderr)
        assert run.returncode==0,(run.stdout,run.stderr)
        round12_retain_native('round13-mutation-locator',directory,[dict(argv=argv,cwd=str(directory),exitCode=run.returncode)])
        _ROUND13_LOCATOR=directory
    target=_ROUND13_LOCATOR/'input.dll.bytes';target.write_bytes(binary)
    result=subprocess.run(['dotnet',str(_ROUND13_LOCATOR/'bin/Debug/net10.0/Probe.dll'),str(target)],capture_output=True,timeout=30)
    assert result.returncode==0,(result.stdout,result.stderr)
    return json.loads(result.stdout)


@pytest.mark.parametrize('fault',['default','provider','return-kind','nullable-constraint'])
def test_round13_runtime_caller_rejects_malformed_constructor_provider_or_return(qualified_runtime,tmp_path,fault):
    repo=qualified_runtime;candidate=git(repo,'rev-parse','HEAD');name='Hexalith.Conversations.IntegrationTests.Epic16.Story16_2EvidenceTests'
    prefix='namespace Hexalith.Conversations.IntegrationTests.Epic16; '
    if fault=='default':source=prefix+'public class Story16_2EvidenceTests(int value = 8675309) { [Xunit.Fact] public void Scenario02() {} }'
    elif fault=='nullable-constraint':source=prefix+'public class Story16_2EvidenceTests(int? value = null) { [Xunit.Fact] public void Scenario02() {} }'
    elif fault=='provider':source=prefix+'public class Fixture {} [Xunit.CollectionDefinition("R13Collection")] public class Group : Xunit.ICollectionFixture<Fixture> {} [Xunit.Collection("R13Collection")] public class Story16_2EvidenceTests(Fixture value) { [Xunit.Fact] public void Scenario02() {} }'
    else:source=prefix+'public class Story16_2EvidenceTests { [Xunit.Fact] public System.Threading.Tasks.Task Scenario02() => System.Threading.Tasks.Task.CompletedTask; }'
    binary=round13_compile(tmp_path/fault,candidate,source);metadata=tooling.native_helper.inspect_assembly(binary,candidate,name,'Scenario02');assert metadata['method']=='Scenario02'
    locations=round13_locations(binary);mutant=bytearray(binary)
    if fault=='provider':
        row=next(r for r in locations['attributes'] if r['type'].endswith(']Xunit.CollectionAttribute'));offset=row['valueOffset'];blob=bytes.fromhex(row['value']);assert len(blob)<128;mutant[offset+len(blob)-1]=1
    else:
        row=next(r for r in locations['methods'] if r['type']==name and r['name']==('.ctor' if fault in ('default','nullable-constraint') else 'Scenario02'));offset=row['signatureOffset'];blob=bytes.fromhex(row['signature']);assert len(blob)<128
        if fault in ('default','nullable-constraint'):assert blob[-1]==0x08;mutant[offset+len(blob)]=0x0e
        else:assert blob[2]==0x12;mutant[offset+3]=0x11
    root=json.loads((repo/tooling.capture.capture_path('16.2','AC-16.2-02')).read_bytes())['originalRepositoryRoot']
    retain_capture(repo,'AC-16.2-02',bytes(mutant),root=root)
    rejected(runtime_evaluate(repo),'NATIVE_BINARY_DRIFT')
    round11_retain('r13-runtime-caller-'+fault,{'valid.dll.bytes':binary,'invalid.dll.bytes':bytes(mutant)})


@pytest.mark.parametrize('product_index',[7,8])
@pytest.mark.parametrize('fault',['missing','duplicate'])
def test_round13_new_owner_capture_inventory_is_exact_in_schema_and_consumer(qualified_runtime,product_index,fault):
    repo=qualified_runtime;scenario='AC-16.2-02';path=tooling.capture.capture_path('16.2',scenario);document=json.loads((repo/path).read_bytes())
    if fault=='missing':document['products'].pop(product_index)
    else:document['products'][product_index]=deepcopy(document['products'][0])
    validator=jsonschema.Draft202012Validator(json.loads((repo/tooling.SCHEMA).read_bytes())['$defs']['nativeCapture'])
    if fault=='missing':assert list(validator.iter_errors(document))
    else:assert not list(validator.iter_errors(document)) # ordered identity is a consumer obligation
    dump(repo,path,document);result=runtime_evaluate(repo)
    assert result['result']=='FAIL' and result['blockers'],result


@pytest.mark.parametrize('product_index,surface_index',[(7,0),(7,1),(8,0)])
def test_round13_new_owner_requires_actual_behavior_surface(qualified_runtime,product_index,surface_index):
    repo=qualified_runtime;scenario='AC-16.2-02';product=tooling.products.PRODUCTS[product_index]
    path=tooling.capture.capture_path('16.2',scenario);capture=json.loads((repo/path).read_bytes());row=capture['products'][product_index]
    missing=product['surfaces'][surface_index].rsplit('.',1)[1]
    source=round9_product_source(product).replace('class '+missing+' ', 'class UnrelatedBehaviorOwner ')
    binary=round9_compile_product(product,row['sourceRevision'],source);metadata=tooling.native_helper.inspect_product(binary,row['sourceRevision'])
    artifact=tooling.capture.binary_path('16.2',digest(binary));(repo/artifact).write_bytes(binary)
    assert metadata['assemblyName']==product['assemblyName'] and product['surfaces'][surface_index] not in metadata['declaredTypes']
    round10_rewrite_runtime(repo,scenario,lambda p:p['loadedProducts'][product_index].update(sha256=digest(binary),mvid=metadata['mvid']))
    capture=json.loads((repo/path).read_bytes());capture['products'][product_index].update(path=artifact,sha256=digest(binary),mvid=metadata['mvid']);dump(repo,path,capture)
    rejected(runtime_evaluate(repo),'PRODUCT_METADATA_DRIFT')


def round13_compile(directory,revision,source):
    binary,commands=round13_build(directory,source,revision)
    round12_retain_native('round13-build-'+digest((revision+'\n'+source).encode()),directory,commands)
    return binary.read_bytes()


@pytest.mark.parametrize('sign',[0,1,127,128,255])
def test_round13_decimal_default_sign_matches_actual_trusted_constructor(tmp_path,sign):
    expected='1m' if sign==0 else '-1m'
    source='namespace Synthetic; public class TestClass([System.Runtime.CompilerServices.DecimalConstant(0,'+str(sign)+',0u,0u,1u)] decimal value) { [Xunit.Fact] public void Scenario01() { if (value != '+expected+') throw new System.Exception("Incorrect native decimal sign"); } }'
    directory=tmp_path/'decimal-sign';binary,commands=round13_build(directory,source)
    argv=['dotnet',str(binary),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01'];run=subprocess.run(argv,cwd=directory,capture_output=True,timeout=30)
    (directory/'run.stdout').write_bytes(run.stdout);(directory/'run.stderr').write_bytes(run.stderr);commands.append(dict(argv=argv,cwd=str(directory),exitCode=run.returncode))
    assert run.returncode==0,(run.stdout,run.stderr)
    assert tooling.native_helper.inspect_assembly(binary.read_bytes(),'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    round12_retain_native('round13-native-decimal-sign-'+str(sign),directory,commands)


@pytest.mark.parametrize('path',['null','optional','nonnull'])
@pytest.mark.parametrize('argument',['string','object'])
def test_round13_nullable_constraint_applies_before_all_default_paths(tmp_path,path,argument):
    parameter={'null':'int? value = null','optional':'[System.Runtime.InteropServices.Optional] int? value','nonnull':'int? value = 7'}[path]
    condition='value == 7' if path=='nonnull' else 'value is null'
    source='namespace Synthetic; public class TestClass('+parameter+') { [Xunit.Fact] public void Scenario01() { if (!('+condition+')) throw new System.Exception("Unexpected native default"); } }'
    directory=tmp_path/'native';binary_path,commands=round13_build(directory,source);binary=binary_path.read_bytes()
    assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    location=next(r for r in round13_locations(binary)['methods'] if r['type']=='Synthetic.TestClass' and r['name']=='.ctor');signature=bytes.fromhex(location['signature']);assert signature[-1]==0x08 and len(signature)<128
    mutant=bytearray(binary);mutant[location['signatureOffset']+len(signature)]=0x0e if argument=='string' else 0x1c
    round13_nullable_runner_control(directory,binary_path,binary,bytes(mutant),commands,'path-'+path+'-'+argument)


@pytest.mark.parametrize('argument',['array','nested','byref-like'])
def test_round13_nullable_constraint_excludes_invalid_value_shapes(tmp_path,argument):
    support='public struct Wrapper {}'
    parameter='Wrapper? value = null'
    if argument=='nested':support=' '.join('public struct Padding'+str(i)+' {}' for i in range(33))+' public struct Wrapper<T> {}';parameter='Wrapper<int>? value = null'
    if argument=='byref-like':support+=' public ref struct Blocked {}'
    source='namespace Synthetic; '+support+' public class TestClass('+parameter+') { [Xunit.Fact] public void Scenario01() { if (value is not null) throw new System.Exception("Unexpected native default"); } }'
    directory=tmp_path/'native';binary_path,commands=round13_build(directory,source);binary=binary_path.read_bytes();locations=round13_locations(binary)
    if argument=='nested':
        # The source-authored generic struct is runnable, but intentionally outside
        # this inspector's bounded local non-generic default subset.
        with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')
    else:assert tooling.native_helper.inspect_assembly(binary,'a'*40,'Synthetic.TestClass','Scenario01')['method']=='Scenario01'
    location=next(r for r in locations['methods'] if r['type']=='Synthetic.TestClass' and r['name']=='.ctor');signature=bytearray.fromhex(location['signature']);assert len(signature)<128
    wrapper=next(r['coded'] for r in locations['types'] if r['name']=='Synthetic.Wrapper'+('`1' if argument=='nested' else ''))
    if argument=='array':assert signature[-2:]==bytes([0x11,wrapper]);signature[-2:]=bytes([0x1d,0x08])
    elif argument=='nested':
        nullable=next(r['coded'] for r in locations['types'] if r['name'].endswith(']System.Nullable`1'));assert 128<=wrapper<16384 and 128<=nullable<16384
        encoded_wrapper=bytes([0x80 | (wrapper>>8),wrapper&255]);encoded_nullable=bytes([0x80 | (nullable>>8),nullable&255])
        assert signature[-6:]==bytes([0x15,0x11])+encoded_wrapper+bytes([1,8]);signature[-4:-2]=encoded_nullable
    else:
        blocked=next(r['coded'] for r in locations['types'] if r['name']=='Synthetic.Blocked');assert blocked<128 and signature[-2:]==bytes([0x11,wrapper]);signature[-1]=blocked
    offset=location['signatureOffset']+1;mutant=binary[:offset]+bytes(signature)+binary[offset+len(signature):]
    round13_nullable_runner_control(directory,binary_path,binary,mutant,commands,'shape-'+argument)


def round13_nullable_runner_control(directory,binary_path,valid,invalid,commands,label):
    for phase,content in [('valid',valid),('invalid',invalid)]:
        binary_path.write_bytes(content);(directory/(phase+'.dll.bytes')).write_bytes(content)
        argv=['dotnet',str(binary_path),'-automated','sync','-failSkips','-method','Synthetic.TestClass.Scenario01'];run=subprocess.run(argv,cwd=directory,capture_output=True,timeout=30)
        (directory/(phase+'.stdout')).write_bytes(run.stdout);(directory/(phase+'.stderr')).write_bytes(run.stderr);commands.append(dict(argv=argv,cwd=str(directory),exitCode=run.returncode,phase=phase,binarySha256=digest(content)))
        assert run.returncode==(0 if phase=='valid' else 1),(phase,run.stdout,run.stderr)
        if phase=='invalid':assert b'TypeLoadException' in run.stdout+run.stderr
    with pytest.raises(ValueError):tooling.native_helper.inspect_assembly(invalid,'a'*40,'Synthetic.TestClass','Scenario01')
    round12_retain_native('round13-nullable-constraint-'+label,directory,commands)
