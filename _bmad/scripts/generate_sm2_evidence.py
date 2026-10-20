#!/usr/bin/env python3
"""Measure committed fixture bytes and generate deterministic Story 11.3 SM-2 evidence."""
from __future__ import annotations

import argparse
from decimal import Decimal, ROUND_HALF_UP
import importlib.metadata
import json
from pathlib import Path
import platform
import subprocess
import sys

import jsonschema

import evidence_boundary_current as current
import generate_story_record as records
import verify_minimal_module_fixture as fixture
import verify_story_candidate as chain

SCHEMA_VERSION = 'hexalith.conversations.minimal-module-authoring-cost-sm2.v2'
SCHEMA = 'docs/release-evidence/minimal-module-authoring-cost-sm2-v2.schema.json'
JSON_PATH = 'docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json'
MARKDOWN_PATH = 'docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md'
BASELINE = 'docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json'
BASELINE_MD = 'docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md'
SM1 = 'docs/release-evidence/consume-promote-keep-inventory-v1.json'
INTERPRETATION = 'docs/release-evidence/oq-2-target-interpretation-decision-v1.json'
HISTORY_PINS = {
    SM1: '20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7',
    BASELINE: '14de3c86628cb4f900008be42d086a7699ae0869b3b791f92c810ab921192e03',
    BASELINE_MD: '1cb37b3ddbc9186748c7556a87e5c08feb15d8dee30f4a1b0362bcd5536297fd',
    INTERPRETATION: '06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06',
}
LIMITATIONS = [
    'Attainment is against the owner-accepted historical estimate of 58 files and 1,460 LOC; comparison confidence remains low.',
    'The reference is an estimate, not a reconstructed, compiled pre-initiative fixture; no reference fixture is introduced.',
    'Fixture physical LOC includes blank and comment lines. Historical LOC represents estimated nonblank logical skeleton lines; the LOC reduction is indicative and is not a like-for-like physical LOC comparison.',
    'File reduction alone decides OQ-2 at the inclusive 50% threshold before rounding; LOC is supporting evidence only.',
]
EXCLUSION_POLICY = [
    dict(pattern='**/bin/**; **/obj/**', reason='Generated build outputs are not hand-authored module-owned files.'),
    dict(pattern='**/packages.lock.json', reason='NuGet-generated dependency locks are retained and hash-bound by Checkpoint B, excluded from authored cost.'),
    dict(pattern='references/**', reason='Shared platform libraries and runtime boilerplate are platform-owned, outside the fixture.'),
]


class EvidenceError(Exception):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code


def percentage(reference: int, measured: int) -> str:
    return str(((Decimal(reference) - measured) * 100 / reference).quantize(Decimal('0.01'), rounding=ROUND_HALF_UP))


def category(path: str) -> str:
    if '.AppHost/' in path:
        return 'AppHost'
    if '.Contracts/' in path:
        return 'Contracts'
    if '.Server/' in path:
        return 'domain and registration'
    if '.Tests/' in path:
        return 'module tests'
    return 'repository configuration and documentation'


