namespace Hexalith.EventStore.Client.Projections { public class IReadModelStore { public string Existing(string value) => value; } }
namespace Hexalith.EventStore.Client.Streams { public class IAuthoritativeEventStreamReader { public string Existing(string value) => value; } }
