"""Checkpoint C independent measurement, threshold edges and restore-checked fault receipts."""
from __future__ import annotations

from copy import deepcopy
from contextlib import redirect_stdout
from io import StringIO
import json
from pathlib import Path
import shutil
import subprocess
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
    bridge.original = original
    return bridge


@pytest.fixture
def sm2_repository(tmp_path, monkeypatch, committed_facts):
    repository = tmp_path / 'repository'
    repository.mkdir()
    # Read-only object access to real accepted A/B history, without cloning or entering a submodule.
    git_directory = records.run_git(ROOT, 'rev-parse', '--absolute-git-dir').stdout.decode().strip()
    (repository / '.git').write_text('gitdir: ' + git_directory + '\n')
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


def run_verifier_cli(repository):
    """Observe the public entry point's JSON and returned exit, without inferred mappings."""
    output = StringIO()
    with redirect_stdout(output):
        observed_exit = verifier.main(['--repository', str(repository), '--evidence', generator.JSON_PATH,
                                      '--markdown', generator.MARKDOWN_PATH, '--require-oq2-decision'])
    return observed_exit, json.loads(output.getvalue())


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


@pytest.mark.parametrize('relationship', [0, 1, 2], ids=['planning-A', 'A-B', 'B-C'])
def test_candidate_rejects_each_incompatible_ancestry(sm2_repository, monkeypatch, relationship):
    baseline = chain.verify(sm2_repository)
    ancestry = [row for row in baseline['assertionLedger'] if row['id'].startswith('ancestry:')]
    assert len(ancestry) == 3 and all(row['state'] == 'PASS' for row in ancestry)
    rejected = ancestry[relationship]
    original = records.run_git
    def incompatible(repository, *args, **kwargs):
        result = original(repository, *args, **kwargs)
        if args == ('merge-base', '--is-ancestor', rejected['id'].removeprefix('ancestry:'), rejected['subject']):
            result.returncode = 1
        return result
    monkeypatch.setattr(records, 'run_git', incompatible)
    result = chain.verify(sm2_repository)
    assert result['result'] == 'FAIL' and chain.MISMATCH in result['blockers']
    assert next(row for row in result['assertionLedger'] if row['id'] == rejected['id'])['state'] == 'FAIL'


@pytest.mark.parametrize('identity', ['gitlinks:raw-diff', 'gitlinks:B-C'])
def test_candidate_rejects_each_gitlink_boundary(sm2_repository, monkeypatch, identity):
    original = chain.current.bridge
    def incompatible(repository, request):
        result = original(repository, request)
        if identity == 'gitlinks:raw-diff':
            result['diff'].append(dict(path='references/unapproved', sourceMode='160000', destinationMode='000000'))
        else:
            result['rootGitlinks'][0]['commit'] = 'f' * 40
        return result
    monkeypatch.setattr(chain.current, 'bridge', incompatible)
    result = chain.verify(sm2_repository)
    assert result['result'] == 'FAIL' and chain.MISMATCH in result['blockers']
    assert next(row for row in result['assertionLedger'] if row['id'] == identity)['state'] == 'FAIL'


def test_repository_fixture_accepts_a_gitdir_pointer(tmp_path, monkeypatch, committed_facts):
    linked = tmp_path / 'linked'
    linked.mkdir()
    git_directory = records.run_git(ROOT, 'rev-parse', '--absolute-git-dir').stdout.decode().strip()
    (linked / '.git').write_text('gitdir: ' + git_directory + '\n')
    for path in ROOT.iterdir():
        if path.name != '.git':
            (linked / path.name).symlink_to(path, target_is_directory=path.is_dir())
    monkeypatch.setattr(sys.modules[__name__], 'ROOT', linked)
    repo = sm2_repository.__wrapped__(tmp_path, monkeypatch, committed_facts)
    assert fixture.git_head(repo) == fixture.git_head(linked)
    assert verifier.verify(repo)['result'] == 'PASS'


def test_oq2_decision_is_checked_without_the_optional_cli_flag(sm2_repository):
    repo = sm2_repository
    mutate(repo, json.loads((repo / generator.JSON_PATH).read_bytes()), 'decision')
    output = StringIO()
    with redirect_stdout(output):
        status = verifier.main(['--repository', str(repo), '--evidence', generator.JSON_PATH,
                                '--markdown', generator.MARKDOWN_PATH])
    result = json.loads(output.getvalue())
    assert status == 1 and result['result'] == 'FAIL'
    assert 'OQ2_DECISION_INVALID' in result['blockers']
    assert next(row for row in result['assertionLedger'] if row['id'] == 'OQ2:decision')['state'] == 'FAIL'


@pytest.mark.parametrize('module,path', [(verifier, 'artifacts/v9/11.3/AC-11.3-04.json'),
                                        (chain, 'artifacts/v9/11.3/AC-11.3-05.json')], ids=['measurement', 'candidate'])
