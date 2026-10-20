"""Checkpoint C independent measurement, threshold edges and restore-checked fault receipts."""
from __future__ import annotations

from copy import deepcopy
import json
from pathlib import Path
import shutil
import sys

import pytest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / '_bmad/scripts'))
import generate_sm2_evidence as generator
import generate_story_record as records
import verify_minimal_module_fixture as fixture
import verify_sm2_evidence as verifier
import verify_story_candidate as chain
import generate_thin_authoring_guidance as guidance


def copy_input(repository, path):
    target = repository / path
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(ROOT / path, target)


@pytest.fixture(scope='module')
def committed_facts():
    """Cache immutable neutral Git facts only; every content and semantic check still executes."""
    cache = {}
    original = chain.current.bridge
    def bridge(repository, request):
        key = json.dumps(request, sort_keys=True)
        if key not in cache:
            cache[key] = original(ROOT, request)
        return deepcopy(cache[key])
    return bridge


@pytest.fixture
def sm2_repository(tmp_path, monkeypatch, committed_facts):
    repository = tmp_path / 'repository'
    repository.mkdir()
    # Read-only object access to real accepted A/B history, without cloning or entering a submodule.
    (repository / '.git').write_text('gitdir: ' + str(ROOT / '.git') + '\n')
    a, b = (json.loads((ROOT / path).read_bytes()) for path in (chain.RECORDS[0], chain.RECORDS[2]))
    inputs = {chain.CONTRACT, chain.AUTHORITY, generator.SCHEMA, 'global.json', 'Hexalith.Conversations.slnx',
              guidance.SOURCE_PATH, records.V2_11_1_SPEC, records.V2_11_2_SPEC,
              'docs/release-evidence/thin-authoring-guidance-v2.schema.json', fixture.PROOF_SCHEMA_PATH,
              fixture.GUIDANCE_JSON, fixture.GUIDANCE_MARKDOWN, fixture.PROOF_JSON_PATH, fixture.PROOF_MARKDOWN_PATH,
              *chain.RECORDS, *generator.HISTORY_PINS, *fixture.FROZEN_PATHS,
              *(row['path'] for row in a['thinGuidanceExecution']['receipts']),
              *(row['path'] for row in b['fixtureProofExecution']['receipts']),
              *(row['path'] for row in b['fixtureProofExecution']['fixture']['lockFiles']),
              a['thinGuidanceExecution']['contract']['path'], b['fixtureProofExecution']['contract']['path'],
              *(path for _, path, _ in guidance.PREDECESSORS)}
    for path in inputs:
        copy_input(repository, path)
    monkeypatch.setattr(chain.current, 'bridge', committed_facts)
    payload = generator.build_signable(repository)
    j, m = generator.finalize(payload)
    (repository / generator.JSON_PATH).write_bytes(j)
    (repository / generator.MARKDOWN_PATH).write_bytes(m)
    return repository


def snapshot(repository):
    return {path.relative_to(repository).as_posix(): path.read_bytes() for path in repository.rglob('*')
            if path.is_file()}


def snapshot_hash(contents):
    return chain.current.digest(fixture.canonical({path: chain.current.digest(content)
                                                  for path, content in sorted(contents.items())}))


def test_independent_generation_is_byte_identical(sm2_repository):
    repo = sm2_repository
    first = ((repo / generator.JSON_PATH).read_bytes(), (repo / generator.MARKDOWN_PATH).read_bytes())
    assert generator.finalize(generator.build_signable(repo)) == first
    result = verifier.verify(repo)
    assert result['result'] == 'PASS' and result['blockers'] == []
    assert result['metrics']['fileCount'] == 13
    assert result['metrics']['physicalLoc'] == 451
    assert result['metrics']['nonblankLoc'] == 396
    assert result['metrics']['oq2Decision'] == 'accepted'
    assert result['assertionLedger'] and all(row['state'] == 'PASS' for row in result['assertionLedger'])


@pytest.mark.parametrize('state,code,expected', [('FAIL', 'HELPER_FAILURE', 'FAIL'),
                                               ('FAIL', None, 'FAIL'), ('SKIPPED', None, 'BLOCKED')])
def test_candidate_respects_failed_or_skipped_neutral_facts(sm2_repository, monkeypatch, state, code, expected):
    original = chain.current.bridge
    def altered(repository, request):
        result = original(repository, request)
        result['result'] = 'FAIL' if state == 'FAIL' else 'PASS'
        result['blockers'] = [code] if code else []
        result['assertionLedger'].append(dict(id='unanticipated:helper-check', subject='independent neutral fact', state=state))
        return result
    monkeypatch.setattr(chain.current, 'bridge', altered)
    result = chain.verify(sm2_repository)
    assert result['result'] == expected
    assert result['blockers']
    assert next(row for row in result['assertionLedger'] if row['id'] == 'unanticipated:helper-check')['state'] == state
    if code:
        assert code in result['blockers']


@pytest.mark.parametrize('files,decision,reduction', [(28, 'accepted', '51.72'), (29, 'accepted', '50.00'),
                                                    (30, 'rejected', '48.28')])
def test_inclusive_threshold_before_rounding(files, decision, reduction):
    result = generator.comparison(files, 451)
    assert result['oq2Decision'] == decision
    assert result['fileReductionPercentage'] == reduction
    assert result['comparisonConfidence'] == 'low'
    assert generator.sensitivity(files)['addingOneFlipsAttainment'] == (files == 29)
    assert generator.sensitivity(files)['removingOneFlipsAttainment'] == (files == 30)


