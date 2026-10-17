"""Real shared-helper fault injection for the approved 10.4 boundary."""
from __future__ import annotations
import copy
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import pytest
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'_bmad/scripts'))
import evidence_boundary_adoption as adoption
import evidence_boundary_current as current

@pytest.fixture(autouse=True)
def stamp_adoption_candidate(record_property):
    revision=subprocess.run(['git','-C',str(ROOT),'rev-parse','HEAD'],check=True,capture_output=True,text=True).stdout.strip()
    record_property('story104_source_revision',revision)

@pytest.fixture
def adoption_surface(tmp_path):
    for path in ('.agents/skills','.claude/skills','_bmad/custom','_bmad/bmm','_bmad/core'):
        shutil.copytree(ROOT/path,tmp_path/path)
    shutil.copy2(ROOT/'_bmad/config.toml',tmp_path/'_bmad/config.toml')
    for path in adoption.GUIDANCE:
        (tmp_path/path).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/path,tmp_path/path)
    return tmp_path,json.loads((ROOT/adoption.BINDING).read_bytes())

@pytest.fixture(scope='module')
def adoption_git(tmp_path_factory):
    root=tmp_path_factory.mktemp('adoption-git')
    def git(*args):return subprocess.run(['git','-C',str(root),*args],check=True,capture_output=True).stdout.decode().strip()
    git('init','-q');git('config','user.name','Fixture');git('config','user.email','fixture@example.invalid')
    (root/'evidence.txt').write_text('before\n');git('add','.');git('commit','-qm','test: establish fixture');before=git('rev-parse','HEAD')
    (root/'evidence.txt').write_text('after\n');git('add','.');git('commit','-qm','test: update fixture')
    return root,before,git('rev-parse','HEAD')


def observed_fault(record_property,fault,code,observed,before,after):
    assert observed['result'] in ('FAIL','BLOCKED')
    assert code in observed['blockers']
    assert observed['blockers']==sorted(set(observed['blockers']))
    assert observed['assertionLedger']
    assert before==after
    envelope=current.empty_result('a'*40)
    envelope['assertionLedger']=observed['assertionLedger']
    envelope['blockers']=observed['blockers']
    envelope=adoption.finish(envelope)
    decision=current.transition_decision(envelope,2 if envelope['result']=='BLOCKED' else 1)
    assert decision==dict(allowed=False,recordedResult=envelope['result'],blockers=observed['blockers'])
    for key,value in dict(fault_id=fault,expected_code=code,observed_blockers=json.dumps(observed['blockers']),
        before_sha256=before,after_sha256=after,assertion_ledger=json.dumps(observed['assertionLedger'])).items():
        record_property('story104_'+key,value)

@pytest.mark.parametrize('fault,code',[
 ('HASH','EVIDENCE_HASH_MISMATCH'),('PATH_ESCAPE','EVIDENCE_PATH_ESCAPE'),('GENERATED_OUTPUT','EVIDENCE_OUTPUT_FORBIDDEN'),
 ('SUBSET','CHANGED_PATH_SET_MISMATCH'),('SIGNED_ALLOWLIST','SIGNED_EVIDENCE_ALLOWLISTED'),
 ('SIGNABLE','SIGNABLE_PAYLOAD_MISMATCH'),('ROOT','ROOT_OF_TRUST_NOT_PINNED'),('GIT_UNAVAILABLE','GIT_UNAVAILABLE'),
 ('HISTORY_UNAVAILABLE','REVISION_INVALID'),('GITLINK','EVIDENCE_BOUNDARY_GITLINK')])
