var builder = DistributedApplication.CreateBuilder(args);

var mcpServer = builder.AddProject<Projects.FeatBit_McpServer>("featbit-mcp-server")
    .WithExternalHttpEndpoints();

builder.Build().Run();
