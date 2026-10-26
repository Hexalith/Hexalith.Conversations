from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-5/implementation/mutations';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json'])
r='_bmad/scripts/epic16_relationships.py';m='_bmad/scripts/epic16_measurements.py';d='_bmad/scripts/epic16_domain.py';t='_bmad/scripts/epic16_tooling.py';f='_bmad/scripts/epic16_faults.py';n='_bmad/scripts/epic16_native_helper/Program.cs'
mutants=[
 ('stored-object-binding',m,'    relationships.erase_targets(row)','    pass # mutant','round5_erase_targets'),
 ('lifecycle-generation',r,"        require(stored['lifecycleGeneration']==row['beforeGeneration']==target['generation'], 'captured retiring lifecycle generation')",'        pass # mutant','round5_erase_targets and record-generation'),
 ('creation-prefix',m,"        require(stream['start'] == 0, 'full authoritative replay includes creation prefix without a checkpoint snapshot')",'        pass # mutant','round5_rebuild_cannot'),
 ('native-pending-record',m,"                    relationships.tenant_record(record, data['tenant'])",'                    pass # mutant','round5_pending_restart'),
 ('raw-lease-version',f,"            if key in {'lease','version'}: sequence(item)",'            pass # mutant','round5_raw_lease_version and (lease or version)'),
 ('raw-utc-domain',f,"            if key in {'expiresAt','now'}: ticks(item)",'            pass # mutant','round5_raw_lease_version'),
 ('party-normalization',d,"return normalize_party_id(value[len('party:'):])","return value[len('party:'):]",'round5_materialized_party or round5_participant_duplicate or round5_redaction_attribution'),
 ('native-unicode-normalization','_bmad/scripts/epic16_native.py',"return _run(dict(operation='partyId', value=value))['value']","return value.strip().lower()",'round5_party_identity'),
 ('actor-normalization',d,"        meta['actorPartyId'] = 'party:' + identifier(meta['actorPartyId'], 'party')","        identifier(meta['actorPartyId'], 'party')",'round5_materialized_party or round5_redaction_attribution'),
 ('redaction-new-identity',r,"    require(identity not in before['processedEventIds'], 'redaction introduces a new authoritative event identity')",'    pass # mutant','round5_redaction_reused'),
 ('async-void-native',n,'return signature.RemainingBytes == 0\r\n                && !method.GetCustomAttributes().Any(handle =>\r\n                    AttributeType(reader, reader.GetCustomAttribute(handle)) == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");','return signature.RemainingBytes == 0;','round5_genuine_async_void'),
 ('native-run-failure',t,"    need(not root.findall('.//{*}ErrorInfo') and all(node.get('outcome') in ('Passed', 'Completed', 'Warning')\n         for node in root.findall('.//{*}RunInfo')), 'NATIVE_RUN_FAILED', scenario)",'    pass # mutant','round5_native_run_level'),
 ('checkout-shared-helper','.gitattributes','_bmad/scripts/authoring_outputs.py text eol=lf','# mutant omitted shared helper policy','round5_bound_inventory'),
 ('checkout-native-bytes','.gitattributes','artifacts/epic16-runtime/** -text diff','# mutant omitted runtime bytes policy','round5_bound_inventory'),
 ('publication-required',t,"                need(published, 'PUBLICATION_MISSING', story, blocked=True)",'                pass # mutant','round5_runtime_insertion_caller'),
]
results=[]
for name,path,old,new,selection in mutants:
 workspace=Path(tempfile.mkdtemp(prefix='epic16-review5-mutant-'+name+'-'))
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
