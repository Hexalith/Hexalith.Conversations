"""Portable native capture bindings. Original paths are metadata, never read targets."""
import os
import re
import stat
import jsonschema
from epic16_native import strict_json

from contained_reader import relative_path, ReadError
from evidence_boundary_current import digest


def policy(story):
    return dict(captureVersion=2, manifestPattern=f'artifacts/epic16-runtime/{story}/<scenario>.capture.json',
        binaryPattern=f'artifacts/epic16-runtime/{story}/binaries/<sha256>.dll',
        originalPathStyles=['posix','windows'], originalPathsAreMetadataOnly=True,
        publication='five native results and receipts, exact capture/binary inventory, deterministic final pair')


def capture_path(story, scenario):
    return f'artifacts/epic16-runtime/{story}/{scenario}.capture.json'


def binary_path(story, sha256):
    return f'artifacts/epic16-runtime/{story}/binaries/{sha256}.dll'


def original_relative(root, code_base, style):
    """Strict lexical relationship for POSIX or Windows drive-absolute captures.

    No host path conversion, normalization, resolution, UNC guessing or I/O.
    Relative codeBase always has the repository-relative POSIX protocol spelling.
    """
    if not isinstance(root, str) or not isinstance(code_base, str) or '\x00' in root+code_base:
        raise ValueError('capture path type')
    if style == 'posix':
        if not root.startswith('/') or root.startswith('//') or '\\' in root:
            raise ValueError('capture POSIX root')
        tail = root[1:]
        if tail: relative_path(tail)
        absolute = code_base.startswith('/')
        prefix = root.rstrip('/') + '/'
        if absolute:
            if not code_base.startswith(prefix): raise ValueError('original codeBase outside capture root')
            result = code_base[len(prefix):]
        else: result = code_base
    elif style == 'windows':
        if not re.match(r'^[A-Za-z]:[\\/]', root) or root.startswith(('\\\\', '//')):
            raise ValueError('capture Windows root')
        separator = root[2]
        other = '/' if separator == '\\' else '\\'
        if other in root or other in code_base and re.match(r'^[A-Za-z]:', code_base):
            raise ValueError('ambiguous capture separators')
        tail = root[3:].replace(separator, '/')
        if tail: relative_path(tail)
        absolute = bool(re.match(r'^[A-Za-z]:', code_base))
        if absolute:
            prefix = root.rstrip(separator) + separator
            if not code_base.startswith(prefix): raise ValueError('original codeBase outside capture root')
            result = code_base[len(prefix):].replace(separator, '/')
        else: result = code_base
    else: raise ValueError('capture path style')
    relative_path(result)
    return result


def no_alias(reader, path, need):
    # The caller has already read through the neutral no-follow reader. Refuse
    # even contained hardlinks so two logical artifacts cannot alias one inode.
    try:
        observed = os.lstat(reader.repository / path)
    except OSError as error:
        raise ReadError('INPUT_UNAVAILABLE', 'capture alias inspection unavailable', unavailable=True) from error
    need(stat.S_ISREG(observed.st_mode) and observed.st_nlink == 1, 'NATIVE_CAPTURE_ALIAS', path)


