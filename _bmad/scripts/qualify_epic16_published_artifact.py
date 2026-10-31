"""Collect bounded, read-only evidence for an exact published Epic 16 tuple.

This collector never enrolls a module, changes a pin, writes trust, or starts a
runtime. Native exits and qualification verdicts are deliberately independent.
Only a fresh directory below the local qualification observation root is writable.
"""
from __future__ import annotations

import argparse
import ast
import base64
from collections import Counter
from contextlib import ExitStack
import copy
from dataclasses import dataclass
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import selectors
import signal
import stat
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
import zipfile

from authoring_outputs import OutputError, output_target
from contained_reader import ContainedReader, ReadError, relative_path

OBSERVATIONS = "module/validation/epic-16-published-artifact-qualification"
MANIFEST = "module/hexalith.module-manifest.v2.json"
PREDECESSOR = "module/validation/epic-16-entry-2026-10-28-post-tooling-01/adoption.stdout.txt"
PLANNING_INPUTS = "_bmad-output/implementation-artifacts/epic-16-published-artifact-platform-qualification-inputs.json"
TOOLING_CONTRACT = "_bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.1.json"
REVIEW_LEDGER = "module/validation/epic-16-tooling-qualification-v2/attempt-20261028-implementation-03/qualification-ledger.json"
INVALID_KEYS = ("", " ", "1TOKEN", "_TOKEN", "TOKEN_", "APP__API_TOKEN",
                "App::Token", "APP_API_TOKEN\n", "A" * 257)
SCHEMAS = ("hexalith.module-manifest.v2.json", "hexalith.test-environment.v1.json",
           "hexalith.test-result.v1.json")
REPOSITORY = "Hexalith/Hexalith.Builds"
LIMITS = dict(collectionSeconds=1800, networkSeconds=300, restoreSeconds=300,
              validationSeconds=60, diagnosticSeconds=600, streamBytes=8 * 1024**2,
              archiveBytes=256 * 1024**2, expandedBytes=2 * 1024**3,
              watchedEntries=100000, watchedDepth=64)
# Qualification/native availability remains a separate verdict. Diagnostic CLI
# controls require independently verified package/source bytes and their restore.
EXECUTION_PREREQUISITES = ("entry", "producer-provenance", "isolated-restore",
                           "published-artifact", "qualified-unsigned-archive")


class QualificationError(Exception):
    """An identified qualification defect or an unavailable external input."""

    def __init__(self, code: str, detail: str, state: str = "FAIL"):
        super().__init__(detail)
        self.code, self.state = code, state


def need(condition: bool, code: str, detail: str) -> None:
    """Fail a measured assertion without classifying it as unavailability."""
    if not condition:
        raise QualificationError(code, detail)


def sha(data: bytes) -> str:
    """Hash the bytes actually observed."""
    return hashlib.sha256(data).hexdigest()


def load_json(data: bytes) -> object:
    """Reject duplicate JSON fields instead of silently replacing identities."""
    def pairs(rows):
        result = {}
        for key, value in rows:
            need(key not in result, "IDENTITY_MISMATCH", f"duplicate JSON field: {key}")
            result[key] = value
        return result
    try:
        return json.loads(data.decode("utf-8-sig"), object_pairs_hook=pairs)
    except (UnicodeError, ValueError) as error:
        raise QualificationError("IDENTITY_MISMATCH", f"invalid JSON evidence: {error}") from error


@dataclass
class Deadline:
    """Share one monotonic deadline across every collection operation."""

    seconds: float = LIMITS["collectionSeconds"]
    started: float = 0

    def __post_init__(self):
        self.started = time.monotonic()

    def remaining(self, maximum: float) -> float:
        remaining = min(maximum, self.seconds - (time.monotonic() - self.started))
        if remaining <= 0:
            raise QualificationError("COLLECTION_TIMEOUT", "overall collection deadline reached", "BLOCKED")
        return remaining


class FreshOutput:
    """Own a newly created, non-aliased local observation directory."""

    def __init__(self, repository: Path, output: Path):
        self.repository = repository
        absolute = Path(os.path.abspath(output))
        try:
            relative = absolute.relative_to(repository).as_posix()
            relative_path(relative)
            need(relative.startswith(OBSERVATIONS + "/"), "OUTPUT_FORBIDDEN", "output must be a fresh qualification observation")
            target = output_target(repository, relative)
            need(not target.exists() and not target.is_symlink(), "OUTPUT_FORBIDDEN", "output already exists or aliases an input")
            parent = target.parent
            while not parent.exists():
                parent = parent.parent
            need(parent.resolve() == parent, "OUTPUT_FORBIDDEN", "output has an aliased ancestor")
            target.parent.mkdir(parents=True, exist_ok=True)
            target.mkdir(mode=0o700)
            self.path = target
            self.identity = (target.stat().st_dev, target.stat().st_ino)
        except (ValueError, OSError, OutputError, ReadError) as error:
            raise QualificationError("OUTPUT_FORBIDDEN", str(error)) from error

    def target(self, name: str) -> Path:
        """Refuse changed output ownership and existing/symlinked leaves."""
        relative_path(name)
        observed = self.path.lstat()
        need(stat.S_ISDIR(observed.st_mode) and (observed.st_dev, observed.st_ino) == self.identity,
             "OUTPUT_FORBIDDEN", "output directory ownership changed")
        try:
            target = output_target(self.repository, (self.path / name).relative_to(self.repository).as_posix())
        except OutputError as error:
            raise QualificationError("OUTPUT_FORBIDDEN", str(error)) from error
        need(not target.exists() and not target.is_symlink(), "OUTPUT_FORBIDDEN", f"output would overwrite: {name}")
        target.parent.mkdir(parents=True, exist_ok=True)
        return target

    def write(self, name: str, data: bytes) -> Path:
        """Create one owned file without following an existing leaf."""
        target = self.target(name)
        with target.open("xb") as stream:
            stream.write(data)
        return target

    def json(self, name: str, value: object) -> Path:
        """Retain a human-readable machine record in the fresh observation."""
        return self.write(name, (json.dumps(value, indent=2, ensure_ascii=True) + "\n").encode())


class Runner:
    """Stream bounded native outputs and terminate the owned process group."""

    def __init__(self, output: FreshOutput, deadline: Deadline):
        self.output, self.deadline, self.commands = output, deadline, []

    def run(self, name: str, args: list[str], cwd: Path, *, env: dict[str, str] | None = None,
            maximum: float = LIMITS["validationSeconds"], watch_root: Path | None = None) -> tuple[dict, bytes]:
        timeout = self.deadline.remaining(maximum)
        if os.name != "posix":
            raise QualificationError("SAFE_CANCELLATION_UNAVAILABLE", "POSIX process groups required", "BLOCKED")
        start = time.monotonic()
        streams = {key: bytearray() for key in ("stdout", "stderr")}
        paths = {key: self.output.target(f"{name}.{key}.txt") for key in streams}
        handles = {key: path.open("xb") for key, path in paths.items()}
        result = dict(name=name, command=[str(arg) for arg in args], cwd=str(cwd),
                      environment=env or {}, timeoutSeconds=timeout,
                      streamLimitBytes=LIMITS["streamBytes"], exitCode=None, termination=None)
        process = None
        selector = selectors.DefaultSelector()
        try:
            process = subprocess.Popen(args, cwd=cwd, env=dict(os.environ, **(env or {})),
                                       stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                       stderr=subprocess.PIPE, start_new_session=True)
            result["processId"] = result["processGroup"] = process.pid
            for label, stream in (("stdout", process.stdout), ("stderr", process.stderr)):
                selector.register(stream, selectors.EVENT_READ, label)
            while selector.get_map() or process.poll() is None:
                if time.monotonic() - start >= timeout:
                    result["termination"] = "PROCESS_TIMEOUT"
                    break
                if watch_root is not None:
                    def check_watch_deadline():
                        self.deadline.remaining(maximum)
                        if time.monotonic() - start >= timeout:
                            raise QualificationError("PROCESS_TIMEOUT", "watched-tree deadline reached", "BLOCKED")
                    watch_tree(watch_root, check_watch_deadline)
                for key, _ in selector.select(min(.05, timeout)):
                    data = os.read(key.fd, 65536)
                    label = key.data
                    if not data:
                        selector.unregister(key.fileobj)
                        continue
                    if len(streams[label]) + len(data) > LIMITS["streamBytes"]:
                        result["termination"] = "CAPTURE_LIMIT"
                        break
                    streams[label].extend(data)
                    handles[label].write(data)
                if result["termination"]:
                    break
        except QualificationError as error:
            result["termination"], result["detail"] = error.code, str(error)
        except OSError as error:
            result["termination"] = "PROCESS_UNAVAILABLE"
            result["detail"] = str(error)
        except BaseException:
            result["termination"] = "COLLECTION_CANCELLED"
            raise
        finally:
            if process is not None:
                # poll() retains a successful leader's exit before group cleanup.
                # Descendants can remain alive after closing inherited streams.
                process.poll()
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                process.wait(timeout=5)
                result["exitCode"] = process.returncode
            selector.close()
            for handle in handles.values():
                handle.close()
            if process is not None:
                process.stdout.close()
                process.stderr.close()
            result["durationSeconds"] = round(time.monotonic() - start, 6)
            for label, data in streams.items():
                result[label] = dict(path=paths[label].relative_to(self.output.path).as_posix(),
                                     bytes=len(data), sha256=sha(data), complete=result["termination"] is None)
            self.commands.append(result)
        return result, bytes(streams["stdout"])

    def checked(self, name: str, args: list[str], cwd: Path, **kwargs) -> bytes:
        """Preserve a native exit before turning unavailable execution into a blocker."""
        result, data = self.run(name, args, cwd, **kwargs)
        if result["termination"] or result["exitCode"] != 0:
            raise QualificationError(result["termination"] or "NATIVE_COMMAND_UNAVAILABLE",
                                     f"{name}: native exit {result['exitCode']}; {result['termination']}", "BLOCKED")
        return data


def watch_tree(root: Path, check_deadline) -> None:
    """Bound every directory entry, including zero-size files, without following links."""
    pending, total, entries = [(root, 0)], 0, 0
    while pending:
        check_deadline()
        directory, depth = pending.pop()
        if depth > LIMITS["watchedDepth"]:
            raise QualificationError("WATCH_TRAVERSAL_LIMIT", "watched-tree depth bound reached", "BLOCKED")
        with os.scandir(directory) as iterator:
            while True:
                check_deadline()
                entry = next(iterator, None)
                if entry is None:
                    break
                entries += 1
                if entries > LIMITS["watchedEntries"]:
                    raise QualificationError("WATCH_TRAVERSAL_LIMIT", "watched-tree traversal bound reached", "BLOCKED")
                observed = entry.stat(follow_symlinks=False)
                if stat.S_ISDIR(observed.st_mode):
                    pending.append((Path(entry.path), depth + 1))
                elif stat.S_ISREG(observed.st_mode):
                    total += observed.st_size
                    if entry.name.endswith(".nupkg") and observed.st_size > LIMITS["archiveBytes"]:
                        raise QualificationError("ARCHIVE_LIMIT", "watched archive exceeds byte limit", "BLOCKED")
                    if total > LIMITS["expandedBytes"]:
                        raise QualificationError("EXPANSION_LIMIT", "watched contents exceed expansion limit", "BLOCKED")