def test_complete_fault_matrix_integrity(adoption_git,fault,code,record_property):
    root,before,candidate=adoption_git;original=(root/'evidence.txt').read_bytes();index=(root/'.git/index').read_bytes()
    manifest=[dict(path='evidence.txt',sha256=hashlib.sha256(original).hexdigest(),role='evidence')]
    request=dict(baseline=before,candidate=candidate,expectedPaths=['evidence.txt'],manifest=manifest)
    if fault=='HASH':manifest[0]['sha256']='0'*64
    if fault=='PATH_ESCAPE':manifest[0]['path']='../evidence.txt'
    if fault=='GENERATED_OUTPUT':manifest[0]['path']='obj/evidence.txt'
    if fault=='SUBSET':request['expectedPaths']=[]
    if fault=='GIT_UNAVAILABLE':request['gitExecutable']='missing-story104-git-executable'
    if fault=='HISTORY_UNAVAILABLE':request['baseline']='f'*40
    if fault in ('ROOT','SIGNED_ALLOWLIST'):
        (root/'roots.cs').write_text('internal class Roots { const string Trusted = "pinned"; }')
        request['roots']=dict(sourcePath='roots.cs',constants={'Trusted':'wrong' if fault=='ROOT' else 'pinned'},
            allowlist=['docs/release-evidence/signed.json'] if fault=='SIGNED_ALLOWLIST' else [])
    request['signablePayloadHash']=hashlib.sha256(''.join(f'{r["path"]}\t{r["sha256"]}\t{r["role"]}\n' for r in manifest).encode()).hexdigest()
    if fault=='SIGNABLE':request['signablePayloadHash']='0'*64
    if fault=='GITLINK':
        def git(*args):return subprocess.run(['git','-C',str(root),*args],check=True,capture_output=True).stdout.decode().strip()
        git('update-index','--add','--cacheinfo',f'160000,{candidate},reference')
        tree=git('write-tree');request.update(baseline=candidate,candidate=git('commit-tree',tree,'-p',candidate,'-m','test: raw mode'),expectedPaths=['reference'])
    try: observed=current.bridge(root,request)
    finally:
        (root/'evidence.txt').write_bytes(original);(root/'.git/index').write_bytes(index);(root/'roots.cs').unlink(missing_ok=True)
    assert observed['blockers']==[code]
    observed_fault(record_property,fault,code,observed,hashlib.sha256(original+index).hexdigest(),
        hashlib.sha256((root/'evidence.txt').read_bytes()+(root/'.git/index').read_bytes()).hexdigest())

@pytest.mark.parametrize('fault,code',[
 ('CHAIN_CHANGED','EVIDENCE_CHAIN_TABLE_INVALID'),('CHAIN_MISSING','EVIDENCE_CHAIN_TABLE_INVALID'),
 ('CHAIN_REORDERED','EVIDENCE_CHAIN_TABLE_INVALID'),('CHAIN_NESTED','EVIDENCE_CHAIN_MARKER_INVALID'),
 ('CHAIN_DUPLICATE','EVIDENCE_CHAIN_MARKER_INVALID'),('CHAIN_UNTERMINATED','EVIDENCE_CHAIN_MARKER_INVALID'),
 ('CHAIN_DUPLICATE_ROW','EVIDENCE_CHAIN_TABLE_INVALID')])
def test_complete_fault_matrix_chain(adoption_git,fault,code,record_property):
    root,before,candidate=adoption_git
    original=(ROOT/adoption.AUTHORITY_EPICS).read_bytes()
    text=original.decode();family='EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4'
    begin=next(line for line in text.splitlines() if line.startswith('<!-- '+family+':BEGIN'))
    end=next(line for line in text.splitlines() if line.startswith('<!-- '+family+':END'))
    block=text[text.index(begin):text.index(end)+len(end)]
    following='<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:BEGIN'
    if fault=='CHAIN_CHANGED':changed=text.replace('supersedes=epic-6-authority-2026-07-27-v3','supersedes=epic-6-authority-2026-07-15-v2',1)
    if fault=='CHAIN_MISSING':changed=text.replace(block,'')
    if fault=='CHAIN_REORDERED':
        changed=text.replace(block,'');point=changed.index('<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:END')
        point=changed.index(' -->',point)+4;changed=changed[:point]+'\n'+block+changed[point:]
    if fault=='CHAIN_NESTED':
        changed=text.replace(block,'');point=changed.index(following);point=changed.index(' -->',point)+4
        changed=changed[:point]+'\n'+block+changed[point:]
    if fault=='CHAIN_DUPLICATE':changed=text+'\n'+block
    if fault=='CHAIN_UNTERMINATED':changed=text.replace(text.splitlines()[-1],'')
    if fault=='CHAIN_DUPLICATE_ROW':changed=text.replace(begin,begin.replace(' version=',' version=epic-6-authority-2026-07-28-v4 version=',1))
    path=root/adoption.AUTHORITY_EPICS;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(original)
    digest=hashlib.sha256(original).hexdigest()
    try:
        path.write_bytes(changed.encode());result=current.empty_result(candidate)
        adoption.authority_chain_checks(root,result,before,candidate);observed=adoption.finish(result)
    finally:path.write_bytes(original)
    assert observed['blockers']==[code]
    observed_fault(record_property,fault,code,observed,digest,hashlib.sha256(path.read_bytes()).hexdigest())


