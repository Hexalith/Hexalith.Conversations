// <copyright file="ConversationPublicationServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.Publication;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Publication;

/// <summary>
/// Verifies the Story 3.5 telemetry-emitting publication service wrapper records bounded failure signals only on
/// rejection, stays silent on success, and tolerates a missing telemetry sink without throwing.
/// </summary>
public sealed class ConversationPublicationServiceTest
{
    /// <summary>
    /// Ensures a successful publication mapping returns the published event and records no failure signal.
    /// </summary>
    [Fact]
    public void SuccessfulPublicationShouldNotRecordFailureSignal()
    {
        RecordingTelemetry telemetry = new();
        ConversationPublicationService service = new(telemetry);

        ConversationPublicationResult result = service.TryMap(
            PersistedConversationEvent.Success(PublicationSamples.Tenant, ParticipantDomainEvent()));

        result.IsPublished.ShouldBeTrue(result.Diagnostic?.Code.Value);
        telemetry.Failures.ShouldBeEmpty();
    }

    /// <summary>
    /// Ensures a tenant mismatch rejection records exactly one bounded tenant-violation failure signal with the
    /// caller-supplied safe correlation identifier.
    /// </summary>
    [Fact]
    public void TenantMismatchRejectionShouldRecordTenantViolationFailure()
    {
        RecordingTelemetry telemetry = new();
        ConversationPublicationService service = new(telemetry);

        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            new TenantId("other-tenant"),
            ParticipantDomainEvent());

        ConversationPublicationResult result = service.TryMap(persisted, correlationId: "correlation-override-001");

        result.IsPublished.ShouldBeFalse();
        telemetry.Failures.Count.ShouldBe(1);
        telemetry.Failures[0].FailureClass.ShouldBe(ConversationPublicationFailureClass.TenantViolation);
        telemetry.Failures[0].CorrelationId.ShouldBe("correlation-override-001");
    }

    /// <summary>
    /// Ensures an unsupported schema rejection records a bounded unsupported-schema failure signal.
    /// </summary>
    [Fact]
    public void UnsupportedSchemaRejectionShouldRecordUnsupportedSchemaFailure()
    {
        RecordingTelemetry telemetry = new();
        ConversationPublicationService service = new(telemetry);

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

        ConversationPublicationResult result = service.TryMap(persisted, correlationId: "correlation-override-002");

        result.IsPublished.ShouldBeFalse();
        telemetry.Failures.Count.ShouldBe(1);
        telemetry.Failures[0].FailureClass.ShouldBe(ConversationPublicationFailureClass.UnsupportedSchema);
    }

    /// <summary>
    /// Ensures an idempotency-conflict outcome records a replay-required failure signal.
    /// </summary>
    [Fact]
    public void IdempotencyConflictOutcomeShouldRecordReplayRequiredFailure()
    {
        RecordingTelemetry telemetry = new();
        ConversationPublicationService service = new(telemetry);

        PersistedConversationEvent persisted = new(
            ConversationPersistenceOutcome.IdempotencyConflict,
            PublicationSamples.Tenant,
            ParticipantDomainEvent());

        ConversationPublicationResult result = service.TryMap(persisted, correlationId: "correlation-override-003");

        result.IsPublished.ShouldBeFalse();
        telemetry.Failures.Count.ShouldBe(1);
        telemetry.Failures[0].FailureClass.ShouldBe(ConversationPublicationFailureClass.ReplayRequired);
    }

    /// <summary>
    /// Ensures the service tolerates a missing telemetry sink and still returns the bounded diagnostic result.
    /// </summary>
    [Fact]
    public void MissingTelemetrySinkShouldNotThrowOnRejection()
    {
        ConversationPublicationService service = new();

        PersistedConversationEvent persisted = PersistedConversationEvent.Success(
            new TenantId("other-tenant"),
            ParticipantDomainEvent());

        ConversationPublicationResult result = service.TryMap(persisted);

        result.IsPublished.ShouldBeFalse();
        result.Diagnostic.ShouldNotBeNull();
    }

    /// <summary>
    /// Ensures the service fails closed against a null persisted candidate before touching telemetry.
    /// </summary>
    [Fact]
    public void NullPersistedCandidateShouldThrow()
    {
        ConversationPublicationService service = new(new RecordingTelemetry());

        Should.Throw<ArgumentNullException>(() => service.TryMap(null!));
    }

    private static ParticipantAddedDomainEvent ParticipantDomainEvent()
        => new(
            PublicationSamples.ParticipantMetadata,
            PublicationSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private sealed record RecordedFailure(ConversationPublicationFailureClass FailureClass, string CorrelationId);

    private sealed class RecordingTelemetry : IConversationProjectionTelemetry
    {
        public List<RecordedFailure> Failures { get; } = [];

        public void RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass freshnessClass,
            ConversationProjectionLagClass lagClass,
            string correlationId)
        {
            // Not exercised by the publication service path.
        }

        public void RecordProjectionRebuildProgress(
            ConversationProjectionFreshnessClass rebuildClass,
            string correlationId)
        {
            // Not exercised by the publication service path.
        }

        public void RecordPublicationFailure(
            ConversationPublicationFailureClass failureClass,
            string correlationId)
            => Failures.Add(new RecordedFailure(failureClass, correlationId));
    }
}
