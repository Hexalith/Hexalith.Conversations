"""Hermetic current-policy regressions; historical authority lanes remain explicit-only."""
from __future__ import annotations

import copy
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / '_bmad/scripts'))
import evidence_boundary_current as current
import verify_story_completion_workflows as completion


@pytest.fixture(autouse=True)
def _stamp_current_candidate(record_property):
    revision = subprocess.run(['git', '-C', str(ROOT), 'rev-parse', 'HEAD^{commit}'],
        check=True, capture_output=True, text=True).stdout.strip()
    record_property('story103_source_revision', revision)


@pytest.fixture
def surfaces(tmp_path):
    for path in ('.agents/skills', '.claude/skills', '_bmad/custom', '_bmad/bmm', '_bmad/core'):
        shutil.copytree(ROOT / path, tmp_path / path)
    shutil.copy2(ROOT / '_bmad/config.toml', tmp_path / '_bmad/config.toml')
    runbook = tmp_path / current.GUIDANCE[-1]
    runbook.parent.mkdir(parents=True, exist_ok=True)
    runbook.write_bytes((ROOT / current.GUIDANCE[-1]).read_bytes())
    binding = json.loads((ROOT / current.BINDING).read_bytes())
    return tmp_path, binding


def workflows(root, binding):
    result = current.empty_result('a' * 40)
    current.workflow_checks(root, result, binding)
    return current.finalize(result, applicable=True)


@pytest.mark.parametrize('state,code', [('FAIL', 'EVIDENCE_HASH_MISMATCH'), ('BLOCKED', 'HISTORY_UNAVAILABLE')])
def test_stable_code_contract_registry_is_unique(state, code):
    result = current.empty_result('a' * 40)
    result['blockers'] = [code, code]
    result['assertionLedger'] = [dict(id='fixture', subject='fixture', state='SKIPPED' if state == 'BLOCKED' else 'FAIL')]
    observed = current.finalize(result, applicable=True)
    assert observed['result'] == state
    assert observed['blockers'] == [code]
    assert current.FAIL_CODES.isdisjoint(current.BLOCKED_CODES)
    assert (current.FAIL_CODES | current.BLOCKED_CODES).isdisjoint(current.WARNING_CODES)


@pytest.fixture(scope='module')
def git_repository(tmp_path_factory):
    root = tmp_path_factory.mktemp('bridge')
    def git(*args):
        return subprocess.run(['git', '-C', str(root), *args], check=True, capture_output=True).stdout.decode().strip()
    git('init', '-q'); git('config', 'user.email', 'fixture@example.invalid'); git('config', 'user.name', 'Fixture')
    (root / 'evidence.txt').write_text('original\n')
    git('add', '.'); git('commit', '-qm', 'test: establish fixture')
    before = git('rev-parse', 'HEAD')
    (root / 'evidence.txt').write_text('current\n'); git('add', '.'); git('commit', '-qm', 'test: change fixture')
    return root, before, git('rev-parse', 'HEAD')


@pytest.mark.parametrize('fault,expected', [
    ('hash','EVIDENCE_HASH_MISMATCH'), ('path','EVIDENCE_PATH_ESCAPE'),
    ('missing','EVIDENCE_FILE_MISSING'), ('output','EVIDENCE_OUTPUT_FORBIDDEN'),
    ('signable','SIGNABLE_PAYLOAD_MISMATCH'), ('subset','CHANGED_PATH_SET_MISMATCH'),
    ('superset','CHANGED_PATH_SET_MISMATCH'), ('roots','ROOT_OF_TRUST_NOT_PINNED'),
    ('allowlist','SIGNED_EVIDENCE_ALLOWLISTED'), ('history','REVISION_INVALID'),
])
def test_stable_code_contract_bridge_faults(git_repository, fault, expected, record_property):
    root, before, candidate = git_repository
    original = (root / 'evidence.txt').read_bytes()
    manifest = [dict(path='evidence.txt', sha256=hashlib.sha256(original).hexdigest(), role='evidence')]
    request = dict(baseline=before, candidate=candidate, expectedPaths=['evidence.txt'], manifest=manifest)
    if fault == 'hash': manifest[0]['sha256'] = '0' * 64
    if fault == 'path': manifest[0]['path'] = '../evidence.txt'
    if fault == 'missing': manifest[0]['path'] = 'absent.txt'
    if fault == 'output': manifest[0]['path'] = 'obj/evidence.txt'
    if fault == 'subset': request['expectedPaths'] = []
    if fault == 'superset': request['expectedPaths'] += ['other.txt']
    if fault == 'history': request['baseline'] = 'f' * 40
    payload = ''.join(f'{row["path"]}\t{row["sha256"]}\t{row["role"]}\n' for row in manifest)
    request['signablePayloadHash'] = hashlib.sha256(payload.encode()).hexdigest()
    if fault == 'signable': request['signablePayloadHash'] = '0' * 64
    if fault in ('roots','allowlist'):
        (root / 'roots.cs').write_text('internal class Roots { const string Trusted = "pinned"; }')
        request['roots'] = dict(sourcePath='roots.cs', constants={'Trusted': 'wrong' if fault == 'roots' else 'pinned'},
            allowlist=['docs/release-evidence/signed.json'] if fault == 'allowlist' else [])
    try:
        observed = current.bridge(root, request)
        record_property('story103_observed_blockers', json.dumps(observed['blockers']))
        record_property('story103_expected_code', expected)
        assert expected in observed['blockers']
        assert observed['blockers'] == sorted(set(observed['blockers']))
        assert observed['result'] != 'PASS'
    finally:
        (root / 'evidence.txt').write_bytes(original)
        (root / 'roots.cs').unlink(missing_ok=True)
    record_property('story103_before_sha256', hashlib.sha256(original).hexdigest())
    record_property('story103_after_sha256', hashlib.sha256((root / 'evidence.txt').read_bytes()).hexdigest())
    assert (root / 'evidence.txt').read_bytes() == original


