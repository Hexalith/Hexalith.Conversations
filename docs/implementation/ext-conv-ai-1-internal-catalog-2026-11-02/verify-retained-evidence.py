"""Verify retained source evidence bytes; this establishes no installation or acceptance."""

import hashlib
import json
from pathlib import Path, PurePosixPath

root = Path(__file__).resolve().parent
inventory_path = root / "package-inventory.json"
inventory = json.loads(inventory_path.read_text())
expected = {}
for entry in inventory["files"]:
    relative = PurePosixPath(entry["path"])
    if relative.is_absolute() or ".." in relative.parts or entry["path"] in expected:
        raise ValueError("Invalid or duplicate evidence path")
    expected[entry["path"]] = entry
paths = list(root.rglob("*"))
if any(path.is_symlink() for path in paths):
    raise ValueError("Evidence symlinks are unsupported")
actual = {path.relative_to(root).as_posix(): path for path in paths if path.is_file() and path != inventory_path}
if set(actual) != set(expected):
    raise ValueError("Evidence inventory membership differs")
for relative, path in actual.items():
    value = path.read_bytes()
    if len(value) != expected[relative]["size"] or hashlib.sha256(value).hexdigest() != expected[relative]["sha256"]:
        raise ValueError("Evidence bytes differ: " + relative)
print(f"Verified {len(actual)} retained source-evidence files; installation and acceptance remain unmeasured.")
