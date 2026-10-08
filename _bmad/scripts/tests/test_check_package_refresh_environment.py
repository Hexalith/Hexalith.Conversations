"""Named mutations for the additive October package-environment evidence."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
import json
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location("check_package_refresh_environment", ROOT / "_bmad/scripts/check_package_refresh_environment.py")
assert SPEC is not None and SPEC.loader is not None
checker = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(checker)
REAL_BUILDS_CONTEXT = checker.builds_context


@pytest.fixture
def environment(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> Path:
    """Supply real tooling bytes and an isolated, explicitly uncommitted catalog."""
    for path in (*checker.SOURCE_PATHS, *checker.IMMUTABLE):
        target = tmp_path / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / path).read_bytes())
    catalog = b"\xef\xbb\xbf" + (
        '<Project><PropertyGroup><HexalithAspireHostingDaprVersion>'
        + checker.CATALOG_PINS['CommunityToolkit.Aspire.Hosting.Dapr']
        + '</HexalithAspireHostingDaprVersion></PropertyGroup><ItemGroup>'
        + ''.join(f'<PackageVersion Include="{name}" Version="{version}" />' for name,version in checker.CATALOG_PINS.items())
        + '</ItemGroup></Project>'
    ).encode()
    target = tmp_path / checker.BUILDS_PATH / checker.CATALOG_PATH
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(catalog)

    def context(root: Path, candidate: str | None):
        content = checker.worktree_bytes(root / checker.BUILDS_PATH, checker.CATALOG_PATH)
        return content, {
            'path': checker.BUILDS_PATH, 'catalogPath': checker.CATALOG_PATH,
            'catalogSha256': checker.helpers.sha256(content), 'mode':'160000',
            'headCommit':'a'*40, 'recordedCommit':'a'*40, 'clean':False,
            'catalogCommitted':False, 'remoteAvailable':True,
        }

    monkeypatch.setattr(checker, 'builds_context', context)
    return tmp_path


def test_preview_binds_every_source_and_retains_precise_non_success(environment: Path) -> None:
    document = checker.render(environment)
    assert checker.check(environment, document) == document
    assert document['result'] == 'BLOCKED'
    assert document['candidateCommit'] is None
    assert {row['code'] for row in document['blockers']} == {
        'REFRESH_SOURCE_COMMIT_REQUIRED', 'REFRESH_BUILDS_COMMIT_REQUIRED',
    }
    assert [row['path'] for row in document['sourceBindings']] == list(checker.SOURCE_PATHS)
    assert len(document['sourceBindings']) == 12
    assert len(document['assertionLedger']) == 7
    assert all(value is False for value in document['authorityEffect'].values())


@pytest.mark.parametrize('fault', ['extra-field','empty-ledger','empty-blockers','false-pass','scope-escape','missing-source','duplicate-source','false-approval','false-candidate','declared-hash','catalog-hash'])
def test_closed_record_fault_is_rejected(environment: Path, fault: str) -> None:
    document = deepcopy(checker.render(environment))
    if fault == 'extra-field':
        document['approved'] = True
    elif fault == 'empty-ledger':
        document['assertionLedger'] = []
    elif fault == 'empty-blockers':
        document['blockers'] = []
    elif fault == 'false-pass':
        document['result'] = 'PASS'
    elif fault == 'scope-escape':
        document['sourceBindings'][0]['path'] = '../escape'
    elif fault == 'missing-source':
        document['sourceBindings'].pop()
    elif fault == 'duplicate-source':
        document['sourceBindings'][1] = document['sourceBindings'][0]
    elif fault == 'false-approval':
        document['authorityEffect']['ownerApprovalClaimed'] = True
    elif fault == 'false-candidate':
        document['candidateCommit'] = 'a'*40
    elif fault == 'declared-hash':
        document['sourceBindings'][0]['sha256'] = '0'*64
    else:
        document['buildsCatalog']['catalogSha256'] = '0'*64
    with pytest.raises(checker.RefreshError):
        checker.check(environment, document)


@pytest.mark.parametrize('path', ['uv.lock', 'package-lock.json', '.github/workflows/ci.yml'])
def test_changed_exact_source_bytes_are_detected(environment: Path, path: str) -> None:
    document = checker.render(environment)
    target = environment / path
    target.write_bytes(target.read_bytes() + b'\n')
    with pytest.raises(checker.RefreshError, match='REFRESH_RECORD_DRIFT'):
        checker.check(environment, document)


def test_python_graph_and_npm_lock_parity_faults_are_rejected(environment: Path) -> None:
    target = environment / 'uv.lock'
    original = target.read_bytes()
    target.write_bytes(original.replace(b'version = "2.3.1"', b'version = "2.3.0"'))
    with pytest.raises(checker.RefreshError, match='REFRESH_PYTHON_GRAPH_DRIFT'):
        checker.render(environment)
    target.write_bytes(original)
    lock = json.loads((environment / 'package-lock.json').read_bytes())
    lock['packages']['']['devDependencies']['@commitlint/cli'] = '21.2.2'
    (environment / 'package-lock.json').write_text(json.dumps(lock))
    with pytest.raises(checker.RefreshError, match='REFRESH_NPM_LOCK_PARITY_DRIFT'):
        checker.render(environment)


def test_catalog_channel_downgrade_and_bom_loss_are_rejected(environment: Path) -> None:
    target = environment / checker.BUILDS_PATH / checker.CATALOG_PATH
    original = target.read_bytes()
    target.write_bytes(original.replace(checker.CATALOG_PINS['CommunityToolkit.Aspire.Hosting.Dapr'].encode(), b'13.0.0'))
    with pytest.raises(checker.RefreshError, match='REFRESH_CATALOG_PIN_DRIFT'):
        checker.render(environment)
    target.write_bytes(original[3:])
    with pytest.raises(checker.RefreshError, match='REFRESH_CATALOG_BOM_DRIFT'):
        checker.render(environment)


@pytest.mark.parametrize('path', list(checker.IMMUTABLE))
def test_accepted_predecessor_bytes_cannot_change(environment: Path, path: str) -> None:
    target = environment / path
    target.write_bytes(target.read_bytes() + b'\n')
    with pytest.raises(checker.RefreshError, match='REFRESH_PREDECESSOR_DRIFT'):
        checker.render(environment)


def test_symlink_component_is_blocked_without_reading_target(environment: Path, tmp_path: Path) -> None:
    target = environment / 'uv.lock'
    content = target.read_bytes()
    target.unlink()
    outside = tmp_path / 'outside.lock'
    outside.write_bytes(content)
    target.symlink_to(outside)
    with pytest.raises(checker.RefreshError, match='REFRESH_PATH_UNSAFE') as error:
        checker.render(environment)
    assert error.value.state == 'BLOCKED'


def test_symlink_references_directory_is_blocked_before_git_reads(environment: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    references = environment / 'references'
    outside = environment.parent / 'outside-references'
    references.rename(outside)
    references.symlink_to(outside, target_is_directory=True)
    monkeypatch.setattr(checker, 'builds_context', REAL_BUILDS_CONTEXT)
    monkeypatch.setattr(checker.helpers, 'resolve_commit', lambda *args: pytest.fail('Git must not read through the symlink'))
    with pytest.raises(checker.RefreshError, match='REFRESH_PATH_UNSAFE') as error:
        checker.render(environment)
    assert error.value.state == 'BLOCKED'


def test_preview_write_and_check_never_report_pass(environment: Path, capsys: pytest.CaptureFixture[str]) -> None:
    assert checker.main(['--repository', str(environment), '--write']) == 1
    assert json.loads(capsys.readouterr().out)['result'] == 'BLOCKED'
    record = (environment / checker.RECORD_PATH).read_bytes()
    assert checker.main(['--repository', str(environment), '--check']) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['assertionLedger']
    assert result['result'] == 'BLOCKED'
    assert (environment / checker.RECORD_PATH).read_bytes() == record


@pytest.mark.parametrize('document', [None, [], 0, 'invalid'])
def test_non_object_record_is_rejected(environment: Path, document) -> None:
    with pytest.raises(checker.RefreshError, match='REFRESH_RECORD_SCHEMA_INVALID'):
        checker.check(environment, document)


def test_preview_write_rejects_dangling_record_symlink(environment: Path, capsys: pytest.CaptureFixture[str]) -> None:
    target = environment / checker.RECORD_PATH
    outside = environment.parent / 'outside-record.json'
    target.symlink_to(outside)
    assert checker.main(['--repository', str(environment), '--write']) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['result'] == 'BLOCKED'
    assert result['blockers'][0]['code'] == 'REFRESH_PATH_UNSAFE'
    assert not outside.exists()


@pytest.fixture
def committed_environment(environment: Path, monkeypatch: pytest.MonkeyPatch):
    """Model canonical object reads without creating unauthorized fixture commits."""
    candidate, publication = 'c'*40, 'd'*40
    preview_context = checker.builds_context

    def context(root: Path, revision: str | None):
        content, observed = preview_context(root, revision)
        return content, {**observed, 'clean':True, 'catalogCommitted':True}

    monkeypatch.setattr(checker, 'builds_context', context)
    monkeypatch.setattr(checker.helpers, 'resolve_commit', lambda root, revision, code: revision)
    monkeypatch.setattr(checker.helpers, 'require_ancestor', lambda *args: None)
    monkeypatch.setattr(checker.helpers, 'commit_parents', lambda root, revision, code: (candidate,) if revision == publication else (checker.BASELINE,))
    monkeypatch.setattr(checker.helpers, 'changed_paths', lambda root, baseline, revision: (checker.RECORD_PATH,) if revision == publication else checker.C1_PATHS)
    monkeypatch.setattr(checker.helpers, 'changed_gitlinks', lambda *args: (checker.BUILDS_PATH,))
    monkeypatch.setattr(checker.helpers, 'raw_tree_record', lambda *args: ('100644','blob','e'*40))
    monkeypatch.setattr(checker.helpers, 'candidate_blob', lambda root, revision, path: (root / path).read_bytes())
    document = checker.render(environment, candidate)
    (environment / checker.RECORD_PATH).parent.mkdir(parents=True, exist_ok=True)
    (environment / checker.RECORD_PATH).write_bytes(checker.helpers.json_bytes(document))
    monkeypatch.setattr(checker, 'git_text', lambda *args: publication)
    return environment, document, candidate, publication


def test_committed_c1_c2_use_exact_scopes_and_canonical_bytes(committed_environment) -> None:
    root, document, _, _ = committed_environment
    assert checker.check(root, document) == document
    assert document['result'] == 'PASS'
    assert document['blockers'] == []
    assert len(document['assertionLedger']) == 5


def test_committed_record_uses_candidate_schema_when_checkout_schema_changes(committed_environment, monkeypatch: pytest.MonkeyPatch) -> None:
    root, document, _, _ = committed_environment
    schema_path = root / checker.SCHEMA_PATH
    committed_schema = schema_path.read_bytes()
    schema_path.write_text('{"not": {}}')
    monkeypatch.setattr(checker.helpers, 'candidate_blob', lambda repository, revision, path:
                        committed_schema if path == checker.SCHEMA_PATH else (repository / path).read_bytes())
    assert checker.check(root, document) == document


def test_missing_record_publication_is_blocked(committed_environment, monkeypatch: pytest.MonkeyPatch) -> None:
    root, document, _, _ = committed_environment
    monkeypatch.setattr(checker, 'git_text', lambda *args: '')
    with pytest.raises(checker.RefreshError, match='REFRESH_C2_PUBLICATION_REQUIRED') as error:
        checker.check(root, document)
    assert error.value.state == 'BLOCKED'


def test_combined_or_extra_path_publication_fails(committed_environment, monkeypatch: pytest.MonkeyPatch) -> None:
    root, document, _, publication = committed_environment
    monkeypatch.setattr(checker.helpers, 'changed_paths', lambda root, baseline, revision: (checker.RECORD_PATH, 'unexpected.txt') if revision == publication else checker.C1_PATHS)
    with pytest.raises(checker.RefreshError, match='REFRESH_C2_SCOPE_DRIFT'):
        checker.check(root, document)


def test_committed_source_mode_drift_fails(committed_environment, monkeypatch: pytest.MonkeyPatch) -> None:
    root, document, _, _ = committed_environment
    monkeypatch.setattr(checker.helpers, 'raw_tree_record', lambda *args: ('100755','blob','e'*40))
    with pytest.raises(checker.RefreshError, match='REFRESH_SOURCE_MODE_DRIFT'):
        checker.check(root, document)


@pytest.mark.parametrize('revision', ['publication', 'HEAD'])
def test_published_record_mode_drift_fails(committed_environment, monkeypatch: pytest.MonkeyPatch, revision: str) -> None:
    root, document, _, publication = committed_environment
    selected = publication if revision == 'publication' else revision
    monkeypatch.setattr(checker.helpers, 'raw_tree_record', lambda root, rev, path:
                        ('100755', 'blob', 'e'*40) if rev == selected and path == checker.RECORD_PATH
                        else ('100644', 'blob', 'e'*40))
    with pytest.raises(checker.RefreshError, match='REFRESH_SOURCE_MODE_DRIFT'):
        checker.check(root, document)


def test_wrong_c1_gitlink_scope_fails(committed_environment, monkeypatch: pytest.MonkeyPatch) -> None:
    root, _, candidate, _ = committed_environment
    monkeypatch.setattr(checker.helpers, 'changed_gitlinks', lambda *args: ('references/Unapproved',))
    with pytest.raises(checker.RefreshError, match='REFRESH_GITLINK_SCOPE_DRIFT'):
        checker.render(root, candidate)
