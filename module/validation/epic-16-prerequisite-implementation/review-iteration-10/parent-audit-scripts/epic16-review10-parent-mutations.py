from pathlib import Path
import hashlib,json,xml.etree.ElementTree as ET
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-10';runs=[]
for part in ('mutations','mutations-R10P1','mutations-R10P1-shared'):
 directory=base/'implementation'/part
 runs.extend((directory,run) for run in json.loads((directory/'mutation-probes.json').read_bytes()))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
final_cases={x.attrib['name']:x for x in ET.parse(base/'implementation/combined.xml').iter('testcase')}
rows=[];latest={}
for directory,run in runs:
 name=run['name'];source=root/run['path'];current=sha(source)==run['originalSha256']
 if not current:
  source=base/'implementation/before-R10P1/helper-toolchain'/Path(run['path']).name
  assert sha(source)==run['originalSha256'],(name,'unknown source epoch')
 for kind in ('stdout','stderr'):assert sha(directory/(name+'.'+kind+'.txt'))==run[kind+'Sha256'],(name,kind)
 cases=list(ET.parse(directory/(name+'.xml')).iter('testcase'));failures=[x for x in cases if x.find('failure') is not None]
 counts=dict(tests=len(cases),failures=len(failures),errors=sum(x.find('error') is not None for x in cases),skipped=sum(x.find('skipped') is not None for x in cases))
 assert counts==run['tests'] and counts['errors']==counts['skipped']==0,(name,counts,run['tests'])
 demonstrated=run['exitCode']==1 and bool(failures)
 if not demonstrated:assert directory.name=='mutations-R10P1' and name=='inherited-collection-types' and run['exitCode']==0 and len(cases)==9 and not failures
 for case in cases:
  final=final_cases[case.attrib['name']];assert not any(x.tag in ('failure','error','skipped') for x in final),(name,'final control failed')
 mutant=Path(run['workspace'])/run['path'];assert mutant.is_file(),str(mutant);assert sha(mutant)==run['mutatedSha256'],(name,'mutant bytes drifted')
 original=source.read_bytes();a,b=run['old'].encode(),run['new'].encode();assert a in original
 assert mutant.read_bytes() in (original.replace(a,b,1),original.replace(a,b)),(name,'recorded patch not exact')
 row=dict(epoch=directory.name,name=name,sourceHashMatchesFinal=current,archivedSourceVerified=not current,counts=counts,demonstrated=demonstrated,allCasesPassInFinalControl=True,exactMutantBytesAndPatchVerified=True)
 rows.append(row);latest[name]=row
assert len(runs)==24 and len(latest)==20
assert all(r['demonstrated'] and r['sourceHashMatchesFinal'] for r in latest.values()),latest
report=dict(runs=len(runs),demonstratedRuns=sum(r['demonstrated'] for r in rows),demonstratedGuards=len(latest),assertionFailures=sum(r['counts']['failures'] for r in rows),insensitiveIntermediateRuns=sum(not r['demonstrated'] for r in rows),setupErrors=0,skipped=0,latestGuardSourceHashesMatchCurrentFinal=True,stdoutStderrAndJUnitVerified=True,allMutantTestsPassInFinalControl=True,rows=rows)
(base/'parent-mutation-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))
