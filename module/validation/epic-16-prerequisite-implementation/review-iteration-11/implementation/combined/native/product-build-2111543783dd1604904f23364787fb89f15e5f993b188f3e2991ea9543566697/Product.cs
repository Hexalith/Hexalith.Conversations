namespace Probe { [System.Runtime.InteropServices.ComImport][System.Runtime.InteropServices.Guid("F0000000-0000-0000-0000-000000000001")] public interface Api {  int Method(); } }
namespace Hexalith.Tenants.Client.Projections { public class ITenantProjectionStore { public string Existing(string value) => value; } }
namespace Hexalith.Tenants.Client.Handlers { public class TenantProjectionEventHandler { public string Existing(string value) => value; } }
