// <copyright file="Story16_3WireObservation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>A measured request to a production projection receiver, without retained credentials.</summary>
/// <param name="Path">The canonical receiver route.</param>
/// <param name="Status">The actual HTTP response status.</param>
/// <param name="ChannelAuthenticated">Whether the real application-channel credential was present.</param>
/// <param name="WorkloadAuthenticated">Whether the operation assertion was present.</param>
/// <param name="Outcome">The content-free production outcome or refusal code.</param>
/// <param name="Request">The original dispatch input, retained in memory for duplicate controls.</param>
internal sealed record Story16_3WireObservation(string Path, int Status, bool ChannelAuthenticated,
    bool WorkloadAuthenticated, ProjectionDispatchRequest Request, string Outcome);
