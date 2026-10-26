import hashlib,importlib.util,json,os,shutil,subprocess,sys,tempfile
from pathlib import Path
ROOT=Path.cwd();OUT=ROOT/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/parent-tooling-final';OUT.mkdir(parents=True,exist_ok=True)
os.environ['EPIC16_COMMITLINT_EVIDENCE']=str(OUT/'commitlint.json')
sys.path.insert(0,str(ROOT/'_bmad/scripts'))
spec=importlib.util.spec_from_file_location('epic16_probe_tests',ROOT/'_bmad/scripts/tests/test_epic16_tooling.py');tests=importlib.util.module_from_spec(spec);spec.loader.exec_module(tests);t=tests.tooling
work=Path(tempfile.mkdtemp(prefix='epic16-r14-parent-tooling-'));repo=tests.repository.__wrapped__(work)
scenario='AC-16.1-01';good=tests.evaluate(repo,scenario);assert good['result']=='PASS',good
data=tests.deepcopy(tests.OBSERVATIONS[scenario]);data['authorization'][1]['allowed']=True
tests.native(repo,scenario,data)
try:t.measurements.evaluate(scenario,data)
except ValueError as e:explanation=str(e)
else:raise AssertionError('expected invalid')
bad=tests.evaluate(repo,scenario);assert bad['blockers']==['MEASUREMENT_INVALID'],bad
assert explanation in json.dumps(bad),bad
(OUT/'semantic.json').write_text(json.dumps(dict(positive=good,negative=bad,omittedExplanation=explanation),indent=2)+'\n')
tests.native(repo,scenario)
for n in range(1,6):
 s=f'AC-16.1-0{n}';r=tests.evaluate(repo,s);assert r['result']=='PASS',r;tests.dump(repo,t.receipt_path(tests.SCOPE,s),r)
commands=[]
base=['uv','run','--frozen','--no-sync','python3',str(ROOT/'_bmad/scripts/generate_story_record.py'),'--repository',str(repo),'--contract',t.contract_path('16.1'),'--scope',tests.SCOPE,'--format','bundle']
for label,flags in [('omitted',[]),('explicit',['--output-json',t.final_paths(tests.SCOPE,'16.1')[0],'--output-markdown',t.final_paths(tests.SCOPE,'16.1')[1]]),('partial',['--output-json',t.final_paths(tests.SCOPE,'16.1')[0]])]:
 p=subprocess.run(base+flags,cwd=ROOT,capture_output=True,timeout=90)
 (OUT/(label+'.stdout')).write_bytes(p.stdout);(OUT/(label+'.stderr')).write_bytes(p.stderr)
 v=json.loads(p.stdout);assert v['result']==('FAIL' if label=='partial' else 'PASS'),v
 commands.append(dict(label=label,argv=base+flags,cwd=str(ROOT),exitCode=p.returncode,result=v['result'],blockers=v.get('blockers',[])))
(OUT/'commands.json').write_text(json.dumps(commands,indent=2)+'\n');shutil.copy2(__file__,OUT/'probe.py')
(OUT/'report.json').write_text(json.dumps(dict(workspace=str(repo),semanticExplanation=explanation,semanticReported=bad['assertionLedger'],cli=commands),indent=2)+'\n')
print(json.dumps(dict(workspace=str(repo),semanticExplanation=explanation,cli=[(c['label'],c['result']) for c in commands])))
