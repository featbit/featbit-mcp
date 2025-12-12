var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults for observability (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add the MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map MCP endpoint
app.MapMcp();

app.Run();
