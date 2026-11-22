namespace Hexalith.Conversations.Server.Agents;

/// <summary>Transient failure before the owner could authorize or apply a pilot command.</summary>
internal sealed class ConversationWriteAuthorityUnavailableException()
    : Exception("Current conversation write authority could not be established.");
