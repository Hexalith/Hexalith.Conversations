using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>Explicit owner classification of a native label for internal Projects equality scoring.</summary>
public sealed record ConversationResolutionScoringLabel
{
    /// <summary>The sole supported purpose; classification never grants inspection or resource access.</summary>
    public const string SupportedPurpose = "projects-internal-equality-scoring";

    /// <summary>The native owner policy recorded with the label.</summary>
    public const string OwnerClassification = "conversations-owner-safe-metadata-equality-v1";

    /// <summary>The supported label length, measured without trimming or truncation.</summary>
    public const int MaximumLength = 160;

    /// <summary>Creates a classified label only after validating its exact purpose, policy and content.</summary>
    /// <param name="value">The exact descriptive label, or explicitly classified absence.</param>
    /// <param name="purpose">The explicit supported scoring purpose.</param>
    /// <param name="classification">The exact owner policy.</param>
    [JsonConstructor]
    public ConversationResolutionScoringLabel(string? value, string purpose, string classification)
    {
        if (purpose != SupportedPurpose || classification != OwnerClassification)
        {
            throw new ArgumentException("Unsupported scoring label policy.", nameof(purpose));
        }

        if (value is not null)
        {
            if (value.Length > MaximumLength)
            {
                throw new ArgumentException("The scoring label exceeds its supported bound.", nameof(value));
            }

            _ = GovernanceContractValidation.RequiredSafeText(value, nameof(value));
        }

        Value = value;
        Purpose = purpose;
        Classification = classification;
    }

    /// <summary>Gets the original, bounded native label without normalization.</summary>
    public string? Value { get; }

    /// <summary>Gets the explicitly persisted scoring purpose.</summary>
    public string Purpose { get; }

    /// <summary>Gets the explicitly persisted owner classification.</summary>
    public string Classification { get; }
}