def test_frozen_authority_chain_requires_the_final_document_boundary(adoption_git):
    root,before,candidate=adoption_git
    path=root/adoption.AUTHORITY_EPICS;path.parent.mkdir(parents=True,exist_ok=True)
    original=(ROOT/adoption.AUTHORITY_EPICS).read_bytes();path.write_bytes(original)
    request=dict(baseline=before,candidate=candidate,authorityChains=[dict(id='actual-frozen-chain',path=adoption.AUTHORITY_EPICS)])
    try:
        valid=current.bridge(root,request)
        assert valid['result']=='PASS' and valid['assertionLedger']
        path.write_bytes(original+b'after final END\n')
        invalid=current.bridge(root,request)
        assert invalid['result']=='FAIL' and invalid['blockers']==['EVIDENCE_CHAIN_MARKER_INVALID']
        assert invalid['assertionLedger']
    finally:path.write_bytes(original)


@pytest.mark.parametrize('fault,code',[
 ('WORKFLOW_REMOVED','EVIDENCE_GATE_NOT_USED'),('MARKER_MALFORMED','EVIDENCE_GATE_NOT_USED'),('SPAN_DISPLACED','EVIDENCE_GATE_DISPLACED')])
def test_complete_fault_matrix_workflow(adoption_surface,fault,code,record_property):
    root,binding=adoption_surface;path=root/adoption.WORKFLOWS[0];original=path.read_bytes();text=path.read_text()
    start=text.index(current.BEGIN);end=text.index(current.END)+len(current.END);block=text[start:end]
    if fault=='WORKFLOW_REMOVED':text=text.replace(block,'')
    if fault=='MARKER_MALFORMED':text=text.replace(current.END,current.BEGIN)
    if fault=='SPAN_DISPLACED':text=text.replace(block,'')+'\n'+block
    try:
        path.write_text(text);result=current.empty_result('a'*40);current.workflow_checks(root,result,binding)
        observed=adoption.finish(result)
        # The route's primary blocker is measured separately from consequent twin/render drift.
        route_rows=[row for row in observed['assertionLedger'] if row['id'].startswith(('workflow:presence:','workflow:span:')) and row['subject']==adoption.WORKFLOWS[0]]
        assert set(observed['blockers'])=={code,'EVIDENCE_WORKFLOW_PARITY_DRIFT'}
        assert any(row['state']=='FAIL' for row in route_rows)
    finally:path.write_bytes(original)
    observed_fault(record_property,fault,code,observed,hashlib.sha256(original).hexdigest(),hashlib.sha256(path.read_bytes()).hexdigest())

@pytest.mark.parametrize('fault,code',[
 ('GUIDANCE_REMOVED','EVIDENCE_GUIDANCE_NOT_USED'),('GUIDANCE_EQUALITY','EVIDENCE_GUIDANCE_DRIFT'),
 ('GUIDANCE_DECLARED_HASH','EVIDENCE_GUIDANCE_DRIFT'),('GUIDANCE_REDIRECT','EVIDENCE_GUIDANCE_DRIFT')])
def test_complete_fault_matrix_guidance(adoption_surface,fault,code,record_property):
    root,binding=adoption_surface;path=root/'_bmad/custom/bmad-build.toml';original=path.read_bytes()
    try:
        if fault=='GUIDANCE_REMOVED':path.unlink()
        else:
            old,new={'GUIDANCE_EQUALITY':('exact changed-path equality','changed-path containment'),
                'GUIDANCE_DECLARED_HASH':('recomputed hashes','declared hashes'),
                'GUIDANCE_REDIRECT':(adoption.RUNBOOK,'docs/runbooks/current-change-validation.md')}[fault]
            path.write_bytes(original.replace(old.encode(),new.encode()))
        result=current.empty_result('a'*40);adoption.guidance_checks(root,result,binding);observed=adoption.finish(result)
        assert code in observed['blockers']
        assert set(observed['blockers'])=={'EVIDENCE_GUIDANCE_DRIFT','EVIDENCE_GUIDANCE_NOT_USED'}
    finally:path.write_bytes(original)
    observed_fault(record_property,fault,code,observed,hashlib.sha256(original).hexdigest(),hashlib.sha256(path.read_bytes()).hexdigest())

