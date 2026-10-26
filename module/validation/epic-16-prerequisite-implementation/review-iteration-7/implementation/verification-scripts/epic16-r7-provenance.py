from pathlib import Path
import sys,json,hashlib,subprocess,datetime
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));import epic16_native as native
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-7/implementation/helper-toolchain';out.mkdir(exist_ok=True)
original=native.subprocess.run;commands=[]
def run(argv,**kwargs):
 result=original(argv,**kwargs);i=len(commands)
 for name,raw in [('stdout',result.stdout),('stderr',result.stderr)]: (out/f'{i:02}.{name}.txt').write_bytes(raw or b'')
 commands.append(dict(argv=argv,cwd=str(kwargs.get('cwd',root)),exitCode=result.returncode,stdoutSha256=hashlib.sha256(result.stdout or b'').hexdigest(),stderrSha256=hashlib.sha256(result.stderr or b'').hexdigest()))
 return result
native.subprocess.run=run
facts=native.provenance();workspace,assembly=native._helper()
(out/'global.json.bytes').write_bytes((Path(workspace.name)/'global.json').read_bytes())
(out/'Epic16.NativeHelper.dll.bytes').write_bytes(assembly.read_bytes())
(out/'runtimeconfig.json').write_bytes(assembly.with_suffix('.runtimeconfig.json').read_bytes())
root_sdk=run(['dotnet','--version'],cwd=root,capture_output=True,timeout=30)
assert root_sdk.returncode==0 and root_sdk.stdout.decode().strip()==facts['sdk']
(out/'provenance.json').write_text(json.dumps(dict(scope='Trusted fixed helper local execution provenance, not measured product runtime qualification or owner approval.',capturedAt=datetime.datetime.now(datetime.timezone.utc).isoformat(),command=['uv','run','--frozen','--no-sync','python3','/tmp/epic16-r7-provenance.py'],commands=commands,repositorySdk=root_sdk.stdout.decode().strip(),helper=facts,sources={path:hashlib.sha256((root/path).read_bytes()).hexdigest() for path in [*native.HELPER_FILES,'_bmad/scripts/epic16_native.py','global.json']}),indent=2)+'\n')
print(json.dumps(facts,indent=2))
