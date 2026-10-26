from pathlib import Path
import sys,json,hashlib,shutil
root=Path.cwd();sys.path.insert(0,str(root/'_bmad/scripts'));sys.dont_write_bytecode=True
import epic16_native as native
out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation/helper-toolchain-final';out.mkdir(parents=True,exist_ok=True)
proof=native.provenance();workspace,assembly=native._helper();directory=Path(workspace.name)
files=[]
for path in sorted(directory.rglob('*')):
 if path.is_file() and ('obj' not in path.relative_to(directory).parts):
  target=out/path.relative_to(directory);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target);files.append(dict(path=str(target.relative_to(root)),sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
proof.update(sourceSnapshotSha256=hashlib.sha256((out.parent/'combined-final-constraint/source.json').read_bytes()).hexdigest(),retained=files,scope='fixed trusted helper only; evidence assemblies are not executed by the consumer')
(out/'provenance.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(proof,indent=2))
