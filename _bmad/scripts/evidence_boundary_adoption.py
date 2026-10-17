"""Story 10.4 current-policy adoption boundary. Historical 10.3 consumption is independent."""
from __future__ import annotations
import argparse
import base64
import json
import re
from pathlib import Path
import subprocess
import sys
from typing import Any
import evidence_boundary_current as current
from config_utils import load_customization

BASELINE = '1915a47123737859e3ac31872fa787fa8c0bd1e3'
BINDING = '_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json'
CONTRACT = '_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json'
SPEC = '_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md'
SNAPSHOT = 'artifacts/v9/10.4/reader-entry-v1.json'
# Independent consuming-source trust roots, never supplied by the manifest being checked.
SNAPSHOT_SHA256 = 'a90a2cc9a0fcfdeec6b77a760cbcd68ac7deffb79302273f577bc5eba743265f'
# The sole owner-approved assertion change is explicit and independent of the snapshot/binding.
APPROVED_ASSERTION_CORRECTIONS = [dict(
    path='tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs',
    method='SubmodulePathsShouldLiveUnderReferences', decisionDate='2026-10-17',
    before='submodulePaths.Length.ShouldBe(10);', after='submodulePaths.Length.ShouldBe(12);', count=1)]
# The exact owner amendment is independently pinned; it is never a caller-defined allowlist.
APPROVED_GITLINK_AMENDMENT = {
    "decisionDate": "2026-10-17",
    "approvalCommit": "9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5",
    "transitions": [
        {
            "path": "references/Hexalith.Builds",
            "sourceMode": "160000",
            "sourceObject": "abdfca89ed82f388b54664825a8a79fed73e7ccf",
            "destinationMode": "160000",
            "destinationObject": "4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77"
        },
        {
            "path": "references/Hexalith.EventStore",
            "sourceMode": "160000",
            "sourceObject": "6cc9d6114fd055dac7a1d86130208b52271e3740",
            "destinationMode": "160000",
            "destinationObject": "77d9af18a54d92506e310fe5a45021a395e66f60"
        },
        {
            "path": "references/Hexalith.Folders",
            "sourceMode": "160000",
            "sourceObject": "92e87359dd50dc9644e490a04de98adaaade9a86",
            "destinationMode": "160000",
            "destinationObject": "42a2308113e1bdb9b1d07a778b4405dde797ebd3"
        },
        {
            "path": "references/Hexalith.FrontComposer",
            "sourceMode": "160000",
            "sourceObject": "0129bd39095892283371f97836ade5768fef3809",
            "destinationMode": "160000",
            "destinationObject": "b102b85b148f21fb26d5742e120c344109df7084"
        },
        {
            "path": "references/Hexalith.McpCli",
            "sourceMode": "160000",
            "sourceObject": "0b96dfdf2ff6037be88f1fb952d6581a78930e67",
            "destinationMode": "160000",
            "destinationObject": "b35b0975a234f1f97e2e35e9dad381d0a988f4af"
        },
        {
            "path": "references/Hexalith.Memories",
            "sourceMode": "160000",
            "sourceObject": "f7c8ce84a8a765667beafc31154ee2d6981552f8",
            "destinationMode": "160000",
            "destinationObject": "f6b60d5a2e9b81d84fddad789c7d9bed40d3a533"
        },
        {
            "path": "references/Hexalith.Parties",
            "sourceMode": "160000",
            "sourceObject": "c0946ef17b3641fb23f6f17614913bfbab687373",
            "destinationMode": "160000",
            "destinationObject": "90af3047b50046cb4f6f91e01f182f562110aab3"
        },
        {
            "path": "references/Hexalith.Platform",
            "sourceMode": "160000",
            "sourceObject": "b2890d20575356239555df5b113e3100c1d594c1",
            "destinationMode": "160000",
            "destinationObject": "b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b"
        },
        {
            "path": "references/Hexalith.Tenants",
            "sourceMode": "160000",
            "sourceObject": "e44ac7f537e9f192605e24efc3e61a97d00d44f3",
            "destinationMode": "160000",
            "destinationObject": "e667d5391dae306e631dec23acd191863e54130c"
        }
    ]
}
PREDECESSOR_SHA256 = '013416c7f819aafcad2528d4b6892a05707b5f3056830ee550f174048a9929d1'
PREDECESSOR_CANDIDATE = '6908c5264c388cca1b094400c5abfa320482c5e3'
READERS = '_bmad-output/planning-artifacts/v9/inventories/evidence-readers-v1.json'
RUNBOOK = 'docs/runbooks/evidence-boundary-validation.md'
GUIDANCE = (*current.GUIDANCE, RUNBOOK)
WORKFLOWS = current.WORKFLOWS
PROVENANCE = (READERS, '_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json',
    '_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json',
    '_bmad-output/planning-artifacts/v9/story-contracts/10.4.json', '_bmad-output/planning-artifacts/v9-authority-bundle-v1.json',
    'docs/release-evidence/story-10.3-final-record-v2.json', 'docs/release-evidence/story-10.3-final-record-v2.md')
