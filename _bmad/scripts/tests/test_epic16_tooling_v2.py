"""Explicit held successor selection and independent package fault controls."""
from copy import deepcopy
from dataclasses import FrozenInstanceError
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from xml.etree import ElementTree as ET

import pytest

import test_epic16_tooling as legacy
import epic16_tooling as tooling
from epic16_packages import POLICY

ROOT = legacy.ROOT
SCOPE = 'tooling-fixture'


def bind(repo):
    paths = sorted((*POLICY.tool_sources, *POLICY.contract_pins, *POLICY.predecessor_pins))
    legacy.dump(repo, POLICY.binding, dict(schemaVersion=POLICY.binding_schema,
        purpose='prerequisite-tooling-only', authority=tooling.AUTHORITY,
        sources=[dict(path=p, sha256=tooling.digest((repo/p).read_bytes())) for p in paths], entry=None))


@pytest.fixture(scope='module')
def selected_repository(tmp_path_factory):
    repo = legacy.repository.__wrapped__(tmp_path_factory.mktemp('tooling-v2'))
    for path in {*POLICY.tool_sources, *POLICY.contract_pins, *POLICY.predecessor_pins}:
        target=repo/path; target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(ROOT/path,target)
    bind(repo)
    warning=repo/tooling.measurements.execution.WARNING_PATH
    warning.write_bytes(b'// SINGLE-REPLICA-ONLY: synthetic disposable warning surface.\n')
    legacy.git(repo,'add','.')
    legacy.git(repo,'commit','-qm','test: bind explicit successor fixture')
    warning.write_bytes(b'// Synthetic disposable candidate with qualified durable projection.\n')
    legacy.git(repo,'add',str(warning.relative_to(repo)))
    legacy.git(repo,'commit','-qm','test: bind disposable fixture sources')
    for scenario in tooling.measurements.SCENARIOS:
        legacy.native(repo,scenario,contractSha256=POLICY.contract_pins[tooling.contract_path(scenario[3:7],POLICY)])
    return repo


def cli(repo, story='16.1', scenario='AC-16.1-01', *, contract=None, generator=False, scope=SCOPE, extra=()):
    command=[sys.executable,str(repo/'_bmad/scripts'/('generate_story_record.py' if generator else f'verify_story_16_{story[-1]}.py')),
        '--repository',str(repo),'--contract',contract or tooling.contract_path(story,POLICY),'--scope',scope]
    command += ['--format','bundle'] if generator else ['--scenario',scenario]
    result=subprocess.run(command+list(extra),cwd=repo,capture_output=True,timeout=180)
    assert result.stdout, (command,result.stderr)
    return result,json.loads(result.stdout)


def evaluate(repo):
    return tooling.evaluate(repo,'16.1','AC-16.1-01',scope=SCOPE,package=POLICY)


@pytest.mark.parametrize('scenario',tooling.measurements.SCENARIOS)
def test_all_fifteen_native_routes_use_actual_selected_entrypoints(selected_repository,scenario):
    process,result=cli(selected_repository,story=scenario[3:7],scenario=scenario)
    assert process.returncode==0,result
    assert result['runtimeQualified'] is False
    assert result['counts']==dict(total=1,passed=1,failed=0,skipped=0)
    assert result['bindings']['contract']['path']==tooling.contract_path(scenario[3:7],POLICY)
    assert result['bindings']['schema']['path']==POLICY.schema
    assert result['assertionLedger']


@pytest.mark.parametrize('story',tooling.measurements.STORIES)
def test_three_deterministic_final_records_through_actual_generator(selected_repository,story):
    repo=selected_repository
    for n in range(1,6):
        scenario=f'AC-{story}-0{n}'
        process,result=cli(repo,story,scenario,extra=['--output',tooling.receipt_path(SCOPE,scenario)])
        assert process.returncode==0,result
    process,result=cli(repo,story,generator=True)
    assert process.returncode==0,result
    pair=tuple((repo/p).read_bytes() for p in tooling.final_paths(SCOPE,story))
    process,again=cli(repo,story,generator=True)
    assert process.returncode==0 and again==result
    assert pair==tuple((repo/p).read_bytes() for p in tooling.final_paths(SCOPE,story))
    assert result['runtimeQualified'] is False
    assert result['summary']==dict(required=6,passed=6,failed=0,blocked=0,skipped=0,notRun=0)


