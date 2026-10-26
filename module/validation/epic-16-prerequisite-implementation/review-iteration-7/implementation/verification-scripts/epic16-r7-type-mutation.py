from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time,re
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-7/implementation/mutations-version-type';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json'])
n='_bmad/scripts/epic16_native.py';s='_bmad/scripts/epic16_schedules.py';m='_bmad/scripts/epic16_measurements.py';l='_bmad/scripts/epic16_lifecycle.py';d='_bmad/scripts/epic16_domain.py'
mutants=[]
def replace(name,path,old,new,selection):mutants.append(dict(name=name,path=path,old=old,new=new,selection=selection))
def drop(name,path,fragment,selection):
 lines=[line for line in (root/path).read_text().splitlines() if fragment in line];assert len(lines)==1,(name,lines)
 line=lines[0];replace(name,path,line,line[:len(line)-len(line.lstrip())]+'pass # removed validation in disposable mutation',selection)
replace('helper-policy-propagation',n,'            (directory / name).write_bytes(data)',"            if name != POLICY_PATH: (directory / name).write_bytes(data)",'round7_native_helper_propagates')
drop('helper-selected-sdk',n,'validate_sdk(sources[POLICY_PATH], selected)','round7_runtime_consumer_blocks and (wrong-feature-band or older-patch)')
replace('helper-runtime-provenance',n,'return dict(policySha256=hashlib.sha256(policy).hexdigest(), sdk=sdk,',"return dict(policySha256=hashlib.sha256(policy).hexdigest(), sdk='10.0.999',",'round7_native_helper_propagates')
attrs=(root/'.gitattributes').read_text();lines='\n'.join(line for line in attrs.splitlines() if ('.dll ' in line or '.dll.bytes ' in line) and '-diff' in line)+'\n';replace('binary-diff-overrides','.gitattributes',lines,'','round7_retained_pe')
mutants.append(dict(name='native-nullable-schema',path=tooling.SCHEMA,kind='schema',selection='round7_native_nullable_creation'))
replace('terminal-closed-branch',d,"if state['lifecycle'] != 'Archived': state['lifecycle']='Closed'",'pass # lifecycle assignment removed','round7_terminal_native_wire')
replace('terminal-archived-branch',d,"elif kind == 'ConversationArchived': state['lifecycle']='Archived'","elif kind == 'ConversationArchived': pass",'round7_terminal_native_wire')
drop('authoritative-stream-caller',m,'lifecycle.stream_boundaries(row)','round7_rebuild_boundaries')
drop('authoritative-event-position',l,"'position binds complete exact-next authoritative event identity'",'round7_rebuild_boundaries and (wrong-event-id or wrong-envelope or position-hash)')
drop('derived-b-c-cutoffs',l,"'B and C sequence cutoffs derive from authoritative global positions'",'round7_rebuild_boundaries and beyond-b')
drop('erase-reference-bijection',m,'lifecycle.erase_reference_identity(deliveries)','round7_erase_reference and (same-location or same-position)')
drop('lifecycle-stage-caller',m,'lifecycle.lifecycle_transaction(r)','round7_lifecycle_cas and (no-execution or wrong-fence or partial or stale-succeeds)')
drop('lifecycle-source-state',l,"'source-derived complete lifecycle state and component presence at each AD-7 boundary'",'round7_lifecycle_self_consistent and coherent-content')
drop('lifecycle-version-advancement',l,"'each executed lifecycle CAS advances version and opaque etag'",'round7_lifecycle_self_consistent and coherent-version')
replace('lifecycle-distinct-commits',l,"require(transaction not in transactions, 'distinct entry work and completion durable commits');",'pass;', 'round7_lifecycle_self_consistent and coherent-transaction')
drop('lifecycle-executed-cas',l,"'actual EventStore CAS attempt accepts current lifecycle plan'",'round7_lifecycle_cas and (no-execution or wrong-fence)')
drop('lifecycle-atomic-component-commit',l,"'one transaction atomically commits each planned index and component boundary'",'round7_lifecycle_cas and (partial or mixed-transaction)')
drop('lifecycle-stale-preservation',l,"'stale attempt preserves every newer component index generation and watermark'",'round7_lifecycle_cas and stale-after')
drop('lifecycle-stale-attempt',l,"'stale writer actually attempts its earlier captured entry plan'",'round7_lifecycle_cas and (stale-unexecuted or stale-fence)')
drop('lifecycle-stale-rejection',l,"'newer fence rejects stale lifecycle plan without partial writes'",'round7_lifecycle_cas and stale-writes')
drop('lifecycle-commit-order',l,"'lifecycle commits execute in captured order'",'round7_lifecycle_retains_ad7 and commit-order')
# Only the comparison is changed; each new branch otherwise isolates the named fence.
for field in ('lease','checkpoint'):
 old="admissible = same(expected,{key:newer[key] for key in FENCE_FIELDS}) and not expired"
 new="admissible = same({key:expected[key] for key in FENCE_FIELDS if key!='"+field+"'},{key:newer[key] for key in FENCE_FIELDS if key!='"+field+"'}) and not expired"
 replace('isolated-recovery-'+field,s,old,new,'round7_recovery_independently and valid')
