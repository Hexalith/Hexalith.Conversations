namespace Probe { public class StampAttribute : System.Attribute { public StampAttribute(int value) {} public StampAttribute(uint value) {} } [Stamp(1)] public class Api {} }
namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public string Existing(string value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public string Existing(string value) => value; } }