ROOTS = dict(sourcePath=current.CURRENT_PROVENANCE, constants=dict(Story103RecordSha256=PREDECESSOR_SHA256,
    Story103CandidateCommit=PREDECESSOR_CANDIDATE), allowlist=[])
FAIL_CODES = current.FAIL_CODES | frozenset({'EVIDENCE_READER_INVENTORY_DRIFT','EVIDENCE_HELPER_NOT_USED',
    'ASSERTION_STRENGTH_WEAKENED','DAY_ONE_EXEMPTION_FORBIDDEN','GIT_RUNNER_DEADLOCK','PROJECTION_PROOF_SCOPE_ABSORBED',
    'EVIDENCE_RUNBOOK_INVALID','EVIDENCE_GUIDANCE_NOT_USED','EVIDENCE_GUIDANCE_DRIFT',
    'EVIDENCE_CHAIN_MARKER_INVALID','EVIDENCE_CHAIN_TABLE_INVALID','PROMOTION_GATE_SPAN_WEAKENED'})
STABLE_CODES = FAIL_CODES | current.BLOCKED_CODES | current.WARNING_CODES
APPROVED_SOURCE_PATHS = ('.gitattributes', '_bmad-output/implementation-artifacts/spec-10-4-migrate-frozen-readers-repair-gate-spans-publish-the-runbook-and-prove-fault-injection.md', '_bmad-output/implementation-artifacts/sprint-status.yaml', '_bmad-output/planning-artifacts/story-10.4-current-binding-v1.json', '_bmad-output/planning-artifacts/story-10.4-current-contract-v1.json', '_bmad/custom/bmad-build-auto.toml', '_bmad/custom/bmad-build.toml', '_bmad/custom/bmad-review.toml', '_bmad/schemas/story-final-record-v2.schema.json', '_bmad/scripts/evidence_boundary_adoption.py', '_bmad/scripts/evidence_boundary_current.py', '_bmad/scripts/generate_story_record.py', '_bmad/scripts/tests/test_generate_story_record.py', '_bmad/scripts/tests/test_verify_evidence_boundary.py', '_bmad/scripts/tests/test_verify_submodule_promotion.py', '_bmad/scripts/verify_conformance_tiering.py', '_bmad/scripts/verify_evidence_boundary.py', 'artifacts/v9/10.4/reader-entry-v1.json', 'docs/runbooks/evidence-boundary-validation.md', 'tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj', 'tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/EvidenceBoundaryAdoptionValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/FinalConformanceContractDiffEvidenceValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj', 'tests/Hexalith.Conversations.Conformance.Tests/OqTwoTargetInterpretationDecisionValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/OracleBlindSpotAnalysisArtifactGenerationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/RemovedTestJustificationLedgerReconciliationValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs', 'tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Documentation/MinimalModuleAuthoringCostBaselineValidationTest.cs', 'tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj', 'tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj', 'tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs', 'tests/Hexalith.Conversations.TestSupport.Runner/Program.cs', 'tests/Hexalith.Conversations.TestSupport.Tests/AdoptionSeamTests.cs', 'tests/Hexalith.Conversations.TestSupport/BoundaryAssertions.cs', 'tests/Hexalith.Conversations.TestSupport/EvidenceManifest.cs', 'tests/Hexalith.Conversations.TestSupport/GitFacts.cs', 'tests/Hexalith.Conversations.TestSupport/RepositoryLocator.cs', 'tests/tooling/test_evidence_boundary_adoption.py', 'tests/tooling/test_evidence_boundary_current.py')
SECTIONS = ('Invariants','Authoring','Exemptions','Fault injection','Known limitations')
GUIDANCE_CLAUSE = ('Use the canonical evidence-boundary runbook for explicitly required evidence work: '
    'recomputed hashes, contained paths, canonical signable payload, exact changed-path equality, raw-mode gitlink exclusion, '
    'exact inventory identity, root-of-trust pinning, and anti-vacuity. Invoke the effective scoped verifier; '
    'preserve PASS/FAIL/BLOCKED/not-applicable distinctly. FAIL and BLOCKED halt review and completion.')


