from pathlib import Path
from concurrent.futures import ThreadPoolExecutor,as_completed
import os,sys,json,shutil,tempfile,subprocess,hashlib,time
from xml.etree import ElementTree as ET
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/implementation/mutations-supplement';out.mkdir(parents=True,exist_ok=False)
files=set((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs'])
mutants=[]
def add(name,path,old,new,selection):mutants.append(dict(name=name,path=path,old=old,new=new,selection=selection))
p='_bmad/scripts/epic16_native_helper/ProductMetadata.cs';program='_bmad/scripts/epic16_native_helper/Program.cs';products='_bmad/scripts/epic16_products.py';s='_bmad/scripts/epic16_schedules.py';m='_bmad/scripts/epic16_measurements.py';l='_bmad/scripts/epic16_lifecycle.py'
add('interface-annotations',p,'return Names.Name(reader, implementation.Interface) + ":" + Attributes(reader, implementation.GetCustomAttributes());','return Names.Name(reader, implementation.Interface);','round11_native_contract_changes and interface-nullability')
add('attribute-constructor-signatures',p,'return AttributeName(reader, attribute) + ":" + name + ":" + SignatureNames.Format(signature);','return AttributeName(reader, attribute);','(round11_native_contract_changes and attribute-overload) or (round11_framework_ownership and external-constructor)')
add('method-implementation-flags',p,'{method.Attributes}:{method.ImplAttributes}:{SignatureNames.Format','{method.Attributes}:{SignatureNames.Format','round11_native_contract_changes and preserve-sig')
add('assembly-contract-attributes',p,'assemblyAttributes = ContractAttributes(reader, assembly.GetCustomAttributes())','assemblyAttributes = ""','round11_native_contract_changes and assembly-cls')
add('module-contract-attributes',p,'moduleAttributes = ContractAttributes(reader, reader.GetModuleDefinition().GetCustomAttributes())','moduleAttributes = ""','round11_native_contract_changes and module-cls')
add('non-source-assembly-metadata',p,'excluded = value.ReadSerializedString() == "SourceRevisionId";','excluded = true;','round11_native_contract_changes and assembly-metadata')
add('owning-assembly-flags',p,'assemblyFlags = (int)assembly.Flags','assemblyFlags = 0','round11_native_contract_changes and owning-flags')
add('framework-build-attribute-owner',p,'&& IsFrameworkType(reader, (TypeReferenceHandle)reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent);',';','round11_framework_ownership and foreign-build-attribute')
add('fact-derived-discovery',program,'&& method.GetCustomAttributes().Count(attributeHandle => IsFactDerived(reader, reader.GetCustomAttribute(attributeHandle))) == 1)','&& true)','round11_fact_discovery and conflict')
add('framework-method-attribute-owner',program,'if (!ProductMetadata.IsFrameworkType(reader, (TypeReferenceHandle)type))','if (!name.StartsWith("System.", StringComparison.Ordinal))','round11_framework_ownership and hidden-fact')
for n in (1,2,3):
 path=f'_bmad/scripts/verify_story_16_{n}.py'
 add('launcher-exact-dependencies-'+str(n),path,'if error.name not in declared:','if error.name not in declared and not error.name.startswith("epic16_"):','round11_launchers and typo and 16_'+str(n))
 add('launcher-actual-absence-'+str(n),path,'    if importlib.util.find_spec(error.name) is not None:\n        raise','    pass','round11_launchers and present-declared and 16_'+str(n))
add('ack-source-derived-outcome',s,"    require(same(after, expected), 'persisted tenant outcome is the exact source-derived state')",'    pass','round11_acknowledgement_derives and both-swapped')
add('ack-commit-execution',l,"    require(commit['operation'] == 'commit' and ack['operation'] == 'acknowledge' and commit['succeeded'] is True, 'ack requires successful durable commit')",'    pass','round11_acknowledgement_derives and commit-failed')
add('ack-correlated-transaction',l,"        require(operation['transactionId'] == text(commit['transactionId']), 'ack observes the successful durable transaction')",'        pass','round11_acknowledgement_derives and transaction')
add('ack-persisted-observation',l,"    require(same(commit['persisted'], persisted) and same(ack['observedRecord'], persisted), 'operation observations bind actual persisted identity and state')",'    pass','round11_acknowledgement_derives and (persisted or observed)')
add('ack-actual-operations',l,"        require(operation['executed'] is True and operation['deliveryId'] == delivery and operation['recordId'] == record, 'executed operations correlate delivery and durable record')",'        pass','round11_acknowledgement_derives and unexecuted')
add('ack-delivered-duplicate',s,"        require(same(delivered, accepted), 'acknowledged duplicate matches actual committed fingerprint')",'        pass','round11_acknowledgement_derives and delivered and 1')
add('ack-duplicate-no-reapplication',s,"    require(proof['writeCount'] == int(mutation) and proof['expectedEtag'] == before['etag'], 'outcome proves current CAS or previously committed duplicate without reapplication')",'    pass','round11_acknowledgement_derives and write-count')
add('recovery-execution-caller',s,'        recovery_execution(attempt, data)','        pass','round11_recovery_operations and unexecuted and not 6')
add('duplicate-execution-caller',s,'    recovery_execution(retry, data, duplicate=True)','    pass','round11_recovery_operations and unexecuted and 6')
add('recovery-operation-identity',s,"        require(operation['operationId'] == proof['operationId'] and operation['executed'] is True and operation['completed'] is True, 'same recovery operation actually executed and completed')",'        pass','round11_recovery_operations and (identity or incomplete)')
add('recovery-operation-order',s,"        require(operation['order'] > previous, 'recovery request attempt completion occur in order')",'        pass','round11_recovery_operations and order')
add('recovery-exact-request',s,"    require(request['expectedSha256'] == digest(canonical(expected)) and request['proposedSha256'] == digest(canonical(proposed)), 'execution request binds exact expected and proposed state')",'    pass','round11_recovery_operations and (expected or proposed)')
add('recovery-observed-before',s,"    require(attempt['storedBeforeSha256'] == digest(canonical(before)) and attempt['kind'] == ('observe-completion' if duplicate else 'compare-exchange'), 'attempt observes exact scoped stored state')",'    pass','round11_recovery_operations and before')
add('recovery-actual-outcome',s,"    require(attempt['succeeded'] is row['succeeded'] and attempt['transactionId'] == row['commitId'], 'executed attempt records actual CAS or no-write outcome')",'    pass','round11_recovery_operations and (succeeded or transaction)')
add('recovery-completed-state',s,"    require(completion['storedAfterSha256'] == digest(canonical(after)) and completion['cleared'] == row['cleared'] and completion['outcome'] == ('AlreadyCompleted' if duplicate else 'FenceRejected'), 'completion binds exact preserved state and actual outcome')",'    pass','round11_recovery_operations and (after or outcome)')
add('recovery-wire-clearance',s,"    require(proof['afterClearanceTransactionId'] == (current['commitId'] if duplicate else None)\n            and proof['wireRequestId'] == (data['wireObservation']['requestId'] if duplicate else None), 'duplicate execution follows the same current wire clearance')",'    pass','round11_recovery_operations and clearance')
add('resource-dispatch-multiplicity',l,"dispatch=[reference for state in states for reference in resource_dispatch[state['conversationId']]]","dispatch=[row['retiringDispatchId']] if erase and not worked else []",'round11_multi_resource and positive')

mutants=[dict(m,selection='round11_recovery_operations and unexecuted') for m in mutants if m['name']=='recovery-execution-caller']
add('recovery-branch-scope',s,"    require(proof['branchId'] == ('current-clearance' if duplicate else row['id']) and same(proof['scope'], row['scope']), 'execution belongs to exact recovery branch and scope')",'    pass','round11_recovery_operations and scope')
mutants.append(dict(name='native-domain-map-openness',path=t.SCHEMA,old='closed fixture-key regression',new='closed fixture-key regression',selection='round11_durable_native_maps and extra',schemaMaps=True))

def run(mutant):
 name,path,selection=mutant['name'],mutant['path'],mutant['selection'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r11-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 (workspace/'node_modules').symlink_to(root/'node_modules',target_is_directory=True)
 target=workspace/path;content=target.read_bytes();old=mutant['old'].encode();new=mutant['new'].encode()
 if path.endswith('.cs'):old=old.replace(b'\n',b'\r\n');new=new.replace(b'\n',b'\r\n')
 if mutant.get('schemaMaps'):
  doc=json.loads(content);a=doc['$defs']['AC-16.1-04']['properties']['acknowledgements']['items']['properties'];p=a['durableEvidence']['anyOf'][0]['properties']
  for state in (p['sourceState'],p['before']['properties']['projection'],p['after']['properties']['projection'],a['durableCommit']['anyOf'][0]['properties']['persisted']['properties']['projection'],a['acknowledgement']['anyOf'][0]['properties']['observedRecord']['properties']['projection']):
   for key in ('members','configuration'):
    state['properties'][key]['additionalProperties']=False;state['properties'][key]['properties']={('actor-a' if key=='members' else 'conversations.enabled'):dict(type='string')}
  changed=(json.dumps(doc,indent=2,ensure_ascii=False)+'\n').encode()
  trust=workspace/'_bmad/scripts/epic16_trust.py';import re
  trust.write_text(re.sub(r"SCHEMA_PIN = '[0-9a-f]+'", "SCHEMA_PIN = '"+hashlib.sha256(changed).hexdigest()+"'", trust.read_text()))
 else:
  assert content.count(old)==1,(name,content.count(old));changed=content.replace(old,new)
 target.write_bytes(changed)
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=700,env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',EPIC16_COMMITLINT_EVIDENCE=str(out/(name+'-commitlint.json'))))
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(changed).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 return row
results=[]
with ThreadPoolExecutor(max_workers=3) as pool:
 futures=[pool.submit(run,m) for m in mutants]
 for f in as_completed(futures):
  row=f.result();results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(row['name'],row['exitCode'],row['tests'],flush=True)
assert all(r['exitCode']==1 and r['tests']['failures']>0 and r['tests']['errors']==r['tests']['skipped']==0 for r in results),[(r['name'],r['tests']) for r in results if r['exitCode']!=1 or r['tests']['failures']==0 or r['tests']['errors'] or r['tests']['skipped']]
