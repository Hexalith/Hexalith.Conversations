"""Exercise dependency refusal controls from the current real SDK observations."""
import argparse
from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
CONFIGURATION = "Release"
SPEC = importlib.util.spec_from_file_location("portable_surface", ROOT / "tests/compatibility/verify_portable_surface.py")
SURFACE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SURFACE)


class PortableSurfaceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.resolved = SURFACE.msbuild(ROOT, SURFACE.PROJECTS["portable"], CONFIGURATION, resolved=True)
        cls.graphs = {}
        pending = [SURFACE.PROJECTS["portable"]]
        while pending:
            relative = pending.pop()
            if relative in cls.graphs:
                continue
            graph = SURFACE.msbuild(ROOT, relative, CONFIGURATION)
            cls.graphs[relative] = graph
            pending.extend(Path(item["FullPath"]).resolve().relative_to(ROOT).as_posix()
                           for item in graph["Items"]["ProjectReference"])
        cls.assets = json.loads(Path(cls.resolved["Properties"]["ProjectAssetsFile"]).read_bytes())

    def test_actual_sdk_surface_accepts_reconciled_reference_provenance(self):
        # This positive path always invokes the real SDK; observations are never substituted.
        result = SURFACE.portable_surface(ROOT, {"portable": self.resolved}, CONFIGURATION)
        self.assertTrue(result["referencePathAssemblies"])
        self.assertTrue(result["transitiveCompileAssets"])
        self.assertIn("Hexalith.Conversations.Contracts", result["referencePathAssemblies"])

    def test_refuses_dependency_boundary_drift(self):
        mutations = (
            "missing-declaration", "extra-declaration", "duplicate-declaration", "imported-reference",
            "resolved-reference", "nonpackable-closure", "helper-dependency", "outside-closure",
            "nonportable-asset", "mismatched-package-asset", "nonportable-binary", "renamed-binary",
            "package-path", "framework-path", "framework-package", "undeclared-framework",
            "missing-assembly-identity", "package-traversal",
        )
        output_root = ROOT / "TestResults"
        output_root.mkdir(exist_ok=True)
        for mutation in mutations:
            with self.subTest(mutation=mutation), tempfile.TemporaryDirectory(prefix="portable-surface-", dir=output_root) as directory:
                scratch = Path(directory)
                resolved, graphs, assets = deepcopy(self.resolved), deepcopy(self.graphs), deepcopy(self.assets)
                declarations = graphs[SURFACE.PROJECTS["portable"]]["Items"]["ProjectReference"]
                server = ROOT / "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj"
                extra = {"FullPath": str(server), "DefiningProjectFullPath": str(ROOT / SURFACE.PROJECTS["portable"])}
                expected = "PORTABLE_TIER_NONPORTABLE_REFERENCE"
                if mutation == "missing-declaration":
                    declarations.pop()
                elif mutation in ("extra-declaration", "duplicate-declaration", "imported-reference"):
                    if mutation == "duplicate-declaration":
                        extra = deepcopy(declarations[1])
                    elif mutation == "imported-reference":
                        extra["DefiningProjectFullPath"] = str(ROOT / "Directory.Build.props")
                    declarations.append(extra)
                elif mutation == "resolved-reference":
                    resolved["Items"]["ProjectReference"].append(extra)
                elif mutation == "nonpackable-closure":
                    graphs["src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj"]["Properties"]["IsPackable"] = "false"
                elif mutation == "helper-dependency":
                    graphs["tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj"]["Items"]["PackageReference"] = [{"Identity": "NSubstitute"}]
                elif mutation == "outside-closure":
                    graphs["src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj"]["Items"]["ProjectReference"].append({"FullPath": str(ROOT.parent / "Outside.csproj")})
                    expected = "RESOLVED_COMPILE_SURFACE_INVALID"
                elif mutation == "nonportable-asset":
                    for target in assets["targets"].values():
                        target["Hexalith.Foreign.Server/1.0.0"] = {"type": "project", "compile": {"bin/Hexalith.Foreign.Server.dll": {}}}
                elif mutation == "mismatched-package-asset":
                    for target in assets["targets"].values():
                        package = next(row for row in target.values() if row.get("type") == "package" and row.get("compile"))
                        package["compile"]["lib/net11.0/Hexalith.Foreign.Server.dll"] = {}
                elif mutation == "nonportable-binary":
                    resolved["Items"]["ReferencePath"].append({"FullPath": str(ROOT / "src/Hexalith.Conversations.Server/bin" / CONFIGURATION / "net11.0/Hexalith.Conversations.Server.dll"), "FusionName": "Hexalith.Conversations.Server, Version=1.0.0.0"})
                elif mutation in ("renamed-binary", "package-path", "framework-path", "framework-package"):
                    references = resolved["Items"]["ReferencePath"]
                    if mutation == "renamed-binary":
                        reference = next(row for row in references if row["FusionName"].startswith("Hexalith.Conversations.Contracts,"))
                    elif mutation == "package-path":
                        reference = next(row for row in references if row.get("NuGetSourceType") == "Package")
                    else:
                        reference = next(row for row in references if row.get("FrameworkReferenceName"))
                    if mutation == "framework-package":
                        reference["NuGetPackageId"] = "Invented.Ref"
                    else:
                        copy = scratch / ("Renamed.Internal.dll" if mutation == "renamed-binary" else Path(reference["FullPath"]).name)
                        shutil.copyfile(reference["FullPath"], copy)
                        reference["FullPath"] = str(copy)
                elif mutation == "undeclared-framework":
                    resolved["Items"]["FrameworkReference"] = []
                elif mutation == "missing-assembly-identity":
                    resolved["Items"]["ReferencePath"][0].pop("FusionName")
                    expected = "RESOLVED_COMPILE_SURFACE_INVALID"
                elif mutation == "package-traversal":
                    for package in assets["libraries"].values():
                        if package.get("type") == "package":
                            package["path"] = "../outside"
                    expected = "RESOLVED_COMPILE_SURFACE_INVALID"
                asset_path = scratch / "project.assets.json"
                asset_path.write_text(json.dumps(assets))
                resolved["Properties"]["ProjectAssetsFile"] = str(asset_path)
                # Fault injection substitutes only this verifier's captured SDK data.
                # Normal qualification, preparation, restore, build and the positive
                # test above use the actual released producer and SDK unchanged.
                with patch.object(SURFACE, "msbuild", side_effect=lambda _root, project, *_args, **_kwargs: graphs[project]):
                    with self.assertRaises(SURFACE.VerificationError) as refusal:
                        SURFACE.portable_surface(ROOT, {"portable": resolved}, CONFIGURATION)
                self.assertTrue(str(refusal.exception).startswith(expected + ":"), str(refusal.exception))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=ROOT)
    parser.add_argument("--configuration", choices=("Release", "Debug"), default=CONFIGURATION)
    args = parser.parse_args()
    ROOT = args.repository.resolve(strict=True)
    CONFIGURATION = args.configuration
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(PortableSurfaceTests))
    if result.wasSuccessful():
        print("PASS: portable dependency refusal controls")
    raise SystemExit(0 if result.wasSuccessful() else 1)
