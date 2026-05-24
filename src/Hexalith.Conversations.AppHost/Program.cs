// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Hexalith_Conversations_Admin_Web>("conversations-admin-web");

builder.Build().Run();
