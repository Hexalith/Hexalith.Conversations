// <copyright file="ClosedVocabularyJsonConverters.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ConversationErrorCodeJsonConverter : ConversationStringValueJsonConverter<ConversationErrorCode>
{
    protected override ConversationErrorCode Create(string value) => ConversationErrorCode.Parse(value);

    protected override string GetValue(ConversationErrorCode value) => value.Value;
}

internal sealed class ConversationErrorCategoryJsonConverter : ConversationStringValueJsonConverter<ConversationErrorCategory>
{
    protected override ConversationErrorCategory Create(string value) => ConversationErrorCategory.Parse(value);

    protected override string GetValue(ConversationErrorCategory value) => value.Value;
}

internal sealed class ConversationCommandTypeJsonConverter : ConversationStringValueJsonConverter<ConversationCommandType>
{
    protected override ConversationCommandType Create(string value) => ConversationCommandType.Parse(value);

    protected override string GetValue(ConversationCommandType value) => value.Value;
}

internal sealed class ConversationEventTypeJsonConverter : ConversationStringValueJsonConverter<ConversationEventType>
{
    protected override ConversationEventType Create(string value) => ConversationEventType.Parse(value);

    protected override string GetValue(ConversationEventType value) => value.Value;
}

internal sealed class ConversationLifecycleStatusJsonConverter : ConversationStringValueJsonConverter<ConversationLifecycleStatus>
{
    protected override ConversationLifecycleStatus Create(string value) => ConversationLifecycleStatus.Parse(value);

    protected override string GetValue(ConversationLifecycleStatus value) => value.Value;
}

internal sealed class ParticipantTypeJsonConverter : ConversationStringValueJsonConverter<ParticipantType>
{
    protected override ParticipantType Create(string value) => ParticipantType.Parse(value);

    protected override string GetValue(ParticipantType value) => value.Value;
}

internal sealed class ParticipantRoleJsonConverter : ConversationStringValueJsonConverter<ParticipantRole>
{
    protected override ParticipantRole Create(string value) => ParticipantRole.Parse(value);

    protected override string GetValue(ParticipantRole value) => value.Value;
}

internal sealed class GovernanceOperationKindJsonConverter : ConversationStringValueJsonConverter<GovernanceOperationKind>
{
    protected override GovernanceOperationKind Create(string value) => GovernanceOperationKind.Parse(value);

    protected override string GetValue(GovernanceOperationKind value) => value.Value;
}

internal sealed class GovernedTargetKindJsonConverter : ConversationStringValueJsonConverter<GovernedTargetKind>
{
    protected override GovernedTargetKind Create(string value) => GovernedTargetKind.Parse(value);

    protected override string GetValue(GovernedTargetKind value) => value.Value;
}

internal sealed class RetentionActionJsonConverter : ConversationStringValueJsonConverter<RetentionAction>
{
    protected override RetentionAction Create(string value) => RetentionAction.Parse(value);

    protected override string GetValue(RetentionAction value) => value.Value;
}

internal sealed class SensitivityCategoryJsonConverter : ConversationStringValueJsonConverter<SensitivityCategory>
{
    protected override SensitivityCategory Create(string value) => SensitivityCategory.Parse(value);

    protected override string GetValue(SensitivityCategory value) => value.Value;
}

internal sealed class RedactionCategoryJsonConverter : ConversationStringValueJsonConverter<RedactionCategory>
{
    protected override RedactionCategory Create(string value) => RedactionCategory.Parse(value);

    protected override string GetValue(RedactionCategory value) => value.Value;
}

internal sealed class ArchivalStateJsonConverter : ConversationStringValueJsonConverter<ArchivalState>
{
    protected override ArchivalState Create(string value) => ArchivalState.Parse(value);

    protected override string GetValue(ArchivalState value) => value.Value;
}

internal sealed class LegalHoldDeferralJsonConverter : ConversationStringValueJsonConverter<LegalHoldDeferral>
{
    protected override LegalHoldDeferral Create(string value) => LegalHoldDeferral.Parse(value);

    protected override string GetValue(LegalHoldDeferral value) => value.Value;
}

internal sealed class PolicyBlockedOutcomeJsonConverter : ConversationStringValueJsonConverter<PolicyBlockedOutcome>
{
    protected override PolicyBlockedOutcome Create(string value) => PolicyBlockedOutcome.Parse(value);

    protected override string GetValue(PolicyBlockedOutcome value) => value.Value;
}

internal sealed class PrivilegedActionClassJsonConverter : ConversationStringValueJsonConverter<PrivilegedActionClass>
{
    protected override PrivilegedActionClass Create(string value) => PrivilegedActionClass.Parse(value);

    protected override string GetValue(PrivilegedActionClass value) => value.Value;
}

internal sealed class GovernanceOutcomeJsonConverter : ConversationStringValueJsonConverter<GovernanceOutcome>
{
    protected override GovernanceOutcome Create(string value) => GovernanceOutcome.Parse(value);

    protected override string GetValue(GovernanceOutcome value) => value.Value;
}

internal sealed class GovernanceRemediationJsonConverter : ConversationStringValueJsonConverter<GovernanceRemediation>
{
    protected override GovernanceRemediation Create(string value) => GovernanceRemediation.Parse(value);

    protected override string GetValue(GovernanceRemediation value) => value.Value;
}

internal sealed class GovernanceStateConceptJsonConverter : ConversationStringValueJsonConverter<GovernanceStateConcept>
{
    protected override GovernanceStateConcept Create(string value) => GovernanceStateConcept.Parse(value);

    protected override string GetValue(GovernanceStateConcept value) => value.Value;
}