def test_selected_inserted_record_through_actual_generator(selected_repository,tmp_path):
    repo=tmp_path/'inserted';shutil.copytree(selected_repository,repo)
    for n in range(1,6):
        scenario=f'AC-16.1-0{n}'
        process,result=cli(repo,scenario=scenario,extra=['--output',tooling.receipt_path(SCOPE,scenario)])
        assert process.returncode==0,result
    process,record=cli(repo,generator=True)
    assert process.returncode==0,record
    pair=tuple((repo/path).read_bytes() for path in tooling.final_paths(SCOPE,'16.1'))
    spec=repo/'fixture-spec.md';original=spec.read_bytes()
    begin=b'\n<!-- STORY-FINAL-RECORD:BEGIN -->\n';end=b'<!-- STORY-FINAL-RECORD:END -->\n'
    spec.write_bytes(original+begin+pair[1]+end)
    arguments=['--verify-inserted-record','fixture-spec.md']
    process,result=cli(repo,generator=True,extra=arguments)
    assert process.returncode==0 and result['result']=='PASS',result
    assert result==record
    assert result['contract']==dict(path=tooling.contract_path('16.1',POLICY),
        sha256=POLICY.contract_pins[tooling.contract_path('16.1',POLICY)])
    assert result['schema']==dict(path=POLICY.schema,sha256=POLICY.schema_pin)
    spec.write_bytes(original+begin+pair[1]+b'Corrupted inserted record.\n'+end)
    process,result=cli(repo,generator=True,extra=arguments)
    assert process.returncode==1
    legacy.rejected(result,'INSERTED_RECORD_DRIFT')
    assert pair==tuple((repo/path).read_bytes() for path in tooling.final_paths(SCOPE,'16.1'))


@pytest.mark.parametrize('story',tooling.measurements.STORIES)
def test_held_selected_runtime_clis_preserve_existing_evidence(selected_repository,tmp_path,story):
    real_outputs=[tooling.receipt_path('runtime',f'AC-16.1-0{n}') for n in range(1,6)]+tooling.final_paths('runtime','16.1')
    assert all(not (ROOT/path).exists() for path in real_outputs)
    repo=tmp_path/'held';shutil.copytree(selected_repository,repo)
    paths=[tooling.receipt_path('runtime',f'AC-{story}-0{n}') for n in range(1,6)]+tooling.final_paths('runtime',story)
    before={path:('Existing held evidence: '+path+'\r\n').encode() for path in paths}
    for path,raw in before.items():
        target=repo/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
    try:
        commands=[(False,['--output',path],f'AC-{story}-0{n}') for n,path in enumerate(paths[:5],1)]
        commands.append((True,['--output-json',paths[5],'--output-markdown',paths[6]],f'AC-{story}-06'))
        for generator,arguments,scenario in commands:
            process,result=cli(repo,story,scenario,generator=generator,scope='runtime',extra=arguments)
            assert process.returncode==2,result
            legacy.rejected(result,'ENTRY_QUALIFICATION_PENDING','BLOCKED')
            assert result['counts']==dict(total=0,passed=0,failed=0,skipped=0)
            assert result['runtimeQualified'] is False
            assert before=={path:(repo/path).read_bytes() for path in paths}
    finally:
        assert all(not (ROOT/path).exists() for path in real_outputs)


def test_policy_and_preserved_obligations_are_immutable():
    with pytest.raises(FrozenInstanceError): POLICY.base='changed'
    with pytest.raises(TypeError): POLICY.contract_pins['x']='0'*64
    assert not POLICY.trusted_entries
    for story in tooling.measurements.STORIES:
        v1=json.loads((ROOT/tooling.contract_path(story)).read_bytes())
        v2=json.loads((ROOT/tooling.contract_path(story,POLICY)).read_bytes())
        assert v2['predecessor']==tooling.contract_path(story)
        assert v2['preservedContract']==v1['preservedContract']
        assert len(v2['preservedContract']['scenarios'])==6
        assert v2['nativeProtocol']==v1['nativeProtocol']
        assert v2['authority']==tooling.AUTHORITY
    assert POLICY.binding not in POLICY.tool_sources
    assert '_bmad/scripts/epic16_packages.py' not in tooling.TOOL_SOURCES


