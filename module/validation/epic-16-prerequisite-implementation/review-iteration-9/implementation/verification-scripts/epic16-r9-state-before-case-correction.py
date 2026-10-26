from pathlib import Path
import json,hashlib,subprocess,sys
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def git(*args):return subprocess.run(['git',*args],cwd=root,check=True,capture_output=True).stdout.decode().strip()
frozen=json.loads((out/'final-source.json').read_bytes())['files']
for path,value in frozen.items():assert sha(root/path)==value,path
protected=json.loads((out/'preservation-restored.json').read_bytes())['protected']
for path,value in protected.items():assert sha(root/path)==value,path
assert git('rev-parse','HEAD')=='8748a6b6e80b3ecbae2bb0174b43f40e30e6a148'
assert git('diff','--cached','--name-only')==''
spec='_bmad-output/implementation-artifacts/spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md';sprint='_bmad-output/implementation-artifacts/sprint-status.yaml'
assert "status: 'draft'" in (root/spec).read_text()
assert '16-1-persist-tenant-access-projection-state-and-prove-convergence: backlog' in (root/sprint).read_text()
for path in ('src/Hexalith.Conversations.Server/Program.cs','docs/release-evidence/projection-read-store-population-proof-v2.md'):
 assert (root/path).read_bytes()==subprocess.run(['git','show','HEAD:'+path],cwd=root,check=True,capture_output=True).stdout
 assert b'SINGLE-REPLICA-ONLY' in (root/path).read_bytes()
state=dict(rootHead=git('rev-parse','HEAD'),rootIndexUnchanged=True,protectedUnchanged=protected,runtimeSpecSha256=sha(root/spec),sprintSha256=sha(root/sprint),acceptedPin=json.loads((root/'.config/dotnet-tools.json').read_bytes())['tools']['hexalith.builds.module.cli']['version'],activeAndHistoricalWarningsUnchanged=True,branch=git('branch','--show-current'),directSubmodules=git('submodule','status'))
assert state['acceptedPin']=='4.35.2'
(out/'runtime-and-user-state.json').write_text(json.dumps(state,indent=2)+'\n')
owned=set(json.loads((out/'preservation-restored.json').read_bytes())['restored'])|{'_bmad/scripts/epic16_execution.py','_bmad/scripts/epic16_products.py','_bmad/scripts/epic16_native_helper/ProductMetadata.cs','_bmad/scripts/epic16_native_helper/SignatureNames.cs'}
(out/'implementation-inventory.json').write_text(json.dumps(dict(scope='Task-owned implementation inventory; excludes protected user work and parent evidence/docs',files=[dict(path=p,sha256=sha(root/p)) for p in sorted(owned)]),indent=2)+'\n')
print('Frozen source, five protected inputs, root index/HEAD, warnings, draft/backlog and accepted pins preserved')
