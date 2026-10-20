#!/usr/bin/env python3
"""Independently count fixture bytes and verify Story 11.3 SM-2/OQ-2 evidence.

No declared file count, LOC total, baseline hash, confidence or decision is trusted.
Shared rendering is only used after independent semantic checks.
"""
from __future__ import annotations

import argparse
from decimal import Decimal, ROUND_HALF_UP
import json
from pathlib import Path, PurePosixPath
import sys

import jsonschema
import generate_sm2_evidence as generator
import generate_story_record as records
import verify_minimal_module_fixture as fixture
import verify_story_candidate as chain

# Independent pins: changing both historical bytes and a declared digest cannot pass.
HISTORY = (
    ('docs/release-evidence/consume-promote-keep-inventory-v1.json', '20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7'),
    ('docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json', '14de3c86628cb4f900008be42d086a7699ae0869b3b791f92c810ab921192e03'),
    ('docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md', '1cb37b3ddbc9186748c7556a87e5c08feb15d8dee30f4a1b0362bcd5536297fd'),
    ('docs/release-evidence/oq-2-target-interpretation-decision-v1.json', '06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06'),
)
FAULT_EXPECTATIONS = {
    'source-added': 'SM2_SOURCE_INVENTORY_DRIFT',
    'source-missing': 'SM2_SOURCE_INVENTORY_DRIFT',
    'platform-ownership-added': 'SM2_INCLUSION_RULE_VIOLATION',
    'apphost-omitted': 'SM2_APPHOST_COST_OMITTED',
    'apphost-project-omitted': 'SM2_APPHOST_COST_OMITTED',
    'path-duplicated': 'SM2_INCLUSION_RULE_VIOLATION',
    'path-unclassified': 'SM2_INCLUSION_RULE_VIOLATION',
    'source-omitted': 'SM2_INCLUSION_RULE_VIOLATION',
    'declared-files': 'SM2_MEASUREMENT_INVALID',
    'declared-loc': 'SM2_MEASUREMENT_INVALID',
    'declared-nonblank': 'SM2_MEASUREMENT_INVALID',
    'source-hash': 'SM2_MEASUREMENT_INVALID',
    'physical-loc-row': 'SM2_MEASUREMENT_INVALID',
    'sm1-mutated': 'SM1_BASELINE_MUTATED',
    'v1-json-mutated': 'SM2_V1_HISTORY_MUTATED',
    'v1-markdown-mutated': 'SM2_V1_HISTORY_MUTATED',
    'history-declaration-rehashed': 'SM2_V1_HISTORY_MUTATED',
    'arithmetic': 'SM2_MEASUREMENT_INVALID',
    'confidence': 'SM2_MEASUREMENT_INVALID',
    'decision': 'OQ2_DECISION_INVALID',
    'sensitivity': 'SM2_MEASUREMENT_INVALID',
    'payload-digest': 'EVIDENCE_FORMAT_DRIFT',
    'markdown': 'EVIDENCE_FORMAT_DRIFT',
    'unknown-property': 'EVIDENCE_FORMAT_DRIFT',
    'checkpoint-candidate': 'CHECKPOINT_CANDIDATE_MISMATCH',
    'predecessor-binding': 'CHECKPOINT_CANDIDATE_MISMATCH',
    'checkpoint-input-drift': 'CHECKPOINT_CANDIDATE_MISMATCH',
    'history-unavailable': 'HISTORY_UNAVAILABLE',
}


digest = chain.current.digest

# The snapshot covers every input read by the cost/chain measurement, both evidence
# formats, and all non-generated fixture files (including additions/removals).
# Temporary Git pointers, implementation/test copies and bin/obj are not inputs.
MEASUREMENT_INPUTS = tuple(sorted({
    chain.CONTRACT, chain.AUTHORITY, generator.SCHEMA, generator.JSON_PATH, generator.MARKDOWN_PATH,
    'global.json', 'Hexalith.Conversations.slnx', 'docs/domain-module-authoring-template.md',
    'docs/release-evidence/thin-authoring-guidance-v2.schema.json', fixture.PROOF_SCHEMA_PATH,
    fixture.GUIDANCE_JSON, fixture.GUIDANCE_MARKDOWN, fixture.PROOF_JSON_PATH, fixture.PROOF_MARKDOWN_PATH,
    records.V2_11_1_SPEC, records.V2_11_2_SPEC, *chain.RECORDS, *(path for path, _ in HISTORY),
    '_bmad-output/planning-artifacts/v9/story-contracts/11.1.json',
    '_bmad-output/planning-artifacts/v9/story-contracts/11.2.json',
    'docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json',
    'docs/release-evidence/story-7.4-final-record-v2.json', 'docs/release-evidence/story-10.4-final-record-v2.json',
    *(f'artifacts/v9/11.1/AC-11.1-0{number}.{extension}' for number, extension in ((3, 'trx'), (4, 'trx'), (5, 'json'), (6, 'xml'))),
    'artifacts/v9/11.2/AC-11.2-01.json', 'artifacts/v9/11.2/AC-11.2-05.trx',
    'artifacts/v9/11.2/AC-11.2-07.xml', fixture.TEST_RESULT_PATH, *fixture.COMMAND_RECEIPTS.values(),
    *fixture.FROZEN_PATHS,
    *(f'{fixture.FIXTURE_ROOT}/{PurePosixPath(project).parent}/packages.lock.json' for project in fixture.EXPECTED_PROJECTS),
}))


