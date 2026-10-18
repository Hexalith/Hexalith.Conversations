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
        if source.count('and not collision') != 1:
            raise RuntimeError('The collision guard must have exactly one mutation target')
        namespace = {}
        exec(compile(source.replace('and not collision', ''), '<hardlink-mutant>', 'exec'),
             adoption.__dict__, namespace)
        self.adoption, self.original = adoption, adoption.main
        adoption.main = namespace['main']


def main():
    arguments = ['-q', '-p', 'no:cacheprovider',
                 'tests/tooling/test_evidence_boundary_current.py',
                 '-k', 'adoption_output_hardlink']
    if pytest.main(arguments) != pytest.ExitCode.OK:
        raise RuntimeError('The unmodified regression must pass first')
    plugin = DisableCollisionGuard()
    try:
        code = pytest.main(arguments, plugins=[plugin])
    finally:
        if hasattr(plugin, 'original'):
            plugin.adoption.main = plugin.original
    if code != pytest.ExitCode.TESTS_FAILED:
        raise RuntimeError(f'Expected a failing mutation, got {code}')
    print('PASS: the existing hardlink regression rejects the isolated collision-guard mutation.')


if __name__ == '__main__':
    main()
