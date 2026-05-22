// <copyright file="GovernanceVocabularies.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Governance.GovernanceVocabulary;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Defines public governance operation families prepared for governed mutation workflows.
/// </summary>
[JsonConverter(typeof(GovernanceOperationKindJsonConverter))]
public sealed record GovernanceOperationKind
{
    public static GovernanceOperationKind SetRetentionPolicy { get; } = new(nameof(SetRetentionPolicy));

    public static GovernanceOperationKind ReplaceRetentionPolicy { get; } = new(nameof(ReplaceRetentionPolicy));

    public static GovernanceOperationKind MarkContentSensitive { get; } = new(nameof(MarkContentSensitive));

    public static GovernanceOperationKind RedactMessageContent { get; } = new(nameof(RedactMessageContent));

    public static GovernanceOperationKind ArchiveConversation { get; } = new(nameof(ArchiveConversation));

    public static GovernanceOperationKind LogicallyDeleteConversation { get; } = new(nameof(LogicallyDeleteConversation));

    public static GovernanceOperationKind DeferForLegalHold { get; } = new(nameof(DeferForLegalHold));

    public static GovernanceOperationKind GovernAuditRecord { get; } = new(nameof(GovernAuditRecord));

    public static GovernanceOperationKind RecordPrivilegedJustification { get; } = new(nameof(RecordPrivilegedJustification));

    private static readonly IReadOnlyDictionary<string, GovernanceOperationKind> KnownValues = Known(
        SetRetentionPolicy,
        ReplaceRetentionPolicy,
        MarkContentSensitive,
        RedactMessageContent,
        ArchiveConversation,
        LogicallyDeleteConversation,
        DeferForLegalHold,
        GovernAuditRecord,
        RecordPrivilegedJustification);

    private GovernanceOperationKind(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static GovernanceOperationKind Parse(string value) => ParseKnown(value, KnownValues, nameof(GovernanceOperationKind));

    public override string ToString() => Value;
}

/// <summary>
/// Defines safe target categories for governance operations without carrying governed content.
/// </summary>
[JsonConverter(typeof(GovernedTargetKindJsonConverter))]
public sealed record GovernedTargetKind
{
    public static GovernedTargetKind Conversation { get; } = new(nameof(Conversation));

    public static GovernedTargetKind Message { get; } = new(nameof(Message));

    public static GovernedTargetKind File { get; } = new(nameof(File));

    public static GovernedTargetKind Participant { get; } = new(nameof(Participant));

    public static GovernedTargetKind AuditRecord { get; } = new(nameof(AuditRecord));

    public static GovernedTargetKind ContentSegment { get; } = new(nameof(ContentSegment));

    private static readonly IReadOnlyDictionary<string, GovernedTargetKind> KnownValues = Known(
        Conversation,
        Message,
        File,
        Participant,
        AuditRecord,
        ContentSegment);

    private GovernedTargetKind(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static GovernedTargetKind Parse(string value) => ParseKnown(value, KnownValues, nameof(GovernedTargetKind));

    public override string ToString() => Value;
}

/// <summary>
/// Defines bounded retention actions without implementing retention mutation behavior.
/// </summary>
[JsonConverter(typeof(RetentionActionJsonConverter))]
public sealed record RetentionAction
{
    public static RetentionAction ApplyPolicy { get; } = new(nameof(ApplyPolicy));

    public static RetentionAction ReplacePolicy { get; } = new(nameof(ReplacePolicy));

    public static RetentionAction EnforceRetention { get; } = new(nameof(EnforceRetention));

    public static RetentionAction DeferForLegalHold { get; } = new(nameof(DeferForLegalHold));

    private static readonly IReadOnlyDictionary<string, RetentionAction> KnownValues = Known(ApplyPolicy, ReplacePolicy, EnforceRetention, DeferForLegalHold);

    private RetentionAction(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static RetentionAction Parse(string value) => ParseKnown(value, KnownValues, nameof(RetentionAction));

    public override string ToString() => Value;
}

/// <summary>
/// Defines safe sensitivity classes that do not embed Party personal data or content values.
/// </summary>
[JsonConverter(typeof(SensitivityCategoryJsonConverter))]
public sealed record SensitivityCategory
{
    public static SensitivityCategory Sensitive { get; } = new(nameof(Sensitive));

    public static SensitivityCategory Restricted { get; } = new(nameof(Restricted));

    public static SensitivityCategory Regulated { get; } = new(nameof(Regulated));

