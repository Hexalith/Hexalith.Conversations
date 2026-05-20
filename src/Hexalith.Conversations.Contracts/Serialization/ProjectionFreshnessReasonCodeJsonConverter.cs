// <copyright file="ProjectionFreshnessReasonCodeJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ProjectionFreshnessReasonCodeJsonConverter : ConversationStringValueJsonConverter<ProjectionFreshnessReasonCode>
{
    protected override ProjectionFreshnessReasonCode Create(string value) => ProjectionFreshnessReasonCode.Parse(value);

    protected override string GetValue(ProjectionFreshnessReasonCode value) => value.Value;
}
