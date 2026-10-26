from pathlib import Path
import json,hashlib,subprocess
root=Path.cwd();repo='references/Hexalith.Commons';path='src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs';source=(root/repo/path).read_text();start=source.index('    private static bool TryValidateTenantValue(');end=source.index('    private static async ValueTask<TenantAccessEvaluation<TRequirement>?> CheckProjectionHealthAsync',start);methods=source[start:end]
p=root/'_bmad/scripts/epic16_native_helper/Program.cs';s=p.read_text();s='using System.Globalization;\n'+s;marker='            else if (operation == "whitespace")';s=s.replace(marker,'''            else if (operation == "tenantAccessInputs")
            {
                // Exact retained Commons input rules, independent of opaque identifier rules.
                bool tenantValid = TryValidateTenantValue(root.GetProperty("tenant").GetString(), out _);
                bool principalValid = TryValidateCallerPrincipalId(root.GetProperty("principal").GetString());
                Console.Write(JsonSerializer.Serialize(new { tenantValid, principalValid }));
            }
'''+marker);s=s.replace('    private static string TypeName(',methods+'    private static string TypeName(');p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'_bmad/scripts/epic16_native.py';s=p.read_text();s+='''\n\n@lru_cache(maxsize=256)
def tenant_access_inputs(tenant: str, principal: str) -> dict:
    """Exact retained Commons request validation using the approved .NET runtime."""
    return _run(dict(operation='tenantAccessInputs', tenant=tenant, principal=principal))
''';p.write_text(s)
p=root/'_bmad/scripts/epic16_schedules.py';s=p.read_text().replace('from epic16_native import strict_json','from epic16_native import strict_json, tenant_access_inputs');s=s.replace("    require(scope['principal']==scope['principal'].strip(''.join(domain.NATIVE_WHITESPACE)) and not any(ord(c)<32 or 127<=ord(c)<=159 for c in scope['principal']), 'controlled native access principal is validated before the store read')", "    require(tenant_access_inputs(tenant, scope['principal']) == dict(tenantValid=True, principalValid=True), 'controlled native access tenant and principal are validated before the store read')");p.write_text(s)
p=root/'_bmad/scripts/tests/fixtures/epic16-domain-sources.json';v=json.loads(p.read_bytes());seen={(r['repository'],r['path']) for r in v['sources']}
for repo,folder,names in [('references/Hexalith.Commons','src/libraries/Hexalith.Commons.TenantAccess',['ITenantAccessStateStore.cs','ITenantAccessProjectionHealthProvider.cs','TenantAccessState.cs','TenantAccessProjectionHealth.cs','TenantAccessEvaluation.cs','TenantAccessDenialKind.cs']),('references/Hexalith.Tenants','src/Hexalith.Tenants.Contracts/Enums',['TenantStatus.cs','TenantStatusJsonConverter.cs'])]:
 for name in names:
  path=folder+'/'+name
  if (repo,path) in seen:continue
  raw=(root/repo/path).read_bytes();v['sources'].append(dict(repository=repo,path=path,revision=subprocess.check_output(['git','-C',repo,'rev-parse','HEAD']).decode().strip(),sha256=hashlib.sha256(raw).hexdigest(),source=raw.decode()))
p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n')