@pytest.mark.parametrize('fault,code',[
 ('READER_STRENGTH','ASSERTION_STRENGTH_WEAKENED'),('READER_HELPER','EVIDENCE_HELPER_NOT_USED'),
 ('EXEMPTION','DAY_ONE_EXEMPTION_FORBIDDEN'),('READER_INVENTORY','EVIDENCE_READER_INVENTORY_DRIFT')])
def test_complete_fault_matrix_readers(tmp_path,fault,code,record_property):
    binding=json.loads((ROOT/adoption.BINDING).read_bytes());entries=adoption.snapshot(ROOT)['readers']
    for row in entries:
        target=tmp_path/row['path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((ROOT/row['path']).read_bytes())
    for path in (adoption.READERS,adoption.SNAPSHOT):
        (tmp_path/path).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/path,tmp_path/path)
    for project in ('Conformance.Tests','Conformance.Portable.Tests','Contracts.Tests','IntegrationTests'):
        path=f'tests/Hexalith.Conversations.{project}/Hexalith.Conversations.{project}.csproj'
        (tmp_path/path).parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/path,tmp_path/path)
    path=tmp_path/entries[0]['path'];original=path.read_bytes();frozen=copy.deepcopy(binding)
    try:
        if fault=='READER_STRENGTH':path.write_bytes(original.replace(b'first.Tests.ShouldNotBeEmpty();',b'first.Tests.ShouldBeEmpty();'))
        if fault=='READER_HELPER':path.write_bytes(original.replace(b'RepositoryLocator.',b'LocalRepositoryLocator.'))
        if fault=='EXEMPTION':binding['exemptions']=['reader']
        if fault=='READER_INVENTORY':binding['readerInventory']['paths']=binding['readerInventory']['paths'][:-1]
        result=current.empty_result('a'*40);adoption.reader_checks(tmp_path,result,binding,historical=False);observed=adoption.finish(result)
        assert set(observed['blockers'])==({code,'ASSERTION_STRENGTH_WEAKENED'} if fault=='READER_HELPER' else {code})
    finally:path.write_bytes(original);binding=frozen
    observed_fault(record_property,fault,code,observed,hashlib.sha256(original).hexdigest(),hashlib.sha256(path.read_bytes()).hexdigest())


def test_adoption_output_hardlink_cannot_overwrite_a_source(adoption_git,monkeypatch,capsys):
    root,before,candidate=adoption_git
    monkeypatch.setattr(adoption,'BASELINE',before)
    output=root/'artifacts/v9/10.4/AC-10.4-09.json';output.parent.mkdir(parents=True,exist_ok=True)
    source=root/'evidence.txt';original=source.read_bytes()
    output.hardlink_to(source)
    result=current.empty_result(candidate);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    result=adoption.finish(result)
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:copy.deepcopy(result))
    try:
        assert adoption.main(['--repository',str(root),'--candidate',candidate,'--output',output.relative_to(root).as_posix()])==1
        observed=json.loads(capsys.readouterr().out)
        assert observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
        assert source.read_bytes()==original
    finally:output.unlink()


