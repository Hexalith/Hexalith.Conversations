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
    root,before,candidate=adoption_git;begin='<!-- CHAIN:BEGIN -->';end='<!-- CHAIN:END -->'
    rows=['| 10.1 | 10.2 |','| 10.2 | 10.3 |','| 10.3 | 10.4 |'];original=begin+'\n'+'\n'.join(rows)+'\n'+end
    path=root/'chain.md';path.write_text(original);digest=hashlib.sha256(path.read_bytes()).hexdigest()
    changed={'CHAIN_CHANGED':original.replace('10.2 | 10.3','10.2 | 10.4'),
      'CHAIN_MISSING':original.replace(rows[1]+'\n',''),'CHAIN_REORDERED':original.replace('\n'.join(rows),'\n'.join(reversed(rows))),
      'CHAIN_NESTED':original.replace(begin,begin+'\n'+begin),'CHAIN_DUPLICATE':original+'\n'+original,
      'CHAIN_UNTERMINATED':original.replace(end,''),'CHAIN_DUPLICATE_ROW':original.replace(rows[0],rows[0]+'\n'+rows[0])}[fault]
    try:
        path.write_text(changed)
        observed=current.bridge(root,dict(baseline=before,candidate=candidate,chains=[dict(id='frozen-chain',source=path.read_text(),begin=begin,end=end,rows=rows)]))
    finally:path.write_text(original)
    assert observed['blockers']==[code]
    observed_fault(record_property,fault,code,observed,digest,hashlib.sha256(path.read_bytes()).hexdigest())

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


@pytest.mark.parametrize('fault',['none','count','other-assertion','binding','snapshot'])
def test_approved_assertion_correction_preserves_every_other_entry_byte(tmp_path,monkeypatch,fault):
    """Only the exact owner-approved count change may differ from the plumbing migration."""
    binding=json.loads((ROOT/adoption.BINDING).read_bytes());entry=adoption.snapshot(ROOT)
    historical=json.loads(subprocess.check_output(['git','-C',str(ROOT),'show',
        '4e937361613c2dba72c46595226219017e6ba524:'+adoption.SNAPSHOT]))
    assert [(r['path'],r['entryBase64'],r['entrySha256'],r['substitutions']) for r in entry['readers']]==[
        (r['path'],r['entryBase64'],r['entrySha256'],r['substitutions']) for r in historical['readers']]
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
    monkeypatch.setattr(adoption,'snapshot',lambda _:entry)
    result=current.empty_result('a'*40);adoption.reader_checks(tmp_path,result,binding,historical=False);observed=adoption.finish(result)
    assert observed['result']==('PASS' if fault=='none' else 'FAIL')
    assert observed['blockers']==([] if fault=='none' else ['ASSERTION_STRENGTH_WEAKENED'])
    assert observed['assertionLedger']


@pytest.mark.parametrize('fault',['package','project','assembly','publish','pack'])
def test_neutral_helper_portable_surface_stays_closed(tmp_path,monkeypatch,fault):
    import verify_conformance_tiering as tiering
    project=tmp_path/tiering.PROJECTS['portable'];project.parent.mkdir(parents=True);project.write_text('<Project />')
    helper='tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj'
    paths=[f'src/{name}/{name}.csproj' for name in ('Hexalith.Conversations.Client','Hexalith.Conversations.Contracts','Hexalith.Conversations.Testing')]+[helper]
    initial=[dict(FullPath=str(tmp_path/path),DefiningProjectFullPath=str(project)) for path in paths]
    graph=dict(Properties=dict(AssemblyName='Hexalith.Conversations.TestSupport',IsPackable='false',IsPublishable='false'),
        Items=dict(ProjectReference=[],PackageReference=[],Reference=[],ReferencePath=[],FrameworkReference=[dict(Identity='Microsoft.NETCore.App',IsImplicitlyDefined='true')]))
    if fault=='publish':graph['Properties']['IsPublishable']='true'
    elif fault=='pack':graph['Properties']['IsPackable']='true'
    else:graph['Items'][{'package':'PackageReference','project':'ProjectReference','assembly':'Reference'}[fault]]=[dict(Identity='forbidden')]
    monkeypatch.setattr(tiering,'msbuild',lambda *args,**kwargs:graph)
    with pytest.raises(tiering.VerificationError) as caught:
        tiering.portable_surface(tmp_path,dict(portable=dict(Items=dict(ProjectReference=initial))))
    assert caught.value.code=='PORTABLE_TIER_NONPORTABLE_REFERENCE'


