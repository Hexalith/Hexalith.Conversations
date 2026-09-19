from __future__ import annotations

import importlib.util
import hashlib
import io
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

import release_contract  # noqa: E402
import release_state  # noqa: E402


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


verify_release_source = load_script_module("verify_release_source", "verify-release-source.py")
validate_nuget_packages = load_script_module("validate_nuget_packages", "validate-nuget-packages.py")
pack_release_packages = load_script_module("pack_release_packages", "pack-release-packages.py")
validate_consumer_packages = load_script_module(
    "validate_consumer_packages",
    "validate-consumer-package-references.py",
)


def published_payloads() -> dict[str, bytes]:
    payloads: dict[str, bytes] = {}
    for package_id in release_contract.EXACT_PACKAGE_IDS:
        buffer = io.BytesIO()
        with zipfile.ZipFile(buffer, "w") as archive:
            archive.writestr(f"{package_id}.nuspec", f"<package id='{package_id}' />")
            archive.writestr(f"lib/net10.0/{package_id}.dll", f"assembly:{package_id}")
        payloads[package_id] = buffer.getvalue()
    return payloads


def repository_signed(payload: bytes) -> bytes:
    buffer = io.BytesIO(payload)
    with zipfile.ZipFile(buffer, "a") as archive:
        archive.writestr(".signature.p7s", b"nuget.org repository signature")
    return buffer.getvalue()


def release_assets(version: str, payloads: dict[str, bytes]) -> list[dict[str, object]]:
    assets: list[dict[str, object]] = []
    for name in sorted(release_contract.expected_asset_names(version)):
        package_id = name.removesuffix(f".{version}.nupkg") if name.endswith(".nupkg") else None
        content = payloads[package_id] if package_id in payloads else name.encode()
        assets.append(
            {
                "name": name,
                "state": "uploaded",
                "size": len(content),
                "digest": f"sha256:{hashlib.sha256(content).hexdigest()}",
                "browser_download_url": (
                    f"https://github.com/{release_contract.REPOSITORY}/releases/download/v{version}/{name}"
                ),
            }
        )
    return assets


