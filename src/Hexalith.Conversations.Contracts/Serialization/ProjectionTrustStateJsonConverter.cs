// <copyright file="ProjectionTrustStateJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ProjectionTrustStateJsonConverter : ConversationStringValueJsonConverter<ProjectionTrustState>
{
    protected override ProjectionTrustState Create(string value) => ProjectionTrustState.Parse(value);

    protected override string GetValue(ProjectionTrustState value) => value.Value;
}
