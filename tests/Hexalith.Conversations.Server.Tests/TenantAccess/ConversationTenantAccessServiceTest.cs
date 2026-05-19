// <copyright file="ConversationTenantAccessServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies fail-closed tenant access decisions for Conversations server boundaries.
/// </summary>
public sealed class ConversationTenantAccessServiceTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly TenantId OtherTenant = new("tenant-b");
    private const string Caller = "user-1";

    /// <summary>
    /// Tenants roles map conservatively to Conversations read/write/admin requirements.
    /// </summary>
    /// <param name="role">The Tenants role.</param>
    /// <param name="requirement">The Conversations requirement.</param>
    /// <param name="expectedAllowed">Whether access is expected.</param>
    [Theory]
    [InlineData(TenantRole.TenantReader, ConversationTenantAccessRequirement.Read, true)]
    [InlineData(TenantRole.TenantReader, ConversationTenantAccessRequirement.Write, false)]
    [InlineData(TenantRole.TenantReader, ConversationTenantAccessRequirement.Admin, false)]
    [InlineData(TenantRole.TenantContributor, ConversationTenantAccessRequirement.Read, true)]
    [InlineData(TenantRole.TenantContributor, ConversationTenantAccessRequirement.Write, true)]
    [InlineData(TenantRole.TenantContributor, ConversationTenantAccessRequirement.Admin, false)]
    [InlineData(TenantRole.TenantOwner, ConversationTenantAccessRequirement.Read, true)]
    [InlineData(TenantRole.TenantOwner, ConversationTenantAccessRequirement.Write, true)]
    [InlineData(TenantRole.TenantOwner, ConversationTenantAccessRequirement.Admin, true)]
    public async Task CheckAccessAsyncShouldMapTenantRolesConservatively(
        TenantRole role,
        ConversationTenantAccessRequirement requirement,
        bool expectedAllowed)
    {
        FakeTenantProjectionStore store = new(ActiveTenant(role));
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            requirement,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBe(expectedAllowed);
        decision.DenialReason.ShouldBe(expectedAllowed
            ? ConversationTenantAccessDenialReason.None
            : ConversationTenantAccessDenialReason.InsufficientRole);
        store.GetCount.ShouldBe(1);
    }

    /// <summary>
    /// Missing or malformed tenant/caller inputs fail before projection lookup.
    /// </summary>
    /// <param name="tenantId">The trusted tenant id.</param>
    /// <param name="caller">The caller principal id.</param>
    /// <param name="expectedReason">The expected denial reason.</param>
    [Theory]
    [MemberData(nameof(MissingOrMalformedInputs))]
    public async Task CheckAccessAsyncShouldFailClosedBeforeStoreLookupForUnsafeInputs(
        TenantId? tenantId,
        string? caller,
        ConversationTenantAccessDenialReason expectedReason)
    {
        FakeTenantProjectionStore store = new(ActiveTenant());
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            tenantId,
            caller,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(expectedReason);
        store.GetCount.ShouldBe(0);
    }

    /// <summary>
    /// Tenant-bearing inputs must all match exactly before protected state is touched.
    /// </summary>
    [Fact]
    public async Task CheckAccessAsyncShouldDenyTenantMismatchesBeforeStoreLookup()
    {
        FakeTenantProjectionStore store = new(ActiveTenant());
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            Caller,
            routeTenantId: OtherTenant,
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantMismatch);
        store.GetCount.ShouldBe(0);
    }

    /// <summary>
    /// Missing, disabled, non-member, insufficient-role, and unknown projection states fail closed.
    /// </summary>
    /// <param name="state">The projected tenant state.</param>
    /// <param name="requirement">The access requirement.</param>
    /// <param name="expectedReason">The expected denial reason.</param>
    [Theory]
    [MemberData(nameof(ProjectionDenials))]
    public async Task CheckAccessAsyncShouldDenyUnsafeProjectionStates(
        TenantLocalState? state,
        ConversationTenantAccessRequirement requirement,
        ConversationTenantAccessDenialReason expectedReason)
    {
        FakeTenantProjectionStore store = new(state);
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            requirement,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(expectedReason);
        store.GetCount.ShouldBe(1);
    }

    /// <summary>
    /// Projection-store exceptions become retryable internal denials without logging raw upstream text.
    /// </summary>
    [Fact]
    public async Task CheckAccessAsyncShouldClassifyProjectionStoreFailureWithoutLoggingRawDetails()
    {
        CapturingLogger<ConversationTenantAccessService> logger = new();
        FakeTenantProjectionStore store = new(ActiveTenant())
        {
            OnGet = static (_, _) => throw new InvalidOperationException(
                "raw upstream problem body tenant-secret participant Alice conversation title"),
        };
        ConversationTenantAccessService service = new(store, logger);

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessUnavailable);
        decision.IsRetryable.ShouldBeTrue();

        string logText = string.Join(Environment.NewLine, logger.Messages);
        logText.ShouldContain(nameof(InvalidOperationException));
        logText.ShouldNotContain("tenant-secret", Case.Insensitive);
        logText.ShouldNotContain("raw upstream", Case.Insensitive);
        logText.ShouldNotContain("Alice", Case.Insensitive);
        logText.ShouldNotContain("conversation title", Case.Insensitive);
    }

    /// <summary>
    /// Request cancellation propagates instead of becoming a tenant denial.
    /// </summary>
    [Fact]
    public async Task CheckAccessAsyncShouldPropagateOperationCanceledException()
    {
        FakeTenantProjectionStore store = new(ActiveTenant())
        {
            OnGet = static (_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<TenantLocalState?>(null);
            },
        };
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.CheckAccessAsync(ConversationTenantAccessRequirement.Read, Tenant, Caller, cancellationToken: cts.Token).AsTask());
    }

    /// <summary>
    /// Stale, gapped, rolled-back, and poisoned projection signals fail closed before state use.
    /// </summary>
    /// <param name="health">The projected health signal.</param>
    /// <param name="expectedReason">The expected denial reason.</param>
    [Theory]
    [MemberData(nameof(ProjectionHealthDenials))]
    public async Task CheckAccessAsyncShouldDenyProjectionHealthSignals(
        ConversationTenantProjectionHealth health,
        ConversationTenantAccessDenialReason expectedReason)
    {
        SignalingTenantProjectionStore store = new(ActiveTenant(), health);
        ConversationTenantAccessService service = new(store, new CapturingLogger<ConversationTenantAccessService>());

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(expectedReason);
        decision.ProjectionVersion.ShouldBe(42);
        decision.ProjectionWatermark.ShouldBe("watermark-safe");
        store.GetCount.ShouldBe(0);
    }

    /// <summary>
    /// Externally observable denial payloads collapse protected-record states into content-safe errors.
    /// </summary>
    [Fact]
    public void ToSafeErrorResultShouldNotExposeInternalDenialOrProtectedMetadata()
    {
        ConversationTenantAccessDecision decision = ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            new TenantId("tenant-secret"),
            "caller-secret",
            ConversationTenantAccessDenialReason.TenantDisabled,
            isRetryable: true,
            projectionVersion: 42,
            projectionWatermark: "watermark-secret");

        ConversationErrorResult result = decision.ToSafeErrorResult(SchemaVersion.Current, "correlation-safe");

        ConversationError error = result.Errors.Single();
        error.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        error.Category.ShouldBe(ConversationErrorCategory.Authorization);
        error.IsRetryable.ShouldBeFalse();

        string json = JsonSerializer.Serialize(result);
        json.ShouldNotContain("tenant-secret", Case.Insensitive);
        json.ShouldNotContain("caller-secret", Case.Insensitive);
        json.ShouldNotContain("TenantDisabled", Case.Insensitive);
        json.ShouldNotContain("watermark-secret", Case.Insensitive);
        json.ShouldNotContain("participant", Case.Insensitive);
        json.ShouldNotContain("title", Case.Insensitive);
        json.ShouldNotContain("snippet", Case.Insensitive);
        json.ShouldNotContain("count", Case.Insensitive);
        json.ShouldNotContain("pagination", Case.Insensitive);
        json.ShouldNotContain("provider", Case.Insensitive);
    }

    /// <summary>
    /// Supplies unsafe input examples for fail-closed input validation.
    /// </summary>
    public static TheoryData<TenantId?, string?, ConversationTenantAccessDenialReason> MissingOrMalformedInputs()
        => new()
        {
            { null, Caller, ConversationTenantAccessDenialReason.MissingTenant },
            { new TenantId(" tenant-a"), Caller, ConversationTenantAccessDenialReason.MalformedTenant },
            { new TenantId("tenant-a "), Caller, ConversationTenantAccessDenialReason.MalformedTenant },
            { new TenantId("tenant:tenant-a"), Caller, ConversationTenantAccessDenialReason.MalformedTenant },
            { Tenant, null, ConversationTenantAccessDenialReason.MissingCaller },
            { Tenant, string.Empty, ConversationTenantAccessDenialReason.MissingCaller },
            { Tenant, " ", ConversationTenantAccessDenialReason.MissingCaller },
        };

    /// <summary>
    /// Supplies projection-state denial scenarios.
    /// </summary>
    public static TheoryData<TenantLocalState?, ConversationTenantAccessRequirement, ConversationTenantAccessDenialReason> ProjectionDenials()
        => new()
        {
            { null, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.UnknownTenant },
            { ActiveTenant(tenantId: "tenant-b"), ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.TenantMismatch },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Disabled, Members = { [Caller] = TenantRole.TenantOwner } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.TenantDisabled },
            { new TenantLocalState { TenantId = "tenant-a", Status = (TenantStatus)999, Members = { [Caller] = TenantRole.TenantOwner } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.UnmappedStatus },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = { ["other-user"] = TenantRole.TenantOwner } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.MissingMember },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = { [Caller] = TenantRole.TenantReader } }, ConversationTenantAccessRequirement.Write, ConversationTenantAccessDenialReason.InsufficientRole },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = { [Caller] = (TenantRole)999 } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.UnmappedRole },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = { [Caller] = TenantRole.TenantOwner, ["other-user"] = (TenantRole)999 } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.UnmappedRole },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = { [" "] = TenantRole.TenantOwner } }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.TenantProjectionPoisoned },
            { new TenantLocalState { TenantId = "tenant-a", Status = TenantStatus.Active, Members = null! }, ConversationTenantAccessRequirement.Read, ConversationTenantAccessDenialReason.MalformedProjection },
        };

    /// <summary>
    /// Supplies projection health denial scenarios.
    /// </summary>
    public static TheoryData<ConversationTenantProjectionHealth, ConversationTenantAccessDenialReason> ProjectionHealthDenials()
        => new()
        {
            { new ConversationTenantProjectionHealth(IsStale: true, Version: 42, Watermark: "watermark-safe"), ConversationTenantAccessDenialReason.TenantAccessStale },
            { new ConversationTenantProjectionHealth(HasGap: true, Version: 42, Watermark: "watermark-safe"), ConversationTenantAccessDenialReason.TenantAccessGapDetected },
            { new ConversationTenantProjectionHealth(HasRollback: true, Version: 42, Watermark: "watermark-safe"), ConversationTenantAccessDenialReason.TenantAccessRolledBack },
            { new ConversationTenantProjectionHealth(IsPoisoned: true, Version: 42, Watermark: "watermark-safe"), ConversationTenantAccessDenialReason.TenantProjectionPoisoned },
        };

    private static TenantLocalState ActiveTenant(TenantRole role = TenantRole.TenantOwner, string tenantId = "tenant-a")
        => new()
        {
            TenantId = tenantId,
            Status = TenantStatus.Active,
            Members = { [Caller] = role },
        };

    private class FakeTenantProjectionStore(TenantLocalState? state) : ITenantProjectionStore
    {
        public int GetCount { get; private set; }

        public Func<string, CancellationToken, Task<TenantLocalState?>> OnGet { get; init; } =
            (_, _) => Task.FromResult(state);

        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return OnGet(tenantId, cancellationToken);
        }

        public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SignalingTenantProjectionStore(
        TenantLocalState state,
        ConversationTenantProjectionHealth health)
        : FakeTenantProjectionStore(state),
            IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(health);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