class ReleaseToolingTests(unittest.TestCase):
    def test_package_output_must_stay_below_repository_root(self) -> None:
        with self.assertRaisesRegex(ValueError, "child of the repository root"):
            pack_release_packages.prepare_output_directory(Path("/"))

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
            with self.assertRaisesRegex(ValueError, "canonical four-package sequence"):
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
                archive.writestr("lib/net10.0/Hexalith.Conversations.dll", b"assembly")
                archive.writestr("runtimes/linux-x64/lib/net10.0/Hexalith.Commons.Http.dll", b"foreign")
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
                archive.writestr("lib/net10.0/Hexalith.Conversations.pdb", b"not-portable")
            with self.assertRaisesRegex(ValueError, "not a Portable PDB"):
                validate_nuget_packages._validate_symbol_archive(symbol, "Hexalith.Conversations")

    def test_frozen_dispatch_skips_without_network(self) -> None:
        def forbidden_get(_: str):
            self.fail("frozen verification must not query GitHub")

        def forbidden_destinations(_: str, __: str) -> None:
            self.fail("frozen verification must not query destinations")

        enabled = verify_release_source.verify_source(
            "false",
            release_contract.REPOSITORY,
            "refs/heads/main",
            "a" * 40,
            "ci.yml",
            forbidden_get,
            forbidden_destinations,
        )
        self.assertFalse(enabled)

    def test_stale_dispatch_is_rejected(self) -> None:
        def stale_main(_: str):
            return {"object": {"sha": "b" * 40}}

        with self.assertRaisesRegex(ValueError, "STALE_SOURCE"):
            verify_release_source.verify_source(
                "true",
                release_contract.REPOSITORY,
                "refs/heads/main",
                "a" * 40,
                "ci.yml",
                stale_main,
                lambda _version, _repository: None,
            )

    def test_exact_main_and_successful_push_ci_are_required(self) -> None:
        source_sha = "a" * 40
        destinations: list[tuple[str, str]] = []

        def github(url: str):
            if "/git/ref/heads/main" in url:
                return {"object": {"sha": source_sha}}
            return {
                "workflow_runs": [
                    {
                        "id": 101,
                        "run_number": 7,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "success",
                    }
                ]
            }

        enabled = verify_release_source.verify_source(
            "true",
            release_contract.REPOSITORY,
            "refs/heads/main",
            source_sha,
            "ci.yml",
            github,
            lambda version, repository: destinations.append((version, repository)),
        )
        self.assertTrue(enabled)
        self.assertEqual(
            [(release_contract.FIRST_RELEASE_VERSION, release_contract.REPOSITORY)],
            destinations,
        )

    def test_newest_exact_source_ci_attempt_must_succeed(self) -> None:
        source_sha = "a" * 40

        def github(url: str):
            if "/git/ref/heads/main" in url:
                return {"object": {"sha": source_sha}}
            return {
                "workflow_runs": [
                    {
                        "id": 201,
                        "run_number": 8,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "failure",
                    },
                    {
                        "id": 101,
                        "run_number": 7,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "success",
                    },
                ]
            }

        with self.assertRaisesRegex(ValueError, "newest push CI run/attempt"):
            verify_release_source.verify_source(
                "true",
                release_contract.REPOSITORY,
                "refs/heads/main",
                source_sha,
                "ci.yml",
                github,
                lambda _version, _repository: None,
            )

    def test_partial_publication_is_not_treated_as_absent(self) -> None:
        def partial(_method: str, url: str) -> release_state.HttpResponse:
            status = 200 if "hexalith.conversations.contracts" in url else 404
            return release_state.HttpResponse(status)

        with self.assertRaisesRegex(ValueError, "PARTIAL_PUBLICATION"):
            release_state.verify_absent("1.0.0", request=partial)

    def test_occupied_release_is_rejected_before_republication(self) -> None:
        with self.assertRaisesRegex(ValueError, "DESTINATION_OCCUPIED"):
            release_state.verify_absent(
                "1.0.0",
                request=lambda _method, _url: release_state.HttpResponse(200),
            )

    def test_publish_boundary_accepts_only_semantic_releases_exact_tag(self) -> None:
        source_sha = "a" * 40

        def tagged_absent(method: str, url: str) -> release_state.HttpResponse:
            if method == "HEAD" or "/releases/tags/" in url:
                return release_state.HttpResponse(404)
            if "/git/ref/tags/v1.0.0" in url:
                return release_state.HttpResponse(
                    200,
                    json.dumps({"object": {"type": "commit", "sha": source_sha}}).encode(),
                )
            self.fail(f"unexpected publish-boundary URL: {url}")

        release_state.verify_publishable("1.0.0", source_sha, request=tagged_absent)

        with self.assertRaisesRegex(ValueError, "does not resolve to source"):
            release_state.verify_publishable("1.0.0", "b" * 40, request=tagged_absent)

    def test_first_release_present_state_accepts_exact_pairs_and_sha(self) -> None:
        source_sha = "a" * 40
        payloads = published_payloads()
        assets = release_assets("1.0.0", payloads)

        def published(method: str, url: str) -> release_state.HttpResponse:
            if "api.nuget.org" in url:
                package_id = next(
                    package_id
                    for package_id in release_contract.EXACT_PACKAGE_IDS
                    if f"/{package_id.lower()}/" in url
                )
                return release_state.HttpResponse(200, repository_signed(payloads[package_id]))
            if "/git/ref/tags/v1.0.0" in url:
                return release_state.HttpResponse(
                    200,
                    json.dumps({"object": {"type": "commit", "sha": source_sha}}).encode(),
                )
            if "/releases/tags/v1.0.0" in url:
                return release_state.HttpResponse(
                    200,
                    json.dumps(
                        {
                            "tag_name": "v1.0.0",
                            "draft": False,
                            "prerelease": False,
                            "assets": assets,
                        }
                    ).encode(),
                )
            if f"https://github.com/{release_contract.REPOSITORY}/releases/download/v1.0.0/" in url:
                asset_name = url.rsplit("/", maxsplit=1)[-1]
                package_id = asset_name.removesuffix(".1.0.0.nupkg")
                return release_state.HttpResponse(200, payloads[package_id])
            self.fail(f"unexpected publication URL: {url}")

        release_state.verify_present(
            "1.0.0",
            source_sha,
            request=published,
            attempts=1,
            delay_seconds=0,
        )

    def test_release_assets_reject_missing_and_unexpected_names(self) -> None:
        payloads = published_payloads()
        assets = release_assets("1.0.0", payloads)
        release = {
            "tag_name": "v1.0.0",
            "draft": False,
            "prerelease": False,
            "assets": assets[:-1],
        }
        with self.assertRaisesRegex(ValueError, "exactly eight assets"):
            release_state._release_asset_digests(release, "1.0.0")

        unexpected = [*assets]
        unexpected[-1] = {
            "name": "Hexalith.Conversations.Unexpected.1.0.0.snupkg",
            "state": "uploaded",
            "size": 1,
            "digest": f"sha256:{'0' * 64}",
            "browser_download_url": (
                f"https://github.com/{release_contract.REPOSITORY}/releases/download/v1.0.0/"
                "Hexalith.Conversations.Unexpected.1.0.0.snupkg"
            ),
        }
        release["assets"] = unexpected
        with self.assertRaisesRegex(ValueError, "unexpected="):
            release_state._release_asset_digests(release, "1.0.0")

    def test_nuget_publication_poll_retries_transient_responses(self) -> None:
        payloads = published_payloads()
        attempts: dict[str, int] = {}

        def transient_then_present(_method: str, url: str) -> release_state.HttpResponse:
            package_id = next(
                package_id
                for package_id in release_contract.EXACT_PACKAGE_IDS
                if f"/{package_id.lower()}/" in url
            )
            attempts[package_id] = attempts.get(package_id, 0) + 1
            if package_id == "Hexalith.Conversations" and attempts[package_id] == 1:
                return release_state.HttpResponse(429)
            return release_state.HttpResponse(200, payloads[package_id])

        digests = release_state._poll_nuget_packages(
            "1.0.0",
            transient_then_present,
            attempts=2,
            delay_seconds=0,
        )
        self.assertEqual(set(release_contract.EXACT_PACKAGE_IDS), set(digests))
        self.assertEqual(2, attempts["Hexalith.Conversations"])

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
            self.assertEqual(str(root / "bin/Release/net10.0/PublicConsumer.dll"), executed[0])
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

    def test_publication_preflight_routes_verify_and_publish_phases(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            capture = root / "capture.txt"
            python = root / "python3"
            python.write_text(
                "#!/usr/bin/env bash\nprintf '%s\\n' \"$*\" > \"$CAPTURE_PATH\"\n",
                encoding="utf-8",
            )
            python.chmod(0o755)
            environment = {
                **os.environ,
                "PATH": f"{root}{os.pathsep}{os.environ['PATH']}",
                "CAPTURE_PATH": str(capture),
                "HEXALITH_BUILDS_EXECUTION_SHA": release_contract.BUILDS_EXECUTION_SHA,
                "HEXALITH_RELEASE_SOURCE_BRANCH": "main",
                "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
                "HEXALITH_RELEASE_PACKAGE_MANIFEST": "tools/release-packages.json",
                "HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT": "4",
                "HEXALITH_RELEASE_ENVIRONMENT": "production",
                "HEXALITH_RELEASE_REQUIRE_AUTHORITY": "false",
                "GITHUB_REPOSITORY": release_contract.REPOSITORY,
                "GITHUB_REF": "refs/heads/main",
                "GITHUB_SHA": "a" * 40,
            }
            command = ["bash", str(SCRIPTS / "validate-publication-preflight.sh"), "1.0.0"]

            subprocess.run([*command, "verify"], cwd=ROOT, env=environment, check=True)
            verify_arguments = capture.read_text(encoding="utf-8").strip()
            self.assertTrue(verify_arguments.startswith("scripts/verify-release-source.py "))
            self.assertIn("--destination-state absent", verify_arguments)

            subprocess.run([*command, "publish"], cwd=ROOT, env=environment, check=True)
            publish_arguments = capture.read_text(encoding="utf-8").strip()
            self.assertTrue(publish_arguments.startswith("scripts/verify-release-state.py publishable 1.0.0 "))

    def test_msbuild_dependency_mode_matrix_is_evaluated(self) -> None:
        project = ROOT / "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj"
        cases = (("Debug", "false", "true"), ("Release", "false", "false"), ("Debug", "true", "false"))
        for configuration, ci, expected in cases:
            result = subprocess.run(
                [
                    "dotnet",
                    "msbuild",
                    str(project),
                    "-nologo",
                    f"-p:Configuration={configuration}",
                    f"-p:CI={ci}",
                    "-getProperty:UseHexalithProjectReferences",
                ],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            )
            self.assertEqual(expected, result.stdout.strip(), f"Configuration={configuration}, CI={ci}")

    def test_workflows_pin_bypass_and_freeze_contract(self) -> None:
        release = (ROOT / ".github" / "workflows" / "release.yml").read_text(encoding="utf-8")
        self.assertEqual(2, release.count(release_contract.BUILDS_EXECUTION_SHA))
        self.assertIn("require-publication-authority: false", release)
        self.assertIn("publish-containers: false", release)
        self.assertIn("reserved-version: ''", release)
        self.assertIn("release-authority-issue-url: ''", release)
        self.assertIn("release-authority-owner: ''", release)
        self.assertIn("HEXALITH_RELEASE_PUBLISH_ENABLED", release)
        self.assertIn("if: ${{ needs.verify-source.outputs.publish-enabled == 'true' }}", release)
        self.assertIn("verify:release-plan -- 1.0.0", release)
        self.assertNotIn("secrets: inherit", release)

        configuration = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        github_options = configuration["plugins"][-1][1]
        self.assertEqual(["nupkgs/*.nupkg", "nupkgs/*.snupkg"], github_options["assets"])
        self.assertNotIn("--skip-duplicate", json.dumps(configuration))
        exec_options = configuration["plugins"][2][1]
        self.assertIn("validate-nuget-packages.py ./nupkgs ${nextRelease.version}", exec_options["prepareCmd"])
        self.assertTrue(
            exec_options["prepareCmd"].endswith(
                "validate-publication-preflight.sh ${nextRelease.version} verify >&2"
            )
        )
        planner = (SCRIPTS / "verify-semantic-release-plan.mjs").read_text(encoding="utf-8")
        self.assertIn("readFile('.releaserc.json'", planner)
        self.assertIn("['init', '--bare', localRemote]", planner)
        self.assertNotIn("github.com/Hexalith", planner)

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

    def test_ci_and_security_automation_cover_release_boundary(self) -> None:
        ci = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        self.assertIn("domain-ci.yml@main", ci)
        self.assertIn("run-consumer-validation: true", ci)
        self.assertIn("npm audit signatures", ci)
        self.assertIn("npm test", ci)
        self.assertIn("tests/Hexalith.Conversations.Admin.Web.Tests", ci)
        self.assertIn("aspire-timeout-minutes: 20", ci)
        self.assertIn("fetch-depth: 0", ci)
        self.assertIn("npx --no-install commitlint --config commitlint.config.mjs --from", ci)
        self.assertIn(
            "npx --no-install commitlint --config commitlint.config.mjs --verbose <<< \"$PR_TITLE\"",
            ci,
        )
        for excluded_method in (
            "PreservationTraceabilityManifestValidationTest.BindingsClosuresAndFrozenV1BytesShouldValidateIndependently",
            "PreservationTraceabilityManifestValidationTest.CurrentControlsAndTierPrerequisiteShouldStayTruthful",
            "SmC2BaselineReconstructionValidationTest.BaselineShouldRecordAnAuditableReconstructionMethod",
        ):
            self.assertEqual(1, ci.count(excluded_method))

        global_json = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))
        self.assertEqual("Microsoft.Testing.Platform", global_json["test"]["runner"])

        dependabot = (ROOT / ".github" / "dependabot.yml").read_text(encoding="utf-8")
        for ecosystem in ("nuget", "npm", "github-actions"):
            self.assertIn(f'package-ecosystem: "{ecosystem}"', dependabot)
        self.assertTrue((ROOT / ".github" / "workflows" / "codeql.yml").is_file())
        self.assertTrue((ROOT / ".github" / "workflows" / "dependency-review.yml").is_file())


if __name__ == "__main__":
    unittest.main()