def mutate(repo, doc, fault):
    m = doc['measurement']
    if fault == 'source-added':
        (repo / fixture.FIXTURE_ROOT / 'Unexpected.cs').write_text('class Unexpected {}\n')
    elif fault == 'source-missing':
        (repo / fixture.FROZEN_PATHS[2]).unlink()
    elif fault == 'platform-ownership-added':
        target = repo / fixture.FROZEN_PATHS[7]
        target.write_bytes(target.read_bytes() + b'// Module-owned runtime: AddOpenTelemetry\n')
    elif fault in ('apphost-omitted', 'apphost-project-omitted', 'source-omitted'):
        suffix = 'AppHost.cs' if fault == 'apphost-omitted' else '.AppHost.csproj' if fault == 'apphost-project-omitted' else 'README.md'
        m['paths'] = [row for row in m['paths'] if not row['path'].endswith(suffix)]
    elif fault == 'path-duplicated':
        m['paths'].append(deepcopy(m['paths'][0]))
    elif fault == 'path-unclassified':
        m['paths'][0]['category'] = ''
    elif fault == 'declared-files': m['fileCount'] = 12
    elif fault == 'declared-loc': m['physicalLoc'] = 1
    elif fault == 'declared-nonblank': m['nonblankLoc'] = 1
    elif fault == 'source-hash': m['paths'][0]['sha256'] = '0'*64
    elif fault == 'physical-loc-row': m['paths'][0]['physicalLoc'] += 1
    elif fault in ('sm1-mutated', 'v1-json-mutated', 'v1-markdown-mutated', 'history-declaration-rehashed'):
        path = generator.SM1 if fault == 'sm1-mutated' else generator.BASELINE_MD if fault == 'v1-markdown-mutated' else generator.BASELINE
        target = repo / path
        target.write_bytes(target.read_bytes() + b'\n')
        if fault == 'history-declaration-rehashed':
            next(row for row in doc['historicalBaseline']['history'] if row['path'] == path)['sha256'] = verifier.digest(target.read_bytes())
    elif fault == 'arithmetic': doc['comparison']['fileReductionPercentage'] = '99.99'
    elif fault == 'confidence': doc['comparison']['comparisonConfidence'] = 'high'
    elif fault == 'decision': doc['comparison']['oq2Decision'] = 'rejected'
    elif fault == 'sensitivity': doc['singleFileSensitivity']['addingOneFlipsAttainment'] = True
    elif fault == 'payload-digest': doc['digests']['signablePayloadSha256'] = '0'*64
    elif fault == 'markdown':
        target = repo / generator.MARKDOWN_PATH
        target.write_bytes(target.read_bytes() + b'not canonical\n')
    elif fault == 'unknown-property': doc['unapproved'] = True
    elif fault == 'checkpoint-candidate': doc['candidate']['checkpoints']['B'] = doc['candidate']['checkpoints']['A']
    elif fault == 'predecessor-binding': doc['provenance'][0]['sha256'] = '0'*64
    elif fault == 'checkpoint-input-drift':
        target = repo / chain.RECORDS[3]
        target.write_bytes(target.read_bytes() + b'\n')
    elif fault == 'history-unavailable': doc['candidate']['commit'] = 'f'*40
    else: raise AssertionError('unmapped fault: ' + fault)
    # Re-sign semantic mutations: detection must be independent of checksum drift.
    if fault not in ('payload-digest', 'markdown'):
        payload = {key: value for key, value in doc.items() if key != 'digests'}
        j, md = generator.finalize(payload)
        (repo / generator.JSON_PATH).write_bytes(j)
        (repo / generator.MARKDOWN_PATH).write_bytes(md)
    else:
        (repo / generator.JSON_PATH).write_bytes(generator.serialize(doc))


@pytest.mark.parametrize('fault', list(verifier.FAULT_EXPECTATIONS))
def test_checkpoint_c_fault_matrix(sm2_repository, fault, record_property):
    repo = sm2_repository
    before = snapshot(repo)
    doc = json.loads(before[generator.JSON_PATH])
    observed = None
    try:
        mutate(repo, doc, fault)
        observed = verifier.verify(repo)
        expected = verifier.FAULT_EXPECTATIONS[fault]
        assert expected in observed['blockers'], 'FAULT_NOT_DETECTED: ' + repr(observed)
        assert observed['result'] == ('BLOCKED' if fault == 'history-unavailable' else 'FAIL'), 'FAULT_NOT_DETECTED'
        assert observed['assertionLedger'], 'FAULT_NOT_DETECTED: vacuous ledger'
    finally:
        for path in tuple(snapshot(repo)):
            if path not in before:
                (repo / path).unlink()
        for path, content in before.items():
            target = repo / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
        after = snapshot(repo)
        assert after == before, 'FIXTURE_NOT_RESTORED'
    record_property('story113_source_revision', fixture.git_head(ROOT))
    record_property('story113_evidence_sha256', verifier.digest(before[generator.JSON_PATH]))
    record_property('story113_fault_id', fault)
    record_property('story113_expected_code', verifier.FAULT_EXPECTATIONS[fault])
    record_property('story113_observed_blockers', json.dumps(observed['blockers']))
    record_property('story113_observed_result', observed['result'])
    record_property('story113_observed_exit', '2' if observed['result'] == 'BLOCKED' else '1')
    record_property('story113_before_sha256', snapshot_hash(before))
    record_property('story113_after_sha256', snapshot_hash(after))
    record_property('story113_assertion_ledger', json.dumps(observed['assertionLedger']))
