namespace Hexalith.Conversations.TestSupport;

/// <summary>Neutral declaration of evidence paths and expected digests.</summary>
public sealed class EvidenceManifest
{
    private readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal);

    /// <summary>Declared repository-relative paths and lowercase SHA-256 values.</summary>
    public IReadOnlyDictionary<string, string> Digests => _digests;

    /// <summary>Adds a declaration without evaluating its evidence boundary.</summary>
    public void Add(string path, string sha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        if (!_digests.TryAdd(path, sha256)) throw new ArgumentException("The evidence path was already declared.", nameof(path));
    }
}