@pytest.fixture(scope='module')
def approved_gitlink_repository(tmp_path_factory):
    """Use immutable baseline objects without opening or changing dependency checkouts."""
    root=tmp_path_factory.mktemp('approved-gitlink-amendment')/'repository'
    subprocess.run(['git','clone','--shared','--no-checkout','--quiet',str(ROOT),str(root)],check=True,capture_output=True)
    def git(*args,input=None):
        return subprocess.run(['git','-C',str(root),*args],input=input,check=True,capture_output=True).stdout.decode().strip()
    git('config','user.name','Fixture');git('config','user.email','fixture@example.invalid')
    return root,git


@pytest.mark.parametrize('fault',['none','missing','extra','before','after','source-mode','destination-mode','approval','date'])
def test_approved_gitlink_amendment_cannot_redefine_compiled_trust_roots(approved_gitlink_repository,fault):
    root,git=approved_gitlink_repository
    git('read-tree',adoption.BASELINE)
    for row in adoption.APPROVED_GITLINK_AMENDMENT['transitions']:
        git('update-index','--cacheinfo',row['destinationMode'],row['destinationObject'],row['path'])
    candidate=git('commit-tree',git('write-tree'),'-p',adoption.BASELINE,'-m','test: exact approved transitions')
    declared=copy.deepcopy(adoption.APPROVED_GITLINK_AMENDMENT)
    if fault=='missing':declared['transitions'].pop()
    if fault=='extra':declared['transitions'].append(dict(declared['transitions'][-1],path='references/Hexalith.Projects'))
    if fault=='before':declared['transitions'][0]['sourceObject']='a'*40
    if fault=='after':declared['transitions'][0]['destinationObject']='a'*40
    if fault=='source-mode':declared['transitions'][0]['sourceMode']='100644'
    if fault=='destination-mode':declared['transitions'][0]['destinationMode']='100644'
    if fault=='approval':declared['approvalCommit']='a'*40
    if fault=='date':declared['decisionDate']='2026-10-18'
    observed=current.bridge(root,dict(baseline=adoption.BASELINE,candidate=candidate,story104GitlinkAmendment=declared))
    assert observed['result']==('PASS' if fault=='none' else 'FAIL')
    assert observed['blockers']==([] if fault=='none' else ['EVIDENCE_BOUNDARY_GITLINK'])
    assert observed['assertionLedger']


@pytest.mark.parametrize('fault',['missing','extra','endpoint','raw-mode','unchanged-root','unamended','wrong-baseline'])
def test_approved_gitlink_amendment_checks_real_raw_git_diffs(approved_gitlink_repository,fault):
    root,git=approved_gitlink_repository
    git('read-tree',adoption.BASELINE)
    rows=copy.deepcopy(adoption.APPROVED_GITLINK_AMENDMENT['transitions'])
    if fault=='missing':rows.pop()
    if fault in ('extra','unchanged-root'):
        rows.append(dict(path='references/Hexalith.Projects',destinationMode='160000',destinationObject=adoption.BASELINE))
    if fault=='endpoint':rows[0]['destinationObject']=adoption.BASELINE
    if fault=='raw-mode':
        rows[0].update(destinationMode='100644',destinationObject=git('hash-object','-w','--stdin',input=b'regular file\n'))
    for row in rows:git('update-index','--cacheinfo',row['destinationMode'],row['destinationObject'],row['path'])
    candidate=git('commit-tree',git('write-tree'),'-p',adoption.BASELINE,'-m','test: reject unapproved transition drift')
    request=dict(baseline=candidate if fault=='wrong-baseline' else adoption.BASELINE,candidate=candidate)
    if fault!='unamended':request['story104GitlinkAmendment']=adoption.APPROVED_GITLINK_AMENDMENT
    observed=current.bridge(root,request)
    assert observed['result']=='FAIL'
    assert observed['blockers']==['EVIDENCE_BOUNDARY_GITLINK']
    assert observed['assertionLedger']


