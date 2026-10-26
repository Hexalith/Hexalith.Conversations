from pathlib import Path
import subprocess,json,hashlib,re
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13/implementation';out=base/'diagnostic-collection';out.mkdir(exist_ok=False)
argv=['uv','run','--frozen','--no-sync','python3','-m','pytest','--collect-only','-q','_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py'];r=subprocess.run(argv,cwd=root,capture_output=True)
(out/'stdout').write_bytes(r.stdout);(out/'stderr').write_bytes(r.stderr);assert r.returncode==0
ids=[line for line in r.stdout.decode().splitlines() if line.startswith('_bmad/scripts/tests/') and '::' in line];progress=''.join(m.group(0) for line in (base/'combined-final-constraint/stdout').read_text().splitlines() if (m:=re.match(r'^[.FEsyxXP]+',line)));failures=[dict(index=i,nodeid=ids[i],marker=c) for i,c in enumerate(progress) if c in 'FE']
(out/'command.json').write_text(json.dumps(dict(argv=argv,cwd=str(root),exitCode=r.returncode,sourceManifest=str(base/'combined-final-constraint/source.json'),sourceManifestSha256=hashlib.sha256((base/'combined-final-constraint/source.json').read_bytes()).hexdigest(),collected=len(ids),observedProgress=len(progress),failures=failures),indent=2)+'\n');print(json.dumps(failures))
