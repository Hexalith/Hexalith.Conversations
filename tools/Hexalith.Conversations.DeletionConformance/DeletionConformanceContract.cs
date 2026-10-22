using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Closed qualification identity and deterministic receipt bindings.</summary>
public static class DeletionConformanceContract
{
    /// <summary>The isolated EventStore domain; never an Agents domain.</summary>
    public const string Domain = "conversation-deletion-conformance";

    /// <summary>The only qualification scope this service can report.</summary>
    public const string QualificationScope = "OQ-5.4-1";

    /// <summary>Checks a complete canonical source identity without granting source authority.</summary>
    public static bool ValidSignal(ConversationDeletionSignal? signal)
    {
        try
        {
            return signal is not null && signal.TenantId is not null && signal.ConversationId is not null
                && signal.SourceRevision > 0 && signal.SourceContractVersion == 1
                && !string.IsNullOrWhiteSpace(signal.ApprovalReference) && signal.ApprovalReference.Length <= 1024
                && new AggregateIdentity(signal.TenantId.Value, Domain, signal.ConversationDeletionSignalId).AggregateId == signal.ConversationDeletionSignalId
                && new AggregateIdentity(signal.TenantId.Value, "conversation", signal.ConversationId.Value).ActorId == signal.SourceStream;
        }
        catch (ArgumentException) { return false; }
    }

    /// <summary>Requires a canonical immutable full source target.</summary>
    public static bool ValidTarget(string? target) => target is { Length: 40 }
        && target.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>Builds a content-free receipt reference; only authenticated persisted lookup proves acceptance.</summary>
    public static string Evidence(ConversationDeletionSignal signal, string target, long revision)
        => "oq-conformance-v1:" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            QualificationScope, signal.ConversationDeletionSignalId, signal.TenantId.Value, signal.ConversationId.Value,
            signal.SourceStream, signal.SourceRevision.ToString(CultureInfo.InvariantCulture), signal.ApprovalReference,
            signal.SourceContractVersion.ToString(CultureInfo.InvariantCulture), target, revision.ToString(CultureInfo.InvariantCulture),
        })));
}