def measure(repository: Path, commit: str) -> dict:
    errors, inventory = fixture.check_inventory(repository, fixture.FIXTURE_ROOT)
    if errors:
        raise EvidenceError('SM2_SOURCE_INVENTORY_DRIFT', 'the authored inventory differs from the frozen 13 paths')
    graph_errors, _ = fixture.check_graph(repository, fixture.FIXTURE_ROOT)
    if graph_errors:
        raise EvidenceError('SM2_INCLUSION_RULE_VIOLATION', 'the module owns a prohibited capability or violates the AppHost boundary')
    rows = []
    for path in fixture.FROZEN_PATHS:
        content = chain.read(repository, path)
        if records.v2_committed_blob(repository, commit, path) != content:
            raise EvidenceError('SM2_MEASUREMENT_INVALID', 'source bytes differ from the committed candidate: ' + path)
        lines = content.decode('utf-8').splitlines()
        rows.append(dict(path=path, sha256=current.digest(content), bytes=len(content), physicalLoc=len(lines),
                         nonblankLoc=sum(bool(line.strip()) for line in lines), category=category(path),
                         included=True, reason='Hand-authored module-owned source; repository-only test AppHost counts in full.'
                         if '.AppHost/' in path else 'Hand-authored module-owned source, configuration, or documentation.'))
    return dict(root=fixture.FIXTURE_ROOT, inventoryId=fixture.INVENTORY_ID, inventorySha256=fixture.INVENTORY_SHA256,
                paths=rows, fileCount=len(rows), physicalLoc=sum(row['physicalLoc'] for row in rows),
                nonblankLoc=sum(row['nonblankLoc'] for row in rows), confidence='high',
                physicalLocMetric='All physical lines, including blank and comment lines; a final newline adds no empty line.',
                nonblankLocMetric='Physical lines with at least one non-whitespace character, reported separately.',
                exclusions=EXCLUSION_POLICY,
                excludedFiles=[dict(**row, reason='Generated dependency lock, excluded from hand-authored file and LOC totals.')
                               for row in inventory['lockFiles']])


def comparison(files: int, physical_loc: int) -> dict:
    attained = 2 * files <= 58
    return dict(referenceFileCount=58, referenceLoc=1460, referenceMeasurementType='estimated',
                referenceConfidence='low', measurementConfidence='high', comparisonConfidence='low',
                decisiveDimension='hand-authored module-owned file count', supportingDimension='physical LOC',
                thresholdPercentage='50.00', comparisonOperator='>=', evaluatedBeforeRounding=True,
                fileReductionPercentage=percentage(58, files), locReductionPercentage=percentage(1460, physical_loc),
                oq2Decision='accepted' if attained else 'rejected',
                result='met-on-accepted-estimate' if attained else 'not-met-on-accepted-estimate', limitations=LIMITATIONS)


def sensitivity(files: int) -> dict:
    attained = 2 * files <= 58
    return dict(maximumFilesMeetingTarget=29, additionalFilesBeforeFailure=max(0, 29-files),
                filesAfterAddingOne=files+1, filesAfterRemovingOne=max(0, files-1),
                addingOneFlipsAttainment=attained != (2*(files+1) <= 58),
                removingOneFlipsAttainment=attained != (2*max(0, files-1) <= 58),
                singleFileDecisionsThatFlip=[],
                explanation='All 13 frozen authored paths are mandatory. Including or excluding one file does not flip the measured result; the historical 29-file figure sat exactly at 50.00%.')


def versions(repository: Path) -> dict:
    tools = {}
    for name, args in (('dotnetSdk', ['dotnet', '--version']), ('git', ['git', '--version'])):
        result = subprocess.run(args, cwd=repository, capture_output=True, text=True, timeout=60, check=False)
        if result.returncode or not result.stdout.strip():
            raise EvidenceError('HISTORY_UNAVAILABLE', 'tool version unavailable: ' + name)
        tools[name] = result.stdout.strip()
    return dict(python=platform.python_version(), jsonschema=importlib.metadata.version('jsonschema'), **tools)


def build_signable(repository: Path, *, candidate: str = 'HEAD') -> dict:
    commit = fixture.resolve_commit(repository, candidate)
    measurement = measure(repository, commit)
    for path, digest in HISTORY_PINS.items():
        if current.digest(chain.read(repository, path)) != digest or current.digest(records.v2_committed_blob(repository, commit, path) or b'') != digest:
            raise EvidenceError('SM1_BASELINE_MUTATED' if path == SM1 else 'SM2_V1_HISTORY_MUTATED', path)
    candidate_result = chain.verify(repository, commit)
    if candidate_result['result'] != 'PASS':
        raise EvidenceError(candidate_result['blockers'][0], 'checkpoint chain does not verify')
    contract = json.loads(chain.read(repository, chain.CONTRACT))
    return dict(schemaVersion=SCHEMA_VERSION,
                candidate=dict(storyId='11.3', checkpoint='C', commit=commit,
                               planningCandidate=contract['authority']['planningCandidate'],
                               contractPath=chain.CONTRACT, contractSha256=current.digest(chain.read(repository, chain.CONTRACT)),
                               checkpoints=candidate_result['checkpoints'], gitlinks=candidate_result['gitlinks']),
                provenance=candidate_result['bindings'], historicalBaseline=dict(
                    ownerDecision='2026-10-20: retain the accepted estimated reference of 58 files / 1,460 LOC; comparison confidence remains low.',
                    sm1BaselineLoc=13289, history=[dict(path=path, sha256=digest) for path, digest in HISTORY_PINS.items()]),
                measurement=measurement, comparison=comparison(measurement['fileCount'], measurement['physicalLoc']),
                singleFileSensitivity=sensitivity(measurement['fileCount']), toolVersions=versions(repository),
                command=contract['scenarios'][0]['command'])


