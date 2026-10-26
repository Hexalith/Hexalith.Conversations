from pathlib import Path
import os,sys,json,shutil,tempfile,subprocess,hashlib,time
from xml.etree import ElementTree as ET
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10/implementation/mutations-R10P1';out.mkdir(parents=True,exist_ok=True)
files=set((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs'])
mutants=[]
def replace(name,path,old,new,selection):mutants.append(dict(name=name,path=path,old=old,new=new,selection=selection))
p='_bmad/scripts/epic16_products.py';c='_bmad/scripts/epic16_tooling.py';m='_bmad/scripts/epic16_measurements.py';g='_bmad/scripts/generate_story_record.py';cs='_bmad/scripts/epic16_native_helper/'
replace('upstream-loaded-correlation',p,'            correlate(bindings[1], loaded_products, name, owner, need)','            pass','round10_api_candidate and upstream')
replace('root-loaded-correlation',p,"        correlate(bindings[1], loaded_products, product['assemblyName'], '.', need)",'        pass','round10_api_candidate and root')
replace('five-product-consistency',c,"            if scope == 'runtime': products.consistent(rows, need)",'            pass','round10_generator_rejects')
replace('root-compiled-caller',c,"compiled = products.root_surface(repository, reader, facts, surface['baseline'], observations, story, need, binary['products'] if binary else None) if surface else None",'compiled = None','round10_external_build_inputs')
replace('root-declaration-equality',p,"before['api'] == after['api']",'True','round10_root_compiled_shape and (removed-type or addition)')
replace('complete-reference-identity',cs+'SignatureNames.cs','AssemblyIdentity(reader, (AssemblyReferenceHandle)type.ResolutionScope)','"[" + reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name) + "]"','round10_external_signature and not same-identity')
replace('constraint-attributes',cs+'ProductMetadata.cs','return Names.Name(reader, constraint.Type) + ":" + Attributes(reader, constraint.GetCustomAttributes());','return Names.Name(reader, constraint.Type);','round10_constraint_attributes and tighten')
replace('constructor-admission-caller',cs+'Program.cs','            TestConstruction.Validate(reader, handle);','            // validation removed','round10_runtime_admission_rejects')
replace('required-constructor-provider',cs+'TestConstruction.cs','            if (!supplied) throw new InvalidDataException("Required constructor parameter has no supported provider.");','            _ = supplied;','round10_construction and missing')
replace('fixture-parent-providers',cs+'TestConstruction.cs','            Parameters(reader, handle, parents, false);','            // validation removed','round10_construction and missing-fixture-dependency')
replace('sdk-informational-version',cs+'Program.cs',"!versions[0].Contains('+') || versions[0].Split('+')[^1].Split('.')[^1] != revision",'!versions[0].EndsWith("+" + revision, StringComparison.Ordinal)','round10_sdk_informational and existing')
replace('trx-definition-cardinality',c,"    need(len(methods) == len(executions) == 1, 'NATIVE_IDENTITY_DRIFT', scenario)",'    pass','round10_trx_definition')
source=(root/g).read_text();start=source.index('            except ModuleNotFoundError as error:',source.index('def main(arguments:'));end=source.index('                document = v2_failure_document',start)
replace('lazy-import-defect-boundary',g,source[start:end],'            except ImportError:\n','round10_generator_classifies and ImportError')
replace('lazy-import-exact-declarations',g,'                if error.name not in declared:','                if error.name is None:','round10_generator_classifies and misspelled')
replace('lazy-import-actual-absence',g,'                if importlib.util.find_spec(error.name) is not None:\n                    raise','                pass','round10_generator_classifies and available-declared')
replace('authorization-response-caller',m,'            authorization_responses(data, rows)','            pass','round10_authorization_response_is and (correlation or execution or tenant)')
replace('authorization-response-content',m,"        require(domain.same(response['result'], expected), 'actual public response is non-disclosing Hidden result')",'        pass','round10_equal_authorization')
replace('authorization-response-order',m,"        require(integer(request['order'], 1) and integer(response['order'], 1) and request['order'] < response['order'], 'request precedes response')",'        pass','round10_authorization_response_is and order')
replace('compiled-baseline-retention','_bmad/scripts/epic16_capture.py',"        if compiled: entries.extend(compiled[phase] for phase in ('baseline','candidate'))",'        pass','round10_final_inventory_rechecks')
mutants=[m for m in mutants if m['name'] in ('constructor-admission-caller','required-constructor-provider','fixture-parent-providers')]
replace('inherited-collection-types',cs+'TestConstruction.cs','if (++collectionTypes > 1)', 'if (++collectionTypes < 0)', 'round10_inherited_collection')
results=[]
for mutant in mutants:
 name,path,selection=mutant['name'],mutant['path'],mutant['selection'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r10-mutant-'+name+'-'))
 for file in files:
  target=workspace/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/file,target)
 (workspace/'node_modules').symlink_to(root/'node_modules',target_is_directory=True)
 target=workspace/path;content=target.read_bytes();old=mutant['old'].encode();new=mutant['new'].encode()
 if path.endswith('.cs'):old=old.replace(b'\n',b'\r\n');new=new.replace(b'\n',b'\r\n')
 assert content.count(old)==(2 if name=='inherited-collection-types' else 1),(name,content.count(old));changed=content.replace(old,new);target.write_bytes(changed)
 argv=[sys.executable,'-m','pytest','-q',str(workspace/'_bmad/scripts/tests/test_epic16_tooling.py'),'-k',selection,'--junitxml='+str(out/(name+'.xml'))]
 start=time.monotonic();result=subprocess.run(argv,cwd=workspace,capture_output=True,timeout=500,env=dict(os.environ,EPIC16_COMMITLINT_EVIDENCE=str(out/(name+'-commitlint.json'))))
 for suffix,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/(name+'.'+suffix+'.txt')).write_bytes(raw)
 xml=ET.parse(out/(name+'.xml')).getroot();suites=list(xml) if xml.tag=='testsuites' else [xml];counts={k:sum(int(x.get(k,0)) for x in suites) for k in ('tests','failures','errors','skipped')}
 row=dict(**mutant,argv=argv,workspace=str(workspace),exitCode=result.returncode,elapsedSeconds=round(time.monotonic()-start,3),originalSha256=hashlib.sha256(content).hexdigest(),mutatedSha256=hashlib.sha256(changed).hexdigest(),stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),tests=counts)
 results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(name,result.returncode,counts,flush=True)
 assert result.returncode==1 and counts['failures']>0 and counts['errors']==counts['skipped']==0,(name,result.stdout.decode(),result.stderr.decode())
