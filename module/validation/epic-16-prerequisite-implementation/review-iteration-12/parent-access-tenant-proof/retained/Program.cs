using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
foreach (var c in new[]{(Id:"invalid-tenant", Principal:"actor-a", Role:TenantRole.TenantOwner)}) {
 var store=new Store(c.Principal,c.Role);var health=new Health();
 var result=await TenantAccessEvaluator.EvaluateAsync("Read",new[]{"tenant/a"},c.Principal,store,health,
 static requirement=>requirement=="Read", static status=>Enum.IsDefined((TenantStatus)status),static status=>(TenantStatus)status==TenantStatus.Active,static status=>(TenantStatus)status==TenantStatus.Disabled,
 static role=>Enum.IsDefined((TenantRole)role), static (role,requirement)=>(TenantRole)role is TenantRole.TenantReader or TenantRole.TenantContributor or TenantRole.TenantOwner && requirement=="Read",NullLogger.Instance);
 Console.WriteLine(JsonSerializer.Serialize(new{c.Id,result.IsAllowed,Denial=result.DenialKind.ToString(),store.Reads,health.ReadsHealth}));
}
sealed class Store(string principal,TenantRole role):ITenantAccessStateStore {public int Reads;public Task<TenantAccessState?> GetAsync(string tenantId,CancellationToken ct=default){Reads++;return Task.FromResult<TenantAccessState?>(new(tenantId,(int)TenantStatus.Active,new Dictionary<string,int>{{principal,(int)role}}));}}
sealed class Health:ITenantAccessProjectionHealthProvider {public int ReadsHealth;public ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(string tenantId,CancellationToken ct=default){ReadsHealth++;return ValueTask.FromResult<TenantAccessProjectionHealth?>(new(10,"10",false,false,false,false));}}
