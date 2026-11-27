"""Product package inventory, payload, symbol, consumer and mapping contracts."""
from __future__ import annotations

from hashlib import sha256
import importlib.util
import io
import json
import os
import struct
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest import mock
from uuid import UUID
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

verify_published_symbols = load_script_module("verify_published_symbols", "verify-published-symbols.py")


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


    def test_symbol_server_uses_portable_identity_and_requires_exact_bytes(self) -> None:
        guid = UUID("12345678-90ab-cdef-1234-567890abcdef")
        pdb_id = guid.bytes_le + b"\x12\x34\x56\x78"
        header = struct.pack("<4sHHII4sHHII8s", b"BSJB", 1, 1, 0, 4,
                             b"v1\0\0", 0, 1, 40, 32, b"#Pdb\0\0\0\0")
        trailing = bytes(12)
        portable_pdb = header + pdb_id + trailing
        checksum = sha256(header + bytes(20) + trailing).hexdigest()
        name = "Hexalith.Conversations.pdb"
        expected_url = ("https://symbols.nuget.org/download/symbols/hexalith.conversations.pdb/"
                        "1234567890abcdef1234567890abcdefFFFFFFFF/hexalith.conversations.pdb")
        def response(data: bytes, *, status: int = 200, length: int | None = None) -> io.BytesIO:
            body = io.BytesIO(data)
            body.status = status
            body.headers = {} if length is None else {"Content-Length": str(length)}
            return body

        with mock.patch.object(verify_published_symbols, "urlopen", return_value=response(portable_pdb)) as download:
            verify_published_symbols.verify_symbol(name, portable_pdb)
            request = download.call_args.args[0]
            self.assertEqual(expected_url, request.full_url)
            self.assertEqual(f"SHA256:{checksum}", request.get_header("Symbolchecksum"))
        with mock.patch.object(verify_published_symbols, "urlopen", return_value=response(portable_pdb + b"x")):
            with self.assertRaisesRegex(ValueError, "different bytes"):
                verify_published_symbols.verify_symbol(name, portable_pdb)
        for reply, expected_error in (
            (response(portable_pdb, status=206), "HTTP 206"),
            (response(portable_pdb, length=len(portable_pdb) + 1), "different content length"),
        ):
            with mock.patch.object(verify_published_symbols, "urlopen", return_value=reply):
                with self.assertRaisesRegex(ValueError, expected_error):
                    verify_published_symbols.verify_symbol(name, portable_pdb)
        with mock.patch.object(verify_published_symbols, "urlopen") as download:
            with self.assertRaisesRegex(ValueError, "portable PDB"):
                verify_published_symbols.verify_symbol(name, portable_pdb[:-1])
            download.assert_not_called()


    def test_published_symbol_probe_rejects_relabelled_archive_and_checks_other_assets(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            legacy_ids = release_contract.EXACT_PACKAGE_IDS[:4]
            for index, package_id in enumerate(legacy_ids):
                with zipfile.ZipFile(directory / f"{package_id}.1.0.0.snupkg", "w") as archive:
                    version = "1.0.1" if index == 0 else "1.0.0"
                    archive.writestr(f"{package_id}.nuspec", (
                        f"<package><metadata><id>{package_id}</id>"
                        f"<version>{version}</version></metadata></package>"
                    ))
                    archive.writestr(f"lib/net10.0/{package_id}.pdb", b"BSJB-fixture")
            with (
                mock.patch.object(sys, "argv", ["verify-published-symbols.py", str(directory), "1.0.0"]),
                mock.patch.object(verify_published_symbols, "verify_symbol") as query,
                mock.patch("sys.stdout"),
                mock.patch("sys.stderr", new_callable=io.StringIO) as errors,
            ):
                self.assertEqual(1, verify_published_symbols.main())
                self.assertIn(".nuspec identity", errors.getvalue())
                self.assertEqual(
                    [f"{package_id}.pdb" for package_id in legacy_ids[1:]],
                    [call.args[0] for call in query.call_args_list],
                )
            self.assertEqual(release_contract.EXACT_PACKAGE_IDS, verify_published_symbols.published_package_ids("2.0.0"))


    def test_public_consumer_execution_failure_is_blocking(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "PublicConsumer.csproj"
            project.write_text("<Project />\n", encoding="utf-8")
            config = root / "NuGet.Config"
            config.write_text("<configuration />\n", encoding="utf-8")
            failure = subprocess.CalledProcessError(9, ["dotnet", "PublicConsumer.dll"])
            with (
                mock.patch.object(validate_consumer_packages, "run_dotnet", side_effect=[None, None, failure]) as run_dotnet,
                mock.patch.object(validate_consumer_packages, "verify_candidate_packages"),
            ):
                with self.assertRaises(subprocess.CalledProcessError) as raised:
                    validate_consumer_packages.validate_consumer(project, config, {}, False, root, "1.0.0")
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

    def test_consumers_reference_the_exact_five_candidate_packages(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            version = "2.1.0-candidate"
            public = validate_consumer_packages.write_public_consumer(root, version)
            testing = validate_consumer_packages.write_testing_consumer(root, version, {
                "Microsoft.NET.Test.Sdk": "18.12.0", "xunit.v3": "4.0.3", "xunit.runner.visualstudio": "4.0.1",
            })
            candidates = []
            for project in (public, testing):
                for reference in ElementTree.parse(project).findall(".//PackageReference"):
                    identity = reference.attrib["Include"]
                    if identity.startswith("Hexalith.Conversations"):
                        candidates.append(identity)
                        self.assertEqual(f"[{version}]", reference.attrib["Version"], identity)
            self.assertCountEqual(release_contract.EXACT_PACKAGE_IDS, candidates)

    def test_consumer_entry_point_restores_from_candidate_feed_into_two_empty_caches(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            candidate, published = root / "candidate", root / "published"
            version = "1.0.0"
            dependency = "Hexalith.Conversations.PublishedDependency"
            test_tools = {name: version for name in (
                "Microsoft.NET.Test.Sdk", "xunit.v3", "xunit.runner.visualstudio",
            )}
            for feed, identities in (
                (candidate, release_contract.EXACT_PACKAGE_IDS),
                (published, (*release_contract.EXACT_PACKAGE_IDS, dependency, *test_tools)),
            ):
                feed.mkdir()
                for identity in identities:
                    dependencies = ""
                    if identity in release_contract.EXACT_PACKAGE_IDS:
                        dependencies = f'<dependencies><dependency id="{dependency}" version="[{version}]" /></dependencies>'
                    with zipfile.ZipFile(feed / f"{identity}.{version}.nupkg", "w") as archive:
                        archive.writestr(f"{identity}.nuspec", (
                            f"<package><metadata><id>{identity}</id><version>{version}</version>"
                            f"<authors>Hexalith</authors><description>{feed.name}</description>"
                            f"{dependencies}</metadata></package>"
                        ))
                        archive.writestr("lib/net11.0/_._", b"")

            write_config = validate_consumer_packages.write_nuget_config

            def use_local_published_feed(destination, packages):
                config = write_config(destination, packages)
                document = ElementTree.parse(config)
                candidate_patterns = document.findall(".//packageSourceMapping/packageSource[@key='local-conversations']/package")
                self.assertCountEqual(release_contract.EXACT_PACKAGE_IDS, [row.attrib["pattern"] for row in candidate_patterns])
                published_source = document.find(".//packageSources/add[@key='nuget.org']")
                self.assertEqual("https://api.nuget.org/v3/index.json", published_source.attrib["value"])
                published_source.set("value", str(published))
                del published_source.attrib["protocolVersion"]
                document.write(config, encoding="utf-8", xml_declaration=True)
                return config

            work = root / "consumers"
            ambient_packages = root / "ambient"
            seed = root / "Seed.csproj"
            seed.write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework>'
                '</PropertyGroup><ItemGroup><PackageReference Include="Hexalith.Conversations" Version="[1.0.0]" />'
                '</ItemGroup></Project>', encoding="utf-8",
            )
            run_dotnet = validate_consumer_packages.run_dotnet
            run_dotnet(
                ["restore", str(seed), "--source", str(published), "--packages", str(ambient_packages)],
                root, validate_consumer_packages.isolated_consumer_environment(os.environ.copy()),
            )
            cached_copy = ambient_packages / "hexalith.conversations/1.0.0/hexalith.conversations.1.0.0.nupkg"
            published_bytes = (published / "Hexalith.Conversations.1.0.0.nupkg").read_bytes()
            self.assertEqual(published_bytes, cached_copy.read_bytes())
            cache_paths = []
            restored_candidates = set()
            candidate_ids = {item.lower(): item for item in release_contract.EXACT_PACKAGE_IDS}

            def restore_real_consumer(arguments, directory, environment):
                if arguments[0] != "restore":
                    # These feed fixtures contain metadata and placeholders. Real consumer code is
                    # compiled and executed by validate-built-packages.py.
                    return
                packages = Path(arguments[arguments.index("--packages") + 1])
                http_cache = Path(environment["NUGET_HTTP_CACHE_PATH"])
                self.assertEqual(packages, Path(environment["NUGET_PACKAGES"]))
                self.assertTrue(packages.is_relative_to(directory) and http_cache.is_relative_to(directory))
                self.assertFalse(packages.exists())
                self.assertFalse(http_cache.exists())
                cache_paths.append((packages, http_cache))
                run_dotnet(arguments, directory, environment)
                for metadata_path in packages.glob("*/*/.nupkg.metadata"):
                    package = metadata_path.parent
                    identity = package.parent.name
                    source = Path(json.loads(metadata_path.read_bytes())["source"])
                    expected_source = candidate if identity in candidate_ids else published
                    self.assertEqual(expected_source, source, identity)
                    if expected_source == candidate:
                        restored_candidates.add(identity)
                        self.assertEqual(
                            (candidate / f"{candidate_ids[identity]}.{version}.nupkg").read_bytes(),
                            (package / f"{identity}.{version}.nupkg").read_bytes(),
                        )
                self.assertTrue((packages / dependency.lower() / version / ".nupkg.metadata").is_file())

            with (
                mock.patch.dict(os.environ, {
                    "RestorePackagesPath": str(ambient_packages), "RestoreFallbackFolders": str(ambient_packages),
                    "RestoreAdditionalProjectFallbackFolders": str(ambient_packages),
                    "NUGET_FALLBACK_PACKAGES": str(ambient_packages), "NUGET_PACKAGES": str(ambient_packages),
                    "NUGET_HTTP_CACHE_PATH": str(ambient_packages),
                }),
                mock.patch.object(sys, "argv", ["validate-consumer-package-references.py", str(candidate), "--work-directory", str(work)]),
                mock.patch.object(validate_consumer_packages, "catalog_versions", return_value=test_tools),
                mock.patch.object(validate_consumer_packages, "write_nuget_config", side_effect=use_local_published_feed),
                mock.patch.object(validate_consumer_packages, "run_dotnet", side_effect=restore_real_consumer),
            ):
                self.assertEqual(0, validate_consumer_packages.main())
            self.assertEqual(2, len(cache_paths))
            self.assertEqual(4, len({path for pair in cache_paths for path in pair}))
            self.assertEqual(set(candidate_ids), restored_candidates)
            self.assertEqual(published_bytes, cached_copy.read_bytes())

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
        self.assertEqual({"PATH": "/bin"}, isolated)
        self.assertEqual("parent-owned", parent["Configuration"])
        with tempfile.TemporaryDirectory() as temporary:
            project = validate_consumer_packages.write_public_consumer(Path(temporary), "1.0.0")
            with (
                mock.patch.object(validate_consumer_packages, "run_dotnet") as run,
                mock.patch.object(validate_consumer_packages, "verify_candidate_packages"),
            ):
                validate_consumer_packages.validate_consumer(
                    project, Path(temporary) / "NuGet.Config", isolated, False, Path(temporary), "1.0.0",
                )
            self.assertEqual(3, run.call_count)
            self.assertIn("-p:Configuration=Release", run.call_args_list[0].args[0])
            self.assertEqual("Release", run.call_args_list[1].args[0][4])
            for call in run.call_args_list:
                self.assertEqual(isolated | {
                    "NUGET_PACKAGES": str(project.parent / ".nuget/packages"),
                    "NUGET_HTTP_CACHE_PATH": str(project.parent / ".nuget/http-cache"),
                }, call.args[2])


    def test_release_solution_and_projects_exclude_external_release_source_edges(self) -> None:
        solution = (ROOT / "Hexalith.Conversations.slnx").read_text(encoding="utf-8")
        self.assertNotIn("references/Hexalith.", solution)

        for project in [*ROOT.glob("src/**/*.csproj"), *ROOT.glob("tests/**/*.csproj")]:
            if project.is_relative_to(ROOT / "tests/fixtures/Hexalith.Conversations.MinimalModule"):
                # The frozen source edges are checked after conversion by the overlay tests.
                continue
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
