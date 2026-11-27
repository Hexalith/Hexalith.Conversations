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
BUILDS_ROOT = ROOT / "references/Hexalith.Builds"


class MinimalModuleCompatibilityOverlayTests(unittest.TestCase):
    @staticmethod
    def isolated_environment():
        return {name: value for name, value in os.environ.items() if name.lower() not in {
            "custombeforedirectorybuildprops", "customaftermicrosoftcommonprops",
            "custombeforedirectorybuildtargets", "customafterdirectorybuildtargets",
            "aftermicrosoftnetsdktargets", "hexalith_source_mapping_hash", "configuration",
            "hexalithbuildsroot", "hexalitheventstoreversion", "directorypackagespropspath"}}

    def catalog_versions(self):
        result = subprocess.run(
            ["dotnet", "msbuild", str(OVERLAY.parent / "CatalogVersion.proj"), "-nologo",
             "-t:ReadMinimalModuleCompatibilityCatalog", "-getItem:_MinimalModuleCompatibilityCatalogPackage"],
            cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True)
        packages = json.loads(result.stdout)["Items"]["_MinimalModuleCompatibilityCatalogPackage"]
        self.assertTrue(packages)
        return {package["Identity"].lower(): package["Version"] for package in packages}

    @staticmethod
    def qualification_properties(overlay=OVERLAY.parent):
        return [
            "-p:Configuration=Release", "-p:MinimalModuleCompatibilityMode=package",
            "-p:MinimalModuleCompatibilityUseSuccessorLocks=true",
            f"-p:CustomAfterMicrosoftCommonProps={overlay / 'Framework.props'}",
            f"-p:CustomAfterMicrosoftCommonTargets={overlay / 'RegisteredCommand.targets'}",
        ]

    def test_root_solution_keeps_fixture_and_compatibility_project_isolated(self):
        solution_path = ROOT / "Hexalith.Conversations.slnx"
        solution = ElementTree.parse(solution_path)
        solution_properties = [
            f"-p:SolutionDir={ROOT}{os.sep}", f"-p:SolutionPath={solution_path}",
            f"-p:SolutionName={solution_path.stem}", f"-p:SolutionFileName={solution_path.name}",
            f"-p:SolutionExt={solution_path.suffix}",
        ]
        pending = [(ROOT / node.attrib["Path"].replace("\\", "/")).resolve()
                   for node in solution.findall(".//Project")]
        self.assertTrue(pending)
        isolated_roots = (FIXTURE, OVERLAY.parent)
        for project in pending:
            for isolated in isolated_roots:
                self.assertFalse(project.is_relative_to(isolated), str(project))

        if os.environ.get("HEXALITH_SOURCE_MAPPING_HASH"):
            with tempfile.TemporaryDirectory() as temporary:
                graph_file = Path(temporary) / "restore-graph.json"
                subprocess.run([
                    "dotnet", "msbuild", str(solution_path), "-nologo", "-noAutoResponse",
                    "-p:Configuration=Release", "-t:GenerateRestoreGraphFile",
                    f"-p:RestoreGraphOutputPath={graph_file}",
                ], cwd=ROOT, text=True, capture_output=True, check=True, timeout=120)
                graph = json.loads(graph_file.read_text())["projects"]
            self.assertTrue(graph)
            for project_path in graph:
                project = Path(project_path).resolve()
                for isolated in isolated_roots:
                    self.assertFalse(project.is_relative_to(isolated), str(project))
            return

        checked = set()
        while pending:
            project = pending.pop()
            for isolated in isolated_roots:
                self.assertFalse(project.is_relative_to(isolated), str(project))
            if project in checked or not project.is_relative_to(ROOT) or project.is_relative_to(ROOT / "references"):
                continue
            checked.add(project)
            result = subprocess.run([
                "dotnet", "msbuild", str(project), "-nologo", "-noAutoResponse",
                "-p:Configuration=Release", *solution_properties, "-getItem:ProjectReference",
            ], cwd=ROOT, text=True, capture_output=True, check=True, timeout=60)
            references = json.loads(result.stdout)["Items"]["ProjectReference"]
            pending.extend(Path(reference["FullPath"]).resolve() for reference in references)

    def test_actual_overlay_selects_packages_and_external_successor_locks(self):
        environment = self.isolated_environment()
        catalog = self.catalog_versions()
        project_packages = {
            "AppHost": {"Hexalith.EventStore.Aspire", "Aspire.Hosting.AppHost", "CommunityToolkit.Aspire.Hosting.Dapr"},
            "Contracts": {"Hexalith.EventStore.Contracts"},
            "Server": {"Hexalith.EventStore.Client", "Hexalith.EventStore.DomainService",
                       "Microsoft.IdentityModel.Protocols.OpenIdConnect", "System.IdentityModel.Tokens.Jwt"},
            "Tests": {"Microsoft.NET.Test.Sdk", "Shouldly", "xunit.v3", "xunit.runner.visualstudio"},
        }
        lock_directory = OVERLAY.parent / "locks/package"
        self.assertCountEqual(
            [f"Hexalith.Conversations.MinimalModule.{name}.packages.lock.json" for name in project_packages],
            [path.name for path in lock_directory.iterdir()],
        )
        for project in sorted(FIXTURE.rglob("*.csproj")):
            command = ["dotnet", "msbuild", str(project), "-nologo", *self.qualification_properties(),
                       "-getProperty:DirectoryPackagesPropsPath,NuGetLockFilePath,Configuration,TargetFramework,TargetFrameworkIdentifier,TargetFrameworkVersion", "-getItem:ProjectReference,PackageReference,PackageVersion"]
            result = subprocess.run(command, cwd=ROOT, env=environment, text=True, capture_output=True, check=True)
            output = json.loads(result.stdout)
            lock = lock_directory / (project.stem + ".packages.lock.json")
            package_props = Path(output["Properties"]["DirectoryPackagesPropsPath"]).resolve()
            self.assertEqual(BUILDS_ROOT / "Props/Directory.Packages.props", package_props)
            self.assertEqual(str(lock), output["Properties"]["NuGetLockFilePath"])
            self.assertEqual("Release", output["Properties"]["Configuration"])
            self.assertEqual(FRAMEWORK, output["Properties"]["TargetFramework"])
            self.assertEqual(".NETCoreApp", output["Properties"]["TargetFrameworkIdentifier"])
            self.assertEqual("v11.0", output["Properties"]["TargetFrameworkVersion"])
            self.assertTrue(lock.is_file())
            references = output["Items"]["ProjectReference"]
            expected_projects = set()
            for reference in ElementTree.parse(project).findall(".//ProjectReference"):
                include = reference.attrib["Include"].replace("\\", "/")
                if not include.startswith("$(FixtureEventStoreRoot)"):
                    expected_projects.add((project.parent / include).resolve())
            self.assertEqual(expected_projects, {Path(reference["FullPath"]) for reference in references})
            versions = {item["Identity"]: item["Version"] for item in output["Items"]["PackageVersion"]}
            dependencies = json.loads(lock.read_text())["dependencies"]
            self.assertEqual([FRAMEWORK], list(dependencies))
            graph = dependencies[FRAMEWORK]
            selected = output["Items"]["PackageReference"]
            self.assertCountEqual(project_packages[project.stem.rsplit(".", 1)[1]], [row["Identity"] for row in selected])
            for reference in selected:
                identity = reference["Identity"]
                self.assertFalse(reference.get("Version") or reference.get("VersionOverride"), identity)
                self.assertEqual("Direct", graph[identity]["type"], identity)
                self.assertEqual(versions[identity], graph[identity]["resolved"], identity)
            for identity, package in graph.items():
                if identity.lower().startswith("hexalith.eventstore."):
                    self.assertNotEqual("Project", package["type"])
                    self.assertEqual(catalog[identity.lower()], package["resolved"])

    def test_reference_framework_discovery_uses_active_framework(self):
        for project in sorted(FIXTURE.rglob("*.csproj")):
            # The SDK clears global TargetFramework for reference discovery. Reproduce that evaluation.
            result = subprocess.run(
                ["dotnet", "msbuild", str(project), "-nologo", "-t:GetTargetFrameworks",
                 "-p:MinimalModuleCompatibilityMode=package",
                 f"-p:CustomAfterMicrosoftCommonProps={OVERLAY.parent / 'Framework.props'}",
                 f"-p:CustomAfterMicrosoftCommonTargets={OVERLAY}",
                 "-getProperty:TargetFramework,TargetFrameworkIdentifier,TargetFrameworkVersion"],
                cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True)
            properties = json.loads(result.stdout)["Properties"]
            self.assertEqual(FRAMEWORK, properties["TargetFramework"])
            self.assertEqual(".NETCoreApp", properties["TargetFrameworkIdentifier"])
            self.assertEqual("v11.0", properties["TargetFrameworkVersion"])

    def test_static_restore_graph_contains_only_fixture_projects_and_eventstore_packages(self):
        with tempfile.TemporaryDirectory() as temporary:
            graph_file = Path(temporary) / "restore-graph.json"
            subprocess.run([
                "dotnet", "msbuild", str(FIXTURE / "Hexalith.Conversations.MinimalModule.slnx"), "-nologo",
                "-t:GenerateRestoreGraphFile", "-p:RestoreUseStaticGraphEvaluation=true",
                f"-p:RestoreGraphOutputPath={graph_file}", *self.qualification_properties(),
            ], cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True, timeout=60)
            graph = json.loads(graph_file.read_text())["projects"]
        self.assertEqual({project.resolve() for project in FIXTURE.rglob("*.csproj")}, {Path(project) for project in graph})
        eventstore = {}
        for project in graph.values():
            for identity, dependency in project["frameworks"][FRAMEWORK].get("dependencies", {}).items():
                if identity.startswith("Hexalith.EventStore."):
                    self.assertEqual("Package", dependency["target"], identity)
                    eventstore[identity] = dependency["version"]
        self.assertEqual(
            {f"Hexalith.EventStore.{name}": "[5.6.0, )" for name in ("Aspire", "Client", "Contracts", "DomainService")},
            eventstore,
        )

    def test_catalog_drift_refuses_before_using_successor_locks(self):
        self.assert_catalog_refused(
            "direct-version",
            "MinimalModule compatibility selection 5.6.0 differs from the direct pinned Builds catalog",
            "0.0.0-review-drift",
        )

    def test_effective_catalog_override_refuses_before_restore(self):
        self.assert_catalog_refused(
            "effective-version",
            "MinimalModule compatibility selection 5.6.0 differs from the effective Builds catalog",
            "0.0.0-review-drift",
        )

    def test_missing_catalog_entry_refuses_before_restore(self):
        self.assert_catalog_refused(
            "missing-entry",
            "The direct pinned Builds catalog is missing EventStore package selection(s) Hexalith.EventStore.Contracts",
            "MinimalModule compatibility expects 5.6.0",
        )

    def assert_catalog_refused(self, mutation, *expected):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            overlay = root / "tests/compatibility/MinimalModule"
            overlay.mkdir(parents=True)
            for name in ("Framework.props", "RegisteredCommand.targets", "CatalogVersion.proj"):
                shutil.copy2(OVERLAY.parent / name, overlay / name)
            catalog = ElementTree.parse(BUILDS_ROOT / "Props/Directory.Packages.props")
            overrides = []
            if mutation == "direct-version":
                versions = catalog.findall(".//HexalithEventStoreVersion")
                self.assertTrue(versions)
                for version in versions:
                    version.text = "0.0.0-review-drift"
            elif mutation == "effective-version":
                overrides.append("-p:HexalithEventStoreVersion=0.0.0-review-drift")
            else:
                for group in catalog.findall(".//ItemGroup"):
                    for package in group.findall("PackageVersion[@Include='Hexalith.EventStore.Contracts']"):
                        group.remove(package)
            catalog_path = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog_path.parent.mkdir(parents=True)
            catalog.write(catalog_path, encoding="utf-8")
            source_project = next(FIXTURE.rglob("Hexalith.Conversations.MinimalModule.Contracts.csproj"))
            project = root / source_project.relative_to(ROOT)
            project.parent.mkdir(parents=True)
            shutil.copy2(source_project, project)
            shutil.copy2(FIXTURE / "Directory.Build.props", root / FIXTURE.relative_to(ROOT) / "Directory.Build.props")
            result = subprocess.run(
                ["dotnet", "restore", str(project), "--locked-mode", *self.qualification_properties(overlay), *overrides],
                cwd=root, env=self.isolated_environment(), text=True, capture_output=True, check=False, timeout=60)
            self.assertNotEqual(0, result.returncode)
            diagnostic = result.stdout + result.stderr
            for detail in expected:
                self.assertIn(detail, diagnostic)
            self.assertFalse((project.parent / "obj/project.assets.json").exists())


if __name__ == "__main__":
    unittest.main()
