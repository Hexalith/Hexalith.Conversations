from pathlib import Path
import json,hashlib,subprocess,sys,time
root=Path.cwd();sys.dont_write_bytecode=True;sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_tooling as t
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation';prior=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/implementation'
load=lambda p:json.loads(Path(p).read_bytes());sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest();frozen=load(out/'final-source.json')['files']
def check():
 for p,h in frozen.items():assert sha(root/p)==h,p
check();binding=load(root/t.BINDING);commands=[]
for i,old in enumerate(load(prior/'held-commands.json')['commands']):
 check();argv=old['argv'];r=subprocess.run(argv,cwd=root,capture_output=True,timeout=60);check();doc=json.loads(r.stdout);assert r.returncode==2 and doc['result']=='BLOCKED' and doc['blockers']==['ENTRY_QUALIFICATION_PENDING'],(argv,r.stdout,r.stderr)
 for stream,raw in [('stdout',r.stdout),('stderr',r.stderr)]:(out/f'held-{i:02d}.{stream}.txt').write_bytes(raw)
 commands.append(dict(argv=argv,exitCode=r.returncode,result=doc['result'],blockers=doc['blockers'],stdoutSha256=hashlib.sha256(r.stdout).hexdigest(),stderrSha256=hashlib.sha256(r.stderr).hexdigest(),sourceUnchanged=True))
(out/'held-commands.json').write_text(json.dumps(dict(bindingSha256=sha(root/t.BINDING),sources=binding['sources'],commands=commands),indent=2)+'\n')
completion=[]
for i,old in enumerate(load(prior/'completion-commands.json')):
 check();argv=old['argv'];output=root/argv[argv.index('--output')+1];before=output.read_bytes() if output.exists() else None
 try:
  r=subprocess.run(argv,cwd=root,capture_output=True,timeout=60);receipt=output.read_bytes();assert r.returncode==0,(argv,r.stdout,r.stderr)
  for stream,raw in [('stdout',r.stdout),('stderr',r.stderr),('receipt',receipt)]:(out/f'completion-{i+1}.{stream}.txt').write_bytes(raw)
 finally:
  if before is None:output.unlink(missing_ok=True)
  else:output.write_bytes(before)
 check();assert (output.read_bytes() if output.exists() else None)==before
 completion.append(dict(argv=argv,exitCode=r.returncode,stdoutSha256=hashlib.sha256(r.stdout).hexdigest(),stderrSha256=hashlib.sha256(r.stderr).hexdigest(),receiptSha256=hashlib.sha256(receipt).hexdigest(),priorBytesRestored=True,sourceUnchanged=True))
(out/'completion-commands.json').write_text(json.dumps(completion,indent=2)+'\n')
old=load(prior/'runtime-absence.json');assert all(not (root/p).exists() for p in old['absentPaths']);assert binding['entry'] is None and t.TRUSTED_ENTRIES=={} and binding['authority']==old['authority'];accepted=load(root/'.config/dotnet-tools.json')['tools']['hexalith.builds.module.cli']['version']
assert accepted==old['acceptedCliVersion']=='4.35.2';old.update(entry=binding['entry'],trustedEntries=t.TRUSTED_ENTRIES,authority=binding['authority']);(out/'runtime-absence.json').write_text(json.dumps(old,indent=2)+'\n')
protected=load(out/'restoration.json')['protected'];
for p,h in protected.items():assert sha(root/p)==h,p
state=load(prior/'runtime-and-user-state.json');assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==state['rootHead'];assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=root)
for p,h in [('global.json',state['protectedUnchanged']['global.json']),(t.spec_path('runtime','16.1'),state['runtimeSpecSha256']),(t.SPRINT,state['sprintSha256'])]:assert sha(root/p)==h,p
warning_paths=[t.measurements.execution.WARNING_PATH,'docs/release-evidence/projection-read-store-population-proof-v2.md']
for p in warning_paths:
 assert (root/p).read_bytes()==subprocess.check_output(['git','show','HEAD:'+p],cwd=root),p
assert t.measurements.execution.WARNING_MARKER in (root/warning_paths[0]).read_text() and 'single-replica' in (root/warning_paths[1]).read_text()
state.update(warningSurfaceHashes={p:sha(root/p) for p in warning_paths},protectedUnchanged=protected,branch=subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip(),directSubmodules=subprocess.check_output(['git','submodule','status'],cwd=root,text=True));(out/'runtime-and-user-state.json').write_text(json.dumps(state,indent=2)+'\n')
matrix=load(prior/'preserved-scenario-matrix.json');actual=[];requirements=set()
for story in ('16.1','16.2','16.3'):
 contract=load(root/t.contract_path(story));preserved=contract['preservedContract'];predecessor=load(root/f'_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/{story}.json');assert preserved==predecessor['preservedContract']
 for scenario in preserved['scenarios']:
  row=next(r for r in matrix['scenarios'] if r['storyId']==story and r['scenarioId']==scenario['id']);assert row['requirementIds']==scenario['requirementIds'] and row['preservedObligationEqual'] and row['rollbackUnchanged'];actual.append(row);requirements.update(scenario['requirementIds'])
for p,h in matrix['retainedPredecessorPins'].items():assert sha(root/p)==h,p
assert t.PREDECESSOR_PINS==matrix['retainedPredecessorPins'];graph=load(root/'_bmad-output/planning-artifacts/epic-16-adoption-v2/execution-graph.json');assert len(actual)==18 and len(requirements)==32 and len(graph['nodes'])==38 and len(graph['edges'])==61
(out/'preserved-scenario-matrix.json').write_text(json.dumps(matrix,indent=2)+'\n');check()
r=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True);assert r.returncode==0,(r.stdout,r.stderr)
for stream,raw in [('stdout',r.stdout),('stderr',r.stderr)]:(out/f'diff-check.{stream}.txt').write_bytes(raw)
print(json.dumps(dict(held=len(commands),completion=len(completion),absent=len(old['absentPaths']),protected=len(protected),scenarios=len(actual),requirements=len(requirements),sourceFiles=len(frozen),whitespaceExitCode=r.returncode)))