def record(result, identity, subject, passed, code):
    if code not in STABLE_CODES: raise ValueError('unregistered code: '+code)
    result['assertionLedger'].append(dict(id=identity,subject=subject,state='PASS' if passed else 'FAIL'))
    if not passed: result['blockers'].append(code)


def finish(result, applicable=True):
    return current.finalize(result, applicable=applicable)


def snapshot(repository):
    content = (repository/SNAPSHOT).read_bytes()
    if current.digest(content) != SNAPSHOT_SHA256: raise ValueError('entry snapshot trust root differs')
    value = json.loads(content)
    if value['baseline'] != BASELINE: raise ValueError('entry baseline differs')
    return value


def reader_checks(repository, result, binding, *, historical=True):
    """Reconstruct the exact approved migration, rather than comparing counts of assertions."""
    entry_snapshot = snapshot(repository)
    entries = entry_snapshot['readers']
    record(result,'readers:approved-correction',SNAPSHOT,
        entry_snapshot.get('approvedAssertionCorrections')==binding.get('approvedAssertionCorrections')==APPROVED_ASSERTION_CORRECTIONS,
        'ASSERTION_STRENGTH_WEAKENED')
    inventory = json.loads((repository/READERS).read_bytes())
    expected = [row['path'] for row in inventory['rows']]
    paths = [row['path'] for row in entries]
    record(result,'readers:inventory',READERS,len(paths)==27 and paths==expected and len(set(paths))==27,
        'EVIDENCE_READER_INVENTORY_DRIFT')
    record(result,'readers:binding',BINDING,binding['readerInventory']['paths']==expected
        and binding['readerInventory']['id']==inventory['inventoryId']
        and binding['readerInventory']['sha256']==inventory['inventoryDigest'],'EVIDENCE_READER_INVENTORY_DRIFT')
    before = {}; containment = {}
    if historical:
        facts = current.bridge(repository,dict(baseline=BASELINE,candidate=BASELINE,blobPaths=paths,containedPaths=paths))
        merge(result,facts,'entry:')
        before = {row['path']:row for row in facts['blobs']}
        containment = {row['path']:row for row in facts['containedPaths']}
    for row in entries:
        path=row['path']; original=base64.b64decode(row['entryBase64'],validate=True)
        entry_ok = current.digest(original)==row['entrySha256'] and (not historical
            or before.get(path,{}).get('sha256')==row['entrySha256'])
        record(result,'reader:entry:'+path,path,entry_ok,'ASSERTION_STRENGTH_WEAKENED')
        text=original.decode().replace('\r\n','\n')
        for operation in row['substitutions']:
            if text.count(operation['before']) != operation['count']: raise ValueError('ambiguous plumbing substitution')
            text=text.replace(operation['before'],operation['after'])
        for correction in APPROVED_ASSERTION_CORRECTIONS:
            if correction['path'] != path: continue
            if text.count(correction['before']) != correction['count']: raise ValueError('ambiguous approved assertion correction')
            text=text.replace(correction['before'],correction['after'])
        safe = not historical or not containment[path]['code']
        actual = (repository/path).read_bytes() if safe and (repository/path).is_file() else b''
        migrated = text.replace('\n','\r\n').encode() if row['substitutions'] else original
        record(result,'reader:strength:'+path,path,actual==migrated and current.digest(actual)==row['afterSha256'],
            'ASSERTION_STRENGTH_WEAKENED')
        if row['substitutions']:
            adopted= bool(re.search(rb'(?<![A-Za-z0-9_])(?:RepositoryLocator|EvidenceManifest|GitFacts)\.',actual))
        else:
            # These are data/collection declarations, not I/O readers. Their unchanged bytes are
            # consumed by migrated owners; requiring dummy helper construction would be vacuous.
            consumers=row['indirectConsumers']; adopted=bool(consumers)
            for consumer in consumers:
                matches=[entry for entry in entries if entry['path'].endswith('/'+consumer)]
                adopted &= len(matches)==1 and (repository/matches[0]['path']).is_file()
            if path.endswith('ContractSamples.cs'):
                adopted &= all(b'ContractSamples.' in (repository/entry['path']).read_bytes()
                    for entry in entries if any(entry['path'].endswith('/'+name) for name in consumers))
            else:
                adopted &= b'[Collection(ReleaseEvidenceArtifactCollection.Name)]' in (repository/expected[5]).read_bytes()
                adopted &= b'DisableParallelization = true' in (repository/expected[14]).read_bytes()
                adopted &= b'RepositoryLocator.' in (repository/expected[7]).read_bytes()
        record(result,'reader:helper:'+path,path,adopted,'EVIDENCE_HELPER_NOT_USED')
    for project in ('Conformance.Tests','Conformance.Portable.Tests','Contracts.Tests','IntegrationTests'):
        path=f'tests/Hexalith.Conversations.{project}/Hexalith.Conversations.{project}.csproj'
        record(result,'reader:project:'+project,path,'../Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj'
            in (repository/path).read_text(),'EVIDENCE_HELPER_NOT_USED')
    record(result,'readers:exemptions',BINDING,binding.get('exemptions')==[],'DAY_ONE_EXEMPTION_FORBIDDEN')
    projection=next(row for row in entries if row['path'].endswith('/ProjectionReadStorePopulationProofValidationTest.cs'))
    record(result,'projection:scope',projection['path'],all(op['kind'].startswith(('method:','read','hash','using'))
        for op in projection['substitutions']),'PROJECTION_PROOF_SCOPE_ABSORBED')


