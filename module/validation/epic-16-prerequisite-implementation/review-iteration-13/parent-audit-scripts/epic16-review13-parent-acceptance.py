from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';impl=base/'implementation';prior=base.parent/'review-iteration-12';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();frozen=json.loads((impl/'combined-final-order/source.json').read_bytes())['files'];binding=json.loads((root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json').read_bytes())
assert len(frozen)==86 and len(binding['sources'])==71 and binding['entry'] is None
for p,h in frozen.items():assert sha(root/p)==h,p
for r in binding['sources']:assert sha(root/r['path'])==r['sha256']
def cases(path):
 nodes=list(ET.parse(path).iter('testcase'));result={(n.get('classname'),n.get('name')):n for n in nodes};assert len(result)==len(nodes);return result
runs={};counts={}
for name in ('combined-final-order','focused-final-order','legacy-final-order'):
 d=impl/name;c=json.loads((d/'command.json').read_bytes());assert c['exitCode']==0 and c['sourceUnchanged'] is True;assert json.loads((d/'source.json').read_bytes())['files']==frozen and sha(d/'source.json')==c['sourceSha256']
 for stream in ('stdout','stderr'):assert sha(d/stream)==c[stream+'Sha256']
 for p,h in frozen.items():assert sha(d/'source'/(p+'.bytes' if p=='.gitignore' else p))==h,(name,p)
 rows=cases(d/'result.xml');assert rows and not any(any(c.tag in ('failure','error','skipped') for c in n) for n in rows.values());runs[name]=rows;counts[name]=len(rows)
assert counts=={'combined-final-order':3032,'focused-final-order':131,'legacy-final-order':72},counts
old=cases(prior/'implementation/combined/result.xml');assert len(old)==2879 and set(old)<=set(runs['combined-final-order'])
old_legacy=cases(prior/'implementation/legacy/result.xml');assert len(old_legacy)==72 and set(old_legacy)==set(runs['legacy-final-order'])
packaging=cases(prior/'implementation/packaging/result.xml');assert len(packaging)==19 and set(packaging)<=set(runs['combined-final-order'])
assert set(runs['focused-final-order'])<=set(runs['combined-final-order'])
previous_matrix=json.loads((prior/'parent-matrix-audit.json').read_bytes())['matrixRows'];assert len(previous_matrix)==8;passing_names={n[1] for k in ('combined-final-order','legacy-final-order') for n in runs[k]}
for name,row in previous_matrix.items():
 for test in row.get('passedExecutedTests',[]):assert test in passing_names,(name,test)
added=set(runs['combined-final-order'])-set(old);new_round=[name for cls,name in added if name.startswith('test_round13_')];assert len(added)==153 and len(new_round)==127
current_frozen=json.loads((impl/'focused-final-order/source.json').read_bytes())['files'];parent=json.loads((base/'parent-final-controls-order/report.json').read_bytes());assert parent['sourceUnchanged'] and parent['sourceHashes']==frozen and parent['count']==30
matrix=dict(verificationEpoch='review-iteration-13 completed full final-order suite; exact frozen source across combined/focused/legacy',matrixRows=previous_matrix,newExecutedPassedCases=sorted(new_round),newRound13CaseCount=len(new_round),allNewCases=[list(x) for x in sorted(added)],combinedTests=counts['combined-final-order'],focusedTests=counts['focused-final-order'],legacyTests=counts['legacy-final-order'],allSourceHashesVerified=True)
(base/'parent-matrix-audit.json').write_text(json.dumps(matrix,indent=2)+'\n')
report=dict(frozenFiles=86,boundSources=71,testHashesVerified=sum('/tests/test_' in p for p in frozen),counts=counts,previousCombinedCasesPreserved=len(old),newCombinedCases=len(added),newRound13Cases=len(new_round),allPriorLegacyCasesPreserved=True,all19PriorPackagingCasesPreserved=True,allEightMatrixRowsCoveredByExecutedEvidence=True,allFocusedCasesPassInCombined=True,independentParentControls=30,allFinalSourceArchivesVerified=True,allFinalRunsSourceUnchanged=True,runtimeAuthority='none; separate held-state audit')
(base/'parent-acceptance-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
