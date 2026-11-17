"""Product package inventory, payload, symbol, consumer and mapping contracts."""
from __future__ import annotations

import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest import mock
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))
import release_contract

def load_script_module(name: str, filename: str):
    spec = importlib.util.spec_from_file_location(
        name,
        SCRIPTS / filename,
    )
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load release-source verifier")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


validate_nuget_packages = load_script_module("validate_nuget_packages", "validate-nuget-packages.py")
validate_consumer_packages = load_script_module("validate_consumer_packages", "validate-consumer-package-references.py")


class PackageContractTests(unittest.TestCase):
    def test_exact_manifest_loads_and_unknown_inventory_is_rejected(self) -> None:
        packages = release_contract.load_manifest()
        self.assertEqual(release_contract.EXACT_PACKAGE_IDS, tuple(row.package_id for row in packages))

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for _, project in release_contract.EXACT_PACKAGES:
                path = root / project
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("<Project />\n", encoding="utf-8")
            manifest = root / "tools" / "release-packages.json"
            manifest.parent.mkdir(parents=True)
            rows = [
                {"id": package_id, "project": project}
                for package_id, project in release_contract.EXACT_PACKAGES
            ]
            rows.append({"id": "Hexalith.Conversations.Unknown", "project": rows[0]["project"]})
            manifest.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "canonical five-package sequence"):
                release_contract.load_manifest(manifest, root)


    def test_invalid_archive_boundary_rejects_foreign_assembly(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            package = Path(temporary) / "Hexalith.Conversations.1.0.0.nupkg"
            nuspec = """<?xml version="1.0"?>
<package>
  <metadata>
    <id>Hexalith.Conversations</id>
    <version>1.0.0</version>
    <authors>Hexalith</authors>
    <description>test</description>
    <license type="expression">MIT</license>
    <readme>README.md</readme>
  </metadata>
</package>
"""
            with zipfile.ZipFile(package, "w") as archive:
                archive.writestr("Hexalith.Conversations.nuspec", nuspec)
                archive.writestr("README.md", "test")
                archive.writestr("lib/net11.0/Hexalith.Conversations.dll", b"assembly")
                archive.writestr("runtimes/linux-x64/lib/net11.0/Hexalith.Commons.Http.dll", b"foreign")
            with self.assertRaisesRegex(ValueError, "foreign assemblies"):
                validate_nuget_packages._metadata(package)


    def test_internal_dependencies_follow_candidate_version(self) -> None:
        boundary = {
            "Hexalith.Conversations.Contracts": "1.0.0",
            "Hexalith.EventStore.Client": "3.106.0",
        }
        self.assertEqual(
            {
                "Hexalith.Conversations.Contracts": "0.0.0-ci-test",
                "Hexalith.EventStore.Client": "3.106.0",
            },
            validate_nuget_packages._candidate_dependencies(boundary, "0.0.0-ci-test"),
        )


    def test_symbol_archive_requires_canonical_portable_pdb(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            symbol = Path(temporary) / "Hexalith.Conversations.1.0.0.snupkg"
            with zipfile.ZipFile(symbol, "w") as archive:
                archive.writestr("lib/net11.0/Hexalith.Conversations.pdb", b"not-portable")
            with self.assertRaisesRegex(ValueError, "not a Portable PDB"):
                validate_nuget_packages._validate_symbol_archive(symbol, "Hexalith.Conversations")


    def test_public_consumer_execution_failure_is_blocking(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "PublicConsumer.csproj"
            project.write_text("<Project />\n", encoding="utf-8")
            config = root / "NuGet.Config"
            config.write_text("<configuration />\n", encoding="utf-8")
            failure = subprocess.CalledProcessError(9, ["dotnet", "PublicConsumer.dll"])
            with mock.patch.object(
                validate_consumer_packages,
                "run_dotnet",
                side_effect=[None, None, failure],
            ) as run_dotnet:
                with self.assertRaises(subprocess.CalledProcessError) as raised:
                    validate_consumer_packages.validate_consumer(project, config, {}, False)
            self.assertEqual(9, raised.exception.returncode)
            executed = run_dotnet.call_args_list[2].args[0]
            self.assertEqual(str(root / "bin/Release/net11.0/PublicConsumer.dll"), executed[0])
            self.assertEqual(1, len(executed))


    def test_local_package_source_is_xml_attribute_escaped(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            package_source = root / 'feed & "quoted"'
            config = validate_consumer_packages.write_nuget_config(root / "consumer", package_source)
            document = ElementTree.parse(config)
            source = document.find(".//add[@key='local-conversations']")
            self.assertIsNotNone(source)
            self.assertEqual(str(package_source), source.attrib["value"])


    def test_unmapped_build_refuses_every_ambient_configuration(self) -> None:
        # Use the real consumer XML with an inert catalog bootstrap so absent nested
        # checkouts cannot hide the consumer's explicit missing-mapping diagnostic.
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for name in ("Directory.Build.props", "Directory.Packages.props", "global.json"):
                (root / name).write_bytes((ROOT / name).read_bytes())
            catalog = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_text("<Project />", encoding="utf-8")
            project = root / "Hexalith.Conversations.Contracts.csproj"
            project.write_bytes((ROOT / "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj").read_bytes())
            for configuration, ci in (("Debug", "false"), ("Release", "false"), ("Debug", "true")):
                result = subprocess.run(
                    ["dotnet", "msbuild", str(project), "-nologo", "-target:PrepareForBuild",
                     f"-p:Configuration={configuration}", f"-p:CI={ci}"],
                    cwd=root,
                    env=validate_consumer_packages.isolated_consumer_environment(os.environ.copy()),
                    check=False, capture_output=True, text=True,
                )
                self.assertNotEqual(0, result.returncode)
                self.assertIn("requires the Platform tool-selected source mapping", result.stdout + result.stderr)


    def test_standalone_consumers_remove_parent_mapping_imports(self) -> None:
        parent = {key: "parent-owned" for key in (
            "CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
            "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets",
            "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration")}
        parent.update(PATH="/bin", NUGET_PACKAGES="cache", customaftermicrosoftcommonprops="case-insensitive")
        isolated = validate_consumer_packages.isolated_consumer_environment(parent)
        self.assertEqual({"PATH": "/bin", "NUGET_PACKAGES": "cache"}, isolated)
        self.assertEqual("parent-owned", parent["Configuration"])
        with tempfile.TemporaryDirectory() as temporary:
            project = validate_consumer_packages.write_public_consumer(Path(temporary), "1.0.0")
            with mock.patch.object(validate_consumer_packages, "run_dotnet") as run:
                validate_consumer_packages.validate_consumer(project, Path(temporary) / "NuGet.Config", isolated, False)
            self.assertEqual(3, run.call_count)
            self.assertIn("-p:Configuration=Release", run.call_args_list[0].args[0])
            self.assertEqual("Release", run.call_args_list[1].args[0][4])
            for call in run.call_args_list:
                self.assertEqual(isolated, call.args[2])


    def test_release_solution_and_projects_exclude_external_release_source_edges(self) -> None:
        solution = (ROOT / "Hexalith.Conversations.slnx").read_text(encoding="utf-8")
        self.assertNotIn("references/Hexalith.", solution)

        for project in [*ROOT.glob("src/**/*.csproj"), *ROOT.glob("tests/**/*.csproj")]:
            root = __import__("xml.etree.ElementTree", fromlist=["ElementTree"]).parse(project).getroot()
            for item_group in root.findall("ItemGroup"):
                for reference in item_group.findall("ProjectReference"):
                    include = reference.attrib.get("Include", "")
                    if "references\\Hexalith." in include or "$(Hexalith" in include:
                        self.assertEqual(
                            "'$(UseHexalithProjectReferences)' == 'true'",
                            item_group.attrib.get("Condition"),
                            f"external source edge is not Debug-only: {project}: {include}",
                        )


if __name__ == "__main__":
    unittest.main()
