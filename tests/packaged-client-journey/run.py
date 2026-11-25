"""Build and run the installed client outside the source tree."""
import argparse
from contextlib import nullcontext
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from urllib.parse import urlsplit
import xml.etree.ElementTree as ET


from status_outage import describe_resource, interrupted_status


def public_origin(value):
    parts = urlsplit(value)
    if (parts.scheme in {"http", "https"} and parts.hostname and not parts.username and not parts.password
            and parts.path in {"", "/"} and not parts.query and not parts.fragment):
        return parts.scheme, parts.hostname, parts.port or (443 if parts.scheme == "https" else 80)
    return None


def configured_seconds(value):
    """Return a positive 32-bit decimal value, allowing leading zeroes."""
    value = str(value)
    if not value.isascii() or not value.isdecimal():
        return None
    digits = value.lstrip("0")
    if not digits or len(digits) > 10:
        return None
    seconds = int(digits)
    return seconds if seconds <= 2_147_483_647 else None


def verify_host_configuration(apphost, inputs):
    gateway = describe_resource(apphost, "eventstore")["environment"]
    ttl = configured_seconds(gateway.get("EventStore__CommandStatus__TtlSeconds", ""))
    if ttl is None or ttl < 90_000:
        raise RuntimeError("The qualification gateway must retain command status for at least 90000 seconds")
    # EventStore refuses to start when terminal idempotency retention, 86400 seconds by default, is below the status TTL.
    retention = configured_seconds(gateway.get("EventStore__Idempotency__TerminalRetentionSeconds", ""))
    if retention is None or retention < ttl:
        raise RuntimeError("The qualification gateway must retain terminal idempotency records at least as long as command status")

    domain = describe_resource(apphost, "conversations")["environment"]
    sidecar = public_origin(inputs["CONVERSATIONS_DOMAIN_DAPR_ORIGIN"])
    if (sidecar is None or sidecar[0] != "http" or sidecar[1] not in {"localhost", "127.0.0.1", "::1"}
            or str(sidecar[2]) != str(domain.get("DAPR_HTTP_PORT"))):
        raise RuntimeError("CONVERSATIONS_DOMAIN_DAPR_ORIGIN must identify the selected domain sidecar")

    prefix = "conversations:pilot:humanpartybindings:"
    bindings = {}
    for key, value in domain.items():
        key = key.replace("__", ":").lower()
        if key.startswith(prefix):
            child, _, field = key[len(prefix):].partition(":")
            if field in {"tenantid", "principalid", "partyid"}:
                binding = bindings.setdefault(child, {})
                if field in binding:
                    raise RuntimeError("The Conversations host reports ambiguous Party binding settings")
                binding[field] = value
    matches = [entry for entry in bindings.values()
               if entry.get("tenantid") == inputs["CONVERSATIONS_OTHER_TENANT"]
               and entry.get("principalid") == inputs["CONVERSATIONS_OTHER_SUBJECT"]]
    if len(matches) != 1 or matches[0].get("partyid") != inputs["CONVERSATIONS_OTHER_PARTY"]:
        raise RuntimeError("The qualification reader must have one exact Party binding in the running Conversations domain")

    api = describe_resource(apphost, "conversations-api")
    origin = public_origin(inputs["CONVERSATIONS_ORIGIN"])
    if origin is None or not any(origin == public_origin(url["url"]) for url in api.get("urls", []) if not url.get("isInternal", False)):
        raise RuntimeError("CONVERSATIONS_ORIGIN must match a published endpoint of the selected pilot API")
    return api["environment"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--feed", type=Path, required=True)
    parser.add_argument("--version", required=True)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--build-only", action="store_true", help="Verify package installation and compilation")
    mode.add_argument("--without-status-outage", action="store_true", help="Run partial live checks without the status interruption")
    parser.add_argument("--fault-api-dll", type=Path, help="Run a second copy of the real API with interrupted command status confirmation")
    parser.add_argument("--apphost", type=Path, help="Running local Platform AppHost for the status-interruption fixture")
    args = parser.parse_args()
    full = not (args.build_only or args.without_status_outage)
    if full and (args.apphost is None or args.fault_api_dll is None):
        parser.error("Full qualification requires --apphost and --fault-api-dll")
    if not full and (args.apphost is not None or args.fault_api_dll is not None):
        parser.error("--apphost and --fault-api-dll are only used for full qualification")
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
        if args.build_only:
            print("Passed package installation and compilation.")
            return

        fixture = nullcontext()
        if full:
            apphost = args.apphost.resolve(strict=True)
            api_settings = verify_host_configuration(apphost, env)
            fixture = interrupted_status(api_settings, args.fault_api_dll.resolve(strict=True))
        env.pop("CONVERSATIONS_STATUS_FAILURE_ORIGIN", None)
        with fixture as origin:
            if origin is not None:
                env["CONVERSATIONS_STATUS_FAILURE_ORIGIN"] = origin
            run("bin/Release/net11.0/Journey.dll")
        if full:
            print("Passed full packaged-client qualification, including interrupted status confirmation.")
        else:
            print("Passed partial packaged-client journey without status interruption.")


if __name__ == "__main__":
    main()
