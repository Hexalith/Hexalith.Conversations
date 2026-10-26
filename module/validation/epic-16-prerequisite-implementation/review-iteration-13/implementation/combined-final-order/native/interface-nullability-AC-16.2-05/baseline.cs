namespace Probe { public interface IBase<T> {} public interface Api : IBase<string> {} }
namespace Hexalith.Conversations.Contracts.Projections { public class ConversationDetailProjectionV1 { public string Existing(string value) => value; } }
