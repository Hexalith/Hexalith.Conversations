using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Owner D5 classification of safe metadata solely for internal Projects equality scoring.</summary>
internal static class ConversationResolutionScoringPolicy
{
    /// <summary>The only supported purpose; inspection is never implied.</summary>
    internal const string Purpose = ConversationResolutionScoringLabel.SupportedPurpose;

    /// <summary>Persisted owner policy provenance, independent of any caller or grant.</summary>
    internal const string Classification = ConversationResolutionScoringLabel.OwnerClassification;

    /// <summary>Classifies the owner label only under the explicit scoring purpose and safe metadata rules.</summary>
    internal static bool CanClassify(string? label, string purpose)
    {
        if (purpose != Purpose) return false;
        if (label is null) return true;
        if (label.Length > ConversationResolutionMetadataReadModel.MaximumLabelLength) return false;
        try
        {
            _ = GovernanceContractValidation.RequiredSafeText(label, nameof(label));
            return true;
        }
        catch (ArgumentException) { return false; }
    }
}
