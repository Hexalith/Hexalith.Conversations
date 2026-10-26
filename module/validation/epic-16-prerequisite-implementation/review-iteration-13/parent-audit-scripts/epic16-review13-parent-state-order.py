from pathlib import Path
import json,hashlib,subprocess
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';impl=base/'implementation';guards=impl/'guards-final-order';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
frozen=json.loads((impl/'focused-final-order/source.json').read_bytes())['files'];assert json.loads((impl/'combined-final-order/source.json').read_bytes())['files']==frozen
for p,h in frozen.items():assert sha(root/p)==h,p
held=json.loads((guards/'held-commands.json').read_bytes());assert len(held['commands'])==18
for i,r in enumerate(held['commands']):
 assert r['exitCode']==2 and r['result']=='BLOCKED' and r['blockers']==['ENTRY_QUALIFICATION_PENDING'] and r['sourceUnchanged']
 for stream in ('stdout','stderr'):assert sha(guards/f'held-{i:02}.{stream}.txt')==r[stream+'Sha256']
 stdout=json.loads((guards/f'held-{i:02}.stdout.txt').read_bytes());assert stdout['result']=='BLOCKED' and stdout['blockers']==r['blockers']
completion=json.loads((guards/'completion-commands.json').read_bytes());assert len(completion)==2
for i,r in enumerate(completion,1):
 assert r['exitCode']==0 and r['priorBytesRestored'] and r['sourceUnchanged']
 for stream in ('stdout','stderr','receipt'):assert sha(guards/f'completion-{i}.{stream}.txt')==r[stream+'Sha256']
runtime=json.loads((guards/'runtime-absence.json').read_bytes());assert len(runtime['absentPaths'])==54 and not any((root/p).exists() for p in runtime['absentPaths']);assert runtime['entry'] is None and runtime['trustedEntries']=={} and runtime['acceptedCliVersion']=='4.35.2';assert runtime['authority']==dict(executionAllowed=False,implementationHold='ACTIVE',ownerApprovalClaimed=False,pushAuthorized=False,releaseAuthorized=False)
state=json.loads((guards/'runtime-and-user-state.json').read_bytes());assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==state['rootHead']=='46ca7bf39b25318418b7145b7ce75bb306471cd0';assert not subprocess.check_output(['git','diff','--cached','--name-only'])
for p,h in state['protectedUnchanged'].items():assert sha(root/p)==h,p
for p,h in state['warningSurfaceHashes'].items():assert sha(root/p)==h,p
binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes());assert binding['entry'] is None and binding['authority']==runtime['authority'];assert len(binding['sources'])==71
for r in binding['sources']:assert sha(root/r['path'])==r['sha256']
matrix=json.loads((guards/'preserved-scenario-matrix.json').read_bytes());assert (len(matrix['scenarios']),matrix['uniqueRequirementCount'],matrix['graphNodeCount'],matrix['graphEdgeCount'])==(18,32,38,61)
for p,h in matrix['retainedPredecessorPins'].items():assert sha(root/p)==h,p
for story in ('16.1','16.2','16.3'):
 old=json.loads((root/f'_bmad-output/planning-artifacts/epic-16-adoption-v2/story-contracts/{story}.json').read_bytes());new=json.loads((root/f'_bmad-output/planning-artifacts/epic-16-tooling-v1/story-contracts/{story}.json').read_bytes());assert old['preservedContract']==new['preservedContract']
helper=json.loads((impl/'helper-toolchain-final-order/provenance.json').read_bytes());assert helper['sourceSnapshotSha256']==sha(impl/'focused-final-order/source.json');assert helper['policySha256']==sha(root/'global.json');assert helper['sdk']=='10.0.402' and helper['runtime']['runtimeVersion']=='10.0.13'
for r in helper['retained']:assert sha(root/r['path'])==r['sha256']
check=subprocess.run(['git','diff','--check'],capture_output=True);assert check.returncode==0,(check.stdout,check.stderr)
report=dict(frozenFiles=86,boundSources=71,heldCommands=18,heldResult='BLOCKED',heldExitCode=2,heldBlockers=['ENTRY_QUALIFICATION_PENDING'],completionGuards=2,operationalBytesRestored=True,absentRuntimePaths=54,protectedFiles=len(state['protectedUnchanged']),preservedScenarios=18,requirements=32,graphNodes=38,graphEdges=61,helperSdk='10.0.402',helperRuntime='10.0.13',sourceUnchanged=True,mainIndexEmpty=True,gitDiffCheckPassed=True,actualRuntimePublication='not-applicable',runtimeHold='ACTIVE')
(base/'parent-state-audit-order.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
