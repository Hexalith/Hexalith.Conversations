namespace Probe { public interface IBase<T> {} public interface Api : IBase<string> {} }
namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public string Existing(string value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public string Existing(string value) => value; } }
