"""Require the six frozen fixture tests and the registered-command check to pass."""

from collections import Counter
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def verify(path: Path) -> None:
    """Fail on missing, duplicate, skipped, failed, or undiscovered checks."""
    prefix = "Hexalith.Conversations.MinimalModule.Tests."
    expected = {
        prefix + "MinimalModuleFixtureTests." + method
        for method in (
            "Registration_ExposesTheModuleHandlersThroughThePlatformHost",
            "CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay",
            "Projection_IsDispatchedByThePlatformDispatcher",
            "Query_IsDispatchedByThePlatformDispatcher",
            "SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly",
            "PublishedEvent_IsConsumedThroughThePlatformProcessor",
        )
    }
    expected.add(prefix + "RegisteredCommandTests.RegisteredCommandProducesCreatedEventAndIsIdempotentAfterReplay")
    root = ET.parse(path).getroot()
    assemblies = root.findall("assembly")
    tests = root.findall("./assembly/collection/test")
    names = [test.get("name") for test in tests]
    if (
        root.tag != "assemblies"
        or len(assemblies) != 1
        or len(names) != len(expected)
        or set(names) != expected
        or any(test.get("result") != "Pass" for test in tests)
        or assemblies[0].get("total") != "7"
        or assemblies[0].get("passed") != "7"
        or assemblies[0].get("failed") != "0"
        or assemblies[0].get("skipped") != "0"
        or assemblies[0].get("errors") != "0"
    ):
        raise ValueError(
            "Expected all six frozen tests and the registered-command test: 7 passed, 0 failed/skipped/errors; "
            f"document={root.tag!r}; assemblies={len(assemblies)}; "
            f"missing={sorted(expected - set(names))}; "
            f"unexpected={sorted(set(names) - expected, key=str)}; "
            f"duplicates={[name for name, count in Counter(names).items() if count > 1]}; "
            f"unsuccessful={[(test.get('name'), test.get('result')) for test in tests if test.get('result') != 'Pass']}; "
            f"counters={[{key: assembly.get(key) for key in ('total', 'passed', 'failed', 'skipped', 'errors')} for assembly in assemblies]}"
        )
    print("Minimal-module platform compatibility: 7 passed, 0 failed/skipped/errors")


if __name__ == "__main__":
    verify(Path(sys.argv[1]))