@pytest.mark.parametrize('fault',['none','missing','extra','before','after','source-mode','destination-mode','baseline','candidate','unchanged-root','all-missing'])
def test_approved_gitlink_amendment_binding_and_candidate_stay_independent(fault):
    binding=json.loads((ROOT/adoption.BINDING).read_bytes())
    original=copy.deepcopy(binding['rootGitlinks']);candidate=adoption.candidate_gitlinks(original)
    diff=[dict(row,status='M',sourcePath=None) for row in adoption.APPROVED_GITLINK_AMENDMENT['transitions']]
    amendment=binding['approvedGitlinkAmendment']
    if fault=='missing':amendment['transitions'].pop()
    if fault=='extra':amendment['transitions'].append(dict(amendment['transitions'][-1],path='references/Hexalith.Projects'))
    if fault=='before':amendment['transitions'][0]['sourceObject']='a'*40
    if fault=='after':amendment['transitions'][0]['destinationObject']='a'*40
    if fault=='source-mode':amendment['transitions'][0]['sourceMode']='100644'
    if fault=='destination-mode':amendment['transitions'][0]['destinationMode']='100644'
    if fault=='baseline':binding['rootGitlinks'][0]['commit']='a'*40
    if fault=='candidate':candidate[1]['commit']='a'*40
    if fault=='unchanged-root':candidate[0]['commit']='a'*40
    if fault=='all-missing':diff=[];candidate=original
    result=current.empty_result('b'*40)
    result['changedPaths']=[row['path'] for row in diff]
    result['gitlinks']=[{key:row[key] for key in ('path','sourceMode','destinationMode')} for row in diff]
    adoption.gitlink_checks(result,binding,original,candidate,diff);observed=adoption.finish(result)
    assert observed['changedPaths']==[row['path'] for row in diff]
    if fault=='none':assert observed['gitlinks']==[]
    elif fault!='all-missing':assert observed['gitlinks']==result['gitlinks'] and observed['gitlinks']
    assert observed['result']==('PASS' if fault=='none' else 'FAIL')
    assert observed['blockers']==([] if fault=='none' else ['EVIDENCE_BOUNDARY_GITLINK'])
    assert observed['assertionLedger']


def test_approved_gitlink_amendment_retains_valid_non_applicable_state(approved_gitlink_repository):
    root,git=approved_gitlink_repository
    # No governed source and no raw gitlink changes: this is a real non-applicable diff.
    git('read-tree',adoption.BASELINE)
    blob=git('hash-object','-w','--stdin',input=b'unrelated\n')
    git('update-index','--add','--cacheinfo','100644',blob,'unrelated.txt')
    candidate=git('commit-tree',git('write-tree'),'-p',adoption.BASELINE,'-m','test: unrelated scope')
    path=root/adoption.BINDING;path.parent.mkdir(parents=True,exist_ok=True)
    path.write_bytes((ROOT/adoption.BINDING).read_bytes())
    observed=adoption.verify(root,adoption.BASELINE,candidate,authority_bundle=adoption.PROVENANCE[4])
    assert observed['result']=='not-applicable'
    assert observed['blockers']==[]
    assert observed['warnings']==['EVIDENCE_SCOPE_NOT_APPLICABLE']


