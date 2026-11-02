import datetime,hashlib,json,pathlib,sys
root=pathlib.Path(__file__).resolve().parent
source=json.loads((root.parent/'post-review-20261102T194238Z-99eed439d54943489e21d04011237923/source-inventory.json').read_text())
rows=[]
for name,record in source.items():
    for relative in record['sourceFiles']:
        path=pathlib.Path(record['root'])/relative
        rows.append(dict(repository=name,path=relative,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
directories=dict(server='/home/administrator/projects/hexalith/conversations/tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0',domain='/home/administrator/projects/hexalith/conversations/tests/Hexalith.Conversations.Tests/bin/Debug/net10.0',contracts='/home/administrator/projects/hexalith/conversations/tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0',platform='/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0')
runtimes={}
for name,directory in directories.items():
    base=pathlib.Path(directory)
    runtimes[name]=dict(root=directory,files=[dict(path=str(path.relative_to(base)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()) for path in sorted(base.rglob('*')) if path.is_file()])
record=dict(observedAt=datetime.datetime.now(datetime.timezone.utc).isoformat(),scope='Final reruns only; pre-implementation binary measurement remains unavailable.',source=rows,runtimes=runtimes)
(root/(sys.argv[1]+'.json')).write_text(json.dumps(record,indent=2)+'\n')
