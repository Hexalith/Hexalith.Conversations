namespace Hexalith.Conversations.TestSupport;

/// <summary>A declared evidence path, digest, and signed role.</summary>
/// <param name="Path">The repository-relative path.</param>
/// <param name="Sha256">The declared lowercase SHA-256.</param>
/// <param name="Role">The signed evidence role.</param>
public sealed record EvidenceManifestRow(string Path, string Sha256, string Role);