def guidance_checks(repository,result,binding):
    """Verify effective overrides and both-tree deterministic render consumption."""
    for name in ('bmad-build','bmad-build-auto','bmad-review'):
        path=f'_bmad/custom/{name}.toml'; installed=repository/path
        record(result,'guidance:present:'+name,path,installed.is_file(),'EVIDENCE_GUIDANCE_NOT_USED')
        if not installed.is_file():continue
        resolved=load_customization(repository,repository/'.agents/skills'/name)
        material=current.canonical(resolved).decode()
        record(result,'guidance:content:'+name,path,GUIDANCE_CLAUSE in material and 'file:{project-root}/'+RUNBOOK in material,
            'EVIDENCE_GUIDANCE_DRIFT')
        record(result,'guidance:resolved:'+name,path,current.digest(current.canonical(resolved))==binding['resolvedCustomization'][name],
            'EVIDENCE_GUIDANCE_DRIFT')
        if name!='bmad-review':
            renderer=current.completion.load_renderer()
            for tree in current.completion.SKILL_TREES:
                rendered=current.completion.render_in_memory(renderer,repository,repository/tree/name)
                rendered2=current.completion.render_in_memory(renderer,repository,repository/tree/name)
                record(result,'guidance:render:'+tree+':'+name,name,rendered==rendered2,'EVIDENCE_GUIDANCE_DRIFT')
                # The effective customization is part of the workflow's activation content.
                workflow=rendered['workflow.md']
                record(result,'guidance:render-used:'+tree+':'+name,name,GUIDANCE_CLAUSE in workflow and RUNBOOK in workflow,
                    'EVIDENCE_GUIDANCE_NOT_USED')
    runbook=repository/RUNBOOK;text=runbook.read_text() if runbook.is_file() else ''
    record(result,'runbook:sections',RUNBOOK,all('\n## '+section+'\n' in text for section in SECTIONS),'EVIDENCE_RUNBOOK_INVALID')
    required=('exact set equality','160000','nonempty','9/9/0/0/0/0','--current-binding '+BINDING,
        '-k complete_fault_matrix','-k evidence_gate_span_coupling','--verify-inserted-record',
        'EVIDENCE_HELPER_NOT_USED','ASSERTION_STRENGTH_WEAKENED','EVIDENCE_GUIDANCE_NOT_USED','EVIDENCE_GUIDANCE_DRIFT',
        'PASS','FAIL','BLOCKED','not-applicable','does not add CI wiring','historical')
    record(result,'runbook:contract',RUNBOOK,all(item in text for item in required),'EVIDENCE_RUNBOOK_INVALID')
    for row in binding['guidanceInventory']['rows']:
        path=repository/row['path'];record(result,'guidance:hash:'+row['path'],row['path'],path.is_file()
            and current.digest(path.read_bytes())==row['sha256'],'EVIDENCE_GUIDANCE_DRIFT')


