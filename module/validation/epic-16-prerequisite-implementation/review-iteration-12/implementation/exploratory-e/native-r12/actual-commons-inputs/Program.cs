using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
foreach (var c in JsonDocument.Parse(Console.In.ReadToEnd()).RootElement.EnumerateArray()) {
 string tenant=c.GetProperty("tenant").GetString()!,principal=c.GetProperty("principal").GetString()!;
 var store=new Store(principal);var health=new Health();
 var result=await TenantAccessEvaluator.EvaluateAsync("Write",new[]{tenant},principal,store,health,
 static r=>r=="Write",static s=>Enum.IsDefined((TenantStatus)s),static s=>(TenantStatus)s==TenantStatus.Active,static s=>(TenantStatus)s==TenantStatus.Disabled,
 static r=>Enum.IsDefined((TenantRole)r),static (r,q)=>(TenantRole)r is TenantRole.TenantContributor or TenantRole.TenantOwner,NullLogger.Instance);
 Console.WriteLine(JsonSerializer.Serialize(new{tenant,principal,result.IsAllowed,Denial=result.DenialKind.ToString(),store.Reads,health.ReadsHealth}));
}