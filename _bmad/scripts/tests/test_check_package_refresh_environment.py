"""Named mutations for the additive October package-environment evidence."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
import json
import os
from pathlib import Path
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location("check_package_refresh_environment", ROOT / "_bmad/scripts/check_package_refresh_environment.py")
assert SPEC is not None and SPEC.loader is not None
checker = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(checker)
REAL_BUILDS_CONTEXT = checker.builds_context
FIXTURE_COMMIT_MESSAGE = 'test(evidence): model the package refresh boundary'


def fixture_catalog() -> bytes:
    """Keep all fixed families available without reading an initialized submodule."""
    rows = {**dict.fromkeys(checker.MICROSOFT_SERVICING_MEMBERS, '10.0.12'),
            **checker.INDEPENDENT_CATALOG_PINS, **checker.CATALOG_PINS}
    return b'\xef\xbb\xbf' + ('<Project><ItemGroup>'
        + ''.join(f'<PackageVersion Include="{name}" Version="{version}" />' for name, version in sorted(rows.items()))
        + '</ItemGroup></Project>').encode()


@pytest.fixture
def environment(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> Path:
    """Supply real tooling bytes and an isolated, explicitly uncommitted catalog."""
    for path in (*checker.SOURCE_PATHS, *checker.IMMUTABLE, checker.HELPER_PATH):
        target = tmp_path / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / path).read_bytes())
    catalog = fixture_catalog()
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


@pytest.mark.parametrize('path,content', [
    ('package.json', '[]'), ('package-lock.json', '{"packages": []}'),
    ('package-lock.json', '{"packages": {"": []}}'), ('global.json', '{"sdk": []}'),
    ('package-lock.json', json.dumps({'packages': {'': {'devDependencies': checker.NPM_PINS},
                                                  'node_modules/@commitlint/cli': []}})),
    ('uv.lock', 'package = [0]'),
])
def test_malformed_nested_inputs_keep_structured_failures(environment: Path, capsys, path: str, content: str) -> None:
    (environment / path).write_text(content)
    assert checker.main(['--repository', str(environment)]) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['result'] == 'FAIL'
    assert result['assertionLedger'] and result['blockers']


@pytest.mark.parametrize('section', ['dependencies', 'optionalDependencies', 'peerDependencies', 'peerDependenciesMeta'])
@pytest.mark.parametrize('path', ['package.json', 'package-lock.json'])
def test_unselected_npm_sections_are_closed(environment: Path, path: str, section: str) -> None:
    target = environment / path
    document = json.loads(target.read_bytes())
    entry = document if path == 'package.json' else document['packages']['']
    entry[section] = {'unselected-runtime-package': '1.0.0'}
    target.write_text(json.dumps(document))
    with pytest.raises(checker.RefreshError, match='REFRESH_NPM_SCOPE_DRIFT'):
        checker.render(environment)


@pytest.mark.parametrize('fault', ['deleted-edge', 'changed-marker', 'duplicate-metadata', 'conflicting-metadata'])
def test_fixed_python_relationships_and_metadata_are_closed(environment: Path, fault: str) -> None:
    target = environment / 'uv.lock'
    original = target.read_bytes()
    if fault == 'deleted-edge':
        mutated = original.replace(b'    { name = "rpds-py" },\n', b'', 1)
    elif fault == 'changed-marker':
        mutated = original.replace(b"python_full_version < '3.13'", b"python_full_version >= '3.13'")
    else:
        version = b'9.1.1' if fault == 'duplicate-metadata' else b'0.0.0'
        entry = b'    { name = "pytest", specifier = "==' + version + b'" },\n'
        mutated = original.replace(b'requires-dist = [\n', b'requires-dist = [\n' + entry, 1)
    assert mutated != original
    target.write_bytes(mutated)
    code = 'REFRESH_PYTHON_RELATIONSHIP_DRIFT' if fault in ['deleted-edge', 'changed-marker'] else 'REFRESH_PYTHON_LOCK_PARITY_DRIFT'
    with pytest.raises(checker.RefreshError, match=code):
        checker.render(environment)


@pytest.mark.parametrize('name', ['System.Text.Json', 'Microsoft.AspNetCore.Authorization'])
def test_servicing_downgrade_cannot_leave_family_by_changing_version_prefix(environment: Path, name: str) -> None:
    target = environment / checker.BUILDS_PATH / checker.CATALOG_PATH
    target.write_bytes(target.read_bytes().replace(
        f'Include="{name}" Version="10.0.12"'.encode(), f'Include="{name}" Version="9.0.0"'.encode()))
    with pytest.raises(checker.RefreshError, match='REFRESH_MICROSOFT_FAMILY_DRIFT'):
        checker.render(environment)


def test_check_rejects_noncanonical_worktree_record_bytes(committed_environment, capsys) -> None:
    root, document, _, _ = committed_environment
    (root / checker.RECORD_PATH).write_text(json.dumps(document))
    assert checker.main(['--repository', str(root), '--check']) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['blockers'][0]['code'] == 'REFRESH_RECORD_BYTES_NONCANONICAL'


def test_preview_record_requires_the_projection_field_order(environment: Path, capsys) -> None:
    document = checker.render(environment)
    reordered = dict(reversed(list(document.items())))
    (environment / checker.RECORD_PATH).write_bytes(checker.helpers.json_bytes(reordered))
    assert checker.main(['--repository', str(environment), '--check']) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['blockers'][0]['code'] == 'REFRESH_RECORD_BYTES_NONCANONICAL'


def fixture_git(root: Path, *arguments: str) -> str:
    """Mutate only explicitly supplied temporary repositories, without global hooks/config."""
    assert root.resolve() != ROOT.resolve() and not root.resolve().is_relative_to(ROOT.resolve())
    result = subprocess.run(['git', '-C', str(root), *arguments], check=True, capture_output=True,
                            env={**os.environ, 'GIT_CONFIG_NOSYSTEM': '1', 'GIT_CONFIG_GLOBAL': os.devnull}, timeout=30)
    return result.stdout.decode().strip()


def fixture_commit(root: Path) -> str:
    """The exact fixture message was validated with the root's pinned commitlint."""
    fixture_git(root, 'commit', '--quiet', '-m', FIXTURE_COMMIT_MESSAGE)
    return fixture_git(root, 'rev-parse', 'HEAD')


