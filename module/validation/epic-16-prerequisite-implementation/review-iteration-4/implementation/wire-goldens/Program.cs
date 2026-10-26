using System.Reflection;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var baseline = new DateTimeOffset(2026,1,2,3,4,5,TimeSpan.Zero);
ConversationEventMetadata Meta(ConversationEventType kind, int n) => new(SchemaVersion.Current, "event-"+n, kind, new TenantId("tenant-a"), new ConversationId("conversation-a"), "correlation-a", baseline.AddSeconds(n), new PartyId("actor-a"));
object[] events = [
 new ConversationCreated(Meta(ConversationEventType.ConversationCreated,1),new BusinessReference("case","a"),new ProjectId("project-a"),new FolderId("folder-a"),"Seed conversation"),
 new ParticipantAdded(Meta(ConversationEventType.ParticipantAdded,2),new PartyId("participant-a"),ParticipantType.Human,ParticipantRole.Member),
 new MessageAppended(Meta(ConversationEventType.MessageAppended,3),new MessageId("message-a"),new PartyId("actor-a"),"Controlled message"),
 new FileReferenceAttached(Meta(ConversationEventType.FileReferenceAttached,4),new FileId("file-a"),new FolderId("folder-a"),new MessageId("message-a")),
 new ConversationClosed(Meta(ConversationEventType.ConversationClosed,6),null),
 new ConversationClosed(Meta(ConversationEventType.ConversationClosed,7),"completed"),
 new ConversationArchived(Meta(ConversationEventType.ConversationArchived,8),null),
 new ConversationArchived(Meta(ConversationEventType.ConversationArchived,9),"archived"),
 new MessageContentRedacted(Meta(ConversationEventType.MessageContentRedacted,10),new GovernanceTarget(GovernedTargetKind.Message,new MessageId("message-a")),RedactionCategory.DisplayMask,"policy-a","privacy-request",new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("evidence-a"),"policy-a",baseline.AddSeconds(10))),
 new ConversationMetadataUpdated(Meta(ConversationEventType.ConversationMetadataUpdated,5),"Corrected conversation",new BusinessReference("case","b"),new Dictionary<string,string>{{"scope","controlled"}})
];
DateTimeOffset[] times = [baseline, baseline.AddTicks(1), baseline.AddTicks(1234567), new DateTimeOffset(2026,1,2,3,4,5,new TimeSpan(-5,-30,0)),new DateTimeOffset(2000,1,1,0,0,0,TimeSpan.Zero),DateTimeOffset.MaxValue];
var assembly=typeof(ConversationCreated).Assembly;
Console.WriteLine(JsonSerializer.Serialize(new {
 sourceKind="trusted-current-Contracts-assembly", synthetic=true,
 assembly=assembly.GetName().FullName,
 informationalVersion=assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
 events=events.Select(e=>new {eventType=e.GetType().Name,payload=JsonSerializer.Serialize(e,e.GetType(),options)}),
 dateTimes=times.Select(t=>new {json=JsonSerializer.Serialize(t,options),utcTicks=t.UtcTicks})
},new JsonSerializerOptions(options){WriteIndented=true}));
