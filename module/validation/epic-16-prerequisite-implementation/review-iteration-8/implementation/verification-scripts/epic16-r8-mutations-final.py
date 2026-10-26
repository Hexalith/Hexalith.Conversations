from pathlib import Path
import sys,json,shutil,tempfile,subprocess,hashlib,time,re
from xml.etree import ElementTree as ET
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as tooling
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-8/implementation/mutations-final';out.mkdir(parents=True,exist_ok=True)
files=set((*tooling.TOOL_SOURCES,*tooling.CONTRACT_PINS,*tooling.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json'])
s='_bmad/scripts/epic16_schedules.py';m='_bmad/scripts/epic16_measurements.py';l='_bmad/scripts/epic16_lifecycle.py';f='_bmad/scripts/epic16_faults.py'
mutants=[]
def replace(name,path,old,new,selection):mutants.append(dict(name=name,path=path,old=old,new=new,selection=selection))
def drop(name,path,fragment,selection):
 lines=[line for line in (root/path).read_text().splitlines() if fragment in line];assert len(lines)==1,(name,lines)
 line=lines[0];replace(name,path,line,line[:len(line)-len(line.lstrip())]+'pass # removed validation in disposable mutation',selection)
replace('completed-watermark',m,"rebuild['watermarkAfter'] == max(rebuild['watermarkBefore'], rebuild['C'])","rebuild['watermarkAfter'] >= max(rebuild['watermarkBefore'], rebuild['C'])",'round8_completed_watermark and 999')
replace('opaque-domain-map-boundary',f,"                tenant_state(item)\n                continue","                tenant_state(item)",'round8_domain_map and version and False')
drop('ack-delivery-native-nonblank',m,"domain.text(row['deliveryId'])",'round8_acknowledgement and delivery and (4 or 5)')
replace('ack-record-native-nonblank',m,"            domain.text(operation['deliveryId']); domain.text(operation['recordId'])",'            pass','round8_acknowledgement and record')
drop('restart-caller',m,'schedules.restart_resumption(data)','round8_restart_requires and summary')
drop('restart-acquisition-execution',s,"'executed lease acquisition observes complete old snapshot and fence'",'round8_restart_requires and acquire-unexecuted')
drop('restart-acquisition-atomicity',s,"'one durable replacement-lease record transaction'",'round8_restart_requires and acquire-split')
drop('restart-resumption-execution',s,"'restarted recovery actually resumes after acquisition'",'round8_restart_requires and resume-unexecuted')
drop('restart-resumption-scope',s,"'resumption uses acquired worker and tenant store identity'",'round8_restart_requires and resume-process')
drop('restart-resumption-state',s,"'resumption reads acquired durable snapshot without clearing pending work'",'round8_restart_requires and resume-clears')
drop('restart-without-redelivery',s,"'persisted gap resumes exact recovery interval without new delivery'",'round8_restart_requires and resume-sequence')
for field in ('lease','version','checkpoint','horizon'):
 replace('restart-isolated-'+field,s,"admissible=all(same(expected[k],after[k]) for k in RESTART_FENCES)","admissible=all(same(expected[k],after[k]) for k in RESTART_FENCES if k!='"+field+"')",'all_fifteen_measured_routes and AC-16.1-02')
drop('restart-rejection-preserves-snapshot',s,"'stale restart attempt preserves every newer persisted field and fingerprint'",'round8_restart_requires and stale-mutates')
drop('restart-current-positive',s,"'current fences admit durable composite renewal'",'round8_restart_requires and current-failed')
drop('queue-caller',m,'lifecycle.catch_up_queue(rebuild)','round8_catch_up and enqueue-unexecuted')
replace('queue-retained-history',l,"    for observed in authoritative.values():\n        remember(queue_reference(observed,row['afterGeneration']))",'    pass # retained history admission removed','round8_queue_accepts and reuse')
drop('queue-authoritative-event',l,"'queue authoritative location binds exact envelope identity'",'round8_catch_up and event')
drop('queue-executed-enqueue',l,"'enqueue executes after Rebuilding entry before completed catch-up'",'round8_catch_up and enqueue-unexecuted')
drop('queue-atomic-enqueue',l,"'one enqueue transaction commits complete captured index and component state'",'round8_catch_up and enqueue-split')
drop('queue-durable-before-ack',l,"'commit precedes ack by operation order even when clock ticks coincide'",'round8_catch_up and ack-order')
drop('queue-drain-transaction',l,"'executed durable drain shares captured rebuild work transaction'",'round8_catch_up and drain-transaction')
drop('lifecycle-query-inventory',l,"'query execution at every entry deletion quarantine and cutover boundary'",'round8_lifecycle_queries and missing and not missing-reads')
drop('query-execution',l,"'query actually executed and completed at lifecycle boundary'",'round8_lifecycle_queries and unexecuted')
replace('query-order',l,"domain.sequence(query['order']);require(lower<query['order']<upper, 'query executes between exact committed lifecycle boundaries')","domain.sequence(query['order'])",'round8_lifecycle_queries and wrong-order')
drop('query-committed-snapshot',l,"'query binds precise committed index and component snapshot'",'round8_lifecycle_queries and (wrong-snapshot or wrong-transaction)')
drop('query-derived-result',l,"'executed query returns only snapshot-derived public state reason and content'",'round8_lifecycle_queries and false-current')
drop('query-no-repair',l,"'query captures exact reads with no repair recovery or mutation'",'round8_lifecycle_queries and missing-reads')
replace('cross-key-mapping',l,"return ('Rebuilding','mixed_generation')","return ('Unavailable','metadata_contradictory')",'round8_generation_query_contexts and valid')
drop('named-internal-metadata-mapping',m,'if internal: return internal','round8_internal_control_record')
drop('cancellation-caller',m,'faults.cancellation(data)','round8_cancellation and summary-only')
drop('cancellation-request-inventory',f,"'executed start cancellation request observation cleanup and terminal completion'",'round8_cancellation and missing-request')
drop('cancellation-execution-identity',f,"'same executed operation and monotonic measurement clock'",'round8_cancellation and (no-observation or unrelated-completion)')
drop('cancellation-operation-order',f,"'cancellation operations ordered despite equal coarse clock ticks'",'round8_cancellation and (backwards-time or backwards-order)')
drop('cancellation-resource-restoration',f,"'every cancellation boundary retains actual owned and preexisting resource state'",'round8_cancellation and (leaked-port or removed-preexisting)')
drop('cancellation-exact-release',f,"'cleanup releases exactly this operation resources'",'round8_cancellation and unrelated-release')
drop('cancellation-terminal-completion',f,"'terminal completion occurs only after observed cancellation and cleanup'",'round8_cancellation and unfinished')
# Existing exact-integer schema mechanism must cover every new DTO as well.
replace('new-capture-integer-schema','_bmad/scripts/epic16_tooling.py',"lambda checker, value: type(value) is int","lambda checker, value: isinstance(value, (int, float)) and not isinstance(value, bool) and int(value)==value",'round8_new_capture_schema and 1.0')
mutants=mutants[next(i for i,m in enumerate(mutants) if m['name']=='named-internal-metadata-mapping'):]
mutants=[m for m in mutants if m['name']!='cancellation-request-inventory']
results=[]
for mutant in mutants:
 name,path,selection=mutant['name'],mutant['path'],mutant['selection'];workspace=Path(tempfile.mkdtemp(prefix='epic16-review8-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 target=workspace/path;content=target.read_bytes();assert mutant['old'].encode() in content,name;new=content.replace(mutant['old'].encode(),mutant['new'].encode());target.write_bytes(new)
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=300)
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(new).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(name,result.returncode,counts,flush=True)
 assert result.returncode==1 and counts['failures']>0 and counts['errors']==0,(name,result.stdout.decode(),result.stderr.decode())
