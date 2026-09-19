from __future__ import annotations

import os
import pathlib
import stat
import subprocess
import sys
import tempfile
import unittest
import urllib.error
import zipfile
import importlib.util
from unittest import mock


ROOT = pathlib.Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))

from release_package_contract import (  # noqa: E402
    ManifestPackage,
    PackageMetadata,
    _validate_archive_paths,
    load_manifest,
    read_symbol_metadata,
    validate_internal_dependencies,
)
publisher_spec = importlib.util.spec_from_file_location(
    "publish_release_packages",
    SCRIPTS / "publish-release-packages.py",
)
if publisher_spec is None or publisher_spec.loader is None:
    raise RuntimeError("Unable to load the package publisher for testing")
publish_release_packages = importlib.util.module_from_spec(publisher_spec)
publisher_spec.loader.exec_module(publish_release_packages)
verifier_spec = importlib.util.spec_from_file_location(
    "verify_published_release",
    SCRIPTS / "verify-published-release.py",
)
if verifier_spec is None or verifier_spec.loader is None:
    raise RuntimeError("Unable to load the publication verifier for testing")
verify_published_release = importlib.util.module_from_spec(verifier_spec)
verifier_spec.loader.exec_module(verify_published_release)
missing_nuget_packages = verify_published_release.missing_nuget_packages
resolve_tag_commit = verify_published_release.resolve_tag_commit
verify_release_assets = verify_published_release.verify_release_assets


SOURCE_SHA = "0123456789abcdef0123456789abcdef01234567"
BUILDS_SHA = "b93e9889e9e7b67036837015b4b2b115e326c4da"


class ReleasePackageContractTests(unittest.TestCase):
    def test_manifest_is_the_exact_four_package_boundary(self) -> None:
        self.assertEqual(
            [
                "Hexalith.Conversations.Contracts",
                "Hexalith.Conversations",
                "Hexalith.Conversations.Client",
                "Hexalith.Conversations.Testing",
            ],
            [package.package_id for package in load_manifest()],
        )

    def test_rejects_unsafe_archive_entries(self) -> None:
        unsafe_names = ["../payload", "/absolute", "C:/payload", "folder\\payload", "src/project.csproj"]
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            for index, unsafe_name in enumerate(unsafe_names):
                with self.subTest(name=unsafe_name):
                    archive_path = root / f"unsafe-{index}.nupkg"
                    with zipfile.ZipFile(archive_path, "w") as archive:
                        archive.writestr(unsafe_name, b"payload")
                    with zipfile.ZipFile(archive_path, "r") as archive:
                        with self.assertRaisesRegex(ValueError, "unsafe archive path"):
                            _validate_archive_paths(archive, archive_path)

    def test_rejects_symbol_archive_without_pdb(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            archive_path = pathlib.Path(temporary) / "Package.1.2.3.snupkg"
            nuspec = f"""<package><metadata><id>Package</id><version>1.2.3</version>
<projectUrl>https://github.com/Hexalith/Hexalith.Conversations</projectUrl>
<packageTypes><packageType name="SymbolsPackage"/></packageTypes>
<repository url="https://github.com/Hexalith/Hexalith.Conversations" commit="{SOURCE_SHA}"/>
</metadata></package>"""
            with zipfile.ZipFile(archive_path, "w") as archive:
                archive.writestr("Package.nuspec", nuspec)
            with self.assertRaisesRegex(ValueError, "nonempty portable PDB"):
                read_symbol_metadata(archive_path, SOURCE_SHA)

    def test_rejects_noncanonical_or_wrong_version_internal_dependencies(self) -> None:
        manifest = {"hexalith.conversations": "Hexalith.Conversations"}
        lowercase = PackageMetadata("Consumer", "1.2.3", (("hexalith.conversations", "1.2.3"),))
        with self.assertRaisesRegex(ValueError, "noncanonical"):
            validate_internal_dependencies(lowercase, manifest, "1.2.3")
        wrong_version = PackageMetadata("Consumer", "1.2.3", (("Hexalith.Conversations", "1.2.2"),))
        with self.assertRaisesRegex(ValueError, "instead of release version"):
            validate_internal_dependencies(wrong_version, manifest, "1.2.3")

    def test_packer_rejects_repository_root_output(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPTS / "pack-release-packages.py"),
                str(ROOT),
                "1.2.3",
                "--source-revision",
                SOURCE_SHA,
            ],
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("repository-owned package directory", result.stderr)


