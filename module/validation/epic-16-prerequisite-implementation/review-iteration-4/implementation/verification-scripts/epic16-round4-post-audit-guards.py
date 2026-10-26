from pathlib import Path
import subprocess,json,hashlib
root=Path('/home/administrator/projects/hexalith/conversations')
base=root/'module/validation/epic-16-prerequisite-implementation'
out=base/'review-iteration-4/implementation/post-audit';out.mkdir(parents=True,exist_ok=True)
binding=root/'_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json'
commands=json.loads((base/'review-iteration-1/held-commands.json').read_bytes())['commands']
rows=[]
for n,item in enumerate(commands,1):
    argv=item['argv'];targets=[root/argv[index+1] for index,value in enumerate(argv) if value in ('--output','--output-json','--output-markdown')]
    before={str(path):path.read_bytes() if path.exists() else None for path in targets}
    result=subprocess.run(argv,cwd=root,capture_output=True,timeout=120)
    document=json.loads(result.stdout)
    assert result.returncode==2 and document['result']=='BLOCKED' and document['blockers']==['ENTRY_QUALIFICATION_PENDING'] and document['assertionLedger'],(argv,result.stdout,result.stderr)
    assert all((path.read_bytes() if path.exists() else None)==before[str(path)] for path in targets)
    for suffix,data in [('stdout',result.stdout),('stderr',result.stderr)]: (out/f'held-{n:02}.{suffix}.txt').write_bytes(data)
    rows.append(dict(argv=argv,exitCode=result.returncode,result=document['result'],blockers=document['blockers'],stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest()))
(out/'held-commands.json').write_text(json.dumps(dict(bindingSha256=hashlib.sha256(binding.read_bytes()).hexdigest(),sources=json.loads(binding.read_bytes())['sources'],commands=rows),indent=2)+'\n')
print('18 held runtime commands: BLOCKED, no runtime outputs written',flush=True)
rows=[]
for number in (1,2):
    target=root/f'artifacts/v9/7.3/AC-7.3-0{number}.json';before=target.read_bytes() if target.exists() else None
    argv=['python3','_bmad/scripts/verify_story_completion_workflows.py','--repository','.','--contract','_bmad-output/planning-artifacts/v9/story-contracts/7.3.json','--scenario',f'AC-7.3-0{number}','--output',target.relative_to(root).as_posix()]
    try:
        result=subprocess.run(argv,cwd=root,capture_output=True,timeout=120)
        output=target.read_bytes() if target.exists() else b''
        for suffix,data in [('stdout',result.stdout),('stderr',result.stderr),('receipt',output)]: (out/f'completion-{number}.{suffix}.txt').write_bytes(data)
        rows.append(dict(argv=argv,exitCode=result.returncode,stdoutSha256=hashlib.sha256(result.stdout).hexdigest(),stderrSha256=hashlib.sha256(result.stderr).hexdigest(),receiptSha256=hashlib.sha256(output).hexdigest()))
        assert result.returncode==0,(argv,result.stdout,result.stderr)
    finally:
        if before is None:target.unlink(missing_ok=True)
        else:target.write_bytes(before)
(out/'completion-commands.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Both exact Story 7.3 completion commands: exit 0; operational files restored',flush=True)