    private static readonly IReadOnlyDictionary<string, SensitivityCategory> KnownValues = Known(Sensitive, Restricted, Regulated);

    private SensitivityCategory(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static SensitivityCategory Parse(string value) => ParseKnown(value, KnownValues, nameof(SensitivityCategory));

    public override string ToString() => Value;
}

/// <summary>
/// Defines append-only redaction classes for future policy-governed display changes.
/// </summary>
[JsonConverter(typeof(RedactionCategoryJsonConverter))]
public sealed record RedactionCategory
{
    public static RedactionCategory DisplayMask { get; } = new(nameof(DisplayMask));

    public static RedactionCategory ContentSuppression { get; } = new(nameof(ContentSuppression));

    public static RedactionCategory ReferenceWithheld { get; } = new(nameof(ReferenceWithheld));

    private static readonly IReadOnlyDictionary<string, RedactionCategory> KnownValues = Known(DisplayMask, ContentSuppression, ReferenceWithheld);

    private RedactionCategory(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static RedactionCategory Parse(string value) => ParseKnown(value, KnownValues, nameof(RedactionCategory));

    public override string ToString() => Value;
}

/// <summary>
/// Defines public archival and logical deletion states without implying irreversible source-event deletion.
/// </summary>
[JsonConverter(typeof(ArchivalStateJsonConverter))]
public sealed record ArchivalState
{
    public static ArchivalState Active { get; } = new(nameof(Active));

    public static ArchivalState Archived { get; } = new(nameof(Archived));

    public static ArchivalState LogicallyDeleted { get; } = new(nameof(LogicallyDeleted));

    public static ArchivalState Closed { get; } = new(nameof(Closed));

    private static readonly IReadOnlyDictionary<string, ArchivalState> KnownValues = Known(Active, Archived, LogicallyDeleted, Closed);

    private ArchivalState(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static ArchivalState Parse(string value) => ParseKnown(value, KnownValues, nameof(ArchivalState));

    public override string ToString() => Value;
}

/// <summary>
/// Defines legal-hold deferral states as explicit governance outcomes, never silent no-ops.
/// </summary>
[JsonConverter(typeof(LegalHoldDeferralJsonConverter))]
public sealed record LegalHoldDeferral
{
    public static LegalHoldDeferral NotDeferred { get; } = new(nameof(NotDeferred));

    public static LegalHoldDeferral DeferredUntilRelease { get; } = new(nameof(DeferredUntilRelease));

    private static readonly IReadOnlyDictionary<string, LegalHoldDeferral> KnownValues = Known(NotDeferred, DeferredUntilRelease);

    private LegalHoldDeferral(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static LegalHoldDeferral Parse(string value) => ParseKnown(value, KnownValues, nameof(LegalHoldDeferral));

    public override string ToString() => Value;
}

/// <summary>
/// Defines bounded policy-blocked reasons that avoid disclosing policy internals.
/// </summary>
[JsonConverter(typeof(PolicyBlockedOutcomeJsonConverter))]
public sealed record PolicyBlockedOutcome
{
    public static PolicyBlockedOutcome PolicyDenied { get; } = new(nameof(PolicyDenied));

    public static PolicyBlockedOutcome LegalHoldActive { get; } = new(nameof(LegalHoldActive));

    public static PolicyBlockedOutcome PrivilegeRequired { get; } = new(nameof(PrivilegeRequired));

    private static readonly IReadOnlyDictionary<string, PolicyBlockedOutcome> KnownValues = Known(PolicyDenied, LegalHoldActive, PrivilegeRequired);

    private PolicyBlockedOutcome(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static PolicyBlockedOutcome Parse(string value) => ParseKnown(value, KnownValues, nameof(PolicyBlockedOutcome));

    public override string ToString() => Value;
}

/// <summary>
/// Defines privileged action classes for operator justification contracts.
/// </summary>
[JsonConverter(typeof(PrivilegedActionClassJsonConverter))]
public sealed record PrivilegedActionClass
{
    public static PrivilegedActionClass OperationalOverride { get; } = new(nameof(OperationalOverride));

    public static PrivilegedActionClass ComplianceReview { get; } = new(nameof(ComplianceReview));

    public static PrivilegedActionClass SupportAssistance { get; } = new(nameof(SupportAssistance));

    private static readonly IReadOnlyDictionary<string, PrivilegedActionClass> KnownValues = Known(OperationalOverride, ComplianceReview, SupportAssistance);

