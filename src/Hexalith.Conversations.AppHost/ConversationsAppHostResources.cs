// <copyright file="ConversationsAppHostResources.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.AppHost;

using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

/// <summary>
/// Contains the Conversations AppHost resources composed for local development.
/// </summary>
/// <param name="StateStore">The shared Dapr state-store component.</param>
/// <param name="PubSub">The shared Dapr pub/sub component.</param>
/// <param name="EventStore">The local EventStore command gateway project.</param>
/// <param name="ConversationsServer">The Conversations domain server project.</param>
/// <param name="AdminWeb">The Conversations admin web project.</param>
public sealed record ConversationsAppHostResources(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> EventStore,
    IResourceBuilder<ProjectResource> ConversationsServer,
    IResourceBuilder<ProjectResource> AdminWeb);
