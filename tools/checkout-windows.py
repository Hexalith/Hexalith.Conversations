"""Validate every current path with native Windows Git before sparse historical checkout.

The eighteen reviewed archive blobs remain in the full index and object database. Only
those exact entries are omitted from the protected temporary validation index.
This receipt qualifies path/index policy, never source execution or DLLs.
"""
import argparse
import ctypes
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

SPARSE = b"/*\n!/module/validation/**/isolated/http-cache/\n"
PREFIX = "module/validation/epic-16-published-artifact-qualification/attempt-20261028-implementation-01/"
CACHE = "isolated/http-cache/670c1461c29885f9aa22c281d8b7da90845b38e4$ps:_api.nuget.org_v3_index.json/"
ARCHIVES = {
    (PREFIX + f"collection-{number:02d}/" + CACHE + filename).encode(): (b"100644", oid.encode(), size)
    for number in range(1, 6)
    for filename, oid, size in (
        ("repository_signatures_5.0.0.dat", "6e702dc6ac043750782c6c9bf40e8b068309df03", 2007),
        ("service_index.dat", "36dc12aad499d007082d3357a25c2eb671043cd3", 9272),
    )
}
CURRENT_PREFIX = "module/validation/epic-16-published-artifact-qualification/attempt-20261031-4383-03/"
ARCHIVES.update({
    (CURRENT_PREFIX + collection + "/" + CACHE + filename).encode(): (b"100644", oid.encode(), size)
    for collection in ("collection-final-code", "collection-final-source", "collection-final-verified", "collection-measured")
    for filename, oid, size in (
        ("repository_signatures_5.0.0.dat", "6e702dc6ac043750782c6c9bf40e8b068309df03", 2007),
        ("service_index.dat", "36dc12aad499d007082d3357a25c2eb671043cd3", 9272),
    )
})


def run_git(root, *args, env=None, data=None, protected=True):
    command = ["git", "-c", "core.longpaths=true", "-c", "core.protectNTFS=" + ("true" if protected else "false"), *args]
    result = subprocess.run(command, cwd=root, env=env, input=data, capture_output=True)
    if result.returncode:
        raise ValueError("Native Git command refused: " + args[0])
    return result


def tree_records(data):
    result = {}
    for entry in data.split(b"\0"):
        if not entry:
            continue
        header, path = entry.split(b"\t", 1)
        mode, kind, oid = header.split(b" ")
        if not path or path in result or kind not in (b"blob", b"commit"):
            raise ValueError("The candidate tree has a malformed or duplicate source entry.")
        result[path] = (mode, oid)
    return result


def stage_records(data):
    result = {}
    for entry in data.split(b"\0"):
        if not entry:
            continue
        header, path = entry.split(b"\t", 1)
        mode, oid, stage = header.split(b" ")
        if stage != b"0" or path in result:
            raise ValueError("The index has unmerged or duplicate source entries.")
        result[path] = (mode, oid)
    return result


def require_archives(root, records):
    for path, (mode, oid, size) in ARCHIVES.items():
        if records.get(path) != (mode, oid):
            raise ValueError("An exact immutable archive path, mode or blob is missing or changed.")
        if int(run_git(root, "cat-file", "-s", oid.decode()).stdout) != size:
            raise ValueError("An immutable archive blob has the wrong byte size.")


def windows_compare():
    compare = ctypes.WinDLL("kernel32", use_last_error=True).CompareStringOrdinal
    compare.argtypes = [ctypes.c_wchar_p, ctypes.c_int, ctypes.c_wchar_p, ctypes.c_int, ctypes.c_int]
    compare.restype = ctypes.c_int
    def ordinal(left, right):
        result = compare(left, -1, right, -1, True)
        if result == 0:
            raise ctypes.WinError(ctypes.get_last_error())
        return result - 2
    return ordinal


def require_distinct_paths(records, compare):
    # Native update-index permits case aliases and file/directory prefix aliases.
    # Compare using Windows' actual ordinal case rule, not a Unix casefold guess.
    root = {"children": [], "leaf": False}
    for raw in records:
        parts = raw.decode("utf-8", errors="strict").split("/")
        node = root
        for index, name in enumerate(parts):
            if not name or node["leaf"]:
                raise ValueError("A materialized path aliases a file/directory prefix.")
            children = node["children"]
            lower, upper = 0, len(children)
            while lower < upper:
                middle = (lower + upper) // 2
                if compare(children[middle]["name"], name) < 0:
                    lower = middle + 1
                else:
                    upper = middle
            if lower < len(children) and compare(children[lower]["name"], name) == 0:
                child = children[lower]
                if child["name"] != name:
                    raise ValueError("Materialized paths contain a Windows case alias.")
            else:
                child = {"name": name, "children": [], "leaf": False}
                children.insert(lower, child)
            node = child
            if index == len(parts) - 1:
                if node["leaf"] or node["children"]:
                    raise ValueError("A materialized path aliases a file/directory prefix.")
                node["leaf"] = True