def check_signature(result: dict, diagnostics: bytes) -> None:
    """Separate proven NuGet signature/integrity defects from unavailable verification."""
    text = diagnostics.decode(errors="replace")
    invalid = re.search(r"\bNU300[58]\b|package integrity check failed|invalid package signature|signature validation failed|signature is invalid(?! or cannot be verified on this platform)", text, re.I)
    if invalid:
        raise QualificationError("SIGNATURE_INVALID", "NuGet reported a proven invalid signature or package integrity")
    if result["termination"] or result["exitCode"] != 0 or re.search(r"revocation server could not be reached|unable to check revocation", text, re.I):
        raise QualificationError("SIGNATURE_VERIFICATION_UNAVAILABLE", "NuGet verification did not complete successfully: " + str(result["termination"] or result["exitCode"]), "BLOCKED")


TRANSFER_WORKER = r"""
import hashlib, http.client, json, pathlib, sys, urllib.error, urllib.request

url, destination, prefix, limit_text, timeout_text = sys.argv[1:]
limit, timeout = int(limit_text), float(timeout_text)

class TransferFailure(Exception):
    def __init__(self, code, detail, state='BLOCKED'):
        self.code, self.detail, self.state = code, detail, state

try:
    if not url.startswith(prefix):
        raise TransferFailure('IDENTITY_MISMATCH', 'unexpected artifact feed URL', 'FAIL')
    with urllib.request.urlopen(url, timeout=timeout) as response:
        if not response.url.startswith(prefix):
            raise TransferFailure('IDENTITY_MISMATCH', 'unexpected artifact feed redirect', 'FAIL')
        headers = dict(response.headers)
        length = response.headers.get('Content-Length')
        if length is not None and not length.isdecimal():
            raise TransferFailure('INCOMPLETE_TRANSFER', 'invalid HTTP Content-Length')
        digest, count = hashlib.sha256(), 0
        with pathlib.Path(destination).open('xb') as stream:
            while True:
                block = response.read(min(65536, limit + 1 - count))
                if not block:
                    break
                if count + len(block) > limit:
                    raise TransferFailure('CAPTURE_LIMIT', 'artifact transfer exceeds byte cap')
                count += len(block)
                digest.update(block)
                stream.write(block)
        if length is not None and count != int(length):
            raise TransferFailure('INCOMPLETE_TRANSFER', 'premature HTTP EOF before Content-Length')
        print(json.dumps(dict(state='PASS', url=url, responseUrl=response.url,
                              headers=headers, bytes=count, sha256=digest.hexdigest())))
except TransferFailure as error:
    print(json.dumps(dict(state=error.state, code=error.code, detail=error.detail)))
    sys.exit(2 if error.state == 'BLOCKED' else 1)
except (OSError, urllib.error.URLError, http.client.HTTPException) as error:
    print(json.dumps(dict(state='BLOCKED', code='ARTIFACT_UNAVAILABLE', detail=str(error))))
    sys.exit(2)
"""


def bounded_transfer(runner: Runner, name: str, url: str, cwd: Path, *, archive: bool = False,
                     allowed_prefix: str = "https://api.nuget.org/") -> Path:
    """Let the parent enforce wall time while an owned worker performs blocking I/O.

    The worker writes a separate capped body file; its two native streams retain
    their 8 MiB limits. The process deadline covers opening, redirects and every
    read, including a server that continuously trickles below the socket timeout.
    Only tests supply a loopback prefix; the production caller uses the default.
    """
    need(url.startswith(allowed_prefix), "IDENTITY_MISMATCH", "unexpected artifact feed URL")
    maximum = LIMITS["archiveBytes"] if archive else LIMITS["streamBytes"]
    timeout = runner.deadline.remaining(LIMITS["networkSeconds"])
    target = runner.output.target(name)
    result, data, failure = None, b"", None
    try:
        result, data = runner.run("network-" + name, [sys.executable, "-c", TRANSFER_WORKER,
                                  url, str(target), allowed_prefix, str(maximum), str(timeout)],
                                  cwd, maximum=timeout)
        if result["termination"]:
            raise QualificationError(result["termination"], "network worker terminated before complete capture", "BLOCKED")
        if not data:
            raise QualificationError("NETWORK_WORKER_ERROR", "network worker returned no result")
        report = load_json(data)
        if report.get("state") != "PASS":
            need(report.get("state") in ("FAIL", "BLOCKED") and report.get("code") in
                 ("IDENTITY_MISMATCH", "ARTIFACT_UNAVAILABLE", "CAPTURE_LIMIT", "INCOMPLETE_TRANSFER"),
                 "NETWORK_WORKER_ERROR", "network worker returned an invalid failure")
            raise QualificationError(report["code"], report.get("detail", "transfer failed"), report["state"])
        need(result["exitCode"] == 0 and target.is_file() and target.stat().st_size <= maximum,
             "NETWORK_WORKER_ERROR", "network success has no complete bounded file")
        digest, count = hashlib.sha256(), 0
        with target.open("rb") as stream:
            while block := stream.read(65536):
                runner.deadline.remaining(LIMITS["networkSeconds"])
                count += len(block)
                need(count <= maximum, "NETWORK_WORKER_ERROR", "network worker exceeded byte cap")
                digest.update(block)
        need(report.get("bytes") == count and report.get("sha256") == digest.hexdigest(),
             "NETWORK_WORKER_ERROR", "network worker file/hash disagrees with independently read bytes")
    except QualificationError as error:
        failure = error
        raise
    except KeyboardInterrupt:
        failure = QualificationError("COLLECTION_CANCELLED", "network transfer cancelled; owned worker group stopped", "BLOCKED")
        raise
    except Exception as error:
        failure = QualificationError("NETWORK_WORKER_ERROR", f"{type(error).__name__}: {error}")
        raise
    finally:
        # Keep failures and partial bodies as incomplete evidence, never as a
        # silently truncated success. Only the capped regular body is rehashed.
        digest, count = hashlib.sha256(), 0
        if target.exists():
            with target.open("rb") as stream:
                while block := stream.read(65536):
                    count += len(block)
                    need(count <= maximum, "NETWORK_WORKER_ERROR", "network worker exceeded byte cap")
                    digest.update(block)
        metadata = dict(url=url, bytes=count, sha256=digest.hexdigest(), timeoutSeconds=timeout,
                        limitBytes=maximum, complete=failure is None, state=failure.state if failure else "PASS",
                        workerCommand="network-" + name)
        if failure:
            metadata.update(code=failure.code, detail=str(failure))
        elif data:
            metadata.update(headers=report["headers"], responseUrl=report["responseUrl"])
        runner.output.json(name + ".http.json", metadata)
    return target


def validate_inventory(data: dict, version: str, revision: str) -> dict:
    """Check exact producer Release identity, package roles, and non-vacuous controls."""
    need(data.get("schema") == "hexalith.g4-tool-package-inventory.v1" and
         data.get("version") == version and data.get("configuration") == "Release",
         "IDENTITY_MISMATCH", "inventory is not the exact candidate Release inventory")
    rows = data.get("packages", [])
    need(len(rows) == 2 and {row.get("id") for row in rows} ==
         {"Hexalith.Builds.Module.Cli", "Hexalith.Builds.Evidence.Cli"},
         "IDENTITY_MISMATCH", "inventory must contain exactly both approved package IDs")
    for row in rows:
        need(row.get("version") == version, "IDENTITY_MISMATCH", "package version substitution")
        for role in ("nupkg", "snupkg"):
            artifact = row.get(role, {})
            need(artifact.get("file") == f"{row['id']}.{version}.{role}" and
                 re.fullmatch(r"[a-fA-F0-9]{64}", artifact.get("sha256", "")) is not None and
                 type(artifact.get("sizeBytes")) is int and artifact["sizeBytes"] > 0,
                 "IDENTITY_MISMATCH", "package role, name, hash or size substitution")
    qualification = data.get("qualification", {})
    need(qualification.get("sourceTree") == dict(clean=True, revision=revision),
         "IDENTITY_MISMATCH", "inventory source revision/clean identity disagrees")
    need(qualification.get("releaseEligible") is True and qualification.get("ineligibilityReasons") == [],
         "CONTROLS_INCOMPLETE", "producer inventory is not release eligible")
    for lane, mode in (("packageBuild", "official"), ("sourceValidation", "executed"), ("controls", "executed")):
        need(qualification.get(lane) == dict(mode=mode, result="passed"),
             "CONTROLS_INCOMPLETE", f"producer {lane} is partial, skipped or failed")
    for label, inventory in (("fixtures", qualification.get("fixtures", {}).get("files", [])),
                             ("qualificationEvidence", data.get("qualificationEvidence", []))):
        paths = [row.get("file") for row in inventory]
        need(bool(paths) and len(paths) == len(set(paths)), "CONTROLS_INCOMPLETE", f"empty/duplicate {label}")
        for row in inventory:
            relative_path(row["file"])
            need(re.fullmatch(r"[a-fA-F0-9]{64}", row.get("sha256", "")) is not None and
                 type(row.get("sizeBytes")) is int and row["sizeBytes"] >= 0,
                 "IDENTITY_MISMATCH", f"invalid {label} hash/size")
    return next(row for row in rows if row["id"] == "Hexalith.Builds.Module.Cli")


def configuration_fields(data: dict) -> list[tuple[tuple, str]]:
    """Find the app-channel secret and its extension input by declared relationship."""
    matches = []
    for mi, module in enumerate(data.get("modules", [])):
        for si, secret in enumerate(module.get("integration", {}).get("secrets", [])):
            if secret.get("configurationKey") != "APP_API_TOKEN":
                continue
            inputs = []
            for ei, extension in enumerate(module.get("runtime", {}).get("extensions", [])):
                for ki, item in enumerate(extension.get("configurationInputs", [])):
                    if item.get("source") == "secret" and item.get("reference") == secret.get("name") and item.get("key") == "APP_API_TOKEN":
                        inputs.append((("modules", mi, "runtime", "extensions", ei, "configurationInputs", ki, "key"),
                                       f"modules[{mi}].runtime.extensions[{ei}].configurationInputs[{ki}].key"))
            matches.append([(("modules", mi, "integration", "secrets", si, "configurationKey"),
                             f"modules[{mi}].integration.secrets[{si}].configurationKey"), *inputs])
    need(len(matches) == 1 and len(matches[0]) == 2, "CONTROLS_INCOMPLETE", "require exactly one semantically paired app-channel key")
    return matches[0]


def mutate(data: dict, path: tuple, value: str) -> dict:
    """Create a disposable copy without changing the original declaration."""
    result = copy.deepcopy(data)
    target = result
    for part in path[:-1]:
        target = target[part]
    target[path[-1]] = value
    return result


def check_negative(result: dict, stdout: bytes, field: str, source: Path, cwd: Path, format_name: str, stderr: bytes = b"") -> None:
    """Require one exact-field HXP002 and the actual malformed input identity."""
    if result["termination"]:
        raise QualificationError(result["termination"], "incomplete malformed-key execution", "BLOCKED")
    need(result["exitCode"] == 1, "CONTROL_MISMATCH", "malformed-key native exit must be 1")
    if format_name == "json":
        need(not re.search(rb"(?m)^\s*HX[A-Z]+\d+\b", stderr), "CONTROL_MISMATCH", "unexpected stderr diagnostic")
        report = load_json(stdout)
        diagnostics = report.get("diagnostics", [])
        need(report.get("status") == "failed" and len(diagnostics) == 1,
             "CONTROL_MISMATCH", "negative must return exactly one failed diagnostic")
        diagnostic = diagnostics[0]
        code, actual_field, actual_source = diagnostic.get("ruleId"), diagnostic.get("field"), diagnostic.get("source")
    else:
        lines = [line.strip() for line in (stdout + b"\n" + stderr).decode().splitlines() if re.match(r"^\s*HX[A-Z]+\d+\b", line)]
        need(len(lines) == 1, "CONTROL_MISMATCH", "human negative must contain exactly one diagnostic")
        matched = re.fullmatch(r"(HXP\d+) \S+ \S+ source=(.+?) field=(.+?):.*", lines[0])
        need(matched is not None, "CONTROL_MISMATCH", "human diagnostic shape changed")
        code, actual_source, actual_field = matched.groups()
    need(code == "HXP002" and actual_field == field and isinstance(actual_source, str) and
         (cwd / actual_source).resolve() == source.resolve(), "CONTROL_MISMATCH", "negative diagnostic field/source identity disagrees")


