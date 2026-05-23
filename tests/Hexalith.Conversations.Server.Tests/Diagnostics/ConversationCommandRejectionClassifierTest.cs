// <copyright file="ConversationCommandRejectionClassifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies the bounded rejection classifier maps error codes and denial reasons correctly.
/// </summary>
public sealed class ConversationCommandRejectionClassifierTest
{
    [Fact]
    public void ClassifyErrorCode_TenantBindingMissing_ReturnsTenantBinding()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.TenantBindingMissing)
            .ShouldBe(ConversationCommandRejectionClass.TenantBinding);

    [Fact]
    public void ClassifyErrorCode_TenantIsolationViolation_ReturnsTenantIsolation()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.TenantIsolationViolation)
            .ShouldBe(ConversationCommandRejectionClass.TenantIsolation);

    [Fact]
    public void ClassifyErrorCode_TenantProjectionStale_ReturnsTenantProjectionUnavailable()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.TenantProjectionStale)
            .ShouldBe(ConversationCommandRejectionClass.TenantProjectionUnavailable);

    [Fact]
    public void ClassifyErrorCode_CommandValidationFailed_ReturnsValidation()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.CommandValidationFailed)
            .ShouldBe(ConversationCommandRejectionClass.Validation);

    [Fact]
    public void ClassifyErrorCode_SchemaVersionUnsupported_ReturnsValidation()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.SchemaVersionUnsupported)
            .ShouldBe(ConversationCommandRejectionClass.Validation);

    [Fact]
    public void ClassifyErrorCode_IdempotencyConflict_ReturnsIdempotency()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.IdempotencyConflict)
            .ShouldBe(ConversationCommandRejectionClass.Idempotency);

    [Fact]
    public void ClassifyErrorCode_AuditSinkUnavailable_ReturnsAuditUnavailable()
        => ConversationCommandRejectionClassifier.Classify(ConversationErrorCode.AuditSinkUnavailable)
            .ShouldBe(ConversationCommandRejectionClass.AuditUnavailable);

    [Fact]
    public void ClassifyDenialReason_MissingTenant_ReturnsMissingContext()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.MissingTenant)
            .ShouldBe(ConversationTenantDenialClass.MissingContext);

    [Fact]
    public void ClassifyDenialReason_MalformedTenant_ReturnsMissingContext()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.MalformedTenant)
            .ShouldBe(ConversationTenantDenialClass.MissingContext);

    [Fact]
    public void ClassifyDenialReason_UnknownTenant_ReturnsUnknownOrDisabled()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.UnknownTenant)
            .ShouldBe(ConversationTenantDenialClass.UnknownOrDisabled);

    [Fact]
    public void ClassifyDenialReason_TenantDisabled_ReturnsUnknownOrDisabled()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.TenantDisabled)
            .ShouldBe(ConversationTenantDenialClass.UnknownOrDisabled);

    [Fact]
    public void ClassifyDenialReason_InsufficientRole_ReturnsInsufficientAccess()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.InsufficientRole)
            .ShouldBe(ConversationTenantDenialClass.InsufficientAccess);

    [Fact]
    public void ClassifyDenialReason_TenantAccessUnavailable_ReturnsProjectionUnavailable()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.TenantAccessUnavailable)
            .ShouldBe(ConversationTenantDenialClass.ProjectionUnavailable);

    [Fact]
    public void ClassifyDenialReason_TenantAccessStale_ReturnsProjectionUnavailable()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.TenantAccessStale)
            .ShouldBe(ConversationTenantDenialClass.ProjectionUnavailable);

    [Fact]
    public void ClassifyDenialReason_TenantMismatch_ReturnsContextMismatch()
        => ConversationCommandRejectionClassifier.Classify(ConversationTenantAccessDenialReason.TenantMismatch)
            .ShouldBe(ConversationTenantDenialClass.ContextMismatch);
}