def test_approved_gitlink_amendment_complete_verifier_preserves_result_schema(tmp_path):
    """A real committed scope passes the complete verifier/finalizer without changing v1 schema."""
    root=tmp_path/'repository'
    pair=ROOT/'docs/release-evidence/story-10.4-final-record-v2.json'
    # Once published, reconstruct the original source scope rather than following
    # later receipt/lifecycle commits or unrelated changes on the live branch.
    source=json.loads(pair.read_bytes())['candidate']['commit'] if pair.is_file() else subprocess.check_output(
        ['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip()
    subprocess.run(['git','clone','--shared','--no-checkout','--quiet',str(ROOT),str(root)],check=True,capture_output=True)
    def git(*args):
        return subprocess.run(['git','-C',str(root),*args],check=True,capture_output=True).stdout.decode().strip()
    git('checkout','--detach',source)
    git('config','user.name','Fixture');git('config','user.email','fixture@example.invalid')
    for path in adoption.APPROVED_SOURCE_PATHS:
        (root/path).parent.mkdir(parents=True,exist_ok=True)
        (root/path).write_bytes((ROOT/path).read_bytes())
    git('add','--force','--',*adoption.APPROVED_SOURCE_PATHS)
    candidate=git('commit-tree',git('write-tree'),'-p',git('rev-parse','HEAD'),'-m','test: freeze the scoped gitlink fixture')
    git('update-ref','HEAD',candidate)
    observed=adoption.verify(root,adoption.BASELINE,candidate,authority_bundle=adoption.PROVENANCE[4],verify_workflows=True)
    assert observed['result']=='PASS'
    assert observed['blockers']==[] and observed['gitlinks']==[]
    approved={row['path'] for row in adoption.APPROVED_GITLINK_AMENDMENT['transitions']}
    assert approved.issubset(observed['changedPaths'])
    assert all(row['state']=='PASS' for row in observed['assertionLedger'])
    assert {row['id'] for row in observed['assertionLedger'] if row['id'].startswith('gitlinks:')}=={
        'gitlinks:amendment','gitlinks:transitions','gitlinks:provenance','gitlinks:candidate'}
    wrong_baseline=git('commit-tree',git('rev-parse',adoption.BASELINE+'^{tree}'),'-p',adoption.BASELINE,
        '-m','test: freeze the scoped gitlink fixture')
    wrong=adoption.verify(root,wrong_baseline,candidate,authority_bundle=adoption.PROVENANCE[4],verify_workflows=True)
    assert wrong['result']=='FAIL'
    assert set(wrong['blockers'])=={'AUTHORITY_BINDING_INVALID','EVIDENCE_BOUNDARY_GITLINK'}
    assert {row['path'] for row in wrong['gitlinks']}==approved
    path=root/adoption.BINDING;original=path.read_bytes();binding=json.loads(original)
    binding['approvedGitlinkAmendment']['transitions'][0]['destinationObject']='a'*40
    try:
        path.write_text(json.dumps(binding))
        invalid=adoption.verify(root,adoption.BASELINE,candidate,authority_bundle=adoption.PROVENANCE[4],verify_workflows=True)
        assert invalid['result']=='FAIL'
        assert set(invalid['blockers'])=={'EVIDENCE_BOUNDARY_GITLINK','EVIDENCE_HASH_MISMATCH'}
        assert {row['path'] for row in invalid['gitlinks']}==approved
        assert any(row['state']=='FAIL' for row in invalid['assertionLedger'])
    finally:path.write_bytes(original)
    assert hashlib.sha256(path.read_bytes()).digest()==hashlib.sha256(original).digest()


@pytest.mark.parametrize('fault,code',[('git','GIT_UNAVAILABLE'),('baseline','REVISION_INVALID'),('candidate','REVISION_INVALID')])
def test_approved_gitlink_amendment_unavailable_history_stays_blocked(approved_gitlink_repository,monkeypatch,fault,code):
    """Unavailable facts must survive the complete amendment verifier/finalizer as BLOCKED."""
    root,git=approved_gitlink_repository
    bridge=current.bridge
    def unavailable(repository,request):
        request=dict(request)
        if fault=='git':request['gitExecutable']='missing-story104-approved-git-executable'
        else:request[fault]='f'*40
        return bridge(repository,request)
    monkeypatch.setattr(current,'bridge',unavailable)
    observed=adoption.verify(root,adoption.BASELINE,adoption.BASELINE,authority_bundle=adoption.PROVENANCE[4],verify_workflows=True)
    assert observed['result']=='BLOCKED'
    assert observed['blockers']==[code]
    assert any(row['state']=='SKIPPED' for row in observed['assertionLedger'])
    assert not any(row['state']=='FAIL' for row in observed['assertionLedger'])


@pytest.mark.parametrize('row_id',['output:tree','output:head','output:head-tree','output:index','output:index-completed'])
@pytest.mark.parametrize('code,state,exit_code',[('HELPER_FAILURE','FAIL',1),('HISTORY_UNAVAILABLE','SKIPPED',2)])
def test_approved_gitlink_amendment_output_requires_available_ownership(tmp_path,monkeypatch,capsys,row_id,code,state,exit_code):
    output='artifacts/v9/10.4/AC-10.4-09.json';target=tmp_path/output
    target.parent.mkdir(parents=True);target.write_bytes(b'preserved receipt\n');original=target.read_bytes()
    result=current.empty_result('a'*40);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    result=adoption.finish(result)
    required=['output:tree','output:head','output:head-tree','output:index','output:index-completed']
    rows=[dict(id=identity,subject='ownership',state=state if identity==row_id else 'PASS') for identity in required]
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:copy.deepcopy(result))
    monkeypatch.setattr(current,'bridge',lambda *args,**kwargs:dict(result='BLOCKED' if state=='SKIPPED' else 'FAIL',
        assertionLedger=rows,blockers=[code],headPaths=[],indexPaths=[],treePaths=[],
        containedPaths=[dict(code=None,fullPath=str(target))]))
    assert adoption.main(['--repository',str(tmp_path),'--output',output])==exit_code
    observed=json.loads(capsys.readouterr().out)
    assert observed['result']==('BLOCKED' if state=='SKIPPED' else 'FAIL')
    assert observed['blockers']==[code]
    assert target.read_bytes()==original


def test_approved_gitlink_amendment_output_requires_evaluated_candidate_tree(tmp_path,monkeypatch,capsys):
    output='artifacts/v9/10.4/AC-10.4-09.json';target=tmp_path/output
    target.parent.mkdir(parents=True);target.write_bytes(b'preserved candidate receipt\n');original=target.read_bytes()
    result=current.empty_result('a'*40);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    result=adoption.finish(result)
    rows=[dict(id=identity,subject='ownership',state='PASS') for identity in ['output:head','output:head-tree','output:index','output:index-completed']]
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:copy.deepcopy(result))
    monkeypatch.setattr(current,'bridge',lambda *args,**kwargs:dict(result='PASS',assertionLedger=rows,blockers=[],
        headPaths=[],indexPaths=[],treePaths=[],containedPaths=[dict(code=None,fullPath=str(target))]))
    assert adoption.main(['--repository',str(tmp_path),'--output',output])==2
    observed=json.loads(capsys.readouterr().out)
    assert observed['result']=='BLOCKED' and observed['blockers']==['HELPER_UNAVAILABLE']
    assert target.read_bytes()==original


