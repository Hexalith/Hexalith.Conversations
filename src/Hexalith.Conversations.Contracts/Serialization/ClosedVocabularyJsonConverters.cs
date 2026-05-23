// <copyright file="ClosedVocabularyJsonConverters.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

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

internal sealed class ConversationErrorClientActionJsonConverter : ConversationStringValueJsonConverter<ConversationErrorClientAction>
{
    protected override ConversationErrorClientAction Create(string value) => ConversationErrorClientAction.Parse(value);

    protected override string GetValue(ConversationErrorClientAction value) => value.Value;
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

internal sealed class PrivilegedOperationalActionClassJsonConverter : ConversationStringValueJsonConverter<PrivilegedOperationalActionClass>
{
    protected override PrivilegedOperationalActionClass Create(string value) => PrivilegedOperationalActionClass.Parse(value);

    protected override string GetValue(PrivilegedOperationalActionClass value) => value.Value;
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

internal sealed class ConversationGovernanceVerificationScopeKindJsonConverter :
    ConversationStringValueJsonConverter<ConversationGovernanceVerificationScopeKind>
{
    protected override ConversationGovernanceVerificationScopeKind Create(string value)
        => ConversationGovernanceVerificationScopeKind.Parse(value);

    protected override string GetValue(ConversationGovernanceVerificationScopeKind value) => value.Value;
}

internal sealed class ConversationGovernanceVerificationSuiteJsonConverter :
    ConversationStringValueJsonConverter<ConversationGovernanceVerificationSuite>
{
    protected override ConversationGovernanceVerificationSuite Create(string value)
        => ConversationGovernanceVerificationSuite.Parse(value);

    protected override string GetValue(ConversationGovernanceVerificationSuite value) => value.Value;
}

internal sealed class ConversationGovernanceVerificationExecutionStatusJsonConverter :
    ConversationStringValueJsonConverter<ConversationGovernanceVerificationExecutionStatus>
{
    protected override ConversationGovernanceVerificationExecutionStatus Create(string value)
        => ConversationGovernanceVerificationExecutionStatus.Parse(value);

    protected override string GetValue(ConversationGovernanceVerificationExecutionStatus value) => value.Value;
}

internal sealed class ConversationGovernanceVerificationFailureClassificationJsonConverter :
    ConversationStringValueJsonConverter<ConversationGovernanceVerificationFailureClassification>
{
    protected override ConversationGovernanceVerificationFailureClassification Create(string value)
        => ConversationGovernanceVerificationFailureClassification.Parse(value);

    protected override string GetValue(ConversationGovernanceVerificationFailureClassification value) => value.Value;
}

internal sealed class ConversationGovernanceVerificationRemediationJsonConverter :
    ConversationStringValueJsonConverter<ConversationGovernanceVerificationRemediation>
{
    protected override ConversationGovernanceVerificationRemediation Create(string value)
        => ConversationGovernanceVerificationRemediation.Parse(value);

    protected override string GetValue(ConversationGovernanceVerificationRemediation value) => value.Value;
}

internal sealed class AuditRecordActionClassificationJsonConverter : ConversationStringValueJsonConverter<AuditRecordActionClassification>
{
    protected override AuditRecordActionClassification Create(string value) => AuditRecordActionClassification.Parse(value);

    protected override string GetValue(AuditRecordActionClassification value) => value.Value;
}

internal sealed class ConversationCitationAvailabilityJsonConverter : ConversationStringValueJsonConverter<ConversationCitationAvailability>
{
    protected override ConversationCitationAvailability Create(string value) => ConversationCitationAvailability.Parse(value);

    protected override string GetValue(ConversationCitationAvailability value) => value.Value;
}

internal sealed class ConversationAuditReadinessStateJsonConverter : ConversationStringValueJsonConverter<ConversationAuditReadinessState>
{
    protected override ConversationAuditReadinessState Create(string value) => ConversationAuditReadinessState.Parse(value);

