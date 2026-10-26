from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-6/implementation/mutations';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json'])
c='_bmad/scripts/epic16_capture.py';s='_bmad/scripts/epic16_schedules.py';m='_bmad/scripts/epic16_measurements.py';t='_bmad/scripts/epic16_tooling.py';f='_bmad/scripts/epic16_faults.py';d='_bmad/scripts/epic16_domain.py'
mutants=[
 ('retained-publication',t,"            publication_paths |= {row['path'] for row in result['retainedNativeArtifacts']}",'            pass # mutant','round5_runtime_insertion_caller'),
 ('native-capture-binding',c,"    need(document['native'] == dict(path=native_path,sha256=digest(raw)) and document['originalCodeBase'] == code_base, 'NATIVE_CAPTURE_DRIFT', path)",'    pass # mutant','round6_capture_rejects_changed and (native-path or native-hash or original-codebase)'),
 ('original-capture-path',c,"        relative = original_relative(document['originalRepositoryRoot'],code_base,document['pathStyle'])","        relative = document['originalRelativePath']",'round6_capture_rejects_changed and (traversal-root or relative-mapping or foreign-original)'),
 ('retained-location',c,"    need(binary['path'] == binary_path(story,binary['sha256']), 'NATIVE_CAPTURE_PATH_INVALID', binary['path'])",'    pass # mutant','round6_capture_rejects_changed and retained-outside'),
 ('retained-byte-hash',c,"    need(digest(content) == binary['sha256'], 'NATIVE_CAPTURE_DRIFT', binary['path'])",'    pass # mutant','round6_capture_rejects_changed and binary-byte'),
 ('capture-alias',c,"    for target in (path,native_path,binary['path']): no_alias(reader,target,need)",'    pass # mutant','round6_capture_rejects_changed and (manifest-alias or binary-alias)'),
 ('unique-assembly-hash',c,"            need(path not in artifacts or artifacts[path] == sha, 'NATIVE_CAPTURE_CONFLICT', path)",'            pass # mutant','genuine_native_capture_publication'),
 ('exact-capture-inventory',c,"    need(directory_names(reader,root) == expected, 'NATIVE_CAPTURE_INVENTORY', root)",'    pass # mutant','genuine_native_capture_publication'),
 ('exact-binary-inventory',c,"    need(directory_names(reader,root+'/binaries') == {path.rsplit('/',1)[1] for path in artifacts if path.endswith('.dll')}, 'NATIVE_CAPTURE_INVENTORY', root+'/binaries')",'    pass # mutant','genuine_native_capture_publication'),
 ('future-measured-schedules',m,'            schedules.future_deliveries(data)','            pass # mutant','round6_future_repeat'),
 ('future-conflict-durable-cas',s,"            require(after['etag'] != before['etag'] and cas['succeeded'] is True and all(cas[key] == text(cas['commitId']) for key in commit_fields), 'future conflict durably CAS-marks composite corrupt state')",'            pass # mutant','round6_future_repeat and conflict and split-commit'),
 ('healthy-tenant-source-consumer',m,'        schedules.healthy_restart_identity(row, tenant)','        pass # mutant','round6_healthy_restart or round6_relabelling'),
 ('healthy-domain-state',s,"    require(same(before['domain'], state), 'healthy persisted full domain state derives from retained events')",'    pass # mutant','round6_healthy_restart and domain-tenant'),
 ('unrelated-preflight-executed',f,"    require(text(negative['operationId']) != text(data['collisionOperationId']) and negative['executed'] is True and negative['completed'] is True, 'separate executed unrelated-only preflight')",'    pass # mutant','round6_unrelated_port and (not-executed or not-completed or same-operation)'),
 ('unrelated-preflight-occupancy',f,"    require(same(negative['endpointInventory'],data['endpointInventory']) and same(negative['occupancy'],expected_before), 'unrelated-only preflight uses same inventory with every owned binding free')",'    pass # mutant','round6_unrelated_port and (owned-occupied or unrelated-free or inventory)'),
 ('readiness-terminal',f,"return attempts == CONTROL_POLICY['attemptLimit'] or elapsed == CONTROL_POLICY['timeoutMs']",'return True # mutant','round6_readiness_failure'),
 ('prior-recovery-horizon',f,"max(before['horizon'], *values)",'max(values)','round6_future_fault_horizon'),
 ('concurrent-reconciliation',m,'            schedules.reconciliation(data)','            pass # mutant','round6_stale_worker or round6_current_recovery'),
 ('persisted-newer-progress',s,"        require(same(attempt['storedBefore'],newer) and same(attempt['storedAfter'],newer), 'stale attempt preserves complete newer stored state')",'        pass # mutant','round6_stale_worker and (checkpoint or generation or watermark or content)'),
 ('authoritative-global-position',s,"        require(observed['position'] > previous and observed['sequence'] == event['sequence'] and observed['eventSha256'] == digest(canonical(event)), 'global position bound to exact authoritative envelope')",'        pass # mutant','round6_current_recovery and foreign-event'),
 ('current-clearance-atomicity',s,"    require(current['succeeded'] is True and current['cleared'] == [scope['pendingId']] and all(current[key] == text(current['commitId']) for key in ('projectionCommitId','checkpointCommitId','pendingCommitId','watermarkCommitId')), 'one durable current-worker composite clearance')",'    pass # mutant','round6_current_recovery and current-split'),
 ('opaque-native-whitespace',d,"return isinstance(value, str) and any(char not in NATIVE_WHITESPACE for char in value)","return isinstance(value, str) and bool(value.strip())",'round6_opaque_id'),
 ('selected-native-whitespace',m,"and domain.nonblank(r['selectedId']), 'selected authoritative identity'","and r['selectedId'].strip(), 'selected authoritative identity'",'round6_opaque_id'),
 ('story163-ownership',t,"requirements = ENTRY_REQUIREMENTS + (('appHostOwnership',) if story == '16.3' else ())",'requirements = ENTRY_REQUIREMENTS','round6_story163_entry'),
 ('atomic-publication-required',t,"                need(published, 'PUBLICATION_MISSING', story, blocked=True)",'                pass # mutant','round5_runtime_insertion_caller'),
]
results=[]
for name,path,old,new,selection in mutants:
 workspace=Path(tempfile.mkdtemp(prefix='epic16-review6-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 target=workspace/path;content=target.read_bytes();count=content.count(old.encode());assert count>=1,(name,count);target.write_bytes(content.replace(old.encode(),new.encode()))
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=300)
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]: (out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(name=name,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),replacements=count,originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(target.read_bytes()).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(name,result.returncode,counts,flush=True)
 assert result.returncode==1 and counts['failures']>0 and counts['errors']==0,(name,result.stdout.decode(),result.stderr.decode())
