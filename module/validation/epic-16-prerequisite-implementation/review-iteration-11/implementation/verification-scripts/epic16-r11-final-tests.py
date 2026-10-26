from pathlib import Path
import json,subprocess,sys,os,tempfile,hashlib
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11/implementation';frozen=json.loads((out/'final-source.json').read_bytes())['files'];temp=Path(tempfile.mkdtemp(prefix='epic16-r11-final-'));(out/'final-test-workspaces.json').write_text(json.dumps(dict(root=str(temp)),indent=2)+'\n');commands=[]
for name,selector,files in [('combined','ALL',['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py']),('legacy','story_111 or story_112 or standalone_generator',['_bmad/scripts/tests/test_generate_story_record.py','_bmad/scripts/tests/test_epic16_tooling.py'])]:
 for p,h in frozen.items():assert hashlib.sha256((root/p).read_bytes()).hexdigest()==h,p
 command=['python3','/tmp/epic16-r11-run.py',name,selector,*files]
 result=subprocess.run(command,cwd=root,env=dict(os.environ,EPIC16_BASETEMP=str(temp/name)))
 record=json.loads((out/name/'command.json').read_bytes());commands.append(record);(out/'test-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
 for p,h in frozen.items():assert hashlib.sha256((root/p).read_bytes()).hexdigest()==h,p
 if result.returncode:sys.exit(result.returncode)
