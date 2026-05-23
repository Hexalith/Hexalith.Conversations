// <copyright file="ConversationPublicationFailureClassifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Server.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies the bounded publication failure classifier maps error codes correctly.
/// </summary>
public sealed class ConversationPublicationFailureClassifierTest
{
    [Fact]
    public void ClassifyCode_SchemaVersionUnsupported_ReturnsUnsupportedSchema()
        => ConversationPublicationFailureClassifier
            .Classify(ConversationErrorCode.SchemaVersionUnsupported)
            .ShouldBe(ConversationPublicationFailureClass.UnsupportedSchema);

    [Fact]
    public void ClassifyCode_TenantContextMismatch_ReturnsTenantViolation()
        => ConversationPublicationFailureClassifier
            .Classify(ConversationErrorCode.TenantContextMismatch)
            .ShouldBe(ConversationPublicationFailureClass.TenantViolation);

    [Fact]
    public void ClassifyCode_TenantIsolationViolation_ReturnsTenantViolation()
        => ConversationPublicationFailureClassifier
            .Classify(ConversationErrorCode.TenantIsolationViolation)
            .ShouldBe(ConversationPublicationFailureClass.TenantViolation);

    [Fact]
    public void ClassifyCode_CommandValidationFailed_ReturnsTransientFailure()
        => ConversationPublicationFailureClassifier
            .Classify(ConversationErrorCode.CommandValidationFailed)
            .ShouldBe(ConversationPublicationFailureClass.TransientFailure);
}
