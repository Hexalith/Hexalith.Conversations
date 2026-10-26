"""Fixed native oracle; evidence assemblies are inspected, never loaded/executed."""
from __future__ import annotations

import base64
from functools import lru_cache
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess
import tempfile

from contained_reader import ContainedReader

HELPER_ROOT = '_bmad/scripts/epic16_native_helper'
POLICY_PATH = 'global.json'
HELPER_FILES = tuple(HELPER_ROOT + '/' + name for name in (
    'Epic16.NativeHelper.csproj', 'Program.cs', 'TestConstruction.cs', 'ProductMetadata.cs', 'SignatureNames.cs', 'ReadModelBatchCanonicalJson.cs',
    'README.md', 'EventStore-LICENSE'))
CANONICALIZER_PIN = '1f9430e33cefaa51757c36f27d57ed509a31d08aa5f35b05a4159b7d806362f9'


class HelperUnavailable(Exception):
    """The fixed .NET helper cannot build or run in this environment."""


def _process_failure(stage, result):
    """Retain separate bounded diagnostic tails and the actual prerequisite exit."""
    def bounded(value):
        if isinstance(value, bytes): value = value.decode('utf-8', errors='replace')
        return (value or '')[-2000:]
    return f"{stage}: exit={result.returncode}; stdout={bounded(result.stdout)!r}; stderr={bounded(result.stderr)!r}"


def validate_sdk(policy_bytes, selected=None):
    """Independently enforce this repository's explicit latestPatch SDK policy."""
    policy = strict_json(policy_bytes)
    if not isinstance(policy, dict):
        raise ValueError('repository SDK policy must be an object')
    sdk = policy.get('sdk', {})
    if not isinstance(sdk, dict) or set(sdk) != {'version', 'rollForward'} or sdk['rollForward'] != 'latestPatch':
        raise ValueError('unsupported repository SDK policy')
    def version(value):
        if not isinstance(value, str) or re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', value) is None:
            raise ValueError('invalid SDK version')
        return tuple(map(int, value.split('.')))
    wanted = version(sdk['version'])
    if selected is None:
        return
    try:
        actual = version(selected)
    except ValueError as error:
        raise HelperUnavailable('selected SDK identity is unavailable or incompatible') from error
    if actual[:2] != wanted[:2] or actual[2] // 100 != wanted[2] // 100 or actual[2] < wanted[2]:
        raise HelperUnavailable('selected SDK does not satisfy repository latestPatch policy')


def strict_json(content: bytes):
    """Retain shared duplicate-key rules and reject exponent overflow to infinity."""
    from generate_story_record import v2_parse_json
    value = v2_parse_json(content)
    def finite(item):
        if isinstance(item, float) and not math.isfinite(item):
            raise ValueError('non-finite JSON number')
        if isinstance(item, dict):
            for child in item.values(): finite(child)
        elif isinstance(item, list):
            for child in item: finite(child)
    finite(value)
    return value


