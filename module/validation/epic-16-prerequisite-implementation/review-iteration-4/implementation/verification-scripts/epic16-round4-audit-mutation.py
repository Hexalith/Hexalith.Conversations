from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time,ast
from xml.etree import ElementTree as ET
root=Path('/home/administrator/projects/hexalith/conversations');sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/post-audit/mutations';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json'])
node=ast.parse(Path('/tmp/epic16-review3-final-utc-mutations.py').read_text());prior=next(ast.literal_eval(n.value) for n in node.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='mutants' for t in n.targets))
mutants=[]
for name,path,old,new,selection in prior:
 old=old.replace("value['items'] == expected_items", "domain.same(value['items'], expected_items)").replace("data['persistedProjection'] == dict(sequence=data['afterCheckpoint'], state=recovered)","domain.same(data['persistedProjection'], dict(sequence=data['afterCheckpoint'], state=recovered))")
 mutants.append((name,path,old,new,selection))
r='_bmad/scripts/epic16_relationships.py';m='_bmad/scripts/epic16_measurements.py';d='_bmad/scripts/epic16_domain.py';t='_bmad/scripts/epic16_tooling.py'
mutants += [
 ('recursive-types',d,'    if type(left) is not type(right): return False','    pass # mutant','round4_recursive_domain and occurredAtUtcTicks'),
 ('raw-sequence-domains',d,"    require(type(value) is int and (0 if checkpoint else 1) <= value <= MAX_SEQUENCE, 'representable native Int64 sequence')",'    pass # mutant','round4_raw_fault_sequence_domains'),
 ('configuration-keys',d,"isinstance(value, str) and len(value) > 0", "isinstance(value, str) and bool(value.strip())",'round4_actual_configuration_whitespace'),
 ('configuration-values',d,"state['configuration'][configuration_key(value['key'])]=value['value']","state['configuration'][configuration_key(value['key'])]=text(value['value'])",'round4_tenant_configuration_values'),
 ('general-tenant-dates',d,"if conversation: require(date.year >= 2000", "if True: require(date.year >= 2000",'round4_tenant_dates'),
 ('terminal-wire',d,"'ConversationClosed': ('reasonCode',), 'ConversationArchived': ('reasonCode',)","'ConversationClosed': (), 'ConversationArchived': ()",'round4_genuine_terminal'),
 ('tenant-isolation',m,'relationships.isolation(data)','pass # mutant','round4_measured_tenant_isolation'),
 ('complete-query-key',m,"data['authorityKey'] == relationships.index_key(data['tenant'])","data['authorityKey'].startswith('projection:conversations-index:')",'round4_query_authority'),
 ('conflict-inputs',m,'relationships.conflicts(list(conflicts.values()))','pass # mutant','round4_conflict_labels or round4_contract_malformed'),
 ('transport-time-inputs',m,"relationships.transport_time(data['transportTimeProof'])",'pass # mutant','round4_contract_time_precedence'),
 ('tenant-transition-inputs',m,"relationships.tenant_transitions(data['transitions'])",'pass # mutant','round4_tenant_transition or round4_last_accepted'),
 ('redaction-observations',m,"relationships.redaction(data['redactionProof'])",'pass # mutant','round4_redaction_preserves'),
 ('no-silent-governance',d,"else: raise ValueError('governance event requires separate bounded redaction experiment')",'else: pass # mutant','round4_general_replay'),
 ('emitted-data-inputs',m,"relationships.tenant_data_safety(data['tenantDataExperiment'])",'pass # mutant','round4_emitted_events_logs'),
 ('actual-canary-inspection',r,"require(all(canary not in raw and canary not in visible for canary in canaries.values()),'controlled tenant canaries absent from emitted bytes including nested values')",'pass # mutant','round4_actual_canary_scan'),
 ('catalog-inputs',m,'relationships.catalog(data)','pass # mutant','round4_catalog_bytes or round4_correctly_rehashed'),
 ('actual-catalog-framing',r,"return digest(b''.join(chunks))","return digest(canonical(routes))",'round4_genuine_terminal'),
 ('catalog-capabilities',r,"require(entry['fullReplayProjectionTypes']==['conversation-read-model'] and all(entry[key]==[] for key in subsets[2:]), 'existing controlled handler has only full-replay capability')",'pass # mutant','round4_catalog_capabilities_match'),
 ('catalog-enrollment',t,"need(len(servers)==1 and servers[0]['appId']==measurements.relationships.CATALOG_CONTROL['appId'], 'CONTRACT_DRIFT', 'enrolled runtime identity')",'pass # mutant','round4_catalog_identity_must_match'),
 ('erase-tick-bounds',m,"utc_ticks(delivery['durableAt']) and utc_ticks(delivery['acknowledgedAt']) and delivery['durableAt'] <= delivery['acknowledgedAt']","0 < delivery['durableAt'] <= delivery['acknowledgedAt']",'round4_erase_ack'),
]
mutants=[('audit-governance-time', '_bmad/scripts/epic16_domain.py', "require(int(audit['capturedAt'][:4]) <= 9998 and audit['capturedAt'].endswith(('Z','+00:00','-00:00')), 'actual governance audit timestamp uses UTC and audited business range')", 'pass # mutant', 'round4_audit_governance')]
results=[]
for name,path,old,new,selection in mutants:
 workspace=Path(tempfile.mkdtemp(prefix='epic16-review4-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 target=workspace/path;content=target.read_bytes();count=content.count(old.encode());assert count>=1,(name,count);target.write_bytes(content.replace(old.encode(),new.encode()))
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 started=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=300)
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]: (out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(name=name,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-started,3),replacements=count,originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(target.read_bytes()).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n')
 print(name,result.returncode,counts,flush=True)
 assert result.returncode==1 and counts['failures']>0 and counts['errors']==0,(name,result.stdout.decode(),result.stderr.decode())
