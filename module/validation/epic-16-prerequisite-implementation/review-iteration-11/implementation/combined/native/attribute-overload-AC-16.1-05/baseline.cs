namespace Probe { public class StampAttribute : System.Attribute { public StampAttribute(int value) {} public StampAttribute(uint value) {} } [Stamp(1)] public class Api {} }
namespace Hexalith.Conversations.Contracts.Projections { public class ConversationDetailProjectionV1 { public string Existing(string value) => value; } }
