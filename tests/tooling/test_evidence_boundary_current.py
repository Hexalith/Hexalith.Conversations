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
    # 10.3 is retained evidence: later project overrides must not rewrite its fixture.
    for row in binding['guidanceInventory']['rows']:
        path = tmp_path / row['path']
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(subprocess.run(['git', '-C', str(ROOT), 'show', current.BASELINE + ':' + row['path']],
            check=True, capture_output=True).stdout)
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
        expected_codes = {expected}
        assert observed['blockers'] == sorted(expected_codes)
        assert observed['result'] != 'PASS'
    finally:
        (root / 'evidence.txt').write_bytes(original)
        (root / 'roots.cs').unlink(missing_ok=True)
    record_property('story103_before_sha256', hashlib.sha256(original).hexdigest())
    record_property('story103_after_sha256', hashlib.sha256((root / 'evidence.txt').read_bytes()).hexdigest())
    assert (root / 'evidence.txt').read_bytes() == original


def test_stable_code_contract_raw_gitlink_is_never_filtered(git_repository,record_property):
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
    record_property('story103_expected_code','EVIDENCE_BOUNDARY_GITLINK')
    record_property('story103_observed_blockers',json.dumps(observed['blockers']))
    record_property('story103_before_sha256',hashlib.sha256(original).hexdigest())
    record_property('story103_after_sha256',hashlib.sha256(index.read_bytes()).hexdigest())


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
        expected_codes = {('PASS',0):[],('PASS',1):['EVIDENCE_RESULT_EXIT_MISMATCH'],
            ('FAIL',1):['EVIDENCE_HASH_MISMATCH'],('FAIL',0):['EVIDENCE_HASH_MISMATCH','EVIDENCE_RESULT_EXIT_MISMATCH'],
            ('BLOCKED',2):['HISTORY_UNAVAILABLE'],('BLOCKED',0):['EVIDENCE_RESULT_EXIT_MISMATCH','HISTORY_UNAVAILABLE'],
            ('not-applicable',0):[],('not-applicable',1):['EVIDENCE_RESULT_EXIT_MISMATCH']}
        assert decision['blockers'] == expected_codes[(state,exit_code)]
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
        expected_codes = {
            'remove':['EVIDENCE_GATE_NOT_USED','EVIDENCE_WORKFLOW_PARITY_DRIFT'],
            'malformed':['EVIDENCE_GATE_NOT_USED','EVIDENCE_WORKFLOW_PARITY_DRIFT'],
            'displace':['EVIDENCE_GATE_DISPLACED','EVIDENCE_WORKFLOW_PARITY_DRIFT'],
            'retired':['EVIDENCE_ALIAS_ROUTE_BROKEN'],
            'guidance':['EVIDENCE_GUIDANCE_NOT_BOUND'],
        }
        assert observed['blockers'] == expected_codes[fault]
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
    BoundaryAssertions.True(ledger, "identity", valid); } }'''
    source.write_bytes(original); git('add','.'); git('commit','-qm','test: establish reader')
    before = git('rev-parse','HEAD')
    if fault == 'comment-spoof':
        source.write_bytes(b'// GitFacts git = new(); EvidenceManifest manifest = new(); AssertionLedger ledger = new(); BoundaryAssertions.True\nclass Reader { const string Trusted = "pinned"; }')
    else:
        source.write_bytes(original.replace(b'BoundaryAssertions.True(ledger, "identity", valid);',b'BoundaryAssertions.NoGitlinks(ledger, "identity", diff);'))
    git('add','.'); git('commit','-qm','test: inject reader fault')
    observed = current.bridge(root,dict(baseline=before,candidate=git('rev-parse','HEAD'),readerPaths=['tests/Reader.cs']))
    record_property('story103_observed_blockers',json.dumps(observed['blockers']))
    record_property('story103_expected_code',expected)
    source.write_bytes(original)
    record_property('story103_before_sha256',hashlib.sha256(original).hexdigest())
    record_property('story103_after_sha256',hashlib.sha256(source.read_bytes()).hexdigest())
    assert observed['blockers'] == sorted({expected,'EVIDENCE_READER_STRENGTH_REDUCED'} if fault == 'comment-spoof' else {expected})
    assert source.read_bytes() == original


@pytest.mark.parametrize('code,state,exit_code', [('HELPER_UNAVAILABLE','BLOCKED',2),('HELPER_FAILURE','FAIL',1)])
def test_current_cli_output_preserves_helper_failure_json(tmp_path,monkeypatch,capsys,code,state,exit_code):
    def fault(*args,**kwargs): raise RuntimeError(code)
    monkeypatch.setattr(current,'bridge',fault)
    observed_exit=current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,
        '--authority-bundle','authority.json','--output','result.json'])
    observed=json.loads(capsys.readouterr().out)
    assert observed_exit == exit_code
    assert observed['result'] == state
    assert observed['blockers'] == [code]
    assert not (tmp_path/'result.json').exists()


def test_stable_code_contract_output_write_failure_is_schema_valid_blocked(tmp_path,monkeypatch,capsys):
    success=current.empty_result('a'*40)
    success['result']='PASS';success['assertionLedger']=[dict(id='evaluated',subject='fixture',state='PASS')]
    monkeypatch.setattr(current,'verify',lambda *args,**kwargs:copy.deepcopy(success))
    monkeypatch.setattr(current,'bridge',lambda *args,**kwargs:dict(headPaths=[],indexPaths=[],containedPaths=[dict(code=None,fullPath=str(tmp_path/'result.json'))]))
    def unavailable(*args,**kwargs): raise OSError('write unavailable')
    monkeypatch.setattr(Path,'write_bytes',unavailable)
    observed_exit=current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,
        '--authority-bundle','authority.json','--output','result.json'])
    observed=json.loads(capsys.readouterr().out)
    assert observed_exit == 2
    assert observed['result']=='BLOCKED'
    assert observed['blockers']==['OUTPUT_WRITE_FAILED']
    assert any(row['state']=='SKIPPED' for row in observed['assertionLedger'])


@pytest.mark.parametrize('fault', ['blockers','failed-row','skipped-row','unknown-code','non-object'])
def test_lifecycle_transition_semantics_rejects_forged_not_applicable(fault):
    document=current.empty_result('a'*40)
    if fault=='blockers':document['blockers']=['EVIDENCE_HASH_MISMATCH']
    if fault=='failed-row':document['assertionLedger']=[dict(id='forged',subject='forged',state='FAIL')]
    if fault=='skipped-row':document['assertionLedger']=[dict(id='forged',subject='forged',state='SKIPPED')]
    if fault=='unknown-code':document['warnings']=['UNKNOWN_CODE']
    if fault=='non-object':document=[]
    for path in current.WORKFLOWS:
        observed=current.transition_decision(document,0)
        assert not observed['allowed'],path
        expected=['EVIDENCE_RESULT_EXIT_MISMATCH'] if fault!='blockers' else ['EVIDENCE_HASH_MISMATCH','EVIDENCE_RESULT_EXIT_MISMATCH']
        assert observed['blockers']==expected


@pytest.mark.parametrize('fault', ['workflow-digest','guidance-digest','workflow-row','guidance-row',
    'omitted-source','unreviewed-source','unreviewed-diff','duplicate-diff'])
def test_current_binding_inventory_and_source_scope_faults_fail_closed(tmp_path,monkeypatch,capsys,fault):
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    approved=list(binding['sourcePaths'])
    scope_fault=fault in ('omitted-source','unreviewed-source','unreviewed-diff','duplicate-diff')
    if fault=='workflow-digest':binding['workflowInventory']['sha256']='0'*64
    if fault=='guidance-digest':binding['guidanceInventory']['sha256']='0'*64
    if fault=='workflow-row':binding['workflowInventory']['rows'].pop()
    if fault=='guidance-row':binding['guidanceInventory']['rows'].pop()
    if fault=='omitted-source':binding['sourcePaths'].remove(current.CURRENT_PROVENANCE)
    if fault=='unreviewed-source':binding['sourcePaths']=sorted([*approved,'unreviewed.txt'])
    if fault=='unreviewed-diff':binding['expectedChangedPaths']=sorted([*binding['expectedChangedPaths'],'unreviewed.txt'])
    if fault=='duplicate-diff':binding['expectedChangedPaths'].append(binding['expectedChangedPaths'][0])
    binding_file=tmp_path/current.BINDING;binding_file.parent.mkdir(parents=True);binding_file.write_text(json.dumps(binding))
    authority=tmp_path/binding['authorityBundle']['path'];authority.write_text('{}')
    initial=dict(result='PASS',candidate='a'*40,evidenceReaders=[],blobs=[dict(path=path,content=None) for path in (
        'docs/release-evidence/story-10.3-final-record-v2.json','docs/release-evidence/story-10.3-final-record-v2.md')],
        containedPaths=[dict(path=current.BINDING,fullPath=str(binding_file),code=None),dict(path=binding['authorityBundle']['path'],fullPath=str(authority),code=None)],
        diff=[dict(path='ordinary.txt' if scope_fault else '_bmad/scripts/changed.py',sourcePath=None,sourceMode='100644',destinationMode='100644')],assertionLedger=[],blockers=[])
    def facts(root,request):
        if 'inspectChangedReaders' in request:return copy.deepcopy(initial)
        if request.get('candidate') == current.PROVENANCE_BASELINE:
            return dict(result='PASS',blobs=[dict(path=row['path'],sha256=row['sha256'],code=None)
                for row in binding['provenance'] if row['path'] in current.HISTORICAL_PROVENANCE],assertionLedger=[],blockers=[])
        return dict(result='PASS',blobs=[],assertionLedger=[dict(id='neutral',subject='neutral',state='PASS')],blockers=[])
    monkeypatch.setattr(current,'bridge',facts)
    monkeypatch.setattr(current,'workflow_checks',lambda *args:None)
    monkeypatch.setattr(current,'approved_roots',lambda *args:binding['rootConstants'])
    monkeypatch.setattr(current,'approved_source_paths',lambda *_args:approved)
    monkeypatch.setattr(current,'approved_withdrawal',lambda *_args:binding['withdrawnPublication'])
    import generate_story_record as generator
    monkeypatch.setattr(generator,'v2_verified_predecessor',lambda *args:binding['predecessor']['sha256'])
    exit_code=current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--candidate','HEAD',
        '--authority-bundle',binding['authorityBundle']['path']])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==1
    assert observed['result']=='FAIL'
    assert observed['blockers']==['AUTHORITY_BINDING_INVALID']
    if scope_fault:
        assert observed['changedPaths']==['ordinary.txt']
        assert dict(id='binding:source-paths',subject=current.BINDING,state='FAIL') in observed['assertionLedger']
        assert dict(id='neutral',subject='neutral',state='PASS') in observed['assertionLedger']
        assert observed['warnings']==[]


@pytest.mark.parametrize('reader', [True,False])
def test_current_reader_only_candidate_is_inspected_before_applicability(tmp_path,reader):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    subprocess.run(['git','-C',str(root),'config','user.email','fixture@example.invalid'],check=True)
    subprocess.run(['git','-C',str(root),'config','user.name','Fixture'],check=True)
    subprocess.run(['git','-C',str(root),'checkout','--quiet',current.BASELINE],check=True,capture_output=True)
    path=root/'tests/SomeEvidenceReader.cs'
    source='class Reader { void Read() { File.ReadAllText("docs/release-evidence/signed.json"); } }' if reader else '// EvidenceManifest docs/release-evidence/fake.json\nclass Ordinary { int Value = 1; }'
    path.write_text(source)
    subprocess.run(['git','-C',str(root),'add','tests/SomeEvidenceReader.cs'],check=True)
    subprocess.run(['git','-C',str(root),'commit','--quiet','-m','test: reader-only candidate'],check=True,capture_output=True)
    (root/current.BINDING).write_bytes((ROOT/current.BINDING).read_bytes())
    observed=current.verify(root,current.BASELINE,'HEAD',authority_bundle='_bmad-output/planning-artifacts/v9-authority-bundle-v1.json')
    if reader:
        assert observed['result']=='FAIL'
        assert 'EVIDENCE_READER_NOT_SHARED' in observed['blockers']
        assert any(row['id'].startswith('reader:') for row in observed['assertionLedger'])
    else:
        assert observed['result']=='not-applicable'
        assert observed['assertionLedger']==[]


def test_current_cli_baseline_override_cannot_erase_scope(tmp_path,capsys):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    (root/'ordinary.txt').write_text('post-entry ordinary change\n')
    subprocess.run(['git','-C',str(root),'add','ordinary.txt'],check=True)
    subprocess.run(['git','-C',str(root),'-c','user.name=Fixture','-c','user.email=fixture@example.invalid','commit','--quiet','-m','test: advance beyond approved entry'],check=True,capture_output=True)
    (root/current.BINDING).write_bytes((ROOT/current.BINDING).read_bytes())
    observed_exit=current.main(['--repository',str(root),'--baseline','HEAD','--candidate','HEAD',
        '--authority-bundle','_bmad-output/planning-artifacts/v9-authority-bundle-v1.json'])
    observed=json.loads(capsys.readouterr().out)
    assert observed_exit == 1
    assert observed['result'] == 'FAIL'
    assert observed['blockers'] == ['AUTHORITY_BINDING_INVALID']
    assert observed['assertionLedger'] == [dict(id='binding:baseline',subject=current.BINDING,state='FAIL')]


def test_current_historical_provenance_cannot_be_rebound_to_mutated_bytes(tmp_path):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    subprocess.run(['git','-C',str(root),'config','user.email','fixture@example.invalid'],check=True)
    subprocess.run(['git','-C',str(root),'config','user.name','Fixture'],check=True)
    path=current.HISTORICAL_PROVENANCE[2]
    target=root/path
    original=target.read_bytes()
    target.write_bytes(original+b' ')
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    for row in binding['provenance']:
        if row['path']==path:row['sha256']=current.digest(target.read_bytes())
    binding['expectedChangedPaths']=sorted([current.BINDING,path])
    (root/current.BINDING).write_text(json.dumps(binding,indent=2)+'\n')
    subprocess.run(['git','-C',str(root),'add','--',current.BINDING,path],check=True)
    subprocess.run(['git','-C',str(root),'commit','--quiet','-m','test: rebind altered historical provenance'],check=True,capture_output=True)
    observed=current.verify(root,current.BASELINE,'HEAD',authority_bundle=binding['authorityBundle']['path'],verify_workflows=True)
    assert observed['result']=='FAIL'
    assert 'EVIDENCE_HASH_MISMATCH' in observed['blockers']
    assert dict(id='provenance:baseline:'+path,subject=path,state='FAIL') in observed['assertionLedger']
    assert next(row['sha256'] for row in binding['provenance'] if row['path']==path)==current.digest(target.read_bytes())


def test_current_historical_provenance_unavailable_is_blocked(tmp_path,monkeypatch):
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    monkeypatch.setattr(current,'bridge',lambda *_args:dict(result='PASS',blobs=[],assertionLedger=[],blockers=[]))
    observed=current.historical_provenance_result(tmp_path,binding['provenance'])
    assert observed['result']=='BLOCKED'
    assert observed['blockers']==['AUTHORITY_UNAVAILABLE']
    assert any(row['state']=='SKIPPED' for row in observed['assertionLedger'])


def reader_fixture(root, old_source, new_source):
    def git(*args):
        return subprocess.run(['git','-C',str(root),*args],check=True,capture_output=True,text=True).stdout.strip()
    git('init','-q');git('config','user.name','Fixture');git('config','user.email','fixture@example.invalid')
    path=root/'tests/Reader.cs';path.parent.mkdir()
    if old_source is not None:path.write_text(old_source)
    else:(root/'ordinary.txt').write_text('baseline')
    git('add','.');git('commit','-qm','test: establish reader fixture');before=git('rev-parse','HEAD')
    if new_source is None:path.unlink()
    else:path.write_text(new_source)
    git('add','.');git('commit','-qm','test: change reader fixture')
    return git,before,git('rev-parse','HEAD')


@pytest.mark.parametrize('expression', [
    '"docs/release-evidence/signed.json"', '@"docs\\release-evidence\\signed.json"',
    '"""docs/release-evidence/signed.json"""', '$"docs/release-evidence/{name}.json"',
    '$@"docs\\release-evidence\\{name}.json"', '$$"""docs/release-evidence/{{name}}.json"""',
    '"docs\\\\release-evidence\\\\signed.json"', 'Path.Combine("docs", "release-evidence", "signed.json")',
])
def test_current_reader_literal_forms_are_inspected(tmp_path,expression):
    source='class Reader { void Read() { File.ReadAllText('+expression+'); } }'
    _,before,candidate=reader_fixture(tmp_path,None,source)
    observed=current.bridge(tmp_path,dict(baseline=before,candidate=candidate,inspectChangedReaders=True))
    assert observed['evidenceReaders']==['tests/Reader.cs']
    assert observed['blockers']==['EVIDENCE_READER_NOT_SHARED']


@pytest.mark.parametrize('construction', ['GitFacts git = new(); EvidenceManifest manifest = new(); AssertionLedger ledger = new();',
    'var git = new GitFacts(); var manifest = new EvidenceManifest(); var ledger = new AssertionLedger();'])
def test_current_shared_reader_construction_and_meaningful_predicate_pass(tmp_path,construction):
    source='class Reader { void Read() { '+construction+' BoundaryAssertions.True(ledger, "stable-id", measured); } }'
    _,before,candidate=reader_fixture(tmp_path,None,source)
    observed=current.bridge(tmp_path,dict(baseline=before,candidate=candidate,inspectChangedReaders=True))
    assert observed['result']=='PASS'
    assert observed['blockers']==[]


@pytest.mark.parametrize('change', ['identity','true','parenthesized-true','meaningful','delete'])
def test_current_reader_assertion_identity_and_predicate_strength(tmp_path,change):
    original='class Reader { void Read() { var git = new GitFacts(); var manifest = new EvidenceManifest(); var ledger = new AssertionLedger(); BoundaryAssertions.True(ledger, "stable-id", measured); } }'
    changed=original
    if change=='identity':changed=original.replace('stable-id','different-id')
    if change=='true':changed=original.replace('measured','true')
    if change=='parenthesized-true':changed=original.replace('measured','(true)')
    if change=='meaningful':changed=original.replace('measured','otherMeasured')
    if change=='delete':changed=None
    _,before,candidate=reader_fixture(tmp_path,original,changed)
    observed=current.bridge(tmp_path,dict(baseline=before,candidate=candidate,inspectChangedReaders=True))
    expected=[] if change=='meaningful' else ['EVIDENCE_READER_STRENGTH_REDUCED']
    if change=='delete':expected=['EVIDENCE_READER_NOT_SHARED','EVIDENCE_READER_STRENGTH_REDUCED']
    assert observed['blockers']==expected
    assert not any(row['state']=='SKIPPED' for row in observed['assertionLedger'])


@pytest.mark.parametrize('failure', ['compiler','missing-tool','request','malformed-envelope'])
def test_current_bridge_preserves_helper_failure_classification(tmp_path,monkeypatch,failure):
    calls=[]
    def run(*args,**kwargs):
        calls.append(args[0])
        if failure=='missing-tool':raise FileNotFoundError('dotnet')
        if len(calls)==1:return subprocess.CompletedProcess(args[0],1 if failure=='compiler' else 0,b'error CS1002' if failure=='compiler' else b'',b'')
        return subprocess.CompletedProcess(args[0],1,json.dumps(dict(result='FAIL',blockers=['HELPER_REQUEST_INVALID'])).encode() if failure=='request' else b'broken',b'')
    monkeypatch.setattr(current.subprocess,'run',run)
    code={'compiler':'HELPER_FAILURE','missing-tool':'HELPER_UNAVAILABLE','request':'HELPER_REQUEST_INVALID','malformed-envelope':'HELPER_FAILURE'}[failure]
    observed=current.verify(tmp_path,current.BASELINE,'HEAD',authority_bundle='authority.json')
    assert observed['blockers']==[code]
    assert observed['result']==('BLOCKED' if failure=='missing-tool' else 'FAIL')


@pytest.mark.parametrize('output', [current.BINDING,'authority.json','tracked.cs','.git/config','.git/index','alias.json','metadata-alias.json'])
def test_current_output_collision_preserves_protected_bytes(tmp_path,monkeypatch,capsys,output):
    subprocess.run(['git','init','--quiet',str(tmp_path)],check=True)
    for path in (current.BINDING,'authority.json','tracked.cs'):
        target=tmp_path/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text('protected')
    (tmp_path/'alias.json').hardlink_to(tmp_path/'tracked.cs')
    (tmp_path/'metadata-alias.json').hardlink_to(tmp_path/'.git/config')
    target=tmp_path/output
    if not target.exists():target.write_bytes(b'disposable-index-fixture')
    original=target.read_bytes()
    document=current.empty_result('a'*40);document.update(result='PASS',assertionLedger=[dict(id='scope',subject='evaluated',state='PASS')])
    monkeypatch.setattr(current,'verify',lambda *_args,**_kwargs:copy.deepcopy(document))
    monkeypatch.setattr(current,'bridge',lambda *_args:dict(result='PASS',treePaths=['tracked.cs'],headPaths=[],indexPaths=[],containedPaths=[dict(code=None,fullPath=str(target))]))
    exit_code=current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--authority-bundle','authority.json','--output',output])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==1
    assert observed['result']=='FAIL'
    assert observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
    assert target.read_bytes()==original
    assert (tmp_path/'tracked.cs').read_bytes()==b'protected'


def test_current_public_cli_dispatch_emits_closed_scoped_failure(tmp_path):
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(tmp_path/'checkout')],check=True,capture_output=True)
    run=subprocess.run([sys.executable,str(ROOT/'_bmad/scripts/verify_evidence_boundary.py'),'--repository',str(tmp_path/'checkout'),
        '--baseline','HEAD','--candidate','HEAD','--authority-bundle','_bmad-output/planning-artifacts/v9-authority-bundle-v1.json','--format','json'],capture_output=True,text=True)
    observed=json.loads(run.stdout)
    assert run.returncode==1 and observed['result']=='FAIL'
    assert observed['blockers']==['AUTHORITY_BINDING_INVALID']
    assert not current.transition_decision(observed,run.returncode)['allowed']


def test_current_result_schema_failure_keeps_raw_filename_and_blocks_transition():
    document=current.empty_result('a'*40)
    document.update(result='PASS',changedPaths=['tests/line\nbreak.cs'],assertionLedger=[dict(id='scope',subject='measured',state='PASS')])
    observed=current.finalize(document,applicable=True)
    assert observed['result']=='FAIL' and observed['blockers']==['RESULT_SCHEMA_INVALID']
    raw=json.loads(observed['assertionLedger'][0]['subject'])
    assert raw['changedPaths']==['tests/line\nbreak.cs']
    assert not current.transition_decision(observed,1)['allowed']


def test_stable_code_contract_unavailable_history_cannot_pass():
    document=current.empty_result('a'*40)
    document.update(blockers=['UNAVAILABLE_HISTORY_PASSED'],assertionLedger=[dict(id='history',subject='missing authority',state='SKIPPED'),dict(id='history',subject='missing authority',state='PASS')])
    observed=current.finalize(document,applicable=True)
    assert observed['result']=='FAIL' and observed['blockers']==['UNAVAILABLE_HISTORY_PASSED']


def test_current_approved_roots_are_derived_from_verified_baseline():
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    assert current.approved_roots(ROOT)==binding['rootConstants']


@pytest.mark.parametrize('pin', ['source','digest','candidate'])
def test_current_roots_rebinding_is_rejected_independently(tmp_path,pin):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    if pin=='source':
        binding['rootConstants']['sourcePath']='forged-roots.cs'
        (root/'forged-roots.cs').write_bytes((root/current.CURRENT_PROVENANCE).read_bytes())
        binding['expectedChangedPaths']=sorted([current.BINDING,'forged-roots.cs'])
    else:
        key='Story102RecordSha256' if pin=='digest' else 'Story102CandidateCommit'
        old=binding['rootConstants']['constants'][key];new='f'*len(old)
        binding['rootConstants']['constants'][key]=new
        source=root/current.CURRENT_PROVENANCE;source.write_bytes(source.read_bytes().replace(old.encode(),new.encode()))
        for row in binding['provenance']:
            if row['path']==current.CURRENT_PROVENANCE:row['sha256']=current.digest(source.read_bytes())
    (root/current.BINDING).write_text(json.dumps(binding,indent=2)+'\n')
    subprocess.run(['git','-C',str(root),'add','--',current.BINDING,current.CURRENT_PROVENANCE,*(['forged-roots.cs'] if pin=='source' else [])],check=True)
    subprocess.run(['git','-C',str(root),'-c','user.name=Fixture','-c','user.email=fixture@example.invalid','commit','--quiet','-m','test: rebind candidate roots'],check=True,capture_output=True)
    observed=current.verify(root,current.BASELINE,'HEAD',authority_bundle=binding['authorityBundle']['path'],verify_workflows=True)
    assert observed['result']=='FAIL' and 'ROOT_OF_TRUST_NOT_PINNED' in observed['blockers']
    assert dict(id='roots:approved',subject=current.CURRENT_PROVENANCE,state='FAIL') in observed['assertionLedger']


def test_current_verifier_carries_canonical_candidate_into_dependent_requests(tmp_path,monkeypatch,capsys):
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    target=tmp_path/current.BINDING;target.parent.mkdir(parents=True);target.write_text(json.dumps(binding))
    authority=tmp_path/binding['authorityBundle']['path'];authority.write_text('{}')
    requests=[]
    def bridge(root,request):
        requests.append(copy.deepcopy(request))
        if request.get('includeTreePaths'):
            return dict(result='PASS',treePaths=[],headPaths=[],indexPaths=[],containedPaths=[dict(code=None,fullPath=str(tmp_path/'result.json'))])
        if request.get('inspectChangedReaders'):
            return dict(result='PASS',candidate='a'*40,baseline=current.BASELINE,evidenceReaders=[],
                containedPaths=[dict(code=None,fullPath=str(target)),dict(code=None,fullPath=str(authority))],
                blobs=[dict(path=path,content=None) for path in ('docs/release-evidence/story-10.3-final-record-v2.json','docs/release-evidence/story-10.3-final-record-v2.md')],
                diff=[dict(path='_bmad/scripts/changed.py',sourcePath=None,sourceMode='100644',destinationMode='100644')]
                    +[dict(path=path,sourcePath=None,sourceMode='100644',destinationMode='000000') for path in current.PUBLICATION_PATHS],assertionLedger=[],blockers=[])
        return dict(result='PASS',blobs=[],assertionLedger=[dict(id='measured',subject='fixture',state='PASS')],blockers=[])
    monkeypatch.setattr(current,'bridge',bridge)
    monkeypatch.setattr(current,'historical_provenance_result',lambda *_args:dict(result='PASS',assertionLedger=[],blockers=[]))
    monkeypatch.setattr(current,'approved_roots',lambda *_args:binding['rootConstants'])
    monkeypatch.setattr(current,'approved_source_paths',lambda *_args:binding['sourcePaths'])
    monkeypatch.setattr(current,'approved_withdrawal',lambda *_args:binding['withdrawnPublication'])
    monkeypatch.setattr(current,'workflow_checks',lambda *_args:None)
    import generate_story_record as generator
    monkeypatch.setattr(generator,'v2_verified_predecessor',lambda *_args:binding['predecessor']['sha256'])
    assert current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--candidate','HEAD',
        '--authority-bundle',binding['authorityBundle']['path'],'--output','result.json'])==0
    assert json.loads(capsys.readouterr().out)['candidate']=='a'*40
    assert requests[0]['candidate']=='HEAD'
    assert all(request['candidate']=='a'*40 and request['baseline']==current.BASELINE for request in requests[1:])


def test_current_public_cli_invalid_filename_is_schema_valid_fail(tmp_path):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    subprocess.run(['git','-C',str(root),'checkout','--quiet',current.BASELINE],check=True,capture_output=True)
    name='tests/line\nbreak.cs';(root/name).write_text('class Reader { void Read() { File.ReadAllText("docs/release-evidence/signed.json"); } }')
    subprocess.run(['git','-C',str(root),'add','--',name],check=True)
    subprocess.run(['git','-C',str(root),'-c','user.name=Fixture','-c','user.email=fixture@example.invalid','commit','--quiet','-m','test: legal newline path'],check=True,capture_output=True)
    (root/current.BINDING).write_bytes((ROOT/current.BINDING).read_bytes())
    run=subprocess.run([sys.executable,str(ROOT/'_bmad/scripts/verify_evidence_boundary.py'),'--repository',str(root),
        '--baseline',current.BASELINE,'--candidate','HEAD','--authority-bundle','_bmad-output/planning-artifacts/v9-authority-bundle-v1.json','--verify-workflows','--format','json'],capture_output=True,text=True)
    observed=json.loads(run.stdout)
    assert run.returncode==1 and observed['blockers']==['RESULT_SCHEMA_INVALID']
    assert name in json.loads(observed['assertionLedger'][0]['subject'])['changedPaths']


def install_git_fault_wrapper(root,tmp_path,monkeypatch,body):
    wrapper=tmp_path/'tools';wrapper.mkdir()
    actual=shutil.which('git')
    script=wrapper/'git'
    script.write_text('#!'+sys.executable+'\nimport os,sys,subprocess,json\nactual='+repr(actual)+'\nroot='+repr(str(root))+'\n'+body+'\nos.execv(actual,[actual,*sys.argv[1:]])\n')
    script.chmod(0o755)
    import os
    monkeypatch.setenv('PATH',str(wrapper)+os.pathsep+os.environ['PATH'])


@pytest.mark.parametrize('side', ['old','current'])
def test_current_existing_reader_blob_failure_is_skipped_and_blocked(tmp_path,monkeypatch,side):
    root=tmp_path/'repo';root.mkdir()
    original='class Reader { void Read() { var git = new GitFacts(); var manifest = new EvidenceManifest(); var ledger = new AssertionLedger(); BoundaryAssertions.True(ledger,"id",measured); } }'
    git,before,candidate=reader_fixture(root,original,original.replace('measured','otherMeasured'))
    object_id=git('rev-parse',(before if side=='old' else candidate)+':tests/Reader.cs')
    install_git_fault_wrapper(root,tmp_path,monkeypatch,'if os.getcwd()==root and "cat-file" in sys.argv and sys.argv[-1]=='+repr(object_id)+':\n    sys.stderr.write("fixture object unavailable");sys.exit(128)')
    observed=current.bridge(root,dict(baseline=before,candidate=candidate,inspectChangedReaders=True))
    assert observed['result']=='BLOCKED' and observed['blockers']==['HISTORY_UNAVAILABLE']
    assert dict(id='reader:blob:'+side+':tests/Reader.cs',subject='tests/Reader.cs',state='SKIPPED') in observed['assertionLedger']


def test_current_reader_reads_pin_the_first_resolved_head(tmp_path,monkeypatch):
    root=tmp_path/'repo';root.mkdir()
    source='class Reader { void Read() { var git = new GitFacts(); var manifest = new EvidenceManifest(); var ledger = new AssertionLedger(); BoundaryAssertions.True(ledger,"id",measured); } }'
    git,before,candidate=reader_fixture(root,source,source.replace('measured','otherMeasured'))
    (root/'tests/Reader.cs').write_text('class Reader { void Read() { File.ReadAllText("docs/release-evidence/signed.json"); } }')
    git('add','.');git('commit','-qm','test: advance mutable head');advanced=git('rev-parse','HEAD');git('update-ref','HEAD',candidate)
    install_git_fault_wrapper(root,tmp_path,monkeypatch,'if os.getcwd()==root and "rev-parse" in sys.argv and "HEAD^{commit}" in sys.argv:\n    result=subprocess.run([actual,*sys.argv[1:]],capture_output=True);subprocess.run([actual,"update-ref","HEAD",'+repr(advanced)+'],check=True);sys.stdout.buffer.write(result.stdout);sys.stderr.buffer.write(result.stderr);sys.exit(result.returncode)')
    observed=current.bridge(root,dict(baseline=before,candidate='HEAD',inspectChangedReaders=True))
    assert git('rev-parse','HEAD')==advanced
    assert observed['candidate']==candidate and observed['result']=='PASS' and observed['blockers']==[]


def test_current_runner_reports_registered_invalid_request(tmp_path):
    with pytest.raises(RuntimeError,match='^HELPER_REQUEST_INVALID$'):
        current.bridge(tmp_path,dict(baseline='a'*40))


def test_current_cli_argument_failure_is_closed_blocked(tmp_path,capsys):
    exit_code=current.main(['--repository',str(tmp_path)])
    result=json.loads(capsys.readouterr().out)
    assert exit_code==2 and result['result']=='BLOCKED'
    assert result['blockers']==['ARGUMENT_INVALID']


@pytest.mark.parametrize('fault', [None,'omitted-source','unreviewed-source','unreviewed-diff','provenance-baseline',
    'source-inventory-baseline','withdrawal-digest','withdrawal-path','withdrawal-row','partial-withdrawal'])
def test_current_source_inventory_is_pinned_to_entry_independently_of_diff(monkeypatch,fault):
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    approved=json.loads(subprocess.check_output(['git','-C',str(ROOT),'show',current.SOURCE_INVENTORY_BASELINE+':'+current.BINDING]))['expectedChangedPaths']
    withdrawal=copy.deepcopy(binding['withdrawnPublication'])
    assert len(approved)==34 and binding['sourcePaths']==approved
    assert current.BASELINE != current.PROVENANCE_BASELINE
    monkeypatch.setattr(current,'approved_source_paths',lambda *_args:approved)
    monkeypatch.setattr(current,'approved_withdrawal',lambda *_args:withdrawal)
    if fault=='omitted-source':binding['sourcePaths'].remove('tests/Hexalith.Conversations.TestSupport.Runner/Program.cs')
    if fault=='unreviewed-source':binding['sourcePaths']=sorted([*binding['sourcePaths'],'unreviewed.txt'])
    if fault=='unreviewed-diff':binding['expectedChangedPaths']=sorted([*binding['expectedChangedPaths'],'unreviewed.txt'])
    if fault=='provenance-baseline':binding['provenanceBaseline']=current.BASELINE
    if fault=='source-inventory-baseline':binding['sourceInventoryBaseline']=current.BASELINE
    if fault=='withdrawal-digest':binding['withdrawnPublication'][0]['sha256']='f'*64
    if fault=='withdrawal-path':binding['withdrawnPublication'][0]['path']='unreviewed.txt'
    if fault=='withdrawal-row':binding['withdrawnPublication'].pop()
    if fault=='partial-withdrawal':binding['expectedChangedPaths'].remove(current.PUBLICATION_PATHS[0])
    assert current.source_binding_valid(ROOT,binding) is (fault is None)


def test_current_provenance_and_source_inventory_use_independent_approved_commits(monkeypatch):
    import base64
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    requests=[]
    def facts(repository,request):
        requests.append(copy.deepcopy(request))
        blobs=[]
        for path in request['blobPaths']:
            content=subprocess.check_output(['git','-C',str(ROOT),'show',request['candidate']+':'+path])
            blobs.append(dict(path=path,content=base64.b64encode(content).decode(),sha256=current.digest(content),code=None))
        return dict(result='PASS',blobs=blobs,assertionLedger=[],blockers=[])
    monkeypatch.setattr(current,'bridge',facts)
    assert current.historical_provenance_result(ROOT,binding['provenance'])['result']=='PASS'
    assert current.approved_source_paths(ROOT)==binding['sourcePaths']
    assert current.approved_withdrawal(ROOT)==binding['withdrawnPublication']
    assert requests[0]['baseline']==requests[0]['candidate']==current.PROVENANCE_BASELINE
    assert requests[1]['baseline']==requests[1]['candidate']==current.SOURCE_INVENTORY_BASELINE
    assert requests[2]['baseline']==requests[2]['candidate']==current.BASELINE
    assert requests[2]['blobPaths']==list(current.PUBLICATION_PATHS)


@pytest.mark.parametrize('fault', [None,'missing','edit','rename','gitlink','duplicate','extra'])
def test_current_withdrawal_requires_nine_exact_deletions(fault):
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    rows=[dict(path=path,sourcePath=None,sourceMode='100644',destinationMode='000000') for path in current.PUBLICATION_PATHS]
    if fault=='missing':rows.pop()
    if fault=='edit':rows[0]['destinationMode']='100644'
    if fault=='rename':rows[0]['sourcePath']=rows[0]['path'];rows[0]['path']='renamed.json'
    if fault=='gitlink':rows[0]['sourceMode']='160000'
    if fault=='duplicate':rows.append(copy.deepcopy(rows[0]))
    if fault=='extra':rows.append(dict(path='other.json',sourcePath=current.PUBLICATION_PATHS[0],sourceMode='100644',destinationMode='100644'))
    assert current.withdrawal_valid(binding,rows) is (fault is None)


def withdrawn_source_repository(tmp_path, *, edited_xml=False):
    root=tmp_path/'checkout'
    subprocess.run(['git','clone','--quiet','--local','--no-hardlinks',str(ROOT),str(root)],check=True,capture_output=True)
    subprocess.run(['git','-C',str(root),'checkout','--quiet',current.BASELINE],check=True,capture_output=True)
    binding=json.loads((ROOT/current.BINDING).read_bytes())
    # Reproduce the accepted 10.3 source, independent of later shared-helper and
    # project-guidance changes. The live checker still evaluates this fixture.
    source_candidate=json.loads((ROOT/'docs/release-evidence/story-10.3-final-record-v2.json').read_bytes())['candidate']['commit']
    for path in binding['sourcePaths']:
        (root/path).write_bytes(subprocess.check_output(['git','-C',str(ROOT),'show',source_candidate+':'+path]))
    import generate_story_record as generator
    spec=root/generator.V2_10_3_SPEC
    source=spec.read_bytes()
    begin=source.find(generator.RECORD_BEGIN_MARKER.encode())
    if begin>=0:
        end=source.index(generator.RECORD_END_MARKER.encode(),begin)+len(generator.RECORD_END_MARKER.encode())
        spec.write_bytes(source[:begin]+source[end:])
    for path in current.PUBLICATION_PATHS:(root/path).unlink()
    if edited_xml:
        path='artifacts/v9/10.3/AC-10.3-02.xml'
        original=subprocess.check_output(['git','-C',str(root),'show',current.BASELINE+':'+path])
        (root/path).write_bytes(original+b'\n<!-- edited withdrawal -->\n')
    subprocess.run(['git','-C',str(root),'add','--all','--',*binding['expectedChangedPaths']],check=True)
    changed=subprocess.check_output(['git','-C',str(root),'diff','--cached','--name-only'],text=True).splitlines()
    assert changed==binding['expectedChangedPaths']
    subprocess.run(['git','-C',str(root),'-c','user.name=Fixture','-c','user.email=fixture@example.invalid',
        'commit','--quiet','-m','test: exercise approved evidence refresh'],check=True,capture_output=True)
    candidate=subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD'],text=True).strip()
    return root,binding,candidate


@pytest.mark.parametrize('receipt', ['artifacts/v9/10.3/AC-10.3-01.json','artifacts/v9/10.3/AC-10.3-03.json'])
def test_current_refreshed_source_accepts_exact_withdrawal_and_writes_receipt(tmp_path,capsys,receipt):
    root,binding,candidate=withdrawn_source_repository(tmp_path)
    exit_code=current.main(['--repository',str(root),'--baseline',current.BASELINE,'--candidate','HEAD',
        '--authority-bundle',binding['authorityBundle']['path'],'--verify-workflows','--output',receipt])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==0 and observed['result']=='PASS' and observed['blockers']==[]
    assert observed['changedPaths']==binding['expectedChangedPaths'] and observed['gitlinks']==[]
    assert dict(id='binding:withdrawal',subject='prior publication',state='PASS') in observed['assertionLedger']
    assert json.loads((root/receipt).read_bytes())==observed
    assert (root/receipt).read_bytes()==current.canonical(observed)+b'\n'
    binding['withdrawnPublication'][0]['sha256']='f'*64
    (root/current.BINDING).write_text(json.dumps(binding))
    rejected=current.verify(root,current.BASELINE,'HEAD',authority_bundle=binding['authorityBundle']['path'])
    assert rejected['result']=='FAIL' and 'AUTHORITY_BINDING_INVALID' in rejected['blockers']
    assert dict(id='binding:source-paths',subject=current.BINDING,state='FAIL') in rejected['assertionLedger']


@pytest.mark.parametrize('fault', ['tracked-receipt','unproven-deletion','pair'])
def test_current_withdrawal_output_exception_preserves_other_inputs(tmp_path,monkeypatch,capsys,fault):
    output='docs/release-evidence/story-10.3-final-record-v2.json' if fault=='pair' else 'artifacts/v9/10.3/AC-10.3-01.json'
    target=tmp_path/output;target.parent.mkdir(parents=True);target.write_bytes(b'protected')
    document=current.empty_result('a'*40)
    document.update(result='PASS',changedPaths=list(current.PUBLICATION_PATHS),
        assertionLedger=[dict(id='scope',subject='measured',state='PASS')])
    if fault!='unproven-deletion':document['assertionLedger'].append(dict(id='binding:withdrawal',subject='prior publication',state='PASS'))
    monkeypatch.setattr(current,'verify',lambda *_args,**_kwargs:copy.deepcopy(document))
    monkeypatch.setattr(current,'bridge',lambda *_args:dict(result='PASS',treePaths=[output] if fault=='tracked-receipt' else [],headPaths=[],indexPaths=[],
        containedPaths=[dict(code=None,fullPath=str(target))]))
    exit_code=current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--authority-bundle','authority.json','--output',output])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==1 and observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
    assert target.read_bytes()==b'protected'


@pytest.mark.parametrize('ownership', ['head','index'])
def test_stable_code_contract_current_ownership_preserves_receipt(tmp_path,capsys,ownership):
    root,binding,candidate=withdrawn_source_repository(tmp_path)
    receipt='artifacts/v9/10.3/AC-10.3-01.json'
    sentinel=b'protected ownership receipt\r\n'
    target=root/receipt;target.write_bytes(sentinel)
    subprocess.run(['git','-C',str(root),'add','--force','--',receipt],check=True)
    if ownership=='head':
        subprocess.run(['git','-C',str(root),'-c','user.name=Fixture','-c','user.email=fixture@example.invalid',
            'commit','--quiet','-m','test: retain published receipt ownership'],check=True,capture_output=True)
    exit_code=current.main(['--repository',str(root),'--baseline',current.BASELINE,'--candidate',candidate,
        '--authority-bundle',binding['authorityBundle']['path'],'--output',receipt])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==1 and observed['result']=='FAIL'
    assert observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
    assert dict(id='output:protected-input',subject=receipt,state='FAIL') in observed['assertionLedger']
    assert target.read_bytes()==sentinel


def test_stable_code_contract_committed_edited_xml_withdrawal_fails(tmp_path,capsys):
    root,binding,candidate=withdrawn_source_repository(tmp_path,edited_xml=True)
    target=root/'artifacts/v9/10.3/AC-10.3-02.xml';original=target.read_bytes()
    exit_code=current.main(['--repository',str(root),'--baseline',current.BASELINE,'--candidate',candidate,
        '--authority-bundle',binding['authorityBundle']['path']])
    observed=json.loads(capsys.readouterr().out)
    assert exit_code==1 and observed['result']=='FAIL' and observed['blockers']==['AUTHORITY_BINDING_INVALID']
    assert dict(id='binding:source-paths',subject=current.BINDING,state='PASS') in observed['assertionLedger']
    assert dict(id='changed-paths',subject='changed-paths',state='PASS') in observed['assertionLedger']
    assert dict(id='binding:withdrawal',subject='prior publication',state='FAIL') in observed['assertionLedger']
    assert target.read_bytes()==original


def test_stable_code_contract_receipt_is_canonical_utf8_bytes(tmp_path,monkeypatch,capsys):
    document=current.empty_result('a'*40)
    document.update(result='PASS',assertionLedger=[dict(id='scope',subject='measured é',state='PASS')])
    target=tmp_path/'result.json'
    monkeypatch.setattr(current,'verify',lambda *_args,**_kwargs:copy.deepcopy(document))
    monkeypatch.setattr(current,'bridge',lambda *_args:dict(result='PASS',treePaths=[],headPaths=[],indexPaths=[],
        containedPaths=[dict(code=None,fullPath=str(target))]))
    def text_write(*_args,**_kwargs):raise AssertionError('receipt writes must use bytes')
    monkeypatch.setattr(Path,'write_text',text_write)
    assert current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--authority-bundle','authority.json','--output','result.json'])==0
    observed=json.loads(capsys.readouterr().out)
    assert target.read_bytes()==current.canonical(observed)+b'\n'
    assert b'\xc3\xa9' in target.read_bytes() and b'\r\n' not in target.read_bytes()


@pytest.mark.parametrize('row_id', ['output:head','output:head-tree','output:index','output:index-completed'])
@pytest.mark.parametrize('code,state,exit_code', [('HELPER_FAILURE','FAIL',1),('HISTORY_UNAVAILABLE','SKIPPED',2)])
def test_stable_code_contract_current_ownership_unavailable_cannot_write(tmp_path,monkeypatch,capsys,code,state,exit_code,row_id):
    document=current.empty_result('a'*40)
    document.update(result='PASS',assertionLedger=[dict(id='scope',subject='measured',state='PASS')])
    target=tmp_path/'result.json'
    monkeypatch.setattr(current,'verify',lambda *_args,**_kwargs:copy.deepcopy(document))
    monkeypatch.setattr(current,'bridge',lambda *_args:dict(result='FAIL',treePaths=[],headPaths=[],indexPaths=[],
        assertionLedger=[dict(id=row_id,subject='ownership',state=state)],blockers=[code],
        containedPaths=[dict(code=None,fullPath=str(target))]))
    assert current.main(['--repository',str(tmp_path),'--baseline',current.BASELINE,'--authority-bundle','authority.json','--output','result.json'])==exit_code
    observed=json.loads(capsys.readouterr().out)
    assert observed['blockers']==[code] and not target.exists()

# The current CI lane names this module explicitly. Include the scoped successor's
# real-helper faults here while keeping historical 10.3 fixture bytes independent.
import importlib.util as _adoption_importlib
_adoption_path = Path(__file__).with_name('test_evidence_boundary_adoption.py')
_adoption_spec = _adoption_importlib.spec_from_file_location('story104_current_lane', _adoption_path)
_adoption_module = _adoption_importlib.module_from_spec(_adoption_spec)
_adoption_spec.loader.exec_module(_adoption_module)
for _adoption_name in dir(_adoption_module):
    if _adoption_name.startswith(('test_complete_fault_matrix','test_approved_gitlink_amendment','test_neutral_helper_','test_runbook_acceptance_sequence_','test_fault_receipt_')) or _adoption_name in (
            'test_adoption_output_hardlink_cannot_overwrite_a_source',
            'stamp_adoption_candidate','adoption_surface','adoption_git','approved_gitlink_repository'):
        globals()[_adoption_name] = getattr(_adoption_module, _adoption_name)
