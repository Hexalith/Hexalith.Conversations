namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public string RemovedReplacement(string value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public string RemovedReplacement(string value) => value; } }
