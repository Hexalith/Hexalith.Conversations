"""Owned product PE bindings and conservative upstream public declaration additivity."""
import re

import epic16_native as native
import epic16_capture as capture
from evidence_boundary_current import digest
from contained_reader import ContainedReader
import generate_story_record as records

PRODUCTS = (
    dict(assemblyName='Hexalith.Conversations.Server', owner='.', surfaces=['Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelWriter','Hexalith.Conversations.Server.TenantAccess.ConversationTenantAccessService']),
    dict(assemblyName='Hexalith.Conversations.Contracts', owner='.', surfaces=['Hexalith.Conversations.Contracts.Projections.ConversationDetailProjectionV1']),
    dict(assemblyName='Hexalith.Tenants.Client', owner='references/Hexalith.Tenants', surfaces=['Hexalith.Tenants.Client.Projections.ITenantProjectionStore','Hexalith.Tenants.Client.Handlers.TenantProjectionEventHandler']),
    dict(assemblyName='Hexalith.Tenants.Contracts', owner='references/Hexalith.Tenants', surfaces=['Hexalith.Tenants.Contracts.Events.TenantDisabled']),
    dict(assemblyName='Hexalith.EventStore.Client', owner='references/Hexalith.EventStore', surfaces=['Hexalith.EventStore.Client.Projections.IReadModelStore','Hexalith.EventStore.Client.Streams.IAuthoritativeEventStreamReader']),
    dict(assemblyName='Hexalith.EventStore.Contracts', owner='references/Hexalith.EventStore', surfaces=['Hexalith.EventStore.Contracts.Projections.ProjectionDispatchRequest']),
    dict(assemblyName='Hexalith.EventStore.DomainService', owner='references/Hexalith.EventStore', surfaces=['Hexalith.EventStore.DomainService.DomainProjectionDispatcher']),
)


def policy():
    return dict(captureVersion=2, requiredProducts=list(PRODUCTS),
        publicApiComparison='conservative-ecma335-public-declarations-v1',
        unsupported='native marshalling, field RVAs, PInvoke and exported/forwarded types fail; no general binary compatibility claim')


def revisions(facts):
    return {'.':facts['commit'], **{row['path']:row['commit'] for row in facts['gitlinks']}}


def loaded(reader, raw, candidate, scenario, document, facts, need):
    """Bind output from this exact native result to every retained owned assembly."""
    output=native.strict_json(records.parse_trx(raw)['results'][0]['output'].encode())
    observed=output.get('loadedProducts');retained=document['products']
    need(isinstance(observed,list) and len(observed)==len(PRODUCTS), 'PRODUCT_INVENTORY_INVALID', scenario)
    need([row['assemblyName'] for row in observed]==[row['assemblyName'] for row in PRODUCTS]
        and [row['assemblyName'] for row in retained]==[row['assemblyName'] for row in PRODUCTS], 'PRODUCT_INVENTORY_INVALID', scenario)
    owners=revisions(facts);result=[]
    for expected,observation,artifact in zip(PRODUCTS,observed,retained):
        name=expected['assemblyName'];owner=expected['owner']
        need(owner in owners, 'PRODUCT_OWNER_UNAVAILABLE', owner, blocked=True)
        need(set(observation)=={'assemblyName','owner','sourceRevision','mvid','sha256','codeBase'}, 'PRODUCT_INVENTORY_INVALID', name)
        need(observation['owner']==owner and observation['sourceRevision']==owners[owner], 'PRODUCT_SOURCE_DRIFT', name)
        need(artifact['owner']==owner and artifact['sourceRevision']==owners[owner], 'PRODUCT_SOURCE_DRIFT', name)
        relative=capture.original_relative(document['originalRepositoryRoot'],observation['codeBase'],document['pathStyle'])
        need(artifact['originalCodeBase']==observation['codeBase'] and artifact['originalRelativePath']==relative, 'PRODUCT_CAPTURE_DRIFT', name)
        need(artifact['path']==capture.binary_path(scenario[3:7],artifact['sha256']), 'PRODUCT_CAPTURE_DRIFT', name)
        content=reader.read(artifact['path']);capture.no_alias(reader,artifact['path'],need)
        need(digest(content)==artifact['sha256']==observation['sha256'], 'PRODUCT_CAPTURE_DRIFT', name)
        metadata=native.inspect_product(content,owners[owner])
        need(metadata['assemblyName']==name and metadata['mvid']==observation['mvid']==artifact['mvid'] and set(expected['surfaces'])<=set(metadata['declaredTypes']), 'PRODUCT_METADATA_DRIFT', name)
        result.append(dict(path=artifact['path'],sha256=artifact['sha256'],owner=owner,**metadata))
    need(len({row['path'] for row in result})==len(result), 'PRODUCT_INVENTORY_INVALID', scenario)
    return result


