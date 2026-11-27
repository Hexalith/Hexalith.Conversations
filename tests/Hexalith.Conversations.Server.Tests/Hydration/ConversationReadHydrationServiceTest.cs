// <copyright file="ConversationReadHydrationServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Hydration;

namespace Hexalith.Conversations.Server.Tests.Hydration;

/// <summary>
/// Verifies read-time hydration remains request-scoped, deduplicated, and content safe.
/// </summary>
public sealed class ConversationReadHydrationServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly FileId File = new("file-001");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Detail hydration deduplicates stable references, maps inaccessible resources safely, and never rewrites source IDs.
    /// </summary>
    [Fact]
    public async Task HydrateDetailShouldDeduplicateAndAvoidPoisonDisclosure()
    {
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Actor] = new ReferenceHydrationResult<PartyId>(
                    Actor,
                    ReferenceHydrationStatus.Forbidden,
                    SafeLabel: "POISON actor email@example.invalid",
                    SafeToken: "POISON-token",
                    SafeStatus: "POISON-status"),
                [Participant] = new ReferenceHydrationResult<PartyId>(
                    Participant,
                    ReferenceHydrationStatus.Stale,
                    SafeLabel: "Renamed participant",
                    SafeToken: "participant-token",
                    SafeStatus: "Stale"),
            },
            ProjectResults =
            {
                [Project] = new ReferenceHydrationResult<ProjectId>(
                    Project,
                    ReferenceHydrationStatus.Unavailable,
                    SafeLabel: "POISON project name"),
            },
            FolderResults =
            {
                [Folder] = new ReferenceHydrationResult<FolderId>(
                    Folder,
                    ReferenceHydrationStatus.Redacted,
                    SafeLabel: "POISON folder path"),
            },
            FileResults =
            {
                [File] = new ReferenceHydrationResult<FileId>(
                    File,
                    ReferenceHydrationStatus.Stale,
                    SafeLabel: "Current file label",
                    SafeToken: "file-token",
                    SafeStatus: "Stale"),
            },
        };
        ConversationReadHydrationService service = new(directory);
        ConversationDetailsV1 source = ConversationDetailsV1.FromProjection(Detail());

        ConversationDetailsV1 hydrated = await service.HydrateDetailAsync(
            source,
            new ConversationHydrationContext(Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        directory.PartyBatchCalls.ShouldBe(1);
        directory.ProjectBatchCalls.ShouldBe(1);
        directory.FolderBatchCalls.ShouldBe(1);
        directory.FileBatchCalls.ShouldBe(1);
        directory.LastPartyIds.ShouldBe([Actor, Participant], ignoreOrder: true);
        hydrated.Participants[0].ParticipantPartyId.ShouldBe(Participant);
        hydrated.Messages[0].AuthorPartyId.ShouldBe(Actor);
        hydrated.ProjectId.ShouldBe(Project);
        hydrated.FolderId.ShouldBe(Folder);
        hydrated.FileReferences[0].FileId.ShouldBe(File);

        hydrated.PartyHydration.Count.ShouldBe(2);
        hydrated.PartyHydration.Single(x => x.PartyId == Participant).SafeLabel.ShouldBe("Renamed participant");
        PartyReferenceHydrationV1 forbiddenActor = hydrated.PartyHydration.Single(x => x.PartyId == Actor);
        forbiddenActor.HydrationState.ShouldBe(ProjectionTrustState.Forbidden);
        forbiddenActor.Resolved.ShouldBeFalse();
        forbiddenActor.SafeLabel.ShouldBe("Participant unavailable");
        forbiddenActor.SafeToken.ShouldBe("unavailable");
        forbiddenActor.SafeStatus.ShouldBe("Unavailable");

        hydrated.ProjectHydration.ShouldNotBeNull();
        hydrated.ProjectHydration.HydrationState.ShouldBe(ProjectionTrustState.Unavailable);
        hydrated.ProjectHydration.SafeLabel.ShouldBe("Reference unavailable");
        hydrated.FolderHydration.ShouldNotBeNull();
        hydrated.FolderHydration.HydrationState.ShouldBe(ProjectionTrustState.Redacted);
        hydrated.FolderHydration.SafeLabel.ShouldBe("Reference redacted");
        hydrated.FileHydration.Single().HydrationState.ShouldBe(ProjectionTrustState.Stale);
        hydrated.TrustPosture.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Forbidden);
        hydrated.TrustPosture.EvidenceCompletenessState.ShouldBe(ProjectionTrustState.Current);
        hydrated.TrustPosture.CitationAvailability.ShouldBe(ConversationCitationAvailability.Available);

        string serialized = System.Text.Json.JsonSerializer.Serialize(
            hydrated,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        serialized.ShouldNotContain("POISON", Case.Insensitive);
        serialized.ShouldNotContain("email@example.invalid", Case.Insensitive);
    }

    /// <summary>
    /// Internal adapter outcomes collapse to public non-enumerable states without carrying adapter labels.
    /// </summary>
    [Theory]
    [InlineData(ReferenceHydrationStatus.Deleted, "Forbidden", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.NotFound, "Forbidden", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.Gone, "Forbidden", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.CrossTenantDenied, "Forbidden", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.PolicyFiltered, "Redacted", "Reference redacted", "Redacted")]
    [InlineData(ReferenceHydrationStatus.Erased, "Redacted", "Reference redacted", "Redacted")]
    [InlineData(ReferenceHydrationStatus.Timeout, "Unavailable", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.Throttled, "Unavailable", "Participant unavailable", "Unavailable")]
    [InlineData(ReferenceHydrationStatus.Rebuilding, "Rebuilding", "Participant unavailable", "Rebuilding")]
    public async Task DegradedPartyHydrationShouldUseSafeFallbacks(
        ReferenceHydrationStatus status,
        string expectedState,
        string expectedLabel,
        string expectedStatus)
    {
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Participant] = new ReferenceHydrationResult<PartyId>(
                    Participant,
                    status,
                    SafeLabel: "POISON secret label",
                    SafeToken: "POISON-token",
                    SafeStatus: "POISON-status"),
            },
        };
        ConversationReadHydrationService service = new(directory);
        ConversationDetailsV1 source = new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            Freshness(),
            "Open",
            Participants: [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)]);

        ConversationDetailsV1 hydrated = await service.HydrateDetailAsync(
            source,
            new ConversationHydrationContext(Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        PartyReferenceHydrationV1 party = hydrated.PartyHydration.Single();
        party.HydrationState.Value.ShouldBe(expectedState);
        party.Resolved.ShouldBeFalse();
        party.SafeLabel.ShouldBe(expectedLabel);
        party.SafeToken.ShouldNotContain("POISON", Case.Insensitive);
        party.SafeStatus.ShouldBe(expectedStatus);
    }

    /// <summary>
    /// Adapter failures degrade affected references without throwing raw upstream details into the response.
    /// </summary>
    [Fact]
    public async Task AdapterFailureShouldMapRequestedReferencesToUnavailable()
    {
        FakeReferenceHydrationDirectory directory = new() { ThrowOnParties = true };
        ConversationReadHydrationService service = new(directory);
        ConversationDetailsV1 source = new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            Freshness(),
            "Open",
            Participants: [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)]);

        ConversationDetailsV1 hydrated = await service.HydrateDetailAsync(
            source,
            new ConversationHydrationContext(Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        PartyReferenceHydrationV1 party = hydrated.PartyHydration.Single();
        party.HydrationState.ShouldBe(ProjectionTrustState.Unavailable);
        party.SafeLabel.ShouldBe("Participant unavailable");
        party.SafeStatus.ShouldBe("Unavailable");
    }

    /// <summary>
    /// Summary list hydration deduplicates shared party, project, and folder references into one batch call per kind
    /// and populates each summary's hydration fields accurately.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task HydrateSummariesShouldDeduplicateProjectsFoldersAndPartiesAndPopulateHydration()
    {
        ProjectId project2 = new("project-002");
        FolderId folder2 = new("folder-002");
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Actor] = new ReferenceHydrationResult<PartyId>(Actor, ReferenceHydrationStatus.Current, "Actor Name", "actor", "Current"),
                [Participant] = new ReferenceHydrationResult<PartyId>(Participant, ReferenceHydrationStatus.Stale, "Member Name", "member", "Stale"),
            },
            ProjectResults =
            {
                [Project] = new ReferenceHydrationResult<ProjectId>(Project, ReferenceHydrationStatus.Current, "Project Alpha", "project", "Current"),
                [project2] = new ReferenceHydrationResult<ProjectId>(project2, ReferenceHydrationStatus.Stale, "Project Beta", "project", "Stale"),
            },
            FolderResults =
            {
                [Folder] = new ReferenceHydrationResult<FolderId>(Folder, ReferenceHydrationStatus.Current, "Folder One", "folder", "Current"),
                [folder2] = new ReferenceHydrationResult<FolderId>(folder2, ReferenceHydrationStatus.Unavailable, "POISON folder"),
            },
        };
        ConversationReadHydrationService service = new(directory);

        ConversationSummaryV1 first = new(
            SchemaVersion.Current,
            Tenant,
            new ConversationId("conv-s1"),
            Freshness(),
            "Open",
            "First",
            ProjectId: Project,
            FolderId: Folder,
            ParticipantPartyIds: [Actor, Participant]);
        ConversationSummaryV1 second = new(
            SchemaVersion.Current,
            Tenant,
            new ConversationId("conv-s2"),
            Freshness(),
            "Open",
            "Second",
            ProjectId: Project,
            FolderId: folder2,
            ParticipantPartyIds: [Actor]);
        ConversationSummaryV1 third = new(
            SchemaVersion.Current,
            Tenant,
            new ConversationId("conv-s3"),
            Freshness(),
            "Open",
            "Third",
            ProjectId: project2,
            FolderId: Folder,
            ParticipantPartyIds: [Participant]);

        IReadOnlyList<ConversationSummaryV1> hydrated = await service.HydrateSummariesAsync(
            [first, second, third],
            new ConversationHydrationContext(Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        directory.PartyBatchCalls.ShouldBe(1);
        directory.ProjectBatchCalls.ShouldBe(1);
        directory.FolderBatchCalls.ShouldBe(1);
        directory.LastPartyIds.ShouldBe([Actor, Participant], ignoreOrder: true);
        directory.LastProjectIds.ShouldBe([Project, project2], ignoreOrder: true);
        directory.LastFolderIds.ShouldBe([Folder, folder2], ignoreOrder: true);

        hydrated.Count.ShouldBe(3);
        hydrated[0].ProjectHydration.ShouldNotBeNull().SafeLabel.ShouldBe("Project Alpha");
        hydrated[0].FolderHydration.ShouldNotBeNull().SafeLabel.ShouldBe("Folder One");
        hydrated[0].PartyHydration.Count.ShouldBe(2);
        hydrated[0].SearchTrustPreview.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Stale);

        hydrated[1].ProjectHydration.ShouldNotBeNull().SafeLabel.ShouldBe("Project Alpha");
        hydrated[1].FolderHydration.ShouldNotBeNull().HydrationState.ShouldBe(ProjectionTrustState.Unavailable);
        hydrated[1].FolderHydration!.SafeLabel.ShouldBe("Reference unavailable");
        hydrated[1].SearchTrustPreview.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Current);

        hydrated[2].ProjectHydration.ShouldNotBeNull().SafeLabel.ShouldBe("Project Beta");
        hydrated[2].FolderHydration.ShouldNotBeNull().SafeLabel.ShouldBe("Folder One");
    }

    private static ConversationDetailProjectionV1 Detail()
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            Freshness(),
            "Open",
            "Case 123",
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            null,
            [
                new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
                new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
            ],
            [
                new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello.", Now),
                new ConversationTimelineMessageProjectionV1(new MessageId("message-002"), Participant, "Hi.", Now.AddSeconds(1)),
            ],
            [
                new ConversationFileReferenceProjectionV1(File, Folder, new MessageId("message-001")),
                new ConversationFileReferenceProjectionV1(File, Folder, new MessageId("message-002")),
            ],
            TrustPosture: new ConversationEvidenceTrustPostureV1(
                SchemaVersion.Current,
                Tenant,
                Conversation,
                "pos:0000000001",
                Freshness(),
                ProjectionTrustState.Current,
                ProjectionTrustState.Unavailable,
                ConversationCitationAvailability.Available,
                ConversationAuditReadinessState.Ready,
                ConversationVerificationState.Unknown));

    private static ProjectionFreshnessV1 Freshness()
        => new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private sealed class FakeReferenceHydrationDirectory : IConversationReferenceHydrationDirectory
    {
        public Dictionary<PartyId, ReferenceHydrationResult<PartyId>> PartyResults { get; } = [];

        public Dictionary<ProjectId, ReferenceHydrationResult<ProjectId>> ProjectResults { get; } = [];

        public Dictionary<FolderId, ReferenceHydrationResult<FolderId>> FolderResults { get; } = [];

        public Dictionary<FileId, ReferenceHydrationResult<FileId>> FileResults { get; } = [];

        public int PartyBatchCalls { get; private set; }

        public int ProjectBatchCalls { get; private set; }

        public int FolderBatchCalls { get; private set; }

        public int FileBatchCalls { get; private set; }

        public IReadOnlyList<PartyId> LastPartyIds { get; private set; } = [];

        public IReadOnlyList<ProjectId> LastProjectIds { get; private set; } = [];

        public IReadOnlyList<FolderId> LastFolderIds { get; private set; } = [];

        public bool ThrowOnParties { get; set; }

        public ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<PartyId> partyIds,
            CancellationToken cancellationToken = default)
        {
            context.TenantId.ShouldBe(Tenant);
            context.CallerPrincipalId.ShouldBe("caller-001");
            context.CorrelationId.ShouldBe("correlation-001");
            cancellationToken.ThrowIfCancellationRequested();
            PartyBatchCalls++;
            LastPartyIds = partyIds.ToList();
            if (ThrowOnParties)
            {
                throw new TimeoutException("POISON upstream timeout detail");
            }

            return ValueTask.FromResult((IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>)PartyResults);
        }

        public ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<ProjectId> projectIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectBatchCalls++;
            LastProjectIds = projectIds.ToList();
            return ValueTask.FromResult((IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>)ProjectResults);
        }

        public ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FolderId> folderIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FolderBatchCalls++;
            LastFolderIds = folderIds.ToList();
            return ValueTask.FromResult((IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>)FolderResults);
        }

        public ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FileId> fileIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileBatchCalls++;
            return ValueTask.FromResult((IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>)FileResults);
        }
    }
}