def check_control_inventory(rows: list[dict], fields: list[str]) -> None:
    """Compare exact identities rather than only a control count."""
    expected = Counter((field, value, format_name) for field in fields for value in INVALID_KEYS for format_name in ("human", "json"))
    observed = Counter((row.get("field"), row.get("value"), row.get("format")) for row in rows)
    need(observed == expected and len(rows) == 36 and all(row.get("passed") is True and
         row.get("nativeExitCode") == 1 and row.get("ruleId") == "HXP002" for row in rows),
         "CONTROLS_INCOMPLETE", "missing, extra, duplicate or failed malformed-key controls")


def human_adoption(data: bytes) -> list[dict]:
    """Preserve the native ordered adoption row identities."""
    rows = []
    for line in data.decode().splitlines():
        match = re.fullmatch(r"(PASS|FAIL) (\S+) file=(.*?): (.*)", line)
        if match:
            rows.append(dict(requirement=match[2], passed=match[1] == "PASS", file=match[3], reason=match[4]))
    return rows


def adoption_delta(previous: list[dict], current: list[dict]) -> dict:
    """Retain multiplicity, order, reason changes and every actual row delta."""
    def keys(rows):
        return [json.dumps(row, sort_keys=True, separators=(",", ":")) for row in rows]
    before, after = Counter(keys(previous)), Counter(keys(current))
    need(bool(previous) and bool(current), "ADOPTION_INVENTORY_DRIFT", "adoption inventory is empty")
    need(all(count == 1 for count in before.values()) and all(count == 1 for count in after.values()),
         "ADOPTION_INVENTORY_DRIFT", "duplicate adoption rows")
    removed, added = before - after, after - before
    identities = lambda row: (row["requirement"], row["file"])
    old = {identities(row): row for row in previous}
    changes = [dict(previous=old[identities(row)], current=row) for row in current
               if identities(row) in old and old[identities(row)] != row]
    return dict(orderedEqual=previous == current,
                removed=[json.loads(row) for row in removed.elements()],
                added=[json.loads(row) for row in added.elements()], reasonChanges=changes,
                predecessorRows=previous, currentRows=current)


def disposition(row: dict) -> dict:
    """Assign accountable owner scope and a proposed action without granting acceptance."""
    path, requirement = row["file"], row["requirement"]
    if path.startswith("module/validation/"):
        classification = "generated import" if "/prepared-imports/" in path or "/qualification-prepared-imports/" in path else "historical source"
    elif path.startswith("tests/fixtures/"):
        classification = "accepted fixture"
    else:
        classification = "live consumer"
    if requirement == "tool-pin":
        owner, remedy = "Hexalith.Conversations + Hexalith.Platform maintainers", "Select and approve an integration-qualified exact CLI/Platform tuple before a separately scoped pin alignment."
    elif requirement == "dependency-selectors":
        owner = "Hexalith.Builds producer policy owner + Hexalith.Conversations maintainer"
        remedy = "Reproduce this exact tracked-path finding; separately review producer policy, generated-import handling, fixture migration or consumer correction without excluding scanner paths."
    else:
        owner = "Hexalith.Conversations maintainer + owning Platform/EventStore policy owner"
        remedy = "Review the exact literal/component use and injected-key replacement with a named regression; preserve historical/fixture bytes until an approved migration exists."
    return dict(**row, classification=classification, ownerScope=owner, evidence="adoption-json.stdout.txt",
                proposedRemedy=remedy, acceptanceState="pending", nextAction="Owner review and separately scoped correction; classification does not waive this failure.")


def package_payload(path: Path, deadline: Deadline) -> list[dict]:
    """Hash every uncompressed payload entry while enforcing expansion limits."""
    rows, total = [], 0
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        names = [entry.filename for entry in entries]
        need(len(names) == len(set(names)), "IDENTITY_MISMATCH", "duplicate archive members")
        for entry in sorted(entries, key=lambda item: item.filename):
            deadline.remaining(LIMITS["networkSeconds"])
            relative_path(entry.filename.rstrip("/"))
            need(not stat.S_ISLNK(entry.external_attr >> 16), "IDENTITY_MISMATCH", "archive symlink entry")
            total += entry.file_size
            need(total <= LIMITS["expandedBytes"], "ARCHIVE_LIMIT", "expanded package exceeds 2 GiB")
            if entry.is_dir():
                continue
            digest, count = hashlib.sha256(), 0
            with archive.open(entry) as stream:
                while block := stream.read(65536):
                    deadline.remaining(LIMITS["networkSeconds"])
                    count += len(block)
                    need(count <= entry.file_size and count <= LIMITS["expandedBytes"], "ARCHIVE_LIMIT", "archive expansion exceeded declared size")
                    digest.update(block)
            need(count == entry.file_size, "IDENTITY_MISMATCH", "incomplete ZIP entry")
            rows.append(dict(file=entry.filename, sha256=digest.hexdigest(), sizeBytes=count))
    return rows


def compare_unsigned(path: Path, expected: dict, signed_rows: list[dict], deadline: Deadline) -> dict:
    """Require full CI archive equality and independently equal uncompressed payload."""
    need(path.stat().st_size <= LIMITS["archiveBytes"], "ARCHIVE_LIMIT", "unsigned archive exceeds archive bound")
    raw = path.read_bytes()
    need(sha(raw) == expected["sha256"].lower() and len(raw) == expected["sizeBytes"],
         "IDENTITY_MISMATCH", "reconstructed unsigned full archive differs from exact qualified CI archive")
    payload = package_payload(path, deadline)
    need(payload == [row for row in signed_rows if row["file"] != ".signature.p7s"],
         "IDENTITY_MISMATCH", "signed and qualified unsigned uncompressed payloads disagree")
    return dict(sha256=sha(raw), sizeBytes=len(raw), exactCiArchiveEquality=True, uncompressedPayloadEqual=True)


def compare_catalog(props: bytes, catalog: dict) -> dict:
    """Compare catalog selections to explicit defaults in the exact source Props."""
    document = ET.fromstring(props)
    values = {}
    for group in document.findall("PropertyGroup"):
        if group.get("Condition"):
            continue
        for node in group:
            condition = node.get("Condition", "")
            default = f"'$({node.tag})' == ''"
            if condition and condition != default:
                continue
            if not condition or not values.get(node.tag):
                values[node.tag] = node.text or ""
    packages = {}
    for group in document.findall("ItemGroup"):
        if group.get("Condition"):
            continue
        for node in group.findall("PackageVersion"):
            if not node.get("Condition"):
                value = node.get("Version", "")
                match = re.fullmatch(r"\$\(([^)]+)\)", value)
                packages[node.get("Include")] = values.get(match[1], "") if match else value
    expected = dict(eventStoreVersion=packages.get("Hexalith.EventStore.Aspire"),
                    frontComposerVersion=packages.get("Hexalith.FrontComposer.Shell"),
                    daprSdkVersion=packages.get("Dapr.Client"), aspireHostingVersion=packages.get("Aspire.Hosting"),
                    aspireHostingDaprVersion=packages.get("CommunityToolkit.Aspire.Hosting.Dapr"))
    for field, property_name in (("aspireAppHostSdkVersion", "HexalithAspireAppHostSdkVersion"),
                                 ("daprRuntimeVersion", "HexalithDaprRuntimeVersion"), ("daprCliVersion", "HexalithDaprCliVersion"),
                                 ("redisImage", "HexalithRedisImage"), ("redisImageTag", "HexalithRedisImageTag"),
                                 ("redisImageDigest", "HexalithRedisImageDigest")):
        expected[field] = values.get(property_name)
    need(all(expected.values()), "IDENTITY_MISMATCH", "source Props catalog defaults are incomplete")
    need(all(catalog.get(key) == value for key, value in expected.items()),
         "IDENTITY_MISMATCH", "embedded catalog disagrees with exact source Props selections")
    return expected


def platform_catalog_binding(raw_gitlink: str, documented_source: str) -> bool:
    """Compare the available recorded catalog identity separately from missing tags."""
    match = re.fullmatch(r"160000 commit ([a-f0-9]{40})\treferences/Hexalith.Builds", raw_gitlink)
    need(match is not None, "IDENTITY_MISMATCH", "Platform Builds dependency is not an exact raw gitlink")
    return match[1] == documented_source


def prepared_platform_identity(source: bytes) -> dict:
    """Read producer declarations without importing or executing owner code."""
    try:
        assignments = {node.targets[0].id: ast.literal_eval(node.value)
                       for node in ast.parse(source).body
                       if isinstance(node, ast.Assign) and len(node.targets) == 1
                       and isinstance(node.targets[0], ast.Name)
                       and node.targets[0].id in ("VERSION", "SOURCE", "ARCHIVE_SHA256", "TOOLING_SHA256")}
    except (SyntaxError, ValueError, TypeError):
        raise QualificationError("IDENTITY_MISMATCH", "invalid prepared Platform identity declarations") from None
    need(set(assignments) == {"VERSION", "SOURCE", "ARCHIVE_SHA256", "TOOLING_SHA256"},
         "IDENTITY_MISMATCH", "incomplete prepared Platform identity declarations")
    need(isinstance(assignments["VERSION"], str) and re.fullmatch(r"\d+\.\d+\.\d+", assignments["VERSION"]) is not None
         and isinstance(assignments["SOURCE"], str) and re.fullmatch(r"[a-f0-9]{40}", assignments["SOURCE"]) is not None
         and all(isinstance(assignments[key], str) and re.fullmatch(r"[a-f0-9]{64}", assignments[key]) is not None
                 for key in ("ARCHIVE_SHA256", "TOOLING_SHA256")),
         "IDENTITY_MISMATCH", "malformed prepared Platform identity declarations")
    return assignments


UNSIGN_SOURCE = """using NuGet.Packaging.Signing;

/// <summary>Removes NuGet signing metadata into a fresh, separate archive.</summary>
internal static class Program
{
    /// <summary>Retains the signed input and creates one unsigned reconstruction.</summary>
    /// <param name="args">The signed input and fresh unsigned output paths.</param>
    /// <returns>The asynchronous completion.</returns>
    internal static async Task Main(string[] args)
    {
        using FileStream input = new(args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream output = new(args[1], FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using SignedPackageArchive archive = new(input, output);
        await archive.RemoveSignatureAsync(CancellationToken.None);
        await output.FlushAsync();
    }
}
"""


RESOURCE_PATH_GUARD = r"""
function Get-ContainedResourcePath([string]$Directory, [string]$Name) {
 if([string]::IsNullOrWhiteSpace($Name) -or $Name.Contains('..') -or $Name.IndexOfAny([char[]]'/\:') -ge 0 -or [IO.Path]::IsPathRooted($Name)) { throw 'Invalid embedded resource name' }
 $root=[IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
 $path=[IO.Path]::GetFullPath([IO.Path]::Combine($root,$Name))
 if(-not $path.StartsWith($root,[StringComparison]::Ordinal)) { throw 'Embedded resource path escape' }
 return $path
}
"""