drop('commit-before-ack-order',l,"'commit precedes ack by operation order even when clock ticks coincide'",'round7_commit_ack_order and reversed')
drop('ack-durable-transaction',l,"'ack observes the successful durable transaction'",'round7_commit_ack_order and transaction')
drop('ack-persisted-state',l,"'operation observations bind actual persisted identity and state'",'round7_commit_ack_order and persisted')
drop('ack-durable-success',l,"'ack requires successful durable commit'",'round7_commit_ack_order and failed')
drop('horizon-schedule-caller',m,'schedules.horizon_extension(data)','round7_recovery_requires')
drop('horizon-earlier-worker-binding',s,"'earlier worker captured before the observed extension'",'round7_recovery_requires and old-worker-unbound')
drop('horizon-executed-cas',s,"'observed gap CAS attempt binds complete worker expectation'",'round7_recovery_requires and (merge-unexecuted or stale-no-attempt)')
drop('horizon-stale-preserves-enlarged-state',s,"'stale gap completion preserves complete enlarged persisted state'",'round7_recovery_requires and stale-mutates')
drop('horizon-current-full-state',s,"'current completion proves consumed evidence and establishes derived access state'",'round7_recovery_requires and completion-allowed')
replace('replay-consumes-pending-before-healthy',s,"projection=recovered,pending=[],version=prior['version']+1","projection=recovered,version=prior['version']+1",'round7_recovery_requires and completion-pending')
mutants[-1]['alsoDrop']="'AD-6 completion CAS observes no remaining pending fingerprint'"
mutants=[dict(name='capture-integer-tokens',path=tooling.SCHEMA,kind='schema',selection='round7_capture_versions_require')]
results=[]
for mutant in mutants:
 name,path,selection=mutant['name'],mutant['path'],mutant['selection']
 workspace=Path(tempfile.mkdtemp(prefix='epic16-review7-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 target=workspace/path;content=target.read_bytes();new=content
 if mutant.get('kind')=='schema':
  document=json.loads(content)
  def visit(value):
   if isinstance(value,dict):
    if type(value.get('const')) is int:value.pop('type',None)
    for child in value.values():visit(child)
   elif isinstance(value,list):
    for child in value:visit(child)
  visit(document);new=(json.dumps(document,indent=2)+'\n').encode()
  trust=workspace/'_bmad/scripts/epic16_trust.py';trust.write_text(re.sub(r"SCHEMA_PIN = '[a-f0-9]+'","SCHEMA_PIN = '"+hashlib.sha256(new).hexdigest()+"'",trust.read_text()))
 else:
  assert mutant['old'].encode() in content,name
  new=content.replace(mutant['old'].encode(),mutant['new'].encode())
  if 'alsoDrop' in mutant:
   lines=new.decode().splitlines();matched=[line for line in lines if mutant['alsoDrop'] in line];assert len(matched)==1
   line=matched[0];new=new.replace(line.encode(),(line[:len(line)-len(line.lstrip())]+'pass # companion pending prerequisite removed').encode())
 target.write_bytes(new)
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=300)
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]: (out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(new).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(name,result.returncode,counts,flush=True)
 assert result.returncode==1 and counts['failures']>0 and counts['errors']==0,(name,result.stdout.decode(),result.stderr.decode())
