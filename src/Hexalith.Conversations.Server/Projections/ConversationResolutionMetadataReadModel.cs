using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Isolated metadata with native projection generation; never contains caller grants.</summary>
/// <param name="TenantId">The owner Tenant.</param>
/// <param name="ConversationId">The exact resource.</param>
/// <param name="Metadata">The bounded scoring subset.</param>
/// <param name="Freshness">Native freshness, including original generation time.</param>
/// <param name="DispatchId">The stable winning dispatch.</param>
/// <param name="RequestFingerprint">The source request fingerprint.</param>
/// <param name="LabelClassification">The explicit owner scoring policy provenance, never a grant.</param>
/// <param name="IsLabelComplete">Whether the original label fits the scoring contract.</param>
public sealed record ConversationResolutionMetadataReadModel(
    TenantId TenantId,
    ConversationId ConversationId,
    ConversationResolutionMetadataV1 Metadata,
    ProjectionFreshnessV1 Freshness,
    string DispatchId,
    string RequestFingerprint,
    bool IsLabelComplete,
    string? LabelClassification = null)
{
    /// <summary>Gets the original frozen admission decision; omitted for legacy generations.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ConversationProjectionNativeAdmission? NativeAdmission { get; init; }

    /// <summary>Gets the original SDK-verified native incarnation and complete prefix; absent for legacy input.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Hexalith.EventStore.Contracts.Streams.NativeSourceProof? NativeSourceProof { get; init; }

    /// <summary>Gets the exact native source-local sequence admitted by the named producer.</summary>
    public long? SourceHeadSequence { get; init; }

    /// <summary>Gets the actual persisted event identity, never an allocated generation identity.</summary>
    public string? SourceHeadMessageId { get; init; }

    /// <summary>Gets the native label purpose persisted with this isolated generation.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? LabelPurpose { get; init; }

    /// <summary>The owner classification bound for internal equality scoring.</summary>
    public const int MaximumLabelLength = ConversationResolutionScoringLabel.MaximumLength;

    /// <summary>Builds the classified subset without truncating over-bound labels.</summary>
    internal static ConversationResolutionMetadataReadModel From(
        ConversationProjectedReadModels models, string fingerprint)
    {
        var summary = models.Summary;
        ConversationResolutionScoringLabel? scoring = models.ResolutionScoringLabel;
        bool complete = scoring is null ? summary.Label is null : scoring.Value == summary.Label
            && scoring.Purpose == ConversationResolutionScoringPolicy.Purpose
            && scoring.Classification == ConversationResolutionScoringPolicy.Classification
            && ConversationResolutionScoringPolicy.CanClassify(scoring.Value, scoring.Purpose);
        return new(summary.TenantId, summary.ConversationId,
            new(summary.ConversationId.Value, summary.ProjectId?.Value,
                complete ? summary.Label : null, summary.LifecycleState),
            summary.Freshness, models.DispatchId, fingerprint, complete,
            complete ? ConversationResolutionScoringPolicy.Classification : null)
        { NativeAdmission = models.NativeAdmission, NativeSourceProof = models.NativeSourceProof, SourceHeadSequence = models.SourceHeadSequence, SourceHeadMessageId = models.SourceHeadMessageId,
            LabelPurpose = complete ? ConversationResolutionScoringPolicy.Purpose : null };
    }

    /// <summary>Computes a content-free consistency binding over the entire isolated generation.</summary>
    internal string Digest() => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this)));

    /// <summary>Checks stored subset invariants before any disclosure.</summary>
    internal bool IsWellFormed() => TenantId is not null && ConversationId is not null
        && Metadata is not null && Freshness is not null
        && Freshness.ProjectionContractSchemaVersion == SchemaVersion.Current
        && Metadata.ConversationId == ConversationId.Value
        && Metadata.LifecycleState is "Initializing" or "Open" or "Closed" or "Archived"
        && (Metadata.ProjectId is null || !string.IsNullOrWhiteSpace(Metadata.ProjectId))
        && Metadata.SafeLabel?.Length is not > MaximumLabelLength
        && (IsLabelComplete
            ? LabelClassification == ConversationResolutionScoringPolicy.Classification
                && LabelPurpose == ConversationResolutionScoringPolicy.Purpose
                && ConversationResolutionScoringPolicy.CanClassify(Metadata.SafeLabel, ConversationResolutionScoringPolicy.Purpose)
            : Metadata.SafeLabel is null && LabelClassification is null && LabelPurpose is null)
        && !string.IsNullOrWhiteSpace(DispatchId)
        && (NativeAdmission is null || NativeAdmission.AdmittedProof == NativeSourceProof
            && (NativeSourceProof is null || NativeAdmission.ProducerHint == NativeSourceProof))
        && (NativeSourceProof is null || NativeSourceProof is { Erased: false } proof
            && proof.HeadSequence == SourceHeadSequence && proof.HeadMessageId == SourceHeadMessageId
            && Hexalith.EventStore.Client.Streams.NativeSourceProofCodec.IsValid(proof,
                new(TenantId.Value, ConversationProjectionHandler.ConversationDomain, ConversationId.Value),
                proof.HeadSequence, proof.RetainedFloor))
        && RequestFingerprint is { Length: 64 } && RequestFingerprint.All(Uri.IsHexDigit);
}