def measurement_input_paths(repository: Path) -> list[str]:
    return sorted(set(MEASUREMENT_INPUTS) | {fixture.FIXTURE_ROOT + '/' + path
                                           for path in fixture.fixture_files(repository / fixture.FIXTURE_ROOT)})


def measurement_input_sha256(repository: Path) -> str:
    """Hash sorted path/content-digest rows; missing fixed inputs have an explicit null value."""
    contents = {path: digest(chain.read(repository, path)) if (repository / path).is_file() else None
                for path in measurement_input_paths(repository)}
    return digest(fixture.canonical(contents))


def verify(repository: Path, evidence: str = generator.JSON_PATH, markdown: str = generator.MARKDOWN_PATH,
           require_oq2_decision: bool = True, *, candidate: str | None = None) -> dict:
    report = dict(schemaVersion='hexalith.conversations.sm2-verification-result.v1', result='BLOCKED',
                  candidate=None, evidenceSha256=None, metrics={}, assertionLedger=[], blockers=[])

    def check(identity, subject, passed, code):
        report['assertionLedger'].append(dict(id=identity, subject=subject, state='PASS' if passed else 'FAIL'))
        if not passed:
            report['blockers'].append(code)

    try:
        raw, md = chain.read(repository, evidence), chain.read(repository, markdown)
        doc = records.v2_parse_json(raw)
        report['evidenceSha256'] = digest(raw)
        revision = candidate or doc['candidate']['commit']
        commit = fixture.resolve_commit(repository, revision)
        report['candidate'] = commit
        for index, (path, expected_hash) in enumerate(HISTORY):
            content = chain.read(repository, path)
            check('history:' + path, path, digest(content) == expected_hash
                  and digest(records.v2_committed_blob(repository, commit, path) or b'') == expected_hash,
                  'SM1_BASELINE_MUTATED' if index == 0 else 'SM2_V1_HISTORY_MUTATED')
        historical = doc['historicalBaseline']
        check('history:owner-decision', 'approved estimated reference and comparison confidence',
              historical['ownerDecision'] == '2026-10-20: retain the accepted estimated reference of 58 files / 1,460 LOC; comparison confidence remains low.',
              'SM2_MEASUREMENT_INVALID')
        check('history:bindings', 'independent historical pins', historical['history'] == [
            dict(path=path, sha256=expected) for path, expected in HISTORY], 'SM2_V1_HISTORY_MUTATED')
        check('history:SM1', '13,289', historical['sm1BaselineLoc'] == 13289
              and json.loads(chain.read(repository, HISTORY[0][0]))['plumbingBaselineLoc'] == 13289, 'SM1_BASELINE_MUTATED')
        inventory_errors, inv = fixture.check_inventory(repository, fixture.FIXTURE_ROOT)
        check('inventory:exact', '13 frozen authored sources', not inventory_errors, 'SM2_SOURCE_INVENTORY_DRIFT')
        graph_errors, _ = fixture.check_graph(repository, fixture.FIXTURE_ROOT)
        check('ownership', 'no prohibited platform ownership', not graph_errors, 'SM2_INCLUSION_RULE_VIOLATION')
        measurement = doc['measurement']
        check('measurement:definitions', 'physical and nonblank line meanings',
              measurement['physicalLocMetric'] == 'All physical lines, including blank and comment lines; a final newline adds no empty line.'
              and measurement['nonblankLocMetric'] == 'Physical lines with at least one non-whitespace character, reported separately.',
              'SM2_MEASUREMENT_INVALID')
        rows = measurement['paths']
        paths = [row.get('path') for row in rows]
        apphost_paths = [path for path in fixture.FROZEN_PATHS if '.AppHost/' in path]
        check('inclusion:AppHost', 'both AppHost authored paths count', all(
            any(row.get('path') == path and row.get('included') is True for row in rows) for path in apphost_paths),
            'SM2_APPHOST_COST_OMITTED')
        check('classification:once', 'all 13 paths classified exactly once', len(paths) == len(set(paths))
              and set(paths) == set(fixture.FROZEN_PATHS) and all(row.get('included') is True
              and row.get('category') and row.get('reason') for row in rows), 'SM2_INCLUSION_RULE_VIOLATION')
        physical = nonblank = 0
        for path in fixture.FROZEN_PATHS:
            if not (repository / path).is_file():
                continue
            content = chain.read(repository, path)
            text = content.decode('utf-8')
            # Deliberately independent of generator.splitlines() and declared proof counts.
            physical_lines = text.count('\n') + int(bool(text) and not text.endswith('\n'))
            nonblank_lines = sum(1 for line in text.split('\n') if line.strip())
            physical += physical_lines
            nonblank += nonblank_lines
            match = [row for row in rows if row.get('path') == path]
            if len(match) != 1:
                continue
            row = match[0]
            check('measurement:' + path, path, row.get('sha256') == digest(content)
                  and row.get('physicalLoc') == physical_lines and row.get('nonblankLoc') == nonblank_lines
                  and row.get('bytes') == len(content) and records.v2_committed_blob(repository, commit, path) == content,
                  'SM2_MEASUREMENT_INVALID')
            expected_category = ('AppHost' if '.AppHost/' in path else 'Contracts' if '.Contracts/' in path
                                 else 'domain and registration' if '.Server/' in path else 'module tests' if '.Tests/' in path
                                 else 'repository configuration and documentation')
            check('classification:' + path, path, row.get('category') == expected_category, 'SM2_INCLUSION_RULE_VIOLATION')
        check('measurement:totals', 'independently counted files, physical and nonblank LOC',
              (measurement['fileCount'], measurement['physicalLoc'], measurement['nonblankLoc']) == (13, physical, nonblank),
              'SM2_MEASUREMENT_INVALID')
        check('measurement:exclusions', 'generated locks, build outputs, and platform sources',
              measurement['exclusions'] == generator.EXCLUSION_POLICY
              and measurement['excludedFiles'] == [dict(**row, reason='Generated dependency lock, excluded from hand-authored file and LOC totals.')
                                                   for row in inv['lockFiles']], 'SM2_INCLUSION_RULE_VIOLATION')
        c = doc['comparison']
        check('comparison:dimensions', 'files decide; physical LOC supports',
              c['decisiveDimension'] == 'hand-authored module-owned file count' and c['supportingDimension'] == 'physical LOC',
              'SM2_MEASUREMENT_INVALID')
        reduce_files = str((Decimal(58-13)*100/58).quantize(Decimal('.01'), rounding=ROUND_HALF_UP))
        reduce_loc = str((Decimal(1460-physical)*100/1460).quantize(Decimal('.01'), rounding=ROUND_HALF_UP))
        check('comparison:arithmetic', 'reductions independently recomputed',
              c['referenceFileCount'] == 58 and c['referenceLoc'] == 1460 and c['thresholdPercentage'] == '50.00'
              and c['comparisonOperator'] == '>=' and c['evaluatedBeforeRounding'] is True
              and c['fileReductionPercentage'] == reduce_files and c['locReductionPercentage'] == reduce_loc,
              'SM2_MEASUREMENT_INVALID')
        check('comparison:confidence', 'measured high, estimated comparison low', measurement['confidence'] == 'high'
              and c['measurementConfidence'] == 'high' and c['comparisonConfidence'] == 'low'
              and c['referenceConfidence'] == 'low' and c['referenceMeasurementType'] == 'estimated'
              and c['limitations'] == generator.LIMITATIONS, 'SM2_MEASUREMENT_INVALID')
        attained = (58 - len(fixture.FROZEN_PATHS)) * 100 >= 58 * 50
        if require_oq2_decision:
            check('OQ2:decision', 'inclusive threshold evaluated before rounding',
                  c['oq2Decision'] == ('accepted' if attained else 'rejected')
                  and c['result'] == ('met-on-accepted-estimate' if attained else 'not-met-on-accepted-estimate'), 'OQ2_DECISION_INVALID')
        ss = doc['singleFileSensitivity']
        check('sensitivity', 'single-file decisions and threshold margin', ss['maximumFilesMeetingTarget'] == 29
              and ss['additionalFilesBeforeFailure'] == 16 and ss['filesAfterAddingOne'] == 14
              and ss['filesAfterRemovingOne'] == 12 and ss['addingOneFlipsAttainment'] is False
              and ss['removingOneFlipsAttainment'] is False and ss['singleFileDecisionsThatFlip'] == []
              and ss['explanation'] == 'All 13 frozen authored paths are mandatory. Including or excluding one file does not flip the measured result; the historical 29-file figure sat exactly at 50.00%.',
              'SM2_MEASUREMENT_INVALID')
        binding = chain.verify(repository, commit)
        report['assertionLedger'] += [dict(row, id='chain:' + row['id']) for row in binding['assertionLedger']]
        report['blockers'] += binding['blockers']
        contract = json.loads(chain.read(repository, chain.CONTRACT))
        expected_candidate = dict(storyId='11.3', checkpoint='C', commit=commit,
                                  planningCandidate=contract['authority']['planningCandidate'], contractPath=chain.CONTRACT,
                                  contractSha256=digest(chain.read(repository, chain.CONTRACT)),
                                  checkpoints=binding['checkpoints'], gitlinks=binding['gitlinks'])
        check('candidate:binding', 'compatible A-C chain', doc['candidate'] == expected_candidate
              and doc['provenance'] == binding['bindings'], 'CHECKPOINT_CANDIDATE_MISMATCH')
        check('command', 'exact contract generation command', doc['command'] == contract['scenarios'][0]['command'], 'EVIDENCE_FORMAT_DRIFT')
        check('tools', 'recorded generation provenance bound by the canonical payload',
              generator.valid_versions(doc['toolVersions']), 'SM2_MEASUREMENT_INVALID')
        schema = json.loads(chain.read(repository, generator.SCHEMA))
        check('schema', 'closed SM-2 v2 shape', not list(jsonschema.Draft202012Validator(schema).iter_errors(doc)), 'EVIDENCE_FORMAT_DRIFT')
        payload = {key: value for key, value in doc.items() if key != 'digests'}
        check('formats', 'canonical JSON, complete Markdown and payload digests', raw == generator.serialize(doc)
              and md == generator.render_markdown(doc) and doc['digests']['markdownSha256'] == digest(md)
              and doc['digests']['signablePayloadSha256'] == digest(fixture.canonical(payload)), 'EVIDENCE_FORMAT_DRIFT')
        report['metrics'] = dict(fileCount=13, physicalLoc=physical, nonblankLoc=nonblank,
                                 fileReductionPercentage=reduce_files, locReductionPercentage=reduce_loc,
                                 measurementConfidence='high', comparisonConfidence='low', oq2Decision=c['oq2Decision'])
    except generator.EvidenceError as error:
        report['blockers'].append(error.code)
        report['assertionLedger'].append(dict(id='tool:unavailable', subject=str(error),
                                             state='SKIPPED' if error.code in chain.current.BLOCKED_CODES else 'FAIL'))
    except fixture.Blocked:
        report['blockers'].append('HISTORY_UNAVAILABLE')
        report['assertionLedger'].append(dict(id='history', subject='committed candidate', state='SKIPPED'))
    except OSError:
        report['blockers'].append('HISTORY_UNAVAILABLE')
        report['assertionLedger'].append(dict(id='input', subject='evidence source unavailable', state='SKIPPED'))
    except (ValueError, KeyError, TypeError, AttributeError, UnicodeError):
        check('format:malformed', 'SM-2 evidence', False, 'EVIDENCE_FORMAT_DRIFT')
    report['blockers'] = sorted(set(report['blockers']))
    report['result'] = ('BLOCKED' if any(code in chain.current.BLOCKED_CODES for code in report['blockers'])
                        else 'FAIL' if report['blockers'] else 'PASS')
    return report


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(allow_abbrev=False)
    parser.add_argument('--repository', required=True)
    parser.add_argument('--evidence', required=True)
    parser.add_argument('--markdown', required=True)
    parser.add_argument('--require-oq2-decision', action='store_true')
    parser.add_argument('--output')
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    result = verify(repository, args.evidence, args.markdown, args.require_oq2_decision)
    if args.output:
        if args.output != 'artifacts/v9/11.3/AC-11.3-04.json':
            result['result'] = 'FAIL'
            result['blockers'] = sorted(set(result['blockers'] + ['EVIDENCE_FORMAT_DRIFT']))
        else:
            target = records.v2_output_target(repository, args.output)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(generator.serialize(result))
    print(json.dumps(result, indent=2))
    return {'PASS': 0, 'FAIL': 1, 'BLOCKED': 2}[result['result']]


if __name__ == '__main__':
    sys.exit(main())
