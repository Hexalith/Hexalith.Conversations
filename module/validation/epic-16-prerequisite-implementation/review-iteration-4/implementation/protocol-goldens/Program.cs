using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
var ids = new[]{("tenant-a","same-resource"),("a:b","c"),("a","b:c"),("é/𐀀","+?=&"),("A","r"),("a","r")};
ProjectionDispatchRoute[] routes=[new("conversation","conversation-read-model")];
var output = new {synthetic=true, keys=ids.Select(x=> new {tenant=x.Item1,resource=x.Item2,key=ConversationProjectionReadModelKeys.ConversationKey(new TenantId(x.Item1),new ConversationId(x.Item2)),index=ConversationProjectionReadModelKeys.TenantIndexKey(new TenantId(x.Item1))}),catalogs=new[]{new {appId="conversations",serviceVersion="1.0.0",routes=routes.Select(r=>new {domain=r.Domain,projectionType=r.ProjectionType}),ordinary=routes.Select(r=>new {domain=r.Domain,projectionType=r.ProjectionType}),fingerprint=ProjectionRouteCatalogFingerprint.Compute("conversations","1.0.0",routes,routes)},new {appId="conversations",serviceVersion="1.0.0",routes=routes.Select(r=>new {domain=r.Domain,projectionType=r.ProjectionType}),ordinary=Array.Empty<ProjectionDispatchRoute>().Select(r=>new {domain=r.Domain,projectionType=r.ProjectionType}),fingerprint=ProjectionRouteCatalogFingerprint.Compute("conversations","1.0.0",routes,[])}}};
Console.WriteLine(JsonSerializer.Serialize(output,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
