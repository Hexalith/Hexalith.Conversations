from pathlib import Path
from copy import deepcopy as cp
import json,hashlib,re
p=Path('_bmad/schemas/epic-16-tooling-v1.schema.json');schema=json.loads(p.read_text());o=json.loads(Path('_bmad/scripts/tests/fixtures/epic16-observations-v1.json').read_text());defs=schema['$defs']
I=dict(type='integer',minimum=0,maximum=9223372036854775807);T=dict(type='string',minLength=1)
def obj(properties):return dict(type='object',required=list(properties),additionalProperties=False,properties=properties)
def add(target,name,value):target['properties'][name]=value;target['required'].append(name) if name not in target['required'] else None
def find(value,predicate):
 if isinstance(value,dict):
  if predicate(value):return value
  for child in value.values():
   found=find(child,predicate)
   if found:return found
 elif isinstance(value,list):
  for child in value:
   found=find(child,predicate)
   if found:return found
native=cp(find(defs,lambda s:s.get('required')==['tenantId','conversationId','lifecycle','label','businessReference','projectId','folderId','attributes','participants','messages','fileReferences','processedEventIds']))
tenant=cp(find(defs,lambda s:s.get('required')==['tenantId','name','description','status','members','configuration']))
ref=obj({k:cp(I if k in ('sequence','position','generation') else T) for k in o['AC-16.2-04']['lifecycle'][1]['catchUp']['deliveries'][0]['reference']})
# Existing lifecycle schema fields remain; add explicit internal metadata and the
# closed immutable queue reference shape wherever complete snapshots are retained.
def patch(value):
 if isinstance(value,dict):
  props=value.get('properties',{})
  if {'schemaVersion','tenantWatermark','quarantineReferences'}<=set(props):add(value,'metadataGenerations',obj({k:cp(I) for k in ('lifecycle','manifest','watermark')}))
  if {'kind','tenant','resource','key','generation','value'}==set(props):props['value']['anyOf'].append(dict(type='array',items=cp(ref)))
  for child in list(value.values()):patch(child)
 elif isinstance(value,list):
  for child in value:patch(child)
patch(defs['AC-16.2-04'])
snapshot=cp(defs['AC-16.2-04']['properties']['lifecycle']['items']['anyOf'][0]['properties']['atomicity']['properties']['stages']['items']['properties']['after'])
common=dict(captureSnapshot=snapshot,conversationState=native,tenantState=tenant,queueReference=ref)
def infer(value):
 if isinstance(value,dict):
  keys=set(value)
  if keys=={'tenant','index','components'}:return {'$ref':'#/$defs/captureSnapshot'}
  if keys==set(native['properties']):return {'$ref':'#/$defs/conversationState'}
  if keys==set(tenant['properties']):return {'$ref':'#/$defs/tenantState'}
  if keys==set(ref['properties']):return {'$ref':'#/$defs/queueReference'}
  props={k:infer(v) for k,v in value.items()}
  for k in ('captureVersion',):
   if k in props:props[k]=dict(type='integer',const=value[k])
  return obj(props)
 if isinstance(value,list):
  if not value:return dict(type='array',maxItems=0)
  choices=[]
  for item in value:
   derived=infer(item)
   if derived not in choices:choices.append(derived)
  return dict(type='array',items=choices[0] if len(choices)==1 else dict(anyOf=choices))
 if type(value) is bool:return dict(type='boolean')
 if type(value) is int:return cp(I)
 if value is None:return dict(type='null')
 return cp(T)
for scenario in ('AC-16.1-02','AC-16.2-04','AC-16.2-05','AC-16.3-05'):
 defs[scenario]['$defs']=cp(common)
add(defs['AC-16.1-02'],'restartSchedule',infer(o['AC-16.1-02']['restartSchedule']))
# Failed and successful observations remain typed, not fixed result enums.
restart=defs['AC-16.1-02']['properties']['restartSchedule']
for row in [restart['properties']['rejectedAttempts']['items']]:
 row['properties']['transactionId']={'anyOf':[cp(T),dict(type='null')]}
for schema_row,data in zip(defs['AC-16.2-04']['properties']['lifecycle']['items']['anyOf'],o['AC-16.2-04']['lifecycle']):
 add(schema_row,'queries',infer(data['queries']))
 if data['id']=='rebuild':add(schema_row,'catchUp',infer(data['catchUp']))
add(defs['AC-16.2-05'],'generationQueries',infer(o['AC-16.2-05']['generationQueries']))
add(defs['AC-16.3-05'],'cancellation',infer(o['AC-16.3-05']['cancellation']))
for query in defs['AC-16.2-03']['properties']['queries']['items']['anyOf']:
 add(query,'controlMetadata',obj({k:cp(I) for k in ('index','lifecycle','manifest','watermark','dispatch')}))
 query['properties']['componentGenerations']['description']='Derived generation values inside the same captured lifecycle control record, not generations of separate detail/dispatch keys. Named controlMetadata fields are authoritative.'
# Make nullable native outcome observations capable of retaining failures for evaluation.
def relax(value,key=None):
 if isinstance(value,dict):
  if value=={'type':'null'} and key in ('transactionId','indexTransactionId','outcome','key','endpointId','port'):
   value.update(anyOf=[{'type':'null'},cp(I if key=='port' else T)]);value.pop('type')
  if value=={'type':'null'} and key=='succeeded':value.update(anyOf=[{'type':'null'},{'type':'boolean'}]);value.pop('type')
  for k,child in value.items():relax(child,k)
 elif isinstance(value,list):
  for child in value:relax(child,key)
for scenario in ('AC-16.1-02','AC-16.2-04','AC-16.2-05','AC-16.3-05'):relax(defs[scenario])
p.write_text(json.dumps(schema,ensure_ascii=False,indent=2)+'\n')
trust=Path('_bmad/scripts/epic16_trust.py');trust.write_text(re.sub(r"SCHEMA_PIN = '[a-f0-9]+'",f"SCHEMA_PIN = '{hashlib.sha256(p.read_bytes()).hexdigest()}'",trust.read_text()))
print('Schema updated',p.stat().st_size)
