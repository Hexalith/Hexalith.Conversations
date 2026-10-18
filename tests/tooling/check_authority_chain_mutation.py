"""Prove all seven real authority faults fail with the compiled guard disabled.

Only temporary helper sources and an in-memory bridge target are changed.
Workspace source, authority bytes and accepted receipts are never rewritten.
"""
from __future__ import annotations

from pathlib import Path
import shutil
import tempfile

from check_adoption_hardlink_mutation import run_regressions


class DisableAuthorityGuard:
    def __init__(self, project, assembly):
        self.project, self.assembly = str(project), str(assembly)

    def pytest_collection_modifyitems(self, items):
        tests = [item for item in items if item.originalname == 'test_complete_fault_matrix_chain']
        if len(tests) != 7:
            raise RuntimeError('The authority mutation must select exactly seven real fault cases')
        self.current = tests[0].obj.__globals__['current']
        self.original = self.current.PROJECT, self.current.ASSEMBLY
        self.current.PROJECT, self.current.ASSEMBLY = self.project, self.assembly

    def restore(self):
        if hasattr(self, 'original'):
            self.current.PROJECT, self.current.ASSEMBLY = self.original


def main():
    arguments = ['-q', '-p', 'no:cacheprovider',
                 'tests/tooling/test_evidence_boundary_current.py',
                 '-k', 'test_complete_fault_matrix_chain']
    run_regressions(arguments, 7)
    root = Path(__file__).resolve().parents[2]
    with tempfile.TemporaryDirectory(prefix='authority-chain-mutant-') as temporary:
        isolated = Path(temporary)
        for relative in ('global.json', 'Directory.Build.props', 'tests/Directory.Build.props'):
            target = isolated / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(root / relative, target)
        for name in ('Hexalith.Conversations.TestSupport', 'Hexalith.Conversations.TestSupport.Runner'):
            relative = Path('tests') / name
            shutil.copytree(root / relative, isolated / relative,
                            ignore=shutil.ignore_patterns('bin', 'obj'))
        source_path = isolated / 'tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs'
        source = source_path.read_text(encoding='utf-8')
        signature = '    public static bool AuthorityChain('
        following = '    /// <summary>Records whether an expected value equals an observed value.</summary>'
        if source.count(signature) != 1 or source.count(following) != 1:
            raise RuntimeError('The compiled authority guard must have exactly one mutation target')
        start, end = source.index(signature), source.index(following)
        if end <= start:
            raise RuntimeError('The compiled authority guard mutation boundaries changed')
        mutant = '''    public static bool AuthorityChain(AssertionLedger ledger, string identity, string source,
        IEnumerable<(string MarkerFamily, string Version, string Supersedes)> expectedRows)
    {
        ledger.Record(identity + ":markers", identity, true);
        ledger.Record(identity + ":rows", identity, true);
        return true;
    }

'''
        source_path.write_bytes((source[:start] + mutant + source[end:]).replace('\n', '\r\n').encode('utf-8'))
        runner = isolated / 'tests/Hexalith.Conversations.TestSupport.Runner'
        plugin = DisableAuthorityGuard(runner / 'Hexalith.Conversations.TestSupport.Runner.csproj',
                                      runner / 'bin/Debug/net10.0/Hexalith.Conversations.TestSupport.Runner.dll')
        try:
            run_regressions(arguments, 7, mutant=True,
                            failure_message='Authority-chain fault must be rejected by the compiled guard',
                            plugins=[plugin])
        finally:
            plugin.restore()
    print('PASS: baseline 7/7; disabling the compiled authority guard fails all seven fault assertions.')


if __name__ == '__main__':
    main()
