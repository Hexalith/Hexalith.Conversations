"""Current, explicitly scoped evidence verification; neutral facts come from TestSupport."""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from typing import Any

import jsonschema
from config_utils import load_customization
import verify_story_completion_workflows as completion

BASELINE = '7f24b72d57da65745cdecdea5b5e7c0da1313b4d'
BINDING = '_bmad-output/planning-artifacts/story-10.3-current-binding-v1.json'
CONTRACT = '_bmad-output/planning-artifacts/story-10.3-current-contract-v1.json'
SCHEMA = '_bmad/schemas/evidence-boundary-result-v1.schema.json'
PROJECT = 'tests/Hexalith.Conversations.TestSupport.Runner/Hexalith.Conversations.TestSupport.Runner.csproj'
ASSEMBLY = 'tests/Hexalith.Conversations.TestSupport.Runner/bin/Debug/net10.0/Hexalith.Conversations.TestSupport.Runner.dll'
BEGIN = '<!-- EVIDENCE-BOUNDARY-GATE:BEGIN v1 -->'
END = '<!-- EVIDENCE-BOUNDARY-GATE:END v1 -->'
LOGICAL_ROUTES = ('bmad-build/step-04-review.md', *(route.path for route in completion.ROUTES))
WORKFLOWS = tuple(f'{tree}/{route}' for tree in completion.SKILL_TREES for route in LOGICAL_ROUTES)
GUIDANCE = ('_bmad/custom/bmad-build.toml', '_bmad/custom/bmad-build-auto.toml', '_bmad/custom/bmad-review.toml', 'docs/runbooks/current-change-validation.md')
RETIRED = tuple(f'{tree}/{name}' for tree in completion.SKILL_TREES for name in ('bmad-dev-story', 'bmad-quick-dev'))
HISTORICAL_PROVENANCE = (
    '_bmad-output/planning-artifacts/v9/story-contracts/10.3.json',
    '_bmad-output/planning-artifacts/v9-authority-bundle-v1.json',
    '_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json',
    '_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json',
    'docs/release-evidence/story-10.2-final-record-v2.json',
    'docs/release-evidence/story-10.2-final-record-v2.md',
)
CURRENT_PROVENANCE = 'tests/Hexalith.Conversations.TestSupport.Runner/Program.cs'
FAIL_CODES = frozenset({
    'EVIDENCE_HASH_MISMATCH', 'EVIDENCE_PATH_ESCAPE', 'EVIDENCE_OUTPUT_FORBIDDEN',
    'EVIDENCE_FILE_MISSING', 'SIGNABLE_PAYLOAD_MISMATCH', 'CHANGED_PATH_SET_MISMATCH',
    'EVIDENCE_BOUNDARY_GITLINK', 'ROOT_OF_TRUST_NOT_PINNED', 'SIGNED_EVIDENCE_ALLOWLISTED',
    'UNAVAILABLE_HISTORY_PASSED', 'SCOPE_NOT_EVALUATED', 'EVIDENCE_GATE_NOT_USED',
    'EVIDENCE_GATE_DISPLACED', 'EVIDENCE_WORKFLOW_PARITY_DRIFT', 'EVIDENCE_ALIAS_ROUTE_BROKEN',
    'EVIDENCE_GUIDANCE_NOT_BOUND', 'EVIDENCE_READER_NOT_SHARED', 'EVIDENCE_READER_STRENGTH_REDUCED',
    'AUTHORITY_BINDING_INVALID', 'EVIDENCE_RESULT_EXIT_MISMATCH', 'HELPER_REQUEST_INVALID', 'HELPER_FAILURE', 'RESULT_SCHEMA_INVALID',
})
BLOCKED_CODES = frozenset({'GIT_UNAVAILABLE', 'HISTORY_UNAVAILABLE', 'REVISION_UNAVAILABLE', 'REVISION_INVALID',
    'GIT_TIMEOUT', 'GIT_CANCELLED', 'AUTHORITY_UNAVAILABLE', 'HELPER_UNAVAILABLE', 'ARGUMENT_INVALID', 'OUTPUT_WRITE_FAILED'})
WARNING_CODES = frozenset({'EVIDENCE_SCOPE_NOT_APPLICABLE'})
STABLE_CODES = FAIL_CODES | BLOCKED_CODES | WARNING_CODES