class PublicationPreflightTests(unittest.TestCase):
    def write_executable(self, path: pathlib.Path, content: str) -> None:
        path.write_text(content, encoding="utf-8")
        path.chmod(path.stat().st_mode | stat.S_IXUSR)

    def run_preflight(self, *, duplicate: bool = False, stale: bool = False) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as temporary:
            bin_directory = pathlib.Path(temporary)
            self.write_executable(
                bin_directory / "gh",
                f"""#!/usr/bin/env python3
import sys
if any('/git/ref/heads/main' in value for value in sys.argv):
    print('{('f' * 40) if stale else SOURCE_SHA}')
else:
    print('{{"workflow_runs":[{{"head_sha":"{SOURCE_SHA}","head_branch":"main","event":"push","status":"completed","conclusion":"success"}}]}}')
""",
            )
            self.write_executable(
                bin_directory / "curl",
                """#!/usr/bin/env python3
import json, os, pathlib, sys
arguments = sys.argv[1:]
output = pathlib.Path(arguments[arguments.index('--output') + 1])
duplicate = os.environ.get('TEST_DUPLICATE') == '1'
output.write_text(json.dumps({'versions':['1.0.0'] if duplicate else []}), encoding='utf-8')
print('200' if duplicate else '404', end='')
""",
            )
            environment = os.environ.copy()
            environment.update(
                {
                    "PATH": f"{bin_directory}{os.pathsep}{environment['PATH']}",
                    "TEST_DUPLICATE": "1" if duplicate else "0",
                    "GITHUB_SHA": SOURCE_SHA,
                    "GITHUB_TOKEN": "test-token",
                    "GITHUB_REPOSITORY": "Hexalith/Hexalith.Conversations",
                    "HEXALITH_BUILDS_EXECUTION_SHA": BUILDS_SHA,
                    "HEXALITH_RELEASE_SOURCE_BRANCH": "main",
                    "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
                    "HEXALITH_RELEASE_ENVIRONMENT": "production",
                    "HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT": "4",
                    "HEXALITH_RELEASE_REQUIRE_AUTHORITY": "false",
                    "HEXALITH_RELEASE_PACKAGE_MANIFEST": "tools/release-packages.json",
                }
            )
            return subprocess.run(
                ["bash", str(SCRIPTS / "validate-publication-preflight.sh"), "1.0.0", "verify"],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )

    def test_accepts_exact_green_source_when_destinations_are_absent(self) -> None:
        result = self.run_preflight()
        self.assertEqual(0, result.returncode, result.stderr)

    def test_rejects_stale_source(self) -> None:
        result = self.run_preflight(stale=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("no longer the exact live main tip", result.stderr)

    def test_rejects_existing_package_version(self) -> None:
        result = self.run_preflight(duplicate=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("already contains", result.stderr)

    def test_release_caller_is_pinned_and_uses_the_freeze_gated_shared_workflow(self) -> None:
        release = (ROOT / ".github" / "workflows" / "release.yml").read_text(encoding="utf-8")
        self.assertIn(f"domain-release.yml@{BUILDS_SHA}", release)
        self.assertIn(f"builds-execution-sha: {BUILDS_SHA}", release)
        self.assertIn("require-publication-authority: false", release)


class PartialPublicationTests(unittest.TestCase):
    def test_publisher_stops_immediately_after_first_failed_write(self) -> None:
        manifest = [
            ManifestPackage("Hexalith.Conversations.Contracts", "one.csproj", ROOT / "one.csproj"),
            ManifestPackage("Hexalith.Conversations", "two.csproj", ROOT / "two.csproj"),
            ManifestPackage("Hexalith.Conversations.Client", "three.csproj", ROOT / "three.csproj"),
        ]
        calls: list[list[str]] = []

        def runner(arguments: list[str], **_: object) -> subprocess.CompletedProcess[bytes]:
            calls.append(arguments)
            if len(calls) == 2:
                raise subprocess.CalledProcessError(1, arguments)
            return subprocess.CompletedProcess(arguments, 0)

        with mock.patch.object(publish_release_packages, "validate_packages", return_value=(manifest, "1.0.0")):
            with self.assertRaises(subprocess.CalledProcessError):
                publish_release_packages.publish_packages(ROOT / "nupkgs", "1.0.0", SOURCE_SHA, "secret", runner)
        self.assertEqual(2, len(calls))
        self.assertNotIn("--skip-duplicate", calls[0])


class PublishedReleaseVerifierTests(unittest.TestCase):
    def test_reports_the_exact_partial_nuget_inventory(self) -> None:
        def fetch(url: str, _: str | None) -> object:
            if "hexalith.conversations.client" in url:
                raise urllib.error.HTTPError(url, 404, "missing", {}, None)
            return {"versions": ["1.0.0"]}

        self.assertEqual(
            ["Hexalith.Conversations.Client"],
            missing_nuget_packages(
                ["Hexalith.Conversations", "Hexalith.Conversations.Client"],
                "1.0.0",
                fetch,
            ),
        )

    def test_resolves_annotated_tag_and_requires_exact_release_assets(self) -> None:
        expected_assets = {"Hexalith.Conversations.1.0.0.nupkg", "Hexalith.Conversations.1.0.0.snupkg"}

        def fetch(url: str, _: str | None) -> object:
            if "/git/ref/tags/" in url:
                return {"object": {"type": "tag", "sha": "a" * 40}}
            if "/git/tags/" in url:
                return {"object": {"type": "commit", "sha": SOURCE_SHA}}
            if "/releases/tags/" in url:
                return {"tag_name": "v1.0.0", "assets": [{"name": name} for name in expected_assets]}
            raise AssertionError(url)

        self.assertEqual(SOURCE_SHA, resolve_tag_commit("Hexalith/Hexalith.Conversations", "1.0.0", "token", fetch))
        verify_release_assets(
            "Hexalith/Hexalith.Conversations",
            "1.0.0",
            expected_assets,
            "token",
            fetch,
        )

    def test_rejects_missing_or_unexpected_release_assets(self) -> None:
        def fetch(_: str, __: str | None) -> object:
            return {"tag_name": "v1.0.0", "assets": [{"name": "unexpected.zip"}]}

        with self.assertRaisesRegex(ValueError, "asset boundary drifted"):
            verify_release_assets(
                "Hexalith/Hexalith.Conversations",
                "1.0.0",
                {"Hexalith.Conversations.1.0.0.nupkg"},
                "token",
                fetch,
            )


if __name__ == "__main__":
    unittest.main()