@pytest.fixture
def real_environment_factory(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    """Create isolated root/Builds commits and a local bare remote; mock no Git reads."""
    def create(*, audit_fault=None, extra_parent=False, helper_drift=False):
        root = tmp_path / 'root'
        builds = root / checker.BUILDS_PATH
        builds.mkdir(parents=True)
        for repo in (root, builds):
            fixture_git(repo, 'init', '--quiet', '--initial-branch=main')
            fixture_git(repo, 'config', 'user.name', 'Package Refresh Fixtures')
            fixture_git(repo, 'config', 'user.email', 'package-refresh@example.invalid')
            fixture_git(repo, 'config', 'core.autocrlf', 'false')
        for path in (*checker.SOURCE_PATHS, *checker.IMMUTABLE, checker.HELPER_PATH,
                     '_bmad-output/implementation-artifacts/spec-update-all-packages.md'):
            target = root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes((ROOT / path).read_bytes())
        if helper_drift:
            target = root / checker.HELPER_PATH
            target.write_bytes(target.read_bytes() + b'\n')
        catalog_path = builds / checker.CATALOG_PATH
        catalog_path.parent.mkdir(parents=True)
        catalog = fixture_catalog()
        old_catalog = catalog.replace(b'16.6.8', b'16.6.7').replace(b'33.3.2', b'33.3.1')
        catalog_path.write_bytes(old_catalog)
        fixture_git(builds, 'add', '--', checker.CATALOG_PATH)
        old_revision = fixture_commit(builds)

        def audit_document(content, revision):
            return {'catalogPath': checker.CATALOG_PATH, 'catalogRawSha256': checker.helpers.sha256(content),
                    'generatedFromRevision': revision,
                    'packages': [{'id': n, 'selectedVersion': v} for n, v in checker.catalog_versions(content).items()]}

        audit_path = builds / checker.AUDIT_PATH
        audit_path.parent.mkdir(parents=True)
        old_audit = audit_document(old_catalog, old_revision)
        audit_path.write_bytes(checker.helpers.json_bytes(old_audit))
        fixture_git(builds, 'add', '--', checker.AUDIT_PATH)
        old_head = fixture_commit(builds)
        (root / '.gitmodules').write_text('[submodule "Builds"]\n\tpath = references/Hexalith.Builds\n\turl = ./Builds.git\n')
        fixture_git(root, 'add', '--', '.')
        baseline = fixture_commit(root)
        monkeypatch.setattr(checker, 'BASELINE', baseline)

        catalog_path.write_bytes(catalog)
        fixture_git(builds, 'add', '--', checker.CATALOG_PATH)
        catalog_revision = fixture_commit(builds)
        audit = audit_document(catalog, catalog_revision)
        if audit_fault == 'raw-hash':
            audit['catalogRawSha256'] = '0'*64
        elif audit_fault == 'selection':
            audit['packages'][0]['selectedVersion'] = '0.0.0'
        elif audit_fault == 'revision-alias':
            audit['generatedFromRevision'] = 'HEAD'
        elif audit_fault == 'revision-catalog':
            audit['generatedFromRevision'] = old_revision
        elif audit_fault == 'revision-ancestry':
            tree = fixture_git(builds, 'rev-parse', 'HEAD^{tree}')
            audit['generatedFromRevision'] = fixture_git(builds, 'commit-tree', tree, '-m', FIXTURE_COMMIT_MESSAGE)
        if audit_fault != 'stale-audit':
            audit_path.write_bytes(checker.helpers.json_bytes(audit))
            fixture_git(builds, 'add', '--', checker.AUDIT_PATH)
            fixture_commit(builds)
        owning = fixture_git(builds, 'rev-parse', 'HEAD')
        remote = tmp_path / 'Builds.git'
        fixture_git(builds, 'clone', '--bare', str(builds), str(remote))
        fixture_git(builds, 'remote', 'add', 'origin', str(remote))
        fixture_git(builds, 'fetch', '--quiet', 'origin')
        if extra_parent:
            (root / 'unrelated.txt').write_text('unrelated history\n')
            fixture_git(root, 'add', '--', 'unrelated.txt')
            fixture_commit(root)
        for path in checker.C1_PATHS:
            if path == checker.BUILDS_PATH:
                continue
            target = root / path
            if path == checker.SCHEMA_PATH:
                schema = json.loads(target.read_bytes())
                schema['properties']['baselineCommit']['const'] = baseline
                target.write_bytes(checker.helpers.json_bytes(schema))
            else:
                target.write_bytes(target.read_bytes() + b'\n')
            fixture_git(root, 'add', '--', path)
        fixture_git(root, 'update-index', '--cacheinfo', f'160000,{owning},{checker.BUILDS_PATH}')
        candidate = fixture_commit(root)
        return {'root': root, 'builds': builds, 'candidate': candidate,
                'baseline': baseline, 'owning': owning, 'old_head': old_head}
    return create


def publish_fixture(state, *, extra_path=False):
    root, candidate = state['root'], state['candidate']
    document = checker.render(root, candidate)
    (root / checker.RECORD_PATH).write_bytes(checker.helpers.json_bytes(document))
    fixture_git(root, 'add', '--', checker.RECORD_PATH)
    if extra_path:
        (root / 'unexpected.txt').write_text('extra C2 path\n')
        fixture_git(root, 'add', '--', 'unexpected.txt')
    state['publication'] = fixture_commit(root)
    state['document'] = document
    return state


def test_real_clean_builds_c1_c2_gitlink_and_remote_reads(real_environment_factory, capsys) -> None:
    state = publish_fixture(real_environment_factory())
    assert checker.check(state['root'], state['document']) == state['document']
    context = state['document']['buildsCatalog']
    assert context['clean'] and context['catalogCommitted'] and context['remoteAvailable']
    assert context['headCommit'] == context['recordedCommit'] == state['owning']
    assert checker.main(['--repository', str(state['root']), '--candidate', state['candidate']]) == 0
    assert json.loads(capsys.readouterr().out)['result'] == 'PASS'


@pytest.mark.parametrize('kind', ['tracked', 'untracked'])
def test_real_dirty_builds_cannot_pass(real_environment_factory, kind: str) -> None:
    state = real_environment_factory()
    target = state['builds'] / (checker.CATALOG_PATH if kind == 'tracked' else 'untracked.txt')
    target.write_bytes(target.read_bytes() + b'\n' if target.exists() else b'untracked\n')
    document = checker.render(state['root'], state['candidate'])
    assert document['result'] == 'BLOCKED'
    assert 'REFRESH_BUILDS_COMMIT_REQUIRED' in {b['code'] for b in document['blockers']}


def test_real_remote_containment_is_required(real_environment_factory) -> None:
    state = real_environment_factory()
    fixture_git(state['builds'], 'update-ref', '-d', 'refs/remotes/origin/main')
    document = checker.render(state['root'], state['candidate'])
    assert document['result'] == 'BLOCKED'
    assert 'REFRESH_REMOTE_COMMIT_UNAVAILABLE' in {b['code'] for b in document['blockers']}


def test_real_gitlink_must_equal_builds_head(real_environment_factory) -> None:
    state = real_environment_factory()
    fixture_git(state['builds'], 'checkout', '--quiet', '--detach', state['old_head'])
    document = checker.render(state['root'], state['candidate'])
    assert document['result'] == 'BLOCKED'
    assert 'REFRESH_GITLINK_COMMIT_REQUIRED' in {b['code'] for b in document['blockers']}


def test_real_extra_history_before_c1_is_rejected(real_environment_factory) -> None:
    state = real_environment_factory(extra_parent=True)
    with pytest.raises(checker.RefreshError, match='REFRESH_C1_PARENT_DRIFT'):
        checker.render(state['root'], state['candidate'])


def test_real_candidate_only_and_writer_stay_blocked_until_c2(real_environment_factory, capsys) -> None:
    state = real_environment_factory()
    args = ['--repository', str(state['root']), '--candidate', state['candidate']]
    for tail in [[], ['--write']]:
        assert checker.main(args + tail) == 1
        result = json.loads(capsys.readouterr().out)
        assert result['result'] == 'BLOCKED' and result['assertionLedger']
        assert result['blockers'][0]['code'] == 'REFRESH_C2_PUBLICATION_REQUIRED'


def test_real_extra_c2_paths_are_rejected(real_environment_factory) -> None:
    state = publish_fixture(real_environment_factory(), extra_path=True)
    with pytest.raises(checker.RefreshError, match='REFRESH_C2_SCOPE_DRIFT'):
        checker.check(state['root'], state['document'])


def test_real_intervening_commit_before_c2_is_rejected(real_environment_factory) -> None:
    state = real_environment_factory()
    (state['root'] / 'intervening.txt').write_text('intervening history\n')
    fixture_git(state['root'], 'add', '--', 'intervening.txt')
    fixture_commit(state['root'])
    publish_fixture(state)
    with pytest.raises(checker.helpers.PackageAuthorityError, match='REFRESH_C2_PARENT_DRIFT'):
        checker.check(state['root'], state['document'])


def test_real_accepted_record_writer_preserves_exact_bytes(real_environment_factory, capsys) -> None:
    state = publish_fixture(real_environment_factory())
    record = state['root'] / checker.RECORD_PATH
    original = record.read_bytes()
    assert checker.main(['--repository', str(state['root']), '--candidate', state['candidate'], '--write']) == 1
    result = json.loads(capsys.readouterr().out)
    assert result['blockers'][0]['code'] == 'REFRESH_ACCEPTED_RECORD_IMMUTABLE'
    assert record.read_bytes() == original


def test_real_candidate_helper_bytes_must_match_effective_helper(real_environment_factory) -> None:
    state = real_environment_factory(helper_drift=True)
    with pytest.raises(checker.RefreshError, match='REFRESH_HELPER_DRIFT'):
        checker.render(state['root'], state['candidate'])


@pytest.mark.parametrize('fault,code', [
    ('stale-audit', 'REFRESH_BUILDS_AUDIT_CATALOG_DRIFT'), ('raw-hash', 'REFRESH_BUILDS_AUDIT_CATALOG_DRIFT'),
    ('selection', 'REFRESH_BUILDS_AUDIT_SELECTION_DRIFT'), ('revision-alias', 'REFRESH_BUILDS_AUDIT_REVISION_INVALID'),
    ('revision-catalog', 'REFRESH_BUILDS_AUDIT_REVISION_CATALOG_DRIFT'),
    ('revision-ancestry', 'REFRESH_BUILDS_AUDIT_REVISION_NOT_ANCESTOR'),
])
def test_real_committed_audit_provenance_is_required(real_environment_factory, fault: str, code: str) -> None:
    state = real_environment_factory(audit_fault=fault)
    assert fixture_git(state['builds'], 'status', '--porcelain') == ''
    with pytest.raises(checker.helpers.PackageAuthorityError, match=code):
        checker.render(state['root'], state['candidate'])
