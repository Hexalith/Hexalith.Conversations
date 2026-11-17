#!/usr/bin/env python3
"""Pack existing outputs and validate five package/symbol pairs and new consumers."""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

from release_contract import ROOT, load_manifest, validate_semver


def main() -> int:
    first = load_manifest()[0]
    result = subprocess.run(["dotnet", "msbuild", first.project, "-nologo", "-verbosity:quiet",
        "-noAutoResponse", "-p:Configuration=Release", "-warnaserror", "-getProperty:Version"],
        cwd=ROOT, text=True, capture_output=True, check=True)
    version = validate_semver(result.stdout.strip())
    # The pack helper requires a contained repository path. This job owns and
    # reliably removes it; isolated consumer caches have their own cleanup.
    with tempfile.TemporaryDirectory(prefix="conversations-product-packages-", dir=ROOT) as temporary:
        packages = Path(temporary)
        for command in ([sys.executable, "scripts/pack-release-packages.py", str(packages), version],
                        [sys.executable, "scripts/validate-nuget-packages.py", str(packages), version],
                        [sys.executable, "scripts/validate-consumer-package-references.py", str(packages)]):
            subprocess.run(command, cwd=ROOT, check=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        raise SystemExit(error.returncode)
