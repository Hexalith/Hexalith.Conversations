#!/usr/bin/env python3
"""Verify the committed Checkpoint A -> B -> C chain without rebinding predecessors.

Only Story 11.3 uses this route. Git facts come from the neutral TestSupport bridge;
missing history is BLOCKED, incompatible or modified evidence is FAIL.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
import sys

import evidence_boundary_current as current
import generate_story_record as records
import verify_minimal_module_fixture as fixture

CONTRACT = '_bmad-output/planning-artifacts/v9/story-contracts/11.3.json'
AUTHORITY = '_bmad-output/planning-artifacts/v9-authority-bundle-v1.json'
PREDECESSOR_PUBLICATION = '984daa891ad6afafa378bff305748bf3e62bc04c'
MISMATCH = 'CHECKPOINT_CANDIDATE_MISMATCH'
RECORDS = tuple(f'docs/release-evidence/story-{story}-final-record-v2.{extension}'
                for story in ('11.1', '11.2') for extension in ('json', 'md'))


def read(repository: Path, path: str) -> bytes:
    return records.v2_10_1_artifact_bytes(repository, path)


def check(result: dict, identity: str, subject: str, passed: bool, code: str = MISMATCH) -> None:
    result['assertionLedger'].append(dict(id=identity, subject=subject, state='PASS' if passed else 'FAIL'))
    if not passed:
        result['blockers'].append(code)


def finish(result: dict) -> dict:
    if not result['assertionLedger']:
        check(result, 'nonempty-ledger', 'candidate verification', False)
    failed = any(row['state'] == 'FAIL' for row in result['assertionLedger'])
    skipped = any(row['state'] == 'SKIPPED' for row in result['assertionLedger'])
    if failed and not result['blockers']:
        result['blockers'].append(MISMATCH)
    if skipped and not failed and not any(code in current.BLOCKED_CODES for code in result['blockers']):
        result['blockers'].append('HISTORY_UNAVAILABLE')
    result['blockers'] = sorted(set(result['blockers']))
    result['result'] = ('FAIL' if failed else
                        'BLOCKED' if skipped or any(code in current.BLOCKED_CODES for code in result['blockers'])
                        else 'FAIL' if result['blockers'] else 'PASS')
    return result


def verify(repository: Path, candidate: str = 'HEAD', *, authority: str = AUTHORITY,
           contract_path: str = CONTRACT) -> dict:
    result = dict(schemaVersion='hexalith.conversations.story-candidate-result.v1', result='BLOCKED',
                  candidate=None, checkpoints={}, gitlinks=[], bindings=[], assertionLedger=[], blockers=[])
    try:
        commit = fixture.resolve_commit(repository, candidate)
        result['candidate'] = commit
        if authority != AUTHORITY or contract_path != CONTRACT:
            raise ValueError('the closed Story 11.3 route requires its contracted inputs')
        contract = records.v2_parse_json(read(repository, CONTRACT))
        bundle = records.v2_parse_json(read(repository, AUTHORITY))
        _, validators = records.v2_load_schemas()
        findings = []
        bound = records.v2_authority(repository, commit, CONTRACT, contract, validators['bundle'], findings)
        check(result, 'authority', CONTRACT, bound is not None and not findings and contract['storyId'] == '11.3')
        loaded = []
        for story, jp, mp in (('11.1', *RECORDS[:2]), ('11.2', *RECORDS[2:])):
            j, m = read(repository, jp), read(repository, mp)
            record = records.v2_parse_json(j)
            errors = records.v2_schema_errors(validators['record'], record) + records.v2_verify_pair(j, m)
            errors += (records.v2_11_1_pair_errors(record) if story == '11.1'
                       else records.v2_11_2_pair_errors(record))
            check(result, 'pair:' + story, jp, not errors and record['storyId'] == story)
            loaded.append(record)
        a, b = loaded
        fixture.resolve_commit(repository, PREDECESSOR_PUBLICATION)
        for path in (*RECORDS, fixture.PROOF_JSON_PATH, fixture.PROOF_MARKDOWN_PATH):
            accepted = records.v2_committed_blob(repository, PREDECESSOR_PUBLICATION, path)
            check(result, 'accepted-publication:' + path, PREDECESSOR_PUBLICATION,
                  accepted is not None and accepted == read(repository, path))
        for spec_path, markdown_path in ((records.V2_11_1_SPEC, RECORDS[1]), (records.V2_11_2_SPEC, RECORDS[3])):
            spec_bytes = read(repository, spec_path)
            region = records.v2_record_region(spec_bytes)
            accepted_spec = records.v2_committed_blob(repository, PREDECESSOR_PUBLICATION, spec_path)
            check(result, 'predecessor:insertion:' + spec_path, markdown_path, region is not None
                  and spec_bytes[region[0]:region[1]] == read(repository, markdown_path)
                  and spec_bytes == accepted_spec)
        ac, bc = (item['candidate']['commit'] for item in loaded)
        # Resolve both original source revisions before ancestry checks; unavailable is never a mismatch PASS.
        for checkpoint in (ac, bc, contract['authority']['planningCandidate']):
            fixture.resolve_commit(repository, checkpoint)
        for first, second in ((contract['authority']['planningCandidate'], ac), (ac, bc), (bc, commit)):
            ancestry = records.run_git(repository, 'merge-base', '--is-ancestor', first, second,
                                       allowed_returncodes=(0, 1))
            check(result, 'ancestry:' + first, second, ancestry.returncode == 0)
        result['checkpoints'] = dict(A=ac, B=bc, C=commit)
        af, bf = a['thinGuidanceExecution'], b['fixtureProofExecution']
        check(result, 'predecessor:B-A', RECORDS[0], bf['predecessor']['recordSha256'] == current.digest(read(repository, RECORDS[0])))

        # Every measured predecessor input stays byte-identical at C and in this working tree.
        original_inputs = {row['path']: row['sha256'] for row in bf['fixture']['lockFiles']}
        proof = records.v2_parse_json(read(repository, fixture.PROOF_JSON_PATH))
        original_inputs.update({row['path']: row['sha256'] for row in proof['fixtureInventory']['paths']})
        original_inputs.update({row['path']: row['sha256'] for row in bf['receipts']})
        original_inputs.update({bf['contract']['path']: bf['contract']['sha256']})
        for prefix in ('json', 'markdown', 'schema'):
            original_inputs[bf['proof'][prefix + 'Path']] = bf['proof'][prefix + 'Sha256']
        for prefix in ('source', 'json', 'markdown', 'schema'):
            original_inputs[af['guidance'][prefix + 'Path']] = af['guidance'][prefix + 'Sha256']
        original_inputs.update({row['path']: row['sha256'] for row in af['receipts']})
        original_inputs[af['contract']['path']] = af['contract']['sha256']
        original_inputs.update({path: current.digest(read(repository, path)) for path in RECORDS})
        paths = sorted(set(original_inputs) | {CONTRACT, AUTHORITY})
        raw = current.bridge(repository, dict(baseline=bc, candidate=commit, blobPaths=paths,
                                               includeRootGitlinks=True))
        result['assertionLedger'] += raw['assertionLedger']
        result['blockers'] += raw['blockers']
        if raw['result'] == 'BLOCKED':
            if not any(row['state'] == 'SKIPPED' for row in raw['assertionLedger']):
                result['assertionLedger'].append(dict(id='helper:blocked', subject='neutral Git facts', state='SKIPPED'))
            return finish(result)
        check(result, 'helper:result', 'neutral Git facts must pass', raw['result'] == 'PASS')
        check(result, 'gitlinks:raw-diff', 'B to C raw modes', not any(
            '160000' in (row['sourceMode'], row['destinationMode']) for row in raw['diff']))
        result['gitlinks'] = raw['rootGitlinks']
        check(result, 'gitlinks:B-C', 'all root gitlinks', raw['rootGitlinks'] == b['candidate']['gitlinks'])
        blobs = {row['path']: row for row in raw['blobs']}
        for path in paths:
            content = read(repository, path)
            row = blobs[path]
            expected = original_inputs.get(path, current.digest(content))
            check(result, 'input:' + path, path, row['code'] is None and row['sha256'] == expected
                  and current.digest(content) == expected)
            result['bindings'].append(dict(path=path, sha256=expected))
        # Bind predecessor material to its original committed sources rather than trusting updated declarations.
        for facts, revision in ((af, ac), (bf, bc)):
            source = facts['contract']
            check(result, 'original:' + source['path'], revision,
                  current.digest(records.v2_committed_blob(repository, revision, source['path']) or b'') == source['sha256'])
        for row in proof['fixtureInventory']['paths']:
            check(result, 'original:fixture:' + row['path'], bc,
                  current.digest(records.v2_committed_blob(repository, bc, row['path']) or b'') == row['sha256'])
        for prefix in ('source', 'json', 'markdown', 'schema'):
            path, digest = af['guidance'][prefix + 'Path'], af['guidance'][prefix + 'Sha256']
            check(result, 'original:guidance:' + prefix, ac,
                  current.digest(records.v2_committed_blob(repository, ac, path) or b'') == digest)
        import generate_minimal_module_fixture_proof as proof_generator
        import verify_thin_authoring_guidance as guidance
        check(result, 'guidance:verified', 'Checkpoint A source, review, deterministic pair',
              not guidance.verify(repository, fixture.GUIDANCE_JSON, fixture.GUIDANCE_MARKDOWN, True))
        proof_signable = proof_generator.signable_of(proof)
        proof_md = read(repository, fixture.PROOF_MARKDOWN_PATH)
        import jsonschema
        proof_errors = list(jsonschema.Draft202012Validator(json.loads(read(repository, fixture.PROOF_SCHEMA_PATH))).iter_errors(proof))
        check(result, 'proof:binding', 'Checkpoint B original candidate and proof pair', not proof_errors
              and proof['candidate']['commit'] == bc
              and proof['candidate']['eventStoreGitlink'] in b['candidate']['gitlinks']
              and proof['candidate']['contractSha256'] == bf['contract']['sha256']
              and proof['digests']['signablePayloadSha256'] == current.digest(current.canonical(proof_signable))
              and proof_md == proof_generator.render_markdown(proof).encode()
              and current.digest(proof_md) == proof['digests']['markdownSha256']
              and proof['digests']['signablePayloadSha256'] == bf['proof']['signablePayloadSha256'])
        inventory_errors, inventory = fixture.check_inventory(repository, fixture.FIXTURE_ROOT)
        graph_errors, _ = fixture.check_graph(repository, fixture.FIXTURE_ROOT)
        check(result, 'fixture:original-B', 'frozen inventory and public-API graph at B',
              not inventory_errors and not graph_errors and inventory['paths'] == proof['fixtureInventory']['paths'])
    except fixture.Blocked:
        result['blockers'].append('HISTORY_UNAVAILABLE')
        result['assertionLedger'].append(dict(id='history', subject=candidate, state='SKIPPED'))
    except (OSError, subprocess.TimeoutExpired, records.GateError, RuntimeError) as error:
        code = str(error) if str(error) in current.BLOCKED_CODES else 'HELPER_UNAVAILABLE'
        result['blockers'].append(code)
        result['assertionLedger'].append(dict(id='helper', subject='Git and TestSupport', state='SKIPPED'))
    except (ValueError, KeyError, TypeError, AttributeError):
        check(result, 'binding:malformed', 'checkpoint inputs', False)
    return finish(result)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(allow_abbrev=False)
    parser.add_argument('--repository', required=True)
    parser.add_argument('--authority-bundle', required=True)
    parser.add_argument('--contract', required=True)
    parser.add_argument('--candidate', default='HEAD')
    parser.add_argument('--output')
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    result = verify(repository, args.candidate, authority=args.authority_bundle, contract_path=args.contract)
    content = json.dumps(result, indent=2) + '\n'
    if args.output:
        # This verifier owns one receipt only; it cannot overwrite any source or historical input.
        if args.output != 'artifacts/v9/11.3/AC-11.3-05.json':
            check(result, 'output', args.output, False)
            finish(result)
            content = json.dumps(result, indent=2) + '\n'
        else:
            target = repository / args.output
            records.v2_output_target(repository, args.output)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(content, encoding='utf-8')
    print(content, end='')
    return {'PASS': 0, 'FAIL': 1, 'BLOCKED': 2}[result['result']]


if __name__ == '__main__':
    sys.exit(main())