@pytest.mark.parametrize('fault',['none','count','other-assertion','binding','snapshot','root-substitution','existing-substitution'])
def test_approved_assertion_correction_preserves_every_other_entry_byte(tmp_path,monkeypatch,fault):
    """Only the exact owner-approved count change may differ from the plumbing migration."""
    binding=json.loads((ROOT/adoption.BINDING).read_bytes());entry=adoption.snapshot(ROOT)
    historical=json.loads(subprocess.check_output(['git','-C',str(ROOT),'show',
        '4e937361613c2dba72c46595226219017e6ba524:'+adoption.SNAPSHOT]))
    assert [(r['path'],r['entryBase64'],r['entrySha256']) for r in entry['readers']]==[
        (r['path'],r['entryBase64'],r['entrySha256']) for r in historical['readers']]
    # Independently pin the four fixture-root plumbing additions; every earlier substitution is immutable.
    expected_fixture_roots = [{'kind': 'read:explicit-fixture-root', 'before': 'RepositoryLocator.ReadAllText(regular, Encoding.UTF8)', 'after': 'RepositoryLocator.ReadAllText(regular, Encoding.UTF8, fixtureRoot)', 'count': 1}, {'kind': 'read:explicit-fixture-root', 'before': 'byte[] regularBytes = RepositoryLocator.ReadAllBytes(regularPath);', 'after': 'byte[] regularBytes = RepositoryLocator.ReadAllBytes(regularPath, fixtureRoot);', 'count': 1}, {'kind': 'read:explicit-fixture-root', 'before': 'RepositoryLocator.ReadAllBytes(regularPath).ShouldBe(regularBytes);\n                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);\n\n                try\n                {\n                    RunProcessInDirectory(fixtureRoot, "git", ["rm",', 'after': 'RepositoryLocator.ReadAllBytes(regularPath, fixtureRoot).ShouldBe(regularBytes);\n                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);\n\n                try\n                {\n                    RunProcessInDirectory(fixtureRoot, "git", ["rm",', 'count': 1}, {'kind': 'read:explicit-fixture-root', 'before': 'RepositoryLocator.ReadAllBytes(regularPath).ShouldBe(regularBytes);\n                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);\n\n                string stagedLink', 'after': 'RepositoryLocator.ReadAllBytes(regularPath, fixtureRoot).ShouldBe(regularBytes);\n                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);\n\n                string stagedLink', 'count': 1}]
    for current_row,original_row in zip(entry['readers'],historical['readers']):
        original_substitutions=original_row['substitutions']
        assert current_row['substitutions'][:len(original_substitutions)]==original_substitutions
        assert current_row['substitutions'][len(original_substitutions):]==(
            expected_fixture_roots if current_row['path'].endswith('/PreservationTraceabilityManifestValidationTest.cs') else [])
    for row in entry['readers']:
        path=tmp_path/row['path'];path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes((ROOT/row['path']).read_bytes())
    for path in (adoption.READERS,):
        (tmp_path/path).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/path,tmp_path/path)
    for project in ('Conformance.Tests','Conformance.Portable.Tests','Contracts.Tests','IntegrationTests'):
        path=f'tests/Hexalith.Conversations.{project}/Hexalith.Conversations.{project}.csproj'
        (tmp_path/path).parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/path,tmp_path/path)
    path=tmp_path/adoption.APPROVED_ASSERTION_CORRECTIONS[0]['path'];original=path.read_bytes()
    if fault=='count':path.write_bytes(original.replace(b'ShouldBe(12);',b'ShouldBe(13);'))
    if fault=='other-assertion':path.write_bytes(original.replace(b'.ShouldBeEmpty();',b'.ShouldNotBeEmpty();',1))
    if fault=='binding':binding['approvedAssertionCorrections'][0]['count']=2
    if fault=='snapshot':entry['approvedAssertionCorrections'][0]['after']='submodulePaths.Length.ShouldBe(13);'
    if fault in ('root-substitution','existing-substitution'):
        row=next(row for row in entry['readers'] if row['path'].endswith('/PreservationTraceabilityManifestValidationTest.cs'))
        row['substitutions'][-1 if fault=='root-substitution' else 0]['after']+=' // unapproved plumbing'
    monkeypatch.setattr(adoption,'snapshot',lambda _:entry)
    result=current.empty_result('a'*40);adoption.reader_checks(tmp_path,result,binding,historical=False);observed=adoption.finish(result)
    assert observed['result']==('PASS' if fault=='none' else 'FAIL')
    assert observed['blockers']==([] if fault=='none' else ['ASSERTION_STRENGTH_WEAKENED'])
    assert observed['assertionLedger']