def test_approved_gitlink_amendment_output_missing_git_preserves_real_tracked_receipt(approved_gitlink_repository,monkeypatch,capsys):
    root,git=approved_gitlink_repository;output='artifacts/v9/10.4/AC-10.4-09.json';target=root/output
    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b'committed receipt must survive\n');original=target.read_bytes()
    git('read-tree',adoption.BASELINE)
    blob=git('hash-object','-w','--stdin',input=original)
    git('update-index','--add','--cacheinfo','100644',blob,output)
    candidate=git('commit-tree',git('write-tree'),'-p',adoption.BASELINE,'-m','test: freeze the scoped gitlink fixture')
    git('update-ref','HEAD',candidate)
    assert output in git('ls-tree','-r','--name-only','HEAD').splitlines()
    assert output in git('ls-files').splitlines()
    bridge=current.bridge
    monkeypatch.setattr(current,'bridge',lambda repository,request:bridge(repository,
        dict(request,gitExecutable='missing-story104-output-git-executable')))
    assert adoption.main(['--repository',str(root),'--candidate',candidate,'--output',output])==2
    observed=json.loads(capsys.readouterr().out)
    assert observed['result']=='BLOCKED' and observed['blockers']==['GIT_UNAVAILABLE']
    assert target.read_bytes()==original


def test_approved_gitlink_amendment_output_can_publish_fail_with_proven_ownership(tmp_path,monkeypatch,capsys):
    output='artifacts/v9/10.4/AC-10.4-09.json';target=tmp_path/output
    result=current.empty_result('a'*40);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='FAIL')]
    result['blockers']=['EVIDENCE_HASH_MISMATCH'];result=adoption.finish(result)
    required=['output:tree','output:head','output:head-tree','output:index','output:index-completed']
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:copy.deepcopy(result))
    monkeypatch.setattr(current,'bridge',lambda *args,**kwargs:dict(result='PASS',
        assertionLedger=[dict(id=identity,subject='ownership',state='PASS') for identity in required],blockers=[],
        headPaths=[],indexPaths=[],treePaths=[],containedPaths=[dict(code=None,fullPath=str(target))]))
    assert adoption.main(['--repository',str(tmp_path),'--output',output])==1
    observed=json.loads(capsys.readouterr().out)
    assert observed['result']=='FAIL' and observed['blockers']==['EVIDENCE_HASH_MISMATCH']
    assert target.read_bytes()==current.canonical(observed)+b'\n'


def test_approved_gitlink_amendment_output_checks_resolved_candidate_ownership(tmp_path,monkeypatch,capsys):
    output='artifacts/v9/10.4/AC-10.4-09.json';target=tmp_path/output
    target.parent.mkdir(parents=True);target.write_bytes(b'candidate-owned receipt\n');original=target.read_bytes()
    result=current.empty_result('a'*40);result['assertionLedger']=[dict(id='fixture',subject='fixture',state='PASS')]
    result=adoption.finish(result)
    required=['output:tree','output:head','output:head-tree','output:index','output:index-completed']
    monkeypatch.setattr(adoption,'verify',lambda *args,**kwargs:copy.deepcopy(result))
    def ownership(repository,request):
        assert request['candidate']==result['candidate']
        return dict(result='PASS',assertionLedger=[dict(id=identity,subject='ownership',state='PASS') for identity in required],
            blockers=[],headPaths=[],indexPaths=[],treePaths=[output],containedPaths=[dict(code=None,fullPath=str(target))])
    monkeypatch.setattr(current,'bridge',ownership)
    assert adoption.main(['--repository',str(tmp_path),'--candidate','HEAD','--output',output])==1
    observed=json.loads(capsys.readouterr().out)
    assert observed['blockers']==['EVIDENCE_OUTPUT_FORBIDDEN']
    assert target.read_bytes()==original