@pytest.mark.parametrize('path,code',[
    (tooling.contract_path('16.1',POLICY),'CONTRACT_DRIFT'),
    (POLICY.schema,'SCHEMA_DRIFT'),
    (tooling.contract_path('16.1'),'PREDECESSOR_DRIFT'),
    ('_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json','PREDECESSOR_DRIFT'),
    ('.config/dotnet-tools.json','SOURCE_DRIFT'),
    ('.gitattributes','SOURCE_DRIFT'),
    ('_bmad/schemas/.gitattributes','SOURCE_DRIFT'),
])
def test_independent_pin_and_source_guards_restore_exactly(selected_repository,path,code):
    repo=selected_repository;target=repo/path;before=target.read_bytes()
    assert evaluate(repo)['result']=='PASS'
    try:
        target.write_bytes(before+b'\n')
        result=evaluate(repo);legacy.rejected(result,code)
        assert result['assertionLedger'][-1]['subject']==path
    finally:target.write_bytes(before)
    assert target.read_bytes()==before and evaluate(repo)['result']=='PASS'


@pytest.mark.parametrize('fault',['duplicate','omit','addition','self','cross-version'])
def test_source_row_identity_is_exact(selected_repository,fault):
    repo=selected_repository;target=repo/POLICY.binding;before=target.read_bytes();document=json.loads(before)
    if fault=='duplicate':document['sources'].append(deepcopy(document['sources'][0]))
    elif fault=='omit':document['sources'].pop()
    elif fault=='addition':document['sources'].append(dict(path='unexpected',sha256='0'*64))
    elif fault=='self':document['sources'].append(dict(path=POLICY.binding,sha256=tooling.digest(before)))
    else:document['sources']=json.loads((repo/tooling.BINDING).read_bytes())['sources']
    try:
        legacy.dump(repo,POLICY.binding,document)
        legacy.rejected(evaluate(repo),'INVENTORY_DRIFT')
    finally:target.write_bytes(before)
    assert evaluate(repo)['result']=='PASS'


@pytest.mark.parametrize('path',['_bmad/scripts/epic16_packages.py','_bmad/scripts/epic16_trust_v2.py','_bmad/scripts/epic16_tooling.py'])
def test_executing_snapshot_independently_rejects_rebound_substitution(selected_repository,path):
    repo=selected_repository;target=repo/path;original=target.read_bytes();binding=(repo/POLICY.binding).read_bytes()
    try:
        target.write_bytes(original+b'\n# substituted\n');bind(repo)
        legacy.rejected(evaluate(repo),'EXECUTING_SOURCE_DRIFT')
    finally:target.write_bytes(original);(repo/POLICY.binding).write_bytes(binding)
    assert evaluate(repo)['result']=='PASS'


@pytest.mark.parametrize('generator',[False,True])
def test_unknown_selector_and_no_fallback(selected_repository,generator):
    process,result=cli(selected_repository,contract='unknown-contract.json',generator=generator)
    assert process.returncode!=0 and result['assertionLedger']
    assert result['result'] in ('FAIL','BLOCKED')
    if not generator:assert result['blockers']==['CONTRACT_UNKNOWN']


def test_wrong_story_is_rejected(selected_repository):
    process,result=cli(selected_repository,contract=tooling.contract_path('16.2',POLICY))
    assert process.returncode==1
    legacy.rejected(result,'CONTRACT_STORY_MISMATCH')


def test_owner_json_cannot_nominate_entry(selected_repository):
    repo=selected_repository;target=repo/POLICY.binding;before=target.read_bytes();document=json.loads(before)
    owner=repo/'owner.json';owner.write_text('{"executionAllowed":true}')
    document['entry']=dict(path='owner.json',sha256=tooling.digest(owner.read_bytes()))
    try:
        legacy.dump(repo,POLICY.binding,document)
        legacy.rejected(tooling.evaluate(repo,'16.1','AC-16.1-01',package=POLICY),'ENTRY_UNTRUSTED')
    finally:target.write_bytes(before);owner.unlink()


@pytest.mark.parametrize('kind',['outside','hardlink','symlink'])
def test_unsafe_outputs_reject_before_any_write(selected_repository,tmp_path,kind):
    repo=selected_repository
    if kind=='outside':
        process,result=cli(repo,extra=['--output',str(tmp_path/'outside.json')])
        legacy.rejected(result,'OUTPUT_FORBIDDEN');return
    path=repo/tooling.receipt_path(SCOPE,'AC-16.1-01');path.parent.mkdir(parents=True,exist_ok=True)
    original=path.read_bytes() if path.exists() else None
    if path.exists():path.unlink()
    peer=tmp_path/'peer';peer.write_text('preserved')
    try:
        if kind=='hardlink':os.link(peer,path)
        else:path.symlink_to(peer)
        process,result=cli(repo,extra=['--output',tooling.receipt_path(SCOPE,'AC-16.1-01')])
        assert process.returncode==1 and result['assertionLedger']
        assert peer.read_text()=='preserved'
    finally:
        path.unlink()
        if original is not None:path.write_bytes(original)