def merge(result,facts,prefix=''):
    result['assertionLedger'].extend(dict(row,id=prefix+row['id']) for row in facts['assertionLedger'])
    result['blockers'].extend(code if code in STABLE_CODES else 'SCOPE_NOT_EVALUATED' if code=='ASSERTION_LEDGER_EMPTY' else 'HELPER_FAILURE'
        for code in facts['blockers'] if facts['result']!='BLOCKED' or code in current.BLOCKED_CODES)


def candidate_gitlinks(baseline_roots):
    """Retain all twelve original roots and apply only the independently approved endpoints."""
    transitions={row['path']:row for row in APPROVED_GITLINK_AMENDMENT['transitions']}
    return [dict(row,commit=transitions[row['path']]['destinationObject']) if row['path'] in transitions else dict(row)
        for row in baseline_roots]


def gitlink_checks(result,binding,baseline_roots,candidate_roots,diff):
    """Require exact amendment metadata, raw transitions, baseline provenance and unchanged roots."""
    record(result,'gitlinks:amendment',BINDING,binding.get('approvedGitlinkAmendment')==APPROVED_GITLINK_AMENDMENT,
        'EVIDENCE_BOUNDARY_GITLINK')
    transitions=APPROVED_GITLINK_AMENDMENT['transitions']
    actual=[{key:row[key] for key in ('path','sourceMode','sourceObject','destinationMode','destinationObject')}
        for row in diff if '160000' in (row['sourceMode'],row['destinationMode'])]
    raw_ok=actual==transitions and all(row.get('sourcePath') is None and row.get('status')=='M'
        for row in diff if '160000' in (row['sourceMode'],row['destinationMode']))
    original={row['path']:row for row in baseline_roots}
    record(result,'gitlinks:transitions','nine exact mode-160000 transitions',raw_ok and all(
        original.get(row['path'])==dict(path=row['path'],mode=row['sourceMode'],commit=row['sourceObject']) for row in transitions),
        'EVIDENCE_BOUNDARY_GITLINK')
    record(result,'gitlinks:provenance','twelve baseline root gitlinks',len(baseline_roots)==12
        and baseline_roots==binding['rootGitlinks'],'EVIDENCE_BOUNDARY_GITLINK')
    record(result,'gitlinks:candidate','nine approved and three unchanged roots',candidate_roots==candidate_gitlinks(baseline_roots),
        'EVIDENCE_BOUNDARY_GITLINK')
    if all(row['state']=='PASS' for row in result['assertionLedger']):
        # The inherited schema's `gitlinks` field lists prohibited raw transitions.
        # The exact approved set remains in changedPaths, these measured assertions,
        # the independent compiled checker and the closed final-record amendment.
        result['gitlinks']=[]


