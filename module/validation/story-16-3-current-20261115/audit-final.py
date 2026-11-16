"""Recompute parent-final-audit.json and scoped-checks.json from the retained lane files."""
import hashlib
import importlib.util
import json
import pathlib
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('story163_native', HERE/'run-native.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


def lane_audit(lane, current):
    native = runner.verify_native(HERE/(lane+'.xml'), runner.EXPECTED_TOTALS[lane], runner.CLASSES[lane])
    retained = json.loads((HERE/(lane+'-native-result.json')).read_text())
    identity = json.loads((HERE/(lane+'-source-identity.json')).read_text())
    run_result = json.loads((HERE/(lane+'-run-result.json')).read_text())
    counts = native['counts']
    return {
        'passed': counts['passed'], 'failed': counts['failed'], 'skipped': counts['skipped'], 'notRun': counts['not-run'],
        'expected': runner.EXPECTED_TOTALS[lane],
        'retainedResultMatchesXml': retained == native,
        'runResult': run_result.get('result'),
        'currentSources': 'PASS' if identity == current else 'FAIL',
        'selectedClasses': 'PASS' if not native['missingClasses'] else 'FAIL',
    }


def git_check(owner):
    result = subprocess.run(['git', 'diff', '--check'], cwd=owner, capture_output=True, text=True)
    return {'command': 'git diff --check', 'repository': str(owner.relative_to(runner.ROOT)) or '.',
        'exitCode': result.returncode, 'output': result.stdout + result.stderr}


def main():
    current = runner.source_identity()
    lanes = {lane: lane_audit(lane, current) for lane in runner.CLASSES}
    controls = json.loads((HERE/'runner-controls-result.json').read_text())
    empty = json.loads((HERE/'empty-selection-control.json').read_text())
    sources = {name: hashlib.sha256((HERE/name).read_bytes()).hexdigest()
        for name in ['run-native.py', 'test_native_runner.py', 'audit-final.py']}
    lanes_pass = all(item['currentSources'] == item['selectedClasses'] == item['runResult'] == 'PASS'
        and item['retainedResultMatchesXml'] and item['passed'] == item['expected'] for item in lanes.values())
    empty_pass = empty.get('status') == 'REJECTED' and empty.get('xunitExitCode') == 0
    controls_pass = controls.get('valid') is True and controls.get('failures') == controls.get('errors') == 0
    checks = [git_check(runner.ROOT), git_check(runner.ROOT/'references/Hexalith.EventStore')]
    audit = {**lanes, 'nativePassed': sum(item['passed'] for item in lanes.values()),
        'runnerControlsPassed': controls.get('tests') if controls_pass else 0,
        'emptySelectionControl': 'REJECTED' if empty_pass else 'INVALID',
        'result': 'PASS' if lanes_pass and empty_pass and controls_pass and all(c['exitCode'] == 0 for c in checks) else 'FAIL',
        'formalEvidenceGate': 'waived', 'v9FinalRecordGate': 'waived', 'defaultKeycloakBaseline': 'BLOCKED',
        'runnerSources': sources}
    (HERE/'parent-final-audit.json').write_text(json.dumps(audit, indent=2)+'\n')
    (HERE/'scoped-checks.json').write_text(json.dumps(checks, indent=2)+'\n')
    print(json.dumps({'result': audit['result'], 'nativePassed': audit['nativePassed'],
        'runnerControlsPassed': audit['runnerControlsPassed']}))
    return 0 if audit['result'] == 'PASS' else 1


if __name__ == '__main__':
    sys.exit(main())
