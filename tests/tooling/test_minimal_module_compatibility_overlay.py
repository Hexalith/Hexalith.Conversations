"""Verify the external compatibility graph and successor lock boundary."""
from pathlib import Path
import json
import os
import subprocess
import shutil
import tempfile
from xml.etree import ElementTree
import unittest

ROOT = Path(__file__).resolve().parents[2]
OVERLAY = ROOT / "tests/compatibility/MinimalModule/RegisteredCommand.targets"
FIXTURE = ROOT / "tests/fixtures/Hexalith.Conversations.MinimalModule"
FRAMEWORK = "net11.0"


class MinimalModuleCompatibilityOverlayTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        result = subprocess.run([
            "dotnet", "msbuild", str(OVERLAY.parent / "Hexalith.Conversations.MinimalModule.Compatibility.csproj"),
            "-nologo", "-noAutoResponse", "-verbosity:quiet", "-p:Configuration=Release", "-getProperty:HexalithBuildsRoot",
        ], cwd=ROOT, text=True, capture_output=True, check=True)
        cls.builds_root = Path(result.stdout.strip())
        if not (cls.builds_root / "Props/Directory.Packages.props").is_file():
            raise RuntimeError("Compatibility checks require the already producer-selected actual Builds catalog")

    @staticmethod
    def isolated_environment():
        return {name: value for name, value in os.environ.items() if name.lower() not in {
            "custombeforedirectorybuildprops", "customaftermicrosoftcommonprops",
            "custombeforedirectorybuildtargets", "customafterdirectorybuildtargets",
            "aftermicrosoftnetsdktargets", "hexalith_source_mapping_hash", "configuration"}}

    def catalog_versions(self):
        result = subprocess.run(
            ["dotnet", "msbuild", str(OVERLAY.parent / "CatalogVersion.proj"), "-nologo",
             f"-p:HexalithBuildsRoot={self.builds_root}", "-t:ReadMinimalModuleCompatibilityCatalog", "-getItem:_MinimalModuleCompatibilityCatalogPackage"],
            cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True)
        packages = json.loads(result.stdout)["Items"]["_MinimalModuleCompatibilityCatalogPackage"]
        self.assertTrue(packages)
        return {package["Identity"].lower(): package["Version"] for package in packages}

    def test_actual_overlay_selects_packages_and_external_successor_locks(self):
        environment = self.isolated_environment()
        catalog = self.catalog_versions()
        for project in sorted(FIXTURE.rglob("*.csproj")):
            command = ["dotnet", "msbuild", str(project), "-nologo", "-t:MinimalModuleCompatibilitySelectPackages",
                       "-p:Configuration=Release", f"-p:TargetFramework={FRAMEWORK}", "-p:MinimalModuleCompatibilityMode=package",
                       f"-p:CustomAfterMicrosoftCommonProps={OVERLAY.parent / 'Framework.props'}",
                       "-p:MinimalModuleCompatibilityUseSuccessorLocks=true", f"-p:CustomAfterMicrosoftCommonTargets={OVERLAY}", f"-p:HexalithBuildsRoot={self.builds_root}",
                       "-getProperty:DirectoryPackagesPropsPath,NuGetLockFilePath,Configuration,TargetFramework,TargetFrameworkIdentifier,TargetFrameworkVersion", "-getItem:ProjectReference,PackageReference,PackageVersion"]
            result = subprocess.run(command, cwd=ROOT, env=environment, text=True, capture_output=True, check=True)
            output = json.loads(result.stdout)
            lock = OVERLAY.parent / "locks/package" / (project.stem + ".packages.lock.json")
            package_props = Path(output["Properties"]["DirectoryPackagesPropsPath"]).resolve()
            self.assertEqual(self.builds_root / "Props/Directory.Packages.props", package_props)
            self.assertEqual(str(lock), output["Properties"]["NuGetLockFilePath"])
            self.assertEqual("Release", output["Properties"]["Configuration"])
            self.assertEqual(FRAMEWORK, output["Properties"]["TargetFramework"])
            self.assertEqual(".NETCoreApp", output["Properties"]["TargetFrameworkIdentifier"])
            self.assertEqual("v11.0", output["Properties"]["TargetFrameworkVersion"])
            self.assertTrue(lock.is_file())
            for reference in output["Items"]["ProjectReference"]:
                self.assertNotIn("Hexalith.EventStore.", reference["Identity"])
            versions = {item["Identity"]: item["Version"] for item in output["Items"]["PackageVersion"]}
            selected = [reference for reference in output["Items"]["PackageReference"] if reference["Identity"].startswith("Hexalith.EventStore.")]
            if not project.stem.endswith(".Tests"):
                self.assertTrue(selected)
            for reference in selected:
                self.assertFalse(reference.get("Version") or reference.get("VersionOverride"))
                self.assertEqual(catalog[reference["Identity"].lower()], versions[reference["Identity"]])
            dependencies = json.loads(lock.read_text())["dependencies"]
            self.assertEqual([FRAMEWORK], list(dependencies))
            graph = dependencies[FRAMEWORK]
            for identity, package in graph.items():
                if identity.lower().startswith("hexalith.eventstore."):
                    self.assertNotEqual("Project", package["type"])
                    self.assertEqual(catalog[identity.lower()], package["resolved"])
            if project.stem.endswith(".Tests"):
                selected = {reference["Identity"]: reference for reference in output["Items"]["PackageReference"]}
                for identity, version in {"Microsoft.NET.Test.Sdk": "18.12.0", "xunit.v3": "4.0.3"}.items():
                    self.assertFalse(selected[identity].get("Version") or selected[identity].get("VersionOverride"))
                    self.assertEqual(version, versions[identity])
                    self.assertEqual(version, graph[identity]["resolved"])


    def test_reference_framework_discovery_uses_active_framework(self):
        for project in sorted(FIXTURE.rglob("*.csproj")):
            # The SDK clears global TargetFramework for reference discovery. Reproduce that evaluation.
            result = subprocess.run(
                ["dotnet", "msbuild", str(project), "-nologo", "-t:GetTargetFrameworks",
                 "-p:MinimalModuleCompatibilityMode=package",
                 f"-p:CustomAfterMicrosoftCommonProps={OVERLAY.parent / 'Framework.props'}",
                 f"-p:CustomAfterMicrosoftCommonTargets={OVERLAY}", f"-p:HexalithBuildsRoot={self.builds_root}",
                 "-getProperty:TargetFramework,TargetFrameworkIdentifier,TargetFrameworkVersion"],
                cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True)
            properties = json.loads(result.stdout)["Properties"]
            self.assertEqual(FRAMEWORK, properties["TargetFramework"])
            self.assertEqual(".NETCoreApp", properties["TargetFrameworkIdentifier"])
            self.assertEqual("v11.0", properties["TargetFrameworkVersion"])

    def test_catalog_drift_refuses_before_using_successor_locks(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            overlay = root / "tests/compatibility/MinimalModule"
            overlay.mkdir(parents=True)
            for name in ("RegisteredCommand.targets", "CatalogVersion.proj"):
                shutil.copy2(OVERLAY.parent / name, overlay / name)
            catalog = ElementTree.parse(self.builds_root / "Props/Directory.Packages.props")
            versions = catalog.findall(".//HexalithEventStoreVersion")
            self.assertTrue(versions)
            for version in versions:
                version.text = "0.0.0-review-drift"
            catalog_path = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog_path.parent.mkdir(parents=True)
            catalog.write(catalog_path, encoding="utf-8")
            project = root / "Fixture.csproj"
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>')
            result = subprocess.run(
                ["dotnet", "msbuild", str(project), "-nologo", "-t:MinimalModuleCompatibilitySelectPackages",
                 "-p:Configuration=Release", "-p:MinimalModuleCompatibilityMode=package",
                 "-p:MinimalModuleCompatibilityUseSuccessorLocks=true",
                 f"-p:CustomAfterMicrosoftCommonTargets={overlay / 'RegisteredCommand.targets'}"],
                cwd=root, env=self.isolated_environment(), text=True, capture_output=True, check=False)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("differs from the direct pinned Builds catalog", result.stdout + result.stderr)



if __name__ == "__main__":
    unittest.main()