def test_verifier_receipts_replace_hardlinks_with_exact_lf_bytes(sm2_repository, module, path, capsys):
    repo = sm2_repository
    original = repo / 'original-receipt.json'
    original.write_bytes(b'original\r\n')
    target = repo / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.hardlink_to(original)
    arguments = ['--repository', str(repo), '--output', path]
    if module is verifier:
        arguments += ['--evidence', generator.JSON_PATH, '--markdown', generator.MARKDOWN_PATH]
    else:
        arguments += ['--authority-bundle', chain.AUTHORITY, '--contract', chain.CONTRACT]
    assert module.main(arguments) == 0
    printed = json.loads(capsys.readouterr().out)
    assert target.read_bytes() == (json.dumps(printed, indent=2) + '\n').encode()
    assert b'\r\n' not in target.read_bytes()
    assert original.read_bytes() == b'original\r\n' and not target.samefile(original)


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
        target = repo / next(path for path in fixture.FROZEN_PATHS if path.endswith('/ConversationModule.cs'))
        content = target.read_bytes()
        registration = b'        ArgumentNullException.ThrowIfNull(builder);\n'
        assert content.count(registration) == 1
        target.write_bytes(content.replace(registration, registration + b'        _ = builder.Services.AddOpenTelemetry();\n', 1))
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
    baseline_hash = verifier.measurement_input_sha256(repo)
    baseline_exit, baseline = run_verifier_cli(repo)
    assert baseline_exit == 0 and baseline['result'] == 'PASS' and not baseline['blockers'], 'FAULT_NOT_DETECTED: baseline must pass'
    observed = None
    try:
        mutate(repo, doc, fault)
        mutated_hash = verifier.measurement_input_sha256(repo)
        assert mutated_hash != baseline_hash, 'FAULT_NOT_DETECTED: mutation did not change measured inputs'
        observed_exit, observed = run_verifier_cli(repo)
        expected = verifier.FAULT_EXPECTATIONS[fault]
        assert expected in observed['blockers'], 'FAULT_NOT_DETECTED: ' + repr(observed)
        assert observed['result'] == ('BLOCKED' if fault == 'history-unavailable' else 'FAIL'), 'FAULT_NOT_DETECTED'
        assert observed_exit == (2 if fault == 'history-unavailable' else 1), 'FAULT_NOT_DETECTED: CLI exit differs'
        assert observed['assertionLedger'], 'FAULT_NOT_DETECTED: vacuous ledger'
        assert observed['metrics'] == {}, 'FAULT_NOT_DETECTED: failed verification must not publish measurement claims'
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
        restored_hash = verifier.measurement_input_sha256(repo)
        restored_exit, restored = run_verifier_cli(repo)
        assert restored_hash == baseline_hash and restored_exit == 0 and restored == baseline, 'FIXTURE_NOT_RESTORED: restored verification must pass'
    record_property('story113_source_revision', fixture.git_head(ROOT))
    record_property('story113_evidence_sha256', verifier.digest(before[generator.JSON_PATH]))
    record_property('story113_fault_id', fault)
    record_property('story113_expected_code', verifier.FAULT_EXPECTATIONS[fault])
    record_property('story113_observed_blockers', json.dumps(observed['blockers']))
    record_property('story113_observed_result', observed['result'])
    record_property('story113_observed_exit', str(observed_exit))
    record_property('story113_before_sha256', baseline_hash)
    record_property('story113_mutated_sha256', mutated_hash)
    record_property('story113_after_sha256', restored_hash)
    record_property('story113_baseline_exit', str(baseline_exit))
    record_property('story113_restored_exit', str(restored_exit))
    record_property('story113_baseline_report', json.dumps(baseline))
    record_property('story113_restored_report', json.dumps(restored))
    record_property('story113_observed_report', json.dumps(observed))
    record_property('story113_assertion_ledger', json.dumps(observed['assertionLedger']))