@pytest.mark.parametrize('generator',[False,True])
@pytest.mark.parametrize('fault',['unavailable','internal'])
def test_selected_lazy_imports_distinguish_unavailable_from_internal_defect(selected_repository,generator,fault):
    repo=selected_repository;path=repo/'_bmad/scripts/epic16_packages.py';before=path.read_bytes()
    try:
        if fault=='unavailable':path.rename(path.with_suffix('.hidden'))
        else:path.write_bytes(b'import missing_internal_dependency\n'+before)
        command=[sys.executable,str(repo/'_bmad/scripts'/('generate_story_record.py' if generator else 'verify_story_16_1.py')),
            '--repository',str(repo),'--contract',tooling.contract_path('16.1',POLICY)]
        command+=['--format','bundle'] if generator else ['--scenario','AC-16.1-01']
        process=subprocess.run(command,cwd=repo,capture_output=True,timeout=60)
        if fault=='unavailable':
            result=json.loads(process.stdout);assert process.returncode==2 and result['assertionLedger']
            assert result['result']=='BLOCKED'
        else:
            assert process.returncode==1 and b'missing_internal_dependency' in process.stderr
            assert b'Traceback' in process.stderr
    finally:
        if fault=='unavailable':path.with_suffix('.hidden').rename(path)
        else:path.write_bytes(before)


def test_actual_v1_cli_retains_current_pin_drift_before_new_imports():
    command=[sys.executable,str(ROOT/'_bmad/scripts/verify_story_16_1.py'),'--repository',str(ROOT),
        '--contract',tooling.contract_path('16.1'),'--scenario','AC-16.1-01']
    process=subprocess.run(command,cwd=ROOT,capture_output=True,timeout=60)
    result=json.loads(process.stdout);assert process.returncode==1
    legacy.rejected(result,'SOURCE_DRIFT')
    assert result['assertionLedger'][-1]['subject']=='.config/dotnet-tools.json'


def test_held_selected_v2_emits_no_runtime_outputs():
    result=tooling.evaluate(ROOT,'16.1','AC-16.1-01',package=POLICY)
    legacy.rejected(result,'ENTRY_QUALIFICATION_PENDING','BLOCKED')
    assert result['counts']==dict(total=0,passed=0,failed=0,skipped=0)
    for path in [tooling.receipt_path('runtime',f'AC-16.1-0{n}') for n in range(1,6)]+tooling.final_paths('runtime','16.1'):
        assert not (ROOT/path).exists()


@pytest.fixture(scope='module')
def preserved_repository(tmp_path_factory):
    return legacy.repository.__wrapped__(tmp_path_factory.mktemp('preserved-v1'))


@pytest.mark.parametrize('path',['.gitattributes','.config/dotnet-tools.json'])
def test_v1_independent_source_faults_keep_drift_before_optional_v2_imports(preserved_repository,path):
    repo=preserved_repository
    assert not (repo/'_bmad/scripts/epic16_packages.py').exists()
    process,result=cli(repo,contract=tooling.contract_path('16.1'))
    assert process.returncode==0 and result['result']=='PASS'
    target=repo/path;before=target.read_bytes()
    try:
        target.write_bytes(before+b'\n')
        process,result=cli(repo,contract=tooling.contract_path('16.1'))
        assert process.returncode==1
        legacy.rejected(result,'SOURCE_DRIFT')
        assert result['assertionLedger'][-1]['subject']==path
    finally:target.write_bytes(before)
    process,result=cli(repo,contract=tooling.contract_path('16.1'))
    assert process.returncode==0 and result['result']=='PASS'


def test_independent_source_pin_rejects_with_unchanged_source_bytes(selected_repository):
    repo=selected_repository;target=repo/POLICY.binding;before=target.read_bytes();document=json.loads(before)
    path=document['sources'][0]['path'];source=(repo/path).read_bytes()
    try:
        document['sources'][0]['sha256']='0'*64;legacy.dump(repo,POLICY.binding,document)
        legacy.rejected(evaluate(repo),'SOURCE_DRIFT')
        assert (repo/path).read_bytes()==source
    finally:target.write_bytes(before)
    assert evaluate(repo)['result']=='PASS'


