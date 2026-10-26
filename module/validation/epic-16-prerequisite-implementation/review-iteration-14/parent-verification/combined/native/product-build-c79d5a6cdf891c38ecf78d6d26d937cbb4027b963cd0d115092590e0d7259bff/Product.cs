namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public string Existing(string value) => value; public int Added() => 1; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public string Existing(string value) => value; public int Added() => 1; } }
