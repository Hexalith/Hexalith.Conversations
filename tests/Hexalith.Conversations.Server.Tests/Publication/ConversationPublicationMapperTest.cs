// <copyright file="ConversationPublicationMapperTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Publication;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Publication;

/// <summary>
/// Verifies the safe publication mapping boundary required by Story 1.10.
/// </summary>
public sealed class ConversationPublicationMapperTest
{
    /// <summary>
    /// Ensures a durable domain event maps to exactly one public Conversations event after persistence.
    /// </summary>
    [Fact]
    public void PublishedDomainEventShouldMapToPublicConversationEvent()
    {
        ConversationCreatedDomainEvent domainEvent = new(
            PublicationSamples.CreatedMetadata,
            PublicationSamples.Business,
            PublicationSamples.Project,
            PublicationSamples.Folder,
            "Case 123",
            "idempotency-001",
            PublicationSamples.ProviderCorrelation);

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, domainEvent));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        ConversationCreated published = result.GetPublishedEvent<ConversationCreated>();
        published.Metadata.EventId.ShouldBe("event-001");
        published.Metadata.DeduplicationKey.ShouldBe("tenant:tenant-001|conv:conversation-001|event-001|1");
        published.Metadata.CorrelationId.ShouldBe("correlation-001");
        published.Metadata.CausationId.ShouldBe("causation-001");
    }

    /// <summary>
    /// Ensures rejected commands and other non-successful outcomes never produce successful publication events.
    /// </summary>
    [Theory]
    [InlineData(ConversationPersistenceOutcome.RejectedCommand)]
    [InlineData(ConversationPersistenceOutcome.NoOpIdempotentReplay)]
    [InlineData(ConversationPersistenceOutcome.IdempotencyConflict)]
    [InlineData(ConversationPersistenceOutcome.FailedPersistence)]
    [InlineData(ConversationPersistenceOutcome.FailedTenantCheck)]
    [InlineData(ConversationPersistenceOutcome.FailedParticipantValidation)]
    public void NonSuccessfulPersistenceOutcomesShouldNotPublish(ConversationPersistenceOutcome outcome)
    {
        PersistedConversationEvent persisted = new(
            outcome,
            PublicationSamples.Tenant,
            new ParticipantAddedDomainEvent(
                PublicationSamples.ParticipantMetadata,
                PublicationSamples.Participant,
                ParticipantType.Human,
                ParticipantRole.Member));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
    }

    /// <summary>
    /// Ensures tenant mismatches fail before any publisher can receive a public event.
    /// </summary>
    [Fact]
    public void TenantMismatchShouldReturnBoundedDiagnosticWithoutPublication()
    {
        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            new TenantId("other-tenant"),
            new ParticipantAddedDomainEvent(
                PublicationSamples.ParticipantMetadata,
                PublicationSamples.Participant,
                ParticipantType.Human,
                ParticipantRole.Member));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.Code.ShouldBe(ConversationErrorCode.TenantContextMismatch);
        result.Diagnostic.TenantId.ShouldBe(PublicationSamples.Tenant);
        result.Diagnostic.ConversationId.ShouldBe(PublicationSamples.Conversation);
        JsonSerializer.Serialize(result.Diagnostic, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ShouldNotContain("other-tenant", Case.Insensitive);
    }

    /// <summary>
    /// Ensures unsupported versions fail closed with safe version diagnostics only.
    /// </summary>
    [Fact]
    public void UnsupportedVersionShouldReturnBoundedDiagnosticWithoutPublication()
    {
        ConversationEventMetadata unsupported = PublicationSamples.ParticipantMetadata with
        {
            SchemaVersion = new SchemaVersion(2),
        };

        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            PublicationSamples.Tenant,
            new ParticipantAddedDomainEvent(
                unsupported,
                PublicationSamples.Participant,
                ParticipantType.Human,
                ParticipantRole.Member));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Diagnostic.SchemaVersion.ShouldBe(new SchemaVersion(2));
        result.Diagnostic.CorrelationId.ShouldBe("correlation-001");
    }

    /// <summary>
    /// Plants forbidden sentinel values in payload-adjacent fields (ParticipantPartyId, ActorPartyId)
    /// and proves the publication diagnostic JSON exposes only the bounded identifier set declared
    /// by <see cref="ConversationPublicationDiagnostic"/> — never the planted payload sentinels or
    /// substrate vocabulary that lives outside the diagnostic surface.
    /// </summary>
    [Fact]
    public void DiagnosticShouldNotEchoForbiddenSentinelsFromAdjacentFields()
    {
        const string sentinelPartyDisplay = "party@example.com|prompt=leaked|providerPayload=leaked";
        const string sentinelActor = "actor-bearer=leaked|envelope=EventStoreEnvelope|stream=tenant.events";

        ConversationEventMetadata unsupported = PublicationSamples.ParticipantMetadata with
        {
            SchemaVersion = new SchemaVersion(2),
            ActorPartyId = new PartyId(sentinelActor),
        };

        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            PublicationSamples.Tenant,
            new ParticipantAddedDomainEvent(
                unsupported,
                new PartyId(sentinelPartyDisplay),
                ParticipantType.Human,
                ParticipantRole.Member));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);

        string diagnosticJson = JsonSerializer.Serialize(result.Diagnostic, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        diagnosticJson.ShouldNotContain("party@example.com", Case.Insensitive);
        diagnosticJson.ShouldNotContain("prompt=leaked", Case.Insensitive);
        diagnosticJson.ShouldNotContain("providerPayload", Case.Insensitive);
        diagnosticJson.ShouldNotContain("actor-bearer", Case.Insensitive);
        diagnosticJson.ShouldNotContain("EventStoreEnvelope", Case.Insensitive);
        diagnosticJson.ShouldNotContain("stream=", Case.Insensitive);
        diagnosticJson.ShouldNotContain("Dapr", Case.Insensitive);
        diagnosticJson.ShouldNotContain("SignalR", Case.Insensitive);
    }

    /// <summary>
    /// Ensures retry/replay mapping preserves the persisted public identity and safe metadata.
    /// </summary>
    [Fact]
    public void RetryShouldPreserveStablePublishedIdentity()
    {
        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            PublicationSamples.Tenant,
            new ParticipantAddedDomainEvent(
                PublicationSamples.ParticipantMetadata,
                PublicationSamples.Participant,
                ParticipantType.Human,
                ParticipantRole.Member));

        ConversationPublicationResult first = ConversationPublicationMapper.TryMap(persisted);
        ConversationPublicationResult second = ConversationPublicationMapper.TryMap(persisted);

        first.GetPublishedEvent<ParticipantAdded>().Metadata.EventId.ShouldBe(second.GetPublishedEvent<ParticipantAdded>().Metadata.EventId);
        first.GetPublishedEvent<ParticipantAdded>().Metadata.DeduplicationKey.ShouldBe(second.GetPublishedEvent<ParticipantAdded>().Metadata.DeduplicationKey);
    }

    [Fact]
    public void ProjectChangedDomainEventShouldMapToPublicProjectChangedEvent()
    {
        ProjectId reassigned = new("project-002");
        ConversationProjectChangedDomainEvent domainEvent = new(
            PublicationSamples.ProjectMetadata,
            PublicationSamples.Project,
            reassigned);

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, domainEvent));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        ConversationProjectChanged published = result.GetPublishedEvent<ConversationProjectChanged>();
        published.Metadata.EventType.ShouldBe(ConversationEventType.ConversationProjectChanged);
        published.PreviousProjectId.ShouldBe(PublicationSamples.Project);
        published.CurrentProjectId.ShouldBe(reassigned);
    }

    /// <summary>
    /// A durable sensitivity domain event maps to one public content-safe event.
    /// </summary>
    [Fact]
    public void SensitivityDomainEventShouldMapToPublicSensitivityEvent()
    {
        ConversationContentMarkedSensitiveDomainEvent domainEvent = new(
            PublicationSamples.SensitivityMetadata,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: PublicationSamples.Message),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "sensitivity-policy-standard",
                PublicationSamples.SensitivityMetadata.CommittedAt));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, domainEvent));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        ConversationContentMarkedSensitive published = result.GetPublishedEvent<ConversationContentMarkedSensitive>();
        published.Target.MessageId.ShouldBe(PublicationSamples.Message);
        published.PolicyReference.ShouldBe("sensitivity-policy-standard");
    }

    /// <summary>
    /// A durable redaction domain event maps to one public content-safe event.
    /// </summary>
    [Fact]
    public void RedactionDomainEventShouldMapToPublicRedactionEvent()
    {
        MessageContentRedactedDomainEvent domainEvent = new(
            PublicationSamples.RedactionMetadata,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: PublicationSamples.Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "redaction-policy-standard",
                PublicationSamples.RedactionMetadata.CommittedAt));

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, domainEvent));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        MessageContentRedacted published = result.GetPublishedEvent<MessageContentRedacted>();
        published.Metadata.EventType.ShouldBe(ConversationEventType.MessageContentRedacted);
        published.Target.MessageId.ShouldBe(PublicationSamples.Message);
        published.Category.ShouldBe(RedactionCategory.ContentSuppression);
        published.PolicyReference.ShouldBe("redaction-policy-standard");
        published.ToString().ShouldNotContain("customer-request", Case.Insensitive);
    }

    /// <summary>
    /// Ensures the mapper fails closed against a null persisted candidate rather than publishing.
    /// </summary>
    [Fact]
    public void NullPersistedCandidateShouldThrow()
        => Should.Throw<ArgumentNullException>(() => ConversationPublicationMapper.TryMap(null!));

    /// <summary>
    /// Ensures a successful outcome carrying a payload the mapper does not recognize fails closed with a bounded
    /// command-validation diagnostic and never publishes an unmapped object.
    /// </summary>
    [Fact]
    public void UnsupportedPayloadShouldReturnBoundedDiagnosticWithoutPublication()
    {
        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            PublicationSamples.Tenant,
            "unsupported-payload");

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
    }

    /// <summary>
    /// Ensures a public event whose declared metadata event type contradicts its payload type fails closed with a
    /// bounded command-validation diagnostic instead of publishing a mislabeled event.
    /// </summary>
    [Fact]
    public void EventTypeMetadataMismatchShouldReturnBoundedDiagnosticWithoutPublication()
    {
        ParticipantAdded mismatched = new(
            PublicationSamples.ParticipantMetadata with
            {
                EventType = ConversationEventType.MessageAppended,
            },
            PublicationSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, mismatched));

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
        result.Diagnostic.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        result.Diagnostic.EventType.ShouldBe(ConversationEventType.MessageAppended);
        result.Diagnostic.TenantId.ShouldBe(PublicationSamples.Tenant);
    }

    /// <summary>
    /// Ensures a closed conversation maps to a bounded lifecycle-changed public event (Open -> Closed) with the
    /// rewritten lifecycle event type and the original safe reason code preserved.
    /// </summary>
    [Fact]
    public void ConversationClosedShouldMapToLifecycleChangedOpenToClosed()
    {
        ConversationClosed closed = new(
            PublicationSamples.CreatedMetadata with
            {
                EventType = ConversationEventType.ConversationClosed,
                EventId = "event-closed-001",
            },
            "case-resolved");

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, closed));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        ConversationLifecycleChanged published = result.GetPublishedEvent<ConversationLifecycleChanged>();
        published.Metadata.EventType.ShouldBe(ConversationEventType.ConversationLifecycleChanged);
        published.PreviousState.ShouldBe(ConversationLifecycleStatus.Open);
        published.CurrentState.ShouldBe(ConversationLifecycleStatus.Closed);
        published.ReasonCode.ShouldBe("case-resolved");
    }

    /// <summary>
    /// Ensures an archived conversation maps to a bounded lifecycle-changed public event (Closed -> Archived).
    /// </summary>
    [Fact]
    public void ConversationArchivedShouldMapToLifecycleChangedClosedToArchived()
    {
        ConversationArchived archived = new(
            PublicationSamples.CreatedMetadata with
            {
                EventType = ConversationEventType.ConversationArchived,
                EventId = "event-archived-001",
            },
            "retention-window-elapsed");

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, archived));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        ConversationLifecycleChanged published = result.GetPublishedEvent<ConversationLifecycleChanged>();
        published.Metadata.EventType.ShouldBe(ConversationEventType.ConversationLifecycleChanged);
        published.PreviousState.ShouldBe(ConversationLifecycleStatus.Closed);
        published.CurrentState.ShouldBe(ConversationLifecycleStatus.Archived);
        published.ReasonCode.ShouldBe("retention-window-elapsed");
    }
}