def test_loaded_policy_module_location_is_bound(selected_repository,monkeypatch,tmp_path):
    assert evaluate(selected_repository)['result']=='PASS'
    with monkeypatch.context() as changed:
        changed.setattr(sys.modules['epic16_packages'],'__file__',str(tmp_path/'epic16_packages.py'))
        legacy.rejected(evaluate(selected_repository),'EXECUTING_SOURCE_DRIFT')
    assert evaluate(selected_repository)['result']=='PASS'


def test_native_contract_digest_cannot_cross_versions(selected_repository):
    repo=selected_repository;target=repo/tooling.native_path(SCOPE,'AC-16.1-01');before=target.read_bytes()
    root=ET.fromstring(before);node=root.find('.//StdOut');payload=json.loads(node.text)
    try:
        payload['contractSha256']=tooling.CONTRACT_PINS[tooling.contract_path('16.1')]
        node.text=json.dumps(payload);target.write_bytes(ET.tostring(root))
        legacy.rejected(evaluate(repo),'NATIVE_BINDING_INVALID')
    finally:target.write_bytes(before)
    assert evaluate(repo)['result']=='PASS'


def test_preserved_requirement_and_graph_inventory_counts():
    base=ROOT/'_bmad-output/planning-artifacts/epic-16-adoption-v2'
    graph=json.loads((base/'execution-graph.json').read_bytes())
    inventory=json.loads((base/'inventories.json').read_bytes())
    assert len(graph['nodes'])==38 and len(graph['edges'])==61
    assert len(inventory['requirements'])==32 and len(inventory['topologicalOrder'])==38
    identities=[]
    for story in tooling.measurements.STORIES:
        contract=json.loads((ROOT/tooling.contract_path(story,POLICY)).read_bytes())
        identities += [row['id'] for row in contract['preservedContract']['scenarios']]
    assert len(identities)==len(set(identities))==18


@pytest.mark.parametrize('generator',[False,True])
@pytest.mark.parametrize('fault',['policy-symlink','trust-symlink','secure-capture-unavailable'])
def test_selected_package_capture_failures_are_structured(selected_repository,tmp_path,generator,fault):
    repo=selected_repository
    process,result=cli(repo,generator=False)
    assert process.returncode==0 and result['result']=='PASS'
    name='epic16_trust_v2.py' if fault=='trust-symlink' else 'epic16_packages.py'
    target=repo/'_bmad/scripts'/name;before=target.read_bytes()
    try:
        if fault.endswith('symlink'):
            peer=tmp_path/name;peer.write_bytes(before);target.unlink();target.symlink_to(peer)
        else:
            target.write_bytes(b'import contained_reader\ncontained_reader.secure_open_supported = lambda: False\n'+before)
        process,result=cli(repo,generator=generator)
        expected='SECURE_OPEN_UNAVAILABLE' if fault=='secure-capture-unavailable' else 'PATH_ESCAPE'
        legacy.rejected(result,expected,'BLOCKED' if fault=='secure-capture-unavailable' else 'FAIL')
        assert process.returncode==(2 if fault=='secure-capture-unavailable' else 1)
        assert b'Traceback' not in process.stderr
    finally:
        if target.is_symlink():target.unlink()
        target.write_bytes(before)
    process,result=cli(repo,generator=False)
    assert process.returncode==0 and result['result']=='PASS'


def test_independently_pinned_schema_and_attribute_policy_survive_autocrlf_checkout(tmp_path):
    repo=tmp_path/'autocrlf';repo.mkdir()
    legacy.git(repo,'init','-q');legacy.git(repo,'config','core.autocrlf','true')
    paths=['_bmad/schemas/.gitattributes',POLICY.schema]
    for path in paths:
        target=repo/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((ROOT/path).read_bytes())
    legacy.git(repo,'add',*paths)
    for path in paths:(repo/path).unlink()
    legacy.git(repo,'checkout-index','--all','--force')
    for path in paths:
        assert (repo/path).read_bytes()==(ROOT/path).read_bytes()
        assert b'\r' not in (repo/path).read_bytes()
        attributes=legacy.git(repo,'check-attr','text','eol','--',path)
        assert attributes==f'{path}: text: set\n{path}: eol: lf'
        assert path in POLICY.tool_sources