@pytest.mark.parametrize('section,field,value,blocker,assertion', [
    ('measurement', 'physicalLocMetric', 'logical source lines', 'SM2_MEASUREMENT_INVALID', 'measurement:definitions'),
    ('measurement', 'nonblankLocMetric', 'all physical lines', 'SM2_MEASUREMENT_INVALID', 'measurement:definitions'),
    ('comparison', 'decisiveDimension', 'LOC', 'SM2_MEASUREMENT_INVALID', 'comparison:dimensions'),
    ('comparison', 'supportingDimension', 'files', 'SM2_MEASUREMENT_INVALID', 'comparison:dimensions'),
    ('historicalBaseline', 'ownerDecision', 'unconditional high-confidence estimate', 'SM2_MEASUREMENT_INVALID', 'history:owner-decision'),
    ('singleFileSensitivity', 'explanation', 'excluding one file flips attainment', 'SM2_MEASUREMENT_INVALID', 'sensitivity'),
    ('comparison', 'limitations', [], 'SM2_MEASUREMENT_INVALID', 'comparison:confidence'),
    ('measurement', 'exclusions', [], 'SM2_INCLUSION_RULE_VIOLATION', 'measurement:exclusions'),
    ('measurement', 'excludedFiles', [], 'SM2_INCLUSION_RULE_VIOLATION', 'measurement:exclusions'),
    ('measurement', 'paths', 'AppHost', 'SM2_INCLUSION_RULE_VIOLATION', 'classification:' + fixture.FROZEN_PATHS[0]),
    (None, 'command', 'python3 unapproved.py', 'EVIDENCE_FORMAT_DRIFT', 'command'),
    ('toolVersions', 'python', 'unversioned', 'SM2_MEASUREMENT_INVALID', 'tools'),
])
def test_resigned_metric_meanings_cannot_contradict_the_measurement(sm2_repository, section, field, value, blocker, assertion):
    repo = sm2_repository
    document = json.loads((repo / generator.JSON_PATH).read_bytes())
    if field == 'paths':
        document[section][field][0]['category'] = value
    else:
        (document[section] if section else document)[field] = value
    j, md = generator.finalize({key: value for key, value in document.items() if key != 'digests'})
    (repo / generator.JSON_PATH).write_bytes(j)
    (repo / generator.MARKDOWN_PATH).write_bytes(md)
    status, observed = run_verifier_cli(repo)
    assert status == 1 and observed['result'] == 'FAIL'
    assert blocker in observed['blockers']
    assert next(row for row in observed['assertionLedger'] if row['id'] == assertion)['state'] == 'FAIL'
    assert observed['metrics'] == {}


def test_recorded_generation_versions_survive_unrelated_environment_upgrades(sm2_repository, monkeypatch):
    repo = sm2_repository
    document = json.loads((repo / generator.JSON_PATH).read_bytes())
    upgraded = dict(python='4.0.0', jsonschema='10.0.0', dotnetSdk='20.0.100', git='git version 5.0.0')
    monkeypatch.setattr(generator, 'versions', lambda repository: upgraded)
    assert run_verifier_cli(repo)[0] == 0  # independent verification never probes current versions
    assert generator.finalize(generator.build_signable(repo, _recorded_versions=document['toolVersions'])) == (
        (repo / generator.JSON_PATH).read_bytes(), (repo / generator.MARKDOWN_PATH).read_bytes())
    fresh = generator.build_signable(repo)
    assert fresh['toolVersions'] == upgraded and fresh['measurement'] == document['measurement']


def test_generation_tool_timeout_is_a_controlled_blocker(sm2_repository, monkeypatch, capsys):
    original = generator.subprocess.run
    def timeout(args, **kwargs):
        if args == ['dotnet', '--version']:
            raise subprocess.TimeoutExpired(args, 60)
        return original(args, **kwargs)
    monkeypatch.setattr(generator.subprocess, 'run', timeout)
    with pytest.raises(generator.EvidenceError) as caught:
        generator.versions(sm2_repository)
    assert caught.value.code == 'HISTORY_UNAVAILABLE'
    assert run_verifier_cli(sm2_repository)[0] == 0  # historical verification does not probe the timed-out tool
    assert generator.main(['--repository', str(sm2_repository), '--contract', chain.CONTRACT,
                           '--fixture-proof', fixture.PROOF_JSON_PATH, '--baseline', generator.BASELINE,
                           '--output-json', generator.JSON_PATH, '--output-markdown', generator.MARKDOWN_PATH]) == 2
    assert 'HISTORY_UNAVAILABLE: tool version unavailable: dotnetSdk' in capsys.readouterr().err


def test_recorded_git_provenance_allows_vendor_versions():
    recorded = dict(python='3.13.5', jsonschema='4.25.1', dotnetSdk='10.0.402', git='git version 2.50.1 (Apple Git-155)')
    assert generator.valid_versions(recorded)
    assert not generator.valid_versions(dict(recorded, git='git version 2.50.1\nforged'))


def test_measurement_snapshot_has_explicit_portable_inputs(sm2_repository):
    repo = sm2_repository
    before = verifier.measurement_input_sha256(repo)
    (repo / '.git').write_text('a different temporary Git pointer\n')
    (repo / fixture.FIXTURE_ROOT / 'src/bin').mkdir()
    (repo / fixture.FIXTURE_ROOT / 'src/bin/generated.dll').write_bytes(b'generated output')
    assert verifier.measurement_input_sha256(repo) == before
    target = repo / fixture.FIXTURE_ROOT / 'Added.cs'
    target.write_bytes(b'// authored source\n')
    assert verifier.measurement_input_sha256(repo) != before
    target.unlink()
    assert verifier.measurement_input_sha256(repo) == before
    (repo / fixture.FROZEN_PATHS[0]).unlink()
    assert verifier.measurement_input_sha256(repo) != before
