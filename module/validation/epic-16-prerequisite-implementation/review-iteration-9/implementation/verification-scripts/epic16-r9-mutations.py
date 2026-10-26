from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time,concurrent.futures
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/mutations';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs'])
frozen={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in sorted(files)};(out/'source-epoch.json').write_text(json.dumps(frozen,indent=2)+'\n')
m='_bmad/scripts/epic16_measurements.py';e='_bmad/scripts/epic16_execution.py';p='_bmad/scripts/epic16_products.py';t='_bmad/scripts/epic16_tooling.py';c='_bmad/scripts/epic16_capture.py';l='_bmad/scripts/epic16_lifecycle.py';f='_bmad/scripts/epic16_faults.py';mutants=[]
def replace(name,path,old,new,selection):
 assert (root/path).read_bytes().count(old.encode())==1,(name,old)
 mutants.append(dict(name=name,path=path,old=old,new=new,selection=selection))
def drop(name,path,fragment,selection):
 lines=[line for line in (root/path).read_text().splitlines() if fragment in line];assert len(lines)==1,(name,lines)
 line=lines[0];replace(name,path,line,line[:len(line)-len(line.lstrip())]+'pass # removed guard in disposable mutant',selection)
drop('replay-caller',m,'execution.replay_rejections(data)','round9_rejected_replay and execution')
drop('replay-executed',e,"'negative writer operation executed and completed'",'round9_rejected_replay and execution')
drop('replay-scope',e,"'negative operation binds actual tenant resource and writer'",'round9_rejected_replay and (tenant or resource or writer)')
drop('replay-invalid-key',e,"'actual native identity cannot supply stable collection key'",'round9_rejected_replay and valid-input and 0')
drop('replay-invalid-order',e,"'actual ordinal collection order is invalid'",'round9_rejected_replay and valid-input and 1')
drop('replay-no-commit',e,"'invalid operation rejected before any persistence'",'round9_rejected_replay and committed')
drop('replay-no-clamping',e,"'contradictory generation is never clamped'",'round9_rejected_replay and clamped')
drop('wire-caller',m,'execution.reconciliation_wire(data)','round9_wire_rejects and wrong-policy')
replace('wire-order',e,"require(row['order']>previous, 'authorization precedes hosted dispatch commit and response')","pass",'round9_wire_requires and order and not 0')
drop('wire-execution-and-correlation',e,"'same executed request response pending runtime and route scope'",'round9_wire_requires and (execution or request-id or scope)')
drop('wire-method-route',e,"'actual request method and canonical route'",'round9_wire_requires and method')
drop('wire-authorization',e,"'wire authorization succeeds before dispatch'",'round9_wire_rejects and (denied or wrong-policy)')
drop('wire-hosted-dispatch',e,"'hosted coordinator dispatch without handler bypass'",'round9_wire_rejects and (handler-bypass or wrong-dispatch)')
drop('wire-transaction-state',e,"'wire durable outcome binds exact measured transaction and complete persisted state'",'round9_wire_rejects and (uncommitted-response or wrong-state-hash)')
drop('wire-outcomes',e,"'actual response outcomes prove Completed or AlreadyCompleted for each pending projection'",'round9_http_success and (failed or retryable or indeterminate or missing or foreign)')
drop('wire-request-scope',e,"'observed request body metadata binds actual aggregate scope'",'round9_http_success and scope')
drop('retry-emitted-advice',f,"'safe emitted diagnostic contents and bounded retry advice'",'round9_readiness_diagnostic and (unbounded or used or elapsed or remaining or reason or action)')
drop('query-provider-domain',m,"'known query provider state'",'round9_query_domain_semantics and providerState')
drop('query-lifecycle-domain',m,"'known query lifecycle'",'round9_query_domain_semantics and lifecycle')
drop('query-component-caller',l,'    query_component_identity(snapshot)','round9_cross_key_admission and foreign-dispatch')
drop('query-component-tenant',l,"'every query component belongs to control tenant'",'round9_cross_key_admission and foreign-dispatch')
drop('query-component-dispatch',l,"'actual dispatch references match control index'",'round9_cross_key_admission and unindexed-dispatch')
drop('capture-publication-reread',c,"need(digest(reader.read(path)) == sha",'round9_final_inventory and altered')
drop('capture-publication-alias',c,'            no_alias(reader, path, need)','round9_final_inventory and hardlink')
for story in ('1','2','3'):
 replace('wrapper-import-'+story,'_bmad/scripts/verify_story_16_'+story+'.py','except ModuleNotFoundError as error:','except FileNotFoundError as error:',f'round9_all_wrappers and 16.{story}')