def validate_native_index(root, records):
    payload = b"".join(mode + b" " + oid + b"\t" + path + b"\0" for path, (mode, oid) in records.items())
    with tempfile.TemporaryDirectory(prefix="conversations-protected-index-") as temporary:
        # Git requires a fresh path, not an existing empty file.
        env = os.environ | {"GIT_INDEX_FILE": str(Path(temporary) / "index")}
        written = run_git(root, "update-index", "-z", "--index-info", env=env, data=payload)
        if written.stdout or written.stderr:
            raise ValueError("Native protected Git diagnosed or ignored a source path.")
        observed = run_git(root, "ls-files", "--stage", "-z", env=env)
        if observed.stderr or stage_records(observed.stdout) != records:
            raise ValueError("Native protected index omitted or changed a source path, mode or blob.")


def checkout(root, revision, verify_only=False):
    if os.name != "nt":
        raise ValueError("This guard requires native Windows Git and Windows path comparison.")
    if not re.fullmatch(r"[0-9a-f]{40}", revision):
        raise ValueError("An exact lowercase candidate commit SHA is required.")
    for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"):
        if key in os.environ:
            raise ValueError("Ambient Git repository selectors cannot redirect this guard.")
    protection = subprocess.run(["git", "config", "--bool", "--get", "core.protectNTFS"], cwd=root, capture_output=True)
    if protection.returncode not in (0, 1) or protection.stdout.strip() not in (b"", b"true") or protection.stderr:
        raise ValueError("Persistent or ambient Windows path protection must remain enabled.")
    git_version = run_git(root, "--version").stdout.decode().strip()
    if ".windows." not in git_version:
        raise ValueError("Native Git for Windows is required.")
    actual = run_git(root, "rev-parse", "--verify", revision + "^{commit}").stdout.decode().strip()
    if actual != revision:
        raise ValueError("The requested candidate is not that exact commit.")
    records = tree_records(run_git(root, "ls-tree", "-r", "--full-tree", "-z", revision).stdout)
    require_archives(root, records)
    materialized = {path: value for path, value in records.items() if path not in ARCHIVES}
    require_distinct_paths(materialized, windows_compare())
    validate_native_index(root, materialized)
    receipt = {"candidate": revision, "gitVersion": git_version, "fullTreeEntries": len(records),
               "protectedNativeEntries": len(materialized), "exactArchiveEntries": len(ARCHIVES),
               "qualification": "path/index policy only", "sourceExecution": "not performed", "candidateAcceptance": False}
    if verify_only:
        return receipt
    git_directory = Path(run_git(root, "rev-parse", "--absolute-git-dir").stdout.decode().strip())
    if git_directory != root / ".git" or git_directory.is_symlink() or git_directory.lstat().st_file_attributes & 0x400:
        raise ValueError("Checkout requires the fresh owned workspace Git directory.")
    run_git(root, "config", "core.sparseCheckout", "true")
    run_git(root, "config", "core.sparseCheckoutCone", "false")
    sparse = git_directory / "info" / "sparse-checkout"
    sparse.parent.mkdir(parents=True, exist_ok=True)
    sparse.write_bytes(SPARSE)
    run_git(root, "update-ref", "--no-deref", "HEAD", revision)
    # This is the ONLY unprotected command, after complete native validation.
    run_git(root, "read-tree", "--reset", "-u", revision, protected=False)
    if run_git(root, "rev-parse", "HEAD").stdout.decode().strip() != revision:
        raise ValueError("Checkout HEAD differs from the validated candidate.")
    if stage_records(run_git(root, "ls-files", "--stage", "-z").stdout) != records:
        raise ValueError("The full source index differs from the complete candidate tree.")
    skipped = set()
    for entry in run_git(root, "ls-files", "-v", "-z").stdout.split(b"\0"):
        if not entry:
            continue
        flag, path = entry[:1], entry[2:]
        if flag == b"S":
            skipped.add(path)
        elif flag != b"H":
            raise ValueError("The source index has an unexpected hidden or assumed-unchanged entry.")
    if skipped != set(ARCHIVES):
        raise ValueError("Sparse checkout must skip exactly the eighteen immutable archives and no current source.")
    if run_git(root, "status", "--porcelain", "--untracked-files=no").stdout:
        raise ValueError("Protected status found a source/index difference.")
    for path in ARCHIVES:
        safe_parent = path.split(b"/http-cache/", 1)[0] + b"/http-cache"
        if (root / safe_parent.decode()).exists():
            raise ValueError("An archived HTTP cache was materialized.")
    require_archives(root, records)
    receipt.update(fullIndexParity=True, protectedStatusClean=True, skippedArchiveEntries=len(skipped))
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--revision", required=True)
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    print(json.dumps(checkout(args.repository.resolve(), args.revision, args.verify_only), sort_keys=True))


if __name__ == "__main__":
    main()
