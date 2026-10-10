namespace Hexalith.Conversations.TestSupport;

/// <summary>A raw recursive tree entry.</summary>
/// <param name="Mode">Git tree mode.</param>
/// <param name="Type">Object type.</param>
/// <param name="ObjectId">Object identifier.</param>
/// <param name="Path">Repository relative path.</param>
public sealed record GitTreeEntry(string Mode, string Type, string ObjectId, string Path);