@lru_cache(maxsize=1)
def _helper():
    root = Path(__file__).absolute().parents[2]
    with ContainedReader(root) as reader:
        sources = {Path(path).name: reader.read(path) for path in HELPER_FILES}
        sources[POLICY_PATH] = reader.read(POLICY_PATH)
    # Malformed policy is invalid evidence, before the SDK resolver can turn it
    # into an apparent unavailable installation.
    validate_sdk(sources[POLICY_PATH])
    if hashlib.sha256(sources['ReadModelBatchCanonicalJson.cs']).hexdigest() != CANONICALIZER_PIN:
        raise ValueError('retained EventStore canonicalizer drift')
    # Setup failures are unavailable prerequisites, including before dotnet starts.
    workspace = None
    try:
        workspace = tempfile.TemporaryDirectory(prefix='epic16-native-oracle-')
        directory = Path(workspace.name)
        for name, data in sources.items():
            (directory / name).write_bytes(data)
        sdk = subprocess.run(['dotnet', '--version'], cwd=directory, capture_output=True, timeout=30)
        if sdk.returncode:
            raise HelperUnavailable(_process_failure('repository-approved SDK unavailable', sdk))
        selected = sdk.stdout.decode('utf-8').strip()
        validate_sdk(sources[POLICY_PATH], selected)
        (directory / 'execution-toolchain.json').write_text(json.dumps(dict(sdk=selected,
            policySha256=hashlib.sha256(sources[POLICY_PATH]).hexdigest())))
        build = subprocess.run(['dotnet', 'build', 'Epic16.NativeHelper.csproj', '-c', 'Debug',
            '--nologo', '-m:1', '-p:NuGetAudit=false'], cwd=directory, capture_output=True, timeout=120)
        if build.returncode:
            raise HelperUnavailable(_process_failure('fixed helper build failed', build))
    except (OSError, subprocess.TimeoutExpired, HelperUnavailable) as error:
        if workspace is not None:
            try:
                workspace.cleanup()
            except OSError:
                pass
        if isinstance(error, HelperUnavailable):
            raise
        raise HelperUnavailable('fixed helper setup/build unavailable') from error
    return workspace, directory / 'bin/Debug/net10.0/Epic16.NativeHelper.dll'


def provenance():
    """Local execution facts, deliberately separate from portable runtime receipts."""
    workspace, assembly = _helper()
    with ContainedReader(Path(workspace.name)) as reader:
        policy = reader.read(POLICY_PATH)
        runtime_config = strict_json(reader.read('bin/Debug/net10.0/Epic16.NativeHelper.runtimeconfig.json'))
        binary = reader.read('bin/Debug/net10.0/Epic16.NativeHelper.dll')
        built = strict_json(reader.read('execution-toolchain.json'))
    try:
        selected = subprocess.run(['dotnet', '--version'], cwd=workspace.name,
            capture_output=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise HelperUnavailable('helper SDK provenance unavailable') from error
    if selected.returncode:
        raise HelperUnavailable(_process_failure('helper SDK provenance unavailable', selected))
    sdk = selected.stdout.decode('utf-8').strip()
    validate_sdk(policy, sdk)
    if built != dict(sdk=sdk, policySha256=hashlib.sha256(policy).hexdigest()):
        raise HelperUnavailable('helper toolchain changed after compilation')
    return dict(policySha256=hashlib.sha256(policy).hexdigest(), sdk=sdk,
        runtime=_run(dict(operation='provenance')), runtimeConfig=runtime_config,
        helperSha256=hashlib.sha256(binary).hexdigest())


def _run(request):
    workspace, assembly = _helper()
    try:
        result = subprocess.run(['dotnet', str(assembly)], input=json.dumps(request).encode(),
            cwd=workspace.name, capture_output=True, timeout=60)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise HelperUnavailable('fixed helper execution unavailable') from error
    if result.returncode:
        raise ValueError('invalid native evidence')
    return json.loads(result.stdout)


@lru_cache(maxsize=128)
def canonicalize(value: str) -> str:
    """Use actual pinned EventStore serialization, preserving number token spelling."""
    return _run(dict(operation='canonicalize', json=value))['canonical']


@lru_cache(maxsize=256)
def normalize_party_id(value: str) -> str:
    """Exact source-derived PartyId normalization using the fixed .NET runtime."""
    return _run(dict(operation='partyId', value=value))['value']


def inspect_assembly(content: bytes, revision: str, test_type: str, method: str) -> dict:
    return _run(dict(operation='metadata', assembly=base64.b64encode(content).decode(),
                     revision=revision, type=test_type, method=method))


@lru_cache(maxsize=256)
def inspect_product(content: bytes, revision: str) -> dict:
    return _run(dict(operation='productMetadata', assembly=base64.b64encode(content).decode(), revision=revision))
