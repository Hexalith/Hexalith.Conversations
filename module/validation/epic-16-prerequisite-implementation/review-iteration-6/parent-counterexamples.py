import sys,json,pathlib,copy,hashlib
sys.path.insert(0,str(pathlib.Path.cwd()/'_bmad/scripts'))
import jsonschema
import epic16_measurements as m
import epic16_native as native
from evidence_boundary_current import digest
v=json.loads(pathlib.Path('_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_bytes());schema=json.loads(pathlib.Path('_bmad/schemas/epic-16-tooling-v1.schema.json').read_bytes()); results=[]
def check(name,s,d):
 row={'finding':name,'scenario':s}
 try:
  jsonschema.Draft202012Validator(schema['$defs'][s]).validate(d);row['schema']='accepted';row['checks']=m.evaluate(s,d);row['semantic']='accepted'
 except Exception as e:row['error']=type(e).__name__+': '+str(e)
 results.append(row)
s='AC-16.1-04';d=copy.deepcopy(v[s]);r=next(x for x in d['faults'] if x['id']=='three-future-deliveries'); before=json.loads(r['beforeBytes']);before['state']['horizon']=999
r['beforeBytes']=r['afterBytes']=json.dumps(before,separators=(',',':'));r['beforeSha256']=r['afterSha256']=digest(r['beforeBytes'].encode());check('R6B3 prior horizon 999 becomes 14',s,d)
s='AC-16.1-02';d=copy.deepcopy(v[s]);d['tenant']='tenant-b'
for key in ('beforeRestart','afterRestart'):
 d[key]['tenant']='tenant-b'
 for r in d[key]['pending']:
  p=json.loads(r['persistedPayload']);p['tenantId']='tenant-b';r['persistedPayload']=json.dumps(p,separators=(',',':'));r['payloadSha256']=digest(r['persistedPayload'].encode())
check('R6B5 healthy rows reused for another tenant',s,d)
s='AC-16.3-01';d=copy.deepcopy(v[s]);d['attempts']=1;d['elapsedMs']=1;d['diagnostics'][0]['attempts']=1;check('R6B7 readiness terminates after 1 attempt and 1 ms',s,d)
s='AC-16.3-04';d=copy.deepcopy(v[s]);d['beforeWatermark']=d['afterWatermark']=0;check('R6B9 zero watermarks with recovered checkpoint',s,d)
s='AC-16.2-02';d=copy.deepcopy(v[s]);d['authoritativeEvents']=d['authoritativeEvents'][:2];state,times=m.domain.conversation_replay(d['authoritativeEvents']);items=m.domain.collection_items(state);d['contributingEventTicks']=times;d['collectionKeys']=[x['key'] for x in items]
for r in d['rebuilds']:
 anchor=max(times);raw=native.canonicalize(json.dumps(dict(version=2,anchorUtcTicks=anchor,items=items)));r.update(canonicalJson=raw,roundTripJson=raw,stateHash=digest(raw.encode()),anchorTicks=anchor,lastAppliedTicks=anchor,lagTicks=r['generatedTicks']-anchor)
check('R6B6 legitimate smaller multicollection history',s,d)
for n in range(0x1c,0x20):
 c=chr(n);row={'finding':'R6B10/R6E1 native whitespace','codepoint':f'U+{n:04X}'}
 try:row['nativePartyId']=native.normalize_party_id(c)
 except Exception as e:row['nativeError']=str(e)
 try:row['opaqueOracle']=m.domain.identifier('conv:'+c,'conv')
 except Exception as e:row['opaqueError']=str(e)
 results.append(row)
out=pathlib.Path('module/validation/epic-16-prerequisite-implementation/review-iteration-6');out.mkdir(exist_ok=True);(out/'parent-counterexamples.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps(results,indent=2))