def test_stable_code_contract_raw_gitlink_is_never_filtered(git_repository):
    root, before, candidate = git_repository
    index = root / '.git/index'
    original = index.read_bytes()
    git = lambda *args: subprocess.run(['git', '-C', str(root), *args], check=True, capture_output=True).stdout.decode().strip()
    try:
        git('update-index', '--add', '--cacheinfo', f'160000,{candidate},reference')
        tree = git('write-tree')
        link_commit = git('commit-tree', tree, '-p', candidate, '-m', 'test: raw gitlink fixture')
        observed = current.bridge(root, dict(baseline=candidate, candidate=link_commit, expectedPaths=['reference']))
        assert observed['blockers'] == ['EVIDENCE_BOUNDARY_GITLINK']
        assert observed['diff'][0]['destinationMode'] == '160000'
    finally:
        index.write_bytes(original)
    assert index.read_bytes() == original


def test_workflow_tree_and_render_parity(surfaces):
    root, binding = surfaces
    assert workflows(root, binding)['result'] == 'PASS'
    path = root / current.WORKFLOWS[0]
    original = path.read_bytes()
    try:
        path.write_bytes(original.replace(b'record `not-applicable` distinctly.', b'record `not-applicable` distinctly. Drift.'))
        assert 'EVIDENCE_WORKFLOW_PARITY_DRIFT' in workflows(root, binding)['blockers']
    finally:
        path.write_bytes(original)
    assert path.read_bytes() == original


def test_not_applicable_is_not_pass():
    observed = current.finalize(current.empty_result('a' * 40), applicable=False)
    assert observed['result'] == 'not-applicable'
    assert observed['assertionLedger'] == []
    assert current.finalize(current.empty_result('a' * 40), applicable=True)['blockers'] == ['SCOPE_NOT_EVALUATED']


def test_not_applicable_is_not_pass_missing_history(git_repository):
    root, before, candidate = git_repository
    observed = current.bridge(root, dict(baseline='f' * 40, candidate=candidate))
    assert observed['result'] == 'BLOCKED'
    assert any(row['state'] == 'SKIPPED' for row in observed['assertionLedger'])


@pytest.mark.parametrize('state,exit_code,allowed', [
    ('PASS',0,True),('FAIL',1,False),('BLOCKED',2,False),('not-applicable',0,True),
    ('PASS',1,False),('FAIL',0,False),('BLOCKED',0,False),('not-applicable',1,False),
])
def test_lifecycle_transition_semantics(surfaces,state,exit_code,allowed):
    root, binding = surfaces
    result = workflows(root, binding)
    assert result['result'] == 'PASS'
    document = current.empty_result('a' * 40)
    document['result'] = state
    if state == 'PASS':
        document['assertionLedger'] = [dict(id='executed',subject='executed',state='PASS')]
    if state == 'FAIL':
        document['blockers'] = ['EVIDENCE_HASH_MISMATCH']
        document['assertionLedger'] = [dict(id='executed',subject='executed',state='FAIL')]
    if state == 'BLOCKED':
        document['blockers'] = ['HISTORY_UNAVAILABLE']
        document['assertionLedger'] = [dict(id='executed',subject='executed',state='SKIPPED')]
    for path in current.WORKFLOWS:
        decision = current.transition_decision(document,exit_code)
        assert decision['allowed'] is allowed
        assert decision['recordedResult'] == state
        if exit_code != {'PASS':0,'FAIL':1,'BLOCKED':2,'not-applicable':0}[state]:
            assert 'EVIDENCE_RESULT_EXIT_MISMATCH' in decision['blockers']
        content = (root / path).read_text()
        block = content[content.index(current.BEGIN):content.index(current.END)]
        for clause in ('`FAIL`/`1`', '`BLOCKED`/`2`', 'halt review, completion, and unattended finalization',
                       '`not-applicable`/`0`', 'record `not-applicable` distinctly', 'never write `review` or `done`'):
            assert clause in block
    for scenario in completion.SCENARIOS:
        assert completion.evaluate(root, 'a' * 40, {'id':scenario})[0] == []


