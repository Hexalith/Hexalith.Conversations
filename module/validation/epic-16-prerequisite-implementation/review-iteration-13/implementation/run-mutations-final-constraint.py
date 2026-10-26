from pathlib import Path
from concurrent.futures import ThreadPoolExecutor,as_completed
from xml.etree import ElementTree as ET
import os,sys,json,shutil,tempfile,subprocess,hashlib,time,ast,difflib
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/mutations-final-constraint';out.mkdir(parents=True,exist_ok=False)
files=set((*t.TOOL_SOURCES,*t.CONTRACT_PINS,*t.PREDECESSOR_PINS));files.update(['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/fixtures/epic16-observations-v1.json','commitlint.config.mjs']);files.update(json.loads((root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/exploratory-4/source.json').read_bytes())['files']);mutants=[]
construction='_bmad/scripts/epic16_native_helper/TestConstruction.cs';program='_bmad/scripts/epic16_native_helper/Program.cs';native='_bmad/scripts/epic16_native.py';capture='_bmad/scripts/epic16_capture.py';product='_bmad/scripts/epic16_products.py'
def add(name,path,old,new,selector):mutants.append(dict(name=name,path=path,old=old,new=new,selector=selector))
add('typed-default-kind',construction,'if (!(targetPrimitive == kind || targetPrimitive == "Object"))','if (false)','round13_defaults_validate and wrong-type')
add('nullable-enum-default',construction,'if (underlying is not null && EnumPrimitive(reader, underlying) is not null)','if (false)','round13_selected_typed_defaults and nullable-enum')
add('constant-encoding',construction,'if (bytes.Length != length || length < 0 || constant.TypeCode == ConstantTypeCode.Boolean && bytes[0] > 1)','if (false)','round13_defaults_validate and (truncated-constant or invalid-bool)')
add('special-default-complete',construction,'if (blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) throw new InvalidDataException("Invalid default attribute blob.");','_ = blob.ReadUInt16();','round13_defaults_validate and (decimal-named or date-named)')
add('provider-constructor',construction,'|| signature.GenericParameterCount != 0 || signature.ReturnType != "Void"','|| signature.GenericParameterCount != 0','round13_provider and constructor and (collection-string or assembly])')
add('provider-constructor-trailing',construction,'signatureBlob.RemainingBytes != 0 || reader.GetString(constructor.Name)','reader.GetString(constructor.Name)','round13_provider and signature-trailing and collection-type')
add('owner-schema-count','_bmad/schemas/epic-16-tooling-v1.schema.json','"minItems": 9,\n          "maxItems": 9','"minItems": 7,\n          "maxItems": 9','round13_new_owner_capture and missing')
add('decimal-native-scale',construction,'if (scale > 28)','if (false)','round13_defaults_validate and decimal-scale')
add('nullable-underlying-constraint',construction,'if (!NonNullableValueType(reader, argument))','if (false)','round13_nullable_constraint or (round13_runtime_caller and nullable-constraint)')
source=(root/construction).read_text();start=source.index('        int named = blob.ReadUInt16();');end=source.index('        return value;',start)
add('provider-complete-blob',construction,source[start:end],'','round13_provider and (named-count or trailing) and not signature-trailing')
add('async-encoded-kind',program,'&& encodedKind == (TypeName(reader, handle) == "System.Threading.Tasks.Task"\n                ? (byte)SignatureTypeKind.Class : (byte)SignatureTypeKind.ValueType)','&& true','round13_async_return_kind')
add('product-owner-surface',product," and set(expected['surfaces'])<=set(metadata['declaredTypes'])",'','round13_new_owner_requires_actual_behavior_surface')
add('product-five-scenario-consistency',product,"need(all(inventory == inventories[0] for inventory in inventories[1:]), 'PRODUCT_IMPLEMENTATION_DRIFT', 'five measured scenarios')",'pass','round10_generator_rejects_different_owned and (7 or 8)')
add('raw-native-digest',capture,"need(digest(reader.read(native['path'])) == native['sha256'], 'NATIVE_CAPTURE_DRIFT', native['path'])","reader.read(native['path'])",'round13_generator_rereads_native and changed')
add('raw-native-alias',capture,"no_alias(reader, native['path'], need)",'pass','round13_generator_rereads_native and hardlink')
add('helper-host-blocked',native,"raise HelperUnavailable(_process_failure('fixed helper host failed', result))","raise ValueError('invalid native evidence')",'round13_host_failures and 150')
attrs=(root/'.gitattributes').read_text()
for pattern in ('*.pdb','*.snk','*.cache','apphost','Epic16.NativeHelper','Evidence','Goldens'):
 line=next(x for x in attrs.splitlines() if x.endswith('/'+pattern+' -text -diff'))
 add('binary-diff-'+pattern.replace('*','wildcard').replace('.','-'),'.gitattributes',line,line.replace(' -text -diff',' -text diff'),'round13_opaque_native and '+('sample.pdb' if pattern=='*.pdb' else 'public.snk' if pattern=='*.snk' else 'Product.assets.cache' if pattern=='*.cache' else pattern))

def run(mutant):
 name,path=mutant['name'],mutant['path'];workspace=Path(tempfile.mkdtemp(prefix='epic16-r13-mutant-'+name+'-'));directory=out/name;directory.mkdir()
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
with ThreadPoolExecutor(max_workers=2) as pool:
 futures=[pool.submit(run,m) for m in mutants]
 for f in as_completed(futures):
  row=f.result();results.append(row);(out/'mutation-probes.json').write_text(json.dumps(results,indent=2)+'\n');print(row['name'],row['exitCode'],row['tests'],flush=True)
assert all(r['exitCode']==1 and r['tests']['failures']>0 and r['tests']['errors']==r['tests']['skipped']==0 for r in results),[(r['name'],r['tests']) for r in results if r['exitCode']!=1 or r['tests']['failures']==0 or r['tests']['errors'] or r['tests']['skipped']]