def digest(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def canonical(value: Any) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()


def bridge(repository: Path, request: dict[str, Any]) -> dict[str, Any]:
    """Invoke the non-shipping bridge, with bounded build and execution."""
    tool_root = Path(__file__).resolve().parents[2]
    assembly = tool_root / ASSEMBLY
    # Build on each entry so changed helper sources cannot use stale compiled facts.
    try:
        build = subprocess.run(['dotnet', 'build', str(tool_root / PROJECT), '--configuration', 'Debug',
            '--nologo', '-p:NuGetAudit=false', '-p:ImportDirectoryPackagesProps=false'], capture_output=True, timeout=120)
    except FileNotFoundError:
        raise RuntimeError('HELPER_UNAVAILABLE') from None
    if build.returncode:
        raise RuntimeError('HELPER_FAILURE')
    try:
        run = subprocess.run(['dotnet', str(assembly)], input=json.dumps({'repository': str(repository), **request}).encode(),
            capture_output=True, timeout=60)
    except FileNotFoundError:
        raise RuntimeError('HELPER_UNAVAILABLE') from None
    try:
        response = json.loads(run.stdout)
        if run.returncode and isinstance(response, dict):
            codes = response.get('blockers', [])
            if len(codes) == 1 and codes[0] in FAIL_CODES | BLOCKED_CODES:
                raise RuntimeError(codes[0])
        if run.returncode or not isinstance(response, dict) or 'assertionLedger' not in response:
            raise ValueError('invalid helper envelope')
    except (ValueError, TypeError):
        raise RuntimeError('HELPER_FAILURE') from None
    return response


def empty_result(candidate: str | None = None) -> dict[str, Any]:
    return dict(schemaVersion='hexalith.conversations.evidence-boundary-result.v1', result='not-applicable',
        candidate=candidate, manifest=[], changedPaths=[], gitlinks=[], assertionLedger=[], warnings=[], blockers=[])


def finalize(result: dict[str, Any], *, applicable: bool) -> dict[str, Any]:
    result['blockers'] = sorted(set(result['blockers']))
    result['warnings'] = sorted(set(result['warnings']))
    rows = result['assertionLedger']
    if applicable and not any(row['state'] != 'SKIPPED' for row in rows):
        if not any(row['state'] == 'SKIPPED' for row in rows):
            result['blockers'] = sorted(set(result['blockers']) | {'SCOPE_NOT_EVALUATED'})
            rows.append(dict(id='scope', subject='applicable-scope', state='FAIL'))
    if any(row['state'] == 'SKIPPED' for row in rows) and not any(row['state'] == 'FAIL' for row in rows) and not set(result['blockers']) & FAIL_CODES:
        result['result'] = 'BLOCKED'
    elif result['blockers'] or any(row['state'] == 'FAIL' for row in rows):
        result['result'] = 'FAIL'
    else:
        result['result'] = 'PASS' if applicable else 'not-applicable'
    if not applicable and not result['blockers']:
        result['assertionLedger'] = []
        result['warnings'] = ['EVIDENCE_SCOPE_NOT_APPLICABLE']
    schema = json.loads((Path(__file__).resolve().parents[2] / SCHEMA).read_bytes())
    try:
        jsonschema.validate(result, schema)
    except jsonschema.ValidationError:
        raw = json.dumps(result, ensure_ascii=True, sort_keys=True, default=str)
        candidate = result.get('candidate')
        result = empty_result(candidate if isinstance(candidate, str) and len(candidate) == 40
            and all(character in '0123456789abcdef' for character in candidate) else None)
        result['result'] = 'FAIL'
        result['blockers'] = ['RESULT_SCHEMA_INVALID']
        result['assertionLedger'] = [dict(id='result:schema', subject=raw, state='FAIL')]
        jsonschema.validate(result, schema)
    return result


def record(result: dict[str, Any], identity: str, subject: str, passed: bool, code: str) -> None:
    if code not in STABLE_CODES:
        raise ValueError(f'unregistered code: {code}')
    result['assertionLedger'].append(dict(id=identity, subject=subject, state='PASS' if passed else 'FAIL'))
    if not passed:
        result['blockers'].append(code)


def historical_provenance_result(repository: Path, rows: list[dict[str, Any]]) -> dict[str, Any]:
    """Pin historical bytes to approved baseline blobs independently of candidate declarations."""
    result = empty_result(BASELINE)
    expected = set(HISTORICAL_PROVENANCE) | {CURRENT_PROVENANCE}
    record(result, 'provenance:membership', 'historical provenance',
        len(rows) == len(expected) and {row['path'] for row in rows} == expected,
        'AUTHORITY_BINDING_INVALID')
    if result['blockers']:
        return finalize(result, applicable=True)
    facts = bridge(repository, dict(baseline=BASELINE, candidate=BASELINE, blobPaths=list(HISTORICAL_PROVENANCE)))
    if facts['result'] != 'PASS':
        result['assertionLedger'].extend(dict(row, id='provenance:' + row['id']) for row in facts['assertionLedger'])
        result['blockers'].extend(code if code in STABLE_CODES else 'HELPER_FAILURE' for code in facts['blockers'])
        return finalize(result, applicable=True)
    declared = {row['path']: row['sha256'] for row in rows}
    measured = {row['path']: row for row in facts['blobs']}
    for path in HISTORICAL_PROVENANCE:
        row = measured.get(path)
        if row is None or row.get('code') or row.get('sha256') is None:
            result['assertionLedger'].append(dict(id='provenance:baseline:' + path, subject=path, state='SKIPPED'))
            result['blockers'].append('AUTHORITY_UNAVAILABLE')
        else:
            record(result, 'provenance:baseline:' + path, path, declared[path] == row['sha256'], 'EVIDENCE_HASH_MISMATCH')
    return finalize(result, applicable=True)


def approved_roots(repository: Path) -> dict[str, Any]:
    """Derive the permitted consumer and pins from verified approved-baseline provenance."""
    import generate_story_record as generator
    path = HISTORICAL_PROVENANCE[4]
    measured = bridge(repository, dict(baseline=BASELINE, candidate=BASELINE, blobPaths=[path]))
    row = measured['blobs'][0]
    if measured['result'] != 'PASS' or row.get('content') is None:
        raise RuntimeError('AUTHORITY_UNAVAILABLE')
    _, validators = generator.v2_load_schemas()
    previous = generator.v2_verified_predecessor(repository, BASELINE, '10.2', path,
        HISTORICAL_PROVENANCE[5], validators['record'])
    if previous != row['sha256']:
        raise RuntimeError('ROOT_OF_TRUST_NOT_PINNED')
    record = generator.v2_parse_json(base64.b64decode(row['content']))
    return dict(sourcePath=CURRENT_PROVENANCE, constants=dict(Story102RecordSha256=previous,
        Story102CandidateCommit=record['candidate']['commit']), allowlist=[])


def workflow_checks(repository: Path, result: dict[str, Any], binding: dict[str, Any]) -> None:
    """Check exact route spans, installed twins, deterministic renders and effective guidance."""
    blocks: dict[str, str] = {}
    renderer = completion.load_renderer()
    renders: dict[tuple[str, str], dict[str, str]] = {}
    for tree in completion.SKILL_TREES:
        for logical in LOGICAL_ROUTES:
            path = f'{tree}/{logical}'
            source = repository / path
            text = source.read_text() if source.is_file() else ''
            start, end = text.find(BEGIN), text.find(END)
            present = text.count(BEGIN) == text.count(END) == 1 and start < end
            record(result, f'workflow:presence:{path}', path, present, 'EVIDENCE_GATE_NOT_USED')
            if not present:
                continue
            block = text[start:end + len(END)]
            blocks[path] = block
            # The first transition is the governed review or completion operation.
            if logical in ('bmad-build/step-04-review.md', 'bmad-build-auto/step-04-review.md'):
                transition = 'Change `{spec_file}` status to `in-review`'
            else:
                transition = next(route.transition for route in completion.ROUTES if route.path == logical)
            transition_position = text.find(transition)
            call = block.find('python3 _bmad/scripts/verify_evidence_boundary.py')
            later_promotion = text.find('<!-- PROMOTION-GATE:END', end)
            in_span = call >= 0 and end < transition_position and (later_promotion < 0 or later_promotion > transition_position)
            record(result, f'workflow:span:{path}', path, in_span, 'EVIDENCE_GATE_DISPLACED')
            for clause in ('`FAIL`/`1`', '`BLOCKED`/`2`', '`not-applicable`/`0`', '--verify-workflows', 'explicitly requires', 'never write `review` or `done`'):
                record(result, f'workflow:clause:{path}:{clause}', path, clause in block, 'EVIDENCE_GATE_NOT_USED')
            skill = logical.split('/')[0]
            if skill == 'bmad-code-review':
                continue
            key = (tree, skill)
            if key not in renders:
                try:
                    renders[key] = completion.render_in_memory(renderer, repository, repository / tree / skill)
                    second = completion.render_in_memory(renderer, repository, repository / tree / skill)
                except (renderer.RenderError, ValueError, OSError):
                    code = 'EVIDENCE_GATE_NOT_USED' if any(not (repository / route).is_file() for route in WORKFLOWS) else 'EVIDENCE_WORKFLOW_PARITY_DRIFT'
                    record(result, f'render:unavailable:{path}', path, False, code)
                    continue
                record(result, f'render:deterministic:{tree}:{skill}', skill, second == renders[key], 'EVIDENCE_WORKFLOW_PARITY_DRIFT')
            rendered = renders[key][logical.split('/', 1)[1]]
            rendered_block = rendered[rendered.find(BEGIN):rendered.find(END) + len(END)]
            record(result, f'render:gate:{path}', path, block == rendered_block, 'EVIDENCE_WORKFLOW_PARITY_DRIFT')
    for logical in LOGICAL_ROUTES:
        left, right = (f'{tree}/{logical}' for tree in completion.SKILL_TREES)
        # Normalize line endings consistently with the renderer; all authored blocks are LF.
        record(result, f'workflow:twins:{logical}', logical,
            left in blocks and right in blocks and blocks[left] == blocks[right]
            and (repository / left).read_bytes() == (repository / right).read_bytes(), 'EVIDENCE_WORKFLOW_PARITY_DRIFT')
    for path in RETIRED:
        record(result, f'route:retired:{path}', path, not (repository / path).exists(), 'EVIDENCE_ALIAS_ROUTE_BROKEN')
    for name in ('bmad-build', 'bmad-build-auto', 'bmad-review'):
        resolved = load_customization(repository, repository / '.agents/skills' / name)
        expected = binding['resolvedCustomization'][name]
        record(result, f'guidance:resolved:{name}', name, digest(canonical(resolved)) == expected,
            'EVIDENCE_GUIDANCE_NOT_BOUND')
    for row in binding['guidanceInventory']['rows']:
        path = repository / row['path']
        record(result, f'guidance:file:{row["path"]}', row['path'], path.is_file()
            and digest(path.read_bytes()) == row['sha256'], 'EVIDENCE_GUIDANCE_NOT_BOUND')
    # Story 7.3's independent guard stays operational on every installed/rendered surface.
    try:
        surfaces, _ = completion.collect_surfaces(repository)
    except completion.Blocked:
        record(result, 'completion:surfaces', 'Story 7.3 surfaces', False, 'EVIDENCE_GATE_NOT_USED')
        return
    for check in (completion.presence_checks, completion.parity_checks):
        findings, rows = check(surfaces)
        for ordinal, (subject, state) in enumerate(rows):
            record(result, f'completion:{check.__name__}:{ordinal}', subject, state == 'PASS', 'EVIDENCE_GATE_NOT_USED')


def verify(repository: Path, baseline: str, candidate: str, *, authority_bundle: str,
           binding_path: str = BINDING, verify_workflows: bool = False) -> dict[str, Any]:
    result = empty_result()
    try:
        initial = bridge(repository, dict(baseline=baseline, candidate=candidate,inspectChangedReaders=True,containedPaths=[binding_path,authority_bundle],blobPaths=[
            'docs/release-evidence/story-10.3-final-record-v2.json','docs/release-evidence/story-10.3-final-record-v2.md']))
        result['candidate'] = initial['candidate']
        candidate = initial['candidate'] or candidate
        baseline = initial.get('baseline') or baseline
        if initial['result'] == 'BLOCKED':
            result['blockers'] = [code for code in initial['blockers'] if code in BLOCKED_CODES] or ['HISTORY_UNAVAILABLE']
            result['assertionLedger'] = initial['assertionLedger']
            return finalize(result, applicable=True)
        if any(row['code'] for row in initial['containedPaths']):
            result['blockers'] = ['EVIDENCE_PATH_ESCAPE']
            result['assertionLedger'] = [dict(id='containment',subject=binding_path,state='FAIL')]
            return finalize(result,applicable=True)
        binding_file = Path(initial['containedPaths'][0]['fullPath'])
        if not binding_file.is_file():
            raise FileNotFoundError(binding_path)
        if not Path(initial['containedPaths'][1]['fullPath']).is_file():
            raise FileNotFoundError(authority_bundle)
        import generate_story_record as generator
        binding = generator.v2_parse_json(binding_file.read_bytes())
        entries = initial.get('diff') or []
        paths = sorted({entry['path'] for entry in entries} | {entry['sourcePath'] for entry in entries if entry['sourcePath']})
        result['changedPaths'] = paths
        result['gitlinks'] = [dict(path=entry['path'], sourceMode=entry['sourceMode'], destinationMode=entry['destinationMode'])
            for entry in entries if '160000' in (entry['sourceMode'], entry['destinationMode'])]
        record(result, 'binding:baseline', binding_path, baseline == binding['baseline'] == BASELINE, 'AUTHORITY_BINDING_INVALID')
        if 'AUTHORITY_BINDING_INVALID' in result['blockers']:
            return finalize(result, applicable=True)
        applicable = verify_workflows or bool(initial['evidenceReaders']) or any(path.startswith(('docs/release-evidence/', '_bmad-output/planning-artifacts/',
            'tests/Hexalith.Conversations.TestSupport', '_bmad/scripts/')) or path in WORKFLOWS + GUIDANCE for path in paths)
        if not applicable:
            return finalize(result, applicable=False)
        result['assertionLedger'].extend(row for row in initial['assertionLedger'] if row['id'].startswith('reader:'))
        result['blockers'].extend(code for code in initial['blockers'] if code.startswith('EVIDENCE_READER_'))
        expected_paths = list(binding['expectedChangedPaths'])
        publication_manifest = []
        pair_blobs = {row['path']:row for row in initial['blobs']}
        pair_json = pair_blobs['docs/release-evidence/story-10.3-final-record-v2.json']
        if pair_json['content'] is not None:
            pair_markdown = pair_blobs['docs/release-evidence/story-10.3-final-record-v2.md']
            json_bytes = base64.b64decode(pair_json['content'])
            markdown_bytes = base64.b64decode(pair_markdown['content']) if pair_markdown['content'] is not None else b''
            published = generator.v2_parse_json(json_bytes)
            _, validators = generator.v2_load_schemas()
            problems = generator.v2_schema_errors(validators['record'],published) + generator.v2_verify_pair(json_bytes,markdown_bytes)
            if not problems:
                problems = generator.v2_10_3_source_errors(repository,published,validators['record'],result['candidate'])
            if not problems:
                problems = generator.v2_10_3_descendant_errors(repository,published,result['candidate'])
            record(result,'publication:verified','10.3',not problems,'AUTHORITY_BINDING_INVALID')
            if not problems:
                publication_manifest = [dict(path=path,sha256=row['sha256'],role='publication') for path,row in pair_blobs.items()]
                publication_manifest += [dict(path=row['path'],sha256=row['sha256'],role='receipt') for row in published['evidenceWorkflowExecution']['receipts']]
                expected_paths = sorted(set(expected_paths) | {published['outputs'][kind]['path'] for kind in ('json','markdown')}
                    | {row['path'] for row in published['evidenceWorkflowExecution']['receipts']}
                    | ({'_bmad-output/implementation-artifacts/sprint-status.yaml'} if '_bmad-output/implementation-artifacts/sprint-status.yaml' in paths else set()))
        record(result, 'binding:exemptions', binding_path, binding.get('exemptions') == [], 'SIGNED_EVIDENCE_ALLOWLISTED')
        for name, expected in (('workflowInventory', WORKFLOWS), ('guidanceInventory', GUIDANCE)):
            inventory = binding[name]
            rows = inventory['rows']
            valid = (len(rows) == len(expected) and {row['path'] for row in rows} == set(expected)
                and len({row['path'] for row in rows}) == len(rows) and inventory['sha256'] == digest(canonical(rows)))
            record(result, 'binding:inventory:'+name, name, valid, 'AUTHORITY_BINDING_INVALID')
        provenance = historical_provenance_result(repository, binding['provenance'])
        result['assertionLedger'].extend(provenance['assertionLedger'])
        result['blockers'].extend(provenance['blockers'])
        if provenance['result'] == 'BLOCKED':
            return finalize(result, applicable=True)
        roots = approved_roots(repository)
        record(result, 'roots:approved', CURRENT_PROVENANCE, binding['rootConstants'] == roots, 'ROOT_OF_TRUST_NOT_PINNED')
        manifest = binding['provenance'] + binding['workflowInventory']['rows'] + binding['guidanceInventory']['rows'] + publication_manifest
        manifest = [dict(path=row['path'], sha256=row['sha256'], role=row.get('role', 'evidence')) for row in manifest]
        payload = ''.join(f'{row["path"]}\t{row["sha256"]}\t{row["role"]}\n' for row in sorted(manifest, key=lambda row: row['path']))
        facts = bridge(repository, dict(baseline=baseline, candidate=candidate,
            expectedPaths=expected_paths, manifest=manifest,
            signablePayloadHash=digest(payload.encode()), roots=roots,
            blobPaths=[row['path'] for row in manifest] + [binding_path]))
        result['manifest'] = manifest
        result['assertionLedger'].extend(facts['assertionLedger'])
        result['blockers'].extend('SCOPE_NOT_EVALUATED' if code == 'ASSERTION_LEDGER_EMPTY' else
            code if code in STABLE_CODES else 'HELPER_FAILURE' for code in facts['blockers'])
        for row in facts['blobs']:
            expected = next((entry['sha256'] for entry in manifest if entry['path'] == row['path']), digest(binding_file.read_bytes()))
            record(result, 'candidate:blob:' + row['path'], row['path'], row['sha256'] == expected, 'EVIDENCE_HASH_MISMATCH')
        record(result, 'authority:selection', authority_bundle, authority_bundle == binding['authorityBundle']['path'], 'AUTHORITY_BINDING_INVALID')
        import generate_story_record as generator
        _, validators = generator.v2_load_schemas()
        previous = generator.v2_verified_predecessor(repository, result['candidate'], '10.2',
            'docs/release-evidence/story-10.2-final-record-v2.json', 'docs/release-evidence/story-10.2-final-record-v2.md', validators['record'])
        record(result, 'predecessor:10.2', '10.2', previous == binding['predecessor']['sha256'], 'AUTHORITY_BINDING_INVALID')
        if 'EVIDENCE_PATH_ESCAPE' not in result['blockers']:
            workflow_checks(repository, result, binding)
        return finalize(result, applicable=True)
    except FileNotFoundError:
        result['blockers'].append('AUTHORITY_UNAVAILABLE')
        result['assertionLedger'].append(dict(id='authority', subject=binding_path, state='SKIPPED'))
    except (subprocess.TimeoutExpired, OSError, RuntimeError) as error:
        code = str(error) if str(error) in STABLE_CODES else 'HELPER_UNAVAILABLE'
        result['blockers'].append(code)
        result['assertionLedger'].append(dict(id='helper-fault', subject='bridge', state='FAIL' if code in FAIL_CODES else 'SKIPPED'))
    except (ValueError, KeyError, TypeError, jsonschema.ValidationError) as error:
        result['blockers'].append('AUTHORITY_BINDING_INVALID')
        result['assertionLedger'].append(dict(id='invalid-binding', subject=binding_path, state='FAIL'))
    return finalize(result, applicable=True)


def transition_decision(document: Any, observed_exit: int) -> dict[str, Any]:
    """Advance only a schema-valid, registered, matching PASS or empty non-applicable result."""
    expected_exit = {'PASS': 0, 'FAIL': 1, 'BLOCKED': 2, 'not-applicable': 0}
    if not isinstance(document, dict):
        return dict(allowed=False,recordedResult=None,blockers=['EVIDENCE_RESULT_EXIT_MISMATCH'])
    state = document.get('result')
    valid = False
    try:
        jsonschema.validate(document, json.loads((Path(__file__).resolve().parents[2] / SCHEMA).read_bytes()))
        valid = (document['blockers'] == sorted(set(document['blockers']))
            and document['warnings'] == sorted(set(document['warnings']))
            and set(document['blockers']).issubset(FAIL_CODES | BLOCKED_CODES)
            and set(document['warnings']).issubset(WARNING_CODES)
            and (state != 'not-applicable' or not document['assertionLedger'] and not document['blockers']))
    except (jsonschema.ValidationError, KeyError, TypeError, ValueError):
        pass
    matches = valid and expected_exit.get(state) == observed_exit
    known = {code for code in document.get('blockers', []) if isinstance(code,str) and code in STABLE_CODES} if isinstance(document.get('blockers'),list) else set()
    return dict(allowed=matches and state in ('PASS', 'not-applicable'), recordedResult=state,
        blockers=sorted(known | (set() if matches else {'EVIDENCE_RESULT_EXIT_MISMATCH'})))


def main(arguments: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(allow_abbrev=False)
    parser.add_argument('--repository', default='.')
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--candidate', default='HEAD')
    parser.add_argument('--authority-bundle', required=True)
    parser.add_argument('--current-binding', default=BINDING)
    parser.add_argument('--verify-workflows', action='store_true')
    parser.add_argument('--format', choices=['json'], default='json')
    parser.add_argument('--output')
    try:
        args = parser.parse_args(arguments)
    except SystemExit as error:
        if error.code == 0:
            return 0
        failure = empty_result()
        failure['blockers'] = ['ARGUMENT_INVALID']
        failure['assertionLedger'] = [dict(id='arguments',subject='argv',state='SKIPPED')]
        sys.stdout.write(json.dumps(finalize(failure,applicable=True),indent=2)+'\n')
        return 2
    repository = Path(args.repository).resolve()
    document = verify(repository, args.baseline, args.candidate, authority_bundle=args.authority_bundle,
        binding_path=args.current_binding, verify_workflows=args.verify_workflows)
    content = json.dumps(document, indent=2) + '\n'
    if args.output:
        try:
            containment = bridge(repository,dict(baseline=BASELINE,candidate=document['candidate'] or args.candidate,
                containedPaths=[args.output],includeTreePaths=True))
            path_fact = containment['containedPaths'][0]
            if path_fact['code']:
                document['blockers'].append('EVIDENCE_PATH_ESCAPE')
                document['assertionLedger'].append(dict(id='output:containment',subject=args.output,state='FAIL'))
            else:
                target = Path(path_fact['fullPath'])
                protected = set(containment.get('treePaths', [])) | set(document['changedPaths']) | set(WORKFLOWS + GUIDANCE + HISTORICAL_PROVENANCE)
                protected |= {row['path'] for row in document['manifest']} | {args.authority_bundle,args.current_binding,CONTRACT,SCHEMA,PROJECT,CURRENT_PROVENANCE}
                protected |= {path.relative_to(repository).as_posix() for path in (repository/'.git').rglob('*') if path.is_file()}
                relative = target.relative_to(repository)
                collision = any(part.casefold() == '.git' for part in relative.parts) or relative.as_posix() in protected or (target.exists() and any(
                    (repository/path).is_file() and target.samefile(repository/path) for path in protected))
                if collision:
                    record(document, 'output:protected-input', args.output, False, 'EVIDENCE_OUTPUT_FORBIDDEN')
                elif containment.get('result') == 'BLOCKED':
                    document['blockers'].extend(containment['blockers'])
                    document['assertionLedger'].append(dict(id='output:history',subject=args.output,state='SKIPPED'))
                else:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_text(content)
        except (RuntimeError, subprocess.TimeoutExpired) as error:
            code = str(error) if str(error) in STABLE_CODES else 'HELPER_UNAVAILABLE'
            document['blockers'].append(code)
            document['assertionLedger'].append(dict(id='output:helper',subject=args.output,
                state='FAIL' if code in FAIL_CODES else 'SKIPPED'))
        except (ValueError, KeyError, TypeError):
            document['blockers'].append('HELPER_FAILURE')
            document['assertionLedger'].append(dict(id='output:malformed-helper',subject=args.output,state='FAIL'))
        except OSError:
            document['blockers'].append('OUTPUT_WRITE_FAILED')
            document['assertionLedger'].append(dict(id='output:write',subject=args.output,state='SKIPPED'))
        document = finalize(document,applicable=document['result'] != 'not-applicable' or bool(document['blockers']))
        content = json.dumps(document,indent=2)+'\n'
    sys.stdout.write(content)
    exit_code = {'PASS': 0, 'FAIL': 1, 'BLOCKED': 2, 'not-applicable': 0}[document['result']]
    decision = transition_decision(document, exit_code)
    return exit_code if decision['allowed'] or exit_code != 0 else 1
