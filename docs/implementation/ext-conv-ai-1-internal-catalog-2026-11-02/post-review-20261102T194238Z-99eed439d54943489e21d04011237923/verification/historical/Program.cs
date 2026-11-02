using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
var at = new DateTimeOffset(2026,11,2,12,0,0,TimeSpan.Zero);
var request = new AppendMessageCommand(new(SchemaVersion.Current,new("tenant-one"),new("party-one"),"correlation-one","causation-one","retry-one"),
 new("conversation-one"),new("message-one"),new("party-one"),"original-text",new("provider-one","model",SchemaVersion.Current),
 new(SchemaVersion.Current,ClientName:"client-one"),new("trace-one",true,false),at);
Console.WriteLine(HistoricalPostFingerprint.PostFingerprint(request,at));
