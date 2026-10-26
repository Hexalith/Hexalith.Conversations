namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public int Existing(int value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public int Existing(int value) => value; } }
