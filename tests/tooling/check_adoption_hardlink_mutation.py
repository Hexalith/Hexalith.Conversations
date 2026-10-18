"""Prove the routine hardlink regression fails without the writer's guard.

Run from the repository root with Python. Only an in-memory function is changed;
the regression's disposable repository contains any destructive fixture writes.
Exit zero means the selected regression failed under the intended mutation.
"""
from __future__ import annotations

import inspect

import pytest


class DisableCollisionGuard:
    def pytest_collection_modifyitems(self, items):
        test = next(item for item in items
                    if item.name == 'test_adoption_output_hardlink_cannot_overwrite_a_source')
        adoption = test.obj.__globals__['adoption']
        source = inspect.getsource(adoption.main)
        guards = ('and not collision',
                  'and (target is None or not target.exists() or target.stat().st_nlink==1)')
        for guard in guards:
            if source.count(guard) != 1:
                raise RuntimeError(f'The hardlink guard must have exactly one mutation target: {guard}')
            source = source.replace(guard, '')
        namespace = {}
        exec(compile(source, '<hardlink-mutant>', 'exec'),
             adoption.__dict__, namespace)
        self.adoption, self.original = adoption, adoption.main
        adoption.main = namespace['main']


class RegressionResults:
    """Distinguish killed mutants from collection, fixture and teardown errors."""

    def __init__(self, failure_message):
        self.selected = 0
        self.reports = []
        self.assertion_failures = 0
        self.failure_message = failure_message

    def pytest_collection_finish(self, session):
        self.selected = len(session.items)

    def pytest_runtest_logreport(self, report):
        self.reports.append(report)

    def pytest_runtest_makereport(self, item, call):
        if (call.when == 'call' and call.excinfo is not None and call.excinfo.type is AssertionError
                and self.failure_message and str(call.excinfo.value).startswith(self.failure_message)):
            self.assertion_failures += 1


def run_regressions(arguments, count, *, mutant=False, failure_message=None, plugins=()):
    """Require exact collection and outcomes; pytest errors never prove a mutation."""
    if mutant and not failure_message:
        raise RuntimeError('A mutant run requires the exact guard-rejection assertion message')
    results = RegressionResults(failure_message)
    code = pytest.main(arguments, plugins=[results, *plugins])
    calls = [report for report in results.reports if report.when == 'call']
    failures = [report for report in results.reports if report.failed]
    expected = pytest.ExitCode.TESTS_FAILED if mutant else pytest.ExitCode.OK
    valid = code == expected and results.selected == count and len(calls) == count
    if mutant:
        valid = valid and len(failures) == count and all(report.when == 'call' for report in failures)
        valid = valid and results.assertion_failures == count
    else:
        valid = valid and all(report.passed for report in results.reports)
    if not valid:
        raise RuntimeError(f'Invalid {"mutant" if mutant else "baseline"} run: exit={code}, '
                           f'selected={results.selected}, calls={len(calls)}, failures={len(failures)}, '
                           f'assertion_failures={results.assertion_failures}')


def main():
    arguments = ['-q', '-p', 'no:cacheprovider',
                 'tests/tooling/test_evidence_boundary_current.py',
                 '-k', 'adoption_output_hardlink']
    run_regressions(arguments, 1)
    plugin = DisableCollisionGuard()
    try:
        run_regressions(arguments, 1, mutant=True,
                        failure_message='Hardlink output must be rejected by the writer', plugins=[plugin])
    finally:
        if hasattr(plugin, 'original'):
            plugin.adoption.main = plugin.original
    print('PASS: baseline 1/1; disabling both hardlink protections fails the regression assertion.')


if __name__ == '__main__':
    main()