def additive(before, after, need, subject):
    """Retain every public type/member, visibility, generic/parameter/property contract.

    Attribute blobs are compared conservatively, so some semantically compatible
    changes can fail. Interface additions also fail because they may add implementor
    obligations. This is a supported metadata subset, not a general APICompat claim.
    """
    for key in ('assemblyName','culture','publicKey'):
        need(before[key]==after[key], 'UPSTREAM_API_DRIFT', subject)
    btypes=before['api']['types'];atypes=after['api']['types']
    need(bool(btypes) and set(btypes)<=set(atypes), 'UPSTREAM_API_DRIFT', subject)
    for name,b in btypes.items():
        a=atypes[name]
        need(b['header']==a['header'] and b['interfaces']==a['interfaces'] and set(b['members'])<=set(a['members']), 'UPSTREAM_API_DRIFT', name)
        need(not any(member.startswith('method:') and 'Abstract' in member.split(':',3)[2].split(', ') for member in set(a['members'])-set(b['members'])), 'UPSTREAM_API_DRIFT', 'new abstract implementor obligation: '+name)


def upstream(repository, reader, facts, baseline, data, need):
    rows=data['upstreamSurfaces'];expected=[p for p in PRODUCTS if p['owner']!='.']
    need([row['assemblyName'] for row in rows]==[p['assemblyName'] for p in expected], 'UPSTREAM_API_INVENTORY', 'upstream APIs')
    before_links=dict(records.v2_raw_gitlinks(repository,baseline));after_links=revisions(facts)
    result=[]
    for row,p in zip(rows,expected):
        owner=p['owner'];name=p['assemblyName']
        need(row['owner']==owner and owner in before_links and owner in after_links, 'UPSTREAM_API_OWNER', name)
        child=repository/owner
        with ContainedReader(child):
            records.validate_repository(child)
        need(records.resolve_commit(child,'HEAD','DEPENDENCY_UNAVAILABLE')==after_links[owner]
            and not records.worktree_path_status(child), 'DEPENDENCY_DIRTY', owner, blocked=True)
        metadata=[];bindings=[]
        for phase,revision in (('baseline',before_links[owner]),('candidate',after_links[owner])):
            records.resolve_commit(child,revision,'PUBLIC_BASELINE_UNAVAILABLE')
            artifact=row[phase]
            need(artifact['revision']==revision, 'UPSTREAM_API_SOURCE_DRIFT', name)
            need(re.fullmatch(r'artifacts/epic16-(?:tooling-fixture|runtime)/16\.1/binaries/[0-9a-f]{64}\.dll',artifact['path']) is not None and artifact['path'].endswith('/'+artifact['sha256']+'.dll'), 'UPSTREAM_API_PATH', name)
            content=reader.read(artifact['path']);capture.no_alias(reader,artifact['path'],need)
            need(digest(content)==artifact['sha256'], 'UPSTREAM_API_SOURCE_DRIFT', name)
            current=native.inspect_product(content,revision)
            need(current['assemblyName']==name and set(p['surfaces'])<=set(current['declaredTypes']), 'UPSTREAM_API_SOURCE_DRIFT', name)
            metadata.append(current);bindings.append(dict(artifact,metadata=current))
        additive(*metadata,need,name)
        result.append(dict(assemblyName=name,owner=owner,baseline=bindings[0],candidate=bindings[1]))
    return result
