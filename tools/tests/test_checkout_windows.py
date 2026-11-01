"""Pure guard controls; native Windows materialization is separately required."""
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("checkout_windows", Path(__file__).parents[1] / "checkout-windows.py")
GUARD = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GUARD)


class CheckoutWindowsControls(unittest.TestCase):
    def test_archive_whitelist_has_only_the_eighteen_reviewed_full_tuples(self):
        self.assertEqual(len(GUARD.ARCHIVES), 18)
        records = {path: (mode, oid) for path, (mode, oid, _) in GUARD.ARCHIVES.items()}
        def size(root, *args):
            oid = args[-1]
            actual = next(size for _, blob, size in GUARD.ARCHIVES.values() if blob.decode() == oid)
            return subprocess.CompletedProcess([], 0, str(actual).encode(), b"")
        with patch.object(GUARD, "run_git", side_effect=size):
            GUARD.require_archives(Path("unused"), records)
            for selected in records:
                self.check_archive_faults(selected, records)

    def check_archive_faults(self, selected, records):
        for fault in ("missing", "renamed-safe", "renamed-unsafe", "blob", "mode"):
            changed = dict(records)
            mode, oid = changed.pop(selected)
            if fault == "renamed-safe": changed[b"archive/legal-name.dat"] = (mode, oid)
            elif fault == "renamed-unsafe": changed[selected + b":extra"] = (mode, oid)
            elif fault == "blob": changed[selected] = (mode, b"0" * 40)
            elif fault == "mode": changed[selected] = (b"100755", oid)
            with self.subTest(fault=fault), self.assertRaises(ValueError): GUARD.require_archives(Path("unused"), changed)

    def test_native_success_exit_with_diagnostics_or_omitted_entries_is_refused(self):
        records = {b"src/Current.cs": (b"100644", b"1" * 40), b".gitmodules": (b"120000", b"2" * 40)}
        valid = b"".join(mode + b" " + oid + b" 0\t" + path + b"\0" for path, (mode, oid) in records.items())
        for warning, observed in ((b"Ignoring path .gitmodules", valid), (b"", b""), (b"", valid.replace(b"120000", b"100644"))):
            with self.subTest(warning=warning, complete=bool(observed)), patch.object(GUARD, "run_git", side_effect=[subprocess.CompletedProcess([], 0, b"", warning), subprocess.CompletedProcess([], 0, observed, b"")]), self.assertRaises(ValueError):
                GUARD.validate_native_index(Path("unused"), records)
        with patch.object(GUARD, "run_git", side_effect=[subprocess.CompletedProcess([], 0, b"", b""), subprocess.CompletedProcess([], 0, valid, b"")]) as calls:
            GUARD.validate_native_index(Path("unused"), records)
            self.assertIn(b"120000 " + b"2" * 40 + b"\t.gitmodules\0", calls.call_args_list[0].kwargs["data"])
            self.assertEqual(calls.call_args_list[0].args[1:], ("update-index", "-z", "--index-info"))

    def test_case_alias_and_file_prefix_checks_are_independent_of_native_index_exit(self):
        def ascii_compare(left, right):return (left.lower() > right.lower()) - (left.lower() < right.lower())
        GUARD.require_distinct_paths({b"src/One.cs": None, b"src/Two.cs": None}, ascii_compare)
        for paths in ((b"src/A.cs", b"src/a.cs"), (b"src/Node", b"src/node/Thing.cs"), (b"src/Foo/A.cs", b"src/foo/B.cs")):
            with self.subTest(paths=paths), self.assertRaises(ValueError):GUARD.require_distinct_paths(dict.fromkeys(paths), ascii_compare)

    def test_index_parser_refuses_unmerged_or_duplicate_entries(self):
        record = b"100644 " + b"1" * 40 + b" 0\tsrc/Current.cs\0"
        for payload in (record + record, record.replace(b" 0\t", b" 2\t")):
            with self.assertRaises(ValueError):GUARD.stage_records(payload)


if __name__ == "__main__":unittest.main()
