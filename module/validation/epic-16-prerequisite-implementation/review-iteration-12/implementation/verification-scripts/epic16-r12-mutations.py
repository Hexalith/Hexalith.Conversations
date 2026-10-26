from pathlib import Path
from concurrent.futures import ThreadPoolExecutor,as_completed
from xml.etree import ElementTree as ET
import os,sys,json,shutil,tempfile,subprocess,hashlib,time,ast,difflib
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation/mutations';out.mkdir(parents=True,exist_ok=False)
files=set((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs']);mutants=[]
s='_bmad/scripts/epic16_schedules.py';program='_bmad/scripts/epic16_native_helper/Program.cs';product='_bmad/scripts/epic16_native_helper/ProductMetadata.cs';construction='_bmad/scripts/epic16_native_helper/TestConstruction.cs';native='_bmad/scripts/epic16_native.py';lifecycle='_bmad/scripts/epic16_lifecycle.py'
def add(name,path,old,new,selector):mutants.append(dict(name=name,path=path,old=old,new=new,selector=selector))
def remove(name,detail,selector):
 source=(root/s).read_text();matches=[n for n in ast.walk(ast.parse(source)) if isinstance(n,ast.Call) and isinstance(n.func,ast.Name) and n.func.id=='require' and len(n.args)>1 and isinstance(n.args[1],ast.Constant) and n.args[1].value==detail];assert len(matches)==1,(name,len(matches));add(name,s,ast.get_source_segment(source,matches[0]),'None',selector)
remove('tenant-executed-completed','tenant operation actually executed and completed','round12 and (unexecuted or incomplete) and not native')
remove('tenant-operation-scope','tenant operation exact request tenant store record process scope','round12 and (attempt-scope or read-scope or principal or process) and not native')
remove('tenant-operation-order','ordered tenant operation and completion','round12 and (attempt-order or read-early or same-order)')
remove('reverse-original-cas','both reverse saves retain their original CAS expectation','round12_reverse_save and expectation')
remove('reverse-native-proposal','reverse-save complete proposed projection and checkpoint derive from authoritative replay','round12_reverse_save and proposal')
remove('reverse-durable-chain','reverse save observes preceding durable commit','round12_reverse_save and before')
remove('reverse-cas-result','newer CAS succeeds and stale older CAS fails against that durable state','round12_reverse_save and (newer-result or stale-result)')
remove('reverse-preserved-after','save after-state preserves exact committed full snapshot','round12_reverse_save and after')
remove('reverse-final-store-read','both replicas read the same actual final durable snapshot','round12_reverse_save and (read-state or read-transaction)')
remove('reverse-process-replicas','distinct reverse-save replicas and processes','round12_reverse_save_rejects_coherent_shared_process')
remove('reverse-read-identities','distinct executed store read and response identities','round12_reverse_read and (store-operation or response-operation)')
remove('access-source-state','authorization native projection checkpoint and fingerprint derive from retained source','round12_authorization_state and projection')
remove('access-freshness','valid persisted freshness and observation clock','round12_authorization_state and (freshness or future-clock)')
remove('access-stored-cause','stored authorization health derives from actual cause','round12_authorization_state and health')
remove('access-missing-metadata','absent or unavailable read cannot manufacture persisted state or metadata','round12_authorization_state and (unknown-snapshot or unavailable-snapshot)')
remove('access-response','executed native access response binds actual read result and safe metadata','round12_six_authorization and (response-value or read-correlation)')
# Exact response test name contains response-path; selecting response also runs incomplete and scope controls.
mutants[-1]['selector']='round12_six_authorization and (read-correlation or response)'
remove('access-principal','controlled native access tenant and principal are validated before the store read','round12_native_access_principal or round12_native_tenant')
remove('access-policy','pinned tenant freshness observation control','round12_six_authorization and threshold')
remove('failure-attempt-count','bounded count derives from actual failed attempt inventory','round12_failed_deliveries and attempt-count')
remove('failure-proposal','ordered attempt actually proposes source-derived exact-next composite write','round12_failed_deliveries and (number or expected or proposed)')
remove('failure-provider-outcome','actual provider failure cannot claim a successful write or unavailable snapshot','round12_failed_deliveries and (success or transaction or result)')
remove('failure-terminal','completed retryable processing records exact failed attempts and zero commits','round12_failed_deliveries and (outcome or attempt-reference)')
remove('failure-ack-observation','completed failed delivery has observed empty acknowledgement inventory','round12_failed_deliveries and ack-reference')
remove('failure-restoration','provider restored according to same fault experiment','round12_failed_deliveries and restoration and not unexecuted')
remove('failure-preserved-state','actual read after restoration proves complete durable state unchanged','round12_failed_deliveries and after and not unexecuted')
remove('failure-injected-correlation','failed delivery binds exact injected and restored fault evidence','round12_failed_deliveries and fault-path')
mutants[-1]['selector']='round12_failed_deliveries and fault and not missing'
add('enqueue-stage-transactions',lifecycle,"transactions={stage['cas']['transactionId'] for stage in row['atomicity']['stages']}","transactions=set()",'round12_enqueue_transaction')
add('diagnostic-exit',native,'exit={result.returncode}','exit=unknown','round12_helper_prerequisite_failure')
add('diagnostic-stdout',native,'stdout={bounded(result.stdout)!r}','stdout=omitted','round12_helper_prerequisite_failure and stdout')
add('diagnostic-stderr',native,'stderr={bounded(result.stderr)!r}','stderr=omitted','round12_helper_prerequisite_failure and stderr')
add('diagnostic-bound',native,"return (value or '')[-2000:]","return value or ''",'round12_helper_prerequisite_failure and oversized')
add('test-source-owner',program,'if (!ProductMetadata.IsFrameworkAttribute(reader, attribute, name, signature))','if (false)','round12_native_framework and (local-stamp or external-stamp or external-metadata)')
add('product-source-owner',product,'if (!IsFrameworkAttribute(reader, attribute, expectedName, signature))','if (false)','round12_native_framework and (external-stamp or external-metadata)')
add('source-constructor-signature',product,'&& reader.GetBlobBytes(constructor.Signature).AsSpan().SequenceEqual(signature);','&& true;','round12_source_stamp and constructor-signature')
add('test-source-blob',program,'if (blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) throw new BadImageFormatException();','_ = blob.ReadUInt16();','round12_source_stamp and test and (named-blob or trailing-blob)')
add('product-source-blob',product,'if (value.ReadUInt16() != 0 || value.RemainingBytes != 0) throw new BadImageFormatException();','_ = value.ReadUInt16();','round12_source_stamp and product and (named-blob or trailing-blob)')
add('async-return-owner',program,'return ProductMetadata.IsFrameworkType(reader, (TypeReferenceHandle)handle);','return true;','round12_native_framework and custom-task')
add('params-framework-owner',construction,'=> ProductMetadata.IsFrameworkAttribute(reader, attribute, "System.ParamArrayAttribute", [0x20, 0, 1])','=> true','round12_native_framework and custom-params')
add('params-complete-blob',construction,'&& reader.GetBlobBytes(attribute.Value).AsSpan().SequenceEqual(new byte[] { 1, 0, 0, 0 });','&& true;','round12_params_requires')

def run(mutant):
 name,path=mutant['name'],mutant['path'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r12-mutant-'+name+'-'));directory=out/name;directory.mkdir()
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 (workspace/'node_modules').symlink_to(root/'node_modules',target_is_directory=True)
 target=workspace/path;content=target.read_bytes();old=mutant['old'].encode();new=mutant['new'].encode()
 if path.endswith('.cs'):old=old.replace(b'\n',b'\r\n');new=new.replace(b'\n',b'\r\n')
 assert content.count(old)==1,(name,content.count(old));changed=content.replace(old,new);target.write_bytes(changed)
 (directory/'original.bytes').write_bytes(content);(directory/'mutated.bytes').write_bytes(changed);(directory/'change.diff').write_text(''.join(difflib.unified_diff(content.decode().splitlines(True),changed.decode().splitlines(True),fromfile=path,tofile=path)))
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',mutant['selector'],'--junitxml='+str(directory/'result.xml')]
 started=time.monotonic();r=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=700,env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',EPIC16_COMMITLINT_EVIDENCE=str(directory/'commitlint.json')))
 (directory/'stdout').write_bytes(r.stdout);(directory/'stderr').write_bytes(r.stderr);xml=ET.parse(directory/'result.xml').getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,cwd=str(workspace),exitCode=r.returncode,elapsedSeconds=time.monotonic()-started,originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(changed).hexdigest(),testSha256=hashlib.sha256((workspace/'_bmad/scripts/tests/test_epic16_tooling.py').read_bytes()).hexdigest(),tests=counts)
 (directory/'command.json').write_text(json.dumps(row,indent=2)+'\n');return row
results=[]
with ThreadPoolExecutor(max_workers=3) as pool:
 futures=[pool.submit(run,m) for m in mutants]
 for f in as_completed(futures):
  row=f.result();results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(row['name'],row['exitCode'],row['tests'],flush=True)
assert all(r['exitCode']==1 and r['tests']['failures']>0 and r['tests']['errors']==r['tests']['skipped']==0 for r in results),[(r['name'],r['tests']) for r in results if r['exitCode']!=1 or r['tests']['failures']==0 or r['tests']['errors'] or r['tests']['skipped']]