def verify(repository,baseline,candidate,*,authority_bundle,binding_path=BINDING,verify_workflows=False,check=None):
    result=current.empty_result()
    try:
        initial=current.bridge(repository,dict(baseline=baseline,candidate=candidate,containedPaths=[binding_path,authority_bundle],includeRootGitlinks=True,
            story104GitlinkAmendment=APPROVED_GITLINK_AMENDMENT))
        merge(result,initial,'initial:');result['candidate']=initial['candidate']
        if initial['result']=='BLOCKED':return finish(result)
        if any(row['code'] for row in initial['containedPaths']):return finish(result)
        binding=json.loads((repository/binding_path).read_bytes())
        record(result,'binding:baseline',binding_path,baseline==binding['baseline']==BASELINE and binding['storyId']=='10.4','AUTHORITY_BINDING_INVALID')
        entries=initial['diff'] or []; paths=sorted({entry['path'] for entry in entries}|{entry['sourcePath'] for entry in entries if entry['sourcePath']})
        result['changedPaths']=paths
        result['gitlinks']=[dict(path=row['path'],sourceMode=row['sourceMode'],destinationMode=row['destinationMode'])
            for row in entries if '160000' in (row['sourceMode'],row['destinationMode'])]
        applicable=verify_workflows or check is not None or bool(set(paths)&set(binding['sourcePaths']))
        if not applicable and not result['blockers']:return finish(result,False)
        record(result,'binding:exemptions',binding_path,binding.get('exemptions')==[],'DAY_ONE_EXEMPTION_FORBIDDEN')
        record(result,'roots:independent',ROOTS['sourcePath'],binding['rootConstants']==ROOTS,'ROOT_OF_TRUST_NOT_PINNED')
        base=current.bridge(repository,dict(baseline=BASELINE,candidate=BASELINE,blobPaths=list(PROVENANCE)+list(WORKFLOWS),includeRootGitlinks=True))
        merge(result,base,'baseline:')
        gitlink_checks(result,binding,base['rootGitlinks'],initial['rootGitlinks'],entries)
        baseline_blobs={row['path']:row for row in base['blobs']}
        record(result,'provenance:membership',BINDING,[row['path'] for row in binding['provenance']]==list(PROVENANCE),'AUTHORITY_BINDING_INVALID')
        for row in binding['provenance']:
            record(result,'provenance:'+row['path'],row['path'],row['sha256']==baseline_blobs.get(row['path'],{}).get('sha256'),'EVIDENCE_HASH_MISMATCH')
        for name,expected_members in (('workflowInventory',WORKFLOWS),('guidanceInventory',GUIDANCE)):
            inventory=binding[name];rows=inventory['rows']
            record(result,'inventory:'+name,name,len(rows)==len(expected_members)
                and [row['path'] for row in rows]==list(expected_members)
                and inventory['sha256']==current.digest(current.canonical(rows)),'AUTHORITY_BINDING_INVALID')
        for row in binding['workflowInventory']['rows']:
            record(result,'workflow:entry:'+row['path'],row['path'],row['sha256']==baseline_blobs[row['path']]['sha256'],'EVIDENCE_HASH_MISMATCH')
        manifest=binding['provenance']+binding['workflowInventory']['rows']+binding['guidanceInventory']['rows']
        manifest += [dict(path=SNAPSHOT,sha256=SNAPSHOT_SHA256,role='entry-snapshot')]
        expected=binding['expectedChangedPaths']
        record(result,'binding:exact-sources',BINDING,binding['sourcePaths']==list(APPROVED_SOURCE_PATHS)
            and expected==sorted(set(APPROVED_SOURCE_PATHS)|{row['path'] for row in APPROVED_GITLINK_AMENDMENT['transitions']}),'AUTHORITY_BINDING_INVALID')
        import generate_story_record as generator
        # Publication adds only the bound receipt/pair set and checked lifecycle changes.
        pair=repository/'docs/release-evidence/story-10.4-final-record-v2.json'
        if pair.is_file():
            published=json.loads(pair.read_bytes());source=published['candidate']['commit']
            if source != initial['candidate']:
                problems=generator.v2_10_4_source_errors(repository,published,initial['candidate'])
                problems+=generator.v2_10_4_descendant_errors(repository,published,initial['candidate'])
                record(result,'publication:verified','10.4',not problems,'AUTHORITY_BINDING_INVALID')
                if not problems:expected=sorted(set(expected)|set(generator.v2_10_4_publication_paths(published))|({SPEC} if SPEC in paths else set()))
        payload=''.join(f'{row["path"]}\t{row["sha256"]}\t{row.get("role","provenance")}\n' for row in sorted(manifest,key=lambda row:row['path']))
        facts=current.bridge(repository,dict(baseline=baseline,candidate=initial['candidate'],expectedPaths=expected,
            roots=ROOTS,manifest=manifest,signablePayloadHash=current.digest(payload.encode()),
            story104GitlinkAmendment=APPROVED_GITLINK_AMENDMENT,
            blobPaths=list(dict.fromkeys([row['path'] for row in manifest]+binding['sourcePaths'])),containedPaths=binding['sourcePaths']))
        merge(result,facts,'boundary:');result['manifest']=manifest
        declared={row['path']:row['sha256'] for row in manifest}
        contained={row['path']:row for row in facts['containedPaths']}
        for row in facts['blobs']:
            path=row['path']; expected_hash=declared.get(path)
            if expected_hash is not None:
                valid=row['sha256']==expected_hash
            else:
                item=contained[path];target=Path(item['fullPath']) if item['fullPath'] else None
                valid=not item['code'] and target is not None and target.is_file() and current.digest(target.read_bytes())==row['sha256']
                if not valid and path==SPEC and pair.is_file() and target is not None and target.is_file():
                    valid=generator.v2_working_spec_lifecycle_only_change(repository,initial['candidate'],path,target.read_bytes(),record_region=True,task_checkboxes=True)
            record(result,'candidate:blob:'+path,path,valid,'EVIDENCE_HASH_MISMATCH')
        if check in (None,'readers','projection','exemptions'):reader_checks(repository,result,binding)
        if check in (None,'guidance','runbook'):guidance_checks(repository,result,binding)
        if verify_workflows or check is None:
            # Use the approved ten-route inventory and 10.4 resolved guidance, preserving all 7.3 checks.
            current.workflow_checks(repository,result,binding)
        record(result,'authority:selection',authority_bundle,authority_bundle==PROVENANCE[4],'AUTHORITY_BINDING_INVALID')
        _,validators=generator.v2_load_schemas()
        predecessor=generator.v2_verified_predecessor(repository,initial['candidate'],'10.3',PROVENANCE[5],PROVENANCE[6],validators['record'])
        record(result,'predecessor:10.3',PROVENANCE[5],predecessor==PREDECESSOR_SHA256,'AUTHORITY_BINDING_INVALID')
    except FileNotFoundError:
        result['blockers'].append('AUTHORITY_UNAVAILABLE');result['assertionLedger'].append(dict(id='authority:unavailable',subject=BINDING,state='SKIPPED'))
    except (RuntimeError,subprocess.TimeoutExpired,OSError) as error:
        code=str(error) if str(error) in STABLE_CODES else 'HELPER_UNAVAILABLE'
        result['blockers'].append(code);result['assertionLedger'].append(dict(id='helper:fault',subject='bridge',state='FAIL' if code in FAIL_CODES else 'SKIPPED'))
    except (ValueError,KeyError,TypeError):
        result['blockers'].append('AUTHORITY_BINDING_INVALID');result['assertionLedger'].append(dict(id='binding:invalid',subject=BINDING,state='FAIL'))
    return finish(result)


