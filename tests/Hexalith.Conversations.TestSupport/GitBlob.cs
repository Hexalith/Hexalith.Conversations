namespace Hexalith.Conversations.TestSupport;

/// <summary>A historical blob and its content digest.</summary>
/// <param name="Revision">Resolved commit identifier.</param>
/// <param name="Path">Repository relative path.</param>
/// <param name="Content">Exact historical bytes.</param>
/// <param name="Sha256">Lowercase SHA-256 of the content.</param>
public sealed record GitBlob(string Revision, string Path, byte[] Content, string Sha256);
