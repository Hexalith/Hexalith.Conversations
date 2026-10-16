using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.TestSupport;

/// <summary>Declarations and recomputed verification of a signed evidence manifest.</summary>
public sealed class EvidenceManifest
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal);
    private readonly List<EvidenceManifestRow> _rows = [];

    /// <summary>Declared repository-relative paths and lowercase SHA-256 values.</summary>
    public IReadOnlyDictionary<string, string> Digests => new ReadOnlyDictionary<string, string>(_digests);

    /// <summary>Signed rows in their original declaration order.</summary>
    public IReadOnlyList<EvidenceManifestRow> Rows => _rows.AsReadOnly();

    /// <summary>Recomputes a contained file hash from a bounded-memory stream.</summary>
    public static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(RepositoryLocator.ContainedFile(path));
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Recomputes a byte payload hash.</summary>
    public static string HashBytes(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    /// <summary>Recomputes a UTF-8 text payload hash without changing its canonicalization.</summary>
    public static string HashText(string content) => HashBytes(Encoding.UTF8.GetBytes(content));

    /// <summary>Adds a declaration without evaluating its evidence boundary.</summary>
    public void Add(string path, string sha256)
        => Add(path, sha256, "evidence");

    /// <summary>Adds a role-bearing declaration; paths are evaluated during verification.</summary>
    public void Add(string path, string sha256, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        if (path.IndexOfAny(['\t', '\r', '\n']) >= 0 || role.IndexOfAny(['\t', '\r', '\n']) >= 0)
            throw new ArgumentException("A signed path or role cannot contain row delimiters.");
        // Reject ill-formed UTF-16 before it can become replacement bytes in a signed payload.
        _ = StrictUtf8.GetByteCount(path);
        _ = StrictUtf8.GetByteCount(role);
        if (sha256.Length != 64 || sha256.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("A lowercase SHA-256 is required.", nameof(sha256));
        if (!_digests.TryAdd(path, sha256)) throw new ArgumentException("The evidence path was already declared.", nameof(path));
        _rows.Add(new(path, sha256, role));
    }

    /// <summary>Recomputes the UTF-8, BOM-free path-tab-hash-tab-role-LF payload digest.</summary>
    /// <param name="order">Canonical ordinal by default; declared order supports previously signed payloads only.</param>
    public string ComputeSignablePayloadHash(SignablePayloadOrder order = SignablePayloadOrder.Canonical)
    {
        if (!Enum.IsDefined(order)) throw new ArgumentOutOfRangeException(nameof(order));
        IEnumerable<EvidenceManifestRow> rows = order == SignablePayloadOrder.Declared
            ? _rows : _rows.OrderBy(row => row.Path, StringComparer.Ordinal);
        string payload = string.Concat(rows.Select(row => $"{row.Path}\t{row.Sha256}\t{row.Role}\n"));
        return Convert.ToHexStringLower(SHA256.HashData(StrictUtf8.GetBytes(payload)));
    }

    /// <summary>Recomputes each file digest and the signed row digest, recording every evaluation.</summary>
    public BoundaryEvaluation Verify(string root, string signablePayloadHash, AssertionLedger ledger,
        SignablePayloadOrder order = SignablePayloadOrder.Canonical)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        foreach (EvidenceManifestRow row in _rows)
        {
            string path = row.Path.Replace('\\', '/');
            GitFact<string> contained = RepositoryLocator.ResolveContainedPath(root, path);
            bool passed = true;
            if (!contained.IsSuccess || (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
            {
                ledger.Block("EVIDENCE_PATH_ESCAPE", row.Path, contained.Message ?? "An absolute drive path is not relative.");
                passed = false;
            }
            else if (Path.GetRelativePath(root, contained.Value!).Replace('\\', '/').Split('/').SkipLast(1).Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase) || segment.Equals("generated", StringComparison.OrdinalIgnoreCase)))
            {
                ledger.Block("EVIDENCE_OUTPUT_FORBIDDEN", row.Path, "Build and generated directory segments cannot be evidence.");
                passed = false;
            }
            else if (!File.Exists(contained.Value))
            {
                ledger.Block("EVIDENCE_FILE_MISSING", row.Path, "The evidence file does not exist.");
                passed = false;
            }
            else
            {
                try
                {
                    using FileStream file = File.OpenRead(contained.Value!);
                    string actual = Convert.ToHexStringLower(SHA256.HashData(file));
                    if (actual != row.Sha256)
                    {
                        ledger.Block("EVIDENCE_HASH_MISMATCH", row.Path, $"Expected {row.Sha256}; observed {actual}.");
                        passed = false;
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    ledger.Block("EVIDENCE_FILE_MISSING", row.Path, exception.Message);
                    passed = false;
                }
            }
            ledger.Record("manifest:file:" + row.Path, row.Path, passed);
        }
        string payloadHash = ComputeSignablePayloadHash(order);
        bool payloadPassed = string.Equals(payloadHash, signablePayloadHash, StringComparison.Ordinal);
        if (!payloadPassed)
            ledger.Block("SIGNABLE_PAYLOAD_MISMATCH", "manifest", $"Expected {signablePayloadHash}; observed {payloadHash}.");
        ledger.Record("manifest:signable-payload", "manifest", payloadPassed);
        return ledger.Evaluate();
    }
}
