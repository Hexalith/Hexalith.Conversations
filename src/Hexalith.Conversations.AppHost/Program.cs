// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

_ = ConversationsAppHostTopology.AddConversations(builder);

builder.Build().Run();