INSPECTOR = r"""
param([string]$AssemblyPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
""" + RESOURCE_PATH_GUARD + r"""
$assembly=[Reflection.Assembly]::LoadFrom($AssemblyPath)
$resources=@()
foreach($name in @($assembly.GetManifestResourceNames() | Sort-Object)) {
 if(-not $name.EndsWith('.json',[StringComparison]::Ordinal)){continue}
 $path=Get-ContainedResourcePath $OutputDirectory $name
 $stream=$assembly.GetManifestResourceStream($name)
 if($stream.Length -gt 8388608){throw 'Embedded resource exceeds 8 MiB'}
 $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
 try{$stream.CopyTo($output)}finally{$output.Dispose();$stream.Dispose()}
 $resources+=@{name=$name;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$version=@($assembly.GetCustomAttributesData() | Where-Object {$_.AttributeType.Name -ceq 'AssemblyInformationalVersionAttribute'})[0].ConstructorArguments[0].Value
@{informationalVersion=$version;assemblyLocation=$assembly.Location;resources=$resources}|ConvertTo-Json -Depth 5
"""


class Collector:
    """Measure independent gates, preserving failures and unavailable evidence."""

    def __init__(self, args):
        self.args = args
        self.root = Path(os.path.abspath(args.conversations))
        self.builds, self.platform = Path(os.path.abspath(args.builds)), Path(os.path.abspath(args.platform))
        for path in (self.root, self.builds, self.platform):
            need(path.resolve(strict=True) == path, "PATH_ESCAPE", "repository roots cannot be aliases")
        need(self.builds == self.root / "references/Hexalith.Builds" and
             self.platform == self.root / "references/Hexalith.Platform", "PATH_ESCAPE", "dependencies must be contained direct root repositories")
        need(re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", args.version) is not None and
             re.fullmatch(r"[a-f0-9]{40}", args.release_sha) is not None,
             "IDENTITY_MISMATCH", "exact version and full source revision required")
        inventory = Path(os.path.abspath(args.inventory))
        need(inventory.is_relative_to(self.root), "PATH_ESCAPE", "inventory must be contained by Conversations")
        self.inventory_relative = inventory.relative_to(self.root).as_posix()
        relative_path(self.inventory_relative)
        for name, default in (("planning_inputs", PLANNING_INPUTS), ("adoption_predecessor", PREDECESSOR),
                              ("tooling_contract", TOOLING_CONTRACT)):
            selected = Path(getattr(args, name, None) or default)
            selected = selected if selected.is_absolute() else self.root / selected
            need(selected.is_relative_to(self.root), "PATH_ESCAPE", name + " must be contained by Conversations")
            value = selected.relative_to(self.root).as_posix()
            relative_path(value)
            setattr(self, name, value)
        need(re.fullmatch(r"_bmad-output/planning-artifacts/epic-16-tooling-v[123]/story-contracts/16\.[123]\.json", self.tooling_contract) is not None,
             "IDENTITY_MISMATCH", "unknown exact tooling contract selector")
        self.output = FreshOutput(self.root, args.output)
        self.deadline = Deadline()
        self.runner = Runner(self.output, self.deadline)
        self.ledger, self.identity = [], {}
        self.readers = ExitStack()
        self.reader = self.readers.enter_context(ContainedReader(self.root))
        self.builds_reader = self.readers.enter_context(ContainedReader(self.builds))
        self.platform_reader = self.readers.enter_context(ContainedReader(self.platform))
        self.originals = {}

    def record(self, gate: str, state: str, code: str, detail: str, **facts):
        self.ledger.append(dict(gate=gate, state=state, code=code, detail=detail, **facts))

    def gate(self, name: str, action):
        try:
            action()
        except QualificationError as error:
            self.record(name, error.state, error.code, str(error))
        except ReadError as error:
            self.record(name, "BLOCKED" if error.unavailable else "FAIL", error.code, str(error))

    def require_candidate_prerequisites(self, *, version: bool = True):
        """Execute no candidate command after missing or failed prerequisite evidence."""
        missing = [gate for gate in EXECUTION_PREREQUISITES if not any(row["gate"] == gate and row["state"] == "PASS" for row in self.ledger)
                   or any(row["gate"] == gate and row["state"] in ("FAIL", "BLOCKED") for row in self.ledger)]
        if missing or version and not getattr(self, "candidate_verified", False):
            raise QualificationError("CANDIDATE_EXECUTION_BLOCKED", "candidate prerequisites unavailable: " + ", ".join(missing or ["candidate-version"]), "BLOCKED")

    def git(self, label: str, repository: Path, *args: str) -> bytes:
        return self.runner.checked(label, ["git", *args], repository)

    def api(self, label: str, path: str) -> object:
        return load_json(self.runner.checked(label, ["gh", "api", path], self.root, maximum=LIMITS["networkSeconds"]))

    def protect(self, path: str):
        self.originals[path] = sha(self.reader.read(path))

    def index_digest(self, label):
        # Hash the complete index without emitting its large path inventory into
        # a diagnostic stream. The child shares the runner's cancellation group.
        worker = "import hashlib,pathlib,subprocess; p=subprocess.run(['git','rev-parse','--git-path','index'],check=True,capture_output=True,text=True).stdout.strip(); h=hashlib.sha256(); f=pathlib.Path(p).open('rb'); n=0\nwhile b:=f.read(65536):\n n+=len(b)\n if n>268435456: raise ValueError('index size bound')\n h.update(b)\nprint(h.hexdigest())"
        value = self.runner.checked(label, [sys.executable, "-c", worker], self.root).decode().strip()
        need(re.fullmatch(r"[a-f0-9]{64}", value) is not None, "IDENTITY_MISMATCH", "invalid complete index digest")
        return value

    def entry(self):
        package = str(PurePosixPath(self.tooling_contract).parent.parent)
        version = package.rsplit("-", 1)[1]
        for path in (MANIFEST, ".config/dotnet-tools.json", ".gitmodules", ".gitattributes",
                     package + "/binding.json", self.tooling_contract, self.planning_inputs, self.adoption_predecessor,
                     "_bmad/scripts/epic16_trust.py" if version == "v1" else f"_bmad/scripts/epic16_trust_{version}.py"):
            self.protect(path)
        selected_binding = load_json(self.reader.read(package + "/binding.json"))
        for row in selected_binding["sources"]:
            self.protect(row["path"])
        self.protect("_bmad/scripts/qualify_epic16_published_artifact.py")
        self.protect("tests/tooling/test_epic16_published_artifact_qualification.py")
        planning = load_json(self.reader.read(self.planning_inputs))
        for row in planning["sources"]:
            self.protect(row["path"])
        planning_differences = [dict(path=row["path"], plannedSha256=row["sha256"], actualSha256=self.originals[row["path"]])
                                for row in planning["sources"] if self.originals[row["path"]] != row["sha256"]]
        actual_root = self.git("root-identity", self.root, "rev-parse", "--show-toplevel").decode().strip()
        need(actual_root == str(self.root), "PATH_ESCAPE", "Conversations path is not its owning repository root")
        tool_manifest = load_json(self.reader.read(".config/dotnet-tools.json"))
        need(isinstance(tool_manifest, dict) and isinstance(tool_manifest.get("tools"), dict)
             and isinstance(tool_manifest["tools"].get("hexalith.builds.module.cli"), dict),
             "IDENTITY_MISMATCH", "root tool pin metadata is missing or malformed")
        pin = tool_manifest["tools"]["hexalith.builds.module.cli"]
        need(isinstance(pin.get("version"), str) and
             re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", pin["version"]) is not None,
             "IDENTITY_MISMATCH", "root tool pin must identify a valid exact version")
        need(pin.get("rollForward") is False and (getattr(self.args, "candidate_only", False) or pin["version"] == self.args.version),
             "IDENTITY_MISMATCH", "candidate differs from the exact disabled-roll-forward root pin")
        root_pin = dict(rootVersion=pin["version"], candidateVersion=self.args.version, rollForward=pin.get("rollForward"),
                        versionsEqual=pin["version"] == self.args.version, adoptionAccepted=False,
                        candidateOnly=getattr(self.args, "candidate_only", False))
        self.identity["rootPin"] = root_pin
        self.output.json("candidate-root-selection.json", root_pin)
        # All rows retain raw modes/object IDs/stages, while the twelve long
        # gitlink paths are captured separately. Full index bytes bind every path.
        self.entry_index_bytes = self.index_digest("root-index-digest")
        modes = self.git("root-index-modes", self.root, "ls-files", "-z", "--format=%(objectmode) %(objectname) %(stage)")
        links = self.git("root-gitlinks", self.root, "ls-files", "--stage", "-z", "--", "references/")
        self.entry_index = sha(links)
        rows = {}
        for row in links.split(b"\0"):
            if not row:
                continue
            meta, path = row.split(b"\t")
            mode, revision, stage = meta.split()
            if mode == b"160000":
                need(stage == b"0", "IDENTITY_MISMATCH", "unmerged gitlink")
                rows[os.fsdecode(path)] = revision.decode()
        need(sum(row.startswith(b"160000 ") for row in modes.split(b"\0")) == len(rows),
             "IDENTITY_MISMATCH", "unexpected gitlinks outside the exact direct dependency inventory")
        identities = []
        self.dependency_identities = []
        for relative, revision in sorted(rows.items()):
            repository = self.root / relative
            head = self.git("dependency-head-" + repository.name, repository, "rev-parse", "HEAD").decode().strip()
            dirty = self.git("dependency-status-" + repository.name, repository, "status", "--porcelain=v1", "--untracked-files=normal")
            need(head == revision and not dirty, "DEPENDENCY_IDENTITY_MISMATCH", f"dirty or unpinned dependency: {relative}")
            actual_root = self.git("dependency-root-" + repository.name, repository, "rev-parse", "--show-toplevel").decode().strip()
            need(actual_root == str(repository), "PATH_ESCAPE", "dependency is not its own contained repository: " + relative)
            nested = self.git("dependency-nested-" + repository.name, repository, "submodule", "status").decode().splitlines()
            need(all(line.startswith("-") for line in nested), "DEPENDENCY_IDENTITY_MISMATCH", "initialized nested dependency forbidden: " + relative)
            identities.append(dict(path=relative, mode="160000", revision=revision, checkout=head, clean=True, nestedStatus=nested))
            self.dependency_identities.append((relative, revision, nested))
        need(rows.get("references/Hexalith.Builds") is not None and rows.get("references/Hexalith.Platform") is not None,
             "IDENTITY_MISMATCH", "direct dependencies are not raw mode-160000 gitlinks")
        self.output.json("entry-identities.json", dict(dependencies=identities, indexRowsSha256=self.entry_index, protectedInputs=self.originals,
                         fullIndexSha256=self.entry_index_bytes, allIndexModesSha256=sha(modes),
                         planningInputs=self.planning_inputs, planningDifferences=planning_differences,
                         toolingContract=self.tooling_contract, adoptionPredecessor=self.adoption_predecessor, rootPin=root_pin))
        self.record("entry", "PASS", "INPUT_IDENTITIES_MEASURED", "Root pin and clean direct dependency tuples measured independently")
        if not root_pin["versionsEqual"]:
            self.record("root-pin-adoption", "FAIL", "ROOT_PIN_CANDIDATE_MISMATCH", "Candidate measured independently; unchanged root pin mismatch grants no adoption", **root_pin)

    def provenance(self):
        data = self.reader.read(self.inventory_relative)
        self.inventory = load_json(data)
        self.package_inventory = validate_inventory(self.inventory, self.args.version, self.args.release_sha)
        actual = self.git("release-tag-source", self.builds, "rev-parse", f"v{self.args.version}^{{commit}}").decode().strip()
        need(actual == self.args.release_sha, "IDENTITY_MISMATCH", "exact tag and requested source disagree")
        release = self.api("producer-release", f"repos/{REPOSITORY}/releases/tags/v{self.args.version}")
        need(release.get("tag_name") == f"v{self.args.version}" and not release.get("draft") and not release.get("prerelease"),
             "IDENTITY_MISMATCH", "unexpected published release identity")
        assets = [asset for asset in release.get("assets", []) if asset.get("name") == "g4-tool-package-inventory.json"]
        need(len(assets) == 1, "IDENTITY_MISMATCH", "release inventory missing or ambiguous")
        asset = assets[0]
        remote = self.runner.checked("producer-inventory", ["gh", "api", "-H", "Accept: application/octet-stream",
                                      f"repos/{REPOSITORY}/releases/assets/{asset['id']}"], self.root, maximum=LIMITS["networkSeconds"])
        need(remote == data and asset.get("size") == len(data) and asset.get("digest") == "sha256:" + sha(data),
             "IDENTITY_MISMATCH", "independent release asset/inventory hash substitution")
        runs = self.api("producer-source-runs", f"repos/{REPOSITORY}/actions/runs?head_sha={self.args.release_sha}&per_page=100")["workflow_runs"]
        self.release_run = None
        self.producer_runs = []
        self.release_assets = release.get("assets", [])
        for name, event in (("CI", "push"), ("Release", "workflow_dispatch")):
            matched = [run for run in runs if run.get("name") == name and run.get("event") == event and
                       run.get("conclusion") == "success" and run.get("status") == "completed" and run.get("head_sha") == self.args.release_sha]
            if not matched:
                raise QualificationError("ARTIFACT_UNAVAILABLE", f"successful exact-source {name} unavailable", "BLOCKED")
            run = max(matched, key=lambda item: item["id"])
            self.producer_runs.append(run)
            jobs = self.api("producer-" + name.lower() + "-jobs", f"repos/{REPOSITORY}/actions/runs/{run['id']}/jobs?per_page=100")
            self.output.json("producer-" + name.lower() + "-identity.json", dict(run=run, jobs=jobs))
            if name == "Release":
                self.release_run = run
        self.identity.update(version=self.args.version, releaseSha=actual, inventorySha256=sha(data),
                             unsignedPackage=self.package_inventory["nupkg"], releaseUrl=release["html_url"])
        self.record("producer-provenance", "PASS", "PRODUCER_IDENTITY_MEASURED", "Exact tag/source, successful CI/release and published inventory independently agree")

    def fixture_inventory(self):
        if not hasattr(self, "inventory"):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "producer inventory unavailable", "BLOCKED")
        fixtures = self.inventory["qualification"]["fixtures"]
        need(fixtures.get("mode") == "repository-tracked" and fixtures.get("root") == "test/fixtures",
             "IDENTITY_MISMATCH", "untracked/external producer fixtures")
        tree = self.git("fixture-source-tree", self.builds, "ls-tree", "-r", "--name-only", self.args.release_sha, "test/fixtures").decode().splitlines()
        expected = {path.removeprefix("test/fixtures/") for path in tree if not any(part.startswith(".") for part in PurePosixPath(path).parts)}
        rows = fixtures["files"]
        need({row["file"] for row in rows} == expected and len(rows) == len(expected),
             "IDENTITY_MISMATCH", "producer-selected visible fixture inventory differs from source")
        differences = []
        for index, row in enumerate(rows):
            source = "test/fixtures/" + row["file"]
            raw = self.git(f"fixture-raw-{index}", self.builds, "show", f"{self.args.release_sha}:{source}")
            filtered = self.git(f"fixture-filtered-{index}", self.builds, "cat-file", "--filters", f"{self.args.release_sha}:{source}")
            need(sha(filtered) == row["sha256"].lower() and len(filtered) == row["sizeBytes"],
                 "IDENTITY_MISMATCH", "fixture filtered source hash/size mismatch: " + source)
            if raw != filtered:
                differences.append(dict(file=source, rawSha256=sha(raw), filteredSha256=sha(filtered)))
        manifest = "\n".join(f"{row['file']}|{row['sha256']}|{row['sizeBytes']}" for row in rows)
        need(sha(manifest.encode()) == fixtures["sha256"].lower(), "IDENTITY_MISMATCH", "ordered fixture manifest hash mismatch")
        self.output.json("fixture-scope.json", dict(selectedRows=rows, sourceTrackedPaths=tree,
                         hiddenExclusions=sorted(set(path.removeprefix("test/fixtures/") for path in tree) - expected),
                         rawFilteredDifferences=differences, orderedManifestSha256=sha(manifest.encode())))
        self.record("producer-fixtures", "PASS", "FIXTURE_INVENTORY_MEASURED", "Exact producer-selected fixture scope and filtered/raw source identities measured")

    def download(self, name: str, url: str, *, archive: bool = False) -> Path:
        return bounded_transfer(self.runner, name, url, self.root, archive=archive)

    def restore(self):
        work = self.output.path / "isolated"
        work.mkdir()
        self.env = dict(NUGET_PACKAGES=str(work / "packages"), NUGET_HTTP_CACHE_PATH=str(work / "http-cache"),
                        DOTNET_CLI_HOME=str(work / "dotnet-home"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1",
                        NUGET_CERT_REVOCATION_MODE="online")
        config = self.output.write("isolated/NuGet.Config", b'<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>\n')
        self.output.json("isolated-environment.json", self.env)
        self.runner.checked("feed-tool-install", ["dotnet", "tool", "install", "hexalith.builds.module.cli", "--version", self.args.version,
                            "--tool-path", str(work / "tool"), "--configfile", str(config), "--no-cache"], self.root,
                            env=self.env, maximum=LIMITS["restoreSeconds"], watch_root=work)
        self.tool = work / "tool/hexalith-module"
        packages = list((work / "tool").rglob(f"hexalith.builds.module.cli.{self.args.version}.nupkg"))
        if not packages:
            raise QualificationError("ARTIFACT_UNAVAILABLE", "isolated install retained no actual archive", "BLOCKED")
        need(all(path.stat().st_size <= LIMITS["archiveBytes"] for path in packages), "ARCHIVE_LIMIT", "installed archive exceeds download bound")
        hashes = {sha(path.read_bytes()) for path in packages}
        need(len(hashes) == 1, "IDENTITY_MISMATCH", "isolated restored archives disagree")
        self.package = self.output.write("published-signed.nupkg", packages[0].read_bytes())
        self.record("isolated-restore", "PASS", "ISOLATED_ARTIFACT_RESTORED", "Exact candidate restored into fresh explicit tool/cache/configuration directories")

    def artifact(self):
        if not hasattr(self, "package"):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "actual signed package unavailable", "BLOCKED")
        raw = self.package.read_bytes()
        signature, diagnostics = self.runner.run("feed-signature-verification", ["dotnet", "nuget", "verify", str(self.package), "--all"], self.root,
                                                env=self.env, maximum=LIMITS["networkSeconds"])
        stderr = signature.get("stderr", {}).get("path")
        check_signature(signature, diagnostics + ((self.output.path / stderr).read_bytes() if stderr else b""))
        base = f"hexalith.builds.module.cli/{self.args.version}"
        registration = load_json(self.download("feed-registration.json", f"https://api.nuget.org/v3/registration5-semver1/{base}.json").read_bytes())
        catalog = load_json(self.download("feed-catalog.json", registration["catalogEntry"]).read_bytes())
        need(catalog.get("packageHashAlgorithm") == "SHA512" and catalog.get("packageHash") == base64.b64encode(hashlib.sha512(raw).digest()).decode() and
             catalog.get("packageSize") == len(raw), "IDENTITY_MISMATCH", "actual signed archive disagrees with feed SHA-512/size")
        feed_nuspec = self.download("feed.nuspec", f"https://api.nuget.org/v3-flatcontainer/{base}/hexalith.builds.module.cli.nuspec").read_bytes()
        payload = package_payload(self.package, self.deadline)
        self.output.json("published-payload-entries.json", payload)
        with zipfile.ZipFile(self.package) as archive:
            need(".signature.p7s" in archive.namelist(), "IDENTITY_MISMATCH", "actual package is unsigned")
            nuspecs = [name for name in archive.namelist() if name.endswith(".nuspec")]
            need(nuspecs == ["Hexalith.Builds.Module.Cli.nuspec"], "IDENTITY_MISMATCH", "package NuSpec identity/role ambiguous")
            nuspec = archive.read(nuspecs[0])
            need(nuspec.removeprefix(b"\xef\xbb\xbf") == feed_nuspec.removeprefix(b"\xef\xbb\xbf"), "IDENTITY_MISMATCH", "archive/feed NuSpec bytes differ beyond BOM")
            repository = next(node for node in ET.fromstring(nuspec).iter() if node.tag.endswith("repository")).attrib
            need(repository.get("commit") == self.args.release_sha and repository.get("url", "").removesuffix(".git").rstrip("/").lower() == "https://github.com/hexalith/hexalith.builds",
                 "IDENTITY_MISMATCH", "NuSpec repository/source identity disagrees")
            self.output.write("archive.nuspec", nuspec)
            prefix = "tools/net10.0/any/"
            tooling = archive.read(prefix + "Hexalith.Builds.Tooling.dll")
            installed = list((self.output.path / "isolated/tool").rglob("Hexalith.Builds.Tooling.dll"))
            need(bool(installed) and all(sha(path.read_bytes()) == sha(tooling) for path in installed), "IDENTITY_MISMATCH", "packaged/installed Tooling assemblies disagree")
            assembly = self.output.write("inspection/Hexalith.Builds.Tooling.dll", tooling)
            inspector = self.output.write("inspection/inspect.ps1", INSPECTOR.encode())
            embedded_dir = self.output.path / "inspection/embedded"
            embedded_dir.mkdir()
            embedded = load_json(self.runner.checked("embedded-resource-identity", ["pwsh", "-NoProfile", "-File", str(inspector),
                                    "-AssemblyPath", str(assembly), "-OutputDirectory", str(embedded_dir)], self.root))
            need(embedded.get("informationalVersion") == f"{self.args.version}+{self.args.release_sha}" and Path(embedded.get("assemblyLocation", "")) == assembly,
                 "IDENTITY_MISMATCH", "loaded/package Tooling informational/location identity disagrees")
            for index, name in enumerate(SCHEMAS):
                loose = archive.read(prefix + "schemas/" + name)
                source = self.git("source-schema-" + str(index), self.builds, "show", f"{self.args.release_sha}:schemas/{name}")
                matches = [row for row in embedded["resources"] if row["name"].endswith("." + name)]
                need(len(matches) == 1 and sha(loose) == matches[0]["sha256"] == sha(source), "IDENTITY_MISMATCH", "loose/embedded/source schema disagreement: " + name)
            catalog_name = "Hexalith.Builds.Tooling.Manifest.platform-version-catalog.json"
            embedded_catalog = (embedded_dir / catalog_name).read_bytes()
            self.identity.update(signedArchive=dict(sha256=sha(raw), sha512Base64=catalog["packageHash"], sizeBytes=len(raw)),
                                 nuspec=dict(repository=repository, archiveSha256=sha(nuspec), feedSha256=sha(feed_nuspec), archiveBom=nuspec.startswith(b"\xef\xbb\xbf"), feedBom=feed_nuspec.startswith(b"\xef\xbb\xbf")),
                                 toolingAssemblySha256=sha(tooling), embeddedCatalog=load_json(embedded_catalog), embeddedCatalogSha256=sha(embedded_catalog),
                                 installedAndLoadedAssemblyEqual=True)
        props = self.git("source-props", self.builds, "show", f"{self.args.release_sha}:Props/Directory.Packages.props")
        filtered = self.git("source-props-filtered", self.builds, "cat-file", "--filters", f"{self.args.release_sha}:Props/Directory.Packages.props")
        self.identity["sourceProps"] = dict(rawSha256=sha(props), filteredSha256=sha(filtered), bytesEqual=props == filtered)
        self.identity["sourceProps"]["catalogSelectionsEqual"] = compare_catalog(props, self.identity["embeddedCatalog"])
        self.artifact_validated = True
        self.record("published-artifact", "PASS", "PUBLISHED_ARTIFACT_IDENTITY_MEASURED", "Feed signature/hash/size, NuSpecs, packaged/loaded assembly and schemas independently agree")

    def candidate_version(self):
        self.require_candidate_prerequisites(version=False)
        observed = self.runner.checked("feed-tool-version", [str(self.tool), "--version"], self.root, env=self.env).decode().strip()
        need(observed == f"{self.args.version}+{self.args.release_sha}", "IDENTITY_MISMATCH", "installed CLI informational identity disagrees")
        self.version_text, self.candidate_verified = observed, True
        self.identity["toolVersion"] = observed
        self.record("candidate-version", "PASS", "CANDIDATE_VERSION_MEASURED", "Verified candidate version executed after all required independent artifact/CI gates")

    def qualification_carriers(self, available):
        """Inspect exact-source CI inventories; missing declared bytes stay missing."""
        declared = {row["file"] for row in self.inventory.get("qualificationEvidence", [])}
        carriers = []
        for run in getattr(self, "producer_runs", []):
            data = self.api(f"producer-artifacts-{run['id']}", f"repos/{REPOSITORY}/actions/runs/{run['id']}/artifacts?per_page=100")
            need(data.get("total_count") == len(data.get("artifacts", [])), "CONTROLS_INCOMPLETE", "incomplete CI artifact inventory")
            for artifact in data["artifacts"]:
                item = dict(runId=run["id"], source=run["head_sha"], artifact=artifact, files=[])
                carriers.append(item)
                if artifact.get("expired") or artifact.get("size_in_bytes", LIMITS["archiveBytes"] + 1) > LIMITS["archiveBytes"]:
                    item.update(state="BLOCKED", code="ARTIFACT_UNAVAILABLE")
                    continue
                label = f"producer-artifact-{artifact['id']}"
                native, raw = self.runner.run(label, ["gh", "api", f"repos/{REPOSITORY}/actions/artifacts/{artifact['id']}/zip"], self.root,
                                              maximum=LIMITS["networkSeconds"])
                if native["termination"] or native["exitCode"] != 0:
                    item.update(state="BLOCKED", code=native["termination"] or "ARTIFACT_UNAVAILABLE")
                    continue
                need(len(raw) <= LIMITS["archiveBytes"] and zipfile.is_zipfile(io.BytesIO(raw)), "IDENTITY_MISMATCH", "CI carrier is not a bounded archive")
                self.output.write(label + ".zip", raw)
                digest = artifact.get("digest")
                need(digest is None or digest == "sha256:" + sha(raw), "IDENTITY_MISMATCH", "CI archive digest mismatch")
                with zipfile.ZipFile(io.BytesIO(raw)) as archive:
                    entries = archive.infolist()
                    need(len(entries) <= LIMITS["watchedEntries"] and sum(row.file_size for row in entries) <= LIMITS["expandedBytes"], "ARCHIVE_LIMIT", "CI carrier expansion limit")
                    need(len({row.filename for row in entries}) == len(entries), "IDENTITY_MISMATCH", "duplicate CI carrier paths")
                    for entry in entries:
                        relative_path(entry.filename.rstrip("/"))
                        if entry.filename not in declared:
                            continue
                        need(entry.file_size <= LIMITS["streamBytes"], "CAPTURE_LIMIT", "declared evidence exceeds stream bound")
                        content = archive.read(entry)
                        need(entry.filename not in available or available[entry.filename] == content, "IDENTITY_MISMATCH", "conflicting declared evidence carriers")
                        available[entry.filename] = content
                        item["files"].append(dict(file=entry.filename, sha256=sha(content), sizeBytes=len(content)))
                item.update(state="PASS", archiveSha256=sha(raw), sizeBytes=len(raw))
        self.output.json("producer-qualification-carriers.json", carriers)

    def producer_native(self):
        if not getattr(self, "release_run", None):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "producer Release identity unavailable", "BLOCKED")
        data = self.runner.checked("producer-release-log-archive", ["gh", "api", f"repos/{REPOSITORY}/actions/runs/{self.release_run['id']}/logs"], self.root, maximum=LIMITS["networkSeconds"])
        if not data:
            raise QualificationError("PRODUCER_NATIVE_EVIDENCE_UNAVAILABLE", "producer returned no retained native log/package payload evidence", "BLOCKED")
        self.output.write("producer-release-logs.zip", data)
        need(zipfile.is_zipfile(io.BytesIO(data)), "IDENTITY_MISMATCH", "producer log response is not an archive")
        logs, available_evidence = [], {}
        self.available_qualification_evidence = available_evidence
        declared = self.inventory.get("qualificationEvidence", [])
        declared_names = {row["file"] for row in declared}
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            need(sum(entry.file_size for entry in archive.infolist()) <= LIMITS["expandedBytes"], "ARCHIVE_LIMIT", "producer logs exceed expansion limit")
            for entry in archive.infolist():
                need(entry.file_size <= LIMITS["streamBytes"], "CAPTURE_LIMIT", "individual native log exceeds stream limit")
                raw = archive.read(entry)
                if entry.filename in declared_names:
                    available_evidence[entry.filename] = raw
                logs.append(raw.decode(errors="replace"))
        text = "\n".join(logs)
        expected = self.package_inventory["nupkg"]["sha256"]
        signed = self.identity.get("signedArchive", {}).get("sha256", "")
        match = re.search(r"Verified remote 'Hexalith.Builds.Module.Cli' '" + re.escape(self.args.version) + r"' SHA-256 ([A-Fa-f0-9]{64}) \(qualified local SHA-256 ([A-Fa-f0-9]{64})\)", text)
        if not match:
            raise QualificationError("PRODUCER_NATIVE_EVIDENCE_UNAVAILABLE", "matched signed/qualified payload publisher evidence unavailable", "BLOCKED")
        need(match[1].lower() == signed and match[2].lower() == expected.lower(), "IDENTITY_MISMATCH", "publisher signed/unsigned archive identity disagrees")
        self.record("producer-payload-binding", "PASS", "PRODUCER_SIGNED_PAYLOAD_BOUND", "Exact publisher signed/unsigned package identities agree; this is separate from native test evidence")
        normalized = re.sub(r"\x1b\[[0-9;]*m", "", text)
        summaries = []
        for match in re.finditer(r"Running tests from ([^\r\n]+).*?Test run summary: ([^\r\n]+).*?total: (\d+).*?failed: (\d+).*?succeeded: (\d+).*?skipped: (\d+)", normalized, re.DOTALL):
            summaries.append(dict(assembly=match[1], outcome=match[2], total=int(match[3]), failed=int(match[4]), passed=int(match[5]), skipped=int(match[6])))
        skip_lines = [line for line in normalized.splitlines() if "skipped " in line or "A skipped live test is not passing evidence" in line or "Native process inspection is explicitly gated" in line]
        self.output.json("producer-native-summary.json", dict(source=self.args.release_sha, releaseRun=self.release_run["id"],
                         nativeSummaries=summaries, rawSkipLines=skip_lines,
                         coverage="Producer Linux Release lanes; each native skip stays a skip and earns no live or Windows/macOS qualification credit."))
        native_state = "BLOCKED" if not summaries else "FAIL" if any(row["failed"] or row["outcome"] != "Passed!" or row["total"] <= 0 or row["total"] != row["failed"] + row["passed"] + row["skipped"] for row in summaries) else "PASS"
        self.record("producer-native", native_state, "PRODUCER_NATIVE_EVIDENCE_UNAVAILABLE" if not summaries else "PRODUCER_NATIVE_RESULTS_MEASURED",
                    "Nonempty native test summaries required independently of publisher payload binding; skips retain their actual state")
    def qualification_evidence(self):
        available_evidence = getattr(self, "available_qualification_evidence", {})
        declared = getattr(self, "inventory", {}).get("qualificationEvidence", [])
        carrier_error = None
        try:
            if hasattr(self, "inventory"):
                self.qualification_carriers(available_evidence)
        except QualificationError as error:
            carrier_error = error
        evidence = []
        for row in declared:
            raw = available_evidence.get(row["file"])
            state = "BLOCKED" if raw is None else "PASS" if sha(raw) == row["sha256"].lower() and len(raw) == row["sizeBytes"] else "FAIL"
            if raw is not None:
                self.output.write("producer-qualification-evidence/" + row["file"], raw)
            evidence.append(dict(**row, state=state, observedSha256=sha(raw) if raw is not None else None,
                                 observedSizeBytes=len(raw) if raw is not None else None))
        self.output.json("producer-qualification-evidence.json", evidence)
        evidence_state = "FAIL" if any(row["state"] == "FAIL" for row in evidence) else "PASS" if evidence and all(row["state"] == "PASS" for row in evidence) else "BLOCKED"
        self.record("producer-qualification-evidence", evidence_state,
                    "QUALIFICATION_EVIDENCE_MEASURED" if evidence_state == "PASS" else "QUALIFICATION_EVIDENCE_MISMATCH" if evidence_state == "FAIL" else "QUALIFICATION_EVIDENCE_UNAVAILABLE",
                    "Every declared qualificationEvidence file/hash/size measured independently; missing bytes remain BLOCKED")
        if carrier_error is not None:
            raise carrier_error

    def unsigned_archive(self):
        if not getattr(self, "artifact_validated", False) or not hasattr(self, "package_inventory"):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "independently identified signed artifact/CI inventory unavailable", "BLOCKED")
        version = self.runner.checked("unsigned-helper-sdk-version", ["dotnet", "--version"], self.root).decode().strip()
        sdks = self.runner.checked("unsigned-helper-sdk-list", ["dotnet", "--list-sdks"], self.root).decode().splitlines()
        matches = [re.fullmatch(re.escape(version) + r" \[(.+)\]", line) for line in sdks]
        roots = [Path(match[1]) / version for match in matches if match]
        need(len(roots) == 1, "IDENTITY_MISMATCH", "selected SDK identity is ambiguous")
        sdk = roots[0]
        references = []
        sdk_inputs = []
        for name in ("NuGet.Packaging", "NuGet.Common", "NuGet.Frameworks", "NuGet.Versioning", "NuGet.Configuration"):
            assembly = sdk / (name + ".dll")
            sdk_inputs.append(dict(path=str(assembly), sha256=sha(assembly.read_bytes())))
            node = ET.Element("Reference", Include=name)
            ET.SubElement(node, "HintPath").text = str(assembly)
            references.append(ET.tostring(node, encoding="unicode"))
        project = ('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>' + "".join(references) + '</ItemGroup></Project>\n').encode()
        # The metadata-only helper lives outside the consumer build hierarchy;
        # it must not inherit or weaken the product's enrollment build gates.
        with tempfile.TemporaryDirectory(prefix="epic16-unsigned-helper-") as temporary:
            directory = Path(temporary)
            source = UNSIGN_SOURCE.replace("\n", "\r\n").encode()
            sdk_pin = (json.dumps(dict(sdk=dict(version=version, rollForward="disable")), indent=2) + "\n").encode()
            for name, data in (("UnsignedProof.csproj", project), ("Program.cs", source), ("global.json", sdk_pin)):
                (directory / name).write_bytes(data)
                self.output.write("unsigned-helper/" + name + ".bytes", data)
            self.runner.checked("unsigned-helper-debug-build", ["dotnet", "build", "UnsignedProof.csproj", "--configuration", "Debug", "--nologo", "-m:1", "-p:NuGetAudit=false"], directory,
                                env=self.env, maximum=LIMITS["diagnosticSeconds"])
            helper = directory / "bin/Debug/net10.0/UnsignedProof.dll"
            self.output.write("unsigned-helper/UnsignedProof.dll.bytes", helper.read_bytes())
            before = sha(self.package.read_bytes())
            output = self.output.target("qualified-unsigned.nupkg")
            self.runner.checked("unsigned-archive-reconstruction", ["dotnet", str(helper), str(self.package), str(output)], directory,
                                env=self.env, maximum=LIMITS["diagnosticSeconds"], watch_root=directory)
            signed_rows = load_json((self.output.path / "published-payload-entries.json").read_bytes())
            result = compare_unsigned(output, self.package_inventory["nupkg"], signed_rows, self.deadline)
            need(sha(self.package.read_bytes()) == before, "INPUT_RESTORATION_MISMATCH", "signature removal changed the signed input")
            self.identity["qualifiedUnsignedArchive"] = result
            self.output.json("unsigned-reconstruction-proof.json", dict(**result, signedInputSha256=before, signedInputUnchanged=True,
                             helperSha256=sha(helper.read_bytes()), helperSourceSha256=sha(UNSIGN_SOURCE.replace("\n", "\r\n").encode()),
                             sdkVersion=version, sdkAssemblies=sdk_inputs,
                             mechanism="NuGet.Packaging.Signing.SignedPackageArchive.RemoveSignatureAsync; exact full CI hash/size and every uncompressed payload entry independently compared"))
            self.record("qualified-unsigned-archive", "PASS", "CI_ARCHIVE_AND_PAYLOAD_EQUAL", "Unsigned reconstruction exactly equals CI archive hash/size and signed archive uncompressed payload")

    def package_preparation(self):
        self.require_candidate_prerequisites()
        if not getattr(self, "artifact_validated", False):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "identified published tool unavailable for package preparation", "BLOCKED")
        workspace = self.output.path / "package-preparation"
        result, data = self.runner.run("package-preparation", [str(self.tool), "prepare-build", "--manifest", MANIFEST,
                                      "--mode", "package", "--workspace", str(workspace), "--output", "json"], self.root,
                                      env=self.env, maximum=LIMITS["validationSeconds"])
        if result["termination"] or result["exitCode"] != 0:
            self.output.json("package-preparation-action.json", dict(nativeExitCode=result["exitCode"], termination=result["termination"],
                             result="BLOCKED", reason="Exact published package-mode preparation did not produce a supported complete Release mapping; no source-mode substitution.",
                             actionExports="BLOCKED: no successful published package preparation to export"))
            raise QualificationError("PACKAGE_PREPARATION_UNAVAILABLE", f"exact published package preparation native exit {result['exitCode']}; action export remains unavailable", "BLOCKED")
        source = self.git("package-action-exact-source", self.builds, "show", f"{self.args.release_sha}:Github/prepare-module-build/prepare_module_build.py")
        action = self.output.write("action/prepare_module_build.py", source)
        bridge = "import importlib.util,json,sys,pathlib; spec=importlib.util.spec_from_file_location('producer_action',sys.argv[1]); module=importlib.util.module_from_spec(spec); spec.loader.exec_module(module); result=json.loads(pathlib.Path(sys.argv[2]).read_bytes()); env=module.verified_environment(result,pathlib.Path(sys.argv[3])); print(json.dumps(env,sort_keys=True))"
        verified = self.runner.checked("package-action-verified-export", [sys.executable, "-c", bridge, str(action), str(self.output.path / "package-preparation.stdout.txt"), str(workspace)], self.root)
        environment = load_json(verified)
        need(environment.get("Configuration") == "Release", "IDENTITY_MISMATCH", "package action failed to select Release")
        self.output.json("package-preparation-action.json", dict(result="PASS", nativeExitCode=0, environment=environment,
                         actionSourceSha256=sha(source), scope="Exact release-source action environment verifier; no consumer Release compilation or runtime launch"))
        self.record("package-preparation-action", "PASS", "PACKAGE_ACTION_EXPORT_MEASURED", "Published package preparation and exact-source action export verified complete Release mapping")

    def review_findings(self):
        source = self.reader.read(REVIEW_LEDGER)
        retained = load_json(source)
        findings = retained["findings"]
        need(len(findings) == 13 and {row["id"] for row in findings} == {"B" + str(i) for i in range(1, 12)} | {"E1", "G1"} and
             len(retained["groups"]) == 10 and retained["rejected"] == ["B8", "B11"],
             "REVIEW_INVENTORY_DRIFT", "all thirteen findings, ten groups and both rejections must survive")
        bindings = []
        for finding in findings:
            current = copy.deepcopy(finding)
            current["candidateEvidence"] = []
            for index, evidence in enumerate(finding.get("currentEvidence", [])):
                data = self.git(f"review-source-{finding['id']}-{index}", self.builds, "show", f"{self.args.release_sha}:{evidence['path']}")
                current["candidateEvidence"].append(dict(path=evidence["path"], sourceRevision=self.args.release_sha,
                            sha256=sha(data), sizeBytes=len(data), matchesPreviousInspectedSource=sha(data) == evidence["sha256"],
                            scope="Actual published artifact source; producer native lane does not individually close this retained finding"))
            current["ownerScope"] = "Hexalith.Builds runtime maintainer; native host/policy owner where applicable"
            current["acceptanceState"] = "rejected disposition retained" if finding["id"] in retained["rejected"] else "pending exact finding/host owner acceptance"
            bindings.append(current)
            self.record("producer-review-" + finding["id"], finding["state"], "REJECTED_FINDING_PRESERVED" if finding["id"] in retained["rejected"] else "OWNER_EVIDENCE_PENDING",
                        finding.get("qualificationBoundary", finding.get("scope", "Retained disposition")))
        self.output.json("producer-review-dispositions.json", dict(candidateSource=self.args.release_sha, predecessorLedger=REVIEW_LEDGER,
                         predecessorLedgerSha256=sha(source), findings=bindings, groups=retained["groups"], rejected=retained["rejected"],
                         qualificationCredit="No blanket closure from preparation, broad CI success or Linux execution; exact finding and native-host gates remain separate"))

    def controls(self):
        self.require_candidate_prerequisites()
        if not getattr(self, "artifact_validated", False):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "independently identified signed published tool unavailable", "BLOCKED")
        original = self.reader.read(MANIFEST)
        data = load_json(original)
        fields = configuration_fields(data)
        for format_name in ("human", "json"):
            args = [str(self.tool), "validate", "--manifest", MANIFEST]
            if format_name == "json":
                args += ["--output", "json"]
            result, stdout = self.runner.run("ordinary-" + format_name, args, self.root, env=self.env)
            if result["termination"]:
                raise QualificationError(result["termination"], "positive control incomplete", "BLOCKED")
            need(result["exitCode"] == 0, "CONTROL_MISMATCH", "ordinary declaration positive control rejected")
            if format_name == "json":
                report = load_json(stdout)
                need(report.get("status") == "validated" and report.get("diagnostics") == [], "CONTROL_MISMATCH", "positive JSON control not clean")
            else:
                need("validated" in stdout.decode().lower(), "CONTROL_MISMATCH", "positive human validation missing")
        rows = []
        try:
            for fi, (path, field) in enumerate(fields):
                for vi, value in enumerate(INVALID_KEYS):
                    raw = (json.dumps(mutate(data, path, value), indent=2) + "\n").encode()
                    source = self.output.write(f"malformed-inputs/{fi}-{vi}.json", raw)
                    for format_name in ("human", "json"):
                        result, stdout = self.runner.run(f"negative-{fi}-{vi}-{format_name}", [str(self.tool), "validate", "--manifest", str(source), "--output", format_name], self.root, env=self.env)
                        check_negative(result, stdout, field, source, self.root, format_name,
                                       (self.output.path / result["stderr"]["path"]).read_bytes())
                        need(source.read_bytes() == raw, "INPUT_RESTORATION_MISMATCH", "validation changed malformed input")
                        rows.append(dict(field=field, value=value, format=format_name, nativeExitCode=result["exitCode"], ruleId="HXP002", inputSha256=sha(raw), passed=True))
        finally:
            self.output.json("malformed-controls.json", rows)
        need(self.reader.read(MANIFEST) == original, "INPUT_RESTORATION_MISMATCH", "real manifest changed")
        check_control_inventory(rows, [field for _, field in fields])
        self.record("malformed-key-controls", "PASS", "ALL_36_CONTROLS_MEASURED", "Both real-manifest positives and all exact-field malformed-key controls passed with unchanged inputs")

    def adoption(self):
        self.require_candidate_prerequisites()
        if not getattr(self, "artifact_validated", False):
            raise QualificationError("ARTIFACT_UNAVAILABLE", "independently identified signed published tool unavailable", "BLOCKED")
        reports, exits = {}, {}
        for format_name in ("human", "json"):
            args = [str(self.tool), "validate", "--manifest", MANIFEST, "--adoption"]
            if format_name == "json":
                args += ["--output", "json"]
            result, reports[format_name] = self.runner.run("adoption-" + format_name, args, self.root, env=self.env)
            if result["termination"]:
                raise QualificationError(result["termination"], "adoption native output incomplete", "BLOCKED")
            exits[format_name] = result["exitCode"]
        current = load_json(reports["json"])["adoption"]
        predecessor = getattr(self, "adoption_predecessor", PREDECESSOR)
        previous = load_json(self.reader.read(predecessor))["adoption"]
        rows = [disposition(row) for row in current if not row["passed"]]
        self.output.json("adoption-inventories.json", dict(predecessor=predecessor, predecessorRows=previous, currentRows=current,
                         predecessorDispositions=[disposition(row) for row in previous], currentDispositions=[disposition(row) for row in current],
                         nativeExits=exits, failures=rows))
        need(human_adoption(reports["human"]) == current, "ADOPTION_INVENTORY_DRIFT", "human/JSON ordered rows disagree")
        delta = adoption_delta(previous, current)
        self.output.json("adoption-comparison.json", delta)
        self.output.json("adoption-dispositions.json", dict(nativeExits=exits, predecessorCounts=dict(total=len(previous), failed=sum(not row["passed"] for row in previous)),
                         currentCounts=dict(total=len(current), failed=len(rows)), delta=delta, failures=rows))
        expected_exit = 1 if rows else 0
        need(all(value == expected_exit for value in exits.values()), "ADOPTION_INVENTORY_DRIFT", "native adoption exits disagree with actual result rows")
        if not delta["orderedEqual"]:
            self.record("adoption-delta", "FAIL", "ADOPTION_INVENTORY_DRIFT", "Exact current additions/removals/reason changes retained; owner disposition required")
        else:
            self.record("adoption-delta", "PASS", "ADOPTION_INVENTORY_MEASURED", "Fresh inventory exactly matches every ordered predecessor row")
        self.record("adoption-acceptance", "FAIL" if rows else "PASS", "ADOPTION_FAILURES_RETAINED" if rows else "ADOPTION_ACCEPTED_STATIC",
                    f"{len(current) - len(rows)} passing / {len(rows)} failing rows; every failure remains unaccepted", nativeExits=exits)

    def platform_pair(self):
        pin = load_json(self.platform_reader.read(".config/dotnet-tools.json"))["tools"]["hexalith.builds.module.cli"]
        platform_config = load_json(self.platform_reader.read("hexalith-platform.json"))
        revision = self.git("platform-head", self.platform, "rev-parse", "HEAD").decode().strip()
        tags = self.git("platform-tags", self.platform, "tag", "--points-at", revision).decode().splitlines()
        builds_link = self.git("platform-builds-gitlink", self.platform, "ls-tree", "HEAD", "references/Hexalith.Builds").decode().strip()
        prepared = prepared_platform_identity(self.platform_reader.read("eng/prepare_builds_artifacts.py"))
        # Retain prose as context only. Neither a prepared helper nor README is an
        # authenticated owner decision, and historical acceptance is not inherited.
        readme = self.platform_reader.read("README.md")
        range_match = re.fullmatch(r"\[(\d+\.\d+\.\d+), (\d+\.\d+\.\d+)-0\)", platform_config.get("toolVersionRange", ""))
        stable = re.fullmatch(r"\d+\.\d+\.\d+", self.args.version)
        eligible = None
        if range_match and stable:
            numeric = lambda value: tuple(int(part) for part in value.split("."))
            eligible = numeric(range_match[1]) <= numeric(self.args.version) < numeric(range_match[2])
        owner_runs = None
        try:
            owner_runs = self.api("platform-owner-runs", f"repos/Hexalith/Hexalith.Platform/actions/runs?head_sha={revision}&per_page=100")
            self.record("platform-owner-results", "BLOCKED", "OWNER_ACCEPTANCE_PENDING", "Available exact-candidate owner run inventory retained; workflow success cannot grant authenticated acceptance")
        except QualificationError as error:
            self.record("platform-owner-results", error.state, error.code, str(error))
        catalog_matches = platform_catalog_binding(builds_link, prepared["SOURCE"])
        prepared_matches = pin.get("rollForward") is False and pin.get("version") == prepared["VERSION"] == self.args.version and prepared["SOURCE"] == self.args.release_sha
        measured = self.identity.get("signedArchive", {})
        tooling = self.identity.get("toolingAssemblySha256")
        # When actual archive/assembly identity is available, the prepared tuple
        # must agree with it as well as the requested exact version/source.
        artifact_matches = (not measured or measured.get("sha256") == prepared["ARCHIVE_SHA256"]) and (not tooling or tooling == prepared["TOOLING_SHA256"])
        self.output.json("platform-pair.json", dict(revision=revision, tags=tags, pin=pin, range=platform_config,
                         recordedBuildsGitlink=builds_link, preparedIdentity=prepared, readmeSha256=sha(readme), ownerRunInventory=owner_runs,
                         candidateRangeEligible=eligible, candidateVersion=self.args.version, candidateSource=self.args.release_sha,
                         candidateCatalog=self.identity.get("embeddedCatalog"), registrationAcceptance="pending",
                         acceptance=dict(state="BLOCKED", code="OWNER_EVIDENCE_PENDING", evidence=None,
                                         reason="Authenticated exact-candidate decision and generated/direct-host qualification unavailable")))
        self.record("platform-prepared-identity", "PASS" if prepared_matches and artifact_matches else "FAIL",
                    "PLATFORM_PREPARED_IDENTITY_MEASURED" if prepared_matches and artifact_matches else "PLATFORM_PREPARED_IDENTITY_MISMATCH",
                    "Prepared version/source/hash declarations compared independently; no acceptance inferred")
        self.record("platform-recorded-catalog", "PASS" if catalog_matches else "FAIL",
                    "PLATFORM_CATALOG_BINDING_MEASURED" if catalog_matches else "PLATFORM_CATALOG_BINDING_MISMATCH",
                    "Raw recorded Builds gitlink compared with prepared source; no repair or acceptance inferred")
        self.record("platform-range", "BLOCKED" if eligible is None else "PASS" if eligible else "FAIL",
                    "PLATFORM_IDENTITY_UNAVAILABLE" if eligible is None else "PLATFORM_RANGE_MEASURED" if eligible else "PLATFORM_INCOMPATIBLE",
                    "Range eligibility measured separately from recorded identity and acceptance")
        release_tagged = any(re.fullmatch(r"v\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", tag) for tag in tags)
        self.record("platform-release-identity", "PASS" if release_tagged else "BLOCKED",
                    "PLATFORM_TAG_MEASURED" if release_tagged else "PLATFORM_IDENTITY_UNAVAILABLE", "Exact Platform release identity; no tags fetched or fabricated")
        self.record("platform-pair", "BLOCKED", "OWNER_EVIDENCE_PENDING", "Prepared tuple, README and any preflight success confer no authenticated candidate acceptance")

    def pending_gates(self):
        for gate in ("provider-source-package-deployed-parity", "ad5-envelope", "recovery-operator-capacity", "published-testing-helper-strategy", "native-windows-macos", "human-sm2", "registration-source-config-auth-gateway-dapr"):
            self.record(gate, "BLOCKED", "OWNER_EVIDENCE_PENDING", "Exact candidate-bound owner/native evidence unavailable; no source-only or synthetic qualification credit")

    def restoration(self):
        for path, expected in self.originals.items():
            need(sha(self.reader.read(path)) == expected, "INPUT_RESTORATION_MISMATCH", "protected input changed: " + path)
        if hasattr(self, "entry_index"):
            need(sha(self.git("final-index", self.root, "ls-files", "--stage", "-z", "--", "references/")) == self.entry_index,
                 "INPUT_RESTORATION_MISMATCH", "index rows changed")
        if hasattr(self, "entry_index_bytes"):
            need(self.index_digest("final-index-digest") == self.entry_index_bytes,
                 "INPUT_RESTORATION_MISMATCH", "complete index bytes changed")
        for relative, expected, nested in getattr(self, "dependency_identities", []):
            repository = self.root / relative
            name = repository.name
            head = self.git("final-head-" + name, repository, "rev-parse", "HEAD").decode().strip()
            dirty = self.git("final-status-" + name, repository, "status", "--porcelain=v1", "--untracked-files=normal")
            current_nested = self.git("final-nested-" + name, repository, "submodule", "status").decode().splitlines()
            need(head == expected and not dirty and current_nested == nested, "INPUT_RESTORATION_MISMATCH", "dependency tuple or nested initialization changed: " + relative)
        self.record("restoration", "PASS", "INPUTS_UNCHANGED", "Protected inputs and exact raw index rows are unchanged")

    def collect(self) -> tuple[dict, int]:
        try:
            for name, action in (("entry", self.entry), ("producer-provenance", self.provenance),
                                 ("producer-fixtures", self.fixture_inventory), ("isolated-restore", self.restore),
                                 ("published-artifact", self.artifact), ("producer-native", self.producer_native),
                                 ("producer-qualification-carriers", self.qualification_evidence),
                                 ("qualified-unsigned-archive", self.unsigned_archive), ("candidate-version", self.candidate_version),
                                 ("malformed-key-controls", self.controls),
                                 ("adoption", self.adoption), ("package-preparation-action", self.package_preparation),
                                 ("producer-review", self.review_findings), ("platform-pair", self.platform_pair)):
                self.gate(name, action)
                # Do not execute an unverified, substituted artifact or proceed from
                # invalid source/pin/dependency identities.
                if name == "entry" and not any(row["gate"] == "entry" and row["state"] == "PASS" for row in self.ledger):
                    break
            self.pending_gates()
        except KeyboardInterrupt:
            self.record("collector", "BLOCKED", "COLLECTION_CANCELLED", "caller cancelled; owned process group terminated")
        except Exception as error:
            self.record("collector", "FAIL", "COLLECTOR_INTERNAL_ERROR", f"{type(error).__name__}: {error}")
        finally:
            try:
                if getattr(self, "originals", {}) or hasattr(self, "entry_index"):
                    self.gate("restoration", self.restoration)
            except KeyboardInterrupt:
                self.record("restoration", "BLOCKED", "INPUT_VERIFICATION_CANCELLED", "Captured-input verification was cancelled")
            except Exception as error:
                self.record("restoration", "FAIL", "COLLECTOR_INTERNAL_ERROR", f"Captured-input verification: {type(error).__name__}: {error}")
            finally:
                self.readers.close()
        state, exit_code = verdict(self.ledger)
        result = dict(schemaVersion="hexalith.conversations.epic16-artifact-qualification.v1", state=state,
                      collectorExitCode=exit_code, candidateVersion=self.args.version, candidateSource=self.args.release_sha,
                      limits=LIMITS, identity=self.identity, ledger=self.ledger,
                      authority=dict(hold="ACTIVE", entry=None, trustedEntries=[], runtimeEntryAllowed=False,
                                     deploymentPerformed=False, platformAcceptanceClaimed=False, pinChanges=False,
                                     implementationHold="ACTIVE", executionAllowed=False, ownerApprovalClaimed=False,
                                     releaseAuthorized=False, pushAuthorized=False))
        self.output.json("commands.json", self.runner.commands)
        self.output.json("identity.json", self.identity)
        self.output.json("qualification-ledger.json", result)
        return result, exit_code


