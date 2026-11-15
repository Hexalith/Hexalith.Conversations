"""Retain routine native proof; all mapping/build scratch stays inside run-with-temp."""
import fnmatch
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parents[3]
LOGS = pathlib.Path(__file__).resolve().parent
CLASSES = {
    'diagnostics': ['*DaprTestPrerequisiteDiagnosticsTests', '*GeneratedApiSmokePreflightDiagnosticsTests'],
    'apphost': ['*ConversationsAppHostRuntimeBoundaryTest'],
    'integration': ['*Story16_3EvidenceTests'],
    'regression': ['*ConversationProjectionGatewayDispatchLiveTests', '*ConversationGatewaySecurityTests',
        '*ConversationGatewayAdmissionTests', '*ConversationGatewayRouteCatalogPolicyTests', '*ConversationGatewayCommandAuthorityTests'],
    'server': ['*ConversationAsyncProjectionHandlerTest', '*ConversationProjectionReadModelPersistenceTest',
        '*ConversationProjectionConfigurationTest', '*ConversationTenantIndexLifecycleTest', '*ConversationTenantAccessRegistrationTest'],
}


def archive_files(paths, directory, label):
    existing = [path for path in paths if path.is_file()]
    if not existing:
        return
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    target = directory / 'attempts' / f'{label}-{stamp}-{uuid.uuid4().hex[:8]}'
    target.mkdir(parents=True)
    for path in existing:
        shutil.move(str(path), target / path.name)


def invalidate_prior_results(directory, mode):
    """Move a lane's previous outputs together before writing any new source identity."""
    if mode == 'baseline':
        # Keep the separately collected Keycloak/security blocker and state snapshots visible.
        names = ['baseline-commands.json', 'baseline-source-identity.json', 'baseline-result.json', 'baseline-run-result.json',
            'baseline-prepare.log', 'baseline-before.log', 'baseline-start.log', 'baseline-describe.log', 'baseline-stop.log', 'baseline-after.log',
            'baseline-wait-eventstore.log', 'baseline-wait-conversations.log', 'baseline-wait-conversations-admin-web.log']
        paths = [directory / name for name in names]
    else:
        paths = [*directory.glob(mode + '-*.json'), *directory.glob(mode + '-*.log'), directory / (mode + '.xml')]
    if mode == 'integration':
        paths += [*directory.glob('empty-selection*')]
    archive_files(sorted(set(paths)), directory, mode + '-prior')


def verify_native(path, expected, selected_classes):
    """Require native rows, consistent counters and representation of every selected class."""
    tree = ET.parse(path)
    assemblies = tree.getroot().findall('assembly')
    keys = ['total', 'passed', 'failed', 'skipped', 'not-run', 'errors']
    totals = dict.fromkeys(keys, 0)
    tests = []
    seen_ids = set()
    valid = tree.getroot().tag == 'assemblies' and bool(assemblies) and bool(selected_classes)
    for assembly in assemblies:
        counts = {key: int(assembly.get(key, '0')) for key in keys}
        rows = assembly.findall('.//test')
        valid &= counts['total'] == len(rows) == counts['passed']
        valid &= not any(counts[key] for key in ['failed', 'skipped', 'not-run', 'errors']) and not assembly.findall('.//error')
        for row in rows:
            test = {key: row.get(key) for key in ['id', 'name', 'type', 'method', 'result']}
            valid &= all(test.values()) and test['result'] == 'Pass' and row.find('failure') is None
            valid &= test['id'] not in seen_ids
            seen_ids.add(test['id'])
            valid &= any(fnmatch.fnmatchcase(test['type'] or '', pattern) for pattern in selected_classes)
            tests.append(test)
        for key in keys:
            totals[key] += counts[key]
    missing = [pattern for pattern in selected_classes
        if not any(fnmatch.fnmatchcase(test['type'] or '', pattern) for test in tests)]
    valid &= bool(tests) and not missing and (expected is None or len(tests) == expected)
    result = {'counts': totals, 'tests': tests, 'selectedClasses': selected_classes, 'missingClasses': missing, 'valid': bool(valid)}
    if not valid:
        raise RuntimeError('Native result is empty, inconsistent, partial or unsuccessful: ' + str(totals) + '; missing classes=' + str(missing))
    return result


