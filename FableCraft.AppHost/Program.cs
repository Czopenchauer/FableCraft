var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.FableCraft_Server>("fablecraft-server");

builder.Build().Run();