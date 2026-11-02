using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Conversations.Server.Tests.Routing;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.EventStore.DomainService;
var cases = (System.Collections.IEnumerable)typeof(ConversationsCatalogIntentTest).GetMethod("Cases",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,null)!;
var samples=cases.Cast<object>().Select(item=>item.GetType().GetField("Item1")!.GetValue(item)!).ToArray();
foreach(var (name,path) in new[]{("AddParticipant","metadata.actorPartyId"),("AppendAgentMessage","messageId"),("AppendAgentMessage","authorPartyId")})
{
 var payload=samples.Single(sample=>sample.GetType().Name==name); var node=JsonNode.Parse(JsonSerializer.SerializeToUtf8Bytes(payload,payload.GetType(),EventStorePayloadSerialization.Options))!;
 JsonNode parent=node["publicCommand"]!; var parts=path.Split('.'); foreach(string part in parts[..^1])parent=parent[part]!;parent[parts[^1]]=null;
 var adapter=ConversationsCatalogAdapters.Create().Single(adapter=>adapter.CommandType==name);
 try{adapter.CreateIntent(new(name,"tenant-one","conversation","conversation-one",JsonSerializer.SerializeToUtf8Bytes(node),null));Console.WriteLine(name+" "+path+": accepted unexpectedly");}
 catch(Exception exception){Console.WriteLine(name+" "+path+": "+exception.GetType().FullName);}
}
