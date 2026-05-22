// <copyright file="ConversationReadApiTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Api;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Api;

public sealed class ConversationReadApiTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly BusinessReference Business = new("crm", "case-123");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReadRoutesShouldRequireAuthorization()
    {
        using WebApplication app = BuildApp(AllowedAccess(), new FakeProjectionReadStore());

        RouteEndpoint detail = FindEndpoint(app, "/api/v1/conversations/{conversationId}");
        RouteEndpoint citation = FindEndpoint(app, "/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}");
        RouteEndpoint temporal = FindEndpoint(app, "/api/v1/conversations/{conversationId}/temporal");
        RouteEndpoint audit = FindEndpoint(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}");
        RouteEndpoint list = FindEndpoint(app, "/api/v1/conversations/");

        detail.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        citation.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        temporal.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        audit.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        list.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        detail.RoutePattern.RawText.ShouldBe("/api/v1/conversations/{conversationId}");
        citation.RoutePattern.RawText.ShouldBe("/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}");
        temporal.RoutePattern.RawText.ShouldBe("/api/v1/conversations/{conversationId}/temporal");
        audit.RoutePattern.RawText.ShouldBe("/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}");
        list.RoutePattern.RawText.ShouldBe("/api/v1/conversations/");
    }

    [Fact]
    public async Task DetailRequestMissingTenantClaimShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new();
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "conversation-001" },
            user: AuthenticatedUserWithoutTenant());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task DetailRequestMalformedConversationIdShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "   " },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task DetailRequestShouldBindTenantAndCallerOnlyFromTrustedClaims()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            queryString: "?tenantId=tenant-evil&callerPrincipalId=caller-evil&user=caller-evil&role=admin&commandPermission=conversations.governance",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        access.Calls.ShouldBe(1);
        access.LastTrustedTenantId.ShouldBe(Tenant);
        access.LastRouteTenantId.ShouldBe(Tenant);
        access.LastProjectionTenantId.ShouldBe(Tenant);
        access.LastCallerPrincipalId.ShouldBe("caller-001");
        response.Body.ShouldNotContain("tenant-evil", Case.Insensitive);
        response.Body.ShouldNotContain("caller-evil", Case.Insensitive);
        response.Body.ShouldNotContain("commandPermission", Case.Insensitive);
    }

    [Fact]
    public async Task DetailRequestHandlerFailureShouldReturnUnavailableShape()
    {
        using WebApplication app = BuildApp(new ThrowingTenantAccessService(), new FakeProjectionReadStore());

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "conversation-001" },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        response.Body.ShouldContain("\"freshnessState\":\"Unavailable\"");
        response.Body.ShouldNotContain("tenant-001", Case.Insensitive);
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
    }

    [Fact]
    public async Task DetailRequestShouldReturnGovernedTrustPostureAndEvidenceEntries()
    {
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(AllowedAccess(), store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"trustPosture\"");
        response.Body.ShouldContain("\"evidenceEntries\"");
        response.Body.ShouldContain("\"commandEligibility\"");
        response.Body.ShouldContain("\"availabilityState\":\"Unavailable\"");
        response.Body.ShouldContain("\"kind\":\"Message\"");
        response.Body.ShouldNotContain("EventStore", Case.Insensitive);
        response.Body.ShouldNotContain("providerSessionReference", Case.Insensitive);
        response.Body.ShouldNotContain("transcript", Case.Insensitive);
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task AuditRecordRequestShouldBindTrustedClaimsAndReturnSafeDetails()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["auditEvidenceHandle"] = "audit-evidence-001",
            },
            queryString: "?tenantId=tenant-evil&callerPrincipalId=caller-evil&action=Exported",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"actionClass\":\"Allowed\"");
        response.Body.ShouldContain("\"policyBasis\":\"retention-policy-standard\"");
        response.Body.ShouldContain("\"auditEvidence\"");
        response.Body.ShouldNotContain("tenant-evil", Case.Insensitive);
        response.Body.ShouldNotContain("caller-evil", Case.Insensitive);
        response.Body.ShouldNotContain("storage", Case.Insensitive);
        response.Body.ShouldNotContain("EventStore", Case.Insensitive);
        access.Calls.ShouldBe(1);
        access.LastTrustedTenantId.ShouldBe(Tenant);
        access.LastCallerPrincipalId.ShouldBe("caller-001");
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task CitationRequestShouldBindTrustedClaimsAndReturnSafeCopiedText()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["evidenceEntryId"] = "message:message-001",
            },
            queryString: "?tenantId=tenant-evil&callerPrincipalId=caller-evil&permission=admin",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"citation\"");
        response.Body.ShouldContain("\"safeCopiedText\"");
        response.Body.ShouldContain("\"temporalCursor\"");
        response.Body.ShouldNotContain("Hello from the adopter.", Case.Insensitive);
        response.Body.ShouldNotContain("tenant-evil", Case.Insensitive);
        response.Body.ShouldNotContain("caller-evil", Case.Insensitive);
        response.Body.ShouldNotContain("permission", Case.Insensitive);
        access.LastTrustedTenantId.ShouldBe(Tenant);
        access.LastCallerPrincipalId.ShouldBe("caller-001");
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task CitationMalformedTargetShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["evidenceEntryId"] = "bad target",
            },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("message-001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task CitationPermissionDowngradeShouldClearClipboardAndLinkMetadata()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);
        Dictionary<string, object?> routeValues = new()
        {
            ["conversationId"] = Conversation.Value,
            ["evidenceEntryId"] = "message:message-001",
        };

        ApiResponse visible = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}",
            routeValues: routeValues,
            user: AuthenticatedUser());
        access.SetDecision(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.MissingMember));

        ApiResponse denied = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/citations/{evidenceEntryId}",
            routeValues: routeValues,
            user: AuthenticatedUser());

        visible.StatusCode.ShouldBe(StatusCodes.Status200OK);
        visible.Body.ShouldContain("\"safeCopiedText\"");
        visible.Body.ShouldContain("\"temporalCursor\"");
        denied.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        denied.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        denied.Body.ShouldNotContain("\"safeCopiedText\"", Case.Insensitive);
        denied.Body.ShouldNotContain("\"temporalCursor\"", Case.Insensitive);
        denied.Body.ShouldNotContain("message-001", Case.Insensitive);
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task TemporalMalformedCursorShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/temporal",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            queryString: "?cursor=raw-stream-position",
            user: AuthenticatedUser());
        ApiResponse malformedProjection = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/temporal",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            queryString: "?cursor=temporal:v1:pos:0000000002:projection:not-a-number",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        response.Body.ShouldNotContain("raw-stream-position", Case.Insensitive);
        malformedProjection.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        malformedProjection.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        malformedProjection.Body.ShouldNotContain("not-a-number", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task TemporalBarePositionCursorShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/temporal",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            queryString: "?cursor=temporal:v1:pos:0000000001",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("temporal:v1:pos:0000000001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task AuditRecordMalformedHandleShouldReturnHiddenShape()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["auditEvidenceHandle"] = "bad handle",
            },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task AuditRecordMissingTenantClaimShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["auditEvidenceHandle"] = "audit-evidence-001",
            },
            user: AuthenticatedUserWithoutTenant());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        response.Body.ShouldNotContain("audit-evidence-001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task AuditRecordStoreFailureShouldReturnUnavailableShapeWithoutRawTerms()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            ReadException = new UnauthorizedAccessException("raw audit backend path"),
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: new Dictionary<string, object?>
            {
                ["conversationId"] = Conversation.Value,
                ["auditEvidenceHandle"] = "audit-evidence-001",
            },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        response.Body.ShouldContain("\"freshnessState\":\"Unavailable\"");
        response.Body.ShouldNotContain("raw", Case.Insensitive);
        response.Body.ShouldNotContain("backend", Case.Insensitive);
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        access.Calls.ShouldBe(1);
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task AuditRecordPermissionDowngradeShouldClearProtectedDetail()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        using WebApplication app = BuildApp(access, store);
        Dictionary<string, object?> routeValues = new()
        {
            ["conversationId"] = Conversation.Value,
            ["auditEvidenceHandle"] = "audit-evidence-001",
        };

        ApiResponse visible = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: routeValues,
            user: AuthenticatedUser());
        access.SetDecision(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.MissingMember));

        ApiResponse denied = await InvokeAsync(app, "/api/v1/conversations/{conversationId}/audit-records/{auditEvidenceHandle}",
            routeValues: routeValues,
            user: AuthenticatedUser());

        visible.StatusCode.ShouldBe(StatusCodes.Status200OK);
        visible.Body.ShouldContain("\"policyBasis\":\"retention-policy-standard\"");
        denied.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        denied.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        denied.Body.ShouldNotContain("retention-policy-standard", Case.Insensitive);
        denied.Body.ShouldNotContain("audit-evidence-001", Case.Insensitive);
        denied.Body.ShouldNotContain("\"auditEvidence\"", Case.Insensitive);
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task ListRequestWithIncompleteBusinessFilterShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, new ConversationId("conversation-match"), Business, Project, Folder, Participant)],
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: "?businessSystem=crm",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldContain("\"conversations\":[]");
        access.Calls.ShouldBe(0);
        store.ListReads.ShouldBe(0);
    }

    [Fact]
    public async Task ListRequestShouldBindFilterAndPageParameters()
    {
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conversation-match"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conversation-business-miss"), new BusinessReference("crm", "case-999"), Project, Folder, Participant),
                Summary(OtherTenant, new ConversationId("conversation-cross-tenant"), Business, Project, Folder, Participant),
            ],
        };
        using WebApplication app = BuildApp(AllowedAccess(), store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: "?businessSystem=crm&businessValue=case-123&projectId=project-001&folderId=folder-001&lifecycleState=Open&participantPartyId=party-participant&pageSize=1",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement conversations = document.RootElement.GetProperty("conversations");
        conversations.GetArrayLength().ShouldBe(1);
        conversations[0].GetProperty("conversationId").GetString().ShouldBe("conv:conversation-match");
        document.RootElement.GetProperty("page").GetProperty("returnedCount").GetInt32().ShouldBe(1);
        response.Body.ShouldNotContain("conversation-business-miss", Case.Insensitive);
        response.Body.ShouldNotContain("conversation-cross-tenant", Case.Insensitive);
    }

    [Fact]
    public async Task ListRequestShouldBindStory31TrustFilterParameters()
    {
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(
                    Tenant,
                    new ConversationId("conversation-match"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    TrustPreview(
                        ProjectionTrustState.Redacted,
                        ProjectionTrustState.Stale,
                        ConversationAuditReadinessState.Ready,
                        ConversationVerificationState.Verified),
                    ProjectionTrustState.Stale,
                    ProjectionFreshnessReasonCode.StaleThresholdExceeded),
                Summary(
                    Tenant,
                    new ConversationId("conversation-miss"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    TrustPreview(
                        ProjectionTrustState.Current,
                        ProjectionTrustState.Current,
                        ConversationAuditReadinessState.Incomplete,
                        ConversationVerificationState.Unverified)),
            ],
        };
        using WebApplication app = BuildApp(AllowedAccess(), store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: "?redactionState=Redacted&freshnessState=Stale&auditReadiness=Ready&verificationState=Verified",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement conversations = document.RootElement.GetProperty("conversations");
        conversations.GetArrayLength().ShouldBe(1);
        conversations[0].GetProperty("conversationId").GetString().ShouldBe("conv:conversation-match");
        conversations[0].GetProperty("searchTrustPreview").GetProperty("redactionState").GetString().ShouldBe("Redacted");
        response.Body.ShouldNotContain("conversation-miss", Case.Insensitive);
        response.Body.ShouldNotContain("autocomplete", Case.Insensitive);
        response.Body.ShouldNotContain("recentSearch", Case.Insensitive);
        response.Body.ShouldNotContain("facet", Case.Insensitive);
    }

    [Theory]
    [InlineData("?projectedAtFrom=not-a-date")]
    [InlineData("?redactionState=TranscriptText")]
    [InlineData("?freshnessState=Maybe")]
    [InlineData("?auditReadiness=almost-ready")]
    [InlineData("?verificationState=maybe")]
    [InlineData("?pageSize=not-a-number")]
    public async Task ListRequestWithMalformedStory31FilterShouldReturnHiddenShapeWithoutProjectionRead(string queryString)
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, new ConversationId("conversation-match"), Business, Project, Folder, Participant)],
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: queryString,
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldContain("\"conversations\":[]");
        access.Calls.ShouldBe(0);
        store.ListReads.ShouldBe(0);
    }

    private static WebApplication BuildApp(IConversationTenantAccessService access, IConversationProjectionReadStore store)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(access);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(new ConversationProjectionReadService(access, store));
        builder.Services.AddSingleton(CreateCursor());
        builder.Services.AddSingleton<ConversationQueryHandler>();

        WebApplication app = builder.Build();
        app.MapConversationReadApi();
        return app;
    }

    private static async Task<ApiResponse> InvokeAsync(
        WebApplication app,
        string routePattern,
        IReadOnlyDictionary<string, object?>? routeValues = null,
        string? queryString = null,
        ClaimsPrincipal? user = null)
    {
        RouteEndpoint endpoint = FindEndpoint(app, routePattern);
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString(queryString ?? string.Empty);
        context.Response.Body = new MemoryStream();

        if (routeValues is not null)
        {
            foreach (KeyValuePair<string, object?> routeValue in routeValues)
            {
                context.Request.RouteValues[routeValue.Key] = routeValue.Value;
            }
        }

        await endpoint.RequestDelegate!(context).ConfigureAwait(false);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        string body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return new ApiResponse(context.Response.StatusCode, body);
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string routePattern)
        => ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, routePattern, StringComparison.Ordinal));

    private static ClaimsPrincipal AuthenticatedUser() => new(new ClaimsIdentity(
        [new Claim(ConversationReadApi.TenantIdClaimType, Tenant.Value), new Claim(ClaimTypes.NameIdentifier, "caller-001")],
        authenticationType: "Test"));

    private static ClaimsPrincipal AuthenticatedUserWithoutTenant() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "caller-001")],
        authenticationType: "Test"));

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static ConversationQueryCursor CreateCursor()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return new ConversationQueryCursor(Options.Create(new ConversationQueryCursorOptions { SigningKey = key, KeyId = "api-test-key" }));
    }

    private static ConversationSummaryProjectionV1 Summary(
        TenantId tenantId,
        ConversationId conversationId,
        BusinessReference? business,
        ProjectId? project,
        FolderId? folder,
        PartyId participant,
        ConversationSearchTrustPreviewV1? trustPreview = null,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            new ProjectionFreshnessV1(
                SchemaVersion.Current,
                "pos:0000000001",
                1,
                Now,
                Now.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                IsStale: freshnessState == ProjectionTrustState.Stale,
                freshnessState ?? ProjectionTrustState.Current,
                reason ?? ProjectionFreshnessReasonCode.Current),
            "Open",
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

    private static ConversationProjectedReadModels ProjectedModels(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            new ConversationDetailProjectionV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                Freshness(),
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
                    "pos:0000000001",
                    Freshness(),
                    ProjectionTrustState.Current,
                    ProjectionTrustState.Unavailable,
                    ConversationCitationAvailability.Available,
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
                        ConversationCitationAvailability.Available,
                        ConversationAuditReadinessState.Incomplete,
                        ProjectionTrustState.Current,
                        MessageId: new MessageId("message-001"),
                        VisibleText: "Hello from the adopter."),
                ]));

    private static ConversationProjectedReadModels ProjectedModelsWithAuditRecord(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            new ConversationDetailProjectionV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                Freshness(),
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
                        Now))));

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

    private sealed record ApiResponse(int StatusCode, string Body);

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        private ConversationTenantAccessDecision _decision = decision;

        public int Calls { get; private set; }

        public string? LastCallerPrincipalId { get; private set; }

        public TenantId? LastProjectionTenantId { get; private set; }

        public TenantId? LastRouteTenantId { get; private set; }

        public TenantId? LastTrustedTenantId { get; private set; }

        public void SetDecision(ConversationTenantAccessDecision decision)
            => _decision = decision;

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
            LastTrustedTenantId = trustedTenantId;
            LastCallerPrincipalId = callerPrincipalId;
            LastRouteTenantId = routeTenantId;
            LastProjectionTenantId = projectionTenantId;
            return ValueTask.FromResult(_decision);
        }
    }

    private sealed class ThrowingTenantAccessService : IConversationTenantAccessService
    {
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
            => throw new InvalidOperationException("Synthetic tenant projection outage.");
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; init; } = [];

        public ConversationProjectedReadModels? Models { get; init; }

        public Exception? ReadException { get; init; }

        public int DetailReads { get; private set; }

        public int ListReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            if (ReadException is not null)
            {
                throw ReadException;
            }

            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            ListReads++;
            return ValueTask.FromResult(Summaries);
        }
    }
}