replace('warning-candidate-caller',t,"warning = verify_warning(repository, reader, facts, scope, observations, contract) if scenario=='AC-16.1-05' else None",'warning = None','round9_warning')
replace('products-caller',t,"result['products'] = products.loaded(reader, raw, candidate, scenario, document, facts, need)","result['products'] = []",'round9_loaded_products and reported-owner')
drop('products-observed-owner-revision',p,"need(observation['owner']==owner",'round9_loaded_products and (reported-owner or reported-source)')
drop('products-captured-owner-revision',p,"need(artifact['owner']==owner",'round9_loaded_products and (capture-owner or capture-source)')
drop('products-exact-bytes',p,"need(digest(content)==artifact['sha256']==observation['sha256']",'round9_loaded_products and managed-bytes')
drop('products-managed-identity',p,"'PRODUCT_METADATA_DRIFT'",'round9_loaded_products and mvid')
replace('products-inventory',p,"need([row['assemblyName'] for row in observed]==[row['assemblyName'] for row in PRODUCTS]\n        and [row['assemblyName'] for row in retained]==[row['assemblyName'] for row in PRODUCTS], 'PRODUCT_INVENTORY_INVALID', scenario)","pass # required ordered identities removed",'round9_loaded_products and relabelled')
replace('upstream-caller',t,"upstream = products.upstream(repository, reader, facts, surface['baseline'], observations, need) if scenario=='AC-16.1-05' else None",'upstream = None','round9_upstream_additivity and (removed-member or signature or type)')
drop('upstream-raw-revision',p,"need(artifact['revision']==revision",'round9_upstream_additivity and wrong-revision')
drop('upstream-member-contract',p,"need(b['header']==a['header']",'round9_native_api_comparison and (return or parameter or nullable or property or generic or visibility)')
drop('upstream-abstract-obligation',p,"'new abstract implementor obligation: '",'round9_native_api_comparison and (abstract-interface or abstract-class)')
replace('optional-native-revision','_bmad/scripts/epic16_native_helper/ProductMetadata.cs',' || revisions.Any(value => value != revision)','', 'round9_product_metadata_rejects')
results=[]
def run(mutant):
 name,path,selection=mutant['name'],mutant['path'],mutant['selection'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r9-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 (workspace/'node_modules').symlink_to(root/'node_modules',target_is_directory=True)
 target=workspace/path;original=target.read_bytes();changed=original.replace(mutant['old'].encode(),mutant['new'].encode());target.write_bytes(changed)
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=600)
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),originalSha256=hashlib.sha256(original).hexdigest(),mutatedSha256=hashlib.sha256(changed).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 row['demonstrated']=result.returncode==1 and counts['failures']>0 and counts['errors']==counts['skipped']==0
 return row
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
 for future in concurrent.futures.as_completed([pool.submit(run,m) for m in mutants]):
  row=future.result();results.append(row);(out/'mutation-probes.json').write_text(json.dumps(sorted(results,key=lambda r:r['name']),indent=2)+'\n');print(row['name'],row['exitCode'],row['tests'],flush=True)
for path,sha in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==sha,path
assert all(row['demonstrated'] for row in results),[(r['name'],r['tests']) for r in results if not r['demonstrated']]
print('Demonstrated',len(results),'guard removals with',sum(r['tests']['failures'] for r in results),'failures and zero setup errors',flush=True)
