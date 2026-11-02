namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>The actual list-query fields carried in the SDK query envelope payload.</summary>
/// <param name="Filter">The exact-match tenant-scoped filter, or the default empty filter.</param>
/// <param name="Page">The bounded page request, or the default first page.</param>
public sealed record ListConversationsPayload(ConversationListFilterV1? Filter, ConversationPageRequest? Page);
