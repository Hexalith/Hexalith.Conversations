"""Focused negative controls for the retained native proof runner."""
import importlib.util
import json
import os
import sys
import subprocess
import sys
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

# Keep dynamic-import bytecode out of the retained source/evidence directory.
sys.dont_write_bytecode = True

spec = importlib.util.spec_from_file_location('story163_native', Path(__file__).with_name('run-native.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class NativeRunnerControls(unittest.TestCase):
    def setUp(self):
        self.workspace = tempfile.TemporaryDirectory(dir=os.environ['TMPDIR'])
        self.directory = Path(self.workspace.name)
        self.addCleanup(self.workspace.cleanup)

    def document(self, classes=('Fixture.First', 'Fixture.Second')):
        root = ET.Element('assemblies')
        assembly = ET.SubElement(root, 'assembly', total=str(len(classes)), passed=str(len(classes)),
            failed='0', skipped='0', errors='0', **{'not-run': '0'})
        collection = ET.SubElement(assembly, 'collection')
        for index, name in enumerate(classes):
            ET.SubElement(collection, 'test', id=str(index), name=name+'.Checks', type=name, method='Checks', result='Pass')
        return ET.ElementTree(root)

    def verify(self, tree, expected=None, classes=('*First', '*Second')):
        path = self.directory/'native.xml'
        tree.write(path)
        return runner.verify_native(path, expected, list(classes))

    def test_complete_rows_and_each_selected_class_are_accepted(self):
        self.assertTrue(self.verify(self.document(), 2)['valid'])

    def test_forged_pass_counters_without_rows_are_rejected(self):
        tree = self.document(())
        assembly = tree.find('assembly')
        assembly.set('total', '2')
        assembly.set('passed', '2')
        with self.assertRaises(RuntimeError):
            self.verify(tree, 2)

    def test_missing_row_is_rejected_even_when_counters_claim_pass(self):
        tree = self.document()
        collection = tree.find('assembly/collection')
        collection.remove(collection[-1])
        with self.assertRaises(RuntimeError):
            self.verify(tree, 2)

    def test_failed_row_is_rejected_even_when_counters_claim_pass(self):
        tree = self.document()
        tree.find('.//test').set('result', 'Fail')
        with self.assertRaises(RuntimeError):
            self.verify(tree, 2)

    def test_partial_selected_class_coverage_is_rejected_without_fixed_total(self):
        with self.assertRaises(RuntimeError):
            self.verify(self.document(('Fixture.First',)))

    def test_duplicate_native_row_ids_are_rejected(self):
        tree = self.document()
        tree.findall('.//test')[1].set('id', '0')
        with self.assertRaises(RuntimeError):
            self.verify(tree, 2)

    def test_collection_error_cannot_hide_behind_zero_error_counter(self):
        tree = self.document()
        ET.SubElement(ET.SubElement(tree.find('assembly'), 'errors'), 'error')
        with self.assertRaises(RuntimeError):
            self.verify(tree, 2)

    def test_rerun_archives_prior_pass_with_its_original_identity(self):
        originals = {'integration.xml': '<old-pass/>', 'integration-native-result.json': '{"valid":true}',
            'integration-source-identity.json': '{"revision":"old"}', 'empty-selection-control.json': '{"old":true}'}
        for name, value in originals.items():
            (self.directory/name).write_text(value)
        runner.invalidate_prior_results(self.directory, 'integration')
        for name, value in originals.items():
            self.assertFalse((self.directory/name).exists())
            archived = list((self.directory/'attempts').rglob(name))
            self.assertEqual([path.read_text() for path in archived], [value])
        (self.directory/'integration-source-identity.json').write_text('{"revision":"new"}')
        self.assertFalse((self.directory/'integration-native-result.json').exists())
        self.assertFalse((self.directory/'integration.xml').exists())

    def test_baseline_archiving_keeps_the_separate_keycloak_blocker_visible(self):
        blocker = self.directory/'baseline-security-logs.json'
        blocker.write_text('existing Keycloak description too long')
        (self.directory/'baseline-result.json').write_text('{"result":"PASS"}')
        runner.invalidate_prior_results(self.directory, 'baseline')
        self.assertTrue(blocker.exists())
        self.assertFalse((self.directory/'baseline-result.json').exists())

    def baseline(self, readiness=None, cleanup=0, after=None):
        return runner.baseline_result([], [] if after is None else after, 0,
            {'eventstore': 0, 'conversations': 0, 'conversations-admin-web': 0} if readiness is None else readiness, cleanup)

    def test_failed_readiness_reports_blocked_and_nonzero_with_equal_inventories(self):
        result = self.baseline({'eventstore': 17, 'conversations': 17, 'conversations-admin-web': 17})
        self.assertEqual((result['result'], result['exitCode'], result['inventoryEqual']), ('BLOCKED', 2, True))
        self.assertIn('READINESS_FAILED', result['reasons'])
        self.assertIn('Keycloak', result['separateRetainedKeycloakBlocker'])

    def test_failed_cleanup_reports_blocked_even_when_inventories_match(self):
        result = self.baseline(cleanup=1)
        self.assertEqual(result['exitCode'], 2)
        self.assertTrue(result['inventoryEqual'])
        self.assertIn('CLEANUP_FAILED', result['reasons'])

    def test_inventory_drift_reports_blocked_and_unequal(self):
        result = self.baseline(after=[{'appHostPath': 'leftover'}])
        self.assertEqual(result['exitCode'], 2)
        self.assertFalse(result['inventoryEqual'])

    def test_missing_baseline_readiness_resource_is_rejected(self):
        self.assertEqual(self.baseline({'eventstore': 0})['exitCode'], 2)

    def test_ordinary_exception_record_matches_actual_cli_exit(self):
        child = """
import importlib.util
from pathlib import Path
import sys
from unittest.mock import patch
spec = importlib.util.spec_from_file_location('native_failure_control', sys.argv[1])
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
runner.LOGS = Path(sys.argv[2])
with patch.object(runner, 'source_identity', return_value={}), patch.object(runner.subprocess, 'run', side_effect=RuntimeError('ordinary-failure-control')):
    sys.exit(runner.main('diagnostics'))
"""
        result = subprocess.run([sys.executable, '-B', '-c', child,
            str(Path(__file__).with_name('run-native.py')), str(self.directory)], capture_output=True, text=True)
        recorded = json.loads((self.directory/'diagnostics-run-result.json').read_text())
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertEqual(recorded['result'], 'FAIL')
        self.assertEqual(recorded['exitCode'], result.returncode)

    def test_complete_baseline_with_equal_inventories_reports_success(self):
        result = self.baseline()
        self.assertEqual((result['result'], result['exitCode'], result['inventoryEqual']), ('PASS', 0, True))


if __name__ == '__main__':
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(NativeRunnerControls)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    Path(__file__).with_name('runner-controls-result.json').write_text(json.dumps({
        'tests': result.testsRun, 'failures': len(result.failures), 'errors': len(result.errors),
        'skipped': len(result.skipped), 'valid': result.wasSuccessful()})+'\n')
    raise SystemExit(not result.wasSuccessful())