def baseline_result(before, after, start_exit, readiness, cleanup_exit, inventory_exit=0):
    """Keep a failed readiness/cleanup attempt distinct from native proof."""
    reasons = []
    if start_exit != 0:
        reasons.append('START_FAILED')
    if set(readiness) != {'eventstore', 'conversations', 'conversations-admin-web'} or any(code != 0 for code in readiness.values()):
        reasons.append('READINESS_FAILED')
    if cleanup_exit != 0:
        reasons.append('CLEANUP_FAILED')
    equal = isinstance(before, list) and isinstance(after, list) and before == after
    if inventory_exit != 0 or not equal:
        reasons.append('INVENTORY_CHANGED_OR_UNAVAILABLE')
    return {'result': 'BLOCKED' if reasons else 'PASS', 'exitCode': 2 if reasons else 0,
        'reasons': reasons, 'readiness': readiness, 'beforeInventory': before, 'afterInventory': after,
        'inventoryEqual': equal, 'cleanupExitCode': cleanup_exit,
        'separateRetainedKeycloakBlocker': 'Existing realm worker-client description is 300 characters; Keycloak DESCRIPTION permits 255. See baseline-security-logs.json and README.md.'}


def source_identity():
    def git(*args, owner=ROOT):
        return subprocess.check_output(['git', *args], cwd=owner, text=True).strip()
    paths = [ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration'/name
        for name in ['DaprDiagnostics.cs', 'DaprReadModelStoreDecorator.cs']]
    paths += list((ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Integration.Tests').glob('*DiagnosticsTests.cs'))
    paths += list((ROOT/'tests/Hexalith.Conversations.IntegrationTests/Epic16').glob('Story16_3*.cs'))
    paths += [ROOT/'tests/Hexalith.Conversations.IntegrationTests/Projections'/name for name in ['ConversationGatewayLiveFixture.cs', 'ConversationGatewayRouteCatalog.cs']]
    paths += [ROOT/'tests/Hexalith.Conversations.AppHost.Tests'/name for name in ['ConversationsAppHostRuntimeBoundaryTest.cs', 'Hexalith.Conversations.AppHost.Tests.csproj']]
    return {'rootCommit': git('rev-parse', 'HEAD'), 'rawGitlinks': git('ls-files', '--stage', 'references'),
        'eventStoreCommit': git('rev-parse', 'HEAD', owner=ROOT/'references/Hexalith.EventStore'),
        'files': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(set(paths))}}


def main(mode):
    if mode not in [*CLASSES, 'baseline']:
        raise ValueError('Unknown native lane: ' + mode)
    scratch = pathlib.Path(os.environ['TMPDIR'])
    env = os.environ.copy()
    ledger = []
    invalidate_prior_results(LOGS, mode)
    identity = source_identity()
    (LOGS/(mode+'-source-identity.json')).write_text(json.dumps(identity, indent=2)+'\n')

    def run(args, label, required=True):
        path = LOGS/(label+'.log')
        with path.open('w') as output:
            result = subprocess.run(args, cwd=ROOT, env=env, stdout=output, stderr=subprocess.STDOUT)
        if label.startswith('baseline'):
            path.write_text(re.sub(r'login[?]t=[a-zA-Z0-9_-]+', 'login?t=[redacted-token]', path.read_text()))
        ledger.append({'command': args, 'cwd': str(ROOT), 'exitCode': result.returncode, 'log': path.name})
        (LOGS/(mode+'-commands.json')).write_text(json.dumps(ledger, indent=2)+'\n')
        print(label, result.returncode, flush=True)
        if required and result.returncode:
            raise RuntimeError(label + ' failed with exit ' + str(result.returncode))
        return result

    try:
        if mode != 'diagnostics':
            run(['dotnet', 'tool', 'run', 'hexalith-module', '--', 'prepare-build', '--manifest', 'module/hexalith.module-manifest.v2.json',
                '--workspace', str(scratch/'workspace'), '--mode', 'source', '--output', 'json'], mode+'-prepare')
            prepared = json.loads((LOGS/(mode+'-prepare.log')).read_text())
            env.update(prepared['buildPreparation']['environment'])
            env.update(ArtifactsPath=str(scratch), NuGetAudit='false', HEXALITH_RUN_APPHOST_BOUNDARY_TESTS='true')
        if mode == 'baseline':
            apphost = 'src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj'
            run(['aspire', 'ps', '--format', 'Json', '--non-interactive'], 'baseline-before')
            before = json.loads((LOGS/'baseline-before.log').read_text())
            if not isinstance(before, list) or before:
                raise RuntimeError('Baseline will not disturb an existing Aspire session.')
            start_exit = -1
            readiness = {}
            cleanup_exit = -1
            after = None
            inventory_exit = -1
            try:
                start_exit = run(['aspire', 'start', '--apphost', apphost, '--isolated', '--format', 'Json', '--non-interactive'], 'baseline-start', required=False).returncode
                if start_exit == 0:
                    run(['aspire', 'describe', '--apphost', apphost, '--format', 'Json', '--non-interactive'], 'baseline-describe', required=False)
                    for resource in ['eventstore', 'conversations', 'conversations-admin-web']:
                        readiness[resource] = run(['aspire', 'wait', resource, '--apphost', apphost, '--status', 'healthy', '--timeout', '120', '--non-interactive'],
                            'baseline-wait-'+resource, required=False).returncode
            finally:
                try:
                    cleanup_exit = run(['aspire', 'stop', '--apphost', apphost, '--non-interactive'], 'baseline-stop', required=False).returncode
                finally:
                    inventory_exit = run(['aspire', 'ps', '--format', 'Json', '--non-interactive'], 'baseline-after', required=False).returncode
                    if inventory_exit == 0:
                        try:
                            after = json.loads((LOGS/'baseline-after.log').read_text())
                        except (ValueError, OSError):
                            inventory_exit = 2
                    result = baseline_result(before, after, start_exit, readiness, cleanup_exit, inventory_exit)
                    (LOGS/'baseline-result.json').write_text(json.dumps(result, indent=2)+'\n')
            return result['exitCode']

        project_name = ('Hexalith.EventStore.Testing.Integration.Tests' if mode == 'diagnostics' else
            'Hexalith.Conversations.AppHost.Tests' if mode == 'apphost' else
            'Hexalith.Conversations.Server.Tests' if mode == 'server' else 'Hexalith.Conversations.IntegrationTests')
        owner = ROOT/'references/Hexalith.EventStore' if mode == 'diagnostics' else ROOT
        project = owner/'tests'/project_name/(project_name+'.csproj')
        build = ['dotnet', 'build', str(project), '-c', 'Debug', '-m:1', f'-p:ArtifactsPath={scratch}', '-p:NuGetAudit=false']
        if mode == 'diagnostics':
            build.append('-p:UseHexalithProjectReferences=true')
        run(build, mode+'-build')
        assembly = next((scratch/'bin'/project_name).rglob(project_name+'.dll'))
        names = [assembly.name, 'Hexalith.EventStore.Testing.Integration.dll', 'Hexalith.EventStore.Server.dll',
            'Hexalith.EventStore.DomainService.dll', 'Hexalith.Conversations.Server.dll', 'Hexalith.Tenants.Client.dll']
        (LOGS/(mode+'-assembly-identity.json')).write_text(json.dumps({name: hashlib.sha256((assembly.parent/name).read_bytes()).hexdigest()
            for name in names if (assembly.parent/name).exists()}, indent=2)+'\n')
        classes = CLASSES[mode]
        args = ['dotnet', str(assembly)]
        for cls in classes:
            args += ['-class', cls]
        run(args+['-automated', 'sync', '-failSkips', '-diagnostics', '-parallel', 'none', '-xml', str(LOGS/(mode+'.xml'))], mode+'-native')
        expected = {'diagnostics': 43, 'apphost': 2, 'integration': 5}.get(mode)
        native = verify_native(LOGS/(mode+'.xml'), expected, classes)
        if mode == 'integration':
            empty = LOGS/'empty-selection.xml'
            run(['dotnet', str(assembly), '-class', '*Story16_3_NoTests*', '-automated', 'sync', '-failSkips', '-xml', str(empty)], 'empty-selection', required=False)
            try:
                verify_native(empty, 5, ['*Story16_3EvidenceTests'])
            except (RuntimeError, FileNotFoundError):
                (LOGS/'empty-selection-control.json').write_text(json.dumps({'status': 'REJECTED', 'reason': 'EMPTY_TEST_SELECTION', 'expectedTests': 5})+'\n')
            else:
                raise RuntimeError('Empty selection was accepted')
        if source_identity() != identity:
            raise RuntimeError('Source changed during the native run; rerun before claiming proof.')
        (LOGS/(mode+'-native-result.json')).write_text(json.dumps(native, indent=2)+'\n')
        (LOGS/(mode+'-run-result.json')).write_text(json.dumps({'result': 'PASS', 'exitCode': 0})+'\n')
        return 0
    except Exception as error:
        # A failed attempt can retain raw observations, but no active PASS carrier.
        archive_files([LOGS/(mode+'.xml'), LOGS/(mode+'-native-result.json')], LOGS, mode+'-rejected')
        (LOGS/(mode+'-run-result.json')).write_text(json.dumps({'result': 'FAIL', 'exitCode': 1,
            'reason': type(error).__name__ + ': ' + str(error)})+'\n')
        raise


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