@pytest.mark.parametrize('fault,expected', [
    ('remove', 'EVIDENCE_GATE_NOT_USED'), ('malformed', 'EVIDENCE_GATE_NOT_USED'),
    ('displace', 'EVIDENCE_GATE_DISPLACED'), ('retired', 'EVIDENCE_ALIAS_ROUTE_BROKEN'),
    ('guidance', 'EVIDENCE_GUIDANCE_NOT_BOUND'),
])
def test_invocation_removal_and_displacement(surfaces, fault, expected, record_property):
    root, binding = surfaces
    path = root / current.WORKFLOWS[0]
    original = path.read_bytes()
    guidance = root / current.GUIDANCE[0]
    original_guidance = guidance.read_bytes()
    retired = root / current.RETIRED[0]
    try:
        content = original.decode()
        start = content.index(current.BEGIN); end = content.index(current.END) + len(current.END)
        if fault == 'remove': path.write_text(content[:start]+content[end:])
        if fault == 'malformed': path.write_text(content.replace(current.END, '<!-- malformed -->'))
        if fault == 'displace': path.write_text(content[:start]+content[end:]+'\n'+content[start:end])
        if fault == 'retired': retired.mkdir(); (retired/'SKILL.md').write_text('forked retired alias')
        if fault == 'guidance': guidance.write_text('[workflow]\npersistent_facts = []\n')
        observed = workflows(root, binding)
        record_property('story103_observed_blockers', json.dumps(observed['blockers']))
        record_property('story103_expected_code', expected)
        assert expected in observed['blockers']
    finally:
        path.write_bytes(original); guidance.write_bytes(original_guidance)
        if retired.exists(): shutil.rmtree(retired)
    record_property('story103_before_sha256', hashlib.sha256(original + original_guidance).hexdigest())
    record_property('story103_after_sha256', hashlib.sha256(path.read_bytes() + guidance.read_bytes()).hexdigest())
    assert path.read_bytes() == original
    assert guidance.read_bytes() == original_guidance
    assert not retired.exists()


def test_current_cli_preserves_historical_verify_callable():
    import verify_evidence_boundary as historical
    assert callable(historical.verify)
    assert current.WORKFLOWS and len(current.WORKFLOWS) == 10
    binding = json.loads((ROOT / current.BINDING).read_bytes())
    assert binding['baseline'] == current.BASELINE
    assert binding['exemptions'] == []
    assert len(binding['workflowInventory']['rows']) == 10


def test_current_missing_workflow_is_gate_failure(surfaces):
    root, binding = surfaces
    (root / current.WORKFLOWS[0]).unlink()
    assert 'EVIDENCE_GATE_NOT_USED' in workflows(root, binding)['blockers']


def test_current_helper_integrity_fault_is_fail(monkeypatch, tmp_path):
    binding = tmp_path / current.BINDING
    binding.parent.mkdir(parents=True)
    binding.write_text('{}')
    def fault(*args, **kwargs):
        raise RuntimeError('HELPER_FAILURE')
    monkeypatch.setattr(current, 'bridge', fault)
    observed = current.verify(tmp_path, current.BASELINE, 'HEAD', authority_bundle='fixture')
    assert observed['result'] == 'FAIL'
    assert observed['blockers'] == ['HELPER_FAILURE']


@pytest.mark.parametrize('fault,expected', [('comment-spoof','EVIDENCE_READER_NOT_SHARED'),('assertion-removal','EVIDENCE_READER_STRENGTH_REDUCED')])
def test_stable_code_contract_changed_reader_requires_real_helpers(tmp_path,fault,expected,record_property):
    root = tmp_path
    def git(*args):
        return subprocess.run(['git','-C',str(root),*args],check=True,capture_output=True).stdout.decode().strip()
    git('init','-q'); git('config','user.name','Fixture'); git('config','user.email','fixture@example.invalid')
    source = root/'tests/Reader.cs'; source.parent.mkdir()
    original = b'''class Reader { const string Trusted = "pinned";
    void Check() { GitFacts git = new(); EvidenceManifest manifest = new(); AssertionLedger ledger = new();
    BoundaryAssertions.True(ledger, "identity", true); } }'''
    source.write_bytes(original); git('add','.'); git('commit','-qm','test: establish reader')
    before = git('rev-parse','HEAD')
    if fault == 'comment-spoof':
        source.write_bytes(b'// GitFacts git = new(); EvidenceManifest manifest = new(); AssertionLedger ledger = new(); BoundaryAssertions.True\nclass Reader { const string Trusted = "pinned"; }')
    else:
        source.write_bytes(original.replace(b'BoundaryAssertions.True(ledger, "identity", true);',b'BoundaryAssertions.NoGitlinks(ledger, "identity", diff);'))
    git('add','.'); git('commit','-qm','test: inject reader fault')
    observed = current.bridge(root,dict(baseline=before,candidate=git('rev-parse','HEAD'),readerPaths=['tests/Reader.cs']))
    record_property('story103_observed_blockers',json.dumps(observed['blockers']))
    record_property('story103_expected_code',expected)
    source.write_bytes(original)
    record_property('story103_before_sha256',hashlib.sha256(original).hexdigest())
    record_property('story103_after_sha256',hashlib.sha256(source.read_bytes()).hexdigest())
    assert expected in observed['blockers']
    assert source.read_bytes() == original