@pytest.mark.parametrize('fault',['none','package','project','assembly','publish','pack'])
def test_neutral_helper_portable_surface_stays_closed(tmp_path,monkeypatch,fault):
    import verify_conformance_tiering as tiering
    project=tmp_path/tiering.PROJECTS['portable'];project.parent.mkdir(parents=True);project.write_text('<Project />')
    helper='tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj'
    paths=[f'src/{name}/{name}.csproj' for name in ('Hexalith.Conversations.Client','Hexalith.Conversations.Contracts','Hexalith.Conversations.Testing')]+[helper]
    initial=[dict(FullPath=str(tmp_path/path),DefiningProjectFullPath=str(project)) for path in paths]
    graphs={}
    for path in paths:
        name=Path(path).stem;output=tmp_path/'bin'/f'{name}.dll';output.parent.mkdir(exist_ok=True);output.write_bytes(b'fixture assembly')
        graphs[path]=dict(Properties=dict(AssemblyName=name,IsPackable='false' if path==helper else 'true',
            IsPublishable='false',TargetPath=str(output)),Items=dict(ProjectReference=[],PackageReference=[],Reference=[],ReferencePath=[],
                FrameworkReference=[dict(Identity='Microsoft.NETCore.App',IsImplicitlyDefined='true')]))
    graph=graphs[helper]
    if fault=='publish':graph['Properties']['IsPublishable']='true'
    elif fault=='pack':graph['Properties']['IsPackable']='true'
    elif fault!='none':graph['Items'][{'package':'PackageReference','project':'ProjectReference','assembly':'Reference'}[fault]]=[dict(Identity='forbidden')]
    assets=tmp_path/'obj/project.assets.json';assets.parent.mkdir();assets.write_text(json.dumps(dict(targets={'net10.0':{
        'Hexalith.Conversations.Client/1.0':dict(type='project',compile={'lib/Hexalith.Conversations.Client.dll':{}})}})))
    portable=dict(Properties=dict(ProjectAssetsFile=str(assets)),Items=dict(ProjectReference=initial,PackageReference=[],
        ReferencePath=[dict(FullPath=graphs[paths[0]]['Properties']['TargetPath'],FusionName='Hexalith.Conversations.Client, Version=1.0.0.0')]))
    monkeypatch.setattr(tiering,'msbuild',lambda _root,path,*args,**kwargs:copy.deepcopy(graphs[path]))
    if fault=='none':
        observed=tiering.portable_surface(tmp_path,dict(portable=portable))
        assert len(observed['evaluatedProjects'])==4
        assert next(row for row in observed['evaluatedProjects'] if row['project']==helper)==dict(
            project=helper,assembly='Hexalith.Conversations.TestSupport',packable=False)
        return
    with pytest.raises(tiering.VerificationError) as caught:
        tiering.portable_surface(tmp_path,dict(portable=portable))
    assert caught.value.code=='PORTABLE_TIER_NONPORTABLE_REFERENCE'
    assert str(caught.value)=='Evaluated non-packable module reference: Hexalith.Conversations.TestSupport'


@pytest.mark.parametrize('ownership_failure',['history','index'])
def test_adoption_output_ownership_failure_preserves_receipt(adoption_git,monkeypatch,capsys,ownership_failure):
    root,before,candidate=adoption_git
    output=root/'artifacts/v9/10.4/AC-10.4-09.json';output.parent.mkdir(parents=True,exist_ok=True)
    original=b'owned sentinel receipt\r\n';output.write_bytes(original)
    result=current.empty_result(candidate);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:adoption.finish(copy.deepcopy(result)))
    monkeypatch.setattr(adoption,'BASELINE',before if ownership_failure=='index' else 'f'*40)
    index=root/'.git/index';saved=index.read_bytes()
    try:
        if ownership_failure=='index':index.write_bytes(b'invalid index')
        exit_code=adoption.main(['--repository',str(root),'--candidate',candidate,'--output',output.relative_to(root).as_posix()])
        observed=json.loads(capsys.readouterr().out)
        assert exit_code==(2 if ownership_failure=='history' else 1)
        assert observed['result']==('BLOCKED' if ownership_failure=='history' else 'FAIL')
        assert observed['blockers']==(['REVISION_INVALID'] if ownership_failure=='history' else ['HELPER_FAILURE'])
        assert observed['assertionLedger'] and output.read_bytes()==original
    finally:index.write_bytes(saved);output.unlink()


@pytest.mark.parametrize('alias',['gitfile','external-gitdir'])
def test_adoption_output_git_admin_hardlink_cannot_overwrite(adoption_git,tmp_path,monkeypatch,capsys,alias):
    root,before,candidate=adoption_git
    worktree=tmp_path/'worktree'
    subprocess.run(['git','-C',str(root),'worktree','add','--detach',str(worktree),candidate],check=True,capture_output=True)
    gitfile=worktree/'.git';admin=Path(gitfile.read_text().strip().removeprefix('gitdir: '))
    source=gitfile if alias=='gitfile' else admin/'HEAD';original=source.read_bytes()
    output=worktree/'artifacts/v9/10.4/AC-10.4-09.json';output.parent.mkdir(parents=True);output.hardlink_to(source)
    result=current.empty_result(candidate);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:adoption.finish(copy.deepcopy(result)))
    monkeypatch.setattr(adoption,'BASELINE',before)
    try:
        assert adoption.main(['--repository',str(worktree),'--candidate',candidate,'--output',output.relative_to(worktree).as_posix()])==1
        observed=json.loads(capsys.readouterr().out)
        assert observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
        assert source.read_bytes()==original
    finally:
        output.unlink();subprocess.run(['git','-C',str(root),'worktree','remove','--force',str(worktree)],check=True,capture_output=True)