    protected override string GetValue(ConversationAuditReadinessState value) => value.Value;
}

internal sealed class ConversationVerificationStateJsonConverter : ConversationStringValueJsonConverter<ConversationVerificationState>
{
    protected override ConversationVerificationState Create(string value) => ConversationVerificationState.Parse(value);

    protected override string GetValue(ConversationVerificationState value) => value.Value;
}

internal sealed class ConversationSearchMatchSourceJsonConverter : ConversationStringValueJsonConverter<ConversationSearchMatchSource>
{
    protected override ConversationSearchMatchSource Create(string value) => ConversationSearchMatchSource.Parse(value);

    protected override string GetValue(ConversationSearchMatchSource value) => value.Value;
}

internal sealed class BuyerAcceptanceDemoStepKindJsonConverter : ConversationStringValueJsonConverter<BuyerAcceptanceDemoStepKind>
{
    protected override BuyerAcceptanceDemoStepKind Create(string value) => BuyerAcceptanceDemoStepKind.Parse(value);

    protected override string GetValue(BuyerAcceptanceDemoStepKind value) => value.Value;
}

internal sealed class BuyerAcceptanceDemoFixtureKindJsonConverter : ConversationStringValueJsonConverter<BuyerAcceptanceDemoFixtureKind>
{
    protected override BuyerAcceptanceDemoFixtureKind Create(string value) => BuyerAcceptanceDemoFixtureKind.Parse(value);

    protected override string GetValue(BuyerAcceptanceDemoFixtureKind value) => value.Value;
}

internal sealed class BuyerAcceptanceDemoTrustStateJsonConverter : ConversationStringValueJsonConverter<BuyerAcceptanceDemoTrustState>
{
    protected override BuyerAcceptanceDemoTrustState Create(string value) => BuyerAcceptanceDemoTrustState.Parse(value);

    protected override string GetValue(BuyerAcceptanceDemoTrustState value) => value.Value;
}

internal sealed class BuyerAcceptanceEvidenceOwnershipJsonConverter :
    ConversationStringValueJsonConverter<BuyerAcceptanceEvidenceOwnership>
{
    protected override BuyerAcceptanceEvidenceOwnership Create(string value) => BuyerAcceptanceEvidenceOwnership.Parse(value);

    protected override string GetValue(BuyerAcceptanceEvidenceOwnership value) => value.Value;
}

internal sealed class BuyerAcceptanceDemoExecutionStatusJsonConverter :
    ConversationStringValueJsonConverter<BuyerAcceptanceDemoExecutionStatus>
{
    protected override BuyerAcceptanceDemoExecutionStatus Create(string value) => BuyerAcceptanceDemoExecutionStatus.Parse(value);

    protected override string GetValue(BuyerAcceptanceDemoExecutionStatus value) => value.Value;
}

internal sealed class ContractCompatibilityStatusJsonConverter :
    ConversationStringValueJsonConverter<ContractCompatibilityStatus>
{
    protected override ContractCompatibilityStatus Create(string value) => ContractCompatibilityStatus.Parse(value);

    protected override string GetValue(ContractCompatibilityStatus value) => value.Value;
}

internal sealed class OnboardingDiagnosticCheckJsonConverter :
    ConversationStringValueJsonConverter<OnboardingDiagnosticCheck>
{
    protected override OnboardingDiagnosticCheck Create(string value) => OnboardingDiagnosticCheck.Parse(value);

    protected override string GetValue(OnboardingDiagnosticCheck value) => value.Value;
}

internal sealed class OnboardingDiagnosticStatusJsonConverter :
    ConversationStringValueJsonConverter<OnboardingDiagnosticStatus>
{
    protected override OnboardingDiagnosticStatus Create(string value) => OnboardingDiagnosticStatus.Parse(value);

    protected override string GetValue(OnboardingDiagnosticStatus value) => value.Value;
}