    private PrivilegedActionClass(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static PrivilegedActionClass Parse(string value) => ParseKnown(value, KnownValues, nameof(PrivilegedActionClass));

    public override string ToString() => Value;
}

/// <summary>
/// Defines public evidence result states for governance operations.
/// </summary>
[JsonConverter(typeof(GovernanceOutcomeJsonConverter))]
public sealed record GovernanceOutcome
{
    public static GovernanceOutcome Succeeded { get; } = new(nameof(Succeeded));

    public static GovernanceOutcome Denied { get; } = new(nameof(Denied));

    public static GovernanceOutcome AuditUnavailableFailed { get; } = new(nameof(AuditUnavailableFailed));

    public static GovernanceOutcome PolicyBlocked { get; } = new(nameof(PolicyBlocked));

    private static readonly IReadOnlyDictionary<string, GovernanceOutcome> KnownValues = Known(Succeeded, Denied, AuditUnavailableFailed, PolicyBlocked);

    private GovernanceOutcome(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static GovernanceOutcome Parse(string value) => ParseKnown(value, KnownValues, nameof(GovernanceOutcome));

    public override string ToString() => Value;
}

/// <summary>
/// Defines safe remediation classes without exposing diagnostics or target existence.
/// </summary>
[JsonConverter(typeof(GovernanceRemediationJsonConverter))]
public sealed record GovernanceRemediation
{
    public static GovernanceRemediation None { get; } = new(nameof(None));

    public static GovernanceRemediation ResubmitWithPolicyReference { get; } = new(nameof(ResubmitWithPolicyReference));

    public static GovernanceRemediation RetryWhenAuditAvailable { get; } = new(nameof(RetryWhenAuditAvailable));

    public static GovernanceRemediation RequestAuthorization { get; } = new(nameof(RequestAuthorization));

    public static GovernanceRemediation WaitForLegalHoldRelease { get; } = new(nameof(WaitForLegalHoldRelease));

    private static readonly IReadOnlyDictionary<string, GovernanceRemediation> KnownValues = Known(
        None,
        ResubmitWithPolicyReference,
        RetryWhenAuditAvailable,
        RequestAuthorization,
        WaitForLegalHoldRelease);

    private GovernanceRemediation(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static GovernanceRemediation Parse(string value) => ParseKnown(value, KnownValues, nameof(GovernanceRemediation));

    public override string ToString() => Value;
}

/// <summary>
/// Defines governance state concepts that projections must keep distinct.
/// </summary>
[JsonConverter(typeof(GovernanceStateConceptJsonConverter))]
public sealed record GovernanceStateConcept
{
    public static GovernanceStateConcept EventHistory { get; } = new(nameof(EventHistory));

    public static GovernanceStateConcept DisplayedContent { get; } = new(nameof(DisplayedContent));

    public static GovernanceStateConcept AuditRecord { get; } = new(nameof(AuditRecord));

    public static GovernanceStateConcept DerivedMaterialization { get; } = new(nameof(DerivedMaterialization));

    public static GovernanceStateConcept Archival { get; } = new(nameof(Archival));

    public static GovernanceStateConcept LogicalDeletion { get; } = new(nameof(LogicalDeletion));

    public static GovernanceStateConcept RetentionEnforcement { get; } = new(nameof(RetentionEnforcement));

    public static GovernanceStateConcept LegalHoldDeferral { get; } = new(nameof(LegalHoldDeferral));

    private static readonly IReadOnlyDictionary<string, GovernanceStateConcept> KnownValues = Known(
        EventHistory,
        DisplayedContent,
        AuditRecord,
        DerivedMaterialization,
        Archival,
        LogicalDeletion,
        RetentionEnforcement,
        LegalHoldDeferral);

    private GovernanceStateConcept(string value) => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static GovernanceStateConcept Parse(string value) => ParseKnown(value, KnownValues, nameof(GovernanceStateConcept));

    public override string ToString() => Value;
}

file static class GovernanceVocabulary
{
    internal static IReadOnlyDictionary<string, T> Known<T>(params T[] values)
        where T : notnull
        => values.ToDictionary(value => value.ToString() ?? string.Empty, StringComparer.Ordinal);

    internal static T ParseKnown<T>(string value, IReadOnlyDictionary<string, T> knownValues, string vocabularyName)
    {
        GovernanceContractValidation.RequiredSafeToken(value, nameof(value));
        return knownValues.TryGetValue(value, out T? known)
            ? known
            : throw new ArgumentException($"Unsupported {vocabularyName} value.", nameof(value));
    }
}
