using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Diagnostics;
var logger=new CaptureLogger<DefaultConversationAuditAvailabilitySignal>();
var result=await new DefaultConversationAuditAvailabilitySignal(logger).GetAuditAvailabilityAsync(new TenantId("tenant-a"));
Console.WriteLine(JsonSerializer.Serialize(new {synthetic=true,tenant="tenant-a",result=result.ToString(),emitted=logger.Rows},new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