def resolve(reader, raw, code_base, candidate, scenario, read, load, need, schema):
    story = scenario[3:7]
    path = capture_path(story, scenario)
    raw_capture = read(reader, path)
    try:
        document = strict_json(raw_capture)
        jsonschema.validate(document, schema)
    except (ValueError, jsonschema.ValidationError):
        need(False, 'NATIVE_CAPTURE_INVALID', path)
    need(isinstance(document,dict) and set(document) == {'captureVersion','scenarioId','candidate','native','pathStyle','originalRepositoryRoot','originalCodeBase','originalRelativePath','binary','products'}, 'NATIVE_CAPTURE_INVALID', path)
    need(type(document['captureVersion']) is int and document['captureVersion'] == 2 and document['scenarioId'] == scenario and document['candidate'] == candidate, 'NATIVE_CAPTURE_DRIFT', path)
    native_path = f'artifacts/epic16-runtime/{story}/{scenario}.trx'
    need(document['native'] == dict(path=native_path,sha256=digest(raw)) and document['originalCodeBase'] == code_base, 'NATIVE_CAPTURE_DRIFT', path)
    try:
        relative = original_relative(document['originalRepositoryRoot'],code_base,document['pathStyle'])
    except (ValueError, TypeError):
        need(False, 'NATIVE_CAPTURE_PATH_INVALID', path)
    need(document['originalRelativePath'] == relative, 'NATIVE_CAPTURE_PATH_INVALID', path)
    binary = document['binary']
    need(isinstance(binary,dict) and set(binary) == {'path','sha256'} and isinstance(binary['sha256'],str) and re.fullmatch('[0-9a-f]{64}', binary['sha256']), 'NATIVE_CAPTURE_INVALID', path)
    need(binary['path'] == binary_path(story,binary['sha256']), 'NATIVE_CAPTURE_PATH_INVALID', binary['path'])
    content = read(reader,binary['path'])
    need(digest(content) == binary['sha256'], 'NATIVE_CAPTURE_DRIFT', binary['path'])
    for target in (path,native_path,binary['path']): no_alias(reader,target,need)
    return binary['path'], content, dict(path=path,sha256=digest(raw_capture)), document


def directory_names(reader, path):
    """List one fixed evidence directory through the same no-follow descriptors."""
    descriptors = []
    try:
        parent = reader._root_fds[-1]
        for part in relative_path(path):
            parent, _ = reader._open(part,parent,directory=True)
            descriptors.append(parent)
        return set(os.listdir(parent))
    except OSError as error:
        raise ReadError('INPUT_UNAVAILABLE', 'capture directory inventory unavailable', unavailable=True) from error
    finally:
        for fd in reversed(descriptors): os.close(fd)


def inventory(reader, story, rows, need):
    """Derive the exact retained transaction from five independently admitted tests."""
    artifacts = {}
    for row in rows:
        binary = row['bindings']['testBinary']
        need(isinstance(binary,dict) and binary.get('capture'), 'NATIVE_CAPTURE_INVALID', row['scenarioId'])
        entries=[{key:binary[key] for key in ('path','sha256')},binary['capture'],*binary['products']]
        for surface in row['bindings'].get('upstreamApiPreservation') or []:
            entries.extend(surface[phase] for phase in ('baseline','candidate'))
        compiled = row['bindings'].get('compiledPublicPreservation')
        if compiled: entries.extend(compiled[phase] for phase in ('baseline','candidate'))
        for entry in entries:
            path, sha = entry['path'], entry['sha256']
            need(path not in artifacts or artifacts[path] == sha, 'NATIVE_CAPTURE_CONFLICT', path)
            # Admission and publication are separate reads. Never publish cached
            # identities after retained bytes or their inode relationships drift.
            need(digest(reader.read(path)) == sha, 'NATIVE_CAPTURE_DRIFT', path)
            no_alias(reader, path, need)
            artifacts[path] = sha
    need(len([path for path in artifacts if path.endswith('.capture.json')]) == 5, 'NATIVE_CAPTURE_INVENTORY', story)
    root = f'artifacts/epic16-runtime/{story}'
    expected = {f'AC-{story}-0{n}.{ext}' for n in range(1,6) for ext in ('trx','json','capture.json')} | {'binaries'}
    need(directory_names(reader,root) == expected, 'NATIVE_CAPTURE_INVENTORY', root)
    need(directory_names(reader,root+'/binaries') == {path.rsplit('/',1)[1] for path in artifacts if path.endswith('.dll')}, 'NATIVE_CAPTURE_INVENTORY', root+'/binaries')
    return [dict(path=path,sha256=artifacts[path]) for path in sorted(artifacts)]
