// <copyright file="ConversationQueryHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Queries;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies tenant-safe conversation query handling.
/// </summary>
public sealed class ConversationQueryHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly BusinessReference Business = new("crm", "case-123");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    // One ephemeral Data Protection provider for the whole fixture: building two codecs from it with
    // different purposes gives deterministic cross-purpose isolation (the "different key" fail-closed case)
    // without depending on on-disk key persistence.
    private static readonly IDataProtectionProvider s_dataProtection = new EphemeralDataProtectionProvider();

    /// <summary>
    /// Tenant denial returns the same hidden shape as a missing record and never reads projection storage.
    /// </summary>
    [Fact]
    public async Task DetailDeniedTenantShouldNotReadProjection()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.MissingMember));
        FakeProjectionReadStore store = new();
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(0);
        access.Calls.ShouldBe(1);
    }

    /// <summary>
    /// Projection poison data is denied instead of trusting tenant ids returned by storage.
    /// </summary>
    [Fact]
    public async Task DetailShouldRejectProjectionTenantMismatch()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(OtherTenant, Conversation),
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// Nonexistent projection returns the same hidden shape as an unauthorized caller.
    /// </summary>
    [Fact]
    public async Task DetailNonexistentConversationShouldReturnHiddenSameAsUnauthorized()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new() { Models = null };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Forbidden);
        result.Details.ShouldBeNull();
        result.SafeNextAction.ShouldBe("The requested conversation is not available.");
    }

    /// <summary>
    /// Authorized detail reads hydrate stable references after projection data is accepted.
    /// </summary>
    [Fact]
    public async Task DetailShouldHydrateAfterAuthorizedProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new() { Models = ProjectedModels(Tenant, Conversation) };
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Actor] = new ReferenceHydrationResult<PartyId>(Actor, ReferenceHydrationStatus.Current, "Actor", "actor-token", "Available"),
                [Participant] = new ReferenceHydrationResult<PartyId>(Participant, ReferenceHydrationStatus.Current, "Participant", "participant-token", "Available"),
            },
            ProjectResults =
            {
                [Project] = new ReferenceHydrationResult<ProjectId>(Project, ReferenceHydrationStatus.Unavailable),
            },
            FolderResults =
            {
                [Folder] = new ReferenceHydrationResult<FolderId>(Folder, ReferenceHydrationStatus.Redacted),
            },
        };
        ConversationQueryHandler handler = CreateHandler(access, store, hydration: new ConversationReadHydrationService(directory));

        ConversationDetailResult result = await handler.GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        result.Details.PartyHydration.Count.ShouldBe(2);
        result.Details.PartyHydration.Single(x => x.PartyId == Participant).SafeLabel.ShouldBe("Participant");
        result.Details.TrustPosture.TenantId.ShouldBe(Tenant);
        result.Details.TrustPosture.ConversationId.ShouldBe(Conversation);
        result.Details.TrustPosture.TemporalCursor.ShouldBe("pos:0000000001");
        result.Details.TrustPosture.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Current);
        result.Details.TrustPosture.CommandEligibility.ShouldAllBe(item => item.AvailabilityState == ProjectionTrustState.Unavailable);
        result.Details.TrustPosture.CommandEligibility.ShouldAllBe(
            item => item.ActionClassification == ConversationCommandAvailabilityV1.ReadOnlyActionClassification);
        result.Details.TrustPosture.CommandEligibility.ShouldAllBe(item => item.RequiresFreshServerRecheck);
        result.Details.EvidenceEntries.ShouldContain(entry => entry.Kind == "Message" && entry.MessageId == new MessageId("message-001"));
        result.Details.ProjectHydration.ShouldNotBeNull();
        result.Details.ProjectHydration.HydrationState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Details.FolderHydration.ShouldNotBeNull();
        result.Details.FolderHydration.HydrationState.ShouldBe(ProjectionTrustState.Redacted);
        directory.PartyBatchCalls.ShouldBe(1);
        directory.LastContext.ShouldNotBeNull();
        directory.LastContext.TenantId.ShouldBe(Tenant);
        directory.LastContext.CallerPrincipalId.ShouldBe("caller-001");
        directory.LastContext.CorrelationId.ShouldBe("correlation-001");

        static GetConversationQuery GetQuery()
            => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation);
    }

    /// <summary>
    /// Projection-owned missing citation and partial evidence states remain explicit through the query boundary.
    /// </summary>
    [Fact]
    public async Task DetailShouldPreserveProjectionOwnedDegradedTrustMetadata()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithDegradedTrust(Tenant, Conversation),
        };
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Actor] = new ReferenceHydrationResult<PartyId>(Actor, ReferenceHydrationStatus.Current, "Actor", "actor-token", "Available"),
                [Participant] = new ReferenceHydrationResult<PartyId>(Participant, ReferenceHydrationStatus.Current, "Participant", "participant-token", "Available"),
            },
        };
        ConversationQueryHandler handler = CreateHandler(access, store, hydration: new ConversationReadHydrationService(directory));

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.TrustPosture.EvidenceCompletenessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Details.TrustPosture.CitationAvailability.ShouldBe(ConversationCitationAvailability.Unavailable);
        result.Details.TrustPosture.AuditReadiness.ShouldBe(ConversationAuditReadinessState.Incomplete);
        result.Details.TrustPosture.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Current);
        ConversationEvidenceEntryV1 entry = result.Details.EvidenceEntries.Single(e => e.Kind == "Message");
        entry.CitationAvailability.ShouldBe(ConversationCitationAvailability.Unavailable);
        entry.DegradedState.ShouldBe(ProjectionTrustState.Unavailable);
        entry.VisibleText.ShouldBe("Partial evidence available.");
    }

    /// <summary>
    /// Projection-owned governance command metadata is preserved as unavailable advisory metadata by the query boundary.
    /// </summary>
    [Fact]
    public async Task DetailShouldPreserveProjectionOwnedGovernanceCommandMetadata()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                DetailWithCommandMetadata(Tenant, Conversation)),
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        ConversationCommandAvailabilityV1 command = result.Details.TrustPosture.CommandEligibility.Single();
        command.ActionName.ShouldBe("set-retention-policy");
        command.ActionClassification.ShouldBe(ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);
        command.RequiresFreshServerRecheck.ShouldBeTrue();
        command.AvailabilityState.ShouldBe(ProjectionTrustState.Unavailable);
        command.RequiredPermission.ShouldBe("conversations.governance");
        command.FreshnessRequirementState.ShouldBe(ProjectionTrustState.Current);
        command.AuditRequirement.ShouldBe(ConversationAuditReadinessState.Ready);
        store.DetailReads.ShouldBe(1);
        access.Calls.ShouldBe(1);
    }

    /// <summary>
    /// Available projection-owned command metadata is still advisory and keeps its mandatory server recheck flag.
    /// </summary>
    [Fact]
    public async Task DetailShouldPreserveAvailableCommandMetadataOnlyAsAdvisoryRecheckMetadata()
    {
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                DetailWithCommandMetadata(Tenant, Conversation, commandAvailability: ProjectionTrustState.Current)),
        };
        ConversationQueryHandler handler = CreateHandler(AllowedAccess(), store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        ConversationCommandAvailabilityV1 command = result.Details.TrustPosture.CommandEligibility.Single();
        command.AvailabilityState.ShouldBe(ProjectionTrustState.Current);
        command.ActionClassification.ShouldBe(ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);
        command.RequiresFreshServerRecheck.ShouldBeTrue();
        command.PreconditionState.ShouldBe(ProjectionTrustState.Current);
        command.FreshnessRequirementState.ShouldBe(ProjectionTrustState.Current);
        command.AuditRequirement.ShouldBe(ConversationAuditReadinessState.Ready);
        command.RequiredPermission.ShouldBe("conversations.governance");
    }

    /// <summary>
    /// Stale detail projections close protected detail state instead of returning stale command/citation metadata.
    /// </summary>
    [Fact]
    public async Task DetailStaleProjectionShouldClearProtectedDetailState()
    {
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                Summary(
                    Tenant,
                    Conversation,
                    Business,
                    Project,
                    Folder,
                    Participant,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
                DetailWithCommandMetadata(
                    Tenant,
                    Conversation,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded)),
        };
        ConversationQueryHandler handler = CreateHandler(AllowedAccess(), store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.Details.ShouldBeNull();
        result.SafeNextAction.ShouldBe("The requested conversation is not available.");
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// The audit-record handler entry point uses the governed read boundary and returns citeable evidence.
    /// </summary>
    [Fact]
    public async Task AuditRecordShouldReadThroughGovernedQueryEntryPoint()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationAuditRecordResult result = await handler.GetAuditRecordAsync(
            new GetConversationAuditRecordQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "audit-evidence-001",
                AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.Allowed);
        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        result.Details.ActorPartyId.ShouldBe(Actor);
        result.Details.PolicyTreatment.ExportEligible.ShouldBeFalse();
        access.Calls.ShouldBe(1);
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// Citation copy is built from the governed projection after the current authorization/freshness boundary.
    /// </summary>
    [Fact]
    public async Task CitationShouldReadSafeDtoThroughGovernedQueryEntryPoint()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                DetailWithCitation(Tenant, Conversation)),
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationCitationResult result = await handler.GetCitationAsync(
            new GetConversationCitationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "message:message-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Citation.ShouldNotBeNull();
        result.Citation.EvidenceEntryId.ShouldBe("message:message-001");
        result.Citation.SafeCopiedText.ShouldContain("message:message-001");
        result.Citation.SafeCopiedText.ShouldContain("pos:0000000001");
        result.Citation.SafeCopiedText.ShouldNotContain("Hello from the adopter.", Case.Insensitive);
        result.Citation.AuditEvidence!.Handle.Value.ShouldBe("audit-evidence-001");
        access.Calls.ShouldBe(1);
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// Missing audit handles downgrade audit-linked citation targets instead of returning trusted copied text.
    /// </summary>
    [Fact]
    public async Task CitationMissingAuditHandleShouldReturnIncompleteSafeDto()
    {
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                DetailWithCitation(Tenant, Conversation, includeAuditEvidence: false)),
        };
        ConversationQueryHandler handler = CreateHandler(AllowedAccess(), store);

        ConversationCitationResult result = await handler.GetCitationAsync(
            new GetConversationCitationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "message:message-001"),
            TestContext.Current.CancellationToken);

        result.Citation.ShouldNotBeNull();
        result.Citation.CitationAvailability.ShouldBe(ConversationCitationAvailability.Incomplete);
        result.Citation.AuditReadiness.ShouldBe(ConversationAuditReadinessState.Incomplete);
        result.Citation.AuditEvidence.ShouldBeNull();
        result.Citation.SafeCopiedText.ShouldBe("Citation is incomplete.");
    }

    /// <summary>
    /// Redacted citation targets cite only the canonical placeholder and redaction attribution metadata.
    /// </summary>
    [Fact]
    public async Task CitationRedactedTargetShouldUsePlaceholderAndAttributionWithoutOriginalText()
    {
        ConversationQueryHandler handler = CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = new ConversationProjectedReadModels(
                    Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                    DetailWithRedactedCitation(Tenant, Conversation)),
            });

        ConversationCitationResult result = await handler.GetCitationAsync(
            new GetConversationCitationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "message:message-001"),
            TestContext.Current.CancellationToken);

        result.Citation.ShouldNotBeNull();
        result.Citation.TrustState.ShouldBe(ProjectionTrustState.Redacted);
        result.Citation.SafeCopiedText.ShouldContain("redaction=[redacted]");
        result.Citation.SafeCopiedText.ShouldContain("redactionPolicy=redaction-policy-standard");
        result.Citation.SafeCopiedText.ShouldContain("redactionReason=customer-request");
        result.Citation.SafeCopiedText.ShouldNotContain("secret customer content", Case.Insensitive);
        result.Citation.SafeLabel.ShouldBe("Redacted message evidence citation");
        result.Citation.SafeAccessibilityLabel.ShouldBe("Copy redacted message evidence citation");
    }

    /// <summary>
    /// Citation denial, missing targets, and stale projections stay content-safe.
    /// </summary>
    [Fact]
    public async Task CitationDeniedMissingOrStaleTargetShouldFailClosed()
    {
        ConversationCitationResult denied = await CreateHandler(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Read,
                Tenant,
                "caller-001",
                ConversationTenantAccessDenialReason.MissingMember)),
            new FakeProjectionReadStore())
            .GetCitationAsync(CitationQuery("message:message-001"), TestContext.Current.CancellationToken);

        ConversationCitationResult missing = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = new ConversationProjectedReadModels(
                    Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                    DetailWithCitation(Tenant, Conversation)),
            })
            .GetCitationAsync(CitationQuery("message:missing"), TestContext.Current.CancellationToken);

        ConversationCitationResult stale = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = new ConversationProjectedReadModels(
                    Summary(Tenant, Conversation, Business, Project, Folder, Participant, freshnessState: ProjectionTrustState.Stale, reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
                    DetailWithCitation(Tenant, Conversation, freshnessState: ProjectionTrustState.Stale, reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded)),
            })
            .GetCitationAsync(CitationQuery("message:message-001"), TestContext.Current.CancellationToken);

        ConversationCitationResult crossTenant = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = new ConversationProjectedReadModels(
                    Summary(OtherTenant, Conversation, Business, Project, Folder, Participant),
                    DetailWithCitation(OtherTenant, Conversation)),
            })
            .GetCitationAsync(CitationQuery("message:message-001"), TestContext.Current.CancellationToken);

        denied.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        missing.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        stale.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        crossTenant.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        denied.Citation.ShouldBeNull();
        missing.Citation.ShouldBeNull();
        stale.Citation.ShouldBeNull();
        crossTenant.Citation.ShouldBeNull();

        static GetConversationCitationQuery CitationQuery(string evidenceEntryId)
            => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation, evidenceEntryId);
    }

    /// <summary>
    /// Citation links with source positions beyond the current projection cursor are treated as incomplete evidence.
    /// </summary>
    [Fact]
    public async Task CitationWithFutureSourcePositionShouldNotReturnTrustedTemporalCursor()
    {
        ConversationQueryHandler handler = CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = new ConversationProjectedReadModels(
                    Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                    DetailWithCitation(Tenant, Conversation, safeSourcePosition: 99)),
            });

        ConversationCitationResult result = await handler.GetCitationAsync(
            new GetConversationCitationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "message:message-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);
        result.Citation.ShouldBeNull();
    }

    /// <summary>
    /// The privileged-action review entry point delegates to the governed review boundary.
    /// </summary>
    [Fact]
    public async Task PrivilegedJustificationShouldReadThroughGovernedQueryEntryPoint()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "caller-001"));
        FakePrivilegedReviewSource source = new()
        {
            Details = PrivilegedDetails(),
        };
        ConversationPrivilegedJustificationReviewService reviewService = new(access, source);
        ConversationQueryHandler handler = CreateHandler(
            access,
            new FakeProjectionReadStore(),
            privilegedReview: reviewService);

        PrivilegedOperationalJustificationResult result = await handler.GetPrivilegedOperationalJustificationAsync(
            new GetPrivilegedOperationalJustificationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-privileged-001");
        result.Details.OperationClass.ShouldBe(PrivilegedOperationalActionClass.Read);
        access.Calls.ShouldBe(1);
        source.Reads.ShouldBe(1);
    }

    /// <summary>
    /// Unauthorized, nonexistent, cross-tenant, and missing-projection details all return the same external shape.
    /// </summary>
    [Fact]
    public async Task DetailDenialPathsShouldShareSameShape()
    {
        ConversationDetailResult unauthorized = await CreateHandler(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Read,
                Tenant,
                "caller-001",
                ConversationTenantAccessDenialReason.MissingMember)),
            new FakeProjectionReadStore())
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        ConversationDetailResult nonexistent = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = null })
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        ConversationDetailResult crossTenant = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = ProjectedModels(OtherTenant, Conversation) })
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        unauthorized.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        nonexistent.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        crossTenant.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);

        unauthorized.ReasonCode.ShouldBe(nonexistent.ReasonCode);
        unauthorized.ReasonCode.ShouldBe(crossTenant.ReasonCode);
        unauthorized.SafeNextAction.ShouldBe(nonexistent.SafeNextAction);
        unauthorized.SafeNextAction.ShouldBe(crossTenant.SafeNextAction);
        unauthorized.Details.ShouldBeNull();
        nonexistent.Details.ShouldBeNull();
        crossTenant.Details.ShouldBeNull();

        static GetConversationQuery GetQuery()
            => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation);
    }

    /// <summary>
    /// List authorization occurs before any filter evaluation or projection read.
    /// </summary>
    [Fact]
    public async Task ListDeniedTenantShouldNotReadOrFilterProjection()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.TenantDisabled));
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(BusinessReference: Business)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Projection list storage failures are coarsened to unavailable without leaking infrastructure details.
    /// </summary>
    [Fact]
    public async Task ListProjectionStoreFailureShouldReturnUnavailable()
    {
        FakeProjectionReadStore store = new()
        {
            ListException = new UnauthorizedAccessException("raw projection backend path"),
        };
        ConversationQueryHandler handler = CreateHandler(AllowedAccess(), store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Conversations.ShouldBeEmpty();
        result.SafeNextAction.ShouldNotContain("raw", Case.Insensitive);
        store.ListReads.ShouldBe(1);
    }

    /// <summary>
    /// List filters are exact, tenant-scoped, and do not trust mixed-tenant projection rows.
    /// </summary>
    [Fact]
    public async Task ListShouldApplyTenantScopeBeforeFiltersAndPagination()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(OtherTenant, new ConversationId("conversation-poison"), Business, Project, Folder, Participant),
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conversation-folder-miss"), Business, Project, new FolderId("folder-other"), Participant),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(
                    Business,
                    Project,
                    Folder,
                    "Open",
                    ParticipantPartyId: Participant),
                new ConversationPageRequest(10)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.ShouldBe(Conversation);
        result.Page.ReturnedCount.ShouldBe(1);
    }

    /// <summary>
    /// Mixed-generation rows from the projection store surface as Rebuilding instead of leaking inconsistent rows.
    /// </summary>
    [Fact]
    public async Task ListShouldRejectMixedGenerationCandidates()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-a"), Business, Project, Folder, Participant, cursor: "pos:1"),
                Summary(Tenant, new ConversationId("conv-b"), Business, Project, Folder, Participant, cursor: "pos:2"),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
        result.Conversations.ShouldBeEmpty();
    }

    /// <summary>
    /// A page combining Current and Stale rows reports the worst-case freshness, not the first row.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldAggregateWorstCaseAcrossPage()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current"), Business, Project, Folder, Participant),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Count.ShouldBe(2);
    }

    /// <summary>
    /// A non-current accessible match beyond the returned page still downgrades list freshness.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldAggregateWorstCaseAcrossAllAccessibleMatchesBeforePaging()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current"), Business, Project, Folder, Participant),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(1)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Count.ShouldBe(1);
        result.Page.ContinuationCursor.ShouldNotBeNull();
    }

    /// <summary>
    /// A non-current accessible match beyond the continuation lookahead still downgrades list freshness.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldIncludeAccessibleMatchesBeyondLookahead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current-1"), Business, Project, Folder, Participant, lastAppliedAt: Now.AddMinutes(3)),
                Summary(Tenant, new ConversationId("conv-current-2"), Business, Project, Folder, Participant, lastAppliedAt: Now.AddMinutes(2)),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    lastAppliedAt: Now.AddMinutes(1),
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(1)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Single().ConversationId.Value.ShouldBe("conv-current-1");
        result.Page.ContinuationCursor.ShouldNotBeNull();
    }

    /// <summary>
    /// Each list filter dimension narrows the result to only its matching row, in isolation.
    /// </summary>
    [Theory]
    [InlineData("business")]
    [InlineData("project")]
    [InlineData("folder")]
    [InlineData("lifecycle")]
    [InlineData("participant")]
    [InlineData("redaction")]
    [InlineData("freshness")]
    [InlineData("audit")]
    [InlineData("verification")]
    public async Task ListShouldFilterByEachDimensionExactly(string dimension)
    {
        FakeTenantAccessService access = AllowedAccess();
        BusinessReference otherBusiness = new("crm", "case-999");
        ProjectId otherProject = new("project-999");
        FolderId otherFolder = new("folder-999");
        PartyId otherParticipant = new("party-other");
        ConversationSearchTrustPreviewV1 matchingTrust = TrustPreview(
            ProjectionTrustState.Redacted,
            ProjectionTrustState.Stale,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified);
        ConversationSearchTrustPreviewV1 nonMatchingTrust = TrustPreview(
            ProjectionTrustState.Current,
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Incomplete,
            ConversationVerificationState.Unverified);

        (IReadOnlyList<ConversationSummaryProjectionV1> rows, ConversationListFilterV1 filter) = dimension switch
        {
            "business" => (
                (IReadOnlyList<ConversationSummaryProjectionV1>)
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), otherBusiness, Project, Folder, Participant),
                ],
                new ConversationListFilterV1(BusinessReference: Business)),
            "project" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, otherProject, Folder, Participant),
                ],
                new ConversationListFilterV1(ProjectId: Project)),
            "folder" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, otherFolder, Participant),
                ],
                new ConversationListFilterV1(FolderId: Folder)),
            "lifecycle" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, lifecycle: "Closed"),
                ],
                new ConversationListFilterV1(LifecycleState: "Open")),
            "participant" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, otherParticipant),
                ],
                new ConversationListFilterV1(ParticipantPartyId: Participant)),
            "redaction" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(RedactionState: ProjectionTrustState.Redacted)),
            "freshness" => (
                [
                    Summary(
                        Tenant,
                        new ConversationId("match"),
                        Business,
                        Project,
                        Folder,
                        Participant,
                        freshnessState: ProjectionTrustState.Stale,
                        reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded,
                        trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(FreshnessState: ProjectionTrustState.Stale)),
            "audit" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(AuditReadiness: ConversationAuditReadinessState.Ready)),
            "verification" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(VerificationState: ConversationVerificationState.Verified)),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension)),
        };

        FakeProjectionReadStore store = new() { Summaries = rows };
        ConversationQueryHandler handler = CreateHandler(access, store);
        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", filter),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.Value.ShouldBe("match");
        result.Conversations[0].SearchTrustPreview.MatchSource.ShouldNotBe(ConversationSearchMatchSource.Unknown);
    }

    [Fact]
    public async Task ProjectFilterShouldUseCurrentProjectedProjectAfterReassignmentAndClear()
    {
        FakeTenantAccessService access = AllowedAccess();
        ProjectId reassigned = new("project-reassigned");
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-old"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-new"), Business, reassigned, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-cleared"), Business, project: null, Folder, Participant),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult oldProject = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(ProjectId: Project)),
            TestContext.Current.CancellationToken);
        ConversationListResult newProject = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(ProjectId: reassigned)),
            TestContext.Current.CancellationToken);

        oldProject.Conversations.Select(summary => summary.ConversationId)
            .ShouldBe([new ConversationId("conv-old")], ignoreOrder: false);
        newProject.Conversations.Select(summary => summary.ConversationId)
            .ShouldBe([new ConversationId("conv-new")], ignoreOrder: false);
        oldProject.Conversations.ShouldAllBe(summary => summary.ConversationId != new ConversationId("conv-cleared"));
        newProject.Conversations.ShouldAllBe(summary => summary.ConversationId != new ConversationId("conv-cleared"));
    }

    /// <summary>
    /// ProjectedAt range and RecentActivityAfter filter out rows outside the window.
    /// </summary>
    [Fact]
    public async Task ListShouldFilterByProjectedAtRangeAndRecentActivity()
    {
        FakeTenantAccessService access = AllowedAccess();
        DateTimeOffset early = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset middle = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset late = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-early"), Business, Project, Folder, Participant, lastAppliedAt: early),
                Summary(Tenant, new ConversationId("conv-middle"), Business, Project, Folder, Participant, lastAppliedAt: middle),
                Summary(Tenant, new ConversationId("conv-late"), Business, Project, Folder, Participant, lastAppliedAt: late),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(
                    ProjectedAtFrom: middle.AddDays(-1),
                    ProjectedAtTo: late.AddDays(-1),
                    RecentActivityAfter: early)),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.Value.ShouldBe("conv-middle");
    }

    /// <summary>
    /// Pagination boundary: the page returns at most PageSize rows even when more accessible rows exist.
    /// </summary>
    [Fact]
    public async Task ListPaginationShouldNotLeakBeyondPageSize()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-a"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-b"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-c"), Business, Project, Folder, Participant),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2)),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(2);
        result.Page.ReturnedCount.ShouldBe(2);
        result.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// List hydration runs only after stable ordering and paging select the visible page.
    /// </summary>
    [Fact]
    public async Task ListHydrationShouldOnlyUseReturnedPageReferences()
    {
        FakeTenantAccessService access = AllowedAccess();
        PartyId first = new("party-first");
        PartyId second = new("party-second");
        PartyId third = new("party-third");
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-first"), Business, Project, Folder, first, lastAppliedAt: Now.AddMinutes(3)),
                Summary(Tenant, new ConversationId("conv-second"), Business, Project, Folder, second, lastAppliedAt: Now.AddMinutes(2)),
                Summary(Tenant, new ConversationId("conv-third"), Business, Project, Folder, third, lastAppliedAt: Now.AddMinutes(1)),
            ],
        };
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [first] = new ReferenceHydrationResult<PartyId>(first, ReferenceHydrationStatus.Current, "Z label", "first-token", "Available"),
                [second] = new ReferenceHydrationResult<PartyId>(second, ReferenceHydrationStatus.Current, "A label", "second-token", "Available"),
                [third] = new ReferenceHydrationResult<PartyId>(third, ReferenceHydrationStatus.Current, "Hidden page label", "third-token", "Available"),
            },
        };
        ConversationQueryHandler handler = CreateHandler(access, store, hydration: new ConversationReadHydrationService(directory));

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2)),
            TestContext.Current.CancellationToken);

        result.Conversations.Select(summary => summary.ConversationId.Value).ShouldBe(["conv-first", "conv-second"]);
        directory.LastPartyIds.ShouldBe([first, second], ignoreOrder: true);
        directory.LastPartyIds.ShouldNotContain(third);
        result.Conversations[0].PartyHydration.Single().SafeLabel.ShouldBe("Z label");
        result.Conversations[1].PartyHydration.Single().SafeLabel.ShouldBe("A label");
        result.Conversations[0].SearchTrustPreview.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// Empty list results use safe copy and expose no facet, autocomplete, or recent-search metadata.
    /// </summary>
    [Fact]
    public async Task NoAccessibleMatchesShouldUseSafeEmptyShape()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, new BusinessReference("crm", "case-999"), Project, Folder, Participant)],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(BusinessReference: Business)),
            TestContext.Current.CancellationToken);

        result.Conversations.ShouldBeEmpty();
        result.Page.ReturnedCount.ShouldBe(0);
        result.SafeNextAction.ShouldBe("No accessible matches.");

        string json = JsonSerializer.Serialize(result);
        json.ShouldNotContain("facet", Case.Insensitive);
        json.ShouldNotContain("autocomplete", Case.Insensitive);
        json.ShouldNotContain("recentSearch", Case.Insensitive);
        json.ShouldNotContain("total", Case.Insensitive);
    }

    /// <summary>
    /// Malformed cursors fail closed before authorization-sensitive reads.
    /// </summary>
    [Fact]
    public async Task MalformedCursorShouldFailClosedWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new();
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, "not-a-valid-cursor")),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Caller-mismatched cursors fail closed after authorization and do not widen reads.
    /// </summary>
    [Fact]
    public async Task CallerMismatchedCursorShouldNotFallBackToFirstPage()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        ConversationQueryHandler handler = CreateHandler(access, store, cursor: codec);
        string cursor = EncodeCursor(
            codec,
            Tenant,
            "different-caller",
            ConversationListFilterV1.Empty,
            offset: 1,
            generationToken: "pos:1:1",
            issuedAt: Now);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, cursor)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();

        // A caller mismatch is now caught at the codec scope boundary, before any projection read.
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Tampered cursor signatures fail closed; the verifier never reads projection storage.
    /// </summary>
    [Fact]
    public async Task TamperedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        string original = EncodeCursor(
            codec, Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        // Corrupt a character so Data Protection Unprotect fails (tamper-or-key-rotation).
        string tampered = TamperCursor(original);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, tampered)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors protected under a different Data Protection purpose/key are rejected.
    /// </summary>
    [Fact]
    public async Task CursorSignedWithDifferentKeyShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec foreignCodec = CreateCodec("Hexalith.Conversations.QueryCursor.foreign");
        string foreign = EncodeCursor(
            foreignCodec, Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: CreateCodec()).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, foreign)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors older than the configured MaxAge fail closed.
    /// </summary>
    [Fact]
    public async Task ExpiredCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        FakeTimeProvider time = new(Now.AddHours(2));
        string aged = EncodeCursor(
            codec, Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec, time: time).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, aged)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Future-dated cursors (clock skew or forged) fail closed via the age lower bound.
    /// </summary>
    [Fact]
    public async Task FutureDatedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        string futureCursor = EncodeCursor(
            codec, Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now.AddHours(1));

        ConversationListResult result = await CreateHandler(access, store, cursor: codec, time: new FakeTimeProvider(Now)).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, futureCursor)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors issued against a different projection generation token fail closed.
    /// </summary>
    [Fact]
    public async Task GenerationMismatchedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        string staleGen = EncodeCursor(
            codec, Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:OLD:0", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, staleGen)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);

        // The generation token rides in the protected position and is re-compared after the projection read,
        // so a superseded-generation cursor fails closed only after the read (zero rows leak regardless).
        store.ListReads.ShouldBe(1);
    }

    /// <summary>
    /// Cursors issued for a different tenant fail closed.
    /// </summary>
    [Fact]
    public async Task TenantMismatchedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        string foreign = EncodeCursor(
            codec, OtherTenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, foreign)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);

        // A tenant mismatch is caught at the codec scope boundary, before any projection read.
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors issued under a different filter set fail closed. The filter fingerprint is one of the four
    /// scope bindings AC-2 pins (tenant / caller / filter / generation); this covers the filter binding the
    /// tenant- and caller-mismatch cases do not.
    /// </summary>
    [Fact]
    public async Task FilterMismatchedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();

        // Mint the cursor under a project-scoped filter, then present it against the empty filter: the filter
        // fingerprint folded into the scope differs, so TryDecode returns wrong-scope before any projection read.
        string foreignFilter = EncodeCursor(
            codec,
            Tenant,
            "caller-001",
            new ConversationListFilterV1(ProjectId: Project),
            offset: 1,
            generationToken: "pos:1:1",
            issuedAt: Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Filter: ConversationListFilterV1.Empty,
                Page: new ConversationPageRequest(10, foreignFilter)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();

        // A filter mismatch is caught at the codec scope boundary, before any projection read.
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors with offsets above the configured MaxOffset fail closed.
    /// </summary>
    [Fact]
    public async Task ExcessiveOffsetCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        IQueryCursorCodec codec = CreateCodec();
        // Forge a decodable cursor whose offset exceeds the configured MaxOffset; the handler re-applies the
        // offset bound after a successful decode (the codec itself has no offset ceiling).
        string oversize = EncodeCursor(
            codec, Tenant, "caller-001", ConversationListFilterV1.Empty, offset: 999_999, generationToken: "pos:1:1", issuedAt: Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: codec, maxOffset: 10).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, oversize)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// A continuation cursor issued by the SDK codec round-trips: presented with the same tenant, caller,
    /// filters, and projection generation it resumes at the next page rather than failing closed — proving the
    /// adopted codec preserves cursor identity (AC-4).
    /// </summary>
    [Fact]
    public async Task IssuedContinuationCursorShouldRoundTripToNextPage()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-1"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-2"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-3"), Business, Project, Folder, Participant),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult firstPage = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2)),
            TestContext.Current.CancellationToken);

        firstPage.Page.ReturnedCount.ShouldBe(2);
        firstPage.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        ConversationListResult secondPage = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2, firstPage.Page.ContinuationCursor)),
            TestContext.Current.CancellationToken);

        secondPage.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        secondPage.Page.ReturnedCount.ShouldBe(1);
        secondPage.Page.ContinuationCursor.ShouldBeNull();

        // The two pages partition the accessible set with no overlap and no skipped rows.
        IEnumerable<string> ids = firstPage.Conversations
            .Concat(secondPage.Conversations)
            .Select(summary => summary.ConversationId.Value);
        ids.ShouldBe(["conv-1", "conv-2", "conv-3"], ignoreOrder: true);
    }

    /// <summary>
    /// Public query contracts do not expose any field that would let a caller supply a provider session id.
    /// </summary>
    [Fact]
    public void GetAndListContractsShouldNotExposeProviderSessionField()
    {
        Type detailQuery = typeof(GetConversationQuery);
        Type listQuery = typeof(ListConversationsQuery);
        Type filter = typeof(ConversationListFilterV1);

        foreach (Type type in new[] { detailQuery, listQuery, filter })
        {
            foreach (System.Reflection.PropertyInfo property in type.GetProperties())
            {
                property.Name.ShouldNotContain("Session", Case.Insensitive);
                property.Name.ShouldNotContain("Provider", Case.Insensitive);
            }
        }
    }

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static ConversationQueryHandler CreateHandler(
        FakeTenantAccessService access,
        FakeProjectionReadStore store,
        IQueryCursorCodec? cursor = null,
        TimeProvider? time = null,
        ConversationReadHydrationService? hydration = null,
        ConversationPrivilegedJustificationReviewService? privilegedReview = null,
        int maxOffset = 100_000)
    {
        IQueryCursorCodec codec = cursor ?? CreateCodec();
        ConversationProjectionReadService readService = new(access, store);
        return new ConversationQueryHandler(
            access,
            store,
            readService,
            codec,
            Options.Create(new ConversationQueryCursorOptions { MaxOffset = maxOffset }),
            time ?? new FakeTimeProvider(Now),
            hydration,
            privilegedJustificationReviewService: privilegedReview);
    }

    private static IQueryCursorCodec CreateCodec(string purpose = ConversationQueryServiceCollectionExtensions.CursorCodecPurpose)
        => new QueryCursorCodec(s_dataProtection, purpose);

    // Forges a list continuation cursor via the SDK codec exactly as ConversationQueryHandler would issue one
    // (scope = tenant/caller/filter/sort; position = offset/issued-at/generation). Re-expresses the retired
    // ForgeCursorWithOffset helper that hand-built an HMAC payload.
    private static string EncodeCursor(
        IQueryCursorCodec codec,
        TenantId tenant,
        string caller,
        ConversationListFilterV1 filter,
        int offset,
        string generationToken,
        DateTimeOffset issuedAt)
        => codec.Encode(
            ConversationListCursor.QueryType,
            ConversationListCursor.BuildScope(tenant, caller, filter),
            ConversationListCursor.EncodePosition(offset, issuedAt, generationToken));

    // Corrupts one character of a protected cursor so Data Protection Unprotect fails (tamper-or-key-rotation).
    private static string TamperCursor(string cursor)
    {
        char[] chars = cursor.ToCharArray();
        int index = chars.Length / 2;
        chars[index] = chars[index] == 'A' ? 'B' : 'A';
        return new string(chars);
    }

    private static ConversationProjectedReadModels ProjectedModels(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            Detail(tenantId, conversationId));

    private static ConversationProjectedReadModels ProjectedModelsWithDegradedTrust(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            DetailWithDegradedTrust(tenantId, conversationId));

    private static ConversationProjectedReadModels ProjectedModelsWithAuditRecord(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            DetailWithAuditRecord(tenantId, conversationId));

    private static ConversationSummaryProjectionV1 Summary(
        TenantId tenantId,
        ConversationId conversationId,
        BusinessReference? business,
        ProjectId? project,
        FolderId? folder,
        PartyId participant,
        string lifecycle = "Open",
        string cursor = "pos:0000000001",
        DateTimeOffset? lastAppliedAt = null,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null,
        ConversationSearchTrustPreviewV1? trustPreview = null)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness(cursor, lastAppliedAt, freshnessState, reason),
            lifecycle,
            "Case 123",
            business,
            project,
            folder,
            [participant],
            MessageCount: 1,
            FileReferenceCount: 0,
            SearchTrustPreview: trustPreview);

    private static ConversationSearchTrustPreviewV1 TrustPreview(
        ProjectionTrustState redactionState,
        ProjectionTrustState freshnessState,
        ConversationAuditReadinessState auditReadiness,
        ConversationVerificationState verificationState)
        => new(
            freshnessState,
            freshnessState == ProjectionTrustState.Current
                ? ProjectionFreshnessReasonCode.Current
                : ProjectionFreshnessReasonCode.StaleThresholdExceeded,
            redactionState,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            auditReadiness,
            verificationState,
            ConversationSearchMatchSource.TenantScope,
            "Visible through authorized tenant scope.");

    private static ConversationDetailProjectionV1 Detail(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness("pos:0000000001"),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            [],
            TrustPosture: CurrentTrustPosture(tenantId, conversationId, auditReady: false),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    "message:message-001",
                    "Message",
                    Actor,
                    Now,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Available,
                    ConversationAuditReadinessState.Incomplete,
                    ProjectionTrustState.Current,
                    MessageId: new MessageId("message-001"),
                    VisibleText: "Hello from the adopter."),
            ]);

    private static ConversationDetailProjectionV1 DetailWithDegradedTrust(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness("pos:0000000001"),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Partial evidence available.", Now)],
            [],
            TrustPosture: new ConversationEvidenceTrustPostureV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                "pos:0000000001",
                Freshness("pos:0000000001"),
                ProjectionTrustState.Unavailable,
                ProjectionTrustState.Unavailable,
                ConversationCitationAvailability.Unavailable,
                ConversationAuditReadinessState.Incomplete,
                ConversationVerificationState.Unknown),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    "message:message-001",
                    "Message",
                    Actor,
                    Now,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Unavailable,
                    ConversationAuditReadinessState.Incomplete,
                    ProjectionTrustState.Unavailable,
                    MessageId: new MessageId("message-001"),
                    VisibleText: "Partial evidence available."),
            ]);

    private static ConversationDetailProjectionV1 DetailWithCommandMetadata(
        TenantId tenantId,
        ConversationId conversationId,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null,
        ProjectionTrustState? commandAvailability = null)
    {
        ProjectionFreshnessV1 freshness = Freshness(
            "pos:0000000001",
            freshnessState: freshnessState,
            reason: reason);

        return new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            [],
            TrustPosture: new ConversationEvidenceTrustPostureV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                freshness.ProjectionCursor,
                freshness,
                ProjectionTrustState.Current,
                ProjectionTrustState.Current,
                ConversationCitationAvailability.Available,
                ConversationAuditReadinessState.Ready,
                ConversationVerificationState.Unknown,
                [
                    new ConversationCommandAvailabilityV1(
                        "set-retention-policy",
                        commandAvailability ?? ProjectionTrustState.Unavailable,
                        "conversations.governance",
                        ProjectionTrustState.Current,
                        "governance",
                        ProjectionTrustState.Current,
                        ConversationAuditReadinessState.Ready,
                        "Command execution requires a fresh server recheck.",
                        Now,
                        ConversationCommandAvailabilityV1.GovernanceChangingActionClassification),
                ]));
    }

    private static ConversationDetailProjectionV1 DetailWithAuditRecord(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness("pos:0000000001"),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            [],
            ActiveRetentionPolicy: new ConversationRetentionPolicyProjectionV1(
                "retention-policy-standard",
                "customer-request",
                Actor,
                Now,
                new GovernanceAuditEvidenceReference(
                    new AuditEvidenceHandle("audit-evidence-001"),
                "retention-policy-standard",
                Now)),
            TrustPosture: CurrentTrustPosture(tenantId, conversationId, auditReady: true));

    private static ConversationDetailProjectionV1 DetailWithCitation(
        TenantId tenantId,
        ConversationId conversationId,
        bool includeAuditEvidence = true,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null,
        long? safeSourcePosition = null)
    {
        ProjectionFreshnessV1 freshness = Freshness(
            "pos:0000000001",
            freshnessState: freshnessState,
            reason: reason);
        GovernanceAuditEvidenceReference? auditEvidence = includeAuditEvidence
            ? new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "retention-policy-standard",
                Now)
            : null;
        ConversationAuditReadinessState auditReadiness = includeAuditEvidence
            ? ConversationAuditReadinessState.Ready
            : ConversationAuditReadinessState.Incomplete;

        return new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            [],
            TrustPosture: new ConversationEvidenceTrustPostureV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                freshness.ProjectionCursor,
                freshness,
                ProjectionTrustState.Current,
                ProjectionTrustState.Current,
                ConversationCitationAvailability.Available,
                auditReadiness,
                ConversationVerificationState.Unknown),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    "message:message-001",
                    "Message",
                    Actor,
                    Now,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Available,
                    auditReadiness,
                    ProjectionTrustState.Current,
                    MessageId: new MessageId("message-001"),
                    VisibleText: "Hello from the adopter.",
                    AuditEvidence: auditEvidence,
                    SafeSummaryLabel: "Message evidence citation",
                    SafeAccessibilityLabel: "Copy message evidence citation",
                    SafeNextAction: "Open stable temporal evidence link.",
                    SafeSourcePosition: safeSourcePosition),
            ]);
    }

    private static ConversationDetailProjectionV1 DetailWithRedactedCitation(TenantId tenantId, ConversationId conversationId)
    {
        ProjectionFreshnessV1 freshness = Freshness("pos:0000000001");
        GovernanceAuditEvidenceReference auditEvidence = new(
            new AuditEvidenceHandle("audit-evidence-001"),
            "redaction-policy-standard",
            Now);
        ConversationRedactionAttributionV1 attribution = new(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            Actor,
            Now,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: new MessageId("message-001")),
            "message:message-001",
            auditEvidence,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            "[redacted]",
            "Redacted message evidence citation",
            "Copy redacted message evidence citation",
            "Open stable temporal evidence link.");

        return new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "secret customer content", Now)],
            [],
            TrustPosture: new ConversationEvidenceTrustPostureV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                freshness.ProjectionCursor,
                freshness,
                ProjectionTrustState.Redacted,
                ProjectionTrustState.Current,
                ConversationCitationAvailability.Available,
                ConversationAuditReadinessState.Ready,
                ConversationVerificationState.Unknown),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    "message:message-001",
                    "Message",
                    Actor,
                    Now,
                    ProjectionTrustState.Redacted,
                    ConversationCitationAvailability.Available,
                    ConversationAuditReadinessState.Ready,
                    ProjectionTrustState.Redacted,
                    MessageId: new MessageId("message-001"),
                    VisibleText: "[redacted]",
                    AuditEvidence: auditEvidence,
                    SafeSummaryLabel: "Redacted message evidence citation",
                    SafeAccessibilityLabel: "Copy redacted message evidence citation",
                    SafeNextAction: "Open stable temporal evidence link.",
                    RedactionAttribution: attribution),
            ]);
    }

    private static ConversationEvidenceTrustPostureV1 CurrentTrustPosture(
        TenantId tenantId,
        ConversationId conversationId,
        bool auditReady)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            "pos:0000000001",
            Freshness("pos:0000000001"),
            ProjectionTrustState.Current,
            ProjectionTrustState.Unavailable,
            ConversationCitationAvailability.Available,
            auditReady ? ConversationAuditReadinessState.Ready : ConversationAuditReadinessState.Incomplete,
            ConversationVerificationState.Unknown);

    private static PrivilegedOperationalJustificationDetailsV1 PrivilegedDetails()
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            Now,
            GovernanceOutcome.Succeeded,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-privileged-001"),
                "privileged-review-policy",
                Now),
            ProjectionTrustState.Current,
            Freshness("pos:0000000001"),
            "Use the returned audit handle as governed evidence.",
            "correlation-001");

    private static ProjectionFreshnessV1 Freshness(
        string cursor = "pos:0000000001",
        DateTimeOffset? lastAppliedAt = null,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null)
    {
        ProjectionTrustState state = freshnessState ?? ProjectionTrustState.Current;
        bool isStale = state == ProjectionTrustState.Stale;
        return new(
            SchemaVersion.Current,
            cursor,
            1,
            lastAppliedAt ?? Now,
            (lastAppliedAt ?? Now).AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: isStale,
            state,
            reason ?? ProjectionFreshnessReasonCode.Current);
    }

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Calls { get; private set; }

        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(decision);
        }
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; set; } = [];

        public Exception? ListException { get; set; }

        public int DetailReads { get; private set; }

        public int ListReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            ListReads++;
            if (ListException is not null)
            {
                throw ListException;
            }

            return ValueTask.FromResult(Summaries);
        }
    }

    private sealed class FakePrivilegedReviewSource : IPrivilegedOperationalJustificationReviewSource
    {
        public PrivilegedOperationalJustificationDetailsV1? Details { get; set; }

        public int Reads { get; private set; }

        public ValueTask<PrivilegedOperationalJustificationDetailsV1?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            AuditEvidenceHandle auditEvidenceHandle,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(Details);
        }
    }

    private sealed class FakeReferenceHydrationDirectory : IConversationReferenceHydrationDirectory
    {
        public Dictionary<PartyId, ReferenceHydrationResult<PartyId>> PartyResults { get; } = [];

        public Dictionary<ProjectId, ReferenceHydrationResult<ProjectId>> ProjectResults { get; } = [];

        public Dictionary<FolderId, ReferenceHydrationResult<FolderId>> FolderResults { get; } = [];

        public Dictionary<FileId, ReferenceHydrationResult<FileId>> FileResults { get; } = [];

        public int PartyBatchCalls { get; private set; }

        public ConversationHydrationContext? LastContext { get; private set; }

        public IReadOnlyList<PartyId> LastPartyIds { get; private set; } = [];

        public ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<PartyId> partyIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PartyBatchCalls++;
            LastContext = context;
            LastPartyIds = partyIds.ToList();
            return ValueTask.FromResult((IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>)PartyResults);
        }

        public ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<ProjectId> projectIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>)ProjectResults);
        }

        public ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FolderId> folderIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>)FolderResults);
        }

        public ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FileId> fileIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>)FileResults);
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
