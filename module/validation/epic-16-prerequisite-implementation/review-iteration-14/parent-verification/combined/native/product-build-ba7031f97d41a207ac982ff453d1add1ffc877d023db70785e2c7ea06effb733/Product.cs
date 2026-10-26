namespace Hexalith.Tenants.Client.Projections { public struct ITenantProjectionStore { public string Existing(string value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public struct TenantProjectionEventHandler { public string Existing(string value) => value; } }
