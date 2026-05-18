// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
