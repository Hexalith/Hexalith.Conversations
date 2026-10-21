"""Fault controls for the live minimal-module compatibility result gate."""

from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET


SCRIPT = Path(__file__).resolve().parents[1] / "compatibility/MinimalModule/verify_results.py"
SPEC = spec_from_file_location("minimal_module_compatibility_results", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
checker = module_from_spec(SPEC)
SPEC.loader.exec_module(checker)


class MinimalModuleCompatibilityResultsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.path = Path(self.temporary.name) / "results.xml"
        self.root = ET.Element("assemblies")
        self.assembly = ET.SubElement(
            self.root, "assembly", total="7", passed="7", failed="0", skipped="0", errors="0"
        )
        self.collection = ET.SubElement(self.assembly, "collection")
        for name in (
            "MinimalModuleFixtureTests.Registration_ExposesTheModuleHandlersThroughThePlatformHost",
            "MinimalModuleFixtureTests.CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay",
            "MinimalModuleFixtureTests.Projection_IsDispatchedByThePlatformDispatcher",
            "MinimalModuleFixtureTests.Query_IsDispatchedByThePlatformDispatcher",
            "MinimalModuleFixtureTests.SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly",
            "MinimalModuleFixtureTests.PublishedEvent_IsConsumedThroughThePlatformProcessor",
            "RegisteredCommandTests.RegisteredCommandProducesCreatedEventAndIsIdempotentAfterReplay",
        ):
            ET.SubElement(self.collection, "test", name="Hexalith.Conversations.MinimalModule.Tests." + name, result="Pass")

    def verify(self) -> None:
        ET.ElementTree(self.root).write(self.path, encoding="utf-8")
        checker.verify(self.path)

    def test_accepts_complete_passing_suite(self) -> None:
        self.verify()

    def test_rejects_missing_registered_test_even_with_green_counters(self) -> None:
        self.collection.remove(self.collection[-1])
        with self.assertRaisesRegex(ValueError, r"missing=.*RegisteredCommandTests"):
            self.verify()

    def test_rejects_duplicate_test_replacing_a_required_test(self) -> None:
        self.collection[-1].set("name", self.collection[0].get("name"))
        with self.assertRaisesRegex(ValueError, r"duplicates=.*Registration_Exposes"):
            self.verify()

    def test_rejects_failed_skipped_or_unknown_outcomes_with_green_counters(self) -> None:
        for result in ("Fail", "Skip", "", "Unknown"):
            with self.subTest(result=result):
                self.collection[-1].set("result", result)
                with self.assertRaisesRegex(ValueError, r"unsuccessful=.*RegisteredCommandTests"):
                    self.verify()

    def test_rejects_bad_counters_even_with_passing_results(self) -> None:
        for attribute, value in (("total", "0"), ("passed", "6"), ("failed", "1"), ("skipped", "1"), ("errors", "1")):
            with self.subTest(attribute=attribute):
                self.assembly.set(attribute, value)
                with self.assertRaisesRegex(ValueError, "counters=.*'" + attribute + "': '" + value + "'"):
                    self.verify()
                self.assembly.set(attribute, "7" if attribute in ("total", "passed") else "0")

    def test_rejects_multiple_assemblies(self) -> None:
        ET.SubElement(self.root, "assembly")
        with self.assertRaises(ValueError):
            self.verify()

    def test_rejects_wrong_document_shape(self) -> None:
        self.root.tag = "TestRun"
        with self.assertRaises(ValueError):
            self.verify()

    def test_rejects_malformed_and_missing_results(self) -> None:
        with self.assertRaises(FileNotFoundError):
            checker.verify(self.path)
        self.path.write_text("<assemblies>", encoding="utf-8")
        with self.assertRaises(ET.ParseError):
            checker.verify(self.path)


if __name__ == "__main__":
    unittest.main()
