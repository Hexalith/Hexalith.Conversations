"""Build and run the installed client outside the source tree."""
import argparse
from contextlib import nullcontext
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET


from status_outage import interrupted_status


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--feed", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--build-only", action="store_true")
    parser.add_argument("--fault-api-dll", type=Path, help="Run a second copy of the real API with interrupted command status confirmation")
    parser.add_argument("--apphost", type=Path, help="Running local Platform AppHost for the status-interruption fixture")
    args = parser.parse_args()
    if bool(args.apphost) != bool(args.fault_api_dll) or args.build_only and args.apphost:
        parser.error("Supply --apphost and --fault-api-dll together for a live journey")
    owners = ["Hexalith.Conversations.Client", "Hexalith.Conversations.Contracts"]
    feed = args.feed.resolve(strict=True)
    for owner in owners:
        if not (feed / f"{owner}.{args.version}.nupkg").is_file():
            parser.error(f"Feed is missing {owner} {args.version}")

    with tempfile.TemporaryDirectory(prefix="conversations-adopter-") as tmp:
        work = Path(tmp)
        shutil.copyfile(Path(__file__).with_name("Program.cs"), work / "Program.cs")
        shutil.copyfile(Path(__file__).with_name("Journey.csproj.template"), work / "Journey.csproj")
        config = ET.Element("configuration")
        sources = ET.SubElement(config, "packageSources")
        ET.SubElement(sources, "clear")
        mapping = ET.SubElement(config, "packageSourceMapping")
        for key, source, patterns in (
            ("candidate", str(feed), owners),
            ("nuget.org", "https://api.nuget.org/v3/index.json", ["*"]),
        ):
            ET.SubElement(sources, "add", key=key, value=source)
            group = ET.SubElement(mapping, "packageSource", key=key)
            for pattern in patterns:
                ET.SubElement(group, "package", pattern=pattern)
        ET.ElementTree(config).write(work / "NuGet.Config", encoding="utf-8")
        env = dict(os.environ, NUGET_PACKAGES=str(work / "packages"))
        # The adopter must not inherit the producer's injected MSBuild project-reference mapping.
        for key in ("CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets",
                    "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"):
            env.pop(key, None)

        def run(*args):
            subprocess.run(["dotnet", *args], cwd=work, env=env, check=True)
        version = f"-p:ClientVersion={args.version}"
        run("restore", "Journey.csproj", "--configfile", "NuGet.Config", version)
        libraries = json.loads((work / "obj/project.assets.json").read_text())["libraries"]
        for owner in owners:
            if libraries.get(f"{owner}/{args.version}", {}).get("type") != "package":
                raise SystemExit(f"Unexpected resolved {owner} identity")
        run("build", "-c", "Release", "--no-restore", version)
        if not args.build_only:
            fixture = (interrupted_status(args.apphost.resolve(strict=True), args.fault_api_dll.resolve(strict=True))
                       if args.apphost else nullcontext())
            with fixture as origin:
                env.pop("CONVERSATIONS_STATUS_FAILURE_ORIGIN", None)
                if origin:
                    env["CONVERSATIONS_STATUS_FAILURE_ORIGIN"] = origin
                run("bin/Release/net11.0/Journey.dll")


if __name__ == "__main__":
    main()