def serialize(document: dict) -> bytes:
    return (json.dumps(document, indent=2, ensure_ascii=False) + '\n').encode()


def render_markdown(document: dict) -> bytes:
    # A complete canonical payload is included, making every field reviewable and format parity exact.
    payload = {key: value for key, value in document.items() if key != 'digests'}
    m, c = document['measurement'], document['comparison']
    lines = ['# Minimal-module authoring cost — SM-2 v2', '',
             f"Candidate: `{document['candidate']['commit']}`. Checkpoint C.", '',
             f"Measured: **{m['fileCount']} files**, **{m['physicalLoc']} physical LOC**, **{m['nonblankLoc']} nonblank LOC**.",
             f"Reference: **58 estimated files / 1,460 estimated LOC**. File reduction **{c['fileReductionPercentage']}%**; indicative LOC reduction **{c['locReductionPercentage']}%**.",
             f"OQ-2: `{c['oq2Decision']}` — `{c['result']}`. Measurement confidence `high`; comparison confidence `low`.", '',
             *['- ' + item for item in c['limitations']], '',
             f"Signable payload: `{document['digests']['signablePayloadSha256']}`", '',
             '## Complete measured payload', '', '```json', json.dumps(payload, indent=2, ensure_ascii=False), '```', '']
    return '\n'.join(lines).encode()


def finalize(signable: dict) -> tuple[bytes, bytes]:
    document = {**signable, 'digests': dict(signablePayloadSha256=current.digest(current.canonical(signable)), markdownSha256='')}
    markdown = render_markdown(document)
    document['digests']['markdownSha256'] = current.digest(markdown)
    return serialize(document), markdown


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(allow_abbrev=False)
    for option in ('repository', 'contract', 'fixture-proof', 'baseline', 'output-json', 'output-markdown'):
        parser.add_argument('--' + option, required=True)
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    try:
        if [args.contract, args.fixture_proof, args.baseline, args.output_json, args.output_markdown] != [
                chain.CONTRACT, fixture.PROOF_JSON_PATH, BASELINE, JSON_PATH, MARKDOWN_PATH]:
            raise EvidenceError('EVIDENCE_FORMAT_DRIFT', 'the closed Story 11.3 input/output paths are required')
        j, m = finalize(build_signable(repository))
        jsonschema.Draft202012Validator(json.loads(chain.read(repository, SCHEMA))).validate(json.loads(j))
        records.v2_write_outputs([(records.v2_output_target(repository, JSON_PATH), j),
                                  (records.v2_output_target(repository, MARKDOWN_PATH), m)])
    except EvidenceError as error:
        print(f'{error.code}: {error}', file=sys.stderr)
        return 2 if error.code in current.BLOCKED_CODES else 1
    except (fixture.Blocked, OSError, subprocess.TimeoutExpired) as error:
        print(f'HISTORY_UNAVAILABLE: {error}', file=sys.stderr)
        return 2
    except (ValueError, KeyError, TypeError, jsonschema.ValidationError) as error:
        print(f'SM2_MEASUREMENT_INVALID: {error}', file=sys.stderr)
        return 1
    print('PASS')
    return 0


if __name__ == '__main__':
    sys.exit(main())
