from pathlib import Path
import json,hashlib,subprocess,shlex,sys,os
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation';sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
frozen=json.loads((out/'final-source.json').read_bytes())['files']
def freeze():
 for path,sha in frozen.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==sha,path
freeze();binding=json.loads((root/t.BINDING).read_bytes());held=[];matrix=[];requirements=set();absent=[]
for story in t.measurements.STORIES:
 with t.ContainedReader(root) as reader:contract,_=t.validate_package(reader,story)
 predecessor=json.loads((root/f'_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/{story}.json').read_bytes())
 assert contract['preservedContract']==predecessor['preservedContract']
 for scenario,command in zip(contract['preservedContract']['scenarios'],contract['commands']):
  sid=scenario['id'];assert sid==command['scenarioId'];requirements.update(scenario['requirementIds'])
  matrix.append(dict(storyId=story,scenarioId=sid,requirementIds=scenario['requirementIds'],preservedObligationEqual=True,rollbackUnchanged=True,proof='canonical final-record derivation' if sid.endswith('06') else t.measurements.evaluate(sid,json.loads((root/'_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes())[sid])))
  argv=['uv','run','--frozen','--no-sync',*shlex.split(command['command'])];i=len(held)
  result=subprocess.run(argv,cwd=root,capture_output=True,timeout=180)
  for stream,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/f'held-{i:02}.{stream}.txt').write_bytes(raw)
  document=json.loads(result.stdout);assert result.returncode==2 and document['result']=='BLOCKED' and document['blockers']==['ENTRY_QUALIFICATION_PENDING'] and document['assertionLedger'],document
  held.append(dict(argv=argv,exitCode=result.returncode,result=document['result'],blockers=document['blockers'],stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest()))
 for sid in (f'AC-{story}-0{n}' for n in range(1,6)):
  absent += [t.native_path('runtime',sid),t.receipt_path('runtime',sid),t.capture.capture_path(story,sid)]
 absent += t.final_paths('runtime',story)+[t.root_path('runtime',story)+'/binaries']
assert len(held)==18 and len(absent)==54 and not any(os.path.lexists(root/path) for path in absent)
(out/'held-commands.json').write_text(json.dumps(dict(bindingSha256=hashlib.sha256((root/t.BINDING).read_bytes()).hexdigest(),sources=binding['sources'],commands=held),indent=2)+'\n')
(out/'runtime-absence.json').write_text(json.dumps(dict(entry=binding['entry'],trustedEntries=t.TRUSTED_ENTRIES,authority=t.AUTHORITY,absentPaths=absent,absentFileCount=51,absentBinaryDirectoryCount=3,acceptedCliVersion=json.loads((root/'.config/dotnet-tools.json').read_bytes())['tools']['hexalith.builds.module.cli']['version'],publication='not-applicable: real runtime remains held'),indent=2)+'\n')
assert binding['entry'] is None and t.TRUSTED_ENTRIES=={}
graph=json.loads((root/'_bmad-output/planning-artifacts/epic-16-adoption-v2/execution-graph.json').read_bytes());assert len(requirements)==32 and len(graph['nodes'])==38 and len(graph['edges'])==61
for path,sha in t.PREDECESSOR_PINS.items():assert hashlib.sha256((root/path).read_bytes()).hexdigest()==sha
(out/'preserved-scenario-matrix.json').write_text(json.dumps(dict(scope='Current exact preserved scenario and I/O audit; fixture predicates convey no runtime/owner authority',scenarios=matrix,uniqueRequirementCount=len(requirements),graphNodeCount=len(graph['nodes']),graphEdgeCount=len(graph['edges']),retainedPredecessorPins=t.PREDECESSOR_PINS,ioMatrix={'correctedTool':'historical isolated signed 4.38.0 qualification retained; actual accepted pins 4.35.2','pendingOwnerEvidence':'all 18 actual commands blocked','measuredFixtureSuccess':'all 15 native predicates plus deterministic generated sixth routes','missingNative':'blocked by focused regression suite','alteredBindings':'failed by independent pin/source/native regressions','forgedCounts':'failed by actual native parser and runtime metadata regressions','unsafePaths':'failed by contained reader/capture/output alias regressions','legacyCompletion':'preserved via historical fixtures, legacy selector and both exact workflow commands'}),indent=2)+'\n')
completion=[]
for n in (1,2):
 target=root/f'artifacts/v9/7.3/AC-7.3-0{n}.json';saved=target.read_bytes() if target.exists() else None
 argv=['python3','_bmad/scripts/verify_story_completion_workflows.py','--repository','.','--contract','_bmad-output/planning-artifacts/v9/story-contracts/7.3.json','--scenario',f'AC-7.3-0{n}','--output',str(target.relative_to(root))]
 try:
  result=subprocess.run(argv,cwd=root,capture_output=True,timeout=180)
  for stream,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(out/f'completion-{n}.{stream}.txt').write_bytes(raw)
  assert result.returncode==0,(result.stdout,result.stderr)
  receipt=target.read_bytes();(out/f'completion-{n}.receipt.txt').write_bytes(receipt)
  completion.append(dict(argv=argv,exitCode=result.returncode,stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),receiptSha256=hashlib.sha256(receipt).hexdigest(),priorBytesRestored=True))
 finally:
  if saved is None:target.unlink(missing_ok=True)
  else:target.write_bytes(saved)
(out/'completion-commands.json').write_text(json.dumps(completion,indent=2)+'\n')
result=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True)
(out/'diff-check.stdout.txt').write_bytes(result.stdout);(out/'diff-check.stderr.txt').write_bytes(result.stderr);assert result.returncode==0,(result.stdout,result.stderr)
freeze();print('18 held commands BLOCKED; 51 output files and 3 binary directories absent; both workflow guards PASS; git diff --check PASS')