def main(arguments=None):
    parser=argparse.ArgumentParser(allow_abbrev=False)
    parser.add_argument('--repository',default='.');parser.add_argument('--baseline',default=BASELINE)
    parser.add_argument('--candidate',default='HEAD');parser.add_argument('--authority-bundle',default=PROVENANCE[4])
    parser.add_argument('--current-binding',default=BINDING);parser.add_argument('--verify-workflows',action='store_true')
    parser.add_argument('--check',choices=('readers','projection','exemptions','guidance','runbook'))
    parser.add_argument('--format',choices=('json',),default='json');parser.add_argument('--output')
    try:
        args=parser.parse_args(arguments)
    except SystemExit as error:
        if error.code==0:return 0
        result=current.empty_result();result['blockers']=['ARGUMENT_INVALID']
        result['assertionLedger']=[dict(id='arguments',subject='argv',state='SKIPPED')]
        current.write_stdout(current.canonical(finish(result))+b'\n')
        return 2
    repository=Path(args.repository).resolve()
    result=verify(repository,args.baseline,args.candidate,authority_bundle=args.authority_bundle,
        binding_path=args.current_binding,verify_workflows=args.verify_workflows,check=args.check)
    if args.output:
        try:
            containment=current.bridge(repository,dict(baseline=BASELINE,candidate=args.candidate,containedPaths=[args.output],includeTreePaths=True,
                story104GitlinkAmendment=APPROVED_GITLINK_AMENDMENT))
            path=containment['containedPaths'][0]
            owned=set(containment['headPaths'])|set(containment['indexPaths'])|set(containment['treePaths'])
            allowed={f'artifacts/v9/10.4/AC-10.4-0{n}.json' for n in (9,)}
            target=Path(path['fullPath']) if path['fullPath'] else None
            protected=owned|set(APPROVED_SOURCE_PATHS)|set(PROVENANCE)|set(WORKFLOWS)|set(GUIDANCE)
            protected|={entry.relative_to(repository).as_posix() for entry in (repository/'.git').rglob('*') if entry.is_file()}
            collision=target is not None and target.exists() and any((repository/item).is_file()
                and target.samefile(repository/item) for item in protected)
            safe=not path['code'] and args.output in allowed and args.output not in owned and not collision
            record(result,'output:contained',args.output,safe,'EVIDENCE_OUTPUT_FORBIDDEN')
            result=finish(result,result['result']!='not-applicable')
            if safe:
                target=Path(path['fullPath']);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(current.canonical(result)+b'\n')
        except (RuntimeError,subprocess.TimeoutExpired,OSError) as error:
            code=str(error) if str(error) in STABLE_CODES else 'OUTPUT_WRITE_FAILED'
            result['blockers'].append(code)
            result['assertionLedger'].append(dict(id='output:failure',subject=args.output,state='FAIL' if code in FAIL_CODES else 'SKIPPED'))
            result=finish(result)
    current.write_stdout(current.canonical(result)+b'\n')
    return {'PASS':0,'FAIL':1,'BLOCKED':2,'not-applicable':0}[result['result']]

if __name__=='__main__':raise SystemExit(main())