def verdict(rows: list[dict]) -> tuple[str, int]:
    """A defect wins over unavailability; zero evaluated gates can never pass."""
    need(bool(rows), "EMPTY_LEDGER", "qualification ledger cannot be empty")
    if any(row["state"] == "FAIL" for row in rows):
        return "FAIL", 1
    if any(row["state"] == "BLOCKED" for row in rows):
        return "BLOCKED", 2
    need(all(row["state"] in ("PASS", "not-applicable") for row in rows) and any(row["state"] == "PASS" for row in rows),
         "EMPTY_LEDGER", "no evaluated qualification gate")
    return "PASS", 0


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("builds", "platform", "conversations", "inventory", "output"):
        parser.add_argument("--" + name, type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--release-sha", required=True)
    parser.add_argument("--candidate-only", action="store_true")
    parser.add_argument("--planning-inputs", type=Path, default=Path(PLANNING_INPUTS))
    parser.add_argument("--adoption-predecessor", type=Path, default=Path(PREDECESSOR))
    parser.add_argument("--tooling-contract", type=Path, default=Path(TOOLING_CONTRACT))
    args = parser.parse_args(argv)
    try:
        result, exit_code = Collector(args).collect()
        print(json.dumps(dict(state=result["state"], collectorExitCode=exit_code, gates=len(result["ledger"]), output=str(args.output))))
        return exit_code
    except (QualificationError, ReadError, OSError, OutputError) as error:
        state = getattr(error, "state", "BLOCKED" if getattr(error, "unavailable", False) else "FAIL")
        row = dict(gate="preflight", state=state, code=getattr(error, "code", "OUTPUT_FORBIDDEN"), detail=str(error))
        print(json.dumps(dict(state=state, ledger=[row], code=row["code"], detail=str(error))), file=sys.stderr)
        return 2 if state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
