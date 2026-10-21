using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Permanent content-free proof of one metadata generation.</summary>
/// <param name="TenantId">The Tenant binding.</param>
/// <param name="ConversationId">The resource binding.</param>
/// <param name="DispatchId">The stable dispatch.</param>
/// <param name="RequestFingerprint">The native source fingerprint.</param>
/// <param name="Freshness">The unmodified native generation.</param>
/// <param name="SubsetDigest">The isolated subset digest.</param>
/// <param name="IsPending">Whether durable components are still incomplete.</param>
public sealed record ConversationResolutionGeneration(
    TenantId TenantId, ConversationId ConversationId, string DispatchId,
    string RequestFingerprint, ProjectionFreshnessV1 Freshness, string SubsetDigest, bool IsPending)
{
    /// <summary>Gets the original frozen admission decision; omitted for legacy generations.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ConversationProjectionNativeAdmission? NativeAdmission { get; init; }

    /// <summary>Gets the original SDK-verified native incarnation and complete prefix; absent for legacy input.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Hexalith.EventStore.Contracts.Streams.NativeSourceProof? NativeSourceProof { get; init; }

    /// <summary>Gets the original named producer's source-local head, absent for legacy generations.</summary>
    public long? SourceHeadSequence { get; init; }

    /// <summary>Gets the original persisted event identity; redelivery cannot replace it with transport metadata.</summary>
    public string? SourceHeadMessageId { get; init; }

    /// <summary>Creates the content-free binding.</summary>
    internal static ConversationResolutionGeneration From(ConversationResolutionMetadataReadModel model, bool pending)
        => new(model.TenantId, model.ConversationId, model.DispatchId, model.RequestFingerprint,
            model.Freshness, model.Digest(), pending)
        { NativeAdmission = model.NativeAdmission, NativeSourceProof = model.NativeSourceProof, SourceHeadSequence = model.SourceHeadSequence, SourceHeadMessageId = model.SourceHeadMessageId };

    /// <summary>Compares using native position then ordinal dispatch identity.</summary>
    internal int CompareTo(ConversationResolutionGeneration other)
    {
        int order = Freshness.LastAppliedEventPosition.CompareTo(other.Freshness.LastAppliedEventPosition);
        return order == 0 ? StringComparer.Ordinal.Compare(DispatchId, other.DispatchId) : order;
    }

    /// <summary>Checks every field that binds a stored subset.</summary>
    internal bool Matches(ConversationResolutionMetadataReadModel model)
        => model.IsWellFormed() && TenantId == model.TenantId && ConversationId == model.ConversationId
            && DispatchId == model.DispatchId && RequestFingerprint == model.RequestFingerprint
            && Freshness == model.Freshness && SubsetDigest == model.Digest()
            && NativeSourceProof == model.NativeSourceProof
            && NativeAdmission == model.NativeAdmission
            && SourceHeadSequence == model.SourceHeadSequence && SourceHeadMessageId == model.SourceHeadMessageId;
}
